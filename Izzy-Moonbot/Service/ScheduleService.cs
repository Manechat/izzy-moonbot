using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;

namespace Izzy_Moonbot.Service
{
    /*
     * This service is responsible for the management and execution of
     * scheduled tasks that need to be non-volitile.
     *
     * All tasks that "need to happen later" that aren't just changing
     * volitle data values are added to this scheduler.
     *
     * This scheduler is not exposed in user-friendly ways, with the only
     * user management for them being via debug commands. This is thus not
     * equal to SweetieBot's `Scheduler` module.
     */
    public class ScheduleService
    {
        private List<ScheduledTask> _scheduledTasks;
        private LoggingService _logging;
        private ModService _mod;
        private ModLoggingService _modLoggingService;

        public ScheduleService(List<ScheduledTask> scheduledTasks, ModService mod, ModLoggingService modLoggingService, LoggingService logging)
        {
            _scheduledTasks = scheduledTasks;
            _mod = mod;
            _modLoggingService = modLoggingService;
            _logging = logging;
        }

        public void ResumeScheduledTasks(SocketGuild guild)
        {
            _scheduledTasks.ToArray().ToList().ForEach(scheduledTask =>
            {
                // Work out if the task needs to execute now
                if (scheduledTask.ExecuteAt.ToUniversalTime().ToUnixTimeMilliseconds() <=
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    _logging.Log("Executing following task due to immediate on startup", null,
                        level: LogLevel.Trace);
                    this.ExecuteTask(scheduledTask.Action, guild);
                    this.DeleteScheduledTask(scheduledTask);
                }
                else
                {
                    // Task does not need to execute now, but should have a Task set up on a seperate thread to trigger when needed.
                    Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(Convert.ToInt32(scheduledTask.ExecuteAt.ToUnixTimeMilliseconds() -
                                                         DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
                        if (!_scheduledTasks.Contains(scheduledTask)) return;
                        _logging.Log("Executing following task due to time passing after restart", null,
                            level: LogLevel.Trace);
                        await this.ExecuteTask(scheduledTask.Action, guild);
                        await this.DeleteScheduledTask(scheduledTask);
                    });
                }
            });
        }

        private async Task ExecuteTask(ScheduledTaskAction action, SocketGuild guild)
        {
            switch (action.Type)
            {
                case ScheduledTaskActionType.RemoveRole:
                    SocketRole roleToRemove = guild.GetRole(ulong.Parse(action.Fields["roleId"]));
                    SocketGuildUser userToRemoveFrom = guild.GetUser(ulong.Parse(action.Fields["userId"]));
                    if (roleToRemove == null || userToRemoveFrom == null) return;
                    string reasonForRemoval = null;
                    if (action.Fields.ContainsKey("reason")) reasonForRemoval = action.Fields["reason"];

                    _logging.Log($"Removing {roleToRemove.Name} ({roleToRemove.Id}) from {userToRemoveFrom.Username}#{userToRemoveFrom.Discriminator} ({userToRemoveFrom.Id})", null,
                        level: LogLevel.Trace);
                    
                    await _mod.RemoveRole(userToRemoveFrom, roleToRemove.Id, DateTimeOffset.Now, reasonForRemoval);
                    await _modLoggingService.CreateModLog(guild)
                        .SetContent(
                            $"Removed <@&{roleToRemove.Id}> from <@{userToRemoveFrom.Id}> (`{userToRemoveFrom.Id}`)")
                        .Send();
                    break;
                case ScheduledTaskActionType.AddRole:
                    SocketRole roleToAdd = guild.GetRole(ulong.Parse(action.Fields["roleId"]));
                    SocketGuildUser userToAddTo = guild.GetUser(ulong.Parse(action.Fields["userId"]));
                    if (roleToAdd == null || userToAddTo == null) return;
                    string reasonForAdding = null;
                    if (action.Fields.ContainsKey("reason")) reasonForAdding = action.Fields["reason"];
                    
                    _logging.Log($"Adding {roleToAdd.Name} ({roleToAdd.Id}) from {userToAddTo.Username}#{userToAddTo.Discriminator} ({userToAddTo.Id})", null,
                        level: LogLevel.Trace);

                    await _mod.RemoveRole(userToAddTo, roleToAdd.Id, DateTimeOffset.Now, reasonForAdding);
                    await _modLoggingService.CreateModLog(guild)
                        .SetContent(
                            $"Gave <@&{roleToAdd.Id}> to <@{userToAddTo.Id}> (`{userToAddTo.Id}`)")
                        .Send();
                    break;
                case ScheduledTaskActionType.Unban:
                    throw new NotImplementedException("Timed bans are not implemented at this current time.");
                    break;
                case ScheduledTaskActionType.Echo:
                    SocketTextChannel channelToEchoTo = guild.GetTextChannel(ulong.Parse(action.Fields["channelId"]));
                    if (channelToEchoTo == null) return;
                    string contentToEcho = action.Fields["content"];
                    Console.WriteLine(
                        $"#{channelToEchoTo.Name} ({channelToEchoTo.Id}) Scheduled Echo: {contentToEcho}");
                    channelToEchoTo.SendMessageAsync(contentToEcho);
                    break;
                default:
                    throw new NotSupportedException($"{action.Type} is currently not supported.");
            }
        }

