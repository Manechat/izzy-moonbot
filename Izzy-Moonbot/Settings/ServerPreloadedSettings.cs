namespace Izzy_Moonbot.Settings
{
    using System.Collections.Generic;

    public class ServerPreloadedSettings
    {
        public ServerPreloadedSettings()
        {
            Name = "";
            Prefix = '.';
            ListenToBots = false;
            Aliases = new Dictionary<string, string>();
        }

        public string Name { get; set; }
        public char Prefix { get; set; }
        public bool ListenToBots { get; set; }
        public Dictionary<string, string> Aliases { get; set; }
    }
}
