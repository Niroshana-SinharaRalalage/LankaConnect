/**
 * LankaConnect Design Tokens
 *
 * Central source of truth for all design values used across the application.
 * All UI components MUST use these tokens - NO hardcoded values allowed.
 *
 * Based on: docs/UI_STYLE_GUIDE.md
 */

export const colors = {
  // Primary Colors
  primary: {
    blue: '#1E40AF',        // Primary brand color
    darkBlue: '#1E3A8A',    // Hover states
    lightBlue: '#3B82F6'    // Active states
  },

  // Semantic Colors
  semantic: {
    success: '#10B981',     // Green - success messages
    error: '#EF4444',       // Red - error messages
    warning: '#F59E0B',     // Amber - warnings
    info: '#3B82F6'         // Blue - informational
  },

  // Neutral Colors
  neutral: {
    gray50: '#F9FAFB',      // Lightest gray - backgrounds
    gray100: '#F3F4F6',     // Light gray - subtle backgrounds
    gray200: '#E5E7EB',     // Border color
    gray300: '#D1D5DB',     // Disabled states
    gray400: '#9CA3AF',     // Placeholder text
    gray500: '#6B7280',     // Secondary text
    gray600: '#4B5563',     // Body text
    gray700: '#374151',     // Headings
    gray800: '#1F2937',     // Dark text
    gray900: '#111827',     // Darkest - primary text
    white: '#FFFFFF',
    black: '#000000'
  }
};

export const typography = {
  // Font Families
  fontFamily: {
    primary: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif",
    mono: "'Fira Code', 'Courier New', monospace"
  },

  // Font Sizes (4px base scale)
  text: {
    xs: '0.75rem',      // 12px - Small labels, captions
    sm: '0.875rem',     // 14px - Secondary text
    base: '1rem',       // 16px - Body text (default)
    lg: '1.125rem',     // 18px - Large body text
    xl: '1.25rem',      // 20px - Small headings
    '2xl': '1.5rem',    // 24px - Section headings
    '3xl': '1.875rem',  // 30px - Page headings
    '4xl': '2.25rem',   // 36px - Hero headings
    '5xl': '3rem'       // 48px - Large display text
  },

  // Font Weights
  weight: {
    normal: 400,
    medium: 500,
    semibold: 600,
    bold: 700
  },

  // Line Heights
  leading: {
    tight: 1.25,
    normal: 1.5,
    relaxed: 1.75
  }
};

export const spacing = {
  0: '0',
  1: '0.25rem',   // 4px
  2: '0.5rem',    // 8px
  3: '0.75rem',   // 12px
  4: '1rem',      // 16px (base unit)
  5: '1.25rem',   // 20px
  6: '1.5rem',    // 24px
  8: '2rem',      // 32px
  10: '2.5rem',   // 40px
  12: '3rem',     // 48px
  16: '4rem',     // 64px
  20: '5rem',     // 80px
  24: '6rem'      // 96px
};

export const borderRadius = {
  none: '0',
  sm: '0.125rem',   // 2px
  base: '0.25rem',  // 4px
  md: '0.375rem',   // 6px
  lg: '0.5rem',     // 8px
  xl: '0.75rem',    // 12px
  '2xl': '1rem',    // 16px
  full: '9999px'    // Fully rounded (pills, circles)
};

export const shadows = {
  sm: '0 1px 2px 0 rgba(0, 0, 0, 0.05)',
  base: '0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px 0 rgba(0, 0, 0, 0.06)',
  md: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
  lg: '0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05)',
  xl: '0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)',
  none: 'none'
};

export const breakpoints = {
  sm: '640px',    // Mobile landscape
  md: '768px',    // Tablet
  lg: '1024px',   // Desktop
  xl: '1280px',   // Large desktop
  '2xl': '1536px' // Extra large desktop
};

export const transitions = {
  fast: '150ms ease-in-out',
  base: '200ms ease-in-out',
  slow: '300ms ease-in-out'
};

// Z-index scale (consistent layering)
export const zIndex = {
  dropdown: 1000,
  sticky: 1020,
  fixed: 1030,
  modalBackdrop: 1040,
  modal: 1050,
  popover: 1060,
  tooltip: 1070
};

/**
 * Usage Example:
 *
 * import { colors, spacing, typography } from '@/lib/design-tokens';
 *
 * const MyComponent = () => (
 *   <div style={{
 *     color: colors.primary.blue,
 *     padding: spacing[4],
 *     fontSize: typography.text.base
 *   }}>
 *     Content
 *   </div>
 * );
 */
