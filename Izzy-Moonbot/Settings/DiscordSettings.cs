using System;
using System.Collections.Generic;

namespace Izzy_Moonbot.Settings;

public class DiscordSettings
{
    public string Token { get; set; } = "";
    public HashSet<ulong> DevUsers { get; set; } = new HashSet<ulong>();
    public ulong DefaultGuild { get; set; }
}