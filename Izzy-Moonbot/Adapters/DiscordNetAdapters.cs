using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Izzy_Moonbot.Adapters.IIzzyClient;

namespace Izzy_Moonbot.Adapters;

public class DiscordNetUserAdapter : IIzzyUser
{
    private readonly IUser _user;

    public DiscordNetUserAdapter(IUser user)
    {
        _user = user;
    }

    public ulong Id { get => _user.Id; }
    public string Username { get => _user.Username; }
    public string? GlobalName { get => _user.GlobalName; }
    public bool IsBot => _user.IsBot;
    public async Task<IIzzyUserMessage> SendMessageAsync(string text) =>
        new DiscordNetUserMessageAdapter(await _user.SendMessageAsync(text));

    public override string? ToString()
    {
        return _user.ToString();
    }
}

public class SocketGuildUserAdapter : IIzzyGuildUser
{
    private readonly SocketGuildUser _user;

    public SocketGuildUserAdapter(SocketGuildUser user)
    {
        _user = user;
    }

    public ulong Id { get => _user.Id; }
    public string Username { get => _user.Username; }
    public string? GlobalName { get => _user.GlobalName; }
    public string? Nickname { get => _user.Nickname; }
    public string DisplayName { get => _user.DisplayName; }
    public int Hierarchy { get => _user.Hierarchy; }
    public bool IsBot => _user.IsBot;
    public IReadOnlyCollection<IIzzyRole> Roles => _user.Roles.Select(r => new DiscordNetRoleAdapter(r)).ToList();
    public IIzzyGuild Guild { get => new SocketGuildAdapter(_user.Guild); }
    public DateTimeOffset? JoinedAt { get => _user.JoinedAt; }

    public override string? ToString()
    {
        return _user.ToString();
    }

    public async Task AddRoleAsync(ulong roleId, RequestOptions? requestOptions) =>
        await _user.AddRoleAsync(roleId, requestOptions);
    public async Task AddRolesAsync(IEnumerable<ulong> roles, RequestOptions? requestOptions) =>
        await _user.AddRolesAsync(roles, requestOptions);
    public async Task RemoveRoleAsync(ulong memberRole, RequestOptions? requestOptions) =>
        await _user.RemoveRoleAsync(memberRole, requestOptions);
    public async Task SetTimeOutAsync(TimeSpan span, RequestOptions? requestOptions) =>
        await _user.SetTimeOutAsync(span, requestOptions);
}

public class DiscordNetRoleAdapter : IIzzyRole
{
    private readonly IRole _role;

    public DiscordNetRoleAdapter(IRole role)
    {
        _role = role;
    }

    public string Name { get => _role.Name; }

    public ulong Id { get => _role.Id; }
    public int Position { get => _role.Position; }

    public override string? ToString()
    {
        return _role.ToString();
    }
}

class MessagePropertiesAdapter : IIzzyMessageProperties
{
    public Optional<string> Content { set => _msg.Content = value; }
    public Optional<MessageComponent> Components { set => _msg.Components = value; }

    private MessageProperties _msg;

    public MessagePropertiesAdapter(MessageProperties msg)
    {
        _msg = msg;
    }
};

public class DiscordNetMessageAdapter : IIzzyMessage
{
    private readonly IMessage _message;

    public DiscordNetMessageAdapter(IMessage message)
    {
        _message = message;
    }

    public ulong Id { get => _message.Id; }
    public string Content { get => _message.Content; }
    public string CleanContent => _message.CleanContent;
    public IIzzyUser Author
    {
        get => _message.Author is SocketGuildUser author ?
            new SocketGuildUserAdapter(author) :
            new DiscordNetUserAdapter(_message.Author);
    }
    public IIzzyMessageChannel Channel => new DiscordNetMessageChannelAdapter(_message.Channel);
    public IReadOnlyCollection<IMessageComponent> Components => _message.Components;
    public IReadOnlyCollection<IAttachment> Attachments => _message.Attachments;
    public IReadOnlyCollection<IEmbed> Embeds => _message.Embeds;
    public IReadOnlyCollection<IStickerItem> Stickers => _message.Stickers;
    public DateTimeOffset CreatedAt => _message.CreatedAt;
    public DateTimeOffset Timestamp => _message.Timestamp;
    public async Task DeleteAsync() => await _message.DeleteAsync();
    public string GetJumpUrl() => _message.GetJumpUrl();
}

public class DiscordNetUserMessageAdapter : IIzzyUserMessage
{

