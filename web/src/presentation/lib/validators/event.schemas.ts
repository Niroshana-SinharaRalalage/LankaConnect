import { z } from 'zod';
import { EventCategory, Currency, PricingType, RegistrationMode, EventPaymentMode, SecondaryLocationType, SignUpItemType, SignUpItemCategory } from '@/infrastructure/api/types/events.types';

/**
 * Event Validation Schemas
 * Zod schemas for event form validation matching backend CreateEventRequest requirements
 */

/**
 * Group pricing tier validation schema
 * Phase 6D: Tiered Group Pricing
 */
export const groupPricingTierSchema = z.object({
  minAttendees: z
    .number()
    .int('Minimum attendees must be a whole number')
    .min(1, 'Minimum attendees must be at least 1')
    .max(10000, 'Minimum attendees cannot exceed 10,000'),

  maxAttendees: z
    .number()
    .int('Maximum attendees must be a whole number')
    .min(1, 'Maximum attendees must be at least 1')
    .max(10000, 'Maximum attendees cannot exceed 10,000')
    .optional()
    .nullable(),

  pricePerPerson: z
    .number()
    .min(0, 'Price per person cannot be negative')
    .max(10000, 'Price per person cannot exceed $10,000'),

  currency: z.nativeEnum(Currency),
}).refine(
  (data) => {
    // If maxAttendees is provided, it must be >= minAttendees
    if (data.maxAttendees !== null && data.maxAttendees !== undefined) {
      return data.maxAttendees >= data.minAttendees;
    }
    return true;
  },
  {
    message: 'Maximum attendees must be greater than or equal to minimum attendees',
    path: ['maxAttendees'],
  }
);

export type GroupPricingTierFormData = z.infer<typeof groupPricingTierSchema>;

/**
 * Create Event Form Schema
 * Validates all fields required for event creation
 */
