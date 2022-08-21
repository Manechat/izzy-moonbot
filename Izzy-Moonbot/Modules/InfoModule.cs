using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Modules;

[Summary("Module for providing information")]
public class InfoModule : ModuleBase<SocketCommandContext>
{
    private readonly CommandService _commands;
    private readonly LoggingService _logger;
    private readonly ModService _mod;
    private readonly ModLoggingService _modLogging;
    private readonly PressureService _pressureService;
    private readonly RaidService _raid;
    private readonly ScheduleService _scheduleService;
    private readonly ServerSettings _settings;
    private readonly Dictionary<ulong, User> _users;
    private StateStorage _state;

    public InfoModule(LoggingService logger, StateStorage state, ModLoggingService modLogging, ServerSettings settings,
        ModService mod, PressureService pressureService, CommandService commands, ScheduleService scheduleService,
        Dictionary<ulong, User> users, RaidService raid)
    {
        _logger = logger;
        _state = state;
        _modLogging = modLogging;
        _settings = settings;
        _mod = mod;
        _pressureService = pressureService;
        _commands = commands;
        _scheduleService = scheduleService;
        _users = users;
        _raid = raid;
    }

    [Command("help")]
    [Summary("Lists all commands")]
    public async Task HelpCommandAsync(
        [Remainder] [Summary("The command/module you want to look at.")] string item = "")
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
                    moduleInfo += $"{Environment.NewLine}    {submodule.Name} - {submodule.Summary}";

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
                var ponyReadable = $"**{prefix}{commandInfo.Name}** - {commandInfo.Summary}{Environment.NewLine}";
                if (commandInfo.Preconditions.Any(attribute => attribute is ModCommandAttribute))
                    ponyReadable += $"ℹ  *This is a moderator only command.*{Environment.NewLine}";
                if (commandInfo.Preconditions.Any(attribute => attribute is DevCommandAttribute))
                    ponyReadable += $"ℹ  *This is a developer only command.*{Environment.NewLine}";

                ponyReadable += $"```{Environment.NewLine}";

                foreach (var parameters in commandInfo.Parameters)
                    ponyReadable +=
                        $"{parameters.Name} [{parameters.Type.Name}] - {parameters.Summary}{Environment.NewLine}";

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
        await ReplyAsync(
            $"Izzy Moonbot{Environment.NewLine}Programmed in C# with Virtual Studio and JetBrains Rider{Environment.NewLine}Programmed by Dr. Romulus#4444 and Cloudburst#0001 (Twi/Leah){Environment.NewLine}Supervisor programmed by MinteckPony#2245{Environment.NewLine}{Environment.NewLine}Profile picture by confetticakez#7352 (Confetti){Environment.NewLine}https://manebooru.art/images/4023149",
            allowedMentions: AllowedMentions.None);
    }
}