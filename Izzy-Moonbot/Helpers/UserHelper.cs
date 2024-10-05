using Discord;
using Discord.WebSocket;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using System.Linq;
using Izzy_Moonbot.Service;
using System.Threading.Tasks;
using Izzy_Moonbot.EventListeners;

namespace Izzy_Moonbot.Helpers;

public static class UserHelper
{
    public static bool updateUserInfoFromDiscord(User userInfo, SocketGuildUser socketGuildUser, Config config)
    {
        var userInfoChanged = false;

        // This format is effectively obsolete now that Discord is replacing discriminators
        // with globally unique usernames, but it's what Izzy's userinfo objects expect.
        var oldFashionedUserIdentifier = $"{socketGuildUser.Username}#{socketGuildUser.Discriminator}";
        if (userInfo.Username != oldFashionedUserIdentifier)
        {
            userInfo.Username = oldFashionedUserIdentifier;
            userInfoChanged = true;
        }

        if (!userInfo.Aliases.Contains(socketGuildUser.DisplayName))
        {
            userInfo.Aliases.Add(socketGuildUser.DisplayName);
            userInfoChanged = true;
        }

        if (!userInfo.Aliases.Contains(socketGuildUser.DisplayName))
        {
            userInfo.Aliases.Add(socketGuildUser.DisplayName);
            userInfoChanged = true;
        }

        if (socketGuildUser.JoinedAt.HasValue &&
            !userInfo.Joins.Contains(socketGuildUser.JoinedAt.Value))
        {
            userInfo.Joins.Add(socketGuildUser.JoinedAt.Value);
            userInfoChanged = true;
        }

        return userInfoChanged;
    }

    // Currently "join roles" include:
    // - MemberRole (depending on whether the user was silenced when they left)
    // - NewMemberRole ("applying" this includes scheduling its automated removal)
    // - RolesToReapplyOnRejoin (varies by config)
    public record JoinRoleResult(bool userInfoChanged, bool configChanged, HashSet<ulong> rolesAdded, ScheduledJob? newUserRoleUpdateJob = null);

    public static async Task<JoinRoleResult> applyJoinRolesToUser(
        User userInfo,
        SocketGuildUser socketGuildUser,
        Config config,
        ModService modService,
        ScheduleService scheduleService
    )
    {
        bool userInfoChanged = false;
        bool configChanged = false;
        HashSet<ulong> rolesToAddIfMissing = new HashSet<ulong>();
        ScheduledJob? newUserRoleUpdateJob = null;

        if (config.ManageNewUserRoles)
        {
            bool silencingUser = config.AutoSilenceNewJoins || userInfo.Silenced;
            if (silencingUser)
            {
                rolesToAddIfMissing.Add(DiscordHelper.BanishedRoleId);
            }

            if (config.ZeroJoinRoles)
            {
                if (config.MemberRole != null && config.MemberRole > 0 && socketGuildUser.JoinedAt is not null)
                {
                    var joinTime = socketGuildUser.JoinedAt.Value;
                    var memberAcceptanceTime = joinTime.AddMinutes(config.NewMemberRoleDecay);

                    if (DateTimeOffset.UtcNow < memberAcceptanceTime)
                    {
                        // no change to rolesToAddIfMissing

                        var action = new ScheduledRoleAdditionJob(config.MemberRole.Value, socketGuildUser.Id,
                            $"Member role added, {config.NewMemberRoleDecay} minutes (`NewMemberRoleDecay`) passed.");
                        var task = new ScheduledJob(DateTimeOffset.UtcNow, memberAcceptanceTime, action);
                        await scheduleService.CreateScheduledJob(task);

                        newUserRoleUpdateJob = task;
                    }
                }
            }
            else
            {
                if (config.NewMemberRole != null && config.NewMemberRole > 0 && socketGuildUser.JoinedAt is not null)
                {
                    var joinTime = socketGuildUser.JoinedAt.Value;
                    var newMemberExpiry = joinTime.AddMinutes(config.NewMemberRoleDecay);

                    if (DateTimeOffset.UtcNow < newMemberExpiry)
                    {
                        rolesToAddIfMissing.Add((ulong)config.NewMemberRole);

                        var action = new ScheduledRoleRemovalJob(config.NewMemberRole.Value, socketGuildUser.Id,
                            $"New member role removal, {config.NewMemberRoleDecay} minutes (`NewMemberRoleDecay`) passed.");
                        var task = new ScheduledJob(DateTimeOffset.UtcNow, newMemberExpiry, action);
                        await scheduleService.CreateScheduledJob(task);

                        newUserRoleUpdateJob = task;
                    }
                }
            }
        }

        foreach (var roleId in userInfo.RolesToReapplyOnRejoin)
        {
            if (!socketGuildUser.Guild.Roles.Select(role => role.Id).Contains(roleId))
            {
                userInfo.RolesToReapplyOnRejoin.Remove(roleId);
                config.RolesToReapplyOnRejoin.Remove(roleId);
                userInfoChanged = configChanged = true;
            }
            else if (!config.RolesToReapplyOnRejoin.Contains(roleId))
            {
                userInfo.RolesToReapplyOnRejoin.Remove(roleId);
                userInfoChanged = true;
            }
        }

        string auditLogMessage;
        if (rolesToAddIfMissing.Count > 0 && userInfo.RolesToReapplyOnRejoin.Count == 0)
            auditLogMessage = "New user join";
        else if (rolesToAddIfMissing.Count == 0 && userInfo.RolesToReapplyOnRejoin.Count > 0)
            auditLogMessage = "Roles reapplied due to having them before leaving";
        else
            auditLogMessage = "New user join (including roles reapplied due to having them before leaving)";

        if (userInfo.RolesToReapplyOnRejoin.Count > 0)
            rolesToAddIfMissing.UnionWith(userInfo.RolesToReapplyOnRejoin);

        if (rolesToAddIfMissing.Count == 0)
            return new JoinRoleResult(userInfoChanged, configChanged, [], newUserRoleUpdateJob);

        HashSet<ulong> existingRoleIds = socketGuildUser.Roles.Select(r => r.Id).ToHashSet();
        HashSet<ulong> rolesToActuallyAdd = rolesToAddIfMissing.Where(r => !existingRoleIds.Contains(r)).ToHashSet();
        await modService.AddRoles(socketGuildUser, rolesToActuallyAdd, auditLogMessage);
        return new JoinRoleResult(userInfoChanged, configChanged, rolesToActuallyAdd, newUserRoleUpdateJob);
    }