export const createEventSchema = z.object({
  // Basic Information
  title: z
    .string()
    .min(1, 'Event title is required')
    .min(5, 'Title must be at least 5 characters')
    .max(200, 'Title must be less than 200 characters'),

  description: z
    .string()
    .min(1, 'Event description is required')
    .refine(
      (val) => new Blob([val]).size <= 5 * 1024 * 1024,
      'Content size must be less than 5MB (including images and formatting)'
    ),

  category: z.nativeEnum(EventCategory),

  // Date and Time
  // Phase 8YA.3: Dates are optional when datesUnknown=true (TBD event).
  // When both supplied, the cross-field refines below enforce future-start
  // and end > start. When mixed (one set, one empty), the mixed-dates refine
  // rejects.
  startDate: z.string().optional().default(''),

  endDate: z.string().optional().default(''),

  // Phase 8YA.3: TBD-event toggle. When true the form submits null dates and
  // the backend creates a Planning-status event; when false both dates must
  // be provided and pass the refines below.
  datesUnknown: z.boolean().optional().default(false),

  // Capacity
  capacity: z
    .number()
    .int('Capacity must be a whole number')
    .min(1, 'Capacity must be at least 1')
    .max(10000, 'Capacity cannot exceed 10,000'),

  // Issue #51: Max attendees per registration
  maxAttendeesPerRegistration: z
    .number()
    .int('Must be a whole number')
    .min(1, 'Must be at least 1')
    .max(50, 'Cannot exceed 50')
    .optional()
    .default(10),

  // Location (Optional but recommended)
  locationAddress: z
    .string()
    .max(200, 'Address must be less than 200 characters')
    .optional()
    .or(z.literal('')),

  locationCity: z
    .string()
    .max(100, 'City must be less than 100 characters')
    .optional()
    .or(z.literal('')),

  locationState: z
    .string()
    .max(100, 'State must be less than 100 characters')
    .optional()
    .or(z.literal('')),

  locationZipCode: z
    .string()
    .regex(/^\d{5}(-\d{4})?$/, 'ZIP code must be in format 12345 or 12345-6789')
    .optional()
    .or(z.literal('')),

  locationCountry: z
    .string()
    .max(100, 'Country must be less than 100 characters')
    .optional()
    .or(z.literal('')),

  // Phase 7C.1: Primary venue name
  locationName: z
    .string()
    .max(150, 'Venue name must be less than 150 characters')
    .optional()
    .or(z.literal('')),

  // Phase 7C.1: Secondary location (parking lot or secondary venue)
  secondaryLocationType: z
    .nativeEnum(SecondaryLocationType)
    .optional()
    .nullable(),

  secondaryLocationName: z
    .string()
    .max(150, 'Secondary venue name must be less than 150 characters')
    .optional()
    .or(z.literal('')),

  secondaryLocationAddress: z
    .string()
    .max(200, 'Address must be less than 200 characters')
    .optional()
    .or(z.literal('')),

  secondaryLocationCity: z
    .string()
    .max(100, 'City must be less than 100 characters')
    .optional()
    .or(z.literal('')),

  secondaryLocationState: z
    .string()
    .max(100, 'State must be less than 100 characters')
    .optional()
    .or(z.literal('')),

  secondaryLocationZipCode: z
    .string()
    .regex(/^\d{5}(-\d{4})?$/, 'ZIP code must be in format 12345 or 12345-6789')
    .optional()
    .or(z.literal('')),

  secondaryLocationCountry: z
    .string()
    .max(100, 'Country must be less than 100 characters')
    .optional()
    .or(z.literal('')),

  // Pricing (Required)
  isFree: z.boolean(),

  // Phase 8X: Event payment mode. Optional on the wire — backend infers from isFree per
  // the architect-locked inference table when absent. Required as 'ExternalPaid' for
  // external-payment events; in that case ExternalRegistrationUrl is also required.
  paymentMode: z.nativeEnum(EventPaymentMode).optional(),

  externalRegistrationUrl: z
    .string()
    .max(2048, 'URL cannot exceed 2048 characters')
    .url('Must be a valid URL')
    .refine(u => u.toLowerCase().startsWith('https://'), 'URL must use https')
    .optional()
    .or(z.literal('')),

  externalRegistrationInstructions: z
    .string()
    .max(4000, 'Instructions cannot exceed 4000 characters')
    .optional()
    .or(z.literal('')),

  externalRegistrationVendorName: z
    .string()
    .max(100, 'Vendor name cannot exceed 100 characters')
    .optional()
    .or(z.literal('')),

  // Phase 7E.5: Per-event registration capture mode. Optional — defaults to
  // DetailedAttendees server-side when absent. Picker UI sets this; legacy create-event
  // payloads (no field) keep working unchanged.
  registrationMode: z.nativeEnum(RegistrationMode).optional(),

  // Legacy single pricing (backward compatibility)
  // Note: valueAsNumber converts empty strings to NaN, so we preprocess to handle this
  ticketPriceAmount: z
    .preprocess(
      (val) => (typeof val === 'number' && isNaN(val) ? undefined : val),
      z.number()
        .min(0, 'Price cannot be negative')
        .max(10000, 'Price cannot exceed $10,000')
        .optional()
        .nullable()
    ),

  ticketPriceCurrency: z
    .nativeEnum(Currency)
    .optional()
    .nullable(),

  // Session 21: Dual pricing (adult/child)
  enableDualPricing: z.boolean(),

  adultPriceAmount: z
    .preprocess(
      (val) => (typeof val === 'number' && isNaN(val) ? undefined : val),
      z.number()
        .min(0, 'Adult price cannot be negative')
        .max(10000, 'Adult price cannot exceed $10,000')
        .optional()
        .nullable()
    ),

  adultPriceCurrency: z
    .nativeEnum(Currency)
    .optional()
    .nullable(),

  childPriceAmount: z
    .preprocess(
      (val) => (typeof val === 'number' && isNaN(val) ? undefined : val),
      z.number()
        .min(0, 'Child price cannot be negative')
        .max(10000, 'Child price cannot exceed $10,000')
        .optional()
        .nullable()
    ),

  childPriceCurrency: z
    .nativeEnum(Currency)
    .optional()
    .nullable(),

  childAgeLimit: z
    .preprocess(
      (val) => (typeof val === 'number' && isNaN(val) ? undefined : val),
      z.number()
        .int('Age limit must be a whole number')
        .min(1, 'Age limit must be at least 1')
        .max(18, 'Age limit cannot exceed 18')
        .optional()
        .nullable()
    ),

  // Phase 6D: Group tiered pricing
  enableGroupPricing: z.boolean(),

  groupPricingTiers: z
    .array(groupPricingTierSchema)
    .optional()
    .nullable(),

  // Phase 8: Multi-tier ticketing
  enableTieredTicketing: z.boolean().default(false),

  ticketTiers: z
    .array(z.object({
      id: z.string().optional(),
      name: z.string().min(1, 'Tier name is required').max(100),
      description: z.string().max(500).default(''),
      adultPriceAmount: z.number().min(0, 'Price must be 0 or greater'),
      adultPriceCurrency: z.nativeEnum(Currency),
      childPriceAmount: z.number().min(0).optional().nullable(),
      childPriceCurrency: z.nativeEnum(Currency).optional().nullable(),
      childAgeLimit: z.number().int().min(1).max(17).optional().nullable(),
      capacity: z.number().int().min(1, 'Capacity must be at least 1'),
      maxPerUser: z.number().int().min(1).max(50).default(10),
      sortOrder: z.number().int().min(0).default(0),
      isFree: z.boolean().optional(),
    }))
    .optional()
    .nullable(),

  // Phase 6A.32: Email Groups Integration
  emailGroupIds: z
    .array(z.string().uuid('Invalid email group ID'))
    .optional()
    .nullable(),

  // Organizer Contact Details (supports multiple contacts)
  publishOrganizerContact: z.boolean().default(false),

  organizerContacts: z.array(z.object({
    contactName: z
      .string()
      .min(1, 'Contact name is required')
      .max(200, 'Contact name must be less than 200 characters'),
    contactEmail: z
      .string()
      .email('Invalid email address')
      .max(255, 'Email must be less than 255 characters')
      .optional()
      .nullable()
      .or(z.literal('')),
    contactPhone: z
      .string()
      .max(20, 'Phone number must be less than 20 characters')
      .optional()
      .nullable()
      .or(z.literal('')),
    isPrimary: z.boolean().optional().default(false),
    linkedUserId: z.string().uuid().optional().nullable(),
  })).max(10, 'Maximum 10 organizer contacts allowed').optional().default([]),
}).superRefine((data, ctx) => {
  // Phase 8YA.3: TBD-dates pair invariant + future-date + end > start, gated on
  // the datesUnknown toggle.
  // - datesUnknown=true: dates are submitted as null; skip all date refines.
  // - datesUnknown=false: both dates must be provided AND start in future AND end > start.
  if (data.datesUnknown) {
    return;
  }
  const startProvided = !!data.startDate?.trim();
  const endProvided = !!data.endDate?.trim();
  if (!startProvided && !endProvided) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Start date and time are required (or check "Dates not yet decided" for a TBD event)',
      path: ['startDate'],
    });
    return;
  }
  if (startProvided !== endProvided) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Both start and end dates must be provided together, or check "Dates not yet decided" for a TBD event',
      path: startProvided ? ['endDate'] : ['startDate'],
    });
    return;
  }
  // Both provided — run create-mode validations.
  const start = new Date(data.startDate);
  const end = new Date(data.endDate);
  if (start <= new Date()) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Start date must be in the future',
      path: ['startDate'],
    });
  }
  if (end <= start) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'End date must be after start date',
      path: ['endDate'],
    });
  }
}).refine(
  (data) => {
    // Session 33: If not free and not using dual, group, or tiered pricing, single price and currency are required
    if (!data.isFree && !data.enableDualPricing && !data.enableGroupPricing && !data.enableTieredTicketing) {
      return data.ticketPriceAmount !== null &&
             data.ticketPriceAmount !== undefined &&
             data.ticketPriceAmount > 0 &&
             data.ticketPriceCurrency !== null;
    }
    return true;
  },
  {
    message: 'Price and currency are required for paid events',
    path: ['ticketPriceAmount'],
  }
).refine(
  (data) => {
    // If using dual pricing, adult price and currency are required
    if (!data.isFree && data.enableDualPricing) {
      return data.adultPriceAmount !== null &&
             data.adultPriceAmount !== undefined &&
             data.adultPriceAmount > 0 &&
             data.adultPriceCurrency !== null;
    }
    return true;
  },
  {
    message: 'Adult price and currency are required for dual pricing',
    path: ['adultPriceAmount'],
  }
).refine(
  (data) => {
    // If using dual pricing, child price and age limit are required
    if (!data.isFree && data.enableDualPricing) {
      return data.childPriceAmount !== null &&
             data.childPriceAmount !== undefined &&
             data.childPriceAmount >= 0 &&
             data.childPriceCurrency !== null &&
             data.childAgeLimit !== null &&
             data.childAgeLimit !== undefined &&
             data.childAgeLimit >= 1 &&
             data.childAgeLimit <= 18;
    }
    return true;
  },
  {
    message: 'Child price, currency, and age limit (1-18) are required for dual pricing',
    path: ['childPriceAmount'],
  }
).refine(
  (data) => {
    // If using dual pricing, child price cannot exceed adult price
    if (!data.isFree && data.enableDualPricing &&
        data.adultPriceAmount !== null && data.adultPriceAmount !== undefined &&
        data.childPriceAmount !== null && data.childPriceAmount !== undefined) {
      return data.childPriceAmount <= data.adultPriceAmount;
    }
    return true;
  },
  {
    message: 'Child price cannot exceed adult price',
    path: ['childPriceAmount'],
  }
).refine(
  (data) => {
    // If using dual pricing, both prices must use the same currency
    if (!data.isFree && data.enableDualPricing &&
        data.adultPriceCurrency !== null && data.childPriceCurrency !== null) {
      return data.adultPriceCurrency === data.childPriceCurrency;
    }
    return true;
  },
  {
    message: 'Adult and child prices must use the same currency',
    path: ['childPriceCurrency'],
  }
).refine(
  (data) => {
    // Phase 6D: If using group pricing, at least one tier is required
    if (!data.isFree && data.enableGroupPricing) {
      return data.groupPricingTiers !== null &&
             data.groupPricingTiers !== undefined &&
             data.groupPricingTiers.length > 0;
    }
    return true;
  },
  {
    message: 'At least one pricing tier is required for group pricing',
    path: ['groupPricingTiers'],
  }
).refine(
  (data) => {
    // Phase 6D: All tiers in group pricing must use the same currency
    if (!data.isFree && data.enableGroupPricing &&
        data.groupPricingTiers && data.groupPricingTiers.length > 1) {
      const firstCurrency = data.groupPricingTiers[0].currency;
      return data.groupPricingTiers.every(tier => tier.currency === firstCurrency);
    }
    return true;
  },
  {
    message: 'All pricing tiers must use the same currency',
    path: ['groupPricingTiers'],
  }
).refine(
  (data) => {
    // Phase 6D: First tier must start at 1 attendee
    if (!data.isFree && data.enableGroupPricing &&
        data.groupPricingTiers && data.groupPricingTiers.length > 0) {
      const sortedTiers = [...data.groupPricingTiers].sort((a, b) => a.minAttendees - b.minAttendees);
      return sortedTiers[0].minAttendees === 1;
    }
    return true;
  },
  {
    message: 'First pricing tier must start at 1 attendee',
    path: ['groupPricingTiers'],
  }
).refine(
  (data) => {
    // Phase 6D: Tiers must not overlap and have no gaps
    if (!data.isFree && data.enableGroupPricing &&
        data.groupPricingTiers && data.groupPricingTiers.length > 1) {
      const sortedTiers = [...data.groupPricingTiers].sort((a, b) => a.minAttendees - b.minAttendees);

      for (let i = 0; i < sortedTiers.length - 1; i++) {
        const currentTier = sortedTiers[i];
        const nextTier = sortedTiers[i + 1];

        // Current tier must have maxAttendees (unless it's the last tier)
        if (currentTier.maxAttendees === null || currentTier.maxAttendees === undefined) {
          return false; // Only last tier can be unlimited
        }

        // Check for gaps: next tier must start at current tier's max + 1
        if (nextTier.minAttendees !== currentTier.maxAttendees + 1) {
          return false;
        }
      }
      return true;
    }
    return true;
  },
  {
    message: 'Pricing tiers must not have gaps or overlaps. Each tier must start where the previous ends.',
    path: ['groupPricingTiers'],
  }
).refine(
  (data) => {
    // Phase 6D + Phase 8: Only one pricing mode can be enabled at a time
    const modesEnabled = [data.enableDualPricing, data.enableGroupPricing, data.enableTieredTicketing].filter(Boolean).length;
    if (!data.isFree && modesEnabled > 1) {
      return false;
    }
    return true;
  },
  {
    message: 'Only one pricing mode can be enabled at a time (single, dual, group, or tiered)',
    path: ['enableGroupPricing'],
  }
).refine(
  (data) => {
    // Phase 8: If using tiered ticketing, at least one tier is required
    if (!data.isFree && data.enableTieredTicketing) {
      return data.ticketTiers !== null &&
             data.ticketTiers !== undefined &&
             data.ticketTiers.length > 0;
    }
    return true;
  },
  {
    message: 'At least one ticket tier is required for tiered ticketing',
    path: ['ticketTiers'],
  }
).refine(
  (data) => {
    // If publishing organizer contact, at least one contact with valid data is required
    if (data.publishOrganizerContact) {
      const contacts = data.organizerContacts || [];
      if (contacts.length === 0) return false;
      // Each contact must have name and at least email or phone
      return contacts.every(c => {
        const hasName = c.contactName && c.contactName.trim().length > 0;
        const hasEmail = c.contactEmail && c.contactEmail.trim().length > 0;
        const hasPhone = c.contactPhone && c.contactPhone.trim().length > 0;
        return hasName && (hasEmail || hasPhone);
      });
    }
    return true;
  },
  {
    message: 'At least one organizer contact with name and contact method (email or phone) is required',
    path: ['organizerContacts'],
  }
).superRefine((data, ctx) => {
  // Phase 7C.1: If secondary location type is selected, address and city are required
  if (data.secondaryLocationType) {
    const address = (data.secondaryLocationAddress ?? '').trim();
    const city = (data.secondaryLocationCity ?? '').trim();
    if (!address) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'Secondary location address is required when a secondary location type is selected',
        path: ['secondaryLocationAddress'],
      });
    }
    if (!city) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'Secondary location city is required when a secondary location type is selected',
        path: ['secondaryLocationCity'],
      });
    }
  }
});

