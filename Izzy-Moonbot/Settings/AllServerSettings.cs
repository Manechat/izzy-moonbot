namespace Izzy_Moonbot.Settings
{
    using System.Collections.Generic;

    public class AllServerSettings
    {
        public AllServerSettings()
        {
            Settings = new ServerSettings();
        }

        public ServerSettings Settings { get; set; }
    }
}
