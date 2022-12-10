using System;
using System.Collections.Generic;

namespace Izzy_Moonbot.Describers;

// Literally only exists to describe settings in ServerSettings
public class ConfigDescriber
{
    private readonly Dictionary<string, ConfigItem> _config = new();

    public ConfigDescriber()
    {
        // Core settings
        _config.Add("Prefix",
            new ConfigItem(ConfigItemType.Char, "The prefix I will listen to for commands.",
                ConfigItemCategory.Core));
        _config.Add("UnicycleInterval",
            new ConfigItem(ConfigItemType.Integer,
                "How often, in milliseconds, I'll check scheduled jobs for execution.",
                ConfigItemCategory.Core));
        _config.Add("MentionResponseEnabled",
            new ConfigItem(ConfigItemType.Boolean, "Whether I will respond to someone mentioning me.",
                ConfigItemCategory.Core));
        _config.Add("MentionResponses",
            new ConfigItem(ConfigItemType.StringSet,
                "A list of responses I will send whenever someone mentions me.", ConfigItemCategory.Core));
        _config.Add("MentionResponseCooldown",
            new ConfigItem(ConfigItemType.Double,
                "How many seconds I should wait between responding to a mention", ConfigItemCategory.Core));
        _config.Add("DiscordActivityName",
            new ConfigItem(ConfigItemType.String,
                "The content of my Discord status. Note: This takes time to set.",
                ConfigItemCategory.Core, true));
        _config.Add("DiscordActivityWatching",
            new ConfigItem(ConfigItemType.Boolean,
                "Whether my Discord status says 'Playing' (`false`) or 'Watching' (`true`). Note: This takes time to set.",
                ConfigItemCategory.Core));
        _config.Add("Aliases",
            new ConfigItem(ConfigItemType.StringDictionary,
                "Shorthand commands which can be used as an alternative to executing a different, often longer, command.",
                ConfigItemCategory.Core));
        _config.Add("FirstRuleMessageId",
            new ConfigItem(ConfigItemType.UnsignedInteger,
                "Id of the message in our rules channel that `.rule 1` should print.",
                ConfigItemCategory.Core));
        _config.Add("HiddenRules",
            new ConfigItem(ConfigItemType.StringDictionary,
                "Rules that we want `.rule` to display on but aren't or can't be messages in the rules channel.",
                ConfigItemCategory.Core));

        // Server settings
        _config.Add("BannerMode",
            new ConfigItem(ConfigItemType.Enum,
                "The mode I will use when setting banners.",
                ConfigItemCategory.Server));
        _config.Add("BannerInterval",
            new ConfigItem(ConfigItemType.Double,
                "How often I'll change the banner in minutes. If `BannerMode` is `None`, this has no effect. In `CustomRotation` mode, this is how often I'll randomly select a new image from `BannerImages`. In `ManebooruFeatured` mode, this is how often I'll poll Manebooru's featured image.",
                ConfigItemCategory.Server));
        _config.Add("BannerImages",
            new ConfigItem(ConfigItemType.StringSet,
                "The list of banners I'll rotate through (if `BannerMode` is set to `CustomRotation`).",
                ConfigItemCategory.Server));

        // Mod settings
        _config.Add("ModRole",
            new ConfigItem(ConfigItemType.Role, "The role that I allow to execute sensitive commands.",
                ConfigItemCategory.Moderation));
        _config.Add("ModChannel",
            new ConfigItem(ConfigItemType.Channel,
                "The channel I will post raid notifications and template action logs in.",
                ConfigItemCategory.Moderation));
        _config.Add("LogChannel",
            new ConfigItem(ConfigItemType.Channel, "The channel where I will post verbose message edit/deletion logs.",
                ConfigItemCategory.Moderation));

        // User based settings
        _config.Add("ManageNewUserRoles",
            new ConfigItem(ConfigItemType.Boolean,
                "Whether I'll give roles to users on join.",
                ConfigItemCategory.User));
        _config.Add("MemberRole",
            new ConfigItem(ConfigItemType.Role,
                "The role I apply to users when they join the server. This is also the role I remove when I silence a user. Set to nothing to disable.",
                ConfigItemCategory.User, true));
        _config.Add("NewMemberRole",
            new ConfigItem(ConfigItemType.Role,
                "The role used to limit user permissions when they join the server. This is removed after `NewMemberRoleDecay` minutes. Set to nothing to disable.",
                ConfigItemCategory.User, true));
        _config.Add("NewMemberRoleDecay",
            new ConfigItem(ConfigItemType.Double,
                "The amount of minutes I'll wait before removing `NewMemberRole` from a user.",
                ConfigItemCategory.User));
        _config.Add("RolesToReapplyOnRejoin",
            new ConfigItem(ConfigItemType.RoleSet,
                "The roles I'll reapply to a user when they join **if they had that role when they left**.",
                ConfigItemCategory.User));


        // Filter settings
        _config.Add("FilterEnabled",
            new ConfigItem(ConfigItemType.Boolean,
                "Whether I will filter messages for words in the `FilteredWords` list.", ConfigItemCategory.Filter));
        _config.Add("FilterIgnoredChannels",
            new ConfigItem(ConfigItemType.ChannelSet, "The list of channels I will not filter messages in.",
                ConfigItemCategory.Filter));
        _config.Add("FilterBypassRoles",
            new ConfigItem(ConfigItemType.RoleSet,
                "The list of roles I will not take action against when I detect a slur. Please note that I __will still check for filter violations for roles in this value__. I just won't try to delete the message or silence the user.",
                ConfigItemCategory.Filter));
        _config.Add("FilterDevBypass",
            new ConfigItem(ConfigItemType.Boolean,
                "Whether I will not take action against my developers when I detect a slur. Please note that I __will still check for filter violations for developers__. I just won't try to delete the message or silence the user.",
                ConfigItemCategory.Filter));
        _config.Add("FilteredWords",
            new ConfigItem(ConfigItemType.StringSetDictionary,
                "The map of the list of words I will filter. Each key is a separate filter category.",
                ConfigItemCategory.Filter));
        _config.Add("FilterResponseMessages",
            new ConfigItem(ConfigItemType.StringDictionary,
                "The map of messages I will send on a filter violation depending on which filter category was violated.",
                ConfigItemCategory.Filter, true));
        _config.Add("FilterResponseSilence",
            new ConfigItem(ConfigItemType.StringSet,
                "The list of filter categories that will cause me to silence a user on a filter violation.",
                ConfigItemCategory.Filter));

        // Pressure settings
        _config.Add("SpamEnabled",
            new ConfigItem(ConfigItemType.Boolean,
                "Whether I will process messages and apply pressure to users.", ConfigItemCategory.Spam));
        _config.Add("SpamBypassRoles",
            new ConfigItem(ConfigItemType.RoleSet,
                "The roles I will not silence if they exceed `SpamMaxPressure`. Please note that I __will still process pressure for roles in this value__, I just won't silence them and I'll just log that it occured.",
                ConfigItemCategory.Spam));
        _config.Add("SpamIgnoredChannels",
            new ConfigItem(ConfigItemType.ChannelSet,
                "The channels I will not process pressure for. Best used for channels where spam is allowed.",
                ConfigItemCategory.Spam));
        _config.Add("SpamDevBypass",
            new ConfigItem(ConfigItemType.Boolean,
                "Whether I will not silence my developers if they exceed `SpamMaxPressure`. Please note that I __will still process pressure for developers__, I just won't silence them and I'll just log that it occured.",
                ConfigItemCategory.Spam));
        _config.Add("SpamBasePressure",
            new ConfigItem(ConfigItemType.Double,
                "The base pressure given to a user when they send a message.", ConfigItemCategory.Spam));
        _config.Add("SpamImagePressure",
            new ConfigItem(ConfigItemType.Double,
                "Additional pressure generated by each image, link, or attachment. This is only parsed if I detect that there's an attachment on the message.",
                ConfigItemCategory.Spam));
        _config.Add("SpamLengthPressure",
            new ConfigItem(ConfigItemType.Double,
                "Additional pressure generated by each individual character in the message.",
                ConfigItemCategory.Spam));
        _config.Add("SpamLinePressure",
            new ConfigItem(ConfigItemType.Double,
                "Additional pressure generated by each newline in the message.", ConfigItemCategory.Spam));
        _config.Add("SpamPingPressure",
            new ConfigItem(ConfigItemType.Double,
                "Additional pressure generated by each ping in a message (non-unique pings are counted).",
                ConfigItemCategory.Spam));
        _config.Add("SpamRepeatPressure",
            new ConfigItem(ConfigItemType.Double,
                "Additional pressure generated by a message that is identical to the previous message sent (ignores case).",
                ConfigItemCategory.Spam));
        _config.Add("SpamUnusualCharacterPressure",
            new ConfigItem(ConfigItemType.Double,
                "Additional pressure generated by each unusual character in a message. A character is considered 'unusual' if it is not a newline (\\r or \\n) and not in one of the following Unicode character classes: " +
                "UppercaseLetter, LowercaseLetter, SpaceSeparator, DecimalDigitNumber, OpenPunctuation, ClosePunctuation, OtherPunctuation, FinalQuotePunctuation",
                ConfigItemCategory.Spam));
        _config.Add("SpamMaxPressure",
            new ConfigItem(ConfigItemType.Double, "How much pressure a user can have before I silence them.",
                ConfigItemCategory.Spam));
        _config.Add("SpamPressureDecay",
            new ConfigItem(ConfigItemType.Double,
                "How long in seconds it takes for a user to lose `SpamBasePressure` from their pressure amount.",
                ConfigItemCategory.Spam));
        _config.Add("SpamMessageDeleteLookback",
            new ConfigItem(ConfigItemType.Double,
                "How far back, in seconds, should I look back for deleting a user's messages when they trip anti-spam.",
                ConfigItemCategory.Spam, true));

        // Raid settings
        _config.Add("RaidProtectionEnabled",
            new ConfigItem(ConfigItemType.Boolean,
                "Whether or not I will protect this server against raids. It's best to disable if you anticipate a large increase in member count over a few days (3000 in 4 days for example)",
                ConfigItemCategory.Raid));
        _config.Add("AutoSilenceNewJoins",
            new ConfigItem(ConfigItemType.Boolean,
                "Whether or not I should automatically silence new users joining the server. Usually toggled on and off by the `ass` and `assoff` commands respectivly.",
                ConfigItemCategory.Raid));
        _config.Add("SmallRaidSize",
            new ConfigItem(ConfigItemType.Integer,
                "How many users have to join in `SmallRaidTime` seconds in order for me to notify mods about a potential raid.",
                ConfigItemCategory.Raid));
        _config.Add("SmallRaidTime",
            new ConfigItem(ConfigItemType.Double,
                "In how many seconds do `SmallRaidSize` users need to join in order for me to notify mods about a potential raid.",
                ConfigItemCategory.Raid));
        _config.Add("LargeRaidSize",
            new ConfigItem(ConfigItemType.Integer,
                "How many users have to join in `LargeRaidTime` seconds in order for me to automatically enable raidsilence.",
                ConfigItemCategory.Raid));
        _config.Add("LargeRaidTime",
            new ConfigItem(ConfigItemType.Double,
                "In how many seconds do `LargeRaidSize` users need to join in order for me to automatically enable raidsilence.",
                ConfigItemCategory.Raid));
        _config.Add("RecentJoinDecay",
            new ConfigItem(ConfigItemType.Double,
                "How long to wait, in seconds, before I no longer considering a new member a 'recent join' (no longer considered part of any raid if one were to occur).",
                ConfigItemCategory.Raid));
        _config.Add("SmallRaidDecay",
            new ConfigItem(ConfigItemType.Double,
                "How many minutes does there have to be no raid for me to automatically downgrade a Small raid to No raid.",
                ConfigItemCategory.Raid, true));
        _config.Add("LargeRaidDecay",
            new ConfigItem(ConfigItemType.Double,
                "How many minutes does there have to be no raid for me to automatically downgrade a Large raid to a Small raid.",
                ConfigItemCategory.Raid, true));
    }

