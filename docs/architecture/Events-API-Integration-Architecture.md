# Events API Integration Architecture

**Document Version:** 1.0
**Date:** 2025-11-09
**Author:** System Architect
**Status:** Design Proposal

## Executive Summary

This document outlines the architectural design for integrating the Events API from the .NET backend into the Next.js frontend, following Clean Architecture and DDD principles established in the LankaConnect project.

## Current Pattern Analysis

### Existing Infrastructure Patterns

**API Client Layer** (`web/src/infrastructure/api/client/`)
- Singleton `ApiClient` class using Axios
- Centralized error handling with custom error types
- JWT token management via interceptors
- Multipart form-data support for file uploads

**Repository Pattern** (`web/src/infrastructure/api/repositories/`)
- `AuthRepository`: CRUD operations for authentication
- `ProfileRepository`: User profile management
- Singleton instances exported for dependency injection

**Service Pattern** (`web/src/infrastructure/api/services/`)
- `newsletterService`: Simple object with methods (not class-based)
- Direct fetch API usage (inconsistent with ApiClient pattern)

**State Management**
- Zustand stores (`useAuthStore`, `useProfileStore`)
- Direct repository calls from stores
- No React Query hooks currently implemented

### Pattern Evaluation

| Pattern | Used For | Pros | Cons |
|---------|----------|------|------|
| Repository | Auth, Profile | Type-safe, testable, follows DDD | Requires boilerplate |
| Service | Newsletter | Lightweight, quick setup | Inconsistent with ApiClient |
| Zustand Store | Auth, Profile | Global state, dirty tracking | Not cacheable, manual refetch |
| React Query | - | Cache, refetch, optimistic updates | Not yet implemented |

## Recommended Architecture for Events API

### Decision: Hybrid Repository + React Query Hooks

**Rationale:**
1. **Repository Layer** - Maintains consistency with existing `AuthRepository` and `ProfileRepository`
2. **React Query Hooks** - Adds caching, automatic refetching, and optimistic updates
3. **Type Safety** - TypeScript DTOs matching backend contracts
4. **Clean Architecture** - Infrastructure concerns separated from domain logic

### Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                     Presentation Layer                       │
│  ┌────────────────────────────────────────────────────────┐ │
│  │   React Components (EventCard, EventDetails, etc.)    │ │
│  │   Uses: useEvents(), useEventById(), useCreateEvent() │ │
│  └────────────────────────────────────────────────────────┘ │
└───────────────────────────┬─────────────────────────────────┘
                            │ imports hooks
┌───────────────────────────▼─────────────────────────────────┐
│              Application/Presentation Hooks                  │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ web/src/presentation/hooks/queries/                    │ │
│  │ - useEvents.ts       (GET /events with filters)        │ │
│  │ - useEventById.ts    (GET /events/:id)                 │ │
│  │ - useNearbyEvents.ts (GET /events/nearby)              │ │
│  │ - useSearchEvents.ts (GET /events/search)              │ │
│  │                                                          │ │
│  │ web/src/presentation/hooks/mutations/                  │ │
│  │ - useCreateEvent.ts  (POST /events)                    │ │
│  │ - useUpdateEvent.ts  (PUT /events/:id)                 │ │
│  │ - useRsvpToEvent.ts  (POST /events/:id/rsvp)           │ │
│  │ - useCancelRsvp.ts   (DELETE /events/:id/rsvp)         │ │
│  └────────────────────────────────────────────────────────┘ │
└───────────────────────────┬─────────────────────────────────┘
                            │ uses repository
┌───────────────────────────▼─────────────────────────────────┐
│                    Infrastructure Layer                      │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ web/src/infrastructure/api/repositories/               │ │
│  │ - events.repository.ts (EventsRepository class)        │ │
│  │                                                          │ │
│  │   Methods:                                              │ │
│  │   - getEvents(filters): Promise<EventDto[]>            │ │
│  │   - getEventById(id): Promise<EventDto>                │ │
│  │   - searchEvents(params): Promise<PagedResult<...>>    │ │
│  │   - getNearbyEvents(lat, lon, radius): Promise<...>    │ │
│  │   - createEvent(data): Promise<Guid>                   │ │
│  │   - updateEvent(id, data): Promise<void>               │ │
│  │   - deleteEvent(id): Promise<void>                     │ │
│  │   - rsvpToEvent(id, userId, qty): Promise<void>        │ │
│  │   - cancelRsvp(id, userId): Promise<void>              │ │
│  │   - uploadEventImage(id, file): Promise<EventImage>    │ │
│  └────────────────────────────────────────────────────────┘ │
└───────────────────────────┬─────────────────────────────────┘
                            │ uses ApiClient
