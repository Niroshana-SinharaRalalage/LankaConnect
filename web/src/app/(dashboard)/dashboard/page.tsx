'use client';

import { ProtectedRoute } from '@/presentation/components/auth/ProtectedRoute';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { Button } from '@/presentation/components/ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '@/presentation/components/ui/Card';
import { Logo } from '@/presentation/components/atoms/Logo';
import { useRouter } from 'next/navigation';
import { authRepository } from '@/infrastructure/api/repositories/auth.repository';
import {
  Bell,
  Calendar,
  MapPin,
  MessageSquare,
  Store,
  Users,
  Heart,
  MessageCircle,
  Share2,
  ChevronDown,
  User,
  LogOut
} from 'lucide-react';
import { useState, useRef, useEffect } from 'react';
import { CulturalCalendar } from '@/presentation/components/features/dashboard/CulturalCalendar';
import { FeaturedBusinesses } from '@/presentation/components/features/dashboard/FeaturedBusinesses';
import { CommunityStats } from '@/presentation/components/features/dashboard/CommunityStats';
import type { CulturalEvent } from '@/presentation/components/features/dashboard/CulturalCalendar';
import type { Business } from '@/presentation/components/features/dashboard/FeaturedBusinesses';
import type { CommunityStatsData } from '@/presentation/components/features/dashboard/CommunityStats';

// Mock data for widgets
const MOCK_CULTURAL_EVENTS: CulturalEvent[] = [
  {
    id: '1',
    name: 'Vesak Day Celebration',
    date: '2025-05-23',
    category: 'religious'
  },
  {
    id: '2',
    name: 'Sri Lankan Independence Day',
    date: '2025-02-04',
    category: 'national'
  },
  {
    id: '3',
    name: 'Sinhala & Tamil New Year',
    date: '2025-04-14',
    category: 'cultural'
  },
  {
    id: '4',
    name: 'Poson Poya Day',
    date: '2025-06-21',
    category: 'holiday'
  }
];

const MOCK_BUSINESSES: Business[] = [
  {
    id: '1',
    name: 'Ceylon Spice Market',
    category: 'Grocery',
    rating: 4.8,
    reviewCount: 125
  },
  {
    id: '2',
    name: 'Lanka Restaurant',
    category: 'Restaurant',
    rating: 4.6,
    reviewCount: 89
  },
  {
    id: '3',
    name: 'Serendib Boutique',
    category: 'Retail',
    rating: 4.9,
    reviewCount: 203
  }
];

const MOCK_COMMUNITY_STATS: CommunityStatsData = {
  activeUsers: 12500,
  activeUsersTrend: { value: '+8.5%', direction: 'up' },
  recentPosts: 450,
  recentPostsTrend: { value: '+12.3%', direction: 'up' },
  upcomingEvents: 2200,
  upcomingEventsTrend: { value: '+5.7%', direction: 'up' }
};

// Mock activity data
interface ActivityItem {
  id: string;
  type: 'event' | 'post' | 'business';
  author: {
    name: string;
    avatar: string;
  };
  content: string;
  location: string;
  timestamp: string;
  likes: number;
  comments: number;
  image?: string;
}

const MOCK_ACTIVITIES: ActivityItem[] = [
  {
    id: '1',
    type: 'event',
    author: { name: 'Priya Perera', avatar: 'PP' },
    content: 'Organizing a traditional Sri Lankan New Year celebration in Toronto! Join us for games, food, and cultural performances.',
    location: 'Toronto, ON',
    timestamp: '2 hours ago',
    likes: 24,
    comments: 8,
    image: undefined
  },
  {
    id: '2',
    type: 'post',
    author: { name: 'Raj Fernando', avatar: 'RF' },
    content: 'Just discovered an amazing Sri Lankan grocery store in Vancouver! They have fresh hoppers and all the spices you need.',
    location: 'Vancouver, BC',
    timestamp: '4 hours ago',
    likes: 15,
    comments: 5
  },
  {
    id: '3',
    type: 'business',
    author: { name: 'Lanka Restaurant', avatar: 'LR' },
    content: 'New menu items available! Try our authentic kottu roti and string hoppers. Special discount for community members this week.',
    location: 'Montreal, QC',
    timestamp: '6 hours ago',
    likes: 42,
    comments: 12
  }
];

