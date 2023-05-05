using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Serilog;

namespace Izzy_Moonbot.Modules;

[Summary("Anti-raid related commands.")]
public class RaidModule : ModuleBase<SocketCommandContext>
{
    private readonly ModService _modService;
    private readonly RaidService _raidService;
    private readonly ScheduleService _scheduleService;
    private readonly Config _config;
    private readonly State _state;
    private readonly GeneralStorage _generalStorage;

    public RaidModule(Config config, RaidService raidService, State state,
        ScheduleService scheduleService, ModService modService, GeneralStorage generalStorage)
    {
        _config = config;
        _raidService = raidService;
        _state = state;
        _scheduleService = scheduleService;
        _modService = modService;
        _generalStorage = generalStorage;
    }

    [Command("ass")]
    [Summary("Set `AutoSilenceNewJoins` to `true` and silence recent joins (as defined by `.config RecentJoinDecay`).")]
    [Remarks("This is usually used after Izzy detects a spike of recent joins, but it can be used at any time since 'slow trickle raids' occasionally happen too.\n" +
        "Using this command also ensures Izzy will not change `AutoSilenceNewJoins` to `false` until a moderator runs `.assoff`.")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task AssAsync()
    {
        var recentJoins = _raidService.GetRecentJoins(Context.Guild);
        try
        {
            await _modService.SilenceUsers(recentJoins, await DiscordHelper.AuditLogForCommand(Context));
        }
        catch (Exception ex)
        {
            await ReplyAsync($"Failed to silence recent joins:\n{ex.Message}");
            return;
        }

        _config.AutoSilenceNewJoins = true;
        await FileHelper.SaveConfigAsync(_config);

        _generalStorage.ManualRaidSilence = true;
        await FileHelper.SaveGeneralStorageAsync(_generalStorage);

        await ReplyAsync($"I've set `AutoSilenceNewJoins` to `true` and silenced the following recent joins: {string.Join(' ', recentJoins.Select(u => $"<@{u.Id}>"))}");
    }

    [Command("assoff")]
    [Summary("Set `AutoSilenceNewJoins` to `false`.")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task AssOffAsync()
    {
        _config.AutoSilenceNewJoins = false;
        await FileHelper.SaveConfigAsync(_config);

        _generalStorage.CurrentRaidMode = RaidMode.None;
        _generalStorage.ManualRaidSilence = false;
        await FileHelper.SaveGeneralStorageAsync(_generalStorage);

        await ReplyAsync($"Jinxie avoided! I've set `AutoSilenceNewJoins` back to `false`");
    }

    [Command("getrecentjoins")]
    [Summary("Get a list of recent joins (as defined by `.config RecentJoinDecay`).")]
    [Remarks("These are the users who will be silenced if you run `.ass`")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task GetRecentJoinsAsync()
    {
        var recentJoins = _raidService.GetRecentJoins(Context.Guild);

        await ReplyAsync($"The following users are recent joins:\n" +
            "```\n" +
            string.Join(", ", recentJoins.Select(user => $"{user.Username}#{user.Discriminator}")) + "\n" +
            "```");
    }
}