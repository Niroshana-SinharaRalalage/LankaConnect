/**
 * Phase 6A.156-fix-2 — modal-isolation regression test.
 *
 * Operator UAT hit a bug where clicking the modal's "Create Package" submit
 * button also submitted EventEditForm. Two interlocking causes:
 *   (a) Button.tsx didn't default type="button", so unmarked <Button> calls
 *       acted as submit buttons inside the parent <form>.
 *   (b) The modal renders its own <form>, which is DOM-nested inside
 *       EventEditForm's <form>. Nested forms are invalid HTML; browsers strip
 *       the inner form, so the modal's submit re-submitted the outer one.
 *
 * The fix portals the modal to document.body (escaping the parent form's DOM
 * tree) AND defaults Button to type="button". These tests pin BOTH contracts:
 * the modal's submit must NOT bubble into a parent form, and the modal must
 * still call its own submit handler (onSubmitOverride in local mode).
 */
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { SponsorshipPackageEditModal } from '../SponsorshipPackageEditModal';

// Hook stubs — the modal still calls the React Query mutation hooks at the
// top of the component (rules of hooks), but in local mode they're never
// invoked. We stub them so the tree mounts without a QueryClient.
vi.mock('@/presentation/hooks/useSponsorshipPackages', () => ({
  useCreateSponsorshipPackage: () => ({
    mutateAsync: vi.fn().mockResolvedValue(undefined),
    isPending: false,
  }),
  useUpdateSponsorshipPackage: () => ({
    mutateAsync: vi.fn().mockResolvedValue(undefined),
    isPending: false,
  }),
}));

beforeEach(() => {
  vi.stubGlobal('alert', vi.fn());
});

describe('SponsorshipPackageEditModal — form-nesting isolation', () => {
  it('does NOT submit a parent <form> when the modal submit button is clicked', async () => {
    const onSubmitOverride = vi.fn().mockResolvedValue(undefined);
    const onClose = vi.fn();
    const onSaved = vi.fn();
    const parentFormSubmit = vi.fn((e: { preventDefault: () => void }) => e.preventDefault());

    render(
      <form onSubmit={parentFormSubmit} aria-label="parent">
        <SponsorshipPackageEditModal
          eventId=""
          pkg={null}
          isOpen={true}
          onClose={onClose}
          onSaved={onSaved}
          onSubmitOverride={onSubmitOverride}
        />
      </form>
    );

    // Fill the required name + price so validation passes.
    const nameInput = screen.getByLabelText(/Package Name/i);
    fireEvent.change(nameInput, { target: { value: 'Smoke Gold' } });
    const priceInput = screen.getByLabelText(/^Price/i);
    fireEvent.change(priceInput, { target: { value: '100' } });

    // Click the modal's create submit button.
    const createButton = screen.getByRole('button', { name: /Create Package|Save Changes/i });
    fireEvent.click(createButton);

    // The modal's own submit MUST run.
    await waitFor(() => expect(onSubmitOverride).toHaveBeenCalledTimes(1));
    expect(onSubmitOverride.mock.calls[0][0]).toMatchObject({
      name: 'Smoke Gold',
      price: 100,
    });

    // The parent <form>'s onSubmit MUST NOT have been triggered — this is the
    // load-bearing assertion that catches the form-nesting bug class.
    expect(parentFormSubmit).not.toHaveBeenCalled();
  });

  it('does NOT submit a parent <form> when the Cancel button is clicked', () => {
    const onClose = vi.fn();
    const parentFormSubmit = vi.fn((e: { preventDefault: () => void }) => e.preventDefault());

    render(
      <form onSubmit={parentFormSubmit}>
        <SponsorshipPackageEditModal
          eventId=""
          pkg={null}
          isOpen={true}
          onClose={onClose}
          onSaved={vi.fn()}
          onSubmitOverride={vi.fn().mockResolvedValue(undefined)}
        />
      </form>
    );

    const cancelButton = screen.getByRole('button', { name: /Cancel/i });
    fireEvent.click(cancelButton);

    expect(onClose).toHaveBeenCalled();
    expect(parentFormSubmit).not.toHaveBeenCalled();
  });

  it('does NOT submit a parent <form> when the close (X) icon button is clicked', () => {
    const onClose = vi.fn();
    const parentFormSubmit = vi.fn((e: { preventDefault: () => void }) => e.preventDefault());

    render(
      <form onSubmit={parentFormSubmit}>
        <SponsorshipPackageEditModal
          eventId=""
          pkg={null}
          isOpen={true}
          onClose={onClose}
          onSaved={vi.fn()}
          onSubmitOverride={vi.fn().mockResolvedValue(undefined)}
        />
      </form>
    );

    // The X icon has aria-label="Close" on its raw <button>.
    fireEvent.click(screen.getByRole('button', { name: /Close/i }));

    expect(onClose).toHaveBeenCalled();
    expect(parentFormSubmit).not.toHaveBeenCalled();
  });
});
