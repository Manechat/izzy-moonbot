namespace Izzy_Moonbot.Describers;

public class ConfigItem
{
    public ConfigItem(ConfigItemType type, string description, ConfigItemCategory category,
        bool nullable = false)
    {
        Description = description;
        Type = type;
        Category = category;
        Nullable = nullable;
    }

    public ConfigItemType Type { get; }
    public string Description { get; }
    public ConfigItemCategory Category { get; }
    public bool Nullable { get; }
}

public enum ConfigItemCategory
{
    Core,
    Server,
    Moderation,
    Debug,
    User,
    Filter,
    Spam,
    Raid
}

public enum ConfigItemType
{
    // Values
    String,
    Char,
    Boolean,
    Integer,
    Double,
    Enum,
    User,
    Role,
    Channel,
    
    // Lists
    StringList,
    CharList,
    BooleanList,
    IntegerList,
    DoubleList,
    //EnumList, // Note: Implement when needed
    UserList,
    RoleList,
    ChannelList,
    
    // Dictionaries
    StringDictionary,
    //CharDictionary, // Note: Implement when needed
    BooleanDictionary,
    //IntegerDictionary, // Note: Implement when needed
    //DoubleDictionary, // Note: Implement when needed
    //EnumDictionary, // Note: Implement when needed
    //UserDictionary, // Note: Implement when needed
    //RoleDictionary, // Note: Implement when needed
    //ChannelDictionary, // Note: Implement when needed
    
    // Dictionaries of lists
    StringListDictionary
    //CharListDictionary, // Note: Implement when needed
    //BooleanListDictionary, // Note: Implement when needed
    //IntegerListDictionary, // Note: Implement when needed
    //DoubleListDictionary, // Note: Implement when needed
    //EnumListDictionary, // Note: Implement when needed
    //UserListDictionary, // Note: Implement when needed
    //RoleListDictionary, // Note: Implement when needed
    //ChannelListDictionary // Note: Implement when needed
}