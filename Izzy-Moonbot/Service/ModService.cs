using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Service;

public class ModService
{
    private readonly ModLoggingService _modLog;
    private readonly Config _config;
    private readonly Dictionary<ulong, User> _users;

    public ModService(Config config, Dictionary<ulong, User> users, ModLoggingService modLog)
    {
        _config = config;
        _users = users;
        _modLog = modLog;
    }

    /*public async Task<string> GenerateSuggestedLog(ActionType action, SocketGuildUser target, DateTimeOffset time, DateTimeOffset? until, string reason)
    {
        string actionName = GetActionName(action);
        string untilTimestamp = "";

        if (until.HasValue == false) untilTimestamp = "Never (Permanent)";
        else
        {
            long untilUnixTimestamp = until.Value.ToUnixTimeSeconds();
            untilTimestamp = $"<t:{untilUnixTimestamp}:F>";
        }

        string template = $"Type: {actionName}{Environment.NewLine}" +
               $"User: {target.Mention} (`{target.Id}`){Environment.NewLine}" +
               $"Expires: {untilTimestamp}{Environment.NewLine}" +
               $"Info: {reason}";

        return template;
    }*/

    public async Task KickUser(SocketGuildUser user, DateTimeOffset time, string? reason = null)
    {
        if (!_config.SafeMode) await user.KickAsync(reason);

        await _modLog.CreateActionLog(user.Guild)
            .SetActionType(LogType.Kick)
            .AddTarget(user)
            .SetTime(time)
            .SetReason(reason)
            .Send();
    }

    public async Task KickUsers(List<SocketGuildUser> users, DateTimeOffset time, string? reason = null)
    {
        if (users.Count == 0) throw new NullReferenceException("users must have users in them");
        var actionLog = _modLog.CreateActionLog(users[0].Guild)
            .SetActionType(LogType.Kick)
            .SetTime(time)
            .SetReason(reason);

        foreach (var user in users)
        {
            if (!_config.SafeMode) await user.KickAsync(reason);
            actionLog.AddTarget(user);
        }

        await actionLog.Send();
    }

    public async Task SilenceUser(SocketGuildUser user, DateTimeOffset time, DateTimeOffset? until,
        string? reason = null)
    {
        if (_config.MemberRole == null) throw new TargetException("MemberRole config value is null (not set)");
        
        if (!_config.SafeMode)
        {
            await user.RemoveRoleAsync((ulong) _config.MemberRole);

            if (until != null)
            {
                
            }
            
            _users[user.Id].Silenced = true;
            await FileHelper.SaveUsersAsync(_users);
        }

        await _modLog.CreateActionLog(user.Guild)
            .SetActionType(LogType.Silence)
            .AddTarget(user)
            .SetTime(time)
            .SetUntilTime(until)
            .SetReason(reason)
            .Send();
    }

    public async Task SilenceUsers(List<SocketGuildUser> users, DateTimeOffset time, DateTimeOffset? until,
        string? reason = null)
    {
        if (_config.MemberRole == null) throw new TargetException("MemberRole config value is null (not set)");
        
        if (users.Count == 0) throw new NullReferenceException("users must have users in them");
        var actionLog = _modLog.CreateActionLog(users[0].Guild)
            .SetActionType(LogType.Silence)
            .SetTime(time)
            .SetUntilTime(until)
            .SetReason(reason);

        foreach (var user in users)
        {
            actionLog.AddTarget(user);

            if (_config.SafeMode) continue;
            await user.RemoveRoleAsync((ulong)_config.MemberRole);

            _users[user.Id].Silenced = true;
            await FileHelper.SaveUsersAsync(_users);
        }

        await actionLog.Send();
    }

    public async Task AddRole(SocketGuildUser user, ulong roleId, DateTimeOffset time, string? reason = null)
    {
        if (!_config.SafeMode) await user.AddRoleAsync(roleId);

        await _modLog.CreateActionLog(user.Guild)
            .SetActionType(LogType.AddRoles)
            .AddTarget(user)
            .AddRole(roleId)
            .SetReason(reason)
            .Send();
    }

