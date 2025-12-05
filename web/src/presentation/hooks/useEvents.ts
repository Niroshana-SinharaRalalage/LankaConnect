/**
 * Events React Query Hooks
 *
 * Provides React Query hooks for Events API integration
 * Implements caching, optimistic updates, and proper error handling
 *
 * PREREQUISITES:
 * - events.repository.ts must be created in infrastructure/repositories/
 * - events.types.ts must be created in infrastructure/api/types/
 *
 * @requires @tanstack/react-query
 * @requires eventsRepository from infrastructure/repositories/events.repository
 * @requires Event types from infrastructure/api/types/events.types
 */

import {
  useQuery,
  useMutation,
  useQueryClient,
  UseQueryOptions,
  UseMutationOptions,
} from '@tanstack/react-query';

import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type {
  EventDto,
  GetEventsRequest,
  CreateEventRequest,
  UpdateEventRequest,
  RsvpRequest,
  RegistrationDetailsDto,
} from '@/infrastructure/api/types/events.types';

import { ApiError } from '@/infrastructure/api/client/api-errors';

/**
 * Query Keys for Events
 * Centralized query key management for cache invalidation
 */
export const eventKeys = {
  all: ['events'] as const,
  lists: () => [...eventKeys.all, 'list'] as const,
  list: (filters: any) => [...eventKeys.lists(), filters] as const,
  details: () => [...eventKeys.all, 'detail'] as const,
  detail: (id: string) => [...eventKeys.details(), id] as const,
  search: (searchTerm: string) => [...eventKeys.all, 'search', searchTerm] as const,
  featured: (userId?: string, lat?: number, lng?: number) => [...eventKeys.all, 'featured', { userId, lat, lng }] as const,
};

/**
 * useEvents Hook
 *
 * Fetches a list of events with optional filters
 *
 * Features:
 * - Automatic caching with 5-minute stale time
 * - Refetch on window focus
 * - Proper error handling with ApiError types
 * - Query key includes filters for granular cache control
 *
 * @param filters - Optional filters for events (location, date, category, etc.)
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data, isLoading, error } = useEvents({
 *   metroArea: 'colombo',
 *   startDate: '2024-01-01',
 *   category: 'cultural'
 * });
 * ```
 */
export function useEvents(
  filters?: GetEventsRequest,
  options?: Omit<UseQueryOptions<EventDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: eventKeys.list(filters || {}),
    queryFn: async () => {
      const result = await eventsRepository.getEvents(filters);
      return result;
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true,
    retry: 1, // Only retry once
    ...options,
  });
}

/**
 * useEventById Hook
 *
 * Fetches a single event by ID
 *
 * Features:
 * - Caches individual event details
 * - Longer stale time (10 minutes) for detail pages
 * - Automatic refetch on window focus
 * - Enabled only when ID is provided
 *
 * @param id - Event ID (GUID)
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data: event, isLoading } = useEventById(eventId);
 * ```
 */
export function useEventById(
  id: string | undefined,
  options?: Omit<UseQueryOptions<EventDto, ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: eventKeys.detail(id || ''),
    queryFn: () => eventsRepository.getEventById(id!),
    enabled: !!id, // Only fetch when ID is provided
    staleTime: 10 * 60 * 1000, // 10 minutes
    refetchOnWindowFocus: true,
    ...options,
  });
}

/**
 * useSearchEvents Hook
 *
 * Searches events by search term
 *
 * Features:
 * - Debounced search (implement debouncing in component)
 * - Separate cache for search results
 * - 2-minute stale time for search results
 * - Only enabled when search term is provided
 *
 * @param searchTerm - Search query string
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const debouncedSearch = useDebounce(searchTerm, 500);
 * const { data: results } = useSearchEvents(debouncedSearch);
 * ```
 */
export function useSearchEvents(
  searchTerm: string | undefined,
  options?: Omit<UseQueryOptions<any, ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: eventKeys.search(searchTerm || ''),
    queryFn: () => eventsRepository.searchEvents({
      searchTerm: searchTerm!,
      page: 1,
      pageSize: 20
    }),
    enabled: !!searchTerm && searchTerm.length >= 2, // Only search with 2+ characters
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchOnWindowFocus: false, // Don't refetch searches on focus
    ...options,
  });
}

