using Discord;
using Discord.WebSocket;
using static Izzy_Moonbot.Adapters.IIzzyClient;

namespace Izzy_Moonbot.Adapters;

// This file is for test implementations of the Discord.NET
// adapter interfaces in IzzyInterfaces.cs.

// Unfortunately CS1998 doesn't understand the concept of a synchronous implementation of
// an asynchronous API, so there's no way to satisfy it without spawning useless threads.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

public class TestUser : IIzzyUser
{
    public ulong Id { get; }
    public string Username { get; }

    public TestUser(string username, ulong id)
    {
        Id = id;
        Username = username;
    }
}

public class TestRole : IIzzyRole
{
    public string Name { get; }
    public ulong Id { get; }

    public TestRole(string name, ulong id)
    {
        Name = name;
        Id = id;
    }

    public override string ToString() => Name;
}

public class StubMessage
{
    public ulong Id;
    public string Content;
    public ulong AuthorId;

    public StubMessage(ulong id, string content, ulong authorId)
    {
        Id = id;
        Content = content;
        AuthorId = authorId;
    }
}

public class StubMessageProperties : IIzzyMessageProperties
{
    public Optional<string> Content { get; set; } = new Optional<string>();
    public Optional<MessageComponent> Components { get; set; } = new Optional<MessageComponent>();
}

public class TestMessage : IIzzyMessage
{
    public ulong Id { get; }
    public string Content { get; }
    public IIzzyUser Author { get; }

    private StubChannel _channelBackref;

    public TestMessage(ulong id, string content, IIzzyUser author, StubChannel channel)
    {
        Id = id;
        Content = content;
        Author = author;
        _channelBackref = channel;
    }

    public async Task ReplyAsync(string message)
    {
        var lastId = _channelBackref.Messages.Last().Id;
        _channelBackref.Messages.Add(new StubMessage(lastId + 1, message, Author.Id));
    }

    public Task ModifyAsync(Action<IIzzyMessageProperties> action)
    {
        var messageBackref = _channelBackref.Messages.Where(m => m.Id == Id).Single();
        var stubProps = new StubMessageProperties();
        action(stubProps);
        if (stubProps.Content.IsSpecified)
        {
            messageBackref.Content = stubProps.Content.Value;
        }
        // TODO: message.Components not implemented yet
        return Task.CompletedTask;
    }
}

public class StubChannel
{
    public ulong Id;
    public string Name;
    public List<StubMessage> Messages;

    public StubChannel(ulong id, string name, List<StubMessage>? messages = null)
    {
        Id = id;
        Name = name;
        Messages = messages ?? new List<StubMessage>();
    }
}

public class TestGuildChannel : IIzzySocketGuildChannel
{
    public string Name { get; }
    public ulong Id { get; }

    public TestGuildChannel(string name, ulong id)
    {
        Name = name;
        Id = id;
    }

    public override string ToString() => Name;
}

public class TestTextChannel : IIzzySocketTextChannel
{
    public string Name { get; }
    public ulong Id { get; }

    private readonly StubGuild _guildBackref;

    public TestTextChannel(StubGuild guild, ulong id, string name)
    {
        Id = id;
        Name = name;
        _guildBackref = guild;
    }

    public IReadOnlyCollection<IIzzyUser> Users { get => _guildBackref.Users; }
}

public class TestMessageChannel : IIzzySocketMessageChannel
{
    public string Name { get; }
    public ulong Id { get; }

    private readonly StubGuild _guildBackref;
    private readonly StubClient _clientBackref;

    public TestMessageChannel(string name, ulong id, StubGuild guildBackref, StubClient client)
    {
        Name = name;
        Id = id;
        _guildBackref = guildBackref;
        _clientBackref = client;
    }

    public async Task<IIzzyUser> GetUserAsync(ulong userId)
    {
        if (_guildBackref.Users.Find(u => u.Id == userId) is IIzzyUser user)
            return user;
        else
            throw new KeyNotFoundException($"No user with id {userId}");
    }

    public async Task<IIzzyMessage> SendMessageAsync(
        string message,
        AllowedMentions? allowedMentions = null,
        MessageComponent? components = null,
        RequestOptions? options = null)
    {
        var maybeUser = _guildBackref.Users.Find(u => u.Id == _clientBackref.CurrentUser.Id);
        var maybeChannel = _guildBackref.Channels.Find(c => c.Id == Id);
        if (maybeUser is TestUser user && maybeChannel is StubChannel channel)
        {
            var lastId = channel.Messages.Last().Id;
            var stubMessage = new StubMessage(lastId + 1, message, user.Id);
            channel.Messages.Add(stubMessage);
            return new TestMessage(stubMessage.Id, stubMessage.Content, user, channel);
        }
        else
            if (maybeUser is null)
                throw new KeyNotFoundException($"CurrentUser is somehow not in this channel");
            else
                throw new KeyNotFoundException($"This channel is somehow not in its own guild");
    }
}

public class StubGuild
{
    public ulong Id { get; }
    public string Name { get; }
    public List<TestRole> Roles;
    public List<TestUser> Users;
    public List<StubChannel> Channels;

    public StubGuild(ulong id, string name, List<TestRole> roles, List<TestUser> users, List<StubChannel> channels)
    {
        Id = id;
        Name = name;
        Roles = roles;
        Users = users;
        Channels = channels;
    }
}

