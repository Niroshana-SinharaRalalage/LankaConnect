'use client';

import * as React from 'react';

const VIDEO_CLIPS = [
  '/images/117734-713278961_small.mp4',
  '/images/video-clip-2.mp4',
  '/images/video-clip-3.mp4',
];

interface VideoPlayerProps {
  className?: string;
}

/**
 * Auto-cycling video player that plays short clips one after another.
 * Muted, no controls, loops through all clips continuously.
 */
export function VideoPlayer({ className = '' }: VideoPlayerProps) {
  const videoRef = React.useRef<HTMLVideoElement>(null);
  const [currentIndex, setCurrentIndex] = React.useState(0);

  const handleEnded = React.useCallback(() => {
    setCurrentIndex((prev) => (prev + 1) % VIDEO_CLIPS.length);
  }, []);

  // When index changes, load and play the new video
  React.useEffect(() => {
    const video = videoRef.current;
    if (!video) return;
    video.load();
    video.play().catch(() => {
      // Autoplay may be blocked; silently ignore
    });
  }, [currentIndex]);

  return (
    <video
      ref={videoRef}
      className={`w-full h-full object-cover ${className}`}
      src={VIDEO_CLIPS[currentIndex]}
      muted
      playsInline
      autoPlay
      onEnded={handleEnded}
    />
  );
}
