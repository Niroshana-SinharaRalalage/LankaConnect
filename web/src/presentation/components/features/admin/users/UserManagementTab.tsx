/**
 * Phase 6A.90: User Management Tab
 * Main tab component for admin user management
 */

'use client';

import { useState, useMemo, useCallback } from 'react';
import { Search, Users, UserCheck, Lock, RefreshCw, ChevronLeft, ChevronRight, CheckCircle, XCircle } from 'lucide-react';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import {
  useAdminUsers,
  useAdminUserStatistics,
  useDeactivateUser,
  useActivateUser,
  useLockUser,
  useUnlockUser,
  useResendVerification,
  useForcePasswordReset,
} from '@/presentation/hooks/useAdminUsers';
import type { AdminUserDto, GetAdminUsersRequest } from '@/infrastructure/api/types/admin-users.types';
import { USER_ROLES } from '@/infrastructure/api/types/admin-users.types';
import { UsersTable } from './UsersTable';
import { UserDetailsModal } from './UserDetailsModal';
import { LockUserModal } from './LockUserModal';
import { ConfirmDialog } from '@/presentation/components/ui/ConfirmDialog';

export function UserManagementTab() {
  const { user: currentUser } = useAuthStore();

  // Filter state
  const [filters, setFilters] = useState<GetAdminUsersRequest>({
    page: 1,
    pageSize: 10,
    searchTerm: '',
    roleFilter: '',
    isActiveFilter: null,
  });

  // Modal state
  const [selectedUser, setSelectedUser] = useState<AdminUserDto | null>(null);
  const [isDetailsModalOpen, setIsDetailsModalOpen] = useState(false);
  const [isLockModalOpen, setIsLockModalOpen] = useState(false);
  const [loadingUserId, setLoadingUserId] = useState<string | null>(null);

  // GitHub Issue #31: Replace native confirm()/alert() with styled components
  const [showDeactivateDialog, setShowDeactivateDialog] = useState(false);
  const [showPasswordResetDialog, setShowPasswordResetDialog] = useState(false);
  const [actionUserId, setActionUserId] = useState<string | null>(null);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  // Auto-dismiss notification after 5 seconds
  const showNotification = (type: 'success' | 'error', message: string) => {
    setNotification({ type, message });
    setTimeout(() => setNotification(null), 5000);
  };

  // Debounced search
  const [searchInput, setSearchInput] = useState('');

  // Queries
  const { data: usersData, isLoading, error, refetch } = useAdminUsers(filters);
  const { data: statistics } = useAdminUserStatistics();

  // Mutations
  const deactivateMutation = useDeactivateUser();
  const activateMutation = useActivateUser();
  const lockMutation = useLockUser();
  const unlockMutation = useUnlockUser();
  const resendVerificationMutation = useResendVerification();
  const forcePasswordResetMutation = useForcePasswordReset();

  // Handlers
  const handleSearch = useCallback((value: string) => {
    setSearchInput(value);
    // Debounce search
    const timeoutId = setTimeout(() => {
      setFilters((prev) => ({ ...prev, searchTerm: value, page: 1 }));
    }, 300);
    return () => clearTimeout(timeoutId);
  }, []);

  const handleRoleFilter = (role: string) => {
    setFilters((prev) => ({ ...prev, roleFilter: role || '', page: 1 }));
  };

  const handleStatusFilter = (status: string) => {
    let isActiveFilter: boolean | null = null;
    if (status === 'active') isActiveFilter = true;
    if (status === 'inactive') isActiveFilter = false;
    setFilters((prev) => ({ ...prev, isActiveFilter, page: 1 }));
  };

  const handlePageChange = (newPage: number) => {
    setFilters((prev) => ({ ...prev, page: newPage }));
  };

  const handleViewDetails = (user: AdminUserDto) => {
    setSelectedUser(user);
    setIsDetailsModalOpen(true);
  };

  const handleDeactivate = (userId: string) => {
    setActionUserId(userId);
    setShowDeactivateDialog(true);
  };

  const confirmDeactivate = async () => {
    if (!actionUserId) return;
    setLoadingUserId(actionUserId);
    try {
      await deactivateMutation.mutateAsync(actionUserId);
      showNotification('success', 'User deactivated successfully.');
    } catch (error) {
      console.error('Failed to deactivate user:', error);
      showNotification('error', 'Failed to deactivate user. Please try again.');
    } finally {
      setLoadingUserId(null);
      setActionUserId(null);
    }
  };

  const handleActivate = async (userId: string) => {
    setLoadingUserId(userId);
    try {
      await activateMutation.mutateAsync(userId);
      showNotification('success', 'User activated successfully.');
    } catch (error) {
      console.error('Failed to activate user:', error);
      showNotification('error', 'Failed to activate user. Please try again.');
    } finally {
      setLoadingUserId(null);
    }
  };

  const handleOpenLockModal = (user: AdminUserDto) => {
    setSelectedUser(user);
    setIsLockModalOpen(true);
  };

  const handleLock = async (lockUntil: string, reason?: string) => {
    if (!selectedUser) return;
    setLoadingUserId(selectedUser.userId);
    try {
      await lockMutation.mutateAsync({
        userId: selectedUser.userId,
        request: { lockUntil, reason },
      });
      setIsLockModalOpen(false);
      setSelectedUser(null);
      showNotification('success', 'User account locked successfully.');
    } catch (error) {
      console.error('Failed to lock user:', error);
      showNotification('error', 'Failed to lock user. Please try again.');
    } finally {
      setLoadingUserId(null);
    }
  };

  const handleUnlock = async (userId: string) => {
    setLoadingUserId(userId);
    try {
      await unlockMutation.mutateAsync(userId);
      showNotification('success', 'User account unlocked successfully.');
    } catch (error) {
      console.error('Failed to unlock user:', error);
      showNotification('error', 'Failed to unlock user. Please try again.');
    } finally {
      setLoadingUserId(null);
    }
  };

  const handleResendVerification = async (userId: string) => {
    setLoadingUserId(userId);
    try {
      await resendVerificationMutation.mutateAsync(userId);
      showNotification('success', 'Verification email sent successfully.');
    } catch (error) {
      console.error('Failed to resend verification:', error);
      showNotification('error', 'Failed to send verification email. Please try again.');
    } finally {
      setLoadingUserId(null);
    }
  };

  const handleForcePasswordReset = (userId: string) => {
    setActionUserId(userId);
    setShowPasswordResetDialog(true);
  };

  const confirmPasswordReset = async () => {
    if (!actionUserId) return;
    setLoadingUserId(actionUserId);
    try {
      await forcePasswordResetMutation.mutateAsync(actionUserId);
      showNotification('success', 'Password reset email sent successfully.');
    } catch (error) {
      console.error('Failed to force password reset:', error);
      showNotification('error', 'Failed to send password reset email. Please try again.');
    } finally {
      setLoadingUserId(null);
      setActionUserId(null);
    }
  };

  // Computed values
  const totalPages = usersData ? Math.ceil(usersData.totalCount / (filters.pageSize || 10)) : 0;

  return (
    <div className="space-y-6">
      {/* Statistics Cards */}
      {statistics && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <StatCard
            icon={Users}
            label="Total Users"
            value={statistics.totalUsers}
            color="blue"
          />
          <StatCard
            icon={UserCheck}
            label="Active Users"
            value={statistics.activeUsers}
            color="green"
          />
          <StatCard
            icon={Lock}
            label="Locked Accounts"
            value={statistics.lockedAccounts}
            color="amber"
          />
          <StatCard
            icon={Users}
            label="Admins"
            value={(statistics.usersByRole?.['Admin'] || 0) + (statistics.usersByRole?.['AdminManager'] || 0)}
            color="purple"
          />
        </div>
      )}

      {/* Filters */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
        <div className="flex flex-col md:flex-row gap-4">
          {/* Search */}
          <div className="flex-1">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                type="text"
                value={searchInput}
                onChange={(e) => handleSearch(e.target.value)}
                placeholder="Search by name or email..."
                className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-[#8B1538] focus:border-[#8B1538]"
              />
            </div>
          </div>

          {/* Role Filter */}
          <div className="w-full md:w-48">
            <select
              value={filters.roleFilter || ''}
              onChange={(e) => handleRoleFilter(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-[#8B1538] focus:border-[#8B1538]"
            >
              <option value="">All Roles</option>
              {Object.entries(USER_ROLES).map(([value, label]) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </select>
          </div>

          {/* Status Filter */}
          <div className="w-full md:w-40">
            <select
              value={
                filters.isActiveFilter === true ? 'active' :
                filters.isActiveFilter === false ? 'inactive' : ''
              }
              onChange={(e) => handleStatusFilter(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-[#8B1538] focus:border-[#8B1538]"
            >
              <option value="">All Status</option>
              <option value="active">Active</option>
              <option value="inactive">Inactive</option>
            </select>
          </div>

          {/* Refresh Button */}
          <button
            onClick={() => refetch()}
            disabled={isLoading}
            className="flex items-center justify-center gap-2 px-4 py-2 text-gray-600 bg-gray-100 rounded-md hover:bg-gray-200 transition-colors disabled:opacity-50"
          >
            <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} />
            <span className="hidden sm:inline">Refresh</span>
          </button>
        </div>
      </div>

      {/* Users Table */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        {isLoading ? (
          <div className="flex items-center justify-center py-12">
            <div className="w-8 h-8 border-4 border-gray-200 border-t-[#8B1538] rounded-full animate-spin" />
          </div>
        ) : error ? (
          <div className="text-center py-12 text-red-600">
            <p>Failed to load users. Please try again.</p>
            <button
              onClick={() => refetch()}
              className="mt-4 px-4 py-2 text-sm text-white bg-[#8B1538] rounded-md hover:bg-[#6d1029]"
            >
              Retry
            </button>
          </div>
        ) : usersData ? (
          <>
            <UsersTable
              users={usersData.items}
              onViewDetails={handleViewDetails}
              onDeactivate={handleDeactivate}
              onActivate={handleActivate}
              onLock={handleOpenLockModal}
              onUnlock={handleUnlock}
              onResendVerification={handleResendVerification}
              onForcePasswordReset={handleForcePasswordReset}
              loadingUserId={loadingUserId}
              currentUserId={currentUser?.userId || ''}
              currentUserRole={currentUser?.role || ''}
            />

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex items-center justify-between px-6 py-4 border-t border-gray-200 bg-gray-50">
                <div className="text-sm text-gray-600">
                  Showing {((filters.page || 1) - 1) * (filters.pageSize || 10) + 1} to{' '}
                  {Math.min((filters.page || 1) * (filters.pageSize || 10), usersData.totalCount)} of{' '}
                  {usersData.totalCount} users
                </div>
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => handlePageChange((filters.page || 1) - 1)}
                    disabled={(filters.page || 1) <= 1}
                    className="p-2 text-gray-600 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <ChevronLeft className="w-4 h-4" />
                  </button>
                  <span className="text-sm text-gray-600">
                    Page {filters.page || 1} of {totalPages}
                  </span>
                  <button
                    onClick={() => handlePageChange((filters.page || 1) + 1)}
                    disabled={(filters.page || 1) >= totalPages}
                    className="p-2 text-gray-600 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <ChevronRight className="w-4 h-4" />
                  </button>
                </div>
              </div>
            )}
          </>
        ) : null}
      </div>

      {/* Modals */}
      <UserDetailsModal
        isOpen={isDetailsModalOpen}
        user={selectedUser}
        onClose={() => {
          setIsDetailsModalOpen(false);
          setSelectedUser(null);
        }}
      />

      <LockUserModal
        isOpen={isLockModalOpen}
        userName={selectedUser?.fullName || ''}
        onClose={() => {
          setIsLockModalOpen(false);
          setSelectedUser(null);
        }}
        onConfirm={handleLock}
        isLoading={!!loadingUserId}
      />

      {/* GitHub Issue #31: Styled notification display instead of alert() */}
      {notification && (
        <div
          className={`fixed bottom-4 right-4 z-50 flex items-center gap-3 px-4 py-3 rounded-lg shadow-lg ${
            notification.type === 'success'
              ? 'bg-green-50 border border-green-200 text-green-800'
              : 'bg-red-50 border border-red-200 text-red-800'
          }`}
        >
          {notification.type === 'success' ? (
            <CheckCircle className="w-5 h-5 text-green-600" />
          ) : (
            <XCircle className="w-5 h-5 text-red-600" />
          )}
          <span className="text-sm font-medium">{notification.message}</span>
          <button
            onClick={() => setNotification(null)}
            className="ml-2 text-gray-400 hover:text-gray-600"
          >
            ×
          </button>
        </div>
      )}

      {/* GitHub Issue #31: Styled confirmation dialogs */}
      <ConfirmDialog
        open={showDeactivateDialog}
        onOpenChange={setShowDeactivateDialog}
        title="Deactivate User"
        description="Are you sure you want to deactivate this user? They will no longer be able to access the platform."
        confirmLabel="Yes, Deactivate"
        cancelLabel="Cancel"
        variant="danger"
        onConfirm={confirmDeactivate}
        isLoading={!!loadingUserId}
      />

      <ConfirmDialog
        open={showPasswordResetDialog}
        onOpenChange={setShowPasswordResetDialog}
        title="Force Password Reset"
        description="Are you sure you want to force a password reset for this user? They will receive an email with instructions to reset their password."
        confirmLabel="Yes, Send Reset Email"
        cancelLabel="Cancel"
        variant="warning"
        onConfirm={confirmPasswordReset}
        isLoading={!!loadingUserId}
      />
    </div>
  );
}

// Statistics Card Component
function StatCard({
  icon: Icon,
  label,
  value,
  color,
}: {
  icon: typeof Users;
  label: string;
  value: number;
  color: 'blue' | 'green' | 'amber' | 'purple';
}) {
  const colorStyles = {
    blue: 'bg-blue-50 text-blue-600',
    green: 'bg-green-50 text-green-600',
    amber: 'bg-amber-50 text-amber-600',
    purple: 'bg-purple-50 text-purple-600',
  };

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
      <div className="flex items-center gap-3">
        <div className={`p-2 rounded-lg ${colorStyles[color]}`}>
          <Icon className="w-5 h-5" />
        </div>
        <div>
          <p className="text-sm text-gray-500">{label}</p>
          <p className="text-2xl font-semibold text-gray-900">{value}</p>
        </div>
      </div>
    </div>
  );
}
