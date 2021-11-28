namespace Izzy_Moonbot.Settings
{
    public static class DevSettings
    {
        //-------------------------Production-------------------------
        public static char prefix { get; } = '.'; //production prefix
        public static bool useDevPrefix { get; } = false; //Use the alternate prefix
        public static string RootPath { get; } = "botsettings"; //Production folders
        //------------------------------------------------------------

        ////-------------------------Development------------------------
        //public static char prefix { get; } = '.'; //development prefix
        //public static bool useDevPrefix { get; } = true; //Use the alternate prefix
        //public static string RootPath { get; } = "devsettings"; //Development folders
        ////------------------------------------------------------------
    }
}