export type CreateEventFormData = z.infer<typeof createEventSchema>;

/**
 * Base Edit Event Schema (without refinements)
 * Same structure as create but without future date requirement
 */
const baseEditEventSchema = z.object({
  // Basic Information
  title: z
    .string()
    .min(1, 'Event title is required')
    .min(5, 'Title must be at least 5 characters')
    .max(200, 'Title must be less than 200 characters'),

  description: z
    .string()
    .min(1, 'Event description is required')
    .refine(
      (val) => new Blob([val]).size <= 5 * 1024 * 1024,
      'Content size must be less than 5MB (including images and formatting)'
    ),

  category: z.nativeEnum(EventCategory),

  // Date and Time - NO future date validation for edit mode (events being edited may
  // be ongoing or past). Phase 8YA.3: dates optional when datesUnknown=true.
  startDate: z.string().optional().default(''),
  endDate: z.string().optional().default(''),

  // Phase 8YA.3: TBD-event toggle.
  datesUnknown: z.boolean().optional().default(false),

  // Capacity
  capacity: z
    .number()
    .int('Capacity must be a whole number')
    .min(1, 'Capacity must be at least 1')
    .max(10000, 'Capacity cannot exceed 10,000'),

  // Issue #51: Max attendees per registration
  maxAttendeesPerRegistration: z
    .number()
    .int('Must be a whole number')
    .min(1, 'Must be at least 1')
    .max(50, 'Cannot exceed 50')
    .optional()
    .default(10),

  // Location (Optional)
  locationAddress: z
    .string()
    .max(200, 'Address must be less than 200 characters')
    .optional()
    .or(z.literal('')),

  locationCity: z
    .string()
    .max(100, 'City must be less than 100 characters')
    .optional()
    .or(z.literal('')),

  locationState: z
    .string()
    .max(100, 'State must be less than 100 characters')
    .optional()
    .or(z.literal('')),

  locationZipCode: z
    .string()
    .regex(/^\d{5}(-\d{4})?$/, 'ZIP code must be in format 12345 or 12345-6789')
    .optional()
    .or(z.literal('')),

  locationCountry: z
    .string()
    .max(100, 'Country must be less than 100 characters')
    .optional()
    .or(z.literal('')),

  // Phase 7C.1: Primary venue name
  locationName: z
    .string()
    .max(150, 'Venue name must be less than 150 characters')
    .optional()
    .or(z.literal('')),

  // Phase 7C.1: Secondary location (parking lot or secondary venue)
  secondaryLocationType: z
    .nativeEnum(SecondaryLocationType)
    .optional()
    .nullable(),

  secondaryLocationName: z
    .string()
    .max(150, 'Secondary venue name must be less than 150 characters')
    .optional()
    .or(z.literal('')),

  secondaryLocationAddress: z
    .string()
    .max(200, 'Address must be less than 200 characters')
    .optional()
    .or(z.literal('')),

  secondaryLocationCity: z
    .string()
    .max(100, 'City must be less than 100 characters')
    .optional()
    .or(z.literal('')),

  secondaryLocationState: z
    .string()
    .max(100, 'State must be less than 100 characters')
    .optional()
    .or(z.literal('')),

  secondaryLocationZipCode: z
    .string()
    .regex(/^\d{5}(-\d{4})?$/, 'ZIP code must be in format 12345 or 12345-6789')
    .optional()
    .or(z.literal('')),

  secondaryLocationCountry: z
    .string()
    .max(100, 'Country must be less than 100 characters')
    .optional()
    .or(z.literal('')),

  // Pricing
  isFree: z.boolean(),

  // Phase 8X: Event payment mode (mirrored on the edit schema). Same rules as create.
  paymentMode: z.nativeEnum(EventPaymentMode).optional(),

  externalRegistrationUrl: z
    .string()
    .max(2048, 'URL cannot exceed 2048 characters')
    .url('Must be a valid URL')
    .refine(u => u.toLowerCase().startsWith('https://'), 'URL must use https')
    .optional()
    .or(z.literal('')),

  externalRegistrationInstructions: z
    .string()
    .max(4000, 'Instructions cannot exceed 4000 characters')
    .optional()
    .or(z.literal('')),

  externalRegistrationVendorName: z
    .string()
    .max(100, 'Vendor name cannot exceed 100 characters')
    .optional()
    .or(z.literal('')),

  // Phase 7E.5: Per-event registration capture mode (mirrored on the edit schema).
  registrationMode: z.nativeEnum(RegistrationMode).optional(),

  // Note: valueAsNumber converts empty strings to NaN, so we preprocess to handle this
  ticketPriceAmount: z
    .preprocess(
      (val) => (typeof val === 'number' && isNaN(val) ? undefined : val),
      z.number()
        .min(0, 'Price cannot be negative')
        .max(10000, 'Price cannot exceed $10,000')
        .optional()
        .nullable()
    ),

  ticketPriceCurrency: z
    .nativeEnum(Currency)
    .optional()
    .nullable(),

  // Dual pricing (adult/child)
  enableDualPricing: z.boolean(),

  adultPriceAmount: z
    .preprocess(
      (val) => (typeof val === 'number' && isNaN(val) ? undefined : val),
      z.number()
        .min(0, 'Adult price cannot be negative')
        .max(10000, 'Adult price cannot exceed $10,000')
        .optional()
        .nullable()
    ),

  adultPriceCurrency: z
    .nativeEnum(Currency)
    .optional()
    .nullable(),

  childPriceAmount: z
    .preprocess(
      (val) => (typeof val === 'number' && isNaN(val) ? undefined : val),
      z.number()
        .min(0, 'Child price cannot be negative')
        .max(10000, 'Child price cannot exceed $10,000')
        .optional()
        .nullable()
    ),

  childPriceCurrency: z
    .nativeEnum(Currency)
    .optional()
    .nullable(),

  childAgeLimit: z
    .preprocess(
      (val) => (typeof val === 'number' && isNaN(val) ? undefined : val),
      z.number()
        .int('Age limit must be a whole number')
        .min(1, 'Age limit must be at least 1')
        .max(18, 'Age limit cannot exceed 18')
        .optional()
        .nullable()
    ),

  // Group tiered pricing
  enableGroupPricing: z.boolean(),

  groupPricingTiers: z
    .array(groupPricingTierSchema)
    .optional()
    .nullable(),

  // Phase 8: Multi-tier ticketing (same schema as create)
  enableTieredTicketing: z.boolean().default(false),

  ticketTiers: z
    .array(z.object({
      id: z.string().optional(),
      name: z.string().min(1).max(100),
      description: z.string().max(500).default(''),
      adultPriceAmount: z.number().min(0),
      adultPriceCurrency: z.nativeEnum(Currency),
      childPriceAmount: z.number().min(0).optional().nullable(),
      childPriceCurrency: z.nativeEnum(Currency).optional().nullable(),
      childAgeLimit: z.number().int().min(1).max(17).optional().nullable(),
      capacity: z.number().int().min(1),
      maxPerUser: z.number().int().min(1).max(50).default(10),
      sortOrder: z.number().int().min(0).default(0),
      isFree: z.boolean().optional(),
    }))
    .optional()
    .nullable(),

  // Phase 6A.32: Email Groups Integration
  emailGroupIds: z
    .array(z.string().uuid('Invalid email group ID'))
    .optional()
    .nullable(),

  // Organizer Contact Details (supports multiple contacts)
  publishOrganizerContact: z.boolean().default(false),

  organizerContacts: z.array(z.object({
    contactName: z
      .string()
      .min(1, 'Contact name is required')
      .max(200, 'Contact name must be less than 200 characters'),
    contactEmail: z
      .string()
      .email('Invalid email address')
      .max(255, 'Email must be less than 255 characters')
      .optional()
      .nullable()
      .or(z.literal('')),
    contactPhone: z
      .string()
      .max(20, 'Phone number must be less than 20 characters')
      .optional()
      .nullable()
      .or(z.literal('')),
    isPrimary: z.boolean().optional().default(false),
    linkedUserId: z.string().uuid().optional().nullable(),
  })).max(10, 'Maximum 10 organizer contacts allowed').optional().default([]),
});

