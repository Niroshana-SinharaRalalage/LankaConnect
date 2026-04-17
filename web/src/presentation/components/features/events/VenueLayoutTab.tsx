'use client';

import { useState, useMemo } from 'react';
import {
  LayoutGrid,
  Plus,
  Trash2,
  Armchair,
  CheckCircle,
  AlertCircle,
  Accessibility,
} from 'lucide-react';
import toast from 'react-hot-toast';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import {
  useVenueLayoutByEvent,
  useCreateVenueLayout,
  useGenerateSeats,
  useAssignLayoutToEvent,
} from '@/presentation/hooks/useVenueLayouts';
import type {
  VenueLayoutDto,
  VenueZoneDto,
  SeatDto,
  CreateVenueLayoutRequest,
  CreateVenueZoneRequest,
  GenerateSeatsRequest,
} from '@/infrastructure/api/types/events.types';
import { LayoutType } from '@/infrastructure/api/types/events.types';

// ---------------------------------------------------------------------------
// Props
// ---------------------------------------------------------------------------

interface VenueLayoutTabProps {
  eventId: string;
  ticketTiers?: Array<{ id: string; name: string }>;
}

// ---------------------------------------------------------------------------
// Local types for form state
// ---------------------------------------------------------------------------

interface ZoneFormState {
  name: string;
  color: string;
  ticketTierId: string;
}

interface SeatGenFormState {
  rowsOrTables: number;
  seatsPerUnit: number;
}

const DEFAULT_ZONE_COLORS = ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899'];

// ---------------------------------------------------------------------------
// Component
// ---------------------------------------------------------------------------