    public List<string> GetSettableConfigItems()
    {
        List<string> settableConfigItems = new();

        foreach (var key in _config.Keys) settableConfigItems.Add(key);

        return settableConfigItems;
    }

    public List<string> GetSettableConfigItemsByCategory(ConfigItemCategory category)
    {
        List<string> settableConfigItems = new();

        foreach (var key in _config.Keys)
            if (_config[key].Category == category)
                settableConfigItems.Add(key);

        return settableConfigItems;
    }

    public ConfigItem? GetItem(string key)
    {
        if (!_config.ContainsKey(key)) return null;

        return _config[key];
    }

    public ConfigItemCategory? StringToCategory(string category)
    {
        switch (category.ToLower())
        {
            case "core":
                return ConfigItemCategory.Core;
            case "server":
            case "guild":
                return ConfigItemCategory.Server;
            case "moderation":
            case "mod":
                return ConfigItemCategory.Moderation;
            case "debug":
            case "dev":
            case "development":
                return ConfigItemCategory.Debug;
            case "user":
                return ConfigItemCategory.User;
            case "filter":
            case "wordfilter":
            case "word-filter":
                return ConfigItemCategory.Filter;
            case "spam":
            case "antispam":
            case "anti-spam":
            case "pressure":
                return ConfigItemCategory.Spam;
            case "raid":
            case "antiraid":
            case "anti-raid":
                return ConfigItemCategory.Raid;
            default:
                return null;
        }
    }

