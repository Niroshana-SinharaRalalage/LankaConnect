/**
 * EventDetailsTab Component
 *
 * Phase 6A.45: Event details tab for manage page
 * Displays event information, statistics, and media galleries
 *
 * Extracted from original manage page to support tabbed layout
 */

'use client';

import React from 'react';
import { useRouter } from 'next/navigation';
import {
  Calendar,
  MapPin,
  DollarSign,
  Users,
  Image as ImageIcon,
  Video as VideoIcon,
  Mail,
  AlertTriangle,
} from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Badge } from '@/presentation/components/ui/Badge';
import { ImageUploader } from '@/presentation/components/features/events/ImageUploader';
import { VideoUploader } from '@/presentation/components/features/events/VideoUploader';
import { useEmailGroups } from '@/presentation/hooks/useEmailGroups';
import { type EventDto } from '@/infrastructure/api/types/events.types';

interface EventDetailsTabProps {
  event: EventDto;
  onRefetch: () => Promise<any>;
  isDraft: boolean;
  isPublished: boolean;
  isPublishing: boolean;
  isUnpublishing: boolean;
  onPublish: () => Promise<void>;
  onUnpublish: () => Promise<void>;
}

export function EventDetailsTab({
  event,
  onRefetch,
}: EventDetailsTabProps) {
  const router = useRouter();
  const { data: emailGroups = [] } = useEmailGroups();

  const spotsLeft = event.capacity - event.currentRegistrations;
  const registrationPercentage = (event.currentRegistrations / event.capacity) * 100;

  return (
    <div className="space-y-6">
      {/* Phase 6A.X: Table-Style Grid Layout for Better Readability */}

      {/* Statistics Section */}
      <Card>
        <CardHeader>
          <CardTitle style={{ color: '#8B1538' }}>Event Statistics</CardTitle>
          <CardDescription>Track your event's performance</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="p-4 bg-blue-50 rounded-lg border border-blue-200">
              <div className="flex items-center justify-between mb-2">
                <Users className="h-5 w-5 text-blue-600" />
                <span className="text-2xl font-bold text-blue-900">{event.currentRegistrations}</span>
              </div>
              <p className="text-sm text-blue-700 font-medium">Registered</p>
              <p className="text-xs text-blue-600 mt-1">{registrationPercentage.toFixed(0)}% of capacity</p>
            </div>
            <div className="p-4 bg-green-50 rounded-lg border border-green-200">
              <div className="flex items-center justify-between mb-2">
                <Users className="h-5 w-5 text-green-600" />
                <span className="text-2xl font-bold text-green-900">{spotsLeft}</span>
              </div>
              <p className="text-sm text-green-700 font-medium">Spots Left</p>
              <p className="text-xs text-green-600 mt-1">Total capacity: {event.capacity}</p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Event Details Section - Combined */}
      <Card>
        <CardHeader>
          <CardTitle style={{ color: '#8B1538' }}>Event Details</CardTitle>
          <CardDescription>Basic information, schedule, and location</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {/* Event Title */}
            <div className="grid grid-cols-[140px_1fr] gap-x-4 gap-y-3 border-b pb-3">
              <span className="text-sm font-semibold text-neutral-700">Event Title:</span>
              <span className="text-sm text-neutral-900 font-medium">{event.title}</span>
            </div>

            {/* Description */}
            <div className="grid grid-cols-[140px_1fr] gap-x-4 gap-y-3 border-b pb-3">
              <span className="text-sm font-semibold text-neutral-700">Description:</span>
              <span className="text-sm text-neutral-600">{event.description}</span>
            </div>

            {/* Start Date */}
            <div className="grid grid-cols-[140px_1fr] gap-x-4 items-center border-b pb-3">
              <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                <Calendar className="h-4 w-4 text-[#FF7900]" />
                Start Date:
              </span>
              <span className="text-sm text-neutral-900">
                {new Date(event.startDate).toLocaleString('en-US', {
                  dateStyle: 'full',
                  timeStyle: 'short',
                })}
              </span>
            </div>

            {/* End Date */}
            {event.endDate && (
              <div className="grid grid-cols-[140px_1fr] gap-x-4 items-center border-b pb-3">
                <span className="text-sm font-semibold text-neutral-700">End Date:</span>
                <span className="text-sm text-neutral-900">
                  {new Date(event.endDate).toLocaleString('en-US', {
                    dateStyle: 'full',
                    timeStyle: 'short',
                  })}
                </span>
              </div>
            )}

            {/* Location */}
            {event.city && (
              <div className="grid grid-cols-[140px_1fr] gap-x-4 items-center border-b pb-3">
                <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                  <MapPin className="h-4 w-4 text-[#FF7900]" />
                  Location:
                </span>
                <span className="text-sm text-neutral-900">
                  {event.address && `${event.address}, `}
                  {event.city}, {event.state} {event.zipCode}
                </span>
              </div>
            )}

            {/* Capacity */}
            <div className="grid grid-cols-[140px_1fr] gap-x-4 items-center">
              <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                <Users className="h-4 w-4 text-[#FF7900]" />
                Capacity:
              </span>
              <span className="text-sm text-neutral-900">{event.capacity} attendees</span>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Pricing Section - Table Grid */}
      <Card>
        <CardHeader>
          <CardTitle style={{ color: '#8B1538' }}>Pricing Information</CardTitle>
          <CardDescription>Ticket prices and payment details</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-[140px_1fr] gap-x-4 items-start">
            <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
              <DollarSign className="h-4 w-4 text-[#FF7900]" />
              Pricing:
            </span>
            <div>
              {event.isFree ? (
                <Badge className="bg-green-100 text-green-700">Free Event</Badge>
              ) : event.hasGroupPricing && event.groupPricingTiers && event.groupPricingTiers.length > 0 ? (
                <div className="space-y-2">
                  <Badge className="bg-purple-100 text-purple-700">Group Tiered Pricing</Badge>
                  <div className="flex flex-wrap items-center gap-1 text-sm mt-2">
                    {event.groupPricingTiers.map((tier, index) => (
                      <span key={index} className="inline-flex items-center">
                        <Badge className="bg-[#FFE8CC] text-[#8B1538]">
                          ${tier.pricePerPerson}/{tier.maxAttendees
                            ? (tier.minAttendees === tier.maxAttendees
                                ? `${tier.minAttendees} ${tier.minAttendees === 1 ? 'person' : 'persons'}`
                                : `${tier.minAttendees}-${tier.maxAttendees} persons`)
                            : `${tier.minAttendees}+ persons`}
                        </Badge>
                        {index < event.groupPricingTiers.length - 1 && (
                          <span className="mx-1 text-neutral-400">|</span>
                        )}
                      </span>
                    ))}
                  </div>
                </div>
              ) : event.hasDualPricing ? (
                <div className="flex flex-col gap-1">
                  <Badge className="bg-[#FFE8CC] text-[#8B1538]">Adult: ${event.adultPriceAmount}</Badge>
                  <Badge className="bg-[#FFE8CC] text-[#8B1538]">
                    Child: ${event.childPriceAmount} (under {event.childAgeLimit})
                  </Badge>
                </div>
              ) : event.ticketPriceAmount != null ? (
                <Badge className="bg-[#FFE8CC] text-[#8B1538]">${event.ticketPriceAmount} per ticket</Badge>
              ) : (
                <Badge className="bg-yellow-100 text-yellow-700">Paid Event</Badge>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Email Groups Section - Table Grid */}
      <Card>
        <CardHeader>
          <CardTitle style={{ color: '#8B1538' }}>Email Groups</CardTitle>
          <CardDescription>Newsletter groups assigned to this event</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-[140px_1fr] gap-x-4 items-start">
            <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
              <Mail className="h-4 w-4 text-[#FF7900]" />
              Groups:
            </span>
            <div>
              {event.emailGroupIds && event.emailGroupIds.length > 0 ? (
                <div className="flex flex-wrap gap-2">
                  {event.emailGroupIds.map((groupId) => {
                    const group = emailGroups.find(g => g.id === groupId);
                    return group ? (
                      <Badge key={groupId} className="bg-[#FFE8CC] text-[#8B1538]">
                        {group.name}
                      </Badge>
                    ) : (
                      <Badge key={groupId} className="bg-gray-100 text-gray-600">
                        Unknown Group
                      </Badge>
                    );
                  })}
                </div>
              ) : (
                <div className="flex items-start gap-3 p-3 bg-amber-50 border border-amber-200 rounded-lg">
                  <AlertTriangle className="h-5 w-5 text-amber-600 mt-0.5 flex-shrink-0" />
                  <div className="flex-1">
                    <p className="text-sm text-amber-700 mb-1">No email groups assigned</p>
                    <p className="text-xs text-amber-600">
                      <button
                        onClick={() => router.push(`/events/${event.id}/edit`)}
                        className="underline hover:text-amber-800 font-medium"
                      >
                        Edit event to add email groups
                      </button>
                    </p>
                  </div>
                </div>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Phase 6A.X: Organizer Contact Section - Table Grid */}
      {event.publishOrganizerContact && event.organizerContactName && (
        <Card>
          <CardHeader>
            <CardTitle style={{ color: '#8B1538' }}>Organizer Contact</CardTitle>
            <CardDescription>Event organizer's contact information</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              <div className="grid grid-cols-[140px_1fr] gap-x-4 items-center border-b pb-3">
                <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                  <Users className="h-4 w-4 text-[#FF7900]" />
                  Name:
                </span>
                <span className="text-sm text-neutral-900">{event.organizerContactName}</span>
              </div>
              {event.organizerContactEmail && (
                <div className="grid grid-cols-[140px_1fr] gap-x-4 items-center border-b pb-3">
                  <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                    <Mail className="h-4 w-4 text-[#FF7900]" />
                    Email:
                  </span>
                  <a
                    href={`mailto:${event.organizerContactEmail}`}
                    className="text-sm text-blue-600 hover:underline"
                  >
                    {event.organizerContactEmail}
                  </a>
                </div>
              )}
              {event.organizerContactPhone && (
                <div className="grid grid-cols-[140px_1fr] gap-x-4 items-center">
                  <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                    <svg className="h-4 w-4 text-[#FF7900]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
                    </svg>
                    Phone:
                  </span>
                  <a
                    href={`tel:${event.organizerContactPhone}`}
                    className="text-sm text-blue-600 hover:underline"
                  >
                    {event.organizerContactPhone}
                  </a>
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Media Section */}
      <Card>
        <CardHeader>
          <CardTitle style={{ color: '#8B1538' }}>Event Media</CardTitle>
          <CardDescription>Upload images and videos to promote your event</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-6">
            {/* Images */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                <ImageIcon className="h-5 w-5" style={{ color: '#FF7900' }} />
                <h4 className="text-sm font-semibold text-neutral-700">Event Images</h4>
              </div>
              <ImageUploader
                eventId={event.id}
                existingImages={event.images ? [...event.images] : []}
                maxImages={10}
                onUploadComplete={onRefetch}
              />
            </div>

            {/* Videos */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                <VideoIcon className="h-5 w-5" style={{ color: '#FF7900' }} />
                <h4 className="text-sm font-semibold text-neutral-700">Event Videos</h4>
              </div>
              <VideoUploader
                eventId={event.id}
                existingVideos={event.videos ? [...event.videos] : []}
                maxVideos={3}
                onUploadComplete={onRefetch}
              />
            </div>
          </div>
        </CardContent>
      </Card>

    </div>
  );
}
