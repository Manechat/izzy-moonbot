using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;

namespace Izzy_Moonbot.Modules;

[Summary("Moderator-only commands that are either infrequently used or just for fun.")]
public class ModMiscModule : ModuleBase<SocketCommandContext>
{
    private readonly Config _config;
    private readonly ScheduleService _schedule;
    private readonly Dictionary<ulong, User> _users;

    public ModMiscModule(Config config, Dictionary<ulong, User> users, ScheduleService schedule)
    {
        _config = config;
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
    [Parameter("user", ParameterType.User, "The user to remove the scheduled removal from.")]
    public async Task PermaNpCommandAsync(
        [Remainder]string user = "")
    {
        if (user == "")
        {
            await ReplyAsync(
                "Hey uhh... I can't remove the scheduled new pony role removal for a user if you haven't given me the user to remove it from...");
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
            job.Action is ScheduledRoleRemovalJob removalJob &&
            removalJob.User == member.Id &&
            removalJob.Role == _config.NewMemberRole);

        var job = _schedule.GetScheduledJob(getSingleNewPonyRemoval);
        if (job != null)
        {
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
    public async Task ScanCommandAsync()
    {
        var _ = Task.Run(async () =>
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
    [Summary("Posts a message (and/or sticker) to a specified channel")]
    [ModCommand(Group = "Permission")]
    [DevCommand(Group = "Permission")]
    [Parameter("channel", ParameterType.Channel, "The channel to send the message to.", true)]
    [Parameter("content", ParameterType.String, "The message to send.")]
    public async Task EchoCommandAsync(
        [Remainder] string argsString = "")
    {
        await TestableEchoCommandAsync(
            new SocketCommandContextAdapter(Context),
            argsString,
            // retrieving the stickers requires knowing that Context.Message is a SocketUserMessage instead of any other
            // IUserMessage, which is not worth the hassle of trying to simulate in tests when we're only going to echo them
            Context.Message.Stickers.Where(s => s.IsAvailable ?? false).ToArray()
        );
    }

    public async Task TestableEchoCommandAsync(
        IIzzyContext context,
        string argsString = "",
        ISticker[]? stickers = null)
    {
        var args = DiscordHelper.GetArguments(argsString);

        var channelName = args.Arguments.FirstOrDefault("");
        var message = "";
        try
        {
            message = string.Join("", argsString.Skip(args.Indices[0]));
            message = DiscordHelper.StripQuotes(message);
        }
        catch
        {
            message = "";
        }

        var channelId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, context);
        if (channelId == 0)
        {
            message = DiscordHelper.StripQuotes(argsString);
        }

        if (message == "" && (stickers is null || !stickers.Any()))
        {
            await context.Channel.SendMessageAsync("You must provide either a non-empty message or an available sticker for me to echo.");
            return;
        }

        if (channelId > 0)
        {
            var channel = context.Guild?.GetTextChannel(channelId);
            if (channel != null)
            {
                await channel.SendMessageAsync(message, stickers: stickers);
                return;
            }

            await context.Channel.SendMessageAsync("I can't send a message there.");
            return;
        }

        await context.Channel.SendMessageAsync(message, stickers: stickers);
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

            var stowawaySet = new HashSet<SocketGuildUser>();
            
            await foreach (var socketGuildUser in Context.Guild.Users.ToAsyncEnumerable())
            {
                if (socketGuildUser.IsBot) continue; // Bots aren't stowaways
                if (socketGuildUser.Roles.Select(role => role.Id).Contains(_config.ModRole)) continue; // Mods aren't stowaways

                if (!socketGuildUser.Roles.Select(role => role.Id).Contains((ulong)_config.MemberRole))
                {
                    // Doesn't have member role, add to stowaway set.
                    stowawaySet.Add(socketGuildUser);
                }
            }

            if (stowawaySet.Count == 0)
            {
                // There's no stowaways
                await ReplyAsync("I didn't find any stowaways.");
            }
            else
            {
                var stowawayStringList = stowawaySet.Select(user => $"<@{user.Id}>");

                await ReplyAsync(
                    $"I found these following stowaways:{Environment.NewLine}{string.Join(", ", stowawayStringList)}");
            }
        });
    }

    [Command("schedule")]
    [Summary("View and modify Izzy's scheduler.")]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    [Parameter("[...]", ParameterType.Complex, "")]
    public async Task ScheduleCommandAsync([Remainder]string argsString = "")
    {
        await TestableScheduleCommandAsync(
            new SocketCommandContextAdapter(Context),
            argsString
        );
    }

    public async Task TestableScheduleCommandAsync(
        IIzzyContext context,
        string argsString = "")
    {
        var jobTypes = new Dictionary<string, Type>
        {
            { "remove-role", typeof(ScheduledRoleRemovalJob) },
            { "add-role", typeof(ScheduledRoleAdditionJob) },
            { "unban", typeof(ScheduledUnbanJob) },
            { "echo", typeof(ScheduledEchoJob) },
            { "banner", typeof(ScheduledBannerRotationJob) },
        };
        var supportedJobTypesMessage = $"The currently supported job types are: {string.Join(", ", jobTypes.Keys.Select(k => $"`{k}`"))}";

        if (argsString == "")
        {
            await context.Channel.SendMessageAsync(
                $"Heya! Here's a list of subcommands for {_config.Prefix}schedule!\n" +
                $"\n" +
                $"`{_config.Prefix}schedule list [jobtype]` - Show all scheduled jobs (or all jobs of the specified type) in a Discord message.\n" +
                $"`{_config.Prefix}schedule list-file [jobtype]` - Post a text file attachment listing all scheduled jobs (or all jobs of the specified type).\n" +
                $"`{_config.Prefix}schedule about <jobtype>` - Get information about a job type, including the `.schedule add` syntax to create one.\n" +
                $"`{_config.Prefix}schedule about <id>` - Get information about a specific scheduled job by its ID.\n" +
                $"`{_config.Prefix}schedule add <jobtype> <date/time> [...]` - Create and schedule a job. Run `{_config.Prefix}schedule about <jobtype>` to figure out the arguments.\n" +
                $"`{_config.Prefix}schedule remove <id>` - Remove a scheduled job by its ID.\n" +
                $"\n" +
                supportedJobTypesMessage);
            return;
        }

        var args = DiscordHelper.GetArguments(argsString);
        
        if (args.Arguments[0].ToLower() == "list")
        {
            if (args.Arguments.Length == 1)
            {
                // All
                var jobs = _schedule.GetScheduledJobs().Select(job => job.ToDiscordString()).ToList();
                if (jobs.Count > 10)
                {
                    // Use pagination
                    var pages = new List<string>();
                    var pageNumber = -1;
                    for (var i = 0; i < jobs.Count; i++)
                    {
                        if (i % 10 == 0)
                        {
                            pageNumber += 1;
                            pages.Add("");
                        }

                        pages[pageNumber] += $"{jobs[i]}\n";
                    }

                    string[] staticParts =
                    {
                        "Heya! Here's a list of all the scheduled jobs!",
                        "If you need a raw text list, run `.schedule list-file`."
                    };

                    var paginationMessage =
                        new PaginationHelper(context, pages.ToArray(), staticParts, codeblock: false, allowedMentions: AllowedMentions.None);
                }
                else
                {
                    await context.Channel.SendMessageAsync(
                        $"Heya! Here's a list of all the scheduled jobs!\n\n" +
                        string.Join(Environment.NewLine, jobs) +
                        $"\n\nIf you need a raw text file, run `.schedule list-file`.", allowedMentions: AllowedMentions.None);
                }
            }
            else
            {
                // Specific job type
                var jobType = string.Join("", argsString.Skip(args.Indices[0]));

                if (jobTypes[jobType] is not Type type)
                {
                    await context.Channel.SendMessageAsync(
                        $"There is no \"{jobType}\" job type.\n{supportedJobTypesMessage}");
                    return;
                }

                var jobs = _schedule.GetScheduledJobs().Where(job => job.Action.GetType().FullName == type.FullName).Select(job => job.ToDiscordString()).ToList();
                if (jobs.Count > 10)
                {
                    // Use pagination
                    var pages = new List<string>();
                    var pageNumber = -1;
                    for (var i = 0; i < jobs.Count; i++)
                    {
                        if (i % 10 == 0)
                        {
                            pageNumber += 1;
                            pages.Add("");
                        }

                        pages[pageNumber] += $"{jobs[i]}\n";
                    }

                    string[] staticParts =
                    {
                        $"Heya! Here's a list of all the scheduled {jobType} jobs!",
                        $"If you need a raw text list, run `.schedule list-file {jobType}`."
                    };

                    var paginationMessage =
                        new PaginationHelper(context, pages.ToArray(), staticParts, codeblock: false, allowedMentions: AllowedMentions.None);
                }
                else
                {
                    await context.Channel.SendMessageAsync(
                        $"Heya! Here's a list of all the scheduled {jobType} jobs!\n\n" +
                        string.Join(Environment.NewLine, jobs) +
                        $"\n\nIf you need a raw text list, run `.schedule list-file {jobType}`.", allowedMentions: AllowedMentions.None);
                }
            }
        }
        else if (args.Arguments[0].ToLower() == "list-file")
        {
            if (args.Arguments.Length == 1)
            {
                // All
                var jobs = _schedule.GetScheduledJobs().Select(job => job.ToFileString()).ToList();
                
                var s = new MemoryStream(Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, jobs)));
                var fa = new FileAttachment(s, $"all_scheduled_jobs_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.txt");

                await context.Channel.SendFileAsync(fa, $"Here's the file list of all scheduled jobs!");
            }
            else
            {
                // Specific job type
                var jobType = string.Join("", argsString.Skip(args.Indices[0]));

                if (jobTypes[jobType] is not Type type)
                {
                    await context.Channel.SendMessageAsync(
                        $"There is no \"{jobType}\" job type.\n{supportedJobTypesMessage}");
                    return;
                }
                
                var jobs = _schedule.GetScheduledJobs().Where(job => job.Action.GetType().FullName == type.FullName).Select(job => job.ToFileString()).ToList();
                
                var s = new MemoryStream(Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, jobs)));
                var fa = new FileAttachment(s, $"{jobType}_scheduled_jobs_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.txt");

                await context.Channel.SendFileAsync(fa, $"Here's the file list of all scheduled {jobType} jobs!");
            }
        }
        else if (args.Arguments[0].ToLower() == "about")
        {
            var searchString = string.Join("", argsString.Skip(args.Indices[0]));

            if (searchString == "")
            {
                await context.Channel.SendMessageAsync("You need to provide either a job type, or an ID for a specific job.");
                return;
            }
            
            // Check IDs first
            var potentialJob = _schedule.GetScheduledJob(searchString);
            if (potentialJob != null)
            {
                // Not null, this job exists. Display information about it.
                var jobType = jobTypes.First(kv => kv.Value == potentialJob.Action.GetType()).Key;

                var expandedJobInfo = potentialJob.Action switch
                {
                    ScheduledRoleJob roleJob => $"Target user: <@{roleJob.User}>\n" +
                                                $"Target role: <@&{roleJob.Role}>\n" +
                                                $"{(roleJob.Reason != null ? $"Reason: {roleJob.Reason}\n" : "")}",
                    ScheduledUnbanJob unbanJob => $"Target user: <@{unbanJob.User}>\n",
                    ScheduledEchoJob echoJob => $"Target channel/user: <#{echoJob.Channel}> / <@{echoJob.Channel}>\n" +
                                                $"Content:\n```\n{echoJob.Content}\n```\n",
                    ScheduledBannerRotationJob => $"Current banner mode: {_config.BannerMode}\n" +
                                                  $"Configure this job via `.config`.\n",
                    _ => ""
                };
                
                var expandedRepeatInfo = potentialJob.RepeatType switch
                {
                    ScheduledJobRepeatType.None => "",
                    ScheduledJobRepeatType.Relative => ConstructRelativeRepeatTimeString(potentialJob) + "\n",
                    ScheduledJobRepeatType.Daily => $"Every day at {potentialJob.ExecuteAt:T} UTC\n",
                    ScheduledJobRepeatType.Weekly => $"Every week at {potentialJob.ExecuteAt:T} on {potentialJob.ExecuteAt:dddd}\n",
                    ScheduledJobRepeatType.Yearly => $"Every year at {potentialJob.ExecuteAt:T} on {potentialJob.ExecuteAt:dd MMMM}\n",
                    _ => throw new NotImplementedException("Unknown repeat type.")
                };
                
                await context.Channel.SendMessageAsync(
                    $"Here's information regarding the scheduled job with ID of `{potentialJob.Id}`:\n" +
                    $"Job type: {jobType}\n" +
                    $"Created <t:{potentialJob.CreatedAt.ToUnixTimeSeconds()}:F>\n" +
                    $"Executes <t:{potentialJob.ExecuteAt.ToUnixTimeSeconds()}:R>\n" +
                    $"{expandedRepeatInfo}" +
                    $"{expandedJobInfo}", allowedMentions: AllowedMentions.None);
            }
            else
            {
                // Not an id, so must be a job type
                if (jobTypes[searchString] is not Type type)
                {
                    await context.Channel.SendMessageAsync(
                        $"There is no \"{searchString}\" job ID or job type.\n{supportedJobTypesMessage}");
                    return;
                }
                
                var content = "";
                switch (type.Name)
                {
                    case "ScheduledRoleRemovalJob":
                        content = "**Role Removal**\n" +
                                  "*Removes a role from a user after a specified amount of time.*\n" +
                                  "Creation syntax:\n" +
                                  "```\n" +
                                  $"{_config.Prefix}schedule add {searchString} <date/time> <user> <role> [reason]\n" +
                                  "```\n" +
                                  "`user` - The user to remove the role from.\n" +
                                  "`role` - The role to remove.\n" +
                                  "`reason` - Optional reason.";
                        break;
                    case "ScheduledRoleAdditionJob":
                        content = "**Role Addition**\n" +
                                  "*Adds a role to a user in a specified amount of time.*\n" +
                                  "Creation syntax:\n" +
                                  "```\n" +
                                  $"{_config.Prefix}schedule add {searchString} <date/time> <user> <role> [reason]\n" +
                                  "```\n" +
                                  "`user` - The user to add the role to.\n" +
                                  "`role` - The role to add.\n" +
                                  "`reason` - Optional reason.";
                        break;
                    case "ScheduledUnbanJob":
                        content = "**Unban User**\n" +
                                  "*Unbans a user after a specified amount of time.*\n" +
                                  "Creation syntax:\n" +
                                  "```\n" +
                                  $"{_config.Prefix}schedule add {searchString} <date/time> <user>\n" +
                                  "```\n" +
                                  "`user` - The user to unban.";
                        break;
                    case "ScheduledEchoJob":
                        content = "**Echo**\n" +
                                  "*Sends a message in a channel, or to a users DMs.*\n" +
                                  "Creation syntax:\n" +
                                  "```\n" +
                                  $"{_config.Prefix}schedule add {searchString} <date/time> <channel/user> <content>\n" +
                                  "```\n" +
                                  "`channel/user` - Either the channel or user to send the message to.\n" +
                                  "`content` - The message to send.";
                        break;
                    case "ScheduledBannerRotationJob":
                        content = "**Banner Rotation**\n" +
                                  "*Runs banner rotation, or checks Manebooru for featured image depending on `BannerMode`.*\n" +
                                  ":warning: This scheduled job is managed by Izzy internally. It is best not to modify it with this command.\n" +
                                  "Creation syntax:\n" +
                                  "```\n" +
                                  $"{_config.Prefix}schedule add {searchString} <date/time>\n" +
                                  "```";
                        break;
                    default:
                        content = "**Unknown type**\n" +
                                  "*I don't know what this type is?*";
                        break;
                }

                await context.Channel.SendMessageAsync(content);
            }
        } 
        else if (args.Arguments[0].ToLower() == "add")
        {
            await context.Channel.SendMessageAsync($":warning: This subcommand isn't written yet, as other features have higher priority than it. Please ask Cloudburst (Leah) to add the job you wish to add.");
        }
        else if (args.Arguments[0].ToLower() == "remove")
        {
            var searchString = string.Join("", argsString.Skip(args.Indices[0]));

            if (searchString == "")
            {
                await context.Channel.SendMessageAsync("You need to provide an ID for a specific scheduled job.");
                return;
            }
            
            // Check IDs first
            var potentialJob = _schedule.GetScheduledJob(searchString);
            if (potentialJob == null)
            {
                await context.Channel.SendMessageAsync("Sorry, I couldn't find that job.");
                return;
            }

            try
            {
                await _schedule.DeleteScheduledJob(potentialJob);

                await context.Channel.SendMessageAsync("Successfully deleted scheduled job.");
            }
            catch (NullReferenceException)
            {
                await context.Channel.SendMessageAsync("Sorry, I couldn't find that job.");
            }
        }
    } 
    
    private static string ConstructRelativeRepeatTimeString(ScheduledJob job)
    {
        var secondsBetweenExecution = job.ExecuteAt.ToUnixTimeSeconds() - (job.LastExecutedAt?.ToUnixTimeSeconds() ?? job.CreatedAt.ToUnixTimeSeconds());

        var seconds = Math.Floor(double.Parse(secondsBetweenExecution.ToString()) % 60);
        var minutes = Math.Floor(double.Parse(secondsBetweenExecution.ToString()) / 60 % 60);
        var hours = Math.Floor(double.Parse(secondsBetweenExecution.ToString()) / 60 / 60 % 24);
        var days = Math.Floor(double.Parse(secondsBetweenExecution.ToString()) / 60 / 60 / 24);

        return $"Executes every {(days == 0 ? "" : $"{days} Day{(days is < 1.9 and > 0.9 ? "" : "s")}")} " +
               $"{(hours == 0 ? "" : $"{hours} Hour{(hours is < 1.9 and > 0.9 ? "" : "s")}")} " +
               $"{(minutes == 0 ? "" : $"{minutes} Minute{(minutes is < 1.9 and > 0.9 ? "" : "s")}")} " +
               $"{(seconds == 0 ? "" : $"{seconds} Second{(seconds is < 1.9 and > 0.9 ? "" : "s")}")}";
    }
}