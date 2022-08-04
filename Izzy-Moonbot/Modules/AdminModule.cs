using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Describers;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Modules;

[Summary("Module for managing admin functions")]
public class AdminModule : ModuleBase<SocketCommandContext>
{
    private readonly LoggingService _logger;
    private readonly ServerSettings _settings;
    private readonly Dictionary<ulong, User> _users;

    private DateTimeOffset _lastMentionResponse = DateTimeOffset.MinValue;

    public AdminModule(LoggingService logger, ServerSettings settings, Dictionary<ulong, User> users)
    {
        _logger = logger;
        _settings = settings;
        _users = users;
    }

    [Command("panic")]
    [Summary("Immediately disconnects the client.")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task PanicCommand()
    {
        // Just closes the connection.
        await ReplyAsync("<a:izzywhat:891381404741550130>");
        Environment.Exit(255);
    }

    [Command("scan")]
    [Summary("Refresh the stored userlist")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task ScanCommandAsync(
        [Summary("The type of scan to do")] [Remainder]
        string scanType = ""
    )
    {
        if (scanType.ToLower() == "full")
            Task.Factory.StartNew(async () =>
            {
                if (!Context.Guild.HasAllMembers) await Context.Guild.DownloadUsersAsync();

                var newUserCount = 0;
                var reloadUserCount = 0;
                var knownUserCount = 0;

                await foreach (var socketGuildUser in Context.Guild.Users.ToAsyncEnumerable())
                    if (!_users.ContainsKey(socketGuildUser.Id))
                    {
                        var newUser = new User();
                        newUser.Username = $"{socketGuildUser.Username}#{socketGuildUser.Discriminator}";
                        newUser.Aliases.Add(socketGuildUser.Username);
                        _users.Add(socketGuildUser.Id, newUser);
                        newUserCount += 1;
                    }
                    else
                    {
                        if (_users[socketGuildUser.Id].Username == "")
                        {
                            _users[socketGuildUser.Id].Username =
                                $"{socketGuildUser.Username}#{socketGuildUser.Discriminator}";
                            reloadUserCount += 1;
                        }
                        else if (!_users[socketGuildUser.Id].Aliases.Contains(socketGuildUser.DisplayName))
                        {
                            _users[socketGuildUser.Id].Aliases.Add(socketGuildUser.DisplayName);
                            reloadUserCount += 1;
                        }
                        else
                        {
                            knownUserCount += 1;
                        }
                    }

                await FileHelper.SaveUsersAsync(_users);

                await Context.Message.ReplyAsync(
                    $"Done! I discovered {Context.Guild.Users.Count} members, of which{Environment.NewLine}" +
                    $"{newUserCount} were unknown to me until now,{Environment.NewLine}" +
                    $"{reloadUserCount} had out of date Username and Alias information,{Environment.NewLine}" +
                    $"and {knownUserCount} didn't need to be updated.");
            });
    }

    [Command("echo")]
    [Summary("Posts a message to a specified channel")]
    [ModCommand(Group = "Permission")]
    [DevCommand(Group = "Permission")]
    public async Task EchoCommandAsync([Summary("The channel to send to")] string channelName = "",
        [Remainder] [Summary("The message to send")] string message = "")
    {
        if (channelName == "")
        {
            await ReplyAsync("You must specify a channel name or a message.");
            return;
        }

        var channelId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);

        if (channelId > 0)
        {
            var channel = Context.Guild.GetTextChannel(channelId);
            if (message == "")
            {
                await ReplyAsync("There's no message to send there.");
                return;
            }

            if (channel != null)
            {
                await channel.SendMessageAsync(message);
                return;
            }


            await ReplyAsync("I can't send a message there.");
            return;
        }

        await ReplyAsync($"{channelName} {message}");
    }

    [Command("getsettings")]
    [Summary("Posts the settings file to the cui channel")]
    public async Task GetSettingsCommandAsync()
    {
        if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, _settings)) return;

        if (Context.IsPrivate)
        {
            await ReplyAsync("Cannot get settings in a DM.");
            return;
        }

