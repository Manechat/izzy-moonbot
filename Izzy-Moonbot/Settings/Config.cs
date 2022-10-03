using System.Collections.Generic;

namespace Izzy_Moonbot.Settings;

public class Config
{
    public Config()
    {
        // Core settings
        Prefix = '.';
        SafeMode = true;
        ThreadOnlyMode = true;
        BatchSendLogs = false;
        BatchLogsSendRate = 10;
        IgnoredChannels = new HashSet<ulong>();
        IgnoredRoles = new HashSet<ulong>();
        MentionResponseEnabled = false;
        MentionResponses = new List<string>();
        MentionResponseCooldown = 600;
        DiscordActivityName = "you all soon";
        DiscordActivityWatching = true;

        // Mod settings
        AllowedUsers = new HashSet<ulong>();
        ModRole = 0;
        ModChannel = 0;
        LogChannel = 0;

        // User based settings
        ManageNewUserRoles = true;
        MemberRole = 0;
        NewMemberRole = 0;
        NewMemberRoleDecay = 0;

        // Filter Settings
        FilterEnabled = true;
        FilterMonitorEdits = true;
        FilterIgnoredChannels = new HashSet<ulong>();
        FilterBypassRoles = new HashSet<ulong>();
        FilteredWords = new Dictionary<string, List<string>>();
        FilterResponseDelete = new Dictionary<string, bool>();
        FilterResponseMessages = new Dictionary<string, string?>();
        FilterResponseSilence = new Dictionary<string, bool>();

        // Pressure settings
        SpamEnabled = true;
        SpamMonitorEdits = true;
        SpamEditReprocessThreshold = 10;
        FilterBypassRoles = new HashSet<ulong>();
        SpamIgnoredChannels = new HashSet<ulong>();
        SpamBasePressure = 10.0;
        SpamImagePressure = 8.3;
        SpamLengthPressure = 0.00625;
        SpamLinePressure = 0.714;
        SpamPingPressure = 2.5;
        SpamRepeatPressure = 10.0;
        SpamMaxPressure = 60.0;
        SpamPressureDecay = 2.5;

        // Raid settings
        RaidProtectionEnabled = true;
        NormalVerificationLevel = 3;
        RaidVerificationLevel = 4;
        AutoSilenceNewJoins = false;
        SmallRaidSize = 3;
        SmallRaidTime = 180;
        LargeRaidSize = 10;
        LargeRaidTime = 120;
        RecentJoinDecay = 300;
        SmallRaidDecay = 5;
        LargeRaidDecay = 30;
    }

    // Core settings
    public char Prefix { get; set; }
    public bool SafeMode { get; set; }
    public bool ThreadOnlyMode { get; set; }
    public bool BatchSendLogs { get; set; }
    public double BatchLogsSendRate { get; set; }
    public HashSet<ulong> IgnoredChannels { get; set; }
    public HashSet<ulong> IgnoredRoles { get; set; }
    public bool MentionResponseEnabled { get; set; }
    public List<string> MentionResponses { get; set; }
    public double MentionResponseCooldown { get; set; }
    public string? DiscordActivityName { get; set; }
    public bool DiscordActivityWatching { get; set; }

    // Moderation settings
    public HashSet<ulong> AllowedUsers { get; set; }
    public ulong ModRole { get; set; }
    public ulong ModChannel { get; set; }
    public ulong LogChannel { get; set; }

    // User based settings
    public bool ManageNewUserRoles { get; set; }
    public ulong? MemberRole { get; set; }
    public ulong? NewMemberRole { get; set; }
    public double NewMemberRoleDecay { get; set; }

    // Filter settings
    public bool FilterEnabled { get; set; }
    public bool FilterMonitorEdits { get; set; }
    public HashSet<ulong> FilterIgnoredChannels { get; set; }
    public HashSet<ulong> FilterBypassRoles { get; set; }
    public Dictionary<string, List<string>> FilteredWords { get; set; }
    public Dictionary<string, bool> FilterResponseDelete { get; set; }
    public Dictionary<string, string?> FilterResponseMessages { get; set; }
    public Dictionary<string, bool> FilterResponseSilence { get; set; }

    // Pressure settings
    public bool SpamEnabled { get; set; }
    public bool SpamMonitorEdits { get; set; }
    public int SpamEditReprocessThreshold { get; set; }
    public HashSet<ulong> SpamBypassRoles { get; set; }
    public HashSet<ulong> SpamIgnoredChannels { get; set; }
    public double SpamBasePressure { get; set; }
    public double SpamImagePressure { get; set; }
    public double SpamLengthPressure { get; set; }
    public double SpamLinePressure { get; set; }
    public double SpamPingPressure { get; set; }
    public double SpamRepeatPressure { get; set; }
    public double SpamMaxPressure { get; set; }
    public double SpamPressureDecay { get; set; }

    // Raid settings
    public bool RaidProtectionEnabled { get; set; }
    public int? NormalVerificationLevel { get; set; }
    public int? RaidVerificationLevel { get; set; }
    public bool AutoSilenceNewJoins { get; set; }
    public int SmallRaidSize { get; set; }
    public double SmallRaidTime { get; set; }
    public int LargeRaidSize { get; set; }
    public double LargeRaidTime { get; set; }
    public double RecentJoinDecay { get; set; }
    public double? SmallRaidDecay { get; set; }
    public double? LargeRaidDecay { get; set; }
}