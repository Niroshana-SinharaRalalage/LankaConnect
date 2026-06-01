/**
 * Phase 6A.162 — SponsorBrochurePopup vitest contract.
 *
 * Pins the user-locked UX (2026-06-01): click ANY sponsor card on the
 * public strip → portal'd popup showing the brochure full-size if set,
 * else the logo as fallback. Replaces today's scroll-to-section UX on
 * the SponsorsPreviewStrip + in-section sponsor wall.
 *
 * Load-bearing assertions:
 *  - popup renders nothing when sponsor=null
 *  - popup shows the BROCHURE URL when brochureUrl is set
 *  - popup falls back to the LOGO URL when only imageUrl is set
 *  - ESC closes
 *  - backdrop click closes
 *  - X icon closes
 */
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { SponsorBrochurePopup } from '../SponsorBrochurePopup';

const sponsorWithBoth = {
  id: 'sponsor-1',
  sponsorName: 'Jane Smith',
  sponsorOrganization: 'Acme Corp',
  imageUrl: 'https://blob.example.com/logo.png',
  brochureUrl: 'https://blob.example.com/brochure.png',
};

const sponsorLogoOnly = {
  id: 'sponsor-2',
  sponsorName: 'Bob Tay',
  sponsorOrganization: 'Tay Industries',
  imageUrl: 'https://blob.example.com/logo-only.png',
  brochureUrl: null,
};

describe('SponsorBrochurePopup — Phase 6A.162 click-to-popup contract', () => {
  it('renders nothing when sponsor is null', () => {
    const { container } = render(
      <SponsorBrochurePopup sponsor={null} onClose={vi.fn()} />
    );
    // Portal target is document.body; either nothing mounted there or
    // the wrapping container is empty. Assert no role=dialog exists.
    expect(container.firstChild).toBeNull();
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('renders the BROCHURE URL when sponsor has a brochure set', () => {
    render(
      <SponsorBrochurePopup sponsor={sponsorWithBoth} onClose={vi.fn()} />
    );

    expect(screen.getByRole('dialog')).toBeInTheDocument();
    // The displayed <img> must be the brochure, NOT the logo
    const img = screen.getByAltText(/Acme Corp/i);
    expect(img.getAttribute('src')).toBe('https://blob.example.com/brochure.png');
  });

  it('falls back to the LOGO URL when sponsor has no brochure', () => {
    render(
      <SponsorBrochurePopup sponsor={sponsorLogoOnly} onClose={vi.fn()} />
    );

    expect(screen.getByRole('dialog')).toBeInTheDocument();
    const img = screen.getByAltText(/Tay Industries/i);
    expect(img.getAttribute('src')).toBe('https://blob.example.com/logo-only.png');
  });

  it('calls onClose when the X icon button is clicked', () => {
    const onClose = vi.fn();
    render(
      <SponsorBrochurePopup sponsor={sponsorWithBoth} onClose={onClose} />
    );

    fireEvent.click(screen.getByRole('button', { name: /Close/i }));
    expect(onClose).toHaveBeenCalled();
  });

  it('calls onClose when the backdrop is clicked', () => {
    const onClose = vi.fn();
    render(
      <SponsorBrochurePopup sponsor={sponsorWithBoth} onClose={onClose} />
    );

    // Click the dialog wrapper itself (the backdrop). The inner content has
    // stopPropagation, so a click on the image must NOT close.
    fireEvent.click(screen.getByRole('dialog'));
    expect(onClose).toHaveBeenCalled();
  });

  it('calls onClose when ESC key is pressed', () => {
    const onClose = vi.fn();
    render(
      <SponsorBrochurePopup sponsor={sponsorWithBoth} onClose={onClose} />
    );

    fireEvent.keyDown(window, { key: 'Escape' });
    expect(onClose).toHaveBeenCalled();
  });
});
