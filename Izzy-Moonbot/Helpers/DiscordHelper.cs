using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Izzy_Moonbot.Settings;
using Izzy_Moonbot.Adapters;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using Flurl.Http;

namespace Izzy_Moonbot.Helpers;

public static class DiscordHelper
{
    // These setters should only be used by tests
    public static ulong? DefaultGuildId { get; set; } = null;
    public static List<ulong>? DevUserIds { get; set; } = null;
    public static bool PleaseAwaitEvents { get; set; } = false;

    // In production code, our event handlers need to return immediately no matter how
    // much work there is to do, or else we "block the gateway task".
    // But in tests we need to wait for that work to complete.
    public static async Task<object?> LeakOrAwaitTask(Task t)
    {
        if (PleaseAwaitEvents)
            await t;
        return Task.CompletedTask;
    }

    public static bool ShouldExecuteInPrivate(bool externalUsageAllowedFlag, SocketCommandContext context)
    {
        return ShouldExecuteInPrivate(externalUsageAllowedFlag, new SocketCommandContextAdapter(context));
    }
    public static bool ShouldExecuteInPrivate(bool externalUsageAllowedFlag, IIzzyContext context)
    {
        if (context.IsPrivate || context.Guild?.Id != DefaultGuild())
        {
            return externalUsageAllowedFlag;
        }
        
        return true;
    }

    public static bool IsDefaultGuild(SocketCommandContext context)
    {
        return IsDefaultGuild(new SocketCommandContextAdapter(context));
    }
    public static bool IsDefaultGuild(IIzzyContext context)
    {
        if (context.IsPrivate) return false;
        
        return context.Guild?.Id == DefaultGuild();
    }
    
    public static ulong DefaultGuild()
    {
        var maybeDefaultGuildId = DefaultGuildId;
        if (maybeDefaultGuildId is ulong defaultGuildId)
            return defaultGuildId;

        try
        {
            var settings = GetDiscordSettings();
            return settings.DefaultGuild;
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("Caught FileNotFoundException in DefaultGuild(). " +
                "If you're seeing this in tests, you probably forgot to set a fake DefaultGuildId.");
            Console.WriteLine(e.Message);
            throw;
        }
    }
    
