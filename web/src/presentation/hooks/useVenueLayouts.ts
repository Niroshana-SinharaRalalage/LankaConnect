/**
 * Venue Layout React Query Hooks
 *
 * Provides hooks for venue layout management and seat booking.
 * Includes seat availability polling (5-second interval during selection).
 */

import {
  useQuery,
  useMutation,
  useQueryClient,
  UseQueryOptions,
} from '@tanstack/react-query';

import { venueLayoutsRepository } from '@/infrastructure/api/repositories/venue-layouts.repository';
import { eventKeys } from '@/presentation/hooks/useEvents';
import type {
  VenueLayoutDto,
  SeatAvailabilityDto,
  HoldSeatsResult,
  CreateVenueLayoutRequest,
  GenerateSeatsRequest,
  AssignLayoutRequest,
  HoldSeatsRequest,
  ReleaseSeatsRequest,
  UpdateVenueLayoutRequest,
  UpdateZoneRequest,
  AddTableRequest,
  AddTableResponse,
  UpdateTableRequest,
  AddDecorationRequest,
  AddDecorationResponse,
  UpdateDecorationRequest,
  AssignTierRequest,
  AssignableKind,
  BatchLayoutPayload,
  LayoutPresetDto,
  CreateLayoutFromPresetRequest,
  CreateLayoutFromTemplateRequest,
} from '@/infrastructure/api/types/events.types';

import { ApiError } from '@/infrastructure/api/client/api-errors';

/**
 * Query keys for venue layouts and seat availability
 */
export const venueLayoutKeys = {
  all: ['venue-layouts'] as const,
  detail: (id: string) => [...venueLayoutKeys.all, 'detail', id] as const,
  byEvent: (eventId: string) => [...venueLayoutKeys.all, 'by-event', eventId] as const,
  seatAvailability: (eventId: string) => [...venueLayoutKeys.all, 'seats', eventId] as const,
  presets: [...['venue-layouts'] as const, 'presets'] as const,
  /** Slice 8 S8.10: per-user template list. Stable across mounts so the
   * "Mine" tab in PresetLibraryModal hits the cache on re-open. */
  userTemplates: [...['venue-layouts'] as const, 'my-templates'] as const,
};

/**
 * Get a venue layout by ID
 */
export function useVenueLayout(
  layoutId: string | undefined,
  options?: Omit<UseQueryOptions<VenueLayoutDto, ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: venueLayoutKeys.detail(layoutId!),
    queryFn: () => venueLayoutsRepository.getLayout(layoutId!),
    enabled: !!layoutId,
    staleTime: 5 * 60 * 1000,
    ...options,
  });
}

/**
 * Get the venue layout assigned to an event
 */
export function useVenueLayoutByEvent(
  eventId: string | undefined,
  options?: Omit<UseQueryOptions<VenueLayoutDto | null, ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: venueLayoutKeys.byEvent(eventId!),
    queryFn: () => venueLayoutsRepository.getLayoutByEvent(eventId!),
    enabled: !!eventId,
    staleTime: 5 * 60 * 1000,
    ...options,
  });
}

/**
 * Get seat availability for an event.
 * When `polling` is true, refetches every 5 seconds for live availability.
 */
export function useSeatAvailability(
  eventId: string | undefined,
  polling = false,
  options?: Omit<UseQueryOptions<SeatAvailabilityDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: venueLayoutKeys.seatAvailability(eventId!),
    queryFn: () => venueLayoutsRepository.getSeatAvailability(eventId!),
    enabled: !!eventId,
    staleTime: polling ? 0 : 30 * 1000,
    refetchInterval: polling ? 5000 : false,
    ...options,
  });
}

/**
 * Create a new venue layout
 */
export function useCreateVenueLayout() {
  const queryClient = useQueryClient();

  return useMutation<VenueLayoutDto, ApiError, CreateVenueLayoutRequest>({
    mutationFn: (request) => venueLayoutsRepository.createLayout(request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: venueLayoutKeys.all });
      if (data.eventId) {
        queryClient.invalidateQueries({ queryKey: venueLayoutKeys.byEvent(data.eventId) });
      }
    },
  });
}

/**
 * Slice 6 S6.5: lists the 8 built-in layout presets. The metadata is static
 * server-side code, so the cache stays fresh for the full session.
 */
export function useLayoutPresets(
  options?: Omit<UseQueryOptions<LayoutPresetDto[], ApiError>, 'queryKey' | 'queryFn'>,
) {
  return useQuery({
    queryKey: venueLayoutKeys.presets,
    queryFn: () => venueLayoutsRepository.listPresets(),
    staleTime: Infinity,
    ...options,
  });
}

