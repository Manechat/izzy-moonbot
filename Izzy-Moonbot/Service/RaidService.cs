using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;

namespace Izzy_Moonbot.Service;

/*
 * The service responsible for handling antiraid functions
 * TODO: better description lol
 */
public class RaidService
{
    private readonly LoggingService _log;
    private readonly ModLoggingService _modLog;
    private readonly ModService _modService;
    private readonly Config _config;
    private readonly State _state;
    private readonly GeneralStorage _generalStorage;

    public RaidService(Config config, ModService modService, LoggingService log, ModLoggingService modLog,
        State state, GeneralStorage generalStorage)
    {
        _config = config;
        _modService = modService;
        _log = log;
        _modLog = modLog;
        _state = state;
        _generalStorage = generalStorage;
    }

    public void RegisterEvents(DiscordSocketClient client)
    {
        client.UserJoined += (member) => Task.Run(async () => { await ProcessMemberJoin(member); });
    }

    public bool UserRecentlyJoined(ulong id)
    {
        return _state.RecentJoins.Contains(id);
    }

    // This method proactively checks that the joins still are recent and updates _state if any expired,
    // so call sites should only call this once and reuse the result as much as possible.
    public List<SocketGuildUser> GetRecentJoins(SocketGuild guild)
    {
        var recentGuildUsers = new List<SocketGuildUser>();
        var expiredUserIds = new List<ulong>();

        _state.RecentJoins.ForEach(userId =>
        {
            var member = guild.GetUser(userId);

            if (member is not { JoinedAt: { } })
                expiredUserIds.Add(userId);
            else if (member.JoinedAt.Value.AddSeconds(_config.RecentJoinDecay) < DateTimeOffset.Now)
                expiredUserIds.Add(userId);
            else
                recentGuildUsers.Add(member);
        });

        if (expiredUserIds.Count > 0)
        {
            _log.Log($"Removing id(s) {string.Join(' ', expiredUserIds)} from _state.RecentJoins");
            expiredUserIds.ForEach(expiredUserId => _state.RecentJoins.Remove(expiredUserId));
        }

        return recentGuildUsers;
    }

    private async Task DecaySmallRaid(SocketGuild guild)
    {
        _generalStorage.CurrentRaidMode = RaidMode.None;

        _config.AutoSilenceNewJoins = false;
        
        await FileHelper.SaveConfigAsync(_config);

        await _modLog.CreateModLog(guild)
            .SetContent(
                $"The raid has ended. I've disabled raid defences and cleared my internal cache of all recent joins.")
            .SetFileLogContent("The raid has ended. I've disabled raid defences and cleared my internal cache of all recent joins.")
            .Send();

        await FileHelper.SaveGeneralStorageAsync(_generalStorage);
    }

    private async Task DecayLargeRaid(SocketGuild guild)
    {
        _generalStorage.CurrentRaidMode = RaidMode.Small;

        if (!_generalStorage.ManualRaidSilence) _config.AutoSilenceNewJoins = false;

        var manualRaidActive =
            " `.ass` was run manually, so I'm not automatically disabling `AutoSilenceNewJoins`. Run `.assoff` to manually end the raid.";
        if (!_generalStorage.ManualRaidSilence) manualRaidActive = "";
        await _modLog.CreateModLog(guild)
            .SetContent($"The raid has deescalated. I'm lowering the raid level down to Small.{manualRaidActive}")
            .SetFileLogContent($"The raid has deescalated. I'm lowering the raid level down to Small.{manualRaidActive}")
            .Send();

        await FileHelper.SaveConfigAsync(_config);
        await FileHelper.SaveGeneralStorageAsync(_generalStorage);
    }

