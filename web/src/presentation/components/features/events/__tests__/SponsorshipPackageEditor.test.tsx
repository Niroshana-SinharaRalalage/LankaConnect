/**
 * Phase 6A.156-fix — TDD tests for the dual-mode SponsorshipPackageEditor.
 *
 * The editor mirrors AddOnDefinitionEditor's shape:
 *   - local mode (no eventId): manages a PendingSponsorshipPackage[] in parent state,
 *     used inside EventCreationForm before the event exists
 *   - live mode (eventId provided): fetches via React Query hooks and mutates via API,
 *     used inside EventEditForm AND inside SponsorsManagementTab post-creation
 *
 * Same component, two mount sites — exactly the add-on pattern. These tests pin
 * the load-bearing contract; child Card + EditModal primitives are mocked so the
 * tests focus on dispatch logic, not rendering details (the card's own
 * presentation is covered by component-level tests for SponsorshipPackageCard).
 */
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import {
  SponsorshipPackageEditor,
  type PendingSponsorshipPackage,
} from '../SponsorshipPackageEditor';
import type { SponsorshipPackageDto } from '@/infrastructure/api/types/events.types';

// ──────────────────────────────────────────────────────────────────────────────
// Mocks
//
// Card: render a minimal stub exposing the name + a delete trigger so we can
//       assert dispatch wiring without entangling tests in card visuals.
// Modal: render an "open" indicator + a Save button hooked to a captured onSaved.
//        We don't drive the full form here — SponsorshipPackageEditModal already
//        owns its own validation tests.
// Hooks: live-mode hooks are mocked so we can drive the data shape per-test and
//        assert mutation invocation. React Query isn't needed for these unit
//        tests — the editor is the unit under test.
// ──────────────────────────────────────────────────────────────────────────────

vi.mock('../SponsorshipPackageCard', () => ({
  SponsorshipPackageCard: ({
    pkg,
    onEdit,
    onDelete,
  }: {
    pkg: { id: string; name: string };
    onEdit: (p: unknown) => void;
    onDelete: (p: unknown) => void;
  }) => (
    <div data-testid={`pkg-card-${pkg.id}`}>
      <span data-testid={`pkg-name-${pkg.id}`}>{pkg.name}</span>
      <button data-testid={`pkg-edit-${pkg.id}`} onClick={() => onEdit(pkg)}>
        edit
      </button>
      <button data-testid={`pkg-delete-${pkg.id}`} onClick={() => onDelete(pkg)}>
        delete
      </button>
    </div>
  ),
}));

vi.mock('../SponsorshipPackageEditModal', () => ({
  SponsorshipPackageEditModal: ({
    isOpen,
    onClose,
    onSaved,
    pkg,
    eventId,
  }: {
    isOpen: boolean;
    onClose: () => void;
    onSaved: () => void;
    pkg: { id: string; name: string } | null;
    eventId: string;
  }) =>
    isOpen ? (
      <div data-testid="modal-open">
        <span data-testid="modal-mode">{pkg ? `editing-${pkg.id}` : 'creating'}</span>
        <span data-testid="modal-eventid">{eventId}</span>
        <button data-testid="modal-saved" onClick={onSaved}>
          saved
        </button>
        <button data-testid="modal-close" onClick={onClose}>
          close
        </button>
      </div>
    ) : null,
}));

const mockUseSponsorshipPackages = vi.fn();
const mockDeleteMutate = vi.fn().mockResolvedValue(undefined);
const mockUploadImageMutate = vi.fn().mockResolvedValue(undefined);
const mockClearImageMutate = vi.fn().mockResolvedValue(undefined);

vi.mock('@/presentation/hooks/useSponsorshipPackages', () => ({
  useSponsorshipPackages: (eventId: string, enabled: boolean) =>
    mockUseSponsorshipPackages(eventId, enabled),
  useDeleteSponsorshipPackage: () => ({ mutateAsync: mockDeleteMutate }),
  useUploadSponsorshipPackageImage: () => ({ mutateAsync: mockUploadImageMutate }),
  useDeleteSponsorshipPackageImage: () => ({ mutateAsync: mockClearImageMutate }),
}));

