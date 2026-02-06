using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class EventImage
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string BlobName { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public DateTime UploadedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsPrimary { get; set; }

    public virtual Event Event { get; set; } = null!;
}
