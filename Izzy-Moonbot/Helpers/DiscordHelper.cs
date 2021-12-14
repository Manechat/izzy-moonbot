namespace Izzy_Moonbot.Helpers
{
    using System.Linq;
    using System.Threading.Tasks;
    using Izzy_Moonbot.Settings;
    using Discord.Commands;

    public static class DiscordHelper
    {
        public static async Task<ulong> GetChannelIdIfAccessAsync(string channelName, SocketCommandContext context)
        {
            var id = ConvertChannelPingToId(channelName);
            if (id > 0)
            {
                return await CheckIfChannelExistsAsync(id, context);
            }

            return await CheckIfChannelExistsAsync(channelName, context);
        }

        public static string ConvertChannelPingToName(string channelPing, SocketCommandContext context)
        {
            var id = ConvertChannelPingToId(channelPing);
            if (id <= 0)
            {
                return "<ERROR> Invalid channel";
            }

            var channel = context.Guild.GetTextChannel(id);
            return channel == null ? "<ERROR> Invalid channel" : channel.Name;
        }

        public static ulong GetRoleIdIfAccessAsync(string roleName, SocketCommandContext context)
        {
            var id = ConvertRolePingToId(roleName);
            return id > 0 ? CheckIfRoleExistsAsync(id, context) : CheckIfRoleExistsAsync(roleName, context);
        }

        public static bool DoesUserHaveAdminRoleAsync(SocketCommandContext context, ServerSettings settings)
        {
            if (context.IsPrivate)
            {
                return true;
            }

            return settings.adminRole == 0 || context.Guild.GetUser(context.User.Id).Roles.Any(x => x.Id == settings.adminRole);
        }

        public static bool CanUserRunThisCommand(SocketCommandContext context, ServerSettings settings)
        {
            if (context.IsPrivate)
            {
                return true;
            }

            if (context.Guild.GetUser(context.User.Id).Roles.Any(x => x.Id == settings.adminRole))
            {
                return true;
            }

            foreach (var allowedUser in settings.allowedUsers)
            {
                if (context.User.Id == allowedUser)
                {
                    return true;
                }
            }

            foreach (var ignoredChannel in settings.ignoredChannels)
            {
                if (context.Channel.Id == ignoredChannel)
                {
                    return false;
                }
            }

            foreach (var ignoredRole in settings.ignoredRoles)
            {
                if (context.Guild.GetUser(context.User.Id).Roles.Any(x => x.Id == ignoredRole))
                {
                    return false;
                }
            }

            return true;
        }

        public static async Task<ulong> GeUserIdFromPingOrIfOnlySearchResultAsync(string userName, SocketCommandContext context)
        {
            var userId = ConvertUserPingToId(userName);
            if (userId > 0)
            {
                return userId;
            }

            var userList = await context.Guild.SearchUsersAsync(userName);
            return userList.Count != 1 ? 0 : userList.First().Id;
        }

        public static string CheckAliasesAsync(string message, ServerPreloadedSettings settings)
        {
            var parsedMessage = message.Substring(1).TrimStart();
            foreach (var (shortForm, longForm) in settings.aliases)
            {
                if (message.Substring(1).TrimStart().StartsWith(shortForm))
                {
                    parsedMessage = message.Replace(shortForm, longForm).Substring(1).TrimStart();
                }
            }

            return parsedMessage;
        }

        private static async Task<ulong> CheckIfChannelExistsAsync(string channelName, SocketCommandContext context)
        {
            var izzyMoonbot = await context.Channel.GetUserAsync(context.Client.CurrentUser.Id);
            if (context.IsPrivate)
            {
                return 0;
            }

            foreach (var channel in context.Guild.TextChannels)
            {
                if (channel.Name == channelName && channel.Users.Contains(izzyMoonbot))
                {
                    return channel.Id;
                }
            }

            return 0;
        }

        private static async Task<ulong> CheckIfChannelExistsAsync(ulong channelId, SocketCommandContext context)
        {
            var izzyMoonbot = await context.Channel.GetUserAsync(context.Client.CurrentUser.Id);
            if (context.IsPrivate)
            {
                return 0;
            }

            foreach (var channel in context.Guild.TextChannels)
            {
                if (channel.Id == channelId && channel.Users.Contains(izzyMoonbot))
                {
                    return channel.Id;
                }
            }

            return 0;
        }

        private static ulong ConvertChannelPingToId(string channelPing)
        {
            if (!channelPing.Contains("<#") || !channelPing.Contains(">"))
            {
                return 0;
            }

            var frontTrim = channelPing.Substring(2);
            var trim = frontTrim.Split('>', 2)[0];
            return ulong.Parse(trim);
        }

        private static ulong ConvertUserPingToId(string userPing)
        {
            if (!userPing.Contains("<@!") || !userPing.Contains(">"))
            {
                return 0;
            }

            var frontTrim = userPing.Substring(3);
            var trim = frontTrim.Split('>', 2)[0];
            return ulong.Parse(trim);
        }

        private static ulong CheckIfRoleExistsAsync(string roleName, SocketCommandContext context)
        {
            if (context.IsPrivate)
            {
                return 0;
            }

            foreach (var role in context.Guild.Roles)
            {
                if (role.Name == roleName)
                {
                    return role.Id;
                }
            }

            return 0;
        }

        private static ulong CheckIfRoleExistsAsync(ulong roleId, SocketCommandContext context)
        {
            if (context.IsPrivate)
            {
                return 0;
            }

            foreach (var role in context.Guild.Roles)
            {
                if (role.Id == roleId)
                {
                    return role.Id;
                }
            }

            return 0;
        }

        private static ulong ConvertRolePingToId(string rolePing)
        {
            if (!rolePing.Contains("<@&") || !rolePing.Contains(">"))
            {
                return 0;
            }

            var frontTrim = rolePing.Substring(3);
            var trim = frontTrim.Split('>', 2)[0];
            return ulong.Parse(trim);
        }
    }
}
