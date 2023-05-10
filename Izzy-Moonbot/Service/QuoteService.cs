using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Discord;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
    
namespace Izzy_Moonbot.Service;

public class QuoteService
{
    private readonly QuoteStorage _quoteStorage;
    private readonly Dictionary<ulong, User> _users;
    
    public QuoteService(QuoteStorage quoteStorage, Dictionary<ulong, User> users)
    {
        _quoteStorage = quoteStorage;
        _users = users;
    }

    /// <summary>
    /// Check whether an alias exists or not.
    /// </summary>
    /// <param name="alias">The alias to check.</param>
    /// <returns>Whether the alias exists or not.</returns>
    public bool AliasExists(string alias)
    {
        return _quoteStorage.Aliases.Keys.Any(key => key.ToLower() == alias.ToLower());
    }

    /// <summary>
    /// Process an alias into a IIzzyUser.
    /// </summary>
    /// <param name="alias">The alias to process.</param>
    /// <param name="guild">The guild to get the user from.</param>
    /// <returns>An instance of IIzzyUser that this alias refers to.</returns>
    /// <exception cref="TargetException">If the user couldn't be found (left the server).</exception>
    /// <exception cref="ArgumentException">If the alias doesn't refer to a user.</exception>
    /// <exception cref="NullReferenceException">If the alias doesn't exist.</exception>
    public IIzzyUser ProcessAlias(string alias, IIzzyGuild? guild)
    {
        alias = alias.ToLower();
        
        if (_quoteStorage.Aliases.ContainsKey(alias))
        {
            var value = _quoteStorage.Aliases[alias];

            if (ulong.TryParse(value, out var id))
            {
                var potentialUser = guild?.GetUser(id);
                if (potentialUser == null)
                    throw new TargetException("The user this alias referenced to cannot be found.");

                return potentialUser;
            }
            throw new ArgumentException("This alias cannot be converted to an IIzzyUser.");
        }

        throw new NullReferenceException("That alias does not exist.");
    }

    /// <summary>
    /// Adds an alias to a user.
    /// </summary>
    /// <param name="alias">The alias to add.</param>
    /// <param name="user">The user to map it to.</param>
    /// <exception cref="DuplicateNameException">If the alias already exists.</exception>
    public async Task AddAlias(string alias, IIzzyUser user)
    {
        if (_quoteStorage.Aliases.ContainsKey(alias)) throw new DuplicateNameException("This alias already exists.");
        
        _quoteStorage.Aliases.Add(alias, user.Id.ToString());
        
        await FileHelper.SaveQuoteStorageAsync(_quoteStorage);
    }
    
    public async Task RemoveAlias(string alias)
    {
        alias = alias.ToLower();

        var toDelete = _quoteStorage.Aliases.Keys.Single(key => key.ToLower() == alias.ToLower());
        
        _quoteStorage.Aliases.Remove(toDelete);
        
        await FileHelper.SaveQuoteStorageAsync(_quoteStorage);
    }

    public string[] GetAliasKeyList()
    {
        return _quoteStorage.Aliases.Keys.ToArray();
    }

    public string[] GetKeyList(IIzzyGuild guild)
    {
        return _quoteStorage.Quotes.Keys.ToArray().Select(key =>
        {
            var aliasText = "";
            if (_quoteStorage.Aliases.ContainsValue(key))
            {
                var aliases = _quoteStorage.Aliases.Where(alias => alias.Value == key).Select(alias => alias.Key);
                aliasText = $"(aliases: {string.Join(", ", aliases)})";
            }

            var id = ulong.Parse(key);
            var potentialUser = guild.GetUser(id);
            if (potentialUser == null)
                return _users.ContainsKey(id) ? $"{id} ({_users[id].Username}) {aliasText}" : $"{id} {aliasText}";

            return $"{potentialUser.DisplayName} ({potentialUser.Username}#{potentialUser.Discriminator}) {aliasText}";
        }).ToArray();
    }

    /// <summary>
    /// Get a quote by a valid Discord user and a quote id.
    /// </summary>
    /// <param name="user">The user to get the quote of.</param>
    /// <param name="id">The quote id to get.</param>
    /// <returns>A Quote containing the quote information.</returns>
    /// <exception cref="NullReferenceException">If the user doesn't have any quotes.</exception>
    /// <exception cref="IndexOutOfRangeException">If the id provided is larger than the number of quotes the user has.</exception>
    public Quote GetQuote(IIzzyUser user, int id)
    {
        if (!_quoteStorage.Quotes.ContainsKey(user.Id.ToString()))
            throw new NullReferenceException("That user does not have any quotes.");
        
        var quotes = _quoteStorage.Quotes[user.Id.ToString()];

        if (quotes.Count <= id) throw new IndexOutOfRangeException("That quote ID does not exist.");

        var quoteName = user.Username;
        if (user is IGuildUser guildUser) quoteName = guildUser.DisplayName;
        var quoteContent = quotes[id];

        return new Quote(id, quoteName, quoteContent);
    }

