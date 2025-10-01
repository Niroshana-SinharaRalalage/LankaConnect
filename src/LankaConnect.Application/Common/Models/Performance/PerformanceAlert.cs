using System;

namespace LankaConnect.Application.Common.Models.Performance
{
    /// <summary>
    /// Represents a performance alert with cultural intelligence context
    /// for the LankaConnect platform monitoring system.
    /// </summary>
    public class PerformanceAlert
    {
        /// <summary>
        /// Gets the unique identifier for this alert.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the type of performance alert (e.g., CPU_HIGH, MEMORY_HIGH, CULTURAL_LOAD).
        /// </summary>
        public string AlertType { get; }

        /// <summary>
        /// Gets the alert message describing the performance issue.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the severity level of the alert (e.g., Low, Medium, High, Critical).
        /// </summary>
        public string Severity { get; }

        /// <summary>
        /// Gets the timestamp when the alert was created.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets or sets the cultural event context associated with this alert.
        /// Used for cultural intelligence analysis and load prediction.
        /// </summary>
        public string? CulturalEventContext { get; set; }

        /// <summary>
        /// Gets or sets the geographic region affected by this performance alert.
        /// Important for diaspora community clustering and regional load balancing.
        /// </summary>
        public string? AffectedRegion { get; set; }

        /// <summary>
        /// Initializes a new instance of the PerformanceAlert class.
        /// </summary>
        /// <param name="alertType">The type of performance alert.</param>
        /// <param name="message">The alert message.</param>
        /// <param name="severity">The severity level.</param>
        /// <exception cref="ArgumentException">Thrown when alertType is null or empty.</exception>
        public PerformanceAlert(string alertType, string message, string severity)
        {
            if (string.IsNullOrEmpty(alertType))
                throw new ArgumentException("Alert type cannot be null or empty", nameof(alertType));

            Id = Guid.NewGuid();
            AlertType = alertType;
            Message = message;
            Severity = severity;
            Timestamp = DateTime.UtcNow;
        }
    }
}