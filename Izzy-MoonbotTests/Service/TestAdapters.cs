using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Izzy_Moonbot.Settings;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Izzy_Moonbot.Adapters;

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

public class TestMessage : IIzzyMessage
{
    public ulong Id { get; }
    public string Content { get; }
    public IIzzyUser Author { get; }

    private readonly Func<string, Task> _replier;

    public TestMessage(ulong id, string content, IIzzyUser author, Func<string, Task> replier)
    {
        Id = id;
        Content = content;
        Author = author;

        _replier = replier;
    }

    public Task ReplyAsync(string message) => _replier(message);

    public Task ModifyAsync(Action<object> action)
    {
        throw new NotImplementedException();
    }
}

public class TestTextChannel : IIzzySocketTextChannel
{
    public string Name { get; }
    public ulong Id { get; }

    private readonly Func<IReadOnlyCollection<IIzzyUser>> _usersGetter;

    public TestTextChannel(string name, ulong id, Func<IReadOnlyCollection<IIzzyUser>> usersGetter)
    {
        Name = name;
        Id = id;

        _usersGetter = usersGetter;
    }

    public IReadOnlyCollection<IIzzyUser> Users { get => _usersGetter(); }
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

public class TestMessageChannel : IIzzySocketMessageChannel
{
    public string Name { get; }
    public ulong Id { get; }

    // Test-only API
    public List<(ulong, string, IIzzyUser?)> Messages { get; }

    private readonly Func<ulong, Task<IIzzyUser>> _userGetter;

    public TestMessageChannel(string name, ulong id, Func<ulong, Task<IIzzyUser>> userGetter)
    {
        Name = name;
        Id = id;
        Messages = new List<(ulong, string, IIzzyUser?)>();

        _userGetter = userGetter;
    }

    public async Task<IIzzyUser> GetUserAsync(ulong userId) => await _userGetter(userId);

    private void AddMessageToChannel(string message)
    {
        Messages.Add((0ul, message, null));
    }

    public async Task<IIzzyMessage> SendMessageAsync(
        string message,
        AllowedMentions? allowedMentions = null,
        MessageComponent? components = null,
        RequestOptions? options = null
    )
    {
        AddMessageToChannel(message);
        return new TestMessage(0ul, message, null, async (replyMessage) => AddMessageToChannel(replyMessage));
    }
}

public class TestGuild : IIzzyGuild
{
    public ulong Id { get; }
    public IReadOnlyCollection<IIzzySocketTextChannel> TextChannels { get; }
    public IReadOnlyCollection<IIzzyRole> Roles { get; }

    private IList<TestUser> _users;

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

public class TestClient: IIzzyClient
{
    public IIzzyUser CurrentUser { get; }
    public IReadOnlyCollection<IIzzyGuild> Guilds { get; }

    public TestClient(TestUser user, IList<TestGuild> guilds)
    {
        CurrentUser = user;
        Guilds = (IReadOnlyCollection<IIzzyGuild>)guilds;
    }
}

public class TestIzzyContext : IIzzyContext
{
    public bool IsPrivate { get; }
    public IIzzyGuild Guild { get; }
    public IIzzyClient Client { get; }
    public IIzzySocketMessageChannel Channel { get; }
    public IIzzyMessage Message { get; }

    public TestIzzyContext(bool isPrivate, TestGuild guild, TestClient client, TestMessageChannel messageChannel, IIzzyMessage message)
    {
        IsPrivate = isPrivate;
        Guild = guild;
        Client = client;
        Channel = messageChannel;
        Message = message;
    }
}
