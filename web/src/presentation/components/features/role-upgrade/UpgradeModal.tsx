'use client';

import * as React from 'react';
import { X, Check, Sparkles } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { useRequestRoleUpgrade } from '@/presentation/hooks/useRoleUpgrade';

/**
 * Phase 6A.7: User Upgrade Workflow - Upgrade Modal Component
 * Modal for requesting role upgrade to Event Organizer
 *
 * Features:
 * - Shows Event Organizer benefits
 * - Collects upgrade reason from user
 * - Validates reason input
 * - Displays loading and error states
 */

interface UpgradeModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export function UpgradeModal({ isOpen, onClose }: UpgradeModalProps) {
  const [reason, setReason] = React.useState('');
  const [showSuccess, setShowSuccess] = React.useState(false);
  const requestUpgrade = useRequestRoleUpgrade();

  // Reset state when modal opens/closes
  React.useEffect(() => {
    if (!isOpen) {
      setReason('');
      setShowSuccess(false);
      requestUpgrade.reset();
    }
  }, [isOpen, requestUpgrade]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (reason.trim().length < 20) {
      return;
    }

    try {
      await requestUpgrade.mutateAsync({
        targetRole: 'EventOrganizer',
        reason: reason.trim(),
      });
      setShowSuccess(true);
      setTimeout(() => {
        onClose();
      }, 2000);
    } catch (error) {
      console.error('Failed to request upgrade:', error);
    }
  };

  if (!isOpen) return null;

  if (showSuccess) {
    return (
      <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
        <div className="bg-white rounded-xl p-8 max-w-md w-full text-center">
          <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <Check className="w-8 h-8 text-green-600" />
          </div>
          <h3 className="text-2xl font-bold text-[#8B1538] mb-2">Request Submitted!</h3>
          <p className="text-gray-600">
            Your upgrade request has been submitted for admin review. You'll be notified once it's processed.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div
          className="p-6 flex items-center justify-between"
          style={{ background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)' }}
        >
          <div className="flex items-center gap-3">
            <Sparkles className="w-6 h-6 text-white" />
            <h2 className="text-2xl font-bold text-white">Upgrade to Event Organizer</h2>
          </div>
          <button
            onClick={onClose}
            className="text-white hover:bg-white/20 rounded-lg p-2 transition-colors"
            disabled={requestUpgrade.isPending}
          >
            <X className="w-6 h-6" />
          </button>
        </div>

        {/* Content */}
        <div className="p-6">
          {/* Benefits Section */}
          <div className="mb-6">
            <h3 className="text-lg font-semibold text-[#8B1538] mb-4">Event Organizer Benefits</h3>
            <div className="space-y-3">
              {[
                'Create and manage unlimited events',
                '6-month free trial to explore all features',
                'Access to event analytics and insights',
                'Priority event listing in search results',
                'Custom event branding and themes',
                'Email notifications to interested attendees',
              ].map((benefit, index) => (
                <div key={index} className="flex items-start gap-3">
                  <div className="w-6 h-6 rounded-full bg-green-100 flex items-center justify-center flex-shrink-0 mt-0.5">
                    <Check className="w-4 h-4 text-green-600" />
                  </div>
                  <p className="text-gray-700">{benefit}</p>
                </div>
              ))}
            </div>
          </div>

          {/* Pricing Info */}
          <div className="mb-6 p-4 bg-orange-50 rounded-lg border border-orange-200">
            <p className="text-sm text-gray-700">
              <span className="font-semibold text-[#FF7900]">Free for 6 months!</span> After your trial,
              Event Organizer subscription is just $9.99/month. Cancel anytime.
            </p>
          </div>

          {/* Reason Form */}
          <form onSubmit={handleSubmit}>
            <div className="mb-6">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Tell us why you want to become an Event Organizer <span className="text-red-500">*</span>
              </label>
              <textarea
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF7900] focus:border-transparent transition-all resize-none"
                rows={4}
                placeholder="Describe the types of events you plan to organize and how you'll engage the community... (minimum 20 characters)"
                disabled={requestUpgrade.isPending}
                required
              />
              <p className="text-sm text-gray-500 mt-1">
                {reason.length}/20 characters minimum
              </p>
            </div>

            {/* Error Message */}
            {requestUpgrade.isError && (
              <div className="mb-4 p-4 bg-red-50 border border-red-200 rounded-lg">
                <p className="text-sm text-red-600">
                  {requestUpgrade.error instanceof Error
                    ? requestUpgrade.error.message
                    : 'Failed to submit upgrade request. Please try again.'}
                </p>
              </div>
            )}

            {/* Action Buttons */}
            <div className="flex gap-3">
              <Button
                type="button"
                onClick={onClose}
                variant="outline"
                className="flex-1"
                disabled={requestUpgrade.isPending}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                className="flex-1"
                style={{
                  background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)',
                  color: 'white',
                }}
                disabled={requestUpgrade.isPending || reason.trim().length < 20}
              >
                {requestUpgrade.isPending ? 'Submitting...' : 'Submit Request'}
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
