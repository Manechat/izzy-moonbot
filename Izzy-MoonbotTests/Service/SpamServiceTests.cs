using Izzy_Moonbot;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Modules;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Izzy_Moonbot_Tests.Services;

[TestClass()]
public class SpamServiceTests
{
    [TestMethod()]
    public async Task Breathing_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        DiscordHelper.DevUserIds = new List<ulong>();
        DiscordHelper.PleaseAwaitEvents = true;
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;

        cfg.ModChannel = modChat.Id;

        // SpamService assumes that every MessageReceived event it receives is for
        // a user who is already in the users Dictionary and has a timestamp
        var users = new Dictionary<ulong, User>();
        users[sunny.Id] = new User();
        users[sunny.Id].Timestamp = DateTimeHelper.UtcNow;

        var mod = new ModService(cfg, users);
        var modLog = new ModLoggingService(cfg);
        var logger = new LoggingService(new TestLogger<Worker>());
        var ss = new SpamService(logger, mod, modLog, cfg, users);

        ss.RegisterEvents(client);

        Assert.AreEqual(0, generalChannel.Messages.Count);
        Assert.AreEqual(0, modChat.Messages.Count);

        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, SpamService._testString);

        // The spam message has already been deleted
        Assert.AreEqual(0, generalChannel.Messages.Count);
        Assert.AreEqual(1, modChat.Messages.Count);

        Assert.AreEqual("<@&0> Spam detected by <@2>", modChat.Messages.Last().Content);

        TestUtils.AssertEmbedFieldsAre(modChat.Messages.Last().Embeds[0].Fields, new List<(string, string)>
        {
            ("User", "<@2> (`2`)"),
            ("Channel", "<#1>"),
            ("Pressure", "This user's last message raised their pressure from 0 to 60, exceeding 60"),
            ("Breakdown of last message", "**Test string**"),
        });
    }
}