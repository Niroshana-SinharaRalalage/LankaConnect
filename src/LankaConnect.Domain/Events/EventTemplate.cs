using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Phase 6A.8: Event Template System
/// Pre-designed event templates to help organizers quickly create events
/// </summary>
public class EventTemplate : BaseEntity
{
    /// <summary>
    /// Template name (e.g., "Vesak Day Celebration", "Cricket Match Viewing")
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Brief description of what this template is for
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Event category this template belongs to
    /// </summary>
    public EventCategory Category { get; private set; }

    /// <summary>
    /// Inline SVG code for template preview thumbnail
    /// </summary>
    public string ThumbnailSvg { get; private set; }

    /// <summary>
    /// JSON string containing default values for event creation
    /// Example: { "title": "...", "description": "...", "capacity": 100, "durationHours": 3 }
    /// </summary>
    public string TemplateDataJson { get; private set; }

    /// <summary>
    /// Whether this template is active and should be shown to users
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Display order for template sorting (lower number = higher priority)
    /// </summary>
    public int DisplayOrder { get; private set; }

    // EF Core constructor
    private EventTemplate()
    {
        Name = null!;
        Description = null!;
        ThumbnailSvg = null!;
        TemplateDataJson = null!;
    }

    private EventTemplate(
        string name,
        string description,
        EventCategory category,
        string thumbnailSvg,
        string templateDataJson,
        int displayOrder = 0)
    {
        Name = name;
        Description = description;
        Category = category;
        ThumbnailSvg = thumbnailSvg;
        TemplateDataJson = templateDataJson;
        IsActive = true;
        DisplayOrder = displayOrder;
    }

    /// <summary>
    /// Creates a new event template
    /// </summary>
    public static Result<EventTemplate> Create(
        string name,
        string description,
        EventCategory category,
        string thumbnailSvg,
        string templateDataJson,
        int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<EventTemplate>.Failure("Template name is required");

        if (name.Length > 100)
            return Result<EventTemplate>.Failure("Template name cannot exceed 100 characters");

        if (string.IsNullOrWhiteSpace(description))
            return Result<EventTemplate>.Failure("Template description is required");

        if (description.Length > 500)
            return Result<EventTemplate>.Failure("Template description cannot exceed 500 characters");

        if (string.IsNullOrWhiteSpace(thumbnailSvg))
            return Result<EventTemplate>.Failure("Template thumbnail SVG is required");

        if (string.IsNullOrWhiteSpace(templateDataJson))
            return Result<EventTemplate>.Failure("Template data JSON is required");

        if (displayOrder < 0)
            return Result<EventTemplate>.Failure("Display order cannot be negative");

        var template = new EventTemplate(name, description, category, thumbnailSvg, templateDataJson, displayOrder);
        return Result<EventTemplate>.Success(template);
    }

    /// <summary>
    /// Updates the template's active status
    /// </summary>
    public Result SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Updates the template's display order
    /// </summary>
    public Result UpdateDisplayOrder(int newOrder)
    {
        if (newOrder < 0)
            return Result.Failure("Display order cannot be negative");

        DisplayOrder = newOrder;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Updates the template's thumbnail SVG
    /// </summary>
    public Result UpdateThumbnail(string thumbnailSvg)
    {
        if (string.IsNullOrWhiteSpace(thumbnailSvg))
            return Result.Failure("Thumbnail SVG cannot be empty");

        ThumbnailSvg = thumbnailSvg;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Updates the template data JSON
    /// </summary>
    public Result UpdateTemplateData(string templateDataJson)
    {
        if (string.IsNullOrWhiteSpace(templateDataJson))
            return Result.Failure("Template data JSON cannot be empty");

        TemplateDataJson = templateDataJson;
        MarkAsUpdated();
        return Result.Success();
    }
}
