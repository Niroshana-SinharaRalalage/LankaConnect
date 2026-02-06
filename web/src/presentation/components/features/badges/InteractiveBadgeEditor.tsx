'use client';

import * as React from 'react';
import { BadgeLocationEditor } from './BadgeLocationEditor';
import {
  BadgeLocationConfigDto,
  BadgeDisplayLocation,
  type BadgeDto,
} from '@/infrastructure/api/types/badges.types';

interface InteractiveBadgeEditorProps {
  /** Badge being edited */
  badge: BadgeDto;
  /** Called when any location config changes */
  onConfigChange: (
    location: BadgeDisplayLocation,
    config: BadgeLocationConfigDto
  ) => void;
  /** Whether the editor is disabled */
  disabled?: boolean;
}

/**
 * Phase 6A.32: InteractiveBadgeEditor Component
 * Orchestrates badge positioning for all three display locations with tabbed interface
 *
 * Features:
 * - Three tabs for different display locations (Listing, Featured, Detail)
 * - Manages state for all three location configs
 * - Mini preview section showing all 3 locations
 * - Real-time updates across all locations
 */
export function InteractiveBadgeEditor({
  badge,
  onConfigChange,
  disabled = false,
}: InteractiveBadgeEditorProps) {
  const [activeTab, setActiveTab] = React.useState<BadgeDisplayLocation>('listing');

  // Local state for all three configs (initialized from badge prop)
  const [listingConfig, setListingConfig] = React.useState<BadgeLocationConfigDto>(
    badge.listingConfig
  );
  const [featuredConfig, setFeaturedConfig] = React.useState<BadgeLocationConfigDto>(
    badge.featuredConfig
  );
  const [detailConfig, setDetailConfig] = React.useState<BadgeLocationConfigDto>(
    badge.detailConfig
  );

  // Sync with badge prop changes
  React.useEffect(() => {
    setListingConfig(badge.listingConfig);
    setFeaturedConfig(badge.featuredConfig);
    setDetailConfig(badge.detailConfig);
  }, [badge.listingConfig, badge.featuredConfig, badge.detailConfig]);

  // Handle config changes
  const handleListingChange = (config: BadgeLocationConfigDto) => {
    setListingConfig(config);
    onConfigChange('listing', config);
  };

  const handleFeaturedChange = (config: BadgeLocationConfigDto) => {
    setFeaturedConfig(config);
    onConfigChange('featured', config);
  };

  const handleDetailChange = (config: BadgeLocationConfigDto) => {
    setDetailConfig(config);
    onConfigChange('detail', config);
  };

  // Tab configuration
  const tabs: Array<{ key: BadgeDisplayLocation; label: string; icon: string }> = [
    { key: 'listing', label: 'Events Listing', icon: '📋' },
    { key: 'featured', label: 'Home Featured', icon: '⭐' },
    { key: 'detail', label: 'Event Detail', icon: '🖼️' },
  ];

  return (
    <div className="space-y-4">
      {/* Header */}
      <div>
        <h3 className="text-lg font-semibold text-gray-900">
          Interactive Badge Positioning
        </h3>
        <p className="text-sm text-gray-500 mt-1">
          Customize badge position, size, and rotation for each display location
        </p>
      </div>

      {/* Tab navigation */}
      <div className="border-b border-gray-200">
        <nav className="flex -mb-px space-x-2" aria-label="Location tabs">
          {tabs.map((tab) => (
            <button
              key={tab.key}
              type="button"
              onClick={() => setActiveTab(tab.key)}
              disabled={disabled}
              className={`
                flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors
                ${
                  activeTab === tab.key
                    ? 'border-[#FF7900] text-[#FF7900]'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }
                ${disabled ? 'cursor-not-allowed opacity-50' : 'cursor-pointer'}
              `}
            >
              <span className="text-base">{tab.icon}</span>
              <span>{tab.label}</span>
            </button>
          ))}
        </nav>
      </div>

      {/* Active tab content */}
      <div className="pt-2">
        {activeTab === 'listing' && (
          <BadgeLocationEditor
            location="listing"
            config={listingConfig}
            badgeImageUrl={badge.imageUrl}
            onChange={handleListingChange}
            disabled={disabled}
          />
        )}
        {activeTab === 'featured' && (
          <BadgeLocationEditor
            location="featured"
            config={featuredConfig}
            badgeImageUrl={badge.imageUrl}
            onChange={handleFeaturedChange}
            disabled={disabled}
          />
        )}
        {activeTab === 'detail' && (
          <BadgeLocationEditor
            location="detail"
            config={detailConfig}
            badgeImageUrl={badge.imageUrl}
            onChange={handleDetailChange}
            disabled={disabled}
          />
        )}
      </div>

      {/* Mini preview section - shows all 3 locations at once */}
      <div className="pt-4 border-t border-gray-200">
        <h4 className="text-sm font-medium text-gray-700 mb-3">
          Preview Across All Locations
        </h4>
        <div className="grid grid-cols-3 gap-3">
          {/* Listing mini preview */}
          <div className="space-y-1">
            <button
              type="button"
              onClick={() => setActiveTab('listing')}
              className={`
                w-full text-left p-2 rounded-lg border-2 transition-all
                ${
                  activeTab === 'listing'
                    ? 'border-[#FF7900] bg-orange-50'
                    : 'border-gray-200 hover:border-gray-300'
                }
              `}
            >
              <div className="text-[10px] font-medium text-gray-600 mb-1">
                📋 Events Listing
              </div>
              <div className="relative w-full aspect-[4/3] rounded overflow-hidden bg-gray-100">
                <img
                  src="/images/sri-lankan-background.jpg"
                  alt="Listing preview"
                  className="w-full h-full object-cover"
                />
                <div
                  className="absolute"
                  style={{
                    left: `${listingConfig.positionX * 100}%`,
                    top: `${listingConfig.positionY * 100}%`,
                    width: `${listingConfig.sizeWidth * 100}%`,
                    height: `${listingConfig.sizeHeight * 100}%`,
                    transform: `rotate(${listingConfig.rotation}deg)`,
                    transformOrigin: 'center',
                  }}
                >
                  <img
                    src={badge.imageUrl}
                    alt="Badge"
                    className="w-full h-full object-contain drop-shadow"
                  />
                </div>
              </div>
            </button>
            <p className="text-[9px] text-gray-400 text-center">192×144px</p>
          </div>

          {/* Featured mini preview */}
          <div className="space-y-1">
            <button
              type="button"
              onClick={() => setActiveTab('featured')}
              className={`
                w-full text-left p-2 rounded-lg border-2 transition-all
                ${
                  activeTab === 'featured'
                    ? 'border-[#FF7900] bg-orange-50'
                    : 'border-gray-200 hover:border-gray-300'
                }
              `}
            >
              <div className="text-[10px] font-medium text-gray-600 mb-1">
                ⭐ Home Featured
              </div>
              <div className="relative w-full aspect-[4/3] rounded overflow-hidden bg-gray-100">
                <img
                  src="/images/sri-lankan-background.jpg"
                  alt="Featured preview"
                  className="w-full h-full object-cover"
                />
                <div
                  className="absolute"
                  style={{
                    left: `${featuredConfig.positionX * 100}%`,
                    top: `${featuredConfig.positionY * 100}%`,
                    width: `${featuredConfig.sizeWidth * 100}%`,
                    height: `${featuredConfig.sizeHeight * 100}%`,
                    transform: `rotate(${featuredConfig.rotation}deg)`,
                    transformOrigin: 'center',
                  }}
                >
                  <img
                    src={badge.imageUrl}
                    alt="Badge"
                    className="w-full h-full object-contain drop-shadow"
                  />
                </div>
              </div>
            </button>
            <p className="text-[9px] text-gray-400 text-center">160×120px</p>
          </div>

          {/* Detail mini preview */}
          <div className="space-y-1">
            <button
              type="button"
              onClick={() => setActiveTab('detail')}
              className={`
                w-full text-left p-2 rounded-lg border-2 transition-all
                ${
                  activeTab === 'detail'
                    ? 'border-[#FF7900] bg-orange-50'
                    : 'border-gray-200 hover:border-gray-300'
                }
              `}
            >
              <div className="text-[10px] font-medium text-gray-600 mb-1">
                🖼️ Event Detail
              </div>
              <div className="relative w-full aspect-[4/3] rounded overflow-hidden bg-gray-100">
                <img
                  src="/images/sri-lankan-background.jpg"
                  alt="Detail preview"
                  className="w-full h-full object-cover"
                />
                <div
                  className="absolute"
                  style={{
                    left: `${detailConfig.positionX * 100}%`,
                    top: `${detailConfig.positionY * 100}%`,
                    width: `${detailConfig.sizeWidth * 100}%`,
                    height: `${detailConfig.sizeHeight * 100}%`,
                    transform: `rotate(${detailConfig.rotation}deg)`,
                    transformOrigin: 'center',
                  }}
                >
                  <img
                    src={badge.imageUrl}
                    alt="Badge"
                    className="w-full h-full object-contain drop-shadow"
                  />
                </div>
              </div>
            </button>
            <p className="text-[9px] text-gray-400 text-center">384×288px</p>
          </div>
        </div>
        <p className="text-xs text-gray-400 mt-2">
          Click any preview to switch to that location's editor
        </p>
      </div>
    </div>
  );
}