┌───────────────────────────▼─────────────────────────────────┐
│                        HTTP Client                           │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ web/src/infrastructure/api/client/api-client.ts        │ │
│  │ - Singleton ApiClient with Axios                       │ │
│  │ - Interceptors for auth & error handling               │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## File Structure

```
web/src/
├── infrastructure/
│   └── api/
│       ├── client/
│       │   ├── api-client.ts               # Existing - no changes
│       │   └── api-errors.ts               # Existing - no changes
│       ├── repositories/
│       │   ├── auth.repository.ts          # Existing
│       │   ├── profile.repository.ts       # Existing
│       │   └── events.repository.ts        # NEW - Events API calls
│       ├── types/
│       │   ├── auth.types.ts               # Existing
│       │   ├── events.types.ts             # NEW - Event DTOs
│       │   └── common.types.ts             # NEW - Shared types (PagedResult, etc.)
│       └── services/
│           └── newsletter.service.ts       # Existing - consider migrating to repository
│
├── presentation/
│   ├── hooks/
│   │   ├── queries/
│   │   │   ├── events/
│   │   │   │   ├── useEvents.ts            # NEW - List events with filters
│   │   │   │   ├── useEventById.ts         # NEW - Get single event
│   │   │   │   ├── useSearchEvents.ts      # NEW - Full-text search
│   │   │   │   ├── useNearbyEvents.ts      # NEW - Geospatial query
│   │   │   │   ├── useUserRsvps.ts         # NEW - User's RSVPs
│   │   │   │   └── useUpcomingEvents.ts    # NEW - Upcoming events
│   │   │   └── index.ts                    # Re-export all query hooks
│   │   │
│   │   └── mutations/
│   │       ├── events/
│   │       │   ├── useCreateEvent.ts       # NEW - Create event
│   │       │   ├── useUpdateEvent.ts       # NEW - Update event
│   │       │   ├── useDeleteEvent.ts       # NEW - Delete event
│   │       │   ├── useRsvpToEvent.ts       # NEW - RSVP to event
│   │       │   ├── useCancelRsvp.ts        # NEW - Cancel RSVP
│   │       │   ├── useUpdateRsvp.ts        # NEW - Update RSVP quantity
│   │       │   ├── useUploadEventImage.ts  # NEW - Upload image
│   │       │   └── usePublishEvent.ts      # NEW - Publish event
│   │       └── index.ts                    # Re-export all mutation hooks
│   │
│   └── store/
│       ├── useAuthStore.ts                 # Existing
│       └── useProfileStore.ts              # Existing
│
└── domain/
    ├── models/
    │   ├── UserProfile.ts                  # Existing
    │   ├── FeedItem.ts                     # Existing
    │   └── Event.ts                        # NEW - Domain model (if needed)
    └── constants/
        └── events.constants.ts             # NEW - Event categories, statuses

```

## Type Definitions

### Location: `web/src/infrastructure/api/types/events.types.ts`

```typescript
/**
 * Events API Type Definitions
 * DTOs matching backend API contracts (LankaConnect.Application.Events.Common)
 */

import { EventStatus, EventCategory, RegistrationStatus, Currency } from '@/domain/constants/events.constants';

// ==================== Event DTOs ====================

export interface EventDto {
  id: string;
  title: string;
  description: string;
  startDate: string; // ISO 8601 date-time
  endDate: string;
  organizerId: string;
  capacity: number;
  currentRegistrations: number;
  status: EventStatus;
  category: EventCategory;
  createdAt: string;
  updatedAt?: string | null;

  // Location (nullable for virtual events)
  address?: string | null;
  city?: string | null;
  state?: string | null;
  zipCode?: string | null;
  country?: string | null;
  latitude?: number | null;
  longitude?: number | null;

  // Pricing (nullable for free events)
  ticketPriceAmount?: number | null;
  ticketPriceCurrency?: Currency | null;
  isFree: boolean;

  // Media galleries
  images: readonly EventImageDto[];
  videos: readonly EventVideoDto[];
}

export interface EventImageDto {
  id: string;
  imageUrl: string;
  displayOrder: number;
  uploadedAt: string;
}

export interface EventVideoDto {
  id: string;
  videoUrl: string;
  thumbnailUrl: string;
  duration?: string | null; // ISO 8601 duration (PT1H30M)
  format: string;
  fileSizeBytes: number;
  displayOrder: number;
  uploadedAt: string;
}

export interface RsvpDto {
  id: string;
  eventId: string;
  userId: string;
  quantity: number;
  status: RegistrationStatus;
  createdAt: string;
  updatedAt?: string | null;

  // Denormalized event info
  eventTitle?: string | null;
  eventStartDate?: string | null;
  eventEndDate?: string | null;
  eventStatus?: EventStatus | null;
}

export interface EventSearchResultDto extends EventDto {
  searchRank: number; // PostgreSQL FTS relevance score
}

export interface WaitingListEntryDto {
  id: string;
  eventId: string;
  userId: string;
  addedAt: string;
  position: number;
}

// ==================== Request DTOs ====================

export interface GetEventsRequest {
  status?: EventStatus;
  category?: EventCategory;
  startDateFrom?: string; // ISO 8601 date
  startDateTo?: string;
  isFreeOnly?: boolean;
  city?: string;
}

export interface SearchEventsRequest {
  searchTerm: string;
  page?: number;
  pageSize?: number;
  category?: EventCategory;
  isFreeOnly?: boolean;
  startDateFrom?: string;
}

export interface GetNearbyEventsRequest {
  latitude: number;
  longitude: number;
  radiusKm: number;
  category?: EventCategory;
  isFreeOnly?: boolean;
  startDateFrom?: string;
}

export interface CreateEventRequest {
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  organizerId: string;
  capacity: number;
  category: EventCategory;

  // Location (optional)
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  latitude?: number;
  longitude?: number;

  // Pricing (optional)
  ticketPriceAmount?: number;
  ticketPriceCurrency?: Currency;
  isFree?: boolean;
}

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

export interface RsvpRequest {
  userId: string;
  quantity?: number; // Default: 1
}

export interface UpdateRsvpRequest {
  userId: string;
  newQuantity: number;
}

export interface CancelEventRequest {
  reason: string;
}

export interface PostponeEventRequest {
  reason: string;
}

// ==================== Response DTOs ====================

export interface CreateEventResponse {
  id: string;
}

export interface UploadEventImageResponse {
  id: string;
  imageUrl: string;
  displayOrder: number;
  uploadedAt: string;
}
```

