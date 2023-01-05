using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Izzy_Moonbot.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Izzy_Moonbot.Helpers;

public static class FileHelper
{
    public static string SetUpFilepath(FilePathType type, string filename, string extension,
        SocketCommandContext? context = null, string logChannel = "", string date = "")
    {
        //Root
        var filepath = DevSettings.RootPath;
        CreateDirectoryIfNotExists(filepath);

        //Server
        if (type != FilePathType.Root)
        {
            if (context != null && context.IsPrivate)
            {
                filepath = Path.Join(filepath, "_users");
                CreateDirectoryIfNotExists(filepath);
                filepath = Path.Join(filepath, $"{context.User.Username}");
                CreateDirectoryIfNotExists(filepath);
            }
            else
            {
                if (context != null)
                {
                    filepath = Path.Join(filepath, "channels");
                    CreateDirectoryIfNotExists(filepath);

                    //channel
                    if (type == FilePathType.Channel)
                    {
                        filepath = Path.Join(filepath, $"{context.Channel.Name}");
                        CreateDirectoryIfNotExists(filepath);
                    }
                    else
                    {
                        filepath = Path.Join(filepath, $"{logChannel}");
                        CreateDirectoryIfNotExists(filepath);
                        filepath = Path.Join(filepath, $"{date}.{extension}");
                        return filepath;
                    }
                }
            }
        }

        filepath = filename switch
        {
            "" => Path.Join(filepath, $"default.{extension}"),
            "<date>" => Path.Join(filepath, $"{DateTime.UtcNow:yyyy-MM-dd}.{extension}"),
            _ => Path.Join(filepath, $"{filename}.{extension}")
        };
        return filepath;
    }

    public static async Task<Config> LoadConfigAsync()
    {
        var settings = new Config();
        var filepath = SetUpFilepath(FilePathType.Root, "config", "conf");
        if (!File.Exists(filepath))
        {
            var defaultFileContents = JsonConvert.SerializeObject(settings, Formatting.Indented);
            await File.WriteAllTextAsync(filepath, defaultFileContents);
        }
        else
        {
            var fileContents = await File.ReadAllTextAsync(filepath);
            settings = JsonConvert.DeserializeObject<Config>(fileContents);
            if (settings == null)
                throw new InvalidDataException($"Failed to deserialize settings at {filepath}");
        }

        return settings;
    }

    public static async Task SaveConfigAsync(Config settings)
    {
        var filepath = SetUpFilepath(FilePathType.Root, "config", "conf");
        var fileContents = JsonConvert.SerializeObject(settings, Formatting.Indented);
        await File.WriteAllTextAsync(filepath, fileContents);
    }

    public static async Task<Dictionary<ulong, User>> LoadUsersAsync()
    {
        var users = new Dictionary<ulong, User>();
        var filepath = SetUpFilepath(FilePathType.Root, "users", "conf");
        if (!File.Exists(filepath))
        {
            var defaultFileContents = JsonConvert.SerializeObject(users, Formatting.Indented);
            await File.WriteAllTextAsync(filepath, defaultFileContents);
        }
        else
        {
            var fileContents = await File.ReadAllTextAsync(filepath);
            users = JsonConvert.DeserializeObject<Dictionary<ulong, User>>(fileContents);
            if (users == null)
                throw new InvalidDataException($"Failed to deserialize users at {filepath}");
        }

        return users;
    }

    public static async Task SaveUsersAsync(Dictionary<ulong, User> users)
    {
        var filepath = SetUpFilepath(FilePathType.Root, "users", "conf");
        var fileContents = JsonConvert.SerializeObject(users, Formatting.Indented);
        await File.WriteAllTextAsync(filepath, fileContents);
    }

    public static async Task<List<ScheduledJob>> LoadScheduleAsync()
    {
        var scheduledJobs = new List<ScheduledJob>();
        var filepath = SetUpFilepath(FilePathType.Root, "scheduled-tasks", "conf");
        if (!File.Exists(filepath))
        {
            var defaultFileContents = JsonConvert.SerializeObject(scheduledJobs, Formatting.Indented);
            await File.WriteAllTextAsync(filepath, defaultFileContents);
        }
        else
        {
            var fileContents = await File.ReadAllTextAsync(filepath);
            scheduledJobs = TestableDeserializeSchedule(fileContents);
        }

        return scheduledJobs;
    }