    private readonly IUserMessage _message;

    public DiscordNetUserMessageAdapter(IUserMessage message)
    {
        _message = message;
    }

    public ulong Id { get => _message.Id; }
    public string Content { get => _message.Content; }
    public string CleanContent => _message.CleanContent;
    public IIzzyUser Author
    {
        get => _message.Author is SocketGuildUser author ?
            new SocketGuildUserAdapter(author) :
            new DiscordNetUserAdapter(_message.Author);
    }
    public IIzzyMessageChannel Channel => new DiscordNetMessageChannelAdapter(_message.Channel);

    public IReadOnlyCollection<IMessageComponent> Components => _message.Components;
    public IReadOnlyCollection<IAttachment> Attachments => _message.Attachments;
    public IReadOnlyCollection<IEmbed> Embeds => _message.Embeds;
    public IReadOnlyCollection<IStickerItem> Stickers => _message.Stickers;
    public DateTimeOffset CreatedAt => _message.CreatedAt;
    public DateTimeOffset Timestamp => _message.Timestamp;

    public async Task ModifyAsync(Action<IIzzyMessageProperties> action)
    {
        await _message.ModifyAsync(msg => {
            action(new MessagePropertiesAdapter(msg));
        });
    }
    public async Task DeleteAsync()
    {
        await _message.DeleteAsync();
    }
    public string GetJumpUrl() => _message.GetJumpUrl();
}

public class SocketTextChannelAdapter : IIzzySocketTextChannel
{
    private readonly SocketTextChannel _channel;

    public SocketTextChannelAdapter(SocketTextChannel channel)
    {
        _channel = channel;
    }

    public ulong Id { get => _channel.Id; }

    public string Name { get => _channel.Name; }

    public IReadOnlyCollection<IIzzyUser> Users {
        get => _channel.Users.Select(user => new DiscordNetUserAdapter(user)).ToList();
    }
    public async Task<IIzzyUserMessage> SendMessageAsync(
        string message,
        AllowedMentions? allowedMentions = null,
        MessageComponent? components = null,
        RequestOptions? options = null,
        ISticker[]? stickers = null,
        Embed[]? embeds = null
    )
    {
        var sentMesssage = await _channel.SendMessageAsync(message, allowedMentions: allowedMentions, components: components, options: options, stickers: stickers, embeds: embeds);
        return new DiscordNetUserMessageAdapter(sentMesssage);
    }
    public async Task<IIzzyMessage?> GetMessageAsync(ulong messageId)
    {
        var msg = await _channel.GetMessageAsync(messageId);
        return msg is null ? null : new DiscordNetMessageAdapter(msg);
    }
    public async Task<IIzzyUserMessage> SendFileAsync(FileAttachment fa, string message)
    {
        var sentMesssage = await _channel.SendFileAsync(fa, message);
        return new DiscordNetUserMessageAdapter(sentMesssage);
    }
    public async IAsyncEnumerable<IReadOnlyCollection<IIzzyMessage>> GetMessagesAsync(ulong firstMessageId, Direction dir, int limit)
    {
        await foreach (var syncEnumerable in _channel.GetMessagesAsync(firstMessageId, dir, limit))
            yield return syncEnumerable.Select(m => new DiscordNetMessageAdapter(m)).ToList();
    }
    public async IAsyncEnumerable<IReadOnlyCollection<IIzzyMessage>> GetMessagesAsync(int count)
    {
        await foreach (var syncEnumerable in _channel.GetMessagesAsync(count))
            yield return syncEnumerable.Select(m => new DiscordNetMessageAdapter(m)).ToList();
    }

    public override string? ToString()
    {
        return _channel.ToString();
    }
}

public class DiscordNetMessageChannelAdapter : IIzzyMessageChannel
{
    // this needs to be readable elsewhere in this file because Discord.NET's API fundamentally
    // requires runtime type casting in order to make a context out of a client and a message
    public readonly IMessageChannel _channel;

    public DiscordNetMessageChannelAdapter(IMessageChannel channel)
    {
        _channel = channel;
    }

    public ulong Id { get => _channel.Id; }

    public string Name { get => _channel.Name; }

