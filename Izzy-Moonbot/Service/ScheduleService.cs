using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Flurl.Http;
using Izzy_Moonbot.EventListeners;
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
    private State _state;

    private readonly List<ScheduledJob> _scheduledJobs;

    private bool _alreadyInitiated;

    public ScheduleService(Config config, ModService mod, ModLoggingService modLogging, LoggingService logger, State state,
        List<ScheduledJob> scheduledJobs)
    {
        _config = config;
        _logger = logger;
        _mod = mod;
        _modLogging = modLogging;
        _state = state;
        _scheduledJobs = scheduledJobs;
    }

    public void BeginUnicycleLoop(DiscordSocketClient client)
    {
        if (_alreadyInitiated) return;
        _alreadyInitiated = true;
        UnicycleLoop(client);
    }

    private void UnicycleLoop(DiscordSocketClient client)
    {
        // Core event loop. Executes every Config.UnicycleInterval seconds.
        Task.Run(async () =>
        {
            await Task.Delay(_config.UnicycleInterval);
            
            // Run unicycle.
            try
            {
                await Unicycle(client);
            }
            catch (Exception exception)
            {
                _logger.Log($"{exception.Message}{Environment.NewLine}{exception.StackTrace}", level: LogLevel.Error);
            }

            // Call self
            UnicycleLoop(client);
        });
    }

    private async Task Unicycle(DiscordSocketClient client)
    {
        var scheduledJobsToExecute = new List<ScheduledJob>();

        foreach (var job in _scheduledJobs)
        {
            if (job.ExecuteAt.ToUnixTimeMilliseconds() <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                scheduledJobsToExecute.Add(job);
            }
        }

        foreach (var job in scheduledJobsToExecute)
        {
            await _logger.Log($"Executing scheduled job queued for execution at {job.ExecuteAt:F}", level: LogLevel.Debug);

            // Do processing here I guess!
            switch (job.Action.Type)
            {
                case RemoveRole:
                    await Unicycle_RemoveRole(job, client.GetGuild(DiscordHelper.DefaultGuild()), client);
                    break;
                case AddRole:
                    await Unicycle_AddRole(job, client.GetGuild(DiscordHelper.DefaultGuild()), client);
                    break;
                case Unban:
                    await Unicycle_Unban(job, client.GetGuild(DiscordHelper.DefaultGuild()), client);
                    break;
                case Echo:
                    await Unicycle_Echo(job, client.GetGuild(DiscordHelper.DefaultGuild()), client);
                    break;
                case BannerRotation:
                    await Unicycle_BannerRotation(job, client.GetGuild(DiscordHelper.DefaultGuild()), client);
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
                    var nextExecuteAt = executeAt + repeatEvery;
            
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
            case "banner-rotation":
                // banner-rotation
                actionType = BannerRotation;
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
            case BannerRotation:
                output += "banner-rotation";
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
                $"Gave <@&{role.Id}> to <@{user.Id}> (`{user.Id}`).")
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
                $"Removed <@&{role.Id}> from <@{user.Id}> (`{user.Id}`)")
            .SetFileLogContent(
                $"Removed {role.Name} ({role.Id}) from {user.Username}#{user.Discriminator} ({user.Id}). {(reason != null ? $"Reason: {reason}." : "")}")
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
        if (job.Action.Fields["content"] == "") return;
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

    public async Task Unicycle_BannerRotation(ScheduledJob job, SocketGuild guild, DiscordSocketClient client)
    {
        if (_config.BannerMode == ConfigListener.BannerMode.None) return;
        if (_config.BannerMode == ConfigListener.BannerMode.CustomRotation && _config.BannerImages.Count == 0) return;

        if (_config.BannerMode == ConfigListener.BannerMode.CustomRotation)
        {
            try
            {
                // Rotate through banners.
                var rand = new Random();
                var number = rand.Next(_config.BannerImages.Count);
                var url = _config.BannerImages.ToList()[number];
                Stream stream;
                try
                {
                    stream = await url
                        .WithHeader("user-agent", $"Izzy-Moonbot (Linux x86_64) Flurl.Http/3.2.4 DotNET/6.0")
                        .GetStreamAsync();
                }
                catch (FlurlHttpException ex)
                {
                    await _logger.Log($"Recieved HTTP exception when executing Banner Rotation: {ex.Message}");
                    return;
                }

                var image = new Image(stream);

                await guild.ModifyAsync(properties => properties.Banner = image);

                await _modLogging.CreateModLog(guild)
                    .SetContent(
                        $"Changed banner to <{url}> for banner rotation.")
                    .SetFileLogContent(
                        $"Changed banner to {url} for banner rotation.")
                    .Send();
            }
            catch (FlurlHttpTimeoutException ex)
            {
                await _modLogging.CreateModLog(guild)
                    .SetContent(
                        $"Tried to change banner but the host server didn't respond fast enough, is it down? If so please run `.config BannerMode None` to avoid unnecessarily pinging Manebooru.")
                    .SetFileLogContent(
                        $"Tried to change banner but the host server didn't respond fast enough, is it down? If so please run `.config BannerMode None` to avoid unnecessarily pinging Manebooru.")
                    .Send();
                await _logger.Log(
                    $"Encountered HTTP timeout exception when trying to change banner: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
            catch (FlurlHttpException ex)
            {
                // Http request failure.
                await _modLogging.CreateModLog(guild)
                    .SetContent(
                        $"Tried to change banner and received a {ex.StatusCode} status code when attempting to ask the host server for the image. Doing nothing.")
                    .SetFileLogContent(
                        $"Tried to change banner and received a {ex.StatusCode} status code when attempting to ask the host server for the image. Doing nothing.")
                    .Send();
                await _logger.Log(
                    $"Encountered HTTP exception when trying to change banner: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
            catch (Exception ex)
            {
                await _modLogging.CreateModLog(guild)
                    .SetContent(
                        $"Tried to change banner and received a general error when attempting to ask the host server for the image. Doing nothing.")
                    .SetFileLogContent(
                        $"Tried to change banner and received a general error when attempting to ask the host server for the image. Doing nothing.")
                    .Send();
                await _logger.Log(
                    $"Encountered exception when trying to change banner: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
        }
        else if (_config.BannerMode == ConfigListener.BannerMode.ManebooruFeatured)
        {
            // Set to Manebooru featured.
            try
            {
                var image = await BooruHelper.GetFeaturedImage();

                if (_state.CurrentBooruFeaturedImage != null)
                {
                    if (image.Id == _state.CurrentBooruFeaturedImage.Id)
                    {
                        // Update the cache in case of change, but return
                        _state.CurrentBooruFeaturedImage =
                            image; // Cache to not anger CULTPONY or Twilight (API docs say to cache)
                        return;
                    }
                }

                _state.CurrentBooruFeaturedImage =
                    image; // Cache to not anger CULTPONY or Twilight (API docs say to cache)

                // Don't check the images if they're not ready yet!
                if (!image.ThumbnailsGenerated || image.Representations == null)
                {
                    await _modLogging.CreateModLog(guild)
                        .SetContent(
                            $"Tried to change banner to <https://manebooru.art/images/{image.Id}> but that image hasn't fully been generated yet. Doing nothing and trying again in {_config.BannerInterval} minutes.")
                        .SetFileLogContent(
                            $"Tried to change banner to https://manebooru.art/images/{image.Id} but that image hasn't fully been generated yet. Doing nothing and trying again in {_config.BannerInterval} minutes.")
                        .Send();
                    return;
                }

                if (image.Spoilered)
                {
                    // Image is blocked by current filter, complain.
                    await _modLogging.CreateModLog(guild)
                        .SetContent(
                            $"Tried to change banner to <https://manebooru.art/images/{image.Id}> but that image is blocked by my filter! Doing nothing.")
                        .SetFileLogContent(
                            $"Tried to change banner to https://manebooru.art/images/{image.Id} but that image is blocked by my filter! Doing nothing.")
                        .Send();
                    return;
                }

                var imageStream = await image.Representations.Full.GetStreamAsync();

                await guild.ModifyAsync(properties => properties.Banner = new Image(imageStream));
                
                await _modLogging.CreateModLog(guild)
                    .SetContent(
                        $"Changed banner to <https://manebooru.art/images/{image.Id}> for Manebooru featured image.")
                    .SetFileLogContent(
                        $"Changed banner to https://manebooru.art/images/{image.Id} for Manebooru featured image.")
                    .Send();
            }
            catch (FlurlHttpTimeoutException ex)
            {
                await _modLogging.CreateModLog(guild)
                    .SetContent(
                        $"Tried to change banner but Manebooru didn't respond fast enough, is it down? If so please run `.config BannerMode None` to avoid unnecessarily pinging Manebooru.")
                    .SetFileLogContent(
                        $"Tried to change banner but Manebooru didn't respond fast enough, is it down? If so please run `.config BannerMode None` to avoid unnecessarily pinging Manebooru.")
                    .Send();
                await _logger.Log(
                    $"Encountered HTTP timeout exception when trying to change banner: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
            catch (FlurlHttpException ex)
            {
                // Http request failure.
                await _modLogging.CreateModLog(guild)
                    .SetContent(
                        $"Tried to change banner and received a {ex.StatusCode} status code when attempting to ask Manebooru for the featured image. Doing nothing.")
                    .SetFileLogContent(
                        $"Tried to change banner and recieved a {ex.StatusCode} status code when attempting to ask Manebooru for the featured image. Doing nothing.")
                    .Send();
                await _logger.Log(
                    $"Encountered HTTP exception when trying to change banner: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
            catch (Exception ex)
            {
                await _modLogging.CreateModLog(guild)
                    .SetContent(
                        $"Tried to change banner and received a general error when attempting to ask Manebooru for the featured image. Doing nothing.")
                    .SetFileLogContent(
                        $"Tried to change banner and received a general error when attempting to ask Manebooru for the featured image. Doing nothing.")
                    .Send();
                await _logger.Log(
                    $"Encountered exception when trying to change banner: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
        }
    }
}
