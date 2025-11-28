import * as React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '@/presentation/lib/utils';

const featureCardVariants = cva(
  'rounded-lg border transition-all duration-200 shadow-sm',
  {
    variants: {
      variant: {
        default: 'bg-white border-gray-200 text-gray-900',
        gradient:
          'bg-gradient-to-br from-purple-600 to-purple-800 text-white border-purple-700',
        cultural: 'bg-gradient-cultural text-white border-transparent',
      },
      size: {
        sm: 'p-4',
        md: 'p-6',
        lg: 'p-8',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'md',
    },
  }
);

const iconWrapperVariants = cva(
  'w-12 h-12 rounded-full flex items-center justify-center',
  {
    variants: {
      variant: {
        default: 'bg-gradient-to-br from-saffron-400 to-maroon-400 text-white',
        gradient: 'bg-white text-purple-600',
        cultural: 'bg-white/20 text-white',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  }
);

export interface FeatureCardProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof featureCardVariants> {
  /** Icon element (from lucide-react or custom) */
  icon: React.ReactNode;
  /** Card title */
  title: string;
  /** Card description */
  description: string;
  /** Optional stat number to display */
  stat?: string;
  /** Optional click handler */
  onClick?: () => void;
}

/**
 * FeatureCard Component
 *
 * Display feature highlights with icon, title, description, and optional stat.
 * Used for showcasing key features on landing pages and dashboards.
 *
 * @example
 * ```tsx
 * <FeatureCard
 *   icon={<Users />}
 *   title="Active Members"
 *   description="Connect with Sri Lankans worldwide"
 *   stat="1,234"
 *   variant="gradient"
 *   onClick={() => router.push('/members')}
 * />
 * ```
 *
 * Variants:
 * - default: White background with gradient icon (standard feature)
 * - gradient: Purple gradient background (featured highlight)
 * - cultural: Cultural gradient background (cultural features)
 *
 * Sizes:
 * - sm: Small padding (compact layout)
 * - md: Medium padding (standard layout)
 * - lg: Large padding (spacious layout)
 *
 * Features:
 * - Hover effects when clickable
 * - Gradient icon backgrounds
 * - Optional stat display
 * - Responsive sizing
 *
 * Phase: 6C.1 - Landing Page Redesign
 */
const FeatureCard = React.forwardRef<HTMLDivElement, FeatureCardProps>(
  (
    {
      className,
      variant,
      size,
      icon,
      title,
      description,
      stat,
      onClick,
      ...props
    },
    ref
  ) => {
    const isClickable = !!onClick;

    return (
      <div
        ref={ref}
        className={cn(
          featureCardVariants({ variant, size }),
          isClickable && 'cursor-pointer hover:shadow-lg hover:scale-105',
          className
        )}
        onClick={onClick}
        {...props}
      >
        <div className="flex flex-col gap-3">
          {/* Icon */}
          <div className={cn(iconWrapperVariants({ variant }))}>{icon}</div>

          {/* Content */}
          <div className="flex flex-col gap-2">
            <h3
              className={cn(
                'text-lg font-semibold',
                variant === 'default' ? 'text-gray-900' : 'text-white'
              )}
            >
              {title}
            </h3>

            <p
              className={cn(
                'text-sm leading-relaxed',
                variant === 'default' ? 'text-gray-600' : 'text-white/90'
              )}
            >
              {description}
            </p>

            {/* Optional Stat */}
            {stat && (
              <p
                className={cn(
                  'text-2xl font-bold mt-2',
                  variant === 'default' ? 'text-saffron-600' : 'text-white'
                )}
              >
                {stat}
              </p>
            )}
          </div>
        </div>
      </div>
    );
  }
);

FeatureCard.displayName = 'FeatureCard';

export { FeatureCard, featureCardVariants };
