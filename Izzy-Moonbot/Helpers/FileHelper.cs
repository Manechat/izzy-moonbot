using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord.Commands;
using Izzy_Moonbot.Settings;
using Newtonsoft.Json;

namespace Izzy_Moonbot.Helpers;

public static class FileHelper
{
    public static string SetUpFilepath(FilePathType type, string filename, string extension,
        SocketCommandContext context = null, string logChannel = "", string date = "")
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

    public static async Task<ServerSettings> LoadSettingsAsync()
    {
        var settings = new ServerSettings();
        var filepath = SetUpFilepath(FilePathType.Root, "settings", "conf");
        if (!File.Exists(filepath))
        {
            var defaultFileContents = JsonConvert.SerializeObject(settings, Formatting.Indented);
            await File.WriteAllTextAsync(filepath, defaultFileContents);
        }
        else
        {
            var fileContents = await File.ReadAllTextAsync(filepath);
            settings = JsonConvert.DeserializeObject<ServerSettings>(fileContents);
        }

        return settings;
    }

    public static async Task SaveSettingsAsync(ServerSettings settings)
    {
        var filepath = SetUpFilepath(FilePathType.Root, "settings", "conf");
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
        }

        return users;
    }

    public static async Task SaveUsersAsync(Dictionary<ulong, User> users)
    {
        var filepath = SetUpFilepath(FilePathType.Root, "users", "conf");
        var fileContents = JsonConvert.SerializeObject(users, Formatting.Indented);
        await File.WriteAllTextAsync(filepath, fileContents);
    }

    public static async Task<List<ScheduledTask>> LoadScheduleAsync()
    {
        var scheduledTasks = new List<ScheduledTask>();
        var filepath = SetUpFilepath(FilePathType.Root, "scheduled-tasks", "conf");
        if (!File.Exists(filepath))
        {
            var defaultFileContents = JsonConvert.SerializeObject(scheduledTasks, Formatting.Indented);
            await File.WriteAllTextAsync(filepath, defaultFileContents);
        }
        else
        {
            var fileContents = await File.ReadAllTextAsync(filepath);
            scheduledTasks = JsonConvert.DeserializeObject<List<ScheduledTask>>(fileContents);
        }

        return scheduledTasks;
    }

    public static async Task SaveScheduleAsync(List<ScheduledTask> scheduledTasks)
    {
        var filepath = SetUpFilepath(FilePathType.Root, "scheduled-tasks", "conf");
        var fileContents = JsonConvert.SerializeObject(scheduledTasks, Formatting.Indented);
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