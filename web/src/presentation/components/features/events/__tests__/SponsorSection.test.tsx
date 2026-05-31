/**
 * Phase 6A.157 [6/6] — SponsorSection integration regression tests.
 *
 * Pins the load-bearing contract for the new sponsorship-package grid that
 * mounts ABOVE the existing custom-amount form. Goals:
 *
 *   1. Package grid renders when EnablePackages flag is ON AND server returns packages
 *   2. Package grid HIDDEN when EnablePackages flag is OFF (zero regression for non-packages events)
 *   3. Package grid HIDDEN when server returns empty list (event has packages disabled OR all sold out)
 *   4. Divider "Or choose your own amount" only shows when packages are present
 *   5. Custom-amount form (money mode) STILL renders alongside packages (mode toggle preserved)
 *   6. Clicking a package card opens the purchase modal scoped to that package
 *
 * The existing custom-amount sponsor flow is left completely untouched — this
 * test suite is the regression net guaranteeing that.
 */
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { SponsorSection } from '../SponsorSection';
import type {
  SponsorConfigurationDto,
  SponsorshipPackagePublicDto,
} from '@/infrastructure/api/types/events.types';

// Mocks — package hook drives per-test data; the rest stay quiet so we focus
// on the integration contract.
const mockUsePublicSponsorshipPackages = vi.fn();
vi.mock('@/presentation/hooks/useSponsorshipPackages', () => ({
  usePublicSponsorshipPackages: (eventId: string, enabled: boolean) =>
    mockUsePublicSponsorshipPackages(eventId, enabled),
  // Modal hook stub — never invoked in these tests (we don't drive form submit)
  usePurchasePackageSponsor: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));

// Full stub set of hooks transitively pulled by SponsorSection + EditSponsorModal.
// vi.mock factories are hoisted above all top-level statements, so we cannot
// reference an outer `noopMutation` const here — inline the stubs instead.
vi.mock('@/presentation/hooks/useSponsors', () => {
  const stub = () => ({ mutateAsync: vi.fn().mockResolvedValue({}), isPending: false });
  return {
    sponsorKeys: { all: ['sponsors'], list: () => ['sponsors', 'list'] },
    useEventSponsors: () => ({ data: { sponsors: [] }, isLoading: false }),
    usePublicEventSponsors: () => ({ data: { sponsors: [] }, isLoading: false }),
    useSponsorSummary: () => ({ data: null, isLoading: false }),
    useMySponsors: () => ({ data: [], isLoading: false }),
    useCreateMoneySponsor: stub,
    useCreateItemSponsor: stub,
    useUploadSponsorImage: stub,
    useDeleteSponsorImage: stub,
    useUpdateSponsor: stub,
    useCreateOffPlatformSponsor: stub,
  };
});

vi.mock('@/infrastructure/api/repositories/events.repository', () => ({
  eventsRepository: {},
}));

function makeConfig(overrides: Partial<SponsorConfigurationDto> = {}): SponsorConfigurationDto {
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

function renderWithQuery(ui: React.ReactElement) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={qc}>{ui}</QueryClientProvider>);
}

beforeEach(() => {
  mockUsePublicSponsorshipPackages.mockReset();
  mockUsePublicSponsorshipPackages.mockReturnValue({
    data: [],
    isLoading: false,
    error: null,
    refetch: vi.fn(),
  });
});

