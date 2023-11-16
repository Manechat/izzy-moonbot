using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;

namespace Izzy_Moonbot.Service;

public class SpamService
{
    private readonly LoggingService _logger;
    private readonly ModService _mod;
    private readonly ModLoggingService _modLogger;
    private readonly Config _config;
    private readonly Dictionary<ulong, User> _users;
    private readonly TransientState _state;
    
    private readonly Regex _mention = new("<@&?[0-9]+>");
    private readonly Regex _url = new("https?://(.+\\.)*(.+)\\.([A-z]+)(/?.+)*", RegexOptions.IgnoreCase);
    private readonly Regex _noUnfurlUrl = new("<{1}https?://(.+\\.)*(.+)\\.([A-z]+)(/?.+)*>{1}", RegexOptions.IgnoreCase);
    /*
     * The test string is a way to test the spam filter without actually spamming
     * The test string is programmed to immediately set pressure to Config.SpamMaxPressure.
     */
    public static readonly string _testString = "=+i7B3s+#(-{×jn6Ga3F~lA:IZZY_PRESSURE_TEST:H4fgd3!#!";

    private List<ulong> usersCurrentlyTrippingSpam = new();

    public SpamService(LoggingService logger, ModService mod, ModLoggingService modLogger, Config config, Dictionary<ulong, User> users, TransientState state)
    {
        _logger = logger;
        _mod = mod;
        _modLogger = modLogger;
        _config = config;
        _users = users;
        _state = state;
    }
    
    public void RegisterEvents(IIzzyClient client)
    {
        // Register MessageReceived event
        client.MessageReceived += async (message) => await DiscordHelper.LeakOrAwaitTask(MessageReceiveEvent(message, client));
    }

    public double GetPressure(ulong id)
    {
        if (!_state.RecentMessages.ContainsKey(id)) return 0.0;

        var recentMessages = _state.RecentMessages[id];
        RecentMessage? previousRecentMessage = null;

        var pressureDecayPerSecond = _config.SpamBasePressure / _config.SpamPressureDecay;
        double totalPressure = 0.0;

        foreach (var rm in recentMessages)
        {
            calculateMessagePressureWithoutDecay(rm, previousRecentMessage, out var pressureForMessage, out _);

            var pressureDecay = 0.0;
            if (previousRecentMessage is not null)
                pressureDecay = (rm.Timestamp - previousRecentMessage.Timestamp).TotalSeconds * pressureDecayPerSecond;
            totalPressure = pressureForMessage + Math.Max(totalPressure - pressureDecay, 0);

            previousRecentMessage = rm;
        }

        var finalPressureDecay = (DateTimeHelper.UtcNow - recentMessages.Last().Timestamp).TotalSeconds * pressureDecayPerSecond;
        totalPressure -= finalPressureDecay;

        return totalPressure;
    }

