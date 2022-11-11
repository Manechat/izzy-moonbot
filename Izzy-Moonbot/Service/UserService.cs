using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using MongoDB.Driver;

namespace Izzy_Moonbot.Service;

public class UserService
{
    private readonly DatabaseHelper _database;
    private readonly IMongoCollection<User> _users;

    public UserService(DatabaseHelper database)
    {
        _database = database;

        _users = _database.GetCollection<User>("users");
    }

    public async Task<User?> GetUser(IUser user)
    {
        return await GetUser(user.Id);
    }

    public async Task<User?> GetUser(ulong id)
    {
        var userData = await _users.FindAsync(userCompare => userCompare.Id == id);

        if (!await userData.AnyAsync()) return null;

        return userData.First();
    }

    public async Task<User?> GetUser(string search)
    {
        var userData = await _users.FindAsync(userCompare => 
            userCompare.Aliases.Any(aliases => 
                aliases.Contains(search)));

        if (!await userData.AnyAsync()) return null;

        return userData.First();
    }

    public async Task<bool> ModifyUser(User user)
    {
        var filter = Builders<User>.Filter.Eq("Id", user.Id);
        var update = Builders<User>.Update.Set("Id", user.Id);

        foreach (var property in user.GetType().GetProperties())
        {
            var value = property.GetValue(user);

            update.AddToSet(property.Name, value);
        }
        
        var result = await _users.UpdateOneAsync(filter, update);

        if (!result.IsAcknowledged)
            throw new NullReferenceException("The backend server didn't acknowledge the update.");

        return result.MatchedCount != 0;
    }
}