'use client';

import { use, useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  ArrowLeft,
  Edit,
  Upload,
  Users,
  Calendar,
  MapPin,
  DollarSign,
  Eye,
  Settings,
  Image as ImageIcon,
  Video
} from 'lucide-react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { useEventById } from '@/presentation/hooks/useEvents';
import { SignUpManagementSection } from '@/presentation/components/features/events/SignUpManagementSection';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { EventCategory, EventStatus } from '@/infrastructure/api/types/events.types';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';

/**
 * Event Management Page
 * Organizer-only page for managing event details, publishing, editing, and uploading media
 *
 * Features:
 * - Event statistics (registrations, capacity, revenue)
 * - Publish/unpublish events
 * - Edit event details
 * - Upload images and videos
 * - Manage sign-up lists
 * - View public event page
 */
export default function EventManagePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const router = useRouter();
  const { user } = useAuthStore();
  const [isPublishing, setIsPublishing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedImage, setSelectedImage] = useState<File | null>(null);
  const [isUploading, setIsUploading] = useState(false);

  // Fetch event details
  const { data: event, isLoading, error: fetchError, refetch } = useEventById(id);

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

  // Status labels
  const statusLabels: Record<EventStatus, string> = {
    [EventStatus.Draft]: 'Draft',
    [EventStatus.Published]: 'Published',
    [EventStatus.Active]: 'Active',
    [EventStatus.Postponed]: 'Postponed',
    [EventStatus.Cancelled]: 'Cancelled',
    [EventStatus.Completed]: 'Completed',
    [EventStatus.Archived]: 'Archived',
    [EventStatus.UnderReview]: 'Under Review',
  };

  // Handle Publish Event
  const handlePublishEvent = async () => {
    if (!event || event.organizerId !== user?.userId) {
      return;
    }

    try {
      setIsPublishing(true);
      setError(null);
      await eventsRepository.publishEvent(id);
      setIsPublishing(false);
      // Refetch to show updated status
      await refetch();
    } catch (err) {
      console.error('Failed to publish event:', err);
      setError(err instanceof Error ? err.message : 'Failed to publish event. Please try again.');
      setIsPublishing(false);
    }
  };

  // Handle Image Upload
  const handleImageUpload = async () => {
    if (!selectedImage || !event) {
      return;
    }

    try {
      setIsUploading(true);
      setError(null);
      await eventsRepository.uploadEventImage(id, selectedImage);
      setSelectedImage(null);
      setIsUploading(false);
      // Refetch to show new image
      await refetch();
    } catch (err) {
      console.error('Failed to upload image:', err);
      setError(err instanceof Error ? err.message : 'Failed to upload image. Please try again.');
      setIsUploading(false);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <Header />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <Card className="animate-pulse">
            <CardContent className="p-12">
              <div className="h-8 bg-neutral-200 rounded w-3/4 mb-4"></div>
              <div className="h-4 bg-neutral-200 rounded w-1/2 mb-8"></div>
              <div className="h-64 bg-neutral-200 rounded"></div>
            </CardContent>
          </Card>
        </div>
        <Footer />
      </div>
    );
  }

  if (fetchError || !event) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <Header />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <Card>
            <CardContent className="p-12 text-center">
              <h2 className="text-2xl font-bold text-red-600 mb-4">Event Not Found</h2>
              <p className="text-neutral-600 mb-6">The event you're looking for doesn't exist or you don't have permission to manage it.</p>
              <Button onClick={() => router.push('/dashboard')}>
                Go to Dashboard
              </Button>
            </CardContent>
          </Card>
        </div>
        <Footer />
      </div>
    );
  }

  // Check if user is the organizer
  const isOrganizer = event.organizerId === user?.userId;

  if (!isOrganizer) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <Header />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <Card>
            <CardContent className="p-12 text-center">
              <h2 className="text-2xl font-bold text-red-600 mb-4">Access Denied</h2>
              <p className="text-neutral-600 mb-6">You don't have permission to manage this event.</p>
              <Button onClick={() => router.push(`/events/${id}`)}>
                View Event
              </Button>
            </CardContent>
          </Card>
        </div>
        <Footer />
      </div>
    );
  }

  const spotsLeft = event.capacity - event.currentRegistrations;
  const registrationPercentage = (event.currentRegistrations / event.capacity) * 100;

  // Debug logging
  console.log('Event status:', event.status, 'Type:', typeof event.status);
  console.log('EventStatus.Draft:', EventStatus.Draft, 'Type:', typeof EventStatus.Draft);
  console.log('Status === Draft:', event.status === EventStatus.Draft);
  console.log('Status === 0:', event.status === 0);
  console.log('Number(status) === 0:', Number(event.status) === 0);

  // Check if event is in Draft status (handle both number and string)
  const isDraft = Number(event.status) === EventStatus.Draft;
  console.log('isDraft:', isDraft);

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      {/* Page Header */}
      <div className="bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 py-8">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-white mb-2">Manage Event</h1>
              <p className="text-white/90">{event.title}</p>
            </div>
            <Badge
              className="text-sm px-4 py-2"
              style={{
                backgroundColor: event.status === EventStatus.Published ? '#10B981' : '#6B7280',
              }}
            >
              {statusLabels[event.status]}
            </Badge>
          </div>
        </div>
      </div>

      {/* Navigation Buttons */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        <div className="flex items-center justify-between gap-4 flex-wrap">
          <Button
            variant="outline"
            onClick={() => router.push('/dashboard')}
            className="flex items-center gap-2 text-neutral-900 border-neutral-300"
          >
            <ArrowLeft className="h-4 w-4" />
            Back to Dashboard
          </Button>

          <div className="flex items-center gap-3 flex-wrap">
            {/* Publish/Unpublish Button - Show for Draft events */}
            {isDraft && (
              <Button
                onClick={handlePublishEvent}
                disabled={isPublishing}
                className="flex items-center gap-2 text-white"
                style={{ background: '#10B981', color: 'white' }}
              >
                {isPublishing ? 'Publishing...' : 'Publish Event'}
              </Button>
            )}

            {/* Edit Event Button - Temporarily disabled until page is created */}
            <Button
              onClick={() => alert('Edit Event page is not yet implemented. Please create /events/[id]/edit page.')}
              className="flex items-center gap-2 text-white"
              style={{ background: '#FF7900', color: 'white' }}
            >
              <Edit className="h-4 w-4" />
              Edit Event
            </Button>
          </div>
        </div>
      </div>

      {/* Error Message */}
      {error && (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mb-6">
          <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
            <p className="text-sm text-red-600">{error}</p>
          </div>
        </div>
      )}

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pb-12">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Left Column - Event Info & Stats */}
          <div className="lg:col-span-2 space-y-6">
            {/* Event Statistics */}
            <Card>
              <CardHeader>
                <CardTitle style={{ color: '#8B1538' }}>Event Statistics</CardTitle>
                <CardDescription>Track your event's performance</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  {/* Registrations */}
                  <div className="p-4 bg-blue-50 rounded-lg border border-blue-200">
                    <div className="flex items-center justify-between mb-2">
                      <Users className="h-5 w-5 text-blue-600" />
                      <span className="text-2xl font-bold text-blue-900">
                        {event.currentRegistrations}
                      </span>
                    </div>
                    <p className="text-sm text-blue-700 font-medium">Registered</p>
                    <p className="text-xs text-blue-600 mt-1">
                      {registrationPercentage.toFixed(0)}% of capacity
                    </p>
                  </div>

                  {/* Available Spots */}
                  <div className="p-4 bg-green-50 rounded-lg border border-green-200">
                    <div className="flex items-center justify-between mb-2">
                      <Users className="h-5 w-5 text-green-600" />
                      <span className="text-2xl font-bold text-green-900">
                        {spotsLeft}
                      </span>
                    </div>
                    <p className="text-sm text-green-700 font-medium">Spots Left</p>
                    <p className="text-xs text-green-600 mt-1">
                      Total capacity: {event.capacity}
                    </p>
                  </div>

                  {/* Revenue (if paid event) */}
                  {!event.isFree && event.ticketPriceAmount && (
                    <div className="p-4 bg-orange-50 rounded-lg border border-orange-200">
                      <div className="flex items-center justify-between mb-2">
                        <DollarSign className="h-5 w-5 text-orange-600" />
                        <span className="text-2xl font-bold text-orange-900">
                          ${(event.ticketPriceAmount * event.currentRegistrations).toFixed(2)}
                        </span>
                      </div>
                      <p className="text-sm text-orange-700 font-medium">Revenue</p>
                      <p className="text-xs text-orange-600 mt-1">
                        ${event.ticketPriceAmount} per ticket
                      </p>
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>

            {/* Event Details */}
            <Card>
              <CardHeader>
                <CardTitle style={{ color: '#8B1538' }}>Event Details</CardTitle>
                <CardDescription>Your event information</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                {/* Event Title */}
                <div>
                  <h4 className="text-sm font-semibold text-neutral-700 mb-2">Event Title</h4>
                  <p className="text-lg font-semibold text-neutral-900">{event.title}</p>
                </div>

                {/* Description */}
                <div>
                  <h4 className="text-sm font-semibold text-neutral-700 mb-2">Description</h4>
                  <p className="text-neutral-600">{event.description}</p>
                </div>

                {/* Date & Time */}
                <div className="flex items-start gap-3">
                  <Calendar className="h-5 w-5 text-[#FF7900] mt-0.5" />
                  <div>
                    <h4 className="text-sm font-semibold text-neutral-700">Date & Time</h4>
                    <p className="text-neutral-600">
                      {new Date(event.startDate).toLocaleString('en-US', {
                        dateStyle: 'full',
                        timeStyle: 'short',
                      })}
                    </p>
                    {event.endDate && (
                      <p className="text-sm text-neutral-500">
                        Ends: {new Date(event.endDate).toLocaleString('en-US', {
                          dateStyle: 'full',
                          timeStyle: 'short',
                        })}
                      </p>
                    )}
                  </div>
                </div>

                {/* Location */}
                {event.city && (
                  <div className="flex items-start gap-3">
                    <MapPin className="h-5 w-5 text-[#FF7900] mt-0.5" />
                    <div>
                      <h4 className="text-sm font-semibold text-neutral-700">Location</h4>
                      <p className="text-neutral-600">
                        {event.address && `${event.address}, `}
                        {event.city}, {event.state} {event.zipCode}
                      </p>
                    </div>
                  </div>
                )}

                {/* Category */}
                <div>
                  <h4 className="text-sm font-semibold text-neutral-700 mb-2">Category</h4>
                  <Badge className="bg-gray-100 text-gray-700">
                    {categoryLabels[event.category]}
                  </Badge>
                </div>

                {/* Pricing */}
                <div>
                  <h4 className="text-sm font-semibold text-neutral-700 mb-2">Pricing</h4>
                  {event.isFree ? (
                    <Badge className="bg-green-100 text-green-700">Free Event</Badge>
                  ) : (
                    <Badge className="bg-[#FFE8CC] text-[#8B1538]">
                      ${event.ticketPriceAmount} per ticket
                    </Badge>
                  )}
                </div>
              </CardContent>
            </Card>

            {/* Sign-up Management */}
            <SignUpManagementSection eventId={id} isOrganizer={true} />
          </div>

          {/* Right Column - Media & Actions */}
          <div className="space-y-6">
            {/* Image Upload */}
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <ImageIcon className="h-5 w-5" style={{ color: '#FF7900' }} />
                  <CardTitle style={{ color: '#8B1538' }}>Event Images</CardTitle>
                </div>
                <CardDescription>Upload photos to attract attendees</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                {/* Current Images */}
                {event.images && event.images.length > 0 && (
                  <div className="space-y-2">
                    <h4 className="text-sm font-semibold text-neutral-700">Current Images ({event.images.length})</h4>
                    <div className="grid grid-cols-2 gap-2">
                      {event.images.slice(0, 4).map((image) => (
                        <div key={image.id} className="relative aspect-square">
                          <img
                            src={image.imageUrl}
                            alt="Event"
                            className="w-full h-full object-cover rounded-lg"
                          />
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {/* Upload Form */}
                <div>
                  <label
                    htmlFor="image-upload"
                    className="block text-sm font-medium text-neutral-700 mb-2"
                  >
                    Upload New Image
                  </label>
                  <input
                    id="image-upload"
                    type="file"
                    accept="image/*"
                    onChange={(e) => setSelectedImage(e.target.files?.[0] || null)}
                    className="block w-full text-sm text-neutral-600 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-orange-50 file:text-orange-700 hover:file:bg-orange-100"
                  />
                </div>

                {selectedImage && (
                  <div className="p-3 bg-blue-50 rounded-lg border border-blue-200">
                    <p className="text-sm text-blue-700 font-medium mb-1">Selected:</p>
                    <p className="text-xs text-blue-600">{selectedImage.name}</p>
                  </div>
                )}

                <Button
                  onClick={handleImageUpload}
                  disabled={!selectedImage || isUploading}
                  className="w-full"
                  style={{ background: '#FF7900' }}
                >
                  <Upload className="h-4 w-4 mr-2" />
                  {isUploading ? 'Uploading...' : 'Upload Image'}
                </Button>
              </CardContent>
            </Card>

            {/* Quick Actions */}
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Settings className="h-5 w-5" style={{ color: '#FF7900' }} />
                  <CardTitle style={{ color: '#8B1538' }}>Quick Actions</CardTitle>
                </div>
              </CardHeader>
              <CardContent className="space-y-3">
                {/* Publish Event Button - Show for Draft events */}
                {isDraft && (
                  <Button
                    onClick={handlePublishEvent}
                    disabled={isPublishing}
                    className="w-full justify-start text-white"
                    style={{ background: '#10B981', color: 'white' }}
                  >
                    <Upload className="h-4 w-4 mr-2" />
                    {isPublishing ? 'Publishing...' : 'Publish Event'}
                  </Button>
                )}

                <Button
                  variant="outline"
                  className="w-full justify-start"
                  onClick={() => router.push(`/events/${id}/manage-signups`)}
                >
                  <Users className="h-4 w-4 mr-2" />
                  Manage Sign-up Lists
                </Button>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>

      <Footer />
    </div>
  );
}
