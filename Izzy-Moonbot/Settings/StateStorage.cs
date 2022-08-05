using System;
using System.Collections.Generic;
using Izzy_Moonbot.Service;

namespace Izzy_Moonbot.Settings;

// Storage for Izzy's internal states.
// This is used for volatile data that needs to persist across Izzy's various services and modules.
public class StateStorage
{
    // RaidService
    public List<ulong> RecentJoins = new List<ulong>();
    public int CurrentSmallJoinCount = 0;
    public int CurrentLargeJoinCount = 0;
    public bool ManualRaidSilence = false;
    public RaidMode CurrentRaidMode = RaidMode.None;
    
    // AdminModule
    public DateTimeOffset LastMentionResponse = DateTimeOffset.MinValue;
}