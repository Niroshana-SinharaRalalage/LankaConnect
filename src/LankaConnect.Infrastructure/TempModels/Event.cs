using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NpgsqlTypes;

namespace LankaConnect.Infrastructure.TempModels;

public partial class Event
{
    public Guid Id { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public Guid OrganizerId { get; set; }

    public int Capacity { get; set; }

    public string Status { get; set; } = null!;

    public string? CancellationReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string Description { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? AddressCity { get; set; }

    public string? AddressCountry { get; set; }

    public string? AddressState { get; set; }

    public string? AddressStreet { get; set; }

    public string? AddressZipCode { get; set; }

    public decimal? CoordinatesLatitude { get; set; }

    public decimal? CoordinatesLongitude { get; set; }

    public bool? HasLocation { get; set; }

    public Point? Location { get; set; }

    public string Category { get; set; } = null!;

    public NpgsqlTsVector? SearchVector { get; set; }

    public string? Pricing { get; set; }

    public string? TicketPrice { get; set; }

    public DateTime? PublishedAt { get; set; }

    public virtual ICollection<EventBadge> EventBadges { get; set; } = new List<EventBadge>();

    public virtual ICollection<EventEmailGroup> EventEmailGroups { get; set; } = new List<EventEmailGroup>();

    public virtual EventImage? EventImage { get; set; }

    public virtual ICollection<EventVideo1> EventVideo1s { get; set; } = new List<EventVideo1>();

    public virtual ICollection<EventVideo> EventVideos { get; set; } = new List<EventVideo>();

    public virtual ICollection<EventWaitingList> EventWaitingLists { get; set; } = new List<EventWaitingList>();

    public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();

    public virtual ICollection<SignUpList> SignUpLists { get; set; } = new List<SignUpList>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
