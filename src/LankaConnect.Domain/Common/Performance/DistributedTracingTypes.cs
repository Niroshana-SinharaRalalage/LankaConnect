using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Performance
{
    /// <summary>
    /// Metrics for distributed tracing performance monitoring
    /// </summary>
    public class DistributedTracingMetrics
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string MetricsId { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public string SpanId { get; set; } = string.Empty;
        public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset EndTime { get; set; } = DateTimeOffset.UtcNow;
        public TimeSpan Duration { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string OperationName { get; set; } = string.Empty;
        public Dictionary<string, object> Tags { get; set; } = new();
        public List<string> Logs { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, double> CustomMetrics { get; set; } = new();
        public List<string> ChildSpanIds { get; set; } = new();
        public bool HasErrors { get; set; }
    }

    /// <summary>
    /// Synthetic transaction for monitoring
    /// </summary>
    public class SyntheticTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TransactionId { get; set; } = string.Empty;
        public string TransactionName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
        public TimeSpan ExpectedDuration { get; set; }
        public int FrequencyMinutes { get; set; }
        public bool IsEnabled { get; set; } = true;
        public List<string> Endpoints { get; set; } = new();
        public Dictionary<string, string> Headers { get; set; } = new();
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Results from synthetic transaction execution
    /// </summary>
    public class SyntheticTransactionResults
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ResultsId { get; set; } = string.Empty;
        public List<SyntheticTransaction> ExecutedTransactions { get; set; } = new();
        public Dictionary<string, bool> TransactionResults { get; set; } = new();
        public Dictionary<string, TimeSpan> ExecutionTimes { get; set; } = new();
        public Dictionary<string, string> ErrorMessages { get; set; } = new();
        public double OverallSuccessRate { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public List<string> FailedTransactions { get; set; } = new();
        public string ExecutionSummary { get; set; } = string.Empty;
        public DateTimeOffset ExecutedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}