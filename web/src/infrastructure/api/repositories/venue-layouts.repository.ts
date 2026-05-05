import { apiClient } from '../client/api-client';
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
  BatchLayoutPayload,
  AssignableKind,
  LayoutPresetDto,
  CreateLayoutFromPresetRequest,
  CreateLayoutFromTemplateRequest,
  ApplyPresetToEventRequest,
  ApplyTemplateToEventRequest,
  PublishReadinessReportDto,
} from '../types/events.types';

/**
 * VenueLayoutsRepository
 * Handles all venue layout and seat booking API calls.
 *
 * Backend endpoints from VenueLayoutsController.cs:
 * - POST   /api/venue-layouts — create layout with zones
 * - GET    /api/venue-layouts/{id} — get layout by ID
 * - GET    /api/venue-layouts/by-event/{eventId} — get layout for event
 * - PUT    /api/venue-layouts/{id} — update layout name/canvas (If-Match)
 * - DELETE /api/venue-layouts/{id} — hard-delete layout (If-Match)
 * - PUT    /api/venue-layouts/{id}/batch — atomic batch update (If-Match)
 * - PATCH  /api/venue-layouts/{id}/zones/{zoneId} — update zone (If-Match)
 * - DELETE /api/venue-layouts/{id}/zones/{zoneId} — delete zone (If-Match)
 * - POST   /api/venue-layouts/{id}/tables — add table (If-Match)
 * - PATCH  /api/venue-layouts/{id}/tables/{tableId} — update table (If-Match)
 * - DELETE /api/venue-layouts/{id}/tables/{tableId} — delete table (If-Match)
 * - POST   /api/venue-layouts/{id}/decorations — add decoration (If-Match)
 * - PATCH  /api/venue-layouts/{id}/decorations/{decorationId} — update decoration (If-Match)
 * - DELETE /api/venue-layouts/{id}/decorations/{decorationId} — delete decoration (If-Match)
 * - POST   /api/venue-layouts/{id}/tier-assignments — assign tier (If-Match)
 * - DELETE /api/venue-layouts/{id}/tier-assignments/{tierId}/{kind}/{assignableId} — remove tier (If-Match)
 * - POST   /api/venue-layouts/{layoutId}/zones/{zoneId}/generate-seats — generate seats
 * - POST   /api/venue-layouts/assign — assign layout to event
 * - GET    /api/venue-layouts/events/{eventId}/seats — seat availability
 * - POST   /api/venue-layouts/events/{eventId}/seats/hold — hold seats
 * - POST   /api/venue-layouts/events/{eventId}/seats/release — release seats
 */
export class VenueLayoutsRepository {
  private readonly basePath = '/venue-layouts';

  /**
   * Build an Axios config that carries the `If-Match` header required by every
   * mutating Slice 5 endpoint for optimistic concurrency. The RowVersion comes
   * from the layout's last GET response; the backend compares against the
   * current `xmin` and returns 409 on mismatch.
   */
  private ifMatch(rowVersion: number): { headers: Record<string, string> } {
    return { headers: { 'If-Match': rowVersion.toString() } };
  }

  // ==================== LAYOUT MANAGEMENT ====================

  async createLayout(request: CreateVenueLayoutRequest): Promise<VenueLayoutDto> {
    return await apiClient.post<VenueLayoutDto>(this.basePath, request);
  }

  /**
   * Slice 6 S6.5: fetches the 8-preset metadata list powering the
   * preset-library modal. Safe to call on every modal open — server-side
   * is static code, no DB hit.
   */
  async listPresets(): Promise<LayoutPresetDto[]> {
    return await apiClient.get<LayoutPresetDto[]>(`${this.basePath}/presets`);
  }

  /**
   * Slice 8 S8.10: lists every venue layout the calling user has saved as a
   * template (`isTemplate=true` AND `createdByUserId=caller`), most-recent-first.
   * Powers the "My Templates" tab in `PresetLibraryModal`. Empty array when
   * the user has no saved templates.
   */
  async listUserTemplates(): Promise<VenueLayoutDto[]> {
    return await apiClient.get<VenueLayoutDto[]>(`${this.basePath}/templates`);
  }

