import Link from 'next/link';
import { Logo } from './Logo';
import { cn } from '@/presentation/lib/utils';

export interface OfficialLogoProps {
  size?: 'sm' | 'md' | 'lg';
  textColor?: string;
  subtitleColor?: string;
  linkTo?: string | null;
  className?: string;
}

/**
 * Official LankaConnect Logo Component
 * Complete branding with icon, title, and subtitle
 * Used consistently across all pages: landing, dashboard, profile, auth pages
 */
export function OfficialLogo({
  size = 'md',
  textColor = 'text-[#8B1538]',
  subtitleColor = 'text-gray-600',
  linkTo = '/',
  className,
}: OfficialLogoProps) {
  const sizeConfig = {
    sm: {
      logoSize: 'sm' as const,
      titleSize: 'text-lg',
      subtitleSize: 'text-[10px]',
      gap: 'ml-2',
    },
    md: {
      logoSize: 'md' as const,
      titleSize: 'text-2xl',
      subtitleSize: 'text-xs',
      gap: 'ml-3',
    },
    lg: {
      logoSize: 'lg' as const,
      titleSize: 'text-3xl',
      subtitleSize: 'text-sm',
      gap: 'ml-4',
    },
  };

  const config = sizeConfig[size];

  const logoContent = (
    <div className={cn('flex items-center', className)}>
      <Logo size={config.logoSize} showText={false} />
      <div className={config.gap}>
        <div className={cn(config.titleSize, textColor)}>LankaConnect</div>
        <div className={cn(config.subtitleSize, subtitleColor, '-mt-1')}>
          Sri Lankan Community Hub
        </div>
      </div>
    </div>
  );

  if (linkTo) {
    return (
      <Link href={linkTo} className="hover:opacity-90 transition-opacity">
        {logoContent}
      </Link>
    );
  }

  return logoContent;
}
