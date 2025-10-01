using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Business.ValueObjects;

public class SocialMediaLinks : ValueObject
{
    public string? Facebook { get; }
    public string? Instagram { get; }
    public string? Twitter { get; }
    public string? LinkedIn { get; }
    public string? TikTok { get; }
    public string? YouTube { get; }

    private SocialMediaLinks(
        string? facebook,
        string? instagram,
        string? twitter,
        string? linkedIn,
        string? tikTok,
        string? youTube)
    {
        Facebook = facebook;
        Instagram = instagram;
        Twitter = twitter;
        LinkedIn = linkedIn;
        TikTok = tikTok;
        YouTube = youTube;
    }

    public static Result<SocialMediaLinks> Create(
        string? facebook = null,
        string? instagram = null,
        string? twitter = null,
        string? linkedIn = null,
        string? tikTok = null,
        string? youTube = null)
    {
        // Validate URLs if provided
        var urls = new[]
        {
            (facebook, nameof(facebook)),
            (instagram, nameof(instagram)),
            (twitter, nameof(twitter)),
            (linkedIn, nameof(linkedIn)),
            (tikTok, nameof(tikTok)),
            (youTube, nameof(youTube))
        };

        foreach (var (url, name) in urls)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                if (url.Length > 200)
                    return Result<SocialMediaLinks>.Failure($"{name} URL cannot exceed 200 characters");

                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != "http" && uri.Scheme != "https"))
                {
                    return Result<SocialMediaLinks>.Failure($"Invalid {name} URL format");
                }
            }
        }

        return Result<SocialMediaLinks>.Success(new SocialMediaLinks(
            string.IsNullOrWhiteSpace(facebook) ? null : facebook.Trim(),
            string.IsNullOrWhiteSpace(instagram) ? null : instagram.Trim(),
            string.IsNullOrWhiteSpace(twitter) ? null : twitter.Trim(),
            string.IsNullOrWhiteSpace(linkedIn) ? null : linkedIn.Trim(),
            string.IsNullOrWhiteSpace(tikTok) ? null : tikTok.Trim(),
            string.IsNullOrWhiteSpace(youTube) ? null : youTube.Trim()
        ));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        if (Facebook != null) yield return Facebook;
        if (Instagram != null) yield return Instagram;
        if (Twitter != null) yield return Twitter;
        if (LinkedIn != null) yield return LinkedIn;
        if (TikTok != null) yield return TikTok;
        if (YouTube != null) yield return YouTube;
    }

    public bool HasAnyLinks()
    {
        return !string.IsNullOrWhiteSpace(Facebook) ||
               !string.IsNullOrWhiteSpace(Instagram) ||
               !string.IsNullOrWhiteSpace(Twitter) ||
               !string.IsNullOrWhiteSpace(LinkedIn) ||
               !string.IsNullOrWhiteSpace(TikTok) ||
               !string.IsNullOrWhiteSpace(YouTube);
    }

    public override string ToString()
    {
        var links = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(Facebook)) links.Add($"Facebook: {Facebook}");
        if (!string.IsNullOrWhiteSpace(Instagram)) links.Add($"Instagram: {Instagram}");
        if (!string.IsNullOrWhiteSpace(Twitter)) links.Add($"Twitter: {Twitter}");
        if (!string.IsNullOrWhiteSpace(LinkedIn)) links.Add($"LinkedIn: {LinkedIn}");
        if (!string.IsNullOrWhiteSpace(TikTok)) links.Add($"TikTok: {TikTok}");
        if (!string.IsNullOrWhiteSpace(YouTube)) links.Add($"YouTube: {YouTube}");

        return string.Join(" | ", links);
    }
}