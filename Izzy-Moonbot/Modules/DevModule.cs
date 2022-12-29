using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Flurl;
using Flurl.Http;
using Izzy_Moonbot.Adapters;
using Izzy_Moonbot.Attributes;
using Izzy_Moonbot.Helpers;
using Izzy_Moonbot.Service;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Izzy_Moonbot.Modules;

[Summary("Development commands.")]
public class DevModule : ModuleBase<SocketCommandContext>
{
    private readonly FilterService _filterService;
    private readonly LoggingService _loggingService;
    private readonly ModLoggingService _modLoggingService;
    private readonly ModService _modService;
    private readonly SpamService _pressureService;
    private readonly RaidService _raidService;
    private readonly ScheduleService _scheduleService;
    private readonly Config _config;
    private readonly State _state;
    private readonly Dictionary<ulong, User> _users;
    private readonly DatabaseHelper _database;
    private readonly UserService _userService;

    public DevModule(Config config, Dictionary<ulong, User> users, FilterService filterService,
        LoggingService loggingService, ModLoggingService modLoggingService, ModService modService,
        SpamService pressureService, RaidService raidService, ScheduleService scheduleService, State state, DatabaseHelper database, UserService userService)
    {
        _config = config;
        _users = users;
        _filterService = filterService;
        _loggingService = loggingService;
        _modLoggingService = modLoggingService;
        _modService = modService;
        _pressureService = pressureService;
        _raidService = raidService;
        _scheduleService = scheduleService;
        _state = state;
        _database = database;
        _userService = userService;
    }

    [NamedArgumentType]
    public class TypeTestArguments
    {
        public bool? boolean { get; set; }
        public char? character { get; set; }
        public byte? nom { get; set; }
        public short? pipp { get; set; }
        public int? integer { get; set; }
        public long? starlight { get; set; }
        public double? rainboom { get; set; }
        public string? single { get; set; }
        public string? multiword { get; set; }
        public TestEnum? how { get; set; }
        public DateTimeOffset? time { get; set; }
        public SocketTextChannel? channel { get; set; }
        public SocketGuildUser? user { get; set; }
        public SocketUserMessage? message { get; set; }
        public SocketRole? role { get; set; }
    }

    [Command("typetest")]
    [Summary("Type testing")]
    [DevCommand]
    public async Task TypeTestCommandAsync(TypeTestArguments tests)
    {
        var testsCompleted = new Dictionary<string, string?>();

        if (tests.boolean.HasValue) testsCompleted.Add("bool", tests.boolean.Value.ToString());
        if (tests.character.HasValue) testsCompleted.Add("char", tests.character.Value.ToString());
        if (tests.nom.HasValue) testsCompleted.Add("byte", tests.nom.Value.ToString());
        if (tests.pipp.HasValue) testsCompleted.Add("short", tests.pipp.Value.ToString());
        if (tests.integer.HasValue) testsCompleted.Add("int", tests.integer.Value.ToString());
        if (tests.starlight.HasValue) testsCompleted.Add("long", tests.starlight.Value.ToString());
        if (tests.rainboom.HasValue) testsCompleted.Add("double", tests.rainboom.Value.ToString());
        if (tests.single != null) testsCompleted.Add("string (single)", tests.single);
        if (tests.multiword != null) testsCompleted.Add("string (multiple)", tests.multiword);
        if (tests.how.HasValue) testsCompleted.Add("enum", tests.how.Value.ToString());
        if (tests.time.HasValue) testsCompleted.Add("datetimeoffset", tests.time.Value.ToString());
        if (tests.channel != null) testsCompleted.Add("channel", tests.channel.Name);
        if (tests.user != null) testsCompleted.Add("user", tests.user.DisplayName);
        if (tests.message != null) testsCompleted.Add("message", tests.message.GetJumpUrl());
        if (tests.role != null) testsCompleted.Add("role", tests.role.Name);

        var resultToPrint = testsCompleted.Select(pair => $"{pair.Key}: {pair.Value}");
        
        await ReplyAsync($"Type testing results: {Environment.NewLine}" +
                         $"{String.Join(Environment.NewLine, resultToPrint)}");
    }

    public enum TestEnum
    {
        Hello,
        Goodbye,
        None
    }

