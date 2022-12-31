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
    public async Task BasicPagingTestsAsync()
    {
        var (cfg, cd, (izzyHerself, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "make some pages");

        PaginationHelper.PaginateIfNeededAndSendMessage(context,
            "Once upon a time...",
            new string[] { "Twilight became Twilicorn", "Everypony lived happily ever after", "Then Opaline ruined it" },
            "...the end!",
            pageSize: 1,
            codeblock: true
        );

        var paginatedMessage = generalChannel.Messages.Last();
        var firstContent = paginatedMessage.Content;
        Assert.AreEqual($"Once upon a time...\n" +
            $"```\n" +
            $"Twilight became Twilicorn\n" +
            $"````Page 1 out of 3`\n" +
            $"...the end!\n" +
            $"\n", firstContent);

        client.FireButtonExecuted(sunny.Id, paginatedMessage.Id, "goto-next");
        Assert.AreEqual($"Once upon a time...\n" +
            $"```\n" +
            $"Everypony lived happily ever after\n" +
            $"````Page 2 out of 3`\n" +
            $"...the end!\n" +
            $"\n", paginatedMessage.Content);

        client.FireButtonExecuted(sunny.Id, paginatedMessage.Id, "goto-next");
        Assert.AreEqual($"Once upon a time...\n" +
            $"```\n" +
            $"Then Opaline ruined it\n" +
            $"````Page 3 out of 3`\n" +
            $"...the end!\n" +
            $"\n", paginatedMessage.Content);

        client.FireButtonExecuted(sunny.Id, paginatedMessage.Id, "goto-start");
        Assert.AreEqual(firstContent, paginatedMessage.Content);

        // passing the wrong ids doesn't do anything
        client.FireButtonExecuted(999, 999, "asdf");
        Assert.AreEqual(firstContent, paginatedMessage.Content);
    }

    [TestMethod()]
    public async Task PageTruncation_TestsAsync()
    {
        var (_, _, (_, sunny), _, (generalChannel, _, _), guild, client) = TestUtils.DefaultStubs();

        var context = await client.AddMessageAsync(guild.Id, generalChannel.Id, sunny.Id, "make some pages");

        PaginationHelper.PaginateIfNeededAndSendMessage(context,
            // ~700 character header
            "NANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-NA-NA-NA-NA-\n" +
            "NANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-NA-NA-NA-NA-\n" +
            "NANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-NA-NA-NA-NA-\n" +
            "NANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-NA-NA-NA-NA-",
            // ~1,500 characters of Pony Ipsum
            new string[] {
                "Twilight Sparkle Silver Spoon tail unicorns. Flash Sentry Gummy Lord Tirek wing Equestria Gilda Mr. Cake. Cheese Sandwich Prim Hemline chaos Princess Celestia Mrs. Cake. Magic mane Snips Lightning Dust, Zecora friendship Flam friends wing Princess Celestia Wonderbolts Suri Polomare.",
                "Derpy rainbow power Ms. Peachbottom mane. Peewee Featherweight unicorn flank. Cupcake Prim Hemline Daring Do Cheerilee Cherry Jubilee. Apple Fluttershy Gilda Wonderbolts Mr. Cake flank Apple Jack.",
                "Owlowiscious Trenderhoof Lyra, Sweetie Drops Peewee Snails Cheerilee Lord Tirek. Winona Philomena Everfree Forest gryphon Spitfire waterfall kindness, Daring Do Silver Spoon King Sombra. Kindness honesty Filthy Rich pegasai Bloomberg Nightmare Moon. Bloomberg friend pegasus Gilda Donut Joe sun. Fluttershy Winona Silver Shill party Opalescence Lord Tirek.",
                "Owlowiscious cutie mark Twilight Sparkle Tank apples hot air balloon Opalescence Zecora hooves Sweetie Belle Caramel Prince Blueblood friend Pinkie Pie. Apples horn Mr. Cake Daring Do, Ms. Peachbottom hay Snails pies Trixie hooves. Trixie Aria Blaze A. K. Yearling Cherry Jubilee. Diamond Tiara mane Flam, Pipsqueak apples Ponyville Vinyl Scratch Cranky Doodle Donkey kindness Angel Rarity Nightmare Moon Canterlot. Hay generosity apples sun Princess Luna, Owlowiscious King Sombra Snails Snips apple gryphon.",
                "You're going to love me! Cloud hooves Mayor Mare Equestria. And that's how Equestria was made! Muffin Gilda moon Goldie Delicious Pumpkin Cake waterfall.",
                "6th item you'll never see so there's a 2nd page",
            },
            // ~700 character footer
            "NANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-NA-NA-NA-NA-\n" +
            "NANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-NA-NA-NA-NA-\n" +
            "NANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-NA-NA-NA-NA-\n" +
            "NANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-NA-NA-NA-NA-",
            pageSize: 5,
            codeblock: true
        );

        var paginatedMessage = generalChannel.Messages.Last();
        Assert.AreEqual(
            "NANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-NA-NA-NA-NA-\n" +
            "NANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-NA-NA-NA-NA-\n" +
            "NANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-NA-NA-NA-NA-\n" +
            "NANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-NA-NA-NA-NA-\n" +
            "⚠️ Some items needed to be truncated```\n" +
            "Twilight Sparkle Silver Spoon tail unicorns. Flash Sentry Gummy Lord Tir[...]\n" +
            "Derpy rainbow power Ms. Peachbottom mane. Peewee Featherweight unicorn f[...]\n" +
            "Owlowiscious Trenderhoof Lyra, Sweetie Drops Peewee Snails Cheerilee Lor[...]\n" +
            "Owlowiscious cutie mark Twilight Sparkle Tank apples hot air balloon Opa[...]\n" +
            "You're going to love me! Cloud hooves Mayor Mare Equestria. And that's h[...]\n" +
            "````Page 1 out of 2`\n" +
            "NANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-NA-NA-NA-NA-\n" +
            "NANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-NA-NA-NA-NA-\n" +
            "NANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-NA-NA-NA-NA-\n" +
            "NANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-\nNANA-NANANANANA-NA-NA-NA, NA-NA-NANA-NA-NA-NA-NA-NA-\n" +
            "\n",
            paginatedMessage.Content);
    }
}
