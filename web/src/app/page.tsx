'use client';

import * as React from 'react';
import { Button } from '@/presentation/components/ui/Button';
import { StatCard } from '@/presentation/components/ui/StatCard';
import { Logo } from '@/presentation/components/atoms/Logo';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import { useRouter } from 'next/navigation';
import { Users, Calendar, Building2, Heart, MessageSquare, TrendingUp, MapPin, ThumbsUp, Sparkles } from 'lucide-react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { FeedTabs, ActivityFeed } from '@/presentation/components/features/feed';
import { MetroAreaProvider, useMetroArea } from '@/presentation/components/features/location/MetroAreaContext';
import { MetroAreaSelector } from '@/presentation/components/features/location/MetroAreaSelector';
import { ALL_METRO_AREAS, getMetroById } from '@/domain/constants/metroAreas.constants';
import type { FeedItem } from '@/domain/models/FeedItem';
import type { MetroArea } from '@/domain/models/MetroArea';
import { useEvents } from '@/presentation/hooks/useEvents';
import { mapEventListToFeedItems } from '@/application/mappers/eventMapper';

/**
 * Landing Page Component
 * Public landing page showcasing LankaConnect platform with Sri Lankan flag colors
 * Features: Flag header, sticky navbar, hero section, community stats, activity feed
 */

/**
 * State name to abbreviation mapping for API compatibility
 * API returns full state names (e.g., "Ohio") but metro areas use abbreviations (e.g., "OH")
 */
const STATE_ABBR_MAP: Record<string, string> = {
  'Ohio': 'OH',
  'Pennsylvania': 'PA',
  'California': 'CA',
  'Texas': 'TX',
  'New York': 'NY',
  'Illinois': 'IL',
  'Arizona': 'AZ',
  'Colorado': 'CO',
  'Georgia': 'GA',
  'Indiana': 'IN',
  'Massachusetts': 'MA',
  'Washington': 'WA',
} as const;

/**
 * Metro Area Selector with Geolocation for Landing Page
 * Uses full MetroAreaSelector component with geolocation support
 */
function LandingMetroSelector() {
  const {
    selectedMetroArea,
    setMetroArea,
    userLocation,
    isDetecting,
    detectionError,
    detectLocation,
    setAvailableMetros
  } = useMetroArea();

  // Set available metros on mount
  React.useEffect(() => {
    setAvailableMetros(ALL_METRO_AREAS);
  }, [setAvailableMetros]);

  const handleChange = (metroId: string | null) => {
    if (!metroId) {
      setMetroArea(null);
    } else {
      const metro = ALL_METRO_AREAS.find(m => m.id === metroId);
      setMetroArea(metro || null);
    }
  };

  return (
    <div className="w-full max-w-md">
      <MetroAreaSelector
        value={selectedMetroArea?.id || null}
        metros={ALL_METRO_AREAS}
        onChange={handleChange}
        userLocation={userLocation}
        isDetecting={isDetecting}
        detectionError={detectionError}
        onDetectLocation={detectLocation}
        placeholder="Select your metro area"
      />
    </div>
  );
}

/**
 * Main Page Content Component
 * Separated to use MetroAreaContext hooks
 * Phase 5B.9: Display events organized by preferred metros vs other metros
 */