    public async Task AddRoleToUsers(List<SocketGuildUser> users, ulong roleId, DateTimeOffset time,
        string? reason = null)
    {
        var actionLog = _modLog.CreateActionLog(users[0].Guild)
            .SetActionType(LogType.AddRoles)
            .AddRole(roleId)
            .SetTime(time)
            .SetReason(reason);

        foreach (var socketGuildUser in users)
        {
            if (!_config.SafeMode) await socketGuildUser.AddRoleAsync(roleId);
            actionLog.AddTarget(socketGuildUser);
        }

        await actionLog.Send();
    }

    public async Task RemoveRole(SocketGuildUser user, ulong roleId, DateTimeOffset time, string? reason = null)
    {
        if (!_config.SafeMode) await user.RemoveRoleAsync(roleId);

        await _modLog.CreateActionLog(user.Guild)
            .SetActionType(LogType.RemoveRoles)
            .AddTarget(user)
            .AddRole(roleId)
            .SetTime(time)
            .SetReason(reason)
            .Send();
    }

    public async Task RemoveRoleFromUsers(List<SocketGuildUser> users, ulong roleId, DateTimeOffset time,
        string? reason = null)
    {
        var actionLog = _modLog.CreateActionLog(users[0].Guild)
            .SetActionType(LogType.RemoveRoles)
            .AddRole(roleId)
            .SetTime(time)
            .SetReason(reason);

        foreach (var socketGuildUser in users)
        {
            if (!_config.SafeMode) await socketGuildUser.RemoveRoleAsync(roleId);
            actionLog.AddTarget(socketGuildUser);
        }

        await actionLog.Send();
    }

    public async Task AddRoles(SocketGuildUser user, List<ulong> roles, DateTimeOffset time, string? reason = null)
    {
        if (!_config.SafeMode) await user.AddRolesAsync(roles);

        await _modLog.CreateActionLog(user.Guild)
            .SetActionType(LogType.AddRoles)
            .AddTarget(user)
            .AddRoles(roles)
            .SetTime(time)
            .SetReason(reason)
            .Send();
    }

    public async Task AddRolesToUsers(List<SocketGuildUser> users, List<ulong> roles, DateTimeOffset time,
        string? reason = null)
    {
        var actionLog = _modLog.CreateActionLog(users[0].Guild)
            .SetActionType(LogType.AddRoles)
            .AddRoles(roles)
            .SetTime(time)
            .SetReason(reason);

        foreach (var socketGuildUser in users)
        {
            if (!_config.SafeMode) await socketGuildUser.AddRolesAsync(roles);
            actionLog.AddTarget(socketGuildUser);
        }

        await actionLog.Send();
    }

    public async Task RemoveRoles(SocketGuildUser user, List<ulong> roles, DateTimeOffset time, string? reason = null)
    {
        if (!_config.SafeMode) await user.RemoveRolesAsync(roles);

        await _modLog.CreateActionLog(user.Guild)
            .SetActionType(LogType.RemoveRoles)
            .AddTarget(user)
            .AddRoles(roles)
            .SetTime(time)
            .SetReason(reason)
            .Send();
    }

    public async Task RemoveRolesFromUsers(List<SocketGuildUser> users, List<ulong> roles, DateTimeOffset time,
        string? reason = null)
    {
        var actionLog = _modLog.CreateActionLog(users[0].Guild)
            .SetActionType(LogType.RemoveRoles)
            .AddRoles(roles)
            .SetTime(time)
            .SetReason(reason);

        foreach (var socketGuildUser in users)
        {
            if (!_config.SafeMode) await socketGuildUser.RemoveRolesAsync(roles);
            actionLog.AddTarget(socketGuildUser);
        }

        await actionLog.Send();
    }
}