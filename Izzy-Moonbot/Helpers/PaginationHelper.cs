using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
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
    private bool _easterEggChanged;
    private string _easterEggEmoji;
    private IIzzyUserMessage? _message;
    public DateTime ExpiresAt;
    private int _pageNumber = 0;
    public string[] Pages;

    public static void PaginateIfNeededAndSendMessage(IIzzyContext context, string header, IList<string> lineItems, string footer, bool codeblock = true, uint pageSize = 10, AllowedMentions? allowedMentions = null)
    {
        if (lineItems.Count <= pageSize)
        {
            context.Channel.SendMessageAsync(
                $"{header}\n" +
                (codeblock ? "```\n" : "") +
                $"{string.Join('\n', lineItems)}" +
                (codeblock ? "\n```" : "") +
                $"\n{footer}",
                allowedMentions: allowedMentions);
            return;
        }

        var pages = new List<string>();
        var pageNumber = -1;
        for (var i = 0; i < lineItems.Count; i++)
        {
            if (i % pageSize == 0)
            {
                pageNumber += 1;
                pages.Add("");
            }

            if (pages[pageNumber] != "")
                pages[pageNumber] += '\n';
            pages[pageNumber] += lineItems[i];
        }

        new PaginationHelper(context, pages.ToArray(), new string[] { header, footer }, codeblock: codeblock, allowedMentions: allowedMentions);
    }

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
        _easterEggChanged = false;
        _easterEggEmoji = GetRandomEmoji();
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
            .WithButton(customId: "trigger-easteregg", emote: Emote.Parse(_easterEggEmoji), disabled: false)
            .WithButton(customId: "goto-next", emote: Emoji.Parse(":arrow_forward:"), disabled: false)
            .WithButton(customId: "goto-end", emote: Emoji.Parse(":track_next:"), disabled: false);


        _message = await context.Channel.SendMessageAsync(
            $"{_staticParts[0]}\n\n<a:rdloop:910875692785336351> Pagination is loading. Please wait...\n\n{_staticParts[1]}",
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

        try
        {
            await _message.ModifyAsync(msg =>
            {
                var codeBlock = "";
                if (_useCodeBlock) codeBlock = "```";

                var truncationWarning = "";
                var paginationBoilerplate = 200; // intentionally high so we have space for future changes

                var header = _staticParts[0];
                var page = Pages[_pageNumber];
                var footer = _staticParts[1];
                if (header.Length + footer.Length + page.Length + paginationBoilerplate > DiscordHelper.MessageLengthLimit)
                {
                    truncationWarning = "⚠️ Some items needed to be truncated";
                    var items = page.Split('\n');
                    var newlinesInPage = items.Count();
                    var spaceForPage = DiscordHelper.MessageLengthLimit - header.Length - footer.Length - paginationBoilerplate - newlinesInPage;

                    var maxItemLength = spaceForPage / items.Count();
                    var truncationMarker = "[...]";
                    var truncatedItems = items.Select(i =>
                        i.Length <= maxItemLength ? i :
                        i.Substring(0, maxItemLength - truncationMarker.Length) + truncationMarker);

                    page = string.Join('\n', truncatedItems);
                }

                msg.Content =
                    $"{header}\n{truncationWarning}{codeBlock}\n{page}\n{codeBlock}`Page {_pageNumber + 1} out of {Pages.Length}`\n{footer}\n\n{expireMessage}";

                var disableButtons = ExpiresAt <= DateTime.UtcNow;

                var easterEggButtonId = _easterEggChanged ? "trigger-easteregg-active" : "trigger-easteregg";
                var easterEggButtonStyle = _easterEggChanged ? ButtonStyle.Success : ButtonStyle.Primary;

                var builder = new ComponentBuilder()
                    .WithButton(customId: "goto-start", emote: Emoji.Parse(":track_previous:"), disabled: disableButtons)
                    .WithButton(customId: "goto-previous", emote: Emoji.Parse(":arrow_backward:"), disabled: disableButtons)
                    .WithButton(customId: easterEggButtonId, emote: Emote.Parse(_easterEggEmoji), style: easterEggButtonStyle, disabled: disableButtons)
                    .WithButton(customId: "goto-next", emote: Emoji.Parse(":arrow_forward:"), disabled: disableButtons)
                    .WithButton(customId: "goto-end", emote: Emoji.Parse(":track_next:"), disabled: disableButtons);
                msg.Components = builder.Build();
            });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            await _message.Channel.SendMessageAsync("Paginated message update failed with an ArgumentOutOfRangeException. " +
                "This likely means the page's content was too long for a Discord message. " +
                $"\nex.Message: {ex.Message}");
        }
    }

    private async Task ButtonEvent(IIzzySocketMessageComponent component)
    {
        // Be sure to early return without DeferAsync()ing if this is someone else's button
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
                _easterEggChanged = true;
                _easterEggEmoji = GetRandomEmoji();
                RedrawPagination();
                break;
        }

        await component.DeferAsync();
    }

    private async Task MessageDeletedEvent(ulong messageId, IIzzyMessage? _message, ulong _channelId, IIzzyMessageChannel? _channel)
    {
        if (_message?.Id == messageId)
        {
            _client.ButtonExecuted -= ButtonEvent;
            _client.MessageDeleted -= MessageDeletedEvent;

            _message = null;
        }
    }

    private string GetRandomEmoji()
    {
        var index = new Random().Next(EasterEggEmojis.Count());
        return EasterEggEmojis[index];
    }

    private readonly static string[] EasterEggEmojis = new string[]
    {
        "<:izzynothoughtsheadempty:910198222255972382>",
        "<a:izzywhat:891381404741550130>",
        "<:izzystare:884921135312015460>",
        "<:izzyooh:889126310260113449>",
        "<:izzyangy:938893998393815143>",
        "<:izzybeans:897571951726436444>",
        "<:izzybedsheet:814540631555833907>",
        "<a:izzyblep:819291324872130560>",
        "<a:izzybongocat:1018915049902968962>",
        "<:izzyburn:859900496109502484>",
        "<:izzycat:819294858967384084>",
        "<:izzydeletethis:1028964499723661372>",
        "<:izzycoolglasses:819294859865227264>",
        "<:izzyderp:891008714222501890>",
        "<a:izzyearflop:892858128641687572>",
        "<:izzyfloof:884918388185497652>",
        "<a:izzygooglyeyes:965230685382144000>",
        "<:izzygun:908419314250571886>",
        "<:izzyhappy:819294868424753153>",
        "<a:izzyimmagetya:1068184442457301202>",
        "<:izzylmao:819294865670144000>",
        "<:izzylurk:909475647427067904>",
        "<:izzymediumsneaky:897569795606732830>",
        "<:izzynoticesyoursparkleowo:989521523876446258>",
        "<:izzysilly:814551113109209138>",
        "<a:izzymwahaha:1045311925959004211>",
        "<:izzypartyhat:814551115752013886>",
        "<:izzyohyou:859937581081034782>",
        "<:izzymaximumsneaky:892869437970059314>",
        "<a:izzynom:1007272501451178096>",
        "<:izzyscrunch:907312716979511317>",
        "<:izzysneaksy:1044262492932673607>",
    };
}
