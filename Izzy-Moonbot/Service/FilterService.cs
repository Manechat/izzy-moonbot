using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Izzy_Moonbot.Service
{
    using Izzy_Moonbot.Settings;
    using Discord.Commands;

    public class FilterService
    {
        private ServerSettings _settings;
        private ModService _mod;
        private DiscordSocketClient _client;

        /*
         * The testString is a specific string that, while not in the actual filter list
         * is treated as if it is. Anyone who says it will be treated the same as if they
         * actually tripped the filter. This is used for testing the filter.
         * Only gets defined and used if compiled as Debug.
        */
        #if DEBUG
        private string[] _testString = {
            "=+i8F8s+#(-{×nsBIo8~lA:IZZY_FILTER_TEST:G8282!#",
            "#!"
        };
        #endif

        public FilterService(ServerSettings settings, ModService mod)
        {
            _settings = settings;
            _mod = mod;
        }

        private async Task LogFilterTrip(SocketCommandContext context, string word, string category, List<string> actionsTaken, bool onEdit = false, RestUserMessage message = null)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder()
                .WithTitle(":warning: Filter violation detected" + (onEdit ? " on message edit" : ""))
                .WithColor(16732240)
                .AddField("User", $"<@{context.User.Id}> (`{context.User.Id}`)", true)
                .AddField("Category", category, true)
                .AddField("Channel", $"<#{context.Channel.Id}>", true)
                .AddField("Filtered message (trigger word in bold)",
                    $"{context.Message.CleanContent.Replace(word, $"**{word}**")}")
                .WithTimestamp(onEdit ? (DateTimeOffset)context.Message.EditedTimestamp : context.Message.Timestamp);

            List<string> actions = new List<string>();
            actions.Add(":x: - **I've deleted the offending message**");

            if (actionsTaken.Contains("message"))
            {
                actions.Add($":speech_balloon: - **I've sent a message in response [[Go to Message]]({message.GetJumpUrl()})**");
            }
            if (actionsTaken.Contains("silence")) 
            { 
                actions.Add(":speech_balloon: - **I've silenced the user**");
            }

            if (_settings.SafeMode)
            {
                embedBuilder.AddField("How do I want to respond?", string.Join(Environment.NewLine, actions));
            }
            else
            {
                embedBuilder.AddField("What have I done in response?", string.Join(Environment.NewLine, actions));
            }
            
            await context.Guild.GetTextChannel(_settings.ModChannel).SendMessageAsync(embed: embedBuilder.Build());
        }

        private async Task ProcessFilterTrip(SocketCommandContext context, string word, string category, bool onEdit = false)
        {
            try
            {
                //await context.Message.DeleteAsync();
                if (!_settings.FilterResponseMessages.ContainsKey(category))
                    _settings.FilterResponseMessages[category] = null;
                if (!_settings.FilterResponseSilence.ContainsKey(category))
                    _settings.FilterResponseSilence[category] = false;
                
                string? messageResponse = _settings.FilterResponseMessages[category];
                bool shouldSilence = _settings.FilterResponseSilence[category];

                List<string> actions = new List<string>();
                RestUserMessage message = null;

                if (messageResponse != null)
                {
                    if (_settings.SafeMode)
                        message = await context.Guild.GetTextChannel(_settings.LogChannel).SendMessageAsync(
                            $"<@{context.User.Id}> {messageResponse}{Environment.NewLine}{Environment.NewLine}*I am posting this message here as safe mode is enabled.*");
                    else message = await context.Channel.SendMessageAsync($"<@{context.User.Id}> {messageResponse}");
                    actions.Add("message");
                }

                if (shouldSilence)
                {
                    _mod.SilenceUser(context.Guild.GetUser(context.User.Id), DateTimeOffset.UtcNow, null,
                        $"Filter violation ({category} category)");
                    actions.Add("silence");
                }
                
                await this.LogFilterTrip(context, word, category, actions, onEdit);
            }
            catch (KeyNotFoundException ex)
            {
                //await context.Message.DeleteAsync(); // Just in case
                List<string> actions = new List<string>();
                await this.LogFilterTrip(context, word, category, actions, onEdit);
                await context.Guild.GetTextChannel(_settings.ModChannel).SendMessageAsync(
                    $":warning: **I encountered a `KeyNotFoundException` while processing the above filter violation.**");
            }
        }
        
        public async Task ProcessMessageUpdate(SocketCommandContext context)
        {
            if (_settings.FilterMonitorEdits)
            {
                List<ulong> roleIds = (context.User as SocketGuildUser).Roles.Select(role => role.Id).ToList();
                if (_settings.FilterIgnoredRoles.Overlaps(roleIds)) return;

                    foreach (var (category, words) in _settings.FilteredWords)
                {
                    #if DEBUG
                    words.Add(_testString[0] + category + _testString[1]);
                    #endif
                
                    foreach (var word in words)
                    {
                        if (context.Message.Content.Contains(word))
                        {
                            // Filter Trip!
                            this.ProcessFilterTrip(context, word, category, true);
                        }
                    }
                }
            }
        }

        public void ProcessMessage(SocketCommandContext context)
        {
            if (!_settings.FilterEnabled) return;
            foreach (var (category, words) in _settings.FilteredWords)
            {
                #if DEBUG
                words.Add(_testString[0] + category + _testString[1]);
                #endif
                
                foreach (var word in words)
                {
                    if (context.Message.Content.Contains(word))
                    {
                        // Filter Trip!
                        this.ProcessFilterTrip(context, word, category);
                    }
                }
            }
        }
    }
}
