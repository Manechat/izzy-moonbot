using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Service
{
    /*
     * The service responsible for handling assigning roles on specific events.
     * TODO: better description lol
     */
    public class RoleService
    {
        private ServerSettings _settings;
        private Dictionary<ulong, User> _users;
        private ModService _mod;
        private LoggingService _logger;
        private ScheduleService _scheduleService;

        public RoleService(ServerSettings settings, Dictionary<ulong, User> users, ModService mod, LoggingService logger, ScheduleService scheduleService)
        {
            this._settings = settings;
            this._users = users;
            this._mod = mod;
            this._logger = logger;
            _scheduleService = scheduleService;
        }

        public async Task ProcessMemberJoin(SocketGuildUser user)
        {
            _logger.Log($"{user.Username}#{user.DiscriminatorValue} ({user.Id}) Joined and recieved MemberRole and NewMemberRole", null, false); 
            await _mod.AddJoinRoles(user);
            
            _users[user.Id].FirstMessageTimestamp = null;

            await FileHelper.SaveUsersAsync(_users);
            
            if (_settings.NewMemberRole != null)
            {
                Dictionary<string, string> fields = new Dictionary<string, string>
                {
                    { "roleId", _settings.NewMemberRole.ToString() },
                    { "userId", user.Id.ToString() },
                    { "reason", $"{_settings.NewMemberRoleDecay} minutes (`NewMemberRoleDecay`) passed, user no longer a new pony." }
                };
                ScheduledTaskAction action = new ScheduledTaskAction(ScheduledTaskActionType.RemoveRole, fields);
                ScheduledTask task = new ScheduledTask(DateTimeOffset.UtcNow,
                    (DateTimeOffset.UtcNow + TimeSpan.FromMinutes(_settings.NewMemberRoleDecay)), action);
                _scheduleService.CreateScheduledTask(task, user.Guild);
            }
        }

        public async Task ProcessMemberMessage(SocketCommandContext context)
        {
            if (_users[context.User.Id].FirstMessageTimestamp == null)
            {
                // They haven't talked before.
                _users[context.User.Id].FirstMessageTimestamp = DateTimeOffset.UtcNow;

                await FileHelper.SaveUsersAsync(_users);

                
            }
        }
    }
}