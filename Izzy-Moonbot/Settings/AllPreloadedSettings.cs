namespace Izzy_Moonbot.Settings
{
    using System.Collections.Generic;

    public class AllPreloadedSettings
    {
        public AllPreloadedSettings()
        {
            Settings = new Dictionary<ulong, ServerPreloadedSettings>();
        }

        public Dictionary<ulong, ServerPreloadedSettings> Settings { get; set; }
    }
}
