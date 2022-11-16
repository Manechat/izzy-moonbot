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

[Summary("Basic information related commands.")]
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
    [Parameter("search", ParameterType.String, "The command, category, or alias you want to get information about.")]
    public async Task HelpCommandAsync(
        [Remainder]string item = "")
    {
        var prefix = _settings.Prefix;

        if (item == "")
        {
            Context.Client.GetUser(4);
            // List modules.
            var moduleList = new List<string>();

            foreach (var module in _commands.Modules)
            {
                if (module.IsSubmodule) continue;
                if (module.Name == "DevModule") continue; // Hide dev module
                var moduleInfo = $"{module.Name.Replace("Module", "").ToLower()} - {module.Summary}";
                foreach (var submodule in module.Submodules)
                    moduleInfo += $"{Environment.NewLine}    {submodule.Name.Replace("Submodule", "").ToLower()} - {submodule.Summary}";

                moduleList.Add(moduleInfo);
            }

            await ReplyAsync(
                $"Hii! Here's how to use the help command!{Environment.NewLine}" +
                $"Run `{prefix}help <category>` to list the commands in a category.{Environment.NewLine}" +
                $"Run `{prefix}help <command>` to view information about a command.{Environment.NewLine}{Environment.NewLine}" +
                $"Here's a list of all the categories I have!{Environment.NewLine}" +
                $"```{Environment.NewLine}{string.Join(Environment.NewLine, moduleList)}{Environment.NewLine}```{Environment.NewLine}" +
                $"ℹ  **See also: `{prefix}config`. Run `{prefix}help config` for more information.**");
        }
        else
        {
            if (_commands.Commands.Any(command => command.Name.ToLower() == item.ToLower()))
            {
                // It's a command!
                var commandInfo = _commands.Commands.Single<CommandInfo>(command => command.Name.ToLower() == item.ToLower());
                var ponyReadable = $"**{prefix}{commandInfo.Name}** - {commandInfo.Module.Name.Replace("Module", "").Replace("Submodule", "")} category{Environment.NewLine}";
                if (commandInfo.Preconditions.Any(attribute => attribute is ModCommandAttribute) &&
                    commandInfo.Preconditions.Any(attribute => attribute is DevCommandAttribute))
                    ponyReadable += $"ℹ  *This is a moderator and developer only command.*{Environment.NewLine}";
                else if (commandInfo.Preconditions.Any(attribute => attribute is ModCommandAttribute))
                    ponyReadable += $"ℹ  *This is a moderator only command.*{Environment.NewLine}";
                else if (commandInfo.Preconditions.Any(attribute => attribute is DevCommandAttribute))
                    ponyReadable += $"ℹ  *This is a developer only command.*{Environment.NewLine}";

                ponyReadable += $"*{commandInfo.Summary}*{Environment.NewLine}";
                if (commandInfo.Remarks != null) ponyReadable += $"*{commandInfo.Remarks}*{Environment.NewLine}";

                ponyReadable += $"```{Environment.NewLine}";

                var parameters = commandInfo.Attributes.OfType<ParameterAttribute>();

                ponyReadable = parameters.Aggregate(ponyReadable, (current, parameter) => current + $"{parameter}{Environment.NewLine}");

                ponyReadable += $"```";
                
                if (commandInfo.Aliases.Any(alternative => alternative.ToLower() != commandInfo.Name.ToLower() && alternative.ToLower() != item.ToLower()))
                    ponyReadable += $"{Environment.NewLine}" +
                                    $"Alternative names: {string.Join(", ", commandInfo.Aliases.Where(alternative => alternative.ToLower() != commandInfo.Name.ToLower() && alternative.ToLower() != item.ToLower()))}";

                await ReplyAsync(ponyReadable);
                return;
            }
            else
            {
                // Module.
                if (_commands.Modules.Any(module => module.Name.ToLower() == item.ToLower() ||
                                                    module.Name.ToLower() == item.ToLower() + "module" ||
                                                    module.Name.ToLower() == item.ToLower() + "submodule"))
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

                        var potentialAliases = _commands.Commands.Where(command =>
                            command.Aliases.Select(alias => alias.ToLower()).Contains(item.ToLower())).ToArray();
                        
                        string[] staticParts =
                        {
                            $"Hii! Here's a list of all the commands I could find in the {moduleInfo.Name.Replace("Module", "").Replace("Submodule", "")} category!",
                            $"Run `{prefix}help <command>` for help regarding a specific command!" +
                            $"{(potentialAliases.Length != 0 ? $"{Environment.NewLine}ℹ  This category shares a name with an alias. For information regarding this alias, run `{prefix}help {potentialAliases.First().Name.ToLower()}`.": "")}"
                        };

                        var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                        return;
                    }
                    else
                    {
                        var potentialAliases = _commands.Commands.Where(command =>
                            command.Aliases.Select(alias => alias.ToLower()).Contains(item.ToLower())).ToArray();

                        await ReplyAsync(
                            $"Hii! Here's a list of all the commands I could find in the {moduleInfo.Name.Replace("Module", "").Replace("Submodule", "")} category!{Environment.NewLine}" +
                            $"```{Environment.NewLine}{string.Join(Environment.NewLine, commands)}{Environment.NewLine}```{Environment.NewLine}" +
                            $"Run `{prefix}help <command>` for help regarding a specific command!" +
                            $"{(potentialAliases.Length != 0 ? $"{Environment.NewLine}ℹ  This category shares a name with an alias. For information regarding this alias, run `{prefix}help {potentialAliases.First().Name.ToLower()}`.": "")}");
                        return;
                    }
                }
                // Try command aliases
                if (_commands.Commands.Any(command =>
                        command.Aliases.Select(alias => alias.ToLower()).Contains(item.ToLower())))
                {
                    // Alias detected!
                    var commandInfo = _commands.Commands.Single<CommandInfo>(command => command.Aliases.Select(alias => alias.ToLower()).Contains(item.ToLower()));
                    var ponyReadable = $"**{prefix}{commandInfo.Aliases.Single(alias => alias.ToLower() == item.ToLower())}** (alias of **{commandInfo.Name}**) - {commandInfo.Module.Name.Replace("Module", "").Replace("Submodule", "")} category{Environment.NewLine}";
                    if (commandInfo.Preconditions.Any(attribute => attribute is ModCommandAttribute) &&
                        commandInfo.Preconditions.Any(attribute => attribute is DevCommandAttribute))
                        ponyReadable += $"ℹ  *This is a moderator and developer only command.*{Environment.NewLine}";
                    else if (commandInfo.Preconditions.Any(attribute => attribute is ModCommandAttribute))
                        ponyReadable += $"ℹ  *This is a moderator only command.*{Environment.NewLine}";
                    else if (commandInfo.Preconditions.Any(attribute => attribute is DevCommandAttribute))
                        ponyReadable += $"ℹ  *This is a developer only command.*{Environment.NewLine}";

                    ponyReadable += $"*{commandInfo.Summary}*{Environment.NewLine}";
                    if (commandInfo.Remarks != null) ponyReadable += $"*{commandInfo.Remarks}*{Environment.NewLine}";

                    ponyReadable += $"```{Environment.NewLine}";

                    var parameters = commandInfo.Attributes.OfType<ParameterAttribute>();

                    ponyReadable = parameters.Aggregate(ponyReadable, (current, parameter) => current + $"{parameter}{Environment.NewLine}");

                    ponyReadable += $"```";
                
                    if (commandInfo.Aliases.Any(alternative => alternative.ToLower() != commandInfo.Name.ToLower() && alternative.ToLower() != item.ToLower()))
                        ponyReadable += $"{Environment.NewLine}" +
                                        $"Alternative names: {string.Join(", ", commandInfo.Aliases.Where(alternative => alternative.ToLower() != commandInfo.Name.ToLower() && alternative.ToLower() != item.ToLower()))}";

                    await ReplyAsync(ponyReadable);
                    return;
                }
            }

            await ReplyAsync($"Sorry, I was unable to find \"{item}\" as either a command, category, or alias.");
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