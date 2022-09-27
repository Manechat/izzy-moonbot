using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Service;

public class ModService
{
    private readonly Config _config;
    private readonly Dictionary<ulong, User> _users;
    private readonly ModLoggingService _modLog;

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

    public async Task KickUsers(IEnumerable<SocketGuildUser> users, string? reason = null)
    {
        if (!users.Any()) throw new NullReferenceException("users must have users in them");
        var actionLog = _modLog.CreateActionLog(users.First().Guild)
            .SetActionType(LogType.Kick)
            .AddTargets(users)
            .SetTime(DateTimeOffset.UtcNow)
            .SetReason(reason);

        foreach (var user in users)
        {
            if (!_config.SafeMode) await user.KickAsync(reason);
        }

        await actionLog.Send();
    }

    public async Task SilenceUser(SocketGuildUser user, string? reason = null)
    {
        if (_config.MemberRole == null) throw new TargetException("MemberRole config value is null (not set)");
        
        if (!_config.SafeMode)
        {
            await user.RemoveRoleAsync((ulong) _config.MemberRole);
            
            _users[user.Id].Silenced = true;
            await FileHelper.SaveUsersAsync(_users);
        }

        await _modLog.CreateActionLog(user.Guild)
            .SetActionType(LogType.Silence)
            .AddTarget(user)
            .SetTime(DateTimeOffset.UtcNow)
            .SetReason(reason)
            .Send();
    }

    public async Task SilenceUsers(IEnumerable<SocketGuildUser> users, string? reason = null)
    {
        if (_config.MemberRole == null) throw new TargetException("MemberRole config value is null (not set)");
        
        if (!users.Any()) throw new NullReferenceException("users must have users in them");
        var actionLog = _modLog.CreateActionLog(users.First().Guild)
            .SetActionType(LogType.Silence)
            .AddTargets(users)
            .SetTime(DateTimeOffset.UtcNow)
            .SetReason(reason);

        foreach (var user in users)
        {
            if (_config.SafeMode) continue;
            await user.RemoveRoleAsync((ulong)_config.MemberRole);

            _users[user.Id].Silenced = true;
            await FileHelper.SaveUsersAsync(_users);
        }

        await actionLog.Send();
    }

    public async Task AddRole(SocketGuildUser user, ulong roleId, string? reason = null)
    {
        if (!_config.SafeMode) await user.AddRoleAsync(roleId);

        await _modLog.CreateActionLog(user.Guild)
            .SetActionType(LogType.AddRoles)
            .AddTarget(user)
            .AddRole(roleId)
            .SetTime(DateTimeOffset.UtcNow)
            .SetReason(reason)
            .Send();
    }

    public async Task AddRoleToUsers(IEnumerable<SocketGuildUser> users, ulong roleId, string? reason = null)
    {
        if (!users.Any()) throw new NullReferenceException("users must have users in them");
        var actionLog = _modLog.CreateActionLog(users.First().Guild)
            .SetActionType(LogType.AddRoles)
            .AddTargets(users)
            .AddRole(roleId)
            .SetTime(DateTimeOffset.UtcNow)
            .SetReason(reason);

        foreach (var socketGuildUser in users)
        {
            if (!_config.SafeMode) await socketGuildUser.AddRoleAsync(roleId);
        }

        await actionLog.Send();
    }

    public async Task RemoveRole(SocketGuildUser user, ulong roleId, string? reason = null)
    {
        if (!_config.SafeMode) await user.RemoveRoleAsync(roleId);

        await _modLog.CreateActionLog(user.Guild)
            .SetActionType(LogType.RemoveRoles)
            .AddTarget(user)
            .AddRole(roleId)
            .SetTime(DateTimeOffset.UtcNow)
            .SetReason(reason)
            .Send();
    }

    public async Task RemoveRoleFromUsers(IEnumerable<SocketGuildUser> users, ulong roleId, string? reason = null)
    {
        if (!users.Any()) throw new NullReferenceException("users must have users in them");
        var actionLog = _modLog.CreateActionLog(users.First().Guild)
            .SetActionType(LogType.RemoveRoles)
            .AddTargets(users)
            .AddRole(roleId)
            .SetTime(DateTimeOffset.UtcNow)
            .SetReason(reason);

        foreach (var socketGuildUser in users)
        {
            if (!_config.SafeMode) await socketGuildUser.RemoveRoleAsync(roleId);
        }

        await actionLog.Send();
    }

    public async Task AddRoles(SocketGuildUser user, IEnumerable<ulong> roles, string? reason = null)
    {
        if (!_config.SafeMode) await user.AddRolesAsync(roles);

        await _modLog.CreateActionLog(user.Guild)
            .SetActionType(LogType.AddRoles)
            .AddTarget(user)
            .AddRoles(roles)
            .SetTime(DateTimeOffset.UtcNow)
            .SetReason(reason)
            .Send();
    }

    public async Task AddRolesToUsers(IEnumerable<SocketGuildUser> users, IEnumerable<ulong> roles, string? reason = null)
    {
        if (!users.Any()) throw new NullReferenceException("users must have users in them");
        if (!roles.Any()) throw new NullReferenceException("roles must have role ids in them");
        var actionLog = _modLog.CreateActionLog(users.First().Guild)
            .SetActionType(LogType.AddRoles)
            .AddTargets(users)
            .AddRoles(roles)
            .SetTime(DateTimeOffset.UtcNow)
            .SetReason(reason);

        foreach (var socketGuildUser in users)
        {
            if (!_config.SafeMode) await socketGuildUser.AddRolesAsync(roles);
        }

        await actionLog.Send();
    }

    public async Task RemoveRoles(SocketGuildUser user, IEnumerable<ulong> roles, string? reason = null)
    {
        if (!roles.Any()) throw new NullReferenceException("roles must have role ids in them");
        if (!_config.SafeMode) await user.RemoveRolesAsync(roles);

        await _modLog.CreateActionLog(user.Guild)
            .SetActionType(LogType.RemoveRoles)
            .AddTarget(user)
            .AddRoles(roles)
            .SetTime(DateTimeOffset.UtcNow)
            .SetReason(reason)
            .Send();
    }

    public async Task RemoveRolesFromUsers(IEnumerable<SocketGuildUser> users, IEnumerable<ulong> roles, string? reason = null)
    {
        if (!users.Any()) throw new NullReferenceException("users must have users in them");
        if (!roles.Any()) throw new NullReferenceException("roles must have role ids in them");
        var actionLog = _modLog.CreateActionLog(users.First().Guild)
            .SetActionType(LogType.RemoveRoles)
            .AddTargets(users)
            .AddRoles(roles)
            .SetTime(DateTimeOffset.UtcNow)
            .SetReason(reason);

        foreach (var socketGuildUser in users)
        {
            if (!_config.SafeMode) await socketGuildUser.RemoveRolesAsync(roles);
        }

        await actionLog.Send();
    }
}