'use client';

import * as React from 'react';
import { useRouter } from 'next/navigation';
import { Calendar, RefreshCcw, Package, ListChecks, ShieldCheck, X } from 'lucide-react';
import { Dialog, DialogContent } from '@/presentation/components/ui/Dialog';
import { Button } from '@/presentation/components/ui/Button';
import { cn } from '@/presentation/lib/utils';

/**
 * Phase 6A.144 — Auth Encouragement Modal
 *
 * Soft nudge shown when an unauthenticated user clicks Register on a paid event
 * (or other surfaces where signing in unlocks management features). Offers three
 * explicit exits: Sign In, Sign Up, or Continue as Guest. Closing the modal
 * (ESC / backdrop / X) does NOT count as choosing guest mode — the user must
 * make a deliberate choice.
 *
 * Generic by design: the `context` prop selects the default copy variant, and
 * `benefits` allows full override. Reusable for add-on, donation, and refund
 * surfaces in later phases.
 *
 * Accessibility: implements role=dialog, aria-modal, aria-labelledby/-describedby,
 * a ref-based focus trap, and focus restoration on close. Built on the shared
 * `Dialog` primitive but adds these locally (the shared Dialog's JSDoc claims
 * a focus trap but only implements ESC + backdrop — flagged in 6A.144 RCA;
 * a global Dialog refactor was deliberately kept out of scope).
 */

export type AuthNudgeContext = 'event-paid' | 'addon' | 'donation' | 'refund';

export interface AuthEncouragementModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  context: AuthNudgeContext;
  /** Absolute in-app path (with any query string) to return to after Sign In / Sign Up. */
  redirectTo: string;
  onContinueAsGuest: () => void;
  /** Override the default copy for the given context. */
  benefits?: string[];
  title?: string;
  description?: string;
}

const DEFAULT_COPY: Record<
  AuthNudgeContext,
  { title: string; description: string; benefits: string[] }
> = {
  'event-paid': {
    title: 'Sign in to get the most out of this event',
    description:
      'You can register as a guest, but signing in unlocks the tools that make paid events much easier to manage.',
    benefits: [
      'Manage your tickets and view them anytime',
      'Request refunds and track payment history',
      'Add and update event add-ons after purchase',
      'See all your sign-ups and registrations in one place',
    ],
  },
  addon: {
    title: 'Sign in to keep track of your add-ons',
    description: 'Add-ons are easier to manage when they are attached to your account.',
    benefits: [
      'Edit or remove add-ons after purchase',
      'View receipts in your purchase history',
      'Request refunds without re-entering your details',
    ],
  },
  donation: {
    title: 'Sign in to save your giving history',
    description: 'Signing in lets you keep a personal record of your support.',
    benefits: [
      'View past donations in one place',
      'Get receipts emailed to your account',
      'Recurring donations require a member account',
    ],
  },
  refund: {
    title: 'Sign in to request a refund',
    description: 'Refunds are processed against your member account.',
    benefits: [
      'Track refund status from your dashboard',
      'Avoid re-entering payment details',
      'Get refund confirmations on your account',
    ],
  },
};

const BENEFIT_ICONS = [Calendar, RefreshCcw, Package, ListChecks];

const FOCUSABLE_SELECTOR =
  '[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