    private void calculateMessagePressureWithoutDecay(RecentMessage message, RecentMessage? previousMessage, out double pressure, out List<(double, string)> pressureBreakdown)
    {
        pressure = 0.0;
        pressureBreakdown = new List<(double, string)> { };

        var lengthPressure = Math.Round(_config.SpamLengthPressure * message.Content.Length, 2);
        if (lengthPressure > 0)
        {
            pressure += lengthPressure;
            pressureBreakdown.Add((lengthPressure, $"Length: {lengthPressure} ≈ {message.Content.Length} characters × {_config.SpamLengthPressure}"));
        }

        var newlineCount = (message.Content.Split("\n").Length - 1);
        var linePressure = Math.Round(_config.SpamLinePressure * newlineCount, 2);
        if (linePressure > 0)
        {
            pressure += linePressure;
            pressureBreakdown.Add((linePressure, $"Lines: {linePressure} ≈ {newlineCount} line breaks × {_config.SpamLinePressure}"));
        }

        // Attachments, embeds, and stickers count as Image pressure
        // TODO: figure out better names for this
        var embedsCount = message.EmbedsCount;
        if (embedsCount >= 1)
        {
            var embedPressure = Math.Round(_config.SpamImagePressure * embedsCount, 2);
            pressure += embedPressure;
            pressureBreakdown.Add((embedPressure, $"Embeds: {embedPressure} ≈ {embedsCount} embeds × {_config.SpamImagePressure}"));
        }

        // Check if there's at least one url in the message (and there's no embeds)
        if (_url.IsMatch(message.Content) && message.EmbedsCount == 0)
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
                    if (matchToRemove == null) continue;

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
        if (previousMessage is not null && message.Content.ToLower() == previousMessage.Content.ToLower() && message.Content != "")
        {
            pressure += _config.SpamRepeatPressure;
            pressureBreakdown.Add((_config.SpamRepeatPressure, $"Repeat of Previous Message: {_config.SpamRepeatPressure}"));
        }

        // Unusual character pressure

        // If you change this list of categories, be sure to update the config item's documentation too
        var usualCategories = new List<UnicodeCategory> {
            UnicodeCategory.UppercaseLetter,
            UnicodeCategory.LowercaseLetter,
            UnicodeCategory.SpaceSeparator,
            UnicodeCategory.DecimalDigitNumber,
            UnicodeCategory.OpenPunctuation,
            UnicodeCategory.ClosePunctuation,
            UnicodeCategory.OtherPunctuation,
            UnicodeCategory.FinalQuotePunctuation
        };
        var unusualCharactersCount = message.Content.ToCharArray().Where(c => c != '\r' && c != '\n' && !usualCategories.Contains(CharUnicodeInfo.GetUnicodeCategory(c))).Count();
        var unusualCharacterPressure = Math.Round(unusualCharactersCount * _config.SpamUnusualCharacterPressure, 2);
        if (unusualCharacterPressure > 0)
        {
            pressure += unusualCharacterPressure;
            pressureBreakdown.Add((unusualCharacterPressure, $"Unusual Characters: {unusualCharacterPressure} ≈ {unusualCharactersCount} unusual characters × {_config.SpamUnusualCharacterPressure}"));
        }

        // Add the Base pressure last so that, if one of the other categories happens to equal it,
        // that other category will show up higher in the sorted breakdown.
        pressure += _config.SpamBasePressure;
        pressureBreakdown.Add((_config.SpamBasePressure, $"Base: {_config.SpamBasePressure}"));

        // Test string.
        if (message.Content == _testString)
        {
            pressure = _config.SpamMaxPressure;
            pressureBreakdown = new List<(double, string)> { (_config.SpamMaxPressure, "Test string") };
        }
    }

