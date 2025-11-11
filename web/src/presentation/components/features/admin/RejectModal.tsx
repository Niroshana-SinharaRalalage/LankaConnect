'use client';

import * as React from 'react';
import { Button } from '@/presentation/components/ui/Button';

export interface RejectModalProps {
  isOpen: boolean;
  userName: string;
  onClose: () => void;
  onConfirm: (reason: string) => void;
  isLoading?: boolean;
}

/**
 * RejectModal Component
 * Phase 6A.5: Modal for rejecting role upgrade requests with reason
 */
export function RejectModal({
  isOpen,
  userName,
  onClose,
  onConfirm,
  isLoading = false
}: RejectModalProps) {
  const [reason, setReason] = React.useState('');

  const handleConfirm = () => {
    onConfirm(reason);
  };

  const handleClose = () => {
    setReason('');
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full mx-4 p-6">
        <h2 className="text-2xl font-bold text-[#8B1538] mb-4">
          Reject Role Upgrade
        </h2>
        <p className="text-gray-700 mb-4">
          Are you sure you want to reject the role upgrade request for <strong>{userName}</strong>?
        </p>
        <div className="mb-6">
          <label htmlFor="reason" className="block text-sm font-medium text-gray-700 mb-2">
            Reason (Optional)
          </label>
          <textarea
            id="reason"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#FF7900] focus:border-transparent"
            rows={4}
            placeholder="Provide a reason for rejection (optional)"
            disabled={isLoading}
          />
        </div>
        <div className="flex gap-3 justify-end">
          <Button
            variant="outline"
            onClick={handleClose}
            disabled={isLoading}
            className="border-gray-300 text-gray-700 hover:bg-gray-50"
          >
            Cancel
          </Button>
          <Button
            variant="default"
            onClick={handleConfirm}
            disabled={isLoading}
            className="bg-red-600 hover:bg-red-700 text-white"
          >
            {isLoading ? 'Rejecting...' : 'Reject'}
          </Button>
        </div>
      </div>
    </div>
  );
}
