'use client';

import { useState, useEffect } from 'react';
import { Input } from '@/presentation/components/ui/Input';
import { Button } from '@/presentation/components/ui/Button';
import { Clock } from 'lucide-react';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import type { AnonymousRegistrationRequest, AttendeeDto, RsvpRequest } from '@/infrastructure/api/types/events.types';

/**
 * Event Registration Form Component
 * Session 21: Multi-attendee registration with individual names and ages
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

  // Session 21: Multi-attendee state (array of { name, age } objects)
  const [attendees, setAttendees] = useState<Array<{ name: string; age: number | '' }>>([
    { name: '', age: '' },
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
      // Note: Age must still be entered by user as it's not stored in profile
      setAttendees([
        {
          name: `${profile.firstName} ${profile.lastName}`.trim(),
          age: '',
        },
      ]);
    }
  }, [user, profile]);

  // Session 21: Update attendee array when quantity changes
  useEffect(() => {
    const newAttendees = Array.from({ length: quantity }, (_, index) => {
      // Preserve existing attendee data if available
      return attendees[index] || { name: '', age: '' };
    });
    setAttendees(newAttendees);
    setTouched(prev => ({
      ...prev,
      attendees: Array(quantity).fill(false),
    }));
  }, [quantity]);

  // Session 21: Update individual attendee
  const handleAttendeeChange = (index: number, field: 'name' | 'age', value: string | number) => {
    const updated = [...attendees];
    updated[index] = {
      ...updated[index],
      [field]: field === 'age' ? (value === '' ? '' : Number(value)) : value,
    };
    setAttendees(updated);
  };

  const handleAttendeeTouched = (index: number) => {
    const updatedTouched = [...touched.attendees];
    updatedTouched[index] = true;
    setTouched(prev => ({ ...prev, attendees: updatedTouched }));
  };

  // Session 21: Calculate total price with dual pricing support
  const calculateTotalPrice = (): number => {
    if (isFree || !ticketPrice) return 0;

    if (hasDualPricing && adultPrice && childPrice && childAgeLimit) {
      // Calculate based on attendee ages
      return attendees.reduce((total, attendee) => {
        if (attendee.age === '' || attendee.age === 0) return total;
        const age = Number(attendee.age);
        return total + (age < childAgeLimit ? childPrice : adultPrice);
      }, 0);
    }

    // Legacy single pricing
    return (ticketPrice || 0) * quantity;
  };

  // Validation
  const errors = {
    address: touched.address && !user && !address.trim() ? 'Address is required' : '',
    email: touched.email && !user && (!email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) ? 'Valid email is required' : '',
    phoneNumber: touched.phoneNumber && !user && (!phoneNumber.trim() || !/^\+?[\d\s\-()]+$/.test(phoneNumber)) ? 'Valid phone number is required' : '',
    attendees: attendees.map((attendee, index) => {
      if (!touched.attendees[index]) return { name: '', age: '' };
      return {
        name: !attendee.name.trim() ? 'Name is required' : '',
        age: !attendee.age || attendee.age < 1 || attendee.age > 120 ? 'Valid age is required (1-120)' : '',
      };
    }),
  };

  const isFormValid = !user
    ? address.trim() &&
      email.trim() &&
      /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email) &&
      phoneNumber.trim() &&
      /^\+?[\d\s\-()]+$/.test(phoneNumber) &&
      attendees.every(a => a.name.trim() && a.age && a.age >= 1 && a.age <= 120)
    : attendees.every(a => a.name.trim() && a.age && a.age >= 1 && a.age <= 120);

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
    const attendeesData: AttendeeDto[] = attendees.map(a => ({
      name: a.name.trim(),
      age: Number(a.age),
    }));

    if (!user) {
      // Anonymous registration with multi-attendee
      const anonymousData: AnonymousRegistrationRequest = {
        attendees: attendeesData,
        address: address.trim(),
        email: email.trim(),
        phoneNumber: phoneNumber.trim(),
      };

      await onSubmit(anonymousData);
    } else {
      // Authenticated registration with multi-attendee
      const rsvpData: RsvpRequest = {
        userId: user.userId,
        attendees: attendeesData,
        email: email.trim() || undefined,
        phoneNumber: phoneNumber.trim() || undefined,
        address: address.trim() || undefined,
      };

      await onSubmit(rsvpData);
    }
  };

  const totalPrice = calculateTotalPrice();

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
            Please provide name and age for each attendee
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

                {/* Age */}
                <div>
                  <label className="block text-sm font-medium mb-2 text-neutral-700">
                    Age <span className="text-red-500">*</span>
                    {hasDualPricing && childAgeLimit && (
                      <span className="text-xs text-neutral-500 ml-2">
                        (Under {childAgeLimit} = child price)
                      </span>
                    )}
                  </label>
                  <Input
                    type="number"
                    min="1"
                    max="120"
                    value={attendee.age}
                    onChange={(e) => handleAttendeeChange(index, 'age', e.target.value ? parseInt(e.target.value) : '')}
                    onBlur={() => handleAttendeeTouched(index)}
                    error={!!errors.attendees[index]?.age}
                    disabled={isProcessing}
                    placeholder="Age"
                    className="w-full"
                  />
                  {errors.attendees[index]?.age && (
                    <p className="text-xs text-red-600 mt-1">{errors.attendees[index].age}</p>
                  )}
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

      {/* Authenticated User Contact Info Display */}
      {user && profile && (
        <div className="border-t pt-4">
          <h4 className="text-sm font-semibold mb-3 text-neutral-700">Contact Details</h4>
          <div className="bg-neutral-50 p-4 rounded-lg space-y-2 text-sm">
            <div>
              <span className="font-medium">Email:</span> {profile.email}
            </div>
            {profile.phoneNumber && (
              <div>
                <span className="font-medium">Phone:</span> {profile.phoneNumber}
              </div>
            )}
            {address && (
              <div>
                <span className="font-medium">Address:</span> {address}
              </div>
            )}
          </div>
          <p className="text-xs text-neutral-500 mt-2">
            Contact details from your profile
          </p>
        </div>
      )}

      {/* Total Price with Dual Pricing Breakdown */}
      {!isFree && totalPrice > 0 && (
        <div className="p-4 bg-neutral-50 rounded-lg border-t-2 border-orange-500">
          {hasDualPricing && adultPrice && childPrice && childAgeLimit && (
            <div className="mb-3 space-y-2 text-sm">
              <h5 className="font-medium text-neutral-700">Price Breakdown:</h5>
              {attendees.map((attendee, index) => {
                if (!attendee.age) return null;
                const age = Number(attendee.age);
                const price = age < childAgeLimit ? childPrice : adultPrice;
                const priceType = age < childAgeLimit ? 'Child' : 'Adult';
                return (
                  <div key={index} className="flex justify-between text-xs text-neutral-600">
                    <span>{attendee.name || `Attendee ${index + 1}`} ({priceType}, Age {age})</span>
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