### Location: `web/src/infrastructure/api/types/common.types.ts`

```typescript
/**
 * Common API Types
 * Shared across multiple API domains
 */

export interface PagedResult<T> {
  items: readonly T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
}

export interface ApiErrorResponse {
  error: string;
  errors?: Record<string, string[]>; // Validation errors
  statusCode?: number;
}
```

### Location: `web/src/domain/constants/events.constants.ts`

```typescript
/**
 * Event Domain Constants
 * Enums matching backend domain (LankaConnect.Domain.Events.Enums)
 */

export enum EventStatus {
  Draft = 'Draft',
  PendingApproval = 'PendingApproval',
  Approved = 'Approved',
  Published = 'Published',
  Cancelled = 'Cancelled',
  Postponed = 'Postponed',
  Completed = 'Completed',
  Rejected = 'Rejected',
}

export enum EventCategory {
  Cultural = 'Cultural',
  Religious = 'Religious',
  Social = 'Social',
  Educational = 'Educational',
  Sports = 'Sports',
  Arts = 'Arts',
  Business = 'Business',
  Food = 'Food',
  Music = 'Music',
  Other = 'Other',
}

export enum RegistrationStatus {
  Pending = 'Pending',
  Confirmed = 'Confirmed',
  Cancelled = 'Cancelled',
  Waitlisted = 'Waitlisted',
  Attended = 'Attended',
  NoShow = 'NoShow',
}

export enum Currency {
  USD = 'USD',
  EUR = 'EUR',
  GBP = 'GBP',
  LKR = 'LKR',
}

export const EVENT_STATUS_LABELS: Record<EventStatus, string> = {
  [EventStatus.Draft]: 'Draft',
  [EventStatus.PendingApproval]: 'Pending Approval',
  [EventStatus.Approved]: 'Approved',
  [EventStatus.Published]: 'Published',
  [EventStatus.Cancelled]: 'Cancelled',
  [EventStatus.Postponed]: 'Postponed',
  [EventStatus.Completed]: 'Completed',
  [EventStatus.Rejected]: 'Rejected',
};

export const EVENT_CATEGORY_LABELS: Record<EventCategory, string> = {
  [EventCategory.Cultural]: 'Cultural',
  [EventCategory.Religious]: 'Religious',
  [EventCategory.Social]: 'Social',
  [EventCategory.Educational]: 'Educational',
  [EventCategory.Sports]: 'Sports',
  [EventCategory.Arts]: 'Arts',
  [EventCategory.Business]: 'Business',
  [EventCategory.Food]: 'Food',
  [EventCategory.Music]: 'Music',
  [EventCategory.Other]: 'Other',
};
```

## Repository Implementation

### Location: `web/src/infrastructure/api/repositories/events.repository.ts`

