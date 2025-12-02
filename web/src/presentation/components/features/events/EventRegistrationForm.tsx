'use client';

import { useState, useEffect } from 'react';
import { Input } from '@/presentation/components/ui/Input';
import { Button } from '@/presentation/components/ui/Button';
import { Clock } from 'lucide-react';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import type { AnonymousRegistrationRequest } from '@/infrastructure/api/types/events.types';

/**
 * Event Registration Form Component
 * Supports both anonymous and authenticated registration flows
 * - Anonymous users: Fill in all contact details
 * - Authenticated users: Details auto-filled from profile, can edit if needed
 */
interface EventRegistrationFormProps {
  eventId: string;
  spotsLeft: number;
  isFree: boolean;
  ticketPrice?: number;
  isProcessing: boolean;
  onSubmit: (data: AnonymousRegistrationRequest | { userId: string; quantity: number }) => Promise<void>;
  error?: string | null;
}

export function EventRegistrationForm({
  eventId,
  spotsLeft,
  isFree,
  ticketPrice,
  isProcessing,
  onSubmit,
  error,
}: EventRegistrationFormProps) {
  const { user } = useAuthStore();
  const { profile, loadProfile } = useProfileStore();

  // Form state
  const [quantity, setQuantity] = useState(1);
  const [name, setName] = useState('');
  const [age, setAge] = useState<number | ''>('');
  const [address, setAddress] = useState('');
  const [email, setEmail] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');

  // Validation state
  const [touched, setTouched] = useState({
    name: false,
    age: false,
    address: false,
    email: false,
    phoneNumber: false,
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
      setName(`${profile.firstName} ${profile.lastName}`.trim());
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
    }
  }, [user, profile]);

  // Validation
  const errors = {
    name: touched.name && !name.trim() ? 'Name is required' : '',
    age: touched.age && (!age || age < 1 || age > 120) ? 'Valid age is required (1-120)' : '',
    address: touched.address && !address.trim() ? 'Address is required' : '',
    email: touched.email && (!email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) ? 'Valid email is required' : '',
    phoneNumber: touched.phoneNumber && (!phoneNumber.trim() || !/^\+?[\d\s\-()]+$/.test(phoneNumber)) ? 'Valid phone number is required' : '',
  };

  const isFormValid = !user
    ? name.trim() && age && age >= 1 && age <= 120 && address.trim() && email.trim() && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email) && phoneNumber.trim() && /^\+?[\d\s\-()]+$/.test(phoneNumber)
    : true;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!user) {
      // Mark all fields as touched for validation
      setTouched({
        name: true,
        age: true,
        address: true,
        email: true,
        phoneNumber: true,
      });

      if (!isFormValid) {
        return;
      }

      // Anonymous registration
      const anonymousData: AnonymousRegistrationRequest = {
        name: name.trim(),
        age: Number(age),
        address: address.trim(),
        email: email.trim(),
        phoneNumber: phoneNumber.trim(),
        quantity,
      };

      await onSubmit(anonymousData);
    } else {
      // Authenticated registration
      await onSubmit({ userId: user.userId, quantity });
    }
  };

  const totalPrice = ticketPrice ? ticketPrice * quantity : 0;

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
      </div>

      {/* Anonymous User Fields */}
      {!user && (
        <>
          <div className="border-t pt-4">
            <h4 className="text-sm font-semibold mb-3 text-neutral-700">Your Contact Information</h4>
            <p className="text-xs text-neutral-500 mb-4">
              We'll use this information to send you event updates and confirmations.
            </p>
          </div>

          {/* Name */}
          <div>
            <label className="block text-sm font-medium mb-2 text-neutral-700">
              Full Name <span className="text-red-500">*</span>
            </label>
            <Input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              onBlur={() => setTouched({ ...touched, name: true })}
              error={!!errors.name}
              disabled={isProcessing}
              placeholder="Enter your full name"
              className="w-full"
            />
            {errors.name && (
              <p className="text-xs text-red-600 mt-1">{errors.name}</p>
            )}
          </div>

          {/* Age */}
          <div>
            <label className="block text-sm font-medium mb-2 text-neutral-700">
              Age <span className="text-red-500">*</span>
            </label>
            <Input
              type="number"
              min="1"
              max="120"
              value={age}
              onChange={(e) => setAge(e.target.value ? parseInt(e.target.value) : '')}
              onBlur={() => setTouched({ ...touched, age: true })}
              error={!!errors.age}
              disabled={isProcessing}
              placeholder="Enter your age"
              className="w-full"
            />
            {errors.age && (
              <p className="text-xs text-red-600 mt-1">{errors.age}</p>
            )}
          </div>

          {/* Address */}
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

          {/* Email */}
          <div>
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
        </>
      )}

      {/* Authenticated User Info Display */}
      {user && profile && (
        <div className="border-t pt-4">
          <h4 className="text-sm font-semibold mb-3 text-neutral-700">Registration Details</h4>
          <div className="bg-neutral-50 p-4 rounded-lg space-y-2 text-sm">
            <div>
              <span className="font-medium">Name:</span> {profile.firstName} {profile.lastName}
            </div>
            <div>
              <span className="font-medium">Email:</span> {profile.email}
            </div>
            {profile.phoneNumber && (
              <div>
                <span className="font-medium">Phone:</span> {profile.phoneNumber}
              </div>
            )}
          </div>
          <p className="text-xs text-neutral-500 mt-2">
            Registration details from your profile
          </p>
        </div>
      )}

      {/* Total Price */}
      {!isFree && totalPrice > 0 && (
        <div className="p-4 bg-neutral-50 rounded-lg border-t-2 border-orange-500">
          <div className="flex justify-between items-center">
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
        disabled={isProcessing || (!user && !isFormValid)}
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
