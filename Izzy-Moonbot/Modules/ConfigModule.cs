using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Describers;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Modules;

[Summary("Module for managing Izzy's configuation")]
public class ConfigModule : ModuleBase<SocketCommandContext>
{
    private readonly ServerSettings _settings;
    private readonly ServerSettingsDescriber _settingsDescriber;


    public ConfigModule(ServerSettings settings, ServerSettingsDescriber settingsDescriber)
    {
        _settings = settings;
        _settingsDescriber = settingsDescriber;
    }

# nullable enable
    [Command("config")]
    [Summary("Config management")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task ConfigCommandAsync(
        [Summary("The item to get/modify.")] string configItemKey = "",
        [Summary("")] [Remainder] string? value = "")
    {
        if (configItemKey == "")
        {
            await Context.Message.ReplyAsync($"Hii!! Here's now to use the config command!{Environment.NewLine}" +
                                             $"Run `{_settings.Prefix}config <category>` to list the config items in a category.{Environment.NewLine}" +
                                             $"Run `{_settings.Prefix}config <item>` to view information about an item.{Environment.NewLine}{Environment.NewLine}" +
                                             $"Here's a list of all possible categories.{Environment.NewLine}```{Environment.NewLine}" +
                                             $"core - Config items which dictate core settings (often global).{Environment.NewLine}" +
                                             $"moderation - Config items which dictate moderation settings.{Environment.NewLine}" +
                                             $"debug - Debug config items used to debug Izzy.{Environment.NewLine}" +
                                             $"user - Config items regarding users.{Environment.NewLine}" +
                                             $"filter - Config items regarding the filter.{Environment.NewLine}" +
                                             $"spam - Config items regarding spam pressure.{Environment.NewLine}" +
                                             $"raid - Config items regarding antiraid.{Environment.NewLine}```");

            return;
        }

        var configItem = _settingsDescriber.GetItem(configItemKey);

        if (configItem == null)
        {
            // Config item not found, but could be a category.
            var configCategory = _settingsDescriber.StringToCategory(configItemKey);
            if (configCategory.HasValue)
            {
                // it's not null we literally check above u stupid piece of code
                var category = configCategory.Value;

                var itemList = _settingsDescriber.GetSettableConfigItemsByCategory(category);

                if (itemList.Count > 10)
                {
                    // Use pagination
                    var pages = new List<string>();
                    var pageNumber = -1;
                    for (var i = 0; i < itemList.Count; i++)
                    {
                        if (i % 10 == 0)
                        {
                            pageNumber += 1;
                            pages.Add("");
                        }

                        pages[pageNumber] += itemList[i] + Environment.NewLine;
                    }


                    string[] staticParts =
                    {
                        $"Hii!! Here's a list of all the config items I could find in the {_settingsDescriber.CategoryToString(category)} category!",
                        $"Run `{_settings.Prefix}config <item> to view information about an item!"
                    };

                    var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                }
                else
                {
                    await ReplyAsync(
                        $"Hii!! Here's a list of all the config items I could find in the {_settingsDescriber.CategoryToString(category)} category!" +
                        $"{Environment.NewLine}```{Environment.NewLine}{string.Join(Environment.NewLine, itemList)}{Environment.NewLine}```{Environment.NewLine}" +
                        $"Run `{_settings.Prefix}config <item> to view information about an item!");
                }

                return;
            }

            await ReplyAsync($"Sorry, I couldn't find a config value or category called `{configItemKey}`!");
            return;
        }

        if (value == "")
        {
            // Only the configItemKey was given, we give the user what their data was
            var nullableString = "(Pass `<nothing>` as the value when setting to set to nothing/null)";
            if (!configItem.Nullable) nullableString = "";

            switch (configItem.Type)
            {
                case SettingsItemType.String:
                case SettingsItemType.Char:
                case SettingsItemType.Boolean:
                case SettingsItemType.Integer:
                case SettingsItemType.Double:
                    await ReplyAsync(
                        $"**{configItemKey}** - {_settingsDescriber.TypeToString(configItem.Type)} - {_settingsDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Current value: `{ConfigHelper.GetValue<ServerSettings>(_settings, configItemKey)}`{Environment.NewLine}" +
                        $"Run `{_settings.Prefix}config {configItemKey} <value>` to set this value. {nullableString}",
                        allowedMentions: AllowedMentions.None);
                    break;
                case SettingsItemType.User:
                    await ReplyAsync(
                        $"**{configItemKey}** - {_settingsDescriber.TypeToString(configItem.Type)} - {_settingsDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Current value: <@{ConfigHelper.GetValue<ServerSettings>(_settings, configItemKey)}>{Environment.NewLine}" +
                        $"Run `{_settings.Prefix}config {configItemKey} <value>` to set this value. {nullableString}",
                        allowedMentions: AllowedMentions.None);
                    break;
                case SettingsItemType.Role:
                    await ReplyAsync(
                        $"**{configItemKey}** - {_settingsDescriber.TypeToString(configItem.Type)} - {_settingsDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Current value: <@&{ConfigHelper.GetValue<ServerSettings>(_settings, configItemKey)}>{Environment.NewLine}" +
                        $"Run `{_settings.Prefix}config {configItemKey} <value>` to set this value. {nullableString}",
                        allowedMentions: AllowedMentions.None);
                    break;
                case SettingsItemType.Channel:
                    await ReplyAsync(
                        $"**{configItemKey}** - {_settingsDescriber.TypeToString(configItem.Type)} - {_settingsDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Current value: <#{ConfigHelper.GetValue<ServerSettings>(_settings, configItemKey)}>{Environment.NewLine}" +
                        $"Run `{_settings.Prefix}config {configItemKey} <value>` to set this value. {nullableString}",
                        allowedMentions: AllowedMentions.None);
                    break;
                case SettingsItemType.StringList:
                case SettingsItemType.CharList:
                case SettingsItemType.BooleanList:
                case SettingsItemType.IntegerList:
                case SettingsItemType.DoubleList:
                case SettingsItemType.UserList:
                case SettingsItemType.RoleList:
                case SettingsItemType.ChannelList:
                    await ReplyAsync(
                        $"**{configItemKey}** - {_settingsDescriber.TypeToString(configItem.Type)} - {_settingsDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Run `{_settings.Prefix}config {configItemKey} list` to view the contents of this list.{Environment.NewLine}" +
                        $"Run `{_settings.Prefix}config {configItemKey} add <value>` to add a value to this list. {nullableString}{Environment.NewLine}" +
                        $"Run `{_settings.Prefix}config {configItemKey} remove <value>` to remove a value from this list. {nullableString}",
                        allowedMentions: AllowedMentions.None);
                    break;
                case SettingsItemType.StringDictionary:
                //case SettingsItemType.CharDictionary: // Note: Implement when needed
                case SettingsItemType.BooleanDictionary:
                //case SettingsItemType.IntegerDictionary: // Note: Implement when needed
                //case SettingsItemType.DoubleDictionary: // Note: Implement when needed
                //case SettingsItemType.UserDictionary: // Note: Implement when needed
                //case SettingsItemType.RoleDictionary: // Note: Implement when needed
                //case SettingsItemType.ChannelDictionary: // Note: Implement when needed
                case SettingsItemType.StringListDictionary:
                    //case SettingsItemType.CharListDictionary: // Note: Implement when needed
                    //case SettingsItemType.BooleanListDictionary: // Note: Implement when needed
                    //case SettingsItemType.IntegerListDictionary: // Note: Implement when needed
                    //case SettingsItemType.DoubleListDictionary: // Note: Implement when needed
                    //case SettingsItemType.UserListDictionary: // Note: Implement when needed
                    //case SettingsItemType.RoleListDictionary: // Note: Implement when needed
                    //case SettingsItemType.ChannelListDictionary: // Note: Implement when needed
                    await ReplyAsync(
                        $"**{configItemKey}** - {_settingsDescriber.TypeToString(configItem.Type)} - {_settingsDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Run `{_settings.Prefix}config {configItemKey} list` to view a list of keys in this map.{Environment.NewLine}" +
                        $"Run `{_settings.Prefix}config {configItemKey} create <key>` to create a new key in this map.{Environment.NewLine}" +
                        $"Run `{_settings.Prefix}config {configItemKey} delete <key>` to delete a key from this map.{Environment.NewLine}" +
                        $"Run `{_settings.Prefix}config {configItemKey} <key>` to view information about a key in this map.",
                        allowedMentions: AllowedMentions.None);
                    break;
                default:
                    await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                    break;
            }
        }
        else
        {
            // value provided
            if (_settingsDescriber.TypeIsValue(configItem.Type))
            {
                switch (configItem.Type)
                {
                    case SettingsItemType.String:
                        if (configItem.Nullable && value == "<nothing>") value = null;

                        var resultString =
                            await ConfigHelper.SetStringValue<ServerSettings>(_settings, configItemKey, value);
                        await ReplyAsync($"I've set `{configItemKey}` to the following content: {resultString}",
                            allowedMentions: AllowedMentions.None);
                        break;
                    case SettingsItemType.Char:
                        if (configItem.Nullable && value == "<nothing>") value = null;

                        try
                        {
                            char? output = null;
                            if (value != null)
                            {
                                if (!char.TryParse(value, out var res))
                                    throw new FormatException(); // Trip "invalid content" catch below.
                                output = res;
                            }

                            var resultChar =
                                await ConfigHelper.SetCharValue<ServerSettings>(_settings, configItemKey, output);
                            await ReplyAsync($"I've set `{configItemKey}` to the following content: {resultChar}",
                                allowedMentions: AllowedMentions.None);
                        }
                        catch (FormatException)
                        {
                            await ReplyAsync(
                                $"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a character. Please try again.",
                                allowedMentions: AllowedMentions.None);
                        }

                        break;
                    case SettingsItemType.Boolean:
                        if (configItem.Nullable && value == "<nothing>") value = null;

                        try
                        {
                            var resultBoolean =
                                await ConfigHelper.SetBooleanValue<ServerSettings>(_settings, configItemKey, value);
                            await ReplyAsync($"I've set `{configItemKey}` to the following content: {resultBoolean}",
                                allowedMentions: AllowedMentions.None);
                        }
                        catch (FormatException)
                        {
                            await ReplyAsync(
                                $"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a boolean. Please try again.",
                                allowedMentions: AllowedMentions.None);
                        }

                        break;
                    case SettingsItemType.Integer:
                        if (configItem.Nullable && value == "<nothing>") value = null;

                        try
                        {
                            int? output = null;
                            if (value != null)
                            {
                                if (!int.TryParse(value, out var res))
                                    throw new FormatException(); // Trip "invalid content" catch below.
                                output = res;
                            }

                            var resultInteger =
                                await ConfigHelper.SetIntValue<ServerSettings>(_settings, configItemKey, output);
                            await ReplyAsync($"I've set `{configItemKey}` to the following content: {resultInteger}",
                                allowedMentions: AllowedMentions.None);
                        }
                        catch (FormatException)
                        {
                            await ReplyAsync(
                                $"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a integer. Please try again.",
                                allowedMentions: AllowedMentions.None);
                        }

                        break;
                    case SettingsItemType.Double:
                        if (configItem.Nullable && value == "<nothing>") value = null;

                        try
                        {
                            double? output = null;
                            if (value != null)
                            {
                                if (!double.TryParse(value, out var res))
                                    throw new FormatException(); // Trip "invalid content" catch below.
                                output = res;
                            }

                            var resultDouble =
                                await ConfigHelper.SetDoubleValue<ServerSettings>(_settings, configItemKey, output);
                            await ReplyAsync($"I've set `{configItemKey}` to the following content: {resultDouble}",
                                allowedMentions: AllowedMentions.None);
                        }
                        catch (FormatException)
                        {
                            await ReplyAsync(
                                $"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a double. Please try again.",
                                allowedMentions: AllowedMentions.None);
                        }

                        break;
                    case SettingsItemType.User:
                        if (configItem.Nullable && value == "<nothing>") value = null;
                        try
                        {
                            var result =
                                await ConfigHelper.SetUserValue<ServerSettings>(_settings, configItemKey, value,
                                    Context);
                            var response = "`null`";
                            if (result != null) response = $"<@{result.Id}>";
                            await ReplyAsync($"I've set `{configItemKey}` to the following content: {response}");
                        }
                        catch (MemberAccessException)
                        {
                            await ReplyAsync(
                                $"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a user. Please try again.",
                                allowedMentions: AllowedMentions.None);
                        }

                        break;
                    case SettingsItemType.Role:
                        if (configItem.Nullable && value == "<nothing>") value = null;
                        try
                        {
                            var result =
                                await ConfigHelper.SetRoleValue<ServerSettings>(_settings, configItemKey, value,
                                    Context);
                            var response = "`null`";
                            if (result != null) response = $"<@&{result.Id}>";
                            await ReplyAsync($"I've set `{configItemKey}` to the following content: {response}",
                                allowedMentions: AllowedMentions.None);
                        }
                        catch (MemberAccessException)
                        {
                            await ReplyAsync(
                                $"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a role. Please try again.",
                                allowedMentions: AllowedMentions.None);
                        }

                        break;
                    case SettingsItemType.Channel:
                        if (configItem.Nullable && value == "<nothing>") value = null;
                        try
                        {
                            var result =
                                await ConfigHelper.SetChannelValue<ServerSettings>(_settings, configItemKey, value,
                                    Context);
                            var response = "`null`";
                            if (result != null) response = $"<#{result.Id}>";
                            await ReplyAsync($"I've set `{configItemKey}` to the following content: {response}",
                                allowedMentions: AllowedMentions.None);
                        }
                        catch (MemberAccessException)
                        {
                            await ReplyAsync(
                                $"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a channel. Please try again.",
                                allowedMentions: AllowedMentions.None);
                        }

                        break;
                    default:
                        await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                        break;
                }
            }
            else if (_settingsDescriber.TypeIsList(configItem.Type))
            {
                var action = value.Split(' ')[0].ToLower();
                value = value.Replace(action + " ", "");

                if (action == "list")
                    switch (configItem.Type)
                    {
                        case SettingsItemType.StringList:
                            var stringList = ConfigHelper.GetStringList<ServerSettings>(_settings, configItemKey);

                            if (stringList == null)
                            {
                                await ReplyAsync("Somehow, the entire list is null.");
                                return;
                            }

                            if (stringList.Count > 10)
                            {
                                // Use pagination
                                var pages = new List<string>();
                                var pageNumber = -1;
                                for (var i = 0; i < stringList.Count; i++)
                                {
                                    if (i % 10 == 0)
                                    {
                                        pageNumber += 1;
                                        pages.Add("");
                                    }

                                    pages[pageNumber] += stringList[i] + Environment.NewLine;
                                }


                                string[] staticParts =
                                {
                                    $"**{configItemKey}** contains the following values:",
                                    ""
                                };

                                var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                            }
                            else
                            {
                                await ReplyAsync(
                                    $"**{configItemKey}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", stringList)}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }

                            break;
                        case SettingsItemType.CharList:
                            var charList = ConfigHelper.GetCharList<ServerSettings>(_settings, configItemKey);

                            if (charList == null)
                            {
                                await ReplyAsync("Somehow, the entire list is null.");
                                return;
                            }

                            if (charList.Count > 10)
                            {
                                // Use pagination
                                var pages = new List<string>();
                                var pageNumber = -1;
                                for (var i = 0; i < charList.Count; i++)
                                {
                                    if (i % 10 == 0)
                                    {
                                        pageNumber += 1;
                                        pages.Add("");
                                    }

                                    pages[pageNumber] += charList[i] + Environment.NewLine;
                                }


                                string[] staticParts =
                                {
                                    $"**{configItemKey}** contains the following values:",
                                    ""
                                };

                                var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                            }
                            else
                            {
                                await ReplyAsync(
                                    $"**{configItemKey}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", charList)}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }

                            break;
                        case SettingsItemType.BooleanList:
                            var booleanList = ConfigHelper.GetBooleanList<ServerSettings>(_settings, configItemKey);

                            if (booleanList == null)
                            {
                                await ReplyAsync("Somehow, the entire list is null.");
                                return;
                            }

                            if (booleanList.Count > 10)
                            {
                                // Use pagination
                                var pages = new List<string>();
                                var pageNumber = -1;
                                for (var i = 0; i < booleanList.Count; i++)
                                {
                                    if (i % 10 == 0)
                                    {
                                        pageNumber += 1;
                                        pages.Add("");
                                    }

                                    pages[pageNumber] += booleanList[i] + Environment.NewLine;
                                }


                                string[] staticParts =
                                {
                                    $"**{configItemKey}** contains the following values:",
                                    ""
                                };

                                var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                            }
                            else
                            {
                                await ReplyAsync(
                                    $"**{configItemKey}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", booleanList)}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }

                            break;
                        case SettingsItemType.IntegerList:
                            var intList = ConfigHelper.GetIntList<ServerSettings>(_settings, configItemKey);

                            if (intList == null)
                            {
                                await ReplyAsync("Somehow, the entire list is null.");
                                return;
                            }

                            if (intList.Count > 10)
                            {
                                // Use pagination
                                var pages = new List<string>();
                                var pageNumber = -1;
                                for (var i = 0; i < intList.Count; i++)
                                {
                                    if (i % 10 == 0)
                                    {
                                        pageNumber += 1;
                                        pages.Add("");
                                    }

                                    pages[pageNumber] += intList[i] + Environment.NewLine;
                                }


                                string[] staticParts =
                                {
                                    $"**{configItemKey}** contains the following values:",
                                    ""
                                };

                                var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                            }
                            else
                            {
                                await ReplyAsync(
                                    $"**{configItemKey}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", intList)}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }

                            break;
                        case SettingsItemType.DoubleList:
                            var doubleList = ConfigHelper.GetDoubleList<ServerSettings>(_settings, configItemKey);

                            if (doubleList == null)
                            {
                                await ReplyAsync("Somehow, the entire list is null.");
                                return;
                            }

                            if (doubleList.Count > 10)
                            {
                                // Use pagination
                                var pages = new List<string>();
                                var pageNumber = -1;
                                for (var i = 0; i < doubleList.Count; i++)
                                {
                                    if (i % 10 == 0)
                                    {
                                        pageNumber += 1;
                                        pages.Add("");
                                    }

                                    pages[pageNumber] += doubleList[i] + Environment.NewLine;
                                }


                                string[] staticParts =
                                {
                                    $"**{configItemKey}** contains the following values:",
                                    ""
                                };

                                var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                            }
                            else
                            {
                                await ReplyAsync(
                                    $"**{configItemKey}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", doubleList)}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }

