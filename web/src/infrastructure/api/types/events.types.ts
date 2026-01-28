/**
 * Events API Type Definitions
 * DTOs matching backend API contracts (LankaConnect.Application.Events.Common)
 */

import type { EventBadgeDto } from './badges.types';

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
 * Phase 6A.81: Updated to support Three-State Registration Lifecycle for payment security
 */
export enum RegistrationStatus {
  /**
   * Phase 6A.81: NEW - Temporary state while waiting for payment confirmation
   * - Does NOT consume event capacity
   * - Does NOT block email from re-registering
   * - Auto-expires after 25 hours (Stripe checkout expires at 24h)
   * - Used for paid events only
   */
  Preliminary = 0,

  /**
   * DEPRECATED: Use Preliminary instead for backward compatibility
   */
  Pending = 1,

  /**
   * Payment completed (for paid events) OR registration completed (for free events)
   * - Consumes event capacity
   * - Blocks email from re-registering
   * - Triggers confirmation email
   */
  Confirmed = 2,

  Waitlisted = 3,
  CheckedIn = 4,

  /**
   * Event attendance completed - user attended the event
   */
  Attended = 5,

  /**
   * DEPRECATED: Use Attended instead for clarity
   * Same value as Attended for backward compatibility
   */
  Completed = 5,

  Cancelled = 6,

  /**
   * Phase 6A.81: Kept for backward compatibility with existing refunded registrations
   */
  Refunded = 7,

  /**
   * Phase 6A.81: NEW - Stripe checkout session expired or user never completed payment
   * - Does NOT consume event capacity
   * - Does NOT block email from re-registering
   * - Auto soft-deleted after 30 days for audit trail
   */
  Abandoned = 8,

  /**
   * Phase 6A.91: NEW - Refund requested but not yet completed
   * - User cancelled paid confirmed registration
   * - Stripe refund initiated, awaiting confirmation
   * - User can withdraw request to restore Confirmed status
   */
  RefundRequested = 9,
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

/**
 * Payment status enum matching backend LankaConnect.Domain.Events.Enums.PaymentStatus
 * Session 23: Stripe payment integration
 */
export enum PaymentStatus {
  Pending = 0,
  Completed = 1,
  Failed = 2,
  Refunded = 3,
  NotRequired = 4,
}

/**
 * Phase 6A.43: Age category enum matching backend LankaConnect.Domain.Events.Enums.AgeCategory
 * Used for attendee registration to distinguish adults from children
 */
export enum AgeCategory {
  Adult = 1,
  Child = 2,
}

/**
 * Phase 6A.43: Gender enum matching backend LankaConnect.Domain.Events.Enums.Gender
 * Optional field for attendee registration
 */
export enum Gender {
  Male = 1,
  Female = 2,
  Other = 3,
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
  isPrimary: boolean; // Phase 6A.13: Primary image flag
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
 * Phase 6A.X: Revenue breakdown DTO
 * Matches backend RevenueBreakdownDto
 * Shows detailed fee breakdown for paid events
 */
export interface RevenueBreakdownDto {
  /** Gross amount (ticket price) paid by buyer */
  grossAmount: number;
  /** Sales tax amount (state tax based on event location) */
  salesTaxAmount: number;
  /** Taxable amount (gross minus sales tax) */
  taxableAmount: number;
  /** Stripe payment processing fee (2.9% + $0.30) */
  stripeFeeAmount: number;
  /** Platform commission (2% of taxable amount) */
  platformCommissionAmount: number;
  /** Net amount to event organizer after all fees and taxes */
  organizerPayoutAmount: number;
  /** Currency for all amounts */
  currency: Currency;
  /** Sales tax rate as decimal (e.g., 0.0725 for 7.25%) */
  salesTaxRate: number;
  /** Display-friendly tax rate percentage (e.g., "7.25%") */
  taxRateDisplay: string;
  /** State/jurisdiction where tax was calculated */
  taxJurisdiction?: string | null;
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

