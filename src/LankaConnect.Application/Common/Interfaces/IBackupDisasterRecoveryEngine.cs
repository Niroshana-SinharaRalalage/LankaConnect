using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using LankaConnect.Application.Common.Models.Backup;
using LankaConnect.Domain.Shared.Types;
using LankaConnect.Application.Common.Models.Performance;
using LankaConnect.Application.Common.Models.Security;
using CriticalModels = LankaConnect.Application.Common.Models.Critical;
using LankaConnect.Domain.Common.Enums;
using DisasterRecoveryModels = LankaConnect.Application.Common.DisasterRecovery;
using LankaConnect.Application.Common.DisasterRecovery;
using LankaConnect.Application.Common.Enterprise;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.DisasterRecovery;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common;
using LankaConnect.Application.Common.Models.Critical;

namespace LankaConnect.Application.Common.Interfaces
{
    /// <summary>
    /// Comprehensive interface for backup and disaster recovery operations
    /// for LankaConnect's cultural intelligence platform with multi-region deployment support
    /// </summary>
    public interface IBackupDisasterRecoveryEngine
    {
        #region Cultural Intelligence-Aware Backup Operations
        
        /// <summary>
        /// Initiates cultural intelligence-aware backup with event sensitivity
        /// </summary>
        Task<BackupOperationResult> InitiateCulturalIntelligenceBackupAsync(
            CulturalBackupConfiguration configuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates priority backup for active cultural events
        /// </summary>
        Task<BackupOperationResult> CreateCulturalEventPriorityBackupAsync(
            Guid culturalEventId,
            BackupPriority priority,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Schedules incremental backup based on cultural activity patterns
        /// </summary>
        Task<BackupScheduleResult> ScheduleCulturalActivityIncrementalBackupAsync(
            CulturalActivityPattern activityPattern,
            LankaConnect.Domain.Shared.Types.BackupFrequency frequency,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates diaspora community-specific backup partitions
        /// </summary>
        Task<BackupOperationResult> CreateDiasporaCommunityBackupPartitionAsync(
            Guid communityId,
            GeographicRegion region,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Backs up cultural intelligence models and learning data
        /// </summary>
        Task<BackupOperationResult> BackupCulturalIntelligenceModelsAsync(
            ModelBackupConfiguration configuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates backup of cultural event prediction algorithms
        /// </summary>
        Task<BackupOperationResult> BackupCulturalEventPredictionAlgorithmsAsync(
            AlgorithmBackupScope scope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs multi-language content backup with cultural context preservation
        /// </summary>
        Task<BackupOperationResult> BackupMultiLanguageCulturalContentAsync(
            List<CultureInfo> supportedCultures,
            ContentBackupOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates backup of user cultural preferences and affinity data
        /// </summary>
        Task<BackupOperationResult> BackupUserCulturalAffinityDataAsync(
            UserSegment userSegment,
            DataRetentionPolicy retentionPolicy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Backs up cultural event attendance and engagement metrics
        /// </summary>
        Task<BackupOperationResult> BackupCulturalEngagementMetricsAsync(
            DateRange dateRange,
            MetricAggregationLevel aggregationLevel,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates backup of cultural conflict resolution data
        /// </summary>
        Task<BackupOperationResult> BackupCulturalConflictResolutionDataAsync(
            ConflictResolutionScope scope,
            CancellationToken cancellationToken = default);

        #endregion

        #region Multi-Region Disaster Recovery Coordination

        /// <summary>
        /// Coordinates multi-region disaster recovery failover
        /// </summary>
        Task<LankaConnect.Domain.Shared.Types.DisasterRecoveryResult> CoordinateMultiRegionFailoverAsync(
            LankaConnect.Domain.Shared.Types.DisasterScenario scenario,
            LankaConnect.Domain.Shared.Types.FailoverStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Initiates cross-region data synchronization for disaster recovery
        /// </summary>
        Task<SynchronizationResult> InitiateCrossRegionDataSynchronizationAsync(
            List<GeographicRegion> sourceRegions,
            GeographicRegion targetRegion,
            LankaConnect.Domain.Common.Enums.SynchronizationPriority priority,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates multi-region disaster recovery readiness
        /// </summary>
        Task<ReadinessValidationResult> ValidateMultiRegionDisasterRecoveryReadinessAsync(
            List<GeographicRegion> regions,
            RecoveryScenario scenario,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes regional failback after disaster recovery
        /// </summary>
        Task<FailbackResult> ExecuteRegionalFailbackAsync(
            GeographicRegion primaryRegion,
            GeographicRegion recoveryRegion,
            FailbackStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Monitors cross-region replication lag for disaster recovery
        /// </summary>
        Task<ReplicationStatus> MonitorCrossRegionReplicationLagAsync(
            GeographicRegion sourceRegion,
            GeographicRegion targetRegion,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Coordinates cultural intelligence failover across regions
        /// </summary>
        Task<CulturalIntelligenceFailoverResult> CoordinateCulturalIntelligenceFailoverAsync(
            CulturalIntelligenceFailoverConfiguration configuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages regional capacity scaling during disaster recovery
        /// </summary>
        Task<CapacityScalingResult> ManageRegionalCapacityScalingAsync(
            GeographicRegion region,
            DisasterRecoveryLoadProfile loadProfile,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Synchronizes cultural event data across disaster recovery regions
        /// </summary>
        Task<EventSynchronizationResult> SynchronizeCulturalEventDataAsync(
            List<Guid> culturalEventIds,
            List<GeographicRegion> targetRegions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Coordinates diaspora community failover across regions
        /// </summary>
        Task<CommunityFailoverResult> CoordinateDiasporaCommunityFailoverAsync(
            List<Guid> communityIds,
            GeographicRegion targetRegion,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages multi-region cultural intelligence model synchronization
        /// </summary>
        Task<ModelSynchronizationResult> SynchronizeCulturalIntelligenceModelsAsync(
            List<Guid> modelIds,
            SynchronizationStrategy strategy,
            CancellationToken cancellationToken = default);

        #endregion

        #region Business Continuity Management

        /// <summary>
        /// Initiates comprehensive business continuity assessment
        /// </summary>
        Task<BusinessContinuityAssessment> InitiateBusinessContinuityAssessmentAsync(
            BusinessContinuityScope scope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Activates business continuity plan for cultural events
        /// </summary>
        Task<BusinessContinuityActivationResult> ActivateCulturalEventBusinessContinuityAsync(
            Guid culturalEventId,
            ContinuityActivationReason reason,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages critical business process continuity during disasters
        /// </summary>
        Task<ProcessContinuityResult> ManageCriticalProcessContinuityAsync(
            List<CriticalBusinessProcess> processes,
            ContinuityStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Coordinates stakeholder communication during business continuity events
        /// </summary>
        Task<StakeholderCommunicationResult> CoordinateStakeholderCommunicationAsync(
            BusinessContinuityEvent continuityEvent,
            List<StakeholderGroup> stakeholderGroups,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Maintains service level agreements during disaster recovery
        /// </summary>
        Task<ServiceLevelMaintenanceResult> MaintainServiceLevelAgreementsAsync(
            List<ServiceLevelAgreement> agreements,
            DisasterRecoveryContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages cultural intelligence service degradation gracefully
        /// </summary>
        Task<ServiceDegradationResult> ManageCulturalIntelligenceServiceDegradationAsync(
            ServiceDegradationLevel degradationLevel,
            GracefulDegradationStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Coordinates business continuity testing and validation
        /// </summary>
        Task<ContinuityTestResult> CoordinateBusinessContinuityTestingAsync(
            BusinessContinuityTestPlan testPlan,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages regulatory compliance during disaster recovery
        /// </summary>
        Task<ComplianceMaintenanceResult> MaintainRegulatoryComplianceDuringRecoveryAsync(
            List<RegulatoryRequirement> requirements,
            ComplianceMaintenanceStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Coordinates vendor and third-party service continuity
        /// </summary>
        Task<VendorContinuityResult> CoordinateVendorServiceContinuityAsync(
            List<VendorService> vendorServices,
            ContinuityCoordinationStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages cultural event revenue protection during disasters
        /// </summary>
        Task<RevenueProtectionResult> ManageCulturalEventRevenueProtectionAsync(
            List<Guid> culturalEventIds,
            RevenueProtectionStrategy strategy,
            CancellationToken cancellationToken = default);

        #endregion

        #region Data Integrity Validation and Verification

        /// <summary>
        /// Validates cultural intelligence data integrity across regions
        /// </summary>
        Task<DisasterRecoveryModels.DataIntegrityValidationResult> ValidateCulturalIntelligenceDataIntegrityAsync(
            CriticalTypes.DataIntegrityValidationScope scope,
            List<GeographicRegion> regions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs comprehensive backup data verification
        /// </summary>
        Task<DisasterRecoveryModels.BackupVerificationResult> PerformComprehensiveBackupVerificationAsync(
            Guid backupId,
            VerificationLevel verificationLevel,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates cultural event data consistency across backups
        /// </summary>
        Task<DisasterRecoveryModels.ConsistencyValidationResult> ValidateCulturalEventDataConsistencyAsync(
            List<Guid> culturalEventIds,
            CriticalTypes.ConsistencyCheckLevel checkLevel,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs real-time data integrity monitoring
        /// </summary>
        Task<DisasterRecoveryModels.IntegrityMonitoringResult> PerformRealTimeDataIntegrityMonitoringAsync(
            CriticalTypes.IntegrityMonitoringConfiguration configuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates diaspora community data integrity
        /// </summary>
        Task<DisasterRecoveryModels.CommunityDataIntegrityResult> ValidateDiasporaCommunityDataIntegrityAsync(
            List<Guid> communityIds,
            IntegrityValidationCriteria criteria,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs checksum validation for cultural intelligence models
        /// </summary>
        Task<CriticalModels.ChecksumValidationResult> PerformCulturalIntelligenceModelChecksumValidationAsync(
            List<Guid> modelIds,
            CriticalModels.ChecksumAlgorithm algorithm,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates cross-region data synchronization integrity
        /// </summary>
        Task<DisasterRecoveryModels.SynchronizationIntegrityResult> ValidateCrossRegionSynchronizationIntegrityAsync(
            GeographicRegion sourceRegion,
            GeographicRegion targetRegion,
            CriticalModels.IntegrityValidationDepth depth,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs automated data corruption detection
        /// </summary>
        Task<DisasterRecoveryModels.CorruptionDetectionResult> PerformAutomatedDataCorruptionDetectionAsync(
            CriticalModels.CorruptionDetectionScope scope,
            CriticalModels.DetectionSensitivity sensitivity,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates backup restore point integrity
        /// </summary>
        Task<DisasterRecoveryModels.RestorePointIntegrityResult> ValidateBackupRestorePointIntegrityAsync(
            Guid restorePointId,
            CriticalModels.IntegrityValidationMode validationMode,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs comprehensive data lineage validation
        /// </summary>
        Task<DataLineageValidationResult> PerformDataLineageValidationAsync(
            DataLineageScope scope,
            LineageValidationCriteria criteria,
            CancellationToken cancellationToken = default);

        #endregion

        #region Recovery Time Objective Management

        /// <summary>
        /// Manages recovery time objectives for cultural events
        /// </summary>
        Task<RecoveryTimeManagementResult> ManageCulturalEventRecoveryTimeObjectivesAsync(
            List<Guid> culturalEventIds,
            RecoveryTimeObjective rto,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes recovery time objectives based on business priority
        /// </summary>
        Task<RecoveryOptimizationResult> OptimizeRecoveryTimeObjectivesAsync(
            BusinessPriorityMatrix priorityMatrix,
            OptimizationStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Monitors actual recovery time against objectives
        /// </summary>
        Task<RecoveryTimeMonitoringResult> MonitorActualRecoveryTimeAsync(
            RecoveryOperation recoveryOperation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages tiered recovery time objectives for different service levels
        /// </summary>
        Task<TieredRecoveryManagementResult> ManageTieredRecoveryTimeObjectivesAsync(
            List<ServiceTier> serviceTiers,
            TieredRecoveryStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs recovery time objective testing and validation
        /// </summary>
        Task<RecoveryTimeTestResult> PerformRecoveryTimeObjectiveTestingAsync(
            RecoveryTimeTestConfiguration testConfiguration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adjusts recovery time objectives based on cultural event importance
        /// </summary>
        Task<RecoveryTimeAdjustmentResult> AdjustRecoveryTimeObjectivesForCulturalEventsAsync(
            CulturalEventImportanceMatrix importanceMatrix,
            AdjustmentStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages recovery point objectives in conjunction with time objectives
        /// </summary>
        Task<RecoveryPointManagementResult> ManageRecoveryPointObjectivesAsync(
            RecoveryPointObjective rpo,
            RecoveryTimeObjective rto,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Coordinates recovery time objectives across multiple regions
        /// </summary>
        Task<MultiRegionRecoveryCoordinationResult> CoordinateMultiRegionRecoveryTimeObjectivesAsync(
            List<GeographicRegion> regions,
            CrossRegionCoordinationStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages dynamic recovery time objective adjustment
        /// </summary>
        Task<DynamicRecoveryAdjustmentResult> ManageDynamicRecoveryTimeObjectiveAdjustmentAsync(
            DynamicAdjustmentTriggers triggers,
            AdjustmentParameters parameters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs recovery time objective compliance reporting
        /// </summary>
        Task<RecoveryComplianceReportResult> PerformRecoveryTimeObjectiveComplianceReportingAsync(
            ComplianceReportingScope scope,
            ReportingPeriod period,
            CancellationToken cancellationToken = default);

        #endregion

        #region Revenue Protection Integration

        /// <summary>
        /// Implements revenue protection strategies during disaster recovery
        /// </summary>
        Task<RevenueProtectionImplementationResult> ImplementRevenueProtectionStrategiesAsync(
            List<RevenueProtectionStrategy> strategies,
            DisasterRecoveryContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Monitors revenue impact during disaster scenarios
        /// </summary>
        Task<RevenueImpactMonitoringResult> MonitorRevenueImpactDuringDisasterAsync(
            RevenueImpactMonitoringConfiguration configuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages cultural event revenue continuity during outages
        /// </summary>
        Task<EventRevenueContinuityResult> ManageCulturalEventRevenueContinuityAsync(
            List<Guid> culturalEventIds,
            RevenueContinuityStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Implements alternative revenue channels during disasters
        /// </summary>
        Task<AlternativeRevenueChannelResult> ImplementAlternativeRevenueChannelsAsync(
            AlternativeChannelConfiguration configuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages subscription and billing continuity during recovery
        /// </summary>
        Task<BillingContinuityResult> ManageSubscriptionBillingContinuityAsync(
            BillingContinuityConfiguration configuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Coordinates revenue recovery after disaster resolution
        /// </summary>
        Task<RevenueRecoveryCoordinationResult> CoordinateRevenueRecoveryAsync(
            RevenueRecoveryPlan recoveryPlan,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages enterprise client revenue protection
        /// </summary>
        Task<EnterpriseRevenueProtectionResult> ManageEnterpriseClientRevenueProtectionAsync(
            List<EnterpriseClient> enterpriseClients,
            EnterpriseProtectionStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Implements revenue loss mitigation strategies
        /// </summary>
        Task<RevenueLossMitigationResult> ImplementRevenueLossMitigationStrategiesAsync(
            RevenueLossMitigationPlan mitigationPlan,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages cultural event ticket and registration revenue protection
        /// </summary>
        Task<TicketRevenueProtectionResult> ManageCulturalEventTicketRevenueProtectionAsync(
            List<Guid> culturalEventIds,
            TicketRevenueProtectionStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Coordinates insurance claim processes for revenue protection
        /// </summary>
        Task<InsuranceClaimCoordinationResult> CoordinateInsuranceClaimProcessesAsync(
            InsuranceClaimConfiguration claimConfiguration,
            CancellationToken cancellationToken = default);

        #endregion

        #region Monitoring and Auto-Scaling Integration

        /// <summary>
        /// Integrates disaster recovery with monitoring systems
        /// </summary>
        Task<MonitoringIntegrationResult> IntegrateDisasterRecoveryMonitoringAsync(
            MonitoringIntegrationConfiguration configuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages auto-scaling during disaster recovery scenarios
        /// </summary>
        Task<AutoScalingManagementResult> ManageAutoScalingDuringDisasterRecoveryAsync(
            AutoScalingConfiguration scalingConfiguration,
            DisasterRecoveryContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Monitors cultural intelligence system health during recovery
        /// </summary>
        Task<SystemHealthMonitoringResult> MonitorCulturalIntelligenceSystemHealthAsync(
            SystemHealthMonitoringConfiguration configuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Implements predictive scaling based on cultural event patterns
        /// </summary>
        Task<PredictiveScalingResult> ImplementCulturalEventPredictiveScalingAsync(
            CulturalEventPredictionModel predictionModel,
            PredictiveScalingStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages alert escalation during disaster recovery
        /// </summary>
        Task<AlertEscalationResult> ManageAlertEscalationDuringRecoveryAsync(
            AlertEscalationConfiguration escalationConfiguration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Coordinates capacity planning for disaster recovery scenarios
        /// </summary>
        Task<CapacityPlanningResult> CoordinateCapacityPlanningForDisasterRecoveryAsync(
            CapacityPlanningConfiguration planningConfiguration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Monitors and manages resource utilization during recovery
        /// </summary>
        Task<ResourceUtilizationResult> MonitorResourceUtilizationDuringRecoveryAsync(
            ResourceUtilizationMonitoringConfiguration configuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Implements automated recovery trigger mechanisms
        /// </summary>
        Task<AutomatedRecoveryTriggerResult> ImplementAutomatedRecoveryTriggersAsync(
            AutomatedRecoveryTriggerConfiguration triggerConfiguration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages performance monitoring during disaster recovery testing
        /// </summary>
        Task<PerformanceMonitoringResult> ManagePerformanceMonitoringDuringRecoveryTestingAsync(
            PerformanceMonitoringConfiguration configuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Coordinates load balancing during disaster recovery scenarios
        /// </summary>
        Task<LoadBalancingCoordinationResult> CoordinateLoadBalancingDuringRecoveryAsync(
            LoadBalancingConfiguration loadBalancingConfiguration,
            CancellationToken cancellationToken = default);

        #endregion

        #region Advanced Recovery Operations

        /// <summary>
        /// Performs granular data recovery for specific cultural events
        /// </summary>
        Task<GranularRecoveryResult> PerformGranularCulturalEventRecoveryAsync(
            Guid culturalEventId,
            GranularRecoveryConfiguration configuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages time-travel recovery for cultural intelligence data
        /// </summary>
        Task<TimeTravelRecoveryResult> ManageTimeTravelRecoveryAsync(
            DateTime targetPointInTime,
            TimeTravelRecoveryScope scope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Coordinates partial system recovery for critical operations
        /// </summary>
        Task<PartialSystemRecoveryResult> CoordinatePartialSystemRecoveryAsync(
            List<CriticalSystemComponent> components,
            PartialRecoveryStrategy strategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages hot-standby activation for cultural intelligence services
        /// </summary>
        Task<HotStandbyActivationResult> ManageHotStandbyActivationAsync(
            HotStandbyConfiguration standbyConfiguration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs differential backup recovery
        /// </summary>
        Task<DifferentialRecoveryResult> PerformDifferentialBackupRecoveryAsync(
            DifferentialRecoveryConfiguration configuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages cross-platform disaster recovery coordination
        /// </summary>
        Task<CrossPlatformRecoveryResult> ManageCrossPlatformDisasterRecoveryAsync(
            CrossPlatformRecoveryConfiguration configuration,
            CancellationToken cancellationToken = default);

        #endregion
    }
}
