'use client';

import { useEffect, useState, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useMyEvents, useDeleteEvent } from '@/presentation/hooks/useEvents';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { Calendar, MapPin, Users, DollarSign, Edit, Trash2, Plus, TrendingUp, CheckCircle, AlertCircle } from 'lucide-react';
import { EventCategory, EventStatus, type EventDto } from '@/infrastructure/api/types/events.types';

/**
 * Organizer Dashboard (My Events Page)
 * Allows organizers to view and manage all events they've created
 *
 * Features:
 * - Quick stats dashboard (total events, registrations, revenue)
 * - Event list with status indicators
 * - Edit/Delete functionality
 * - Filter by event status
 * - Responsive design
 */
export default function MyEventsPage() {
  const router = useRouter();
  const { user, isAuthenticated } = useAuthStore();
  const { data: events, isLoading, error } = useMyEvents();
  const deleteEventMutation = useDeleteEvent();

  const [statusFilter, setStatusFilter] = useState<EventStatus | 'all'>('all');
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);

  // Redirect to login if not authenticated
  useEffect(() => {
    if (!isAuthenticated || !user?.userId) {
      router.push('/login?redirect=' + encodeURIComponent('/events/my-events'));
    }
  }, [isAuthenticated, user, router]);

  // Filter events by status
  const filteredEvents = useMemo(() => {
    if (!events) return [];
    if (statusFilter === 'all') return events;
    return events.filter(event => event.status === statusFilter);
  }, [events, statusFilter]);

  // Calculate stats
  const stats = useMemo(() => {
    if (!events) return { totalEvents: 0, totalRegistrations: 0, totalRevenue: 0, upcomingEvents: 0 };

    const now = new Date();
    return {
      totalEvents: events.length,
      totalRegistrations: events.reduce((sum, e) => sum + e.currentRegistrations, 0),
      totalRevenue: events.reduce((sum, e) => {
        if (!e.isFree && e.ticketPriceAmount) {
          return sum + (e.ticketPriceAmount * e.currentRegistrations);
        }
        return sum;
      }, 0),
      upcomingEvents: events.filter(e => new Date(e.startDate) > now).length,
    };
  }, [events]);

  // Handle event deletion
  const handleDelete = async (eventId: string) => {
    try {
      await deleteEventMutation.mutateAsync(eventId);
      setDeleteConfirmId(null);
    } catch (err) {
      console.error('Failed to delete event:', err);
      alert('Failed to delete event. Please try again.');
    }
  };

  // Category labels
  const categoryLabels: Record<EventCategory, string> = {
    [EventCategory.Religious]: 'Religious',
    [EventCategory.Cultural]: 'Cultural',
    [EventCategory.Community]: 'Community',
    [EventCategory.Educational]: 'Educational',
    [EventCategory.Social]: 'Social',
    [EventCategory.Business]: 'Business',
    [EventCategory.Charity]: 'Charity',
    [EventCategory.Entertainment]: 'Entertainment',
  };

  // Status labels and colors
  const statusConfig: Record<EventStatus, { label: string; color: string }> = {
    [EventStatus.Draft]: { label: 'Draft', color: 'bg-neutral-500' },
    [EventStatus.Published]: { label: 'Published', color: 'bg-green-600' },
    [EventStatus.Active]: { label: 'Active', color: 'bg-blue-600' },
    [EventStatus.Postponed]: { label: 'Postponed', color: 'bg-yellow-600' },
    [EventStatus.Cancelled]: { label: 'Cancelled', color: 'bg-red-600' },
    [EventStatus.Completed]: { label: 'Completed', color: 'bg-purple-600' },
    [EventStatus.Archived]: { label: 'Archived', color: 'bg-neutral-400' },
    [EventStatus.UnderReview]: { label: 'Under Review', color: 'bg-orange-600' },
  };

  // Don't render until authentication is confirmed
  if (!isAuthenticated || !user?.userId) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <Header />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <div className="text-center">
            <p className="text-neutral-500">Redirecting to login...</p>
          </div>
        </div>
        <Footer />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      {/* Page Header */}
      <div className="bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 py-12 relative overflow-hidden">
        {/* Decorative Background Pattern */}
        <div className="absolute inset-0 opacity-10">
          <div
            className="absolute inset-0"
            style={{
              backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
            }}
          ></div>
        </div>

        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-4xl font-bold text-white mb-4">
                My Events
              </h1>
              <p className="text-lg text-white/90">
                Manage your events and track registrations
              </p>
            </div>
            <Button
              onClick={() => router.push('/events/create')}
              className="flex items-center gap-2"
              style={{ background: '#FF7900' }}
            >
              <Plus className="h-5 w-5" />
              Create New Event
            </Button>
          </div>
        </div>
      </div>

      {/* Stats Dashboard */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
          {/* Total Events */}
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-neutral-500">Total Events</p>
                  <p className="text-3xl font-bold text-neutral-900 mt-1">{stats.totalEvents}</p>
                </div>
                <div className="p-3 rounded-lg" style={{ background: '#FFF4ED' }}>
                  <Calendar className="h-6 w-6" style={{ color: '#FF7900' }} />
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Upcoming Events */}
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-neutral-500">Upcoming Events</p>
                  <p className="text-3xl font-bold text-neutral-900 mt-1">{stats.upcomingEvents}</p>
                </div>
                <div className="p-3 rounded-lg" style={{ background: '#FFF4ED' }}>
                  <TrendingUp className="h-6 w-6" style={{ color: '#FF7900' }} />
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Total Registrations */}
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-neutral-500">Total Registrations</p>
                  <p className="text-3xl font-bold text-neutral-900 mt-1">{stats.totalRegistrations}</p>
                </div>
                <div className="p-3 rounded-lg" style={{ background: '#FFF4ED' }}>
                  <Users className="h-6 w-6" style={{ color: '#FF7900' }} />
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Total Revenue */}
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-neutral-500">Total Revenue</p>
                  <p className="text-3xl font-bold text-neutral-900 mt-1">${stats.totalRevenue.toFixed(2)}</p>
                </div>
                <div className="p-3 rounded-lg" style={{ background: '#FFF4ED' }}>
                  <DollarSign className="h-6 w-6" style={{ color: '#FF7900' }} />
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Filter Section */}
        <Card className="mb-8">
          <CardHeader>
            <CardTitle style={{ color: '#8B1538' }}>Filter Events</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex gap-2 flex-wrap">
              <Button
                variant={statusFilter === 'all' ? 'default' : 'outline'}
                onClick={() => setStatusFilter('all')}
                style={statusFilter === 'all' ? { background: '#FF7900' } : {}}
              >
                All Events ({events?.length || 0})
              </Button>
              {Object.entries(statusConfig).map(([status, config]) => {
                const count = events?.filter(e => e.status === Number(status)).length || 0;
                return (
                  <Button
                    key={status}
                    variant={statusFilter === Number(status) ? 'default' : 'outline'}
                    onClick={() => setStatusFilter(Number(status) as EventStatus)}
                    style={statusFilter === Number(status) ? { background: '#FF7900' } : {}}
                  >
                    {config.label} ({count})
                  </Button>
                );
              })}
            </div>
          </CardContent>
        </Card>

        {/* Events List */}
        {isLoading ? (
          <div className="grid grid-cols-1 gap-6">
            {[...Array(3)].map((_, i) => (
              <Card key={i} className="animate-pulse">
                <CardContent className="p-6">
                  <div className="h-6 bg-neutral-200 rounded w-3/4 mb-4"></div>
                  <div className="h-4 bg-neutral-200 rounded w-1/2 mb-2"></div>
                  <div className="h-4 bg-neutral-200 rounded w-full"></div>
                </CardContent>
              </Card>
            ))}
          </div>
        ) : error ? (
          <Card>
            <CardContent className="p-12 text-center">
              <AlertCircle className="h-16 w-16 mx-auto mb-4 text-destructive" />
              <p className="text-destructive text-lg">
                Failed to load events. Please try again later.
              </p>
            </CardContent>
          </Card>
        ) : filteredEvents.length === 0 ? (
          <Card>
            <CardContent className="p-12 text-center">
              <Calendar className="h-16 w-16 mx-auto mb-4 text-neutral-400" />
              <h3 className="text-xl font-semibold text-neutral-900 mb-2">
                {statusFilter === 'all' ? 'No Events Yet' : 'No Events Found'}
              </h3>
              <p className="text-neutral-500 mb-6">
                {statusFilter === 'all'
                  ? "You haven't created any events yet. Create your first event to get started!"
                  : 'Try adjusting your filter to see more events.'}
              </p>
              {statusFilter === 'all' && (
                <Button
                  onClick={() => router.push('/events/create')}
                  style={{ background: '#FF7900' }}
                >
                  <Plus className="h-4 w-4 mr-2" />
                  Create Your First Event
                </Button>
              )}
            </CardContent>
          </Card>
        ) : (
          <div className="grid grid-cols-1 gap-6">
            {filteredEvents.map((event) => {
              const startDate = new Date(event.startDate);
              const formattedDate = startDate.toLocaleDateString('en-US', {
                month: 'long',
                day: 'numeric',
                year: 'numeric',
              });

              return (
                <Card key={event.id} className="hover:shadow-lg transition-shadow">
                  <CardContent className="p-6">
                    <div className="flex items-start justify-between gap-4">
                      {/* Event Info */}
                      <div className="flex-1">
                        <div className="flex items-center gap-3 mb-3">
                          <h3 className="text-xl font-semibold text-neutral-900">
                            {event.title}
                          </h3>
                          <Badge className={`${statusConfig[event.status].color} text-white`}>
                            {statusConfig[event.status].label}
                          </Badge>
                          <Badge
                            className="border-2"
                            style={{ borderColor: '#8B1538', color: '#8B1538', background: 'white' }}
                          >
                            {categoryLabels[event.category]}
                          </Badge>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 text-sm">
                          {/* Date */}
                          <div className="flex items-center gap-2 text-neutral-600">
                            <Calendar className="h-4 w-4" style={{ color: '#FF7900' }} />
                            <span>{formattedDate}</span>
                          </div>

                          {/* Location */}
                          {event.city && event.state && (
                            <div className="flex items-center gap-2 text-neutral-600">
                              <MapPin className="h-4 w-4" style={{ color: '#FF7900' }} />
                              <span>{event.city}, {event.state}</span>
                            </div>
                          )}

                          {/* Registrations */}
                          <div className="flex items-center gap-2 text-neutral-600">
                            <Users className="h-4 w-4" style={{ color: '#FF7900' }} />
                            <span>{event.currentRegistrations} / {event.capacity} registered</span>
                          </div>

                          {/* Pricing */}
                          <div className="flex items-center gap-2 text-neutral-600">
                            <DollarSign className="h-4 w-4" style={{ color: '#FF7900' }} />
                            <span>{event.isFree ? 'Free' : `$${event.ticketPriceAmount?.toFixed(2)}`}</span>
                          </div>
                        </div>
                      </div>

                      {/* Actions */}
                      <div className="flex flex-col gap-2">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => router.push(`/events/${event.id}`)}
                        >
                          View
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => router.push(`/events/${event.id}/edit`)}
                          className="flex items-center gap-2"
                        >
                          <Edit className="h-4 w-4" />
                          Edit
                        </Button>
                        {deleteConfirmId === event.id ? (
                          <>
                            <Button
                              variant="destructive"
                              size="sm"
                              onClick={() => handleDelete(event.id)}
                              disabled={deleteEventMutation.isPending}
                            >
                              Confirm Delete
                            </Button>
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => setDeleteConfirmId(null)}
                            >
                              Cancel
                            </Button>
                          </>
                        ) : (
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => setDeleteConfirmId(event.id)}
                            className="flex items-center gap-2 text-red-600 hover:text-red-700"
                          >
                            <Trash2 className="h-4 w-4" />
                            Delete
                          </Button>
                        )}
                      </div>
                    </div>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        )}
      </div>

      <Footer />
    </div>
  );
}
