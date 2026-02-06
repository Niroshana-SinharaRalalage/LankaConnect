'use client';

import * as React from 'react';
import Image from 'next/image';
import { Plus, X, Loader2, AlertCircle, Check } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import {
  useBadges,
  useEventBadges,
  useAssignBadgeToEvent,
  useRemoveBadgeFromEvent,
} from '@/presentation/hooks/useBadges';
import {
  getPositionDisplayName,
  getBadgePositionStyles,
  DURATION_PRESETS,
  type BadgeDto,
  type EventBadgeDto,
  type DurationPreset,
} from '@/infrastructure/api/types/badges.types';

interface BadgeAssignmentProps {
  eventId: string;
  existingBadges?: EventBadgeDto[];
  onAssignmentChange?: () => Promise<void>;
  maxBadges?: number;
}

/**
 * Phase 6A.26: Badge Assignment Component
 * Full implementation for assigning badges to events
 * Features:
 * - Display currently assigned badges
 * - Select from available badges
 * - Assign/remove badges with optimistic updates
 * - Position preview indicator
 * Phase 6A.27: Uses forAssignment=true to get role-appropriate badges (excludes expired)
 * Phase 6A.28: Duration override during assignment - organizers can set custom duration
 */
export function BadgeAssignment({
  eventId,
  existingBadges = [],
  onAssignmentChange,
  maxBadges = 3,
}: BadgeAssignmentProps) {
  // Phase 6A.27: Fetch badges for assignment (forAssignment=true):
  // - EventOrganizer sees: their own custom badges + all system badges
  // - Excludes expired badges
  const { data: allBadges, isLoading: loadingBadges, error: badgesError } = useBadges(true, false, true);

  // Fetch event's assigned badges
  const { data: eventBadges, isLoading: loadingEventBadges } = useEventBadges(eventId);

  // Mutations
  const assignBadge = useAssignBadgeToEvent();
  const removeBadge = useRemoveBadgeFromEvent();

  // UI state
  const [isSelectingBadge, setIsSelectingBadge] = React.useState(false);
  // Phase 6A.28: Duration override state - 'default' means use badge's default duration
  const [selectedBadgeForAssign, setSelectedBadgeForAssign] = React.useState<BadgeDto | null>(null);
  const [durationOverride, setDurationOverride] = React.useState<'default' | DurationPreset>('default');
  const [customDays, setCustomDays] = React.useState<number>(30);

  // Use existingBadges prop if provided, otherwise use fetched eventBadges
  const currentBadges = existingBadges.length > 0 ? existingBadges : eventBadges || [];

  // Get badge IDs that are already assigned
  const assignedBadgeIds = new Set(currentBadges.map((eb) => eb.badgeId));

  // Filter available badges (not already assigned)
  const availableBadges = allBadges?.filter((b) => !assignedBadgeIds.has(b.id)) || [];

  // Check if max badges reached
  const isMaxReached = currentBadges.length >= maxBadges;

  // Phase 6A.28: Get effective duration for assignment
  const getEffectiveDuration = (): number | null | undefined => {
    if (durationOverride === 'default') return undefined; // Use badge default
    if (durationOverride === null) return null; // Never expire
    if (durationOverride === 'custom') return customDays > 0 ? customDays : null;
    return durationOverride as number;
  };

  // Phase 6A.28: Get available duration options based on badge's max duration
  const getAvailableDurationOptions = (badge: BadgeDto) => {
    const maxDays = badge.defaultDurationDays;
    if (maxDays === null) {
      // No max limit - all options available
      return DURATION_PRESETS;
    }
    // Filter to only show options <= max duration
    return DURATION_PRESETS.filter(p => {
      if (p.value === null) return true; // "Never expire" always available
      if (p.value === 'custom') return true; // Custom always available (validated separately)
      return (p.value as number) <= maxDays;
    });
  };

  // Phase 6A.28: Open duration selection dialog for a badge
  const handleSelectBadge = (badge: BadgeDto) => {
    setSelectedBadgeForAssign(badge);
    setDurationOverride('default');
    setCustomDays(badge.defaultDurationDays || 30);
  };

  // Handle assign badge
  const handleAssignBadge = async () => {
    if (!eventId || isMaxReached || !selectedBadgeForAssign) return;

    try {
      // Phase 6A.28: Validate custom duration against badge max
      const effectiveDuration = getEffectiveDuration();
      const maxDays = selectedBadgeForAssign.defaultDurationDays;
      if (maxDays !== null && effectiveDuration !== undefined && effectiveDuration !== null) {
        if (effectiveDuration > maxDays) {
          console.error(`Duration cannot exceed ${maxDays} days for this badge`);
          return;
        }
      }

      await assignBadge.mutateAsync({
        eventId,
        badgeId: selectedBadgeForAssign.id,
        durationDays: effectiveDuration,
      });
      setSelectedBadgeForAssign(null);
      setIsSelectingBadge(false);
      if (onAssignmentChange) {
        await onAssignmentChange();
      }
    } catch (err) {
      console.error('Failed to assign badge:', err);
    }
  };

  // Phase 6A.28: Cancel badge selection
  const handleCancelSelection = () => {
    setSelectedBadgeForAssign(null);
    setDurationOverride('default');
  };

  // Handle remove badge
  const handleRemoveBadge = async (badgeId: string) => {
    if (!eventId) return;

    try {
      await removeBadge.mutateAsync({ eventId, badgeId });
      if (onAssignmentChange) {
        await onAssignmentChange();
      }
    } catch (err) {
      console.error('Failed to remove badge:', err);
    }
  };

  // Loading state
  if (loadingBadges || loadingEventBadges) {
    return (
      <div className="flex items-center justify-center py-6">
        <Loader2 className="w-5 h-5 animate-spin" style={{ color: '#FF7900' }} />
        <span className="ml-2 text-sm text-gray-500">Loading badges...</span>
      </div>
    );
  }

  // Error state
  if (badgesError) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-4 text-center">
        <AlertCircle className="w-5 h-5 mx-auto mb-1 text-red-500" />
        <p className="text-sm text-red-700">Failed to load badges</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Currently Assigned Badges */}
      <div>
        <div className="flex items-center justify-between mb-2">
          <span className="text-sm font-medium text-gray-700">
            Assigned Badges ({currentBadges.length}/{maxBadges})
          </span>
          {!isMaxReached && !isSelectingBadge && (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setIsSelectingBadge(true)}
              className="gap-1 text-xs"
              style={{ color: '#FF7900' }}
            >
              <Plus className="w-3 h-3" />
              Add Badge
            </Button>
          )}
        </div>

        {currentBadges.length === 0 ? (
          <div className="bg-gray-50 rounded-lg p-4 text-center text-sm text-gray-500">
            No badges assigned. Add a badge to highlight your event!
          </div>
        ) : (
          <div className="space-y-2">
            {currentBadges.map((eventBadge) => (
              <AssignedBadgeCard
                key={eventBadge.id}
                eventBadge={eventBadge}
                onRemove={() => handleRemoveBadge(eventBadge.badgeId)}
                isRemoving={removeBadge.isPending}
              />
            ))}
          </div>
        )}
      </div>

      {/* Badge Selection */}
      {isSelectingBadge && (
        <div className="border rounded-lg p-4 bg-gray-50">
          <div className="flex items-center justify-between mb-3">
            <span className="text-sm font-medium text-gray-700">Select a Badge</span>
            <button
              onClick={() => setIsSelectingBadge(false)}
              className="p-1 hover:bg-gray-200 rounded transition-colors"
            >
              <X className="w-4 h-4 text-gray-400" />
            </button>
          </div>

          {availableBadges.length === 0 ? (
            <p className="text-sm text-gray-500 text-center py-2">
              No more badges available
            </p>
          ) : selectedBadgeForAssign ? (
            /* Phase 6A.28: Duration Override Selection */
            <div className="space-y-3">
              <div className="flex items-center gap-3 bg-white rounded-lg border p-3">
                <div className="w-12 h-12 bg-gray-100 rounded flex items-center justify-center flex-shrink-0">
                  {selectedBadgeForAssign.imageUrl && (
                    <Image
                      src={selectedBadgeForAssign.imageUrl}
                      alt={selectedBadgeForAssign.name}
                      width={40}
                      height={40}
                      className="object-contain"
                      unoptimized
                    />
                  )}
                </div>
                <div className="flex-1">
                  <p className="text-sm font-medium text-gray-900">{selectedBadgeForAssign.name}</p>
                  <p className="text-xs text-gray-500">
                    Default: {selectedBadgeForAssign.defaultDurationDays
                      ? `${selectedBadgeForAssign.defaultDurationDays} days`
                      : 'Never expires'}
                  </p>
                </div>
              </div>

              {/* Duration Override Dropdown */}
              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">
                  Badge Duration
                </label>
                <select
                  value={durationOverride === 'default' ? 'default' : (durationOverride === null ? '' : String(durationOverride))}
                  onChange={(e) => {
                    const val = e.target.value;
                    if (val === 'default') {
                      setDurationOverride('default');
                    } else if (val === '') {
                      setDurationOverride(null);
                    } else if (val === 'custom') {
                      setDurationOverride('custom');
                    } else {
                      setDurationOverride(Number(val) as DurationPreset);
                    }
                  }}
                  className="w-full h-9 rounded-md border border-input bg-background px-3 py-1 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                >
                  <option value="default">
                    Use badge default ({selectedBadgeForAssign.defaultDurationDays
                      ? `${selectedBadgeForAssign.defaultDurationDays} days`
                      : 'Never'})
                  </option>
                  {getAvailableDurationOptions(selectedBadgeForAssign).map((preset) => (
                    <option
                      key={preset.value === null ? 'never' : String(preset.value)}
                      value={preset.value === null ? '' : String(preset.value)}
                    >
                      {preset.label}
                    </option>
                  ))}
                </select>

                {/* Custom days input */}
                {durationOverride === 'custom' && (
                  <div className="mt-2">
                    <input
                      type="number"
                      min="1"
                      max={selectedBadgeForAssign.defaultDurationDays || 365}
                      value={customDays}
                      onChange={(e) => setCustomDays(Number(e.target.value))}
                      className="w-full h-9 rounded-md border border-input bg-background px-3 py-1 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                      placeholder="Enter days"
                    />
                    {selectedBadgeForAssign.defaultDurationDays && (
                      <p className="text-[10px] text-gray-400 mt-1">
                        Max: {selectedBadgeForAssign.defaultDurationDays} days
                      </p>
                    )}
                  </div>
                )}
              </div>

              {/* Action Buttons */}
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleCancelSelection}
                  className="flex-1"
                  disabled={assignBadge.isPending}
                >
                  Cancel
                </Button>
                <Button
                  size="sm"
                  onClick={handleAssignBadge}
                  className="flex-1 gap-1"
                  style={{ background: '#FF7900', color: 'white' }}
                  disabled={assignBadge.isPending}
                >
                  {assignBadge.isPending ? (
                    <Loader2 className="w-3 h-3 animate-spin" />
                  ) : (
                    <Check className="w-3 h-3" />
                  )}
                  Assign
                </Button>
              </div>
            </div>
          ) : (
            <div className="grid grid-cols-2 gap-2">
              {availableBadges.map((badge) => (
                <BadgeSelectCard
                  key={badge.id}
                  badge={badge}
                  onSelect={() => handleSelectBadge(badge)}
                  isAssigning={false}
                />
              ))}
            </div>
          )}
        </div>
      )}

      {/* Max badges warning */}
      {isMaxReached && (
        <p className="text-xs text-amber-600 bg-amber-50 rounded p-2 text-center">
          Maximum {maxBadges} badges allowed per event
        </p>
      )}
    </div>
  );
}

