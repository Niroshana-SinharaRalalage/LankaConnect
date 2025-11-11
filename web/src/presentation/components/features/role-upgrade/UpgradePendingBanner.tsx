'use client';

import * as React from 'react';
import { Clock, X } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { useCancelRoleUpgrade } from '@/presentation/hooks/useRoleUpgrade';

/**
 * Phase 6A.7: User Upgrade Workflow - Upgrade Pending Banner Component
 * Banner shown when user has a pending role upgrade request
 *
 * Features:
 * - Informative message about pending status
 * - Cancel upgrade button
 * - Dismissible (hides temporarily)
 * - Optimistic updates with React Query
 */

interface UpgradePendingBannerProps {
  upgradeRequestedAt?: Date;
}

export function UpgradePendingBanner({ upgradeRequestedAt }: UpgradePendingBannerProps) {
  const [isDismissed, setIsDismissed] = React.useState(false);
  const [showCancelConfirm, setShowCancelConfirm] = React.useState(false);
  const cancelUpgrade = useCancelRoleUpgrade();

  const handleCancel = async () => {
    try {
      await cancelUpgrade.mutateAsync();
      setShowCancelConfirm(false);
    } catch (error) {
      console.error('Failed to cancel upgrade:', error);
    }
  };

  if (isDismissed) return null;

  return (
    <>
      <div
        className="mb-6 p-4 rounded-lg border-2 flex items-start gap-4"
        style={{
          background: 'linear-gradient(135deg, #FFF5E6 0%, #FFE8CC 100%)',
          borderColor: '#FF7900',
        }}
      >
        <div className="flex-shrink-0">
          <div className="w-10 h-10 rounded-full bg-[#FF7900] flex items-center justify-center">
            <Clock className="w-5 h-5 text-white" />
          </div>
        </div>

        <div className="flex-1">
          <h3 className="text-lg font-semibold text-[#8B1538] mb-1">
            Event Organizer Upgrade Pending
          </h3>
          <p className="text-gray-700 text-sm mb-3">
            Your request to become an Event Organizer is under review. Our team will review your application
            and notify you within 2-3 business days.
          </p>
          {upgradeRequestedAt && (
            <p className="text-xs text-gray-600">
              Requested on: {new Date(upgradeRequestedAt).toLocaleDateString('en-US', {
                month: 'long',
                day: 'numeric',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit',
              })}
            </p>
          )}

          <div className="mt-3 flex gap-2">
            <Button
              size="sm"
              variant="outline"
              onClick={() => setShowCancelConfirm(true)}
              disabled={cancelUpgrade.isPending}
              className="text-sm"
              style={{
                borderColor: '#8B1538',
                color: '#8B1538',
              }}
            >
              {cancelUpgrade.isPending ? 'Canceling...' : 'Cancel Request'}
            </Button>
          </div>
        </div>

        <button
          onClick={() => setIsDismissed(true)}
          className="flex-shrink-0 text-gray-500 hover:text-gray-700 transition-colors"
          title="Dismiss banner"
        >
          <X className="w-5 h-5" />
        </button>
      </div>

      {/* Cancel Confirmation Modal */}
      {showCancelConfirm && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-xl p-6 max-w-md w-full">
            <h3 className="text-xl font-bold text-[#8B1538] mb-3">Cancel Upgrade Request?</h3>
            <p className="text-gray-700 mb-6">
              Are you sure you want to cancel your Event Organizer upgrade request? You can submit a new
              request anytime.
            </p>

            {cancelUpgrade.isError && (
              <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg">
                <p className="text-sm text-red-600">
                  Failed to cancel request. Please try again.
                </p>
              </div>
            )}

            <div className="flex gap-3">
              <Button
                variant="outline"
                onClick={() => setShowCancelConfirm(false)}
                className="flex-1"
                disabled={cancelUpgrade.isPending}
              >
                Keep Request
              </Button>
              <Button
                onClick={handleCancel}
                className="flex-1"
                style={{
                  background: '#8B1538',
                  color: 'white',
                }}
                disabled={cancelUpgrade.isPending}
              >
                {cancelUpgrade.isPending ? 'Canceling...' : 'Yes, Cancel'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
