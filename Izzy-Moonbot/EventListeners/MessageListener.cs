using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;

namespace Izzy_Moonbot.EventListeners;

public class MessageListener
{
    private readonly LoggingService _logger;
    private readonly Config _config;

    public MessageListener(LoggingService logger, Config config)
    {
        _logger = logger;
        _config = config;
    }

    public void RegisterEvents(IIzzyClient client)
    {
        client.MessageUpdated += async (oldContent, newMessage, channel) => await DiscordHelper.LeakOrAwaitTask(ProcessMessageUpdate(oldContent, newMessage, channel, client));
        client.MessageDeleted += async (messageId, message, channelId, channel) => await DiscordHelper.LeakOrAwaitTask(ProcessMessageDelete(messageId, message, channelId, channel, client));
    }

    private async Task ProcessMessageUpdate(
        string? oldContent,
        IIzzyMessage newMessage,
        IIzzyMessageChannel channel,
        IIzzyClient client)
    {
        var logChannel = GetLogChannel(client);
        if (logChannel == null) return;

        var defaultGuild = client.GetGuild(DiscordHelper.DefaultGuild());
        if (defaultGuild?.GetChannel(channel.Id) is null) return;

        if (oldContent is null)
        {
            _logger.Log($"Received MessageUpdated event without an oldContent. " +
                $"This usually means the message was too old to be in Izzy's local cache. " +
                $"Skipping LogChannel post since we don't know anything Discord isn't already displaying.");
            return;
        }

        if (newMessage.Content == oldContent)
        {
            _logger.Log($"Skipping LogChannel post for MessageUpdated event because the message's .Content did not change. " +
                $"This means some other property was edited, e.g. Discord auto-unfurled a link, or the message was pinned.");
            return;
        }

        var author = newMessage.Author;
        if (author.Id == client.CurrentUser.Id) return; // Don't process self.
        if (author.IsBot) return; // Don't listen to bots

        var logMessage =
            $"Message by {author.Username}#{author.Discriminator} ({author.Id}) **edited** in {channel.Name}:\n" +
            $"__Before__:\n{oldContent}\n" +
            $"__After__:\n{newMessage.Content}";

        await logChannel.SendMessageAsync(logMessage, allowedMentions: AllowedMentions.None);
    }

    private async Task ProcessMessageDelete(
        ulong messageId,
        IIzzyMessage? message,
        ulong channelId,
        IIzzyMessageChannel? channel,
        IIzzyClient client)
    {
        var logChannel = GetLogChannel(client);
        if (logChannel == null) return;

        var defaultGuild = client.GetGuild(DiscordHelper.DefaultGuild());
        if (defaultGuild?.GetChannel(channelId) is null) return;

        if (message is null)
        {
            _logger.Log($"Received MessageDeleted event for an unknown message with id {messageId}. " +
                $"This usually means the message was too old to be in Izzy's local cache. " +
                $"Skipping LogChannel post since we don't know the author or content of the deleted message.");
            return;
        }

        var author = message.Author;
        if (author.Id == client.CurrentUser.Id) return; // Don't process self.
        if (author.IsBot) return; // Don't listen to bots

        var logMessage = $"Message by {author.Username}#{author.Discriminator} ({author.Id}) **deleted**";

        if (channel is null)
            logMessage += $" in unknown channel {channelId}:\n";
        else
            logMessage += $" in {channel.Name}:\n";

        if (message.Attachments?.Any() ?? false)
            logMessage += $"__Content__:\n{message.Content}\n" +
                $"__Attachments__:\n{string.Join('\n', message.Attachments.Select(a => a.ProxyUrl))}";
        else
            logMessage += $"{message.Content}";

        await logChannel.SendMessageAsync(logMessage, allowedMentions: AllowedMentions.None);
    }

    private IIzzySocketTextChannel? GetLogChannel(IIzzyClient client)
    {
        var defaultGuild = client.GetGuild(DiscordHelper.DefaultGuild());

        var logChannelId = _config.LogChannel;
        if (logChannelId == 0)
        {
            _logger.Log("Can't post logs because .config LogChannel hasn't been set.");
            return null;
        }
        var logChannel = defaultGuild?.GetTextChannel(logChannelId);
        if (logChannel == null)
        {
            _logger.Log("Something went wrong trying to access LogChannel.");
            return null;
        }

        return logChannel;
    }
}