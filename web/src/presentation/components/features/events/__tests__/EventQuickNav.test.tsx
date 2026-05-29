import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { EventQuickNav, type EventQuickNavPill } from '../EventQuickNav';

const buildPill = (overrides: Partial<EventQuickNavPill> = {}): EventQuickNavPill => ({
  id: 'registration',
  label: 'Register',
  icon: <span data-testid="icon-register" aria-hidden="true">R</span>,
  show: true,
  ...overrides,
});

describe('EventQuickNav (Phase 8YB.4)', () => {
  describe('Visibility filter', () => {
    it('renders only pills with show=true', () => {
      render(
        <EventQuickNav
          pills={[
            buildPill({ id: 'registration', label: 'Register', show: true }),
            buildPill({ id: 'donations', label: 'Donate', show: false }),
            buildPill({ id: 'signup-lists', label: 'Signup Lists', show: true }),
            buildPill({ id: 'signup-forms', label: 'Signup Forms', show: false }),
          ]}
        />,
      );

      expect(screen.getByRole('button', { name: /register/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /signup lists/i })).toBeInTheDocument();
      expect(screen.queryByRole('button', { name: /donate/i })).not.toBeInTheDocument();
      expect(screen.queryByRole('button', { name: /signup forms/i })).not.toBeInTheDocument();
    });

    it('renders nothing when no pill has show=true', () => {
      const { container } = render(
        <EventQuickNav
          pills={[
            buildPill({ id: 'a', label: 'A', show: false }),
            buildPill({ id: 'b', label: 'B', show: false }),
          ]}
        />,
      );
      // Component returns null in this case so nothing should be in the DOM.
      expect(container.querySelector('button')).toBeNull();
    });

    it('renders nothing when pills array is empty', () => {
      const { container } = render(<EventQuickNav pills={[]} />);
      expect(container.querySelector('button')).toBeNull();
    });
  });

  describe('Pill rendering', () => {
    it('renders the icon + label for each visible pill', () => {
      render(
        <EventQuickNav
          pills={[
            buildPill({
              id: 'registration',
              label: 'Register',
              icon: <span data-testid="icon-1" aria-hidden="true" />,
            }),
            buildPill({
              id: 'donations',
              label: 'Donate',
              icon: <span data-testid="icon-2" aria-hidden="true" />,
            }),
          ]}
        />,
      );
      expect(screen.getByTestId('icon-1')).toBeInTheDocument();
      expect(screen.getByTestId('icon-2')).toBeInTheDocument();
      expect(screen.getByText(/register/i)).toBeInTheDocument();
      expect(screen.getByText(/donate/i)).toBeInTheDocument();
    });
  });

  describe('Click → smooth scroll to section', () => {
    let scrollIntoViewMock: ReturnType<typeof vi.fn>;

    beforeEach(() => {
      scrollIntoViewMock = vi.fn();
      // jsdom has no scrollIntoView by default; install a spy on the prototype.
      Element.prototype.scrollIntoView = scrollIntoViewMock;
    });

    it('clicking a pill scrolls to its anchored section by id', () => {
      // Anchor target needs to exist for getElementById to find it.
      const target = document.createElement('div');
      target.id = 'registration';
      document.body.appendChild(target);

      try {
        render(<EventQuickNav pills={[buildPill({ id: 'registration', label: 'Register' })]} />);
        fireEvent.click(screen.getByRole('button', { name: /register/i }));

        expect(scrollIntoViewMock).toHaveBeenCalledWith({
          behavior: 'smooth',
          block: 'start',
        });
      } finally {
        target.remove();
      }
    });

    it('clicking a pill whose anchor is missing does not throw', () => {
      // No anchor in DOM — getElementById returns null.
      render(<EventQuickNav pills={[buildPill({ id: 'no-such-anchor', label: 'Phantom' })]} />);
      expect(() =>
        fireEvent.click(screen.getByRole('button', { name: /phantom/i })),
      ).not.toThrow();
      expect(scrollIntoViewMock).not.toHaveBeenCalled();
    });
  });

  // Phase 6A.155: Promoted Register/RSVP CTA. The registration pill is the
  // page's primary conversion action and must visually dominate the row.
  // emphasis="primary" applies solid-fill brand styling; other pills stay
  // outlined (default).
  describe('Primary emphasis (Register/RSVP CTA)', () => {
    it('applies primary styling when emphasis="primary"', () => {
      render(
        <EventQuickNav
          pills={[buildPill({ id: 'registration', label: 'Register', emphasis: 'primary' })]}
        />,
      );
      const btn = screen.getByRole('button', { name: /register/i });
      // Stable test surface — explicit data attribute drives CSS branch.
      expect(btn).toHaveAttribute('data-emphasis', 'primary');
      // Primary pill is filled (white text on brand orange) and bolder.
      expect(btn.className).toMatch(/text-white/);
      expect(btn.className).toMatch(/font-semibold/);
    });

    it('default pills are not marked primary and keep the outlined look', () => {
      render(
        <EventQuickNav
          pills={[
            buildPill({ id: 'donations', label: 'Donate' }),
            buildPill({ id: 'sponsors', label: 'Sponsor', emphasis: 'default' }),
          ]}
        />,
      );
      const donate = screen.getByRole('button', { name: /donate/i });
      const sponsor = screen.getByRole('button', { name: /sponsor/i });
      expect(donate).toHaveAttribute('data-emphasis', 'default');
      expect(sponsor).toHaveAttribute('data-emphasis', 'default');
      // Default pills should NOT carry font-semibold (primary marker).
      expect(donate.className).not.toMatch(/font-semibold/);
      expect(sponsor.className).not.toMatch(/font-semibold/);
    });

    it('primary pill is keyboard-focusable (no tabIndex=-1)', () => {
      render(
        <EventQuickNav
          pills={[buildPill({ id: 'registration', label: 'Register', emphasis: 'primary' })]}
        />,
      );
      const btn = screen.getByRole('button', { name: /register/i });
      expect(btn.tabIndex).not.toBe(-1);
    });

    it('preserves DOM order — primary pill rendered first when listed first', () => {
      render(
        <EventQuickNav
          pills={[
            buildPill({ id: 'registration', label: 'Register', emphasis: 'primary' }),
            buildPill({ id: 'donations', label: 'Donate' }),
            buildPill({ id: 'sponsors', label: 'Sponsor' }),
          ]}
        />,
      );
      const buttons = screen.getAllByRole('button');
      expect(buttons).toHaveLength(3);
      expect(buttons[0]).toHaveAccessibleName(/register/i);
      expect(buttons[1]).toHaveAccessibleName(/donate/i);
      expect(buttons[2]).toHaveAccessibleName(/sponsor/i);
    });

    it('primary pill click still scrolls to anchor (behavior unchanged)', () => {
      const scrollSpy = vi.fn();
      Element.prototype.scrollIntoView = scrollSpy;
      const target = document.createElement('div');
      target.id = 'registration';
      document.body.appendChild(target);

      try {
        render(
          <EventQuickNav
            pills={[buildPill({ id: 'registration', label: 'Register', emphasis: 'primary' })]}
          />,
        );
        fireEvent.click(screen.getByRole('button', { name: /register/i }));
        expect(scrollSpy).toHaveBeenCalledWith({ behavior: 'smooth', block: 'start' });
      } finally {
        target.remove();
      }
    });

    it('Mode-B RSVP label also receives primary emphasis', () => {
      // Mode B (HeadCount*) reuses the same pill descriptor with label="RSVP".
      // The visual treatment must apply regardless of label text.
      render(
        <EventQuickNav
          pills={[buildPill({ id: 'registration', label: 'RSVP', emphasis: 'primary' })]}
        />,
      );
      const btn = screen.getByRole('button', { name: /rsvp/i });
      expect(btn).toHaveAttribute('data-emphasis', 'primary');
      expect(btn.className).toMatch(/text-white/);
    });
  });
});
