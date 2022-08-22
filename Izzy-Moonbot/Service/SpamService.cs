using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;

namespace Izzy_Moonbot.Service;

/*
 * This service handles managing of user "pressure".
 * If the pressure goes too high, then the user is silenced until a mod unsilences them.
 * 
 * Other services or modules can hook into this to assign a high pressure on other incidents
 * (e.g. filter trip, tags, etc)
 */
public class PressureService
{
    private readonly LoggingService _logger;
    private readonly Regex _mention = new("<@&?[0-9]+>");

    private readonly Regex _noEmbedUrl =
        new("<{1}https?://(.+\\.)*(.+)\\.([A-z]+)(/?.+)*>{1}", RegexOptions.IgnoreCase);

    private readonly string _testString = "=+i7B3s+#(-{×jn6Ga3F~lA:IZZY_PRESSURE_TEST:H4fgd3!#!";
    private readonly Regex _url = new("https?://(.+\\.)*(.+)\\.([A-z]+)(/?.+)*", RegexOptions.IgnoreCase);
    private readonly ModService _mod;
    private readonly ModLoggingService _modLog;
    private readonly Config _config;
    private readonly Dictionary<ulong, User> _users;

    public PressureService(LoggingService logger, ModLoggingService modLog, Config config, ModService mod,
        Dictionary<ulong, User> users)
    {
        _logger = logger;
        _modLog = modLog;
        _config = config;
        _mod = mod;
        _users = users;
    }
    
    public void RegisterEvents(DiscordSocketClient client)
    {
        client.MessageReceived += (message) => Task.Factory.StartNew(async () => { await ProcessMessage(message, client); });
        client.MessageUpdated += (oldMessage, newMessage, channel) =>Task.Factory.StartNew(async () => { await ProcessMessageUpdate(oldMessage, newMessage, channel, client); });
    }

    /// <summary>
    ///     Get the pressure of a given user by their Discord id.
    /// </summary>
    /// <param name="id">The Discord id of the user to get pressure for.</param>
    /// <returns>A <c>double</c> of the pressure. If the result is <c>-1</c> then the user hasn't been processed yet.</returns>
    public async Task<double> GetPressure(ulong id)
    {
        // If that user hasn't been processed yet, just return -1.
        if (!_users.ContainsKey(id)) return -1;

        var now = DateTime.UtcNow;
        var pressureLossPerSecond = _config.SpamBasePressure / _config.SpamPressureDecay;
        var pressure = _users[id].Pressure;
        var difference = now - _users[id].Timestamp;
        var totalPressureLoss = difference.TotalSeconds * pressureLossPerSecond;
        pressure -= totalPressureLoss;
        if (pressure < 0) pressure = 0;

        _users[id].Pressure = pressure;
        _users[id].Timestamp = now;
        await FileHelper.SaveUsersAsync(_users);

        return pressure;
    }

    /// <summary>
    ///     Get the pressure of a given user by their Discord id, without preforming pressure decay calculations.
    /// </summary>
    /// <param name="id">The Discord id of the user to get pressure for.</param>
    /// <returns>A <c>double</c> of the pressure. If the result is <c>-1</c> then the user hasn't been processed yet.</returns>
    public async Task<double> GetPressureWithoutModifying(ulong id)
    {
        // If that user hasn't been processed yet, just return -1.
        if (!_users.ContainsKey(id)) return -1;

        var pressure = _users[id].Pressure;

        if (pressure < 0) pressure = 0;

        return pressure;
    }

    /// <summary>
    ///     Increase the pressure of a user by their Discord id.
    /// </summary>
    /// <param name="id">The Discord id of the user to increase pressure for.</param>
    /// <param name="pressure">The pressure to add onto the current pressure.</param>
    /// <returns>A <c>double</c> of the new pressure.</returns>
    public async Task<double> IncreasePressure(ulong id, double pressure)
    {
        var now = DateTime.UtcNow;
        var currentPressure = await GetPressure(id);
        currentPressure += pressure;
        _users[id].Pressure = currentPressure;
        _users[id].Timestamp = now;

        await FileHelper.SaveUsersAsync(_users);

        return currentPressure;
    }

