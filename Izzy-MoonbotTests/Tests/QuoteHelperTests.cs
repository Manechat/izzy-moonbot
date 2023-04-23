using Microsoft.VisualStudio.TestTools.UnitTesting;
using Izzy_Moonbot.Helpers;

namespace Izzy_Moonbot_Tests.Helpers;

[TestClass()]
public class QuoteHelperTests
{
    [TestMethod()]
    public void ParseQuoteArgs_NoArgsTests()
    {
        Assert.AreEqual(("", null), QuoteHelper.ParseQuoteArgs(""));

        Assert.AreEqual((" ", null), QuoteHelper.ParseQuoteArgs(" "));
    }

    [TestMethod()]
    public void ParseQuoteArgs_JustAUserTests()
    {
        Assert.AreEqual(("foo", null), QuoteHelper.ParseQuoteArgs("foo"));

        Assert.AreEqual(("foo bar baz", null), QuoteHelper.ParseQuoteArgs("foo bar baz"));

        Assert.AreEqual(("foo", null), QuoteHelper.ParseQuoteArgs("\"foo\""));

        Assert.AreEqual(("\"foo bar\" baz", null), QuoteHelper.ParseQuoteArgs("\"foo bar\" baz"));
    }

    [TestMethod()]
    public void ParseQuoteArgs_UserAndNumberTests()
    {
        Assert.AreEqual(("foo", 1), QuoteHelper.ParseQuoteArgs("foo 1"));

        Assert.AreEqual(("foo bar", 1), QuoteHelper.ParseQuoteArgs("foo bar 1"));

        Assert.AreEqual(("foo bar", 1234), QuoteHelper.ParseQuoteArgs("foo bar 1234"));

        Assert.AreEqual(("foo bar 1.23", null), QuoteHelper.ParseQuoteArgs("foo bar 1.23"));
    }

    [TestMethod()]
    public void ParseQuoteArgs_TextAfterNumberTests()
    {
        Assert.AreEqual(("", 1), QuoteHelper.ParseQuoteArgs("1 foo"));

        Assert.AreEqual(("foo", 1), QuoteHelper.ParseQuoteArgs("foo 1 bar"));

        Assert.AreEqual(("foo bar", 1), QuoteHelper.ParseQuoteArgs("foo bar 1 baz"));

        Assert.AreEqual(("foo", 1), QuoteHelper.ParseQuoteArgs("foo 1 bar 2 baz"));
    }
}