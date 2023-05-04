using Izzy_Moonbot;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Izzy_Moonbot_Tests.Services;

[TestClass()]
public class FilterServiceTests
{
    public static void SetupFilterService(Config cfg, StubGuild guild, StubClient client, TestUser sunny)
    {
        DiscordHelper.DefaultGuildId = guild.Id;
        DiscordHelper.DevUserIds = new List<ulong>();
        DiscordHelper.PleaseAwaitEvents = true;

        // FilterService assumes that every MessageReceived event it receives is for
        // a user who is already in the users Dictionary
        var users = new Dictionary<ulong, User>();
        users[sunny.Id] = new User();

        var mod = new ModService(cfg, users);
        var modLog = new ModLoggingService(cfg);
        var logger = new LoggingService(new TestLogger<Worker>());
        var fs = new FilterService(cfg, mod, modLog, logger);

        fs.RegisterEvents(client);
    }

    [TestMethod()]
    public async Task Breathing_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        cfg.ModChannel = modChat.Id;
        cfg.FilterWords = new HashSet<string> { "magic", "wing", "feather", "mayonnaise" };
        SetupFilterService(cfg, guild, client, sunny);

        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "this is a completely ordinary chat message");

        Assert.AreEqual(1, generalChannel.Messages.Count);
        Assert.AreEqual("this is a completely ordinary chat message", generalChannel.Messages.Last().Content);
        Assert.AreEqual(0, modChat.Messages.Count);

        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "magic wings of mayonnaise");

        Assert.AreEqual(1, generalChannel.Messages.Count);
        Assert.AreEqual("this is a completely ordinary chat message", generalChannel.Messages.Last().Content);
        Assert.AreEqual(1, modChat.Messages.Count);
        Assert.AreEqual($"<@&{cfg.ModRole}> Filter Violation for <@{sunny.Id}>", modChat.Messages.Last().Content);

        TestUtils.AssertEmbedFieldsAre(modChat.Messages.Last().Embeds[0].Fields, new List<(string, string)>
        {
            ("User", $"<@{sunny.Id}> (`{sunny.Id}`)"),
            ("Channel", $"<#{generalChannel.Id}>"),
            ("Trigger Word", "magic"),
            ("Filtered Message", "magic wings of mayonnaise"),
            ("What have I done in response?", ":x: - **I've deleted the offending message.**\n" +
                                              ":mute: - **I've silenced the user.**\n" +
                                              ":exclamation: - **I've pinged all moderators.**"),
        });
    }

    [TestMethod()]
    public async Task FilteringIsCaseInsensitive_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        cfg.ModChannel = modChat.Id;
        cfg.FilterWords = new HashSet<string> { "magic", "wing", "feather", "mayonnaise" };
        SetupFilterService(cfg, guild, client, sunny);

        Assert.AreEqual(0, modChat.Messages.Count);

        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "MaGiC");

        Assert.AreEqual(1, modChat.Messages.Count);
        Assert.AreEqual($"<@&{cfg.ModRole}> Filter Violation for <@{sunny.Id}>", modChat.Messages.Last().Content);

        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "fEatHer");

        Assert.AreEqual(2, modChat.Messages.Count);
        Assert.AreEqual($"<@&{cfg.ModRole}> Filter Violation for <@{sunny.Id}>", modChat.Messages.Last().Content);
    }
}