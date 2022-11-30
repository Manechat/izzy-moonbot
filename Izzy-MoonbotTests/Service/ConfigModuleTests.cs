using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Describers;
using Izzy_Moonbot.Modules;
using Izzy_Moonbot.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Izzy_Moonbot.EventListeners.ConfigListener;

namespace Izzy_MoonbotTests.Service;

[TestClass()]
public class ConfigModuleTests
{
    [TestMethod()]
    public async Task ConfigCommand_BreathingTestsAsync()
    {
        var izzyHerself = new TestUser("Izzy Moonbot", 1);
        var sunny = new TestUser("Sunny", 2);
        var users = new List<TestUser> { izzyHerself, sunny };

        var roles = new List<TestRole> { new TestRole("Alicorn", 1) };

        var generalChannel = new StubChannel(1, "general");
        var channels = new List<StubChannel> { generalChannel };

        var guild = new StubGuild(1, roles, users, channels);
        var client = new StubClient(izzyHerself, new List<StubGuild> { guild });

        var cfg = new Config();
        var cd = new ConfigDescriber();

        // post ".config prefix *", mis-capitalized on purpose

        var context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config prefix *");

        Assert.AreEqual(cfg.Prefix, '.');
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "prefix", "*");
        Assert.AreEqual(cfg.Prefix, '.');

        Assert.AreEqual(2, generalChannel.Messages.Count);

        Assert.AreEqual(sunny.Id, generalChannel.Messages[0].AuthorId);
        Assert.AreEqual(".config prefix *", generalChannel.Messages[0].Content);

        Assert.AreEqual(izzyHerself.Id, generalChannel.Messages[1].AuthorId);
        Assert.AreEqual("Sorry, I couldn't find a config value or category called `prefix`!", generalChannel.Messages[1].Content);

        // post ".config Prefix *"

        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config Prefix *");

