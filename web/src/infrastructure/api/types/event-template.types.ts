/**
 * Phase 6A.8: Event Template System
 * TypeScript types for event templates
 */

import { EventCategory } from './events.types';

/**
 * Event template DTO from backend
 */
export interface EventTemplateDto {
  id: string;
  name: string;
  description: string;
  category: EventCategory;
  thumbnailSvg: string;
  templateDataJson: string;
  isActive: boolean;
  displayOrder: number;
  createdAt: string;
  updatedAt?: string;
}

/**
 * Parsed template data structure
 * This is the JSON structure stored in templateDataJson
 */
export interface TemplateData {
  title: string;
  description: string;
  capacity: number;
  durationHours: number;
  suggestedStartTime: string;
  ticketPrice: number;
}

/**
 * Query parameters for fetching templates
 */
export interface GetEventTemplatesParams {
  category?: EventCategory;
  isActive?: boolean;
}
