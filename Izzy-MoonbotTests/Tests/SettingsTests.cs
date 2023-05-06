using Izzy_Moonbot.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Izzy_Moonbot_Tests.Settings;

[TestClass()]
public class SettingsTests
{
    [TestMethod()]
    public void ConstructSettingsAndTypesObjects()
    {
        new BooruSettings(); // regression test for the bug fixed in PR #244

        new Config();

        // skip DevSettings because it's a static class

        new DiscordSettings();
        new GeneralStorage();
        new QuoteStorage();
        new State();

        new User();
        new Dictionary<ulong, User>();

        new List<ScheduledJob>();
    }

    // Ideally we'd compare the actual settings objects as well as their serialized forms, but C# doesn't provide deep
    // comparisons by default, and writing our own by hand only for tests would be textbook tautologcal testing.
    [TestMethod()]
    public void RoundTripSerializeSettingsObjects()
    {
        var config = new Config();
        var fileContents = JsonConvert.SerializeObject(config, Formatting.Indented);
        var config2 = JsonConvert.DeserializeObject<Config>(fileContents);
        var fileContents2 = JsonConvert.SerializeObject(config2, Formatting.Indented);
        Assert.AreEqual(fileContents, fileContents2);

        var users = new Dictionary<ulong, User> { { 123, new User() } };
        fileContents = JsonConvert.SerializeObject(users, Formatting.Indented);
        var users2 = JsonConvert.DeserializeObject<Dictionary<ulong, User>>(fileContents);
        fileContents2 = JsonConvert.SerializeObject(users2, Formatting.Indented);
        Assert.AreEqual(fileContents, fileContents2);

        // skip schedule because it has custom (de)serialization logic

        var generalStorage = new GeneralStorage();
        fileContents = JsonConvert.SerializeObject(generalStorage, Formatting.Indented);
        var generalStorage2 = JsonConvert.DeserializeObject<GeneralStorage>(fileContents);
        fileContents2 = JsonConvert.SerializeObject(generalStorage2, Formatting.Indented);
        Assert.AreEqual(fileContents, fileContents2);

        var quoteStorage = new QuoteStorage();
        fileContents = JsonConvert.SerializeObject(quoteStorage, Formatting.Indented);
        var quoteStorage2 = JsonConvert.DeserializeObject<QuoteStorage>(fileContents);
        fileContents2 = JsonConvert.SerializeObject(quoteStorage2, Formatting.Indented);
        Assert.AreEqual(fileContents, fileContents2);

        var state = new State();
        fileContents = JsonConvert.SerializeObject(state, Formatting.Indented);
        var state2 = JsonConvert.DeserializeObject<State>(fileContents);
        fileContents2 = JsonConvert.SerializeObject(state2, Formatting.Indented);
        Assert.AreEqual(fileContents, fileContents2);
    }
}
