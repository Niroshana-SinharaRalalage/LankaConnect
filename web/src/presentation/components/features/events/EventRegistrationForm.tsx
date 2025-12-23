'use client';

import { useState, useEffect } from 'react';
import { Input } from '@/presentation/components/ui/Input';
import { Button } from '@/presentation/components/ui/Button';
import { Clock } from 'lucide-react';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import type { AnonymousRegistrationRequest, AttendeeDto, RsvpRequest, GroupPricingTierDto } from '@/infrastructure/api/types/events.types';
import { AgeCategory, Gender } from '@/infrastructure/api/types/events.types';

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
  isProcessing,
  onSubmit,
  error,
}: EventRegistrationFormProps) {
  const { user } = useAuthStore();
  const { profile, loadProfile } = useProfileStore();

  // Form state
  const [quantity, setQuantity] = useState(1);
  const [address, setAddress] = useState('');
  const [email, setEmail] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');

  // Session 21: Multi-attendee state
  // Phase 6A.43: Updated to use AgeCategory and Gender instead of age
  const [attendees, setAttendees] = useState<Array<{ name: string; ageCategory: AgeCategory | ''; gender: Gender | null }>>([
    { name: '', ageCategory: '', gender: null },
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
        },
      ]);
    }
  }, [user, profile]);

  // Session 21: Update attendee array when quantity changes
  useEffect(() => {
    const newAttendees = Array.from({ length: quantity }, (_, index) => {
      // Preserve existing attendee data if available
      return attendees[index] || { name: '', ageCategory: '', gender: null };
    });
    setAttendees(newAttendees);
    setTouched(prev => ({
      ...prev,
      attendees: Array(quantity).fill(false),
    }));
  }, [quantity]);

  // Session 21: Update individual attendee
  // Phase 6A.43: Updated to handle AgeCategory and Gender
  const handleAttendeeChange = (index: number, field: 'name' | 'ageCategory' | 'gender', value: string | AgeCategory | Gender | null) => {
    const updated = [...attendees];
    if (field === 'name') {
      updated[index] = { ...updated[index], name: value as string };
    } else if (field === 'ageCategory') {
      updated[index] = { ...updated[index], ageCategory: value === '' ? '' : (value as AgeCategory) };
    } else if (field === 'gender') {
      updated[index] = { ...updated[index], gender: value === '' ? null : (value as Gender) };
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
    if (!hasGroupPricing || !groupPricingTiers || groupPricingTiers.length === 0) {
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

  // Session 21 + Phase 6D: Calculate total price with group/dual/single pricing support
  const calculateTotalPrice = (): number => {
    if (isFree) return 0;

    // Phase 6D: Group tiered pricing (highest priority)
    if (hasGroupPricing && groupPricingTiers && groupPricingTiers.length > 0) {
      const applicableTier = findApplicableTier();
      if (applicableTier) {
        return applicableTier.pricePerPerson * quantity;
      }
      return 0; // No applicable tier found
    }

    // Session 21: Dual pricing (age category-based)
    // Phase 6A.43: Updated to use AgeCategory instead of age
    if (hasDualPricing && adultPrice && childPrice) {
      // Calculate based on attendee age categories
      return attendees.reduce((total, attendee) => {
        if (attendee.ageCategory === '') return total;
        return total + (attendee.ageCategory === AgeCategory.Child ? childPrice : adultPrice);
      }, 0);
    }

    // Legacy single pricing
    if (ticketPrice) {
      return ticketPrice * quantity;
    }

    return 0;
  };

  // Validation - BOTH authenticated and anonymous users need contact info
  // Phase 6A.43: Updated validation to use AgeCategory instead of age
  const errors = {
    address: touched.address && !address.trim() ? 'Address is required' : '',
    email: touched.email && (!email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) ? 'Valid email is required' : '',
    phoneNumber: touched.phoneNumber && (!phoneNumber.trim() || !/^\+?[\d\s\-()]+$/.test(phoneNumber)) ? 'Valid phone number is required' : '',
    attendees: attendees.map((attendee, index) => {
      if (!touched.attendees[index]) return { name: '', ageCategory: '' };
      return {
        name: !attendee.name.trim() ? 'Name is required' : '',
        ageCategory: attendee.ageCategory === '' ? 'Please select Adult or Child' : '',
      };
    }),
  };

  // BOTH authenticated and anonymous users must provide all fields
  // Phase 6A.43: Updated to validate AgeCategory instead of age
  const isFormValid =
    address.trim() &&
    email.trim() &&
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email) &&
    phoneNumber.trim() &&
    /^\+?[\d\s\-()]+$/.test(phoneNumber) &&
    attendees.every(a => a.name.trim() && a.ageCategory !== '');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

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
    }));

    if (!user) {
      // Anonymous registration
      // Phase 6A.43: Use multi-attendee format with AgeCategory and Gender
      const anonymousData: AnonymousRegistrationRequest = {
        // Contact information
        address: address.trim(),
        email: email.trim(),
        phoneNumber: phoneNumber.trim(),
        // Quantity for multiple attendees
        quantity: attendeesData.length,
        // Attendees array with AgeCategory and Gender
        attendees: attendeesData,
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
      };

      await onSubmit(rsvpData);
    }
  };

  const totalPrice = calculateTotalPrice();
  const applicableTier = hasGroupPricing ? findApplicableTier() : null;

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Quantity Selector */}
      <div>
        <label className="block text-sm font-medium mb-2 text-neutral-700">
          Number of Attendees
        </label>
        <Input
          type="number"
          min="1"
          max={Math.min(10, spotsLeft)}
          value={quantity}
          onChange={(e) => setQuantity(parseInt(e.target.value) || 1)}
          disabled={isProcessing}
          className="w-full"
        />
        <p className="text-xs text-neutral-500 mt-1">
          You'll provide name and age for each attendee below
        </p>
      </div>

      {/* Session 21: Individual Attendee Fields */}
      <div className="border-t pt-4">
        <h4 className="text-sm font-semibold mb-3 text-neutral-700">Attendee Information</h4>
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
              <h5 className="text-sm font-medium mb-3 text-neutral-700">
                Attendee {index + 1}
              </h5>
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
                    {hasDualPricing && (
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
              </div>
            </div>
          ))}
        </div>
      </div>

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
              Address <span className="text-red-500">*</span>
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

          {/* Phone Number */}
          <div>
            <label className="block text-sm font-medium mb-2 text-neutral-700">
              Phone Number <span className="text-red-500">*</span>
            </label>
            <Input
              type="tel"
              value={phoneNumber}
              onChange={(e) => setPhoneNumber(e.target.value)}
              onBlur={() => setTouched({ ...touched, phoneNumber: true })}
              error={!!errors.phoneNumber}
              disabled={isProcessing}
              placeholder="+1-123-456-7890"
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

          {/* Phone Number - Pre-filled but editable */}
          <div className="mb-4">
            <label className="block text-sm font-medium mb-2 text-neutral-700">
              Phone Number <span className="text-red-500">*</span>
            </label>
            <Input
              type="tel"
              value={phoneNumber}
              onChange={(e) => setPhoneNumber(e.target.value)}
              onBlur={() => setTouched({ ...touched, phoneNumber: true })}
              error={!!errors.phoneNumber}
              disabled={isProcessing}
              placeholder="+1-123-456-7890"
              className="w-full"
            />
            {errors.phoneNumber && (
              <p className="text-xs text-red-600 mt-1">{errors.phoneNumber}</p>
            )}
          </div>

          {/* Address - Pre-filled but editable */}
          <div>
            <label className="block text-sm font-medium mb-2 text-neutral-700">
              Address <span className="text-red-500">*</span>
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

      {/* Total Price with Group/Dual/Single Pricing Breakdown */}
      {!isFree && totalPrice > 0 && (
        <div className="p-4 bg-neutral-50 rounded-lg border-t-2 border-orange-500">
          {/* Phase 6D: Group Tiered Pricing Breakdown */}
          {hasGroupPricing && applicableTier && (
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
          {hasDualPricing && adultPrice && childPrice && (
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

          <div className="flex justify-between items-center border-t pt-3">
            <span className="text-base font-medium text-neutral-700">Total</span>
            <span className="text-xl font-bold" style={{ color: '#8B1538' }}>
              ${totalPrice.toFixed(2)}
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

      {/* Submit Button */}
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
