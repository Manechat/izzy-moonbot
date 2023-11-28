using System;
using System.Collections.Generic;

namespace Izzy_Moonbot.Describers;

// Literally only exists to describe settings in ServerSettings
public class ConfigDescriber
{
    private readonly Dictionary<string, ConfigItem> _config = new(StringComparer.InvariantCultureIgnoreCase);

    public ConfigDescriber()
    {
        // Setup settings
        _config.Add("Prefix",
            new ConfigItem("Prefix", ConfigItemType.Char, "The prefix I will listen to for commands.",
                ConfigItemCategory.Setup));
        _config.Add("ModRole",
            new ConfigItem("ModRole", ConfigItemType.Role, "The role that I allow to execute sensitive commands.",
                ConfigItemCategory.Setup));
        _config.Add("ModChannel",
            new ConfigItem("ModChannel", ConfigItemType.Channel,
                "The channel where I'll post messages about possible raids, spam trips, filter violations, users joining or leaving, automated role changes, automated unbans, and so on.",
                ConfigItemCategory.Setup));
        _config.Add("LogChannel",
            new ConfigItem("LogChannel", ConfigItemType.Channel, "The channel where I will post verbose message edit/deletion logs, including bulk deletion logs created by spam trips or the `wipe` command.",
                ConfigItemCategory.Setup));

        // Misc settings
        _config.Add("UnicycleInterval",
            new ConfigItem("UnicycleInterval", ConfigItemType.Integer,
                "How often, in milliseconds, I'll check scheduled jobs for execution.",
                ConfigItemCategory.Misc));
        _config.Add("MentionResponseEnabled",
            new ConfigItem("MentionResponseEnabled", ConfigItemType.Boolean, "Whether I will respond to someone mentioning me.",
                ConfigItemCategory.Misc));
        _config.Add("MentionResponses",
            new ConfigItem("MentionResponses", ConfigItemType.StringSet,
                "A list of responses I will send whenever someone mentions me.", ConfigItemCategory.Misc));
        _config.Add("MentionResponseCooldown",
            new ConfigItem("MentionResponseCooldown", ConfigItemType.Double,
                "How many seconds I should wait between responding to a mention", ConfigItemCategory.Misc));
        _config.Add("DiscordActivityName",
            new ConfigItem("DiscordActivityName", ConfigItemType.String,
                "The content of my Discord status. Note: This takes time to set.",
                ConfigItemCategory.Misc, true));
        _config.Add("DiscordActivityWatching",
            new ConfigItem("DiscordActivityWatching", ConfigItemType.Boolean,
                "Whether my Discord status says 'Playing' (`false`) or 'Watching' (`true`). Note: This takes time to set.",
                ConfigItemCategory.Misc));
        _config.Add("Aliases",
            new ConfigItem("Aliases", ConfigItemType.StringDictionary,
                "Shorthand commands which can be used as an alternative to executing a different, often longer, command.",
                ConfigItemCategory.Misc));
        _config.Add("FirstRuleMessageId",
            new ConfigItem("FirstRuleMessageId", ConfigItemType.UnsignedInteger,
                "Id of the message in our rules channel that `.rule 1` should print.",
                ConfigItemCategory.Misc));
        _config.Add("HiddenRules",
            new ConfigItem("HiddenRules", ConfigItemType.StringDictionary,
                "Rules that we want `.rule` to display but aren't or can't be messages in the rules channel.",
                ConfigItemCategory.Misc));
        _config.Add("BestPonyChannel",
            new ConfigItem("BestPonyChannel", ConfigItemType.Channel, "The channel for Best Pony winners. If this is set, .rollforbestpony wins will send a message here in addition to ModChannel.",
                ConfigItemCategory.Misc));
        _config.Add("RecentMessagesPerUser",
            new ConfigItem("RecentMessagesPerUser", ConfigItemType.Integer, "How many of a user's messages I'll store even after they're too old to affect spam pressure. See .help recentmessages for details.",
                ConfigItemCategory.Misc));

        // Banner settings
        _config.Add("BannerMode",
            new ConfigItem("BannerMode", ConfigItemType.Enum,
                "If and how I will manage the server banner.\n" +
                "`None` means no action at all; I will never change the server banner unless someone runs a banner-related command.\n" +
                "`Rotate` means starting at the first URL in `BannerImages`, using each one in order, then going back to the first one after all have been used.\n" +
                "`Shuffle` means randomly selecting any of the URLs in `BannerImages`, no matter which of them have or haven't been used recently.\n" +
                "`ManebooruFeatured` means syncing the server banner to manebooru.art's featured image. This may fail since Manebooru and Discord don't support all of the same image files.\n" +
                "In most modes, I will post a message in `ModChannel` every time I change the banner. In `ManebooruFeatured`, I will post success messages in `LogChannel` instead, but errors will still go to `ModChannel`.",
                ConfigItemCategory.Banner));
        _config.Add("BannerInterval",
            new ConfigItem("BannerInterval", ConfigItemType.Double,
                "How often I'll change the banner in minutes. If `BannerMode` is `None`, this has no effect. " +
                "In `Rotate` and `Shuffle` modes, this is how often I'll select a new image from `BannerImages`. " +
                "In `ManebooruFeatured` mode, this is how often I'll poll Manebooru's featured image.",
                ConfigItemCategory.Banner));
        _config.Add("BannerImages",
            new ConfigItem("BannerImages", ConfigItemType.StringSet,
                "The list of banner image URLs I'll `Rotate` or `Shuffle` through in those `BannerMode`s.",
                ConfigItemCategory.Banner));

        // ManagedRoles settings
        _config.Add("ManageNewUserRoles",
            new ConfigItem("ManageNewUserRoles", ConfigItemType.Boolean,
                "Whether I'll give roles to users on join.",
                ConfigItemCategory.ManagedRoles));
        _config.Add("MemberRole",
            new ConfigItem("MemberRole", ConfigItemType.Role,
                "The role I apply to users when they join the server. This is also the role I remove when I silence a user. Set to nothing to disable.",
                ConfigItemCategory.ManagedRoles, true));
        _config.Add("NewMemberRole",
            new ConfigItem("NewMemberRole", ConfigItemType.Role,
                "The role used to limit user permissions when they join the server. This is removed after `NewMemberRoleDecay` minutes. Set to nothing to disable.",
                ConfigItemCategory.ManagedRoles, true));
        _config.Add("NewMemberRoleDecay",
            new ConfigItem("NewMemberRoleDecay", ConfigItemType.Double,
                "The number of minutes I'll wait before removing `NewMemberRole` from a user.",
                ConfigItemCategory.ManagedRoles));
        _config.Add("RolesToReapplyOnRejoin",
            new ConfigItem("RolesToReapplyOnRejoin", ConfigItemType.RoleSet,
                "The roles I'll reapply to a user when they join **if they had that role when they left**.",
                ConfigItemCategory.ManagedRoles));


        // Filter settings
        _config.Add("FilterEnabled",
            new ConfigItem("FilterEnabled", ConfigItemType.Boolean,
                "Whether I will filter messages for words in the `FilterWords` list.", ConfigItemCategory.Filter));
        _config.Add("FilterIgnoredChannels",
            new ConfigItem("FilterIgnoredChannels", ConfigItemType.ChannelSet, "The list of channels I will not filter messages in.",
                ConfigItemCategory.Filter));
        _config.Add("FilterBypassRoles",
            new ConfigItem("FilterBypassRoles", ConfigItemType.RoleSet,
                "The list of roles I will not take action against when I detect a slur. " +
                "Please note that I __will still check for filter violations for roles in this value__. I just won't try to delete the message or silence the user.",
                ConfigItemCategory.Filter));
        _config.Add("FilterDevBypass",
            new ConfigItem("FilterDevBypass", ConfigItemType.Boolean,
                "Whether I will not take action against my developers when I detect a slur. " +
                "Please note that I __will still check for filter violations for developers__. I just won't try to delete the message or silence the user.",
                ConfigItemCategory.Filter));
        _config.Add("FilterWords",
            new ConfigItem("FilterWords", ConfigItemType.StringSet,
                "The list of words I filter. If FilterEnabled is true, then anytime I see a message with one of these words, I'll delete the message, silence the user, and post a notification in ModChannel.\n" +
                "See FilterIgnoredChannels, FilterBypassRoles and FilterDevBypass for exceptions.\n" +
                "If you want a filter that doesn't silence the user, see Discord's AutoMod.",
                ConfigItemCategory.Filter));

        // Spam settings
        _config.Add("SpamEnabled",
            new ConfigItem("SpamEnabled", ConfigItemType.Boolean,
                "Whether I will process messages and apply pressure to users.", ConfigItemCategory.Spam));
        _config.Add("SpamBypassRoles",
            new ConfigItem("SpamBypassRoles", ConfigItemType.RoleSet,
                "The roles I will not silence if they exceed `SpamMaxPressure`. Please note that I __will still process pressure for roles in this value__, I just won't silence them and I'll just log that it occured.",
                ConfigItemCategory.Spam));
        _config.Add("SpamIgnoredChannels",
            new ConfigItem("SpamIgnoredChannels", ConfigItemType.ChannelSet,
                "The channels I will not process pressure for. Best used for channels where spam is allowed.",
                ConfigItemCategory.Spam));
        _config.Add("SpamDevBypass",
            new ConfigItem("SpamDevBypass", ConfigItemType.Boolean,
                "Whether I will not silence my developers if they exceed `SpamMaxPressure`. Please note that I __will still process pressure for developers__, I just won't silence them and I'll just log that it occured.",
                ConfigItemCategory.Spam));
        _config.Add("SpamBasePressure",
            new ConfigItem("SpamBasePressure", ConfigItemType.Double,
                "The base pressure given to a user when they send a message.", ConfigItemCategory.Spam));
        _config.Add("SpamImagePressure",
            new ConfigItem("SpamImagePressure", ConfigItemType.Double,
                "Additional pressure generated by each image, link, or attachment. This is only parsed if I detect that there's an attachment on the message.",
                ConfigItemCategory.Spam));
        _config.Add("SpamLengthPressure",
            new ConfigItem("SpamLengthPressure", ConfigItemType.Double,
                "Additional pressure generated by each individual character in the message.",
                ConfigItemCategory.Spam));
        _config.Add("SpamLinePressure",
            new ConfigItem("SpamLinePressure", ConfigItemType.Double,
                "Additional pressure generated by each newline in the message.", ConfigItemCategory.Spam));
        _config.Add("SpamPingPressure",
            new ConfigItem("SpamPingPressure", ConfigItemType.Double,
                "Additional pressure generated by each ping in a message (including repeats of the same ping).",
                ConfigItemCategory.Spam));
        _config.Add("SpamRepeatPressure",
            new ConfigItem("SpamRepeatPressure", ConfigItemType.Double,
                "Additional pressure generated by a non-empty message that is (case-insensitive) identical to the last message sent by that user.",
                ConfigItemCategory.Spam));
        _config.Add("SpamUnusualCharacterPressure",
            new ConfigItem("SpamUnusualCharacterPressure", ConfigItemType.Double,
                "Additional pressure generated by each unusual character in a message. A character is considered 'unusual' if it is not a newline (\\r or \\n) and not in one of the following Unicode character classes: " +
                "UppercaseLetter, LowercaseLetter, SpaceSeparator, DecimalDigitNumber, OpenPunctuation, ClosePunctuation, OtherPunctuation, FinalQuotePunctuation",
                ConfigItemCategory.Spam));
        _config.Add("SpamMaxPressure",
            new ConfigItem("SpamMaxPressure", ConfigItemType.Double, "How much pressure a user can have before I silence them.",
                ConfigItemCategory.Spam));
        _config.Add("SpamPressureDecay",
            new ConfigItem("SpamPressureDecay", ConfigItemType.Double,
                "How long in seconds it takes for a user to lose `SpamBasePressure` from their pressure total.",
                ConfigItemCategory.Spam));
        _config.Add("SpamMessageDeleteLookback",
            new ConfigItem("SpamMessageDeleteLookback", ConfigItemType.Double,
                "How far back, in seconds, should I look back for messages to delete when a user trips spam.",
                ConfigItemCategory.Spam, true));

        // Raid settings
        _config.Add("RaidProtectionEnabled",
            new ConfigItem("RaidProtectionEnabled", ConfigItemType.Boolean,
                "Whether or not I will protect this server against raids. Consider disabling this if you anticipate a large increase in member count over a few days (e.g. 3000 in 4 days)",
                ConfigItemCategory.Raid));
        _config.Add("AutoSilenceNewJoins",
            new ConfigItem("AutoSilenceNewJoins", ConfigItemType.Boolean,
                "Whether or not I should automatically silence new users joining the server. Usually toggled on and off by the `ass` and `assoff` commands during and after a raid. " +
                "To avoid Izzy being rate limited during a raid, this also causes ModChannel messages to be delayed and posted in batches.",
                ConfigItemCategory.Raid));
        _config.Add("SmallRaidSize",
            new ConfigItem("SmallRaidSize", ConfigItemType.Integer,
                "How many recent joins (see `RecentJoinDecay`) it takes for me to notify mods about a potential raid.",
                ConfigItemCategory.Raid));
        _config.Add("LargeRaidSize",
            new ConfigItem("LargeRaidSize", ConfigItemType.Integer,
                "How many recent joins (see `RecentJoinDecay`) it takes for me to automatically enable `AutoSilenceNewUsers`.",
                ConfigItemCategory.Raid));
        _config.Add("RecentJoinDecay",
            new ConfigItem("RecentJoinDecay", ConfigItemType.Double,
                "How long to wait, in seconds, before I no longer consider a new member a 'recent join'.",
                ConfigItemCategory.Raid));
        _config.Add("SmallRaidDecay",
            new ConfigItem("SmallRaidDecay", ConfigItemType.Double,
                "How many minutes do I wait before declaring a small raid over.",
                ConfigItemCategory.Raid, true));
        _config.Add("LargeRaidDecay",
            new ConfigItem("LargeRaidDecay", ConfigItemType.Double,
                "How many minutes do I wait before declaring a large raid over (and automatically disabling `AutoSilenceNewUsers`).",
                ConfigItemCategory.Raid, true));

        // Bored settings
        _config.Add("BoredChannel",
            new ConfigItem("BoredChannel", ConfigItemType.Channel,
                "The channel where I'll execute a randomly selected command from BoredCommands after BoredCooldown seconds of inactivity.\n" +
                "Set to nothing to disabled bored commands.", ConfigItemCategory.Bored));
        _config.Add("BoredCooldown",
            new ConfigItem("BoredCooldown", ConfigItemType.Double,
                "How many seconds I'll wait before executing a randomly selected command from BoredCommands in BoredChannel.", ConfigItemCategory.Bored));
        _config.Add("BoredCommands",
            new ConfigItem("BoredCommands", ConfigItemType.StringSet,
                "The commands I'll randomly execute in BoredChannel after BoredCooldown seconds of inactivity.\n" +
                "Since I'm a bot, each command must allow bots to run it, or nothing will actually happen.", ConfigItemCategory.Bored));

        // Witty settings
        _config.Add("Witties",
            new ConfigItem("Witties", ConfigItemType.StringDictionary,
                "A map from message patterns to automated Izzy responses. Also known as an 'autoresponder.'\n" +
                " For example, `.config Witties set \"izzy\" \"hi!\"` will make Izzy post \"hi!\" whenever any non-bot posts" +
                    " a message with \"izzy\" (in one of `WittyChannels`, at least `WittyCooldown` seconds after the last witty response).\n" +
                "\n" +
                "If the response contains `|`s, Izzy will treat it as multiple possible responses, and select one at random.\n" +
                "\n" +
                "There is no regex or wildcard syntax, but witty pattern matching is 'smart' or 'fuzzy' in the following ways:\n" +
                "- case-insensitive: `izzy` matches `Izzy` and `IZZY`\n" +
                "- some punctuation marks (,.'\"!) are optional: `hi, ponies!` matches `hi ponies`\n" +
                "- spaces are optional and allow extra spaces: `hi izzy` matches `hiizzy` and `hi    izzy`\n" +
                "- whole 'word' matches only: `test` matches `this is a test!` but does NOT match `testy` or `attest`",
                ConfigItemCategory.Witty));
        _config.Add("WittyChannels",
            new ConfigItem("WittyChannels", ConfigItemType.ChannelSet,
                "The channels where I will check for witty patterns and post witty responses.",
                ConfigItemCategory.Witty));
        _config.Add("WittyCooldown",
            new ConfigItem("WittyCooldown", ConfigItemType.Double,
                "After posting a witty response, I will not respond to any witty patterns for this many seconds.",
                ConfigItemCategory.Witty));
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
            case "setup":
                return ConfigItemCategory.Setup;
            case "misc":
                return ConfigItemCategory.Misc;
            case "banner":
                return ConfigItemCategory.Banner;
            case "managedroles":
                return ConfigItemCategory.ManagedRoles;
            case "filter":
                return ConfigItemCategory.Filter;
            case "spam":
                return ConfigItemCategory.Spam;
            case "raid":
                return ConfigItemCategory.Raid;
            case "bored":
                return ConfigItemCategory.Bored;
            case "witty":
                return ConfigItemCategory.Witty;
            default:
                return null;
        }
    }

    public string CategoryToString(ConfigItemCategory category)
    {
        switch (category)
        {
            case ConfigItemCategory.Setup:
                return "Setup";
            case ConfigItemCategory.Misc:
                return "Misc";
            case ConfigItemCategory.Banner:
                return "Banner";
            case ConfigItemCategory.ManagedRoles:
                return "ManagedRoles";
            case ConfigItemCategory.Filter:
                return "Filter";
            case ConfigItemCategory.Spam:
                return "Spam";
            case ConfigItemCategory.Raid:
                return "Raid";
            case ConfigItemCategory.Bored:
                return "Bored";
            case ConfigItemCategory.Witty:
                return "Witty";
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
        return
            type == ConfigItemType.String ||
            type == ConfigItemType.Char ||
            type == ConfigItemType.Boolean ||
            type == ConfigItemType.Integer ||
            type == ConfigItemType.UnsignedInteger ||
            type == ConfigItemType.Double ||
            type == ConfigItemType.Enum ||
            type == ConfigItemType.Role ||
            type == ConfigItemType.Channel;
    }

    public bool TypeIsSet(ConfigItemType type)
    {
        return
            type == ConfigItemType.StringSet ||
            type == ConfigItemType.RoleSet ||
            type == ConfigItemType.ChannelSet;
    }

    public bool TypeIsDictionaryValue(ConfigItemType type) => type == ConfigItemType.StringDictionary;

    public bool TypeIsDictionarySet(ConfigItemType type)
    {
        return type == ConfigItemType.StringSetDictionary;
    }
}
