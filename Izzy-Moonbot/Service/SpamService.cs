using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using HtmlAgilityPack;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;

namespace Izzy_Moonbot.Service;

/*
 * This service handles anti-spam routines.
 * The anti-spam works like this:
 * - Each user has "pressure", this is a number based on several factors including but not limitied to:
 *   - Message length
 *   - Message attachments
 *   - Whether their last message is the same as this one
 * - This pressure decays by Config.SpamBasePressure every Config.SpamPressureDecay seconds
 * - If a users pressure reaches or exceeds Config.SpamMaxPressure, the bot will automatically silence them and inform the mods of this action
 * - Users will stay silenced until either banned, kicked, or unsilenced by the mods.
 *
 * Other modules/services are capable of reading and adding pressure to users. This can be useful for increasing pressure due to a filter violation.
 * (however this is mainly implemented so that SpamModule can output the pressure of a user via command)
 */
public class SpamService
{
    // Required services
    private readonly LoggingService _logger;
    private readonly ModService _mod;
    private readonly ModLoggingService _modLogger;
    
    // Configuation
    private readonly Config _config;
    private readonly Dictionary<ulong, User> _users;
    
    // Utility parameters
    private readonly Regex _mention = new("<@&?[0-9]+>");
    private readonly Regex _url = new("https?://(.+\\.)*(.+)\\.([A-z]+)(/?.+)*", RegexOptions.IgnoreCase);
    private readonly Regex _noUnfurlUrl = new("<{1}https?://(.+\\.)*(.+)\\.([A-z]+)(/?.+)*>{1}", RegexOptions.IgnoreCase);
    #if DEBUG
    /*
     * The test string is a way to test the spam filter without actually spamming
     * The test string is programmed to immediately set pressure to Config.SpamMaxPressure and is not defined if the bot is built with the Release flag.
     */
    private readonly string _testString = "=+i7B3s+#(-{×jn6Ga3F~lA:IZZY_PRESSURE_TEST:H4fgd3!#!";
    #endif
    
    // Pull services from the service system
    public SpamService(LoggingService logger, ModService mod, ModLoggingService modLogger, Config config, Dictionary<ulong, User> users)
    {
        _logger = logger;
        _mod = mod;
        _modLogger = modLogger;
        _config = config;
        _users = users;
    }
    
    // Register required events
    public void RegisterEvents(DiscordSocketClient client)
    {
        // Register MessageReceived event
        client.MessageReceived += (message) => Task.Run(async () => { await MessageReceiveEvent(message, client); });
    }

    /// <summary>
    /// Get the last known pressure of a given user by their Discord id.
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>The pressure of the user.</returns>
    public double GetPressure(ulong id) => _users[id].Pressure; // Just return the user's pressure

    public List<PreviousMessageItem> GetPreviousMessages(ulong id) => _users[id].PreviousMessages; // Just return the user's previous messages

    private async Task<double> GetAndDecayPressure(ulong id)
    {
        // Get current time, calculate pressure loss per second and time difference between now and last pressure task then calculate full pressure loss
        var now = DateTimeOffset.UtcNow;
        var pressureLossPerSecond = _config.SpamBasePressure / _config.SpamPressureDecay;
        var pressure = _users[id].Pressure;
        var difference = now - _users[id].Timestamp;
        var pressureLoss = difference.TotalSeconds * pressureLossPerSecond;

        // Execute pressure loss
        pressure -= pressureLoss;
        if (pressure <= 0) pressure = 0; // Pressure cannot be negative

        // Save pressure loss
        _users[id].Pressure = pressure;
        _users[id].Timestamp = now;
        
        // Remove out of date message cache.
        // TODO: Move to it's own method. Not sure how to without saving the users file again...
        var messages = _users[id].PreviousMessages.ToArray().ToList(); // .NET gets angry if we modify the iterator while iterating
        
        foreach (var previousMessageItem in messages)
        {
            if ((previousMessageItem.Timestamp.ToUniversalTime().ToUnixTimeMilliseconds() + (_config.SpamMessageDeleteLookback * 1000)) <=
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                // Message is out of date, remove it
                _users[id].PreviousMessages.Remove(previousMessageItem);
            }
        }
        
        await FileHelper.SaveUsersAsync(_users);

        // Return pressure
        return pressure;
    }

