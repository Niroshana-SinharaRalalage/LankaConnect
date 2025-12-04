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

/**
 * Pricing type enum matching backend LankaConnect.Domain.Events.Enums.PricingType
 * Phase 6D: Tiered Group Pricing
 */
export enum PricingType {
  Single = 0,      // Flat rate per attendee
  AgeDual = 1,     // Age-based (Adult/Child)
  GroupTiered = 2, // Quantity-based with tiers
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
 * Group pricing tier DTO
 * Matches backend GroupPricingTierDto
 * Phase 6D: Tiered Group Pricing
 */
export interface GroupPricingTierDto {
  minAttendees: number;
  maxAttendees?: number | null; // Null for unlimited tier (e.g., "6+")
  pricePerPerson: number;
  currency: Currency;
  tierRange: string; // Display format: "1-2", "3-5", "6+"
}

/**
 * Main Event DTO
 * Matches backend EventDto from LankaConnect.Application.Events.Common
 * Session 21: Added dual ticket pricing support
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
  // Legacy single pricing (backward compatibility)
  ticketPriceAmount?: number | null;
  ticketPriceCurrency?: Currency | null;
  isFree: boolean;

  // Session 21: Dual ticket pricing (adult/child)
  adultPriceAmount?: number | null;
  adultPriceCurrency?: Currency | null;
  childPriceAmount?: number | null;
  childPriceCurrency?: Currency | null;
  childAgeLimit?: number | null; // Age limit for child pricing (e.g., 12 = under 12 years old)
  hasDualPricing: boolean; // True if event uses adult/child pricing

  // Phase 6D: Group tiered pricing
  pricingType?: PricingType | null; // Pricing model type (Single, AgeDual, GroupTiered)
  groupPricingTiers: readonly GroupPricingTierDto[]; // Quantity-based pricing tiers
  hasGroupPricing: boolean; // True if event uses group tiered pricing

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

// ==================== Sign-Up Management ====================

/**
 * Sign-up type enum matching backend SignUpType
 */
export enum SignUpType {
  Open = 0,
  Predefined = 1,
}

/**
 * Sign-up item category enum matching backend SignUpItemCategory
 * For category-based sign-up lists
 */
export enum SignUpItemCategory {
  Mandatory = 0,
  Preferred = 1,
  Suggested = 2,
}

/**
 * Sign-up commitment DTO
 * Represents a user's commitment to bring an item to an event
 * Phase 2: Added contact information fields
 */
export interface SignUpCommitmentDto {
  id: string;
  signUpItemId?: string | null; // Null for legacy Open sign-ups
  userId: string;
  itemDescription: string;
  quantity: number;
  committedAt: string; // ISO 8601 date-time
  notes?: string | null;

  // Phase 2: Contact information
  contactName?: string | null;
  contactEmail?: string | null;
  contactPhone?: string | null;
}

/**
 * Sign-up item DTO
 * Represents a specific item in a category-based sign-up list
 */
export interface SignUpItemDto {
  id: string;
  itemDescription: string;
  quantity: number;
  remainingQuantity: number;
  itemCategory: SignUpItemCategory;
  notes?: string | null;
  commitments: SignUpCommitmentDto[];
  isFullyCommitted: boolean;
  committedQuantity: number;
}

/**
 * Sign-up list DTO
 * Matches backend SignUpListDto - supports both legacy and category-based models
 */
export interface SignUpListDto {
  id: string;
  category: string;
  description: string;
  signUpType: SignUpType;

  // Legacy fields (for Open/Predefined sign-ups)
  predefinedItems: string[];
  commitments: SignUpCommitmentDto[];
  commitmentCount: number;

  // New category-based fields
  hasMandatoryItems: boolean;
  hasPreferredItems: boolean;
  hasSuggestedItems: boolean;
  items: SignUpItemDto[];
}

// ==================== Request DTOs ====================

/**
 * Get events query filters
 * Matches backend GetEventsQuery parameters
 *
 * Location-based sorting parameters:
 * - For authenticated users with preferred metros: userId (uses user's preferred metro areas)
 * - For authenticated users without preferences: userId (uses user's home location)
 * - For anonymous users: latitude + longitude (uses provided coordinates)
 * - For specific metro filter: metroAreaIds
 */
export interface GetEventsRequest {
  status?: EventStatus;
  category?: EventCategory;
  startDateFrom?: string; // ISO 8601 date
  startDateTo?: string; // ISO 8601 date
  isFreeOnly?: boolean;
  city?: string;
  state?: string; // NEW: State filter for location-based filtering
  userId?: string; // NEW: Authenticated user ID for location-based sorting
  latitude?: number; // NEW: Latitude for anonymous user location-based sorting
  longitude?: number; // NEW: Longitude for anonymous user location-based sorting
  metroAreaIds?: string[]; // NEW: Specific metro area IDs filter
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
 * Session 21: Added dual ticket pricing support
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

