namespace Izzy_Moonbot.Helpers
{
    using Discord.Commands;
    using Izzy_Moonbot.Settings;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public static class FileHelper
    {
        public static string SetUpFilepath(FilePathType type, string filename, string extension, SocketCommandContext context = null, string logChannel = "", string date = "")
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
                _ => Path.Join(filepath, $"{filename}.{extension}"),
            };
            return filepath;
        }

        public static async Task<ServerSettings> LoadAllPresettingsAsync()
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

        public static async Task<ServerSettings> LoadServerPresettingsAsync(ServerSettings allPresettingsInput = null)
        {
            ServerSettings settings;
            if (allPresettingsInput == null)
            {
                settings = await LoadAllPresettingsAsync();
            }
            else
            {
                settings = allPresettingsInput;
                await SaveAllPresettingsAsync(settings);
            }

            return settings;
        }

        public static async Task SaveAllPresettingsAsync(ServerSettings settings)
        {
            var filepath = SetUpFilepath(FilePathType.Root, "settings", "conf");
            var fileContents = JsonConvert.SerializeObject(settings, Formatting.Indented);
            await File.WriteAllTextAsync(filepath, fileContents);
        }

        private static void CreateDirectoryIfNotExists(string path)
        {
            var directory = new DirectoryInfo(path);
            if (!directory.Exists)
            {
                directory.Create();
            }
        }
    }

    public enum FilePathType
    {
        Root,
        Channel,
        LogRetrieval,
    }
}
