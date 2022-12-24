﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Describers;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;

namespace Izzy_Moonbot.Modules;

[Summary("The need-to-know moderator-only commands.")]
public class ModCoreModule : ModuleBase<SocketCommandContext>
{
    private readonly LoggingService _logger;
    private readonly Config _config;
    private readonly ScheduleService _schedule;
    private readonly Dictionary<ulong, User> _users;
    private readonly ModService _mod;
    private readonly ConfigDescriber _configDescriber;

    public ModCoreModule(LoggingService logger, Config config, Dictionary<ulong, User> users,
        ScheduleService schedule, ModService mod, ConfigDescriber configDescriber)
    {
        _logger = logger;
        _config = config;
        _schedule = schedule;
        _users = users;
        _mod = mod;
        _configDescriber = configDescriber;
    }

    [Command("config")]
    [Summary("Config management")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    [Parameter("key", ParameterType.String, "The config item to get/modify. This is case sensitive.")]
    [Parameter("[...]", ParameterType.String, "...", true)]
    public async Task ConfigCommandAsync(
        [Summary("The item to get/modify.")] string configItemKey = "",
        [Summary("")][Remainder] string? value = "")
    {
        await ConfigCommand.TestableConfigCommandAsync(
            new SocketCommandContextAdapter(Context),
            _config,
            _configDescriber,
            configItemKey,
            value
        );
    }

    [Command("userinfo")]
    [Summary("Get information about a user (or yourself)")]
    [ModCommand(Group = "Permission")]
    [DevCommand(Group = "Permission")]
    [Alias("uinfo")]
    [Parameter("user", ParameterType.User, "The user to get information about, or yourself if not provided.", true)]
    public async Task UserInfoCommandAsync(
        [Remainder] string user = "")
    {
        if (user == "") user = Context.User.Id.ToString(); // Set to user ID to target self.

        var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(user, Context);

        if (userId == 0)
        {
            await ReplyAsync("I couldn't find that user's id.");
            return;
        }

        var output = $"";

        var member = Context.Guild.GetUser(userId);

        if (member == null)
        {
            // Even if `userId` is not a member of Manechat, it might still be a real user elsewhere in Discord
            var discordUser = await Context.Client.GetUserAsync(userId);

            if (discordUser == null)
            {
                await ReplyAsync("I couldn't find that user, sorry!");
                return;
            }

            output += $"**User:** `<@{discordUser.Id}>` {discordUser.Username} ({discordUser.Id}){Environment.NewLine}";
            output += _users.ContainsKey(discordUser.Id)
                ? $"**Names:** {string.Join(", ", _users[discordUser.Id].Aliases)}{Environment.NewLine}"
                : $"**Names:** None (user isn't known by Izzy){Environment.NewLine}";
            output += $"**Roles:** None (user isn't in this server){Environment.NewLine}";
            output += "**History:** ";
            output += $"Created <t:{discordUser.CreatedAt.ToUnixTimeSeconds()}:R>";
            output += _users.ContainsKey(discordUser.Id)
                ? $", last seen <t:{_users[discordUser.Id].Timestamp.ToUnixTimeSeconds()}:R>{Environment.NewLine}"
                : Environment.NewLine;
            output += $"**Avatar(s):** {Environment.NewLine}";
            output += $"    Server: User is not in this server.{Environment.NewLine}";
            output += $"    Global: {discordUser.GetAvatarUrl() ?? "No global avatar found."}";
        }
        else
        {
            output += $"**User:** `<@{member.Id}>` {member.Username} ({member.Id}){Environment.NewLine}";
            output += $"**Names:** {string.Join(", ", _users[member.Id].Aliases)}{Environment.NewLine}";
            output +=
                $"**Roles:** {string.Join(", ", member.Roles.Where(role => role.Id != Context.Guild.Id).Select(role => role.Name))}{Environment.NewLine}";
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
        }

        await ReplyAsync(output, allowedMentions: AllowedMentions.None);
    }

    [Command("ban")]
    [Summary(
        "Bans a user without deleting message history, and optionally schedules unbanning them at a later date. If the user is already banned, this will only schedule unbanning.")]
    [Remarks("Note that an \"indefinite\" ban, with no scheduled unban, is the same as banning with the regular Discord UI.")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    [Parameter("user", ParameterType.User, "The user to ban.")]
    [Parameter("duration", ParameterType.DateTime, "How long the ban should last, e.g. \"2 weeks\" or \"6 months\". Omit for an indefinite ban.", true)]
    [Example(".ban 123456789012345678 1 month")]
    public async Task BanCommandAsync(
        [Remainder] string argsString = "")
    {
        await TestableBanCommandAsync(
            new SocketCommandContextAdapter(Context),
            argsString
        );
    }

    public async Task TestableBanCommandAsync(
        IIzzyContext Context,
        string argsString = "")
    {
        if (argsString == "")
        {
            await Context.Channel.SendMessageAsync($"Please provide a user to ban. Refer to `{_config.Prefix}help ban` for more information.");
            return;
        }

        var args = DiscordHelper.GetArguments(argsString);

        var user = args.Arguments[0];
        var duration = string.Join("", argsString.Skip(args.Indices[0]));

        duration = DiscordHelper.StripQuotes(duration);

        var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(user, Context);
        var member = Context.Guild.GetUser(userId);

        if (userId == Context.Client.CurrentUser.Id)
        {
            var rnd = new Random();
            if (rnd.Next(100) == 0)
            {
                await Context.Channel.SendMessageAsync("<:izzydeletethis:1028964499723661372>");
            }
            else if (rnd.NextSingle() > 0.5)
            {
                await Context.Channel.SendMessageAsync("<:sweetiebroken:399725081674383360>");
            }
            else
            {
                await Context.Channel.SendMessageAsync("<:izzysadness:910198257702031362>");
            }

            return;
        }

        if (member != null && member.Roles.Select(role => role.Id).Contains(_config.ModRole))
        {
            await Context.Channel.SendMessageAsync("I can't ban a mod. <:izzynothoughtsheadempty:910198222255972382>");
            return;
        }

        if (member != null && member.Hierarchy >= Context.Guild.GetUser(Context.Client.CurrentUser.Id).Hierarchy)
        {
            await Context.Channel.SendMessageAsync(
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
            await Context.Channel.SendMessageAsync($"I encountered an error while attempting to comprehend time: {exception.Message.Split(": ")[1]}");
            return;
        }

        if (time is { Repeats: true })
        {
            await Context.Channel.SendMessageAsync("I can't ban a user repeatedly! Please give me a time that isn't repeating.");
            return;
        }

        // Okay, enough joking around, serious Izzy time
        var hasExistingBan = await Context.Guild.GetIsBannedAsync(userId);

        if (!hasExistingBan)
        {
            // No ban exists, very serious Izzy time.
            await Context.Guild.AddBanAsync(userId, pruneDays: 0, reason: $"Banned by {Context.User.Username}#{Context.User.Discriminator}{(time == null ? "" : $" for {duration}")}.");

            if (time != null)
            {
                // Create scheduled task!
                Dictionary<string, string> fields = new Dictionary<string, string>
                {
                    { "userId", userId.ToString() }
                };
                var action = new ScheduledUnbanJob(userId);
                var job = new ScheduledJob(DateTimeOffset.UtcNow, time.Time, action);
                await _schedule.CreateScheduledJob(job);
            }

            await Context.Channel.SendMessageAsync(
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
                job.Action is ScheduledUnbanJob unbanJob &&
                unbanJob.User == userId);

            // ban exists, make sure a time is declared
            if (time == null)
            {
                // time not declared, make ban permanent.
                if (_schedule.GetScheduledJobs(getUserUnban).Any())
                {
                    var job = _schedule.GetScheduledJobs(getUserUnban).First();

                    await _schedule.DeleteScheduledJob(job);

                    await Context.Channel.SendMessageAsync($"This user is already banned. I have removed an existing unban for them which was scheduled <t:{job.ExecuteAt.ToUnixTimeSeconds()}:R>.{Environment.NewLine}{Environment.NewLine}" +
                                     $"Here's a userlog I unicycled that you can use if you want to!{Environment.NewLine}```{Environment.NewLine}" +
                                     $"Type: Ban (Indefinite){Environment.NewLine}" +
                                     $"User: <@{userId}> {(member != null ? $"({member.Username}#{member.Discriminator})" : "")} ({userId}){Environment.NewLine}" +
                                     $"Names: {(_users.ContainsKey(userId) ? string.Join(", ", _users[userId].Aliases) : "None (user isn't known by Izzy)")}{Environment.NewLine}" +
                                     $"```");
                }
                else
                {
                    // Doesn't exist, it's already permanent.
                    await Context.Channel.SendMessageAsync("This user is already banned, with no scheduled unban. No changes made.");
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

                await Context.Channel.SendMessageAsync($"This user is already banned. I have modified an existing scheduled unban for them from <t:{jobOriginalExecution}:R> to <t:{job.ExecuteAt.ToUnixTimeSeconds()}:R>.{Environment.NewLine}{Environment.NewLine}" +
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
                var action = new ScheduledUnbanJob(userId);
                var job = new ScheduledJob(DateTimeOffset.UtcNow, time.Time, action);
                await _schedule.CreateScheduledJob(job);

                await Context.Channel.SendMessageAsync(
                    $"This user is already banned. I have scheduled an unban for this user. They'll be unbanned <t:{time.Time.ToUnixTimeSeconds()}:R>{Environment.NewLine}{Environment.NewLine}" +
                    $"Here's a userlog I unicycled that you can use if you want to!{Environment.NewLine}```{Environment.NewLine}" +
                    $"Type: Ban ({duration} <t:{time.Time.ToUnixTimeSeconds()}:R>){Environment.NewLine}" +
                    $"User: <@{userId}> {(member != null ? $"({member.Username}#{member.Discriminator})" : "")} ({userId}){Environment.NewLine}" +
                    $"Names: {(_users.ContainsKey(userId) ? string.Join(", ", _users[userId].Aliases) : "None (user isn't known by Izzy)")}{Environment.NewLine}" +
                    $"```");
            }
        }
    }

    [Command("assignrole")]
    [Summary(
        "Assigns a role to a user, and optionally schedules removing that role. If the user already has that role, this will only schedule removal.")]
    [Remarks("Note that an \"indefinite\" role assignment, with no scheduled removal, is the same as assigning a role with the regular Discord UI.")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    [Parameter("role", ParameterType.Role, "The role to assign.")]
    [Parameter("user", ParameterType.User, "The user to assign the role.")]
    [Parameter("duration", ParameterType.DateTime, "How long the role should last, e.g. \"2 weeks\" or \"6 months\". Omit for an indefinite role assignment.", true)]
    [Example(".assignrole @Best Pony @Izzy Moonbot 24 hours")]
    public async Task AssignRoleCommandAsync(
        [Remainder] string argsString = "")
    {
        await TestableAssignRoleCommandAsync(
            new SocketCommandContextAdapter(Context),
            argsString
        );
    }

    public async Task TestableAssignRoleCommandAsync(
        IIzzyContext context,
        string argsString = "")
    {
        if (argsString == "")
        {
            await context.Channel.SendMessageAsync($"Please provide a user and a role to assign. Refer to `{_config.Prefix}help assignrole` for more information.");
            return;
        }

        var args = DiscordHelper.GetArguments(argsString);

        var roleResolvable = args.Arguments[0];
        var userResolvable = args.Arguments[1];
        var duration = string.Join("", argsString.Skip(args.Indices[1]));

        duration = DiscordHelper.StripQuotes(duration);

        var roleId = DiscordHelper.GetRoleIdIfAccessAsync(roleResolvable, context);
        if (roleId == 0)
        {
            await context.Channel.SendMessageAsync("I couldn't find that role, sorry!");
            return;
        }
        var role = context.Guild.GetRole(roleId);

        var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(userResolvable, context);
        if (userId == 0)
        {
            await context.Channel.SendMessageAsync("I couldn't find that user, sorry!");
            return;
        }
        var maybeMember = context.Guild.GetUser(userId);

        // Comprehend time
        TimeHelperResponse? time = null;
        try
        {
            if (duration != "")
                time = TimeHelper.Convert(duration);
        }
        catch (FormatException exception)
        {
            await context.Channel.SendMessageAsync($"I encountered an error while attempting to comprehend time: {exception.Message.Split(": ")[1]}");
            return;
        }

        if (time is { Repeats: true })
        {
            await context.Channel.SendMessageAsync("I can't assign a role repeatedly! Please give me a time that isn't repeating.");
            return;
        }

        if (maybeMember is IIzzyGuildUser member)
        {
            if (role.Position >= context.Guild.GetUser(context.Client.CurrentUser.Id).Hierarchy)
            {
                await context.Channel.SendMessageAsync(
                    "That role is either at the same level or higher than me in the role hierarchy, I cannot assign it. <:izzynothoughtsheadempty:910198222255972382>");
                return;
            }

            // Actually add the role, if they don't have it already.
            var alreadyHasRole = member.Roles.Select(role => role.Id).Contains(roleId);
            if (!alreadyHasRole)
            {
                await _mod.AddRoles(member, new[] { roleId }, "Role applied through .assignrole command.");
            }

            var message = alreadyHasRole ? $"<@{userId}> already has that role." : $"I've given <@&{roleId}> to <@{userId}>.";

            // Delete any existing scheduled removals for this user and role
            var getRoleRemoval = new Func<ScheduledJob, bool>(job =>
                job.Action is ScheduledRoleRemovalJob removalJob &&
                removalJob.User == member.Id &&
                removalJob.Role == roleId);

            var hasExistingRemovalJob = _schedule.GetScheduledJobs(getRoleRemoval).Any();
            if (hasExistingRemovalJob)
            {
                var jobs = _schedule.GetScheduledJobs(getRoleRemoval);
                foreach (var job in jobs)
                {
                    await _schedule.DeleteScheduledJob(job);
                }
            }

            // If a duration was provided, schedule removal.
            if (time is not null)
            {
                _logger.Log($"Adding scheduled job to remove role {roleId} from user {userId} at {time.Time}", level: LogLevel.Debug);
                var action = new ScheduledRoleRemovalJob(roleId, member.Id,
                    $".assignrole command for user {member.Id} and role {roleId} with duration {duration}.");
                var task = new ScheduledJob(DateTimeOffset.UtcNow, time.Time, action);
                await _schedule.CreateScheduledJob(task);
                _logger.Log($"Added scheduled job for new user", level: LogLevel.Debug);

                if (hasExistingRemovalJob)
                {
                    message += $" I've replaced an existing removal job with a new one scheduled <t:{time.Time.ToUnixTimeSeconds()}:R>.";
                }
                else
                {
                    message += $" I've scheduled a removal <t:{time.Time.ToUnixTimeSeconds()}:R>.";
                }
            }
            else
            {
                if (hasExistingRemovalJob)
                {
                    message += $" I've removed a previously scheduled role removal.";
                }
            }

            await context.Channel.SendMessageAsync(message, allowedMentions: AllowedMentions.None);
        }
        else
        {
            await context.Channel.SendMessageAsync("I couldn't find that user, sorry!");
            return;
        }
    }

    [Command("wipe")]
    [Summary("Deletes all messages in a channel sent within a certain amount of time.")]
    [Remarks("Then posts a 'bulk deletion log' as a text file in LogChannel.")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    [Parameter("channel", ParameterType.Channel, "The channel to wipe.")]
    [Parameter("duration", ParameterType.DateTime, "How far back to wipe messages, e.g. \"5 minutes\" or \"10 days\". " +
        "Defaults to 24 hours. Note that Discord doesn't support bulk deleting messages older than 14 days.", true)]
    [Example(".wipe #the-moon 20 minutes")]
    public async Task WipeCommandAsync(
        [Remainder] string argsString = "")
    {
        if (argsString == "")
        {
            await ReplyAsync($"Please provide a channel to wipe. Refer to `{_config.Prefix}help wipe` for more information.");
            return;
        }

        var args = DiscordHelper.GetArguments(argsString);

        var channelName = args.Arguments[0];
        var duration = string.Join("", argsString.Skip(args.Indices[0]));

        duration = DiscordHelper.StripQuotes(duration);

        var channelId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
        var channel = (channelId != 0) ? Context.Guild.GetTextChannel(channelId) : null;
        if (channel == null)
        {
            await ReplyAsync("I can't send a message there.");
            return;
        }

        var izzyGuildUser = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
        var izzysPermsInChannel = izzyGuildUser.GetPermissions(channel);
        if (!izzysPermsInChannel.Has(ChannelPermission.ManageMessages))
        {
            await ReplyAsync("Sorry, I don't have permission to manage/delete messages there.");
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
            await ReplyAsync("I can't wipe a channel repeatedly! Please give me a time that isn't repeating.");
            return;
        }

        var wipeThreshold = (time == null) ?
            // default to wiping the last 24 hours
            DateTimeOffset.UtcNow.AddHours(-24) :
            // unfortunately TimeHelper assumes user input is always talking about a future time, but we want a past time
            // TODO: rethink the TimeHelper API to avoid hacks like this
            (DateTimeOffset.UtcNow - (time.Time - DateTimeOffset.UtcNow));

        if ((DateTimeOffset.UtcNow - wipeThreshold).TotalDays > 14)
        {
            await ReplyAsync("I can't delete messages older than 14 days, sorry!");
            return;
        }

        _logger.Log($"Parsed .wipe command arguments. Scanning for messages in channel {channelName} more recent than {wipeThreshold}");

        // Gather up all the messages we need to delete and log.

        var messageIdsToDelete = new List<ulong>();

        // We've seen GetMessagesAsync return the messages in both chronological and reverse chronological order,
        // so we need to remember the message creation times in order to do our own sorting at the end.
        var bulkDeletionLog = new List<(DateTimeOffset, string, string)>();

        // Because snowflake ids correspond to datetime moments, we can use a snowflake to directly ask for all messages
        // after a certain moment, even if that snowflake is not the actual id of any message in the channel.
        var snowflakeWipeThreshold = SnowflakeUtils.ToSnowflake(wipeThreshold);
        var asyncRecentMessages = channel.GetMessagesAsync(snowflakeWipeThreshold, Direction.After);
        await foreach (var recentMessages in asyncRecentMessages)
        {
            foreach (var message in recentMessages)
            {
                messageIdsToDelete.Add(message.Id);
                bulkDeletionLog.Add((
                    message.CreatedAt,
                    $"{message.Author.Username} ({message.Author.Id})",
                    $"[{message.CreatedAt}] {message.Author.Username}: {message.Content}"
                ));
            }
        }

        // Actually do the deletion
        var messagesToDeleteCount = messageIdsToDelete.Count;
        _logger.Log($"Deleting {messagesToDeleteCount} messages from channel {channelName}");
        var discordBulkDeletionLimit = 100;
        while (messageIdsToDelete.Any())
        {
            var messageIdsBatch = messageIdsToDelete.Take(discordBulkDeletionLimit).ToList();
            messageIdsToDelete.RemoveRange(0, Math.Min(messageIdsToDelete.Count, discordBulkDeletionLimit));

            await channel.DeleteMessagesAsync(messageIdsBatch);
        }

        // Finally, post a bulk deletion log in LogChannel
        var logChannelId = _config.LogChannel;
        if (logChannelId == 0)
        {
            await ReplyAsync("I can't post a bulk deletion log, because .config LogChannel hasn't been set.");
            return;
        }
        var logChannel = Context.Guild.GetTextChannel(logChannelId);
        if (logChannel == null)
        {
            await ReplyAsync("Something went wrong trying to access LogChannel.");
            return;
        }

        if (messagesToDeleteCount > 0)
        {
            _logger.Log($"Assembling a bulk deletion log from the content of {messagesToDeleteCount} deleted messages");
            bulkDeletionLog.Sort((x, y) => x.Item1.CompareTo(y.Item1));

            var bulkDeletionLogString = string.Join(
                Environment.NewLine + Environment.NewLine,
                bulkDeletionLog.Select(logElement => logElement.Item3)
            );

            var involvedUserDescriptions = string.Join(", ", bulkDeletionLog.Select(logElement => logElement.Item2).ToHashSet());

            var s = new MemoryStream(Encoding.UTF8.GetBytes(bulkDeletionLogString));
            var fa = new FileAttachment(s, $"{channel.Name}_bulk_deletion_log_{DateTimeOffset.UtcNow.ToString()}.txt");
            var bulkDeletionMessage = await logChannel.SendFileAsync(fa, $"Finished wiping {channelName}, here's the bulk deletion log involving {involvedUserDescriptions}:");

            await ReplyAsync($"Finished wiping {channelName}. {messagesToDeleteCount} messages were deleted: {bulkDeletionMessage.GetJumpUrl()}");
        }
        else
        {
            await ReplyAsync($"I didn't find any messages that recent in {channelName}. Deleted nothing.");
        }
    }
}