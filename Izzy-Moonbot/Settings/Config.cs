using System;
using System.Collections.Generic;
using Izzy_Moonbot.EventListeners;
using Izzy_Moonbot.Types;

namespace Izzy_Moonbot.Settings;

public class Config
{
    public event EventHandler<ConfigValueChangeEvent>? Changed;
    
    public Config()
    {
        // Setup settings
        Prefix = '.';
        ModRole = 0;
        ModChannel = 0;
        LogChannel = 0;

        // Misc settings
        UnicycleInterval = 100;
        MentionResponseEnabled = false;
        MentionResponses = new HashSet<string>();
        MentionResponseCooldown = 600;
        DiscordActivityName = "you all soon";
        DiscordActivityWatching = true;
        Aliases = new Dictionary<string, string>();
        FirstRuleMessageId = 0;
        HiddenRules = new Dictionary<string, string>();
        BestPonyChannel = 0;
        RecentMessagesPerUser = 10;

        // Banner settings
        _bannerMode = ConfigListener.BannerMode.None;
        _bannerInterval = 60;
        BannerImages = new HashSet<string>();

        // ManagedRoles settings
        ManageNewUserRoles = false;
        MemberRole = 0;
        NewMemberRole = 0;
        NewMemberRoleDecay = 0;
        RolesToReapplyOnRejoin = new HashSet<ulong>();

        // Filter Settings
        FilterEnabled = true;
        FilterIgnoredChannels = new HashSet<ulong>();
        FilterBypassRoles = new HashSet<ulong>();
        FilterDevBypass = true;
        FilterWords = new HashSet<string>();

        // Spam settings
        SpamEnabled = true;
        SpamBypassRoles = new HashSet<ulong>();
        SpamIgnoredChannels = new HashSet<ulong>();
        SpamDevBypass = true;
        SpamBasePressure = 10.0;
        SpamImagePressure = 8.3;
        SpamLengthPressure = 0.00625;
        SpamLinePressure = 0.714;
        SpamPingPressure = 2.5;
        SpamRepeatPressure = 10.0;
        SpamUnusualCharacterPressure = 0.01;
        SpamMaxPressure = 60.0;
        SpamPressureDecay = 2.5;
        SpamMessageDeleteLookback = 60;

        // Raid settings
        RaidProtectionEnabled = true;
        AutoSilenceNewJoins = false;
        SmallRaidSize = 3;
        LargeRaidSize = 6;
        RecentJoinDecay = 120;
        SmallRaidDecay = 5;
        LargeRaidDecay = 30;

        // Bored settings
        _boredChannel = 0;
        _boredCooldown = 300;
        BoredCommands = new HashSet<string>();

        // Witty settings
        Witties = new Dictionary<string, string>();
        WittyChannels = new HashSet<ulong>();
        WittyCooldown = 300;
    }

    // Core settings
    public char Prefix { get; set; }
    public int UnicycleInterval { get; set; }
    public bool MentionResponseEnabled { get; set; }
    public HashSet<string> MentionResponses { get; set; }
    public double MentionResponseCooldown { get; set; }
    public string? DiscordActivityName { get; set; }
    public bool DiscordActivityWatching { get; set; }
    public Dictionary<string, string> Aliases { get; set; }
    public ulong FirstRuleMessageId { get; set; }
    public Dictionary<string, string> HiddenRules { get; set; }
    public ulong BestPonyChannel { get; set; }
    public int RecentMessagesPerUser { get; set; }

    // Server settings
    private ConfigListener.BannerMode _bannerMode { get; set; }
    public ConfigListener.BannerMode BannerMode {
        get => _bannerMode;
        set
        {
            var eventData = new ConfigValueChangeEvent("BannerMode", _bannerMode, value);
            Changed?.Invoke(this, eventData);
            _bannerMode = value;
        }
    }
    private double _bannerInterval { get; set; }
    public double BannerInterval
    {
        get => _bannerInterval;
        set
        {
            var eventData = new ConfigValueChangeEvent("BannerInterval", _bannerInterval, value);
            Changed?.Invoke(this, eventData);
            _bannerInterval = value;
        }
    }
    public HashSet<string> BannerImages { get; set; }
    

    // Moderation settings
    public ulong ModRole { get; set; }
    public ulong ModChannel { get; set; }
    public ulong LogChannel { get; set; }

    // User based settings
    public bool ManageNewUserRoles { get; set; }
    public ulong? MemberRole { get; set; }
    public ulong? NewMemberRole { get; set; }
    public double NewMemberRoleDecay { get; set; }
    public HashSet<ulong> RolesToReapplyOnRejoin { get; set; }

    // Filter settings
    public bool FilterEnabled { get; set; }
    public HashSet<ulong> FilterIgnoredChannels { get; set; }
    public HashSet<ulong> FilterBypassRoles { get; set; }
    public bool FilterDevBypass { get; set; }
    public HashSet<string> FilterWords { get; set; }

    // Pressure settings
    public bool SpamEnabled { get; set; }
    public HashSet<ulong> SpamBypassRoles { get; set; }
    public HashSet<ulong> SpamIgnoredChannels { get; set; }
    public bool SpamDevBypass { get; set; }
    public double SpamBasePressure { get; set; }
    public double SpamImagePressure { get; set; }
    public double SpamLengthPressure { get; set; }
    public double SpamLinePressure { get; set; }
    public double SpamPingPressure { get; set; }
    public double SpamRepeatPressure { get; set; }
    public double SpamUnusualCharacterPressure { get; set; }
    public double SpamMaxPressure { get; set; }
    public double SpamPressureDecay { get; set; }
    public double? SpamMessageDeleteLookback { get; set; }

    // Raid settings
    public bool RaidProtectionEnabled { get; set; }
    public bool AutoSilenceNewJoins { get; set; }
    public int SmallRaidSize { get; set; }
    public int LargeRaidSize { get; set; }
    public double RecentJoinDecay { get; set; }
    public double? SmallRaidDecay { get; set; }
    public double? LargeRaidDecay { get; set; }

    // Bored settings
    private ulong _boredChannel { get; set; }
    public ulong BoredChannel
    {
        get => _boredChannel;
        set
        {
            var eventData = new ConfigValueChangeEvent("BoredChannel", _boredChannel, value);
            Changed?.Invoke(this, eventData);
            _boredChannel = value;
        }
    }
    private double _boredCooldown { get; set; }
    public double BoredCooldown
    {
        get => _boredCooldown;
        set
        {
            var eventData = new ConfigValueChangeEvent("BoredCooldown", _boredCooldown, value);
            Changed?.Invoke(this, eventData);
            _boredCooldown = value;
        }
    }
    public HashSet<string> BoredCommands { get; set; }

    // Witty settings
    public Dictionary<string, string> Witties { get; set; }
    public HashSet<ulong> WittyChannels { get; set; }
    public double WittyCooldown { get; set; }
}
