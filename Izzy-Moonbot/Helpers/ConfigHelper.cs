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
    public static bool DoesValueExist<T>(ServerSettings settings, string key) where T : ServerSettings
    {
        var t = typeof(T);

        if (t.GetProperty(key) == null) return false;
        return true;
    }

#nullable enable
    public static object? GetValue<T>(ServerSettings settings, string key) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from ServerSettings.");

        var t = typeof(T);

        return t.GetProperty(key).GetValue(settings);
    }

    public static async Task<string?> SetStringValue<T>(ServerSettings settings, string key, string? stringResolvable)
        where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings.");

        var t = typeof(T);

        t.GetProperty(key).SetValue(settings, stringResolvable);

        await FileHelper.SaveSettingsAsync(settings);
        return stringResolvable;
    }

    public static async Task<char?> SetCharValue<T>(ServerSettings settings, string key, char? charResolvable)
        where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings.");

        if (charResolvable == null)
        {
            // spain without the `s`
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, null);

            await FileHelper.SaveSettingsAsync(settings);
            return null;
        }
        else
        {
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, charResolvable);

            await FileHelper.SaveSettingsAsync(settings);
            return charResolvable;
        }
    }

    public static async Task<bool?> SetBooleanValue<T>(ServerSettings settings, string key, string? boolResolvable)
        where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings.");

        if (boolResolvable == null)
        {
            // spain without the `s`
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, null);

            await FileHelper.SaveSettingsAsync(settings);
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

                    await FileHelper.SaveSettingsAsync(settings);
                    return true;
                case "false":
                case "no":
                case "disable":
                case "deactivate":
                case "off":
                case "n":
                    t.GetProperty(key).SetValue(settings, false);

                    await FileHelper.SaveSettingsAsync(settings);
                    return false;
                default:
                    throw new FormatException($"Couldn't process {boolResolvable} into a boolean.");
            }
        }
    }

    public static async Task<int?> SetIntValue<T>(ServerSettings settings, string key, int? intResolvable)
        where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        if (intResolvable == null)
        {
            // spain without the `s`
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, null);

            await FileHelper.SaveSettingsAsync(settings);
            return null;
        }
        else
        {
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, intResolvable);

            await FileHelper.SaveSettingsAsync(settings);
            return intResolvable;
        }
    }

    public static async Task<double?> SetDoubleValue<T>(ServerSettings settings, string key, double? doubleResolvable)
        where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        if (doubleResolvable == null)
        {
            // spain without the `s`
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, null);

            await FileHelper.SaveSettingsAsync(settings);
            return null;
        }
        else
        {
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, doubleResolvable);

            await FileHelper.SaveSettingsAsync(settings);
            return doubleResolvable;
        }
    }

    public static async Task<SocketGuildUser?> SetUserValue<T>(ServerSettings settings, string key,
        string? userResolvable, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        if (userResolvable == null)
        {
            // spain without the `s`
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, null);

            await FileHelper.SaveSettingsAsync(settings);
            return null;
        }
        else
        {
            var userId = await DiscordHelper.GetUserIdFromPingOrIfOnlySearchResultAsync(userResolvable, context);

            if (userId == 0) throw new MemberAccessException($"Couldn't find user using resolvable `{userResolvable}`");

            var user = context.Guild.GetUser(userId);

            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, userId, null);

            await FileHelper.SaveSettingsAsync(settings);
            return user;
        }
    }

    public static async Task<SocketRole?> SetRoleValue<T>(ServerSettings settings, string key, string? roleResolvable,
        SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        if (roleResolvable == null)
        {
            // spain without the `s`
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, null);

            await FileHelper.SaveSettingsAsync(settings);
            return null;
        }
        else
        {
            var roleId = DiscordHelper.GetRoleIdIfAccessAsync(roleResolvable, context);

            if (roleId == 0) throw new MemberAccessException($"Couldn't find role using resolvable `{roleResolvable}`");

            var role = context.Guild.GetRole(roleId);

            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, roleId, null);

            await FileHelper.SaveSettingsAsync(settings);
            return role;
        }
    }

    public static async Task<SocketGuildChannel?> SetChannelValue<T>(ServerSettings settings, string key,
        string? channelResolvable, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        if (channelResolvable == null)
        {
            // spain without the `s`
            var t = typeof(T);

            t.GetProperty(key).SetValue(settings, null);

            await FileHelper.SaveSettingsAsync(settings);
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

            await FileHelper.SaveSettingsAsync(settings);
            return channel;
        }
    }

    public static bool HasValueInList<T>(ServerSettings settings, string key, object? value) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = t.GetProperty(key).GetValue(settings) as List<object?>;

        if (list.Contains(value)) return true;
        return false;
    }

    public static List<string>? GetStringList<T>(ServerSettings settings, string key) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<string>?)t.GetProperty(key).GetValue(settings);

        if (list == null)
            throw new NullReferenceException(
                $"'{key}' in ServerSettings is null when it should be a List. Is the config corrupted?");

        return list;
    }

    public static async Task<string> AddToStringList<T>(ServerSettings settings, string key, string value)
        where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<string>?)t.GetProperty(key).GetValue(settings);

        list.Add(value);

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveSettingsAsync(settings);
        return value;
    }

    public static async Task<string> RemoveFromStringList<T>(ServerSettings settings, string key, string value)
        where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<string>?)t.GetProperty(key).GetValue(settings);

        list.Remove(value);

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveSettingsAsync(settings);
        return value;
    }

    public static List<char>? GetCharList<T>(ServerSettings settings, string key) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<char>?)t.GetProperty(key).GetValue(settings);

        return list;
    }

    public static async Task<char> AddToCharList<T>(ServerSettings settings, string key, char charResolvable)
        where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<char>?)t.GetProperty(key).GetValue(settings);

        list.Add(charResolvable);

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveSettingsAsync(settings);
        return charResolvable;
    }

    public static async Task<char> RemoveFromCharList<T>(ServerSettings settings, string key, char charResolvable)
        where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<char>?)t.GetProperty(key).GetValue(settings);

        list.Remove(charResolvable);

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveSettingsAsync(settings);
        return charResolvable;
    }

    public static List<bool>? GetBooleanList<T>(ServerSettings settings, string key) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<bool>?)t.GetProperty(key).GetValue(settings);

        return list;
    }

    public static async Task<bool> AddToBooleanList<T>(ServerSettings settings, string key, string booleanResolvable)
        where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

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

                await FileHelper.SaveSettingsAsync(settings);
                return true;
            case "false":
            case "no":
            case "disable":
            case "deactivate":
            case "off":
            case "n":
                list.Add(false);

                t.GetProperty(key).SetValue(settings, list);

                await FileHelper.SaveSettingsAsync(settings);
                return false;
            default:
                throw new FormatException($"Couldn't process {booleanResolvable} into a boolean.");
        }
    }

    public static async Task<bool> RemoveFromBooleanList<T>(ServerSettings settings, string key,
        string booleanResolvable) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

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

                await FileHelper.SaveSettingsAsync(settings);
                return true;
            case "false":
            case "no":
            case "disable":
            case "deactivate":
            case "off":
            case "n":
                list.Remove(false);

                t.GetProperty(key).SetValue(settings, list);

                await FileHelper.SaveSettingsAsync(settings);
                return false;
            default:
                throw new FormatException($"Couldn't process {booleanResolvable} into a boolean.");
        }
    }

    public static List<int>? GetIntList<T>(ServerSettings settings, string key) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<int>?)t.GetProperty(key).GetValue(settings);

        return list;
    }

    public static async Task<int> AddToIntList<T>(ServerSettings settings, string key, int intResolvable)
        where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<int>?)t.GetProperty(key).GetValue(settings);

        list.Add(intResolvable);

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveSettingsAsync(settings);
        return intResolvable;
    }

    public static async Task<int> RemoveFromIntList<T>(ServerSettings settings, string key, int intResolvable)
        where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<int>?)t.GetProperty(key).GetValue(settings);

        var result = list.Remove(intResolvable);
        if (!result) throw new ArgumentOutOfRangeException($"'{intResolvable}' was not in the list to begin with.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveSettingsAsync(settings);
        return intResolvable;
    }

    public static List<double>? GetDoubleList<T>(ServerSettings settings, string key) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<double>?)t.GetProperty(key).GetValue(settings);

        return list;
    }

    public static async Task<double> AddToDoubleList<T>(ServerSettings settings, string key, double doubleResolvable)
        where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<double>?)t.GetProperty(key).GetValue(settings);

        list.Add(doubleResolvable);

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveSettingsAsync(settings);
        return doubleResolvable;
    }

    public static async Task<double> RemoveFromDoubleList<T>(ServerSettings settings, string key,
        double doubleResolvable) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IList &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(List<>)))) throw new ArgumentException($"'{key}' is not a List.");

        var list = (List<double>?)t.GetProperty(key).GetValue(settings);

        var result = list.Remove(doubleResolvable);
        if (!result) throw new ArgumentOutOfRangeException($"'{doubleResolvable}' was not in the list to begin with.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveSettingsAsync(settings);
        return doubleResolvable;
    }

    private static HashSet<SocketGuildUser> UserIdToUser(HashSet<ulong> list, SocketCommandContext context)
    {
        HashSet<SocketGuildUser> finalList = new();

        foreach (var user in list) finalList.Add(context.Guild.GetUser(user));

        return finalList;
    }

    public static HashSet<SocketGuildUser> GetUserList<T>(ServerSettings settings, string key,
        SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is ISet<ulong> &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(HashSet<>)))) throw new ArgumentException($"'{key}' is not a HashSet.");

        var list = (HashSet<ulong>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        return UserIdToUser(list, context);
    }

    public static async Task<SocketGuildUser> AddToUserList<T>(ServerSettings settings, string key,
        string userResolvable, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

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

        await FileHelper.SaveSettingsAsync(settings);
        return user;
    }

    public static async Task<SocketGuildUser> RemoveFromUserList<T>(ServerSettings settings, string key,
        string userResolvable, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

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

        await FileHelper.SaveSettingsAsync(settings);
        return user;
    }

    private static HashSet<SocketRole> RoleIdToRole(HashSet<ulong> list, SocketCommandContext context)
    {
        HashSet<SocketRole> finalList = new();

        foreach (var role in list) finalList.Add(context.Guild.GetRole(role));

        return finalList;
    }

    public static HashSet<SocketRole> GetRoleList<T>(ServerSettings settings, string key, SocketCommandContext context)
        where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is ISet<ulong> &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(HashSet<>)))) throw new ArgumentException($"'{key}' is not a HashSet.");

        var list = (HashSet<ulong>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        return RoleIdToRole(list, context);
    }

    public static async Task<SocketRole> AddToRoleList<T>(ServerSettings settings, string key, string roleResolvable,
        SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

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

        await FileHelper.SaveSettingsAsync(settings);
        return role;
    }

    public static async Task<SocketRole> RemoveFromRoleList<T>(ServerSettings settings, string key,
        string roleResolvable, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

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

        await FileHelper.SaveSettingsAsync(settings);
        return role;
    }

    private static HashSet<SocketGuildChannel> ChannelIdToChannel(HashSet<ulong> list, SocketCommandContext context)
    {
        HashSet<SocketGuildChannel> finalList = new();

        foreach (var channel in list) finalList.Add(context.Guild.GetChannel(channel));

        return finalList;
    }

    public static HashSet<SocketGuildChannel> GetChannelList<T>(ServerSettings settings, string key,
        SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is ISet<ulong> &&
              t.GetProperty(key).GetValue(settings).GetType().IsGenericType &&
              t.GetProperty(key).GetValue(settings).GetType().GetGenericTypeDefinition()
                  .IsAssignableFrom(typeof(HashSet<>)))) throw new ArgumentException($"'{key}' is not a HashSet.");

        var list = (HashSet<ulong>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        return ChannelIdToChannel(list, context);
    }

    public static async Task<SocketGuildChannel> AddToChannelList<T>(ServerSettings settings, string key,
        string channelResolvable, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

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

        await FileHelper.SaveSettingsAsync(settings);
        return channel;
    }

    public static async Task<SocketGuildChannel> RemoveFromChannelList<T>(ServerSettings settings, string key,
        string channelResolvable, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

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

        await FileHelper.SaveSettingsAsync(settings);
        return channel;
    }

    public static Dictionary<string, string> GetStringDictionary<T>(ServerSettings settings, string key,
        SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, string>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string>.");

        var list = (Dictionary<string, string>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        return list;
    }

    public static async Task<string> CreateStringDictionaryKey<T>(ServerSettings settings, string key,
        string dictionaryKey, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is Dictionary<string, string>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string>.");

        var list = (IDictionary<string, string>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        try
        {
            list.Add(dictionaryKey, "");

            t.GetProperty(key).SetValue(settings, list);

            await FileHelper.SaveSettingsAsync(settings);
            return dictionaryKey;
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentOutOfRangeException($"'{dictionaryKey}' is already present within the Dictionary.");
        }
    }

    public static async Task<string> RemoveStringDictionaryKey<T>(ServerSettings settings, string key,
        string dictionaryKey, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, string>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string>.");

        var list = (IDictionary<string, string>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        var result = list.Remove(dictionaryKey);
        if (!result)
            throw new ArgumentOutOfRangeException($"'{dictionaryKey}' was not in the Dictionary to begin with.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveSettingsAsync(settings);
        return dictionaryKey;
    }

    public static string GetStringDictionaryValue<T>(ServerSettings settings, string key, string dictionaryKey,
        SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, string>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string>.");

        var list = (IDictionary<string, string>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        return list[dictionaryKey];
    }

    public static async Task<string> SetStringDictionaryValue<T>(ServerSettings settings, string key,
        string dictionaryKey, string value, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, string>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string>.");

        var list = (IDictionary<string, string>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        list[dictionaryKey] = value;

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveSettingsAsync(settings);
        return dictionaryKey;
    }

    public static Dictionary<string, string?> GetNullableStringDictionary<T>(ServerSettings settings, string key,
        SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, string?>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string?>.");

        var list = (Dictionary<string, string?>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        return list;
    }

    public static async Task<string> CreateNullableStringDictionaryKey<T>(ServerSettings settings, string key,
        string dictionaryKey, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is Dictionary<string, string?>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string?>.");

        var list = (IDictionary<string, string?>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        try
        {
            list.Add(dictionaryKey, null);

            t.GetProperty(key).SetValue(settings, list);

            await FileHelper.SaveSettingsAsync(settings);
            return dictionaryKey;
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentOutOfRangeException($"'{dictionaryKey}' is already present within the Dictionary.");
        }
    }

    public static async Task<string> RemoveNullableStringDictionaryKey<T>(ServerSettings settings, string key,
        string dictionaryKey, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, string?>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string?>.");

        var list = (IDictionary<string, string?>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        var result = list.Remove(dictionaryKey);
        if (!result)
            throw new ArgumentOutOfRangeException($"'{dictionaryKey}' was not in the Dictionary to begin with.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveSettingsAsync(settings);
        return dictionaryKey;
    }

    public static string? GetNullableStringDictionaryValue<T>(ServerSettings settings, string key, string dictionaryKey,
        SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, string?>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string?>.");

        var list = (IDictionary<string, string?>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        return list[dictionaryKey];
    }

    public static async Task<string?> SetNullableStringDictionaryValue<T>(ServerSettings settings, string key,
        string dictionaryKey, string? value, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, string?>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string?>.");

        var list = (IDictionary<string, string?>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        list[dictionaryKey] = value;

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveSettingsAsync(settings);
        return value;
    }

    public static Dictionary<string, bool> GetBooleanDictionary<T>(ServerSettings settings, string key,
        SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, bool>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, bool>.");

        var list = (Dictionary<string, bool>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        return list;
    }

    public static async Task<string> CreateBooleanDictionaryKey<T>(ServerSettings settings, string key,
        string dictionaryKey, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is Dictionary<string, bool>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, bool>.");

        var list = (IDictionary<string, bool>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        try
        {
            list.Add(dictionaryKey, false);

            t.GetProperty(key).SetValue(settings, list);

            await FileHelper.SaveSettingsAsync(settings);
            return dictionaryKey;
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentOutOfRangeException($"'{dictionaryKey}' is already present within the Dictionary.");
        }
    }

    public static async Task<string> RemoveBooleanDictionaryKey<T>(ServerSettings settings, string key,
        string dictionaryKey, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, bool>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, bool>.");

        var list = (IDictionary<string, bool>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        var result = list.Remove(dictionaryKey);
        if (!result)
            throw new ArgumentOutOfRangeException($"'{dictionaryKey}' was not in the Dictionary to begin with.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveSettingsAsync(settings);
        return dictionaryKey;
    }

    public static bool GetBooleanDictionaryValue<T>(ServerSettings settings, string key, string dictionaryKey,
        SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, bool>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, bool>.");

        var list = (IDictionary<string, bool>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        return list[dictionaryKey];
    }

    public static async Task<bool> SetBooleanDictionaryValue<T>(ServerSettings settings, string key,
        string dictionaryKey, string value, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, bool>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, bool>.");

        var list = (IDictionary<string, bool>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

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

        await FileHelper.SaveSettingsAsync(settings);
        return list[dictionaryKey];
    }

    public static Dictionary<string, List<string>> GetStringListDictionary<T>(ServerSettings settings, string key,
        SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, List<string>>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, List<string>>.");

        var list = (Dictionary<string, List<string>>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("HashSet is null *despite* already being nullchecked?");

        return list;
    }

    public static async Task<string> CreateStringListDictionaryKey<T>(ServerSettings settings, string key,
        string dictionaryKey, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is Dictionary<string, List<string>>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, List<string>>.");

        var list = (IDictionary<string, List<string>>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        try
        {
            list.Add(dictionaryKey, new List<string>());

            t.GetProperty(key).SetValue(settings, list);

            await FileHelper.SaveSettingsAsync(settings);
            return dictionaryKey;
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentOutOfRangeException($"'{dictionaryKey}' is already present within the Dictionary.");
        }
    }

    public static async Task<string> RemoveStringListDictionaryKey<T>(ServerSettings settings, string key,
        string dictionaryKey, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, List<string>>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, List<string>>.");

        var list = (IDictionary<string, List<string>>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        var result = list.Remove(dictionaryKey);
        if (!result)
            throw new ArgumentOutOfRangeException($"'{dictionaryKey}' was not in the Dictionary to begin with.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveSettingsAsync(settings);
        return dictionaryKey;
    }

    public static List<string> GetStringListDictionaryValue<T>(ServerSettings settings, string key,
        string dictionaryKey, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, List<string>>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, List<string>>.");

        var list = (IDictionary<string, List<string>>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        return list[dictionaryKey];
    }

    public static async Task<string> AddToStringListDictionaryValue<T>(ServerSettings settings, string key,
        string dictionaryKey, string value, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, List<string>>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, List<string>>.");

        var list = (IDictionary<string, List<string>>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        list[dictionaryKey].Add(value);

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveSettingsAsync(settings);
        return value;
    }

    public static async Task<string> RemoveFromStringListDictionaryValue<T>(ServerSettings settings, string key,
        string dictionaryKey, string value, SocketCommandContext context) where T : ServerSettings
    {
        if (!DoesValueExist<ServerSettings>(settings, key))
            throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from ServerSettings!");

        var t = typeof(T);

        if (!(t.GetProperty(key).GetValue(settings) is IDictionary<string, List<string>>))
            throw new ArgumentException($"'{key}' is not a Dictionary<string, List<string>>.");

        var list = (IDictionary<string, List<string>>?)t.GetProperty(key).GetValue(settings);

        if (list == null) throw new NullReferenceException("Dictionary is null *despite* already being nullchecked?");

        var result = list[dictionaryKey].Remove(value);
        if (!result)
            throw new ArgumentOutOfRangeException($"'{value}' was not in the Dictionary StringList to begin with.");

        t.GetProperty(key).SetValue(settings, list);

        await FileHelper.SaveSettingsAsync(settings);
        return value;
    }
}