    public List<string>? GetQuotes(ulong userId)
    {
        if (!_quoteStorage.Quotes.ContainsKey(userId.ToString()))
            return null;

        return _quoteStorage.Quotes[userId.ToString()];
    }

    /// <summary>
    /// Gets a random quote from a random user.
    /// </summary>
    /// <param name="guild">The guild to get the user from, for name fetching purposes.</param>
    /// <returns>A Quote containing the quote information.</returns>
    public Quote GetRandomQuote(IIzzyGuild guild)
    {
        Random rnd = new Random();
        var key = _quoteStorage.Quotes.Keys.ToArray()[rnd.Next(_quoteStorage.Quotes.Keys.Count)];

        var quotes = _quoteStorage.Quotes[key];

        var id = ulong.Parse(key);
        string quoteName;
        var potentialUser = guild.GetUser(id);
        if (potentialUser == null)
            quoteName = _users.ContainsKey(id) ? _users[id].Username : $"<@{id}>";
        else
            quoteName = potentialUser.DisplayName;

        var quoteId = rnd.Next(quotes.Count);
        var quoteContent = quotes[quoteId];

        return new Quote(quoteId, quoteName, quoteContent);
    }
    
    /// <summary>
    /// Get a random quote by a valid Discord user.
    /// </summary>
    /// <param name="user">The user to get the quote of.</param>
    /// <returns>A Quote containing the quote information.</returns>
    /// <exception cref="NullReferenceException">If the user doesn't have any quotes.</exception>
    public Quote GetRandomQuote(IIzzyUser user)
    {
        if (!_quoteStorage.Quotes.ContainsKey(user.Id.ToString()))
            throw new NullReferenceException("That user does not have any quotes.");
        
        var quotes = _quoteStorage.Quotes[user.Id.ToString()];
        
        var quoteName = user.Username;
        if (user is IGuildUser guildUser) quoteName = guildUser.DisplayName;
        
        Random rnd = new Random();
        var quoteId = rnd.Next(quotes.Count);
        var quoteContent = quotes[quoteId];

        return new Quote(quoteId, quoteName, quoteContent);
    }

    /// <summary>
    /// Add a quote to a user.
    /// </summary>
    /// <param name="user">The user to add the quote to.</param>
    /// <param name="content">The content of the quote.</param>
    /// <returns>The newly created Quote.</returns>
    public async Task<Quote> AddQuote(IIzzyUser user, string content)
    {
        if (!_quoteStorage.Quotes.ContainsKey(user.Id.ToString()))
            _quoteStorage.Quotes.Add(user.Id.ToString(), new List<string>());

        _quoteStorage.Quotes[user.Id.ToString()].Add(content);

        await FileHelper.SaveQuoteStorageAsync(_quoteStorage);

        var quoteId = _quoteStorage.Quotes[user.Id.ToString()].Count - 1;
        var quoteName = user.Username;
        if (user is IGuildUser guildUser) quoteName = guildUser.DisplayName;

        return new Quote(quoteId, quoteName, content);
    }

    /// <summary>
    /// Remove a quote from a user.
    /// </summary>
    /// <param name="user">The user to remove the quote from.</param>
    /// <param name="id">The id of the quote to remove.</param>
    /// <returns>The Quote that was removed.</returns>
    /// <exception cref="NullReferenceException">If the user doesn't have any quotes.</exception>
    /// <exception cref="IndexOutOfRangeException">If the quote id provided doesn't exist.</exception>
    public async Task<Quote> RemoveQuote(IIzzyUser user, int id)
    {
        if (!_quoteStorage.Quotes.ContainsKey(user.Id.ToString()))
            throw new NullReferenceException("That user does not have any quotes.");
        
        var quotes = _quoteStorage.Quotes[user.Id.ToString()];

        if (quotes.Count <= id) throw new IndexOutOfRangeException("That quote ID does not exist.");
        
        var quoteName = user.Username;
        if (user is IGuildUser guildUser) quoteName = guildUser.DisplayName;

        var quoteContent = quotes[id];
        
        _quoteStorage.Quotes[user.Id.ToString()].RemoveAt(id);

        if (_quoteStorage.Quotes[user.Id.ToString()].Count == 0)
            _quoteStorage.Quotes.Remove(user.Id.ToString());
        
        await FileHelper.SaveQuoteStorageAsync(_quoteStorage);
        
        return new Quote(id, quoteName, quoteContent);
    }
}

public struct Quote
{
    public int Id;
    public string Name;
    public string Content;

    public Quote(int id, string name, string content)
    {
        Id = id;
        Name = name;
        Content = content;
    }
}