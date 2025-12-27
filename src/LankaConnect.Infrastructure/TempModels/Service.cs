using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class Service
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? Duration { get; set; }

    public bool IsActive { get; set; }

    public Guid BusinessId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public decimal? PriceAmount { get; set; }

    public int? PriceCurrency { get; set; }

    public virtual Business Business { get; set; } = null!;
}
