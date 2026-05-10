'use client';

/**
 * Phase 8YB.2 — Sandbox route for the legacy contained-column hero (Option C).
 *
 * The user picked the full-bleed hero (Option E) as the new default for `/events/{id}`.
 * This route stays around so they can keep iterating on the contained variant without
 * disturbing the primary route — same page logic, just `heroVariant="contained"`.
 *
 * URL: /events/{id}/v2
 */

import { EventDetailPageInternal } from '../page';

export default function EventDetailPageV2({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  return <EventDetailPageInternal params={params} heroVariant="contained" />;
}