    private async Task ProcessPressure(ulong id, IIzzyUserMessage message, IIzzyGuildUser user, IIzzyContext context)
    {
        if (context.Guild == null)
            throw new InvalidOperationException("ProcessPressure was somehow called with a non-guild context");

        var recentMessages = _state.RecentMessages[id];
        RecentMessage? previousRecentMessage = null;

        var pressureDecayPerSecond = _config.SpamBasePressure / _config.SpamPressureDecay;
        List<(double, string)> lastPressureBreakdown = new();
        double oldPressureAfterDecay = 0.0;
        double totalPressure = 0.0;

        foreach (var rm in recentMessages)
        {
            calculateMessagePressureWithoutDecay(rm, previousRecentMessage, out var pressureForMessage, out lastPressureBreakdown);

            var pressureDecay = 0.0;
            if (previousRecentMessage is not null)
                pressureDecay = (rm.Timestamp - previousRecentMessage.Timestamp).TotalSeconds * pressureDecayPerSecond;
            oldPressureAfterDecay = Math.Max(totalPressure - pressureDecay, 0);
            totalPressure = pressureForMessage + oldPressureAfterDecay;

            previousRecentMessage = rm;
        }

        var finalPressureDecay = (DateTimeHelper.UtcNow - recentMessages.Last().Timestamp).TotalSeconds * pressureDecayPerSecond;
        totalPressure -= finalPressureDecay;

        if (totalPressure >= _config.SpamMaxPressure)
        {
            _logger.Log("Spam pressure trip, checking whether user should be silenced or not...", context, level: LogLevel.Debug);
            var roleIds = user.Roles.Select(roles => roles.Id).ToList();
            if (_config.SpamBypassRoles.Overlaps(roleIds) || 
                (DiscordHelper.IsDev(user.Id) && _config.SpamDevBypass))
            {
                // User has a role which bypasses the punishment of spam trigger. Mention it in action log.
                _logger.Log("No silence, user has role(s) in Config.SpamBypassRoles", context, level: LogLevel.Debug);

                var embedBuilder = new EmbedBuilder()
                    .WithTitle(":warning: Spam detected")
                    .WithDescription("No action was taken against this user as they have a role which bypasses punishment (`SpamBypassRoles`)")
                    .WithColor(3355443)
                    .AddField("User", $"<@{context.User.Id}> (`{context.User.Id}`)", true)
                    .AddField("Channel", $"<#{context.Channel.Id}>", true)
                    .AddField("Pressure", $"This user's last message raised their pressure from {oldPressureAfterDecay} to {totalPressure}, exceeding {_config.SpamMaxPressure}")
                    .AddField("Breakdown of last message", PonyReadableBreakdown(lastPressureBreakdown));

                await _modLogger.CreateModLog(context.Guild)
                    .SetContent($"Spam detected by <@{user.Id}>")
                    .SetEmbed(embedBuilder.Build())
                    .SetFileLogContent(
                        $"{user.DisplayName} (`{user.Username}`/`{user.Id}`) exceeded pressure max ({totalPressure}/{_config.SpamMaxPressure}) in #{message.Channel.Name} (`{message.Channel.Id}`).\n" +
                        $"Pressure breakdown: {PonyReadableBreakdown(lastPressureBreakdown)}\n" +
                        $"Did nothing: User has a role which bypasses punishment or has dev bypass.") 
                    .Send();
            }
            else
            {
                // User is not immune to spam punishments, process trip.
                _logger.Log($"Message {message.Id} task attempting to acquire lock on usersCurrentlyTrippingSpam.", context);
                lock (usersCurrentlyTrippingSpam)
                {
                    if (usersCurrentlyTrippingSpam.Contains(id))
                    {
                        _logger.Log($"User {id} already has a spam trip being processed. Message {message.Id} task returning early.", context);
                        return;
                    }
                    else
                    {
                        _logger.Log($"No spam trip in progress for user {id}. Message {message.Id} task proceeding with ProcessTrip() call.", context);
                        usersCurrentlyTrippingSpam.Add(id);
                    }
                }

                await ProcessTrip(id, oldPressureAfterDecay, totalPressure, lastPressureBreakdown, message, user, context);
            }
        }
    }

