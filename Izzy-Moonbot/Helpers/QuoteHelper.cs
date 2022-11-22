using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Izzy_Moonbot.Helpers;

public static class QuoteHelper
{
    public static (string, int?) ParseQuoteArgs(string argsString)
    {
        var search = argsString;
        var number = int.MinValue;

        var args = DiscordHelper.GetArguments(argsString);

        if (args.Arguments.Length >= 2)
        {
            if (!int.TryParse(args.Arguments[1], out number))
            {
                number = int.MinValue;
                foreach (var s in argsString.Split(" "))
                {
                    if (int.TryParse(s, out number))
                    {
                        // Found the number, all content before it is search
                        var index = argsString.Split(" ").ToList().IndexOf(s);
                        search = string.Join(" ", argsString.Split(" ")[new Range(0, index)]);
                        break;
                    }
                    else
                    {
                        number = int.MinValue;
                    }
                }
            }
            else
            {
                search = args.Arguments[0];
            }
        }

        search = DiscordHelper.StripQuotes(search);

        return number == int.MinValue
            ? (search, null)
            : (search, number);
    }
}