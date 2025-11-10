import * as React from 'react';
import { Star, Store } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { cn } from '@/presentation/lib/utils';

export interface Business {
  id: string;
  name: string;
  category: string;
  rating: number;
  reviewCount: number;
  imageUrl?: string;
}

export interface FeaturedBusinessesProps extends React.HTMLAttributes<HTMLDivElement> {
  businesses: Business[];
  onBusinessClick?: (businessId: string) => void;
  limit?: number;
}

const renderStars = (rating: number) => {
  const stars = [];
  const fullStars = Math.floor(rating);

  for (let i = 0; i < 5; i++) {
    stars.push(
      <Star
        key={i}
        className={cn(
          'h-4 w-4',
          i < fullStars ? 'text-yellow-400 fill-yellow-400' : 'text-gray-300'
        )}
        data-icon="star"
      />
    );
  }

  return stars;
};

/**
 * FeaturedBusinesses Component
 * Displays featured businesses with ratings, reviews, and categories
 * Supports optional click interactions and display limits
 */
export const FeaturedBusinesses = React.forwardRef<HTMLDivElement, FeaturedBusinessesProps>(
  ({ className, businesses, onBusinessClick, limit, ...props }, ref) => {
    const displayedBusinesses = React.useMemo(() => {
      return limit ? businesses.slice(0, limit) : businesses;
    }, [businesses, limit]);

    const handleClick = (businessId: string) => {
      if (onBusinessClick) {
        onBusinessClick(businessId);
      }
    };

    const handleKeyPress = (event: React.KeyboardEvent, businessId: string) => {
      if (event.key === 'Enter' && onBusinessClick) {
        onBusinessClick(businessId);
      }
    };

    const getInitials = (name: string) => {
      return name
        .split(' ')
        .map(word => word[0])
        .join('')
        .toUpperCase()
        .slice(0, 2);
    };

    return (
      <div ref={ref} className={cn('', className)} {...props}>
        <div
          className="rounded-xl overflow-hidden"
          style={{
            background: 'white',
            boxShadow: '0 4px 6px rgba(0, 0, 0, 0.05)'
          }}
        >
          {/* Widget Header */}
          <div
            className="px-5 py-4 font-semibold border-b"
            style={{
              background: 'linear-gradient(135deg, rgba(255,121,0,0.1) 0%, rgba(139,21,56,0.1) 100%)',
              borderBottom: '1px solid #e2e8f0',
              color: '#8B1538'
            }}
          >
            ⭐ Featured Businesses
          </div>

          {/* Widget Content */}
          <div className="p-5">
            {displayedBusinesses.length === 0 ? (
              <p className="text-sm text-center py-4" style={{ color: '#718096' }}>
                No featured businesses available
              </p>
            ) : (
              <div className="space-y-0">
                {displayedBusinesses.map((business, index) => (
                  <div
                    key={business.id}
                    role={onBusinessClick ? 'button' : undefined}
                    tabIndex={onBusinessClick ? 0 : undefined}
                    onClick={() => onBusinessClick && handleClick(business.id)}
                    onKeyPress={(e) => handleKeyPress(e, business.id)}
                    className={cn(
                      'flex items-center py-3 transition-all',
                      onBusinessClick && 'cursor-pointer'
                    )}
                    style={{
                      borderBottom: index === displayedBusinesses.length - 1 ? 'none' : '1px solid #f1f5f9'
                    }}
                  >
                    {/* Business Logo */}
                    <div
                      className="rounded-lg flex items-center justify-center text-white font-bold flex-shrink-0 mr-3"
                      style={{
                        background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)',
                        width: '40px',
                        height: '40px',
                        fontSize: '0.8rem'
                      }}
                    >
                      {getInitials(business.name)}
                    </div>

                    {/* Business Info */}
                    <div className="flex-1 min-w-0">
                      <h4
                        className="font-medium truncate"
                        style={{ fontSize: '0.9rem', marginBottom: '0.25rem', color: '#2d3748' }}
                      >
                        {business.name}
                      </h4>
                      <p style={{ fontSize: '0.8rem', color: '#718096' }}>
                        {business.category}
                      </p>
                    </div>

                    {/* Rating */}
                    <div
                      className="ml-auto text-sm font-medium"
                      style={{ color: '#FFD700' }}
                    >
                      ⭐ {business.rating.toFixed(1)}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    );
  }
);

FeaturedBusinesses.displayName = 'FeaturedBusinesses';
