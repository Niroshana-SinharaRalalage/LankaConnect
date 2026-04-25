import { z } from 'zod';

/**
 * WhatsApp Validation Schemas
 * Phase 7A.4: WhatsApp Integration Frontend
 * Zod schemas for WhatsApp form validation matching backend requests
 */

/**
 * E.164 phone number regex
 * Matches: +1234567890 (country code + number, 1-14 digits after +)
 * Used by backend UserWhatsAppPreferences.EnableWhatsApp()
 */
const E164_PHONE_REGEX = /^\+[1-9]\d{1,14}$/;

/**
 * Schema for enabling WhatsApp with phone number
 */
export const enableWhatsAppSchema = z.object({
  phoneNumber: z
    .string()
    .min(1, 'Phone number is required')
    .regex(E164_PHONE_REGEX, 'Phone number must be in E.164 format (e.g., +12345678901)'),
});

export type EnableWhatsAppFormData = z.infer<typeof enableWhatsAppSchema>;

/**
 * Schema for verifying phone with 6-digit code
 */
export const verifyPhoneSchema = z.object({
  code: z
    .string()
    .min(1, 'Verification code is required')
    .length(6, 'Verification code must be 6 digits')
    .regex(/^\d{6}$/, 'Verification code must be 6 digits'),
});

export type VerifyPhoneFormData = z.infer<typeof verifyPhoneSchema>;

// Empty-string submissions from <input type="time"> / <select> without a value
// fail .NET TimeOnly?/string? binding with HTTP 400 before the action runs.
// Normalize "" -> null at the validation boundary so the API receives valid JSON.
const nullableTrimmedString = z
  .string()
  .optional()
  .nullable()
  .transform((value) => (value ? value : null));

/**
 * Schema for updating WhatsApp notification preferences
 */
export const updatePreferencesSchema = z.object({
  eventRegistration: z.boolean(),
  eventReminder: z.boolean(),
  eventCancellation: z.boolean(),
  eventUpdate: z.boolean(),
  signupCommitment: z.boolean(),
  refund: z.boolean(),
  newsletter: z.boolean(),
  newEvent: z.boolean(),
  payment: z.boolean(),
  preferredLanguage: nullableTrimmedString,
  quietHoursStart: nullableTrimmedString,
  quietHoursEnd: nullableTrimmedString,
  respectCulturalTiming: z.boolean(),
});

// z.input is the shape react-hook-form sees (empty strings allowed from <input type="time">).
// z.infer is the post-transform shape — what handleSubmit delivers and what we send to the API.
export type UpdatePreferencesFormInput = z.input<typeof updatePreferencesSchema>;
export type UpdatePreferencesFormData = z.infer<typeof updatePreferencesSchema>;

/**
 * Helper to strip non-digit characters and format as E.164
 * Converts user-friendly formats like "+1 (234) 567-8901" to "+12345678901"
 */
export function toE164(phone: string): string {
  // Keep the + prefix and strip everything else except digits
  const hasPlus = phone.startsWith('+');
  const digits = phone.replace(/\D/g, '');
  return hasPlus ? `+${digits}` : `+${digits}`;
}
