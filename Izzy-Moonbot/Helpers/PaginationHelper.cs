using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Izzy_Moonbot.Adapters;
using static Izzy_Moonbot.Adapters.IIzzyClient;

namespace Izzy_Moonbot.Helpers;

public class PaginationHelper
{
    private readonly AllowedMentions? _allowedMentions;

    private readonly IIzzyClient _client;
    private readonly string[] _staticParts;

    private readonly bool _useCodeBlock;
    private ulong _authorId;
    private bool _easterEgg;
    private IIzzyUserMessage? _message;
    public DateTime ExpiresAt;
    private int _pageNumber = 0;
    public string[] Pages;

    public PaginationHelper(SocketCommandContext context, string[] pages, string[] staticParts,
        bool codeblock = true,
        AllowedMentions? allowedMentions = null)
        : this(new SocketCommandContextAdapter(context), pages, staticParts, codeblock, allowedMentions)
    { }

    public PaginationHelper(IIzzyContext context, string[] pages, string[] staticParts,
        bool codeblock = true,
        AllowedMentions? allowedMentions = null)
    {
        _client = context.Client;
        _authorId = context.Message.Author.Id;
        Pages = pages;
        _staticParts = staticParts;
        _easterEgg = false;
        _useCodeBlock = codeblock;
        _allowedMentions = allowedMentions;

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
            components: builder.Build(), allowedMentions: _allowedMentions);

        _client.ButtonExecuted += ButtonEvent;
        _client.MessageDeleted += MessageDeletedEvent;

        RedrawPagination();

        await Task.Run(() =>
        {
            Thread.Sleep(5 * 60 * 1000 + 1); // Sleep for 5 minutes

            if (_message == null) return;
            
            _client.ButtonExecuted -= ButtonEvent; // Remove the event listener

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

            msg.Content =
                $"{_staticParts[0]}{Environment.NewLine}{codeBlock}{Environment.NewLine}{Pages[_pageNumber]}{Environment.NewLine}{codeBlock}`Page {_pageNumber + 1} out of {Pages.Length}`{Environment.NewLine}{_staticParts[1]}{Environment.NewLine}{Environment.NewLine}{expireMessage}";

            if (_easterEgg)
            {
                var builder = new ComponentBuilder()
                    .WithButton(customId: "goto-start", emote: Emoji.Parse(":track_previous:"))
                    .WithButton(customId: "goto-previous", emote: Emoji.Parse(":arrow_backward:"))
                    .WithButton(customId: "trigger-easteregg-active",
                        emote: Emote.Parse("<:izzyohyou:967943490698887258>"), style: ButtonStyle.Success)
                    .WithButton(customId: "goto-next", emote: Emoji.Parse(":arrow_forward:"))
                    .WithButton(customId: "goto-end", emote: Emoji.Parse(":track_next:"));
                msg.Components = builder.Build();
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
                    msg.Components = builder.Build();
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
                    msg.Components = builder.Build();
                }
            }
        });
    }

    private async Task ButtonEvent(IIzzySocketMessageComponent component)
    {
        if (component.User.Id != _authorId) return;
        if (component.Message.Id != _message?.Id) return;

        switch (component.Data.CustomId)
        {
            case "goto-start":
                _pageNumber = 0;
                RedrawPagination();
                break;
            case "goto-previous":
                if (_pageNumber >= 1) _pageNumber -= 1;
                RedrawPagination();
                break;
            case "goto-next":
                if (_pageNumber < Pages.Length - 1) _pageNumber += 1;
                RedrawPagination();
                break;
            case "goto-end":
                _pageNumber = Pages.Length - 1;
                RedrawPagination();
                break;
            case "trigger-easteregg":
                _easterEgg = true;
                RedrawPagination();
                break;
        }

        await component.DeferAsync();
    }

    private async Task MessageDeletedEvent(IIzzyHasId message, IIzzyHasId channel)
    {
        if (_message?.Id == message.Id)
        {
            _client.ButtonExecuted -= ButtonEvent;
            _client.MessageDeleted -= MessageDeletedEvent;

            _message = null;
        }
    }
}