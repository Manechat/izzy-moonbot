using Microsoft.VisualStudio.TestTools.UnitTesting;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Modules;

namespace Izzy_Moonbot_Tests.Modules;

[TestClass()]
public class QuoteModuleTests
{
    [TestMethod()]
    public async Task QuoteCommand_Tests()
    {
        // setup

        var (cfg, _, (_, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;

        // only one quote so that the "random" selection is deterministic for now
        var quotes = new QuoteStorage();
        quotes.Quotes.Add(sunny.Id.ToString(), new List<string> { "gonna be my day" });

        var userinfo = new Dictionary<ulong, User>();
        var qs = new QuoteService(quotes, userinfo);
        var qm = new QuotesModule(cfg, qs, userinfo);

        // setup end

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".quote");
        await qm.TestableQuoteCommandAsync(context, "");
        Assert.AreEqual($"**{sunny.GlobalName}**, #1: gonna be my day", generalChannel.Messages.Last().Content);

        //

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".quote Sunny");
        await qm.TestableQuoteCommandAsync(context, "Sunny");
        Assert.AreEqual($"**{sunny.GlobalName}**, #1: gonna be my day", generalChannel.Messages.Last().Content);

        //

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".quote <@{sunny.Id}>");
        await qm.TestableQuoteCommandAsync(context, $"<@{sunny.Id}>");
        Assert.AreEqual($"**{sunny.GlobalName}**, #1: gonna be my day", generalChannel.Messages.Last().Content);

