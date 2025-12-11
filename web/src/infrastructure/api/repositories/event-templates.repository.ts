import { apiClient } from '../client/api-client';
import type { EventTemplateDto, GetEventTemplatesParams } from '../types/event-template.types';

/**
 * Phase 6A.8: Event Template System - API Repository
 * Repository for event template API calls
 */
export class EventTemplatesRepository {
  private readonly basePath = '/eventtemplates';

  /**
   * Get all event templates with optional filtering
   */
  async getEventTemplates(params?: GetEventTemplatesParams): Promise<EventTemplateDto[]> {
    return await apiClient.get<EventTemplateDto[]>(this.basePath, { params });
  }

  /**
   * Get event template by ID
   */
  async getEventTemplateById(id: string): Promise<EventTemplateDto> {
    return await apiClient.get<EventTemplateDto>(`${this.basePath}/${id}`);
  }
}

// Export singleton instance
export const eventTemplatesRepository = new EventTemplatesRepository();