    public async Task CheckForTrip(SocketGuild guild)
    {
        var recentJoins = GetRecentJoins(guild);

        if (_state.CurrentSmallJoinCount >= _config.SmallRaidSize && _generalStorage.CurrentRaidMode == RaidMode.None)
        {
            var potentialRaiders = new List<string>();

            _log.Log(
                "Small raid detected!");

            recentJoins.ForEach(member =>
            {
                var joinDate = "`Couldn't get member join time`";
                if (member.JoinedAt.HasValue) joinDate = $"<t:{member.JoinedAt.Value.ToUnixTimeSeconds()}:F>";
                potentialRaiders.Add($"{member.Username}#{member.Discriminator} (joined: {joinDate})");
            });

            // Potential raid. Bug the mods
            await _modLog.CreateModLog(guild)
                .SetContent(
                    $"<@&{_config.ModRole}> Bing-bong! Possible raid detected! ({_config.SmallRaidSize} (`SmallRaidSize`) users joined within {_config.SmallRaidTime} (`SmallRaidTime`) seconds.)\n\n" +
                    $"{string.Join($"\n", potentialRaiders)}\n\n" +
                    $"Possible commands for this scenario are:\n" +
                    $"`{_config.Prefix}ass` - Set `AutoSilenceNewJoins` to `true` and silence recent joins (as defined by `.config RecentJoinDecay`).\n" +
                    $"`{_config.Prefix}assoff` - Set `AutoSilenceNewJoins` to `false`.\n" +
                    $"`{_config.Prefix}stowaways` - List non-bot, non-mod users who do not have the member role.\n" +
                    $"`{_config.Prefix}getrecentjoins` - Get a list of recent joins (as defined by `.config RecentJoinDecay`).\n" +
                    $"\n" +
                    $"If you do not believe this is a raid, simply do nothing. If you do believe this is a raid, typically you should run `.ass`, then manually vet every user who joins " +
                        $"(ending in a kick, ban, or manually adding the MemberRole) until you believe the raid is over, then run `.assoff`, and finally `.stowaways` to double-check if we missed anyone.")
                .SetFileLogContent($"Bing-bong! Possible raid detected! ({_config.SmallRaidSize} (`SmallRaidSize`) users joined within {_config.SmallRaidTime} (`SmallRaidTime`) seconds.)\n" +
                                   $"{string.Join($"\n", potentialRaiders)}\n")
                .Send();

            _generalStorage.CurrentRaidMode = RaidMode.Small;
            await FileHelper.SaveGeneralStorageAsync(_generalStorage);
        }

        if (_state.CurrentLargeJoinCount >= _config.LargeRaidSize && _generalStorage.CurrentRaidMode != RaidMode.Large)
        {
            var potentialRaiders = new List<string>();

            _log.Log(
                "Large raid detected!");

            await _modService.SilenceUsers(recentJoins, "auto-silenced all suspected raiders after a large raid was detected");

            recentJoins.ForEach(async member =>
            {
                var joinDate = "`Couldn't get member join time`";
                if (member.JoinedAt.HasValue) joinDate = $"<t:{member.JoinedAt.Value.ToUnixTimeSeconds()}:F>";
                potentialRaiders.Add($"{member.Username}#{member.Discriminator} (joined: {joinDate})");

                if (member.Roles.Any(role => role.Id == _config.MemberRole))
                {
                    await _modService.SilenceUser(member, "auto-silenced all suspected raiders after a large raid was detected");
                }
            });

            if (_generalStorage.CurrentRaidMode == RaidMode.None)
            {
                await _modLog.CreateModLog(guild)
                    .SetContent(
                        $"<@&{_config.ModRole}> Bing-bong! Raid detected! ({_config.LargeRaidSize} (`LargeRaidSize`) users joined within {_config.LargeRaidTime} (`LargeRaidTime`) seconds.)\n" +
                        $"I have automatically silenced all the members below and enabled autosilencing users on join.\n\n" +
                        $"{string.Join($"\n", potentialRaiders)}\n\n" +
                        $"Possible commands for this scenario are:\n" +
                        $"`{_config.Prefix}assoff` - Set `AutoSilenceNewJoins` to `false`.\n" +
                        $"`{_config.Prefix}stowaways` - List non-bot, non-mod users who do not have the member role.\n" +
                        $"`{_config.Prefix}getrecentjoins` - Get a list of recent joins (as defined by `.config RecentJoinDecay`).")
                    .SetFileLogContent($"Bing-bong! Raid detected! ({_config.LargeRaidSize} (`LargeRaidSize`) users joined within {_config.LargeRaidTime} (`LargeRaidTime`) seconds.)\n" +
                                       $"I have automatically silenced all the members below members and enabled autosilencing users on join.\n" +
                                       $"{string.Join($"\n", potentialRaiders)}\n")
                    .Send();
            }
            else
            {
                if (_config.AutoSilenceNewJoins)
                    await _modLog.CreateModLog(guild)
                        .SetContent(
                            $"<@&{_config.ModRole}> **The current raid has escalated. Silencing new joins has already been enabled manually.** ({_config.LargeRaidSize} (`LargeRaidSize`) users joined within {_config.LargeRaidTime} (`LargeRaidTime`) seconds.)")
                        .SetFileLogContent($"The current raid has escalated. Silencing new joins has already been enabled manually. ({_config.LargeRaidSize} (`LargeRaidSize`) users joined within {_config.LargeRaidTime} (`LargeRaidTime`) seconds.)")
                        .Send();
                else
                    await _modLog.CreateModLog(guild)
                        .SetContent(
                            $"<@&{_config.ModRole}> **The current raid has escalated and I have automatically enabled silencing new joins and I've silenced those considered part of the raid.** ({_config.LargeRaidSize} (`LargeRaidSize`) users joined within {_config.LargeRaidTime} (`LargeRaidTime`) seconds.)")
                        .SetFileLogContent($"The current raid has escalated and I have automatically enabled silencing new joins and I've silenced those considered part of the raid. ({_config.LargeRaidSize} (`LargeRaidSize`) users joined within {_config.LargeRaidTime} (`LargeRaidTime`) seconds.)")
                        .Send();
            }

            _generalStorage.CurrentRaidMode = RaidMode.Large;
            _config.AutoSilenceNewJoins = true;

            await FileHelper.SaveConfigAsync(_config);
            await FileHelper.SaveGeneralStorageAsync(_generalStorage);
        }
    }

