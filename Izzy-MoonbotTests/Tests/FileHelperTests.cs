using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Izzy_Moonbot_Tests.Helpers;

// The main goal here is to ensure that when we change one of these file formats in the future, we do so in a way
// that's backwards compatible with the existing files on production. Thus, whenever you want to migrate file formats,
// these tests should be changed to use both the old and new formats (explicitly checking for any auto-defaulting or
// auto-converting we're doing) at least until the production state is only using the newer formats.
[TestClass()]
public class FileHelperTests
{
    // regression test for the bug fixed in PR #255
    [TestMethod()]
    public void ScheduleRoundTrip()
    {
        var testEmptySchedule = """
            []
            """;

        var jobs = FileHelper.TestableDeserializeSchedule(testEmptySchedule);
        var serialized = FileHelper.TestableSerializeSchedule(jobs);
        Assert.AreEqual(testEmptySchedule, serialized);

        var testMixedSchedule = """
            [
              {
                "Id": "41eeae89-6488-44b2-9a35-c52b69d8017c",
                "CreatedAt": "2023-05-06T16:27:02.2577592+00:00",
                "LastExecutedAt": null,
                "ExecuteAt": "2024-05-06T16:27:02.2521832+00:00",
                "Action": {
                  "ChannelOrUser": 283037539755884554,
                  "Content": "test reminder",
                  "Type": 3
                },
                "RepeatType": 0
              },
              {
                "Id": "0e61fee0-fcd6-4609-871f-4127b6bd1ec7",
                "CreatedAt": "2023-05-06T16:27:27.4640875+00:00",
                "LastExecutedAt": null,
                "ExecuteAt": "2023-05-06T18:27:27.4640877+00:00",
                "Action": {
                  "Role": 1039194817231601695,
                  "User": 908806033156227134,
                  "Reason": "New member role removal, 120 minutes (`NewMemberRoleDecay`) passed.",
                  "Type": 0
                },
                "RepeatType": 0
              },
              {
                "Id": "0a603ae8-e82a-48c1-97d8-53c9d062f1a1",
                "CreatedAt": "2023-05-06T16:28:01.719841+00:00",
                "LastExecutedAt": null,
                "ExecuteAt": "2023-05-06T16:29:01.719841+00:00",
                "Action": {
                  "LastBannerIndex": null,
                  "Type": 4
                },
                "RepeatType": 1
              },
              {
                "Id": "857fd853-2bf1-483c-8917-8dee97631fa0",
                "CreatedAt": "2023-05-06T16:29:22.901023+00:00",
                "LastExecutedAt": null,
                "ExecuteAt": "2023-05-06T16:29:32.2954305+00:00",
                "Action": {
                  "User": 908806033156227134,
                  "Reason": "Command `.ban <@908806033156227134> 10 seconds` was run by Ixrec#7992 (283037539755884554) in #general (963788928702373918) at 06/05/2023 16:29:22 +00:00 (<t:1683390562298>)",
                  "Type": 2
                },
                "RepeatType": 0
              }
            ]
            """;

        jobs = FileHelper.TestableDeserializeSchedule(testMixedSchedule);
        serialized = FileHelper.TestableSerializeSchedule(jobs);
        Assert.AreEqual(testMixedSchedule, serialized);
    }

    [TestMethod()]
    public void GeneralStorageRoundTrip()
    {
        var testGeneralStorage = """
            {
              "CurrentRaidMode": 0,
              "ManualRaidSilence": false,
              "CurrentBooruFeaturedImage": null,
              "LastRollTime": "2023-05-01T10:09:04.7142987Z",
              "UsersWhoRolledToday": [
                283037539755884554
              ]
            }
            """;

        var generalStorage = JsonConvert.DeserializeObject<GeneralStorage>(testGeneralStorage);
        var serialized = JsonConvert.SerializeObject(generalStorage, Formatting.Indented).Replace("\r\n", "\n");
        Assert.AreEqual(testGeneralStorage, serialized);
    }

