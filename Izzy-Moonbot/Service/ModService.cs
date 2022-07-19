using System.Linq;
using System.Net;

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

        public ModService(ServerSettings settings, Dictionary<ulong, User> users)
        {
            _settings = settings;
            _users = users;
        }

        private string GetActionName(ActionType action)
        {
            string output = "";
            switch (action)
            {
                case ActionType.Notice:
                    output = "Notice";
                    break;
                case ActionType.AddRoles:
                    output = "Roles added";
                    break;
                case ActionType.RemoveRoles:
                    output = "Roles removed";
                    break;
                case ActionType.Silence:
                    output = "Silence";
                    break;
                case ActionType.Banish:
                    output = "Banish";
                    break;
                case ActionType.Ban:
                    output = "Ban";
                    break;
                case ActionType.Unban:
                    output = "Unban";
                    break;
                default:
                    output = "what";
                    break;
            }

            return output;
        }

        private Discord.Color GetActionColor(ActionType action)
        {
            int output = 0x000000;
            switch (action)
            {
                case ActionType.Notice:
                case ActionType.AddRoles: 
                case ActionType.RemoveRoles:
                    output = 0x002920;
                    break;
                case ActionType.Silence:
                    output = 0xffbb00;
                    break;
                case ActionType.Banish:
                    output = 0xff8800;
                    break;
                case ActionType.Ban:
                    output = 0xaa0000;
                    break;
                case ActionType.Unban:
                    output = 0x00ff00;
                    break;
                default:
                    output = 0x000000;
                    break;
            }

            return new Discord.Color((uint)output);
        }

        /// <summary>
        /// Log an action Izzy has made to the bot log.
        /// </summary>
        /// <param name="action">The type of action to log.</param>
        /// <param name="target">The target of the action.</param>
        /// <param name="time">The time the action took place.</param>
        /// <param name="until">The time the action is over, if applicable.</param>
        /// <param name="reason">The reason the action was taken.</param>
        /// <param name="role">The role Izzy gave/removed, if any</param>
        /// <returns></returns>
        public async Task LogBotAction(ActionType action, SocketGuildUser target, DateTimeOffset time, DateTimeOffset? until, string reason, List<ulong>? roles = null)
        {
            long startUnixTimestamp = time.ToUnixTimeSeconds();
            string startTimestamp = $"<t:{startUnixTimestamp}:F>";
            string untilTimestamp = "";

            if (until.HasValue == false) untilTimestamp = "Never (Permanent)";
            else
            {
                long untilUnixTimestamp = until.Value.ToUnixTimeSeconds();
                untilTimestamp = $"<t:{untilUnixTimestamp}:F>";
            }

            if (untilTimestamp == "<t:0:F>") untilTimestamp = null;

            Discord.EmbedBuilder embed = new Discord.EmbedBuilder()
                .WithDescription("<:info:964284521488986133> **This was an automated action Izzy Moonbot took.**")
                .WithColor(GetActionColor(action))
                .AddField("User", $"<@{target.Id}> (`{target.Id}`)", true)
                .AddField("Action", GetActionName(action), true)
                .AddField("Occured At", startTimestamp, true);

            if (action == ActionType.Silence || action == ActionType.Banish || action == ActionType.Ban)
            {
                embed.AddField("Ends At", untilTimestamp, true);
            }
            
            if (action == ActionType.AddRoles || action == ActionType.RemoveRoles)
            {
                embed.AddField($"Roles", string.Join(", ", roles.Select(role => $"<@&{role}>")), true);
            }

            embed.AddField("Reason", reason, false);

            await target.Guild.GetTextChannel(_settings.LogChannel).SendMessageAsync(embed: embed.Build());
        }

        /// <summary>
        /// Log an action Izzy wants to make but cannot due to SafeMode to the bot log.
        /// </summary>
        /// <param name="action">The type of action to log.</param>
        /// <param name="target">The target of the action.</param>
        /// <param name="time">The time the action was going to take place.</param>
        /// <param name="until">The time the action would be over, if applicable.</param>
        /// <param name="reason">The reason the action Izzy wanted to take would be taken.</param>
        /// <param name="role">The role Izzy wanted to give/remove, if any</param>
        /// <returns></returns>
        public async Task LogSafeModeBotAction(ActionType action, SocketGuildUser target, DateTimeOffset time, DateTimeOffset? until, string reason, List<ulong>? roles = null)
        {
            long startUnixTimestamp = time.ToUnixTimeSeconds();
            string startTimestamp = $"<t:{startUnixTimestamp}:F>";
            string untilTimestamp = "";

            if (until.HasValue == false) untilTimestamp = "Never (Permanent)";
            else
            {
                long untilUnixTimestamp = until.Value.ToUnixTimeSeconds();
                untilTimestamp = $"<t:{untilUnixTimestamp}:F>";
            }

            if (untilTimestamp == "<t:0:F>") untilTimestamp = null;

            Discord.EmbedBuilder embed = new Discord.EmbedBuilder()
                .WithDescription("<:info:964284521488986133> **This was an automated action Izzy Moonbot would have taken outside of `SafeMode`.**")
                .WithColor(GetActionColor(action))
                .AddField("User", $"<@{target.Id}> (`{target.Id}`)", true)
                .AddField("Action", GetActionName(action), true)
                .AddField("Occured At", startTimestamp, true);

            if (action == ActionType.Silence || action == ActionType.Banish || action == ActionType.Ban)
            {
                embed.AddField("Ends At", untilTimestamp, true);
            }

            if (action == ActionType.AddRoles || action == ActionType.RemoveRoles)
            {
                embed.AddField($"Roles", string.Join(", ", roles.Select(role => $"<@&{role}>")), true);
            }

            embed.AddField("Reason", reason, false);

            await target.Guild.GetTextChannel(_settings.LogChannel).SendMessageAsync(embed: embed.Build());
        }

        public async Task<string> GenerateSuggestedLog(ActionType action, SocketGuildUser target, DateTimeOffset time, DateTimeOffset? until, string reason)
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
        }

        public async Task SilenceUser(SocketGuildUser target, DateTimeOffset time, DateTimeOffset? until, string reason = "No reason provided.", bool log = true)
        {
            if (target.IsBot) throw new NotSupportedException("Bots cannot be silenced.");
            if (!(_settings.SafeMode))
            {
                //await target.RemoveRoleAsync(_settings.MemberRole);

                Punishment punishment = new Punishment();
                punishment.Action = ActionType.Silence;
                punishment.EndsAt = until;

                _users[target.Id].ActivePunishments.Add(punishment);
                await FileHelper.SaveUsersAsync(_users);

                if (log)
                {
                    await LogBotAction(ActionType.Silence, target, time, until, reason);
                }
            }
            else
            {
                await LogSafeModeBotAction(ActionType.Silence, target, time, until, reason);
            }
        }

        public async Task AddRole(SocketGuildUser user, ulong roleId, string? reason = null, bool log = true)
        {
            if (!_settings.SafeMode)
            {
                //await user.AddRoleAsync(roleId);

                List<ulong> roles = new List<ulong>();
                roles.Add(roleId);
                
                if (log)
                {
                    await LogBotAction(ActionType.AddRoles, user, DateTimeOffset.Now, null, reason, roles);
                }
            }
            else
            {
                List<ulong> roles = new List<ulong>();
                roles.Add(roleId);
                
                await LogSafeModeBotAction(ActionType.AddRoles, user, DateTimeOffset.Now, null, reason, roles);
            }
        }
        
        public async Task RemoveRole(SocketGuildUser user, ulong roleId, string? reason = null, bool log = true)
        {
            if (!_settings.SafeMode)
            {
                //await user.RemoveRoleAsync(roleId);
                
                List<ulong> roles = new List<ulong>();
                roles.Add(roleId);
                
                if (log)
                {
                    await LogBotAction(ActionType.RemoveRoles, user, DateTimeOffset.Now, null, reason, roles);
                }
            }
            else
            {
                List<ulong> roles = new List<ulong>();
                roles.Add(roleId);
                
                await LogSafeModeBotAction(ActionType.RemoveRoles, user, DateTimeOffset.Now, null, reason, roles);
            }
        }
        
        public async Task AddJoinRoles(SocketGuildUser user, bool log = true)
        {
            if (!_settings.SafeMode)
            {
                log = false;
                List<ulong> roles = new List<ulong>();
                string expiresString = "";

                if (_settings.MemberRole != null)
                {
                    if (!_settings.AutoSilenceNewJoins)
                    {
                        log = true;
                        //user.AddRoleAsync((ulong) _settings.MemberRole);
                        roles.Add((ulong)_settings.MemberRole);
                    }
                }
                
                if (_settings.NewMemberRole != null)
                {
                    log = true;
                    //user.AddRoleAsync((ulong) _settings.NewMemberRole);
                    roles.Add((ulong) _settings.NewMemberRole);
                    expiresString =
                        $"{Environment.NewLine}New Member role expires in <t:{(DateTimeOffset.UtcNow + TimeSpan.FromMinutes(_settings.NewMemberRoleDecay)).ToUnixTimeSeconds()}:R>";
                }
                    
                if (log)
                {
                    string autoSilence = $" (User autosilenced, `AuthoSilenceNewJoins` is true.";
                    if (!_settings.AutoSilenceNewJoins) autoSilence = "";
                    
                    await LogBotAction(ActionType.AddRoles, user, DateTimeOffset.Now, null, $"New user join{autoSilence}.{expiresString}", roles);
                }
            }
            else if (_settings.MemberRole != null || _settings.NewMemberRole != null)
            {
                List<ulong> roles = new List<ulong>();
                string expiresString = "";

                if (_settings.MemberRole != null)
                {
                    roles.Add((ulong) _settings.MemberRole);
                }
                
                if (_settings.NewMemberRole != null)
                {
                    roles.Add((ulong) _settings.NewMemberRole);
                    expiresString =
                        $"{Environment.NewLine}New Member role expires in <t:{(DateTimeOffset.UtcNow + TimeSpan.FromMinutes(_settings.NewMemberRoleDecay)).ToUnixTimeSeconds()}:R>";
                }
                
                await LogSafeModeBotAction(ActionType.AddRoles, user, DateTimeOffset.Now, null, $"New user join.{expiresString}", roles);
            }
        }
    }

    public enum ActionType
    {
        Notice,
        AddRoles,
        RemoveRoles,
        Silence,
        Banish,
        Ban,
        Unban
    }
}
