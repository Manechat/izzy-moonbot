using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Modules;

[Summary("Administration / Moderation related commands.")]
public class AdminModule : ModuleBase<SocketCommandContext>
{
    private readonly LoggingService _logger;
    private readonly Config _config;
    private readonly State _state;
    private readonly Dictionary<ulong, User> _users;

    public AdminModule(LoggingService logger, Config config, Dictionary<ulong, User> users,
        State state)
    {
        _logger = logger;
        _config = config;
        _state = state;
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
            Task.Run(async () =>
            {
                if (!Context.Guild.HasAllMembers) await Context.Guild.DownloadUsersAsync();

                var newUserCount = 0;
                var reloadUserCount = 0;
                var knownUserCount = 0;

                await foreach (var socketGuildUser in Context.Guild.Users.ToAsyncEnumerable())
                {
                    var skip = false;
                    if (!_users.ContainsKey(socketGuildUser.Id))
                    {
                        var newUser = new User();
                        newUser.Username = $"{socketGuildUser.Username}#{socketGuildUser.Discriminator}";
                        newUser.Aliases.Add(socketGuildUser.Username);
                        if (socketGuildUser.JoinedAt.HasValue) newUser.Joins.Add(socketGuildUser.JoinedAt.Value);
                        _users.Add(socketGuildUser.Id, newUser);
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

                await Context.Message.ReplyAsync(
                    $"Done! I discovered {Context.Guild.Users.Count} members, of which{Environment.NewLine}" +
                    $"{newUserCount} were unknown to me until now,{Environment.NewLine}" +
                    $"{reloadUserCount} had out of date information,{Environment.NewLine}" +
                    $"and {knownUserCount} didn't need to be updated.");
            });
    }

    [Command("echo")]
    [Summary("Posts a message to a specified channel")]
    [ModCommand(Group = "Permission")]
    [DevCommand(Group = "Permission")]
    public async Task EchoCommandAsync([Summary("The channel to send to")] string channelName = "",
        [Remainder] [Summary("The message to send")]
        string message = "")
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

    [Command("userinfo")]
    [Summary("Get information about a user")]
    [ModCommand]
    [DevCommand]
    public async Task UserInfoCommandAsync(
        [Remainder][Summary("The user to get information about")] string user = "")
    {
        if (user == "") user = Context.User.Id.ToString(); // Set to user ID to target self.
        
        var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(user, Context);
        var member = Context.Guild.GetUser(userId);

        var output = $"```{Environment.NewLine}";
        output += $"User: <@{member.Id}> (`{member.Id}`){Environment.NewLine}";
        output += $"Names: {string.Join(", ", _users[member.Id].Aliases)}{Environment.NewLine}";
        output += $"Roles: {string.Join(", ", member.Roles.Select(role => role.Name))}{Environment.NewLine}";
        output += $"History: ";
        output += $"Created <t:{member.CreatedAt.ToUnixTimeSeconds()}:F>";
        if (member.JoinedAt.HasValue)
        {
            output +=
                $", joined <t:{member.JoinedAt.Value.ToUnixTimeSeconds()}:F>";
        }

        output += $", last seen <t:{_users[member.Id].Timestamp.ToUnixTimeSeconds()}:R>{Environment.NewLine}";
        output += $"Avatar(s): {Environment.NewLine}";
        output += $"    Guild: {member.GetGuildAvatarUrl() ?? "No guild avatar found."}";
        output += $"    Global: {member.GetAvatarUrl() ?? "No global avatar found."}";

        await ReplyAsync(output);
    }

    [Command("<mention>")]
    [Summary("Runs when someone mentions Izzy")]
    public async Task MentionCommandAsync()
    {
        if (!_config.MentionResponseEnabled) return; // Responding to mention is disabled.
        if ((DateTimeOffset.UtcNow - _state.LastMentionResponse).TotalMinutes < _config.MentionResponseCooldown)
            return; // Still on cooldown.

        var random = new Random();
        var index = random.Next(_config.MentionResponses.Count);
        var response = _config.MentionResponses[index]; // Random response

        _state.LastMentionResponse = DateTimeOffset.UtcNow;

        await ReplyAsync($"{response}");
    }

    [Command("stowaways")]
    [Summary("List users who do not have the member role.")]
    [RequireContext(ContextType.Guild)]
    [ModCommand(Group = "Permissions")]
    [DevCommand(Group = "Permissions")]
    public async Task StowawaysCommandAsync()
    {
        if (_config.MemberRole == null)
        {
            await ReplyAsync(
                "I'm unable to detect stowaways because the `MemberRole` config value is set to nothing.");
            return;
        }
            
        await Task.Run(async () =>
        {
            if (!Context.Guild.HasAllMembers) await Context.Guild.DownloadUsersAsync();

            var stowawayList = new HashSet<SocketGuildUser>();
            
            await foreach (var socketGuildUser in Context.Guild.Users.ToAsyncEnumerable())
            {
                if (socketGuildUser.IsBot) continue; // Bots aren't stowaways

                if (!socketGuildUser.Roles.Select(role => role.Id).Contains((ulong)_config.MemberRole))
                {
                    // Doesn't have member role, add to stowaway list.
                    stowawayList.Add(socketGuildUser);
                }
            }

            if (stowawayList.Count == 0)
            {
                // There's no stowaways
                await ReplyAsync("I didn't find any stowaways.");
            }
            else
            {
                var stowawayStringList = stowawayList.Select(user => $"<@{user.Id}>");

                await ReplyAsync(
                    $"I found these following stowaways:{Environment.NewLine}{string.Join(", ", stowawayStringList)}");
            }
        });
    }
}