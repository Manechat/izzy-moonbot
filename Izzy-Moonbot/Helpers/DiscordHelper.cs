using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Helpers;

public static class DiscordHelper
{
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

    public static string CheckAliasesAsync(string message, ServerSettings settings)
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