// Confirm() is fired before delete in live mode; force-true so the delete path
// runs in tests. Same approach as production code.
beforeEach(() => {
  mockUseSponsorshipPackages.mockReset();
  // Default to a safe, fully-populated React Query result shape so any test
  // (including local-mode ones, which call the hook with enabled=false) gets
  // a non-throwing return value. Live-mode tests override per-case.
  mockUseSponsorshipPackages.mockReturnValue({
    data: undefined,
    isLoading: false,
    error: null,
    refetch: vi.fn(),
  });
  mockDeleteMutate.mockClear();
  mockUploadImageMutate.mockClear();
  mockClearImageMutate.mockClear();
  // jsdom doesn't implement window.confirm — stub to true so delete path runs.
  vi.stubGlobal('confirm', vi.fn(() => true));
  vi.stubGlobal('alert', vi.fn());
});

// ──────────────────────────────────────────────────────────────────────────────
// LOCAL MODE — used inside EventCreationForm before the event exists.
// Parent owns the PendingSponsorshipPackage[] via onPendingPackagesChange.
// ──────────────────────────────────────────────────────────────────────────────

describe('SponsorshipPackageEditor — local mode (event creation)', () => {
  it('renders the empty state when no pending packages', () => {
    render(
      <SponsorshipPackageEditor
        pendingPackages={[]}
        onPendingPackagesChange={() => {}}
      />
    );
    // Header must reflect zero
    expect(screen.getByText(/Sponsorship Packages \(0\)/i)).toBeInTheDocument();
    // Add CTA must be present so the organizer can start defining tiers
    // Header button is always rendered; empty-state CTA also renders when
    // packages.length === 0. We assert AT LEAST one exists.
    expect(screen.getAllByRole('button', { name: /Add Package|Create.*Package/i }).length).toBeGreaterThan(0);
  });

  it('renders one card per pending package with correct names', () => {
    const pending: PendingSponsorshipPackage[] = [
      makePending('local-a', 'Gold Tier'),
      makePending('local-b', 'Silver Tier'),
    ];
    render(
      <SponsorshipPackageEditor
        pendingPackages={pending}
        onPendingPackagesChange={() => {}}
      />
    );
    expect(screen.getByText(/Sponsorship Packages \(2\)/i)).toBeInTheDocument();
    expect(screen.getByTestId('pkg-name-local-a')).toHaveTextContent('Gold Tier');
    expect(screen.getByTestId('pkg-name-local-b')).toHaveTextContent('Silver Tier');
  });

  it('opens the modal in "creating" mode when Add Package is clicked', () => {
    render(
      <SponsorshipPackageEditor
        pendingPackages={[]}
        onPendingPackagesChange={() => {}}
      />
    );
    // Empty state renders both the header "Add Package" CTA and the empty-state
    // "Create your first package" CTA — both dispatch to handleAddNew, so we
    // pick the first (header) to drive the test deterministically.
    fireEvent.click(screen.getAllByRole('button', { name: /Add Package|Create.*Package/i })[0]);
    expect(screen.getByTestId('modal-open')).toBeInTheDocument();
    expect(screen.getByTestId('modal-mode')).toHaveTextContent('creating');
    // Local mode → modal must NOT receive a real eventId (sentinel empty string)
    expect(screen.getByTestId('modal-eventid')).toHaveTextContent('');
  });

  it('opens the modal in "editing" mode with the clicked package', () => {
    const pending: PendingSponsorshipPackage[] = [makePending('local-a', 'Gold Tier')];
    render(
      <SponsorshipPackageEditor
        pendingPackages={pending}
        onPendingPackagesChange={() => {}}
      />
    );
    fireEvent.click(screen.getByTestId('pkg-edit-local-a'));
    expect(screen.getByTestId('modal-mode')).toHaveTextContent('editing-local-a');
  });

  it('calls onPendingPackagesChange filtering out the deleted package', () => {
    const onChange = vi.fn();
    const pending: PendingSponsorshipPackage[] = [
      makePending('local-a', 'Gold Tier'),
      makePending('local-b', 'Silver Tier'),
    ];
    render(
      <SponsorshipPackageEditor
        pendingPackages={pending}
        onPendingPackagesChange={onChange}
      />
    );
    fireEvent.click(screen.getByTestId('pkg-delete-local-a'));
    expect(onChange).toHaveBeenCalledTimes(1);
    const remaining = onChange.mock.calls[0][0] as PendingSponsorshipPackage[];
    expect(remaining).toHaveLength(1);
    expect(remaining[0].localId).toBe('local-b');
  });

  it('does NOT call the live delete mutation when in local mode', () => {
    const pending: PendingSponsorshipPackage[] = [makePending('local-a', 'Gold Tier')];
    render(
      <SponsorshipPackageEditor
        pendingPackages={pending}
        onPendingPackagesChange={() => {}}
      />
    );
    fireEvent.click(screen.getByTestId('pkg-delete-local-a'));
    expect(mockDeleteMutate).not.toHaveBeenCalled();
  });

  it('calls the live-mode hook with enabled=false in local mode (no fetch fires)', () => {
    // React's rules-of-hooks force the query to be declared unconditionally,
    // but the editor passes enabled=false when eventId is absent so no network
    // request fires. We verify the disabled flag rather than the
    // call-count because the hook IS invoked.
    mockUseSponsorshipPackages.mockReturnValue({
      data: undefined,
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });
    render(
      <SponsorshipPackageEditor
        pendingPackages={[]}
        onPendingPackagesChange={() => {}}
      />
    );
    expect(mockUseSponsorshipPackages).toHaveBeenCalledWith(undefined, false);
  });
});

