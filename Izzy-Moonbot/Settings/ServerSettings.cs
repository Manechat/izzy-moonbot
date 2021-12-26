namespace Izzy_Moonbot.Settings
{
    using System.Collections.Generic;

    public class ServerSettings
    {
        public ServerSettings()
        {
            Prefix = '.';
            ListenToBots = false;
            Aliases = new Dictionary<string, string>();
            AdminChannel = 0;
            AdminRole = 0;
            LogPostChannel = 0;
            IgnoredChannels = new List<ulong>();
            IgnoredRoles = new List<ulong>();
            AllowedUsers = new List<ulong>();
        }

        public char Prefix { get; set; }
        public bool ListenToBots { get; set; }
        public Dictionary<string, string> Aliases { get; set; }
        public ulong AdminChannel { get; set; }
        public ulong AdminRole { get; set; }
        public ulong LogPostChannel { get; set; }
        public List<ulong> IgnoredChannels { get; set; }
        public List<ulong> IgnoredRoles { get; set; }
        public List<ulong> AllowedUsers { get; set; }
    }
}
