namespace Izzy_Moonbot.Describers;

public class ConfigItem
{
    public ConfigItem(string name, ConfigItemType type, string description, ConfigItemCategory category,
        bool nullable = false)
    {
        Name = name;
        Description = description;
        Type = type;
        Category = category;
        Nullable = nullable;
    }

    public string Name { get; }
    public ConfigItemType Type { get; }
    public string Description { get; }
    public ConfigItemCategory Category { get; }
    public bool Nullable { get; }
}

public enum ConfigItemCategory
{
    Setup,
    Misc,
    Banner,
    ManagedRoles,
    Filter,
    Spam,
    Raid,
    Bored,
    Witty,
    Monitoring
}

public enum ConfigItemType
{
    // Values
    String,
    Char,
    Boolean,
    Integer,
    UnsignedInteger,
    Double,
    Enum,
    Role,
    Channel,
    
    // Sets
    StringSet,
    RoleSet,
    ChannelSet,
    
    // Dictionaries
    StringDictionary,
    
    // Dictionaries of Sets
    StringSetDictionary
}
