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
        client.MessageReceived += (message) => Task.Run(async () => { await ProcessMessage(message, client); });
        client.MessageUpdated += (oldMessage, newMessage, channel) => Task.Run(async () => { await ProcessMessageUpdate(oldMessage, newMessage, channel, client); });
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
            .AddField("Filtered message (trigger word in bold)",
                $"{context.Message.CleanContent.Replace(word, $"**{word}**")}")
            .WithTimestamp(onEdit ? (DateTimeOffset)context.Message.EditedTimestamp : context.Message.Timestamp);

        var actions = new List<string>();
        actions.Add(":x: - **I've deleted the offending message.**");

        if (actionsTaken.Contains("message"))
            actions.Add(
                $":speech_balloon: - **I've sent a message in response.**");
        if (actionsTaken.Contains("silence")) actions.Add(":mute: - **I've silenced the user.**");

        var roleIds = context.Guild.GetUser(context.User.Id).Roles.Select(role => role.Id).ToList();
        if (_config.FilterBypassRoles.Overlaps(roleIds))
        {
            actions.Clear();
            actions.Add(
                ":information_source: - **I've done nothing as this user has a role which is in `FilterBypassRoles`.**");
        }

        if (_config.SafeMode)
            embedBuilder.AddField("How do I want to respond? (`SafeMode` is enabled)",
                string.Join(Environment.NewLine, actions));
        else
            embedBuilder.AddField("What have I done in response?", string.Join(Environment.NewLine, actions));

        await _modLog.CreateModLog(context.Guild)
            .SetContent($"<@&{_config.ModRole}> Filter Violation for <@{context.User.Id}>")
            .SetEmbed(embedBuilder.Build())
            .Send();
    }

    private async Task ProcessFilterTrip(SocketCommandContext context, string word, string category,
        bool onEdit = false)
    {
        try
        {
            await context.Message.DeleteAsync();
            if (!_config.FilterResponseMessages.ContainsKey(category))
                _config.FilterResponseMessages[category] = null;
            if (!_config.FilterResponseSilence.ContainsKey(category))
                _config.FilterResponseSilence[category] = false;

            var messageResponse = _config.FilterResponseMessages[category];
            var shouldSilence = _config.FilterResponseSilence[category];

            var roleIds = context.Guild.GetUser(context.User.Id).Roles.Select(role => role.Id).ToList();
            if (_config.FilterBypassRoles.Overlaps(roleIds))
            {
                messageResponse = null;
                shouldSilence = false;
            }

            var actions = new List<string>();
            RestUserMessage message = null;

            if (messageResponse != null)
            {
                if (_config.SafeMode)
                    message = await context.Guild.GetTextChannel(_config.LogChannel).SendMessageAsync(
                        $"<@{context.User.Id}> {messageResponse}{Environment.NewLine}{Environment.NewLine}*I am posting this message here as safe mode is enabled.*");
                else message = await context.Channel.SendMessageAsync($"<@{context.User.Id}> {messageResponse}");
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
            //await context.Message.DeleteAsync(); // Just in case
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
        
        if (!_config.FilterEnabled || !_config.FilterMonitorEdits) return;
        if (!DiscordHelper.IsInGuild(newMessage)) return;
        if (!DiscordHelper.IsProcessableMessage(newMessage)) return; // Not processable
        if (newMessage is not SocketUserMessage message) return; // Not processable
        SocketCommandContext context = new SocketCommandContext(client, message);
        
        if (_config.ThreadOnlyMode &&
            (message.Channel.GetChannelType() != ChannelType.PublicThread &&
             message.Channel.GetChannelType() != ChannelType.PrivateThread)) return; // Not a thread, in thread only mode

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
        if (!_config.FilterEnabled) return;
        if (!DiscordHelper.IsInGuild(messageParam)) return;
        if (!DiscordHelper.IsProcessableMessage(messageParam)) return; // Not processable
        if (messageParam is not SocketUserMessage message) return; // Not processable
        SocketCommandContext context = new SocketCommandContext(client, message);
        
        if (_config.ThreadOnlyMode &&
            (message.Channel.GetChannelType() != ChannelType.PublicThread &&
             message.Channel.GetChannelType() != ChannelType.PrivateThread)) return; // Not a thread, in thread only mode

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