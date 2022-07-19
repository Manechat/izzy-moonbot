using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Izzy_Moonbot.Helpers;

public class PaginationHelper
{
    public string[] Pages;
    public int PageNumber;
    public DateTime ExpiresAt;
    
    private DiscordSocketClient _client;
    private ulong _authorId;
    private IUserMessage _message;
    private string[] _staticParts;
    private Task _cancelTask;
    private bool _easterEgg;

    private bool _useCodeBlock;
    private AllowedMentions _allowedMentions;
    private RequestOptions _options;
    
    public PaginationHelper(SocketCommandContext context, string[] pages, string[] staticParts, int pageNumber = 0, bool codeblock = true, 
        AllowedMentions allowedMentions = null, 
        RequestOptions options = null)
    {
        this._client = context.Client;
        this._authorId = context.Message.Author.Id;
        this.Pages = pages;
        this.PageNumber = pageNumber;
        this._staticParts = staticParts;
        this._easterEgg = false;
        this._useCodeBlock = codeblock;
        this._allowedMentions = allowedMentions;
        this._options = options;

        this.ExpiresAt = DateTime.UtcNow + TimeSpan.FromMinutes(5);
        
        this.CreatePaginationMessage(context);
    }

    private async void CreatePaginationMessage(SocketCommandContext context)
    {
        var builder = new ComponentBuilder()
            .WithButton(customId: "goto-start", emote: Emoji.Parse(":track_previous:"), disabled: false)
            .WithButton(customId: "goto-previous", emote: Emoji.Parse(":arrow_backward:"), disabled: false)
            .WithButton(customId: "trigger-easteregg", emote: Emote.Parse("<:izzylurk:994638513431646298>"), disabled: false)
            .WithButton(customId: "goto-next", emote: Emoji.Parse(":arrow_forward:"), disabled: false)
            .WithButton(customId: "goto-end", emote: Emoji.Parse(":track_next:"), disabled: false);
            
        
        this._message = await context.Message.ReplyAsync($"{this._staticParts[0]}{Environment.NewLine}{Environment.NewLine}<a:loading:967008483956387840> Pagination is loading. Please wait...{Environment.NewLine}{Environment.NewLine}{this._staticParts[1]}", components: builder.Build(), allowedMentions: this._allowedMentions, options: this._options);

        this._client.ButtonExecuted += ButtonEvent;
        
        this.RedrawPagination();
        
        await Task.Factory.StartNew(() =>
        {
            Thread.Sleep((5*60*1000)+1); // Sleep for 5 minutes
            
            this._client.ButtonExecuted -= ButtonEvent; // Remove the event listener

            this.RedrawPagination();
        });
    }

    private async void RedrawPagination()
    {
        string expireMessage = "";
        if (this.ExpiresAt <= DateTime.UtcNow) expireMessage = "<:info:964284521488986133> **This paginated message has expired.**";

        await this._message.ModifyAsync(msg =>
        {
            string codeBlock = "";
            if (this._useCodeBlock) codeBlock = $"```";
            
            msg.Content =
                $"{this._staticParts[0]}{Environment.NewLine}{codeBlock}{Environment.NewLine}{this.Pages[this.PageNumber]}{Environment.NewLine}{codeBlock}`Page {this.PageNumber+1} out of {this.Pages.Length}`{Environment.NewLine}{this._staticParts[1]}{Environment.NewLine}{Environment.NewLine}{expireMessage}";

            if (this._easterEgg)
            {
                var builder = new ComponentBuilder()
                    .WithButton(customId: "goto-start", emote: Emoji.Parse(":track_previous:"))
                    .WithButton(customId: "goto-previous", emote: Emoji.Parse(":arrow_backward:"))
                    .WithButton(customId: "trigger-easteregg-active", emote: Emote.Parse("<:izzyohyou:967943490698887258>"), style: ButtonStyle.Success)
                    .WithButton(customId: "goto-next", emote: Emoji.Parse(":arrow_forward:"))
                    .WithButton(customId: "goto-end", emote: Emoji.Parse(":track_next:"));
                msg.Components = builder.Build();
            }

            if (this.ExpiresAt <= DateTime.UtcNow)
            {
                if (this._easterEgg)
                {
                    var builder = new ComponentBuilder()
                        .WithButton(customId: "goto-start", emote: Emoji.Parse(":track_previous:"), disabled: true)
                        .WithButton(customId: "goto-previous", emote: Emoji.Parse(":arrow_backward:"), disabled: true)
                        .WithButton(customId: "trigger-easteregg-active", emote: Emote.Parse("<:izzyohyou:967943490698887258>"), style: ButtonStyle.Success, disabled: true)
                        .WithButton(customId: "goto-next", emote: Emoji.Parse(":arrow_forward:"), disabled: true)
                        .WithButton(customId: "goto-end", emote: Emoji.Parse(":track_next:"), disabled: true);
                    msg.Components = builder.Build();
                }
                else
                {
                    var builder = new ComponentBuilder()
                        .WithButton(customId: "goto-start", emote: Emoji.Parse(":track_previous:"), disabled: true)
                        .WithButton(customId: "goto-previous", emote: Emoji.Parse(":arrow_backward:"), disabled: true)
                        .WithButton(customId: "trigger-easteregg", emote: Emote.Parse("<:izzylurk:994638513431646298>"), disabled: true)
                        .WithButton(customId: "goto-next", emote: Emoji.Parse(":arrow_forward:"), disabled: true)
                        .WithButton(customId: "goto-end", emote: Emoji.Parse(":track_next:"), disabled: true);
                    msg.Components = builder.Build();
                }
            }
        });
    }

    private async Task ButtonEvent(SocketMessageComponent component)
    {
        try
        {
            if (component.User.Id == this._client.CurrentUser.Id) return;
            if (component.Message.Id != this._message.Id) return;

            switch (component.Data.CustomId)
            {
                case "goto-start":
                    this.PageNumber = 0;
                    this.RedrawPagination();
                    break;
                case "goto-previous":
                    if (this.PageNumber >= 1) this.PageNumber -= 1;
                    this.RedrawPagination();
                    break;
                case "goto-next":
                    if (this.PageNumber < this.Pages.Length-1) this.PageNumber += 1;
                    this.RedrawPagination();
                    break;
                case "goto-end":
                    this.PageNumber = this.Pages.Length-1;
                    this.RedrawPagination();
                    break;
                case "trigger-easteregg":
                    this._easterEgg = true;
                    this.RedrawPagination();
                    break;
                default:
                    // We don't actually know what this emote is. Don't do anything
                    break;
            }

            await component.DeferAsync();
        }
        catch (Exception exp)
        {
            component.Channel.SendMessageAsync($"{exp.Message}{Environment.NewLine}{exp.StackTrace}");
        }
    }
}