/**
 * Slice 6 S6.5: creates a layout from a preset. When `eventId` is supplied,
 * invalidates the event's layout cache; always invalidates the shared layouts
 * tree so any listing hooks refetch.
 */
export function useCreateLayoutFromPreset() {
  const queryClient = useQueryClient();

  return useMutation<VenueLayoutDto, ApiError, CreateLayoutFromPresetRequest>({
    mutationFn: (request) => venueLayoutsRepository.createFromPreset(request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: venueLayoutKeys.all });
      if (data.eventId) {
        queryClient.invalidateQueries({ queryKey: venueLayoutKeys.byEvent(data.eventId) });
      }
    },
  });
}

/**
 * Slice 8 S8.10: lists the calling user's saved templates. Empty array when
 * the user has none. Powers the "My Templates" tab in `PresetLibraryModal`.
 * The list is invalidated on `useSaveLayoutAsTemplate` success (S8.9b) and on
 * `useCreateLayoutFromTemplate` success (S8.10), so newly-saved templates
 * appear without a manual refetch.
 */
export function useUserTemplates(
  options?: Omit<UseQueryOptions<VenueLayoutDto[], ApiError>, 'queryKey' | 'queryFn'>,
) {
  return useQuery({
    queryKey: venueLayoutKeys.userTemplates,
    queryFn: () => venueLayoutsRepository.listUserTemplates(),
    ...options,
  });
}

/**
 * Slice 8 S8.10: applies a saved template to a target event. Mirror of
 * `useCreateLayoutFromPreset` for user templates instead of built-in presets.
 * Invalidates the target event's layout cache (so the picker refetches and
 * the new layout shows up) and the shared layouts tree.
 */
export function useCreateLayoutFromTemplate() {
  const queryClient = useQueryClient();

  return useMutation<VenueLayoutDto, ApiError, CreateLayoutFromTemplateRequest>({
    mutationFn: (request) => venueLayoutsRepository.createFromTemplate(request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: venueLayoutKeys.all });
      if (data.eventId) {
        queryClient.invalidateQueries({ queryKey: venueLayoutKeys.byEvent(data.eventId) });
      }
    },
  });
}

/**
 * Slice 8 S8.9b — POST /api/venue-layouts/{sourceLayoutId}/save-as-template.
 * Clones an existing layout as a per-user template. Returns the newly-created
 * template DTO; the source layout is unchanged. Invalidates the layout-list
 * cache so the new template appears in the user's templates list when it's
 * opened (Slice 8 still doesn't have a "My Templates" picker UI — the
 * invalidation is futureproofing against the upcoming preset-modal "Mine" tab).
 */
export function useSaveLayoutAsTemplate() {
  const queryClient = useQueryClient();

  return useMutation<
    VenueLayoutDto,
    ApiError,
    { sourceLayoutId: string; templateName: string }
  >({
    mutationFn: ({ sourceLayoutId, templateName }) =>
      venueLayoutsRepository.saveLayoutAsTemplate(sourceLayoutId, templateName),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: venueLayoutKeys.all });
    },
  });
}

/**
 * Generate seats for a zone
 */
export function useGenerateSeats() {
  const queryClient = useQueryClient();

  return useMutation<
    VenueLayoutDto,
    ApiError,
    { layoutId: string; zoneId: string; request: GenerateSeatsRequest }
  >({
    mutationFn: ({ layoutId, zoneId, request }) =>
      venueLayoutsRepository.generateSeats(layoutId, zoneId, request),
    onSuccess: (data) => {
      queryClient.setQueryData(venueLayoutKeys.detail(data.id), data);
      if (data.eventId) {
        queryClient.invalidateQueries({ queryKey: venueLayoutKeys.byEvent(data.eventId) });
      }
    },
  });
}

/**
 * Assign a layout to an event
 */
export function useAssignLayoutToEvent() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, AssignLayoutRequest>({
    mutationFn: (request) => venueLayoutsRepository.assignLayoutToEvent(request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: venueLayoutKeys.byEvent(variables.eventId),
      });
      queryClient.invalidateQueries({
        queryKey: venueLayoutKeys.seatAvailability(variables.eventId),
      });
      // Event's seatingMode / venueLayoutId changed on the backend — refetch event.
      queryClient.invalidateQueries({
        queryKey: eventKeys.detail(variables.eventId),
      });
    },
  });
}

/**
 * Hold seats for the current user
 */
export function useHoldSeats(eventId: string) {
  const queryClient = useQueryClient();

  return useMutation<HoldSeatsResult, ApiError, HoldSeatsRequest>({
    mutationFn: (request) => venueLayoutsRepository.holdSeats(eventId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: venueLayoutKeys.seatAvailability(eventId),
      });
    },
  });
}

