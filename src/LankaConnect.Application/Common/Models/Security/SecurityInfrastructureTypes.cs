using System;
using System.Collections.Generic;
using System.Linq;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;

namespace LankaConnect.Application.Common.Models.Security;

/// <summary>
/// TDD GREEN Phase: Security Infrastructure Types Implementation
/// Cultural intelligence integrated security and infrastructure for LankaConnect platform
/// </summary>

#region ZeroTrust

/// <summary>
/// Zero Trust security configuration with cultural intelligence
/// </summary>
public class ZeroTrustConfiguration
{
    public string ConfigurationId { get; private set; }
    public string ConfigurationName { get; private set; }
    public IReadOnlyList<string> TrustedServices { get; private set; }
    public IReadOnlyList<VerificationLevel> RequiredVerificationLevels { get; private set; }
    public Dictionary<string, string> SecurityPolicies { get; private set; }
    public IReadOnlyList<CulturalEventType> CulturalSecurityExceptions { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets whether configuration includes cultural intelligence considerations
    /// </summary>
    public bool IncludesCulturalIntelligence => CulturalSecurityExceptions.Any();

    /// <summary>
    /// Gets the strictest verification level required
    /// </summary>
    public VerificationLevel StrictestVerificationLevel => RequiredVerificationLevels.Any() ?
        RequiredVerificationLevels.Max() : VerificationLevel.Basic;

    private ZeroTrustConfiguration(string configurationName, IEnumerable<string> trustedServices,
        IEnumerable<VerificationLevel> requiredVerificationLevels, Dictionary<string, string> securityPolicies,
        IEnumerable<CulturalEventType> culturalSecurityExceptions)
    {
        ConfigurationId = Guid.NewGuid().ToString();
        ConfigurationName = configurationName;
        TrustedServices = trustedServices.ToList().AsReadOnly();
        RequiredVerificationLevels = requiredVerificationLevels.ToList().AsReadOnly();
        SecurityPolicies = securityPolicies;
        CulturalSecurityExceptions = culturalSecurityExceptions.ToList().AsReadOnly();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates zero trust configuration
    /// </summary>
    public static ZeroTrustConfiguration Create(string configurationName, IEnumerable<string> trustedServices,
        IEnumerable<VerificationLevel> requiredVerificationLevels, Dictionary<string, string> securityPolicies,
        IEnumerable<CulturalEventType>? culturalSecurityExceptions = null)
    {
        return new ZeroTrustConfiguration(configurationName, trustedServices, requiredVerificationLevels,
            securityPolicies, culturalSecurityExceptions ?? Array.Empty<CulturalEventType>());
    }
}

/// <summary>
/// Zero Trust security result
/// </summary>
public class ZeroTrustResult
{
    public string ResultId { get; private set; }
    public string ConfigurationId { get; private set; }
    public IReadOnlyList<string> VerifiedServices { get; private set; }
    public IReadOnlyList<string> BlockedServices { get; private set; }
    public Dictionary<string, VerificationLevel> ServiceVerificationLevels { get; private set; }
    public bool IsSecurityCompliant { get; private set; }
    public double SecurityScore { get; private set; }
    public DateTime EvaluatedAt { get; private set; }

    /// <summary>
    /// Gets verification success rate
    /// </summary>
    public double VerificationSuccessRate
    {
        get
        {
            var totalServices = VerifiedServices.Count + BlockedServices.Count;
            return totalServices > 0 ? (double)VerifiedServices.Count / totalServices : 1.0;
        }
    }

    /// <summary>
    /// Gets whether all services are verified
    /// </summary>
    public bool AllServicesVerified => !BlockedServices.Any();

    /// <summary>
    /// Gets whether result meets enterprise security standards
    /// </summary>
    public bool MeetsEnterpriseStandards => SecurityScore >= 0.95 && IsSecurityCompliant;

    private ZeroTrustResult(string configurationId, IEnumerable<string> verifiedServices,
        IEnumerable<string> blockedServices, Dictionary<string, VerificationLevel> serviceVerificationLevels,
        bool isSecurityCompliant, double securityScore)
    {
        ResultId = Guid.NewGuid().ToString();
        ConfigurationId = configurationId;
        VerifiedServices = verifiedServices.ToList().AsReadOnly();
        BlockedServices = blockedServices.ToList().AsReadOnly();
        ServiceVerificationLevels = serviceVerificationLevels;
        IsSecurityCompliant = isSecurityCompliant;
        SecurityScore = securityScore;
        EvaluatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates zero trust result
    /// </summary>
    public static ZeroTrustResult Create(string configurationId, IEnumerable<string> verifiedServices,
        IEnumerable<string> blockedServices, Dictionary<string, VerificationLevel> serviceVerificationLevels,
        bool isSecurityCompliant, double securityScore)
    {
        return new ZeroTrustResult(configurationId, verifiedServices, blockedServices,
            serviceVerificationLevels, isSecurityCompliant, securityScore);
    }
}

/// <summary>
/// Verification levels for zero trust security
/// </summary>
public enum VerificationLevel
{
    Basic = 1,
    Standard = 2,
    Enhanced = 3,
    Maximum = 4,
    CulturalEvent = 5
}

#endregion

#region VendorServices

/// <summary>
/// Vendor service configuration and management
/// </summary>
public class VendorService
{
    public string ServiceId { get; private set; }
    public string ServiceName { get; private set; }
    public string VendorName { get; private set; }
    public VendorServiceType ServiceType { get; private set; }
    public IReadOnlyList<string> SupportedRegions { get; private set; }
    public Dictionary<string, string> ServiceConfiguration { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets whether service supports cultural intelligence
    /// </summary>
    public bool SupportsCulturalIntelligence => ServiceConfiguration.ContainsKey("cultural-intelligence") ||
        ServiceName.Contains("Cultural", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether service is globally available
    /// </summary>
    public bool IsGloballyAvailable => SupportedRegions.Contains("global") || SupportedRegions.Count >= 5;

    private VendorService(string serviceName, string vendorName, VendorServiceType serviceType,
        IEnumerable<string> supportedRegions, Dictionary<string, string> serviceConfiguration)
    {
        ServiceId = Guid.NewGuid().ToString();
        ServiceName = serviceName;
        VendorName = vendorName;
        ServiceType = serviceType;
        SupportedRegions = supportedRegions.ToList().AsReadOnly();
        ServiceConfiguration = serviceConfiguration;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates vendor service
    /// </summary>
    public static VendorService Create(string serviceName, string vendorName, VendorServiceType serviceType,
        IEnumerable<string> supportedRegions, Dictionary<string, string>? serviceConfiguration = null)
    {
        return new VendorService(serviceName, vendorName, serviceType, supportedRegions,
            serviceConfiguration ?? new Dictionary<string, string>());
    }
}

/// <summary>
/// Vendor continuity result
/// </summary>
public class VendorContinuityResult
{
    public string ResultId { get; private set; }
    public string VendorServiceId { get; private set; }
    public IReadOnlyList<string> TestedScenarios { get; private set; }
    public Dictionary<string, bool> ScenarioResults { get; private set; }
    public double ContinuityScore { get; private set; }
    public TimeSpan MaxRecoveryTime { get; private set; }
    public bool MeetsSLA { get; private set; }
    public DateTime TestedAt { get; private set; }

    /// <summary>
    /// Gets scenario success rate
    /// </summary>
    public double ScenarioSuccessRate
    {
        get
        {
            if (!ScenarioResults.Any()) return 0.0;
            var successfulScenarios = ScenarioResults.Count(kvp => kvp.Value);
            return (double)successfulScenarios / ScenarioResults.Count;
        }
    }

    /// <summary>
    /// Gets whether all scenarios passed
    /// </summary>
    public bool AllScenariosPassed => ScenarioResults.All(kvp => kvp.Value);

    private VendorContinuityResult(string vendorServiceId, IEnumerable<string> testedScenarios,
        Dictionary<string, bool> scenarioResults, double continuityScore, TimeSpan maxRecoveryTime, bool meetsSLA)
    {
        ResultId = Guid.NewGuid().ToString();
        VendorServiceId = vendorServiceId;
        TestedScenarios = testedScenarios.ToList().AsReadOnly();
        ScenarioResults = scenarioResults;
        ContinuityScore = continuityScore;
        MaxRecoveryTime = maxRecoveryTime;
        MeetsSLA = meetsSLA;
        TestedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates vendor continuity result
    /// </summary>
    public static VendorContinuityResult Create(string vendorServiceId, IEnumerable<string> testedScenarios,
        Dictionary<string, bool> scenarioResults, double continuityScore, TimeSpan maxRecoveryTime, bool meetsSLA)
    {
        return new VendorContinuityResult(vendorServiceId, testedScenarios, scenarioResults,
            continuityScore, maxRecoveryTime, meetsSLA);
    }
}

/// <summary>
/// Vendor service types
/// </summary>
public enum VendorServiceType
{
    CloudStorage,
    DatabaseService,
    MessagingService,
    AnalyticsService,
    SecurityService,
    CulturalIntelligenceService,
    MonitoringService
}

#endregion

#region ValidationCriteria

/// <summary>
/// Validation criteria for security and compliance
/// </summary>
public class ValidationCriteria
{
    public string CriteriaId { get; private set; }
    public string CriteriaName { get; private set; }
    public IReadOnlyList<string> RequiredChecks { get; private set; }
    public Dictionary<string, object> ValidationParameters { get; private set; }
    public ValidationSeverity Severity { get; private set; }
    public IReadOnlyList<CulturalEventType> ApplicableCulturalEvents { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets whether criteria apply to cultural events
    /// </summary>
    public bool AppliesToCulturalEvents => ApplicableCulturalEvents.Any();

    /// <summary>
    /// Gets whether criteria are mandatory
    /// </summary>
    public bool IsMandatory => Severity == ValidationSeverity.Critical || Severity == ValidationSeverity.Mandatory;

    /// <summary>
    /// Gets validation complexity score
    /// </summary>
    public int ComplexityScore => RequiredChecks.Count + ValidationParameters.Count;

    protected ValidationCriteria(string criteriaName, IEnumerable<string> requiredChecks,
        Dictionary<string, object> validationParameters, ValidationSeverity severity,
        IEnumerable<CulturalEventType> applicableCulturalEvents)
    {
        CriteriaId = Guid.NewGuid().ToString();
        CriteriaName = criteriaName;
        RequiredChecks = requiredChecks.ToList().AsReadOnly();
        ValidationParameters = validationParameters;
        Severity = severity;
        ApplicableCulturalEvents = applicableCulturalEvents.ToList().AsReadOnly();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates validation criteria
    /// </summary>
    public static ValidationCriteria Create(string criteriaName, IEnumerable<string> requiredChecks,
        Dictionary<string, object> validationParameters, ValidationSeverity severity,
        IEnumerable<CulturalEventType>? applicableCulturalEvents = null)
    {
        return new ValidationCriteria(criteriaName, requiredChecks, validationParameters, severity,
            applicableCulturalEvents ?? Array.Empty<CulturalEventType>());
    }
}

/// <summary>
/// Validation severity levels
/// </summary>
public enum ValidationSeverity
{
    Info,
    Warning,
    Required,
    Critical,
    Mandatory
}

/// <summary>
/// Alignment validation criteria for security optimization
/// </summary>
public class AlignmentValidationCriteria : ValidationCriteria
{
    public IReadOnlyList<string> AlignmentTargets { get; private set; }
    public double RequiredAlignmentPercentage { get; private set; }

    private AlignmentValidationCriteria(string criteriaName, IEnumerable<string> requiredChecks,
        Dictionary<string, object> validationParameters, ValidationSeverity severity,
        IEnumerable<string> alignmentTargets, double requiredAlignmentPercentage)
        : base(criteriaName, requiredChecks, validationParameters, severity, Array.Empty<CulturalEventType>())
    {
        AlignmentTargets = alignmentTargets.ToList().AsReadOnly();
        RequiredAlignmentPercentage = requiredAlignmentPercentage;
    }

    public static AlignmentValidationCriteria Create(string criteriaName, IEnumerable<string> requiredChecks,
        Dictionary<string, object> validationParameters, ValidationSeverity severity,
        IEnumerable<string> alignmentTargets, double requiredAlignmentPercentage)
    {
        return new AlignmentValidationCriteria(criteriaName, requiredChecks, validationParameters, 
            severity, alignmentTargets, requiredAlignmentPercentage);
    }
}

/// <summary>
/// Integrity validation criteria for disaster recovery
/// </summary>
public class IntegrityValidationCriteria : ValidationCriteria
{
    public IReadOnlyList<string> IntegrityChecks { get; private set; }
    public TimeSpan ValidationTimeout { get; private set; }

    private IntegrityValidationCriteria(string criteriaName, IEnumerable<string> requiredChecks,
        Dictionary<string, object> validationParameters, ValidationSeverity severity,
        IEnumerable<string> integrityChecks, TimeSpan validationTimeout)
        : base(criteriaName, requiredChecks, validationParameters, severity, Array.Empty<CulturalEventType>())
    {
        IntegrityChecks = integrityChecks.ToList().AsReadOnly();
        ValidationTimeout = validationTimeout;
    }

    public static IntegrityValidationCriteria Create(string criteriaName, IEnumerable<string> requiredChecks,
        Dictionary<string, object> validationParameters, ValidationSeverity severity,
        IEnumerable<string> integrityChecks, TimeSpan validationTimeout)
    {
        return new IntegrityValidationCriteria(criteriaName, requiredChecks, validationParameters,
            severity, integrityChecks, validationTimeout);
    }
}

#endregion

#region UserSession

/// <summary>
/// User session with cultural context awareness
/// </summary>
public class UserSession
{
    public string SessionId { get; private set; }
    public string UserId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public string UserAgent { get; private set; }
    public string IPAddress { get; private set; }
    public Dictionary<string, object> SessionData { get; private set; }
    public CulturalSessionContext? CulturalContext { get; private set; }

    /// <summary>
    /// Gets session duration
    /// </summary>
    public TimeSpan SessionDuration => (EndTime ?? DateTime.UtcNow).Subtract(StartTime);

    /// <summary>
    /// Gets whether session is active
    /// </summary>
    public bool IsActive => !EndTime.HasValue;

    /// <summary>
    /// Gets whether session has cultural context
    /// </summary>
    public bool HasCulturalContext => CulturalContext != null;

    /// <summary>
    /// Gets whether session is long-running
    /// </summary>
    public bool IsLongRunning => SessionDuration > TimeSpan.FromHours(4);

    private UserSession(string userId, string userAgent, string ipAddress,
        Dictionary<string, object> sessionData, CulturalSessionContext? culturalContext)
    {
        SessionId = Guid.NewGuid().ToString();
        UserId = userId;
        StartTime = DateTime.UtcNow;
        UserAgent = userAgent;
        IPAddress = ipAddress;
        SessionData = sessionData;
        CulturalContext = culturalContext;
    }

    /// <summary>
    /// Creates user session
    /// </summary>
    public static UserSession Create(string userId, string userAgent, string ipAddress,
        Dictionary<string, object>? sessionData = null, CulturalSessionContext? culturalContext = null)
    {
        return new UserSession(userId, userAgent, ipAddress,
            sessionData ?? new Dictionary<string, object>(), culturalContext);
    }

    /// <summary>
    /// Ends the user session
    /// </summary>
    public void EndSession()
    {
        if (!EndTime.HasValue)
        {
            EndTime = DateTime.UtcNow;
        }
    }
}

/// <summary>
/// Cultural session context
/// </summary>
public record CulturalSessionContext(string CulturalRegion, string PreferredLanguage, 
    CulturalEventType? ActiveCulturalEvent = null);

#endregion

#region TracingConfiguration

/// <summary>
/// Distributed tracing configuration with cultural intelligence
/// </summary>
public class TracingConfiguration
{
    public string ConfigurationId { get; private set; }
    public string ConfigurationName { get; private set; }
    public IReadOnlyList<string> TracedServices { get; private set; }
    public Dictionary<string, string> TracingSettings { get; private set; }
    public TracingLevel DefaultTracingLevel { get; private set; }
    public TracingLevel CulturalEventTracingLevel { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets whether configuration supports cultural event tracing
    /// </summary>
    public bool SupportsCulturalEventTracing => CulturalEventTracingLevel > DefaultTracingLevel;

    /// <summary>
    /// Gets whether configuration uses detailed tracing
    /// </summary>
    public bool UsesDetailedTracing => DefaultTracingLevel >= TracingLevel.Detailed;

    /// <summary>
    /// Gets number of traced services
    /// </summary>
    public int TracedServiceCount => TracedServices.Count;

    private TracingConfiguration(string configurationName, IEnumerable<string> tracedServices,
        Dictionary<string, string> tracingSettings, TracingLevel defaultTracingLevel,
        TracingLevel culturalEventTracingLevel)
    {
        ConfigurationId = Guid.NewGuid().ToString();
        ConfigurationName = configurationName;
        TracedServices = tracedServices.ToList().AsReadOnly();
        TracingSettings = tracingSettings;
        DefaultTracingLevel = defaultTracingLevel;
        CulturalEventTracingLevel = culturalEventTracingLevel;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates tracing configuration
    /// </summary>
    public static TracingConfiguration Create(string configurationName, IEnumerable<string> tracedServices,
        Dictionary<string, string> tracingSettings, TracingLevel defaultTracingLevel,
        TracingLevel? culturalEventTracingLevel = null)
    {
        return new TracingConfiguration(configurationName, tracedServices, tracingSettings, defaultTracingLevel,
            culturalEventTracingLevel ?? defaultTracingLevel);
    }

    /// <summary>
    /// Creates cultural event-aware tracing configuration
    /// </summary>
    public static TracingConfiguration CreateCulturalAware(string configurationName,
        IEnumerable<string> tracedServices, Dictionary<string, string> tracingSettings)
    {
        return Create(configurationName, tracedServices, tracingSettings,
            TracingLevel.Standard, TracingLevel.Detailed);
    }
}

/// <summary>
/// Tracing levels
/// </summary>
public enum TracingLevel
{
    None = 0,
    Basic = 1,
    Standard = 2,
    Detailed = 3,
    Verbose = 4
}

#endregion