        Assert.AreEqual(cfg.Prefix, '.');
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "Prefix", "*");
        Assert.AreEqual(cfg.Prefix, '*');

        Assert.AreEqual(4, generalChannel.Messages.Count);

        Assert.AreEqual(sunny.Id, generalChannel.Messages[0].AuthorId);
        Assert.AreEqual(".config prefix *", generalChannel.Messages[0].Content);

        Assert.AreEqual(izzyHerself.Id, generalChannel.Messages[1].AuthorId);
        Assert.AreEqual("Sorry, I couldn't find a config value or category called `prefix`!", generalChannel.Messages[1].Content);

        Assert.AreEqual(sunny.Id, generalChannel.Messages[2].AuthorId);
        Assert.AreEqual(".config Prefix *", generalChannel.Messages[2].Content);

        Assert.AreEqual(izzyHerself.Id, generalChannel.Messages[3].AuthorId);
        Assert.AreEqual("I've set `Prefix` to the following content: *", generalChannel.Messages[3].Content);
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

    void AssertSetsAreEqual<T>(ISet<T>? expected, ISet<T>? actual, string message = "")
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

    // even my AssertDictionariesAreEqual helper falls apart on Set values
    void AssertDictsOfSetsAreEqual<K, V>(IDictionary<K, HashSet<V>>? expected, IDictionary<K, HashSet<V>>? actual, string message = "")
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

    [TestMethod()]
    public async Task ConfigCommand_TestEditingEveryConfigItemAsync()
    {
        var izzyHerself = new TestUser("Izzy Moonbot", 1);
        var sunny = new TestUser("Sunny", 2);
        var users = new List<TestUser> { izzyHerself, sunny };

        var alicorn = new TestRole("Alicorn", 1);
        var pony = new TestRole("Pony", 2);
        var newPony = new TestRole("New Pony", 3);
        var roles = new List<TestRole> { alicorn, pony, newPony };

        var generalChannel = new StubChannel(1, "general");
        var modChannel = new StubChannel(2, "modchat");
        var logChannel = new StubChannel(3, "bot-logs");
        var channels = new List<StubChannel> { generalChannel, modChannel, logChannel };

        var guild = new StubGuild(1, roles, users, channels);
        var client = new StubClient(izzyHerself, new List<StubGuild> { guild });

        var cfg = new Config();
        var cd = new ConfigDescriber();


        // post ".config Prefix *"
        Assert.AreEqual(cfg.Prefix, '.');
        var context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config prefix *");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "Prefix", "*");
        Assert.AreEqual(cfg.Prefix, '*');
        Assert.AreEqual("I've set `Prefix` to the following content: *", generalChannel.Messages.Last().Content);

        // post ".config UnicycleInterval 42"
        Assert.AreEqual(cfg.UnicycleInterval, 100);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config UnicycleInterval 42");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "UnicycleInterval", "42");
        Assert.AreEqual(cfg.UnicycleInterval, 42);
        Assert.AreEqual("I've set `UnicycleInterval` to the following content: 42", generalChannel.Messages.Last().Content);

        // post ".config SafeMode false"
        Assert.AreEqual(cfg.SafeMode, true);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config SafeMode false");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "SafeMode", "false");
        Assert.AreEqual(cfg.SafeMode, false);
        Assert.AreEqual("I've set `SafeMode` to the following content: False", generalChannel.Messages.Last().Content);

        // post ".config BatchSendLogs true"
        Assert.AreEqual(cfg.BatchSendLogs, false);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config BatchSendLogs true");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "BatchSendLogs", "true");
        Assert.AreEqual(cfg.BatchSendLogs, true);
        Assert.AreEqual("I've set `BatchSendLogs` to the following content: True", generalChannel.Messages.Last().Content);

        // post ".config BatchLogsSendRate 999"
        Assert.AreEqual(cfg.BatchLogsSendRate, 10);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config BatchLogsSendRate 999");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "BatchLogsSendRate", "999");
        Assert.AreEqual(cfg.BatchLogsSendRate, 999);
        Assert.AreEqual("I've set `BatchLogsSendRate` to the following content: 999", generalChannel.Messages.Last().Content);

        // post ".config MentionResponseEnabled true"
        Assert.AreEqual(cfg.MentionResponseEnabled, false);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config MentionResponseEnabled true");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "MentionResponseEnabled", "true");
        Assert.AreEqual(cfg.MentionResponseEnabled, true);
        Assert.AreEqual("I've set `MentionResponseEnabled` to the following content: True", generalChannel.Messages.Last().Content);

        // post ".config MentionResponses add yes i am bot"
        AssertSetsAreEqual(new HashSet<string>(), cfg.MentionResponses);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config MentionResponses add yes i am bot");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "MentionResponses", "add yes i am bot");
        AssertSetsAreEqual(new HashSet<string> { "yes i am bot" }, cfg.MentionResponses);
        Assert.AreEqual($"I added the following content to the `MentionResponses` string list:{Environment.NewLine}" +
            $"```{Environment.NewLine}" +
            $"yes i am bot{Environment.NewLine}" +
            $"```",
            generalChannel.Messages.Last().Content);

        // post ".config MentionResponseCooldown 50"
        Assert.AreEqual(cfg.MentionResponseCooldown, 600);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config MentionResponseCooldown 50");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "MentionResponseCooldown", "50");
        Assert.AreEqual(cfg.MentionResponseCooldown, 50);
        Assert.AreEqual("I've set `MentionResponseCooldown` to the following content: 50", generalChannel.Messages.Last().Content);

        // post ".config DiscordActivityName buckball"
        Assert.AreEqual(cfg.DiscordActivityName, "you all soon");
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config DiscordActivityName buckball");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "DiscordActivityName", "buckball");
        Assert.AreEqual(cfg.DiscordActivityName, "buckball");
        Assert.AreEqual("I've set `DiscordActivityName` to the following content: buckball", generalChannel.Messages.Last().Content);

        // post ".config DiscordActivityWatching false"
        Assert.AreEqual(cfg.DiscordActivityWatching, true);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config DiscordActivityWatching false");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "DiscordActivityWatching", "false");
        Assert.AreEqual(cfg.DiscordActivityWatching, false);
        Assert.AreEqual("I've set `DiscordActivityWatching` to the following content: False", generalChannel.Messages.Last().Content);

        // post ".config Aliases set moonlaser addquote moon"
        AssertDictionariesAreEqual(new Dictionary<string, string>(), cfg.Aliases);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config Aliases set moonlaser addquote moon");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "Aliases", "set moonlaser addquote moon");
        AssertDictionariesAreEqual(new Dictionary<string, string> { { "moonlaser", "addquote moon" } }, cfg.Aliases);
        Assert.AreEqual("I added the following string to the `moonlaser` map key in the `Aliases` map: `addquote moon`", generalChannel.Messages.Last().Content);

        // post ".config BannerMode ManebooruFeatured"
        Assert.AreEqual(cfg.BannerMode, BannerMode.None);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config BannerMode ManebooruFeatured");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "BannerMode", "ManebooruFeatured");
        Assert.AreEqual(cfg.BannerMode, BannerMode.ManebooruFeatured);
        Assert.AreEqual("I've set `BannerMode` to the following content: ManebooruFeatured", generalChannel.Messages.Last().Content);

        // post ".config BannerInterval 1"
        Assert.AreEqual(cfg.BannerInterval, 60);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config BannerInterval 1");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "BannerInterval", "1");
        Assert.AreEqual(cfg.BannerInterval, 1);
        Assert.AreEqual("I've set `BannerInterval` to the following content: 1", generalChannel.Messages.Last().Content);

        // post ".config BannerImages add https://static.manebooru.art/img/2022/11/23/4025857/large.png"
        AssertSetsAreEqual(new HashSet<string>(), cfg.BannerImages);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config BannerImages add https://static.manebooru.art/img/2022/11/23/4025857/large.png");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "BannerImages", "add https://static.manebooru.art/img/2022/11/23/4025857/large.png");
        AssertSetsAreEqual(new HashSet<string> { "https://static.manebooru.art/img/2022/11/23/4025857/large.png" }, cfg.BannerImages);
        Assert.AreEqual($"I added the following content to the `BannerImages` string list:{Environment.NewLine}" +
            $"```{Environment.NewLine}" +
            $"https://static.manebooru.art/img/2022/11/23/4025857/large.png{Environment.NewLine}" +
            $"```",
            generalChannel.Messages.Last().Content);

        // post ".config ModRole <@&1>"
        Assert.AreEqual(cfg.ModRole, 0ul);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config ModRole <@&1>");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "ModRole", "<@&1>");
        Assert.AreEqual(cfg.ModRole, 1ul);
        Assert.AreEqual("I've set `ModRole` to the following content: <@&1>", generalChannel.Messages.Last().Content);

        // post ".config ModChannel <#2>"
        Assert.AreEqual(cfg.ModChannel, 0ul);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config ModChannel <#2>");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "ModChannel", "<#2>");
        Assert.AreEqual(cfg.ModChannel, 2ul);
        Assert.AreEqual("I've set `ModChannel` to the following content: <#2>", generalChannel.Messages.Last().Content);

        // post ".config LogChannel <#3>"
        Assert.AreEqual(cfg.LogChannel, 0ul);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config LogChannel <#3>");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "LogChannel", "<#3>");
        Assert.AreEqual(cfg.LogChannel, 3ul);
        Assert.AreEqual("I've set `LogChannel` to the following content: <#3>", generalChannel.Messages.Last().Content);

        // post ".config ManageNewUserRoles false"
        Assert.AreEqual(cfg.ManageNewUserRoles, true);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config ManageNewUserRoles false");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "ManageNewUserRoles", "false");
        Assert.AreEqual(cfg.ManageNewUserRoles, false);
        Assert.AreEqual("I've set `ManageNewUserRoles` to the following content: False", generalChannel.Messages.Last().Content);

        // post ".config MemberRole <@&2>"
        Assert.AreEqual(cfg.MemberRole, 0ul);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config MemberRole <@&2>");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "MemberRole", "<@&2>");
        Assert.AreEqual(cfg.MemberRole, 2ul);
        Assert.AreEqual("I've set `MemberRole` to the following content: <@&2>", generalChannel.Messages.Last().Content);

        // post ".config NewMemberRole <@&3>"
        Assert.AreEqual(cfg.NewMemberRole, 0ul);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config NewMemberRole <@&3>");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "NewMemberRole", "<@&3>");
        Assert.AreEqual(cfg.NewMemberRole, 3ul);
        Assert.AreEqual("I've set `NewMemberRole` to the following content: <@&3>", generalChannel.Messages.Last().Content);

        // post ".config NewMemberRoleDecay 120"
        Assert.AreEqual(cfg.NewMemberRoleDecay, 0);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config NewMemberRoleDecay 120");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "NewMemberRoleDecay", "120");
        Assert.AreEqual(cfg.NewMemberRoleDecay, 120);
        Assert.AreEqual("I've set `NewMemberRoleDecay` to the following content: 120", generalChannel.Messages.Last().Content);

        // post ".config RolesToReapplyOnRejoin add <@&3>"
        AssertSetsAreEqual(new HashSet<ulong>(), cfg.RolesToReapplyOnRejoin);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config RolesToReapplyOnRejoin add <@&3>");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "RolesToReapplyOnRejoin", "add <@&3>");
        AssertSetsAreEqual(new HashSet<ulong> { 3ul }, cfg.RolesToReapplyOnRejoin);
        Assert.AreEqual($"I added the following content to the `RolesToReapplyOnRejoin` role list:{Environment.NewLine}" +
            $"New Pony",
            generalChannel.Messages.Last().Content);

        // post ".config FilterEnabled false"
        Assert.AreEqual(cfg.FilterEnabled, true);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config FilterEnabled false");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "FilterEnabled", "false");
        Assert.AreEqual(cfg.FilterEnabled, false);
        Assert.AreEqual("I've set `FilterEnabled` to the following content: False", generalChannel.Messages.Last().Content);

        // post ".config FilterIgnoredChannels add <#2>"
        AssertSetsAreEqual(new HashSet<ulong>(), cfg.FilterIgnoredChannels);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config FilterIgnoredChannels add <#2>");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "FilterIgnoredChannels", "add <#2>");
        AssertSetsAreEqual(new HashSet<ulong> { 2ul }, cfg.FilterIgnoredChannels);
        Assert.AreEqual($"I added the following content to the `FilterIgnoredChannels` channel list:{Environment.NewLine}" +
            $"modchat",
            generalChannel.Messages.Last().Content);

        // post ".config FilterBypassRoles add <@&1>"
        AssertSetsAreEqual(new HashSet<ulong>(), cfg.FilterBypassRoles);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config FilterBypassRoles add <@&1>");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "FilterBypassRoles", "add <@&1>");
        AssertSetsAreEqual(new HashSet<ulong> { 1ul }, cfg.FilterBypassRoles);
        Assert.AreEqual($"I added the following content to the `FilterBypassRoles` role list:{Environment.NewLine}" +
            $"Alicorn",
            generalChannel.Messages.Last().Content);

        // post ".config FilterDevBypass false"
        Assert.AreEqual(cfg.FilterDevBypass, true);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config FilterDevBypass false");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "FilterDevBypass", "false");
        Assert.AreEqual(cfg.FilterDevBypass, false);
        Assert.AreEqual("I've set `FilterDevBypass` to the following content: False", generalChannel.Messages.Last().Content);

        // post ".config FilteredWords add slurs mudpony"
        AssertDictsOfSetsAreEqual(new Dictionary<string, HashSet<string>>(), cfg.FilteredWords);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config FilteredWords add slurs mudpony");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "FilteredWords", "add slurs mudpony");
        AssertDictsOfSetsAreEqual(new Dictionary<string, HashSet<string>> { { "slurs", new HashSet<string> { "mudpony" } } }, cfg.FilteredWords);
        Assert.AreEqual("I added the following string to the `slurs` string list in the `FilteredWords` map: `mudpony`", generalChannel.Messages.Last().Content);

        // post ".config FilterResponseMessages set slurs true"
        AssertDictionariesAreEqual(new Dictionary<string, string?>(), cfg.FilterResponseMessages);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config FilterResponseMessages set slurs that wasn't very nice");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "FilterResponseMessages", "set slurs that wasn't very nice");
        AssertDictionariesAreEqual(new Dictionary<string, string?> { { "slurs", "that wasn't very nice" } }, cfg.FilterResponseMessages);
        Assert.AreEqual("I added the following string to the `slurs` map key in the `FilterResponseMessages` map: `that wasn't very nice`", generalChannel.Messages.Last().Content);

        // post ".config FilterResponseSilence set slurs true"
        AssertSetsAreEqual(new HashSet<string>(), cfg.FilterResponseSilence);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config FilterResponseSilence add slurs");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "FilterResponseSilence", "add slurs");
        AssertSetsAreEqual(new HashSet<string> { "slurs" }, cfg.FilterResponseSilence);
        Assert.AreEqual($"I added the following content to the `FilterResponseSilence` string list:{Environment.NewLine}" +
            $"```{Environment.NewLine}" +
            $"slurs{Environment.NewLine}" +
            $"```",
            generalChannel.Messages.Last().Content);

        // post ".config SpamEnabled false"
        Assert.AreEqual(cfg.SpamEnabled, true);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config SpamEnabled false");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "SpamEnabled", "false");
        Assert.AreEqual(cfg.SpamEnabled, false);
        Assert.AreEqual("I've set `SpamEnabled` to the following content: False", generalChannel.Messages.Last().Content);

        // post ".config SpamBypassRoles add <@&1>"
        AssertSetsAreEqual(new HashSet<ulong>(), cfg.SpamBypassRoles);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config SpamBypassRoles add <@&1>");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "SpamBypassRoles", "add <@&1>");
        AssertSetsAreEqual(new HashSet<ulong> { 1ul }, cfg.SpamBypassRoles);
        Assert.AreEqual($"I added the following content to the `SpamBypassRoles` role list:{Environment.NewLine}" +
            $"Alicorn",
            generalChannel.Messages.Last().Content);

        // post ".config SpamIgnoredChannels add <#2>"
        AssertSetsAreEqual(new HashSet<ulong>(), cfg.SpamIgnoredChannels);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config SpamIgnoredChannels add <#2>");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "SpamIgnoredChannels", "add <#2>");
        AssertSetsAreEqual(new HashSet<ulong> { 2ul }, cfg.SpamIgnoredChannels);
        Assert.AreEqual($"I added the following content to the `SpamIgnoredChannels` channel list:{Environment.NewLine}" +
            $"modchat",
            generalChannel.Messages.Last().Content);

        // post ".config SpamDevBypass false"
        Assert.AreEqual(cfg.SpamDevBypass, true);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config SpamDevBypass false");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "SpamDevBypass", "false");
        Assert.AreEqual(cfg.SpamDevBypass, false);
        Assert.AreEqual("I've set `SpamDevBypass` to the following content: False", generalChannel.Messages.Last().Content);

        // post ".config SpamBasePressure 15"
        Assert.AreEqual(cfg.SpamBasePressure, 10.0);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config SpamBasePressure 15");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "SpamBasePressure", "15");
        Assert.AreEqual(cfg.SpamBasePressure, 15.0);
        Assert.AreEqual("I've set `SpamBasePressure` to the following content: 15", generalChannel.Messages.Last().Content);

        // post ".config SpamImagePressure 15"
        Assert.AreEqual(cfg.SpamImagePressure, 8.3);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config SpamImagePressure 15");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "SpamImagePressure", "15");
        Assert.AreEqual(cfg.SpamImagePressure, 15.0);
        Assert.AreEqual("I've set `SpamImagePressure` to the following content: 15", generalChannel.Messages.Last().Content);

        // post ".config SpamLengthPressure 15"
        Assert.AreEqual(cfg.SpamLengthPressure, 0.00625);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config SpamLengthPressure 15");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "SpamLengthPressure", "15");
        Assert.AreEqual(cfg.SpamLengthPressure, 15.0);
        Assert.AreEqual("I've set `SpamLengthPressure` to the following content: 15", generalChannel.Messages.Last().Content);

        // post ".config SpamLinePressure 15"
        Assert.AreEqual(cfg.SpamLinePressure, 0.714);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config SpamLinePressure 15");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "SpamLinePressure", "15");
        Assert.AreEqual(cfg.SpamLinePressure, 15.0);
        Assert.AreEqual("I've set `SpamLinePressure` to the following content: 15", generalChannel.Messages.Last().Content);

        // post ".config SpamPingPressure 15"
        Assert.AreEqual(cfg.SpamPingPressure, 2.5);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config SpamPingPressure 15");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "SpamPingPressure", "15");
        Assert.AreEqual(cfg.SpamPingPressure, 15.0);
        Assert.AreEqual("I've set `SpamPingPressure` to the following content: 15", generalChannel.Messages.Last().Content);

        // post ".config SpamRepeatPressure 15"
        Assert.AreEqual(cfg.SpamRepeatPressure, 10.0);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config SpamRepeatPressure 15");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "SpamRepeatPressure", "15");
        Assert.AreEqual(cfg.SpamRepeatPressure, 15.0);
        Assert.AreEqual("I've set `SpamRepeatPressure` to the following content: 15", generalChannel.Messages.Last().Content);

        // post ".config SpamMaxPressure 15"
        Assert.AreEqual(cfg.SpamMaxPressure, 60.0);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config SpamMaxPressure 15");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "SpamMaxPressure", "15");
        Assert.AreEqual(cfg.SpamMaxPressure, 15.0);
        Assert.AreEqual("I've set `SpamMaxPressure` to the following content: 15", generalChannel.Messages.Last().Content);

        // post ".config SpamPressureDecay 15"
        Assert.AreEqual(cfg.SpamPressureDecay, 2.5);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config SpamPressureDecay 15");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "SpamPressureDecay", "15");
        Assert.AreEqual(cfg.SpamPressureDecay, 15.0);
        Assert.AreEqual("I've set `SpamPressureDecay` to the following content: 15", generalChannel.Messages.Last().Content);

        // post ".config SpamMessageDeleteLookback 15"
        Assert.AreEqual(cfg.SpamMessageDeleteLookback, 60);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config SpamMessageDeleteLookback 15");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "SpamMessageDeleteLookback", "15");
        Assert.AreEqual(cfg.SpamMessageDeleteLookback, 15.0);
        Assert.AreEqual("I've set `SpamMessageDeleteLookback` to the following content: 15", generalChannel.Messages.Last().Content);

        // post ".config RaidProtectionEnabled false"
        Assert.AreEqual(cfg.RaidProtectionEnabled, true);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config RaidProtectionEnabled false");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "RaidProtectionEnabled", "false");
        Assert.AreEqual(cfg.RaidProtectionEnabled, false);
        Assert.AreEqual("I've set `RaidProtectionEnabled` to the following content: False", generalChannel.Messages.Last().Content);

        // post ".config AutoSilenceNewJoins true"
        Assert.AreEqual(cfg.AutoSilenceNewJoins, false);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config AutoSilenceNewJoins true");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "AutoSilenceNewJoins", "true");
        Assert.AreEqual(cfg.AutoSilenceNewJoins, true);
        Assert.AreEqual("I've set `AutoSilenceNewJoins` to the following content: True", generalChannel.Messages.Last().Content);

        // post ".config SmallRaidSize 5"
        Assert.AreEqual(cfg.SmallRaidSize, 3);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config SmallRaidSize 5");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "SmallRaidSize", "5");
        Assert.AreEqual(cfg.SmallRaidSize, 5);
        Assert.AreEqual("I've set `SmallRaidSize` to the following content: 5", generalChannel.Messages.Last().Content);

        // post ".config SmallRaidTime 50"
        Assert.AreEqual(cfg.SmallRaidTime, 180);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config SmallRaidTime 50");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "SmallRaidTime", "50");
        Assert.AreEqual(cfg.SmallRaidTime, 50);
        Assert.AreEqual("I've set `SmallRaidTime` to the following content: 50", generalChannel.Messages.Last().Content);

        // post ".config LargeRaidSize 20"
        Assert.AreEqual(cfg.LargeRaidSize, 10);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config LargeRaidSize 20");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "LargeRaidSize", "20");
        Assert.AreEqual(cfg.LargeRaidSize, 20);
        Assert.AreEqual("I've set `LargeRaidSize` to the following content: 20", generalChannel.Messages.Last().Content);

        // post ".config LargeRaidTime 200"
        Assert.AreEqual(cfg.LargeRaidTime, 120);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config LargeRaidTime 200");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "LargeRaidTime", "200");
        Assert.AreEqual(cfg.LargeRaidTime, 200);
        Assert.AreEqual("I've set `LargeRaidTime` to the following content: 200", generalChannel.Messages.Last().Content);

        // post ".config RecentJoinDecay 100"
        Assert.AreEqual(cfg.RecentJoinDecay, 300);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config RecentJoinDecay 100");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "RecentJoinDecay", "100");
        Assert.AreEqual(cfg.RecentJoinDecay, 100);
        Assert.AreEqual("I've set `RecentJoinDecay` to the following content: 100", generalChannel.Messages.Last().Content);

        // post ".config SmallRaidDecay 10"
        Assert.AreEqual(cfg.SmallRaidDecay, 5);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config SmallRaidDecay 10");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "SmallRaidDecay", "10");
        Assert.AreEqual(cfg.SmallRaidDecay, 10);
        Assert.AreEqual("I've set `SmallRaidDecay` to the following content: 10", generalChannel.Messages.Last().Content);

        // post ".config LargeRaidDecay 50"
        Assert.AreEqual(cfg.LargeRaidDecay, 30);
        context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, ".config LargeRaidDecay 50");
        await ConfigModule.TestableConfigCommandAsync(context, cfg, cd, "LargeRaidDecay", "50");
        Assert.AreEqual(cfg.LargeRaidDecay, 50);
        Assert.AreEqual("I've set `LargeRaidDecay` to the following content: 50", generalChannel.Messages.Last().Content);


        // Ensure we can't forget to keep this test up to date
        var configPropsCount = typeof(Config).GetProperties().Length;

        Assert.AreEqual(51, configPropsCount,
            $"{Environment.NewLine}If you just added or removed a config item, then this test is probably out of date");

        Assert.AreEqual(configPropsCount * 2, generalChannel.Messages.Count(),
            $"{Environment.NewLine}If you just added or removed a config item, then this test is probably out of date");
    }
}
