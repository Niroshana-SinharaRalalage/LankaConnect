using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class Registration
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public Guid? UserId { get; set; }

    public int Quantity { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? AttendeeInfo { get; set; }

    public string? Attendees { get; set; }

    public string? Contact { get; set; }

    public decimal? TotalPriceAmount { get; set; }

    public string? TotalPriceCurrency { get; set; }

    public int PaymentStatus { get; set; }

    public string? StripeCheckoutSessionId { get; set; }

    public string? StripePaymentIntentId { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
