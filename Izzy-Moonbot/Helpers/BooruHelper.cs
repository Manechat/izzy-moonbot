using System;
using System.Threading.Tasks;
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
        
        var image = new BooruImage
        {
            CreatedAt = results.image.created_at,
            Id = results.image.id,
            Spoilered = results.image.spoilered,
            ThumbnailsGenerated = results.image.thumbnails_generated
        };

        // Special parameters which need to be initialised outside the object assignment.
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
        image.Representations = new BooruImagesRepresentations(image.Id, image.Format, image.CreatedAt);

        return image;
    }
    
    private static BooruSettings _getBooruSettings()
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
    public DateTimeOffset CreatedAt { get; set; }
    public BooruImageFormat Format { get; set; }
    public long Id { get; set; }
    public BooruImagesRepresentations? Representations { get; set; }
    public bool Spoilered { get; set; }
    public bool ThumbnailsGenerated { get; set; }

    public BooruImage() { }
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
        _rawUrl = $"https://static.manebooru.art/img/{createdAt.Year}/{createdAt.Month}/{createdAt.Day}/{id}";
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
    public string Medium => _getRepresentation("medium");
    public string Small => _getRepresentation("small");
    public string Tall => _getRepresentation("tall");
    public string Thumbnail => _getRepresentation("thumb");
    public string ThumbnailSmall => _getRepresentation("thumb_small");
    public string ThumbnailTiny => _getRepresentation("thumb_tiny");
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