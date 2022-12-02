using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;

namespace Izzy_Moonbot.Service;

public class FilterService
{
    private readonly ModService _mod;
    private readonly ModLoggingService _modLog;
    private readonly Config _config;
    private readonly LoggingService _logger;

    /*
     * The testString is a specific string that, while not in the actual filter list
     * is treated as if it is. Anyone who says it will be treated the same as if they
     * actually tripped the filter. This is used for testing the filter.
     * Only gets defined and used if compiled as Debug.
    */
#if DEBUG
    private readonly string[] _testString =
    {
        "=+i8F8s+#(-{×nsBIo8~lA:IZZY_FILTER_TEST:G8282!#",
        "#!"
    };
#endif

    public FilterService(Config config, ModService mod, ModLoggingService modLog, LoggingService logger)
    {
        _config = config;
        _mod = mod;
        _modLog = modLog;
        _logger = logger;
    }
    
    public void RegisterEvents(DiscordSocketClient client)
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        client.MessageReceived += async (message) => ProcessMessage(message, client);
        client.MessageUpdated += async (oldMessage, newMessage, channel) => ProcessMessageUpdate(oldMessage, newMessage, channel, client);
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    private async Task LogFilterTrip(SocketCommandContext context, string word, string category,
        List<string> actionsTaken, bool onEdit = false, RestUserMessage message = null)
    {
        var embedBuilder = new EmbedBuilder()
            .WithTitle(":warning: Filter violation detected" + (onEdit ? " on message edit" : ""))
            .WithColor(16732240)
            .AddField("User", $"<@{context.User.Id}> (`{context.User.Id}`)", true)
            .AddField("Category", category, true)
            .AddField("Channel", $"<#{context.Channel.Id}>", true)
            .AddField("Trigger Word", $"{word}")
            .AddField("Filtered Message", $"{context.Message.CleanContent}")
            .WithTimestamp(onEdit ? (DateTimeOffset)context.Message.EditedTimestamp : context.Message.Timestamp);

        var actions = new List<string>();
        actions.Add(":x: - **I've deleted the offending message.**");

        if (actionsTaken.Contains("message"))
            actions.Add(
                $":speech_balloon: - **I've sent a message in response.**");
        if (actionsTaken.Contains("silence"))
        {
            actions.Add(":mute: - **I've silenced the user.**");
            actions.Add(":exclamation: - **I've pinged all moderators.**");
        }

        var roleIds = context.Guild.GetUser(context.User.Id).Roles.Select(role => role.Id).ToList();
        if (_config.FilterBypassRoles.Overlaps(roleIds))
        {
            actions.Clear();
            actions.Add(
                ":information_source: - **I've done nothing as this user has a role which is in `FilterBypassRoles`.**");
        }
        else if (DiscordHelper.IsDev(context.User.Id) && _config.FilterDevBypass)
        {
            actions.Clear();
            actions.Add(
                ":information_source: - **I've done nothing as this is one of my developers and `FilterDevBypass` is true.**");
        }

        embedBuilder.AddField("What have I done in response?", string.Join(Environment.NewLine, actions));

        var fileLogResponse = "Delete";
        if (actionsTaken.Contains("message")) fileLogResponse += ", Send Message";
        if (actionsTaken.Contains("silence")) fileLogResponse += ", Silence user";

        if (_config.FilterBypassRoles.Overlaps(roleIds) ||
            (DiscordHelper.IsDev(context.User.Id) && _config.FilterDevBypass)) fileLogResponse = "Nothing";

        await _modLog.CreateModLog(context.Guild)
            .SetContent($"{(actionsTaken.Contains("silence") ? $"<@&{_config.ModRole}>" : "")} Filter Violation for <@{context.User.Id}>")
            .SetEmbed(embedBuilder.Build())
            .SetFileLogContent($"Filter violation by {context.User.Username}#{context.User.Discriminator} ({context.Guild.GetUser(context.User.Id).DisplayName}) (`{context.User.Id}`) in #{context.Channel.Name} (`{context.Channel.Id}`){Environment.NewLine}" +
                               $"Category: {category}{Environment.NewLine}" +
                               $"Trigger: {context.Message.CleanContent.Replace(word, $"[[{word}]]")}{Environment.NewLine}" +
                               $"Response: {fileLogResponse}")
            .Send();
    }

