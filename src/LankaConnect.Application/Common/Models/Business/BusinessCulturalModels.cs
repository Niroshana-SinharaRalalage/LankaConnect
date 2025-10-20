using System;
using System.Collections.Generic;

namespace LankaConnect.Application.Common.Models.Business;

/// <summary>
/// Business Cultural Models - Application Layer
/// Moved from Stage5MissingTypes.cs to correct architectural layer
/// </summary>

/// <summary>
/// Geographic scope for multi-region operations
/// </summary>
public enum GeographicScope
{
    Local,
    Regional,
    National,
    Continental,
    Global
}

/// <summary>
/// Business cultural context for diaspora engagement
/// </summary>
public class BusinessCulturalContext
{
    public string ContextId { get; set; } = string.Empty;
    public List<string> CulturalAttributes { get; set; } = new();
    public List<string> BusinessSectors { get; set; } = new();
    public string PrimaryCulture { get; set; } = string.Empty;
    public GeographicScope Scope { get; set; }
}

/// <summary>
/// Cross-community connection opportunities for diaspora networking
/// </summary>
public class CrossCommunityConnectionOpportunities
{
    public string OpportunityId { get; set; } = string.Empty;
    public List<string> CommunitiesInvolved { get; set; } = new();
    public string ConnectionType { get; set; } = string.Empty;
    public double SynergyScore { get; set; }
    public List<string> SharedInterests { get; set; } = new();
}

/// <summary>
/// Business discovery opportunity for enterprise connections
/// </summary>
public class BusinessDiscoveryOpportunity
{
    public string OpportunityId { get; set; } = string.Empty;
    public string BusinessSector { get; set; } = string.Empty;
    public List<string> TargetCommunities { get; set; } = new();
    public decimal EstimatedValue { get; set; }
    public double SuccessProbability { get; set; }
}
