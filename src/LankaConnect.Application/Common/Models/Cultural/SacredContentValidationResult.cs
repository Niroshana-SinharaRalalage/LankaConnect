using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Cultural;

public class SacredContentValidationResult : BaseEntity
{
    public Guid ValidationId { get; set; } = Guid.NewGuid();
    public bool IsValid { get; set; } = true;
    public decimal Appropriateness { get; set; } = 1.0m;
    public List<string> ValidationMessages { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}