# Logo Text Alignment Fix

**Date**: 2026-02-12
**Issue**: "LankaConnect" and "Sri Lankan Community Hub" text in the logo had different widths, causing misalignment in the UI.

## Problem

In the web application, the logo displayed two lines of text:
- **Line 1**: "LankaConnect" (12 characters)
- **Line 2**: "Sri Lankan Community Hub" (27 characters with spaces)

The subtitle was visually shorter than the title, creating an unbalanced appearance. The email templates already had proper alignment with letter-spacing applied.

## Solution

Added CSS letter-spacing to the subtitle ("Sri Lankan Community Hub") to match the width of the title ("LankaConnect").

### Changes Made

**File**: `web/src/presentation/components/atoms/OfficialLogo.tsx`

Added `subtitleLetterSpacing` property to the `sizeConfig` object with different values for each size:

```typescript
const sizeConfig = {
  sm: {
    logoSize: 'sm' as const,
    titleSize: 'text-lg',
    subtitleSize: 'text-[10px]',
    subtitleLetterSpacing: 'tracking-[0.08em]',  // ← Added
    gap: 'ml-2',
  },
  md: {
    logoSize: 'md' as const,
    titleSize: 'text-2xl',
    subtitleSize: 'text-xs',
    subtitleLetterSpacing: 'tracking-[0.15em]',  // ← Added
    gap: 'ml-3',
  },
  lg: {
    logoSize: 'lg' as const,
    titleSize: 'text-3xl',
    subtitleSize: 'text-sm',
    subtitleLetterSpacing: 'tracking-[0.12em]',  // ← Added
    gap: 'ml-4',
  },
};
```

Applied the letter-spacing to the subtitle div:

```tsx
<div className={cn(config.subtitleSize, config.subtitleLetterSpacing, subtitleColor, '-mt-1')}>
  Sri Lankan Community Hub
</div>
```

## Letter-Spacing Values

- **Small (sm)**: `tracking-[0.08em]` - For compact layouts
- **Medium (md)**: `tracking-[0.15em]` - Default size, most commonly used
- **Large (lg)**: `tracking-[0.12em]` - For hero sections and large displays

These values were calculated to visually align the subtitle width with the title width across different font sizes.

## Impact

### Files Automatically Fixed (11 locations)

The `OfficialLogo` component is used in the following pages, all of which now display the aligned logo:

1. `web/src/app/(auth)/reset-password/page.tsx`
2. `web/src/app/(auth)/forgot-password/page.tsx`
3. `web/src/presentation/components/layout/Header.tsx`
4. `web/src/app/newsletter/unsubscribe/page.tsx`
5. `web/src/app/newsletter/confirm/page.tsx`
6. `web/src/app/(dashboard)/dashboard/page.tsx`
7. `web/src/app/(dashboard)/profile/page.tsx`
8. `web/src/app/(auth)/verify-email/page.tsx`
9. `web/src/app/(auth)/login/page.tsx`
10. `web/src/app/(auth)/register/page.tsx`
11. `web/src/presentation/components/atoms/OfficialLogo.tsx` (component itself)

### Email Templates

No changes were made to email templates. The email templates already have proper letter-spacing and the user confirmed they are satisfied with the current alignment.

## Testing

- ✅ Build completed successfully (`npm run build`)
- ✅ No TypeScript errors
- ✅ All 29 pages generated without errors
- ✅ Logo alignment now matches email template style

## Before vs After

### Before
```
LankaConnect
Sri Lankan Community Hub
^^^^^^^^^^^^^           ← Subtitle shorter than title
```

### After
```
LankaConnect
Sri Lankan Community Hub
^^^^^^^^^^^^^^^^^^^^^^^^^ ← Subtitle matches title width
```

## Notes

- The Tailwind CSS `tracking-[value]` utility maps to CSS `letter-spacing`
- `tracking-[0.15em]` means letter-spacing of 0.15× the current font size
- Values were chosen empirically to achieve visual alignment across different sizes
- The component is responsive and works across all breakpoints (mobile, tablet, desktop)
