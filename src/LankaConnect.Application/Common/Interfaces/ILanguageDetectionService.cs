using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LankaConnect.Application.Common.Models.MultiLanguage;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Language Detection Service Interface
/// Core language analysis functionality for South Asian diaspora
/// Handles Sinhala, Tamil, Hindi, Urdu, Punjabi, Bengali, Gujarati detection
/// Decomposed from IMultiLanguageAffinityRoutingEngine following DDD principles
/// Performance target: <100ms for real-time detection
/// </summary>
public interface ILanguageDetectionService
{
    /// <summary>
    /// Detect language preferences from user content with cultural context
    /// Performance target: <100ms for real-time detection
    /// </summary>
    /// <param name="userId">User identifier for profile association</param>
    /// <param name="userContent">Content to analyze for language detection</param>
    /// <returns>Language detection result with confidence scores</returns>
    Task<LanguageDetectionResult> DetectLanguagePreferencesAsync(Guid userId, string userContent);

    /// <summary>
    /// Analyze generational language patterns for diaspora communities
    /// Critical for accurate routing across first/second/third generation users
    /// </summary>
    /// <param name="userProfile">User language profile with generational data</param>
    /// <returns>Generational pattern analysis with preferences</returns>
    Task<GenerationalPatternAnalysis> AnalyzeGenerationalPatternAsync(MultiLanguageUserProfile userProfile);

    /// <summary>
    /// Detect multiple languages in content with priority scoring
    /// Handles code-switching common in diaspora communications
    /// </summary>
    /// <param name="content">Multi-language content for analysis</param>
    /// <returns>Dictionary of detected languages with confidence scores</returns>
    Task<Dictionary<SouthAsianLanguage, decimal>> DetectMultipleLanguagesAsync(string content);

    /// <summary>
    /// Analyze language complexity and script requirements
    /// Optimizes for Sinhala, Tamil, Devanagari, and Arabic scripts
    /// </summary>
    /// <param name="languages">Languages to analyze for complexity</param>
    /// <returns>Script complexity analysis results</returns>
    Task<LanguageComplexityAnalysis> AnalyzeLanguageComplexityAsync(List<SouthAsianLanguage> languages);

    /// <summary>
    /// Validate language detection accuracy against user feedback
    /// Continuous improvement for cultural intelligence precision
    /// </summary>
    /// <param name="userId">User providing feedback</param>
    /// <param name="detectionResult">Original detection result</param>
    /// <param name="userCorrection">User's language preference correction</param>
    /// <returns>Validation result with accuracy metrics</returns>
    Task<LanguageDetectionValidation> ValidateDetectionAccuracyAsync(
        Guid userId,
        LanguageDetectionResult detectionResult,
        SouthAsianLanguage userCorrection);

    /// <summary>
    /// Get real-time language detection performance metrics
    /// Monitoring for Fortune 500 SLA compliance
    /// </summary>
    /// <returns>Performance metrics including accuracy, latency, throughput</returns>
    Task<LanguageDetectionMetrics> GetDetectionPerformanceMetricsAsync();
}