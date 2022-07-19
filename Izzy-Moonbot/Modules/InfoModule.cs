namespace Izzy_Moonbot.Modules
{
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Izzy_Moonbot.Helpers;
    using Izzy_Moonbot.Service;
    using Izzy_Moonbot.Settings;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    [Summary("Module for providing information")]
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        private readonly LoggingService _logger;
        private readonly ServerSettings _settings;
        private readonly ModService _mod;
        private readonly PressureService _pressureService;
        private readonly ScheduleService _scheduleService;
        private readonly Dictionary<ulong, User> _users;

        public InfoModule(LoggingService logger, ServerSettings settings, ModService mod, PressureService pressureService, ScheduleService scheduleService, Dictionary<ulong, User> users)
        {
            _logger = logger;
            _settings = settings;
            _mod = mod;
            _pressureService = pressureService;
            _scheduleService = scheduleService;
            _users = users;
        }

        [Command("test")]
        [Summary("maybe end the world")]
        public async Task TestCommandAsync([Remainder][Summary("test id")] string testId = "")
        {
            switch (testId)
            {
                case "pagination":
                    string[] pages =
                        "Hello!||This is a test of pagination!||If this works, you're able to see this.||The paginated message will expire in 5 minutes.||Hopefully my code isn't broken..."
                            .Split("||");
                    string[] staticParts =
                        $"**Test utility** - Pagination test{Environment.NewLine}*This is a simple test for the pagination utility!*{Environment.NewLine}*This is a header which will remain regardless of the current page.*{Environment.NewLine}Below is the paginated content.||This is the footer of the pagination message which will remain regardless of the current page{Environment.NewLine}There is a countdown below as well as buttons to change the page."
                            .Split("||");

                    PaginationHelper paginationHelper = new PaginationHelper(Context, pages, staticParts, 0);
                    break;
                case "pressure-hook":
                    Context.Message.ReplyAsync(
                        $"**Test utility** - Pressure hookin test.{Environment.NewLine}*Other services or modules can hook into the pressure service to do specific things.*{Environment.NewLine}*An example of this is getting pressure for a user.*{Environment.NewLine}*Like, your current pressure is `{await _pressureService.GetPressure(Context.User.Id)}`*");
                    break;
                case "nocontext-log":
                    _logger.Log("no context log test", null, false);
                    break;
                case "dump-users-size":
                    Context.Message.ReplyAsync($"UserStore size: {_users.Count}");
                    break;
                case "addition":
                    Context.Message.ReplyAsync($"{0.1 + 0.2}");
                    break;
                case "stop":
                    await Context.Client.StopAsync();
                    break;
                case "create-echo-task":
                    ScheduledTaskAction action = _scheduleService.stringToAction(
                        $"echo in {Context.Channel.Id} content Hello! Exactly 1 minute should have passed between the test command and this message!");
                    ScheduledTask task = new ScheduledTask(DateTimeOffset.UtcNow,
                        (DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1)), action);
                    await _scheduleService.CreateScheduledTask(task, Context.Guild);
                    await Context.Message.ReplyAsync("Created scheduled task.");
                    break;
                default:
                    Context.Message.ReplyAsync("Unknown test.");
                    break;
            }
        }

        [Command("help")]
        [Summary("Lists all commands")]
        public async Task HelpCommandAsync([Summary("First subcommand")] string command = "", [Remainder][Summary("Second subcommand")] string subCommand = "")
        {
            if (!DiscordHelper.CanUserRunThisCommand(Context, _settings))
            {
                return;
            }

            char prefix = _settings.Prefix;

            await _logger.Log($"help {command} {subCommand}", Context);

            switch (command)
            {
                case "":
                    await ReplyAsync(
                        $"**__All Commands:__**{Environment.NewLine}**Admin Module:**{Environment.NewLine}`{prefix}setup ...`{Environment.NewLine}`{prefix}admin ...`{Environment.NewLine}`{prefix}log ...`{Environment.NewLine}`{prefix}echo ...`{Environment.NewLine}`{prefix}setprefix ...`{Environment.NewLine}`{prefix}listentobots ...`{Environment.NewLine}`{prefix}alias ...`{Environment.NewLine}`{prefix}getsettings`{Environment.NewLine}**Info Module:**{Environment.NewLine}`{prefix}about`{Environment.NewLine}{Environment.NewLine}Use `{prefix}help <command>` for more details on a particular command.{Environment.NewLine}Type '<@{Context.Client.CurrentUser.Id}> <message>' with the ping and any message if you forget the prefix. Yes, I know you needed to know the prefix to see this message, but try to remember in case someone else asks, ok?");
                    break;
                case "setup":
                    await ReplyAsync(
                        $"`{prefix}setup <admin channel> <admin role>`{Environment.NewLine}*Only a server administrator may use this command.*{Environment.NewLine}Initial bot setup. Sets <admin channel> for important admin output messages and <admin role> as users who are allowed to use admin module commands.");
                    break;
                case "admin":
                    switch (subCommand)
                    {
                        case "":
                            await ReplyAsync(
                                $"**__{prefix}admin Commands:__**{Environment.NewLine}*Only users with the specified admin role may use these commands*{Environment.NewLine}`{prefix}admin adminchannel ...`{Environment.NewLine}`{prefix}admin adminrole ...`{Environment.NewLine}`{prefix}admin ignorechannel ...`{Environment.NewLine}`{prefix}admin ignorerole ...`{Environment.NewLine}`{prefix}admin allowuser ...`{Environment.NewLine}`{prefix}admin logchannel ...`{Environment.NewLine}{Environment.NewLine}Use `{prefix}help admin <command>` for more details on a particular command.");
                            break;
                        case "adminchannel":
                            await ReplyAsync(
                                $"__{prefix}admin adminchannel Commands:__{Environment.NewLine}*Manages the admin channel.*{Environment.NewLine}`{prefix}admin adminchannel get` Gets the current admin channel.{Environment.NewLine}`{prefix}admin adminchannel set <channel>` Sets the admin channel to <channel>. Accepts a channel ping or plain text.");
                            break;
                        case "adminrole":
                            await ReplyAsync(
                                $"__{prefix}admin adminrole Commands:__{Environment.NewLine}*Manages the admin role.*{Environment.NewLine}`{prefix}admin adminrole get` Gets the current admin role.{Environment.NewLine}`{prefix}admin adminrole set <role>` Sets the admin role to <role>. Accepts a role ping or plain text.");
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
                                $"__{prefix}admin allowuser Commands:__{Environment.NewLine}*Manages the list of users to allow commands from.*{Environment.NewLine}`{prefix}admin allowuser get` Gets the current list of allowd users.{Environment.NewLine}`{prefix}admin allowuser add <user>` Adds <user> to the list of allowd users. Accepts a user ping or plain text.{Environment.NewLine}`{prefix}admin allowuser remove <user>` Removes <user> from the list of allowed users. Accepts a user ping or plain text.{Environment.NewLine}`{prefix}admin allowuser clear` Clears the list of allowed users.");
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
                case "alias":
                    await ReplyAsync(
                        $"**__{prefix}alias Commands:__**{Environment.NewLine}*Only users with the specified admin role may use these commands.*{Environment.NewLine}Manages the list of command aliases.{Environment.NewLine}`{prefix}alias add <short> <long>` Sets <short> as an alias of <long>. If a command starts with <short>, <short> is replaced with <long> and the command is then processed normally. Do not include prefixes in <short> or <long>. Example: `{prefix}alias cute pick cute` sets `{prefix}cute` to run `{prefix}pick cute` instead. To use an alias that includes spaces, surround the entire <short> term with \"\" quotes. If an alias for <short> already exists, it replaces the previous value of <long> with the new one.{Environment.NewLine}`{prefix}alias remove <short>` Removes <short> as an alias for anything.{Environment.NewLine}`{prefix}alias get` Gets the current list of aliases.{Environment.NewLine}`{prefix}alias clear` Clears all aliases.");
                    break;
                case "getsettings":
                    await ReplyAsync(
                        $"`{prefix}getsettings`{Environment.NewLine}*Only users with the specified admin role may use this command.*{Environment.NewLine}Posts the settings file to the log channel.");
                    break;
                case "about":
                    await ReplyAsync($"`{prefix}about` Information about this bot.");
                    break;
                case "help":
                    await ReplyAsync("<:sweetiegrump:667949401343524883>");
                    break;
                default:
                    await ReplyAsync($"Invalid command. Use `{prefix}help` for a list of available commands.");
                    break;
            }
        }

        [Command("about")]
        [Summary("About the bot")]
        public async Task AboutCommandAsync()
        {
            if (!DiscordHelper.CanUserRunThisCommand(Context, _settings))
            {
                return;
            }

            await _logger.Log("about", Context);
            await ReplyAsync(
                allowedMentions: AllowedMentions.None);
        }
    }
}