                            break;
                        case SettingsItemType.UserList:
                            var userList = ConfigHelper.GetUserList<ServerSettings>(_settings, configItemKey, Context);

                            var userMentionList = new List<string>();
                            foreach (var user in userList) userMentionList.Add($"<@{user.Id}>");

                            if (userMentionList.Count > 10)
                            {
                                // Use pagination
                                var pages = new List<string>();
                                var pageNumber = -1;
                                for (var i = 0; i < userMentionList.Count; i++)
                                {
                                    if (i % 10 == 0)
                                    {
                                        pageNumber += 1;
                                        pages.Add("");
                                    }

                                    pages[pageNumber] += userMentionList[i] + Environment.NewLine;
                                }


                                string[] staticParts =
                                {
                                    $"**{configItemKey}** contains the following values:",
                                    ""
                                };

                                var paginationMessage =
                                    new PaginationHelper(Context, pages.ToArray(), staticParts, 0, false);
                            }
                            else
                            {
                                await ReplyAsync(
                                    $"**{configItemKey}** contains the following values:{Environment.NewLine}{string.Join(", ", userMentionList)}");
                            }

                            break;
                        case SettingsItemType.RoleList:
                            var roleList = ConfigHelper.GetRoleList<ServerSettings>(_settings, configItemKey, Context);

