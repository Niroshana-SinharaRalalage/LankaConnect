/**
 * Events API Type Definitions
 * DTOs matching backend API contracts (LankaConnect.Application.Events.Common)
 */

import type { EventBadgeDto } from './badges.types';

// ==================== Enums ====================

/**
 * Event status enum matching backend LankaConnect.Domain.Events.Enums.EventStatus.
 *
 * Phase 8YB.5 (D2=B): converted from numeric to string-valued so identity
 * comparisons against API responses (which arrive as strings via
 * JsonStringEnumConverter) work without `(event.status as any)` casts.
 *
 * Phase 8YA.1: `Planning` value added — TBD events created without dates land
 * in this state. Phase 8YB.5 lifts the manage-page Publish gate so Planning
 * events can be Published directly.
 *
 * Existing call-sites (equality compares + Record-as-object-key + Array.includes)
 * keep working: `EventStatus.Draft === event.status` is `'Draft' === 'Draft'`.
 * No reverse-lookup or arithmetic usage exists in the codebase (audited 2026-05-09).
 */
export enum EventStatus {
  Draft = 'Draft',
  Published = 'Published',
  Active = 'Active',
  Postponed = 'Postponed',
  Cancelled = 'Cancelled',
  Completed = 'Completed',
  Archived = 'Archived',
  UnderReview = 'UnderReview',
  /** Phase 8YA.1 — TBD events with no committed dates yet. */
  Planning = 'Planning',
}

/**
 * Issue #36: User-friendly status filter groups for event listing pages.
 * Maps to multiple EventStatus values internally for simplified filtering.
 *
 * Mappings:
 * - All: Everything (context-dependent: public excludes Draft/UnderReview)
 * - Active: Published + Active (upcoming/ongoing events)
 * - Inactive: Completed + Archived + Postponed (past/paused events)
 * - Cancelled: Cancelled only
 * - Unpublished: Draft + UnderReview (organizer-only visibility)
 */
export enum EventStatusFilter {
  All = 0,
  Active = 1,
  Inactive = 2,
  Cancelled = 3,
  Unpublished = 4,
}

/**
 * Issue #36: Display labels for EventStatusFilter options
 */
export const EventStatusFilterLabels: Record<EventStatusFilter, string> = {
  [EventStatusFilter.All]: 'All Events',
  [EventStatusFilter.Active]: 'Active Events',
  [EventStatusFilter.Inactive]: 'Inactive Events',
  [EventStatusFilter.Cancelled]: 'Cancelled Events',
  [EventStatusFilter.Unpublished]: 'Unpublished Events',
};

/**
 * Event category enum matching backend LankaConnect.Domain.Events.Enums.EventCategory
 * Updated to match production database reference data (12 categories)
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
  Workshop = 8,
  Festival = 9,
  Ceremony = 10,
  Celebration = 11,
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
 * Phase 7C.1: Secondary location type.
 * String-valued to match backend JsonStringEnumConverter output.
 */
export enum SecondaryLocationType {
  ParkingLot = 'ParkingLot',
  SecondaryVenue = 'SecondaryVenue',
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
 * Phase 8: Ticketing mode — determines whether event uses single pricing or multi-tier pricing.
 * Matches backend LankaConnect.Domain.Events.Enums.TicketingMode
 */
export enum TicketingMode {
  SingleTier = 'SingleTier',   // Legacy single pricing (default)
  Tiered = 'Tiered',           // Multi-tier pricing (VIP/Plus/Basic/custom)
}

/**
 * Phase 7E: Per-event registration capture mode chosen by the organiser.
 * Matches backend `LankaConnect.Domain.Events.Enums.RegistrationMode`. String-valued
 * to align with backend `JsonStringEnumConverter` (memory 6A.124 — numeric TS enums
 * compared against backend's string output silently never match).
 *
 * - DetailedAttendees (default for all pre-7E events): per-attendee Name + Age + Gender.
 * - HeadCountOnly (B1): lead name + total head count.
 * - HeadCountByAge (B2): lead + Adults + Children (Total auto-derived).
 * - HeadCountByGender (B3): lead + Males + Females (Total auto-derived).
 * - HeadCountByAgeAndGender (B4): lead + 4 leaf counts (AM/AF/CM/CF; Total auto-derived).
 * - NoRegistration (C): drop-in event; standalone donations/sponsors/add-ons/collections still work.
 */
export enum RegistrationMode {
  DetailedAttendees = 'DetailedAttendees',
  HeadCountOnly = 'HeadCountOnly',
  HeadCountByAge = 'HeadCountByAge',
  HeadCountByGender = 'HeadCountByGender',
  HeadCountByAgeAndGender = 'HeadCountByAgeAndGender',
  NoRegistration = 'NoRegistration',
  /** Phase 8X.11 — registration captured externally (Eventbrite, cash-at-door, etc.).
   * Only allowed when paymentMode === ExternalPaid. */
  External = 'External',
}

/**
 * Phase 8X: Event payment mode (Free / OnPlatformPaid / ExternalPaid).
 * Matches backend `LankaConnect.Domain.Events.Enums.EventPaymentMode`. String-valued
 * to align with backend `JsonStringEnumConverter` (memory 6A.124 — numeric TS enums
 * compared against backend's string output silently never match).
 *
 * - Free: free event (no pricing).
 * - OnPlatformPaid: paid event with payment collected on LankaConnect via Stripe.
 * - ExternalPaid: paid event whose payment + registration happens off-platform.
 *   Pricing is displayed; in-page CTA links to organiser-supplied URL with optional
 *   vendor name and instructions. No internal Registration row is created.
 */
export enum EventPaymentMode {
  Free = 'Free',
  OnPlatformPaid = 'OnPlatformPaid',
  ExternalPaid = 'ExternalPaid',
}

/**
 * Phase 8: Ticket category for multi-tier ticket generation.
 * Matches backend LankaConnect.Domain.Events.Enums.TicketCategory
 */
export enum TicketCategory {
  Standard = 'Standard',     // Legacy single ticket per registration
  Master = 'Master',         // Group check-in ticket (one per tier group)
  Individual = 'Individual', // Per-attendee ticket
}

/**
 * Phase 8: Ticket tier DTO — represents a pricing tier (VIP, Plus, Basic, custom).
 * Matches backend LankaConnect.Application.Events.Common.TicketTierDto
 */
export interface TicketTierDto {
  id: string;
  name: string;
  description?: string | null;
  adultPriceAmount: number;
  adultPriceCurrency: Currency;
  childPriceAmount?: number | null;
  childPriceCurrency?: Currency | null;
  childAgeLimit?: number | null;
  hasChildPricing: boolean;
  capacity: number;
  reservedCount: number;
  availableQuantity: number;
  maxPerUser: number;
  sortOrder: number;
  isActive: boolean;
  isFree: boolean;
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
 * Organizer contact DTO - supports multiple contacts per event
 */
export interface OrganizerContactDto {
  id: string;
  contactName: string;
  contactEmail?: string | null;
  contactPhone?: string | null;
  isPrimary: boolean;
  sortOrder: number;
  /** Phase 6A.133: Linked user ID for co-organizer management */
  linkedUserId?: string | null;
}

/**
 * Phase 6A.133: Minimal user DTO for co-organizer search results.
 */
export interface UserSearchResultDto {
  id: string;
  displayName: string;
  email: string;
  profilePhotoUrl?: string | null;
}

/**
 * Request model for creating/updating an organizer contact
 */
export interface OrganizerContactRequest {
  contactName: string;
  contactEmail?: string | null;
  contactPhone?: string | null;
  isPrimary?: boolean;
  linkedUserId?: string | null;
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
  // Phase 8YA.3: Null on TBD events (Status === Planning, or rare Published-with-TBD
  // per Q1=A). Display surfaces must render a "Date TBD" badge when null.
  startDate: string | null; // ISO 8601 date-time, or null for TBD events
  endDate: string | null;   // ISO 8601 date-time, or null for TBD events
  organizerId: string;
  capacity: number;
  currentRegistrations: number;
  /**
   * Issue #51: Maximum attendees allowed per single registration
   * Configurable by event organizer (default: 10, max: 50)
   */
  maxAttendeesPerRegistration: number;

  /**
   * Phase 7E: Per-event registration capture mode chosen by the organiser.
   * Defaults to `DetailedAttendees` for legacy events (DB-level DEFAULT 0). Consumers MUST
   * use the nullish-coalesce default `event.registrationMode ?? RegistrationMode.DetailedAttendees`
   * to tolerate stale React Query cached payloads from before deploy.
   */
  registrationMode?: RegistrationMode;

  /**
   * Phase 7E paid-B-mode gate (review iteration 1, 2026-04-28): tells the UI whether the
   * configured `registrationMode` is currently implementable.
   * - `'active'` — configured mode passes the compatibility validator AND is shipped; render the proper RSVP form.
   * - `'deferred'` — configured mode is fine per the target-state plan but the implementation
   *   slice hasn't shipped yet (today: paid + B-mode, awaiting Phase 7E.3b). Render a read-only
   *   "coming soon — contact organiser" panel instead of a fillable form.
   * Optional + defaults to `'deferred'` (fail-safe) so any pre-fix cached payload doesn't
   * accidentally render a form for a known-broken combination.
   */
  registrationModeStatus?: 'active' | 'deferred';

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

  // Phase 7C.1: Primary venue name (distinct from street address)
  locationName?: string | null;

  // Phase 7C.1: Secondary location (parking lot or secondary venue)
  secondaryLocationType?: SecondaryLocationType | null;
  secondaryLocationName?: string | null;
  secondaryAddress?: string | null;
  secondaryCity?: string | null;
  secondaryState?: string | null;
  secondaryZipCode?: string | null;
  secondaryCountry?: string | null;
  secondaryLatitude?: number | null;
  secondaryLongitude?: number | null;
  hasSecondaryLocation: boolean;

  /**
   * Phase 6A.97: IANA timezone identifier for consistent date/time display
   * Example: "America/New_York", "America/Los_Angeles"
   * Derived from event's state location
   */
  timeZoneId?: string | null;

  /**
   * Phase 6A.97: Timezone abbreviation for display (e.g., "EST", "PST")
   * Accounts for Daylight Saving Time
   */
  timeZoneAbbreviation?: string | null;

  // Ticket pricing (nullable - free events)
  // Legacy single pricing (backward compatibility)
  ticketPriceAmount?: number | null;
  ticketPriceCurrency?: Currency | null;
  isFree: boolean;

  /**
   * Phase 8X: Source of truth for payment mode. Defaults to Free for stale-cache
   * back-compat with FE bundles cached before Phase 8X.5 shipped — those will
   * fall back to "paid event" rendering for ExternalPaid (acceptable degradation).
   * Use this instead of `isFree` to decide between Register/RSVP CTA and the
   * external "Buy Ticket" link.
   */
  paymentMode?: EventPaymentMode;
  externalRegistrationUrl?: string | null;
  externalRegistrationInstructions?: string | null;
  externalRegistrationVendorName?: string | null;

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

  // Phase 2: Seating
  seatingMode?: SeatingMode;
  venueLayoutId?: string | null;

  // Phase 8: Multi-tier ticketing
  ticketingMode: TicketingMode;
  ticketTiers: readonly TicketTierDto[];
  hasTicketTiers: boolean;

