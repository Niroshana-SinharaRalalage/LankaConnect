/**
 * EventDetailsTab Component
 *
 * Phase 6A.45: Event details tab for manage page
 * Displays event information, statistics, media galleries, and badges
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
  Upload,
  Award,
  Image as ImageIcon,
  Video as VideoIcon,
  Mail,
  AlertTriangle,
} from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Badge } from '@/presentation/components/ui/Badge';
import { ImageUploader } from '@/presentation/components/features/events/ImageUploader';
import { VideoUploader } from '@/presentation/components/features/events/VideoUploader';
import { BadgeAssignment } from '@/presentation/components/features/badges';
import { useEmailGroups } from '@/presentation/hooks/useEmailGroups';
import { EventCategory, EventStatus, type EventDto } from '@/infrastructure/api/types/events.types';
import type { EventBadgeDto } from '@/infrastructure/api/types/badges.types';

interface EventDetailsTabProps {
  event: EventDto;
  eventBadges: EventBadgeDto[];
  onRefetch: () => Promise<any>;
  onRefetchBadges: () => Promise<any>;
  isDraft: boolean;
  isPublished: boolean;
  isPublishing: boolean;
  isUnpublishing: boolean;
  onPublish: () => Promise<void>;
  onUnpublish: () => Promise<void>;
}

export function EventDetailsTab({
  event,
  eventBadges,
  onRefetch,
  onRefetchBadges,
}: EventDetailsTabProps) {
  const router = useRouter();
  const { data: emailGroups = [] } = useEmailGroups();

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

  const spotsLeft = event.capacity - event.currentRegistrations;
  const registrationPercentage = (event.currentRegistrations / event.capacity) * 100;

  return (
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
                  <span className="text-2xl font-bold text-blue-900">{event.currentRegistrations}</span>
                </div>
                <p className="text-sm text-blue-700 font-medium">Registered</p>
                <p className="text-xs text-blue-600 mt-1">{registrationPercentage.toFixed(0)}% of capacity</p>
              </div>

              {/* Available Spots */}
              <div className="p-4 bg-green-50 rounded-lg border border-green-200">
                <div className="flex items-center justify-between mb-2">
                  <Users className="h-5 w-5 text-green-600" />
                  <span className="text-2xl font-bold text-green-900">{spotsLeft}</span>
                </div>
                <p className="text-sm text-green-700 font-medium">Spots Left</p>
                <p className="text-xs text-green-600 mt-1">Total capacity: {event.capacity}</p>
              </div>

              {/* Revenue (if paid event) */}
              {!event.isFree && (event.ticketPriceAmount || event.hasDualPricing || event.hasGroupPricing) && (
                <div className="p-4 bg-orange-50 rounded-lg border border-orange-200">
                  <div className="flex items-center justify-between mb-2">
                    <DollarSign className="h-5 w-5 text-orange-600" />
                    <span className="text-2xl font-bold text-orange-900">
                      {event.hasGroupPricing && event.groupPricingTiers && event.groupPricingTiers.length > 0 ? (
                        `$${(event.groupPricingTiers[0].pricePerPerson * event.currentRegistrations).toFixed(2)}`
                      ) : event.hasDualPricing ? (
                        `$${((event.adultPriceAmount ?? 0) * event.currentRegistrations).toFixed(2)}`
                      ) : (
                        `$${((event.ticketPriceAmount ?? 0) * event.currentRegistrations).toFixed(2)}`
                      )}
                    </span>
                  </div>
                  <p className="text-sm text-orange-700 font-medium">Est. Revenue</p>
                  <p className="text-xs text-orange-600 mt-1">
                    {event.hasGroupPricing && event.groupPricingTiers && event.groupPricingTiers.length > 0 ? (
                      (() => {
                        const prices = event.groupPricingTiers.map(t => t.pricePerPerson);
                        const minPrice = Math.min(...prices);
                        const maxPrice = Math.max(...prices);
                        return minPrice === maxPrice ? `$${minPrice} range` : `$${minPrice}-$${maxPrice} range`;
                      })()
                    ) : event.hasDualPricing ? (
                      <>Adult: ${event.adultPriceAmount} | Child: ${event.childPriceAmount}</>
                    ) : (
                      <>${event.ticketPriceAmount} per ticket</>
                    )}
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
              <Badge className="bg-gray-100 text-gray-700">{categoryLabels[event.category]}</Badge>
            </div>

            {/* Pricing */}
            <div>
              <h4 className="text-sm font-semibold text-neutral-700 mb-2">Pricing</h4>
              {event.isFree ? (
                <Badge className="bg-green-100 text-green-700">Free Event</Badge>
              ) : event.hasGroupPricing && event.groupPricingTiers && event.groupPricingTiers.length > 0 ? (
                <div className="space-y-2">
                  <Badge className="bg-purple-100 text-purple-700 mb-2">Group Tiered Pricing</Badge>
                  <div className="flex flex-wrap items-center gap-1 text-sm">
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

            {/* Email Groups */}
            <div>
              <h4 className="text-sm font-semibold text-neutral-700 mb-2 flex items-center gap-2">
                <Mail className="h-4 w-4 text-[#FF7900]" />
                Email Groups
              </h4>
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
                    <p className="text-sm text-amber-700 mb-1">No email groups assigned to this event</p>
                    <p className="text-xs text-amber-600">
                      Want to notify specific groups about this event?{' '}
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
          </CardContent>
        </Card>
      </div>

      {/* Right Column - Media */}
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
          <CardContent>
            <ImageUploader
              eventId={event.id}
              existingImages={event.images ? [...event.images] : []}
              maxImages={10}
              onUploadComplete={onRefetch}
            />
          </CardContent>
        </Card>

        {/* Badge Assignment */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Award className="h-5 w-5" style={{ color: '#FF7900' }} />
              <CardTitle style={{ color: '#8B1538' }}>Event Badges</CardTitle>
            </div>
            <CardDescription>Add promotional badges to your event</CardDescription>
          </CardHeader>
          <CardContent>
            <BadgeAssignment
              eventId={event.id}
              existingBadges={eventBadges}
              onAssignmentChange={async () => {
                await onRefetchBadges();
                await onRefetch();
              }}
              maxBadges={3}
            />
          </CardContent>
        </Card>

        {/* Video Upload */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <VideoIcon className="h-5 w-5" style={{ color: '#FF7900' }} />
              <CardTitle style={{ color: '#8B1538' }}>Event Videos</CardTitle>
            </div>
            <CardDescription>Upload videos to showcase your event</CardDescription>
          </CardHeader>
          <CardContent>
            <VideoUploader
              eventId={event.id}
              existingVideos={event.videos ? [...event.videos] : []}
              maxVideos={3}
              onUploadComplete={onRefetch}
            />
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