  /**
   * Phase 6A.46: User-facing display label based on event lifecycle
   * Computed based on PublishedAt, StartDate, EndDate, and Status
   * Values: "New", "Upcoming", "Cancelled", "Completed", "Inactive", or status name
   */
  displayLabel: string;

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

  // Phase 6A.25: Badge overlays (optional - populated when badges are assigned)
  badges?: readonly EventBadgeDto[];

  // Phase 6A.32: Email Groups Integration
  emailGroupIds?: string[];

  // Phase 6A.X: Event Organizer Contact Details
  publishOrganizerContact: boolean;
  organizerContactName?: string | null;
  organizerContactPhone?: string | null;
  organizerContactEmail?: string | null;

  // Phase 6A.X: Revenue Breakdown for paid events
  /** Detailed fee breakdown (null for free events) */
  revenueBreakdown?: RevenueBreakdownDto | null;
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
 *
 * IMPORTANT: Uses string values to match ASP.NET Core's JsonStringEnumConverter
 * The API serializes enums as strings: "Mandatory", "Preferred", "Suggested", "Open"
 *
 * Phase 6A.27: Added Open category for user-submitted items
 * Note: Preferred is deprecated, use Suggested instead
 */
export enum SignUpItemCategory {
  Mandatory = "Mandatory",
  /** @deprecated Use Suggested instead. Preferred is being deprecated. */
  Preferred = "Preferred",
  Suggested = "Suggested",
  /** Phase 6A.27: User-submitted items - users can add their own items */
  Open = "Open",
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
 * Phase 6A.27: Enhanced with Open item support (createdByUserId, isOpenItem)
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
  /** Phase 6A.27: User ID who created this item (only for Open items) */
  createdByUserId?: string | null;
  /** Phase 6A.27: True if this is a user-submitted Open item */
  isOpenItem: boolean;
}

/**
 * Sign-up list DTO
 * Matches backend SignUpListDto - supports both legacy and category-based models
 * Phase 6A.27: Added hasOpenItems for user-submitted items
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
  /** @deprecated Use hasSuggestedItems instead. Preferred is being deprecated. */
  hasPreferredItems: boolean;
  hasSuggestedItems: boolean;
  /** Phase 6A.27: True if users can add their own Open items */
  hasOpenItems: boolean;
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
  searchTerm?: string; // Phase 6A.58: Text-based search filter
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

  // Phase 6A.32: Email Groups Integration
  emailGroupIds?: string[];
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

  // Session 21: Dual ticket pricing (optional)
  adultPriceAmount?: number | null;
  adultPriceCurrency?: Currency | null;
  childPriceAmount?: number | null;
  childPriceCurrency?: Currency | null;
  childAgeLimit?: number | null;

  // Session 33: Group tiered pricing (optional)
  groupPricingTiers?: GroupPricingTierRequest[];

  // Phase 6A.32: Email Groups Integration
  emailGroupIds?: string[];

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
 * Phase 6A.44: Added successUrl and cancelUrl for Stripe Checkout
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

  // Phase 6A.44: Stripe checkout URLs (required for paid events)
  successUrl?: string;
  cancelUrl?: string;
}

/**
 * Phase 6A.44: Response from anonymous registration
 * - For FREE events: success=true, checkoutUrl=null
 * - For PAID events: success=true, checkoutUrl=<stripe_checkout_url>
 * - For errors: success=false (shouldn't happen, API returns error status)
 */
export interface AnonymousRegistrationResponse {
  success: boolean;
  checkoutUrl: string | null;
  message: string;
}

/**
 * Session 21: Individual attendee information
 * Used for multi-attendee registration
 * Phase 6A.43: Updated to use AgeCategory and Gender instead of Age
 */
export interface AttendeeDto {
  name: string;
  ageCategory: AgeCategory;
  gender?: Gender | null;
}

/**
 * Registration details DTO with attendee information
 * Fix 1: Enhanced registration status detection
 * Matches backend RegistrationDetailsDto
 * Phase 6A.79 Part 3: .NET serializes enums as strings, not numbers
 */
export interface RegistrationDetailsDto {
  id: string;
  eventId: string;
  userId?: string | null;
  quantity: number;
  /** Phase 6A.81/6A.91: Updated with Preliminary, Abandoned, and RefundRequested states for payment security */
  status: 'Preliminary' | 'Pending' | 'Confirmed' | 'Waitlisted' | 'CheckedIn' | 'Completed' | 'Cancelled' | 'Refunded' | 'Abandoned' | 'Attended' | 'RefundRequested';  // String values from .NET API
  createdAt: string;
  updatedAt?: string | null;

