using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class BusinessImage
{
    public string Id { get; set; } = null!;

    public string OriginalUrl { get; set; } = null!;

    public string ThumbnailUrl { get; set; } = null!;

    public string MediumUrl { get; set; } = null!;

    public string LargeUrl { get; set; } = null!;

    public string AltText { get; set; } = null!;

    public string Caption { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public bool IsPrimary { get; set; }

    public long FileSizeBytes { get; set; }

    public string ContentType { get; set; } = null!;

    public DateTime UploadedAt { get; set; }

    public Dictionary<string, string> Metadata { get; set; } = null!;

    public Guid? BusinessId { get; set; }

    public virtual Business? Business { get; set; }
}
