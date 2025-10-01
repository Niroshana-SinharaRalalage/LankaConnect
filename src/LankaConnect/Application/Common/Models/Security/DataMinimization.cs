using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Security
{
    public class DataProcessingPurpose : BaseEntity
    {
        public string PurposeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string LegalBasis { get; set; } = string.Empty;
        public bool IsEssential { get; set; } = true;
        public List<string> DataCategories { get; set; } = new();
        public TimeSpan RetentionPeriod { get; set; }
        public string ComplianceRegion { get; set; } = string.Empty;
        public bool RequiresConsent { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }

    public class MinimizationStrategy : BaseEntity
    {
        public string StrategyName { get; set; } = string.Empty;
        public string DataCategory { get; set; } = string.Empty;
        public string ReductionMethod { get; set; } = string.Empty;
        public decimal ExpectedReduction { get; set; }
        public List<string> FieldsToMinimize { get; set; } = new();
        public string AnonymizationTechnique { get; set; } = string.Empty;
        public bool PreserveFunctionality { get; set; } = true;
        public DateTime ImplementationDate { get; set; }
        public bool IsApproved { get; set; }
    }

    public class DataMinimizationResult : BaseEntity
    {
        public string OperationId { get; set; } = string.Empty;
        public string DataSetId { get; set; } = string.Empty;
        public int OriginalRecordCount { get; set; }
        public int MinimizedRecordCount { get; set; }
        public decimal ReductionPercentage { get; set; }
        public List<string> MinimizedFields { get; set; } = new();
        public TimeSpan ProcessingTime { get; set; }
        public bool MaintainsFunctionality { get; set; } = true;
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public string QualityScore { get; set; } = string.Empty;
    }
}