using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;

namespace Izzy_Moonbot.EventListeners;

public class UserListener
{
    // This listener handles listening to user related events (join, leave, etc)
    // This is mostly used for logging and constructing user settings
    
    private readonly LoggingService _logger;
    private readonly Dictionary<ulong, User> _users;
    private readonly ModLoggingService _modLogger;
    private readonly ModService _mod;
    private readonly ScheduleService _schedule;
    private readonly Config _config;
    
    public UserListener(LoggingService logger, Dictionary<ulong, User> users, ModLoggingService modLogger, ModService mod, ScheduleService schedule, Config config)
    {
        _logger = logger;
        _users = users;
        _modLogger = modLogger;
        _mod = mod;
        _schedule = schedule;
        _config = config;
    }

    public void RegisterEvents(DiscordSocketClient client)
    {
        client.UserUnbanned += (user, guild) => Task.Run(async () => { await MemberUnbanEvent(user, guild); });
        client.UserJoined += (member) => Task.Run(async () => { await MemberJoinEvent(member); });
        client.UserLeft += (guild, user) => Task.Run(async () => { await MemberLeaveEvent(guild, user); });
        client.GuildMemberUpdated += (oldMember, newMember) => Task.Run(async () => { await MemberUpdateEvent(oldMember, newMember); });
    }

    public async Task MemberUnbanEvent(SocketUser user, SocketGuild guild)
    {
        if (guild.Id != DiscordHelper.DefaultGuild()) return;
        
        var scheduledJobs = _schedule.GetScheduledJobs(job => 
            job.Action switch
            {
                ScheduledUnbanJob unbanJob => unbanJob.User == user.Id,
                _ => false
            }
        );
        _logger.Log($"User was unbanned: {user.Username} ({user.Id}).{(scheduledJobs.Any() ? $" Cancelling {scheduledJobs.Count} scheduled unban job(s) for this user." : "")}");
        foreach (var scheduledJob in scheduledJobs)
            await _schedule.DeleteScheduledJob(scheduledJob);
    }

    public async Task MemberJoinEvent(SocketGuildUser member)
    {
        if (member.Guild.Id != DiscordHelper.DefaultGuild()) return;

        bool userInfoChanged = false;
        bool configChanged = false;

        User userInfo;
        if (!_users.ContainsKey(member.Id))
        {
            userInfo = new User();
            _users.Add(member.Id, userInfo);
            userInfoChanged = true;
        }
        else
        {
            userInfo = _users[member.Id];
        }

        bool changed = UserHelper.updateUserInfoFromDiscord(userInfo, member, _config);
        if (changed) userInfoChanged = true;

        var result = await UserHelper.applyJoinRolesToUser(userInfo, member, _config, _mod, _schedule);
        userInfoChanged |= result.userInfoChanged;
        configChanged |= result.configChanged;

        if (configChanged) await FileHelper.SaveConfigAsync(_config);
        if (userInfoChanged) await FileHelper.SaveUsersAsync(_users);

        _logger.Log($"New member join: {member.DisplayName} ({member.Username}/{member.Id})");

        string autoSilence = "";
        if (_config.AutoSilenceNewJoins) autoSilence = ", silenced (`AutoSilenceNewJoins` is on)";
        if (_users[member.Id].Silenced) autoSilence = ", silenced (attempted silence bypass)";

        string joinedBefore = $", Joined {_users[member.Id].Joins.Count - 1} times before";
        if (_users[member.Id].Joins.Count <= 1) joinedBefore = "";

        var rolesReappliedString = "";
        var reappliedRoles = result.rolesAdded.Where(r => _config.RolesToReapplyOnRejoin.Contains(r)).ToHashSet();
        if (reappliedRoles.Count > 0)
            rolesReappliedString = $", Reapplied roles (from `RolesToReapplyOnRejoin`): {string.Join(", ", reappliedRoles.Select(r => $"<&{r}>"))}";

        var msg = $"Join: <@{member.Id}> (`{member.Id}`), created <t:{member.CreatedAt.ToUnixTimeSeconds()}:R>{autoSilence}{joinedBefore}{rolesReappliedString}";
        _logger.Log($"Generated moderation log for user join: {msg}");
        await _modLogger.CreateModLog(member.Guild)
            .SetContent(msg)
            .SetFileLogContent(msg)
            .Send();
    }
    