    public string CategoryToString(ConfigItemCategory category)
    {
        switch (category)
        {
            case ConfigItemCategory.Core:
                return "Core";
            case ConfigItemCategory.Server:
                return "Server";
            case ConfigItemCategory.Moderation:
                return "Moderation";
            case ConfigItemCategory.Debug:
                return "Debug";
            case ConfigItemCategory.User:
                return "User";
            case ConfigItemCategory.Filter:
                return "Filter";
            case ConfigItemCategory.Spam:
                return "Spam";
            case ConfigItemCategory.Raid:
                return "Raid";
            default:
                return "<UNKNOWN>";
        }
    }

    public string TypeToString(ConfigItemType type)
    {
        switch (type)
        {
            case ConfigItemType.String:
                return "String";
            case ConfigItemType.Char:
                return "Character";
            case ConfigItemType.Boolean:
                return "Boolean";
            case ConfigItemType.Integer:
                return "Integer";
            case ConfigItemType.UnsignedInteger:
                return "Unsigned Integer";
            case ConfigItemType.Double:
                return "Double";
            case ConfigItemType.Enum:
                return "Enum";
            case ConfigItemType.Role:
                return "Role";
            case ConfigItemType.Channel:
                return "Channel";
            // Keep calling them "lists" in the UI, since "list means order matters, set means it doesn't"
            // is a programmer thing our users won't expect. It also goes better with the `list` action.
            case ConfigItemType.StringSet:
                return "List of Strings";
            case ConfigItemType.RoleSet:
                return "List of Roles";
            case ConfigItemType.ChannelSet:
                return "List of Channels";
            case ConfigItemType.StringDictionary:
                return "Map of String";
            case ConfigItemType.StringSetDictionary:
                return "Map of Lists of Strings";
            default:
                return "<UNKNOWN>";
        }
    }

    public bool TypeIsValue(ConfigItemType type)
    {
        if (type == ConfigItemType.String ||
            type == ConfigItemType.Char ||
            type == ConfigItemType.Boolean ||
            type == ConfigItemType.Integer ||
            type == ConfigItemType.UnsignedInteger ||
            type == ConfigItemType.Double ||
            type == ConfigItemType.Enum ||
            type == ConfigItemType.Role ||
            type == ConfigItemType.Channel) return true;
        return false;
    }

    public bool TypeIsSet(ConfigItemType type)
    {
        if (type == ConfigItemType.StringSet ||
            type == ConfigItemType.RoleSet ||
            type == ConfigItemType.ChannelSet) return true;
        return false;
    }

    public bool TypeIsDictionaryValue(ConfigItemType type) => type == ConfigItemType.StringDictionary;

    public bool TypeIsDictionarySet(ConfigItemType type)
    {
        if (type == ConfigItemType.StringSetDictionary) return true;
        return false;
    }
}