  /**
   * Slice 8 S8.10: applies one of the caller's saved templates to a target
   * event. Mirror of {@link createFromPreset} for user templates instead of
   * built-in presets. The new layout is event-attached
   * (`isTemplate=false`, `eventId=request.eventId`); the source template is
   * unchanged. Throws `ApiError` on 403 (caller doesn't own template OR
   * isn't event organizer) / 404 (source or target missing).
   */
  async createFromTemplate(
    request: CreateLayoutFromTemplateRequest,
  ): Promise<VenueLayoutDto> {
    return await apiClient.post<VenueLayoutDto>(
      `${this.basePath}/from-template`,
      request,
    );
  }

  /**
   * Slice 6 S6.5: builds a new layout from the given preset ID. When
   * `eventId` is supplied the layout is attached to that event (caller
   * must own it); otherwise a per-user template is created.
   */
  async createFromPreset(
    request: CreateLayoutFromPresetRequest,
  ): Promise<VenueLayoutDto> {
    return await apiClient.post<VenueLayoutDto>(
      `${this.basePath}/from-preset`,
      request,
    );
  }

  /**
   * Slice 9.2: atomic preset apply. Single round-trip replacement for the
   * broken {@link createFromPreset} + {@link assignLayoutToEvent} two-step.
   * The backend creates the layout AND flips the event into assigned-seating
   * mode pointing at the new layout in a single transaction — no orphan on
   * partial failure. No auto-tier-mapping (organiser maps later in Customize).
   * 403 → caller does not own the event. 404 → event not found.
   */
  async applyPresetToEvent(
    request: ApplyPresetToEventRequest,
  ): Promise<VenueLayoutDto> {
    return await apiClient.post<VenueLayoutDto>(
      `${this.basePath}/apply-preset`,
      request,
    );
  }

  /**
   * Slice 9.2: atomic template apply. Mirror of {@link applyPresetToEvent}
   * for user-saved templates.
   */
  async applyTemplateToEvent(
    request: ApplyTemplateToEventRequest,
  ): Promise<VenueLayoutDto> {
    return await apiClient.post<VenueLayoutDto>(
      `${this.basePath}/apply-template`,
      request,
    );
  }

  /**
   * Slice 8 S8.9b: clones an existing layout as a per-user template
   * (`isTemplate=true`, `eventId=null`, `createdByUserId` = current user).
   * Backend's `VenueLayout.CloneAsTemplate` factory preserves zones, tables,
   * decorations, canvas, and per-seat `IsEnabled`/`IsAccessible` flags.
   * Tier mappings are deliberately dropped — templates are tier-free.
   *
   * Returns the newly-created template DTO. Throws an `ApiError` when the
   * caller isn't authorized to read the source layout (403) or when the
   * source can't be found (404).
   */
  async saveLayoutAsTemplate(
    sourceLayoutId: string,
    templateName: string,
  ): Promise<VenueLayoutDto> {
    return await apiClient.post<VenueLayoutDto>(
      `${this.basePath}/${sourceLayoutId}/save-as-template`,
      { templateName },
    );
  }

  async getLayout(layoutId: string): Promise<VenueLayoutDto> {
    return await apiClient.get<VenueLayoutDto>(`${this.basePath}/${layoutId}`);
  }

  async getLayoutByEvent(eventId: string): Promise<VenueLayoutDto | null> {
    try {
      return await apiClient.get<VenueLayoutDto>(
        `${this.basePath}/by-event/${eventId}`
      );
    } catch {
      return null;
    }
  }

  /**
   * Slice S4 — non-gating publish-readiness snapshot. Returns every blocker +
   * warning + per-tier mapping summary at once. Distinct from the strict
   * publish gate (`POST /api/Events/{id}/publish`) which short-circuits on
   * the first issue.
   */
  async getLayoutPublishReadiness(
    layoutId: string,
  ): Promise<PublishReadinessReportDto> {
    return await apiClient.get<PublishReadinessReportDto>(
      `${this.basePath}/${layoutId}/publish-readiness`,
    );
  }

