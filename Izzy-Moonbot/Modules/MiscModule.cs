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

[Summary("Misc commands which exist for fun.")]
public class MiscModule : ModuleBase<SocketCommandContext>
{
    private readonly Config _config;

    public MiscModule(Config config)
    {
        _config = config;
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
            await ReplyAsync("You need to give me a snowflake to convert!");
            return;
        }
        
        try
        {
            var snowflake = ulong.Parse(snowflakeString);
            var time = SnowflakeUtils.FromSnowflake(snowflake);

            await ReplyAsync($"`{snowflake}` -> <t:{time.ToUnixTimeSeconds()}:F> (<t:{time.ToUnixTimeSeconds()}:R>)");
        }
        catch
        {
            await ReplyAsync("Sorry, I couldn't convert the snowflake you gave me to an actual snowflake.");
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
            var defaultGuild = Context.Client.Guilds.Single(guild => guild.Id == DiscordHelper.DefaultGuild());
            
            if (argsString == "")
            {
                // Get random quote and post
                var quote = _quoteService.GetRandomQuote(defaultGuild);

                await ReplyAsync($"**{quote.Name} `#{quote.Id+1}`:** {quote.Content}", allowedMentions: AllowedMentions.None);
                return;
            }

            var (search, number) = ParseQuoteArgs(argsString);

            if (search == "" && number != null)
            {
                await ReplyAsync("You need to provide a user to get the quotes from!");
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
                            await ReplyAsync($"**{quote.Name} `#{quote.Id + 1}`:** {quote.Content}", allowedMentions: AllowedMentions.None);
                            return;
                        }
                        catch (NullReferenceException)
                        {
                            await ReplyAsync($"I couldn't find any quotes for that user.");
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
                        await ReplyAsync($"**{quote.Name} `#{quote.Id + 1}`:** {quote.Content}", allowedMentions: AllowedMentions.None);
                        return;
                    }
                    catch (NullReferenceException)
                    {
                        await ReplyAsync($"I couldn't find any quotes in that category.");
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
                        await ReplyAsync($"**{quote.Name} `#{quote.Id+1}`:** {quote.Content}", allowedMentions: AllowedMentions.None);
                        return;
                    } 
                    catch (NullReferenceException)
                    {
                        await ReplyAsync($"I couldn't find any quotes in that category.");
                        return;
                    }
                }
                // It isn't, this is a user.
                var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(search, Context, true);
                var member = defaultGuild.GetUser(userId);
                        
                // Check if the user exists or not
                if (member == null)
                {
                    // They don't, send a fail message and return.
                    await ReplyAsync("I was unable to find the user you asked for. Sorry!");
                    return;
                }
                    
