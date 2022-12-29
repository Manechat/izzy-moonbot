using Izzy_Moonbot.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Izzy_Moonbot.Helpers.DiscordHelper;

namespace Izzy_Moonbot_Tests.Helpers;

[TestClass()]
public class TimeHelperTests
{
    [TestMethod()]
    public void GetTimeTypeTests()
    {
        Assert.AreEqual("unknown", TimeHelper.GetTimeType(""));
        Assert.AreEqual("absolute date", TimeHelper.GetTimeType("on friday at 03:00"));
        Assert.AreEqual("absolute time", TimeHelper.GetTimeType("at 10 am"));
        Assert.AreEqual("relative", TimeHelper.GetTimeType("in 10 minutes"));
        Assert.AreEqual("repeat", TimeHelper.GetTimeType("every january"));
    }

    public static void AssertTimeHelperResponsesAreEqual(TimeHelperResponse expected, TimeHelperResponse actual)
    {
        Assert.AreEqual(expected.RepeatType, actual.RepeatType, "\nRepeatType");
        Assert.AreEqual(expected.Time, actual.Time, "\nTime");
    }

    public static void AssertTryParseDateTime(
        (TimeHelperResponse, string)? actualResponse,
        DateTimeOffset expectedTime,
        string? expectedRepeatType,
        string expectedRemainingArgsString)
    {
        Assert.IsNotNull(actualResponse);
        if (actualResponse is var (thr, remainingArgsString))
        {
            AssertTimeHelperResponsesAreEqual(
                new TimeHelperResponse(expectedTime, expectedRepeatType),
                thr
            );
            Assert.AreEqual(expectedRemainingArgsString, remainingArgsString);
        }
    }

    [TestMethod()]
    public void TryParseDateTime_RelativeTests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("in 10 minutes", out err),
            DateTimeHelper.UtcNow.AddMinutes(10), null, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("in 1 hour", out err),
            DateTimeHelper.UtcNow.AddHours(1), null, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("in 37 seconds", out err),
            DateTimeHelper.UtcNow.AddSeconds(37), null, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("in 7 days", out err),
            DateTimeHelper.UtcNow.AddDays(7), null, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("in 6 months", out err),
            DateTimeHelper.UtcNow.AddMonths(6), null, ""
        );
        Assert.AreEqual(err, null);


        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("in 1 hour here's some text", out err),
            DateTimeHelper.UtcNow.AddHours(1), null, "here's some text"
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("1 hour here's some text", out err),
            DateTimeHelper.UtcNow.AddHours(1), null, "here's some text"
        );
        Assert.AreEqual(err, null);

        Assert.IsNull(TimeHelper.TryParseDateTime("one hour", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "Failed to");
    }

    [TestMethod()]
    public void TryParseDateTime_MiscTests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        Assert.IsNull(TimeHelper.TryParseDateTime("", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"\" can't be a datetime");

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("at 03:15", out err),
            new DateTimeOffset(2010, 10, 10, 3, 15, 0, TimeSpan.Zero), null, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("on 1st jan at 12:00", out err),
            new DateTimeOffset(2010, 1, 1, 12, 0, 0, 0, TimeSpan.Zero), null, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("every hour", out err),
            DateTimeHelper.UtcNow.AddHours(1), "relative", ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("every day at 12:30", out err),
            new DateTimeOffset(2010, 10, 10, 12, 30, 0, TimeSpan.Zero), "daily", ""
        );
        Assert.AreEqual(err, null);
    }

    // TODO: refactor the impl regex so we can just accept arbitrarily long numbers without hacks
    [TestMethod()]
    public void TryParseDateTime_MultipleDigitsTests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("in 123 seconds", out err),
            DateTimeHelper.UtcNow.AddSeconds(123), null, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("in 1234 seconds", out err),
            DateTimeHelper.UtcNow.AddSeconds(1234), null, ""
        );
        Assert.AreEqual(err, null);

        Assert.IsNull(TimeHelper.TryParseDateTime("in 12345 seconds", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "Failed to");
    }

    [TestMethod()]
    public void TryParseDateTime_DiscordTimestamp_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("<t:123456789>", out err),
            DateTimeOffset.FromUnixTimeSeconds(123456789), null, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("<t:123456789:R>", out err),
            DateTimeOffset.FromUnixTimeSeconds(123456789), null, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("<t:0:x>", out err),
            DateTimeOffset.FromUnixTimeSeconds(0), null, ""
        );
        Assert.AreEqual(err, null);

        Assert.IsNull(TimeHelper.TryParseDateTime("123456789", out err));
        Assert.IsNotNull(err);
    }
}