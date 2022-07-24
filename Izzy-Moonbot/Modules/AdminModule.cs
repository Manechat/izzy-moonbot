using System.Diagnostics;
using Izzy_Moonbot.Describers;

namespace Izzy_Moonbot.Modules
{
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Izzy_Moonbot.Attributes;
    using Izzy_Moonbot.Helpers;
    using Izzy_Moonbot.Service;
    using Izzy_Moonbot.Settings;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    [Summary("Module for managing admin functions")]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        private readonly LoggingService _logger;
        private ServerSettings _settings;
        private ServerSettingsDescriber _settingsDescriber;
        private Dictionary<ulong, User> _users;

        private DateTimeOffset lastMentionResponse = DateTimeOffset.MinValue;

        public AdminModule(LoggingService logger, ServerSettings settings, ServerSettingsDescriber settingsDescriber, Dictionary<ulong, User> users)
        {
            _logger = logger;
            _settings = settings;
            _settingsDescriber = settingsDescriber;
            _users = users;
        }

        [Command("panic")]
        [Summary("Immediatly disconnects the client.")]
        [RequireContext(ContextType.Guild)]
        [ModCommand(Group = "Permissions")]
        [DevCommand(Group = "Permissions")]
        public async Task PanicCommand()
        {
            // Just closes the connection.
            await ReplyAsync("<a:izzywhat:891381404741550130>");
            Context.Client.LogoutAsync();
        }
        
