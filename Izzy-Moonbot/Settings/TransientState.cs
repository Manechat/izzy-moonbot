using System;
using System.Collections.Generic;

namespace Izzy_Moonbot.Settings;

// Storage for Izzy's transient shared state.
// This is used for volatile data that needs to be used by multiple services and modules.
public class TransientState
{
    public int CurrentLargeJoinCount = 0;
    public int CurrentSmallJoinCount = 0;

    public DateTimeOffset LastWittyResponse = DateTimeOffset.MinValue;

    // AdminModule
    public DateTimeOffset LastMentionResponse = DateTimeOffset.MinValue;

    // RaidService
    public List<ulong> RecentJoins = new();

    public Dictionary<ulong, List<(DateTimeOffset, string)>> RecentMessages = new();
}
