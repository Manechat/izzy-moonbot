using Izzy_Moonbot.Attributes;

namespace Izzy_Moonbot.Modules
{
    using Discord.Commands;
    using Izzy_Moonbot.Helpers;
    using Izzy_Moonbot.Service;
    using Izzy_Moonbot.Settings;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    [Summary("Module for interacting with the antiraid services.")]
    public class RaidModule : ModuleBase<SocketCommandContext>
    {
        private ServerSettings _settings;
        private readonly RaidService _raidService;
        private StateStorage _state;

        public RaidModule(ServerSettings settings, RaidService raidService, StateStorage state)
        {
            _settings = settings;
            _raidService = raidService;
            _state = state;
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
                await ReplyAsync("There doesn't seem to be any raids going on...", messageReference: new Discord.MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id));
                return;
            }

            await _raidService.SilenceRecentJoins(Context);
            await ReplyAsync($"I've enabled autosilencing new members! I also autosilenced those who joined earlier than {_settings.RecentJoinDecay} seconds ago.", messageReference: new Discord.MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id));
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
                await ReplyAsync("There doesn't seem to be any raids going on...", messageReference: new Discord.MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id));
                return;
            }

            await _raidService.EndRaid(Context);
            
            await ReplyAsync($"Jinxie avoided! I'm returning to normal operation.{Environment.NewLine}", messageReference: new Discord.MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id));
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
                await ReplyAsync("There doesn't seem to be any raids going on...", messageReference: new Discord.MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id));
                return;
            }

            List<string> potentialRaiders = new List<string>();

            _raidService.GetRecentJoins(Context).ForEach((user) =>
            {
                potentialRaiders.Add($"{user.Username}#{user.Discriminator}");
            });

            await ReplyAsync($"I consider the following users as part of the current raid.{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", potentialRaiders)}{Environment.NewLine}```", messageReference: new Discord.MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id));
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
}
