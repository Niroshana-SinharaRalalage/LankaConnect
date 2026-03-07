/**
 * Photo Album React Query Hooks
 *
 * Provides React Query hooks for Photo Album API integration
 * Implements caching, infinite scroll pagination, and proper cache invalidation
 *
 * @requires @tanstack/react-query
 * @requires photoAlbumRepository from infrastructure/repositories/photoAlbum.repository
 * @requires Photo Album types from infrastructure/api/types/events.types
 */

import {
  useQuery,
  useMutation,
  useInfiniteQuery,
  useQueryClient,
} from '@tanstack/react-query';

import { photoAlbumRepository } from '@/infrastructure/api/repositories/photoAlbum.repository';
import type {
  PhotoAlbumDto,
  AlbumPhotoDto,
  PaginatedAlbumPhotosResponse,
  CreatePhotoAlbumRequest,
  UpdateAlbumSettingsRequest,
} from '@/infrastructure/api/types/events.types';

/**
 * Query Keys for Photo Albums
 * Centralized query key management for cache invalidation
 */
export const albumKeys = {
  all: ['albums'] as const,
  detail: (eventId: string) => [...albumKeys.all, 'detail', eventId] as const,
  photos: (eventId: string) => [...albumKeys.all, 'photos', eventId] as const,
  pending: (eventId: string) => [...albumKeys.all, 'pending', eventId] as const,
};

// ==================== Query Hooks ====================

/**
 * usePhotoAlbum Hook
 *
 * Fetches album metadata for a specific event
 *
 * Features:
 * - Returns null if event has no album (404 handled gracefully)
 * - Automatic caching with 5-minute stale time
 * - Refetch on window focus
 * - Only enabled when eventId is provided
 *
 * @param eventId - Event ID to fetch album for
 *
 * @example
 * ```tsx
 * const { data: album, isLoading } = usePhotoAlbum(eventId);
 * if (album) {
 *   console.log('Album status:', album.status);
 *   console.log('Photo count:', album.photoCount);
 * }
 * ```
 */
export function usePhotoAlbum(eventId: string) {
  return useQuery({
    queryKey: albumKeys.detail(eventId),
    queryFn: () => photoAlbumRepository.getAlbum(eventId),
    enabled: !!eventId,
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true,
    retry: 1,
  });
}

/**
 * useAlbumPhotos Hook
 *
 * Fetches album photos with cursor-based infinite scroll pagination
 *
 * Features:
 * - Infinite scroll with cursor-based pagination
 * - Configurable page size
 * - Automatic caching with 5-minute stale time
 * - Only enabled when eventId is provided
 *
 * @param eventId - Event ID to fetch photos for
 * @param pageSize - Number of photos per page (default: 20)
 *
 * @example
 * ```tsx
 * const {
 *   data,
 *   fetchNextPage,
 *   hasNextPage,
 *   isFetchingNextPage,
 * } = useAlbumPhotos(eventId);
 *
 * const allPhotos = data?.pages.flatMap(page => page.photos) ?? [];
 * ```
 */
export function useAlbumPhotos(eventId: string, pageSize = 20) {
  return useInfiniteQuery<PaginatedAlbumPhotosResponse>({
    queryKey: [...albumKeys.photos(eventId), { pageSize }],
    queryFn: ({ pageParam }) =>
      photoAlbumRepository.getPhotos(eventId, pageSize, pageParam as string | undefined),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) =>
      lastPage.hasMore ? lastPage.nextCursor ?? undefined : undefined,
    enabled: !!eventId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * usePendingPhotos Hook
 *
 * Fetches photos awaiting moderation approval
 *
 * Features:
 * - Automatic caching with 2-minute stale time
 * - Refetch on window focus for real-time moderation
 * - Only enabled when eventId is provided
 *
 * @param eventId - Event ID to fetch pending photos for
 *
 * @example
 * ```tsx
 * const { data: pendingPhotos } = usePendingPhotos(eventId);
 * console.log('Photos awaiting review:', pendingPhotos?.length);
 * ```
 */
export function usePendingPhotos(eventId: string) {
  return useQuery({
    queryKey: albumKeys.pending(eventId),
    queryFn: () => photoAlbumRepository.getPendingPhotos(eventId),
    enabled: !!eventId,
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchOnWindowFocus: true,
  });
}

// ==================== Mutation Hooks ====================

/**
 * useCreateAlbum Hook
 *
 * Mutation hook for creating a new photo album for an event
 *
 * Features:
 * - Automatic cache invalidation on success
 * - Proper error handling
 *
 * @example
 * ```tsx
 * const createAlbum = useCreateAlbum();
 *
 * await createAlbum.mutateAsync({
 *   eventId: 'event-123',
 *   request: { description: 'Event photos' },
 * });
 * ```
 */
export function useCreateAlbum() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      request,
    }: {
      eventId: string;
      request?: CreatePhotoAlbumRequest;
    }) => photoAlbumRepository.createAlbum(eventId, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: albumKeys.detail(variables.eventId) });
    },
  });
}

/**
 * useUpdateAlbumSettings Hook
 *
 * Mutation hook for updating album settings (upload permission, moderation, description)
 *
 * Features:
 * - Automatic cache invalidation on success
 * - Proper error handling
 *
 * @example
 * ```tsx
 * const updateSettings = useUpdateAlbumSettings();
 *
 * await updateSettings.mutateAsync({
 *   eventId: 'event-123',
 *   request: {
 *     uploadPermission: AlbumUploadPermission.RegisteredAttendees,
 *     moderationMode: AlbumModerationMode.PostModeration,
 *   },
 * });
 * ```
 */
