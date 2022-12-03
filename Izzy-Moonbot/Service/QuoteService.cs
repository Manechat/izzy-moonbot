using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
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
    /// Work out what the alias refers to.
    /// </summary>
    /// <param name="alias">The alias to check.</param>
    /// <param name="guild">The guild to check for the user in.</param>
    /// <returns>"user" if the alias refers to a user, "category" if not.</returns>
    /// <exception cref="NullReferenceException">If the alias doesn't exist.</exception>
    public string AliasRefersTo(string alias, SocketGuild guild)
    {
        if (_quoteStorage.Aliases.Keys.Any(key => key.ToLower() == alias.ToLower()))
        {
            var value = _quoteStorage.Aliases.Single(pair => pair.Key.ToLower() == alias.ToLower()).Value;

            if (ulong.TryParse(value, out var id))
            {
                var potentialUser = guild.GetUser(id);
                if (potentialUser == null) return "category";

                return "user";
            }

            return "category";
        }

        throw new NullReferenceException("That alias does not exist.");
    }

    /// <summary>
    /// Process an alias into a IUser.
    /// </summary>
    /// <param name="alias">The alias to process.</param>
    /// <param name="guild">The guild to get the user from.</param>
    /// <returns>An instance of IUser that this alias refers to.</returns>
    /// <exception cref="TargetException">If the user couldn't be found (left the server).</exception>
    /// <exception cref="ArgumentException">If the alias doesn't refer to a user.</exception>
    /// <exception cref="NullReferenceException">If the alias doesn't exist.</exception>
    public IUser ProcessAlias(string alias, SocketGuild guild)
    {
        if (_quoteStorage.Aliases.ContainsKey(alias))
        {
            var value = _quoteStorage.Aliases[alias];

            if (ulong.TryParse(value, out var id))
            {
                var potentialUser = guild.GetUser(id);
                if (potentialUser == null)
                    throw new TargetException("The user this alias referenced to cannot be found.");

                return potentialUser;
            }
            throw new ArgumentException("This alias cannot be converted to an IUser.");
        }

        throw new NullReferenceException("That alias does not exist.");
    }
    
    /// <summary>
    /// Process an alias into a category name.
    /// </summary>
    /// <param name="alias">The alias to process.</param>
    /// <returns>The category name this alias refers to.</returns>
    /// <exception cref="NullReferenceException">If the alias doesn't exist.</exception>
    public string ProcessAlias(string alias)
    {
        if (_quoteStorage.Aliases.Keys.Any(key => key.ToLower() == alias.ToLower()))
        {
            var value = _quoteStorage.Aliases.Single(pair => pair.Key.ToLower() == alias.ToLower()).Value;

            return value;
        }

        throw new NullReferenceException("That alias does not exist.");
    }

    /// <summary>
    /// Adds an alias to a user.
    /// </summary>
    /// <param name="alias">The alias to add.</param>
    /// <param name="user">The user to map it to.</param>
    /// <exception cref="DuplicateNameException">If the alias already exists.</exception>
    public async Task AddAlias(string alias, IUser user)
    {
        if (_quoteStorage.Aliases.ContainsKey(alias)) throw new DuplicateNameException("This alias already exists.");
        
        _quoteStorage.Aliases.Add(alias, user.Id.ToString());
        
        await FileHelper.SaveQuoteStorageAsync(_quoteStorage);
    }
    
    /// <summary>
    /// Adds an alias to a category.
    /// </summary>
    /// <param name="alias">The alias to add.</param>
    /// <param name="category">The user to map it to.</param>
    /// <exception cref="DuplicateNameException">If the alias already exists.</exception>
    public async Task AddAlias(string alias, string category)
    {
        if (_quoteStorage.Aliases.ContainsKey(alias)) throw new DuplicateNameException("This alias already exists.");
        
        _quoteStorage.Aliases.Add(alias, category);
        
        await FileHelper.SaveQuoteStorageAsync(_quoteStorage);
    }
    
    public async Task RemoveAlias(string alias)
    {
        if (!CategoryExists(alias)) throw new NullReferenceException("This alias doesn't exist.");

        var toDelete = _quoteStorage.Aliases.Keys.Single(key => key.ToLower() == alias.ToLower());
        
        _quoteStorage.Aliases.Remove(toDelete);
        
        await FileHelper.SaveQuoteStorageAsync(_quoteStorage);
    }

    public string[] GetAliasKeyList()
    {
        return _quoteStorage.Aliases.Keys.ToArray();
    }
    
    /// <summary>
    /// Check if a category exists.
    /// </summary>
    /// <param name="name">The category name to check.</param>
    /// <returns>Whether the category exists or not.</returns>
    public bool CategoryExists(string name)
    {
        return _quoteStorage.Quotes.Keys.Any(key => key.ToLower() == name.ToLower());
    }

    public string[] GetKeyList(SocketGuild guild)
    {
        return _quoteStorage.Quotes.Keys.ToArray().Select(key =>
        {
            var aliasText = "";
            if (_quoteStorage.Aliases.ContainsValue(key))
            {
                var aliases = _quoteStorage.Aliases.Where(alias => alias.Value == key).Select(alias => alias.Key);
                aliasText = $"(aliases: {string.Join(", ", aliases)})";
            }
            
            if (ulong.TryParse(key, out var id))
            {
                // Potential user
                var potentialUser = guild.GetUser(id);

                if (potentialUser == null)
                {
                    return _users.ContainsKey(id) ? $"{id} ({_users[id].Username}) {aliasText}" : $"{id} {aliasText}";
                }

                return
                    $"{potentialUser.DisplayName} ({potentialUser.Username}#{potentialUser.Discriminator}) {aliasText}";
            }

            // Category
            
            return $"{key} {aliasText}";
        }).ToArray();
    }

    /// <summary>
    /// Get a quote by a valid Discord user and a quote id.
    /// </summary>
    /// <param name="user">The user to get the quote of.</param>
    /// <param name="id">The quote id to get.</param>
    /// <returns>A Quote containing the quote information.</returns>
    /// <exception cref="NullReferenceException">If the user doesn't have any quotes.</exception>
    /// <exception cref="IndexOutOfRangeException">If the id provided is larger than the amount of quotes the user has.</exception>
    public Quote GetQuote(IUser user, int id)
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
    
    /// <summary>
    /// Get a quote in a category by a quote id.
    /// </summary>
    /// <param name="name">The category name to get the quote of.</param>
    /// <param name="id">The quote id to get.</param>
    /// <returns>A Quote containing the quote information.</returns>
    /// <exception cref="NullReferenceException">If the category doesn't have any quotes.</exception>
    /// <exception cref="IndexOutOfRangeException">If the id provided is larger than the amount of quotes the category contains.</exception>
    public Quote GetQuote(string name, int id)
    {
        if (_quoteStorage.Quotes.Keys.All(key => key.ToLower() != name.ToLower()))
            throw new NullReferenceException("That category does not have any quotes.");
        
        var quotes = _quoteStorage.Quotes.Single(pair => pair.Key.ToLower() == name.ToLower()).Value;

        if (quotes.Count <= id) throw new IndexOutOfRangeException("That quote ID does not exist.");

        var quoteName = name;
        if (ulong.TryParse(name, out var userId))
        {
            // It's a user, but since we're technically looking for a category, this user likely left.
            // Check to see if Izzy knows about them, if she does, set quote name to username, else just mention them.
            quoteName = _users.ContainsKey(userId) ? _users[userId].Username : $"<@{userId}>";
        }
        
        var quoteContent = quotes[id];

        return new Quote(id, quoteName, quoteContent);
    }

    /// <summary>
    /// Get a list of quotes from a valid Discord user.
    /// </summary>
    /// <param name="user">The user to get the quotes of.</param>
    /// <returns>An array of Quotes that this user has.</returns>
    /// <exception cref="NullReferenceException">If the user doesn't have any quotes.</exception>
    public Quote[] GetQuotes(IUser user)
    {
        if (!_quoteStorage.Quotes.ContainsKey(user.Id.ToString()))
            throw new NullReferenceException("That user does not have any quotes.");
        
        var quotes = _quoteStorage.Quotes[user.Id.ToString()].Select(quoteContent =>
        {
            var quoteName = user.Username;
            if (user is IGuildUser guildUser) quoteName = guildUser.DisplayName;

            return new Quote(_quoteStorage.Quotes[user.Id.ToString()].IndexOf(quoteContent), quoteName, quoteContent);
        }).ToArray();

        return quotes;
    }
    
    /// <summary>
    /// Get a list of quotes from a category.
    /// </summary>
    /// <param name="name">The category name to get the quotes of.</param>
    /// <returns>An array of Quotes that this category contains.</returns>
    /// <exception cref="NullReferenceException">If the category doesn't have any quotes.</exception>
    public Quote[] GetQuotes(string name)
    {
        if (_quoteStorage.Quotes.Keys.All(key => key.ToLower() != name.ToLower()))
            throw new NullReferenceException("That category does not have any quotes.");

        var keyValuePair = _quoteStorage.Quotes.Single(pair => pair.Key.ToLower() == name.ToLower());
        
        var quoteName = keyValuePair.Key;
        if (ulong.TryParse(quoteName, out var userId))
        {
            // It's a user, but since we're technically looking for a category, this user likely left.
            // Check to see if Izzy knows about them, if she does, set quote name to username, else just mention them.
            quoteName = _users.ContainsKey(userId) ? _users[userId].Username : $"<@{userId}>";
        }
        
        var quotes = keyValuePair.Value.Select(quoteContent => 
            new Quote(keyValuePair.Value.IndexOf(quoteContent), quoteName, quoteContent)).ToArray();

        return quotes;
    }

    /// <summary>
    /// Gets a random quote from a random user/category.
    /// </summary>
    /// <param name="guild">The guild to get the user from, for name fetching purposes.</param>
    /// <returns>A Quote containing the quote information.</returns>
    public Quote GetRandomQuote(SocketGuild guild)
    {
        Random rnd = new Random();
        var key = _quoteStorage.Quotes.Keys.ToArray()[rnd.Next(_quoteStorage.Quotes.Keys.Count)];

        var quotes = _quoteStorage.Quotes[key];

        var isUser = ulong.TryParse(key, out var id);
        var quoteName = key;

        if (isUser)
        {
            var potentialUser = guild.GetUser(id);
            if (potentialUser == null)
            {
                quoteName = _users.ContainsKey(id) ? _users[id].Username : $"<@{id}>";
            }
            else quoteName = potentialUser.DisplayName;
        }

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
    public Quote GetRandomQuote(IUser user)
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
    /// Get a random quote in a category.
    /// </summary>
    /// <param name="name">The category name to get the quote of.</param>
    /// <returns>A Quote containing the quote information.</returns>
    /// <exception cref="NullReferenceException">If the category doesn't have any quotes.</exception>
    public Quote GetRandomQuote(string name)
    {
        if (_quoteStorage.Quotes.Keys.All(key => key.ToLower() != name.ToLower()))
            throw new NullReferenceException("That category does not have any quotes.");

        var keyValuePair = _quoteStorage.Quotes.Single(pair => pair.Key.ToLower() == name.ToLower());
        var quotes = keyValuePair.Value;

        Random rnd = new Random();
        var quoteId = rnd.Next(quotes.Count);
        
        var quoteName = keyValuePair.Key;
        if (ulong.TryParse(quoteName, out var userId))
        {
            // It's a user, but since we're technically looking for a category, this user likely left.
            // Check to see if Izzy knows about them, if she does, set quote name to username, else just mention them.
            quoteName = _users.ContainsKey(userId) ? _users[userId].Username : $"<@{userId}>";
        }
        
        var quoteContent = quotes[quoteId];

        return new Quote(quoteId, quoteName, quoteContent);
    }

    /// <summary>
    /// Add a quote to a user.
    /// </summary>
    /// <param name="user">The user to add the quote to.</param>
    /// <param name="content">The content of the quote.</param>
    /// <returns>The newly created Quote.</returns>
    public async Task<Quote> AddQuote(IUser user, string content)
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
    /// Add a quote to a category.
    /// </summary>
    /// <param name="name">The category name to add the quote to.</param>
    /// <param name="content">The content of the quote.</param>
    /// <returns>The newly created Quote.</returns>
    public async Task<Quote> AddQuote(string name, string content)
    {
        if (_quoteStorage.Quotes.Keys.All(key => key.ToLower() != name.ToLower()))
            _quoteStorage.Quotes.Add(name, new List<string>());

        var keyValuePair = _quoteStorage.Quotes.Single(pair => pair.Key.ToLower() != name.ToLower());

        _quoteStorage.Quotes[keyValuePair.Key].Add(content);
        
        await FileHelper.SaveQuoteStorageAsync(_quoteStorage);

        var quoteId = _quoteStorage.Quotes[keyValuePair.Key].Count - 1;

        return new Quote(quoteId, keyValuePair.Key, content);
    }
    
    /// <summary>
    /// Remove a quote from a user.
    /// </summary>
    /// <param name="user">The user to remove the quote from.</param>
    /// <param name="id">The id of the quote to remove.</param>
    /// <returns>The Quote that was removed.</returns>
    /// <exception cref="NullReferenceException">If the user doesn't have any quotes.</exception>
    /// <exception cref="IndexOutOfRangeException">If the quote id provided doesn't exist.</exception>
    public async Task<Quote> RemoveQuote(IUser user, int id)
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
    
    /// <summary>
    /// Remove a quote from a category.
    /// </summary>
    /// <param name="name">The category name to remove the quote from.</param>
    /// <param name="id">The id of the quote to remove.</param>
    /// <returns>The Quote that was removed.</returns>
    /// <exception cref="NullReferenceException">If the category doesn't have any quotes.</exception>
    /// <exception cref="IndexOutOfRangeException">If the quote id provided doesn't exist.</exception>
    public async Task<Quote> RemoveQuote(string name, int id)
    {
        if (_quoteStorage.Quotes.Keys.Any(key => key.ToLower() != name.ToLower()))
            throw new NullReferenceException("That category does not have any quotes.");
        
        var keyValuePair = _quoteStorage.Quotes.Single(pair => pair.Key.ToLower() != name.ToLower());
        var quotes = keyValuePair.Value;

        if (quotes.Count <= id) throw new IndexOutOfRangeException("That quote ID does not exist.");

        var quoteContent = quotes[id];
        
        _quoteStorage.Quotes[keyValuePair.Key].RemoveAt(id);

        if (_quoteStorage.Quotes[keyValuePair.Key].Count == 0)
            _quoteStorage.Quotes.Remove(keyValuePair.Key);
        
        await FileHelper.SaveQuoteStorageAsync(_quoteStorage);
        
        return new Quote(id, keyValuePair.Key, quoteContent);
    }
}

public class Quote
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