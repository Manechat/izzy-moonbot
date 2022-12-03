using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Izzy_Moonbot.Settings;
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
    public string Discriminator { get => _user.Discriminator; }

    public override string? ToString()
    {
        return _user.ToString();
    }
}

public class DiscordNetGuildUserAdapter : IIzzyGuildUser
{
    private readonly IGuildUser _user;

    public DiscordNetGuildUserAdapter(IGuildUser user)
    {
        _user = user;
    }

    public ulong Id { get => _user.Id; }
    public string Username { get => _user.Username; }
    public string Discriminator { get => _user.Discriminator; }
    public string DisplayName { get => _user.DisplayName; }

    public override string? ToString()
    {
        return _user.ToString();
    }
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

public class SocketUserMessageAdapter : IIzzyMessage
{
    private readonly SocketUserMessage _message;

    public SocketUserMessageAdapter(SocketUserMessage message)
    {
        _message = message;
    }

    public ulong Id { get => _message.Id; }
    public string Content { get => _message.Content; }
    public IIzzyUser Author { get => new DiscordNetUserAdapter(_message.Author); }
    public async Task ReplyAsync(string message)
    {
        await _message.ReplyAsync(message);
    }
    public async Task ModifyAsync(Action<IIzzyMessageProperties> action)
    {
        await _message.ModifyAsync(msg => {
            action(new MessagePropertiesAdapter(msg));
        });
    }
}

public class RestUserMessageAdapter : IIzzyMessage
{

    private readonly RestUserMessage _message;

    public RestUserMessageAdapter(RestUserMessage message)
    {
        _message = message;
    }

    public ulong Id { get => _message.Id; }
    public string Content { get => _message.Content; }
    public IIzzyUser Author { get => new DiscordNetUserAdapter(_message.Author); }

    public async Task ReplyAsync(string message)
    {
        await _message.ReplyAsync(message);
    }
    public async Task ModifyAsync(Action<IIzzyMessageProperties> action)
    {
        await _message.ModifyAsync(msg => {
            action(new MessagePropertiesAdapter(msg));
        });
    }
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
    public async Task<IIzzyMessage> SendMessageAsync(
        string message,
        AllowedMentions? allowedMentions = null,
        MessageComponent? components = null,
        RequestOptions? options = null,
        Embed[]? embeds = null
    )
    {
        var sentMesssage = await _channel.SendMessageAsync(message, allowedMentions: allowedMentions, components: components, options: options, embeds: embeds);
        return new RestUserMessageAdapter(sentMesssage);
    }

    public override string? ToString()
    {
        return _channel.ToString();
    }
}

public class SocketMessageChannelAdapter : IIzzySocketMessageChannel
{
    private readonly ISocketMessageChannel _channel;

    public SocketMessageChannelAdapter(ISocketMessageChannel channel)
    {
        _channel = channel;
    }

    public ulong Id { get => _channel.Id; }

    public string Name { get => _channel.Name; }

    public async Task<IIzzyUser> GetUserAsync(ulong userId)
    {
        var user = await _channel.GetUserAsync(userId);
        return new DiscordNetUserAdapter(user);
    }
    public async Task<IIzzyMessage> SendMessageAsync(
        string message,
        AllowedMentions? allowedMentions = null,
        MessageComponent? components = null,
        RequestOptions? options = null
    )
    {
        var sentMesssage = await _channel.SendMessageAsync(message, allowedMentions: allowedMentions, components: components, options: options);
        return new RestUserMessageAdapter(sentMesssage);
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

    public IIzzyGuildUser GetUser(ulong userId) => new DiscordNetGuildUserAdapter(_guild.GetUser(userId));
    public IIzzyRole GetRole(ulong roleId) => new DiscordNetRoleAdapter(_guild.GetRole(roleId));
    public IIzzySocketGuildChannel GetChannel(ulong channelId) => new SocketGuildChannelAdapter(_guild.GetChannel(channelId));
    public IIzzySocketTextChannel GetTextChannel(ulong channelId) => new SocketTextChannelAdapter(_guild.GetTextChannel(channelId));
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
    public Task DeferAsync() => _component.DeferAsync();
}

public class DiscordSocketClientAdapter : IIzzyClient
{
    private readonly DiscordSocketClient _client;

    public DiscordSocketClientAdapter(DiscordSocketClient client)
    {
        _client = client;

        _client.ButtonExecuted += async (arg) =>
        {
            ButtonExecuted?.Invoke(new SocketMessageComponentAdapter(arg));
        };
        _client.MessageDeleted += async (message, channel) =>
        {
            MessageDeleted?.Invoke(new IdHaver(message.Id), new IdHaver(channel.Id));
        };
    }

    public IIzzyUser CurrentUser { get => new DiscordNetUserAdapter(_client.CurrentUser); }

    public IReadOnlyCollection<IIzzyGuild> Guilds {
        get => _client.Guilds.Select(guild => new SocketGuildAdapter(guild)).ToList();
    }

    public event Func<IIzzySocketMessageComponent, Task>? ButtonExecuted;
    public event Func<IIzzyHasId, IIzzyHasId, Task>? MessageDeleted;
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

    public IIzzySocketMessageChannel Channel { get => new SocketMessageChannelAdapter(_context.Channel); }

    public IIzzyMessage Message { get => new SocketUserMessageAdapter(_context.Message); }

    public IIzzyUser User { get => new DiscordNetUserAdapter(_context.User); }
}
