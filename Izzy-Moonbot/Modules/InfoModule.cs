using System.Linq;
using Izzy_Moonbot.Attributes;
using Microsoft.AspNetCore.Http;

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
        private readonly ModLoggingService _modLogging;
        private readonly ServerSettings _settings;
        private readonly RaidService _raid;
        private readonly ModService _mod;
        private readonly PressureService _pressureService;
        private readonly CommandService _commands;
        private readonly ScheduleService _scheduleService;
        private readonly Dictionary<ulong, User> _users;

        public InfoModule(LoggingService logger, ModLoggingService modLogging, ServerSettings settings, ModService mod, PressureService pressureService, CommandService commands, ScheduleService scheduleService, Dictionary<ulong, User> users, RaidService raid)
        {
            _logger = logger;
            _modLogging = modLogging;
            _settings = settings;
            _mod = mod;
            _pressureService = pressureService;
            _commands = commands;
            _scheduleService = scheduleService;
            _users = users;
            _raid = raid;
        }

        [Command("test")]
        [Summary("Unit tests for Izzy Moonbow")]
        public async Task TestCommandAsync([Summary("Test Identifier")] string testId = "", [Remainder][Summary("Test arguments")] string argString = "")
        {
            var args = argString.Split(" ");
            switch (testId)
            {
                case "pagination":
                    string[] pages =
                        "Hello!||This is a test of pagination!||If this works, you're able to see this.||The paginated message will expire in 5 minutes.||Hopefully my code isn't broken..."
                            .Split("||");
                    string[] staticParts =
                        $"**Test utility** - Pagination test{Environment.NewLine}*This is a simple test for the pagination utility!*{Environment.NewLine}*This is a header which will remain regardless of the current page.*{Environment.NewLine}Below is the paginated content.||This is the footer of the pagination message which will remain regardless of the current page{Environment.NewLine}There is a countdown below as well as buttons to change the page."
                            .Split("||");

                    var paginationHelper = new PaginationHelper(Context, pages, staticParts, 0);
                    break;
                case "pressure-hook":
                    Context.Message.ReplyAsync(
                        $"**Test utility** - Pressure hookin test.{Environment.NewLine}*Other services or modules can hook into the pressure service to do specific things.*{Environment.NewLine}*An example of this is getting pressure for a user.*{Environment.NewLine}*Like, your current pressure is `{await _pressureService.GetPressure(Context.User.Id)}`*");
                    break;
                case "dump-users-size":
                    Context.Message.ReplyAsync($"UserStore size: {_users.Count}");
                    break;
                case "create-echo-task":
                    var action = _scheduleService.stringToAction(
                        $"echo in {Context.Channel.Id} content Hello! Exactly 1 minute should have passed between the test command and this message!");
                    var task = new ScheduledTask(DateTimeOffset.UtcNow,
                        (DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1)), action);
                    await _scheduleService.CreateScheduledTask(task, Context.Guild);
                    await Context.Message.ReplyAsync("Created scheduled task.");
                    break;
                case "test-twilight":
                    await Context.Channel.SendMessageAsync(
                        $"Dear Princess Twilight,{Environment.NewLine}```{Environment.NewLine}" +
                        $"[2022-07-30 00:19:07 ERR] Izzy Moonbot has encountered an error. Logging information...{Environment.NewLine}" +
                        $"[2022-07-30 00:19:07 ERR] Message: Server requested a reconnect{Environment.NewLine}" +
                        $"[2022-07-30 00:19:07 ERR] Source: System.Private.CoreLib{Environment.NewLine}" +
                        $"[2022-07-30 00:19:07 ERR] HResult: -2146233088{Environment.NewLine}" +
                        $"[2022-07-30 00:19:07 ERR] Stack trace:    at Discord.ConnectionManager.<>c__DisplayClass29_0.<<StartAsync>b__0>d.MoveNext()" +
                        $"{Environment.NewLine}```Your faithful Bot,{Environment.NewLine}Izzy Moonbot");
                    break;
                case "twilight":
                    await Context.Guild.GetTextChannel(1002687344199094292).SendMessageAsync(
                        $"Dear Princess Twilight,{Environment.NewLine}```{Environment.NewLine}" +
                        $"[2022-07-30 00:19:07 ERR] Izzy Moonbot has encountered an error. Logging information...{Environment.NewLine}" +
                        $"[2022-07-30 00:19:07 ERR] Message: Server requested a reconnect{Environment.NewLine}" +
                        $"[2022-07-30 00:19:07 ERR] Source: System.Private.CoreLib{Environment.NewLine}" +
                        $"[2022-07-30 00:19:07 ERR] HResult: -2146233088{Environment.NewLine}" +
                        $"[2022-07-30 00:19:07 ERR] Stack trace:    at Discord.ConnectionManager.<>c__DisplayClass29_0.<<StartAsync>b__0>d.MoveNext()" +
                        $"{Environment.NewLine}```Your faithful Bot,{Environment.NewLine}Izzy Moonbot");
                    
                    break;
                case "immediate-log":
                    await _modLogging.CreateActionLog(Context.Guild)
                        .SetActionType(LogType.Notice)
                        .SetReason(
                            $"This is a test of the new ModLoggingService.{Environment.NewLine}This should log immediatly.{Environment.NewLine}Run the `batch-log` test to test batch logging.")
                        .Send();
                    break;
                case "batch-log":
                    _settings.BatchSendLogs = true;
                    await FileHelper.SaveSettingsAsync(_settings);
                    await _modLogging.CreateActionLog(Context.Guild)
                        .SetActionType(LogType.Notice)
                        .SetReason(
                            $"This is a test of the new ModLoggingService.{Environment.NewLine}This should log in batch with several Mod log types.{Environment.NewLine}Run the `immediate-log` test to test immediate logging.")
                        .Send();
                    await _modLogging.CreateModLog(Context.Guild)
                        .SetContent("Mod log #1")
                        .Send();
                    await _modLogging.CreateModLog(Context.Guild)
                        .SetContent("Mod log #2")
                        .Send();
                    await _modLogging.CreateModLog(Context.Guild)
                        .SetContent("Mod log #3")
                        .Send();
                    await _modLogging.CreateModLog(Context.Guild)
                        .SetContent("Mod log #4")
                        .Send();
                    await _modLogging.CreateModLog(Context.Guild)
                        .SetContent("Mod log #5")
                        .Send();
                    break;
                case "import-filter":
                    var toFilter = Context.Message.ReferencedMessage.CleanContent.Split(Environment.NewLine).AsEnumerable();
                    if (args[1] == "no") toFilter = toFilter.Skip(1);
                    else toFilter = toFilter.Skip(2);

                    var msg = await ReplyAsync(
                        $"Confirm: Import the list of words you replied to into the `{args[0]}` list? Checking reactions in 10 seconds.");
                    await msg.AddReactionAsync(Emoji.Parse("✅"));
                    Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(Convert.ToInt32(10000));
                        var users = msg.GetReactionUsersAsync(Emoji.Parse("✅"), 2);
                        if (users.AnyAsync(users =>
                            {
                                return users.Any(user => user.Id == Context.User.Id) ? true : false;
                            }).Result)
                        {
                            await msg.RemoveAllReactionsAsync();
                            await msg.ModifyAsync(message => message.Content = "⚠  **Importing. Please wait...**");
                            
                            _settings.FilteredWords[args[1]].AddRange(toFilter);
                            
                            await FileHelper.SaveSettingsAsync(_settings);
                            await msg.ModifyAsync(message => message.Content = "⚠  **Done!**");
                        }
                    });

                    break;
                case "changelog":
                    await _modLogging.CreateActionLog(Context.Guild)
                        .SetActionType(LogType.VerificationLevel)
                        .SetChangelog("Old content", "New content")
                        .SetReason(
                            $"Testing changelog logging (used for changing server settings e.g. verification level)")
                        .Send();
                    break;
                case "raid":
                    // Simulates a raid.
                    // args[0] is time in seconds between joins
                    // rest is user ids.
                    var timePeriod = Convert.ToInt32(args[0]) * 1000;
                    var users = args.Skip(1).Select(user =>
                    {
                        if (ulong.TryParse(user, out var id))
                        {
                            return Context.Guild.GetUser(id);
                        }
                        return null;
                    }).Where(user =>
                    {
                        if (user == null) return false;
                        return true;
                    });

                    var raidMsg = await ReplyAsync(
                        $"Confirm: Simulate {users.Count()} users joining {timePeriod} milliseconds apart? Checking reactions in 10 seconds.");
                    await raidMsg.AddReactionAsync(Emoji.Parse("✅"));
                    Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(Convert.ToInt32(10000));
                        var raidMsgUsers = raidMsg.GetReactionUsersAsync(Emoji.Parse("✅"), 2);
                        if (raidMsgUsers.AnyAsync(raidMsgUsersActual =>
                            {
                                return raidMsgUsersActual.Any(user => user.Id == Context.User.Id) ? true : false;
                            }).Result)
                        {
                            await raidMsg.RemoveAllReactionsAsync();
                            await raidMsg.ModifyAsync(message => message.Content = "⚠  **Executing...**");

                            Task.Factory.StartNew(async () =>
                            {
                                foreach (var user in users)
                                {
                                    await Task.Delay(timePeriod);
                                    await _raid.ProcessMemberJoin(user);
                                }
                            });

                            await raidMsg.ModifyAsync(message =>
                                message.Content = "⚠  **Executed. Expect raid alarms if hit.**");
                        }
                        else
                        {
                            await raidMsg.RemoveAllReactionsAsync();
                            await raidMsg.ModifyAsync(message => message.Content = "⚠  **Cancelled.**");
                        }
                    });
                    break;
                default:
                    Context.Message.ReplyAsync("Unknown test.");
                    break;
            }
        }

        [Command("help")]
        [Summary("Lists all commands")]
        public async Task HelpCommandAsync([Remainder][Summary("The command/module you want to look at.")] string item = "")
        {
            var prefix = _settings.Prefix;

            if (item == "")
            {
                // List modules.
                var moduleList = new List<string>();

                foreach (var module in _commands.Modules)
                {
                    if (module.IsSubmodule) continue;
                    var moduleInfo = $"{module.Name} - {module.Summary}";
                    foreach (var submodule in module.Submodules)
                    {
                        moduleInfo += $"{Environment.NewLine}    {submodule.Name} - {submodule.Summary}";
                    }
                    
                    moduleList.Add(moduleInfo);
                }

                await ReplyAsync(
                    $"Hii! Here's a list of all modules!{Environment.NewLine}```{Environment.NewLine}{string.Join(Environment.NewLine, moduleList)}{Environment.NewLine}```Run `{prefix}help <module>` for commands in that module.");
            }
            else
            {
                if (_commands.Commands.Any(command => command.Name == item))
                {
                    // It's a command!
                    var commandInfo = _commands.Commands.Single<CommandInfo>(command => command.Name == item);
                    string ponyReadable = $"**{prefix}{commandInfo.Name}** - {commandInfo.Summary}{Environment.NewLine}";
                    if (commandInfo.Preconditions.Any(attribute => attribute is ModCommandAttribute))
                        ponyReadable += $"ℹ  *This is a moderator only command.*{Environment.NewLine}";
                    if (commandInfo.Preconditions.Any(attribute => attribute is DevCommandAttribute))
                        ponyReadable += $"ℹ  *This is a developer only command.*{Environment.NewLine}";

                    ponyReadable += $"```{Environment.NewLine}";

                    foreach (var parameters in commandInfo.Parameters)
                    {
                        ponyReadable += $"{parameters.Name} [{parameters.Type.Name}] - {parameters.Summary}{Environment.NewLine}";
                    }

                    ponyReadable += "```";

                    await ReplyAsync(ponyReadable);
                } 
                else
                {
                    // Module.
                    if (_commands.Modules.Any(module => module.Name == item))
                    {
                        // It's a command!
                        var moduleInfo = _commands.Modules.Single<ModuleInfo>(module => module.Name == item);

                        var commands = moduleInfo.Commands.Select<CommandInfo, string>(command =>
                        {
                            return $"{prefix}{command.Name} - {command.Summary}";
                        }).ToList();
                        
                        if (commands.Count > 10)
                        {
                            // Use pagination
                            var pages = new List<string>();
                            var pageNumber = -1;
                            for (var i = 0; i < commands.Count; i++)
                            {
                                if (i % 10 == 0)
                                {
                                    pageNumber += 1;
                                    pages.Add("");
                                }

                                pages[pageNumber] += commands[i] + Environment.NewLine;
                            }


                            string[] staticParts =
                            {
                                $"**List of commands in {item}.",
                                ""
                            };

                            var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                        }
                        else
                        {
                            await ReplyAsync(
                                $"**List of commands in {item}**{Environment.NewLine}```{Environment.NewLine}{string.Join(Environment.NewLine, commands)}{Environment.NewLine}```");
                        }
                    }
                }
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
