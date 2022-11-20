using System.Collections.Generic;

namespace Izzy_Moonbot.Settings;

public class DiscordSettings
{
    public string Token { get; set; }
    public HashSet<ulong> DevUsers { get; set; }
    public ulong DefaultGuild { get; set; }
}