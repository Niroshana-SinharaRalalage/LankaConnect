using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class ReferenceValue
{
    public Guid Id { get; set; }

    public string EnumType { get; set; } = null!;

    public string Code { get; set; } = null!;

    public int IntValue { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
