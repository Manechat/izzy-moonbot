using Discord;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Describers;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Izzy_Moonbot_Tests.Helpers;

[TestClass()]
public class PaginationHelperTests
{
    [TestMethod()]
    public void BasicPagingTests()
    {
        var (cfg, cd, (izzyHerself, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();

        var context = client.AddMessage(guild.Id, generalChannel.Id, sunny.Id, "make some pages");

        var ph = new PaginationHelper(
            context,
            new string[] { "Twilight became Twilicorn", "Everypony lived happily ever after", "Then Opaline ruined it" },
            new string[] { "Once upon a time...", "...the end!" }
        );

        var paginatedMessage = generalChannel.Messages.Last();
        var firstContent = paginatedMessage.Content;
        Assert.AreEqual($"Once upon a time...{Environment.NewLine}" +
            $"```{Environment.NewLine}" +
            $"Twilight became Twilicorn{Environment.NewLine}" +
            $"````Page 1 out of 3`{Environment.NewLine}" +
            $"...the end!{Environment.NewLine}" +
            $"{Environment.NewLine}", firstContent);

        client.FireButtonExecuted(sunny.Id, paginatedMessage.Id, "goto-next");
        Assert.AreEqual($"Once upon a time...{Environment.NewLine}" +
            $"```{Environment.NewLine}" +
            $"Everypony lived happily ever after{Environment.NewLine}" +
            $"````Page 2 out of 3`{Environment.NewLine}" +
            $"...the end!{Environment.NewLine}" +
            $"{Environment.NewLine}", paginatedMessage.Content);

        client.FireButtonExecuted(sunny.Id, paginatedMessage.Id, "goto-next");
        Assert.AreEqual($"Once upon a time...{Environment.NewLine}" +
            $"```{Environment.NewLine}" +
            $"Then Opaline ruined it{Environment.NewLine}" +
            $"````Page 3 out of 3`{Environment.NewLine}" +
            $"...the end!{Environment.NewLine}" +
            $"{Environment.NewLine}", paginatedMessage.Content);

        client.FireButtonExecuted(sunny.Id, paginatedMessage.Id, "goto-start");
        Assert.AreEqual(firstContent, paginatedMessage.Content);

        // passing the wrong ids doesn't do anything
        client.FireButtonExecuted(999, 999, "asdf");
        Assert.AreEqual(firstContent, paginatedMessage.Content);
    }

}