  // Ticket Price (optional - legacy single pricing for backward compatibility)
  ticketPriceAmount?: number;
  ticketPriceCurrency?: Currency;

  // Session 21: Dual ticket pricing (optional)
  adultPriceAmount?: number;
  adultPriceCurrency?: Currency;
  childPriceAmount?: number;
  childPriceCurrency?: Currency;
  childAgeLimit?: number; // Age limit for child pricing (1-18)

  // Phase 6D: Group tiered pricing (optional)
  groupPricingTiers?: GroupPricingTierRequest[];
}

/**
 * Group pricing tier request
 * Matches backend GroupPricingTierRequest
 * Phase 6D: Tiered Group Pricing
 */
export interface GroupPricingTierRequest {
  minAttendees: number;
  maxAttendees?: number | null; // Null for unlimited tier (e.g., "6+")
  pricePerPerson: number;
  currency: Currency;
}

/**
 * Update event request
 * Matches backend UpdateEventCommand signature exactly
 */
export interface UpdateEventRequest {
  eventId: string;
  title?: string;
  description?: string;
  startDate?: string;
  endDate?: string;
  capacity?: number;
  category?: EventCategory;

  // Location (with "Location" prefix to match backend)
  locationAddress?: string | null;
  locationCity?: string | null;
  locationState?: string | null;
  locationZipCode?: string | null;
  locationCountry?: string | null;
  locationLatitude?: number | null;
  locationLongitude?: number | null;

  // Pricing (nullable to match C# decimal? and Currency?)
  ticketPriceAmount?: number | null;
  ticketPriceCurrency?: Currency | null;
  // Note: isFree is NOT in backend UpdateEventCommand - backend infers it from ticketPriceAmount
}

/**
 * RSVP to event request
 * Matches backend RsvpToEventCommand for authenticated user registration
 * Session 21: Added multi-attendee support with individual names and ages
 * Session 23: Added payment redirect URLs for paid events
 */
export interface RsvpRequest {
  userId: string;

  // Legacy format (backward compatibility)
  quantity?: number; // Default: 1

  // New format (Session 21 - multi-attendee)
  attendees?: AttendeeDto[];

  // Contact information (new format only)
  email?: string;
  phoneNumber?: string;
  address?: string;

  // Session 23: Payment redirect URLs (required for paid events)
  successUrl?: string;
  cancelUrl?: string;
}

/**
 * Anonymous registration request
 * Matches backend AnonymousRegistrationRequest for unauthenticated event registration
 * Session 21: Added multi-attendee support with individual names and ages
 */
export interface AnonymousRegistrationRequest {
  // Legacy format (Session 20 - backward compatibility)
  name?: string;
  age?: number;

  // New format (Session 21 - multi-attendee)
  attendees?: AttendeeDto[];

  // Contact information (shared for all attendees)
  email: string;
  phoneNumber: string;
  address?: string;

  // Legacy quantity field (backward compatibility)
  quantity?: number; // Default: 1
}

/**
 * Session 21: Individual attendee information
 * Used for multi-attendee registration
 */
export interface AttendeeDto {
  name: string;
  age: number;
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

/**
 * Add sign-up list to event request
 */
export interface AddSignUpListRequest {
  category: string;
  description: string;
  signUpType: SignUpType;
  predefinedItems?: string[];
}

/**
 * Commit to sign-up request
 */
export interface CommitToSignUpRequest {
  userId: string;
  itemDescription: string;
  quantity: number;
}

/**
 * Cancel sign-up commitment request
 */
export interface CancelCommitmentRequest {
  userId: string;
}

/**
 * Create sign-up list with items request
 * Matches backend CreateSignUpListRequest - creates list WITH items in single API call
 */
export interface CreateSignUpListRequest {
  category: string;
  description: string;
  hasMandatoryItems: boolean;
  hasPreferredItems: boolean;
  hasSuggestedItems: boolean;
  items: SignUpItemRequestDto[];
}

/**
 * Sign-up item within CreateSignUpListRequest
 */
export interface SignUpItemRequestDto {
  itemDescription: string;
  quantity: number;
  itemCategory: SignUpItemCategory;
  notes?: string | null;
}

/**
 * Add sign-up item request
 */
export interface AddSignUpItemRequest {
  itemDescription: string;
  quantity: number;
  itemCategory: SignUpItemCategory;
  notes?: string | null;
}

/**
 * Commit to sign-up item request
 * Phase 2: Added optional contact information
 */
export interface CommitToSignUpItemRequest {
  userId: string;
  quantity: number;
  notes?: string | null;
  contactName?: string | null;
  contactEmail?: string | null;
  contactPhone?: string | null;
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