/**
 * Card component for displaying an assigned badge
 */
function AssignedBadgeCard({
  eventBadge,
  onRemove,
  isRemoving,
}: {
  eventBadge: EventBadgeDto;
  onRemove: () => void;
  isRemoving: boolean;
}) {
  const badge = eventBadge.badge;

  return (
    <div className="flex items-center gap-3 bg-white rounded-lg border p-2">
      {/* Badge Image */}
      <div className="w-12 h-12 bg-gray-100 rounded flex items-center justify-center flex-shrink-0">
        {badge.imageUrl ? (
          <Image
            src={badge.imageUrl}
            alt={badge.name}
            width={40}
            height={40}
            className="object-contain"
            unoptimized
          />
        ) : (
          <div className="text-xs text-gray-400">No img</div>
        )}
      </div>

      {/* Badge Info */}
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium text-gray-900 truncate">{badge.name}</p>
        <p className="text-xs text-gray-500">
          {getPositionDisplayName(badge.position)}
        </p>
        {/* Phase 6A.28: Show expiration info */}
        {eventBadge.expiresAt ? (
          <p className={`text-[10px] ${eventBadge.isExpired ? 'text-red-500' : 'text-gray-400'}`}>
            {eventBadge.isExpired
              ? `Expired: ${new Date(eventBadge.expiresAt).toLocaleDateString()}`
              : `Expires: ${new Date(eventBadge.expiresAt).toLocaleDateString()}`}
          </p>
        ) : (
          <p className="text-[10px] text-gray-400">Never expires</p>
        )}
      </div>

      {/* Remove Button */}
      <button
        onClick={onRemove}
        disabled={isRemoving}
        className="p-1.5 hover:bg-red-50 rounded transition-colors text-gray-400 hover:text-red-500"
        title="Remove badge"
      >
        {isRemoving ? (
          <Loader2 className="w-4 h-4 animate-spin" />
        ) : (
          <X className="w-4 h-4" />
        )}
      </button>
    </div>
  );
}

