'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type {
  AddOnDefinitionDto,
  AddOnPurchaseDto,
  EventAddOnPurchasesResponse,
  AddOnPurchaseSummaryDto,
  CreateAddOnDefinitionRequest,
  UpdateAddOnDefinitionRequest,
  PurchaseAddOnRequest,
  PurchaseAddOnCartRequest,
} from '@/infrastructure/api/types/events.types';

export const addOnKeys = {
  all: ['addOns'] as const,
  definitions: (eventId: string) => [...addOnKeys.all, 'definitions', eventId] as const,
  purchases: (eventId: string) => [...addOnKeys.all, 'purchases', eventId] as const,
  summary: (eventId: string) => [...addOnKeys.all, 'summary', eventId] as const,
  myPurchases: (eventId: string, email: string) => [...addOnKeys.all, 'myPurchases', eventId, email] as const,
  mine: (eventId: string) => [...addOnKeys.all, 'mine', eventId] as const,
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

/**
 * Phase 6A.143 — upload (or replace) the add-on display image.
 * Invalidates the definitions list so AddOnSelector + manage tab pick up the new URL.
 */
export function useUploadAddOnImage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string; definitionId: string; file: File }) =>
      eventsRepository.uploadAddOnImage(data.eventId, data.definitionId, data.file),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: addOnKeys.definitions(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: addOnKeys.purchases(variables.eventId) });
    },
  });
}

/**
 * Phase 6A.143 — clear the add-on display image. Idempotent.
 */
export function useDeleteAddOnImage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string; definitionId: string }) =>
      eventsRepository.deleteAddOnImage(data.eventId, data.definitionId),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: addOnKeys.definitions(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: addOnKeys.purchases(variables.eventId) });
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

export function useMyAddOnPurchases(eventId: string | undefined, email: string | null, enabled = true) {
  return useQuery<AddOnPurchaseDto[]>({
    queryKey: addOnKeys.myPurchases(eventId || '', email || ''),
    queryFn: () => eventsRepository.getMyAddOnPurchases(eventId!, email!),
    enabled: !!eventId && !!email && enabled,
    staleTime: 30 * 1000, // 30 seconds — refresh more frequently after purchase
    refetchOnWindowFocus: true,
  });
}

/**
 * Fetches the authenticated user's own add-on purchases for an event.
 */
export function useMyAddOnPurchasesMine(eventId: string | undefined, enabled = true) {
  return useQuery<AddOnPurchaseDto[]>({
    queryKey: addOnKeys.mine(eventId || ''),
    queryFn: () => eventsRepository.getMyAddOnPurchasesMine(eventId!),
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000,
    refetchOnWindowFocus: true,
  });
}

export function usePurchaseAddOnCart() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string; request: PurchaseAddOnCartRequest }) =>
      eventsRepository.purchaseAddOnCart(data.eventId, data.request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: addOnKeys.purchases(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: addOnKeys.definitions(variables.eventId) });
    },
  });
}
