using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;

namespace Izzy_Moonbot.Settings;

// General misc storage
// This is used for storage which, while it needs to be persistent, is not worth having it's own config file.
// Examples include the list of users waiting in the Best Pony queue.
public class GeneralStorage
{
    public GeneralStorage()
    {
        // Antiraid
        CurrentRaidMode = RaidMode.None;
        ManualRaidSilence = false;
        
        // Banner management
        CurrentBooruFeaturedImage = null;
    }
    
    // Antiraid
    public RaidMode CurrentRaidMode { get; set; }
    public bool ManualRaidSilence { get; set; }
    
    // Banner management
    public BooruImage? CurrentBooruFeaturedImage { get; set; }
}