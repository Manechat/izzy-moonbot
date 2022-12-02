using System;
using System.Collections.Generic;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;

namespace Izzy_Moonbot.Settings;

// Storage for Izzy's internal states.
// This is used for volatile data that needs to persist across Izzy's various services and modules.
public class State
{
    public int CurrentLargeJoinCount = 0;
    public int CurrentSmallJoinCount = 0;

    // AdminModule
    public DateTimeOffset LastMentionResponse = DateTimeOffset.MinValue;

    // RaidService
    public List<ulong> RecentJoins = new();
}