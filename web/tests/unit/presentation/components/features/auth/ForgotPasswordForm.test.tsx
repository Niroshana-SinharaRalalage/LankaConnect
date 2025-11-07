import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { ForgotPasswordForm } from '@/presentation/components/features/auth/ForgotPasswordForm';
import { authRepository } from '@/infrastructure/api/repositories/auth.repository';
import { ApiError } from '@/infrastructure/api/client/api-errors';

// Mock the auth repository
vi.mock('@/infrastructure/api/repositories/auth.repository', () => ({
  authRepository: {
    requestPasswordReset: vi.fn(),
  },
}));

describe('ForgotPasswordForm Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('rendering', () => {
    it('should render the forgot password form', () => {
      render(<ForgotPasswordForm />);

      expect(screen.getByRole('heading', { name: /forgot password/i })).toBeInTheDocument();
      expect(screen.getByText(/enter your email address/i)).toBeInTheDocument();
    });

    it('should render email input field', () => {
      render(<ForgotPasswordForm />);

      const emailInput = screen.getByLabelText(/email address/i);
      expect(emailInput).toBeInTheDocument();
      expect(emailInput).toHaveAttribute('type', 'email');
    });

    it('should render submit button', () => {
      render(<ForgotPasswordForm />);

      const submitButton = screen.getByRole('button', { name: /send reset instructions/i });
      expect(submitButton).toBeInTheDocument();
    });

    it('should render back to login link', () => {
      render(<ForgotPasswordForm />);

      const loginLink = screen.getByRole('link', { name: /back to login/i });
      expect(loginLink).toBeInTheDocument();
      expect(loginLink).toHaveAttribute('href', '/login');
    });
  });

  describe('validation', () => {
    it('should show error when email is empty', async () => {
      render(<ForgotPasswordForm />);

      const submitButton = screen.getByRole('button', { name: /send reset instructions/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/email is required/i)).toBeInTheDocument();
      });
    });

    it('should not show error when email is valid', async () => {
      render(<ForgotPasswordForm />);

      const emailInput = screen.getByLabelText(/email address/i);
      fireEvent.change(emailInput, { target: { value: 'test@example.com' } });

      const submitButton = screen.getByRole('button', { name: /send reset instructions/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        const errorMessage = screen.queryByText(/please enter a valid email address/i);
        expect(errorMessage).not.toBeInTheDocument();
      });
    });
  });

  describe('form submission', () => {
    it('should call requestPasswordReset with correct email', async () => {
      const mockResponse = { message: 'Reset instructions sent' };
      vi.mocked(authRepository.requestPasswordReset).mockResolvedValueOnce(mockResponse);

      render(<ForgotPasswordForm />);

      const emailInput = screen.getByLabelText(/email address/i);
      fireEvent.change(emailInput, { target: { value: 'test@example.com' } });

      const submitButton = screen.getByRole('button', { name: /send reset instructions/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(authRepository.requestPasswordReset).toHaveBeenCalledWith('test@example.com');
      });
    });

    it('should show success message after successful submission', async () => {
      const mockResponse = { message: 'Reset instructions sent to your email' };
      vi.mocked(authRepository.requestPasswordReset).mockResolvedValueOnce(mockResponse);

      render(<ForgotPasswordForm />);

      const emailInput = screen.getByLabelText(/email address/i);
      fireEvent.change(emailInput, { target: { value: 'test@example.com' } });

      const submitButton = screen.getByRole('button', { name: /send reset instructions/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/reset instructions sent to your email/i)).toBeInTheDocument();
      });
    });

    it('should show generic success message when API returns no message', async () => {
      const mockResponse = { message: '' };
      vi.mocked(authRepository.requestPasswordReset).mockResolvedValueOnce(mockResponse);

      render(<ForgotPasswordForm />);

      const emailInput = screen.getByLabelText(/email address/i);
      fireEvent.change(emailInput, { target: { value: 'test@example.com' } });

      const submitButton = screen.getByRole('button', { name: /send reset instructions/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/if an account exists with this email/i)).toBeInTheDocument();
      });
    });

    it('should disable submit button during submission', async () => {
      vi.mocked(authRepository.requestPasswordReset).mockImplementationOnce(
        () => new Promise((resolve) => setTimeout(() => resolve({ message: 'Sent' }), 100))
      );

      render(<ForgotPasswordForm />);

      const emailInput = screen.getByLabelText(/email address/i);
      fireEvent.change(emailInput, { target: { value: 'test@example.com' } });

      const submitButton = screen.getByRole('button', { name: /send reset instructions/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(submitButton).toBeDisabled();
      });
    });
  });

  describe('error handling', () => {
    it('should show API error message when request fails', async () => {
      const apiError = new ApiError('User not found', 404);
      vi.mocked(authRepository.requestPasswordReset).mockRejectedValueOnce(apiError);

      render(<ForgotPasswordForm />);

      const emailInput = screen.getByLabelText(/email address/i);
      fireEvent.change(emailInput, { target: { value: 'test@example.com' } });

      const submitButton = screen.getByRole('button', { name: /send reset instructions/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/user not found/i)).toBeInTheDocument();
      });
    });

    it('should show generic error message for unexpected errors', async () => {
      vi.mocked(authRepository.requestPasswordReset).mockRejectedValueOnce(
        new Error('Network error')
      );

      render(<ForgotPasswordForm />);

      const emailInput = screen.getByLabelText(/email address/i);
      fireEvent.change(emailInput, { target: { value: 'test@example.com' } });

      const submitButton = screen.getByRole('button', { name: /send reset instructions/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/an unexpected error occurred/i)).toBeInTheDocument();
      });
    });

    it('should clear previous error when submitting again', async () => {
      const apiError = new ApiError('Rate limit exceeded', 429);
      vi.mocked(authRepository.requestPasswordReset)
        .mockRejectedValueOnce(apiError)
        .mockResolvedValueOnce({ message: 'Success' });

      render(<ForgotPasswordForm />);

      const emailInput = screen.getByLabelText(/email address/i);
      const submitButton = screen.getByRole('button', { name: /send reset instructions/i });

      // First submission - error
      fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/rate limit exceeded/i)).toBeInTheDocument();
      });

      // Second submission - success
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.queryByText(/rate limit exceeded/i)).not.toBeInTheDocument();
        expect(screen.getByText(/success/i)).toBeInTheDocument();
      });
    });
  });

  describe('accessibility', () => {
    it('should have proper aria labels', () => {
      render(<ForgotPasswordForm />);

      const emailInput = screen.getByLabelText(/email address/i);
      expect(emailInput).toHaveAttribute('aria-invalid', 'false');
    });

    it('should set aria-invalid when email has error', async () => {
      render(<ForgotPasswordForm />);

      const submitButton = screen.getByRole('button', { name: /send reset instructions/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        const emailInput = screen.getByLabelText(/email address/i);
        expect(emailInput).toHaveAttribute('aria-invalid', 'true');
      });
    });

    it('should associate error message with input using aria-describedby', async () => {
      render(<ForgotPasswordForm />);

      const submitButton = screen.getByRole('button', { name: /send reset instructions/i });
      fireEvent.click(submitButton);

      await waitFor(() => {
        const emailInput = screen.getByLabelText(/email address/i);
        expect(emailInput).toHaveAttribute('aria-describedby', 'email-error');
      });
    });
  });
});
