using Izzy_Moonbot;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot_Tests;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Izzy_Moonbot.Helpers.DiscordHelper;

namespace Izzy_Moonbot_Tests.Services;

public class TestLogger<T> : ILogger<T>
{
    public List<string> Logs = new List<string>();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => throw new NotImplementedException();
    public bool IsEnabled(LogLevel logLevel) => throw new NotImplementedException();

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Logs.Add(formatter(state, exception));
    }
}

[TestClass()]
public class LoggingServiceTests
{
    [TestMethod()]
    public async Task BasicTests()
    {
        var logger = new TestLogger<Worker>();
        var logService = new LoggingService(logger);

        await logService.Log("test");
        TestUtils.AssertListsAreEqual(new List<string> { "test" }, logger.Logs);

        var (_, _, (_, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "good morning everypony");

        await logService.Log("sunny said something", context);
        TestUtils.AssertListsAreEqual(new List<string> {
            "test",
            "server: Maretime Bay (1) #general (1) @Sunny#1234 (2), sunny said something"
        }, logger.Logs);
    }
}
