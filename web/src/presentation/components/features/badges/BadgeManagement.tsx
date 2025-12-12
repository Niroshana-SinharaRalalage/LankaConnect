'use client';

import * as React from 'react';
import Image from 'next/image';
import { Plus, Pencil, Trash2, Check, X, Upload, Loader2, AlertCircle } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/presentation/components/ui/Dialog';
import {
  useBadges,
  useCreateBadge,
  useUpdateBadge,
  useUpdateBadgeImage,
  useDeleteBadge,
} from '@/presentation/hooks/useBadges';
import {
  BadgePosition,
  getPositionDisplayName,
  type BadgeDto,
  type UpdateBadgeDto,
} from '@/infrastructure/api/types/badges.types';

/**
 * Phase 6A.26: Badge Management Component
 * Full implementation for managing promotional badges
 * Features:
 * - List all badges in a grid/table format
 * - Create new badge with image upload
 * - Edit badge details (name, position, active status)
 * - Delete custom badges (system badges can only be deactivated)
 * - Preview badge overlay
 */
export function BadgeManagement() {
  // Fetch all badges (including inactive for admin management)
  const { data: badges, isLoading, error, refetch } = useBadges(false);

  // Mutations
  const createBadge = useCreateBadge();
  const updateBadge = useUpdateBadge();
  const updateBadgeImage = useUpdateBadgeImage();
  const deleteBadge = useDeleteBadge();

  // Dialog states
  const [isCreateOpen, setIsCreateOpen] = React.useState(false);
  const [isEditOpen, setIsEditOpen] = React.useState(false);
  const [isDeleteOpen, setIsDeleteOpen] = React.useState(false);
  const [selectedBadge, setSelectedBadge] = React.useState<BadgeDto | null>(null);

  // Form states
  const [newBadgeName, setNewBadgeName] = React.useState('');
  const [newBadgePosition, setNewBadgePosition] = React.useState<BadgePosition>(BadgePosition.TopRight);
  const [newBadgeFile, setNewBadgeFile] = React.useState<File | null>(null);
  const [editBadgeName, setEditBadgeName] = React.useState('');
  const [editBadgePosition, setEditBadgePosition] = React.useState<BadgePosition>(BadgePosition.TopRight);
  const [editBadgeActive, setEditBadgeActive] = React.useState(true);
  const [editBadgeFile, setEditBadgeFile] = React.useState<File | null>(null);

  // File input refs
  const createFileInputRef = React.useRef<HTMLInputElement>(null);
  const editFileInputRef = React.useRef<HTMLInputElement>(null);

  // Loading states
  const isCreating = createBadge.isPending;
  const isUpdating = updateBadge.isPending || updateBadgeImage.isPending;
  const isDeleting = deleteBadge.isPending;

  // Handle create badge
  const handleCreate = async () => {
    if (!newBadgeName.trim() || !newBadgeFile) {
      return;
    }

    try {
      await createBadge.mutateAsync({
        name: newBadgeName.trim(),
        position: newBadgePosition,
        imageFile: newBadgeFile,
      });

      // Reset form and close dialog
      setNewBadgeName('');
      setNewBadgePosition(BadgePosition.TopRight);
      setNewBadgeFile(null);
      setIsCreateOpen(false);
    } catch (err) {
      console.error('Failed to create badge:', err);
    }
  };

  // Handle edit badge
  const handleEdit = async () => {
    if (!selectedBadge || !editBadgeName.trim()) {
      return;
    }

    try {
      // Update badge details
      const dto: UpdateBadgeDto = {
        name: editBadgeName.trim(),
        position: editBadgePosition,
        isActive: editBadgeActive,
      };
      await updateBadge.mutateAsync({ badgeId: selectedBadge.id, dto });

      // Update badge image if new file selected
      if (editBadgeFile) {
        await updateBadgeImage.mutateAsync({
          badgeId: selectedBadge.id,
          imageFile: editBadgeFile,
        });
      }

      // Reset and close
      setSelectedBadge(null);
      setEditBadgeFile(null);
      setIsEditOpen(false);
    } catch (err) {
      console.error('Failed to update badge:', err);
    }
  };

  // Handle delete badge
  const handleDelete = async () => {
    if (!selectedBadge) return;

    try {
      await deleteBadge.mutateAsync(selectedBadge.id);
      setSelectedBadge(null);
      setIsDeleteOpen(false);
    } catch (err) {
      console.error('Failed to delete badge:', err);
    }
  };

  // Open edit dialog
  const openEditDialog = (badge: BadgeDto) => {
    setSelectedBadge(badge);
    setEditBadgeName(badge.name);
    setEditBadgePosition(badge.position);
    setEditBadgeActive(badge.isActive);
    setEditBadgeFile(null);
    setIsEditOpen(true);
  };

  // Open delete dialog
  const openDeleteDialog = (badge: BadgeDto) => {
    setSelectedBadge(badge);
    setIsDeleteOpen(true);
  };

  // Toggle active status directly
  const handleToggleActive = async (badge: BadgeDto) => {
    try {
      await updateBadge.mutateAsync({
        badgeId: badge.id,
        dto: { isActive: !badge.isActive },
      });
    } catch (err) {
      console.error('Failed to toggle badge status:', err);
    }
  };

  // Render loading state
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="w-8 h-8 animate-spin" style={{ color: '#FF7900' }} />
        <span className="ml-2 text-gray-600">Loading badges...</span>
      </div>
    );
  }

  // Render error state
  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
        <AlertCircle className="w-8 h-8 mx-auto mb-2 text-red-500" />
        <p className="text-red-700">Failed to load badges. Please try again.</p>
        <Button onClick={() => refetch()} className="mt-4" variant="outline">
          Retry
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header with Create Button */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold" style={{ color: '#8B1538' }}>
            Badge Management
          </h2>
          <p className="text-sm text-gray-500 mt-1">
            Create and manage promotional badges for events
          </p>
        </div>
        <Button
          onClick={() => setIsCreateOpen(true)}
          className="gap-2"
          style={{ background: '#FF7900', color: 'white' }}
        >
          <Plus className="w-4 h-4" />
          Create Badge
        </Button>
      </div>

      {/* Badges Grid */}
      {!badges || badges.length === 0 ? (
        <div className="bg-white rounded-lg shadow p-8 text-center">
          <p className="text-gray-500">No badges found. Create your first badge!</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          {badges.map((badge) => (
            <div
              key={badge.id}
              className={`bg-white rounded-lg shadow-sm border overflow-hidden transition-all hover:shadow-md ${
                !badge.isActive ? 'opacity-60' : ''
              }`}
            >
              {/* Badge Image Preview */}
              <div className="relative aspect-square bg-gray-100 flex items-center justify-center">
                {badge.imageUrl ? (
                  <Image
                    src={badge.imageUrl}
                    alt={badge.name}
                    width={120}
                    height={120}
                    className="object-contain max-h-[120px]"
                    unoptimized // Azure blob storage
                  />
                ) : (
                  <div className="text-gray-400 text-sm">No image</div>
                )}

                {/* Position indicator */}
                <div
                  className="absolute px-2 py-1 text-xs font-medium rounded"
                  style={{
                    background: 'rgba(139, 21, 56, 0.9)',
                    color: 'white',
                    ...getIndicatorPosition(badge.position),
                  }}
                >
                  {getPositionDisplayName(badge.position)}
                </div>

                {/* System badge indicator */}
                {badge.isSystem && (
                  <div
                    className="absolute top-2 left-2 px-2 py-0.5 text-xs font-medium rounded"
                    style={{ background: '#FF7900', color: 'white' }}
                  >
                    System
                  </div>
                )}
              </div>

              {/* Badge Info */}
              <div className="p-4">
                <div className="flex items-center justify-between mb-2">
                  <h3 className="font-medium text-gray-900 truncate" title={badge.name}>
                    {badge.name}
                  </h3>
                  <button
                    onClick={() => handleToggleActive(badge)}
                    className={`px-2 py-0.5 text-xs rounded-full transition-colors ${
                      badge.isActive
                        ? 'bg-green-100 text-green-700 hover:bg-green-200'
                        : 'bg-gray-100 text-gray-500 hover:bg-gray-200'
                    }`}
                    disabled={isUpdating}
                  >
                    {badge.isActive ? 'Active' : 'Inactive'}
                  </button>
                </div>

                {/* Action Buttons */}
                <div className="flex items-center gap-2 mt-3">
                  <Button
                    variant="outline"
                    size="sm"
                    className="flex-1 gap-1"
                    onClick={() => openEditDialog(badge)}
                  >
                    <Pencil className="w-3 h-3" />
                    Edit
                  </Button>
                  {!badge.isSystem && (
                    <Button
                      variant="outline"
                      size="sm"
                      className="text-red-600 hover:bg-red-50 hover:text-red-700"
                      onClick={() => openDeleteDialog(badge)}
                    >
                      <Trash2 className="w-3 h-3" />
                    </Button>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Create Badge Dialog */}
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Create New Badge</DialogTitle>
            <DialogDescription>
              Upload a badge image and set its display properties.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            {/* Badge Name */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Badge Name
              </label>
              <Input
                placeholder="e.g., New Event, Sale, Featured"
                value={newBadgeName}
                onChange={(e) => setNewBadgeName(e.target.value)}
                maxLength={50}
              />
            </div>

            {/* Badge Position */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Position on Image
              </label>
              <select
                value={newBadgePosition}
                onChange={(e) => setNewBadgePosition(Number(e.target.value) as BadgePosition)}
                className="w-full h-10 rounded-md border border-input bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              >
                <option value={BadgePosition.TopLeft}>Top Left</option>
                <option value={BadgePosition.TopRight}>Top Right</option>
                <option value={BadgePosition.BottomLeft}>Bottom Left</option>
                <option value={BadgePosition.BottomRight}>Bottom Right</option>
              </select>
            </div>

            {/* Badge Image Upload */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Badge Image (PNG recommended)
              </label>
              <input
                ref={createFileInputRef}
                type="file"
                accept="image/png,image/jpeg,image/webp"
                onChange={(e) => setNewBadgeFile(e.target.files?.[0] || null)}
                className="hidden"
              />
              <div
                onClick={() => createFileInputRef.current?.click()}
                className="border-2 border-dashed border-gray-300 rounded-lg p-6 text-center cursor-pointer hover:border-gray-400 transition-colors"
              >
                {newBadgeFile ? (
                  <div className="flex items-center justify-center gap-2">
                    <Check className="w-5 h-5 text-green-500" />
                    <span className="text-sm text-gray-700">{newBadgeFile.name}</span>
                  </div>
                ) : (
                  <>
                    <Upload className="w-8 h-8 mx-auto mb-2 text-gray-400" />
                    <p className="text-sm text-gray-500">
                      Click to upload badge image
                    </p>
                    <p className="text-xs text-gray-400 mt-1">
                      Recommended: 80-120px PNG with transparency
                    </p>
                  </>
                )}
              </div>
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsCreateOpen(false)}
              disabled={isCreating}
            >
              Cancel
            </Button>
            <Button
              onClick={handleCreate}
              disabled={!newBadgeName.trim() || !newBadgeFile || isCreating}
              loading={isCreating}
              style={{ background: '#FF7900', color: 'white' }}
            >
              Create Badge
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Badge Dialog */}
      <Dialog open={isEditOpen} onOpenChange={setIsEditOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Edit Badge</DialogTitle>
            <DialogDescription>
              Update badge properties. {selectedBadge?.isSystem && '(System badge - some options limited)'}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            {/* Current Image Preview */}
            {selectedBadge?.imageUrl && (
              <div className="flex items-center gap-4">
                <div className="w-16 h-16 bg-gray-100 rounded-lg flex items-center justify-center">
                  <Image
                    src={selectedBadge.imageUrl}
                    alt={selectedBadge.name}
                    width={48}
                    height={48}
                    className="object-contain"
                    unoptimized
                  />
                </div>
                <div className="text-sm text-gray-500">Current badge image</div>
              </div>
            )}

            {/* Badge Name */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Badge Name
              </label>
              <Input
                placeholder="Badge name"
                value={editBadgeName}
                onChange={(e) => setEditBadgeName(e.target.value)}
                maxLength={50}
                disabled={selectedBadge?.isSystem}
              />
              {selectedBadge?.isSystem && (
                <p className="text-xs text-gray-400 mt-1">System badge names cannot be changed</p>
              )}
            </div>

            {/* Badge Position */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Position on Image
              </label>
              <select
                value={editBadgePosition}
                onChange={(e) => setEditBadgePosition(Number(e.target.value) as BadgePosition)}
                className="w-full h-10 rounded-md border border-input bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                disabled={selectedBadge?.isSystem}
              >
                <option value={BadgePosition.TopLeft}>Top Left</option>
                <option value={BadgePosition.TopRight}>Top Right</option>
                <option value={BadgePosition.BottomLeft}>Bottom Left</option>
                <option value={BadgePosition.BottomRight}>Bottom Right</option>
              </select>
            </div>

            {/* Active Status */}
            <div className="flex items-center gap-3">
              <label className="text-sm font-medium text-gray-700">Active Status</label>
              <button
                type="button"
                onClick={() => setEditBadgeActive(!editBadgeActive)}
                className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
                  editBadgeActive ? 'bg-green-500' : 'bg-gray-300'
                }`}
              >
                <span
                  className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                    editBadgeActive ? 'translate-x-6' : 'translate-x-1'
                  }`}
                />
              </button>
              <span className="text-sm text-gray-500">
                {editBadgeActive ? 'Visible to users' : 'Hidden from selection'}
              </span>
            </div>

            {/* Replace Image */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Replace Image (optional)
              </label>
              <input
                ref={editFileInputRef}
                type="file"
                accept="image/png,image/jpeg,image/webp"
                onChange={(e) => setEditBadgeFile(e.target.files?.[0] || null)}
                className="hidden"
              />
              <div
                onClick={() => editFileInputRef.current?.click()}
                className="border-2 border-dashed border-gray-300 rounded-lg p-4 text-center cursor-pointer hover:border-gray-400 transition-colors"
              >
                {editBadgeFile ? (
                  <div className="flex items-center justify-center gap-2">
                    <Check className="w-5 h-5 text-green-500" />
                    <span className="text-sm text-gray-700">{editBadgeFile.name}</span>
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        setEditBadgeFile(null);
                      }}
                      className="p-1 hover:bg-gray-100 rounded"
                    >
                      <X className="w-4 h-4 text-gray-400" />
                    </button>
                  </div>
                ) : (
                  <p className="text-sm text-gray-500">Click to upload new image</p>
                )}
              </div>
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsEditOpen(false)}
              disabled={isUpdating}
            >
              Cancel
            </Button>
            <Button
              onClick={handleEdit}
              disabled={!editBadgeName.trim() || isUpdating}
              loading={isUpdating}
              style={{ background: '#FF7900', color: 'white' }}
            >
              Save Changes
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={isDeleteOpen} onOpenChange={setIsDeleteOpen}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>Delete Badge</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete the badge "{selectedBadge?.name}"? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>

          {selectedBadge?.imageUrl && (
            <div className="flex justify-center py-4">
              <div className="w-20 h-20 bg-gray-100 rounded-lg flex items-center justify-center">
                <Image
                  src={selectedBadge.imageUrl}
                  alt={selectedBadge.name}
                  width={60}
                  height={60}
                  className="object-contain"
                  unoptimized
                />
              </div>
            </div>
          )}

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsDeleteOpen(false)}
              disabled={isDeleting}
            >
              Cancel
            </Button>
            <Button
              onClick={handleDelete}
              disabled={isDeleting}
              loading={isDeleting}
              className="bg-red-600 hover:bg-red-700 text-white"
            >
              Delete Badge
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

/**
 * Helper function to get position indicator CSS for preview
 */
function getIndicatorPosition(position: BadgePosition): React.CSSProperties {
  switch (position) {
    case BadgePosition.TopLeft:
      return { top: '8px', left: '8px' };
    case BadgePosition.TopRight:
      return { top: '8px', right: '8px' };
    case BadgePosition.BottomLeft:
      return { bottom: '8px', left: '8px' };
    case BadgePosition.BottomRight:
      return { bottom: '8px', right: '8px' };
    default:
      return { top: '8px', right: '8px' };
  }
}