    private async Task ProcessFilterTrip(SocketCommandContext context, string word, string category,
        bool onEdit = false)
    {
        var roleIds = context.Guild.GetUser(context.User.Id).Roles.Select(role => role.Id).ToList();

        if (!_config.FilterBypassRoles.Overlaps(roleIds) &&
            !(DiscordHelper.IsDev(context.User.Id) && _config.FilterDevBypass))
            await context.Message.DeleteAsync();
        
        try
        {
            if (!_config.FilterResponseMessages.ContainsKey(category))
                _config.FilterResponseMessages[category] = null;

            var messageResponse = _config.FilterResponseMessages[category];
            var shouldSilence = _config.FilterResponseSilence.Contains(category);
            
            if (_config.FilterBypassRoles.Overlaps(roleIds) || 
                (DiscordHelper.IsDev(context.User.Id) && _config.FilterDevBypass))
            {
                messageResponse = null;
                shouldSilence = false;
            }

            var actions = new List<string>();
            RestUserMessage message = null;

            if (messageResponse != null)
            {
                message = await context.Channel.SendMessageAsync($"<@{context.User.Id}> {messageResponse}");
                actions.Add("message");
            }

            if (shouldSilence)
            {
                _mod.SilenceUser(context.Guild.GetUser(context.User.Id), $"Filter violation ({category} category)");
                actions.Add("silence");
            }

            await LogFilterTrip(context, word, category, actions, onEdit);
        }
        catch (KeyNotFoundException ex)
        {
            var actions = new List<string>();
            await LogFilterTrip(context, word, category, actions, onEdit);
            await context.Guild.GetTextChannel(_config.ModChannel).SendMessageAsync(
                ":warning: **I encountered a `KeyNotFoundException` while processing the above filter violation.**");
        }
    }

    public async Task ProcessMessageUpdate(Cacheable<IMessage, ulong> oldMessage, SocketMessage newMessage,
        ISocketMessageChannel channel, DiscordSocketClient client)
    {
        if (newMessage.Author.Id == client.CurrentUser.Id) return; // Don't process self.
        
        if (!_config.FilterEnabled) return;
        if (newMessage.Author.IsBot) return; // Don't listen to bots
        if (!DiscordHelper.IsInGuild(newMessage)) return;
        if (!DiscordHelper.IsProcessableMessage(newMessage)) return; // Not processable
        if (newMessage is not SocketUserMessage message) return; // Not processable
        SocketCommandContext context = new SocketCommandContext(client, message);
        
        if (!DiscordHelper.IsDefaultGuild(context)) return;
        
        if (_config.FilterIgnoredChannels.Contains(context.Channel.Id)) return;
        foreach (var (category, words) in _config.FilteredWords)
        {
            var filteredWords = words.ToArray().ToList();
            var trip = false;
#if DEBUG
            filteredWords.Add(_testString[0] + category + _testString[1]);
#endif


            foreach (var word in filteredWords)
            {
                if (trip) continue;
                if (context.Message.Content.Contains(word))
                {
                    // Filter Trip!
                    await ProcessFilterTrip(context, word, category);
                    trip = true;
                }
            }
        }
    }

    public async Task ProcessMessage(SocketMessage messageParam, DiscordSocketClient client)
    {
        if (messageParam.Author.Id == client.CurrentUser.Id) return; // Don't process self.
        
        if (!_config.FilterEnabled) return;
        if (messageParam.Author.IsBot) return; // Don't listen to bots
        if (!DiscordHelper.IsInGuild(messageParam)) return;
        if (!DiscordHelper.IsProcessableMessage(messageParam)) return; // Not processable
        if (messageParam is not SocketUserMessage message) return; // Not processable
        SocketCommandContext context = new SocketCommandContext(client, message);
        
        if (!DiscordHelper.IsDefaultGuild(context)) return;
        
        if (_config.FilterIgnoredChannels.Contains(context.Channel.Id)) return;
        foreach (var (category, words) in _config.FilteredWords)
        {
            var filteredWords = words.ToArray().ToList();
            var trip = false;
#if DEBUG
            filteredWords.Add(_testString[0] + category + _testString[1]);
#endif


            foreach (var word in filteredWords)
            {
                if (trip) continue;
                if (context.Message.Content.Contains(word))
                {
                    // Filter Trip!
                    await ProcessFilterTrip(context, word, category);
                    trip = true;
                }
            }
        }
    }
}