'use client';

import * as React from 'react';
import { Sparkles } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';

/**
 * Phase 6A.144 — Auth Encouragement Prompt
 *
 * Lightweight panel rendered in place of the RSVP form when an unauthenticated
 * user is viewing a paid event and has not yet acknowledged guest mode for the
 * current session. Clicking Register opens the AuthEncouragementModal, where
 * the user can choose to sign in, sign up, or continue as guest.
 *
 * Kept as its own component (vs inlined in events/[id]/page.tsx) because
 * page.tsx is already ~1900 lines and because we expect to reuse this prompt
 * pattern for add-on / donation / refund surfaces in later phases.
 */

interface AuthEncouragementPromptProps {
  onClick: () => void;
  eventTitle: string;
}

export function AuthEncouragementPrompt({
  onClick,
  eventTitle,
}: AuthEncouragementPromptProps) {
  return (
    <div className="rounded-lg border border-orange-200 bg-gradient-to-br from-orange-50 via-white to-emerald-50 p-5 dark:border-orange-900/40 dark:from-orange-950/30 dark:via-neutral-900 dark:to-emerald-950/30">
      <div className="flex items-start gap-3">
        <Sparkles
          className="h-5 w-5 flex-shrink-0 text-orange-600"
          aria-hidden="true"
        />
        <div className="flex-1">
          <h3 className="text-base font-semibold text-neutral-900 dark:text-neutral-100">
            Register for {eventTitle}
          </h3>
          <p className="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
            This event has a ticket price. We recommend signing in so you can
            manage your tickets, add-ons, and refunds from your account &mdash;
            but you can also continue as a guest.
          </p>
        </div>
      </div>
      <div className="mt-4 flex justify-end">
        <Button type="button" onClick={onClick}>
          Register
        </Button>
      </div>
    </div>
  );
}