  // Media galleries (Epic 2 Phase 2)
  images: readonly EventImageDto[];
  videos: readonly EventVideoDto[];

  // Phase 6A.25: Badge overlays (optional - populated when badges are assigned)
  badges?: readonly EventBadgeDto[];

  // Phase 6A.32: Email Groups Integration
  emailGroupIds?: string[];

  // Organizer Contact Details (supports multiple contacts)
  publishOrganizerContact: boolean;
  organizerContacts?: OrganizerContactDto[];

  // Phase 6A.X: Revenue Breakdown for paid events
  /** Detailed fee breakdown (null for free events) */
  revenueBreakdown?: RevenueBreakdownDto | null;

  // Donation Feature: Donation configuration
  donationConfig?: DonationConfigurationDto | null;
  collectionConfig?: CollectionConfigurationDto | null;
  sponsorConfig?: SponsorConfigurationDto | null;
  addOnConfig?: AddOnConfigurationDto | null;

  /**
   * Issue #2: User's registration status for this event (if user is registered)
   * Only populated for authenticated queries like /my-rsvps
   * Null if user is not registered or for public event listings
   * Used to show accurate "You are registered" badge (only for Confirmed status)
   */
  userRegistrationStatus?: RegistrationStatus | null;

  /**
   * Phase 6A.133: Server-computed organizer check for the current user.
   * null/undefined = unauthenticated, true = user is primary or co-organizer, false = not an organizer.
   * Frontend uses this instead of comparing organizerId === userId client-side.
   */
  isCurrentUserOrganizer?: boolean | null;
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
 * Result of a cancellation operation, including details about optional actions.
 * Enables the frontend to show what succeeded and what failed.
 */
export interface CancelRsvpResult {
  registrationCancelled: boolean;
  commitmentsDeleted?: boolean | null;
  formResponsesDeleted?: boolean | null;
  formResponsesDeletedCount?: number | null;
  addOnRefundsProcessed?: boolean | null;
  addOnRefundedCount?: number | null;
  addOnFailedCount?: number | null;
  addOnRefundTotal?: number | null;
  // Phase 6A.137F: Collection and sponsor refund results
  collectionRefundProcessed?: boolean | null;
  collectionRefundAmount?: number | null;
  sponsorRefundProcessed?: boolean | null;
  sponsorRefundAmount?: number | null;
  warnings?: string[] | null;
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
  /** @deprecated Use physicalQuantity or slotsClaimed instead. Will be removed in Phase 7. */
  quantity: number;
  /** Phase 6A.121: For quantity-based commitments (e.g., "5 plates") */
  physicalQuantity?: number | null;
  /** Phase 6A.121: For slot-based commitments (e.g., "2 slots") */
  slotsClaimed?: number | null;
  committedAt: string; // ISO 8601 date-time
  notes?: string | null;

  // Phase 2: Contact information
  contactName?: string | null;
  contactEmail?: string | null;
  contactPhone?: string | null;
}

/**
 * Phase 6A.121: Enum for sign-up item type discriminator
 */
/**
 * Phase 6A.124: String values match the API's JsonStringEnumConverter serialization.
 * Backend uses JsonStringEnumConverter globally, so enums are returned as strings
 * (e.g. "Quantity" not 0). Type guards use === comparison so values must match exactly.
 */
export enum SignUpItemType {
  Quantity = 'Quantity',
  Slot = 'Slot',
}

/**
 * Phase 7D.1: Discriminator for the kind of sign-up list.
 * - `Items` — traditional bring-an-item lists (e.g. potluck dishes).
 * - `Volunteers` — volunteer-role lists (e.g. food committee, setup crew).
 *
 * String values match the backend's JsonStringEnumConverter output so JSON
 * round-trips work without a numeric-to-string shim (MEMORY 6A.124).
 */
export enum SignUpKind {
  Items = 'Items',
  Volunteers = 'Volunteers',
}

/**
 * Phase 6A.121: Base interface for discriminated union of sign-up item DTOs
 */
interface SignUpItemDtoBase {
  id: string;
  itemDescription: string;
  itemCategory: SignUpItemCategory;
  notes?: string | null;
  commitments: SignUpCommitmentDto[];
  isFullyCommitted: boolean;
  isOpenItem: boolean;
  /** Phase 6A.27: User ID who created this item (only for Open items) */
  createdByUserId?: string | null;
  /** Phase 6A.132: Render order within the list (0-based, ascending). */
  displayOrder: number;
}

/**
 * Phase 6A.121: DTO for quantity-based signup items
 * Example: "Rice - 10 plates"
 */
export interface QuantityBasedItemDto extends SignUpItemDtoBase {
  itemType: SignUpItemType.Quantity;
  targetQuantity: number;
  committedQuantity: number;
  remainingQuantity: number;
}

/**
 * Phase 6A.121: DTO for slot-based signup items
 * Example: "Assorted Fruits - 3 slots"
 */
export interface SlotBasedItemDto extends SignUpItemDtoBase {
  itemType: SignUpItemType.Slot;
  totalSlots: number;
  filledSlots: number;
  remainingSlots: number;
  suggestedQuantityPerSlot?: number | null;
  estimatedTotalQuantity?: number | null;
}

/**
 * Phase 6A.121: Discriminated union of sign-up item DTOs
 * Use type guards isQuantityBased() / isSlotBased() to narrow the type
 */
export type SignUpItemDto = QuantityBasedItemDto | SlotBasedItemDto;

/**
 * Phase 6A.121: Type guard for quantity-based items
 */
export function isQuantityBased(item: SignUpItemDto): item is QuantityBasedItemDto {
  return item.itemType === SignUpItemType.Quantity;
}

/**
 * Phase 6A.121: Type guard for slot-based items
 */
export function isSlotBased(item: SignUpItemDto): item is SlotBasedItemDto {
  return item.itemType === SignUpItemType.Slot;
}

/**
 * Sign-up list DTO
 * Matches backend SignUpListDto - supports both legacy and category-based models
 * Phase 6A.27: Added hasOpenItems for user-submitted items
 * Phase 6A.121: Items now use discriminated union (SignUpItemDto = QuantityBasedItemDto | SlotBasedItemDto)
 */
export interface SignUpListDto {
  id: string;
  category: string;
  description: string;
  signUpType: SignUpType;
  /**
   * Phase 7D.1: Discriminator between bring-an-item lists and volunteer-role lists.
   * Optional for backward compatibility with any cached payloads that predate the
   * Phase A backend; consumers should default missing values to `SignUpKind.Items`.
   */
  kind?: SignUpKind;

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
 *
 * Issue #36: Status filtering options:
 * - status: Single specific status filter (legacy, backward compatible)
 * - statusFilter: User-friendly status group filter (Active, Inactive, Cancelled, etc.)
 * - statusFilter takes precedence over status when both provided
 */
export interface GetEventsRequest {
  status?: EventStatus;
  /** Issue #36: User-friendly status filter (takes precedence over status) */
  statusFilter?: EventStatusFilter;
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
  /** Issue #36: When true, includes Draft/UnderReview events (organizer view) */
  includeAllStatuses?: boolean;
}

/**
 * Search events request with pagination
 * Phase 6A.X Issue #36: Added excludeCancelled parameter to filter out cancelled events
 */
export interface SearchEventsRequest {
  searchTerm: string;
  page?: number;
  pageSize?: number;
  category?: EventCategory;
  isFreeOnly?: boolean;
  startDateFrom?: string;
  excludeCancelled?: boolean;
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
  // Phase 8YA.3: Both null -> backend creates a TBD event in Planning status.
  // Both set -> Draft. Mixed (one null, one set) -> backend validator returns 400.
  startDate: string | null;
  endDate: string | null;
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

  // Phase 7C.1: Primary venue name (optional)
  locationName?: string;

  // Phase 7C.1: Secondary location (optional - all fields required when type is set)
  secondaryLocationType?: SecondaryLocationType;
  secondaryLocationName?: string;
  secondaryLocationAddress?: string;
  secondaryLocationCity?: string;
  secondaryLocationState?: string;
  secondaryLocationZipCode?: string;
  secondaryLocationCountry?: string;
  secondaryLocationLatitude?: number;
  secondaryLocationLongitude?: number;

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

  // IsFreeEvent fix: Explicit free event flag
  isFree?: boolean;

  // Phase 8X: Payment mode (Free / OnPlatformPaid / ExternalPaid). Optional on the wire
  // — backend infers from isFree per the architect-locked inference table when absent.
  // Required when ExternalPaid; the validator returns 400 if URL is missing or insecure.
  paymentMode?: EventPaymentMode;
  externalRegistrationUrl?: string;
  externalRegistrationInstructions?: string;
  externalRegistrationVendorName?: string;

  // Phase 7E: Per-event registration capture mode. Optional on the wire — backend defaults
  // to DetailedAttendees when absent for back-compat with pre-7E API clients.
  registrationMode?: RegistrationMode;

  // Donation Feature: Donation configuration
  donationsEnabled?: boolean;
  donationSuggestedAmounts?: number[];
  donationAllowCustomAmount?: boolean;
  donationMinAmount?: number | null;
  donationMaxAmount?: number | null;
  donationMessage?: string | null;
  showDonationSummary?: boolean;

  // Phase 6A.132: Multiple organizer contacts
  publishOrganizerContact?: boolean;
  organizerContacts?: OrganizerContactRequest[];
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
  // Phase 8YA.3: Both null -> backend keeps existing dates unchanged (organiser
  // editing other fields). Both set -> SetDates path. Mixed -> backend validator
  // returns 400.
  startDate?: string | null;
  endDate?: string | null;
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

  // Phase 7C.1: Primary venue name (optional; null clears)
  locationName?: string | null;

  // Phase 7C.1: Secondary location (null type clears entire secondary location)
  secondaryLocationType?: SecondaryLocationType | null;
  secondaryLocationName?: string | null;
  secondaryLocationAddress?: string | null;
  secondaryLocationCity?: string | null;
  secondaryLocationState?: string | null;
  secondaryLocationZipCode?: string | null;
  secondaryLocationCountry?: string | null;
  secondaryLocationLatitude?: number | null;
  secondaryLocationLongitude?: number | null;

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

  // IsFreeEvent fix: Explicit free event flag
  isFree?: boolean;

  // Phase 8X: Payment mode (Free / OnPlatformPaid / ExternalPaid). Optional on the wire
  // — backend infers from isFree per the architect-locked inference table when absent.
  // Required when ExternalPaid; the validator returns 400 if URL is missing or insecure.
  paymentMode?: EventPaymentMode;
  externalRegistrationUrl?: string;
  externalRegistrationInstructions?: string;
  externalRegistrationVendorName?: string;

  // Phase 7E: Per-event registration capture mode. Optional on the wire — backend defaults
  // to DetailedAttendees when absent for back-compat with pre-7E API clients.
  registrationMode?: RegistrationMode;

  // Donation Feature: Donation configuration
  donationsEnabled?: boolean;
  donationSuggestedAmounts?: number[];
  donationAllowCustomAmount?: boolean;
  donationMinAmount?: number | null;
  donationMaxAmount?: number | null;
  donationMessage?: string | null;
  showDonationSummary?: boolean;

  // Phase 6A.132: Multiple organizer contacts
  publishOrganizerContact?: boolean;
  organizerContacts?: OrganizerContactRequest[];
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

