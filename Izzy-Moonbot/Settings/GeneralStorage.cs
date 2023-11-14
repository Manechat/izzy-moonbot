using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using System;
using System.Collections.Generic;

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
        SuspectedRaiders = new HashSet<ulong>();

        // Banner management
        CurrentBooruFeaturedImage = null;

        // Best Pony rolls
        LastRollTime = null;
        UsersWhoRolledToday = new HashSet<ulong>();
    }
    
    // Antiraid
    public RaidMode CurrentRaidMode { get; set; }
    public bool ManualRaidSilence { get; set; }
    public HashSet<ulong> SuspectedRaiders { get; set; }

    // Banner management
    public BooruImage? CurrentBooruFeaturedImage { get; set; }

    // Best Pony rolls
    public DateTime? LastRollTime { get; set; }
    public ISet<ulong> UsersWhoRolledToday { get; set; }
}
