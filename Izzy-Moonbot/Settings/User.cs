using Izzy_Moonbot.Helpers;
using System;
using System.Collections.Generic;

namespace Izzy_Moonbot.Settings
{
    public class User
    {
        public User()
        {
            Username = "";
            Aliases = new List<string>();
            Joins = new List<DateTime>();
            Pressure = 0;
            Timestamp = DateTime.UtcNow;
        }
        public string Username { get; set; }
        public List<string> Aliases { get; set; }
        public List<DateTime> Joins { get; set; }
        public double Pressure { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
