using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Izzy_Moonbot.EventListeners;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Izzy_MoonbotTests.Helper;

[TestClass()]
public class ConfigHelperTests
{
    [TestMethod()]
    public void Config_GetValueTests()
    {
        var cfg = new Config();
        Assert.AreEqual("you all soon", ConfigHelper.GetValue(cfg, "DiscordActivityName"));
        Assert.AreEqual('.', ConfigHelper.GetValue(cfg, "Prefix"));
        Assert.AreEqual(true, ConfigHelper.GetValue(cfg, "ManageNewUserRoles"));
        Assert.AreEqual(100, ConfigHelper.GetValue(cfg, "UnicycleInterval"));
        Assert.IsTrue(ConfigHelper.GetValue(cfg, "FilterIgnoredChannels") is HashSet<ulong>);
        Assert.IsTrue(ConfigHelper.GetValue(cfg, "Aliases") is Dictionary<string, string>);

        Assert.ThrowsException<KeyNotFoundException>(() => ConfigHelper.GetValue(cfg, "foo"));
    }

    [TestMethod()]
    public async Task Config_SetValue_ValidScalars_TestsAsync()
    {
        var cfg = new Config();

        Assert.AreEqual("you all soon", cfg.DiscordActivityName);
        await ConfigHelper.SetStringValue(cfg, "DiscordActivityName", "the hoofball game");
        Assert.AreEqual("the hoofball game", cfg.DiscordActivityName);

        Assert.AreEqual('.', cfg.Prefix);
        await ConfigHelper.SetCharValue(cfg, "Prefix", '!');
        Assert.AreEqual('!', cfg.Prefix);

        Assert.AreEqual(true, cfg.ManageNewUserRoles);
        await ConfigHelper.SetBooleanValue(cfg, "ManageNewUserRoles", "false");
        Assert.AreEqual(false, cfg.ManageNewUserRoles);
        await ConfigHelper.SetBooleanValue(cfg, "ManageNewUserRoles", "y");
        Assert.AreEqual(true, cfg.ManageNewUserRoles);
        await ConfigHelper.SetBooleanValue(cfg, "ManageNewUserRoles", "deactivate");
        Assert.AreEqual(false, cfg.ManageNewUserRoles);
        await ConfigHelper.SetBooleanValue(cfg, "ManageNewUserRoles", "enable");
        Assert.AreEqual(true, cfg.ManageNewUserRoles);

        Assert.AreEqual(100, cfg.UnicycleInterval);
        await ConfigHelper.SetIntValue(cfg, "UnicycleInterval", 42);
        Assert.AreEqual(42, cfg.UnicycleInterval);

        Assert.AreEqual(10.0, cfg.SpamBasePressure);
        await ConfigHelper.SetDoubleValue(cfg, "SpamBasePressure", 0.5);
        Assert.AreEqual(0.5, cfg.SpamBasePressure);

        Assert.AreEqual(ConfigListener.BannerMode.None, cfg.BannerMode);
        await ConfigHelper.SetEnumValue(cfg, "BannerMode", ConfigListener.BannerMode.ManebooruFeatured);
        Assert.AreEqual(ConfigListener.BannerMode.ManebooruFeatured, cfg.BannerMode);
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

        Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.SetStringValue(cfg, "foo", "bar"));
        Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.SetStringValue(cfg, "Aliases", "bar"));

        Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.SetCharValue(cfg, "foo", 'b'));
        Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.SetCharValue(cfg, "Aliases", 'b'));

        Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.SetBooleanValue(cfg, "foo", "bar"));
        Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.SetBooleanValue(cfg, "Aliases", "bar"));

        Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.SetIntValue(cfg, "foo", 42));
        Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.SetIntValue(cfg, "Aliases", 42));

        Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.SetDoubleValue(cfg, "foo", 1.0));
        Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.SetDoubleValue(cfg, "Aliases", 1.0));

        Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.SetEnumValue(cfg, "foo", ConfigListener.BannerMode.ManebooruFeatured));
        Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.SetEnumValue(cfg, "Aliases", ConfigListener.BannerMode.ManebooruFeatured));
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
        AssertListsAreEqual(new List<string>(), ConfigHelper.GetStringList(cfg, "MentionResponses"));

        await ConfigHelper.AddToStringList(cfg, "MentionResponses", "hello there");

        AssertListsAreEqual(new List<string> { "hello there" }, cfg.MentionResponses);
        AssertListsAreEqual(new List<string> { "hello there" }, ConfigHelper.GetStringList(cfg, "MentionResponses"));

        await ConfigHelper.RemoveFromStringList(cfg, "MentionResponses", "hello there");

        AssertListsAreEqual(new List<string>(), cfg.MentionResponses);
        AssertListsAreEqual(new List<string>(), ConfigHelper.GetStringList(cfg, "MentionResponses"));

        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.AddToStringList(cfg, "foo", "bar"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.AddToStringList(cfg, "Aliases", "bar"));

        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => ConfigHelper.AddToBooleanList(cfg, "foo", "bar"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => ConfigHelper.AddToBooleanList(cfg, "Aliases", "bar"));
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
        AssertDictionariesAreEqual(new Dictionary<string, string>(), ConfigHelper.GetStringDictionary(cfg, "Aliases"));
        Assert.IsFalse(ConfigHelper.DoesStringDictionaryKeyExist(cfg, "Aliases", "testalias"));

        await ConfigHelper.CreateStringDictionaryKey(cfg, "Aliases", "testalias", "echo hi");

        AssertDictionariesAreEqual(new Dictionary<string, string> { { "testalias", "echo hi" } }, cfg.Aliases);
        AssertDictionariesAreEqual(new Dictionary<string, string> { { "testalias", "echo hi" } }, ConfigHelper.GetStringDictionary(cfg, "Aliases"));
        Assert.IsTrue(ConfigHelper.DoesStringDictionaryKeyExist(cfg, "Aliases", "testalias"));
        Assert.AreEqual("echo hi", ConfigHelper.GetStringDictionaryValue(cfg, "Aliases", "testalias"));

        await ConfigHelper.SetStringDictionaryValue(cfg, "Aliases", "testalias", "echo belizzle it");

        AssertDictionariesAreEqual(new Dictionary<string, string> { { "testalias", "echo belizzle it" } }, cfg.Aliases);
        AssertDictionariesAreEqual(new Dictionary<string, string> { { "testalias", "echo belizzle it" } }, ConfigHelper.GetStringDictionary(cfg, "Aliases"));
        Assert.IsTrue(ConfigHelper.DoesStringDictionaryKeyExist(cfg, "Aliases", "testalias"));
        Assert.AreEqual("echo belizzle it", ConfigHelper.GetStringDictionaryValue(cfg, "Aliases", "testalias"));

        await ConfigHelper.RemoveStringDictionaryKey(cfg, "Aliases", "testalias");

        AssertDictionariesAreEqual(new Dictionary<string, string>(), cfg.Aliases);
        AssertDictionariesAreEqual(new Dictionary<string, string>(), ConfigHelper.GetStringDictionary(cfg, "Aliases"));
        Assert.IsFalse(ConfigHelper.DoesStringDictionaryKeyExist(cfg, "Aliases", "testalias"));

        Assert.ThrowsException<KeyNotFoundException>(() => ConfigHelper.GetStringDictionary(cfg, "foo"));
        Assert.ThrowsException<ArgumentException>(() => ConfigHelper.GetStringDictionary(cfg, "Prefix"));

        // FilterResponseMessages is the only Dict<string, string?> in Config

        AssertDictionariesAreEqual(new Dictionary<string, string?>(), cfg.Aliases);
        AssertDictionariesAreEqual(new Dictionary<string, string?>(), ConfigHelper.GetNullableStringDictionary(cfg, "FilterResponseMessages"));
        Assert.IsFalse(ConfigHelper.DoesStringDictionaryKeyExist(cfg, "FilterResponseMessages", "spam"));

        await ConfigHelper.CreateStringDictionaryKey(cfg, "FilterResponseMessages", "spam", "this is a ham server");

        AssertDictionariesAreEqual(new Dictionary<string, string?> { { "spam", "this is a ham server" } }, cfg.FilterResponseMessages);
        AssertDictionariesAreEqual(new Dictionary<string, string?> { { "spam", "this is a ham server" } }, ConfigHelper.GetNullableStringDictionary(cfg, "FilterResponseMessages"));
        Assert.IsTrue(ConfigHelper.DoesNullableStringDictionaryKeyExist(cfg, "FilterResponseMessages", "spam"));
        Assert.AreEqual("this is a ham server", ConfigHelper.GetNullableStringDictionaryValue(cfg, "FilterResponseMessages", "spam"));

        await ConfigHelper.SetStringDictionaryValue(cfg, "FilterResponseMessages", "spam", "begone spambots");

        AssertDictionariesAreEqual(new Dictionary<string, string?> { { "spam", "begone spambots" } }, cfg.FilterResponseMessages);
        AssertDictionariesAreEqual(new Dictionary<string, string?> { { "spam", "begone spambots" } }, ConfigHelper.GetNullableStringDictionary(cfg, "FilterResponseMessages"));
        Assert.IsTrue(ConfigHelper.DoesNullableStringDictionaryKeyExist(cfg, "FilterResponseMessages", "spam"));
        Assert.AreEqual("begone spambots", ConfigHelper.GetNullableStringDictionaryValue(cfg, "FilterResponseMessages", "spam"));

        await ConfigHelper.RemoveNullableStringDictionaryKey(cfg, "FilterResponseMessages", "spam");

        AssertDictionariesAreEqual(new Dictionary<string, string?>(), cfg.FilterResponseMessages);
        AssertDictionariesAreEqual(new Dictionary<string, string?>(), ConfigHelper.GetNullableStringDictionary(cfg, "FilterResponseMessages"));
        Assert.IsFalse(ConfigHelper.DoesNullableStringDictionaryKeyExist(cfg, "FilterResponseMessages", "spam"));

        Assert.ThrowsException<KeyNotFoundException>(() => ConfigHelper.GetNullableStringDictionary(cfg, "foo"));
        Assert.ThrowsException<ArgumentException>(() => ConfigHelper.GetNullableStringDictionary(cfg, "Prefix"));

        // FilterResponseDelete and FilterResponseSilence are the only Dict<string, bool>s in Config

        AssertDictionariesAreEqual(new Dictionary<string, bool>(), cfg.FilterResponseDelete);
        AssertDictionariesAreEqual(new Dictionary<string, bool>(), ConfigHelper.GetBooleanDictionary(cfg, "FilterResponseDelete"));
        Assert.IsFalse(ConfigHelper.DoesStringDictionaryKeyExist(cfg, "FilterResponseDelete", "spam"));

        await ConfigHelper.CreateBooleanDictionaryKey(cfg, "FilterResponseDelete", "spam", "true");

        AssertDictionariesAreEqual(new Dictionary<string, bool> { { "spam", true } }, cfg.FilterResponseDelete);
        AssertDictionariesAreEqual(new Dictionary<string, bool> { { "spam", true } }, ConfigHelper.GetBooleanDictionary(cfg, "FilterResponseDelete"));
        Assert.IsTrue(ConfigHelper.DoesBooleanDictionaryKeyExist(cfg, "FilterResponseDelete", "spam"));
        Assert.IsTrue(ConfigHelper.GetBooleanDictionaryValue(cfg, "FilterResponseDelete", "spam"));

        await ConfigHelper.SetBooleanDictionaryValue(cfg, "FilterResponseDelete", "spam", "false");

        AssertDictionariesAreEqual(new Dictionary<string, bool> { { "spam", false } }, cfg.FilterResponseDelete);
        AssertDictionariesAreEqual(new Dictionary<string, bool> { { "spam", false } }, ConfigHelper.GetBooleanDictionary(cfg, "FilterResponseDelete"));
        Assert.IsTrue(ConfigHelper.DoesBooleanDictionaryKeyExist(cfg, "FilterResponseDelete", "spam"));
        Assert.IsFalse(ConfigHelper.GetBooleanDictionaryValue(cfg, "FilterResponseDelete", "spam"));

        await ConfigHelper.RemoveBooleanDictionaryKey(cfg, "FilterResponseDelete", "spam");

        AssertDictionariesAreEqual(new Dictionary<string, bool>(), cfg.FilterResponseDelete);
        AssertDictionariesAreEqual(new Dictionary<string, bool>(), ConfigHelper.GetBooleanDictionary(cfg, "FilterResponseDelete"));
        Assert.IsFalse(ConfigHelper.DoesBooleanDictionaryKeyExist(cfg, "FilterResponseDelete", "spam"));

        Assert.ThrowsException<KeyNotFoundException>(() => ConfigHelper.GetBooleanDictionary(cfg, "foo"));
        Assert.ThrowsException<ArgumentException>(() => ConfigHelper.GetBooleanDictionary(cfg, "Prefix"));
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
        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>>(), ConfigHelper.GetStringListDictionary(cfg, "FilteredWords"));
        Assert.IsFalse(ConfigHelper.DoesStringListDictionaryKeyExist(cfg, "FilteredWords", "jinxies"));

        await ConfigHelper.CreateStringListDictionaryKey(cfg, "FilteredWords", "jinxies", "mayonnaise");

        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>> { { "jinxies", new List<string> { "mayonnaise" } } }, cfg.FilteredWords);
        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>> { { "jinxies", new List<string> { "mayonnaise" } } }, ConfigHelper.GetStringListDictionary(cfg, "FilteredWords"));
        Assert.IsTrue(ConfigHelper.DoesStringListDictionaryKeyExist(cfg, "FilteredWords", "jinxies"));
        AssertListsAreEqual(new List<string> { "mayonnaise" }, ConfigHelper.GetStringListDictionaryValue(cfg, "FilteredWords", "jinxies"));

        await ConfigHelper.AddToStringListDictionaryValue(cfg, "FilteredWords", "jinxies", "magic");
        await ConfigHelper.AddToStringListDictionaryValue(cfg, "FilteredWords", "jinxies", "wing");
        await ConfigHelper.AddToStringListDictionaryValue(cfg, "FilteredWords", "jinxies", "feather");

        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>> { { "jinxies", new List<string> { "mayonnaise", "magic", "wing", "feather" } } }, cfg.FilteredWords);
        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>> { { "jinxies", new List<string> { "mayonnaise", "magic", "wing", "feather" } } }, ConfigHelper.GetStringListDictionary(cfg, "FilteredWords"));
        Assert.IsTrue(ConfigHelper.DoesStringListDictionaryKeyExist(cfg, "FilteredWords", "jinxies"));
        AssertListsAreEqual(new List<string> { "mayonnaise", "magic", "wing", "feather" }, ConfigHelper.GetStringListDictionaryValue(cfg, "FilteredWords", "jinxies"));

        await ConfigHelper.RemoveFromStringListDictionaryValue(cfg, "FilteredWords", "jinxies", "mayonnaise");
        await ConfigHelper.RemoveFromStringListDictionaryValue(cfg, "FilteredWords", "jinxies", "magic");

        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>> { { "jinxies", new List<string> { "wing", "feather" } } }, cfg.FilteredWords);
        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>> { { "jinxies", new List<string> { "wing", "feather" } } }, ConfigHelper.GetStringListDictionary(cfg, "FilteredWords"));
        Assert.IsTrue(ConfigHelper.DoesStringListDictionaryKeyExist(cfg, "FilteredWords", "jinxies"));
        AssertListsAreEqual(new List<string> { "wing", "feather" }, ConfigHelper.GetStringListDictionaryValue(cfg, "FilteredWords", "jinxies"));

        await ConfigHelper.RemoveStringListDictionaryKey(cfg, "FilteredWords", "jinxies");

        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>>(), cfg.FilteredWords);
        AssertDictsOfListsAreEqual(new Dictionary<string, List<string>>(), ConfigHelper.GetStringListDictionary(cfg, "FilteredWords"));
        Assert.IsFalse(ConfigHelper.DoesStringListDictionaryKeyExist(cfg, "FilteredWords", "jinxies"));

        Assert.ThrowsException<KeyNotFoundException>(() => ConfigHelper.GetStringListDictionary(cfg, "foo"));
        Assert.ThrowsException<ArgumentException>(() => ConfigHelper.GetStringListDictionary(cfg, "Prefix"));
    }
}