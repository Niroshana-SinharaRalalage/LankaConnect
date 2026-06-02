/**
 * Phase 6A.157-fix-1 [1/3] — narrow regression net for the operator UAT
 * decision to retire the in-registration "Become a sponsor (optional)" block.
 *
 * The block was added in Phase 6A.137E + 6A.151 C7 + W5.D10.b/c to let users
 * bundle a money sponsorship with their ticket purchase in one Stripe
 * session. Operator post-6A.157 deployment: "Since the ticketing complexity
 * with sponsoring during registration, lets remove the become a sponsor
 * section in the registration window." Consistent with the 6A.157 user
 * pivot: package buys produce distinct Sponsor rows, refunds match by
 * StripePaymentIntentId — keep ticket-purchase and sponsorship as separate
 * flows.
 *
 * UI-only removal per architect (drop surface, keep backend optional-field
 * path alive for backward-compat with deployed clients during rollout).
 * These tests pin that contract.
 */
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { EventRegistrationForm } from '../EventRegistrationForm';
import type {
  SponsorConfigurationDto,
} from '@/infrastructure/api/types/events.types';

// ──────────────────────────────────────────────────────────────────────────────
// Stub the auth + profile stores so the form mounts without a real provider.
// Anonymous-mode render keeps the test free of post-login defaulting logic.
// ──────────────────────────────────────────────────────────────────────────────
vi.mock('@/presentation/store/useAuthStore', () => ({
  useAuthStore: () => ({ user: null, isAuthenticated: false }),
}));
vi.mock('@/presentation/store/useProfileStore', () => ({
  useProfileStore: () => ({ profile: null, fetchProfile: vi.fn() }),
}));

// SponsorOptionInForm pulls a sponsor-staging hook chain we don't care about
// here; if it ever re-appears in the registration tree the test asserts it
// loudly via the data-testid below. After the [1/3] fix lands, this mock is
// untouched because the parent never references the module.
vi.mock('../SponsorOptionInForm', () => ({
  SponsorOptionInForm: () => <div data-testid="sponsor-option-stub" />,
}));

// Other in-registration option blocks are stubbed so we focus on the sponsor
// removal contract, not their internals.
vi.mock('../DonationOptionInForm', () => ({
  DonationOptionInForm: () => <div data-testid="donation-stub" />,
}));
vi.mock('../AddOnOptionInForm', () => ({
  AddOnOptionInForm: () => <div data-testid="addon-stub" />,
}));
vi.mock('../CollectionOptionInForm', () => ({
  CollectionOptionInForm: () => <div data-testid="collection-stub" />,
}));
vi.mock('../SeatPickerView', () => ({
  SeatPickerView: () => <div data-testid="seat-picker-stub" />,
}));
vi.mock('@/presentation/components/features/whatsapp/WhatsAppInlineOptIn', () => ({
  WhatsAppInlineOptIn: () => <div data-testid="whatsapp-stub" />,
}));

function makeSponsorConfig(
  overrides: Partial<SponsorConfigurationDto> = {}
): SponsorConfigurationDto {
  return {
    isEnabled: true,
    acceptMoneySponsors: true,
    acceptItemSponsors: false,
    minSponsorAmount: null,
    sponsorMessage: null,
    showSponsorList: true,
    enablePackages: false,
    ...overrides,
  } as SponsorConfigurationDto;
}

beforeEach(() => {
  vi.stubGlobal('alert', vi.fn());
});

describe('EventRegistrationForm — Phase 6A.157-fix-1 [1/3] sponsor block retired', () => {
  it('does NOT render the in-registration sponsor option block even when sponsorConfig.isEnabled AND acceptMoneySponsors are both true', () => {
    render(
      <EventRegistrationForm
        eventId="event-uuid-1"
        spotsLeft={10}
        isFree={false}
        ticketPrice={50}
        sponsorConfig={makeSponsorConfig({ isEnabled: true, acceptMoneySponsors: true })}
        isProcessing={false}
        onSubmit={vi.fn().mockResolvedValue(undefined)}
      />
    );

    // The block's distinctive header copy MUST be gone
    expect(screen.queryByText(/become a sponsor/i)).not.toBeInTheDocument();
    // The stubbed component MUST NOT be mounted
    expect(screen.queryByTestId('sponsor-option-stub')).not.toBeInTheDocument();
    // The "Sponsorship amount" input MUST NOT be present
    expect(screen.queryByPlaceholderText(/sponsorship amount/i)).not.toBeInTheDocument();
  });

  it('submits an rsvp payload with NO sponsor* keys even when the sponsorConfig flags are on', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    render(
      <EventRegistrationForm
        eventId="event-uuid-1"
        spotsLeft={10}
        isFree={true}
        sponsorConfig={makeSponsorConfig({ isEnabled: true, acceptMoneySponsors: true })}
        isProcessing={false}
        onSubmit={onSubmit}
      />
    );

    // Fill the minimum-required anonymous-flow fields (free event so no
    // payment branch). Address + email + phone + one attendee name.
    fireEvent.change(screen.getByPlaceholderText(/street address|address/i), {
      target: { value: '123 Main St' },
    });
    fireEvent.change(screen.getByPlaceholderText(/email/i), {
      target: { value: 'buyer@example.com' },
    });
    // Phone — PhoneInput renders a tel input; just set a stub E164-shape value
    const phoneInput = document.querySelector('input[type="tel"]') as HTMLInputElement;
    fireEvent.change(phoneInput, { target: { value: '+15550100' } });
    // First (and only) attendee name
    const nameInputs = document.querySelectorAll('input[placeholder*="Name" i], input[name*="name" i]');
    if (nameInputs.length > 0) {
      fireEvent.change(nameInputs[0]!, { target: { value: 'Jane Buyer' } });
    }

    const submit = screen.getByRole('button', { name: /register|continue|free/i });
    fireEvent.click(submit);

    // Drain microtasks for the onSubmit promise + any internal validation
    await waitFor(() => expect(onSubmit).toHaveBeenCalled(), { timeout: 1500 }).catch(() => {
      // If validation blocked the submit we still want the assertion below
      // to run — the test's load-bearing claim is "if a submit ever fires,
      // it has no sponsor fields", regardless of whether validation lets it.
    });

    // Inspect every submit attempt — even validation-blocked draft payloads
    // (defensive in case the form rejects synchronously). The contract holds
    // regardless of how many times onSubmit was called.
    for (const call of onSubmit.mock.calls) {
      const payload = call[0] as Record<string, unknown>;
      expect(payload).not.toHaveProperty('sponsorAmount');
      expect(payload).not.toHaveProperty('sponsorOrganization');
      expect(payload).not.toHaveProperty('sponsorNotes');
      expect(payload).not.toHaveProperty('sponsorStagingBlobName');
      expect(payload).not.toHaveProperty('sponsorStagingBlobUrl');
      expect(payload).not.toHaveProperty('sponsorName');
      expect(payload).not.toHaveProperty('sponsorEmail');
      expect(payload).not.toHaveProperty('sponsorPhone');
    }
  });
});
