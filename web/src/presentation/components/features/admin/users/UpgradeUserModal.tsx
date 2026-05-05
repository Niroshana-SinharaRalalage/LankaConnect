/**
 * Phase 6A.139: Upgrade User Modal
 * Modal for upgrading a GeneralUser to EventOrganizer (admin-initiated).
 * Symmetric counterpart to DowngradeUserModal — positive variant (emerald accent).
 */

import { useState } from 'react';
import { X, ArrowUpCircle } from 'lucide-react';

interface UpgradeUserModalProps {
  isOpen: boolean;
  userName: string;
  onClose: () => void;
  onConfirm: (reason: string) => void;
  isLoading?: boolean;
}

export function UpgradeUserModal({
  isOpen,
  userName,
  onClose,
  onConfirm,
  isLoading = false,
}: UpgradeUserModalProps) {
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
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200 bg-emerald-50">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-emerald-100 rounded-full">
              <ArrowUpCircle className="w-5 h-5 text-emerald-600" />
            </div>
            <h3 className="text-lg font-semibold text-gray-900">Upgrade to Event Organizer</h3>
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
            You are about to upgrade <span className="font-semibold text-gray-900">{userName}</span> from{' '}
            <span className="font-semibold text-gray-900">Member</span> to{' '}
            <span className="font-semibold text-gray-900">Event Organizer</span>.
          </p>

          {/* Info Box */}
          <div className="bg-emerald-50 border border-emerald-200 rounded-md p-3">
            <p className="text-sm text-emerald-800 font-medium mb-2">This will:</p>
            <ul className="text-sm text-emerald-700 space-y-1 list-disc list-inside">
              <li>Grant Event Organizer privileges and dashboard access</li>
              <li>Send a confirmation email to the user</li>
              <li>Create an in-app notification</li>
              <li>Clear any pending upgrade request</li>
            </ul>
            <p className="mt-2 text-xs text-emerald-700">
              The user will need to log out and back in for the new role to take effect on their session.
            </p>
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
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 resize-none"
              placeholder="Enter reason for upgrading this user (minimum 10 characters)..."
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
            className="px-4 py-2 text-sm font-medium text-white bg-emerald-600 rounded-md hover:bg-emerald-700 transition-colors disabled:opacity-50 flex items-center gap-2"
          >
            {isLoading ? (
              <>
                <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                Upgrading...
              </>
            ) : (
              <>
                <ArrowUpCircle className="w-4 h-4" />
                Yes, Upgrade to Event Organizer
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
}
