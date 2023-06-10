using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Modules;

[Summary("Commands for viewing and modifying quotes.")]
public class QuotesModule : ModuleBase<SocketCommandContext>
{
    private readonly Config _config;
    private readonly QuoteService _quoteService;
    private readonly Dictionary<ulong, User> _users;

    public QuotesModule(Config config, QuoteService quoteService, Dictionary<ulong, User> users)
    {
        _config = config;
        _quoteService = quoteService;
        _users = users;
    }

    private async Task<string> DisplayUserName(ulong userId, IIzzyClient client, IIzzyGuild guild)
    {
        // First, assuming the user is in the server, try to get them from cache
        var potentialGuildUser = guild.GetUser(userId);

        if (potentialGuildUser != null)
            return potentialGuildUser.DisplayName;

        // If the user is not in the server then we perform a request to find them
        var potentialUser = await client.GetUserAsync(userId);

        // If they aren't a deleted user
        // Deleted users should (I think) always have 21 chars in their name
        // (13 for "Deleted User " and 8 for the ID afterwards)
        // IMPORTANT: If that changes this MUST be revised
        if (potentialUser != null && !(potentialUser.Username.Length == 21 && potentialUser.Username.StartsWith("Deleted User ")))
            return potentialUser.Username;

        // If all else fails, check our own 'userinfo' cache
        if (_users.TryGetValue(userId, out var user))
            return user.Username;

        // Finally, if we didn't find them in our own user info just default to "Deleted User XXXXXXXX"
        if (potentialUser != null)
            return potentialUser.Username;

        // Never mind, we know nothing after all, just let them render as <@123456>
        return $"<@{userId}>";
    }

    private static string SanitizeQuote(string quote)
    {
        // Find all unfurling urls in the given string and surround them with <>. If any are already non-unfurling then ignore them
        // Regex taken from https://stackoverflow.com/a/3809435
        return Regex.Replace(quote, @"(?<!<)(https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*))(?!>)", "<$&>");
    }

    [Command("quote")]
    [Summary("Get a quote, either randomly, from a specific user, or a specific quote.")]
    [Alias("q")]
    [Parameter("user", ParameterType.UserResolvable, "The user to get a quote from.", true)]
    [Parameter("id", ParameterType.Integer, "The specific quote number from that user to post.", true)]
    [BotsAllowed]
    [ExternalUsageAllowed]
    public async Task QuoteCommandAsync(
        [Remainder] string argsString = "")
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

        // Get random quote
        if (argsString == "")
        {
            var (randomUserId, index, quote) = _quoteService.GetRandomQuote();

            await context.Channel.SendMessageAsync($"**{await DisplayUserName(randomUserId, context.Client, defaultGuild)}**, #{index + 1}: {quote}", allowedMentions: AllowedMentions.None);
            return;
        }

        var (search, number) = QuoteHelper.ParseQuoteArgs(argsString);
        if (search == "")
        {
            if (number != null)
                await context.Channel.SendMessageAsync("You need to provide a user to get the quotes from!");
            else
                await context.Channel.SendMessageAsync($"I... don't know what you want me to do?");
            return;
        }

        ulong userId = _quoteService.AliasExists(search)
            ? _quoteService.ProcessAlias(search, defaultGuild)
            : await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(search, context, true);
        if (userId == 0)
        {
            await context.Channel.SendMessageAsync("I was unable to find the user you asked for. Sorry!");
            return;
        }