    public async Task<double> IncreasePressure(ulong id, double pressure)
    {
        // Increase pressure
        _users[id].Pressure += pressure;

        // Save user
        _users[id].Timestamp = DateTimeOffset.UtcNow;
        await FileHelper.SaveUsersAsync(_users);

        // Return new pressure
        return _users[id].Pressure;
    }

    private async Task ProcessPressure(ulong id, SocketUserMessage message, SocketGuildUser user,
        SocketCommandContext context)
    {
        var pressure = 0.0;
        var pressureBreakdown = new List<(double, string)>{};

        var lengthPressure = Math.Round(_config.SpamLengthPressure * message.Content.Length, 2);
        if (lengthPressure > 0)
        {
            pressure += lengthPressure;
            pressureBreakdown.Add((lengthPressure, $"Length: {lengthPressure} ≈ {message.Content.Length} characters × {_config.SpamLengthPressure}"));
        }

        var newlineCount = (message.Content.Split("\n").Length - 1);
        var linePressure = Math.Round(_config.SpamLengthPressure * newlineCount, 2);
        if (linePressure > 0)
        {
            pressure += linePressure;
            pressureBreakdown.Add((linePressure, $"Lines: {linePressure} ≈ {newlineCount} line breaks × {_config.SpamLengthPressure}"));
        }

        // Attachments, embeds, and stickers count as Image pressure
        // TODO: figure out better names for this
        var embedsCount = message.Attachments.Count + message.Embeds.Count + message.Stickers.Count;
        if (embedsCount >= 1)
        {
            var embedPressure = Math.Round(_config.SpamImagePressure * embedsCount, 2);
            pressure += embedPressure;
            pressureBreakdown.Add((embedPressure , $"Embeds: {embedPressure} ≈ {embedsCount} embeds × {_config.SpamImagePressure}"));
        }

        // Check if there's at least one url in the message (and there's no embeds)
        if (_url.IsMatch(message.Content) && message.Embeds.Count == 0)
        {
            // Because url pressure can occur multiple times, we store the pressure to add here
            var totalMatches = 0;
            
            // Go through each "word" because the URL regex is funky
            foreach (var content in message.Content.Split(" "))
            {
                // Filter out matches 
                var matches = _url.Matches(content).ToList();
                foreach (Match match in _noUnfurlUrl.Matches(content))
                {
                    // Check if url is in fact set to not unfurl
                    var matchToRemove = matches.Find(urlMatch => match.Value.Contains(urlMatch.Value));
                    // If not, just continue
                    if(matchToRemove == null) continue;
                    
                    // If it is, remove the match.
                    matches.Remove(matchToRemove);
                }

                totalMatches += matches.Count;
            }

            var embedPressure = Math.Round(_config.SpamImagePressure * totalMatches, 2);
            if (embedPressure > 0.0)
            {
                // It is, increase pressure and add pressure trace
                pressure += embedPressure;
                pressureBreakdown.Add((embedPressure, $"URLs: {embedPressure} ≈ {totalMatches} unfurling URLs × {_config.SpamImagePressure}"));
            }
        }

        // Mention pressure
        if (_mention.IsMatch(message.Content))
        {
            var mentionCount = _mention.Matches(message.Content).Count;
            var mentionPressure = Math.Round(_config.SpamPingPressure * mentionCount, 2);
            pressure += mentionPressure;
            pressureBreakdown.Add((mentionPressure, $"Mentions: {mentionPressure} ≈ {mentionCount} mentions × {_config.SpamPingPressure}"));
        }

        // Repeat pressure
        if (message.CleanContent.ToLower() == _users[id].PreviousMessage.ToLower())
        {
            pressure += _config.SpamRepeatPressure;
            pressureBreakdown.Add((_config.SpamRepeatPressure, $"Repeat of Previous Message: {_config.SpamRepeatPressure}"));
        }

        // Add the Base pressure last so that, if one of the other categories happens to equal it,
        // that other category will show up higher in the sorted breakdown.
        pressure += _config.SpamBasePressure;
        pressureBreakdown.Add((_config.SpamBasePressure, $"Base: {_config.SpamBasePressure}"));

        #if DEBUG
        // Test string, only exists when built on Debug.
        if (message.Content == _testString)
        {
            pressure = _config.SpamMaxPressure;
            pressureBreakdown = new List<(double, string)> { (_config.SpamMaxPressure, "Test string") };
        }
        #endif

        _users[id].PreviousMessage = context.Message.CleanContent;

        var messageItem =
            new PreviousMessageItem(message.Id, context.Channel.Id, context.Guild.Id, DateTimeOffset.UtcNow);
        
        _users[id].PreviousMessages.Add(messageItem);
        
        await FileHelper.SaveUsersAsync(_users);

        var oldPressureBeforeDecay = _users[id].Pressure * 1; // seperate it from the thingy

        var oldPressureAfterDecay = await GetAndDecayPressure(id);

        var newPressure = await IncreasePressure(id, pressure);

        await _logger.Log($"Pressure increase from {oldPressureAfterDecay} by {pressure} to {newPressure}/{_config.SpamMaxPressure}. Pressure breakdown: {string.Join(Environment.NewLine, pressureBreakdown)}", context, level: LogLevel.Debug);

        // If this user already tripped spam pressure, but was either immune to silencing or managed to
        // send another message before Izzy could respond, we don't want duplicate notifications
        var alreadyAlerted = oldPressureBeforeDecay >= _config.SpamMaxPressure;

        if (newPressure >= _config.SpamMaxPressure)
        {
            await _logger.Log("Spam pressure trip, checking whether user should be silenced or not...", context, level: LogLevel.Debug);
            var roleIds = user.Roles.Select(roles => roles.Id).ToList();
            if (_config.SpamBypassRoles.Overlaps(roleIds) || 
                (DiscordHelper.IsDev(user.Id) && _config.SpamDevBypass))
            {
                // User has a role which bypasses the punishment of spam trigger. Mention it in action log.
                await _logger.Log("No silence, user has role(s) in Config.SpamBypassRoles", context, level: LogLevel.Debug);

                if (alreadyAlerted) return;

                var embedBuilder = new EmbedBuilder()
                    .WithTitle(":warning: Spam detected")
                    .WithDescription("No action was taken against this user as they have a role which bypasses punishment (`SpamBypassRoles`)")
                    .WithColor(3355443)
                    .AddField("User", $"<@{context.User.Id}> (`{context.User.Id}`)", true)
                    .AddField("Channel", $"<#{context.Channel.Id}>", true)
                    .AddField("Pressure", $"This user's last message raised their pressure from {oldPressureAfterDecay} to {newPressure}, exceeding {_config.SpamMaxPressure}")
                    .AddField("Breakdown of last message", PonyReadableBreakdown(pressureBreakdown))
                    .WithTimestamp(context.Message.Timestamp);

                await _modLogger.CreateModLog(context.Guild)
                    .SetContent($"Spam detected by <@{user.Id}>")
                    .SetEmbed(embedBuilder.Build())
                    .SetFileLogContent(
                        $"{user.Username}#{user.Discriminator} ({user.DisplayName}) (`{user.Id}`) exceeded pressure max ({newPressure}/{_config.SpamMaxPressure}) in #{message.Channel.Name} (`{message.Channel.Id}`).{Environment.NewLine}" +
                        $"Pressure breakdown: {PonyReadableBreakdown(pressureBreakdown)}{Environment.NewLine}" +
                        $"Did nothing: User has a role which bypasses punishment or has dev bypass.") 
                    .Send();
            }
            else
            {
                // User is not immune to spam punishments, process trip.
                await _logger.Log("Silence, executing trip method.", context, level: LogLevel.Debug);
                await ProcessTrip(id, oldPressureAfterDecay, newPressure, pressureBreakdown, message, user, context, alreadyAlerted);
            }
        }
    }