public class TestGuild : IIzzyGuild
{
    public ulong Id { get; }
    public string Name { get; }
    public IReadOnlyCollection<IIzzySocketTextChannel> TextChannels { get; }
    public IReadOnlyCollection<IIzzyRole> Roles { get; }

    private IList<TestUser> _users;

    public TestGuild(StubGuild stub)
    {
        Id = stub.Id;
        Name = stub.Name;
        _users = stub.Users;
        Roles = stub.Roles;
        TextChannels = stub.Channels.Select(c => new TestTextChannel(stub, c.Id, c.Name)).ToList();
    }

    public TestGuild(ulong id, IList<TestUser> users, IList<TestTextChannel> textChannels, IList<TestRole> roles)
    {
        Id = id;
        _users = users;
        TextChannels = (IReadOnlyCollection<IIzzySocketTextChannel>)textChannels;
        Roles = (IReadOnlyCollection<IIzzyRole>)roles;
    }

    public Task<IReadOnlyCollection<IIzzyUser>> SearchUsersAsync(string userSearchQuery)
    {
        return Task.FromResult((IReadOnlyCollection<IIzzyUser>)_users.Where(user => user.Username.StartsWith(userSearchQuery)).ToList());
    }

    public IIzzyUser GetUser(ulong userId) => _users.Where(user => user.Id == userId).Single();
    public IIzzyRole GetRole(ulong roleId) => Roles.Where(role => role.Id == roleId).Single();
    public IIzzySocketGuildChannel GetChannel(ulong channelId)
    {
        var tc = TextChannels.Where(tc => tc.Id == channelId).Single();
        return new TestGuildChannel(tc.Name, tc.Id);
    }
}

public class TestIzzyContext : IIzzyContext
{
    public bool IsPrivate { get; }
    public IIzzyGuild Guild { get; }
    public IIzzyClient Client { get; }
    public IIzzySocketMessageChannel Channel { get; }
    public IIzzyMessage Message { get; }
    public IIzzyUser User { get; }

    public TestIzzyContext(bool isPrivate, IIzzyGuild guild, IIzzyClient client, IIzzySocketMessageChannel messageChannel, IIzzyMessage message, IIzzyUser user)
    {
        IsPrivate = isPrivate;
        Guild = guild;
        Client = client;
        Channel = messageChannel;
        Message = message;
        User = user;
    }
}

public class IdHaver : IIzzyHasId
{
    public ulong Id { get; }
    public IdHaver(ulong id) => Id = id;
}

public class CustomIdHaver : IIzzyHasCustomId
{
    public string CustomId { get; }
    public CustomIdHaver(string id) => CustomId = id;
}

public class StubClient : IIzzyClient
{
    public IIzzyUser CurrentUser { get => _currentUser; }
    public IReadOnlyCollection<IIzzyGuild> Guilds { get => (IReadOnlyCollection<IIzzyGuild>)_guilds; }

    public event Func<IIzzySocketMessageComponent, Task>? ButtonExecuted;
    public event Func<IIzzyHasId, IIzzyHasId, Task>? MessageDeleted;

    public StubClient(TestUser user, List<StubGuild> guilds)
    {
        _currentUser = user;
        _guilds = guilds;
    }

    private ulong NextId = 0;

    private TestUser _currentUser;
    private List<StubGuild> _guilds;

    public TestIzzyContext AddMessage(ulong guildId, ulong channelId, ulong userId, string textContent)
    {
        if (_guilds.Find(g => g.Id == guildId) is StubGuild guild)
        {
            var maybeUser = guild.Users.Find(u => u.Id == userId);
            var maybeChannel = guild.Channels.Find(c => c.Id == channelId);
            if (maybeUser is TestUser user && maybeChannel is StubChannel channel)
            {
                var stubMessage = new StubMessage(NextId++, textContent, userId);
                channel.Messages.Add(stubMessage);
                return new TestIzzyContext(
                    false,
                    new TestGuild(guild),
                    this,
                    new TestMessageChannel(channel.Name, channelId, guild, this),
                    new TestMessage(stubMessage.Id, stubMessage.Content, user, channel),
                    user
                );
            }
            else
                if (maybeUser is null)
                    throw new KeyNotFoundException($"No user with id {userId}");
                else
                    throw new KeyNotFoundException($"No channel with id {channelId}");
        }
        else throw new KeyNotFoundException($"No guild with id {guildId}");
    }

    public class TestSocketMessageComponent : IIzzySocketMessageComponent
    {
        public IIzzyHasId User { get; }
        public IIzzyHasId Message { get; }
        public IIzzyHasCustomId Data { get; }

        public bool Acknowledged { get; set; } = false;

        public TestSocketMessageComponent(ulong userId, ulong messageId, string customId)
        {
            User = new IdHaver(userId);
            Message = new IdHaver(messageId);
            Data = new CustomIdHaver(customId);
        }

        public Task DeferAsync()
        {
            Acknowledged = true;
            return Task.CompletedTask;
        }
    }
    public void FireButtonExecuted(ulong userId, ulong messageId, string customId)
    {
        ButtonExecuted?.Invoke(new TestSocketMessageComponent(userId, messageId, customId));
    }

    public void FireMessageDeleted(ulong messageId, ulong channelId)
    {
        MessageDeleted?.Invoke(new IdHaver(messageId), new IdHaver(channelId));
    }
}
