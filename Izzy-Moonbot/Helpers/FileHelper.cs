namespace Izzy_Moonbot.Helpers
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Izzy_Moonbot.Settings;
    using Discord.Commands;
    using Newtonsoft.Json;

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
                filepath = Path.Join(filepath, "servers");
                CreateDirectoryIfNotExists(filepath);

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
                        filepath = Path.Join(filepath, $"{context.Guild.Name}");
                        CreateDirectoryIfNotExists(filepath);

                        //channel
                        if (type != FilePathType.Server)
                        {
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
            }

            filepath = filename switch
            {
                "" => Path.Join(filepath, $"default.{extension}"),
                "<date>" => Path.Join(filepath, $"{DateTime.UtcNow:yyyy-MM-dd}.{extension}"),
                _ => Path.Join(filepath, $"{filename}.{extension}"),
            };
            return filepath;
        }

        public static async Task<ServerSettings> LoadServerSettingsAsync(SocketCommandContext context)
        {
            var filepath = SetUpFilepath(FilePathType.Server, "settings", "conf", context);
            var settings = new ServerSettings();
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

        public static async Task SaveServerSettingsAsync(ServerSettings settings, SocketCommandContext context)
        {
            var filepath = SetUpFilepath(FilePathType.Server, "settings", "conf", context);
            var fileContents = JsonConvert.SerializeObject(settings, Formatting.Indented);
            await File.WriteAllTextAsync(filepath, fileContents);
        }

        public static async Task<AllPreloadedSettings> LoadAllPresettingsAsync()
        {
            var servers = new AllPreloadedSettings();
            var filepath = SetUpFilepath(FilePathType.Root, "preloadedsettings", "conf");
            if (!File.Exists(filepath))
            {
                var defaultFileContents = JsonConvert.SerializeObject(servers, Formatting.Indented);
                await File.WriteAllTextAsync(filepath, defaultFileContents);
            }
            else
            {
                var fileContents = await File.ReadAllTextAsync(filepath);
                servers = JsonConvert.DeserializeObject<AllPreloadedSettings>(fileContents);
            }

            return servers;
        }

        public static async Task<ServerPreloadedSettings> LoadServerPresettingsAsync(SocketCommandContext context, AllPreloadedSettings allPresettingsInput = null)
        {
            AllPreloadedSettings allPresettings;
            if (allPresettingsInput == null)
            {
                allPresettings = await LoadAllPresettingsAsync();
            }
            else
            {
                allPresettings = allPresettingsInput;
            }

            var settings = new ServerPreloadedSettings();
            var serverId = context.IsPrivate ? context.User.Id : context.Guild.Id;
            var name = context.IsPrivate ? context.User.Username + "#" + context.User.Discriminator : context.Guild.Name;
            settings.Name = name;
            if (allPresettings.Settings.ContainsKey(serverId))
            {
                settings = allPresettings.Settings[serverId];
            }
            else
            {
                allPresettings.Settings.Add(serverId, settings);
                await SaveAllPresettingsAsync(allPresettings);
            }

            return settings;
        }

        public static async Task SaveAllPresettingsAsync(AllPreloadedSettings settings)
        {
            var filepath = SetUpFilepath(FilePathType.Root, "preloadedsettings", "conf");
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
        Server,
        Channel,
        LogRetrieval,
    }
}
