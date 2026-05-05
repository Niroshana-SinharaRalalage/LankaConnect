import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { HeadCountRsvpForm } from '../HeadCountRsvpForm';
import {
  RegistrationMode,
  TicketingMode,
  type TicketTierDto,
  Currency,
} from '@/infrastructure/api/types/events.types';

/**
 * Phase 7F-C.3 (updated for 7F-E.4b architect-approved 2026-05-01):
 *
 * Phase 7F-C originally introduced an OPT-IN per-tier-by-age toggle in B2/B4 modes.
 * Phase 7F-E.4b (this slice) shifts that to ALWAYS-ON for B2 + tiered + ChildPrice
 * (the "merged-age" layout) and introduces analogous merges for B3 (gender) and B4
 * (4-leaf). The opt-in toggle therefore disappears in the B2 merged-age path; it
 * remains only as a no-op safety net in the B4 path (which uses the 4-leaf merge,
 * not the age merge).
 *
 * Architect Q4 + Q6 calls being tested:
 *   - Default: B1 + tiered never shows the toggle (no demographic axis at all).
 *   - B2 + tiered + ChildPrice: per-tier Adults / Children spinners are ALWAYS visible
 *     (no opt-in toggle).
 *   - B2 + tiered without ChildPrice on a tier (Q6): no per-tier age spinners on that
 *     tier; instead, a helper line explains "children billed at adult price."
 *   - B2 + tiered without ChildPrice on ANY tier: top-level Adults / Children stay
 *     visible (architect rule — pricing depends on those numbers).
 */

vi.mock('@/presentation/store/useAuthStore', () => ({
  useAuthStore: () => ({ user: null }),
}));
vi.mock('@/presentation/store/useProfileStore', () => ({
  useProfileStore: () => ({ profile: null, loadProfile: vi.fn() }),
}));

function makeTier(overrides: Partial<TicketTierDto> = {}): TicketTierDto {
  return {
    id: 'tier-vip',
    name: 'VIP',
    description: 'VIP tier',
    adultPriceAmount: 50,
    adultPriceCurrency: Currency.USD,
    childPriceAmount: 25,
    childPriceCurrency: Currency.USD,
    childAgeLimit: 12,
    hasChildPricing: true,
    capacity: 10,
    availableQuantity: 10,
    reservedCount: 0,
    isActive: true,
    isFree: false,
    sortOrder: 1,
    maxPerUser: 10,
    ...overrides,
  } as TicketTierDto;
}

const noopSubmit = async () => {};

describe('HeadCountRsvpForm — Phase 7F-C / 7F-E.4b per-tier merged layouts', () => {
  it('hides the per-age toggle in B1 mode even when the tier has child pricing', () => {
    render(
      <HeadCountRsvpForm
        eventId="evt"
        registrationMode={RegistrationMode.HeadCountOnly}
        isFree={false}
        maxAttendeesPerRegistration={10}
        spotsLeft={10}
        isProcessing={false}
        onSubmit={noopSubmit}
        ticketingMode={TicketingMode.Tiered}
        ticketTiers={[makeTier()]}
      />,
    );

    fireEvent.click(screen.getByLabelText(/Increment/i));
    expect(screen.queryByText(/Add per-age split/i)).not.toBeInTheDocument();
  });

  it('B2 + tiered + ChildPrice: per-tier Adults/Children always visible (no opt-in toggle)', () => {
    render(
      <HeadCountRsvpForm
        eventId="evt"
        registrationMode={RegistrationMode.HeadCountByAge}
        isFree={false}
        maxAttendeesPerRegistration={10}
        spotsLeft={10}
        isProcessing={false}
        onSubmit={noopSubmit}
        ticketingMode={TicketingMode.Tiered}
        ticketTiers={[makeTier()]}
      />,
    );

    // Pick 2 tickets on the tier — the per-tier Adults / Children spinners should appear
    // immediately (no opt-in toggle).
    const incrementButtons = screen.getAllByLabelText(/Increment/i);
    fireEvent.click(incrementButtons[0]); // 0 → 1
    fireEvent.click(incrementButtons[0]); // 1 → 2

    // Toggle is REMOVED under the merged-age layout.
    expect(screen.queryByText(/Add per-age split/i)).not.toBeInTheDocument();
    // Per-tier Adults / Children spinners are visible (the only ones on the form —
    // top-level demographic block is hidden under the merged layout).
    expect(screen.getByLabelText(/^Adults$/)).toBeInTheDocument();
    expect(screen.getByLabelText(/^Children$/)).toBeInTheDocument();
  });

  it('B2 + tiered without ChildPrice on a tier: no per-tier spinners on that tier (architect Q6)', () => {
    const adultOnly = makeTier({
      id: 'tier-std',
      name: 'Standard',
      childPriceAmount: null,
      childPriceCurrency: null,
      childAgeLimit: null,
      hasChildPricing: false,
    });

    render(
      <HeadCountRsvpForm
        eventId="evt"
        registrationMode={RegistrationMode.HeadCountByAge}
        isFree={false}
        maxAttendeesPerRegistration={10}
        spotsLeft={10}
        isProcessing={false}
        onSubmit={noopSubmit}
        ticketingMode={TicketingMode.Tiered}
        ticketTiers={[adultOnly]}
      />,
    );

    const incrementButtons = screen.getAllByLabelText(/Increment/i);
    fireEvent.click(incrementButtons[0]);
    expect(screen.queryByText(/Add per-age split/i)).not.toBeInTheDocument();
    expect(screen.getByText(/children are billed at adult price/i)).toBeInTheDocument();
  });

  it('B2 + tiered with NO ChildPrice on ANY tier: top-level Adults / Children visible', () => {
    // When no tier offers child pricing, the merged-age layout does NOT activate. The
    // top-level Adults / Children block stays visible because pricing depends on the
    // (registration-level) age split.
    const adultOnly = makeTier({
      id: 'tier-std',
      name: 'Standard',
      childPriceAmount: null,
      childPriceCurrency: null,
      childAgeLimit: null,
      hasChildPricing: false,
    });

    render(
      <HeadCountRsvpForm
        eventId="evt"
        registrationMode={RegistrationMode.HeadCountByAge}
        isFree={false}
        maxAttendeesPerRegistration={10}
        spotsLeft={10}
        isProcessing={false}
        onSubmit={noopSubmit}
        ticketingMode={TicketingMode.Tiered}
        ticketTiers={[adultOnly]}
      />,
    );

    expect(screen.getByLabelText(/^Adults$/)).toBeInTheDocument();
    expect(screen.getByLabelText(/^Children$/)).toBeInTheDocument();
  });
});
