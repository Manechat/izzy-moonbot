using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Describers;
using Izzy_Moonbot.EventListeners;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;

namespace Izzy_Moonbot.Modules;

[Summary("Misc commands which exist for fun.")]
public class MiscModule : ModuleBase<SocketCommandContext>
{
    private readonly Config _config;
    private readonly ScheduleService _schedule;
    private readonly LoggingService _logger;
    private readonly CommandService _commands;

    public MiscModule(Config config, ScheduleService schedule, LoggingService logger, CommandService commands)
    {
        _config = config;
        _schedule = schedule;
        _logger = logger;
        _commands = commands;
    }

    [Command("banner")]
    [Summary("Get the current banner of Manechat.")]
    [Alias("getbanner", "currentbanner")]
    public async Task BannerCommandAsync()
    {
        if (_config.BannerMode == ConfigListener.BannerMode.ManebooruFeatured)
        {
            await Context.Channel.SendMessageAsync("I'm currently syncing the banner with the Manebooru featured image");
            await Context.Channel.SendMessageAsync(";featured");
            return;
        }

        if (Context.Guild.BannerUrl == null)
        {
            await Context.Channel.SendMessageAsync("No banner is currently set.");
            return;
        }

        var message = "";
        if (_config.BannerMode == ConfigListener.BannerMode.None)
            message += $"I'm not currently managing the banner, but here's the current server's banner.{Environment.NewLine}";

        message += $"{Context.Guild.BannerUrl}?size=4096";

        await Context.Channel.SendMessageAsync(message);
    }

    [Command("snowflaketime")]
    [Summary("Get the creation date of a Discord resource via its snowflake ID.")]
    [Alias("sft")]
    [Parameter("snowflake", ParameterType.Snowflake, "The snowflake to get the creation date from.")]
    [ExternalUsageAllowed]
    public async Task SnowflakeTimeCommandAsync([Remainder]string snowflakeString = "")
    {
        if (snowflakeString == "")
        {
            await Context.Channel.SendMessageAsync("You need to give me a snowflake to convert!");
            return;
        }
        
        try
        {
            var snowflake = ulong.Parse(snowflakeString);
            var time = SnowflakeUtils.FromSnowflake(snowflake);

            await Context.Channel.SendMessageAsync($"`{snowflake}` -> <t:{time.ToUnixTimeSeconds()}:F> (<t:{time.ToUnixTimeSeconds()}:R>)");
        }
        catch
        {
            await Context.Channel.SendMessageAsync("Sorry, I couldn't convert the snowflake you gave me to an actual snowflake.");
        }
    }

    [Command("remindme")]
    [Summary("Ask Izzy to DM you a message in the future.")]
    [Alias("remind", "dmme")]
    [Parameter("time", ParameterType.DateTime,
        "How long to wait until sending the message, e.g. \"5 days\" or \"2 hours\".")]
    [Parameter("message", ParameterType.String, "The reminder message to DM.")]
    [ExternalUsageAllowed]
    [Example(".remindme 2 hours join stream")]
    [Example(".remindme 6 months rethink life")]
    public async Task RemindMeCommandAsync([Remainder] string argsString = "")
    {
        await TestableRemindMeCommandAsync(
            new SocketCommandContextAdapter(Context),
            argsString
        );
    }

