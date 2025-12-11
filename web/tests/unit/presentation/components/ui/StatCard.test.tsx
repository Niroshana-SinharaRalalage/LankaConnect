import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { StatCard } from '@/presentation/components/ui/StatCard';

describe('StatCard Component', () => {
  describe('rendering', () => {
    it('should render stat card with title and value', () => {
      render(<StatCard title="Total Members" value="12,500+" />);

      expect(screen.getByText('Total Members')).toBeInTheDocument();
      expect(screen.getByText('12,500+')).toBeInTheDocument();
    });

    it('should render with icon when provided', () => {
      const TestIcon = () => <svg data-testid="test-icon" />;
      render(<StatCard title="Total Events" value="450+" icon={<TestIcon />} />);

      expect(screen.getByTestId('test-icon')).toBeInTheDocument();
    });

    it('should render with subtitle when provided', () => {
      render(
        <StatCard
          title="Total Members"
          value="12,500+"
          subtitle="Active community members"
        />
      );

      expect(screen.getByText('Active community members')).toBeInTheDocument();
    });

    it('should render with trend indicator when provided', () => {
      render(
        <StatCard
          title="Total Members"
          value="12,500+"
          trend={{ value: '+12%', direction: 'up' }}
        />
      );

      expect(screen.getByText('+12%')).toBeInTheDocument();
    });

    it('should render with change description when provided', () => {
      render(
        <StatCard
          title="Total Members"
          value="12,500+"
          change="from last month"
        />
      );

      expect(screen.getByText('from last month')).toBeInTheDocument();
    });
  });

  describe('variants', () => {
    it('should render with default variant', () => {
      const { container } = render(<StatCard title="Total Members" value="12,500+" />);

      const card = container.firstChild as HTMLElement;
      expect(card).toHaveClass('bg-white');
    });

    it('should render with primary variant (gradient)', () => {
      const { container } = render(<StatCard title="Total Members" value="12,500+" variant="primary" />);

      const card = container.firstChild as HTMLElement;
      expect(card).toHaveClass('bg-gradient-to-br');
    });

    it('should render with secondary variant', () => {
      const { container } = render(<StatCard title="Total Members" value="12,500+" variant="secondary" />);

      const card = container.firstChild as HTMLElement;
      expect(card).toHaveClass('bg-secondary');
    });
  });

  describe('sizes', () => {
    it('should render with default (md) size', () => {
      render(<StatCard title="Total Members" value="12,500+" />);

      const value = screen.getByText('12,500+');
      expect(value).toHaveClass('text-2xl');
    });

    it('should render with sm size', () => {
      render(<StatCard title="Total Members" value="12,500+" size="sm" />);

      const value = screen.getByText('12,500+');
      expect(value).toHaveClass('text-lg');
    });

    it('should render with lg size', () => {
      render(<StatCard title="Total Members" value="12,500+" size="lg" />);

      const value = screen.getByText('12,500+');
      expect(value).toHaveClass('text-3xl');
    });
  });

  describe('trend indicators', () => {
    it('should show up trend with positive styling', () => {
      render(
        <StatCard
          title="Members"
          value="12,500+"
          trend={{ value: '+12%', direction: 'up' }}
        />
      );

      const trendElement = screen.getByText('+12%');
      expect(trendElement).toHaveClass('text-green-600');
    });

    it('should show down trend with negative styling', () => {
      render(
        <StatCard
          title="Members"
          value="12,500+"
          trend={{ value: '-5%', direction: 'down' }}
        />
      );

      const trendElement = screen.getByText('-5%');
      expect(trendElement).toHaveClass('text-red-600');
    });

    it('should show neutral trend', () => {
      render(
        <StatCard
          title="Members"
          value="12,500+"
          trend={{ value: '0%', direction: 'neutral' }}
        />
      );

      const trendElement = screen.getByText('0%');
      expect(trendElement).toHaveClass('text-gray-600');
    });
  });

  describe('custom styling', () => {
    it('should accept custom className', () => {
      const { container } = render(<StatCard title="Members" value="12,500+" className="custom-class" />);

      const card = container.firstChild as HTMLElement;
      expect(card).toHaveClass('custom-class');
    });
  });

  describe('accessibility', () => {
    it('should have proper semantic structure', () => {
      render(<StatCard title="Total Members" value="12,500+" />);

      expect(screen.getByText('Total Members')).toBeInTheDocument();
      expect(screen.getByText('12,500+')).toBeInTheDocument();
    });

    it('should support aria-label', () => {
      render(
        <StatCard
          title="Total Members"
          value="12,500+"
          aria-label="Community statistics showing 12,500 total members"
        />
      );

      const card = screen.getByLabelText(/community statistics/i);
      expect(card).toBeInTheDocument();
    });
  });
});
