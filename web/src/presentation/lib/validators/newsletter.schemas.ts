import { z } from 'zod';

/**
 * Newsletter Validation Schemas
 * Phase 6A.74: Newsletter/News Alert Feature
 * Zod schemas for newsletter form validation matching backend requests
 */

// Base schema for form validation (used with react-hook-form)
const newsletterBaseSchema = z.object({
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

  // Allow empty string for form's "No event linkage" option
  // Will be transformed to undefined before API submission
  eventId: z
    .string()
    .optional()
    .refine((val) => !val || val === '' || z.string().uuid().safeParse(val).success, {
      message: 'Invalid event ID',
    }),

  targetAllLocations: z
    .boolean(),

  // Allow any string array, will be filtered to valid UUIDs before API submission
  metroAreaIds: z
    .array(z.string())
    .optional(),

  // Phase 6A.74 Part 14: Announcement-only newsletters
  // When true: Auto-activates on creation, NOT visible on /newsletters public page
  // When false (default): Creates in Draft status, must publish to make visible
  isAnnouncementOnly: z
    .boolean(),
});

// Add the recipient validation refinement
export const createNewsletterSchema = newsletterBaseSchema.refine(
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
);

// Phase 6A.74 Part 13 Issue #6 CRITICAL FIX: Location validation COMPLETELY REMOVED
// Location targeting is OPTIONAL - users can create newsletters without selecting locations
// They just need at least one recipient source (email groups OR subscribers)
// No location validation refine needed at all

export type CreateNewsletterFormData = z.infer<typeof createNewsletterSchema>;

/**
 * Clean form data for API submission
 * Transforms empty strings to undefined and filters invalid UUIDs
 */
export function cleanNewsletterDataForApi(data: CreateNewsletterFormData): CreateNewsletterFormData {
  const uuidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

  return {
    ...data,
    // Transform empty string eventId to undefined
    eventId: data.eventId && data.eventId.trim() !== '' ? data.eventId : undefined,
    // Transform empty emailGroupIds array to undefined
    emailGroupIds: data.emailGroupIds && data.emailGroupIds.length > 0 ? data.emailGroupIds : undefined,
    // Transform empty metroAreaIds array to undefined and filter to valid UUIDs
    metroAreaIds: data.metroAreaIds && data.metroAreaIds.length > 0
      ? data.metroAreaIds.filter(id => uuidRegex.test(id))
      : undefined,
    // Phase 6A.74 Part 14: Include isAnnouncementOnly (defaults to false)
    isAnnouncementOnly: data.isAnnouncementOnly ?? false,
  };
}

// Update schema is identical to create schema
export const updateNewsletterSchema = createNewsletterSchema;

export type UpdateNewsletterFormData = z.infer<typeof updateNewsletterSchema>;
