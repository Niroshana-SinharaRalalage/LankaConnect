'use client';

import { useEffect } from 'react';

/**
 * NumberInputScrollPrevention Component
 * GitHub Issue #20: Prevents scroll wheel from changing values in number inputs
 *
 * Problem: When a user scrolls the page while a number input has focus,
 * the browser's default behavior changes the input value based on scroll direction.
 * This causes unexpected value changes and poor UX.
 *
 * Solution: This component adds a global event listener that blurs any focused
 * number input when a wheel event occurs, preventing the value change.
 *
 * Usage: Add this component once in the app's Providers or layout.
 *
 * Note: CSS in globals.css (Issue #19) hides the visual spinner arrows,
 * but this component handles the scroll behavior that CSS cannot prevent.
 */
export function NumberInputScrollPrevention() {
  useEffect(() => {
    /**
     * Handle wheel events on number inputs
     * Blurs the input to prevent scroll-to-change behavior
     */
    const handleWheel = (event: WheelEvent) => {
      const target = event.target as HTMLElement;

      // Check if the target is a focused number input
      if (
        target.tagName === 'INPUT' &&
        (target as HTMLInputElement).type === 'number' &&
        document.activeElement === target
      ) {
        // Blur the input to prevent value change
        // This is safer than preventDefault() which could block legitimate page scrolling
        (target as HTMLInputElement).blur();
      }
    };

    // Add event listener with passive: false to allow preventDefault if needed
    // Using 'wheel' event which is the modern standard (replaces mousewheel)
    document.addEventListener('wheel', handleWheel, { passive: true });

    // Cleanup on unmount
    return () => {
      document.removeEventListener('wheel', handleWheel);
    };
  }, []);

  // This component doesn't render anything
  return null;
}
