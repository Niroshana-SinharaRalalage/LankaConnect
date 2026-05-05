import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { AddHeadCountModal } from '../AddHeadCountModal';
import { RegistrationMode } from '@/infrastructure/api/types/events.types';

/**
 * Phase 7F-D.4 — RTL coverage for AddHeadCountModal.
 *
 * Architect-required behaviours:
 *   - Mode-aware spinners render correctly per RegistrationMode (B1/B2/B3/B4).
 *   - Submit fires `eventsRepository.initiateAddHeadCount` with the right shape.
 *   - Free path (`checkoutSessionId === "free-no-stripe"`) calls onSuccess + closes.
 *   - Paid path redirects via window.location.
 */

const initiateAddHeadCountMock = vi.fn();
vi.mock('@/infrastructure/api/repositories/events.repository', () => ({
  eventsRepository: {
    initiateAddHeadCount: (...args: unknown[]) => initiateAddHeadCountMock(...args),
  },
}));

beforeEach(() => {
  initiateAddHeadCountMock.mockReset();
});

describe('AddHeadCountModal — Phase 7F-D.4', () => {
  it('renders B2 spinners (Adults / Children) for HeadCountByAge mode', () => {
    render(
      <AddHeadCountModal
        open
        onOpenChange={() => {}}
        registrationId="reg-1"
        mode={RegistrationMode.HeadCountByAge}
        maxAttendeesPerRegistration={10}
        currentAttendeeCount={3}
      />,
    );
    expect(screen.getByText('+ Adults')).toBeInTheDocument();
    expect(screen.getByText('+ Children')).toBeInTheDocument();
  });

  it('renders B4 spinners (4-leaf cross) for HeadCountByAgeAndGender mode', () => {
    render(
      <AddHeadCountModal
        open
        onOpenChange={() => {}}
        registrationId="reg-1"
        mode={RegistrationMode.HeadCountByAgeAndGender}
        maxAttendeesPerRegistration={10}
        currentAttendeeCount={2}
      />,
    );
    expect(screen.getByText('+ Adult Males')).toBeInTheDocument();
    expect(screen.getByText('+ Adult Females')).toBeInTheDocument();
    expect(screen.getByText('+ Child Males')).toBeInTheDocument();
    expect(screen.getByText('+ Child Females')).toBeInTheDocument();
  });

  it('submits the delta to the repository and calls onSuccess on free-event success', async () => {
    initiateAddHeadCountMock.mockResolvedValueOnce({
      success: true,
      checkoutSessionId: 'free-no-stripe',
      checkoutUrl: '',
      newAttendeesCount: 1,
    });
    const onSuccess = vi.fn();
    const onOpenChange = vi.fn();

    render(
      <AddHeadCountModal
        open
        onOpenChange={onOpenChange}
        registrationId="reg-1"
        mode={RegistrationMode.HeadCountByAge}
        maxAttendeesPerRegistration={10}
        currentAttendeeCount={3}
        onSuccess={onSuccess}
      />,
    );

    // Default state: adults=1, children=0 → delta=1.
    fireEvent.click(screen.getByRole('button', { name: /Continue/i }));

    await waitFor(() => expect(initiateAddHeadCountMock).toHaveBeenCalled());
    const call = initiateAddHeadCountMock.mock.calls[0];
    expect(call[0]).toBe('reg-1');
    expect(call[1].headCountDelta).toEqual({ adults: 1, children: 0 });
    await waitFor(() => expect(onSuccess).toHaveBeenCalled());
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });

  it('disables Continue when delta exceeds max-attendees cap', () => {
    render(
      <AddHeadCountModal
        open
        onOpenChange={() => {}}
        registrationId="reg-1"
        mode={RegistrationMode.HeadCountOnly}
        maxAttendeesPerRegistration={5}
        currentAttendeeCount={5}
      />,
    );

    // current=5, max=5 → remaining=0; default total=1 → over cap.
    expect(screen.getByText(/Adding:/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Continue/i })).toBeDisabled();
  });
});
