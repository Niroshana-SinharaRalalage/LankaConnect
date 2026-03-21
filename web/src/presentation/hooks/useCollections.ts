'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type {
  EventCollectionsResponse,
  CollectionSummaryDto,
  CollectionDto,
  PublicCollectionSummaryDto,
  CreateCollectionRequest,
} from '@/infrastructure/api/types/events.types';

export const collectionKeys = {
  all: ['collections'] as const,
  byEvent: (eventId: string) => [...collectionKeys.all, 'event', eventId] as const,
  summary: (eventId: string) => [...collectionKeys.all, 'summary', eventId] as const,
  publicSummary: (eventId: string) => [...collectionKeys.all, 'public-summary', eventId] as const,
  mine: (eventId: string) => [...collectionKeys.all, 'mine', eventId] as const,
};

export function useEventCollections(eventId: string | undefined, enabled = true) {
  return useQuery<EventCollectionsResponse>({
    queryKey: collectionKeys.byEvent(eventId || ''),
    queryFn: () => eventsRepository.getEventCollections(eventId!),
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000,
    refetchOnWindowFocus: true,
  });
}

export function useCollectionSummary(eventId: string | undefined, enabled = true) {
  return useQuery<CollectionSummaryDto>({
    queryKey: collectionKeys.summary(eventId || ''),
    queryFn: () => eventsRepository.getCollectionSummary(eventId!),
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000,
  });
}

/**
 * Fetches public collection summary for an event (anyone can call).
 * Returns null if collections not enabled.
 */
export function usePublicCollectionSummary(eventId: string | undefined, enabled = true) {
  return useQuery<PublicCollectionSummaryDto | null>({
    queryKey: collectionKeys.publicSummary(eventId || ''),
    queryFn: () => eventsRepository.getPublicCollectionSummary(eventId!),
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000,
  });
}

/**
 * Fetches the authenticated user's own collections for an event.
 */
export function useMyCollections(eventId: string | undefined, enabled = true) {
  return useQuery<CollectionDto[]>({
    queryKey: collectionKeys.mine(eventId || ''),
    queryFn: () => eventsRepository.getMyCollections(eventId!),
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000,
    refetchOnWindowFocus: true,
  });
}

export function useCreateCollection() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string; request: CreateCollectionRequest }) =>
      eventsRepository.createCollection(data.eventId, data.request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: collectionKeys.byEvent(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: collectionKeys.summary(variables.eventId) });
    },
  });
}
