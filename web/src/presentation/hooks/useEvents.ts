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
  AttendeeDto,
  EventAttendeesResponse, // Phase 6A.45
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
    mutationFn: (data: { eventId: string; userId: string; quantity?: number; attendees?: any[]; email?: string; phoneNumber?: string; address?: string; successUrl?: string; cancelUrl?: string }) => {
      // Phase 6A.11: Construct full RsvpRequest with all fields (legacy and new format support)
      const rsvpRequest = {
        userId: data.userId,
        quantity: data.quantity ?? 1,
        attendees: data.attendees,
        email: data.email,
        phoneNumber: data.phoneNumber,
        address: data.address,
        successUrl: data.successUrl,
        cancelUrl: data.cancelUrl,
      };
      return eventsRepository.rsvpToEvent(data.eventId, rsvpRequest);
    },
    onMutate: async ({ eventId, quantity, attendees }) => {
      // Cancel queries
      await queryClient.cancelQueries({ queryKey: eventKeys.detail(eventId) });

      // Snapshot
      const previousEvent = queryClient.getQueryData(eventKeys.detail(eventId));

      // Optimistically update RSVP count
      // Session 30: Fixed to use actual attendee count for multi-attendee registrations
      const attendeeCount = attendees?.length || quantity || 1;

      queryClient.setQueryData(eventKeys.detail(eventId), (old: EventDto | undefined) => {
        if (!old) return old;

        return {
          ...old,
          currentRegistrations: old.currentRegistrations + attendeeCount,
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
      // Phase 6A.25 Fix: Invalidate all relevant caches after successful RSVP
      // This ensures the UI updates correctly without needing a page reload

      // Refetch event details to get accurate registration count
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(variables.eventId) });

      // Invalidate user's RSVP list so isUserRegistered updates correctly
      queryClient.invalidateQueries({ queryKey: ['user-rsvps'] });

      // Invalidate registration details for this specific event
      queryClient.invalidateQueries({ queryKey: ['user-registration', variables.eventId] });
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
    // Phase 6A.25 Fix: Allow one retry after 401 to handle token refresh scenario
    // The auth interceptor refreshes the token, but with retry: false the query wouldn't retry
    // This allows the query to retry once after successful token refresh
    retry: (failureCount, error) => {
      // Only retry once for 401 errors (after token refresh)
      if (failureCount < 1 && (error as any)?.response?.status === 401) {
        return true;
      }
      return false;
    },
    retryDelay: 1000, // Wait 1 second for token refresh to complete
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
      console.log('[useUserRegistrationDetails] 📍 Starting fetch for event:', eventId);
      try {
        const result = await eventsRepository.getUserRegistrationForEvent(eventId!);
        console.log('[useUserRegistrationDetails] ✅ Success - Raw result:', result);
        console.log('[useUserRegistrationDetails] Contact Email:', result?.contactEmail);
        console.log('[useUserRegistrationDetails] Contact Phone:', result?.contactPhone);
        console.log('[useUserRegistrationDetails] Contact Address:', result?.contactAddress);
        console.log('[useUserRegistrationDetails] Attendees array exists:', !!result?.attendees);
        console.log('[useUserRegistrationDetails] Attendees count:', result?.attendees?.length);
        if (result?.attendees?.length) {
          console.log('[useUserRegistrationDetails] First attendee:', result.attendees[0]);
        }
        console.log('[useUserRegistrationDetails] Full JSON:', JSON.stringify(result, null, 2));
        return result;
      } catch (error: any) {
        console.error('[useUserRegistrationDetails] ❌ Error:', {
          message: error?.message,
          status: error?.response?.status,
          data: error?.response?.data,
          hasResponse: !!error?.response,
          errorObject: error,
        });
        // Return null on 401/404 instead of throwing
        if (error?.response?.status === 401 || error?.response?.status === 404) {
          console.warn('[useUserRegistrationDetails] ⚠️ No registration found or unauthorized (expected for some users)');
          return null;
        }
        throw error;
      }
    },
    enabled: !!eventId && isUserRegistered, // Only fetch if user is registered
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true,
    // Phase 6A.25 Fix: Allow one retry after 401 to handle token refresh scenario
    // Note: 404 errors are handled in queryFn and return null, so they won't trigger retry
    retry: (failureCount, error) => {
      // Only retry once for 401 errors (after token refresh)
      if (failureCount < 1 && (error as any)?.response?.status === 401) {
        return true;
      }
      return false;
    },
    retryDelay: 1000, // Wait 1 second for token refresh to complete
    ...options,
  });
}

/**
 * useUpdateRegistrationDetails Hook
 *
 * Phase 6A.14: Mutation hook for updating registration details (attendees, contact info)
 *
 * Features:
 * - Updates attendee names and ages
 * - Updates contact information (email, phone, address)
 * - Automatic cache invalidation after success
 * - Proper error handling
 *
 * @example
 * ```tsx
 * const updateRegistration = useUpdateRegistrationDetails();
 *
 * await updateRegistration.mutateAsync({
 *   eventId: 'event-123',
 *   attendees: [{ name: 'John Doe', age: 30 }],
 *   email: 'john@example.com',
 *   phoneNumber: '555-1234'
 * });
 * ```
 */
