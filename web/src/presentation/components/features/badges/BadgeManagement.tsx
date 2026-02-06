'use client';

import * as React from 'react';
import Image from 'next/image';
import { Plus, Pencil, Trash2, Check, X, Upload, Loader2, AlertCircle, Move } from 'lucide-react';
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
  DURATION_PRESETS,
  type BadgeDto,
  type UpdateBadgeDto,
  type DurationPreset,
  type BadgeLocationConfigDto,
  type BadgeDisplayLocation,
} from '@/infrastructure/api/types/badges.types';
import { BadgePreviewSection } from './BadgePreviewSection';
import { InteractiveBadgeEditor } from './InteractiveBadgeEditor';

/**
 * Phase 6A.26: Badge Management Component
 * Full implementation for managing promotional badges
 * Features:
 * - List all badges in a grid/table format
 * - Create new badge with image upload
 * - Edit badge details (name, position, active status, default duration)
 * - Delete custom badges (system badges can only be deactivated)
 * - Preview badge overlay
 * Phase 6A.27: Added expiry date support and type indicators
 * - Admin sees ALL badges (system + custom) with type tags
 * - EventOrganizer sees only their own custom badges
 * Phase 6A.28: Changed to duration-based expiration model
 * - Replaced date picker with duration dropdown
 * - Duration presets: 7 days, 14 days, 30 days, 90 days, Never, Custom
 */
