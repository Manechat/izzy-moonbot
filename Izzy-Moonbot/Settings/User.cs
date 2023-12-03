using System;
using System.Collections.Generic;

namespace Izzy_Moonbot.Settings;

public class User
{
    public User()
    {
        Username = "";
        Aliases = new List<string>();
        Joins = new List<DateTimeOffset>();
        Silenced = false;
        RolesToReapplyOnRejoin = new HashSet<ulong>();
        LastMessageTimeInMonitoredChannel = DateTimeOffset.MinValue;
    }

    public string Username { get; set; }
    public List<string> Aliases { get; set; }
    public List<DateTimeOffset> Joins { get; set; }
    public bool Silenced { get; set; }
    public HashSet<ulong> RolesToReapplyOnRejoin { get; set; }
    public DateTimeOffset LastMessageTimeInMonitoredChannel { get; set; }
}

public class PreviousMessageItem
{
    public PreviousMessageItem(ulong id, ulong channelId, ulong guildId, DateTimeOffset timestamp)
    {
        Id = id;
        ChannelId = channelId;
        GuildId = guildId;
        Timestamp = timestamp;
    }
    
    public ulong Id { get; set; }
    public ulong ChannelId { get; set; }
    public ulong GuildId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
