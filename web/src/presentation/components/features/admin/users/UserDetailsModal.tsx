/**
 * Phase 6A.90: User Details Modal
 * Modal for viewing detailed user information
 */

import { X, User, Mail, Phone, MapPin, Shield, Calendar, Clock, CheckCircle, XCircle, AlertTriangle } from 'lucide-react';
import type { AdminUserDto } from '@/infrastructure/api/types/admin-users.types';
import { ROLE_BADGE_COLORS } from '@/infrastructure/api/types/admin-users.types';

interface UserDetailsModalProps {
  isOpen: boolean;
  user: AdminUserDto | null;
  onClose: () => void;
}

export function UserDetailsModal({ isOpen, user, onClose }: UserDetailsModalProps) {
  if (!isOpen || !user) return null;

  const formatDate = (dateString: string | null) => {
    if (!dateString) return 'Never';
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  const formatDateTime = (dateString: string | null) => {
    if (!dateString) return 'Never';
    return new Date(dateString).toLocaleString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const isLocked = () => {
    if (!user.accountLockedUntil) return false;
    return new Date(user.accountLockedUntil) > new Date();
  };

  const getRoleBadgeStyle = (role: string) => {
    const colors = ROLE_BADGE_COLORS[role] || { bg: '#E5E7EB', text: '#374151' };
    return {
      backgroundColor: colors.bg,
      color: colors.text,
    };
  };

  const DetailRow = ({ icon: Icon, label, value, highlight = false }: {
    icon: typeof User;
    label: string;
    value: React.ReactNode;
    highlight?: boolean;
  }) => (
    <div className="flex items-start gap-3 py-3 border-b border-gray-100 last:border-0">
      <div className={`p-2 rounded-lg ${highlight ? 'bg-[#8B1538]/10' : 'bg-gray-100'}`}>
        <Icon className={`w-4 h-4 ${highlight ? 'text-[#8B1538]' : 'text-gray-500'}`} />
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-xs text-gray-500 uppercase tracking-wide">{label}</p>
        <div className="text-sm font-medium text-gray-900 mt-0.5">{value}</div>
      </div>
    </div>
  );

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div className="bg-white rounded-lg shadow-xl max-w-lg w-full mx-4 max-h-[90vh] overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200 bg-gradient-to-r from-[#8B1538] to-[#FF7900]">
          <div className="flex items-center gap-3">
            <div className="h-12 w-12 bg-white/20 rounded-full flex items-center justify-center backdrop-blur">
              <span className="text-white font-semibold text-lg">
                {user.firstName?.charAt(0)?.toUpperCase() || '?'}
                {user.lastName?.charAt(0)?.toUpperCase() || ''}
              </span>
            </div>
            <div>
              <h3 className="text-lg font-semibold text-white">{user.fullName}</h3>
              <p className="text-white/80 text-sm">{user.email}</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="text-white/80 hover:text-white transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Status Banner */}
        {(isLocked() || !user.isActive) && (
          <div className={`px-6 py-3 ${isLocked() ? 'bg-amber-50' : 'bg-red-50'}`}>
            <div className="flex items-center gap-2">
              <AlertTriangle className={`w-4 h-4 ${isLocked() ? 'text-amber-600' : 'text-red-600'}`} />
              <span className={`text-sm font-medium ${isLocked() ? 'text-amber-800' : 'text-red-800'}`}>
                {isLocked()
                  ? `Account locked until ${formatDateTime(user.accountLockedUntil)}`
                  : 'Account is deactivated'}
              </span>
            </div>
          </div>
        )}

        {/* Content */}
        <div className="px-6 py-4 overflow-y-auto max-h-[60vh]">
          <div className="space-y-1">
            <DetailRow
              icon={Shield}
              label="Role"
              value={
                <span
                  className="px-2.5 py-1 text-xs font-medium rounded-full"
                  style={getRoleBadgeStyle(user.role)}
                >
                  {user.role}
                </span>
              }
              highlight
            />

            <DetailRow
              icon={User}
              label="Identity Provider"
              value={user.identityProvider === 'Local' ? 'Local Account' : user.identityProvider}
            />

            <DetailRow
              icon={Mail}
              label="Email Verified"
              value={
                user.isEmailVerified ? (
                  <span className="inline-flex items-center gap-1 text-green-600">
                    <CheckCircle className="w-4 h-4" />
                    Verified
                  </span>
                ) : (
                  <span className="inline-flex items-center gap-1 text-gray-400">
                    <XCircle className="w-4 h-4" />
                    Not Verified
                  </span>
                )
              }
            />

            {user.phone && (
              <DetailRow
                icon={Phone}
                label="Phone Number"
                value={user.phone}
              />
            )}

            {user.location && (
              <DetailRow
                icon={MapPin}
                label="Location"
                value={
                  <div>
                    {user.location.metroAreaName && <span>{user.location.metroAreaName}</span>}
                    {user.location.city && <span>, {user.location.city}</span>}
                    {user.location.state && <span className="text-gray-500"> ({user.location.state})</span>}
                  </div>
                }
              />
            )}

            <DetailRow
              icon={Calendar}
              label="Registered On"
              value={formatDate(user.createdAt)}
            />

            <DetailRow
              icon={Clock}
              label="Last Login"
              value={formatDateTime(user.lastLoginAt)}
            />

            <DetailRow
              icon={user.isActive ? CheckCircle : XCircle}
              label="Account Status"
              value={
                <span className={`inline-flex items-center gap-1 ${user.isActive ? 'text-green-600' : 'text-red-600'}`}>
                  {user.isActive ? (
                    <>
                      <CheckCircle className="w-4 h-4" />
                      Active
                    </>
                  ) : (
                    <>
                      <XCircle className="w-4 h-4" />
                      Inactive
                    </>
                  )}
                </span>
              }
            />
          </div>
        </div>

        {/* Footer */}
        <div className="flex justify-end px-6 py-4 border-t border-gray-200 bg-gray-50">
          <button
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
}
