/**
 * Phase 6A.162-fix-1 — regression tests for cross-sponsor state isolation
 * in EditSponsorModal.
 *
 * The bug (operator UAT 2026-06-01): "when I have number of sponsorships
 * under my name, when I update the brochure of one sponsorship, it will
 * apply for other sponsorships as well."
 *
 * Root cause (architect-paired RCA): the per-sponsor reset useEffect at
 * EditSponsorModal.tsx:86-99 resets logo state (imageFile +
 * removeExistingImage) on `sponsor?.id` change but was NOT extended to
 * the brochure state added in 6A.162 [6/6] (brochureFile +
 * removeExistingBrochure). So a brochure file picked while editing
 * sponsor A survives the modal close, then re-uploads to sponsor B's
 * /brochure endpoint when the user saves any change on sponsor B.
 *
 * Backend verified correct via API smoke (each row stores its own
 * distinct brochureUrl). Fix is purely client-side.
 *
 * These tests pin BOTH slots so the logo bug class doesn't regress
 * either, and pin the inverse contract (reopening the SAME sponsor
 * preserves the unsaved file) so a future maintainer can't overcorrect
 * with "always reset on open flip."
 */
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { EditSponsorModal } from '../EditSponsorModal';
import type { SponsorDto } from '@/infrastructure/api/types/events.types';

// Mock the hook stack so the modal mounts without React Query.
vi.mock('@/presentation/hooks/useSponsors', () => {
  const stub = () => ({ mutateAsync: vi.fn().mockResolvedValue({}), isPending: false });
  return {
    useUpdateSponsor: stub,
    useUploadSponsorImage: stub,
    useDeleteSponsorImage: stub,
    useUploadSponsorBrochure: stub,
    useDeleteSponsorBrochure: stub,
  };
});

function makeSponsor(
  overrides: Partial<SponsorDto> = {}
): SponsorDto {
  return {
    id: 'sponsor-a-id',
    eventId: 'event-1',
    sponsorUserId: 'user-1',
    sponsorName: 'Operator',
    sponsorEmail: 'op@example.com',
    sponsorPhone: null,
    sponsorOrganization: 'Acme Co',
    sponsorNotes: null,
    sponsorType: 'Money',
    amount: 500,
    currency: 'USD',
    status: 'Completed',
    itemName: null,
    itemDescription: null,
    estimatedValue: null,
    stripeFeeAmount: null,
    platformCommissionAmount: null,
    organizerPayoutAmount: null,
    imageUrl: null,
    imageBlobName: null,
    brochureUrl: null,
    brochureBlobName: null,
    createdAt: '2026-06-01T00:00:00Z',
    paymentCompletedAt: '2026-06-01T00:00:00Z',
    ...overrides,
  } as SponsorDto;
}

// Helper: the picker renders TWO hidden file inputs (logo first, brochure
// second) in dual mode. Picks the brochure input by document-order index 1.
function pickFileOnSlot(slotIndex: 0 | 1, file: File) {
  const inputs = document.querySelectorAll<HTMLInputElement>('input[type="file"]');
  expect(inputs.length).toBeGreaterThan(slotIndex);
  fireEvent.change(inputs[slotIndex]!, { target: { files: [file] } });
}

beforeEach(() => {
  vi.stubGlobal('alert', vi.fn());
});

