'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type {
  EventCollectionsResponse,
  CollectionSummaryDto,
  CreateCollectionRequest,
} from '@/infrastructure/api/types/events.types';

export const collectionKeys = {
  all: ['collections'] as const,
  byEvent: (eventId: string) => [...collectionKeys.all, 'event', eventId] as const,
  summary: (eventId: string) => [...collectionKeys.all, 'summary', eventId] as const,
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
