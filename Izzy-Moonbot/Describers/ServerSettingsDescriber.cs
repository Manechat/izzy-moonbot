using System;
using System.Collections.Generic;
using System.Reflection;

namespace Izzy_Moonbot.Describers
{
    // Literally only exists to describe settings in ServerSettings
    public class ServerSettingsDescriber
    {
        private readonly Dictionary<string, ServerSettingsItem> _settings = new Dictionary<string, ServerSettingsItem>();

        public ServerSettingsDescriber()
        {
            // Core settings
            _settings.Add("Prefix", new ServerSettingsItem(SettingsItemType.Char, "The prefix I will listen to for commands.", SettingsItemCategory.Core));
            _settings.Add("SafeMode", new ServerSettingsItem(SettingsItemType.Boolean, "If set to true, I will not preform any moderation actions. This is best used when testing moderation functions in case of potentially broken code.", SettingsItemCategory.Core));
            _settings.Add("BatchSendLogs", new ServerSettingsItem(SettingsItemType.Boolean, "If set to true, I will batch send mod/action logs instead of sending them immediately. This is managed automatically by the Raid service to prevent me from being ratelimited.", SettingsItemCategory.Core));
            _settings.Add("BatchLogsSendRate", new ServerSettingsItem(SettingsItemType.Double, "The amount of seconds between each batch send.", SettingsItemCategory.Core));
            _settings.Add("IgnoredChannels", new ServerSettingsItem(SettingsItemType.ChannelList, "A list of channels I will ignore commands from.", SettingsItemCategory.Core));
            _settings.Add("IgnoredRoles", new ServerSettingsItem(SettingsItemType.RoleList, "A list of roles I will ignore commands from.", SettingsItemCategory.Core));
            _settings.Add("MentionResponseEnabled", new ServerSettingsItem(SettingsItemType.Boolean, "Whether I will respond to someone mentioning me.", SettingsItemCategory.Core));
            _settings.Add("MentionResponses", new ServerSettingsItem(SettingsItemType.StringList, "A list of responses I will send whenever someone mentions me.", SettingsItemCategory.Core));
            _settings.Add("MentionResponseCooldown", new ServerSettingsItem(SettingsItemType.Double, "How many seconds I should wait between responding to a mention", SettingsItemCategory.Core));

            // Mod settings
            _settings.Add("ModRole", new ServerSettingsItem(SettingsItemType.Role, "The role that I allow to execute sensitive commands.", SettingsItemCategory.Moderation));
            _settings.Add("ModChannel", new ServerSettingsItem(SettingsItemType.Channel, "The channel I will post raid notifications and template action logs in.", SettingsItemCategory.Moderation));
            _settings.Add("LogChannel", new ServerSettingsItem(SettingsItemType.Channel, "The channel I will post my own action logs in.", SettingsItemCategory.Moderation));

            // User based settings
            _settings.Add("MemberRole", new ServerSettingsItem(SettingsItemType.Role, "The role I apply to users when they join the server. This is also the role I remove when I silence a user. Set to nothing to disable.", SettingsItemCategory.User, true));
            _settings.Add("NewMemberRole", new ServerSettingsItem(SettingsItemType.Role, "The role used to limit user permissions when they join the server. This is removed after `NewMemberRoleDecay` minutes. Set to nothing to disable.", SettingsItemCategory.User, true));
            _settings.Add("NewMemberRoleDecay", new ServerSettingsItem(SettingsItemType.Double, "The amount of minutes I'll wait before removing `NewMemberRole` from a user.", SettingsItemCategory.User));

            
            // Filter settings
            _settings.Add("FilterEnabled", new ServerSettingsItem(SettingsItemType.Boolean, "Whether I will filter messages for words in the `FilteredWords` list.", SettingsItemCategory.Filter));
            _settings.Add("FilterMonitorEdits", new ServerSettingsItem(SettingsItemType.Boolean, "Whether I will refilter edited messages for words in the `FilteredWords` list.", SettingsItemCategory.Filter));
            _settings.Add("FilterIgnoredChannels", new ServerSettingsItem(SettingsItemType.ChannelList, "The list of channels I will not filter messages in.", SettingsItemCategory.Filter));
            _settings.Add("FilterBypassRoles", new ServerSettingsItem(SettingsItemType.RoleList, "The list of roles I will not take action against when I detect a slur. Please note that I __will still check for filter violations for roles in this value__. I just won't try to delete the message or silence the user..", SettingsItemCategory.Filter));
            _settings.Add("FilteredWords", new ServerSettingsItem(SettingsItemType.StringListDictionary, "The map of the list of words I will filter. Each key is a separate filter category.", SettingsItemCategory.Filter));
            _settings.Add("FilterResponseDelete", new ServerSettingsItem(SettingsItemType.BooleanDictionary, "The map containing if I'll delete a message on filter violation depending on which filter category was violated.", SettingsItemCategory.Filter));
            _settings.Add("FilterResponseMessages", new ServerSettingsItem(SettingsItemType.StringDictionary, "The map of messages I will send on a filter violation depending on which filter category was violated.", SettingsItemCategory.Filter, true));
            _settings.Add("FilterResponseSilence", new ServerSettingsItem(SettingsItemType.BooleanDictionary, "The map containing if I'll silence a user on a filter violation depending on which filter category was violated.", SettingsItemCategory.Filter));
            
            // Pressure settings
            _settings.Add("SpamEnabled", new ServerSettingsItem(SettingsItemType.Boolean, "Whether I will process messages and apply pressure to users.", SettingsItemCategory.Spam));
            _settings.Add("SpamMonitorEdits", new ServerSettingsItem(SettingsItemType.Boolean, "Whether I will reprocess edited messages for pressure. This only happens if the message is `SpamEditReprocessThreshold` more characters than the original message and I still know what the previous message was.", SettingsItemCategory.Spam));
            _settings.Add("SpamEditReprocessThreshold", new ServerSettingsItem(SettingsItemType.Integer, "The amount of differences in characters needed on a message edit to reprocess the edited message for pressure, if `SpamMonitorEdits` is set to `true`.", SettingsItemCategory.Spam));
            _settings.Add("SpamBypassRoles", new ServerSettingsItem(SettingsItemType.RoleList, "The roles I will not silence if they exceed `SpamMaxPressure`. Please note that I __will still process pressure for roles in this value__, I just won't silence them and I'll just log that it occured.", SettingsItemCategory.Spam));
            _settings.Add("SpamIgnoredChannels", new ServerSettingsItem(SettingsItemType.ChannelList, "The channels I will not process pressure for. Best used for channels where spam is allowed.", SettingsItemCategory.Spam));
            _settings.Add("SpamBasePressure", new ServerSettingsItem(SettingsItemType.Double, "The base pressure given to a user when they send a message.", SettingsItemCategory.Spam));
            _settings.Add("SpamImagePressure", new ServerSettingsItem(SettingsItemType.Double, "Additional pressure generated by each image, link, or attachment. This is only parsed if I detect that there's an attachment on the message.", SettingsItemCategory.Spam));
            _settings.Add("SpamLengthPressure", new ServerSettingsItem(SettingsItemType.Double, "Additional pressure generated by each individual character in the message.", SettingsItemCategory.Spam));
            _settings.Add("SpamLinePressure", new ServerSettingsItem(SettingsItemType.Double, "Additional pressure generated by each newline in the message.", SettingsItemCategory.Spam));
            _settings.Add("SpamPingPressure", new ServerSettingsItem(SettingsItemType.Double, "Additional pressure generated by each ping in a message (non-unique pings are counted).", SettingsItemCategory.Spam));
            _settings.Add("SpamRepeatPressure", new ServerSettingsItem(SettingsItemType.Double, "Additional pressure generated by a message that is identical to the previous message sent (ignores case).", SettingsItemCategory.Spam));
            _settings.Add("SpamMaxPressure", new ServerSettingsItem(SettingsItemType.Double, "How much pressure a user can have before I silence them.", SettingsItemCategory.Spam));
            _settings.Add("SpamPressureDecay", new ServerSettingsItem(SettingsItemType.Double, "How long in seconds it takes for a user to lose `SpamBasePressure` from their pressure amount.", SettingsItemCategory.Spam));
            _settings.Add("SilenceTimeout", new ServerSettingsItem(SettingsItemType.Double, "How long it'll take, in seconds, for unsilence those I silence automatically as part of the spam service or the antiraid service.", SettingsItemCategory.Spam, true));
            
            // Raid settings
            _settings.Add("RaidProtectionEnabled", new ServerSettingsItem(SettingsItemType.Boolean, "Whether or not I will protect this server against raids. It's best to disable if you anticipate a large increase in member count over a few days (3000 in 4 days for example)", SettingsItemCategory.Raid));
            _settings.Add("NormalVerificationLevel", new ServerSettingsItem(SettingsItemType.Integer, $"The verification level I will set when a raid ends. Values not specified below will result in this feature being disabled.{Environment.NewLine}0 - Unrestricted{Environment.NewLine}1 - Verified email{Environment.NewLine}2 - Account age older than 5 minutes{Environment.NewLine}3 - Member of server for longer than 10 minutes.{Environment.NewLine}4 - Must have verified phone.", SettingsItemCategory.Raid, true));
            _settings.Add("RaidVerificationLevel", new ServerSettingsItem(SettingsItemType.Integer, $"The verification level I will set when I detect a raid. Values not specified below will result in this feature being disabled.{Environment.NewLine}0 - Unrestricted{Environment.NewLine}1 - Verified email{Environment.NewLine}2 - Account age older than 5 minutes{Environment.NewLine}3 - Member of server for longer than 10 minutes.{Environment.NewLine}4 - Must have verified phone.", SettingsItemCategory.Raid, true));
            _settings.Add("AutoSilenceNewJoins", new ServerSettingsItem(SettingsItemType.Boolean, "Whether or not I should automatically silence new users joining the server. Usually toggled on and off by the `ass` and `assoff` commands respectivly.", SettingsItemCategory.Raid));
            _settings.Add("SmallRaidSize", new ServerSettingsItem(SettingsItemType.Integer, "How many users have to join in `SmallRaidTime` seconds in order for me to notify mods about a potential raid.", SettingsItemCategory.Raid));
            _settings.Add("SmallRaidTime", new ServerSettingsItem(SettingsItemType.Double, "In how many seconds do `SmallRaidSize` users need to join in order for me to notify mods about a potential raid.", SettingsItemCategory.Raid));
            _settings.Add("LargeRaidSize", new ServerSettingsItem(SettingsItemType.Integer, "How many users have to join in `LargeRaidTime` seconds in order for me to automatically enable raidsilence.", SettingsItemCategory.Raid));
            _settings.Add("LargeRaidTime", new ServerSettingsItem(SettingsItemType.Double, "In how many seconds do `LargeRaidSize` users need to join in order for me to automatically enable raidsilence.", SettingsItemCategory.Raid));
            _settings.Add("RecentJoinDecay", new ServerSettingsItem(SettingsItemType.Double, "How long to wait, in seconds, before I no longer considering a new member a 'recent join' (no longer considered part of any raid if one were to occur).", SettingsItemCategory.Raid));
            _settings.Add("SmallRaidDecay", new ServerSettingsItem(SettingsItemType.Double, "How many minutes does there have to be no raid for me to automatically downgrade a Small raid to No raid.", SettingsItemCategory.Raid, true));
            _settings.Add("LargeRaidDecay", new ServerSettingsItem(SettingsItemType.Double, "How many minutes does there have to be no raid for me to automatically downgrade a Large raid to a Small raid.", SettingsItemCategory.Raid, true));
        }

