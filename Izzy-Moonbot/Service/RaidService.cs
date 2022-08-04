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

        private List<ulong> RecentJoins = new List<ulong>();

        private int CurrentSmallJoinCount = 0;
        private int CurrentLargeJoinCount = 0;

        private bool ManualRaidSilence = false;

        public RaidMode CurrentRaidMode = RaidMode.None;

        public RaidService(ServerSettings settings, ModService modService, LoggingService log, ModLoggingService modLog)
        {
            _settings = settings;
            _modService = modService;
            _log = log;
            _modLog = modLog;
        }

        public bool UserRecentlyJoined(ulong id)
        {
            return RecentJoins.Contains(id);
        }

        public List<SocketGuildUser> GetRecentJoins(SocketCommandContext context)
        {
            List<SocketGuildUser> RecentUsers = new List<SocketGuildUser>();

            RecentJoins.ForEach((userId) =>
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
            
            RecentJoins.ForEach(async (userId) =>
            {
                SocketGuildUser member = context.Guild.GetUser(userId);

                if (member != null)
                {
                    DateTimeOffset? muteUntil = null;
                    if (_settings.SilenceTimeout.HasValue) muteUntil = DateTimeOffset.Now.AddSeconds(_settings.SilenceTimeout.Value);

                    await _modService.SilenceUser(member, DateTimeOffset.Now, muteUntil, "Suspected raider");
                }
            });

            ManualRaidSilence = true;

            await FileHelper.SaveSettingsAsync(_settings);
        }

        public async Task EndRaid(SocketCommandContext context)
        {
            CurrentRaidMode = RaidMode.None;
            
            _settings.AutoSilenceNewJoins = false;
            _settings.BatchSendLogs = false;

            RecentJoins.ForEach((userId) =>
            {
                SocketGuildUser member = context.Guild.GetUser(userId);

                if (member != null)
                {
                    if(member.JoinedAt.HasValue)
                    {
                        if(member.JoinedAt.Value.AddSeconds(_settings.RecentJoinDecay) >= DateTimeOffset.Now)
                        {
                            RecentJoins.Remove(userId);
                        }
                    }
                    else
                    {
                        // ????
                        // Just remove them lol
                        RecentJoins.Remove(userId);
                    }
                }
                else
                {
                    // They got yeeted. Save on memory
                    RecentJoins.Remove(userId);
                }
            });

            ManualRaidSilence = false;
            if (_settings.NormalVerificationLevel != null && _settings.RaidVerificationLevel != null)
            {
                bool raidResult = Enum.TryParse(_settings.RaidVerificationLevel.Value.ToString(), out VerificationLevel raidLevel);
                bool normalResult = Enum.TryParse(_settings.NormalVerificationLevel.Value.ToString(), out VerificationLevel normalLevel);
                if (!raidResult || !normalResult) return;
                await _modService.ChangeVerificationLevel(context.Guild, normalLevel, DateTimeOffset.Now, null, "Raid was ended manually (`.assoff`).");
            }

            await FileHelper.SaveSettingsAsync(_settings);
        }

        private async Task DecaySmallRaid(SocketGuild guild)
        {
            CurrentRaidMode = RaidMode.None;
            
            _settings.AutoSilenceNewJoins = false;
            _settings.BatchSendLogs = false;
            
            await _modLog.CreateModLog(guild)
                .SetContent($"The raid has ended. I've disabled raid defences and cleared my internal cache of all recent joins.")
                .Send();

            RecentJoins.ForEach((userId) =>
            {
                SocketGuildUser member = guild.GetUser(userId);

                if (member != null)
                {
                    if(member.JoinedAt.HasValue)
                    {
                        if(member.JoinedAt.Value.AddSeconds(_settings.RecentJoinDecay) >= DateTimeOffset.Now)
                        {
                            RecentJoins.Remove(userId);
                        }
                    }
                    else
                    {
                        // ????
                        // Just remove them lol
                        RecentJoins.Remove(userId);
                    }
                }
                else
                {
                    // They got yeeted. Save on memory
                    RecentJoins.Remove(userId);
                }
            });
            
            if (_settings.NormalVerificationLevel != null && _settings.RaidVerificationLevel != null)
            {
                bool raidResult = Enum.TryParse(_settings.RaidVerificationLevel.Value.ToString(), out VerificationLevel raidLevel);
                bool normalResult = Enum.TryParse(_settings.NormalVerificationLevel.Value.ToString(), out VerificationLevel normalLevel);
                if (!raidResult || !normalResult) return;
                await _modService.ChangeVerificationLevel(guild, normalLevel, DateTimeOffset.Now, null, "Raid has ended.");
            }

            await FileHelper.SaveSettingsAsync(_settings);
        }
        
        private async Task DecayLargeRaid(SocketGuild guild)
        {
            CurrentRaidMode = RaidMode.Small;

            if(!ManualRaidSilence) _settings.AutoSilenceNewJoins = false;
            _settings.BatchSendLogs = false;

            var manualRaidActive =
                "`.ass` was ran manually, not disabling `AutoSilenceNewJoins`. Run `.assoff` to end the raid.";
            if (!ManualRaidSilence) manualRaidActive = "";
            await _modLog.CreateModLog(guild)
                .SetContent($"The raid has deescalated. I'm lowering the raid level down to Small. {manualRaidActive}")
                .Send();
            
            await FileHelper.SaveSettingsAsync(_settings);
        }

        public async Task CheckForTrip(SocketGuild guild)
        {
            if(CurrentSmallJoinCount >= _settings.SmallRaidSize && CurrentRaidMode == RaidMode.None)
            {
                List<string> potentialRaiders = new List<string>();
                
                _log.Log(
                    $"Small raid detected!",
                    null, false);

                RecentJoins.ForEach((userId) =>
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
                
                var verificationLevelRaised = "";
                if (_settings.NormalVerificationLevel != null && _settings.RaidVerificationLevel != null)
                {
                    bool raidResult = Enum.TryParse(_settings.RaidVerificationLevel.Value.ToString(), out VerificationLevel raidLevel);
                    bool normalResult = Enum.TryParse(_settings.NormalVerificationLevel.Value.ToString(), out VerificationLevel normalLevel);
                    if (!raidResult || !normalResult) return;
                    await _modService.ChangeVerificationLevel(guild, raidLevel, DateTimeOffset.Now, null, "Server is being raided.");
                    verificationLevelRaised =
                        $"{Environment.NewLine}{Environment.NewLine}*I have raised the server verification level to {raidLevel.ToString()}. This will be lowered down to {normalLevel.ToString()} when the raid ends.";
                }

                // Potential raid. Bug the mods
                await _modLog.CreateModLog(guild)
                    .SetContent(
                        $"<@-&{_settings.ModRole}> Bing-bong! Possible raid detected! (over {_settings.SmallRaidSize} (`SmallRaidSize`) users joined within {_settings.SmallRaidTime} (`SmallRaidTime`) seconds.){Environment.NewLine}{Environment.NewLine}" +
                        $"{string.Join($"{Environment.NewLine}", potentialRaiders)}{Environment.NewLine}{Environment.NewLine}" +
                        $"Possible commands for this scenario are:{Environment.NewLine}" +
                        $"`{_settings.Prefix}ass` - Enable automatically silencing new joins *and* autosilence those considered part of the raid (those who joined within {_settings.RecentJoinDecay} (`RecentJoinDecay`) seconds).{Environment.NewLine}" +
                        $"`{_settings.Prefix}assoff` - Disable automatically silencing new joins and resets the raid level back to 'no raid'. This will **not** unsilence those considered part of the raid.{Environment.NewLine}" +
                        $"`{_settings.Prefix}getraid` - Returns a list of those who are considered part of the raid by Izzy. (those who joined {_settings.RecentJoinDecay} (`RecentJoinDecay`) seconds before the raid began).{Environment.NewLine}" +
                        $"`{_settings.Prefix}banraid` - Bans everyone considered to be part of the raid. **This should be a last measure if Izzy becomes ratelimited while trying to deal with the raid. Use `getraid` to see who would be banned.**"+
                        $"{verificationLevelRaised}")
                    .Send();

                CurrentRaidMode = RaidMode.Small;
            }
            if(CurrentLargeJoinCount >= _settings.LargeRaidSize && CurrentRaidMode != RaidMode.Large)
            {
                List<string> potentialRaiders = new List<string>();
                
                _log.Log(
                    $"Large raid detected!",
                    null, false);

                RecentJoins.ForEach(async (userId) =>
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

                        if (member.Roles.Any(role => role.Id == _settings.MemberRole))
                        {
                            DateTimeOffset? muteUntil = null;
                            if (_settings.SilenceTimeout.HasValue)
                                muteUntil = DateTimeOffset.Now.AddSeconds(_settings.SilenceTimeout.Value);

                            await _modService.SilenceUser(member, DateTimeOffset.Now, muteUntil, "Suspected raider");
                        }
                    }
                });

                if (CurrentRaidMode == RaidMode.None)
                {
                    var verificationLevelRaised = "";
                    if (_settings.NormalVerificationLevel != null && _settings.RaidVerificationLevel != null)
                    {
                        bool raidResult = Enum.TryParse(_settings.RaidVerificationLevel.Value.ToString(), out VerificationLevel raidLevel);
                        bool normalResult = Enum.TryParse(_settings.NormalVerificationLevel.Value.ToString(), out VerificationLevel normalLevel);
                        if (!raidResult || !normalResult) return;
                        await _modService.ChangeVerificationLevel(guild, raidLevel, DateTimeOffset.Now, null, "Server is being raided.");
                        verificationLevelRaised =
                            $"{Environment.NewLine}{Environment.NewLine}*I have raised the server verification level to {raidLevel.ToString()}. This will be lowered down to {normalLevel.ToString()} when the raid ends.";
                    }
                    
                    await _modLog.CreateModLog(guild)
                        .SetContent(
                            $"<@-&{_settings.ModRole}> Bing-bong! Raid detected! (over {_settings.LargeRaidSize} (`LargeRaidSize`) users joined within {_settings.LargeRaidTime} (`LargeRaidTime`) seconds.){Environment.NewLine}" +
                            $"I have automatically silenced all the members below members and enabled autosilencing users on join.{Environment.NewLine}{Environment.NewLine}" +
                            $"{string.Join($"{Environment.NewLine}", potentialRaiders)}{Environment.NewLine}{Environment.NewLine}" +
                            $"Possible commands for this scenario are:{Environment.NewLine}" +
                            $"`{_settings.Prefix}assoff` - Disable automatically silencing new joins and resets the raid level back to 'no raid'.. This will **not** unsilence those considered part of the raid.{Environment.NewLine}" +
                            $"`{_settings.Prefix}getraid` - Returns a list of those who are considered part of the raid by Izzy. (those who joined within {_settings.RecentJoinDecay} (`RecentJoinDecay`) seconds).{Environment.NewLine}" +
                            $"`{_settings.Prefix}banraid` - Bans everyone considered to be part of the raid. **This should be a last measure if Izzy becomes ratelimited while trying to deal with the raid. Use `getraid` to see who would be banned.**"+
                            $"{verificationLevelRaised}")
                        .Send();
                }
                else
                {
                    if (_settings.AutoSilenceNewJoins)
                    {
                        await _modLog.CreateModLog(guild)
                            .SetContent(
                                $"<@-&{_settings.ModRole}> **The current raid has escalated. Silencing new joins has already been enabled manually.** (over {_settings.LargeRaidSize} (`LargeRaidSize`) users joined within {_settings.LargeRaidTime} (`LargeRaidTime`) seconds.)")
                            .Send();
                    }
                    else
                    {
                        await _modLog.CreateModLog(guild)
                            .SetContent(
                                $"<@-&{_settings.ModRole}> **The current raid has escalated and I have automatically enabled silencing new joins and I've silenced those considered part of the raid.** (over {_settings.LargeRaidSize} (`LargeRaidSize`) users joined within {_settings.LargeRaidTime} (`LargeRaidTime`) seconds.)")
                            .Send();
                    }
                }

                CurrentRaidMode = RaidMode.Large;
                _settings.AutoSilenceNewJoins = true;

                await FileHelper.SaveSettingsAsync(_settings);
            }
        }

        public async Task ProcessMemberJoin(SocketGuildUser member)
        {
            if (!_settings.RaidProtectionEnabled) return;
            if (!this.UserRecentlyJoined(member.Id))
            {
                CurrentSmallJoinCount++;
                await _log.Log(
                    $"Small raid join count raised for {member.DisplayName} ({member.Id}). Now at {CurrentSmallJoinCount}/{_settings.SmallRaidSize} for {_settings.SmallRaidTime} seconds.",
                    null, level: LogLevel.Debug);
                CurrentLargeJoinCount++;
                await _log.Log(
                    $"Large raid join count raised for {member.DisplayName} ({member.Id}). Now at {CurrentLargeJoinCount}/{_settings.LargeRaidSize} for {_settings.LargeRaidTime} seconds.",
                    null, level: LogLevel.Debug);

                RecentJoins.Add(member.Id);

                await _log.Log(
                    $"Recent join: {member.DisplayName} ({member.Id}), No longer considered recent join in {_settings.RecentJoinDecay} seconds.",
                    null, false);

                await CheckForTrip(member.Guild);

                Task.Factory.StartNew(async() =>
                {
                    await Task.Delay(Convert.ToInt32(_settings.SmallRaidTime * 1000));
                    CurrentSmallJoinCount--;
                    await _log.Log(
                        $"Small raid join count dropped for {member.DisplayName} ({member.Id}). Now at {CurrentSmallJoinCount}./{_settings.SmallRaidSize} after {_settings.SmallRaidTime} seconds.",
                        null, level: LogLevel.Debug);
                });

                Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(Convert.ToInt32(_settings.LargeRaidTime * 1000));
                    CurrentLargeJoinCount--;
                    await _log.Log(
                        $"Large raid join count dropped for {member.DisplayName} ({member.Id}). Now at {CurrentLargeJoinCount}/{_settings.LargeRaidSize} after {_settings.LargeRaidTime} seconds.",
                        null, level: LogLevel.Debug);
                });

                Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(Convert.ToInt32(_settings.RecentJoinDecay * 1000));
                    if (CurrentRaidMode == RaidMode.None)
                    {
                        await _log.Log(
                            $"{member.DisplayName} ({member.Id}) no longer a recent join",
                            null);
                        RecentJoins.Remove(member.Id);
                    }
                });
                if (_settings.SmallRaidDecay != null)
                    Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(Convert.ToInt32(_settings.SmallRaidDecay * 60 * 1000));
                        if (_settings.SmallRaidDecay == null) return; // Was disabled
                        if (CurrentRaidMode != RaidMode.Small) return; // Not a small raid
                        if (CurrentSmallJoinCount >= _settings.SmallRaidSize)
                            return; // Small raid join count is still ongoing.
                        if (ManualRaidSilence) return; // This raid was manually silenced. Don't decay.

                        await _log.Log("Decaying raid: Small -> None", null, level: LogLevel.Debug);
                        await DecaySmallRaid(member.Guild);
                    });
                if (_settings.LargeRaidDecay != null)
                    Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(Convert.ToInt32(_settings.LargeRaidDecay * 60 * 1000));
                        if (_settings.SmallRaidDecay == null) return; // Was disabled
                        if (CurrentRaidMode != RaidMode.Large) return; // Not a large raid
                        if (CurrentLargeJoinCount >= _settings.LargeRaidSize)
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