                            var roleMentionList = new List<string>();
                            foreach (var role in roleList) roleMentionList.Add(role.Mention);

                            if (roleMentionList.Count > 10)
                            {
                                // Use pagination
                                var pages = new List<string>();
                                var pageNumber = -1;
                                for (var i = 0; i < roleMentionList.Count; i++)
                                {
                                    if (i % 10 == 0)
                                    {
                                        pageNumber += 1;
                                        pages.Add("");
                                    }

                                    pages[pageNumber] += roleMentionList[i] + Environment.NewLine;
                                }


                                string[] staticParts =
                                {
                                    $"**{configItemKey}** contains the following values:",
                                    ""
                                };

                                var paginationMessage = new PaginationHelper(Context, pages.ToArray(),
                                    staticParts, 0, false, AllowedMentions.None);
                            }
                            else
                            {
                                await ReplyAsync(
                                    $"**{configItemKey}** contains the following values:{Environment.NewLine}{string.Join(", ", roleMentionList)}",
                                    allowedMentions: AllowedMentions.None);
                            }

                            break;
                        case SettingsItemType.ChannelList:
                            var channelList =
                                ConfigHelper.GetChannelList<ServerSettings>(_settings, configItemKey, Context);

                            var channelMentionList = new List<string>();
                            foreach (var channel in channelList) channelMentionList.Add($"<#{channel.Id}>");