export function useUpdateAlbumSettings() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      request,
    }: {
      eventId: string;
      request: UpdateAlbumSettingsRequest;
    }) => photoAlbumRepository.updateSettings(eventId, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: albumKeys.detail(variables.eventId) });
    },
  });
}

/**
 * usePublishAlbum Hook
 *
 * Mutation hook for publishing an album (making it visible to attendees)
 *
 * Features:
 * - Automatic cache invalidation on success
 * - Proper error handling
 *
 * @example
 * ```tsx
 * const publishAlbum = usePublishAlbum();
 *
 * await publishAlbum.mutateAsync({ eventId: 'event-123' });
 * ```
 */
export function usePublishAlbum() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId }: { eventId: string }) =>
      photoAlbumRepository.publishAlbum(eventId),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: albumKeys.detail(variables.eventId) });
    },
  });
}

/**
 * useCloseAlbum Hook
 *
 * Mutation hook for closing an album (preventing further uploads)
 *
 * Features:
 * - Automatic cache invalidation on success
 * - Proper error handling
 *
 * @example
 * ```tsx
 * const closeAlbum = useCloseAlbum();
 *
 * await closeAlbum.mutateAsync({ eventId: 'event-123' });
 * ```
 */
export function useCloseAlbum() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId }: { eventId: string }) =>
      photoAlbumRepository.closeAlbum(eventId),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: albumKeys.detail(variables.eventId) });
    },
  });
}

/**
 * useUploadAlbumPhoto Hook
 *
 * Mutation hook for uploading a photo to an album
 *
 * Features:
 * - FormData-based file upload
 * - Optional caption support
 * - Invalidates both photos and album detail (photo count) on success
 *
 * @example
 * ```tsx
 * const uploadPhoto = useUploadAlbumPhoto();
 *
 * const handleUpload = async (file: File) => {
 *   await uploadPhoto.mutateAsync({
 *     eventId: 'event-123',
 *     file,
 *     caption: 'Group photo',
 *   });
 * };
 * ```
 */
export function useUploadAlbumPhoto() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      file,
      caption,
    }: {
      eventId: string;
      file: File;
      caption?: string;
    }) => photoAlbumRepository.uploadPhoto(eventId, file, caption),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: albumKeys.photos(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: albumKeys.detail(variables.eventId) });
    },
  });
}

/**
 * useDeleteAlbumPhoto Hook
 *
 * Mutation hook for deleting a photo from an album
 *
 * Features:
 * - Invalidates photos, album detail, and pending photos on success
 *
 * @example
 * ```tsx
 * const deletePhoto = useDeleteAlbumPhoto();
 *
 * await deletePhoto.mutateAsync({
 *   eventId: 'event-123',
 *   photoId: 'photo-456',
 * });
 * ```
 */
export function useDeleteAlbumPhoto() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, photoId }: { eventId: string; photoId: string }) =>
      photoAlbumRepository.deletePhoto(eventId, photoId),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: albumKeys.photos(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: albumKeys.detail(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: albumKeys.pending(variables.eventId) });
    },
  });
}

/**
 * useApprovePhoto Hook
 *
 * Mutation hook for approving a pending photo (moderation action)
 *
 * Features:
 * - Invalidates photos and pending photos on success
 * - Approved photo moves from pending queue to published photos
 *
 * @example
 * ```tsx
 * const approvePhoto = useApprovePhoto();
 *
 * await approvePhoto.mutateAsync({
 *   eventId: 'event-123',
 *   photoId: 'photo-456',
 * });
 * ```
 */
export function useApprovePhoto() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, photoId }: { eventId: string; photoId: string }) =>
      photoAlbumRepository.approvePhoto(eventId, photoId),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: albumKeys.photos(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: albumKeys.pending(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: albumKeys.detail(variables.eventId) });
    },
  });
}

/**
 * useRejectPhoto Hook
 *
 * Mutation hook for rejecting a pending photo (moderation action)
 *
 * Features:
 * - Invalidates photos and pending photos on success
 * - Rejected photo is removed from pending queue
 *
 * @example
 * ```tsx
 * const rejectPhoto = useRejectPhoto();
 *
 * await rejectPhoto.mutateAsync({
 *   eventId: 'event-123',
 *   photoId: 'photo-456',
 * });
 * ```
 */
export function useRejectPhoto() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, photoId }: { eventId: string; photoId: string }) =>
      photoAlbumRepository.rejectPhoto(eventId, photoId),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: albumKeys.photos(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: albumKeys.pending(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: albumKeys.detail(variables.eventId) });
    },
  });
}

/**
 * useSetCoverPhoto Hook
 *
 * Mutation hook for setting a photo as the album cover
 *
 * Features:
 * - Invalidates album detail on success (cover photo URL changes)
 *
 * @example
 * ```tsx
 * const setCover = useSetCoverPhoto();
 *
 * await setCover.mutateAsync({
 *   eventId: 'event-123',
 *   photoId: 'photo-456',
 * });
 * ```
 */
export function useSetCoverPhoto() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, photoId }: { eventId: string; photoId: string }) =>
      photoAlbumRepository.setCoverPhoto(eventId, photoId),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: albumKeys.detail(variables.eventId) });
    },
  });
}
