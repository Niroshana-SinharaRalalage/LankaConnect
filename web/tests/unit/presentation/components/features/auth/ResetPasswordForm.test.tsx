import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { ResetPasswordForm } from '@/presentation/components/features/auth/ResetPasswordForm';
import { authRepository } from '@/infrastructure/api/repositories/auth.repository';
import { ApiError } from '@/infrastructure/api/client/api-errors';

// Mock next/navigation
const mockPush = vi.fn();
vi.mock('next/navigation', () => ({
  useRouter: () => ({
    push: mockPush,
  }),
}));

// Mock the auth repository
vi.mock('@/infrastructure/api/repositories/auth.repository', () => ({
  authRepository: {
    resetPassword: vi.fn(),
  },
}));

describe('ResetPasswordForm Component', () => {
  const validToken = 'valid-reset-token-123';

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe('rendering', () => {
    it('should render the reset password form', () => {
      render(<ResetPasswordForm token={validToken} />);

      expect(screen.getByRole('heading', { name: /reset password/i })).toBeInTheDocument();
      expect(screen.getByText(/enter your new password below/i)).toBeInTheDocument();
    });

    it('should render password input field', () => {
      render(<ResetPasswordForm token={validToken} />);

      const passwordInput = screen.getByLabelText(/^new password$/i);
      expect(passwordInput).toBeInTheDocument();
      expect(passwordInput).toHaveAttribute('type', 'password');
    });

    it('should render confirm password input field', () => {
      render(<ResetPasswordForm token={validToken} />);

      const confirmPasswordInput = screen.getByLabelText(/confirm new password/i);
      expect(confirmPasswordInput).toBeInTheDocument();
      expect(confirmPasswordInput).toHaveAttribute('type', 'password');
    });

    it('should render submit button', () => {
      render(<ResetPasswordForm token={validToken} />);

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      expect(submitButton).toBeInTheDocument();
    });

    it('should render password requirements list', () => {
      render(<ResetPasswordForm token={validToken} />);

      expect(screen.getByText(/password must contain:/i)).toBeInTheDocument();
      expect(screen.getByText(/at least 8 characters/i)).toBeInTheDocument();
      expect(screen.getByText(/one uppercase letter/i)).toBeInTheDocument();
      expect(screen.getByText(/one lowercase letter/i)).toBeInTheDocument();
      expect(screen.getByText(/one number/i)).toBeInTheDocument();
      expect(screen.getByText(/one special character/i)).toBeInTheDocument();
    });
  });

  describe('validation', () => {
    it('should show error when password is empty', async () => {
      render(<ResetPasswordForm token={validToken} />);

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/password must be at least 8 characters/i)).toBeInTheDocument();
      });
    });

    it('should show error when password is too short', async () => {
      render(<ResetPasswordForm token={validToken} />);

      const passwordInput = screen.getByLabelText(/^new password$/i);
      fireEvent.change(passwordInput, { target: { value: 'Short1!' } });

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/password must be at least 8 characters/i)).toBeInTheDocument();
      });
    });

    it('should show error when password lacks uppercase letter', async () => {
      render(<ResetPasswordForm token={validToken} />);

      const passwordInput = screen.getByLabelText(/^new password$/i);
      fireEvent.change(passwordInput, { target: { value: 'lowercase123!' } });

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(
          screen.getByText(/password must contain at least one uppercase letter/i)
        ).toBeInTheDocument();
      });
    });

    it('should show error when password lacks lowercase letter', async () => {
      render(<ResetPasswordForm token={validToken} />);

      const passwordInput = screen.getByLabelText(/^new password$/i);
      fireEvent.change(passwordInput, { target: { value: 'UPPERCASE123!' } });

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(
          screen.getByText(/password must contain at least one lowercase letter/i)
        ).toBeInTheDocument();
      });
    });

    it('should show error when password lacks number', async () => {
      render(<ResetPasswordForm token={validToken} />);

      const passwordInput = screen.getByLabelText(/^new password$/i);
      fireEvent.change(passwordInput, { target: { value: 'NoNumbers!' } });

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(
          screen.getByText(/password must contain at least one number/i)
        ).toBeInTheDocument();
      });
    });

    it('should show error when password lacks special character', async () => {
      render(<ResetPasswordForm token={validToken} />);

      const passwordInput = screen.getByLabelText(/^new password$/i);
      fireEvent.change(passwordInput, { target: { value: 'NoSpecial123' } });

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(
          screen.getByText(/password must contain at least one special character/i)
        ).toBeInTheDocument();
      });
    });

    it('should show error when passwords do not match', async () => {
      render(<ResetPasswordForm token={validToken} />);

      const passwordInput = screen.getByLabelText(/^new password$/i);
      const confirmPasswordInput = screen.getByLabelText(/confirm new password/i);

      fireEvent.change(passwordInput, { target: { value: 'ValidPass123!' } });
      fireEvent.change(confirmPasswordInput, { target: { value: 'DifferentPass123!' } });

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/passwords do not match/i)).toBeInTheDocument();
      });
    });

    it('should show error when confirm password is empty', async () => {
      render(<ResetPasswordForm token={validToken} />);

      const passwordInput = screen.getByLabelText(/^new password$/i);
      fireEvent.change(passwordInput, { target: { value: 'ValidPass123!' } });

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/please confirm your password/i)).toBeInTheDocument();
      });
    });
  });

  describe('form submission', () => {
    it('should call resetPassword with correct token and password', async () => {
      const mockResponse = { message: 'Password reset successful' };
      vi.mocked(authRepository.resetPassword).mockResolvedValueOnce(mockResponse);

      render(<ResetPasswordForm token={validToken} />);

      const passwordInput = screen.getByLabelText(/^new password$/i);
      const confirmPasswordInput = screen.getByLabelText(/confirm new password/i);

      fireEvent.change(passwordInput, { target: { value: 'NewPassword123!' } });
      fireEvent.change(confirmPasswordInput, { target: { value: 'NewPassword123!' } });

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(authRepository.resetPassword).toHaveBeenCalledWith(validToken, 'NewPassword123!');
      });
    });

    it('should show success message after successful submission', async () => {
      const mockResponse = { message: 'Your password has been reset successfully!' };
      vi.mocked(authRepository.resetPassword).mockResolvedValueOnce(mockResponse);

      render(<ResetPasswordForm token={validToken} />);

      const passwordInput = screen.getByLabelText(/^new password$/i);
      const confirmPasswordInput = screen.getByLabelText(/confirm new password/i);

      fireEvent.change(passwordInput, { target: { value: 'NewPassword123!' } });
      fireEvent.change(confirmPasswordInput, { target: { value: 'NewPassword123!' } });

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/your password has been reset successfully!/i)).toBeInTheDocument();
        expect(screen.getByText(/redirecting to login/i)).toBeInTheDocument();
      });
    });

    it('should show generic success message when API returns no message', async () => {
      const mockResponse = { message: '' };
      vi.mocked(authRepository.resetPassword).mockResolvedValueOnce(mockResponse);

      render(<ResetPasswordForm token={validToken} />);

      const passwordInput = screen.getByLabelText(/^new password$/i);
      const confirmPasswordInput = screen.getByLabelText(/confirm new password/i);

      fireEvent.change(passwordInput, { target: { value: 'NewPassword123!' } });
      fireEvent.change(confirmPasswordInput, { target: { value: 'NewPassword123!' } });

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/your password has been reset successfully!/i)).toBeInTheDocument();
      });
    });

    it('should disable submit button during submission', async () => {
      vi.mocked(authRepository.resetPassword).mockImplementationOnce(
        () => new Promise((resolve) => setTimeout(() => resolve({ message: 'Success' }), 100))
      );

      render(<ResetPasswordForm token={validToken} />);

      const passwordInput = screen.getByLabelText(/^new password$/i);
      const confirmPasswordInput = screen.getByLabelText(/confirm new password/i);

      fireEvent.change(passwordInput, { target: { value: 'NewPassword123!' } });
      fireEvent.change(confirmPasswordInput, { target: { value: 'NewPassword123!' } });

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(submitButton).toBeDisabled();
      });
    });

    it('should disable submit button when success message is shown', async () => {
      const mockResponse = { message: 'Success' };
      vi.mocked(authRepository.resetPassword).mockResolvedValueOnce(mockResponse);

      render(<ResetPasswordForm token={validToken} />);

      const passwordInput = screen.getByLabelText(/^new password$/i);
      const confirmPasswordInput = screen.getByLabelText(/confirm new password/i);

      fireEvent.change(passwordInput, { target: { value: 'NewPassword123!' } });
      fireEvent.change(confirmPasswordInput, { target: { value: 'NewPassword123!' } });

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/success/i)).toBeInTheDocument();
        expect(submitButton).toBeDisabled();
      });
    });
  });

  describe('error handling', () => {
    it('should show API error message when request fails', async () => {
      const apiError = new ApiError('Invalid or expired token', 400);
      vi.mocked(authRepository.resetPassword).mockRejectedValueOnce(apiError);

      render(<ResetPasswordForm token={validToken} />);

      const passwordInput = screen.getByLabelText(/^new password$/i);
      const confirmPasswordInput = screen.getByLabelText(/confirm new password/i);

      fireEvent.change(passwordInput, { target: { value: 'NewPassword123!' } });
      fireEvent.change(confirmPasswordInput, { target: { value: 'NewPassword123!' } });

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/invalid or expired token/i)).toBeInTheDocument();
      });
    });

    it('should show generic error message for unexpected errors', async () => {
      vi.mocked(authRepository.resetPassword).mockRejectedValueOnce(
        new Error('Network error')
      );

      render(<ResetPasswordForm token={validToken} />);

      const passwordInput = screen.getByLabelText(/^new password$/i);
      const confirmPasswordInput = screen.getByLabelText(/confirm new password/i);

      fireEvent.change(passwordInput, { target: { value: 'NewPassword123!' } });
      fireEvent.change(confirmPasswordInput, { target: { value: 'NewPassword123!' } });

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/an unexpected error occurred/i)).toBeInTheDocument();
      });
    });

    it('should clear previous error when submitting again', async () => {
      const apiError = new ApiError('Token expired', 400);
      vi.mocked(authRepository.resetPassword)
        .mockRejectedValueOnce(apiError)
        .mockResolvedValueOnce({ message: 'Success' });

      render(<ResetPasswordForm token={validToken} />);

      const passwordInput = screen.getByLabelText(/^new password$/i);
      const confirmPasswordInput = screen.getByLabelText(/confirm new password/i);
      const submitButton = screen.getByRole('button', { name: /reset password/i });

      // First submission - error
      fireEvent.change(passwordInput, { target: { value: 'NewPassword123!' } });
      fireEvent.change(confirmPasswordInput, { target: { value: 'NewPassword123!' } });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/token expired/i)).toBeInTheDocument();
      });

      // Second submission - success
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.queryByText(/token expired/i)).not.toBeInTheDocument();
        expect(screen.getByText(/success/i)).toBeInTheDocument();
      });
    });
  });

  describe('accessibility', () => {
    it('should have proper aria labels', () => {
      render(<ResetPasswordForm token={validToken} />);

      const passwordInput = screen.getByLabelText(/^new password$/i);
      const confirmPasswordInput = screen.getByLabelText(/confirm new password/i);

      expect(passwordInput).toHaveAttribute('aria-invalid', 'false');
      expect(confirmPasswordInput).toHaveAttribute('aria-invalid', 'false');
    });

    it('should set aria-invalid when password has error', async () => {
      render(<ResetPasswordForm token={validToken} />);

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        const passwordInput = screen.getByLabelText(/^new password$/i);
        expect(passwordInput).toHaveAttribute('aria-invalid', 'true');
      });
    });

    it('should associate error message with password input using aria-describedby', async () => {
      render(<ResetPasswordForm token={validToken} />);

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        const passwordInput = screen.getByLabelText(/^new password$/i);
        expect(passwordInput).toHaveAttribute('aria-describedby', 'password-error');
      });
    });

    it('should associate error message with confirm password input using aria-describedby', async () => {
      render(<ResetPasswordForm token={validToken} />);

      const passwordInput = screen.getByLabelText(/^new password$/i);
      fireEvent.change(passwordInput, { target: { value: 'ValidPass123!' } });

      const submitButton = screen.getByRole('button', { name: /reset password/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        const confirmPasswordInput = screen.getByLabelText(/confirm new password/i);
        expect(confirmPasswordInput).toHaveAttribute('aria-describedby', 'confirmPassword-error');
      });
    });
  });
});
