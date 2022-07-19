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
        private LoggingService _logging;

        private List<ulong> RecentJoins = new List<ulong>();

        private int CurrentSmallJoinCount = 0;
        private int CurrentLargeJoinCount = 0;

        public RaidMode CurrentRaidMode = RaidMode.NONE;

        public RaidService(ServerSettings settings, ModService modService, LoggingService logging)
        {
            _settings = settings;
            _modService = modService;
            _logging = logging;
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
            RecentJoins.ForEach(async (userId) =>
            {
                SocketGuildUser member = context.Guild.GetUser(userId);

                if (member != null)
                {
                    DateTimeOffset? muteUntil = null;
                    if (_settings.SilenceTimeout.HasValue) muteUntil = DateTimeOffset.Now.AddSeconds(_settings.SilenceTimeout.Value);

                    await _modService.SilenceUser(member, DateTimeOffset.Now, muteUntil, "Suspected raider", false);
                }
            });

            _settings.AutoSilenceNewJoins = true;

            await FileHelper.SaveSettingsAsync(_settings);
        }

        public async Task EndRaid(SocketCommandContext context)
        {
            CurrentRaidMode = RaidMode.NONE;

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

            _settings.AutoSilenceNewJoins = false;

            await FileHelper.SaveSettingsAsync(_settings);
        }

        public async Task CheckForTrip(SocketGuild guild)
        {
            if(CurrentSmallJoinCount >= _settings.SmallRaidSize && CurrentRaidMode == RaidMode.NONE)
            {
                List<string> potentialRaiders = new List<string>();
                
                _logging.Log(
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

                // Potential raid. Bug the mods
                await guild.GetTextChannel(_settings.LogChannel).SendMessageAsync($"<@&{_settings.ModRole}> Bing-bong! Possible raid detected! (over {_settings.SmallRaidSize} (`SmallRaidSize`) users joined within {_settings.SmallRaidTime} (`SmallRaidTime`) seconds.){Environment.NewLine}{Environment.NewLine}" +
                    $"{string.Join($"{Environment.NewLine}", potentialRaiders)}{Environment.NewLine}{Environment.NewLine}" +
                    $"Possible commands for this scenario are:{Environment.NewLine}" +
                    $"`{_settings.Prefix}ass` - Enable automatically silencing new joins *and* autosilence those considered part of the raid (those who joined within {_settings.RecentJoinDecay} (`RecentJoinDecay`) seconds).{Environment.NewLine}" +
                    $"`{_settings.Prefix}assoff` - Disable automatically silencing new joins and resets the raid level back to 'no raid'. This will **not** unsilence those considered part of the raid.{Environment.NewLine}" +
                    $"`{_settings.Prefix}getraid` - Returns a list of those who are considered part of the raid by Izzy. (those who joined {_settings.RecentJoinDecay} (`RecentJoinDecay`) seconds before the raid began).{Environment.NewLine}" +
                    $"`{_settings.Prefix}banraid` - Bans everyone considered to be part of the raid. **This should be a last measure if Izzy becomes ratelimited while trying to deal with the raid. Use `getraid` to see who would be banned.**");

                CurrentRaidMode = RaidMode.SMALL;
            }
            if(CurrentLargeJoinCount >= _settings.LargeRaidSize && CurrentRaidMode != RaidMode.LARGE)
            {
                List<string> potentialRaiders = new List<string>();
                
                _logging.Log(
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

                        DateTimeOffset? muteUntil = null;
                        if (_settings.SilenceTimeout.HasValue) muteUntil = DateTimeOffset.Now.AddSeconds(_settings.SilenceTimeout.Value);

                        await _modService.SilenceUser(member, DateTimeOffset.Now, muteUntil, "Suspected raider", false);
                    }
                });

                if (CurrentRaidMode == RaidMode.NONE)
                {
                    await guild.GetTextChannel(_settings.LogChannel).SendMessageAsync($"<@&{_settings.ModRole}> Bing-bong! Raid detected! (over {_settings.LargeRaidSize} (`LargeRaidSize`) users joined within {_settings.LargeRaidTime} (`LargeRaidTime`) seconds.){Environment.NewLine}" +
                        $"I have automatically silenced all the above members and enabled autosilencing users on join.{Environment.NewLine}{Environment.NewLine}" +
                        $"{string.Join($"{Environment.NewLine}", potentialRaiders)}{Environment.NewLine}{Environment.NewLine}" +
                        $"Possible commands for this scenario are:{Environment.NewLine}" +
                        $"`{_settings.Prefix}assoff` - Disable automatically silencing new joins and resets the raid level back to 'no raid'.. This will **not** unsilence those considered part of the raid.{Environment.NewLine}" +
                        $"`{_settings.Prefix}getraid` - Returns a list of those who are considered part of the raid by Izzy. (those who joined within {_settings.RecentJoinDecay} (`RecentJoinDecay`) seconds).{Environment.NewLine}" +
                        $"`{_settings.Prefix}banraid` - Bans everyone considered to be part of the raid. **This should be a last measure if Izzy becomes ratelimited while trying to deal with the raid. Use `getraid` to see who would be banned.**");
                }
                else
                {
                    if (_settings.AutoSilenceNewJoins)
                    {
                        await guild.GetTextChannel(_settings.LogChannel).SendMessageAsync($"<@&{_settings.ModRole}> **The current raid has escalated. Silencing new joins has already been enabled manually.** (over {_settings.LargeRaidSize} (`LargeRaidSize`) users joined within {_settings.LargeRaidTime} (`LargeRaidTime`) seconds.)");
                    }
                    else
                    {
                        await guild.GetTextChannel(_settings.LogChannel).SendMessageAsync($"<@&{_settings.ModRole}> **The current raid has escalated and I have automatically enabled silencing new joins and I've silenced those considered part of the raid.** (over {_settings.LargeRaidSize} (`LargeRaidSize`) users joined within {_settings.LargeRaidTime} (`LargeRaidTime`) seconds.)");
                    }
                }

                CurrentRaidMode = RaidMode.SMALL;
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
                _logging.Log(
                    $"Small raid join count raised for {member.DisplayName} ({member.Id}). Now at {CurrentSmallJoinCount}/{_settings.SmallRaidSize} for {_settings.SmallRaidTime} seconds.",
                    null, level: LogLevel.Debug);
                CurrentLargeJoinCount++;
                _logging.Log(
                    $"Large raid join count raised for {member.DisplayName} ({member.Id}). Now at {CurrentLargeJoinCount}/{_settings.LargeRaidSize} for {_settings.LargeRaidTime} seconds.",
                    null, level: LogLevel.Debug);

                RecentJoins.Add(member.Id);

                _logging.Log(
                    $"Recent join: {member.DisplayName} ({member.Id}), No longer considered recent join in {_settings.RecentJoinDecay} seconds.",
                    null, false);

                await CheckForTrip(member.Guild);

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(Convert.ToInt32(_settings.SmallRaidTime * 1000));
                    CurrentSmallJoinCount--;
                    _logging.Log(
                        $"Small raid join count dropped for {member.DisplayName} ({member.Id}). Now at {CurrentSmallJoinCount}./{_settings.SmallRaidSize} after {_settings.SmallRaidTime} seconds.",
                        null, level: LogLevel.Debug);
                });

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(Convert.ToInt32(_settings.LargeRaidTime * 1000));
                    CurrentLargeJoinCount--;
                    _logging.Log(
                        $"Large raid join count dropped for {member.DisplayName} ({member.Id}). Now at {CurrentLargeJoinCount}/{_settings.LargeRaidSize} after {_settings.LargeRaidTime} seconds.",
                        null, level: LogLevel.Debug);
                });

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(Convert.ToInt32(_settings.RecentJoinDecay * 1000));
                    if (CurrentRaidMode == RaidMode.NONE)
                    {
                        _logging.Log(
                            $"{member.DisplayName} ({member.Id}) no longer a recent join",
                            null);
                        RecentJoins.Remove(member.Id);
                    }
                });
            }
            else
            {
                _logging.Log(
                    $"{member.DisplayName}#{member.Discriminator} ({member.Id}) rejoined while still considered a recent join. Not calculating additional raid pressure.",
                    null);
            }
        }
    }

    public enum RaidMode
    {
        NONE,
        SMALL,
        LARGE
    }
}