/**
 * Edit Event Form Schema
 * Same as CreateEventFormData but without future date requirement (since events being edited may be ongoing or past)
 * Note: We must redefine refinements because .omit().extend() loses them
 */
export const editEventSchema = baseEditEventSchema.superRefine((data, ctx) => {
  // Phase 8YA.3: TBD-dates pair invariant for edit. No future-date check (edit
  // mode allows historical events). Same gating on datesUnknown toggle as create.
  if (data.datesUnknown) {
    return;
  }
  const startProvided = !!data.startDate?.trim();
  const endProvided = !!data.endDate?.trim();
  if (!startProvided && !endProvided) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Start date and time are required (or check "Dates not yet decided" for a TBD event)',
      path: ['startDate'],
    });
    return;
  }
  if (startProvided !== endProvided) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Both start and end dates must be provided together, or check "Dates not yet decided" for a TBD event',
      path: startProvided ? ['endDate'] : ['startDate'],
    });
    return;
  }
  const start = new Date(data.startDate);
  const end = new Date(data.endDate);
  if (end <= start) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'End date must be after start date',
      path: ['endDate'],
    });
  }
}).refine(
  (data) => {
    // If not free and not using any advanced pricing mode, single price and currency are required
    if (!data.isFree && !data.enableDualPricing && !data.enableGroupPricing && !data.enableTieredTicketing) {
      return data.ticketPriceAmount !== null &&
             data.ticketPriceAmount !== undefined &&
             data.ticketPriceAmount > 0 &&
             data.ticketPriceCurrency !== null;
    }
    return true;
  },
  {
    message: 'Price and currency are required for paid events',
    path: ['ticketPriceAmount'],
  }
).refine(
  (data) => {
    // If using dual pricing, adult price and currency are required
    if (!data.isFree && data.enableDualPricing) {
      return data.adultPriceAmount !== null &&
             data.adultPriceAmount !== undefined &&
             data.adultPriceAmount > 0 &&
             data.adultPriceCurrency !== null;
    }
    return true;
  },
  {
    message: 'Adult price and currency are required for dual pricing',
    path: ['adultPriceAmount'],
  }
).refine(
  (data) => {
    // If using dual pricing, child price and age limit are required
    if (!data.isFree && data.enableDualPricing) {
      return data.childPriceAmount !== null &&
             data.childPriceAmount !== undefined &&
             data.childPriceAmount >= 0 &&
             data.childPriceCurrency !== null &&
             data.childAgeLimit !== null &&
             data.childAgeLimit !== undefined &&
             data.childAgeLimit >= 1 &&
             data.childAgeLimit <= 18;
    }
    return true;
  },
  {
    message: 'Child price, currency, and age limit (1-18) are required for dual pricing',
    path: ['childPriceAmount'],
  }
).refine(
  (data) => {
    // If using dual pricing, child price cannot exceed adult price
    if (!data.isFree && data.enableDualPricing &&
        data.adultPriceAmount !== null && data.adultPriceAmount !== undefined &&
        data.childPriceAmount !== null && data.childPriceAmount !== undefined) {
      return data.childPriceAmount <= data.adultPriceAmount;
    }
    return true;
  },
  {
    message: 'Child price cannot exceed adult price',
    path: ['childPriceAmount'],
  }
).refine(
  (data) => {
    // If using dual pricing, both prices must use the same currency
    if (!data.isFree && data.enableDualPricing &&
        data.adultPriceCurrency !== null && data.childPriceCurrency !== null) {
      return data.adultPriceCurrency === data.childPriceCurrency;
    }
    return true;
  },
  {
    message: 'Adult and child prices must use the same currency',
    path: ['childPriceCurrency'],
  }
).refine(
  (data) => {
    // If using group pricing, at least one tier is required
    if (!data.isFree && data.enableGroupPricing) {
      return data.groupPricingTiers !== null &&
             data.groupPricingTiers !== undefined &&
             data.groupPricingTiers.length > 0;
    }
    return true;
  },
  {
    message: 'At least one pricing tier is required for group pricing',
    path: ['groupPricingTiers'],
  }
).refine(
  (data) => {
    // Only one pricing mode can be enabled at a time
    const modesEnabled = [data.enableDualPricing, data.enableGroupPricing, data.enableTieredTicketing].filter(Boolean).length;
    if (!data.isFree && modesEnabled > 1) {
      return false;
    }
    return true;
  },
  {
    message: 'Only one pricing mode can be enabled at a time (single, dual, group, or tiered)',
    path: ['enableGroupPricing'],
  }
).refine(
  (data) => {
    // Phase 8: If using tiered ticketing, at least one tier is required
    if (!data.isFree && data.enableTieredTicketing) {
      return data.ticketTiers !== null &&
             data.ticketTiers !== undefined &&
             data.ticketTiers.length > 0;
    }
    return true;
  },
  {
    message: 'At least one ticket tier is required for multi-tier ticketing',
    path: ['ticketTiers'],
  }
).refine(
  (data) => {
    // If publishing organizer contact, at least one contact with valid data is required
    if (data.publishOrganizerContact) {
      const contacts = data.organizerContacts || [];
      if (contacts.length === 0) return false;
      return contacts.every(c => {
        const hasName = c.contactName && c.contactName.trim().length > 0;
        const hasEmail = c.contactEmail && c.contactEmail.trim().length > 0;
        const hasPhone = c.contactPhone && c.contactPhone.trim().length > 0;
        return hasName && (hasEmail || hasPhone);
      });
    }
    return true;
  },
  {
    message: 'At least one organizer contact with name and contact method (email or phone) is required',
    path: ['organizerContacts'],
  }
).superRefine((data, ctx) => {
  // Phase 7C.1: If secondary location type is selected, address and city are required
  if (data.secondaryLocationType) {
    const address = (data.secondaryLocationAddress ?? '').trim();
    const city = (data.secondaryLocationCity ?? '').trim();
    if (!address) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'Secondary location address is required when a secondary location type is selected',
        path: ['secondaryLocationAddress'],
      });
    }
    if (!city) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'Secondary location city is required when a secondary location type is selected',
        path: ['secondaryLocationCity'],
      });
    }
  }
});

