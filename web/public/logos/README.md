# Logo Assets

This directory contains the LankaConnect logo assets.

## Files

- `lankaconnect-logo-transparent.png` - Main logo with transparent background
  - Source: C:\Work\LankaConnect\mockup\logos\lankaconnect-logo-transparent.png
  - Features: Lotus pattern with Sri Lankan lion
  - Usage: Used throughout the web application

## Usage

Import the logo in components using Next.js Image:

```typescript
import Image from 'next/image';

<Image
  src="/logos/lankaconnect-logo-transparent.png"
  alt="LankaConnect"
  width={64}
  height={64}
/>
```

Or use the Logo component:

```typescript
import { Logo } from '@/presentation/components/atoms/Logo';

<Logo size="md" showText />
```