    public static bool IsDev(ulong user)
    {
        var maybeDevUserIds = DevUserIds;
        if (maybeDevUserIds is List<ulong> devUserIds)
            return devUserIds.Any(userId => userId == user);

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
    
    public static bool IsProcessableMessage(IIzzyMessage msg)
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

    public static object? GetSafely<T>(IEnumerable<T> array, int index)
    {
        if (array.Count() <= index) return null;
        if (index < 0) return null;

        return array.ElementAt(index);
    }

    // The return value is (nextArgString, remainingArgsIfAny)
    // string.Split() does not suffice because we want to support quoted arguments
    public static (string?, string?) GetArgument(string args)
    {
        Func<string, string> trimQuotes = arg =>
        {
            return (arg[0] == '"' && arg.Last() == '"') ?
                arg[1..(arg.Length - 1)] :
                arg;
        };

        var betweenQuotes = false;
        for (var i = 0; i < args.Length; i++)
        {
            var c = args[i];

            // start or end a quoted argument only if that quote is unescaped
            // (and \ only has this special meaning when preceding ")
            if ((c == '"') && (i <= 1 || args[i-1] != '\\'))
                betweenQuotes = !betweenQuotes;

            // found a space outside quotes; that means we've reached the end of the current arg
            if (IsSpace(c) && !betweenQuotes)
            {
                var nextArg = args.Substring(0, i);

                var endOfSpaceRun = i;
                while (endOfSpaceRun < args.Length && IsSpace(args[endOfSpaceRun]))
                    endOfSpaceRun++;

                // If there are only spaces after the arg, then it's the last arg if any
                if (endOfSpaceRun >= args.Length)
                    return (nextArg == "") ?
                        (null, null) :
                        (trimQuotes(nextArg), null);
                // otherwise there are more args left to parse with future GetArgument() calls
                else
                    return (nextArg == "") ?
                        // if this "arg" was the empty string, recurse so caller gets the first non-empty arg if any
                        GetArgument(args.Substring(endOfSpaceRun)) :
                        (trimQuotes(nextArg), args.Substring(endOfSpaceRun));
            }
        }

        // If we never found a space (outside a quoted arg), then the whole string is at most one arg
        return (args == "") ?
            (null, null) :
            (trimQuotes(args), null);
    }

    public static ArgumentResult GetArguments(string content)
    {
        var arguments = new List<string>();
        var indices = new List<int>();

        var (nextArg, remainingArgs) = GetArgument(content);
        if (nextArg != null)
        {
            arguments.Add(nextArg);
            if (remainingArgs != null)
                indices.Add(content.Length - remainingArgs.Length);
            else
                indices.Add(content.Length);

            while (remainingArgs != null)
            {
                var remainingLength = remainingArgs!.Length;

                (nextArg, remainingArgs) = GetArgument(remainingArgs);
                if (nextArg != null)
                {
                    arguments.Add(nextArg);
                    if (remainingArgs != null)
                        indices.Add(indices.Last() + (remainingLength - remainingArgs.Length));
                    else
                        indices.Add(content.Length);
                }
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

    public static bool IsInGuild(IIzzyMessage msg)
    {
        if (msg.Channel.GetChannelType() == ChannelType.DM ||
            msg.Channel.GetChannelType() == ChannelType.Group) return false;
        return true;
    }

    public static async Task<ulong> GetChannelIdIfAccessAsync(string channelName, SocketCommandContext context)
    {
        return await GetChannelIdIfAccessAsync(channelName, new SocketCommandContextAdapter(context));
    }
    public static async Task<ulong> GetChannelIdIfAccessAsync(string channelName, IIzzyContext context)
    {
        var id = ConvertChannelPingToId(channelName);
        if (id > 0) return await CheckIfChannelExistsAsync(id, context);

        return await CheckIfChannelExistsAsync(channelName, context);
    }

    public static ulong GetRoleIdIfAccessAsync(string roleName, SocketCommandContext context)
    {
        return GetRoleIdIfAccessAsync(roleName, new SocketCommandContextAdapter(context));
    }
    public static ulong GetRoleIdIfAccessAsync(string roleName, IIzzyContext context)
    {
        var id = ConvertRolePingToId(roleName);
        return id > 0 ? CheckIfRoleExistsAsync(id, context) : CheckIfRoleExistsAsync(roleName, context);
    }

    public static async Task<ulong> GetUserIdFromPingOrIfOnlySearchResultAsync(string userName,
        SocketCommandContext context, bool searchDefaultGuild = false)
    {
        return await GetUserIdFromPingOrIfOnlySearchResultAsync(userName, new SocketCommandContextAdapter(context), searchDefaultGuild);
    }
    public static async Task<ulong> GetUserIdFromPingOrIfOnlySearchResultAsync(string userName,
        IIzzyContext context, bool searchDefaultGuild = false)
    {
        var userId = ConvertUserPingToId(userName);
        if (userId > 0) return userId;

        var userList = searchDefaultGuild 
            ? await context.Client.Guilds.Single(guild => guild.Id == DefaultGuild()).SearchUsersAsync(userName)
            : await context.Guild!.SearchUsersAsync(userName);
        return userList.Count < 1 ? 0 : userList.First().Id;
    }

    private static async Task<ulong> CheckIfChannelExistsAsync(string channelName, SocketCommandContext context)
    {
        return await CheckIfChannelExistsAsync(channelName, new SocketCommandContextAdapter(context));
    }
    private static async Task<ulong> CheckIfChannelExistsAsync(string channelName, IIzzyContext context)
    {
        var izzyMoonbot = await context.Channel.GetUserAsync(context.Client.CurrentUser.Id);
        if (context.IsPrivate || context.Guild == null || izzyMoonbot == null) return 0;

        foreach (var channel in context.Guild.TextChannels)
            if (channel.Name == channelName && channel.Users.Any(u => u.Id == izzyMoonbot.Id))
                return channel.Id;

        return 0;
    }

    private static async Task<ulong> CheckIfChannelExistsAsync(ulong channelId, SocketCommandContext context)
    {
        return await CheckIfChannelExistsAsync(channelId, new SocketCommandContextAdapter(context));
    }
    private static async Task<ulong> CheckIfChannelExistsAsync(ulong channelId, IIzzyContext context)
    {
        var izzyMoonbot = await context.Channel.GetUserAsync(context.Client.CurrentUser.Id);
        if (context.IsPrivate || context.Guild == null || izzyMoonbot == null) return 0;

        foreach (var channel in context.Guild.TextChannels)
            if (channel.Id == channelId && channel.Users.Any(u => u.Id == izzyMoonbot.Id))
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
        return CheckIfRoleExistsAsync(roleName, new SocketCommandContextAdapter(context));
    }
    private static ulong CheckIfRoleExistsAsync(string roleName, IIzzyContext context)
    {
        if (context.IsPrivate || context.Guild == null) return 0;

        foreach (var role in context.Guild.Roles)
            if (role.Name == roleName)
                return role.Id;

        return 0;
    }

    private static ulong CheckIfRoleExistsAsync(ulong roleId, SocketCommandContext context)
    {
        return CheckIfRoleExistsAsync(roleId, new SocketCommandContextAdapter(context));
    }
    private static ulong CheckIfRoleExistsAsync(ulong roleId, IIzzyContext context)
    {
        if (context.IsPrivate || context.Guild == null) return 0;

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

    // Where "Discord whitespace" refers to Char.IsWhiteSpace as well as the ":blank:" emoji
    public static string TrimDiscordWhitespace(string wholeString)
    {
        var singleCharacterOrEmojiOfWhitespace = new List<string> {
            @"\s",
            @":blank:",
            @"<:blank:[0-9]+>"
        };
        var runOfDiscordWhitespace = $"({ string.Join("|", singleCharacterOrEmojiOfWhitespace)})+";

        var leadingWhitespaceRegex = new Regex($"^{runOfDiscordWhitespace}");
        var trailingWhitespaceRegex = new Regex($"{runOfDiscordWhitespace}$");

        var s = wholeString;
        if (leadingWhitespaceRegex.Matches(s).Any())
            s = leadingWhitespaceRegex.Replace(s, "");
        if (trailingWhitespaceRegex.Matches(s).Any())
            s = trailingWhitespaceRegex.Replace(s, "");

        return s;
    }

    public static bool WithinLevenshteinDistanceOf(string source, string target, uint maxDist)
    {
        // only null checks are necessary here, but we might as well early return on empty strings too
        if (String.IsNullOrEmpty(source) && String.IsNullOrEmpty(target))
            return true;
        if (String.IsNullOrEmpty(source))
            return target.Length <= maxDist;
        if (String.IsNullOrEmpty(target))
            return source.Length <= maxDist;

        // The idea is that after j iterations of the main loop, we want:
        // currDists[i] == LD(s[0..i], t[0..j+1])
        // prevDists[i] == LD(s[0..i], t[0..j])
        // So when the loop is over LD(s, t) == currDists[s.Length]

        int[] currDists = new int[source.Length + 1];
        int[] prevDists = new int[source.Length + 1];

        // For the j == 0 base case, there are no prevDists yet, but
        // LD(s[0..i], t[0..j]) == LD(s[0..i], "") == i so that's easy.
        // Set these to currDists so the initial swap puts them in prevDists
        for (int i = 0; i <= source.Length; i++) { currDists[i] = i; }

        // actually compute LD(s[0..i], t[0..j+1]) for every i and j
        for (int j = 0; j < target.Length; j++)
        {
            int[] swap = prevDists;
            prevDists = currDists;
            currDists = swap;

            currDists[0] = j + 1; // i == 0 base case: LD(s[0..0], t[0..j+1]) == j+1

            for (int i = 0; i < source.Length; i++)
            {
                int deletion = currDists[i] + 1;      // if s[i+1] gets deleted,          then LD(s[0..i+1], t[0..j]) == 1        + LD(s[0..i],   t[0..j])
                                                      // example:                              LD("Izzy",    "Izz")   == 1        + LD("Izz",     "Izz") = 1
                int insertion = prevDists[i + 1] + 1; // if t[j] gets inserted at s[i+1], then LD(s[0..i+1], t[0..j]) == 1        + LD(s[0..i+1], t[0..j-1])
                                                      // example:                              LD("Izz",     "Izzy")  == 1        + LD("Izz",     "Izz") = 1
                int substitution = prevDists[i] +     // if s[i+1] gets set to t[j],      then LD(s[0..i+1], t[0..j]) == (0 or 1) + LD(s[0..i],   t[0..j-1])
                    (source[i] == target[j] ? 0 : 1); // example:                              LD("Izzz",    "Izzy")  == 1        + LD("Izz",     "Izz") = 1
                                                      //                                       LD("Izzy",    "Izzy")  == 0        + LD("Izz",     "Izz") = 0

                currDists[i + 1] = Math.Min(deletion, Math.Min(insertion, substitution));
            }

            // if all of currDists is already too high, then we know the final LD will be too
            if (currDists.Min() > maxDist) return false;
        }

        var actualLevenshteinDistance = currDists[source.Length];
        return actualLevenshteinDistance <= maxDist;
    }

    public static async Task SetBannerToUrlImage(string url, IIzzyGuild guild)
    {
        Stream stream = await url
            .WithHeader("user-agent", $"Izzy-Moonbot (Linux x86_64) Flurl.Http/3.2.4 DotNET/7.0")
            .GetStreamAsync();

        var image = new Image(stream);

        await guild.SetBanner(image);
    }

    public static async Task<string> AuditLogForCommand(SocketCommandContext context)
    {
        return await AuditLogForCommand(new SocketCommandContextAdapter(context));
    }
    public static async Task<string> AuditLogForCommand(IIzzyContext context)
    {
        var user = context.User;
        var channel = context.Channel;
        var now = DateTimeHelper.UtcNow;

        // note that newlines, markdown, mentions, etc. aren't applied in audit log messages,
        // but some of them are still useful to make the message clearer
        return $"Command `{context.Message.Content}` " +
            $"was run by {user.Username}#{user.Discriminator} ({user.Id}) " +
            $"in #{channel.Name} ({channel.Id}) " +
            $"at {now} (<t:{now.ToUnixTimeMilliseconds()}>)";
    }
}
