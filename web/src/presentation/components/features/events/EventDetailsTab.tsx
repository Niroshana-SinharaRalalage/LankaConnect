/**
 * EventDetailsTab Component
 *
 * Phase 6A.45: Event details tab for manage page
 * Displays event information, statistics, and media galleries
 *
 * Extracted from original manage page to support tabbed layout
 */

'use client';

import React, { useState, useMemo } from 'react';
import { sanitizeHtml } from '@/lib/html-utils';
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
  Settings,
  Save,
  X,
  Edit,
  Heart,
  CheckCircle,
  XCircle,
  UserPlus,
  Link2,
  Unlink,
  Loader2,
} from 'lucide-react';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Badge } from '@/presentation/components/ui/Badge';
import { ImageUploader } from '@/presentation/components/features/events/ImageUploader';
import { VideoUploader } from '@/presentation/components/features/events/VideoUploader';
import { useEmailGroups } from '@/presentation/hooks/useEmailGroups';
import { type EventDto } from '@/infrastructure/api/types/events.types';
import { CoOrganizerSearchModal } from '@/presentation/components/features/events/CoOrganizerSearchModal';
// Phase 6A.97: Import timezone-aware date formatter
import { formatEventDateTime } from '@/presentation/lib/utils/date-formatter';

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

  // Issue #51: State for editing max attendees per registration
  const [isEditingMaxAttendees, setIsEditingMaxAttendees] = useState(false);
  const [maxAttendeesValue, setMaxAttendeesValue] = useState(event.maxAttendeesPerRegistration || 10);
  const [isSavingMaxAttendees, setIsSavingMaxAttendees] = useState(false);
  const [maxAttendeesError, setMaxAttendeesError] = useState<string | null>(null);

  // Phase 6A.133: Co-organizer management state
  const [isLinkModalOpen, setIsLinkModalOpen] = useState(false);
  const [unlinkingContactId, setUnlinkingContactId] = useState<string | null>(null);

  const spotsLeft = event.capacity - event.currentRegistrations;
  const registrationPercentage = (event.currentRegistrations / event.capacity) * 100;

  // Phase 6A.133: Co-organizer stats
  const linkedCount = event.organizerContacts?.filter(c => c.linkedUserId).length ?? 0;
  const totalContacts = event.organizerContacts?.length ?? 0;
  const unlinkableContacts = event.organizerContacts?.filter(c => !c.linkedUserId) ?? [];

  // Phase 6A.133: Handle unlinking a co-organizer
  const handleUnlink = async (contactId: string) => {
    try {
      setUnlinkingContactId(contactId);
      await eventsRepository.unlinkOrganizerContact(event.id, contactId);
      await onRefetch();
    } catch (err) {
      console.error('Failed to unlink co-organizer:', err);
    } finally {
      setUnlinkingContactId(null);
    }
  };

  // Issue #51: Handle saving max attendees per registration
  const handleSaveMaxAttendees = async () => {
    try {
      setIsSavingMaxAttendees(true);
      setMaxAttendeesError(null);
      await eventsRepository.updateMaxAttendeesPerRegistration(event.id, maxAttendeesValue);
      await onRefetch();
      setIsEditingMaxAttendees(false);
    } catch (err) {
      console.error('Failed to update max attendees per registration:', err);
      setMaxAttendeesError(err instanceof Error ? err.message : 'Failed to update. Please try again.');
    } finally {
      setIsSavingMaxAttendees(false);
    }
  };

  const handleCancelEditMaxAttendees = () => {
    setMaxAttendeesValue(event.maxAttendeesPerRegistration || 10);
    setIsEditingMaxAttendees(false);
    setMaxAttendeesError(null);
  };

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
              <div
                className="prose prose-sm max-w-none text-neutral-600 prose-a:text-orange-600 prose-a:underline hover:prose-a:text-orange-700"
                dangerouslySetInnerHTML={{
                  __html: sanitizeHtml(event.description)
                }}
              />
            </div>

            {/* Start Date - Phase 6A.97: Uses event's timezone for consistent display */}
            <div className="grid grid-cols-[140px_1fr] gap-x-4 items-center border-b pb-3">
              <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                <Calendar className="h-4 w-4 text-[#FF7900]" />
                Start Date:
              </span>
              <span className="text-sm text-neutral-900">
                {formatEventDateTime(event.startDate, event.timeZoneId)}
              </span>
            </div>

            {/* End Date - Phase 6A.97: Uses event's timezone for consistent display */}
            {event.endDate && (
              <div className="grid grid-cols-[140px_1fr] gap-x-4 items-center border-b pb-3">
                <span className="text-sm font-semibold text-neutral-700">End Date:</span>
                <span className="text-sm text-neutral-900">
                  {formatEventDateTime(event.endDate, event.timeZoneId)}
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

      {/* Issue #51: Registration Settings Section */}
      <Card>
        <CardHeader>
          <CardTitle style={{ color: '#8B1538' }}>Registration Settings</CardTitle>
          <CardDescription>Configure how attendees can register for this event</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-[180px_1fr] gap-x-4 items-start">
            <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
              <Settings className="h-4 w-4 text-[#FF7900]" />
              Max per Registration:
            </span>
            <div>
              {isEditingMaxAttendees ? (
                <div className="flex items-center gap-2">
                  <input
                    type="number"
                    min={1}
                    max={Math.min(event.capacity, 50)}
                    value={maxAttendeesValue}
                    onChange={(e) => setMaxAttendeesValue(parseInt(e.target.value) || 1)}
                    className="w-20 px-2 py-1 text-sm border border-neutral-300 rounded focus:outline-none focus:ring-2 focus:ring-[#FF7900]"
                    disabled={isSavingMaxAttendees}
                  />
                  <span className="text-xs text-neutral-500">(1-{Math.min(event.capacity, 50)})</span>
                  <button
                    onClick={handleSaveMaxAttendees}
                    disabled={isSavingMaxAttendees}
                    className="p-1 text-green-600 hover:bg-green-50 rounded"
                    title="Save"
                  >
                    <Save className="h-4 w-4" />
                  </button>
                  <button
                    onClick={handleCancelEditMaxAttendees}
                    disabled={isSavingMaxAttendees}
                    className="p-1 text-red-600 hover:bg-red-50 rounded"
                    title="Cancel"
                  >
                    <X className="h-4 w-4" />
                  </button>
                </div>
              ) : (
                <div className="flex items-center gap-2">
                  <span className="text-sm text-neutral-900">
                    {event.maxAttendeesPerRegistration || 10} attendees
                  </span>
                  <button
                    onClick={() => setIsEditingMaxAttendees(true)}
                    className="p-1 text-[#FF7900] hover:bg-orange-50 rounded"
                    title="Edit"
                  >
                    <Edit className="h-4 w-4" />
                  </button>
                </div>
              )}
              {maxAttendeesError && (
                <p className="text-xs text-red-600 mt-1">{maxAttendeesError}</p>
              )}
              <p className="text-xs text-neutral-500 mt-1">
                Maximum number of attendees that can be added in a single registration
              </p>
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

      {/* Organizer Contact Section - Multiple Contacts with Co-Organizer Management */}
      {event.organizerContacts && event.organizerContacts.length > 0 && (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle style={{ color: '#8B1538' }}>Organizer Contacts</CardTitle>
                <CardDescription>
                  Event organizer contact information
                  {linkedCount > 0 && (
                    <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">
                      {linkedCount}/{totalContacts} co-organizers linked
                    </span>
                  )}
                </CardDescription>
              </div>
              {event.isCurrentUserOrganizer && unlinkableContacts.length > 0 && (
                <button
                  onClick={() => setIsLinkModalOpen(true)}
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-white bg-[#8B1538] hover:bg-[#6d1029] rounded-lg transition-colors"
                >
                  <UserPlus className="h-4 w-4" />
                  Link Co-Organizers
                </button>
              )}
            </div>
          </CardHeader>
          <CardContent>
            <div className="overflow-x-auto rounded-lg border border-neutral-200">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-neutral-50 border-b border-neutral-200">
                    <th className="text-left px-4 py-3 font-medium text-neutral-600">Name</th>
                    <th className="text-left px-4 py-3 font-medium text-neutral-600">Email</th>
                    <th className="text-left px-4 py-3 font-medium text-neutral-600">Phone</th>
                    <th className="text-left px-4 py-3 font-medium text-neutral-600">Linked User</th>
                    {event.isCurrentUserOrganizer && (
                      <th className="text-left px-4 py-3 font-medium text-neutral-600">Actions</th>
                    )}
                  </tr>
                </thead>
                <tbody>
                  {event.organizerContacts.map((contact, idx) => (
                    <tr key={contact.id || idx} className="border-b border-neutral-100 hover:bg-neutral-50">
                      <td className="px-4 py-3">
                        <span className="font-medium text-neutral-800">{contact.contactName}</span>
                        {contact.isPrimary && (
                          <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800">
                            Primary
                          </span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-neutral-600">
                        {contact.contactEmail ? (
                          <a href={`mailto:${contact.contactEmail}`} className="text-blue-600 hover:underline">{contact.contactEmail}</a>
                        ) : '\u2014'}
                      </td>
                      <td className="px-4 py-3 text-neutral-600">
                        {contact.contactPhone ? (
                          <a href={`tel:${contact.contactPhone}`} className="text-blue-600 hover:underline">{contact.contactPhone}</a>
                        ) : '\u2014'}
                      </td>
                      <td className="px-4 py-3">
                        {contact.linkedUserId ? (
                          <span className="inline-flex items-center gap-1.5 px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-700">
                            <Link2 className="h-3 w-3" />
                            Linked
                          </span>
                        ) : (
                          <span className="text-xs text-neutral-400">Not linked</span>
                        )}
                      </td>
                      {event.isCurrentUserOrganizer && (
                        <td className="px-4 py-3">
                          {contact.linkedUserId ? (
                            <button
                              onClick={() => handleUnlink(contact.id)}
                              disabled={unlinkingContactId === contact.id}
                              className="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium text-red-600 hover:bg-red-50 rounded transition-colors disabled:opacity-50"
                              title="Unlink co-organizer"
                            >
                              {unlinkingContactId === contact.id ? (
                                <Loader2 className="h-3 w-3 animate-spin" />
                              ) : (
                                <Unlink className="h-3 w-3" />
                              )}
                              Unlink
                            </button>
                          ) : (
                            <button
                              onClick={() => setIsLinkModalOpen(true)}
                              className="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium text-[#FF7900] hover:bg-orange-50 rounded transition-colors"
                              title="Link a user to this contact"
                            >
                              <UserPlus className="h-3 w-3" />
                              Link User
                            </button>
                          )}
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Phase 6A.133: Co-Organizer Search Modal */}
      {event.isCurrentUserOrganizer && (
        <CoOrganizerSearchModal
          open={isLinkModalOpen}
          onOpenChange={setIsLinkModalOpen}
          eventId={event.id}
          unlinkableContacts={unlinkableContacts}
          onLinkComplete={onRefetch}
        />
      )}

      {/* Donation Feature: Donation Configuration Section */}
      {event.donationConfig && (
        <Card>
          <CardHeader>
            <CardTitle style={{ color: '#8B1538' }}>Donation Configuration</CardTitle>
            <CardDescription>Donation settings for this event</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              <div className="grid grid-cols-[180px_1fr] gap-x-4 items-center border-b pb-3">
                <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                  <Heart className="h-4 w-4 text-[#FF7900]" />
                  Status:
                </span>
                <div className="flex items-center gap-2">
                  {event.donationConfig.isEnabled ? (
                    <>
                      <CheckCircle className="h-4 w-4 text-emerald-500" />
                      <span className="text-sm text-emerald-700 font-medium">Donations enabled</span>
                    </>
                  ) : (
                    <>
                      <XCircle className="h-4 w-4 text-neutral-400" />
                      <span className="text-sm text-neutral-500">Donations disabled</span>
                    </>
                  )}
                </div>
              </div>
              {event.donationConfig.isEnabled && (
                <>
                  {event.donationConfig.suggestedAmounts && event.donationConfig.suggestedAmounts.length > 0 && (
                    <div className="grid grid-cols-[180px_1fr] gap-x-4 items-center border-b pb-3">
                      <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                        <DollarSign className="h-4 w-4 text-[#FF7900]" />
                        Suggested Amounts:
                      </span>
                      <div className="flex flex-wrap gap-2">
                        {event.donationConfig.suggestedAmounts.map((amount: number) => (
                          <Badge key={amount} className="bg-rose-50 text-rose-700 border border-rose-200">
                            ${amount.toFixed(2)}
                          </Badge>
                        ))}
                      </div>
                    </div>
                  )}
                  <div className="grid grid-cols-[180px_1fr] gap-x-4 items-center border-b pb-3">
                    <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                      <Settings className="h-4 w-4 text-[#FF7900]" />
                      Custom Amount:
                    </span>
                    <div className="flex items-center gap-2">
                      {event.donationConfig.allowCustomAmount ? (
                        <>
                          <CheckCircle className="h-4 w-4 text-emerald-500" />
                          <span className="text-sm text-neutral-700">Allowed</span>
                        </>
                      ) : (
                        <>
                          <XCircle className="h-4 w-4 text-neutral-400" />
                          <span className="text-sm text-neutral-500">Not allowed</span>
                        </>
                      )}
                    </div>
                  </div>
                  {(event.donationConfig.minAmount || event.donationConfig.maxAmount) && (
                    <div className="grid grid-cols-[180px_1fr] gap-x-4 items-center border-b pb-3">
                      <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                        <DollarSign className="h-4 w-4 text-[#FF7900]" />
                        Amount Range:
                      </span>
                      <span className="text-sm text-neutral-700">
                        {event.donationConfig.minAmount ? `$${event.donationConfig.minAmount.toFixed(2)}` : 'No min'}
                        {' — '}
                        {event.donationConfig.maxAmount ? `$${event.donationConfig.maxAmount.toFixed(2)}` : 'No max'}
                      </span>
                    </div>
                  )}
                  {event.donationConfig.donationMessage && (
                    <div className="grid grid-cols-[180px_1fr] gap-x-4 items-start">
                      <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                        <Mail className="h-4 w-4 text-[#FF7900]" />
                        Donation Message:
                      </span>
                      <span className="text-sm text-neutral-600 italic">
                        &ldquo;{event.donationConfig.donationMessage}&rdquo;
                      </span>
                    </div>
                  )}
                </>
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
