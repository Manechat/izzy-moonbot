using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Izzy_MoonbotTests.Helper;

[TestClass()]
public class ConfigHelperTests
{
    [TestMethod()]
    public void Config_GetValueTests()
    {
        var cfg = new Config();
        Assert.AreEqual("you all soon", ConfigHelper.GetValue<Config>(cfg, "DiscordActivityName"));
        Assert.AreEqual('.', ConfigHelper.GetValue<Config>(cfg, "Prefix"));
        Assert.AreEqual(true, ConfigHelper.GetValue<Config>(cfg, "ManageNewUserRoles"));
        Assert.AreEqual(100, ConfigHelper.GetValue<Config>(cfg, "UnicycleInterval"));
        Assert.IsTrue(ConfigHelper.GetValue<Config>(cfg, "FilterIgnoredChannels") is HashSet<ulong>);
        Assert.IsTrue(ConfigHelper.GetValue<Config>(cfg, "Aliases") is Dictionary<string, string>);

        Assert.ThrowsException<KeyNotFoundException>(() => ConfigHelper.GetValue<Config>(cfg, "foo"));
    }

    [TestMethod()]
    public async Task Config_SetValue_ValidScalars_TestsAsync()
    {
        var cfg = new Config();

        Assert.AreEqual("you all soon", cfg.DiscordActivityName);
        await ConfigHelper.SetStringValue<Config>(cfg, "DiscordActivityName", "the hoofball game");
        Assert.AreEqual("the hoofball game", cfg.DiscordActivityName);

        Assert.AreEqual('.', cfg.Prefix);
        await ConfigHelper.SetCharValue<Config>(cfg, "Prefix", '!');
        Assert.AreEqual('!', cfg.Prefix);

        Assert.AreEqual(true, cfg.ManageNewUserRoles);
        await ConfigHelper.SetBooleanValue<Config>(cfg, "ManageNewUserRoles", "false");
        Assert.AreEqual(false, cfg.ManageNewUserRoles);
        await ConfigHelper.SetBooleanValue<Config>(cfg, "ManageNewUserRoles", "y");
        Assert.AreEqual(true, cfg.ManageNewUserRoles);
        await ConfigHelper.SetBooleanValue<Config>(cfg, "ManageNewUserRoles", "deactivate");
        Assert.AreEqual(false, cfg.ManageNewUserRoles);
        await ConfigHelper.SetBooleanValue<Config>(cfg, "ManageNewUserRoles", "enable");
        Assert.AreEqual(true, cfg.ManageNewUserRoles);

        Assert.AreEqual(100, cfg.UnicycleInterval);
        await ConfigHelper.SetIntValue<Config>(cfg, "UnicycleInterval", 42);
        Assert.AreEqual(42, cfg.UnicycleInterval);

        Assert.AreEqual(10.0, cfg.SpamBasePressure);
        await ConfigHelper.SetDoubleValue<Config>(cfg, "SpamBasePressure", 0.5);
        Assert.AreEqual(0.5, cfg.SpamBasePressure);
    }

    // TODO: figure out Discord.NET test doubles to enable testing users, roles, channels, etc
    /*[TestMethod()]
    public async Task Config_SetValue_ValidDiscordEntitiesTestsAsync()
    {
    }*/

