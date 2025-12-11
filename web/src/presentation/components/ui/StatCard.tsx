import * as React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '@/presentation/lib/utils';
import { TrendingUp, TrendingDown, Minus } from 'lucide-react';

const statCardVariants = cva(
  'rounded-lg shadow-sm p-3 transition-all hover:shadow-md',
  {
    variants: {
      variant: {
        default: 'bg-white border border-gray-200',
        primary: 'bg-gradient-to-br from-purple-600 to-purple-800 text-white',
        secondary: 'bg-secondary text-secondary-foreground',
      },
      size: {
        sm: '',
        md: '',
        lg: '',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'md',
    },
  }
);

const valueVariants = cva('font-bold', {
  variants: {
    size: {
      sm: 'text-lg',
      md: 'text-2xl',
      lg: 'text-3xl',
    },
  },
  defaultVariants: {
    size: 'md',
  },
});

export interface TrendIndicator {
  value: string;
  direction: 'up' | 'down' | 'neutral';
}

export interface StatCardProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof statCardVariants> {
  title: string;
  value: string;
  subtitle?: string;
  icon?: React.ReactNode;
  trend?: TrendIndicator;
  change?: string;
}

/**
 * StatCard Component
 * Display statistical information with optional icon, trend, and subtitle
 * Used for showing key metrics on landing pages and dashboards
 */
export const StatCard = React.forwardRef<HTMLDivElement, StatCardProps>(
  (
    {
      className,
      variant,
      size,
      title,
      value,
      subtitle,
      icon,
      trend,
      change,
      ...props
    },
    ref
  ) => {
    const getTrendColor = (direction: TrendIndicator['direction']) => {
      switch (direction) {
        case 'up':
          return 'text-green-600';
        case 'down':
          return 'text-red-600';
        case 'neutral':
          return 'text-gray-600';
        default:
          return 'text-gray-600';
      }
    };

    const getTrendIcon = (direction: TrendIndicator['direction']) => {
      switch (direction) {
        case 'up':
          return <TrendingUp className="h-4 w-4" />;
        case 'down':
          return <TrendingDown className="h-4 w-4" />;
        case 'neutral':
          return <Minus className="h-4 w-4" />;
        default:
          return null;
      }
    };

    return (
      <div
        ref={ref}
        className={cn(statCardVariants({ variant, size }), className)}
        {...props}
      >
        <div className="flex items-start justify-between">
          <div className="flex-1">
            <p
              className={cn(
                'text-xs font-medium',
                variant === 'primary' ? 'text-purple-100' : 'text-gray-600'
              )}
            >
              {title}
            </p>
            <div className="mt-0.5 flex items-baseline gap-2">
              <p className={cn(valueVariants({ size }))}>{value}</p>
              {trend && (
                <span className="flex items-center gap-1">
                  {getTrendIcon(trend.direction)}
                  <span
                    className={cn(
                      'text-sm font-medium',
                      variant === 'primary' ? 'text-purple-100' : getTrendColor(trend.direction)
                    )}
                  >
                    {trend.value}
                  </span>
                </span>
              )}
            </div>
            {(subtitle || change) && (
              <p
                className={cn(
                  'mt-1 text-sm',
                  variant === 'primary' ? 'text-purple-100' : 'text-gray-500'
                )}
              >
                {subtitle || change}
              </p>
            )}
          </div>
          {icon && (
            <div
              className={cn(
                'ml-4 rounded-full p-3',
                variant === 'primary'
                  ? 'bg-purple-700/30'
                  : 'bg-purple-100 text-purple-600'
              )}
            >
              {icon}
            </div>
          )}
        </div>
      </div>
    );
  }
);

StatCard.displayName = 'StatCard';