    public async Task<IIzzyUser?> GetUserAsync(ulong userId)
    {
        var user = await _channel.GetUserAsync(userId);
        return user is null ? null : new DiscordNetUserAdapter(user);
    }
    public async Task<IIzzyUserMessage> SendMessageAsync(
        string message,
        AllowedMentions? allowedMentions = null,
        MessageComponent? components = null,
        RequestOptions? options = null,
        ISticker[]? stickers = null
    )
    {
        var sentMesssage = await _channel.SendMessageAsync(message, allowedMentions: allowedMentions, components: components, options: options, stickers: stickers);
        return new DiscordNetUserMessageAdapter(sentMesssage);
    }
    public async Task<IIzzyUserMessage> SendFileAsync(FileAttachment fa, string message)
    {
        var sentMesssage = await _channel.SendFileAsync(fa, message);
        return new DiscordNetUserMessageAdapter(sentMesssage);
    }

    public override string? ToString()
    {
        return _channel.ToString();
    }
}

public class SocketGuildChannelAdapter : IIzzySocketGuildChannel
{
    private readonly SocketGuildChannel _channel;

    public SocketGuildChannelAdapter(SocketGuildChannel channel)
    {
        _channel = channel;
    }

    public ulong Id { get => _channel.Id; }

    public string Name { get => _channel.Name; }

    public override string? ToString()
    {
        return _channel.ToString();
    }
}

public class SocketGuildAdapter : IIzzyGuild
{
    private readonly SocketGuild _guild;

    public SocketGuildAdapter(SocketGuild guild)
    {
        _guild = guild;
    }

    public ulong Id { get => _guild.Id; }
    public string Name { get => _guild.Name; }

    public async Task<IReadOnlyCollection<IIzzyUser>> SearchUsersAsync(string userSearchQuery)
    {
        var users = await _guild.SearchUsersAsync(userSearchQuery);
        return users.Select(user => new DiscordNetUserAdapter(user)).ToList();
    }

    public IReadOnlyCollection<IIzzySocketTextChannel> TextChannels {
        get => _guild.TextChannels.Select(stc => new SocketTextChannelAdapter(stc)).ToList();
    }

    public IReadOnlyCollection<IIzzyRole> Roles {
        get => _guild.Roles.Select(role => new DiscordNetRoleAdapter(role)).ToList();
    }

    public IIzzyGuildUser? GetUser(ulong userId)
    {
        var user = _guild.GetUser(userId);
        return user is null ? null : new SocketGuildUserAdapter(user);
    }
    public IIzzyRole? GetRole(ulong roleId)
    {
        var role = _guild.GetRole(roleId);
        return role is null ? null : new DiscordNetRoleAdapter(role);
    }
    public IIzzySocketGuildChannel? GetChannel(ulong channelId)
    {
        var channel = _guild.GetChannel(channelId);
        return channel is null ? null : new SocketGuildChannelAdapter(channel);
    }
    public IIzzySocketTextChannel? GetTextChannel(ulong channelId)
    {
        var textChannel = _guild.GetTextChannel(channelId);
        return textChannel is null ? null : new SocketTextChannelAdapter(textChannel);
    }
    public async Task AddBanAsync(ulong userId, int pruneDays, string reason) =>
        await _guild.AddBanAsync(userId, pruneDays: pruneDays, reason: reason);
    public async Task<bool> GetIsBannedAsync(ulong userId) =>
        await _guild.GetBanAsync(userId) != null;
    public async Task RemoveBanAsync(ulong userId, string? reason) =>
        await _guild.RemoveBanAsync(userId, reason is null ? null : new RequestOptions { AuditLogReason = reason });
    public async Task SetBanner(Image image) =>
        await _guild.ModifyAsync(properties => properties.Banner = image);
    public IIzzySocketTextChannel? RulesChannel => new SocketTextChannelAdapter(_guild.RulesChannel);
}

public class SocketMessageComponentAdapter : IIzzySocketMessageComponent
{
    private SocketMessageComponent _component;

    public SocketMessageComponentAdapter(SocketMessageComponent component)
    {
        _component = component;
    }

    public IIzzyHasId User { get => new IdHaver(_component.User.Id); }
    public IIzzyHasId Message { get => new IdHaver(_component.Message.Id); }
    public IIzzyHasCustomId Data { get => new CustomIdHaver(_component.Data.CustomId); }
    public async Task UpdateAsync(Action<IIzzyMessageProperties> action) =>
        await _component.UpdateAsync(msg => {
            action(new MessagePropertiesAdapter(msg));
        });
    public Task DeferAsync() => _component.DeferAsync();
}

public class DiscordSocketClientAdapter : IIzzyClient
{
    private readonly DiscordSocketClient _client;

