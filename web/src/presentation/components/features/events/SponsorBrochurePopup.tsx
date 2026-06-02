'use client';

import { useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { X } from 'lucide-react';

interface SponsorBrochurePopupProps {
  /**
   * Sponsor whose brochure (or logo fallback) should display. When null,
   * the popup is closed. Parent owns the open/close state.
   */
  sponsor: {
    id: string;
    sponsorName: string;
    sponsorOrganization?: string | null;
    imageUrl?: string | null;
    brochureUrl?: string | null;
  } | null;
  onClose: () => void;
}

/**
 * Phase 6A.162 — public sponsor click-to-popup. Replaces the
 * scroll-to-section behavior on the public sponsor strips.
 *
 * UX: shows the brochure full-size when set, falls back to the logo
 * when the sponsor has no brochure. When neither is set the popup
 * surfaces a generic placeholder (the sponsor card with initials is
 * the only entry surface anyway, so this branch is rare).
 *
 * Portal'd to document.body per the 6A.156-fix-2 form-nesting contract
 * (locked after the EventEditForm modal-flash bug). Submit-event
 * isolation is moot here — the popup has no <form> — but the portal
 * also lets the image escape any `overflow:hidden` ancestor.
 *
 * ESC + backdrop-click + X icon all close.
 */
export function SponsorBrochurePopup({ sponsor, onClose }: SponsorBrochurePopupProps) {
  // SSR-safe portal mount guard (same pattern as the existing 6A.156-fix-2
  // PurchaseSponsorshipPackageModal). Renders nothing on the server pass.
  const [mounted, setMounted] = useState(false);
  useEffect(() => {
    setMounted(true);
  }, []);

  // ESC key support — listener attached only while the popup is open.
  useEffect(() => {
    if (!sponsor) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [sponsor, onClose]);

  if (!sponsor || !mounted) return null;

  // Brochure wins; logo is the documented fallback per the user-locked
  // UX decision 2026-06-01: "If a user clicks on the logo, it should
  // open up the brochure/flyer in a popup window. If there is no
  // brochure/flyer added, then just display the logo on a popup window."
  const displayUrl = sponsor.brochureUrl || sponsor.imageUrl || null;
  const altText = sponsor.sponsorOrganization || sponsor.sponsorName;
  const title = sponsor.sponsorOrganization || sponsor.sponsorName;

  return createPortal(
    <div
      role="dialog"
      aria-modal="true"
      aria-label={`Sponsor: ${title}`}
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
      onClick={onClose}
    >
      <div
        className="relative max-h-[90vh] max-w-3xl bg-white rounded-lg shadow-2xl overflow-hidden"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header — sponsor name + close button */}
        <div className="flex items-center justify-between px-4 py-2 border-b border-neutral-200 bg-white sticky top-0 z-10">
          <h3 className="text-sm font-semibold text-neutral-800 truncate">{title}</h3>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close"
            className="p-1 rounded hover:bg-neutral-100 text-neutral-500"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Body — brochure (or logo fallback) full-size */}
        <div className="overflow-auto" style={{ maxHeight: 'calc(90vh - 48px)' }}>
          {displayUrl ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={displayUrl}
              alt={altText}
              className="block w-full h-auto object-contain"
            />
          ) : (
            <div className="flex h-64 items-center justify-center text-sm text-neutral-500">
              No image available for this sponsor.
            </div>
          )}
        </div>
      </div>
    </div>,
    document.body
  );
}