    [Command("test")]
    [Summary("Unit tests for Izzy Moonbow")]
    [DevCommand]
    public async Task TestCommandAsync([Summary("Test Identifier")] string testId = "",
        [Remainder] [Summary("Test arguments")]
        string argString = "")
    {
        var args = argString.Split(" ");
        switch (testId)
        {
            case "pagination":
                var pages =
                    "Hello!||This is a test of pagination!||If this works, you're able to see this.||The paginated message will expire in 5 minutes.||Hopefully my code isn't broken..."
                        .Split("||");
                var staticParts =
                    $"**Test utility** - Pagination test{Environment.NewLine}*This is a simple test for the pagination utility!*{Environment.NewLine}*This is a header which will remain regardless of the current page.*{Environment.NewLine}Below is the paginated content.||This is the footer of the pagination message which will remain regardless of the current page{Environment.NewLine}There is a countdown below as well as buttons to change the page."
                        .Split("||");

                var paginationHelper = new PaginationHelper(Context, pages, staticParts);
                break;
            case "pressure-hook":
                await Context.Message.ReplyAsync(
                    $"**Test utility** - Pressure hookin test.{Environment.NewLine}*Other services or modules can hook into the pressure service to do specific things.*{Environment.NewLine}*An example of this is getting pressure for a user.*{Environment.NewLine}*Like, your current pressure is `{_pressureService.GetPressure(Context.User.Id)}`*");
                break;
            case "dump-users-size":
                await Context.Message.ReplyAsync($"UserStore size: {_users.Count}");
                break;
            case "create-echo-task":
                var action = new ScheduledEchoJob(Context.Channel.Id,
                    "Hello! Exactly 1 minute should have passed between the test command and this message!");
                var task = new ScheduledJob(DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1), action);
                await _scheduleService.CreateScheduledJob(task);
                await Context.Message.ReplyAsync("Created scheduled task.");
                break;
            case "test-twilight":
                await Context.Channel.SendMessageAsync(
                    $"Dear Princess Twilight,{Environment.NewLine}```{Environment.NewLine}" +
                    $"[2022-07-30 00:19:07 ERR] Izzy Moonbot has encountered an error. Logging information...{Environment.NewLine}" +
                    $"[2022-07-30 00:19:07 ERR] Message: Server requested a reconnect{Environment.NewLine}" +
                    $"[2022-07-30 00:19:07 ERR] Source: System.Private.CoreLib{Environment.NewLine}" +
                    $"[2022-07-30 00:19:07 ERR] HResult: -2146233088{Environment.NewLine}" +
                    "[2022-07-30 00:19:07 ERR] Stack trace:    at Discord.ConnectionManager.<>c__DisplayClass29_0.<<StartAsync>b__0>d.MoveNext()" +
                    $"{Environment.NewLine}```Your faithful Bot,{Environment.NewLine}Izzy Moonbot");
                break;
            case "twilight":
                await Context.Guild.GetTextChannel(1002687344199094292).SendMessageAsync(
                    $"Dear Princess Twilight,{Environment.NewLine}```{Environment.NewLine}" +
                    $"[2022-07-30 00:19:07 ERR] Izzy Moonbot has encountered an error. Logging information...{Environment.NewLine}" +
                    $"[2022-07-30 00:19:07 ERR] Message: Server requested a reconnect{Environment.NewLine}" +
                    $"[2022-07-30 00:19:07 ERR] Source: System.Private.CoreLib{Environment.NewLine}" +
                    $"[2022-07-30 00:19:07 ERR] HResult: -2146233088{Environment.NewLine}" +
                    "[2022-07-30 00:19:07 ERR] Stack trace:    at Discord.ConnectionManager.<>c__DisplayClass29_0.<<StartAsync>b__0>d.MoveNext()" +
                    $"{Environment.NewLine}```Your faithful Bot,{Environment.NewLine}Izzy Moonbot");

                break;
            case "import-filter":
                {
                    var toFilter = Context.Message.ReferencedMessage.CleanContent.Split(Environment.NewLine).AsEnumerable();
                    if (args[1] == "no") toFilter = toFilter.Skip(1);
                    else toFilter = toFilter.Skip(2);

                    var msg = await ReplyAsync(
                        $"Confirm: Import the list of words you replied to into the `{args[0]}` list? Checking reactions in 10 seconds.");
                    await msg.AddReactionAsync(Emoji.Parse("✅"));
                    var _ = Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(Convert.ToInt32(10000));
                        var users = msg.GetReactionUsersAsync(Emoji.Parse("✅"), 2);
                        if (users.AnyAsync(users =>
                        {
                            return users.Any(user => user.Id == Context.User.Id) ? true : false;
                        }).Result)
                        {
                            await msg.RemoveAllReactionsAsync();
                            await msg.ModifyAsync(message => message.Content = "⚠  **Importing. Please wait...**");

                            _config.FilteredWords[args[1]].UnionWith(toFilter);

                            await FileHelper.SaveConfigAsync(_config);
                            await msg.ModifyAsync(message => message.Content = "⚠  **Done!**");
                        }
                    });

