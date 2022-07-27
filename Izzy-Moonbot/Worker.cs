using System.Linq;

namespace Izzy_Moonbot
{
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Izzy_Moonbot.Helpers;
    using Izzy_Moonbot.Settings;
    using Izzy_Moonbot.Service;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Izzy_Moonbot.Attributes;

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceCollection _services;
        private readonly PressureService _pressureService;
        private readonly ModService _modService;
        private readonly RoleService _roleService;
        private readonly RaidService _raidService;
        private readonly FilterService _filterService;
        private readonly ScheduleService _scheduleService;
        private readonly DiscordSettings _discordSettings;
        private readonly CommandService _commands;
        private readonly ServerSettings _settings;
        private readonly Dictionary<ulong, User> _users;
        private DiscordSocketClient _client;
        private int LaserCount = 10;

        public Worker(ILogger<Worker> logger, IServiceCollection services, PressureService pressureService, ModService modService, RoleService roleService, RaidService raidService, FilterService filterService, ScheduleService scheduleService, IOptions<DiscordSettings> discordSettings, ServerSettings settings, Dictionary<ulong, User> users)
        {
            _logger = logger;
            _pressureService = pressureService;
            _modService = modService;
            _roleService = roleService;
            _raidService = raidService;
            _filterService = filterService;
            _scheduleService = scheduleService;
            _commands = new CommandService();
            _discordSettings = discordSettings.Value;
            _services = services;
            _settings = settings;
            _users = users;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var _config = new DiscordSocketConfig { GatewayIntents = GatewayIntents.All, MessageCacheSize = 50 };
                _client = new DiscordSocketClient(_config);
                _client.Log += Log;
                await _client.LoginAsync(TokenType.Bot,
                    _discordSettings.Token);

                _client.Ready += ReadyEvent;
                
                await _client.StartAsync();
                //await _client.SetGameAsync($"Go away");
                await _client.SetGameAsync($"Pressure + AntiRaid System Test - SafeMode Active");
                await InstallCommandsAsync();

                _client.UserJoined += HandleMemberJoin;
                _client.MessageUpdated += HandleMessageUpdate;

                _client.LatencyUpdated += async (int old, int value) =>
                {
                    _logger.Log(LogLevel.Debug, $"Latency = {value}ms.");
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
            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services.BuildServiceProvider());
        }

        public async Task ReadyEvent()
        {
            _logger.LogTrace("Ready event called");
            foreach (var socketGuild in _client.Guilds.ToArray())
            {
                _logger.LogTrace(socketGuild.Name);
            }

            _logger.LogTrace(_client.Guilds.ToArray()[0].Name);
            _scheduleService.ResumeScheduledTasks(_client.Guilds.ToArray()[0]);
        }

        public async Task HandleMemberJoin(SocketGuildUser member)
        {
            if (!_users.ContainsKey(member.Id))
            {
                User newUser = new User();
                newUser.Username = $"{member.Username}#{member.Discriminator}";
                newUser.Aliases.Add(member.Username);
                _users.Add(member.Id, newUser);
                await FileHelper.SaveUsersAsync(_users);
            }

            Task.Factory.StartNew(async () =>
            {
                await _roleService.ProcessMemberJoin(member);
            });

            string autoSilence = " and was silenced (`AutoSilenceNewJoins` is on)";
            if (!_settings.AutoSilenceNewJoins) autoSilence = "";
            if (_users[member.Id].Silenced)
                autoSilence =
                    " and was silenced (user's `Silenced` value is true, they likely tried to bypass a silence)";
            member.Guild.GetTextChannel(_settings.ModChannel)
                .SendMessageAsync($"<@{member.Id}> ({member.Id}) joined the server{autoSilence}.");
            
            if (_settings.RaidProtectionEnabled)
            {
                Task.Factory.StartNew(async () =>
                {
                    await _raidService.ProcessMemberJoin(member);
                });
            }
        }

        private async Task HandleMessageUpdate(Cacheable<IMessage, ulong> oldMessage, SocketMessage newMessage,
            ISocketMessageChannel channel)
        {
            SocketUserMessage message = newMessage as SocketUserMessage;
            SocketCommandContext context = new SocketCommandContext(_client, message);

            Task.Factory.StartNew(() =>
            {
                if (oldMessage.HasValue)
                {
                    _pressureService.ProcessMessageUpdate(oldMessage.Value, newMessage);
                }

                _filterService.ProcessMessageUpdate(context);

            });
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            //_logger.Log(LogLevel.Debug, $"{messageParam.CleanContent}; {messageParam.EditedTimestamp}");
            if (messageParam.Type != MessageType.Default && messageParam.Type != MessageType.Reply && messageParam.Type != MessageType.ThreadStarterMessage) return;
            SocketUserMessage message = messageParam as SocketUserMessage;
            int argPos = 0;
            SocketCommandContext context = new SocketCommandContext(_client, message);

            Task.Factory.StartNew(() =>
            {
                _pressureService.ProcessMessage(context);
                _roleService.ProcessMemberMessage(context);
                _filterService.ProcessMessage(context);
            });
            
            if (DevSettings.UseDevPrefix)
            {
                _settings.Prefix = DevSettings.Prefix;
            }

            if (message.HasCharPrefix(_settings.Prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                string parsedMessage;
                bool checkCommands = true;
                if (message.Content.StartsWith("<@"+_client.CurrentUser.Id+">"))
                {
                    parsedMessage = "<mention>";
                    checkCommands = false;
                }
                else
                {
                    parsedMessage = DiscordHelper.CheckAliasesAsync(message.Content, _settings);
                }

                if (checkCommands)
                {
                    bool validCommand = false;
                    foreach (var command in _commands.Commands)
                    {
                        if (command.Name != parsedMessage.Split(" ")[0])
                        {
                            continue;
                        }

                        validCommand = true;
                        break;
                    }

                    if (!validCommand) return;
                }

                // Check for BotsAllowed attribute
                bool hasBotsAllowedAttribute = false;
                SearchResult searchResult = _commands.Search(parsedMessage);
                CommandInfo commandToExec = searchResult.Commands[0].Command;

                foreach (var attribute in commandToExec.Attributes)
                {
                    if (attribute == null) continue;
                    if (!(attribute is BotsAllowedAttribute)) continue;

                    hasBotsAllowedAttribute = true;
                    break;
                }

                if (!hasBotsAllowedAttribute && context.User.IsBot) return;

                await _commands.ExecuteAsync(context, parsedMessage, _services.BuildServiceProvider());
            }
        }

        private Task Log(LogMessage msg)
        {
            if (msg.Exception != null)
            {
                if (msg.Exception.Message == "Server missed last heartbeat")
                {
                    _logger.LogWarning("Izzy Moonbot missed a heartbeat. Rebooting...");
                    _client.StopAsync();
                    _client.LoginAsync(TokenType.Bot,
                        _discordSettings.Token);
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
