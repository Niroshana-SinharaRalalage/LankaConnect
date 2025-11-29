'use client';

import Link from 'next/link';
import Image from 'next/image';
import { ProtectedRoute } from '@/presentation/components/auth/ProtectedRoute';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { Button } from '@/presentation/components/ui/Button';
import { TabPanel } from '@/presentation/components/ui/TabPanel';
import { OfficialLogo } from '@/presentation/components/atoms/OfficialLogo';
import Footer from '@/presentation/components/layout/Footer';
import { useRouter } from 'next/navigation';
import { authRepository } from '@/infrastructure/api/repositories/auth.repository';
import { canCreateEvents, isAdmin } from '@/infrastructure/api/utils/role-helpers';
import { UserRole } from '@/infrastructure/api/types/auth.types';
import {
  Calendar,
  Users,
  ChevronDown,
  User,
  LogOut,
  Sparkles,
  ClipboardCheck,
  FolderOpen,
  Bell
} from 'lucide-react';
import { useState, useRef, useEffect } from 'react';
import { EventsList } from '@/presentation/components/features/dashboard/EventsList';
import { ApprovalsTable } from '@/presentation/components/features/admin/ApprovalsTable';
import { UpgradeModal } from '@/presentation/components/features/role-upgrade/UpgradeModal';
import { UpgradePendingBanner } from '@/presentation/components/features/role-upgrade/UpgradePendingBanner';
import { NotificationsList } from '@/presentation/components/features/dashboard/NotificationsList';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import { approvalsRepository } from '@/infrastructure/api/repositories/approvals.repository';
import { useUnreadNotifications, useMarkNotificationAsRead } from '@/presentation/hooks/useNotifications';
import type { EventDto } from '@/infrastructure/api/types/events.types';
import type { PendingRoleUpgradeDto } from '@/infrastructure/api/types/approvals.types';

