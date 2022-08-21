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
        Pressure = 0;
        Timestamp = DateTimeOffset.UtcNow;
        KnownAlts = new HashSet<ulong>();
        PreviousMessage = "";
        Silenced = false;
    }

    public string Username { get; set; }
    public List<string> Aliases { get; set; }
    public List<DateTimeOffset> Joins { get; set; }
    public double Pressure { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public HashSet<ulong> KnownAlts { get; set; }
    public string PreviousMessage { get; set; }
    public bool Silenced { get; set; }
}