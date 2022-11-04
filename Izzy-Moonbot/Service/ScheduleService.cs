using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;
using static Izzy_Moonbot.Settings.ScheduledJobActionType;
using static Izzy_Moonbot.Settings.ScheduledJobRepeatType;

namespace Izzy_Moonbot.Service;

/// <summary>
/// Service responsible for the management and execution of scheduled tasks which need to be non-volatile.
/// </summary>
public class ScheduleService
{
    private readonly Config _config;
    private readonly LoggingService _logger;
    private readonly ModService _mod;
    private readonly ModLoggingService _modLogging;

    private readonly List<ScheduledJob> _scheduledJobs;

    private bool _alreadyInitiated;

    public ScheduleService(Config config, ModService mod, ModLoggingService modLogging, LoggingService logger, 
        List<ScheduledJob> scheduledJobs)
    {
        _config = config;
        _logger = logger;
        _mod = mod;
        _modLogging = modLogging;
        _scheduledJobs = scheduledJobs;
    }

    public void BeginUnicycleLoop(SocketGuild guild, DiscordSocketClient client)
    {
        if (_alreadyInitiated) return;
        _alreadyInitiated = true;
        UnicycleLoop(guild, client);
    }

    private void UnicycleLoop(SocketGuild guild, DiscordSocketClient client)
    {
        // Core event loop. Executes every Config.UnicycleInterval seconds.
        Task.Run(async () =>
        {
            await Task.Delay(_config.UnicycleInterval * 1000);
            
            // Run unicycle.
            await Unicycle(guild, client);
            
            // Call self
            UnicycleLoop(guild, client);
        });
    }

