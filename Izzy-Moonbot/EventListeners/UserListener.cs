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
        await _logger.Log($"User was unbanned: {user.Username}#{user.Discriminator}.", level: LogLevel.Debug);
        var scheduledJobs = _schedule.GetScheduledJobs(job => 
            (job.Action.Fields.ContainsKey("userId") && job.Action.Fields["userId"] == user.Id.ToString()) ||
            (job.Action.Fields.ContainsKey("channelId") && job.Action.Fields["channelId"] == user.Id.ToString()));

        await _logger.Log($"Cancelling all scheduled unban jobs for this user", level: LogLevel.Debug);
        foreach (var scheduledJob in scheduledJobs)
        {
            if (scheduledJob.Action.Type != ScheduledJobActionType.Unban) continue;
            await _schedule.DeleteScheduledJob(scheduledJob);
        }
        await _logger.Log($"Cancelled all scheduled unban jobs for this user", level: LogLevel.Debug);
    }

    public async Task MemberJoinEvent(SocketGuildUser member, bool catchingUp = false)
    {
        await _logger.Log($"New member join{(catchingUp ? " found after reboot" : "")}: {member.Username}#{member.DiscriminatorValue} ({member.Id})", level: LogLevel.Debug);
        if (!_users.ContainsKey(member.Id))
        {
            await _logger.Log($"No user data entry for new user, generating one now...", level: LogLevel.Debug);
            User newUser = new User();
            newUser.Username = $"{member.Username}#{member.Discriminator}";
            newUser.Aliases.Add(member.Username);
            newUser.Joins.Add(member.JoinedAt.Value); // I really fucking hope it isn't null the user literally just joined
            _users.Add(member.Id, newUser);
            await FileHelper.SaveUsersAsync(_users);
            await _logger.Log($"New user data entry generated.", level: LogLevel.Debug);
        }
        else if (!catchingUp)
        {
            await _logger.Log($"Found user data entry for new user, add new join date", level: LogLevel.Debug);
            _users[member.Id].Joins.Add(member.JoinedAt.Value); // I still really fucking hope it isn't null because the user did just join
            await FileHelper.SaveUsersAsync(_users);
            await _logger.Log($"Added new join date for new user", level: LogLevel.Debug);
        }
        
        List<ulong> roles = new List<ulong>();
        string expiresString = "";

        await _logger.Log($"Processing roles for new user join", level: LogLevel.Debug);
        if (_config.ManageNewUserRoles && _config.MemberRole != null && !(_config.AutoSilenceNewJoins || _users[member.Id].Silenced))
        {
            await _logger.Log($"Adding Config.MemberRole ({_config.MemberRole}) to new user", level: LogLevel.Debug);
            roles.Add((ulong)_config.MemberRole);
        }

        if (_config.ManageNewUserRoles && _config.NewMemberRole != null && (!_config.AutoSilenceNewJoins || !_users[member.Id].Silenced))
        {
            await _logger.Log($"Adding Config.NewMemberRole ({_config.NewMemberRole}) to new user", level: LogLevel.Debug);
            roles.Add((ulong)_config.NewMemberRole);
            expiresString = $"{Environment.NewLine}New Member role expires in <t:{(DateTimeOffset.UtcNow + TimeSpan.FromMinutes(_config.NewMemberRoleDecay)).ToUnixTimeSeconds()}:R>";

            await _logger.Log($"Adding scheduled job to remove Config.NewMemberRole from new user in {_config.NewMemberRoleDecay} minutes", level: LogLevel.Debug);
            Dictionary<string, string> fields = new Dictionary<string, string>
            {
                { "roleId", _config.NewMemberRole.Value.ToString() },
                { "userId", member.Id.ToString() },
                {
                    "reason",
                    $"New member role removal, {_config.NewMemberRoleDecay} minutes (`NewMemberRoleDecay`) passed."
                }
            };
            var action = new ScheduledJobAction(ScheduledJobActionType.RemoveRole, fields);
            var task = new ScheduledJob(DateTimeOffset.UtcNow, 
                (DateTimeOffset.UtcNow + TimeSpan.FromMinutes(_config.NewMemberRoleDecay)), action);
            await _schedule.CreateScheduledJob(task);
            await _logger.Log($"Added scheduled job for new user", level: LogLevel.Debug);
        }
        
        await _logger.Log($"Generating action reason", level: LogLevel.Debug);
        
        string autoSilence = $" (User autosilenced, `AutoSilenceNewJoins` is true.)";
        
        if (roles.Count != 0)
        {
            if (!_config.AutoSilenceNewJoins) autoSilence = "";
            if (_users[member.Id].Silenced)
                autoSilence = 
                    ", silenced (attempted silence bypass)";
            await _logger.Log($"Generated action reason, executing action", level: LogLevel.Debug);

            await _mod.AddRoles(member, roles, $"New user join{autoSilence}.{expiresString}"); 
            await _logger.Log($"Action executed, generating moderation log content", level: LogLevel.Debug);
        }

        autoSilence = ", silenced (`AutoSilenceNewJoins` is on)";
        if (!_config.AutoSilenceNewJoins) autoSilence = "";
        if (_users[member.Id].Silenced)
            autoSilence = 
                ", silenced (attempted silence bypass)";
        string joinedBefore = $", Joined {_users[member.Id].Joins.Count - 1} times before";
        if (_users[member.Id].Joins.Count <= 1) joinedBefore = "";

        var rolesAutoapplied = new List<string>();

        foreach (var roleId in _users[member.Id].RolesToReapplyOnRejoin)
        {
            var shouldAdd = true;
            
            if (!member.Guild.Roles.Select(role => role.Id).Contains(roleId))
            {
                await _logger.Log(
                    $"{member.Username}#{member.Discriminator} ({member.Id}) had role which would of reapplied on join but no longer exists, role id {roleId}");
                _users[member.Id].RolesToReapplyOnRejoin.Remove(roleId);
                _config.RolesToReapplyOnRejoin.Remove(roleId);
                await FileHelper.SaveConfigAsync(_config);
                await FileHelper.SaveUsersAsync(_users);
                shouldAdd = false;
            }
            else
            {

                if (!_config.RolesToReapplyOnRejoin.Contains(roleId))
                {
                    await _logger.Log(
                        $"{member.Username}#{member.Discriminator} ({member.Id}) has role which will no longer reapply on join, role {member.Guild.Roles.Single(role => role.Id == roleId).Name} ({roleId})");
                    _users[member.Id].RolesToReapplyOnRejoin.Remove(roleId);
                    await FileHelper.SaveUsersAsync(_users);
                    shouldAdd = false;
                }
            }
            
            if(shouldAdd) rolesAutoapplied.Add($"<@&{roleId}>");
        }

        if(_users[member.Id].RolesToReapplyOnRejoin.Count != 0) 
            await _mod.AddRoles(member, _users[member.Id].RolesToReapplyOnRejoin,
                "Roles reapplied due to having them before leaving.");

        var rolesAutoappliedString = $", Reapplied roles (from `RolesToReapplyOnRejoin`): {string.Join(", ", rolesAutoapplied)}";

        if (rolesAutoapplied.Count == 0) rolesAutoappliedString = "";
        
        await _logger.Log($"Generated moderation log content, posting log", level: LogLevel.Debug);
        
        await _modLogger.CreateModLog(member.Guild)
            .SetContent($"{(catchingUp ? "Catching up on ": "")}Join: <@{member.Id}> (`{member.Id}`), created <t:{member.CreatedAt.ToUnixTimeSeconds()}:R>{autoSilence}{joinedBefore}{rolesAutoappliedString}")
            .SetFileLogContent($"{(catchingUp ? "Catching up on ": "")}Join: {member.Username}#{member.Discriminator} (`{member.Id}`), created {member.CreatedAt:O}{autoSilence}{joinedBefore}{rolesAutoappliedString}")
            .Send();
        await _logger.Log($"Log posted", level: LogLevel.Debug);
    }
    
    private async Task MemberLeaveEvent(SocketGuild guild, SocketUser user)
    {
        await _logger.Log($"Member leaving: {user.Username}#{user.Discriminator} ({user.Id}), getting last nickname", level: LogLevel.Debug);
        var lastNickname = "";
        try
        {
            lastNickname = _users[user.Id].Aliases.Last();
        }
        catch (InvalidOperationException)
        {
            lastNickname = "<UNKNOWN>";
        }
        await _logger.Log($"Last nickname was {lastNickname}, checking whether user was kicked or banned", level: LogLevel.Debug);
        var wasKicked = guild.GetAuditLogsAsync(2, actionType: ActionType.Kick).FirstAsync()
            .GetAwaiter().GetResult()
            .Select(audit =>
            {
                var data = audit.Data as KickAuditLogData;
                if (data.Target.Id == user.Id)
                {
                    if ((audit.CreatedAt.ToUnixTimeSeconds() - DateTimeOffset.UtcNow.ToUnixTimeSeconds()) <= 2)
                        return audit;
                }
                return null;
            });

        var wasBanned = guild.GetAuditLogsAsync(2, actionType: ActionType.Ban).FirstAsync()
            .GetAwaiter().GetResult()
            .Select(audit =>
            {
                var data = audit.Data as BanAuditLogData;
                if (data.Target.Id == user.Id)
                {
                    if ((audit.CreatedAt.ToUnixTimeSeconds() - DateTimeOffset.UtcNow.ToUnixTimeSeconds()) <= 2)
                        return audit;
                }
                return null;
            });

        await _logger.Log($"Constructing moderation log content", level: LogLevel.Debug);
        var output = 
            $"Leave: {user.Username}#{user.Discriminator} ({lastNickname}) (`{user.Id}`) joined <t:{_users[user.Id].Joins.Last().ToUnixTimeSeconds()}:R>";
        var fileOutput = 
            $"Leave: {user.Username}#{user.Discriminator} ({lastNickname}) (`{user.Id}`) joined {_users[user.Id].Joins.Last():O}";

        var banAuditLogEntries = wasBanned as RestAuditLogEntry[] ?? wasBanned.ToArray();
        if (banAuditLogEntries.Any(audit => audit != null))
        {
            await _logger.Log($"User was banned, fetching the reason and moderator", level: LogLevel.Debug);
            var audit = banAuditLogEntries.First();
            await _logger.Log($"Fetched, user was banned by {audit.User.Username}#{audit.User.Discriminator} ({audit.User.Id}) for \"{audit.Reason}\"", level: LogLevel.Debug);
            output =
                $"Leave (Ban): {user.Username}#{user.Discriminator} ({lastNickname}) (`{user.Id}`) joined <t:{_users[user.Id].Joins.Last().ToUnixTimeSeconds()}:R>, \"{audit.Reason}\" by {audit.User.Username}#{audit.User.Discriminator} ({guild.GetUser(audit.User.Id).DisplayName})";
            fileOutput =
                $"Leave (Ban): {user.Username}#{user.Discriminator} ({lastNickname}) (`{user.Id}`) joined {_users[user.Id].Joins.Last():O}, \"{audit.Reason}\" by {audit.User.Username}#{audit.User.Discriminator} ({guild.GetUser(audit.User.Id).DisplayName})";
        }

        var kickAuditLogEntries = wasKicked as RestAuditLogEntry[] ?? wasKicked.ToArray();
        if (kickAuditLogEntries.Any(audit => audit != null))
        {
            await _logger.Log($"User was kicked, fetching the reason and moderator", level: LogLevel.Debug);
            var audit = kickAuditLogEntries.First();
            await _logger.Log($"Fetched, user was kicked by {audit.User.Username}#{audit.User.Discriminator} ({audit.User.Id}) for \"{audit.Reason}\"", level: LogLevel.Debug);
            output =
                $"Leave (Kick): {user.Username}#{user.Discriminator} ({lastNickname}) (`{user.Id}`) joined <t:{_users[user.Id].Joins.Last().ToUnixTimeSeconds()}:R>, \"{audit.Reason}\" by {audit.User.Username}#{audit.User.Discriminator} ({guild.GetUser(audit.User.Id).DisplayName})";
            fileOutput =
                $"Leave (Kick): {user.Username}#{user.Discriminator} ({lastNickname}) (`{user.Id}`) joined {_users[user.Id].Joins.Last():O}, \"{audit.Reason}\" by {audit.User.Username}#{audit.User.Discriminator} ({guild.GetUser(audit.User.Id).DisplayName})";
        }
        await _logger.Log($"Finished constructing moderation log content", level: LogLevel.Debug);

        await _logger.Log($"Fetch all scheduled jobs for this user", level: LogLevel.Debug);
        var scheduledTasks = _schedule.GetScheduledJobs(job => 
            (job.Action.Fields.ContainsKey("userId") && job.Action.Fields["userId"] == user.Id.ToString()) ||
            (job.Action.Fields.ContainsKey("channelId") && job.Action.Fields["channelId"] == user.Id.ToString()));

        await _logger.Log($"Cancelling all scheduled jobs for this user", level: LogLevel.Debug);
        foreach (var scheduledTask in scheduledTasks)
        {
            if (scheduledTask.Action.Type == ScheduledJobActionType.Unban) continue;
            await _schedule.DeleteScheduledJob(scheduledTask);
        }
        await _logger.Log($"Cancelled all scheduled jobs for this user", level: LogLevel.Debug);

        await _logger.Log($"Sending moderation log", level: LogLevel.Debug);
        await _modLogger.CreateModLog(guild)
            .SetContent(output)
            .SetFileLogContent(fileOutput)
            .Send();
        await _logger.Log($"Moderation log sent", level: LogLevel.Debug);
    }

    private async Task MemberUpdateEvent(Cacheable<SocketGuildUser,ulong> oldUser, SocketGuildUser newUser)
    {
        var changed = false;
        
        if (!_users.ContainsKey(newUser.Id))
        {
            changed = true;
            await _logger.Log($"{newUser.Username}#{newUser.Discriminator} ({newUser.Id}) has no metadata, creating now...", null, level: LogLevel.Debug);
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
                await _logger.Log($"User name/discriminator changed from {_users[newUser.Id].Username} to {newUser.Username}#{newUser.Discriminator}, updating...", null, level: LogLevel.Debug);
                _users[newUser.Id].Username =
                    $"{newUser.Username}#{newUser.Discriminator}";
                changed = true;
            }

            if (!_users[newUser.Id].Aliases.Contains(newUser.DisplayName))
            {
                await _logger.Log($"{newUser.Username}#{newUser.Discriminator} ({newUser.Id}) has new displayname, updating...", null, level: LogLevel.Debug);
                _users[newUser.Id].Aliases.Add(newUser.DisplayName);
                changed = true;
            }
        }
        
        if (_config.MemberRole != null)
        {
            if (_users[newUser.Id].Silenced &&
                newUser.Roles.Select(role => role.Id).Contains((ulong)_config.MemberRole))
            {
                // Unsilenced, Remove the flag.
                await _logger.Log(
                    $"{newUser.Username}#{newUser.Discriminator} ({newUser.Id}) unsilenced, removing silence flag...");
                _users[newUser.Id].Silenced = false;
                changed = true;
            }

            if (!_users[newUser.Id].Silenced &&
                !newUser.Roles.Select(role => role.Id).Contains((ulong)_config.MemberRole))
            {
                // Silenced, add the flag
                await _logger.Log(
                    $"{newUser.Username}#{newUser.Discriminator} ({newUser.Id}) silenced, adding silence flag...");
                _users[newUser.Id].Silenced = true;
                changed = true;
            }
        }

        foreach (var roleId in _config.RolesToReapplyOnRejoin)
        {
            if (!_users[newUser.Id].RolesToReapplyOnRejoin.Contains(roleId) &&
                newUser.Roles.Select(role => role.Id).Contains(roleId))
            {
                await _logger.Log(
                    $"{newUser.Username}#{newUser.Discriminator} ({newUser.Id}) gained role which will reapply on join, role {newUser.Roles.Single(role => role.Id == roleId).Name} ({roleId})");
                _users[newUser.Id].RolesToReapplyOnRejoin.Add(roleId);
                changed = true;
            }

            if (_users[newUser.Id].RolesToReapplyOnRejoin.Contains(roleId) &&
                !newUser.Roles.Select(role => role.Id).Contains(roleId))
            {
                await _logger.Log(
                    $"{newUser.Username}#{newUser.Discriminator} ({newUser.Id}) lost role which would reapply on join, role {newUser.Guild.Roles.Single(role => role.Id == roleId).Name} ({roleId})");
                _users[newUser.Id].RolesToReapplyOnRejoin.Remove(roleId);
                changed = true;
            }
        }
        
        foreach (var roleId in _users[newUser.Id].RolesToReapplyOnRejoin)
        {
            if (!newUser.Guild.Roles.Select(role => role.Id).Contains(roleId))
            {
                await _logger.Log(
                    $"{newUser.Username}#{newUser.Discriminator} ({newUser.Id}) had role which would of reapplied on join but no longer exists, role id {roleId}");
                _users[newUser.Id].RolesToReapplyOnRejoin.Remove(roleId);
                _config.RolesToReapplyOnRejoin.Remove(roleId);
                await FileHelper.SaveConfigAsync(_config);
                changed = true;       
            }
            else
            {

                if (!_config.RolesToReapplyOnRejoin.Contains(roleId))
                {
                    await _logger.Log(
                        $"{newUser.Username}#{newUser.Discriminator} ({newUser.Id}) has role which will no longer reapply on join, role {newUser.Guild.Roles.Single(role => role.Id == roleId).Name} ({roleId})");
                    _users[newUser.Id].RolesToReapplyOnRejoin.Remove(roleId);
                    changed = true;
                }
            }
        }

        if(changed) await FileHelper.SaveUsersAsync(_users);
    }
}