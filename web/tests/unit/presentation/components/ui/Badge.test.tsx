import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Badge } from '@/presentation/components/ui/Badge';

describe('Badge', () => {
  describe('Rendering', () => {
    it('should render with children text', () => {
      render(<Badge>Test Badge</Badge>);
      expect(screen.getByText('Test Badge')).toBeInTheDocument();
    });

    it('should render as a span element', () => {
      const { container } = render(<Badge>Badge</Badge>);
      expect(container.querySelector('span')).toBeInTheDocument();
    });
  });

  describe('Variants', () => {
    it('should apply default variant styles', () => {
      const { container } = render(<Badge>Default</Badge>);
      const badge = container.querySelector('span');
      expect(badge).toHaveClass('bg-gray-100', 'text-gray-800');
    });

    it('should apply cultural variant styles', () => {
      const { container } = render(<Badge variant="cultural">Cultural</Badge>);
      const badge = container.querySelector('span');
      expect(badge).toHaveClass('bg-saffron-100', 'text-saffron-800');
    });

    it('should apply arts variant styles', () => {
      const { container } = render(<Badge variant="arts">Arts</Badge>);
      const badge = container.querySelector('span');
      expect(badge).toHaveClass('bg-pink-100', 'text-pink-800');
    });

    it('should apply food variant styles', () => {
      const { container} = render(<Badge variant="food">Food</Badge>);
      const badge = container.querySelector('span');
      expect(badge).toHaveClass('bg-orange-100', 'text-orange-800');
    });

    it('should apply business variant styles', () => {
      const { container } = render(<Badge variant="business">Business</Badge>);
      const badge = container.querySelector('span');
      expect(badge).toHaveClass('bg-cyan-100', 'text-cyan-800');
    });

    it('should apply community variant styles', () => {
      const { container } = render(<Badge variant="community">Community</Badge>);
      const badge = container.querySelector('span');
      expect(badge).toHaveClass('bg-purple-100', 'text-purple-800');
    });

    it('should apply featured variant styles', () => {
      const { container } = render(<Badge variant="featured">Featured</Badge>);
      const badge = container.querySelector('span');
      expect(badge).toHaveClass('bg-green-100', 'text-green-800');
    });

    it('should apply new variant styles', () => {
      const { container } = render(<Badge variant="new">New</Badge>);
      const badge = container.querySelector('span');
      expect(badge).toHaveClass('bg-emerald-100', 'text-emerald-800');
    });

    it('should apply hot variant styles', () => {
      const { container } = render(<Badge variant="hot">Hot</Badge>);
      const badge = container.querySelector('span');
      expect(badge).toHaveClass('bg-red-100', 'text-red-800');
    });
  });

  describe('Custom className', () => {
    it('should accept and apply custom className', () => {
      const { container } = render(
        <Badge className="custom-class">Badge</Badge>
      );
      const badge = container.querySelector('span');
      expect(badge).toHaveClass('custom-class');
    });

    it('should merge custom className with variant styles', () => {
      const { container } = render(
        <Badge variant="cultural" className="ml-2">Badge</Badge>
      );
      const badge = container.querySelector('span');
      expect(badge).toHaveClass('bg-saffron-100', 'text-saffron-800', 'ml-2');
    });
  });

  describe('Accessibility', () => {
    it('should have proper base styles for readability', () => {
      const { container } = render(<Badge>Badge</Badge>);
      const badge = container.querySelector('span');
      expect(badge).toHaveClass('rounded-full', 'px-2.5', 'py-0.5', 'text-xs', 'font-medium');
    });

    it('should be inline-flex for proper alignment', () => {
      const { container } = render(<Badge>Badge</Badge>);
      const badge = container.querySelector('span');
      expect(badge).toHaveClass('inline-flex', 'items-center');
    });
  });
});
