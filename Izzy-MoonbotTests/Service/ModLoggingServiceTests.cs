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

    // TODO: test batch logging if we can figure out how to deal with Task.Delay()
}
