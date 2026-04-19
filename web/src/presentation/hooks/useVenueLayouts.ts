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