  // Phase 7A.6B: WhatsApp opt-in during event registration
  whatsAppPhoneNumber?: string;

  // Session 23: Payment redirect URLs (required for paid events)
  successUrl?: string;
  cancelUrl?: string;

  // Donation Feature: Optional donation during registration
  donationAmount?: number | null;
  donorName?: string | null;
  donorPhone?: string | null;
  donorNotes?: string | null;

  // Phase 6A.137D: Add-ons bundled with registration checkout
  addOnSelections?: AddOnSelectionRequest[];

  // Phase 6A.137E: Collection (event fund) contribution during registration
  collectionAmount?: number | null;
  collectionNotes?: string | null;

  // Phase 6A.137E: Money sponsorship during registration
  sponsorAmount?: number | null;
  sponsorOrganization?: string | null;
  sponsorNotes?: string | null;

  // Phase 2: Assigned seating — seat hold session
  seatSessionId?: string;
  seatIds?: string[];

  // Phase 7E.3a: Head-count payload for Mode B events. Mutually exclusive with `attendees`.
  // Backend dispatches by event.RegistrationMode and rejects mismatched shapes with 400.
  leadAttendeeName?: string;
  headCount?: HeadCountDto;
}

/**
 * Phase 7E.3a — head-count payload for Mode B (B1-B4) RSVPs. The backend's mode-specific
 * factory validates which fields are required:
 * - HeadCountOnly (B1): `total` required.
 * - HeadCountByAge (B2): `adults` + `children` required (Total auto-derived).
 * - HeadCountByGender (B3): `males` + `females` required (Total auto-derived).
 * - HeadCountByAgeAndGender (B4): all four leaf counts required.
 * - `tierCounts` is required iff the event has ticket tiers configured (7E.3c).
 */
export interface HeadCountDto {
  total?: number;
  adults?: number;
  children?: number;
  males?: number;
  females?: number;
  adultMales?: number;
  adultFemales?: number;
  childMales?: number;
  childFemales?: number;
  tierCounts?: TierCountDto[];
}

/**
 * Phase 7E.3c — per-tier count for a registration. `tierName` is resolved server-side from
 * `tierId` and snapshotted onto the registration; client supplies `tierId` + `count` only.
 *
 * Phase 7F-C (architect-approved 2026-04-30): optional `adultCount` + `childCount` per-tier-
 * by-age axis. Used in B2 / B4 modes with tiered pricing when the user opts into per-tier-by-
 * age billing (adults pay `tier.AdultPrice`, children pay `tier.ChildPrice`). Domain invariant:
 * both fields set or both null (half-set is rejected); when set, sum must equal `count`.
 */
export interface TierCountDto {
  tierId: string;
  count: number;
  /**
   * Phase 7F-E.7 (architect-approved 2026-05-04, re-opens §2.2 #4 deferred decision):
   * optional per-tier 4-leaf demographic split. All-or-nothing per tier (any of 4 set
   * → all 4 must be set; sum equals count). When set on a B4-mode + tiered registration,
   * the per-tier rows of the breakdown card render captured 4-leaf instead of N/A.
   */
  adultMaleCount?: number;
  adultFemaleCount?: number;
  childMaleCount?: number;
  childFemaleCount?: number;
  adultCount?: number;
  childCount?: number;
}

/**
 * Phase 6A.137D: Add-on selection during registration.
 * Matches backend AddOnSelectionDto.
 */
export interface AddOnSelectionRequest {
  definitionId: string;
  quantity: number;
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

  // Phase 7A.6B: WhatsApp opt-in during event registration
  whatsAppPhoneNumber?: string;

  // Legacy quantity field (backward compatibility)
  quantity?: number; // Default: 1

  // Phase 6A.44: Stripe checkout URLs (required for paid events)
  successUrl?: string;
  cancelUrl?: string;

  // Donation Feature: Optional donation during registration
  donationAmount?: number | null;
  donorName?: string | null;
  donorPhone?: string | null;
  donorNotes?: string | null;

  // Phase 7E.3a: Head-count payload for Mode B (anonymous flow). Mutually exclusive with `attendees`.
  leadAttendeeName?: string;
  headCount?: HeadCountDto;
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
  // Phase 8: Optional ticket tier assignment for tiered events
  ticketTierId?: string | null;
  // Phase 6A.161: Denormalized tier name for display (e.g. "VIP"). Null for
  // single-tier/free/legacy registrations.
  ticketTierName?: string | null;
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

  // Phase 6A.137F-Fix: Financial breakdown for bundled checkout items
  donationAmount?: number | null;
  addOnTotal?: number | null;
  collectionTotal?: number | null;
  sponsorTotal?: number | null;
  /** Grand total = totalPriceAmount (tickets) + donationAmount + addOnTotal + collectionTotal + sponsorTotal */
  grandTotal?: number | null;

