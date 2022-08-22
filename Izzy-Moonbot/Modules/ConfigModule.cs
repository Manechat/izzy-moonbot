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
    private readonly Config _config;
    private readonly ConfigDescriber _configDescriber;


    public ConfigModule(Config config, ConfigDescriber configDescriber)
    {
        _config = config;
        _configDescriber = configDescriber;
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
                                             $"Run `{_config.Prefix}config <category>` to list the config items in a category.{Environment.NewLine}" +
                                             $"Run `{_config.Prefix}config <item>` to view information about an item.{Environment.NewLine}{Environment.NewLine}" +
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

        var configItem = _configDescriber.GetItem(configItemKey);

        if (configItem == null)
        {
            // Config item not found, but could be a category.
            var configCategory = _configDescriber.StringToCategory(configItemKey);
            if (configCategory.HasValue)
            {
                // it's not null we literally check above u stupid piece of code
                var category = configCategory.Value;

                var itemList = _configDescriber.GetSettableConfigItemsByCategory(category);

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
                        $"Hii!! Here's a list of all the config items I could find in the {_configDescriber.CategoryToString(category)} category!",
                        $"Run `{_config.Prefix}config <item> to view information about an item!"
                    };

                    var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                }
                else
                {
                    await ReplyAsync(
                        $"Hii!! Here's a list of all the config items I could find in the {_configDescriber.CategoryToString(category)} category!" +
                        $"{Environment.NewLine}```{Environment.NewLine}{string.Join(Environment.NewLine, itemList)}{Environment.NewLine}```{Environment.NewLine}" +
                        $"Run `{_config.Prefix}config <item> to view information about an item!");
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
                case ConfigItemType.String:
                case ConfigItemType.Char:
                case ConfigItemType.Boolean:
                case ConfigItemType.Integer:
                case ConfigItemType.Double:
                    await ReplyAsync(
                        $"**{configItemKey}** - {_configDescriber.TypeToString(configItem.Type)} - {_configDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Current value: `{ConfigHelper.GetValue<Config>(_config, configItemKey)}`{Environment.NewLine}" +
                        $"Run `{_config.Prefix}config {configItemKey} <value>` to set this value. {nullableString}",
                        allowedMentions: AllowedMentions.None);
                    break;
                case ConfigItemType.User:
                    await ReplyAsync(
                        $"**{configItemKey}** - {_configDescriber.TypeToString(configItem.Type)} - {_configDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Current value: <@{ConfigHelper.GetValue<Config>(_config, configItemKey)}>{Environment.NewLine}" +
                        $"Run `{_config.Prefix}config {configItemKey} <value>` to set this value. {nullableString}",
                        allowedMentions: AllowedMentions.None);
                    break;
                case ConfigItemType.Role:
                    await ReplyAsync(
                        $"**{configItemKey}** - {_configDescriber.TypeToString(configItem.Type)} - {_configDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Current value: <@&{ConfigHelper.GetValue<Config>(_config, configItemKey)}>{Environment.NewLine}" +
                        $"Run `{_config.Prefix}config {configItemKey} <value>` to set this value. {nullableString}",
                        allowedMentions: AllowedMentions.None);
                    break;
                case ConfigItemType.Channel:
                    await ReplyAsync(
                        $"**{configItemKey}** - {_configDescriber.TypeToString(configItem.Type)} - {_configDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Current value: <#{ConfigHelper.GetValue<Config>(_config, configItemKey)}>{Environment.NewLine}" +
                        $"Run `{_config.Prefix}config {configItemKey} <value>` to set this value. {nullableString}",
                        allowedMentions: AllowedMentions.None);
                    break;
                case ConfigItemType.StringList:
                case ConfigItemType.CharList:
                case ConfigItemType.BooleanList:
                case ConfigItemType.IntegerList:
                case ConfigItemType.DoubleList:
                case ConfigItemType.UserList:
                case ConfigItemType.RoleList:
                case ConfigItemType.ChannelList:
                    await ReplyAsync(
                        $"**{configItemKey}** - {_configDescriber.TypeToString(configItem.Type)} - {_configDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Run `{_config.Prefix}config {configItemKey} list` to view the contents of this list.{Environment.NewLine}" +
                        $"Run `{_config.Prefix}config {configItemKey} add <value>` to add a value to this list. {nullableString}{Environment.NewLine}" +
                        $"Run `{_config.Prefix}config {configItemKey} remove <value>` to remove a value from this list. {nullableString}",
                        allowedMentions: AllowedMentions.None);
                    break;
                case ConfigItemType.StringDictionary:
                //case SettingsItemType.CharDictionary: // Note: Implement when needed
                case ConfigItemType.BooleanDictionary:
                //case SettingsItemType.IntegerDictionary: // Note: Implement when needed
                //case SettingsItemType.DoubleDictionary: // Note: Implement when needed
                //case SettingsItemType.UserDictionary: // Note: Implement when needed
                //case SettingsItemType.RoleDictionary: // Note: Implement when needed
                //case SettingsItemType.ChannelDictionary: // Note: Implement when needed
                case ConfigItemType.StringListDictionary:
                    //case SettingsItemType.CharListDictionary: // Note: Implement when needed
                    //case SettingsItemType.BooleanListDictionary: // Note: Implement when needed
                    //case SettingsItemType.IntegerListDictionary: // Note: Implement when needed
                    //case SettingsItemType.DoubleListDictionary: // Note: Implement when needed
                    //case SettingsItemType.UserListDictionary: // Note: Implement when needed
                    //case SettingsItemType.RoleListDictionary: // Note: Implement when needed
                    //case SettingsItemType.ChannelListDictionary: // Note: Implement when needed
                    await ReplyAsync(
                        $"**{configItemKey}** - {_configDescriber.TypeToString(configItem.Type)} - {_configDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Run `{_config.Prefix}config {configItemKey} list` to view a list of keys in this map.{Environment.NewLine}" +
                        $"Run `{_config.Prefix}config {configItemKey} create <key>` to create a new key in this map.{Environment.NewLine}" +
                        $"Run `{_config.Prefix}config {configItemKey} delete <key>` to delete a key from this map.{Environment.NewLine}" +
                        $"Run `{_config.Prefix}config {configItemKey} <key>` to view information about a key in this map.",
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
            if (_configDescriber.TypeIsValue(configItem.Type))
            {
                switch (configItem.Type)
                {
                    case ConfigItemType.String:
                        if (configItem.Nullable && value == "<nothing>") value = null;

                        var resultString =
                            await ConfigHelper.SetStringValue<Config>(_config, configItemKey, value);
                        await ReplyAsync($"I've set `{configItemKey}` to the following content: {resultString}",
                            allowedMentions: AllowedMentions.None);
                        break;
                    case ConfigItemType.Char:
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
                                await ConfigHelper.SetCharValue<Config>(_config, configItemKey, output);
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
                    case ConfigItemType.Boolean:
                        if (configItem.Nullable && value == "<nothing>") value = null;

                        try
                        {
                            var resultBoolean =
                                await ConfigHelper.SetBooleanValue<Config>(_config, configItemKey, value);
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
                    case ConfigItemType.Integer:
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
                                await ConfigHelper.SetIntValue<Config>(_config, configItemKey, output);
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
                    case ConfigItemType.Double:
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
                                await ConfigHelper.SetDoubleValue<Config>(_config, configItemKey, output);
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
                    case ConfigItemType.User:
                        if (configItem.Nullable && value == "<nothing>") value = null;
                        try
                        {
                            var result =
                                await ConfigHelper.SetUserValue<Config>(_config, configItemKey, value,
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
                    case ConfigItemType.Role:
                        if (configItem.Nullable && value == "<nothing>") value = null;
                        try
                        {
                            var result =
                                await ConfigHelper.SetRoleValue<Config>(_config, configItemKey, value,
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
                    case ConfigItemType.Channel:
                        if (configItem.Nullable && value == "<nothing>") value = null;
                        try
                        {
                            var result =
                                await ConfigHelper.SetChannelValue<Config>(_config, configItemKey, value,
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
            else if (_configDescriber.TypeIsList(configItem.Type))
            {
                var action = value.Split(' ')[0].ToLower();
                value = value.Replace(action + " ", "");

                if (action == "list")
                    switch (configItem.Type)
                    {
                        case ConfigItemType.StringList:
                            var stringList = ConfigHelper.GetStringList<Config>(_config, configItemKey);

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
                        case ConfigItemType.CharList:
                            var charList = ConfigHelper.GetCharList<Config>(_config, configItemKey);

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
                        case ConfigItemType.BooleanList:
                            var booleanList = ConfigHelper.GetBooleanList<Config>(_config, configItemKey);

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
                        case ConfigItemType.IntegerList:
                            var intList = ConfigHelper.GetIntList<Config>(_config, configItemKey);

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
                        case ConfigItemType.DoubleList:
                            var doubleList = ConfigHelper.GetDoubleList<Config>(_config, configItemKey);

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
                        case ConfigItemType.UserList:
                            var userList = ConfigHelper.GetUserList<Config>(_config, configItemKey, Context);

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
                        case ConfigItemType.RoleList:
                            var roleList = ConfigHelper.GetRoleList<Config>(_config, configItemKey, Context);

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
                        case ConfigItemType.ChannelList:
                            var channelList =
                                ConfigHelper.GetChannelList<Config>(_config, configItemKey, Context);

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
                        case ConfigItemType.StringList:
                            try
                            {
                                var output =
                                    await ConfigHelper.AddToStringList<Config>(_config, configItemKey, value);

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
                        case ConfigItemType.CharList:
                            try
                            {
                                if (!char.TryParse(value, out var charValue)) throw new FormatException();

                                var output =
                                    await ConfigHelper.AddToCharList<Config>(_config, configItemKey,
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
                        case ConfigItemType.BooleanList:
                            try
                            {
                                var output =
                                    await ConfigHelper.AddToBooleanList<Config>(_config, configItemKey,
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
                        case ConfigItemType.IntegerList:
                            try
                            {
                                if (!int.TryParse(value, out var intValue)) throw new FormatException();

                                var output =
                                    await ConfigHelper.AddToIntList<Config>(_config, configItemKey, intValue);

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
                        case ConfigItemType.DoubleList:
                            try
                            {
                                if (!double.TryParse(value, out var doubleValue)) throw new FormatException();

                                var output =
                                    await ConfigHelper.AddToDoubleList<Config>(_config, configItemKey,
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
                        case ConfigItemType.UserList:
                            try
                            {
                                var output =
                                    await ConfigHelper.AddToUserList<Config>(_config, configItemKey, value,
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
                        case ConfigItemType.RoleList:
                            try
                            {
                                var output =
                                    await ConfigHelper.AddToRoleList<Config>(_config, configItemKey, value,
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
                        case ConfigItemType.ChannelList:
                            try
                            {
                                var output =
                                    await ConfigHelper.AddToChannelList<Config>(_config, configItemKey, value,
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
                        case ConfigItemType.StringList:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromStringList<Config>(_config, configItemKey,
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
                        case ConfigItemType.CharList:
                            try
                            {
                                if (!char.TryParse(value, out var charValue)) throw new FormatException();

                                var output =
                                    await ConfigHelper.AddToCharList<Config>(_config, configItemKey,
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
                        case ConfigItemType.BooleanList:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromBooleanList<Config>(_config, configItemKey,
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
                        case ConfigItemType.IntegerList:
                            try
                            {
                                if (!int.TryParse(value, out var intValue)) throw new FormatException();

                                var output =
                                    await ConfigHelper.RemoveFromIntList<Config>(_config, configItemKey,
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
                        case ConfigItemType.DoubleList:
                            try
                            {
                                if (!double.TryParse(value, out var doubleValue)) throw new FormatException();

                                var output =
                                    await ConfigHelper.RemoveFromDoubleList<Config>(_config, configItemKey,
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
                        case ConfigItemType.UserList:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromUserList<Config>(_config, configItemKey,
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
                        case ConfigItemType.RoleList:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromRoleList<Config>(_config, configItemKey,
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
                        case ConfigItemType.ChannelList:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromChannelList<Config>(_config, configItemKey,
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
            else if (_configDescriber.TypeIsDictionaryValue(configItem.Type))
            {
                var action = value.Split(' ')[0].ToLower();
                value = value.Replace(action + " ", "");

                if (action == "list")
                {
                    switch (configItem.Type)
                    {
                        case ConfigItemType.StringDictionary:
                            try
                            {
                                var keys = new List<string>();
                                if (configItem.Nullable)
                                    keys = ConfigHelper
                                        .GetNullableStringDictionary<Config>(_config, configItemKey, Context)
                                        .Keys.ToList();
                                else
                                    keys = ConfigHelper
                                        .GetStringDictionary<Config>(_config, configItemKey, Context).Keys
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
                        case ConfigItemType.BooleanDictionary:
                            try
                            {
                                var keys = ConfigHelper
                                    .GetBooleanDictionary<Config>(_config, configItemKey, Context).Keys
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
                        case ConfigItemType.StringDictionary:
                            try
                            {
                                if (configItem.Nullable)
                                    await ConfigHelper.CreateNullableStringDictionaryKey<Config>(_config,
                                        configItemKey, value, Context);
                                else
                                    await ConfigHelper.CreateStringDictionaryKey<Config>(_config,
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
                        case ConfigItemType.BooleanDictionary:
                            try
                            {
                                await ConfigHelper.CreateBooleanDictionaryKey<Config>(_config, configItemKey,
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
                        case ConfigItemType.StringDictionary:
                            try
                            {
                                if (configItem.Nullable)
                                    await ConfigHelper.RemoveNullableStringDictionaryKey<Config>(_config,
                                        configItemKey, value, Context);
                                else
                                    await ConfigHelper.RemoveStringDictionaryKey<Config>(_config,
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
                        case ConfigItemType.BooleanDictionary:
                            try
                            {
                                await ConfigHelper.RemoveBooleanDictionaryKey<Config>(_config, configItemKey,
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
                            case ConfigItemType.StringDictionary:
                                try
                                {
                                    var contents = "";

                                    if (configItem.Nullable)
                                        contents = ConfigHelper.GetNullableStringDictionaryValue<Config>(
                                            _config,
                                            configItemKey, key, Context);
                                    else
                                        contents = (string?)ConfigHelper.GetStringDictionaryValue<Config>(
                                            _config,
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
                            case ConfigItemType.BooleanDictionary:
                                try
                                {
                                    var contents = ConfigHelper.GetBooleanDictionaryValue<Config>(_config,
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
                            case ConfigItemType.StringDictionary:
                                try
                                {
                                    if (configItem.Nullable)
                                    {
                                        if (value == "<nothing>") value = null;

                                        var result =
                                            await ConfigHelper.SetNullableStringDictionaryValue<Config>(
                                                _config, configItemKey, key, value, Context);

                                        await Context.Message.ReplyAsync(
                                            $"I've set `{key}` to the following content: {result}",
                                            allowedMentions: AllowedMentions.None);
                                    }
                                    else
                                    {
                                        var result = await ConfigHelper.SetStringDictionaryValue<Config>(
                                            _config, configItemKey, key, value, Context);

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
                            case ConfigItemType.BooleanDictionary:
                                try
                                {
                                    var result = await ConfigHelper.SetBooleanDictionaryValue<Config>(
                                        _config, configItemKey, key, value, Context);

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
                                         $"Run `{_config.Prefix}config {configItemKey} {key} get` to get the value in this map key.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}config {configItemKey} {key} set <value>` to set the value for this map key{nullableString}.",
                            allowedMentions: AllowedMentions.None);
                    }
                }
            }
            else if (_configDescriber.TypeIsDictionaryList(configItem.Type))
            {
                var action = value.Split(' ')[0].ToLower();
                value = value.Replace(action + " ", "");

                if (action == "list")
                {
                    switch (configItem.Type)
                    {
                        case ConfigItemType.StringListDictionary:
                            try
                            {
                                var keys = ConfigHelper
                                    .GetStringListDictionary<Config>(_config, configItemKey, Context).Keys
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
                        case ConfigItemType.StringListDictionary:
                            try
                            {
                                await ConfigHelper.CreateStringListDictionaryKey<Config>(_config,
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
                        case ConfigItemType.StringListDictionary:
                            try
                            {
                                await ConfigHelper.RemoveStringListDictionaryKey<Config>(_config,
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
                            case ConfigItemType.StringListDictionary:
                                var stringList =
                                    ConfigHelper.GetStringListDictionaryValue<Config>(_config, configItemKey,
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
                            case ConfigItemType.StringListDictionary:
                                try
                                {
                                    var output =
                                        await ConfigHelper.AddToStringListDictionaryValue<Config>(_config,
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
                            case ConfigItemType.StringListDictionary:
                                try
                                {
                                    var output =
                                        await ConfigHelper.RemoveFromStringListDictionaryValue<Config>(
                                            _config, configItemKey, key, value, Context);

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
                                         $"Run `{_config.Prefix}config {configItemKey} {key} list` to view a list of values in this map key.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}config {configItemKey} {key} add <value>` to add a value to this map key{nullableString}.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}config {configItemKey} {key} remove <value>` to remove a value from this map key{nullableString}.",
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