                            if (channelMentionList.Count > 10)
                            {
                                // Use pagination
                                var pages = new List<string>();
                                var pageNumber = -1;
                                for (var i = 0; i < channelMentionList.Count; i++)
                                {
                                    if (i % 10 == 0)
                                    {
                                        pageNumber += 1;
                                        pages.Add("");
                                    }

                                    pages[pageNumber] += channelMentionList[i] + Environment.NewLine;
                                }


                                string[] staticParts =
                                {
                                    $"**{configItemKey}** contains the following values:",
                                    ""
                                };

                                var paginationMessage =
                                    new PaginationHelper(Context, pages.ToArray(), staticParts, 0, false);
                            }
                            else
                            {
                                await ReplyAsync(
                                    $"**{configItemKey}** contains the following values:{Environment.NewLine}{string.Join(", ", channelMentionList)}",
                                    allowedMentions: AllowedMentions.None);
                            }

                            break;
                        default:
                            await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                else if (action == "add")
                    switch (configItem.Type)
                    {
                        case SettingsItemType.StringList:
                            try
                            {
                                var output =
                                    await ConfigHelper.AddToStringList<ServerSettings>(_settings, configItemKey, value);

                                await ReplyAsync(
                                    $"I added the following content to the `{configItemKey}` string list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't add your content to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case SettingsItemType.CharList:
                            try
                            {
                                if (!char.TryParse(value, out var charValue)) throw new FormatException();

                                var output =
                                    await ConfigHelper.AddToCharList<ServerSettings>(_settings, configItemKey,
                                        charValue);

                                await ReplyAsync(
                                    $"I added the following content to the `{configItemKey}` character list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await ReplyAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into a single character. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case SettingsItemType.BooleanList:
                            try
                            {
                                var output =
                                    await ConfigHelper.AddToBooleanList<ServerSettings>(_settings, configItemKey,
                                        value);

                                await ReplyAsync(
                                    $"I added the following content to the `{configItemKey}` boolean list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await ReplyAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into a boolean. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case SettingsItemType.IntegerList:
                            try
                            {
                                if (!int.TryParse(value, out var intValue)) throw new FormatException();

                                var output =
                                    await ConfigHelper.AddToIntList<ServerSettings>(_settings, configItemKey, intValue);

                                await ReplyAsync(
                                    $"I added the following content to the `{configItemKey}` integer list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await ReplyAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into an integer. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case SettingsItemType.DoubleList:
                            try
                            {
                                if (!double.TryParse(value, out var doubleValue)) throw new FormatException();

                                var output =
                                    await ConfigHelper.AddToDoubleList<ServerSettings>(_settings, configItemKey,
                                        doubleValue);

                                await ReplyAsync(
                                    $"I added the following content to the `{configItemKey}` double list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await ReplyAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into a double. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case SettingsItemType.UserList:
                            try
                            {
                                var output =
                                    await ConfigHelper.AddToUserList<ServerSettings>(_settings, configItemKey, value,
                                        Context);

                                await ReplyAsync(
                                    $"I added the following content to the `{configItemKey}` user list:{Environment.NewLine}{output}");
                            }
                            catch (MemberAccessException)
                            {
                                await ReplyAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into a user I know about. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await ReplyAsync(
                                    $"I couldn't add the user you provided to the `{configItemKey}` list because the user is already in that list.");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case SettingsItemType.RoleList:
                            try
                            {
                                var output =
                                    await ConfigHelper.AddToRoleList<ServerSettings>(_settings, configItemKey, value,
                                        Context);

                                await ReplyAsync(
                                    $"I added the following content to the `{configItemKey}` role list:{Environment.NewLine}{output}");
                            }
                            catch (MemberAccessException)
                            {
                                await ReplyAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into a role I know about. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await ReplyAsync(
                                    $"I couldn't add the role you provided to the `{configItemKey}` list because the role is already in that list.");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case SettingsItemType.ChannelList:
                            try
                            {
                                var output =
                                    await ConfigHelper.AddToChannelList<ServerSettings>(_settings, configItemKey, value,
                                        Context);

                                await ReplyAsync(
                                    $"I added the following content to the `{configItemKey}` channel list:{Environment.NewLine}{output}");
                            }
                            catch (MemberAccessException)
                            {
                                await ReplyAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into a channel I know about. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await ReplyAsync(
                                    $"I couldn't add the channel you provided to the `{configItemKey}` list because the channel is already in that list.");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        default:
                            await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                else if (action == "remove")
                    switch (configItem.Type)
                    {
                        case SettingsItemType.StringList:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromStringList<ServerSettings>(_settings, configItemKey,
                                        value);

                                await ReplyAsync(
                                    $"I removed the following content from the `{configItemKey}` string list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the content isn't in that list to begin with.");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case SettingsItemType.CharList:
                            try
                            {
                                if (!char.TryParse(value, out var charValue)) throw new FormatException();

                                var output =
                                    await ConfigHelper.AddToCharList<ServerSettings>(_settings, configItemKey,
                                        charValue);

                                await ReplyAsync(
                                    $"I removed the following content from the `{configItemKey}` character list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a character. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the content isn't in that list to begin with.");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case SettingsItemType.BooleanList:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromBooleanList<ServerSettings>(_settings, configItemKey,
                                        value);

                                await ReplyAsync(
                                    $"I removed the following content from the `{configItemKey}` boolean list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a boolean. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the content isn't in that list to begin with.");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case SettingsItemType.IntegerList:
                            try
                            {
                                if (!int.TryParse(value, out var intValue)) throw new FormatException();

                                var output =
                                    await ConfigHelper.RemoveFromIntList<ServerSettings>(_settings, configItemKey,
                                        intValue);

                                await ReplyAsync(
                                    $"I removed the following content from the `{configItemKey}` integer list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a integer. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the content isn't in that list to begin with.");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case SettingsItemType.DoubleList:
                            try
                            {
                                if (!double.TryParse(value, out var doubleValue)) throw new FormatException();

                                var output =
                                    await ConfigHelper.RemoveFromDoubleList<ServerSettings>(_settings, configItemKey,
                                        doubleValue);

                                await ReplyAsync(
                                    $"I removed the following content from the `{configItemKey}` double list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a double. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the content isn't in that list to begin with.");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case SettingsItemType.UserList:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromUserList<ServerSettings>(_settings, configItemKey,
                                        value, Context);

                                await ReplyAsync(
                                    $"I removed the following content from the `{configItemKey}` user list:{Environment.NewLine}{output}");
                            }
                            catch (MemberAccessException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a user I know about. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the user you provided from the `{configItemKey}` list because the user isn't in that list to begin with.");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case SettingsItemType.RoleList:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromRoleList<ServerSettings>(_settings, configItemKey,
                                        value, Context);

                                await ReplyAsync(
                                    $"I removed the following content from the `{configItemKey}` role list:{Environment.NewLine}{output}");
                            }
                            catch (MemberAccessException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a role I know about. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the role you provided from the `{configItemKey}` list because the role isn't in that list to begin with.");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case SettingsItemType.ChannelList:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromChannelList<ServerSettings>(_settings, configItemKey,
                                        value, Context);

                                await ReplyAsync(
                                    $"I removed the following content from the `{configItemKey}` channel list:{Environment.NewLine}{output}");
                            }
                            catch (MemberAccessException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a channel I know about. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the channel you provided from the `{configItemKey}` list because the channel isn't in that list to begin with.");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        default:
                            await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
            }
            else if (_settingsDescriber.TypeIsDictionaryValue(configItem.Type))
            {
                var action = value.Split(' ')[0].ToLower();
                value = value.Replace(action + " ", "");

                if (action == "list")
                {
                    switch (configItem.Type)
                    {
                        case SettingsItemType.StringDictionary:
                            try
                            {
                                var keys = new List<string>();
                                if (configItem.Nullable)
                                    keys = ConfigHelper
                                        .GetNullableStringDictionary<ServerSettings>(_settings, configItemKey, Context)
                                        .Keys.ToList();
                                else
                                    keys = ConfigHelper
                                        .GetStringDictionary<ServerSettings>(_settings, configItemKey, Context).Keys
                                        .ToList();

                                if (keys.Count > 10)
                                {
                                    // Use pagination
                                    var pages = new List<string>();
                                    var pageNumber = -1;
                                    for (var i = 0; i < keys.Count; i++)
                                    {
                                        if (i % 10 == 0)
                                        {
                                            pageNumber += 1;
                                            pages.Add("");
                                        }

                                        pages[pageNumber] += keys[i] + Environment.NewLine;
                                    }

                                    string[] staticParts =
                                    {
                                        $"**{configItemKey}** contains the following keys:",
                                        ""
                                    };

                                    var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                                }
                                else
                                {
                                    await ReplyAsync(
                                        $"**{configItemKey}** contains the following keys:{Environment.NewLine}```{Environment.NewLine}{string.Join($"{Environment.NewLine}", keys)}{Environment.NewLine}```");
                                }
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                await ReplyAsync(
                                    $"I couldn't list the keys in the `{configItemKey}` map? {ex.Message}");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't list the keys in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case SettingsItemType.BooleanDictionary:
                            try
                            {
                                var keys = ConfigHelper
                                    .GetBooleanDictionary<ServerSettings>(_settings, configItemKey, Context).Keys
                                    .ToList();

                                if (keys.Count > 10)
                                {
                                    // Use pagination
                                    var pages = new List<string>();
                                    var pageNumber = -1;
                                    for (var i = 0; i < keys.Count; i++)
                                    {
                                        if (i % 10 == 0)
                                        {
                                            pageNumber += 1;
                                            pages.Add("");
                                        }

                                        pages[pageNumber] += keys[i] + Environment.NewLine;
                                    }

                                    string[] staticParts =
                                    {
                                        $"**{configItemKey}** contains the following keys:",
                                        ""
                                    };

                                    var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                                }
                                else
                                {
                                    await ReplyAsync(
                                        $"**{configItemKey}** contains the following keys:{Environment.NewLine}```{Environment.NewLine}{string.Join($"{Environment.NewLine}", keys)}{Environment.NewLine}```");
                                }
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                await ReplyAsync(
                                    $"I couldn't list the keys in the `{configItemKey}` map? {ex.Message}");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't list the keys in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        default:
                            await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                }
                else if (action == "create")
                {
                    switch (configItem.Type)
                    {
                        case SettingsItemType.StringDictionary:
                            try
                            {
                                if (configItem.Nullable)
                                    await ConfigHelper.CreateNullableStringDictionaryKey<ServerSettings>(_settings,
                                        configItemKey, value, Context);
                                else
                                    await ConfigHelper.CreateStringDictionaryKey<ServerSettings>(_settings,
                                        configItemKey, value, Context);

                                await ReplyAsync(
                                    $"I created the string with the following key in the `{configItemKey}` map: {value}");
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await ReplyAsync(
                                    $"I couldn't create the string you wanted in the `{configItemKey}` map because the `{value}` key already exists.");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't create the string you wanted in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case SettingsItemType.BooleanDictionary:
                            try
                            {
                                await ConfigHelper.CreateBooleanDictionaryKey<ServerSettings>(_settings, configItemKey,
                                    value, Context);

                                await ReplyAsync(
                                    $"I created the boolean with the following key in the `{configItemKey}` map: {value}");
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await ReplyAsync(
                                    $"I couldn't create the boolean you wanted in the `{configItemKey}` map because the `{value}` key already exists.");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't create the boolean you wanted in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        default:
                            await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                }
                else if (action == "remove")
                {
                    switch (configItem.Type)
                    {
                        case SettingsItemType.StringDictionary:
                            try
                            {
                                if (configItem.Nullable)
                                    await ConfigHelper.RemoveNullableStringDictionaryKey<ServerSettings>(_settings,
                                        configItemKey, value, Context);
                                else
                                    await ConfigHelper.RemoveStringDictionaryKey<ServerSettings>(_settings,
                                        configItemKey, value, Context);

                                await ReplyAsync(
                                    $"I removed the string with the following key from the `{configItemKey}` map: {value}");
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the string you wanted from the `{configItemKey}` map because the `{value}` key already doesn't exist.");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the string you wanted from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case SettingsItemType.BooleanDictionary:
                            try
                            {
                                await ConfigHelper.RemoveBooleanDictionaryKey<ServerSettings>(_settings, configItemKey,
                                    value, Context);

                                await ReplyAsync(
                                    $"I removed the boolean with the following key from the `{configItemKey}` map: {value}");
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the boolean you wanted from the `{configItemKey}` map because the `{value}` key already exists.");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't remove the boolean you wanted from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        default:
                            await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                }
                else
                {
                    // Assume key
                    var key = action.ToLower();
                    action = value.Split(' ')[0].ToLower();
                    value = value.Replace(action + " ", "");

                    if (action == "get")
                    {
                        switch (configItem.Type)
                        {
                            case SettingsItemType.StringDictionary:
                                try
                                {
                                    var contents = "";

                                    if (configItem.Nullable)
                                        contents = ConfigHelper.GetNullableStringDictionaryValue<ServerSettings>(
                                            _settings,
                                            configItemKey, key, Context);
                                    else
                                        contents = (string?)ConfigHelper.GetStringDictionaryValue<ServerSettings>(
                                            _settings,
                                            configItemKey, key, Context);

                                    await ReplyAsync(
                                        $"**{key}** contains the following value: {contents}");
                                }
                                catch (ArgumentOutOfRangeException ex)
                                {
                                    await ReplyAsync(
                                        $"I couldn't get the value in the `{key}` key from the `{configItemKey}` map? {ex.Message}");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync(
                                        $"I couldn't get the value in the `{key}` key from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                                }

                                break;
                            case SettingsItemType.BooleanDictionary:
                                try
                                {
                                    var contents = ConfigHelper.GetBooleanDictionaryValue<ServerSettings>(_settings,
                                        configItemKey, key, Context);

                                    await ReplyAsync(
                                        $"**{key}** contains the following value: {contents}");
                                }
                                catch (ArgumentOutOfRangeException ex)
                                {
                                    await ReplyAsync(
                                        $"I couldn't get the value in the `{key}` key from the `{configItemKey}` map? {ex.Message}");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync(
                                        $"I couldn't get the value in the `{key}` key from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                                }

                                break;
                            default:
                                await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                                break;
                        }
                    }
                    else if (action == "set")
                    {
                        switch (configItem.Type)
                        {
                            case SettingsItemType.StringDictionary:
                                try
                                {
                                    if (configItem.Nullable)
                                    {
                                        if (value == "<nothing>") value = null;

                                        var result =
                                            await ConfigHelper.SetNullableStringDictionaryValue<ServerSettings>(
                                                _settings, configItemKey, key, value, Context);

                                        await Context.Message.ReplyAsync(
                                            $"I've set `{key}` to the following content: {result}",
                                            allowedMentions: AllowedMentions.None);
                                    }
                                    else
                                    {
                                        var result = await ConfigHelper.SetStringDictionaryValue<ServerSettings>(
                                            _settings, configItemKey, key, value, Context);

                                        await Context.Message.ReplyAsync(
                                            $"I've set `{key}` to the following content: {result}",
                                            allowedMentions: AllowedMentions.None);
                                    }
                                }
                                catch (ArgumentOutOfRangeException ex)
                                {
                                    await ReplyAsync(
                                        $"I couldn't set the value in the `{key}` key from the `{configItemKey}` map? {ex.Message}");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync(
                                        $"I couldn't set the value in the `{key}` key from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                                }

                                break;
                            case SettingsItemType.BooleanDictionary:
                                try
                                {
                                    var result = await ConfigHelper.SetBooleanDictionaryValue<ServerSettings>(
                                        _settings, configItemKey, key, value, Context);

                                    await Context.Message.ReplyAsync(
                                        $"I've set `{key}` to the following content: {result}",
                                        allowedMentions: AllowedMentions.None);
                                }
                                catch (ArgumentOutOfRangeException ex)
                                {
                                    await ReplyAsync(
                                        $"I couldn't set the value in the `{key}` key from the `{configItemKey}` map? {ex.Message}");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync(
                                        $"I couldn't set the value in the `{key}` key from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                                }

                                break;
                            default:
                                await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                                break;
                        }
                    }
                    else
                    {
                        var nullableString = " (Pass `<nothing>` as the value when setting to set to nothing/null)";
                        if (!configItem.Nullable) nullableString = "";

                        await ReplyAsync($"**{key}** - Key in {configItemKey}{Environment.NewLine}" +
                                         $"Run `{_settings.Prefix}config {configItemKey} {key} get` to get the value in this map key.{Environment.NewLine}" +
                                         $"Run `{_settings.Prefix}config {configItemKey} {key} set <value>` to set the value for this map key{nullableString}.",
                            allowedMentions: AllowedMentions.None);
                    }
                }
            }
            else if (_settingsDescriber.TypeIsDictionaryList(configItem.Type))
            {
                var action = value.Split(' ')[0].ToLower();
                value = value.Replace(action + " ", "");

                if (action == "list")
                {
                    switch (configItem.Type)
                    {
                        case SettingsItemType.StringListDictionary:
                            try
                            {
                                var keys = ConfigHelper
                                    .GetStringListDictionary<ServerSettings>(_settings, configItemKey, Context).Keys
                                    .ToList();

                                if (keys.Count > 10)
                                {
                                    // Use pagination
                                    var pages = new List<string>();
                                    var pageNumber = -1;
                                    for (var i = 0; i < keys.Count; i++)
                                    {
                                        if (i % 10 == 0)
                                        {
                                            pageNumber += 1;
                                            pages.Add("");
                                        }

                                        pages[pageNumber] += keys[i] + Environment.NewLine;
                                    }


                                    string[] staticParts =
                                    {
                                        $"**{configItemKey}** contains the following keys:",
                                        ""
                                    };

                                    var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                                }
                                else
                                {
                                    await ReplyAsync(
                                        $"**{configItemKey}** contains the following keys:{Environment.NewLine}```{Environment.NewLine}{string.Join($"{Environment.NewLine}", keys)}{Environment.NewLine}```");
                                }
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                await ReplyAsync(
                                    $"I couldn't list the keys in the `{configItemKey}` map? {ex.Message}");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't list the keys in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        default:
                            await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                }
                else if (action == "create")
                {
                    // TODO: Creating keys in Dictionary Lists
                    switch (configItem.Type)
                    {
                        case SettingsItemType.StringListDictionary:
                            try
                            {
                                await ConfigHelper.CreateStringListDictionaryKey<ServerSettings>(_settings,
                                    configItemKey, value, Context);

                                await ReplyAsync(
                                    $"I created the string list with the following key in the `{configItemKey}` map: {value}");
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await ReplyAsync(
                                    $"I couldn't create the string list you wanted in the `{configItemKey}` map because the `{value}` key already exists.");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't create the string list you wanted in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        default:
                            await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                }
                else if (action == "delete")
                {
                    switch (configItem.Type)
                    {
                        case SettingsItemType.StringListDictionary:
                            try
                            {
                                await ConfigHelper.RemoveStringListDictionaryKey<ServerSettings>(_settings,
                                    configItemKey, value, Context);

                                await ReplyAsync(
                                    $"I deleted the string list with the following key from the `{configItemKey}` map: {value}");
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await ReplyAsync(
                                    $"I couldn't delete the string list you wanted from the `{configItemKey}` map because the `{value}` key already doesn't exist.");
                            }
                            catch (ArgumentException)
                            {
                                await ReplyAsync(
                                    $"I couldn't delete the string list you wanted from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        // TODO: Other types, I don't see any reason to add them until I need them.
                        default:
                            await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                }
                else
                {
                    // Assume key
                    var key = action.ToLower();
                    action = value.Split(' ')[0].ToLower();
                    value = value.Replace(action + " ", "");

                    if (action == "list")
                    {
                        switch (configItem.Type)
                        {
                            case SettingsItemType.StringListDictionary:
                                var stringList =
                                    ConfigHelper.GetStringListDictionaryValue<ServerSettings>(_settings, configItemKey,
                                        key, Context);

                                if (stringList == null)
                                {
                                    await ReplyAsync("Somehow, the entire list is null.");
                                    return;
                                }

                                if (stringList.Count > 10)
                                {
                                    // Use pagination
                                    var pages = new List<string>();
                                    var pageNumber = -1;
                                    for (var i = 0; i < stringList.Count; i++)
                                    {
                                        if (i % 10 == 0)
                                        {
                                            pageNumber += 1;
                                            pages.Add("");
                                        }

                                        pages[pageNumber] += stringList[i] + Environment.NewLine;
                                    }

                                    string[] staticParts =
                                    {
                                        $"**{key}** contains the following values:",
                                        ""
                                    };

                                    var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                                }
                                else
                                {
                                    await ReplyAsync(
                                        $"**{key}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", stringList)}{Environment.NewLine}```",
                                        allowedMentions: AllowedMentions.None);
                                }

                                break;
                            // TODO: Other types, I don't see any reason to add them until I need them.
                            default:
                                await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                                break;
                        }
                    }
                    else if (action == "add")
                    {
                        switch (configItem.Type)
                        {
                            case SettingsItemType.StringListDictionary:
                                try
                                {
                                    var output =
                                        await ConfigHelper.AddToStringListDictionaryValue<ServerSettings>(_settings,
                                            configItemKey, key, value, Context);

                                    await ReplyAsync(
                                        $"I added the following content to the `{key}` string list in the `{configItemKey}` map:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                        allowedMentions: AllowedMentions.None);
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync(
                                        $"I couldn't add your content to the `{key}` string list in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                                }

                                break;
                            // TODO: Other types, I don't see any reason to add them until I need them.
                            default:
                                await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                                break;
                        }
                    }
                    else if (action == "remove")
                    {
                        switch (configItem.Type)
                        {
                            case SettingsItemType.StringListDictionary:
                                try
                                {
                                    var output =
                                        await ConfigHelper.RemoveFromStringListDictionaryValue<ServerSettings>(
                                            _settings, configItemKey, key, value, Context);

                                    await ReplyAsync(
                                        $"I removed the following content from the `{key}` string list in the `{configItemKey}` map:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                        allowedMentions: AllowedMentions.None);
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync(
                                        $"I couldn't remove your content from the `{key}` string list in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                                }

                                break;
                            // TODO: Other types, I don't see any reason to add them until I need them.
                            default:
                                await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                                break;
                        }
                    }
                    else
                    {
                        var nullableString = " (Pass `<nothing>` as the value when setting to set to nothing/null)";
                        if (!configItem.Nullable) nullableString = "";

                        await ReplyAsync($"**{key}** - Key in {configItemKey}{Environment.NewLine}" +
                                         $"Run `{_settings.Prefix}config {configItemKey} {key} list` to view a list of values in this map key.{Environment.NewLine}" +
                                         $"Run `{_settings.Prefix}config {configItemKey} {key} add <value>` to add a value to this map key{nullableString}.{Environment.NewLine}" +
                                         $"Run `{_settings.Prefix}config {configItemKey} {key} remove <value>` to remove a value from this map key{nullableString}.",
                            allowedMentions: AllowedMentions.None);
                    }
                }
            }
            else
            {
                Context.Message.ReplyAsync($"I couldn't determine what type {configItem.Type} is.");
            }
        }
    }
}