        [Command("setup")]
        [Summary("Bot setup command")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetupCommandAsync(
            [Summary("Mod channel name")] string modChannelName = "",
            [Summary("Mod log channel name")] string logChannelName = "",
            [Remainder][Summary("Mod role name")] string modRoleName = "")
        {
            var prefix = _settings.Prefix;
            ulong channelSetId;

            await ReplyAsync("Moving in to my new place...");
            if (modChannelName == "")
            {
                channelSetId = Context.Channel.Id;
            }
            else
            {
                channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(modChannelName, Context);
            }

            if (channelSetId > 0)
            {
                _settings.ModChannel = channelSetId;
                await ReplyAsync($"Moved into <#{channelSetId}>!");
                var modChannel = Context.Guild.GetTextChannel(_settings.ModChannel);
                await FileHelper.SaveSettingsAsync(this._settings);
                await modChannel.SendMessageAsync("Hi new friends! I will send important messages here now.");
            }
            else
            {
                await ReplyAsync($"I couldn't find a place called #{modChannelName}.");
                await _logger.Log($"setup: channel {modChannelName} <FAIL>, channel {logChannelName} <NOT CHECKED>, role {modRoleName} <NOT CHECKED>", Context);
                return;
            }

            await ReplyAsync("Looking for the bot log channel...");
            var logSetId = await DiscordHelper.GetChannelIdIfAccessAsync(logChannelName, Context);
            if(logSetId > 0)
            {
                _settings.LogChannel = logSetId;
                await ReplyAsync($"Posting logs into <#{logSetId}>!");
                var logChannel = Context.Guild.GetTextChannel(_settings.LogChannel);
                await FileHelper.SaveSettingsAsync(this._settings);
                await logChannel.SendMessageAsync("Hi new friends! I'll be posting my logs in here now.");
            }
            else
            {
                await ReplyAsync($"I couldn't find a place called #{logChannelName}.");
                await _logger.Log($"setup: channel {modChannelName} <SUCCESS>, channel {logChannelName} <FAIL>, role {modRoleName} <NOT CHECKED>", Context);
                return;
            }

            await ReplyAsync("Looking for the bosses...");
            var roleSetId = DiscordHelper.GetRoleIdIfAccessAsync(modRoleName, Context);
            if (roleSetId > 0)
            {
                _settings.ModRole = roleSetId;
                await ReplyAsync($"<@&{roleSetId}> is in charge now!", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"I couldn't find @{modRoleName}.");
                await _logger.Log($"setup: channel {modChannelName} <SUCCESS>, channel {logChannelName} <SUCCESS>, role {modRoleName} <FAIL>", Context, true);
                return;
            }

            await ReplyAsync("Setting the remaining admin settings to default values (all alerts will post to the admin channel, and no roles will be pinged)...");
            await FileHelper.SaveSettingsAsync(this._settings);
            await ReplyAsync(
                $"Settings saved. I'm all set! Type `{prefix}help admin` for a list of other admin setup commands.");
            await _logger.Log($"setup: channel {modChannelName} <SUCCESS>, channel {logChannelName} <SUCCESS>, role {modRoleName} <SUCCESS>", Context, true);
        }

        [Command("scan")]
        [Summary("Refresh the stored userlist")]
        [RequireContext(ContextType.Guild)]
        [ModCommand(Group = "Permissions")]
        [DevCommand(Group = "Permissions")]
        public async Task ScanCommandAsync(
            [Summary("The type of scan to do")] [Remainder]
            string scanType = ""
        )
        {
            if (scanType.ToLower() == "full")
            {
                Task.Factory.StartNew(async () =>
                {
                    if(!Context.Guild.HasAllMembers) await Context.Guild.DownloadUsersAsync();
                    
                    int newUserCount = 0;
                    int reloadUserCount = 0;
                    int knownUserCount = 0;
                    
                    await foreach (var socketGuildUser in Context.Guild.Users.ToAsyncEnumerable())
                    {
                        if (!_users.ContainsKey(socketGuildUser.Id))
                        {
                            User newUser = new User();
                            newUser.Username = $"{socketGuildUser.Username}#{socketGuildUser.Discriminator}";
                            newUser.Aliases.Add(socketGuildUser.Username);
                            _users.Add(socketGuildUser.Id, newUser);
                            newUserCount += 1;
                        }
                        else
                        {
                            if (_users[socketGuildUser.Id].Username == "")
                            {
                                _users[socketGuildUser.Id].Username =
                                    $"{socketGuildUser.Username}#{socketGuildUser.Discriminator}";
                                reloadUserCount += 1;
                            }
                            else if (!_users[socketGuildUser.Id].Aliases.Contains(socketGuildUser.DisplayName))
                            {
                                _users[socketGuildUser.Id].Aliases.Add(socketGuildUser.DisplayName);
                                reloadUserCount += 1;
                            }
                            else
                            {
                                knownUserCount += 1;
                            }
                        }
                    }

                    await FileHelper.SaveUsersAsync(_users);

                    await Context.Message.ReplyAsync(
                        $"Done! I discovered {Context.Guild.Users.Count} members, of which{Environment.NewLine}" +
                        $"{newUserCount} were unknown to me until now,{Environment.NewLine}" +
                        $"{reloadUserCount} had out of date Username and Alias information,{Environment.NewLine}" +
                        $"and {knownUserCount} didn't need to be updated.");
                });
            }
        }

#nullable enable
        [Command("config")]
        [Summary("Config management")]
        [RequireContext(ContextType.Guild)]
        [ModCommand(Group = "Permissions")]
        [DevCommand(Group = "Permissions")]
        public async Task ConfigCommandAsync(
            [Summary("The item to get/modify.")] string configItemKey = "",
            [Summary("")][Remainder] string? value = "")
        {
            if (configItemKey == "")
            {
                await Context.Message.ReplyAsync($"Hii!! Here's now to use the config command!{Environment.NewLine}" +
                                 $"Run `{_settings.Prefix}config <category>` to list the config items in a category.{Environment.NewLine}"+
                                 $"Run `{_settings.Prefix}config <item>` to view information about an item.{Environment.NewLine}{Environment.NewLine}"+
                                 $"Here's a list of all possible categories.{Environment.NewLine}```{Environment.NewLine}"+
                                 $"core - Config items which dictate core settings (often global).{Environment.NewLine}"+
                                 $"moderation - Config items which dictate moderation settings.{Environment.NewLine}"+
                                 $"debug - Debug config items used to debug Izzy.{Environment.NewLine}"+
                                 $"user - Config items regarding users.{Environment.NewLine}"+
                                 $"filter - Config items regarding the filter.{Environment.NewLine}"+
                                 $"spam - Config items regarding spam pressure.{Environment.NewLine}"+
                                 $"raid - Config items regarding antiraid.{Environment.NewLine}```");

                return;
            }

            ServerSettingsItem? configItem = _settingsDescriber.GetItem(configItemKey);

            if (configItem == null)
            {
                // Config item not found, but could be a category.
                if (_settingsDescriber.StringToCategory(configItemKey) != null)
                {
                    // it's not null we literally check above u stupid piece of code
                    SettingsItemCategory category = _settingsDescriber.StringToCategory(configItemKey).Value;
                    
                    List<string> itemList = _settingsDescriber.GetSettableConfigItemsByCategory(category);

                    if (itemList.Count > 10)
                    {
                        // Use pagination
                        List<string> pages = new List<string>();
                        int pageNumber = -1;
                        for (int i = 0; i < itemList.Count; i++)
                        {
                            if (i % 10 == 0)
                            {
                                pageNumber += 1;
                                pages.Add("");
                            }

                            pages[pageNumber] += itemList[i] + Environment.NewLine;
                        }


                        string[] staticParts = {
                            $"Hii!! Here's a list of all the config items I could find in the {_settingsDescriber.CategoryToString(category)} category!",
                            $"Run `{_settings.Prefix}config <item> to view information about an item!"
                        };

                        PaginationHelper paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts, 0);
                    }
                    else
                    {
                        await ReplyAsync($"Hii!! Here's a list of all the config items I could find in the {_settingsDescriber.CategoryToString(category)} category!" +
                                         $"{Environment.NewLine}```{Environment.NewLine}{string.Join(Environment.NewLine, itemList)}{Environment.NewLine}```{Environment.NewLine}" +
                                         $"Run `{_settings.Prefix}config <item> to view information about an item!");
                    }

                    return;
                }
                else
                {
                    await ReplyAsync($"Sorry, I couldn't find a config value or category called `{configItemKey}`!");
                    return;
                }
            }

            if (value == "")
            {
                // Only the configItemKey was given, we give the user what their data was
                string nullableString = $"(Pass `<nothing>` as the value when setting to set to nothing/null)";
                if (!configItem.Nullable) nullableString = "";

                switch(configItem.Type)
                {
                    case SettingsItemType.String:
                    case SettingsItemType.Char:
                    case SettingsItemType.Boolean:
                    case SettingsItemType.Integer:
                    case SettingsItemType.Double:
                        await ReplyAsync($"**{configItemKey}** - {_settingsDescriber.TypeToString(configItem.Type)} - {_settingsDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                             $"*{configItem.Description}*{Environment.NewLine}" +
                             $"Current value: `{ConfigHelper.GetValue<ServerSettings>(_settings, configItemKey)}`{Environment.NewLine}" +
                             $"Run `{_settings.Prefix}config {configItemKey} <value>` to set this value. {nullableString}", allowedMentions: AllowedMentions.None);
                        break;
                    case SettingsItemType.User:
                        await ReplyAsync($"**{configItemKey}** - {_settingsDescriber.TypeToString(configItem.Type)} - {_settingsDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                             $"*{configItem.Description}*{Environment.NewLine}" +
                             $"Current value: <@{ConfigHelper.GetValue<ServerSettings>(_settings, configItemKey)}>{Environment.NewLine}" +
                             $"Run `{_settings.Prefix}config {configItemKey} <value>` to set this value. {nullableString}", allowedMentions: AllowedMentions.None);
                        break;
                    case SettingsItemType.Role:
                        await ReplyAsync($"**{configItemKey}** - {_settingsDescriber.TypeToString(configItem.Type)} - {_settingsDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                             $"*{configItem.Description}*{Environment.NewLine}" +
                             $"Current value: <@&{ConfigHelper.GetValue<ServerSettings>(_settings, configItemKey)}>{Environment.NewLine}" +
                             $"Run `{_settings.Prefix}config {configItemKey} <value>` to set this value. {nullableString}", allowedMentions: AllowedMentions.None);
                        break;
                    case SettingsItemType.Channel:
                        await ReplyAsync($"**{configItemKey}** - {_settingsDescriber.TypeToString(configItem.Type)} - {_settingsDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                             $"*{configItem.Description}*{Environment.NewLine}" +
                             $"Current value: <#{ConfigHelper.GetValue<ServerSettings>(_settings, configItemKey)}>{Environment.NewLine}" +
                             $"Run `{_settings.Prefix}config {configItemKey} <value>` to set this value. {nullableString}", allowedMentions: AllowedMentions.None);
                        break;
                    case SettingsItemType.StringList:
                    case SettingsItemType.CharList:
                    case SettingsItemType.BooleanList:
                    case SettingsItemType.IntegerList:
                    case SettingsItemType.DoubleList:
                    case SettingsItemType.UserList:
                    case SettingsItemType.RoleList:
                    case SettingsItemType.ChannelList:
                        await ReplyAsync($"**{configItemKey}** - {_settingsDescriber.TypeToString(configItem.Type)} - {_settingsDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                             $"*{configItem.Description}*{Environment.NewLine}" +
                             $"Run `{_settings.Prefix}config {configItemKey} list` to view the contents of this list.{Environment.NewLine}" +
                             $"Run `{_settings.Prefix}config {configItemKey} add <value>` to add a value to this list. {nullableString}{Environment.NewLine}"+
                             $"Run `{_settings.Prefix}config {configItemKey} remove <value>` to remove a value from this list. {nullableString}", allowedMentions: AllowedMentions.None);
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
                        await ReplyAsync($"**{configItemKey}** - {_settingsDescriber.TypeToString(configItem.Type)} - {_settingsDescriber.CategoryToString(configItem.Category)} category{Environment.NewLine}" +
                                         $"*{configItem.Description}*{Environment.NewLine}" +
                                         $"Run `{_settings.Prefix}config {configItemKey} list` to view a list of keys in this map.{Environment.NewLine}" + 
                                         $"Run `{_settings.Prefix}config {configItemKey} create <key>` to create a new key in this map.{Environment.NewLine}" +
                                         $"Run `{_settings.Prefix}config {configItemKey} delete <key>` to delete a key from this map.{Environment.NewLine}"+
                                         $"Run `{_settings.Prefix}config {configItemKey} <key>` to view information about a key in this map.", allowedMentions: AllowedMentions.None);
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

                            string? resultString = await ConfigHelper.SetStringValue<ServerSettings>(_settings, configItemKey, value);
                            await ReplyAsync($"I've set `{configItemKey}` to the following content: {resultString}", allowedMentions: AllowedMentions.None);
                            break;
                        case SettingsItemType.Char:
                            if (configItem.Nullable && value == "<nothing>") value = null;

                            try
                            {
                                char? output = null;
                                if (value != null)
                                {
                                    if (!char.TryParse(value, out char res)) throw new FormatException(); // Trip "invalid content" catch below.
                                    output = res;
                                }

                                char? resultChar = await ConfigHelper.SetCharValue<ServerSettings>(_settings, configItemKey, output);
                                await ReplyAsync($"I've set `{configItemKey}` to the following content: {resultChar}", allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await ReplyAsync($"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a character. Please try again.", allowedMentions: AllowedMentions.None);
                            }
                            break;
                        case SettingsItemType.Boolean:
                            if (configItem.Nullable && value == "<nothing>") value = null;

                            try
                            {
                                bool? resultBoolean = await ConfigHelper.SetBooleanValue<ServerSettings>(_settings, configItemKey, value);
                                await ReplyAsync($"I've set `{configItemKey}` to the following content: {resultBoolean}", allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await ReplyAsync($"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a boolean. Please try again.", allowedMentions: AllowedMentions.None);
                            }
                            break;
                        case SettingsItemType.Integer:
                            if (configItem.Nullable && value == "<nothing>") value = null;

                            try
                            {
                                int? output = null;
                                if (value != null)
                                {
                                    if (!int.TryParse(value, out int res)) throw new FormatException(); // Trip "invalid content" catch below.
                                    output = res;
                                }

                                int? resultInteger = await ConfigHelper.SetIntValue<ServerSettings>(_settings, configItemKey, output);
                                await ReplyAsync($"I've set `{configItemKey}` to the following content: {resultInteger}", allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await ReplyAsync($"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a integer. Please try again.", allowedMentions: AllowedMentions.None);
                            }
                            break;
                        case SettingsItemType.Double:
                            if (configItem.Nullable && value == "<nothing>") value = null;

                            try
                            {
                                double? output = null;
                                if (value != null)
                                {
                                    if (!double.TryParse(value, out double res)) throw new FormatException(); // Trip "invalid content" catch below.
                                    output = res;
                                }

                                double? resultDouble = await ConfigHelper.SetDoubleValue<ServerSettings>(_settings, configItemKey, output);
                                await ReplyAsync($"I've set `{configItemKey}` to the following content: {resultDouble}", allowedMentions: AllowedMentions.None);
                            }
                            catch (FormatException)
                            {
                                await ReplyAsync($"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a double. Please try again.", allowedMentions: AllowedMentions.None);
                            }
                            break;
                        case SettingsItemType.User:
                            if (configItem.Nullable && value == "<nothing>") value = null;
                            try
                            {
                                SocketGuildUser? result = await ConfigHelper.SetUserValue<ServerSettings>(_settings, configItemKey, value, Context);
                                string response = "`null`";
                                if(result != null) response = $"<@{result.Id}>";
                                await ReplyAsync($"I've set `{configItemKey}` to the following content: {response}");
                            }
                            catch (MemberAccessException)
                            {
                                await ReplyAsync($"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a user. Please try again.", allowedMentions: AllowedMentions.None);
                            }
                            break;
                        case SettingsItemType.Role:
                            if (configItem.Nullable && value == "<nothing>") value = null;
                            try
                            {
                                SocketRole? result = await ConfigHelper.SetRoleValue<ServerSettings>(_settings, configItemKey, value, Context);
                                string response = "`null`";
                                if (result != null) response = $"<@&{result.Id}>";
                                await ReplyAsync($"I've set `{configItemKey}` to the following content: {response}", allowedMentions: AllowedMentions.None);
                            }
                            catch (MemberAccessException)
                            {
                                await ReplyAsync($"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a role. Please try again.", allowedMentions: AllowedMentions.None);
                            }
                            break;
                        case SettingsItemType.Channel:
                            if (configItem.Nullable && value == "<nothing>") value = null;
                            try
                            {
                                SocketGuildChannel? result = await ConfigHelper.SetChannelValue<ServerSettings>(_settings, configItemKey, value, Context);
                                string response = "`null`";
                                if (result != null) response = $"<#{result.Id}>";
                                await ReplyAsync($"I've set `{configItemKey}` to the following content: {response}", allowedMentions: AllowedMentions.None);
                            }
                            catch (MemberAccessException)
                            {
                                await ReplyAsync($"I couldn't set `{configItemKey}` to the content provided because you provided content that I couldn't turn into a channel. Please try again.", allowedMentions: AllowedMentions.None);
                            }
                            break;
                        default:
                            await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                            break;
                    }
                }
                else if(_settingsDescriber.TypeIsList(configItem.Type))
                {
                    string action = value.Split(' ')[0].ToLower();
                    value = value.Replace(action + " ", "");

                    if (action == "list")
                    {
                        switch (configItem.Type)
                        {
                            case SettingsItemType.StringList:
                                List<string>? stringList = ConfigHelper.GetStringList<ServerSettings>(_settings, configItemKey);

                                if (stringList == null)
                                {
                                    await ReplyAsync("Somehow, the entire list is null.");
                                    return;
                                }

                                if (stringList.Count > 10)
                                {
                                    // Use pagination
                                    List<string> pages = new List<string>();
                                    int pageNumber = -1;
                                    for (int i = 0; i < stringList.Count; i++)
                                    {
                                        if (i % 10 == 0)
                                        {
                                            pageNumber += 1;
                                            pages.Add("");
                                        }

                                        pages[pageNumber] += stringList[i] + Environment.NewLine;
                                    }


                                    string[] staticParts = new[]
                                    {
                                        $"**{configItemKey}** contains the following values:",
                                        ""
                                    };

                                    PaginationHelper paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts, 0);
                                }
                                else
                                {
                                    await ReplyAsync(
                                        $"**{configItemKey}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", stringList)}{Environment.NewLine}```",
                                        allowedMentions: AllowedMentions.None);
                                }
                                break;
                            case SettingsItemType.CharList:
                                List<char>? charList = ConfigHelper.GetCharList<ServerSettings>(_settings, configItemKey);

                                if (charList == null)
                                {
                                    await ReplyAsync("Somehow, the entire list is null.");
                                    return;
                                }

                                if (charList.Count > 10)
                                {
                                    // Use pagination
                                    List<string> pages = new List<string>();
                                    int pageNumber = -1;
                                    for (int i = 0; i < charList.Count; i++)
                                    {
                                        if (i % 10 == 0)
                                        {
                                            pageNumber += 1;
                                            pages.Add("");
                                        }

                                        pages[pageNumber] += charList[i] + Environment.NewLine;
                                    }


                                    string[] staticParts = new[]
                                    {
                                        $"**{configItemKey}** contains the following values:",
                                        ""
                                    };

                                    PaginationHelper paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts, 0);
                                }
                                else
                                {
                                    await ReplyAsync(
                                        $"**{configItemKey}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", charList)}{Environment.NewLine}```",
                                        allowedMentions: AllowedMentions.None);
                                }
                                break;
                            case SettingsItemType.BooleanList:
                                List<bool>? booleanList = ConfigHelper.GetBooleanList<ServerSettings>(_settings, configItemKey);

                                if (booleanList == null)
                                {
                                    await ReplyAsync("Somehow, the entire list is null.");
                                    return;
                                }

                                if (booleanList.Count > 10)
                                {
                                    // Use pagination
                                    List<string> pages = new List<string>();
                                    int pageNumber = -1;
                                    for (int i = 0; i < booleanList.Count; i++)
                                    {
                                        if (i % 10 == 0)
                                        {
                                            pageNumber += 1;
                                            pages.Add("");
                                        }

                                        pages[pageNumber] += booleanList[i] + Environment.NewLine;
                                    }


                                    string[] staticParts = new[]
                                    {
                                        $"**{configItemKey}** contains the following values:",
                                        ""
                                    };

                                    PaginationHelper paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts, 0);
                                }
                                else
                                {
                                    await ReplyAsync(
                                        $"**{configItemKey}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", booleanList)}{Environment.NewLine}```",
                                        allowedMentions: AllowedMentions.None);
                                }
                                break;
                            case SettingsItemType.IntegerList:
                                List<int>? intList = ConfigHelper.GetIntList<ServerSettings>(_settings, configItemKey);

                                if (intList == null)
                                {
                                    await ReplyAsync("Somehow, the entire list is null.");
                                    return;
                                }

                                if (intList.Count > 10)
                                {
                                    // Use pagination
                                    List<string> pages = new List<string>();
                                    int pageNumber = -1;
                                    for (int i = 0; i < intList.Count; i++)
                                    {
                                        if (i % 10 == 0)
                                        {
                                            pageNumber += 1;
                                            pages.Add("");
                                        }

                                        pages[pageNumber] += intList[i] + Environment.NewLine;
                                    }


                                    string[] staticParts = new[]
                                    {
                                        $"**{configItemKey}** contains the following values:",
                                        ""
                                    };

                                    PaginationHelper paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts, 0);
                                }
                                else
                                {
                                    await ReplyAsync(
                                        $"**{configItemKey}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", intList)}{Environment.NewLine}```",
                                        allowedMentions: AllowedMentions.None);
                                }
                                break;
                            case SettingsItemType.DoubleList:
                                List<double>? doubleList = ConfigHelper.GetDoubleList<ServerSettings>(_settings, configItemKey);

                                if (doubleList == null)
                                {
                                    await ReplyAsync("Somehow, the entire list is null.");
                                    return;
                                }

                                if (doubleList.Count > 10)
                                {
                                    // Use pagination
                                    List<string> pages = new List<string>();
                                    int pageNumber = -1;
                                    for (int i = 0; i < doubleList.Count; i++)
                                    {
                                        if (i % 10 == 0)
                                        {
                                            pageNumber += 1;
                                            pages.Add("");
                                        }

                                        pages[pageNumber] += doubleList[i] + Environment.NewLine;
                                    }


                                    string[] staticParts = new[]
                                    {
                                        $"**{configItemKey}** contains the following values:",
                                        ""
                                    };

                                    PaginationHelper paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts, 0);
                                }
                                else
                                {
                                    await ReplyAsync(
                                        $"**{configItemKey}** contains the following values:{Environment.NewLine}```{Environment.NewLine}{string.Join(", ", doubleList)}{Environment.NewLine}```",
                                        allowedMentions: AllowedMentions.None);
                                }
                                break;
                            case SettingsItemType.UserList:
                                HashSet<SocketGuildUser> userList = ConfigHelper.GetUserList<ServerSettings>(_settings, configItemKey, Context);

