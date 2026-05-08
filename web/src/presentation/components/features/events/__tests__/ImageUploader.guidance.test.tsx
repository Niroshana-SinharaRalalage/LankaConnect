import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { ImageUploader } from '../ImageUploader';

// Mock react-dropzone so we don't need a real DOM dropzone
vi.mock('react-dropzone', () => ({
  useDropzone: () => ({
    getRootProps: () => ({}),
    getInputProps: () => ({}),
    isDragActive: false,
  }),
}));

// Mock the upload hook — we only need its interface, not network behaviour
vi.mock('@/presentation/hooks/useImageUpload', () => ({
  useImageUpload: () => ({
    uploadImage: vi.fn(),
    deleteImage: vi.fn(),
    reorderImages: vi.fn(),
    setPrimaryImage: vi.fn(),
    validateImages: () => ({ isValid: true, errors: [] }),
    isUploading: false,
    isReordering: false,
    isSettingPrimary: false,
    error: null,
    reset: vi.fn(),
  }),
}));

describe('ImageUploader — banner-image guidance (Phase 8YB.1)', () => {
  it('shows recommended dimensions / aspect ratio for the hero banner image', () => {
    render(<ImageUploader eventId="evt-1" existingImages={[]} maxImages={10} />);
    expect(
      screen.getByText(
        /banner.*3:1.*2400.*800|2400.*800.*3:1|recommended.*3:1.*landscape/i,
      ),
    ).toBeInTheDocument();
  });

  it('explains the letterbox fallback so organizers know off-ratio images stay visible', () => {
    render(<ImageUploader eventId="evt-1" existingImages={[]} maxImages={10} />);
    expect(
      screen.getByText(/full image visible|letterbox|won.?t be cropped/i),
    ).toBeInTheDocument();
  });

  it('does not show the guidance when the gallery is full (canUploadMore=false)', () => {
    const fullGallery = Array.from({ length: 10 }).map((_, i) => ({
      id: `img-${i}`,
      imageUrl: `https://cdn/test/${i}.jpg`,
      displayOrder: i + 1,
      isPrimary: i === 0,
    }));
    render(
      <ImageUploader eventId="evt-1" existingImages={fullGallery} maxImages={10} />,
    );
    expect(screen.queryByText(/recommended.*3:1/i)).not.toBeInTheDocument();
  });
});
