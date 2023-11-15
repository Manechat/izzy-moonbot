using Discord;
using Izzy_Moonbot.Helpers;
using System.Text.RegularExpressions;
using static Izzy_Moonbot.Adapters.IIzzyClient;

namespace Izzy_Moonbot.Adapters;

// This file is for test implementations of the Discord.NET
// adapter interfaces in IzzyInterfaces.cs.

public class TestUser : IIzzyUser
{
    public ulong Id { get; }
    public string Username { get; }
    public string? GlobalName => Username;
    public bool IsBot { get; }

    public TestUser(string username, ulong id, bool isBot = false)
    {
        Id = id;
        Username = username;
        IsBot = isBot;
    }
}

public class StubGuildUser : IIzzyUser
{
    public ulong Id { get; }
    public string Username { get; }
    public string? GlobalName => Username;
    public bool IsBot { get; }
    public DateTimeOffset? JoinedAt { get; }

    public StubGuildUser(string username, ulong id, DateTimeOffset? joinedAt = null, bool isBot = false)
    {
        Id = id;
        Username = username;
        IsBot = isBot;
        JoinedAt = joinedAt;
    }
}

public class TestGuildUser : IIzzyGuildUser
{
    private readonly StubGuildUser _user;
    public ulong Id { get => _user.Id; }
    public string Username { get => _user.Username; }
    public string? GlobalName => Username;
    public string? Nickname => Username;
    public string DisplayName => Username;
    public bool IsBot { get => _user.IsBot; }
    public DateTimeOffset? JoinedAt { get => _user.JoinedAt; }

    private readonly StubGuild _guildBackref;
    private readonly StubClient _clientBackref;
    public IIzzyGuild Guild { get => new TestGuild(_guildBackref, _clientBackref); }

    public TestGuildUser(StubGuildUser user, StubGuild guild, StubClient client)
    {
        _user = user;
        _guildBackref = guild;
        _clientBackref = client;
    }

    public IReadOnlyCollection<IIzzyRole> Roles =>
        _guildBackref.UserRoles.ContainsKey(Id) ?
            _guildBackref.UserRoles[Id].Select(roleId => _guildBackref.Roles.Where(r => r.Id == roleId).Single()).ToList() :
            new List<IIzzyRole>();

    public async Task AddRoleAsync(ulong roleId, RequestOptions? _requestOptions)
    {
        var ur = _guildBackref.UserRoles;
        if (ur.ContainsKey(Id))
            ur[Id].Add(roleId);
        else
            ur[Id] = new List<ulong> { roleId };
    }
    public async Task AddRolesAsync(IEnumerable<ulong> roles, RequestOptions? _requestOptions)
    {
        var ur = _guildBackref.UserRoles;
        if (ur.ContainsKey(Id))
            ur[Id].AddRange(roles);
        else
            ur[Id] = roles.ToList();
    }
    public async Task RemoveRoleAsync(ulong roleId, RequestOptions? _requestOptions)
    {
        var ur = _guildBackref.UserRoles;
        if (ur.ContainsKey(Id))
            ur[Id].Remove(roleId);
    }
    public async Task SetTimeOutAsync(TimeSpan span, RequestOptions? requestOptions)
    {
        /* not implemented */
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
    public MessageComponent? Components;
    public IList<IAttachment> Attachments = new List<IAttachment>();
    public IList<IEmbed> Embeds = new List<IEmbed>();
    public IList<IStickerItem> Stickers = new List<IStickerItem>();

    public StubMessage(ulong id, string content, ulong authorId,
        MessageComponent? components = null,
        IList<IEmbed>? embeds = null,
        IList<IStickerItem>? stickers = null,
        List<IAttachment>? attachments = null)
    {
        Id = id;
        Content = content;
        AuthorId = authorId;
        if (components is not null)
            Components = components;
        if (embeds is not null)
            Embeds = embeds;
        if (stickers is not null)
            Stickers = stickers;
        if (attachments is not null)
            Attachments = attachments;
    }

    public string CleanContent
    {
        // The docs for the real property read:
        // "A string that contains the body of the message stripped of mentions, markdown, emojis and pings; note that this field may be empty if there is an embed."
        // For now, our tests do far less than that.
        get
        {
            var regex = new Regex("<(#|@|@&)[0-9]+>");
            return regex.Replace(Content, "");
        }
    }
}

public class StubMessageProperties : IIzzyMessageProperties
{
    public Optional<string> Content { get; set; } = new Optional<string>();
    public Optional<MessageComponent> Components { get; set; } = new Optional<MessageComponent>();
}

public class TestMessage : IIzzyUserMessage
{
    public ulong Id => _message.Id;
    public string Content => _message.Content;
    public string CleanContent => _message.CleanContent;
    public IIzzyUser Author { get; }
    public IIzzyMessageChannel Channel => new TestMessageChannel(_channelBackref.Name, _channelBackref.Id, _guildBackref, _clientBackref);

