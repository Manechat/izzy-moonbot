using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.EventListeners;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Izzy_Moonbot
{
    public class Worker : BackgroundService
    {
        private readonly CommandService _commands;
        private readonly DiscordSettings _discordSettings;
        private readonly FilterService _filterService;
        private readonly ILogger<Worker> _logger;
        private readonly ModLoggingService _modLog;
        private readonly ModService _modService;
        private readonly SpamService _spamService;
        private readonly RaidService _raidService;
        private readonly ScheduleService _scheduleService;
        private readonly IServiceCollection _services;
        private readonly Config _config;
        private readonly Dictionary<ulong, User> _users;
        private readonly UserListener _userListener;
        private DiscordSocketClient _client;
        public bool hasProgrammingSocks = true;
        public int LaserCount = 10;

        public Worker(ILogger<Worker> logger, ModLoggingService modLog, IServiceCollection services, ModService modService, RaidService raidService,
            FilterService filterService, ScheduleService scheduleService, IOptions<DiscordSettings> discordSettings,
            Config config, Dictionary<ulong, User> users, UserListener userListener, SpamService spamService)
        {
            _logger = logger;
            _modLog = modLog;
            _modService = modService;
            _raidService = raidService;
            _filterService = filterService;
            _scheduleService = scheduleService;
            _commands = new CommandService();
            _discordSettings = discordSettings.Value;
            _services = services;
            _config = config;
            _users = users;
            _userListener = userListener;
            _spamService = spamService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var discordConfig = new DiscordSocketConfig { GatewayIntents = GatewayIntents.All, MessageCacheSize = 50 };
                _client = new DiscordSocketClient(discordConfig);
                _client.Log += Log;
                await _client.LoginAsync(TokenType.Bot,
                    _discordSettings.Token);

                _client.Ready += ReadyEvent;

                await _client.StartAsync();
                
                var filepath = FileHelper.SetUpFilepath(FilePathType.Root, "moderation", "log");
                
                if (!File.Exists(filepath))
                    await File.WriteAllTextAsync(filepath, $"----------= {DateTimeOffset.UtcNow:F} =----------{Environment.NewLine}", stoppingToken);
                
                await File.AppendAllTextAsync(filepath, $"----------= {DateTimeOffset.UtcNow:F} =----------{Environment.NewLine}", stoppingToken);

                if (_config.DiscordActivityName != null)
                {
                    await _client.SetGameAsync(_config.DiscordActivityName, type: (_config.DiscordActivityWatching ? ActivityType.Watching : ActivityType.Playing));
                }

                await InstallCommandsAsync();
                
                _userListener.RegisterEvents(_client);
                
                _spamService.RegisterEvents(_client);
                _raidService.RegisterEvents(_client);
                _filterService.RegisterEvents(_client);

                _client.LatencyUpdated += async (int old, int value) =>
                {
                    _logger.Log(LogLevel.Debug, $"Latency = {value}ms.");
                    
                    if (_config.DiscordActivityName != null)
                    {
                        if (_client.Activity.Name != _config.DiscordActivityName ||
                            _client.Activity.Type != (_config.DiscordActivityWatching ? ActivityType.Watching : ActivityType.Playing)) 
                            await _client.SetGameAsync(_config.DiscordActivityName, type: (_config.DiscordActivityWatching ? ActivityType.Watching : ActivityType.Playing));
                    }
                    else
                    {
                        if (_client.Activity.Name != "") 
                            await _client.SetGameAsync("");
                    }
                };

                _client.Disconnected += async (Exception ex) =>
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(5000, stoppingToken);
                        if (_client.ConnectionState is ConnectionState.Disconnected or ConnectionState.Disconnecting)
                        {
                            // Assume softlock, reboot
                            Environment.Exit(254);
                        }

                        if (_client.ConnectionState == ConnectionState.Connecting)
                        {
                            await Task.Delay(5000, stoppingToken);
                            if (_client.ConnectionState is ConnectionState.Disconnected or ConnectionState.Disconnecting or ConnectionState.Connecting)
                            {
                                // Assume softlock, reboot
                                Environment.Exit(254);
                            }
                        }
                    });
                };

                // Block this task until the program is closed.
                await Task.Delay(-1, stoppingToken);
                
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(1000, stoppingToken);
                }
            }
            finally
            {
                await _client.StopAsync();
            }
        }

        private async Task InstallCommandsAsync()
        {
            _client.MessageReceived += HandleMessageReceivedAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services.BuildServiceProvider());
        }

        public async Task ReadyEvent()
        {
            _logger.LogTrace("Ready event called");
            _scheduleService.BeginUnicycleLoop(_client);
            
            foreach (var clientGuild in _client.Guilds)
            {
                await clientGuild.DownloadUsersAsync();
            }

            ResyncUsers();
        }

        private void ResyncUsers()
        {
            Task.Run(async () =>
            {
                var guild = _client.Guilds.Single(guild => guild.Id == _discordSettings.DefaultGuild);
                if (!guild.HasAllMembers) await guild.DownloadUsersAsync();

                var newUserCount = 0;
                var reloadUserCount = 0;
                var knownUserCount = 0;

                await foreach (var socketGuildUser in guild.Users.ToAsyncEnumerable())
                {
                    var skip = false;
                    if (!_users.ContainsKey(socketGuildUser.Id))
                    {
                        var newUser = new User();
                        newUser.Username = $"{socketGuildUser.Username}#{socketGuildUser.Discriminator}";
                        newUser.Aliases.Add(socketGuildUser.Username);
                        if (socketGuildUser.JoinedAt.HasValue) newUser.Joins.Add(socketGuildUser.JoinedAt.Value);
                        _users.Add(socketGuildUser.Id, newUser);
                        
                        // Process new member
                        _userListener.MemberJoinEvent(socketGuildUser, true);
                        
                        newUserCount += 1;
                        skip = true;
                    }
                    else
                    {
                        if (_users[socketGuildUser.Id].Username !=
                            $"{socketGuildUser.Username}#{socketGuildUser.Discriminator}")
                        {
                            _users[socketGuildUser.Id].Username =
                                $"{socketGuildUser.Username}#{socketGuildUser.Discriminator}";
                            if (!skip) reloadUserCount += 1;
                            skip = true;
                        }

                        if (!_users[socketGuildUser.Id].Aliases.Contains(socketGuildUser.DisplayName))
                        {
                            _users[socketGuildUser.Id].Aliases.Add(socketGuildUser.DisplayName);
                            if (!skip) reloadUserCount += 1;
                            skip = true;
                        }

                        if (socketGuildUser.JoinedAt.HasValue &&
                            !_users[socketGuildUser.Id].Joins.Contains(socketGuildUser.JoinedAt.Value))
                        {
                            _users[socketGuildUser.Id].Joins.Add(socketGuildUser.JoinedAt.Value);
                            if (!skip) reloadUserCount += 1;
                            skip = true;
                        }

                        if (_config.MemberRole != null)
                        {
                            if (_users[socketGuildUser.Id].Silenced &&
                                socketGuildUser.Roles.Select(role => role.Id).Contains((ulong)_config.MemberRole))
                            {
                                // Unsilenced, Remove the flag.
                                _users[socketGuildUser.Id].Silenced = false;
                                if (!skip) reloadUserCount += 1;
                                skip = true;
                            }

                            if (!_users[socketGuildUser.Id].Silenced &&
                                !socketGuildUser.Roles.Select(role => role.Id).Contains((ulong)_config.MemberRole))
                            {
                                // Silenced, add the flag
                                _users[socketGuildUser.Id].Silenced = true;
                                if (!skip) reloadUserCount += 1;
                                skip = true;
                            }
                        }

                        foreach (var roleId in _config.RolesToReapplyOnRejoin)
                        {
                            if (!_users[socketGuildUser.Id].RolesToReapplyOnRejoin.Contains(roleId) &&
                                socketGuildUser.Roles.Select(role => role.Id).Contains(roleId))
                            {
                                _users[socketGuildUser.Id].RolesToReapplyOnRejoin.Add(roleId);
                                if (!skip) reloadUserCount += 1;
                                skip = true;
                            }

                            if (_users[socketGuildUser.Id].RolesToReapplyOnRejoin.Contains(roleId) &&
                                !socketGuildUser.Roles.Select(role => role.Id).Contains(roleId))
                            {
                                _users[socketGuildUser.Id].RolesToReapplyOnRejoin.Remove(roleId);
                                if (!skip) reloadUserCount += 1;
                                skip = true;
                            }
                        }

                        foreach (var roleId in _users[socketGuildUser.Id].RolesToReapplyOnRejoin)
                        {
                            if (!socketGuildUser.Guild.Roles.Select(role => role.Id).Contains(roleId))
                            {
                                _users[socketGuildUser.Id].RolesToReapplyOnRejoin.Remove(roleId);
                                _config.RolesToReapplyOnRejoin.Remove(roleId);
                                await FileHelper.SaveConfigAsync(_config);
                                if (!skip) reloadUserCount += 1;
                                skip = true;
                            }
                            else
                            {

                                if (!_config.RolesToReapplyOnRejoin.Contains(roleId))
                                {
                                    _users[socketGuildUser.Id].RolesToReapplyOnRejoin.Remove(roleId);
                                    if (!skip) reloadUserCount += 1;
                                    skip = true;
                                }
                            }
                        }

                        if (!skip) knownUserCount += 1;
                    }
                }

                await FileHelper.SaveUsersAsync(_users);

                _logger.LogInformation(
                    $"Resynced users. {guild.Users.Count} users found, {newUserCount} unknown, {reloadUserCount} required update, {knownUserCount} up to date.");
                
                // Get stowaways
                var stowawayList = new HashSet<SocketGuildUser>();
        
                await foreach (var socketGuildUser in guild.Users.ToAsyncEnumerable())
                {
                    if (socketGuildUser.IsBot) continue; // Bots aren't stowaways
                    if (socketGuildUser.Roles.Select(role => role.Id).Contains(_config.ModRole)) continue; // Mods aren't stowaways

                    if (!socketGuildUser.Roles.Select(role => role.Id).Contains((ulong)_config.MemberRole))
                    {
                        // Doesn't have member role, add to stowaway list.
                        stowawayList.Add(socketGuildUser);
                    }
                }

                if (stowawayList.Count != 0)
                {
                    var stowawayStringList = stowawayList.Select(user => $"<@{user.Id}>");
                    var stowawayStringFileList = stowawayList.Select(user => $"{user.Username}#{user.Discriminator}");
                    
                    await _modLog.CreateModLog(guild)
                        .SetContent($"I found these stowaways after I rebooted, cannot tell if they're new users:{Environment.NewLine}" +
                                    string.Join(", ", stowawayStringList))
                        .SetFileLogContent($"I found these stowaways after I rebooted, cannot tell if they're new users:{Environment.NewLine}" +
                                           string.Join(", ", stowawayStringFileList))
                        .Send();
                }
            });
        }

        private async Task HandleMessageReceivedAsync(SocketMessage messageParam)
        {
            if (messageParam.Type != MessageType.Default && messageParam.Type != MessageType.Reply &&
                messageParam.Type != MessageType.ThreadStarterMessage) return;
            SocketUserMessage message = messageParam as SocketUserMessage;
            int argPos = 0;
            SocketCommandContext context = new SocketCommandContext(_client, message);

            if (DevSettings.UseDevPrefix)
            {
                _config.Prefix = DevSettings.Prefix;
            }

            if (message.HasCharPrefix(_config.Prefix, ref argPos) ||
                message.Content.StartsWith($"<@{_client.CurrentUser.Id}>"))
            {
                _logger.Log(LogLevel.Information, $"Received possible command: {messageParam.CleanContent}");

                string parsedMessage;
                var checkCommands = true;
                if (message.Content.StartsWith($"<@{_client.CurrentUser.Id}>"))
                {
                    checkCommands = false;
                    parsedMessage = "<mention>";
                }
                else
                {
                    parsedMessage = message.Content[1..].TrimStart();
                    
                    if (_config.Aliases.Count != 0)
                    {
                        var command = parsedMessage.Split(" ");
                        
                        foreach (var keyValuePair in _config.Aliases)
                        {
                            if (command[0] != keyValuePair.Key) continue;
                            // Alias match
                            
                            var commandAlias = keyValuePair.Value.StartsWith(_config.Prefix)
                                ? keyValuePair.Value[1..].TrimStart().Split(" ")[0]
                                : keyValuePair.Value.TrimStart().Split(" ")[0];
                            
                            if (_config.Aliases.Any(alias => alias.Key == commandAlias))
                            {
                                await context.Channel.SendMessageAsync(
                                    $"**Warning!** This alias directs to another alias!{Environment.NewLine}Izzy doesn't support aliases feeding into aliases. Please remove this alias or redirect it to an existing command.");
                                return;
                            }

                            if (_commands.Commands.All(cmd => cmd.Name != commandAlias))
                            {
                                await context.Channel.SendMessageAsync(
                                    $"**Warning!** This alias directs to a non-existent command!{Environment.NewLine}Please remove this alias or redirect it to an existing command.");
                                return;
                            }

                            command[0] = keyValuePair.Value.StartsWith(_config.Prefix)
                                ? keyValuePair.Value[1..]
                                : keyValuePair.Value;
                        }

                        parsedMessage = string.Join(" ", command);
                    }
                }
                
                if (checkCommands)
                {
                    var validCommand = _commands.Commands.Any(command => 
                        command.Name == parsedMessage.Split(" ")[0] 
                        || command.Aliases.Contains(parsedMessage.Split(" ")[0]));

                    if (!validCommand)
                    {
                        _logger.Log(LogLevel.Information, $"Ignoring message {messageParam.CleanContent} because it doesn't match any command or alias names");
                        return;
                    }
                }
                
                var searchResult = _commands.Search(parsedMessage);
                var commandToExec = searchResult.Commands[0].Command;

                // Check for DMsAllowed attribute
                var hasDMsAllowedAttribute = commandToExec.Preconditions.Where(attribute => attribute != null).OfType<DMsAllowedAttribute>().Any();

                if (!DiscordHelper.ShouldExecuteInPrivate(hasDMsAllowedAttribute, context)) return;

                // Check for BotsAllowed attribute
                var hasBotsAllowedAttribute = commandToExec.Preconditions.Where(attribute => attribute != null).OfType<BotsAllowedAttribute>().Any();

                if (!hasBotsAllowedAttribute && context.User.IsBot) return;

                var result = await _commands.ExecuteAsync(context, parsedMessage, _services.BuildServiceProvider());
                if (result.Error == CommandError.ParseFailed &&
                    result.ErrorReason.StartsWith("Failed to parse "))
                {
                    await context.Channel.SendMessageAsync(
                        $"Sorry, I was unable to process that command because when I tried to parse a value into an {result.ErrorReason.Split(" ")[3]} but failed." +
                        $"Please run `.help {commandToExec.Name}` for usage information about this command.");
                }

                if (result.Error == CommandError.Exception)
                {
                    await context.Channel.SendMessageAsync(
                        $"Sorry, something went wrong while processing that command.");
                    
                    _logger.LogError($"An exception occured while processing a command: {Environment.NewLine}" +
                                     $"Command: {parsedMessage}" +
                                     $"Exception: {result.ErrorReason}");
                }
            }
        }

        private Task Log(LogMessage msg)
        {
            if (msg.Exception != null)
            {
                if (msg.Exception.Message == "Server missed last heartbeat")
                {
                    _logger.LogWarning("Izzy Moonbot missed a heartbeat (likely network interruption).");
                }
                else
                {
                    _logger.LogError("Izzy Moonbot has encountered an error. Logging information...");
                    _logger.LogError($"Message: {msg.Exception.Message}");
                    _logger.LogError($"Source: {msg.Exception.Source}");
                    _logger.LogError($"HResult: {msg.Exception.HResult}");
                    _logger.LogError($"Stack trace: {msg.Exception.StackTrace}");
                }
            }

            _logger.LogInformation(msg.Message);
            return Task.CompletedTask;
        }
    }
}