    public record UserScanResult(int totalUsersCount, int updatedUserCount, int newUserCount, Dictionary<ulong, int> roleAddedCounts, HashSet<ulong> newUserRoleUpdatesScheduled);

    public static async Task<UserScanResult> scanAllUsers(
        SocketGuild guild,
        Dictionary<ulong, User> allUserInfo,
        Config config,
        ModService modService,
        ScheduleService scheduleService,
        LoggingService logger
    )
    {
        // check for and log config errors that would be too spammy if we re-logged them for every user
        if (config.ManageNewUserRoles)
        {
            if (config.MemberRole == null || config.MemberRole <= 0)
                logger.Log($"ManageNewUserRoles is true but MemberRole is {config.MemberRole}", level: LogLevel.Warning);

            if (config.NewMemberRole == null || config.NewMemberRole <= 0)
                logger.Log($"ManageNewUserRoles is true but NewMemberRole is {config.NewMemberRole}", level: LogLevel.Warning);
        }

        if (!guild.HasAllMembers) await guild.DownloadUsersAsync();

        var totalUsersCount = guild.Users.Count;
        var newUserCount = 0;
        var updatedUserCount = 0;
        Dictionary<ulong, int> roleAddedCounts = new();
        HashSet<ulong> newUserRoleUpdatesScheduled = new();

        bool userInfoChanged = false;
        bool configChanged = false;

        await foreach (var socketGuildUser in guild.Users.ToAsyncEnumerable())
        {
            User userInfo;
            if (!allUserInfo.ContainsKey(socketGuildUser.Id))
            {
                userInfo = new User();
                allUserInfo.Add(socketGuildUser.Id, userInfo);
                newUserCount += 1;
                userInfoChanged = true;

                var result = await applyJoinRolesToUser(userInfo, socketGuildUser, config, modService, scheduleService);
                userInfoChanged |= result.userInfoChanged;
                configChanged |= result.configChanged;
                if (result.newUserRoleUpdateJob != null)
                    newUserRoleUpdatesScheduled.Add(socketGuildUser.Id);
                foreach (var roleId in result.rolesAdded)
                    if (roleAddedCounts.ContainsKey(roleId)) roleAddedCounts[roleId] += 1;
                    else                                     roleAddedCounts[roleId] = 1;
            }
            else
            {
                userInfo = allUserInfo[socketGuildUser.Id];
            }

            bool changed = updateUserInfoFromDiscord(userInfo, socketGuildUser, config);
            if (changed)
            {
                updatedUserCount += 1;
                userInfoChanged = true;
            }
        }

        var scanSummary = $"Finished scanning all {totalUsersCount} users. " +
            $"{updatedUserCount} required a userinfo update, of which {newUserCount} were new to me. " +
            $"The other {totalUsersCount - updatedUserCount} were up to date.";

        logger.Log(scanSummary);

        if (configChanged) await FileHelper.SaveConfigAsync(config);
        if (userInfoChanged) await FileHelper.SaveUsersAsync(allUserInfo);
        // we don't save the schedule file here because scheduling the job already does that; it's likely not worth batching that

        return new UserScanResult(totalUsersCount, updatedUserCount, newUserCount, roleAddedCounts, newUserRoleUpdatesScheduled);
    }
}
