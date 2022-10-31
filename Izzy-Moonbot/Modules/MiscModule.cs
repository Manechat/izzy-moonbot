using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task QuoteCommandAsync(
            [Summary("A user (or quote category) to search for.")]
            string search = "",
            [Summary("A specific quote number in a user (or quote category)")]
            int? number = null)
        {
            if (search == "" && number == null)
            {
                // Get random quote and post
                var quote = _quoteService.GetRandomQuote(Context.Guild);

                await ReplyAsync($"**{quote.Name} `#{quote.Id+1}`:** {quote.Content}");
                return;
            }

            if (search != "" && number == null)
            {
                // Get random quote depending on if search is user-resolvable or not
                // First check if the search resolves to an alias.

                if (_quoteService.AliasExists(search))
                {
                    // This is an alias, check what type
                    if (_quoteService.AliasRefersTo(search, Context.Guild) == "user")
                    {
                        // The alias refers to an existing user. 
                        var user = _quoteService.ProcessAlias(search, Context.Guild);

                        // Choose a random quote from this user.
                        try
                        {
                            var quote = _quoteService.GetRandomQuote(user);

                            // Send quote and return
                            await ReplyAsync($"**{quote.Name} `#{quote.Id + 1}`:** {quote.Content}");
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
                        await ReplyAsync($"**{quote.Name} `#{quote.Id + 1}`:** {quote.Content}");
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
                        await ReplyAsync($"**{quote.Name} `#{quote.Id+1}`:** {quote.Content}");
                        return;
                    } 
                    catch (NullReferenceException)
                    {
                        await ReplyAsync($"I couldn't find any quotes in that category.");
                        return;
                    }
                }
                // It isn't, this is a user.
                var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(search, Context);
                var member = Context.Guild.GetUser(userId);
                        
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
                    await ReplyAsync($"**{quote.Name} `#{quote.Id+1}`:** {quote.Content}");
                    return;
                } 
                catch (NullReferenceException)
                {
                    await ReplyAsync($"I couldn't find any quotes in that category.");
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
                    if (_quoteService.AliasRefersTo(search, Context.Guild) == "user")
                    {
                        // The alias refers to an existing user. 
                        var user = _quoteService.ProcessAlias(search, Context.Guild);

                        // Choose a random quote from this user.
                        try
                        {
                            var quote = _quoteService.GetQuote(user, number.Value - 1);

                            // Send quote and return
                            await ReplyAsync($"**{quote.Name} `#{quote.Id + 1}`:** {quote.Content}");
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
                        await ReplyAsync($"**{quote.Name} `#{quote.Id+1}`:** {quote.Content}");
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
                        await ReplyAsync($"**{quote.Name} `#{quote.Id+1}`:** {quote.Content}");
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
                var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(search, Context);
                var member = Context.Guild.GetUser(userId);
                
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
                    await ReplyAsync($"**{quote.Name} `#{quote.Id+1}`:** {quote.Content}");
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
            return;
        }

        [Command("searchquote")]
        [Summary(
            "Find a list of quotes for a specific user or category, or a list of categories and users with quotes if one is not provided.")]
        [Alias("searchquotes", "sq", "listquotes", "lq", "quotes")]
        public async Task SearchQuoteCommandAsync(
            [Summary("A user (or quote category) to search for.")]
            string search = ""
        )
        {
            if (search == "")
            {
                // Show a list of list keys, paginating them if possible.
                var quoteKeys = _quoteService.GetKeyList(Context.Guild);

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
                        $"Run `{_config.Prefix}quote [user/category]` to get a random quote from that user/category if specified.{Environment.NewLine}" +
                        $"Run `{_config.Prefix}quote` for a random quote from a random user/category."
                    };

                    var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                    return;
                }
                // No pagination, just output
                await ReplyAsync($"Here's a list of users/categories of quotes I've found.{Environment.NewLine}```{Environment.NewLine}" +
                                 $"{string.Join(Environment.NewLine, quoteKeys)}{Environment.NewLine}```{Environment.NewLine}" +
                                 $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category if specified.{Environment.NewLine}" +
                                 $"Run `{_config.Prefix}quote` for a random quote from a random user/category.");
                return;
            }
            
            // Search for user/category
            if (_quoteService.AliasExists(search))
            {
                // This is an alias, check what type
                if (_quoteService.AliasRefersTo(search, Context.Guild) == "user")
                {
                    // The alias refers to an existing user. 
                    var user = _quoteService.ProcessAlias(search, Context.Guild);

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
                                $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category if specified.{Environment.NewLine}" +
                                $"Run `{_config.Prefix}quote` for a random quote from a random user/category."
                            };

                            var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                            return;
                        }
                        // No pagination, just output
                        await ReplyAsync($"Here's all the quotes I could find for **{user.Username}#{user.Discriminator}**.{Environment.NewLine}```{Environment.NewLine}" +
                                         $"{string.Join(Environment.NewLine, quotes)}{Environment.NewLine}```{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category> <number>` to get a specific quote.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote [user/category]` to get a random quote from that user/category if specified.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote` for a random quote from a random user/category.");
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
                                $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category if specified.{Environment.NewLine}" +
                                $"Run `{_config.Prefix}quote` for a random quote from a random user/category."
                            };

                            var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                            return;
                        }
                        // No pagination, just output
                        await ReplyAsync($"Here's all the quotes I could find in **{category}**.{Environment.NewLine}```{Environment.NewLine}" +
                                         $"{string.Join(Environment.NewLine, quotes)}{Environment.NewLine}```{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category> <number>` to get a specific quote.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote [user/category]` to get a random quote from that user/category if specified.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote` for a random quote from a random user/category.");
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
                                $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category if specified.{Environment.NewLine}" +
                                $"Run `{_config.Prefix}quote` for a random quote from a random user/category."
                            };

                            var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                            return;
                        }
                        // No pagination, just output
                        await ReplyAsync($"Here's all the quotes I could find in **{search}**.{Environment.NewLine}```{Environment.NewLine}" +
                                         $"{string.Join(Environment.NewLine, quotes)}{Environment.NewLine}```{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category> <number>` to get a specific quote.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote [user/category]` to get a random quote from that user/category if specified.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote` for a random quote from a random user/category.");
                        return;
                } 
                catch (NullReferenceException)
                {
                    await ReplyAsync($"I couldn't find any quotes in that category.");
                    return;
                }
            }
            // It isn't, this is a user.
            var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(search, Context);
            var member = Context.Guild.GetUser(userId);
            
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
                                $"Run `{_config.Prefix}quote <user/category>` to get a random quote from that user/category if specified.{Environment.NewLine}" +
                                $"Run `{_config.Prefix}quote` for a random quote from a random user/category."
                            };

                            var paginationMessage = new PaginationHelper(Context, pages.ToArray(), staticParts);
                            return;
                        }
                        // No pagination, just output
                        await ReplyAsync($"Here's all the quotes I could find for **{member.DisplayName}**.{Environment.NewLine}```{Environment.NewLine}" +
                                         $"{string.Join(Environment.NewLine, quotes)}{Environment.NewLine}```{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote <user/category> <number>` to get a specific quote.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote [user/category]` to get a random quote from that user/category if specified.{Environment.NewLine}" +
                                         $"Run `{_config.Prefix}quote` for a random quote from a random user/category.");
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
        public async Task AddQuoteCommandAsync(
            [Summary("The user/category to add the quote to.")] string user,
            [Remainder] [Summary("The quote content.")] string content)
        {
            // Check for aliases
            if (_quoteService.AliasExists(user))
            {
                if (_quoteService.AliasRefersTo(user, Context.Guild) == "user")
                {
                    var quoteUser = _quoteService.ProcessAlias(user, Context.Guild);

                    var newAliasUserQuote = await _quoteService.AddQuote(quoteUser, content);

                    await ReplyAsync(
                        $"Added the quote to **{quoteUser.Username}#{quoteUser.Discriminator}** as quote number {newAliasUserQuote.Id + 1}.{Environment.NewLine}" +
                        $"> {newAliasUserQuote.Content}");
                    return;
                }
                var quoteCategory = _quoteService.ProcessAlias(user, Context.Guild);
                    
                var newAliasCategoryQuote = await _quoteService.AddQuote(quoteCategory, content);

                await ReplyAsync(
                    $"Added the quote to **{newAliasCategoryQuote.Name}** as quote number {newAliasCategoryQuote.Id + 1}.{Environment.NewLine}" +
                    $"> {newAliasCategoryQuote.Content}");
                return;
            }

            // Prioritise existing categories
            if (_quoteService.CategoryExists(user))
            {
                // Category exists, add new quote to it.
                var newCategoryQuote = await _quoteService.AddQuote(user, content);

                await ReplyAsync(
                    $"Added the quote to **{user}** as quote number {newCategoryQuote.Id + 1}.{Environment.NewLine}" +
                    $"> {newCategoryQuote.Content}");
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
                    $"> {newCategoryNewQuote.Content}");
                return;
            }
                
            var newUserQuote = await _quoteService.AddQuote(member, content);

            await ReplyAsync(
                $"Added the quote to **{newUserQuote.Name}** as quote number {newUserQuote.Id + 1}.{Environment.NewLine}" +
                $"> {newUserQuote.Content}");
            return;
        }
        
        [Command("removequote")]
        [Summary(
            "Removes a quote from a user or category.")]
        [ModCommand(Group="Permission")]
        [DevCommand(Group="Permission")]
        [Alias("deletequote", "rmquote", "delquote")]
        public async Task RemoveQuoteCommandAsync(
            [Summary("The user/category to remove the quote from.")] string user,
            [Summary("The quote number to remove.")] int number)
        {
            // Check for aliases
            if (_quoteService.AliasExists(user))
            {
                if (_quoteService.AliasRefersTo(user, Context.Guild) == "user")
                {
                    var quoteUser = _quoteService.ProcessAlias(user, Context.Guild);

                    var newAliasUserQuote = await _quoteService.RemoveQuote(quoteUser, number-1);

                    await ReplyAsync(
                        $"Removed quote number {number+1} from **{quoteUser.Username}#{quoteUser.Discriminator}**.");
                    return;
                }
                var quoteCategory = _quoteService.ProcessAlias(user, Context.Guild);
                    
                var newAliasCategoryQuote = await _quoteService.RemoveQuote(quoteCategory, number-1);

                await ReplyAsync(
                    $"Removed quote number {number+1} from **{newAliasCategoryQuote.Name}**.");
                return;
            }

            // Prioritise existing categories
            if (_quoteService.CategoryExists(user))
            {
                // Category exists, add new quote to it.
                var newCategoryQuote = await _quoteService.RemoveQuote(user, number-1);

                await ReplyAsync(
                    $"Removed quote number {number+1} from **{newCategoryQuote.Name}**.");
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
                
            var newUserQuote = await _quoteService.RemoveQuote(member, number-1);

            await ReplyAsync(
                $"Removed quote number {number+1} from **{newUserQuote.Name}**.");
            return;
        }
    }
}