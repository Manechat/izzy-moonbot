using Izzy_Moonbot;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Modules;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Izzy_Moonbot_Tests.Services;

[TestClass()]
public class SpamModuleTests
{
    [TestMethod()]
    public async Task GetPressure_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        DiscordHelper.DevUserIds = new List<ulong>();
        DiscordHelper.PleaseAwaitEvents = true;
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;

        // SpamService assumes that every MessageReceived event it receives is for
        // a user who is already in the users database and has a timestamp
        var users = new UserService(null);
        var s = new User(); s.Id = sunny.Id; s.Timestamp = DateTimeHelper.UtcNow; await users.CreateUser(s);

        var mod = new ModService(cfg, users);
        var modLog = new ModLoggingService(cfg);
        var logger = new LoggingService(new TestLogger<Worker>());
        var ss = new SpamService(logger, mod, modLog, cfg, users);
        ss.RegisterEvents(client);
        var sm = new SpamModule(ss);

        var commandLengthPenalty = Math.Round(cfg.SpamLengthPressure * ".getpressure".Length, 2);

        // start out with no pressure besides what .getpressure itself generates
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".getpressure");
        await sm.TestableGetPressureAsync(context, "");

        var firstPressure = cfg.SpamBasePressure + commandLengthPenalty;
        Assert.AreEqual($"Current Pressure for Sunny#1234: {firstPressure}", generalChannel.Messages.Last().Content);

        // say a few normal messages without any time passing to raise pressure
        var message1 = "hi everypony";
        var message2 = "my name is Sunny";

        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, message1);
        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, message2);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".getpressure");
        await sm.TestableGetPressureAsync(context, "");

        var secondPressure = firstPressure +
            (cfg.SpamBasePressure + Math.Round(cfg.SpamLengthPressure * message1.Length, 2)) +
            (cfg.SpamBasePressure + Math.Round(cfg.SpamLengthPressure * message2.Length, 2)) +
            (cfg.SpamBasePressure + commandLengthPenalty);
        Assert.AreEqual($"Current Pressure for Sunny#1234: {secondPressure}", generalChannel.Messages.Last().Content);

        // simulate 10 seconds of pressure decay
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddSeconds(10);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".getpressure");
        await sm.TestableGetPressureAsync(context, "");

        var thirdPressure = (secondPressure - (10 * (cfg.SpamBasePressure / cfg.SpamPressureDecay))) +
            (cfg.SpamBasePressure + cfg.SpamRepeatPressure + commandLengthPenalty);
        Assert.AreEqual($"Current Pressure for Sunny#1234: {thirdPressure}", generalChannel.Messages.Last().Content);

        // let pressure fully decay back to zero
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(10);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".getpressure");
        await sm.TestableGetPressureAsync(context, "");

        var finalPressure = cfg.SpamBasePressure + cfg.SpamRepeatPressure + commandLengthPenalty;
        Assert.AreEqual($"Current Pressure for Sunny#1234: {finalPressure}", generalChannel.Messages.Last().Content);
    }

    [TestMethod()]
    public async Task GetMessages_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        DiscordHelper.DevUserIds = new List<ulong>();
        DiscordHelper.PleaseAwaitEvents = true;
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;

        // SpamService assumes that every MessageReceived event it receives is for
        // a user who is already in the users database and has a timestamp
        var users = new UserService(null);
        var s = new User(); s.Id = sunny.Id; s.Timestamp = DateTimeHelper.UtcNow; await users.CreateUser(s);

        var mod = new ModService(cfg, users);
        var modLog = new ModLoggingService(cfg);
        var logger = new LoggingService(new TestLogger<Worker>());
        var ss = new SpamService(logger, mod, modLog, cfg, users);
        ss.RegisterEvents(client);
        var sm = new SpamModule(ss);

        // start out with no previous messages besides .getmessages itself
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".getmessages");
        await sm.TestableGetPreviousMessagesAsync(context, "");

        Assert.AreEqual($"I consider the following messages from Sunny#1234 to be recent: " +
            $"\nhttps://discord.com/channels/{guild.Id}/{generalChannel.Id}/0 at <t:1286668800:F> (<t:1286668800:R>)" +
            $"\n*Note that these messages may not actually be recent as their age is only checked when the user sends more messages.*",
            generalChannel.Messages.Last().Content);

        // say a few normal messages without any time passing
        var message1 = "hi everypony";
        var message2 = "my name is Sunny";

        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, message1);
        await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, message2);
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".getmessages");
        await sm.TestableGetPreviousMessagesAsync(context, "");

        Assert.AreEqual($"I consider the following messages from Sunny#1234 to be recent: " +
            $"\nhttps://discord.com/channels/{guild.Id}/{generalChannel.Id}/0 at <t:1286668800:F> (<t:1286668800:R>)" +
            $"\nhttps://discord.com/channels/{guild.Id}/{generalChannel.Id}/1 at <t:1286668800:F> (<t:1286668800:R>)" +
            $"\nhttps://discord.com/channels/{guild.Id}/{generalChannel.Id}/2 at <t:1286668800:F> (<t:1286668800:R>)" +
            $"\nhttps://discord.com/channels/{guild.Id}/{generalChannel.Id}/3 at <t:1286668800:F> (<t:1286668800:R>)" +
            $"\n*Note that these messages may not actually be recent as their age is only checked when the user sends more messages.*",
            generalChannel.Messages.Last().Content);

        // simulate 10 seconds, which should still count as "recent"
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddSeconds(10);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".getmessages");
        await sm.TestableGetPreviousMessagesAsync(context, "");

        Assert.AreEqual($"I consider the following messages from Sunny#1234 to be recent: " +
            $"\nhttps://discord.com/channels/{guild.Id}/{generalChannel.Id}/0 at <t:1286668800:F> (<t:1286668800:R>)" +
            $"\nhttps://discord.com/channels/{guild.Id}/{generalChannel.Id}/1 at <t:1286668800:F> (<t:1286668800:R>)" +
            $"\nhttps://discord.com/channels/{guild.Id}/{generalChannel.Id}/2 at <t:1286668800:F> (<t:1286668800:R>)" +
            $"\nhttps://discord.com/channels/{guild.Id}/{generalChannel.Id}/3 at <t:1286668800:F> (<t:1286668800:R>)" +
            $"\nhttps://discord.com/channels/{guild.Id}/{generalChannel.Id}/4 at <t:1286668810:F> (<t:1286668810:R>)" + // slightly different timestamp
            $"\n*Note that these messages may not actually be recent as their age is only checked when the user sends more messages.*",
            generalChannel.Messages.Last().Content);

        // simulate enough time that none of the above messages should remain "recent"
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(10);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".getmessages");
        await sm.TestableGetPreviousMessagesAsync(context, "");

        Assert.AreEqual($"I consider the following messages from Sunny#1234 to be recent: " +
            $"\nhttps://discord.com/channels/{guild.Id}/{generalChannel.Id}/5 at <t:1286669410:F> (<t:1286669410:R>)" +
            $"\n*Note that these messages may not actually be recent as their age is only checked when the user sends more messages.*",
            generalChannel.Messages.Last().Content);
    }

    [TestMethod()]
    public async Task GetPressure_OnRegularUser_InPublicOrPrivateChannels_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        DiscordHelper.DevUserIds = new List<ulong>();
        DiscordHelper.PleaseAwaitEvents = true;
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;

        cfg.ModChannel = modChat.Id;

        var regularUserId = guild.Users[2].Id;

        var users = new UserService(null);
        var s = new User(); s.Id = sunny.Id; s.Timestamp = DateTimeHelper.UtcNow; await users.CreateUser(s);
        var r = new User(); r.Id = regularUserId; r.Timestamp = DateTimeOffset.UtcNow; await users.CreateUser(r);

        var mod = new ModService(cfg, users);
        var modLog = new ModLoggingService(cfg);
        var logger = new LoggingService(new TestLogger<Worker>());
        var ss = new SpamService(logger, mod, modLog, cfg, users);
        ss.RegisterEvents(client);
        var sm = new SpamModule(ss);

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".getpressure {regularUserId}");
        await sm.TestableGetPressureAsync(context, $"{regularUserId}");

        Assert.AreEqual($"Current Pressure for Zipp#1234: 0", generalChannel.Messages.Last().Content);

        context = await client.AddMessageAsync(guild.Id, modChat.Id, sunny.Id, $".getpressure {regularUserId}");
        await sm.TestableGetPressureAsync(context, $"{regularUserId}");

        Assert.AreEqual($"Current Pressure for Zipp#1234: 0", modChat.Messages.Last().Content);
    }

}