        public List<string> GetSettableConfigItems()
        {
            List<string> settableConfigItems = new();

            foreach (string key in _settings.Keys)
            {
                settableConfigItems.Add(key);
            }

            return settableConfigItems;
        }

        public List<string> GetSettableConfigItemsByCategory(SettingsItemCategory category)
        {
            List<string> settableConfigItems = new();

            foreach (string key in _settings.Keys)
            {
                if (_settings[key].Category == category)
                {
                    settableConfigItems.Add(key);
                }
            }

            return settableConfigItems;
        }

#nullable enable
        public ServerSettingsItem? GetItem(string key)
        {
            if (!_settings.ContainsKey(key)) return null;

            return _settings[key];
        }

        public SettingsItemCategory? StringToCategory(string category)
        {
            switch (category.ToLower())
            {
                case "core":
                    return SettingsItemCategory.Core;
                case "moderation":
                case "mod":
                    return SettingsItemCategory.Moderation;
                case "debug":
                case "dev":
                case "development":
                    return SettingsItemCategory.Debug;
                case "user":
                    return SettingsItemCategory.User;
                case "filter":
                case "wordfilter":
                case "word-filter":
                    return SettingsItemCategory.Filter;
                case "spam":
                case "antispam":
                case "anti-spam":
                case "pressure":
                    return SettingsItemCategory.Spam;
                case "raid":
                case "antiraid":
                case "anti-raid":
                    return SettingsItemCategory.Raid;
                default:
                    return null;
            }
        }