  // Session 21: Multi-attendee details
  attendees: AttendeeDto[];

  // Contact information
  contactEmail?: string | null;
  contactPhone?: string | null;
  contactAddress?: string | null;

  // Payment information
  paymentStatus: 'Pending' | 'Completed' | 'Failed' | 'Refunded' | 'NotRequired';  // String values from .NET API
  totalPriceAmount?: number | null;
  totalPriceCurrency?: string | null;

  // Phase 6A.81 Part 3: Checkout session information for Preliminary registrations
  /** Stripe checkout session ID (stored in DB). Used to retrieve checkout URL from Stripe. */
  stripeCheckoutSessionId?: string | null;
  /** Stripe checkout URL for resuming payment (only for Preliminary status). Retrieved from Stripe at query time. */
  stripeCheckoutUrl?: string | null;
  /** Timestamp when the Stripe checkout session expires (24 hours from creation). Used for countdown timer in UI. */
  checkoutSessionExpiresAt?: string | null;
}

/**
 * Update RSVP request
 */
export interface UpdateRsvpRequest {
  userId: string;
  newQuantity: number;
}

/**
 * Phase 6A.14: Update registration details request
 * Allows users to edit their registration after initial RSVP
 */
export interface UpdateRegistrationRequest {
  attendees: UpdateRegistrationAttendeeDto[];
  email: string;
  phoneNumber: string;
  address?: string;
}

/**
 * Phase 6A.14: Attendee DTO for registration update
 * Phase 6A.43: Updated to use AgeCategory and Gender instead of Age
 */
export interface UpdateRegistrationAttendeeDto {
  name: string;
  ageCategory: AgeCategory;
  gender?: Gender | null;
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
 * Phase 6A.27: Added hasOpenItems for user-submitted items
 */
export interface CreateSignUpListRequest {
  category: string;
  description: string;
  hasMandatoryItems: boolean;
  /** @deprecated Use hasSuggestedItems instead. Preferred is being deprecated. */
  hasPreferredItems: boolean;
  hasSuggestedItems: boolean;
  /** Phase 6A.27: Allow users to add their own Open items */
  hasOpenItems?: boolean;
  items: SignUpItemRequestDto[];
}

/**
 * Update sign-up list request
 * Phase 6A.13: Edit Sign-Up List feature
 * Phase 6A.28: Added hasOpenItems for user-submitted items
 * Matches backend UpdateSignUpListRequest
 */
export interface UpdateSignUpListRequest {
  category: string;
  description: string;
  hasMandatoryItems: boolean;
  /** @deprecated Use hasSuggestedItems instead. Preferred is being deprecated. */
  hasPreferredItems: boolean;
  hasSuggestedItems: boolean;
  /** Phase 6A.28: Allow users to add their own Open items */
  hasOpenItems: boolean; // Made required for type safety
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
 * Update sign-up item request
 * Phase 6A.14: Edit Sign-Up Item feature
 * Matches backend UpdateSignUpItemRequest
 */
export interface UpdateSignUpItemRequest {
  itemDescription: string;
  quantity: number;
  notes?: string | null;
}

// ==================== Phase 6A.27: Open Sign-Up Items ====================

/**
 * Phase 6A.27: Add an Open sign-up item (user-submitted)
 * POST /api/events/{eventId}/signups/{signupId}/open-items
 */
export interface AddOpenSignUpItemRequest {
  /** Name of the item the user will bring */
  itemName: string;
  /** Number of items */
  quantity: number;
  /** Optional notes/description */
  notes?: string | null;
  /** Optional contact name */
  contactName?: string | null;
  /** Optional contact email */
  contactEmail?: string | null;
  /** Optional contact phone */
  contactPhone?: string | null;
}

/**
 * Phase 6A.44: Add an Open sign-up item (anonymous user version)
 * POST /api/events/{eventId}/signups/{signupId}/open-items-anonymous
 */
export interface AddOpenSignUpItemAnonymousRequest {
  /** Contact email (required for anonymous users) */
  contactEmail: string;
  /** Name of the item the user will bring */
  itemName: string;
  /** Number of items */
  quantity: number;
  /** Optional notes/description */
  notes?: string | null;
  /** Optional contact name */
  contactName?: string | null;
  /** Optional contact phone */
  contactPhone?: string | null;
}

/**
 * Phase 6A.27: Update an Open sign-up item
 * PUT /api/events/{eventId}/signups/{signupId}/open-items/{itemId}
 * Only the user who created the item can update it
 */
export interface UpdateOpenSignUpItemRequest {
  /** Updated item name */
  itemName: string;
  /** Updated quantity */
  quantity: number;
  /** Updated notes/description */
  notes?: string | null;
  /** Updated contact name */
  contactName?: string | null;
  /** Updated contact email */
  contactEmail?: string | null;
  /** Updated contact phone */
  contactPhone?: string | null;
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

/**
 * Anonymous commit to sign-up item request
 * Phase 6A.23: Supports anonymous sign-up workflow
 * Email is used to verify event registration and identify the anonymous user
 */
export interface CommitToSignUpItemAnonymousRequest {
  contactEmail: string;
  quantity: number;
  notes?: string | null;
  contactName?: string | null;
  contactPhone?: string | null;
}

/**
 * Result of checking event registration by email
 * Phase 6A.23: Enhanced to support proper UX flow for anonymous sign-up
 */
export interface EventRegistrationCheckResult {
  /** Whether the email belongs to a LankaConnect member (User account exists) */
  hasUserAccount: boolean;
  /** The UserId if the email belongs to a member */
  userId?: string | null;
  /** Whether the email is registered for this specific event */
  isRegisteredForEvent: boolean;
  /** The registration ID if registered for the event */
  registrationId?: string | null;
  /** Can proceed with anonymous commitment (NOT a member AND registered for event) */
  canCommitAnonymously: boolean;
  /** Should prompt user to log in (IS a member) */
  shouldPromptLogin: boolean;
  /** Needs to register for event first (NOT a member AND NOT registered) */
  needsEventRegistration: boolean;
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

// ==================== Ticket DTOs (Phase 6A.24) ====================

/**
 * Phase 6A.24: Ticket attendee information
 * Phase 6A.43: Updated to use AgeCategory and Gender instead of Age
 */
export interface TicketAttendeeDto {
  name: string;
  ageCategory: AgeCategory;
  gender?: Gender | null;
}

/**
 * Phase 6A.24: Event ticket DTO
 * Returned by GET /api/events/{eventId}/my-registration/ticket
 */
export interface TicketDto {
  id: string;
  registrationId: string;
  eventId: string;
  userId?: string | null;
  ticketCode: string;
  qrCodeBase64?: string | null;
  pdfBlobUrl?: string | null;
  isValid: boolean;
  validatedAt?: string | null;
  expiresAt: string;
  createdAt: string;