    public IReadOnlyCollection<IMessageComponent> Components => _message.Components is null ? new List<IMessageComponent>() : _message.Components.Components;
    public IReadOnlyCollection<IAttachment> Attachments => (IReadOnlyCollection<IAttachment>)_message.Attachments;
    public IReadOnlyCollection<IEmbed> Embeds => (IReadOnlyCollection<IEmbed>)_message.Embeds;
    public IReadOnlyCollection<IStickerItem> Stickers => (IReadOnlyCollection<IStickerItem>)_message.Stickers;
    public DateTimeOffset CreatedAt => new DateTimeOffset(2010, 10, 10, 0, 0, 0, TimeSpan.Zero);
    public DateTimeOffset Timestamp => new DateTimeOffset(2010, 10, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly StubMessage _message;
    private readonly StubChannel _channelBackref;
    public readonly StubGuild _guildBackref; // TODO: can we use different Discord.NET APIs to avoid having to hackily mimic its circularity?
    private readonly StubClient _clientBackref;

    public TestMessage(StubMessage message, IIzzyUser author, StubChannel channel, StubGuild guild, StubClient client)
    {
        _message = message;
        Author = author;
        _channelBackref = channel;
        _guildBackref = guild;
        _clientBackref = client;
    }

    public async Task ReplyAsync(string message)
    {
        var lastId = _channelBackref.Messages.Last().Id;
        _channelBackref.Messages.Add(new StubMessage(lastId + 1, message, Author.Id));
    }

    public Task ModifyAsync(Action<IIzzyMessageProperties> action)
    {
        var stubProps = new StubMessageProperties();
        action(stubProps);
        if (stubProps.Content.IsSpecified)
            _message.Content = stubProps.Content.Value;
        if (stubProps.Components.IsSpecified)
            _message.Components = stubProps.Components.Value;
        return Task.CompletedTask;
    }

    public async Task DeleteAsync() =>
        _channelBackref.Messages.RemoveAll(m => m.Id == Id);

    public string GetJumpUrl() => $"https://discord.com/channels/{_guildBackref.Id}/{_channelBackref.Id}/{_message.Id}";
}

public class TestAttachment : IAttachment
{
    private readonly FileAttachment _fileAttachment;

    public TestAttachment(FileAttachment fa) => _fileAttachment = fa;

    public ulong Id => throw new NotImplementedException();
    public string Filename => _fileAttachment.FileName;
    public string Url => throw new NotImplementedException();
    public string ProxyUrl => throw new NotImplementedException();
    public int Size => throw new NotImplementedException();
    public int? Height => throw new NotImplementedException();
    public int? Width => throw new NotImplementedException();
    public bool Ephemeral => throw new NotImplementedException();
    public string Description => _fileAttachment.Description;
    public string ContentType => throw new NotImplementedException();
    public AttachmentFlags Flags => throw new NotImplementedException();
    double? IAttachment.Duration => throw new NotImplementedException();
    string IAttachment.Waveform => throw new NotImplementedException();
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
    public string Name => _channel.Name;
    public ulong Id => _channel.Id;

    private readonly StubChannel _channel;
    private readonly StubGuild _guildBackref;
    private readonly StubClient _clientBackref;

    public TestTextChannel(StubGuild guild, StubChannel channel, StubClient client)
    {
        _channel = channel;
        _guildBackref = guild;
        _clientBackref = client;
    }

