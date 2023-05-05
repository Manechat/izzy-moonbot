using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.WebSocket;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Service;

public class ModService
{
    private readonly Config _config;
    private readonly Dictionary<ulong, User> _users;

    public ModService(Config config, Dictionary<ulong, User> users)
    {
        _config = config;
        _users = users;
    }

    public async Task SilenceUser(SocketGuildUser user, string? reason)
    {
        await SilenceUser(new SocketGuildUserAdapter(user), reason);
    }
    public async Task SilenceUser(IIzzyGuildUser user, string? reason)
    {
        if (_config.MemberRole == null) throw new TargetException("MemberRole config value is null (not set)");
        
        await user.RemoveRoleAsync((ulong) _config.MemberRole, reason is null ? null : new Discord.RequestOptions { AuditLogReason = reason });
            
        _users[user.Id].Silenced = true;
        await FileHelper.SaveUsersAsync(_users);
    }

    public async Task SilenceUsers(IEnumerable<SocketGuildUser> users, string? reason)
    {
        await SilenceUsers(users.Select(u => new SocketGuildUserAdapter(u)).ToList(), reason);
    }
    public async Task SilenceUsers(IEnumerable<IIzzyGuildUser> users, string? reason)
    {
        if (_config.MemberRole == null) throw new TargetException("MemberRole config value is null (not set)");

        foreach (var user in users)
        {
            await user.RemoveRoleAsync((ulong)_config.MemberRole, reason is null ? null : new Discord.RequestOptions { AuditLogReason = reason });

            _users[user.Id].Silenced = true;
            await FileHelper.SaveUsersAsync(_users);
        }
    }

    public async Task AddRole(SocketGuildUser user, ulong roleId, string? reason)
    {
        await AddRole(new SocketGuildUserAdapter(user), roleId, reason);
    }
    public async Task AddRole(IIzzyGuildUser user, ulong roleId, string? reason)
    {
        await user.AddRoleAsync(roleId, reason is null ? null : new Discord.RequestOptions { AuditLogReason = reason });
    }

    public async Task RemoveRole(SocketGuildUser user, ulong roleId, string? reason)
    {
        await RemoveRole(new SocketGuildUserAdapter(user), roleId, reason);
    }
    public async Task RemoveRole(IIzzyGuildUser user, ulong roleId, string? reason)
    {
        await user.RemoveRoleAsync(roleId, reason is null ? null : new Discord.RequestOptions { AuditLogReason = reason });
    }

    public async Task AddRoles(SocketGuildUser user, IEnumerable<ulong> roles, string? reason)
    {
        await AddRoles(new SocketGuildUserAdapter(user), roles, reason);
    }
    public async Task AddRoles(IIzzyGuildUser user, IEnumerable<ulong> roles, string? reason)
    {
        await user.AddRolesAsync(roles, reason is null ? null : new Discord.RequestOptions { AuditLogReason = reason });
    }
}