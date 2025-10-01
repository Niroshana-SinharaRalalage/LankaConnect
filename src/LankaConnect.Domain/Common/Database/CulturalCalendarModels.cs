using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Common;

public enum CulturalCalendarType
{
    Buddhist,
    Hindu,
    Islamic,
    Sikh,
    Unified
}

public enum BuddhistEventType
{
    BuddhistPoyaDay,
    Vesak,
    Kathina,
    Magha,
    Asalha,
    Uposatha
}

public enum HinduEventType
{
    Diwali,
    Holi,
    Navaratri,
    Dussehra,
    Karva_Chauth,
    Makar_Sankranti,
    Ram_Navami,
    Krishna_Janmashtami
}

public enum IslamicEventType
{
    Eid_al_Fitr,
    Eid_al_Adha,
    Ramadan_Start,
    Laylat_al_Qadr,
    Mawlid_al_Nabi,
    Ashura
}

public enum SikhEventType
{
    Vaisakhi,
    Guru_Nanak_Jayanti,
    Guru_Gobind_Singh_Jayanti,
    Hola_Mohalla,
    Diwali_Bandi_Chhor
}

public enum CalendarAccuracyLevel
{
    Astronomical,
    CommunityAuthority,
    RegionalConsensus,
    EstimatedCalculation
}

public class BuddhistCalendarSyncRequest
{
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public BuddhistEventType EventType { get; set; }
    public DateTime ProposedEventDate { get; set; }
    public List<string> TargetRegions { get; set; } = new();
    public string BuddhistAuthority { get; set; } = string.Empty; // Sri Lankan, Thai, Myanmar
    public bool RequireLunarValidation { get; set; } = true;
    public Dictionary<string, DateTime> RegionalVariations { get; set; } = new();
    public CulturalSignificance EventSignificance { get; set; }
    public string CommunityContext { get; set; } = string.Empty;
}

public class BuddhistCalendarSyncResult
{
    public Guid SyncId { get; set; }
    public bool SynchronizationSuccessful { get; set; }
    public DateTime FinalEventDate { get; set; }
    public Dictionary<string, DateTime> RegionSpecificDates { get; set; } = new();
    public bool LunarValidationPassed { get; set; }
    public string AuthoritativeSource { get; set; } = string.Empty;
    public List<string> SynchronizedRegions { get; set; } = new();
    public TimeSpan SynchronizationDuration { get; set; }
    public List<string> CommunityNotifications { get; set; } = new();
    public double CommunityAcceptanceScore { get; set; }
}

public class HinduCalendarSyncRequest
{
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public HinduEventType EventType { get; set; }
    public DateTime ProposedEventDate { get; set; }
    public List<string> TargetRegions { get; set; } = new();
    public string PanchangAuthority { get; set; } = string.Empty; // North Indian, South Indian
    public bool RequireAstrologyValidation { get; set; } = true;
    public Dictionary<string, string> RegionalTraditions { get; set; } = new();
    public CulturalSignificance EventSignificance { get; set; }
    public string TithiDetails { get; set; } = string.Empty;
}

public class HinduCalendarSyncResult
{
    public Guid SyncId { get; set; }
    public bool SynchronizationSuccessful { get; set; }
    public DateTime FinalEventDate { get; set; }
    public Dictionary<string, DateTime> RegionalEventDates { get; set; } = new();
    public bool AstrologyValidationPassed { get; set; }
    public string PanchangConsensus { get; set; } = string.Empty;
    public List<string> TithiCalculations { get; set; } = new();
    public TimeSpan SynchronizationDuration { get; set; }
    public double CommunityConsensusScore { get; set; }
}

public class IslamicCalendarSyncRequest
{
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public IslamicEventType EventType { get; set; }
    public DateTime ProposedEventDate { get; set; }
    public List<string> TargetRegions { get; set; } = new();
    public string IslamicAuthority { get; set; } = string.Empty; // Regional Islamic Council
    public bool RequireMoonSightingValidation { get; set; } = true;
    public Dictionary<string, bool> RegionalSightings { get; set; } = new();
    public CulturalSignificance EventSignificance { get; set; }
    public string HijriDateReference { get; set; } = string.Empty;
}

public class IslamicCalendarSyncResult
{
    public Guid SyncId { get; set; }
    public bool SynchronizationSuccessful { get; set; }
    public DateTime FinalEventDate { get; set; }
    public Dictionary<string, DateTime> RegionalObservanceDates { get; set; } = new();
    public bool MoonSightingConfirmed { get; set; }
    public string IslamicAuthorityConsensus { get; set; } = string.Empty;
    public List<string> RegionalIslamicCouncils { get; set; } = new();
    public TimeSpan SynchronizationDuration { get; set; }
    public double CommunityAccordanceScore { get; set; }
}

public class SikhCalendarSyncRequest
{
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public SikhEventType EventType { get; set; }
    public DateTime ProposedEventDate { get; set; }
    public List<string> TargetRegions { get; set; } = new();
    public string GurudwaraAuthority { get; set; } = string.Empty;
    public bool UseNanakshahiCalendar { get; set; } = true;
    public Dictionary<string, string> RegionalObservances { get; set; } = new();
    public CulturalSignificance EventSignificance { get; set; }
}