```typescript
import { apiClient } from '../client/api-client';
import type {
  EventDto,
  RsvpDto,
  EventSearchResultDto,
  WaitingListEntryDto,
  GetEventsRequest,
  SearchEventsRequest,
  GetNearbyEventsRequest,
  CreateEventRequest,
  UpdateEventRequest,
  RsvpRequest,
  UpdateRsvpRequest,
  CancelEventRequest,
  PostponeEventRequest,
  CreateEventResponse,
  UploadEventImageResponse,
  EventImageDto,
} from '../types/events.types';
import type { PagedResult } from '../types/common.types';

/**
 * EventsRepository
 * Handles all event-related API calls
 * Repository pattern for event operations
 *
 * Endpoints from EventsController.cs:
 * - GET /api/events - Get all events with filters
 * - GET /api/events/{id} - Get event by ID
 * - GET /api/events/search - Full-text search
 * - GET /api/events/nearby - Geospatial search
 * - POST /api/events - Create event
 * - PUT /api/events/{id} - Update event
 * - DELETE /api/events/{id} - Delete event
 * - POST /api/events/{id}/rsvp - RSVP to event
 * - DELETE /api/events/{id}/rsvp - Cancel RSVP
 * - PUT /api/events/{id}/rsvp - Update RSVP
 * - GET /api/events/my-rsvps - Get user's RSVPs
 * - POST /api/events/{id}/images - Upload image
 */
export class EventsRepository {
  private readonly basePath = '/events';

  // ==================== PUBLIC QUERIES ====================

  /**
   * Get all events with optional filtering
   */
  async getEvents(filters: GetEventsRequest = {}): Promise<EventDto[]> {
    const params = new URLSearchParams();
    if (filters.status) params.append('status', filters.status);
    if (filters.category) params.append('category', filters.category);
    if (filters.startDateFrom) params.append('startDateFrom', filters.startDateFrom);
    if (filters.startDateTo) params.append('startDateTo', filters.startDateTo);
    if (filters.isFreeOnly !== undefined) params.append('isFreeOnly', String(filters.isFreeOnly));
    if (filters.city) params.append('city', filters.city);

    const queryString = params.toString();
    const url = queryString ? `${this.basePath}?${queryString}` : this.basePath;

    return await apiClient.get<EventDto[]>(url);
  }

  /**
   * Search events using full-text search (PostgreSQL FTS)
   */
  async searchEvents(request: SearchEventsRequest): Promise<PagedResult<EventSearchResultDto>> {
    const params = new URLSearchParams({
      searchTerm: request.searchTerm,
      page: String(request.page ?? 1),
      pageSize: String(request.pageSize ?? 20),
    });

    if (request.category) params.append('category', request.category);
    if (request.isFreeOnly !== undefined) params.append('isFreeOnly', String(request.isFreeOnly));
    if (request.startDateFrom) params.append('startDateFrom', request.startDateFrom);

    return await apiClient.get<PagedResult<EventSearchResultDto>>(
      `${this.basePath}/search?${params.toString()}`
    );
  }

  /**
   * Get event by ID
   */
  async getEventById(id: string): Promise<EventDto> {
    return await apiClient.get<EventDto>(`${this.basePath}/${id}`);
  }

  /**
   * Get nearby events using geospatial query
   */
  async getNearbyEvents(request: GetNearbyEventsRequest): Promise<EventDto[]> {
    const params = new URLSearchParams({
      latitude: String(request.latitude),
      longitude: String(request.longitude),
      radiusKm: String(request.radiusKm),
    });

    if (request.category) params.append('category', request.category);
    if (request.isFreeOnly !== undefined) params.append('isFreeOnly', String(request.isFreeOnly));
    if (request.startDateFrom) params.append('startDateFrom', request.startDateFrom);

    return await apiClient.get<EventDto[]>(`${this.basePath}/nearby?${params.toString()}`);
  }

  // ==================== AUTHENTICATED MUTATIONS ====================

  /**
   * Create a new event (requires authentication)
   */
  async createEvent(data: CreateEventRequest): Promise<string> {
    const response = await apiClient.post<CreateEventResponse>(this.basePath, data);
    return response.id;
  }

  /**
   * Update an existing event (owner only)
   */
  async updateEvent(id: string, data: UpdateEventRequest): Promise<void> {
    await apiClient.put<void>(`${this.basePath}/${id}`, { ...data, eventId: id });
  }

  /**
   * Delete an event (owner only, draft/cancelled only)
   */
  async deleteEvent(id: string): Promise<void> {
    await apiClient.delete<void>(`${this.basePath}/${id}`);
  }

  /**
   * Submit event for approval
   */
  async submitForApproval(id: string): Promise<void> {
    await apiClient.post<void>(`${this.basePath}/${id}/submit`);
  }

  /**
   * Publish event (owner only)
   */
  async publishEvent(id: string): Promise<void> {
    await apiClient.post<void>(`${this.basePath}/${id}/publish`);
  }

  /**
   * Cancel event with reason
   */
  async cancelEvent(id: string, reason: string): Promise<void> {
    const request: CancelEventRequest = { reason };
    await apiClient.post<void>(`${this.basePath}/${id}/cancel`, request);
  }

  /**
   * Postpone event with reason
   */
  async postponeEvent(id: string, reason: string): Promise<void> {
    const request: PostponeEventRequest = { reason };
    await apiClient.post<void>(`${this.basePath}/${id}/postpone`, request);
  }

  // ==================== RSVP OPERATIONS ====================

  /**
   * RSVP to an event
   */
  async rsvpToEvent(eventId: string, userId: string, quantity: number = 1): Promise<void> {
    const request: RsvpRequest = { userId, quantity };
    await apiClient.post<void>(`${this.basePath}/${eventId}/rsvp`, request);
  }

  /**
   * Cancel RSVP
   */
  async cancelRsvp(eventId: string): Promise<void> {
    await apiClient.delete<void>(`${this.basePath}/${eventId}/rsvp`);
  }

  /**
   * Update RSVP quantity
   */
  async updateRsvp(eventId: string, userId: string, newQuantity: number): Promise<void> {
    const request: UpdateRsvpRequest = { userId, newQuantity };
    await apiClient.put<void>(`${this.basePath}/${eventId}/rsvp`, request);
  }

  /**
   * Get user's RSVPs
   */
  async getUserRsvps(): Promise<RsvpDto[]> {
    return await apiClient.get<RsvpDto[]>(`${this.basePath}/my-rsvps`);
  }

  /**
   * Get upcoming events for user
   */
  async getUpcomingEvents(): Promise<EventDto[]> {
    return await apiClient.get<EventDto[]>(`${this.basePath}/upcoming`);
  }

  // ==================== WAITING LIST ====================

  /**
   * Add user to waiting list
   */
  async addToWaitingList(eventId: string): Promise<void> {
    await apiClient.post<void>(`${this.basePath}/${eventId}/waiting-list`);
  }

  /**
   * Remove user from waiting list
   */
  async removeFromWaitingList(eventId: string): Promise<void> {
    await apiClient.delete<void>(`${this.basePath}/${eventId}/waiting-list`);
  }

  /**
   * Get waiting list for event
   */
  async getWaitingList(eventId: string): Promise<WaitingListEntryDto[]> {
    return await apiClient.get<WaitingListEntryDto[]>(`${this.basePath}/${eventId}/waiting-list`);
  }

  // ==================== MEDIA OPERATIONS ====================

  /**
   * Upload image to event gallery
   */
  async uploadEventImage(eventId: string, file: File): Promise<EventImageDto> {
    const formData = new FormData();
    formData.append('image', file);

    return await apiClient.postMultipart<EventImageDto>(
      `${this.basePath}/${eventId}/images`,
      formData
    );
  }

  /**
   * Delete image from event gallery
   */
  async deleteEventImage(eventId: string, imageId: string): Promise<void> {
    await apiClient.delete<void>(`${this.basePath}/${eventId}/images/${imageId}`);
  }

  /**
   * Export event as ICS calendar file
   */
  async getEventIcs(eventId: string): Promise<Blob> {
    // Note: This returns a file, handle differently
    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/events/${eventId}/ics`);
    return await response.blob();
  }

  /**
   * Record social share for analytics
   */
  async recordEventShare(eventId: string, platform?: string): Promise<void> {
    await apiClient.post<void>(`${this.basePath}/${eventId}/share`, { platform });
  }
}

