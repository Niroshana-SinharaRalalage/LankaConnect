'use client';

import { useState, useEffect } from 'react';
import { Input } from '@/presentation/components/ui/Input';
import { PhoneInput } from '@/presentation/components/ui/PhoneInput';
import { Button } from '@/presentation/components/ui/Button';
import { Clock, Plus, Trash2 } from 'lucide-react';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import type { AnonymousRegistrationRequest, AttendeeDto, RsvpRequest, GroupPricingTierDto, DonationConfigurationDto, AddOnConfigurationDto, CollectionConfigurationDto, SponsorConfigurationDto, TicketTierDto } from '@/infrastructure/api/types/events.types';
import { AgeCategory, Gender, TicketingMode, SeatingMode } from '@/infrastructure/api/types/events.types';
// Slice 7 S7.6: swap from the DOM-based SeatSelector to the Konva-based
// SeatPickerView. Same public contract (eventId, maxSeats, userId,
// onSeatsConfirmed, onCancel) so only the import changes. SeatSelector.tsx
// remains in the tree as dead code until Slice 7 closeout confirms no
// rollback is needed, then it gets removed in the cleanup PR.
import { SeatPickerView as SeatSelector } from './SeatPickerView';
import { DonationOptionInForm } from './DonationOptionInForm';
import { AddOnOptionInForm, type AddOnSelection } from './AddOnOptionInForm';
import { CollectionOptionInForm } from './CollectionOptionInForm';
// Phase 6A.157-fix-1 [1/3] — SponsorOptionInForm retired from the registration
// flow per operator UAT 2026-06-01. Backend RsvpToEventCommand still accepts
// the optional sponsor* fields (UI-only removal preserves backward-compat with
// in-flight deployed clients during rollout). Sponsorship is now its own flow
// via SponsorSection on the event detail page (Phase 6A.157 buyer purchase).
// import { SponsorOptionInForm } from './SponsorOptionInForm';
import { validatePhoneNumber, isValidPhoneNumber } from '@/presentation/lib/validators/phone';
import { WhatsAppInlineOptIn } from '@/presentation/components/features/whatsapp/WhatsAppInlineOptIn';
import { toE164 } from '@/presentation/lib/validators/whatsapp.schemas';

/**
 * Event Registration Form Component
 * Session 21: Multi-attendee registration with individual names and ages
 * Phase 6D: Group tiered pricing support
 * Supports both anonymous and authenticated registration flows
 * - Anonymous users: Fill in contact details + individual attendee names/ages
 * - Authenticated users: Pre-populate first attendee from profile, details auto-filled
 */
interface EventRegistrationFormProps {
  eventId: string;
  spotsLeft: number;
  isFree: boolean;
  ticketPrice?: number;
  // Session 21: Dual pricing support
  hasDualPricing?: boolean;
  adultPrice?: number;
  childPrice?: number;
  childAgeLimit?: number;
  // Phase 6D: Group tiered pricing support
  hasGroupPricing?: boolean;
  groupPricingTiers?: readonly GroupPricingTierDto[];
  // Phase 2: Seating support
  seatingMode?: SeatingMode;
  // Phase 8: Multi-tier ticketing support
  ticketingMode?: TicketingMode;
  ticketTiers?: readonly TicketTierDto[];
  // Issue #51: Max attendees per registration (configurable by event organizer)
  maxAttendeesPerRegistration?: number;
  // Donation Feature: Optional donation configuration
  donationConfig?: DonationConfigurationDto | null;
  // Add-On Feature: Optional add-on configuration for registration bundling
  addOnConfig?: AddOnConfigurationDto | null;
  // Phase 6A.137E: Collection/sponsor configuration for registration bundling
  collectionConfig?: CollectionConfigurationDto | null;
  sponsorConfig?: SponsorConfigurationDto | null;
  isProcessing: boolean;
  onSubmit: (data: AnonymousRegistrationRequest | RsvpRequest) => Promise<void>;
  error?: string | null;
}