                                List<string> userMentionList = new List<string>();
                                foreach (SocketGuildUser user in userList)
                                {
                                    userMentionList.Add($"<@{user.Id}>");
                                }

                                if (userMentionList.Count > 10)
                                {
                                    // Use pagination
                                    List<string> pages = new List<string>();
                                    int pageNumber = -1;
                                    for (int i = 0; i < userMentionList.Count; i++)
                                    {
                                        if (i % 10 == 0)
                                        {
                                            pageNumber += 1;
                                            pages.Add("");
                                        }

                                        pages[pageNumber] += userMentionList[i] + Environment.NewLine;
                                    }


                                    string[] staticParts = new[]
                                    {
                                        $"**{configItemKey}** contains the following values:",
                                        ""
                                    };

                                    PaginationHelper paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts, 0, false);
                                }
                                else
                                {
                                    await ReplyAsync(
                                        $"**{configItemKey}** contains the following values:{Environment.NewLine}{string.Join(", ", userMentionList)}");
                                }
                                break;
                            case SettingsItemType.RoleList:
                                HashSet<SocketRole> roleList = ConfigHelper.GetRoleList<ServerSettings>(_settings, configItemKey, Context);

                                List<string> roleMentionList = new List<string>();
                                foreach (SocketRole role in roleList)
                                {
                                    roleMentionList.Add(role.Mention);
                                }

                                if (roleMentionList.Count > 10)
                                {
                                    // Use pagination
                                    List<string> pages = new List<string>();
                                    int pageNumber = -1;
                                    for (int i = 0; i < roleMentionList.Count; i++)
                                    {
                                        if (i % 10 == 0)
                                        {
                                            pageNumber += 1;
                                            pages.Add("");
                                        }

                                        pages[pageNumber] += roleMentionList[i] + Environment.NewLine;
                                    }


                                    string[] staticParts = new[]
                                    {
                                        $"**{configItemKey}** contains the following values:",
                                        ""
                                    };

                                    PaginationHelper paginationMessage = new PaginationHelper(Context, pages.ToArray(),
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
                                HashSet<SocketGuildChannel> channelList = ConfigHelper.GetChannelList<ServerSettings>(_settings, configItemKey, Context);

                                List<string> channelMentionList = new List<string>();
                                foreach (SocketGuildChannel channel in channelList)
                                {
                                    channelMentionList.Add($"<#{channel.Id}>");
                                }

                                if (channelMentionList.Count > 10)
                                {
                                    // Use pagination
                                    List<string> pages = new List<string>();
                                    int pageNumber = -1;
                                    for (int i = 0; i < channelMentionList.Count; i++)
                                    {
                                        if (i % 10 == 0)
                                        {
                                            pageNumber += 1;
                                            pages.Add("");
                                        }

                                        pages[pageNumber] += channelMentionList[i] + Environment.NewLine;
                                    }


                                    string[] staticParts = new[]
                                    {
                                        $"**{configItemKey}** contains the following values:",
                                        ""
                                    };

                                    PaginationHelper paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts, 0, false);
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
                    }
                    else if(action == "add")
                    {
                        switch (configItem.Type)
                        {
                            case SettingsItemType.StringList:
                                try
                                {
                                    string output = await ConfigHelper.AddToStringList<ServerSettings>(_settings, configItemKey, value);

                                    await ReplyAsync($"I added the following content to the `{configItemKey}` string list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```", allowedMentions: AllowedMentions.None);
                                }
                                catch(ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't add your content to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                                }
                                break;
                            case SettingsItemType.CharList:
                                try
                                {
                                    if (!char.TryParse(value, out char charValue)) throw new FormatException();

                                    char output = await ConfigHelper.AddToCharList<ServerSettings>(_settings, configItemKey, charValue);

                                    await ReplyAsync($"I added the following content to the `{configItemKey}` character list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```", allowedMentions: AllowedMentions.None);
                                }
                                catch (FormatException)
                                {
                                    await ReplyAsync($"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into a single character. Please try again.", allowedMentions: AllowedMentions.None);
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                                }
                                break;
                            case SettingsItemType.BooleanList:
                                try
                                {
                                    bool output = await ConfigHelper.AddToBooleanList<ServerSettings>(_settings, configItemKey, value);

                                    await ReplyAsync($"I added the following content to the `{configItemKey}` boolean list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```", allowedMentions: AllowedMentions.None);
                                }
                                catch (FormatException)
                                {
                                    await ReplyAsync($"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into a boolean. Please try again.", allowedMentions: AllowedMentions.None);
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                                }
                                break;
                            case SettingsItemType.IntegerList:
                                try
                                {
                                    if (!int.TryParse(value, out int intValue)) throw new FormatException();

                                    int output = await ConfigHelper.AddToIntList<ServerSettings>(_settings, configItemKey, intValue);

                                    await ReplyAsync($"I added the following content to the `{configItemKey}` integer list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```", allowedMentions: AllowedMentions.None);
                                }
                                catch (FormatException)
                                {
                                    await ReplyAsync($"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into an integer. Please try again.", allowedMentions: AllowedMentions.None);
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                                }
                                break;
                            case SettingsItemType.DoubleList:
                                try
                                {
                                    if (!double.TryParse(value, out double doubleValue)) throw new FormatException();

                                    double output = await ConfigHelper.AddToDoubleList<ServerSettings>(_settings, configItemKey, doubleValue);

                                    await ReplyAsync($"I added the following content to the `{configItemKey}` double list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```", allowedMentions: AllowedMentions.None);
                                }
                                catch (FormatException)
                                {
                                    await ReplyAsync($"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into a double. Please try again.", allowedMentions: AllowedMentions.None);
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                                }
                                break;
                            case SettingsItemType.UserList:
                                try
                                {
                                    SocketGuildUser output = await ConfigHelper.AddToUserList<ServerSettings>(_settings, configItemKey, value, Context);

                                    await ReplyAsync($"I added the following content to the `{configItemKey}` user list:{Environment.NewLine}{output}");
                                }
                                catch (MemberAccessException)
                                {
                                    await ReplyAsync($"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into a user I know about. Please try again.", allowedMentions: AllowedMentions.None);
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    await ReplyAsync($"I couldn't add the user you provided to the `{configItemKey}` list because the user is already in that list.");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                                }
                                break;
                            case SettingsItemType.RoleList:
                                try
                                {
                                    SocketRole output = await ConfigHelper.AddToRoleList<ServerSettings>(_settings, configItemKey, value, Context);

                                    await ReplyAsync($"I added the following content to the `{configItemKey}` role list:{Environment.NewLine}{output}");
                                }
                                catch (MemberAccessException)
                                {
                                    await ReplyAsync($"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into a role I know about. Please try again.", allowedMentions: AllowedMentions.None);
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    await ReplyAsync($"I couldn't add the role you provided to the `{configItemKey}` list because the role is already in that list.");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                                }
                                break;
                            case SettingsItemType.ChannelList:
                                try
                                {
                                    SocketGuildChannel output = await ConfigHelper.AddToChannelList<ServerSettings>(_settings, configItemKey, value, Context);

                                    await ReplyAsync($"I added the following content to the `{configItemKey}` channel list:{Environment.NewLine}{output}");
                                }
                                catch (MemberAccessException)
                                {
                                    await ReplyAsync($"I couldn't add the content you provided to the `{configItemKey}` list because you provided content that I couldn't turn into a channel I know about. Please try again.", allowedMentions: AllowedMentions.None);
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    await ReplyAsync($"I couldn't add the channel you provided to the `{configItemKey}` list because the channel is already in that list.");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't add the content you provided to the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
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
                            case SettingsItemType.StringList:
                                try
                                {
                                    string output = await ConfigHelper.RemoveFromStringList<ServerSettings>(_settings, configItemKey, value);

                                    await ReplyAsync($"I removed the following content from the `{configItemKey}` string list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```", allowedMentions: AllowedMentions.None);
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because the content isn't in that list to begin with.");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                                }
                                break;
                            case SettingsItemType.CharList:
                                try
                                {
                                    if (!char.TryParse(value, out char charValue)) throw new FormatException();

                                    char output = await ConfigHelper.AddToCharList<ServerSettings>(_settings, configItemKey, charValue);

                                    await ReplyAsync($"I removed the following content from the `{configItemKey}` character list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```", allowedMentions: AllowedMentions.None);
                                }
                                catch (FormatException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a character. Please try again.", allowedMentions: AllowedMentions.None);
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because the content isn't in that list to begin with.");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                                }
                                break;
                            case SettingsItemType.BooleanList:
                                try
                                {
                                    bool output = await ConfigHelper.RemoveFromBooleanList<ServerSettings>(_settings, configItemKey, value);

                                    await ReplyAsync($"I removed the following content from the `{configItemKey}` boolean list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```", allowedMentions: AllowedMentions.None);
                                }
                                catch (FormatException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a boolean. Please try again.", allowedMentions: AllowedMentions.None);
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because the content isn't in that list to begin with.");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                                }
                                break;
                            case SettingsItemType.IntegerList:
                                try
                                {
                                    if (!int.TryParse(value, out int intValue)) throw new FormatException();

                                    int output = await ConfigHelper.RemoveFromIntList<ServerSettings>(_settings, configItemKey, intValue);

                                    await ReplyAsync($"I removed the following content from the `{configItemKey}` integer list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```", allowedMentions: AllowedMentions.None);
                                }
                                catch (FormatException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a integer. Please try again.", allowedMentions: AllowedMentions.None);
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because the content isn't in that list to begin with.");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                                }
                                break;
                            case SettingsItemType.DoubleList:
                                try
                                {
                                    if (!double.TryParse(value, out double doubleValue)) throw new FormatException();

                                    double output = await ConfigHelper.RemoveFromDoubleList<ServerSettings>(_settings, configItemKey, doubleValue);

                                    await ReplyAsync($"I removed the following content from the `{configItemKey}` double list:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```", allowedMentions: AllowedMentions.None);
                                }
                                catch (FormatException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a double. Please try again.", allowedMentions: AllowedMentions.None);
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because the content isn't in that list to begin with.");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                                }
                                break;
                            case SettingsItemType.UserList:
                                try
                                {
                                    SocketGuildUser output = await ConfigHelper.RemoveFromUserList<ServerSettings>(_settings, configItemKey, value, Context);

                                    await ReplyAsync($"I removed the following content from the `{configItemKey}` user list:{Environment.NewLine}{output}");
                                }
                                catch (MemberAccessException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a user I know about. Please try again.", allowedMentions: AllowedMentions.None);
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    await ReplyAsync($"I couldn't remove the user you provided from the `{configItemKey}` list because the user isn't in that list to begin with.");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                                }
                                break;
                            case SettingsItemType.RoleList:
                                try
                                {
                                    SocketRole output = await ConfigHelper.RemoveFromRoleList<ServerSettings>(_settings, configItemKey, value, Context);

                                    await ReplyAsync($"I removed the following content from the `{configItemKey}` role list:{Environment.NewLine}{output}");
                                }
                                catch (MemberAccessException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a role I know about. Please try again.", allowedMentions: AllowedMentions.None);
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    await ReplyAsync($"I couldn't remove the role you provided from the `{configItemKey}` list because the role isn't in that list to begin with.");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                                }
                                break;
                            case SettingsItemType.ChannelList:
                                try
                                {
                                    SocketGuildChannel output = await ConfigHelper.RemoveFromChannelList<ServerSettings>(_settings, configItemKey, value, Context);

                                    await ReplyAsync($"I removed the following content from the `{configItemKey}` channel list:{Environment.NewLine}{output}");
                                }
                                catch (MemberAccessException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because you provided content that I couldn't turn into a channel I know about. Please try again.", allowedMentions: AllowedMentions.None);
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    await ReplyAsync($"I couldn't remove the channel you provided from the `{configItemKey}` list because the channel isn't in that list to begin with.");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't remove the content you provided from the `{configItemKey}` list because the `{configItemKey}` config item isn't a list. There is likely a misconfiguration in the config item describer.");
                                }
                                break;
                            default:
                                await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                                break;
                        }
                    }
                }
                else if(_settingsDescriber.TypeIsDictionaryValue(configItem.Type))
                {
                    string action = value.Split(' ')[0].ToLower();
                    value = value.Replace(action + " ", "");

                    if (action == "list")
                    {
                        switch (configItem.Type)
                        { 
                            case SettingsItemType.StringDictionary:
                                try 
                                {
                                    List<string> keys = new List<string>();
                                    if (configItem.Nullable)
                                    {
                                        keys = ConfigHelper.GetNullableStringDictionary<ServerSettings>(_settings, configItemKey, Context).Keys.ToList();
                                    }
                                    else
                                    {
                                        keys = ConfigHelper.GetStringDictionary<ServerSettings>(_settings, configItemKey, Context).Keys.ToList();
                                    }

                                    if (keys.Count > 10)
                                    {
                                        // Use pagination
                                        List<string> pages = new List<string>();
                                        int pageNumber = -1;
                                        for (int i = 0; i < keys.Count; i++)
                                        {
                                            if (i % 10 == 0)
                                            {
                                                pageNumber += 1;
                                                pages.Add("");
                                            }

                                            pages[pageNumber] += keys[i] + Environment.NewLine;
                                        }

                                        string[] staticParts = {
                                            $"**{configItemKey}** contains the following keys:",
                                            ""
                                        };

                                        PaginationHelper paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts, 0);
                                    }
                                    else
                                    {
                                        await ReplyAsync(
                                            $"**{configItemKey}** contains the following keys:{Environment.NewLine}```{Environment.NewLine}{string.Join($"{Environment.NewLine}", keys)}{Environment.NewLine}```");
                                    }
                                }
                                catch (ArgumentOutOfRangeException ex)
                                {
                                    await ReplyAsync($"I couldn't list the keys in the `{configItemKey}` map? {ex.Message}");
                                }
                                catch (ArgumentException) 
                                {
                                    await ReplyAsync($"I couldn't list the keys in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                                }
                                break;
                            case SettingsItemType.BooleanDictionary:
                                try 
                                {
                                    List<string> keys = ConfigHelper.GetBooleanDictionary<ServerSettings>(_settings, configItemKey, Context).Keys.ToList();

                                    if (keys.Count > 10)
                                    {
                                        // Use pagination
                                        List<string> pages = new List<string>();
                                        int pageNumber = -1;
                                        for (int i = 0; i < keys.Count; i++)
                                        {
                                            if (i % 10 == 0)
                                            {
                                                pageNumber += 1;
                                                pages.Add("");
                                            }

                                            pages[pageNumber] += keys[i] + Environment.NewLine;
                                        }

                                        string[] staticParts = {
                                            $"**{configItemKey}** contains the following keys:",
                                            ""
                                        };

                                        PaginationHelper paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts, 0);
                                    }
                                    else
                                    {
                                        await ReplyAsync(
                                            $"**{configItemKey}** contains the following keys:{Environment.NewLine}```{Environment.NewLine}{string.Join($"{Environment.NewLine}", keys)}{Environment.NewLine}```");
                                    }
                                }
                                catch (ArgumentOutOfRangeException ex)
                                {
                                    await ReplyAsync($"I couldn't list the keys in the `{configItemKey}` map? {ex.Message}");
                                }
                                catch (ArgumentException) 
                                {
                                    await ReplyAsync($"I couldn't list the keys in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
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
                                    {
                                        await ConfigHelper.CreateNullableStringDictionaryKey<ServerSettings>(_settings, configItemKey, value, Context);
                                    }
                                    else
                                    {
                                        await ConfigHelper.CreateStringDictionaryKey<ServerSettings>(_settings, configItemKey, value, Context);
                                    }

                                    await ReplyAsync($"I created the string with the following key in the `{configItemKey}` map: {value}");
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    await ReplyAsync($"I couldn't create the string you wanted in the `{configItemKey}` map because the `{value}` key already exists.");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't create the string you wanted in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                                }
                                break;
                            case SettingsItemType.BooleanDictionary:
                                try
                                {
                                    await ConfigHelper.CreateBooleanDictionaryKey<ServerSettings>(_settings, configItemKey, value, Context);

                                    await ReplyAsync($"I created the boolean with the following key in the `{configItemKey}` map: {value}");
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    await ReplyAsync($"I couldn't create the boolean you wanted in the `{configItemKey}` map because the `{value}` key already exists.");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't create the boolean you wanted in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
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
                                    {
                                        await ConfigHelper.RemoveNullableStringDictionaryKey<ServerSettings>(_settings, configItemKey, value, Context);
                                    }
                                    else
                                    {
                                        await ConfigHelper.RemoveStringDictionaryKey<ServerSettings>(_settings, configItemKey, value, Context);
                                    }

                                    await ReplyAsync($"I removed the string with the following key from the `{configItemKey}` map: {value}");
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    await ReplyAsync($"I couldn't remove the string you wanted from the `{configItemKey}` map because the `{value}` key already doesn't exist.");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't remove the string you wanted from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                                }
                                break;
                            case SettingsItemType.BooleanDictionary:
                                try
                                {
                                    await ConfigHelper.RemoveBooleanDictionaryKey<ServerSettings>(_settings, configItemKey, value, Context);

                                    await ReplyAsync($"I removed the boolean with the following key from the `{configItemKey}` map: {value}");
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    await ReplyAsync($"I couldn't remove the boolean you wanted from the `{configItemKey}` map because the `{value}` key already exists.");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't remove the boolean you wanted from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
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
                        string key = action.ToLower();
                        action = value.Split(' ')[0].ToLower();
                        value = value.Replace(action + " ", "");

                        if (action == "get")
                        {
                            switch (configItem.Type)
                            {
                                case SettingsItemType.StringDictionary:
                                    try
                                    {
                                        string? contents = "";

                                        if (configItem.Nullable)
                                        {
                                            contents = ConfigHelper.GetNullableStringDictionaryValue<ServerSettings>(_settings,
                                                configItemKey, key, Context);
                                        }
                                        else
                                        {
                                            contents = (string?) ConfigHelper.GetStringDictionaryValue<ServerSettings>(_settings,
                                                configItemKey, key, Context);
                                        }
                                        
                                        await ReplyAsync(
                                                $"**{key}** contains the following value: {contents}");
                                    }
                                    catch (ArgumentOutOfRangeException ex)
                                    {
                                        await ReplyAsync($"I couldn't get the value in the `{key}` key from the `{configItemKey}` map? {ex.Message}");
                                    }
                                    catch (ArgumentException)
                                    {
                                        await ReplyAsync($"I couldn't get the value in the `{key}` key from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                                    }
                                    break;
                                case SettingsItemType.BooleanDictionary:
                                    try
                                    {
                                        bool contents = ConfigHelper.GetBooleanDictionaryValue<ServerSettings>(_settings,
                                            configItemKey, key, Context);

                                        await ReplyAsync(
                                            $"**{key}** contains the following value: {contents}");
                                    }
                                    catch (ArgumentOutOfRangeException ex)
                                    {
                                        await ReplyAsync($"I couldn't get the value in the `{key}` key from the `{configItemKey}` map? {ex.Message}");
                                    }
                                    catch (ArgumentException)
                                    {
                                        await ReplyAsync($"I couldn't get the value in the `{key}` key from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                                    }
                                    
                                    break;
                                default:
                                    await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                                    break;
                            }
                        }
                        else if(action == "set")
                        {
                            switch (configItem.Type)
                            {
                                case SettingsItemType.StringDictionary:
                                    try
                                    {
                                        if (configItem.Nullable)
                                        {
                                            if (value == "<nothing>") value = null;

                                            string? result = await ConfigHelper.SetNullableStringDictionaryValue<ServerSettings>(
                                                _settings, configItemKey, key, value, Context);

                                            await Context.Message.ReplyAsync(
                                                $"I've set `{key}` to the following content: {result}",
                                                allowedMentions: AllowedMentions.None);
                                        }
                                        else
                                        {
                                            string result = await ConfigHelper.SetStringDictionaryValue<ServerSettings>(
                                                _settings, configItemKey, key, value, Context);

                                            await Context.Message.ReplyAsync(
                                                $"I've set `{key}` to the following content: {result}",
                                                allowedMentions: AllowedMentions.None);
                                        }
                                    }
                                    catch (ArgumentOutOfRangeException ex)
                                    {
                                        await ReplyAsync($"I couldn't set the value in the `{key}` key from the `{configItemKey}` map? {ex.Message}");
                                    }
                                    catch (ArgumentException)
                                    {
                                        await ReplyAsync($"I couldn't set the value in the `{key}` key from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                                    }
                                    break;
                                case SettingsItemType.BooleanDictionary:
                                    try
                                    {
                                        bool result = await ConfigHelper.SetBooleanDictionaryValue<ServerSettings>(
                                            _settings, configItemKey, key, value, Context);

                                        await Context.Message.ReplyAsync(
                                            $"I've set `{key}` to the following content: {result}",
                                            allowedMentions: AllowedMentions.None);
                                    }
                                    catch (ArgumentOutOfRangeException ex)
                                    {
                                        await ReplyAsync($"I couldn't set the value in the `{key}` key from the `{configItemKey}` map? {ex.Message}");
                                    }
                                    catch (ArgumentException)
                                    {
                                        await ReplyAsync($"I couldn't set the value in the `{key}` key from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                                    }
                                    
                                    break;
                                default:
                                    await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                                    break;
                            }
                        }
                        else
                        {
                            string nullableString = $" (Pass `<nothing>` as the value when setting to set to nothing/null)";
                            if (!configItem.Nullable) nullableString = "";
                            
                            await ReplyAsync($"**{key}** - Key in {configItemKey}{Environment.NewLine}" +
                                             $"Run `{_settings.Prefix}config {configItemKey} {key} get` to get the value in this map key.{Environment.NewLine}" + 
                                             $"Run `{_settings.Prefix}config {configItemKey} {key} set <value>` to set the value for this map key{nullableString}.", allowedMentions: AllowedMentions.None);
                        }
                    }
                }
                else if(_settingsDescriber.TypeIsDictionaryList(configItem.Type))
                {
                    string action = value.Split(' ')[0].ToLower();
                    value = value.Replace(action + " ", "");

                    if (action == "list")
                    {
                        switch (configItem.Type)
                        {
                            case SettingsItemType.StringListDictionary:
                                try
                                {
                                    List<string> keys = ConfigHelper.GetStringListDictionary<ServerSettings>(_settings, configItemKey, Context).Keys.ToList();

                                    if (keys.Count > 10)
                                    {
                                        // Use pagination
                                        List<string> pages = new List<string>();
                                        int pageNumber = -1;
                                        for (int i = 0; i < keys.Count; i++)
                                        {
                                            if (i % 10 == 0)
                                            {
                                                pageNumber += 1;
                                                pages.Add("");
                                            }

                                            pages[pageNumber] += keys[i] + Environment.NewLine;
                                        }
                                        

                                        string[] staticParts = new[]
                                        {
                                            $"**{configItemKey}** contains the following keys:",
                                            ""
                                        };

                                        PaginationHelper paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts, 0);
                                    }
                                    else
                                    {
                                        await ReplyAsync(
                                            $"**{configItemKey}** contains the following keys:{Environment.NewLine}```{Environment.NewLine}{string.Join($"{Environment.NewLine}", keys)}{Environment.NewLine}```");
                                    }
                                }
                                catch (ArgumentOutOfRangeException ex)
                                {
                                    await ReplyAsync($"I couldn't list the keys in the `{configItemKey}` map? {ex.Message}");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't list the keys in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
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
                                    await ConfigHelper.CreateStringListDictionaryKey<ServerSettings>(_settings, configItemKey, value, Context);

                                    await ReplyAsync($"I created the string list with the following key in the `{configItemKey}` map: {value}");
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    await ReplyAsync($"I couldn't create the string list you wanted in the `{configItemKey}` map because the `{value}` key already exists.");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't create the string list you wanted in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
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
                                    await ConfigHelper.RemoveStringListDictionaryKey<ServerSettings>(_settings, configItemKey, value, Context);

                                    await ReplyAsync($"I deleted the string list with the following key from the `{configItemKey}` map: {value}");
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    await ReplyAsync($"I couldn't delete the string list you wanted from the `{configItemKey}` map because the `{value}` key already doesn't exist.");
                                }
                                catch (ArgumentException)
                                {
                                    await ReplyAsync($"I couldn't delete the string list you wanted from the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
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
                        string key = action.ToLower();
                        action = value.Split(' ')[0].ToLower();
                        value = value.Replace(action + " ", "");

                        if (action == "list")
                        {
                            switch (configItem.Type)
                            {
                                case SettingsItemType.StringListDictionary: 
                                    List<string>? stringList = ConfigHelper.GetStringListDictionaryValue<ServerSettings>(_settings, configItemKey, key, Context);

                                    if (stringList == null)
                                    {
                                        await ReplyAsync("Somehow, the entire list is null.");
                                        return;
                                    }

                                    if (stringList.Count > 10)
                                    {
                                        // Use pagination
                                        List<string> pages = new List<string>();
                                        int pageNumber = -1;
                                        for (int i = 0; i < stringList.Count; i++)
                                        {
                                            if (i % 10 == 0)
                                            {
                                                pageNumber += 1;
                                                pages.Add("");
                                            }

                                            pages[pageNumber] += stringList[i] + Environment.NewLine;
                                        }
                                        
                                        string[] staticParts = new[]
                                        {
                                            $"**{key}** contains the following values:",
                                            ""
                                        };

                                        PaginationHelper paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
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
                        else if(action == "add")
                        {
                            switch (configItem.Type)
                            {
                                case SettingsItemType.StringListDictionary:
                                    try
                                    {
                                        string output = await ConfigHelper.AddToStringListDictionaryValue<ServerSettings>(_settings, configItemKey, key, value, Context);

                                        await ReplyAsync($"I added the following content to the `{key}` string list in the `{configItemKey}` map:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```", allowedMentions: AllowedMentions.None);
                                    }
                                    catch(ArgumentException)
                                    {
                                        await ReplyAsync($"I couldn't add your content to the `{key}` string list in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
                                    }
                                    break;
                                // TODO: Other types, I don't see any reason to add them until I need them.
                                default:
                                    await ReplyAsync("I seem to have encountered a setting type that I do not know about.");
                                    break;
                            }
                        }
                        else if(action == "remove")
                        {
                            switch (configItem.Type)
                            {
                                case SettingsItemType.StringListDictionary:
                                    try
                                    {
                                        string output = await ConfigHelper.RemoveFromStringListDictionaryValue<ServerSettings>(_settings, configItemKey, key, value, Context);

                                        await ReplyAsync($"I removed the following content from the `{key}` string list in the `{configItemKey}` map:{Environment.NewLine}```{Environment.NewLine}{output}{Environment.NewLine}```", allowedMentions: AllowedMentions.None);
                                    }
                                    catch(ArgumentException)
                                    {
                                        await ReplyAsync($"I couldn't remove your content from the `{key}` string list in the `{configItemKey}` map because the `{configItemKey}` config item isn't a map. There is likely a misconfiguration in the config item describer.");
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
                            string nullableString = $" (Pass `<nothing>` as the value when setting to set to nothing/null)";
                            if (!configItem.Nullable) nullableString = "";
                            
                            await ReplyAsync($"**{key}** - Key in {configItemKey}{Environment.NewLine}" +
                                             $"Run `{_settings.Prefix}config {configItemKey} {key} list` to view a list of values in this map key.{Environment.NewLine}" + 
                                             $"Run `{_settings.Prefix}config {configItemKey} {key} add <value>` to add a value to this map key{nullableString}.{Environment.NewLine}" +
                                             $"Run `{_settings.Prefix}config {configItemKey} {key} remove <value>` to remove a value from this map key{nullableString}.", allowedMentions: AllowedMentions.None);
                        }
                    }
                }
                else
                {
                    Context.Message.ReplyAsync($"I couldn't determine what type {configItem.Type} is.");
                }
            }
        }

        [Command("echo")]
        [Summary("Posts a message to a specified channel")]
        [ModCommand(Group = "Permission")]
        [DevCommand(Group = "Permission")]
        public async Task EchoCommandAsync([Summary("The channel to send to")] string channelName = "", [Remainder][Summary("The message to send")] string message = "")
        {
            if (channelName == "")
            {
                await ReplyAsync("You must specify a channel name or a message.");
                await _logger.Log("echo: <FAIL>", Context);
                return;
            }

            var channelId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);

            if (channelId > 0)
            {
                var channel = Context.Guild.GetTextChannel(channelId);
                if (message == "")
                {
                    await ReplyAsync("There's no message to send there.");
                    await _logger.Log($"echo: {channelName} <FAIL>", Context);
                    return;
                }

                if (channel != null)
                {
                    await channel.SendMessageAsync(message);
                    await _logger.Log($"echo: {channelName} {message} <SUCCESS>", Context, true);
                    return;
                }


                await ReplyAsync("I can't send a message there.");
                await _logger.Log($"echo: {channelName} {message} <FAIL>", Context);
                return;
            }

            await ReplyAsync($"{channelName} {message}");
            await _logger.Log($"echo: {channelName} {message} <SUCCESS>", Context, true);
        }

        [Command("setprefix")]
        [Summary("Sets the bot listen prefix")]
        public async Task SetPrefixCommandAsync([Summary("The prefix character")] char prefix = ';')
        {
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, _settings))
            {
                return;
            }

            await ReplyAsync($"I will now listen for '{prefix}' on this server.");
            this._settings.Prefix = prefix;
            await FileHelper.SaveSettingsAsync(this._settings);
        }

        /*[Command("alias")]
        [Summary("Sets an alias")]
        public async Task AliasCommandAsync(string subcommand = "", string shortForm = "", [Remainder] string longForm = "")
        {
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, _settings))
            {
                return;
            }

            switch (subcommand)
            {
                case "":
                    await ReplyAsync("You must enter a subcommand");
                    break;
                case "get":
                    var output = $"__Current aliases:__{Environment.NewLine}";
                    foreach (var (shortFormA, longFormA) in _settings.Aliases)
                    {
                        output += $"`{shortFormA}`: `{longFormA}`{Environment.NewLine}";
                    }

                    await ReplyAsync(output);
                    break;
                case "add":
                    if (_settings.Aliases.ContainsKey(shortForm))
                    {
                        _settings.Aliases[shortForm] = longForm;
                        await FileHelper.SaveSettingsAsync(this._settings);
                        await ReplyAsync($"`{shortForm}` now aliased to `{longForm}`, replacing what was there before.");
                    }
                    else
                    {
                        _settings.Aliases.Add(shortForm, longForm);
                        await FileHelper.SaveSettingsAsync(this._settings);
                        await ReplyAsync($"`{shortForm}` now aliased to `{longForm}`");
                    }

                    break;
                case "remove":
                    _settings.Aliases.Remove(shortForm);
                    await FileHelper.SaveSettingsAsync(this._settings);
                    await ReplyAsync($"`{shortForm}` alias cleared.");
                    break;
                case "clear":
                    _settings.Aliases.Clear();
                    await FileHelper.SaveSettingsAsync(this._settings);
                    await ReplyAsync("All aliases cleared.");
                    break;
                default:
                    await ReplyAsync($"Invalid subcommand {subcommand}");
                    break;
            }
        }*/

        [Command("getsettings")]
        [Summary("Posts the settings file to the log channel")]
        public async Task GetSettingsCommandAsync()
        {
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, _settings))
            {
                return;
            }

            if (Context.IsPrivate)
            {
                await ReplyAsync("Cannot get settings in a DM.");
                return;
            }

            var errorMessage = await SettingsGetAsync(Context, _settings);
            if (errorMessage.Contains("<ERROR>"))
            {
                await ReplyAsync(errorMessage);
                await _logger.Log($"getsettings: {errorMessage} <FAIL>", Context);
                return;
            }

            await _logger.Log("getsettings: <SUCCESS>", Context);
        }

        [Command("getusers")]
        [RequireOwner]
        public async Task GetUsersCommandAsync()
        {
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, _settings))
            {
                return;
            }

            if (Context.IsPrivate)
            {
                await ReplyAsync("Cannot get settings in a DM.");
                return;
            }

            var errorMessage = await UsersGetAsync(Context, _settings);
            if (errorMessage.Contains("<ERROR>"))
            {
                await ReplyAsync(errorMessage);
                await _logger.Log($"getsettings: {errorMessage} <FAIL>", Context);
                return;
            }

            await _logger.Log("getsettings: <SUCCESS>", Context);
        }

        [Command("gethistory")]
        [RequireOwner]
        public async Task GetChannelHistoryCommandAsync(string channel, ulong messageId)
        {
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, _settings))
            {
                return;
            }

            if (Context.IsPrivate)
            {
                await ReplyAsync("Cannot get settings in a DM.");
                return;
            }

            var errorMessage = await ChannelHistoryGetAsync(Context, _settings, channel, messageId);
            if (errorMessage.Contains("<ERROR>"))
            {
                await ReplyAsync(errorMessage);
                await _logger.Log($"getsettings: {errorMessage} <FAIL>", Context);
                return;
            }

            await _logger.Log("getsettings: <SUCCESS>", Context);
        }

        /*[Command("<blank message>")]
        [Summary("Runs on a blank message")]
        public async Task BlankMessageCommandAsync()
        {
            if (!DiscordHelper.CanUserRunThisCommand(Context, _settings))
            {
                return;
            }

            await ReplyAsync("Did you need something?");
        }

        [Command("<invalid command>")]
        [Summary("Runs on an invalid command")]
        public async Task InvalidCommandAsync()
        {
            if (!DiscordHelper.CanUserRunThisCommand(Context, _settings))
            {
                return;
            }

            await ReplyAsync("I don't know that command.");
        }*/

        [Command("<mention>")]
        [Summary("Runs when someone mentions Izzy")]
        public async Task MentionCommandAsync()
        {
            if (!_settings.MentionResponseEnabled) return; // Responding to mention is disabled.
            _logger.Log((DateTimeOffset.UtcNow - lastMentionResponse).TotalSeconds.ToString(), Context, false);
            if ((DateTimeOffset.UtcNow - lastMentionResponse).TotalSeconds < _settings.MentionResponseCooldown) return; // Still on cooldown.

            var random = new Random();
            int index = random.Next(_settings.MentionResponses.Count);
            string response = _settings.MentionResponses[index];
            
            lastMentionResponse = DateTimeOffset.Now;

            await ReplyAsync($"{response}", messageReference: new MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id));
        }

        private async Task<string> SettingsGetAsync(SocketCommandContext context, ServerSettings settings)
        {
            await ReplyAsync("Retrieving settings file...");
            var filepath = FileHelper.SetUpFilepath(FilePathType.Root, "settings", "conf", Context);
            if (!File.Exists(filepath))
            {
                return "<ERROR> File does not exist";
            }

            await context.Channel.SendFileAsync(filepath, "settings.conf");
            return "SUCCESS";
        }

        private async Task<string> UsersGetAsync(SocketCommandContext context, ServerSettings settings)
        {
            await ReplyAsync("Writing user list to file...");
            var filepath = FileHelper.SetUpFilepath(FilePathType.Root, "users", "conf", Context);
            if (!File.Exists(filepath))
            {
                File.Create(filepath);
            }

            var userlist = new List<string>();
            await context.Guild.DownloadUsersAsync();
            var users = context.Guild.Users;
            foreach (var user in users)
            {
                userlist.Add($"<@{user.Id}>, {user.Username}#{user.Discriminator}");
            }
            await File.WriteAllLinesAsync(filepath, userlist);

            await context.Channel.SendFileAsync(filepath, $"{context.Guild.Name}-users.conf");
            return "SUCCESS";
        }

        private async Task<string> ChannelHistoryGetAsync(SocketCommandContext context, ServerSettings settings, string channelString, ulong messageId)
        {
            await ReplyAsync("Writing channel history to file...");
            var channelId = await DiscordHelper.GetChannelIdIfAccessAsync(channelString, context);
            if (channelId == 0)
            {
                return "<ERROR> No channel access";
            }

            var channel = context.Guild.GetTextChannel(channelId);
            var filepath = FileHelper.SetUpFilepath(FilePathType.Channel, channelString, "log", Context);
            var id = messageId;
            var messageList = new List<string>();
            var limit = 100;
            var initialMessage = await channel.GetMessageAsync(id);
            messageList.Add($"{initialMessage.Author.Username}#{initialMessage.Author.Discriminator} ({initialMessage.Author.Id}) at {initialMessage.CreatedAt.UtcDateTime.ToShortDateString()} {initialMessage.CreatedAt.UtcDateTime.ToShortTimeString()}: {initialMessage.Content}");
            await File.WriteAllLinesAsync(filepath, messageList);
            messageList.Clear();

            var messages = await channel.GetMessagesAsync(id, Direction.After, limit).FlattenAsync();
            var list = messages.ToList();
            list.Reverse();
            var done = false;

            while (!done)
            {
                if (list.Count < limit)
                {
                    done = true;
                }

                foreach (var message in list)
                {
                    messageList.Add($"{message.Author.Username}#{message.Author.Discriminator} ({message.Author.Id}) at {message.CreatedAt.UtcDateTime.ToShortDateString()} {message.CreatedAt.UtcDateTime.ToShortTimeString()}: {message.Content}");
                }
                await File.AppendAllLinesAsync(filepath, messageList);
                messageList.Clear();
                if (list.Count > 0)
                {
                    id = list[^1].Id;
                    messages = await channel.GetMessagesAsync(id, Direction.After, limit).FlattenAsync();
                    list = messages.ToList();
                    list.Reverse();
                }
            }

            await context.Channel.SendFileAsync(filepath, $"{context.Guild.Name}-{channelString}.log");
            await ReplyAsync("Success!");
            return "SUCCESS";
        }

        [Summary("Submodule for retreiving log files")]
        public class LogModule : ModuleBase<SocketCommandContext>
        {
            private readonly LoggingService _logger;
            private readonly ServerSettings _settings;

            public LogModule(LoggingService logger, ServerSettings settings)
            {
                _logger = logger;
                _settings = settings;
            }

            [Command("log")]
            [Summary("Retrieves a log file")]
            public async Task LogCommandAsync(
                [Summary("The channel to get the log from")] string channel = "",
                [Summary("The date (in format (YYYY-MM-DD) to get the log from")] string date = "")
            {
                if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, _settings))
                {
                    return;
                }

                if (Context.IsPrivate)
                {
                    await ReplyAsync("Cannot get logs in a DM.");
                    return;
                }

                if (channel == "")
                {
                    await ReplyAsync("You need to enter a channel and date.");
                    await _logger.Log("log: <FAIL>", Context);
                    return;
                }

                if (date == "")
                {
                    await ReplyAsync("You need to enter a date.");
                    await _logger.Log($"log: {channel} <FAIL>", Context);
                    return;
                }

                var errorMessage = await LogGetAsync(channel, date, Context);
                if (errorMessage.Contains("<ERROR>"))
                {
                    await ReplyAsync(errorMessage);
                    await _logger.Log($"log: {channel} {date} {errorMessage} <FAIL>", Context);
                    return;
                }

                await _logger.Log($"log: {channel} {date} <SUCCESS>", Context);
            }

            private async Task<string> LogGetAsync(string channelName, string date, SocketCommandContext context)
            {
                await ReplyAsync($"Retrieving log from {channelName} on {date}...");
                var confirmedName = DiscordHelper.ConvertChannelPingToName(channelName, context);
                if (confirmedName.Contains("<ERROR>"))
                {
                    return confirmedName;
                }

                if (_settings.LogChannel <= 0)
                {
                    return "<ERROR> Log post channel not set.";
                }

                var filepath = FileHelper.SetUpFilepath(FilePathType.LogRetrieval, date, "log", context, confirmedName, date);
                if (!File.Exists(filepath))
                {
                    return "<ERROR> File does not exist";
                }

                var logPostChannel = context.Guild.GetTextChannel(_settings.LogChannel);
                await logPostChannel.SendFileAsync(filepath, $"{confirmedName}-{date}.log");
                return "SUCCESS";
            }
        }
    }
}
