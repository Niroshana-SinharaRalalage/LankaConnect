/**
 * Phase 6A.87: Email Metrics Dashboard Repository
 * Repository for email metrics API endpoints
 */

import { apiClient } from '../client/api-client';
import type {
  EmailMetricsSummaryDto,
  TemplateStatsListDto,
  TemplateStatsDto,
  EmailFailuresDto,
  ValidationFailuresDto,
  MigrationProgressDto,
  ResetMetricsResponseDto,
  GetFailuresParams, // Phase 6A.99
} from '../types/email-metrics.types';

export class EmailMetricsRepository {
  private readonly basePath = '/admin/email-metrics';

  /**
   * Get email metrics summary
   */
  async getSummary(): Promise<EmailMetricsSummaryDto> {
    const response = await apiClient.get<EmailMetricsSummaryDto>(`${this.basePath}/summary`);
    return response;
  }

  /**
   * Get stats for all templates
   */
  async getTemplateStats(): Promise<TemplateStatsListDto> {
    const response = await apiClient.get<TemplateStatsListDto>(`${this.basePath}/by-template`);
    return response;
  }

  /**
   * Get stats for a specific template
   */
  async getTemplateStatsByName(templateName: string): Promise<TemplateStatsDto> {
    const response = await apiClient.get<TemplateStatsDto>(
      `${this.basePath}/by-template/${encodeURIComponent(templateName)}`
    );
    return response;
  }

  /**
   * Get recent email failures
   * Phase 6A.99: Added optional params for limit and unmasked (SuperAdmin only)
   */
  async getFailures(params?: GetFailuresParams): Promise<EmailFailuresDto> {
    const searchParams = new URLSearchParams();
    if (params?.limit) searchParams.set('limit', params.limit.toString());
    if (params?.unmasked) searchParams.set('unmasked', 'true');

    const queryString = searchParams.toString();
    const url = queryString
      ? `${this.basePath}/failures?${queryString}`
      : `${this.basePath}/failures`;

    const response = await apiClient.get<EmailFailuresDto>(url);
    return response;
  }

  /**
   * Get validation failures
   */
  async getValidationFailures(): Promise<ValidationFailuresDto> {
    const response = await apiClient.get<ValidationFailuresDto>(`${this.basePath}/validation-failures`);
    return response;
  }

  /**
   * Get migration progress
   */
  async getMigrationProgress(): Promise<MigrationProgressDto> {
    const response = await apiClient.get<MigrationProgressDto>(`${this.basePath}/migration-progress`);
    return response;
  }

  /**
   * Reset metrics (Dev/Staging only)
   */
  async resetMetrics(): Promise<ResetMetricsResponseDto> {
    const response = await apiClient.post<ResetMetricsResponseDto>(`${this.basePath}/reset`, {});
    return response;
  }
}

// Export singleton instance
export const emailMetricsRepository = new EmailMetricsRepository();
