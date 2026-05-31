/**
 * Phase 6A.157 [5/6] — buyer-facing purchase modal tests.
 *
 * Covers the load-bearing behaviours:
 *   1. Renders when open / hidden when closed
 *   2. Displays the chosen package's details (name, tier, price, perks)
 *   3. IncludedTicketCount info note appears > 0, hidden when 0
 *   4. Validates required buyer fields (name, email)
 *   5. Calls usePurchasePackageSponsor with the right payload on submit
 *   6. Free-package CTA copy differs from paid-package CTA copy
 *   7. Redirects to checkoutUrl on success (covers paid Stripe URL AND free
 *      SuccessUrl — same code path)
 *   8. Shows error message on mutation failure
 *   9. Cancel / X close calls onClose without submitting
 *  10. Form-nesting safety — submit inside a parent <form> does NOT submit
 *      the parent (mirrors the 6A.156-fix-2 regression contract for
 *      SponsorshipPackageEditModal — portal + stopPropagation belt-and-suspenders)
 *
 * Modal is portal'd to document.body per the same pattern locked in
 * 6A.156-fix-2 (operator UAT bug: nested forms cause browser to submit the
 * outer one).
 */
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { PurchaseSponsorshipPackageModal } from '../PurchaseSponsorshipPackageModal';
import type {
  SponsorshipPackagePublicDto,
  CreatePackageSponsorResult,
} from '@/infrastructure/api/types/events.types';

// Mock the purchase hook so we can drive success / error per-test without a
// real QueryClient. The hook's full behaviour is tested separately via
// staging API smoke (per master TODO defer rationale).
const mockMutateAsync = vi.fn<[unknown], Promise<CreatePackageSponsorResult>>();
let mockIsPending = false;

vi.mock('@/presentation/hooks/useSponsorshipPackages', () => ({
  usePurchasePackageSponsor: () => ({
    mutateAsync: mockMutateAsync,
    isPending: mockIsPending,
  }),
}));

// Stub window.location.assign so the redirect-to-Stripe path is observable
// without actually navigating the test page.
const mockAssign = vi.fn();

beforeEach(() => {
  mockMutateAsync.mockReset();
  mockIsPending = false;
  mockAssign.mockReset();
  vi.stubGlobal('alert', vi.fn());
  // Replace location.assign for the duration of each test
  Object.defineProperty(window, 'location', {
    value: { ...window.location, assign: mockAssign, origin: 'https://test.local' },
    writable: true,
  });
});

const liveEventId = 'event-uuid-1';
const paidPackage: SponsorshipPackagePublicDto = makePkg({
  id: 'pkg-paid',
  name: 'Gold Sponsor',
  tier: 'Gold',
  priceAmount: 500,
  perks: ['Logo on banner', '5 mins on stage'],
  includedTicketCount: 5,
});
const freePackage: SponsorshipPackagePublicDto = makePkg({
  id: 'pkg-free',
  name: 'Community Recognition',
  tier: 'Friend',
  priceAmount: 0,
  perks: [],
  includedTicketCount: 0,
});

describe('PurchaseSponsorshipPackageModal — render gating', () => {
  it('renders nothing when isOpen is false', () => {
    const { container } = render(
      <PurchaseSponsorshipPackageModal
        eventId={liveEventId}
        pkg={paidPackage}
        isOpen={false}
        onClose={vi.fn()}
      />
    );
    // Empty portal — modal NOT in document.
    expect(container.firstChild).toBeNull();
    expect(screen.queryByText(/Gold Sponsor/)).not.toBeInTheDocument();
  });

  it('renders the chosen package details when isOpen is true', () => {
    render(
      <PurchaseSponsorshipPackageModal
        eventId={liveEventId}
        pkg={paidPackage}
        isOpen={true}
        onClose={vi.fn()}
      />
    );
    // Name appears twice (modal header + summary card); assert at least one.
    expect(screen.getAllByText(/Gold Sponsor/).length).toBeGreaterThan(0);
    // Tier badge ("Gold") + perks
    expect(screen.getByText('Gold')).toBeInTheDocument();
    expect(screen.getByText(/Logo on banner/)).toBeInTheDocument();
    expect(screen.getByText(/5 mins on stage/)).toBeInTheDocument();
  });
});

