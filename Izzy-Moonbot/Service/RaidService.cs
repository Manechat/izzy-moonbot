using System.Linq;
using Discord;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Izzy_Moonbot.Service
{
    using Discord.Commands;
    using Discord.WebSocket;
    using Izzy_Moonbot.Helpers;
    using Izzy_Moonbot.Settings;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /*
     * The service responsible for handling antiraid functions
     * TODO: better description lol
     */
    public class RaidService
    {
        private ServerSettings _settings;
        private ModService _modService;
        private LoggingService _log;
        private ModLoggingService _modLog;
        private StateStorage _state;

        public RaidService(ServerSettings settings, ModService modService, LoggingService log, ModLoggingService modLog, StateStorage state)
        {
            _settings = settings;
            _modService = modService;
            _log = log;
            _modLog = modLog;
            _state = state;
        }

        public bool UserRecentlyJoined(ulong id)
        {
            return _state.RecentJoins.Contains(id);
        }

        public List<SocketGuildUser> GetRecentJoins(SocketCommandContext context)
        {
            List<SocketGuildUser> RecentUsers = new List<SocketGuildUser>();

            _state.RecentJoins.ForEach((userId) =>
            {
                SocketGuildUser member = context.Guild.GetUser(userId);

                if (member != null) RecentUsers.Add(member);
            });

            return RecentUsers;
        }

        public async Task SilenceRecentJoins(SocketCommandContext context)
        {
            _settings.AutoSilenceNewJoins = true;
            _settings.BatchSendLogs = true;

            var recentJoins = _state.RecentJoins.Select(recentJoin =>
            {
                var member = context.Guild.GetUser(recentJoin);

                if (member == null) return null;
                return member;
            }).Where(member => member != null).ToList();

            DateTimeOffset? muteUntil = null;
            if (_settings.SilenceTimeout.HasValue) muteUntil = DateTimeOffset.Now.AddSeconds(_settings.SilenceTimeout.Value);

            await _modService.SilenceUsers(recentJoins, DateTimeOffset.Now, muteUntil, "Suspected raider");
            
            /*_state.RecentJoins.ForEach(async (userId) =>
            {
                SocketGuildUser member = context.Guild.GetUser(userId);

                if (member != null)
                {
                    DateTimeOffset? muteUntil = null;
                    if (_settings.SilenceTimeout.HasValue) muteUntil = DateTimeOffset.Now.AddSeconds(_settings.SilenceTimeout.Value);

                    await _modService.SilenceUser(member, DateTimeOffset.Now, muteUntil, "Suspected raider");
                }
            });*/

            _state.ManualRaidSilence = true;

            await FileHelper.SaveSettingsAsync(_settings);
        }

        public async Task<List<string>> EndRaid(SocketCommandContext context)
        {
            _state.CurrentRaidMode = RaidMode.None;
            
            _settings.AutoSilenceNewJoins = false;
            _settings.BatchSendLogs = false;
            
            var userList = _state.RecentJoins.Select(userId =>
            {
                var user = context.Guild.GetUser(userId);
                if (user == null) return $"Unknown user (`{userId}`)";
                return "<@{user.Id}>";
            });

            _state.RecentJoins.ForEach((userId) =>
            {
                SocketGuildUser member = context.Guild.GetUser(userId);

                if (member != null)
                {
                    if(member.JoinedAt.HasValue)
                    {
                        if(member.JoinedAt.Value.AddSeconds(_settings.RecentJoinDecay) >= DateTimeOffset.Now)
                        {
                            _state.RecentJoins.Remove(userId);
                        }
                    }
                    else
                    {
                        // ????
                        // Just remove them lol
                        _state.RecentJoins.Remove(userId);
                    }
                }
                else
                {
                    // They got yeeted. Save on memory
                    _state.RecentJoins.Remove(userId);
                }
            });

            _state.ManualRaidSilence = false;

            await FileHelper.SaveSettingsAsync(_settings);
            return userList.ToList();
        }

        private async Task DecaySmallRaid(SocketGuild guild)
        {
            _state.CurrentRaidMode = RaidMode.None;
            
            _settings.AutoSilenceNewJoins = false;
            _settings.BatchSendLogs = false;

            var userList = _state.RecentJoins.Select(userId =>
            {
                var user = guild.GetUser(userId);
                if (user == null) return $"Unknown user (`{userId}`)";
                return "<@{user.Id}>";
            });

            var stowawaysString =
                $"{Environment.NewLine}These users were autosilenced. Please run `{_settings.Prefix}stowaways fix` while replying to this message to unsilence or `{_settings.Prefix}stowaways kick` while replying to this message to kick.{Environment.NewLine}{string.Join(", ", userList)}{Environment.NewLine}||!stowaway-usable!||";
            if (!userList.Any()) stowawaysString = "";
            
            await _modLog.CreateModLog(guild)
                .SetContent($"The raid has ended. I've disabled raid defences and cleared my internal cache of all recent joins.{stowawaysString}")
                .Send();

            _state.RecentJoins.ForEach((userId) =>
            {
                SocketGuildUser member = guild.GetUser(userId);

                if (member != null)
                {
                    if(member.JoinedAt.HasValue)
                    {
                        if(member.JoinedAt.Value.AddSeconds(_settings.RecentJoinDecay) >= DateTimeOffset.Now)
                        {
                            _state.RecentJoins.Remove(userId);
                        }
                    }
                    else
                    {
                        // ????
                        // Just remove them lol
                        _state.RecentJoins.Remove(userId);
                    }
                }
                else
                {
                    // They got yeeted. Save on memory
                    _state.RecentJoins.Remove(userId);
                }
            });

            await FileHelper.SaveSettingsAsync(_settings);
        }
        
        private async Task DecayLargeRaid(SocketGuild guild)
        {
            _state.CurrentRaidMode = RaidMode.Small;
            
            if (!_state.ManualRaidSilence) _settings.AutoSilenceNewJoins = false;
            _settings.BatchSendLogs = false;

            var manualRaidActive =
                "`.ass` was ran manually, not disabling `AutoSilenceNewJoins`. Run `.assoff` to end the raid.";
            if (!_state.ManualRaidSilence) manualRaidActive = "";
            await _modLog.CreateModLog(guild)
                .SetContent($"The raid has deescalated. I'm lowering the raid level down to Small. {manualRaidActive}")
                .Send();
            
            await FileHelper.SaveSettingsAsync(_settings);
        }

        public async Task CheckForTrip(SocketGuild guild)
        {
            if(_state.CurrentSmallJoinCount >= _settings.SmallRaidSize && _state.CurrentRaidMode == RaidMode.None)
            {
                List<string> potentialRaiders = new List<string>();
                
                _log.Log(
                    $"Small raid detected!",
                    null, false);

                _state.RecentJoins.ForEach((userId) =>
                {
                    SocketGuildUser member = guild.GetUser(userId);

                    if(member == null)
                    {
                        // We.. don't know who this user is?
                        // Well since we don't know who they are we can't silence them anyway
                        // Just mention that we couldn't find them and provide the id.
                        potentialRaiders.Add($"Unknown User (`{userId}`)");
                    }
                    else
                    {
                        string joinDate = "`Couldn't get member join time`";
                        if (member.JoinedAt.HasValue) joinDate = $"<t:{member.JoinedAt.Value.ToUnixTimeSeconds()}:F>";
                        potentialRaiders.Add($"{member.Username}#{member.Discriminator} (joined: {joinDate})");
                    }
                });

                // Potential raid. Bug the mods
                await _modLog.CreateModLog(guild)
                    .SetContent(
                        $"<@-&{_settings.ModRole}> Bing-bong! Possible raid detected! ({_settings.SmallRaidSize} (`SmallRaidSize`) users joined within {_settings.SmallRaidTime} (`SmallRaidTime`) seconds.){Environment.NewLine}{Environment.NewLine}" +
                        $"{string.Join($"{Environment.NewLine}", potentialRaiders)}{Environment.NewLine}{Environment.NewLine}" +
                        $"Possible commands for this scenario are:{Environment.NewLine}" +
                        $"`{_settings.Prefix}ass` - Enable automatically silencing new joins *and* autosilence those considered part of the raid (those who joined within {_settings.RecentJoinDecay} (`RecentJoinDecay`) seconds).{Environment.NewLine}" +
                        $"`{_settings.Prefix}assoff` - Disable automatically silencing new joins and resets the raid level back to 'no raid'. This will **not** unsilence those considered part of the raid.{Environment.NewLine}" +
                        $"`{_settings.Prefix}getraid` - Returns a list of those who are considered part of the raid by Izzy. (those who joined {_settings.RecentJoinDecay} (`RecentJoinDecay`) seconds before the raid began).{Environment.NewLine}" +
                        $"`{_settings.Prefix}banraid` - Bans everyone considered to be part of the raid. **This should be a last measure if Izzy becomes ratelimited while trying to deal with the raid. Use `getraid` to see who would be banned.**")
                    .Send();

                _state.CurrentRaidMode = RaidMode.Small;
            }
            if(_state.CurrentLargeJoinCount >= _settings.LargeRaidSize && _state.CurrentRaidMode != RaidMode.Large)
            {
                List<string> potentialRaiders = new List<string>();
                
                _log.Log(
                    $"Large raid detected!",
                    null, false);
                
                var recentJoins = _state.RecentJoins.Select(recentJoin =>
                {
                    var member = guild.GetUser(recentJoin);

                    if (member == null) return null;
                    return member;
                }).Where(member => member != null).ToList();

                DateTimeOffset? muteUntil = null;
                if (_settings.SilenceTimeout.HasValue) muteUntil = DateTimeOffset.Now.AddSeconds(_settings.SilenceTimeout.Value);

                await _modService.SilenceUsers(recentJoins, DateTimeOffset.Now, muteUntil, "Suspected raider");

                _state.RecentJoins.ForEach(async (userId) =>
                {
                    SocketGuildUser member = guild.GetUser(userId);

                    if (member == null)
                    {
                        // We.. don't know who this user is?
                        // Well since we don't know who they are we can't silence them anyway
                        // Just mention that we couldn't find them and provide the id.
                        potentialRaiders.Add($"Unknown User (`{userId}`)");
                    }
                    else
                    {
                        string joinDate = "`Couldn't get member join time`";
                        if (member.JoinedAt.HasValue) joinDate = $"<t:{member.JoinedAt.Value.ToUnixTimeSeconds()}:F>";
                        potentialRaiders.Add($"{member.Username}#{member.Discriminator} (joined: {joinDate})");

                        /*if (member.Roles.Any(role => role.Id == _settings.MemberRole))
                        {
                            DateTimeOffset? muteUntil = null;
                            if (_settings.SilenceTimeout.HasValue)
                                muteUntil = DateTimeOffset.Now.AddSeconds(_settings.SilenceTimeout.Value);

                            await _modService.SilenceUser(member, DateTimeOffset.Now, muteUntil, "Suspected raider");
                        }*/
                    }
                });

                if (_state.CurrentRaidMode == RaidMode.None)
                {
                    await _modLog.CreateModLog(guild)
                        .SetContent(
                            $"<@-&{_settings.ModRole}> Bing-bong! Raid detected! ({_settings.LargeRaidSize} (`LargeRaidSize`) users joined within {_settings.LargeRaidTime} (`LargeRaidTime`) seconds.){Environment.NewLine}" +
                            $"I have automatically silenced all the members below members and enabled autosilencing users on join.{Environment.NewLine}{Environment.NewLine}" +
                            $"{string.Join($"{Environment.NewLine}", potentialRaiders)}{Environment.NewLine}{Environment.NewLine}" +
                            $"Possible commands for this scenario are:{Environment.NewLine}" +
                            $"`{_settings.Prefix}assoff` - Disable automatically silencing new joins and resets the raid level back to 'no raid'.. This will **not** unsilence those considered part of the raid.{Environment.NewLine}" +
                            $"`{_settings.Prefix}getraid` - Returns a list of those who are considered part of the raid by Izzy. (those who joined within {_settings.RecentJoinDecay} (`RecentJoinDecay`) seconds).{Environment.NewLine}" +
                            $"`{_settings.Prefix}banraid` - Bans everyone considered to be part of the raid. **This should be a last measure if Izzy becomes ratelimited while trying to deal with the raid. Use `getraid` to see who would be banned.**")
                        .Send();
                }
                else
                {
                    if (_settings.AutoSilenceNewJoins)
                    {
                        await _modLog.CreateModLog(guild)
                            .SetContent(
                                $"<@-&{_settings.ModRole}> **The current raid has escalated. Silencing new joins has already been enabled manually.** ({_settings.LargeRaidSize} (`LargeRaidSize`) users joined within {_settings.LargeRaidTime} (`LargeRaidTime`) seconds.)")
                            .Send();
                    }
                    else
                    {
                        await _modLog.CreateModLog(guild)
                            .SetContent(
                                $"<@-&{_settings.ModRole}> **The current raid has escalated and I have automatically enabled silencing new joins and I've silenced those considered part of the raid.** ({_settings.LargeRaidSize} (`LargeRaidSize`) users joined within {_settings.LargeRaidTime} (`LargeRaidTime`) seconds.)")
                            .Send();
                    }
                }

                _state.CurrentRaidMode = RaidMode.Large;
                _settings.AutoSilenceNewJoins = true;

                await FileHelper.SaveSettingsAsync(_settings);
            }
        }

        public async Task ProcessMemberJoin(SocketGuildUser member)
        {
            if (!_settings.RaidProtectionEnabled) return;
            if (!UserRecentlyJoined(member.Id))
            {
                _state.CurrentSmallJoinCount++;
                await _log.Log(
                    $"Small raid join count raised for {member.DisplayName} ({member.Id}). Now at {_state.CurrentSmallJoinCount}/{_settings.SmallRaidSize} for {_settings.SmallRaidTime} seconds.",
                    null, level: LogLevel.Debug);
                _state.CurrentLargeJoinCount++;
                await _log.Log(
                    $"Large raid join count raised for {member.DisplayName} ({member.Id}). Now at {_state.CurrentLargeJoinCount}/{_settings.LargeRaidSize} for {_settings.LargeRaidTime} seconds.",
                    null, level: LogLevel.Debug);

                _state.RecentJoins.Add(member.Id);

                await _log.Log(
                    $"Recent join: {member.DisplayName} ({member.Id}), No longer considered recent join in {_settings.RecentJoinDecay} seconds.",
                    null, false);

                await CheckForTrip(member.Guild);

                Task.Factory.StartNew(async() =>
                {
                    await Task.Delay(Convert.ToInt32(_settings.SmallRaidTime * 1000));
                    _state.CurrentSmallJoinCount--;
                    await _log.Log(
                        $"Small raid join count dropped for {member.DisplayName} ({member.Id}). Now at {_state.CurrentSmallJoinCount}./{_settings.SmallRaidSize} after {_settings.SmallRaidTime} seconds.",
                        null, level: LogLevel.Debug);
                });

                Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(Convert.ToInt32(_settings.LargeRaidTime * 1000));
                    _state.CurrentLargeJoinCount--;
                    await _log.Log(
                        $"Large raid join count dropped for {member.DisplayName} ({member.Id}). Now at {_state.CurrentLargeJoinCount}/{_settings.LargeRaidSize} after {_settings.LargeRaidTime} seconds.",
                        null, level: LogLevel.Debug);
                });

                Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(Convert.ToInt32(_settings.RecentJoinDecay * 1000));
                    if (_state.CurrentRaidMode == RaidMode.None)
                    {
                        await _log.Log(
                            $"{member.DisplayName} ({member.Id}) no longer a recent join",
                            null);
                        _state.RecentJoins.Remove(member.Id);
                    }
                });
                if (_settings.SmallRaidDecay != null)
                    Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(Convert.ToInt32(_settings.SmallRaidDecay * 60 * 1000));
                        if (_settings.SmallRaidDecay == null) return; // Was disabled
                        if (_state.CurrentRaidMode != RaidMode.Small) return; // Not a small raid
                        if (_state.CurrentSmallJoinCount >= _settings.SmallRaidSize)
                            return; // Small raid join count is still ongoing.
                        if (_state.ManualRaidSilence) return; // This raid was manually silenced. Don't decay.

                        await _log.Log("Decaying raid: Small -> None", null, level: LogLevel.Debug);
                        await DecaySmallRaid(member.Guild);
                    });
                if (_settings.LargeRaidDecay != null)
                    Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(Convert.ToInt32(_settings.LargeRaidDecay * 60 * 1000));
                        if (_settings.SmallRaidDecay == null) return; // Was disabled
                        if (_state.CurrentRaidMode != RaidMode.Large) return; // Not a large raid
                        if (_state.CurrentLargeJoinCount >= _settings.LargeRaidSize)
                            return; // Small raid join count is still ongoing.

                        await _log.Log("Decaying raid: Large -> Small", null, level: LogLevel.Debug);
                        await DecayLargeRaid(member.Guild);
                    });
            }
            else
            {
                _log.Log(
                    $"{member.DisplayName}#{member.Discriminator} ({member.Id}) rejoined while still considered a recent join. Not calculating additional raid pressure.",
                    null);
            }
        }
    }

    public enum RaidMode
    {
        None,
        Small,
        Large
    }
}
