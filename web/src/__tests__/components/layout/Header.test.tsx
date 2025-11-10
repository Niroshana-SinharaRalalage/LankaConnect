import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Header } from '@/presentation/components/layout/Header';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useRouter } from 'next/navigation';

// Mock Next.js router
vi.mock('next/navigation', () => ({
  useRouter: vi.fn(),
}));

// Mock auth store
vi.mock('@/presentation/store/useAuthStore', () => ({
  useAuthStore: vi.fn(),
}));

/**
 * Test Suite for Header Component
 * Tests authentication states, navigation, user interactions, and accessibility
 */
describe('Header Component', () => {
  const mockPush = vi.fn();
  const mockUseRouter = useRouter as ReturnType<typeof vi.fn>;
  const mockUseAuthStore = useAuthStore as unknown as ReturnType<typeof vi.fn>;

  beforeEach(() => {
    vi.clearAllMocks();
    mockUseRouter.mockReturnValue({ push: mockPush });
  });

  describe('Rendering with Unauthenticated State', () => {
    beforeEach(() => {
      mockUseAuthStore.mockReturnValue({
        user: null,
        isAuthenticated: false,
      });
    });

    it('should render logo and LankaConnect text', () => {
      render(<Header />);
      expect(screen.getByText('LankaConnect')).toBeInTheDocument();
    });

    it('should render Login button when not authenticated', () => {
      render(<Header />);
      const loginButton = screen.getByRole('button', { name: /login/i });
      expect(loginButton).toBeInTheDocument();
      expect(loginButton).toHaveClass('border-[#8B1538]');
    });

    it('should render Sign Up button when not authenticated', () => {
      render(<Header />);
      const signUpButton = screen.getByRole('button', { name: /sign up/i });
      expect(signUpButton).toBeInTheDocument();
      expect(signUpButton).toHaveClass('bg-[#FF7900]');
    });

    it('should not render Dashboard link when not authenticated', () => {
      render(<Header />);
      const dashboardLinks = screen.queryAllByText(/dashboard/i);
      expect(dashboardLinks).toHaveLength(0);
    });

    it('should not render user avatar when not authenticated', () => {
      render(<Header />);
      const buttons = screen.getAllByRole('button');
      // Only Login and Sign Up buttons should exist
      expect(buttons).toHaveLength(2);
      // Verify no avatar by checking no element with title matching a user name
      const avatar = screen.queryByTitle(/John Doe|Jane Smith/);
      expect(avatar).not.toBeInTheDocument();
    });
  });

  describe('Rendering with Authenticated State', () => {
    const mockUser = {
      id: '123',
      email: 'test@example.com',
      fullName: 'John Doe',
      isEmailVerified: true,
    };

    beforeEach(() => {
      mockUseAuthStore.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
      });
    });

    it('should render user avatar when authenticated', () => {
      render(<Header />);
      const avatar = screen.getByTitle('John Doe');
      expect(avatar).toBeInTheDocument();
      expect(avatar).toHaveTextContent('JD'); // Initials
    });

    it('should display user full name on large screens', () => {
      render(<Header />);
      const userName = screen.getByText('John Doe');
      expect(userName).toBeInTheDocument();
      expect(userName).toHaveClass('hidden', 'lg:inline');
    });

    it('should render Dashboard link when authenticated', () => {
      render(<Header />);
      const dashboardLink = screen.getByRole('link', { name: /dashboard/i });
      expect(dashboardLink).toBeInTheDocument();
      expect(dashboardLink).toHaveAttribute('href', '/dashboard');
    });

    it('should not render Login/Sign Up buttons when authenticated', () => {
      render(<Header />);
      const loginButton = screen.queryByRole('button', { name: /login/i });
      const signUpButton = screen.queryByRole('button', { name: /sign up/i });
      expect(loginButton).not.toBeInTheDocument();
      expect(signUpButton).not.toBeInTheDocument();
    });

    it('should generate correct initials for user with one name', () => {
      mockUseAuthStore.mockReturnValue({
        user: { ...mockUser, fullName: 'Madonna' },
        isAuthenticated: true,
      });
      render(<Header />);
      const avatar = screen.getByTitle('Madonna');
      expect(avatar).toHaveTextContent('MA');
    });
  });

  describe('Navigation Links', () => {
    beforeEach(() => {
      mockUseAuthStore.mockReturnValue({
        user: null,
        isAuthenticated: false,
      });
    });

    it('should render Home navigation link', () => {
      render(<Header />);
      const homeLink = screen.getByRole('link', { name: /^home$/i });
      expect(homeLink).toBeInTheDocument();
      expect(homeLink).toHaveAttribute('href', '/');
    });

    it('should render Events navigation link', () => {
      render(<Header />);
      const eventsLink = screen.getByRole('link', { name: /events/i });
      expect(eventsLink).toBeInTheDocument();
      expect(eventsLink).toHaveAttribute('href', '#events');
    });

    it('should render Forums navigation link', () => {
      render(<Header />);
      const forumsLink = screen.getByRole('link', { name: /forums/i });
      expect(forumsLink).toBeInTheDocument();
      expect(forumsLink).toHaveAttribute('href', '#forums');
    });

    it('should render Business navigation link', () => {
      render(<Header />);
      const businessLink = screen.getByRole('link', { name: /business/i });
      expect(businessLink).toBeInTheDocument();
      expect(businessLink).toHaveAttribute('href', '#business');
    });

    it('should render Culture navigation link', () => {
      render(<Header />);
      const cultureLink = screen.getByRole('link', { name: /culture/i });
      expect(cultureLink).toBeInTheDocument();
      expect(cultureLink).toHaveAttribute('href', '#culture');
    });
  });

  describe('User Interactions', () => {
    beforeEach(() => {
      mockUseAuthStore.mockReturnValue({
        user: null,
        isAuthenticated: false,
      });
    });

    it('should navigate to "/" when logo is clicked', () => {
      render(<Header />);
      const logoLink = screen.getByRole('link', { name: /lankaconnect/i });
      expect(logoLink).toHaveAttribute('href', '/');
    });

    it('should navigate to /login when Login button is clicked', () => {
      render(<Header />);
      const loginButton = screen.getByRole('button', { name: /login/i });
      fireEvent.click(loginButton);
      expect(mockPush).toHaveBeenCalledWith('/login');
    });

    it('should navigate to /register when Sign Up button is clicked', () => {
      render(<Header />);
      const signUpButton = screen.getByRole('button', { name: /sign up/i });
      fireEvent.click(signUpButton);
      expect(mockPush).toHaveBeenCalledWith('/register');
    });

    it('should navigate to /profile when avatar is clicked', () => {
      mockUseAuthStore.mockReturnValue({
        user: { id: '123', email: 'test@example.com', fullName: 'John Doe', isEmailVerified: true },
        isAuthenticated: true,
      });
      render(<Header />);
      const avatar = screen.getByTitle('John Doe');
      fireEvent.click(avatar);
      expect(mockPush).toHaveBeenCalledWith('/profile');
    });

    it('should navigate to /profile when Enter key is pressed on avatar', () => {
      mockUseAuthStore.mockReturnValue({
        user: { id: '123', email: 'test@example.com', fullName: 'John Doe', isEmailVerified: true },
        isAuthenticated: true,
      });
      render(<Header />);
      const avatar = screen.getByTitle('John Doe');
      fireEvent.keyDown(avatar, { key: 'Enter' });
      expect(mockPush).toHaveBeenCalledWith('/profile');
    });

    it('should navigate to /profile when Space key is pressed on avatar', () => {
      mockUseAuthStore.mockReturnValue({
        user: { id: '123', email: 'test@example.com', fullName: 'John Doe', isEmailVerified: true },
        isAuthenticated: true,
      });
      render(<Header />);
      const avatar = screen.getByTitle('John Doe');
      fireEvent.keyDown(avatar, { key: ' ' });
      expect(mockPush).toHaveBeenCalledWith('/profile');
    });
  });

  describe('Responsive Behavior', () => {
    beforeEach(() => {
      mockUseAuthStore.mockReturnValue({
        user: null,
        isAuthenticated: false,
      });
    });

    it('should apply sticky positioning classes', () => {
      const { container } = render(<Header />);
      const header = container.querySelector('header');
      expect(header).toHaveClass('sticky', 'top-0', 'z-50');
    });

    it('should apply shadow styling', () => {
      const { container } = render(<Header />);
      const header = container.querySelector('header');
      expect(header).toHaveClass('shadow-[0_2px_10px_rgba(0,0,0,0.08)]');
    });

    it('should hide navigation links on mobile (md:flex)', () => {
      const { container } = render(<Header />);
      const nav = container.querySelector('ul');
      expect(nav).toHaveClass('hidden', 'md:flex');
    });

    it('should accept custom className prop', () => {
      const { container } = render(<Header className="custom-class" />);
      const header = container.querySelector('header');
      expect(header).toHaveClass('custom-class');
    });
  });

  describe('Accessibility', () => {
    it('should have proper role for header', () => {
      const { container } = render(<Header />);
      const header = container.querySelector('header');
      expect(header).toBeInTheDocument();
    });

    it('should have proper role for navigation', () => {
      const { container } = render(<Header />);
      const nav = container.querySelector('nav');
      expect(nav).toBeInTheDocument();
    });

    it('should make avatar keyboard accessible with tabIndex', () => {
      mockUseAuthStore.mockReturnValue({
        user: { id: '123', email: 'test@example.com', fullName: 'John Doe', isEmailVerified: true },
        isAuthenticated: true,
      });
      render(<Header />);
      const avatar = screen.getByTitle('John Doe');
      expect(avatar).toHaveAttribute('tabIndex', '0');
      expect(avatar).toHaveAttribute('role', 'button');
    });

    it('should have proper hover states for navigation links', () => {
      render(<Header />);
      const homeLink = screen.getByRole('link', { name: /^home$/i });
      expect(homeLink).toHaveClass('hover:text-[#FF7900]');
    });
  });
});
