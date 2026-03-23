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
  AddOpenSignUpItemAnonymousRequest,
  UpdateOpenSignUpItemRequest,
  // Phase 6A.45: Attendee Management
  EventAttendeesResponse,
  // Phase 6A.61: Event Notification
  EventNotificationHistoryDto,
  // Phase 6A.76: Event Reminder History
  EventReminderHistoryDto,
  // Add-Only Attendees with Delta Payment
  CalculateAdditionPriceRequest,
  AdditionPriceResultDto,
  InitiateAddAttendeesRequest,
  InitiateAddAttendeesResult,
  PendingAdditionDto,
  CancelPendingAdditionResult,
  // Custom Forms (Survey/Form Sign-Up Type)
  EventFormDto,
  EventFormDetailDto,
  FormResponseDto,
  FormResponsesPagedDto,
  CreateEventFormRequest,
  UpdateEventFormRequest,
  AddFormQuestionRequest,
  UpdateFormQuestionRequest,
  ReorderFormQuestionsRequest,
  SubmitFormResponseRequest,
  SubmitFormResponseResult,
  UpdateFormResponseRequest,
  // Phase 6A.133: Co-Organizer Management
  UserSearchResultDto,
  // Cancellation result
  CancelRsvpResult,
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
   *
   * Issue #36: Status filtering:
   * - statusFilter: User-friendly status group filter (Active, Inactive, Cancelled, Unpublished, All)
   * - includeAllStatuses: When true, includes Draft/UnderReview events (for organizer view)
   */
  async getEvents(filters: GetEventsRequest = {}): Promise<EventDto[]> {
    const params = new URLSearchParams();

    // Traditional filters
    if (filters.status !== undefined) params.append('status', String(filters.status));
    // Issue #36: User-friendly status filter (takes precedence over status)
    if (filters.statusFilter !== undefined) params.append('statusFilter', String(filters.statusFilter));
    if (filters.category !== undefined) params.append('category', String(filters.category));
    if (filters.startDateFrom) params.append('startDateFrom', filters.startDateFrom);
    if (filters.startDateTo) params.append('startDateTo', filters.startDateTo);
    if (filters.isFreeOnly !== undefined) params.append('isFreeOnly', String(filters.isFreeOnly));
    if (filters.city) params.append('city', filters.city);

    // Location-based sorting parameters
    if (filters.state) params.append('state', filters.state);
    if (filters.userId) params.append('userId', filters.userId);
    if (filters.latitude !== undefined) params.append('latitude', String(filters.latitude));
    if (filters.longitude !== undefined) params.append('longitude', String(filters.longitude));
    if (filters.metroAreaIds && filters.metroAreaIds.length > 0) {
      // Add each metro area ID as a separate query parameter
      filters.metroAreaIds.forEach(id => params.append('metroAreaIds', id));
    }

    // Phase 6A.58: Text search filter
    if (filters.searchTerm) params.append('searchTerm', filters.searchTerm);

    // Issue #36: Include Draft/UnderReview events (for organizer's Event Management view)
    if (filters.includeAllStatuses) params.append('includeAllStatuses', 'true');

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
   * Phase 6A.114 Issue #81: Get events created by the authenticated organizer
   * Maps to backend GET /api/Events/my-events
   * Requires authentication - returns only events owned by current user
   *
   * Security: Filtered by OrganizerId from JWT token on the backend
   * Used for newsletter creation, event management dashboard, etc.
   *
   * @param filters - Optional filters (category, date range, search term, status)
   * @returns EventDto[] - Array of events created by the authenticated organizer
   */
  async getMyEvents(filters?: {
    searchTerm?: string;
    category?: number;
    startDateFrom?: string;
    startDateTo?: string;
    state?: string;
    metroAreaIds?: string[];
    statusFilter?: number;
  }): Promise<EventDto[]> {
    const params = new URLSearchParams();

    if (filters?.searchTerm) params.append('searchTerm', filters.searchTerm);
    if (filters?.category !== undefined) params.append('category', String(filters.category));
    if (filters?.startDateFrom) params.append('startDateFrom', filters.startDateFrom);
    if (filters?.startDateTo) params.append('startDateTo', filters.startDateTo);
    if (filters?.state) params.append('state', filters.state);
    if (filters?.metroAreaIds && filters.metroAreaIds.length > 0) {
      filters.metroAreaIds.forEach(id => params.append('metroAreaIds', id));
    }
    if (filters?.statusFilter !== undefined) params.append('statusFilter', String(filters.statusFilter));

    const queryString = params.toString();
    const url = queryString ? `${this.basePath}/my-events?${queryString}` : `${this.basePath}/my-events`;

    return await apiClient.get<EventDto[]>(url);
  }

  /**
   * Search events using full-text search (PostgreSQL FTS)
   * Returns paginated results with relevance scores
   * Phase 6A.X Issue #36: Added excludeCancelled parameter to filter out cancelled events
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
    if (request.excludeCancelled !== undefined) params.append('excludeCancelled', String(request.excludeCancelled));

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
   * Issue #51: Update max attendees per registration
   * Allows event organizers to configure how many attendees can be added in a single registration
   * @param id Event ID
   * @param maxAttendeesPerRegistration New max value (1 to min(eventCapacity, 50))
   */
  async updateMaxAttendeesPerRegistration(id: string, maxAttendeesPerRegistration: number): Promise<void> {
    await apiClient.put<void>(`${this.basePath}/${id}/max-attendees-per-registration`, {
      maxAttendeesPerRegistration,
    });
  }

  /**
   * Cancel event with reason
   * Notifies all registered users
   *
   * Phase 6A.64: Background job implementation - instant API response
   * Event cancellation completes immediately. Emails are sent asynchronously via Hangfire.
   * Uses default 30s timeout (sufficient for instant response).
   */
  async cancelEvent(id: string, reason: string): Promise<void> {
    const request: CancelEventRequest = { reason };
    await apiClient.post<void>(`${this.basePath}/${id}/cancel`, request);
    // Note: Emails are sent in background. Check Hangfire dashboard for email job status.
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
   * Cancellation enhancement: Added deleteFormResponses and refundAddOnPurchases parameters
   * @param eventId - The event ID
   * @param options - Cancellation options (all default to false)
   */
  async cancelRsvp(
    eventId: string,
    options: {
      deleteSignUpCommitments?: boolean;
      deleteFormResponses?: boolean;
      refundAddOnPurchases?: boolean;
    } = {}
  ): Promise<CancelRsvpResult | null> {
    const params = new URLSearchParams();
    if (options.deleteSignUpCommitments) params.append('deleteSignUpCommitments', 'true');
    if (options.deleteFormResponses) params.append('deleteFormResponses', 'true');
    if (options.refundAddOnPurchases) params.append('refundAddOnPurchases', 'true');
    const queryString = params.toString();
    return await apiClient.delete<CancelRsvpResult | null>(`${this.basePath}/${eventId}/rsvp${queryString ? `?${queryString}` : ''}`);
  }

  /**
   * Phase 6A.91: Withdraw a pending refund request
   * Transitions registration from RefundRequested back to Confirmed
   * Only allowed before event has started
   * @param eventId - The event ID
   */
  async withdrawRefundRequest(eventId: string): Promise<void> {
    await apiClient.post<void>(`${this.basePath}/${eventId}/rsvp/withdraw-refund`);
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
   * Phase 6A.58: Added optional filters for category, date range, location, and text search
   */
  async getUserRsvps(filters?: GetEventsRequest): Promise<EventDto[]> {
    if (!filters) {
      return await apiClient.get<EventDto[]>(`${this.basePath}/my-rsvps`);
    }

    const params = new URLSearchParams();
    if (filters.searchTerm) params.append('searchTerm', filters.searchTerm);
    if (filters.category !== undefined) params.append('category', String(filters.category));
    if (filters.startDateFrom) params.append('startDateFrom', filters.startDateFrom);
    if (filters.startDateTo) params.append('startDateTo', filters.startDateTo);
    if (filters.state) params.append('state', filters.state);
    if (filters.metroAreaIds && filters.metroAreaIds.length > 0) {
      filters.metroAreaIds.forEach(id => params.append('metroAreaIds', id));
    }

    const queryString = params.toString();
    const url = queryString
      ? `${this.basePath}/my-rsvps?${queryString}`
      : `${this.basePath}/my-rsvps`;

    return await apiClient.get<EventDto[]>(url);
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
   * Phase 6A.44: Get registration details by registration ID (for anonymous users after payment)
   * Maps to backend GetRegistrationByIdQuery
   */
  async getRegistrationById(registrationId: string): Promise<RegistrationDetailsDto | null> {
    try {
      return await apiClient.get<RegistrationDetailsDto>(`${this.basePath}/registrations/${registrationId}`);
    } catch (error) {
      console.error('Failed to get registration by ID:', error);
      return null;
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
   * Phase 6A.58: Added optional filters for category, date range, location, and text search
   * Issue #36: Added statusFilter and includeAllStatuses for status group filtering
   */
  async getUserCreatedEvents(filters?: GetEventsRequest): Promise<EventDto[]> {
    if (!filters) {
      return await apiClient.get<EventDto[]>(`${this.basePath}/my-events`);
    }

    const params = new URLSearchParams();
    if (filters.searchTerm) params.append('searchTerm', filters.searchTerm);
    if (filters.category !== undefined) params.append('category', String(filters.category));
    if (filters.startDateFrom) params.append('startDateFrom', filters.startDateFrom);
    if (filters.startDateTo) params.append('startDateTo', filters.startDateTo);
    if (filters.state) params.append('state', filters.state);
    if (filters.metroAreaIds && filters.metroAreaIds.length > 0) {
      filters.metroAreaIds.forEach(id => params.append('metroAreaIds', id));
    }
    // Issue #36: Status filter parameters
    if (filters.statusFilter !== undefined) params.append('statusFilter', String(filters.statusFilter));
    if (filters.includeAllStatuses) params.append('includeAllStatuses', 'true');

    const queryString = params.toString();
    const url = queryString
      ? `${this.basePath}/my-events?${queryString}`
      : `${this.basePath}/my-events`;

    return await apiClient.get<EventDto[]>(url);
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
   * Add an Open sign-up item (anonymous user version)
   * Phase 6A.44: Anonymous users can add Open items if registered for the event
   * Maps to backend POST /api/events/{eventId}/signups/{signupId}/open-items-anonymous
   *
   * @param eventId - Event ID (GUID)
   * @param signupId - Sign-up list ID (GUID)
   * @param request - Open item details with contact info
   * @returns Created item ID
   */
  async addOpenSignUpItemAnonymous(
    eventId: string,
    signupId: string,
    request: AddOpenSignUpItemAnonymousRequest
  ): Promise<string> {
    return await apiClient.post<string>(
      `${this.basePath}/${eventId}/signups/${signupId}/open-items-anonymous`,
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

  // ==================== ATTENDEE MANAGEMENT (Phase 6A.45) ====================

  /**
   * Get all attendees for an event (organizer only)
   * Phase 6A.45: Returns complete list of registrations with attendee details
   * Maps to backend GET /api/events/{eventId}/attendees
   *
   * @param eventId - Event ID (GUID)
   * @returns Event attendees response with statistics
   */
  async getEventAttendees(eventId: string): Promise<EventAttendeesResponse> {
    return await apiClient.get<EventAttendeesResponse>(`${this.basePath}/${eventId}/attendees`);
  }

  /**
   * Export event attendees to Excel, CSV, or sign-up lists ZIP (organizer only)
   * Phase 6A.45: Returns file download with attendee data and signup lists
   * Phase 6A.69: Added 'signuplistszip' format for ZIP archive with multiple CSV files
   * Maps to backend GET /api/events/{eventId}/export?format={format}
   *
   * @param eventId - Event ID (GUID)
   * @param format - Export format ('excel', 'csv', 'signuplistszip', or 'signuplistsexcel')
   * @returns Blob for file download (Excel .xlsx, CSV .csv, or ZIP with multiple CSVs)
   */
  async exportEventAttendees(
    eventId: string,
    format: 'excel' | 'csv' | 'signuplistszip' | 'signuplistsexcel' = 'excel'
  ): Promise<Blob> {
    return await apiClient.get<Blob>(
      `${this.basePath}/${eventId}/export?format=${format}`,
      { responseType: 'blob' }
    );
  }

  /**
   * Phase 6A.61: Send event notification email to all attendees
   * @param eventId - Event ID (GUID)
   * @returns Recipient count (placeholder, actual count from background job)
   */
  async sendEventNotification(eventId: string): Promise<{ recipientCount: number }> {
    return await apiClient.post<{ recipientCount: number }>(
      `${this.basePath}/${eventId}/send-notification`,
      {} // Empty body - eventId is in URL
    );
  }

  /**
   * Phase 6A.76: Send manual event reminder email to all registered attendees
   * @param eventId - Event ID (GUID)
   * @param reminderType - Type of reminder: "1day", "2day", "7day", or "custom"
   * @returns Recipient count (actual sends may vary due to idempotency)
   */
  async sendEventReminder(
    eventId: string,
    reminderType: string = '1day'
  ): Promise<{ recipientCount: number }> {
    return await apiClient.post<{ recipientCount: number }>(
      `${this.basePath}/${eventId}/send-reminder?reminderType=${encodeURIComponent(reminderType)}`,
      {} // Empty body - eventId is in URL, reminderType in query string
    );
  }

  /**
   * Phase 6A.X: Resend registration confirmation email to specific attendee (Organizer action)
   * Allows organizers to manually resend confirmation emails from Attendees tab
   * Works for both free and paid event registrations
   * @param eventId - Event ID (GUID)
   * @param registrationId - Registration ID (GUID)
   * @returns Success message
   */
  async resendAttendeeConfirmation(
    eventId: string,
    registrationId: string
  ): Promise<{ message: string }> {
    return await apiClient.post<{ message: string }>(
      `${this.basePath}/${eventId}/attendees/${registrationId}/resend-confirmation`,
      {} // Empty body - IDs are in URL
    );
  }

  /**
   * Phase 6A.61: Get event notification history
   * @param eventId - Event ID (GUID)
   * @returns List of notification history records
   */
  async getEventNotificationHistory(eventId: string): Promise<EventNotificationHistoryDto[]> {
    return await apiClient.get<EventNotificationHistoryDto[]>(
      `${this.basePath}/${eventId}/notification-history`
    );
  }

  /**
   * Phase 6A.76: Get event reminder history
   * @param eventId - Event ID (GUID)
   * @returns List of reminder history records aggregated by type and date
   */
  async getEventReminderHistory(eventId: string): Promise<EventReminderHistoryDto[]> {
    return await apiClient.get<EventReminderHistoryDto[]>(
      `${this.basePath}/${eventId}/reminder-history`
    );
  }

  // ==================== ADD-ONLY ATTENDEES WITH DELTA PAYMENT ====================

  /**
   * Calculate the additional amount required to add new attendees to a registration
   * Add-Only Attendees Feature: Delta payment calculation
   * Maps to backend POST /api/events/registrations/{registrationId}/calculate-addition
   *
   * @param registrationId - Registration ID (GUID)
   * @param request - New attendees to calculate price for
   * @returns Pricing calculation result with breakdown
   */
  async calculateAdditionPrice(
    registrationId: string,
    request: CalculateAdditionPriceRequest
  ): Promise<AdditionPriceResultDto> {
    return await apiClient.post<AdditionPriceResultDto>(
      `${this.basePath}/registrations/${registrationId}/calculate-addition`,
      request
    );
  }

  /**
   * Initiate adding attendees to a paid registration
   * Creates a Stripe checkout session for the delta payment
   * Add-Only Attendees Feature: Initiate addition with payment
   * Maps to backend POST /api/events/registrations/{registrationId}/add-attendees
   *
   * @param registrationId - Registration ID (GUID)
   * @param request - New attendees and checkout URLs
   * @returns Result with Stripe checkout URL
   */
  async initiateAddAttendees(
    registrationId: string,
    request: InitiateAddAttendeesRequest
  ): Promise<InitiateAddAttendeesResult> {
    return await apiClient.post<InitiateAddAttendeesResult>(
      `${this.basePath}/registrations/${registrationId}/add-attendees`,
      request
    );
  }

  /**
   * Get pending addition for a registration
   * Returns null if no pending addition exists
   * Add-Only Attendees Feature: Check pending status
   * Maps to backend GET /api/events/registrations/{registrationId}/pending-addition
   *
   * @param registrationId - Registration ID (GUID)
   * @returns Pending addition details or null
   */
  async getPendingAddition(registrationId: string): Promise<PendingAdditionDto | null> {
    try {
      const result = await apiClient.get<PendingAdditionDto | null>(
        `${this.basePath}/registrations/${registrationId}/pending-addition`
      );
      // Phase 6A.128c: Defense-in-depth - validate we got an actual object back.
      // Backend returns 204 for no pending addition; API client normalizes to null.
      if (!result || typeof result !== 'object') {
        return null;
      }
      return result;
    } catch (error: any) {
      // 404 or 204 means no pending addition exists
      if (error?.response?.status === 404 || error?.response?.status === 204) {
        return null;
      }
      throw error;
    }
  }

  /**
   * Cancel a pending addition
   * Marks the addition as abandoned and invalidates checkout session
   * Add-Only Attendees Feature: Cancel pending addition
   * Maps to backend DELETE /api/events/registrations/{registrationId}/pending-addition
   *
   * @param registrationId - Registration ID (GUID)
   * @returns Cancellation result
   */
  async cancelPendingAddition(registrationId: string): Promise<CancelPendingAdditionResult> {
    return await apiClient.delete<CancelPendingAdditionResult>(
      `${this.basePath}/registrations/${registrationId}/pending-addition`
    );
  }

  // ==================== CUSTOM FORMS (SURVEY/FORM SIGN-UP TYPE) ====================

  /**
   * Get all forms for an event (organizer view)
   * Custom Forms Feature: List all forms with response counts
   * Maps to backend GET /api/events/{id}/forms
   *
   * @param eventId - Event ID (GUID)
   * @returns Array of event form summaries
   */
  async getEventForms(eventId: string): Promise<EventFormDto[]> {
    return await apiClient.get<EventFormDto[]>(`${this.basePath}/${eventId}/forms`);
  }

  /**
   * Get form detail with questions (public endpoint - AllowAnonymous)
   * Custom Forms Feature: Retrieve form for filling out
   * Maps to backend GET /api/events/{id}/forms/{formId}
   *
   * @param eventId - Event ID (GUID)
   * @param formId - Form ID (GUID)
   * @returns Event form detail with questions
   */
  async getEventFormDetail(eventId: string, formId: string): Promise<EventFormDetailDto> {
    return await apiClient.get<EventFormDetailDto>(`${this.basePath}/${eventId}/forms/${formId}`);
  }

  /**
   * Create a new event form with questions (organizer)
   * Custom Forms Feature: Create form
   * Maps to backend POST /api/events/{id}/forms
   *
   * @param eventId - Event ID (GUID)
   * @param request - Form creation request
   * @returns Created form ID (GUID)
   */
  async createEventForm(eventId: string, request: CreateEventFormRequest): Promise<string> {
    return await apiClient.post<string>(`${this.basePath}/${eventId}/forms`, request);
  }

  /**
   * Update form metadata (title, description, settings)
   * Custom Forms Feature: Edit form details
   * Maps to backend PUT /api/events/{id}/forms/{formId}
   *
   * @param eventId - Event ID (GUID)
   * @param formId - Form ID (GUID)
   * @param request - Form update request
   */
  async updateEventForm(
    eventId: string,
    formId: string,
    request: UpdateEventFormRequest
  ): Promise<void> {
    await apiClient.put(`${this.basePath}/${eventId}/forms/${formId}`, request);
  }

  /**
   * Delete a form (only if no responses)
   * Custom Forms Feature: Delete form
   * Maps to backend DELETE /api/events/{id}/forms/{formId}
   *
   * @param eventId - Event ID (GUID)
   * @param formId - Form ID (GUID)
   */
  async deleteEventForm(eventId: string, formId: string): Promise<void> {
    await apiClient.delete(`${this.basePath}/${eventId}/forms/${formId}`);
  }

  /**
   * Publish a form (Draft -> Active)
   * Custom Forms Feature: Make form available for responses
   * Maps to backend POST /api/events/{id}/forms/{formId}/publish
   *
   * @param eventId - Event ID (GUID)
   * @param formId - Form ID (GUID)
   */
  async publishEventForm(eventId: string, formId: string): Promise<void> {
    await apiClient.post(`${this.basePath}/${eventId}/forms/${formId}/publish`, {});
  }

  /**
   * Close a form (Active -> Closed)
   * Custom Forms Feature: Stop accepting new responses
   * Maps to backend POST /api/events/{id}/forms/{formId}/close
   *
   * @param eventId - Event ID (GUID)
   * @param formId - Form ID (GUID)
   */
  async closeEventForm(eventId: string, formId: string): Promise<void> {
    await apiClient.post(`${this.basePath}/${eventId}/forms/${formId}/close`, {});
  }

  /**
   * Reopen a form (Closed -> Active)
   * Custom Forms Feature: Resume accepting responses
   * Maps to backend POST /api/events/{id}/forms/{formId}/reopen
   *
   * @param eventId - Event ID (GUID)
   * @param formId - Form ID (GUID)
   */
  async reopenEventForm(eventId: string, formId: string): Promise<void> {
    await apiClient.post(`${this.basePath}/${eventId}/forms/${formId}/reopen`, {});
  }

  /**
   * Add a question to a form
   * Custom Forms Feature: Add question
   * Maps to backend POST /api/events/{id}/forms/{formId}/questions
   *
   * @param eventId - Event ID (GUID)
   * @param formId - Form ID (GUID)
   * @param request - Question request
   * @returns Created question ID (GUID)
   */
  async addFormQuestion(
    eventId: string,
    formId: string,
    request: AddFormQuestionRequest
  ): Promise<string> {
    return await apiClient.post<string>(
      `${this.basePath}/${eventId}/forms/${formId}/questions`,
      request
    );
  }

  /**
   * Update a question
   * Custom Forms Feature: Edit question
   * Maps to backend PUT /api/events/{id}/forms/{formId}/questions/{questionId}
   *
   * @param eventId - Event ID (GUID)
   * @param formId - Form ID (GUID)
   * @param questionId - Question ID (GUID)
   * @param request - Question update request
   */
  async updateFormQuestion(
    eventId: string,
    formId: string,
    questionId: string,
    request: UpdateFormQuestionRequest
  ): Promise<void> {
    await apiClient.put(
      `${this.basePath}/${eventId}/forms/${formId}/questions/${questionId}`,
      request
    );
  }

  /**
   * Delete a question (blocked if responses exist)
   * Custom Forms Feature: Remove question
   * Maps to backend DELETE /api/events/{id}/forms/{formId}/questions/{questionId}
   *
   * @param eventId - Event ID (GUID)
   * @param formId - Form ID (GUID)
   * @param questionId - Question ID (GUID)
   */
  async deleteFormQuestion(eventId: string, formId: string, questionId: string): Promise<void> {
    await apiClient.delete(`${this.basePath}/${eventId}/forms/${formId}/questions/${questionId}`);
  }

  /**
   * Reorder questions
   * Custom Forms Feature: Change question order
   * Maps to backend PUT /api/events/{id}/forms/{formId}/questions/reorder
   *
   * @param eventId - Event ID (GUID)
   * @param formId - Form ID (GUID)
   * @param request - Reorder request
   */
  async reorderFormQuestions(
    eventId: string,
    formId: string,
    request: ReorderFormQuestionsRequest
  ): Promise<void> {
    await apiClient.put(`${this.basePath}/${eventId}/forms/${formId}/questions/reorder`, request);
  }

  /**
   * Submit a form response (public endpoint - AllowAnonymous)
   * Custom Forms Feature: Submit response
   * Maps to backend POST /api/events/{id}/forms/{formId}/responses
   *
   * @param eventId - Event ID (GUID)
   * @param formId - Form ID (GUID)
   * @param request - Response submission request
   * @returns Response ID and access token for editing
   */
  async submitFormResponse(
    eventId: string,
    formId: string,
    request: SubmitFormResponseRequest
  ): Promise<SubmitFormResponseResult> {
    return await apiClient.post<SubmitFormResponseResult>(
      `${this.basePath}/${eventId}/forms/${formId}/responses`,
      request
    );
  }

  /**
   * Update a form response (Phase 6A.106-110 Fix: Supports both token and userId auth)
   * Anonymous users: Requires access token
   * Logged-in users: Uses JWT token (no access token needed)
   * Maps to backend PUT /api/events/{id}/forms/{formId}/responses/{responseId}?token={token}
   *
   * @param eventId - Event ID (GUID)
   * @param formId - Form ID (GUID)
   * @param responseId - Response ID (GUID)
   * @param accessToken - Access token (optional for logged-in users)
   * @param request - Response update request
   */
  async updateFormResponse(
    eventId: string,
    formId: string,
    responseId: string,
    accessToken: string | undefined,
    request: UpdateFormResponseRequest
  ): Promise<void> {
    const url = accessToken
      ? `${this.basePath}/${eventId}/forms/${formId}/responses/${responseId}?token=${accessToken}`
      : `${this.basePath}/${eventId}/forms/${formId}/responses/${responseId}`;

    // Phase 6A.111: Increase timeout for form updates (complex operations can take time)
    await apiClient.put(url, request, { timeout: 120000 }); // 2 minutes (was 30 seconds)
  }

  /**
   * Get own response using access token (public endpoint - AllowAnonymous)
   * Custom Forms Feature: Retrieve response for editing
   * Maps to backend GET /api/events/{id}/forms/{formId}/responses/mine?token={token}
   *
   * @param eventId - Event ID (GUID)
   * @param formId - Form ID (GUID)
   * @param accessToken - Access token from submission
   * @returns Form response with answers
   */
  async getMyFormResponse(
    eventId: string,
    formId: string,
    accessToken: string
  ): Promise<FormResponseDto> {
    return await apiClient.get<FormResponseDto>(
      `${this.basePath}/${eventId}/forms/${formId}/responses/mine?token=${accessToken}`
    );
  }

  /**
   * Get own response using userId (authenticated endpoint - Requires login)
   * Phase 6A.106-110 Fix: Enables Edit/Delete buttons for logged-in users in Signup Forms tab
   * Maps to backend GET /api/events/{id}/forms/{formId}/responses/my
   *
   * @param eventId - Event ID (GUID)
   * @param formId - Form ID (GUID)
   * @returns Form response with answers, or null if no response exists (HTTP 204)
   */
  async getMyFormResponseByUserId(
    eventId: string,
    formId: string
  ): Promise<FormResponseDto | null> {
    try {
      const result = await apiClient.get<FormResponseDto>(
        `${this.basePath}/${eventId}/forms/${formId}/responses/my`
      );
      // Phase 6A.128c: Defense-in-depth validation. The API client normalizes 204 to null,
      // but also guard against any non-object response (e.g., "" from older Axios behavior).
      if (!result || typeof result !== 'object') {
        return null;
      }
      return result;
    } catch (error: any) {
      // Fallback: some HTTP clients may throw on 204
      if (error.response?.status === 204) {
        return null;
      }
      throw error;
    }
  }

  /**
   * Get paginated responses for a form (organizer view)
   * Custom Forms Feature: View all responses
   * Maps to backend GET /api/events/{id}/forms/{formId}/responses?page=1&pageSize=20
   *
   * @param eventId - Event ID (GUID)
   * @param formId - Form ID (GUID)
   * @param page - Page number (default 1)
   * @param pageSize - Items per page (default 20)
   * @returns Paginated responses
   */
  async getFormResponses(
    eventId: string,
    formId: string,
    page: number = 1,
    pageSize: number = 20
  ): Promise<FormResponsesPagedDto> {
    return await apiClient.get<FormResponsesPagedDto>(
      `${this.basePath}/${eventId}/forms/${formId}/responses?page=${page}&pageSize=${pageSize}`
    );
  }

  /**
   * Delete a form response
   * Phase 6A.106: Supports both organizer and user deletion
   * - Organizer: Authenticated, no token required
   * - Anonymous user: Access token required
   * - Logged-in user: Authenticated, no token required (verified by userId)
   * Maps to backend DELETE /api/events/{id}/forms/{formId}/responses/{responseId}?token={token}
   *
   * @param eventId - Event ID (GUID)
   * @param formId - Form ID (GUID)
   * @param responseId - Response ID (GUID)
   * @param accessToken - Optional access token for anonymous users
   */
  async deleteFormResponse(
    eventId: string,
    formId: string,
    responseId: string,
    accessToken?: string
  ): Promise<void> {
    const params = accessToken ? `?token=${accessToken}` : '';
    await apiClient.delete(
      `${this.basePath}/${eventId}/forms/${formId}/responses/${responseId}${params}`
    );
  }

  /**
   * Export form responses as CSV (organizer only)
   * Custom Forms Feature: Download responses for analysis
   * Maps to backend GET /api/events/{id}/forms/{formId}/responses/export?format=csv
   *
   * @param eventId - Event ID (GUID)
   * @param formId - Form ID (GUID)
   * @param format - Export format ('csv' or 'excel')
   * @returns Blob containing the file data
   */
  async exportFormResponses(
    eventId: string,
    formId: string,
    format: 'csv' | 'excel' = 'csv'
  ): Promise<Blob> {
    // Use apiClient.get with responseType: 'blob' for file downloads
    const blob = await apiClient.get<Blob>(
      `${this.basePath}/${eventId}/forms/${formId}/responses/export?format=${format}`,
      { responseType: 'blob' as any }
    );
    return blob;
  }

  // ==================== DONATIONS ====================

  /**
   * Creates a standalone donation for an event.
   * Returns the Stripe checkout URL for payment redirect.
   */
  async createDonation(
    eventId: string,
    request: import('../types/events.types').CreateDonationRequest
  ): Promise<string> {
    return await apiClient.post<string>(
      `${this.basePath}/${eventId}/donations`,
      request
    );
  }

  /**
   * Gets all donations for an event with summary (organizer only).
   */
  async getEventDonations(
    eventId: string
  ): Promise<import('../types/events.types').EventDonationsResponse> {
    return await apiClient.get<import('../types/events.types').EventDonationsResponse>(
      `${this.basePath}/${eventId}/donations`
    );
  }

  /**
   * Gets donation summary for an event (organizer only).
   */
  async getDonationSummary(
    eventId: string
  ): Promise<import('../types/events.types').DonationSummaryDto> {
    return await apiClient.get<import('../types/events.types').DonationSummaryDto>(
      `${this.basePath}/${eventId}/donations/summary`
    );
  }

  /**
   * Gets public donation summary for an event (anyone can call).
   * Only returns data if organizer has enabled ShowDonationSummary.
   */
  async getPublicDonationSummary(
    eventId: string
  ): Promise<import('../types/events.types').PublicDonationSummaryDto | null> {
    try {
      return await apiClient.get<import('../types/events.types').PublicDonationSummaryDto>(
        `${this.basePath}/${eventId}/donations/public-summary`
      );
    } catch (error: any) {
      // 404 means donations not enabled or summary not shown
      if (error?.response?.status === 404) {
        return null;
      }
      throw error;
    }
  }

  /**
   * Gets the authenticated user's own donations for an event.
   * Returns individual donation line items.
   */
  async getMyDonations(
    eventId: string
  ): Promise<import('../types/events.types').DonationDto[]> {
    return await apiClient.get<import('../types/events.types').DonationDto[]>(
      `${this.basePath}/${eventId}/donations/mine`
    );
  }

  /**
   * Exports donations for an event in Excel or CSV format.
   */
  async exportDonations(
    eventId: string,
    format: 'csv' | 'excel' = 'excel'
  ): Promise<Blob> {
    return await apiClient.get<Blob>(
      `${this.basePath}/${eventId}/donations/export?format=${format}`,
      { responseType: 'blob' as any }
    );
  }

  // ==================== COLLECTIONS ====================

  async createCollection(eventId: string, request: import('../types/events.types').CreateCollectionRequest): Promise<string> {
    return await apiClient.post<string>(`${this.basePath}/${eventId}/collections`, request);
  }

  async getEventCollections(eventId: string): Promise<import('../types/events.types').EventCollectionsResponse> {
    return await apiClient.get<import('../types/events.types').EventCollectionsResponse>(`${this.basePath}/${eventId}/collections`);
  }

  async getCollectionSummary(eventId: string): Promise<import('../types/events.types').CollectionSummaryDto> {
    return await apiClient.get<import('../types/events.types').CollectionSummaryDto>(`${this.basePath}/${eventId}/collections/summary`);
  }

  async exportCollections(eventId: string, format: 'csv' | 'excel' = 'excel'): Promise<Blob> {
    return await apiClient.get<Blob>(
      `${this.basePath}/${eventId}/collections/export?format=${format}`,
      { responseType: 'blob' as any }
    );
  }

  async getPublicCollectionSummary(
    eventId: string
  ): Promise<import('../types/events.types').PublicCollectionSummaryDto | null> {
    try {
      return await apiClient.get<import('../types/events.types').PublicCollectionSummaryDto>(
        `${this.basePath}/${eventId}/collections/public-summary`
      );
    } catch (error: any) {
      if (error?.response?.status === 404) {
        return null;
      }
      throw error;
    }
  }

  async getMyCollections(
    eventId: string
  ): Promise<import('../types/events.types').CollectionDto[]> {
    return await apiClient.get<import('../types/events.types').CollectionDto[]>(
      `${this.basePath}/${eventId}/collections/mine`
    );
  }

  // ==================== SPONSORS ====================

  async createMoneySponsor(eventId: string, request: import('../types/events.types').CreateMoneySponsorRequest): Promise<string> {
    return await apiClient.post<string>(`${this.basePath}/${eventId}/sponsors/money`, request);
  }

  async createItemSponsor(eventId: string, request: import('../types/events.types').CreateItemSponsorRequest): Promise<string> {
    return await apiClient.post<string>(`${this.basePath}/${eventId}/sponsors/item`, request);
  }

  async getEventSponsors(eventId: string): Promise<import('../types/events.types').EventSponsorsResponse> {
    return await apiClient.get<import('../types/events.types').EventSponsorsResponse>(`${this.basePath}/${eventId}/sponsors`);
  }

  async getSponsorSummary(eventId: string): Promise<import('../types/events.types').SponsorSummaryDto> {
    return await apiClient.get<import('../types/events.types').SponsorSummaryDto>(`${this.basePath}/${eventId}/sponsors/summary`);
  }

  async exportSponsors(eventId: string, format: 'csv' | 'excel' = 'excel'): Promise<Blob> {
    return await apiClient.get<Blob>(
      `${this.basePath}/${eventId}/sponsors/export?format=${format}`,
      { responseType: 'blob' as any }
    );
  }

  async getMySponsors(
    eventId: string
  ): Promise<import('../types/events.types').SponsorDto[]> {
    return await apiClient.get<import('../types/events.types').SponsorDto[]>(
      `${this.basePath}/${eventId}/sponsors/mine`
    );
  }

  // ==================== ADD-ONS ====================

  async getAddOnDefinitions(eventId: string): Promise<import('../types/events.types').AddOnDefinitionDto[]> {
    return await apiClient.get<import('../types/events.types').AddOnDefinitionDto[]>(`${this.basePath}/${eventId}/add-ons`);
  }

  async createAddOnDefinition(eventId: string, request: import('../types/events.types').CreateAddOnDefinitionRequest): Promise<string> {
    return await apiClient.post<string>(`${this.basePath}/${eventId}/add-ons`, request);
  }

  async updateAddOnDefinition(eventId: string, definitionId: string, request: import('../types/events.types').UpdateAddOnDefinitionRequest): Promise<void> {
    return await apiClient.put<void>(`${this.basePath}/${eventId}/add-ons/${definitionId}`, request);
  }

  async purchaseAddOn(eventId: string, definitionId: string, request: import('../types/events.types').PurchaseAddOnRequest): Promise<string> {
    return await apiClient.post<string>(`${this.basePath}/${eventId}/add-ons/${definitionId}/purchase`, request);
  }

  async purchaseAddOnCart(eventId: string, request: import('../types/events.types').PurchaseAddOnCartRequest): Promise<string> {
    return await apiClient.post<string>(`${this.basePath}/${eventId}/add-ons/purchase-cart`, request);
  }

  async getEventAddOnPurchases(eventId: string): Promise<import('../types/events.types').EventAddOnPurchasesResponse> {
    return await apiClient.get<import('../types/events.types').EventAddOnPurchasesResponse>(`${this.basePath}/${eventId}/add-ons/purchases`);
  }

  async getAddOnPurchaseSummary(eventId: string): Promise<import('../types/events.types').AddOnPurchaseSummaryDto> {
    return await apiClient.get<import('../types/events.types').AddOnPurchaseSummaryDto>(`${this.basePath}/${eventId}/add-ons/purchases/summary`);
  }

  async getMyAddOnPurchases(eventId: string, email: string): Promise<import('../types/events.types').AddOnPurchaseDto[]> {
    return await apiClient.get<import('../types/events.types').AddOnPurchaseDto[]>(
      `${this.basePath}/${eventId}/add-ons/my-purchases?email=${encodeURIComponent(email)}`
    );
  }

  async getMyAddOnPurchasesMine(eventId: string): Promise<import('../types/events.types').AddOnPurchaseDto[]> {
    return await apiClient.get<import('../types/events.types').AddOnPurchaseDto[]>(
      `${this.basePath}/${eventId}/add-ons/mine`
    );
  }

  async exportAddOnPurchases(eventId: string, format: 'csv' | 'excel' = 'excel'): Promise<Blob> {
    return await apiClient.get<Blob>(
      `${this.basePath}/${eventId}/add-ons/purchases/export?format=${format}`,
      { responseType: 'blob' as any }
    );
  }

  async exportAllFinancials(eventId: string, format: 'csv' | 'excel' = 'excel'): Promise<Blob> {
    return await apiClient.get<Blob>(
      `${this.basePath}/${eventId}/export-all?format=${format}`,
      { responseType: 'blob' as any }
    );
  }

  // ==================== CONFIG UPDATES ====================

  async updateCollectionConfig(eventId: string, request: import('../types/events.types').UpdateCollectionConfigRequest): Promise<void> {
    return await apiClient.put<void>(`${this.basePath}/${eventId}/collection-config`, request);
  }

  async updateSponsorConfig(eventId: string, request: import('../types/events.types').UpdateSponsorConfigRequest): Promise<void> {
    return await apiClient.put<void>(`${this.basePath}/${eventId}/sponsor-config`, request);
  }

  async updateAddOnConfig(eventId: string, request: import('../types/events.types').UpdateAddOnConfigRequest): Promise<void> {
    return await apiClient.put<void>(`${this.basePath}/${eventId}/add-on-config`, request);
  }

  // ==================== Phase 6A.133: Co-Organizer Management ====================

  /**
   * Search registered users by name, email, or phone for co-organizer linking.
   * Returns max 10 results. Excludes the current user.
   */
  async searchUsers(query: string): Promise<UserSearchResultDto[]> {
    return await apiClient.get<UserSearchResultDto[]>(
      `/users/search?query=${encodeURIComponent(query)}`
    );
  }

}

/**
 * Singleton instance of the events repository
 * Export for use in React components and hooks
 */
export const eventsRepository = new EventsRepository();
