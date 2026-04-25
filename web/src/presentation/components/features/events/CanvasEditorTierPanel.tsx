/**
 * Slice 8 S8.7: CanvasEditorTierPanel — ticket-tier mapping for a selected
 * zone or table. Rendered inside {@link CanvasEditorPropertyPanel} when the
 * selected item is a zone or table (tier assignments don't apply to
 * decorations).
 *
 * Backend model: Slice 4's polymorphic `tier_assignments` junction lets one
 * tier map to many zones/tables. The editor treats the assignment set as a
 * multi-select per item. Checkbox clicks emit an `onToggleTier(tierId)`
 * callback — the parent owns the draft state and commits through the
 * history hook.
 *
 * Empty states:
 * - `tiers` empty → "This event has no tiers yet" hint (either the layout
 *   is a template, or the organizer hasn't created tiers).
 * - `tiersLoading` → compact loading placeholder so the panel doesn't
 *   flash between "no tiers" and "here are tiers".
 */

'use client';

import React from 'react';
import { Loader2 } from 'lucide-react';
import type { TicketTierDto } from '@/infrastructure/api/types/events.types';

export interface CanvasEditorTierPanelProps {
  /** Available tiers for the event. Empty when layout is a template or
   * the event has no tiers yet. */
  tiers: TicketTierDto[];
  /** Tier IDs currently assigned to the selected item. */
  assignedTierIds: readonly string[];
  /** Fired on checkbox toggle — caller flips membership via
   * toggleTierAssignment() and commits through history. */
  onToggleTier: (tierId: string) => void;
  tiersLoading?: boolean;
  /** True when the layout has no associated event (template). */
  isTemplateLayout?: boolean;
  className?: string;
}

export function CanvasEditorTierPanel({
  tiers,
  assignedTierIds,
  onToggleTier,
  tiersLoading = false,
  isTemplateLayout = false,
  className,
}: CanvasEditorTierPanelProps) {
  return (
    <section
      className={className ?? 'mt-4 pt-4 border-t border-neutral-200'}
      data-testid="canvas-editor-tier-panel"
      aria-label="Ticket tier mapping"
    >
      <header className="mb-2">
        <p className="text-xs uppercase tracking-wide text-neutral-500">
          Ticket tiers
        </p>
        <p className="text-xs text-neutral-500 mt-0.5">
          Attendees holding a checked tier can pick seats in this item.
        </p>
      </header>

      {isTemplateLayout && (
        <p
          className="text-sm text-neutral-600 bg-neutral-50 border border-neutral-200 rounded-md p-3"
          data-testid="tier-panel-template-hint"
        >
          Attach this layout to an event to map it to the event&apos;s ticket tiers.
        </p>
      )}

      {!isTemplateLayout && tiersLoading && (
        <div
          className="flex items-center gap-2 text-sm text-neutral-500 py-1"
          data-testid="tier-panel-loading"
        >
          <Loader2 className="w-3.5 h-3.5 animate-spin" aria-hidden="true" />
          Loading tiers…
        </div>
      )}

      {!isTemplateLayout && !tiersLoading && tiers.length === 0 && (
        <p
          className="text-sm text-neutral-600 bg-amber-50 border border-amber-200 rounded-md p-3"
          data-testid="tier-panel-empty"
        >
          This event has no ticket tiers yet. Add tiers on the event page,
          then come back to map them.
        </p>
      )}

      {!isTemplateLayout && !tiersLoading && tiers.length > 0 && (
        <ul
          className="space-y-1 max-h-56 overflow-y-auto"
          data-testid="tier-panel-list"
        >
          {tiers.map((tier) => {
            const checked = assignedTierIds.includes(tier.id);
            return (
              <li key={tier.id}>
                <label
                  className="flex items-center gap-2 text-sm text-neutral-800 cursor-pointer py-1 px-2 rounded-md hover:bg-neutral-50"
                  data-testid={`tier-panel-row-${tier.id}`}
                >
                  <input
                    type="checkbox"
                    checked={checked}
                    onChange={() => onToggleTier(tier.id)}
                    className="rounded border-neutral-300 text-primary-600 focus:ring-primary-500"
                    data-testid={`tier-panel-checkbox-${tier.id}`}
                  />
                  <span className="flex-1 truncate">{tier.name}</span>
                </label>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}

export default CanvasEditorTierPanel;
