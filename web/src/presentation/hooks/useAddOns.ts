'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type {
  AddOnDefinitionDto,
  EventAddOnPurchasesResponse,
  AddOnPurchaseSummaryDto,
  CreateAddOnDefinitionRequest,
  UpdateAddOnDefinitionRequest,
  PurchaseAddOnRequest,
} from '@/infrastructure/api/types/events.types';

export const addOnKeys = {
  all: ['addOns'] as const,
  definitions: (eventId: string) => [...addOnKeys.all, 'definitions', eventId] as const,
  purchases: (eventId: string) => [...addOnKeys.all, 'purchases', eventId] as const,
  summary: (eventId: string) => [...addOnKeys.all, 'summary', eventId] as const,
};

export function useAddOnDefinitions(eventId: string | undefined, enabled = true) {
  return useQuery<AddOnDefinitionDto[]>({
    queryKey: addOnKeys.definitions(eventId || ''),
    queryFn: () => eventsRepository.getAddOnDefinitions(eventId!),
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000,
    refetchOnWindowFocus: true,
  });
}

export function useEventAddOnPurchases(eventId: string | undefined, enabled = true) {
  return useQuery<EventAddOnPurchasesResponse>({
    queryKey: addOnKeys.purchases(eventId || ''),
    queryFn: () => eventsRepository.getEventAddOnPurchases(eventId!),
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000,
    refetchOnWindowFocus: true,
  });
}

export function useAddOnPurchaseSummary(eventId: string | undefined, enabled = true) {
  return useQuery<AddOnPurchaseSummaryDto>({
    queryKey: addOnKeys.summary(eventId || ''),
    queryFn: () => eventsRepository.getAddOnPurchaseSummary(eventId!),
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000,
  });
}

export function useCreateAddOnDefinition() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string; request: CreateAddOnDefinitionRequest }) =>
      eventsRepository.createAddOnDefinition(data.eventId, data.request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: addOnKeys.definitions(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: addOnKeys.purchases(variables.eventId) });
    },
  });
}

export function useUpdateAddOnDefinition() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string; definitionId: string; request: UpdateAddOnDefinitionRequest }) =>
      eventsRepository.updateAddOnDefinition(data.eventId, data.definitionId, data.request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: addOnKeys.definitions(variables.eventId) });
    },
  });
}

export function usePurchaseAddOn() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string; definitionId: string; request: PurchaseAddOnRequest }) =>
      eventsRepository.purchaseAddOn(data.eventId, data.definitionId, data.request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: addOnKeys.purchases(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: addOnKeys.definitions(variables.eventId) });
    },
  });
}
