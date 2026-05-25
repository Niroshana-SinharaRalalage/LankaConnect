import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { AuthEncouragementModal } from '../AuthEncouragementModal';

/**
 * Phase 6A.142 — Auth Encouragement Modal RTL coverage.
 *
 * Soft-nudge modal shown when an unauthenticated user clicks Register on a paid event.
 * Offers Sign In / Sign Up / Continue as Guest. Generic (context prop) so it can be
 * reused for add-ons, donations, and refunds later.
 *
 * Architect-required behaviours:
 *   - Three explicit exits (Sign In, Sign Up, Continue as Guest) + standard close (X).
 *   - Sign In / Sign Up navigate via Next router with the caller's redirectTo encoded.
 *   - Continue as Guest fires the parent callback exactly once.
 *   - Backdrop / ESC / X close the modal WITHOUT triggering the guest callback.
 *   - ARIA: role=dialog, aria-modal=true, aria-labelledby pointing at the title.
 *   - Focus moves to the title on open, returns to the previously-focused element on close.
 *   - benefits prop overrides the default event-paid bullets.
 */

const pushMock = vi.fn();
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: pushMock }),
}));

beforeEach(() => {
  pushMock.mockReset();
});

const baseProps = {
  open: true,
  onOpenChange: vi.fn(),
  context: 'event-paid' as const,
  redirectTo: '/events/abc?intent=register',
  onContinueAsGuest: vi.fn(),
};

describe('AuthEncouragementModal — Phase 6A.142', () => {
  it('renders all four exits (Sign In, Sign Up, Continue as Guest, close)', () => {
    render(<AuthEncouragementModal {...baseProps} onOpenChange={vi.fn()} onContinueAsGuest={vi.fn()} />);
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /sign up/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /continue as guest/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /close/i })).toBeInTheDocument();
  });

  it('renders default event-paid benefit bullets', () => {
    render(<AuthEncouragementModal {...baseProps} onOpenChange={vi.fn()} onContinueAsGuest={vi.fn()} />);
    expect(screen.getByText(/manage your tickets/i)).toBeInTheDocument();
    expect(screen.getByText(/refunds/i)).toBeInTheDocument();
    expect(screen.getByText(/add-ons/i)).toBeInTheDocument();
    expect(screen.getByText(/sign-ups/i)).toBeInTheDocument();
  });

  it('renders custom benefits when the benefits prop is supplied', () => {
    render(
      <AuthEncouragementModal
        {...baseProps}
        onOpenChange={vi.fn()}
        onContinueAsGuest={vi.fn()}
        benefits={['Custom benefit alpha', 'Custom benefit beta']}
      />,
    );
    expect(screen.getByText('Custom benefit alpha')).toBeInTheDocument();
    expect(screen.getByText('Custom benefit beta')).toBeInTheDocument();
    // default bullets should NOT be rendered
    expect(screen.queryByText(/manage your tickets/i)).not.toBeInTheDocument();
  });

  it('Sign In click pushes /login with the redirectTo URL-encoded', () => {
    render(<AuthEncouragementModal {...baseProps} onOpenChange={vi.fn()} onContinueAsGuest={vi.fn()} />);
    fireEvent.click(screen.getByRole('button', { name: /sign in/i }));
    expect(pushMock).toHaveBeenCalledTimes(1);
    expect(pushMock).toHaveBeenCalledWith(
      `/login?redirect=${encodeURIComponent('/events/abc?intent=register')}`,
    );
  });

  it('Sign Up click pushes /register with the redirectTo URL-encoded', () => {
    render(<AuthEncouragementModal {...baseProps} onOpenChange={vi.fn()} onContinueAsGuest={vi.fn()} />);
    fireEvent.click(screen.getByRole('button', { name: /sign up/i }));
    expect(pushMock).toHaveBeenCalledTimes(1);
    expect(pushMock).toHaveBeenCalledWith(
      `/register?redirect=${encodeURIComponent('/events/abc?intent=register')}`,
    );
  });

  it('Continue as Guest fires onContinueAsGuest exactly once and does not navigate', () => {
    const onContinueAsGuest = vi.fn();
    render(
      <AuthEncouragementModal {...baseProps} onOpenChange={vi.fn()} onContinueAsGuest={onContinueAsGuest} />,
    );
    fireEvent.click(screen.getByRole('button', { name: /continue as guest/i }));
    expect(onContinueAsGuest).toHaveBeenCalledTimes(1);
    expect(pushMock).not.toHaveBeenCalled();
  });

  it('Close (X) fires onOpenChange(false) and does NOT fire onContinueAsGuest', () => {
    const onOpenChange = vi.fn();
    const onContinueAsGuest = vi.fn();
    render(
      <AuthEncouragementModal
        {...baseProps}
        onOpenChange={onOpenChange}
        onContinueAsGuest={onContinueAsGuest}
      />,
    );
    fireEvent.click(screen.getByRole('button', { name: /close/i }));
    expect(onOpenChange).toHaveBeenCalledWith(false);
    expect(onContinueAsGuest).not.toHaveBeenCalled();
  });

  it('ESC key fires onOpenChange(false) and does NOT fire onContinueAsGuest', () => {
    const onOpenChange = vi.fn();
    const onContinueAsGuest = vi.fn();
    render(
      <AuthEncouragementModal
        {...baseProps}
        onOpenChange={onOpenChange}
        onContinueAsGuest={onContinueAsGuest}
      />,
    );
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(onOpenChange).toHaveBeenCalledWith(false);
    expect(onContinueAsGuest).not.toHaveBeenCalled();
  });

  it('exposes role=dialog, aria-modal, and aria-labelledby pointing at the title', () => {
    render(<AuthEncouragementModal {...baseProps} onOpenChange={vi.fn()} onContinueAsGuest={vi.fn()} />);
    const dialog = screen.getByRole('dialog');
    expect(dialog).toHaveAttribute('aria-modal', 'true');
    const labelId = dialog.getAttribute('aria-labelledby');
    expect(labelId).toBeTruthy();
    const titleEl = document.getElementById(labelId as string);
    expect(titleEl).not.toBeNull();
    expect(titleEl).toHaveTextContent(/sign in/i);
  });

  it('moves focus to the title on open and restores it to the previously-focused element on close', () => {
    // Render a trigger button outside the modal, focus it, then mount modal with open=true
    const Wrapper = ({ open }: { open: boolean }) => (
      <>
        <button type="button" data-testid="trigger">Open</button>
        <AuthEncouragementModal
          open={open}
          onOpenChange={vi.fn()}
          context="event-paid"
          redirectTo="/events/abc?intent=register"
          onContinueAsGuest={vi.fn()}
        />
      </>
    );

    const { rerender } = render(<Wrapper open={false} />);
    const trigger = screen.getByTestId('trigger');
    trigger.focus();
    expect(document.activeElement).toBe(trigger);

    // Open the modal — focus should move into the dialog (title)
    rerender(<Wrapper open={true} />);
    const dialog = screen.getByRole('dialog');
    const labelId = dialog.getAttribute('aria-labelledby') as string;
    const titleEl = document.getElementById(labelId);
    expect(document.activeElement).toBe(titleEl);

    // Close the modal — focus should return to the trigger
    rerender(<Wrapper open={false} />);
    expect(document.activeElement).toBe(trigger);
  });
});