    [TestMethod()]
    public void Config_SetValue_InvalidValues_Tests()
    {
        var cfg = new Config();

        Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.SetStringValue<Config>(cfg, "foo", "bar"));
        Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.SetStringValue<Config>(cfg, "Aliases", "bar"));

        Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.SetCharValue<Config>(cfg, "foo", 'b'));
        Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.SetCharValue<Config>(cfg, "Aliases", 'b'));

        Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.SetBooleanValue<Config>(cfg, "foo", "bar"));
        Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.SetBooleanValue<Config>(cfg, "Aliases", "bar"));

        Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.SetIntValue<Config>(cfg, "foo", 42));
        Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.SetIntValue<Config>(cfg, "Aliases", 42));

        Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.SetDoubleValue<Config>(cfg, "foo", 1.0));
        Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.SetDoubleValue<Config>(cfg, "Aliases", 1.0));
    }

    // The built-in Assert.AreEqual and CollectionsAssert.AreEqual have error messages so bad it was worth writing my own asserts
    void AssertListsAreEqual<T>(IList<T>? expected, IList<T>? actual, string message = "")
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

    [TestMethod()]
    public async Task Config_Lists_TestsAsync()
    {
        var cfg = new Config();

        // Apparently we have only one List in Config: MentionResponses
        // So only the *StringList methods are non-dead and have a testable success path

        // Also don't test HasValueInList because it's both broken and dead

        AssertListsAreEqual(new List<string>(), cfg.MentionResponses);
        AssertListsAreEqual(new List<string>(), ConfigHelper.GetStringList<Config>(cfg, "MentionResponses"));

        await ConfigHelper.AddToStringList<Config>(cfg, "MentionResponses", "hello there");

        AssertListsAreEqual(new List<string> { "hello there" }, cfg.MentionResponses);
        AssertListsAreEqual(new List<string> { "hello there" }, ConfigHelper.GetStringList<Config>(cfg, "MentionResponses"));

        await ConfigHelper.RemoveFromStringList<Config>(cfg, "MentionResponses", "hello there");

        AssertListsAreEqual(new List<string>(), cfg.MentionResponses);
        AssertListsAreEqual(new List<string>(), ConfigHelper.GetStringList<Config>(cfg, "MentionResponses"));

        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.AddToStringList<Config>(cfg, "foo", "bar"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.AddToStringList<Config>(cfg, "Aliases", "bar"));

        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.AddToBooleanList<Config>(cfg, "foo", "bar"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.AddToBooleanList<Config>(cfg, "Aliases", "bar"));
    }

    // The built-in Assert.AreEqual and CollectionsAssert.AreEqual don't even work on Dictionaries, so everyone has to write their own
    void AssertDictionariesAreEqual<K, V>(IDictionary<K, V>? expected, IDictionary<K, V>? actual, string message = "")
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

    [TestMethod()]
    public async Task Config_DictionariesOfScalars_TestsAsync()
    {
        var cfg = new Config();

        // Aliases is the only Dict<string, string> in Config

        AssertDictionariesAreEqual(new Dictionary<string, string>(), cfg.Aliases);
        AssertDictionariesAreEqual(new Dictionary<string, string>(), ConfigHelper.GetStringDictionary<Config>(cfg, "Aliases"));
        Assert.IsFalse(ConfigHelper.DoesStringDictionaryKeyExist<Config>(cfg, "Aliases", "testalias"));

        await ConfigHelper.CreateStringDictionaryKey<Config>(cfg, "Aliases", "testalias", "echo hi");

        AssertDictionariesAreEqual(new Dictionary<string, string> { { "testalias", "echo hi" } }, cfg.Aliases);
        AssertDictionariesAreEqual(new Dictionary<string, string> { { "testalias", "echo hi" } }, ConfigHelper.GetStringDictionary<Config>(cfg, "Aliases"));
        Assert.IsTrue(ConfigHelper.DoesStringDictionaryKeyExist<Config>(cfg, "Aliases", "testalias"));
        Assert.AreEqual("echo hi", ConfigHelper.GetStringDictionaryValue<Config>(cfg, "Aliases", "testalias"));

        await ConfigHelper.SetStringDictionaryValue<Config>(cfg, "Aliases", "testalias", "echo belizzle it");

        AssertDictionariesAreEqual(new Dictionary<string, string> { { "testalias", "echo belizzle it" } }, cfg.Aliases);
        AssertDictionariesAreEqual(new Dictionary<string, string> { { "testalias", "echo belizzle it" } }, ConfigHelper.GetStringDictionary<Config>(cfg, "Aliases"));
        Assert.IsTrue(ConfigHelper.DoesStringDictionaryKeyExist<Config>(cfg, "Aliases", "testalias"));
        Assert.AreEqual("echo belizzle it", ConfigHelper.GetStringDictionaryValue<Config>(cfg, "Aliases", "testalias"));

        await ConfigHelper.RemoveStringDictionaryKey<Config>(cfg, "Aliases", "testalias");

        AssertDictionariesAreEqual(new Dictionary<string, string>(), cfg.Aliases);
        AssertDictionariesAreEqual(new Dictionary<string, string>(), ConfigHelper.GetStringDictionary<Config>(cfg, "Aliases"));
        Assert.IsFalse(ConfigHelper.DoesStringDictionaryKeyExist<Config>(cfg, "Aliases", "testalias"));

        Assert.ThrowsException<KeyNotFoundException>(() => ConfigHelper.GetStringDictionary<Config>(cfg, "foo"));
        Assert.ThrowsException<ArgumentException>(() => ConfigHelper.GetStringDictionary<Config>(cfg, "Prefix"));

        // FilterResponseMessages is the only Dict<string, string?> in Config

        AssertDictionariesAreEqual(new Dictionary<string, string?>(), cfg.Aliases);
        AssertDictionariesAreEqual(new Dictionary<string, string?>(), ConfigHelper.GetNullableStringDictionary<Config>(cfg, "FilterResponseMessages"));
        Assert.IsFalse(ConfigHelper.DoesStringDictionaryKeyExist<Config>(cfg, "FilterResponseMessages", "spam"));

        await ConfigHelper.CreateStringDictionaryKey<Config>(cfg, "FilterResponseMessages", "spam", "this is a ham server");

        AssertDictionariesAreEqual(new Dictionary<string, string?> { { "spam", "this is a ham server" } }, cfg.FilterResponseMessages);
        AssertDictionariesAreEqual(new Dictionary<string, string?> { { "spam", "this is a ham server" } }, ConfigHelper.GetNullableStringDictionary<Config>(cfg, "FilterResponseMessages"));
        Assert.IsTrue(ConfigHelper.DoesNullableStringDictionaryKeyExist<Config>(cfg, "FilterResponseMessages", "spam"));
        Assert.AreEqual("this is a ham server", ConfigHelper.GetNullableStringDictionaryValue<Config>(cfg, "FilterResponseMessages", "spam"));

        await ConfigHelper.SetStringDictionaryValue<Config>(cfg, "FilterResponseMessages", "spam", "begone spambots");

        AssertDictionariesAreEqual(new Dictionary<string, string?> { { "spam", "begone spambots" } }, cfg.FilterResponseMessages);
        AssertDictionariesAreEqual(new Dictionary<string, string?> { { "spam", "begone spambots" } }, ConfigHelper.GetNullableStringDictionary<Config>(cfg, "FilterResponseMessages"));
        Assert.IsTrue(ConfigHelper.DoesNullableStringDictionaryKeyExist<Config>(cfg, "FilterResponseMessages", "spam"));
        Assert.AreEqual("begone spambots", ConfigHelper.GetNullableStringDictionaryValue<Config>(cfg, "FilterResponseMessages", "spam"));

        await ConfigHelper.RemoveNullableStringDictionaryKey<Config>(cfg, "FilterResponseMessages", "spam");

        AssertDictionariesAreEqual(new Dictionary<string, string?>(), cfg.FilterResponseMessages);
        AssertDictionariesAreEqual(new Dictionary<string, string?>(), ConfigHelper.GetNullableStringDictionary<Config>(cfg, "FilterResponseMessages"));
        Assert.IsFalse(ConfigHelper.DoesNullableStringDictionaryKeyExist<Config>(cfg, "FilterResponseMessages", "spam"));

        Assert.ThrowsException<KeyNotFoundException>(() => ConfigHelper.GetNullableStringDictionary<Config>(cfg, "foo"));
        Assert.ThrowsException<ArgumentException>(() => ConfigHelper.GetNullableStringDictionary<Config>(cfg, "Prefix"));

        // FilterResponseDelete and FilterResponseSilence are the only Dict<string, bool>s in Config

        AssertDictionariesAreEqual(new Dictionary<string, bool>(), cfg.FilterResponseDelete);
        AssertDictionariesAreEqual(new Dictionary<string, bool>(), ConfigHelper.GetBooleanDictionary<Config>(cfg, "FilterResponseDelete"));
        Assert.IsFalse(ConfigHelper.DoesStringDictionaryKeyExist<Config>(cfg, "FilterResponseDelete", "spam"));

        await ConfigHelper.CreateBooleanDictionaryKey<Config>(cfg, "FilterResponseDelete", "spam", "true");

        AssertDictionariesAreEqual(new Dictionary<string, bool> { { "spam", true } }, cfg.FilterResponseDelete);
        AssertDictionariesAreEqual(new Dictionary<string, bool> { { "spam", true } }, ConfigHelper.GetBooleanDictionary<Config>(cfg, "FilterResponseDelete"));
        Assert.IsTrue(ConfigHelper.DoesBooleanDictionaryKeyExist<Config>(cfg, "FilterResponseDelete", "spam"));
        Assert.IsTrue(ConfigHelper.GetBooleanDictionaryValue<Config>(cfg, "FilterResponseDelete", "spam"));

        await ConfigHelper.SetBooleanDictionaryValue<Config>(cfg, "FilterResponseDelete", "spam", "false");

        AssertDictionariesAreEqual(new Dictionary<string, bool> { { "spam", false } }, cfg.FilterResponseDelete);
        AssertDictionariesAreEqual(new Dictionary<string, bool> { { "spam", false } }, ConfigHelper.GetBooleanDictionary<Config>(cfg, "FilterResponseDelete"));
        Assert.IsTrue(ConfigHelper.DoesBooleanDictionaryKeyExist<Config>(cfg, "FilterResponseDelete", "spam"));
        Assert.IsFalse(ConfigHelper.GetBooleanDictionaryValue<Config>(cfg, "FilterResponseDelete", "spam"));

        await ConfigHelper.RemoveBooleanDictionaryKey<Config>(cfg, "FilterResponseDelete", "spam");

        AssertDictionariesAreEqual(new Dictionary<string, bool>(), cfg.FilterResponseDelete);
        AssertDictionariesAreEqual(new Dictionary<string, bool>(), ConfigHelper.GetBooleanDictionary<Config>(cfg, "FilterResponseDelete"));
        Assert.IsFalse(ConfigHelper.DoesBooleanDictionaryKeyExist<Config>(cfg, "FilterResponseDelete", "spam"));

        Assert.ThrowsException<KeyNotFoundException>(() => ConfigHelper.GetBooleanDictionary<Config>(cfg, "foo"));
        Assert.ThrowsException<ArgumentException>(() => ConfigHelper.GetBooleanDictionary<Config>(cfg, "Prefix"));
    }

    // even my AssertDictionariesAreEqual helper falls apart on List values
    void AssertDictsOfListsAreEqual<K, V>(IDictionary<K, List<V>>? expected, IDictionary<K, List<V>>? actual, string message = "")
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
            AssertListsAreEqual(expected[kv.Key], actual[kv.Key], $"\nKey {kv.Key}" + message);
        }
    }

    [TestMethod()]
    public async Task Config_DictionariesOfLists_TestsAsync()
    {
        var cfg = new Config();

        // FilteredWords is the only Dict<string, List<>> in Config

        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>>(), cfg.FilteredWords);
        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>>(), ConfigHelper.GetStringListDictionary<Config>(cfg, "FilteredWords"));
        Assert.IsFalse(ConfigHelper.DoesStringListDictionaryKeyExist<Config>(cfg, "FilteredWords", "jinxies"));

        await ConfigHelper.CreateStringListDictionaryKey<Config>(cfg, "FilteredWords", "jinxies", "mayonnaise");

        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>> { { "jinxies", new List<string> { "mayonnaise" } } }, cfg.FilteredWords);
        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>> { { "jinxies", new List<string> { "mayonnaise" } } }, ConfigHelper.GetStringListDictionary<Config>(cfg, "FilteredWords"));
        Assert.IsTrue(ConfigHelper.DoesStringListDictionaryKeyExist<Config>(cfg, "FilteredWords", "jinxies"));
        AssertListsAreEqual(new List<string> { "mayonnaise" }, ConfigHelper.GetStringListDictionaryValue<Config>(cfg, "FilteredWords", "jinxies"));

        await ConfigHelper.AddToStringListDictionaryValue<Config>(cfg, "FilteredWords", "jinxies", "magic");
        await ConfigHelper.AddToStringListDictionaryValue<Config>(cfg, "FilteredWords", "jinxies", "wing");
        await ConfigHelper.AddToStringListDictionaryValue<Config>(cfg, "FilteredWords", "jinxies", "feather");

        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>> { { "jinxies", new List<string> { "mayonnaise", "magic", "wing", "feather" } } }, cfg.FilteredWords);
        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>> { { "jinxies", new List<string> { "mayonnaise", "magic", "wing", "feather" } } }, ConfigHelper.GetStringListDictionary<Config>(cfg, "FilteredWords"));
        Assert.IsTrue(ConfigHelper.DoesStringListDictionaryKeyExist<Config>(cfg, "FilteredWords", "jinxies"));
        AssertListsAreEqual(new List<string> { "mayonnaise", "magic", "wing", "feather" }, ConfigHelper.GetStringListDictionaryValue<Config>(cfg, "FilteredWords", "jinxies"));

        await ConfigHelper.RemoveFromStringListDictionaryValue<Config>(cfg, "FilteredWords", "jinxies", "mayonnaise");
        await ConfigHelper.RemoveFromStringListDictionaryValue<Config>(cfg, "FilteredWords", "jinxies", "magic");

        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>> { { "jinxies", new List<string> { "wing", "feather" } } }, cfg.FilteredWords);
        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>> { { "jinxies", new List<string> { "wing", "feather" } } }, ConfigHelper.GetStringListDictionary<Config>(cfg, "FilteredWords"));
        Assert.IsTrue(ConfigHelper.DoesStringListDictionaryKeyExist<Config>(cfg, "FilteredWords", "jinxies"));
        AssertListsAreEqual(new List<string> { "wing", "feather" }, ConfigHelper.GetStringListDictionaryValue<Config>(cfg, "FilteredWords", "jinxies"));

        await ConfigHelper.RemoveStringListDictionaryKey<Config>(cfg, "FilteredWords", "jinxies");

        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>>(), cfg.FilteredWords);
        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>>(), ConfigHelper.GetStringListDictionary<Config>(cfg, "FilteredWords"));
        Assert.IsFalse(ConfigHelper.DoesStringListDictionaryKeyExist<Config>(cfg, "FilteredWords", "jinxies"));

        Assert.ThrowsException<KeyNotFoundException>(() => ConfigHelper.GetStringListDictionary<Config>(cfg, "foo"));
        Assert.ThrowsException<ArgumentException>(() => ConfigHelper.GetStringListDictionary<Config>(cfg, "Prefix"));
    }
}