/**
 * useFeaturedEvents Hook
 *
 * Fetches featured events for the landing page
 *
 * Features:
 * - Returns up to 4 events sorted by location relevance
 * - For authenticated users: Uses preferred metro areas
 * - For anonymous users: Uses provided coordinates or default location
 * - 5-minute stale time for landing page performance
 * - Automatic refetch on window focus
 *
 * @param userId - Optional authenticated user ID
 * @param latitude - Optional latitude for anonymous users
 * @param longitude - Optional longitude for anonymous users
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * // Authenticated user
 * const { data: events } = useFeaturedEvents(user?.userId);
 *
 * // Anonymous user with location
 * const { data: events } = useFeaturedEvents(undefined, 34.0522, -118.2437);
 * ```
 */
export function useFeaturedEvents(
  userId?: string,
  latitude?: number,
  longitude?: number,
  options?: Omit<UseQueryOptions<EventDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: eventKeys.featured(userId, latitude, longitude),
    queryFn: () => eventsRepository.getFeaturedEvents(userId, latitude, longitude),
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true,
    retry: 1,
    ...options,
  });
}

/**
 * useCreateEvent Hook
 *
 * Mutation hook for creating a new event
 *
 * Features:
 * - Optimistic updates (optional)
 * - Automatic cache invalidation after success
 * - Proper error handling
 * - Success/error callbacks
 *
 * @param options - Additional React Query mutation options
 *
 * @example
 * ```tsx
 * const createEvent = useCreateEvent();
 *
 * await createEvent.mutateAsync({
 *   title: 'New Event',
 *   date: '2024-12-01',
 *   location: 'Colombo'
 * });
 * ```
 */
export function useCreateEvent() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateEventRequest) => eventsRepository.createEvent(data),
    onSuccess: () => {
      // Invalidate all event lists to refetch with new event
      queryClient.invalidateQueries({ queryKey: eventKeys.lists() });
    },
  });
}

/**
 * useUpdateEvent Hook
 *
 * Mutation hook for updating an existing event
 *
 * Features:
 * - Optimistic updates
 * - Automatic cache invalidation
 * - Rollback on error
 *
 * @param options - Additional React Query mutation options
 *
 * @example
 * ```tsx
 * const updateEvent = useUpdateEvent();
 *
 * await updateEvent.mutateAsync({
 *   id: 'event-123',
 *   title: 'Updated Title'
 * });
 * ```
 */
export function useUpdateEvent() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, ...data }: { id: string } & UpdateEventRequest) =>
      eventsRepository.updateEvent(id, data),
    onMutate: async ({ id, ...newData }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: eventKeys.detail(id) });

      // Snapshot previous value for rollback
      const previousEvent = queryClient.getQueryData(eventKeys.detail(id));

      // Optimistically update
      queryClient.setQueryData(eventKeys.detail(id), (old: EventDto | undefined) => {
        if (!old) return old;
        return {
          ...old,
          ...newData,
        };
      });

      return { previousEvent };
    },
    onError: (err, { id }, context) => {
      // Rollback on error
      if (context?.previousEvent) {
        queryClient.setQueryData(eventKeys.detail(id), context.previousEvent);
      }
    },
    onSuccess: (_data, variables) => {
      // Invalidate affected queries
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: eventKeys.lists() });
    },
  });
}

/**
 * useDeleteEvent Hook
 *
 * Mutation hook for deleting an event
 *
 * Features:
 * - Immediate cache removal
 * - Automatic list invalidation
 * - Rollback on error
 *
 * @param options - Additional React Query mutation options
 *
 * @example
 * ```tsx
 * const deleteEvent = useDeleteEvent();
 *
 * await deleteEvent.mutateAsync('event-123');
 * ```
 */
