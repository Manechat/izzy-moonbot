using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Modules;

[Summary("Basic information related commands.")]
public class InfoModule : ModuleBase<SocketCommandContext>
{
    private readonly CommandService _commands;
    private readonly Config _config;

    public InfoModule(Config config, CommandService commands)
    {
        _config = config;
        _commands = commands;
    }

    [Command("help")]
    [Summary("Lists all commands")]
    [Parameter("search", ParameterType.String, "The command, category, or alias you want to get information about.")]
    [ExternalUsageAllowed]
    public async Task HelpCommandAsync(
        [Remainder]string item = "")
    {
        await TestableHelpCommandAsync(
            new SocketCommandContextAdapter(Context),
            item
        );
    }

    public async Task TestableHelpCommandAsync(
        IIzzyContext context,
        string item = "")
    {
        var prefix = _config.Prefix;

        var isDev = DiscordHelper.IsDev(context.User.Id);
        var isMod = (context.User is IIzzyGuildUser guildUser) && (guildUser.Roles.Any(r => r.Id == _config.ModRole));

        Func<CommandInfo, bool> canRunCommand = cinfo =>
        {
            if (cinfo.Preconditions.Any(attribute => attribute is ModCommandAttribute)) return isMod;
            if (cinfo.Preconditions.Any(attribute => attribute is DevCommandAttribute)) return isDev;
            return true;
        };

        if (item == "")
        {
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

            await context.Channel.SendMessageAsync(
                $"Hii! Here's how to use the help command!{Environment.NewLine}" +
                $"Run `{prefix}help <category>` to list the commands in a category.{Environment.NewLine}" +
                $"Run `{prefix}help <command>` to view information about a command.{Environment.NewLine}{Environment.NewLine}" +
                $"Here's a list of all the categories I have!{Environment.NewLine}" +
                $"```{Environment.NewLine}{string.Join(Environment.NewLine, moduleList)}{Environment.NewLine}```{Environment.NewLine}" +
                $"ℹ  **See also: `{prefix}config`. Run `{prefix}help config` for more information.**");
        }
        else if (_commands.Commands.Any(command => command.Name.ToLower() == item.ToLower()))
        {
            // It's a command!
            var commandInfo = _commands.Commands.Single<CommandInfo>(command => command.Name.ToLower() == item.ToLower());
            if (canRunCommand(commandInfo))
            {
                var ponyReadable = PonyReadableCommandHelp(prefix, item, commandInfo);
                ponyReadable += PonyReadableRelevantAliases(prefix, item);
                await context.Channel.SendMessageAsync(ponyReadable);
            }
            else await context.Channel.SendMessageAsync(
                $"Sorry, you don't have permission to use the {prefix}{commandInfo.Name} command.");
        }
        // Module.
        else if (_commands.Modules.Any(module => module.Name.ToLower() == item.ToLower() ||
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

                var paginationMessage = new PaginationHelper(context, pages.ToArray(), staticParts);
            }
            else
            {
                var potentialAliases = _commands.Commands.Where(command =>
                    command.Aliases.Select(alias => alias.ToLower()).Contains(item.ToLower())).ToArray();

                await context.Channel.SendMessageAsync(
                    $"Hii! Here's a list of all the commands I could find in the {moduleInfo.Name.Replace("Module", "").Replace("Submodule", "")} category!{Environment.NewLine}" +
                    $"```{Environment.NewLine}{string.Join(Environment.NewLine, commands)}{Environment.NewLine}```{Environment.NewLine}" +
                    $"Run `{prefix}help <command>` for help regarding a specific command!" +
                    $"{(potentialAliases.Length != 0 ? $"{Environment.NewLine}ℹ  This category shares a name with an alias. For information regarding this alias, run `{prefix}help {potentialAliases.First().Name.ToLower()}`." : "")}");
            }
        }
        // Try alternate command names
        else if (_commands.Commands.Any(command =>
            command.Aliases.Select(alias => alias.ToLower()).Contains(item.ToLower())))
        {
            // Alternate detected!
            var commandInfo = _commands.Commands.Single<CommandInfo>(command => command.Aliases.Select(alias => alias.ToLower()).Contains(item.ToLower()));
            var alternateName = commandInfo.Aliases.Single(alias => alias.ToLower() == item.ToLower());
            var ponyReadable = PonyReadableCommandHelp(prefix, item, commandInfo, alternateName);
            ponyReadable += PonyReadableRelevantAliases(prefix, item);
            await context.Channel.SendMessageAsync(ponyReadable);
        }
        // Try aliases
        else if (_config.Aliases.Any(alias => alias.Key.ToLower() == item.ToLower()))
        {
            var alias = _config.Aliases.First(alias => alias.Key.ToLower() == item.ToLower());
            var ponyReadable = $"**{prefix}{alias.Key}** is an alias for **{prefix}{alias.Value}** (see {prefix}config Aliases){Environment.NewLine}{Environment.NewLine}";

            var commandInfo = _commands.Commands.FirstOrDefault(command => command.Name.ToLower() == alias.Value.Split(" ")[0].ToLower());

            if (commandInfo != null)
            {
                ponyReadable += PonyReadableCommandHelp(prefix, item, commandInfo);
                await context.Channel.SendMessageAsync(ponyReadable);
                return;
            }

            // Complain
            await context.Channel.SendMessageAsync(
                $"**Warning!** This alias directs to a non-existent command!{Environment.NewLine}Please remove this alias or redirect it to an existing command.");
        }
        else
        {
            await context.Channel.SendMessageAsync($"Sorry, I was unable to find any command, category, or alias named \"{item}\" that you have access to.");
        }
    }