    private async Task MemberLeaveEvent(SocketGuild guild, SocketUser user)
    {
        if (guild.Id != DiscordHelper.DefaultGuild()) return;
        
        _logger.Log($"Member leaving: {DiscordHelper.DisplayName(user, guild)} ({user.Username}/{user.Id})");
        var lastNickname = "";
        try
        {
            lastNickname = _users[user.Id].Aliases.Last();
        }
        catch (InvalidOperationException)
        {
            lastNickname = "<UNKNOWN>";
        }

        // Unfortunately Discord(.NET) doesn't tell us anything about why or how a user left a server, merely that they did.
        // To infer that they left *because* of a kick/ban, we arbitrarily assume that whenever a user is kicked/banned,
        // Discord will send the UserLeft event within 100 seconds, before 5 other kicks/bans take place, *and*
        // that the user will not be unbannned, re-join and re-leave all within 100 seconds.

        var kickAuditLog = guild.GetAuditLogsAsync(5, actionType: ActionType.Kick).FirstAsync()
            .GetAwaiter().GetResult()
            .Select(audit =>
            {
                var data = audit.Data as KickAuditLogData;
                if (data?.Target.Id == user.Id)
                {
                    if ((DateTimeOffset.UtcNow.ToUnixTimeSeconds() - audit.CreatedAt.ToUnixTimeSeconds()) <= 100)
                        return audit;
                }
                return null;
            }).Where(audit => audit != null).FirstOrDefault();

        var banAuditLog = guild.GetAuditLogsAsync(5, actionType: ActionType.Ban).FirstAsync()
            .GetAwaiter().GetResult()
            .Select(audit =>
            {
                var data = audit.Data as BanAuditLogData;
                if (data?.Target.Id == user.Id)
                {
                    if ((DateTimeOffset.UtcNow.ToUnixTimeSeconds() - audit.CreatedAt.ToUnixTimeSeconds()) <= 100)
                        return audit;
                }
                return null;
            }).Where(audit => audit != null).FirstOrDefault();

        var output = $"Leave: {lastNickname} (`{user.Username}`/`{user.Id}`) joined <t:{_users[user.Id].Joins.Last().ToUnixTimeSeconds()}:R>";

        if (banAuditLog != null)
            output += $"\n\n" +
                $"According to the server audit log, they were **banned** <t:{banAuditLog.CreatedAt.ToUnixTimeSeconds()}:R> by {DiscordHelper.DisplayName(banAuditLog.User, guild)} ({banAuditLog.User.Username}/{banAuditLog.User.Id}) for the following reason:\n" +
                $"\"{banAuditLog.Reason}\"" +
                "\n\n" +
                $"Here's a userlog I unicycled that you can use if you want to!\n```\n" +
                $"Type: Ban\n" +
                $"User: <@{user.Id}> ({user.Username}/{user.Id})\n" +
                $"Names: {(_users.TryGetValue(user.Id, out var userInfo) ? string.Join(", ", userInfo.Aliases) : "None (user isn't known by Izzy)")}\n" +
                $"```";

        if (kickAuditLog != null)
            output += $"\n\n" +
                $"According to the server audit log, they were **kicked** <t:{kickAuditLog.CreatedAt.ToUnixTimeSeconds()}:R> by {DiscordHelper.DisplayName(kickAuditLog.User, guild)} ({kickAuditLog.User.Username}/{kickAuditLog.User.Id})) for the following reason:\n" +
                $"\"{kickAuditLog.Reason}\"" +
                "\n\n" +
                $"Here's a userlog I unicycled that you can use if you want to!\n```\n" +
                $"Type: Kick\n" +
                $"User: <@{user.Id}> ({user.Username}/{user.Id})\n" +
                $"Names: {(_users.TryGetValue(user.Id, out var userInfo) ? string.Join(", ", userInfo.Aliases) : "None (user isn't known by Izzy)")}\n" +
                $"```";

        // Scheduled jobs that require a user to be in the server create a difficult question.
        // Do we delete them on leave so there's no error later? Or keep them in case the user rejoins?
        // The rules we've settled on are:
        // - NewMemberRole removals should be silently deleted, since Izzy will create
        // a fresh removal job if the user rejoins, and we do want that timer to reset
        // - All other *non-repeating* jobs should be kept, since they were probably manually created,
        // possibly for moderation purposes, and will cause at most one error message
        // - All *repeating* jobs should be deleted and mentioned in the ModChannel leave message,
        // since keeping them would cause errors forever and this almost never happens
        var newMemberRoleRemovals = _schedule.GetScheduledJobs(job =>
            job.Action switch
            {
                ScheduledRoleJob roleJob => roleJob.User == user.Id && roleJob.Role == _config.NewMemberRole,
                _ => false
            }
        );
        foreach (var job in newMemberRoleRemovals)
            await _schedule.DeleteScheduledJob(job);

        var repeatingJobsForUser = _schedule.GetScheduledJobs(job =>
            job.RepeatType != ScheduledJobRepeatType.None && job.Action switch
            {
                ScheduledRoleJob roleJob => roleJob.User == user.Id,
                ScheduledEchoJob echoJob => echoJob.ChannelOrUser == user.Id,
                _ => false
            }
        );
        if (repeatingJobsForUser.Any())
            output += $"\n\nCancelled the following repeating scheduled job(s) for that user:";
        foreach (var job in repeatingJobsForUser)
        {
            output += $"\n{job.ToDiscordString()}";
            await _schedule.DeleteScheduledJob(job);
        }

        _logger.Log($"Sending moderation log: ${output}");
        await _modLogger.CreateModLog(guild).SetContent(output).SetFileLogContent(output).Send();
    }

    private async Task MemberUpdateEvent(Cacheable<SocketGuildUser,ulong> oldUser, SocketGuildUser newUser)
    {
        if (newUser.Guild.Id != DiscordHelper.DefaultGuild()) return;

        var changed = false;
        
        if (!_users.ContainsKey(newUser.Id))
        {
            changed = true;
            _logger.Log($"{newUser.DisplayName} ({newUser.Username}/{newUser.Id}) has no metadata, creating now...", level: LogLevel.Debug);
            var newUserData = new User();
            newUserData.Username = $"{newUser.Username}#{newUser.Discriminator}";
            newUserData.Aliases.Add(newUser.Username);
            if(newUser.JoinedAt.HasValue) newUserData.Joins.Add(newUser.JoinedAt.Value);
            _users.Add(newUser.Id, newUserData);
        }
        else
        {
            if (_users[newUser.Id].Username != $"{newUser.Username}#{newUser.Discriminator}")
            {
                _logger.Log($"User id {newUser.Id} changed their name and/or discriminator from {_users[newUser.Id].Username} to {newUser.Username}#{newUser.Discriminator}. Updating userinfo.", level: LogLevel.Debug);
                _users[newUser.Id].Username = $"{newUser.Username}#{newUser.Discriminator}";
                if (!_users[newUser.Id].Aliases.Contains(newUser.DisplayName))
                    _users[newUser.Id].Aliases.Add(newUser.DisplayName);
                changed = true;
            }
            else if (!_users[newUser.Id].Aliases.Contains(newUser.DisplayName))
            {
                _logger.Log($"{newUser.DisplayName} ({newUser.Username}/{newUser.Id}) changed their DisplayName. Updating userinfo.", level: LogLevel.Debug);
                _users[newUser.Id].Aliases.Add(newUser.DisplayName);
                changed = true;
            }
        }
        
        if (_config.MemberRole != null)
        {
            if (_users[newUser.Id].Silenced &&
                !newUser.Roles.Select(role => role.Id).Contains(DiscordHelper.BanishedRoleId))
            {
                // Unsilenced, Remove the flag.
                _logger.Log(
                    $"{newUser.DisplayName} ({newUser.Username}/{newUser.Id}) unbanished, removing silence flag...");
                _users[newUser.Id].Silenced = false;
                changed = true;
            }

            if (!_users[newUser.Id].Silenced &&
                newUser.Roles.Select(role => role.Id).Contains(DiscordHelper.BanishedRoleId))
            {
                // Silenced, add the flag
                _logger.Log(
                    $"{newUser.DisplayName} ({newUser.Username}/{newUser.Id}) banished, adding silence flag...");
                _users[newUser.Id].Silenced = true;
                changed = true;
            }
        }

        foreach (var roleId in _config.RolesToReapplyOnRejoin)
        {
            if (!_users[newUser.Id].RolesToReapplyOnRejoin.Contains(roleId) &&
                newUser.Roles.Select(role => role.Id).Contains(roleId))
            {
                _logger.Log(
                    $"{newUser.DisplayName} ({newUser.Username}/{newUser.Id}) gained role which will reapply on join, role {newUser.Roles.Single(role => role.Id == roleId).Name} ({roleId})");
                _users[newUser.Id].RolesToReapplyOnRejoin.Add(roleId);
                changed = true;
            }

            if (_users[newUser.Id].RolesToReapplyOnRejoin.Contains(roleId) &&
                !newUser.Roles.Select(role => role.Id).Contains(roleId))
            {
                _logger.Log(
                    $"{newUser.DisplayName} ({newUser.Username}/{newUser.Id}) lost role which would reapply on join, role {newUser.Guild.Roles.Single(role => role.Id == roleId).Name} ({roleId})");
                _users[newUser.Id].RolesToReapplyOnRejoin.Remove(roleId);
                changed = true;
            }
        }
        
        foreach (var roleId in _users[newUser.Id].RolesToReapplyOnRejoin)
        {
            if (!newUser.Guild.Roles.Select(role => role.Id).Contains(roleId))
            {
                _logger.Log(
                    $"{newUser.DisplayName} ({newUser.Username}/{newUser.Id}) had role which I would have reapplied on join but no longer exists: role id {roleId}");
                _users[newUser.Id].RolesToReapplyOnRejoin.Remove(roleId);
                _config.RolesToReapplyOnRejoin.Remove(roleId);
                await FileHelper.SaveConfigAsync(_config);
                changed = true;       
            }
            else
            {

                if (!_config.RolesToReapplyOnRejoin.Contains(roleId))
                {
                    _logger.Log(
                        $"{newUser.DisplayName} ({newUser.Username}/{newUser.Id}) has role which will no longer reapply on join, role {newUser.Guild.Roles.Single(role => role.Id == roleId).Name} ({roleId})");
                    _users[newUser.Id].RolesToReapplyOnRejoin.Remove(roleId);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            await FileHelper.SaveUsersAsync(_users);
            _logger.Log($"in the {(DateTimeOffset.UtcNow - FileHelper.firstFileAccess!).Value.TotalMinutes} minutes since first file access, I've made {FileHelper.usersSaves} usersSaves, {FileHelper.configSaves} configSaves, {FileHelper.scheduleSaves} scheduleSaves, {FileHelper.generalStorageSaves} generalStorageSaves, {FileHelper.quoteSaves} quoteSaves");
        }

        var IMABOT_ROLE_ID = 1163260573606219856u;
        if (newUser.Roles.Any(role => role.Id == IMABOT_ROLE_ID))
        {
            var msg = $"While handling a GuildMemberUpdated event for user <@{newUser.Id}>, I noticed they have the <@&{IMABOT_ROLE_ID}> role." +
                $" They joined <t:{newUser.JoinedAt?.ToUnixTimeSeconds()}:R>";
            await _modLogger.CreateModLog(newUser.Guild).SetContent(msg).SetFileLogContent(msg).Send();
        }
    }
}
