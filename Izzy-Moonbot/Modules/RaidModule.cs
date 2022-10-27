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

    public RaidModule(Config config, RaidService raidService, State state,
        ScheduleService scheduleService, ModService modService)
    {
        _config = config;
        _raidService = raidService;
        _state = state;
        _scheduleService = scheduleService;
        _modService = modService;
    }

    [Command("ass")]
    [Summary("Change whether raidsilence is enabled or not.")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task AssAsync()
    {
        if (_state.CurrentRaidMode == RaidMode.None)
        {
            await ReplyAsync("There doesn't seem to be any raids going on...");
            return;
        }

        await _raidService.SilenceRecentJoins(Context);
        await ReplyAsync(
            $"I've enabled autosilencing new members! I also autosilenced those who joined earlier than {_config.RecentJoinDecay} seconds ago.");
    }

    [Command("assoff")]
    [Summary("Disable autosilencing on join and resets the raid mode.")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task AssOffAsync()
    {
        if (_state.CurrentRaidMode == RaidMode.None)
        {
            await ReplyAsync("There doesn't seem to be any raids going on...",
                messageReference: new MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id));
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
        if (_state.CurrentRaidMode == RaidMode.None)
        {
            await ReplyAsync("There doesn't seem to be any raids going on...",
                messageReference: new MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id));
            return;
        }

        var potentialRaiders = new List<string>();

        _raidService.GetRecentJoins(Context).ForEach(user =>
        {
            potentialRaiders.Add($"{user.Username}#{user.Discriminator}");
        });

        await ReplyAsync(
            $"I consider the following users as part of the current raid.{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", potentialRaiders)}{Environment.NewLine}```",
            messageReference: new MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id));
    }

    /*[Command("banraid")]
    [Summary("Ban all those considered part of the current raid. **ONLY USE AS LAST RESORT**")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task BanRaidAsync()
    {
        if (_state.CurrentRaidMode == RaidMode.NONE)
        {
            await ReplyAsync("There doesn't seem to be any raids going on...", messageReference: new Discord.MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id));
            return;
        }

        List<string> potentialRaiders = new List<string>();

        _raidService.GetRecentJoins(Context).ForEach((user) =>
        {
            potentialRaiders.Add($"{user.Username}#{user.Discriminator}");

            Context.Guild.AddBanAsync(user, pruneDays: 7, reason: "Ban Raid Command");
        });

        await ReplyAsync($"I consider the following users as part of the current raid and thus have been banned.{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", potentialRaiders)}{Environment.NewLine}```", messageReference: new Discord.MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id));
    }*/
}