using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using MongoDB.Driver;

namespace Izzy_Moonbot.Service;

public class UserService
{
    private readonly List<User>? _testUsers;
    private readonly IMongoCollection<User>? _users;

    public UserService(DatabaseHelper? database)
    {
        if (database == null)
        {
            _testUsers = new List<User>();
        }
        else
        {
            _users = database.GetCollection<User>("users");
        }
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
    /// <param name="user">The IIzzyGuildUser instance to check.</param>
    /// <returns>`true` if the user exists, `false` if not.</returns>
    public async Task<bool> Exists(IIzzyGuildUser user) => await GetUser(user) != null;
    
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
    /// Get a user by an instance of IIzzyGuildUser.
    /// </summary>
    /// <param name="user">The IIzzyGuildUser instance to get information for.</param>
    /// <returns>A user object, or null if no user is found.</returns>
    public async Task<User?> GetUser(IIzzyGuildUser user) => await GetUser(user.Id);
    
    /// <summary>
    /// Get a user by their Discord ID.
    /// </summary>
    /// <param name="id">The ID to search for.</param>
    /// <returns>A user object, or null if no user is found.</returns>
    public async Task<User?> GetUser(ulong id)
    {
        if (_users != null)
        {
            var userData = await _users.FindAsync(userCompare => userCompare.Id == id);

            return userData.FirstOrDefault();
        }
        else
        {
            if (_testUsers == null) throw new InvalidOperationException("Both _users and _testUsers is null!");
            var userData = _testUsers.FindAll(userCompare => userCompare.Id == id);

            return userData.FirstOrDefault();
        }
    }
    
    /// <summary>
    /// Get a user by searching the users username (and previous nicknames).
    /// </summary>
    /// <param name="search">The string to search for.</param>
    /// <returns>A user object, or null if no user is found.</returns>
    public async Task<User?> GetUser(string search)
    {
        if (_users != null)
        {
            var usersData = await _users.FindAsync(userCompare =>
                userCompare.Aliases.Any(aliases => aliases.Contains(search)));

            return usersData.FirstOrDefault();
        }
        else
        {
            if (_testUsers == null) throw new InvalidOperationException("Both _users and _testUsers is null!");
            var usersData = _testUsers.FindAll(userCompare =>
                userCompare.Aliases.Any(aliases => aliases.Contains(search)));

            return usersData.FirstOrDefault();
        }
    }
    
    /// <summary>
    /// Get multiple users by their instances of IUser.
    /// </summary>
    /// <remarks>
    /// The order is not maintained, do not assume that users[0] == result[0].
    /// </remarks>
    /// <param name="users">The IUser instances to get the users of.</param>
    /// <returns>An array of users.</returns>
    public async Task<User[]> GetUsers(IEnumerable<IUser> users) => await GetUsers(users.Select(user => user.Id));

    /// <summary>
    /// Get multiple users by their instances of IIzzyGuildUser.
    /// </summary>
    /// <remarks>
    /// The order is not maintained, do not assume that users[0] == result[0].
    /// </remarks>
    /// <param name="user">The IIzzyGuildUser instances to get the users of.</param>
    /// <returns>An array of users.</returns>
    public async Task<User[]> GetUsers(IEnumerable<IIzzyGuildUser> users) => await GetUsers(users.Select(user => user.Id));
    
    /// <summary>
    /// Get multiple users by their IDs.
    /// </summary>
    /// <remarks>
    /// The order is not maintained, do not assume that ids[0] == result[0].
    /// </remarks>
    /// <param name="ids">The IDs of the users to get.</param>
    /// <returns>An array of users.</returns>
    public async Task<User[]> GetUsers(IEnumerable<ulong> ids)
    {
        if (_users != null)
        {
            var userData = await _users.FindAsync(userCompare => ids.Contains(userCompare.Id));

            return userData.ToEnumerable().ToArray();
        }
        else
        {
            if (_testUsers == null) throw new InvalidOperationException("Both _users and _testUsers is null!");
            var userData = _testUsers.FindAll(userCompare => ids.Contains(userCompare.Id));

            return userData.ToArray();
        }
    }

    /// <summary>
    /// Get multiple users from a search string.
    /// </summary>
    /// <param name="search">The string to search for.</param>
    /// <returns>An array of users.</returns>
    public async Task<User[]> GetUsers(string search)
    {
        if (_users != null)
        {
            var usersData =
                await _users.FindAsync(userCompare => userCompare.Aliases.Any(aliases => aliases.Contains(search)));

            return usersData.ToEnumerable().ToArray();
        }
        else
        {
            if (_testUsers == null) throw new InvalidOperationException("Both _users and _testUsers is null!");
            var usersData =
                _testUsers.FindAll(userCompare => userCompare.Aliases.Any(aliases => aliases.Contains(search)));

            return usersData.ToArray();
        }
    }

    /// <summary>
    /// Create a user.
    /// </summary>
    /// <param name="user">The User object to add to the database.</param>
    public async Task CreateUser(User user)
    {
        if (_users != null)
        {
            await _users.InsertOneAsync(user);
        }
        else
        {
            if (_testUsers == null) throw new InvalidOperationException("Both _users and _testUsers is null!");
            _testUsers.Add(user);
        }
    }

    /// <summary>
    /// Create multiple users at once.
    /// </summary>
    /// <param name="users">The User objects to add to the database.</param>
    public async Task CreateUsers(IEnumerable<User> users)
    {
        if (_users != null)
        {
            await _users.InsertManyAsync(users);
        }
        else
        {
            if (_testUsers == null) throw new InvalidOperationException("Both _users and _testUsers is null!");
            _testUsers.AddRange(users);
        }
    }

    private (UpdateDefinition<User>, FilterDefinition<User>) _processUserUpdate(User user)
    {
        var filter = Builders<User>.Filter.Eq("Id", user.Id);
        var update = Builders<User>.Update.Set("Id", user.Id);

        foreach (var property in user.GetType().GetProperties())
        {
            var value = property.GetValue(user);

            update = update.Set(property.Name, value);
        }

        return (update, filter);
    }

    /// <summary>
    /// Modify a users entry in the database.
    /// </summary>
    /// <param name="user">The updated user object</param>
    /// <returns>Whether the modification succeeded.</returns>
    /// <exception cref="NullReferenceException">The user does not exist in the database.</exception>
    /// <exception cref="MongoException">The modification wasn't acknowledged by the database.</exception>
    public async Task<bool> ModifyUser(User user)
    {
        var oldUser = await GetUser(user.Id);
        if (oldUser == null)
            throw new NullReferenceException($"User with id {user.Id} does not exist, cannot modify.");
        
        if (_users != null)
        {
            var builtUpdate = _processUserUpdate(user);
            var update = builtUpdate.Item1;
            var filter = builtUpdate.Item2;

            var result = await _users.UpdateOneAsync(filter, update);

            if (!result.IsAcknowledged)
                throw new MongoException("The backend server did not acknowledge the update.");

            return result.MatchedCount != 0;
        }
        else
        {
            if (_testUsers == null) throw new InvalidOperationException("Both _users and _testUsers is null!");
            var userIndex = _testUsers.FindIndex(userCompare => userCompare.Id == user.Id);

            _testUsers[userIndex] = user;

            return true;
        }
    }
}