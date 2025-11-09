'use client';

import * as React from 'react';
import { Button } from '@/presentation/components/ui/Button';
import { StatCard } from '@/presentation/components/ui/StatCard';
import { Logo } from '@/presentation/components/atoms/Logo';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useRouter } from 'next/navigation';
import { Users, Calendar, Building2, Heart, MessageSquare, TrendingUp, MapPin, ThumbsUp } from 'lucide-react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { FeedTabs, ActivityFeed } from '@/presentation/components/features/feed';
import { MetroAreaProvider, useMetroArea } from '@/presentation/components/features/location/MetroAreaContext';
import { MetroAreaSelector } from '@/presentation/components/features/location/MetroAreaSelector';
import { mockFeedItems } from '@/domain/data/mockFeedData';
import { US_METRO_AREAS } from '@/domain/constants/metroAreas.constants';
import type { FeedItem } from '@/domain/models/FeedItem';
import type { MetroArea } from '@/domain/models/MetroArea';

/**
 * Landing Page Component
 * Public landing page showcasing LankaConnect platform with Sri Lankan flag colors
 * Features: Flag header, sticky navbar, hero section, community stats, activity feed
 */

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
    setAvailableMetros(US_METRO_AREAS);
  }, [setAvailableMetros]);

  const handleChange = (metroId: string | null) => {
    if (!metroId) {
      setMetroArea(null);
    } else {
      const metro = US_METRO_AREAS.find(m => m.id === metroId);
      setMetroArea(metro || null);
    }
  };

  return (
    <div className="w-full max-w-md">
      <MetroAreaSelector
        value={selectedMetroArea?.id || null}
        metros={US_METRO_AREAS}
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
 */
function HomeContent() {
  const { selectedMetroArea } = useMetroArea();
  const [activeTab, setActiveTab] = React.useState<'all' | 'event' | 'business' | 'forum' | 'culture'>('all');

  // Filter feed items by selected metro area and active tab
  const filteredItems = React.useMemo((): FeedItem[] => {
    let items = mockFeedItems;

    // Filter by metro area
    if (selectedMetroArea) {
      items = items.filter(item => {
        // Check if item location matches any city in the metro area
        const itemCity = item.location.split(',')[0].trim();
        return selectedMetroArea.cities.some(city => city === itemCity) ||
               item.location.includes(selectedMetroArea.state);
      });
    }

    // Filter by tab
    if (activeTab !== 'all') {
      items = items.filter(item => item.type === activeTab);
    }

    return items;
  }, [selectedMetroArea, activeTab]);

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

            {/* Activity Feed - 2 Column Grid */}
            <div className="bg-white rounded-b-xl shadow-[0_2px_15px_rgba(0,0,0,0.08)] overflow-hidden">
              <ActivityFeed items={filteredItems} loading={false} gridView={true} />
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
