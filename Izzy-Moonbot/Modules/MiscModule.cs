using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Attributes;
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

    public MiscModule(Config config, ScheduleService schedule, LoggingService logger)
    {
        _config = config;
        _schedule = schedule;
        _logger = logger;
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
        if (argsString == "")
        {
            await Context.Channel.SendMessageAsync(
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
                await Context.Channel.SendMessageAsync("Please provide a time/date!");
                return;
            }

            if (args.Arguments.Length < (timeType == "unknown" ? 3 : 4))
            {
                await Context.Channel.SendMessageAsync("You have to tell me what to remind you!");
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
                await Context.Channel.SendMessageAsync($"I couldn't convert `{timeNumber}` to a number, please try again.");
                return;
            }

            if (!relativeTimeUnits.Contains(timeUnit))
            {
                await Context.Channel.SendMessageAsync($"I couldn't convert `{timeUnit}` to a duration type, please try again.");
                return;
            }

            var timeHelperResponse = TimeHelper.Convert($"in {time} {timeUnit}");

            var content = string.Join("", argsString.Skip(args.Indices[(timeType == "unknown" ? 1 : 2)]));
            content = DiscordHelper.StripQuotes(content);

            if (content == "")
            {
                await Context.Channel.SendMessageAsync("You have to tell me what to remind you!");
                return;
            }

            await _logger.Log($"Adding scheduled job to remind user to \"{content}\" at {timeHelperResponse.Time:F}",
                context: Context, level: LogLevel.Debug);
            var action = new ScheduledEchoJob(Context.User, content);
            var task = new ScheduledJob(DateTimeOffset.UtcNow,
                timeHelperResponse.Time, action);
            await _schedule.CreateScheduledJob(task);
            await _logger.Log($"Added scheduled job for user", context: Context, level: LogLevel.Debug);

            await Context.Channel.SendMessageAsync($"Okay! I'll DM you a reminder <t:{timeHelperResponse.Time.ToUnixTimeSeconds()}:R>.");
        }
        else
        {
            await Context.Channel.SendMessageAsync($"<@186730180872634368> https://www.youtube.com/watch?v=-5wpm-gesOY{Environment.NewLine}(I don't currently support timezones, which is required for the input you just gave me, so I'm telling my primary dev that she has to make me support them)");
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
        argString = argString.Trim();
        if (argString == "")
        {
            await ReplyAsync("You need to give me a rule number to look up!");
            return;
        }

        if (_config.HiddenRules.ContainsKey(argString))
        {
            await ReplyAsync(_config.HiddenRules[argString]);
            return;
        }

        var firstMessageId = _config.FirstRuleMessageId;
        if (firstMessageId == 0)
        {
            await ReplyAsync("I can't look up rules without knowing where the first one is. Please ask a mod to use `.config FirstRuleMessageId`.");
            return;
        }

        if (int.TryParse(argString, out var ruleNumber))
        {
            var rulesChannel = Context.Guild.RulesChannel;

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
                    await ReplyAsync($"Sorry, there doesn't seem to be a rule {ruleNumber}");
                    return;
                }

                // But we can assume all snowflake ids in Discord are monotonic, i.e. later rules will have higher ids
                // -2 because of 0-indexing plus the fact that these are messages *after* rule 1
                ruleMessage = rulesAfterFirst.OrderBy(t => t.Item1).ElementAt(ruleNumber - 2).Item2;
            }

            await ReplyAsync(ruleMessage.Trim(), allowedMentions: AllowedMentions.None);
        }
        else
        {
            await ReplyAsync($"Sorry, I couldn't convert {argString} to a number.");
        }
    }

    [Summary("Commands for viewing and modifying quotes.")]
    public class QuotesSubmodule : ModuleBase<SocketCommandContext>
    {
        private readonly Config _config;
        private readonly QuoteService _quoteService;

        public QuotesSubmodule(Config config, QuoteService quoteService)
        {
            _config = config;
            _quoteService = quoteService;
        }

        [Command("quote")]
        [Summary("Get a quote, either randomly, from a specific user, or a specific quote.")]
        [Alias("q")]
        [Parameter("user", ParameterType.User, "The user to get a quote from.", true)]
        [Parameter("id", ParameterType.Integer, "The specific quote number from that user to post.", true)]
        [BotsAllowed]
        [ExternalUsageAllowed]
        public async Task QuoteCommandAsync(
            [Remainder]string argsString = "")
        {
            await TestableQuoteCommandAsync(
                new SocketCommandContextAdapter(Context),
                argsString
            );
        }

        public async Task TestableQuoteCommandAsync(
            IIzzyContext context,
            string argsString = "")
        {
            var defaultGuild = context.Client.Guilds.Single(guild => guild.Id == DiscordHelper.DefaultGuild());
            
            if (argsString == "")
            {
                // Get random quote and post
                var quote = _quoteService.GetRandomQuote(defaultGuild);

                await context.Channel.SendMessageAsync($"**{quote.Name} `#{quote.Id+1}`:** {quote.Content}", allowedMentions: AllowedMentions.None);
                return;
            }

            var (search, number) = QuoteHelper.ParseQuoteArgs(argsString);

            if (search == "" && number != null)
            {
                await context.Channel.SendMessageAsync("You need to provide a user to get the quotes from!");
                return;
            }

            if (search != "" && number == null)
            {
                // Get random quote depending on if search is user-resolvable or not
                // First check if the search resolves to an alias.

                if (_quoteService.AliasExists(search))
                {
                    // This is an alias, check what type
                    if (_quoteService.AliasRefersTo(search, defaultGuild) == "user")
                    {
                        // The alias refers to an existing user. 
                        var user = _quoteService.ProcessAlias(search, defaultGuild);

                        // Choose a random quote from this user.
                        try
                        {
                            var quote = _quoteService.GetRandomQuote(user);

                            // Send quote and return
                            await context.Channel.SendMessageAsync($"**{quote.Name} `#{quote.Id + 1}`:** {quote.Content}", allowedMentions: AllowedMentions.None);
                            return;
                        }
                        catch (NullReferenceException)
                        {
                            await context.Channel.SendMessageAsync($"I couldn't find any quotes for that user.");
                            return;
                        }
                    }

                    // This alias refers to a category or user who left.
                    var category = _quoteService.ProcessAlias(search);

                    // Choose a random quote from this category.
                    try
                    {
                        var quote = _quoteService.GetRandomQuote(category);

                        // Send quote and return.
                        await context.Channel.SendMessageAsync($"**{quote.Name} `#{quote.Id + 1}`:** {quote.Content}", allowedMentions: AllowedMentions.None);
                        return;
                    }
                    catch (NullReferenceException)
                    {
                        await context.Channel.SendMessageAsync($"I couldn't find any quotes in that category.");
                        return;
                    }
                }
                // This isn't an alias. Check if this is a category.
                if (_quoteService.CategoryExists(search))
                {
                    // It is, this either refers to a category or a user who left.
                    // Get a random quote from the category
                    try
                    {
                        var quote = _quoteService.GetRandomQuote(search);

                        // Send quote and return
                        await context.Channel.SendMessageAsync($"**{quote.Name} `#{quote.Id+1}`:** {quote.Content}", allowedMentions: AllowedMentions.None);
                        return;
                    } 
                    catch (NullReferenceException)
                    {
                        await context.Channel.SendMessageAsync($"I couldn't find any quotes in that category.");
                        return;
                    }
                }
                // It isn't, this is a user.
                var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(search, context, true);
                var member = defaultGuild.GetUser(userId);
                        
                // Check if the user exists or not
                if (member == null)
                {
                    // They don't, send a fail message and return.
                    await context.Channel.SendMessageAsync("I was unable to find the user you asked for. Sorry!");
                    return;
                }
                    
                // User exists, choose a random quote from this user.
                try
                {
                    var quote = _quoteService.GetRandomQuote(member);

                    // Send quote and return
                    await context.Channel.SendMessageAsync($"**{quote.Name} `#{quote.Id+1}`:** {quote.Content}", allowedMentions: AllowedMentions.None);
                    return;
                } 
                catch (NullReferenceException)
                {
                    await context.Channel.SendMessageAsync($"I couldn't find any for that user.");
                    return;
                }
            }

            if (search != "" && number != null)
            {
                if (number.Value <= 0)
                {
                    await context.Channel.SendMessageAsync($"Quotes begin at #1, not #{number.Value}!");
                    return;
                }
                
                if (_quoteService.AliasExists(search))
                {
                    // This is an alias, check what type
                    if (_quoteService.AliasRefersTo(search, defaultGuild) == "user")
                    {
                        // The alias refers to an existing user. 
                        var user = _quoteService.ProcessAlias(search, defaultGuild);

                        // Choose a random quote from this user.
                        try
                        {
                            var quote = _quoteService.GetQuote(user, number.Value - 1);

                            // Send quote and return
                            await context.Channel.SendMessageAsync($"**{quote.Name} `#{quote.Id + 1}`:** {quote.Content}", allowedMentions: AllowedMentions.None);
                            return;
                        }
                        catch (NullReferenceException)
                        {
                            await context.Channel.SendMessageAsync($"I couldn't find any quotes for that user.");
                            return;
                        }
                        catch (IndexOutOfRangeException)
                        {
                            await context.Channel.SendMessageAsync($"I couldn't find that quote, sorry!");
                            return;
                        }
                    }
                    // This alias refers to a category or user who left.
                    var category = _quoteService.ProcessAlias(search);
                    
                    // Choose a random quote from this category.
                    try
                    {
                        var quote = _quoteService.GetQuote(category, number.Value-1);

                        // Send quote and return.
                        await context.Channel.SendMessageAsync($"**{quote.Name} `#{quote.Id+1}`:** {quote.Content}", allowedMentions: AllowedMentions.None);
                        return;
                    }
                    catch (NullReferenceException)
                    {
                        await context.Channel.SendMessageAsync($"I couldn't find any quotes in that category.");
                        return;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        await context.Channel.SendMessageAsync($"I couldn't find that quote, sorry!");
                        return;
                    }
                }
                // This isn't an alias. Check if this is a category.
                if (_quoteService.CategoryExists(search))
                {
                    // It is, this either refers to a category or a user who left.
                    // Get a random quote from the category
                    try
                    {
                        var quote = _quoteService.GetQuote(search, number.Value-1);

                        // Send quote and return
                        await context.Channel.SendMessageAsync($"**{quote.Name} `#{quote.Id+1}`:** {quote.Content}", allowedMentions: AllowedMentions.None);
                        return;
                    } 
                    catch (NullReferenceException)
                    {
                        await context.Channel.SendMessageAsync($"I couldn't find any quotes in that category.");
                        return;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        await context.Channel.SendMessageAsync($"I couldn't find that quote, sorry!");
                        return;
                    }
                }
                // It isn't, this is a user.
                var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(search, context, true);
                var member = defaultGuild.GetUser(userId);
                
                // Check if the user exists or not
                if (member == null)
                {
                    // They don't, send a fail message and return.
                    await context.Channel.SendMessageAsync("I was unable to find the user you asked for. Sorry!");
                    return;
                }
                
                // User exists, choose a random quote from this user.
                try
                {
                    var quote = _quoteService.GetQuote(member, number.Value-1);

                    // Send quote and return
                    await context.Channel.SendMessageAsync($"**{quote.Name} `#{quote.Id+1}`:** {quote.Content}", allowedMentions: AllowedMentions.None);
                    return;
                } 
                catch (NullReferenceException)
                {
                    await context.Channel.SendMessageAsync($"I couldn't find any quotes in that category.");
                    return;
                }
                catch (IndexOutOfRangeException)
                {
                    await context.Channel.SendMessageAsync($"I couldn't find that quote, sorry!");
                    return;
                }
            }

            await context.Channel.SendMessageAsync($"I... don't know what you want me to do?");
        }

        [Command("listquotes")]
        [Summary(
            "List all the quotes for a specific user or category, or list all the users and categories that have quotes if one is not provided.")]
        [Alias("lq", "searchquotes", "searchquote", "sq")]
        [Parameter("user", ParameterType.User, "The user to search for.", true)]
        [ExternalUsageAllowed]
        public async Task ListQuotesCommandAsync(
            [Remainder] string search = ""
        )
        {
            await TestableListQuotesCommandAsync(
                new SocketCommandContextAdapter(Context),
                search
            );
        }

        public async Task TestableListQuotesCommandAsync(
            IIzzyContext context,
            string search = "")
        {
            var defaultGuild = context.Client.Guilds.Single(guild => guild.Id == DiscordHelper.DefaultGuild());
            
            if (search == "")
            {
                // Show a list of list keys, paginating them if possible.
                var quoteKeys = _quoteService.GetKeyList(defaultGuild);

                if (quoteKeys.Length > 15)
                {
                    // Pagination
                    // Use pagination
                    var pages = new List<string>();
                    var pageNumber = -1;
                    for (var i = 0; i < quoteKeys.Length; i++)
                    {
                        if (i % 15 == 0)
                        {
                            pageNumber += 1;
                            pages.Add("");
                        }

                        pages[pageNumber] += quoteKeys[i] + Environment.NewLine;
                    }


                    string[] staticParts =
                    {
                        $"Here's a list of users/categories of quotes I've found.",
                        $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category if specified.{Environment.NewLine}" +
                        $"Run `{_config.Prefix}quote` for a random quote from a random user/category."
                    };

                    var paginationMessage = new PaginationHelper(context, pages.ToArray(), staticParts, allowedMentions: AllowedMentions.None);
                    return;
                }
                // No pagination, just output
                await context.Channel.SendMessageAsync($"Here's a list of users/categories of quotes I've found.{Environment.NewLine}```{Environment.NewLine}" +
                                 $"{string.Join(Environment.NewLine, quoteKeys)}{Environment.NewLine}```{Environment.NewLine}" +
                                 $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category if specified.{Environment.NewLine}" +
                                 $"Run `{_config.Prefix}quote` for a random quote from a random user/category.", allowedMentions: AllowedMentions.None);
                return;
            }
            
            // Search for user/category
            if (_quoteService.AliasExists(search))
            {
                // This is an alias, check what type
                if (_quoteService.AliasRefersTo(search, defaultGuild) == "user")
                {
                    // The alias refers to an existing user. 
                    var user = _quoteService.ProcessAlias(search, defaultGuild);

                    // Choose a random quote from this user.
                    try
                    {
                        var quotes = _quoteService.GetQuotes(user).Select(quote => $"{quote.Id+1}: {quote.Content}").ToArray();

                        if (quotes.Length > 15)
                        {
                            // Pagination
                            // Use pagination
                            var pages = new List<string>();
                            var pageNumber = -1;
                            for (var i = 0; i < quotes.Length; i++)
                            {
                                if (i % 15 == 0)
                                {
                                    pageNumber += 1;
                                    pages.Add("");
                                }

                                pages[pageNumber] += quotes[i] + Environment.NewLine;
                            }


                            string[] staticParts =
                            {
                                $"Here's all the quotes I could find for **{user.Username}#{user.Discriminator}**.",
                                $"Run `{_config.Prefix}quote <user/category> <number>` to get a specific quote.{Environment.NewLine}" +
                                $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category.{Environment.NewLine}" +
                                $"Run `{_config.Prefix}quote` for a random quote from a random user/category."
                            };

                            var paginationMessage = new PaginationHelper(context, pages.ToArray(), staticParts, allowedMentions: AllowedMentions.None);
                            return;
                        }
                        // No pagination, just output
                        await context.Channel.SendMessageAsync($"Here's all the quotes I could find for **{user.Username}#{user.Discriminator}**.{Environment.NewLine}```{Environment.NewLine}" +
                                         $"{string.Join(Environment.NewLine, quotes)}{Environment.NewLine}```{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category> <number>` to get a specific quote.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote` for a random quote from a random user/category.", allowedMentions: AllowedMentions.None);
                        return;
                    }
                    catch (NullReferenceException)
                    {
                        await context.Channel.SendMessageAsync($"I couldn't find any quotes for that user.");
                        return;
                    }
                }
                // This alias refers to a category or user who left.
                var category = _quoteService.ProcessAlias(search);

                // Choose a random quote from this category.
                try
                {
                    var quotes = _quoteService.GetQuotes(category).Select(quote => $"{quote.Id+1}: {quote.Content}").ToArray();

                        if (quotes.Length > 15)
                        {
                            // Pagination
                            // Use pagination
                            var pages = new List<string>();
                            var pageNumber = -1;
                            for (var i = 0; i < quotes.Length; i++)
                            {
                                if (i % 15 == 0)
                                {
                                    pageNumber += 1;
                                    pages.Add("");
                                }

                                pages[pageNumber] += quotes[i] + Environment.NewLine;
                            }


                            string[] staticParts =
                            {
                                $"Here's all the quotes I could find in **{category}**.",
                                $"Run `{_config.Prefix}quote <user/category> <number>` to get a specific quote.{Environment.NewLine}" +
                                $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category.{Environment.NewLine}" +
                                $"Run `{_config.Prefix}quote` for a random quote from a random user/category."
                            };

                            var paginationMessage = new PaginationHelper(context, pages.ToArray(), staticParts, allowedMentions: AllowedMentions.None);
                            return;
                        }
                        // No pagination, just output
                        await context.Channel.SendMessageAsync($"Here's all the quotes I could find in **{category}**.{Environment.NewLine}```{Environment.NewLine}" +
                                         $"{string.Join(Environment.NewLine, quotes)}{Environment.NewLine}```{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category> <number>` to get a specific quote.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote` for a random quote from a random user/category.", allowedMentions: AllowedMentions.None);
                        return;
                }
                catch (NullReferenceException)
                {
                    await context.Channel.SendMessageAsync($"I couldn't find any quotes in that category.");
                    return;
                }
            }
            // This isn't an alias. Check if this is a category.
            if (_quoteService.CategoryExists(search))
            {
                // It is, this either refers to a category or a user who left.
                // Get a random quote from the category
                try
                {
                    var quotes = _quoteService.GetQuotes(search).Select(quote => $"{quote.Id+1}: {quote.Content}").ToArray();

                        if (quotes.Length > 15)
                        {
                            // Pagination
                            // Use pagination
                            var pages = new List<string>();
                            var pageNumber = -1;
                            for (var i = 0; i < quotes.Length; i++)
                            {
                                if (i % 15 == 0)
                                {
                                    pageNumber += 1;
                                    pages.Add("");
                                }

                                pages[pageNumber] += quotes[i] + Environment.NewLine;
                            }


                            string[] staticParts =
                            {
                                $"Here's all the quotes I could find in **{search}**.",
                                $"Run `{_config.Prefix}quote <user/category> <number>` to get a specific quote.{Environment.NewLine}" +
                                $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category.{Environment.NewLine}" +
                                $"Run `{_config.Prefix}quote` for a random quote from a random user/category."
                            };

                            var paginationMessage = new PaginationHelper(context, pages.ToArray(), staticParts, allowedMentions: AllowedMentions.None);
                            return;
                        }
                        // No pagination, just output
                        await context.Channel.SendMessageAsync($"Here's all the quotes I could find in **{search}**.{Environment.NewLine}```{Environment.NewLine}" +
                                         $"{string.Join(Environment.NewLine, quotes)}{Environment.NewLine}```{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category> <number>` to get a specific quote.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote` for a random quote from a random user/category.", allowedMentions: AllowedMentions.None);
                        return;
                } 
                catch (NullReferenceException)
                {
                    await context.Channel.SendMessageAsync($"I couldn't find any quotes in that category.");
                    return;
                }
            }
            // It isn't, this is a user.
            var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(search, context, true);
            var member = defaultGuild.GetUser(userId);
            
            // Check if the user exists or not
            if (member == null)
            {
                // They don't, send a fail message and return.
                await context.Channel.SendMessageAsync("I was unable to find the user you asked for. Sorry!");
                return;
            }
            
            // User exists, choose a random quote from this user.
            try
            {
                var quotes = _quoteService.GetQuotes(member).Select(quote => $"{quote.Id+1}: {quote.Content}").ToArray();

                        if (quotes.Length > 15)
                        {
                            // Pagination
                            // Use pagination
                            var pages = new List<string>();
                            var pageNumber = -1;
                            for (var i = 0; i < quotes.Length; i++)
                            {
                                if (i % 15 == 0)
                                {
                                    pageNumber += 1;
                                    pages.Add("");
                                }

                                pages[pageNumber] += quotes[i] + Environment.NewLine;
                            }


                            string[] staticParts =
                            {
                                $"Here's all the quotes I could find for **{member.DisplayName}**.",
                                $"Run `{_config.Prefix}quote <user/category> <number>` to get a specific quote.{Environment.NewLine}" +
                                $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category.{Environment.NewLine}" +
                                $"Run `{_config.Prefix}quote` for a random quote from a random user/category."
                            };

                            var paginationMessage = new PaginationHelper(context, pages.ToArray(), staticParts, allowedMentions: AllowedMentions.None);
                            return;
                        }
                        // No pagination, just output
                        await context.Channel.SendMessageAsync($"Here's all the quotes I could find for **{member.DisplayName}**.{Environment.NewLine}```{Environment.NewLine}" +
                                         $"{string.Join(Environment.NewLine, quotes)}{Environment.NewLine}```{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category> <number>` to get a specific quote.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote` for a random quote from a random user/category.", allowedMentions: AllowedMentions.None);
                        return;
            } 
            catch (NullReferenceException)
            {
                await context.Channel.SendMessageAsync($"I couldn't find any quotes in that category.");
                return;
            }
        }

        [Command("addquote")]
        [Summary(
            "Adds a quote to a user or category.")]
        [ModCommand(Group="Permission")]
        [DevCommand(Group="Permission")]
        [Parameter("user", ParameterType.User, "The user to add the quote to.")]
        [Parameter("content", ParameterType.String, "The quote content to add.")]
        [Example(".addquote @UserName hello there")]
        [Example(".addquote \"Izzy Moonbot\" belizzle it")]
        public async Task AddQuoteCommandAsync(
            [Remainder] string argsString = "")
        {
            await TestableAddQuoteCommandAsync(
                new SocketCommandContextAdapter(Context),
                argsString
            );
        }

        public async Task TestableAddQuoteCommandAsync(
            IIzzyContext context,
            string argsString = "")
        {
            if (argsString == "")
            {
                await context.Channel.SendMessageAsync(
                    "You need to tell me the user you want to add the quote to, and the content of the quote.");
                return;
            }
            
            var args = DiscordHelper.GetArguments(argsString);

            var user = args.Arguments[0];
            var content = string.Join("", argsString.Skip(args.Indices[0]));

            if (user == "")
            {
                await context.Channel.SendMessageAsync("You need to tell me the user/category you want to add the quote to.");
                return;
            }

            if (content == "")
            {
                await context.Channel.SendMessageAsync("You need to provide content to add.");
                return;
            }
            
            if (content.StartsWith("\"") && content.EndsWith("\""))
            {
                content = content[new Range(1, ^1)];
            }
            
            // Check for aliases
            if (_quoteService.AliasExists(user))
            {
                if (_quoteService.AliasRefersTo(user, context.Guild) == "user")
                {
                    var quoteUser = _quoteService.ProcessAlias(user, context.Guild);

                    var newAliasUserQuote = await _quoteService.AddQuote(quoteUser, content);

                    await context.Channel.SendMessageAsync(
                        $"Added the quote to **{quoteUser.Username}#{quoteUser.Discriminator}** as quote number {newAliasUserQuote.Id + 1}.{Environment.NewLine}" +
                        $"> {newAliasUserQuote.Content}", allowedMentions: AllowedMentions.None);
                    return;
                }
                var quoteCategory = _quoteService.ProcessAlias(user, context.Guild);
                    
                var newAliasCategoryQuote = await _quoteService.AddQuote(quoteCategory, content);

                await context.Channel.SendMessageAsync(
                    $"Added the quote to **{newAliasCategoryQuote.Name}** as quote number {newAliasCategoryQuote.Id + 1}.{Environment.NewLine}" +
                    $"> {newAliasCategoryQuote.Content}", allowedMentions: AllowedMentions.None);
                return;
            }

            // Prioritise existing categories
            if (_quoteService.CategoryExists(user))
            {
                // Category exists, add new quote to it.
                var newCategoryQuote = await _quoteService.AddQuote(user, content);

                await context.Channel.SendMessageAsync(
                    $"Added the quote to **{user}** as quote number {newCategoryQuote.Id + 1}.{Environment.NewLine}" +
                    $"> {newCategoryQuote.Content}", allowedMentions: AllowedMentions.None);
                return;
            }
                
            // Now check user
            var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(user, context);
            var member = context.Guild.GetUser(userId);

            if (member == null)
            {
                // New category
                var newCategoryNewQuote = await _quoteService.AddQuote(user, content);

                await context.Channel.SendMessageAsync(
                    $"Added the quote to **{user}** as quote number {newCategoryNewQuote.Id + 1}.{Environment.NewLine}" +
                    $"> {newCategoryNewQuote.Content}", allowedMentions: AllowedMentions.None);
                return;
            }
                
            var newUserQuote = await _quoteService.AddQuote(member, content);

            await context.Channel.SendMessageAsync(
                $"Added the quote to **{newUserQuote.Name}** as quote number {newUserQuote.Id + 1}.{Environment.NewLine}" +
                $"> {newUserQuote.Content}", allowedMentions: AllowedMentions.None);
            return;
        }

        [Command("removequote")]
        [Summary(
            "Removes a quote from a user or category.")]
        [ModCommand(Group = "Permission")]
        [DevCommand(Group = "Permission")]
        [Alias("deletequote", "rmquote", "delquote")]
        [Parameter("user", ParameterType.User, "The user to remove the quote from.")]
        [Parameter("id", ParameterType.Integer, "The quote number to remove.")]
        public async Task RemoveQuoteCommandAsync(
            [Remainder] string argsString = "")
        {
            await TestableRemoveQuoteCommandAsync(
                new SocketCommandContextAdapter(Context),
                argsString
            );
        }

        public async Task TestableRemoveQuoteCommandAsync(
            IIzzyContext context,
            string argsString = "")
        {
            if (argsString == "")
            {
                await context.Channel.SendMessageAsync(
                    "You need to tell me the user you want to remove the quote from, and the quote number to remove.");
                return;
            }

            var (user, number) = QuoteHelper.ParseQuoteArgs(argsString);

            if (user == "")
            {
                await context.Channel.SendMessageAsync("You need to tell me the user/category you want to remove the quote from.");
                return;
            }

            if (number == null)
            {
                await context.Channel.SendMessageAsync("You need to tell me the quote number to remove.");
                return;
            }

            // Check for aliases
            if (_quoteService.AliasExists(user))
            {
                if (_quoteService.AliasRefersTo(user, context.Guild) == "user")
                {
                    var quoteUser = _quoteService.ProcessAlias(user, context.Guild);

                    var newAliasUserQuote = await _quoteService.RemoveQuote(quoteUser, number.Value-1);

                    await context.Channel.SendMessageAsync(
                        $"Removed quote number {number.Value} from **{quoteUser.Username}#{quoteUser.Discriminator}**.", allowedMentions: AllowedMentions.None);
                    return;
                }
                var quoteCategory = _quoteService.ProcessAlias(user, context.Guild);
                    
                var newAliasCategoryQuote = await _quoteService.RemoveQuote(quoteCategory, number.Value-1);

                await context.Channel.SendMessageAsync(
                    $"Removed quote number {number.Value} from **{newAliasCategoryQuote.Name}**.", allowedMentions: AllowedMentions.None);
                return;
            }

            // Prioritise existing categories
            if (_quoteService.CategoryExists(user))
            {
                // Category exists, add new quote to it.
                var newCategoryQuote = await _quoteService.RemoveQuote(user, number.Value-1);

                await context.Channel.SendMessageAsync(
                    $"Removed quote number {number.Value} from **{newCategoryQuote.Name}**.", allowedMentions: AllowedMentions.None);
                return;
            }
                
            // Now check user
            var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(user, context);
            var member = context.Guild.GetUser(userId);

            if (member == null)
            {
                await context.Channel.SendMessageAsync(
                    $"Sorry, I couldn't find that user");
                return;
            }
                
            var newUserQuote = await _quoteService.RemoveQuote(member, number.Value-1);

            await context.Channel.SendMessageAsync(
                $"Removed quote number {number.Value} from **{newUserQuote.Name}**.", allowedMentions: AllowedMentions.None);
            return;
        }
        
        [Command("quotealias")]
        [Summary(
            "Manage quote aliases.")]
        [ModCommand(Group="Permission")]
        [DevCommand(Group="Permission")]
        [Parameter("operation", ParameterType.String, "The operation to complete (get/list/set/delete)")]
        [Parameter("alias", ParameterType.String, "The alias name.")]
        [Parameter("target", ParameterType.User, "The user to set the alias to, if applicable.", true)]
        public async Task QuoteAliasCommandAsync(
            [Remainder] string argsString = "")
        {
            await TestableQuoteAliasCommandAsync(
                new SocketCommandContextAdapter(Context),
                argsString
            );
        }

        public async Task TestableQuoteAliasCommandAsync(
            IIzzyContext context,
            string argsString = "")
        {
            if (argsString == "")
            {
                await context.Channel.SendMessageAsync($"Hiya! This is how to use the quote alias command!{Environment.NewLine}" +
                                 $"`{_config.Prefix}quotealias get <alias>` - Work out what an alias maps to.{Environment.NewLine}" +
                                 $"`{_config.Prefix}quotealias list` - List all aliases.{Environment.NewLine}" +
                                 $"`{_config.Prefix}quotealias set/add <alias> <user/category>` - Creates an alias.{Environment.NewLine}" +
                                 $"`{_config.Prefix}quotealias delete/remove <alias>` - Deletes an alias.");
                return;
            }

            var args = DiscordHelper.GetArguments(argsString);

            var operation = args.Arguments[0];
            var alias = args.Arguments.Length >= 2 ? args.Arguments[1] : "";
            var target = args.Arguments.Length >= 3 ? args.Arguments[2] : "";
            
            if (operation.ToLower() == "list")
            {
                var aliases = _quoteService.GetAliasKeyList();

                await context.Channel.SendMessageAsync(
                    $"Here's all the aliases I could find.{Environment.NewLine}```{Environment.NewLine}" +
                    $"{string.Join(", ", aliases)}{Environment.NewLine}```{Environment.NewLine}" +
                    $"Run `{_config.Prefix}quotealias get <alias>` to find out what an alias maps to.{Environment.NewLine}" +
                    $"Run `{_config.Prefix}quotealias set/add <alias> <user/category>` to create a new alias.{Environment.NewLine}" +
                    $"Run `{_config.Prefix}quotealias delete/remove <alias>` to delete an alias.", allowedMentions: AllowedMentions.None);
            }
            else if (operation.ToLower() == "get")
            {
                if (alias == "")
                {
                    await context.Channel.SendMessageAsync("Uhhh... I can't get an alias if you haven't told me what alias you want...");
                    return;
                }
                
                if (_quoteService.AliasExists(alias))
                {
                    if (_quoteService.AliasRefersTo(alias, context.Guild) == "user")
                    {
                        var user = _quoteService.ProcessAlias(alias, context.Guild);

                        await context.Channel.SendMessageAsync(
                            $"Quote alias **{alias}** maps to user **{user.Username}#{user.Discriminator}**.", allowedMentions: AllowedMentions.None);
                    }
                    else
                    {
                        var category = _quoteService.ProcessAlias(alias);

                        await context.Channel.SendMessageAsync(
                            $"Quote alias **{alias}** maps to category **{category}**.", allowedMentions: AllowedMentions.None);
                    }
                }
            } else if (operation.ToLower() == "set" || operation.ToLower() == "add")
            {
                if (alias == "")
                {
                    await context.Channel.SendMessageAsync("You need to provide an alias to create.");
                    return;
                }

                if (target == "")
                {
                    await context.Channel.SendMessageAsync("You need to provide a user or category name to set the alias to.");
                    return;
                }

                if (_quoteService.CategoryExists(target))
                {
                    await _quoteService.AddAlias(alias, target);
                    
                    await context.Channel.SendMessageAsync($"Added alias **{alias}** to map to category **{target}**.", allowedMentions: AllowedMentions.None);
                }
                else
                {
                    var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(target, context);
                    var member = context.Guild.GetUser(userId);

                    if (member == null)
                    {
                        // Category
                        await context.Channel.SendMessageAsync($"I couldn't find a user or category with the target you provided.");
                        return;
                    }

                    await _quoteService.AddAlias(alias, member);

                    await context.Channel.SendMessageAsync($"Added alias **{alias}** to map to user **{target}**.", allowedMentions: AllowedMentions.None);
                }
            } else if (operation.ToLower() == "delete" || operation.ToLower() == "remove")
            {
                if (alias == "")
                {
                    await context.Channel.SendMessageAsync("Uhhh... I can't delete an alias if you haven't told me what alias you want to delete...");
                    return;
                }
                await _quoteService.RemoveAlias(alias);

                await context.Channel.SendMessageAsync($"Removed alias **{alias}**.", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await context.Channel.SendMessageAsync(
                    "Sorry, I don't understand what you want me to do.");
            }
        }
    }
}