    public async Task ProcessMemberJoin(SocketGuildUser member)
    {
        if (member.Guild.Id != DiscordHelper.DefaultGuild()) return; // Don't process non-default server.
        if (!_config.RaidProtectionEnabled) return;
        if (!UserRecentlyJoined(member.Id))
        {
            _state.CurrentSmallJoinCount++;
            _log.Log(
                $"Small raid join count raised for {member.DisplayName} ({member.Id}). Now at {_state.CurrentSmallJoinCount}/{_config.SmallRaidSize} for {_config.SmallRaidTime} seconds.",
                level: LogLevel.Debug);
            _state.CurrentLargeJoinCount++;
            _log.Log(
                $"Large raid join count raised for {member.DisplayName} ({member.Id}). Now at {_state.CurrentLargeJoinCount}/{_config.LargeRaidSize} for {_config.LargeRaidTime} seconds.",
                level: LogLevel.Debug);

            _state.RecentJoins.Add(member.Id);

            _log.Log(
                $"{member.DisplayName} ({member.Id}) will be considered a recent join for the next {_config.RecentJoinDecay} seconds.");

            await CheckForTrip(member.Guild);

            var _ = Task.Run(async () =>
            {
                await Task.Delay(Convert.ToInt32(_config.SmallRaidTime * 1000));
                _state.CurrentSmallJoinCount--;
                _log.Log(
                    $"Small raid join count dropped for {member.DisplayName} ({member.Id}). Now at {_state.CurrentSmallJoinCount}/{_config.SmallRaidSize} after {_config.SmallRaidTime} seconds.",
                    level: LogLevel.Debug);
            });

            _ = Task.Run(async () =>
            {
                await Task.Delay(Convert.ToInt32(_config.LargeRaidTime * 1000));
                _state.CurrentLargeJoinCount--;
                _log.Log(
                    $"Large raid join count dropped for {member.DisplayName} ({member.Id}). Now at {_state.CurrentLargeJoinCount}/{_config.LargeRaidSize} after {_config.LargeRaidTime} seconds.",
                    level: LogLevel.Debug);
            });

            _ = Task.Run(async () =>
            {
                await Task.Delay(Convert.ToInt32(_config.RecentJoinDecay * 1000));
                if (_generalStorage.CurrentRaidMode == RaidMode.None)
                {
                    _log.Log(
                        $"{member.DisplayName} ({member.Id}) no longer a recent join");
                    _state.RecentJoins.Remove(member.Id);
                }
            });
            if (_config.SmallRaidDecay != null)
                _ = Task.Run(async () =>
                {
                    await Task.Delay(Convert.ToInt32(_config.SmallRaidDecay * 60 * 1000));
                    if (_config.SmallRaidDecay == null) return; // Was disabled
                    if (_generalStorage.CurrentRaidMode != RaidMode.Small) return; // Not a small raid
                    if (_state.CurrentSmallJoinCount > 0)
                        return; // Small raid join count is still ongoing.
                    if (_generalStorage.ManualRaidSilence) return; // This raid was manually silenced. Don't decay.

                    _log.Log("Decaying raid: Small -> None", level: LogLevel.Debug);
                    await DecaySmallRaid(member.Guild);
                });
            if (_config.LargeRaidDecay != null)
                _ = Task.Run(async () =>
                {
                    await Task.Delay(Convert.ToInt32(_config.LargeRaidDecay * 60 * 1000));
                    if (_config.SmallRaidDecay == null) return; // Was disabled
                    if (_generalStorage.CurrentRaidMode != RaidMode.Large) return; // Not a large raid
                    if (_state.CurrentLargeJoinCount > _config.SmallRaidSize)
                        return; // Large raid join count is still ongoing.

                    _log.Log("Decaying raid: Large -> Small", level: LogLevel.Debug);
                    await DecayLargeRaid(member.Guild);
                });
        }
        else
        {
            _log.Log(
                $"{member.DisplayName}#{member.Discriminator} ({member.Id}) rejoined while still considered a recent join. Not calculating additional raid pressure.");
        }
    }
}

public enum RaidMode
{
    None,
    Small,
    Large
}