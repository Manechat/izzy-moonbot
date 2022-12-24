using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Settings;
using Izzy_Moonbot.Service;
using static Izzy_Moonbot.Modules.MiscModule;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot;
using System.Reflection.Metadata;

namespace Izzy_Moonbot_Tests.Services;

[TestClass()]
public class ScheduleServiceTests
{
    [TestMethod()]
    public async Task Breathing_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;

        cfg.ModChannel = modChat.Id;

        // ModService assumes that every user it's asked to silence is already in the users Dictionary
        var users = new Dictionary<ulong, User>();
        users[sunny.Id] = new User();

        var generalStorage = new GeneralStorage();

        var scheduledJobs = new List<ScheduledJob>();

        var mod = new ModService(cfg, users);
        var modLog = new ModLoggingService(cfg);
        var logger = new LoggingService(new TestLogger<Worker>());
        var ss = new ScheduleService(cfg, mod, modLog, logger, generalStorage, scheduledJobs);

        Assert.AreEqual(0, generalChannel.Messages.Count);

        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;

        await ss.Unicycle(client);
        Assert.AreEqual(0, generalChannel.Messages.Count);

        var action = new ScheduledEchoJob(generalChannel.Id, "test echo");
        await ss.CreateScheduledJob(new ScheduledJob(DateTimeHelper.UtcNow, DateTimeHelper.UtcNow.AddMinutes(2), action));

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(1);

        await ss.Unicycle(client);
        Assert.AreEqual(0, generalChannel.Messages.Count);

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(1);

        await ss.Unicycle(client);
        Assert.AreEqual(1, generalChannel.Messages.Count);
        Assert.AreEqual("test echo", generalChannel.Messages.Last().Content);
    }

    // For now, BannerRotation isn't worth testing because of its non-Discord network dependencies
    [TestMethod()]
    public async Task RunBasicJobTypes_Tests()
    {
        var (cfg, _, (_, sunny), roles, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;

        var alicorn = roles[0];

        ulong sproutId = 10;
        guild.BannedUserIds.Add(sproutId);

        var users = new Dictionary<ulong, User>();
        guild.Users.ForEach(u => users[u.Id] = new User());
        users[sproutId] = new User();

        var generalStorage = new GeneralStorage();
        var scheduledJobs = new List<ScheduledJob>();
        var mod = new ModService(cfg, users);
        var modLog = new ModLoggingService(cfg);
        var logger = new LoggingService(new TestLogger<Worker>());
        var ss = new ScheduleService(cfg, mod, modLog, logger, generalStorage, scheduledJobs);

        // assert initial state
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        TestUtils.AssertListsAreEqual(new List<ulong> { alicorn.Id }, guild.UserRoles[sunny.Id]);
        Assert.IsFalse(client.DirectMessages.ContainsKey(sunny.Id));
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { sproutId }, guild.BannedUserIds);

        // should be a no-op
        await ss.Unicycle(client);

        TestUtils.AssertListsAreEqual(new List<ulong> { alicorn.Id }, guild.UserRoles[sunny.Id]);
        Assert.IsFalse(client.DirectMessages.ContainsKey(sunny.Id));
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { sproutId }, guild.BannedUserIds);

        var removeRoleAction = new ScheduledRoleRemovalJob(alicorn.Id, sunny.Id, "somepony stole the crystals");
        await ss.CreateScheduledJob(new ScheduledJob(DateTimeHelper.UtcNow, DateTimeHelper.UtcNow.AddMinutes(1), removeRoleAction));

        var echoAction = new ScheduledEchoJob(sunny.Id, "your sparkle is rainbow!");
        await ss.CreateScheduledJob(new ScheduledJob(DateTimeHelper.UtcNow, DateTimeHelper.UtcNow.AddMinutes(2), echoAction));

        var addRoleAction = new ScheduledRoleAdditionJob(alicorn.Id, sunny.Id, "found the unity crystals");
        await ss.CreateScheduledJob(new ScheduledJob(DateTimeHelper.UtcNow, DateTimeHelper.UtcNow.AddMinutes(3), addRoleAction));

        var unbanAction = new ScheduledUnbanJob(sproutId);
        await ss.CreateScheduledJob(new ScheduledJob(DateTimeHelper.UtcNow, DateTimeHelper.UtcNow.AddMinutes(5), unbanAction));

        // still a no-op, no time has passed
        await ss.Unicycle(client);

        TestUtils.AssertListsAreEqual(new List<ulong> { alicorn.Id }, guild.UserRoles[sunny.Id]);
        Assert.IsFalse(client.DirectMessages.ContainsKey(sunny.Id));
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { sproutId }, guild.BannedUserIds);

        // RemoveRole should happen now
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(1);
        await ss.Unicycle(client);

        TestUtils.AssertListsAreEqual(new List<ulong>(), guild.UserRoles[sunny.Id]); // role removed
        Assert.IsFalse(client.DirectMessages.ContainsKey(sunny.Id));
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { sproutId }, guild.BannedUserIds);

        // Echo should happen now
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(1);
        await ss.Unicycle(client);

        TestUtils.AssertListsAreEqual(new List<ulong>(), guild.UserRoles[sunny.Id]);
        TestUtils.AssertListsAreEqual(new List<string> { "your sparkle is rainbow!" }, client.DirectMessages[sunny.Id].Select(sm => sm.Content).ToList()); // DM added
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { sproutId }, guild.BannedUserIds);

        // AddRole should happen now
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(1);
        await ss.Unicycle(client);

        TestUtils.AssertListsAreEqual(new List<ulong> { alicorn.Id }, guild.UserRoles[sunny.Id]); // role added
        TestUtils.AssertListsAreEqual(new List<string> { "your sparkle is rainbow!" }, client.DirectMessages[sunny.Id].Select(sm => sm.Content).ToList());
        TestUtils.AssertSetsAreEqual(new HashSet<ulong> { sproutId }, guild.BannedUserIds);

        // Unban should happen now
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(2);
        await ss.Unicycle(client);

        TestUtils.AssertListsAreEqual(new List<ulong> { alicorn.Id }, guild.UserRoles[sunny.Id]);
        TestUtils.AssertListsAreEqual(new List<string> { "your sparkle is rainbow!" }, client.DirectMessages[sunny.Id].Select(sm => sm.Content).ToList());
        TestUtils.AssertSetsAreEqual(new HashSet<ulong>(), guild.BannedUserIds); // sprout's back in
    }
}
