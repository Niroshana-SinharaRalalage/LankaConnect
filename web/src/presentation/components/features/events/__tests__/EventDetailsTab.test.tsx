/**
 * Phase 6A.156-fix-3 — TDD coverage for the Sponsorship Packages display
 * section added to the Event Details tab.
 *
 * Scope is intentionally narrow: pin the LOAD-BEARING contract that the
 * useSponsorshipPackages hook is called with the right enabled flag based on
 * the two-layer gate (sponsorConfig.isEnabled AND sponsorConfig.enablePackages).
 * The table's HTML structure mirrors the existing Add-On Items table byte-for-byte
 * (lines 924-984 in EventDetailsTab.tsx) and is verified by operator UAT — the
 * existing codebase has zero unit tests for the AddOn table either, so we
 * follow that convention rather than gold-plate this one display section.
 *
 * The wider EventDetailsTab is huge (1000+ lines, lots of children); we mock
 * the heavy descendants and the routing/repo deps to keep the render fast and
 * deterministic.
 */
import { render } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { EventDetailsTab } from '../EventDetailsTab';
import type { EventDto } from '@/infrastructure/api/types/events.types';

// Routing — EventDetailsTab uses next/navigation's useRouter for inline edit nav.
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), refresh: vi.fn(), back: vi.fn() }),
}));

// Heavy children — stub so we don't drag in their hook trees / canvas APIs.
vi.mock('@/presentation/components/features/events/ImageUploader', () => ({
  ImageUploader: () => null,
}));
vi.mock('@/presentation/components/features/events/VideoUploader', () => ({
  VideoUploader: () => null,
}));

// Sibling hooks the tab already calls — return empty so the surrounding sections
// render cheaply.
vi.mock('@/presentation/hooks/useEmailGroups', () => ({
  useEmailGroups: () => ({ data: [] }),
}));
vi.mock('@/presentation/hooks/useAddOns', () => ({
  useAddOnDefinitions: () => ({ data: [] }),
}));

// Repository — EventDetailsTab imports eventsRepository for inline update
// handlers (max attendees, etc). Stub the module so the import resolves.
vi.mock('@/infrastructure/api/repositories/events.repository', () => ({
  eventsRepository: {
    updateMaxAttendees: vi.fn().mockResolvedValue(undefined),
  },
}));

// The unit under test — record every call to assert the (eventId, enabled) tuple.
const mockUseSponsorshipPackages = vi.fn();
vi.mock('@/presentation/hooks/useSponsorshipPackages', () => ({
  useSponsorshipPackages: (eventId: string, enabled: boolean) =>
    mockUseSponsorshipPackages(eventId, enabled),
}));

beforeEach(() => {
  mockUseSponsorshipPackages.mockReset();
  mockUseSponsorshipPackages.mockReturnValue({
    data: [],
    isLoading: false,
    error: null,
    refetch: vi.fn(),
  });
});

describe('EventDetailsTab — Sponsorship Packages section (6A.156-fix-3)', () => {
  it('calls useSponsorshipPackages with enabled=TRUE when sponsor + packages flags are both ON', () => {
    render(
      <EventDetailsTab
        event={makeEvent({ sponsorEnabled: true, packagesEnabled: true })}
        onRefetch={vi.fn().mockResolvedValue(undefined)}
        isDraft={false}
        isPublished
        isPublishing={false}
        isUnpublishing={false}
        onPublish={vi.fn().mockResolvedValue(undefined)}
        onUnpublish={vi.fn().mockResolvedValue(undefined)}
      />
    );
    expect(mockUseSponsorshipPackages).toHaveBeenCalledWith(EVENT_ID, true);
  });

  it('calls useSponsorshipPackages with enabled=FALSE when packages toggle is OFF', () => {
    render(
      <EventDetailsTab
        event={makeEvent({ sponsorEnabled: true, packagesEnabled: false })}
        onRefetch={vi.fn().mockResolvedValue(undefined)}
        isDraft={false}
        isPublished
        isPublishing={false}
        isUnpublishing={false}
        onPublish={vi.fn().mockResolvedValue(undefined)}
        onUnpublish={vi.fn().mockResolvedValue(undefined)}
      />
    );
    expect(mockUseSponsorshipPackages).toHaveBeenCalledWith(EVENT_ID, false);
  });

  it('calls useSponsorshipPackages with enabled=FALSE when sponsors are disabled (outer gate)', () => {
    render(
      <EventDetailsTab
        event={makeEvent({ sponsorEnabled: false, packagesEnabled: true })}
        onRefetch={vi.fn().mockResolvedValue(undefined)}
        isDraft={false}
        isPublished
        isPublishing={false}
        isUnpublishing={false}
        onPublish={vi.fn().mockResolvedValue(undefined)}
        onUnpublish={vi.fn().mockResolvedValue(undefined)}
      />
    );
    // Outer gate (sponsorConfig.isEnabled === false) collapses the inner gate
    // to false too — defence in depth so no fetch fires for sponsor-disabled
    // events even if a legacy row has stray enablePackages=true.
    expect(mockUseSponsorshipPackages).toHaveBeenCalledWith(EVENT_ID, false);
  });

  it('calls useSponsorshipPackages with enabled=FALSE when sponsorConfig is absent (legacy event)', () => {
    render(
      <EventDetailsTab
        event={makeEvent({ omitSponsorConfig: true })}
        onRefetch={vi.fn().mockResolvedValue(undefined)}
        isDraft={false}
        isPublished
        isPublishing={false}
        isUnpublishing={false}
        onPublish={vi.fn().mockResolvedValue(undefined)}
        onUnpublish={vi.fn().mockResolvedValue(undefined)}
      />
    );
    expect(mockUseSponsorshipPackages).toHaveBeenCalledWith(EVENT_ID, false);
  });
});

// ──────────────────────────────────────────────────────────────────────────────
// Fixtures
// ──────────────────────────────────────────────────────────────────────────────

const EVENT_ID = 'ad8903c4-e98e-49dd-b44e-d89f916c49dc';

interface MakeEventOpts {
  sponsorEnabled?: boolean;
  packagesEnabled?: boolean;
  omitSponsorConfig?: boolean;
}

function makeEvent(opts: MakeEventOpts = {}): EventDto {
  const { sponsorEnabled = true, packagesEnabled = false, omitSponsorConfig = false } = opts;
  // Cast: EventDto is large; we only populate the fields EventDetailsTab actually
  // reads in the render path under test. Tests fail fast if a missing field is
  // dereferenced, surfacing real coverage gaps.
  return {
    id: EVENT_ID,
    name: 'Smoke Event',
    description: 'For test',
    eventType: 'Cultural',
    paymentMode: 'FreeEvent',
    status: 'Published',
    venue: 'Aurora, OH',
    startDate: '2026-06-01T18:00:00Z',
    endDate: '2026-06-01T22:00:00Z',
    timezone: 'America/New_York',
    isPublic: true,
    addOnConfig: null,
    sponsorConfig: omitSponsorConfig
      ? null
      : {
          isEnabled: sponsorEnabled,
          acceptMoneySponsors: true,
          acceptItemSponsors: false,
          minSponsorAmount: null,
          sponsorMessage: null,
          showSponsorList: true,
          enablePackages: packagesEnabled,
        },
  } as unknown as EventDto;
}