    private string PonyReadableCommandHelp(char prefix, string command, CommandInfo commandInfo, string? alternateName = null)
    {
        var ponyReadable = (alternateName == null ? $"**{prefix}{commandInfo.Name}**" : $"**{prefix}{alternateName}** (alternate name of **{prefix}{commandInfo.Name}**)") +
            $" - {commandInfo.Module.Name.Replace("Module", "").Replace("Submodule", "")} category{Environment.NewLine}";

        if (commandInfo.Preconditions.Any(attribute => attribute is ModCommandAttribute) &&
            commandInfo.Preconditions.Any(attribute => attribute is DevCommandAttribute))
            ponyReadable += $"ℹ  *This is a moderator and developer only command.*{Environment.NewLine}";
        else if (commandInfo.Preconditions.Any(attribute => attribute is ModCommandAttribute))
            ponyReadable += $"ℹ  *This is a moderator only command.*{Environment.NewLine}";
        else if (commandInfo.Preconditions.Any(attribute => attribute is DevCommandAttribute))
            ponyReadable += $"ℹ  *This is a developer only command.*{Environment.NewLine}";

        ponyReadable += $"*{commandInfo.Summary}*{Environment.NewLine}";
        if (commandInfo.Remarks != null) ponyReadable += $"*{commandInfo.Remarks}*{Environment.NewLine}";

        var parameters = commandInfo.Attributes.OfType<ParameterAttribute>();
        if (parameters.Any())
        {
            ponyReadable += $"{Environment.NewLine}Syntax: `{prefix}{commandInfo.Name}";
            ponyReadable = parameters.Aggregate(ponyReadable, (current, parameter) => current + $" {(parameter.Optional ? $"[{parameter.Name}]" : parameter.Name)}");
            ponyReadable += $"`{Environment.NewLine}";

            ponyReadable += $"```{Environment.NewLine}";
            ponyReadable = parameters.Aggregate(ponyReadable, (current, parameter) => current + $"{parameter}{Environment.NewLine}");
            ponyReadable += $"```";
        }

        var examples = commandInfo.Attributes.OfType<ExampleAttribute>();
        if (examples.Count() == 1)
            ponyReadable += $"Example: `{examples.Single()}`";
        else if (examples.Count() > 1)
            ponyReadable += $"Examples: {string.Join(",  ", examples.Select(e => $"`{e}`"))}";

        var remainingAlternates = commandInfo.Aliases.Where(alternate => alternate.ToLower() != commandInfo.Name.ToLower() && alternate.ToLower() != command.ToLower());
        if (remainingAlternates.Any())
            ponyReadable += $"{Environment.NewLine}" +
                $"Alternate names: {string.Join(", ", remainingAlternates.Select(alt => $"{prefix}{alt}"))}";

        return ponyReadable;
    }

    private string PonyReadableRelevantAliases(char prefix, string command)
    {
        var relevantAliases = _config.Aliases.Where(alias =>
            // be careful not to e.g. make `.help ass` display .assignrole aliases
            alias.Value.ToLower() == command ||
                alias.Value.ToLower().StartsWith($"{command} "));

        if (relevantAliases.Any())
            return $"{Environment.NewLine}Relevant aliases: {string.Join(", ", relevantAliases.Select(alias => $"{prefix}{alias.Key}"))}";
        else
            return "";
    }

    [Command("about")]
    [Summary("About the bot")]
    [ExternalUsageAllowed]
    public async Task AboutCommandAsync()
    {
        await Context.Channel.SendMessageAsync(
            $"Izzy Moonbot{Environment.NewLine}" +
            $"Programmed in C# with Virtual Studio and JetBrains Rider{Environment.NewLine}" +
            $"Programmed by Dr. Romulus#4444, Cloudburst#0001 (Twi/Leah) and Ixrec#7992{Environment.NewLine}" +

            $"Supervisor programmed by Raindrops#2245{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"Profile picture by confetticakez#7352 (Confetti){Environment.NewLine}" +
            $"https://manebooru.art/images/4023149",
            allowedMentions: AllowedMentions.None);
    }
}
