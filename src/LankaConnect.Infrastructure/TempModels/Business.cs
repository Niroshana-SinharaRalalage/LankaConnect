using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class Business
{
    public Guid Id { get; set; }

    public string BusinessHours { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string Status { get; set; } = null!;

    public Guid OwnerId { get; set; }

    public decimal? Rating { get; set; }

    public int ReviewCount { get; set; }

    public bool IsVerified { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public long Version { get; set; }

    public string AddressCity { get; set; } = null!;

    public string AddressCountry { get; set; } = null!;

    public string AddressState { get; set; } = null!;

    public string AddressStreet { get; set; } = null!;

    public string AddressZipCode { get; set; } = null!;

    public string? ContactEmail { get; set; }

    public string? ContactFacebook { get; set; }

    public string? ContactInstagram { get; set; }

    public string? ContactPhone { get; set; }

    public string? ContactTwitter { get; set; }

    public string? ContactWebsite { get; set; }

    public string Description { get; set; } = null!;

    public decimal? LocationLatitude { get; set; }

    public decimal? LocationLongitude { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<BusinessImage> BusinessImages { get; set; } = new List<BusinessImage>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}