export function useDeleteEvent() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => eventsRepository.deleteEvent(id),
    onMutate: async (id) => {
      // Cancel queries
      await queryClient.cancelQueries({ queryKey: eventKeys.detail(id) });

      // Snapshot for rollback
      const previousEvent = queryClient.getQueryData(eventKeys.detail(id));

      // Remove from cache immediately
      queryClient.removeQueries({ queryKey: eventKeys.detail(id) });

      return { previousEvent };
    },
    onError: (err, id, context) => {
      // Restore on error
      if (context?.previousEvent) {
        queryClient.setQueryData(eventKeys.detail(id), context.previousEvent);
      }
    },
    onSuccess: () => {
      // Invalidate lists
      queryClient.invalidateQueries({ queryKey: eventKeys.lists() });
    },
  });
}

/**
 * useRsvpToEvent Hook
 *
 * Mutation hook for RSVPing to an event
 * Session 23: Returns Stripe checkout URL for paid events, null for free events
 *
 * Features:
 * - Optimistic RSVP count update
 * - Automatic event detail refetch
 * - Rollback on error
 * - Returns checkout URL for payment redirect
 *
 * @param options - Additional React Query mutation options
 *
 * @example
 * ```tsx
 * const rsvpToEvent = useRsvpToEvent();
 *
 * const checkoutUrl = await rsvpToEvent.mutateAsync({
 *   eventId: 'event-123',
 *   userId: 'user-456',
 *   quantity: 1,
 *   successUrl: 'https://app.com/events/payment/success',
 *   cancelUrl: 'https://app.com/events/payment/cancel'
 * });
 *
 * if (checkoutUrl) {
 *   window.location.href = checkoutUrl; // Redirect to Stripe
 * }
 * ```
 */
export function useRsvpToEvent() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: { eventId: string; userId: string; quantity?: number; attendees?: any[]; email?: string; phoneNumber?: string; address?: string; successUrl?: string; cancelUrl?: string }) =>
      eventsRepository.rsvpToEvent(data.eventId, data.userId, data.quantity),
    onMutate: async ({ eventId }) => {
      // Cancel queries
      await queryClient.cancelQueries({ queryKey: eventKeys.detail(eventId) });

      // Snapshot
      const previousEvent = queryClient.getQueryData(eventKeys.detail(eventId));

      // Optimistically update RSVP count
      queryClient.setQueryData(eventKeys.detail(eventId), (old: EventDto | undefined) => {
        if (!old) return old;

        return {
          ...old,
          currentRegistrations: old.currentRegistrations + 1,
        };
      });

      return { previousEvent };
    },
    onError: (err, { eventId }, context) => {
      // Rollback
      if (context?.previousEvent) {
        queryClient.setQueryData(eventKeys.detail(eventId), context.previousEvent);
      }
    },
    onSuccess: (_data, variables) => {
      // Refetch to get accurate data from server
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(variables.eventId) });
    },
  });
}

/**
 * usePrefetchEvent Hook
 *
 * Utility hook to prefetch an event for better UX
 * Useful for hover states or predictive loading
 *
 * @example
 * ```tsx
 * const prefetchEvent = usePrefetchEvent();
 *
 * <Link
 *   onMouseEnter={() => prefetchEvent(eventId)}
 * >
 *   Event Details
 * </Link>
 * ```
 */
export function usePrefetchEvent() {
  const queryClient = useQueryClient();

  return (id: string) => {
    queryClient.prefetchQuery({
      queryKey: eventKeys.detail(id),
      queryFn: () => eventsRepository.getEventById(id),
      staleTime: 10 * 60 * 1000,
    });
  };
}

/**
 * useInvalidateEvents Hook
 *
 * Utility hook to manually invalidate event queries
 * Useful for force refresh scenarios
 *
 * @example
 * ```tsx
 * const invalidateEvents = useInvalidateEvents();
 *
 * <button onClick={() => invalidateEvents.all()}>
 *   Refresh All Events
 * </button>
 * ```
 */