export type EditEventFormData = z.infer<typeof editEventSchema>;

/**
 * Phase 7D.1: Volunteer-list validation schema.
 *
 * Volunteer lists are a constrained flavour of sign-up list where every item
 * is slot-based (1 volunteer = 1 slot). Quantity-based items are rejected at
 * the validation boundary so the volunteer UI never sends a payload the
 * backend would refuse. Schema rejects instead of silently coercing so the
 * user sees an obvious form error rather than a cryptic API failure.
 */
export const volunteerRoleItemSchema = z.object({
  itemDescription: z
    .string()
    .min(1, 'Volunteer role is required')
    .max(200, 'Volunteer role must be less than 200 characters'),
  itemType: z.literal(SignUpItemType.Slot, {
    message: 'Volunteer lists only support slot-based roles',
  }),
  itemCategory: z.nativeEnum(SignUpItemCategory),
  availableSlots: z
    .number({ message: 'Volunteers needed must be a whole number' })
    .int('Volunteers needed must be a whole number')
    .min(1, 'At least 1 volunteer slot is required')
    .max(500, 'Volunteer slots cannot exceed 500'),
  suggestedPerSlot: z.number().optional().nullable(),
  notes: z
    .string()
    .max(1000, 'Notes must be less than 1000 characters')
    .optional()
    .nullable()
    .or(z.literal('')),
  // Accept but forbid quantity-based fields. Sending targetQuantity flips the
  // item into quantity-based mode server-side; the schema refuses before the
  // request leaves the browser.
  targetQuantity: z.undefined().optional(),
});

export const volunteerListSchema = z.object({
  category: z
    .string()
    .min(1, 'Volunteer list name is required')
    .max(100, 'Volunteer list name must be less than 100 characters'),
  description: z
    .string()
    .max(1000, 'Description must be less than 1000 characters')
    .optional()
    .default(''),
  hasOpenItems: z
    .literal(false, { message: 'Volunteer lists cannot allow user-added roles' })
    .default(false),
  items: z
    .array(volunteerRoleItemSchema)
    .min(1, 'At least one volunteer role is required'),
});

export type VolunteerListFormData = z.infer<typeof volunteerListSchema>;
