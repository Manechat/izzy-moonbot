using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Izzy_Moonbot.Adapters;

namespace Izzy_Moonbot.Helpers;

public class PaginationHelper
{
    private readonly AllowedMentions _allowedMentions;

    private readonly IIzzyClient _client;
    private readonly RequestOptions _options;
    private readonly string[] _staticParts;

    private readonly bool _useCodeBlock;
    private ulong _authorId;
    private bool _easterEgg;
    private IIzzyMessage _message;
    public DateTime ExpiresAt;
    public int PageNumber;
    public string[] Pages;

    public PaginationHelper(SocketCommandContext context, string[] pages, string[] staticParts, int pageNumber = 0,
    bool codeblock = true,
    AllowedMentions allowedMentions = null,
    RequestOptions options = null)
        : this(new SocketCommandContextAdapter(context), pages, staticParts, pageNumber, codeblock, allowedMentions, options)
    { }

    public PaginationHelper(IIzzyContext context, string[] pages, string[] staticParts, int pageNumber = 0,
        bool codeblock = true,
        AllowedMentions allowedMentions = null,
        RequestOptions options = null)
    {
        _client = context.Client;
        _authorId = context.Message.Author.Id;
        Pages = pages;
        PageNumber = pageNumber;
        _staticParts = staticParts;
        _easterEgg = false;
        _useCodeBlock = codeblock;
        _allowedMentions = allowedMentions;
        _options = options;

        ExpiresAt = DateTime.UtcNow + TimeSpan.FromMinutes(5);

        CreatePaginationMessage(context);
    }

    private async void CreatePaginationMessage(IIzzyContext context)
    {
        var builder = new ComponentBuilder()
            .WithButton(customId: "goto-start", emote: Emoji.Parse(":track_previous:"), disabled: false)
            .WithButton(customId: "goto-previous", emote: Emoji.Parse(":arrow_backward:"), disabled: false)
            .WithButton(customId: "trigger-easteregg", emote: Emote.Parse("<:izzylurk:994638513431646298>"),
                disabled: false)
            .WithButton(customId: "goto-next", emote: Emoji.Parse(":arrow_forward:"), disabled: false)
            .WithButton(customId: "goto-end", emote: Emoji.Parse(":track_next:"), disabled: false);


        _message = await context.Channel.SendMessageAsync(
            $"{_staticParts[0]}{Environment.NewLine}{Environment.NewLine}<a:rdloop:910875692785336351> Pagination is loading. Please wait...{Environment.NewLine}{Environment.NewLine}{_staticParts[1]}",
            components: builder.Build(), allowedMentions: _allowedMentions, options: _options);

        //_client.ButtonExecuted += ButtonEvent;
        //_client.MessageDeleted += MessageDeletedEvent;

        RedrawPagination();

        await Task.Run(() =>
        {
            Thread.Sleep(5 * 60 * 1000 + 1); // Sleep for 5 minutes

            if (_message == null) return;
            
            //_client.ButtonExecuted -= ButtonEvent; // Remove the event listener

            RedrawPagination();
        });
    }

    private async void RedrawPagination()
    {
        if (_message == null) return;
        
        var expireMessage = "";
        if (ExpiresAt <= DateTime.UtcNow) expireMessage = "ℹ **This paginated message has expired.**";

        await _message.ModifyAsync(msg =>
        {
            var codeBlock = "";
            if (_useCodeBlock) codeBlock = "```";

            //msg.Content =
            //    $"{_staticParts[0]}{Environment.NewLine}{codeBlock}{Environment.NewLine}{Pages[PageNumber]}{Environment.NewLine}{codeBlock}`Page {PageNumber + 1} out of {Pages.Length}`{Environment.NewLine}{_staticParts[1]}{Environment.NewLine}{Environment.NewLine}{expireMessage}";

            if (_easterEgg)
            {
                var builder = new ComponentBuilder()
                    .WithButton(customId: "goto-start", emote: Emoji.Parse(":track_previous:"))
                    .WithButton(customId: "goto-previous", emote: Emoji.Parse(":arrow_backward:"))
                    .WithButton(customId: "trigger-easteregg-active",
                        emote: Emote.Parse("<:izzyohyou:967943490698887258>"), style: ButtonStyle.Success)
                    .WithButton(customId: "goto-next", emote: Emoji.Parse(":arrow_forward:"))
                    .WithButton(customId: "goto-end", emote: Emoji.Parse(":track_next:"));
                //msg.Components = builder.Build();
            }

            if (ExpiresAt <= DateTime.UtcNow)
            {
                if (_easterEgg)
                {
                    var builder = new ComponentBuilder()
                        .WithButton(customId: "goto-start", emote: Emoji.Parse(":track_previous:"), disabled: true)
                        .WithButton(customId: "goto-previous", emote: Emoji.Parse(":arrow_backward:"), disabled: true)
                        .WithButton(customId: "trigger-easteregg-active",
                            emote: Emote.Parse("<:izzyohyou:967943490698887258>"), style: ButtonStyle.Success,
                            disabled: true)
                        .WithButton(customId: "goto-next", emote: Emoji.Parse(":arrow_forward:"), disabled: true)
                        .WithButton(customId: "goto-end", emote: Emoji.Parse(":track_next:"), disabled: true);
                    //msg.Components = builder.Build();
                }
                else
                {
                    var builder = new ComponentBuilder()
                        .WithButton(customId: "goto-start", emote: Emoji.Parse(":track_previous:"), disabled: true)
                        .WithButton(customId: "goto-previous", emote: Emoji.Parse(":arrow_backward:"), disabled: true)
                        .WithButton(customId: "trigger-easteregg", emote: Emote.Parse("<:izzylurk:994638513431646298>"),
                            disabled: true)
                        .WithButton(customId: "goto-next", emote: Emoji.Parse(":arrow_forward:"), disabled: true)
                        .WithButton(customId: "goto-end", emote: Emoji.Parse(":track_next:"), disabled: true);
                    //msg.Components = builder.Build();
                }
            }
        });
    }

    private async Task ButtonEvent(SocketMessageComponent component)
    {
        if (component.User.Id != _authorId) return;
        if (component.Message.Id != _message.Id) return;

        switch (component.Data.CustomId)
        {
            case "goto-start":
                PageNumber = 0;
                RedrawPagination();
                break;
            case "goto-previous":
                if (PageNumber >= 1) PageNumber -= 1;
                RedrawPagination();
                break;
            case "goto-next":
                if (PageNumber < Pages.Length - 1) PageNumber += 1;
                RedrawPagination();
                break;
            case "goto-end":
                PageNumber = Pages.Length - 1;
                RedrawPagination();
                break;
            case "trigger-easteregg":
                _easterEgg = true;
                RedrawPagination();
                break;
        }

        await component.DeferAsync();
    }

    private async Task MessageDeletedEvent(Cacheable<IMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel)
    {
        if (_message.Id == message.Id)
        {
            //_client.ButtonExecuted -= ButtonEvent;
            //_client.MessageDeleted -= MessageDeletedEvent;

            _message = null;
        }
    }
}