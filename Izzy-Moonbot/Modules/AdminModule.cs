namespace Izzy_Moonbot.Modules
{
    using Discord;
    using Discord.Commands;
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
        private readonly ServerSettings _settings;

        public AdminModule(LoggingService logger, ServerSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        [Command("setup")]
        [Summary("Bot setup command")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetupCommandAsync(
            [Summary("Admin channel name")] string adminChannelName = "",
            [Remainder][Summary("Admin role name")] string adminRoleName = "")
        {
            char prefix = _settings.Prefix;
            ulong channelSetId;

            await ReplyAsync("Moving in to my new place...");
            if (adminChannelName == "")
            {
                channelSetId = Context.Channel.Id;
            }
            else
            {
                channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(adminChannelName, Context);
            }

            if (channelSetId > 0)
            {
                _settings.AdminChannel = channelSetId;
                await ReplyAsync($"Moved into <#{channelSetId}>!");
                var adminChannel = Context.Guild.GetTextChannel(_settings.AdminChannel);
                await FileHelper.SaveAllPresettingsAsync(this._settings);
                await adminChannel.SendMessageAsync("Hi new friends! I will send important message here now.");
            }
            else
            {
                await ReplyAsync($"I couldn't find a place called #{adminChannelName}.");
                await _logger.Log($"setup: channel {adminChannelName} <FAIL>, role {adminRoleName} <NOT CHECKED>", Context);
                return;
            }

            await ReplyAsync("Looking for the bosses...");
            var roleSetId = DiscordHelper.GetRoleIdIfAccessAsync(adminRoleName, Context);
            if (roleSetId > 0)
            {
                _settings.AdminRole = roleSetId;
                await ReplyAsync($"<@&{roleSetId}> is in charge now!", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"I couldn't find @{adminRoleName}.");
                await _logger.Log($"setup: channel {adminChannelName} <SUCCESS>, role {adminRoleName} <FAIL>", Context, true);
                return;
            }

            await ReplyAsync("Setting the remaining admin settings to default values (all alerts will post to the admin channel, and no roles will be pinged)...");
            _settings.LogPostChannel = _settings.AdminChannel;
            await FileHelper.SaveAllPresettingsAsync(this._settings);
            await ReplyAsync(
                $"Settings saved. I'm all set! Type `{prefix}help admin` for a list of other admin setup commands.");
            await _logger.Log($"setup: channel {adminChannelName} <SUCCESS>, role {adminRoleName} <SUCCESS>", Context, true);
        }

        [Command("admin")]
        [Summary("Manages admin commands")]
        public async Task AdminCommandAsync(
            [Summary("First subcommand")] string commandOne = "",
            [Summary("Second subcommand")] string commandTwo = "",
            [Remainder][Summary("Third subcommand")] string commandThree = "")
        {
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, _settings))
            {
                return;
            }

            switch (commandOne)
            {
                case "":
                    await ReplyAsync("You need to specify an admin command.");
                    await _logger.Log("admin: <FAIL>", Context);
                    break;
                case "adminchannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await AdminChannelGetAsync();
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await AdminChannelSetAsync(commandThree);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "adminrole":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await AdminRoleGetAsync();
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await AdminRoleSetAsync(commandThree);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "ignorechannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await IgnoreChannelGetAsync();
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "add":
                            await IgnoreChannelAddAsync(commandThree);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "remove":
                            await IgnoreChannelRemoveAsync(commandThree);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            this._settings.IgnoredChannels.Clear();
                            await FileHelper.SaveAllPresettingsAsync(this._settings);
                            await ReplyAsync("Ignored channels list cleared.");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "ignorerole":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await IgnoreRoleGetAsync();
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "add":
                            await IgnoreRoleAddAsync(commandThree);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "remove":
                            await IgnoreRoleRemoveAsync(commandThree);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            _settings.IgnoredRoles.Clear();
                            await FileHelper.SaveAllPresettingsAsync(this._settings);
                            await ReplyAsync("Ignored roles list cleared.");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "allowuser":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await AllowUserGetAsync();
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "add":
                            await AllowUserAddAsync(commandThree);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "remove":
                            await AllowUserRemoveAsync(commandThree);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            _settings.AllowedUsers.Clear();
                            await FileHelper.SaveAllPresettingsAsync(this._settings);
                            await ReplyAsync("Allowed users list cleared.");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "logchannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await LogChannelGetAsync();
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await LogChannelSetAsync(commandThree);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            await LogChannelClearAsync();
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                default:
                    await ReplyAsync($"Invalid command `{commandOne}`");
                    await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                    break;
            }
        }

        [Command("echo")]
        [Summary("Posts a message to a specified channel")]
        public async Task EchoCommandAsync([Summary("The channel to send to")] string channelName = "", [Remainder][Summary("The message to send")] string message = "")
        {
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, _settings))
            {
                return;
            }

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
            await FileHelper.SaveAllPresettingsAsync(this._settings);
        }

        [Command("listentobots")]
        [Summary("Sets the bot listen prefix")]
        public async Task ListenToBotsCommandAsync([Summary("yes or no")] string command = "")
        {
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, _settings))
            {
                return;
            }

            switch (command.ToLower())
            {
                case "":
                    var not = "";
                    if (!_settings.ListenToBots)
                    {
                        not = " not";
                    }

                    await ReplyAsync($"Currently{not} listening to bots.");
                    break;
                case "y":
                case "yes":
                case "on":
                case "true":
                    await ReplyAsync("Now listening to bots.");
                    _settings.ListenToBots = true;
                    await FileHelper.SaveAllPresettingsAsync(this._settings);
                    break;
                case "n":
                case "no":
                case "off":
                case "false":
                    await ReplyAsync("Not listening to bots.");
                    _settings.ListenToBots = false;
                    await FileHelper.SaveAllPresettingsAsync(this._settings);
                    break;
                default:
                    await ReplyAsync("Invalid command.");
                    break;
            }
        }

        [Command("alias")]
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
                        await FileHelper.SaveAllPresettingsAsync(this._settings);
                        await ReplyAsync($"`{shortForm}` now aliased to `{longForm}`, replacing what was there before.");
                    }
                    else
                    {
                        _settings.Aliases.Add(shortForm, longForm);
                        await FileHelper.SaveAllPresettingsAsync(this._settings);
                        await ReplyAsync($"`{shortForm}` now aliased to `{longForm}`");
                    }

                    break;
                case "remove":
                    _settings.Aliases.Remove(shortForm);
                    await FileHelper.SaveAllPresettingsAsync(this._settings);
                    await ReplyAsync($"`{shortForm}` alias cleared.");
                    break;
                case "clear":
                    _settings.Aliases.Clear();
                    await FileHelper.SaveAllPresettingsAsync(this._settings);
                    await ReplyAsync("All aliases cleared.");
                    break;
                default:
                    await ReplyAsync($"Invalid subcommand {subcommand}");
                    break;
            }
        }

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

        [Command("<blank message>")]
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
        }

        [Command("<mention>")]
        [Summary("Runs on a name ping")]
        public async Task MentionCommandAsync()
        {
            if (!DiscordHelper.CanUserRunThisCommand(Context, _settings))
            {
                return;
            }

            await ReplyAsync($"The current prefix is '{_settings.Prefix}'. Type `{_settings.Prefix}help` for a list of commands.");
        }

        private async Task<string> SettingsGetAsync(SocketCommandContext context, ServerSettings settings)
        {
            await ReplyAsync("Retrieving settings file...");
            var filepath = FileHelper.SetUpFilepath(FilePathType.Root, "settings", "conf", Context);
            if (!File.Exists(filepath))
            {
                return "<ERROR> File does not exist";
            }

            var logPostChannel = context.Guild.GetTextChannel(settings.LogPostChannel);
            await logPostChannel.SendFileAsync(filepath, "settings.conf");
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

            var logPostChannel = context.Guild.GetTextChannel(settings.LogPostChannel);
            await logPostChannel.SendFileAsync(filepath, $"{context.Guild.Name}-users.conf");
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

            var logPostChannel = context.Guild.GetTextChannel(settings.LogPostChannel);
            await logPostChannel.SendFileAsync(filepath, $"{context.Guild.Name}-{channelString}.log");
            await ReplyAsync("Success!");
            return "SUCCESS";
        }

        private async Task AdminChannelSetAsync(string channelName)
        {
            var channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelSetId > 0)
            {
                _settings.AdminChannel = channelSetId;
                await FileHelper.SaveAllPresettingsAsync(_settings);
                await ReplyAsync($"Admin channel set to <#{channelSetId}>");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task AdminChannelGetAsync()
        {
            if (_settings.AdminChannel > 0)
            {
                await ReplyAsync($"Admin channel is <#{_settings.AdminChannel}>");
            }
            else
            {
                await ReplyAsync("Admin channel not set yet.");
            }
        }

        private async Task AdminRoleSetAsync(string roleName)
        {
            var roleSetId = DiscordHelper.GetRoleIdIfAccessAsync(roleName, Context);
            if (roleSetId > 0)
            {
                _settings.AdminRole = roleSetId;
                await FileHelper.SaveAllPresettingsAsync(_settings);
                await ReplyAsync($"Admin role set to <@&{roleSetId}>", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid role name @{roleName}.");
            }
        }

        private async Task AdminRoleGetAsync()
        {
            if (_settings.AdminRole > 0)
            {
                await ReplyAsync($"Admin role is <@&{_settings.AdminRole}>", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync("Admin role not set yet.");
            }
        }

        private async Task IgnoreChannelGetAsync()
        {
            if (_settings.IgnoredChannels.Count > 0)
            {
                var output = $"__Channel Ignore List:__{Environment.NewLine}";
                foreach (var channel in _settings.IgnoredChannels)
                {
                    output += $"<#{channel}>{Environment.NewLine}";
                }

                await ReplyAsync(output);
            }
            else
            {
                await ReplyAsync("No channels on ignore list.");
            }
        }

        private async Task IgnoreChannelRemoveAsync(string channelName)
        {
            var channelRemoveId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelRemoveId > 0)
            {
                for (var x = _settings.IgnoredChannels.Count - 1; x >= 0; x--)
                {
                    var channel = _settings.IgnoredChannels[x];
                    if (channel != channelRemoveId)
                    {
                        continue;
                    }

                    _settings.IgnoredChannels.Remove(channel);
                    await FileHelper.SaveAllPresettingsAsync(_settings);
                    await ReplyAsync($"Removed <#{channelRemoveId}> from ignore list.");
                    return;
                }

                await ReplyAsync($"<#{channelRemoveId}> was not on the list.");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task IgnoreChannelAddAsync(string channelName)
        {
            var channelAddId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelAddId > 0)
            {
                foreach (var channel in _settings.IgnoredChannels)
                {
                    if (channel != channelAddId)
                    {
                        continue;
                    }

                    await ReplyAsync($"<#{channelAddId}> is already on the list.");
                    return;
                }

                _settings.IgnoredChannels.Add(channelAddId);
                await FileHelper.SaveAllPresettingsAsync(_settings);
                await ReplyAsync($"Added <#{channelAddId}> to ignore list.");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task IgnoreRoleRemoveAsync(string roleName)
        {
            var roleRemoveId = DiscordHelper.GetRoleIdIfAccessAsync(roleName, Context);
            if (roleRemoveId > 0)
            {
                for (var x = _settings.IgnoredRoles.Count - 1; x >= 0; x--)
                {
                    var role = _settings.IgnoredRoles[x];
                    if (role != roleRemoveId)
                    {
                        continue;
                    }

                    _settings.IgnoredRoles.Remove(role);
                    await FileHelper.SaveAllPresettingsAsync(_settings);
                    await ReplyAsync($"Removed <@&{roleRemoveId}> from ignore list.", allowedMentions: AllowedMentions.None);
                    return;
                }

                await ReplyAsync($"<@&{roleRemoveId}> was not on the list.", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid channel name @{roleName}.", allowedMentions: AllowedMentions.None);
            }
        }

        private async Task IgnoreRoleAddAsync(string roleName)
        {
            var roleAddId = DiscordHelper.GetRoleIdIfAccessAsync(roleName, Context);
            if (roleAddId > 0)
            {
                foreach (var role in _settings.IgnoredRoles)
                {
                    if (role != roleAddId)
                    {
                        continue;
                    }

                    await ReplyAsync($"<@&{roleAddId}> is already on the list.", allowedMentions: AllowedMentions.None);
                    return;
                }

                _settings.IgnoredRoles.Add(roleAddId);
                await FileHelper.SaveAllPresettingsAsync(_settings);
                await ReplyAsync($"Added <@&{roleAddId}> to ignore list.", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid role name @{roleName}.", allowedMentions: AllowedMentions.None);
            }
        }

        private async Task IgnoreRoleGetAsync()
        {
            if (_settings.IgnoredRoles.Count > 0)
            {
                var output = $"__Role Ignore List:__{Environment.NewLine}";
                foreach (var role in _settings.IgnoredRoles)
                {
                    output += $"<@&{role}>{Environment.NewLine}";
                }

                await ReplyAsync(output, allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync("No roles on ignore list.");
            }
        }

        private async Task AllowUserRemoveAsync(string userName)
        {
            var userRemoveId = await DiscordHelper.GeUserIdFromPingOrIfOnlySearchResultAsync(userName, Context);
            if (userRemoveId > 0)
            {
                for (var x = _settings.AllowedUsers.Count - 1; x >= 0; x--)
                {
                    var user = _settings.AllowedUsers[x];
                    if (user != userRemoveId)
                    {
                        continue;
                    }

                    _settings.AllowedUsers.Remove(user);
                    await FileHelper.SaveAllPresettingsAsync(_settings);
                    await ReplyAsync($"Removed <@{userRemoveId}> from allow list.", allowedMentions: AllowedMentions.None);
                    return;
                }

                await ReplyAsync($"<@{userRemoveId}> was not on the list.", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid user name @{userName}.", allowedMentions: AllowedMentions.None);
            }
        }

        private async Task AllowUserAddAsync(string userName)
        {
            var userAddId = await DiscordHelper.GeUserIdFromPingOrIfOnlySearchResultAsync(userName, Context);
            if (userAddId > 0)
            {
                foreach (var user in _settings.AllowedUsers)
                {
                    if (user != userAddId)
                    {
                        continue;
                    }

                    await ReplyAsync($"<@{userAddId}> is already on the list.", allowedMentions: AllowedMentions.None);
                    return;
                }

                _settings.AllowedUsers.Add(userAddId);
                await FileHelper.SaveAllPresettingsAsync(_settings);
                await ReplyAsync($"Added <@{userAddId}> to allow list.", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid user name @{userName}.", allowedMentions: AllowedMentions.None);
            }
        }

        private async Task AllowUserGetAsync()
        {
            if (_settings.AllowedUsers.Count > 0)
            {
                var output = $"__Allowed User List:__{Environment.NewLine}";
                foreach (var user in _settings.AllowedUsers)
                {
                    output += $"<@{user}>{Environment.NewLine}";
                }

                await ReplyAsync(output, allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync("No users on allow list.");
            }
        }

        private async Task LogChannelClearAsync()
        {
            _settings.LogPostChannel = _settings.AdminChannel;
            await FileHelper.SaveAllPresettingsAsync(_settings);
            await ReplyAsync($"Report alert channel reset to the current admin channel, <#{_settings.LogPostChannel}>");
        }

        private async Task LogChannelSetAsync(string channelName)
        {
            var channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelSetId > 0)
            {
                _settings.LogPostChannel = channelSetId;
                await FileHelper.SaveAllPresettingsAsync(_settings);
                await ReplyAsync($"Retrieved logs will be sent to <#{channelSetId}>");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task LogChannelGetAsync()
        {
            if (_settings.LogPostChannel > 0)
            {
                await ReplyAsync($"Logs are being posted in <#{_settings.LogPostChannel}>");
            }
            else
            {
                await ReplyAsync("Log posting channel not set yet.");
            }
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

                if (_settings.LogPostChannel <= 0)
                {
                    return "<ERROR> Log post channel not set.";
                }

                var filepath = FileHelper.SetUpFilepath(FilePathType.LogRetrieval, date, "log", context, confirmedName, date);
                if (!File.Exists(filepath))
                {
                    return "<ERROR> File does not exist";
                }

                var logPostChannel = context.Guild.GetTextChannel(_settings.LogPostChannel);
                await logPostChannel.SendFileAsync(filepath, $"{confirmedName}-{date}.log");
                return "SUCCESS";
            }
        }
    }
}
