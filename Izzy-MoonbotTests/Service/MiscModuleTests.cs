using Microsoft.VisualStudio.TestTools.UnitTesting;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Modules;
using Izzy_Moonbot;
using Izzy_Moonbot_Tests.Services;

namespace Izzy_Moonbot_Tests.Modules;

[TestClass()]
public class MiscModuleTests
{
    [TestMethod()]
    public async Task RemindMe_Command_Tests()
    {
        var (cfg, _, (_, sunny), _, (generalChannel, modChat, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;
        cfg.ModChannel = modChat.Id;

        var generalStorage = new GeneralStorage();
        var scheduledJobs = new List<ScheduledJob>();
        var mod = new ModService(cfg, new Dictionary<ulong, User>());
        var modLog = new ModLoggingService(cfg);
        var logger = new LoggingService(new TestLogger<Worker>());
        var ss = new ScheduleService(cfg, mod, modLog, logger, generalStorage, scheduledJobs);

        var mm = new MiscModule(cfg, ss, logger);

        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;

        await ss.Unicycle(client);

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".remindme");
        await mm.TestableRemindMeCommandAsync(context, "");
        Assert.AreEqual("Hey uhh... I think you forgot something... (Missing `time` and `message` parameters, see `.help remindme`)", generalChannel.Messages.Last().Content);
        Assert.IsFalse(client.DirectMessages.ContainsKey(sunny.Id));
        Assert.AreEqual(0, ss.GetScheduledJobs().Count);

        await ss.Unicycle(client);

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".remindme 1 minute test");
        await mm.TestableRemindMeCommandAsync(context, "1 minute test");
        Assert.AreEqual("Okay! I'll DM you a reminder <t:1286668860:R>.", generalChannel.Messages.Last().Content);
        Assert.IsFalse(client.DirectMessages.ContainsKey(sunny.Id));
        Assert.AreEqual(1, ss.GetScheduledJobs().Count);

        await ss.Unicycle(client);

        DateTimeHelper.FakeUtcNow = DateTimeHelper.FakeUtcNow?.AddMinutes(1);

        await ss.Unicycle(client);

        Assert.AreEqual(1, client.DirectMessages[sunny.Id].Count);
        Assert.AreEqual("test", client.DirectMessages[sunny.Id].Last().Content);
        Assert.AreEqual(0, ss.GetScheduledJobs().Count);
    }
}