    public DiscordSocketClientAdapter(DiscordSocketClient client)
    {
        _client = client;

        _client.MessageReceived += async (msg) =>
            MessageReceived?.Invoke(
                msg is SocketUserMessage sumsg ? new DiscordNetUserMessageAdapter(sumsg) : new DiscordNetMessageAdapter(msg)
            );
        _client.MessageUpdated += async (oldMessage, newMessage, channel) =>
            MessageUpdated?.Invoke(
                oldMessage.Value?.Content,
                newMessage is SocketUserMessage sumsg ? new DiscordNetUserMessageAdapter(sumsg) : new DiscordNetMessageAdapter(newMessage),
                new DiscordNetMessageChannelAdapter(channel)
            );
        _client.ButtonExecuted += async (arg) =>
            ButtonExecuted?.Invoke(new SocketMessageComponentAdapter(arg));
        _client.MessageDeleted += async (message, channel) =>
            MessageDeleted?.Invoke(
                message.Id,
                message.Value is null ? null : new DiscordNetMessageAdapter(message.Value),
                channel.Id,
                channel.Value is null ? null : new DiscordNetMessageChannelAdapter(channel.Value)
            );
    }

    public IIzzyUser CurrentUser { get => new DiscordNetUserAdapter(_client.CurrentUser); }

    public IReadOnlyCollection<IIzzyGuild> Guilds {
        get => _client.Guilds.Select(guild => new SocketGuildAdapter(guild)).ToList();
    }

    public event Func<IIzzyMessage, Task>? MessageReceived;
    public event Func<string?, IIzzyMessage, IIzzyMessageChannel, Task>? MessageUpdated;
    public event Func<IIzzySocketMessageComponent, Task>? ButtonExecuted;
    public event Func<ulong, IIzzyMessage?, ulong, IIzzyMessageChannel?, Task>? MessageDeleted;

    public IIzzyContext MakeContext(IIzzyUserMessage message) =>
        new ClientAndMessageContextAdapter(this, message);

    public IIzzyGuild? GetGuild(ulong userId)
    {
        var user = _client.GetGuild(userId);
        return user is null ? null : new SocketGuildAdapter(user);
    }
    public async Task<IIzzyUser?> GetUserAsync(ulong userId)
    {
        var user = await _client.GetUserAsync(userId);
        return user is null ? null : new DiscordNetUserAdapter(user);
    }
    public async Task SendDirectMessageAsync(ulong userId, string text, MessageComponent? components = null)
    {
        var user = await _client.GetUserAsync(userId);
        if (user == null) return;

        await user.SendMessageAsync(text, components: components);
    }
}

public class ClientAndMessageContextAdapter : IIzzyContext
{
    private readonly IIzzyClient _client;
    private readonly IIzzyUserMessage _message;

    public ClientAndMessageContextAdapter(IIzzyClient client, IIzzyUserMessage message)
    {
        _client = client;
        _message = message;
    }

    // the casting strategy is taken from Discord.NET's source
    public bool IsPrivate => (_message.Channel as DiscordNetMessageChannelAdapter)?._channel is IPrivateChannel;

    public IIzzyGuild? Guild
    {
        get
        {
            // the casting strategy is taken from Discord.NET's source
            var maybeGuild = ((_message.Channel as DiscordNetMessageChannelAdapter)?._channel as SocketGuildChannel)?.Guild;
            return maybeGuild is null ? null : new SocketGuildAdapter(maybeGuild);
        }
    }

    public IIzzyClient Client => _client;

    public IIzzyMessageChannel Channel => _message.Channel;

    public IIzzyUserMessage Message => _message;

    public IIzzyUser User => _message.Author;
}

public class SocketCommandContextAdapter : IIzzyContext
{
    private readonly SocketCommandContext _context;

    public SocketCommandContextAdapter(SocketCommandContext context)
    {
        _context = context;
    }

    public bool IsPrivate { get => _context.IsPrivate; }

    public IIzzyGuild Guild { get => new SocketGuildAdapter(_context.Guild); }

    public IIzzyClient Client { get => new DiscordSocketClientAdapter(_context.Client); }

    public IIzzyMessageChannel Channel { get => new DiscordNetMessageChannelAdapter(_context.Channel); }

    public IIzzyUserMessage Message { get => new DiscordNetUserMessageAdapter(_context.Message); }

    public IIzzyUser User {
        get => _context.User is SocketGuildUser user ?
            new SocketGuildUserAdapter(user) :
            new DiscordNetUserAdapter(_context.User);
    }
}
