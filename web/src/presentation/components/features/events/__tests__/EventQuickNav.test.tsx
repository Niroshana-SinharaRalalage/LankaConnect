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
});
