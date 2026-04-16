import { z } from 'zod';
import { EventCategory, Currency, PricingType } from '@/infrastructure/api/types/events.types';

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
  startDate: z
    .string()
    .min(1, 'Start date and time are required')
    .refine((date) => {
      const selectedDate = new Date(date);
      const now = new Date();
      return selectedDate > now;
    }, 'Start date must be in the future'),

  endDate: z
    .string()
    .min(1, 'End date and time are required'),

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

  // Pricing (Required)
  isFree: z.boolean(),

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
}).refine(
  (data) => {
    // Validate that end date is after start date
    const start = new Date(data.startDate);
    const end = new Date(data.endDate);
    return end > start;
  },
  {
    message: 'End date must be after start date',
    path: ['endDate'],
  }
).refine(
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
);

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

  // Date and Time - NO future date validation for edit mode
  startDate: z
    .string()
    .min(1, 'Start date and time are required'),

  endDate: z
    .string()
    .min(1, 'End date and time are required'),

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

  // Pricing
  isFree: z.boolean(),

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
export const editEventSchema = baseEditEventSchema.refine(
  (data) => {
    // Validate that end date is after start date
    const start = new Date(data.startDate);
    const end = new Date(data.endDate);
    return end > start;
  },
  {
    message: 'End date must be after start date',
    path: ['endDate'],
  }
).refine(
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
);

export type EditEventFormData = z.infer<typeof editEventSchema>;
