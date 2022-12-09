using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Izzy_Moonbot.Adapters;

public interface IIzzyUser
{
    ulong Id { get; }
    string Username { get; }
    string Discriminator { get => "1234"; }
    bool IsBot { get; }
}

public interface IIzzyGuildUser : IIzzyUser
{
    string DisplayName { get; }
    IReadOnlyCollection<IIzzyRole> Roles { get; }
    Task AddRoleAsync(ulong roleId, RequestOptions? requestOptions);
    Task AddRolesAsync(IEnumerable<ulong> roles, RequestOptions? requestOptions);
    Task RemoveRoleAsync(ulong memberRole, RequestOptions? requestOptions);
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
    string CleanContent { get; }
    IIzzyUser Author { get; }
    IIzzyMessageChannel Channel { get; }

    IReadOnlyCollection<IMessageComponent> Components { get; }
    IReadOnlyCollection<IAttachment> Attachments { get; }
    IReadOnlyCollection<IEmbed> Embeds { get; }
    IReadOnlyCollection<IStickerItem> Stickers { get; }

    // Izzy only ever checks message type to avoid processing unusual ones (e.g. UserPremiumGuildSubscription messages)
    MessageType Type => MessageType.Default;

    // all we ever do with these is attach them to an embed, so no point properly faking them
    DateTimeOffset Timestamp { get => new DateTimeOffset(2010, 10, 10, 0, 0, 0, TimeSpan.Zero); }
    DateTimeOffset? EditedTimestamp { get => null; }

    Task DeleteAsync();
    string GetJumpUrl();
}

public interface IIzzyUserMessage : IIzzyMessage
{
    Task ReplyAsync(string message);
    Task ModifyAsync(Action<IIzzyMessageProperties> action);
}

public interface IIzzyMessageChannel
{
    ulong Id { get; }
    string Name { get; }
    Task<IIzzyUser?> GetUserAsync(ulong userId);
    Task<IIzzyUserMessage> SendMessageAsync(
        string message,
        AllowedMentions? allowedMentions = null,
        MessageComponent? components = null,
        RequestOptions? options = null
    );

    // Izzy only checks the channel type to avoid processing unusual ones
    ChannelType GetChannelType() => ChannelType.Text;
}

public interface IIzzySocketTextChannel
{
    ulong Id { get; }
    string Name { get; }
    IReadOnlyCollection<IIzzyUser> Users { get; }
    Task<IIzzyUserMessage> SendMessageAsync(
        string message,
        AllowedMentions? allowedMentions = null,
        MessageComponent? components = null,
        RequestOptions? options = null,
        Embed[]? embeds = null
    );
    Task<IIzzyMessage> GetMessageAsync(ulong messageId);
    Task<IIzzyUserMessage> SendFileAsync(FileAttachment fa, string message);
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
    IIzzyGuildUser? GetUser(ulong userId);
    IIzzyRole? GetRole(ulong roleId);
    IIzzySocketGuildChannel? GetChannel(ulong channelId);
    IIzzySocketTextChannel? GetTextChannel(ulong channelId);
}

public interface IIzzyClient
{
    IIzzyUser CurrentUser { get; }
    IReadOnlyCollection<IIzzyGuild> Guilds { get; }

    event Func<IIzzyMessage, Task> MessageReceived;

    event Func<IIzzyMessage, IIzzyMessageChannel, Task> MessageUpdated;

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

    IIzzyContext MakeContext(IIzzyUserMessage message);
}

public interface IIzzyContext
{
    bool IsPrivate { get; }
    IIzzyGuild? Guild { get; }
    IIzzyClient Client { get; }
    IIzzyMessageChannel Channel { get; }
    IIzzyUserMessage Message { get; }
    IIzzyUser User { get; } // note: runtime type must be IIzzyGuildUser whenever possible
}