  // Event details for display
  eventTitle?: string | null;
  eventStartDate?: string | null;
  eventLocation?: string | null;

  // Attendee information
  attendeeCount: number;
  attendees?: TicketAttendeeDto[] | null;
}

// ==================== Phase 6A.45: Attendee Management ====================

/**
 * Phase 6A.45: Event attendee DTO (single registration)
 * Matches backend EventAttendeeDto
 */
export interface EventAttendeeDto {
  // Registration Info
  registrationId: string;
  userId?: string | null;
  status: RegistrationStatus;
  paymentStatus: PaymentStatus;
  createdAt: string;

  // Contact Info
  contactEmail: string;
  contactPhone: string;
  contactAddress?: string | null;

  // Attendee Details
  attendees: AttendeeDto[];
  totalAttendees: number;
  adultCount: number;
  childCount: number;
  genderDistribution: string;

  // Payment Info
  /** Phase 6A.71: GROSS amount (what customer paid, before commission) */
  totalAmount?: number | null;
  /** Phase 6A.71: NET amount (organizer's payout after 5% platform commission) */
  netAmount?: number | null;
  currency?: string | null;

  // Phase 6A.X: Per-registration revenue breakdown
  /** Sales tax amount for this registration */
  salesTaxAmount?: number | null;
  /** Stripe processing fee for this registration */
  stripeFeeAmount?: number | null;
  /** Platform commission for this registration */
  platformCommissionAmount?: number | null;
  /** Organizer payout for this registration */
  organizerPayoutAmount?: number | null;
  /** Sales tax rate applied to this registration */
  salesTaxRate: number;