function HomeContent() {
  const { selectedMetroArea } = useMetroArea();
  const { isAuthenticated, user } = useAuthStore();
  const { profile } = useProfileStore();
  const [activeTab, setActiveTab] = React.useState<'all' | 'event' | 'business' | 'forum' | 'culture'>('all');
  const [showOtherMetros, setShowOtherMetros] = React.useState(true);

  // Fetch events from API
  const { data: events, isLoading, error } = useEvents();

  // Convert API events to feed items (no mock data)
  const allFeedItems = React.useMemo((): FeedItem[] => {
    if (events && events.length > 0) {
      return mapEventListToFeedItems(events);
    }
    return [];
  }, [events]);

  /**
   * Helper: Check if an event is in a specific metro area
   * Handles both state-level and city-level metros
   */
  const isEventInMetro = React.useCallback((item: FeedItem, metro: MetroArea): boolean => {
    // State-level filtering: If metro area is marked as "Statewide"
    if (metro.cities.includes('Statewide')) {
      const fullStateName = Object.keys(STATE_ABBR_MAP).find(
        name => STATE_ABBR_MAP[name as keyof typeof STATE_ABBR_MAP] === metro.state
      );
      const statePattern = new RegExp(`[,\\s]${fullStateName}([,\\s]|$)`, 'i');
      return statePattern.test(item.location);
    }

    // City-level filtering: Check if item location matches any city in the metro area
    const itemCity = item.location.split(',')[0].trim();
    return metro.cities.some(city => city === itemCity);
  }, []);

  /**
   * Filter events by preferred metros
   * Phase 5B.9: Separate events into preferred metros vs other metros
   */
  const { preferredItems, otherItems } = React.useMemo(() => {
    let preferred: FeedItem[] = [];
    let other: FeedItem[] = [];

    let itemsToProcess = allFeedItems;

    // Apply active tab filter first
    if (activeTab !== 'all') {
      itemsToProcess = itemsToProcess.filter(item => item.type === activeTab);
    }

    // If user is authenticated and has preferred metros, separate items
    if (isAuthenticated && profile?.preferredMetroAreas && profile.preferredMetroAreas.length > 0) {
      const preferredMetroIds = new Set(profile.preferredMetroAreas);

      itemsToProcess.forEach(item => {
        let isInPreferred = false;

        // Check if item matches any preferred metro
        for (const metroId of preferredMetroIds) {
          const metro = getMetroById(metroId);
          if (metro && isEventInMetro(item, metro)) {
            isInPreferred = true;
            break;
          }
        }

        if (isInPreferred) {
          preferred.push(item);
        } else {
          other.push(item);
        }
      });
    } else if (selectedMetroArea) {
      // If user has manually selected a metro area, use that for filtering
      itemsToProcess = itemsToProcess.filter(item => isEventInMetro(item, selectedMetroArea));
      other = itemsToProcess;
    } else {
      // If no preferred metros and no selected metro, show all items
      other = itemsToProcess;
    }

    return { preferredItems: preferred, otherItems: other };
  }, [allFeedItems, activeTab, isAuthenticated, profile, selectedMetroArea, isEventInMetro]);

  return (
    <div className="min-h-screen bg-[#f8f9fa]">
      <Header />

      {/* Hero Section with Flag Colors Gradient */}
      <section
        className="relative text-white py-8 overflow-hidden"
        style={{
          background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 50%, #006400 100%)'
        }}
      >
        {/* Diagonal Stripe Pattern Overlay */}
        <div
          className="absolute inset-0 opacity-[0.05]"
          style={{
            backgroundImage: 'repeating-linear-gradient(45deg, transparent, transparent 35px, rgba(255,255,255,1) 35px, rgba(255,255,255,1) 70px)'
          }}
        />

        <div className="container mx-auto px-4 sm:px-6 lg:px-8 relative z-10">
          <div className="text-center max-w-4xl mx-auto">
            {/* Hero Heading */}
            <h1 className="text-5xl font-bold mb-3 leading-tight">
              Connect. Celebrate. Thrive.
            </h1>

            {/* Hero Subheading */}
            <p className="text-xl opacity-95">
              The complete Sri Lankan American community platform bringing our diaspora together
            </p>
          </div>
        </div>
      </section>

      {/* Community Stats Section - Ultra Compact */}
      <section className="py-1 bg-white">
        <div className="container mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-1.5 max-w-7xl mx-auto">
            <StatCard
              title="Members"
              value="12,500+"
              size="sm"
              className="border-l-4 border-[#FF7900] !p-2 !shadow-none"
            />
            <StatCard
              title="Events"
              value="450+"
              size="sm"
              className="border-l-4 border-[#8B1538] !p-2 !shadow-none"
            />
            <StatCard
              title="Businesses"
              value="2,200+"
              size="sm"
              className="border-l-4 border-[#006400] !p-2 !shadow-none"
            />
            <StatCard
              title="Discussions"
              value="456"
              size="sm"
              className="border-l-4 border-[#FF7900] !p-2 !shadow-none"
            />
          </div>
        </div>
      </section>

      {/* Widgets Section - Horizontal 3-Column Layout (MOVED FROM BOTTOM) */}
      <section className="py-2 bg-[#f8f9fa]">
        <div className="container mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3 max-w-7xl mx-auto">
            {/* Cultural Calendar Widget */}
            <div className="bg-white rounded-xl shadow-[0_2px_15px_rgba(0,0,0,0.08)] overflow-hidden">
              <div className="bg-[#FFF9F5] px-4 py-3 border-b border-[#e2e8f0] font-semibold text-[#8B1538]">
                🗓️ Cultural Calendar
              </div>
              <div className="p-3">
                <div className="flex items-center py-2 border-b border-[#f1f5f9]">
                  <div
                    className="text-white px-2 py-2 rounded-lg text-center min-w-[60px] mr-4"
                    style={{
                      background: 'linear-gradient(135deg, #FF7900, #8B1538)'
                    }}
                  >
                    <div className="text-xl font-bold leading-none">13</div>
                    <div className="text-xs opacity-90">APR</div>
                  </div>
                  <div>
                    <h4 className="text-sm font-semibold text-[#8B1538] mb-1">Sinhala New Year</h4>
                    <p className="text-xs text-[#718096]">Traditional celebrations nationwide</p>
                  </div>
                </div>
                <div className="flex items-center py-2 border-b border-[#f1f5f9]">
                  <div
                    className="text-white px-2 py-2 rounded-lg text-center min-w-[60px] mr-4"
                    style={{
                      background: 'linear-gradient(135deg, #FF7900, #8B1538)'
                    }}
                  >
                    <div className="text-xl font-bold leading-none">23</div>
                    <div className="text-xs opacity-90">MAY</div>
                  </div>
                  <div>
                    <h4 className="text-sm font-semibold text-[#8B1538] mb-1">Vesak Day</h4>
                    <p className="text-xs text-[#718096]">Buddhist celebration of enlightenment</p>
                  </div>
                </div>
                <div className="flex items-center py-2">
                  <div
                    className="text-white px-2 py-2 rounded-lg text-center min-w-[60px] mr-4"
                    style={{
                      background: 'linear-gradient(135deg, #FF7900, #8B1538)'
                    }}
                  >
                    <div className="text-xl font-bold leading-none">15</div>
                    <div className="text-xs opacity-90">AUG</div>
                  </div>
                  <div>
                    <h4 className="text-sm font-semibold text-[#8B1538] mb-1">Independence Day</h4>
                    <p className="text-xs text-[#718096]">Sri Lankan independence celebrations</p>
                  </div>
                </div>
              </div>
            </div>

            {/* Featured Businesses Widget */}
            <div className="bg-white rounded-xl shadow-[0_2px_15px_rgba(0,0,0,0.08)] overflow-hidden">
              <div className="bg-[#FFF9F5] px-4 py-3 border-b border-[#e2e8f0] font-semibold text-[#8B1538]">
                ⭐ Featured Businesses
              </div>
              <div className="p-3">
                <div className="flex items-center py-2 border-b border-[#f1f5f9]">
                  <div
                    className="w-10 h-10 rounded-lg flex items-center justify-center text-white font-bold mr-3"
                    style={{
                      background: 'linear-gradient(135deg, #FF7900, #8B1538)'
                    }}
                  >
                    LK
                  </div>
                  <div className="flex-1">
                    <h4 className="text-sm font-semibold text-[#8B1538] mb-1">Lanka Kitchen</h4>
                    <p className="text-xs text-[#718096]">Authentic Sri Lankan Restaurant</p>
                  </div>
                  <div className="text-[#FF7900] text-xs font-semibold">⭐ 4.8</div>
                </div>
                <div className="flex items-center py-2 border-b border-[#f1f5f9]">
                  <div
                    className="w-10 h-10 rounded-lg flex items-center justify-center text-white font-bold mr-3"
                    style={{
                      background: 'linear-gradient(135deg, #FF7900, #8B1538)'
                    }}
                  >
                    ST
                  </div>
                  <div className="flex-1">
                    <h4 className="text-sm font-semibold text-[#8B1538] mb-1">Spice Temple</h4>
                    <p className="text-xs text-[#718096]">Grocery & Spices</p>
                  </div>
                  <div className="text-[#FF7900] text-xs font-semibold">⭐ 4.9</div>
                </div>
                <div className="flex items-center py-2">
                  <div
                    className="w-10 h-10 rounded-lg flex items-center justify-center text-white font-bold mr-3"
                    style={{
                      background: 'linear-gradient(135deg, #FF7900, #8B1538)'
                    }}
                  >
                    DL
                  </div>
                  <div className="flex-1">
                    <h4 className="text-sm font-semibold text-[#8B1538] mb-1">Dr. Lanka Immigration</h4>
                    <p className="text-xs text-[#718096]">Legal Services</p>
                  </div>
                  <div className="text-[#FF7900] text-xs font-semibold">⭐ 4.7</div>
                </div>
              </div>
            </div>

            {/* Community Stats Widget */}
            <div className="bg-white rounded-xl shadow-[0_2px_15px_rgba(0,0,0,0.08)] overflow-hidden">
              <div className="bg-[#FFF9F5] px-4 py-3 border-b border-[#e2e8f0] font-semibold text-[#8B1538]">
                📊 Community Stats
              </div>
              <div className="p-3">
                <div className="flex justify-between mb-3">
                  <span className="text-sm text-[#718096]">Active Today</span>
                  <strong className="text-[#8B1538]">2,340</strong>
                </div>
                <div className="flex justify-between mb-3">
                  <span className="text-sm text-[#718096]">Events This Week</span>
                  <strong className="text-[#8B1538]">127</strong>
                </div>
                <div className="flex justify-between mb-3">
                  <span className="text-sm text-[#718096]">New Businesses</span>
                  <strong className="text-[#8B1538]">23</strong>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-[#718096]">Forum Discussions</span>
                  <strong className="text-[#8B1538]">456</strong>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Main Content - Activity Feed with Compact Header */}
      <section className="py-4 bg-white">
        <div className="container mx-auto px-4 sm:px-6 lg:px-8">
          <div className="max-w-7xl mx-auto">
            {/* Feed Header with Metro Selector - Ultra Compact */}
            <div className="bg-white rounded-t-xl shadow-[0_2px_15px_rgba(0,0,0,0.08)] overflow-hidden">
              <div
                className="text-white px-3 py-1.5"
                style={{
                  background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)'
                }}
              >
                <div className="flex justify-between items-center mb-1.5">
                  <h2 className="text-sm font-semibold">Community Activity</h2>
                </div>
                <LandingMetroSelector />
              </div>

              {/* Feed Tabs */}
              <FeedTabs activeTab={activeTab} onTabChange={setActiveTab} />
            </div>

            {/* Activity Feed - Two Section Layout (Phase 5B.9) */}
            <div className="bg-white rounded-b-xl shadow-[0_2px_15px_rgba(0,0,0,0.08)] overflow-hidden">
              {error ? (
                <div className="p-8 text-center">
                  <p className="text-red-600 mb-4">Failed to load events. Please try again later.</p>
                  <p className="text-sm text-gray-600">Showing cached content...</p>
                </div>
              ) : null}

              {/* Preferred Metros Section (Phase 5B.9) */}
              {isAuthenticated && preferredItems.length > 0 && (
                <div className="border-b border-[#e2e8f0]">
                  <div className="px-4 py-3 bg-[#FFF9F5] border-b border-[#e2e8f0] flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <Sparkles className="w-5 h-5" style={{ color: '#FF7900' }} />
                      <h3 className="font-semibold text-[#8B1538]">
                        Events in Your Preferred Metros
                      </h3>
                      <span className="text-xs font-semibold px-2 py-1 rounded-full" style={{ background: '#FFE8CC', color: '#8B1538' }}>
                        {preferredItems.length}
                      </span>
                    </div>
                  </div>
                  <ActivityFeed items={preferredItems} loading={isLoading} gridView={true} />
                </div>
              )}

              {/* All Other Events Section */}
              {(otherItems.length > 0 || preferredItems.length === 0) && (
                <div>
                  {preferredItems.length > 0 && (
                    <div className="px-4 py-2">
                      <button
                        onClick={() => setShowOtherMetros(!showOtherMetros)}
                        className="w-full flex items-center justify-between gap-2 text-left hover:bg-[#f8f9fa] py-2"
                      >
                        <div className="flex items-center gap-2">
                          <MapPin className="w-5 h-5" style={{ color: '#8B1538' }} />
                          <h3 className="font-semibold text-[#8B1538]">
                            All Other Events
                          </h3>
                          <span className="text-xs font-semibold px-2 py-1 rounded-full" style={{ background: '#e2e8f0', color: '#4b5563' }}>
                            {otherItems.length}
                          </span>
                        </div>
                        <span className="text-[#718096]">{showOtherMetros ? '▼' : '▶'}</span>
                      </button>
                    </div>
                  )}
                  {showOtherMetros && <ActivityFeed items={otherItems} loading={isLoading} gridView={true} />}
                </div>
              )}

              {/* Empty State */}
              {preferredItems.length === 0 && otherItems.length === 0 && !isLoading && (
                <div className="p-8 text-center">
                  <p className="text-gray-600">No events found.</p>
                  {isAuthenticated && (
                    <p className="text-sm text-gray-500 mt-2">Try updating your preferred metro areas in your profile!</p>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      </section>


      <Footer />
    </div>
  );
}

/**
 * Main Home Component wrapped with MetroAreaProvider
 */
export default function Home() {
  return (
    <MetroAreaProvider>
      <HomeContent />
    </MetroAreaProvider>
  );
}
