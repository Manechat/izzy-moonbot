using System.Collections.Generic;

namespace Izzy_Moonbot.Settings;

// General misc storage
// This is used for storage which, while it needs to be persistent, is not worth having it's own config file.
// Examples include the list of users waiting in the Best Pony queue and the list of stowaway users (users silenced during a raid).
public class GeneralStorage
{
    public GeneralStorage()
    {
        Stowaways = new Dictionary<string, HashSet<ulong>>();
    }
    
    public Dictionary<string, HashSet<ulong>> Stowaways { get; set; }
}