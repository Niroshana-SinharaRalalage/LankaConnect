using System;

namespace LankaConnect.Domain.Common.Enums
{
    /// <summary>
    /// Performance objective levels for cultural intelligence platform operations
    /// Supports Fortune 500 SLA requirements and cultural event performance monitoring
    /// </summary>
    public enum PerformanceObjective
    {
        /// <summary>
        /// Standard performance objective for regular cultural intelligence operations
        /// Target: 95% availability, &lt;500ms response time
        /// </summary>
        Standard = 1,

        /// <summary>
        /// High performance objective for premium cultural intelligence features
        /// Target: 99% availability, &lt;200ms response time
        /// </summary>
        High = 2,

        /// <summary>
        /// Critical performance objective for essential cultural intelligence services
        /// Target: 99.9% availability, &lt;100ms response time
        /// </summary>
        Critical = 3,

        /// <summary>
        /// Fortune 500 SLA performance objective for enterprise cultural intelligence
        /// Target: 99.99% availability, &lt;50ms response time, zero data loss
        /// </summary>
        FortuneF500SLA = 4
    }
}