public class SikhCalendarSyncResult
{
    public Guid SyncId { get; set; }
    public bool SynchronizationSuccessful { get; set; }
    public DateTime FinalEventDate { get; set; }
    public bool NanakshahiCalendarUsed { get; set; }
    public Dictionary<string, DateTime> GurudwaraObservances { get; set; } = new();
    public string SikhAuthorityConsensus { get; set; } = string.Empty;
    public TimeSpan SynchronizationDuration { get; set; }
    public double CommunityHarmonyScore { get; set; }
}

public class AstronomicalValidationRequest
{
    public Guid ValidationId { get; set; } = Guid.NewGuid();
    public CulturalEventType EventType { get; set; }
    public DateTime ProposedDate { get; set; }
    public List<string> GeographicRegions { get; set; } = new();
    public CulturalCalendarType CalendarType { get; set; }
    public bool RequirePreciseCalculation { get; set; } = true;
    public Dictionary<string, double> GeographicCoordinates { get; set; } = new();
}

public class AstronomicalValidationResult
{
    public Guid ValidationId { get; set; }
    public bool ValidationSuccessful { get; set; }
    public DateTime AstronomicallyCorrectDate { get; set; }
    public Dictionary<string, DateTime> RegionalAstronomicalDates { get; set; } = new();
    public List<string> CalculationMethods { get; set; } = new();
    public double AccuracyConfidence { get; set; }
    public List<string> AstronomicalNotes { get; set; } = new();
    public TimeSpan ValidationDuration { get; set; }
}

public class CalendarConflictRequest
{
    public Guid ConflictId { get; set; } = Guid.NewGuid();
    public CulturalEventType EventType { get; set; }
    public string ConflictingRegion1 { get; set; } = string.Empty;
    public string ConflictingRegion2 { get; set; } = string.Empty;
    public DateTime Region1Date { get; set; }
    public DateTime Region2Date { get; set; }
    public string Authority1 { get; set; } = string.Empty;
    public string Authority2 { get; set; } = string.Empty;
    public CulturalSignificance ConflictSeverity { get; set; }
    public List<string> AffectedCommunities { get; set; } = new();
}

public class CalendarConflictResolutionResult
{
    public Guid ConflictId { get; set; }
    public bool ResolutionSuccessful { get; set; }
    public DateTime ResolvedDate { get; set; }
    public string ResolutionStrategy { get; set; } = string.Empty;
    public string AuthoritativeDecision { get; set; } = string.Empty;
    public List<string> CommunityNotifications { get; set; } = new();
    public TimeSpan ResolutionDuration { get; set; }
    public double CommunityAcceptanceRate { get; set; }
    public List<string> AlternativeObservanceDates { get; set; } = new();
}

public class CulturalCalendarEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public CulturalEventType EventType { get; set; }
    public CulturalCalendarType CalendarType { get; set; }
    public DateTime EventDate { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public CulturalSignificance Significance { get; set; }
    public CalendarAccuracyLevel AccuracyLevel { get; set; }
    public string CulturalAuthority { get; set; } = string.Empty;
    public List<string> AffectedCommunities { get; set; } = new();
    public Dictionary<string, string> RegionalVariations { get; set; } = new();
    public bool AstronomicallyValidated { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class RegionalVariationSyncRequest
{
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public CulturalEventType BaseEventType { get; set; }
    public DateTime BaseEventDate { get; set; }
    public Dictionary<string, CulturalEventVariation> RegionalVariations { get; set; } = new();
    public bool RequireConsensusBuilding { get; set; } = true;
    public string PrimaryAuthority { get; set; } = string.Empty;
    public List<string> ConsultingAuthorities { get; set; } = new();
}

public class CulturalEventVariation
{
    public string Region { get; set; } = string.Empty;
    public DateTime LocalEventDate { get; set; }
    public string LocalEventName { get; set; } = string.Empty;
    public List<string> LocalCustoms { get; set; } = new();
    public string CulturalAuthority { get; set; } = string.Empty;
    public CulturalSignificance LocalSignificance { get; set; }
    public Dictionary<string, string> CulturalNotes { get; set; } = new();
}

public class RegionalVariationSyncResult
{
    public Guid SyncId { get; set; }
    public bool SynchronizationSuccessful { get; set; }
    public DateTime UnifiedEventDate { get; set; }
    public Dictionary<string, CulturalEventVariation> AcceptedVariations { get; set; } = new();
    public string ConsensusRationale { get; set; } = string.Empty;
    public List<string> ConsultedAuthorities { get; set; } = new();
    public TimeSpan ConsensusBuildingDuration { get; set; }
    public double CulturalHarmonyScore { get; set; }
    public List<string> CommunityFeedback { get; set; } = new();
}

public class CalendarAccuracyMetrics
{
    public DateTime MetricsTimestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<CulturalCalendarType, double> CalendarTypeAccuracy { get; set; } = new();
    public Dictionary<string, double> RegionalAccuracyScores { get; set; } = new();
    public int TotalCalendarEvents { get; set; }
    public int AstronomicallyValidatedEvents { get; set; }
    public int CommunityDisputedEvents { get; set; }
    public double OverallAccuracyScore { get; set; }
    public List<string> AccuracyImprovementRecommendations { get; set; } = new();
    public Dictionary<CulturalEventType, double> EventTypeAccuracy { get; set; } = new();
    public TimeSpan AverageConflictResolutionTime { get; set; }
    public double CommunityAlignmentScore { get; set; }
}