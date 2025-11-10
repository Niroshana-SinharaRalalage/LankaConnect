import Image from 'next/image';
import { cn } from '@/presentation/lib/utils';

export interface LogoProps {
  size?: 'sm' | 'md' | 'lg' | 'xl';
  showText?: boolean;
  className?: string;
}

const sizeClasses = {
  sm: 'h-10 w-10',
  md: 'h-16 w-16',
  lg: 'h-20 w-20',
  xl: 'h-24 w-24',
};

const textSizeClasses = {
  sm: 'text-lg',
  md: 'text-xl',
  lg: 'text-2xl',
  xl: 'text-3xl',
};

const imageSizes = {
  sm: 40,
  md: 64,
  lg: 80,
  xl: 96,
};

/**
 * Logo Component
 * Displays the LankaConnect logo with optional text
 */
export function Logo({ size = 'md', showText = false, className }: LogoProps) {
  return (
    <div className={cn('flex items-center gap-3', className)} suppressHydrationWarning>
      <div className={cn(sizeClasses[size], 'relative flex-shrink-0')}>
        <Image
          src="/images/lankaconnect-logo.png"
          alt="LankaConnect"
          width={imageSizes[size]}
          height={imageSizes[size]}
          className="object-contain w-full h-full"
          priority
        />
      </div>
      {showText && (
        <span className={cn('font-bold text-maroon', textSizeClasses[size])}>
          LankaConnect
        </span>
      )}
    </div>
  );
}