    private async Task ProcessTrip(ulong id, double oldPressureAfterDecay, double pressure, List<(double, string)> pressureBreakdown,
        SocketUserMessage message, SocketGuildUser user, SocketCommandContext context, bool alreadyAlerted = false)
    {
        // Silence user, this also logs the action.
        await _mod.SilenceUser(user, $"Exceeded pressure max ({pressure}/{_config.SpamMaxPressure}) in <#{message.Channel.Id}>");

        var alreadyDeletedMessages = 0;
        
        // Remove all messages considered part of spam.
        foreach (var previousMessageItem in _users[id].PreviousMessages)
        {
            try
            {
                var previousMessage = await context.Guild.GetTextChannel(previousMessageItem.ChannelId)
                    .GetMessageAsync(previousMessageItem.Id);
                if (previousMessage == null) alreadyDeletedMessages++;
                else await previousMessage.DeleteAsync();
            }
            catch (HttpException exception)
            {
                if (exception.DiscordCode == DiscordErrorCode.UnknownMessage)
                {
                    // Message already deleted
                    alreadyDeletedMessages++;
                }
            }
            catch (Exception ex)
            {
                // Something funky is going on here
                await _logger.Log($"Exception occured while trying to delete message, assuming deleted.", level: LogLevel.Warning);
                await _logger.Log($"Message ID: {previousMessageItem.Id}", level: LogLevel.Warning);
                await _logger.Log($"Message: {ex.Message}", level: LogLevel.Warning);
                await _logger.Log($"Source: {ex.Source}", level: LogLevel.Warning);
                await _logger.Log($"Method: {ex.TargetSite}", level: LogLevel.Warning);
                await _logger.Log($"Stack Trace: {ex.StackTrace}", level: LogLevel.Warning);

                alreadyDeletedMessages++;
            }
        }

        if (alreadyAlerted) return;
        
        var embedBuilder = new EmbedBuilder()
            .WithTitle(":warning: Spam detected")
            .WithColor(16776960)
            .AddField("User", $"<@{context.User.Id}> (`{context.User.Id}`)", true)
            .AddField("Channel", $"<#{context.Channel.Id}>", true)
            .AddField("Pressure", $"This user's last message raised their pressure from {oldPressureAfterDecay} to {pressure}, exceeding {_config.SpamMaxPressure}")
            .AddField("Breakdown of last message", $"{PonyReadableBreakdown(pressureBreakdown)}")
            .WithTimestamp(context.Message.Timestamp);

        if (alreadyDeletedMessages != 0)
            embedBuilder.WithDescription(
                $":information_source: **I was unable to delete {alreadyDeletedMessages} messages by this user. Please double check that these messages have been deleted.**");

        await _modLogger.CreateModLog(context.Guild)
            .SetContent($"<@&{_config.ModRole}> Spam detected by <@{user.Id}>")
            .SetEmbed(embedBuilder.Build())
            .SetFileLogContent(
                $"{user.Username}#{user.Discriminator} ({user.DisplayName}) (`{user.Id}`) was silenced for exceeding pressure max ({pressure}/{_config.SpamMaxPressure}) in #{message.Channel.Name} (`{message.Channel.Id}`).{Environment.NewLine}" +
                $"Pressure breakdown: {PonyReadableBreakdown(pressureBreakdown)}{Environment.NewLine}" +
                $"{(alreadyDeletedMessages != 0 ? $"I was unable to delete {alreadyDeletedMessages} messages from this user, please double check whether their messages have been deleted." : "")}")
            .Send();
    }