export function VenueLayoutTab({ eventId, ticketTiers = [] }: VenueLayoutTabProps) {
  // ---- Remote state ----
  const {
    data: layout,
    isLoading: layoutLoading,
    error: layoutError,
  } = useVenueLayoutByEvent(eventId);

  const createLayout = useCreateVenueLayout();
  const generateSeats = useGenerateSeats();
  const assignLayout = useAssignLayoutToEvent();

  // ---- Local UI state ----
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [layoutName, setLayoutName] = useState('');
  const [layoutType, setLayoutType] = useState<LayoutType>(LayoutType.Theater);
  const [zones, setZones] = useState<ZoneFormState[]>([
    { name: 'Zone A', color: DEFAULT_ZONE_COLORS[0], ticketTierId: '' },
  ]);

  // Per-zone seat generation inputs keyed by zone id
  const [seatGenForms, setSeatGenForms] = useState<Record<string, SeatGenFormState>>({});

  // ---- Loading / Error gates ----
  if (layoutLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <LayoutGrid className="h-6 w-6 animate-spin text-neutral-400" />
        <span className="ml-2 text-neutral-500">Loading venue layout...</span>
      </div>
    );
  }

  if (layoutError) {
    return (
      <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-6 text-center">
        <AlertCircle className="mx-auto h-8 w-8 text-destructive" />
        <p className="mt-2 text-sm text-destructive">
          Failed to load venue layout. Please try again later.
        </p>
      </div>
    );
  }

  // ---- If a layout exists, show details view ----
  if (layout) {
    return <LayoutDetailsView layout={layout} eventId={eventId} ticketTiers={ticketTiers} />;
  }

  // ---- No layout — show prompt or create form ----
  if (!showCreateForm) {
    return (
      <div className="flex flex-col items-center justify-center gap-4 rounded-lg border border-dashed border-neutral-300 bg-neutral-50 py-16">
        <Armchair className="h-12 w-12 text-neutral-400" />
        <p className="text-neutral-600">No venue layout configured for this event.</p>
        <Button onClick={() => setShowCreateForm(true)}>
          <Plus className="mr-2 h-4 w-4" />
          Create Layout
        </Button>
      </div>
    );
  }

  // ---- Zone helpers ----
  const addZone = () => {
    const nextColor = DEFAULT_ZONE_COLORS[zones.length % DEFAULT_ZONE_COLORS.length];
    setZones((prev) => [
      ...prev,
      { name: `Zone ${String.fromCharCode(65 + prev.length)}`, color: nextColor, ticketTierId: '' },
    ]);
  };

  const removeZone = (index: number) => {
    if (zones.length <= 1) return;
    setZones((prev) => prev.filter((_, i) => i !== index));
  };

  const updateZone = (index: number, field: keyof ZoneFormState, value: string) => {
    setZones((prev) => prev.map((z, i) => (i === index ? { ...z, [field]: value } : z)));
  };

  // ---- Submit handler ----
  const handleCreateLayout = () => {
    if (!layoutName.trim()) {
      toast.error('Please enter a layout name.');
      return;
    }
    if (zones.some((z) => !z.name.trim())) {
      toast.error('All zones must have a name.');
      return;
    }

    const request: CreateVenueLayoutRequest = {
      name: layoutName.trim(),
      layoutType,
      eventId,
      isTemplate: false,
      zones: zones.map<CreateVenueZoneRequest>((z, i) => ({
        name: z.name.trim(),
        color: z.color,
        ticketTierId: z.ticketTierId || null,
        sortOrder: i,
      })),
    };

    createLayout.mutate(request, {
      onSuccess: () => {
        toast.success('Layout created successfully.');
        setShowCreateForm(false);
      },
      onError: (err) => {
        toast.error(err.message || 'Failed to create layout.');
      },
    });
  };

  return (
    <div className="space-y-6">
      <h3 className="text-lg font-semibold">Create Venue Layout</h3>

      {/* Name */}
      <div>
        <label htmlFor="layout-name" className="mb-1 block text-sm font-medium text-neutral-700">
          Layout Name
        </label>
        <Input
          id="layout-name"
          placeholder="e.g. Main Hall Layout"
          value={layoutName}
          onChange={(e) => setLayoutName(e.target.value)}
        />
      </div>

      {/* Layout Type */}
      <div>
        <label htmlFor="layout-type" className="mb-1 block text-sm font-medium text-neutral-700">
          Layout Type
        </label>
        <select
          id="layout-type"
          className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
          value={layoutType}
          onChange={(e) => setLayoutType(e.target.value as LayoutType)}
        >
          <option value={LayoutType.Theater}>Theater (Rows &amp; Seats)</option>
          <option value={LayoutType.Banquet}>Banquet (Tables &amp; Seats)</option>
        </select>
      </div>

      {/* Zones */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <label className="text-sm font-medium text-neutral-700">Zones</label>
          <Button variant="outline" size="sm" onClick={addZone}>
            <Plus className="mr-1 h-3 w-3" />
            Add Zone
          </Button>
        </div>

        {zones.map((zone, index) => (
          <div
            key={index}
            className="flex flex-wrap items-end gap-3 rounded-md border border-neutral-200 bg-white p-3"
          >
            {/* Zone name */}
            <div className="min-w-[140px] flex-1">
              <label className="mb-1 block text-xs text-neutral-500">Name</label>
              <Input
                value={zone.name}
                onChange={(e) => updateZone(index, 'name', e.target.value)}
                placeholder="Zone name"
              />
            </div>

            {/* Color */}
            <div className="w-24">
              <label className="mb-1 block text-xs text-neutral-500">Color</label>
              <div className="flex items-center gap-2">
                <input
                  type="color"
                  value={zone.color}
                  onChange={(e) => updateZone(index, 'color', e.target.value)}
                  className="h-10 w-10 cursor-pointer rounded border border-neutral-200"
                />
                <span className="text-xs text-neutral-400">{zone.color}</span>
              </div>
            </div>

            {/* Ticket tier mapping */}
            {ticketTiers.length > 0 && (
              <div className="min-w-[160px] flex-1">
                <label className="mb-1 block text-xs text-neutral-500">Ticket Tier</label>
                <select
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                  value={zone.ticketTierId}
                  onChange={(e) => updateZone(index, 'ticketTierId', e.target.value)}
                >
                  <option value="">None</option>
                  {ticketTiers.map((tier) => (
                    <option key={tier.id} value={tier.id}>
                      {tier.name}
                    </option>
                  ))}
                </select>
              </div>
            )}

            {/* Remove */}
            {zones.length > 1 && (
              <Button variant="ghost" size="icon" onClick={() => removeZone(index)}>
                <Trash2 className="h-4 w-4 text-destructive" />
              </Button>
            )}
          </div>
        ))}
      </div>

      {/* Actions */}
      <div className="flex gap-3">
        <Button onClick={handleCreateLayout} loading={createLayout.isPending}>
          Create Layout
        </Button>
        <Button
          variant="outline"
          onClick={() => setShowCreateForm(false)}
          disabled={createLayout.isPending}
        >
          Cancel
        </Button>
      </div>
    </div>
  );
}

// ===========================================================================
// Layout Details Sub-Component
// ===========================================================================

interface LayoutDetailsViewProps {
  layout: VenueLayoutDto;
  eventId: string;
  ticketTiers: Array<{ id: string; name: string }>;
}

