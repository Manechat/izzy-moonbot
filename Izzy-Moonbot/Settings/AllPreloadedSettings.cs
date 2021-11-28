namespace Izzy_Moonbot.Settings
{
    using System.Collections.Generic;

    public class AllPreloadedSettings
    {
        public AllPreloadedSettings()
        {
            settings = new Dictionary<ulong, ServerPreloadedSettings>();
            guildList = new Dictionary<ulong, ulong>();
        }

        public Dictionary<ulong, ServerPreloadedSettings> settings { get; set; }
        public Dictionary<ulong, ulong> guildList { get; set; }
    }
}
