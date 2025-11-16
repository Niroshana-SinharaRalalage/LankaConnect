'use client';

import * as React from 'react';
import { Button } from '@/presentation/components/ui/Button';
import { RejectModal } from './RejectModal';
import { approvalsRepository } from '@/infrastructure/api/repositories/approvals.repository';
import type { PendingRoleUpgradeDto } from '@/infrastructure/api/types/approvals.types';

export interface ApprovalsTableProps {
  approvals: PendingRoleUpgradeDto[];
  onUpdate: () => void;
}

/**
 * ApprovalsTable Component
 * Phase 6A.5: Admin table for managing role upgrade approvals
 */
export function ApprovalsTable({ approvals, onUpdate }: ApprovalsTableProps) {
  const [selectedUser, setSelectedUser] = React.useState<PendingRoleUpgradeDto | null>(null);
  const [isRejectModalOpen, setIsRejectModalOpen] = React.useState(false);
  const [loadingUserId, setLoadingUserId] = React.useState<string | null>(null);

  const handleApprove = async (userId: string) => {
    try {
      setLoadingUserId(userId);
      await approvalsRepository.approveRoleUpgrade(userId);
      onUpdate();
    } catch (error) {
      console.error('Error approving role upgrade:', error);
      alert('Failed to approve role upgrade');
    } finally {
      setLoadingUserId(null);
    }
  };

  const handleRejectClick = (approval: PendingRoleUpgradeDto) => {
    setSelectedUser(approval);
    setIsRejectModalOpen(true);
  };

  const handleRejectConfirm = async (reason: string) => {
    if (!selectedUser) return;

    try {
      setLoadingUserId(selectedUser.userId);
      await approvalsRepository.rejectRoleUpgrade(selectedUser.userId, reason);
      setIsRejectModalOpen(false);
      setSelectedUser(null);
      onUpdate();
    } catch (error) {
      console.error('Error rejecting role upgrade:', error);
      alert('Failed to reject role upgrade');
    } finally {
      setLoadingUserId(null);
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  if (approvals.length === 0) {
    return (
      <div className="bg-white rounded-lg shadow p-8 text-center">
        <p className="text-gray-500 text-lg">No pending approvals</p>
      </div>
    );
  }

  return (
    <>
      <div className="bg-white rounded-lg shadow overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                User
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Email
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Current Role
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Requested Role
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Requested At
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {approvals.map((approval) => (
              <tr key={approval.userId} className="hover:bg-gray-50">
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm font-medium text-gray-900">{approval.fullName}</div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm text-gray-500">{approval.email}</div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-gray-100 text-gray-800">
                    {approval.currentRole}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-[#FFE8CC] text-[#8B1538]">
                    {approval.requestedRole}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  {formatDate(approval.requestedAt)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                  <div className="flex gap-2 justify-end">
                    <Button
                      size="sm"
                      variant="default"
                      onClick={() => handleApprove(approval.userId)}
                      disabled={loadingUserId === approval.userId}
                      className="bg-green-600 hover:bg-green-700 text-white"
                    >
                      {loadingUserId === approval.userId ? 'Approving...' : 'Approve'}
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => handleRejectClick(approval)}
                      disabled={loadingUserId === approval.userId}
                      className="border-red-600 text-red-600 hover:bg-red-50"
                    >
                      Reject
                    </Button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <RejectModal
        isOpen={isRejectModalOpen}
        userName={selectedUser?.fullName || ''}
        onClose={() => {
          setIsRejectModalOpen(false);
          setSelectedUser(null);
        }}
        onConfirm={handleRejectConfirm}
        isLoading={loadingUserId === selectedUser?.userId}
      />
    </>
  );
}