export default function DashboardPage() {
  const router = useRouter();
  const { user, clearAuth } = useAuthStore();
  const [showUserMenu, setShowUserMenu] = useState<boolean>(false);
  const [mounted, setMounted] = useState<boolean>(false);
  const [showUpgradeModal, setShowUpgradeModal] = useState<boolean>(false);
  const userMenuRef = useRef<HTMLDivElement>(null);

  // State for events
  const [registeredEvents, setRegisteredEvents] = useState<EventDto[]>([]);
  const [createdEvents, setCreatedEvents] = useState<EventDto[]>([]);
  const [loadingRegistered, setLoadingRegistered] = useState(false);
  const [loadingCreated, setLoadingCreated] = useState(false);

  // State for admin approvals
  const [pendingApprovals, setPendingApprovals] = useState<PendingRoleUpgradeDto[]>([]);
  const [loadingApprovals, setLoadingApprovals] = useState(false);

  // Notifications
  const { data: notifications = [], isLoading: loadingNotifications } = useUnreadNotifications();
  const markAsRead = useMarkNotificationAsRead();

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

  // Load registered events
  useEffect(() => {
    const loadRegisteredEvents = async () => {
      try {
        setLoadingRegistered(true);
        // Epic 1: Backend now returns full EventDto[] instead of RsvpDto[]
        const events = await eventsRepository.getUserRsvps();
        setRegisteredEvents(events);
      } catch (error) {
        console.error('Error loading registered events:', error);
      } finally {
        setLoadingRegistered(false);
      }
    };

    if (user) {
      loadRegisteredEvents();
    }
  }, [user]);

  // Load created events (for Event Organizers and Admins)
  useEffect(() => {
    const loadCreatedEvents = async () => {
      try {
        setLoadingCreated(true);
        const events = await eventsRepository.getUserCreatedEvents();
        setCreatedEvents(events);
      } catch (error) {
        console.error('Error loading created events:', error);
      } finally {
        setLoadingCreated(false);
      }
    };

    if (user && (user.role === UserRole.EventOrganizer || isAdmin(user.role as UserRole))) {
      loadCreatedEvents();
    }
  }, [user]);

  // Load pending approvals (for Admins only)
  useEffect(() => {
    const loadApprovals = async () => {
      try {
        setLoadingApprovals(true);
        const approvals = await approvalsRepository.getPendingApprovals();
        setPendingApprovals(approvals);
      } catch (error) {
        console.error('Error loading approvals:', error);
      } finally {
        setLoadingApprovals(false);
      }
    };

    if (user && isAdmin(user.role as UserRole)) {
      loadApprovals();
    }
  }, [user]);

  const handleApprovalsUpdate = async () => {
    try {
      const approvals = await approvalsRepository.getPendingApprovals();
      setPendingApprovals(approvals);
    } catch (error) {
      console.error('Error refreshing approvals:', error);
    }
  };

  const handleNotificationClick = async (notificationId: string) => {
    try {
      await markAsRead.mutateAsync(notificationId);
    } catch (error) {
      console.error('Failed to mark notification as read:', error);
    }
  };

  const handleEventClick = (eventId: string) => {
    router.push(`/events/${eventId}`);
  };

  const handleManageEventClick = (eventId: string) => {
    router.push(`/events/${eventId}/manage`);
  };

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
                <OfficialLogo size="md" />

                {/* Navigation Links */}
                <nav className="hidden md:flex items-center gap-6">
                  <a href="/events" className="text-[#333] hover:text-[#FF7900] font-medium transition-colors">
                    Events
                  </a>
                  <a href="/forums" className="text-[#333] hover:text-[#FF7900] font-medium transition-colors">
                    Forums
                  </a>
                  <a href="/business" className="text-[#333] hover:text-[#FF7900] font-medium transition-colors">
                    Business
                  </a>
                  <a href="/marketplace" className="text-[#333] hover:text-[#FF7900] font-medium transition-colors">
                    Marketplace
                  </a>
                  <a href="/dashboard" className="text-[#FF7900] font-medium transition-colors">
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
                      {user?.profilePhotoUrl ? (
                        <Image
                          src={user.profilePhotoUrl}
                          alt={user.fullName}
                          width={40}
                          height={40}
                          className="w-10 h-10 rounded-full object-cover"
                        />
                      ) : (
                        <div
                          className="w-10 h-10 rounded-full flex items-center justify-center text-white font-semibold"
                          style={{ background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)' }}
                        >
                          {getInitials(user?.fullName || 'U')}
                        </div>
                      )}
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
          {/* Phase 6A.7: Upgrade Pending Banner - Show if user has pending upgrade */}
          {user?.pendingUpgradeRole && (
            <UpgradePendingBanner
              upgradeRequestedAt={user.upgradeRequestedAt ? new Date(user.upgradeRequestedAt) : undefined}
            />
          )}

          {/* Quick Actions - Epic 1: Role-based visibility, Post Topic removed */}
          <div className="mb-8">
            <div className="flex flex-col sm:flex-row gap-3">
              {/* Show 'Upgrade to Event Organizer' button for GeneralUser (if not already pending) */}
              {user && user.role === UserRole.GeneralUser && !user.pendingUpgradeRole && (
                <Button
                  onClick={() => setShowUpgradeModal(true)}
                  className="flex-1 sm:flex-none rounded-lg"
                  style={{
                    background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)',
                    color: 'white',
                    transition: 'all 0.3s'
                  }}
                  variant="default"
                >
                  <Sparkles className="w-4 h-4 mr-2" />
                  Upgrade to Event Organizer
                </Button>
              )}
              {/* Show 'Create Event' for EventOrganizer, Admin, and AdminManager */}
              {user && (user.role === UserRole.EventOrganizer || user.role === UserRole.Admin || user.role === UserRole.AdminManager) && (
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
              )}
              {/* Post Topic button removed per Epic 1 requirements */}
            </div>
          </div>

          {/* Epic 1: Tabbed Dashboard Content - Full width layout (Culture Calendar & Featured Businesses removed) */}
          <div>
            {/* Dashboard Content - Full width */}
            <div>
              <div className="bg-white rounded-xl shadow-sm">
                {/* Render tabs based on user role */}
                {user && isAdmin(user.role as UserRole) ? (
                  <TabPanel
                    tabs={[
                      {
                        id: 'registered',
                        label: 'My Registered Events',
                        icon: Users,
                        content: (
                          <EventsList
                            events={registeredEvents}
                            isLoading={loadingRegistered}
                            emptyMessage="You haven't registered for any events yet"
                            onEventClick={handleEventClick}
                          />
                        ),
                      },
                      {
                        id: 'created',
                        label: 'My Created Events',
                        icon: FolderOpen,
                        content: (
                          <EventsList
                            events={createdEvents}
                            isLoading={loadingCreated}
                            emptyMessage="You haven't created any events yet"
                            onEventClick={handleManageEventClick}
                          />
                        ),
                      },
                      {
                        id: 'admin',
                        label: 'Admin Tasks',
                        icon: ClipboardCheck,
                        content: (
                          <div>
                            <h3 className="text-lg font-semibold mb-4 text-[#8B1538]">
                              Pending Approvals
                            </h3>
                            {loadingApprovals ? (
                              <div className="text-center py-8">
                                <p className="text-gray-600">Loading approvals...</p>
                              </div>
                            ) : (
                              <ApprovalsTable approvals={pendingApprovals} onUpdate={handleApprovalsUpdate} />
                            )}
                          </div>
                        ),
                      },
                      {
                        id: 'notifications',
                        label: 'Notifications',
                        icon: Bell,
                        content: (
                          <NotificationsList
                            notifications={notifications}
                            isLoading={loadingNotifications}
                            onNotificationClick={handleNotificationClick}
                          />
                        ),
                      },
                    ]}
                  />
                ) : user && user.role === UserRole.EventOrganizer ? (
                  <TabPanel
                    tabs={[
                      {
                        id: 'registered',
                        label: 'My Registered Events',
                        icon: Users,
                        content: (
                          <EventsList
                            events={registeredEvents}
                            isLoading={loadingRegistered}
                            emptyMessage="You haven't registered for any events yet"
                            onEventClick={handleEventClick}
                          />
                        ),
                      },
                      {
                        id: 'created',
                        label: 'My Created Events',
                        icon: FolderOpen,
                        content: (
                          <EventsList
                            events={createdEvents}
                            isLoading={loadingCreated}
                            emptyMessage="You haven't created any events yet"
                            onEventClick={handleManageEventClick}
                          />
                        ),
                      },
                      {
                        id: 'notifications',
                        label: 'Notifications',
                        icon: Bell,
                        content: (
                          <NotificationsList
                            notifications={notifications}
                            isLoading={loadingNotifications}
                            onNotificationClick={handleNotificationClick}
                          />
                        ),
                      },
                    ]}
                  />
                ) : (
                  /* General User - Tabbed interface with Registered Events and Notifications */
                  <TabPanel
                    tabs={[
                      {
                        id: 'registered',
                        label: 'My Registered Events',
                        icon: Users,
                        content: (
                          <EventsList
                            events={registeredEvents}
                            isLoading={loadingRegistered}
                            emptyMessage="You haven't registered for any events yet. Browse events to join!"
                            onEventClick={handleEventClick}
                          />
                        ),
                      },
                      {
                        id: 'notifications',
                        label: 'Notifications',
                        icon: Bell,
                        content: (
                          <NotificationsList
                            notifications={notifications}
                            isLoading={loadingNotifications}
                            onNotificationClick={handleNotificationClick}
                          />
                        ),
                      },
                    ]}
                  />
                )}
              </div>
            </div>
          </div>
        </main>

        {/* Phase 6A.2.4: Add Footer component */}
        <Footer />

        {/* Phase 6A.7: Upgrade Modal */}
        <UpgradeModal isOpen={showUpgradeModal} onClose={() => setShowUpgradeModal(false)} />
      </div>
    </ProtectedRoute>
  );
}