// ──────────────────────────────────────────────────────────────────────────────
// LIVE MODE — used inside EventEditForm + SponsorsManagementTab.
// Editor calls the live React Query hooks; parent passes only eventId.
// ──────────────────────────────────────────────────────────────────────────────

describe('SponsorshipPackageEditor — live mode (existing event)', () => {
  const liveEventId = '00000000-0000-0000-0000-eventabcd1234';

  it('calls useSponsorshipPackages with the eventId in live mode', () => {
    mockUseSponsorshipPackages.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });
    render(<SponsorshipPackageEditor eventId={liveEventId} />);
    expect(mockUseSponsorshipPackages).toHaveBeenCalledWith(liveEventId, true);
  });

  it('renders the fetched packages as cards', () => {
    mockUseSponsorshipPackages.mockReturnValue({
      data: [makeLive('uuid-a', 'Live Gold'), makeLive('uuid-b', 'Live Silver')],
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });
    render(<SponsorshipPackageEditor eventId={liveEventId} />);
    expect(screen.getByText(/Sponsorship Packages \(2\)/i)).toBeInTheDocument();
    expect(screen.getByTestId('pkg-name-uuid-a')).toHaveTextContent('Live Gold');
    expect(screen.getByTestId('pkg-name-uuid-b')).toHaveTextContent('Live Silver');
  });

  it('renders the loading state while the hook is pending', () => {
    mockUseSponsorshipPackages.mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
      refetch: vi.fn(),
    });
    render(<SponsorshipPackageEditor eventId={liveEventId} />);
    expect(screen.getByText(/Loading packages/i)).toBeInTheDocument();
  });

  it('opens the modal with the eventId on Add Package (so live save uses API hooks)', () => {
    mockUseSponsorshipPackages.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });
    render(<SponsorshipPackageEditor eventId={liveEventId} />);
    // Empty state renders both the header "Add Package" CTA and the empty-state
    // "Create your first package" CTA — both dispatch to handleAddNew, so we
    // pick the first (header) to drive the test deterministically.
    fireEvent.click(screen.getAllByRole('button', { name: /Add Package|Create.*Package/i })[0]);
    expect(screen.getByTestId('modal-eventid')).toHaveTextContent(liveEventId);
  });

  it('calls the live delete mutation when a package is deleted in live mode', async () => {
    mockUseSponsorshipPackages.mockReturnValue({
      data: [makeLive('uuid-a', 'Live Gold')],
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });
    render(<SponsorshipPackageEditor eventId={liveEventId} />);
    fireEvent.click(screen.getByTestId('pkg-delete-uuid-a'));
    // Drained from the click handler's microtask queue
    await Promise.resolve();
    expect(mockDeleteMutate).toHaveBeenCalledWith({
      eventId: liveEventId,
      packageId: 'uuid-a',
    });
  });
});

// ──────────────────────────────────────────────────────────────────────────────
// Helpers
// ──────────────────────────────────────────────────────────────────────────────

function makePending(
  localId: string,
  name: string
): PendingSponsorshipPackage {
  return {
    localId,
    name,
    description: null,
    price: 100,
    currency: 'USD',
    quantityLimit: null,
    sortOrder: 0,
    tier: null,
    perks: [],
    includedTicketCount: 0,
    isActive: true,
  };
}

function makeLive(id: string, name: string): SponsorshipPackageDto {
  return {
    id,
    eventId: '00000000-0000-0000-0000-eventabcd1234',
    name,
    description: null,
    priceAmount: 100,
    priceCurrency: 'USD',
    quantityLimit: null,
    quantitySold: 0,
    remainingStock: null,
    isActive: true,
    sortOrder: 0,
    imageUrl: null,
    imageBlobName: null,
    tier: null,
    perks: [],
    includedTicketCount: 0,
    createdAt: '2026-05-30T00:00:00Z',
    updatedAt: null,
  };
}
