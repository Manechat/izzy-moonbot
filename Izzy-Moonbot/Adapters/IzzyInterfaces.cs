using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    int Hierarchy => DisplayName.Contains("Izzy") ? 1 : 0; // not used enough to be worth accurately imitating in tests
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
    int Position => 0; // not used enough to be worth accurately imitating in tests
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
        RequestOptions? options = null,
        ISticker[]? stickers = null
    );
    Task<IIzzyUserMessage> SendFileAsync(FileAttachment fa, string message);

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
        ISticker[]? stickers = null,
        Embed[]? embeds = null
    );
    Task<IIzzyMessage?> GetMessageAsync(ulong messageId);
    Task<IIzzyUserMessage> SendFileAsync(FileAttachment fa, string message);
    IAsyncEnumerable<IReadOnlyCollection<IIzzyMessage>> GetMessagesAsync(ulong firstMessageId, Direction dir, int limit);
    IAsyncEnumerable<IReadOnlyCollection<IIzzyMessage>> GetMessagesAsync(int messageCount);
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
    Task AddBanAsync(ulong userId, int pruneDays, string reason);
    Task<bool> GetIsBannedAsync(ulong userId); // replaces the real GetBanAsync method
    Task RemoveBanAsync(ulong userId, string? reason);
    Task SetBanner(Image image); // replaces the real ModifyAsync(props => ...) method
    IIzzySocketTextChannel? RulesChannel { get; }
}

public interface IIzzyHasId { ulong Id { get; } }
public class IdHaver : IIzzyHasId
{
    public ulong Id { get; }
    public IdHaver(ulong id) => Id = id;
}

public interface IIzzyHasCustomId { string CustomId { get; } }
public class CustomIdHaver : IIzzyHasCustomId
{
    public string CustomId { get; }
    public CustomIdHaver(string id) => CustomId = id;
}

public interface IIzzyClient
{
    IIzzyUser CurrentUser { get; }
    IReadOnlyCollection<IIzzyGuild> Guilds { get; }

    event Func<IIzzyMessage, Task> MessageReceived;

    event Func<string?, IIzzyMessage, IIzzyMessageChannel, Task> MessageUpdated;

    public interface IIzzySocketMessageComponent {
        IIzzyHasId User { get; }
        IIzzyHasId Message { get; }
        IIzzyHasCustomId Data { get; }
        Task UpdateAsync(Action<IIzzyMessageProperties> action);
        Task DeferAsync();
    }
    event Func<IIzzySocketMessageComponent, Task> ButtonExecuted;

    event Func<ulong, IIzzyMessage?, ulong, IIzzyMessageChannel?, Task>? MessageDeleted;

    IIzzyContext MakeContext(IIzzyUserMessage message);
    Task<IIzzyUser?> GetUserAsync(ulong userId);
    IIzzyGuild? GetGuild(ulong v);
    Task SendDirectMessageAsync(ulong userId, string text, MessageComponent? components = null);
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

