/**
 * Email Groups API Types
 * Phase 6A.25: Email Groups Management
 */

/**
 * Email Group DTO matching backend EmailGroupDto
 */
export interface EmailGroupDto {
  id: string;
  name: string;
  description?: string;
  ownerId: string;
  ownerName: string;
  emailAddresses: string;
  emailCount: number;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

/**
 * Create email group request DTO
 */
export interface CreateEmailGroupRequest {
  name: string;
  description?: string;
  emailAddresses: string;
}

/**
 * Update email group request DTO
 */
export interface UpdateEmailGroupRequest {
  name: string;
  description?: string;
  emailAddresses: string;
}

/**
 * Helper function to parse email addresses from comma-separated string
 */
export function parseEmailAddresses(emailAddresses: string): string[] {
  if (!emailAddresses || !emailAddresses.trim()) {
    return [];
  }
  return emailAddresses
    .split(',')
    .map((email) => email.trim())
    .filter((email) => email.length > 0);
}

/**
 * Helper function to validate email format
 */
export function isValidEmail(email: string): boolean {
  const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
  return emailRegex.test(email);
}

/**
 * Helper function to validate all email addresses in a comma-separated string
 * Returns an object with valid emails, invalid emails, duplicates, and whether all are valid
 */
export function validateEmailAddresses(emailAddresses: string): {
  valid: string[];
  invalid: string[];
  duplicates: string[];
  uniqueCount: number;
  allValid: boolean;
} {
  const emails = parseEmailAddresses(emailAddresses);
  const valid: string[] = [];
  const invalid: string[] = [];
  const duplicates: string[] = [];
  const seen = new Set<string>();

  emails.forEach((email) => {
    const normalizedEmail = email.toLowerCase();

    if (!isValidEmail(email)) {
      invalid.push(email);
    } else if (seen.has(normalizedEmail)) {
      // Email is valid but already seen - it's a duplicate
      if (!duplicates.includes(normalizedEmail)) {
        duplicates.push(normalizedEmail);
      }
    } else {
      seen.add(normalizedEmail);
      valid.push(email);
    }
  });

  return {
    valid,
    invalid,
    duplicates,
    uniqueCount: valid.length,
    allValid: invalid.length === 0 && duplicates.length === 0 && valid.length > 0,
  };
}
