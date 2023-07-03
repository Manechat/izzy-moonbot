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

    public ulong ProcessAlias(string alias, IIzzyGuild? guild)
    {
        alias = alias.ToLower();

        if (!_quoteStorage.Aliases.TryGetValue(alias, out var value))
            throw new NullReferenceException("That alias does not exist.");

        return ulong.Parse(value);
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
                return _users.TryGetValue(id, out var user) ? $"{id} ({user.Username}) {aliasText}" : $"{id} {aliasText}";

            return $"{potentialUser.DisplayName} ({potentialUser.Username}/{potentialUser.Id}) {aliasText}";
        }).ToArray();
    }

    public string? GetQuote(ulong userId, int index)
    {
        if (!_quoteStorage.Quotes.TryGetValue(userId.ToString(), out var quotes))
            return null;

        if (quotes.Count <= index)
            return null;

        return quotes[index];
    }

    public List<string>? GetQuotes(ulong userId)
    {
        if (!_quoteStorage.Quotes.TryGetValue(userId.ToString(), out var quotes))
            return null;

        return quotes;
    }

    public (ulong, int, string) GetRandomQuote()
    {
        var rnd = new Random();

        var quoteKeys = _quoteStorage.Quotes.Keys.ToArray();
        var key = quoteKeys[rnd.Next(quoteKeys.Length)];

        var userId = ulong.Parse(key);

        var quotes = _quoteStorage.Quotes[key];
        var index = rnd.Next(quotes.Count);

        return (userId, index, quotes[index]);
    }

    public (int, string)? GetRandomQuote(ulong userId)
    {
        if (!_quoteStorage.Quotes.TryGetValue(userId.ToString(), out var quotes))
            return null;

        var index = new Random().Next(quotes.Count);

        return (index, quotes[index]);
    }

    /// <summary>
    /// Add a quote to a user.
    /// </summary>
    /// <param name="user">The user to add the quote to.</param>
    /// <param name="content">The content of the quote.</param>
    /// <returns>The newly created Quote.</returns>
    public async Task<Quote> AddQuote(IIzzyUser user, string content)
    {
        if (!_quoteStorage.Quotes.TryGetValue(user.Id.ToString(), out var quotes))
        {
            quotes = new List<string>();
            _quoteStorage.Quotes.Add(user.Id.ToString(), quotes);
        }

        quotes.Add(content);

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
        if (!_quoteStorage.Quotes.TryGetValue(user.Id.ToString(), out var quotes))
            throw new NullReferenceException("That user does not have any quotes.");
        
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
