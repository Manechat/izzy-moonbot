namespace Izzy_Moonbot.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Izzy_Moonbot.Helpers;
    using Izzy_Moonbot.Service;
    using Izzy_Moonbot.Settings;
    using Discord;
    using Discord.Commands;

    [Summary("Module for interfacing with Manebooru")]
    public class BooruModule : ModuleBase<SocketCommandContext>
    {
        private readonly BooruService _booru;
        private readonly LoggingService _logger;
        private readonly MixinsService _mixins;

        public BooruModule(BooruService booru, LoggingService logger, MixinsService mixins)
        {
            _booru = booru;
            _logger = logger;
            _mixins = mixins;
        }

        [Command("pick")]
        [Summary("Selects an image at random")]
        public async Task PickCommandAsync([Remainder] [Summary("Query string")] string query = "*")
        {
            var checkLists = true;
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            query = _mixins.Transpile(query);

            var filterId = settings.defaultFilterId;
            foreach (var (filteredChannel, filteredId) in settings.filteredChannels)
            {
                if (filteredChannel != Context.Channel.Id)
                {
                    continue;
                }

                filterId = filteredId;
                checkLists = false;
            }

            if (checkLists)
            {
                if (!await CheckBadlistsAsync(query, settings))
                {
                    return;
                }
            }

            var (code, imageId, total, spoilered, spoilerList) = await _booru.GetRandomImageByQueryAsync(query, settings, filterId);
            if (code >= 300 && code < 400)
            {
                await ReplyAsync($"Something is giving me the runaround (HTTP {code})");
                await _logger.Log($"pick: {query}, HTTP ERROR {code}", Context);
            }
            else if (code >= 400 && code < 500)
            {
                await ReplyAsync($"I think you may have entered in something incorrectly (HTTP {code})");
                await _logger.Log($"pick: {query}, HTTP ERROR {code}", Context);
            }
            else if (code >= 500)
            {
                await ReplyAsync($"I'm having trouble accessing the site, please try again later (HTTP {code})");
                await _logger.Log($"pick: {query}, HTTP ERROR {code}", Context);
            }
            else if (total == 0)
            {
                await _logger.Log($"pick: {query}, total: {total}", Context);
                await ReplyAsync("I could not find any images with that query.");
            }
            else
            {
                var totalString = $"[{total} result";
                if (total == 1)
                {
                    totalString += "] ";
                }
                else
                {
                    totalString += "s] ";
                }

                totalString += $"[Id# {imageId}] ";

                if (spoilered)
                {
                    var spoilerStrings = SetupTagListOutput(spoilerList);
                    var output = totalString + $"Spoiler for {spoilerStrings}:{Environment.NewLine}|| https://manebooru.art/images/{imageId} ||";
                    await _logger.Log($"pick: {query}, total: {total} result: {imageId} SPOILERED {spoilerStrings}", Context);
                    await ReplyAsync(output);
                }
                else
                {
                    var output = totalString + $"https://manebooru.art/images/{imageId}";
                    await _logger.Log($"pick: {query}, total: {total} result: {imageId}", Context);
                    await ReplyAsync(output);
                }
            }
        }

        [Command("pickrecent")]
        [Summary("Selects first image in a search")]
        public async Task PickRecentCommandAsync([Remainder] [Summary("Query string")] string query = "*")
        {
            var checkLists = true;
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            query = _mixins.Transpile(query);

            var filterId = settings.defaultFilterId;
            foreach (var (filteredChannel, filteredId) in settings.filteredChannels)
            {
                if (filteredChannel != Context.Channel.Id)
                {
                    continue;
                }

                filterId = filteredId;
                checkLists = false;
            }

            if (checkLists)
            {
                if (!await CheckBadlistsAsync(query, settings))
                {
                    return;
                }
            }

            var (code, imageId, total, spoilered, spoilerList) = await _booru.GetFirstRecentImageByQueryAsync(query, settings, filterId);
            if (code >= 300 && code < 400)
            {
                await ReplyAsync($"Something is giving me the runaround (HTTP {code})");
                await _logger.Log($"pick: {query}, HTTP ERROR {code}", Context);
            }
            else if (code >= 400 && code < 500)
            {
                await ReplyAsync($"I think you may have entered in something incorrectly (HTTP {code})");
                await _logger.Log($"pick: {query}, HTTP ERROR {code}", Context);
            }
            else if (code >= 500)
            {
                await ReplyAsync($"I'm having trouble accessing the site, please try again later (HTTP {code})");
                await _logger.Log($"pick: {query}, HTTP ERROR {code}", Context);
            }
            else if (total == 0)
            {
                await _logger.Log($"pickrecent: {query}, total: {total}", Context);
                await ReplyAsync("I could not find any images with that query.");
            }
            else
            {
                var totalString = $"[{total} result";
                if (total == 1)
                {
                    totalString += "] ";
                }
                else
                {
                    totalString += "s] ";
                }

                totalString += $"[Id# {imageId}] ";

                if (spoilered)
                {
                    var spoilerStrings = SetupTagListOutput(spoilerList);
                    var output = totalString + $"Spoiler for {spoilerStrings}:{Environment.NewLine}|| https://manebooru.art/images/{imageId} ||";
                    await _logger.Log($"pickrecent: {query}, total: {total} result: {imageId} SPOILERED {spoilerStrings}", Context);
                    await ReplyAsync(output);
                }
                else
                {
                    var output = totalString + $"https://manebooru.art/images/{imageId}";
                    await _logger.Log($"pickrecent: {query}, total: {total} result: {imageId}", Context);
                    await ReplyAsync(output);
                }
            }
        }

        [Command("id")]
        [Summary("Selects an image by image id")]
        public async Task IdCommandAsync([Summary("The image Id")] long id = 4010266)
        {
            var checkLists = true;
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            var filterId = settings.defaultFilterId;
            foreach (var (filteredChannel, filteredId) in settings.filteredChannels)
            {
                if (filteredChannel != Context.Channel.Id)
                {
                    continue;
                }

                filterId = filteredId;
                checkLists = false;
            }

            if (checkLists)
            {
                if (!await CheckBadlistsAsync(id.ToString(), settings))
                {
                    return;
                }
            }

            var (code, imageId, spoilered, spoilerList) = await _booru.GetImageByIdAsync(id, settings, filterId);
            if (code >= 300 && code < 400)
            {
                await ReplyAsync($"Something is giving me the runaround (HTTP {code})");
                await _logger.Log($"pick: {id}, HTTP ERROR {code}", Context);
            }
            else if (code >= 400 && code < 500)
            {
                await ReplyAsync($"I think you may have entered in something incorrectly (HTTP {code})");
                await _logger.Log($"pick: {id}, HTTP ERROR {code}", Context);
            }
            else if (code >= 500)
            {
                await ReplyAsync($"I'm having trouble accessing the site, please try again later (HTTP {code})");
                await _logger.Log($"pick: {id}, HTTP ERROR {code}", Context);
            }
            else if (imageId == -1)
            {
                await ReplyAsync("I could not find that image.");
                await _logger.Log($"id: requested {id}, NOT FOUND", Context);
            }
            else
            {
                if (spoilered)
                {
                    var spoilerStrings = SetupTagListOutput(spoilerList);
                    var output = $"[Id# {imageId}] Result is a spoiler for {spoilerStrings}:{Environment.NewLine}|| https://manebooru.art/images/{imageId} ||";
                    await _logger.Log($"id: requested {id}, found {imageId} SPOILERED {spoilerStrings}", Context);
                    await ReplyAsync(output);
                }
                else
                {
                    await _logger.Log($"id: requested {id}, found {imageId}", Context);
                    await ReplyAsync($"[Id# {imageId}] https://manebooru.art/images/{imageId}");
                }
            }
        }

        [Command("tags")]
        [Summary("Selects a tag list by image Id")]
        public async Task TagsCommandAsync([Summary("The image Id")] long id = 4010266)
        {
            var checkLists = true;
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            var filterId = settings.defaultFilterId;
            foreach (var (filteredChannel, filteredId) in settings.filteredChannels)
            {
                if (filteredChannel != Context.Channel.Id)
                {
                    continue;
                }

                filterId = filteredId;
                checkLists = false;
            }

            if (checkLists)
            {
                if (!await CheckBadlistsAsync(id.ToString(), settings))
                {
                    return;
                }
            }

            var (code, tagList, spoilered, spoilerList) = await _booru.GetImageTagsIdAsync(id, settings, filterId);
            if (code >= 300 && code < 400)
            {
                await ReplyAsync($"Something is giving me the runaround (HTTP {code})");
                await _logger.Log($"pick: {id}, HTTP ERROR {code}", Context);
            }
            else if (code >= 400 && code < 500)
            {
                await ReplyAsync($"I think you may have entered in something incorrectly (HTTP {code})");
                await _logger.Log($"pick: {id}, HTTP ERROR {code}", Context);
            }
            else if (code >= 500)
            {
                await ReplyAsync($"I'm having trouble accessing the site, please try again later (HTTP {code})");
                await _logger.Log($"pick: {id}, HTTP ERROR {code}", Context);
            }
            else if (tagList.Count == 0)
            {
                await ReplyAsync("I could not find that image.");
                await _logger.Log($"tags: requested {id}, NOT FOUND", Context);
            }
            else
            {
                var tagStrings = SetupTagListOutput(tagList);
                var output = $"Image #{id} has the tags {tagStrings}";
                if (spoilered)
                {
                    var spoilerStrings = SetupTagListOutput(spoilerList);
                    output += $" including the spoiler tags {spoilerStrings}";
                    await _logger.Log($"tags: requested {id}, found {tagStrings} SPOILERED {spoilerStrings}", Context);
                }
                else
                {
                    await _logger.Log($"tags: requested {id}, found {tagStrings}", Context);
                }

                await ReplyAsync(output);
            }
        }

        [Command("getspoilers")]
        [Summary("Gets the list of spoiler tags")]
        public async Task GetSpoilersCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            var output = $"__Spoilered tags:__{Environment.NewLine}";
            for (var x = 0; x < settings.spoilerList.Count; x++)
            {
                output += $"`{settings.spoilerList[x].Item2}`";
                if (x < settings.spoilerList.Count - 1)
                {
                    output += ", ";
                }
            }

            await _logger.Log("getspoilers", Context);
            await ReplyAsync(output);
        }

        [Command("featured")]
        [Summary("Selects the current Featured Image on Manebooru")]
        public async Task FeaturedCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            var (code, featured) = await _booru.GetFeaturedImageIdAsync();
            if (code >= 300 && code < 400)
            {
                await ReplyAsync($"Something is giving me the runaround (HTTP {code})");
                await _logger.Log($"pick: {featured}, HTTP ERROR {code}", Context);
            }
            else if (code >= 400 && code < 500)
            {
                await ReplyAsync($"I think you may have entered in something incorrectly (HTTP {code})");
                await _logger.Log($"pick: {featured}, HTTP ERROR {code}", Context);
            }
            else if (code >= 500)
            {
                await ReplyAsync($"I'm having trouble accessing the site, please try again later (HTTP {code})");
                await _logger.Log($"pick: {featured}, HTTP ERROR {code}", Context);
            }
            else
            {
                await _logger.Log("featured", Context);
                await ReplyAsync($"[Id# {featured}] https://manebooru.art/images/{featured}");
            }
        }

        [Command("report")]
        [Summary("Reports an image id to the admin channel")]
        public async Task ReportAsync(long reportedImageId, [Remainder] string reason = "")
        {
            var checkLists = true;
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            var filterId = settings.defaultFilterId;
            foreach (var (filteredChannel, filteredId) in settings.filteredChannels)
            {
                if (filteredChannel != Context.Channel.Id)
                {
                    continue;
                }

                filterId = filteredId;
                checkLists = false;
            }

            var badTerms = "";
            if (checkLists)
            {
                badTerms = BadlistHelper.CheckYellowList(reportedImageId.ToString(), settings);
            }

            if (badTerms != "")
            {
                await ReplyAsync("That image is already blocked.");
            }
            else
            {
                var (_, imageId, _, _) = await _booru.GetImageByIdAsync(reportedImageId, settings, filterId);
                if (imageId == -1)
                {
                    await ReplyAsync("I could not find that image.");
                }
                else
                {
                    var output = $"<@{Context.User.Id}> has reported Image #{reportedImageId}";
                    if (reason != "")
                    {
                        output += $" with reason `{reason}`";
                    }

                    output += $" || <https://manebooru.art/images/{imageId}> ||";
                    await _logger.Log($"report: {reportedImageId} <SUCCESS>", Context, true);
                    var reportChannel = Context.Guild.GetTextChannel(settings.reportChannel);
                    if (settings.reportPing)
                    {
                        output = $"<@&{settings.reportRole}> " + output;
                        await reportChannel.SendMessageAsync(output);
                    }
                    else
                    {
                        await reportChannel.SendMessageAsync(output, allowedMentions: AllowedMentions.None);
                    }

                    await ReplyAsync("Admins have been notified. Thank you for your report.");
                }
            }
        }

        [Command("refreshlists")]
        [Summary("Refreshes the spoiler list and redlist")]
        public async Task RefreshListsCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
            {
                return;
            }

            await ReplyAsync("Refreshing spoiler list and redlist. This may take a few minutes.");
            await _booru.RefreshListsAsync(Context, settings);
            await ReplyAsync("Spoiler list and redlist refreshed!");
        }

        private static string SetupTagListOutput(List<string> tagList)
        {
            var sortedList = tagList;
            sortedList.Sort();
            var newList = new List<string>();
            foreach (var tag in sortedList)
            {
                if (tag.StartsWith("artist:"))
                {
                    newList.Add(tag);
                }
            }

            foreach (var tag in sortedList)
            {
                if (tag.StartsWith("editor:"))
                {
                    newList.Add(tag);
                }
            }

            foreach (var tag in sortedList)
            {
                if (tag.StartsWith("character:"))
                {
                    newList.Add(tag);
                }
            }

            foreach (var tag in sortedList)
            {
                if (tag.StartsWith("species:"))
                {
                    newList.Add(tag);
                }
            }

            foreach (var tag in sortedList)
            {
                if (tag.StartsWith("episode:"))
                {
                    newList.Add(tag);
                }
            }

            foreach (var tag in sortedList)
            {
                if (tag.StartsWith("artist:") || tag.StartsWith("editor:") || tag.StartsWith("character:") || tag.StartsWith("species:") || tag.StartsWith("episode:"))
                {
                    continue;
                }

                newList.Add(tag);
            }

            var output = "";
            for (var x = 0; x < newList.Count; x++)
            {
                var spoilerTerm = newList[x];
                output += $"`{spoilerTerm}`";
                if (x < newList.Count - 1)
                {
                    output += ", ";
                }
            }

            return output;
        }

        private async Task<bool> CheckBadlistsAsync(string query, ServerSettings settings)
        {
            var yellowTerms = BadlistHelper.CheckYellowList(query, settings);
            var redTerms = BadlistHelper.CheckRedList(query, settings);
            if (redTerms != "")
            {
                await _logger.Log($"pick: {query}, REDLISTED {redTerms}", Context, true);
                await ReplyAsync("You're kidding me, right?");
                var redChannel = Context.Guild.GetTextChannel(settings.redAlertChannel);
                if (settings.redPing)
                {
                    await redChannel.SendMessageAsync(
                        $"<@&{settings.redAlertRole}> <@{Context.User.Id}> searched for a banned term in <#{Context.Channel.Id}> RED TERMS: {redTerms}");
                }
                else
                {
                    await redChannel.SendMessageAsync($"<@{Context.User.Id}> searched for a banned term in <#{Context.Channel.Id}> RED TERMS: {redTerms}",
                        allowedMentions: AllowedMentions.None);
                }

                await Context.Message.DeleteAsync();
                return false;
            }

            if (yellowTerms != "")
            {
                await _logger.Log($"pick: {query}, YELLOWLISTED {yellowTerms}", Context, true);
                await ReplyAsync("I'm not gonna go look for that.");
                var yellowChannel = Context.Guild.GetTextChannel(settings.yellowAlertChannel);
                if (settings.yellowPing)
                {
                    await yellowChannel.SendMessageAsync(
                        $"<@&{settings.yellowAlertRole}> <@{Context.User.Id}> searched for a naughty term in <#{Context.Channel.Id}> YELLOW TERMS: {yellowTerms}");
                }
                else
                {
                    await yellowChannel.SendMessageAsync($"<@{Context.User.Id}> searched for a naughty term in <#{Context.Channel.Id}> YELLOW TERMS: {yellowTerms}",
                        allowedMentions: AllowedMentions.None);
                }

                return false;
            }

            return true;
        }
    }
}
