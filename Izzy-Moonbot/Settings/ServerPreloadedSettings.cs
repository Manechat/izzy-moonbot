namespace Izzy_Moonbot.Settings
{
    using System.Collections.Generic;

    public class ServerPreloadedSettings
    {
        public ServerPreloadedSettings()
        {
            name = "";
            prefix = '.';
            listenToBots = false;
            aliases = new Dictionary<string, string>();
        }

        public string name { get; set; }
        public char prefix { get; set; }
        public bool listenToBots { get; set; }
        public Dictionary<string, string> aliases { get; set; }
    }
}
