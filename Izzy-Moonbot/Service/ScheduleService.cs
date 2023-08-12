using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Flurl.Http;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.EventListeners;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;
using static Izzy_Moonbot.Settings.ScheduledJobRepeatType;
using static Izzy_Moonbot.Adapters.IIzzyClient;

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

    public ScheduleService(Config config, ModService mod, ModLoggingService modLogging, LoggingService logger, List<ScheduledJob> scheduledJobs)
    {
        _config = config;
        _logger = logger;
        _mod = mod;
        _modLogging = modLogging;
        _scheduledJobs = scheduledJobs;
    }

    public void RegisterEvents(IIzzyClient client)
    {
        client.ButtonExecuted += async (component) => await DiscordHelper.LeakOrAwaitTask(ButtonEvent(component));
    }

    public void BeginUnicycleLoop(IIzzyClient client)
    {
        if (_alreadyInitiated) return;
        _alreadyInitiated = true;
        UnicycleLoop(client);
    }

    private void UnicycleLoop(IIzzyClient client)
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
                _logger.Log($"{exception.Message}\n{exception.StackTrace}", level: LogLevel.Error);
            }

            // Call self
            UnicycleLoop(client);
        });
    }

    public async Task Unicycle(IIzzyClient client)
    {
        if (client.GetGuild(DiscordHelper.DefaultGuild()) is not IIzzyGuild defaultGuild)
            throw new InvalidOperationException("Failed to get default guild");

        var scheduledJobsToExecute = new List<ScheduledJob>();

        foreach (var job in _scheduledJobs)
        {
            if (job.ExecuteAt.ToUnixTimeMilliseconds() <= DateTimeHelper.UtcNow.ToUnixTimeMilliseconds())
            {
                scheduledJobsToExecute.Add(job);
            }
        }

        foreach (var job in scheduledJobsToExecute)
        {
            _logger.Log($"Executing {job.Action.Type} job {job.Id} since it was scheduled to execute at {job.ExecuteAt:F}", level: LogLevel.Debug);

            try
            {
                // Do processing here I guess!
                switch (job.Action)
                {
                    case ScheduledRoleRemovalJob roleRemovalJob:
                        await Unicycle_RemoveRole(roleRemovalJob, defaultGuild);
                        break;
                    case ScheduledRoleAdditionJob roleAdditionJob:
                        await Unicycle_AddRole(roleAdditionJob, defaultGuild);
                        break;
                    case ScheduledUnbanJob unbanJob:
                        await Unicycle_Unban(unbanJob, defaultGuild, client);
                        break;
                    case ScheduledEchoJob echoJob:
                        await Unicycle_Echo(echoJob, defaultGuild, client, job.RepeatType, job.Id);
                        break;
                    case ScheduledBannerRotationJob bannerRotationJob:
                        await Unicycle_BannerRotation(bannerRotationJob, defaultGuild, client);
                        break;
                    case ScheduledBoredCommandsJob boredCommandsJob:
                        await Unicycle_BoredCommands(boredCommandsJob, defaultGuild, client);
                        break;
                    case ScheduledEndRaidJob endRaidJob:
                        await Unicycle_EndRaid(endRaidJob, defaultGuild, client);
                        break;
                    default:
                        throw new NotSupportedException($"{job.Action.GetType().Name} is currently not supported.");
                }
            }
            catch (Exception ex)
            {
                var msg = $":warning: Failed to execute scheduled job :warning:\n" +
                    $"\n" +
                    $"Job was: {job.ToDiscordString()}\n" +
                    $"\n" +
                    $"Error was: [{ex.GetType().Name}] {ex.Message}\n" +
                    $"(Check Izzy's logs for a full stack trace)";
                await _modLogging.CreateModLog(defaultGuild).SetContent(msg).SetFileLogContent(msg).Send();

                _logger.Log(
                    $"Scheduled job threw an exception when trying to execute!\n" +
                    $"Type: {ex.GetType().Name}\n" +
                    $"Message: {ex.Message}\n" +
                    $"Job: {job.ToFileString()}\n" +
                    $"Stack Trace: {ex.StackTrace}");
            }

            await DeleteOrRepeatScheduledJob(job);
        }
    }

    public ScheduledJob? GetScheduledJob(string id)
    {
        return _scheduledJobs.SingleOrDefault(job => job.Id == id);
    }

    public ScheduledJob? GetScheduledJob(Func<ScheduledJob, bool> predicate)
    {
        return _scheduledJobs.SingleOrDefault(predicate);
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
        if (job.RepeatType == ScheduledJobRepeatType.Relative && (job.LastExecutedAt ?? job.CreatedAt) >= job.ExecuteAt)
            throw new ArgumentException($"CreateScheduledJob() was passed a relative repeating job with non-positive interval: {job.ToDiscordString()}");

        _scheduledJobs.Add(job);
        await FileHelper.SaveScheduleAsync(_scheduledJobs);
    }

    public async Task ModifyScheduledJob(string id, ScheduledJob job)
    {
        if (job.RepeatType == ScheduledJobRepeatType.Relative && (job.LastExecutedAt ?? job.CreatedAt) >= job.ExecuteAt)
            throw new ArgumentException($"ModifyScheduledJob() was passed a relative repeating job with non-positive interval: {job.ToDiscordString()}");

        _scheduledJobs[_scheduledJobs.IndexOf(_scheduledJobs.First(altJob => altJob.Id == id))] = job;
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
    
    // Executors for different types.
    private async Task Unicycle_AddRole(ScheduledRoleAdditionJob job, IIzzyGuild guild)
    {
        var role = guild.GetRole(job.Role);
        if (role == null)
        {
            var msg = $"Unable to execute scheduled assignment of role {job.Role} to user {job.User} because this server has no role with that id.";
            await _modLogging.CreateModLog(guild).SetContent(msg).SetFileLogContent(msg).Send();
            return;
        }

        var user = guild.GetUser(job.User);
        if (user == null)
        {
            var msg = $"Unable to execute scheduled assignment of role {job.Role} to user {job.User} because this server has no user with that id (did they leave?).";
            await _modLogging.CreateModLog(guild).SetContent(msg).SetFileLogContent(msg).Send();
            return;
        }

        var reason = job.Reason;
        
        _logger.Log(
            $"Adding {role.Name} ({role.Id}) to {user.DisplayName} ({user.Username}/{user.Id})", level: LogLevel.Debug);
        
        await _mod.AddRole(user, role.Id, reason);
        await _modLogging.CreateModLog(guild)
            .SetContent(
                $"Gave <@&{role.Id}> to <@{user.Id}> (`{user.Id}`).")
            .SetFileLogContent(
                $"Gave {role.Name} ({role.Id}) to {user.DisplayName} ({user.Username}/{user.Id}). {(reason != null ? $"Reason: {reason}." : "")}")
            .Send();
    }
    
    private async Task Unicycle_RemoveRole(ScheduledRoleRemovalJob job, IIzzyGuild guild)
    {
        var role = guild.GetRole(job.Role);
        if (role == null)
        {
            var msg = $"Unable to execute scheduled removal of role {job.Role} from user {job.User} because this server has no role with that id.";
            await _modLogging.CreateModLog(guild).SetContent(msg).SetFileLogContent(msg).Send();
            return;
        }

        var user = guild.GetUser(job.User);
        if (user == null)
        {
            var msg = $"Unable to execute scheduled removal of role {job.Role} from user {job.User} because this server has no user with that id (did they leave?).";
            await _modLogging.CreateModLog(guild).SetContent(msg).SetFileLogContent(msg).Send();
            return;
        }

        string? reason = job.Reason;
        
        _logger.Log(
            $"Removing {role.Name} ({role.Id}) from {user.DisplayName} ({user.Username}/{user.Id})", level: LogLevel.Debug);
        
        await _mod.RemoveRole(user, role.Id, reason);
        await _modLogging.CreateModLog(guild)
            .SetContent(
                $"Removed <@&{role.Id}> from <@{user.Id}> (`{user.Id}`)")
            .SetFileLogContent(
                $"Removed {role.Name} ({role.Id}) from {user.DisplayName} ({user.Username}/{user.Id}). {(reason != null ? $"Reason: {reason}." : "")}")
            .Send();
    }

    private async Task Unicycle_Unban(ScheduledUnbanJob job, IIzzyGuild guild, IIzzyClient client)
    {
        if (!await guild.GetIsBannedAsync(job.User)) return;

        var user = await client.GetUserAsync(job.User);
        
        _logger.Log(
            $"Unbanning {(user == null ? job.User : $"")}.",
            level: LogLevel.Debug);

        await guild.RemoveBanAsync(job.User, job.Reason);

        var embed = new EmbedBuilder()
            .WithTitle(
                $"Unbanned {(user != null ? $"{DiscordHelper.DisplayName(user, guild)} ({user.Username}/{user.Id})" : "")}")
            .WithColor(16737792)
            .WithDescription($"Gasp! Does this mean I can invite <@{job.User}> to our next traditional unicorn sleepover?")
            .Build();
        
        await _modLogging.CreateModLog(guild)
            .SetEmbed(embed)
            .SetFileLogContent($"Unbanned {job.User}")
            .Send();
    }

    private async Task Unicycle_Echo(ScheduledEchoJob job, IIzzyGuild guild, IIzzyClient client, ScheduledJobRepeatType repeatType, string jobId)
    {
        if (job.Content == "") return;

        var channel = guild.GetTextChannel(job.ChannelOrUser);
        if (channel == null)
        {
            MessageComponent? components = null;
            if (repeatType != None)
                components = new ComponentBuilder().WithButton(
                    customId: $"cancel-echo-job:{jobId}",
                    label: "Unsubscribe",
                    style: ButtonStyle.Primary
                ).Build();

            await client.SendDirectMessageAsync(job.ChannelOrUser, job.Content, components: components);
            return;
        }

        await channel.SendMessageAsync(job.Content);
    }

    public async Task Unicycle_BannerRotation(ScheduledBannerRotationJob job, IIzzyGuild guild,
        IIzzyClient client)
    {
        if (_config.BannerMode == ConfigListener.BannerMode.None) {
            _logger.Log("Unicycle_BannerRotation early returning because BannerMode is None.");
            return;
        }

        if (_config.BannerMode == ConfigListener.BannerMode.Shuffle || _config.BannerMode == ConfigListener.BannerMode.Rotate)
        {
            if (_config.BannerImages.Count == 0)
            {
                var modeString = _config.BannerMode.GetType().GetEnumName(_config.BannerMode);
                var msg = $"Unable to change banner because BannerMode is {modeString} but BannerImages is empty";
                await _modLogging.CreateModLog(guild).SetContent(msg).SetFileLogContent(msg).Send();
                _logger.Log(msg);
                return;
            }

            try
            {
                int bannerIndex;
                if (_config.BannerMode == ConfigListener.BannerMode.Shuffle)
                {
                    var rand = new Random();
                    bannerIndex = rand.Next(_config.BannerImages.Count);
                }
                else // BannerMode.Rotate
                {
                    var lastIndex = job.LastBannerIndex ?? -1;
                    bannerIndex = (lastIndex + 1) % _config.BannerImages.Count;
                }
                job.LastBannerIndex = bannerIndex;

                var url = _config.BannerImages.ToList()[bannerIndex];
                try
                {
                    await DiscordHelper.SetBannerToUrlImage(url, guild);
                }
                catch (FlurlHttpException ex)
                {
                    _logger.Log($"Recieved HTTP exception when executing Banner Rotation: {ex.Message}");
                    return;
                }

                await _modLogging.CreateModLog(guild)
                    .SetContent(
                        $"Set banner to <{url}> for banner rotation.")
                    .SetFileLogContent(
                        $"Set banner to {url} for banner rotation.")
                    .Send();
            }
            catch (Exception ex)
            {
                var msg = $"Failed to change banner: [{ex.GetType().Name}] {ex.Message}\n{ex.StackTrace}";
                await _modLogging.CreateModLog(guild).SetContent(msg).SetFileLogContent(msg).Send();
                _logger.Log(msg);
            }
        }
        else if (_config.BannerMode == ConfigListener.BannerMode.ManebooruFeatured)
        {
            // Set to Manebooru featured.
            try
            {
                var image = await BooruHelper.GetFeaturedImage();

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

                var imageStream = await image.Representations.Thumbnail.GetStreamAsync();

                await guild.SetBanner(new Image(imageStream));

                var defaultGuild = client.GetGuild(DiscordHelper.DefaultGuild());
                if (_config.LogChannel != 0)
                {
                    var logChannel = defaultGuild?.GetTextChannel(_config.LogChannel);
                    if (logChannel != null)
                        await logChannel.SendMessageAsync($"Set banner to <https://manebooru.art/images/{image.Id}> for Manebooru featured image.", allowedMentions: AllowedMentions.None);
                    else
                        _logger.Log("Something went wrong trying to access LogChannel.");
                }
                else
                    _logger.Log("Can't post logs because .config LogChannel hasn't been set.");
            }
            catch (Exception ex)
            {
                var msg = $"Failed to change banner to the Manebooru featured image: [{ex.GetType().Name}] {ex.Message}\n{ex.StackTrace}";
                await _modLogging.CreateModLog(guild).SetContent(msg).SetFileLogContent(msg).Send();
                _logger.Log(msg);
            }
        }
    }

    private async Task ButtonEvent(IIzzySocketMessageComponent component)
    {
        // Be sure to early return without DeferAsync()ing if this is someone else's button
        var buttonId = component.Data.CustomId;
        var idParts = buttonId.Split(':');
        if (idParts.Length != 2 || idParts[0] != "cancel-echo-job") return;

        _logger.Log($"Received ButtonExecuted event with button id {buttonId}");
        var jobId = idParts[1];
        var job = GetScheduledJob(jobId);
        if (job is null)
        {
            _logger.Log($"Ignoring unsubscribe button click for job {jobId} because that job no longer exists");
            return;
        }

        _logger.Log($"Cancelling job {jobId} due to unsubscribe button click");
        await DeleteScheduledJob(job);

        await component.UpdateAsync(msg =>
        {
            msg.Components = new ComponentBuilder().WithButton(
                customId: "successfully-unsubscribed",
                label: "Successfully Unsubscribed",
                disabled: true,
                style: ButtonStyle.Success
            ).Build();
        });

        await component.DeferAsync();
    }

    public async Task Unicycle_BoredCommands(ScheduledBoredCommandsJob _job, IIzzyGuild guild, IIzzyClient _client)
    {
        var boredChannel = guild.GetTextChannel(_config.BoredChannel);
        if (boredChannel is null)
        {
            _logger.Log($"Could not get a text channel with id {_config.BoredChannel} from the default guild. " +
                "Will not reschedule bored task");
            return;
        }

        DateTimeOffset lastMessage = DateTimeOffset.UnixEpoch;
        await foreach (var messageBatch in boredChannel.GetMessagesAsync(1))
            foreach (var message in messageBatch)
                lastMessage = message.Timestamp;

        DateTimeOffset nextExecuteTime;
        if ((DateTimeHelper.UtcNow - lastMessage).TotalSeconds > _config.BoredCooldown)
        {
            var cmds = _config.BoredCommands.ToArray();
            var cmd = cmds[new Random().Next(cmds.Length)];

            nextExecuteTime = DateTimeHelper.UtcNow.AddSeconds(_config.BoredCooldown);

            _logger.Log($"last BoredChannel message was posted {lastMessage} which was over {_config.BoredCooldown} seconds ago.\n" +
                $"Posting randomly selected command message: {cmd}\n" +
                $"and scheduling next BoredCommands job for {nextExecuteTime}");
            await boredChannel.SendMessageAsync(cmd);
        }
        else
        {
            nextExecuteTime = lastMessage.AddSeconds(_config.BoredCooldown);
            _logger.Log($"BoredChannel has recent activity at {lastMessage}, not executing anything." +
                $"Scheduling next BoredCommands job for {nextExecuteTime}");
        }

        var nextJob = new ScheduledJob(DateTimeHelper.UtcNow, nextExecuteTime, new ScheduledBoredCommandsJob(), ScheduledJobRepeatType.None);
        await CreateScheduledJob(nextJob);
    }

    public async Task Unicycle_EndRaid(ScheduledEndRaidJob job, IIzzyGuild guild, IIzzyClient _client)
    {

    }
}