  // Ticket Info
  ticketCode?: string | null;
  qrCodeData?: string | null;
  hasTicket: boolean;

  // Computed Properties (computed on backend)
  mainAttendeeName: string;
  additionalAttendees: string;
}

/**
 * Phase 6A.45/6A.71/6A.X: Event attendees response with commission-aware revenue
 * Matches backend EventAttendeesResponse
 */
export interface EventAttendeesResponse {
  eventId: string;
  eventTitle: string;
  attendees: EventAttendeeDto[];
  totalRegistrations: number;
  totalAttendees: number;

  // Phase 6A.71: Commission-aware revenue properties
  /** Total revenue before commission deduction */
  grossRevenue: number;
  /** Platform commission amount (LankaConnect + Stripe combined - legacy) */
  commissionAmount: number;
  /** Net revenue after commission deduction (organizer's payout) */
  netRevenue: number;
  /** Commission rate applied (e.g., 0.05 for 5%) */
  commissionRate: number;
  /** Whether this is a free event */
  isFreeEvent: boolean;

  // Phase 6A.X: Detailed revenue breakdown totals
  /** Total sales tax collected from all registrations */
  totalSalesTax: number;
  /** Total Stripe processing fees for all registrations */
  totalStripeFees: number;
  /** Total platform commission for all registrations */
  totalPlatformCommission: number;
  /** Total organizer payout after all deductions */
  totalOrganizerPayout: number;
  /** Average sales tax rate applied across registrations */
  averageTaxRate: number;
  /** Whether this event has detailed revenue breakdown data */
  hasRevenueBreakdown: boolean;

  /** @deprecated Use grossRevenue instead */
  totalRevenue?: number | null;
}

/**
 * Phase 6A.45/6A.73: Export format enum
 * Matches backend ExportFormat
 */
export enum ExportFormat {
  Excel = 0,
  Csv = 1,
  SignUpListsZip = 2,     // Phase 6A.69: ZIP archive with CSV files
  SignUpListsExcel = 3,   // Phase 6A.73: Excel file with category sheets
}

/**
 * Phase 6A.61: Event notification history DTO
 * Matches backend EventNotificationHistoryDto
 */
export interface EventNotificationHistoryDto {
  id: string;
  sentAt: string;
  sentByUserName: string;
  recipientCount: number;
  successfulSends: number;
  failedSends: number;
}

/**
 * Phase 6A.76: Event reminder history DTO
 * Matches backend EventReminderHistoryDto
 */
export interface EventReminderHistoryDto {
  reminderType: string;
  reminderTypeLabel: string;
  sentDate: string;
  recipientCount: number;
}