                    break;
                }
            case "raid":
                {
                    // Simulates a raid.
                    // args[0] is time in seconds between joins
                    // rest is user ids.
                    var timePeriod = Convert.ToInt32(args[0]) * 1000;
                    var users = args.Skip(1).Select(user =>
                    {
                        if (ulong.TryParse(user, out var id)) return Context.Guild.GetUser(id);
                        return null;
                    }).Where(user => user != null) as IEnumerable<SocketGuildUser>; // cast away nullability

                    var raidMsg = await ReplyAsync(
                        $"Confirm: Simulate {users.Count()} users joining {timePeriod} milliseconds apart? Checking reactions in 10 seconds.");
                    await raidMsg.AddReactionAsync(Emoji.Parse("✅"));
                    var _ = Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(Convert.ToInt32(10000));
                        var raidMsgUsers = raidMsg.GetReactionUsersAsync(Emoji.Parse("✅"), 2);
                        if (raidMsgUsers.AnyAsync(raidMsgUsersActual =>
                            {
                                return raidMsgUsersActual.Any(user => user.Id == Context.User.Id) ? true : false;
                            }).Result)
                        {
                            await raidMsg.RemoveAllReactionsAsync();
                            await raidMsg.ModifyAsync(message => message.Content = "⚠  **Executing...**");

                            var _ = Task.Factory.StartNew(async () =>
                            {
                                foreach (var user in users)
                                {
                                    await Task.Delay(timePeriod);
                                    await _raidService.ProcessMemberJoin(user);
                                }
                            });

                            await raidMsg.ModifyAsync(message =>
                                message.Content = "⚠  **Executed. Expect raid alarms if hit.**");
                        }
                        else
                        {
                            await raidMsg.RemoveAllReactionsAsync();
                            await raidMsg.ModifyAsync(message => message.Content = "⚠  **Cancelled.**");
                        }
                    });
                    break;
                }
            case "state":
                _state.CurrentSmallJoinCount++;
                await ReplyAsync($"At {_state.CurrentSmallJoinCount}.");
                break;
            case "asyncSyncTesk":
                Console.WriteLine("Application executing on thread {0}",
                    Thread.CurrentThread.ManagedThreadId);
                var asyncTask = Task.Run( () => {  Console.WriteLine("Task {0} (asyncTask) executing on Thread {1}",
                        Task.CurrentId,
                        Thread.CurrentThread.ManagedThreadId);
                    long sum = 0;
                    for (int ctr = 1; ctr <= 1000000; ctr++ )
                        sum += ctr;
                    return sum;
                });
                var syncTask = new Task<long>( () =>  { Console.WriteLine("Task {0} (syncTask) executing on Thread {1}",
                        Task.CurrentId,
                        Thread.CurrentThread.ManagedThreadId);
                    long sum = 0;
                    for (int ctr = 1; ctr <= 1000000; ctr++ )
                        sum += ctr;
                    return sum;
                });
                syncTask.RunSynchronously();
                Console.WriteLine();
                Console.WriteLine("Task {0} returned {1:N0}", syncTask.Id, syncTask.Result);
                Console.WriteLine("Task {0} returned {1:N0}", asyncTask.Id, asyncTask.Result);
                break;
            case "overloadFilter":
                {
                    var izzyContext = new SocketCommandContextAdapter(Context);
                    for (var i = 0; i < 10; i++)
                    {
                        var _ = Task.Run(async () => await _filterService.ProcessMessage(izzyContext.Message, izzyContext.Client));
                    }
                    break;
                }
            case "logTest":
                var pressureTracer = new Dictionary<string, double>{ {"Base", _config.SpamBasePressure} };
                _loggingService.Log($"Pressure increase by 0 to 0/{_config.SpamMaxPressure}.{Environment.NewLine}                          Pressure trace: {string.Join(", ", pressureTracer)}", Context, level: LogLevel.Debug);
                break;
            case "invitesDisabled":
                await ReplyAsync("Invites disabled: " + Context.Guild.Features.HasFeature("INVITES_DISABLED"));
                break;
            case "timehelper":
                var timestring = string.Join(" ", args);
                try
                {
                    var time = TimeHelper.Convert(timestring);

                    await ReplyAsync($"Successfully converted {timestring} to a DateTimeOffset.{Environment.NewLine}" +
                                     $"DateTime: <t:{time.Time.ToUnixTimeSeconds()}:F> (<t:{time.Time.ToUnixTimeSeconds()}:R>){Environment.NewLine}" +
                                     $"Repeats: {(time.Repeats ? "yes" : "no")}{Environment.NewLine}" +
                                     $"Repeats every: {time.RepeatType ?? "Doesn't repeat"}");
                }
                catch (FormatException exception)
                {
                    await ReplyAsync($"Exception: {exception.Message}");
                }
                break;
            case "repeat-scheduled":
                var timeinput = string.Join(" ", args);
                try
                {
                    var time = TimeHelper.Convert(timeinput);

                    var repeatType = time.RepeatType switch
                    {
                        "relative" => ScheduledJobRepeatType.Relative,
                        "daily" => ScheduledJobRepeatType.Daily,
                        "weekly" => ScheduledJobRepeatType.Weekly,
                        "yearly" => ScheduledJobRepeatType.Yearly,
                        _ => ScheduledJobRepeatType.None
                    };
                    var repeataction = new ScheduledEchoJob(Context.Channel.Id,
                        $"Hello! I'm a repeating job occuring `{timeinput}`!");
                    var repeattask = new ScheduledJob(DateTimeOffset.UtcNow,
                        time.Time, repeataction, repeatType);
                    await _scheduleService.CreateScheduledJob(repeattask);
                    await Context.Message.ReplyAsync("Created repeating scheduled task.");
                    break;
                }
                catch (FormatException exception)
                {
                    await ReplyAsync($"Exception: {exception.Message}");
                }
                break;
            case "repeat-scheduled-misty":
                var timein = string.Join(" ", args);
                try
                {
                    var time = TimeHelper.Convert(timein);

                    var repeatType = time.RepeatType switch
                    {
                        "relative" => ScheduledJobRepeatType.Relative,
                        "daily" => ScheduledJobRepeatType.Daily,
                        "weekly" => ScheduledJobRepeatType.Weekly,
                        "yearly" => ScheduledJobRepeatType.Yearly,
                        _ => ScheduledJobRepeatType.None
                    };

                    _loggingService.Log($"{time.Time:F} {time.RepeatType}");
                    var repeataction = new ScheduledEchoJob(Context.Channel.Id, "misty");
                    var repeattask = new ScheduledJob(DateTimeOffset.UtcNow,
                        time.Time, repeataction, repeatType);
                    await _scheduleService.CreateScheduledJob(repeattask);
                    await Context.Message.ReplyAsync("Created repeating scheduled task.");
                    break;
                }
                catch (FormatException exception)
                {
                    await ReplyAsync($"Exception: {exception.Message}");
                }
                break;
            case "getuser":
                if (ulong.TryParse(args[0], out var id))
                {
                    var user = await Context.Client.GetUserAsync(id);

                    if (user == null)
                    {
                        await ReplyAsync("Couldn't find user");
                    }
                    else
                    {
                        await ReplyAsync($"User info:{Environment.NewLine}" +
                                         $"Id: {id}{Environment.NewLine}" +
                                         $"Name: {user.Username}#{user.Discriminator}");
                    }
                }
                else
                {
                    await ReplyAsync($"not valid user");
                }
                break;
            case "customArgument":
                var customArgument_Result = DiscordHelper.GetArguments(argString);

                await ReplyAsync($"Processed. Here's what I got:{Environment.NewLine}```{Environment.NewLine}" +
                                 $"{string.Join(", ", customArgument_Result)}" +
                                 $"{Environment.NewLine}```");
                break;
            case "listEnum":
                var enumNames = Enum.GetNames<TestEnum>();

                await ReplyAsync($"```{Environment.NewLine}{string.Join(", ", enumNames)}{Environment.NewLine}```");
                break;
            case "parseEnum":
                if (!Enum.TryParse<TestEnum>(args[0], out var testEnum))
                {
                    await ReplyAsync("Parse fail.");
                    return;
                }

                await ReplyAsync($"Parse success. `{testEnum}`");
                break;
            case "parseImage":
                var attachment = new FileAttachment(args[0].GetStreamAsync().Result, "test.png");

                await Context.Channel.SendFileAsync(attachment, "Test Success");
                break;
            case "db-collection":
                var collectionDbCollection = _database.GetCollection("test");
                await ReplyAsync(collectionDbCollection.CollectionNamespace.FullName);
                break;
            case "db-document":
                var collectionDbDocument = _database.GetCollection("test");
                var documentDbDocument = new BsonDocument
                {
                    { "name", "MongoDB" },
                    { "type", "Database" },
                    { "count", 1 },
                    { "info", new BsonDocument
                    {
                        { "x", 203 },
                        { "y", 102 }
                    }}
                };
                try
                {
                    await collectionDbDocument.InsertOneAsync(documentDbDocument);
                    var gottenDocumentDbDocument =
                        await collectionDbDocument.Find(new BsonDocument()).FirstOrDefaultAsync();
                    await ReplyAsync(
                        $"```json{Environment.NewLine}{gottenDocumentDbDocument.ToString()}{Environment.NewLine}```");
                }
                catch (Exception ex)
                {
                    _loggingService.Log(ex.Message, level: LogLevel.Critical);
                    _loggingService.Log(ex.Source ?? "None", level: LogLevel.Critical);
                    _loggingService.Log(ex.HelpLink ?? "None", level: LogLevel.Critical);
                    _loggingService.Log(ex.StackTrace ?? "None", level: LogLevel.Critical);
                    _loggingService.Log(ex.InnerException?.Message ?? "None", level: LogLevel.Critical);
                }

                break;
            case "try-migrate-users":

                var migratedUsers = _users.Select(pair =>
                {
                    var user = pair.Value;
                    user.Id = pair.Key;

                    return user;
                });
                
                try
                {
                    await _userService.CreateUsers(migratedUsers);
                    await ReplyAsync("Successfully migrated all users.");
                }
                catch (Exception ex)
                {
                    _loggingService.Log($"Exception on attempt to migrate:\n" +
                                        $"Message: {ex.Message}\n" +
                                        $"Stack: {ex.StackTrace}", level: LogLevel.Error);
                    await ReplyAsync($"Failed to migrate. See logs for details.");
                }
                break;
            case "try-get-user":
                if (!ulong.TryParse(args[0], out var userId))
                {
                    await ReplyAsync("not id");
                    return;
                }

                var userFromFile = _users[userId];
                var userFromDB = await _userService.GetUser(userId);

                await ReplyAsync($"Done. Here's what I found:\n" +
                                 $"File:\n```\n" +
                                 $"Username: {userFromFile.Username}\n" +
                                 $"Aliases: {string.Join(",", userFromFile.Aliases)}\n" +
                                 $"Last talk time: <t:{userFromFile.Timestamp.ToUnixTimeSeconds()}:F>\n" +
                                 $"Last pressure: {userFromFile.Pressure}\n```\n\n" +
                                 $"Database:\n```\n" +
                                 $"Username: {userFromDB.Username}\n" +
                                 $"Aliases: {string.Join(",", userFromDB.Aliases)}\n" +
                                 $"Last talk time: <t:{userFromDB.Timestamp.ToUnixTimeSeconds()}:F>\n" +
                                 $"Last pressure: {userFromDB.Pressure}\n```\n\n");
                
                break;
            default:
                await Context.Message.ReplyAsync("Unknown test.");
                break;
        }
    }
}