using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Izzy_Moonbot.Attributes;
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
        private readonly PressureService _pressureService;
        private readonly RaidService _raidService;
        private readonly ScheduleService _scheduleService;
        private readonly IServiceCollection _services;
        private readonly Config _config;
        private readonly Dictionary<ulong, User> _users;
        private DiscordSocketClient _client;
        public bool hasProgrammingSocks = false;
        public int LaserCount = 10;

        public Worker(ILogger<Worker> logger, ModLoggingService modLog, IServiceCollection services,
            PressureService pressureService, ModService modService, RaidService raidService,
            FilterService filterService, ScheduleService scheduleService, IOptions<DiscordSettings> discordSettings,
            Config config, Dictionary<ulong, User> users)
        {
            _logger = logger;
            _modLog = modLog;
            _pressureService = pressureService;
            _modService = modService;
            _raidService = raidService;
            _filterService = filterService;
            _scheduleService = scheduleService;
            _commands = new CommandService();
            _discordSettings = discordSettings.Value;
            _services = services;
            _config = config;
            _users = users;
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
                //await _client.SetGameAsync($"Go away");
                await _client.SetGameAsync($"MVP System Test - SafeMode Active");
                await InstallCommandsAsync();

                _client.UserJoined += HandleMemberJoin;
                _client.UserLeft += HandleUserLeave;

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
            _scheduleService.ResumeScheduledTasks(_client.Guilds.ToArray()[0]);
            
            _pressureService.RegisterEvents(_client);
            _raidService.RegisterEvents(_client);
            _filterService.RegisterEvents(_client);
        }

        public async Task HandleMemberJoin(SocketGuildUser member)
        {
            if (!_users.ContainsKey(member.Id))
            {
                User newUser = new User();
                newUser.Username = $"{member.Username}#{member.Discriminator}";
                newUser.Aliases.Add(member.Username);
                newUser.Joins.Add(member.JoinedAt.Value);
                _users.Add(member.Id, newUser);
                await FileHelper.SaveUsersAsync(_users);
            }
            else
            {
                _users[member.Id].Joins.Add(member.JoinedAt.Value);
                await FileHelper.SaveUsersAsync(_users);
            }

            Task.Factory.StartNew(async () =>
            {
                List<ulong> roles = new List<ulong>();
                string expiresString = "";

                _logger.Log(LogLevel.Information,
                    $"{member.Username}#{member.DiscriminatorValue} ({member.Id}) Joined. Processing roles...");
                if (_config.MemberRole != null && (!_config.AutoSilenceNewJoins || !_users[member.Id].Silenced))
                {
                    roles.Add((ulong)_config.MemberRole);
                }

                if (_config.NewMemberRole != null && (!_config.AutoSilenceNewJoins || !_users[member.Id].Silenced))
                {
                    roles.Add((ulong)_config.NewMemberRole);
                    expiresString =
                        $"{Environment.NewLine}New Member role expires in <t:{(DateTimeOffset.UtcNow + TimeSpan.FromMinutes(_config.NewMemberRoleDecay)).ToUnixTimeSeconds()}:R>";

                    Dictionary<string, string> fields = new Dictionary<string, string>
                    {
                        { "roleId", _config.NewMemberRole.ToString() },
                        { "userId", member.Id.ToString() },
                        {
                            "reason",
                            $"{_config.NewMemberRoleDecay} minutes (`NewMemberRoleDecay`) passed, user no longer a new pony."
                        }
                    };
                    ScheduledTaskAction action = new ScheduledTaskAction(ScheduledTaskActionType.RemoveRole, fields);
                    ScheduledTask task = new ScheduledTask(DateTimeOffset.UtcNow,
                        (DateTimeOffset.UtcNow + TimeSpan.FromMinutes(_config.NewMemberRoleDecay)), action);
                    _scheduleService.CreateScheduledTask(task, member.Guild);
                }

                if (roles.Count == 0) return; // What's the point of logging if Izzy doesn't do anything??
                string autoSilence = $" (User autosilenced, `AutoSilenceNewJoins` is true.)";
                if (!_config.AutoSilenceNewJoins) autoSilence = "";
                if (_users[member.Id].Silenced)
                    autoSilence =
                        ", silenced (attempted silence bypass)";

                await _modService.AddRoles(member, roles, DateTimeOffset.Now,
                    $"New user join{autoSilence}.{expiresString}");
            });

            string autoSilence = ", silenced (`AutoSilenceNewJoins` is on)";
            if (!_config.AutoSilenceNewJoins) autoSilence = "";
            if (_users[member.Id].Silenced)
                autoSilence =
                    ", silenced (attempted silence bypass)";
            string joinedBefore = $", Joined {_users[member.Id].Joins.Count - 1} times before";
            if (_users[member.Id].Joins.Count <= 1) joinedBefore = "";
            await _modLog.CreateModLog(member.Guild)
                .SetContent(
                    $"Join: <@{member.Id}> (`{member.Id}`), created <t:{member.CreatedAt.ToUnixTimeSeconds()}:R>{autoSilence}{joinedBefore}")
                .Send();
        }

        private async Task HandleUserLeave(SocketGuild guild, SocketUser user)
        {
            var lastNickname = _users[user.Id].Aliases.Last();
            var wasKicked = guild.GetAuditLogsAsync(2, actionType: ActionType.Kick).FirstAsync()
                .GetAwaiter().GetResult()
                .Select(audit =>
                {
                    var data = audit.Data as KickAuditLogData;
                    if (data.Target.Id == user.Id)
                    {
                        _logger.Log(LogLevel.Trace, $"Target: {data.Target.Username}#{data.Target.Discriminator}");
                        _logger.Log(LogLevel.Trace, $"Moderator: {audit.User.Username}#{audit.User.Discriminator}");
                        _logger.Log(LogLevel.Trace, $"Action: {audit.Action.ToString()}");
                        _logger.Log(LogLevel.Trace, $"Reason: {audit.Reason}");

                        if ((audit.CreatedAt.ToUnixTimeSeconds() - DateTimeOffset.UtcNow.ToUnixTimeSeconds()) <= 2)
                            return audit;
                    }

                    return null;
                });

            var wasBanned = guild.GetAuditLogsAsync(2, actionType: ActionType.Ban).FirstAsync()
                .GetAwaiter().GetResult()
                .Select(audit =>
                {
                    var data = audit.Data as BanAuditLogData;
                    if (data.Target.Id == user.Id)
                    {
                        _logger.Log(LogLevel.Trace, $"Target: {data.Target.Username}#{data.Target.Discriminator}");
                        _logger.Log(LogLevel.Trace, $"Moderator: {audit.User.Username}#{audit.User.Discriminator}");
                        _logger.Log(LogLevel.Trace, $"Action: {audit.Action.ToString()}");
                        _logger.Log(LogLevel.Trace, $"Reason: {audit.Reason}");

                        if ((audit.CreatedAt.ToUnixTimeSeconds() - DateTimeOffset.UtcNow.ToUnixTimeSeconds()) <= 2)
                            return audit;
                    }

                    return null;
                });

            var output =
                $"Leave: {user.Username}#{user.Discriminator} ({lastNickname}) (`{user.Id}`) joined <t:{_users[user.Id].Joins.Last().ToUnixTimeSeconds()}:R>";

            var banAuditLogEntries = wasBanned as RestAuditLogEntry[] ?? wasBanned.ToArray();
            if (banAuditLogEntries.Any(audit => audit != null))
            {
                var audit = banAuditLogEntries.First();
                output =
                    $"Leave (Ban): {user.Username}#{user.Discriminator} ({lastNickname}) (`{user.Id}`) joined <t:{_users[user.Id].Joins.Last().ToUnixTimeSeconds()}:R>, \"{audit.Reason}\" by {audit.User.Username}#{audit.User.Discriminator} ({guild.GetUser(audit.User.Id).DisplayName})";
            }

            var kickAuditLogEntries = wasKicked as RestAuditLogEntry[] ?? wasKicked.ToArray();
            if (kickAuditLogEntries.Any(audit => audit != null))
            {
                var audit = kickAuditLogEntries.First();
                output =
                    $"Leave (Kick): {user.Username}#{user.Discriminator} ({lastNickname}) (`{user.Id}`) joined <t:{_users[user.Id].Joins.Last().ToUnixTimeSeconds()}:R>, \"{audit.Reason}\" by {audit.User.Username}#{audit.User.Discriminator} ({guild.GetUser(audit.User.Id).DisplayName})";
            }

            var scheduledTasks = _scheduleService.GetScheduledTasks().ToList().Select(action =>
            {
                return action.Action.Fields.ContainsKey("userId") &&
                       action.Action.Fields["userId"] == user.Id.ToString()
                    ? action
                    : null;
            });

            foreach (var scheduledTask in scheduledTasks)
            {
                if (scheduledTask == null) continue;
                await _scheduleService.DeleteScheduledTask(scheduledTask);
            }

            await _modLog.CreateModLog(guild)
                .SetContent(output)
                .Send();
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            //_logger.Log(LogLevel.Debug, $"{messageParam.CleanContent}; {messageParam.EditedTimestamp}");
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
                string parsedMessage = null;
                bool checkCommands = true;
                if (message.Content.StartsWith($"<@{_client.CurrentUser.Id}>"))
                {
                    checkCommands = false;
                    parsedMessage = "<mention>";
                }
                else
                {
                    parsedMessage = DiscordHelper.CheckAliasesAsync(message.Content, _config);
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