function LayoutDetailsView({ layout, eventId, ticketTiers }: LayoutDetailsViewProps) {
  const generateSeats = useGenerateSeats();
  const assignLayout = useAssignLayoutToEvent();

  const [seatGenForms, setSeatGenForms] = useState<Record<string, SeatGenFormState>>({});

  const isAssigned = layout.eventId === eventId;

  const getSeatGenForm = (zoneId: string): SeatGenFormState =>
    seatGenForms[zoneId] ?? { rowsOrTables: 5, seatsPerUnit: 10 };

  const updateSeatGenForm = (zoneId: string, field: keyof SeatGenFormState, value: number) => {
    setSeatGenForms((prev) => ({
      ...prev,
      [zoneId]: { ...getSeatGenForm(zoneId), [field]: value },
    }));
  };

  const handleGenerateSeats = (zoneId: string) => {
    const form = getSeatGenForm(zoneId);
    if (form.rowsOrTables < 1 || form.seatsPerUnit < 1) {
      toast.error('Rows/tables and seats per unit must be at least 1.');
      return;
    }

    const generationType =
      layout.layoutType === LayoutType.Banquet ? 'Table' : 'Row';

    const request: GenerateSeatsRequest = {
      generationType,
      rowsOrTables: form.rowsOrTables,
      seatsPerUnit: form.seatsPerUnit,
    };

    generateSeats.mutate(
      { layoutId: layout.id, zoneId, request },
      {
        onSuccess: () => toast.success('Seats generated successfully.'),
        onError: (err) => toast.error(err.message || 'Failed to generate seats.'),
      }
    );
  };

  const handleAssign = () => {
    assignLayout.mutate(
      { eventId, layoutId: layout.id },
      {
        onSuccess: () => toast.success('Layout assigned to event.'),
        onError: (err) => toast.error(err.message || 'Failed to assign layout.'),
      }
    );
  };

  const rowLabel = layout.layoutType === LayoutType.Banquet ? 'Tables' : 'Rows';

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h3 className="text-lg font-semibold">{layout.name}</h3>
          <p className="text-sm text-neutral-500">
            {layout.layoutType} layout &middot; {layout.totalCapacity} total seats
          </p>
        </div>
        {isAssigned ? (
          <span className="inline-flex items-center gap-1 rounded-full bg-green-50 px-3 py-1 text-xs font-medium text-green-700">
            <CheckCircle className="h-3 w-3" />
            Assigned
          </span>
        ) : (
          <Button onClick={handleAssign} loading={assignLayout.isPending} size="sm">
            Assign to Event
          </Button>
        )}
      </div>

      {/* Zones */}
      {layout.zones.map((zone) => (
        <ZoneCard
          key={zone.id}
          zone={zone}
          rowLabel={rowLabel}
          seatGenForm={getSeatGenForm(zone.id)}
          onUpdateSeatGenForm={(field, value) => updateSeatGenForm(zone.id, field, value)}
          onGenerateSeats={() => handleGenerateSeats(zone.id)}
          isGenerating={generateSeats.isPending}
          ticketTiers={ticketTiers}
        />
      ))}
    </div>
  );
}

// ===========================================================================
// Zone Card Sub-Component
// ===========================================================================

interface ZoneCardProps {
  zone: VenueZoneDto;
  rowLabel: string;
  seatGenForm: SeatGenFormState;
  onUpdateSeatGenForm: (field: keyof SeatGenFormState, value: number) => void;
  onGenerateSeats: () => void;
  isGenerating: boolean;
  ticketTiers: Array<{ id: string; name: string }>;
}

