using Izzy_Moonbot;
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
        // a user who is already in the users Dictionary and has a timestamp
        var users = new Dictionary<ulong, User>();
        users[sunny.Id] = new User();
        users[sunny.Id].Timestamp = DateTimeHelper.UtcNow;

        var mod = new ModService(cfg, users);
        var modLog = new ModLoggingService(cfg);
        var logger = new LoggingService(new TestLogger<Worker>());
        var state = new TransientState();
        var ss = new SpamService(logger, mod, modLog, cfg, users, state);
        ss.RegisterEvents(client);
        var sm = new SpamModule(ss);

        var commandLengthPenalty = Math.Round(cfg.SpamLengthPressure * ".getpressure".Length, 2);

        // start out with no pressure besides what .getpressure itself generates
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".getpressure");
        await sm.TestableGetPressureAsync(context, "");

        var firstPressure = cfg.SpamBasePressure + commandLengthPenalty;
        Assert.AreEqual($"Current Pressure for Sunny (Sunny/2): {firstPressure}", generalChannel.Messages.Last().Content);

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
        Assert.AreEqual($"Current Pressure for Sunny (Sunny/2): {secondPressure}", generalChannel.Messages.Last().Content);

        // simulate 10 seconds of pressure decay
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddSeconds(10);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".getpressure");
        await sm.TestableGetPressureAsync(context, "");

        var thirdPressure = (secondPressure - (10 * (cfg.SpamBasePressure / cfg.SpamPressureDecay))) +
            (cfg.SpamBasePressure + cfg.SpamRepeatPressure + commandLengthPenalty);
        Assert.AreEqual($"Current Pressure for Sunny (Sunny/2): {thirdPressure}", generalChannel.Messages.Last().Content);

        // let pressure fully decay back to zero
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(10);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".getpressure");
        await sm.TestableGetPressureAsync(context, "");

        var finalPressure = cfg.SpamBasePressure + cfg.SpamRepeatPressure + commandLengthPenalty;
        Assert.AreEqual($"Current Pressure for Sunny (Sunny/2): {finalPressure}", generalChannel.Messages.Last().Content);
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

        var users = new Dictionary<ulong, User>();
        users[sunny.Id] = new User();
        users[sunny.Id].Timestamp = DateTimeHelper.UtcNow;
        users[regularUserId] = new User();
        users[regularUserId].Timestamp = DateTimeHelper.UtcNow;

        var mod = new ModService(cfg, users);
        var modLog = new ModLoggingService(cfg);
        var logger = new LoggingService(new TestLogger<Worker>());
        var state = new TransientState();
        var ss = new SpamService(logger, mod, modLog, cfg, users, state);
        ss.RegisterEvents(client);
        var sm = new SpamModule(ss);

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".getpressure {regularUserId}");
        await sm.TestableGetPressureAsync(context, $"{regularUserId}");

        Assert.AreEqual($"Current Pressure for Zipp (Zipp/3): 0", generalChannel.Messages.Last().Content);

        context = await client.AddMessageAsync(guild.Id, modChat.Id, sunny.Id, $".getpressure {regularUserId}");
        await sm.TestableGetPressureAsync(context, $"{regularUserId}");

        Assert.AreEqual($"Current Pressure for Zipp (Zipp/3): 0", modChat.Messages.Last().Content);
    }

}