/**
 * Singleton instance of the events repository
 */
export const eventsRepository = new EventsRepository();
```

## React Query Hooks Implementation

### Query Hook Example: `web/src/presentation/hooks/queries/events/useEvents.ts`

```typescript
import { useQuery, UseQueryOptions } from '@tanstack/react-query';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type { EventDto, GetEventsRequest } from '@/infrastructure/api/types/events.types';
import type { ApiError } from '@/infrastructure/api/client/api-errors';

export const EVENTS_QUERY_KEY = 'events';

/**
 * Hook to fetch events with optional filters
 * Includes React Query caching, refetching, and error handling
 *
 * @example
 * const { data: events, isLoading, error } = useEvents({ category: 'Cultural', isFreeOnly: true });
 */
export function useEvents(
  filters: GetEventsRequest = {},
  options?: Omit<UseQueryOptions<EventDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery<EventDto[], ApiError>({
    queryKey: [EVENTS_QUERY_KEY, filters],
    queryFn: () => eventsRepository.getEvents(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes - events don't change frequently
    ...options,
  });
}
```

### Query Hook Example: `web/src/presentation/hooks/queries/events/useEventById.ts`

```typescript
import { useQuery, UseQueryOptions } from '@tanstack/react-query';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type { EventDto } from '@/infrastructure/api/types/events.types';
import type { ApiError } from '@/infrastructure/api/client/api-errors';

export const EVENT_BY_ID_QUERY_KEY = 'event';

/**
 * Hook to fetch a single event by ID
 *
 * @example
 * const { data: event, isLoading, error } = useEventById('event-uuid-123');
 */
export function useEventById(
  eventId: string,
  options?: Omit<UseQueryOptions<EventDto, ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery<EventDto, ApiError>({
    queryKey: [EVENT_BY_ID_QUERY_KEY, eventId],
    queryFn: () => eventsRepository.getEventById(eventId),
    enabled: !!eventId, // Only run if eventId is provided
    staleTime: 5 * 60 * 1000, // 5 minutes
    ...options,
  });
}
```

### Mutation Hook Example: `web/src/presentation/hooks/mutations/events/useCreateEvent.ts`

```typescript
import { useMutation, useQueryClient, UseMutationOptions } from '@tanstack/react-query';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type { CreateEventRequest } from '@/infrastructure/api/types/events.types';
import type { ApiError } from '@/infrastructure/api/client/api-errors';
import { EVENTS_QUERY_KEY } from '../../queries/events/useEvents';

/**
 * Hook to create a new event
 * Automatically invalidates events list on success
 *
 * @example
 * const { mutate: createEvent, isPending } = useCreateEvent();
 * createEvent(eventData, {
 *   onSuccess: (eventId) => router.push(`/events/${eventId}`),
 *   onError: (error) => toast.error(error.message),
 * });
 */
export function useCreateEvent(
  options?: Omit<UseMutationOptions<string, ApiError, CreateEventRequest>, 'mutationFn'>
) {
  const queryClient = useQueryClient();

  return useMutation<string, ApiError, CreateEventRequest>({
    mutationFn: (data: CreateEventRequest) => eventsRepository.createEvent(data),
    onSuccess: (eventId, variables, context) => {
      // Invalidate events list to refetch with new event
      queryClient.invalidateQueries({ queryKey: [EVENTS_QUERY_KEY] });

      // Call user-provided onSuccess
      options?.onSuccess?.(eventId, variables, context);
    },
    ...options,
  });
}
```

### Mutation Hook Example: `web/src/presentation/hooks/mutations/events/useRsvpToEvent.ts`

```typescript
import { useMutation, useQueryClient, UseMutationOptions } from '@tanstack/react-query';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type { ApiError } from '@/infrastructure/api/client/api-errors';
import { EVENT_BY_ID_QUERY_KEY } from '../../queries/events/useEventById';
import { EVENTS_QUERY_KEY } from '../../queries/events/useEvents';

interface RsvpMutationVariables {
  eventId: string;
  userId: string;
  quantity?: number;
}

/**
 * Hook to RSVP to an event
 * Implements optimistic updates for instant UI feedback
 *
 * @example
 * const { mutate: rsvp, isPending } = useRsvpToEvent();
 * rsvp({ eventId: 'uuid', userId: 'user-uuid', quantity: 2 });
 */
export function useRsvpToEvent(
  options?: Omit<UseMutationOptions<void, ApiError, RsvpMutationVariables>, 'mutationFn'>
) {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, RsvpMutationVariables>({
    mutationFn: ({ eventId, userId, quantity }) =>
      eventsRepository.rsvpToEvent(eventId, userId, quantity),

    // Optimistic update - update UI before server confirms
    onMutate: async ({ eventId, quantity = 1 }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: [EVENT_BY_ID_QUERY_KEY, eventId] });

      // Snapshot previous value
      const previousEvent = queryClient.getQueryData([EVENT_BY_ID_QUERY_KEY, eventId]);

      // Optimistically update event's registration count
      queryClient.setQueryData([EVENT_BY_ID_QUERY_KEY, eventId], (old: any) => ({
        ...old,
        currentRegistrations: old.currentRegistrations + quantity,
      }));

      return { previousEvent };
    },

    // Rollback on error
    onError: (err, { eventId }, context) => {
      if (context?.previousEvent) {
        queryClient.setQueryData([EVENT_BY_ID_QUERY_KEY, eventId], context.previousEvent);
      }
      options?.onError?.(err, { eventId, userId: '', quantity: 1 }, context);
    },

    // Refetch after success or error
    onSettled: (data, error, { eventId }) => {
      queryClient.invalidateQueries({ queryKey: [EVENT_BY_ID_QUERY_KEY, eventId] });
      queryClient.invalidateQueries({ queryKey: [EVENTS_QUERY_KEY] });
    },

    ...options,
  });
}
```

## Error Handling Strategy

### Leveraging Existing Error Classes

The existing `api-errors.ts` already provides comprehensive error handling:

- **NetworkError** - No response from server
- **ValidationError** - 400 with validation details
- **UnauthorizedError** - 401 authentication required
- **ForbiddenError** - 403 insufficient permissions
- **NotFoundError** - 404 resource not found
- **ServerError** - 500+ server errors

### React Query Error Handling Pattern

```typescript
// In component
const { data, error, isLoading } = useEvents();

