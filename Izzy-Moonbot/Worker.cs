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
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.EventListeners;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Izzy_Moonbot.Types;
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
        private readonly State _state;
        private readonly Dictionary<ulong, User> _users;
        private readonly ConfigListener _configListener;
        private readonly UserListener _userListener;
        private readonly MessageListener _messageListener;
        private DiscordSocketClient _client;
        public bool hasProgrammingSocks = true;
        public int LaserCount = 10;

        public Worker(ILogger<Worker> logger, ModLoggingService modLog, IServiceCollection services, ModService modService, RaidService raidService,
            FilterService filterService, ScheduleService scheduleService, IOptions<DiscordSettings> discordSettings,
            Config config, State state, Dictionary<ulong, User> users, UserListener userListener, SpamService spamService, ConfigListener configListener, MessageListener messageListener)
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
            _state = state;
            _users = users;
            _userListener = userListener;
            _spamService = spamService;
            _configListener = configListener;
            _messageListener = messageListener;

            var discordConfig = new DiscordSocketConfig {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.MessageContent,
                MessageCacheSize = 50
            };
            _client = new DiscordSocketClient(discordConfig);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
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

                var clientAdapter = new DiscordSocketClientAdapter(_client);

                _configListener.RegisterEvents(_client);
                _userListener.RegisterEvents(_client);
                _messageListener.RegisterEvents(clientAdapter);

                _spamService.RegisterEvents(clientAdapter);
                _raidService.RegisterEvents(_client);
                _filterService.RegisterEvents(clientAdapter);
                _scheduleService.RegisterEvents(clientAdapter);

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
                    var _ = Task.Run(async () =>
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
            _client.MessageReceived += async (message) => await DiscordHelper.LeakOrAwaitTask(HandleMessageReceivedAsync(message));

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services.BuildServiceProvider());
        }

        public async Task ReadyEvent()
        {
            _logger.LogInformation("ReadyEvent() called");

            TaskScheduler.UnobservedTaskException += (object? sender, UnobservedTaskExceptionEventArgs eventArgs) =>
            {
                var unobservedException = eventArgs.Exception.InnerException;
                _logger.LogError($"An UnobservedTaskException occured, i.e. one of Izzy's async tasks threw an exception that remained unhandled " +
                                  "until the task was GC'd. That usually means the issue was in an event handler rather than a command handler.\n" +
                                 $"Unobserved Exception Message: {unobservedException?.Message}\n" +
                                 $"Unobserved Exception Stack: {unobservedException?.StackTrace}");
            };

            foreach (var clientGuild in _client.Guilds)
            {
                _logger.LogDebug($"ReadyEvent() downloading users for guild {clientGuild.Name} ({clientGuild.Id})");
                await clientGuild.DownloadUsersAsync();
            }

            _logger.LogDebug("ReadyEvent() resyncing users");
            ResyncUsers();

            _logger.LogDebug("ReadyEvent() starting unicycle loop");
            _scheduleService.BeginUnicycleLoop(new DiscordSocketClientAdapter(_client));
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
                        await _userListener.MemberJoinEvent(socketGuildUser, true);
                        
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
                var stowawaySet = new HashSet<SocketGuildUser>();
        
                await foreach (var socketGuildUser in guild.Users.ToAsyncEnumerable())
                {
                    if (socketGuildUser.IsBot) continue; // Bots aren't stowaways
                    if (socketGuildUser.Roles.Select(role => role.Id).Contains(_config.ModRole)) continue; // Mods aren't stowaways

                    if (_config.MemberRole is ulong roleId && !socketGuildUser.Roles.Select(role => role.Id).Contains(roleId))
                    {
                        // Doesn't have member role, add to stowaway list.
                        stowawaySet.Add(socketGuildUser);
                    }
                }

                if (stowawaySet.Count != 0)
                {
                    var stowawayStringList = stowawaySet.Select(user => $"<@{user.Id}>");
                    var stowawayStringFileList = stowawaySet.Select(user => $"{user.Username}#{user.Discriminator}");
                    
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
            if (messageParam is not SocketUserMessage message) return;
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

                if (message.Content.StartsWith($"<@{_client.CurrentUser.Id}>"))
                {
                    if (!_config.MentionResponseEnabled)
                    {
                        _logger.Log(LogLevel.Information, $"Ignoring mentioni because MentionResponseEnabled is false.");
                        return;
                    }
                    var secondsSinceLastMention = (DateTimeOffset.UtcNow - _state.LastMentionResponse).TotalSeconds;
                    if (secondsSinceLastMention < _config.MentionResponseCooldown)
                    {
                        _logger.Log(LogLevel.Information, $"Ignoring mention because it's only been {secondsSinceLastMention} seconds since the last one. " +
                            $"(MentionResponseCooldown is {_config.MentionResponseCooldown})");
                        return;
                    }

                    var random = new Random();
                    var index = random.Next(_config.MentionResponses.Count);
                    var response = _config.MentionResponses.ElementAt(index); // Random response

                    _state.LastMentionResponse = DateTimeOffset.UtcNow;

                    await context.Channel.SendMessageAsync($"{response}");
                    return;
                }

                string parsedMessage = message.Content[1..].TrimStart();
                    
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
                
                var inputCommandName = parsedMessage.Split(" ")[0];
                var validCommand = _commands.Commands.Any(command => 
                    command.Name == inputCommandName || command.Aliases.Contains(inputCommandName));

                if (!validCommand)
                {
                    var isDev = DiscordHelper.IsDev(context.User.Id);
                    var isMod = (context.User is SocketGuildUser guildUser) && (guildUser.Roles.Any(r => r.Id == _config.ModRole));

                    Func<string, bool> isSuggestable = item =>
                        DiscordHelper.WithinLevenshteinDistanceOf(inputCommandName, item, Convert.ToUInt32(item.Length / 2));

                    Func<CommandInfo, bool> canRunCommand = cinfo =>
                    {
                        var modAttr = cinfo.Preconditions.Any(attribute => attribute is ModCommandAttribute);
                        var devAttr = cinfo.Preconditions.Any(attribute => attribute is DevCommandAttribute);
                        if (modAttr && devAttr) return isMod || isDev;
                        else if (modAttr) return isMod;
                        else if (devAttr) return isDev;
                        else return true;
                    };
                    Func<string, bool> canRunCommandName = name =>
                    {
                        var cinfo = _commands.Commands.Where(c => c.Name == name).SingleOrDefault((CommandInfo?)null);
                        return cinfo is null ? false : canRunCommand(cinfo);
                    };

                    // don't bother searching command.Name because command.Aliases always includes the main name
                    var alternateNamesToSuggest = _commands.Commands.Where(canRunCommand)
                        .SelectMany(c => c.Aliases).Where(isSuggestable);
                    var aliasesToSuggest = _config.Aliases.Where(pair => canRunCommandName(pair.Value.TrimStart().Split(" ")[0]))
                        .Select(pair => pair.Key).Where(isSuggestable);

                    if (alternateNamesToSuggest.Any() || aliasesToSuggest.Any())
                    {
                        var suggestibles = alternateNamesToSuggest.Concat(aliasesToSuggest).Select(s => $"`.{s}`");
                        var suggestionMessage = $"Sorry, I don't have a `.{inputCommandName}` command. Did you mean {string.Join(" or ", suggestibles)}?";
                        await context.Channel.SendMessageAsync(suggestionMessage);
                        return;
                    }
                    else
                    {
                        _logger.Log(LogLevel.Information, $"Ignoring message {messageParam.CleanContent} because it doesn't match " +
                            $"any command or alias names, and is not similar enough to any of them to make a suggestion.");
                        return;
                    }
                }
                
                var searchResult = _commands.Search(parsedMessage);
                var commandToExec = searchResult.Commands[0].Command;

                // Check for DMsAllowed attribute
                var hasExternalUsageAllowedAttribute = commandToExec.Preconditions.Where(attribute => attribute != null).OfType<ExternalUsageAllowed>().Any();

                if (!DiscordHelper.ShouldExecuteInPrivate(hasExternalUsageAllowedAttribute, context)) return;

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

                    var underlyingException = ((Discord.Commands.ExecuteResult)result).Exception;
                    _logger.LogError($"An exception occured while processing a command:\n" +
                                     $"Command: {parsedMessage}\n" +
                                     $"Exception Message: {underlyingException.Message}\n" +
                                     $"Exception Stack: {underlyingException.StackTrace}");
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