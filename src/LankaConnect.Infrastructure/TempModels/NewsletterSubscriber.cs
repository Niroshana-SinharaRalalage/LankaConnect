using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class NewsletterSubscriber
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public Guid? MetroAreaId { get; set; }

    public bool ReceiveAllLocations { get; set; }

    public bool IsActive { get; set; }

    public bool IsConfirmed { get; set; }

    public string? ConfirmationToken { get; set; }

    public DateTime? ConfirmationSentAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public string UnsubscribeToken { get; set; } = null!;

    public DateTime? UnsubscribedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public byte[] Version { get; set; } = null!;
}
