using Discord;
using Discord.WebSocket;
using Izzy_Moonbot.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Channels;
using static Izzy_Moonbot.Adapters.IIzzyClient;

namespace Izzy_Moonbot.Adapters;

// Unfortunately CS1998 doesn't understand the concept of a synchronous implementation of
// an asynchronous API, so there's no way to satisfy it without spawning useless threads.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

public class TestUser : IIzzyUser
{
    public string Name { get; }
    public ulong Id { get; }

    public TestUser(string name, ulong id)
    {
        Name = name;
        Id = id;
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

    public Task ModifyAsync(Action<MessageProperties> action)
    {
        throw new NotImplementedException();
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
    public List<TestRole> Roles;
    public List<TestUser> Users;
    public List<StubChannel> Channels;

    public StubGuild(ulong id, List<TestRole> roles, List<TestUser> users, List<StubChannel> channels)
    {
        Id = id;
        Roles = roles;
        Users = users;
        Channels = channels;
    }
}

public class TestGuild : IIzzyGuild
{
    public ulong Id { get; }
    public IReadOnlyCollection<IIzzySocketTextChannel> TextChannels { get; }
    public IReadOnlyCollection<IIzzyRole> Roles { get; }

    private IList<TestUser> _users;

    public TestGuild(StubGuild stub)
    {
        Id = stub.Id;
        _users= stub.Users;
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
        return Task.FromResult((IReadOnlyCollection<IIzzyUser>)_users.Where(user => user.Name.StartsWith(userSearchQuery)).ToList());
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

    public TestIzzyContext(bool isPrivate, IIzzyGuild guild, IIzzyClient client, IIzzySocketMessageChannel messageChannel, IIzzyMessage message)
    {
        IsPrivate = isPrivate;
        Guild = guild;
        Client = client;
        Channel = messageChannel;
        Message = message;
    }
}

public class StubClient : IIzzyClient
{
    public IIzzyUser CurrentUser { get => _currentUser; }
    public IReadOnlyCollection<IIzzyGuild> Guilds { get => (IReadOnlyCollection<IIzzyGuild>)_guilds; }

    public event Func<SocketMessageComponent, Task>? ButtonExecuted;
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
                    new TestMessage(stubMessage.Id, stubMessage.Content, user, channel)
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
}