        // Get random quote from a specific user
        if (number == null)
        {
            var result = _quoteService.GetRandomQuote(userId);
            if (result == null)
            {
                await context.Channel.SendMessageAsync($"I couldn't find any quotes for that user.");
                return;
            }

            await context.Channel.SendMessageAsync($"**{await DisplayUserName(userId, context.Client, defaultGuild)}**, #{result.Value.Item1 + 1}: {result.Value.Item2}", allowedMentions: AllowedMentions.None);
        }
        // Get specific quote from a specific user
        else
        {
            if (number.Value <= 0)
            {
                await context.Channel.SendMessageAsync($"Quotes begin at #1, not #{number.Value}!");
                return;
            }

            var quote = _quoteService.GetQuote(userId, number.Value - 1);
            if (quote == null)
            {
                await context.Channel.SendMessageAsync($"I couldn't find that quote, sorry!");
                return;
            }

            await context.Channel.SendMessageAsync($"**{await DisplayUserName(userId, context.Client, defaultGuild)}**, #{number.Value}: {quote}", allowedMentions: AllowedMentions.None);
        }
    }

    [Command("listquotes")]
    [Summary("List all the quotes for a specific user, or list all the users that have quotes.")]
    [Alias("lq", "searchquotes", "searchquote", "sq")]
    [Parameter("user", ParameterType.UserResolvable, "The user to search for.", true)]
    [ExternalUsageAllowed]
    public async Task ListQuotesCommandAsync([Remainder] string search = "")
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

            PaginationHelper.PaginateIfNeededAndSendMessage(
                context,
                "Here's all the users who have quotes.",
                quoteKeys,
                $"Run `{_config.Prefix}quote <user>` to get a random quote from that user.\n" +
                $"Run `{_config.Prefix}quote` for a random quote from a random user.",
                pageSize: 15,
                allowedMentions: AllowedMentions.None
            );
            return;
        }

        ulong userId;
        if (_quoteService.AliasExists(search))
            userId = _quoteService.ProcessAlias(search, defaultGuild);
        else
            userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(search, context, true);

        if (userId == 0)
        {
            await context.Channel.SendMessageAsync($"I was unable to find the user you asked for. Sorry!");
            return;
        }

        var quotes = _quoteService.GetQuotes(userId);
        if (quotes == null)
        {
            await context.Channel.SendMessageAsync($"I couldn't find any quotes for that user.");
            return;
        }

        PaginationHelper.PaginateIfNeededAndSendMessage(
            context,
            $"Here's all the quotes I have for **{await DisplayUserName(userId, context.Client, defaultGuild)}**:",
            // We put a \ before the . to prevent Discord's numbered list markdown formatting from kicking in, since
            // that not only adds weird spacing and margins, but also changes the quote numbers in some cases.
            quotes.Select((quote, index) => $"{index + 1}\\. {quote}").Select(SanitizeQuote).ToArray(),
            $"\nRun `{_config.Prefix}quote <user> <number>` to get a specific quote.\n" +
            $"Run `{_config.Prefix}quote <user>` to get a random quote from that user.\n" +
            $"Run `{_config.Prefix}quote` for a random quote from a random user.",
            codeblock: false,
            pageSize: 15,
            allowedMentions: AllowedMentions.None
        );
    }

    [Command("addquote")]
    [Summary("Adds a quote to a user.")]
    [Remarks("In the Discord UI, right-click on a message and go to 'Apps' for an alternative way of invoking this command.")]
    [ModCommand(Group = "Permission")]
    [DevCommand(Group = "Permission")]
    [Parameter("user", ParameterType.UserResolvable, "The user to add the quote to.")]
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
        if (user == "")
        {
            await context.Channel.SendMessageAsync("You need to tell me the user you want to add the quote to.");
            return;
        }

        var guild = context.Guild;
        if (guild == null)
        {
            await context.Channel.SendMessageAsync("You need to be in a server to add quotes.");
            return;
        }

        ulong userId = _quoteService.AliasExists(user)
            ? _quoteService.ProcessAlias(user, guild)
            : await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(user, context);
        if (userId == 0)
        {
            await context.Channel.SendMessageAsync("I was unable to find the user you asked for. Sorry!");
            return;
        }

        var content = string.Join("", argsString.Skip(args.Indices[0]));
        if (content == "")
        {
            await context.Channel.SendMessageAsync("You need to provide content to add.");
            return;
        }

        if (content.StartsWith("\"") && content.EndsWith("\""))
        {
            content = content[new Range(1, ^1)];
        }

        var output = await AddQuoteCommandImpl(_quoteService, guild, userId, content);

        await context.Channel.SendMessageAsync(output, allowedMentions: AllowedMentions.None);
    }

    public static async Task<string> AddQuoteCommandImpl(QuoteService quoteService, IIzzyGuild guild, ulong userId, string content)
    {
        var member = guild.GetUser(userId);

        if (member == null)
            return "I was unable to find the user you asked for. Sorry!";

        var newQuote = await quoteService.AddQuote(member, content);

        return $"Added quote #{newQuote.Id + 1} to **{member.DisplayName}**:\n\n" +
            $">>> {SanitizeQuote(newQuote.Content)}";
    }

    [Command("removequote")]
    [Summary("Removes a quote from a user.")]
    [ModCommand(Group = "Permission")]
    [DevCommand(Group = "Permission")]
    [Alias("deletequote", "rmquote", "delquote")]
    [Parameter("user", ParameterType.UnambiguousUser, "The user to remove the quote from.")]
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
            await context.Channel.SendMessageAsync("You need to tell me the user you want to remove the quote from.");
            return;
        }

        if (number == null)
        {
            await context.Channel.SendMessageAsync("You need to tell me the quote number to remove.");
            return;
        }

        if (context.Guild == null)
        {
            await context.Channel.SendMessageAsync("You need to be in a server to remove quotes.");
            return;
        }

        // Check for aliases
        if (_quoteService.AliasExists(user))
        {
            var aliasUserId = _quoteService.ProcessAlias(user, context.Guild);
            var quoteUser = context.Guild.GetUser(aliasUserId);
            if (quoteUser == null)
                throw new TargetException("The user this alias referenced to cannot be found.");

            await _quoteService.RemoveQuote(quoteUser, number.Value - 1);

            await context.Channel.SendMessageAsync(
                $"Removed quote #{number.Value} from **{quoteUser.Username}#{quoteUser.Discriminator}**.", allowedMentions: AllowedMentions.None);
            return;
        }

        // Now check user
        var userId = DiscordHelper.ConvertUserPingToId(user);
        var member = context.Guild.GetUser(userId);

        if (member == null)
        {
            await context.Channel.SendMessageAsync($"Sorry, I couldn't find that user");
            return;
        }

        var newUserQuote = await _quoteService.RemoveQuote(member, number.Value - 1);

        await context.Channel.SendMessageAsync(
            $"Removed quote #{number.Value} from **{newUserQuote.Name}**.", allowedMentions: AllowedMentions.None);
        return;
    }

    [Command("quotealias")]
    [Summary("Manage quote aliases.")]
    [ModCommand(Group = "Permission")]
    [DevCommand(Group = "Permission")]
    [Parameter("operation", ParameterType.String, "The operation to complete (get/list/set/delete)")]
    [Parameter("alias", ParameterType.String, "The alias name.")]
    [Parameter("target", ParameterType.UnambiguousUser, "The user to set the alias to, if applicable.", true)]
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
            await context.Channel.SendMessageAsync($"Hiya! This is how to use the quote alias command!\n" +
                             $"`{_config.Prefix}quotealias get <alias>` - Work out what an alias maps to.\n" +
                             $"`{_config.Prefix}quotealias list` - List all aliases.\n" +
                             $"`{_config.Prefix}quotealias set/add <alias> <user>` - Creates an alias.\n" +
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
                $"Here's all the aliases I could find.\n```\n" +
                $"{string.Join(", ", aliases)}\n```\n" +
                $"Run `{_config.Prefix}quotealias get <alias>` to find out what an alias maps to.\n" +
                $"Run `{_config.Prefix}quotealias set/add <alias> <user>` to create a new alias.\n" +
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
                var aliasUserId = _quoteService.ProcessAlias(alias, context.Guild);
                var user = context.Guild?.GetUser(aliasUserId);
                if (user == null)
                    throw new TargetException("The user this alias referenced to cannot be found.");

                await context.Channel.SendMessageAsync(
                    $"Quote alias **{alias}** maps to user **{user.Username}#{user.Discriminator}**.", allowedMentions: AllowedMentions.None);
            }
        }
        else if (operation.ToLower() == "set" || operation.ToLower() == "add")
        {
            if (alias == "")
            {
                await context.Channel.SendMessageAsync("You need to provide an alias to create.");
                return;
            }

            if (target == "")
            {
                await context.Channel.SendMessageAsync("You need to provide a user name to set the alias to.");
                return;
            }

            var userId = DiscordHelper.ConvertUserPingToId(target);
            var member = context.Guild?.GetUser(userId);

            if (member == null)
            {
                await context.Channel.SendMessageAsync($"I couldn't find a user with the target you provided.");
                return;
            }

            await _quoteService.AddAlias(alias, member);

            await context.Channel.SendMessageAsync($"Added alias **{alias}** to map to user **{target}**.", allowedMentions: AllowedMentions.None);
        }
        else if (operation.ToLower() == "delete" || operation.ToLower() == "remove")
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
            await context.Channel.SendMessageAsync("Sorry, I don't understand what you want me to do.");
        }
    }
}
