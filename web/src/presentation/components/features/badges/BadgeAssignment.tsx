'use client';

import type { EventBadgeDto } from '@/infrastructure/api/types/badges.types';

interface BadgeAssignmentProps {
  eventId?: string;
  existingBadges?: EventBadgeDto[];
  onAssignmentChange?: () => Promise<void>;
  maxBadges?: number;
}

/**
 * Phase 6A.26: Badge Assignment Component (Placeholder)
 * TODO: Implement full badge assignment UI
 */
export function BadgeAssignment({
  eventId,
  existingBadges = [],
  onAssignmentChange,
  maxBadges = 3
}: BadgeAssignmentProps) {
  return (
    <div className="p-4 bg-muted/50 rounded-lg text-center text-muted-foreground">
      <p>Badge Assignment - Coming Soon</p>
      {existingBadges.length > 0 && (
        <p className="text-sm mt-2">{existingBadges.length} badge(s) assigned</p>
      )}
    </div>
  );
}
