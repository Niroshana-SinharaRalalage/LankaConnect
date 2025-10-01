using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.MultiLanguage
{
    public class CachePreWarmingResult : BaseEntity
    {
        public string PreWarmingId { get; set; } = string.Empty;
        public List<string> CachedLanguagePairs { get; set; } = new();
        public int CacheEntriesCreated { get; set; }
        public TimeSpan PreWarmingDuration { get; set; }
        public decimal CacheHitRateImprovement { get; set; }
        public List<string> OptimizationActions { get; set; } = new();
        public bool PreWarmingSuccessful { get; set; } = true;
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Completed";
    }

    public class CacheInvalidationStrategy : BaseEntity
    {
        public string StrategyName { get; set; } = string.Empty;
        public string InvalidationType { get; set; } = string.Empty;
        public List<string> TriggerConditions { get; set; } = new();
        public TimeSpan InvalidationDelay { get; set; }
        public bool CascadeInvalidation { get; set; } = true;
        public List<string> AffectedCacheKeys { get; set; } = new();
        public string Priority { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class CacheInvalidationResult : BaseEntity
    {
        public string InvalidationId { get; set; } = string.Empty;
        public string StrategyUsed { get; set; } = string.Empty;
        public int CacheEntriesInvalidated { get; set; }
        public TimeSpan InvalidationDuration { get; set; }
        public bool InvalidationSuccessful { get; set; } = true;
        public List<string> FailedInvalidations { get; set; } = new();
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public string Impact { get; set; } = string.Empty;
    }

    public class LanguageRoutingFailoverResult : BaseEntity
    {
        public string FailoverId { get; set; } = string.Empty;
        public string OriginalRoute { get; set; } = string.Empty;
        public string FailoverRoute { get; set; } = string.Empty;
        public string FailoverReason { get; set; } = string.Empty;
        public TimeSpan FailoverDuration { get; set; }
        public bool FailoverSuccessful { get; set; } = true;
        public List<string> ServicesAffected { get; set; } = new();
        public DateTime FailoverInitiatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Active";
    }

    public class CulturalIntelligenceState : BaseEntity
    {
        public string StateId { get; set; } = string.Empty;
        public string CulturalContext { get; set; } = string.Empty;
        public Dictionary<string, object> StateData { get; set; } = new();
        public string Version { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public bool RequiresBackup { get; set; } = true;
        public List<string> DependentServices { get; set; } = new();
        public string Status { get; set; } = "Active";
    }

    public class CulturalIntelligencePreservationResult : BaseEntity
    {
        public string PreservationId { get; set; } = string.Empty;
        public string CulturalContext { get; set; } = string.Empty;
        public List<string> PreservedData { get; set; } = new();
        public string BackupLocation { get; set; } = string.Empty;
        public bool PreservationSuccessful { get; set; } = true;
        public TimeSpan PreservationDuration { get; set; }
        public string IntegrityVerification { get; set; } = string.Empty;
        public DateTime PreservedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Preserved";
    }
}