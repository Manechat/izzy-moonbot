﻿using System;
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
    private readonly UserService _users;

    public ModService(Config config, UserService users)
    {
        _config = config;
        _users = users;
    }

    public async Task SilenceUser(SocketGuildUser user, string? reason = null)
    {
        await SilenceUser(new SocketGuildUserAdapter(user), reason);
    }
    public async Task SilenceUser(IIzzyGuildUser user, string? reason = null)
    {
        if (_config.MemberRole == null) throw new TargetException("MemberRole config value is null (not set)");
        
        await user.RemoveRoleAsync((ulong) _config.MemberRole, reason is null ? null : new Discord.RequestOptions { AuditLogReason = reason });

        var userData = await _users.GetUser(user) ?? throw new NullReferenceException("User is null!");

        userData.Silenced = true;

        await _users.ModifyUser(userData);
    }

    public async Task SilenceUsers(IEnumerable<SocketGuildUser> users, string? reason = null)
    {
        await SilenceUsers(users.Select(u => new SocketGuildUserAdapter(u)).ToList(), reason);
    }
    public async Task SilenceUsers(IEnumerable<IIzzyGuildUser> users, string? reason = null)
    {
        if (_config.MemberRole == null) throw new TargetException("MemberRole config value is null (not set)");
        
        if (!users.Any()) throw new NullReferenceException("users must have users in them");

        foreach (var user in users)
        {
            await user.RemoveRoleAsync((ulong)_config.MemberRole, reason is null ? null : new Discord.RequestOptions { AuditLogReason = reason });

            var userData = await _users.GetUser(user) ?? throw new NullReferenceException("User is null!");

            userData.Silenced = true;

            await _users.ModifyUser(userData);
        }
    }

    public async Task AddRole(SocketGuildUser user, ulong roleId, string? reason = null)
    {
        await AddRole(new SocketGuildUserAdapter(user), roleId, reason);
    }
    public async Task AddRole(IIzzyGuildUser user, ulong roleId, string? reason = null)
    {
        await user.AddRoleAsync(roleId, reason is null ? null : new Discord.RequestOptions { AuditLogReason = reason });
    }

    public async Task RemoveRole(SocketGuildUser user, ulong roleId, string? reason = null)
    {
        await RemoveRole(new SocketGuildUserAdapter(user), roleId, reason);
    }
    public async Task RemoveRole(IIzzyGuildUser user, ulong roleId, string? reason = null)
    {
        await user.RemoveRoleAsync(roleId, reason is null ? null : new Discord.RequestOptions { AuditLogReason = reason });
    }

    public async Task AddRoles(SocketGuildUser user, IEnumerable<ulong> roles, string? reason = null)
    {
        await AddRoles(new SocketGuildUserAdapter(user), roles, reason);
    }
    public async Task AddRoles(IIzzyGuildUser user, IEnumerable<ulong> roles, string? reason = null)
    {
        await user.AddRolesAsync(roles, reason is null ? null : new Discord.RequestOptions { AuditLogReason = reason });
    }
}