    public async Task TestableRemindMeCommandAsync(
        IIzzyContext context,
        string argsString = "")
    {
        if (argsString == "")
        {
            await context.Channel.SendMessageAsync(
                $"Hey uhh... I think you forgot something... (Missing `time` and `message` parameters, see `{_config.Prefix}help remindme`)");
            return;
        }

        var args = DiscordHelper.GetArguments(argsString);

        var timeType = TimeHelper.GetTimeType(args.Arguments[0]);

        _logger.Log(timeType, level: LogLevel.Trace);

        if (timeType == "unknown" || timeType == "relative")
        {
            if (args.Arguments.Length < (timeType == "unknown" ? 2 : 3))
            {
                await context.Channel.SendMessageAsync("Please provide a time/date!");
                return;
            }

            if (args.Arguments.Length < (timeType == "unknown" ? 3 : 4))
            {
                await context.Channel.SendMessageAsync("You have to tell me what to remind you!");
                return;
            }

            var relativeTimeUnits = new[]
            {
                "years", "year", "months", "month", "days", "day", "weeks", "week", "days", "day", "hours", "hour",
                "minutes", "minute", "seconds", "second"
            };

            var timeNumber = args.Arguments[(timeType == "unknown" ? 0 : 1)];
            var timeUnit = args.Arguments[(timeType == "unknown" ? 1 : 2)];

            if (!int.TryParse(timeNumber, out var time))
            {
                await context.Channel.SendMessageAsync($"I couldn't convert `{timeNumber}` to a number, please try again.");
                return;
            }

            if (!relativeTimeUnits.Contains(timeUnit))
            {
                await context.Channel.SendMessageAsync($"I couldn't convert `{timeUnit}` to a duration type, please try again.");
                return;
            }

            var timeHelperResponse = TimeHelper.Convert($"in {time} {timeUnit}");

            var content = string.Join("", argsString.Skip(args.Indices[(timeType == "unknown" ? 1 : 2)]));
            content = DiscordHelper.StripQuotes(content);

            if (content == "")
            {
                await context.Channel.SendMessageAsync("You have to tell me what to remind you!");
                return;
            }

            _logger.Log($"Adding scheduled job to remind user to \"{content}\" at {timeHelperResponse.Time:F}",
                context: context, level: LogLevel.Debug);
            var action = new ScheduledEchoJob(context.User, content);
            var task = new ScheduledJob(DateTimeOffset.UtcNow,
                timeHelperResponse.Time, action);
            await _schedule.CreateScheduledJob(task);
            _logger.Log($"Added scheduled job for user", context: context, level: LogLevel.Debug);

            await context.Channel.SendMessageAsync($"Okay! I'll DM you a reminder <t:{timeHelperResponse.Time.ToUnixTimeSeconds()}:R>.");
        }
        else
        {
            await context.Channel.SendMessageAsync($"<@186730180872634368> https://www.youtube.com/watch?v=-5wpm-gesOY{Environment.NewLine}(I don't currently support timezones, which is required for the input you just gave me, so I'm telling my primary dev that she has to make me support them)");
            return;
        }
    }

    [Command("rule")]
    [Summary("Show one of our server rules.")]
    [Remarks("Takes the text from FirstRuleMessageId or one of the messages after it, depending on the number given. If the number is a key in HiddenRules, the corresponding value is displayed instead.")]
    [Alias("rules")]
    [Parameter("number", ParameterType.Integer, "The rule number to get.")]
    [ExternalUsageAllowed]
    public async Task RuleCommandAsync([Remainder] string argString = "")
    {
        await TestableRuleCommandAsync(
            new SocketCommandContextAdapter(Context),
            argString
        );
    }

