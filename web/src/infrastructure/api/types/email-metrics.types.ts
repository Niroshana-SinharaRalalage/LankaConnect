/**
 * Phase 6A.87: Email Metrics Dashboard Types
 * Type definitions for email metrics API responses
 */

/**
 * Summary response from /api/admin/email-metrics/summary
 */
export interface EmailMetricsSummaryDto {
  totalEmailsSent: number;
  totalSuccesses: number;
  totalFailures: number;
  globalSuccessRate: number;
  totalTemplatesUsed: number;
  totalHandlersRecorded: number;
  handlersUsingTypedParameters: number;
  migrationProgressPercentage: number;
  timestamp: string;
}

/**
 * Template stats from /api/admin/email-metrics/by-template
 */
export interface TemplateStatsDto {
  templateName: string;
  totalSent: number;
  successCount: number;
  failureCount: number;
  validationFailures: number;
  templateNotFoundCount: number;
  successRate: number;
  averageDurationMs: number;
}

/**
 * Response from /api/admin/email-metrics/by-template
 */
export interface TemplateStatsListDto {
  templates: TemplateStatsDto[];
  totalTemplates: number;
  timestamp: string;
}

/**
 * Email failure record
 */
export interface EmailFailureDto {
  timestamp: string;
  templateName: string;
  recipientEmail: string; // Masked for privacy
  errorMessage: string;
  handlerName?: string;
}

/**
 * Response from /api/admin/email-metrics/failures
 */
export interface EmailFailuresDto {
  failures: EmailFailureDto[];
  totalCount: number;
  timestamp: string;
}

/**
 * Validation failure record
 */
export interface ValidationFailureDto {
  timestamp: string;
  templateName: string;
  handlerName: string;
  errors: string[];
  parameterName?: string;
}

/**
 * Response from /api/admin/email-metrics/validation-failures
 */
export interface ValidationFailuresDto {
  failures: ValidationFailureDto[];
  totalCount: number;
  timestamp: string;
}

/**
 * Handler migration status
 */
export interface HandlerMigrationStatusDto {
  handlerName: string;
  usesTypedParameters: boolean;
  totalEmails: number;
  lastUsed?: string;
}

/**
 * Response from /api/admin/email-metrics/migration-progress
 */
export interface MigrationProgressDto {
  handlers: HandlerMigrationStatusDto[];
  totalHandlers: number;
  migratedHandlers: number;
  migrationPercentage: number;
  timestamp: string;
}

/**
 * Response from /api/admin/email-metrics/reset
 */
export interface ResetMetricsResponseDto {
  message: string;
  timestamp: string;
}
