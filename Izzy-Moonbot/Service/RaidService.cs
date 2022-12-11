using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

    public List<SocketGuildUser> GetRecentJoins(SocketCommandContext context)
    {
        var RecentUsers = new List<SocketGuildUser>();

        _state.RecentJoins.ForEach(userId =>
        {
            var member = context.Guild.GetUser(userId);

            if (member != null) RecentUsers.Add(member);
        });

        return RecentUsers;
    }

    public async Task SilenceRecentJoins(SocketCommandContext context)
    {
        _config.AutoSilenceNewJoins = true;

        var recentJoins = _state.RecentJoins.Select(recentJoin =>
        {
            var member = context.Guild.GetUser(recentJoin);

            return member ?? null;
        }).Where(member => member != null).ToList();

        await _modService.SilenceUsers(recentJoins, "Suspected raider");

        _generalStorage.ManualRaidSilence = true;

        await FileHelper.SaveConfigAsync(_config);
        await FileHelper.SaveGeneralStorageAsync(_generalStorage);
    }

    public async Task EndRaid(SocketCommandContext context)
    {
        _generalStorage.CurrentRaidMode = RaidMode.None;

        _config.AutoSilenceNewJoins = false;

        await FileHelper.SaveConfigAsync(_config);

        _state.RecentJoins.RemoveAll( userId =>
        {
            var member = context.Guild.GetUser(userId);

            if (member is not { JoinedAt: { } }) return true;
            if (member.JoinedAt.Value.AddSeconds(_config.RecentJoinDecay) < DateTimeOffset.Now) return false;
            
            _log.Log(
                $"{member.DisplayName} ({member.Id}) no longer a recent join (immediate after raid)");
            return true;
        });
        
        _state.RecentJoins.ForEach(async userId =>
        {
            var member = context.Guild.GetUser(userId);

            if (member is not { JoinedAt: { } }) return;
            if (member.JoinedAt.Value.AddSeconds(_config.RecentJoinDecay) < DateTimeOffset.Now)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(Convert.ToInt32((member.JoinedAt.Value.AddSeconds(_config.RecentJoinDecay) - DateTimeOffset.Now) * 1000));
                    await _log.Log(
                        $"{member.DisplayName} ({member.Id}) no longer a recent join (after raid)");
                    _state.RecentJoins.Remove(member.Id);
                });
            }
        });

        _generalStorage.ManualRaidSilence = false;
        await FileHelper.SaveGeneralStorageAsync(_generalStorage);
    }

    private async Task DecaySmallRaid(SocketGuild guild)
    {
        _generalStorage.CurrentRaidMode = RaidMode.None;

        _config.AutoSilenceNewJoins = false;
        
        await FileHelper.SaveConfigAsync(_config);

        _state.RecentJoins.RemoveAll( userId =>
        {
            var member = guild.GetUser(userId);

            if (member is not { JoinedAt: { } }) return true;
            if (member.JoinedAt.Value.AddSeconds(_config.RecentJoinDecay) < DateTimeOffset.Now) return false;
            
            _log.Log(
                $"{member.DisplayName} ({member.Id}) no longer a recent join (immediate after raid)");
            return true;
        });
        
        _state.RecentJoins.ForEach(async userId =>
        {
            var member = guild.GetUser(userId);

            if (member is not { JoinedAt: { } }) return;
            if (member.JoinedAt.Value.AddSeconds(_config.RecentJoinDecay) < DateTimeOffset.Now)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(Convert.ToInt32((member.JoinedAt.Value.AddSeconds(_config.RecentJoinDecay) - DateTimeOffset.Now) * 1000));
                    await _log.Log(
                        $"{member.DisplayName} ({member.Id}) no longer a recent join (after raid)");
                    _state.RecentJoins.Remove(member.Id);
                });
            }
        });
        
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
            "`.ass` was ran manually, not disabling `AutoSilenceNewJoins`. Run `.assoff` to end the raid.";
        if (!_generalStorage.ManualRaidSilence) manualRaidActive = "";
        await _modLog.CreateModLog(guild)
            .SetContent($"The raid has deescalated. I'm lowering the raid level down to Small. {manualRaidActive}")
            .SetFileLogContent($"The raid has deescalated. I'm lowering the raid level down to Small. {manualRaidActive}")
            .Send();

        await FileHelper.SaveConfigAsync(_config);
        await FileHelper.SaveGeneralStorageAsync(_generalStorage);
    }

    public async Task CheckForTrip(SocketGuild guild)
    {
        if (_state.CurrentSmallJoinCount >= _config.SmallRaidSize && _generalStorage.CurrentRaidMode == RaidMode.None)
        {
            var potentialRaiders = new List<string>();

            _log.Log(
                "Small raid detected!");

            _state.RecentJoins.ForEach(userId =>
            {
                var member = guild.GetUser(userId);

                if (member == null)
                {
                    // We.. don't know who this user is?
                    // Well since we don't know who they are we can't silence them anyway
                    // Just mention that we couldn't find them and provide the id.
                    potentialRaiders.Add($"Unknown User (`{userId}`)");
                }
                else
                {
                    var joinDate = "`Couldn't get member join time`";
                    if (member.JoinedAt.HasValue) joinDate = $"<t:{member.JoinedAt.Value.ToUnixTimeSeconds()}:F>";
                    potentialRaiders.Add($"{member.Username}#{member.Discriminator} (joined: {joinDate})");
                }
            });

            // Potential raid. Bug the mods
            await _modLog.CreateModLog(guild)
                .SetContent(
                    $"<@&{_config.ModRole}> Bing-bong! Possible raid detected! ({_config.SmallRaidSize} (`SmallRaidSize`) users joined within {_config.SmallRaidTime} (`SmallRaidTime`) seconds.){Environment.NewLine}{Environment.NewLine}" +
                    $"{string.Join($"{Environment.NewLine}", potentialRaiders)}{Environment.NewLine}{Environment.NewLine}" +
                    $"Possible commands for this scenario are:{Environment.NewLine}" +
                    $"`{_config.Prefix}ass` - Enable automatically silencing new joins *and* autosilence those considered part of the raid (those who joined within {_config.RecentJoinDecay} (`RecentJoinDecay`) seconds).{Environment.NewLine}" +
                    $"`{_config.Prefix}assoff` - Disable automatically silencing new joins and resets the raid level back to 'no raid'. This will **not** unsilence those considered part of the raid.{Environment.NewLine}" +
                    $"`{_config.Prefix}getraid` - Returns a list of those who are considered part of the raid by Izzy. (those who joined {_config.RecentJoinDecay} (`RecentJoinDecay`) seconds before the raid began).")
                .SetFileLogContent($"Bing-bong! Possible raid detected! ({_config.SmallRaidSize} (`SmallRaidSize`) users joined within {_config.SmallRaidTime} (`SmallRaidTime`) seconds.){Environment.NewLine}" +
                                   $"{string.Join($"{Environment.NewLine}", potentialRaiders)}{Environment.NewLine}")
                .Send();

            _generalStorage.CurrentRaidMode = RaidMode.Small;
            await FileHelper.SaveGeneralStorageAsync(_generalStorage);
        }

        if (_state.CurrentLargeJoinCount >= _config.LargeRaidSize && _generalStorage.CurrentRaidMode != RaidMode.Large)
        {
            var potentialRaiders = new List<string>();

            _log.Log(
                "Large raid detected!");

            var recentJoins = _state.RecentJoins.Select(recentJoin =>
            {
                var member = guild.GetUser(recentJoin);

                if (member == null) return null;
                return member;
            }).Where(member => member != null).ToList();

            await _modService.SilenceUsers(recentJoins, "Suspected raider");

            _state.RecentJoins.ForEach(async userId =>
            {
                var member = guild.GetUser(userId);

                if (member == null)
                {
                    // We.. don't know who this user is?
                    // Well since we don't know who they are we can't silence them anyway
                    // Just mention that we couldn't find them and provide the id.
                    potentialRaiders.Add($"Unknown User (`{userId}`)");
                }
                else
                {
                    var joinDate = "`Couldn't get member join time`";
                    if (member.JoinedAt.HasValue) joinDate = $"<t:{member.JoinedAt.Value.ToUnixTimeSeconds()}:F>";
                    potentialRaiders.Add($"{member.Username}#{member.Discriminator} (joined: {joinDate})");

                    if (member.Roles.Any(role => role.Id == _config.MemberRole))
                    {
                        await _modService.SilenceUser(member, "Suspected raider");
                    }
                }
            });

            if (_generalStorage.CurrentRaidMode == RaidMode.None)
            {
                await _modLog.CreateModLog(guild)
                    .SetContent(
                        $"<@&{_config.ModRole}> Bing-bong! Raid detected! ({_config.LargeRaidSize} (`LargeRaidSize`) users joined within {_config.LargeRaidTime} (`LargeRaidTime`) seconds.){Environment.NewLine}" +
                        $"I have automatically silenced all the members below members and enabled autosilencing users on join.{Environment.NewLine}{Environment.NewLine}" +
                        $"{string.Join($"{Environment.NewLine}", potentialRaiders)}{Environment.NewLine}{Environment.NewLine}" +
                        $"Possible commands for this scenario are:{Environment.NewLine}" +
                        $"`{_config.Prefix}assoff` - Disable automatically silencing new joins and resets the raid level back to 'no raid'.. This will **not** unsilence those considered part of the raid.{Environment.NewLine}" +
                        $"`{_config.Prefix}getraid` - Returns a list of those who are considered part of the raid by Izzy. (those who joined within {_config.RecentJoinDecay} (`RecentJoinDecay`) seconds).")
                    .SetFileLogContent($"Bing-bong! Raid detected! ({_config.LargeRaidSize} (`LargeRaidSize`) users joined within {_config.LargeRaidTime} (`LargeRaidTime`) seconds.){Environment.NewLine}" +
                                       $"I have automatically silenced all the members below members and enabled autosilencing users on join.{Environment.NewLine}" +
                                       $"{string.Join($"{Environment.NewLine}", potentialRaiders)}{Environment.NewLine}")
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
            await _log.Log(
                $"Small raid join count raised for {member.DisplayName} ({member.Id}). Now at {_state.CurrentSmallJoinCount}/{_config.SmallRaidSize} for {_config.SmallRaidTime} seconds.",
                level: LogLevel.Debug);
            _state.CurrentLargeJoinCount++;
            await _log.Log(
                $"Large raid join count raised for {member.DisplayName} ({member.Id}). Now at {_state.CurrentLargeJoinCount}/{_config.LargeRaidSize} for {_config.LargeRaidTime} seconds.",
                level: LogLevel.Debug);

            _state.RecentJoins.Add(member.Id);

            await _log.Log(
                $"Recent join: {member.DisplayName} ({member.Id}), No longer considered recent join in {_config.RecentJoinDecay} seconds.");

            await CheckForTrip(member.Guild);

            Task.Run(async () =>
            {
                await Task.Delay(Convert.ToInt32(_config.SmallRaidTime * 1000));
                _state.CurrentSmallJoinCount--;
                await _log.Log(
                    $"Small raid join count dropped for {member.DisplayName} ({member.Id}). Now at {_state.CurrentSmallJoinCount}./{_config.SmallRaidSize} after {_config.SmallRaidTime} seconds.",
                    level: LogLevel.Debug);
            });

            Task.Run(async () =>
            {
                await Task.Delay(Convert.ToInt32(_config.LargeRaidTime * 1000));
                _state.CurrentLargeJoinCount--;
                await _log.Log(
                    $"Large raid join count dropped for {member.DisplayName} ({member.Id}). Now at {_state.CurrentLargeJoinCount}/{_config.LargeRaidSize} after {_config.LargeRaidTime} seconds.",
                    level: LogLevel.Debug);
            });

            Task.Run(async () =>
            {
                await Task.Delay(Convert.ToInt32(_config.RecentJoinDecay * 1000));
                if (_generalStorage.CurrentRaidMode == RaidMode.None)
                {
                    await _log.Log(
                        $"{member.DisplayName} ({member.Id}) no longer a recent join");
                    _state.RecentJoins.Remove(member.Id);
                }
            });
            if (_config.SmallRaidDecay != null)
                Task.Run(async () =>
                {
                    await Task.Delay(Convert.ToInt32(_config.SmallRaidDecay * 60 * 1000));
                    if (_config.SmallRaidDecay == null) return; // Was disabled
                    if (_generalStorage.CurrentRaidMode != RaidMode.Small) return; // Not a small raid
                    if (_state.CurrentSmallJoinCount > 0)
                        return; // Small raid join count is still ongoing.
                    if (_generalStorage.ManualRaidSilence) return; // This raid was manually silenced. Don't decay.

                    await _log.Log("Decaying raid: Small -> None", level: LogLevel.Debug);
                    await DecaySmallRaid(member.Guild);
                });
            if (_config.LargeRaidDecay != null)
                Task.Run(async () =>
                {
                    await Task.Delay(Convert.ToInt32(_config.LargeRaidDecay * 60 * 1000));
                    if (_config.SmallRaidDecay == null) return; // Was disabled
                    if (_generalStorage.CurrentRaidMode != RaidMode.Large) return; // Not a large raid
                    if (_state.CurrentLargeJoinCount > _config.SmallRaidSize)
                        return; // Large raid join count is still ongoing.

                    await _log.Log("Decaying raid: Large -> Small", level: LogLevel.Debug);
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