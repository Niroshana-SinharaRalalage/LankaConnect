'use client';

import * as React from 'react';
import Image from 'next/image';
import { Rnd } from 'react-rnd';
import { useBadgePositioning } from '@/presentation/hooks/useBadgePositioning';
import {
  BadgeLocationConfigDto,
  BADGE_CONTAINER_SIZES,
  type BadgeDisplayLocation,
} from '@/infrastructure/api/types/badges.types';

interface BadgeLocationEditorProps {
  /** The display location being edited (listing, featured, or detail) */
  location: BadgeDisplayLocation;
  /** Current badge configuration for this location */
  config: BadgeLocationConfigDto;
  /** Badge image URL to display */
  badgeImageUrl: string;
  /** Called when the configuration changes */
  onChange: (config: BadgeLocationConfigDto) => void;
  /** Whether the editor is disabled */
  disabled?: boolean;
}

/**
 * Phase 6A.32: BadgeLocationEditor Component
 * Interactive badge positioning editor using react-rnd for drag/resize functionality
 *
 * Features:
 * - Drag badge to reposition
 * - Resize badge by dragging corners
 * - Rotation slider
 * - Fine-tuning sliders for position and size
 * - Real-time preview with percentage-based coordinates
 * - Converts pixel coordinates to ratio (0-1) for responsive scaling
 */
