namespace Izzy_Moonbot
{
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Izzy_Moonbot.Helpers;
    using Izzy_Moonbot.Settings;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceCollection _services;
        private readonly DiscordSettings _discordSettings;
        private readonly CommandService _commands;
        private readonly ServerSettings _settings;
        private readonly Dictionary<ulong, User> _users;
        private DiscordSocketClient _client;

        public Worker(ILogger<Worker> logger, IServiceCollection services, IOptions<DiscordSettings> discordSettings, ServerSettings settings, Dictionary<ulong, User> users)
        {
            _logger = logger;
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
                _client = new DiscordSocketClient();
                _client.Log += Log;
                await _client.LoginAsync(TokenType.Bot,
                    _discordSettings.Token);

                await _client.StartAsync();
                await _client.SetGameAsync($"Go away");
                await InstallCommandsAsync();

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

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            var argPos = 0;
            var context = new SocketCommandContext(_client, message);

            await ProcessUser(message);

            if (DevSettings.UseDevPrefix)
            {
                _settings.Prefix = DevSettings.Prefix;
            }

            if (message.HasCharPrefix(_settings.Prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                if (!(_settings.ListenToBots) && message.Author.IsBot)
                {
                    return;
                }

                string parsedMessage;
                var checkCommands = true;
                if (message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                {
                    parsedMessage = "<mention>";
                    checkCommands = false;
                }
                else
                {
                    parsedMessage = DiscordHelper.CheckAliasesAsync(message.Content, _settings);
                }

                if (parsedMessage == "")
                {
                    parsedMessage = "<blank message>";
                    checkCommands = false;
                }

                if (checkCommands)
                {
                    var validCommand = false;
                    foreach (var command in _commands.Commands)
                    {
                        if (command.Name != parsedMessage.Split(" ")[0])
                        {
                            continue;
                        }

                        validCommand = true;
                        break;
                    }

                    if (!validCommand)
                    {
                        parsedMessage = "<invalid command>";
                    }
                }

                await _commands.ExecuteAsync(context, parsedMessage, _services.BuildServiceProvider());
            }
        }

        private Task Log(LogMessage msg)
        {
            _logger.LogInformation(msg.Message);
            return Task.CompletedTask;
        }
        private double ProcessPressure(ulong id, SocketUserMessage message)
        {
            var now = DateTime.UtcNow;
            var basePressure = 10.0;
            var pressure = GetCurrentPressure(id);
            pressure += basePressure;
            _users[id].Pressure = pressure;
            _users[id].Timestamp = now;
            return pressure;
        }

        private double GetCurrentPressure(ulong id)
        {
            var now = DateTime.UtcNow;
            var basePressure = 10.0;
            var pressureDecay = 2.5;
            var pressureLossPerSecond = basePressure / pressureDecay;
            var pressure = _users[id].Pressure;
            var difference = now - _users[id].Timestamp;
            var totalSeconds = difference.TotalSeconds;
            var totalPressureLoss = totalSeconds * pressureLossPerSecond;
            pressure -= totalPressureLoss;
            if (pressure < 0)
            {
                pressure = 0;
            }

            return pressure;
        }

        private async Task ProcessUser(SocketUserMessage message)
        {
            var guildUser = message.Author as SocketGuildUser;
            var id = guildUser.Id;
            if (!_users.ContainsKey(id))
            {
                _users.Add(id, new User());
            }

            _users[id].Username = $"{guildUser.Username}#{guildUser.Discriminator}";
            if (!_users[id].Aliases.Contains(guildUser.Username))
            {
                _users[id].Aliases.Add(guildUser.Username);
            }
            if (guildUser.Nickname != null)
            {

                if (!_users[id].Aliases.Contains(guildUser.Nickname))
                {
                    _users[id].Aliases.Add(guildUser.Nickname);
                }
            }
            ProcessPressure(id, message);
            await FileHelper.SaveUsersAsync(_users);
        }
    }
}
