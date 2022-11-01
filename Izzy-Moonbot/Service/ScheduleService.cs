using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;

namespace Izzy_Moonbot.Service;

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
    private readonly LoggingService _logging;
    private readonly ModService _mod;
    private readonly ModLoggingService _modLoggingService;
    private readonly List<ScheduledTask> _scheduledTasks;

    private bool _alreadyInitiated = false;

    public ScheduleService(List<ScheduledTask> scheduledTasks, ModService mod, ModLoggingService modLoggingService,
        LoggingService logging)
    {
        _scheduledTasks = scheduledTasks;
        _mod = mod;
        _modLoggingService = modLoggingService;
        _logging = logging;
    }

    public void ResumeScheduledTasks(SocketGuild guild)
    {
        if (_alreadyInitiated) return; // Don't allow double execution as this causes scheduled tasks to execute twice
        _alreadyInitiated = true;
        _scheduledTasks.ToArray().ToList().ForEach(scheduledTask =>
        {
            // Work out if the task needs to execute now
            if (scheduledTask.ExecuteAt.ToUniversalTime().ToUnixTimeMilliseconds() <=
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                _logging.Log("Executing following task due to immediate on startup", null,
                    level: LogLevel.Trace);
                ExecuteTask(scheduledTask.Action, guild);
                
                DeleteScheduledTaskOrRepeat(scheduledTask, guild);
            }
            else
            {
                // Task does not need to execute now, but should have a Task set up on a seperate thread to trigger when needed.
                Task.Run(async () =>
                {
                    await Task.Delay(Convert.ToInt32(scheduledTask.ExecuteAt.ToUnixTimeMilliseconds() -
                                                     DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
                    if (!_scheduledTasks.Contains(scheduledTask)) return;
                    _logging.Log("Executing following task due to time passing after restart", null,
                        level: LogLevel.Trace);
                    await ExecuteTask(scheduledTask.Action, guild);
                    await DeleteScheduledTaskOrRepeat(scheduledTask, guild);
                });
            }
        });
    }

    private async Task ExecuteTask(ScheduledTaskAction action, SocketGuild guild)
    {
        switch (action.Type)
        {
            case ScheduledTaskActionType.RemoveRole:
                var roleToRemove = guild.GetRole(ulong.Parse(action.Fields["roleId"]));
                var userToRemoveFrom = guild.GetUser(ulong.Parse(action.Fields["userId"]));
                if (roleToRemove == null || userToRemoveFrom == null) return;
                string reasonForRemoval = null;
                if (action.Fields.ContainsKey("reason")) reasonForRemoval = action.Fields["reason"];

                await _logging.Log(
                    $"Removing {roleToRemove.Name} ({roleToRemove.Id}) from {userToRemoveFrom.Username}#{userToRemoveFrom.Discriminator} ({userToRemoveFrom.Id})",
                    null,
                    level: LogLevel.Trace);

                await _mod.RemoveRole(userToRemoveFrom, roleToRemove.Id, reasonForRemoval);
                await _modLoggingService.CreateModLog(guild)
                    .SetContent(
                        $"Removed <@&{roleToRemove.Id}> from <@{userToRemoveFrom.Id}> (`{userToRemoveFrom.Id}`)")
                    .SetFileLogContent(
                        $"Removed {roleToRemove.Name} ({roleToRemove.Id}) from {userToRemoveFrom.Username}#{userToRemoveFrom.Discriminator} ({userToRemoveFrom.Id})")
                    .Send();
                break;
            case ScheduledTaskActionType.AddRole:
                var roleToAdd = guild.GetRole(ulong.Parse(action.Fields["roleId"]));
                var userToAddTo = guild.GetUser(ulong.Parse(action.Fields["userId"]));
                if (roleToAdd == null || userToAddTo == null) return;
                string reasonForAdding = null;
                if (action.Fields.ContainsKey("reason")) reasonForAdding = action.Fields["reason"];

                await _logging.Log(
                    $"Adding {roleToAdd.Name} ({roleToAdd.Id}) from {userToAddTo.Username}#{userToAddTo.Discriminator} ({userToAddTo.Id})",
                    null,
                    level: LogLevel.Trace);

                await _mod.RemoveRole(userToAddTo, roleToAdd.Id, reasonForAdding);
                await _modLoggingService.CreateModLog(guild)
                    .SetContent(
                        $"Gave <@&{roleToAdd.Id}> to <@{userToAddTo.Id}> (`{userToAddTo.Id}`)")
                    .SetFileLogContent(
                        $"Gave {roleToAdd.Name} ({roleToAdd.Id}) to {userToAddTo.Username}#{userToAddTo.Discriminator} ({userToAddTo.Id})")
                    .Send();
                break;
            case ScheduledTaskActionType.Unban:
                if (!ulong.TryParse(action.Fields["userId"], out var userIdToUnban)) return;
                
                string reasonForUnbanning = null;
                if (action.Fields.ContainsKey("reason")) reasonForUnbanning = action.Fields["reason"];
                
                await _logging.Log(
                    $"Unbanning {userIdToUnban} for {reasonForUnbanning}",
                    level: LogLevel.Trace);

                await guild.RemoveBanAsync(userIdToUnban);
                
                await _modLoggingService.CreateModLog(guild)
                    .SetContent(
                        $"Unbanned <@{userIdToUnban}> for {reasonForUnbanning}")
                    .SetFileLogContent(
                        $"Unbanned {userIdToUnban} for {reasonForUnbanning}")
                    .Send();
                break;
            case ScheduledTaskActionType.Echo:
                var channelToEchoTo = guild.GetTextChannel(ulong.Parse(action.Fields["channelId"]));
                if (channelToEchoTo == null) return;
                var contentToEcho = action.Fields["content"];
                Console.WriteLine(
                    $"#{channelToEchoTo.Name} ({channelToEchoTo.Id}) Scheduled Echo: {contentToEcho}");
                await channelToEchoTo.SendMessageAsync(contentToEcho);
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
            })) return _scheduledTasks.Find(task =>
            {
                if (task.Action.Type == action.Type && task.Action.Fields == action.Fields) return true;
                return false;
            });
        return null;
    }

    public async Task<ScheduledTask> CreateScheduledTask(ScheduledTask task, SocketGuild guild)
    {
        _scheduledTasks.Add(task);
        await FileHelper.SaveScheduleAsync(_scheduledTasks);

        Task.Run(async () =>
        {
            await Task.Delay(Convert.ToInt32(task.ExecuteAt.ToUnixTimeMilliseconds() -
                                             DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
            if (!_scheduledTasks.Contains(task)) return;
            await _logging.Log("Executing following task due to time passing", null,
                level: LogLevel.Trace);
            await ExecuteTask(task.Action, guild);
            await DeleteScheduledTaskOrRepeat(task, guild);
        });

        return task;
    }

    public async Task<ScheduledTask> DeleteScheduledTask(ScheduledTask task)
    {
        var result = _scheduledTasks.Remove(task);
        if (result == null) return null;
        await FileHelper.SaveScheduleAsync(_scheduledTasks);
        return task;
    }

    private async Task<ScheduledTask> DeleteScheduledTaskOrRepeat(ScheduledTask task, SocketGuild guild)
    {
        // Calls DeleteScheduledTask if scheduled task doesn't repeat,
        // Else changes ExecuteAt to be the current time + difference between LastExecutedAt/CreatedAt and ExecuteAt.
        if (task.RepeatType != ScheduledTaskRepeatType.None)
        {
            // Modify task to allow repeatability.
            var taskIndex = _scheduledTasks.FindIndex(scheduledTask => scheduledTask.Id == task.Id);
            
            // Get LastExecutedAt, or CreatedAt if former is null as well as the execution time.
            var creationAt = task.LastExecutedAt ?? task.CreatedAt;
            var executeAt = task.ExecuteAt;

            // RepeatType is checked against null above.
            switch (task.RepeatType)
            {
                case ScheduledTaskRepeatType.Relative:
                    // Get the offset.
                    var repeatEvery = executeAt - creationAt;
            
                    // Get the timestamp of next execution.
                    var nextExecuteAt = DateTimeOffset.UtcNow + repeatEvery;
            
                    // Set previous execution time and new execution time
                    task.LastExecutedAt = executeAt;
                    task.ExecuteAt = nextExecuteAt;
                    break;
                case ScheduledTaskRepeatType.Daily:
                    // Just add a single day to the execute at time lol
                    task.LastExecutedAt = executeAt;
                    task.ExecuteAt = executeAt.AddDays(1);
                    break;
                case ScheduledTaskRepeatType.Weekly:
                    // Add 7 days to the execute at time
                    task.LastExecutedAt = executeAt;
                    task.ExecuteAt = executeAt.AddDays(7);
                    break;
                case ScheduledTaskRepeatType.Yearly:
                    // Add a year to the execute at time
                    task.LastExecutedAt = executeAt;
                    task.ExecuteAt = executeAt.AddYears(1);
                    break;
            }

            // Update the task and save
            _scheduledTasks[taskIndex] = task;
            await FileHelper.SaveScheduleAsync(_scheduledTasks);
            
            Task.Run(async () =>
            {
                await Task.Delay(Convert.ToInt32(task.ExecuteAt.ToUnixTimeMilliseconds() -
                                                 DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
                if (!_scheduledTasks.Contains(_scheduledTasks[taskIndex])) return;
                await _logging.Log("Executing following task due to time passing", null,
                    level: LogLevel.Trace);
                await ExecuteTask(_scheduledTasks[taskIndex].Action, guild);
                await DeleteScheduledTaskOrRepeat(_scheduledTasks[taskIndex], guild);
            });
            
            // Return the new task
            return task;
        }

        // Just delete
        return await DeleteScheduledTask(task);
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
        var fields = new Dictionary<string, string>();

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
        var output = "";

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