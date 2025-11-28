import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { IconButton } from '@/presentation/components/ui/IconButton';
import { Calendar, Building2, MessageCircle } from 'lucide-react';

describe('IconButton', () => {
  describe('Rendering', () => {
    it('should render with icon and label', () => {
      render(
        <IconButton icon={<Calendar />} label="New Event" />
      );
      expect(screen.getByText('New Event')).toBeInTheDocument();
    });

    it('should render as a button element', () => {
      const { container } = render(
        <IconButton icon={<Calendar />} label="Test" />
      );
      expect(container.querySelector('button')).toBeInTheDocument();
    });

    it('should render the icon', () => {
      const { container } = render(
        <IconButton icon={<Calendar data-testid="calendar-icon" />} label="Event" />
      );
      expect(container.querySelector('[data-testid="calendar-icon"]')).toBeInTheDocument();
    });
  });

  describe('Variants', () => {
    it('should apply default variant styles', () => {
      const { container } = render(
        <IconButton icon={<Calendar />} label="Default" />
      );
      const button = container.querySelector('button');
      expect(button).toHaveClass('bg-white', 'text-gray-700');
    });

    it('should apply primary variant styles', () => {
      const { container } = render(
        <IconButton icon={<Calendar />} label="Primary" variant="primary" />
      );
      const button = container.querySelector('button');
      expect(button).toHaveClass('bg-gradient-to-br', 'from-saffron-500', 'to-maroon-500');
    });

    it('should apply secondary variant styles', () => {
      const { container } = render(
        <IconButton icon={<Calendar />} label="Secondary" variant="secondary" />
      );
      const button = container.querySelector('button');
      expect(button).toHaveClass('bg-gray-100', 'text-gray-700');
    });
  });

  describe('Sizes', () => {
    it('should apply small size styles', () => {
      const { container } = render(
        <IconButton icon={<Calendar />} label="Small" size="sm" />
      );
      const button = container.querySelector('button');
      expect(button).toHaveClass('p-3', 'min-w-[100px]');
    });

    it('should apply medium size styles', () => {
      const { container } = render(
        <IconButton icon={<Calendar />} label="Medium" size="md" />
      );
      const button = container.querySelector('button');
      expect(button).toHaveClass('p-4', 'min-w-[120px]');
    });

    it('should apply large size styles', () => {
      const { container } = render(
        <IconButton icon={<Calendar />} label="Large" size="lg" />
      );
      const button = container.querySelector('button');
      expect(button).toHaveClass('p-5', 'min-w-[140px]');
    });
  });

  describe('Interactions', () => {
    it('should call onClick when clicked', () => {
      const handleClick = vi.fn();
      render(
        <IconButton icon={<Calendar />} label="Click Me" onClick={handleClick} />
      );

      const button = screen.getByRole('button', { name: /Click Me/i });
      fireEvent.click(button);

      expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it('should not call onClick when disabled', () => {
      const handleClick = vi.fn();
      render(
        <IconButton
          icon={<Calendar />}
          label="Disabled"
          onClick={handleClick}
          disabled
        />
      );

      const button = screen.getByRole('button', { name: /Disabled/i });
      fireEvent.click(button);

      expect(handleClick).not.toHaveBeenCalled();
    });

    it('should have hover effect when not disabled', () => {
      const { container } = render(
        <IconButton icon={<Calendar />} label="Hover" />
      );
      const button = container.querySelector('button');
      expect(button).toHaveClass('hover:shadow-lg', 'hover:scale-105');
    });
  });

  describe('Custom className', () => {
    it('should accept and apply custom className', () => {
      const { container } = render(
        <IconButton
          icon={<Calendar />}
          label="Custom"
          className="custom-class"
        />
      );
      const button = container.querySelector('button');
      expect(button).toHaveClass('custom-class');
    });

    it('should merge custom className with variant styles', () => {
      const { container } = render(
        <IconButton
          icon={<Calendar />}
          label="Merged"
          variant="primary"
          className="ml-2"
        />
      );
      const button = container.querySelector('button');
      expect(button).toHaveClass('bg-gradient-to-br', 'from-saffron-500', 'ml-2');
    });
  });

  describe('Accessibility', () => {
    it('should have proper base styles for readability', () => {
      const { container } = render(
        <IconButton icon={<Calendar />} label="Accessible" />
      );
      const button = container.querySelector('button');
      expect(button).toHaveClass('rounded-lg', 'transition-all', 'duration-200');
    });

    it('should be flex column for vertical layout', () => {
      const { container } = render(
        <IconButton icon={<Calendar />} label="Vertical" />
      );
      const button = container.querySelector('button');
      expect(button).toHaveClass('flex', 'flex-col', 'items-center', 'justify-center');
    });

    it('should have proper text styles', () => {
      const { container } = render(
        <IconButton icon={<Calendar />} label="Text" />
      );
      const button = container.querySelector('button');
      const label = screen.getByText('Text');
      expect(label).toHaveClass('text-xs', 'font-medium');
    });

    it('should support disabled state', () => {
      render(
        <IconButton icon={<Calendar />} label="Disabled" disabled />
      );
      const button = screen.getByRole('button', { name: /Disabled/i });
      expect(button).toBeDisabled();
    });

    it('should have reduced opacity when disabled', () => {
      const { container } = render(
        <IconButton icon={<Calendar />} label="Disabled" disabled />
      );
      const button = container.querySelector('button');
      expect(button).toHaveClass('disabled:opacity-50', 'disabled:cursor-not-allowed');
    });
  });

  describe('Layout', () => {
    it('should have icon above label', () => {
      const { container } = render(
        <IconButton icon={<Calendar data-testid="icon" />} label="Below" />
      );
      const button = container.querySelector('button');
      const children = Array.from(button?.children || []);

      // Icon wrapper should be first, label should be second
      expect(children.length).toBeGreaterThanOrEqual(2);
    });

    it('should have gap between icon and label', () => {
      const { container } = render(
        <IconButton icon={<Calendar />} label="Spaced" />
      );
      const button = container.querySelector('button');
      expect(button).toHaveClass('gap-2');
    });
  });
});
