'use client';

import { useEffect, useState, use } from 'react';
import { useRouter, notFound } from 'next/navigation';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';

/**
 * Phase 6A.154 — public vanity URL route.
 *
 * Matches single-segment paths like `lankaconnect.app/cleveland-show`. Next.js
 * static + explicit dynamic routes resolve BEFORE this catch-all, so all the
 * top-level directories (events, login, dashboard, …) win automatically; this
 * route only fires when nothing else matches.
 *
 * v1 strategy: client-side fetch + `router.replace` to the canonical
 * `/events/{id}` URL once the slug resolves. This is the minimum viable
 * version that ships the feature end-to-end. SSR with `generateMetadata`
 * (for OG / Twitter Card tags) is deferred to a follow-up phase so the
 * scope of this PR stays bounded.
 *
 * Unknown slug → `notFound()` (Next.js 404 page).
 */
export default function VanitySlugPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = use(params);
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const event = await eventsRepository.getByVanitySlug(slug);
        if (cancelled) return;
        if (!event?.id) {
          // notFound() inside useEffect would throw outside the render
          // boundary. Set a sentinel and let render handle the 404 below.
          setError('not-found');
          return;
        }
        router.replace(`/events/${event.id}`);
      } catch (err) {
        if (cancelled) return;
        // eslint-disable-next-line no-console
        console.error('[6A.154] slug resolution failed', err);
        setError('not-found');
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [slug, router]);

  if (error === 'not-found') {
    notFound();
  }

  // Brief loading shimmer while the slug resolves. Most users will see this
  // for ~200ms; the redirect lands on the existing event detail page.
  return (
    <div className="flex min-h-screen items-center justify-center">
      <div className="text-center">
        <div className="h-8 w-8 mx-auto animate-spin rounded-full border-4 border-orange-200 border-t-orange-600" />
        <p className="mt-4 text-sm text-neutral-500">Loading event…</p>
      </div>
    </div>
  );
}