/**
 * Release held seats
 */
export function useReleaseSeats(eventId: string) {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, ReleaseSeatsRequest>({
    mutationFn: (request) => venueLayoutsRepository.releaseSeats(eventId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: venueLayoutKeys.seatAvailability(eventId),
      });
    },
  });
}

// ==================== SLICE 5 CHUNK 11 — LAYOUT CRUD MUTATIONS ====================

/**
 * Invalidate caches downstream of a structural layout mutation. Always touches
 * the layout's detail cache; touches the by-event + event-detail caches only
 * when the layout is attached to an event.
 */
function invalidateLayoutScopes(
  queryClient: ReturnType<typeof useQueryClient>,
  layoutId: string,
  eventId?: string | null,
  includeSeatAvailability = false
) {
  queryClient.invalidateQueries({ queryKey: venueLayoutKeys.detail(layoutId) });
  if (eventId) {
    queryClient.invalidateQueries({ queryKey: venueLayoutKeys.byEvent(eventId) });
    if (includeSeatAvailability) {
      queryClient.invalidateQueries({
        queryKey: venueLayoutKeys.seatAvailability(eventId),
      });
    }
  }
}

/** Slice 5 Chunk 4 — PUT /api/venue-layouts/{id} (name/canvas). */
export function useUpdateVenueLayout(layoutId: string, eventId?: string | null) {
  const queryClient = useQueryClient();

  return useMutation<
    void,
    ApiError,
    { rowVersion: number; request: UpdateVenueLayoutRequest }
  >({
    mutationFn: ({ rowVersion, request }) =>
      venueLayoutsRepository.updateLayout(layoutId, rowVersion, request),
    onSuccess: () => invalidateLayoutScopes(queryClient, layoutId, eventId),
  });
}

/** Slice 5 Chunk 9 — DELETE /api/venue-layouts/{id} (hard delete). */
export function useDeleteVenueLayout(layoutId: string, eventId?: string | null) {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, { rowVersion: number }>({
    mutationFn: ({ rowVersion }) =>
      venueLayoutsRepository.deleteLayout(layoutId, rowVersion),
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: venueLayoutKeys.detail(layoutId) });
      if (eventId) {
        queryClient.invalidateQueries({ queryKey: venueLayoutKeys.byEvent(eventId) });
        queryClient.invalidateQueries({
          queryKey: venueLayoutKeys.seatAvailability(eventId),
        });
        // Layout delete flips event.SeatingMode → GeneralAdmission and clears VenueLayoutId.
        queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });
      }
    },
  });
}

/** Slice 5 Chunk 10 — PUT /api/venue-layouts/{id}/batch (atomic full replacement). */
export function useBatchUpdateVenueLayout(layoutId: string, eventId?: string | null) {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, { rowVersion: number; payload: BatchLayoutPayload }>({
    mutationFn: ({ rowVersion, payload }) =>
      venueLayoutsRepository.batchUpdateLayout(layoutId, rowVersion, payload),
    // Batch can create/remove seats, so seat availability must refetch too.
    onSuccess: () => invalidateLayoutScopes(queryClient, layoutId, eventId, true),
  });
}

/** Slice 5 Chunk 5 — PATCH /api/venue-layouts/{id}/zones/{zoneId}. */
export function useUpdateZone(layoutId: string, eventId?: string | null) {
  const queryClient = useQueryClient();

  return useMutation<
    void,
    ApiError,
    { zoneId: string; rowVersion: number; request: UpdateZoneRequest }
  >({
    mutationFn: ({ zoneId, rowVersion, request }) =>
      venueLayoutsRepository.updateZone(layoutId, zoneId, rowVersion, request),
    onSuccess: () => invalidateLayoutScopes(queryClient, layoutId, eventId),
  });
}

/** Slice 5 Chunk 5 — DELETE /api/venue-layouts/{id}/zones/{zoneId}. */
export function useDeleteZone(layoutId: string, eventId?: string | null) {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, { zoneId: string; rowVersion: number }>({
    mutationFn: ({ zoneId, rowVersion }) =>
      venueLayoutsRepository.deleteZone(layoutId, zoneId, rowVersion),
    // Zone delete cascades to seats → availability cache is now stale.
    onSuccess: () => invalidateLayoutScopes(queryClient, layoutId, eventId, true),
  });
}