if (error) {
  if (error instanceof ValidationError) {
    // Show validation errors
    return <ValidationErrorDisplay errors={error.validationErrors} />;
  }
  if (error instanceof UnauthorizedError) {
    // Redirect to login
    router.push('/login');
  }
  if (error instanceof NotFoundError) {
    return <NotFound message="Events not found" />;
  }
  // Generic error
  return <ErrorDisplay message={error.message} />;
}
```

## Integration Points

### 1. Feed Integration

The Events API will integrate with the existing Feed system:

**Location:** `web/src/domain/models/FeedItem.ts`

```typescript
// Mapper function to convert EventDto to FeedItem
export function eventDtoToFeedItem(event: EventDto): FeedItem {
  return createFeedItem({
    id: event.id,
    type: 'event',
    author: {
      id: event.organizerId,
      name: 'Event Organizer', // TODO: Fetch organizer details
      initials: 'EO',
    },
    timestamp: new Date(event.createdAt),
    location: event.city || 'Virtual Event',
    title: event.title,
    description: event.description,
    actions: [
      { icon: 'calendar', label: 'Interested', count: event.currentRegistrations },
      { icon: 'message', label: 'Comments', count: 0 }, // TODO: Add comments
    ],
    metadata: {
      type: 'event',
      date: event.startDate,
      time: new Date(event.startDate).toLocaleTimeString(),
      venue: event.address || 'Virtual',
      interestedCount: event.currentRegistrations,
      commentCount: 0,
    },
  });
}
```

### 2. Authentication Integration

Events repository automatically uses `ApiClient` which has JWT token interceptors:

```typescript
// No additional code needed - ApiClient handles auth
// Token is automatically added to all requests via interceptor
```

### 3. Zustand Store Integration (Optional)

For complex event management pages, consider a Zustand store:

**Location:** `web/src/presentation/store/useEventsStore.ts`

```typescript
import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import type { EventDto } from '@/infrastructure/api/types/events.types';