    private async Task Unicycle(SocketGuild guild, DiscordSocketClient client)
    {
        var scheduledJobsToExecute = _scheduledJobs.Where(job =>
            job.ExecuteAt.ToUnixTimeMilliseconds() <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        foreach (var job in scheduledJobsToExecute)
        {
            await _logger.Log($"Executing scheduled job queued for execution at {job.ExecuteAt:F}", level: LogLevel.Debug);

            // Do processing here I guess!
            switch (job.Action.Type)
            {
                case RemoveRole:
                    await Unicycle_RemoveRole(job, guild, client);
                    break;
                case AddRole:
                    await Unicycle_AddRole(job, guild, client);
                    break;
                case Unban:
                    await Unicycle_Unban(job, guild, client);
                    break;
                case Echo:
                    await Unicycle_Echo(job, guild, client);
                    break;
                default:
                    throw new NotSupportedException($"{job.Action.Type} is currently not supported.");
            }
            
            await DeleteOrRepeatScheduledJob(job);
        }
    }

    public ScheduledJob GetScheduledJob(string id)
    {
        return _scheduledJobs.Single(job => job.Id == id);
    }

    public ScheduledJob GetScheduledJob(Func<ScheduledJob, bool> predicate)
    {
        return _scheduledJobs.Single(predicate);
    }
    
    public List<ScheduledJob> GetScheduledJobs()
    {
        return _scheduledJobs.ToList();
    }

    public List<ScheduledJob> GetScheduledJobs(Func<ScheduledJob, bool> predicate)
    {
        return _scheduledJobs.Where(predicate).ToList();
    }

    public async Task CreateScheduledJob(ScheduledJob job)
    {
        _scheduledJobs.Add(job);
        await FileHelper.SaveScheduleAsync(_scheduledJobs);
    }

    public async Task ModifyScheduledJob(string id, ScheduledJob job)
    {
        _scheduledJobs[_scheduledJobs.IndexOf(_scheduledJobs.First(job => job.Id == id))] = job;
        await FileHelper.SaveScheduleAsync(_scheduledJobs);
    }

    public async Task DeleteScheduledJob(ScheduledJob job)
    {
        var result = _scheduledJobs.Remove(job);
        if (!result)
            throw new NullReferenceException("The scheduled job provided was not found in the scheduled job list.");
        await FileHelper.SaveScheduleAsync(_scheduledJobs);
    }

    private async Task DeleteOrRepeatScheduledJob(ScheduledJob job)
    {
        if (job.RepeatType != None)
        {
            // Modify job to allow repeatability.
            var taskIndex = _scheduledJobs.FindIndex(scheduledJob => scheduledJob.Id == job.Id);
            
            // Get LastExecutedAt, or CreatedAt if former is null as well as the execution time.
            var creationAt = job.LastExecutedAt ?? job.CreatedAt;
            var executeAt = job.ExecuteAt;

            // RepeatType is checked against null above.
            switch (job.RepeatType)
            {
                case Relative:
                    // Get the offset.
                    var repeatEvery = executeAt - creationAt;
            
                    // Get the timestamp of next execution.
                    var nextExecuteAt = DateTimeOffset.UtcNow + repeatEvery;
            
                    // Set previous execution time and new execution time
                    job.LastExecutedAt = executeAt;
                    job.ExecuteAt = nextExecuteAt;
                    break;
                case Daily:
                    // Just add a single day to the execute at time lol
                    job.LastExecutedAt = executeAt;
                    job.ExecuteAt = executeAt.AddDays(1);
                    break;
                case Weekly:
                    // Add 7 days to the execute at time
                    job.LastExecutedAt = executeAt;
                    job.ExecuteAt = executeAt.AddDays(7);
                    break;
                case Yearly:
                    // Add a year to the execute at time
                    job.LastExecutedAt = executeAt;
                    job.ExecuteAt = executeAt.AddYears(1);
                    break;
            }

            // Update the task and save
            _scheduledJobs[taskIndex] = job;
            await FileHelper.SaveScheduleAsync(_scheduledJobs);

            return;
        }

        await DeleteScheduledJob(job);
    }
    
    public ScheduledJobAction StringToAction(string action)
    {
        ScheduledJobActionType actionType;
        var fields = new Dictionary<string, string>();

        switch (action.Split(" ")[0])
        {
            case "remove-role":
                // remove-role <roleId> from <userId> reason <reason>
                actionType = RemoveRole;
                fields.Add("roleId", action.Split(" ")[1]);
                if (action.Split(" ")[2] != "from") throw new FormatException("Invalid action format");
                fields.Add("userId", action.Split(" ")[3]);
                if (action.Split(" ")[4] != "reason") break;
                fields.Add("reason", string.Join(" ", action.Split(" ").Skip(5)));
                break;
            case "add-role":
                // add-role <roleId> from <userId> reason <reason>
                actionType = AddRole;
                fields.Add("roleId", action.Split(" ")[1]);
                if (action.Split(" ")[2] != "from") throw new FormatException("Invalid action format");
                fields.Add("userId", action.Split(" ")[3]);
                if (action.Split(" ")[4] != "reason") break;
                fields.Add("reason", string.Join(" ", action.Split(" ").Skip(5)));
                break;
            case "unban":
                // unban <userId>
                actionType = Unban;
                fields.Add("userId", action.Split(" ")[1]);
                break;
            case "echo":
                // echo in <channelId> content <content>
                actionType = Echo;
                if (action.Split(" ")[1] != "in") throw new FormatException("Invalid action format");
                fields.Add("channelId", action.Split(" ")[2]);
                if (action.Split(" ")[3] != "content") throw new FormatException("Invalid action format");
                fields.Add("content", string.Join(" ", action.Split(" ").Skip(4)));
                break;
            default:
                throw new FormatException("Invalid action type");
        }

        return new ScheduledJobAction(actionType, fields);
    }

    public string ActionToString(ScheduledJobAction action)
    {
        var output = "";

        switch (action.Type)
        {
            case RemoveRole:
                output += "remove-role ";
                output += $"{action.Fields["roleId"]} ";
                output += $"from {action.Fields["userId"]}";
                if (action.Fields["reason"] != null) output += $" reason {action.Fields["reason"]}";
                break;
            case AddRole:
                output += "add-role ";
                output += $"{action.Fields["roleId"]} ";
                output += $"from {action.Fields["userId"]}";
                if (action.Fields["reason"] != null) output += $" reason {action.Fields["reason"]}";
                break;
            case Unban:
                output += "unban ";
                output += $"{action.Fields["userId"]}";
                break;
            case Echo:
                output += "echo in ";
                output += $"{action.Fields["channelId"]} content ";
                output += $"{action.Fields["content"]}";
                break;
            default:
                throw new FormatException("Invalid action type");
        }

        return output;
    }
    
    // Executors for different types.
    private async Task Unicycle_AddRole(ScheduledJob job, SocketGuild guild, DiscordSocketClient client)
    {
        var role = guild.GetRole(ulong.Parse(job.Action.Fields["roleId"]));
        var user = guild.GetUser(ulong.Parse(job.Action.Fields["userId"]));
        if (role == null || user == null) return;

        string? reason = null;
        if (job.Action.Fields.ContainsKey("reason")) reason = job.Action.Fields["reason"];
        
        await _logger.Log(
            $"Adding {role.Name} ({role.Id}) to {user.Username}#{user.Discriminator} ({user.Id})", level: LogLevel.Debug);
        
        await _mod.AddRole(user, role.Id, reason);
        await _modLogging.CreateModLog(guild)
            .SetContent(
                $"Gave <@&{role.Id}> to <@{user.Id}> (`{user.Id}`). {(reason != null ? $"Reason: {reason}." : "")}")
            .SetFileLogContent(
                $"Gave {role.Name} ({role.Id}) to {user.Username}#{user.Discriminator} ({user.Id}). {(reason != null ? $"Reason: {reason}." : "")}")
            .Send();
    }
    
    private async Task Unicycle_RemoveRole(ScheduledJob job, SocketGuild guild, DiscordSocketClient client)
    {
        if (!ulong.TryParse(job.Action.Fields["roleId"], out var roleId)) return;
        if (!ulong.TryParse(job.Action.Fields["userId"], out var userId)) return;
        var role = guild.GetRole(roleId);
        var user = guild.GetUser(userId);
        if (role == null || user == null) return;

        string? reason = null;
        if (job.Action.Fields.ContainsKey("reason")) reason = job.Action.Fields["reason"];
        
        await _logger.Log(
            $"Removing {role.Name} ({role.Id}) from {user.Username}#{user.Discriminator} ({user.Id})", level: LogLevel.Debug);
        
        await _mod.RemoveRole(user, role.Id, reason);
        await _modLogging.CreateModLog(guild)
            .SetContent(
                $"Removed <@&{role.Id}> from <@{user.Id}> (`{user.Id}`){(reason != null ? $" for {reason}" : "")}")
            .SetFileLogContent(
                $"Removed {role.Name} ({role.Id}) from {user.Username}#{user.Discriminator} ({user.Id}){(reason != null ? $" for {reason}" : "")}")
            .Send();
    }

    private async Task Unicycle_Unban(ScheduledJob job, SocketGuild guild, DiscordSocketClient client)
    {
        if (!ulong.TryParse(job.Action.Fields["userId"], out var userId)) return;
        if (await guild.GetBanAsync(userId) == null) return;

        var user = await client.GetUserAsync(userId);
        
        await _logger.Log(
            $"Unbanning {(user == null ? userId : $"")}.",
            level: LogLevel.Debug);

        await guild.RemoveBanAsync(userId);

        var embed = new EmbedBuilder()
            .WithTitle(
                $"Unbanned {(user != null ? $"{user.Username}#{user.Discriminator} " : "")}<@{userId}> ({userId})")
            .WithColor(16737792)
            .WithFooter("Gasp! Does this mean I can invite them to our next traditional unicorn sleepover?")
            .Build();
        
        await _modLogging.CreateModLog(guild)
            .SetEmbed(embed)
            .SetFileLogContent($"Unbanned {(user != null ? $"{user.Username}#{user.Discriminator} " : "")}<@{userId}> ({userId})")
            .Send();
    }

    private async Task Unicycle_Echo(ScheduledJob job, SocketGuild guild, DiscordSocketClient client)
    {
        if (!job.Action.Fields.ContainsKey("content")) return;
        if (!ulong.TryParse(job.Action.Fields["channelId"], out var channelId)) return;
        var channel = guild.GetTextChannel(channelId);
        if (channel == null)
        {
            var user = await client.GetUserAsync(channelId);
            if (user == null) return;

            await user.SendMessageAsync(job.Action.Fields["content"]);
            return;
        }

        await channel.SendMessageAsync(job.Action.Fields["content"]);
    }
}