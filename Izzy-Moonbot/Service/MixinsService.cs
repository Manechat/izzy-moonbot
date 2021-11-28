namespace Izzy_Moonbot.Service
{
    using System;

    public class MixinsService
    {
        private readonly IDateTimeService _dateTime;
        public MixinsService(IDateTimeService dateTime)
        {
            _dateTime = dateTime;
        }

        public string Transpile(string query)
        {
            var now = _dateTime.UtcNow();
            return query.Replace("{{today}}", now.ToString("yyyy-MM-dd"))
                        .Replace("{{current_year}}", now.ToString("yyyy"))
                        .Replace("{{current_month}}", now.ToString("MM"))
                        .Replace("{{current_day}}", now.ToString("dd"))
                        .Replace("{{current_hour}}", now.ToString("HH"))
                        .Replace("{{current_minute}}", now.ToString("mm"))
                        .Replace("{{current_second}}", now.ToString("ss"))
                        ;
        }
    }
}
