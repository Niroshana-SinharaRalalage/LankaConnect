using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class EmailTemplate
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string SubjectTemplate { get; set; } = null!;

    public string TextTemplate { get; set; } = null!;

    public string? HtmlTemplate { get; set; }

    public string Type { get; set; } = null!;

    public string Category { get; set; } = null!;

    public bool IsActive { get; set; }

    public string? Tags { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