    private async Task ProcessTrip(ulong id, double oldPressureAfterDecay, double pressure, List<(double, string)> pressureBreakdown,
        IIzzyMessage message, IIzzyGuildUser user, IIzzyContext context)
    {
        if (context.Guild == null)
            throw new InvalidOperationException("ProcessTrip was somehow called with a non-guild context");

        // Silence or timeout user, this also logs the action.
        var auditLogMessage = $"Exceeded pressure max ({pressure}/{_config.SpamMaxPressure}) in <#{message.Channel.Id}>";
        var alreadySilenced = _users[id].Silenced;
        if (alreadySilenced)
            await user.SetTimeOutAsync(TimeSpan.FromHours(1), new RequestOptions { AuditLogReason = auditLogMessage });
        else
            await _mod.SilenceUser(user, auditLogMessage);

        var bulkDeletionLog = new List<(DateTimeOffset, string)>();
        var bulkDeletionCount = 0;
        var alreadyDeletedMessages = 0;

        var secondsUntilIrrelevant = _config.SpamPressureDecay * (_config.SpamMaxPressure / _config.SpamBasePressure);

        // Remove all messages considered part of spam.
        foreach (var recentMessageItem in _state.RecentMessages[id])
        {
            if ((DateTimeHelper.UtcNow - recentMessageItem.Timestamp).TotalSeconds > secondsUntilIrrelevant) continue;

            try
            {
                var channel = context.Guild.GetTextChannel(recentMessageItem.ChannelId);
                if (channel == null)
                    throw new InvalidOperationException($"{id}'s RecentMessages are somehow from a non-existent channel");

                var recentMessage = channel is null ? null : await channel.GetMessageAsync(recentMessageItem.MessageId);
                if (recentMessage is not null)
                {
                    if (recentMessage.Content != "")
                        bulkDeletionLog.Add((recentMessageItem.Timestamp,
                            $"[{recentMessageItem.Timestamp}] in #{channel?.Name}: {recentMessage.Content}"));
                    await recentMessage.DeleteAsync();
                    bulkDeletionCount++;
                }
                else
                    alreadyDeletedMessages++;
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
                _logger.Log($"Exception occured while trying to delete message, assuming deleted.", level: LogLevel.Warning);
                _logger.Log($"Message Link: {recentMessageItem.GetJumpUrl()}", level: LogLevel.Warning);
                _logger.Log($"Message: {ex.Message}", level: LogLevel.Warning);
                _logger.Log($"Source: {ex.Source}", level: LogLevel.Warning);
                _logger.Log($"Method: {ex.TargetSite}", level: LogLevel.Warning);
                _logger.Log($"Stack Trace: {ex.StackTrace}", level: LogLevel.Warning);

                alreadyDeletedMessages++;
            }
        }

        // We're done asking Discord to clean up this user's spam, so before we post mod logs
        // mark the user as no longer having an in-progress spam trip.
        lock (usersCurrentlyTrippingSpam)
        {
            if (!usersCurrentlyTrippingSpam.Contains(id))
                _logger.Log($"User {id} is somehow missing from usersCurrentlyTrippingSpam in the ProcessTrip() call for them. This should be impossible.", level: LogLevel.Error);
            else
            {
                _logger.Log($"ProcessTrip() call for message {message.Id} by user {id} is done cleaning up. Removing them from usersCurrentlyTrippingSpam.");
                usersCurrentlyTrippingSpam.Remove(id);
            }
        }

        string? bulkLogJumpUrl = null;
        if (bulkDeletionLog.Count > 0)
        {
            var logChannelId = _config.LogChannel;
            if (logChannelId == 0)
                _logger.Log("I couldn't post a bulk deletion log, because .config LogChannel hasn't been set.");
            else
            {
                var logChannel = context.Guild.GetTextChannel(logChannelId);
                if (logChannel is not null)
                {
                    _logger.Log($"Assembling a bulk deletion log from the content of {bulkDeletionLog.Count} deleted messages");
                    bulkDeletionLog.Sort((x, y) => x.Item1.CompareTo(y.Item1));
                    var bulkDeletionLogString = string.Join("\n\n", bulkDeletionLog.Select(logElement => logElement.Item2));
                    var s = new MemoryStream(Encoding.UTF8.GetBytes(bulkDeletionLogString));
                    var fa = new FileAttachment(s, $"{context.User.Username}_{context.User.Id}_spam_bulk_deletion_log_{DateTimeHelper.UtcNow.ToString()}.txt");

                    var spamBulkDeletionMessage = await logChannel.SendFileAsync(fa,
                        $"Deleted recent messages from {context.User.Username} ({context.User.Id}) after they tripped spam detection, here's the bulk deletion log:");
                    bulkLogJumpUrl = spamBulkDeletionMessage.GetJumpUrl();
                }
                else
                    _logger.Log("Something went wrong trying to access LogChannel.");
            }
        }

        var embedBuilder = new EmbedBuilder()
            .WithTitle(":warning: Spam detected")
            .WithColor(16776960)
            .AddField(alreadySilenced ? "Timeout User" : "Silenced User", $"<@{context.User.Id}> (`{context.User.Id}`)", true)
            .AddField("Channel", $"<#{context.Channel.Id}>", true)
            .AddField("Pressure", $"This user's last message raised their pressure from {oldPressureAfterDecay} to {pressure}, exceeding {_config.SpamMaxPressure}")
            .AddField("Breakdown of last message", $"{PonyReadableBreakdown(pressureBreakdown)}");

        if (bulkLogJumpUrl is not null)
            embedBuilder.AddField("Bulk Deletion Log", bulkLogJumpUrl);

        if (alreadyDeletedMessages != 0)
            embedBuilder.WithDescription(
                $":information_source: **I was unable to delete {alreadyDeletedMessages} messages by this user. Please double check that these messages have been deleted.**");

        await _modLogger.CreateModLog(context.Guild)
            .SetContent(
                (alreadySilenced ?
                    $"I've given <@{user.Id}> a one-hour timeout for spamming after being silenced" :
                    $"<@&{_config.ModRole}> I've silenced <@{user.Id}> for spamming")
                + $" and deleted {bulkDeletionCount} of their message(s)"
            )
            .SetEmbed(embedBuilder.Build())
            .SetFileLogContent(
                $"{user.DisplayName} (`{user.Username}`/`{user.Id}`) was silenced/timed out for exceeding pressure max ({pressure}/{_config.SpamMaxPressure}) in #{message.Channel.Name} (`{message.Channel.Id}`).\n" +
                $"Pressure breakdown: {PonyReadableBreakdown(pressureBreakdown)}\n" +
                $"{(alreadyDeletedMessages != 0 ? $"I was unable to delete {alreadyDeletedMessages} messages from this user, please double check whether their messages have been deleted." : "")}")
            .Send();
    }

