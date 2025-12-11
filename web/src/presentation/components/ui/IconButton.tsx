import * as React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '@/presentation/lib/utils';

const iconButtonVariants = cva(
  'flex flex-col items-center justify-center gap-2 rounded-lg transition-all duration-200 hover:shadow-lg hover:scale-105 disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:scale-100',
  {
    variants: {
      variant: {
        default: 'bg-white text-gray-700 border border-gray-200 hover:border-saffron-300',
        primary: 'bg-gradient-to-br from-saffron-500 to-maroon-500 text-white shadow-md',
        secondary: 'bg-gray-100 text-gray-700 hover:bg-gray-200',
      },
      size: {
        sm: 'p-3 min-w-[100px]',
        md: 'p-4 min-w-[120px]',
        lg: 'p-5 min-w-[140px]',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'md',
    },
  }
);

const iconWrapperVariants = cva('transition-transform', {
  variants: {
    size: {
      sm: 'w-5 h-5',
      md: 'w-6 h-6',
      lg: 'w-7 h-7',
    },
  },
  defaultVariants: {
    size: 'md',
  },
});

const labelVariants = cva('text-xs font-medium mt-1', {
  variants: {
    size: {
      sm: 'text-xs',
      md: 'text-xs',
      lg: 'text-sm',
    },
  },
  defaultVariants: {
    size: 'md',
  },
});

export interface IconButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof iconButtonVariants> {
  /** Icon element (from lucide-react or custom) */
  icon: React.ReactNode;
  /** Label text displayed below icon */
  label: string;
}

/**
 * IconButton Component
 *
 * Vertical button with icon on top and label below.
 * Used for quick action bars and feature highlights.
 *
 * @example
 * ```tsx
 * <IconButton
 *   icon={<Calendar />}
 *   label="New Event"
 *   variant="primary"
 *   onClick={() => console.log('clicked')}
 * />
 * ```
 *
 * Variants:
 * - default: White background with gray text (standard action)
 * - primary: Gradient background with white text (primary action)
 * - secondary: Gray background (secondary action)
 *
 * Sizes:
 * - sm: Small padding and icon (100px min-width)
 * - md: Medium padding and icon (120px min-width)
 * - lg: Large padding and icon (140px min-width)
 *
 * Phase: 6C.1 - Landing Page Redesign
 */
const IconButton = React.forwardRef<HTMLButtonElement, IconButtonProps>(
  ({ className, variant, size, icon, label, disabled, ...props }, ref) => {
    return (
      <button
        ref={ref}
        className={cn(iconButtonVariants({ variant, size }), className)}
        disabled={disabled}
        {...props}
      >
        <div className={cn(iconWrapperVariants({ size }))}>{icon}</div>
        <span className={cn(labelVariants({ size }))}>{label}</span>
      </button>
    );
  }
);

IconButton.displayName = 'IconButton';

export { IconButton, iconButtonVariants };
