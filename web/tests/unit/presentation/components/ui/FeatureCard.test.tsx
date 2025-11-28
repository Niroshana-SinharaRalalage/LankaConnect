import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { FeatureCard } from '@/presentation/components/ui/FeatureCard';
import { Users, Calendar, Building2 } from 'lucide-react';

describe('FeatureCard', () => {
  describe('Rendering', () => {
    it('should render with title and description', () => {
      render(
        <FeatureCard
          icon={<Users />}
          title="Community"
          description="Connect with Sri Lankans"
        />
      );
      expect(screen.getByText('Community')).toBeInTheDocument();
      expect(screen.getByText('Connect with Sri Lankans')).toBeInTheDocument();
    });

    it('should render as a div element', () => {
      const { container } = render(
        <FeatureCard
          icon={<Users />}
          title="Test"
          description="Test description"
        />
      );
      expect(container.firstChild?.nodeName).toBe('DIV');
    });

    it('should render the icon', () => {
      const { container } = render(
        <FeatureCard
          icon={<Users data-testid="users-icon" />}
          title="Feature"
          description="Description"
        />
      );
      expect(container.querySelector('[data-testid="users-icon"]')).toBeInTheDocument();
    });

    it('should render stat when provided', () => {
      render(
        <FeatureCard
          icon={<Users />}
          title="Members"
          description="Active community"
          stat="1,234"
        />
      );
      expect(screen.getByText('1,234')).toBeInTheDocument();
    });
  });

  describe('Variants', () => {
    it('should apply default variant styles', () => {
      const { container } = render(
        <FeatureCard
          icon={<Users />}
          title="Default"
          description="Default variant"
        />
      );
      const card = container.firstChild as HTMLElement;
      expect(card).toHaveClass('bg-white', 'border-gray-200');
    });

    it('should apply gradient variant styles', () => {
      const { container } = render(
        <FeatureCard
          icon={<Users />}
          title="Gradient"
          description="Gradient variant"
          variant="gradient"
        />
      );
      const card = container.firstChild as HTMLElement;
      expect(card).toHaveClass('bg-gradient-to-br', 'from-purple-600', 'to-purple-800');
    });

    it('should apply cultural variant styles', () => {
      const { container } = render(
        <FeatureCard
          icon={<Users />}
          title="Cultural"
          description="Cultural variant"
          variant="cultural"
        />
      );
      const card = container.firstChild as HTMLElement;
      expect(card).toHaveClass('bg-gradient-cultural', 'text-white');
    });
  });

  describe('Sizes', () => {
    it('should apply small size styles', () => {
      const { container } = render(
        <FeatureCard
          icon={<Users />}
          title="Small"
          description="Small size"
          size="sm"
        />
      );
      const card = container.firstChild as HTMLElement;
      expect(card).toHaveClass('p-4');
    });

    it('should apply medium size styles', () => {
      const { container } = render(
        <FeatureCard
          icon={<Users />}
          title="Medium"
          description="Medium size"
          size="md"
        />
      );
      const card = container.firstChild as HTMLElement;
      expect(card).toHaveClass('p-6');
    });

    it('should apply large size styles', () => {
      const { container } = render(
        <FeatureCard
          icon={<Users />}
          title="Large"
          description="Large size"
          size="lg"
        />
      );
      const card = container.firstChild as HTMLElement;
      expect(card).toHaveClass('p-8');
    });
  });

  describe('Interactions', () => {
    it('should call onClick when clicked and onClick is provided', () => {
      const handleClick = vi.fn();
      render(
        <FeatureCard
          icon={<Users />}
          title="Clickable"
          description="Click me"
          onClick={handleClick}
        />
      );

      const card = screen.getByText('Clickable').closest('div');
      fireEvent.click(card!);

      expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it('should have cursor-pointer when onClick is provided', () => {
      const handleClick = vi.fn();
      const { container } = render(
        <FeatureCard
          icon={<Users />}
          title="Clickable"
          description="Has onClick"
          onClick={handleClick}
        />
      );
      const card = container.firstChild as HTMLElement;
      expect(card).toHaveClass('cursor-pointer');
    });

    it('should not have cursor-pointer when onClick is not provided', () => {
      const { container } = render(
        <FeatureCard
          icon={<Users />}
          title="Not Clickable"
          description="No onClick"
        />
      );
      const card = container.firstChild as HTMLElement;
      expect(card).not.toHaveClass('cursor-pointer');
    });

    it('should have hover effect when clickable', () => {
      const handleClick = vi.fn();
      const { container } = render(
        <FeatureCard
          icon={<Users />}
          title="Hover"
          description="Has hover"
          onClick={handleClick}
        />
      );
      const card = container.firstChild as HTMLElement;
      expect(card).toHaveClass('hover:shadow-lg', 'hover:scale-105');
    });
  });

  describe('Custom className', () => {
    it('should accept and apply custom className', () => {
      const { container } = render(
        <FeatureCard
          icon={<Users />}
          title="Custom"
          description="Custom class"
          className="custom-class"
        />
      );
      const card = container.firstChild as HTMLElement;
      expect(card).toHaveClass('custom-class');
    });

    it('should merge custom className with variant styles', () => {
      const { container } = render(
        <FeatureCard
          icon={<Users />}
          title="Merged"
          description="Merged styles"
          variant="gradient"
          className="ml-2"
        />
      );
      const card = container.firstChild as HTMLElement;
      expect(card).toHaveClass('bg-gradient-to-br', 'ml-2');
    });
  });

  describe('Layout', () => {
    it('should have icon at the top', () => {
      const { container } = render(
        <FeatureCard
          icon={<Calendar data-testid="icon" />}
          title="Layout"
          description="Icon top"
        />
      );

      const icon = container.querySelector('[data-testid="icon"]');
      const title = screen.getByText('Layout');

      // Icon should appear before title in DOM
      expect(icon?.parentElement?.compareDocumentPosition(title)).toBe(
        Node.DOCUMENT_POSITION_FOLLOWING
      );
    });

    it('should have proper spacing between elements', () => {
      const { container } = render(
        <FeatureCard
          icon={<Users />}
          title="Spacing"
          description="Proper spacing"
        />
      );
      const card = container.firstChild as HTMLElement;

      // Check for flex and gap classes
      const contentWrapper = card.querySelector('.flex.flex-col');
      expect(contentWrapper).toHaveClass('gap-3');
    });
  });

  describe('Icon styling', () => {
    it('should have proper icon container styles', () => {
      const { container } = render(
        <FeatureCard
          icon={<Users />}
          title="Icon Style"
          description="Icon container"
        />
      );

      // Find the icon wrapper
      const iconWrapper = container.querySelector('.w-12.h-12');
      expect(iconWrapper).toHaveClass('rounded-full', 'flex', 'items-center', 'justify-center');
    });

    it('should apply gradient background to icon in default variant', () => {
      const { container } = render(
        <FeatureCard
          icon={<Users />}
          title="Icon BG"
          description="Default icon bg"
        />
      );

      const iconWrapper = container.querySelector('.w-12.h-12');
      expect(iconWrapper).toHaveClass('bg-gradient-to-br', 'from-saffron-400', 'to-maroon-400');
    });

    it('should apply white background to icon in gradient variant', () => {
      const { container } = render(
        <FeatureCard
          icon={<Users />}
          title="Icon BG"
          description="Gradient icon bg"
          variant="gradient"
        />
      );

      const iconWrapper = container.querySelector('.w-12.h-12');
      expect(iconWrapper).toHaveClass('bg-white');
    });
  });

  describe('Typography', () => {
    it('should have proper title styles', () => {
      render(
        <FeatureCard
          icon={<Users />}
          title="Typography"
          description="Title styles"
        />
      );

      const title = screen.getByText('Typography');
      expect(title).toHaveClass('text-lg', 'font-semibold');
    });

    it('should have proper description styles', () => {
      render(
        <FeatureCard
          icon={<Users />}
          title="Typography"
          description="Description styles"
        />
      );

      const description = screen.getByText('Description styles');
      expect(description).toHaveClass('text-sm', 'leading-relaxed');
    });

    it('should have proper stat styles when stat is provided', () => {
      render(
        <FeatureCard
          icon={<Users />}
          title="Stats"
          description="With stats"
          stat="999"
        />
      );

      const stat = screen.getByText('999');
      expect(stat).toHaveClass('text-2xl', 'font-bold');
    });
  });

  describe('Accessibility', () => {
    it('should have proper base styles for readability', () => {
      const { container } = render(
        <FeatureCard
          icon={<Users />}
          title="Accessible"
          description="Accessibility"
        />
      );
      const card = container.firstChild as HTMLElement;
      expect(card).toHaveClass('rounded-lg', 'border', 'transition-all', 'duration-200');
    });

    it('should have shadow for depth perception', () => {
      const { container } = render(
        <FeatureCard
          icon={<Users />}
          title="Shadow"
          description="Has shadow"
        />
      );
      const card = container.firstChild as HTMLElement;
      expect(card).toHaveClass('shadow-sm');
    });
  });
});
