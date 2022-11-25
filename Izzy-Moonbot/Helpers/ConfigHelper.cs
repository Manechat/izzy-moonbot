using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Helpers;

public static class ConfigHelper
{
    public static bool DoesValueExist<T>(Config settings, string key) where T : Config
    {
        var t = typeof(T);

        if (t.GetProperty(key) == null) return false;
        return true;
    }

#nullable enable
    public static object? GetValue<T>(Config settings, string key) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config.");

        var t = typeof(T);

        return t.GetProperty(key).GetValue(settings);
    }

    public static async Task<string?> SetStringValue<T>(Config settings, string key, string? stringResolvable)
        where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config.");

        var t = typeof(T);

        t.GetProperty(key).SetValue(settings, stringResolvable);

        await FileHelper.SaveConfigAsync(settings);
        return stringResolvable;
    }

    public static async Task<char?> SetCharValue<T>(Config settings, string key, char? charResolvable)
        where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config.");

        if (charResolvable == null)
        {
            // spain without the `s`
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, null);

            await FileHelper.SaveConfigAsync(settings);
            return null;
        }
        else
        {
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, charResolvable);

            await FileHelper.SaveConfigAsync(settings);
            return charResolvable;
        }
    }

    public static async Task<bool?> SetBooleanValue<T>(Config settings, string key, string? boolResolvable)
        where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config.");

        if (boolResolvable == null)
        {
            // spain without the `s`
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, null);

            await FileHelper.SaveConfigAsync(settings);
            return null;
        }
        else
        {
            var t = typeof(T);

            switch (boolResolvable.ToLower())
            {
                case "true":
                case "yes":
                case "enable":
                case "activate":
                case "on":
                case "y":
                    t.GetProperty(key).SetValue(settings, true);

                    await FileHelper.SaveConfigAsync(settings);
                    return true;
                case "false":
                case "no":
                case "disable":
                case "deactivate":
                case "off":
                case "n":
                    t.GetProperty(key).SetValue(settings, false);

                    await FileHelper.SaveConfigAsync(settings);
                    return false;
                default:
                    throw new FormatException($"Couldn't process {boolResolvable} into a boolean.");
            }
        }
    }

    public static async Task<int?> SetIntValue<T>(Config settings, string key, int? intResolvable)
        where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        if (intResolvable == null)
        {
            // spain without the `s`
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, null);

            await FileHelper.SaveConfigAsync(settings);
            return null;
        }
        else
        {
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, intResolvable);

            await FileHelper.SaveConfigAsync(settings);
            return intResolvable;
        }
    }

    public static async Task<double?> SetDoubleValue<T>(Config settings, string key, double? doubleResolvable)
        where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        if (doubleResolvable == null)
        {
            // spain without the `s`
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, null);

            await FileHelper.SaveConfigAsync(settings);
            return null;
        }
        else
        {
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, doubleResolvable);

            await FileHelper.SaveConfigAsync(settings);
            return doubleResolvable;
        }
    }

    public static async Task<SocketGuildUser?> SetUserValue<T>(Config settings, string key,
        string? userResolvable, SocketCommandContext context) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        if (userResolvable == null)
        {
            // spain without the `s`
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, null);

            await FileHelper.SaveConfigAsync(settings);
            return null;
        }
        else
        {
            var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(userResolvable, context);

            if (userId == 0) throw new MemberAccessException($"Couldn't find user using resolvable `{userResolvable}`");

            var user = context.Guild.GetUser(userId);

            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, userId, null);

            await FileHelper.SaveConfigAsync(settings);
            return user;
        }
    }

    public static async Task<SocketRole?> SetRoleValue<T>(Config settings, string key, string? roleResolvable,
        SocketCommandContext context) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        if (roleResolvable == null)
        {
            // spain without the `s`
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, null);

            await FileHelper.SaveConfigAsync(settings);
            return null;
        }
        else
        {
            var roleId = DiscordHelper.GetRoleIdIfAccessAsync(roleResolvable, context);

            if (roleId == 0) throw new MemberAccessException($"Couldn't find role using resolvable `{roleResolvable}`");

            var role = context.Guild.GetRole(roleId);

            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, roleId, null);

            await FileHelper.SaveConfigAsync(settings);
            return role;
        }
    }

    public static async Task<SocketGuildChannel?> SetChannelValue<T>(Config settings, string key,
        string? channelResolvable, SocketCommandContext context) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        if (channelResolvable == null)
        {
            // spain without the `s`
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, null);

            await FileHelper.SaveConfigAsync(settings);
            return null;
        }
        else
        {
            var channelId = await DiscordHelper.GetChannelIdIfAccessAsync(channelResolvable, context);

            if (channelId == 0)
                throw new MemberAccessException($"Couldn't find channel using resolvable `{channelResolvable}`");

            var channel = context.Guild.GetChannel(channelId);

            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, channelId, null);

            await FileHelper.SaveConfigAsync(settings);
            return channel;
        }
    }

    public static bool HasValueInList<T>(Config settings, string key, object? value) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = t.GetProperty(key).GetValue(settings) as List<object?>;

        if (list.Contains(value)) return true;
        return false;
    }

    public static List<string>? GetStringList<T>(Config settings, string key) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<string>?)t.GetProperty(key).GetValue(settings);

        if (list == null)
            throw new NullReferenceException(
                $"'{key}' in Config is null when it should be a List. Is the config corrupted?");

        return list;
    }

    public static async Task<string> AddToStringList<T>(Config settings, string key, string value)
        where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<string>?)t.GetProperty(key).GetValue(settings);

        list.Add(value);

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return value;
    }

    public static async Task<string> RemoveFromStringList<T>(Config settings, string key, string value)
        where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<string>?)t.GetProperty(key).GetValue(settings);

        list.Remove(value);

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return value;
    }

    public static List<char>? GetCharList<T>(Config settings, string key) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<char>?)t.GetProperty(key).GetValue(settings);

        return list;
    }

    public static async Task<char> AddToCharList<T>(Config settings, string key, char charResolvable)
        where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<char>?)t.GetProperty(key).GetValue(settings);

        list.Add(charResolvable);

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return charResolvable;
    }

    public static async Task<char> RemoveFromCharList<T>(Config settings, string key, char charResolvable)
        where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<char>?)t.GetProperty(key).GetValue(settings);

        list.Remove(charResolvable);

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return charResolvable;
    }

    public static List<bool>? GetBooleanList<T>(Config settings, string key) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<bool>?)t.GetProperty(key).GetValue(settings);

        return list;
    }

    public static async Task<bool> AddToBooleanList<T>(Config settings, string key, string booleanResolvable)
        where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<bool>?)t.GetProperty(key).GetValue(settings);

        switch (booleanResolvable.ToLower())
        {
            case "true":
            case "yes":
            case "enable":
            case "activate":
            case "on":
            case "y":
                list.Add(true);

                t.GetProperty(key).SetValue(settings, list);

                await FileHelper.SaveConfigAsync(settings);
                return true;
            case "false":
            case "no":
            case "disable":
            case "deactivate":
            case "off":
            case "n":
                list.Add(false);

                t.GetProperty(key).SetValue(settings, list);

                await FileHelper.SaveConfigAsync(settings);
                return false;
            default:
                throw new FormatException($"Couldn't process {booleanResolvable} into a boolean.");
        }
    }

    public static async Task<bool> RemoveFromBooleanList<T>(Config settings, string key,
        string booleanResolvable) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<bool>?)t.GetProperty(key).GetValue(settings);

        switch (booleanResolvable.ToLower())
        {
            case "true":
            case "yes":
            case "enable":
            case "activate":
            case "on":
            case "y":
                list.Remove(true);

                t.GetProperty(key).SetValue(settings, list);

                await FileHelper.SaveConfigAsync(settings);
                return true;
            case "false":
            case "no":
            case "disable":
            case "deactivate":
            case "off":
            case "n":
                list.Remove(false);

                t.GetProperty(key).SetValue(settings, list);

                await FileHelper.SaveConfigAsync(settings);
                return false;
            default:
                throw new FormatException($"Couldn't process {booleanResolvable} into a boolean.");
        }
    }

    public static List<int>? GetIntList<T>(Config settings, string key) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<int>?)t.GetProperty(key).GetValue(settings);

        return list;
    }

    public static async Task<int> AddToIntList<T>(Config settings, string key, int intResolvable)
        where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<int>?)t.GetProperty(key).GetValue(settings);

        list.Add(intResolvable);

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return intResolvable;
    }

    public static async Task<int> RemoveFromIntList<T>(Config settings, string key, int intResolvable)
        where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<int>?)t.GetProperty(key).GetValue(settings);

        var result = list.Remove(intResolvable);
        if (!result) throw new ArgumentOutOfRangeException($"'{intResolvable}' was not in the list to begin with.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return intResolvable;
    }

    public static List<double>? GetDoubleList<T>(Config settings, string key) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<double>?)t.GetProperty(key).GetValue(settings);

        return list;
    }

    public static async Task<double> AddToDoubleList<T>(Config settings, string key, double doubleResolvable)
        where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<double>?)t.GetProperty(key).GetValue(settings);

        list.Add(doubleResolvable);

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return doubleResolvable;
    }

    public static async Task<double> RemoveFromDoubleList<T>(Config settings, string key,
        double doubleResolvable) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<double>?)t.GetProperty(key).GetValue(settings);

        var result = list.Remove(doubleResolvable);
        if (!result) throw new ArgumentOutOfRangeException($"'{doubleResolvable}' was not in the list to begin with.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return doubleResolvable;
    }

    private static HashSet<SocketGuildUser> UserIdToUser(HashSet<ulong> list, SocketCommandContext context)
    {
        HashSet<SocketGuildUser> finalList = new();

        foreach (var user in list) finalList.Add(context.Guild.GetUser(user));

        return finalList;
    }

    public static HashSet<SocketGuildUser> GetUserList<T>(Config settings, string key,
        SocketCommandContext context) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is ISet<ulong> &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(HashSet<>)))) throw new ArgumentException($"'{key}' is not a HashSet.");

        var list = (HashSet<ulong>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        return UserIdToUser(list, context);
    }

    public static async Task<SocketGuildUser> AddToUserList<T>(Config settings, string key,
        string userResolvable, SocketCommandContext context) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is ISet<ulong> &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(HashSet<>)))) throw new ArgumentException($"'{key}' is not a HashSet.");

        var list = (HashSet<ulong>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(userResolvable, context);

        if (userId == 0) throw new MemberAccessException($"Cannot access user '{userResolvable}'.");

        var user = context.Guild.GetUser(userId);

        var result = list.Add(userId);
        if (!result) throw new ArgumentOutOfRangeException($"'{userId}' is already present within the HashSet.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return user;
    }

    public static async Task<SocketGuildUser> RemoveFromUserList<T>(Config settings, string key,
        string userResolvable, SocketCommandContext context) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is ISet<ulong> &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(HashSet<>)))) throw new ArgumentException($"'{key}' is not a HashSet.");

        var list = (HashSet<ulong>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(userResolvable, context);

        if (userId == 0) throw new MemberAccessException($"Cannot access user '{userResolvable}'.");

        var user = context.Guild.GetUser(userId);

        var result = list.Remove(userId);
        if (!result) throw new ArgumentOutOfRangeException($"'{userResolvable}' was not in the HashSet to begin with.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return user;
    }

    private static HashSet<SocketRole> RoleIdToRole(HashSet<ulong> list, SocketCommandContext context)
    {
        HashSet<SocketRole> finalList = new();

        foreach (var role in list) finalList.Add(context.Guild.GetRole(role));

        return finalList;
    }

    public static HashSet<SocketRole> GetRoleList<T>(Config settings, string key, SocketCommandContext context)
        where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is ISet<ulong> &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(HashSet<>)))) throw new ArgumentException($"'{key}' is not a HashSet.");

        var list = (HashSet<ulong>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        return RoleIdToRole(list, context);
    }

    public static async Task<SocketRole> AddToRoleList<T>(Config settings, string key, string roleResolvable,
        SocketCommandContext context) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is ISet<ulong> &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(HashSet<>)))) throw new ArgumentException($"'{key}' is not a HashSet.");

        var list = (HashSet<ulong>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        var roleId = DiscordHelper.GetRoleIdIfAccessAsync(roleResolvable, context);

        if (roleId == 0) throw new MemberAccessException($"Cannot access role '{roleResolvable}'.");

        var role = context.Guild.GetRole(roleId);

        var result = list.Add(roleId);
        if (!result) throw new ArgumentOutOfRangeException($"'{roleId}' is already present within the HashSet.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return role;
    }

    public static async Task<SocketRole> RemoveFromRoleList<T>(Config settings, string key,
        string roleResolvable, SocketCommandContext context) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is ISet<ulong> &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(HashSet<>)))) throw new ArgumentException($"'{key}' is not a HashSet.");

        var list = (HashSet<ulong>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        var roleId = DiscordHelper.GetRoleIdIfAccessAsync(roleResolvable, context);

        if (roleId == 0) throw new MemberAccessException($"Cannot access role '{roleResolvable}'.");

        var role = context.Guild.GetRole(roleId);

        var result = list.Remove(roleId);
        if (!result) throw new ArgumentOutOfRangeException($"'{roleId}' was not in the HashSet to begin with.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return role;
    }

    private static HashSet<SocketGuildChannel> ChannelIdToChannel(HashSet<ulong> list, SocketCommandContext context)
    {
        HashSet<SocketGuildChannel> finalList = new();

        foreach (var channel in list) finalList.Add(context.Guild.GetChannel(channel));

        return finalList;
    }

    public static HashSet<SocketGuildChannel> GetChannelList<T>(Config settings, string key,
        SocketCommandContext context) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is ISet<ulong> &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(HashSet<>)))) throw new ArgumentException($"'{key}' is not a HashSet.");

        var list = (HashSet<ulong>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        return ChannelIdToChannel(list, context);
    }

    public static async Task<SocketGuildChannel> AddToChannelList<T>(Config settings, string key,
        string channelResolvable, SocketCommandContext context) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is ISet<ulong> &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(HashSet<>)))) throw new ArgumentException($"'{key}' is not a HashSet.");

        var list = (HashSet<ulong>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        var channelId = await DiscordHelper.GetChannelIdIfAccessAsync(channelResolvable, context);

        if (channelId == 0) throw new MemberAccessException($"Cannot access channel '{channelResolvable}'.");

        var channel = context.Guild.GetChannel(channelId);

        var result = list.Add(channelId);
        if (!result) throw new ArgumentOutOfRangeException($"'{channelId}' is already present within the HashSet.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return channel;
    }

    public static async Task<SocketGuildChannel> RemoveFromChannelList<T>(Config settings, string key,
        string channelResolvable, SocketCommandContext context) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is ISet<ulong> &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(HashSet<>)))) throw new ArgumentException($"'{key}' is not a HashSet.");

        var list = (HashSet<ulong>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        var channelId = await DiscordHelper.GetChannelIdIfAccessAsync(channelResolvable, context);

        if (channelId == 0) throw new MemberAccessException($"Cannot access channel '{channelResolvable}'.");

        var channel = context.Guild.GetChannel(channelId);

        var result = list.Remove(channelId);
        if (!result) throw new ArgumentOutOfRangeException($"'{channelId}' was not in the HashSet to begin with.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return channel;
    }

    public static Dictionary<string, string> GetStringDictionary<T>(Config settings, string key) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, string>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string>.");

        var list = (Dictionary<string, string>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        return list;
    }
    
    public static bool DoesStringDictionaryKeyExist<T>(Config settings, string key, string dictionaryKey) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        var property = t.GetProperty(key);
        if (property == null) return false;

        var items = property.GetValue(settings);
        return items is Dictionary<string, string> dictionary && dictionary.ContainsKey(dictionaryKey);
    }

    public static async Task<(string, string?, string)> CreateStringDictionaryKey<T>(Config settings, string key,
        string dictionaryKey, string value) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is Dictionary<string, string>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string>.");

        var list = (IDictionary<string, string>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        try
        {
            list.Add(dictionaryKey, value);

            t.GetProperty(key).SetValue(settings, list);

            await FileHelper.SaveConfigAsync(settings);
            return (dictionaryKey, null, value);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentOutOfRangeException($"'{dictionaryKey}' is already present within the Dictionary.");
        }
    }

    public static async Task<string> RemoveStringDictionaryKey<T>(Config settings, string key,
        string dictionaryKey) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, string>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string>.");

        var list = (IDictionary<string, string>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        var result = list.Remove(dictionaryKey);
        if (!result)
            throw new ArgumentOutOfRangeException($"'{dictionaryKey}' was not in the Dictionary to begin with.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return dictionaryKey;
    }

    public static string GetStringDictionaryValue<T>(Config settings, string key, string dictionaryKey) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, string>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string>.");

        var list = (IDictionary<string, string>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");
        
        if (!list.ContainsKey(dictionaryKey)) throw new KeyNotFoundException($"'{dictionaryKey}' is not in '{key}'");

        return list[dictionaryKey];
    }

    public static async Task<(string, string?, string)> SetStringDictionaryValue<T>(Config settings, string key,
        string dictionaryKey, string value) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, string>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string>.");

        var list = (IDictionary<string, string>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        if (!list.TryGetValue(dictionaryKey, out var oldValue)) oldValue = null;
        
        list[dictionaryKey] = value;

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return (dictionaryKey, oldValue, value);
    }

    public static Dictionary<string, string?> GetNullableStringDictionary<T>(Config settings, string key) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, string?>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string?>.");

        var list = (Dictionary<string, string?>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        return list;
    }

    public static bool DoesNullableStringDictionaryKeyExist<T>(Config settings, string key, string dictionaryKey) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        var property = t.GetProperty(key);
        if (property == null) return false;

        var items = property.GetValue(settings);
        return items is Dictionary<string, string?> dictionary && dictionary.ContainsKey(dictionaryKey);
    }

    public static async Task<(string, string?, string?)> CreateNullableStringDictionaryKey<T>(Config settings, string key,
        string dictionaryKey, string? value) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is Dictionary<string, string?>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string?>.");

        var list = (IDictionary<string, string?>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        try
        {
            list.Add(dictionaryKey, value);

            t.GetProperty(key).SetValue(settings, list);

            await FileHelper.SaveConfigAsync(settings);
            return (dictionaryKey, null, value);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentOutOfRangeException($"'{dictionaryKey}' is already present within the Dictionary.");
        }
    }

    public static async Task<string> RemoveNullableStringDictionaryKey<T>(Config settings, string key,
        string dictionaryKey) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, string?>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string?>.");

        var list = (IDictionary<string, string?>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        var result = list.Remove(dictionaryKey);
        if (!result)
            throw new ArgumentOutOfRangeException($"'{dictionaryKey}' was not in the Dictionary to begin with.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return dictionaryKey;
    }

    public static string? GetNullableStringDictionaryValue<T>(Config settings, string key, string dictionaryKey) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, string?>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string?>.");

        var list = (IDictionary<string, string?>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        if (!list.ContainsKey(dictionaryKey)) throw new KeyNotFoundException($"'{dictionaryKey}' is not in '{key}'");

        return list[dictionaryKey];
    }

    public static async Task<(string, string?, string?)> SetNullableStringDictionaryValue<T>(Config settings, string key,
        string dictionaryKey, string? value) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, string?>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string?>.");

        var list = (IDictionary<string, string?>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        if (!list.TryGetValue(dictionaryKey, out var oldValue)) oldValue = null;
        
        list[dictionaryKey] = value;

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return (dictionaryKey, oldValue, value);
    }

    public static Dictionary<string, bool> GetBooleanDictionary<T>(Config settings, string key) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, bool>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, bool>.");

        var list = (Dictionary<string, bool>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        return list;
    }
    
    public static bool DoesBooleanDictionaryKeyExist<T>(Config settings, string key, string dictionaryKey) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        var property = t.GetProperty(key);
        if (property == null) return false;

        var items = property.GetValue(settings);
        return items is Dictionary<string, bool> dictionary && dictionary.ContainsKey(dictionaryKey);
    }

    public static async Task<(string, bool?, bool)> CreateBooleanDictionaryKey<T>(Config settings, string key,
        string dictionaryKey, string value) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is Dictionary<string, bool>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, bool>.");

        var list = (IDictionary<string, bool>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        try
        {
            var boolean = false;
            
            switch (value.ToLower())
            {
                case "true":
                case "yes":
                case "enable":
                case "activate":
                case "on":
                case "y":
                    boolean = true;
                    break;
                case "false":
                case "no":
                case "disable":
                case "deactivate":
                case "off":
                case "n":
                    boolean = false;
                    break;
                default:
                    throw new FormatException($"Couldn't process {value} into a boolean.");
            }

            list.Add(dictionaryKey, boolean);

            t.GetProperty(key).SetValue(settings, list);

            await FileHelper.SaveConfigAsync(settings);
            return (dictionaryKey, null, boolean);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentOutOfRangeException($"'{dictionaryKey}' is already present within the Dictionary.");
        }
    }

    public static async Task<string> RemoveBooleanDictionaryKey<T>(Config settings, string key,
        string dictionaryKey) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, bool>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, bool>.");

        var list = (IDictionary<string, bool>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        var result = list.Remove(dictionaryKey);
        if (!result)
            throw new ArgumentOutOfRangeException($"'{dictionaryKey}' was not in the Dictionary to begin with.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return dictionaryKey;
    }

    public static bool GetBooleanDictionaryValue<T>(Config settings, string key, string dictionaryKey) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, bool>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, bool>.");

        var list = (IDictionary<string, bool>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        if (!list.ContainsKey(dictionaryKey)) throw new KeyNotFoundException($"'{dictionaryKey}' is not in '{key}'");

        return list[dictionaryKey];
    }

    public static async Task<(string, bool?, bool)> SetBooleanDictionaryValue<T>(Config settings, string key,
        string dictionaryKey, string value) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, bool>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, bool>.");

        var list = (IDictionary<string, bool>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        bool? oldValue = list[dictionaryKey];
        
        switch (value.ToLower())
        {
            case "true":
            case "yes":
            case "enable":
            case "activate":
            case "on":
            case "y":
                list[dictionaryKey] = true;
                break;
            case "false":
            case "no":
            case "disable":
            case "deactivate":
            case "off":
            case "n":
                list[dictionaryKey] = false;
                break;
            default:
                throw new FormatException($"Couldn't process {value} into a boolean.");
        }

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return (dictionaryKey, oldValue, list[dictionaryKey]);
    }

    public static Dictionary<string, List<string>> GetStringListDictionary<T>(Config settings, string key) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, List<string>>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, List<string>>.");

        var list = (Dictionary<string, List<string>>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        return list;
    }
    
    public static bool DoesStringListDictionaryKeyExist<T>(Config settings, string key, string dictionaryKey) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        var property = t.GetProperty(key);
        if (property == null) return false;

        var items = property.GetValue(settings);
        return items is Dictionary<string, List<string>> dictionary && dictionary.ContainsKey(dictionaryKey);
    }

    public static async Task<(string, string)> CreateStringListDictionaryKey<T>(Config settings, string key,
        string dictionaryKey, string value) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is Dictionary<string, List<string>>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, List<string>>.");

        var list = (IDictionary<string, List<string>>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        try
        {
            list.Add(dictionaryKey, new List<string>{ value });

            t.GetProperty(key).SetValue(settings, list);

            await FileHelper.SaveConfigAsync(settings);
            return (dictionaryKey, value);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentOutOfRangeException($"'{dictionaryKey}' is already present within the Dictionary.");
        }
    }

    public static async Task<string> RemoveStringListDictionaryKey<T>(Config settings, string key,
        string dictionaryKey) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, List<string>>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, List<string>>.");

        var list = (IDictionary<string, List<string>>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        var result = list.Remove(dictionaryKey);
        if (!result)
            throw new ArgumentOutOfRangeException($"'{dictionaryKey}' was not in the Dictionary to begin with.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return dictionaryKey;
    }

    public static List<string> GetStringListDictionaryValue<T>(Config settings, string key,
        string dictionaryKey) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, List<string>>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, List<string>>.");

        var list = (IDictionary<string, List<string>>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        if (!list.ContainsKey(dictionaryKey))
            throw new KeyNotFoundException($"'{dictionaryKey}' does not exist within '{key}'");
        
        return list[dictionaryKey];
    }

    public static async Task<(string, string)> AddToStringListDictionaryValue<T>(Config settings, string key,
        string dictionaryKey, string value) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, List<string>>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, List<string>>.");

        var list = (IDictionary<string, List<string>>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        list[dictionaryKey].Add(value);

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return (dictionaryKey, value);
    }

    public static async Task<(string, string)> RemoveFromStringListDictionaryValue<T>(Config settings, string key,
        string dictionaryKey, string value) where T : Config
    {
        if (!DoesValueExist<Config>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, List<string>>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, List<string>>.");

        var list = (IDictionary<string, List<string>>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        var result = list[dictionaryKey].Remove(value);
        if (!result)
            throw new ArgumentOutOfRangeException($"'{value}' was not in the Dictionary StringList to begin with.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveConfigAsync(settings);
        return (dictionaryKey, value);
    }
}