    public IReadOnlyCollection<IIzzyUser> Users {
        get
        {
            if (!_guildBackref.ChannelAccessRole.ContainsKey(Id))
                return _guildBackref.Users;
            else
            {
                var accessRoleId = _guildBackref.ChannelAccessRole[Id];
                return _guildBackref.Users.Where(u =>
                    _guildBackref.UserRoles.ContainsKey(u.Id) && _guildBackref.UserRoles[u.Id].Contains(accessRoleId)
                ).ToList();
            }
        }
    }

    public async Task<IIzzyUserMessage> SendMessageAsync(
        string message,
        AllowedMentions? allowedMentions = null,
        MessageComponent? components = null,
        RequestOptions? options = null,
        ISticker[]? stickers = null,
        Embed[]? embeds = null)
    {
        var maybeUser = _guildBackref.Users.Find(u => u.Id == _clientBackref.CurrentUser.Id);
        if (maybeUser is StubGuildUser user)
        {
            var lastId = _channel.Messages.LastOrDefault()?.Id ?? 0;
            var stubMessage = new StubMessage(lastId + 1, message, user.Id, components: components, stickers: stickers, embeds: embeds);
            _channel.Messages.Add(stubMessage);
            return new TestMessage(stubMessage, user, _channel, _guildBackref, _clientBackref);
        }
        else
            throw new KeyNotFoundException($"CurrentUser is somehow not in this channel");
    }

    public async Task<IIzzyMessage?> GetMessageAsync(ulong messageId)
    {
        var stubMessage = _channel.Messages.Where(m => m.Id == messageId).SingleOrDefault();
        if (stubMessage is null)
            return null;
        var maybeUser = _guildBackref.Users.Find(u => u.Id == stubMessage.AuthorId);
        if (maybeUser is null)
            return null;
        return new TestMessage(stubMessage, maybeUser, _channel, _guildBackref, _clientBackref);
    }

    public async Task<IIzzyUserMessage> SendFileAsync(FileAttachment fa, string message)
    {
        var maybeUser = _guildBackref.Users.Find(u => u.Id == _clientBackref.CurrentUser.Id);
        if (maybeUser is StubGuildUser user)
        {
            var lastId = _channel.Messages.LastOrDefault()?.Id ?? 0;
            var stubMessage = new StubMessage(lastId + 1, message, user.Id, attachments: new List<IAttachment> { new TestAttachment(fa) });
            _channel.Messages.Add(stubMessage);
            return new TestMessage(stubMessage, user, _channel, _guildBackref, _clientBackref);
        }
        else
            throw new KeyNotFoundException($"CurrentUser is somehow not in this channel");
    }

    public async IAsyncEnumerable<IReadOnlyCollection<IIzzyMessage>> GetMessagesAsync(ulong firstMessageId, Direction dir, int limit)
    {
        var firstMessageIndex = _channel.Messages.FindIndex(m => m.Id == firstMessageId);

        var stubMessages = (dir == Direction.After) ?
            _channel.Messages.Skip(firstMessageIndex + 1).Take(limit) :
            Enumerable.Reverse(_channel.Messages).Skip(_channel.Messages.Count - firstMessageIndex + 1).Take(limit);

        foreach (var m in stubMessages)
        {
            var user = _guildBackref.Users.Find(u => u.Id == m.AuthorId);
            if (user is null) continue;
            yield return new List<IIzzyMessage>{
                new TestMessage(m, user, _channel, _guildBackref, _clientBackref)
            };
        }
    }
    public async IAsyncEnumerable<IReadOnlyCollection<IIzzyMessage>> GetMessagesAsync(int messageCount)
    {
        var stubMessages = _channel.Messages.TakeLast(messageCount);

        foreach (var m in stubMessages)
        {
            var user = _guildBackref.Users.Find(u => u.Id == m.AuthorId);
            if (user is null) continue;
            yield return new List<IIzzyMessage>{
                new TestMessage(m, user, _channel, _guildBackref, _clientBackref)
            };
        }
    }
}

public class TestMessageChannel : IIzzyMessageChannel
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

