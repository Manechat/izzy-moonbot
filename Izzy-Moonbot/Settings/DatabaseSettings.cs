using System.Collections.Generic;

namespace Izzy_Moonbot.Settings;

public class DatabaseSettings
{
    public string Protocol { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
    public string Host { get; set; }
    public string Database { get; set; }
    public Dictionary<string, string> Options { get; set; }
}