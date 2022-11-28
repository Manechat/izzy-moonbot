using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Flurl.Http;
using Izzy_Moonbot.Settings;
using Microsoft.Extensions.Configuration;

namespace Izzy_Moonbot.Helpers;

public static class BooruHelper
{
    public static async Task<BooruImage> GetFeaturedImage()
    {
        var booruSettings = _getBooruSettings();

        var results = await $"{booruSettings.Endpoint}/api/{booruSettings.Version}/json/images/featured"
            .WithHeader("user-agent", $"Izzy-Moonbot (Linux x86_64) Flurl.Http/3.2.4 DotNET/6.0")
            .SetQueryParam("key", booruSettings.Token)
            .GetAsync()
            .ReceiveJson();
        
        Console.WriteLine(results.image.created_at);

        var image = new BooruImage
        {
            Animated = (bool)results.image.animated,
            AspectRatio = (double)results.image.aspect_ratio,
            CommentCount = (int)results.image.comment_count,
            CreatedAt = results.image.created_at,
            DeletionReason = results.image.deletion_reason,
            Description = results.image.description,
            Downvotes = (int)results.image.downvotes,
            DuplicateOfId = (int?)results.image.duplicate_of,
            Favourites = (int)results.image.faves,
            FirstSeenAt = results.image.first_seen_at,
            Height = (int)results.image.height,
            HiddenFromUsers = results.image.hidden_from_users,
            Id = results.image.id,
            MimeType = results.image.mime_type,
            FileName = results.image.name,
            OriginalSHA512Hash = results.image.orig_sha512_hash,
            Processed = results.image.processed,
            Score = (int)results.image.score,
            SHA512Hash = results.image.sha512_hash,
            Size = results.image.size,
            SourceUrl = results.image.source_url,
            Spoilered = results.image.spoilered,
            Tags = new List<string>(),
            ThumbnailsGenerated = results.image.thumbnails_generated,
            UpdatedAt = results.image.updated_at != null ? results.image.updated_at : null,
            Uploader = (results.image.uploader, results.image.uploader_id),
            Upvotes = (int)results.image.upvotes,
            ViewUrl = results.image.view_url,
            Width = (int)results.image.width,
            WilsonScore = (double)results.image.wilson_score
        };

        // Special parameters which need to be initialised outside the object assignment.
        image.Duration = image.Animated ? results.image.duration : null;
        image.Format = results.image.format switch
        {
            "png" => BooruImageFormat.PNG,
            "jpg" => BooruImageFormat.JPG,
            "jpeg" => BooruImageFormat.JPEG,
            "svg" => BooruImageFormat.SVG,
            "webm" => BooruImageFormat.WebM,
            "gif" => BooruImageFormat.GIF,
            _ => throw new ArgumentOutOfRangeException(nameof(results.image.format))
        };
        image.Intensities = new BooruImageIntensities(results.image.intensities.ne,
            results.image.intensities.nw,
            results.image.intensities.se,
            results.image.intensities.sw);
        image.Representations = new BooruImagesRepresentations(image.Id, image.Format, image.CreatedAt);
        ((List<object>)results.image.tags).ForEach(tagName => image.Tags.Add(tagName.ToString()));

        return image;
    }
    
    private static BooruSettings _getDiscordSettings()
    {
        var config = new ConfigurationBuilder()
            #if DEBUG
            .AddJsonFile("appsettings.Development.json")
            #else
            .AddJsonFile("appsettings.json")
            #endif
            .Build();

        var section = config.GetSection(nameof(BooruSettings));
        var settings = section.Get<BooruSettings>();
        
        if (settings == null) throw new NullReferenceException("Booru settings is null!");

        return settings;
    }
}

public class BooruImage
{
    public bool Animated { get; set; }
    public double AspectRatio { get; set; }
    public int CommentCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? DeletionReason { get; set; }
    public string? Description { get; set; }
    public int Downvotes { get; set; }
    public long? DuplicateOfId { get; set; }
    public double? Duration { get; set; }
    public int Favourites { get; set; }
    public DateTimeOffset FirstSeenAt { get; set; }
    public BooruImageFormat Format { get; set; }
    public int Height { get; set; }
    public bool HiddenFromUsers { get; set; }
    public long Id { get; set; }
    public BooruImageIntensities? Intensities { get; set; }
    public string MimeType { get; set; }
    public string FileName { get; set; }
    public string OriginalSHA512Hash { get; set; }
    public bool Processed { get; set; }
    public BooruImagesRepresentations? Representations { get; set; }
    public int Score { get; set; }
    public string SHA512Hash { get; set; }
    public long Size { get; set; }
    public string SourceUrl { get; set; }
    public bool Spoilered { get; set; }
    public List<string> Tags { get; set; }
    public bool ThumbnailsGenerated { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public (string, long?) Uploader { get; set; }
    public int Upvotes { get; set; }
    public string ViewUrl { get; set; }
    public int Width { get; set; }
    public double WilsonScore { get; set; }

    public BooruImage() { }
}

public class BooruImageIntensities
{
    public double NorthEast { get; }
    public double NorthWest { get; }
    public double SouthEast { get; }
    public double SouthWest { get; }

    public BooruImageIntensities(double northEast, double northWest, double southEast, double southWest)
    {
        NorthEast = northEast;
        NorthWest = northWest;
        SouthEast = southEast;
        SouthWest = southWest;
    }
}

public class BooruImagesRepresentations
{
    public string Full { get; }
    private string _rawUrl;
    private BooruImageFormat _format;

    public BooruImagesRepresentations(long id, BooruImageFormat format, DateTimeOffset createdAt)
    {
        Full =
            $"https://static.manebooru.art/img/view/{createdAt.Year}/{createdAt.Month}/{createdAt.Day}/{id}.{_imageFormatToString(format)}";
        _rawUrl = $"https://static.manebooru.art/img/view/{createdAt.Year}/{createdAt.Month}/{createdAt.Day}/{id}";
        _format = format;
    }

    private string _imageFormatToString(BooruImageFormat format)
    {
        return format switch
        {
            BooruImageFormat.PNG => "png",
            BooruImageFormat.JPG => "jpg",
            BooruImageFormat.JPEG => "jpeg",
            BooruImageFormat.SVG => "svg",
            BooruImageFormat.WebM => "webm",
            BooruImageFormat.GIF => "gif",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    private string _getRepresentation(string key)
    {
        return $"{_rawUrl}/{key}.{_imageFormatToString(_format)}";
    }

    public string Large => _getRepresentation("large");
    public string Medium => _getRepresentation("large");
    public string Small => _getRepresentation("large");
    public string Tall => _getRepresentation("large");
    public string Thumbnail => _getRepresentation("large");
    public string ThumbnailSmall => _getRepresentation("large");
    public string ThumbnailTiny => _getRepresentation("large");
}

public enum BooruImageFormat
{
    PNG,
    JPG,
    JPEG,
    SVG,
    WebM,
    GIF
}