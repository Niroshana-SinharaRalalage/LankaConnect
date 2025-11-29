import { z } from 'zod';
import { EventCategory, Currency } from '@/infrastructure/api/types/events.types';

/**
 * Event Validation Schemas
 * Zod schemas for event form validation matching backend CreateEventRequest requirements
 */

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
    .min(20, 'Description must be at least 20 characters')
    .max(5000, 'Description must be less than 5000 characters'),

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

  ticketPriceAmount: z
    .number()
    .min(0, 'Price cannot be negative')
    .max(10000, 'Price cannot exceed $10,000')
    .optional()
    .nullable(),

  ticketPriceCurrency: z
    .nativeEnum(Currency)
    .optional()
    .nullable(),
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
    // If not free, price and currency are required
    if (!data.isFree) {
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
);

export type CreateEventFormData = z.infer<typeof createEventSchema>;
