
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Describers;
using Izzy_Moonbot.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Izzy_Moonbot_Tests;

// This file is for any test-related code that we want all the
// other test files to share. Usually test double factories.

public static class TestUtils
{
    public static ulong DefaultGuildId = 901786293531447308;

    public static (Config, ConfigDescriber, (TestUser, TestUser), List<TestRole>, (StubChannel, StubChannel, StubChannel), StubGuild, StubClient) DefaultStubs()
    {
        var izzyHerself = new TestUser("Izzy Moonbot", 1);
        var sunny = new TestUser("Sunny", 2);
        var users = new List<TestUser> { izzyHerself, sunny };

        var roles = new List<TestRole> { new TestRole("Alicorn", 1), new TestRole("Pegasus", 2) };

        var generalChannel = new StubChannel(1, "general");
        var modChat = new StubChannel(2, "modchat");
        var logChat = new StubChannel(3, "botlogs");
        var channels = new List<StubChannel> { generalChannel, modChat, logChat };

        var guild = new StubGuild(901786293531447308, "Maretime Bay", roles, users, channels);
        var client = new StubClient(izzyHerself, new List<StubGuild> { guild });

        var cfg = new Config();
        var cd = new ConfigDescriber();

        return (cfg, cd, (izzyHerself, sunny), roles, (generalChannel, modChat, logChat), guild, client);
    }

    // The built-in Assert.AreEqual and CollectionsAssert.AreEqual have error messages so bad it was worth writing my own asserts
    public static void AssertListsAreEqual<T>(IList<T>? expected, IList<T>? actual, string message = "")
    {
        if (expected is null || actual is null)
        {
            Assert.AreEqual(expected, actual);
            return;
        }
        if (expected.Count() != actual.Count())
            Assert.AreEqual(expected, actual, $"\nCount() mismatch: {expected.Count()} != {actual.Count()}");
        foreach (var i in Enumerable.Range(0, expected.Count()))
            Assert.AreEqual(expected[i], actual[i], $"\nItem {i}" + message);
    }

    public static void AssertSetsAreEqual<T>(ISet<T>? expected, ISet<T>? actual, string message = "")
    {
        if (expected is null || actual is null)
        {
            Assert.AreEqual(expected, actual);
            return;
        }
        if (expected.Count() != actual.Count())
            Assert.AreEqual(expected, actual, $"\nCount() mismatch: {expected.Count()} != {actual.Count()}");
        foreach (var value in expected)
            Assert.IsTrue(actual.Contains(value), $"\nValue {value}" + message);
    }

    // The built-in Assert.AreEqual and CollectionsAssert.AreEqual don't even work on Dictionaries, so everyone has to write their own
    public static void AssertDictionariesAreEqual<K, V>(IDictionary<K, V>? expected, IDictionary<K, V>? actual, string message = "")
    {
        if (expected is null || actual is null)
        {
            Assert.AreEqual(expected, actual);
            return;
        }
        AssertListsAreEqual(
            expected.OrderBy(kv => kv.Key).ToList(),
            actual.OrderBy(kv => kv.Key).ToList()
        );
    }

    // even my AssertDictionariesAreEqual helper falls apart on Set values
    public static void AssertDictsOfSetsAreEqual<K, V>(IDictionary<K, HashSet<V>>? expected, IDictionary<K, HashSet<V>>? actual, string message = "")
    {
        if (expected is null || actual is null)
        {
            Assert.AreEqual(expected, actual);
            return;
        }
        if (expected.Count() != actual.Count())
            Assert.AreEqual(expected, actual, $"\nCount() mismatch: {expected.Count()} != {actual.Count()}");
        foreach (var kv in expected)
        {
            AssertSetsAreEqual(expected[kv.Key], actual[kv.Key], $"\nKey {kv.Key}" + message);
        }
    }
}