describe('SponsorSection — Phase 6A.157 package integration', () => {
  it('renders the package grid when EnablePackages is ON and server returns packages', () => {
    mockUsePublicSponsorshipPackages.mockReturnValue({
      data: [
        makePkg({ id: 'p1', name: 'Gold Sponsor', tier: 'Gold', priceAmount: 500 }),
        makePkg({ id: 'p2', name: 'Silver Sponsor', tier: 'Silver', priceAmount: 250 }),
      ],
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });

    renderWithQuery(
      <SponsorSection
        eventId="event-uuid-1"
        sponsorConfig={makeConfig({ enablePackages: true })}
      />
    );

    // CollapsibleSection wraps the content closed by default — expand it
    fireEvent.click(screen.getByText(/Sponsor This Event/i));

    // Section heading + both cards
    expect(screen.getByText('Sponsorship Packages')).toBeInTheDocument();
    expect(screen.getByText('Gold Sponsor')).toBeInTheDocument();
    expect(screen.getByText('Silver Sponsor')).toBeInTheDocument();
    // Divider between packages and custom-amount form
    expect(screen.getByText(/Or choose your own amount/i)).toBeInTheDocument();
  });

  it('hides the package grid when EnablePackages is OFF (zero regression for non-packages events)', () => {
    mockUsePublicSponsorshipPackages.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });

    renderWithQuery(
      <SponsorSection
        eventId="event-uuid-1"
        sponsorConfig={makeConfig({ enablePackages: false })}
      />
    );

    fireEvent.click(screen.getByText(/Sponsor This Event/i));
    expect(screen.queryByText('Sponsorship Packages')).not.toBeInTheDocument();
    expect(screen.queryByText(/Or choose your own amount/i)).not.toBeInTheDocument();
  });

  it('hides the package grid when EnablePackages is ON but server returns []', () => {
    mockUsePublicSponsorshipPackages.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });

    renderWithQuery(
      <SponsorSection
        eventId="event-uuid-1"
        sponsorConfig={makeConfig({ enablePackages: true })}
      />
    );

    fireEvent.click(screen.getByText(/Sponsor This Event/i));
    // Header gone, divider gone — section reads exactly like a non-packages event
    expect(screen.queryByText('Sponsorship Packages')).not.toBeInTheDocument();
    expect(screen.queryByText(/Or choose your own amount/i)).not.toBeInTheDocument();
  });

  it('preserves the custom-amount money form when packages render alongside', () => {
    mockUsePublicSponsorshipPackages.mockReturnValue({
      data: [makePkg({ id: 'p1', name: 'Gold Sponsor', priceAmount: 500 })],
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });

    renderWithQuery(
      <SponsorSection
        eventId="event-uuid-1"
        sponsorConfig={makeConfig({ enablePackages: true, acceptMoneySponsors: true })}
      />
    );

    fireEvent.click(screen.getByText(/Sponsor This Event/i));
    // Custom-amount input MUST still render below the divider — regression
    // contract for the existing free-form sponsor flow
    expect(screen.getByText('Gold Sponsor')).toBeInTheDocument();
    expect(screen.getByText(/Or choose your own amount/i)).toBeInTheDocument();
    // Money form has these stable labels (kept unchanged by 6A.157)
    expect(screen.getByLabelText(/Sponsorship Amount/i)).toBeInTheDocument();
  });

  it('opens the purchase modal scoped to the clicked package', () => {
    mockUsePublicSponsorshipPackages.mockReturnValue({
      data: [
        makePkg({ id: 'p1', name: 'Gold Sponsor', tier: 'Gold', priceAmount: 500 }),
      ],
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });

    renderWithQuery(
      <SponsorSection
        eventId="event-uuid-1"
        sponsorConfig={makeConfig({ enablePackages: true })}
      />
    );

    fireEvent.click(screen.getByText(/Sponsor This Event/i));

    // Modal closed initially
    expect(screen.queryByText(/Sponsor — Gold Sponsor/i)).not.toBeInTheDocument();

    // Click the Sponsor CTA on the card → modal header should appear
    fireEvent.click(screen.getByRole('button', { name: /Sponsor as Gold|Sponsor as Gold Sponsor/i }));
    expect(screen.getByText(/Sponsor — Gold Sponsor/i)).toBeInTheDocument();
  });

  it('queries the public packages hook with enabled=true ONLY when both isEnabled AND enablePackages are on', () => {
    mockUsePublicSponsorshipPackages.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });

    // Case A: sponsors enabled + packages enabled → enabled=true
    renderWithQuery(
      <SponsorSection
        eventId="event-uuid-1"
        sponsorConfig={makeConfig({ isEnabled: true, enablePackages: true })}
      />
    );
    expect(mockUsePublicSponsorshipPackages).toHaveBeenLastCalledWith('event-uuid-1', true);

    mockUsePublicSponsorshipPackages.mockClear();

    // Case B: sponsors enabled but packages disabled → enabled=false
    renderWithQuery(
      <SponsorSection
        eventId="event-uuid-1"
        sponsorConfig={makeConfig({ isEnabled: true, enablePackages: false })}
      />
    );
    expect(mockUsePublicSponsorshipPackages).toHaveBeenLastCalledWith('event-uuid-1', false);
  });
});
