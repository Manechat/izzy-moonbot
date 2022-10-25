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
    private readonly Config _settings;

    public InfoModule(Config settings, CommandService commands)
    {
        _settings = settings;
        _commands = commands;
    }

    [Command("help")]
    [Summary("Lists all commands")]
    public async Task HelpCommandAsync(
        [Remainder] [Summary("The command/category you want to look at.")] string item = "")
    {
        var prefix = _settings.Prefix;

        if (item == "")
        {
            // List modules.
            var moduleList = new List<string>();

            foreach (var module in _commands.Modules)
            {
                if (module.IsSubmodule) continue;
                if (module.Name == "DevModule") continue; // Hide dev module
                var moduleInfo = $"{module.Name.Replace("Module", "")} - {module.Summary}";
                foreach (var submodule in module.Submodules)
                    moduleInfo += $"{Environment.NewLine}    {submodule.Name.Replace("Submodule", "")} - {submodule.Summary}";

                moduleList.Add(moduleInfo);
            }

            await ReplyAsync(
                $"Hii! Here's a list of all command categories!{Environment.NewLine}```{Environment.NewLine}{string.Join(Environment.NewLine, moduleList)}{Environment.NewLine}```Run `{prefix}help <category>` for commands in that category.");
        }
        else
        {
            if (_commands.Commands.Any(command => command.Name.ToLower() == item.ToLower()))
            {
                // It's a command!
                var commandInfo = _commands.Commands.Single<CommandInfo>(command => command.Name.ToLower() == item.ToLower());
                var ponyReadable = $"**{prefix}{commandInfo.Name}** - {commandInfo.Summary}{Environment.NewLine}";
                if (commandInfo.Preconditions.Any(attribute => attribute is ModCommandAttribute) &&
                    commandInfo.Preconditions.Any(attribute => attribute is DevCommandAttribute))
                    ponyReadable += $"ℹ  *This is a moderator and developer only command.*{Environment.NewLine}";
                else if (commandInfo.Preconditions.Any(attribute => attribute is ModCommandAttribute))
                    ponyReadable += $"ℹ  *This is a moderator only command.*{Environment.NewLine}";
                else if (commandInfo.Preconditions.Any(attribute => attribute is DevCommandAttribute))
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
                if (_commands.Modules.Any(module =>
                    {return module.Name.ToLower() == item.ToLower() ||
                               module.Name.ToLower() == item.ToLower() + "module" ||
                               module.Name.ToLower() == item.ToLower() + "submodule";
                    }))
                {
                    // It's a module!
                    var moduleInfo = _commands.Modules.Single<ModuleInfo>(module => 
                        module.Name.ToLower() == item.ToLower() ||
                        module.Name.ToLower() == item.ToLower() + "module" ||
                        module.Name.ToLower() == item.ToLower() + "submodule");

                    var commands = moduleInfo.Commands.Select<CommandInfo, string>(command => 
                        $"{prefix}{command.Name} - {command.Summary}"
                    ).ToList();

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
                            $"**List of commands in {moduleInfo.Name.Replace("Module", "").Replace("Submodule", "")}.",
                            $"Run `{prefix}help <command>` for help regarding a specific command."
                        };

                        var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                    }
                    else
                    {
                        await ReplyAsync(
                            $"**List of commands in {moduleInfo.Name.Replace("Module", "").Replace("Submodule", "")}**{Environment.NewLine}```{Environment.NewLine}{string.Join(Environment.NewLine, commands)}{Environment.NewLine}```{Environment.NewLine}Run `{prefix}help <command>` for help regarding a specific command.");
                        return;
                    }
                }
            }

            await ReplyAsync($"Sorry, I was unable to find \"{item}\" as either a command, or a category.");
        }
    }

    [Command("about")]
    [Summary("About the bot")]
    public async Task AboutCommandAsync()
    {
        await ReplyAsync(
            $"Izzy Moonbot{Environment.NewLine}Programmed in C# with Virtual Studio and JetBrains Rider{Environment.NewLine}Programmed by Dr. Romulus#4444 and Cloudburst#0001 (Twi/Leah){Environment.NewLine}Supervisor programmed by Raindrops#2245{Environment.NewLine}{Environment.NewLine}Profile picture by confetticakez#7352 (Confetti){Environment.NewLine}https://manebooru.art/images/4023149",
            allowedMentions: AllowedMentions.None);
    }
}