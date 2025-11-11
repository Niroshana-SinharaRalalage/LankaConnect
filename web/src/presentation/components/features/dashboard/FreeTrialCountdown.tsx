'use client';

import React from 'react';
import { AlertCircle, CheckCircle, Clock } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { useRouter } from 'next/navigation';
import { SubscriptionStatus } from '@/infrastructure/api/types/subscription.types';
import { getFreeTrialDaysRemaining } from '@/infrastructure/api/utils/role-helpers';

export interface FreeTrialCountdownProps {
  subscriptionStatus: SubscriptionStatus;
  freeTrialEndsAt?: string;
  className?: string;
}

/**
 * FreeTrialCountdown Component
 * Phase 6A.2.10: Displays free trial status and days remaining for Event Organizers
 * Shows different UI based on subscription status (Trialing, Active, Expired, etc.)
 */
export function FreeTrialCountdown({
  subscriptionStatus,
  freeTrialEndsAt,
  className = ''
}: FreeTrialCountdownProps) {
  const router = useRouter();
  const daysRemaining = getFreeTrialDaysRemaining(freeTrialEndsAt);

  // Don't show component if no trial data
  if (subscriptionStatus === SubscriptionStatus.None) {
    return null;
  }

  // Active paid subscription
  if (subscriptionStatus === SubscriptionStatus.Active) {
    return (
      <Card className={`border-green-200 bg-green-50 ${className}`}>
        <CardHeader className="pb-3">
          <CardTitle className="flex items-center gap-2 text-green-800">
            <CheckCircle className="w-5 h-5" />
            <span className="text-base">Active Subscription</span>
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-green-700">
            Your subscription is active. You have full access to create and manage events.
          </p>
        </CardContent>
      </Card>
    );
  }

  // Free trial active
  if (subscriptionStatus === SubscriptionStatus.Trialing && daysRemaining !== null) {
    const isExpiringSoon = daysRemaining <= 7;
    const bgColor = isExpiringSoon ? 'bg-orange-50' : 'bg-blue-50';
    const borderColor = isExpiringSoon ? 'border-orange-200' : 'border-blue-200';
    const textColor = isExpiringSoon ? 'text-orange-800' : 'text-blue-800';
    const accentColor = isExpiringSoon ? 'text-orange-700' : 'text-blue-700';

    return (
      <Card className={`${borderColor} ${bgColor} ${className}`}>
        <CardHeader className="pb-3">
          <CardTitle className={`flex items-center gap-2 ${textColor}`}>
            <Clock className="w-5 h-5" />
            <span className="text-base">Free Trial</span>
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <div>
            <p className={`text-2xl font-bold ${textColor}`}>
              {daysRemaining} {daysRemaining === 1 ? 'day' : 'days'}
            </p>
            <p className={`text-sm ${accentColor}`}>
              remaining in your 6-month free trial
            </p>
          </div>

          {isExpiringSoon && (
            <div className="pt-2">
              <p className="text-sm text-orange-700 mb-3">
                Your trial is ending soon. Subscribe now to continue creating events.
              </p>
              <Button
                onClick={() => router.push('/subscription/upgrade')}
                className="w-full"
                style={{
                  background: '#FF7900',
                  color: 'white'
                }}
              >
                Subscribe Now - $10/month
              </Button>
            </div>
          )}

          {!isExpiringSoon && (
            <p className="text-sm text-blue-600">
              Enjoy unlimited event creation during your trial period.
            </p>
          )}
        </CardContent>
      </Card>
    );
  }

  // Expired, Past Due, or Canceled
  if (
    subscriptionStatus === SubscriptionStatus.Expired ||
    subscriptionStatus === SubscriptionStatus.PastDue ||
    subscriptionStatus === SubscriptionStatus.Canceled
  ) {
    return (
      <Card className={`border-red-200 bg-red-50 ${className}`}>
        <CardHeader className="pb-3">
          <CardTitle className="flex items-center gap-2 text-red-800">
            <AlertCircle className="w-5 h-5" />
            <span className="text-base">
              {subscriptionStatus === SubscriptionStatus.Expired ? 'Trial Expired' :
               subscriptionStatus === SubscriptionStatus.PastDue ? 'Payment Due' :
               'Subscription Canceled'}
            </span>
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <p className="text-sm text-red-700">
            {subscriptionStatus === SubscriptionStatus.Expired
              ? 'Your free trial has ended. Subscribe to continue creating events.'
              : subscriptionStatus === SubscriptionStatus.PastDue
              ? 'Your payment is past due. Please update your payment method.'
              : 'Your subscription was canceled. Reactivate to continue creating events.'}
          </p>
          <Button
            onClick={() => router.push('/subscription/upgrade')}
            className="w-full"
            style={{
              background: '#8B1538',
              color: 'white'
            }}
          >
            {subscriptionStatus === SubscriptionStatus.PastDue
              ? 'Update Payment'
              : 'Subscribe Now - $10/month'}
          </Button>
        </CardContent>
      </Card>
    );
  }

  return null;
}