        public List<ScheduledTask> GetScheduledTasks()
        {
            return _scheduledTasks;
        }
        
        public ScheduledTask GetScheduledTaskByAction(ScheduledTaskAction action)
        {
            if (_scheduledTasks.Exists(task =>
                {
                    if (task.Action.Type == action.Type && task.Action.Fields == action.Fields) return true;
                    return false;
                }))
            {
                return _scheduledTasks.Find(task =>
                {
                    if (task.Action.Type == action.Type && task.Action.Fields == action.Fields) return true;
                    return false;
                });
            }
            return null;
        }

        public async Task<ScheduledTask> CreateScheduledTask(ScheduledTask task, SocketGuild guild)
        {
            _scheduledTasks.Add(task);
            await FileHelper.SaveScheduleAsync(_scheduledTasks);
            
            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(Convert.ToInt32(task.ExecuteAt.ToUnixTimeMilliseconds() - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
                if (!_scheduledTasks.Contains(task)) return;
                _logging.Log("Executing following task due to time passing", null,
                    level: LogLevel.Trace);
                this.ExecuteTask(task.Action, guild);
                this.DeleteScheduledTask(task);
            });
            
            return task;
        }
        
        public async Task<ScheduledTask> DeleteScheduledTask(ScheduledTask task)
        {
            bool result = _scheduledTasks.Remove(task);
            if (result == null) return null;
            await FileHelper.SaveScheduleAsync(_scheduledTasks);
            return task;
        }
        
        public async Task<ScheduledTask> ModifyScheduledTask(ScheduledTask originalTask, ScheduledTask newTask)
        {
            _scheduledTasks[_scheduledTasks.IndexOf(originalTask)] = newTask;
            await FileHelper.SaveScheduleAsync(_scheduledTasks);
            return newTask;
        }

        public ScheduledTaskAction stringToAction(string action)
        {
            ScheduledTaskActionType actionType;
            Dictionary<string, string> fields = new Dictionary<string, string>();

            switch (action.Split(" ")[0])
            {
                case "remove-role":
                    // remove-role <roleId> from <userId> reason <reason>
                    actionType = ScheduledTaskActionType.RemoveRole;
                    fields.Add("roleId", action.Split(" ")[1]);
                    if (action.Split(" ")[2] != "from") throw new FormatException("Invalid action format");
                    fields.Add("userId", action.Split(" ")[3]);
                    if (action.Split(" ")[4] != "reason") break;
                    fields.Add("reason", string.Join(" ", action.Split(" ").Skip(5)));
                    break;
                case "add-role":
                    // add-role <roleId> from <userId> reason <reason>
                    actionType = ScheduledTaskActionType.AddRole;
                    fields.Add("roleId", action.Split(" ")[1]);
                    if (action.Split(" ")[2] != "from") throw new FormatException("Invalid action format");
                    fields.Add("userId", action.Split(" ")[3]);
                    if (action.Split(" ")[4] != "reason") break;
                    fields.Add("reason", string.Join(" ", action.Split(" ").Skip(5)));
                    break;
                case "unban":
                    // unban <userId> reason <reason>
                    actionType = ScheduledTaskActionType.Unban;
                    fields.Add("userId", action.Split(" ")[1]);
                    if (action.Split(" ")[2] != "reason") break;
                    fields.Add("reason", string.Join(" ", action.Split(" ").Skip(3)));
                    break;
                case "echo":
                    // echo in <channelId> content <content>
                    actionType = ScheduledTaskActionType.Echo;
                    if (action.Split(" ")[1] != "in") throw new FormatException("Invalid action format");
                    fields.Add("channelId", action.Split(" ")[2]);
                    if (action.Split(" ")[3] != "content") throw new FormatException("Invalid action format");
                    fields.Add("content", string.Join(" ", action.Split(" ").Skip(4)));
                    break;
                default:
                    throw new FormatException("Invalid action type");
            }

            return new ScheduledTaskAction(actionType, fields);
        }
        
        public string actionToString(ScheduledTaskAction action)
        {
            string output = "";

            switch (action.Type)
            {
                case ScheduledTaskActionType.RemoveRole:
                    output += "remove-role ";
                    output += $"{action.Fields["roleId"]} ";
                    output += $"from {action.Fields["userId"]}";
                    if (action.Fields["reason"] != null) output += $" reason {action.Fields["reason"]}";
                    break;
                case ScheduledTaskActionType.AddRole:
                    output += "add-role ";
                    output += $"{action.Fields["roleId"]} ";
                    output += $"from {action.Fields["userId"]}";
                    if (action.Fields["reason"] != null) output += $" reason {action.Fields["reason"]}";
                    break;
                case ScheduledTaskActionType.Unban:
                    output += "unban ";
                    output += $"{action.Fields["userId"]}";
                    if (action.Fields["reason"] != null) output += $" reason {action.Fields["reason"]}";
                    break;
                case ScheduledTaskActionType.Echo:
                    output += "echo in ";
                    output += $"{action.Fields["channelId"]} content ";
                    output += $"{action.Fields["content"]}";
                    break;
                default:
                    throw new FormatException("Invalid action type");
            }

            return output;
        }
    }
}