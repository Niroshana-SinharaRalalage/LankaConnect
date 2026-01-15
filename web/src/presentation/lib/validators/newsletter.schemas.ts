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
    .uuid('Invalid event ID')
    .optional(),

  targetAllLocations: z
    .boolean(),

  metroAreaIds: z
    .array(z.string().uuid('Invalid metro area ID'))
    .optional(),
}).refine(
  (data) => {
    // Must have at least one recipient source
    const hasEmailGroups = data.emailGroupIds && data.emailGroupIds.length > 0;
    const hasNewsletterSubscribers = data.includeNewsletterSubscribers;
    const hasEvent = data.eventId !== null && data.eventId !== undefined;

    return hasEmailGroups || hasNewsletterSubscribers || hasEvent;
  },
  {
    message: 'Must select at least one recipient source: email groups, newsletter subscribers, or link to an event',
    path: ['emailGroupIds'],
  }
);

export type CreateNewsletterFormData = z.infer<typeof createNewsletterSchema>;

// Update schema is identical to create schema
export const updateNewsletterSchema = createNewsletterSchema;

export type UpdateNewsletterFormData = z.infer<typeof updateNewsletterSchema>;
