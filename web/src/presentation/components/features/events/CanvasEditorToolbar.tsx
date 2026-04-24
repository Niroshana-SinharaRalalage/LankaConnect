/**
 * Slice 8 S8.5b: CanvasEditorToolbar.
 *
 * Horizontal strip of buttons at the top of the editor modal. Lets the
 * organizer add zones / tables / decorations and delete the currently
 * selected item. All additions are drafts — they live in the
 * CanvasEditor's draftAdditions state until Save (S8.8).
 *
 * Design notes:
 * - Decoration kind is a native <select> next to an Add button so the 7
 *   kinds don't each need a toolbar button (keeps the strip compact).
 * - Delete button is disabled when nothing is selected. Visually muted so
 *   the toolbar doesn't scream "danger" all the time.
 * - Add Zone / Add Round Table / Add Rect Table are always enabled; the
 *   callbacks handle the positioning + auto-label logic.
 * - All buttons + the decoration select have type="button" so the
 *   toolbar never submits a surrounding form by accident.
 */

'use client';

import React, { useState } from 'react';
import { Plus, Trash2 } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { DecorationKind } from '@/infrastructure/api/types/events.types';

const DECORATION_KINDS: Array<{ value: DecorationKind; label: string }> = [
  { value: DecorationKind.Stage, label: 'Stage' },
  { value: DecorationKind.DanceFloor, label: 'Dance Floor' },
  { value: DecorationKind.Aisle, label: 'Aisle' },
  { value: DecorationKind.Door, label: 'Door' },
  { value: DecorationKind.Wall, label: 'Wall' },
  { value: DecorationKind.Text, label: 'Text' },
  { value: DecorationKind.Image, label: 'Image' },
];

export interface CanvasEditorToolbarProps {
  onAddZone: () => void;
  onAddRoundTable: () => void;
  onAddRectTable: () => void;
  onAddDecoration: (kind: DecorationKind) => void;
  onDeleteSelected: () => void;
  canDelete: boolean;
  className?: string;
}

export function CanvasEditorToolbar({
  onAddZone,
  onAddRoundTable,
  onAddRectTable,
  onAddDecoration,
  onDeleteSelected,
  canDelete,
  className,
}: CanvasEditorToolbarProps) {
  const [decorationKind, setDecorationKind] = useState<DecorationKind>(
    DecorationKind.Stage,
  );

  return (
    <div
      className={
        className ??
        'flex flex-wrap items-center gap-2 px-4 py-2 border-b border-neutral-200 bg-white'
      }
      role="toolbar"
      aria-label="Canvas editor toolbar"
      data-testid="canvas-editor-toolbar"
    >
      <Button
        type="button"
        size="sm"
        variant="outline"
        onClick={onAddZone}
        data-testid="toolbar-add-zone"
      >
        <Plus className="w-3.5 h-3.5 mr-1" aria-hidden="true" />
        Zone
      </Button>
      <Button
        type="button"
        size="sm"
        variant="outline"
        onClick={onAddRoundTable}
        data-testid="toolbar-add-round-table"
      >
        <Plus className="w-3.5 h-3.5 mr-1" aria-hidden="true" />
        Round table
      </Button>
      <Button
        type="button"
        size="sm"
        variant="outline"
        onClick={onAddRectTable}
        data-testid="toolbar-add-rect-table"
      >
        <Plus className="w-3.5 h-3.5 mr-1" aria-hidden="true" />
        Rect table
      </Button>

      <div className="flex items-center gap-1 ml-1">
        <label htmlFor="toolbar-decoration-kind" className="sr-only">
          Decoration kind
        </label>
        <select
          id="toolbar-decoration-kind"
          className="rounded-md border border-neutral-300 px-2 py-1.5 text-sm text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500"
          value={decorationKind}
          onChange={(e) => setDecorationKind(e.target.value as DecorationKind)}
          data-testid="toolbar-decoration-kind"
        >
          {DECORATION_KINDS.map((k) => (
            <option key={k.value} value={k.value}>
              {k.label}
            </option>
          ))}
        </select>
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() => onAddDecoration(decorationKind)}
          data-testid="toolbar-add-decoration"
        >
          <Plus className="w-3.5 h-3.5 mr-1" aria-hidden="true" />
          Decoration
        </Button>
      </div>

      <div className="flex-1" />

      <Button
        type="button"
        size="sm"
        variant="outline"
        onClick={onDeleteSelected}
        disabled={!canDelete}
        aria-label="Delete selected item"
        data-testid="toolbar-delete"
        className="text-red-700 border-red-200 hover:bg-red-50 disabled:text-neutral-400 disabled:border-neutral-200"
      >
        <Trash2 className="w-3.5 h-3.5 mr-1" aria-hidden="true" />
        Delete
      </Button>
    </div>
  );
}

export default CanvasEditorToolbar;
