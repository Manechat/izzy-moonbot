using System.Linq;
using System.Text.RegularExpressions;
using Discord;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Izzy_Moonbot.Service
{
    using Izzy_Moonbot.Settings;
    using Izzy_Moonbot.Helpers;

    using Discord.WebSocket;

    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Discord.Commands;

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
        private ModLoggingService _modLog;
        private ServerSettings _settings;
        private ModService _mod;
        private Dictionary<ulong, User> _users;
        private readonly Regex _mention = new Regex("<@&?[0-9]+>");
        private readonly Regex _url = new Regex("https?://(.+\\.)*(.+)\\.([A-z]+)(/?.+)*", RegexOptions.IgnoreCase);
        private readonly Regex _noEmbedUrl = new Regex("<{1}https?://(.+\\.)*(.+)\\.([A-z]+)(/?.+)*>{1}", RegexOptions.IgnoreCase);
        
        private readonly string _testString = "=+i7B3s+#(-{×jn6Ga3F~lA:IZZY_PRESSURE_TEST:H4fgd3!#!";

        public PressureService(LoggingService logger, ModLoggingService modLog, ServerSettings settings, ModService mod, Dictionary<ulong, User> users)
        {
            _logger = logger;
            _modLog = modLog;
            _settings = settings;
            _mod = mod;
            _users = users;
        }

        /// <summary>
        /// Get the pressure of a given user by their Discord id.
        /// </summary>
        /// <param name="id">The Discord id of the user to get pressure for.</param>
        /// <returns>A <c>double</c> of the pressure. If the result is <c>-1</c> then the user hasn't been processed yet.</returns>
        public async Task<double> GetPressure(ulong id)
        {
            // If that user hasn't been processed yet, just return -1.
            if (!_users.ContainsKey(id)) return -1;

            DateTime now = DateTime.UtcNow;
            double pressureLossPerSecond = _settings.SpamBasePressure / _settings.SpamPressureDecay;
            double pressure = _users[id].Pressure;
            TimeSpan difference = now - _users[id].Timestamp;
            double totalPressureLoss = difference.TotalSeconds * pressureLossPerSecond;
            pressure -= totalPressureLoss;
            if (pressure < 0)
            {
                pressure = 0;
            }

            _users[id].Pressure = pressure;
            _users[id].Timestamp = now;
            await FileHelper.SaveUsersAsync(_users);

            return pressure;
        }
        
        /// <summary>
        /// Get the pressure of a given user by their Discord id, without preforming pressure decay calculations.
        /// </summary>
        /// <param name="id">The Discord id of the user to get pressure for.</param>
        /// <returns>A <c>double</c> of the pressure. If the result is <c>-1</c> then the user hasn't been processed yet.</returns>
        public async Task<double> GetPressureWithoutModifying(ulong id)
        {
            // If that user hasn't been processed yet, just return -1.
            if (!_users.ContainsKey(id)) return -1;

            double pressure = _users[id].Pressure;
            
            if (pressure < 0)
            {
                pressure = 0;
            }
            
            return pressure;
        }

        /// <summary>
        /// Increase the pressure of a user by their Discord id.
        /// </summary>
        /// <param name="id">The Discord id of the user to increase pressure for.</param>
        /// <param name="pressure">The pressure to add onto the current pressure.</param>
        /// <returns>A <c>double</c> of the new pressure.</returns>
        public async Task<double> IncreasePressure(ulong id, double pressure)
        {
            DateTime now = DateTime.UtcNow;
            double currentPressure = await GetPressure(id);
            currentPressure += pressure;
            _users[id].Pressure = currentPressure;
            _users[id].Timestamp = now;

            await FileHelper.SaveUsersAsync(_users);

            return currentPressure;
        }

        private async Task ProcessPressure(ulong id, SocketUserMessage message, SocketGuildUser user, SocketCommandContext context)
        {
            // This doesn't do much currently, but it will once we add image checks and the like
            double pressure = _settings.SpamBasePressure;
            string pressureTrace = $"{_settings.SpamBasePressure} base, ";

            if (message.Attachments.Count >= 1 || message.Embeds.Count >= 1 || message.Stickers.Count >= 1)
            {
                // Image pressure.
                pressure += _settings.SpamImagePressure * (message.Attachments.Count + message.Embeds.Count + message.Stickers.Count);
                pressureTrace +=
                    $"{_settings.SpamImagePressure * message.Attachments.Count} attachments, {_settings.SpamImagePressure * message.Embeds.Count} embeds, {_settings.SpamImagePressure * message.Stickers.Count} stickers, ";
            }

            if (_url.IsMatch(message.Content) && !_noEmbedUrl.IsMatch(message.Content) && message.Embeds.Count == 0)
            {
                // No attempt to suppress embed made
                pressure += _settings.SpamImagePressure * _url.Matches(message.Content).Count;
                pressureTrace += $"{_settings.SpamImagePressure * _url.Matches(message.Content).Count} URLs, ";
            }

            if (this._mention.IsMatch(message.Content))
            {
                pressure += _settings.SpamPingPressure * this._mention.Matches(message.Content).Count;
                pressureTrace +=
                    $"{_settings.SpamPingPressure * this._mention.Matches(message.Content).Count} mentions, ";
            }

            if (message.Content.ToLower() == _users[id].PreviousMessage.ToLower())
            {
                pressure += _settings.SpamRepeatPressure;
                pressureTrace += $"{_settings.SpamRepeatPressure} repeat, ";
            }

            pressure += _settings.SpamLengthPressure * message.Content.Length;
            pressureTrace += $"{_settings.SpamLengthPressure * message.Content.Length} length, ";

            pressure += _settings.SpamLinePressure * (message.Content.Replace("\r\n", "\n").Split("\n").Length-1);
            pressureTrace += $"{_settings.SpamLinePressure * (message.Content.Replace("\r\n", "\n").Split("\n").Length-1)} lines.";

            if (message.Content == this._testString)
            {
                // Test string for pressure.
                pressure = _settings.SpamMaxPressure + 1;
                pressureTrace = $"{_settings.SpamMaxPressure + 1} teststring.";
            }
            
            double newPressure = await IncreasePressure(id, pressure);

            _users[message.Author.Id].PreviousMessage = message.Content.ToLower();
            FileHelper.SaveUsersAsync(_users);

            await _logger.Log($"Pressure increased by {pressure} to {newPressure}/{_settings.SpamMaxPressure}", context, level: LogLevel.Debug);
            await _logger.Log(pressureTrace, null, level: LogLevel.Trace);
            
            if (newPressure >= _settings.SpamMaxPressure)
            {
                List<ulong> roleIds = user.Roles.Select(role => role.Id).ToList();
                if (_settings.SpamBypassRoles.Overlaps(roleIds))
                {
                    await _logger.Log("Spam pressure trip, user has role in SpamBypassRoles", context, level: LogLevel.Trace);
                    // Don't silence, but inform mods.
                    await _modLog.CreateActionLog(context.Guild)
                        .SetTarget(message.Author as SocketGuildUser)
                        .SetReason(
                            $"Exceeded pressure max ({newPressure}/{_settings.SpamMaxPressure}) in <#{message.Channel.Id}>. Didn't silence as they have a role which bypasses punishment (`SpamBypassRoles`).")
                        .Send();
                }
                else
                {
                    await _logger.Log("Spam pressure trip, trying to bap", context, level: LogLevel.Trace);
                    // Silence user.
                    await _mod.SilenceUser((message.Author as SocketGuildUser), DateTimeOffset.Now, null, $"Exceeded pressure max ({newPressure}/{_settings.SpamMaxPressure}) in <#{message.Channel.Id}>");
                    await _modLog.CreateModLog(context.Guild)
                        .SetContent(
                            $"<@{user.Id}> (`{user.Id}`) was silenced for exceeding pressure max ({newPressure}/{_settings.SpamMaxPressure}) in <#{message.Channel.Id}>. Please investigate.{Environment.NewLine}"+
                            $"Pressure breakdown: {pressureTrace}")
                        .Send();
                }
            }
        }

        private int GetStringDifference(string oldString, string newString)
        {
            int differenceCount = 0;
            
            for (var i = 0; i < oldString.Length; i++)
            {
                if (oldString[i] != newString[i]) differenceCount += 1;
            }

            return differenceCount;
        }

        public async Task ProcessMessageUpdate(IMessage oldMessage, SocketMessage newMessage)
        {
            if (_settings.SpamMonitorEdits)
            {
                if (this.GetStringDifference(oldMessage.Content, newMessage.Content) >= _settings.SpamEditReprocessThreshold)
                {
                    // Reprocess
                    if(_settings.SpamEnabled) await ProcessPressure(newMessage.Author.Id, newMessage as SocketUserMessage, newMessage.Author as SocketGuildUser, null);
                }
            }
        }

        public async Task ProcessMessage(SocketCommandContext context)
        {
            SocketGuildUser guildUser = context.User as SocketGuildUser;

            if (guildUser.Id == context.Client.CurrentUser.Id) return; // Don't process self lol
            if (_settings.SpamIgnoredChannels.Contains(context.Channel.Id)) return;

            ulong id = guildUser.Id;
            if (!_users.ContainsKey(id))
            {
                _users.Add(id, new User());
            }

            _users[id].Username = $"{guildUser.Username}#{guildUser.Discriminator}";
            if (!_users[id].Aliases.Contains(guildUser.Username))
            {
                _users[id].Aliases.Add(guildUser.Username);
            }
            if (guildUser.Nickname != null)
            {

                if (!_users[id].Aliases.Contains(guildUser.Nickname))
                {
                    _users[id].Aliases.Add(guildUser.Nickname);
                }
            }
            
            if(_settings.SpamEnabled) await ProcessPressure(id, context.Message, guildUser, context);
        }
    }
}