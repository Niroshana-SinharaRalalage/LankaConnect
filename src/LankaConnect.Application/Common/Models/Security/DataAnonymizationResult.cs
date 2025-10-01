using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Security
{
    public class DataAnonymizationResult : BaseEntity
    {
        public string OperationId { get; set; } = string.Empty;
        public string DataSetId { get; set; } = string.Empty;
        public int RecordsProcessed { get; set; }
        public int RecordsAnonymized { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public decimal AnonymizationAccuracy { get; set; }
        public List<string> AnonymizedFields { get; set; } = new();
        public string AnonymizationMethod { get; set; } = string.Empty;
        public bool IsCompliant { get; set; } = true;
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public string ResultHash { get; set; } = string.Empty;
    }


    public class DeletionSchedule : BaseEntity
    {
        public string ScheduleName { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public string DataSetIdentifier { get; set; } = string.Empty;
        public int RecordsToDelete { get; set; }
        public string DeletionReason { get; set; } = string.Empty;
        public bool IsExecuted { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public string BackupLocation { get; set; } = string.Empty;
    }

    public class DataRetentionResult : BaseEntity
    {
        public string OperationId { get; set; } = string.Empty;
        public int RecordsEvaluated { get; set; }
        public int RecordsRetained { get; set; }
        public int RecordsScheduledForDeletion { get; set; }
        public List<string> PolicyViolations { get; set; } = new();
        public decimal ComplianceScore { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}