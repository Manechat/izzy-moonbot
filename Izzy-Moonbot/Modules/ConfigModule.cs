using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Describers;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Modules;

[Summary("Configuration management related commands.")]
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
    [Parameter("key", ParameterType.String, "The config item to get/modify. This is case sensitive.")]
    [Parameter("[...]", ParameterType.String, "...", true)]
    public async Task ConfigCommandAsync(
        [Summary("The item to get/modify.")] string configItemKey = "",
        [Summary("")] [Remainder] string? value = "")
    {
        await TestableConfigCommandAsync(
            new SocketCommandContextAdapter(Context),
            _config,
            _configDescriber,
            configItemKey,
            value
        );
    }

    public static async Task TestableConfigCommandAsync(
        IIzzyContext context,
        Config config,
        ConfigDescriber configDescriber,
        string configItemKey = "",
        string? value = "")
    {
        if (configItemKey == "")
        {
            await context.Message.ReplyAsync($"Hii!! Here's now to use the config command!{Environment.NewLine}" +
                                             $"Run `{config.Prefix}config <category>` to list the config items in a category.{Environment.NewLine}" +
                                             $"Run `{config.Prefix}config <item>` to view information about an item.{Environment.NewLine}{Environment.NewLine}" +
                                             $"Here's a list of all possible categories.{Environment.NewLine}```{Environment.NewLine}" +
                                             $"core - Config items which dictate core settings (often global).{Environment.NewLine}" +
                                             $"moderation - Config items which dictate moderation settings.{Environment.NewLine}" +
                                             $"debug - Debug config items used to debug Izzy.{Environment.NewLine}" +
                                             $"user - Config items regarding users.{Environment.NewLine}" +
                                             $"filter - Config items regarding the filter.{Environment.NewLine}" +
                                             $"spam - Config items regarding spam pressure.{Environment.NewLine}" +
                                             $"raid - Config items regarding antiraid.{Environment.NewLine}```{Environment.NewLine}{Environment.NewLine}" +
                                             $"ℹ  **See also: `{config.Prefix}help`. Run `{config.Prefix}help` for more information.**");

            return;
        }

        var configItem = configDescriber.GetItem(configItemKey);

        if (configItem == null)
        {
            // Config item not found, but could be a category.
            var configCategory = configDescriber.StringToCategory(configItemKey);
            if (configCategory.HasValue)
            {
                // it's not null we literally check above u stupid piece of code
                var category = configCategory.Value;

                var itemList = configDescriber.GetSettableConfigItemsByCategory(category);

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
                        $"Hii!! Here's a list of all the config items I could find in the {configDescriber.CategoryToString(category)} category!",
                        $"Run `{config.Prefix}config <item>` to view information about an item! Please note that config items are *case sensitive*."
                    };

                    var paginationMessage = new PaginationHelper(context, pages.ToArray(), staticParts);
                }
                else
                {
                    await context.Channel.SendMessageAsync(
                        $"Hii!! Here's a list of all the config items I could find in the {configDescriber.CategoryToString(category)} category!" +
                        $"{Environment.NewLine}```{Environment.NewLine}{string.Join(Environment.NewLine, itemList)}{Environment.NewLine}```{Environment.NewLine}" +
                        $"Run `{config.Prefix}config <item>` to view information about an item! Please note that config items are *case sensitive*.");
                }

                return;
            }

            await context.Channel.SendMessageAsync($"Sorry, I couldn't find a config value or category called `{configItemKey}`!");
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
                    await context.Channel.SendMessageAsync(
                        $"**{configItemKey}** - {configDescriber.TypeToString(configItem.Type)} - {configDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Current value: `{ConfigHelper.GetValue(config, configItemKey)}`{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} <value>` to set this value. {nullableString}",
                        allowedMentions: AllowedMentions.None);
                    break;
                case ConfigItemType.Enum:
                    // Figure out what its values are.
                    var enumValue = ConfigHelper.GetValue(config, configItemKey) as Enum;
                    var enumType = enumValue.GetType();
                    var possibleEnumNames = enumType.GetEnumNames().Select(s => $"`{s}`").ToArray();
                    
                    await context.Channel.SendMessageAsync(
                        $"**{configItemKey}** - {configDescriber.TypeToString(configItem.Type)} - {configDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Possible values are: {string.Join(", ", possibleEnumNames)}{Environment.NewLine}" +
                        $"Current value: `{enumType.GetEnumName(enumValue)}`{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} <value>` to set this value. {nullableString}",
                        allowedMentions: AllowedMentions.None);
                    break;
                case ConfigItemType.User:
                    await context.Channel.SendMessageAsync(
                        $"**{configItemKey}** - {configDescriber.TypeToString(configItem.Type)} - {configDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Current value: <@{ConfigHelper.GetValue(config, configItemKey)}>{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} <value>` to set this value. {nullableString}",
                        allowedMentions: AllowedMentions.None);
                    break;
                case ConfigItemType.Role:
                    await context.Channel.SendMessageAsync(
                        $"**{configItemKey}** - {configDescriber.TypeToString(configItem.Type)} - {configDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Current value: <@&{ConfigHelper.GetValue(config, configItemKey)}>{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} <value>` to set this value. {nullableString}",
                        allowedMentions: AllowedMentions.None);
                    break;
                case ConfigItemType.Channel:
                    await context.Channel.SendMessageAsync(
                        $"**{configItemKey}** - {configDescriber.TypeToString(configItem.Type)} - {configDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Current value: <#{ConfigHelper.GetValue(config, configItemKey)}>{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} <value>` to set this value. {nullableString}",
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
                    await context.Channel.SendMessageAsync(
                        $"**{configItemKey}** - {configDescriber.TypeToString(configItem.Type)} - {configDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} list` to view the contents of this list.{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} add <value>` to add a value to this list. {nullableString}{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} remove <value>` to remove a value from this list. {nullableString}",
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
                    await context.Channel.SendMessageAsync(
                        $"**{configItemKey}** - {configDescriber.TypeToString(configItem.Type)} - {configDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} list` to view a list of keys in this map.{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} get <key>` to get the current value of a key in this map.{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} set <key> <value>` to set a key to a value in this map, creating the key if need be.{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} delete <key>` to delete a key from this map.",
                        allowedMentions: AllowedMentions.None);
                    break;
                case ConfigItemType.StringListDictionary:
                    //case SettingsItemType.CharListDictionary: // Note: Implement when needed
                    //case SettingsItemType.BooleanListDictionary: // Note: Implement when needed
                    //case SettingsItemType.IntegerListDictionary: // Note: Implement when needed
                    //case SettingsItemType.DoubleListDictionary: // Note: Implement when needed
                    //case SettingsItemType.UserListDictionary: // Note: Implement when needed
                    //case SettingsItemType.RoleListDictionary: // Note: Implement when needed
                    //case SettingsItemType.ChannelListDictionary: // Note: Implement when needed
                    await context.Channel.SendMessageAsync(
                        $"**{configItemKey}** - {configDescriber.TypeToString(configItem.Type)} - {configDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} list` to view a list of keys in this map.{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} get <key>` to get the values of a key in this map.{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} add <key> <value>` to add a value to a key in this map, creating the key if need be.{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} deleteitem <key> <value>` to remove a value from a key from this map.{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} deletelist <key>` to delete a key from this map.",
                        allowedMentions: AllowedMentions.None);
                    break;
                default:
                    await context.Channel.SendMessageAsync("I seem to have encountered a setting type that I do not know about.");
                    break;
            }
        }
        else
        {
            // value provided
            if (configDescriber.TypeIsValue(configItem.Type))
            {
                switch (configItem.Type)
                {
                    case ConfigItemType.String:
                        if (configItem.Nullable && value == "<nothing>") value = null;

                        var resultString =
                            await ConfigHelper.SetStringValue(config, configItemKey, value);
                        await context.Channel.SendMessageAsync($"I've set `{configItemKey}` to the following content: {resultString}",
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
                                await ConfigHelper.SetCharValue(config, configItemKey, output);
                            await context.Channel.SendMessageAsync($"I've set `{configItemKey}` to the following content: {resultChar}",
                                allowedMentions: AllowedMentions.None);
                        }
                        catch (FormatException)
                        {
                            await context.Channel.SendMessageAsync(
                                $"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a character. Please try again.",
                                allowedMentions: AllowedMentions.None);
                        }

                        break;
                    case ConfigItemType.Boolean:
                        if (configItem.Nullable && value == "<nothing>") value = null;

                        try
                        {
                            var resultBoolean =
                                await ConfigHelper.SetBooleanValue(config, configItemKey, value);
                            await context.Channel.SendMessageAsync($"I've set `{configItemKey}` to the following content: {resultBoolean}",
                                allowedMentions: AllowedMentions.None);
                        }
                        catch (FormatException)
                        {
                            await context.Channel.SendMessageAsync(
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
                                await ConfigHelper.SetIntValue(config, configItemKey, output);
                            await context.Channel.SendMessageAsync($"I've set `{configItemKey}` to the following content: {resultInteger}",
                                allowedMentions: AllowedMentions.None);
                        }
                        catch (FormatException)
                        {
                            await context.Channel.SendMessageAsync(
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
                                await ConfigHelper.SetDoubleValue(config, configItemKey, output);
                            await context.Channel.SendMessageAsync($"I've set `{configItemKey}` to the following content: {resultDouble}",
                                allowedMentions: AllowedMentions.None);
                        }
                        catch (FormatException)
                        {
                            await context.Channel.SendMessageAsync(
                                $"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a double. Please try again.",
                                allowedMentions: AllowedMentions.None);
                        }

                        break;
                    case ConfigItemType.Enum:
                        if (configItem.Nullable && value == "<nothing>") value = null;

                        var enumValue = ConfigHelper.GetValue(config, configItemKey) as Enum;
                        var enumType = enumValue.GetType();

                        try
                        {
                            Enum? output = null;
                            if (value != null)
                            {
                                if (!Enum.TryParse(enumType, value, out var res))
                                    throw new FormatException(); // Trip "invalid content" catch below.
                                output = res as Enum;
                            }

                            var resultDouble =
                                await ConfigHelper.SetEnumValue(config, configItemKey, output);
                            await context.Channel.SendMessageAsync($"I've set `{configItemKey}` to the following content: {resultDouble}",
                                allowedMentions: AllowedMentions.None);
                        }
                        catch (FormatException)
                        {
                            await context.Channel.SendMessageAsync(
                                $"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into this specific enum type ({enumType.Name}). Please try again.",
                                allowedMentions: AllowedMentions.None);
                        }
                        break;
                    case ConfigItemType.User:
                        if (configItem.Nullable && value == "<nothing>") value = null;
                        try
                        {
                            var result =
                                await ConfigHelper.SetUserValue(config, configItemKey, value,
                                    context);
                            var response = "`null`";
                            if (result != null) response = $"<@{result.Id}>";
                            await context.Channel.SendMessageAsync($"I've set `{configItemKey}` to the following content: {response}");
                        }
                        catch (MemberAccessException)
                        {
                            await context.Channel.SendMessageAsync(
                                $"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a user. Please try again.",
                                allowedMentions: AllowedMentions.None);
                        }

                        break;
                    case ConfigItemType.Role:
                        if (configItem.Nullable && value == "<nothing>") value = null;
                        try
                        {
                            var result =
                                await ConfigHelper.SetRoleValue(config, configItemKey, value,
                                    context);
                            var response = "`null`";
                            if (result != null) response = $"<@&{result.Id}>";
                            await context.Channel.SendMessageAsync($"I've set `{configItemKey}` to the following content: {response}",
                                allowedMentions: AllowedMentions.None);
                        }
                        catch (MemberAccessException)
                        {
                            await context.Channel.SendMessageAsync(
                                $"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a role. Please try again.",
                                allowedMentions: AllowedMentions.None);
                        }

                        break;
                    case ConfigItemType.Channel:
                        if (configItem.Nullable && value == "<nothing>") value = null;
                        try
                        {
                            var result =
                                await ConfigHelper.SetChannelValue(config, configItemKey, value,
                                    context);
                            var response = "`null`";
                            if (result != null) response = $"<#{result.Id}>";
                            await context.Channel.SendMessageAsync($"I've set `{configItemKey}` to the following content: {response}",
                                allowedMentions: AllowedMentions.None);
                        }
                        catch (MemberAccessException)
                        {
                            await context.Channel.SendMessageAsync(
                                $"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a channel. Please try again.",
                                allowedMentions: AllowedMentions.None);
                        }

                        break;
                    default:
                        await context.Channel.SendMessageAsync("I seem to have encountered a setting type that I do not know about.");
                        break;
                }
            }
            else if (configDescriber.TypeIsList(configItem.Type))
            {
                var action = value.Split(' ')[0].ToLower();
                value = value.Replace(action + " ", "");

                if (action == "list")
                    switch (configItem.Type)
                    {
                        case ConfigItemType.StringList:
                            var stringList = ConfigHelper.GetStringList(config, configItemKey);

                            if (stringList == null)
                            {
                                await context.Channel.SendMessageAsync("Somehow, the entire list is null.");
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

                                var paginationMessage = new PaginationHelper(context, pages.ToArray(), staticParts);
                            }
                            else
                            {
                                await context.Channel.SendMessageAsync(
                                    $"**{configItemKey}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", stringList)}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }

                            break;
                        case ConfigItemType.CharList:
                            var charList = ConfigHelper.GetCharList(config, configItemKey);

                            if (charList == null)
                            {
                                await context.Channel.SendMessageAsync("Somehow, the entire list is null.");
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

                                var paginationMessage = new PaginationHelper(context, pages.ToArray(), staticParts);
                            }
                            else
                            {
                                await context.Channel.SendMessageAsync(
                                    $"**{configItemKey}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", charList)}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }

                            break;
                        case ConfigItemType.BooleanList:
                            var booleanList = ConfigHelper.GetBooleanList(config, configItemKey);

                            if (booleanList == null)
                            {
                                await context.Channel.SendMessageAsync("Somehow, the entire list is null.");
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

                                var paginationMessage = new PaginationHelper(context, pages.ToArray(), staticParts);
                            }
                            else
                            {
                                await context.Channel.SendMessageAsync(
                                    $"**{configItemKey}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", booleanList)}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }

                            break;
                        case ConfigItemType.IntegerList:
                            var intList = ConfigHelper.GetIntList(config, configItemKey);

                            if (intList == null)
                            {
                                await context.Channel.SendMessageAsync("Somehow, the entire list is null.");
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

                                var paginationMessage = new PaginationHelper(context, pages.ToArray(), staticParts);
                            }
                            else
                            {
                                await context.Channel.SendMessageAsync(
                                    $"**{configItemKey}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", intList)}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }

                            break;
                        case ConfigItemType.DoubleList:
                            var doubleList = ConfigHelper.GetDoubleList(config, configItemKey);

                            if (doubleList == null)
                            {
                                await context.Channel.SendMessageAsync("Somehow, the entire list is null.");
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

                                var paginationMessage = new PaginationHelper(context, pages.ToArray(), staticParts);
                            }
                            else
                            {
                                await context.Channel.SendMessageAsync(
                                    $"**{configItemKey}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", doubleList)}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }

                            break;
                        case ConfigItemType.UserList:
                            var userList = ConfigHelper.GetUserList(config, configItemKey, context);

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
                                    new PaginationHelper(context, pages.ToArray(), staticParts, 0, false);
                            }
                            else
                            {
                                await context.Channel.SendMessageAsync(
                                    $"**{configItemKey}** contains the following values:{Environment.NewLine}{string.Join(", ", userMentionList)}");
                            }

                            break;
                        case ConfigItemType.RoleList:
                            var roleList = ConfigHelper.GetRoleList(config, configItemKey, context);

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

                                var paginationMessage = new PaginationHelper(context, pages.ToArray(),
                                    staticParts, 0, false, AllowedMentions.None);
                            }
                            else
                            {
                                await context.Channel.SendMessageAsync(
                                    $"**{configItemKey}** contains the following values:{Environment.NewLine}{string.Join(", ", roleMentionList)}",
                                    allowedMentions: AllowedMentions.None);
                            }

                            break;
                        case ConfigItemType.ChannelList:
                            var channelList =
                                ConfigHelper.GetChannelList(config, configItemKey, context);

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
                                    new PaginationHelper(context, pages.ToArray(), staticParts, 0, false);
                            }
                            else
                            {
                                await context.Channel.SendMessageAsync(
                                    $"**{configItemKey}** contains the following values:{Environment.NewLine}{string.Join(", ", channelMentionList)}",
                                    allowedMentions: AllowedMentions.None);
                            }

                            break;
                        default:
                            await context.Channel.SendMessageAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                else if (action == "add")
                    switch (configItem.Type)
                    {
                        case ConfigItemType.StringList:
                            try
                            {
                                var output =
                                    await ConfigHelper.AddToStringList(config, configItemKey, value);

                                await context.Channel.SendMessageAsync(
                                    $"I added the following content to the `{configItemKey}` string list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add your content to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case ConfigItemType.CharList:
                            try
                            {
                                if (!char.TryParse(value, out var charValue)) throw new FormatException();

                                var output =
                                    await ConfigHelper.AddToCharList(config, configItemKey,
                                        charValue);

                                await context.Channel.SendMessageAsync(
                                    $"I added the following content to the `{configItemKey}` character list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into a single character. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case ConfigItemType.BooleanList:
                            try
                            {
                                var output =
                                    await ConfigHelper.AddToBooleanList(config, configItemKey,
                                        value);

                                await context.Channel.SendMessageAsync(
                                    $"I added the following content to the `{configItemKey}` boolean list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into a boolean. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case ConfigItemType.IntegerList:
                            try
                            {
                                if (!int.TryParse(value, out var intValue)) throw new FormatException();

                                var output =
                                    await ConfigHelper.AddToIntList(config, configItemKey, intValue);

                                await context.Channel.SendMessageAsync(
                                    $"I added the following content to the `{configItemKey}` integer list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into an integer. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case ConfigItemType.DoubleList:
                            try
                            {
                                if (!double.TryParse(value, out var doubleValue)) throw new FormatException();

                                var output =
                                    await ConfigHelper.AddToDoubleList(config, configItemKey,
                                        doubleValue);

                                await context.Channel.SendMessageAsync(
                                    $"I added the following content to the `{configItemKey}` double list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into a double. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case ConfigItemType.UserList:
                            try
                            {
                                var output =
                                    await ConfigHelper.AddToUserList(config, configItemKey, value,
                                        context);

                                await context.Channel.SendMessageAsync(
                                    $"I added the following content to the `{configItemKey}` user list:{Environment.NewLine}{output}");
                            }
                            catch (MemberAccessException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into a user I know about. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add the user you provided to the `{configItemKey}` list because the user is already in that list.");
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case ConfigItemType.RoleList:
                            try
                            {
                                var output =
                                    await ConfigHelper.AddToRoleList(config, configItemKey, value,
                                        context);

                                await context.Channel.SendMessageAsync(
                                    $"I added the following content to the `{configItemKey}` role list:{Environment.NewLine}{output}");
                            }
                            catch (MemberAccessException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into a role I know about. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add the role you provided to the `{configItemKey}` list because the role is already in that list.");
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case ConfigItemType.ChannelList:
                            try
                            {
                                var output =
                                    await ConfigHelper.AddToChannelList(config, configItemKey, value,
                                        context);

                                await context.Channel.SendMessageAsync(
                                    $"I added the following content to the `{configItemKey}` channel list:{Environment.NewLine}{output}");
                            }
                            catch (MemberAccessException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into a channel I know about. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add the channel you provided to the `{configItemKey}` list because the channel is already in that list.");
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        default:
                            await context.Channel.SendMessageAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                else if (action == "remove")
                    switch (configItem.Type)
                    {
                        case ConfigItemType.StringList:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromStringList(config, configItemKey,
                                        value);

                                await context.Channel.SendMessageAsync(
                                    $"I removed the following content from the `{configItemKey}` string list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the content isn't in that list to begin with.");
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case ConfigItemType.CharList:
                            try
                            {
                                if (!char.TryParse(value, out var charValue)) throw new FormatException();

                                var output =
                                    await ConfigHelper.AddToCharList(config, configItemKey,
                                        charValue);

                                await context.Channel.SendMessageAsync(
                                    $"I removed the following content from the `{configItemKey}` character list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a character. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the content isn't in that list to begin with.");
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case ConfigItemType.BooleanList:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromBooleanList(config, configItemKey,
                                        value);

                                await context.Channel.SendMessageAsync(
                                    $"I removed the following content from the `{configItemKey}` boolean list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a boolean. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the content isn't in that list to begin with.");
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case ConfigItemType.IntegerList:
                            try
                            {
                                if (!int.TryParse(value, out var intValue)) throw new FormatException();

                                var output =
                                    await ConfigHelper.RemoveFromIntList(config, configItemKey,
                                        intValue);

                                await context.Channel.SendMessageAsync(
                                    $"I removed the following content from the `{configItemKey}` integer list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a integer. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the content isn't in that list to begin with.");
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case ConfigItemType.DoubleList:
                            try
                            {
                                if (!double.TryParse(value, out var doubleValue)) throw new FormatException();

                                var output =
                                    await ConfigHelper.RemoveFromDoubleList(config, configItemKey,
                                        doubleValue);

                                await context.Channel.SendMessageAsync(
                                    $"I removed the following content from the `{configItemKey}` double list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a double. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the content isn't in that list to begin with.");
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case ConfigItemType.UserList:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromUserList(config, configItemKey,
                                        value, context);

                                await context.Channel.SendMessageAsync(
                                    $"I removed the following content from the `{configItemKey}` user list:{Environment.NewLine}{output}");
                            }
                            catch (MemberAccessException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a user I know about. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the user you provided from the `{configItemKey}` list because the user isn't in that list to begin with.");
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case ConfigItemType.RoleList:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromRoleList(config, configItemKey,
                                        value, context);

                                await context.Channel.SendMessageAsync(
                                    $"I removed the following content from the `{configItemKey}` role list:{Environment.NewLine}{output}");
                            }
                            catch (MemberAccessException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a role I know about. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the role you provided from the `{configItemKey}` list because the role isn't in that list to begin with.");
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case ConfigItemType.ChannelList:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromChannelList(config, configItemKey,
                                        value, context);

                                await context.Channel.SendMessageAsync(
                                    $"I removed the following content from the `{configItemKey}` channel list:{Environment.NewLine}{output}");
                            }
                            catch (MemberAccessException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a channel I know about. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the channel you provided from the `{configItemKey}` list because the channel isn't in that list to begin with.");
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        default:
                            await context.Channel.SendMessageAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
            }
            else if (configDescriber.TypeIsDictionaryValue(configItem.Type))
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
                                var values = new List<string?>();
                                if (configItem.Nullable)
                                {
                                    keys = ConfigHelper
                                        .GetNullableStringDictionary(config, configItemKey)
                                        .Keys.ToList();
                                    values = ConfigHelper
                                        .GetNullableStringDictionary(config, configItemKey)
                                        .Values.ToList();
                                }
                                else
                                {
                                    keys = ConfigHelper
                                        .GetStringDictionary(config, configItemKey)
                                        .Keys.ToList();
                                    values = ConfigHelper
                                        .GetStringDictionary(config, configItemKey)
                                        .Values.Select(t => (string?)t).ToList();
                                }

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

                                        pages[pageNumber] += $"{keys[i]} = {values[i]}{Environment.NewLine}";
                                    }

                                    string[] staticParts =
                                    {
                                        $"**{configItemKey}** contains the following keys:",
                                        ""
                                    };

                                    var paginationMessage = new PaginationHelper(context, pages.ToArray(), staticParts);
                                }
                                else
                                {
                                    var listString = keys.Select((t, i) => $"{t} = {values[i]}").ToList();
                                    
                                    await context.Channel.SendMessageAsync(
                                        $"**{configItemKey}** contains the following keys:{Environment.NewLine}```{Environment.NewLine}{string.Join($"{Environment.NewLine}", listString)}{Environment.NewLine}```");
                                }
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't list the keys in the `{configItemKey}` map? {ex.Message}");
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't list the keys in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case ConfigItemType.BooleanDictionary:
                            try
                            {
                                var keys = ConfigHelper
                                    .GetBooleanDictionary(config, configItemKey).Keys
                                    .ToList();

                                var values = ConfigHelper
                                    .GetBooleanDictionary(config, configItemKey).Values
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

                                        pages[pageNumber] += $"{keys[i]} = {values[i]}{Environment.NewLine}";
                                    }

                                    string[] staticParts =
                                    {
                                        $"**{configItemKey}** contains the following keys:",
                                        ""
                                    };

                                    var paginationMessage = new PaginationHelper(context, pages.ToArray(), staticParts);
                                }
                                else
                                {
                                    var listString = keys.Select((t, i) => $"{t} = {values[i]}").ToList();

                                    await context.Channel.SendMessageAsync(
                                        $"**{configItemKey}** contains the following keys:{Environment.NewLine}```{Environment.NewLine}{string.Join($"{Environment.NewLine}", listString)}{Environment.NewLine}```");
                                }
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't list the keys in the `{configItemKey}` map? {ex.Message}");
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't list the keys in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        default:
                            await context.Channel.SendMessageAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                }
                else if (action == "get")
                {
                    switch (configItem.Type)
                    {
                        case ConfigItemType.StringDictionary:
                            try
                            {
                                var contents = "";

                                if (configItem.Nullable)
                                    contents = ConfigHelper.GetNullableStringDictionaryValue(
                                        config,
                                        configItemKey, value);
                                else
                                    contents = (string?)ConfigHelper.GetStringDictionaryValue(
                                        config,
                                        configItemKey, value);

                                await context.Channel.SendMessageAsync(
                                    $"**{value}** contains the following value: `{contents.Replace("`", "\\`")}`");
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't get the value in the `{value}` key from the `{configItemKey}` map? {ex.Message}");
                            }
                            catch (KeyNotFoundException ex)
                            {
                                if (ex.Message.Contains(value))
                                {
                                    await context.Channel.SendMessageAsync("The key you provided does not exist within the map.");
                                }
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't get the value in the `{value}` key from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case ConfigItemType.BooleanDictionary:
                            try
                            {
                                var contents = ConfigHelper.GetBooleanDictionaryValue(config,
                                    configItemKey, value);

                                await context.Channel.SendMessageAsync(
                                    $"**{value}** contains the following value: `{contents}`");
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't get the value in the `{value}` key from the `{configItemKey}` map? {ex.Message}");
                            }
                            catch (KeyNotFoundException ex)
                            {
                                if (ex.Message.Contains(value))
                                {
                                    await context.Channel.SendMessageAsync("The key you provided does not exist within the map.");
                                }
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't get the value in the `{value}` key from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        default:
                            await context.Channel.SendMessageAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                }
                else if (action == "set")
                {
                    var key = value.Split(' ')[0].ToLower();
                    value = value.Replace(key + " ", "");
                    
                    switch (configItem.Type)
                    {
                        case ConfigItemType.StringDictionary:
                            try
                            {
                                (string, string?, string?) result = ("", null, null);

                                if (configItem.Nullable)
                                {
                                    if (value == "<nothing>") value = null;
                                    
                                    if (ConfigHelper.DoesNullableStringDictionaryKeyExist(config,
                                            configItemKey, key))
                                        result = await ConfigHelper.SetNullableStringDictionaryValue(config,
                                            configItemKey, key, value);
                                    else
                                        result = await ConfigHelper.CreateNullableStringDictionaryKey(config,
                                            configItemKey, key, value);
                                }
                                else
                                {
                                    if (ConfigHelper.DoesStringDictionaryKeyExist(config,
                                            configItemKey, key))
                                        result = await ConfigHelper.SetStringDictionaryValue(config,
                                            configItemKey, key, value);
                                    else
                                        result = await ConfigHelper.CreateStringDictionaryKey(config,
                                            configItemKey, key, value);
                                }

                                if (result.Item2 == null)
                                {
                                    await context.Channel.SendMessageAsync(
                                        $"I added the following string to the `{result.Item1}` map key in the `{configItemKey}` map: `{result.Item3.Replace("`", "\\`")}`");
                                }
                                else
                                {
                                    await context.Channel.SendMessageAsync(
                                        $"I changed the string in the `{result.Item1}` map key in the `{configItemKey}` map from `{result.Item2.Replace("`", "\\`")}` to `{result.Item3.Replace("`", "\\`")}`");
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't create the string you wanted in the `{configItemKey}` map because the `{key}` key already exists.");
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't create the string you wanted in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case ConfigItemType.BooleanDictionary:
                            try
                            {
                                (string, bool?, bool) result = ("", null, false);
                                
                                if (ConfigHelper.DoesBooleanDictionaryKeyExist(config,
                                        configItemKey, key))
                                    result = await ConfigHelper.SetBooleanDictionaryValue(config,
                                        configItemKey, key, value);
                                else
                                    result = await ConfigHelper.CreateBooleanDictionaryKey(config,
                                        configItemKey, key, value);

                                if (result.Item2 == null)
                                {
                                    await context.Channel.SendMessageAsync(
                                        $"I added the following boolean to the `{result.Item1}` map key in the `{configItemKey}` map: `{result.Item3}`");
                                }
                                else
                                {
                                    await context.Channel.SendMessageAsync(
                                        $"I changed the boolean in the `{result.Item1}` map key in the `{configItemKey}` map from `{result.Item2}` to `{result.Item3}`");
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't create the boolean you wanted in the `{configItemKey}` map because the `{key}` key already exists.");
                            }
                            catch (FormatException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't set `{key}` to the content provided because you provided content that I couldn't turn into a boolean. Please try again.",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't create the boolean you wanted in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        default:
                            await context.Channel.SendMessageAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                }
                else if (action == "delete" || action == "remove")
                {
                    switch (configItem.Type)
                    {
                        case ConfigItemType.StringDictionary:
                            try
                            {
                                if (configItem.Nullable)
                                    await ConfigHelper.RemoveNullableStringDictionaryKey(config,
                                        configItemKey, value);
                                else
                                    await ConfigHelper.RemoveStringDictionaryKey(config,
                                        configItemKey, value);

                                await context.Channel.SendMessageAsync(
                                    $"I removed the string with the following key from the `{configItemKey}` map: `{value}`");
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the string you wanted from the `{configItemKey}` map because the `{value}` key already doesn't exist.");
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the string you wanted from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        case ConfigItemType.BooleanDictionary:
                            try
                            {
                                await ConfigHelper.RemoveBooleanDictionaryKey(config, configItemKey,
                                    value);

                                await context.Channel.SendMessageAsync(
                                    $"I removed the boolean with the following key from the `{configItemKey}` map: `{value}`");
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the boolean you wanted from the `{configItemKey}` map because the `{value}` key already exists.");
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove the boolean you wanted from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        default:
                            await context.Channel.SendMessageAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                }
                else
                {
                    await context.Channel.SendMessageAsync(
                        "The action you wanted to take isn't supported for this type of config item, the available actions are `list`, `get`, `set`, and `delete`.");
                    return;
                }
            }
            else if (configDescriber.TypeIsDictionaryList(configItem.Type))
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
                                    .GetStringListDictionary(config, configItemKey).Keys
                                    .ToList();

                                var values = ConfigHelper
                                    .GetStringListDictionary(config, configItemKey).Values
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

                                        pages[pageNumber] +=
                                            $"{keys[i]} ({values[i].Count} entries){Environment.NewLine}";
                                    }


                                    string[] staticParts =
                                    {
                                        $"**{configItemKey}** contains the following keys:",
                                        ""
                                    };

                                    var paginationMessage =
                                        new PaginationHelper(context, pages.ToArray(), staticParts);
                                }
                                else
                                {
                                    var listString = keys.Select((t, i) =>
                                        $"{t} ({values[i].Count} entries){Environment.NewLine}").ToList();

                                    await context.Channel.SendMessageAsync(
                                        $"**{configItemKey}** contains the following keys:{Environment.NewLine}```{Environment.NewLine}{string.Join($"{Environment.NewLine}", listString)}{Environment.NewLine}```");
                                }
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't list the keys in the `{configItemKey}` map? {ex.Message}");
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't list the keys in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        default:
                            await context.Channel.SendMessageAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                }
                else if (action == "get")
                {
                    if (value == "")
                    {
                        await context.Channel.SendMessageAsync("Please provide a key to get.");
                        return;
                    }
                    
                    switch (configItem.Type)
                    {
                        case ConfigItemType.StringListDictionary:
                            try
                            {
                                var stringList =
                                    ConfigHelper.GetStringListDictionaryValue(config, configItemKey,
                                        value);

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
                                        $"**{value}** contains the following values:",
                                        ""
                                    };

                                    var paginationMessage = new PaginationHelper(context, pages.ToArray(), staticParts);
                                }
                                else
                                {
                                    await context.Channel.SendMessageAsync(
                                        $"**{value}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", stringList)}{Environment.NewLine}```",
                                        allowedMentions: AllowedMentions.None);
                                }
                            }
                            catch (KeyNotFoundException ex)
                            {
                                if (ex.Message.Contains(value))
                                {
                                    await context.Channel.SendMessageAsync("The key you provided does not exist within the map.");
                                }
                            }

                            break;
                        // Other types, I don't see any reason to add them until I need them.
                        default:
                            await context.Channel.SendMessageAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                }
                else if (action == "add")
                {
                    var key = value.Split(' ')[0].ToLower();
                    value = value.Replace(key + " ", "");
                    
                    switch (configItem.Type)
                    {
                        case ConfigItemType.StringListDictionary:
                            try
                            {
                                var output =
                                    ConfigHelper.DoesStringListDictionaryKeyExist(config, configItemKey, key)
                                    ? await ConfigHelper.AddToStringListDictionaryValue(config,
                                        configItemKey, key, value)
                                    : await ConfigHelper.CreateStringListDictionaryKey(config, configItemKey, key, value);

                                await context.Channel.SendMessageAsync(
                                    $"I added the following string to the `{output.Item1}` string list in the `{configItemKey}` map: `{output.Item2}`",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't add your content to the `{key}` string list in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        // Other types, I don't see any reason to add them until I need them.
                        default:
                            await context.Channel.SendMessageAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }                    
                }
                else if (action == "deleteitem")
                {
                    var key = value.Split(' ')[0].ToLower();
                    value = value.Replace(key + " ", "");
                    switch (configItem.Type)
                    {
                        case ConfigItemType.StringListDictionary:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromStringListDictionaryValue(
                                        config, configItemKey, key, value);

                                await context.Channel.SendMessageAsync(
                                    $"I removed the following content from the `{output.Item1}` string list in the `{configItemKey}` map: `{output.Item2}`",
                                    allowedMentions: AllowedMentions.None);
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't remove your content from the `{key}` string list in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        // Other types, I don't see any reason to add them until I need them.
                        default:
                            await context.Channel.SendMessageAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                }
                else if (action == "deletelist")
                {
                    switch (configItem.Type)
                    {
                        case ConfigItemType.StringListDictionary:
                            try
                            {
                                await ConfigHelper.RemoveStringListDictionaryKey(config,
                                    configItemKey, value);

                                await context.Channel.SendMessageAsync(
                                    $"I deleted the string list with the following key from the `{configItemKey}` map: {value}");
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't delete the string list you wanted from the `{configItemKey}` map because the `{value}` key already doesn't exist.");
                            }
                            catch (ArgumentException)
                            {
                                await context.Channel.SendMessageAsync(
                                    $"I couldn't delete the string list you wanted from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                            }

                            break;
                        // Other types, I don't see any reason to add them until I need them.
                        default:
                            await context.Channel.SendMessageAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                }
                else
                {
                    
                }
            }
            else
            {
                context.Message.ReplyAsync($"I couldn't determine what type {configItem.Type} is.");
            }
        }
    }
}