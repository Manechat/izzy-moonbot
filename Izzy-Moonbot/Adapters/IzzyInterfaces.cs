using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Izzy_Moonbot.Adapters;

public interface IIzzyUser
{
    ulong Id { get; }
    string Username { get; }
    string Discriminator { get => "1234"; }
}

public interface IIzzyRole
{
    string Name { get; }
    ulong Id { get; }
    string Mention { get => $"<@&{Id}>"; }
}

public interface IIzzyMessageProperties
{
    public Optional<string> Content { set; }
    public Optional<MessageComponent> Components { set; }
}

public interface IIzzyMessage
{
    ulong Id { get; }
    string Content { get; }
    IIzzyUser Author { get; }
    Task ReplyAsync(string message);
    Task ModifyAsync(Action<IIzzyMessageProperties> action);
}

public interface IIzzySocketMessageChannel
{
    ulong Id { get; }
    string Name { get; }
    Task<IIzzyUser> GetUserAsync(ulong userId);
    Task<IIzzyMessage> SendMessageAsync(
        string message,
        AllowedMentions? allowedMentions = null,
        MessageComponent? components = null,
        RequestOptions? options = null
    );
}

public interface IIzzySocketTextChannel
{
    ulong Id { get; }
    string Name { get; }
    IReadOnlyCollection<IIzzyUser> Users { get; }
    Task<IIzzyMessage> SendMessageAsync(
        string message,
        AllowedMentions? allowedMentions = null,
        MessageComponent? components = null,
        RequestOptions? options = null,
        Embed[]? embeds = null
    );
}

public interface IIzzySocketGuildChannel
{
    ulong Id { get; }
    string Name { get; }
}

public interface IIzzyGuild
{
    ulong Id { get; }
    string Name { get; }
    Task<IReadOnlyCollection<IIzzyUser>> SearchUsersAsync(string userSearchQuery);
    IReadOnlyCollection<IIzzySocketTextChannel> TextChannels { get; }
    IReadOnlyCollection<IIzzyRole> Roles { get; }
    IIzzyUser GetUser(ulong userId);
    IIzzyRole GetRole(ulong roleId);
    IIzzySocketGuildChannel GetChannel(ulong channelId);
    IIzzySocketTextChannel GetTextChannel(ulong channelId);
}

public interface IIzzyClient
{
    IIzzyUser CurrentUser { get; }
    IReadOnlyCollection<IIzzyGuild> Guilds { get; }

    public interface IIzzyHasCustomId { string CustomId { get; } }
    public interface IIzzySocketMessageComponent {
        IIzzyHasId User { get; }
        IIzzyHasId Message { get; }
        IIzzyHasCustomId Data { get; }
        Task DeferAsync();
    }
    event Func<IIzzySocketMessageComponent, Task> ButtonExecuted;

    public interface IIzzyHasId { ulong Id { get; } }
    event Func<IIzzyHasId, IIzzyHasId, Task> MessageDeleted;
}

public interface IIzzyContext
{
    bool IsPrivate { get; }
    IIzzyGuild Guild { get; }
    IIzzyClient Client { get; }
    IIzzySocketMessageChannel Channel { get; }
    IIzzyMessage Message { get; }
    IIzzyUser User { get; }
}