interface EventsState {
  selectedEvent: EventDto | null;
  setSelectedEvent: (event: EventDto | null) => void;

  filters: GetEventsRequest;
  setFilters: (filters: GetEventsRequest) => void;
  clearFilters: () => void;
}

export const useEventsStore = create<EventsState>()(
  devtools(
    (set) => ({
      selectedEvent: null,
      setSelectedEvent: (event) => set({ selectedEvent: event }),

      filters: {},
      setFilters: (filters) => set({ filters }),
      clearFilters: () => set({ filters: {} }),
    }),
    { name: 'EventsStore' }
  )
);
```

## Reusable Components

### Component Hierarchy

```
EventList (uses useEvents hook)
├── EventCard (receives EventDto as prop)
│   ├── EventImage
│   ├── EventDetails
│   ├── EventLocation
│   └── EventActions
│       ├── RsvpButton (uses useRsvpToEvent hook)
│       └── ShareButton
│
EventDetails (uses useEventById hook)
├── EventHeader
├── EventGallery (images + videos)
├── EventDescription
├── EventLocationMap
├── RsvpSection (uses useRsvpToEvent, useCancelRsvp)
└── EventComments
```

### Shared Components to Leverage

**From Existing Codebase:**
- `Button` - Primary action buttons
- `Input` - Form inputs for event creation
- `Card` - Event cards in list view
- `Avatar` - Organizer avatar
- `Badge` - Event status badges
- `Skeleton` - Loading states

**New Components Needed:**
- `EventCard` - Event list item
- `EventDetails` - Full event view
- `RsvpButton` - RSVP action
- `EventFilters` - Filter sidebar
- `EventMap` - Location map (integrate with Google Maps)
- `EventGallery` - Image/video carousel

## Testing Strategy

### Unit Tests (Jest + React Testing Library)

**Repository Tests:**
```typescript
// web/src/infrastructure/api/repositories/__tests__/events.repository.test.ts
describe('EventsRepository', () => {
  it('should fetch events with filters', async () => {
    const mockEvents = [{ id: '1', title: 'Test Event' }];
    jest.spyOn(apiClient, 'get').mockResolvedValue(mockEvents);

    const result = await eventsRepository.getEvents({ category: 'Cultural' });

    expect(apiClient.get).toHaveBeenCalledWith('/events?category=Cultural');
    expect(result).toEqual(mockEvents);
  });
});
```

**Hook Tests:**
```typescript
// web/src/presentation/hooks/queries/events/__tests__/useEvents.test.ts
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useEvents } from '../useEvents';

