using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using HtmlAgilityPack;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Configuration;

namespace Izzy_Moonbot.Helpers;

public static class DiscordHelper
{
    public static bool IsDev(ulong user)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var section = config.GetSection(nameof(DiscordSettings));
        var settings = section.Get<DiscordSettings>();

        return settings.DevUsers.Any(userId => userId == user);
    }
    
    public static bool IsProcessableMessage(SocketMessage msg)
    {
        if (msg.Type != MessageType.Default && msg.Type != MessageType.Reply &&
            msg.Type != MessageType.ThreadStarterMessage) return false;
        return true;
    }

    public static bool IsSpace(char character)
    {
        return character is ' ' or '\t' or '\r';
    }

    public static object GetSafely<T>(IEnumerable<T> array, int index)
    {
        if (array.Count() <= index) return null;
        if (index < 0) return null;

        return array.ElementAt(index);
    }

    public static string[] GetArguments(string content)
    {
        var characters = content.Split("").Select(character =>
        {
            try
            {
                return char.Parse(character);
            }
            catch (FormatException)
            {
                return '\r';
            }
        }).ToArray();
        
        var arguments = new List<string>();

        for (var i = 0; i < content.Length; i++)
        {
            var argument = characters[i];
            if (!IsSpace(argument))
            {
                var start = 0;
                var end = 0;

                if (argument == '"' && (i < 1 || (char?)GetSafely(characters, i-1) != '\\'))
                {
                    i++;
                    start = i;
                    while (i < content.Length && (characters[i] != '"' || (char?)GetSafely(characters, i-1) == '\\'))
                    {
                        i++;
                    }
                    if ((char?)GetSafely(characters, i - 1) == '\\')
                    {
                        end = i - 1;
                    }
                    else
                    {
                        end = i;
                    }
                }
                else
                {
                    start = i;
                    i++;
                    while (i < content.Length && !IsSpace(argument) &&
                           (characters[i] != '"' || (char?)GetSafely(characters, i - 1) == '\\'))
                    {
                        i++;
                    }
                    end = i;
                }
                arguments.Add(string.Join("", content.Split("")[new Range(start, end)]));
            }
        }

        return arguments.ToArray();
    }

    public static bool IsInGuild(SocketMessage msg)
    {
        if (msg.Channel.GetChannelType() == ChannelType.DM ||
            msg.Channel.GetChannelType() == ChannelType.Group) return false;
        return true;
    }

    public static bool WouldUrlEmbed(string url)
    {
        // Construct list of known embeddable file types
        var embeddableFileTypes = new[]
        {
            "\\.jpeg$", "\\.jpg$", "\\.gif$", "\\.png$", "\\.webp$", 
            "\\.webm$", "\\.mkv$", "\\.flv$", "\\.ogg$", "\\.mov$", 
            "\\.wmv$", "\\.mp4$", "\\.m4p$", "\\.m4v$", "\\.mpg$", 
            "\\.mp2$", "\\.mpeg$", "\\.mpe$", "\\.mpv$", "\\.m4v$"
        };
        var fileTypeRegex = new Regex(string.Join("|", embeddableFileTypes), RegexOptions.IgnoreCase);
        if (fileTypeRegex.IsMatch(url)) return true; // ends with a known embeddable file type, so always is true

        if (url.Contains("twitter.com")) return true; // Twitter always embeds but does so in a really annoying way, so just assume always embed
        
        var web = new HtmlWeb();
        var doc = web.Load(url);
                
        var metaNodes = doc.DocumentNode.SelectNodes("//meta");
        
        return metaNodes.Any(node =>
        {
            var propertiesToWatch = new[]
            {
                "og:title",
                "og:description",
                "description",
                "og:image"
            };
                
            if(node.Attributes["property"] != null) return propertiesToWatch.Contains(node.Attributes["property"].Value);
            return false;
        });
    }
    
    public static async Task<ulong> GetChannelIdIfAccessAsync(string channelName, SocketCommandContext context)
    {
        var id = ConvertChannelPingToId(channelName);
        if (id > 0) return await CheckIfChannelExistsAsync(id, context);

        return await CheckIfChannelExistsAsync(channelName, context);
    }

    public static ulong GetRoleIdIfAccessAsync(string roleName, SocketCommandContext context)
    {
        var id = ConvertRolePingToId(roleName);
        return id > 0 ? CheckIfRoleExistsAsync(id, context) : CheckIfRoleExistsAsync(roleName, context);
    }

    public static async Task<ulong> GetUserIdFromPingOrIfOnlySearchResultAsync(string userName,
        SocketCommandContext context)
    {
        var userId = ConvertUserPingToId(userName);
        if (userId > 0) return userId;

        var userList = await context.Guild.SearchUsersAsync(userName);
        return userList.Count < 1 ? 0 : userList.First().Id;
    }

    public static string CheckAliasesAsync(string message, Config config)
    {
        // TODO: Remove this
        var parsedMessage = message[1..].TrimStart();
        return parsedMessage;
    }

    private static async Task<ulong> CheckIfChannelExistsAsync(string channelName, SocketCommandContext context)
    {
        var izzyMoonbot = await context.Channel.GetUserAsync(context.Client.CurrentUser.Id);
        if (context.IsPrivate) return 0;

        foreach (var channel in context.Guild.TextChannels)
            if (channel.Name == channelName && channel.Users.Contains(izzyMoonbot))
                return channel.Id;

        return 0;
    }

    private static async Task<ulong> CheckIfChannelExistsAsync(ulong channelId, SocketCommandContext context)
    {
        var izzyMoonbot = await context.Channel.GetUserAsync(context.Client.CurrentUser.Id);
        if (context.IsPrivate) return 0;

        foreach (var channel in context.Guild.TextChannels)
            if (channel.Id == channelId && channel.Users.Contains(izzyMoonbot))
                return channel.Id;

        return 0;
    }

    private static ulong ConvertChannelPingToId(string channelPing)
    {
        if (!channelPing.Contains("<#") || !channelPing.Contains(">"))
        {
            if (ulong.TryParse(channelPing, out var result)) return result;
            return 0;
        }

        var frontTrim = channelPing[2..];
        var trim = frontTrim.Split('>', 2)[0];
        return ulong.Parse(trim);
    }

    private static ulong ConvertUserPingToId(string userPing)
    {
        if (!userPing.Contains("<@") || !userPing.Contains(">"))
        {
            if (ulong.TryParse(userPing, out var result)) return result;
            return 0;
        }

        var frontTrim = userPing[2..];

        // Discord is sometimes weird and gives us a mention like <@ID> or <@!ID> seemingly randomly???
        if (userPing.Contains("!")) frontTrim = userPing[3..];

        var trim = frontTrim.Split('>', 2)[0];
        return ulong.Parse(trim);
    }

    private static ulong CheckIfRoleExistsAsync(string roleName, SocketCommandContext context)
    {
        if (context.IsPrivate) return 0;

        foreach (var role in context.Guild.Roles)
            if (role.Name == roleName)
                return role.Id;

        return 0;
    }

    private static ulong CheckIfRoleExistsAsync(ulong roleId, SocketCommandContext context)
    {
        if (context.IsPrivate) return 0;

        foreach (var role in context.Guild.Roles)
            if (role.Id == roleId)
                return role.Id;

        return 0;
    }

    private static ulong ConvertRolePingToId(string rolePing)
    {
        if (!rolePing.Contains("<@&") || !rolePing.Contains(">"))
        {
            if (ulong.TryParse(rolePing, out var result)) return result;
            return 0;
        }

        var frontTrim = rolePing[3..];
        var trim = frontTrim.Split('>', 2)[0];
        return ulong.Parse(trim);
    }
}