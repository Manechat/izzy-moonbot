using System;
using System.Collections.Generic;
using Izzy_Moonbot.Helpers;

namespace Izzy_Moonbot.Settings;

public class RecentMessage
{
    public ulong MessageId;
    public ulong ChannelId;
    public DateTimeOffset Timestamp;
    public string Content;
    public int EmbedsCount;

    public RecentMessage(ulong messageId, ulong channelId, DateTimeOffset timestamp, string content, int embedsCount)
    {
        MessageId = messageId;
        ChannelId = channelId;
        Timestamp = timestamp;
        Content = content;
        EmbedsCount = embedsCount;
    }

    public string GetJumpUrl() => $"https://discord.com/channels/{DiscordHelper.DefaultGuild()}/{ChannelId}/{MessageId}";
}

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

    public Dictionary<ulong, List<RecentMessage>> RecentMessages = new();
}
