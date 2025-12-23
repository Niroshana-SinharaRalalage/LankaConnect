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
  AnonymousRegistrationRequest,
  AnonymousRegistrationResponse, // Phase 6A.44
  UpdateRsvpRequest,
  UpdateRegistrationRequest,
  CancelEventRequest,
  PostponeEventRequest,
  CreateEventResponse,
  EventImageDto,
  EventVideoDto,
  SignUpListDto,
  AddSignUpListRequest,
  CommitToSignUpRequest,
  CancelCommitmentRequest,
  CreateSignUpListRequest,
  UpdateSignUpListRequest,
  AddSignUpItemRequest,
  UpdateSignUpItemRequest,
  CommitToSignUpItemRequest,
  CommitToSignUpItemAnonymousRequest,
  EventRegistrationCheckResult,
  RegistrationDetailsDto,
  TicketDto,
  // Phase 6A.27: Open Sign-Up Items
  AddOpenSignUpItemRequest,
  UpdateOpenSignUpItemRequest,
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

  // ==================== AUTHENTICATED QUERIES ====================

  // ==================== AUTHENTICATED MUTATIONS ====================

  /**
   * Create a new event
   * Requires authentication
   * Maps to backend CreateEventCommand
   * Backend returns the event ID as a plain JSON string
   */
  async createEvent(data: CreateEventRequest): Promise<string> {
    // Backend returns event ID as a plain JSON string (e.g., "40b297c9-2867-4f6b-900c-b5d0f230efe8")
    const eventId = await apiClient.post<string>(this.basePath, data);
    return eventId;
  }

  /**
   * Update an existing event
   * Requires authentication and ownership
   * Maps to backend UpdateEventCommand
   */
  async updateEvent(id: string, data: UpdateEventRequest): Promise<void> {
    await apiClient.put<void>(`${this.basePath}/${id}`, data);
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
   * Phase 6A.41: Unpublish event (return to Draft status)
   * Allows organizers to make corrections after premature publication
   */
  async unpublishEvent(id: string): Promise<void> {
    await apiClient.post<void>(`${this.basePath}/${id}/unpublish`);
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
   * Session 23: Returns Stripe checkout URL for paid events, null for free events
   * Phase 6A.11: Updated to support multi-attendee registrations with detailed attendee information
   * - Legacy format: { userId, quantity } - simple quantity-based RSVP
   * - New format: { userId, attendees: [{name, age}, ...], email, phoneNumber, address, successUrl, cancelUrl }
   */
  async rsvpToEvent(eventId: string, request: RsvpRequest): Promise<string | null> {
    return await apiClient.post<string | null>(`${this.basePath}/${eventId}/rsvp`, request);
  }

  /**
   * Cancel RSVP
   * Removes registration and frees up capacity
   * Phase 6A.28: Added deleteSignUpCommitments parameter for user choice
   * @param eventId - The event ID
   * @param deleteSignUpCommitments - If true, deletes sign-up commitments and restores remaining quantities
   */
  async cancelRsvp(eventId: string, deleteSignUpCommitments: boolean = false): Promise<void> {
    const params = deleteSignUpCommitments ? '?deleteSignUpCommitments=true' : '';
    await apiClient.delete<void>(`${this.basePath}/${eventId}/rsvp${params}`);
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
   * Phase 6A.14: Update registration details (attendees and contact information)
   * Allows users to edit their registration after initial RSVP
   * Business Rules:
   * - Cannot change attendee count on paid registrations
   * - Maximum 10 attendees per registration
   * - Cannot update cancelled or refunded registrations
   */
  async updateRegistrationDetails(eventId: string, request: UpdateRegistrationRequest): Promise<void> {
    await apiClient.put<void>(`${this.basePath}/${eventId}/my-registration`, request);
  }

  /**
   * Register anonymous attendee for an event
   * No authentication required - for users without accounts
   * Maps to backend RegisterAnonymousAttendeeCommand
   * Phase 6A.44: Returns checkout URL for paid events, null for free events
   */
  async registerAnonymous(eventId: string, request: AnonymousRegistrationRequest): Promise<AnonymousRegistrationResponse> {
    return await apiClient.post<AnonymousRegistrationResponse>(`${this.basePath}/${eventId}/register-anonymous`, request);
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
   * Get user's registration details for a specific event
   * Fix 1: Enhanced registration status detection
   * Returns full registration with attendee names and ages
   * Maps to backend GetUserRegistrationForEventQuery
   */
  async getUserRegistrationForEvent(eventId: string): Promise<RegistrationDetailsDto | null> {
    try {
      const response = await apiClient.get<any>(`${this.basePath}/${eventId}/my-registration`);

      // Backend returns Result<T> wrapper, unwrap it
      if (response && response.isSuccess && response.value) {
        return response.value as RegistrationDetailsDto;
      }

      // If response is already the DTO (for backward compatibility)
      if (response && response.id && response.eventId) {
        return response as RegistrationDetailsDto;
      }

      return null;
    } catch (error: any) {
      // Return null if no registration found (404)
      if (error?.response?.status === 404) {
        return null;
      }
      throw error;
    }
  }

  /**
   * Check if an email has registered for an event
   * Phase 6A.15: Enhanced sign-up list UX with email validation
   * Phase 6A.23: Updated to return detailed member/registration status
   * Maps to backend CheckEventRegistrationQuery
   */
  async checkEventRegistrationByEmail(eventId: string, email: string): Promise<EventRegistrationCheckResult> {
    return await apiClient.post<EventRegistrationCheckResult>(`${this.basePath}/${eventId}/check-registration`, { email });
  }

  /**
   * Anonymous user commits to a sign-up item
   * Phase 6A.23: Supports anonymous sign-up workflow
   * Email must be registered for the event (member or anonymous)
   * If email belongs to a member, user will be prompted to log in instead
   */
  async commitToSignUpItemAnonymous(
    eventId: string,
    signupId: string,
    itemId: string,
    data: CommitToSignUpItemAnonymousRequest
  ): Promise<string> {
    return await apiClient.post<string>(
      `${this.basePath}/${eventId}/signups/${signupId}/items/${itemId}/commit-anonymous`,
      data
    );
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

  // ==================== SIGN-UP MANAGEMENT ====================

  /**
   * Get all sign-up lists for an event
   * Returns sign-up lists with commitments
   * Maps to backend GET /api/events/{id}/signups
   */
  async getEventSignUpLists(eventId: string): Promise<SignUpListDto[]> {
    return await apiClient.get<SignUpListDto[]>(`${this.basePath}/${eventId}/signups`);
  }

  /**
   * Add a sign-up list to event
   * Organizer-only operation
   * Maps to backend POST /api/events/{id}/signups
   */
  async addSignUpList(eventId: string, request: AddSignUpListRequest): Promise<void> {
    await apiClient.post<void>(`${this.basePath}/${eventId}/signups`, request);
  }

  /**
   * Remove a sign-up list from event
   * Organizer-only operation
   * Maps to backend DELETE /api/events/{eventId}/signups/{signupId}
   */
  async removeSignUpList(eventId: string, signupId: string): Promise<void> {
    await apiClient.delete<void>(`${this.basePath}/${eventId}/signups/${signupId}`);
  }

  /**
   * Commit to bringing an item to event
   * User commits to sign-up list
   * Maps to backend POST /api/events/{eventId}/signups/{signupId}/commit
   */
  async commitToSignUp(
    eventId: string,
    signupId: string,
    request: CommitToSignUpRequest
  ): Promise<void> {
    await apiClient.post<void>(`${this.basePath}/${eventId}/signups/${signupId}/commit`, request);
  }

  /**
   * Cancel user's commitment to sign-up list
   * Maps to backend DELETE /api/events/{eventId}/signups/{signupId}/commit
   */
  async cancelCommitment(
    eventId: string,
    signupId: string,
    request: CancelCommitmentRequest
  ): Promise<void> {
    await apiClient.delete<void>(
      `${this.basePath}/${eventId}/signups/${signupId}/commit`,
      { data: request }
    );
  }

  // ==================== CATEGORY-BASED SIGN-UP MANAGEMENT ====================

  /**
   * Create sign-up list WITH items in a single API call
   * Organizer-only operation
   * Maps to backend POST /api/events/{id}/signups
   * Returns the created sign-up list ID
   */
  async createSignUpList(
    eventId: string,
    request: CreateSignUpListRequest
  ): Promise<string> {
    return await apiClient.post<string>(`${this.basePath}/${eventId}/signups`, request);
  }

  /**
   * Update sign-up list details (category, description, and category flags)
   * Phase 6A.13: Edit Sign-Up List feature
   */
  async updateSignUpList(
    eventId: string,
    signupId: string,
    request: UpdateSignUpListRequest
  ): Promise<void> {
    await apiClient.put<void>(`${this.basePath}/${eventId}/signups/${signupId}`, request);
  }

  /**
   * Add an item to a category-based sign-up list
   * Organizer-only operation
   * Maps to backend POST /api/events/{eventId}/signups/{signupId}/items
   */
  async addSignUpItem(
    eventId: string,
    signupId: string,
    request: AddSignUpItemRequest
  ): Promise<string> {
    return await apiClient.post<string>(`${this.basePath}/${eventId}/signups/${signupId}/items`, request);
  }

  /**
   * Update an item in a category-based sign-up list
   * Phase 6A.14: Edit Sign-Up Item feature
   * Organizer-only operation
   * Maps to backend PUT /api/events/{eventId}/signups/{signupId}/items/{itemId}
   */
  async updateSignUpItem(
    eventId: string,
    signupId: string,
    itemId: string,
    request: UpdateSignUpItemRequest
  ): Promise<void> {
    await apiClient.put<void>(`${this.basePath}/${eventId}/signups/${signupId}/items/${itemId}`, request);
  }

  /**
   * Remove an item from a category-based sign-up list
   * Organizer-only operation
   * Maps to backend DELETE /api/events/{eventId}/signups/{signupId}/items/{itemId}
   */
  async removeSignUpItem(
    eventId: string,
    signupId: string,
    itemId: string
  ): Promise<void> {
    await apiClient.delete<void>(`${this.basePath}/${eventId}/signups/${signupId}/items/${itemId}`);
  }

  /**
   * User commits to bringing a specific item
   * Maps to backend POST /api/events/{eventId}/signups/{signupId}/items/{itemId}/commit
   */
  async commitToSignUpItem(
    eventId: string,
    signupId: string,
    itemId: string,
    request: CommitToSignUpItemRequest
  ): Promise<void> {
    // Increase timeout for commitment operations as email validation adds latency
    await apiClient.post<void>(
      `${this.basePath}/${eventId}/signups/${signupId}/items/${itemId}/commit`,
      request,
      { timeout: 60000 } // 60 seconds timeout for commitment operations
    );
  }

  // ==================== PHASE 6A.27: OPEN SIGN-UP ITEMS ====================

  /**
   * Add an Open sign-up item (user-submitted)
   * Phase 6A.27: Users can add their own items to sign-up lists with hasOpenItems enabled
   * Maps to backend POST /api/events/{eventId}/signups/{signupId}/open-items
   *
   * @param eventId - Event ID (GUID)
   * @param signupId - Sign-up list ID (GUID)
   * @param request - Open item details
   * @returns Created item ID
   */
  async addOpenSignUpItem(
    eventId: string,
    signupId: string,
    request: AddOpenSignUpItemRequest
  ): Promise<string> {
    return await apiClient.post<string>(
      `${this.basePath}/${eventId}/signups/${signupId}/open-items`,
      request
    );
  }

  /**
   * Update an Open sign-up item
   * Phase 6A.27: Only the user who created the item can update it
   * Maps to backend PUT /api/events/{eventId}/signups/{signupId}/open-items/{itemId}
   *
   * @param eventId - Event ID (GUID)
   * @param signupId - Sign-up list ID (GUID)
   * @param itemId - Item ID (GUID)
   * @param request - Updated item details
   */
  async updateOpenSignUpItem(
    eventId: string,
    signupId: string,
    itemId: string,
    request: UpdateOpenSignUpItemRequest
  ): Promise<void> {
    await apiClient.put<void>(
      `${this.basePath}/${eventId}/signups/${signupId}/open-items/${itemId}`,
      request
    );
  }

  /**
   * Cancel/Delete an Open sign-up item
   * Phase 6A.27: Only the user who created the item can cancel it
   * Maps to backend DELETE /api/events/{eventId}/signups/{signupId}/open-items/{itemId}
   *
   * @param eventId - Event ID (GUID)
   * @param signupId - Sign-up list ID (GUID)
   * @param itemId - Item ID (GUID)
   */
  async cancelOpenSignUpItem(
    eventId: string,
    signupId: string,
    itemId: string
  ): Promise<void> {
    await apiClient.delete<void>(
      `${this.basePath}/${eventId}/signups/${signupId}/open-items/${itemId}`
    );
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

  // ==================== MEDIA MANAGEMENT ====================

  /**
   * Upload an image to an event
   * Maps to backend POST /api/events/{id}/images
   *
   * @param eventId - Event ID (GUID)
   * @param file - Image file to upload (max 10MB, jpg/png/gif/webp)
   * @returns EventImageDto with image metadata
   */
  async uploadEventImage(eventId: string, file: File): Promise<EventImageDto> {
    const formData = new FormData();
    formData.append('image', file);

    // Use apiClient.postMultipart for proper authentication and error handling
    return await apiClient.postMultipart<EventImageDto>(
      `${this.basePath}/${eventId}/images`,
      formData
    );
  }

  /**
   * Delete an image from an event
   * Maps to backend DELETE /api/events/{eventId}/images/{imageId}
   *
   * @param eventId - Event ID (GUID)
   * @param imageId - Image ID (GUID)
   */
  async deleteEventImage(eventId: string, imageId: string): Promise<void> {
    await apiClient.delete(`${this.basePath}/${eventId}/images/${imageId}`);
  }

  /**
   * Replace an existing event image
   * Maps to backend PUT /api/events/{eventId}/images/{imageId}
   *
   * @param eventId - Event ID (GUID)
   * @param imageId - Image ID (GUID) to replace
   * @param file - New image file
   * @returns Updated EventImageDto
   */
  async replaceEventImage(eventId: string, imageId: string, file: File): Promise<EventImageDto> {
    const formData = new FormData();
    formData.append('image', file);

    const baseURL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';
    const response = await fetch(`${baseURL}${this.basePath}/${eventId}/images/${imageId}`, {
      method: 'PUT',
      body: formData,
      credentials: 'include',
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Replace failed' }));
      throw new Error(error.message || `Replace failed with status ${response.status}`);
    }

    return await response.json();
  }

  /**
   * Reorder event images
   * Maps to backend PUT /api/events/{id}/images/reorder
   *
   * @param eventId - Event ID (GUID)
   * @param newOrders - Map of image ID to new display order (1-indexed)
   */
  async reorderEventImages(eventId: string, newOrders: Record<string, number>): Promise<void> {
    await apiClient.put(`${this.basePath}/${eventId}/images/reorder`, { newOrders });
  }

  /**
   * Set an image as primary (main thumbnail)
   * Maps to backend POST /api/events/{id}/images/{imageId}/set-primary
   * Phase 6A.13: Primary Image Selection
   *
   * @param eventId - Event ID (GUID)
   * @param imageId - Image ID (GUID) to set as primary
   */
  async setPrimaryImage(eventId: string, imageId: string): Promise<void> {
    await apiClient.post(`${this.basePath}/${eventId}/images/${imageId}/set-primary`, {});
  }

  /**
   * Upload a video to an event
   * Maps to backend POST /api/events/{id}/videos
   *
   * @param eventId - Event ID (GUID)
   * @param videoFile - Video file to upload
   * @param thumbnailFile - Thumbnail image file
   * @returns EventVideoDto with video metadata
   */
  async uploadEventVideo(eventId: string, videoFile: File, thumbnailFile: File): Promise<EventVideoDto> {
    const formData = new FormData();
    formData.append('video', videoFile);
    formData.append('thumbnail', thumbnailFile);

    // Use apiClient.postMultipart for proper authentication and error handling
    // Video files can be large (up to 100MB), so use 5-minute timeout
    return await apiClient.postMultipart<EventVideoDto>(
      `${this.basePath}/${eventId}/videos`,
      formData,
      {
        timeout: 300000, // 5 minutes for large video uploads
      }
    );
  }

  /**
   * Delete a video from an event
   * Maps to backend DELETE /api/events/{eventId}/videos/{videoId}
   *
   * @param eventId - Event ID (GUID)
   * @param videoId - Video ID (GUID)
   */
  async deleteEventVideo(eventId: string, videoId: string): Promise<void> {
    await apiClient.delete(`${this.basePath}/${eventId}/videos/${videoId}`);
  }

  // ==================== TICKET ENDPOINTS (Phase 6A.24) ====================

  /**
   * Get ticket for user's registration
   * Phase 6A.24: Returns ticket details with QR code for paid events
   * Maps to backend GET /api/events/{eventId}/my-registration/ticket
   *
   * @param eventId - Event ID (GUID)
   * @returns Ticket details including QR code and attendee info
   */
  async getMyTicket(eventId: string): Promise<TicketDto> {
    return await apiClient.get<TicketDto>(`${this.basePath}/${eventId}/my-registration/ticket`);
  }

  /**
   * Download ticket as PDF
   * Phase 6A.24: Returns PDF blob for ticket download
   * Phase 6A.24 FIX: Now uses apiClient for proper authentication
   * Maps to backend GET /api/events/{eventId}/my-registration/ticket/pdf
   *
   * @param eventId - Event ID (GUID)
   * @returns PDF blob for download
   */
  async downloadTicketPdf(eventId: string): Promise<Blob> {
    // Use apiClient with responseType: 'blob' to properly handle auth and binary response
    return await apiClient.get<Blob>(
      `${this.basePath}/${eventId}/my-registration/ticket/pdf`,
      { responseType: 'blob' }
    );
  }

  /**
   * Resend ticket email
   * Phase 6A.24: Resends ticket confirmation email to registration contact
   * Maps to backend POST /api/events/{eventId}/my-registration/ticket/resend-email
   *
   * @param eventId - Event ID (GUID)
   */
  async resendTicketEmail(eventId: string): Promise<void> {
    await apiClient.post(`${this.basePath}/${eventId}/my-registration/ticket/resend-email`, {});
  }
}

/**
 * Singleton instance of the events repository
 * Export for use in React components and hooks
 */
export const eventsRepository = new EventsRepository();