        public string CategoryToString(SettingsItemCategory category)
        {
            switch (category)
            {
                case SettingsItemCategory.Core:
                    return "Core";
                case SettingsItemCategory.Moderation:
                    return "Moderation";
                case SettingsItemCategory.Debug:
                    return "Debug";
                case SettingsItemCategory.User:
                    return "User";
                case SettingsItemCategory.Filter:
                    return "Filter";
                case SettingsItemCategory.Spam:
                    return "Spam";
                case SettingsItemCategory.Raid:
                    return "Raid";
                default:
                    return "<UNKNOWN>";
            }
        }

        public string TypeToString(SettingsItemType type)
        {
            switch (type)
            {
                case SettingsItemType.String:
                    return "String";
                case SettingsItemType.Char:
                    return "Character";
                case SettingsItemType.Boolean:
                    return "Boolean";
                case SettingsItemType.Integer:
                    return "Integer";
                case SettingsItemType.Double:
                    return "Double";
                case SettingsItemType.User:
                    return "User";
                case SettingsItemType.Role:
                    return "Role";
                case SettingsItemType.Channel:
                    return "Channel";
                case SettingsItemType.StringList:
                    return "List of Strings";
                case SettingsItemType.CharList:
                    return "List of Characters";
                case SettingsItemType.BooleanList:
                    return "List of Booleans";
                case SettingsItemType.IntegerList:
                    return "List of Integers";
                case SettingsItemType.DoubleList:
                    return "List of Doubles";
                case SettingsItemType.UserList:
                    return "List of Users";
                case SettingsItemType.RoleList:
                    return "List of Roles";
                case SettingsItemType.ChannelList:
                    return "List of Channels";
                case SettingsItemType.StringDictionary:
                    return "Map of String";
                //case SettingsItemType.CharDictionary: // Note: Implement when needed
                //    return "Map of Character";
                case SettingsItemType.BooleanDictionary:
                    return "Map of Boolean";
                //case SettingsItemType.IntegerDictionary: // Note: Implement when needed
                //    return "Map of Integer";
                //case SettingsItemType.DoubleDictionary: // Note: Implement when needed
                //    return "Map of Double";
                //case SettingsItemType.UserDictionary: // Note: Implement when needed
                //    return "Map of User";
                //case SettingsItemType.RoleDictionary: // Note: Implement when needed
                //    return "Map of Role";
                //case SettingsItemType.ChannelDictionary: // Note: Implement when needed
                //    return "Map of Channel";
                case SettingsItemType.StringListDictionary:
                    return "Map of Lists of Strings";
                //case SettingsItemType.CharListDictionary: // Note: Implement when needed
                //    return "Map of Lists of Characters";
                //case SettingsItemType.BooleanListDictionary: // Note: Implement when needed
                //    return "Map of Lists of Booleans";
                //case SettingsItemType.IntegerListDictionary: // Note: Implement when needed
                //    return "Map of Lists of Integers";
                //case SettingsItemType.DoubleListDictionary: // Note: Implement when needed
                //    return "Map of Lists of Doubles";
                //case SettingsItemType.UserListDictionary: // Note: Implement when needed
                //    return "Map of Lists of Users";
                //case SettingsItemType.RoleListDictionary: // Note: Implement when needed
                //    return "Map of Lists of Roles";
                //case SettingsItemType.ChannelListDictionary: // Note: Implement when needed
                //    return "Map of Lists of Channels";
                default:
                    return "<UNKNOWN>";
            }
        }

