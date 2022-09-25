using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
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
    public double GetPressure(ulong id)
    {
        // Just return the user's pressure
        return _users[id].Pressure;
    }

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
        await FileHelper.SaveUsersAsync(_users);

        // Return pressure
        return pressure;
    }

    public async Task<double> IncreasePressure(ulong id, double pressure)
    {
        // Get current time and pressure (also execute pressure decay)
        var now = DateTimeOffset.UtcNow;
        var currentPressure = await GetAndDecayPressure(id);
        
        // Increase pressure
        currentPressure += pressure;

        // Save user
        _users[id].Pressure = currentPressure;
        _users[id].Timestamp = now;
        await FileHelper.SaveUsersAsync(_users);

        // Return new pressure
        return currentPressure;
    }

    private async Task ProcessPressure(ulong id, SocketUserMessage message, SocketGuildUser user,
        SocketCommandContext context)
    {
        // Get the base pressure and create the pressureTracer
        var pressure = _config.SpamBasePressure;
        var pressureTracer = new Dictionary<string, double>{ {"Base", _config.SpamBasePressure} };
        
        // Length pressure
        pressure += _config.SpamLengthPressure * message.Content.Length;
        pressureTracer.Add("Length", _config.SpamLengthPressure * message.Content.Length);
        
        // Line pressure
        pressure += _config.SpamLengthPressure * (message.Content.Split("\n").Length - 1);
        pressureTracer.Add("Lines", _config.SpamLengthPressure * (message.Content.Split("\n").Length - 1));

        // Attachments, embeds, and stickers count as Image pressure
        if (message.Attachments.Count >= 1 || message.Embeds.Count >= 1 || message.Stickers.Count >= 1)
        {
            // Register pressure increase and add increase to the pressure tracer
            pressure += _config.SpamImagePressure *
                        (message.Attachments.Count + message.Embeds.Count + message.Stickers.Count);
            pressureTracer.Add("Embeds", _config.SpamImagePressure *
                               (message.Attachments.Count + message.Embeds.Count + message.Stickers.Count));
        }

        // Check if there's at least one url in the message (and there's no embeds)
        if (_url.IsMatch(message.Content) && message.Embeds.Count == 0)
        {
            // Because url pressure can occur multiple times, we store the pressure to add here
            var pressureToAdd = 0.0;
            
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

                // Go through each match
                foreach (var match in matches)
                {
                    // Check if this URL will embed
                    try
                    {
                        var willEmbed = DiscordHelper.WouldUrlEmbed(match.Value);
                        await _logger.Log($"{match.Value} = {willEmbed}");

                        // If it will, add Image pressure
                        if (willEmbed) pressureToAdd += _config.SpamImagePressure;
                    }
                    catch (Exception exception)
                    {
                        // Somewhere, something went wrong. Report the error but assume not embedding and continue.
                        await _logger.Log($"Exception occured while processing whether a link has an embed. Assuming no embed.", level: LogLevel.Warning);
                        await _logger.Log($"URL Trigger: {match.Value}", level: LogLevel.Warning);
                        await _logger.Log($"Message: {exception.Message}", level: LogLevel.Warning);
                        await _logger.Log($"Source: {exception.Source}", level: LogLevel.Warning);
                        await _logger.Log($"Method: {exception.TargetSite}", level: LogLevel.Warning);
                        await _logger.Log($"Stack Trace: {exception.StackTrace}", level: LogLevel.Warning);
                    }
                }
            }

            // Check if the pressure we need to add is above 0 because no point adding 0 pressure
            if (pressureToAdd > 0.0)
            {
                // It is, increase pressure and add pressure trace
                pressure += pressureToAdd;
                pressureTracer.Add("URLs", pressureToAdd);
            }
        }

        // Mention pressure
        if (_mention.IsMatch(message.Content))
        {
            pressure += _config.SpamPingPressure * _mention.Matches(message.Content).Count;
            pressureTracer.Add("Mention", _config.SpamPingPressure * _mention.Matches(message.Content).Count);
        }

        // Repeat pressure
        if (message.CleanContent.ToLower() == _users[id].PreviousMessage.ToLower())
        {
            pressure += _config.SpamRepeatPressure;
            pressureTracer.Add("Repeat", _config.SpamRepeatPressure);
        }
        
        #if DEBUG
        // Test string, only exists when built on Debug.
        if (message.Content == _testString)
        {
            pressure = _config.SpamMaxPressure;
            pressureTracer = new Dictionary<string, double>() { { "Test string", _config.SpamMaxPressure } };
        }
        #endif

        _users[id].PreviousMessage = context.Message.CleanContent;
        await FileHelper.SaveUsersAsync(_users);
        
        var newPressure = await IncreasePressure(id, pressure);

        await _logger.Log($"Pressure increase by {pressure} to {newPressure}/{_config.SpamMaxPressure}.{Environment.NewLine}                          Pressure trace: {string.Join(", ", pressureTracer)}", context, level: LogLevel.Debug);

        if (newPressure >= _config.SpamMaxPressure)
        {
            await _logger.Log("Spam pressure trip, checking whether user should be silenced or not...", context, level: LogLevel.Debug);
            var roleIds = user.Roles.Select(roles => roles.Id).ToList();
            if (_config.SpamBypassRoles.Overlaps(roleIds))
            {
                // User has a role which bypasses the punishment of spam trigger. Mention it in action log.
                await _logger.Log("No silence, user has role(s) in Config.SpamBypassRoles", context, level: LogLevel.Debug);
                
                await _modLogger.CreateActionLog(context.Guild)
                    .AddTarget(user)
                    .SetReason(
                        $"Exceeded pressure max ({newPressure}/{_config.SpamMaxPressure}) in <#{message.Channel.Id}>. Didn't silence as they have a role which bypasses punishment (`SpamBypassRoles`).")
                    .Send();
            }
            else
            {
                // User is not immune to spam punishments, process trip.
                await _logger.Log("Silence, executing trip method.", context, level: LogLevel.Debug);
                await ProcessTrip(id, newPressure, pressureTracer, message, user, context);
            }
        }
    }

    private async Task ProcessTrip(ulong id, double pressure, Dictionary<string, double> pressureTracer, 
        SocketUserMessage message, SocketGuildUser user, SocketCommandContext context)
    {
        // Silence user, this also logs the action.
        await _mod.SilenceUser(user, DateTimeOffset.UtcNow, null, $"Exceeded pressure max ({pressure}/{_config.SpamMaxPressure}) in <#{message.Channel.Id}>");

        await _modLogger.CreateModLog(context.Guild)
            .SetContent(
                $"<@{user.Id}> (`{user.Id}`) was silenced for exceeding pressure max ({pressure}/{_config.SpamMaxPressure}) in <#{message.Channel.Id}>. Please investigate.{Environment.NewLine}" +
                $"Pressure breakdown: {PressureTraceToPonyReadable(pressureTracer)}")
            .Send();
    }

    private string PressureTraceToPonyReadable(Dictionary<string, double> pressureTracer)
    {
        var output = new List<string>();
        foreach (var (key, value) in pressureTracer)
        {
            output.Add($"{key}: {value}");
        }

        return string.Join(", ", output);
    }

    private async Task MessageReceiveEvent(SocketMessage messageParam, DiscordSocketClient client)
    {
        if (!_config.SpamEnabled) return; // anti-spam is off
        if (!DiscordHelper.IsInGuild(messageParam)) return; // Not in guild (in dm/group)
        if (!DiscordHelper.IsProcessableMessage(messageParam)) return; // Not processable
        if (messageParam is not SocketUserMessage message) return; // Not processable
        
        if (_config.ThreadOnlyMode &&
            (message.Channel.GetChannelType() != ChannelType.PublicThread &&
             message.Channel.GetChannelType() != ChannelType.PrivateThread)) return; // Not a thread, in thread only mode

        var context = new SocketCommandContext(client, message);
        var guildUser = context.User as SocketGuildUser;

        if (guildUser.Id == client.CurrentUser.Id) return; // Don't process the bot
        if (_config.SpamIgnoredChannels.Contains(context.Channel.Id)) return; // Don't process ignored channels

        await ProcessPressure(guildUser.Id, context.Message, guildUser, context);
    }
}