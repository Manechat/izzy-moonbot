namespace Izzy_Moonbot.Settings
{
    using System;
    using System.Collections.Generic;

    public class ServerSettingsOld
    {
        public ServerSettingsOld()
        {
            AdminChannel = 0;
            AdminRole = 0;
            LogPostChannel = 0;
            IgnoredChannels = new List<ulong>();
            IgnoredRoles = new List<ulong>();
            AllowedUsers = new List<ulong>();
        }

        public ulong AdminChannel { get; set; }
        public ulong AdminRole { get; set; }
        public ulong LogPostChannel { get; set; }
        public List<ulong> IgnoredChannels { get; set; }
        public List<ulong> IgnoredRoles { get; set; }
        public List<ulong> AllowedUsers { get; set; }
    }
}
