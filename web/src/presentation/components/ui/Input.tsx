import * as React from 'react';
import { cn } from '@/presentation/lib/utils';

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  error?: boolean;
}

/**
 * Input Component
 * Reusable input component with error states and accessibility
 * Follows UI/UX best practices
 *
 * GitHub Issue #19 Fix: Number inputs prevent scroll from changing values
 * When a number input is focused and user scrolls, the page scrolls normally
 * instead of changing the input value.
 */
const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, type = 'text', error, onWheel, ...props }, ref) => {
    // GitHub Issue #19: Prevent scroll wheel from changing number input values
    // This is a common UX issue where scrolling while focused on a number input
    // accidentally changes the value instead of scrolling the page
    const handleWheel = React.useCallback(
      (e: React.WheelEvent<HTMLInputElement>) => {
        if (type === 'number') {
          // Blur the input to prevent scroll from changing the value
          // The page will scroll normally after blur
          e.currentTarget.blur();
        }
        // Call any existing onWheel handler
        onWheel?.(e);
      },
      [type, onWheel]
    );

    return (
      <input
        type={type}
        className={cn(
          'flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50',
          error && 'border-destructive focus-visible:ring-destructive',
          className
        )}
        ref={ref}
        onWheel={handleWheel}
        aria-invalid={error ? 'true' : undefined}
        {...props}
      />
    );
  }
);

Input.displayName = 'Input';

export { Input };
