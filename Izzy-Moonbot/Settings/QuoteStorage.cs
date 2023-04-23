using System.Collections.Generic;

namespace Izzy_Moonbot.Settings;

public class QuoteStorage
{
    public QuoteStorage()
    {
        Quotes = new Dictionary<
            string, // either a stringified user id, or a category name
            List<string>
        >();
        Aliases = new Dictionary<string, string>();
    }

    public Dictionary<string, List<string>> Quotes { get; set; }
    public Dictionary<string, string> Aliases { get; set; }
}