export function useUpdateRegistrationDetails() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: {
      eventId: string;
      attendees: AttendeeDto[];
      email: string;
      phoneNumber: string;
      address?: string;
    }) => {
      return eventsRepository.updateRegistrationDetails(data.eventId, {
        attendees: data.attendees,
        email: data.email,
        phoneNumber: data.phoneNumber,
        address: data.address,
      });
    },
    onSuccess: (_data, variables) => {
      // Invalidate registration details to refetch with updated data
      queryClient.invalidateQueries({ queryKey: ['user-registration', variables.eventId] });
      // Also invalidate event detail in case registration count changed
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(variables.eventId) });
    },
  });
}

/**
 * useEventAttendees Hook
 *
 * Phase 6A.45: Query hook for fetching event attendee list (organizer only)
 *
 * Features:
 * - Automatic caching with 2-minute stale time
 * - Returns complete registration details with attendee information
 * - Includes summary statistics (total registrations, attendees, revenue)
 * - Proper authorization handling (403 for non-organizers)
 *
 * @param eventId - Event ID to fetch attendees for
 * @param enabled - Whether to enable the query (default: true)
 *
 * @example
 * ```tsx
 * const { data: attendees, isLoading, error } = useEventAttendees('event-123');
 *
 * if (attendees) {
 *   console.log(`Total registrations: ${attendees.totalRegistrations}`);
 *   console.log(`Total attendees: ${attendees.totalAttendees}`);
 *   console.log(`Total revenue: $${attendees.totalRevenue}`);
 * }
 * ```
 */
export function useEventAttendees(
  eventId: string,
  enabled: boolean = true
) {
  return useQuery({
    queryKey: ['event-attendees', eventId] as const,
    queryFn: async () => {
      return await eventsRepository.getEventAttendees(eventId);
    },
    staleTime: 2 * 60 * 1000, // 2 minutes
    enabled: enabled && !!eventId,
  });
}

/**
 * useExportEventAttendees Hook
 *
 * Phase 6A.45: Mutation hook for downloading event attendee data (organizer only)
 *
 * Features:
 * - Supports Excel and CSV export formats
 * - Multi-sheet Excel export includes signup lists
 * - Handles file download with proper MIME types
 * - Automatic error handling for authorization failures
 *
 * @example
 * ```tsx
 * const exportAttendees = useExportEventAttendees();
 *
 * const handleExport = async (format: 'excel' | 'csv') => {
 *   try {
 *     const blob = await exportAttendees.mutateAsync({
 *       eventId: 'event-123',
 *       format
 *     });
 *
 *     // Trigger download
 *     const url = URL.createObjectURL(blob);
 *     const link = document.createElement('a');
 *     link.href = url;
 *     link.download = `event-attendees.${format === 'excel' ? 'xlsx' : 'csv'}`;
 *     link.click();
 *     URL.revokeObjectURL(url);
 *   } catch (error) {
 *     console.error('Export failed:', error);
 *   }
 * };
 * ```
 */
export function useExportEventAttendees() {
  return useMutation({
    mutationFn: async (data: {
      eventId: string;
      format: 'excel' | 'csv';
    }) => {
      return await eventsRepository.exportEventAttendees(data.eventId, data.format);
    },
  });
}

// ==================== Phase 6A.61: Event Notification ====================

/**
 * Hook to send event notification email to all attendees
 */
export function useSendEventNotification() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (eventId: string) => eventsRepository.sendEventNotification(eventId),
    onSuccess: (_data, eventId) => {
      // Invalidate event detail and notification history
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });
      queryClient.invalidateQueries({ queryKey: ['eventNotificationHistory', eventId] });
    },
    onError: (error: any) => {
      console.error('Failed to send event notification:', error);
    }
  });
}

/**
 * Hook to fetch event notification history
 */
export function useEventNotificationHistory(eventId: string) {
  return useQuery({
    queryKey: ['eventNotificationHistory', eventId],
    queryFn: () => eventsRepository.getEventNotificationHistory(eventId),
    enabled: !!eventId,
    refetchInterval: 30000 // Refresh every 30 seconds to show updated statistics
  });
}

// ==================== Phase 6A.76: Event Reminder ====================

/**
 * Phase 6A.76: Hook to send manual event reminder email to all registered attendees
 */
export function useSendEventReminder() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: { eventId: string; reminderType: string }) =>
      eventsRepository.sendEventReminder(data.eventId, data.reminderType),
    onSuccess: (_data, variables) => {
      // Invalidate event detail to refresh any reminder-related state
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(variables.eventId) });
    },
    onError: (error: any) => {
      console.error('Failed to send event reminder:', error);
    }
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
  useUpdateRegistrationDetails,
  useEventAttendees,
  useExportEventAttendees,
  useSendEventNotification,
  useEventNotificationHistory,
  useSendEventReminder,
};
