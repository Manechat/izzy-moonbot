using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Configuration;

namespace Izzy_Moonbot.Helpers;

public static class DiscordHelper
{
    public static bool ShouldExecuteInPrivate(bool externalUsageAllowedFlag, SocketCommandContext context)
    {
        var settings = GetDiscordSettings();

        if (context.IsPrivate || context.Guild.Id != settings.DefaultGuild)
        {
            return externalUsageAllowedFlag;
        }
        
        return true;
    }

    public static bool IsDefaultGuild(SocketCommandContext context)
    {
        var settings = GetDiscordSettings();

        if (context.IsPrivate) return false;
        
        return context.Guild.Id == settings.DefaultGuild;
    }
    
    public static ulong DefaultGuild()
    {
        var settings = GetDiscordSettings();

        return settings.DefaultGuild;
    }
    
    public static bool IsDev(ulong user)
    {
        var settings = GetDiscordSettings();
        
        return settings.DevUsers.Any(userId => userId == user);
    }

    public static DiscordSettings GetDiscordSettings()
    {
        var config = new ConfigurationBuilder()
            #if DEBUG
            .AddJsonFile("appsettings.Development.json")
            #else
            .AddJsonFile("appsettings.json")
            #endif
            .Build();

        var section = config.GetSection(nameof(DiscordSettings));
        var settings = section.Get<DiscordSettings>();
        
        if (settings == null) throw new NullReferenceException("Discord settings is null!");

        return settings;
    }
    
    public static bool IsProcessableMessage(SocketMessage msg)
    {
        if (msg.Type != MessageType.Default && msg.Type != MessageType.Reply &&
            msg.Type != MessageType.ThreadStarterMessage) return false;
        return true;
    }

    public static string StripQuotes(string str)
    {
        var quotes = new[]
        {
            '"', '\'', 'ʺ', '˝', 'ˮ', '˶', 'ײ', '״', '᳓', '“', '”', '‟', '″', '‶', '〃', '＂'
        };

        var needToStrip = str.Length > 0 && quotes.Contains(str.First()) && quotes.Contains(str.Last());

        return needToStrip ? str[new Range(1, ^1)] : str;
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

    public static ArgumentResult GetArguments(string content)
    {
        var characters = content.ToCharArray();
        
        var arguments = new List<string>();
        var indices = new List<int>();

        for (var i = 0; i < characters.Length; i++)
        {
            var argument = characters[i];
            if (!IsSpace(argument))
            {
                var start = 0;
                var end = 0;

                var safePrevious = (char?)GetSafely(characters, i - 1);
                
                if (argument == '"' && (i < 1 || safePrevious != '\\'))
                {
                    i++;
                    start = i;
                    
                    while (i < content.Length && (characters[i] != '"' || characters[i-1] == '\\'))
                    {
                        i++;
                    }
                    if (i-1 >= 0 && characters[i-1] == '\\')
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
                    
                    while (i < content.Length && !IsSpace(characters[i]) &&
                           (characters[i] != '"' || characters[i-1] == '\\'))
                    {
                        i++;
                    }
                    end = i;
                }
                arguments.Add(string.Join("", content[new Range(start, end)]));

                var previous = 0;
                
                if (indices.Count >= 1) previous = indices[^1];
                
                indices.Add(previous + (end - start) + 1);
            }
        }

        return new ArgumentResult
        {
            Arguments = arguments.ToArray(),
            Indices = indices.ToArray()
        };
    }

    public struct ArgumentResult
    {
        public string[] Arguments;
        public int[] Indices;
    }

    public static bool IsInGuild(SocketMessage msg)
    {
        if (msg.Channel.GetChannelType() == ChannelType.DM ||
            msg.Channel.GetChannelType() == ChannelType.Group) return false;
        return true;
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
        SocketCommandContext context, bool searchDefaultGuild = false)
    {
        var userId = ConvertUserPingToId(userName);
        if (userId > 0) return userId;

        var userList = searchDefaultGuild 
            ? await context.Client.Guilds.Single(guild => guild.Id == DefaultGuild()).SearchUsersAsync(userName)
            : await context.Guild.SearchUsersAsync(userName);
        return userList.Count < 1 ? 0 : userList.First().Id;
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

    public static ulong ConvertChannelPingToId(string channelPing)
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

    public static ulong ConvertUserPingToId(string userPing)
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

    public static ulong ConvertRolePingToId(string rolePing)
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