  /** Slice 5 Chunk 4 — update layout name/canvas. Requires If-Match. */
  async updateLayout(
    layoutId: string,
    rowVersion: number,
    request: UpdateVenueLayoutRequest
  ): Promise<void> {
    await apiClient.put(`${this.basePath}/${layoutId}`, request, this.ifMatch(rowVersion));
  }

  /** Slice 5 Chunk 9 — hard-delete layout. Requires If-Match. */
  async deleteLayout(layoutId: string, rowVersion: number): Promise<void> {
    await apiClient.delete(`${this.basePath}/${layoutId}`, this.ifMatch(rowVersion));
  }

  /**
   * Slice 5 Chunk 10 — atomic batch update. Sends a full layout snapshot; the
   * backend resolves creates/updates/removes in one transaction. Requires
   * If-Match. Consumed by the Slice 8 canvas editor save flow.
   */
  async batchUpdateLayout(
    layoutId: string,
    rowVersion: number,
    payload: BatchLayoutPayload
  ): Promise<void> {
    await apiClient.put(
      `${this.basePath}/${layoutId}/batch`,
      payload,
      this.ifMatch(rowVersion)
    );
  }

  // ==================== ZONES ====================

  /** Slice 5 Chunk 5 — update zone (name/color/sort/shape/geometry). Requires If-Match. */
  async updateZone(
    layoutId: string,
    zoneId: string,
    rowVersion: number,
    request: UpdateZoneRequest
  ): Promise<void> {
    await apiClient.patch(
      `${this.basePath}/${layoutId}/zones/${zoneId}`,
      request,
      this.ifMatch(rowVersion)
    );
  }

  /** Slice 5 Chunk 5 — delete zone. Requires If-Match. */
  async deleteZone(
    layoutId: string,
    zoneId: string,
    rowVersion: number
  ): Promise<void> {
    await apiClient.delete(
      `${this.basePath}/${layoutId}/zones/${zoneId}`,
      this.ifMatch(rowVersion)
    );
  }

  // ==================== TABLES ====================

  /** Slice 5 Chunk 6 — add table (auto-generates seats). Requires If-Match. */
  async addTable(
    layoutId: string,
    rowVersion: number,
    request: AddTableRequest
  ): Promise<AddTableResponse> {
    return await apiClient.post<AddTableResponse>(
      `${this.basePath}/${layoutId}/tables`,
      request,
      this.ifMatch(rowVersion)
    );
  }

  /** Slice 5 Chunk 6 — update table (label/shape/capacity/geometry/zone). Requires If-Match. */
  async updateTable(
    layoutId: string,
    tableId: string,
    rowVersion: number,
    request: UpdateTableRequest
  ): Promise<void> {
    await apiClient.patch(
      `${this.basePath}/${layoutId}/tables/${tableId}`,
      request,
      this.ifMatch(rowVersion)
    );
  }

  /** Slice 5 Chunk 6 — delete table. Requires If-Match. */
  async deleteTable(
    layoutId: string,
    tableId: string,
    rowVersion: number
  ): Promise<void> {
    await apiClient.delete(
      `${this.basePath}/${layoutId}/tables/${tableId}`,
      this.ifMatch(rowVersion)
    );
  }

  // ==================== DECORATIONS ====================

  /** Slice 5 Chunk 7 — add decoration. Requires If-Match. */
  async addDecoration(
    layoutId: string,
    rowVersion: number,
    request: AddDecorationRequest
  ): Promise<AddDecorationResponse> {
    return await apiClient.post<AddDecorationResponse>(
      `${this.basePath}/${layoutId}/decorations`,
      request,
      this.ifMatch(rowVersion)
    );
  }

  /** Slice 5 Chunk 7 — update decoration. Requires If-Match. */
  async updateDecoration(
    layoutId: string,
    decorationId: string,
    rowVersion: number,
    request: UpdateDecorationRequest
  ): Promise<void> {
    await apiClient.patch(
      `${this.basePath}/${layoutId}/decorations/${decorationId}`,
      request,
      this.ifMatch(rowVersion)
    );
  }

