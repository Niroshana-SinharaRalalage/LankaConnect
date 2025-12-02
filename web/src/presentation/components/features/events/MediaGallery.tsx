'use client';

import { useState } from 'react';
import { X, Play, ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { Dialog, DialogContent } from '@/presentation/components/ui/Dialog';
import { EventImageDto, EventVideoDto } from '@/infrastructure/api/types/events.types';

interface MediaGalleryProps {
  images?: readonly EventImageDto[];
  videos?: readonly EventVideoDto[];
  className?: string;
}

/**
 * MediaGallery Component
 * Read-only gallery for displaying event images and videos on the event detail page
 * Features:
 * - Responsive grid layout
 * - Lightbox modal for full-screen viewing
 * - Image/video carousel navigation
 * - Dark mode support
 */
export function MediaGallery({ images = [], videos = [], className = '' }: MediaGalleryProps) {
  const [lightboxOpen, setLightboxOpen] = useState(false);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [mediaType, setMediaType] = useState<'image' | 'video'>('image');

  // Combine images and videos for total media count
  const totalImages = images.length;
  const totalVideos = videos.length;
  const totalMedia = totalImages + totalVideos;

  if (totalMedia === 0) {
    return null; // Don't render if no media
  }

  // Open lightbox for image
  const openImageLightbox = (index: number) => {
    setCurrentIndex(index);
    setMediaType('image');
    setLightboxOpen(true);
  };

  // Open lightbox for video
  const openVideoLightbox = (index: number) => {
    setCurrentIndex(index);
    setMediaType('video');
    setLightboxOpen(true);
  };

  // Navigate to previous media
  const goToPrevious = () => {
    if (mediaType === 'image') {
      setCurrentIndex((prev) => (prev === 0 ? totalImages - 1 : prev - 1));
    } else {
      setCurrentIndex((prev) => (prev === 0 ? totalVideos - 1 : prev - 1));
    }
  };

  // Navigate to next media
  const goToNext = () => {
    if (mediaType === 'image') {
      setCurrentIndex((prev) => (prev === totalImages - 1 ? 0 : prev + 1));
    } else {
      setCurrentIndex((prev) => (prev === totalVideos - 1 ? 0 : prev + 1));
    }
  };

  return (
    <>
      <div className={`space-y-6 ${className}`}>
        {/* Images Section */}
        {totalImages > 0 && (
          <div>
            <h3 className="text-lg font-semibold text-neutral-900 dark:text-neutral-100 mb-3">
              Photos ({totalImages})
            </h3>
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
              {[...images]
                .sort((a, b) => a.displayOrder - b.displayOrder)
                .map((image, index) => (
                  <button
                    key={image.id}
                    onClick={() => openImageLightbox(index)}
                    className="relative aspect-square rounded-lg overflow-hidden bg-neutral-100 dark:bg-neutral-800 hover:opacity-90 transition-opacity group"
                  >
                    <img
                      src={image.imageUrl}
                      alt={`Event photo ${image.displayOrder}`}
                      className="w-full h-full object-cover"
                    />
                    <div className="absolute inset-0 bg-black/0 group-hover:bg-black/10 transition-colors" />
                    <div className="absolute top-2 left-2 bg-black/50 text-white text-xs px-2 py-1 rounded">
                      {image.displayOrder}
                    </div>
                  </button>
                ))}
            </div>
          </div>
        )}

        {/* Videos Section */}
        {totalVideos > 0 && (
          <div>
            <h3 className="text-lg font-semibold text-neutral-900 dark:text-neutral-100 mb-3">
              Videos ({totalVideos})
            </h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4">
              {[...videos]
                .sort((a, b) => a.displayOrder - b.displayOrder)
                .map((video, index) => (
                  <button
                    key={video.id}
                    onClick={() => openVideoLightbox(index)}
                    className="relative aspect-video rounded-lg overflow-hidden bg-neutral-100 dark:bg-neutral-800 hover:opacity-90 transition-opacity group"
                  >
                    <img
                      src={video.thumbnailUrl}
                      alt={`Video ${video.displayOrder}`}
                      className="w-full h-full object-cover"
                    />
                    <div className="absolute inset-0 bg-black/30 group-hover:bg-black/40 transition-colors flex items-center justify-center">
                      <div className="bg-white/90 rounded-full p-4">
                        <Play className="h-8 w-8 text-neutral-900" fill="currentColor" />
                      </div>
                    </div>
                    <div className="absolute top-2 left-2 bg-black/50 text-white text-xs px-2 py-1 rounded">
                      {video.displayOrder}
                    </div>
                  </button>
                ))}
            </div>
          </div>
        )}
      </div>

      {/* Lightbox Modal */}
      <Dialog open={lightboxOpen} onOpenChange={setLightboxOpen}>
        <DialogContent className="max-w-7xl w-full h-[90vh] p-0 bg-black/95">
          <div className="relative w-full h-full flex items-center justify-center">
            {/* Close Button */}
            <button
              onClick={() => setLightboxOpen(false)}
              className="absolute top-4 right-4 z-50 p-2 rounded-full bg-white/10 hover:bg-white/20 transition-colors"
              aria-label="Close lightbox"
            >
              <X className="h-6 w-6 text-white" />
            </button>

            {/* Navigation Buttons */}
            {((mediaType === 'image' && totalImages > 1) ||
              (mediaType === 'video' && totalVideos > 1)) && (
              <>
                <button
                  onClick={goToPrevious}
                  className="absolute left-4 z-50 p-3 rounded-full bg-white/10 hover:bg-white/20 transition-colors"
                  aria-label="Previous media"
                >
                  <ChevronLeft className="h-6 w-6 text-white" />
                </button>
                <button
                  onClick={goToNext}
                  className="absolute right-4 z-50 p-3 rounded-full bg-white/10 hover:bg-white/20 transition-colors"
                  aria-label="Next media"
                >
                  <ChevronRight className="h-6 w-6 text-white" />
                </button>
              </>
            )}

            {/* Media Content */}
            <div className="w-full h-full flex items-center justify-center p-12">
              {mediaType === 'image' && images[currentIndex] && (
                <img
                  src={images[currentIndex].imageUrl}
                  alt={`Event photo ${images[currentIndex].displayOrder}`}
                  className="max-w-full max-h-full object-contain"
                />
              )}

              {mediaType === 'video' && videos[currentIndex] && (
                <video
                  src={videos[currentIndex].videoUrl}
                  controls
                  autoPlay
                  className="max-w-full max-h-full"
                  poster={videos[currentIndex].thumbnailUrl}
                >
                  Your browser does not support the video tag.
                </video>
              )}
            </div>

            {/* Media Counter */}
            <div className="absolute bottom-4 left-1/2 -translate-x-1/2 bg-white/10 text-white text-sm px-4 py-2 rounded-full">
              {currentIndex + 1} / {mediaType === 'image' ? totalImages : totalVideos}
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
