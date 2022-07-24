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

                    var paginationHelper = new PaginationHelper(Context, pages, staticParts, 0);
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
                    var action = _scheduleService.stringToAction(
                        $"echo in {Context.Channel.Id} content Hello! Exactly 1 minute should have passed between the test command and this message!");
                    var task = new ScheduledTask(DateTimeOffset.UtcNow,
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
            var prefix = _settings.Prefix;

            
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
