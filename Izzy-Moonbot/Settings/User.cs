using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Izzy_Moonbot.Settings;

public class User
{
    public User()
    {
        Username = "";
        Aliases = new List<string>();
        Joins = new List<DateTimeOffset>();
        Pressure = 0;
        Timestamp = DateTimeOffset.UtcNow;
        KnownAlts = new HashSet<ulong>();
        PreviousMessage = "";
        PreviousMessages = new List<PreviousMessageItem>();
        Silenced = false;
    }

    public string Username { get; set; }
    public List<string> Aliases { get; set; }
    public List<DateTimeOffset> Joins { get; set; }
    public double Pressure { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public HashSet<ulong> KnownAlts { get; set; }
    public string PreviousMessage { get; set; }
    public List<PreviousMessageItem> PreviousMessages { get; set; }
    public bool Silenced { get; set; }
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