    private string PonyReadableBreakdown(List<(double, string)> pressureBreakdown)
    {
        var orderedBreakdown = pressureBreakdown.OrderBy(tuple => -tuple.Item1).Select(tuple => tuple.Item2).ToList();
        orderedBreakdown[0] = "**" + orderedBreakdown[0] + "**"; // show the top contributor in bold
        return string.Join('\n', orderedBreakdown);
    }

    private async Task MessageReceiveEvent(IIzzyMessage messageParam, IIzzyClient client)
    {
        // the RecentMessages cache needs updating even if we aren't doing spam detection
        await UpdateRecentMessages(messageParam, client);

        if (!_config.SpamEnabled) return; // anti-spam is off
        if (messageParam.Author.IsBot) return; // Don't listen to bots
        if (!DiscordHelper.IsInGuild(messageParam)) return; // Not in guild (in dm/group)
        if (!DiscordHelper.IsProcessableMessage(messageParam)) return; // Not processable
        if (messageParam is not IIzzyUserMessage message) return; // Not processable
        
        var context = client.MakeContext(message);

        if (!DiscordHelper.IsDefaultGuild(context)) return;
        
        var guildUser = context.User as IIzzyGuildUser;
        if (guildUser == null) return; // Not processable
        if (guildUser.Id == client.CurrentUser.Id) return; // Don't process the bot
        if (_config.SpamIgnoredChannels.Contains(context.Channel.Id)) return; // Don't process ignored channels

        await ProcessPressure(guildUser.Id, context.Message, guildUser, context);
    }

    private async Task UpdateRecentMessages(IIzzyMessage message, IIzzyClient client)
    {
        var author = message.Author;
        if ((message.Channel.Id != _config.ModChannel) && DiscordHelper.IsInGuild(message))
        {
            var embedsCount = message.Attachments.Count + message.Embeds.Count + message.Stickers.Count;

            if (!_state.RecentMessages.ContainsKey(author.Id))
                _state.RecentMessages[author.Id] = new();

            var recentMessages = _state.RecentMessages[author.Id];
            recentMessages.Add(new RecentMessage(message.Id, message.Channel.Id, message.Timestamp, message.Content, embedsCount));

            if (recentMessages.Count > 5)
            {
                var secondsUntilIrrelevant = _config.SpamPressureDecay * (_config.SpamMaxPressure / _config.SpamBasePressure);
                while (
                    (DateTimeHelper.UtcNow - recentMessages[0].Timestamp).TotalSeconds > secondsUntilIrrelevant &&
                    recentMessages.Count > 5
                )
                {
                    recentMessages.RemoveAt(0);
                }
            }
        }
    }
}
