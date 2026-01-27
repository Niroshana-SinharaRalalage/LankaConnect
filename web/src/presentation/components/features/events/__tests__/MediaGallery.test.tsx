import { render, screen, fireEvent } from '@testing-library/react';
import { MediaGallery } from '../MediaGallery';
import { describe, it, expect, vi } from 'vitest';
import { EventImageDto, EventVideoDto } from '@/infrastructure/api/types/events.types';

// Mock the Dialog component to make it testable
vi.mock('@/presentation/components/ui/Dialog', () => ({
  Dialog: ({ children, open }: { children: React.ReactNode; open: boolean }) => (
    open ? <div data-testid="dialog">{children}</div> : null
  ),
  DialogContent: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="dialog-content">{children}</div>
  ),
}));

describe('MediaGallery', () => {
  // Test data with non-sequential displayOrder to expose the bug
  const mockImagesUnsorted: EventImageDto[] = [
    { id: 'img-1', imageUrl: 'https://example.com/image3.jpg', displayOrder: 3 },
    { id: 'img-2', imageUrl: 'https://example.com/image1.jpg', displayOrder: 1 },
    { id: 'img-3', imageUrl: 'https://example.com/image2.jpg', displayOrder: 2 },
  ];

  const mockVideosUnsorted: EventVideoDto[] = [
    { id: 'vid-1', videoUrl: 'https://example.com/video3.mp4', thumbnailUrl: 'https://example.com/thumb3.jpg', displayOrder: 3 },
    { id: 'vid-2', videoUrl: 'https://example.com/video1.mp4', thumbnailUrl: 'https://example.com/thumb1.jpg', displayOrder: 1 },
    { id: 'vid-3', videoUrl: 'https://example.com/video2.mp4', thumbnailUrl: 'https://example.com/thumb2.jpg', displayOrder: 2 },
  ];

  describe('Rendering', () => {
    it('should render null when no media is provided', () => {
      const { container } = render(<MediaGallery images={[]} videos={[]} />);
      expect(container.firstChild).toBeNull();
    });

    it('should render images section when images are provided', () => {
      render(<MediaGallery images={mockImagesUnsorted} />);
      expect(screen.getByText('Photos (3)')).toBeInTheDocument();
    });

    it('should render videos section when videos are provided', () => {
      render(<MediaGallery videos={mockVideosUnsorted} />);
      expect(screen.getByText('Videos (3)')).toBeInTheDocument();
    });

    it('should render both sections when both media types are provided', () => {
      render(<MediaGallery images={mockImagesUnsorted} videos={mockVideosUnsorted} />);
      expect(screen.getByText('Photos (3)')).toBeInTheDocument();
      expect(screen.getByText('Videos (3)')).toBeInTheDocument();
    });
  });

  describe('Image Thumbnail Display Order', () => {
    it('should display image thumbnails sorted by displayOrder', () => {
      render(<MediaGallery images={mockImagesUnsorted} />);

      const images = screen.getAllByRole('img');

      // Images should be displayed in displayOrder: 1, 2, 3
      // Which corresponds to: image1.jpg, image2.jpg, image3.jpg
      expect(images[0]).toHaveAttribute('src', 'https://example.com/image1.jpg');
      expect(images[1]).toHaveAttribute('src', 'https://example.com/image2.jpg');
      expect(images[2]).toHaveAttribute('src', 'https://example.com/image3.jpg');
    });

    it('should display displayOrder badge on each thumbnail', () => {
      render(<MediaGallery images={mockImagesUnsorted} />);

      // Display order badges should show 1, 2, 3 in visual order
      expect(screen.getByText('1')).toBeInTheDocument();
      expect(screen.getByText('2')).toBeInTheDocument();
      expect(screen.getByText('3')).toBeInTheDocument();
    });
  });

  describe('SCRUM-21: Lightbox shows correct image when clicked', () => {
    it('should show the correct image in lightbox when first sorted thumbnail is clicked', () => {
      render(<MediaGallery images={mockImagesUnsorted} />);

      // Get all thumbnail buttons
      const thumbnailButtons = screen.getAllByRole('button');

      // Click the first thumbnail (should be displayOrder: 1 = image1.jpg)
      fireEvent.click(thumbnailButtons[0]);

      // Lightbox should open and show the correct image
      // Use getAllByAltText since both thumbnail and lightbox have same alt text
      const images = screen.getAllByAltText(/Event photo 1/i);
      // The lightbox image has class 'max-w-full max-h-full object-contain'
      const lightboxImage = images.find(img => img.classList.contains('object-contain'));
      expect(lightboxImage).toHaveAttribute('src', 'https://example.com/image1.jpg');
    });

    it('should show the correct image in lightbox when second sorted thumbnail is clicked', () => {
      render(<MediaGallery images={mockImagesUnsorted} />);

      const thumbnailButtons = screen.getAllByRole('button');

      // Click the second thumbnail (should be displayOrder: 2 = image2.jpg)
      fireEvent.click(thumbnailButtons[1]);

      const images = screen.getAllByAltText(/Event photo 2/i);
      const lightboxImage = images.find(img => img.classList.contains('object-contain'));
      expect(lightboxImage).toHaveAttribute('src', 'https://example.com/image2.jpg');
    });

    it('should show the correct image in lightbox when third sorted thumbnail is clicked', () => {
      render(<MediaGallery images={mockImagesUnsorted} />);

      const thumbnailButtons = screen.getAllByRole('button');

      // Click the third thumbnail (should be displayOrder: 3 = image3.jpg)
      fireEvent.click(thumbnailButtons[2]);

      const images = screen.getAllByAltText(/Event photo 3/i);
      const lightboxImage = images.find(img => img.classList.contains('object-contain'));
      expect(lightboxImage).toHaveAttribute('src', 'https://example.com/image3.jpg');
    });
  });

  describe('SCRUM-21: Lightbox shows correct video when clicked', () => {
    it('should show the correct video in lightbox when first sorted thumbnail is clicked', () => {
      const { container } = render(<MediaGallery videos={mockVideosUnsorted} />);

      const thumbnailButtons = screen.getAllByRole('button');

      // Click the first video thumbnail (should be displayOrder: 1 = video1.mp4)
      fireEvent.click(thumbnailButtons[0]);

      // Query video element using querySelector since video doesn't have an ARIA role
      const lightboxVideo = container.querySelector('video');
      expect(lightboxVideo).toHaveAttribute('src', 'https://example.com/video1.mp4');
    });

    it('should show the correct video in lightbox when second sorted thumbnail is clicked', () => {
      const { container } = render(<MediaGallery videos={mockVideosUnsorted} />);

      const thumbnailButtons = screen.getAllByRole('button');

      // Click the second video thumbnail (should be displayOrder: 2 = video2.mp4)
      fireEvent.click(thumbnailButtons[1]);

      // Query video element using querySelector since video doesn't have an ARIA role
      const lightboxVideo = container.querySelector('video');
      expect(lightboxVideo).toHaveAttribute('src', 'https://example.com/video2.mp4');
    });
  });

  describe('Lightbox Navigation', () => {
    it('should navigate to next image in sorted order', () => {
      render(<MediaGallery images={mockImagesUnsorted} />);

      // Click first thumbnail to open lightbox
      const thumbnailButtons = screen.getAllByRole('button');
      fireEvent.click(thumbnailButtons[0]);

      // Should show image1.jpg (displayOrder: 1) - find lightbox image by class
      let images = screen.getAllByAltText(/Event photo 1/i);
      let lightboxImage = images.find(img => img.classList.contains('object-contain'));
      expect(lightboxImage).toHaveAttribute('src', 'https://example.com/image1.jpg');

      // Click next button
      const nextButton = screen.getByLabelText('Next media');
      fireEvent.click(nextButton);

      // Should show image2.jpg (displayOrder: 2)
      images = screen.getAllByAltText(/Event photo 2/i);
      lightboxImage = images.find(img => img.classList.contains('object-contain'));
      expect(lightboxImage).toHaveAttribute('src', 'https://example.com/image2.jpg');
    });

    it('should navigate to previous image in sorted order', () => {
      render(<MediaGallery images={mockImagesUnsorted} />);

      // Click second thumbnail to open lightbox (displayOrder: 2)
      const thumbnailButtons = screen.getAllByRole('button');
      fireEvent.click(thumbnailButtons[1]);

      // Should show image2.jpg - find lightbox image by class
      let images = screen.getAllByAltText(/Event photo 2/i);
      let lightboxImage = images.find(img => img.classList.contains('object-contain'));
      expect(lightboxImage).toHaveAttribute('src', 'https://example.com/image2.jpg');

      // Click previous button
      const prevButton = screen.getByLabelText('Previous media');
      fireEvent.click(prevButton);

      // Should show image1.jpg (displayOrder: 1)
      images = screen.getAllByAltText(/Event photo 1/i);
      lightboxImage = images.find(img => img.classList.contains('object-contain'));
      expect(lightboxImage).toHaveAttribute('src', 'https://example.com/image1.jpg');
    });

    it('should wrap around when navigating past last image', () => {
      render(<MediaGallery images={mockImagesUnsorted} />);

      // Click last thumbnail (displayOrder: 3)
      const thumbnailButtons = screen.getAllByRole('button');
      fireEvent.click(thumbnailButtons[2]);

      // Should show image3.jpg - find lightbox image by class
      let images = screen.getAllByAltText(/Event photo 3/i);
      let lightboxImage = images.find(img => img.classList.contains('object-contain'));
      expect(lightboxImage).toHaveAttribute('src', 'https://example.com/image3.jpg');

      // Click next button - should wrap to first
      const nextButton = screen.getByLabelText('Next media');
      fireEvent.click(nextButton);

      // Should show image1.jpg (wrap around)
      images = screen.getAllByAltText(/Event photo 1/i);
      lightboxImage = images.find(img => img.classList.contains('object-contain'));
      expect(lightboxImage).toHaveAttribute('src', 'https://example.com/image1.jpg');
    });

    it('should wrap around when navigating before first image', () => {
      render(<MediaGallery images={mockImagesUnsorted} />);

      // Click first thumbnail (displayOrder: 1)
      const thumbnailButtons = screen.getAllByRole('button');
      fireEvent.click(thumbnailButtons[0]);

      // Click previous button - should wrap to last
      const prevButton = screen.getByLabelText('Previous media');
      fireEvent.click(prevButton);

      // Should show image3.jpg (wrap around) - find lightbox image by class
      const images = screen.getAllByAltText(/Event photo 3/i);
      const lightboxImage = images.find(img => img.classList.contains('object-contain'));
      expect(lightboxImage).toHaveAttribute('src', 'https://example.com/image3.jpg');
    });
  });

  describe('Media Counter', () => {
    it('should display correct counter for current image position', () => {
      render(<MediaGallery images={mockImagesUnsorted} />);

      // Click second thumbnail
      const thumbnailButtons = screen.getAllByRole('button');
      fireEvent.click(thumbnailButtons[1]);

      // Counter should show "2 / 3"
      expect(screen.getByText('2 / 3')).toBeInTheDocument();
    });

    it('should update counter when navigating', () => {
      render(<MediaGallery images={mockImagesUnsorted} />);

      // Click first thumbnail
      const thumbnailButtons = screen.getAllByRole('button');
      fireEvent.click(thumbnailButtons[0]);

      // Counter should show "1 / 3"
      expect(screen.getByText('1 / 3')).toBeInTheDocument();

      // Navigate to next
      const nextButton = screen.getByLabelText('Next media');
      fireEvent.click(nextButton);

      // Counter should show "2 / 3"
      expect(screen.getByText('2 / 3')).toBeInTheDocument();
    });
  });

  describe('Edge Cases', () => {
    it('should handle single image correctly', () => {
      const singleImage: EventImageDto[] = [
        { id: 'img-1', imageUrl: 'https://example.com/single.jpg', displayOrder: 1 },
      ];

      render(<MediaGallery images={singleImage} />);

      // Click the thumbnail (first button before lightbox opens)
      const thumbnailButtons = screen.getAllByRole('button');
      fireEvent.click(thumbnailButtons[0]);

      // Should show the image in lightbox - find by class since there are two images with same alt
      const images = screen.getAllByAltText(/Event photo 1/i);
      const lightboxImage = images.find(img => img.classList.contains('object-contain'));
      expect(lightboxImage).toHaveAttribute('src', 'https://example.com/single.jpg');

      // Navigation buttons should not be visible for single image
      expect(screen.queryByLabelText('Previous media')).not.toBeInTheDocument();
      expect(screen.queryByLabelText('Next media')).not.toBeInTheDocument();
    });

    it('should handle images with same displayOrder gracefully', () => {
      const sameOrderImages: EventImageDto[] = [
        { id: 'img-1', imageUrl: 'https://example.com/imageA.jpg', displayOrder: 1 },
        { id: 'img-2', imageUrl: 'https://example.com/imageB.jpg', displayOrder: 1 },
      ];

      render(<MediaGallery images={sameOrderImages} />);

      // Should render both images
      const images = screen.getAllByRole('img');
      expect(images).toHaveLength(2);
    });
  });

  describe('Lightbox Close', () => {
    it('should have close button that closes lightbox', () => {
      render(<MediaGallery images={mockImagesUnsorted} />);

      // Open lightbox
      const thumbnailButtons = screen.getAllByRole('button');
      fireEvent.click(thumbnailButtons[0]);

      // Lightbox should be open
      expect(screen.getByTestId('dialog')).toBeInTheDocument();

      // Click close button
      const closeButton = screen.getByLabelText('Close lightbox');
      fireEvent.click(closeButton);

      // Lightbox should be closed
      expect(screen.queryByTestId('dialog')).not.toBeInTheDocument();
    });
  });
});
