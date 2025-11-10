import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import Footer from '@/presentation/components/layout/Footer';

// Mock Next.js Link component
vi.mock('next/link', () => ({
  default: ({ children, href, ...props }: any) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

/**
 * Test Suite for Footer Component
 * Tests link categories, newsletter signup, email validation, and accessibility
 */
describe('Footer Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe('Link Categories Rendering', () => {
    it('should render all four link category sections', () => {
      render(<Footer />);
      expect(screen.getByText('About')).toBeInTheDocument();
      expect(screen.getByText('Community')).toBeInTheDocument();
      expect(screen.getByText('Resources')).toBeInTheDocument();
      expect(screen.getByText('Connect')).toBeInTheDocument();
    });

    it('should render About category links', () => {
      render(<Footer />);
      expect(screen.getByRole('link', { name: /about us/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /our mission/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /team/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /contact/i })).toBeInTheDocument();
    });

    it('should render Community category links', () => {
      render(<Footer />);
      expect(screen.getByRole('link', { name: /^events$/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /^forums$/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /businesses/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /^culture$/i })).toBeInTheDocument();
    });

    it('should render Resources category links', () => {
      render(<Footer />);
      expect(screen.getByRole('link', { name: /help center/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /privacy policy/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /terms of service/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /faq/i })).toBeInTheDocument();
    });

    it('should render Connect category links', () => {
      render(<Footer />);
      expect(screen.getByRole('link', { name: /facebook/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /twitter/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /instagram/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /linkedin/i })).toBeInTheDocument();
    });
  });

  describe('Newsletter Signup', () => {
    it('should render newsletter signup form', () => {
      render(<Footer />);
      expect(screen.getByText('Stay Connected')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('Enter your email')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /subscribe to newsletter/i })).toBeInTheDocument();
    });

    it('should update email input value on change', () => {
      render(<Footer />);
      const emailInput = screen.getByPlaceholderText('Enter your email') as HTMLInputElement;
      fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
      expect(emailInput.value).toBe('test@example.com');
    });

    it('should show loading state during submission', async () => {
      render(<Footer />);
      const emailInput = screen.getByPlaceholderText('Enter your email');
      const submitButton = screen.getByRole('button', { name: /subscribe to newsletter/i });

      fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
      fireEvent.click(submitButton);

      expect(screen.getByText('Subscribing...')).toBeInTheDocument();
      expect(submitButton).toBeDisabled();
    });

    it('should show success message after successful submission', async () => {
      render(<Footer />);
      const emailInput = screen.getByPlaceholderText('Enter your email') as HTMLInputElement;
      const submitButton = screen.getByRole('button', { name: /subscribe to newsletter/i });

      fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
      fireEvent.click(submitButton);

      // Fast-forward through loading state
      vi.advanceTimersByTime(1000);
      await waitFor(() => {
        expect(screen.getByText('Thank you for subscribing!')).toBeInTheDocument();
        expect(screen.getByText('Subscribed!')).toBeInTheDocument();
      });
    });

    it('should clear email input after successful submission', async () => {
      render(<Footer />);
      const emailInput = screen.getByPlaceholderText('Enter your email') as HTMLInputElement;

      fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
      fireEvent.submit(emailInput.closest('form')!);

      vi.advanceTimersByTime(1000);
      await waitFor(() => {
        expect(emailInput.value).toBe('');
      });
    });

    it('should reset success message after 3 seconds', async () => {
      render(<Footer />);
      const emailInput = screen.getByPlaceholderText('Enter your email');

      fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
      fireEvent.submit(emailInput.closest('form')!);

      vi.advanceTimersByTime(1000);
      await waitFor(() => {
        expect(screen.getByText('Thank you for subscribing!')).toBeInTheDocument();
      });

      vi.advanceTimersByTime(3000);
      await waitFor(() => {
        expect(screen.queryByText('Thank you for subscribing!')).not.toBeInTheDocument();
      });
    });
  });

  describe('Email Validation', () => {
    it('should show error for empty email', async () => {
      render(<Footer />);
      const submitButton = screen.getByRole('button', { name: /subscribe to newsletter/i });

      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText('Please enter a valid email address.')).toBeInTheDocument();
      });
    });

    it('should show error for invalid email without @', async () => {
      render(<Footer />);
      const emailInput = screen.getByPlaceholderText('Enter your email');
      const submitButton = screen.getByRole('button', { name: /subscribe to newsletter/i });

      fireEvent.change(emailInput, { target: { value: 'invalidemail' } });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText('Please enter a valid email address.')).toBeInTheDocument();
      });
    });

    it('should not show error for valid email', async () => {
      render(<Footer />);
      const emailInput = screen.getByPlaceholderText('Enter your email');

      fireEvent.change(emailInput, { target: { value: 'valid@example.com' } });
      fireEvent.submit(emailInput.closest('form')!);

      await waitFor(() => {
        expect(screen.queryByText('Please enter a valid email address.')).not.toBeInTheDocument();
      });
    });

    it('should have required attribute on email input', () => {
      render(<Footer />);
      const emailInput = screen.getByPlaceholderText('Enter your email');
      expect(emailInput).toHaveAttribute('required');
    });
  });

  describe('External Links', () => {
    it('should open Facebook link in new tab', () => {
      render(<Footer />);
      const facebookLink = screen.getByRole('link', { name: /facebook/i });
      expect(facebookLink).toHaveAttribute('target', '_blank');
      expect(facebookLink).toHaveAttribute('rel', 'noopener noreferrer');
    });

    it('should open Twitter link in new tab', () => {
      render(<Footer />);
      const twitterLink = screen.getByRole('link', { name: /twitter/i });
      expect(twitterLink).toHaveAttribute('target', '_blank');
      expect(twitterLink).toHaveAttribute('rel', 'noopener noreferrer');
    });

    it('should open Instagram link in new tab', () => {
      render(<Footer />);
      const instagramLink = screen.getByRole('link', { name: /instagram/i });
      expect(instagramLink).toHaveAttribute('target', '_blank');
      expect(instagramLink).toHaveAttribute('rel', 'noopener noreferrer');
    });

    it('should open LinkedIn link in new tab', () => {
      render(<Footer />);
      const linkedinLink = screen.getByRole('link', { name: /linkedin/i });
      expect(linkedinLink).toHaveAttribute('target', '_blank');
      expect(linkedinLink).toHaveAttribute('rel', 'noopener noreferrer');
    });
  });

  describe('Copyright Section', () => {
    it('should display current year in copyright', () => {
      const currentYear = new Date().getFullYear();
      render(<Footer />);
      expect(screen.getByText(new RegExp(`${currentYear}.*LankaConnect`))).toBeInTheDocument();
    });

    it('should render copyright bottom links', () => {
      render(<Footer />);
      const links = screen.getAllByRole('link', { name: /privacy|terms|cookies/i });
      expect(links.length).toBeGreaterThanOrEqual(3);
    });

    it('should have contentinfo role on footer', () => {
      const { container } = render(<Footer />);
      const footer = container.querySelector('footer');
      expect(footer).toHaveAttribute('role', 'contentinfo');
    });
  });

  describe('Accessibility', () => {
    it('should have proper ARIA label for email input', () => {
      render(<Footer />);
      const emailInput = screen.getByLabelText('Email address for newsletter');
      expect(emailInput).toBeInTheDocument();
    });

    it('should have proper ARIA label for subscribe button', () => {
      render(<Footer />);
      const button = screen.getByLabelText('Subscribe to newsletter');
      expect(button).toBeInTheDocument();
    });

    it('should have role="alert" on error messages', async () => {
      render(<Footer />);
      const submitButton = screen.getByRole('button', { name: /subscribe to newsletter/i });

      fireEvent.click(submitButton);

      await waitFor(() => {
        const errorMessage = screen.getByRole('alert');
        expect(errorMessage).toHaveTextContent('Please enter a valid email address.');
      });
    });

    it('should have role="alert" on success messages', async () => {
      render(<Footer />);
      const emailInput = screen.getByPlaceholderText('Enter your email');

      fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
      fireEvent.submit(emailInput.closest('form')!);

      vi.advanceTimersByTime(1000);
      await waitFor(() => {
        const successMessage = screen.getByRole('alert');
        expect(successMessage).toHaveTextContent('Thank you for subscribing!');
      });
    });

    it('should have role="list" on link lists', () => {
      const { container } = render(<Footer />);
      const lists = container.querySelectorAll('[role="list"]');
      expect(lists.length).toBeGreaterThan(0);
    });
  });
});
