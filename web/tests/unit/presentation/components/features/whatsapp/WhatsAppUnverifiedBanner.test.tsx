/**
 * Fix 3b — WhatsAppUnverifiedBanner contract.
 *
 * Surfaces the silent-drop-off cohort (WhatsApp on, phone unverified) with a
 * persistent, high-visibility prompt on the profile page. One-click resend +
 * inline code entry so the user can recover without scrolling to the
 * existing WhatsAppOptIn card.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { WhatsAppUnverifiedBanner } from '@/presentation/components/features/whatsapp/WhatsAppUnverifiedBanner';
import * as useWhatsAppModule from '@/presentation/hooks/useWhatsApp';

vi.mock('@/presentation/hooks/useWhatsApp');
vi.mock('react-hot-toast', () => ({
  default: { success: vi.fn(), error: vi.fn() },
}));

function prefs(overrides: Partial<NonNullable<ReturnType<typeof useWhatsAppModule.useWhatsAppPreferences>['data']>>) {
  return {
    userId: 'u1',
    whatsAppEnabled: true,
    whatsAppPhoneNumber: '+12345678901',
    phoneVerified: false,
    phoneVerifiedAt: null,
    isFullyVerified: false,
    isLocked: false,
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
    ...overrides,
  };
}

describe('WhatsAppUnverifiedBanner', () => {
  let requestVerificationMutate: ReturnType<typeof vi.fn>;
  let verifyPhoneMutate: ReturnType<typeof vi.fn>;

  function mockHooks(prefsData: ReturnType<typeof prefs> | null) {
    requestVerificationMutate = vi.fn().mockResolvedValue(undefined);
    verifyPhoneMutate = vi.fn().mockResolvedValue({ phoneVerified: true, errorMessage: null });

    vi.spyOn(useWhatsAppModule, 'useWhatsAppPreferences').mockReturnValue({
      data: prefsData,
      isLoading: false,
    } as unknown as ReturnType<typeof useWhatsAppModule.useWhatsAppPreferences>);

    vi.spyOn(useWhatsAppModule, 'useRequestVerification').mockReturnValue({
      mutateAsync: requestVerificationMutate,
      isPending: false,
    } as unknown as ReturnType<typeof useWhatsAppModule.useRequestVerification>);

    vi.spyOn(useWhatsAppModule, 'useVerifyPhone').mockReturnValue({
      mutateAsync: verifyPhoneMutate,
      isPending: false,
    } as unknown as ReturnType<typeof useWhatsAppModule.useVerifyPhone>);
  }

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('visibility', () => {
    it('renders nothing when preferences are null (user never opted in)', () => {
      mockHooks(null);
      const { container } = render(<WhatsAppUnverifiedBanner />);
      expect(container.firstChild).toBeNull();
    });

    it('renders nothing when WhatsApp is disabled', () => {
      mockHooks(prefs({ whatsAppEnabled: false }));
      const { container } = render(<WhatsAppUnverifiedBanner />);
      expect(container.firstChild).toBeNull();
    });

    it('renders nothing when phone is already verified (happy path — nothing to nag about)', () => {
      mockHooks(prefs({ whatsAppEnabled: true, phoneVerified: true, isFullyVerified: true }));
      const { container } = render(<WhatsAppUnverifiedBanner />);
      expect(container.firstChild).toBeNull();
    });

    it('renders when WhatsApp is enabled but phone is unverified', () => {
      mockHooks(prefs({ whatsAppEnabled: true, phoneVerified: false }));
      render(<WhatsAppUnverifiedBanner />);
      expect(screen.getByText(/verify your whatsapp number/i)).toBeInTheDocument();
    });
  });

  describe('content', () => {
    it('masks the phone number keeping only the last 4 digits', () => {
      mockHooks(prefs({ whatsAppPhoneNumber: '+12345678901' }));
      render(<WhatsAppUnverifiedBanner />);
      // +12345678901 → last 4 = 8901, earlier digits masked
      expect(screen.getByText(/8901/)).toBeInTheDocument();
      // Full unmasked number must NOT appear
      expect(screen.queryByText(/12345678901/)).not.toBeInTheDocument();
    });

    it('falls back to generic "your phone" when phone number is null', () => {
      mockHooks(prefs({ whatsAppPhoneNumber: null }));
      render(<WhatsAppUnverifiedBanner />);
      expect(screen.getByText(/your phone/i)).toBeInTheDocument();
    });
  });

  describe('interactions', () => {
    it('calls requestVerification when the resend button is clicked', async () => {
      mockHooks(prefs({}));
      render(<WhatsAppUnverifiedBanner />);
      fireEvent.click(screen.getByRole('button', { name: /resend code/i }));
      await waitFor(() => expect(requestVerificationMutate).toHaveBeenCalledTimes(1));
    });

    it('calls verifyPhone with the 6-digit code when Verify is clicked', async () => {
      mockHooks(prefs({}));
      render(<WhatsAppUnverifiedBanner />);
      const input = screen.getByLabelText(/verification code/i);
      fireEvent.change(input, { target: { value: '123456' } });
      fireEvent.click(screen.getByRole('button', { name: /^verify$/i }));
      await waitFor(() =>
        expect(verifyPhoneMutate).toHaveBeenCalledWith({ code: '123456' }),
      );
    });

    it('does not call verifyPhone when code is shorter than 6 digits', async () => {
      mockHooks(prefs({}));
      render(<WhatsAppUnverifiedBanner />);
      const input = screen.getByLabelText(/verification code/i);
      fireEvent.change(input, { target: { value: '123' } });
      fireEvent.click(screen.getByRole('button', { name: /^verify$/i }));
      await new Promise((r) => setTimeout(r, 0));
      expect(verifyPhoneMutate).not.toHaveBeenCalled();
    });
  });

  describe('rate-limit lockout', () => {
    it('disables resend + verify and shows the unlock time when locked', () => {
      const until = new Date(Date.now() + 60 * 60 * 1000).toISOString();
      mockHooks(prefs({ isLocked: true, verificationLockedUntil: until }));
      render(<WhatsAppUnverifiedBanner />);
      expect(screen.getByText(/too many attempts/i)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /resend code/i })).toBeDisabled();
      expect(screen.getByRole('button', { name: /^verify$/i })).toBeDisabled();
    });
  });
});
