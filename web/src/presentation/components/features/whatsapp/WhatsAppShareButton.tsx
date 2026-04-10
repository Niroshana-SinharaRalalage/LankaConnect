'use client';

import { MessageCircle } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';

interface WhatsAppShareButtonProps {
  eventTitle: string;
  eventUrl: string;
  eventDate?: string;
  eventLocation?: string;
  className?: string;
  size?: 'sm' | 'default' | 'lg';
  variant?: 'default' | 'outline' | 'ghost';
}

/**
 * WhatsAppShareButton Component
 * Phase 7A.4: Share event via WhatsApp deep link (wa.me)
 *
 * Opens WhatsApp with pre-filled message containing event details
 * Uses wa.me deep link — works on mobile and desktop
 */
export function WhatsAppShareButton({
  eventTitle,
  eventUrl,
  eventDate,
  eventLocation,
  className,
  size = 'sm',
  variant = 'outline',
}: WhatsAppShareButtonProps) {
  const handleShare = () => {
    // Build share message
    let message = `Check out this event: ${eventTitle}`;
    if (eventDate) {
      message += `\nDate: ${eventDate}`;
    }
    if (eventLocation) {
      message += `\nLocation: ${eventLocation}`;
    }
    message += `\n\n${eventUrl}`;

    // wa.me deep link with pre-filled text (no phone number = user picks contact)
    const waUrl = `https://wa.me/?text=${encodeURIComponent(message)}`;
    window.open(waUrl, '_blank', 'noopener,noreferrer');
  };

  return (
    <Button
      type="button"
      variant={variant}
      size={size}
      onClick={handleShare}
      className={className}
      style={{
        borderColor: '#25D366',
        color: '#25D366',
      }}
      aria-label="Share via WhatsApp"
    >
      <MessageCircle className="h-4 w-4 mr-1.5" />
      Share
    </Button>
  );
}