export function EventRegistrationForm({
  eventId,
  spotsLeft,
  isFree,
  ticketPrice,
  hasDualPricing,
  adultPrice,
  childPrice,
  childAgeLimit,
  hasGroupPricing,
  groupPricingTiers,
  seatingMode,
  ticketingMode,
  ticketTiers,
  maxAttendeesPerRegistration = 10, // Issue #51: Default 10 for backward compatibility
  donationConfig,
  addOnConfig,
  collectionConfig,
  sponsorConfig,
  isProcessing,
  onSubmit,
  error,
}: EventRegistrationFormProps) {
  const { user } = useAuthStore();
  const { profile, loadProfile } = useProfileStore();

  // Phase 8: When tiered ticketing is active, suppress legacy pricing modes
  const isTieredMode = ticketTiers && ticketTiers.length > 0 && ticketingMode === 'Tiered';
  const effectiveDualPricing = isTieredMode ? false : hasDualPricing;
  const effectiveGroupPricing = isTieredMode ? false : hasGroupPricing;
  const effectiveTicketPrice = isTieredMode ? undefined : ticketPrice;

  // Donation Feature: Donation amount state
  const [donationAmount, setDonationAmount] = useState<number | null>(null);

  // Add-On Feature: Selected add-ons state
  const [addOnSelections, setAddOnSelections] = useState<AddOnSelection[]>([]);
  const addOnTotal = addOnSelections.reduce((sum, s) => sum + s.unitPrice * s.quantity, 0);

  // Phase 6A.137E: Collection state. Sponsor state retired in
  // 6A.157-fix-1 [1/3] (operator UAT 2026-06-01 — sponsorship is now a
  // separate flow on the event detail page; ticket purchase + sponsorship
  // no longer share a single Stripe session). Backend RsvpToEventCommand
  // still accepts the sponsor* fields for backward-compat with deployed
  // clients during rollout; the FE simply stops sending them.
  const [collectionAmount, setCollectionAmount] = useState<number | null>(null);
  const [collectionNotes, setCollectionNotes] = useState<string | null>(null);

  // Form state
  const [quantity, setQuantity] = useState(1);
  const [address, setAddress] = useState('');
  const [email, setEmail] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');

  // Phase 2: Seat selection state
  const isAssignedSeating = seatingMode === SeatingMode.AssignedSeating;
  const [selectedSeatIds, setSelectedSeatIds] = useState<string[]>([]);
  const [seatSessionId, setSeatSessionId] = useState<string>('');
  const [seatsConfirmed, setSeatsConfirmed] = useState(false);

  // Phase 7A.6B: WhatsApp opt-in state
  const [whatsAppEnabled, setWhatsAppEnabled] = useState(false);
  const [whatsAppPhone, setWhatsAppPhone] = useState('');

  // Session 21: Multi-attendee state
  // Phase 6A.43: Updated to use AgeCategory and Gender instead of age
  const [attendees, setAttendees] = useState<Array<{ name: string; ageCategory: AgeCategory | ''; gender: Gender | null; ticketTierId?: string | null }>>([
    { name: '', ageCategory: '', gender: null, ticketTierId: null },
  ]);

  // Validation state
  const [touched, setTouched] = useState({
    address: false,
    email: false,
    phoneNumber: false,
    attendees: [] as boolean[],
  });

  // Load profile for authenticated users
  useEffect(() => {
    if (user?.userId && !profile) {
      loadProfile(user.userId);
    }
  }, [user?.userId, profile, loadProfile]);

  // Auto-fill from profile for authenticated users
  useEffect(() => {
    if (user && profile) {
      setEmail(profile.email);
      setPhoneNumber(profile.phoneNumber || '');

      // Build address from location if available
      if (profile.location) {
        const addressParts = [
          profile.location.city,
          profile.location.state,
          profile.location.zipCode
        ].filter(Boolean);
        setAddress(addressParts.join(', '));
      }

      // Pre-populate first attendee with user's profile name
      // Note: AgeCategory and Gender must still be entered by user
      setAttendees([
        {
          name: `${profile.firstName} ${profile.lastName}`.trim(),
          ageCategory: '',
          gender: null,
          ticketTierId: null,
        },
      ]);
    }
  }, [user, profile]);

  // Session 21: Update quantity when attendees array changes (for submission)
  useEffect(() => {
    setQuantity(attendees.length);
  }, [attendees.length]);

  // Add attendee function
  const handleAddAttendee = () => {
    // Issue #51: Use event's configured max attendees per registration
    const maxAttendees = Math.min(maxAttendeesPerRegistration, spotsLeft);
    if (attendees.length < maxAttendees) {
      setAttendees([...attendees, { name: '', ageCategory: '', gender: null, ticketTierId: null }]);
      setTouched(prev => ({
        ...prev,
        attendees: [...prev.attendees, false],
      }));
    }
  };

  // Remove attendee function
  const handleRemoveAttendee = (index: number) => {
    if (attendees.length > 1) {
      const newAttendees = attendees.filter((_, i) => i !== index);
      setAttendees(newAttendees);
      setTouched(prev => ({
        ...prev,
        attendees: prev.attendees.filter((_, i) => i !== index),
      }));
    }
  };

  // Session 21: Update individual attendee
  // Phase 6A.43: Updated to handle AgeCategory and Gender
  const handleAttendeeChange = (index: number, field: 'name' | 'ageCategory' | 'gender' | 'ticketTierId', value: string | AgeCategory | Gender | null) => {
    const updated = [...attendees];
    if (field === 'name') {
      updated[index] = { ...updated[index], name: value as string };
    } else if (field === 'ageCategory') {
      updated[index] = { ...updated[index], ageCategory: value === '' ? '' : (value as AgeCategory) };
    } else if (field === 'gender') {
      updated[index] = { ...updated[index], gender: value === '' ? null : (value as Gender) };
    } else if (field === 'ticketTierId') {
      updated[index] = { ...updated[index], ticketTierId: value === '' ? null : (value as string) };
    }
    setAttendees(updated);
  };

  const handleAttendeeTouched = (index: number) => {
    const updatedTouched = [...touched.attendees];
    updatedTouched[index] = true;
    setTouched(prev => ({ ...prev, attendees: updatedTouched }));
  };

  // Phase 6D: Find applicable group pricing tier based on total attendee count
  const findApplicableTier = (): GroupPricingTierDto | null => {
    if (!effectiveGroupPricing || !groupPricingTiers || groupPricingTiers.length === 0) {
      return null;
    }

    const totalAttendees = quantity;
    const sortedTiers = [...groupPricingTiers].sort((a, b) => a.minAttendees - b.minAttendees);

    for (const tier of sortedTiers) {
      if (totalAttendees >= tier.minAttendees) {
        // If tier has no max (unlimited), it applies
        if (!tier.maxAttendees) {
          return tier;
        }
        // If tier has max and attendees are within range, it applies
        if (totalAttendees <= tier.maxAttendees) {
          return tier;
        }
      }
    }

    return null;
  };

  // Phase 8: Helper to check if event uses tiered ticketing
  const isTieredTicketing = ticketingMode === TicketingMode.Tiered && ticketTiers && ticketTiers.length > 0;
  // Phase 8: Active tiers only
  const activeTiers = isTieredTicketing ? ticketTiers.filter(t => t.isActive && t.availableQuantity > 0) : [];

  // Session 21 + Phase 6D + Phase 8: Calculate total price
  const calculateTotalPrice = (): number => {
    if (isFree) return 0;

    // Phase 8: Tiered ticketing (highest priority)
    if (isTieredTicketing && ticketTiers) {
      return attendees.reduce((total, attendee) => {
        if (!attendee.ticketTierId || attendee.ageCategory === '') return total;
        const tier = ticketTiers.find(t => t.id === attendee.ticketTierId);
        if (!tier) return total;
        if (tier.isFree) return total;
        if (attendee.ageCategory === AgeCategory.Child && tier.childPriceAmount != null) {
          return total + tier.childPriceAmount;
        }
        return total + tier.adultPriceAmount;
      }, 0);
    }

    // Phase 6D: Group tiered pricing
    if (effectiveGroupPricing && groupPricingTiers && groupPricingTiers.length > 0) {
      const applicableTier = findApplicableTier();
      if (applicableTier) {
        return applicableTier.pricePerPerson * quantity;
      }
      return 0; // No applicable tier found
    }

    // Session 21: Dual pricing (age category-based)
    // Phase 6A.43: Updated to use AgeCategory instead of age
    if (effectiveDualPricing && adultPrice && childPrice) {
      // Calculate based on attendee age categories
      return attendees.reduce((total, attendee) => {
        if (attendee.ageCategory === '') return total;
        return total + (attendee.ageCategory === AgeCategory.Child ? childPrice : adultPrice);
      }, 0);
    }

    // Legacy single pricing
    if (effectiveTicketPrice) {
      return effectiveTicketPrice * quantity;
    }

    return 0;
  };

  // Validation - BOTH authenticated and anonymous users need contact info
  // Phase 6A.43: Updated validation to use AgeCategory instead of age
  // GitHub Issue #30: Updated phone validation to require minimum 7 digits
  const errors = {
    address: '',
    email: touched.email && (!email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) ? 'Valid email is required' : '',
    phoneNumber: touched.phoneNumber ? validatePhoneNumber(phoneNumber).error || '' : '',
    attendees: attendees.map((attendee, index) => {
      if (!touched.attendees[index]) return { name: '', ageCategory: '', ticketTier: '' };
      return {
        name: !attendee.name.trim() ? 'Name is required' : '',
        ageCategory: attendee.ageCategory === '' ? 'Please select Adult or Child' : '',
        ticketTier: isTieredTicketing && !attendee.ticketTierId ? 'Please select a ticket tier' : '',
      };
    }),
  };

  // BOTH authenticated and anonymous users must provide all fields
  // Phase 6A.43: Updated to validate AgeCategory instead of age
  // GitHub Issue #30: Use centralized phone validation
  const isFormValid =
    email.trim() &&
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email) &&
    isValidPhoneNumber(phoneNumber) &&
    attendees.every(a => a.name.trim() && a.ageCategory !== '' && (!isTieredTicketing || a.ticketTierId));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // W5.D10.b defensive guard — even if the submit button's disabled state
    // Phase 6A.157-fix-1 [1/3] — sponsor-logo upload guard removed alongside
    // the in-registration sponsor block. Buyer-side logo upload now lives in
    // PurchaseSponsorshipPackageModal where it can be best-effort without
    // gating the parent submit path.

    // Mark all fields as touched for validation
    setTouched({
      address: true,
      email: true,
      phoneNumber: true,
      attendees: Array(quantity).fill(true),
    });

    if (!isFormValid) {
      return;
    }

    // Session 21: Prepare attendees array in new format
    // Phase 6A.43: Updated to use AgeCategory and Gender instead of age
    const attendeesData: AttendeeDto[] = attendees.map(a => ({
      name: a.name.trim(),
      ageCategory: a.ageCategory as AgeCategory,
      gender: a.gender,
      // Phase 8: Include ticket tier ID for tiered events
      ...(isTieredTicketing && a.ticketTierId && { ticketTierId: a.ticketTierId }),
    }));

    // Phase 6A.137D: Map add-on selections for bundled checkout
    const addOnSelectionsPayload = addOnSelections
      .filter(s => s.quantity > 0)
      .map(s => ({ definitionId: s.definitionId, quantity: s.quantity }));

    if (!user) {
      // Anonymous registration
      // Phase 6A.43: Use multi-attendee format with AgeCategory and Gender
      const anonymousData: AnonymousRegistrationRequest = {
        // Contact information
        address: address.trim(),
        email: email.trim(),
        phoneNumber: phoneNumber.trim(),
        // Phase 7A.6B: WhatsApp opt-in
        ...(whatsAppEnabled && whatsAppPhone && {
          whatsAppPhoneNumber: toE164(whatsAppPhone),
        }),
        // Quantity for multiple attendees
        quantity: attendeesData.length,
        // Attendees array with AgeCategory and Gender
        attendees: attendeesData,
        // Donation Feature: Include donation amount if provided
        ...(donationAmount && donationAmount > 0 && {
          donationAmount,
          donorName: attendeesData[0]?.name?.trim() || email.trim(),
        }),
      };

      await onSubmit(anonymousData);
    } else {
      // Authenticated registration with multi-attendee
      // Phase 6A.43: Updated to use AgeCategory and Gender
      const rsvpData: RsvpRequest = {
        userId: user.userId,
        quantity: attendeesData.length, // Include quantity based on number of attendees
        attendees: attendeesData,
        email: email.trim() || undefined,
        phoneNumber: phoneNumber.trim() || undefined,
        address: address.trim() || undefined,
        // Phase 2: Include seat hold session for assigned seating events
        ...(isAssignedSeating && seatSessionId && {
          seatSessionId,
          seatIds: selectedSeatIds,
        }),
        // Phase 7A.6B: WhatsApp opt-in
        ...(whatsAppEnabled && whatsAppPhone && {
          whatsAppPhoneNumber: toE164(whatsAppPhone),
        }),
        // Donation Feature: Include donation amount if provided
        ...(donationAmount && donationAmount > 0 && {
          donationAmount,
          donorName: user.fullName || undefined,
        }),
        // Phase 6A.137D: Include add-on selections for bundled checkout
        ...(addOnSelectionsPayload.length > 0 && {
          addOnSelections: addOnSelectionsPayload,
        }),
        // Phase 6A.137E: Include collection contribution for bundled checkout
        ...(collectionAmount && collectionAmount > 0 && {
          collectionAmount,
          collectionNotes: collectionNotes || undefined,
        }),
        // Phase 6A.157-fix-1 [1/3] — bundled sponsor-during-registration
        // payload retired; sponsorship now ships as its own flow on the event
        // detail page. Backend optional fields remain for backward-compat with
        // deployed clients (handler null-skips when absent).
      };

      await onSubmit(rsvpData);
    }
  };

  const totalPrice = calculateTotalPrice();
  const applicableTier = effectiveGroupPricing ? findApplicableTier() : null;

  // Issue #51: Use event's configured max attendees per registration
    const maxAttendees = Math.min(maxAttendeesPerRegistration, spotsLeft);
  const canAddMore = attendees.length < maxAttendees;

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Session 21: Individual Attendee Fields */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <h4 className="text-sm font-semibold text-neutral-700">Attendee Information</h4>
          <span className="text-xs text-neutral-500">
            {attendees.length} of {maxAttendees} spots
          </span>
        </div>
        {!user && (
          <p className="text-xs text-neutral-500 mb-4">
            Please provide name, age category, and optionally gender for each attendee
          </p>
        )}
        {user && profile && (
          <p className="text-xs text-neutral-500 mb-4">
            First attendee pre-populated from your profile. You can edit if needed.
          </p>
        )}

        <div className="space-y-4">
          {attendees.map((attendee, index) => (
            <div key={index} className="p-4 bg-neutral-50 rounded-lg border border-neutral-200">
              <div className="flex items-center justify-between mb-3">
                <h5 className="text-sm font-medium text-neutral-700">
                  Attendee {index + 1}
                </h5>
                {/* Show remove button for all except the first attendee */}
                {index > 0 && (
                  <button
                    type="button"
                    onClick={() => handleRemoveAttendee(index)}
                    disabled={isProcessing}
                    className="text-red-500 hover:text-red-700 p-1 rounded-full hover:bg-red-50 transition-colors"
                    title="Remove attendee"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                )}
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* Name */}
                <div>
                  <label className="block text-sm font-medium mb-2 text-neutral-700">
                    Full Name <span className="text-red-500">*</span>
                  </label>
                  <Input
                    type="text"
                    value={attendee.name}
                    onChange={(e) => handleAttendeeChange(index, 'name', e.target.value)}
                    onBlur={() => handleAttendeeTouched(index)}
                    error={!!errors.attendees[index]?.name}
                    disabled={isProcessing}
                    placeholder="Enter full name"
                    className="w-full"
                  />
                  {errors.attendees[index]?.name && (
                    <p className="text-xs text-red-600 mt-1">{errors.attendees[index].name}</p>
                  )}
                </div>

                {/* Phase 6A.43: Age Category - Radio buttons */}
                <div>
                  <label className="block text-sm font-medium mb-2 text-neutral-700">
                    Age Category <span className="text-red-500">*</span>
                    {effectiveDualPricing && (
                      <span className="text-xs text-neutral-500 ml-2">
                        (Child = child price)
                      </span>
                    )}
                  </label>
                  <div className="flex gap-4">
                    <label className="flex items-center gap-2 cursor-pointer">
                      <input
                        type="radio"
                        name={`ageCategory-${index}`}
                        value={AgeCategory.Adult}
                        checked={attendee.ageCategory === AgeCategory.Adult}
                        onChange={() => handleAttendeeChange(index, 'ageCategory', AgeCategory.Adult)}
                        onBlur={() => handleAttendeeTouched(index)}
                        disabled={isProcessing}
                        className="w-4 h-4 text-orange-600 focus:ring-orange-500"
                      />
                      <span className="text-sm text-neutral-700">Adult</span>
                    </label>
                    <label className="flex items-center gap-2 cursor-pointer">
                      <input
                        type="radio"
                        name={`ageCategory-${index}`}
                        value={AgeCategory.Child}
                        checked={attendee.ageCategory === AgeCategory.Child}
                        onChange={() => handleAttendeeChange(index, 'ageCategory', AgeCategory.Child)}
                        onBlur={() => handleAttendeeTouched(index)}
                        disabled={isProcessing}
                        className="w-4 h-4 text-orange-600 focus:ring-orange-500"
                      />
                      <span className="text-sm text-neutral-700">Child</span>
                    </label>
                  </div>
                  {errors.attendees[index]?.ageCategory && (
                    <p className="text-xs text-red-600 mt-1">{errors.attendees[index].ageCategory}</p>
                  )}
                </div>

                {/* Phase 6A.43: Gender - Dropdown (optional) */}
                <div>
                  <label className="block text-sm font-medium mb-2 text-neutral-700">
                    Gender <span className="text-xs text-neutral-400">(optional)</span>
                  </label>
                  <select
                    value={attendee.gender ?? ''}
                    onChange={(e) => handleAttendeeChange(index, 'gender', e.target.value === '' ? null : Number(e.target.value))}
                    disabled={isProcessing}
                    className="w-full px-3 py-2 border border-neutral-300 rounded-md focus:outline-none focus:ring-2 focus:ring-orange-500 focus:border-transparent text-sm"
                  >
                    <option value="">-- Select --</option>
                    <option value={Gender.Male}>Male</option>
                    <option value={Gender.Female}>Female</option>
                    <option value={Gender.Other}>Other</option>
                  </select>
                </div>

                {/* Phase 8: Ticket Tier Selector (shown for tiered events) */}
                {isTieredTicketing && activeTiers.length > 0 && (
                  <div>
                    <label className="block text-sm font-medium mb-2 text-neutral-700">
                      Ticket Tier <span className="text-red-500">*</span>
                    </label>
                    <select
                      value={attendee.ticketTierId ?? ''}
                      onChange={(e) => handleAttendeeChange(index, 'ticketTierId', e.target.value)}
                      onBlur={() => handleAttendeeTouched(index)}
                      disabled={isProcessing}
                      className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-orange-500 focus:border-transparent text-sm ${
                        errors.attendees[index]?.ticketTier ? 'border-red-500' : 'border-neutral-300'
                      }`}
                    >
                      <option value="">-- Select Tier --</option>
                      {activeTiers.map((tier) => (
                        <option key={tier.id} value={tier.id}>
                          {tier.name}
                          {tier.isFree
                            ? ' (Free)'
                            : attendee.ageCategory === AgeCategory.Child && tier.childPriceAmount != null
                            ? ` ($${tier.childPriceAmount})`
                            : ` ($${tier.adultPriceAmount})`}
                          {' '} — {tier.availableQuantity} left
                        </option>
                      ))}
                    </select>
                    {errors.attendees[index]?.ticketTier && (
                      <p className="text-xs text-red-600 mt-1">{errors.attendees[index].ticketTier}</p>
                    )}
                  </div>
                )}
              </div>
            </div>
          ))}

          {/* Add Attendee Button */}
          {canAddMore && (
            <button
              type="button"
              onClick={handleAddAttendee}
              disabled={isProcessing}
              className="w-full py-3 px-4 border-2 border-dashed border-neutral-300 rounded-lg text-neutral-600 hover:border-orange-400 hover:text-orange-600 hover:bg-orange-50 transition-colors flex items-center justify-center gap-2"
            >
              <Plus className="w-5 h-5" />
              <span className="font-medium">Add Attendee</span>
            </button>
          )}
        </div>
      </div>

      {/* Phase 2: Seat Selection for Assigned Seating events */}
      {isAssignedSeating && user && (
        <div className="border-t pt-4">
          <h4 className="text-sm font-semibold mb-3 text-neutral-700">Select Your Seats</h4>
          {!seatsConfirmed ? (
            <SeatSelector
              eventId={eventId}
              maxSeats={attendees.length}
              userId={user.userId}
              onSeatsConfirmed={(seatIds, sessionId) => {
                setSelectedSeatIds(seatIds);
                setSeatSessionId(sessionId);
                setSeatsConfirmed(true);
              }}
              onCancel={() => {
                setSelectedSeatIds([]);
                setSeatSessionId('');
                setSeatsConfirmed(false);
              }}
            />
          ) : (
            <div className="p-4 bg-green-50 border border-green-200 rounded-lg">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-green-800">
                    {selectedSeatIds.length} seat{selectedSeatIds.length !== 1 ? 's' : ''} selected
                  </p>
                  <p className="text-xs text-green-600 mt-1">
                    Your seats are held for 10 minutes. Complete registration to confirm.
                  </p>
                </div>
                <button
                  type="button"
                  onClick={() => setSeatsConfirmed(false)}
                  className="text-sm text-green-700 underline hover:text-green-900"
                >
                  Change seats
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Contact Information (anonymous users only, or editable for authenticated) */}
      {!user && (
        <div className="border-t pt-4">
          <h4 className="text-sm font-semibold mb-3 text-neutral-700">Contact Information</h4>
          <p className="text-xs text-neutral-500 mb-4">
            We'll use this information to send you event updates and confirmations.
          </p>

          {/* Address */}
          <div className="mb-4">
            <label className="block text-sm font-medium mb-2 text-neutral-700">
              Address <span className="text-xs text-neutral-500 font-normal">(optional)</span>
            </label>
            <Input
              type="text"
              value={address}
              onChange={(e) => setAddress(e.target.value)}
              onBlur={() => setTouched({ ...touched, address: true })}
              error={!!errors.address}
              disabled={isProcessing}
              placeholder="Enter your address"
              className="w-full"
            />
            {errors.address && (
              <p className="text-xs text-red-600 mt-1">{errors.address}</p>
            )}
          </div>

          {/* Email */}
          <div className="mb-4">
            <label className="block text-sm font-medium mb-2 text-neutral-700">
              Email <span className="text-red-500">*</span>
            </label>
            <Input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              onBlur={() => setTouched({ ...touched, email: true })}
              error={!!errors.email}
              disabled={isProcessing}
              placeholder="your.email@example.com"
              className="w-full"
            />
            {errors.email && (
              <p className="text-xs text-red-600 mt-1">{errors.email}</p>
            )}
          </div>

          {/* Phone Number - GitHub Issue #30: PhoneInput restricts invalid characters */}
          <div>
            <label className="block text-sm font-medium mb-2 text-neutral-700">
              Phone Number <span className="text-red-500">*</span>
            </label>
            <PhoneInput
              value={phoneNumber}
              onChange={setPhoneNumber}
              onBlur={() => setTouched({ ...touched, phoneNumber: true })}
              error={!!errors.phoneNumber}
              disabled={isProcessing}
              placeholder="+1-234-567-8901"
              className="w-full"
            />
            {errors.phoneNumber && (
              <p className="text-xs text-red-600 mt-1">{errors.phoneNumber}</p>
            )}
          </div>
        </div>
      )}

      {/* Authenticated User Contact Info - EDITABLE for registration */}
      {user && profile && (
        <div className="border-t pt-4">
          <h4 className="text-sm font-semibold mb-3 text-neutral-700">Contact Information</h4>
          <p className="text-xs text-neutral-500 mb-4">
            Please verify and update your contact details for this event registration.
          </p>

          {/* Email - Pre-filled but editable */}
          <div className="mb-4">
            <label className="block text-sm font-medium mb-2 text-neutral-700">
              Email <span className="text-red-500">*</span>
            </label>
            <Input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              onBlur={() => setTouched({ ...touched, email: true })}
              error={!!errors.email}
              disabled={isProcessing}
              placeholder="your.email@example.com"
              className="w-full"
            />
            {errors.email && (
              <p className="text-xs text-red-600 mt-1">{errors.email}</p>
            )}
          </div>

          {/* Phone Number - Pre-filled but editable - GitHub Issue #30: PhoneInput restricts invalid characters */}
          <div className="mb-4">
            <label className="block text-sm font-medium mb-2 text-neutral-700">
              Phone Number <span className="text-red-500">*</span>
            </label>
            <PhoneInput
              value={phoneNumber}
              onChange={setPhoneNumber}
              onBlur={() => setTouched({ ...touched, phoneNumber: true })}
              error={!!errors.phoneNumber}
              disabled={isProcessing}
              placeholder="+1-234-567-8901"
              className="w-full"
            />
            {errors.phoneNumber && (
              <p className="text-xs text-red-600 mt-1">{errors.phoneNumber}</p>
            )}
          </div>

          {/* Address - Pre-filled but editable */}
          <div>
            <label className="block text-sm font-medium mb-2 text-neutral-700">
              Address <span className="text-xs text-neutral-500 font-normal">(optional)</span>
            </label>
            <Input
              type="text"
              value={address}
              onChange={(e) => setAddress(e.target.value)}
              onBlur={() => setTouched({ ...touched, address: true })}
              error={!!errors.address}
              disabled={isProcessing}
              placeholder="Enter your address"
              className="w-full"
            />
            {errors.address && (
              <p className="text-xs text-red-600 mt-1">{errors.address}</p>
            )}
          </div>

          <p className="text-xs text-neutral-500 mt-3">
            Pre-filled from your profile. Please verify and update if needed.
          </p>
        </div>
      )}

      {/* Phase 7A.6B: WhatsApp opt-in during event registration */}
      <div className="border-t pt-4">
        <WhatsAppInlineOptIn
          enabled={whatsAppEnabled}
          onEnabledChange={(enabled) => {
            setWhatsAppEnabled(enabled);
            if (!enabled) setWhatsAppPhone('');
          }}
          phoneNumber={whatsAppPhone}
          onPhoneNumberChange={setWhatsAppPhone}
          disabled={isProcessing}
          description="Get event reminders and updates via WhatsApp for this registration."
        />
      </div>

      {/* Donation Feature: Optional donation add-on */}
      {donationConfig?.isEnabled === true && (
        <DonationOptionInForm
          donationConfig={donationConfig}
          onDonationChange={setDonationAmount}
        />
      )}

      {/* Add-On Feature: Optional add-ons during registration */}
      {addOnConfig?.isEnabled === true && addOnConfig?.availableDuringRegistration === true && (
        <AddOnOptionInForm
          eventId={eventId}
          addOnConfig={addOnConfig}
          onAddOnsChange={setAddOnSelections}
        />
      )}

      {/* Phase 6A.137E: Optional collection contribution during registration */}
      {collectionConfig?.isEnabled === true && (
        <CollectionOptionInForm
          collectionConfig={collectionConfig}
          onCollectionChange={(amount, notes) => {
            setCollectionAmount(amount);
            setCollectionNotes(notes);
          }}
        />
      )}

      {/*
       * Phase 6A.137E: Optional money sponsorship during registration —
       * RETIRED 6A.157-fix-1 [1/3] (operator UAT 2026-06-01). Sponsorship now
       * lives in its own flow on the event detail page (Phase 6A.157 buyer
       * purchase / SponsorSection custom-amount form). Backend optional
       * fields preserved for backward-compat; FE stops sending them.
       */}

      {/* Total Price with Group/Dual/Single Pricing Breakdown */}
      {!isFree && totalPrice > 0 && (
        <div className="p-4 bg-neutral-50 rounded-lg border-t-2 border-orange-500">
          {/* Phase 6D: Group Tiered Pricing Breakdown */}
          {effectiveGroupPricing && applicableTier && (
            <div className="mb-3 space-y-2 text-sm">
              <h5 className="font-medium text-neutral-700">Group Pricing Applied:</h5>
              <div className="flex justify-between items-center p-3 bg-white rounded-lg border border-orange-200">
                <div>
                  <span className="font-medium text-orange-600">{applicableTier.tierRange}</span>
                  <span className="text-neutral-600 ml-2">attendees</span>
                </div>
                <div className="text-right">
                  <div className="text-sm font-medium text-neutral-700">
                    ${applicableTier.pricePerPerson.toFixed(2)} per person
                  </div>
                  <div className="text-xs text-neutral-500">
                    {quantity} × ${applicableTier.pricePerPerson.toFixed(2)}
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Session 21: Dual Pricing Breakdown */}
          {/* Phase 6A.43: Updated to use AgeCategory instead of age */}
          {effectiveDualPricing && adultPrice && childPrice && (
            <div className="mb-3 space-y-2 text-sm">
              <h5 className="font-medium text-neutral-700">Price Breakdown:</h5>
              {attendees.map((attendee, index) => {
                if (attendee.ageCategory === '') return null;
                const isChild = attendee.ageCategory === AgeCategory.Child;
                const price = isChild ? childPrice : adultPrice;
                const priceType = isChild ? 'Child' : 'Adult';
                const genderLabel = attendee.gender ? `, ${Gender[attendee.gender]}` : '';
                return (
                  <div key={index} className="flex justify-between text-xs text-neutral-600">
                    <span>{attendee.name || `Attendee ${index + 1}`} ({priceType}{genderLabel})</span>
                    <span>${price.toFixed(2)}</span>
                  </div>
                );
              })}
            </div>
          )}

          {/* Phase 6A.137F: Donation section with clear label */}
          {donationAmount && donationAmount > 0 && (
            <div className="border-t pt-2 mt-2">
              <span className="text-xs font-medium text-neutral-500 uppercase tracking-wide">Donation</span>
              <div className="flex justify-between items-center text-sm text-neutral-600 mt-1">
                <span>Voluntary donation</span>
                <span>${donationAmount.toFixed(2)}</span>
              </div>
            </div>
          )}

          {/* Phase 6A.137F: Add-ons section — only show items with quantity > 0 (Bug 7) */}
          {addOnSelections.filter(s => s.quantity > 0).length > 0 && (
            <div className="border-t pt-2 mt-2">
              <span className="text-xs font-medium text-neutral-500 uppercase tracking-wide">Add-ons</span>
              <div className="mt-1 space-y-1">
                {addOnSelections.filter(s => s.quantity > 0).map((s) => (
                  <div key={s.definitionId} className="flex justify-between items-center text-sm text-neutral-600">
                    <span>{s.name} x{s.quantity}</span>
                    <span>${(s.unitPrice * s.quantity).toFixed(2)}</span>
                  </div>
                ))}
                {addOnSelections.filter(s => s.quantity > 0).length > 1 && (
                  <div className="flex justify-between items-center text-xs text-neutral-500 pt-1">
                    <span>Add-ons subtotal</span>
                    <span>${addOnTotal.toFixed(2)}</span>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Phase 6A.137F: Collection contribution section */}
          {collectionAmount && collectionAmount > 0 && (
            <div className="border-t pt-2 mt-2">
              <span className="text-xs font-medium text-neutral-500 uppercase tracking-wide">Collection</span>
              <div className="flex justify-between items-center text-sm text-neutral-600 mt-1">
                <span>Event contribution</span>
                <span>${collectionAmount.toFixed(2)}</span>
              </div>
            </div>
          )}

          {/* Phase 6A.137F Sponsorship section retired in 6A.157-fix-1 [1/3] */}

          <div className="flex justify-between items-center border-t pt-3">
            <span className="text-base font-medium text-neutral-700">Total</span>
            <span className="text-xl font-bold" style={{ color: '#8B1538' }}>
              ${(totalPrice + (donationAmount || 0) + addOnTotal + (collectionAmount || 0)).toFixed(2)}
            </span>
          </div>
        </div>
      )}

      {/* Error Message */}
      {error && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
          <p className="text-sm text-red-600">{error}</p>
        </div>
      )}

      {/* Submit Button — 6A.157-fix-1 [1/3] drops the W5.D10.b sponsor-upload
          gate alongside the rest of the in-registration sponsor block. */}
      <Button
        type="submit"
        disabled={isProcessing || !isFormValid}
        className="w-full text-lg py-6"
        style={{ background: '#FF7900' }}
      >
        {isProcessing ? (
          <>
            <Clock className="h-5 w-5 mr-2 animate-spin" />
            Processing...
          </>
        ) : isFree ? (
          'Register for Free'
        ) : (
          'Continue to Payment'
        )}
      </Button>

      {!user && (
        <p className="text-sm text-center text-neutral-500">
          Have an account? <a href="/login" className="text-orange-600 hover:underline">Sign in</a> to register faster
        </p>
      )}
    </form>
  );
}
