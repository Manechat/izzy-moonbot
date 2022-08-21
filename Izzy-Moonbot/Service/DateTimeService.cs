using System;

namespace Izzy_Moonbot.Service;

public interface IDateTimeService
{
    DateTime UtcNow();
}

public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow()
    {
        return DateTime.UtcNow;
    }
}