/** Slice 5 Chunk 6 — POST /api/venue-layouts/{id}/tables (auto-generates seats). */
export function useAddTable(layoutId: string, eventId?: string | null) {
  const queryClient = useQueryClient();

  return useMutation<
    AddTableResponse,
    ApiError,
    { rowVersion: number; request: AddTableRequest }
  >({
    mutationFn: ({ rowVersion, request }) =>
      venueLayoutsRepository.addTable(layoutId, rowVersion, request),
    onSuccess: () => invalidateLayoutScopes(queryClient, layoutId, eventId, true),
  });
}

/** Slice 5 Chunk 6 — PATCH /api/venue-layouts/{id}/tables/{tableId}. */
export function useUpdateTable(layoutId: string, eventId?: string | null) {
  const queryClient = useQueryClient();

  return useMutation<
    void,
    ApiError,
    { tableId: string; rowVersion: number; request: UpdateTableRequest }
  >({
    mutationFn: ({ tableId, rowVersion, request }) =>
      venueLayoutsRepository.updateTable(layoutId, tableId, rowVersion, request),
    // Capacity/shape changes regenerate seats → availability may change.
    onSuccess: () => invalidateLayoutScopes(queryClient, layoutId, eventId, true),
  });
}

/** Slice 5 Chunk 6 — DELETE /api/venue-layouts/{id}/tables/{tableId}. */
export function useDeleteTable(layoutId: string, eventId?: string | null) {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, { tableId: string; rowVersion: number }>({
    mutationFn: ({ tableId, rowVersion }) =>
      venueLayoutsRepository.deleteTable(layoutId, tableId, rowVersion),
    onSuccess: () => invalidateLayoutScopes(queryClient, layoutId, eventId, true),
  });
}

/** Slice 5 Chunk 7 — POST /api/venue-layouts/{id}/decorations. */
export function useAddDecoration(layoutId: string, eventId?: string | null) {
  const queryClient = useQueryClient();

  return useMutation<
    AddDecorationResponse,
    ApiError,
    { rowVersion: number; request: AddDecorationRequest }
  >({
    mutationFn: ({ rowVersion, request }) =>
      venueLayoutsRepository.addDecoration(layoutId, rowVersion, request),
    onSuccess: () => invalidateLayoutScopes(queryClient, layoutId, eventId),
  });
}

/** Slice 5 Chunk 7 — PATCH /api/venue-layouts/{id}/decorations/{decorationId}. */
export function useUpdateDecoration(layoutId: string, eventId?: string | null) {
  const queryClient = useQueryClient();

  return useMutation<
    void,
    ApiError,
    { decorationId: string; rowVersion: number; request: UpdateDecorationRequest }
  >({
    mutationFn: ({ decorationId, rowVersion, request }) =>
      venueLayoutsRepository.updateDecoration(layoutId, decorationId, rowVersion, request),
    onSuccess: () => invalidateLayoutScopes(queryClient, layoutId, eventId),
  });
}

/** Slice 5 Chunk 7 — DELETE /api/venue-layouts/{id}/decorations/{decorationId}. */
export function useDeleteDecoration(layoutId: string, eventId?: string | null) {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, { decorationId: string; rowVersion: number }>({
    mutationFn: ({ decorationId, rowVersion }) =>
      venueLayoutsRepository.deleteDecoration(layoutId, decorationId, rowVersion),
    onSuccess: () => invalidateLayoutScopes(queryClient, layoutId, eventId),
  });
}

/** Slice 5 Chunk 8 — POST /api/venue-layouts/{id}/tier-assignments. */
export function useAssignTier(layoutId: string, eventId?: string | null) {
  const queryClient = useQueryClient();

  return useMutation<
    void,
    ApiError,
    { rowVersion: number; request: AssignTierRequest }
  >({
    mutationFn: ({ rowVersion, request }) =>
      venueLayoutsRepository.assignTier(layoutId, rowVersion, request),
    onSuccess: () => invalidateLayoutScopes(queryClient, layoutId, eventId),
  });
}

/** Slice 5 Chunk 8 — DELETE /api/venue-layouts/{id}/tier-assignments/{tierId}/{kind}/{assignableId}. */
export function useRemoveTierAssignment(layoutId: string, eventId?: string | null) {
  const queryClient = useQueryClient();

  return useMutation<
    void,
    ApiError,
    {
      tierId: string;
      kind: AssignableKind | string;
      assignableId: string;
      rowVersion: number;
    }
  >({
    mutationFn: ({ tierId, kind, assignableId, rowVersion }) =>
      venueLayoutsRepository.removeTierAssignment(
        layoutId,
        tierId,
        kind,
        assignableId,
        rowVersion
      ),
    onSuccess: () => invalidateLayoutScopes(queryClient, layoutId, eventId),
  });
}
