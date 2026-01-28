/**
 * Phase 6A.90: Users Table Component
 * Displays list of users with actions for admin management
 */

import { useState } from 'react';
import {
  Eye,
  UserX,
  UserCheck,
  Lock,
  Unlock,
  Mail,
  KeyRound,
  MoreVertical,
  CheckCircle,
  XCircle,
  AlertTriangle,
} from 'lucide-react';
import type { AdminUserDto } from '@/infrastructure/api/types/admin-users.types';
import { ROLE_BADGE_COLORS } from '@/infrastructure/api/types/admin-users.types';

interface UsersTableProps {
  users: AdminUserDto[];
  onViewDetails: (user: AdminUserDto) => void;
  onDeactivate: (userId: string) => void;
  onActivate: (userId: string) => void;
  onLock: (user: AdminUserDto) => void;
  onUnlock: (userId: string) => void;
  onResendVerification: (userId: string) => void;
  onForcePasswordReset: (userId: string) => void;
  loadingUserId: string | null;
  currentUserId: string;
}

export function UsersTable({
  users,
  onViewDetails,
  onDeactivate,
  onActivate,
  onLock,
  onUnlock,
  onResendVerification,
  onForcePasswordReset,
  loadingUserId,
  currentUserId,
}: UsersTableProps) {
  const [openMenuId, setOpenMenuId] = useState<string | null>(null);

  const formatDate = (dateString: string | null) => {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  const formatDateTime = (dateString: string | null) => {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const isLocked = (user: AdminUserDto) => {
    if (!user.accountLockedUntil) return false;
    return new Date(user.accountLockedUntil) > new Date();
  };

  const canManageUser = (user: AdminUserDto) => {
    // Cannot manage self
    if (user.userId === currentUserId) return false;
    // Admin cannot manage AdminManager or other Admins
    return true; // Backend enforces role hierarchy
  };

  const getRoleBadgeStyle = (role: string) => {
    const colors = ROLE_BADGE_COLORS[role] || { bg: '#E5E7EB', text: '#374151' };
    return {
      backgroundColor: colors.bg,
      color: colors.text,
    };
  };

  const getStatusBadge = (user: AdminUserDto) => {
    if (isLocked(user)) {
      return (
        <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-amber-100 text-amber-800">
          <AlertTriangle className="w-3 h-3" />
          Locked
        </span>
      );
    }
    if (!user.isActive) {
      return (
        <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-red-100 text-red-800">
          <XCircle className="w-3 h-3" />
          Inactive
        </span>
      );
    }
    return (
      <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
        <CheckCircle className="w-3 h-3" />
        Active
      </span>
    );
  };

  if (users.length === 0) {
    return (
      <div className="text-center py-12 bg-gray-50 rounded-lg">
        <p className="text-gray-500">No users found matching your criteria.</p>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="min-w-full divide-y divide-gray-200">
        <thead className="bg-gray-50">
          <tr>
            <th className="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">
              User
            </th>
            <th className="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">
              Role
            </th>
            <th className="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">
              Status
            </th>
            <th className="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">
              Email Verified
            </th>
            <th className="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">
              Last Login
            </th>
            <th className="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">
              Registered
            </th>
            <th className="px-6 py-3 text-right text-xs font-semibold text-gray-600 uppercase tracking-wider">
              Actions
            </th>
          </tr>
        </thead>
        <tbody className="bg-white divide-y divide-gray-200">
          {users.map((user) => (
            <tr key={user.userId} className="hover:bg-gray-50 transition-colors">
              {/* User Info */}
              <td className="px-6 py-4 whitespace-nowrap">
                <div className="flex items-center">
                  <div className="flex-shrink-0 h-10 w-10 bg-gray-200 rounded-full flex items-center justify-center">
                    <span className="text-gray-600 font-medium text-sm">
                      {user.firstName?.charAt(0)?.toUpperCase() || '?'}
                      {user.lastName?.charAt(0)?.toUpperCase() || ''}
                    </span>
                  </div>
                  <div className="ml-4">
                    <div className="text-sm font-medium text-gray-900">{user.fullName}</div>
                    <div className="text-sm text-gray-500">{user.email}</div>
                  </div>
                </div>
              </td>

              {/* Role */}
              <td className="px-6 py-4 whitespace-nowrap">
                <span
                  className="px-2 py-1 text-xs font-medium rounded-full"
                  style={getRoleBadgeStyle(user.role)}
                >
                  {user.role}
                </span>
              </td>

              {/* Status */}
              <td className="px-6 py-4 whitespace-nowrap">
                {getStatusBadge(user)}
              </td>

              {/* Email Verified */}
              <td className="px-6 py-4 whitespace-nowrap">
                {user.isEmailVerified ? (
                  <span className="inline-flex items-center gap-1 text-green-600">
                    <CheckCircle className="w-4 h-4" />
                    <span className="text-sm">Yes</span>
                  </span>
                ) : (
                  <span className="inline-flex items-center gap-1 text-gray-400">
                    <XCircle className="w-4 h-4" />
                    <span className="text-sm">No</span>
                  </span>
                )}
              </td>

              {/* Last Login */}
              <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                {formatDateTime(user.lastLoginAt)}
              </td>

              {/* Registered */}
              <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                {formatDate(user.createdAt)}
              </td>

              {/* Actions */}
              <td className="px-6 py-4 whitespace-nowrap text-right">
                <div className="flex items-center justify-end gap-2">
                  {/* View Details */}
                  <button
                    onClick={() => onViewDetails(user)}
                    className="p-2 text-gray-400 hover:text-[#8B1538] transition-colors"
                    title="View Details"
                  >
                    <Eye className="w-4 h-4" />
                  </button>

                  {/* More Actions Dropdown */}
                  {canManageUser(user) && (
                    <div className="relative">
                      <button
                        onClick={() => setOpenMenuId(openMenuId === user.userId ? null : user.userId)}
                        className="p-2 text-gray-400 hover:text-gray-600 transition-colors"
                        disabled={loadingUserId === user.userId}
                      >
                        {loadingUserId === user.userId ? (
                          <div className="w-4 h-4 border-2 border-gray-300 border-t-[#8B1538] rounded-full animate-spin" />
                        ) : (
                          <MoreVertical className="w-4 h-4" />
                        )}
                      </button>

                      {openMenuId === user.userId && (
                        <>
                          {/* Backdrop to close menu */}
                          <div
                            className="fixed inset-0 z-10"
                            onClick={() => setOpenMenuId(null)}
                          />
                          <div className="absolute right-0 mt-2 w-48 bg-white rounded-md shadow-lg ring-1 ring-black ring-opacity-5 z-20">
                            <div className="py-1">
                              {/* Activate/Deactivate */}
                              {user.isActive ? (
                                <button
                                  onClick={() => {
                                    onDeactivate(user.userId);
                                    setOpenMenuId(null);
                                  }}
                                  className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                                >
                                  <UserX className="w-4 h-4 text-red-500" />
                                  Deactivate
                                </button>
                              ) : (
                                <button
                                  onClick={() => {
                                    onActivate(user.userId);
                                    setOpenMenuId(null);
                                  }}
                                  className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                                >
                                  <UserCheck className="w-4 h-4 text-green-500" />
                                  Activate
                                </button>
                              )}

                              {/* Lock/Unlock */}
                              {isLocked(user) ? (
                                <button
                                  onClick={() => {
                                    onUnlock(user.userId);
                                    setOpenMenuId(null);
                                  }}
                                  className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                                >
                                  <Unlock className="w-4 h-4 text-green-500" />
                                  Unlock
                                </button>
                              ) : (
                                <button
                                  onClick={() => {
                                    onLock(user);
                                    setOpenMenuId(null);
                                  }}
                                  className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                                >
                                  <Lock className="w-4 h-4 text-amber-500" />
                                  Lock Account
                                </button>
                              )}

                              <div className="border-t border-gray-100 my-1" />

                              {/* Resend Verification */}
                              {!user.isEmailVerified && (
                                <button
                                  onClick={() => {
                                    onResendVerification(user.userId);
                                    setOpenMenuId(null);
                                  }}
                                  className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                                >
                                  <Mail className="w-4 h-4 text-blue-500" />
                                  Resend Verification
                                </button>
                              )}

                              {/* Force Password Reset */}
                              <button
                                onClick={() => {
                                  onForcePasswordReset(user.userId);
                                  setOpenMenuId(null);
                                }}
                                className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                              >
                                <KeyRound className="w-4 h-4 text-purple-500" />
                                Force Password Reset
                              </button>
                            </div>
                          </div>
                        </>
                      )}
                    </div>
                  )}

                  {/* Self indicator */}
                  {user.userId === currentUserId && (
                    <span className="text-xs text-gray-400 italic ml-2">(You)</span>
                  )}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
