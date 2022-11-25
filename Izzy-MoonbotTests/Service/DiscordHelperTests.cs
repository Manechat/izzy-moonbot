using Izzy_Moonbot.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Izzy_MoonbotTests.Service;

[TestClass()]
public class DiscordHelperTests
{
    [TestMethod()]
    public void MiscTests()
    {
        Assert.IsTrue(DiscordHelper.IsSpace(' '));
        Assert.IsFalse(DiscordHelper.IsSpace('a'));
    }

    [TestMethod()]
    public void StripQuotesTests()
    {
        Assert.AreEqual("", DiscordHelper.StripQuotes(""));
        Assert.AreEqual("a", DiscordHelper.StripQuotes("a"));
        Assert.AreEqual("ab", DiscordHelper.StripQuotes("ab"));

        Assert.AreEqual("foo", DiscordHelper.StripQuotes("foo"));
        Assert.AreEqual("foo bar", DiscordHelper.StripQuotes("foo bar"));
        Assert.AreEqual("foo \"bar\" baz", DiscordHelper.StripQuotes("foo \"bar\" baz"));

        Assert.AreEqual("foo", DiscordHelper.StripQuotes("\"foo\""));
        Assert.AreEqual("foo bar", DiscordHelper.StripQuotes("\"foo bar\""));
        Assert.AreEqual("foo \"bar\" baz", DiscordHelper.StripQuotes("\"foo \"bar\" baz\""));

        Assert.AreEqual("foo", DiscordHelper.StripQuotes("'foo'"));
        Assert.AreEqual("foo", DiscordHelper.StripQuotes("ʺfooʺ"));
        Assert.AreEqual("foo", DiscordHelper.StripQuotes("˝fooˮ"));
        Assert.AreEqual("foo", DiscordHelper.StripQuotes("“foo”"));
        Assert.AreEqual("foo", DiscordHelper.StripQuotes("'foo”"));
    }

    [TestMethod()]
    public void ConvertPingsTests()
    {
        Assert.AreEqual(0ul, DiscordHelper.ConvertChannelPingToId(""));
        Assert.AreEqual(1234ul, DiscordHelper.ConvertChannelPingToId("1234"));
        Assert.AreEqual(1234ul, DiscordHelper.ConvertChannelPingToId("<#1234>"));
        Assert.ThrowsException<FormatException>(() => DiscordHelper.ConvertChannelPingToId("<#>"));
        Assert.ThrowsException<FormatException>(() => DiscordHelper.ConvertChannelPingToId("foo <#1234> bar"));

        Assert.AreEqual(0ul, DiscordHelper.ConvertUserPingToId(""));
        Assert.AreEqual(1234ul, DiscordHelper.ConvertUserPingToId("1234"));
        Assert.AreEqual(1234ul, DiscordHelper.ConvertUserPingToId("<@1234>"));
        Assert.ThrowsException<FormatException>(() => DiscordHelper.ConvertUserPingToId("<@>"));
        Assert.ThrowsException<FormatException>(() => DiscordHelper.ConvertUserPingToId("foo <@1234> bar"));

        Assert.AreEqual(0ul, DiscordHelper.ConvertRolePingToId(""));
        Assert.AreEqual(1234ul, DiscordHelper.ConvertRolePingToId("1234"));
        Assert.AreEqual(1234ul, DiscordHelper.ConvertRolePingToId("<@&1234>"));
        Assert.ThrowsException<FormatException>(() => DiscordHelper.ConvertRolePingToId("<@&>"));
        Assert.ThrowsException<FormatException>(() => DiscordHelper.ConvertRolePingToId("foo <@&1234> bar"));
    }
}
