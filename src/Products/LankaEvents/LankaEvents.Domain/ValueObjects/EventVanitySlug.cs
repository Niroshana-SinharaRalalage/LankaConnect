using System.Text.RegularExpressions;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.ValueObjects;

/// <summary>
/// Phase 6A.154: Organizer-controlled vanity URL slug.
///
/// Lets `https://lankaconnect.app/cleveland-show` resolve to the event detail
/// page, instead of the 36-character GUID URL. This VO owns the canonical
/// shape — regex, length bounds, lowercase enforcement, and the reserved-words
/// list that prevents a slug from shadowing any real top-level Next.js route.
///
/// Decisions locked-in 2026-05-27 (architect + user):
/// - D1 VO over plain string (mirrors <see cref="EventTitle"/>).
/// - D2 Regex `^[a-z0-9][a-z0-9-]{2,79}$`, no consecutive hyphens, no trailing
///   hyphen, 3–80 chars, lowercase ASCII only.
/// - D5 Store lowercase only — `Create` REJECTS non-lowercase input rather
///   than silently normalizing, so the user sees the canonical form.
/// - D14 Canonical reserved-words list lives HERE in the Domain, exposed via
///   <see cref="ReservedSlugs"/>; backend + frontend both consume it (FE via
///   the `/api/events/slug-config` endpoint).
/// </summary>
public class EventVanitySlug : ValueObject
{
    public const int MinLength = 3;
    public const int MaxLength = 80;

    /// <summary>
    /// Compiled once. Anchored at both ends. First character must be
    /// alphanumeric (no leading hyphen, no leading digit-only is allowed —
    /// architect D2 explicitly rejects leading digit to avoid `/123`
    /// shadowing the future numeric-id route class).
    /// </summary>
    public static readonly Regex SlugRegex = new(
        @"^[a-z][a-z0-9-]{2,79}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Value { get; }

    private EventVanitySlug(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Null-safe factory. Returns:
    /// <list type="bullet">
    ///   <item><description><c>Success(null)</c> when input is null / empty / whitespace
    ///         (slug is optional; null means "no vanity URL").</description></item>
    ///   <item><description><c>Success(slug)</c> when input matches the canonical shape.</description></item>
    ///   <item><description><c>Failure(reason)</c> when input violates any rule.</description></item>
    /// </list>
    /// </summary>
    public static Result<EventVanitySlug?> Create(string? value)
    {
        // Null / empty / whitespace = "no slug" (legacy backward-compatible default).
        if (string.IsNullOrWhiteSpace(value))
            return Result<EventVanitySlug?>.Success(null);

        // No leading/trailing whitespace coercion — if the user typed spaces,
        // surface the error so they fix the input rather than us silently
        // mangling it. Length checks before regex give clearer error messages.
        if (value.Length < MinLength)
            return Result<EventVanitySlug?>.Failure(
                $"Slug must be at least {MinLength} characters.");

        if (value.Length > MaxLength)
            return Result<EventVanitySlug?>.Failure(
                $"Slug cannot exceed {MaxLength} characters.");

        // D5: reject non-lowercase rather than coerce. The organizer needs to
        // see the canonical form; silent normalization breeds confusion later.
        if (value != value.ToLowerInvariant())
            return Result<EventVanitySlug?>.Failure(
                "Slug must be lowercase.");

        if (!SlugRegex.IsMatch(value))
            return Result<EventVanitySlug?>.Failure(
                "Slug must start with a letter and contain only lowercase letters, digits, and hyphens.");

        // Architect D2 — defense against pathological hyphenation that the
        // regex `[a-z0-9-]` alone doesn't catch.
        if (value.Contains("--"))
            return Result<EventVanitySlug?>.Failure(
                "Slug cannot contain consecutive hyphens.");

        if (value.EndsWith('-'))
            return Result<EventVanitySlug?>.Failure(
                "Slug cannot end with a hyphen.");

        // D14: reserved-words check is the last validation step — message is
        // user-actionable ("`events` is reserved" vs. generic "invalid slug").
        if (ReservedSlugs.Contains(value))
            return Result<EventVanitySlug?>.Failure(
                $"'{value}' is a reserved word and cannot be used as a vanity URL.");

        return Result<EventVanitySlug?>.Success(new EventVanitySlug(value));
    }

    /// <summary>
    /// Canonical reserved-words list. Single source of truth — both backend
    /// validators and the frontend Zod schema consume this (FE pulls via
    /// <c>GET /api/events/slug-config</c>).
    ///
    /// Curated 2026-05-27 by walking <c>web/src/app/*</c> directories +
    /// extracting auth/dashboard route-group children + adding well-known
    /// static paths + future namespace squatters + brand/trademark blocks.
    ///
    /// Change-control: any new top-level route under <c>web/src/app/</c>
    /// MUST be added here. CI test (Phase 6A.154 substream M) enforces this
    /// at build time so a missing reservation fails the build, not prod.
    /// </summary>
    public static readonly IReadOnlySet<string> ReservedSlugs = new HashSet<string>(StringComparer.Ordinal)
    {
        // Top-level app routes (walked from web/src/app/* on 2026-05-27)
        "about", "animation-preview", "api", "blog", "business", "contact",
        "dashboard", "events", "forums", "guidelines", "help", "lanka-events",
        "lanka-events-logos", "marketplace", "newsletter", "newsletters",
        "safety", "search", "templates",

        // Auth route group (web/src/app/(auth)/* children)
        "forgot-password", "login", "register", "reset-password", "verify-email",

        // Dashboard route group (web/src/app/(dashboard)/* children)
        "notifications", "profile",

        // Well-known static paths Next.js + browsers expect at root
        "robots.txt", "sitemap.xml", "favicon.ico", "manifest.json",
        ".well-known", "_next", "static", "assets", "public",

        // Future namespace — reserved BEFORE any feature ships so squatters
        // can't grab them. Add to this list when planning a new top-level
        // route; do NOT remove without a deliberate migration plan.
        "admin", "support", "terms", "privacy", "legal", "settings",
        "account", "billing", "payments", "checkout", "cart", "organizer",
        "ticket", "tickets", "refund", "refunds", "refund-request",
        "refund-requests", "my", "me", "user", "users", "group", "groups",
        "community", "communities", "post", "posts", "comment", "comments",
        "like", "likes", "share", "follow", "followers", "following",
        "message", "messages", "chat", "notification", "embed", "oembed",
        "og", "share-card", "qr", "app", "mobile", "web", "ios", "android",
        "desktop", "download", "downloads", "status", "health", "healthz",
        "metrics", "ping", "version",

        // Brand/trademark blocks — organizer can't squat the platform name.
        "lankaconnect", "lc", "sri-lanka", "srilanka", "ceylon",
    };

    public override string ToString() => Value;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