export function AuthEncouragementModal({
  open,
  onOpenChange,
  context,
  redirectTo,
  onContinueAsGuest,
  benefits,
  title,
  description,
}: AuthEncouragementModalProps) {
  const router = useRouter();
  const reactId = React.useId();
  const titleId = `${reactId}-title`;
  const descId = `${reactId}-desc`;
  const dialogRef = React.useRef<HTMLDivElement>(null);
  const titleRef = React.useRef<HTMLHeadingElement>(null);
  const previouslyFocusedRef = React.useRef<HTMLElement | null>(null);

  const copy = DEFAULT_COPY[context] ?? DEFAULT_COPY['event-paid'];
  const finalTitle = title ?? copy.title;
  const finalDescription = description ?? copy.description;
  const finalBenefits = benefits ?? copy.benefits;

  // Capture the trigger element on open so we can restore focus on close.
  // Move focus to the title once mounted so screen readers announce the dialog.
  // Cleanup runs when `open` flips back to false (or component unmounts), at
  // which point we restore focus to the originally-focused element. Effects
  // run synchronously after commit, so titleRef is already populated.
  React.useEffect(() => {
    if (!open) return;
    previouslyFocusedRef.current =
      (document.activeElement as HTMLElement | null) ?? null;
    titleRef.current?.focus();
    return () => {
      const target = previouslyFocusedRef.current;
      previouslyFocusedRef.current = null;
      if (!target) return;
      try {
        target.focus();
      } catch (err) {
        // Element may have been unmounted; safe to ignore.
        console.debug('[AuthEncouragementModal] focus restore skipped', err);
      }
    };
  }, [open]);

  // Focus trap: cycle Tab / Shift+Tab within the dialog content.
  React.useEffect(() => {
    if (!open) return;
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key !== 'Tab' || !dialogRef.current) return;
      const focusables = Array.from(
        dialogRef.current.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR),
      ).filter((el) => !el.hasAttribute('disabled'));
      if (focusables.length === 0) {
        event.preventDefault();
        titleRef.current?.focus();
        return;
      }
      const first = focusables[0];
      const last = focusables[focusables.length - 1];
      const active = document.activeElement as HTMLElement | null;

      // If focus is on the title (or outside the focusable list), redirect to a boundary.
      if (!active || !focusables.includes(active)) {
        event.preventDefault();
        (event.shiftKey ? last : first).focus();
        return;
      }
      if (event.shiftKey && active === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && active === last) {
        event.preventDefault();
        first.focus();
      }
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [open]);

  const handleSignIn = React.useCallback(() => {
    try {
      console.debug('[AuthEncouragementModal] sign-in', { context, redirectTo });
      router.push(`/login?redirect=${encodeURIComponent(redirectTo)}`);
    } catch (err) {
      console.error('[AuthEncouragementModal] navigation failed (sign-in)', err);
    }
  }, [router, redirectTo, context]);

  const handleSignUp = React.useCallback(() => {
    try {
      console.debug('[AuthEncouragementModal] sign-up', { context, redirectTo });
      router.push(`/register?redirect=${encodeURIComponent(redirectTo)}`);
    } catch (err) {
      console.error('[AuthEncouragementModal] navigation failed (sign-up)', err);
    }
  }, [router, redirectTo, context]);

  const handleGuest = React.useCallback(() => {
    try {
      console.debug('[AuthEncouragementModal] continue-as-guest', { context });
      onContinueAsGuest();
    } catch (err) {
      console.error('[AuthEncouragementModal] guest callback failed', err);
    }
  }, [onContinueAsGuest, context]);

  if (!open) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={descId}
        className="max-w-lg sm:max-w-xl"
      >
        {/* Close (X) — labelled for screen readers and matches existing modal pattern. */}
        <button
          type="button"
          aria-label="Close"
          onClick={() => onOpenChange(false)}
          className={cn(
            'absolute right-4 top-4 rounded-md p-1 text-neutral-500 transition',
            'hover:bg-neutral-100 hover:text-neutral-900',
            'dark:hover:bg-neutral-800 dark:hover:text-neutral-100',
            'focus:outline-none focus:ring-2 focus:ring-orange-500 focus:ring-offset-2',
          )}
        >
          <X className="h-5 w-5" aria-hidden="true" />
        </button>

        <div className="flex flex-col">
          <div className="flex items-start gap-3 pr-8">
            <ShieldCheck
              className="h-6 w-6 flex-shrink-0 text-orange-600"
              aria-hidden="true"
            />
            <h2
              id={titleId}
              ref={titleRef}
              tabIndex={-1}
              className="text-lg font-semibold text-neutral-900 outline-none dark:text-neutral-100"
            >
              {finalTitle}
            </h2>
          </div>

          <p
            id={descId}
            className="mt-2 text-sm text-neutral-600 dark:text-neutral-400"
          >
            {finalDescription}
          </p>

          <ul className="mt-4 space-y-2">
            {finalBenefits.map((benefit, idx) => {
              const Icon = BENEFIT_ICONS[idx % BENEFIT_ICONS.length];
              return (
                <li
                  key={benefit}
                  className="flex items-start gap-2 text-sm text-neutral-700 dark:text-neutral-300"
                >
                  <Icon
                    className="mt-0.5 h-4 w-4 flex-shrink-0 text-emerald-600"
                    aria-hidden="true"
                  />
                  <span>{benefit}</span>
                </li>
              );
            })}
          </ul>

          {context === 'event-paid' && (
            <p className="mt-4 rounded-md bg-amber-50 p-3 text-xs text-amber-900 dark:bg-amber-900/20 dark:text-amber-200">
              Already registered with us under this email? Sign in to keep
              everything linked.
            </p>
          )}

          {/* Buttons: stack on mobile, row on sm+. Sign In is primary. */}
          <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:items-center sm:justify-end sm:gap-3">
            <Button
              type="button"
              variant="ghost"
              onClick={handleGuest}
              className="sm:mr-auto"
            >
              Continue as Guest
            </Button>
            <Button type="button" variant="outline" onClick={handleSignUp}>
              Sign Up
            </Button>
            <Button type="button" onClick={handleSignIn}>
              Sign In
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
