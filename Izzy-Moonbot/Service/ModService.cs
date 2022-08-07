using System.Linq;
using System.Net;
using Discord;

namespace Izzy_Moonbot.Service
{
    using Discord.Commands;
    using Discord.WebSocket;
    using Izzy_Moonbot.Helpers;
    using Izzy_Moonbot.Settings;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class ModService
    {
        private ServerSettings _settings;
        private Dictionary<ulong, User> _users;
        private ModLoggingService _modLog;

        public ModService(ServerSettings settings, Dictionary<ulong, User> users, ModLoggingService modLog)
        {
            _settings = settings;
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

        public async Task SilenceUser(SocketGuildUser target, DateTimeOffset time, DateTimeOffset? until, string reason = "No reason provided.")
        {
            if (target.IsBot) throw new NotSupportedException("Bots cannot be silenced.");
            if (!(_settings.SafeMode))
            {
                //await target.RemoveRoleAsync(_settings.MemberRole);

                _users[target.Id].Silenced = true;
                await FileHelper.SaveUsersAsync(_users);
            }
            
            await _modLog.CreateActionLog(target.Guild)
                .SetActionType(LogType.Silence)
                .SetTarget(target)
                .SetTime(time)
                .SetUntilTime(until)
                .SetReason(reason)
                .Send();
        }

        public async Task ChangeVerificationLevel(SocketGuild guild, VerificationLevel level, DateTimeOffset time,
            DateTimeOffset? until, string reason = "No reason provided.")
        {
            var previousLevel = guild.VerificationLevel;
            //if (!_settings.SafeMode) await guild.ModifyAsync(properties => properties.VerificationLevel = level);

            await _modLog.CreateActionLog(guild)
                .SetActionType(LogType.VerificationLevel)
                .SetChangelog(previousLevel.ToString().Replace("Extreme", "Highest"), level.ToString().Replace("Extreme", "Highest"))
                .SetTime(time)
                .SetUntilTime(until)
                .SetReason(reason)
                .Send();
        }

        public async Task AddRole(SocketGuildUser user, ulong roleId, string? reason = null, bool log = true)
        {
            //if (!_settings.SafeMode) await user.AddRoleAsync(roleId);

            await _modLog.CreateActionLog(user.Guild)
                .SetActionType(LogType.AddRoles)
                .SetTarget(user)
                .AddRole(roleId)
                .SetReason(reason)
                .Send();
        }
        
        public async Task RemoveRole(SocketGuildUser user, ulong roleId, string? reason = null, bool log = true)
        {
            //if (!_settings.SafeMode) await user.RemoveRoleAsync(roleId);
                
            await _modLog.CreateActionLog(user.Guild)
                .SetActionType(LogType.RemoveRoles)
                .SetTarget(user)
                .AddRole(roleId)
                .SetReason(reason)
                .Send();
        }
        
        public async Task AddRoles(SocketGuildUser user, List<ulong> roles, string? reason = null)
        {
            //if (!_settings.SafeMode) await user.AddRolesAsync(roles);
            
            await _modLog.CreateActionLog(user.Guild)
                .SetActionType(LogType.AddRoles)
                .SetTarget(user)
                .AddRoles(roles)
                .SetReason(reason)
                .Send();
        }
        
        public async Task RemoveRoles(SocketGuildUser user, List<ulong> roles, string? reason = null)
        {
            //if (!_settings.SafeMode) await user.RemoveRolesAsync(roles);
            
            await _modLog.CreateActionLog(user.Guild)
                .SetActionType(LogType.RemoveRoles)
                .SetTarget(user)
                .AddRoles(roles)
                .SetReason(reason)
                .Send();
        }
    }
}
