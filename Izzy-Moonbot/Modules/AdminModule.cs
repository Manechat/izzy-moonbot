using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;

namespace Izzy_Moonbot.Modules;

[Summary("Administration / Moderation related commands.")]
public class AdminModule : ModuleBase<SocketCommandContext>
{
    private readonly LoggingService _logger;
    private readonly Config _config;
    private readonly State _state;
    private readonly ScheduleService _schedule;
    private readonly Dictionary<ulong, User> _users;

    public AdminModule(LoggingService logger, Config config, Dictionary<ulong, User> users,
        State state, ScheduleService schedule)
    {
        _logger = logger;
        _config = config;
        _state = state;
        _schedule = schedule;
        _users = users;
    }

    [Command("panic")]
    [Summary("Immediately disconnects the client.")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task PanicCommand()
    {
        // Just closes the connection.
        await ReplyAsync("<a:izzywhat:891381404741550130>");
        Environment.Exit(255);
    }

    [Command("permanp")]
    [Summary(
        "Remove the scheduled new pony role removal for this user, essentially meaning they keep the new pony role until manually removed.")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task PermaNpCommandAsync(
        [Remainder] [Summary("The user to remove the scheduled removal from.")] string user = "")
    {
        if (user == "")
        {
            await ReplyAsync("You need to provide a user to remove the scheduled removal from.");
            return;
        }
        
        var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(user, Context);
        var member = Context.Guild.GetUser(userId);

        if (member == null)
        {
            await ReplyAsync("I couldn't find that user, sorry!");
            return;
        }

        var getSingleNewPonyRemoval = new Func<ScheduledJob, bool>(job =>
            job.Action.Type == ScheduledJobActionType.RemoveRole &&
            job.Action.Fields["userId"] == member.Id.ToString() &&
            job.Action.Fields["roleId"] == _config.NewMemberRole.ToString());

        if (_schedule.GetScheduledJobs(getSingleNewPonyRemoval).Any(getSingleNewPonyRemoval))
        {
            // Exists
            var job = _schedule.GetScheduledJob(getSingleNewPonyRemoval);

            await _schedule.DeleteScheduledJob(job);

            await ReplyAsync($"Removed the scheduled new pony role removal from <@{member.Id}>.");
        }
        else
        {
            await ReplyAsync(
                $"I couldn't find a scheduled new pony role removal for <@{member.Id}>. It either already occured or they already have permanent new pony.");
        }
    }

    [Command("scan")]
    [Summary("Refresh the stored userlist")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task ScanCommandAsync(
        [Summary("The type of scan to do")] [Remainder]
        string scanType = ""
    )
    {
        if (scanType.ToLower() == "full")
            Task.Run(async () =>
            {
                if (!Context.Guild.HasAllMembers) await Context.Guild.DownloadUsersAsync();

                var newUserCount = 0;
                var reloadUserCount = 0;
                var knownUserCount = 0;

                await foreach (var socketGuildUser in Context.Guild.Users.ToAsyncEnumerable())
                {
                    var skip = false;
                    if (!_users.ContainsKey(socketGuildUser.Id))
                    {
                        var newUser = new User();
                        newUser.Username = $"{socketGuildUser.Username}#{socketGuildUser.Discriminator}";
                        newUser.Aliases.Add(socketGuildUser.Username);
                        if (socketGuildUser.JoinedAt.HasValue) newUser.Joins.Add(socketGuildUser.JoinedAt.Value);
                        _users.Add(socketGuildUser.Id, newUser);
                        newUserCount += 1;
                        skip = true;
                    }
                    else
                    {
                        if (_users[socketGuildUser.Id].Username !=
                            $"{socketGuildUser.Username}#{socketGuildUser.Discriminator}")
                        {
                            _users[socketGuildUser.Id].Username =
                                $"{socketGuildUser.Username}#{socketGuildUser.Discriminator}";
                            if (!skip) reloadUserCount += 1;
                            skip = true;
                        }

                        if (!_users[socketGuildUser.Id].Aliases.Contains(socketGuildUser.DisplayName))
                        {
                            _users[socketGuildUser.Id].Aliases.Add(socketGuildUser.DisplayName);
                            if (!skip) reloadUserCount += 1;
                            skip = true;
                        }

                        if (socketGuildUser.JoinedAt.HasValue &&
                            !_users[socketGuildUser.Id].Joins.Contains(socketGuildUser.JoinedAt.Value))
                        {
                            _users[socketGuildUser.Id].Joins.Add(socketGuildUser.JoinedAt.Value);
                            if (!skip) reloadUserCount += 1;
                            skip = true;
                        }

                        if (_config.MemberRole != null)
                        {
                            if (_users[socketGuildUser.Id].Silenced &&
                                socketGuildUser.Roles.Select(role => role.Id).Contains((ulong)_config.MemberRole))
                            {
                                // Unsilenced, Remove the flag.
                                _users[socketGuildUser.Id].Silenced = false;
                                if (!skip) reloadUserCount += 1;
                                skip = true;
                            }

                            if (!_users[socketGuildUser.Id].Silenced &&
                                !socketGuildUser.Roles.Select(role => role.Id).Contains((ulong)_config.MemberRole))
                            {
                                // Silenced, add the flag
                                _users[socketGuildUser.Id].Silenced = true;
                                if (!skip) reloadUserCount += 1;
                                skip = true;
                            }
                        }

                        foreach (var roleId in _config.RolesToReapplyOnRejoin)
                        {
                            if (!_users[socketGuildUser.Id].RolesToReapplyOnRejoin.Contains(roleId) &&
                                socketGuildUser.Roles.Select(role => role.Id).Contains(roleId))
                            {
                                _users[socketGuildUser.Id].RolesToReapplyOnRejoin.Add(roleId);
                                if (!skip) reloadUserCount += 1;
                                skip = true;
                            }

                            if (_users[socketGuildUser.Id].RolesToReapplyOnRejoin.Contains(roleId) &&
                                !socketGuildUser.Roles.Select(role => role.Id).Contains(roleId))
                            {
                                _users[socketGuildUser.Id].RolesToReapplyOnRejoin.Remove(roleId);
                                if (!skip) reloadUserCount += 1;
                                skip = true;
                            }
                        }

                        foreach (var roleId in _users[socketGuildUser.Id].RolesToReapplyOnRejoin)
                        {
                            if (!socketGuildUser.Guild.Roles.Select(role => role.Id).Contains(roleId))
                            {
                                _users[socketGuildUser.Id].RolesToReapplyOnRejoin.Remove(roleId);
                                _config.RolesToReapplyOnRejoin.Remove(roleId);
                                await FileHelper.SaveConfigAsync(_config);
                                if (!skip) reloadUserCount += 1;
                                skip = true;
                            }
                            else
                            {

                                if (!_config.RolesToReapplyOnRejoin.Contains(roleId))
                                {
                                    _users[socketGuildUser.Id].RolesToReapplyOnRejoin.Remove(roleId);
                                    if (!skip) reloadUserCount += 1;
                                    skip = true;
                                }
                            }
                        }

                        if (!skip) knownUserCount += 1;
                    }
                }

                await FileHelper.SaveUsersAsync(_users);

                await Context.Message.ReplyAsync(
                    $"Done! I discovered {Context.Guild.Users.Count} members, of which{Environment.NewLine}" +
                    $"{newUserCount} were unknown to me until now,{Environment.NewLine}" +
                    $"{reloadUserCount} had out of date information,{Environment.NewLine}" +
                    $"and {knownUserCount} didn't need to be updated.");
            });
    }

    [Command("echo")]
    [Summary("Posts a message to a specified channel")]
    [ModCommand(Group = "Permission")]
    [DevCommand(Group = "Permission")]
    public async Task EchoCommandAsync([Summary("The channel to send to")] string channelName = "",
        [Remainder] [Summary("The message to send")]
        string message = "")
    {
        if (channelName == "")
        {
            await ReplyAsync("You must specify a channel name or a message.");
            return;
        }

        var channelId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);

        if (channelId > 0)
        {
            var channel = Context.Guild.GetTextChannel(channelId);
            if (message == "")
            {
                await ReplyAsync("There's no message to send there.");
                return;
            }

            if (channel != null)
            {
                await channel.SendMessageAsync(message);
                return;
            }


            await ReplyAsync("I can't send a message there.");
            return;
        }

        await ReplyAsync($"{channelName} {message}");
    }

    [Command("userinfo")]
    [Summary("Get information about a user")]
    [ModCommand(Group = "Permission")]
    [DevCommand(Group = "Permission")]
    public async Task UserInfoCommandAsync(
        [Remainder][Summary("The user to get information about")] string user = "")
    {
        if (user == "") user = Context.User.Id.ToString(); // Set to user ID to target self.

        var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(user, Context);
        var member = Context.Guild.GetUser(userId);

        if (member == null)
        {
            await ReplyAsync("I couldn't find that user, sorry!");
            return;
        }

        var output = $"";
        output += $"**User:** `<@{member.Id}>` {member.Username} ({member.Id}){Environment.NewLine}";
        output += $"**Names:** {string.Join(", ", _users[member.Id].Aliases)}{Environment.NewLine}";
        output += $"**Roles:** {string.Join(", ", member.Roles.Where(role => role.Id != Context.Guild.Id).Select(role => role.Name))}{Environment.NewLine}";
        output += $"**History:** ";
        output += $"Created <t:{member.CreatedAt.ToUnixTimeSeconds()}:R>";
        if (member.JoinedAt.HasValue)
        {
            output +=
                $", joined <t:{member.JoinedAt.Value.ToUnixTimeSeconds()}:R>";
        }

        output += $", last seen <t:{_users[member.Id].Timestamp.ToUnixTimeSeconds()}:R>{Environment.NewLine}";
        output += $"**Avatar(s):** {Environment.NewLine}";
        output += $"    Server: {member.GetGuildAvatarUrl() ?? "No server avatar found."}{Environment.NewLine}";
        output += $"    Global: {member.GetAvatarUrl() ?? "No global avatar found."}";

        await ReplyAsync(output, allowedMentions: AllowedMentions.None);
    }

    [Command("<mention>")]
    [Summary("Runs when someone mentions Izzy")]
    public async Task MentionCommandAsync()
    {
        if (!_config.MentionResponseEnabled) return; // Responding to mention is disabled.
        if ((DateTimeOffset.UtcNow - _state.LastMentionResponse).TotalMinutes < _config.MentionResponseCooldown)
            return; // Still on cooldown.

        var random = new Random();
        var index = random.Next(_config.MentionResponses.Count);
        var response = _config.MentionResponses[index]; // Random response

        _state.LastMentionResponse = DateTimeOffset.UtcNow;

        await ReplyAsync($"{response}");
    }

    [Command("stowaways")]
    [Summary("List users who do not have the member role.")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task StowawaysCommandAsync()
    {
        if (_config.MemberRole == null)
        {
            await ReplyAsync(
                "I'm unable to detect stowaways because the `MemberRole` config value is set to nothing.");
            return;
        }
            
        await Task.Run(async () =>
        {
            if (!Context.Guild.HasAllMembers) await Context.Guild.DownloadUsersAsync();

            var stowawayList = new HashSet<SocketGuildUser>();
            
            await foreach (var socketGuildUser in Context.Guild.Users.ToAsyncEnumerable())
            {
                if (socketGuildUser.IsBot) continue; // Bots aren't stowaways
                if (socketGuildUser.Roles.Select(role => role.Id).Contains(_config.ModRole)) continue; // Mods aren't stowaways

                if (!socketGuildUser.Roles.Select(role => role.Id).Contains((ulong)_config.MemberRole))
                {
                    // Doesn't have member role, add to stowaway list.
                    stowawayList.Add(socketGuildUser);
                }
            }

            if (stowawayList.Count == 0)
            {
                // There's no stowaways
                await ReplyAsync("I didn't find any stowaways.");
            }
            else
            {
                var stowawayStringList = stowawayList.Select(user => $"<@{user.Id}>");

                await ReplyAsync(
                    $"I found these following stowaways:{Environment.NewLine}{string.Join(", ", stowawayStringList)}");
            }
        });
    }

    [Command("ban")]
    [Summary(
        "Bans a user without deleting message history, and optionally schedules unbanning them at a later date. If the user is already banned, this will only schedule unbanning.")]
    [Remarks("Note that an \"indefinite\" ban, with no scheduled unban, is the same as banning with the regular Discord UI.")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task BanCommandAsync(
        [Summary("The user to ban")] string user = "",
        [Summary("How long the ban should last, e.g. \"2 weeks\" or \"6 months\". Omit for an indefinite ban.")]
        string duration = "")
    {
        if (user == "")
        {
            await ReplyAsync($"Please provide a user to ban. Refer to `{_config.Prefix}help ban` for more information.");
            return;
        }
        
        var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(user, Context);
        var member = Context.Guild.GetUser(userId);

        if (userId == Context.Client.CurrentUser.Id)
        {
            var rnd = new Random();
            if (rnd.Next(100) == 0)
            {
                await ReplyAsync("<:izzydeletethis:1028964499723661372>");
            }
            else if (rnd.NextSingle() > 0.5)
            {
                await ReplyAsync("<:sweetiebroken:399725081674383360>");
            }
            else
            {
                await ReplyAsync("<:izzysadness:910198257702031362>");
            }

            return;
        }

        if (member != null && member.Roles.Select(role => role.Id).Contains(_config.ModRole))
        {
            await ReplyAsync("I can't ban a mod. <:izzynothoughtsheadempty:910198222255972382>");
            return;
        }

        if (member != null && member.Hierarchy >= Context.Guild.GetUser(Context.Client.CurrentUser.Id).Hierarchy)
        {
            await ReplyAsync(
                "That user is either at the same level or higher than me in the role hierarchy, I cannot ban them. <:izzynothoughtsheadempty:910198222255972382>");
            return;
        }
        
        // Comprehend time
        TimeHelperResponse? time = null;
        try
        {
            if (duration != "")
                time = TimeHelper.Convert(duration);
        }
        catch (FormatException exception)
        {
            await ReplyAsync($"I encountered an error while attempting to comprehend time: {exception.Message.Split(": ")[1]}");
            return;
        }

        if (time is { Repeats: true })
        {
            await ReplyAsync("I can't ban a user repeatedly! Please give me a time that isn't repeating.");
            return;
        }

        // Okay, enough joking around, serious Izzy time
        var existingBan = await Context.Guild.GetBanAsync(userId);

        if (existingBan == null)
        {
            // No ban exists, very serious Izzy time.
            await Context.Guild.AddBanAsync(userId, pruneDays:0, reason:$"Banned by {Context.User.Username}#{Context.User.Discriminator}{(time == null ? "" : $" for {duration}")}.");

            if (time != null)
            {
                // Create scheduled task!
                Dictionary<string, string> fields = new Dictionary<string, string>
                {
                    { "userId", userId.ToString() }
                };
                var action = new ScheduledJobAction(ScheduledJobActionType.Unban, fields);
                var job = new ScheduledJob(DateTimeOffset.UtcNow, time.Time, action);
                await _schedule.CreateScheduledJob(job);
            }

            await ReplyAsync(
                $"<:izzydeletethis:1028964499723661372> I've banned {(member == null ? $"<@{userId}>" : member.DisplayName)} ({userId}).{(time != null ? $" They'll be unbanned <t:{time.Time.ToUnixTimeSeconds()}:R>." : "")}{Environment.NewLine}{Environment.NewLine}" +
                $"Here's a userlog I unicycled that you can use if you want to!{Environment.NewLine}```{Environment.NewLine}" +
                $"Type: Ban ({(duration == "" ? "" : $"{duration} ")}{(time == null ? "Indefinite" : $"<t:{time.Time.ToUnixTimeSeconds()}:R>")}){Environment.NewLine}" +
                $"User: <@{userId}> {(member != null ? $"({member.Username}#{member.Discriminator})" : "")} ({userId}){Environment.NewLine}" +
                $"Names: {(_users.ContainsKey(userId) ? string.Join(", ", _users[userId].Aliases) : "None (user isn't known by Izzy)")}{Environment.NewLine}" +
                $"```");
        }
        else
        {
            var getUserUnban = new Func<ScheduledJob, bool>(job =>
                job.Action.Type == ScheduledJobActionType.Unban &&
                job.Action.Fields["userId"] == userId.ToString());
            
            // ban exists, make sure a time is declared
            if (time == null)
            {
                // time not declared, make ban permanent.
                if (_schedule.GetScheduledJobs(getUserUnban).Any())
                {
                    var job = _schedule.GetScheduledJobs(getUserUnban).First();

                    await _schedule.DeleteScheduledJob(job);

                    await ReplyAsync($"This user is already banned. I have removed an existing unban for them which was scheduled <t:{job.ExecuteAt.ToUnixTimeSeconds()}:R>.{Environment.NewLine}{Environment.NewLine}" +
                                     $"Here's a userlog I unicycled that you can use if you want to!{Environment.NewLine}```{Environment.NewLine}" +
                                     $"Type: Ban (Indefinite){Environment.NewLine}" +
                                     $"User: <@{userId}> {(member != null ? $"({member.Username}#{member.Discriminator})" : "")} ({userId}){Environment.NewLine}" +
                                     $"Names: {(_users.ContainsKey(userId) ? string.Join(", ", _users[userId].Aliases) : "None (user isn't known by Izzy)")}{Environment.NewLine}" +
                                     $"```");
                }
                else
                {
                    // Doesn't exist, it's already permanent.
                    await ReplyAsync("This user is already banned, with no scheduled unban. No changes made.");
                }
                
                return;
            }
            
            // time declared, make ban temporary.
            if (_schedule.GetScheduledJobs(getUserUnban).Any())
            {
                var jobs = _schedule.GetScheduledJobs(getUserUnban);
                
                jobs.Sort((job1, job2) =>
                {
                    if (job1.ExecuteAt.ToUnixTimeMilliseconds() < job2.ExecuteAt.ToUnixTimeMilliseconds())
                    {
                        return -1;
                    }
                    return job1.ExecuteAt.ToUnixTimeMilliseconds() > job2.ExecuteAt.ToUnixTimeMilliseconds() ? 1 : 0;
                });

                var job = jobs[0];
                var jobOriginalExecution = job.ExecuteAt.ToUnixTimeSeconds();

                job.ExecuteAt = time.Time;

                await _schedule.ModifyScheduledJob(job.Id, job);

                await ReplyAsync($"This user is already banned. I have modified an existing scheduled unban for them from <t:{jobOriginalExecution}:R> to <t:{job.ExecuteAt.ToUnixTimeSeconds()}:R>.{Environment.NewLine}{Environment.NewLine}" +
                                 $"Here's a userlog I unicycled that you can use if you want to!{Environment.NewLine}```{Environment.NewLine}" +
                                 $"Type: Ban ({duration} <t:{time.Time.ToUnixTimeSeconds()}:R>){Environment.NewLine}" +
                                 $"User: <@{userId}> {(member != null ? $"({member.Username}#{member.Discriminator})" : "")} ({userId}){Environment.NewLine}" +
                                 $"Names: {(_users.ContainsKey(userId) ? string.Join(", ", _users[userId].Aliases) : "None (user isn't known by Izzy)")}{Environment.NewLine}" +
                                 $"```");
            }
            else
            {
                // Doesn't exist, it needs to exist.
                // Create scheduled task!
                Dictionary<string, string> fields = new Dictionary<string, string>
                {
                    { "userId", userId.ToString() }
                };
                var action = new ScheduledJobAction(ScheduledJobActionType.Unban, fields);
                var job = new ScheduledJob(DateTimeOffset.UtcNow, time.Time, action);
                await _schedule.CreateScheduledJob(job);

                await ReplyAsync(
                    $"This user is already banned. I have scheduled an unban for this user. They'll be unbanned <t:{time.Time.ToUnixTimeSeconds()}:R>{Environment.NewLine}{Environment.NewLine}" +
                    $"Here's a userlog I unicycled that you can use if you want to!{Environment.NewLine}```{Environment.NewLine}" +
                    $"Type: Ban ({duration} <t:{time.Time.ToUnixTimeSeconds()}:R>){Environment.NewLine}" +
                    $"User: <@{userId}> {(member != null ? $"({member.Username}#{member.Discriminator})" : "")} ({userId}){Environment.NewLine}" +
                    $"Names: {(_users.ContainsKey(userId) ? string.Join(", ", _users[userId].Aliases) : "None (user isn't known by Izzy)")}{Environment.NewLine}" +
                    $"```");
            }
        }
    }
}