  /**
   * Phase 7F-E.2 (architect-approved 2026-05-01): mode-aware fields so the FE event-
   * detail card renders Mode A (DetailedAttendees) and Mode B (B1/B2/B3/B4)
   * registrations through one consistent shape.
   */
  registrationMode?: RegistrationMode;
  /** Mode B only — null on Mode A (Mode A uses the per-attendee `attendees` list). */
  leadAttendeeName?: string | null;
  /** Server-projected per-tier breakdown. Null only when registration has neither
   * a HeadCount nor any Attendees (defensive). */
  breakdown?: RegistrationBreakdownDto | null;
}

/**
 * Phase 7F-E.1: shared cross-surface projection. Shape mirrors the backend
 * `RegistrationBreakdown` record. Each row represents one tier (or the whole
 * registration when non-tiered). `BreakdownPair.captured = false` → renderer shows "N/A".
 */
export interface RegistrationBreakdownDto {
  rows: RegistrationBreakdownRowDto[];
  totalAttendees: number;
  mode: RegistrationMode;
  isTiered: boolean;
  /**
   * Phase 7F-E.6.A: registration-level demographics surfaced for multi-tier B-mode
   * breakdowns (per-tier rows can't carry them per architect Phase 7F-C §2.2 #4
   * deferred per-tier-gender storage). Null when not multi-tier OR when no
   * demographic axis was captured at registration level.
   */
  totals?: RegistrationBreakdownTotalsDto | null;
}

export interface RegistrationBreakdownRowDto {
  /** null = non-tiered */
  tierName: string | null;
  count: number;
  age: BreakdownPairDto;
  gender: BreakdownPairDto;
}

/** Phase 7F-E.6.A: paired demographic pairs only — count + tier list live on the parent. */
export interface RegistrationBreakdownTotalsDto {
  age: BreakdownPairDto;
  gender: BreakdownPairDto;
}

export interface BreakdownPairDto {
  captured: boolean;
  left: number;
  right: number;
  leftLabel: string;   // "Adult" / "Male"
  rightLabel: string;  // "Child" / "Female"
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
  /**
   * Phase 7D.1: Discriminator for volunteer-role lists vs item lists.
   * Optional; backend defaults to `SignUpKind.Items` when absent so existing
   * create-signup-list flows remain unchanged.
   */
  kind?: SignUpKind;
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
 * Phase 6A.131: Updated to support both quantity-based and slot-based items
 */
export interface SignUpItemRequestDto {
  itemDescription: string;
  itemType: SignUpItemType;
  itemCategory: SignUpItemCategory;
  targetQuantity?: number | null;
  availableSlots?: number | null;
  suggestedPerSlot?: number | null;
  notes?: string | null;
}

/**
 * Add sign-up item request
 * Phase 6A.121: Unified request supporting both quantity-based and slot-based items
 */
export interface AddSignUpItemRequest {
  itemDescription: string;
  itemType: SignUpItemType;        // Discriminator: Quantity or Slot
  itemCategory: SignUpItemCategory;
  targetQuantity?: number | null;  // For quantity-based items (e.g., 10 plates)
  availableSlots?: number | null;  // For slot-based items (e.g., 3 slots)
  suggestedPerSlot?: number | null; // Optional: suggested quantity per slot
  notes?: string | null;
}

/**
 * Update sign-up item request.
 * Phase 6A.14: Edit Sign-Up Item feature.
 * Phase 6A.131: Supports both quantity-based and slot-based items.
 *
 * Send `targetQuantity` for quantity-based items; send `availableSlots` (and optionally
 * `suggestedPerSlot`) for slot-based items. The server uses the loaded item's type as the
 * authority — sending the wrong field returns HTTP 400 with an explicit message.
 */
export interface UpdateSignUpItemRequest {
  itemDescription: string;
  targetQuantity?: number | null;
  availableSlots?: number | null;
  suggestedPerSlot?: number | null;
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
  // Phase 6A.161: Registration-level ticket-tier summary computed on backend.
  // Single name when uniform ("VIP"), comma-joined when mixed ("VIP, General"),
  // "—" when no attendee carries a tier. Never null/blank.
  ticketTierSummary?: string | null;
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

// ==================== Add-Only Attendees with Delta Payment ====================

/**
 * Add-Only Attendees: Status of a registration addition
 * Matches backend RegistrationAdditionStatus enum
 */
export type RegistrationAdditionStatus = 'Pending' | 'PaymentCompleted' | 'Merged' | 'Failed' | 'Abandoned';

/**
 * Add-Only Attendees: New attendee to be added
 * Used in calculate-addition and add-attendees requests
 */
export interface NewAttendeeDto {
  name: string;
  ageCategory: AgeCategory;
  gender?: Gender | null;
}

/**
 * Add-Only Attendees: Request to calculate addition price
 * POST /api/events/registrations/{registrationId}/calculate-addition
 */
export interface CalculateAdditionPriceRequest {
  newAttendees: NewAttendeeDto[];
}

/**
 * Add-Only Attendees: Attendee price breakdown
 */
export interface AttendeePrice {
  name: string;
  ageCategory: AgeCategory;
  price: number;
}

/**
 * Add-Only Attendees: Response from calculate-addition endpoint
 */
export interface AdditionPriceResultDto {
  registrationId: string;
  eventId: string;
  eventTitle: string;
  currentAttendeeCount: number;
  newAttendeesCount: number;
  totalAttendeeCount: number;
  maxAttendeesPerRegistration: number;
  currentTotalPaid: number;
  newTotalPrice: number;
  additionalAmount: number;
  currency: string;
  isValid: boolean;
  errorMessage?: string | null;
  hasPendingAddition: boolean;
  attendeeBreakdown: AttendeePrice[];
  remainingCapacity?: number | null;
}

/**
 * Add-Only Attendees: Request to initiate adding attendees
 * POST /api/events/registrations/{registrationId}/add-attendees
 */
export interface InitiateAddAttendeesRequest {
  newAttendees: NewAttendeeDto[];
  successUrl: string;
  cancelUrl: string;
}

/**
 * Add-Only Attendees: Response from add-attendees endpoint
 */
export interface InitiateAddAttendeesResult {
  success: boolean;
  errorMessage?: string | null;
  registrationAdditionId?: string | null;
  checkoutSessionId?: string | null;
  checkoutUrl?: string | null;
  expiresAt?: string | null;
  additionalAmount: number;
  currency: string;
  newAttendeesCount: number;
}

/**
 * Add-Only Attendees: Pending attendee in an addition
 */
export interface PendingAttendeeDto {
  name: string;
  ageCategory: AgeCategory | string;
  gender?: Gender | null;
}

/**
 * Add-Only Attendees: Pending addition details
 * GET /api/events/registrations/{registrationId}/pending-addition
 */
export interface PendingAdditionDto {
  id: string;
  registrationId: string;
  eventId: string;
  status: RegistrationAdditionStatus;
  newAttendees: PendingAttendeeDto[];
  additionalAmount: number;
  currency: string;
  checkoutSessionId?: string | null;
  checkoutUrl?: string | null;
  expiresAt?: string | null;
  createdAt: string;
}

/**
 * Add-Only Attendees: Response from cancel-pending-addition endpoint
 * DELETE /api/events/registrations/{registrationId}/pending-addition
 */
export interface CancelPendingAdditionResult {
  success: boolean;
  errorMessage?: string | null;
  cancelledAdditionId?: string | null;
}

// ==================== Custom Forms (Survey/Form Sign-Up Type) ====================

/**
 * Event form status enum matching backend LankaConnect.Domain.Events.Enums.EventFormStatus
 * Lifecycle: Draft -> Active -> Closed -> Archived
 */
export enum EventFormStatus {
  Draft = 0,
  Active = 1,
  Closed = 2,
  Archived = 3,
}

/**
 * Display labels for EventFormStatus
 */
export const EventFormStatusLabels: Record<EventFormStatus, string> = {
  [EventFormStatus.Draft]: 'Draft',
  [EventFormStatus.Active]: 'Active',
  [EventFormStatus.Closed]: 'Closed',
  [EventFormStatus.Archived]: 'Archived',
};

/**
 * Form question type enum matching backend LankaConnect.Domain.Events.Enums.FormQuestionType
 */
export enum FormQuestionType {
  ShortText = 0,
  LongText = 1,
  SingleChoice = 2,
  MultipleChoice = 3,
  Dropdown = 4,
  Number = 5,
  Date = 6,
  YesNo = 7,
}

/**
 * Display labels for FormQuestionType
 */
export const FormQuestionTypeLabels: Record<FormQuestionType, string> = {
  [FormQuestionType.ShortText]: 'Short Text',
  [FormQuestionType.LongText]: 'Long Text',
  [FormQuestionType.SingleChoice]: 'Single Choice',
  [FormQuestionType.MultipleChoice]: 'Multiple Choice',
  [FormQuestionType.Dropdown]: 'Dropdown',
  [FormQuestionType.Number]: 'Number',
  [FormQuestionType.Date]: 'Date',
  [FormQuestionType.YesNo]: 'Yes/No',
};

/**
 * Question option DTO matching backend QuestionOptionDto
 */
export interface QuestionOptionDto {
  id: string;
  text: string;
  sortOrder: number;
}

/**
 * Form question DTO matching backend FormQuestionDto
 */
export interface FormQuestionDto {
  id: string;
  questionText: string;
  questionType: FormQuestionType | string;
  isRequired: boolean;
  sortOrder: number;
  helpText?: string | null;
  options: QuestionOptionDto[];
}

/**
 * Event form summary DTO matching backend EventFormDto
 * Used in list views
 */
export interface EventFormDto {
  id: string;
  eventId: string;
  title: string;
  description?: string | null;
  status: EventFormStatus | string;
  allowMultipleResponses: boolean;
  responseDeadline?: string | null;
  maxResponses?: number | null;
  hasResponses: boolean;
  responseCount: number;
  createdAt: string;
  updatedAt: string;
  /**
   * Phase 6A.146 — organizer-controlled toggle. When true, the
   * PublicFormResponsesSection renders on the event detail page (subject
   * to status ∈ {Active, Closed}) and the /responses/public endpoint
   * returns PII-redacted DTOs.
   */
  allowAttendeesToViewResponses: boolean;
}

/**
 * Event form detail DTO matching backend EventFormDetailDto
 * Includes questions
 */
export interface EventFormDetailDto extends EventFormDto {
  questions: FormQuestionDto[];
}

/**
 * Form answer DTO matching backend FormAnswerDto
 */
export interface FormAnswerDto {
  id: string;
  formQuestionId: string;
  questionTextSnapshot: string;
  textValue?: string | null;
  selectedOptionIds: string[];
  selectedOptionTextSnapshots: string[];
  booleanValue?: boolean | null;
}

/**
 * Form response DTO matching backend FormResponseDto
 */
export interface FormResponseDto {
  id: string;
  eventFormId: string;
  respondentName?: string | null;
  respondentEmail?: string | null;
  respondentUserId?: string | null;
  submittedAt: string;
  answers: FormAnswerDto[];
}

/**
 * Paginated form responses DTO matching backend FormResponsesPagedDto
 */
export interface FormResponsesPagedDto {
  responses: FormResponseDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// ==================== Phase 6A.146 — Public Form Responses (PII-redacted) ====================

/**
 * Phase 6A.146 — public answer DTO matching backend PublicFormAnswerDto.
 * Question text + answer values are preserved verbatim; organizer is
 * responsible for not asking PII-revealing questions when the toggle is on.
 */
export interface PublicFormAnswerDto {
  questionId: string;
  questionTextSnapshot: string;
  textValue?: string | null;
  selectedOptionTextSnapshots: string[];
  booleanValue?: boolean | null;
}

/**
 * Phase 6A.146 — public response DTO.
 *
 * 2026-05-15 product correction: surface the respondent's NAME when provided
 * (attribution like "Niro K · bringing biriyani" is normal in sign-up contexts).
 * Email and userId are still hidden — those are the actual contact-method PII
 * that the toggle's privacy promise covers. The backend DTO physically lacks
 * those two fields and this interface mirrors that exclusion.
 */
export interface PublicFormResponseDto {
  id: string;
  /**
   * Respondent's self-supplied name. Null when the respondent skipped the
   * optional name field — UI must fall back to `respondentLabel` in that case.
   */
  respondentName?: string | null;
  /** "Respondent 1", "Respondent 2", ... assigned by SubmittedAt ASC. */
  respondentLabel: string;
  /** ISO date string "YYYY-MM-DD" (DateOnly on the wire — no time-of-day). */
  submittedOn: string;
  answers: PublicFormAnswerDto[];
}

/**
 * Phase 6A.146 — top-level payload for GET /forms/{formId}/responses/public.
 */
export interface PublicFormResponsesDto {
  formId: string;
  formTitle: string;
  totalCount: number;
  responses: PublicFormResponseDto[];
}

// ==================== Custom Forms - Request DTOs ====================

/**
 * Create form question item (used in CreateEventFormRequest)
 */
export interface CreateFormQuestionItem {
  questionText: string;
  questionType: FormQuestionType;
  isRequired: boolean;
  sortOrder: number;
  helpText?: string | null;
  options?: { text: string; sortOrder: number }[] | null;
}

/**
 * Create event form request
 * POST /api/events/{id}/forms
 */
export interface CreateEventFormRequest {
  title: string;
  description?: string | null;
  allowMultipleResponses: boolean;
  responseDeadline?: string | null;
  maxResponses?: number | null;
  questions: CreateFormQuestionItem[];
  /** Phase 6A.146 — optional, defaults false on the server when omitted. */
  allowAttendeesToViewResponses?: boolean;
}

/**
 * Update event form request
 * PUT /api/events/{id}/forms/{formId}
 */
export interface UpdateEventFormRequest {
  title: string;
  description?: string | null;
  allowMultipleResponses: boolean;
  responseDeadline?: string | null;
  maxResponses?: number | null;
  /**
   * Phase 6A.146 — nullable/undefined means "leave the flag unchanged"
   * on the server (the domain interprets the backend null the same way).
   * UI sends the explicit boolean when the toggle changes.
   */
  allowAttendeesToViewResponses?: boolean | null;
}

/**
 * Add form question request
 * POST /api/events/{id}/forms/{formId}/questions
 */
export interface AddFormQuestionRequest {
  questionText: string;
  questionType: FormQuestionType;
  isRequired: boolean;
  sortOrder: number;
  helpText?: string | null;
  options?: { text: string; sortOrder: number }[] | null;
}

/**
 * Update form question request
 * PUT /api/events/{id}/forms/{formId}/questions/{questionId}
 */
export interface UpdateFormQuestionRequest {
  questionText: string;
  questionType: FormQuestionType;
  isRequired: boolean;
  sortOrder: number;
  helpText?: string | null;
  options?: { id?: string | null; text: string; sortOrder: number }[] | null;
}

/**
 * Reorder form questions request
 * PUT /api/events/{id}/forms/{formId}/questions/reorder
 */
export interface ReorderFormQuestionsRequest {
  questionIdsInOrder: string[];
}

/**
 * Submit form response answer item
 */
export interface SubmitFormAnswerItem {
  questionId: string;
  textValue?: string | null;
  selectedOptionIds?: string[] | null;
  booleanValue?: boolean | null;
}

/**
 * Submit form response request
 * POST /api/events/{id}/forms/{formId}/responses
 */
export interface SubmitFormResponseRequest {
  respondentName?: string | null;
  respondentEmail?: string | null;
  answers: SubmitFormAnswerItem[];
}

/**
 * Submit form response result
 */
export interface SubmitFormResponseResult {
  responseId: string;
  accessToken: string;
}

/**
 * Update form response request
 * PUT /api/events/{id}/forms/{formId}/responses/{responseId}?token={token}
 * Phase 6A.106-110 Fix: Changed 'answers' to 'Answers' to match backend C# casing
 */
export interface UpdateFormResponseRequest {
  Answers: SubmitFormAnswerItem[];  // Capital 'A' to match backend
}

// ==================== Donations ====================

/**
 * Donation status enum.
 * Uses string values to match backend JsonStringEnumConverter.
 */
export enum DonationStatus {
  Pending = 'Pending',
  Completed = 'Completed',
  Failed = 'Failed',
  Abandoned = 'Abandoned',
  Refunded = 'Refunded',
}

/**
 * Donation configuration for an event.
 * Maps from backend DonationConfigurationDto.
 */
export interface DonationConfigurationDto {
  isEnabled: boolean;
  suggestedAmounts: number[];
  allowCustomAmount: boolean;
  minAmount?: number | null;
  maxAmount?: number | null;
  donationMessage?: string | null;
  showDonationSummary: boolean;
}

/**
 * Public-facing donation summary (no PII).
 * Returned by GET /api/events/{eventId}/donations/public-summary
 * Only available when organizer has enabled ShowDonationSummary.
 */
export interface PublicDonationSummaryDto {
  completedDonations: number;
  netRaisedAmount: number;
  currency: string;
}

/**
 * Public-facing collection summary (no PII).
 * Returned by GET /api/events/{eventId}/collections/public-summary
 */
export interface PublicCollectionSummaryDto {
  totalAmount: number;
  goalAmount?: number | null;
  goalProgressPercent?: number | null;
  completedCollections: number;
  contributorCount: number;
  currency: string;
}

/**
 * Individual donation record.
 */
export interface DonationDto {
  id: string;
  eventId: string;
  registrationId?: string | null;
  donorUserId?: string | null;
  donorName: string;
  donorEmail: string;
  donorPhone?: string | null;
  donorNotes?: string | null;
  amount: number;
  currency: string;
  status: string;
  isBundled: boolean;
  stripeFeeAmount?: number | null;
  platformCommissionAmount?: number | null;
  organizerPayoutAmount?: number | null;
  createdAt: string;
  paymentCompletedAt?: string | null;
}

/**
 * Donation summary statistics.
 */
export interface DonationSummaryDto {
  totalDonations: number;
  completedDonations: number;
  totalAmount: number;
  averageDonation: number;
  currency: string;
  totalStripeFees: number;
  totalPlatformCommission: number;
  totalOrganizerPayout: number;
}

/**
 * Response from GetEventDonationsQuery.
 */
export interface EventDonationsResponse {
  eventId: string;
  eventTitle: string;
  donations: DonationDto[];
  summary: DonationSummaryDto;
}

/**
 * Request body for creating a standalone donation.
 */
export interface CreateDonationRequest {
  donorName: string;
  donorEmail: string;
  donorPhone?: string | null;
  donorNotes?: string | null;
  amount: number;
  currency?: string | null;
  successUrl: string;
  cancelUrl: string;
}

// ==================== Collections ====================

export enum CollectionStatus {
  Pending = 'Pending',
  Completed = 'Completed',
  Failed = 'Failed',
  Abandoned = 'Abandoned',
  Refunded = 'Refunded',
}

export interface CollectionConfigurationDto {
  isEnabled: boolean;
  goalAmount?: number | null;
  showProgress: boolean;
  suggestedAmounts: number[];
  allowCustomAmount: boolean;
  minAmount?: number | null;
  maxAmount?: number | null;
  collectionMessage?: string | null;
  showContributorCount: boolean;
}

export interface CollectionDto {
  id: string;
  eventId: string;
  contributorUserId?: string | null;
  contributorName: string;
  contributorEmail: string;
  contributorPhone?: string | null;
  contributorNotes?: string | null;
  amount: number;
  currency: string;
  status: string;
  stripeFeeAmount?: number | null;
  platformCommissionAmount?: number | null;
  organizerPayoutAmount?: number | null;
  createdAt: string;
  paymentCompletedAt?: string | null;
}

export interface CollectionSummaryDto {
  totalCollections: number;
  completedCollections: number;
  totalAmount: number;
  averageCollection: number;
  currency: string;
  goalAmount?: number | null;
  goalProgressPercent?: number | null;
  contributorCount: number;
  totalStripeFees: number;
  totalPlatformCommission: number;
  totalOrganizerPayout: number;
}

export interface EventCollectionsResponse {
  eventId: string;
  eventTitle: string;
  collections: CollectionDto[];
  summary: CollectionSummaryDto;
}

export interface CreateCollectionRequest {
  contributorName: string;
  contributorEmail: string;
  contributorPhone?: string | null;
  contributorNotes?: string | null;
  amount: number;
  currency?: string | null;
  successUrl: string;
  cancelUrl: string;
}

// ==================== Sponsors ====================

export enum SponsorType {
  Money = 'Money',
  Item = 'Item',
}

export enum SponsorStatus {
  Pending = 'Pending',
  Completed = 'Completed',
  Failed = 'Failed',
  Abandoned = 'Abandoned',
  Refunded = 'Refunded',
  RecordedItem = 'RecordedItem',
}

export interface SponsorConfigurationDto {
  isEnabled: boolean;
  acceptMoneySponsors: boolean;
  acceptItemSponsors: boolean;
  minSponsorAmount?: number | null;
  sponsorMessage?: string | null;
  showSponsorList: boolean;
  /**
   * Phase 6A.156 — gates whether organizer-defined sponsorship packages
   * (Gold/Silver/Bronze tiers) are exposed on the public event page. Default
   * false; existing rows missing this field deserialize to false (backward-
   * compatible for all pre-6A.156 events).
   */
  enablePackages?: boolean;
}

/**
 * Phase 6A.156 — organizer-defined sponsorship package (Gold / Silver /
 * Bronze tiers). Catalogue projection mirrored from `AddOnDefinitionDto`.
 * Buyer transactions live on the `Sponsor` aggregate (FK + snapshots,
 * populated in 6A.157+).
 */
export interface SponsorshipPackageDto {
  id: string;
  eventId: string;
  name: string;
  description?: string | null;
  priceAmount: number;
  priceCurrency: string;
  quantityLimit?: number | null;
  quantitySold: number;
  remainingStock?: number | null;
  isActive: boolean;
  sortOrder: number;
  imageUrl?: string | null;
  imageBlobName?: string | null;
  tier?: string | null;
  perks: string[];
  includedTicketCount: number;
  createdAt: string; // ISO 8601
  updatedAt?: string | null;
}

export interface CreateSponsorshipPackageRequest {
  name: string;
  description?: string | null;
  price: number;
  currency?: string;
  quantityLimit?: number | null;
  sortOrder: number;
  tier?: string | null;
  perks?: string[];
  includedTicketCount: number;
}

export interface UpdateSponsorshipPackageRequest {
  name: string;
  description?: string | null;
  price: number;
  currency?: string;
  quantityLimit?: number | null;
  sortOrder: number;
  tier?: string | null;
  perks?: string[];
  includedTicketCount: number;
  isActive: boolean;
}

export interface SetSponsorshipPackageImageResult {
  imageUrl: string;
  imageBlobName: string;
}

// ────────────────────────────────────────────────────────────────────────────
// Phase 6A.157 — public/buyer-facing sponsorship package types
// ────────────────────────────────────────────────────────────────────────────

/**
 * Phase 6A.157 — public/buyer DTO returned by `GET /sponsorship-packages/active`.
 * Strips organizer-only fields (quantitySold, quantityLimit, imageBlobName,
 * audit, isActive). Mirrors the backend `SponsorshipPackagePublicDto`.
 */
export interface SponsorshipPackagePublicDto {
  id: string;
  eventId: string;
  name: string;
  description?: string | null;
  priceAmount: number;
  priceCurrency: string;
  /** Null = unlimited; 0 = sold out (server filters sold-out rows but the field is exposed for client-side defensive UX). */
  remainingStock?: number | null;
  isSoldOut: boolean;
  sortOrder: number;
  imageUrl?: string | null;
  tier?: string | null;
  perks: string[];
  /** Informational only — system does NOT issue tickets for package sponsors (organizer handles admission off-platform per 6A.157 final scope). */
  includedTicketCount: number;
}

/**
 * Phase 6A.157 — request body for `POST /sponsorship-packages/{packageId}/purchase`.
 * Mirrors the backend `CreatePackageSponsorRequest`. Buyer fields snapshot
 * onto the new Sponsor row; success/cancel URLs forwarded to Stripe Checkout
 * (paid packages) or used directly (free packages).
 */
export interface CreatePackageSponsorRequest {
  buyerName: string;
  buyerEmail: string;
  buyerPhone?: string | null;
  buyerOrganization?: string | null;
  buyerNotes?: string | null;
  successUrl: string;
  cancelUrl: string;
}

/**
 * Phase 6A.157 — response from `POST /sponsorship-packages/{packageId}/purchase`.
 * Mirrors the backend `CreatePackageSponsorResult`.
 *
 * `checkoutUrl` is the Stripe Checkout URL for paid packages, or the
 * SuccessUrl directly for free $0 packages — caller redirects to it either
 * way. `sponsorId` is the new Pending Sponsor row so the FE can attach a
 * buyer logo via `POST /sponsors/{id}/image` BEFORE the Stripe redirect
 * (mirrors 6A.145's widened CreateMoneySponsor pattern).
 */
export interface CreatePackageSponsorResult {
  checkoutUrl: string;
  sponsorId: string;
}

export interface SponsorDto {
  id: string;
  eventId: string;
  sponsorUserId?: string | null;
  sponsorName: string;
  sponsorEmail: string;
  sponsorPhone?: string | null;
  sponsorOrganization?: string | null;
  sponsorNotes?: string | null;
  sponsorType: string;
  amount?: number | null;
  currency?: string | null;
  status: string;
  itemName?: string | null;
  itemDescription?: string | null;
  estimatedValue?: number | null;
  stripeFeeAmount?: number | null;
  platformCommissionAmount?: number | null;
  organizerPayoutAmount?: number | null;
  // Phase 6A.145 — optional sponsor LOGO image.
  imageUrl?: string | null;
  imageBlobName?: string | null;
  // Phase 6A.162 — optional sponsor brochure/flyer (sibling slot to logo).
  // Orthogonal to imageUrl/imageBlobName; touching one does NOT mutate the
  // other (pinned by backend SponsorTests independence invariants).
  brochureUrl?: string | null;
  brochureBlobName?: string | null;
  createdAt: string;
  paymentCompletedAt?: string | null;
}

/**
 * Phase 6A.151 — PATCH /events/{eventId}/sponsors/{sponsorId} request body.
 * All fields optional; null/undefined = leave unchanged. Server enforces the
 * state-edit matrix per-field (see Sponsor.UpdateXxx domain methods).
 */
export interface UpdateSponsorRequest {
  name?: string | null;
  notes?: string | null;
  organization?: string | null;
  amount?: number | null;
  currency?: string | null;
  itemName?: string | null;
  itemDescription?: string | null;
  estimatedValue?: number | null;
}

// Phase 6A.145 Commit 7 — Money sponsor create endpoint now returns both the
// Stripe checkout URL AND the newly-created Sponsor ID. The FE uses the ID to
// attach an optional image to the Pending sponsor BEFORE the Stripe redirect.
export interface CreateMoneySponsorResult {
  checkoutUrl: string;
  sponsorId: string;
}

// Phase 6A.145 — organizer-add-off-platform-sponsor (POST /sponsors/off-platform).
// Multipart on the wire — file rides alongside the form fields. The repository
// layer assembles a FormData; this interface documents the typed payload.
export interface CreateOffPlatformSponsorRequest {
  type: 'Money' | 'Item';
  sponsorName: string;
  sponsorEmail: string;
  sponsorPhone?: string | null;
  sponsorOrganization?: string | null;
  sponsorNotes?: string | null;
  // Money branch
  amount?: number | null;
  currency?: string | null;
  // Item branch
  itemName?: string | null;
  itemDescription?: string | null;
  estimatedValue?: number | null;
  // Optional image file
  image?: File | null;
}

export interface CreateOffPlatformSponsorResult {
  sponsorId: string;
  imageUrl?: string | null;
}

export interface SponsorSummaryDto {
  totalSponsors: number;
  completedMoneySponsors: number;
  recordedItemSponsors: number;
  totalMoneyAmount: number;
  currency: string;
  itemSponsorCount: number;
  totalStripeFees: number;
  totalPlatformCommission: number;
  totalOrganizerPayout: number;
}

export interface EventSponsorsResponse {
  eventId: string;
  eventTitle: string;
  sponsors: SponsorDto[];
  summary: SponsorSummaryDto;
}

/**
 * Phase 6A.150 — sanitized public sponsor DTO returned by
 * GET /api/events/{eventId}/sponsors/public ([AllowAnonymous]).
 *
 * Fields are limited to what SponsorsPreviewStrip and SponsorSection
 * actually display publicly: logo + name + organization + item label.
 * PII fields (email, phone, notes, amount, estimated value, Stripe fee
 * detail, internal blob name, etc.) are PHYSICALLY ABSENT from this
 * interface and from the wire response — the backend handler strips
 * them at projection time. If a future edit adds a property here, the
 * backend reflection-asserted PII guard test must be updated too.
 */
export interface PublicSponsorDto {
  id: string;
  sponsorOrganization?: string | null;
  sponsorName: string;
  itemName?: string | null;
  imageUrl?: string | null;
  /**
   * Phase 6A.162 — optional brochure/flyer URL. When set, the click-to-popup
   * flow on the public sponsor strip shows the brochure full-size; when null
   * the popup falls back to the logo. `brochureBlobName` STAYS ABSENT (PII —
   * internal storage identifier, backend reflection-asserted).
   */
  brochureUrl?: string | null;
  /** "Money" or "Item" — drives the Item-name caption rendering. */
  sponsorType: string;
}

export interface PublicEventSponsorsResponse {
  eventId: string;
  sponsors: PublicSponsorDto[];
}

export interface CreateMoneySponsorRequest {
  sponsorName: string;
  sponsorEmail: string;
  sponsorPhone?: string | null;
  sponsorOrganization?: string | null;
  sponsorNotes?: string | null;
  amount: number;
  currency?: string | null;
  successUrl: string;
  cancelUrl: string;
}

export interface CreateItemSponsorRequest {
  sponsorName: string;
  sponsorEmail: string;
  sponsorPhone?: string | null;
  sponsorOrganization?: string | null;
  sponsorNotes?: string | null;
  itemName: string;
  itemDescription?: string | null;
  estimatedValue?: number | null;
}

// ==================== Add-Ons ====================

export enum AddOnPurchaseStatus {
  Pending = 'Pending',
  Completed = 'Completed',
  Failed = 'Failed',
  Abandoned = 'Abandoned',
  Refunded = 'Refunded',
}

export interface AddOnConfigurationDto {
  isEnabled: boolean;
  availableDuringRegistration: boolean;
  availableStandalone: boolean;
  addOnMessage?: string | null;
}

export interface AddOnDefinitionDto {
  id: string;
  eventId: string;
  name: string;
  description?: string | null;
  price: number;
  currency: string;
  quantityLimit?: number | null;
  quantitySold: number;
  remainingStock?: number | null;
  isActive: boolean;
  sortOrder: number;
  // Phase 6A.143 — optional add-on image (rendered as a thumbnail in AddOnSelector + manage tab).
  // Always either both set or both null. Editing flows through dedicated upload/delete endpoints.
  imageUrl?: string | null;
  imageBlobName?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

// Phase 6A.143 — response from image-upload endpoints (Set commands).
export interface ImageUploadResultDto {
  imageUrl: string;
  imageBlobName: string;
}

export interface AddOnPurchaseDto {
  id: string;
  eventId: string;
  addOnDefinitionId: string;
  addOnName: string;
  registrationId?: string | null;
  buyerUserId?: string | null;
  buyerName: string;
  buyerEmail: string;
  buyerPhone?: string | null;
  quantity: number;
  unitPrice: number;
  totalAmount: number;
  currency: string;
  status: string;
  stripeFeeAmount?: number | null;
  platformCommissionAmount?: number | null;
  organizerPayoutAmount?: number | null;
  createdAt: string;
  paymentCompletedAt?: string | null;
}

export interface AddOnPurchaseSummaryDto {
  totalPurchases: number;
  completedPurchases: number;
  totalRevenue: number;
  currency: string;
  totalStripeFees: number;
  totalPlatformCommission: number;
  totalOrganizerPayout: number;
  totalItemsSold: number;
}

export interface EventAddOnPurchasesResponse {
  eventId: string;
  eventTitle: string;
  definitions: AddOnDefinitionDto[];
  purchases: AddOnPurchaseDto[];
  summary: AddOnPurchaseSummaryDto;
}

export interface CreateAddOnDefinitionRequest {
  name: string;
  description?: string | null;
  price: number;
  currency?: string | null;
  quantityLimit?: number | null;
  sortOrder: number;
}

export interface UpdateAddOnDefinitionRequest {
  name: string;
  description?: string | null;
  price: number;
  currency?: string | null;
  quantityLimit?: number | null;
  sortOrder: number;
  isActive: boolean;
}

export interface PurchaseAddOnRequest {
  buyerName: string;
  buyerEmail: string;
  buyerPhone?: string | null;
  quantity: number;
  successUrl: string;
  cancelUrl: string;
}

export interface PurchaseAddOnCartItemRequest {
  addOnDefinitionId: string;
  quantity: number;
}

export interface PurchaseAddOnCartRequest {
  buyerName: string;
  buyerEmail: string;
  buyerPhone?: string | null;
  items: PurchaseAddOnCartItemRequest[];
  successUrl: string;
  cancelUrl: string;
}

// Config update request types
export interface UpdateCollectionConfigRequest {
  isEnabled: boolean;
  goalAmount?: number | null;
  showProgress: boolean;
  suggestedAmounts?: number[] | null;
  allowCustomAmount: boolean;
  minAmount?: number | null;
  maxAmount?: number | null;
  collectionMessage?: string | null;
  showContributorCount: boolean;
}

export interface UpdateSponsorConfigRequest {
  isEnabled: boolean;
  acceptMoneySponsors: boolean;
  acceptItemSponsors: boolean;
  minSponsorAmount?: number | null;
  sponsorMessage?: string | null;
  showSponsorList: boolean;
  /**
   * Phase 6A.156 — gates the organizer-defined sponsorship-package grid
   * (Gold/Silver/Bronze) on the public event page. Default false on the
   * backend for backward-compatibility with pre-6A.156 clients.
   */
  enablePackages?: boolean;
}

export interface UpdateAddOnConfigRequest {
  isEnabled: boolean;
  availableDuringRegistration: boolean;
  availableStandalone: boolean;
  addOnMessage?: string | null;
}

// ==================== Photo Album Types ====================

/**
 * Album status matching backend AlbumStatus enum (string via JsonStringEnumConverter)
 */
export enum AlbumStatus {
  Draft = 'Draft',
  Published = 'Published',
}

/**
 * Album photo status matching backend AlbumPhotoStatus enum
 */
export enum AlbumPhotoStatus {
  Approved = 'Approved',
}

/**
 * Album media type discriminator matching backend AlbumMediaType enum.
 * Uses string values to match JsonStringEnumConverter output.
 */
export type AlbumMediaType = 'Photo' | 'Video';

/**
 * Photo album DTO matching backend PhotoAlbumDto
 */
export interface PhotoAlbumDto {
  id: string;
  eventId: string;
  organizerId: string;
  eventTitle: string;
  name: string;
  status: AlbumStatus;
  description: string | null;
  coverPhotoUrl: string | null;
  retentionDays: number;
  photoCount: number;
  publishedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
}

/**
 * Album photo DTO matching backend AlbumPhotoDto
 */
export interface AlbumPhotoDto {
  id: string;
  albumId: string;
  uploaderId: string;
  uploaderName: string;
  originalUrl: string;
  thumbnailUrl: string;
  mediumUrl: string;
  caption: string | null;
  status: AlbumPhotoStatus;
  mediaType: AlbumMediaType;
  fileSizeBytes: number;
  durationSeconds: number | null;
  uploadedAt: string;
  expiresAt: string;
  displayOrder: number;
}

/**
 * Paginated album photos response matching backend PaginatedAlbumPhotosResponse
 */
export interface PaginatedAlbumPhotosResponse {
  photos: AlbumPhotoDto[];
  hasMore: boolean;
  nextCursor: string | null;
  totalCount: number;
}

/**
 * Request to create a photo album
 */
export interface CreatePhotoAlbumRequest {
  name: string;
  description?: string;
}

/**
 * Request to update album details (name and description)
 */
export interface UpdateAlbumDetailsRequest {
  name: string;
  description?: string;
}

// ==================== Phase 8: Ticket Tier Request Types ====================

/**
 * Phase 8: Request to set the ticketing mode for an event.
 */
export interface SetTicketingModeRequest {
  ticketingMode: TicketingMode;
}

/**
 * Seating Redesign Slice 1: Request to set the seating mode for an event.
 * AssignedSeating requires the event to already be in TicketingMode.Tiered.
 */
export interface SetSeatingModeRequest {
  seatingMode: SeatingMode;
}

/**
 * Phase 8: Request to create a ticket tier.
 */
export interface CreateTicketTierRequest {
  name: string;
  description?: string | null;
  adultPriceAmount: number;
  adultPriceCurrency: Currency;
  childPriceAmount?: number | null;
  childPriceCurrency?: Currency | null;
  childAgeLimit?: number | null;
  capacity: number;
  maxPerUser?: number;
  sortOrder?: number;
}

/**
 * Phase 8: Request to update a ticket tier.
 */
export interface UpdateTicketTierRequest {
  name: string;
  description?: string | null;
  adultPriceAmount: number;
  adultPriceCurrency: Currency;
  childPriceAmount?: number | null;
  childPriceCurrency?: Currency | null;
  childAgeLimit?: number | null;
  capacity: number;
  maxPerUser?: number;
  sortOrder?: number;
}

// ==================== Phase 2: Seating & Venue Layout ====================

/**
 * Seating mode for an event.
 * Matches backend LankaConnect.Domain.Events.Enums.SeatingMode
 */
export enum SeatingMode {
  GeneralAdmission = 'GeneralAdmission',
  AssignedSeating = 'AssignedSeating',
}

/**
 * Layout type for venue layout.
 * Matches backend LankaConnect.Domain.Events.Enums.LayoutType
 */
export enum LayoutType {
  Theater = 'Theater',
  Banquet = 'Banquet',
  Custom = 'Custom',
  // Slice 2+3A: hybrid layouts combining theater rows and banquet tables.
  Mixed = 'Mixed',
}

/**
 * Slice 2+3A: Canvas shape a zone is rendered with.
 * Matches backend LankaConnect.Domain.Events.Enums.ZoneShape
 */
export enum ZoneShape {
  Rect = 'Rect',
  Curve = 'Curve',
  Polygon = 'Polygon',
}

/**
 * Slice 2+3A: Table shape for a banquet / dining table.
 * Matches backend LankaConnect.Domain.Events.Enums.TableShape
 */
export enum TableShape {
  Round = 'Round',
  Square = 'Square',
  Rect = 'Rect',
}

/**
 * Slice 2+3A: Non-seating decorative/structural element on the canvas.
 * Matches backend LankaConnect.Domain.Events.Enums.DecorationKind
 */
export enum DecorationKind {
  Stage = 'Stage',
  DanceFloor = 'DanceFloor',
  Aisle = 'Aisle',
  Door = 'Door',
  Wall = 'Wall',
  Text = 'Text',
  Image = 'Image',
}

/**
 * Seat availability status (derived from runtime state).
 */
export type SeatStatus = 'Available' | 'Held' | 'Reserved' | 'Disabled';

/**
 * Slice 2+3A: Canvas rendering configuration for a layout. Flat-columns model
 * on the backend (OwnsOne, not JSON) — keep a stable shape on the FE.
 */
export interface CanvasConfigDto {
  width: number;
  height: number;
  scale: number;
  backgroundColor: string;
}

/**
 * Venue layout DTO — aggregate with zones, tables, decorations, and seats.
 * Tables / decorations / canvas are optional for backwards compatibility with
 * legacy layouts that predate Slice 2+3A.
 * Matches backend VenueLayoutDto
 */
export interface VenueLayoutDto {
  id: string;
  name: string;
  eventId?: string | null;
  layoutType: string;
  isTemplate: boolean;
  createdByUserId: string;
  totalCapacity: number;
  createdAt: string;
  updatedAt?: string | null;
  zones: VenueZoneDto[];
  // Slice 2+3A additions — optional to preserve backward compatibility.
  canvas?: CanvasConfigDto;
  tables?: VenueTableDto[];
  decorations?: VenueDecorationDto[];
  /**
   * Slice 5: PostgreSQL xmin serialized as uint. Sent back as the
   * `If-Match` header on PUT/PATCH/DELETE for optimistic concurrency
   * (409 on mismatch).
   */
  rowVersion: number;
}

/**
 * Venue zone DTO — a named section of a layout.
 */
export interface VenueZoneDto {
  id: string;
  name: string;
  color: string;
  /**
   * @deprecated Slice 4 Release N: server returns `null`. Tier mapping is now polymorphic
   * via tier_assignments. Slice 5 will expose GET /api/venue-layouts/{id}/tier-assignments
   * and a TierAssignmentDto[] on the layout. Field removed entirely in Release N+1.
   */
  ticketTierId?: string | null;
  sortOrder: number;
  enabledSeatCount: number;
  totalSeatCount: number;
  seats: SeatDto[];
  // Slice 2+3A: canvas shape + geometry. Geometry is a JSON string matching
  // the shape-specific schema documented on VenueZone.Geometry.
  shape?: ZoneShape;
  geometry?: string;