    [TestMethod()]
    public void ConfigRoundTrip()
    {
        var testConfig = """
            {
              "Prefix": ".",
              "UnicycleInterval": 100,
              "MentionResponseEnabled": true,
              "MentionResponses": [
                "ohayo sekai good morning world",
                "???"
              ],
              "MentionResponseCooldown": 10.0,
              "DiscordActivityName": "you all soon",
              "DiscordActivityWatching": true,
              "Aliases": {
                "testalias": "echo Test successful!",
                "aliasfail": "",
                "member": "assignrole <@&973220758736216084>",
                "a": "ass"
              },
              "FirstRuleMessageId": 994643448948850788,
              "HiddenRules": {
                "-1": "<a:twiactually:613109204118667293>",
                "test": "hello"
              },
              "BannerMode": 0,
              "BannerInterval": 1.0,
              "BannerImages": [
                "test1",
                "test2",
                "test3",
                "test4",
                "test5",
                "test6"
              ],
              "ModRole": 964283794083446804,
              "ModChannel": 973218854237007963,
              "LogChannel": 964283764240973844,
              "ManageNewUserRoles": true,
              "MemberRole": 965978050229571634,
              "NewMemberRole": 1039194817231601695,
              "NewMemberRoleDecay": 120.0,
              "RolesToReapplyOnRejoin": [],
              "FilterEnabled": true,
              "FilterIgnoredChannels": [
                964283764240973844
              ],
              "FilterBypassRoles": [
                964283794083446804
              ],
              "FilterDevBypass": false,
              "FilterWords": [
                "magic",
                "wing",
                "feather",
                "mayonnaise"
              ],
              "SpamEnabled": true,
              "SpamBypassRoles": [
                964283794083446804
              ],
              "SpamIgnoredChannels": [
                964283764240973844
              ],
              "SpamDevBypass": true,
              "SpamBasePressure": 10.0,
              "SpamImagePressure": 8.3,
              "SpamLengthPressure": 0.00625,
              "SpamLinePressure": 2.8,
              "SpamPingPressure": 2.5,
              "SpamRepeatPressure": 20.0,
              "SpamUnusualCharacterPressure": 0.01,
              "SpamMaxPressure": 60.0,
              "SpamPressureDecay": 2.5,
              "SpamMessageDeleteLookback": 60.0,
              "RaidProtectionEnabled": false,
              "AutoSilenceNewJoins": false,
              "SmallRaidSize": 3,
              "SmallRaidTime": 180.0,
              "LargeRaidSize": 10,
              "LargeRaidTime": 120.0,
              "RecentJoinDecay": 300.0,
              "SmallRaidDecay": 5.0,
              "LargeRaidDecay": 30.0
            }
            """;

        var config = JsonConvert.DeserializeObject<Config>(testConfig);
        var serialized = JsonConvert.SerializeObject(config, Formatting.Indented).Replace("\r\n", "\n");

        // testConfig with Small/LargeRaidTime removed
        // and bored config items added
        Assert.AreEqual("""
            {
              "Prefix": ".",
              "UnicycleInterval": 100,
              "MentionResponseEnabled": true,
              "MentionResponses": [
                "ohayo sekai good morning world",
                "???"
              ],
              "MentionResponseCooldown": 10.0,
              "DiscordActivityName": "you all soon",
              "DiscordActivityWatching": true,
              "Aliases": {
                "testalias": "echo Test successful!",
                "aliasfail": "",
                "member": "assignrole <@&973220758736216084>",
                "a": "ass"
              },
              "FirstRuleMessageId": 994643448948850788,
              "HiddenRules": {
                "-1": "<a:twiactually:613109204118667293>",
                "test": "hello"
              },
              "BestPonyChannel": 0,
              "BannerMode": 0,
              "BannerInterval": 1.0,
              "BannerImages": [
                "test1",
                "test2",
                "test3",
                "test4",
                "test5",
                "test6"
              ],
              "ModRole": 964283794083446804,
              "ModChannel": 973218854237007963,
              "LogChannel": 964283764240973844,
              "ManageNewUserRoles": true,
              "MemberRole": 965978050229571634,
              "NewMemberRole": 1039194817231601695,
              "NewMemberRoleDecay": 120.0,
              "RolesToReapplyOnRejoin": [],
              "FilterEnabled": true,
              "FilterIgnoredChannels": [
                964283764240973844
              ],
              "FilterBypassRoles": [
                964283794083446804
              ],
              "FilterDevBypass": false,
              "FilterWords": [
                "magic",
                "wing",
                "feather",
                "mayonnaise"
              ],
              "SpamEnabled": true,
              "SpamBypassRoles": [
                964283794083446804
              ],
              "SpamIgnoredChannels": [
                964283764240973844
              ],
              "SpamDevBypass": true,
              "SpamBasePressure": 10.0,
              "SpamImagePressure": 8.3,
              "SpamLengthPressure": 0.00625,
              "SpamLinePressure": 2.8,
              "SpamPingPressure": 2.5,
              "SpamRepeatPressure": 20.0,
              "SpamUnusualCharacterPressure": 0.01,
              "SpamMaxPressure": 60.0,
              "SpamPressureDecay": 2.5,
              "SpamMessageDeleteLookback": 60.0,
              "RaidProtectionEnabled": false,
              "AutoSilenceNewJoins": false,
              "SmallRaidSize": 3,
              "LargeRaidSize": 10,
              "RecentJoinDecay": 300.0,
              "SmallRaidDecay": 5.0,
              "LargeRaidDecay": 30.0,
              "BoredChannel": 0,
              "BoredCooldown": 300.0,
              "BoredCommands": []
            }
            """, serialized);
    }
}