  /** Slice 5 Chunk 7 — delete decoration. Requires If-Match. */
  async deleteDecoration(
    layoutId: string,
    decorationId: string,
    rowVersion: number
  ): Promise<void> {
    await apiClient.delete(
      `${this.basePath}/${layoutId}/decorations/${decorationId}`,
      this.ifMatch(rowVersion)
    );
  }

  // ==================== TIER ASSIGNMENTS ====================

  /** Slice 5 Chunk 8 — assign ticket tier to zone/table. Requires If-Match. */
  async assignTier(
    layoutId: string,
    rowVersion: number,
    request: AssignTierRequest
  ): Promise<void> {
    await apiClient.post(
      `${this.basePath}/${layoutId}/tier-assignments`,
      request,
      this.ifMatch(rowVersion)
    );
  }

  /** Slice 5 Chunk 8 — remove tier assignment. Requires If-Match. */
  async removeTierAssignment(
    layoutId: string,
    tierId: string,
    kind: AssignableKind | string,
    assignableId: string,
    rowVersion: number
  ): Promise<void> {
    await apiClient.delete(
      `${this.basePath}/${layoutId}/tier-assignments/${tierId}/${kind}/${assignableId}`,
      this.ifMatch(rowVersion)
    );
  }

  // ==================== SEAT GENERATION + ASSIGNMENT ====================

  async generateSeats(
    layoutId: string,
    zoneId: string,
    request: GenerateSeatsRequest
  ): Promise<VenueLayoutDto> {
    return await apiClient.post<VenueLayoutDto>(
      `${this.basePath}/${layoutId}/zones/${zoneId}/generate-seats`,
      request
    );
  }

  async assignLayoutToEvent(request: AssignLayoutRequest): Promise<void> {
    await apiClient.post(`${this.basePath}/assign`, request);
  }

  // ==================== SEAT AVAILABILITY ====================

  async getSeatAvailability(eventId: string): Promise<SeatAvailabilityDto[]> {
    return await apiClient.get<SeatAvailabilityDto[]>(
      `${this.basePath}/events/${eventId}/seats`
    );
  }

  // ==================== SEAT HOLD/RELEASE ====================

  async holdSeats(eventId: string, request: HoldSeatsRequest): Promise<HoldSeatsResult> {
    return await apiClient.post<HoldSeatsResult>(
      `${this.basePath}/events/${eventId}/seats/hold`,
      request
    );
  }

  async releaseSeats(eventId: string, request: ReleaseSeatsRequest): Promise<void> {
    await apiClient.post(`${this.basePath}/events/${eventId}/seats/release`, request);
  }

  /**
   * Slice 7 S7.8: fire-and-forget client-reported seating metric. Posts to
   * `POST /api/seating-metrics/selection-completed` which emits the
   * `seatpicker.selection_completed` named log metric on the backend.
   * Errors are swallowed — this must never block the registration flow.
   */
  async recordSeatPickerSelectionCompleted(
    eventId: string,
    attendeeCount: number,
    timeToCompleteMs: number
  ): Promise<void> {
    try {
      await apiClient.post('/seating-metrics/selection-completed', {
        eventId,
        attendeeCount,
        timeToCompleteMs,
      });
    } catch {
      // Metrics are best-effort — intentionally swallow failures.
    }
  }

  /**
   * Slice 8 S8.1: fire-and-forget metric — organizer opened the canvas editor.
   * Errors swallowed so a metrics-service outage cannot block the editor flow.
   */
  async recordCanvasEditorOpened(layoutId: string): Promise<void> {
    try {
      await apiClient.post('/seating-metrics/canvas-editor-opened', { layoutId });
    } catch {
      // Metrics are best-effort — intentionally swallow failures.
    }
  }

  /**
   * Slice 8 S8.8: fire-and-forget metric — organizer saved a canvas-editor session.
   * `changesCount` is the number of mutations packed into the companion PUT /batch payload.
   */
  async recordCanvasEditorSaved(layoutId: string, changesCount: number): Promise<void> {
    try {
      await apiClient.post('/seating-metrics/canvas-editor-saved', {
        layoutId,
        changesCount,
      });
    } catch {
      // Metrics are best-effort — intentionally swallow failures.
    }
  }
}

export const venueLayoutsRepository = new VenueLayoutsRepository();
