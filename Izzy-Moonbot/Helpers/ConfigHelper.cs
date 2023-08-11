using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Settings;

namespace Izzy_Moonbot.Helpers;

public static class ConfigHelper
{
    public static bool ResolveBool(string boolResolvable)
    {
        switch (boolResolvable.ToLower())
        {
            case "true":
            case "yes":
            case "enable":
            case "activate":
            case "on":
            case "y":
                return true;
            case "false":
            case "no":
            case "disable":
            case "deactivate":
            case "off":
            case "n":
                return false;
            default:
                throw new FormatException($"Couldn't process {boolResolvable} into a boolean.");
        }
    }

#nullable enable
    public static object? GetValue(Config settings, string key)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
            return pinfo.GetValue(settings);

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config.");
    }

    public static async Task<T?> SetSimpleValue<T>(Config settings, string key, T? valueResolvable)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            pinfo.SetValue(settings, valueResolvable);
            await FileHelper.SaveConfigAsync(settings);
            return valueResolvable;
        }

        throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config.");
    }

    public static async Task<bool?> SetBooleanValue(Config settings, string key, string? boolResolvable)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            bool? resolvedBool = boolResolvable is null ? null : ResolveBool(boolResolvable);

            pinfo.SetValue(settings, resolvedBool);
            await FileHelper.SaveConfigAsync(settings);
            return resolvedBool;
        }

        throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config.");
    }

    public static async Task<IIzzyRole?> SetRoleValue(Config settings, string key, string? roleResolvable,
    IIzzyContext context)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            IIzzyRole? role = null;
            if (roleResolvable is not null)
            {
                if (ParseHelper.TryParseRoleResolvable(roleResolvable, context.Guild!, out var roleParseError) is not var (roleId, _))
                    throw new MemberAccessException($"Couldn't find role using resolvable `{roleResolvable}`: {roleParseError}");

                role = context.Guild?.GetRole(roleId);

                pinfo.SetValue(settings, roleId, null);
            }
            else
            {
                pinfo.SetValue(settings, null);
            }

            await FileHelper.SaveConfigAsync(settings);
            return role;
        }

        throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");
    }

    public static async Task<IIzzySocketGuildChannel?> SetChannelValue(Config settings, string key,
        string? channelResolvable, IIzzyContext context)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            IIzzySocketGuildChannel? channel = null;
            if (channelResolvable is null)
            {
                pinfo.SetValue(settings, null);
            }
            else
            {
                if (ParseHelper.TryParseChannelResolvable(channelResolvable, context, out var channelParseError) is not var (channelId, _))
                {
                    pinfo.SetValue(settings, null);
                }
                else
                {
                    channel = context.Guild?.GetChannel(channelId);

                    pinfo.SetValue(settings, channelId, null);
                }
            }

            await FileHelper.SaveConfigAsync(settings);
            return channel;
        }

        throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");
    }

    public static bool HasValueInSet<T>(Config settings, string key, T value)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            var configValue = pinfo.GetValue(settings);
            if (configValue is ISet<T> set
                && set.GetType().IsGenericType
                && set.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(HashSet<>)))
            {
                return set.Contains(value);
            }
            throw new ArgumentException($"'{key}' is not an ISet.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static ISet<string>? GetStringSet(Config settings, string key)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            var configValue = pinfo.GetValue(settings);
            if (configValue is ISet<string> set
                && set.GetType().IsGenericType
                && set.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(HashSet<>)))
            {
                if (set is not null)
                    return set;

                throw new NullReferenceException($"'{key}' in Config is null when it should be a Set. Is the config corrupted?");
            }
            throw new ArgumentException($"'{key}' is not a Set.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static async Task<string> AddToStringSet(Config settings, string key, string value)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            var configValue = pinfo.GetValue(settings);
            if (configValue is ISet<string> set
                && set.GetType().IsGenericType
                && set.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(HashSet<>)))
            {
                set.Add(value);

                pinfo.SetValue(settings, set);
                await FileHelper.SaveConfigAsync(settings);
                return value;
            }
            throw new ArgumentException($"'{key}' is not a Set.");
        }

        throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");
    }

    public static async Task<string> RemoveFromStringSet(Config settings, string key, string value)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            var configValue = pinfo.GetValue(settings);
            if (configValue is ISet<string> set
                && set.GetType().IsGenericType
                && set.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(HashSet<>)))
            {
                set.Remove(value);

                pinfo.SetValue(settings, set);
                await FileHelper.SaveConfigAsync(settings);
                return value;
            }
            throw new ArgumentException($"'{key}' is not a Set.");
        }

        throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");
    }

    public static async Task<ISet<string>> ClearStringSet(Config settings, string key)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            var configValue = pinfo.GetValue(settings);
            if (configValue is ISet<string> set
                && set.GetType().IsGenericType
                && set.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(HashSet<>)))
            {
                var value = new HashSet<string>(set);
                set.Clear();

                pinfo.SetValue(settings, set);
                await FileHelper.SaveConfigAsync(settings);
                return value;
            }
            throw new ArgumentException($"'{key}' is not a Set.");
        }

        throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");
    }

    private static HashSet<IIzzyUser> UserIdToUser(HashSet<ulong> set, IIzzyContext context)
    {
        HashSet<IIzzyUser> finalSet = new();

        foreach (var userId in set)
            if (context.Guild?.GetUser(userId) is IIzzyUser user)
                finalSet.Add(user);

        return finalSet;
    }

    public static HashSet<IIzzyUser> GetUserSet(Config settings, string key,
        IIzzyContext context)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            var configValue = pinfo.GetValue(settings);
            if (configValue is HashSet<ulong> set
                && set.GetType().IsGenericType
                && set.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(HashSet<>)))
            {
                return UserIdToUser(set, context);
            }
            throw new ArgumentException($"'{key}' is not a HashSet.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static async Task<IIzzyUser?> AddToUserSet(Config settings, string key,
        string userResolvable, IIzzyContext context)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            var configValue = pinfo.GetValue(settings);
            if (configValue is HashSet<ulong> set
                && set.GetType().IsGenericType
                && set.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(HashSet<>)))
            {
                var (userId, userError) = await ParseHelper.TryParseUserResolvable(userResolvable, context.Guild!);
                if (userId == null) throw new MemberAccessException($"Cannot access user '{userResolvable}': {userError}");

                var user = context.Guild?.GetUser((ulong)userId);

                var result = set.Add((ulong)userId);
                if (!result) throw new ArgumentOutOfRangeException($"'{userId}' is already present within the HashSet.");

                pinfo.SetValue(settings, set);
                await FileHelper.SaveConfigAsync(settings);
                return user;
            }
            throw new ArgumentException($"'{key}' is not a HashSet.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static async Task<IIzzyUser?> RemoveFromUserSet(Config settings, string key,
        string userResolvable, IIzzyContext context)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            var configValue = pinfo.GetValue(settings);
            if (configValue is HashSet<ulong> set
                && set.GetType().IsGenericType
                && set.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(HashSet<>)))
            {
                var (userId, userError) = await ParseHelper.TryParseUserResolvable(userResolvable, context.Guild!);
                if (userId == null) throw new MemberAccessException($"Cannot access user '{userResolvable}': {userError}");

                var user = context.Guild?.GetUser((ulong)userId);

                var result = set.Remove((ulong)userId);
                if (!result) throw new ArgumentOutOfRangeException($"'{userResolvable}' was not in the HashSet to begin with.");

                pinfo.SetValue(settings, set);
                await FileHelper.SaveConfigAsync(settings);
                return user;
            }
            throw new ArgumentException($"'{key}' is not a HashSet.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    private static HashSet<IIzzyRole> RoleIdToRole(HashSet<ulong> set, IIzzyContext context)
    {
        HashSet<IIzzyRole> finalSet = new();

        foreach (var roleId in set)
            if (context.Guild?.GetRole(roleId) is IIzzyRole role)
                finalSet.Add(role);

        return finalSet;
    }

    public static HashSet<IIzzyRole> GetRoleSet(Config settings, string key, IIzzyContext context)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            var configValue = pinfo.GetValue(settings);
            if (configValue is HashSet<ulong> set
                && set.GetType().IsGenericType
                && set.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(HashSet<>)))
            {
                return RoleIdToRole(set, context);
            }
            throw new ArgumentException($"'{key}' is not a HashSet.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static async Task<IIzzyRole?> AddToRoleSet(Config settings, string key, string roleResolvable,
        IIzzyContext context)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            var configValue = pinfo.GetValue(settings);
            if (configValue is HashSet<ulong> set
                && set.GetType().IsGenericType
                && set.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(HashSet<>)))
            {
                if (ParseHelper.TryParseRoleResolvable(roleResolvable, context.Guild!, out var roleParseError) is not var (roleId, _))
                    throw new MemberAccessException($"Cannot access role '{roleResolvable}': {roleParseError}");

                var role = context.Guild?.GetRole(roleId);

                var result = set.Add(roleId);
                if (!result) throw new ArgumentOutOfRangeException($"'{roleId}' is already present within the HashSet.");

                pinfo.SetValue(settings, set);
                await FileHelper.SaveConfigAsync(settings);
                return role;
            }
            throw new ArgumentException($"'{key}' is not a HashSet.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static async Task<IIzzyRole?> RemoveFromRoleSet(Config settings, string key,
        string roleResolvable, IIzzyContext context)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            var configValue = pinfo.GetValue(settings);
            if (configValue is HashSet<ulong> set
                && set.GetType().IsGenericType
                && set.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(HashSet<>)))
            {
                if (ParseHelper.TryParseRoleResolvable(roleResolvable, context.Guild!, out var roleParseError) is not var (roleId, _))
                    throw new MemberAccessException($"Cannot access role '{roleResolvable}': {roleParseError}");

                var role = context.Guild?.GetRole(roleId);

                var result = set.Remove(roleId);
                if (!result) throw new ArgumentOutOfRangeException($"'{roleId}' was not in the HashSet to begin with.");

                pinfo.SetValue(settings, set);
                await FileHelper.SaveConfigAsync(settings);
                return role;
            }
            throw new ArgumentException($"'{key}' is not a HashSet.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static async Task<ISet<ulong>> ClearRoleSet(Config settings, string key)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            var configValue = pinfo.GetValue(settings);
            if (configValue is ISet<ulong> set
                && set.GetType().IsGenericType
                && set.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(HashSet<>)))
            {
                var value = new HashSet<ulong>(set);
                set.Clear();

                pinfo.SetValue(settings, set);
                await FileHelper.SaveConfigAsync(settings);
                return value;
            }
            throw new ArgumentException($"'{key}' is not a Set.");
        }

        throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");
    }

    private static HashSet<IIzzySocketGuildChannel> ChannelIdToChannel(HashSet<ulong> set, IIzzyContext context)
    {
        HashSet<IIzzySocketGuildChannel> finalSet = new();

        foreach (var channelId in set)
            if (context.Guild?.GetChannel(channelId) is IIzzySocketGuildChannel channel)
                finalSet.Add(channel);

        return finalSet;
    }

    public static HashSet<IIzzySocketGuildChannel> GetChannelSet(Config settings, string key,
        IIzzyContext context)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            var configValue = pinfo.GetValue(settings);
            if (configValue is HashSet<ulong> set
                && set.GetType().IsGenericType
                && set.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(HashSet<>)))
            {
                return ChannelIdToChannel(set, context);
            }
            throw new ArgumentException($"'{key}' is not a HashSet.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static async Task<IIzzySocketGuildChannel?> AddToChannelSet(Config settings, string key,
        string channelResolvable, IIzzyContext context)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            var configValue = pinfo.GetValue(settings);
            if (configValue is HashSet<ulong> set
                && set.GetType().IsGenericType
                && set.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(HashSet<>)))
            {
                if (ParseHelper.TryParseChannelResolvable(channelResolvable, context, out var channelParseError) is not var (channelId, _))
                    throw new MemberAccessException($"Cannot access channel '{channelResolvable}': {channelParseError}");

                var channel = context.Guild?.GetChannel(channelId);

                var result = set.Add(channelId);
                if (!result) throw new ArgumentOutOfRangeException($"'{channelId}' is already present within the HashSet.");

                pinfo.SetValue(settings, set);
                await FileHelper.SaveConfigAsync(settings);
                return channel;
            }
            throw new ArgumentException($"'{key}' is not a HashSet.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static async Task<IIzzySocketGuildChannel?> RemoveFromChannelSet(Config settings, string key,
        string channelResolvable, IIzzyContext context)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            var configValue = pinfo.GetValue(settings);
            if (configValue is HashSet<ulong> set
                && set.GetType().IsGenericType
                && set.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(HashSet<>)))
            {
                if (ParseHelper.TryParseChannelResolvable(channelResolvable, context, out var channelParseError) is not var (channelId, _))
                    throw new MemberAccessException($"Cannot access channel '{channelResolvable}': {channelParseError}");

                var channel = context.Guild?.GetChannel(channelId);

                var result = set.Remove(channelId);
                if (!result) throw new ArgumentOutOfRangeException($"'{channelId}' was not in the HashSet to begin with.");

                pinfo.SetValue(settings, set);
                await FileHelper.SaveConfigAsync(settings);
                return channel;
            }
            throw new ArgumentException($"'{key}' is not a HashSet.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static async Task<ISet<ulong>> ClearChannelSet(Config settings, string key)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            var configValue = pinfo.GetValue(settings);
            if (configValue is ISet<ulong> set
                && set.GetType().IsGenericType
                && set.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(HashSet<>)))
            {
                var value = new HashSet<ulong>(set);
                set.Clear();

                pinfo.SetValue(settings, set);
                await FileHelper.SaveConfigAsync(settings);
                return value;
            }
            throw new ArgumentException($"'{key}' is not a Set.");
        }

        throw new KeyNotFoundException($"Cannot set a nonexistent value ('{key}') from Config!");
    }

    public static IDictionary<string, VALUE> GetDictionary<VALUE>(Config settings, string key)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            if (pinfo.GetValue(settings) is IDictionary<string, VALUE> dict)
            {
                return dict;
            }
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string>.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static bool DoesDictionaryKeyExist<VALUE>(Config settings, string key, string dictionaryKey)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            if (pinfo.GetValue(settings) is IDictionary<string, VALUE> dict)
            {
                return dict.ContainsKey(dictionaryKey);
            }
            return false;
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static async Task<(string, string?, VALUE)> CreateDictionaryKey<VALUE>(Config settings, string key,
        string dictionaryKey, VALUE value)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            if (pinfo.GetValue(settings) is IDictionary<string, VALUE> dict)
            {
                try
                {
                    dict.Add(dictionaryKey, value);

                    pinfo.SetValue(settings, dict);
                    await FileHelper.SaveConfigAsync(settings);
                    return (dictionaryKey, null, value);
                }
                catch (ArgumentException)
                {
                    throw new ArgumentOutOfRangeException($"'{dictionaryKey}' is already present within the Dictionary.");
                }
            }
            throw new ArgumentException($"'{key}' is not a Dictionary.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static async Task<string> RemoveDictionaryKey<VALUE>(Config settings, string key,
        string dictionaryKey)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            if (pinfo.GetValue(settings) is IDictionary<string, VALUE> dict)
            {
                var result = dict.Remove(dictionaryKey);
                if (!result) throw new ArgumentOutOfRangeException($"'{dictionaryKey}' was not in the Dictionary to begin with.");

                pinfo.SetValue(settings, dict);
                await FileHelper.SaveConfigAsync(settings);
                return dictionaryKey;
            }
            throw new ArgumentException($"'{key}' is not a Dictionary.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static VALUE GetDictionaryValue<VALUE>(Config settings, string key, string dictionaryKey)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            if (pinfo.GetValue(settings) is IDictionary<string, VALUE> dict)
            {
                if (!dict.ContainsKey(dictionaryKey)) throw new KeyNotFoundException($"'{dictionaryKey}' is not in '{key}'");

                return dict[dictionaryKey];
            }
            throw new ArgumentException($"'{key}' is not a Dictionary.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static async Task<(string, string?, string)> SetStringDictionaryValue(Config settings, string key,
        string dictionaryKey, string value)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            if (pinfo.GetValue(settings) is IDictionary<string, string> dict)
            {
                if (!dict.TryGetValue(dictionaryKey, out var oldValue)) oldValue = null;

                dict[dictionaryKey] = value;

                pinfo.SetValue(settings, dict);
                await FileHelper.SaveConfigAsync(settings);
                return (dictionaryKey, oldValue, value);
            }
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string>.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static async Task<(string, string?, string?)> SetNullableStringDictionaryValue(Config settings, string key,
        string dictionaryKey, string? value)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            if (pinfo.GetValue(settings) is IDictionary<string, string?> dict)
            {
                if (!dict.TryGetValue(dictionaryKey, out var oldValue)) oldValue = null;

                dict[dictionaryKey] = value;

                pinfo.SetValue(settings, dict);
                await FileHelper.SaveConfigAsync(settings);
                return (dictionaryKey, oldValue, value);
            }
            throw new ArgumentException($"'{key}' is not a Dictionary<string, string?>.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static async Task<(string, string)> CreateStringSetDictionaryKey(Config settings, string key,
        string dictionaryKey, string value)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            if (pinfo.GetValue(settings) is IDictionary<string, HashSet<string>> dict)
            {
                try
                {
                    dict.Add(dictionaryKey, new HashSet<string> { value });

                    pinfo.SetValue(settings, dict);
                    await FileHelper.SaveConfigAsync(settings);
                    return (dictionaryKey, value);
                }
                catch (ArgumentException)
                {
                    throw new ArgumentOutOfRangeException($"'{dictionaryKey}' is already present within the Dictionary.");
                }
            }
            throw new ArgumentException($"'{key}' is not a Dictionary<string, HashSet<string>>.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static async Task<(string, string)> AddToStringSetDictionaryValue(Config settings, string key,
        string dictionaryKey, string value)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            if (pinfo.GetValue(settings) is IDictionary<string, HashSet<string>> dict)
            {
                dict[dictionaryKey].Add(value);

                pinfo.SetValue(settings, dict);
                await FileHelper.SaveConfigAsync(settings);
                return (dictionaryKey, value);
            }
            throw new ArgumentException($"'{key}' is not a Dictionary<string, HashSet<string>>.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }

    public static async Task<(string, string)> RemoveFromStringSetDictionaryValue(Config settings, string key,
        string dictionaryKey, string value)
    {
        if (typeof(Config).GetProperty(key) is PropertyInfo pinfo)
        {
            if (pinfo.GetValue(settings) is IDictionary<string, HashSet<string>> dict)
            {
                var result = dict[dictionaryKey].Remove(value);
                if (!result) throw new ArgumentOutOfRangeException($"'{value}' was not in the Dictionary StringSet to begin with.");

                pinfo.SetValue(settings, dict);
                await FileHelper.SaveConfigAsync(settings);
                return (dictionaryKey, value);
            }
            throw new ArgumentException($"'{key}' is not a Dictionary<string, HashSet<string>>.");
        }

        throw new KeyNotFoundException($"Cannot get a nonexistent value ('{key}') from Config!");
    }
}