    private async Task ProcessPressure(ulong id, SocketUserMessage message, SocketGuildUser user,
        SocketCommandContext context)
    {
        // This doesn't do much currently, but it will once we add image checks and the like
        var pressure = _config.SpamBasePressure;
        var pressureTrace = $"{_config.SpamBasePressure} base, ";

        if (message.Attachments.Count >= 1 || message.Embeds.Count >= 1 || message.Stickers.Count >= 1)
        {
            // Image pressure.
            pressure += _config.SpamImagePressure *
                        (message.Attachments.Count + message.Embeds.Count + message.Stickers.Count);
            pressureTrace +=
                $"{_config.SpamImagePressure * message.Attachments.Count} attachments, {_config.SpamImagePressure * message.Embeds.Count} embeds, {_config.SpamImagePressure * message.Stickers.Count} stickers, ";
        }

        if (_url.IsMatch(message.Content) && !_noEmbedUrl.IsMatch(message.Content) && message.Embeds.Count == 0)
        {
            // No attempt to suppress embed made
            pressure += _config.SpamImagePressure * _url.Matches(message.Content).Count;
            pressureTrace += $"{_config.SpamImagePressure * _url.Matches(message.Content).Count} URLs, ";
        }

        if (_mention.IsMatch(message.Content))
        {
            pressure += _config.SpamPingPressure * _mention.Matches(message.Content).Count;
            pressureTrace +=
                $"{_config.SpamPingPressure * _mention.Matches(message.Content).Count} mentions, ";
        }

        if (message.Content.ToLower() == _users[id].PreviousMessage.ToLower())
        {
            pressure += _config.SpamRepeatPressure;
            pressureTrace += $"{_config.SpamRepeatPressure} repeat, ";
        }

        pressure += _config.SpamLengthPressure * message.Content.Length;
        pressureTrace += $"{_config.SpamLengthPressure * message.Content.Length} length, ";

        pressure += _config.SpamLinePressure * (message.Content.Replace("\r\n", "\n").Split("\n").Length - 1);
        pressureTrace +=
            $"{_config.SpamLinePressure * (message.Content.Replace("\r\n", "\n").Split("\n").Length - 1)} lines.";

        if (message.Content == _testString)
        {
            // Test string for pressure.
            pressure = _config.SpamMaxPressure + 1;
            pressureTrace = $"{_config.SpamMaxPressure + 1} teststring.";
        }

        var newPressure = await IncreasePressure(id, pressure);

        _users[message.Author.Id].PreviousMessage = message.Content.ToLower();
        FileHelper.SaveUsersAsync(_users);

        await _logger.Log($"Pressure increased by {pressure} to {newPressure}/{_config.SpamMaxPressure}", context,
            level: LogLevel.Debug);
        await _logger.Log(pressureTrace, null, level: LogLevel.Trace);

        if (newPressure >= _config.SpamMaxPressure)
        {
            var roleIds = user.Roles.Select(role => role.Id).ToList();
            if (_config.SpamBypassRoles.Overlaps(roleIds))
            {
                await _logger.Log("Spam pressure trip, user has role in SpamBypassRoles", context,
                    level: LogLevel.Trace);
                // Don't silence, but inform mods.
                await _modLog.CreateActionLog(context.Guild)
                    .AddTarget(message.Author as SocketGuildUser)
                    .SetReason(
                        $"Exceeded pressure max ({newPressure}/{_config.SpamMaxPressure}) in <#{message.Channel.Id}>. Didn't silence as they have a role which bypasses punishment (`SpamBypassRoles`).")
                    .Send();
            }
            else
            {
                await _logger.Log("Spam pressure trip, trying to bap", context, level: LogLevel.Trace);
                // Silence user.
                await _mod.SilenceUser(message.Author as SocketGuildUser, DateTimeOffset.Now, null,
                    $"Exceeded pressure max ({newPressure}/{_config.SpamMaxPressure}) in <#{message.Channel.Id}>");
                await _modLog.CreateModLog(context.Guild)
                    .SetContent(
                        $"<@{user.Id}> (`{user.Id}`) was silenced for exceeding pressure max ({newPressure}/{_config.SpamMaxPressure}) in <#{message.Channel.Id}>. Please investigate.{Environment.NewLine}" +
                        $"Pressure breakdown: {pressureTrace}")
                    .Send();
            }
        }
    }

    private int GetStringDifference(string oldString, string newString)
    {
        var differenceCount = 0;

        for (var i = 0; i < oldString.Length; i++)
            if (oldString[i] != newString[i])
                differenceCount += 1;

        return differenceCount;
    }

    public async Task ProcessMessageUpdate(Cacheable<IMessage, ulong> oldMessage, SocketMessage newMessage,
        ISocketMessageChannel channel, DiscordSocketClient client)
    {
        // Make sure user is from guild, message type is processable, and message is from user. if any fail, don't process;
        if (newMessage.Author is not SocketGuildUser user) return;
        if (newMessage.Type != MessageType.Default && newMessage.Type !=
            MessageType.Reply && newMessage.Type != MessageType.ThreadStarterMessage) return;
        if(newMessage is not SocketUserMessage message) return;

        SocketCommandContext context = new SocketCommandContext(client, message);

        IMessage oldMsg = await oldMessage.GetOrDownloadAsync();

        if (_config.SpamMonitorEdits)
            if (GetStringDifference(oldMsg.Content, newMessage.Content) >= _config.SpamEditReprocessThreshold)
                // Reprocess
                if (_config.SpamEnabled)
                    await ProcessPressure(user.Id, message,
                        user, context);
    }

    private async Task ProcessMessage(SocketMessage messageParam, DiscordSocketClient client)
    {
        if (messageParam.Type != MessageType.Default && messageParam.Type != MessageType.Reply &&
            messageParam.Type != MessageType.ThreadStarterMessage) return;
        SocketUserMessage message = messageParam as SocketUserMessage;
        int argPos = 0;
        SocketCommandContext context = new SocketCommandContext(client, message);
        
        var guildUser = context.User as SocketGuildUser;

        if (guildUser.Id == context.Client.CurrentUser.Id) return; // Don't process self lol
        if (_config.SpamIgnoredChannels.Contains(context.Channel.Id)) return;

        var id = guildUser.Id;
        if (!_users.ContainsKey(id)) _users.Add(id, new User());

        _users[id].Username = $"{guildUser.Username}#{guildUser.Discriminator}";
        if (!_users[id].Aliases.Contains(guildUser.Username)) _users[id].Aliases.Add(guildUser.Username);
        if (guildUser.Nickname != null)
            if (!_users[id].Aliases.Contains(guildUser.Nickname))
                _users[id].Aliases.Add(guildUser.Nickname);

        if (_config.SpamEnabled) await ProcessPressure(id, context.Message, guildUser, context);
    }
}