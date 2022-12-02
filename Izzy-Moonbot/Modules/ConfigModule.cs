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
                case ConfigItemType.StringSet:
                case ConfigItemType.RoleSet:
                case ConfigItemType.ChannelSet:
                    await context.Channel.SendMessageAsync(
                        $"**{configItemKey}** - {configDescriber.TypeToString(configItem.Type)} - {configDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} list` to view the contents of this list.{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} add <value>` to add a value to this list. {nullableString}{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} remove <value>` to remove a value from this list. {nullableString}",
                        allowedMentions: AllowedMentions.None);
                    break;
                case ConfigItemType.StringDictionary:
                    await context.Channel.SendMessageAsync(
                        $"**{configItemKey}** - {configDescriber.TypeToString(configItem.Type)} - {configDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                        $"*{configItem.Description}*{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} list` to view a list of keys in this map.{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} get <key>` to get the current value of a key in this map.{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} set <key> <value>` to set a key to a value in this map, creating the key if need be.{Environment.NewLine}" +
                        $"Run `{config.Prefix}config {configItemKey} delete <key>` to delete a key from this map.",
                        allowedMentions: AllowedMentions.None);
                    break;
                case ConfigItemType.StringSetDictionary:
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
                            await ConfigHelper.SetSimpleValue(config, configItemKey, value);
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
                                await ConfigHelper.SetSimpleValue(config, configItemKey, output);
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
                                await ConfigHelper.SetSimpleValue(config, configItemKey, output);
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
                                await ConfigHelper.SetSimpleValue(config, configItemKey, output);
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
                                await ConfigHelper.SetSimpleValue(config, configItemKey, output);
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
            else if (configDescriber.TypeIsSet(configItem.Type))
            {
                var action = value.Split(' ')[0].ToLower();
                value = value.Replace(action + " ", "");

                if (action == "list")
                    switch (configItem.Type)
                    {
                        case ConfigItemType.StringSet:
                            var stringSet = ConfigHelper.GetStringSet(config, configItemKey);

                            if (stringSet == null)
                            {
                                await context.Channel.SendMessageAsync("Somehow, the entire list is null.");
                                return;
                            }

                            if (stringSet.Count > 10)
                            {
                                // Use pagination
                                var pages = new List<string>();
                                var pageNumber = -1;
                                var stringList = stringSet.OrderBy(x => x).ToList();
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
                                    $"**{configItemKey}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", stringSet)}{Environment.NewLine}```",
                                    allowedMentions: AllowedMentions.None);
                            }

                            break;
                        case ConfigItemType.RoleSet:
                            var roleSet = ConfigHelper.GetRoleSet(config, configItemKey, context);

                            var roleMentionList = new List<string>();
                            foreach (var role in roleSet) roleMentionList.Add(role.Mention);

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
                                    staticParts, false, AllowedMentions.None);
                            }
                            else
                            {
                                await context.Channel.SendMessageAsync(
                                    $"**{configItemKey}** contains the following values:{Environment.NewLine}{string.Join(", ", roleMentionList)}",
                                    allowedMentions: AllowedMentions.None);
                            }

                            break;
                        case ConfigItemType.ChannelSet:
                            var channelSet =
                                ConfigHelper.GetChannelSet(config, configItemKey, context);

                            var channelMentionList = new List<string>();
                            foreach (var channel in channelSet) channelMentionList.Add($"<#{channel.Id}>");

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
                                    new PaginationHelper(context, pages.ToArray(), staticParts, false);
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
                        case ConfigItemType.StringSet:
                            try
                            {
                                var output =
                                    await ConfigHelper.AddToStringSet(config, configItemKey, value);

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
                        case ConfigItemType.RoleSet:
                            try
                            {
                                var output =
                                    await ConfigHelper.AddToRoleSet(config, configItemKey, value,
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
                        case ConfigItemType.ChannelSet:
                            try
                            {
                                var output =
                                    await ConfigHelper.AddToChannelSet(config, configItemKey, value,
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
                        case ConfigItemType.StringSet:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromStringSet(config, configItemKey,
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
                        case ConfigItemType.RoleSet:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromRoleSet(config, configItemKey,
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
                        case ConfigItemType.ChannelSet:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromChannelSet(config, configItemKey,
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
                                        .GetDictionary<string?>(config, configItemKey)
                                        .Keys.ToList();
                                    values = ConfigHelper
                                        .GetDictionary<string?>(config, configItemKey)
                                        .Values.ToList();
                                }
                                else
                                {
                                    keys = ConfigHelper
                                        .GetDictionary<string>(config, configItemKey)
                                        .Keys.ToList();
                                    values = ConfigHelper
                                        .GetDictionary<string>(config, configItemKey)
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
                                    contents = ConfigHelper.GetDictionaryValue<string?>(
                                        config,
                                        configItemKey, value);
                                else
                                    contents = (string?)ConfigHelper.GetDictionaryValue<string>(
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
                                    
                                    if (ConfigHelper.DoesDictionaryKeyExist<string?>(config,
                                            configItemKey, key))
                                        result = await ConfigHelper.SetNullableStringDictionaryValue(config,
                                            configItemKey, key, value);
                                    else
                                        result = await ConfigHelper.CreateDictionaryKey<string?>(config,
                                            configItemKey, key, value);
                                }
                                else
                                {
                                    if (ConfigHelper.DoesDictionaryKeyExist<string>(config,
                                            configItemKey, key))
                                        result = await ConfigHelper.SetStringDictionaryValue(config,
                                            configItemKey, key, value);
                                    else
                                        result = await ConfigHelper.CreateDictionaryKey<string>(config,
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
                                    await ConfigHelper.RemoveDictionaryKey<string?>(config,
                                        configItemKey, value);
                                else
                                    await ConfigHelper.RemoveDictionaryKey<string>(config,
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
            else if (configDescriber.TypeIsDictionarySet(configItem.Type))
            {
                var action = value.Split(' ')[0].ToLower();
                value = value.Replace(action + " ", "");

                if (action == "list")
                {
                    switch (configItem.Type)
                    {
                        case ConfigItemType.StringSetDictionary:
                            try
                            {
                                var keys = ConfigHelper
                                    .GetDictionary<HashSet<string>>(config, configItemKey).Keys
                                    .ToList();

                                var values = ConfigHelper
                                    .GetDictionary<HashSet<string>>(config, configItemKey).Values
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
                        case ConfigItemType.StringSetDictionary:
                            try
                            {
                                var stringSet =
                                    ConfigHelper.GetDictionaryValue<HashSet<string>>(config, configItemKey,
                                        value);

                                if (stringSet.Count > 10)
                                {
                                    // Use pagination
                                    var pages = new List<string>();
                                    var pageNumber = -1;
                                    var stringList = stringSet.OrderBy(x => x).ToList();
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
                                        $"**{value}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", stringSet)}{Environment.NewLine}```",
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
                        case ConfigItemType.StringSetDictionary:
                            try
                            {
                                var output =
                                    ConfigHelper.DoesDictionaryKeyExist<HashSet<string>>(config, configItemKey, key)
                                    ? await ConfigHelper.AddToStringSetDictionaryValue(config,
                                        configItemKey, key, value)
                                    : await ConfigHelper.CreateStringSetDictionaryKey(config, configItemKey, key, value);

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
                        case ConfigItemType.StringSetDictionary:
                            try
                            {
                                var output =
                                    await ConfigHelper.RemoveFromStringSetDictionaryValue(
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
                        case ConfigItemType.StringSetDictionary:
                            try
                            {
                                await ConfigHelper.RemoveDictionaryKey<HashSet<string>>(config,
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