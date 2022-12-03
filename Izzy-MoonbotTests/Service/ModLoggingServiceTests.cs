using Izzy_Moonbot;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Izzy_Moonbot_Tests.Services;

[TestClass()]
public class ModLoggingServiceTests
{
    [TestMethod()]
    public async Task RegularLog_Tests()
    {
        var (_, _, (_, _), _, (_, modChat, _), guild, client) = TestUtils.DefaultStubs();
        var cfg = new Config();
        var modLog = new ModLoggingService(cfg);

        cfg.ModChannel = modChat.Id;

        await modLog.CreateModLog(new TestGuild(guild, client))
            .SetContent($"Attention: It's T.U.E.S. Day!")
            // TODO: embeds in stub messages
            // can't meaningfully test .SetFileLogContent()
            .Send();

        Assert.AreEqual(1, modChat.Messages.Count);
        Assert.AreEqual("Attention: It's T.U.E.S. Day!", modChat.Messages.Last().Content);
    }

    [TestMethod()]
    public async Task BatchLog_Tests()
    {
        var (_, _, (_, _), _, (_, modChat, _), guild, client) = TestUtils.DefaultStubs();
        var cfg = new Config();
        var modLog = new ModLoggingService(cfg);

        cfg.ModChannel = modChat.Id;
        cfg.BatchSendLogs = true;
        cfg.BatchLogsSendRate = 0.01; // not realistic, but it's not worth making the test slow

        await modLog.CreateModLog(new TestGuild(guild, client))
            .SetContent($"Attention: It's T.U.E.S. Day!")
            .Send();

        Assert.AreEqual(0, modChat.Messages.Count);

        await Task.Delay(100);

        Assert.AreEqual(1, modChat.Messages.Count);
        Assert.AreEqual("Attention: It's T.U.E.S. Day!", modChat.Messages.Last().Content);

        await modLog.CreateModLog(new TestGuild(guild, client))
            .SetContent($"Señor Butterscotch has joined the server.")
            .Send();
        await modLog.CreateModLog(new TestGuild(guild, client))
            .SetContent($"Grandma Figgy has joined the server.")
            .Send();
        await modLog.CreateModLog(new TestGuild(guild, client))
            .SetContent($"Misty Brightdawn has joined the server.")
            .Send();
        await modLog.CreateModLog(new TestGuild(guild, client))
            .SetContent($"Alphabittle has joined the server.")
            .Send();
        await modLog.CreateModLog(new TestGuild(guild, client))
            .SetContent($"Queen Haven has joined the server.")
            .Send();

        Assert.AreEqual(1, modChat.Messages.Count);

        await Task.Delay(100);

        Assert.AreEqual(2, modChat.Messages.Count);
        Assert.AreEqual($"Señor Butterscotch has joined the server.{Environment.NewLine}" +
            $"Grandma Figgy has joined the server.{Environment.NewLine}" +
            $"Misty Brightdawn has joined the server.{Environment.NewLine}" +
            $"Alphabittle has joined the server.{Environment.NewLine}" +
            $"Queen Haven has joined the server.", modChat.Messages.Last().Content);
    }
}
