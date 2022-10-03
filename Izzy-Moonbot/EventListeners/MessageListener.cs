using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;

namespace Izzy_Moonbot.EventListeners;

public class MessageListener
{
    private readonly Dictionary<ulong, User> _users;
    private readonly LoggingService _logger;

    public MessageListener(Dictionary<ulong, User> users, LoggingService logger)
    {
        _users = users;
        _logger = logger;
    }
    
    public void RegisterEvents(DiscordSocketClient client)
    {
        client.MessageReceived += (message) => Task.Run(async () => { await MessageRecieveEvent(message, client); });
    }

    private async Task MessageRecieveEvent(SocketMessage messageParam, DiscordSocketClient client)
    {
        if (!DiscordHelper.IsInGuild(messageParam)) return; // Not in guild (in dm/group)
        if (!DiscordHelper.IsProcessableMessage(messageParam)) return; // Not processable
        if (messageParam is not SocketUserMessage message) return; // Not processable
        
        SocketCommandContext context = new SocketCommandContext(client, message);

        var user = context.Guild.GetUser(context.User.Id);

        if (user == null)
        {
            await _logger.Log($"User does not exist?", context, level: LogLevel.Warning);
            return;
        }
        
        if (!_users.ContainsKey(user.Id))
        {
            var newUser = new User();
            newUser.Username = $"{user.Username}#{user.Discriminator}";
            newUser.Aliases.Add(user.Username);
            if(user.JoinedAt.HasValue) newUser.Joins.Add(user.JoinedAt.Value);
            _users.Add(user.Id, newUser);
        }
        else
        {
            if (_users[user.Id].Username == "")
            {
                _users[user.Id].Username =
                    $"{user.Username}#{user.Discriminator}";
            }

            if (!_users[user.Id].Aliases.Contains(user.DisplayName))
            {
                _users[user.Id].Aliases.Add(user.DisplayName);
            }

            if (user.JoinedAt.HasValue &&
                !_users[user.Id].Joins.Contains(user.JoinedAt.Value))
            {
                _users[user.Id].Joins.Add(user.JoinedAt.Value);
            }
        }

        await FileHelper.SaveUsersAsync(_users);
    }
}