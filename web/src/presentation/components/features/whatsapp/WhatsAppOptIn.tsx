'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { MessageCircle, Phone, CheckCircle, Shield, Loader2 } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { PhoneInput } from '@/presentation/components/ui/PhoneInput';
import {
  useWhatsAppPreferences,
  useEnableWhatsApp,
  useDisableWhatsApp,
  useRequestVerification,
  useVerifyPhone,
} from '@/presentation/hooks/useWhatsApp';
import {
  enableWhatsAppSchema,
  verifyPhoneSchema,
  toE164,
  type EnableWhatsAppFormData,
  type VerifyPhoneFormData,
} from '@/presentation/lib/validators/whatsapp.schemas';

/**
 * WhatsAppOptIn Component
 * Phase 7A.4: 3-state WhatsApp opt-in widget
 *
 * States:
 * 1. Disabled — Show enable form with phone input
 * 2. Enabled (unverified) — Show verification code input + resend
 * 3. Verified — Show green checkmark + disable option
 */
export function WhatsAppOptIn() {
  const { data: preferences, isLoading } = useWhatsAppPreferences();
  const enableMutation = useEnableWhatsApp();
  const disableMutation = useDisableWhatsApp();
  const requestVerificationMutation = useRequestVerification();
  const verifyPhoneMutation = useVerifyPhone();
  const [showDisableConfirm, setShowDisableConfirm] = useState(false);
  // Phase 7A Phase 1: Track whether verification code was actually requested
  const [codeSent, setCodeSent] = useState(false);

  // Enable form
  const enableForm = useForm<EnableWhatsAppFormData>({
    resolver: zodResolver(enableWhatsAppSchema),
    defaultValues: { phoneNumber: '' },
  });

  // Verify form
  const verifyForm = useForm<VerifyPhoneFormData>({
    resolver: zodResolver(verifyPhoneSchema),
    defaultValues: { code: '' },
  });

  const handleEnable = async (data: EnableWhatsAppFormData) => {
    const e164Phone = toE164(data.phoneNumber);
    await enableMutation.mutateAsync({ phoneNumber: e164Phone });
    enableForm.reset();
    setCodeSent(false); // Reset code sent state for new enable
  };

  const handleVerify = async (data: VerifyPhoneFormData) => {
    await verifyPhoneMutation.mutateAsync({ code: data.code });
    verifyForm.reset();
  };

  const handleSendCode = async () => {
    await requestVerificationMutation.mutateAsync();
    setCodeSent(true);
  };

  const handleDisable = async () => {
    await disableMutation.mutateAsync();
    setShowDisableConfirm(false);
  };

  if (isLoading) {
    return (
      <Card>
        <CardContent className="py-8">
          <div className="flex items-center justify-center gap-3">
            <Loader2 className="h-6 w-6 animate-spin" style={{ color: '#25D366' }} />
            <span className="text-gray-500">Loading WhatsApp preferences...</span>
          </div>
        </CardContent>
      </Card>
    );
  }

  const isEnabled = preferences?.whatsAppEnabled ?? false;
  const isVerified = preferences?.isFullyVerified ?? false;
  const isLocked = preferences?.isLocked ?? false;

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-3">
          <div
            className="p-2 rounded-lg"
            style={{ backgroundColor: '#25D36610' }}
          >
            <MessageCircle className="h-5 w-5" style={{ color: '#25D366' }} />
          </div>
          <div>
            <CardTitle className="text-lg">WhatsApp Notifications</CardTitle>
            <CardDescription>
              Receive event updates and reminders via WhatsApp
            </CardDescription>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        {/* State 1: Disabled — Show enable form */}
        {!isEnabled && (
          <form onSubmit={enableForm.handleSubmit(handleEnable)} className="space-y-4">
            <p className="text-sm text-gray-600">
              Enable WhatsApp notifications to receive instant event updates, registration confirmations,
              and reminders directly on your phone.
            </p>
            <div className="space-y-2">
              <label htmlFor="wa-phone" className="text-sm font-medium text-gray-700">
                WhatsApp Phone Number
              </label>
              <PhoneInput
                id="wa-phone"
                value={enableForm.watch('phoneNumber')}
                onChange={(value) => enableForm.setValue('phoneNumber', value, { shouldValidate: true })}
                placeholder="+1 (234) 567-8901"
                error={!!enableForm.formState.errors.phoneNumber}
              />
              {enableForm.formState.errors.phoneNumber && (
                <p className="text-sm text-red-600">
                  {enableForm.formState.errors.phoneNumber.message}
                </p>
              )}
              <p className="text-xs text-gray-500">
                Enter your number in international format (e.g., +12345678901)
              </p>
            </div>
            <Button
              type="submit"
              disabled={enableMutation.isPending}
              className="w-full"
              style={{ backgroundColor: '#25D366', color: 'white' }}
            >
              {enableMutation.isPending ? (
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              ) : (
                <MessageCircle className="h-4 w-4 mr-2" />
              )}
              Enable WhatsApp Notifications
            </Button>
          </form>
        )}

        {/* State 2: Enabled but not verified — Show verification */}
        {isEnabled && !isVerified && (
          <div className="space-y-4">
            <div className="flex items-center gap-2 p-3 rounded-lg" style={{ backgroundColor: '#FFF7ED' }}>
              <Phone className="h-5 w-5" style={{ color: '#FF7900' }} />
              <div>
                <p className="text-sm font-medium" style={{ color: '#FF7900' }}>
                  Phone verification required
                </p>
                <p className="text-xs text-gray-600">
                  {codeSent
                    ? `A verification code has been sent to ${preferences?.whatsAppPhoneNumber || 'your phone'}`
                    : 'Please send a verification code to verify your phone number'}
                </p>
              </div>
            </div>

            {isLocked ? (
              <div className="p-3 rounded-lg bg-red-50">
                <p className="text-sm text-red-700">
                  Too many verification attempts. Please try again after{' '}
                  {preferences?.verificationLockedUntil
                    ? new Date(preferences.verificationLockedUntil).toLocaleTimeString()
                    : '1 hour'}
                  .
                </p>
              </div>
            ) : !codeSent ? (
              /* Phase 7A Phase 1: Explicit "Send Verification Code" button before showing code input */
              <div className="space-y-3">
                <p className="text-sm text-gray-600">
                  Your WhatsApp number is <strong>{preferences?.whatsAppPhoneNumber}</strong>.
                  Click below to receive a verification code via SMS.
                </p>
                <Button
                  type="button"
                  onClick={handleSendCode}
                  disabled={requestVerificationMutation.isPending}
                  className="w-full"
                  style={{ backgroundColor: '#25D366', color: 'white' }}
                >
                  {requestVerificationMutation.isPending ? (
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  ) : (
                    <Phone className="h-4 w-4 mr-2" />
                  )}
                  Send Verification Code
                </Button>
              </div>
            ) : (
              <form onSubmit={verifyForm.handleSubmit(handleVerify)} className="space-y-3">
                <div className="space-y-2">
                  <label htmlFor="wa-code" className="text-sm font-medium text-gray-700">
                    Verification Code
                  </label>
                  <Input
                    id="wa-code"
                    {...verifyForm.register('code')}
                    placeholder="123456"
                    maxLength={6}
                    inputMode="numeric"
                    autoComplete="one-time-code"
                    className={verifyForm.formState.errors.code ? 'border-red-500' : ''}
                  />
                  {verifyForm.formState.errors.code && (
                    <p className="text-sm text-red-600">
                      {verifyForm.formState.errors.code.message}
                    </p>
                  )}
                </div>
                <div className="flex gap-2">
                  <Button
                    type="submit"
                    disabled={verifyPhoneMutation.isPending}
                    className="flex-1"
                    style={{ backgroundColor: '#25D366', color: 'white' }}
                  >
                    {verifyPhoneMutation.isPending ? (
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    ) : (
                      <Shield className="h-4 w-4 mr-2" />
                    )}
                    Verify
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={handleSendCode}
                    disabled={requestVerificationMutation.isPending}
                  >
                    {requestVerificationMutation.isPending ? (
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    ) : null}
                    Resend Code
                  </Button>
                </div>
              </form>
            )}

            <Button
              variant="outline"
              size="sm"
              onClick={() => disableMutation.mutateAsync()}
              disabled={disableMutation.isPending}
              className="text-gray-500"
            >
              Cancel and disable WhatsApp
            </Button>
          </div>
        )}

        {/* State 3: Verified — Show success + disable option */}
        {isEnabled && isVerified && (
          <div className="space-y-4">
            <div className="flex items-center gap-2 p-3 rounded-lg" style={{ backgroundColor: '#F0FFF4' }}>
              <CheckCircle className="h-5 w-5" style={{ color: '#25D366' }} />
              <div>
                <p className="text-sm font-medium" style={{ color: '#166534' }}>
                  WhatsApp notifications active
                </p>
                <p className="text-xs text-gray-600">
                  {preferences?.whatsAppPhoneNumber || 'Phone verified'}
                  {preferences?.phoneVerifiedAt && (
                    <> &middot; Verified {new Date(preferences.phoneVerifiedAt).toLocaleDateString()}</>
                  )}
                </p>
              </div>
            </div>

            {showDisableConfirm ? (
              <div className="space-y-2">
                <p className="text-sm text-gray-600">
                  Are you sure you want to disable WhatsApp notifications?
                </p>
                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={handleDisable}
                    disabled={disableMutation.isPending}
                    className="text-red-600 border-red-300 hover:bg-red-50"
                  >
                    {disableMutation.isPending && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                    Yes, disable
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setShowDisableConfirm(false)}
                  >
                    Cancel
                  </Button>
                </div>
              </div>
            ) : (
              <Button
                variant="outline"
                size="sm"
                onClick={() => setShowDisableConfirm(true)}
                className="text-gray-500"
              >
                Disable WhatsApp notifications
              </Button>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
