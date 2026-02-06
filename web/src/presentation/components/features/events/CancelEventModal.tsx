'use client';

import * as React from 'react';
import { X } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';

export interface CancelEventModalProps {
  isOpen: boolean;
  eventTitle: string;
  onCancel: () => void;
  onConfirm: (reason: string) => Promise<void>;
}

/**
 * Phase 6A.59: Cancel Event Modal
 * Provides a user-friendly interface for cancelling events with a reason
 */
export function CancelEventModal({
  isOpen,
  eventTitle,
  onCancel,
  onConfirm,
}: CancelEventModalProps) {
  const [reason, setReason] = React.useState('');
  const [isSubmitting, setIsSubmitting] = React.useState(false);
  const [error, setError] = React.useState('');

  // Reset state when modal opens/closes
  React.useEffect(() => {
    if (isOpen) {
      setReason('');
      setError('');
      setIsSubmitting(false);
    }
  }, [isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const trimmedReason = reason.trim();

    if (trimmedReason.length < 10) {
      setError('Cancellation reason must be at least 10 characters');
      return;
    }

    try {
      setIsSubmitting(true);
      setError('');
      await onConfirm(trimmedReason);
      // Modal will close via onCancel in parent after successful submission
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to cancel event');
      setIsSubmitting(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full mx-4">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-gray-200">
          <h2 className="text-xl font-semibold text-[#8B1538]">Cancel Event</h2>
          <button
            onClick={onCancel}
            disabled={isSubmitting}
            className="text-gray-400 hover:text-gray-600 disabled:opacity-50"
            aria-label="Close modal"
          >
            <X className="w-6 h-6" />
          </button>
        </div>

        {/* Body */}
        <form onSubmit={handleSubmit} className="p-6">
          <div className="mb-4">
            <p className="text-sm text-gray-600 mb-2">Event:</p>
            <p className="font-medium text-gray-900">{eventTitle}</p>
          </div>

          <div className="mb-4">
            <label htmlFor="reason" className="block text-sm font-medium text-gray-700 mb-2">
              Reason for cancellation <span className="text-red-500">*</span>
            </label>
            <textarea
              id="reason"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              disabled={isSubmitting}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-[#FF7900] focus:border-transparent disabled:bg-gray-100 disabled:cursor-not-allowed"
              rows={4}
              placeholder="Please provide a reason for cancelling this event (minimum 10 characters)"
              required
            />
            <p className="text-xs text-gray-500 mt-1">
              {reason.trim().length}/10 characters minimum
            </p>
          </div>

          {error && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-md">
              <p className="text-sm text-red-700">{error}</p>
            </div>
          )}

          <div className="bg-yellow-50 border border-yellow-200 rounded-md p-3 mb-4">
            <p className="text-sm text-yellow-800">
              <strong>Warning:</strong> All registered attendees will be notified via email about this cancellation.
            </p>
          </div>

          {/* Footer */}
          <div className="flex gap-3 justify-end">
            <Button
              type="button"
              onClick={onCancel}
              disabled={isSubmitting}
              variant="outline"
              size="default"
            >
              Go Back
            </Button>
            <Button
              type="submit"
              disabled={isSubmitting || reason.trim().length < 10}
              className="bg-[#F59E0B] hover:bg-[#D97706] text-white disabled:opacity-50"
              size="default"
            >
              {isSubmitting ? 'Cancelling...' : 'Cancel Event'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
