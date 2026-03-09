/**
 * Photo Album React Query Hooks — Multi-Album System
 *
 * Provides React Query hooks for multi-album API integration.
 * Implements caching, infinite scroll pagination, and proper cache invalidation.
 *
 * Key changes from single-album system:
 * - All hooks now take albumId (not eventId) for album-specific operations
 * - useEventAlbums replaces usePhotoAlbum (returns list of albums)
 * - Removed: close, moderation (approve/reject/pending), settings hooks
 * - Added: sendNotification, deleteAlbum, updateDetails, downloadZip hooks
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
  PaginatedAlbumPhotosResponse,
  CreatePhotoAlbumRequest,
  UpdateAlbumDetailsRequest,
} from '@/infrastructure/api/types/events.types';

/**
 * Query Keys for Photo Albums — Multi-Album Structure
 */
export const albumKeys = {
  all: ['albums'] as const,
  byEvent: (eventId: string) => [...albumKeys.all, 'byEvent', eventId] as const,
  detail: (albumId: string) => [...albumKeys.all, 'detail', albumId] as const,
  photos: (albumId: string) => [...albumKeys.all, 'photos', albumId] as const,
};

// ==================== Query Hooks ====================

/**
 * useEventAlbums — Fetch all albums for an event
 */
export function useEventAlbums(eventId: string) {
  return useQuery({
    queryKey: albumKeys.byEvent(eventId),
    queryFn: () => photoAlbumRepository.getAlbums(eventId),
    enabled: !!eventId,
    staleTime: 5 * 60 * 1000,
    refetchOnWindowFocus: true,
    retry: 1,
  });
}

/**
 * useAlbumPhotos — Fetch photos with cursor-based infinite scroll
 */
export function useAlbumPhotos(eventId: string, albumId: string, pageSize = 20) {
  return useInfiniteQuery<PaginatedAlbumPhotosResponse>({
    queryKey: [...albumKeys.photos(albumId), { pageSize }],
    queryFn: ({ pageParam }) =>
      photoAlbumRepository.getPhotos(eventId, albumId, pageSize, pageParam as string | undefined),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) =>
      lastPage.hasMore ? lastPage.nextCursor ?? undefined : undefined,
    enabled: !!eventId && !!albumId,
    staleTime: 5 * 60 * 1000,
  });
}

// ==================== Mutation Hooks ====================

/**
 * useCreateAlbum — Create a new photo album for an event
 */
export function useCreateAlbum() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      request,
    }: {
      eventId: string;
      request: CreatePhotoAlbumRequest;
    }) => photoAlbumRepository.createAlbum(eventId, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: albumKeys.byEvent(variables.eventId) });
    },
  });
}

/**
 * useUpdateAlbumDetails — Update album name and description
 */
export function useUpdateAlbumDetails() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      albumId,
      request,
    }: {
      eventId: string;
      albumId: string;
      request: UpdateAlbumDetailsRequest;
    }) => photoAlbumRepository.updateAlbumDetails(eventId, albumId, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: albumKeys.byEvent(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: albumKeys.detail(variables.albumId) });
    },
  });
}

/**
 * useDeleteAlbum — Delete a draft album
 */
export function useDeleteAlbum() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      albumId,
    }: {
      eventId: string;
      albumId: string;
    }) => photoAlbumRepository.deleteAlbum(eventId, albumId),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: albumKeys.byEvent(variables.eventId) });
    },
  });
}

/**
 * usePublishAlbum — Publish an album (make it visible to attendees)
 */
export function usePublishAlbum() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      albumId,
    }: {
      eventId: string;
      albumId: string;
    }) => photoAlbumRepository.publishAlbum(eventId, albumId),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: albumKeys.byEvent(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: albumKeys.detail(variables.albumId) });
    },
  });
}

/**
 * useSendAlbumNotification — Send email notification for a published album
 */
export function useSendAlbumNotification() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      albumId,
    }: {
      eventId: string;
      albumId: string;
    }) => photoAlbumRepository.sendNotification(eventId, albumId),
  });
}

/**
 * useUploadAlbumPhoto — Upload a photo to an album
 */
export function useUploadAlbumPhoto() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      albumId,
      file,
      caption,
    }: {
      eventId: string;
      albumId: string;
      file: File;
      caption?: string;
    }) => photoAlbumRepository.uploadPhoto(eventId, albumId, file, caption),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: albumKeys.photos(variables.albumId) });
      queryClient.invalidateQueries({ queryKey: albumKeys.byEvent(variables.eventId) });
    },
  });
}

/**
 * useDeleteAlbumPhoto — Delete a photo from an album
 */
export function useDeleteAlbumPhoto() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      albumId,
      photoId,
    }: {
      eventId: string;
      albumId: string;
      photoId: string;
    }) => photoAlbumRepository.deletePhoto(eventId, albumId, photoId),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: albumKeys.photos(variables.albumId) });
      queryClient.invalidateQueries({ queryKey: albumKeys.byEvent(variables.eventId) });
    },
  });
}

/**
 * useBulkDeleteAlbumPhotos — Delete multiple photos from an album (organizer only)
 */
export function useBulkDeleteAlbumPhotos() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      albumId,
      photoIds,
    }: {
      eventId: string;
      albumId: string;
      photoIds: string[];
    }) => photoAlbumRepository.bulkDeletePhotos(eventId, albumId, photoIds),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: albumKeys.photos(variables.albumId) });
      queryClient.invalidateQueries({ queryKey: albumKeys.byEvent(variables.eventId) });
    },
  });
}

/**
 * useSetCoverPhoto — Set a photo as the album cover
 */
export function useSetCoverPhoto() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      albumId,
      photoId,
    }: {
      eventId: string;
      albumId: string;
      photoId: string;
    }) => photoAlbumRepository.setCoverPhoto(eventId, albumId, photoId),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: albumKeys.byEvent(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: albumKeys.detail(variables.albumId) });
    },
  });
}

/**
 * useDownloadAlbumZip — Download all album photos as ZIP
 */
export function useDownloadAlbumZip() {
  return useMutation({
    mutationFn: ({
      eventId,
      albumId,
      albumName,
    }: {
      eventId: string;
      albumId: string;
      albumName: string;
    }) => photoAlbumRepository.downloadZip(eventId, albumId).then((blob) => {
      // Trigger browser download
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${albumName.replace(/[^a-zA-Z0-9_-]/g, '_')}_photos.zip`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    }),
  });
}
