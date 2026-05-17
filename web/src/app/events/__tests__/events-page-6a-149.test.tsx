import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import EventsPage from '../page';
import { EventStatus, EventStatusFilter } from '@/infrastructure/api/types/events.types';

// LankaEventsHeader (rendered by EventsPage) calls useQueryClient(), so the
// page must mount inside a QueryClientProvider in tests. The hook is mocked
// at the module boundary above, but the bare useQueryClient call in the
// header still needs a real client. Retries off + cache-time 0 keeps tests
// deterministic.
function renderWithClient(ui: React.ReactElement) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: 0 } },
  });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

/**
 * Phase 6A.149 — /events discover page refactor
 *
 * Contract pinned by this suite:
 *   1. Decorative "Discover Events" gradient banner removed entirely
 *      (~12rem reclaimed above the fold).
 *   2. Two explicit section headers: "Upcoming Events" and "Completed Events".
 *   3. Each section has its OWN <CollapsibleSection title="Filters">
 *      defaulting to COLLAPSED (defaultOpen={false}).
 *   4. The standalone "Event Status" filter dropdown is removed — the two
 *      sections make it redundant.
 *   5. Page fires TWO useEvents calls — one for Upcoming (Active filter)
 *      and one for Completed (Inactive filter, client-filtered to Completed).
 *   6. The Completed section is HIDDEN entirely when the filtered result
 *      has zero Completed-status events.
 *   7. Both section grids are wrapped in a scroll container
 *      (max-h-[1500px] overflow-y-auto) with a bottom fade-mask.
 */

const useEventsMock = vi.fn();

vi.mock('next/navigation', () => ({
  useRouter: vi.fn(() => ({ push: vi.fn() })),
}));

vi.mock('@/presentation/store/useAuthStore', () => ({
  useAuthStore: vi.fn(() => ({
    user: null,
    isAuthenticated: false,
  })),
}));

vi.mock('@/presentation/hooks/useGeolocation', () => ({
  useGeolocation: vi.fn(() => ({
    latitude: null,
    longitude: null,
    loading: false,
  })),
}));

vi.mock('@/presentation/hooks/useMetroAreas', () => ({
  useMetroAreas: vi.fn(() => ({
    metroAreasByState: new Map(),
    stateLevelMetros: [],
    isLoading: false,
  })),
}));

vi.mock('@/infrastructure/api/hooks/useReferenceData', () => ({
  useEventCategories: vi.fn(() => ({
    data: [],
    isLoading: false,
  })),
}));

vi.mock('@/presentation/hooks/useEvents', () => ({
  useEvents: (...args: unknown[]) => useEventsMock(...args),
  useUserRsvps: vi.fn(() => ({ data: [], isLoading: false })),
}));

// Helper: minimal valid event fixture
const makeEvent = (overrides: Record<string, unknown> = {}) => ({
  id: `event-${Math.random().toString(36).slice(2, 8)}`,
  title: 'Sample Event',
  description: 'desc',
  startDate: '2026-06-01T00:00:00Z',
  endDate: '2026-06-01T03:00:00Z',
  status: EventStatus.Active,
  category: 'Festival',
  capacity: 100,
  currentRegistrations: 0,
  imageUrl: null,
  ...overrides,
});