    public async Task<IIzzyUser?> GetUserAsync(ulong userId)
    {
        if (_guildBackref.Users.Find(u => u.Id == userId) is IIzzyUser user)
            if (_guildBackref.ChannelAccessRole.ContainsKey(Id))
                if (_guildBackref.UserRoles.ContainsKey(userId) && _guildBackref.UserRoles[userId].Contains(_guildBackref.ChannelAccessRole[Id]))
                    return user;
                else
                    return null;
            else
                return user;
        else
            throw new KeyNotFoundException($"No user with id {userId}");
    }

    public async Task<IIzzyUserMessage> SendMessageAsync(
        string message,
        AllowedMentions? allowedMentions = null,
        MessageComponent? components = null,
        RequestOptions? options = null,
        ISticker[]? stickers = null)
    {
        var maybeUser = _guildBackref.Users.Find(u => u.Id == _clientBackref.CurrentUser.Id);
        var maybeChannel = _guildBackref.Channels.Find(c => c.Id == Id);
        if (maybeUser is StubGuildUser user && maybeChannel is StubChannel channel)
        {
            var lastId = channel.Messages.LastOrDefault()?.Id ?? 0;
            var stubMessage = new StubMessage(lastId + 1, message, user.Id, components: components, stickers: stickers);
            channel.Messages.Add(stubMessage);
            return new TestMessage(stubMessage, user, channel, _guildBackref, _clientBackref);
        }
        else
            if (maybeUser is null)
                throw new KeyNotFoundException($"CurrentUser is somehow not in this channel");
            else
                throw new KeyNotFoundException($"This channel is somehow not in its own guild");
    }

    public async Task<IIzzyUserMessage> SendFileAsync(FileAttachment fa, string message)
    {
        var maybeUser = _guildBackref.Users.Find(u => u.Id == _clientBackref.CurrentUser.Id);
        var maybeChannel = _guildBackref.Channels.Find(c => c.Id == Id);
        if (maybeUser is StubGuildUser user && maybeChannel is StubChannel channel)
        {
            var lastId = channel.Messages.LastOrDefault()?.Id ?? 0;
            var stubMessage = new StubMessage(lastId + 1, message, user.Id, attachments: new List<IAttachment> { new TestAttachment(fa) });
            channel.Messages.Add(stubMessage);
            return new TestMessage(stubMessage, user, channel, _guildBackref, _clientBackref);
        }
        else
            if (maybeUser is null)
                throw new KeyNotFoundException($"CurrentUser is somehow not in this channel");
            else
                throw new KeyNotFoundException($"This channel is somehow not in its own guild");
    }

    // Izzy only checks the channel type to avoid processing unusual ones
    public ChannelType? GetChannelType() => ChannelType.Text;
}

public class StubGuild
{
    public ulong Id { get; }
    public string Name { get; }

    public List<TestRole> Roles;
    public List<StubGuildUser> Users;
    public List<StubChannel> Channels;
    public StubChannel? RulesChannel = null;

    public Dictionary<ulong, List<ulong>> UserRoles = new();
    public Dictionary<ulong, ulong> ChannelAccessRole = new(); // public channels are absent
    public ISet<ulong> BannedUserIds { get; } = new HashSet<ulong>();

    public StubGuild(ulong id, string name, List<TestRole> roles, List<StubGuildUser> users, List<StubChannel> channels)
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
    public ulong Id { get => _stubGuild.Id; }
    public string Name { get => _stubGuild.Name; }
    public IReadOnlyCollection<IIzzySocketTextChannel> TextChannels { get; }
    public IReadOnlyCollection<IIzzyRole> Roles { get => _stubGuild.Roles; }

    readonly private StubGuild _stubGuild;
    readonly private StubClient _clientBackref;

    public TestGuild(StubGuild stub, StubClient client)
    {
        _stubGuild = stub;
        _clientBackref = client;
        TextChannels = stub.Channels.Select(channel => new TestTextChannel(stub, channel, client)).ToList();
    }

    public Task<IReadOnlyCollection<IIzzyUser>> SearchUsersAsync(string userSearchQuery)
    {
        return Task.FromResult((IReadOnlyCollection<IIzzyUser>)_stubGuild.Users.Where(user => user.Username.StartsWith(userSearchQuery)).ToList());
    }

