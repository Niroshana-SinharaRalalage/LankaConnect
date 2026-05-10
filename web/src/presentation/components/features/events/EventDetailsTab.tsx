/**
 * EventDetailsTab Component
 *
 * Phase 6A.45: Event details tab for manage page
 * Displays event information, statistics, and media galleries
 *
 * Extracted from original manage page to support tabbed layout
 */

'use client';

import React, { useState } from 'react';
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
  Link2,
  Wallet,
  HandCoins,
  PackagePlus,
} from 'lucide-react';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import { CollapsibleSection } from '@/presentation/components/ui/CollapsibleSection';
import { Badge } from '@/presentation/components/ui/Badge';
import { ImageUploader } from '@/presentation/components/features/events/ImageUploader';
import { VideoUploader } from '@/presentation/components/features/events/VideoUploader';
import { useEmailGroups } from '@/presentation/hooks/useEmailGroups';
import { type EventDto, type AddOnDefinitionDto } from '@/infrastructure/api/types/events.types';
import { useAddOnDefinitions } from '@/presentation/hooks/useAddOns';
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
  const { data: addOnDefinitions = [] } = useAddOnDefinitions(event.id, event.addOnConfig?.isEnabled === true);

  // Issue #51: State for editing max attendees per registration
  const [isEditingMaxAttendees, setIsEditingMaxAttendees] = useState(false);
  const [maxAttendeesValue, setMaxAttendeesValue] = useState(event.maxAttendeesPerRegistration || 10);
  const [isSavingMaxAttendees, setIsSavingMaxAttendees] = useState(false);
  const [maxAttendeesError, setMaxAttendeesError] = useState<string | null>(null);

  const spotsLeft = event.capacity - event.currentRegistrations;
  const registrationPercentage = (event.currentRegistrations / event.capacity) * 100;

  // Co-organizer stats (read-only display)
  const linkedCount = event.organizerContacts?.filter(c => c.linkedUserId).length ?? 0;
  const totalContacts = event.organizerContacts?.length ?? 0;

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
      <div id="statistics" className="scroll-mt-20">
        <CollapsibleSection
          title="Event Statistics"
          description="Track your event's performance"
          defaultOpen={true}
        >
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
        </CollapsibleSection>
      </div>

      {/* Event Details Section - Combined */}
      <div id="details" className="scroll-mt-20">
        <CollapsibleSection
          title="Event Details"
          description="Basic information, schedule, and location"
          defaultOpen={true}
        >
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

            {/* Start Date - Phase 6A.97: Uses event's timezone for consistent display.
                Phase 8YA.3: render "Date TBD" placeholder when event has no confirmed
                dates (Status=Planning, or Q1=A Published-with-TBD). */}
            <div className="grid grid-cols-[140px_1fr] gap-x-4 items-center border-b pb-3">
              <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                <Calendar className="h-4 w-4 text-[#FF7900]" />
                Start Date:
              </span>
              <span className="text-sm text-neutral-900">
                {event.startDate
                  ? formatEventDateTime(event.startDate, event.timeZoneId)
                  : 'Date TBD'}
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
              <div className="grid grid-cols-[140px_1fr] gap-x-4 items-start border-b pb-3">
                <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                  <MapPin className="h-4 w-4 text-[#FF7900]" />
                  Location:
                </span>
                <div className="text-sm text-neutral-900 space-y-1">
                  {event.locationName && (
                    <div className="font-semibold">{event.locationName}</div>
                  )}
                  <div>
                    {event.address && `${event.address}, `}
                    {event.city}, {event.state} {event.zipCode}
                  </div>
                </div>
              </div>
            )}

            {/* Phase 7C.1: Secondary Location (parking lot or secondary venue) */}
            {event.hasSecondaryLocation && event.secondaryLocationType && (
              <div className="grid grid-cols-[140px_1fr] gap-x-4 items-start border-b pb-3">
                <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                  <MapPin className="h-4 w-4 text-[#FF7900]" />
                  {event.secondaryLocationType === 'ParkingLot' ? 'Parking Lot:' : 'Secondary Venue:'}
                </span>
                <div className="text-sm text-neutral-900 space-y-1">
                  {event.secondaryLocationName && (
                    <div className="font-semibold">{event.secondaryLocationName}</div>
                  )}
                  <div>
                    {event.secondaryAddress && `${event.secondaryAddress}, `}
                    {event.secondaryCity}{event.secondaryState ? `, ${event.secondaryState}` : ''} {event.secondaryZipCode}
                  </div>
                </div>
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
        </CollapsibleSection>
      </div>

      {/* Pricing Section - Table Grid */}
      <div id="pricing" className="scroll-mt-20">
        <CollapsibleSection
          title="Pricing Information"
          description="Ticket prices and payment details"
          defaultOpen={false}
        >
          <div className="grid grid-cols-[140px_1fr] gap-x-4 items-start">
            <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
              <DollarSign className="h-4 w-4 text-[#FF7900]" />
              Pricing:
            </span>
            <div>
              {event.isFree ? (
                <Badge className="bg-green-100 text-green-700">Free Event</Badge>
              ) : event.hasTicketTiers && event.ticketTiers && event.ticketTiers.length > 0 ? (
                <div className="space-y-2">
                  <Badge className="bg-indigo-100 text-indigo-700">Multi-Tier Pricing</Badge>
                  <div className="flex flex-col gap-1 mt-1">
                    {event.ticketTiers.filter(t => t.isActive).map((tier) => (
                      <div key={tier.id} className="flex items-center gap-2">
                        <Badge className="bg-[#FFE8CC] text-[#8B1538]">
                          {tier.name}: {tier.isFree ? 'Free' : `$${tier.adultPriceAmount}`}
                          {tier.childPriceAmount != null && !tier.isFree && ` / Child: $${tier.childPriceAmount}`}
                        </Badge>
                        <span className={`text-xs px-1.5 py-0.5 rounded-full ${
                          tier.availableQuantity === 0 ? 'bg-red-100 text-red-700'
                            : tier.availableQuantity <= 10 ? 'bg-orange-100 text-orange-700'
                            : 'bg-green-100 text-green-700'
                        }`}>
                          {tier.availableQuantity === 0 ? 'Sold Out' : `${tier.availableQuantity} left`}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
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
        </CollapsibleSection>
      </div>

      {/* Issue #51: Registration Settings Section */}
      <div id="registration-settings" className="scroll-mt-20">
        <CollapsibleSection
          title="Registration Settings"
          description="Configure how attendees can register for this event"
          defaultOpen={false}
        >
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
        </CollapsibleSection>
      </div>

      {/* Email Groups Section - Table Grid */}
      <div id="email-groups" className="scroll-mt-20">
        <CollapsibleSection
          title="Email Groups"
          description="Newsletter groups assigned to this event"
          defaultOpen={false}
        >
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
        </CollapsibleSection>
      </div>

      {/* Organizer Contact Section - Read-only display */}
      {event.organizerContacts && event.organizerContacts.length > 0 && (
        <div id="organizer-contacts" className="scroll-mt-20">
          <CollapsibleSection
            title="Organizer Contacts"
            description="Event organizer contact information"
            defaultOpen={false}
            badge={linkedCount > 0 ? (
              <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">
                {linkedCount}/{totalContacts} co-organizers linked
              </span>
            ) : undefined}
          >
            <div className="overflow-x-auto rounded-lg border border-neutral-200">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-neutral-50 border-b border-neutral-200">
                    <th className="text-left px-4 py-3 font-medium text-neutral-600">Name</th>
                    <th className="text-left px-4 py-3 font-medium text-neutral-600">Email</th>
                    <th className="text-left px-4 py-3 font-medium text-neutral-600">Phone</th>
                    <th className="text-left px-4 py-3 font-medium text-neutral-600">Status</th>
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
                            Co-Organizer
                          </span>
                        ) : (
                          <span className="text-xs text-neutral-400">Contact only</span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {event.isCurrentUserOrganizer && (
              <p className="mt-3 text-xs text-neutral-500">
                To add or manage co-organizers, use the Edit Event page.
              </p>
            )}
          </CollapsibleSection>
        </div>
      )}

      {/* Donation Feature: Donation Configuration Section */}
      {event.donationConfig && (
        <div id="donation-config" className="scroll-mt-20">
          <CollapsibleSection
            title="Donation Configuration"
            description="Donation settings for this event"
            defaultOpen={false}
          >
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
          </CollapsibleSection>
        </div>
      )}

      {/* Collection Configuration Section */}
      {event.collectionConfig && (
        <div id="collection-config" className="scroll-mt-20">
          <CollapsibleSection
            title="Collection Configuration"
            description="Event fund collection settings for this event"
            defaultOpen={false}
          >
            <div className="space-y-3">
              <div className="grid grid-cols-[180px_1fr] gap-x-4 items-center border-b pb-3">
                <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                  <Wallet className="h-4 w-4 text-[#FF7900]" />
                  Status:
                </span>
                <div className="flex items-center gap-2">
                  {event.collectionConfig.isEnabled ? (
                    <>
                      <CheckCircle className="h-4 w-4 text-emerald-500" />
                      <span className="text-sm text-emerald-700 font-medium">Collections enabled</span>
                    </>
                  ) : (
                    <>
                      <XCircle className="h-4 w-4 text-neutral-400" />
                      <span className="text-sm text-neutral-500">Collections disabled</span>
                    </>
                  )}
                </div>
              </div>
              {event.collectionConfig.isEnabled && (
                <>
                  {event.collectionConfig.goalAmount != null && (
                    <div className="grid grid-cols-[180px_1fr] gap-x-4 items-center border-b pb-3">
                      <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                        <DollarSign className="h-4 w-4 text-[#FF7900]" />
                        Goal Amount:
                      </span>
                      <Badge className="bg-rose-50 text-rose-700 border border-rose-200 w-fit">
                        ${event.collectionConfig.goalAmount.toFixed(2)}
                      </Badge>
                    </div>
                  )}
                  <div className="grid grid-cols-[180px_1fr] gap-x-4 items-center border-b pb-3">
                    <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                      <Settings className="h-4 w-4 text-[#FF7900]" />
                      Show Progress:
                    </span>
                    <div className="flex items-center gap-2">
                      {event.collectionConfig.showProgress ? (
                        <>
                          <CheckCircle className="h-4 w-4 text-emerald-500" />
                          <span className="text-sm text-neutral-700">Yes</span>
                        </>
                      ) : (
                        <>
                          <XCircle className="h-4 w-4 text-neutral-400" />
                          <span className="text-sm text-neutral-500">No</span>
                        </>
                      )}
                    </div>
                  </div>
                  {event.collectionConfig.suggestedAmounts && event.collectionConfig.suggestedAmounts.length > 0 && (
                    <div className="grid grid-cols-[180px_1fr] gap-x-4 items-center border-b pb-3">
                      <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                        <DollarSign className="h-4 w-4 text-[#FF7900]" />
                        Suggested Amounts:
                      </span>
                      <div className="flex flex-wrap gap-2">
                        {event.collectionConfig.suggestedAmounts.map((amount: number) => (
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
                      {event.collectionConfig.allowCustomAmount ? (
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
                  {(event.collectionConfig.minAmount || event.collectionConfig.maxAmount) && (
                    <div className="grid grid-cols-[180px_1fr] gap-x-4 items-center border-b pb-3">
                      <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                        <DollarSign className="h-4 w-4 text-[#FF7900]" />
                        Amount Range:
                      </span>
                      <span className="text-sm text-neutral-700">
                        {event.collectionConfig.minAmount ? `$${event.collectionConfig.minAmount.toFixed(2)}` : 'No min'}
                        {' — '}
                        {event.collectionConfig.maxAmount ? `$${event.collectionConfig.maxAmount.toFixed(2)}` : 'No max'}
                      </span>
                    </div>
                  )}
                  <div className="grid grid-cols-[180px_1fr] gap-x-4 items-center border-b pb-3">
                    <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                      <Settings className="h-4 w-4 text-[#FF7900]" />
                      Show Contributor Count:
                    </span>
                    <div className="flex items-center gap-2">
                      {event.collectionConfig.showContributorCount ? (
                        <>
                          <CheckCircle className="h-4 w-4 text-emerald-500" />
                          <span className="text-sm text-neutral-700">Yes</span>
                        </>
                      ) : (
                        <>
                          <XCircle className="h-4 w-4 text-neutral-400" />
                          <span className="text-sm text-neutral-500">No</span>
                        </>
                      )}
                    </div>
                  </div>
                  {event.collectionConfig.collectionMessage && (
                    <div className="grid grid-cols-[180px_1fr] gap-x-4 items-start">
                      <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                        <Mail className="h-4 w-4 text-[#FF7900]" />
                        Collection Message:
                      </span>
                      <span className="text-sm text-neutral-600 italic">
                        &ldquo;{event.collectionConfig.collectionMessage}&rdquo;
                      </span>
                    </div>
                  )}
                </>
              )}
            </div>
          </CollapsibleSection>
        </div>
      )}

      {/* Sponsor Configuration Section */}
      {event.sponsorConfig && (
        <div id="sponsor-config" className="scroll-mt-20">
          <CollapsibleSection
            title="Sponsor Configuration"
            description="Sponsorship settings for this event"
            defaultOpen={false}
          >
            <div className="space-y-3">
              <div className="grid grid-cols-[180px_1fr] gap-x-4 items-center border-b pb-3">
                <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                  <HandCoins className="h-4 w-4 text-[#FF7900]" />
                  Status:
                </span>
                <div className="flex items-center gap-2">
                  {event.sponsorConfig.isEnabled ? (
                    <>
                      <CheckCircle className="h-4 w-4 text-emerald-500" />
                      <span className="text-sm text-emerald-700 font-medium">Sponsors enabled</span>
                    </>
                  ) : (
                    <>
                      <XCircle className="h-4 w-4 text-neutral-400" />
                      <span className="text-sm text-neutral-500">Sponsors disabled</span>
                    </>
                  )}
                </div>
              </div>
              {event.sponsorConfig.isEnabled && (
                <>
                  <div className="grid grid-cols-[180px_1fr] gap-x-4 items-center border-b pb-3">
                    <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                      <Settings className="h-4 w-4 text-[#FF7900]" />
                      Accept Money Sponsors:
                    </span>
                    <div className="flex items-center gap-2">
                      {event.sponsorConfig.acceptMoneySponsors ? (
                        <>
                          <CheckCircle className="h-4 w-4 text-emerald-500" />
                          <span className="text-sm text-neutral-700">Yes</span>
                        </>
                      ) : (
                        <>
                          <XCircle className="h-4 w-4 text-neutral-400" />
                          <span className="text-sm text-neutral-500">No</span>
                        </>
                      )}
                    </div>
                  </div>
                  <div className="grid grid-cols-[180px_1fr] gap-x-4 items-center border-b pb-3">
                    <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                      <Settings className="h-4 w-4 text-[#FF7900]" />
                      Accept Item Sponsors:
                    </span>
                    <div className="flex items-center gap-2">
                      {event.sponsorConfig.acceptItemSponsors ? (
                        <>
                          <CheckCircle className="h-4 w-4 text-emerald-500" />
                          <span className="text-sm text-neutral-700">Yes</span>
                        </>
                      ) : (
                        <>
                          <XCircle className="h-4 w-4 text-neutral-400" />
                          <span className="text-sm text-neutral-500">No</span>
                        </>
                      )}
                    </div>
                  </div>
                  {event.sponsorConfig.minSponsorAmount != null && (
                    <div className="grid grid-cols-[180px_1fr] gap-x-4 items-center border-b pb-3">
                      <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                        <DollarSign className="h-4 w-4 text-[#FF7900]" />
                        Min Sponsor Amount:
                      </span>
                      <Badge className="bg-rose-50 text-rose-700 border border-rose-200 w-fit">
                        ${event.sponsorConfig.minSponsorAmount.toFixed(2)}
                      </Badge>
                    </div>
                  )}
                  <div className="grid grid-cols-[180px_1fr] gap-x-4 items-center border-b pb-3">
                    <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                      <Settings className="h-4 w-4 text-[#FF7900]" />
                      Show Sponsor List:
                    </span>
                    <div className="flex items-center gap-2">
                      {event.sponsorConfig.showSponsorList ? (
                        <>
                          <CheckCircle className="h-4 w-4 text-emerald-500" />
                          <span className="text-sm text-neutral-700">Yes</span>
                        </>
                      ) : (
                        <>
                          <XCircle className="h-4 w-4 text-neutral-400" />
                          <span className="text-sm text-neutral-500">No</span>
                        </>
                      )}
                    </div>
                  </div>
                  {event.sponsorConfig.sponsorMessage && (
                    <div className="grid grid-cols-[180px_1fr] gap-x-4 items-start">
                      <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                        <Mail className="h-4 w-4 text-[#FF7900]" />
                        Sponsor Message:
                      </span>
                      <span className="text-sm text-neutral-600 italic">
                        &ldquo;{event.sponsorConfig.sponsorMessage}&rdquo;
                      </span>
                    </div>
                  )}
                </>
              )}
            </div>
          </CollapsibleSection>
        </div>
      )}

      {/* Add-On Configuration Section */}
      {event.addOnConfig && (
        <div id="addon-config" className="scroll-mt-20">
          <CollapsibleSection
            title="Add-On Configuration"
            description="Add-on settings for this event"
            defaultOpen={false}
          >
            <div className="space-y-3">
              <div className="grid grid-cols-[180px_1fr] gap-x-4 items-center border-b pb-3">
                <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                  <PackagePlus className="h-4 w-4 text-[#FF7900]" />
                  Status:
                </span>
                <div className="flex items-center gap-2">
                  {event.addOnConfig.isEnabled ? (
                    <>
                      <CheckCircle className="h-4 w-4 text-emerald-500" />
                      <span className="text-sm text-emerald-700 font-medium">Add-ons enabled</span>
                    </>
                  ) : (
                    <>
                      <XCircle className="h-4 w-4 text-neutral-400" />
                      <span className="text-sm text-neutral-500">Add-ons disabled</span>
                    </>
                  )}
                </div>
              </div>
              {event.addOnConfig.isEnabled && (
                <>
                  <div className="grid grid-cols-[180px_1fr] gap-x-4 items-center border-b pb-3">
                    <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                      <Settings className="h-4 w-4 text-[#FF7900]" />
                      During Registration:
                    </span>
                    <div className="flex items-center gap-2">
                      {event.addOnConfig.availableDuringRegistration ? (
                        <>
                          <CheckCircle className="h-4 w-4 text-emerald-500" />
                          <span className="text-sm text-neutral-700">Available</span>
                        </>
                      ) : (
                        <>
                          <XCircle className="h-4 w-4 text-neutral-400" />
                          <span className="text-sm text-neutral-500">Not available</span>
                        </>
                      )}
                    </div>
                  </div>
                  <div className="grid grid-cols-[180px_1fr] gap-x-4 items-center border-b pb-3">
                    <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                      <Settings className="h-4 w-4 text-[#FF7900]" />
                      Standalone Purchase:
                    </span>
                    <div className="flex items-center gap-2">
                      {event.addOnConfig.availableStandalone ? (
                        <>
                          <CheckCircle className="h-4 w-4 text-emerald-500" />
                          <span className="text-sm text-neutral-700">Available</span>
                        </>
                      ) : (
                        <>
                          <XCircle className="h-4 w-4 text-neutral-400" />
                          <span className="text-sm text-neutral-500">Not available</span>
                        </>
                      )}
                    </div>
                  </div>
                  {event.addOnConfig.addOnMessage && (
                    <div className="grid grid-cols-[180px_1fr] gap-x-4 items-start border-b pb-3">
                      <span className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
                        <Mail className="h-4 w-4 text-[#FF7900]" />
                        Add-On Message:
                      </span>
                      <span className="text-sm text-neutral-600 italic">
                        &ldquo;{event.addOnConfig.addOnMessage}&rdquo;
                      </span>
                    </div>
                  )}
                  {/* Add-On Items Table */}
                  <div className="pt-1">
                    <div className="flex items-center gap-2 mb-3">
                      <PackagePlus className="h-4 w-4 text-[#FF7900]" />
                      <span className="text-sm font-semibold text-neutral-700">Add-On Items</span>
                    </div>
                    {addOnDefinitions.length > 0 ? (
                      <div className="border border-neutral-200 rounded-lg overflow-hidden">
                        <table className="w-full text-sm">
                          <thead>
                            <tr className="bg-neutral-50 border-b border-neutral-200">
                              <th className="text-left px-4 py-2 font-semibold text-neutral-600">Name</th>
                              <th className="text-right px-4 py-2 font-semibold text-neutral-600">Price</th>
                              <th className="text-center px-4 py-2 font-semibold text-neutral-600">Stock</th>
                              <th className="text-center px-4 py-2 font-semibold text-neutral-600">Status</th>
                            </tr>
                          </thead>
                          <tbody>
                            {addOnDefinitions.map((def: AddOnDefinitionDto, idx: number) => (
                              <tr key={def.id} className={idx < addOnDefinitions.length - 1 ? 'border-b border-neutral-100' : ''}>
                                <td className="px-4 py-2.5">
                                  <span className="text-neutral-800 font-medium">{def.name}</span>
                                  {def.description && (
                                    <p className="text-xs text-neutral-400 mt-0.5 line-clamp-1">{def.description}</p>
                                  )}
                                </td>
                                <td className="px-4 py-2.5 text-right">
                                  {def.price === 0 ? (
                                    <Badge className="bg-emerald-50 text-emerald-700 border border-emerald-200 text-xs px-2 py-0.5">
                                      Free
                                    </Badge>
                                  ) : (
                                    <span className="text-neutral-700 font-medium">${def.price.toFixed(2)}</span>
                                  )}
                                </td>
                                <td className="px-4 py-2.5 text-center text-neutral-600">
                                  {def.quantityLimit != null ? (
                                    <span>{def.quantitySold}/{def.quantityLimit}</span>
                                  ) : (
                                    <span className="text-neutral-400">Unlimited</span>
                                  )}
                                </td>
                                <td className="px-4 py-2.5 text-center">
                                  {def.isActive ? (
                                    <Badge className="bg-emerald-50 text-emerald-700 border border-emerald-200 text-xs px-2 py-0.5">
                                      Active
                                    </Badge>
                                  ) : (
                                    <Badge className="bg-neutral-100 text-neutral-500 border border-neutral-200 text-xs px-2 py-0.5">
                                      Inactive
                                    </Badge>
                                  )}
                                </td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    ) : (
                      <p className="text-sm text-neutral-400 italic pl-6">No add-on items defined yet</p>
                    )}
                  </div>
                </>
              )}
            </div>
          </CollapsibleSection>
        </div>
      )}

      {/* Media Section */}
      <div id="event-media" className="scroll-mt-20">
        <CollapsibleSection
          title="Event Media"
          description="Upload images and videos to promote your event"
          defaultOpen={false}
        >
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
        </CollapsibleSection>
      </div>

    </div>
  );
}
