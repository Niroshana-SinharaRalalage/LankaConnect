import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import {
  SignUpCommitmentModal,
} from '@/presentation/components/features/events/SignUpCommitmentModal';
import {
  SignUpItemCategory,
  SignUpItemType,
  type QuantityBasedItemDto,
} from '@/infrastructure/api/types/events.types';

/**
 * Phase 6A.140 regression guard. The pre-Phase-140 modal called
 * `eventsRepository.checkEventRegistrationByEmail` before submitting an
 * anonymous commitment, then rejected the submit when the email matched a
 * member account or was unregistered for the event. Phase 140 deletes the
 * pre-check entirely — the form submits directly through `onCommitAnonymous`
 * and the backend handles smart UserId resolution.
 *
 * If a refactor re-introduces the pre-check, these tests fail.
 */

// Module under test must never import eventsRepository now — track any future
// regression by spying on the repository module to confirm no method is called.
const checkEventRegistrationByEmailSpy = vi.fn();
vi.mock('@/infrastructure/api/repositories/events.repository', () => ({
  eventsRepository: {
    checkEventRegistrationByEmail: checkEventRegistrationByEmailSpy,
  },
}));

vi.mock('@/presentation/store/useAuthStore', () => ({
  useAuthStore: () => ({ user: null }),
}));

const quantityItem: QuantityBasedItemDto = {
  id: 'item-1',
  itemDescription: 'Rice',
  itemCategory: SignUpItemCategory.Mandatory,
  itemType: SignUpItemType.Quantity,
  notes: null,
  displayOrder: 0,
  commitments: [],
  isFullyCommitted: false,
  isOpenItem: false,
  targetQuantity: 10,
  totalCommitted: 0,
  remainingQuantity: 10,
};

describe('SignUpCommitmentModal — Phase 6A.140 smart resolve (no pre-check)', () => {
  beforeEach(() => {
    checkEventRegistrationByEmailSpy.mockClear();
  });

  it('submits anonymous commitment directly without calling checkEventRegistrationByEmail', async () => {
    const onCommitAnonymous = vi.fn(async () => {});
    const onOpenChange = vi.fn();

    render(
      <SignUpCommitmentModal
        open={true}
        onOpenChange={onOpenChange}
        item={quantityItem}
        signUpListId="list-1"
        eventId="evt-1"
        onCommit={async () => {}}
        onCommitAnonymous={onCommitAnonymous}
      />
    );

    fireEvent.change(screen.getByLabelText(/Your Name/i), { target: { value: 'Niro Sample' } });
    fireEvent.change(screen.getByLabelText(/Email Address/i), { target: { value: 'niro@example.com' } });
    fireEvent.click(screen.getByRole('button', { name: /Confirm Sign Up/i }));

    await waitFor(() => expect(onCommitAnonymous).toHaveBeenCalledTimes(1));
    expect(checkEventRegistrationByEmailSpy).not.toHaveBeenCalled();
    expect(onCommitAnonymous).toHaveBeenCalledWith(expect.objectContaining({
      signUpListId: 'list-1',
      itemId: 'item-1',
      contactEmail: 'niro@example.com',
      contactName: 'Niro Sample',
    }));
  });

  it('does not render the "Click here to log in" or "register for the event" inline links', () => {
    render(
      <SignUpCommitmentModal
        open={true}
        onOpenChange={() => {}}
        item={quantityItem}
        signUpListId="list-1"
        eventId="evt-1"
        onCommit={async () => {}}
        onCommitAnonymous={async () => {}}
      />
    );

    // The two link variants the old gated UI used to render must not exist.
    expect(screen.queryByText(/Click here to log in/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Click here to register for the event/i)).not.toBeInTheDocument();
  });
});