    public IIzzyGuildUser? GetUser(ulong userId) =>
        _stubGuild.Users.Where(user => user.Id == userId).Select(user => new TestGuildUser(new StubGuildUser(user.Username, user.Id, user.JoinedAt), _stubGuild, _clientBackref)).SingleOrDefault();
    public IIzzyRole? GetRole(ulong roleId) => Roles.Where(role => role.Id == roleId).SingleOrDefault();
    public IIzzySocketGuildChannel? GetChannel(ulong channelId)
    {
        var tc = TextChannels.Where(tc => tc.Id == channelId).SingleOrDefault();
        return tc is null ? null : new TestGuildChannel(tc.Name, tc.Id);
    }
    public IIzzySocketTextChannel? GetTextChannel(ulong channelId)
    {
        var stubChannel = _stubGuild.Channels.Where(tc => tc.Id == channelId).SingleOrDefault();
        return stubChannel is null ? null : new TestTextChannel(_stubGuild, stubChannel, _clientBackref);
    }

    public async Task AddBanAsync(ulong userId, int _pruneDays, string _reason) =>
        _stubGuild.BannedUserIds.Add(userId);
    public async Task<bool> GetIsBannedAsync(ulong userId) =>
        _stubGuild.BannedUserIds.Contains(userId);
    public async Task RemoveBanAsync(ulong userId, string? _reason) =>
        _stubGuild.BannedUserIds.Remove(userId);
    public async Task SetBanner(Image _image) { }
    public IIzzySocketTextChannel? RulesChannel => _stubGuild.RulesChannel is null ? null :
        new TestTextChannel(_stubGuild, _stubGuild.RulesChannel, _clientBackref);
}

public class TestIzzyContext : IIzzyContext
{
    public bool IsPrivate { get; }
    public IIzzyGuild? Guild { get; }
    public IIzzyClient Client { get; }
    public IIzzyMessageChannel Channel { get; }
    public IIzzyUserMessage Message { get; }
    public IIzzyUser User { get; }

    public TestIzzyContext(bool isPrivate, IIzzyGuild guild, IIzzyClient client, IIzzyMessageChannel messageChannel, IIzzyUserMessage message, IIzzyUser user)
    {
        IsPrivate = isPrivate;
        Guild = guild;
        Client = client;
        Channel = messageChannel;
        Message = message;
        User = user;
    }
}

public class StubClient : IIzzyClient
{
    public IIzzyUser CurrentUser { get => _currentUser; }
    public IReadOnlyCollection<IIzzyGuild> Guilds { get => _guilds.Select(g => new TestGuild(g, this)).ToList(); }

    public event Func<IIzzySocketMessageComponent, Task>? ButtonExecuted;
    public event Func<ulong, IIzzyMessage?, ulong, IIzzyMessageChannel?, Task>? MessageDeleted;
    public event Func<IIzzyMessage, Task>? MessageReceived;
    public event Func<string?, IIzzyMessage, IIzzyMessageChannel, Task>? MessageUpdated;
    public event Func<IIzzyGuildUser, Task>? UserJoined;

    public StubClient(IIzzyUser user, List<StubGuild> guilds)
    {
        _currentUser = user;
        _guilds = guilds;
    }

    private ulong NextId = 0;

    readonly private IIzzyUser _currentUser;
    readonly private List<StubGuild> _guilds;

