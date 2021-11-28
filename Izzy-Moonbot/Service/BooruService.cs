namespace Izzy_Moonbot.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Izzy_Moonbot.Helpers;
    using Izzy_Moonbot.Settings;
    using Discord.Commands;
    using Flurl;
    using Flurl.Http;
    using Microsoft.Extensions.Options;

    public class BooruService
    {
        private readonly ManebooruSettings _settings;

        public BooruService(IOptions<ManebooruSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task<Tuple<int?, long, bool, List<string>>> GetImageByIdAsync(long imageId, ServerSettings settings, int filterId)
        {
            //GET	/api/v1/json/images/:image_id
            const long searchResult = -1;
            var emptyList = new List<string>();
            var returnResult = new Tuple<int?, long, bool, List<string>>(null, searchResult, false, emptyList);
            var safeQuery = "id:" + imageId;
            if (settings.safeMode)
            {
                safeQuery += ", safe";
            }

            dynamic results;
            try
            {
                results = await _settings.url
                    .AppendPathSegments("/api/v1/json/search/images")
                    .SetQueryParams(new { key = _settings.token, q = safeQuery, filter_id = filterId })
                    .GetAsync()
                    .ReceiveJson();
            }
            catch (FlurlHttpException ex)
            {
                var code = ex.StatusCode;
                returnResult = new Tuple<int?, long, bool, List<string>>(code, returnResult.Item2, returnResult.Item3, returnResult.Item4);
                return returnResult;
            }

            if (results.total <= 0)
            {
                return returnResult;
            }

            bool spoiler = results.images[0].spoilered;
            var spoilerList = CheckSpoilerList(results.images[0].tag_ids, settings);
            returnResult = new Tuple<int?, long, bool, List<string>>(null, results.images[0].id, spoiler, spoilerList);
            return (returnResult);
        }

        public async Task<Tuple<int?, long, long, bool, List<string>>> GetRandomImageByQueryAsync(string query, ServerSettings settings, int filterId)
        {
            //GET	/api/v1/json/search/images?q=safe
            int? code;
            long searchResult = -1;
            long numberOfResults = 0;
            var emptyList = new List<string>();
            var returnResult = new Tuple<int?, long, long, bool, List<string>>(null, searchResult, numberOfResults, false, emptyList);
            var safeQuery = query;
            if (settings.safeMode)
            {
                safeQuery += ", safe";
            }

            dynamic results;
            try
            {
                results = await _settings.url
                    .AppendPathSegments("/api/v1/json/search/images")
                    .SetQueryParams(new { key = _settings.token, q = safeQuery, per_page = 1, filter_id = filterId })
                    .GetAsync()
                    .ReceiveJson();
            }
            catch (FlurlHttpException ex)
            {
                code = ex.StatusCode;
                returnResult = new Tuple<int?, long, long, bool, List<string>>(code, returnResult.Item2, returnResult.Item3, returnResult.Item4, returnResult.Item5);
                return returnResult;
            }

            numberOfResults = results.total;
            if (numberOfResults <= 0)
            {
                return returnResult;
            }

            var page = new Random().Next((int)numberOfResults) + 1;
            try
            {
                results = await _settings.url
                    .AppendPathSegments("/api/v1/json/search/images")
                    .SetQueryParams(new { key = _settings.token, q = query, per_page = 1, page, filter_id = filterId })
                    .GetAsync()
                    .ReceiveJson();
            }
            catch (FlurlHttpException ex)
            {
                code = ex.StatusCode;
                returnResult = new Tuple<int?, long, long, bool, List<string>>(code, returnResult.Item2, returnResult.Item3, returnResult.Item4, returnResult.Item5);
                return returnResult;
            }

            searchResult = results.images[0].id;
            bool spoiler = results.images[0].spoilered;
            var spoilerList = CheckSpoilerList(results.images[0].tag_ids, settings);
            returnResult = new Tuple<int?, long, long, bool, List<string>>(null, searchResult, numberOfResults, spoiler, spoilerList);
            return (returnResult);
        }

        public async Task<Tuple<int?, long, long, bool, List<string>>> GetFirstRecentImageByQueryAsync(string query, ServerSettings settings, int filterId)
        {
            //GET	/api/v1/json/search/images?q=safe
            long searchResult = -1;
            long numberOfResults = 0;
            var emptyList = new List<string>();
            var returnResult = new Tuple<int?, long, long, bool, List<string>>(null, searchResult, numberOfResults, false, emptyList);
            var safeQuery = query;
            if (settings.safeMode)
            {
                safeQuery += ", safe";
            }

            dynamic results;
            try
            {
                results = await _settings.url
                    .AppendPathSegments("/api/v1/json/search/images")
                    .SetQueryParams(new { key = _settings.token, q = safeQuery, per_page = 1, sf = "first_seen_at", sd = "desc", filter_id = filterId })
                    .GetAsync()
                    .ReceiveJson();
            }
            catch (FlurlHttpException ex)
            {
                var code = ex.StatusCode;
                returnResult = new Tuple<int?, long, long, bool, List<string>>(code, returnResult.Item2, returnResult.Item3, returnResult.Item4, returnResult.Item5);
                return returnResult;
            }

            numberOfResults = results.total;
            if (numberOfResults <= 0)
            {
                return returnResult;
            }

            searchResult = results.images[0].id;
            bool spoiler = results.images[0].spoilered;
            var spoilerList = CheckSpoilerList(results.images[0].tag_ids, settings);
            returnResult = new Tuple<int?, long, long, bool, List<string>>(null, searchResult, numberOfResults, spoiler, spoilerList);
            return (returnResult);
        }

        public async Task<Tuple<int?, long>> GetFeaturedImageIdAsync()
        {
            //GET	/api/v1/json/images/featured
            long featuredId = 0;
            var returnResult = new Tuple<int?, long>(null, featuredId);
            dynamic results;
            try
            {
                results = await _settings.url
                    .AppendPathSegments("/api/v1/json/images/featured")
                    .GetAsync()
                    .ReceiveJson();
            }
            catch (FlurlHttpException ex)
            {
                var code = ex.StatusCode;
                returnResult = new Tuple<int?, long>(code, returnResult.Item2);
                return returnResult;
            }

            featuredId = results.image.id;
            returnResult = new Tuple<int?, long>(null, featuredId);
            return returnResult;
        }

        public async Task<Tuple<int?, List<string>, bool, List<string>>> GetImageTagsIdAsync(long imageId, ServerSettings settings, int filterId)
        {
            //GET	/api/v1/json/images/:image_id
            var emptyTagList = new List<string>();
            var emptySpoilerList = new List<string>();
            var returnResult = new Tuple<int?, List<string>, bool, List<string>>(null, emptyTagList, false, emptySpoilerList);
            var safeQuery = "id:" + imageId;
            if (settings.safeMode)
            {
                safeQuery += ", safe";
            }

            dynamic results;
            try
            {
                results = await _settings.url
                    .AppendPathSegments("/api/v1/json/search/images")
                    .SetQueryParams(new { key = _settings.token, q = safeQuery, filter_id = filterId })
                    .GetAsync()
                    .ReceiveJson();
            }
            catch (FlurlHttpException ex)
            {
                var code = ex.StatusCode;
                returnResult = new Tuple<int?, List<string>, bool, List<string>>(code, returnResult.Item2, returnResult.Item3, returnResult.Item4);
                return returnResult;
            }

            if (results.total <= 0)
            {
                return returnResult;
            }

            bool spoiler = results.images[0].spoilered;
            List<object> tagIds = results.images[0].tag_ids;
            List<object> tagNames = results.images[0].tags;
            var spoilerList = CheckSpoilerList(tagIds, settings);
            var tagStrings = new List<string>();
            foreach (var tagName in tagNames)
            {
                if (!spoilerList.Contains(tagName.ToString()))
                {
                    tagStrings.Add(tagName.ToString());
                }
            }

            if (tagIds.Count != tagNames.Count)
            {
                return returnResult;
            }

            returnResult = new Tuple<int?, List<string>, bool, List<string>>(null, tagStrings, spoiler, spoilerList);
            return (returnResult);
        }

        public async Task RefreshListsAsync(SocketCommandContext context, ServerSettings settings)
        {
            await GetSpoilerTagsAsync(context, settings);
            await GetHiddenTagsAsync(context, settings);
        }

        public async Task<int> CheckFilterAsync(int filter)
        {
            //GET	/api/v1/json/filters/user
            const int defaultInt = 0;
            dynamic results = null;
            try
            {
                results = await _settings.url
                    .AppendPathSegments($"/api/v1/json/filters/{filter}")
                    .GetAsync()
                    .ReceiveJson();
            }
            catch (FlurlHttpException ex)
            {
                if (ex.StatusCode >= 300)
                {
                    return defaultInt;
                }
            }

            return results != null ? (int)results.filter.id : defaultInt;
        }

        private static List<string> CheckSpoilerList(List<object> tagIds, ServerSettings settings)
        {
            var tagList = new List<string>();
            foreach (long tagId in tagIds)
            {
                foreach (var (spoilerTagId, spoilerTagName) in settings.spoilerList)
                {
                    if (tagId == spoilerTagId)
                    {
                        tagList.Add(spoilerTagName);
                    }
                }
            }

            return tagList;
        }

        private async Task<string> GetTagNameByIdAsync(long id)
        {
            //GET	/api/v1/json/search/tags
            dynamic results = null;
            const string defaultString = "";
            try
            {
                results = await _settings.url
                    .AppendPathSegments("/api/v1/json/search/tags")
                    .SetQueryParams(new { q = $"id:{id}" })
                    .GetAsync()
                    .ReceiveJson();
            }
            catch (FlurlHttpException ex)
            {
                if (ex.StatusCode >= 300)
                {
                }
            }

            return results != null ? results.tags[0].name.ToString() : defaultString;
        }

        private async Task GetSpoilerTagsAsync(SocketCommandContext context, ServerSettings settings)
        {
            //GET	/api/v1/json/filters/user
            dynamic results = null;
            try
            {
                results = await _settings.url
                    .AppendPathSegments($"/api/v1/json/filters/{settings.defaultFilterId}")
                    .GetAsync()
                    .ReceiveJson();
            }
            catch (FlurlHttpException ex)
            {
                if (ex.StatusCode >= 300)
                {
                }
            }

            var tagIds = results.filter.spoilered_tag_ids;
            var tagNames = await GetTagNamesAsync(tagIds);
            var combinedTags = new List<Tuple<long, string>>();
            for (var x = 0; x < tagIds.Count; x++)
            {
                var newItem = new Tuple<long, string>((long)tagIds[x], tagNames[x].ToString());
                combinedTags.Add(newItem);
            }

            settings.spoilerList = combinedTags;
            await FileHelper.SaveServerSettingsAsync(settings, context);
        }

        private async Task GetHiddenTagsAsync(SocketCommandContext context, ServerSettings settings)
        {
            //GET	/api/v1/json/filters/user
            var hiddenTagResults = await _settings.url
                .AppendPathSegments($"/api/v1/json/filters/{settings.defaultFilterId}")
                .GetAsync()
                .ReceiveJson();
            var hiddenTagIds = hiddenTagResults.filter.hidden_tag_ids;
            var tagIds = new List<object>();
            foreach (var hiddenTagId in hiddenTagIds)
            {
                tagIds.Add(hiddenTagId);
                var furtherTagResults = await _settings.url
                    .AppendPathSegments("/api/v1/json/search/tags")
                    .SetQueryParams(new { q = $"id:{hiddenTagId}" })
                    .GetAsync()
                    .ReceiveJson();
                var aliasedTagSlugs = furtherTagResults.tags[0].aliases;
                var impliedTagSlugs = furtherTagResults.tags[0].implied_by_tags;

                foreach (var aliasedTagSlug in aliasedTagSlugs)
                {
                    string decode = aliasedTagSlug.ToString();
                    decode = decode.Replace("%", "%25");
                    var aliasedTagResults = await _settings.url
                        .AppendPathSegments($"/api/v1/json/tags/{decode}")
                        .GetAsync()
                        .ReceiveJson();
                    var aliasedTagId = aliasedTagResults.tag.id;
                    tagIds.Add(aliasedTagId);
                }

                foreach (var impliedTagSlug in impliedTagSlugs)
                {
                    string decode = impliedTagSlug.ToString();
                    decode = decode.Replace("%", "%25");
                    var impliedTagResults = await _settings.url
                        .AppendPathSegments($"/api/v1/json/tags/{decode}")
                        .GetAsync()
                        .ReceiveJson();
                    var impliedTagId = impliedTagResults.tag.id;
                    tagIds.Add(impliedTagId);
                }
            }

            var combinedList = new List<Tuple<long, string>>();
            var tagNames = await GetTagNamesAsync(tagIds);
            for (var x = 0; x < tagIds.Count; x++)
            {
                var combinedTag = new Tuple<long, string>((long)tagIds[x], tagNames[x]);
                combinedList.Add(combinedTag);
            }

            var dedupedList = combinedList.Distinct().ToList();
            settings.redList = dedupedList;
            await FileHelper.SaveServerSettingsAsync(settings, context);
        }

        private async Task<List<string>> GetTagNamesAsync(List<object> tagIdList)
        {
            var tagNameList = new List<string>();
            foreach (var tagId in tagIdList)
            {
                var name = await GetTagNameByIdAsync((long)tagId);
                tagNameList.Add(name);
            }

            return tagNameList;
        }
    }
}
