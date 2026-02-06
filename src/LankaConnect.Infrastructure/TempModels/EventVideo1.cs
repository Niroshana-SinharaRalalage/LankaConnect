using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class EventVideo1
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public string VideoUrl { get; set; } = null!;

    public string BlobName { get; set; } = null!;

    public string ThumbnailUrl { get; set; } = null!;

    public string ThumbnailBlobName { get; set; } = null!;

    public TimeSpan? Duration { get; set; }

    public string Format { get; set; } = null!;

    public long FileSizeBytes { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime UploadedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Event Event { get; set; } = null!;
}
