using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class StripeWebhookEvent
{
    public Guid Id { get; set; }

    public string EventId { get; set; } = null!;

    public string EventType { get; set; } = null!;

    public bool Processed { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? ErrorMessage { get; set; }

    public int AttemptCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
