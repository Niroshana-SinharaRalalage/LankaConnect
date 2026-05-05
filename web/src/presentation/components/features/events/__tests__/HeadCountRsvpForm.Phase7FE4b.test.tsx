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
 * Phase 7F-E.4b (architect-approved 2026-05-01) — merged tier × demographic layout.
 *
 * Architect Q4 auto-detect rules under test:
 *   - B3 + tiered                         →  per-tier Males / Females ALWAYS visible
 *   - B4 + tiered                         →  per-tier Adult Males / Adult Females /
 *                                            Child Males / Child Females ALWAYS visible
 *   - Top-level demographic section is HIDDEN whenever the merged layout activates,
 *     so users only enter values once.
 *
 * The wire payload still uses the existing registration-level fields (males/females,
 * adultMales/etc.); the form aggregates per-tier values at submit time. Per-tier
 * gender / 4-leaf are NOT new fields on TierCountDto — gender has no per-tier pricing
 * dependency, so per-tier capture is purely a UX improvement.
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

describe('HeadCountRsvpForm — Phase 7F-E.4b merged layout', () => {
  it('B3 + tiered: per-tier Males / Females visible after picking a ticket; top-level hidden', () => {
    render(
      <HeadCountRsvpForm
        eventId="evt"
        registrationMode={RegistrationMode.HeadCountByGender}
        isFree={false}
        maxAttendeesPerRegistration={10}
        spotsLeft={10}
        isProcessing={false}
        onSubmit={noopSubmit}
        ticketingMode={TicketingMode.Tiered}
        ticketTiers={[makeTier()]}
      />,
    );

    // Before picking a ticket, no per-tier gender spinners exist.
    expect(screen.queryByLabelText(/^Males$/)).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/^Females$/)).not.toBeInTheDocument();

    // Pick 2 tickets — per-tier Males / Females become visible (always-on).
    const incrementButtons = screen.getAllByLabelText(/Increment/i);
    fireEvent.click(incrementButtons[0]);
    fireEvent.click(incrementButtons[0]);

    expect(screen.getByLabelText(/^Males$/)).toBeInTheDocument();
    expect(screen.getByLabelText(/^Females$/)).toBeInTheDocument();

    // Top-level demographic section is hidden under merged layout — only one set of
    // Males / Females spinners exists.
    expect(screen.getAllByLabelText(/^Males$/)).toHaveLength(1);
    expect(screen.getAllByLabelText(/^Females$/)).toHaveLength(1);
  });

  it('B4 + tiered: per-tier 4-leaf spinners visible after picking a ticket; top-level hidden', () => {
    render(
      <HeadCountRsvpForm
        eventId="evt"
        registrationMode={RegistrationMode.HeadCountByAgeAndGender}
        isFree={false}
        maxAttendeesPerRegistration={10}
        spotsLeft={10}
        isProcessing={false}
        onSubmit={noopSubmit}
        ticketingMode={TicketingMode.Tiered}
        ticketTiers={[makeTier()]}
      />,
    );

    // Before picking a ticket, no per-tier 4-leaf spinners.
    expect(screen.queryByLabelText(/^Adult Males$/)).not.toBeInTheDocument();

    const incrementButtons = screen.getAllByLabelText(/Increment/i);
    fireEvent.click(incrementButtons[0]);

    // All 4 leaves visible inline under the tier card.
    expect(screen.getByLabelText(/^Adult Males$/)).toBeInTheDocument();
    expect(screen.getByLabelText(/^Adult Females$/)).toBeInTheDocument();
    expect(screen.getByLabelText(/^Child Males$/)).toBeInTheDocument();
    expect(screen.getByLabelText(/^Child Females$/)).toBeInTheDocument();

    // Top-level demographic section hidden — only one set of each leaf exists.
    expect(screen.getAllByLabelText(/^Adult Males$/)).toHaveLength(1);
  });

  it('B3 NON-tiered: original layout (single Males / Females block at top-level)', () => {
    render(
      <HeadCountRsvpForm
        eventId="evt"
        registrationMode={RegistrationMode.HeadCountByGender}
        isFree
        maxAttendeesPerRegistration={10}
        spotsLeft={10}
        isProcessing={false}
        onSubmit={noopSubmit}
      />,
    );

    expect(screen.getByLabelText(/^Males$/)).toBeInTheDocument();
    expect(screen.getByLabelText(/^Females$/)).toBeInTheDocument();
  });

  it('B4 NON-tiered: original layout (single 4-leaf block at top-level)', () => {
    render(
      <HeadCountRsvpForm
        eventId="evt"
        registrationMode={RegistrationMode.HeadCountByAgeAndGender}
        isFree
        maxAttendeesPerRegistration={10}
        spotsLeft={10}
        isProcessing={false}
        onSubmit={noopSubmit}
      />,
    );

    expect(screen.getByLabelText(/^Adult Males$/)).toBeInTheDocument();
    expect(screen.getByLabelText(/^Adult Females$/)).toBeInTheDocument();
    expect(screen.getByLabelText(/^Child Males$/)).toBeInTheDocument();
    expect(screen.getByLabelText(/^Child Females$/)).toBeInTheDocument();
  });

  it('B1 + tiered: NO per-tier demographic spinners — only the tier-count selector', () => {
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

    expect(screen.queryByLabelText(/^Adults$/)).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/^Males$/)).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/^Females$/)).not.toBeInTheDocument();
  });
});