        public bool TypeIsValue(SettingsItemType type)
        {
            if (type == SettingsItemType.String ||
                type == SettingsItemType.Char ||
                type == SettingsItemType.Boolean ||
                type == SettingsItemType.Integer ||
                type == SettingsItemType.Double ||
                type == SettingsItemType.User ||
                type == SettingsItemType.Role ||
                type == SettingsItemType.Channel) return true;
            return false;
        }
        
        public bool TypeIsList(SettingsItemType type)
        {
            if (type == SettingsItemType.StringList ||
                type == SettingsItemType.CharList ||
                type == SettingsItemType.BooleanList ||
                type == SettingsItemType.IntegerList ||
                type == SettingsItemType.DoubleList ||
                type == SettingsItemType.UserList ||
                type == SettingsItemType.RoleList ||
                type == SettingsItemType.ChannelList) return true;
            return false;
        }
        
        public bool TypeIsDictionaryValue(SettingsItemType type)
        {
            if (type == SettingsItemType.StringDictionary || 
                //type == SettingsItemType.CharDictionary || // Note: Implement when needed
                type == SettingsItemType.BooleanDictionary /*|| 
                type == SettingsItemType.IntegerDictionary || // Note: Implement when needed
                type == SettingsItemType.DoubleDictionary || // Note: Implement when needed
                type == SettingsItemType.UserDictionary || // Note: Implement when needed
                type == SettingsItemType.RoleDictionary || // Note: Implement when needed
                type == SettingsItemType.ChannelDictionary*/) return true; // Note: Implement when needed
            return false;
        }
        
        public bool TypeIsDictionaryList(SettingsItemType type)
        {
            if (type == SettingsItemType.StringListDictionary /*||
                type == SettingsItemType.CharListDictionary || // Note: Implement when needed
                type == SettingsItemType.BooleanListDictionary || // Note: Implement when needed
                type == SettingsItemType.IntegerListDictionary || // Note: Implement when needed
                type == SettingsItemType.DoubleListDictionary || // Note: Implement when needed
                type == SettingsItemType.UserListDictionary || // Note: Implement when needed
                type == SettingsItemType.RoleListDictionary || // Note: Implement when needed
                type == SettingsItemType.ChannelListDictionary*/) return true; // Note: Implement when needed
            return false;
        }
    }
}