    public async Task TestableRuleCommandAsync(
        IIzzyContext context,
        string argString = "")
    {
        argString = argString.Trim();
        if (argString == "")
        {
            await context.Channel.SendMessageAsync("You need to give me a rule number to look up!");
            return;
        }

        if (_config.HiddenRules.ContainsKey(argString))
        {
            await context.Channel.SendMessageAsync(_config.HiddenRules[argString]);
            return;
        }

        var firstMessageId = _config.FirstRuleMessageId;
        if (firstMessageId == 0)
        {
            await context.Channel.SendMessageAsync("I can't look up rules without knowing where the first one is. Please ask a mod to use `.config FirstRuleMessageId`.");
            return;
        }

        if (int.TryParse(argString, out var ruleNumber))
        {
            var rulesChannel = context.Guild.RulesChannel;

            string ruleMessage;
            if (ruleNumber == 1)
            {
                ruleMessage = (await rulesChannel.GetMessageAsync(firstMessageId)).Content;
            }
            else
            {
                // There might be too few messages in the rules channel, or GetMessagesAsync() might return the messages
                // in a strange order, so we have to gather all messages from rule 2 to rule N and then sort them.
                var rulesAfterFirst = new List<(ulong, string)>();
                await foreach (var messageBatch in rulesChannel.GetMessagesAsync(firstMessageId, Direction.After, ruleNumber - 1))
                {
                    foreach (var message in messageBatch)
                        rulesAfterFirst.Add((message.Id, message.Content));
                }

                if (rulesAfterFirst.Count < (ruleNumber - 1))
                {
                    await context.Channel.SendMessageAsync($"Sorry, there doesn't seem to be a rule {ruleNumber}");
                    return;
                }

                // But we can assume all snowflake ids in Discord are monotonic, i.e. later rules will have higher ids
                // -2 because of 0-indexing plus the fact that these are messages *after* rule 1
                ruleMessage = rulesAfterFirst.OrderBy(t => t.Item1).ElementAt(ruleNumber - 2).Item2;
            }

            await context.Channel.SendMessageAsync(DiscordHelper.TrimDiscordWhitespace(ruleMessage), allowedMentions: AllowedMentions.None);
        }
        else
        {
            await context.Channel.SendMessageAsync($"Sorry, I couldn't convert {argString} to a number.");
        }
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
            if (isDev || isMod)
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
            else
            {
                // List non-mod/non-dev commands
                var regularUserCommands = _commands.Commands.Where(cinfo =>
                    !cinfo.Preconditions.Any(attribute => attribute is DevCommandAttribute) &&
                    !cinfo.Preconditions.Any(attribute => attribute is ModCommandAttribute));

                var commandSummaries = regularUserCommands.Select<CommandInfo, string>(command =>
                    $"{prefix}{command.Name} - {command.Summary}"
                ).ToList();

                // Izzy is not expected to get enough non-mod commands to ever need pagination here
                await context.Channel.SendMessageAsync(
                    $"Hii! Here's a list of all the commands you can run!{Environment.NewLine}" +
                    $"```{Environment.NewLine}{string.Join(Environment.NewLine, commandSummaries)}{Environment.NewLine}```{Environment.NewLine}" +
                    $"Run `{prefix}help <command>` for help regarding a specific command!");
            }
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
        else if ((isDev || isMod) &&
            _commands.Modules.Any(module => module.Name.ToLower() == item.ToLower() ||
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
            if (canRunCommand(commandInfo))
            {
                var ponyReadable = PonyReadableCommandHelp(prefix, item, commandInfo, alternateName);
                ponyReadable += PonyReadableRelevantAliases(prefix, item);
                await context.Channel.SendMessageAsync(ponyReadable);
            }
            else await context.Channel.SendMessageAsync(
                $"Sorry, you don't have permission to use the {prefix}{alternateName} command.");
        }
        // Try aliases
        else if (_config.Aliases.Any(alias => alias.Key.ToLower() == item.ToLower()))
        {
            var alias = _config.Aliases.First(alias => alias.Key.ToLower() == item.ToLower());
            var ponyReadable = $"**{prefix}{alias.Key}** is an alias for **{prefix}{alias.Value}** (see {prefix}config Aliases){Environment.NewLine}{Environment.NewLine}";

            var commandInfo = _commands.Commands.FirstOrDefault(command => command.Name.ToLower() == alias.Value.Split(" ")[0].ToLower());

            if (commandInfo == null)
            {
                await context.Channel.SendMessageAsync($"**Warning!** This alias directs to a non-existent command!{Environment.NewLine}" +
                    $"Please remove this alias or redirect it to an existing command.");
                return;
            }
            if (!canRunCommand(commandInfo))
            {
                await context.Channel.SendMessageAsync(
                    $"Sorry, you don't have permission to use the {prefix}{alias.Key} command.");
                return;
            }

            ponyReadable += PonyReadableCommandHelp(prefix, item, commandInfo);
            await context.Channel.SendMessageAsync(ponyReadable);
        }
        else
        {
            Func<string, bool> isSuggestable = candidate =>
                DiscordHelper.WithinLevenshteinDistanceOf(item, candidate, Convert.ToUInt32(candidate.Length / 2));

            Func<string, bool> canRunCommandName = name =>
            {
                var cinfo = _commands.Commands.Where(c => c.Name == name).SingleOrDefault((CommandInfo?)null);
                return cinfo is null ? false : canRunCommand(cinfo);
            };

            // don't bother searching command.Name because command.Aliases always includes the main name
            var alternateNamesToSuggest = _commands.Commands.Where(canRunCommand)
                .SelectMany(c => c.Aliases).Where(isSuggestable);
            var aliasesToSuggest = _config.Aliases.Where(pair => canRunCommandName(pair.Value.TrimStart().Split(" ")[0]))
                .Select(pair => pair.Key).Where(isSuggestable);
            var categoriesToSuggest = !(isDev || isMod) ? new List<string>() :
                _commands.Modules.Select(m => m.Name.Replace("Module", "").ToLower()).Where(isSuggestable);

            var message = $"Sorry, I was unable to find any command, category, or alias named \"{item}\" that you have access to.";
            if (alternateNamesToSuggest.Any() || aliasesToSuggest.Any() || categoriesToSuggest.Any())
            {
                var suggestibles = alternateNamesToSuggest.Concat(aliasesToSuggest).Concat(categoriesToSuggest).Select(s => $"`.{s}`");
                message += $"\nDid you mean {string.Join(" or ", suggestibles)}?";
            }
            await context.Channel.SendMessageAsync(message);
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
