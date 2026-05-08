'use client';

/**
 * Phase 8YB.1 — TEMPORARY hero-layout comparison route.
 *
 * Renders the same event detail page logic as `/events/{id}` but with the full-bleed
 * hero variant (Option E) so the user can A/B compare against the contained hero
 * (Option C) on staging. Once a winner is picked, this route + the heroVariant prop
 * will be removed and the chosen layout becomes the only hero.
 *
 * URL: /events/{id}/v2
 */

import { EventDetailPageInternal } from '../page';

export default function EventDetailPageV2({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  return <EventDetailPageInternal params={params} heroVariant="fullWidth" />;
}
