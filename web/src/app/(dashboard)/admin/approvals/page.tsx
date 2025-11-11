'use client';

import * as React from 'react';
import { useRouter } from 'next/navigation';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { ApprovalsTable } from '@/presentation/components/features/admin/ApprovalsTable';
import { approvalsRepository } from '@/infrastructure/api/repositories/approvals.repository';
import type { PendingRoleUpgradeDto } from '@/infrastructure/api/types/approvals.types';

/**
 * Admin Approvals Page
 * Phase 6A.5: Admin-only page for managing role upgrade requests
 */
export default function ApprovalsPage() {
  const router = useRouter();
  const { user, isAuthenticated } = useAuthStore();
  const [approvals, setApprovals] = React.useState<PendingRoleUpgradeDto[]>([]);
  const [isLoading, setIsLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);

  // Check if user is Admin or AdminManager
  const isAdmin = user?.role === 'Admin' || user?.role === 'AdminManager';

  React.useEffect(() => {
    if (!isAuthenticated) {
      router.push('/login');
      return;
    }

    if (!isAdmin) {
      router.push('/dashboard');
      return;
    }

    loadApprovals();
  }, [isAuthenticated, isAdmin, router]);

  const loadApprovals = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await approvalsRepository.getPendingApprovals();
      setApprovals(data);
    } catch (err) {
      console.error('Error loading approvals:', err);
      setError('Failed to load pending approvals');
    } finally {
      setIsLoading(false);
    }
  };

  if (!isAuthenticated || !isAdmin) {
    return null; // Will redirect
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <div className="bg-white shadow">
        <div className="container mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-[#8B1538]">
                Role Upgrade Approvals
              </h1>
              <p className="mt-1 text-sm text-gray-500">
                Manage pending Event Organizer role requests
              </p>
            </div>
            <button
              onClick={loadApprovals}
              disabled={isLoading}
              className="px-4 py-2 bg-[#FF7900] text-white rounded-lg hover:bg-[#E66D00] transition-colors disabled:opacity-50"
            >
              {isLoading ? 'Refreshing...' : 'Refresh'}
            </button>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="container mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Stats */}
        <div className="mb-6 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
          <div className="bg-white overflow-hidden shadow rounded-lg">
            <div className="px-4 py-5 sm:p-6">
              <dt className="text-sm font-medium text-gray-500 truncate">
                Pending Requests
              </dt>
              <dd className="mt-1 text-3xl font-semibold text-[#8B1538]">
                {approvals.length}
              </dd>
            </div>
          </div>
        </div>

        {/* Error Message */}
        {error && (
          <div className="mb-6 bg-red-50 border border-red-200 rounded-lg p-4">
            <p className="text-red-800">{error}</p>
          </div>
        )}

        {/* Approvals Table */}
        {isLoading ? (
          <div className="bg-white rounded-lg shadow p-8 text-center">
            <p className="text-gray-500">Loading approvals...</p>
          </div>
        ) : (
          <ApprovalsTable approvals={approvals} onUpdate={loadApprovals} />
        )}
      </div>
    </div>
  );
}
