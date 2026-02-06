using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class StripeCustomer
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string StripeCustomerId { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DateTime StripeCreatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