/**
 * Card component for selecting a badge to assign
 */
function BadgeSelectCard({
  badge,
  onSelect,
  isAssigning,
}: {
  badge: BadgeDto;
  onSelect: () => void;
  isAssigning: boolean;
}) {
  return (
    <button
      onClick={onSelect}
      disabled={isAssigning}
      className="flex items-center gap-2 p-2 bg-white rounded-lg border hover:border-orange-300 hover:bg-orange-50 transition-colors text-left disabled:opacity-50"
    >
      {/* Badge Image */}
      <div className="w-10 h-10 bg-gray-100 rounded flex items-center justify-center flex-shrink-0">
        {badge.imageUrl ? (
          <Image
            src={badge.imageUrl}
            alt={badge.name}
            width={32}
            height={32}
            className="object-contain"
            unoptimized
          />
        ) : (
          <div className="text-xs text-gray-400">-</div>
        )}
      </div>

      {/* Badge Info */}
      <div className="flex-1 min-w-0">
        <p className="text-xs font-medium text-gray-900 truncate">{badge.name}</p>
        <p className="text-[10px] text-gray-500">
          {getPositionDisplayName(badge.position)}
        </p>
      </div>

      {/* Select Indicator */}
      {isAssigning ? (
        <Loader2 className="w-4 h-4 animate-spin text-orange-500 flex-shrink-0" />
      ) : (
        <Plus className="w-4 h-4 text-orange-500 flex-shrink-0" />
      )}
    </button>
  );
}

