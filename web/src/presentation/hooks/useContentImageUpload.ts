import { useMutation } from '@tanstack/react-query';
import { apiClient } from '@/infrastructure/api/client/api-client';

/**
 * Phase 6A.106 Part 3: Content Image Upload Hook
 *
 * Uploads images to Azure Blob Storage for use in rich text editors.
 * This is a generic hook for content not tied to specific entities (newsletters, events, etc.)
 *
 * @example
 * ```tsx
 * const { mutateAsync: uploadImage, isPending } = useContentImageUpload();
 *
 * const handleImageUpload = async (file: File) => {
 *   try {
 *     const imageUrl = await uploadImage(file);
 *     editor.chain().focus().setImage({ src: imageUrl }).run();
 *   } catch (error) {
 *     toast.error('Failed to upload image');
 *   }
 * };
 * ```
 */
export function useContentImageUpload() {
  return useMutation({
    mutationFn: async (file: File): Promise<string> => {
      // Validate file size (10MB max - matches backend)
      const MAX_SIZE = 10 * 1024 * 1024; // 10MB
      if (file.size > MAX_SIZE) {
        throw new Error('Image size must be less than 10MB');
      }

      // Validate file type
      const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
      if (!ALLOWED_TYPES.includes(file.type)) {
        throw new Error('Invalid file type. Allowed: JPEG, PNG, GIF, WebP');
      }

      // Create FormData
      const formData = new FormData();
      formData.append('image', file);

      // Upload to backend
      const response = await apiClient.postMultipart<ContentImageUploadResponse>(
        '/content/images',
        formData
      );

      return response.imageUrl;
    },
    onError: (error: Error) => {
      console.error('[useContentImageUpload] Upload failed:', error.message);
    },
  });
}

/**
 * Response from content image upload endpoint
 */
interface ContentImageUploadResponse {
  imageUrl: string;
  sizeBytes: number;
  contentType: string;
}
