import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { ProfilePhotoSection } from '@/presentation/components/features/profile/ProfilePhotoSection';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';

// Mock stores
vi.mock('@/presentation/store/useAuthStore');
vi.mock('@/presentation/store/useProfileStore');

describe('ProfilePhotoSection', () => {
  const mockUserId = 'test-user-123';
  const mockUploadPhoto = vi.fn();
  const mockDeletePhoto = vi.fn();
  const mockMarkSectionDirty = vi.fn();
  const mockMarkSectionClean = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();

    // Mock useAuthStore
    (useAuthStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      user: { userId: mockUserId },
      isAuthenticated: true,
    });

    // Mock useProfileStore
    (useProfileStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      profile: null,
      error: null,
      sectionStates: {
        photo: 'idle',
      },
      uploadPhoto: mockUploadPhoto,
      deletePhoto: mockDeletePhoto,
      markSectionDirty: mockMarkSectionDirty,
      markSectionClean: mockMarkSectionClean,
    });
  });

  describe('rendering', () => {
    it('should render section title and description', () => {
      render(<ProfilePhotoSection />);

      expect(screen.getByText(/profile photo/i)).toBeInTheDocument();
      expect(screen.getByText(/upload a photo to personalize/i)).toBeInTheDocument();
    });

    it('should render PhotoUploadWidget', () => {
      render(<ProfilePhotoSection />);

      expect(screen.getByText(/upload photo/i)).toBeInTheDocument();
    });

    it('should show current photo when profile has photo URL', () => {
      (useProfileStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
        profile: {
          id: mockUserId,
          profilePhotoUrl: 'https://example.com/photo.jpg',
        },
        error: null,
        sectionStates: { photo: 'idle' },
        uploadPhoto: mockUploadPhoto,
        deletePhoto: mockDeletePhoto,
        markSectionDirty: mockMarkSectionDirty,
        markSectionClean: mockMarkSectionClean,
      });

      render(<ProfilePhotoSection />);

      const image = screen.getByAltText(/current profile photo/i);
      expect(image).toBeInTheDocument();
    });

    it('should not render when user is not authenticated', () => {
      (useAuthStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
        user: null,
        isAuthenticated: false,
      });

      const { container } = render(<ProfilePhotoSection />);
      expect(container.firstChild).toBeNull();
    });
  });

  describe('photo upload', () => {
    it('should call uploadPhoto when file is selected', async () => {
      render(<ProfilePhotoSection />);

      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
      const file = new File(['dummy'], 'test.jpg', { type: 'image/jpeg' });

      fireEvent.change(fileInput, { target: { files: [file] } });

      await waitFor(() => {
        expect(mockUploadPhoto).toHaveBeenCalledWith(mockUserId, file);
      });
    });

    it('should show loading state while uploading', () => {
      (useProfileStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
        profile: null,
        error: null,
        sectionStates: { photo: 'saving' },
        uploadPhoto: mockUploadPhoto,
        deletePhoto: mockDeletePhoto,
        markSectionDirty: mockMarkSectionDirty,
        markSectionClean: mockMarkSectionClean,
      });

      render(<ProfilePhotoSection />);

      expect(screen.getByText(/uploading/i)).toBeInTheDocument();
      expect(screen.getByRole('status')).toBeInTheDocument();
    });

    it('should handle upload errors gracefully', () => {
      (useProfileStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
        profile: null,
        error: 'Failed to upload photo',
        sectionStates: { photo: 'error' },
        uploadPhoto: mockUploadPhoto,
        deletePhoto: mockDeletePhoto,
        markSectionDirty: mockMarkSectionDirty,
        markSectionClean: mockMarkSectionClean,
      });

      render(<ProfilePhotoSection />);

      expect(screen.getByText(/failed to upload photo/i)).toBeInTheDocument();
    });
  });

  describe('photo deletion', () => {
    it('should call deletePhoto when delete button is clicked', async () => {
      (useProfileStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
        profile: {
          id: mockUserId,
          profilePhotoUrl: 'https://example.com/photo.jpg',
        },
        error: null,
        sectionStates: { photo: 'idle' },
        uploadPhoto: mockUploadPhoto,
        deletePhoto: mockDeletePhoto,
        markSectionDirty: mockMarkSectionDirty,
        markSectionClean: mockMarkSectionClean,
      });

      render(<ProfilePhotoSection />);

      const deleteButton = screen.getByRole('button', { name: /delete photo/i });
      fireEvent.click(deleteButton);

      await waitFor(() => {
        expect(mockDeletePhoto).toHaveBeenCalledWith(mockUserId);
      });
    });

    it('should show loading state while deleting', () => {
      (useProfileStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
        profile: {
          id: mockUserId,
          profilePhotoUrl: 'https://example.com/photo.jpg',
        },
        error: null,
        sectionStates: { photo: 'saving' },
        uploadPhoto: mockUploadPhoto,
        deletePhoto: mockDeletePhoto,
        markSectionDirty: mockMarkSectionDirty,
        markSectionClean: mockMarkSectionClean,
      });

      render(<ProfilePhotoSection />);

      const deleteButton = screen.getByRole('button', { name: /delete photo/i });
      expect(deleteButton).toBeDisabled();
    });

    it('should handle delete errors gracefully', () => {
      (useProfileStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
        profile: {
          id: mockUserId,
          profilePhotoUrl: 'https://example.com/photo.jpg',
        },
        error: 'Failed to delete photo',
        sectionStates: { photo: 'error' },
        uploadPhoto: mockUploadPhoto,
        deletePhoto: mockDeletePhoto,
        markSectionDirty: mockMarkSectionDirty,
        markSectionClean: mockMarkSectionClean,
      });

      render(<ProfilePhotoSection />);

      expect(screen.getByText(/failed to delete photo/i)).toBeInTheDocument();
    });
  });

  describe('section states', () => {
    it('should show success state after upload', () => {
      (useProfileStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
        profile: {
          id: mockUserId,
          profilePhotoUrl: 'https://example.com/photo.jpg',
        },
        error: null,
        sectionStates: { photo: 'success' },
        uploadPhoto: mockUploadPhoto,
        deletePhoto: mockDeletePhoto,
        markSectionDirty: mockMarkSectionDirty,
        markSectionClean: mockMarkSectionClean,
      });

      render(<ProfilePhotoSection />);

      // Success state is handled by PhotoUploadWidget showing the uploaded photo
      const image = screen.getByAltText(/current profile photo/i);
      expect(image).toBeInTheDocument();
    });

    it('should show error state with error message', () => {
      (useProfileStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
        profile: null,
        error: 'Network error occurred',
        sectionStates: { photo: 'error' },
        uploadPhoto: mockUploadPhoto,
        deletePhoto: mockDeletePhoto,
        markSectionDirty: mockMarkSectionDirty,
        markSectionClean: mockMarkSectionClean,
      });

      render(<ProfilePhotoSection />);

      expect(screen.getByText(/network error occurred/i)).toBeInTheDocument();
    });
  });

  describe('integration with stores', () => {
    it('should use authenticated user ID', () => {
      const customUserId = 'custom-user-456';
      (useAuthStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
        user: { userId: customUserId },
        isAuthenticated: true,
      });

      render(<ProfilePhotoSection />);

      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
      const file = new File(['dummy'], 'test.jpg', { type: 'image/jpeg' });

      fireEvent.change(fileInput, { target: { files: [file] } });

      waitFor(() => {
        expect(mockUploadPhoto).toHaveBeenCalledWith(customUserId, file);
      });
    });

    it('should pass current photo URL from profile store', () => {
      const photoUrl = 'https://example.com/my-photo.jpg';
      (useProfileStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
        profile: {
          id: mockUserId,
          profilePhotoUrl: photoUrl,
        },
        error: null,
        sectionStates: { photo: 'idle' },
        uploadPhoto: mockUploadPhoto,
        deletePhoto: mockDeletePhoto,
        markSectionDirty: mockMarkSectionDirty,
        markSectionClean: mockMarkSectionClean,
      });

      render(<ProfilePhotoSection />);

      const image = screen.getByAltText(/current profile photo/i);
      expect(image).toHaveAttribute('src', expect.stringContaining('my-photo.jpg'));
    });

    it('should handle null profile gracefully', () => {
      (useProfileStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
        profile: null,
        error: null,
        sectionStates: { photo: 'idle' },
        uploadPhoto: mockUploadPhoto,
        deletePhoto: mockDeletePhoto,
        markSectionDirty: mockMarkSectionDirty,
        markSectionClean: mockMarkSectionClean,
      });

      render(<ProfilePhotoSection />);

      // Should show upload area when no profile
      expect(screen.getByText(/upload photo/i)).toBeInTheDocument();
      expect(screen.queryByAltText(/current profile photo/i)).not.toBeInTheDocument();
    });
  });

  describe('accessibility', () => {
    it('should have proper section structure', () => {
      render(<ProfilePhotoSection />);

      const section = screen.getByRole('region');
      expect(section).toBeInTheDocument();
    });

    it('should have descriptive heading', () => {
      render(<ProfilePhotoSection />);

      const heading = screen.getByRole('heading', { name: /profile photo/i });
      expect(heading).toBeInTheDocument();
    });
  });

  describe('UI/UX', () => {
    it('should use Card component for consistent styling', () => {
      const { container } = render(<ProfilePhotoSection />);

      // Card adds specific classes
      const card = container.querySelector('.rounded-lg.border');
      expect(card).toBeInTheDocument();
    });

    it('should display helpful description text', () => {
      render(<ProfilePhotoSection />);

      expect(screen.getByText(/upload a photo to personalize/i)).toBeInTheDocument();
    });
  });
});
