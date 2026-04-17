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
} from '../types/events.types';

/**
 * VenueLayoutsRepository
 * Handles all venue layout and seat booking API calls.
 *
 * Backend endpoints from VenueLayoutsController.cs:
 * - POST /api/venue-layouts — create layout with zones
 * - GET /api/venue-layouts/{id} — get layout by ID
 * - GET /api/venue-layouts/by-event/{eventId} — get layout for event
 * - POST /api/venue-layouts/{id}/zones/{zoneId}/generate-seats — generate seats
 * - POST /api/venue-layouts/assign — assign layout to event
 * - GET /api/venue-layouts/events/{eventId}/seats — seat availability
 * - POST /api/venue-layouts/events/{eventId}/seats/hold — hold seats
 * - POST /api/venue-layouts/events/{eventId}/seats/release — release seats
 */
export class VenueLayoutsRepository {
  private readonly basePath = '/venue-layouts';

  // ==================== LAYOUT MANAGEMENT ====================

  async createLayout(request: CreateVenueLayoutRequest): Promise<VenueLayoutDto> {
    return await apiClient.post<VenueLayoutDto>(this.basePath, request);
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
}

export const venueLayoutsRepository = new VenueLayoutsRepository();
