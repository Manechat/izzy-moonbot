using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Service;

public class RaidService
{
    private readonly LoggingService _log;
    private readonly ModLoggingService _modLog;
    private readonly ModService _modService;
    private readonly Config _config;
    private readonly TransientState _state;
    private readonly GeneralStorage _generalStorage;
    private readonly ScheduleService _schedule;

    public RaidService(Config config, ModService modService, LoggingService log, ModLoggingService modLog,
        TransientState state, GeneralStorage generalStorage, ScheduleService schedule)
    {
        _config = config;
        _modService = modService;
        _log = log;
        _modLog = modLog;
        _state = state;
        _generalStorage = generalStorage;
        _schedule = schedule;

        // We want RaidService to schedule a job that runs RaidService code. That's a circular dependency, so
        // our only option is two-phase initialization by passing a delegate/callback after normal construction.
        _schedule.RegisterEndRaidCallback(async (ScheduledEndRaidJob job, IIzzyGuild guild) =>
        {
            if (job.IsLarge)
                await EndLargeRaid(guild);
            else
                await EndSmallRaid(guild);
        });
    }

    public void RegisterEvents(IIzzyClient client)
    {
        client.UserJoined += (member) => Task.Run(async () => { await ProcessMemberJoin(member); });
    }

    // This method proactively checks that the joins still are recent and updates _state if any expired,
    // so call sites should only call this once and reuse the result as much as possible.
    public List<IIzzyGuildUser> GetRecentJoins(IIzzyGuild guild)
    {
        var recentGuildUsers = new List<IIzzyGuildUser>();
        var expiredUserIds = new List<ulong>();

        _state.RecentJoins.ForEach(userId =>
        {
            var member = guild.GetUser(userId);

            if (member is not { JoinedAt: { } })
                expiredUserIds.Add(userId);
            else if (member.JoinedAt.Value.AddSeconds(_config.RecentJoinDecay) < DateTimeHelper.UtcNow)
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

    public string TIME_SINCE_SMALL() => $"{_config.SmallRaidDecay} minutes have passed since I detected a spike of {_config.SmallRaidSize} recent joins";
    public string TIME_SINCE_LARGE() => $"{_config.LargeRaidDecay} minutes have passed since I detected a spike of {_config.LargeRaidSize} recent joins";
    public string BUT_RECENT() => $"but there are {_state.RecentJoins.Count} (>={_config.SmallRaidSize}) recent joins now";
    public string AND_ONLY_RECENT() => $"and there are only {_state.RecentJoins.Count} (<{_config.SmallRaidSize}) recent joins now";
    public static string CONSIDER_OVER = ", so I consider the raid to be over.";
    public static string CONSIDER_ONGOING = ", so I consider the raid to be ongoing and will not generate any additional messages.";
    public static string ALARMS_ACTIVE = "Any future spike in recent joins will generate a new alarm.";
    public static string PLEASE_ASSOFF = "Please run `.assoff` when you believe the raid is over, so I know to start alarming on join spikes again.";

    private string RaidersDescription(List<IIzzyGuildUser> recentJoins)
    {
        var raiderDescriptions = new List<string>();
        recentJoins.ForEach(member =>
        {
            var joinDate = "`Couldn't get member join time`";
            if (member.JoinedAt.HasValue) joinDate = $"<t:{member.JoinedAt.Value.ToUnixTimeSeconds()}:F>";
            raiderDescriptions.Add($"<@{member.Id}> {member.DisplayName} ({member.Username}/{member.Id}) (joined: {joinDate})");
        });
        return string.Join($"\n", raiderDescriptions);
    }
    private async Task TripSmallRaid(IIzzyGuild guild, List<IIzzyGuildUser> recentJoins)
    {
        _log.Log("Small raid detected!");

        var msg = $"<@&{_config.ModRole}> Bing-bong! Possible raid detected! ({_config.SmallRaidSize} (`SmallRaidSize`) recent joins)\n\n" +
            $"{RaidersDescription(recentJoins)}";
        await _modLog.CreateModLog(guild)
            .SetContent(msg +
                $"\n\nPossible commands for this scenario are:\n" +
                $"`{_config.Prefix}ass` - Set `AutoSilenceNewJoins` to `true` and silence recent joins (as defined by `.config RecentJoinDecay`).\n" +
                $"`{_config.Prefix}assoff` - Set `AutoSilenceNewJoins` to `false`.\n" +
                $"`{_config.Prefix}stowaways` - List non-bot, non-mod users who do not have the member role.\n" +
                $"`{_config.Prefix}getrecentjoins` - Get a list of recent joins (as defined by `.config RecentJoinDecay`).\n" +
                $"\n" +
                $"If you do not believe this is a raid, simply do nothing. If you do believe this is a raid, typically you should run `.ass`, then manually vet every user who joins " +
                    $"(ending in a kick, ban, or manually adding the MemberRole) until you believe the raid is over, then run `.assoff`, and finally `.stowaways` to double-check if we missed anyone.")
            .SetFileLogContent(msg)
            .Send();

        _generalStorage.CurrentRaidMode = RaidMode.Small;
        _generalStorage.SuspectedRaiders = recentJoins.Select(j => j.Id).ToHashSet();
        await FileHelper.SaveGeneralStorageAsync(_generalStorage);

        if (_config.SmallRaidDecay != null)
        {
            var action = new ScheduledEndRaidJob(false /* isLarge */);
            var job = new ScheduledJob(DateTimeHelper.UtcNow, DateTimeHelper.UtcNow.AddMinutes((double)_config.SmallRaidDecay), action);
            await _schedule.CreateScheduledJob(job);
        }
    }

    public async Task EndSmallRaid(IIzzyGuild guild)
    {
        if (_config.SmallRaidDecay == null) return; // Was disabled

        // Either someone ran .assoff or this escalated to a large raid. Either way, we don't need a "small raid is over" message.
        if (_generalStorage.CurrentRaidMode != RaidMode.Small) return;

        var recentJoinCount = GetRecentJoins(guild).Count;
        if (recentJoinCount >= _config.SmallRaidSize)
        {
            _log.Log("Small raid is ongoing, inform mods it will have to be ended manually");

            var msg = TIME_SINCE_SMALL() + ", " + BUT_RECENT() + CONSIDER_ONGOING + "\n\n" + PLEASE_ASSOFF;
            await _modLog.CreateModLog(guild).SetContent(msg).SetFileLogContent(msg).Send();
        }
        else if (_generalStorage.ManualRaidSilence)
        {
            var msg = TIME_SINCE_SMALL() + ", but `.ass` was run in the meantime" + CONSIDER_ONGOING + "\n\n" + PLEASE_ASSOFF;
            await _modLog.CreateModLog(guild).SetContent(msg).SetFileLogContent(msg).Send();
        }
        else
        {
            _log.Log("Ending small raid");

            _generalStorage.CurrentRaidMode = RaidMode.None;
            _generalStorage.SuspectedRaiders.Clear();
            await FileHelper.SaveGeneralStorageAsync(_generalStorage);

            var msg = TIME_SINCE_SMALL() + ", " + AND_ONLY_RECENT() + CONSIDER_OVER + " " + ALARMS_ACTIVE;
            await _modLog.CreateModLog(guild).SetContent(msg).SetFileLogContent(msg).Send();
        }
    }

    private async Task TripLargeRaid(IIzzyGuild guild, List<IIzzyGuildUser> recentJoins)
    {
        _log.Log("Large raid detected!");

        _generalStorage.CurrentRaidMode = RaidMode.Large;
        _generalStorage.SuspectedRaiders = recentJoins.Select(j => j.Id).ToHashSet();
        await FileHelper.SaveGeneralStorageAsync(_generalStorage);

        var autoSilenceValueBefore = _config.AutoSilenceNewJoins;
        if (!autoSilenceValueBefore)
        {
            _config.AutoSilenceNewJoins = true;
            await FileHelper.SaveConfigAsync(_config);

            await _modService.SilenceUsers(recentJoins, "auto-silenced all suspected raiders after a large raid was detected");
        }

        var msg = $"<@&{_config.ModRole}> Bing-bong! Raid detected! ({_config.LargeRaidSize} (`LargeRaidSize`) recent joins)";
        msg += autoSilenceValueBefore
            ? "`AutoSilenceNewJoins` was already `true`, so I've taken no action.\n\n"
            : "I've set `AutoSilenceNewJoins` to `true` and silenced the following recent joins:\n\n";
        msg += $"{RaidersDescription(recentJoins)}\n\n";
        msg += autoSilenceValueBefore
            ? PLEASE_ASSOFF
            : $"After {_config.LargeRaidDecay} minutes, if this raid appears to be over, I will automatically reset `AutoSilenceNewJoins` back to `false` (unless you run `.ass` before then).";

        await _modLog.CreateModLog(guild).SetContent(msg).SetFileLogContent(msg).Send();

        if (_config.LargeRaidDecay != null)
        {
            var action = new ScheduledEndRaidJob(true /* isLarge */);
            var job = new ScheduledJob(DateTimeHelper.UtcNow, DateTimeHelper.UtcNow.AddMinutes((double)_config.LargeRaidDecay), action);
            await _schedule.CreateScheduledJob(job);
        }
    }

    public async Task EndLargeRaid(IIzzyGuild guild)
    {
        if (_config.LargeRaidDecay == null) return; // Was disabled

        // Someone must have run.assoff already, so we don't need a "large raid is over" message.
        if (_generalStorage.CurrentRaidMode != RaidMode.Large) return;

        // Rather than "decay" separately from large to small and then to none, it's simpler to just say
        // a large raid doesn't end until we've fallen below the threshhold for any size of raid.
        var recentJoinCount = GetRecentJoins(guild).Count;
        if (recentJoinCount >= _config.SmallRaidSize)
        {
            _log.Log("Large raid is ongoing, inform mods it will have to be ended manually");

            var msg = TIME_SINCE_LARGE() + ", " + BUT_RECENT() + CONSIDER_ONGOING + "\n\n" + PLEASE_ASSOFF;
            await _modLog.CreateModLog(guild).SetContent(msg).SetFileLogContent(msg).Send();
        }
        else if (_generalStorage.ManualRaidSilence)
        {
            var msg = TIME_SINCE_LARGE() + ", but `.ass` was run in the meantime" + CONSIDER_ONGOING + "\n\n" + PLEASE_ASSOFF;
            await _modLog.CreateModLog(guild).SetContent(msg).SetFileLogContent(msg).Send();
        }
        else
        {
            _log.Log("Ending large raid");

            _generalStorage.CurrentRaidMode = RaidMode.None;
            _generalStorage.SuspectedRaiders.Clear();
            await FileHelper.SaveGeneralStorageAsync(_generalStorage);

            _config.AutoSilenceNewJoins = false;
            await FileHelper.SaveConfigAsync(_config);

            var msg = TIME_SINCE_LARGE() + ", " + AND_ONLY_RECENT() + CONSIDER_OVER + " " + ALARMS_ACTIVE;
            await _modLog.CreateModLog(guild).SetContent(msg).SetFileLogContent(msg).Send();
        }
    }

    public async Task ProcessMemberJoin(IIzzyGuildUser member)
    {
        if (member.Guild.Id != DiscordHelper.DefaultGuild()) return; // Don't process non-default server.
        if (!_config.RaidProtectionEnabled) return;
        if (_state.RecentJoins.Contains(member.Id))
        {
            _log.Log($"{member.DisplayName} ({member.Username}/{member.Id}) rejoined while still considered a recent join. Not updating recent joins list.");
            return;
        }

        _state.RecentJoins.Add(member.Id);
        var recentJoins = GetRecentJoins(member.Guild);

        if (recentJoins.Count >= _config.SmallRaidSize && _generalStorage.CurrentRaidMode == RaidMode.None)
            await TripSmallRaid(member.Guild, recentJoins);
        else if (recentJoins.Count >= _config.LargeRaidSize && _generalStorage.CurrentRaidMode != RaidMode.Large)
            await TripLargeRaid(member.Guild, recentJoins);
    }
}

public enum RaidMode
{
    None,
    Small,
    Large
}