const queryClient = new QueryClient();
const wrapper = ({ children }: any) => (
  <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
);

describe('useEvents', () => {
  it('should fetch events successfully', async () => {
    const { result } = renderHook(() => useEvents(), { wrapper });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data).toBeDefined();
  });
});
```

### Integration Tests

```typescript
// web/src/__tests__/integration/events-flow.test.ts
describe('Events Flow', () => {
  it('should create, update, and delete event', async () => {
    // Test full CRUD flow
  });

  it('should RSVP to event and cancel', async () => {
    // Test RSVP flow
  });
});
```

## Performance Optimizations

### React Query Configuration

```typescript
// web/src/app/providers.tsx
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes for events
      cacheTime: 10 * 60 * 1000, // 10 minutes in cache
      refetchOnWindowFocus: false,
      retry: 1, // Retry once on failure
    },
    mutations: {
      retry: 0, // Don't retry mutations
    },
  },
});
```

### Pagination for Large Lists

```typescript
export function useInfiniteEvents(filters: GetEventsRequest) {
  return useInfiniteQuery({
    queryKey: [EVENTS_QUERY_KEY, 'infinite', filters],
    queryFn: ({ pageParam = 1 }) =>
      eventsRepository.searchEvents({ ...filters, page: pageParam, pageSize: 20 }),
    getNextPageParam: (lastPage) =>
      lastPage.hasNextPage ? lastPage.page + 1 : undefined,
  });
}
```

### Prefetching for Better UX

```typescript
// Prefetch event details on hover
function EventCard({ event }: { event: EventDto }) {
  const queryClient = useQueryClient();

  const handleMouseEnter = () => {
    queryClient.prefetchQuery({
      queryKey: [EVENT_BY_ID_QUERY_KEY, event.id],
      queryFn: () => eventsRepository.getEventById(event.id),
    });
  };

  return <div onMouseEnter={handleMouseEnter}>...</div>;
}
```

## Migration Path

### Phase 1: Foundation (Week 1)
1. Create type definitions (`events.types.ts`, `common.types.ts`, `events.constants.ts`)
2. Implement `EventsRepository` class
3. Write unit tests for repository

### Phase 2: Query Hooks (Week 1-2)
1. Implement read hooks (`useEvents`, `useEventById`, `useSearchEvents`, `useNearbyEvents`)
2. Test hooks with React Testing Library
3. Create basic `EventList` and `EventCard` components

### Phase 3: Mutation Hooks (Week 2)
1. Implement mutation hooks (`useCreateEvent`, `useUpdateEvent`, `useRsvpToEvent`)
2. Add optimistic updates
3. Test mutation flows

### Phase 4: Integration (Week 2-3)
1. Integrate with Feed system
2. Add event mappers
3. Build full-featured event pages

### Phase 5: Advanced Features (Week 3)
1. Infinite scrolling
2. Real-time updates (if needed)
3. Performance optimizations

## ADR Summary

**Decision:** Use Repository Pattern + React Query Hooks for Events API

**Reasons:**
1. **Consistency** - Matches existing `AuthRepository` and `ProfileRepository`
2. **Type Safety** - Full TypeScript types matching backend DTOs
3. **Caching** - React Query provides automatic caching and refetching
4. **Testability** - Repository and hooks are easily unit tested
5. **Separation of Concerns** - Infrastructure separated from presentation
6. **DDD Alignment** - Repository abstraction aligns with DDD principles

**Consequences:**
- Requires creating multiple hook files (but organized by domain)
- Learning curve for React Query (minimal - already in package.json)
- Slight duplication between repository and hooks (acceptable for clarity)

**Alternatives Considered:**
1. **Service Pattern Only** - Rejected: Inconsistent with existing repositories
2. **Zustand Store Only** - Rejected: No caching, manual refetch logic
3. **Direct API Calls** - Rejected: No abstraction, hard to test

## References

- Backend API: `src/LankaConnect.API/Controllers/EventsController.cs`
- Backend DTOs: `src/LankaConnect.Application/Events/Common/`
- Frontend API Client: `web/src/infrastructure/api/client/api-client.ts`
- Existing Repository: `web/src/infrastructure/api/repositories/profile.repository.ts`
- React Query Docs: https://tanstack.com/query/latest
- Clean Architecture: `docs/CLAUDE.md`

---

**Next Steps:**
1. Review and approve this architecture
2. Create GitHub issue/epic for implementation
3. Begin Phase 1 implementation
4. Schedule code review after each phase
