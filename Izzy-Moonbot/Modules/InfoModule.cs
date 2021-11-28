namespace Izzy_Moonbot.Modules
{
    using System;
    using System.Threading.Tasks;
    using Izzy_Moonbot.Helpers;
    using Izzy_Moonbot.Service;
    using Izzy_Moonbot.Settings;
    using Discord;
    using Discord.Commands;

    [Summary("Module for providing information")]
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        private readonly LoggingService _logger;
        private readonly AllPreloadedSettings _servers;

        public InfoModule(LoggingService logger, AllPreloadedSettings servers)
        {
            _logger = logger;
            _servers = servers;
        }

        [Command("help")]
        [Summary("Lists all commands")]
        public async Task HelpCommandAsync([Summary("First subcommand")] string command = "", [Remainder] [Summary("Second subcommand")] string subCommand = "")
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            var prefix = ';';
            var serverPresettings = await FileHelper.LoadServerPresettingsAsync(Context);
            prefix = serverPresettings.prefix;

            await _logger.Log($"help {command} {subCommand}", Context);

            switch (command)
            {
                case "":
                    await ReplyAsync(
                        $"**__All Commands:__**{Environment.NewLine}**Booru Module:**{Environment.NewLine}`{prefix}pick ...`{Environment.NewLine}`{prefix}pickrecent ...`{Environment.NewLine}`{prefix}id ...`{Environment.NewLine}`{prefix}tags ...`{Environment.NewLine}`{prefix}featured`{Environment.NewLine}`{prefix}getspoilers`{Environment.NewLine}`{prefix}report ...`{Environment.NewLine}**Admin Module:**{Environment.NewLine}`{prefix}setup ...`{Environment.NewLine}`{prefix}admin ...`{Environment.NewLine}`{prefix}yellowlist ...`{Environment.NewLine}`{prefix}log ...`{Environment.NewLine}`{prefix}echo ...`{Environment.NewLine}`{prefix}setprefix ...`{Environment.NewLine}`{prefix}listentobots ...`{Environment.NewLine}`{prefix}safemode ...`{Environment.NewLine}`{prefix}alias ...`{Environment.NewLine}`{prefix}getsettings`{Environment.NewLine}`{prefix}refreshlists`{Environment.NewLine}**Info Module:**{Environment.NewLine}`{prefix}origin`{Environment.NewLine}`{prefix}about`{Environment.NewLine}{Environment.NewLine}Use `{prefix}help <command>` for more details on a particular command.{Environment.NewLine}Type '<@{Context.Client.CurrentUser.Id}> <message>' with the ping and any message if you forget the prefix. Yes, I know you needed to know the prefix to see this message, but try to remember in case someone else asks, ok?");
                    break;
                case "pick":
                    await ReplyAsync(
                        $"`{prefix}pick <query>`{Environment.NewLine}Posts a random image from a Manebooru <query>, if it is available. Each different search term in the query is separated by a comma. If results include any spoilered tags, the post is made in `||` spoiler bars.");
                    break;
                case "pickrecent":
                    await ReplyAsync(
                        $"`{prefix}pickrecent <query>`{Environment.NewLine}Posts the most recently posted image from a Manebooru <query>, if it is available. Each different search term in the query is separated by a comma. If results include any spoilered tags, the post is made in `||` spoiler bars.");
                    break;
                case "id":
                    await ReplyAsync(
                        $"`{prefix}id <number>`{Environment.NewLine}Posts Image #<number> from Manebooru, if it is available. If the image includes spoilered tags, the post is made in `||` spoiler bars.");
                    break;
                case "tags":
                    await ReplyAsync(
                        $"`{prefix}tags <number>`{Environment.NewLine}Posts the list of tags on Image <number> from Manebooru, if it is available, including identifying any tags that are spoilered.");
                    break;
                case "featured":
                    await ReplyAsync($"`{prefix}featured`{Environment.NewLine}Posts the current Featured Image from Manebooru.");
                    break;
                case "getspoilers":
                    await ReplyAsync($"`{prefix}getspoilers`{Environment.NewLine}Posts a list of currently spoilered tags.");
                    break;
                case "report":
                    await ReplyAsync(
                        $"`{prefix}report <id> <reason>`{Environment.NewLine}Alerts the admins about image #<id> with an optional <reason> for the admins to see. Only use this for images that violate the server rules!");
                    break;
                case "setup":
                    await ReplyAsync(
                        $"`{prefix}setup <filter ID> <admin channel> <admin role>`{Environment.NewLine}*Only a server administrator may use this command.*{Environment.NewLine}Initial bot setup. Sets <filter ID> as the public Manebooru filter to use, <admin channel> for important admin output messages, and <admin role> as users who are allowed to use admin module commands. Validates that <Filter ID> is useable and if not, uses Filter 175.");
                    break;
                case "admin":
                    switch (subCommand)
                    {
                        case "":
                            await ReplyAsync(
                                $"**__{prefix}admin Commands:__**{Environment.NewLine}*Only users with the specified admin role may use these commands*{Environment.NewLine}`{prefix}admin filter ...`{Environment.NewLine}`{prefix}admin adminchannel ...`{Environment.NewLine}`{prefix}admin adminrole ...`{Environment.NewLine}`{prefix}admin filterchannel ...`{Environment.NewLine}`{prefix}admin ignorechannel ...`{Environment.NewLine}`{prefix}admin ignorerole ...`{Environment.NewLine}`{prefix}admin allowuser ...`{Environment.NewLine}`{prefix}admin yellowchannel ...`{Environment.NewLine}`{prefix}admin yellowrole ...`{Environment.NewLine}`{prefix}admin redchannel ...`{Environment.NewLine}`{prefix}admin redrole ...`{Environment.NewLine}`{prefix}admin reportchannel ...`{Environment.NewLine}`{prefix}admin reportrole ...`{Environment.NewLine}`{prefix}admin logchannel ...`{Environment.NewLine}{Environment.NewLine}Use `{prefix}help admin <command>` for more details on a particular command.");
                            break;
                        case "filter":
                            await ReplyAsync(
                                $"__{prefix}admin filter Commands:__{Environment.NewLine}*Manages the active filter.*{Environment.NewLine}`{prefix}admin filter get` Gets the current active filter.{Environment.NewLine}`{prefix}admin filter set <filter ID>` Sets the active filter to <Filter ID>. Validates that the filter is useable by the bot.");
                            break;
                        case "adminchannel":
                            await ReplyAsync(
                                $"__{prefix}admin adminchannel Commands:__{Environment.NewLine}*Manages the admin channel.*{Environment.NewLine}`{prefix}admin adminchannel get` Gets the current admin channel.{Environment.NewLine}`{prefix}admin adminchannel set <channel>` Sets the admin channel to <channel>. Accepts a channel ping or plain text.");
                            break;
                        case "adminrole":
                            await ReplyAsync(
                                $"__{prefix}admin adminrole Commands:__{Environment.NewLine}*Manages the admin role.*{Environment.NewLine}`{prefix}admin adminrole get` Gets the current admin role.{Environment.NewLine}`{prefix}admin adminrole set <role>` Sets the admin role to <role>. Accepts a role ping or plain text.");
                            break;
                        case "filterchannel":
                            await ReplyAsync(
                                $"__{prefix}admin filterchannel Commands:__{Environment.NewLine}*Manages the list of channel-specific filters. NOTE: red and yellow list checks are disabled for any channels on this list!*{Environment.NewLine}`{prefix}admin filterchannel get` Gets the current list of channel-specific filters.{Environment.NewLine}`{prefix}admin filterchannel add <channel> <filterId>` Sets <channel> to use filter #<filterId>. Validates the filter first. Accepts a channel ping or plain text.{Environment.NewLine}`{prefix}admin filterchannel remove <channel>` Removes <channel> from the list of channel-specific filters. This channel will now use the default server filter. Accepts a channel ping or plain text.{Environment.NewLine}`{prefix}admin filterchannel clear` Clears the list of channel-specific filters. All channels will use the default server filter.");
                            break;
                        case "ignorechannel":
                            await ReplyAsync(
                                $"__{prefix}admin ignorechannel Commands:__{Environment.NewLine}*Manages the list of channels to ignore commands from.*{Environment.NewLine}`{prefix}admin ignorechannel get` Gets the current list of ignored channels.{Environment.NewLine}`{prefix}admin ignorechannel add <channel>` Adds <channel> to the list of ignored channels. Accepts a channel ping or plain text.{Environment.NewLine}`{prefix}admin ignorechannel remove <channel>` Removes <channel> from the list of ignored channels. Accepts a channel ping or plain text.{Environment.NewLine}`{prefix}admin ignorechannel clear` Clears the list of ignored channels.");
                            break;
                        case "ignorerole":
                            await ReplyAsync(
                                $"__{prefix}admin ignorerole Commands:__{Environment.NewLine}*Manages the list of roles to ignore commands from.*{Environment.NewLine}`{prefix}admin ignorerole get` Gets the current list of ignored roles.{Environment.NewLine}`{prefix}admin ignorerole add <role>` Adds <role> to the list of ignored roles. Accepts a role ping or plain text.{Environment.NewLine}`{prefix}admin ignorerole remove <role>` Removes <role> from the list of ignored roles. Accepts a role ping or plain text.{Environment.NewLine}`{prefix}admin ignorerole clear` Clears the list of ignored roles.");
                            break;
                        case "allowuser":
                            await ReplyAsync(
                                $"__{prefix}admin allowuser Commands:__{Environment.NewLine}*Manages the list of users to allow commands from.*{Environment.NewLine}`{prefix}admin allowuser get` Gets the current list of allowd users.{Environment.NewLine}`{prefix}admin allowuser add <user>` Adds <user> to the list of allowd users. Accepts a user ping or plain text.{Environment.NewLine}`{prefix}admin allowuser remove <user>` Removes <user> from the list of allowed users. Accepts a user ping or plain text.{Environment.NewLine}`{prefix}admin allowuser clear` Clears the list of allowd users.");
                            break;
                        case "yellowchannel":
                            await ReplyAsync(
                                $"__{prefix}admin yellowchannel Commands:__{Environment.NewLine}*Manages the yellow alert channel.*{Environment.NewLine}`{prefix}admin yellowchannel get` Gets the current yellow alert channel.{Environment.NewLine}`{prefix}admin yellowchannel set <channel>` Sets the yellow alert channel to <channel>. Accepts a channel ping or plain text.{Environment.NewLine}`{prefix}admin yellowchannel clear` Resets the yellow alert channel to the current admin channel.");
                            break;
                        case "yellowrole":
                            await ReplyAsync(
                                $"__{prefix}admin yellowrole Commands:__{Environment.NewLine}*Manages the yellow alert role.*{Environment.NewLine}`{prefix}admin yellowrole get` Gets the current yellow alert role.{Environment.NewLine}`{prefix}admin yellowrole set <role>` Sets the yellow alert role to <role> and turns pinging on. Accepts a role ping or plain text.{Environment.NewLine}`{prefix}admin yellowrole clear` Resets the yellow alert role to no role and turns pinging off.");
                            break;
                        case "redchannel":
                            await ReplyAsync(
                                $"__{prefix}admin redchannel Commands:__{Environment.NewLine}*Manages the red alert channel.*{Environment.NewLine}`{prefix}admin redchannel get` Gets the current red alert channel.{Environment.NewLine}`{prefix}admin redchannel set <channel>` Sets the red alert channel to <channel>. Accepts a channel ping or plain text.{Environment.NewLine}`{prefix}admin redchannel clear` Resets the red alert channel to the current admin channel.");
                            break;
                        case "redrole":
                            await ReplyAsync(
                                $"__{prefix}admin redrole Commands:__{Environment.NewLine}*Manages the red alert role.*{Environment.NewLine}`{prefix}admin redrole get` Gets the current red alert role.{Environment.NewLine}`{prefix}admin redrole set <role>` Sets the red alert role to <role> and turns pinging on. Accepts a role ping or plain text.{Environment.NewLine}`{prefix}admin redrole clear` Resets the red alert channel to no role and turns pinging off.");
                            break;
                        case "reportchannel":
                            await ReplyAsync(
                                $"__{prefix}admin reportchannel Commands:__{Environment.NewLine}*Manages the report alert channel.*{Environment.NewLine}`{prefix}admin reportchannel get` Gets the current report alert channel.{Environment.NewLine}`{prefix}admin reportchannel set <channel>` Sets the report alert channel to <channel>. Accepts a channel ping or plain text.{Environment.NewLine}`{prefix}admin reportchannel clear` Resets the report alert channel to the current admin channel.");
                            break;
                        case "reportrole":
                            await ReplyAsync(
                                $"__{prefix}admin reportrole Commands:__{Environment.NewLine}*Manages the report alert role.*{Environment.NewLine}`{prefix}admin reportrole get` Gets the current report alert role.{Environment.NewLine}`{prefix}admin reportrole set <role>` Sets the report alert role to <role> and turns pinging on. Accepts a role ping or plain text.{Environment.NewLine}`{prefix}admin reportrole clear` Resets the report alert channel to no role and turns pinging off.");
                            break;
                        case "logchannel":
                            await ReplyAsync(
                                $"__{prefix}admin logchannel Commands:__{Environment.NewLine}*Manages the log post channel.*{Environment.NewLine}`{prefix}admin logchannel get` Gets the current log post channel.{Environment.NewLine}`{prefix}admin logchannel set <channel>` Sets the log post channel to <channel>. Accepts a channel ping or plain text.{Environment.NewLine}`{prefix}admin logchannel clear` Resets the log post channel to the current admin channel.");
                            break;
                        default:
                            await ReplyAsync($"Invalid subcommand. Use `{prefix}help admin` for a list of available subcommands.");
                            break;
                    }

                    break;
                case "yellowlist":
                    await ReplyAsync(
                        $"**__{prefix}yellowlist Commands:__**{Environment.NewLine}*Only users with the specified admin role may use these commands.*{Environment.NewLine}Manages the list of terms users are unable to search for.{Environment.NewLine}`{prefix}yellowlist add <term>` Add <term> to the yellowlist. <term> may be a comma-separated list.{Environment.NewLine}`{prefix}yellowlist remove <term>` Removes <term> from the yellowlist.{Environment.NewLine}`{prefix}yellowlist get` Gets the current list of yellowlisted terms.{Environment.NewLine}`{prefix}yellowlist clear` Clears the yellowlist of all terms.");
                    break;
                case "log":
                    await ReplyAsync(
                        $"`{prefix}log <channel> <date>`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Posts the log file from <channel> and <date> into the admin channel. Accepts a channel ping or plain text. <date> must be formatted as YYYY-MM-DD.");
                    break;
                case "echo":
                    await ReplyAsync(
                        $"`{prefix}echo <channel> <message>`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Posts <message> to a valid <channel>. If <channel> is invalid, posts to the current channel instead. Accepts a channel ping or plain text.");
                    break;
                case "setprefix":
                    await ReplyAsync(
                        $"`{prefix}setprefix <prefix>`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Sets the prefix in front of commands to listen for to <prefix>. Accepts a single character.");
                    break;
                case "listentobots":
                    await ReplyAsync(
                        $"`{prefix}listentobots <pos/neg>`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Toggles whether or not to run commands posted by other bots. Accepts y/n, yes/no, on/off, or true/false.");
                    break;
                case "safemode":
                    await ReplyAsync(
                        $"`{prefix}safemode <pos/neg>`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Toggles whether or not to automatically append `safe` to all booru queries. This overrides any channel-specific filters! Accepts y/n, yes/no, on/off, or true/false.");
                    break;
                case "alias":
                    await ReplyAsync(
                        $"**__{prefix}alias Commands:__**{Environment.NewLine}*Only users with the specified admin role may use these commands.*{Environment.NewLine}Manages the list of command aliases.{Environment.NewLine}`{prefix}alias add <short> <long>` Sets <short> as an alias of <long>. If a command starts with <short>, <short> is replaced with <long> and the command is then processed normally. Do not include prefixes in <short> or <long>. Example: `{prefix}alias cute pick cute` sets `{prefix}cute` to run `{prefix}pick cute` instead. To use an alias that includes spaces, surround the entire <short> term with \"\" quotes. If an alias for <short> already exists, it replaces the previous value of <long> with the new one.{Environment.NewLine}`{prefix}alias remove <short>` Removes <short> as an alias for anything.{Environment.NewLine}`{prefix}alias get` Gets the current list of aliases.{Environment.NewLine}`{prefix}alias clear` Clears all aliases.");
                    break;
                case "getsettings":
                    await ReplyAsync(
                        $"`{prefix}getsettings`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Posts the settings file to the log channel. This includes the redlist.");
                    break;
                case "refreshlists":
                    await ReplyAsync(
                        $"`{prefix}refreshlists`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Rebuilds the spoiler list and redlist from the current active filter. This may take several minutes depending on how many tags are in there.");
                    break;
                case "origin":
                    await ReplyAsync($"`{prefix}origin`{Environment.NewLine}Posts the origin of Manebooru's cute kirin mascot and the namesake of this bot, Cloudy Canvas.");
                    break;
                case "about":
                    await ReplyAsync($"`{prefix}about` Information about this bot.");
                    break;
                case "help":
                    await ReplyAsync("<:sweetiegrump:642466824696627200>");
                    break;
                default:
                    await ReplyAsync($"Invalid command. Use `{prefix}help` for a list of available commands.");
                    break;
            }
        }

        [Command("origin")]
        [Summary("Displays the origin of Cloudy Canvas")]
        public async Task OriginCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            await _logger.Log("origin", Context);
            await ReplyAsync(
                $"Here is where I came from, thanks to ConfettiCakez!{Environment.NewLine}<https://www.deviantart.com/confetticakez>{Environment.NewLine}https://imgur.com/a/XUHhKz1");
        }

        [Command("about")]
        [Summary("Displays the origin of Cloudy Canvas")]
        public async Task AboutCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }
            
            await _logger.Log("about", Context);
            await ReplyAsync(
                $"**__Cloudy Canvas__** <:cloudywink:871146664893743155>{Environment.NewLine}<http://cloudycanvas.art/>{Environment.NewLine}Created April 5th, 2021{Environment.NewLine}A Discord bot for interfacing with the <:manebooru:871148109240102942> <https://manebooru.art/> imageboard.{Environment.NewLine}Currently active on {_servers.guildList.Count} servers.{Environment.NewLine}{Environment.NewLine}Written by Raymond Welch (<@221742476153716736>) in C# using Discord.net. Special thanks to Ember Heartshine for hosting.{Environment.NewLine}{Environment.NewLine}**GitHub:** <https://github.com/romulus4444/Cloudy-Canvas>",
                allowedMentions: AllowedMentions.None);
        }
    }
}
