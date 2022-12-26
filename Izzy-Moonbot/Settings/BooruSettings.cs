namespace Izzy_Moonbot.Settings;

public class BooruSettings
{
    public string Token { get; set; }
    public string Endpoint { get; set; }
    public string Version { get; set; }

    public BooruSettings(string token, string endpoint, string version)
    {
        Token = token;
        Endpoint = endpoint;
        Version = version;
    }
}