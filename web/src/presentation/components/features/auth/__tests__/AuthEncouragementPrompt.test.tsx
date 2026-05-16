import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { AuthEncouragementPrompt } from '../AuthEncouragementPrompt';

/**
 * Phase 6A.142 — Auth Encouragement Prompt RTL coverage.
 *
 * Lightweight panel shown in place of the RSVP form when an unauthenticated user
 * is viewing a paid event and has not yet acknowledged guest mode. Clicking the
 * Register button opens the AuthEncouragementModal.
 */

describe('AuthEncouragementPrompt — Phase 6A.142', () => {
  it('renders a teaser and a Register button labeled for paid events', () => {
    render(<AuthEncouragementPrompt onClick={vi.fn()} eventTitle="Avurudu Night" />);
    // teaser should mention that signing in is recommended
    expect(screen.getByText(/sign(ing)? in/i)).toBeInTheDocument();
    // a clear Register / Continue CTA must be present
    expect(screen.getByRole('button', { name: /register/i })).toBeInTheDocument();
  });

  it('invokes onClick exactly once when the Register button is clicked', () => {
    const onClick = vi.fn();
    render(<AuthEncouragementPrompt onClick={onClick} eventTitle="Avurudu Night" />);
    fireEvent.click(screen.getByRole('button', { name: /register/i }));
    expect(onClick).toHaveBeenCalledTimes(1);
  });
});
