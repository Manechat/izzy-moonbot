using System.Threading.Tasks;
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

    public void RegisterEvents()
    {
        _config.Changed += (thing, e) => Task.Run(async () => { await ConfigChangeEvent(thing, e); });;
    }

    public async Task ConfigChangeEvent(object thing, ConfigValueChangeEvent e)
    {
        await _logger.Log($"Config value change: {e.Name} from {e.Original} to {e.Current}", level: LogLevel.Debug);
    }
}