    public async Task<TestIzzyContext> AddMessageAsync(ulong guildId, ulong channelId, ulong userId, string textContent,
        List<IAttachment>? attachments = null,
        Embed[]? embeds = null)
    {
        if (_guilds.Find(g => g.Id == guildId) is StubGuild guild)
        {
            var maybeUser = guild.Users.Find(u => u.Id == userId);
            var maybeChannel = guild.Channels.Find(c => c.Id == channelId);
            if (maybeUser is StubGuildUser user && maybeChannel is StubChannel channel)
            {
                var stubMessage = new StubMessage(NextId++, textContent, userId, attachments: attachments, embeds: embeds);
                channel.Messages.Add(stubMessage);

                var testMessage = new TestMessage(stubMessage, user, channel, guild, this);
                var t = MessageReceived?.Invoke(testMessage);
                if (t is not null) await t;

                return new TestIzzyContext(
                    false,
                    new TestGuild(guild, this),
                    this,
                    new TestMessageChannel(channel.Name, channelId, guild, this),
                    testMessage,
                    // note: runtime type must be IIzzyGuildUser whenever possible, not just IIzzyUser
                    // since we don't support DMs yet, that means it's always a *GuildUser
                    new TestGuildUser(new StubGuildUser(user.Username, user.Id, user.JoinedAt), guild, this)
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

    public async Task UpdateMessageAsync(ulong guildId, ulong channelId, ulong messageId, string newContent)
    {
        if (_guilds.Find(g => g.Id == guildId) is StubGuild guild)
        {
            var maybeChannel = guild.Channels.Find(c => c.Id == channelId);
            if (maybeChannel is StubChannel channel)
            {
                var maybeMessage = channel.Messages.Find(m => m.Id == messageId);
                if (maybeMessage is StubMessage message)
                {
                    var oldContent = message.Content;
                    message.Content = newContent;

                    var maybeUser = guild.Users.Find(u => u.Id == message.AuthorId);
                    var testMessage = new TestMessage(message, maybeUser!, channel, guild, this);

                    var testMessageChannel = new TestMessageChannel(channel.Name, channelId, guild, this);

                    var t = MessageUpdated?.Invoke(oldContent, testMessage, testMessageChannel);
                    if (t is not null) await t;
                }
                else throw new KeyNotFoundException($"No message with id {messageId}");
            }
            else throw new KeyNotFoundException($"No channel with id {channelId}");
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

        public Task UpdateAsync(Action<IIzzyMessageProperties> action) =>
            throw new NotImplementedException();

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

    private void MessageDeletedExecute(ulong messageId, IIzzyMessage? content, ulong channelId, IIzzyMessageChannel? channel)
    {
        MessageDeleted?.Invoke(messageId, content, channelId, channel);
    }

    public IIzzyContext MakeContext(IIzzyUserMessage message)
    {
        var stubGuild = (message as TestMessage)!._guildBackref;
        var testGuild = new TestGuild(stubGuild, this);
        return new TestIzzyContext(
            false,
            testGuild,
            this,
            message.Channel,
            message,
            // note: runtime type must be IIzzyGuildUser whenever possible, not just IIzzyUser
            // since we don't support DMs yet, that means it's always a *GuildUser
            new TestGuildUser(new StubGuildUser(message.Author.Username, message.Author.Id), stubGuild, this)
        );
    }

    public async Task<IIzzyUser?> GetUserAsync(ulong userId)
    {
        if (CurrentUser.Id == userId) return CurrentUser;
        foreach (var g in _guilds)
        {
            var u = g.Users.Find(u => u.Id == userId);
            if (u is not null) return u;
        }
        return null;
    }

    public IIzzyGuild? GetGuild(ulong guildId)
    {
        var g = _guilds.Find(g => g.Id == guildId);
        return g is null ? null : new TestGuild(g, this);
    }

    // Fortunately Izzy only has to think about DMs between herself and another user,
    // so pretending that there is a single List of DMs for each userId is fine.
    public Dictionary<ulong, List<StubMessage>> DirectMessages { get; } = new Dictionary<ulong, List<StubMessage>>();

    public async Task SendDirectMessageAsync(ulong userId, string text, MessageComponent? components = null)
    {
        if (await GetUserAsync(userId) == null) return;

        var dm = new StubMessage(Convert.ToUInt64(DirectMessages.Count), text, CurrentUser.Id, components: components);
        if (DirectMessages.ContainsKey(userId))
            DirectMessages[userId].Add(dm);
        else
            DirectMessages[userId] = new List<StubMessage> { dm };
    }

    public async Task JoinUser(string username, ulong userId, StubGuild stubGuild)
    {
        var stubMember = new StubGuildUser(username, userId, DateTimeHelper.FakeUtcNow);
        stubGuild.Users.Add(stubMember);
        var member = new TestGuildUser(stubMember, stubGuild, this);

        var t = UserJoined?.Invoke(member);
        if (t is not null) await t;
    }
}