                // User exists, choose a random quote from this user.
                try
                {
                    var quote = _quoteService.GetRandomQuote(member);

                    // Send quote and return
                    await ReplyAsync($"**{quote.Name} `#{quote.Id+1}`:** {quote.Content}", allowedMentions: AllowedMentions.None);
                    return;
                } 
                catch (NullReferenceException)
                {
                    await ReplyAsync($"I couldn't find any for that user.");
                    return;
                }
            }

            if (search != "" && number != null)
            {
                if (number.Value <= 0)
                {
                    await ReplyAsync($"Quotes begin at #1, not #{number.Value}!");
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
                            await ReplyAsync($"**{quote.Name} `#{quote.Id + 1}`:** {quote.Content}", allowedMentions: AllowedMentions.None);
                            return;
                        }
                        catch (NullReferenceException)
                        {
                            await ReplyAsync($"I couldn't find any quotes for that user.");
                            return;
                        }
                        catch (IndexOutOfRangeException)
                        {
                            await ReplyAsync($"I couldn't find that quote, sorry!");
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
                        await ReplyAsync($"**{quote.Name} `#{quote.Id+1}`:** {quote.Content}", allowedMentions: AllowedMentions.None);
                        return;
                    }
                    catch (NullReferenceException)
                    {
                        await ReplyAsync($"I couldn't find any quotes in that category.");
                        return;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        await ReplyAsync($"I couldn't find that quote, sorry!");
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
                        await ReplyAsync($"**{quote.Name} `#{quote.Id+1}`:** {quote.Content}", allowedMentions: AllowedMentions.None);
                        return;
                    } 
                    catch (NullReferenceException)
                    {
                        await ReplyAsync($"I couldn't find any quotes in that category.");
                        return;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        await ReplyAsync($"I couldn't find that quote, sorry!");
                        return;
                    }
                }
                // It isn't, this is a user.
                var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(search, Context, true);
                var member = defaultGuild.GetUser(userId);
                
                // Check if the user exists or not
                if (member == null)
                {
                    // They don't, send a fail message and return.
                    await ReplyAsync("I was unable to find the user you asked for. Sorry!");
                    return;
                }
                
                // User exists, choose a random quote from this user.
                try
                {
                    var quote = _quoteService.GetQuote(member, number.Value-1);

                    // Send quote and return
                    await ReplyAsync($"**{quote.Name} `#{quote.Id+1}`:** {quote.Content}", allowedMentions: AllowedMentions.None);
                    return;
                } 
                catch (NullReferenceException)
                {
                    await ReplyAsync($"I couldn't find any quotes in that category.");
                    return;
                }
                catch (IndexOutOfRangeException)
                {
                    await ReplyAsync($"I couldn't find that quote, sorry!");
                    return;
                }
            }

            await ReplyAsync($"I... don't know what you want me to do?");
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
            var defaultGuild = Context.Client.Guilds.Single(guild => guild.Id == DiscordHelper.DefaultGuild());
            
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

                    var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts, allowedMentions: AllowedMentions.None);
                    return;
                }
                // No pagination, just output
                await ReplyAsync($"Here's a list of users/categories of quotes I've found.{Environment.NewLine}```{Environment.NewLine}" +
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

                            var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts, allowedMentions: AllowedMentions.None);
                            return;
                        }
                        // No pagination, just output
                        await ReplyAsync($"Here's all the quotes I could find for **{user.Username}#{user.Discriminator}**.{Environment.NewLine}```{Environment.NewLine}" +
                                         $"{string.Join(Environment.NewLine, quotes)}{Environment.NewLine}```{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category> <number>` to get a specific quote.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote` for a random quote from a random user/category.", allowedMentions: AllowedMentions.None);
                        return;
                    }
                    catch (NullReferenceException)
                    {
                        await ReplyAsync($"I couldn't find any quotes for that user.");
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

                            var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts, allowedMentions: AllowedMentions.None);
                            return;
                        }
                        // No pagination, just output
                        await ReplyAsync($"Here's all the quotes I could find in **{category}**.{Environment.NewLine}```{Environment.NewLine}" +
                                         $"{string.Join(Environment.NewLine, quotes)}{Environment.NewLine}```{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category> <number>` to get a specific quote.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote` for a random quote from a random user/category.", allowedMentions: AllowedMentions.None);
                        return;
                }
                catch (NullReferenceException)
                {
                    await ReplyAsync($"I couldn't find any quotes in that category.");
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

                            var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts, allowedMentions: AllowedMentions.None);
                            return;
                        }
                        // No pagination, just output
                        await ReplyAsync($"Here's all the quotes I could find in **{search}**.{Environment.NewLine}```{Environment.NewLine}" +
                                         $"{string.Join(Environment.NewLine, quotes)}{Environment.NewLine}```{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category> <number>` to get a specific quote.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote` for a random quote from a random user/category.", allowedMentions: AllowedMentions.None);
                        return;
                } 
                catch (NullReferenceException)
                {
                    await ReplyAsync($"I couldn't find any quotes in that category.");
                    return;
                }
            }
            // It isn't, this is a user.
            var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(search, Context, true);
            var member = defaultGuild.GetUser(userId);
            
            // Check if the user exists or not
            if (member == null)
            {
                // They don't, send a fail message and return.
                await ReplyAsync("I was unable to find the user you asked for. Sorry!");
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

                            var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts, allowedMentions: AllowedMentions.None);
                            return;
                        }
                        // No pagination, just output
                        await ReplyAsync($"Here's all the quotes I could find for **{member.DisplayName}**.{Environment.NewLine}```{Environment.NewLine}" +
                                         $"{string.Join(Environment.NewLine, quotes)}{Environment.NewLine}```{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category> <number>` to get a specific quote.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote` for a random quote from a random user/category.", allowedMentions: AllowedMentions.None);
                        return;
            } 
            catch (NullReferenceException)
            {
                await ReplyAsync($"I couldn't find any quotes in that category.");
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
        public async Task AddQuoteCommandAsync(
            [Remainder] string argsString = "")
        {
            if (argsString == "")
            {
                await ReplyAsync(
                    "You need to tell me the user you want to add the quote to, and the content of the quote.");
                return;
            }
            
            var args = DiscordHelper.GetArguments(argsString);

            var user = args.Arguments[0];
            var content = string.Join("", argsString.Skip(args.Indices[0]));

            if (user == "")
            {
                await ReplyAsync("You need to tell me the user/category you want to add the quote to.");
                return;
            }

            if (content == "")
            {
                await ReplyAsync("You need to provide content to add.");
                return;
            }
            
            if (content.StartsWith("\"") && content.EndsWith("\""))
            {
                content = content[new Range(1, ^1)];
            }
            
            // Check for aliases
            if (_quoteService.AliasExists(user))
            {
                if (_quoteService.AliasRefersTo(user, Context.Guild) == "user")
                {
                    var quoteUser = _quoteService.ProcessAlias(user, Context.Guild);

                    var newAliasUserQuote = await _quoteService.AddQuote(quoteUser, content);

                    await ReplyAsync(
                        $"Added the quote to **{quoteUser.Username}#{quoteUser.Discriminator}** as quote number {newAliasUserQuote.Id + 1}.{Environment.NewLine}" +
                        $"> {newAliasUserQuote.Content}", allowedMentions: AllowedMentions.None);
                    return;
                }
                var quoteCategory = _quoteService.ProcessAlias(user, Context.Guild);
                    
                var newAliasCategoryQuote = await _quoteService.AddQuote(quoteCategory, content);

                await ReplyAsync(
                    $"Added the quote to **{newAliasCategoryQuote.Name}** as quote number {newAliasCategoryQuote.Id + 1}.{Environment.NewLine}" +
                    $"> {newAliasCategoryQuote.Content}", allowedMentions: AllowedMentions.None);
                return;
            }

            // Prioritise existing categories
            if (_quoteService.CategoryExists(user))
            {
                // Category exists, add new quote to it.
                var newCategoryQuote = await _quoteService.AddQuote(user, content);

                await ReplyAsync(
                    $"Added the quote to **{user}** as quote number {newCategoryQuote.Id + 1}.{Environment.NewLine}" +
                    $"> {newCategoryQuote.Content}", allowedMentions: AllowedMentions.None);
                return;
            }
                
            // Now check user
            var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(user, Context);
            var member = Context.Guild.GetUser(userId);

            if (member == null)
            {
                // New category
                var newCategoryNewQuote = await _quoteService.AddQuote(user, content);

                await ReplyAsync(
                    $"Added the quote to **{user}** as quote number {newCategoryNewQuote.Id + 1}.{Environment.NewLine}" +
                    $"> {newCategoryNewQuote.Content}", allowedMentions: AllowedMentions.None);
                return;
            }
                
            var newUserQuote = await _quoteService.AddQuote(member, content);

            await ReplyAsync(
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
            if (argsString == "")
            {
                await ReplyAsync(
                    "You need to tell me the user you want to remove the quote from, and the quote number to remove.");
                return;
            }

            var (user, number) = ParseQuoteArgs(argsString);

            if (user == "")
            {
                await ReplyAsync("You need to tell me the user/category you want to remove the quote from.");
                return;
            }

            if (number == null)
            {
                await ReplyAsync("You need to tell me the quote number to remove.");
                return;
            }

            // Check for aliases
            if (_quoteService.AliasExists(user))
            {
                if (_quoteService.AliasRefersTo(user, Context.Guild) == "user")
                {
                    var quoteUser = _quoteService.ProcessAlias(user, Context.Guild);

                    var newAliasUserQuote = await _quoteService.RemoveQuote(quoteUser, number.Value-1);

                    await ReplyAsync(
                        $"Removed quote number {number.Value} from **{quoteUser.Username}#{quoteUser.Discriminator}**.", allowedMentions: AllowedMentions.None);
                    return;
                }
                var quoteCategory = _quoteService.ProcessAlias(user, Context.Guild);
                    
                var newAliasCategoryQuote = await _quoteService.RemoveQuote(quoteCategory, number.Value-1);

                await ReplyAsync(
                    $"Removed quote number {number.Value} from **{newAliasCategoryQuote.Name}**.", allowedMentions: AllowedMentions.None);
                return;
            }

            // Prioritise existing categories
            if (_quoteService.CategoryExists(user))
            {
                // Category exists, add new quote to it.
                var newCategoryQuote = await _quoteService.RemoveQuote(user, number.Value-1);

                await ReplyAsync(
                    $"Removed quote number {number.Value} from **{newCategoryQuote.Name}**.", allowedMentions: AllowedMentions.None);
                return;
            }
                
            // Now check user
            var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(user, Context);
            var member = Context.Guild.GetUser(userId);

            if (member == null)
            {
                await ReplyAsync(
                    $"Sorry, I couldn't find that user");
                return;
            }
                
            var newUserQuote = await _quoteService.RemoveQuote(member, number.Value-1);

            await ReplyAsync(
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
            if (argsString == "")
            {
                await ReplyAsync($"Hiya! This is how to use the quote alias command!{Environment.NewLine}" +
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

                await ReplyAsync(
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
                    await ReplyAsync("Uhhh... I can't get an alias if you haven't told me what alias you want...");
                    return;
                }
                
                if (_quoteService.AliasExists(alias))
                {
                    if (_quoteService.AliasRefersTo(alias, Context.Guild) == "user")
                    {
                        var user = _quoteService.ProcessAlias(alias, Context.Guild);

                        await ReplyAsync(
                            $"Quote alias **{alias}** maps to user **{user.Username}#{user.Discriminator}**.", allowedMentions: AllowedMentions.None);
                    }
                    else
                    {
                        var category = _quoteService.ProcessAlias(alias);

                        await ReplyAsync(
                            $"Quote alias **{alias}** maps to category **{category}**.", allowedMentions: AllowedMentions.None);
                    }
                }
            } else if (operation.ToLower() == "set" || operation.ToLower() == "add")
            {
                if (alias == "")
                {
                    await ReplyAsync("You need to provide an alias to create.");
                    return;
                }

                if (target == "")
                {
                    await ReplyAsync("You need to provide a user or category name to set the alias to.");
                    return;
                }

                if (_quoteService.CategoryExists(target))
                {
                    await _quoteService.AddAlias(alias, target);
                    
                    await ReplyAsync($"Added alias **{alias}** to map to category **{target}**.", allowedMentions: AllowedMentions.None);
                }
                else
                {
                    var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(target, Context);
                    var member = Context.Guild.GetUser(userId);

                    if (member == null)
                    {
                        // Category
                        await ReplyAsync($"I couldn't find a user or category with the target you provided.");
                        return;
                    }

                    await _quoteService.AddAlias(alias, member);

                    await ReplyAsync($"Added alias **{alias}** to map to user **{target}**.", allowedMentions: AllowedMentions.None);
                }
            } else if (operation.ToLower() == "delete" || operation.ToLower() == "remove")
            {
                if (alias == "")
                {
                    await ReplyAsync("Uhhh... I can't delete an alias if you haven't told me what alias you want to delete...");
                    return;
                }
                await _quoteService.RemoveAlias(alias);

                await ReplyAsync($"Removed alias **{alias}**.", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync(
                    "Sorry, I don't understand what you want me to do.");
            }
        }

        private Tuple<string, int?> ParseQuoteArgs(string argsString)
        {
            var search = argsString;
            var number = int.MinValue;

            var args = DiscordHelper.GetArguments(argsString);

            if (args.Arguments.Length >= 2)
            {
                if (!int.TryParse(args.Arguments[1], out number))
                {
                    number = int.MinValue;
                    foreach (var s in argsString.Split(" "))
                    {
                        if (int.TryParse(s, out number))
                        {
                            // Found the number, all content before it is search
                            var index = argsString.Split(" ").ToList().IndexOf(s);
                            search = string.Join(" ", argsString.Split(" ")[new Range(0, index)]);
                        }
                        else
                        {
                            number = int.MinValue;
                        }
                    }
                }
                else
                {
                    search = args.Arguments[0];
                }
            }

            search = DiscordHelper.StripQuotes(search);

            return number == int.MinValue 
                ? new Tuple<string, int?>(search, null) 
                : new Tuple<string, int?>(search, number);
        }
    }
}
