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
  EventImageDto,
} from '../types/events.types';
import type { PagedResult } from '../types/common.types';

/**
 * EventsRepository
 * Handles all event-related API calls following the repository pattern
 *
 * Backend endpoints from EventsController.cs:
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
   * Get all events with optional filtering and location-based sorting
   * Maps to backend GetEventsQuery
   *
   * Location-based sorting:
   * - For authenticated users: Pass userId to sort by preferred metros or home location
   * - For anonymous users: Pass latitude + longitude to sort by coordinates
   * - For specific metro filter: Pass metroAreaIds
   */
  async getEvents(filters: GetEventsRequest = {}): Promise<EventDto[]> {
    const params = new URLSearchParams();

    // Traditional filters
    if (filters.status !== undefined) params.append('status', String(filters.status));
    if (filters.category !== undefined) params.append('category', String(filters.category));
    if (filters.startDateFrom) params.append('startDateFrom', filters.startDateFrom);
    if (filters.startDateTo) params.append('startDateTo', filters.startDateTo);
    if (filters.isFreeOnly !== undefined) params.append('isFreeOnly', String(filters.isFreeOnly));
    if (filters.city) params.append('city', filters.city);

    // NEW: Location-based sorting parameters
    if (filters.state) params.append('state', filters.state);
    if (filters.userId) params.append('userId', filters.userId);
    if (filters.latitude !== undefined) params.append('latitude', String(filters.latitude));
    if (filters.longitude !== undefined) params.append('longitude', String(filters.longitude));
    if (filters.metroAreaIds && filters.metroAreaIds.length > 0) {
      // Add each metro area ID as a separate query parameter
      filters.metroAreaIds.forEach(id => params.append('metroAreaIds', id));
    }

    const queryString = params.toString();
    const url = queryString ? `${this.basePath}?${queryString}` : this.basePath;

    return await apiClient.get<EventDto[]>(url);
  }

  /**
   * Get event by ID
   * Maps to backend GetEventByIdQuery
   */
  async getEventById(id: string): Promise<EventDto> {
    return await apiClient.get<EventDto>(`${this.basePath}/${id}`);
  }

  /**
   * Search events using full-text search (PostgreSQL FTS)
   * Returns paginated results with relevance scores
   */
  async searchEvents(request: SearchEventsRequest): Promise<PagedResult<EventSearchResultDto>> {
    const params = new URLSearchParams({
      searchTerm: request.searchTerm,
      page: String(request.page ?? 1),
      pageSize: String(request.pageSize ?? 20),
    });

    if (request.category !== undefined) params.append('category', String(request.category));
    if (request.isFreeOnly !== undefined) params.append('isFreeOnly', String(request.isFreeOnly));
    if (request.startDateFrom) params.append('startDateFrom', request.startDateFrom);

    return await apiClient.get<PagedResult<EventSearchResultDto>>(
      `${this.basePath}/search?${params.toString()}`
    );
  }

  /**
   * Get nearby events using geospatial query
   * Maps to backend GetNearbyEventsQuery
   */
  async getNearbyEvents(request: GetNearbyEventsRequest): Promise<EventDto[]> {
    const params = new URLSearchParams({
      latitude: String(request.latitude),
      longitude: String(request.longitude),
      radiusKm: String(request.radiusKm),
    });

    if (request.category !== undefined) params.append('category', String(request.category));
    if (request.isFreeOnly !== undefined) params.append('isFreeOnly', String(request.isFreeOnly));
    if (request.startDateFrom) params.append('startDateFrom', request.startDateFrom);

    return await apiClient.get<EventDto[]>(`${this.basePath}/nearby?${params.toString()}`);
  }

  /**
   * Get featured events for landing page
   * Returns up to 4 events sorted by location relevance
   * For authenticated users: Uses preferred metro areas
   * For anonymous users: Uses provided coordinates or default location
   */
  async getFeaturedEvents(
    userId?: string,
    latitude?: number,
    longitude?: number
  ): Promise<EventDto[]> {
    const params = new URLSearchParams();

    if (userId) params.append('userId', userId);
    if (latitude !== undefined) params.append('latitude', String(latitude));
    if (longitude !== undefined) params.append('longitude', String(longitude));

    const queryString = params.toString();
    const url = queryString ? `${this.basePath}/featured?${queryString}` : `${this.basePath}/featured`;

    return await apiClient.get<EventDto[]>(url);
  }

  // ==================== AUTHENTICATED MUTATIONS ====================

  /**
   * Create a new event
   * Requires authentication
   * Maps to backend CreateEventCommand
   */
  async createEvent(data: CreateEventRequest): Promise<string> {
    const response = await apiClient.post<CreateEventResponse>(this.basePath, data);
    return response.id;
  }

  /**
   * Update an existing event
   * Requires authentication and ownership
   * Maps to backend UpdateEventCommand
   */
  async updateEvent(id: string, data: UpdateEventRequest): Promise<void> {
    await apiClient.put<void>(`${this.basePath}/${id}`, { ...data, eventId: id });
  }

  /**
   * Delete an event
   * Requires authentication and ownership
   * Only allowed for Draft/Cancelled events
   */
  async deleteEvent(id: string): Promise<void> {
    await apiClient.delete<void>(`${this.basePath}/${id}`);
  }

  /**
   * Submit event for approval (if approval workflow is enabled)
   */
  async submitForApproval(id: string): Promise<void> {
    await apiClient.post<void>(`${this.basePath}/${id}/submit`);
  }

  /**
   * Publish event (make it visible to public)
   * Requires authentication and ownership
   */
  async publishEvent(id: string): Promise<void> {
    await apiClient.post<void>(`${this.basePath}/${id}/publish`);
  }

  /**
   * Cancel event with reason
   * Notifies all registered users
   */
  async cancelEvent(id: string, reason: string): Promise<void> {
    const request: CancelEventRequest = { reason };
    await apiClient.post<void>(`${this.basePath}/${id}/cancel`, request);
  }

  /**
   * Postpone event with reason
   * Changes status to Postponed
   */
  async postponeEvent(id: string, reason: string): Promise<void> {
    const request: PostponeEventRequest = { reason };
    await apiClient.post<void>(`${this.basePath}/${id}/postpone`, request);
  }

  // ==================== RSVP OPERATIONS ====================

  /**
   * RSVP to an event
   * Creates a registration for the user
   * Maps to backend RsvpToEventCommand
   */
  async rsvpToEvent(eventId: string, userId: string, quantity: number = 1): Promise<void> {
    const request: RsvpRequest = { eventId, userId, quantity };
    await apiClient.post<void>(`${this.basePath}/${eventId}/rsvp`, request);
  }

  /**
   * Cancel RSVP
   * Removes registration and frees up capacity
   */
  async cancelRsvp(eventId: string): Promise<void> {
    await apiClient.delete<void>(`${this.basePath}/${eventId}/rsvp`);
  }

  /**
   * Update RSVP quantity
   * Changes number of attendees for registration
   */
  async updateRsvp(eventId: string, userId: string, newQuantity: number): Promise<void> {
    const request: UpdateRsvpRequest = { userId, newQuantity };
    await apiClient.put<void>(`${this.basePath}/${eventId}/rsvp`, request);
  }

  /**
   * Get current user's RSVPs
   * Epic 1: Backend now returns full EventDto[] instead of RsvpDto[] for better UX
   * Returns all events user has registered for
   */
  async getUserRsvps(): Promise<EventDto[]> {
    return await apiClient.get<EventDto[]>(`${this.basePath}/my-rsvps`);
  }

  /**
   * Get upcoming events for user
   * Returns events happening in the future
   */
  async getUpcomingEvents(): Promise<EventDto[]> {
    return await apiClient.get<EventDto[]>(`${this.basePath}/upcoming`);
  }

  /**
   * Get events created by current user
   * Returns all events user has created as organizer
   */
  async getUserCreatedEvents(): Promise<EventDto[]> {
    return await apiClient.get<EventDto[]>(`${this.basePath}/my-events`);
  }

  // ==================== WAITING LIST ====================

  /**
   * Add user to waiting list
   * Used when event is at capacity
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
   * Returns list of users waiting for spots
   */
  async getWaitingList(eventId: string): Promise<WaitingListEntryDto[]> {
    return await apiClient.get<WaitingListEntryDto[]>(`${this.basePath}/${eventId}/waiting-list`);
  }

  // ==================== MEDIA OPERATIONS ====================

  /**
   * Upload image to event gallery
   * Uses multipart/form-data for file upload
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

  // ==================== UTILITY OPERATIONS ====================

  /**
   * Export event as ICS calendar file
   * Returns blob for download
   */
  async getEventIcs(eventId: string): Promise<Blob> {
    // Note: This endpoint returns a file, not JSON
    // Using fetch directly instead of apiClient
    const baseURL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';
    const response = await fetch(`${baseURL}${this.basePath}/${eventId}/ics`);

    if (!response.ok) {
      throw new Error('Failed to download ICS file');
    }

    return await response.blob();
  }

  /**
   * Record social share for analytics
   * Tracks event sharing on social media
   */
  async recordEventShare(eventId: string, platform?: string): Promise<void> {
    await apiClient.post<void>(`${this.basePath}/${eventId}/share`, { platform });
  }
}

/**
 * Singleton instance of the events repository
 * Export for use in React components and hooks
 */
export const eventsRepository = new EventsRepository();
