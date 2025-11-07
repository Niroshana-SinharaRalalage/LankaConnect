import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { EmailVerification } from '@/presentation/components/features/auth/EmailVerification';
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
    verifyEmail: vi.fn(),
  },
}));

describe('EmailVerification Component', () => {
  const validToken = 'valid-verification-token-123';

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('rendering', () => {
    it('should render the email verification card', () => {
      vi.mocked(authRepository.verifyEmail).mockImplementationOnce(
        () => new Promise(() => {}) // Never resolves to keep verifying state
      );

      render(<EmailVerification token={validToken} />);

      expect(screen.getByRole('heading', { name: /email verification/i })).toBeInTheDocument();
    });

    it('should show verifying state initially', () => {
      vi.mocked(authRepository.verifyEmail).mockImplementationOnce(
        () => new Promise(() => {})
      );

      render(<EmailVerification token={validToken} />);

      expect(screen.getByText(/verifying your email address/i)).toBeInTheDocument();
      expect(screen.getByText(/please wait while we verify your email/i)).toBeInTheDocument();
    });

    it('should show loading spinner during verification', () => {
      vi.mocked(authRepository.verifyEmail).mockImplementationOnce(
        () => new Promise(() => {})
      );

      render(<EmailVerification token={validToken} />);

      const spinner = document.querySelector('.animate-spin');
      expect(spinner).toBeInTheDocument();
    });
  });

  describe('verification flow', () => {
    it('should call verifyEmail with correct token', async () => {
      const mockResponse = { message: 'Email verified successfully' };
      vi.mocked(authRepository.verifyEmail).mockResolvedValueOnce(mockResponse);

      render(<EmailVerification token={validToken} />);

      await waitFor(() => {
        expect(authRepository.verifyEmail).toHaveBeenCalledWith(validToken);
      });
    });

    it('should show success message after successful verification', async () => {
      const mockResponse = { message: 'Your email has been verified!' };
      vi.mocked(authRepository.verifyEmail).mockResolvedValueOnce(mockResponse);

      render(<EmailVerification token={validToken} />);

      await waitFor(() => {
        expect(screen.getByText(/your email has been verified!/i)).toBeInTheDocument();
        expect(screen.getByText(/email verified successfully!/i)).toBeInTheDocument();
      });
    });

    it('should show generic success message when API returns no message', async () => {
      const mockResponse = { message: '' };
      vi.mocked(authRepository.verifyEmail).mockResolvedValueOnce(mockResponse);

      render(<EmailVerification token={validToken} />);

      await waitFor(() => {
        expect(screen.getByText(/your email has been verified successfully!/i)).toBeInTheDocument();
      });
    });

    it('should show redirect message on success', async () => {
      const mockResponse = { message: 'Success' };
      vi.mocked(authRepository.verifyEmail).mockResolvedValueOnce(mockResponse);

      render(<EmailVerification token={validToken} />);

      await waitFor(() => {
        expect(screen.getByText(/you will be redirected to the login page shortly/i)).toBeInTheDocument();
      });
    });

    it('should show "Go to Login" button on success', async () => {
      const mockResponse = { message: 'Success' };
      vi.mocked(authRepository.verifyEmail).mockResolvedValueOnce(mockResponse);

      render(<EmailVerification token={validToken} />);

      await waitFor(() => {
        const loginButton = screen.getByRole('button', { name: /go to login/i });
        expect(loginButton).toBeInTheDocument();
      });
    });

    it('should navigate to login when "Go to Login" button is clicked', async () => {
      const mockResponse = { message: 'Success' };
      vi.mocked(authRepository.verifyEmail).mockResolvedValueOnce(mockResponse);

      render(<EmailVerification token={validToken} />);

      const loginButton = await screen.findByRole('button', { name: /go to login/i });
      fireEvent.click(loginButton);

      expect(mockPush).toHaveBeenCalledWith('/login');
    });
  });

  describe('error handling', () => {
    it('should show error state when token is missing', () => {
      render(<EmailVerification token="" />);

      expect(screen.getByText(/verification failed/i)).toBeInTheDocument();
      expect(screen.getByText(/invalid verification link. no token provided./i)).toBeInTheDocument();
    });

    it('should show API error message when verification fails', async () => {
      const apiError = new ApiError('Invalid or expired token', 400);
      vi.mocked(authRepository.verifyEmail).mockRejectedValueOnce(apiError);

      render(<EmailVerification token={validToken} />);

      await waitFor(() => {
        expect(screen.getByText(/invalid or expired token/i)).toBeInTheDocument();
        expect(screen.getByText(/verification failed/i)).toBeInTheDocument();
      });
    });

    it('should show generic error message for unexpected errors', async () => {
      vi.mocked(authRepository.verifyEmail).mockRejectedValueOnce(
        new Error('Network error')
      );

      render(<EmailVerification token={validToken} />);

      await waitFor(() => {
        expect(screen.getByText(/an unexpected error occurred while verifying your email/i)).toBeInTheDocument();
      });
    });

    it('should show additional error context message', async () => {
      const apiError = new ApiError('Token expired', 400);
      vi.mocked(authRepository.verifyEmail).mockRejectedValueOnce(apiError);

      render(<EmailVerification token={validToken} />);

      await waitFor(() => {
        expect(screen.getByText(/the verification link may have expired or is invalid/i)).toBeInTheDocument();
      });
    });

    it('should show "Back to Login" button on error', async () => {
      const apiError = new ApiError('Token expired', 400);
      vi.mocked(authRepository.verifyEmail).mockRejectedValueOnce(apiError);

      render(<EmailVerification token={validToken} />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /back to login/i })).toBeInTheDocument();
      });
    });

    it('should show "Back to Login" link on error', async () => {
      const apiError = new ApiError('Token expired', 400);
      vi.mocked(authRepository.verifyEmail).mockRejectedValueOnce(apiError);

      render(<EmailVerification token={validToken} />);

      await waitFor(() => {
        const loginLink = screen.getByRole('link', { name: /back to login/i });
        expect(loginLink).toBeInTheDocument();
        expect(loginLink).toHaveAttribute('href', '/login');
      });
    });

    it('should show "Contact Support" link on error', async () => {
      const apiError = new ApiError('Token expired', 400);
      vi.mocked(authRepository.verifyEmail).mockRejectedValueOnce(apiError);

      render(<EmailVerification token={validToken} />);

      await waitFor(() => {
        const supportLink = screen.getByRole('link', { name: /contact support/i });
        expect(supportLink).toBeInTheDocument();
        expect(supportLink).toHaveAttribute('href', '/contact');
      });
    });
  });

  describe('visual feedback', () => {
    it('should show success icon on successful verification', async () => {
      const mockResponse = { message: 'Success' };
      vi.mocked(authRepository.verifyEmail).mockResolvedValueOnce(mockResponse);

      render(<EmailVerification token={validToken} />);

      await waitFor(() => {
        const successIcon = document.querySelector('.text-green-600');
        expect(successIcon).toBeInTheDocument();
      });
    });

    it('should show error icon on failed verification', async () => {
      const apiError = new ApiError('Token expired', 400);
      vi.mocked(authRepository.verifyEmail).mockRejectedValueOnce(apiError);

      render(<EmailVerification token={validToken} />);

      await waitFor(() => {
        const errorIcon = document.querySelector('.text-red-600');
        expect(errorIcon).toBeInTheDocument();
      });
    });

    it('should have success styling on success state', async () => {
      const mockResponse = { message: 'Success' };
      vi.mocked(authRepository.verifyEmail).mockResolvedValueOnce(mockResponse);

      render(<EmailVerification token={validToken} />);

      await waitFor(() => {
        const successBox = document.querySelector('.bg-green-50');
        expect(successBox).toBeInTheDocument();
      });
    });

    it('should have error styling on error state', async () => {
      const apiError = new ApiError('Token expired', 400);
      vi.mocked(authRepository.verifyEmail).mockRejectedValueOnce(apiError);

      render(<EmailVerification token={validToken} />);

      await waitFor(() => {
        const errorBox = document.querySelector('.bg-red-50');
        expect(errorBox).toBeInTheDocument();
      });
    });
  });
});
