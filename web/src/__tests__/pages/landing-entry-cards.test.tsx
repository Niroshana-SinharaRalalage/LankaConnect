/**
 * Landing Page Sub-Brand Entry Cards
 *
 * Covers the umbrella landing page's sub-brand entry points: the existing
 * LankaEvents card and the new LankaSeyla card.
 *
 * The LankaEvents assertions are deliberately written against the card's
 * behaviour BEFORE it was extracted into <EntryCard>, so the extraction has to
 * prove it changed nothing rather than merely claiming it. CLAUDE.md Section 3
 * treats refactors of working UI as high-risk; this file is that mitigation.
 *
 * See docs/superpowers/specs/2026-08-04-lankaseyla-landing-entry-design.md
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import LankaConnectHome from '@/app/page';

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), prefetch: vi.fn() }),
}));

// next/image needs a plain <img> under jsdom; keep alt/src so a11y is assertable.
vi.mock('next/image', () => ({
  default: ({ src, alt, width, height }: any) => (
    // eslint-disable-next-line @next/next/no-img-element
    <img src={typeof src === 'string' ? src : ''} alt={alt} width={width} height={height} />
  ),
}));

// The canvas/SVG world map is irrelevant here and expensive under jsdom.
vi.mock('@/presentation/components/features/landing/WorldMapAnimation', () => ({
  WorldMapAnimation: () => <div data-testid="world-map" />,
  THEMES: [
    { key: 'satellite-navy', nodeFill: '#4FC3F7' },
    { key: 'b', nodeFill: '#fff' },
    { key: 'c', nodeFill: '#fff' },
  ],
}));

vi.mock('@/presentation/components/layout/Footer', () => ({
  default: () => <div data-testid="footer" />,
}));

vi.mock('@/presentation/store/useAuthStore', () => ({
  useAuthStore: () => ({ isAuthenticated: false, user: null, clearAuth: vi.fn() }),
  useHasHydrated: () => true,
}));

const LANKA_SEYLA_URL = 'https://lankaseyla.lankaconnect.app/';

describe('Landing page sub-brand entry cards', () => {
  describe('LankaEvents card (regression guard — must survive the EntryCard extraction)', () => {
    it('links to the internal /lanka-events route', () => {
      render(<LankaConnectHome />);
      const card = screen.getByRole('link', { name: /LankaEvents/i });
      expect(card).toHaveAttribute('href', '/lanka-events');
    });

    it('does NOT open in a new tab', () => {
      render(<LankaConnectHome />);
      const card = screen.getByRole('link', { name: /LankaEvents/i });
      expect(card).not.toHaveAttribute('target');
    });

    it('keeps its "Event Planner" badge and tagline', () => {
      render(<LankaConnectHome />);
      expect(screen.getByText('Event Planner')).toBeInTheDocument();
      expect(screen.getByText('Plan Your Event with Ease')).toBeInTheDocument();
    });

    it('keeps its logo with a non-empty alt', () => {
      render(<LankaConnectHome />);
      const logo = screen.getByAltText('LankaEvents');
      expect(logo).toHaveAttribute('src', '/lanka-events.png');
    });
  });

  describe('LankaSeyla card', () => {
    it('renders as a second entry point', () => {
      render(<LankaConnectHome />);
      expect(screen.getByRole('link', { name: /LankaSeyla/i })).toBeInTheDocument();
    });

    it('points at the external storefront', () => {
      render(<LankaConnectHome />);
      const card = screen.getByRole('link', { name: /LankaSeyla/i });
      expect(card).toHaveAttribute('href', LANKA_SEYLA_URL);
    });

    it('opens in a new tab without leaking window.opener', () => {
      render(<LankaConnectHome />);
      const card = screen.getByRole('link', { name: /LankaSeyla/i });
      expect(card).toHaveAttribute('target', '_blank');
      expect(card.getAttribute('rel')).toContain('noopener');
      expect(card.getAttribute('rel')).toContain('noreferrer');
    });

    it('shows its badge and tagline', () => {
      render(<LankaConnectHome />);
      expect(screen.getByText('Clothing Store')).toBeInTheDocument();
      expect(screen.getByText('Tradition Woven with Elegance')).toBeInTheDocument();
    });

    it('renders the square lockup logo with a non-empty alt', () => {
      render(<LankaConnectHome />);
      const logo = screen.getByAltText('LankaSeyla');
      expect(logo).toHaveAttribute('src', '/lanka-seyla.png');
    });
  });

  describe('cross-card invariants', () => {
    it('renders exactly two sub-brand entry cards', () => {
      render(<LankaConnectHome />);
      expect(screen.getAllByTestId(/^entry-card-/)).toHaveLength(2);
    });

    it('shows the LIVE indicator only on LankaEvents — it would misrepresent an external site', () => {
      render(<LankaConnectHome />);
      const live = screen.getAllByText('Live');
      expect(live).toHaveLength(1);
      expect(screen.getByTestId('entry-card-lanka-events')).toContainElement(live[0]);
    });
  });
});