    private string PonyReadableBreakdown(List<(double, string)> pressureBreakdown)
    {
        var orderedBreakdown = pressureBreakdown.OrderBy(tuple => -tuple.Item1).Select(tuple => tuple.Item2).ToList();
        orderedBreakdown[0] = "**" + orderedBreakdown[0] + "**"; // show the top contributor in bold
        return string.Join(Environment.NewLine, orderedBreakdown);
    }

    private async Task MessageReceiveEvent(SocketMessage messageParam, DiscordSocketClient client)
    {
        if (!_config.SpamEnabled) return; // anti-spam is off
        if (messageParam.Author.IsBot) return; // Don't listen to bots
        if (!DiscordHelper.IsInGuild(messageParam)) return; // Not in guild (in dm/group)
        if (!DiscordHelper.IsProcessableMessage(messageParam)) return; // Not processable
        if (messageParam is not SocketUserMessage message) return; // Not processable
        
        if (_config.ThreadOnlyMode &&
            (message.Channel.GetChannelType() != ChannelType.PublicThread &&
             message.Channel.GetChannelType() != ChannelType.PrivateThread)) return; // Not a thread, in thread only mode

        var context = new SocketCommandContext(client, message);

        if (!DiscordHelper.IsDefaultGuild(context)) return;
        
        var guildUser = context.User as SocketGuildUser;

        if (guildUser.Id == client.CurrentUser.Id) return; // Don't process the bot
        if (_config.SpamIgnoredChannels.Contains(context.Channel.Id)) return; // Don't process ignored channels

        await ProcessPressure(guildUser.Id, context.Message, guildUser, context);
    }
}