export function useInvalidateEvents() {
  const queryClient = useQueryClient();

  return {
    all: () => queryClient.invalidateQueries({ queryKey: eventKeys.all }),
    lists: () => queryClient.invalidateQueries({ queryKey: eventKeys.lists() }),
    detail: (id: string) => queryClient.invalidateQueries({ queryKey: eventKeys.detail(id) }),
  };
}

/**
 * useUserRsvps Hook
 *
 * Fetches all RSVPs for the current authenticated user
 * Epic 1: Backend returns EventDto[] instead of RsvpDto[] for better UX
 *
 * Features:
 * - Automatic caching with 5-minute stale time
 * - Refetch on window focus
 * - Only enabled when user is authenticated
 *
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data: userRsvps } = useUserRsvps();
 * ```
 */
export function useUserRsvps(
  options?: Omit<UseQueryOptions<EventDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: ['user-rsvps'],
    queryFn: () => eventsRepository.getUserRsvps(),
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true,
    retry: 1,
    ...options,
  });
}

/**
 * useUserRsvpForEvent Hook
 *
 * Checks if the current user has an RSVP for a specific event
 * Uses client-side filtering of all user RSVPs
 *
 * Features:
 * - Leverages cached user RSVPs for performance
 * - Returns event if user is registered, undefined otherwise
 * - Only enabled when eventId is provided
 *
 * @param eventId - Event ID to check RSVP for
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data: userRsvp, isLoading } = useUserRsvpForEvent(eventId);
 * const isRegistered = !!userRsvp;
 * ```
 */
export function useUserRsvpForEvent(
  eventId: string | undefined,
  options?: Omit<UseQueryOptions<EventDto[], ApiError, EventDto | undefined>, 'queryKey' | 'queryFn' | 'select'>
) {
  return useQuery<EventDto[], ApiError, EventDto | undefined>({
    queryKey: ['user-rsvps'],
    queryFn: () => eventsRepository.getUserRsvps(),
    select: (events) => events.find(event => event.id === eventId),
    enabled: !!eventId,
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true,
    retry: 1,
    ...options,
  });
}

/**
 * useUserRegistrationDetails Hook
 *
 * Fetches full registration details for a specific event including attendee names and ages
 * Fix 1: Enhanced registration status detection
 *
 * Features:
 * - Returns full registration with attendee details (names, ages)
 * - Includes contact information and payment status
 * - Returns null if user is not registered
 * - Automatic caching with 5-minute stale time
 * - Only enabled when eventId is provided
 *
 * @param eventId - Event ID to get registration details for
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data: registration, isLoading } = useUserRegistrationDetails(eventId);
 * if (registration) {
 *   console.log('Attendees:', registration.attendees);
 * }
 * ```
 */
export function useUserRegistrationDetails(
  eventId: string | undefined,
  isUserRegistered: boolean = false,
  options?: Omit<UseQueryOptions<RegistrationDetailsDto | null, ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: ['user-registration', eventId],
    queryFn: async () => {
      console.log('[useUserRegistrationDetails] Fetching registration details for event:', eventId);
      try {
        const result = await eventsRepository.getUserRegistrationForEvent(eventId!);
        console.log('[useUserRegistrationDetails] Success:', result);
        console.log('[useUserRegistrationDetails] Attendees:', result?.value?.attendees);
        console.log('[useUserRegistrationDetails] Attendees count:', result?.value?.attendees?.length);
        console.log('[useUserRegistrationDetails] Full value:', JSON.stringify(result?.value, null, 2));
        return result;
      } catch (error) {
        console.error('[useUserRegistrationDetails] Error:', error);
        throw error;
      }
    },
    enabled: !!eventId && isUserRegistered, // Only fetch if user is registered
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true,
    retry: false, // Don't retry on 401/404
    ...options,
  });
}

/**
 * Export all hooks
 */
export default {
  useEvents,
  useEventById,
  useSearchEvents,
  useFeaturedEvents,
  useCreateEvent,
  useUpdateEvent,
  useDeleteEvent,
  useRsvpToEvent,
  usePrefetchEvent,
  useInvalidateEvents,
  useUserRsvps,
  useUserRsvpForEvent,
  useUserRegistrationDetails,
};
