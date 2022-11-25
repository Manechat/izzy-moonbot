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
    public void StripQuotesTest()
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
}
