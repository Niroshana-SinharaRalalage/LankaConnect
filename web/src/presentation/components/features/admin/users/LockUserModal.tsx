/**
 * Phase 6A.90: Lock User Modal
 * Modal for locking a user account with duration and reason
 */

import { useState } from 'react';
import { X, Lock } from 'lucide-react';
import { LOCK_DURATION_OPTIONS } from '@/infrastructure/api/types/admin-users.types';

interface LockUserModalProps {
  isOpen: boolean;
  userName: string;
  onClose: () => void;
  onConfirm: (lockUntil: string, reason?: string) => void;
  isLoading?: boolean;
}

export function LockUserModal({
  isOpen,
  userName,
  onClose,
  onConfirm,
  isLoading = false,
}: LockUserModalProps) {
  const [selectedDuration, setSelectedDuration] = useState<number>(24);
  const [customHours, setCustomHours] = useState<string>('48');
  const [reason, setReason] = useState('');

  if (!isOpen) return null;

  const handleConfirm = () => {
    const hours = selectedDuration === -1 ? parseInt(customHours, 10) : selectedDuration;
    const lockUntil = new Date(Date.now() + hours * 60 * 60 * 1000).toISOString();
    onConfirm(lockUntil, reason || undefined);
  };

  const handleClose = () => {
    setSelectedDuration(24);
    setCustomHours('48');
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
              <Lock className="w-5 h-5 text-amber-600" />
            </div>
            <h3 className="text-lg font-semibold text-gray-900">Lock User Account</h3>
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
            You are about to lock the account for <span className="font-semibold text-gray-900">{userName}</span>.
            They will not be able to log in until the lock expires.
          </p>

          {/* Duration Selection */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Lock Duration
            </label>
            <div className="grid grid-cols-2 gap-2">
              {LOCK_DURATION_OPTIONS.map((option) => (
                <button
                  key={option.value}
                  type="button"
                  onClick={() => setSelectedDuration(option.value)}
                  className={`px-3 py-2 text-sm rounded-md border transition-colors ${
                    selectedDuration === option.value
                      ? 'border-amber-500 bg-amber-50 text-amber-700'
                      : 'border-gray-300 bg-white text-gray-700 hover:bg-gray-50'
                  }`}
                >
                  {option.label}
                </button>
              ))}
            </div>
          </div>

          {/* Custom Duration Input */}
          {selectedDuration === -1 && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Custom Duration (hours)
              </label>
              <input
                type="number"
                min="1"
                max="8760"
                value={customHours}
                onChange={(e) => setCustomHours(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
                placeholder="Enter hours"
              />
            </div>
          )}

          {/* Reason */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Reason (optional)
            </label>
            <textarea
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              rows={3}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-amber-500 focus:border-amber-500 resize-none"
              placeholder="Enter reason for locking this account..."
            />
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
            disabled={isLoading || (selectedDuration === -1 && (!customHours || parseInt(customHours, 10) < 1))}
            className="px-4 py-2 text-sm font-medium text-white bg-amber-600 rounded-md hover:bg-amber-700 transition-colors disabled:opacity-50 flex items-center gap-2"
          >
            {isLoading ? (
              <>
                <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                Locking...
              </>
            ) : (
              <>
                <Lock className="w-4 h-4" />
                Lock Account
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
}