    public static List<ScheduledJob> TestableDeserializeSchedule(string fileContents)
    {
        var scheduledJobs = JsonConvert.DeserializeObject<List<ScheduledJob>>(fileContents);
        if (scheduledJobs == null)
            throw new InvalidDataException($"Failed to deserialize scheduled jobs");

        if (scheduledJobs.Count == 0) return scheduledJobs;

        var fileJson = JArray.Parse(fileContents);

        for (var i = fileJson.Count - 1; i >= 0; i--)
        {
            if (fileJson[i]["Id"] == null) continue;
            if (fileJson[i]["Action"] == null) continue;
            if (fileJson[i]["Action"]!["Type"] == null) continue;

            // The following aren't null because we check above. Suppress with !
            var id = fileJson[i]["Id"]!.Value<string>();
            var type = fileJson[i]["Action"]!["Type"]!.Value<int>();

            var jobIndex = scheduledJobs.FindIndex(job => job.Id == id);

            scheduledJobs[jobIndex].Action = type switch
            {
                0 => new ScheduledRoleRemovalJob(fileJson[i]["Action"]!["Role"]!.Value<ulong>(),
                    fileJson[i]["Action"]!["User"]!.Value<ulong>(),
                    fileJson[i]["Action"]!["Reason"]!.Value<string>()),
                1 => new ScheduledRoleAdditionJob(fileJson[i]["Action"]!["Role"]!.Value<ulong>(),
                    fileJson[i]["Action"]!["User"]!.Value<ulong>(),
                    fileJson[i]["Action"]!["Reason"]!.Value<string>()),
                2 => new ScheduledUnbanJob(fileJson[i]["Action"]!["User"]!.Value<ulong>(),
                    fileJson[i]["Action"]?["Reason"]?.Value<string>() ?? ""), // need back-compat with the old reason-less unban jobs
                3 => new ScheduledEchoJob(fileJson[i]["Action"]!["ChannelOrUser"]!.Value<ulong>(),
                    fileJson[i]["Action"]!["Content"]!.Value<string>()!),
                4 => new ScheduledBannerRotationJob(),
                5 => new ScheduledBoredCommandsJob(),
                _ => throw new NotImplementedException("This scheduled job type is not implemented.")
            };
        }

        return scheduledJobs;
    }

    public static async Task SaveScheduleAsync(List<ScheduledJob> scheduledTasks)
    {
        var filepath = SetUpFilepath(FilePathType.Root, "scheduled-tasks", "conf");
        var fileContents = TestableSerializeSchedule(scheduledTasks);
        await File.WriteAllTextAsync(filepath, fileContents);
    }

    public static string TestableSerializeSchedule(List<ScheduledJob> scheduledTasks)
    {
        return JsonConvert.SerializeObject(scheduledTasks, Formatting.Indented);
    }

    public static async Task<GeneralStorage> LoadGeneralStorageAsync()
    {
        var generalStorage = new GeneralStorage();
        var filepath = SetUpFilepath(FilePathType.Root, "general-storage", "conf");
        if (!File.Exists(filepath))
        {
            var defaultFileContents = JsonConvert.SerializeObject(generalStorage, Formatting.Indented);
            await File.WriteAllTextAsync(filepath, defaultFileContents);
        }
        else
        {
            var fileContents = await File.ReadAllTextAsync(filepath);
            generalStorage = JsonConvert.DeserializeObject<GeneralStorage>(fileContents);
            if (generalStorage == null)
                throw new InvalidDataException($"Failed to deserialize general storage at {filepath}");
        }

        return generalStorage;
    }

    public static async Task SaveGeneralStorageAsync(GeneralStorage settings)
    {
        var filepath = SetUpFilepath(FilePathType.Root, "general-storage", "conf");
        var fileContents = JsonConvert.SerializeObject(settings, Formatting.Indented);
        await File.WriteAllTextAsync(filepath, fileContents);
    }
    
    public static async Task<QuoteStorage> LoadQuoteStorageAsync()
    {
        var quoteStorage = new QuoteStorage();
        var filepath = SetUpFilepath(FilePathType.Root, "quotes", "conf");
        if (!File.Exists(filepath))
        {
            var defaultFileContents = JsonConvert.SerializeObject(quoteStorage, Formatting.Indented);
            await File.WriteAllTextAsync(filepath, defaultFileContents);
        }
        else
        {
            var fileContents = await File.ReadAllTextAsync(filepath);
            quoteStorage = JsonConvert.DeserializeObject<QuoteStorage>(fileContents);
            if (quoteStorage == null)
                throw new InvalidDataException($"Failed to deserialize quote storage at {filepath}");
        }

        return quoteStorage;
    }

    public static async Task SaveQuoteStorageAsync(QuoteStorage settings)
    {
        var filepath = SetUpFilepath(FilePathType.Root, "quotes", "conf");
        var fileContents = JsonConvert.SerializeObject(settings, Formatting.Indented);
        await File.WriteAllTextAsync(filepath, fileContents);
    }

    private static void CreateDirectoryIfNotExists(string path)
    {
        var directory = new DirectoryInfo(path);
        if (!directory.Exists) directory.Create();
    }
}

public enum FilePathType
{
    Root,
    Channel,
    LogRetrieval
}