describe('PurchaseSponsorshipPackageModal — IncludedTicketCount info note', () => {
  it('shows the included-tickets gray info note when count > 0', () => {
    render(
      <PurchaseSponsorshipPackageModal
        eventId={liveEventId}
        pkg={paidPackage}
        isOpen={true}
        onClose={vi.fn()}
      />
    );
    // Note text per 6A.157 final scope: tickets are informational only;
    // organizer handles admission off-platform.
    expect(screen.getByText(/5 ticket/i)).toBeInTheDocument();
    expect(screen.getByText(/issued by organizer|handled by organizer|outside the platform|off-platform/i)).toBeInTheDocument();
  });

  it('hides the included-tickets note when count is 0', () => {
    render(
      <PurchaseSponsorshipPackageModal
        eventId={liveEventId}
        pkg={freePackage}
        isOpen={true}
        onClose={vi.fn()}
      />
    );
    // No "ticket" copy anywhere when count is 0
    expect(screen.queryByText(/ticket/i)).not.toBeInTheDocument();
  });
});

describe('PurchaseSponsorshipPackageModal — CTA copy', () => {
  it('shows the paid CTA copy when the package is paid', () => {
    render(
      <PurchaseSponsorshipPackageModal
        eventId={liveEventId}
        pkg={paidPackage}
        isOpen={true}
        onClose={vi.fn()}
      />
    );
    // Paid → mentions Stripe / payment / continue / proceed
    const submit = screen.getByRole('button', { name: /Continue to payment|Proceed to Stripe|Sponsor.*Pay|Pay\b/i });
    expect(submit).toBeInTheDocument();
  });

  it('shows the free CTA copy when the package is $0', () => {
    render(
      <PurchaseSponsorshipPackageModal
        eventId={liveEventId}
        pkg={freePackage}
        isOpen={true}
        onClose={vi.fn()}
      />
    );
    // Free → confirm / complete recognition language (no payment redirect)
    const submit = screen.getByRole('button', { name: /Confirm|Complete|Become.*Sponsor|Sponsor.*Free/i });
    expect(submit).toBeInTheDocument();
  });
});

describe('PurchaseSponsorshipPackageModal — submit flow', () => {
  it('validates required name + email before calling the mutation', async () => {
    render(
      <PurchaseSponsorshipPackageModal
        eventId={liveEventId}
        pkg={paidPackage}
        isOpen={true}
        onClose={vi.fn()}
      />
    );
    // Click submit with empty form — mutation must NOT fire
    const submit = screen.getByRole('button', { name: /Continue to payment|Proceed to Stripe|Sponsor.*Pay|Pay\b/i });
    fireEvent.click(submit);
    // Wait a tick for any sync validation
    await Promise.resolve();
    expect(mockMutateAsync).not.toHaveBeenCalled();
  });

  it('calls usePurchasePackageSponsor with the right payload on valid submit', async () => {
    mockMutateAsync.mockResolvedValue({
      checkoutUrl: 'https://checkout.stripe.com/abc',
      sponsorId: 'sponsor-uuid-1',
    });

    render(
      <PurchaseSponsorshipPackageModal
        eventId={liveEventId}
        pkg={paidPackage}
        isOpen={true}
        onClose={vi.fn()}
      />
    );

    fireEvent.change(screen.getByLabelText(/Your Name/i), { target: { value: 'Jane Buyer' } });
    fireEvent.change(screen.getByLabelText(/Email/i), { target: { value: 'jane@example.com' } });
    fireEvent.change(screen.getByLabelText(/Organization/i), { target: { value: 'Acme LLC' } });
    fireEvent.change(screen.getByLabelText(/Phone/i), { target: { value: '+1-555-0100' } });

    fireEvent.click(screen.getByRole('button', { name: /Continue to payment|Proceed to Stripe|Sponsor.*Pay|Pay\b/i }));

    await waitFor(() => expect(mockMutateAsync).toHaveBeenCalledTimes(1));
    expect(mockMutateAsync.mock.calls[0][0]).toMatchObject({
      eventId: liveEventId,
      packageId: 'pkg-paid',
      request: {
        buyerName: 'Jane Buyer',
        buyerEmail: 'jane@example.com',
        buyerOrganization: 'Acme LLC',
        buyerPhone: '+1-555-0100',
      },
    });
  });

  it('redirects to checkoutUrl on successful purchase', async () => {
    mockMutateAsync.mockResolvedValue({
      checkoutUrl: 'https://checkout.stripe.com/abc',
      sponsorId: 'sponsor-uuid-1',
    });

    render(
      <PurchaseSponsorshipPackageModal
        eventId={liveEventId}
        pkg={paidPackage}
        isOpen={true}
        onClose={vi.fn()}
      />
    );
    fireEvent.change(screen.getByLabelText(/Your Name/i), { target: { value: 'Jane' } });
    fireEvent.change(screen.getByLabelText(/Email/i), { target: { value: 'j@e.com' } });
    fireEvent.click(screen.getByRole('button', { name: /Continue to payment|Proceed to Stripe|Sponsor.*Pay|Pay\b/i }));

    await waitFor(() => expect(mockAssign).toHaveBeenCalledWith('https://checkout.stripe.com/abc'));
  });

  it('shows an error message when the mutation rejects', async () => {
    mockMutateAsync.mockRejectedValue(new Error('Stripe session creation failed'));

    render(
      <PurchaseSponsorshipPackageModal
        eventId={liveEventId}
        pkg={paidPackage}
        isOpen={true}
        onClose={vi.fn()}
      />
    );
    fireEvent.change(screen.getByLabelText(/Your Name/i), { target: { value: 'Jane' } });
    fireEvent.change(screen.getByLabelText(/Email/i), { target: { value: 'j@e.com' } });
    fireEvent.click(screen.getByRole('button', { name: /Continue to payment|Proceed to Stripe|Sponsor.*Pay|Pay\b/i }));

    await waitFor(() => expect(screen.getByText(/failed|error|try again/i)).toBeInTheDocument());
    expect(mockAssign).not.toHaveBeenCalled();
  });
});

