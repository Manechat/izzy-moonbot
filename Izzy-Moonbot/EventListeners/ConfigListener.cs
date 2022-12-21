using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Izzy_Moonbot.Types;
using Microsoft.Extensions.Logging;

namespace Izzy_Moonbot.EventListeners;

public class ConfigListener
{
    private readonly Config _config;
    private readonly LoggingService _logger;

    private readonly ScheduleService _schedule;

    public ConfigListener(Config config, LoggingService logger, ScheduleService schedule)
    {
        _config = config;
        _logger = logger;
        _schedule = schedule;
    }

    public void RegisterEvents(DiscordSocketClient client)
    {
        _config.Changed += (thing, e) => Task.Run(async () => { await ConfigChangeEvent(e, client); });
    }

    public async Task ConfigChangeEvent(ConfigValueChangeEvent e, DiscordSocketClient client)
    {
        _logger.Log($"Config value change: {e.Name} from {e.Original} to {e.Current}", level: LogLevel.Debug);

        switch (e.Name)
        {
            case "BannerMode":
                await Handle_BannerMode(e, client);
                break;
            case "BannerInterval":
                await Handle_BannerInterval(e);
                break;
            default:
                throw new NotImplementedException("This config value doesn't have a method to fire on change.");
        }
    }

    private async Task Handle_BannerMode(ConfigValueChangeEvent e, DiscordSocketClient client)
    {
        /*
         * If BannerMode is `None`, Izzy deletes the internal repeating task.
         * Else, she'll create it if it doesn't exist, or leave it be.
         */
        var original = e.Original is BannerMode originalMode ? originalMode : BannerMode.None;
        var current = e.Current is BannerMode currentMode ? currentMode : BannerMode.None;

        if (original == BannerMode.None && current != BannerMode.None)
        {
            // Create repeating job.
            var currentTime = DateTimeOffset.UtcNow;
            var executeTime = currentTime.AddMinutes(_config.BannerInterval);
            
            _logger.Log($"Adding scheduled job to run the banner rotation job in {_config.BannerInterval} minutes", level: LogLevel.Debug);
            Dictionary<string, string> fields = new Dictionary<string, string>();
            var action = new ScheduledBannerRotationJob();
            var job = new ScheduledJob(currentTime, executeTime, action, ScheduledJobRepeatType.Relative);
            await _schedule.CreateScheduledJob(job);
            _logger.Log($"Added scheduled job.", level: LogLevel.Debug);
            await _schedule.Unicycle_BannerRotation(action, new SocketGuildAdapter(client.GetGuild(DiscordHelper.DefaultGuild())), new DiscordSocketClientAdapter(client));
        }
        else if (original != BannerMode.None && current == BannerMode.None)
        {
            // Delete repeated job.
            var scheduledJobs = _schedule.GetScheduledJobs(job => job.Action is ScheduledBannerRotationJob);

            _logger.Log($"Cancelling all scheduled jobs for banner rotation.", level: LogLevel.Debug);
            foreach (var scheduledJob in scheduledJobs)
            {
                await _schedule.DeleteScheduledJob(scheduledJob);
            }
        }
         // If we're managing the banner, make sure the banner is immediately updated to match the new mode
        if ((original == BannerMode.ManebooruFeatured && current == BannerMode.CustomRotation) ||
            (original == BannerMode.CustomRotation && current == BannerMode.ManebooruFeatured))
        {
            var scheduledJobs = _schedule.GetScheduledJobs(job => job.Action is ScheduledBannerRotationJob);

            foreach (var scheduledJob in scheduledJobs)
            {
                await _schedule.Unicycle_BannerRotation((ScheduledBannerRotationJob)scheduledJob.Action, new SocketGuildAdapter(client.GetGuild(DiscordHelper.DefaultGuild())), new DiscordSocketClientAdapter(client));
            }
        }
    }

    private async Task Handle_BannerInterval(ConfigValueChangeEvent e)
    {
        if (e.Original == e.Current) return;

        var original = e.Original is double originalDouble ? originalDouble : 0;
        var current = e.Current is double currentDouble ? currentDouble : 0;
        
        var scheduledJobs = _schedule.GetScheduledJobs(job => job.Action is ScheduledBannerRotationJob);

        _logger.Log($"Updating all scheduled jobs for banner rotation to occur {current} minutes after enabling rotation instead of after {original} minutes.", level: LogLevel.Debug);
        foreach (var scheduledJob in scheduledJobs)
        {
            scheduledJob.ExecuteAt = scheduledJob.CreatedAt.AddMinutes(current);
            await _schedule.ModifyScheduledJob(scheduledJob.Id, scheduledJob);
        }
    }

    public enum BannerMode
    {
        None,
        CustomRotation,
        ManebooruFeatured
    }
}