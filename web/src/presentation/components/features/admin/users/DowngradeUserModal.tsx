/**
 * Phase 6A.90: Downgrade User Modal
 * Modal for downgrading a user's role to GeneralUser (Member)
 */

import { useState } from 'react';
import { X, ArrowDownCircle } from 'lucide-react';

interface DowngradeUserModalProps {
  isOpen: boolean;
  userName: string;
  currentRole: string;
  onClose: () => void;
  onConfirm: (reason: string) => void;
  isLoading?: boolean;
}

export function DowngradeUserModal({
  isOpen,
  userName,
  currentRole,
  onClose,
  onConfirm,
  isLoading = false,
}: DowngradeUserModalProps) {
  const [reason, setReason] = useState('');

  if (!isOpen) return null;

  const handleConfirm = () => {
    if (reason.trim().length >= 10) {
      onConfirm(reason.trim());
    }
  };

  const handleClose = () => {
    setReason('');
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full mx-4 overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200 bg-amber-50">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-amber-100 rounded-full">
              <ArrowDownCircle className="w-5 h-5 text-amber-600" />
            </div>
            <h3 className="text-lg font-semibold text-gray-900">Downgrade User Role</h3>
          </div>
          <button
            onClick={handleClose}
            className="text-gray-400 hover:text-gray-600 transition-colors"
            disabled={isLoading}
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="px-6 py-4 space-y-4">
          <p className="text-gray-600">
            You are about to downgrade <span className="font-semibold text-gray-900">{userName}</span> from{' '}
            <span className="font-semibold text-gray-900">{currentRole}</span> to{' '}
            <span className="font-semibold text-gray-900">Member</span>.
          </p>

          {/* Warning Box */}
          <div className="bg-amber-50 border border-amber-200 rounded-md p-3">
            <p className="text-sm text-amber-800 font-medium mb-2">This will:</p>
            <ul className="text-sm text-amber-700 space-y-1 list-disc list-inside">
              <li>Remove their elevated privileges</li>
              <li>Unpublish all their future events</li>
              <li>Cancel any active subscription</li>
            </ul>
          </div>

          {/* Reason */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Reason <span className="text-red-500">*</span>
            </label>
            <textarea
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              rows={4}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-amber-500 focus:border-amber-500 resize-none"
              placeholder="Enter reason for downgrading this user (minimum 10 characters)..."
              disabled={isLoading}
            />
            <p className="mt-1 text-xs text-gray-500">
              {reason.length}/10 characters minimum
            </p>
          </div>
        </div>

        {/* Footer */}
        <div className="flex justify-end gap-3 px-6 py-4 border-t border-gray-200 bg-gray-50">
          <button
            type="button"
            onClick={handleClose}
            disabled={isLoading}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={handleConfirm}
            disabled={isLoading || reason.trim().length < 10}
            className="px-4 py-2 text-sm font-medium text-white bg-amber-600 rounded-md hover:bg-amber-700 transition-colors disabled:opacity-50 flex items-center gap-2"
          >
            {isLoading ? (
              <>
                <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                Downgrading...
              </>
            ) : (
              <>
                <ArrowDownCircle className="w-4 h-4" />
                Yes, Downgrade to Member
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
}
