using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;

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
    */
    private readonly string[] _testString =
    {
        "=+i8F8s+#(-{×nsBIo8~lA:IZZY_FILTER_TEST:G8282!#",
        "#!"
    };

    public FilterService(Config config, ModService mod, ModLoggingService modLog, LoggingService logger)
    {
        _config = config;
        _mod = mod;
        _modLog = modLog;
        _logger = logger;
    }
    
    public void RegisterEvents(IIzzyClient client)
    {
        client.MessageReceived += async (message) => await DiscordHelper.LeakOrAwaitTask(ProcessMessage(message, client));
        client.MessageUpdated += async (oldContent, newMessage, channel) => await DiscordHelper.LeakOrAwaitTask(ProcessMessageUpdate(oldContent, newMessage, channel, client));
    }

    private async Task LogFilterTrip(IIzzyContext context, string word, string category,
        List<string> actionsTaken, bool onEdit)
    {
        var embedBuilder = new EmbedBuilder()
            .WithTitle(":warning: Filter violation detected" + (onEdit ? " on message edit" : ""))
            .WithColor(16732240)
            .AddField("User", $"<@{context.User.Id}> (`{context.User.Id}`)", true)
            .AddField("Category", category, true)
            .AddField("Channel", $"<#{context.Channel.Id}>", true)
            .AddField("Trigger Word", $"{word}")
            .AddField("Filtered Message", $"{context.Message.CleanContent}");

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

        var roleIds = context.Guild?.GetUser(context.User.Id)?.Roles.Select(role => role.Id).ToList() ?? new List<ulong>();
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

        embedBuilder.AddField("What have I done in response?", string.Join('\n', actions));

        var fileLogResponse = "Delete";
        if (actionsTaken.Contains("message")) fileLogResponse += ", Send Message";
        if (actionsTaken.Contains("silence")) fileLogResponse += ", Silence user";

        if (_config.FilterBypassRoles.Overlaps(roleIds) ||
            (DiscordHelper.IsDev(context.User.Id) && _config.FilterDevBypass)) fileLogResponse = "Nothing";

        if (context.Guild == null)
            throw new InvalidOperationException("LogFilterTrip was somehow called with a non-guild context");

        await _modLog.CreateModLog(context.Guild)
            .SetContent($"{(actionsTaken.Contains("silence") ? $"<@&{_config.ModRole}>" : "")} Filter Violation for <@{context.User.Id}>")
            .SetEmbed(embedBuilder.Build())
            .SetFileLogContent($"Filter violation by {context.User.Username}#{context.User.Discriminator} ({context.Guild.GetUser(context.User.Id)?.DisplayName}) (`{context.User.Id}`) in #{context.Channel.Name} (`{context.Channel.Id}`)\n" +
                               $"Category: {category}\n" +
                               $"Trigger: {context.Message.CleanContent.Replace(word, $"[[{word}]]")}\n" +
                               $"Response: {fileLogResponse}")
            .Send();
    }

    private async Task ProcessFilterTrip(IIzzyContext context, string word, string category, bool onEdit)
    {
        var roleIds = context.Guild?.GetUser(context.User.Id)?.Roles.Select(role => role.Id).ToList() ?? new List<ulong>();

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

            if (messageResponse != null)
            {
                await context.Channel.SendMessageAsync($"<@{context.User.Id}> {messageResponse}");
                actions.Add("message");
            }

            if (shouldSilence && context.Guild?.GetUser(context.User.Id) is IIzzyGuildUser user)
            {
                await _mod.SilenceUser(user, $"Filter violation ({category} category)");
                actions.Add("silence");
            }

            await LogFilterTrip(context, word, category, actions, onEdit);
        }
        catch (KeyNotFoundException)
        {
            var actions = new List<string>();
            await LogFilterTrip(context, word, category, actions, onEdit);
            if (context.Guild?.GetTextChannel(_config.ModChannel) is IIzzySocketTextChannel modChannel)
                await modChannel.SendMessageAsync(":warning: **I encountered a `KeyNotFoundException` while processing the above filter violation.**");
        }
    }

    public async Task ProcessMessageUpdate(
        string? oldContent, IIzzyMessage newMessage,
        IIzzyMessageChannel channel, IIzzyClient client)
    {
        if (newMessage.Content == oldContent) return; // Ignore non-content edits

        if (newMessage.Author.Id == client.CurrentUser.Id) return; // Don't process self.
        
        if (!_config.FilterEnabled) return;
        if (newMessage.Author.IsBot) return; // Don't listen to bots
        if (!DiscordHelper.IsInGuild(newMessage)) return;
        if (!DiscordHelper.IsProcessableMessage(newMessage)) return; // Not processable
        if (newMessage is not IIzzyUserMessage message) return; // Not processable
        IIzzyContext context = client.MakeContext(message);
        
        if (!DiscordHelper.IsDefaultGuild(context)) return;
        
        if (_config.FilterIgnoredChannels.Contains(context.Channel.Id)) return;
        foreach (var (category, words) in _config.FilteredWords)
        {
            var filteredWords = words.ToArray().ToList();
            var trip = false;
            
            filteredWords.Add(_testString[0] + category + _testString[1]);

            foreach (var word in filteredWords)
            {
                if (trip) continue;
                if (context.Message.Content.ToLower().Contains(word.ToLower()))
                {
                    // Filter Trip!
                    await ProcessFilterTrip(context, word, category, true);
                    trip = true;
                }
            }
        }
    }

    public async Task ProcessMessage(IIzzyMessage messageParam, IIzzyClient client)
    {
        if (messageParam.Author.Id == client.CurrentUser.Id) return; // Don't process self.
        
        if (!_config.FilterEnabled) return;
        if (messageParam.Author.IsBot) return; // Don't listen to bots
        if (!DiscordHelper.IsInGuild(messageParam)) return;
        if (!DiscordHelper.IsProcessableMessage(messageParam)) return; // Not processable
        if (messageParam is not IIzzyUserMessage message) return; // Not processable
        IIzzyContext context = client.MakeContext(message);
        
        if (!DiscordHelper.IsDefaultGuild(context)) return;
        
        if (_config.FilterIgnoredChannels.Contains(context.Channel.Id)) return;
        foreach (var (category, words) in _config.FilteredWords)
        {
            var filteredWords = words.ToArray().ToList();
            var trip = false;
            
            filteredWords.Add(_testString[0] + category + _testString[1]);


            foreach (var word in filteredWords)
            {
                if (trip) continue;
                if (context.Message.Content.ToLower().Contains(word.ToLower()))
                {
                    // Filter Trip!
                    await ProcessFilterTrip(context, word, category, false);
                    trip = true;
                }
            }
        }
    }
}