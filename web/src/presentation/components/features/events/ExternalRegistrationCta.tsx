'use client';

import { ExternalLink, Info } from 'lucide-react';
import { type EventDto } from '@/infrastructure/api/types/events.types';

/**
 * Phase 8X.7+8 (rewritten Phase 8X.11) — Public-facing registration card for ExternalPaid events.
 *
 * Replaces RsvpFormSection on the event detail page when
 * `event.paymentMode === EventPaymentMode.ExternalPaid`.
 *
 * Phase 8X.11 — URL is now OPTIONAL. The card branches on which fields are populated:
 *   - URL + (anything else): primary outbound CTA button + secondary instructions card
 *   - URL only:               outbound CTA button alone
 *   - Instructions only:      "Organiser instructions" card promoted to primary; no button
 *   - Vendor name only:       "Registration handled by {Vendor}" + contact-organiser hint
 *   - All three null/empty:   friendly "Contact the organiser for registration details" card
 *     (architect-approved fallback per product-owner Q2 = B; not an error condition)
 *
 * Security:
 *   - CTA `<a>` uses `target="_blank" rel="noopener noreferrer nofollow"` (anti-window.opener,
 *     anti-search-engine-pumping, anti-phishing).
 *   - Instructions rendered via {text} (NOT dangerouslySetInnerHTML) — any embedded HTML
 *     or `<script>` tags appear as literal characters. XSS prevention is render-side per
 *     architect verdict; backend stores raw payload.
 */
export interface ExternalRegistrationCtaProps {
  event: EventDto;
  /**
   * Phase 8X.12 — optional notice surfaced when this CTA replaces the user's
   * existing RSVP context (e.g. mid-refund, expired-checkout, incomplete-payment).
   * The page-level early-return passes a state-specific message so the user
   * isn't dropped into the CTA without context.
   */
  priorRegistrationNotice?: string;
}

export function ExternalRegistrationCta({ event, priorRegistrationNotice }: ExternalRegistrationCtaProps) {
  const url = event.externalRegistrationUrl?.trim() || null;
  const vendorName = event.externalRegistrationVendorName?.trim() || null;
  const instructions = event.externalRegistrationInstructions?.trim() || null;

  const hasUrl = url !== null;
  const hasInstructions = instructions !== null;
  const hasVendor = vendorName !== null;
  const allEmpty = !hasUrl && !hasInstructions && !hasVendor;

  // Phase 8X.12 — pricing is now optional. When the event has no on-platform
  // pricing configured, the description copy switches to the user-locked phrase
  // "See external site or reach out organizer for pricing". Detection: legacy
  // ticketPriceAmount is null/zero AND no advanced pricing shape is configured.
  const hasOnPlatformPricing =
    (event.ticketPriceAmount != null && event.ticketPriceAmount > 0) ||
    event.hasDualPricing ||
    event.hasGroupPricing ||
    (event.ticketTiers && event.ticketTiers.length > 0);

  // Card heading is consistent across branches; vendor name personalises it when set.
  const heading = hasVendor
    ? `Registration handled by ${vendorName}`
    : 'External registration';

  const pricingSnippet = hasOnPlatformPricing
    ? 'Pricing is shown here for reference'
    : 'See external site or reach out organizer for pricing';

  return (
    <div
      className="p-5 bg-gradient-to-br from-blue-50 to-indigo-50 border-2 border-blue-200 rounded-lg space-y-4"
      data-testid="external-registration-cta"
    >
      {priorRegistrationNotice && (
        <div
          className="p-3 bg-amber-50 border border-amber-200 rounded text-sm text-amber-900"
          data-testid="external-registration-cta-prior-notice"
        >
          {priorRegistrationNotice}
        </div>
      )}

      <div className="flex items-start gap-3">
        <div className="flex-1">
          <h3 className="text-base font-semibold text-neutral-900 mb-1">{heading}</h3>
          <p className="text-sm text-neutral-600">
            {hasUrl
              ? `This event uses an external registration page. ${pricingSnippet} — you'll complete checkout on ${vendorName ?? "the organiser's site"}.`
              : `This event uses external registration. ${pricingSnippet}; see below for how to register.`}
          </p>
        </div>
      </div>

      {/* Primary CTA — only when URL is present. */}
      {hasUrl && (
        <>
          <a
            href={url!}
            target="_blank"
            rel="noopener noreferrer nofollow"
            className="inline-flex items-center justify-center gap-2 w-full px-6 py-3 bg-orange-500 hover:bg-orange-600 text-white font-semibold rounded-lg shadow-sm transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-orange-500"
            data-testid="external-registration-cta-link"
          >
            <span>{vendorName ? `Buy on ${vendorName}` : 'Buy Ticket / Register Externally'}</span>
            <ExternalLink className="h-4 w-4" aria-hidden="true" />
          </a>

          <p className="text-xs text-neutral-500 flex items-center gap-1.5">
            <Info className="h-3.5 w-3.5 flex-shrink-0" aria-hidden="true" />
            <span>You'll leave LankaConnect to complete registration on the organiser's site.</span>
          </p>
        </>
      )}

      {/* Instructions card. When URL is also present, this is secondary. When URL is
          absent, this becomes the primary user-facing content. */}
      {hasInstructions && (
        <div
          className="mt-2 p-3 bg-white border border-neutral-200 rounded text-sm text-neutral-700"
          data-testid="external-registration-cta-instructions"
        >
          <div className="text-xs font-semibold text-neutral-500 uppercase tracking-wide mb-1.5">
            Organiser instructions
          </div>
          {/* Plain-text rendering: whitespace-pre-wrap preserves line breaks; using {text}
              (not dangerouslySetInnerHTML) ensures any embedded HTML/script appears as
              literal characters. XSS defence per architect verdict 2026-05-07. */}
          <p className="whitespace-pre-wrap break-words">{instructions}</p>
        </div>
      )}

      {/* Fallback when organiser has supplied nothing yet. Architect-approved per
          product owner Q2 = B (allow-save-with-empty-fields). The ExternalPaid event
          is still valid; the public page just nudges users to contact the organiser. */}
      {allEmpty && (
        <div
          className="mt-2 p-3 bg-white border border-neutral-200 rounded text-sm text-neutral-700"
          data-testid="external-registration-cta-empty"
        >
          <p>
            The organiser hasn't yet provided registration details on this page. Please contact
            the event organiser directly for instructions on how to register.
          </p>
        </div>
      )}
    </div>
  );
}
