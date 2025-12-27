using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class Ticket
{
    public Guid Id { get; set; }

    public Guid RegistrationId { get; set; }

    public Guid EventId { get; set; }

    public Guid? UserId { get; set; }

    public string TicketCode { get; set; } = null!;

    public string QrCodeData { get; set; } = null!;

    public string? PdfBlobUrl { get; set; }

    public bool IsValid { get; set; }

    public DateTime? ValidatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual Registration Registration { get; set; } = null!;

    public virtual User? User { get; set; }
}
