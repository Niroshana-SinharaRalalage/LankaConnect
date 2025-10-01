using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Security;

public class RegionalComplianceAlignmentResult : BaseEntity
{
    public Guid AlignmentId { get; set; } = Guid.NewGuid();
    public string RegionId { get; set; } = string.Empty;
    public bool IsAligned { get; set; } = true;
    public List<string> Gaps { get; set; } = new();
    public decimal ComplianceScore { get; set; } = 1.0m;
}