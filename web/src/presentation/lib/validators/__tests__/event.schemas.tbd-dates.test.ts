import { describe, it, expect } from 'vitest';
import {
  createEventSchema,
  editEventSchema,
} from '@/presentation/lib/validators/event.schemas';
import { Currency, EventCategory } from '@/infrastructure/api/types/events.types';

/**
 * Phase 8YA.3 — TBD-dates support in event create/edit zod schemas.
 *
 * Architect-locked behavior (matches Domain + Application layers per Phase 1+2):
 * - `datesUnknown === true` (or both startDate/endDate empty) → schema accepts the form
 *   without raising start/end-date errors. Submission produces { startDate: null, endDate: null }.
 * - `datesUnknown === false` AND both dates supplied → schema validates start in future
 *   (create only) AND end > start.
 * - Mixed (one date set, one empty) → schema rejects.
 *
 * Pinning these in tests prevents future schema refactors from quietly re-requiring dates.
 */
describe('createEventSchema — TBD dates (Phase 8YA.3)', () => {
  // Minimal valid scaffold: pulls in all the unrelated required fields so the
  // tests can focus on the date logic without superRefine errors leaking in.
  const baseValid = {
    title: 'A community event title',
    description: 'A meaningful description with enough characters',
    category: EventCategory.Community,
    capacity: 50,
    isFree: true,
    enableDualPricing: false,
    enableGroupPricing: false,
    enableTieredTicketing: false,
    publishOrganizerContact: false,
  } as const;

  describe('happy paths', () => {
    it('accepts both dates set with future start + end > start', () => {
      const start = new Date(Date.now() + 7 * 86400_000).toISOString();
      const end = new Date(Date.now() + 8 * 86400_000).toISOString();
      const result = createEventSchema.safeParse({
        ...baseValid,
        startDate: start,
        endDate: end,
        datesUnknown: false,
      });

      expect(result.success).toBe(true);
    });

    it('accepts datesUnknown=true with both dates empty (TBD event)', () => {
      const result = createEventSchema.safeParse({
        ...baseValid,
        startDate: '',
        endDate: '',
        datesUnknown: true,
      });

      expect(result.success).toBe(true);
    });

    it('accepts datesUnknown=true even if user typed in dates (toggle wins)', () => {
      // datesUnknown=true means the form is submitting null dates regardless of any
      // residual datetime-local input value. The schema must not error on the residual.
      const start = new Date(Date.now() + 7 * 86400_000).toISOString();
      const result = createEventSchema.safeParse({
        ...baseValid,
        startDate: start,
        endDate: '',
        datesUnknown: true,
      });

      expect(result.success).toBe(true);
    });
  });

  describe('rejects mixed dates when datesUnknown=false', () => {
    it('rejects start without end', () => {
      const start = new Date(Date.now() + 7 * 86400_000).toISOString();
      const result = createEventSchema.safeParse({
        ...baseValid,
        startDate: start,
        endDate: '',
        datesUnknown: false,
      });

      expect(result.success).toBe(false);
    });

    it('rejects end without start', () => {
      const end = new Date(Date.now() + 8 * 86400_000).toISOString();
      const result = createEventSchema.safeParse({
        ...baseValid,
        startDate: '',
        endDate: end,
        datesUnknown: false,
      });

      expect(result.success).toBe(false);
    });
  });

  describe('rejects past start date when both dates provided', () => {
    it('rejects start in the past', () => {
      const past = new Date(Date.now() - 86400_000).toISOString();
      const future = new Date(Date.now() + 86400_000).toISOString();
      const result = createEventSchema.safeParse({
        ...baseValid,
        startDate: past,
        endDate: future,
        datesUnknown: false,
      });

      expect(result.success).toBe(false);
    });

    it('does NOT reject past start when datesUnknown=true (start input is ignored)', () => {
      const past = new Date(Date.now() - 86400_000).toISOString();
      const result = createEventSchema.safeParse({
        ...baseValid,
        startDate: past,
        endDate: '',
        datesUnknown: true,
      });

      expect(result.success).toBe(true);
    });
  });

  describe('rejects end <= start', () => {
    it('rejects end before start', () => {
      const start = new Date(Date.now() + 7 * 86400_000).toISOString();
      const earlyEnd = new Date(Date.now() + 6 * 86400_000).toISOString();
      const result = createEventSchema.safeParse({
        ...baseValid,
        startDate: start,
        endDate: earlyEnd,
        datesUnknown: false,
      });

      expect(result.success).toBe(false);
    });
  });
});

describe('editEventSchema — TBD dates (Phase 8YA.3)', () => {
  const baseValid = {
    title: 'A community event title',
    description: 'A meaningful description with enough characters',
    category: EventCategory.Community,
    capacity: 50,
    isFree: true,
    enableDualPricing: false,
    enableGroupPricing: false,
    enableTieredTicketing: false,
    publishOrganizerContact: false,
  } as const;

  it('accepts datesUnknown=true with both dates empty', () => {
    const result = editEventSchema.safeParse({
      ...baseValid,
      startDate: '',
      endDate: '',
      datesUnknown: true,
    });

    expect(result.success).toBe(true);
  });

  it('accepts past start date (edit mode allows historical events)', () => {
    // editEventSchema differs from createEventSchema in that it has no future-date
    // refine — events being edited may have already started or finished.
    const past = new Date(Date.now() - 7 * 86400_000).toISOString();
    const pastEnd = new Date(Date.now() - 6 * 86400_000).toISOString();
    const result = editEventSchema.safeParse({
      ...baseValid,
      startDate: past,
      endDate: pastEnd,
      datesUnknown: false,
    });

    expect(result.success).toBe(true);
  });

  it('rejects mixed dates when datesUnknown=false', () => {
    const start = new Date(Date.now() + 7 * 86400_000).toISOString();
    const result = editEventSchema.safeParse({
      ...baseValid,
      startDate: start,
      endDate: '',
      datesUnknown: false,
    });

    expect(result.success).toBe(false);
  });
});