describe('PurchaseSponsorshipPackageModal — close handlers', () => {
  it('calls onClose when Cancel is clicked', () => {
    const onClose = vi.fn();
    render(
      <PurchaseSponsorshipPackageModal
        eventId={liveEventId}
        pkg={paidPackage}
        isOpen={true}
        onClose={onClose}
      />
    );
    fireEvent.click(screen.getByRole('button', { name: /Cancel/i }));
    expect(onClose).toHaveBeenCalled();
    expect(mockMutateAsync).not.toHaveBeenCalled();
  });

  it('calls onClose when the X icon is clicked', () => {
    const onClose = vi.fn();
    render(
      <PurchaseSponsorshipPackageModal
        eventId={liveEventId}
        pkg={paidPackage}
        isOpen={true}
        onClose={onClose}
      />
    );
    fireEvent.click(screen.getByRole('button', { name: /Close/i }));
    expect(onClose).toHaveBeenCalled();
  });
});

describe('PurchaseSponsorshipPackageModal — form-nesting safety', () => {
  it('does NOT submit a parent <form> when the modal submit button is clicked', async () => {
    mockMutateAsync.mockResolvedValue({
      checkoutUrl: 'https://checkout.stripe.com/abc',
      sponsorId: 'sponsor-uuid-1',
    });
    const parentFormSubmit = vi.fn((e: { preventDefault: () => void }) => e.preventDefault());

    render(
      <form onSubmit={parentFormSubmit}>
        <PurchaseSponsorshipPackageModal
          eventId={liveEventId}
          pkg={paidPackage}
          isOpen={true}
          onClose={vi.fn()}
        />
      </form>
    );

    fireEvent.change(screen.getByLabelText(/Your Name/i), { target: { value: 'Jane' } });
    fireEvent.change(screen.getByLabelText(/Email/i), { target: { value: 'j@e.com' } });
    fireEvent.click(screen.getByRole('button', { name: /Continue to payment|Proceed to Stripe|Sponsor.*Pay|Pay\b/i }));

    await waitFor(() => expect(mockMutateAsync).toHaveBeenCalledTimes(1));
    // Load-bearing: outer form MUST NOT submit (per 6A.156-fix-2 regression contract)
    expect(parentFormSubmit).not.toHaveBeenCalled();
  });

  it('does NOT submit a parent <form> when Cancel is clicked', () => {
    const parentFormSubmit = vi.fn((e: { preventDefault: () => void }) => e.preventDefault());
    render(
      <form onSubmit={parentFormSubmit}>
        <PurchaseSponsorshipPackageModal
          eventId={liveEventId}
          pkg={paidPackage}
          isOpen={true}
          onClose={vi.fn()}
        />
      </form>
    );
    fireEvent.click(screen.getByRole('button', { name: /Cancel/i }));
    expect(parentFormSubmit).not.toHaveBeenCalled();
  });
});

// ──────────────────────────────────────────────────────────────────────────────
// Helpers
// ──────────────────────────────────────────────────────────────────────────────

function makePkg(overrides: Partial<SponsorshipPackagePublicDto>): SponsorshipPackagePublicDto {
  return {
    id: 'pkg-default',
    eventId: 'event-uuid-1',
    name: 'Default Package',
    description: null,
    priceAmount: 100,
    priceCurrency: 'USD',
    remainingStock: null,
    isSoldOut: false,
    sortOrder: 0,
    imageUrl: null,
    tier: null,
    perks: [],
    includedTicketCount: 0,
    ...overrides,
  };
}
