'use client';

import { ExternalLink, Info } from 'lucide-react';
import { type EventDto } from '@/infrastructure/api/types/events.types';

/**
 * Phase 8X.7+8 — Public-facing registration card for ExternalPaid events.
 *
 * Replaces RsvpFormSection on the event detail page when
 * `event.paymentMode === EventPaymentMode.ExternalPaid`.
 *
 * Renders:
 *  - Pricing summary (delegated to the existing pricing card; this component focuses on
 *    the CTA + vendor + instructions block).
 *  - Outbound CTA button — `<a target="_blank" rel="noopener noreferrer nofollow">` per
 *    architect verdict (anti-phishing / anti-window.opener / search-engine hygiene).
 *  - Optional plain-text instructions (whitespace-pre-wrap; rendered via {text} so
 *    `<script>` tags appear as literal text — XSS prevention is render-side per the
 *    architect verdict; backend stores raw payload).
 *  - "You'll leave LankaConnect" notice underneath the button.
 */
export interface ExternalRegistrationCtaProps {
  event: EventDto;
}

export function ExternalRegistrationCta({ event }: ExternalRegistrationCtaProps) {
  const url = event.externalRegistrationUrl;
  const vendorName = event.externalRegistrationVendorName;
  const instructions = event.externalRegistrationInstructions;

  // Defensive: backend should never send ExternalPaid without a URL (validator + domain
  // VO both enforce). If somehow null/empty, show a friendly fallback rather than a
  // broken anchor.
  if (!url) {
    return (
      <div
        role="alert"
        className="p-4 bg-amber-50 border border-amber-200 rounded-lg"
        data-testid="external-registration-cta-missing-url"
      >
        <p className="text-sm text-amber-800">
          The organiser has marked this event as external-payment but hasn't yet provided a
          registration link. Please check back later or contact the organiser.
        </p>
      </div>
    );
  }

  const buttonLabel = vendorName ? `Buy on ${vendorName}` : 'Buy Ticket / Register Externally';

  return (
    <div
      className="p-5 bg-gradient-to-br from-blue-50 to-indigo-50 border-2 border-blue-200 rounded-lg space-y-4"
      data-testid="external-registration-cta"
    >
      <div className="flex items-start gap-3">
        <div className="flex-1">
          <h3 className="text-base font-semibold text-neutral-900 mb-1">
            Registration handled by {vendorName ?? 'the organiser'}
          </h3>
          <p className="text-sm text-neutral-600">
            This event uses an external registration page. Pricing is shown here for reference —
            you'll complete checkout on {vendorName ?? "the organiser's site"}.
          </p>
        </div>
      </div>

      <a
        href={url}
        target="_blank"
        rel="noopener noreferrer nofollow"
        className="inline-flex items-center justify-center gap-2 w-full px-6 py-3 bg-orange-500 hover:bg-orange-600 text-white font-semibold rounded-lg shadow-sm transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-orange-500"
        data-testid="external-registration-cta-link"
      >
        <span>{buttonLabel}</span>
        <ExternalLink className="h-4 w-4" aria-hidden="true" />
      </a>

      <p className="text-xs text-neutral-500 flex items-center gap-1.5">
        <Info className="h-3.5 w-3.5 flex-shrink-0" aria-hidden="true" />
        <span>You'll leave LankaConnect to complete registration on the organiser's site.</span>
      </p>

      {instructions && instructions.trim().length > 0 && (
        <div
          className="mt-2 p-3 bg-white border border-neutral-200 rounded text-sm text-neutral-700"
          data-testid="external-registration-cta-instructions"
        >
          <div className="text-xs font-semibold text-neutral-500 uppercase tracking-wide mb-1.5">
            Organiser instructions
          </div>
          {/* Plain-text rendering: whitespace-pre-wrap preserves formatting; using {text}
              (not dangerouslySetInnerHTML) ensures any embedded HTML/script appears as
              literal characters. XSS defence per architect verdict 2026-05-07. */}
          <p className="whitespace-pre-wrap break-words">{instructions}</p>
        </div>
      )}
    </div>
  );
}
