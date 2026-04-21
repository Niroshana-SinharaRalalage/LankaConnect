import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import {
  SignUpCommitmentModal,
  defaultSignUpCommitmentLabels,
  volunteerCommitmentLabels,
} from '@/presentation/components/features/events/SignUpCommitmentModal';
import {
  SignUpItemCategory,
  SignUpItemType,
  type SlotBasedItemDto,
} from '@/infrastructure/api/types/events.types';

/**
 * Phase 7D.1 Step 21: labels prop regression guard.
 *
 * CLAUDE.md Section 3 forbids silent UI drift. The modal must render exactly
 * the same user-facing copy when `labels` is omitted as it did pre-refactor,
 * and the new `volunteerCommitmentLabels` factory must actually change the
 * copy (otherwise the wrapper components in Phase F/G will be a no-op).
 */
vi.mock('@/presentation/store/useAuthStore', () => ({
  useAuthStore: () => ({ user: null }),
}));

const slotItem: SlotBasedItemDto = {
  id: 'item-1',
  itemDescription: 'Food Committee',
  itemCategory: SignUpItemCategory.Mandatory,
  itemType: SignUpItemType.Slot,
  notes: null,
  commitments: [],
  isFullyCommitted: false,
  isOpenItem: false,
  totalSlots: 5,
  filledSlots: 0,
  remainingSlots: 5,
};

function renderModal(labels?: Parameters<typeof SignUpCommitmentModal>[0]['labels']) {
  return render(
    <SignUpCommitmentModal
      open={true}
      onOpenChange={() => {}}
      item={slotItem}
      signUpListId="list-1"
      eventId="evt-1"
      onCommit={async () => {}}
      labels={labels}
    />
  );
}

describe('SignUpCommitmentModal — default labels (regression guard)', () => {
  it('renders the pre-Phase-7D.1 create-mode title when no labels prop is passed', () => {
    renderModal();
    expect(screen.getByText('Sign Up to Bring Item')).toBeInTheDocument();
    expect(
      screen.getByText('Fill in your details to sign up for bringing this item')
    ).toBeInTheDocument();
  });

  it('uses "Number of Slots" for slot-based items by default', () => {
    renderModal();
    expect(screen.getByText(/Number of Slots/)).toBeInTheDocument();
  });

  it('uses the existing "Confirm Sign Up" submit copy by default', () => {
    renderModal();
    expect(screen.getByRole('button', { name: 'Confirm Sign Up' })).toBeInTheDocument();
  });

  it('defaultSignUpCommitmentLabels matches the hardcoded pre-refactor strings', () => {
    expect(defaultSignUpCommitmentLabels.createTitle).toBe('Sign Up to Bring Item');
    expect(defaultSignUpCommitmentLabels.updateTitle).toBe('Update Sign Up');
    expect(defaultSignUpCommitmentLabels.submitCreate).toBe('Confirm Sign Up');
    expect(defaultSignUpCommitmentLabels.submitUpdate).toBe('Update Sign Up');
    expect(defaultSignUpCommitmentLabels.quantityLabel(true)).toBe('Number of Slots');
    expect(defaultSignUpCommitmentLabels.quantityLabel(false)).toBe('Quantity');
    expect(defaultSignUpCommitmentLabels.unitLabel(true)).toBe('slot(s)');
    expect(defaultSignUpCommitmentLabels.unitLabel(false)).toBe('item(s)');
  });
});

describe('SignUpCommitmentModal — volunteerCommitmentLabels override', () => {
  it('renders the volunteer title when volunteerCommitmentLabels is passed', () => {
    renderModal(volunteerCommitmentLabels);
    expect(screen.getByText('Volunteer for This Role')).toBeInTheDocument();
    expect(screen.queryByText('Sign Up to Bring Item')).not.toBeInTheDocument();
  });

  it('relabels the quantity field for volunteer lists', () => {
    renderModal(volunteerCommitmentLabels);
    expect(screen.getByText(/Number of Volunteers/)).toBeInTheDocument();
    expect(screen.queryByText(/Number of Slots/)).not.toBeInTheDocument();
  });

  it('relabels the submit button for volunteer lists', () => {
    renderModal(volunteerCommitmentLabels);
    expect(screen.getByRole('button', { name: 'Confirm Volunteer' })).toBeInTheDocument();
  });
});
