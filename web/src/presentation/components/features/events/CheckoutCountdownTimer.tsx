'use client';

import React, { useState, useEffect } from 'react';
import { Clock, AlertCircle } from 'lucide-react';

export interface CheckoutCountdownTimerProps {
  /** ISO 8601 timestamp when checkout session expires */
  expiresAt: string;
  /** Optional className for styling */
  className?: string;
  /** Callback when countdown reaches zero */
  onExpired?: () => void;
}

interface TimeRemaining {
  hours: number;
  minutes: number;
  seconds: number;
  isExpired: boolean;
}

/**
 * CheckoutCountdownTimer Component
 * Phase 6A.81 Part 3: Displays countdown for Stripe checkout session expiration
 *
 * Shows real-time countdown (HH:MM:SS) for 24-hour payment window.
 * Transitions to "Expired" state when time runs out.
 *
 * @example
 * ```tsx
 * <CheckoutCountdownTimer
 *   expiresAt="2026-01-27T18:00:00Z"
 *   onExpired={() => setCheckoutExpired(true)}
 * />
 * ```
 */
export function CheckoutCountdownTimer({
  expiresAt,
  className = '',
  onExpired
}: CheckoutCountdownTimerProps) {
  const [timeRemaining, setTimeRemaining] = useState<TimeRemaining>(
    calculateTimeRemaining(expiresAt)
  );

  useEffect(() => {
    // Update countdown every second
    const intervalId = setInterval(() => {
      const remaining = calculateTimeRemaining(expiresAt);
      setTimeRemaining(remaining);

      // Trigger callback when expired
      if (remaining.isExpired && onExpired) {
        onExpired();
        clearInterval(intervalId);
      }
    }, 1000);

    return () => clearInterval(intervalId);
  }, [expiresAt, onExpired]);

  if (timeRemaining.isExpired) {
    return (
      <div className={`inline-flex items-center gap-2 px-3 py-1.5 rounded-md bg-red-50 border border-red-200 ${className}`}>
        <AlertCircle className="w-4 h-4 text-red-600" />
        <span className="text-sm font-medium text-red-700">Checkout Expired</span>
      </div>
    );
  }

  const { hours, minutes, seconds } = timeRemaining;
  const isExpiringSoon = hours === 0 && minutes < 30; // Less than 30 minutes

  const bgColor = isExpiringSoon ? 'bg-red-50' : 'bg-orange-50';
  const borderColor = isExpiringSoon ? 'border-red-200' : 'border-orange-200';
  const textColor = isExpiringSoon ? 'text-red-700' : 'text-orange-700';
  const iconColor = isExpiringSoon ? 'text-red-600' : 'text-orange-600';

  return (
    <div className={`inline-flex items-center gap-2 px-3 py-1.5 rounded-md ${bgColor} border ${borderColor} ${className}`}>
      <Clock className={`w-4 h-4 ${iconColor}`} />
      <span className={`text-sm font-medium ${textColor}`}>
        Payment due in: {formatTime(hours)}:{formatTime(minutes)}:{formatTime(seconds)}
      </span>
    </div>
  );
}

/**
 * Calculate time remaining until checkout session expires
 */
function calculateTimeRemaining(expiresAt: string): TimeRemaining {
  const expiryTime = new Date(expiresAt).getTime();
  const currentTime = Date.now();
  const diff = expiryTime - currentTime;

  if (diff <= 0) {
    return {
      hours: 0,
      minutes: 0,
      seconds: 0,
      isExpired: true
    };
  }

  const totalSeconds = Math.floor(diff / 1000);
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;

  return {
    hours,
    minutes,
    seconds,
    isExpired: false
  };
}

/**
 * Format number with leading zero (e.g., 5 -> "05")
 */
function formatTime(value: number): string {
  return value.toString().padStart(2, '0');
}