function ZoneCard({
  zone,
  rowLabel,
  seatGenForm,
  onUpdateSeatGenForm,
  onGenerateSeats,
  isGenerating,
  ticketTiers,
}: ZoneCardProps) {
  const tierName = useMemo(() => {
    if (!zone.ticketTierId) return null;
    return ticketTiers.find((t) => t.id === zone.ticketTierId)?.name ?? null;
  }, [zone.ticketTierId, ticketTiers]);

  const hasSeats = zone.seats.length > 0;

  return (
    <div className="rounded-lg border border-neutral-200 bg-white">
      {/* Zone header */}
      <div className="flex flex-wrap items-center gap-3 border-b border-neutral-100 px-4 py-3">
        <span
          className="inline-block h-4 w-4 rounded-full"
          style={{ backgroundColor: zone.color }}
        />
        <span className="font-medium">{zone.name}</span>
        <span className="text-sm text-neutral-500">
          {zone.enabledSeatCount} / {zone.totalSeatCount} seats
        </span>
        {tierName && (
          <span className="rounded bg-neutral-100 px-2 py-0.5 text-xs text-neutral-600">
            {tierName}
          </span>
        )}
      </div>

      <div className="p-4">
        {/* Seat generation panel */}
        {!hasSeats && (
          <div className="mb-4 rounded-md border border-dashed border-neutral-300 bg-neutral-50 p-4">
            <p className="mb-3 text-sm text-neutral-600">
              No seats generated yet. Configure and generate seats for this zone.
            </p>
            <div className="flex flex-wrap items-end gap-3">
              <div>
                <label className="mb-1 block text-xs text-neutral-500">{rowLabel}</label>
                <Input
                  type="number"
                  min={1}
                  max={100}
                  className="w-24"
                  value={seatGenForm.rowsOrTables}
                  onChange={(e) => onUpdateSeatGenForm('rowsOrTables', Number(e.target.value))}
                />
              </div>
              <div>
                <label className="mb-1 block text-xs text-neutral-500">Seats per {rowLabel === 'Tables' ? 'table' : 'row'}</label>
                <Input
                  type="number"
                  min={1}
                  max={50}
                  className="w-24"
                  value={seatGenForm.seatsPerUnit}
                  onChange={(e) => onUpdateSeatGenForm('seatsPerUnit', Number(e.target.value))}
                />
              </div>
              <Button size="sm" onClick={onGenerateSeats} loading={isGenerating}>
                Generate Seats
              </Button>
            </div>
          </div>
        )}

        {/* Re-generate option when seats exist */}
        {hasSeats && (
          <div className="mb-4 flex flex-wrap items-end gap-3">
            <div>
              <label className="mb-1 block text-xs text-neutral-500">{rowLabel}</label>
              <Input
                type="number"
                min={1}
                max={100}
                className="w-24"
                value={seatGenForm.rowsOrTables}
                onChange={(e) => onUpdateSeatGenForm('rowsOrTables', Number(e.target.value))}
              />
            </div>
            <div>
              <label className="mb-1 block text-xs text-neutral-500">Seats per {rowLabel === 'Tables' ? 'table' : 'row'}</label>
              <Input
                type="number"
                min={1}
                max={50}
                className="w-24"
                value={seatGenForm.seatsPerUnit}
                onChange={(e) => onUpdateSeatGenForm('seatsPerUnit', Number(e.target.value))}
              />
            </div>
            <Button variant="outline" size="sm" onClick={onGenerateSeats} loading={isGenerating}>
              Regenerate Seats
            </Button>
          </div>
        )}

        {/* Seat preview grid */}
        {hasSeats && <SeatPreviewGrid seats={zone.seats} zoneColor={zone.color} />}
      </div>
    </div>
  );
}

// ===========================================================================
// Seat Preview Grid Sub-Component
// ===========================================================================

interface SeatPreviewGridProps {
  seats: SeatDto[];
  zoneColor: string;
}

function SeatPreviewGrid({ seats, zoneColor }: SeatPreviewGridProps) {
  // Group seats by row
  const rowsMap = useMemo(() => {
    const map = new Map<string, SeatDto[]>();
    for (const seat of seats) {
      const existing = map.get(seat.row) ?? [];
      existing.push(seat);
      map.set(seat.row, existing);
    }
    // Sort seats within each row by number
    for (const [, rowSeats] of map) {
      rowSeats.sort((a, b) => a.number - b.number);
    }
    return map;
  }, [seats]);

  const sortedRows = useMemo(
    () => Array.from(rowsMap.entries()).sort(([a], [b]) => a.localeCompare(b, undefined, { numeric: true })),
    [rowsMap]
  );

  return (
    <div className="overflow-x-auto">
      <div className="inline-block min-w-0">
        {sortedRows.map(([rowLabel, rowSeats]) => (
          <div key={rowLabel} className="mb-1 flex items-center gap-1">
            <span className="w-8 text-right text-xs font-medium text-neutral-500">{rowLabel}</span>
            <div className="flex gap-1">
              {rowSeats.map((seat) => (
                <SeatCircle key={seat.id} seat={seat} zoneColor={zoneColor} />
              ))}
            </div>
          </div>
        ))}
      </div>
      <div className="mt-3 flex flex-wrap gap-4 text-xs text-neutral-500">
        <span className="flex items-center gap-1">
          <span
            className="inline-block h-3 w-3 rounded-full"
            style={{ backgroundColor: zoneColor }}
          />
          Available
        </span>
        <span className="flex items-center gap-1">
          <span className="inline-block h-3 w-3 rounded-full bg-neutral-300" />
          Disabled
        </span>
        <span className="flex items-center gap-1">
          <Accessibility className="h-3 w-3" />
          Accessible
        </span>
      </div>
    </div>
  );
}

// ===========================================================================
// Seat Circle Sub-Component
// ===========================================================================

interface SeatCircleProps {
  seat: SeatDto;
  zoneColor: string;
}

function SeatCircle({ seat, zoneColor }: SeatCircleProps) {
  const bgColor = seat.isEnabled ? zoneColor : '#D1D5DB';

  return (
    <div
      className="relative flex h-6 w-6 items-center justify-center rounded-full text-[8px] font-medium text-white"
      style={{ backgroundColor: bgColor }}
      title={`${seat.label}${seat.isAccessible ? ' (Accessible)' : ''}${!seat.isEnabled ? ' (Disabled)' : ''}`}
    >
      {seat.number}
      {seat.isAccessible && (
        <Accessibility className="absolute -right-0.5 -top-0.5 h-2.5 w-2.5 text-white drop-shadow" />
      )}
    </div>
  );
}
