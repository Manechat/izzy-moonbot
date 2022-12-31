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
    public void TryParseDateTime_IntervalTests()
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


        Assert.IsNull(TimeHelper.TryParseDateTime("in one hour", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"one\"");
        StringAssert.Contains(err, "not a positive integer");

        Assert.IsNull(TimeHelper.TryParseDateTime("one hour", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"one\"");
        StringAssert.Contains(err, "extract a weekday + time"); // without the 'in', this gets mistaken for an attempted weekday

        Assert.IsNull(TimeHelper.TryParseDateTime("in 1 xyz", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"xyz\"");
        StringAssert.Contains(err, "interval units:");

        Assert.IsNull(TimeHelper.TryParseDateTime("1 xyz", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"xyz\"");
        StringAssert.Contains(err, "interval units:");
    }

    [TestMethod()]
    public void TryParseDateTime_MiscError_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        Assert.IsNull(TimeHelper.TryParseDateTime("", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"\" can't be a datetime");
    }

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

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("in 12345 seconds", out err),
            DateTimeHelper.UtcNow.AddSeconds(12345), null, ""
        );
        Assert.AreEqual(err, null);
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
        StringAssert.Contains(err, "\"123456789\"");
        StringAssert.Contains(err, "not a Discord timestamp");
    }

    [TestMethod()]
    public void TryParseDateTime_Time_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("at 03:15 UTC+0", out err),
            new DateTimeOffset(2010, 10, 10, 3, 15, 0, TimeSpan.Zero), null, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("03:15 UTC+0", out err),
            new DateTimeOffset(2010, 10, 10, 3, 15, 0, TimeSpan.Zero), null, ""
        );
        Assert.AreEqual(err, null);

        Assert.IsNull(TimeHelper.TryParseDateTime("at 03:15", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"03:15\"");
        StringAssert.Contains(err, "missing a UTC offset");

        Assert.IsNull(TimeHelper.TryParseDateTime("03:15", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"03:15\"");
        StringAssert.Contains(err, "missing a UTC offset");
    }

    [TestMethod()]
    public void TryParseDateTime_WeekdayTime_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        // Oct 10th 2010 was a Sunday, so "next Monday" is the 11th
        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("on monday 03:15 UTC+0", out err),
            new DateTimeOffset(2010, 10, 11, 3, 15, 0, TimeSpan.Zero), null, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("monday 03:15 UTC+0", out err),
            new DateTimeOffset(2010, 10, 11, 3, 15, 0, TimeSpan.Zero), null, ""
        );
        Assert.AreEqual(err, null);

        Assert.IsNull(TimeHelper.TryParseDateTime("on monday 03:15", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"monday 03:15\"");
        StringAssert.Contains(err, "missing a UTC offset");

        Assert.IsNull(TimeHelper.TryParseDateTime("monday 03:15", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"monday 03:15\"");
        StringAssert.Contains(err, "missing a UTC offset");

        Assert.IsNull(TimeHelper.TryParseDateTime("on monday", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"monday\"");
        StringAssert.Contains(err, "missing a time");

        Assert.IsNull(TimeHelper.TryParseDateTime("monday", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"monday\"");
        StringAssert.Contains(err, "missing a time");
    }

    [TestMethod()]
    public void TryParseDateTime_DateTime_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("on 1 jan 2020 12:00 UTC+0", out err),
            new DateTimeOffset(2020, 1, 1, 12, 0, 0, 0, TimeSpan.Zero), null, ""
        );
        Assert.AreEqual(err, null);

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("1 jan 2020 12:00 UTC+0", out err),
            new DateTimeOffset(2020, 1, 1, 12, 0, 0, 0, TimeSpan.Zero), null, ""
        );
        Assert.AreEqual(err, null);

        Assert.IsNull(TimeHelper.TryParseDateTime("on 1 jan 2020 12:00", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"1 jan 2020 12:00\"");
        StringAssert.Contains(err, "missing a UTC offset");

        Assert.IsNull(TimeHelper.TryParseDateTime("1 jan 2020 12:00", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"1 jan 2020 12:00\"");
        StringAssert.Contains(err, "missing a UTC offset");
    }

    [TestMethod()]
    public void TryParseDateTime_RepeatingInterval_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("every 1 hour", out err),
            DateTimeHelper.UtcNow.AddHours(1), "relative", ""
        );
        Assert.AreEqual(err, null);
    }

    [TestMethod()]
    public void TryParseDateTime_RepeatingTime_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("every 12:30 UTC+0", out err),
            new DateTimeOffset(2010, 10, 10, 12, 30, 0, TimeSpan.Zero), "daily", ""
        );
        Assert.AreEqual(err, null);
    }

    [TestMethod()]
    public void TryParseDateTime_RepeatingWeekdayTime_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        // Oct 10th 2010 was a Sunday, so "next Monday" is the 11th
        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("every monday 12:30 UTC+0", out err),
            new DateTimeOffset(2010, 10, 11, 12, 30, 0, TimeSpan.Zero), "weekly", ""
        );
        Assert.AreEqual(err, null);
    }

    [TestMethod()]
    public void TryParseDateTime_RepeatingDateTime_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        AssertTryParseDateTime(
            TimeHelper.TryParseDateTime("every 1 jan 12:00 UTC+0", out err),
            new DateTimeOffset(2011, 1, 1, 12, 0, 0, TimeSpan.Zero), "yearly", ""
        );
        Assert.AreEqual(err, null);

        Assert.IsNull(TimeHelper.TryParseDateTime("every 1 jan 2020 12:00", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"every 1 jan 2020 12:00\"");
        StringAssert.Contains(err, "\"2020\" is not a valid time");

        Assert.IsNull(TimeHelper.TryParseDateTime("every 1 jan 12:00", out err));
        Assert.IsNotNull(err);
        StringAssert.Contains(err, "\"every 1 jan 12:00\"");
        StringAssert.Contains(err, "missing a UTC offset");
    }

    [TestMethod()]
    public void TryParseInterval_InThePast_Tests()
    {
        DateTimeHelper.FakeUtcNow = TestUtils.FiMEpoch;
        string? err;

        var response = TimeHelper.TryParseInterval("10 minutes", out err, inThePast: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(DateTimeHelper.UtcNow.AddMinutes(-10), response?.Item1);
        Assert.AreEqual("", response?.Item2);
        Assert.AreEqual(err, null);

        response = TimeHelper.TryParseInterval("1 hour", out err, inThePast: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(DateTimeHelper.UtcNow.AddHours(-1), response?.Item1);
        Assert.AreEqual("", response?.Item2);
        Assert.AreEqual(err, null);

        response = TimeHelper.TryParseInterval("37 seconds", out err, inThePast: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(DateTimeHelper.UtcNow.AddSeconds(-37), response?.Item1);
        Assert.AreEqual("", response?.Item2);
        Assert.AreEqual(err, null);

        response = TimeHelper.TryParseInterval("7 days", out err, inThePast: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(DateTimeHelper.UtcNow.AddDays(-7), response?.Item1);
        Assert.AreEqual("", response?.Item2);
        Assert.AreEqual(err, null);

        response = TimeHelper.TryParseInterval("6 months", out err, inThePast: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(DateTimeHelper.UtcNow.AddMonths(-6), response?.Item1);
        Assert.AreEqual("", response?.Item2);
        Assert.AreEqual(err, null);
    }
}