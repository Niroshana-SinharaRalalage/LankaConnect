/**
 * Events API Type Definitions
 * DTOs matching backend API contracts (LankaConnect.Application.Events.Common)
 */

// ==================== Enums ====================

/**
 * Event status enum matching backend LankaConnect.Domain.Events.Enums.EventStatus
 */
export enum EventStatus {
  Draft = 0,
  Published = 1,
  Active = 2,
  Postponed = 3,
  Cancelled = 4,
  Completed = 5,
  Archived = 6,
  UnderReview = 7,
}

/**
 * Event category enum matching backend LankaConnect.Domain.Events.Enums.EventCategory
 */
export enum EventCategory {
  Religious = 0,
  Cultural = 1,
  Community = 2,
  Educational = 3,
  Social = 4,
  Business = 5,
  Charity = 6,
  Entertainment = 7,
}

/**
 * Registration status enum matching backend LankaConnect.Domain.Events.Enums.RegistrationStatus
 */
export enum RegistrationStatus {
  Pending = 0,
  Confirmed = 1,
  Waitlisted = 2,
  CheckedIn = 3,
  Completed = 4,
  Cancelled = 5,
  Refunded = 6,
}

/**
 * Currency enum matching backend LankaConnect.Domain.Shared.Enums.Currency
 */
export enum Currency {
  USD = 1,
  LKR = 2,
  GBP = 3,
  EUR = 4,
  CAD = 5,
  AUD = 6,
}

// ==================== Event DTOs ====================

/**
 * Event image DTO
 * Matches backend EventImageDto
 */
export interface EventImageDto {
  id: string;
  imageUrl: string;
  displayOrder: number;
  uploadedAt: string;
}

/**
 * Event video DTO
 * Matches backend EventVideoDto
 */
export interface EventVideoDto {
  id: string;
  videoUrl: string;
  thumbnailUrl: string;
  duration?: string | null; // ISO 8601 duration (e.g., PT1H30M)
  format: string;
  fileSizeBytes: number;
  displayOrder: number;
  uploadedAt: string;
}

/**
 * Main Event DTO
 * Matches backend EventDto from LankaConnect.Application.Events.Common
 */
export interface EventDto {
  id: string;
  title: string;
  description: string;
  startDate: string; // ISO 8601 date-time
  endDate: string; // ISO 8601 date-time
  organizerId: string;
  capacity: number;
  currentRegistrations: number;
  status: EventStatus;
  category: EventCategory;
  createdAt: string;
  updatedAt?: string | null;

  // Location information (nullable - not all events have physical locations)
  address?: string | null;
  city?: string | null;
  state?: string | null;
  zipCode?: string | null;
  country?: string | null;
  latitude?: number | null;
  longitude?: number | null;

  // Ticket pricing (nullable - free events)
  ticketPriceAmount?: number | null;
  ticketPriceCurrency?: Currency | null;
  isFree: boolean;

  // Media galleries (Epic 2 Phase 2)
  images: readonly EventImageDto[];
  videos: readonly EventVideoDto[];
}

/**
 * Event search result DTO with relevance score
 * Matches backend EventSearchResultDto
 */
export interface EventSearchResultDto extends EventDto {
  searchRank: number; // PostgreSQL FTS relevance score
}

/**
 * RSVP/Registration DTO
 */
export interface RsvpDto {
  id: string;
  eventId: string;
  userId: string;
  quantity: number;
  status: RegistrationStatus;
  createdAt: string;
  updatedAt?: string | null;

  // Denormalized event info (optional)
  eventTitle?: string | null;
  eventStartDate?: string | null;
  eventEndDate?: string | null;
  eventStatus?: EventStatus | null;
}

/**
 * Waiting list entry DTO
 */
export interface WaitingListEntryDto {
  id: string;
  eventId: string;
  userId: string;
  addedAt: string;
  position: number;
}

// ==================== Request DTOs ====================

/**
 * Get events query filters
 * Matches backend GetEventsQuery parameters
 */
export interface GetEventsRequest {
  status?: EventStatus;
  category?: EventCategory;
  startDateFrom?: string; // ISO 8601 date
  startDateTo?: string; // ISO 8601 date
  isFreeOnly?: boolean;
  city?: string;
}

/**
 * Search events request with pagination
 */
export interface SearchEventsRequest {
  searchTerm: string;
  page?: number;
  pageSize?: number;
  category?: EventCategory;
  isFreeOnly?: boolean;
  startDateFrom?: string;
}

/**
 * Get nearby events request (geospatial query)
 */
export interface GetNearbyEventsRequest {
  latitude: number;
  longitude: number;
  radiusKm: number;
  category?: EventCategory;
  isFreeOnly?: boolean;
  startDateFrom?: string;
}

/**
 * Create event request
 * Matches backend CreateEventCommand
 */
export interface CreateEventRequest {
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  organizerId: string;
  capacity: number;
  category?: EventCategory;

  // Location (optional)
  locationAddress?: string;
  locationCity?: string;
  locationState?: string;
  locationZipCode?: string;
  locationCountry?: string;
  locationLatitude?: number;
  locationLongitude?: number;

  // Ticket Price (optional)
  ticketPriceAmount?: number;
  ticketPriceCurrency?: Currency;
}

/**
 * Update event request
 */
export interface UpdateEventRequest {
  eventId: string;
  title?: string;
  description?: string;
  startDate?: string;
  endDate?: string;
  capacity?: number;
  category?: EventCategory;

  // Location
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  latitude?: number;
  longitude?: number;

  // Pricing
  ticketPriceAmount?: number;
  ticketPriceCurrency?: Currency;
  isFree?: boolean;
}

/**
 * RSVP to event request
 * Matches backend RsvpToEventCommand
 */
export interface RsvpRequest {
  eventId: string;
  userId: string;
  quantity?: number; // Default: 1
}

/**
 * Update RSVP request
 */
export interface UpdateRsvpRequest {
  userId: string;
  newQuantity: number;
}

/**
 * Cancel event request
 */
export interface CancelEventRequest {
  reason: string;
}

/**
 * Postpone event request
 */
export interface PostponeEventRequest {
  reason: string;
}

// ==================== Response DTOs ====================

/**
 * Create event response
 * Returns the newly created event ID
 */
export interface CreateEventResponse {
  id: string;
}

/**
 * Upload event image response
 */
export interface UploadEventImageResponse {
  id: string;
  imageUrl: string;
  displayOrder: number;
  uploadedAt: string;
}
