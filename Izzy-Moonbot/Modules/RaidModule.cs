using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Izzy_Moonbot.Attributes;
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
    [Summary("Set `AutoSilenceNewJoins` to `true` and silence those I consider recent joins (if there's a raid ongoing).")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task AssAsync()
    {
        if (_generalStorage.CurrentRaidMode == RaidMode.None)
        {
            await ReplyAsync("There don't seem to be any raids going on...");
            return;
        }

        await _raidService.SilenceRecentJoins(Context);
        await ReplyAsync(
            $"I've enabled autosilencing new members! I also silenced those who joined earlier than {_config.RecentJoinDecay} seconds ago.");
    }

    [Command("assoff")]
    [Summary("Set `AutoSilenceNewJoins` to `false` and stop me from thinking that a raid is ongoing (if there's a raid ongoing).")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task AssOffAsync()
    {
        if (_generalStorage.CurrentRaidMode == RaidMode.None)
        {
            await ReplyAsync("There doesn't seem to be any raids going on...");
            return;
        }

        await _raidService.EndRaid(Context);

        await ReplyAsync($"Jinxie avoided! I'm returning to normal operation.");
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

        _raidService.GetRecentJoins(Context).ForEach(user =>
        {
            potentialRaiders.Add($"{user.Username}#{user.Discriminator}");
        });

        await ReplyAsync(
            $"I consider the following users as part of the current raid.{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", potentialRaiders)}{Environment.NewLine}```");
    }
}