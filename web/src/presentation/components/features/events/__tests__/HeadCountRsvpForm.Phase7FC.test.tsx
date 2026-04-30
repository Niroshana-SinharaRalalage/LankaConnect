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
 * Phase 7F-C.3 — per-tier-by-age opt-in toggle behaviour in `HeadCountRsvpForm`.
 *
 * Architect Q2 + Q6 calls being tested:
 *   - Default: age-unaware (no toggle visible until user picks at least one ticket).
 *   - Opt-in: clicking the toggle reveals Adults / Children spinners summing to the tier total.
 *   - Q6: tiers with `hasChildPricing === false` MUST NOT show the toggle; instead, a helper
 *     line explains "children billed at adult price."
 *   - Toggle is only meaningful in B2 / B4 modes — hidden in B1 / B3 even when child pricing is on.
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

describe('HeadCountRsvpForm — Phase 7F-C per-tier-by-age toggle', () => {
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

    // Bump the tier count so the toggle row would appear if it existed.
    fireEvent.click(screen.getByLabelText(/Increment/i));
    expect(screen.queryByText(/Add per-age split/i)).not.toBeInTheDocument();
  });

  it('shows the per-age toggle in B2 mode after selecting a ticket on a tier with ChildPrice', () => {
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

    expect(screen.queryByText(/Add per-age split/i)).not.toBeInTheDocument();
    // B2 + tiered shows three Increment buttons (tier, demographic adults, demographic children).
    // The tier increment is rendered first inside the tier-card section.
    const incrementButtons = screen.getAllByLabelText(/Increment/i);
    fireEvent.click(incrementButtons[0]); // tier count: 0 → 1
    expect(screen.getByText(/Add per-age split/i)).toBeInTheDocument();
  });

  it('hides the toggle and shows the helper line when the tier has no ChildPrice (architect Q6)', () => {
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
    fireEvent.click(incrementButtons[0]); // tier count: 0 → 1
    expect(screen.queryByText(/Add per-age split/i)).not.toBeInTheDocument();
    expect(screen.getByText(/children are billed at adult price/i)).toBeInTheDocument();
  });

  it('reveals Adults / Children spinners when the toggle is checked', () => {
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

    // Pick 3 tickets first (so the toggle row + age spinners are meaningful)
    const incrementButtons = screen.getAllByLabelText(/Increment/i);
    // first increment button is the tier count
    fireEvent.click(incrementButtons[0]);
    fireEvent.click(incrementButtons[0]);
    fireEvent.click(incrementButtons[0]);

    const toggle = screen.getByLabelText(/Add per-age split/i);
    expect(screen.queryByLabelText(/^Adults$/)).toBeInTheDocument(); // demographic adults spinner already exists
    // Toggle on — the per-tier age leaves should appear
    fireEvent.click(toggle);
    // After the toggle, additional Adults/Children spinners under the tier card render —
    // they have the same label, so we expect the count to grow by 2 vs pre-toggle.
    const allAdultSpinners = screen.getAllByLabelText(/^Adults$/);
    const allChildrenSpinners = screen.getAllByLabelText(/^Children$/);
    // Exactly two each: the demographic-axis spinner + the per-tier-age spinner.
    expect(allAdultSpinners.length).toBe(2);
    expect(allChildrenSpinners.length).toBe(2);
  });
});
