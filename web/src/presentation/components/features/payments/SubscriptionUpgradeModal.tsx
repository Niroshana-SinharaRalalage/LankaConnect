'use client';

import React, { useState, useEffect } from 'react';
import { X, Check, Loader2 } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '@/presentation/components/ui/Card';
import { paymentsRepository } from '@/infrastructure/api/repositories/payments.repository';
import { BillingInterval, PricingTier } from '@/infrastructure/api/types/payments.types';

export interface SubscriptionUpgradeModalProps {
  isOpen: boolean;
  onClose: () => void;
  tier: PricingTier;
}

/**
 * SubscriptionUpgradeModal Component
 * Phase 6A.4: Stripe Payment Integration - Subscription upgrade modal
 * Handles checkout session creation and redirects to Stripe Checkout
 */
export function SubscriptionUpgradeModal({
  isOpen,
  onClose,
  tier
}: SubscriptionUpgradeModalProps) {
  const [billingInterval, setBillingInterval] = useState<BillingInterval>(BillingInterval.Monthly);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Reset state when modal closes
  useEffect(() => {
    if (!isOpen) {
      setBillingInterval(BillingInterval.Monthly);
      setError(null);
    }
  }, [isOpen]);

  if (!isOpen) return null;

  // Pricing configuration (matches backend appsettings.json)
  const pricing = {
    [PricingTier.General]: {
      monthly: { amount: 1000, priceId: 'price_general_monthly' }, // $10.00/month
      annual: { amount: 10000, priceId: 'price_general_annual' },  // $100.00/year
    },
    [PricingTier.EventOrganizer]: {
      monthly: { amount: 2000, priceId: 'price_organizer_monthly' }, // $20.00/month
      annual: { amount: 20000, priceId: 'price_organizer_annual' },  // $200.00/year
    },
  };

  const currentPrice = pricing[tier][billingInterval];
  const displayAmount = (currentPrice.amount / 100).toFixed(2);
  const monthlyEquivalent = billingInterval === BillingInterval.Annual
    ? (currentPrice.amount / 12 / 100).toFixed(2)
    : null;

  const handleSubscribe = async () => {
    try {
      setLoading(true);
      setError(null);

      // Get current URL for success/cancel redirects
      const baseUrl = window.location.origin;
      const successUrl = `${baseUrl}/dashboard?checkout=success`;
      const cancelUrl = `${baseUrl}/dashboard?checkout=canceled`;

      // Create Stripe Checkout session
      const { sessionUrl } = await paymentsRepository.createCheckoutSession({
        priceId: currentPrice.priceId,
        successUrl,
        cancelUrl,
      });

      // Redirect to Stripe Checkout
      window.location.href = sessionUrl;
    } catch (err) {
      console.error('Error creating checkout session:', err);
      setError(err instanceof Error ? err.message : 'Failed to start checkout process');
      setLoading(false);
    }
  };

  const features = tier === PricingTier.General
    ? [
        'Create unlimited events',
        'Event registration management',
        'Basic analytics',
        'Email notifications',
        'Community forum access',
      ]
    : [
        'All General features',
        'Advanced event templates',
        'Priority event placement',
        'Detailed analytics & insights',
        'Custom branding options',
        'Priority support',
      ];

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="sticky top-0 bg-white border-b px-6 py-4 flex items-center justify-between">
          <h2 className="text-xl font-bold" style={{ color: '#8B1538' }}>
            Upgrade to {tier === PricingTier.General ? 'General' : 'Event Organizer'} Plan
          </h2>
          <button
            onClick={onClose}
            disabled={loading}
            className="text-gray-400 hover:text-gray-600 transition-colors"
          >
            <X className="w-6 h-6" />
          </button>
        </div>

        {/* Content */}
        <div className="p-6 space-y-6">
          {/* Billing Interval Toggle */}
          <div className="flex gap-2 p-1 bg-gray-100 rounded-lg">
            <button
              onClick={() => setBillingInterval(BillingInterval.Monthly)}
              disabled={loading}
              className={`flex-1 py-2 px-4 rounded-md font-medium transition-all ${
                billingInterval === BillingInterval.Monthly
                  ? 'bg-white shadow-sm'
                  : 'text-gray-600 hover:text-gray-900'
              }`}
              style={
                billingInterval === BillingInterval.Monthly
                  ? { color: '#8B1538' }
                  : undefined
              }
            >
              Monthly
            </button>
            <button
              onClick={() => setBillingInterval(BillingInterval.Annual)}
              disabled={loading}
              className={`flex-1 py-2 px-4 rounded-md font-medium transition-all ${
                billingInterval === BillingInterval.Annual
                  ? 'bg-white shadow-sm'
                  : 'text-gray-600 hover:text-gray-900'
              }`}
              style={
                billingInterval === BillingInterval.Annual
                  ? { color: '#8B1538' }
                  : undefined
              }
            >
              Annual
              <span className="ml-2 text-xs text-green-600 font-semibold">Save 17%</span>
            </button>
          </div>

          {/* Price Display */}
          <div className="text-center py-4">
            <div className="text-4xl font-bold" style={{ color: '#8B1538' }}>
              ${displayAmount}
              <span className="text-lg font-normal text-gray-600">
                /{billingInterval}
              </span>
            </div>
            {monthlyEquivalent && (
              <p className="text-sm text-gray-600 mt-1">
                (${monthlyEquivalent}/month when billed annually)
              </p>
            )}
          </div>

          {/* Features List */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Plan Features</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              {features.map((feature, index) => (
                <div key={index} className="flex items-start gap-2">
                  <Check className="w-5 h-5 mt-0.5 flex-shrink-0" style={{ color: '#FF7900' }} />
                  <span className="text-sm text-gray-700">{feature}</span>
                </div>
              ))}
            </CardContent>
          </Card>

          {/* Error Message */}
          {error && (
            <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
              <p className="text-sm text-red-700">{error}</p>
            </div>
          )}

          {/* Subscribe Button */}
          <Button
            onClick={handleSubscribe}
            disabled={loading}
            className="w-full py-3"
            style={{
              background: loading ? '#ccc' : '#8B1538',
              color: 'white',
            }}
          >
            {loading ? (
              <>
                <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                Processing...
              </>
            ) : (
              `Subscribe Now - $${displayAmount}/${billingInterval}`
            )}
          </Button>

          {/* Payment Security Note */}
          <p className="text-xs text-gray-500 text-center">
            Secure payment processing powered by Stripe. You can cancel anytime.
          </p>
        </div>
      </div>
    </div>
  );
}
