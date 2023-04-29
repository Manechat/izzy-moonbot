using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;

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
        _config.AutoSilenceNewJoins = true;
        await FileHelper.SaveConfigAsync(_config);

        var recentJoins = _raidService.GetRecentJoins(Context.Guild);
        await _modService.SilenceUsers(recentJoins, "Suspected raider");

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

    [Command("getraid")]
    [Summary("Get a list of those considered to be part of the current raid.")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task GetRaidAsync()
    {
        if (_generalStorage.CurrentRaidMode == RaidMode.None)
        {
            await ReplyAsync("There don't seem to be any raids going on...");
            return;
        }

        var potentialRaiders = new List<string>();

        _raidService.GetRecentJoins(Context.Guild).ForEach(user =>
        {
            potentialRaiders.Add($"{user.Username}#{user.Discriminator}");
        });

        await ReplyAsync(
            $"I consider the following users as part of the current raid.\n```\n{string.Join(", ", potentialRaiders)}\n```");
    }
}