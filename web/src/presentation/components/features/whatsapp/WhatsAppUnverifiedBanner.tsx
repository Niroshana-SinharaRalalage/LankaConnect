'use client';

import { useState } from 'react';
import { AlertTriangle, Loader2, Shield, Send } from 'lucide-react';
import { Input } from '@/presentation/components/ui/Input';
import { Button } from '@/presentation/components/ui/Button';
import {
  useWhatsAppPreferences,
  useRequestVerification,
  useVerifyPhone,
} from '@/presentation/hooks/useWhatsApp';

/**
 * Fix 3b — WhatsAppUnverifiedBanner.
 *
 * Persistent amber banner shown on /profile when WhatsApp is enabled but the
 * phone is not yet verified. Provides one-click resend + inline code entry so
 * the user can recover from the silent-drop-off state without scrolling to the
 * WhatsAppOptIn card further down the page.
 *
 * Self-hiding: renders null when preferences are null, WhatsApp is disabled,
 * or the phone is already verified — so the component is safe to drop in at
 * the top of any page regardless of user state.
 *
 * Rate-limit handling: when `isLocked` is true, the resend + verify controls
 * are disabled and the unlock time is surfaced inline.
 */

/** Masks a phone number keeping only the last 4 digits (`+12345678901` → `•••••••8901`). */
function maskPhone(phone: string | null | undefined): string {
  if (!phone) return 'your phone';
  const digitsOnly = phone.replace(/\D/g, '');
  if (digitsOnly.length <= 4) return phone;
  const last4 = digitsOnly.slice(-4);
  return `•••••••${last4}`;
}

export function WhatsAppUnverifiedBanner() {
  const { data: preferences } = useWhatsAppPreferences();
  const requestVerificationMutation = useRequestVerification();
  const verifyPhoneMutation = useVerifyPhone();
  const [code, setCode] = useState('');

  // Guard clauses (in this order): only render for the silent-drop-off cohort.
  if (!preferences) return null;
  if (!preferences.whatsAppEnabled) return null;
  if (preferences.phoneVerified) return null;

  const isLocked = preferences.isLocked === true;
  const unlockAt = preferences.verificationLockedUntil
    ? new Date(preferences.verificationLockedUntil).toLocaleTimeString()
    : null;

  const handleResend = async () => {
    if (isLocked) return;
    try {
      await requestVerificationMutation.mutateAsync();
    } catch {
      // Hook already surfaces the toast; swallow so the banner stays mounted.
    }
  };

  const handleVerify = async () => {
    // Guard: silently ignore shorter codes — the backend would reject anyway,
    // and we want the button enabled-but-no-op rather than a flicker.
    if (code.trim().length !== 6 || isLocked) return;
    try {
      await verifyPhoneMutation.mutateAsync({ code: code.trim() });
      setCode('');
    } catch {
      // Hook already surfaces the toast; leave the code so the user can edit.
    }
  };

  return (
    <div
      role="alert"
      aria-live="polite"
      className="rounded-lg border border-amber-300 bg-amber-50 p-4 shadow-sm"
    >
      <div className="flex items-start gap-3">
        <div className="flex-shrink-0">
          <AlertTriangle className="h-5 w-5 text-amber-600" aria-hidden="true" />
        </div>
        <div className="flex-1 space-y-3">
          <div>
            <h3 className="text-sm font-semibold text-amber-900">
              Verify your WhatsApp number
            </h3>
            <p className="mt-1 text-sm text-amber-800">
              {isLocked ? (
                <>
                  Too many attempts.{' '}
                  {unlockAt
                    ? <>Try again after <strong>{unlockAt}</strong>.</>
                    : 'Please try again in about an hour.'}
                </>
              ) : (
                <>
                  We sent a code to <strong>{maskPhone(preferences.whatsAppPhoneNumber)}</strong>.
                  Enter it below so you can start receiving WhatsApp notifications.
                </>
              )}
            </p>
          </div>

          <div className="flex flex-col gap-2 sm:flex-row sm:items-start">
            <div className="flex-1">
              <label htmlFor="wa-banner-code" className="sr-only">
                Verification code
              </label>
              <Input
                id="wa-banner-code"
                value={code}
                onChange={(e) => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                placeholder="6-digit code"
                maxLength={6}
                inputMode="numeric"
                autoComplete="one-time-code"
                disabled={isLocked}
                aria-label="Verification code"
              />
            </div>
            <Button
              type="button"
              onClick={handleVerify}
              disabled={isLocked || verifyPhoneMutation.isPending || code.trim().length !== 6}
              style={{ backgroundColor: '#25D366', color: 'white' }}
              className="sm:w-auto"
            >
              {verifyPhoneMutation.isPending ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <Shield className="mr-2 h-4 w-4" />
              )}
              Verify
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={handleResend}
              disabled={isLocked || requestVerificationMutation.isPending}
              className="sm:w-auto border-amber-400 text-amber-900 hover:bg-amber-100"
            >
              {requestVerificationMutation.isPending ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <Send className="mr-2 h-4 w-4" />
              )}
              Resend code
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