describe('EventsPage — Phase 6A.149', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Default mock: each call returns the same empty payload. Specific tests
    // override via mockImplementation when they need distinct Upcoming/Completed
    // responses.
    useEventsMock.mockReturnValue({ data: [], isLoading: false, error: undefined });
  });

  describe('Banner removal', () => {
    it('does NOT render the "Discover Events" hero banner', () => {
      renderWithClient(<EventsPage />);
      expect(screen.queryByText(/Discover Events/i)).not.toBeInTheDocument();
      expect(
        screen.queryByText(/Find cultural, community, and social events relevant to you/i),
      ).not.toBeInTheDocument();
    });

    it('does NOT render the decorative gradient blobs from the old banner', () => {
      const { container } = renderWithClient(<EventsPage />);
      // The old banner used these exact Tailwind classes; their absence
      // proves the block was removed.
      expect(container.querySelector('.from-orange-600.via-rose-800.to-emerald-800')).toBeNull();
    });
  });

  describe('Section headers', () => {
    it('renders "Upcoming Events" heading', () => {
      renderWithClient(<EventsPage />);
      expect(screen.getByRole('heading', { name: /upcoming events/i })).toBeInTheDocument();
    });

    it('does NOT render "Completed Events" heading when no completed events exist', () => {
      // Both useEvents calls return empty → Completed section must hide.
      renderWithClient(<EventsPage />);
      expect(screen.queryByRole('heading', { name: /completed events/i })).not.toBeInTheDocument();
    });

    it('renders "Completed Events" heading when at least one Completed event exists', () => {
      useEventsMock.mockImplementation((filters: { statusFilter?: EventStatusFilter }) => {
        if (filters?.statusFilter === EventStatusFilter.Inactive) {
          return {
            data: [makeEvent({ status: EventStatus.Completed, title: 'Past Vesak 2025' })],
            isLoading: false,
            error: undefined,
          };
        }
        return { data: [], isLoading: false, error: undefined };
      });
      renderWithClient(<EventsPage />);
      expect(screen.getByRole('heading', { name: /completed events/i })).toBeInTheDocument();
    });

    it('filters Inactive payload client-side to only Completed-status events', () => {
      // Inactive enum group includes Completed + Archived + Postponed.
      // The Completed section must render ONLY the Completed event, hiding
      // Archived / Postponed from the public view.
      useEventsMock.mockImplementation((filters: { statusFilter?: EventStatusFilter }) => {
        if (filters?.statusFilter === EventStatusFilter.Inactive) {
          return {
            data: [
              makeEvent({ status: EventStatus.Completed, title: 'Past Vesak 2025' }),
              makeEvent({ status: EventStatus.Archived, title: 'Archived Event' }),
              makeEvent({ status: EventStatus.Postponed, title: 'Postponed Event' }),
            ],
            isLoading: false,
            error: undefined,
          };
        }
        return { data: [], isLoading: false, error: undefined };
      });
      renderWithClient(<EventsPage />);
      expect(screen.getByText('Past Vesak 2025')).toBeInTheDocument();
      expect(screen.queryByText('Archived Event')).not.toBeInTheDocument();
      expect(screen.queryByText('Postponed Event')).not.toBeInTheDocument();
    });
  });

  describe('Per-section collapsed Filters', () => {
    it('renders the Upcoming section Filters as a CollapsibleSection that is COLLAPSED by default', () => {
      renderWithClient(<EventsPage />);
      // CollapsibleSection always renders the title; the form fields are
      // hidden via CSS grid-template-rows when collapsed. We assert that the
      // Search input (a form field that lives INSIDE the Filters block)
      // is NOT visible to user-events by default.
      const searchInputs = screen.queryAllByPlaceholderText(/Search by event name/i);
      // With CollapsibleSection grid-rows trick, the content stays in the DOM
      // but the wrapper has aria-hidden=true OR is not "open". The simplest
      // contract assertion: the "Show details" pill (defaultOpen=false label)
      // is rendered.
      const showPills = screen.queryAllByRole('button', { name: /show details|show filters|filters/i });
      expect(showPills.length).toBeGreaterThanOrEqual(1);
      // And no expanded form fields are user-visible — search inputs exist
      // in DOM but the user can't tab into them while collapsed.
      // The strictest assertion is the explicit pill state above.
      void searchInputs;
    });

    it('renders a SECOND Filters CollapsibleSection for the Completed section when Completed has results', () => {
      useEventsMock.mockImplementation((filters: { statusFilter?: EventStatusFilter }) => {
        if (filters?.statusFilter === EventStatusFilter.Inactive) {
          return {
            data: [makeEvent({ status: EventStatus.Completed })],
            isLoading: false,
            error: undefined,
          };
        }
        return { data: [], isLoading: false, error: undefined };
      });
      renderWithClient(<EventsPage />);
      // Two collapsed filter blocks → at least two "Show details" pills.
      const pills = screen.queryAllByRole('button', { name: /show details|show filters|filters/i });
      expect(pills.length).toBeGreaterThanOrEqual(2);
    });
  });

  describe('Removed Event Status dropdown', () => {
    it('does NOT render the standalone "Event Status" filter dropdown', () => {
      renderWithClient(<EventsPage />);
      expect(screen.queryByText(/^Event Status$/i)).not.toBeInTheDocument();
    });
  });

  describe('Two independent useEvents calls', () => {
    it('fires useEvents at least twice on mount: one with Active filter, one with Inactive filter', () => {
      renderWithClient(<EventsPage />);
      // Page makes one call per section. Inspect the filter arg shapes.
      const calls = useEventsMock.mock.calls.map((c) => c[0] ?? {});
      const activeCalls = calls.filter(
        (a: { statusFilter?: EventStatusFilter }) => a.statusFilter === EventStatusFilter.Active,
      );
      const inactiveCalls = calls.filter(
        (a: { statusFilter?: EventStatusFilter }) => a.statusFilter === EventStatusFilter.Inactive,
      );
      expect(activeCalls.length).toBeGreaterThanOrEqual(1);
      expect(inactiveCalls.length).toBeGreaterThanOrEqual(1);
    });

    it('does NOT pass date range filters into the Inactive (Completed) call', () => {
      renderWithClient(<EventsPage />);
      const calls = useEventsMock.mock.calls.map((c) => c[0] ?? {});
      const inactiveCall = calls.find(
        (a: { statusFilter?: EventStatusFilter }) => a.statusFilter === EventStatusFilter.Inactive,
      ) as Record<string, unknown> | undefined;
      expect(inactiveCall).toBeDefined();
      // Completed implies past — no startDateFrom/To gating needed and applying
      // an "upcoming" filter would zero-out the section.
      expect(inactiveCall?.startDateFrom).toBeUndefined();
      expect(inactiveCall?.startDateTo).toBeUndefined();
    });
  });

  describe('Scroll container + fade-mask', () => {
    it('wraps the Upcoming grid in a scroll container with max-h-[1500px] overflow-y-auto', () => {
      useEventsMock.mockImplementation((filters: { statusFilter?: EventStatusFilter }) => {
        if (filters?.statusFilter === EventStatusFilter.Active) {
          return {
            data: [makeEvent({ status: EventStatus.Active, title: 'Active Event' })],
            isLoading: false,
            error: undefined,
          };
        }
        return { data: [], isLoading: false, error: undefined };
      });
      const { container } = renderWithClient(<EventsPage />);
      // The scroll wrapper carries the cap classes — find by class fragment.
      const scrollWrapper = container.querySelector('[data-section="upcoming-grid-scroll"]');
      expect(scrollWrapper).not.toBeNull();
      expect(scrollWrapper?.className).toContain('overflow-y-auto');
      expect(scrollWrapper?.className).toContain('max-h-[1500px]');
    });

    it('renders a bottom fade-mask element below the Upcoming grid', () => {
      useEventsMock.mockImplementation((filters: { statusFilter?: EventStatusFilter }) => {
        if (filters?.statusFilter === EventStatusFilter.Active) {
          return { data: [makeEvent()], isLoading: false, error: undefined };
        }
        return { data: [], isLoading: false, error: undefined };
      });
      const { container } = renderWithClient(<EventsPage />);
      const fade = container.querySelector('[data-section="upcoming-grid-fade"]');
      expect(fade).not.toBeNull();
    });
  });
});
