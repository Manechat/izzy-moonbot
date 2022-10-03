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
    Moderation,
    Debug,
    User,
    Filter,
    Spam,
    Raid
}

public enum ConfigItemType
{
    String,
    Char,
    Boolean,
    Integer,
    Double,
    User,
    Role,
    Channel,
    StringList,
    CharList,
    BooleanList,
    IntegerList,
    DoubleList,
    UserList,
    RoleList,
    ChannelList,
    StringDictionary,

    //CharDictionary, // Note: Implement when needed
    BooleanDictionary,

    //IntegerDictionary, // Note: Implement when needed
    //DoubleDictionary, // Note: Implement when needed
    //UserDictionary, // Note: Implement when needed
    //RoleDictionary, // Note: Implement when needed
    //ChannelDictionary, // Note: Implement when needed
    StringListDictionary
    //CharListDictionary, // Note: Implement when needed
    //BooleanListDictionary, // Note: Implement when needed
    //IntegerListDictionary, // Note: Implement when needed
    //DoubleListDictionary, // Note: Implement when needed
    //UserListDictionary, // Note: Implement when needed
    //RoleListDictionary, // Note: Implement when needed
    //ChannelListDictionary // Note: Implement when needed
}