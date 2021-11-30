namespace Izzy_Moonbot.Settings
{
    using System;
    using System.Collections.Generic;

    public class ServerSettings
    {
        public ServerSettings()
        {
            adminChannel = 0;
            adminRole = 0;
            logPostChannel = 0;
            ignoredChannels = new List<ulong>();
            ignoredRoles = new List<ulong>();
            allowedUsers = new List<ulong>();
        }

        public ulong adminChannel { get; set; }
        public ulong adminRole { get; set; }
        public ulong logPostChannel { get; set; }
        public List<ulong> ignoredChannels { get; set; }
        public List<ulong> ignoredRoles { get; set; }
        public List<ulong> allowedUsers { get; set; }
    }
}