/**
 * Badge Overlay Component
 * Displays badge on top of event images
 * Phase 6A.28: Filters out expired badge assignments
 */
export function BadgeOverlay({
  badges,
  className = '',
}: {
  badges: EventBadgeDto[];
  className?: string;
}) {
  if (!badges || badges.length === 0) return null;

  // Phase 6A.28: Filter out expired badges
  const activeBadges = badges.filter(eb => !eb.isExpired);
  if (activeBadges.length === 0) return null;

  // Group badges by position to avoid overlap
  const badgesByPosition = activeBadges.reduce((acc, eb) => {
    const pos = eb.badge.position;
    if (!acc[pos]) acc[pos] = [];
    acc[pos].push(eb);
    return acc;
  }, {} as Record<number, EventBadgeDto[]>);

  return (
    <>
      {Object.entries(badgesByPosition).map(([position, positionBadges]) => {
        const firstBadge = positionBadges[0];
        const positionStyles = getBadgePositionStyles(firstBadge.badge.position);

        return (
          <div
            key={position}
            className={`absolute z-10 ${className}`}
            style={{
              ...positionStyles,
              margin: '8px',
            }}
          >
            {firstBadge.badge.imageUrl && (
              <Image
                src={firstBadge.badge.imageUrl}
                alt={firstBadge.badge.name}
                width={48}
                height={48}
                className="object-contain drop-shadow-md"
                unoptimized
                title={firstBadge.badge.name}
              />
            )}
          </div>
        );
      })}
    </>
  );
}