        //

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".quote <@{sunny.Id}> 1");
        await qm.TestableQuoteCommandAsync(context, $"<@{sunny.Id}> 1");
        Assert.AreEqual($"**{sunny.GlobalName}**, #1: gonna be my day", generalChannel.Messages.Last().Content);

        //

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".quote <@{sunny.Id}> 2");
        await qm.TestableQuoteCommandAsync(context, $"<@{sunny.Id}> 2");
        Assert.AreEqual("I couldn't find that quote, sorry!", generalChannel.Messages.Last().Content);

        //

        // Zipp has no quotes because she never appeared on the pippcast
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".quote Zipp");
        await qm.TestableQuoteCommandAsync(context, "Zipp");
        Assert.AreEqual("I couldn't find any quotes for that user.", generalChannel.Messages.Last().Content);

        //

        // Twi didn't make it to G5
        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".quote Twilight");
        await qm.TestableQuoteCommandAsync(context, "Twilight");
        Assert.AreEqual("I was unable to find the user you asked for. Sorry!", generalChannel.Messages.Last().Content);

        // TODO: test cases where user left (both cached in userinfo and not)
    }

    [TestMethod()]
    public async Task ListQuotes_Tests()
    {
        // setup

        var (cfg, _, (izzy, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        var pipp = guild.Users[3];
        DiscordHelper.DefaultGuildId = guild.Id;

        var quotes = new QuoteStorage();
        quotes.Quotes.Add(sunny.Id.ToString(), new List<string> { "gonna be my day", "eat more vegetables" });
        quotes.Quotes.Add(izzy.Id.ToString(), new List<string> { "let's unicycle it" });
        quotes.Quotes.Add(pipp.Id.ToString(), new List<string> {
            "Heeeey pippsqueaks! <https://youtu.be/CLT4aSurqCg> Check out my latest sooong!",
            "It may looks scary but don't be afraid~ cuz nothin's what it seems at a monster par-tay! https://youtu.be/CLT4aSurqCg"
         });

        var userinfo = new Dictionary<ulong, User>();
        var qs = new QuoteService(quotes, userinfo);
        var qm = new QuotesModule(cfg, qs, userinfo);

        // setup end

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".listquotes");
        await qm.TestableListQuotesCommandAsync(context, "");

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Here's all the");
        StringAssert.Contains(description, "```\n" +
            "Sunny (Sunny/2) \n" +
            "Izzy Moonbot (Izzy Moonbot/1) \n" +
            "Pipp (Pipp/4) \n" +
            "```\n");
        StringAssert.Contains(description, "Run `.quote <user>`");
        StringAssert.Contains(description, "Run `.quote`");

        //

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".listquotes Izzy");
        await qm.TestableListQuotesCommandAsync(context, "Izzy");

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "all the quotes");
        StringAssert.Contains(description, $"for **{izzy.GlobalName}**:");
        StringAssert.Contains(description, "\n" +
            $"1\\. let's unicycle it\n" +
            "\n");
        StringAssert.Contains(description, "Run `.quote <user> <number>` to");
        StringAssert.Contains(description, "Run `.quote <user>` to");
        StringAssert.Contains(description, "Run `.quote` for");

        //

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".listquotes Sunny");
        await qm.TestableListQuotesCommandAsync(context, "Sunny");

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "all the quotes");
        StringAssert.Contains(description, $"for **{sunny.GlobalName}**:");
        StringAssert.Contains(description, "\n" +
            "1\\. gonna be my day\n" +
            "2\\. eat more vegetables\n" +
            "\n");
        StringAssert.Contains(description, "Run `.quote <user> <number>` to");
        StringAssert.Contains(description, "Run `.quote <user>` to");
        StringAssert.Contains(description, "Run `.quote` for");

        //

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".listquotes Zipp");
        await qm.TestableListQuotesCommandAsync(context, "Zipp");

        Assert.AreEqual("I couldn't find any quotes for that user.", generalChannel.Messages.Last().Content);

        //

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".listquotes Twilight");
        await qm.TestableListQuotesCommandAsync(context, "Twilight");

        Assert.AreEqual("I was unable to find the user you asked for. Sorry!", generalChannel.Messages.Last().Content);

        //

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".listquotes Pipp");
        await qm.TestableListQuotesCommandAsync(context, "Pipp");

        description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "all the quotes");
        StringAssert.Contains(description, $"for **{pipp.GlobalName}**:");
        StringAssert.Contains(description, "\n" +
            "1\\. Heeeey pippsqueaks! <https://youtu.be/CLT4aSurqCg> Check out my latest sooong!\n" +
            "2\\. It may looks scary but don't be afraid~ cuz nothin's what it seems at a monster par-tay! <https://youtu.be/CLT4aSurqCg>\n" +
            "\n");
        StringAssert.Contains(description, "Run `.quote <user> <number>` to");
        StringAssert.Contains(description, "Run `.quote <user>` to");
        StringAssert.Contains(description, "Run `.quote` for");
    }

    [TestMethod()]
    public async Task ListQuotes_ExternalUsers_Tests()
    {
        // setup

        var (cfg, _, (izzy, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;

        // Test with a quote from a user id that is no longer in our guild
        var quotes = new QuoteStorage();
        quotes.Quotes.Add("1234", new List<string> { "minty was here" });

        var userinfo = new Dictionary<ulong, User>();
        var qs = new QuoteService(quotes, userinfo);
        var qm = new QuotesModule(cfg, qs, userinfo);

        // setup end

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".listquotes");
        await qm.TestableListQuotesCommandAsync(context, "");

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "Here's all the");
        StringAssert.Contains(description, "```\n" +
            "1234 \n" +
            "```\n");
        StringAssert.Contains(description, "Run `.quote <user>`");
        StringAssert.Contains(description, "Run `.quote`");
    }

    // Regression test: This case used to print 1: ... 1: ... instead of 1: ... 2: ...
    [TestMethod()]
    public async Task ListQuotes_Duplicates_Tests()
    {
        // setup

        var (cfg, _, (izzy, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        DiscordHelper.DefaultGuildId = guild.Id;

        var quotes = new QuoteStorage();
        quotes.Quotes.Add(sunny.Id.ToString(), new List<string> { "gonna be my day", "gonna be my day" });

        var userinfo = new Dictionary<ulong, User>();
        var qs = new QuoteService(quotes, userinfo);
        var qm = new QuotesModule(cfg, qs, userinfo);

        // setup end

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".listquotes Sunny");
        await qm.TestableListQuotesCommandAsync(context, "Sunny");

        var description = generalChannel.Messages.Last().Content;
        StringAssert.Contains(description, "all the quotes");
        StringAssert.Contains(description, $"for **{sunny.GlobalName}**:");
        StringAssert.Contains(description, "\n" +
            "1\\. gonna be my day\n" +
            "2\\. gonna be my day\n" +
            "\n");
        StringAssert.Contains(description, "Run `.quote <user> <number>` to");
        StringAssert.Contains(description, "Run `.quote <user>` to");
        StringAssert.Contains(description, "Run `.quote` for");
    }

    [TestMethod()]
    public async Task AddAndRemoveQuotes_Tests()
    {
        // setup

        var (cfg, _, (_, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();
        var pipp = guild.Users[3];
        DiscordHelper.DefaultGuildId = guild.Id;

        var quotes = new QuoteStorage();
        quotes.Quotes.Add(sunny.Id.ToString(), new List<string> { "gonna be my day" });

        var userinfo = new Dictionary<ulong, User>();
        var qs = new QuoteService(quotes, userinfo);
        var qm = new QuotesModule(cfg, qs, userinfo);

        // setup end

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".addquote");
        await qm.TestableAddQuoteCommandAsync(context, "");

        Assert.AreEqual("You need to tell me the user you want to add the quote to, and the content of the quote.", generalChannel.Messages.Last().Content);
        TestUtils.AssertListsAreEqual(quotes.Quotes[sunny.Id.ToString()], new List<string> {
            "gonna be my day"
        });

        //

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".addquote Sunny eat more vegetables");
        await qm.TestableAddQuoteCommandAsync(context, "Sunny eat more vegetables");

        Assert.AreEqual($"Added quote #2 to **{sunny.GlobalName}**:\n" +
            "\n" +
            ">>> eat more vegetables", generalChannel.Messages.Last().Content);
        TestUtils.AssertListsAreEqual(quotes.Quotes[sunny.Id.ToString()], new List<string> {
            "gonna be my day",
            "eat more vegetables"
        });

        //

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".addquote Pipp Heeeey pippsqueaks! <https://youtu.be/CLT4aSurqCg> Check out my latest sooong!");
        await qm.TestableAddQuoteCommandAsync(context, "Pipp Heeeey pippsqueaks! <https://youtu.be/CLT4aSurqCg> Check out my latest sooong!");

        Assert.AreEqual($"Added quote #1 to **{pipp.GlobalName}**:\n" +
            "\n" +
            ">>> Heeeey pippsqueaks! <https://youtu.be/CLT4aSurqCg> Check out my latest sooong!", generalChannel.Messages.Last().Content);

        //

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".addquote It may looks scary but don't be afraid~ cuz nothin's what it seems at a monster par-tay! https://youtu.be/CLT4aSurqCg");
        await qm.TestableAddQuoteCommandAsync(context, "Pipp It may looks scary but don't be afraid~ cuz nothin's what it seems at a monster par-tay! https://youtu.be/CLT4aSurqCg");

        Assert.AreEqual($"Added quote #2 to **{pipp.GlobalName}**:\n" +
            "\n" +
            ">>> It may looks scary but don't be afraid~ cuz nothin's what it seems at a monster par-tay! <https://youtu.be/CLT4aSurqCg>", generalChannel.Messages.Last().Content);

        //

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, ".removequote");
        await qm.TestableRemoveQuoteCommandAsync(context, "");

        Assert.AreEqual("You need to tell me the user you want to remove the quote from, and the quote number to remove.", generalChannel.Messages.Last().Content);
        TestUtils.AssertListsAreEqual(quotes.Quotes[sunny.Id.ToString()], new List<string> {
            "gonna be my day",
            "eat more vegetables"
        });

        //

        context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, $".removequote <@{sunny.Id}> 1");
        await qm.TestableRemoveQuoteCommandAsync(context, $"<@{sunny.Id}> 1");

        Assert.AreEqual($"Removed quote #1 from **{sunny.GlobalName}**.", generalChannel.Messages.Last().Content);
        TestUtils.AssertListsAreEqual(quotes.Quotes[sunny.Id.ToString()], new List<string> {
            "eat more vegetables"
        });
    }

    // TODO: .quotealias command, and aliases in general
}