  /**
   * Slice 5 Chunk 8: tier IDs currently assigned to this zone via the polymorphic
   * tier_assignments junction. Replaces the legacy scalar `ticketTierId`.
   * Empty for template layouts or zones with no tier mapping yet.
   */
  ticketTierIds?: string[];
}

/**
 * Slice 2+3A: Banquet / dining table DTO. Tables live on a layout and own
 * their seats directly; optional VenueZoneId groups tables under a section.
 */
export interface VenueTableDto {
  id: string;
  venueLayoutId: string;
  venueZoneId?: string | null;
  label: string;
  shape: TableShape;
  geometry: string;
  capacity: number;
  sortOrder: number;
  enabledSeatCount: number;
  seats: SeatDto[];

  /**
   * Slice 5 Chunk 8: tier IDs currently assigned to this table via the
   * polymorphic tier_assignments junction. Empty for template layouts or
   * tables with no tier mapping yet.
   */
  ticketTierIds?: string[];
}

/**
 * Slice 2+3A: Decoration DTO for non-seating elements (stage, aisle, text, …).
 */
export interface VenueDecorationDto {
  id: string;
  venueLayoutId: string;
  kind: DecorationKind;
  label?: string | null;
  geometry: string;
  properties: string;
  sortOrder: number;
}

/**
 * Seat DTO — structural data for a single seat.
 * Slice 2+3A: a seat belongs to EITHER a zone OR a table (XOR); angleDeg is set
 * for radial (round-table) seats.
 */
export interface SeatDto {
  id: string;
  row: string;
  number: number;
  label: string;
  sortOrder: number;
  isEnabled: boolean;
  isAccessible: boolean;
  /**
   * Canvas x coordinate. Nullable — the domain leaves x/y null for seats
   * generated by theater zones (Row + Number positioning) and only
   * populates them when the canvas editor places seats explicitly.
   * Matches backend `double?`.
   */
  x?: number | null;
  y?: number | null;
  // Slice 2+3A — optional for backward compatibility with existing zone seats.
  venueZoneId?: string | null;
  venueTableId?: string | null;
  angleDeg?: number | null;
}

/**
 * Seat availability DTO — combines structural data with runtime status.
 * Matches backend SeatAvailabilityDto
 */
export interface SeatAvailabilityDto {
  id: string;
  label: string;
  row: string;
  number: number;
  isEnabled: boolean;
  isAccessible: boolean;
  x: number;
  y: number;
  status: SeatStatus;
  zoneId: string;
  zoneName: string;
  zoneColor: string;
  /** @deprecated Slice 4 Release N: server returns `null`. See VenueZoneDto.ticketTierId. */
  ticketTierId?: string | null;
}

/**
 * Result from holding seats.
 */
export interface HoldSeatsResult {
  heldSeatIds: string[];
  expiresAt: string;
  sessionId: string;
}

// ==================== Seating Request DTOs ====================

export interface CreateVenueLayoutRequest {
  name: string;
  layoutType: string;
  eventId?: string | null;
  isTemplate: boolean;
  zones: CreateVenueZoneRequest[];
}

export interface CreateVenueZoneRequest {
  name: string;
  color: string;
  /**
   * @deprecated Slice 4 Release N: server accepts but ignores this field. Use the
   * tier-assignment endpoints (Slice 5) to map tiers to zones after layout creation.
   * Field removed entirely in Release N+1.
   */
  ticketTierId?: string | null;
  sortOrder: number;
}

export interface GenerateSeatsRequest {
  generationType: string;
  rowsOrTables: number;
  seatsPerUnit: number;
  startLabel?: string | null;
}

export interface AssignLayoutRequest {
  eventId: string;
  layoutId: string;
}

export interface HoldSeatsRequest {
  sessionId: string;
  seatIds: string[];
}

export interface ReleaseSeatsRequest {
  sessionId: string;
}

// ==================== Slice 5 Chunk 11 — layout CRUD request types ====================

/**
 * Slice 5 Chunk 4: PUT /api/venue-layouts/{id} — update layout name and/or canvas.
 * Both fields optional; at least one must be supplied. `If-Match` header carries
 * the expected RowVersion separately.
 */
export interface UpdateVenueLayoutRequest {
  name?: string | null;
  canvas?: UpdateLayoutCanvasRequest | null;
}

export interface UpdateLayoutCanvasRequest {
  width: number;
  height: number;
  scale: number;
  backgroundColor: string;
}

/**
 * Slice 5 Chunk 5: PATCH /api/venue-layouts/{id}/zones/{zoneId}. All fields
 * optional — send only what changed. Structural changes (shape/geometry) are
 * rejected with 422 when seats on the zone are held/reserved.
 */
export interface UpdateZoneRequest {
  name?: string | null;
  color?: string | null;
  sortOrder?: number | null;
  /** Stringified `ZoneShape` enum value for JSON-friendliness. */
  shape?: string | null;
  geometry?: string | null;
}

/**
 * Slice 5 Chunk 6: POST /api/venue-layouts/{id}/tables. Seats are auto-generated
 * based on shape + capacity. `startAngleDeg` applies to round tables only
 * (default 0° when omitted).
 */
export interface AddTableRequest {
  label: string;
  /** Stringified `TableShape` enum value. */
  shape: string;
  capacity: number;
  sortOrder: number;
  zoneId?: string | null;
  geometry?: string | null;
  startAngleDeg?: number | null;
}

export interface AddTableResponse {
  tableId: string;
}

/**
 * Slice 5 Chunk 6: PATCH /api/venue-layouts/{id}/tables/{tableId}. Pass
 * `clearZoneId: true` to explicitly detach the table from its zone (supplying
 * `zoneId: null` alone is treated as "keep current zone" so callers can omit
 * unchanged fields safely).
 */
export interface UpdateTableRequest {
  label?: string | null;
  /** Stringified `TableShape` enum value. */
  shape?: string | null;
  capacity?: number | null;
  sortOrder?: number | null;
  zoneId?: string | null;
  clearZoneId?: boolean | null;
  geometry?: string | null;
}

/**
 * Slice 5 Chunk 7: POST /api/venue-layouts/{id}/decorations. `label` is
 * required only for the `Text` kind; others accept it as optional metadata.
 */
export interface AddDecorationRequest {
  /** Stringified `DecorationKind` enum value. */
  kind: string;
  label?: string | null;
  sortOrder: number;
  geometry?: string | null;
  properties?: string | null;
}

export interface AddDecorationResponse {
  decorationId: string;
}

/**
 * Slice 5 Chunk 7: PATCH /api/venue-layouts/{id}/decorations/{decorationId}.
 * Pass `clearLabel: true` to detach the label (rejected when kind is `Text`).
 */
export interface UpdateDecorationRequest {
  /** Stringified `DecorationKind` enum value. */
  kind?: string | null;
  label?: string | null;
  clearLabel?: boolean | null;
  sortOrder?: number | null;
  geometry?: string | null;
  properties?: string | null;
}

/**
 * Slice 5 Chunk 8: `AssignableKind` for the polymorphic tier-assignment junction.
 * Matches backend `LankaConnect.Domain.Events.Enums.AssignableKind`.
 */
export enum AssignableKind {
  Zone = 'Zone',
  Table = 'Table',
}

/**
 * Slice 5 Chunk 8: POST /api/venue-layouts/{id}/tier-assignments.
 * Idempotent — re-assigning an existing tuple is a no-op. Does NOT bump the
 * layout RowVersion (assignments live on the `TicketTier` aggregate), but
 * `If-Match` is still required for authorization-context freshness.
 */
export interface AssignTierRequest {
  tierId: string;
  /** Stringified `AssignableKind` enum value. */
  kind: string;
  assignableId: string;
}

/**
 * Slice 5 Chunk 10: PUT /api/venue-layouts/{id}/batch body — atomic full-layout
 * replacement consumed by the Slice 8 canvas editor save path. Within each
 * child list: items with `id = null` are created, items with matching `id` are
 * updated in place, and existing children omitted from the payload are removed
 * (guarded against held/reserved seats).
 */
export interface BatchLayoutPayload {
  name?: string | null;
  canvas?: BatchCanvasConfig | null;
  zones?: BatchZone[] | null;
  tables?: BatchTable[] | null;
  decorations?: BatchDecoration[] | null;
  /**
   * Slice 8 S8.8c: declarative reconciliation of the polymorphic
   * `tier_assignments` junction. The list is the *complete desired state*
   * per `(kind, assignableId)` tuple — server diffs against current and
   * applies the minimum mutations inside the same transaction.
   * `null` (or omitted) → skip reconciliation. `[]` → remove all
   * assignments. For newly-added zones/tables, `assignableId` may be the
   * client-side draft Guid; backend resolves via `clientId` on
   * `BatchZone`/`BatchTable`.
   */
  tierAssignments?: BatchTierAssignment[] | null;
  /**
   * Slice S2 (Architect Rev 4 §A.3): explicit deletion opt-in. Any item
   * present in the existing layout but missing from the corresponding
   * `zones` / `tables` / `decorations` array MUST be listed here, otherwise
   * the backend returns **HTTP 409 Conflict**. Closes the destructive-PUT
   * bug class — pre-S2 a client bug that dropped a shape from state would
   * silently delete it (only protected by the structural guard for held
   * /reserved seats; empty zones got nuked silently).
   * `null` / omitted = "no explicit deletions" — therefore any omission is
   * unintentional → 409.
   */
  deletedZoneIds?: string[] | null;
  deletedTableIds?: string[] | null;
  deletedDecorationIds?: string[] | null;
}

export interface BatchCanvasConfig {
  width: number;
  height: number;
  scale: number;
  backgroundColor: string;
}

export interface BatchZone {
  /** `null` → create; matching id → update; omitted existing → remove. */
  id?: string | null;
  name: string;
  color: string;
  sortOrder: number;
  /** `ZoneShape` enum value serialized as a string (matches backend converter). */
  shape: ZoneShape;
  geometry?: string | null;
  /**
   * Slice 8 S8.8c: client-side draft Guid for newly-added zones (`id` ==
   * null). Lets `BatchTierAssignment.assignableId` reference the new zone
   * before the server has assigned its real Guid. Ignored when `id` is set.
   */
  clientId?: string | null;
  /**
   * Slice 9.5: optional theater-style seat-generation parameters. When BOTH
   * are provided (positive integers), the backend invokes
   * `VenueLayout.GenerateTheaterSeats(rows × seatsPerRow)` after the zone is
   * added/updated. The domain method clears existing seats first, so the
   * frontend property panel only surfaces these inputs for empty zones (UX
   * gate) — sending them for a zone with existing seats would wipe them.
   */
  rowCount?: number | null;
  seatsPerRow?: number | null;
}

export interface BatchTable {
  id?: string | null;
  label: string;
  shape: TableShape;
  capacity: number;
  sortOrder: number;
  zoneId?: string | null;
  geometry?: string | null;
  /** Slice 8 S8.8c — see {@link BatchZone.clientId}. */
  clientId?: string | null;
}

export interface BatchDecoration {
  id?: string | null;
  kind: DecorationKind;
  label?: string | null;
  sortOrder: number;
  geometry?: string | null;
  properties?: string | null;
}

/**
 * Slice 8 S8.8c: desired tier-assignment state for a single zone or table
 * in the canvas-editor batch save. `tierIds` is the complete set of tiers
 * the organizer wants assigned to `(kind, assignableId)` after the save
 * lands — backend reconciles via the minimum set of `AssignToZone` /
 * `AssignToTable` / `RemoveAssignment` domain calls in the same UoW commit.
 */
export interface BatchTierAssignment {
  kind: AssignableKind;
  assignableId: string;
  tierIds: string[];
}

// ============================================================================
// Slice 6: Layout preset library
// ============================================================================

/**
 * Slice 6 Chunk S6.2 — metadata for a single preset in the library modal.
 * Matches backend `LankaConnect.Application.Events.Queries.GetLayoutPresets.LayoutPresetDto`.
 *
 * Thumbnails are served from the web app's `/public/layouts/presets/` folder —
 * the modal renders PNG images, NOT react-konva, so the canvas library stays
 * lazy-loaded until the SeatPicker or canvas editor needs it.
 */
export interface LayoutPresetDto {
  /** Stable preset ID, e.g. `"theater-classic"`. Safe to use as a React key. */
  id: string;
  /** Human-readable preset name shown as the card title. */
  name: string;
  /** One-line description shown under the title in the card. */
  description: string;
  /**
   * Layout type the preset produces. Uses the backend's string enum
   * (`JsonStringEnumConverter`), not a number — matches MEMORY 6A.124.
   */
  layoutType: 'Theater' | 'Banquet' | 'Custom' | 'Mixed';
  /** Total enabled seat count for the preset as built. */
  totalCapacity: number;
  /** Absolute path to the pre-generated PNG thumbnail. */
  thumbnailUrl: string;
}

/**
 * Slice 6 Chunk S6.4 — POST /api/venue-layouts/from-preset body.
 * Matches backend `CreateLayoutFromPresetRequest`.
 */
export interface CreateLayoutFromPresetRequest {
  presetId: string;
  /** Omit to create a user-scoped template. Supply to attach to an event you own. */
  eventId?: string | null;
}

/**
 * Slice 8 S8.10 — POST /api/venue-layouts/from-template body. Matches backend
 * `CreateLayoutFromTemplateRequest`. Applies one of the caller's saved
 * templates to a target event the caller organizes. `layoutName` is optional —
 * server defaults to the source template's name.
 */
export interface CreateLayoutFromTemplateRequest {
  sourceTemplateId: string;
  eventId: string;
  layoutName?: string | null;
}

/**
 * Slice 9.2 — POST /api/venue-layouts/apply-preset body. Atomic preset apply:
 * single transaction that creates the layout AND flips the event into
 * assigned-seating mode pointing at the new layout. Replaces the broken
 * from-preset+assign two-step flow. No auto-tier-mapping.
 */
export interface ApplyPresetToEventRequest {
  presetId: string;
  eventId: string;
}

/**
 * Slice 9.2 — POST /api/venue-layouts/apply-template body. Mirror of
 * {@link ApplyPresetToEventRequest} for user-saved templates.
 * `layoutName` is optional — server defaults to the source template's name.
 */
export interface ApplyTemplateToEventRequest {
  sourceTemplateId: string;
  eventId: string;
  layoutName?: string | null;
}

/**
 * Slice S4 — DTOs for `GET /api/venue-layouts/{id}/publish-readiness`.
 * Mirrors the backend `PublishReadinessReportDto` shape. Codes are strings
 * (serialised from the `PublishReadinessCode` domain enum) so the FE doesn't
 * have to keep a parallel TS enum in lockstep.
 */
export interface PublishReadinessReportDto {
  isPublishReady: boolean;
  blockers: PublishReadinessIssueDto[];
  warnings: PublishReadinessIssueDto[];
  tierSummary: TierMappingSummaryDto[];
}

export interface PublishReadinessIssueDto {
  code: string;
  message: string;
  shapeId?: string | null;
  shapeName?: string | null;
  tierId?: string | null;
  tierName?: string | null;
}

export interface TierMappingSummaryDto {
  tierId: string;
  tierName: string;
  tierCapacity: number;
  mappedZones: MappedShapeRefDto[];
  mappedTables: MappedShapeRefDto[];
  totalEnabledSeats: number;
}

export interface MappedShapeRefDto {
  id: string;
  name: string;
  enabledSeatCount: number;
}

/**
 * Phase 7E.5 — query parameters for `GET /api/Events/allowed-registration-modes`.
 * Matches backend `GetAllowedRegistrationModesQuery` shape. All fields optional and default
 * to `false` server-side; the frontend Mode picker passes the current draft form-state on
 * every change so disabled options reflect server-side validation in real time
 * (architect hot-spot #5).
 */
export interface AllowedRegistrationModesRequest {
  isFreeAttendance?: boolean;
  hasSeating?: boolean;
  hasNamedSeating?: boolean;
  requiresAttendeeNameOnTicket?: boolean;
  hasDualPricing?: boolean;
  hasGroupTiers?: boolean;
  hasTicketTiers?: boolean;
  hasIdentityBoundAddOn?: boolean;
  hasMatrixPricing?: boolean;
  /**
   * Phase 8X.11 — payment-mode axis. The picker passes the form's current paymentMode
   * so the External option shows up exactly when the event is ExternalPaid. Defaults
   * to Free server-side for back-compat with pre-8X.11 callers.
   */
  paymentMode?: EventPaymentMode;
}

/**
 * Phase 7F-B (architect-approved 2026-04-30): request body for
 * `POST /api/events/{id}/convert-registration-mode`.
 *
 * - `dryRun`: when true, the backend computes the conversion report but does NOT mutate
 *   any registration. Drives the UI's diff-preview confirmation dialog.
 * - `notifyAttendees`: default false. When true, the backend (Phase 7F-B.4) sends each
 *   affected registrant an "your registration format changed" email via Hangfire fire-
 *   and-forget. Default-off avoids surprise inbox traffic during operator testing.
 */
export interface ConvertRegistrationModeRequest {
  targetMode: RegistrationMode;
  dryRun?: boolean;
  notifyAttendees?: boolean;
}

export interface ConvertedRegistrationRow {
  registrationId: string;
  beforeAttendeeCount: number;
  afterAttendeeCount: number;
}

export interface SkippedRegistrationRow {
  registrationId: string;
  reasonCode: string;
  reason: string;
}

export interface ConvertRegistrationModeResult {
  aggregateConversionId: string | null;
  totalProcessed: number;
  migratedCount: number;
  skippedCount: number;
  migrated: ConvertedRegistrationRow[];
  skipped: SkippedRegistrationRow[];
  wasDryRun: boolean;
}

/**
 * Phase 7F-D (architect-approved 2026-04-30): request body for
 * `POST /api/events/registrations/{id}/add-headcount`. Reuses HeadCountDto so the FE
 * can share the same form components used by RSVP (architect Q5).
 */
export interface InitiateAddHeadCountRequest {
  headCountDelta: HeadCountDto;
  successUrl: string;
  cancelUrl: string;
}

// ============================================================
// Phase 6A.141: Paid-event ticket check-in / QR scanner DTOs
// ============================================================

export interface TierBreakdownEntry {
  tier: string;
  count: number;
}

/**
 * Outcome of a ticket-scan attempt. Mirrors the server-side ScanTicketResult.
 * Most fields are nullable because the shape covers both accepted (green panel)
 * and rejected (red panel) outcomes via a single discriminator (`result`).
 */
export interface ScanTicketResult {
  result: 'accepted' | 'rejected';
  reason?: string | null;
  reasonMessage?: string | null;
  ticketCode?: string | null;
  attendeeName?: string | null;
  tier?: string | null;
  attendeeCount?: number | null;
  tierBreakdown?: TierBreakdownEntry[] | null;
  scannedAt?: string | null;        // ISO 8601 from server
  scannedBy?: string | null;
  usedPreviousKey: boolean;
  wrongEventTitle?: string | null;  // populated only on wrong_event rejection
  // UAT R2 Issue A — populated on already_scanned (and other ticket-resolved rejections).
  previousScanCount?: number | null;
  previousScannedBy?: string | null;
  // UAT R3 — full per-attendee detail. Null/empty for head-count tickets and
  // pre-MultiAttendee registrations; UI falls back to legacy aggregates above.
  attendees?: AttendeeDetail[] | null;
  // UAT R4 — confirmed-bundled add-ons (filter: Completed AND RegistrationId match).
  // Null when the registration has no qualifying add-ons; UI omits the section.
  addOns?: AddOnSummary[] | null;
}

/**
 * UAT R4 — one confirmed add-on purchase bundled with the scanned ticket.
 * Mirrors LankaConnect.Application.Events.Commands.ScanTicket.AddOnSummary.
 */
export interface AddOnSummary {
  name: string;
  quantity: number;
  unitPrice: number;
  totalAmount: number;
  currency: string;        // ISO code e.g. "USD"
}

/**
 * UAT R3 — single attendee on a ticket as projected by the scan endpoint.
 * Mirrors the server-side LankaConnect.Application.Events.Commands.ScanTicket.AttendeeDetail.
 * Stringified enums for AgeCategory and Gender keep the wire stable across schema changes.
 */
export interface AttendeeDetail {
  name: string;
  ageCategory: string;          // "Adult" | "Child"
  gender?: string | null;       // "Male" | "Female" | "Other" | null
  ticketTierName?: string | null;
  priceAmount?: number | null;  // decimal from server; null when tier was deleted post-registration
  priceCurrency?: string | null; // ISO code e.g. "USD", "LKR"
  seatLabel?: string | null;
}

/**
 * Canonical rejection reason codes shared with the server's ReasonCode constants
 * and the audit log's `rejection_reason` column. Adding a new code requires
 * matching server-side updates.
 */
export type ScanRejectionReason =
  | 'invalid_signature'
  | 'malformed_payload'
  | 'ticket_not_found'
  | 'wrong_event'
  | 'expired'
  | 'invalidated'
  | 'already_scanned'
  | 'malformed_request';

export interface UnmarkScannedResult {
  ticketCode: string;
  unmarkedAt: string;
}
