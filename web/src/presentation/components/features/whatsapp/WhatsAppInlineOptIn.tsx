'use client';

import { useState } from 'react';
import { MessageCircle } from 'lucide-react';
import { PhoneInput } from '@/presentation/components/ui/PhoneInput';
import { toE164 } from '@/presentation/lib/validators/whatsapp.schemas';

/**
 * WhatsAppInlineOptIn Component
 * Phase 7A.6A: Lightweight inline WhatsApp opt-in for embedding in forms
 *
 * Unlike the full WhatsAppOptIn (3-state card widget on Profile page),
 * this is a simple checkbox + phone input that exposes values via callbacks.
 * No API calls — the parent form handles submission.
 *
 * Used in: RegisterForm, EventRegistrationForm, Footer newsletter
 */

interface WhatsAppInlineOptInProps {
  /** Whether the user has opted in */
  enabled: boolean;
  /** Callback when opt-in state changes */
  onEnabledChange: (enabled: boolean) => void;
  /** The phone number value (raw, not E.164) */
  phoneNumber: string;
  /** Callback when phone number changes */
  onPhoneNumberChange: (phoneNumber: string) => void;
  /** Validation error message for phone number */
  phoneError?: string;
  /** Whether the component is disabled (e.g., during form submission) */
  disabled?: boolean;
  /** Optional description override */
  description?: string;
}

export function WhatsAppInlineOptIn({
  enabled,
  onEnabledChange,
  phoneNumber,
  onPhoneNumberChange,
  phoneError,
  disabled = false,
  description = 'Receive event updates, registration confirmations, and reminders via WhatsApp.',
}: WhatsAppInlineOptInProps) {
  return (
    <div className="space-y-3">
      {/* Opt-in checkbox */}
      <div className="flex items-start space-x-3 p-3 border border-gray-200 rounded-lg hover:border-[#25D366] transition-colors">
        <input
          id="whatsapp-optin"
          type="checkbox"
          checked={enabled}
          onChange={(e) => {
            onEnabledChange(e.target.checked);
            if (!e.target.checked) {
              onPhoneNumberChange('');
            }
          }}
          disabled={disabled}
          className="mt-1 h-4 w-4 rounded border-gray-300 text-[#25D366] focus:ring-[#25D366]"
          aria-describedby="whatsapp-optin-description"
        />
        <div className="flex-1">
          <label
            htmlFor="whatsapp-optin"
            className="text-sm font-medium text-gray-900 cursor-pointer flex items-center gap-2"
          >
            <MessageCircle className="w-4 h-4 text-[#25D366]" />
            Enable WhatsApp Notifications
          </label>
          <p id="whatsapp-optin-description" className="text-xs text-gray-500 mt-0.5">
            {description}
          </p>
        </div>
      </div>

      {/* Phone number input — shown only when opted in */}
      {enabled && (
        <div className="space-y-1.5 pl-7">
          <label htmlFor="whatsapp-phone" className="text-sm font-medium text-gray-700">
            WhatsApp Phone Number
          </label>
          <PhoneInput
            id="whatsapp-phone"
            value={phoneNumber}
            onChange={onPhoneNumberChange}
            placeholder="+1 (234) 567-8901"
            error={!!phoneError}
            disabled={disabled}
            aria-describedby={phoneError ? 'whatsapp-phone-error' : 'whatsapp-phone-hint'}
          />
          {phoneError ? (
            <p id="whatsapp-phone-error" className="text-xs text-destructive">
              {phoneError}
            </p>
          ) : (
            <p id="whatsapp-phone-hint" className="text-xs text-gray-500">
              Enter in international format (e.g., +12345678901). You can verify later from your profile.
            </p>
          )}
        </div>
      )}
    </div>
  );
}
