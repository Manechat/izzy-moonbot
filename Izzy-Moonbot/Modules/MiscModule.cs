using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
    private readonly ConfigDescriber _configDescriber;
    private readonly ScheduleService _schedule;
    private readonly LoggingService _logger;
    private readonly ModLoggingService _modLog;
    private readonly CommandService _commands;
    private readonly GeneralStorage _generalStorage;
    private readonly QuoteService _quoteService;

    public MiscModule(Config config, ConfigDescriber configDescriber, ScheduleService schedule, LoggingService logger, ModLoggingService modLog, CommandService commands, GeneralStorage generalStorage, QuoteService quoteService)
    {
        _config = config;
        _configDescriber = configDescriber;
        _schedule = schedule;
        _logger = logger;
        _modLog = modLog;
        _commands = commands;
        _generalStorage = generalStorage;
        _quoteService = quoteService;
    }

    [Command("banner")]
    [Summary("Get the current banner of Manechat.")]
    [Alias("getbanner", "currentbanner")]
    [BotsAllowed]
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
            message += $"I'm not currently managing the banner, but here's the current server's banner.\n";

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
    [Alias("dmme")]
    [Parameter("time", ParameterType.DateTime,
        "How long to wait until sending the message. Supported formats are:\n" +
        "    - Relative interval (e.g. \"in 10 seconds\", \"in 2 hours\", \"in 5 days\")\n" +
        "    - Time (e.g. \"at 12:00 UTC+0\", \"at 5pm UTC-7\")\n" +
        "    - Weekday + Time (e.g. \"on monday 12:00 UTC+0\", \"on friday 5pm UTC-7\")\n" +
        "    - Date + Time (e.g. \"on 1 jan 2020 12:00 UTC+0\", \"on 10 oct 2010 5pm UTC-7\")\n" +
        "    - Discord Timestamp (e.g. \"<t:1234567890>\", \"<t:1234567890:R>\")\n" +
        "Repeating reminders are also supported, and will be sent with an Unsubscribe button.\n" +
        "    - Repeating interval (e.g. \"every 10 seconds\", \"every 2 hours\", \"every 5 days\")\n" +
        "    - Daily Repeating Time (e.g. \"every day 12:00 UTC+0\", \"every day 5pm UTC-7\")\n" +
        "    - Weekly Repeating Weekday + Time (e.g. \"every week monday 12:00 UTC+0\", \"every week friday 5pm UTC-7\")\n" +
        "    - Yearly Repeating Date + Time (e.g. \"every year 1 jan 12:00 UTC+0\", \"every year 10 oct 5pm UTC-7\")"
    )]
    [Parameter("message", ParameterType.String, "The reminder message to DM.")]
    [ExternalUsageAllowed]
    [Example(".remindme in 2 hours join stream")]
    [Example(".remindme at 4:30pm go shopping")]
    [Example(".remindme on 1 jan 2020 12:00 UTC+0 rethink life")]
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

        if (ParseHelper.TryParseDateTime(argsString, out var parseError) is not var (parseResult, content))
        {
            await context.Channel.SendMessageAsync($"Failed to comprehend time: {parseError}");
            return;
        }

        if (content == "")
        {
            await context.Channel.SendMessageAsync("You have to tell me what to remind you!");
            return;
        }

        _logger.Log($"Adding scheduled job to remind user to \"{content}\" at {parseResult.Time:F}{(parseResult.RepeatType == ScheduledJobRepeatType.None ? "" : $" repeating {parseResult.RepeatType}")}",
            context: context, level: LogLevel.Debug);
        var action = new ScheduledEchoJob(context.User, content);
        var task = new ScheduledJob(DateTimeHelper.UtcNow, parseResult.Time, action, parseResult.RepeatType);
        await _schedule.CreateScheduledJob(task);
        _logger.Log($"Added scheduled job for user", context: context, level: LogLevel.Debug);

        await context.Channel.SendMessageAsync($"Okay! I'll DM you a reminder <t:{parseResult.Time.ToUnixTimeSeconds()}:R>.");
    }

    [Command("rule")]
    [Summary("Show one of our server rules.")]
    [Remarks("Takes the text from FirstRuleMessageId or one of the messages after it, depending on the number given. If the number is a key in HiddenRules, the corresponding value is displayed instead.")]
    [Alias("rules")]
    [Parameter("number", ParameterType.Integer, "The rule number to get.")]
    [BotsAllowed]
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
            var rulesChannel = context.Guild?.RulesChannel;
            if (rulesChannel == null)
            {
                await context.Channel.SendMessageAsync("Sorry, this server doesn't have a rules channel.");
                return;
            }

            string ruleMessage;
            if (ruleNumber == 1)
            {
                ruleMessage = (await rulesChannel.GetMessageAsync(firstMessageId))?.Content ?? "";
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
    [Summary("Lists all commands or command categories you can use, or describes how to use a certain command or alias.")]
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

        Func<CommandInfo, bool> canRunCommand = cinfo => CanRunCommand(cinfo, isMod, isDev);

        if (item == "")
        {
            if (isDev || isMod)
            {
                // List modules.
                var moduleList = new List<string>();

                foreach (var module in _commands.Modules)
                {
                    if (module.Name == "DevModule") continue; // Hide dev module
                    moduleList.Add($"{module.Name.Replace("Module", "").ToLower()} - {module.Summary}");
                }

                await context.Channel.SendMessageAsync(
                    $"Hii! Here's how to use the help command!\n" +
                    $"Run `{prefix}help <category>` to list the commands in a category.\n" +
                    $"Run `{prefix}help <command>` to view information about a command.\n\n" +
                    $"Here's a list of all the categories I have!\n" +
                    $"```\n{string.Join('\n', moduleList)}\n```\n" +
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
                    $"Hii! Here's a list of all the commands you can run!\n" +
                    $"```\n{string.Join('\n', commandSummaries)}\n```\n" +
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
                ponyReadable += PonyReadableSelfSearch(item, isMod, isDev);
                await context.Channel.SendMessageAsync(ponyReadable);
            }
            else await context.Channel.SendMessageAsync(
                $"Sorry, you don't have permission to use the {prefix}{commandInfo.Name} command.");
        }
        // Module.
        else if ((isDev || isMod) &&
            _commands.Modules.Any(module => module.Name.ToLower() == item.ToLower() ||
                                            module.Name.ToLower() == item.ToLower() + "module"))
        {
            // It's a module!
            var moduleInfo = _commands.Modules.Single<ModuleInfo>(module =>
                module.Name.ToLower() == item.ToLower() ||
                module.Name.ToLower() == item.ToLower() + "module");

            var commands = moduleInfo.Commands.Select<CommandInfo, string>(command =>
                $"{prefix}{command.Name} - {command.Summary}"
            ).ToList();

            var potentialAliases = _commands.Commands.Where(command =>
                command.Aliases.Select(alias => alias.ToLower()).Contains(item.ToLower())).ToArray();

            PaginationHelper.PaginateIfNeededAndSendMessage(
                context,
                $"Hii! Here's a list of all the commands I could find in the {moduleInfo.Name.Replace("Module", "")} category!",
                commands,
                $"Run `{prefix}help <command>` for help regarding a specific command!" +
                $"{(potentialAliases.Length != 0 ? $"\nℹ  This category shares a name with an alias. For information regarding this alias, run `{prefix}help {potentialAliases.First().Name.ToLower()}`." : "")}"
            );
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
                ponyReadable += PonyReadableSelfSearch(item, isMod, isDev);
                await context.Channel.SendMessageAsync(ponyReadable);
            }
            else await context.Channel.SendMessageAsync(
                $"Sorry, you don't have permission to use the {prefix}{alternateName} command.");
        }
        // Try aliases
        else if (_config.Aliases.Any(alias => alias.Key.ToLower() == item.ToLower()))
        {
            var alias = _config.Aliases.First(alias => alias.Key.ToLower() == item.ToLower());
            var ponyReadable = $"**{prefix}{alias.Key}** is an alias for **{prefix}{alias.Value}** (see {prefix}config Aliases)\n\n";

            var commandInfo = _commands.Commands.FirstOrDefault(command => command.Name.ToLower() == alias.Value.Split(" ")[0].ToLower());

            if (commandInfo == null)
            {
                await context.Channel.SendMessageAsync($"**Warning!** This alias directs to a non-existent command!\n" +
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
            ponyReadable += PonyReadableSelfSearch(item, isMod, isDev);
            await context.Channel.SendMessageAsync(ponyReadable);
        }
        // Try quote aliases
        else if (_quoteService.AliasExists(item))
        {
            var userId = _quoteService.ProcessAlias(item, context.Guild);

            await context.Channel.SendMessageAsync($"'{item}' is a quotealias for the user <@{userId}>. Use `.listquotes {item}` or `.quote {item}` to see their quotes." +
                "\n\nSee `.help quote` and `.help quotelias` for more information.");
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
            var suggestibles = alternateNamesToSuggest.Concat(aliasesToSuggest).Concat(categoriesToSuggest);
            if (suggestibles.Any())
                message += $"\nDid you mean {string.Join(" or ", suggestibles.Select(s => $"`.{s}`"))}?";

            message += PonyReadableSelfSearch(item, isMod, isDev, suggestibles);

            await context.Channel.SendMessageAsync(message);
        }
    }

    private string PonyReadableCommandHelp(char prefix, string command, CommandInfo commandInfo, string? alternateName = null)
    {
        var ponyReadable = (alternateName == null ? $"**{prefix}{commandInfo.Name}**" : $"**{prefix}{alternateName}** (alternate name of **{prefix}{commandInfo.Name}**)") +
            $" - {commandInfo.Module.Name.Replace("Module", "")} category\n";

        if (commandInfo.Preconditions.Any(attribute => attribute is ModCommandAttribute) &&
            commandInfo.Preconditions.Any(attribute => attribute is DevCommandAttribute))
            ponyReadable += $"ℹ  *This is a moderator and developer only command.*\n";
        else if (commandInfo.Preconditions.Any(attribute => attribute is ModCommandAttribute))
            ponyReadable += $"ℹ  *This is a moderator only command.*\n";
        else if (commandInfo.Preconditions.Any(attribute => attribute is DevCommandAttribute))
            ponyReadable += $"ℹ  *This is a developer only command.*\n";

        ponyReadable += $"*{commandInfo.Summary}*\n";
        if (commandInfo.Remarks != null) ponyReadable += $"*{commandInfo.Remarks}*\n";

        var parameters = commandInfo.Attributes.OfType<ParameterAttribute>();
        if (parameters.Any())
        {
            ponyReadable += $"\nSyntax: `{prefix}{commandInfo.Name}";
            ponyReadable = parameters.Aggregate(ponyReadable, (current, parameter) => current + $" {(parameter.Optional ? $"[{parameter.Name}]" : parameter.Name)}");
            ponyReadable += $"`\n";

            ponyReadable += $"```\n";
            ponyReadable = parameters.Aggregate(ponyReadable, (current, parameter) => current + $"{parameter}\n");
            ponyReadable += $"```";
        }

        var examples = commandInfo.Attributes.OfType<ExampleAttribute>();
        if (examples.Count() == 1)
            ponyReadable += $"Example: `{examples.Single()}`";
        else if (examples.Count() > 1)
            ponyReadable += $"Examples: {string.Join(",  ", examples.Select(e => $"`{e}`"))}";

        var remainingAlternates = commandInfo.Aliases.Where(alternate => alternate.ToLower() != commandInfo.Name.ToLower() && alternate.ToLower() != command.ToLower());
        if (remainingAlternates.Any())
            ponyReadable += $"\n" +
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
            return $"\nRelevant aliases: {string.Join(", ", relevantAliases.Select(alias => $"{prefix}{alias.Key}"))}";
        else
            return "";
    }

    private string PonyReadableSelfSearch(string item, bool isMod, bool isDev, IEnumerable<string>? suggestibles = null)
    {
        var commandDocHits = _commands.Commands
            .Where(c => {
                if (!CanRunCommand(c, isMod, isDev))
                    return false;
                if (c.Aliases.Contains(item))
                    return false; // this command is the one we're printing help for, so don't repeat it here
                if (suggestibles != null && c.Aliases.Any(name => suggestibles.Contains(name)))
                    return false; // this command's already being suggested, so don't repeat it here
                return PonyReadableCommandHelp(_config.Prefix, c.Name, c).ToLower().Contains(item.ToLower());
            })
            .Select(c => $"`.help {c.Name}`");

        var configDocHits = Enumerable.Empty<string>();
        if (isMod || isDev)
            configDocHits = _configDescriber.GetSettableConfigItems()
                .Where(configItemKey => ConfigCommand.ConfigItemDescription(_config, _configDescriber, configItemKey).ToLower().Contains(item.ToLower()))
                .Select(configItemKey => $"`.config {configItemKey}`");

        // if we get more than 10 hits, then it was probably something like " " or "the" or "category" that's part of how we format
        // the docs rather than useful information we want to dump an exhaustive list of search hits for
        var documentationSearchHits = commandDocHits.Concat(configDocHits);
        if (documentationSearchHits.Any() && documentationSearchHits.Count() < 10)
            return $"\n\nI also see \"{item}\" in the output of: {string.Join(" and ", commandDocHits.Concat(configDocHits))}";
        return "";
    }

    private bool CanRunCommand(CommandInfo cinfo, bool isMod, bool isDev)
    {
        var modAttr = cinfo.Preconditions.Any(attribute => attribute is ModCommandAttribute);
        var devAttr = cinfo.Preconditions.Any(attribute => attribute is DevCommandAttribute);
        if (modAttr && devAttr) return isMod || isDev;
        else if (modAttr) return isMod;
        else if (devAttr) return isDev;
        else return true;
    }

    [Command("about")]
    [Summary("About the bot")]
    [BotsAllowed]
    [ExternalUsageAllowed]
    public async Task AboutCommandAsync()
    {
        await Context.Channel.SendMessageAsync(
            $"Izzy Moonbot\n" +
            $"Programmed in C# with Virtual Studio and JetBrains Rider\n" +
            $"Programmed by Dr. Romulus#4444, Cloudburst#0001 (Twi/Leah) and Ixrec#7992\n" +

            $"Supervisor programmed by Raindrops#2245\n" +
            $"\n" +
            $"Profile picture by confetticakez#7352 (Confetti)\n" +
            $"https://manebooru.art/images/4023149",
            allowedMentions: AllowedMentions.None);
    }

    [Command("rollforbestpony")]
    [Alias("roll")]
    [Summary("Roll a random number from 1 to 100 at most once per day (as defined by UTC midnight). A roll of 100 will inform the moderators that you've won Best Pony.")]
    [BotsAllowed]
    public async Task RollCommandAsync([Remainder] string cheatArg = "")
    {
        var lastRollTime = _generalStorage.LastRollTime;
        var hasAnyoneRolledToday = lastRollTime > DateTime.Today;
        if (!hasAnyoneRolledToday && _generalStorage.UsersWhoRolledToday.Any())
        {
            _logger.Log($"LastRollTime of {lastRollTime} predates DateTime.Today ({DateTime.Today}), and UsersWhoRolledToday is non-empty, so it's time to clear that list");
            _generalStorage.UsersWhoRolledToday.Clear();
            await FileHelper.SaveGeneralStorageAsync(_generalStorage);
        }

        var userId = Context.User.Id;
        var rollResult = new Random().Next(100);
        rollResult += 1; // 0-99 -> 1-100

        if (!_generalStorage.UsersWhoRolledToday.Contains(userId))
        {
            if (rollResult == 100)
            {
                await ReplyAsync($"{Context.User.Mention} rolled a {rollResult}! <:izzyooh:889126310260113449>\n\nI've sent the mods a glitter bomb <a:izzyspin:969209801961771008>", allowedMentions: AllowedMentions.None);

                var modMsg = $"{Context.User.Mention} just won Best Pony! {Context.Message.GetJumpUrl()}";
                await _modLog.CreateModLog(Context.Guild)
                    .SetContent(modMsg)
                    .SetFileLogContent(modMsg)
                    .Send();

                if (_config.BestPonyChannel != 0)
                {
                    var bestPonyChannel = Context.Guild.GetTextChannel(_config.BestPonyChannel);
                    if (bestPonyChannel == null)
                    {
                        _logger.Log("Something went wrong trying to access BestPonyChannel.");
                        return;
                    }

                    await bestPonyChannel.SendMessageAsync(modMsg);
                }
            }
            else
            {
                await ReplyAsync($"{Context.User.Mention} rolled a {rollResult}", allowedMentions: AllowedMentions.None);
            }

            _logger.Log($"Adding {userId} to UsersWhoRolledToday and setting LastRollTime to {DateTime.UtcNow}");
            _generalStorage.UsersWhoRolledToday.Add(userId);
            _generalStorage.LastRollTime = DateTime.UtcNow;
            await FileHelper.SaveGeneralStorageAsync(_generalStorage);
        }
        else
        {
            await ReplyAsync($"You have already rolled today, so this doesn't count for Best Pony.\n\n{Context.User.Mention} rolled a {rollResult}", allowedMentions: AllowedMentions.None);
        }
    }
}
