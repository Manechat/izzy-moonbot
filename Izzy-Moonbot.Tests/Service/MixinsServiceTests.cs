using System;
using Xunit;
using Izzy_Moonbot.Service;

namespace Cloudy_Canvas.Tests.Service
{
    public class MixinsServiceTests
    {
        [Theory]
        [InlineData("created_at:{{today}}, Lyra", "created_at:2021-04-23, Lyra")]
        [InlineData("created_at:{{current_year}}, Lyra", "created_at:2021, Lyra")]
        [InlineData("created_at:{{current_year}}-{{current_month}}, Lyra", "created_at:2021-04, Lyra")]
        [InlineData("created_at:{{current_year}}-{{current_month}}-{{current_day}}, Lyra", "created_at:2021-04-23, Lyra")]
        [InlineData("created_at:{{current_year}}-{{current_month}}-{{current_day}}T{{current_hour}}, Lyra", "created_at:2021-04-23T17, Lyra")]
        [InlineData("created_at:{{current_year}}-{{current_month}}-{{current_day}}T{{current_hour}}:{{current_minute}}, Lyra", "created_at:2021-04-23T17:20, Lyra")]
        [InlineData("created_at:{{current_year}}-{{current_month}}-{{current_day}}T{{current_hour}}:{{current_minute}}:{{current_second}}, Lyra", "created_at:2021-04-23T17:20:07, Lyra")]
        [InlineData("{{today}}{{today}}", "2021-04-232021-04-23")]//Replaces all 
        public void TranspileThoery(string query, string expected)
        {
            var helper = new MixinsService(new MockDateTimeService());
            var result = helper.Transpile(query);
            Assert.Equal(expected, result);
        }
    }

    public class MockDateTimeService : IDateTimeService
    {
        public DateTime UtcNow()
        {
            return new DateTime(2021, 4, 23, 17, 20, 7);
        }
    }
}
