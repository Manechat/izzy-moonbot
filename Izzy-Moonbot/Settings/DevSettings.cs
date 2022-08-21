namespace Izzy_Moonbot.Settings;

public static class DevSettings
{
    //-------------------------Production-------------------------
    public static char Prefix { get; } = '.'; //production prefix
    public static bool UseDevPrefix { get; } = false; //Use the alternate prefix

    public static string RootPath { get; } = "botsettings"; //Production folders
    //------------------------------------------------------------

    ////-------------------------Development------------------------
    //public static char Prefix { get; } = '.'; //development prefix
    //public static bool UseDevPrefix { get; } = true; //Use the alternate prefix
    //public static string RootPath { get; } = "devsettings"; //Development folders
    ////------------------------------------------------------------
}