        var errorMessage = await SettingsGetAsync(Context, _settings);
        if (errorMessage.Contains("<ERROR>"))
        {
            await ReplyAsync(errorMessage);
            await _logger.Log($"getsettings: {errorMessage} <FAIL>", Context);
            return;
        }

        await _logger.Log("getsettings: <SUCCESS>", Context);
    }

    [Command("getusers")]
    [RequireOwner]
    public async Task GetUsersCommandAsync()
    {
        if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, _settings)) return;

        if (Context.IsPrivate)
        {
            await ReplyAsync("Cannot get settings in a DM.");
            return;
        }

        var errorMessage = await UsersGetAsync(Context, _settings);
        if (errorMessage.Contains("<ERROR>"))
        {
            await ReplyAsync(errorMessage);
            await _logger.Log($"getsettings: {errorMessage} <FAIL>", Context);
            return;
        }

        await _logger.Log("getsettings: <SUCCESS>", Context);
    }

    [Command("gethistory")]
    [RequireOwner]
    public async Task GetChannelHistoryCommandAsync(string channel, ulong messageId)
    {
        if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, _settings)) return;

        if (Context.IsPrivate)
        {
            await ReplyAsync("Cannot get settings in a DM.");
            return;
        }

        var errorMessage = await ChannelHistoryGetAsync(Context, _settings, channel, messageId);
        if (errorMessage.Contains("<ERROR>"))
        {
            await ReplyAsync(errorMessage);
            await _logger.Log($"getsettings: {errorMessage} <FAIL>", Context);
            return;
        }

        await _logger.Log("getsettings: <SUCCESS>", Context);
    }

    [Command("<mention>")]
    [Summary("Runs when someone mentions Izzy")]
    public async Task MentionCommandAsync()
    {
        if (!_settings.MentionResponseEnabled) return; // Responding to mention is disabled.
        _logger.Log((DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()), Context);
        _logger.Log(_lastMentionResponse.ToUnixTimeSeconds().ToString(), Context);
        _logger.Log((DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _lastMentionResponse.ToUnixTimeSeconds()).ToString(), Context);
        if ((DateTimeOffset.UtcNow - _lastMentionResponse).TotalMinutes < _settings.MentionResponseCooldown)
            return; // Still on cooldown.

        var random = new Random();
        var index = random.Next(_settings.MentionResponses.Count);
        var response = _settings.MentionResponses[index];

        _lastMentionResponse = DateTimeOffset.UtcNow;

        await ReplyAsync($"{response}");
    }

    private async Task<string> SettingsGetAsync(SocketCommandContext context, ServerSettings settings)
    {
        await ReplyAsync("Retrieving settings file...");
        var filepath = FileHelper.SetUpFilepath(FilePathType.Root, "settings", "conf", Context);
        if (!File.Exists(filepath)) return "<ERROR> File does not exist";

        await context.Channel.SendFileAsync(filepath, "settings.conf");
        return "SUCCESS";
    }

    private async Task<string> UsersGetAsync(SocketCommandContext context, ServerSettings settings)
    {
        await ReplyAsync("Writing user list to file...");
        var filepath = FileHelper.SetUpFilepath(FilePathType.Root, "users", "conf", Context);
        if (!File.Exists(filepath)) File.Create(filepath);

        var userlist = new List<string>();
        await context.Guild.DownloadUsersAsync();
        var users = context.Guild.Users;
        foreach (var user in users) userlist.Add($"<@{user.Id}>, {user.Username}#{user.Discriminator}");
        await File.WriteAllLinesAsync(filepath, userlist);

        await context.Channel.SendFileAsync(filepath, $"{context.Guild.Name}-users.conf");
        return "SUCCESS";
    }

    private async Task<string> ChannelHistoryGetAsync(SocketCommandContext context, ServerSettings settings,
        string channelString, ulong messageId)
    {
        await ReplyAsync("Writing channel history to file...");
        var channelId = await DiscordHelper.GetChannelIdIfAccessAsync(channelString, context);
        if (channelId == 0) return "<ERROR> No channel access";

        var channel = context.Guild.GetTextChannel(channelId);
        var filepath = FileHelper.SetUpFilepath(FilePathType.Channel, channelString, "log", Context);
        var id = messageId;
        var messageList = new List<string>();
        var limit = 100;
        var initialMessage = await channel.GetMessageAsync(id);
        messageList.Add(
            $"{initialMessage.Author.Username}#{initialMessage.Author.Discriminator} ({initialMessage.Author.Id}) at {initialMessage.CreatedAt.UtcDateTime.ToShortDateString()} {initialMessage.CreatedAt.UtcDateTime.ToShortTimeString()}: {initialMessage.Content}");
        await File.WriteAllLinesAsync(filepath, messageList);
        messageList.Clear();

        var messages = await channel.GetMessagesAsync(id, Direction.After, limit).FlattenAsync();
        var list = messages.ToList();
        list.Reverse();
        var done = false;

        while (!done)
        {
            if (list.Count < limit) done = true;

            foreach (var message in list)
                messageList.Add(
                    $"{message.Author.Username}#{message.Author.Discriminator} ({message.Author.Id}) at {message.CreatedAt.UtcDateTime.ToShortDateString()} {message.CreatedAt.UtcDateTime.ToShortTimeString()}: {message.Content}");
            await File.AppendAllLinesAsync(filepath, messageList);
            messageList.Clear();
            if (list.Count > 0)
            {
                id = list[^1].Id;
                messages = await channel.GetMessagesAsync(id, Direction.After, limit).FlattenAsync();
                list = messages.ToList();
                list.Reverse();
            }
        }

        await context.Channel.SendFileAsync(filepath, $"{context.Guild.Name}-{channelString}.log");
        await ReplyAsync("Success!");
        return "SUCCESS";
    }

    [Summary("Submodule for retreiving log files")]
    public class LogModule : ModuleBase<SocketCommandContext>
    {
        private readonly LoggingService _logger;
        private readonly ServerSettings _settings;

        public LogModule(LoggingService logger, ServerSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        [Command("log")]
        [Summary("Retrieves a log file")]
        public async Task LogCommandAsync(
            [Summary("The channel to get the log from")]
            string channel = "",
            [Summary("The date (in format (YYYY-MM-DD) to get the log from")]
            string date = "")
        {
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, _settings)) return;

            if (Context.IsPrivate)
            {
                await ReplyAsync("Cannot get logs in a DM.");
                return;
            }

            if (channel == "")
            {
                await ReplyAsync("You need to enter a channel and date.");
                await _logger.Log("log: <FAIL>", Context);
                return;
            }

            if (date == "")
            {
                await ReplyAsync("You need to enter a date.");
                await _logger.Log($"log: {channel} <FAIL>", Context);
                return;
            }

            var errorMessage = await LogGetAsync(channel, date, Context);
            if (errorMessage.Contains("<ERROR>"))
            {
                await ReplyAsync(errorMessage);
                await _logger.Log($"log: {channel} {date} {errorMessage} <FAIL>", Context);
                return;
            }

            await _logger.Log($"log: {channel} {date} <SUCCESS>", Context);
        }

        private async Task<string> LogGetAsync(string channelName, string date, SocketCommandContext context)
        {
            await ReplyAsync($"Retrieving log from {channelName} on {date}...");
            var confirmedName = DiscordHelper.ConvertChannelPingToName(channelName, context);
            if (confirmedName.Contains("<ERROR>")) return confirmedName;

            if (_settings.LogChannel <= 0) return "<ERROR> Log post channel not set.";

            var filepath =
                FileHelper.SetUpFilepath(FilePathType.LogRetrieval, date, "log", context, confirmedName, date);
            if (!File.Exists(filepath)) return "<ERROR> File does not exist";

            var logPostChannel = context.Guild.GetTextChannel(_settings.LogChannel);
            await logPostChannel.SendFileAsync(filepath, $"{confirmedName}-{date}.log");
            return "SUCCESS";
        }
    }
}