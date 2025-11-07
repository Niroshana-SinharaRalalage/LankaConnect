import Image from 'next/image';
import { cn } from '@/presentation/lib/utils';

export interface LogoProps {
  size?: 'sm' | 'md' | 'lg' | 'xl';
  showText?: boolean;
  className?: string;
}

const sizeClasses = {
  sm: 'h-8 w-8',
  md: 'h-12 w-12',
  lg: 'h-16 w-16',
  xl: 'h-20 w-20',
};

const textSizeClasses = {
  sm: 'text-lg',
  md: 'text-xl',
  lg: 'text-2xl',
  xl: 'text-3xl',
};

/**
 * Logo Component
 * Displays the LankaConnect logo with optional text
 */
export function Logo({ size = 'md', showText = false, className }: LogoProps) {
  return (
    <div className={cn('flex items-center gap-3', className)}>
      <Image
        src="/logos/lankaconnect-logo-transparent.png"
        alt="LankaConnect"
        width={size === 'sm' ? 32 : size === 'md' ? 48 : size === 'lg' ? 64 : 80}
        height={size === 'sm' ? 32 : size === 'md' ? 48 : size === 'lg' ? 64 : 80}
        className={cn(sizeClasses[size], 'object-contain')}
        priority
      />
      {showText && (
        <span className={cn('font-bold text-primary', textSizeClasses[size])}>
          LankaConnect
        </span>
      )}
    </div>
  );
}
