import { z } from 'zod';

/**
 * Newsletter Validation Schemas
 * Phase 6A.74: Newsletter/News Alert Feature
 * Zod schemas for newsletter form validation matching backend requests
 */

export const createNewsletterSchema = z.object({
  title: z
    .string()
    .min(1, 'Newsletter title is required')
    .min(5, 'Title must be at least 5 characters')
    .max(200, 'Title must be less than 200 characters'),

  description: z
    .string()
    .min(1, 'Newsletter description is required')
    .min(20, 'Description must be at least 20 characters')
    .max(50000, 'Description must be less than 50000 characters'),

  emailGroupIds: z
    .array(z.string().uuid('Invalid email group ID'))
    .optional(),

  includeNewsletterSubscribers: z
    .boolean(),

  eventId: z
    .string()
    .optional()
    .refine((val) => !val || val === '' || z.string().uuid().safeParse(val).success, {
      message: 'Invalid event ID',
    }),

  targetAllLocations: z
    .boolean(),

  metroAreaIds: z
    .array(z.string().uuid('Invalid metro area ID'))
    .optional(),
}).refine(
  (data) => {
    // Must have at least one recipient source
    const hasEmailGroups = data.emailGroupIds && data.emailGroupIds.length > 0;
    const hasNewsletterSubscribers = data.includeNewsletterSubscribers === true;

    // Phase 6A.74 Part 11 Issue #2 Fix: Event ID is NOT a recipient source
    // Event linkage is purely optional metadata - doesn't affect recipient validation
    // At least one of: email groups OR newsletter subscribers must be selected
    return hasEmailGroups || hasNewsletterSubscribers;
  },
  {
    message: 'Must have at least one recipient source: select email groups or keep newsletter subscribers checked',
    path: ['emailGroupIds'], // Error displays on email groups field
  }
).refine(
  (data) => {
    // Phase 6A.74 Part 13 Issue #6 FIX: Location validation only applies if targetAllLocations is false
    // If targetAllLocations is true OR event is selected, no metro area validation needed
    if (data.targetAllLocations || (data.eventId && data.eventId !== '')) {
      return true;
    }

    // If targetAllLocations is false and no event, must select at least one metro area
    return data.metroAreaIds && data.metroAreaIds.length > 0;
  },
  {
    message: 'Please select at least one location or check "Target All Locations"',
    path: ['metroAreaIds'], // Error displays on metro areas field
  }
);

export type CreateNewsletterFormData = z.infer<typeof createNewsletterSchema>;

// Update schema is identical to create schema
export const updateNewsletterSchema = createNewsletterSchema;

export type UpdateNewsletterFormData = z.infer<typeof updateNewsletterSchema>;
