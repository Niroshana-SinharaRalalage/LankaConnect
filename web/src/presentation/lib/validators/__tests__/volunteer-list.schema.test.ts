import { describe, it, expect } from 'vitest';
import { volunteerListSchema } from '@/presentation/lib/validators/event.schemas';
import { SignUpItemType, SignUpItemCategory } from '@/infrastructure/api/types/events.types';

/**
 * Phase 7D.1 Step 20: volunteerListSchema rejects quantity-based items.
 *
 * Volunteer lists enforce the 1-volunteer = 1-slot invariant at the validation
 * boundary. The backend will also reject quantity-based volunteer items, but
 * catching it client-side gives the user a specific form error instead of a
 * generic HTTP 400 at submit time.
 */
describe('volunteerListSchema', () => {
  const validSlotItem = {
    itemDescription: 'Food Committee',
    itemType: SignUpItemType.Slot,
    itemCategory: SignUpItemCategory.Mandatory,
    availableSlots: 5,
  };

  describe('happy path', () => {
    it('accepts a list with a single slot-based role', () => {
      const result = volunteerListSchema.safeParse({
        category: 'Potluck Crew',
        description: 'Help run the community potluck',
        items: [validSlotItem],
      });

      expect(result.success).toBe(true);
    });

    it('accepts multiple slot-based roles', () => {
      const result = volunteerListSchema.safeParse({
        category: 'Event Crew',
        items: [
          { ...validSlotItem, itemDescription: 'Setup', availableSlots: 3 },
          { ...validSlotItem, itemDescription: 'Cleanup', availableSlots: 4 },
        ],
      });

      expect(result.success).toBe(true);
    });
  });

  describe('rejects quantity-based items', () => {
    it('rejects itemType=Quantity', () => {
      const result = volunteerListSchema.safeParse({
        category: 'Volunteers',
        items: [{ ...validSlotItem, itemType: SignUpItemType.Quantity }],
      });

      expect(result.success).toBe(false);
      if (!result.success) {
        // Zod points the error at items[0].itemType and names the allowed value.
        const issueJson = JSON.stringify(result.error.issues);
        expect(issueJson).toContain('itemType');
        expect(issueJson).toContain('Slot');
      }
    });

    it('rejects items that include targetQuantity', () => {
      const result = volunteerListSchema.safeParse({
        category: 'Volunteers',
        items: [
          {
            ...validSlotItem,
            targetQuantity: 10,
          },
        ],
      });

      expect(result.success).toBe(false);
    });
  });

  describe('invariants', () => {
    it('requires at least one role', () => {
      const result = volunteerListSchema.safeParse({
        category: 'Volunteers',
        items: [],
      });

      expect(result.success).toBe(false);
    });

    it('rejects availableSlots < 1', () => {
      const result = volunteerListSchema.safeParse({
        category: 'Volunteers',
        items: [{ ...validSlotItem, availableSlots: 0 }],
      });

      expect(result.success).toBe(false);
    });

    it('rejects empty category', () => {
      const result = volunteerListSchema.safeParse({
        category: '',
        items: [validSlotItem],
      });

      expect(result.success).toBe(false);
    });

    it('rejects hasOpenItems=true (volunteer roles must be organizer-defined)', () => {
      const result = volunteerListSchema.safeParse({
        category: 'Volunteers',
        hasOpenItems: true,
        items: [validSlotItem],
      });

      expect(result.success).toBe(false);
    });
  });
});
