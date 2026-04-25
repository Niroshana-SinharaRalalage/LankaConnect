/**
 * Fix 3a — WhatsAppOptIn auto-requests verification code on successful enable.
 *
 * Closes the silent-drop-off gap where users toggled WhatsApp on but never
 * clicked "Send Verification Code" and therefore never received any messages
 * (UserWhatsAppPreferences.EvaluateSkipReason returned PhoneUnverified forever).
 *
 * Contract:
 *   - When enable succeeds, requestVerification is called immediately.
 *   - When enable fails, requestVerification is NOT called.
 *   - When auto-request fails (e.g. rate-limited), user still sees the
 *     manual "Send Verification Code" button so they can retry.
 *   - Existing manual resend still works when already in the verified-pending state.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { WhatsAppOptIn } from '@/presentation/components/features/whatsapp/WhatsAppOptIn';
import * as useWhatsAppModule from '@/presentation/hooks/useWhatsApp';

vi.mock('@/presentation/hooks/useWhatsApp');
vi.mock('react-hot-toast', () => ({
  default: { success: vi.fn(), error: vi.fn() },
}));

describe('WhatsAppOptIn — Fix 3a auto-request on enable', () => {
  let enableMutate: ReturnType<typeof vi.fn>;
  let requestVerificationMutate: ReturnType<typeof vi.fn>;
  let disableMutate: ReturnType<typeof vi.fn>;
  let verifyPhoneMutate: ReturnType<typeof vi.fn>;

  function mockHooks({
    whatsAppEnabled = false,
    isFullyVerified = false,
    isLocked = false,
    whatsAppPhoneNumber = null as string | null,
    enableSucceeds = true,
    requestVerificationSucceeds = true,
  } = {}) {
    enableMutate = vi.fn().mockImplementation(() =>
      enableSucceeds
        ? Promise.resolve({ userId: 'u1', phoneNumber: '+12345678901', isEnabled: true, phoneVerified: false })
        : Promise.reject(new Error('enable failed'))
    );
    requestVerificationMutate = vi.fn().mockImplementation(() =>
      requestVerificationSucceeds
        ? Promise.resolve(undefined)
        : Promise.reject(new Error('rate limited'))
    );
    disableMutate = vi.fn().mockResolvedValue(undefined);
    verifyPhoneMutate = vi.fn().mockResolvedValue({ phoneVerified: true, errorMessage: null });

    vi.spyOn(useWhatsAppModule, 'useWhatsAppPreferences').mockReturnValue({
      data: whatsAppEnabled
        ? {
            userId: 'u1',
            whatsAppEnabled,
            whatsAppPhoneNumber,
            phoneVerified: isFullyVerified,
            phoneVerifiedAt: null,
            isFullyVerified,
            isLocked,
            verificationLockedUntil: null,
            notifyEventRegistration: true,
            notifyEventReminder: true,
            notifyEventCancellation: true,
            notifyEventUpdate: true,
            notifySignupCommitment: true,
            notifyRefund: true,
            notifyNewsletter: true,
            notifyNewEvent: true,
            notifyPayment: true,
            preferredLanguage: 'en',
            quietHoursStart: null,
            quietHoursEnd: null,
            respectCulturalTiming: true,
          }
        : null,
      isLoading: false,
    } as unknown as ReturnType<typeof useWhatsAppModule.useWhatsAppPreferences>);

    vi.spyOn(useWhatsAppModule, 'useEnableWhatsApp').mockReturnValue({
      mutateAsync: enableMutate,
      isPending: false,
    } as unknown as ReturnType<typeof useWhatsAppModule.useEnableWhatsApp>);

    vi.spyOn(useWhatsAppModule, 'useRequestVerification').mockReturnValue({
      mutateAsync: requestVerificationMutate,
      isPending: false,
    } as unknown as ReturnType<typeof useWhatsAppModule.useRequestVerification>);

    vi.spyOn(useWhatsAppModule, 'useDisableWhatsApp').mockReturnValue({
      mutateAsync: disableMutate,
      isPending: false,
    } as unknown as ReturnType<typeof useWhatsAppModule.useDisableWhatsApp>);

    vi.spyOn(useWhatsAppModule, 'useVerifyPhone').mockReturnValue({
      mutateAsync: verifyPhoneMutate,
      isPending: false,
    } as unknown as ReturnType<typeof useWhatsAppModule.useVerifyPhone>);
  }

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('calls requestVerification immediately after enable succeeds', async () => {
    mockHooks({ whatsAppEnabled: false });
    render(<WhatsAppOptIn />);

    const phoneInput = screen.getByPlaceholderText(/\+1 \(234\)/);
    fireEvent.change(phoneInput, { target: { value: '+12345678901' } });
    const enableButton = screen.getByRole('button', { name: /enable whatsapp notifications/i });
    fireEvent.click(enableButton);

    await waitFor(() => expect(enableMutate).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(requestVerificationMutate).toHaveBeenCalledTimes(1));
    // Order: enable first, then request-verification.
    const enableCallOrder = enableMutate.mock.invocationCallOrder[0];
    const requestCallOrder = requestVerificationMutate.mock.invocationCallOrder[0];
    expect(enableCallOrder).toBeLessThan(requestCallOrder);
  });

  it('does NOT call requestVerification when enable fails', async () => {
    mockHooks({ whatsAppEnabled: false, enableSucceeds: false });
    render(<WhatsAppOptIn />);

    const phoneInput = screen.getByPlaceholderText(/\+1 \(234\)/);
    fireEvent.change(phoneInput, { target: { value: '+12345678901' } });
    fireEvent.click(screen.getByRole('button', { name: /enable whatsapp notifications/i }));

    await waitFor(() => expect(enableMutate).toHaveBeenCalledTimes(1));
    // Give any pending microtasks a chance to resolve
    await new Promise((r) => setTimeout(r, 0));
    expect(requestVerificationMutate).not.toHaveBeenCalled();
  });

  it('still shows manual "Send Verification Code" button in unverified state (regression guard)', () => {
    // Scenario: user was enabled by some other path (server-side, past session) but never got a code.
    // Component still renders the manual send button so they can recover.
    mockHooks({
      whatsAppEnabled: true,
      isFullyVerified: false,
      whatsAppPhoneNumber: '+12345678901',
    });
    render(<WhatsAppOptIn />);

    expect(screen.getByRole('button', { name: /send verification code/i })).toBeInTheDocument();
  });
});
