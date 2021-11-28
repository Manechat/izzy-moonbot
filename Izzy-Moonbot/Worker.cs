namespace Izzy_Moonbot
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Izzy_Moonbot.Helpers;
    using Izzy_Moonbot.Settings;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceCollection _services;
        private readonly DiscordSettings _settings;
        private readonly CommandService _commands;
        private readonly AllPreloadedSettings _servers;
        private DiscordSocketClient _client;

        public Worker(ILogger<Worker> logger, IServiceCollection services, IOptions<DiscordSettings> settings, AllPreloadedSettings servers)
        {
            _logger = logger;
            _commands = new CommandService();
            _settings = settings.Value;
            _services = services;
            _servers = servers;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _client = new DiscordSocketClient();
                _client.Log += Log;
                await _client.LoginAsync(TokenType.Bot,
                    _settings.token);

                await _client.StartAsync();
                await _client.SetGameAsync("with my paintbrush");
                await InstallCommandsAsync();

                // Block this task until the program is closed.
                await Task.Delay(-1);


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
            var serverId = context.IsPrivate ? context.User.Id : context.Guild.Id;
            var settings = await FileHelper.LoadServerPresettingsAsync(context, _servers);
            if (DevSettings.useDevPrefix)
            {
                settings.prefix = DevSettings.prefix;
            }

            if (message.HasCharPrefix(settings.prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                if (!(settings.listenToBots) && message.Author.IsBot)
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
                    parsedMessage = DiscordHelper.CheckAliasesAsync(message.Content, settings);
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

                if (parsedMessage.Split(" ")[0] == "broadcast")
                {
                    if (context.User.Id == 221742476153716736) //Dr. Romulus#4444
                    {
                        var messagePart = parsedMessage.Split(' ', 2)[1];
                        await BroadcastAsync(messagePart);
                        await context.Channel.SendMessageAsync("Message broadcasted to all servers' admin channels.");
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

        [RequireOwner]
        private async Task BroadcastAsync(string message = "")
        {
            var guildList = _servers.guildList;
            foreach (var (guild, adminChannel) in guildList)
            {
                await _client.GetGuild(guild).GetTextChannel(adminChannel).SendMessageAsync(message);
            }
        }
    }
}
