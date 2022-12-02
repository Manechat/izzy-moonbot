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

    public ModService(Config config, Dictionary<ulong, User> users)
    {
        _config = config;
        _users = users;
    }

    public async Task KickUser(SocketGuildUser user, DateTimeOffset time, string? reason = null)
    {
        await user.KickAsync(reason);
    }

    public async Task KickUsers(IEnumerable<SocketGuildUser> users, string? reason = null)
    {
        if (!users.Any()) throw new NullReferenceException("users must have users in them");

        foreach (var user in users)
        {
            await user.KickAsync(reason);
        }
    }

    public async Task SilenceUser(SocketGuildUser user, string? reason = null)
    {
        if (_config.MemberRole == null) throw new TargetException("MemberRole config value is null (not set)");
        
        await user.RemoveRoleAsync((ulong) _config.MemberRole);
            
        _users[user.Id].Silenced = true;
        await FileHelper.SaveUsersAsync(_users);
    }

    public async Task SilenceUsers(IEnumerable<SocketGuildUser> users, string? reason = null)
    {
        if (_config.MemberRole == null) throw new TargetException("MemberRole config value is null (not set)");
        
        if (!users.Any()) throw new NullReferenceException("users must have users in them");

        foreach (var user in users)
        {
            await user.RemoveRoleAsync((ulong)_config.MemberRole);

            _users[user.Id].Silenced = true;
            await FileHelper.SaveUsersAsync(_users);
        }
    }

    public async Task AddRole(SocketGuildUser user, ulong roleId, string? reason = null)
    {
        await user.AddRoleAsync(roleId);
    }

    public async Task AddRoleToUsers(IEnumerable<SocketGuildUser> users, ulong roleId, string? reason = null)
    {
        if (!users.Any()) throw new NullReferenceException("users must have users in them");

        foreach (var socketGuildUser in users)
        {
            await socketGuildUser.AddRoleAsync(roleId);
        }
    }

    public async Task RemoveRole(SocketGuildUser user, ulong roleId, string? reason = null)
    {
        await user.RemoveRoleAsync(roleId);
    }

    public async Task RemoveRoleFromUsers(IEnumerable<SocketGuildUser> users, ulong roleId, string? reason = null)
    {
        if (!users.Any()) throw new NullReferenceException("users must have users in them");

        foreach (var socketGuildUser in users)
        {
            await socketGuildUser.RemoveRoleAsync(roleId);
        }
    }

    public async Task AddRoles(SocketGuildUser user, IEnumerable<ulong> roles, string? reason = null)
    {
        await user.AddRolesAsync(roles);
    }

    public async Task AddRolesToUsers(IEnumerable<SocketGuildUser> users, IEnumerable<ulong> roles, string? reason = null)
    {
        if (!users.Any()) throw new NullReferenceException("users must have users in them");
        if (!roles.Any()) throw new NullReferenceException("roles must have role ids in them");

        foreach (var socketGuildUser in users)
        {
            await socketGuildUser.AddRolesAsync(roles);
        }
    }

    public async Task RemoveRoles(SocketGuildUser user, IEnumerable<ulong> roles, string? reason = null)
    {
        if (!roles.Any()) throw new NullReferenceException("roles must have role ids in them");
        await user.RemoveRolesAsync(roles);
    }

    public async Task RemoveRolesFromUsers(IEnumerable<SocketGuildUser> users, IEnumerable<ulong> roles, string? reason = null)
    {
        if (!users.Any()) throw new NullReferenceException("users must have users in them");
        if (!roles.Any()) throw new NullReferenceException("roles must have role ids in them");

        foreach (var socketGuildUser in users)
        {
            await socketGuildUser.RemoveRolesAsync(roles);
        }
    }
}