export function BadgeLocationEditor({
  location,
  config,
  badgeImageUrl,
  onChange,
  disabled = false,
}: BadgeLocationEditorProps) {
  const containerSize = BADGE_CONTAINER_SIZES[location];
  const { toLocationConfig, fromLocationConfig, validateConfig } = useBadgePositioning();

  // Convert config to pixel coordinates for react-rnd
  const { position, size, rotation } = fromLocationConfig(config, containerSize);

  // Handle drag end
  const handleDragStop = (_e: any, data: { x: number; y: number }) => {
    const newConfig = toLocationConfig(
      { x: data.x, y: data.y },
      size,
      rotation,
      containerSize
    );
    onChange(validateConfig(newConfig));
  };

  // Handle resize stop
  const handleResizeStop = (
    _e: any,
    _direction: any,
    ref: HTMLElement,
    _delta: any,
    position: { x: number; y: number }
  ) => {
    const newConfig = toLocationConfig(
      position,
      { width: ref.offsetWidth, height: ref.offsetHeight },
      rotation,
      containerSize
    );
    onChange(validateConfig(newConfig));
  };

  // Handle rotation slider change
  const handleRotationChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newRotation = Number(e.target.value);
    const newConfig = toLocationConfig(position, size, newRotation, containerSize);
    onChange(validateConfig(newConfig));
  };

  // Handle position sliders
  const handlePositionXChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newConfig = { ...config, positionX: Number(e.target.value) };
    onChange(validateConfig(newConfig));
  };

  const handlePositionYChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newConfig = { ...config, positionY: Number(e.target.value) };
    onChange(validateConfig(newConfig));
  };

  // Handle size sliders
  const handleSizeWidthChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newConfig = { ...config, sizeWidth: Number(e.target.value) };
    onChange(validateConfig(newConfig));
  };

  const handleSizeHeightChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newConfig = { ...config, sizeHeight: Number(e.target.value) };
    onChange(validateConfig(newConfig));
  };

  // Get location display name
  const getLocationName = () => {
    switch (location) {
      case 'listing':
        return 'Events Listing';
      case 'featured':
        return 'Home Featured';
      case 'detail':
        return 'Event Detail Hero';
    }
  };

  return (
    <div className="space-y-4">
      {/* Location header */}
      <div>
        <h4 className="text-sm font-medium text-gray-700">{getLocationName()}</h4>
        <p className="text-xs text-gray-500">
          Container: {containerSize.width}×{containerSize.height}px
        </p>
      </div>

      {/* Interactive preview container */}
      <div className="relative border-2 border-gray-300 rounded-lg overflow-hidden bg-gray-50">
        <div
          className="relative"
          style={{
            width: containerSize.width,
            height: containerSize.height,
            margin: '0 auto',
          }}
        >
          {/* Background image */}
          <Image
            src="/images/sri-lankan-background.jpg"
            alt={`${getLocationName()} Preview`}
            width={containerSize.width}
            height={containerSize.height}
            className="object-cover"
            unoptimized
          />

          {/* Draggable/Resizable Badge */}
          <Rnd
            size={{ width: size.width, height: size.height }}
            position={{ x: position.x, y: position.y }}
            onDragStop={handleDragStop}
            onResizeStop={handleResizeStop}
            bounds="parent"
            minWidth={containerSize.width * 0.05}
            minHeight={containerSize.height * 0.05}
            lockAspectRatio={false}
            enableResizing={{
              top: !disabled,
              right: !disabled,
              bottom: !disabled,
              left: !disabled,
              topRight: !disabled,
              bottomRight: !disabled,
              bottomLeft: !disabled,
              topLeft: !disabled,
            }}
            disableDragging={disabled}
            className={disabled ? 'cursor-not-allowed' : 'cursor-move'}
          >
            <div
              className="w-full h-full flex items-center justify-center"
              style={{
                transform: `rotate(${rotation}deg)`,
                transformOrigin: 'center',
              }}
            >
              <Image
                src={badgeImageUrl}
                alt="Badge"
                fill
                className="object-contain drop-shadow-lg pointer-events-none"
                unoptimized
              />
            </div>
          </Rnd>

          {/* Event info overlay (for realism) */}
          <div className="absolute bottom-0 left-0 right-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent p-2 pointer-events-none">
            <h5 className="text-white font-semibold text-xs truncate">Sample Cultural Event</h5>
            <p className="text-white/80 text-[10px] mt-0.5">Dec 25 • Cleveland, OH</p>
          </div>
        </div>
      </div>

      {/* Control sliders */}
      <div className="space-y-3 bg-gray-50 p-4 rounded-lg border border-gray-200">
        {/* Position controls */}
        <div className="space-y-2">
          <label className="text-xs font-medium text-gray-700">Position</label>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-[10px] text-gray-500">Horizontal (X)</label>
              <input
                type="range"
                min="0"
                max="1"
                step="0.01"
                value={config.positionX}
                onChange={handlePositionXChange}
                disabled={disabled}
                className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer accent-[#FF7900]"
              />
              <div className="text-[10px] text-gray-400 text-right">
                {(config.positionX * 100).toFixed(0)}%
              </div>
            </div>
            <div>
              <label className="text-[10px] text-gray-500">Vertical (Y)</label>
              <input
                type="range"
                min="0"
                max="1"
                step="0.01"
                value={config.positionY}
                onChange={handlePositionYChange}
                disabled={disabled}
                className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer accent-[#FF7900]"
              />
              <div className="text-[10px] text-gray-400 text-right">
                {(config.positionY * 100).toFixed(0)}%
              </div>
            </div>
          </div>
        </div>

        {/* Size controls */}
        <div className="space-y-2">
          <label className="text-xs font-medium text-gray-700">Size</label>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-[10px] text-gray-500">Width</label>
              <input
                type="range"
                min="0.05"
                max="1"
                step="0.01"
                value={config.sizeWidth}
                onChange={handleSizeWidthChange}
                disabled={disabled}
                className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer accent-[#FF7900]"
              />
              <div className="text-[10px] text-gray-400 text-right">
                {(config.sizeWidth * 100).toFixed(0)}%
              </div>
            </div>
            <div>
              <label className="text-[10px] text-gray-500">Height</label>
              <input
                type="range"
                min="0.05"
                max="1"
                step="0.01"
                value={config.sizeHeight}
                onChange={handleSizeHeightChange}
                disabled={disabled}
                className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer accent-[#FF7900]"
              />
              <div className="text-[10px] text-gray-400 text-right">
                {(config.sizeHeight * 100).toFixed(0)}%
              </div>
            </div>
          </div>
        </div>

        {/* Rotation control */}
        <div className="space-y-2">
          <label className="text-xs font-medium text-gray-700">Rotation</label>
          <div>
            <input
              type="range"
              min="0"
              max="360"
              step="1"
              value={rotation}
              onChange={handleRotationChange}
              disabled={disabled}
              className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer accent-[#FF7900]"
            />
            <div className="text-[10px] text-gray-400 text-right">{rotation.toFixed(0)}°</div>
          </div>
        </div>

        {/* Hint text */}
        <p className="text-[10px] text-gray-400 mt-2">
          💡 Drag to reposition • Drag corners to resize • Use sliders for fine-tuning
        </p>
      </div>
    </div>
  );
}
