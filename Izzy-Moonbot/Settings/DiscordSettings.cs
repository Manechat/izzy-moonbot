namespace Izzy_Moonbot.Settings
{
    using System.Collections.Generic;

    public class DiscordSettings
    {
        public string Token { get; set; }
        public HashSet<ulong> DevUsers { get; set; }
    }
}
