using Microsoft.VisualStudio.TestTools.UnitTesting;
using Izzy_Moonbot.Settings;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot;
using Izzy_Moonbot.Adapters;

namespace Izzy_Moonbot_Tests.Services;

[TestClass()]
public class RaidServiceTests
{
    public static (RaidService, ScheduleService, TransientState) SetupRaidService(Config cfg, Dictionary<ulong, User> users)
    {
        var mod = new ModService(cfg, users);
        var modLog = new ModLoggingService(cfg);
        var logger = new LoggingService(new TestLogger<Worker>());
        var gs = new GeneralStorage();
        var s = new TransientState();

        var scheduledJobs = new List<ScheduledJob>();
        var ss = new ScheduleService(cfg, mod, modLog, logger, scheduledJobs);

        return (
            new RaidService(cfg, mod, logger, modLog, s, gs, ss),
            ss,
            s
        );
    }

    [TestMethod()]
    public async Task SmallRaid_Test()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;
        var users = new Dictionary<ulong, User>();
        var (rs, ss, state) = SetupRaidService(cfg, users);
        rs.RegisterEvents(client);

        cfg.RaidProtectionEnabled = true;
        cfg.RecentJoinDecay = 120; // seconds
        cfg.SmallRaidSize = 3; // users
        cfg.SmallRaidDecay = 5; // minutes

        // Initial state
        Assert.IsEmpty(ss.GetScheduledJobs());
        Assert.IsEmpty(modChat.Messages);
        Assert.IsEmpty(state.RecentJoins);

        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;

        await ss.Unicycle(client);
        Assert.IsEmpty(ss.GetScheduledJobs());
        Assert.IsEmpty(modChat.Messages);
        Assert.IsEmpty(state.RecentJoins);

        // Users start joining
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(1);
        await client.JoinUser("Peach Fizz", 101, guild);

        await ss.Unicycle(client);
        Assert.IsEmpty(ss.GetScheduledJobs());
        Assert.IsEmpty(modChat.Messages);
        Assert.HasCount(1, state.RecentJoins);

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(1);
        await client.JoinUser("Seashell", 102, guild);

        await ss.Unicycle(client);
        Assert.IsEmpty(ss.GetScheduledJobs());
        Assert.IsEmpty(modChat.Messages);
        Assert.HasCount(2, state.RecentJoins);

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(1);
        await client.JoinUser("Glory", 103, guild);

        // All three pippsqueaks at once is too many. Raid message sent to modchat.
        await ss.Unicycle(client);
        Assert.HasCount(1, ss.GetScheduledJobs());
        Assert.HasCount(1, modChat.Messages);
        Assert.HasCount(3, state.RecentJoins);
        StringAssert.Contains(modChat.Messages.Last().Content, "Possible raid detected!");

        // Only 4 minutes, raid's not over yet.
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(4);
        await ss.Unicycle(client);
        Assert.HasCount(1, ss.GetScheduledJobs());
        Assert.HasCount(1, modChat.Messages);
        Assert.HasCount(3, state.RecentJoins);

        // Raid's over after 5 minutes, and now it's been 6 minutes.
        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(2);
        await ss.Unicycle(client);
        Assert.IsEmpty(ss.GetScheduledJobs());
        Assert.HasCount(2, modChat.Messages);
        Assert.IsEmpty(state.RecentJoins);
        StringAssert.Contains(modChat.Messages.Last().Content, "I consider the raid to be over");
    }
}
