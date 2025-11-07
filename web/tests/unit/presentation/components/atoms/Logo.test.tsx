import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Logo } from '@/presentation/components/atoms/Logo';

describe('Logo Component', () => {
  describe('rendering', () => {
    it('should render logo image', () => {
      render(<Logo />);

      const logo = screen.getByAltText('LankaConnect');
      expect(logo).toBeInTheDocument();
      expect(logo).toHaveAttribute('src', '/logos/lankaconnect-logo-transparent.png');
    });

    it('should render logo with custom size', () => {
      render(<Logo size="lg" />);

      const logo = screen.getByAltText('LankaConnect');
      expect(logo).toBeInTheDocument();
      expect(logo).toHaveClass('h-16', 'w-16');
    });

    it('should render logo with small size', () => {
      render(<Logo size="sm" />);

      const logo = screen.getByAltText('LankaConnect');
      expect(logo).toBeInTheDocument();
      expect(logo).toHaveClass('h-8', 'w-8');
    });

    it('should render logo with medium size by default', () => {
      render(<Logo />);

      const logo = screen.getByAltText('LankaConnect');
      expect(logo).toHaveClass('h-12', 'w-12');
    });

    it('should apply custom className', () => {
      render(<Logo className="custom-class" />);

      const logo = screen.getByAltText('LankaConnect');
      expect(logo).toHaveClass('custom-class');
    });
  });

  describe('with text', () => {
    it('should render logo with text when showText is true', () => {
      render(<Logo showText />);

      const text = screen.getByText('LankaConnect');
      expect(text).toBeInTheDocument();
    });

    it('should not render text by default', () => {
      render(<Logo />);

      const text = screen.queryByText('LankaConnect');
      expect(text).not.toBeInTheDocument();
    });

    it('should render text with appropriate styling', () => {
      render(<Logo showText />);

      const text = screen.getByText('LankaConnect');
      expect(text).toHaveClass('font-bold');
    });
  });

  describe('accessibility', () => {
    it('should have alt text for screen readers', () => {
      render(<Logo />);

      const logo = screen.getByAltText('LankaConnect');
      expect(logo).toBeInTheDocument();
    });

    it('should be keyboard accessible when clickable', () => {
      render(<Logo />);

      const container = screen.getByAltText('LankaConnect').parentElement;
      expect(container?.tagName).not.toBe('BUTTON'); // Should not be clickable by default
    });
  });
});
