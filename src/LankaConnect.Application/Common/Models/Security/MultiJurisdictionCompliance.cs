using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Security;

public class MultiJurisdictionCompliance : BaseEntity
{
    public Guid ComplianceId { get; set; } = Guid.NewGuid();
    public List<string> Jurisdictions { get; set; } = new();
    public Dictionary<string, bool> ComplianceStatus { get; set; } = new();
    public DateTime LastAssessment { get; set; } = DateTime.UtcNow;
}