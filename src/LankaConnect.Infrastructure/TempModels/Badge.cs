using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class Badge
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string ImageUrl { get; set; } = null!;

    public string BlobName { get; set; } = null!;

    public string Position { get; set; } = null!;

    public bool IsActive { get; set; }

    public bool IsSystem { get; set; }

    public int DisplayOrder { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? DefaultDurationDays { get; set; }

    public decimal PositionXDetail { get; set; }

    public decimal PositionXFeatured { get; set; }

    public decimal PositionXListing { get; set; }

    public decimal PositionYDetail { get; set; }

    public decimal PositionYFeatured { get; set; }

    public decimal PositionYListing { get; set; }

    public decimal RotationDetail { get; set; }

    public decimal RotationFeatured { get; set; }

    public decimal RotationListing { get; set; }

    public decimal SizeHeightDetail { get; set; }

    public decimal SizeHeightFeatured { get; set; }

    public decimal SizeHeightListing { get; set; }

    public decimal SizeWidthDetail { get; set; }

    public decimal SizeWidthFeatured { get; set; }

    public decimal SizeWidthListing { get; set; }

    public virtual ICollection<EventBadge> EventBadges { get; set; } = new List<EventBadge>();
}