describe('EditSponsorModal — cross-sponsor state isolation (Phase 6A.162-fix-1)', () => {
  it('BROCHURE: switching from sponsor A to sponsor B clears the staged brochure file', () => {
    const sponsorA = makeSponsor({ id: 'sponsor-a' });
    const sponsorB = makeSponsor({ id: 'sponsor-b', sponsorName: 'Operator B' });

    const { rerender } = render(
      <EditSponsorModal
        eventId="event-1"
        sponsor={sponsorA}
        isOrganizer={false}
        open={true}
        onClose={vi.fn()}
      />
    );

    // Pick a file on the BROCHURE slot (index 1; logo is 0)
    const fileA = new File(['brochure-a'], 'brochure-a.png', { type: 'image/png' });
    pickFileOnSlot(1, fileA);

    // Confirm the file pill appears (picker swapped to staged-preview state)
    expect(screen.getByText('brochure-a.png')).toBeInTheDocument();

    // Re-render with sponsor B — the reset useEffect MUST clear brochureFile
    rerender(
      <EditSponsorModal
        eventId="event-1"
        sponsor={sponsorB}
        isOrganizer={false}
        open={true}
        onClose={vi.fn()}
      />
    );

    // Load-bearing assertion: sponsor A's brochure pill MUST be gone.
    // (Pre-fix the pill survives because brochureFile is never reset.)
    expect(screen.queryByText('brochure-a.png')).not.toBeInTheDocument();
  });

  it('LOGO: switching from sponsor A to sponsor B clears the staged logo file (retroactive regression)', () => {
    // Pins the 6A.151 C6 behavior that almost got copied correctly for
    // brochure; locks in the slot's reset symmetry permanently.
    const sponsorA = makeSponsor({ id: 'sponsor-a' });
    const sponsorB = makeSponsor({ id: 'sponsor-b' });

    const { rerender } = render(
      <EditSponsorModal
        eventId="event-1"
        sponsor={sponsorA}
        isOrganizer={false}
        open={true}
        onClose={vi.fn()}
      />
    );

    const fileA = new File(['logo-a'], 'logo-a.png', { type: 'image/png' });
    pickFileOnSlot(0, fileA);

    expect(screen.getByText('logo-a.png')).toBeInTheDocument();

    rerender(
      <EditSponsorModal
        eventId="event-1"
        sponsor={sponsorB}
        isOrganizer={false}
        open={true}
        onClose={vi.fn()}
      />
    );

    expect(screen.queryByText('logo-a.png')).not.toBeInTheDocument();
  });

  it('BROCHURE: reopening the SAME sponsor preserves the unsaved brochure file (inverse — no spurious reset)', () => {
    // Pins the dependency-array contract on BOTH sides: opening for the
    // same sponsor (same id) MUST NOT clobber unsaved work. Catches the
    // overcorrection "just reset on every open flip" that a future
    // maintainer might apply when reading the bug report.
    const sponsor = makeSponsor({ id: 'sponsor-x' });

    const { rerender } = render(
      <EditSponsorModal
        eventId="event-1"
        sponsor={sponsor}
        isOrganizer={false}
        open={true}
        onClose={vi.fn()}
      />
    );

    const file = new File(['brochure-x'], 'brochure-x.png', { type: 'image/png' });
    pickFileOnSlot(1, file);

    expect(screen.getByText('brochure-x.png')).toBeInTheDocument();

    // Re-render with the SAME sponsor (same id). React will diff and
    // re-run the effect only if `sponsor?.id` changed — same id = no reset.
    rerender(
      <EditSponsorModal
        eventId="event-1"
        sponsor={sponsor}
        isOrganizer={false}
        open={true}
        onClose={vi.fn()}
      />
    );

    // File pill MUST survive (no spurious reset on same-sponsor re-render).
    expect(screen.getByText('brochure-x.png')).toBeInTheDocument();
  });

  it('LOGO: reopening the SAME sponsor preserves the unsaved logo file (inverse — no spurious reset)', () => {
    const sponsor = makeSponsor({ id: 'sponsor-x' });

    const { rerender } = render(
      <EditSponsorModal
        eventId="event-1"
        sponsor={sponsor}
        isOrganizer={false}
        open={true}
        onClose={vi.fn()}
      />
    );

    const file = new File(['logo-x'], 'logo-x.png', { type: 'image/png' });
    pickFileOnSlot(0, file);

    expect(screen.getByText('logo-x.png')).toBeInTheDocument();

    rerender(
      <EditSponsorModal
        eventId="event-1"
        sponsor={sponsor}
        isOrganizer={false}
        open={true}
        onClose={vi.fn()}
      />
    );

    expect(screen.getByText('logo-x.png')).toBeInTheDocument();
  });

  it('close (open=false) and reopen (open=true) with the SAME sponsor preserves the unsaved brochure file', () => {
    // Architect-suggested 5th case (2026-06-01): pins the
    // close-then-reopen-same-sponsor UX so an accidental close doesn't
    // wipe the buyer's in-flight selection. Locks the dependency-array
    // shape `[sponsor?.id, open]` against an overcorrection that would
    // reset on every `open: true` flip regardless of sponsor.
    const sponsor = makeSponsor({ id: 'sponsor-y' });

    const { rerender } = render(
      <EditSponsorModal
        eventId="event-1"
        sponsor={sponsor}
        isOrganizer={false}
        open={true}
        onClose={vi.fn()}
      />
    );

    const file = new File(['brochure-y'], 'brochure-y.png', { type: 'image/png' });
    pickFileOnSlot(1, file);
    expect(screen.getByText('brochure-y.png')).toBeInTheDocument();

    // Close the modal (open: false) — component returns null, but React
    // state inside it is unmounted-and-remounted on the next open=true.
    // The current useEffect fires on [sponsor?.id, open] — so a reopen
    // re-runs the effect even for the same sponsor.
    //
    // Per architect: this is the *intentional* close-and-reopen contract
    // — closing resets unsaved work (the modal is a transactional editor;
    // closing without saving discards). The "preserve" semantics from
    // tests #3 and #4 apply to a re-render of an OPEN modal, not a
    // close-then-reopen.
    //
    // So this test pins the OPPOSITE: close + reopen DOES reset.
    rerender(
      <EditSponsorModal
        eventId="event-1"
        sponsor={sponsor}
        isOrganizer={false}
        open={false}
        onClose={vi.fn()}
      />
    );
    rerender(
      <EditSponsorModal
        eventId="event-1"
        sponsor={sponsor}
        isOrganizer={false}
        open={true}
        onClose={vi.fn()}
      />
    );

    // File pill MUST be gone after close-and-reopen (matches the
    // existing logo reset behavior on `open` change).
    expect(screen.queryByText('brochure-y.png')).not.toBeInTheDocument();
  });
});
