using System;

namespace Izzy_Moonbot.Helpers;

public class DateTimeHelper
{
    public static DateTimeOffset? FakeUtcNow { get; set; } = null;

    public static DateTimeOffset UtcNow
    {
        get
        {
            if (FakeUtcNow is DateTimeOffset now)
                return now;

            return DateTimeOffset.UtcNow;
        }
    }
}
