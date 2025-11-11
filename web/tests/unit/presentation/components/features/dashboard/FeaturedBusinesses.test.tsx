import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { FeaturedBusinesses } from '@/presentation/components/features/dashboard/FeaturedBusinesses';

describe('FeaturedBusinesses Component', () => {
  const mockBusinesses = [
    {
      id: '1',
      name: 'Ceylon Spice House',
      category: 'Restaurant',
      rating: 4.5,
      reviewCount: 128,
      imageUrl: '/images/business1.jpg',
    },
    {
      id: '2',
      name: 'Lanka Tech Solutions',
      category: 'Technology',
      rating: 5.0,
      reviewCount: 89,
      imageUrl: '/images/business2.jpg',
    },
    {
      id: '3',
      name: 'Tropical Travels',
      category: 'Travel Agency',
      rating: 4.8,
      reviewCount: 234,
      imageUrl: '/images/business3.jpg',
    },
  ];

  describe('rendering', () => {
    it('should render featured businesses with title', () => {
      render(<FeaturedBusinesses businesses={mockBusinesses} />);

      expect(screen.getByText('Featured Businesses')).toBeInTheDocument();
    });

    it('should render list of businesses with names', () => {
      render(<FeaturedBusinesses businesses={mockBusinesses} />);

      expect(screen.getByText('Ceylon Spice House')).toBeInTheDocument();
      expect(screen.getByText('Lanka Tech Solutions')).toBeInTheDocument();
      expect(screen.getByText('Tropical Travels')).toBeInTheDocument();
    });

    it('should render business categories', () => {
      render(<FeaturedBusinesses businesses={mockBusinesses} />);

      expect(screen.getByText('Restaurant')).toBeInTheDocument();
      expect(screen.getByText('Technology')).toBeInTheDocument();
      expect(screen.getByText('Travel Agency')).toBeInTheDocument();
    });

    it('should render star ratings', () => {
      render(<FeaturedBusinesses businesses={mockBusinesses} />);

      expect(screen.getByText('4.5')).toBeInTheDocument();
      expect(screen.getByText('5.0')).toBeInTheDocument();
      expect(screen.getByText('4.8')).toBeInTheDocument();
    });

    it('should render review counts', () => {
      render(<FeaturedBusinesses businesses={mockBusinesses} />);

      expect(screen.getByText('(128 reviews)')).toBeInTheDocument();
      expect(screen.getByText('(89 reviews)')).toBeInTheDocument();
      expect(screen.getByText('(234 reviews)')).toBeInTheDocument();
    });

    it('should render star icon for each business', () => {
      const { container } = render(<FeaturedBusinesses businesses={mockBusinesses} />);

      const starIcons = container.querySelectorAll('[data-icon="star"]');
      expect(starIcons.length).toBeGreaterThanOrEqual(3);
    });
  });

  describe('empty state', () => {
    it('should render message when no businesses provided', () => {
      render(<FeaturedBusinesses businesses={[]} />);

      expect(screen.getByText(/no featured businesses/i)).toBeInTheDocument();
    });

    it('should render title even when no businesses', () => {
      render(<FeaturedBusinesses businesses={[]} />);

      expect(screen.getByText('Featured Businesses')).toBeInTheDocument();
    });
  });

  describe('business cards', () => {
    it('should use Card component structure', () => {
      const { container } = render(<FeaturedBusinesses businesses={mockBusinesses} />);

      const cards = container.querySelectorAll('.rounded-lg.border.bg-card');
      expect(cards.length).toBeGreaterThan(0);
    });

    it('should display business images', () => {
      render(<FeaturedBusinesses businesses={mockBusinesses} />);

      const images = screen.getAllByRole('img');
      expect(images).toHaveLength(3);
      expect(images[0]).toHaveAttribute('alt', 'Ceylon Spice House');
      expect(images[1]).toHaveAttribute('alt', 'Lanka Tech Solutions');
      expect(images[2]).toHaveAttribute('alt', 'Tropical Travels');
    });

    it('should handle missing images gracefully', () => {
      const businessesWithoutImages = [
        {
          id: '1',
          name: 'Test Business',
          category: 'Service',
          rating: 4.0,
          reviewCount: 10,
        },
      ];

      render(<FeaturedBusinesses businesses={businessesWithoutImages} />);

      expect(screen.getByText('Test Business')).toBeInTheDocument();
    });
  });

  describe('interaction', () => {
    it('should call onClick when business card is clicked', () => {
      const handleClick = vi.fn();
      render(<FeaturedBusinesses businesses={mockBusinesses} onBusinessClick={handleClick} />);

      const businessCard = screen.getByText('Ceylon Spice House').closest('div[role="button"]');
      fireEvent.click(businessCard!);

      expect(handleClick).toHaveBeenCalledWith('1');
    });

    it('should not call onClick when onBusinessClick is not provided', () => {
      render(<FeaturedBusinesses businesses={mockBusinesses} />);

      const businessCard = screen.getByText('Ceylon Spice House').closest('div');
      expect(businessCard).toBeInTheDocument();
    });

    it('should have hover effect on clickable cards', () => {
      const handleClick = vi.fn();
      render(<FeaturedBusinesses businesses={mockBusinesses} onBusinessClick={handleClick} />);

      const businessCard = screen.getByText('Ceylon Spice House').closest('div[role="button"]');
      expect(businessCard).toHaveClass('cursor-pointer');
    });
  });

  describe('rating display', () => {
    it('should display rating with one decimal place', () => {
      const businesses = [
        {
          id: '1',
          name: 'Test Business',
          category: 'Service',
          rating: 4,
          reviewCount: 50,
        },
      ];

      render(<FeaturedBusinesses businesses={businesses} />);

      expect(screen.getByText('4.0')).toBeInTheDocument();
    });

    it('should show filled stars based on rating', () => {
      const businesses = [
        {
          id: '1',
          name: 'Perfect Rating',
          category: 'Service',
          rating: 5.0,
          reviewCount: 100,
        },
      ];

      const { container } = render(<FeaturedBusinesses businesses={businesses} />);

      const filledStars = container.querySelectorAll('.text-yellow-400');
      expect(filledStars).toHaveLength(5);
    });
  });

  describe('styling', () => {
    it('should accept custom className', () => {
      const { container } = render(
        <FeaturedBusinesses businesses={mockBusinesses} className="custom-class" />
      );

      const wrapper = container.firstChild as HTMLElement;
      expect(wrapper).toHaveClass('custom-class');
    });

    it('should display businesses in grid layout', () => {
      const { container } = render(<FeaturedBusinesses businesses={mockBusinesses} />);

      const grid = container.querySelector('.grid');
      expect(grid).toBeInTheDocument();
    });
  });

  describe('accessibility', () => {
    it('should have proper semantic structure', () => {
      render(<FeaturedBusinesses businesses={mockBusinesses} />);

      expect(screen.getByText('Featured Businesses')).toBeInTheDocument();
    });

    it('should support aria-label', () => {
      render(
        <FeaturedBusinesses
          businesses={mockBusinesses}
          aria-label="Featured local businesses"
        />
      );

      const section = screen.getByLabelText(/featured local businesses/i);
      expect(section).toBeInTheDocument();
    });

    it('should have clickable cards with proper role', () => {
      const handleClick = vi.fn();
      render(<FeaturedBusinesses businesses={mockBusinesses} onBusinessClick={handleClick} />);

      const buttons = screen.getAllByRole('button');
      expect(buttons.length).toBeGreaterThan(0);
    });

    it('should have keyboard navigation support', () => {
      const handleClick = vi.fn();
      render(<FeaturedBusinesses businesses={mockBusinesses} onBusinessClick={handleClick} />);

      const businessCard = screen.getByText('Ceylon Spice House').closest('div[role="button"]');
      fireEvent.keyPress(businessCard!, { key: 'Enter', code: 13, charCode: 13 });

      expect(handleClick).toHaveBeenCalled();
    });
  });

  describe('limit display', () => {
    it('should display all businesses when count is less than limit', () => {
      render(<FeaturedBusinesses businesses={mockBusinesses} limit={5} />);

      expect(screen.getByText('Ceylon Spice House')).toBeInTheDocument();
      expect(screen.getByText('Lanka Tech Solutions')).toBeInTheDocument();
      expect(screen.getByText('Tropical Travels')).toBeInTheDocument();
    });

    it('should limit displayed businesses when limit is provided', () => {
      render(<FeaturedBusinesses businesses={mockBusinesses} limit={2} />);

      expect(screen.getByText('Ceylon Spice House')).toBeInTheDocument();
      expect(screen.getByText('Lanka Tech Solutions')).toBeInTheDocument();
      expect(screen.queryByText('Tropical Travels')).not.toBeInTheDocument();
    });
  });
});
