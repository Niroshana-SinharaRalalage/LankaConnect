using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Database
{
    public enum SearchComplexityLevel
    {
        Simple,
        Moderate,
        Complex,
        Advanced
    }

    public class MultilingualSearchMetrics
    {
        public Guid Id { get; set; }
        public Dictionary<string, int> LanguageSearchCounts { get; set; } = new();
        public Dictionary<string, double> LanguageSearchPerformance { get; set; } = new();
        public TimeSpan AverageSearchTime { get; set; }
        public double SearchAccuracy { get; set; }
        public DateTime MetricsTimestamp { get; set; }
    }

    public class CulturalCorrelationAnalysis
    {
        public Guid Id { get; set; }
        public Dictionary<string, string> CulturalCorrelations { get; set; } = new();
        public double CorrelationStrength { get; set; }
        public List<string> IdentifiedPatterns { get; set; } = new();
        public DateTime AnalysisTimestamp { get; set; }
    }

    public class DatabasePerformanceSnapshot
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
        public DateTime SnapshotTimestamp { get; set; }
        public string DatabaseInstance { get; set; } = string.Empty;
        public List<string> ActiveQueries { get; set; } = new();
    }


    public class QueryComplexityThreshold
    {
        public Guid Id { get; set; }
        public string QueryType { get; set; } = string.Empty;
        public TimeSpan MaxExecutionTime { get; set; }
        public int MaxRowsReturned { get; set; }
        public string ComplexityLevel { get; set; } = string.Empty;
    }

    public class QueryPerformanceAnalysis
    {
        public Guid Id { get; set; }
        public string QueryHash { get; set; } = string.Empty;
        public TimeSpan ExecutionTime { get; set; }
        public string PerformanceRating { get; set; } = string.Empty;
        public List<string> OptimizationSuggestions { get; set; } = new();
        public DateTime AnalysisTimestamp { get; set; }
    }

    public class StorageUtilizationMetrics
    {
        public Guid Id { get; set; }
        public long TotalStorageBytes { get; set; }
        public long UsedStorageBytes { get; set; }
        public double UtilizationPercentage { get; set; }
        public Dictionary<string, long> TableSizes { get; set; } = new();
        public DateTime MetricsTimestamp { get; set; }
    }

    public enum TransactionIsolationLevel
    {
        ReadUncommitted,
        ReadCommitted,
        RepeatableRead,
        Serializable
    }

    public class TransactionPerformanceMetrics
    {
        public Guid Id { get; set; }
        public Guid TransactionId { get; set; }
        public TimeSpan TransactionDuration { get; set; }
        public TransactionIsolationLevel IsolationLevel { get; set; }
        public bool IsSuccessful { get; set; }
        public DateTime TransactionTimestamp { get; set; }
    }

    public class IndexPerformanceAnalysis
    {
        public Guid Id { get; set; }
        public string IndexName { get; set; } = string.Empty;
        public double IndexEfficiency { get; set; }
        public long IndexSizeBytes { get; set; }
        public List<string> RecommendedOptimizations { get; set; } = new();
        public DateTime AnalysisTimestamp { get; set; }
    }

    public enum BackupStrategy
    {
        Full,
        Incremental,
        Differential,
        TransactionLog
    }

    public class BackupRecoveryMetrics
    {
        public Guid Id { get; set; }
        public BackupStrategy Strategy { get; set; }
        public TimeSpan BackupDuration { get; set; }
        public long BackupSizeBytes { get; set; }
        public bool IsSuccessful { get; set; }
        public DateTime BackupTimestamp { get; set; }
    }

    public class PerformanceDataset
    {
        public Guid Id { get; set; }
        public string DatasetName { get; set; } = string.Empty;
        public Dictionary<string, object> DataPoints { get; set; } = new();
        public DateTime CollectionTimestamp { get; set; }
        public string DataSource { get; set; } = string.Empty;
    }
}