export default function DashboardPage() {
  const router = useRouter();
  const { user, clearAuth } = useAuthStore();
  const [selectedLocation, setSelectedLocation] = useState<string>('All Locations');
  const [showUserMenu, setShowUserMenu] = useState<boolean>(false);
  const [mounted, setMounted] = useState<boolean>(false);
  const userMenuRef = useRef<HTMLDivElement>(null);

  // Set mounted state after client-side hydration
  useEffect(() => {
    setMounted(true);
  }, []);

  // Close dropdown when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (userMenuRef.current && !userMenuRef.current.contains(event.target as Node)) {
        setShowUserMenu(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleLogout = async () => {
    try {
      await authRepository.logout();
    } catch (error) {
      // Silently handle logout errors (e.g., 401 when already logged out)
      // The error is expected and clearAuth will handle cleanup
    } finally {
      clearAuth();
      router.push('/login');
    }
  };

  const getInitials = (name: string) => {
    return name
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase();
  };

  return (
    <ProtectedRoute>
      <div className="min-h-screen" style={{ background: '#f7fafc' }}>
        {/* Header - Matching Landing Page */}
        <header className="bg-white sticky top-0 z-40" style={{
          background: 'rgba(255, 255, 255, 0.95)',
          backdropFilter: 'blur(10px)',
          boxShadow: '0 2px 20px rgba(0,0,0,0.1)'
        }}>
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
            <div className="flex items-center justify-between">
              {/* Logo + Navigation */}
              <div className="flex items-center gap-8">
                <div className="flex items-center">
                  <Logo size="md" showText={false} />
                  <span className="ml-3 text-2xl font-bold text-[#8B1538]">LankaConnect</span>
                </div>

                {/* Navigation Links */}
                <nav className="hidden md:flex items-center gap-6">
                  <a href="/" className="text-[#333] hover:text-[#FF7900] font-medium transition-colors">
                    Home
                  </a>
                  <a href="#events" className="text-[#333] hover:text-[#FF7900] font-medium transition-colors">
                    Events
                  </a>
                  <a href="#forums" className="text-[#333] hover:text-[#FF7900] font-medium transition-colors">
                    Forums
                  </a>
                  <a href="#business" className="text-[#333] hover:text-[#FF7900] font-medium transition-colors">
                    Business
                  </a>
                  <a href="#culture" className="text-[#333] hover:text-[#FF7900] font-medium transition-colors">
                    Culture
                  </a>
                  <a href="/dashboard" className="text-[#FF7900] hover:text-[#FF7900] font-medium transition-colors">
                    Dashboard
                  </a>
                </nav>
              </div>

              <div className="flex items-center gap-4" suppressHydrationWarning>
                {/* User Menu Dropdown */}
                {mounted && (
                  <div className="relative" ref={userMenuRef}>
                    <button
                      onClick={() => setShowUserMenu(!showUserMenu)}
                      className="flex items-center gap-3 hover:bg-gray-50 rounded-lg p-2 transition-colors"
                    >
                      <div
                        className="w-10 h-10 rounded-full flex items-center justify-center text-white font-semibold"
                        style={{ background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)' }}
                      >
                        {getInitials(user?.fullName || 'U')}
                      </div>
                      <div className="hidden md:block text-left">
                        <p className="text-sm font-medium" style={{ color: '#8B1538' }}>{user?.fullName}</p>
                        <p className="text-xs" style={{ color: '#718096' }}>{user?.role}</p>
                      </div>
                      <ChevronDown className={`w-4 h-4 text-gray-600 transition-transform ${showUserMenu ? 'rotate-180' : ''}`} />
                    </button>

                    {/* Dropdown Menu - Only render after client-side hydration */}
                    {showUserMenu && (
                      <div
                        className="absolute right-0 mt-2 w-48 rounded-lg shadow-lg overflow-hidden z-50"
                        style={{
                          background: 'white',
                          border: '1px solid #e2e8f0'
                        }}
                      >
                        <button
                          onClick={() => {
                            setShowUserMenu(false);
                            router.push('/profile');
                          }}
                          className="w-full flex items-center gap-3 px-4 py-3 hover:bg-gray-50 transition-colors text-left"
                        >
                          <User className="w-4 h-4" style={{ color: '#FF7900' }} />
                          <span style={{ color: '#2d3748' }}>Profile</span>
                        </button>
                        <div style={{ borderTop: '1px solid #e2e8f0' }}></div>
                        <button
                          onClick={() => {
                            setShowUserMenu(false);
                            handleLogout();
                          }}
                          className="w-full flex items-center gap-3 px-4 py-3 hover:bg-gray-50 transition-colors text-left"
                        >
                          <LogOut className="w-4 h-4" style={{ color: '#8B1538' }} />
                          <span style={{ color: '#2d3748' }}>Logout</span>
                        </button>
                      </div>
                    )}
                  </div>
                )}
              </div>
            </div>
          </div>
        </header>

        <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          {/* Quick Actions */}
          <div className="mb-8">
            <div className="flex flex-col sm:flex-row gap-3">
              <Button
                onClick={() => router.push('/events/create')}
                className="flex-1 sm:flex-none rounded-lg"
                style={{
                  background: '#FF7900',
                  color: 'white',
                  transition: 'all 0.3s'
                }}
                variant="default"
              >
                <Calendar className="w-4 h-4 mr-2" />
                Create Event
              </Button>
              <Button
                onClick={() => router.push('/forum/new')}
                className="flex-1 sm:flex-none rounded-lg"
                variant="outline"
                style={{
                  background: 'white',
                  borderColor: '#FF7900',
                  color: '#FF7900'
                }}
              >
                <MessageSquare className="w-4 h-4 mr-2" />
                Post Topic
              </Button>
              <Button
                onClick={() => router.push('/businesses')}
                className="flex-1 sm:flex-none rounded-lg"
                variant="outline"
                style={{
                  background: 'white',
                  borderColor: '#FF7900',
                  color: '#FF7900'
                }}
              >
                <Store className="w-4 h-4 mr-2" />
                Find Business
              </Button>
            </div>
          </div>

          {/* Two Column Layout */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
            {/* Left Column - Activity Feed (2/3 width on large screens) */}
            <div className="lg:col-span-2 space-y-0">
              {/* Activity Feed Container */}
              <div
                className="rounded-xl overflow-hidden"
                style={{
                  background: 'white',
                  boxShadow: '0 4px 6px rgba(0, 0, 0, 0.05)'
                }}
              >
                {/* Feed Header with Location Selector */}
                <div
                  className="p-6 flex justify-between items-center"
                  style={{
                    background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)',
                    color: 'white'
                  }}
                >
                  <h2 className="text-xl font-semibold">Community Activity</h2>
                  <select
                    className="px-4 py-2 rounded-md text-white text-sm outline-none cursor-pointer"
                    style={{
                      background: 'rgba(255,255,255,0.2)',
                      border: 'none'
                    }}
                    value={selectedLocation}
                    onChange={(e) => setSelectedLocation(e.target.value)}
                  >
                    <option style={{ color: '#2d3748' }}>All Locations</option>
                    <option style={{ color: '#2d3748' }}>Toronto, ON</option>
                    <option style={{ color: '#2d3748' }}>Vancouver, BC</option>
                    <option style={{ color: '#2d3748' }}>Montreal, QC</option>
                  </select>
                </div>

                {/* Feed Items */}
                {MOCK_ACTIVITIES.map((activity, index) => (
                  <div
                    key={activity.id}
                    className="p-6 transition-colors hover:bg-opacity-100"
                    style={{
                      borderBottom: index === MOCK_ACTIVITIES.length - 1 ? 'none' : '1px solid #e2e8f0',
                      background: 'white'
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.background = '#f8f9ff';
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.background = 'white';
                    }}
                  >
                    {/* Activity Header */}
                    <div className="flex items-center mb-3">
                      <div
                        className="w-10 h-10 rounded-full flex items-center justify-center text-white font-bold mr-3"
                        style={{ background: 'linear-gradient(135deg, #FF7900, #8B1538)' }}
                      >
                        {activity.author.avatar}
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="font-semibold" style={{ color: '#8B1538' }}>{activity.author.name}</p>
                        <p className="text-xs" style={{ color: '#718096' }}>
                          {activity.timestamp} • {activity.location}
                        </p>
                      </div>
                      <span
                        className="px-2 py-1 text-xs font-semibold rounded capitalize ml-auto"
                        style={{
                          background: activity.type === 'event' ? '#FFE8CC' :
                                     activity.type === 'post' ? '#FFDAB3' : '#D4EDDA',
                          color: activity.type === 'event' ? '#8B1538' :
                                 activity.type === 'post' ? '#FF7900' : '#006400'
                        }}
                      >
                        {activity.type}
                      </span>
                    </div>

                    {/* Activity Content */}
                    <div className="mb-4">
                      <h3 className="font-semibold mb-2" style={{ fontSize: '1.1rem', color: '#8B1538' }}>
                        {activity.type === 'event' ? 'Join Our Event' :
                         activity.type === 'business' ? 'Special Announcement' : 'Community Discussion'}
                      </h3>
                      <p style={{ color: '#718096', fontSize: '0.9rem', lineHeight: '1.5' }}>
                        {activity.content}
                      </p>
                    </div>

                    {/* Activity Actions */}
                    <div
                      className="flex gap-4 pt-4"
                      style={{ borderTop: '1px solid #f1f5f9' }}
                    >
                      <button
                        className="flex items-center gap-2 transition-colors text-sm"
                        style={{ color: '#718096' }}
                        onMouseEnter={(e) => {
                          e.currentTarget.style.color = '#FF7900';
                        }}
                        onMouseLeave={(e) => {
                          e.currentTarget.style.color = '#718096';
                        }}
                      >
                        <Heart className="w-4 h-4" />
                        <span>{activity.likes}</span>
                      </button>
                      <button
                        className="flex items-center gap-2 transition-colors text-sm"
                        style={{ color: '#718096' }}
                        onMouseEnter={(e) => {
                          e.currentTarget.style.color = '#FF7900';
                        }}
                        onMouseLeave={(e) => {
                          e.currentTarget.style.color = '#718096';
                        }}
                      >
                        <MessageCircle className="w-4 h-4" />
                        <span>{activity.comments}</span>
                      </button>
                      <button
                        className="flex items-center gap-2 transition-colors text-sm ml-auto"
                        style={{ color: '#718096' }}
                        onMouseEnter={(e) => {
                          e.currentTarget.style.color = '#FF7900';
                        }}
                        onMouseLeave={(e) => {
                          e.currentTarget.style.color = '#718096';
                        }}
                      >
                        <Share2 className="w-4 h-4" />
                        <span>Share</span>
                      </button>
                    </div>
                  </div>
                ))}
              </div>

              {/* Load More */}
              <div className="text-center pt-6">
                <Button
                  variant="outline"
                  className="w-full sm:w-auto"
                  style={{
                    borderColor: '#FF7900',
                    color: '#FF7900'
                  }}
                  onMouseEnter={(e) => {
                    e.currentTarget.style.background = '#FF7900';
                    e.currentTarget.style.color = 'white';
                  }}
                  onMouseLeave={(e) => {
                    e.currentTarget.style.background = 'transparent';
                    e.currentTarget.style.color = '#FF7900';
                  }}
                >
                  Load More Activities
                </Button>
              </div>
            </div>

            {/* Right Column - Widgets (1/3 width on large screens) */}
            <div className="space-y-8">
              {/* Cultural Calendar Widget */}
              <CulturalCalendar events={MOCK_CULTURAL_EVENTS} />

              {/* Featured Businesses Widget */}
              <FeaturedBusinesses businesses={MOCK_BUSINESSES} />

              {/* Community Stats Widget */}
              <CommunityStats stats={MOCK_COMMUNITY_STATS} />
            </div>
          </div>
        </main>
      </div>
    </ProtectedRoute>
  );
}
