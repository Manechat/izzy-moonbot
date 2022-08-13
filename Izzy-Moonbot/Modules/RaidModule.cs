using System.Linq;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
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
        private readonly ServerSettings _settings;
        private readonly RaidService _raidService;
        private readonly ScheduleService _scheduleService;
        private readonly ModService _modService;
        private readonly StateStorage _state;

        public RaidModule(ServerSettings settings, RaidService raidService, StateStorage state, ScheduleService scheduleService, ModService modService)
        {
            _settings = settings;
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

            var userList = await _raidService.EndRaid(Context);
            
            var stowawaysString =
                $"{Environment.NewLine}These users were autosilenced. Please run `{_settings.Prefix}stowaways fix` while replying to this message to unsilence or `{_settings.Prefix}stowaways kick` while replying to this message to kick.{Environment.NewLine}{string.Join(", ", userList)}{Environment.NewLine}||!stowaway-usable!||";
            if (!userList.Any()) stowawaysString = "";
            
            await ReplyAsync($"Jinxie avoided! I'm returning to normal operation.{stowawaysString}", messageReference: new Discord.MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id));
        }

        [Command("stowaways")]
        [Summary("Process users who were autosilenced by a raid.")]
        [RequireContext(ContextType.Guild)]
        [ModCommand(Group = "Permissions")]
        [DevCommand(Group = "Permissions")]
        public async Task StowawaysAsync([Summary("Test Identifier")] string task = "")
        {
            if (task == "")
            {
                string fixOption = $"`fix` - Unsilence stowaways and give them <@&{_settings.NewMemberRole}> for {_settings.NewMemberRoleDecay} minutes.";
                if (_settings.NewMemberRole == null) fixOption = $"`fix` - Unsilence stowaways.";

                await ReplyAsync(
                    $"`This command processes users who were autosilenced by a raid.{Environment.NewLine}" +
                    $"To use, run `{_settings.Prefix}stowaways <action>` while replying to a raid end message." +
                    $"`<action>` can be one of the following:{Environment.NewLine}" +
                    $"{fixOption}{Environment.NewLine}" +
                    $"`kick` - Kick stowaways");
                return;
            }
            
            task = task.ToLower();
            if (task != "fix" && task != "kick")
            {
                string fixOption = $"`fix` - Unsilence stowaways and give them <@&{_settings.NewMemberRole}> for {_settings.NewMemberRoleDecay} minutes.";
                if (_settings.NewMemberRole == null) fixOption = $"`fix` - Unsilence stowaways.";

                await ReplyAsync(
                    $"`{task}` is not a valid action to take on stowaways. Please use one of the following{Environment.NewLine}" +
                    $"{fixOption}{Environment.NewLine}" +
                    $"`kick` - Kick stowaways");
                return;
            }

            if (task == "fix" && _settings.MemberRole == null)
            {
                await ReplyAsync(
                    $"`{task}` is not a valid action to take on stowaways at the current time as `MemberRole` is not set. Please set it before continuing.");
                return;
            }

            if (Context.Message.ReferencedMessage == null)
            {
                await ReplyAsync("Please reply to a raid end message in order to process stowaways from a raid.");
                return;
            }

            if (Context.Message.ReferencedMessage.CleanContent.Split(Environment.NewLine).Length != 4 || !Context.Message.Content.EndsWith("||!stowaway-usable!||"))
            {
                await ReplyAsync("I'm... not entirely sure if that's an actual raid end message...");
                return;
            }
            
            var toProcess = Context.Message.ReferencedMessage.CleanContent.Split(Environment.NewLine)[2].Split(", ").ToList();
            var users = new List<SocketGuildUser>();
            
            var unprocessable = 0;
            var processable = 0;
            
            foreach (var userResolvable in toProcess)
            {
                if (userResolvable.StartsWith("Unknown user"))
                {
                    unprocessable++;
                    continue;
                }

                var userId = new Regex("<@([0-9]+)>").Match(userResolvable).Groups[0].Value;
                Console.WriteLine(userId);
                bool result = uint.TryParse(userId, out var uId);
                if (!result)
                {
                    unprocessable++;
                    continue;
                }
                var user = Context.Guild.GetUser(uId);
                if (user == null)
                {
                    unprocessable++;
                    continue;
                }

                processable++;
                users.Add(user);
            }

            var msgContent = "";
            switch (task)
            {
                case "fix":
                    msgContent = $"Giving {users.Count} users the Member and New Pony role ({unprocessable} users were unprocessable). Please wait... <a:rdloop:910875692785336351>";
                    if (_settings.NewMemberRole == null) msgContent = $"Giving {users.Count} users the Member role ({unprocessable} users were unprocessable). Please wait... <a:rdloop:910875692785336351>";
                    break;
                case "kick":
                    msgContent = $"Kicking {users.Count} users ({unprocessable} users were unprocessable). Please wait... <a:rdloop:910875692785336351>";
                    break;
            }

            var message = await ReplyAsync(msgContent);

            _settings.BatchSendLogs = true;
            
            switch (task)
            {
                case "fix":
                    var roles = new List<ulong>();
                    
                    if (_settings.NewMemberRole != null)
                    {
                        roles.Add((ulong)_settings.NewMemberRole);
                    }
                    
                    if (_settings.MemberRole != null)
                    {
                        roles.Add((ulong)_settings.MemberRole);
                    }
                    
                    foreach (var socketGuildUser in users)
                    {
                        if (_settings.NewMemberRole != null)
                        {
                            var expiresString =
                                $"{Environment.NewLine}New Member role expires in <t:{(DateTimeOffset.UtcNow + TimeSpan.FromMinutes(_settings.NewMemberRoleDecay)).ToUnixTimeSeconds()}:R>";

                            Dictionary<string, string> fields = new Dictionary<string, string>
                            {
                                { "roleId", _settings.NewMemberRole.ToString() },
                                { "userId", socketGuildUser.Id.ToString() },
                                {
                                    "reason",
                                    $"{_settings.NewMemberRoleDecay} minutes (`NewMemberRoleDecay`) passed, user no longer a new pony."
                                }
                            };
                            ScheduledTaskAction action =
                                new ScheduledTaskAction(ScheduledTaskActionType.RemoveRole, fields);
                            ScheduledTask scheduledTask = new ScheduledTask(DateTimeOffset.UtcNow,
                                (DateTimeOffset.UtcNow + TimeSpan.FromMinutes(_settings.NewMemberRoleDecay)), action);
                            _scheduleService.CreateScheduledTask(scheduledTask, socketGuildUser.Guild);
                        }
                    }

                    await _modService.AddRolesToUsers(users, roles, DateTimeOffset.Now, "Fixing stowaways");
                    await ReplyAsync($"I've fixed {users.Count} stowaways.");
                    break;
                case "kick":
                    await _modService.KickUsers(users, DateTimeOffset.Now, "Kicking stowaways");
                    await ReplyAsync($"I've kicked {users.Count} stowaways.");
                    break;
            }
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