export function BadgeManagement() {
  // Phase 6A.27: Fetch badges for management (forManagement=true):
  // - Admin sees ALL badges (system + custom) with type indicators
  // - EventOrganizer sees only their own custom badges
  const { data: badges, isLoading, error, refetch } = useBadges(false, true, false);

  // Mutations
  const createBadge = useCreateBadge();
  const updateBadge = useUpdateBadge();
  const updateBadgeImage = useUpdateBadgeImage();
  const deleteBadge = useDeleteBadge();

  // Dialog states
  const [isCreateOpen, setIsCreateOpen] = React.useState(false);
  const [isEditOpen, setIsEditOpen] = React.useState(false);
  const [isDeleteOpen, setIsDeleteOpen] = React.useState(false);
  const [isPositionOpen, setIsPositionOpen] = React.useState(false);
  const [selectedBadge, setSelectedBadge] = React.useState<BadgeDto | null>(null);

  // Form states
  const [newBadgeName, setNewBadgeName] = React.useState('');
  const [newBadgePosition, setNewBadgePosition] = React.useState<BadgePosition>(BadgePosition.TopRight);
  const [newBadgeFile, setNewBadgeFile] = React.useState<File | null>(null);
  // Phase 6A.28: Duration-based expiration - replace date picker with duration dropdown
  const [newBadgeDurationPreset, setNewBadgeDurationPreset] = React.useState<DurationPreset>(null);
  const [newBadgeCustomDays, setNewBadgeCustomDays] = React.useState<number>(30);
  const [editBadgeName, setEditBadgeName] = React.useState('');
  const [editBadgePosition, setEditBadgePosition] = React.useState<BadgePosition>(BadgePosition.TopRight);
  const [editBadgeActive, setEditBadgeActive] = React.useState(true);
  const [editBadgeFile, setEditBadgeFile] = React.useState<File | null>(null);
  // Phase 6A.28: Duration-based expiration for edit dialog
  const [editBadgeDurationPreset, setEditBadgeDurationPreset] = React.useState<DurationPreset>(null);
  const [editBadgeCustomDays, setEditBadgeCustomDays] = React.useState<number>(30);
  const [editBadgeClearDuration, setEditBadgeClearDuration] = React.useState(false);

  // Phase 6A.29: Preview URL for newly uploaded files
  const [newBadgePreviewUrl, setNewBadgePreviewUrl] = React.useState<string | null>(null);
  const [editBadgePreviewUrl, setEditBadgePreviewUrl] = React.useState<string | null>(null);

  // Phase 6A.32: Position dialog state - track unsaved changes
  const [positionListingConfig, setPositionListingConfig] = React.useState<BadgeLocationConfigDto | null>(null);
  const [positionFeaturedConfig, setPositionFeaturedConfig] = React.useState<BadgeLocationConfigDto | null>(null);
  const [positionDetailConfig, setPositionDetailConfig] = React.useState<BadgeLocationConfigDto | null>(null);
  const [hasUnsavedPositionChanges, setHasUnsavedPositionChanges] = React.useState(false);

  // File input refs
  const createFileInputRef = React.useRef<HTMLInputElement>(null);
  const editFileInputRef = React.useRef<HTMLInputElement>(null);

  // Loading states
  const isCreating = createBadge.isPending;
  const isUpdating = updateBadge.isPending || updateBadgeImage.isPending;
  const isDeleting = deleteBadge.isPending;

  // Phase 6A.29: Create and cleanup preview URLs for file uploads
  React.useEffect(() => {
    if (newBadgeFile) {
      const url = URL.createObjectURL(newBadgeFile);
      setNewBadgePreviewUrl(url);
      return () => URL.revokeObjectURL(url);
    } else {
      setNewBadgePreviewUrl(null);
    }
  }, [newBadgeFile]);

  React.useEffect(() => {
    if (editBadgeFile) {
      const url = URL.createObjectURL(editBadgeFile);
      setEditBadgePreviewUrl(url);
      return () => URL.revokeObjectURL(url);
    } else {
      setEditBadgePreviewUrl(null);
    }
  }, [editBadgeFile]);

  // Phase 6A.28: Helper to get effective duration from preset and custom days
  const getEffectiveDuration = (preset: DurationPreset, customDays: number): number | null => {
    if (preset === null) return null; // Never expire
    if (preset === 'custom') return customDays > 0 ? customDays : null;
    return preset as number;
  };

  // Handle create badge
  const handleCreate = async () => {
    if (!newBadgeName.trim() || !newBadgeFile) {
      return;
    }

    try {
      // Phase 6A.28: Use duration-based expiration
      const defaultDurationDays = getEffectiveDuration(newBadgeDurationPreset, newBadgeCustomDays);

      await createBadge.mutateAsync({
        name: newBadgeName.trim(),
        position: newBadgePosition,
        imageFile: newBadgeFile,
        defaultDurationDays,
      });

      // Reset form and close dialog
      setNewBadgeName('');
      setNewBadgePosition(BadgePosition.TopRight);
      setNewBadgeFile(null);
      setNewBadgeDurationPreset(null);
      setNewBadgeCustomDays(30);
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
      // For system badges, only isActive can be changed
      // For custom badges, all properties can be updated
      if (selectedBadge.isSystem) {
        // System badge: only update active status if changed
        if (editBadgeActive !== selectedBadge.isActive) {
          await updateBadge.mutateAsync({
            badgeId: selectedBadge.id,
            dto: { isActive: editBadgeActive },
          });
        }
      } else {
        // Phase 6A.28: Custom badge update with duration-based expiration
        const effectiveDuration = getEffectiveDuration(editBadgeDurationPreset, editBadgeCustomDays);
        const dto: UpdateBadgeDto = {
          name: editBadgeName.trim(),
          position: editBadgePosition,
          isActive: editBadgeActive,
          clearDuration: editBadgeClearDuration,
          defaultDurationDays: editBadgeClearDuration ? undefined : effectiveDuration,
        };
        await updateBadge.mutateAsync({ badgeId: selectedBadge.id, dto });
      }

      // Update badge image if new file selected (works for both system and custom)
      if (editBadgeFile) {
        await updateBadgeImage.mutateAsync({
          badgeId: selectedBadge.id,
          imageFile: editBadgeFile,
        });
      }

      // Reset and close
      setSelectedBadge(null);
      setEditBadgeFile(null);
      setEditBadgeDurationPreset(null);
      setEditBadgeCustomDays(30);
      setEditBadgeClearDuration(false);
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

  // Phase 6A.28: Helper to convert duration days to preset
  const durationToPreset = (days: number | null): DurationPreset => {
    if (days === null) return null;
    const preset = DURATION_PRESETS.find(p => p.value === days);
    return preset ? (days as DurationPreset) : 'custom';
  };

  // Open edit dialog
  const openEditDialog = (badge: BadgeDto) => {
    setSelectedBadge(badge);
    setEditBadgeName(badge.name);
    setEditBadgePosition(badge.position);
    setEditBadgeActive(badge.isActive);
    setEditBadgeFile(null);
    // Phase 6A.28: Set duration state from badge
    const preset = durationToPreset(badge.defaultDurationDays);
    setEditBadgeDurationPreset(preset);
    setEditBadgeCustomDays(badge.defaultDurationDays || 30);
    setEditBadgeClearDuration(false);
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

  // Phase 6A.32: Open position dialog
  const openPositionDialog = (badge: BadgeDto) => {
    setSelectedBadge(badge);
    setPositionListingConfig(badge.listingConfig);
    setPositionFeaturedConfig(badge.featuredConfig);
    setPositionDetailConfig(badge.detailConfig);
    setHasUnsavedPositionChanges(false);
    setIsPositionOpen(true);
  };

  // Phase 6A.32: Handle position config changes
  const handlePositionConfigChange = (location: BadgeDisplayLocation, config: BadgeLocationConfigDto) => {
    setHasUnsavedPositionChanges(true);
    if (location === 'listing') {
      setPositionListingConfig(config);
    } else if (location === 'featured') {
      setPositionFeaturedConfig(config);
    } else if (location === 'detail') {
      setPositionDetailConfig(config);
    }
  };

  // Phase 6A.32: Save position changes
  const handleSavePositionChanges = async () => {
    if (!selectedBadge || !positionListingConfig || !positionFeaturedConfig || !positionDetailConfig) {
      return;
    }

    try {
      const dto: UpdateBadgeDto = {
        listingConfig: positionListingConfig,
        featuredConfig: positionFeaturedConfig,
        detailConfig: positionDetailConfig,
      };
      await updateBadge.mutateAsync({ badgeId: selectedBadge.id, dto });

      // Close and reset
      setIsPositionOpen(false);
      setSelectedBadge(null);
      setHasUnsavedPositionChanges(false);
    } catch (err) {
      console.error('Failed to update badge positions:', err);
    }
  };

  // Phase 6A.32: Close position dialog with unsaved changes warning
  const handleClosePositionDialog = () => {
    if (hasUnsavedPositionChanges) {
      if (confirm('You have unsaved position changes. Are you sure you want to close?')) {
        setIsPositionOpen(false);
        setSelectedBadge(null);
        setHasUnsavedPositionChanges(false);
      }
    } else {
      setIsPositionOpen(false);
      setSelectedBadge(null);
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

                {/* Phase 6A.27: Badge type indicator (System/Custom) */}
                {badge.isSystem ? (
                  <div
                    className="absolute top-2 left-2 px-2 py-0.5 text-xs font-medium rounded bg-blue-600 text-white"
                  >
                    System
                  </div>
                ) : (
                  <div
                    className="absolute top-2 left-2 px-2 py-0.5 text-xs font-medium rounded bg-purple-600 text-white"
                  >
                    Custom
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

                {/* Phase 6A.28: Duration display */}
                {/* Phase 6A.29: Creator display styled like Email Groups tab */}
                <div className="text-xs text-gray-500 space-y-1 mb-2">
                  <div>
                    Duration: {badge.defaultDurationDays
                      ? `${badge.defaultDurationDays} days`
                      : 'Never expires'}
                  </div>
                  {!badge.isSystem && badge.creatorName && (
                    <div className="text-[#8B1538]">
                      Owner: {badge.creatorName}
                    </div>
                  )}
                </div>

                {/* Action Buttons */}
                <div className="space-y-2 mt-3">
                  {/* Phase 6A.32: Position Badge button */}
                  <Button
                    variant="outline"
                    size="sm"
                    className="w-full gap-1"
                    onClick={() => openPositionDialog(badge)}
                    style={{ borderColor: '#FF7900', color: '#FF7900' }}
                  >
                    <Move className="w-3 h-3" />
                    Position Badge
                  </Button>
                  <div className="flex items-center gap-2">
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
            </div>
          ))}
        </div>
      )}

      {/* Create Badge Dialog */}
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="max-w-md md:max-w-2xl lg:max-w-3xl xl:max-w-4xl max-h-[90vh] overflow-y-auto">
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

            {/* Phase 6A.28: Default Duration dropdown */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Default Duration (when assigned to events)
              </label>
              <select
                value={newBadgeDurationPreset === null ? '' : String(newBadgeDurationPreset)}
                onChange={(e) => {
                  const val = e.target.value;
                  if (val === '') {
                    setNewBadgeDurationPreset(null);
                  } else if (val === 'custom') {
                    setNewBadgeDurationPreset('custom');
                  } else {
                    setNewBadgeDurationPreset(Number(val) as DurationPreset);
                  }
                }}
                className="w-full h-10 rounded-md border border-input bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              >
                {DURATION_PRESETS.map((preset) => (
                  <option
                    key={preset.value === null ? 'never' : String(preset.value)}
                    value={preset.value === null ? '' : String(preset.value)}
                  >
                    {preset.label}
                  </option>
                ))}
              </select>

              {/* Custom days input */}
              {newBadgeDurationPreset === 'custom' && (
                <div className="mt-2">
                  <Input
                    type="number"
                    min="1"
                    max="365"
                    placeholder="Enter number of days"
                    value={newBadgeCustomDays}
                    onChange={(e) => setNewBadgeCustomDays(Number(e.target.value))}
                    className="w-full"
                  />
                </div>
              )}

              <p className="text-xs text-gray-400 mt-1">
                Duration determines when badge assignments expire. "Never expire" means the badge stays permanently on assigned events.
              </p>
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

            {/* Phase 6A.29: Badge Preview Section */}
            {newBadgePreviewUrl && (
              <div className="pt-4 border-t border-gray-200">
                <BadgePreviewSection
                  badge={{
                    imageUrl: newBadgePreviewUrl,
                    name: newBadgeName || 'New Badge',
                    position: newBadgePosition,
                  }}
                  position={newBadgePosition}
                  onPositionChange={(pos) => setNewBadgePosition(pos)}
                />
              </div>
            )}
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
        <DialogContent className="max-w-md md:max-w-2xl lg:max-w-3xl xl:max-w-4xl max-h-[90vh] overflow-y-auto">
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

            {/* Phase 6A.28: Default Duration (only for custom badges) */}
            {!selectedBadge?.isSystem && (
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Default Duration (when assigned to events)
                </label>
                <div className="space-y-2">
                  <select
                    value={editBadgeClearDuration ? '' : (editBadgeDurationPreset === null ? '' : String(editBadgeDurationPreset))}
                    onChange={(e) => {
                      const val = e.target.value;
                      setEditBadgeClearDuration(false);
                      if (val === '') {
                        setEditBadgeDurationPreset(null);
                      } else if (val === 'custom') {
                        setEditBadgeDurationPreset('custom');
                      } else {
                        setEditBadgeDurationPreset(Number(val) as DurationPreset);
                      }
                    }}
                    className="w-full h-10 rounded-md border border-input bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                    disabled={editBadgeClearDuration}
                  >
                    {DURATION_PRESETS.map((preset) => (
                      <option
                        key={preset.value === null ? 'never' : String(preset.value)}
                        value={preset.value === null ? '' : String(preset.value)}
                      >
                        {preset.label}
                      </option>
                    ))}
                  </select>

                  {/* Custom days input */}
                  {editBadgeDurationPreset === 'custom' && !editBadgeClearDuration && (
                    <Input
                      type="number"
                      min="1"
                      max="365"
                      placeholder="Enter number of days"
                      value={editBadgeCustomDays}
                      onChange={(e) => setEditBadgeCustomDays(Number(e.target.value))}
                      className="w-full"
                    />
                  )}

                  {/* Option to clear duration */}
                  {selectedBadge?.defaultDurationDays && (
                    <div className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        id="clearDuration"
                        checked={editBadgeClearDuration}
                        onChange={(e) => {
                          setEditBadgeClearDuration(e.target.checked);
                          if (e.target.checked) {
                            setEditBadgeDurationPreset(null);
                          }
                        }}
                        className="h-4 w-4 rounded border-gray-300 text-orange-500 focus:ring-orange-500"
                      />
                      <label htmlFor="clearDuration" className="text-sm text-gray-600">
                        Remove duration (badge assignments will never expire)
                      </label>
                    </div>
                  )}

                  <p className="text-xs text-gray-400">
                    {selectedBadge?.defaultDurationDays
                      ? `Current: ${selectedBadge.defaultDurationDays} days`
                      : 'No duration set. Badge assignments will never expire.'}
                  </p>
                </div>
              </div>
            )}

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

            {/* Phase 6A.29: Badge Preview Section for Edit */}
            {selectedBadge && (
              <div className="pt-4 border-t border-gray-200">
                <BadgePreviewSection
                  badge={{
                    imageUrl: editBadgePreviewUrl || selectedBadge.imageUrl,
                    name: editBadgeName || selectedBadge.name,
                    position: editBadgePosition,
                  }}
                  position={editBadgePosition}
                  onPositionChange={(pos) => !selectedBadge.isSystem && setEditBadgePosition(pos)}
                />
              </div>
            )}
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

      {/* Phase 6A.32: Position Badge Dialog */}
      <Dialog open={isPositionOpen} onOpenChange={handleClosePositionDialog}>
        <DialogContent className="max-w-5xl max-h-[95vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Position Badge: {selectedBadge?.name}</DialogTitle>
            <DialogDescription>
              Customize badge position, size, and rotation for each display location using drag, resize, and sliders.
            </DialogDescription>
          </DialogHeader>

          {selectedBadge && positionListingConfig && positionFeaturedConfig && positionDetailConfig && (
            <div className="py-4">
              <InteractiveBadgeEditor
                badge={{
                  ...selectedBadge,
                  listingConfig: positionListingConfig,
                  featuredConfig: positionFeaturedConfig,
                  detailConfig: positionDetailConfig,
                }}
                onConfigChange={handlePositionConfigChange}
                disabled={isUpdating}
              />
            </div>
          )}

          <DialogFooter>
            <Button
              variant="outline"
              onClick={handleClosePositionDialog}
              disabled={isUpdating}
            >
              Cancel
            </Button>
            <Button
              onClick={handleSavePositionChanges}
              disabled={!hasUnsavedPositionChanges || isUpdating}
              loading={isUpdating}
              style={{ background: '#FF7900', color: 'white' }}
            >
              Save Positions
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
