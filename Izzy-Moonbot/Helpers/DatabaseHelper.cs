using System;
using System.Linq;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Izzy_Moonbot.Helpers;

/// <summary>
/// A helper which helps with Database connections.
/// </summary>
public class DatabaseHelper
{
    private readonly IMongoDatabase _database;

    public DatabaseHelper(IConfiguration botSettings)
    {
        // Get the database config.
        // It has to be done like this because attributes don't get the services and settings.
        var section = botSettings.GetSection(nameof(DatabaseSettings));
        var settings = section.Get<DatabaseSettings>() ?? throw new InvalidOperationException("Database settings is null. Cannot continue.");
        
        var optionString = "";
        if (settings.Options.Count != 0)
            optionString = $"?{string.Join("&", settings.Options.Select(pair => $"{pair.Key}={pair.Value}"))}";
        
        var connectionUrl = $"{settings.Protocol}://{_makeStringSafe(settings.User)}:{_makeStringSafe(settings.Password)}@{settings.Host}/{optionString}";
        
        var client = new MongoClient(connectionUrl);

        client.StartSession();
        
        Console.WriteLine(connectionUrl);

        _database = client.GetDatabase(settings.Database);
        
        Console.WriteLine(_database.DatabaseNamespace.DatabaseName);
    }

    private static string _makeStringSafe(string str)
    {
        str = str.Replace("%", "%25"); // Do this first else we'll be replacing ones we don't want to replace.

        str = str
            .Replace("!", "%21")
            .Replace("#", "%23")
            .Replace("$", "%24")
            .Replace("&", "%26")
            .Replace("'", "%27")
            .Replace("(", "%28")
            .Replace(")", "%29")
            .Replace("*", "%2A")
            .Replace("+", "%2B")
            .Replace(",", "%2C")
            .Replace("/", "%2F")
            .Replace(":", "%3A")
            .Replace(";", "%3B")
            .Replace("=", "%3D")
            .Replace("?", "%3F")
            .Replace("@", "%40")
            .Replace("[", "%5B")
            .Replace("]", "%5D");

        return str;
    }
    
    /// <summary>
    /// Gets a Collection, or creates one if it doesn't exist.
    /// </summary>
    /// <param name="collection">The name of the collection to get.</param>
    /// <returns>The collection containing BsonDocuments.</returns>
    public IMongoCollection<BsonDocument> GetCollection(string collection) => _database.GetCollection<BsonDocument>(collection);

    /// <summary>
    /// Gets a Collection, or creates one if it doesn't exist.
    /// </summary>
    /// <typeparam name="T">The type to cast to.</typeparam>
    /// <param name="collection">The name of the collection to get.</param>
    /// <returns>The collection containing the type casted to.</returns>
    public IMongoCollection<T> GetCollection<T>(string collection) => _database.GetCollection<T>(collection);
}