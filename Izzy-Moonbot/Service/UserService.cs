using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using MongoDB.Driver;

namespace Izzy_Moonbot.Service;

public class UserService
{
    private readonly IMongoCollection<User> _users;

    public UserService(DatabaseHelper database)
    {
        _users = database.GetCollection<User>("users");
    }

    /// <summary>
    /// Check if the user exists in the database.
    /// </summary>
    /// <param name="user">The IUser instance to check.</param>
    /// <returns>`true` if the user exists, `false` if not.</returns>
    public async Task<bool> Exists(IUser user) => await GetUser(user) != null;
    
    /// <summary>
    /// Check if the user exists in the database.
    /// </summary>
    /// <param name="id">The ID of the user to check.</param>
    /// <returns>`true` if the user exists, `false` if not.</returns>
    public async Task<bool> Exists(ulong id) => await GetUser(id) != null;

    /// <summary>
    /// Check if the user exists in the database.
    /// </summary>
    /// <param name="search">The search query to run to find the user.</param>
    /// <returns>`true` if the user exists, `false` if not.</returns>
    public async Task<bool> Exists(string search) => await GetUser(search) != null;

    /// <summary>
    /// Get a user by an instance of IUser.
    /// </summary>
    /// <param name="user">The IUser instance to get information for.</param>
    /// <returns>A user object, or null if no user is found.</returns>
    public async Task<User?> GetUser(IUser user) => await GetUser(user.Id);
    
    /// <summary>
    /// Get a user by their Discord ID.
    /// </summary>
    /// <param name="id">The ID to search for.</param>
    /// <returns>A user object, or null if no user is found.</returns>
    public async Task<User?> GetUser(ulong id)
    {
        var userData = await _users.FindAsync(userCompare => userCompare.Id == id);

        return userData.FirstOrDefault();
    }
    
    /// <summary>
    /// Get a user by searching the users username (and previous nicknames).
    /// </summary>
    /// <param name="search">The string to search for.</param>
    /// <returns>A user object, or null if no user is found.</returns>
    public async Task<User?> GetUser(string search)
    {
        var userData = await _users.FindAsync(userCompare =>
            userCompare.Aliases.Any(aliases => aliases.Contains(search)));
        
        return userData.FirstOrDefault();
    }

    public async Task CreateUser(User user)
    {
        await _users.InsertOneAsync(user);
    }

    public async Task CreateUsers(IEnumerable<User> users)
    {
        await _users.InsertManyAsync(users);
    }

    public async Task<bool> ModifyUser(User user)
    {
        var filter = Builders<User>.Filter.Eq("Id", user.Id);
        var update = Builders<User>.Update.Combine(new ObjectUpdateDefinition<User>(user));

        var oldUser = await GetUser(user.Id);
        if (oldUser == null) throw new NullReferenceException($"User with id {user.Id} does not exist, cannot modify.");

        var result = await _users.UpdateOneAsync(filter, update);

        if (!result.IsAcknowledged)
            throw new MongoException("The backend server did not acknowledge the update.");

        return result.MatchedCount != 0;
    }
}
