'use client';

import * as React from 'react';
import { Mail, Plus, Edit2, Trash2, Users, AlertCircle } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { useEmailGroups, useDeleteEmailGroup } from '@/presentation/hooks/useEmailGroups';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { isAdmin } from '@/infrastructure/api/utils/role-helpers';
import { UserRole } from '@/infrastructure/api/types/auth.types';
import { EmailGroupModal } from './EmailGroupModal';
import type { EmailGroupDto } from '@/infrastructure/api/types/email-groups.types';

/**
 * Phase 6A.25: Email Groups Tab Component
 * Dashboard tab for managing email groups
 *
 * Features:
 * - List all email groups (own for organizers, all for admins)
 * - Create new email groups
 * - Edit existing email groups
 * - Delete email groups (with confirmation)
 * - Empty state with call-to-action
 * - Loading and error states
 */

export function EmailGroupsTab() {
  const { user } = useAuthStore();
  const userIsAdmin = user && isAdmin(user.role as UserRole);

  // For admins, we can show all groups; for organizers, just their own
  const { data: emailGroups = [], isLoading, error, refetch } = useEmailGroups(userIsAdmin || false);

  const deleteEmailGroup = useDeleteEmailGroup();

  const [isModalOpen, setIsModalOpen] = React.useState(false);
  const [editingGroup, setEditingGroup] = React.useState<EmailGroupDto | undefined>(undefined);
  const [deletingGroupId, setDeletingGroupId] = React.useState<string | null>(null);

  const handleCreateClick = () => {
    setEditingGroup(undefined);
    setIsModalOpen(true);
  };

  const handleEditClick = (group: EmailGroupDto) => {
    setEditingGroup(group);
    setIsModalOpen(true);
  };

  const handleDeleteClick = async (groupId: string) => {
    if (!window.confirm('Are you sure you want to delete this email group?')) {
      return;
    }

    setDeletingGroupId(groupId);
    try {
      await deleteEmailGroup.mutateAsync(groupId);
    } catch (err) {
      console.error('Failed to delete email group:', err);
    } finally {
      setDeletingGroupId(null);
    }
  };

  const handleModalClose = () => {
    setIsModalOpen(false);
    setEditingGroup(undefined);
  };

  // Loading state
  if (isLoading) {
    return (
      <div className="p-6">
        <div className="animate-pulse space-y-4">
          <div className="h-10 bg-gray-200 rounded w-48" />
          <div className="h-24 bg-gray-200 rounded" />
          <div className="h-24 bg-gray-200 rounded" />
        </div>
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div className="p-6">
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg flex items-start gap-3">
          <AlertCircle className="w-5 h-5 text-red-500 flex-shrink-0 mt-0.5" />
          <div>
            <p className="text-red-700 font-medium">Failed to load email groups</p>
            <p className="text-red-600 text-sm mt-1">
              {error instanceof Error ? error.message : 'An unexpected error occurred'}
            </p>
            <Button
              variant="outline"
              size="sm"
              className="mt-3"
              onClick={() => refetch()}
            >
              Try Again
            </Button>
          </div>
        </div>
      </div>
    );
  }

  // Empty state
  if (emailGroups.length === 0) {
    return (
      <div className="p-6">
        <div className="text-center py-12 border-2 border-dashed border-gray-200 rounded-xl">
          <div className="w-16 h-16 bg-orange-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <Mail className="w-8 h-8 text-[#FF7900]" />
          </div>
          <h3 className="text-xl font-semibold text-[#8B1538] mb-2">No Email Groups Yet</h3>
          <p className="text-gray-600 mb-6 max-w-md mx-auto">
            Create email groups to organize your contacts for event announcements, invitations, and
            marketing communications.
          </p>
          <Button
            onClick={handleCreateClick}
            style={{
              background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)',
              color: 'white',
            }}
          >
            <Plus className="w-4 h-4 mr-2" />
            Create Your First Email Group
          </Button>
        </div>

        <EmailGroupModal
          isOpen={isModalOpen}
          onClose={handleModalClose}
          emailGroup={editingGroup}
        />
      </div>
    );
  }

  // List view
  return (
    <div className="p-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <div>
          <h3 className="text-lg font-semibold text-[#8B1538]">Email Groups</h3>
          <p className="text-sm text-gray-600">
            {emailGroups.length} group{emailGroups.length !== 1 ? 's' : ''} total
          </p>
        </div>
        <Button
          onClick={handleCreateClick}
          style={{
            background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)',
            color: 'white',
          }}
        >
          <Plus className="w-4 h-4 mr-2" />
          Create Email Group
        </Button>
      </div>

      {/* Email Groups List */}
      <div className="space-y-4">
        {emailGroups.map((group) => (
          <div
            key={group.id}
            className="bg-white border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow"
          >
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-1">
                  <Mail className="w-4 h-4 text-[#FF7900] flex-shrink-0" />
                  <h4 className="font-semibold text-gray-900 truncate">{group.name}</h4>
                </div>
                {group.description && (
                  <p className="text-sm text-gray-600 mb-2 line-clamp-2">{group.description}</p>
                )}
                <div className="flex items-center gap-4 text-sm text-gray-500">
                  <span className="flex items-center gap-1">
                    <Users className="w-4 h-4" />
                    {group.emailCount} email{group.emailCount !== 1 ? 's' : ''}
                  </span>
                  <span>
                    Created {new Date(group.createdAt).toLocaleDateString()}
                  </span>
                  {userIsAdmin && group.ownerName && (
                    <span className="text-[#8B1538]">
                      Owner: {group.ownerName}
                    </span>
                  )}
                </div>
              </div>

              {/* Actions */}
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handleEditClick(group)}
                  disabled={deletingGroupId === group.id}
                >
                  <Edit2 className="w-4 h-4 mr-1" />
                  Edit
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handleDeleteClick(group.id)}
                  disabled={deletingGroupId === group.id}
                  className="text-red-600 hover:text-red-700 hover:bg-red-50 border-red-200"
                >
                  {deletingGroupId === group.id ? (
                    <span className="animate-pulse">Deleting...</span>
                  ) : (
                    <>
                      <Trash2 className="w-4 h-4 mr-1" />
                      Delete
                    </>
                  )}
                </Button>
              </div>
            </div>

            {/* Preview of emails (collapsed) */}
            <div className="mt-3 pt-3 border-t border-gray-100">
              <p className="text-xs text-gray-500 font-mono truncate">
                {group.emailAddresses}
              </p>
            </div>
          </div>
        ))}
      </div>

      <EmailGroupModal
        isOpen={isModalOpen}
        onClose={handleModalClose}
        emailGroup={editingGroup}
      />
    </div>
  );
}
