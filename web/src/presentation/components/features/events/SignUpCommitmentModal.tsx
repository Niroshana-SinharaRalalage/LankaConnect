/**
 * SignUpCommitmentModal Component
 *
 * Professional modal dialog for committing to sign-up list items
 * Features:
 * - Auto-fills Name, Email, Phone from logged-in user
 * - Allows editing contact fields (user can override with different info)
 * - Quantity selector (respects remaining availability)
 * - Optional notes field
 * - Form validation
 * - SignUpGenius-style UX
 */

'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/presentation/components/ui/Dialog';
import { Button } from '@/presentation/components/ui/Button';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { SignUpItemCategory, type SignUpItemDto, type SignUpCommitmentDto } from '@/infrastructure/api/types/events.types';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';

interface SignUpCommitmentModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  item: SignUpItemDto | null;
  signUpListId: string;
  eventId: string;
  existingCommitment?: SignUpCommitmentDto | null; // Existing commitment data (for updates)
  onCommit: (data: CommitmentFormData) => Promise<void>;
  isSubmitting?: boolean;
}

export interface CommitmentFormData {
  userId: string;
  signUpListId: string;
  itemId: string;
  quantity: number;
  notes?: string;
  contactName?: string;
  contactEmail?: string;
  contactPhone?: string;
}

export function SignUpCommitmentModal({
  open,
  onOpenChange,
  item,
  signUpListId,
  eventId,
  existingCommitment,
  onCommit,
  isSubmitting = false,
}: SignUpCommitmentModalProps) {
  const { user } = useAuthStore();

  // Form state
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [quantity, setQuantity] = useState(1);
  const [notes, setNotes] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isValidatingEmail, setIsValidatingEmail] = useState(false);

  // Auto-fill user details and existing commitment data when modal opens
  useEffect(() => {
    if (open && user) {
      // If updating existing commitment, pre-fill with that data
      if (existingCommitment) {
        setName(existingCommitment.contactName || user.fullName || '');
        setEmail(existingCommitment.contactEmail || user.email || '');
        setPhone(existingCommitment.contactPhone || '');
        setQuantity(existingCommitment.quantity);
        setNotes(existingCommitment.notes || '');
      } else {
        // New commitment - use user defaults
        setName(user.fullName || '');
        setEmail(user.email || '');
        setPhone('');
        setQuantity(1);
        setNotes('');
      }
    }
  }, [open, user, existingCommitment]);

  // Reset form when modal closes
  useEffect(() => {
    if (!open) {
      setName('');
      setEmail('');
      setPhone('');
      setQuantity(1);
      setNotes('');
      setErrors({});
    }
  }, [open]);

  // Remove the old quantity initialization effect - now handled in the user/commitment effect above

  if (!item) return null;

  const currentlyCommitted = existingCommitment?.quantity || 0;
  // When updating: user can change their quantity to anything from 1 up to (currentlyCommitted + remainingQty)
  // This allows increasing their commitment OR decreasing it
  const maxQuantity = existingCommitment
    ? currentlyCommitted + item.remainingQuantity
    : item.remainingQuantity;

  // Validate form
  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!name.trim()) {
      newErrors.name = 'Name is required';
    }

    if (!email.trim()) {
      newErrors.email = 'Email is required';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      newErrors.email = 'Invalid email format';
    }

    // Allow quantity = 0 for cancellation, otherwise must be at least 1
    if (quantity < 0) {
      newErrors.quantity = 'Quantity cannot be negative';
    }

    if (quantity > maxQuantity) {
      newErrors.quantity = `Maximum ${maxQuantity} available`;
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Handle form submission
  // Phase 6A.15: Enhanced with email validation to ensure user is registered for event
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    // Phase 6A.15: Check if email is registered for the event before allowing commitment
    setIsValidatingEmail(true);
    try {
      const isRegistered = await eventsRepository.checkEventRegistrationByEmail(eventId, email.trim());

      if (!isRegistered) {
        setErrors({
          email: 'This email is not registered for the event. You must register for the event first.'
        });
        setIsValidatingEmail(false);
        return;
      }
    } catch (error) {
      console.error('Failed to validate email registration:', error);
      setErrors({
        submit: 'Failed to validate your registration. Please try again.'
      });
      setIsValidatingEmail(false);
      return;
    }
    setIsValidatingEmail(false);

    if (!user?.userId) {
      setErrors({ submit: 'User ID not available. Please log in again.' });
      return;
    }

    const commitmentData: CommitmentFormData = {
      userId: user.userId,
      signUpListId,
      itemId: item.id,
      quantity,
      notes: notes.trim() || undefined,
      contactName: name.trim() || undefined,
      contactEmail: email.trim() || undefined,
      contactPhone: phone.trim() || undefined,
    };

    try {
      await onCommit(commitmentData);
      onOpenChange(false);
    } catch (error) {
      console.error('Failed to commit:', error);
      setErrors({ submit: error instanceof Error ? error.message : 'Failed to commit to item' });
    }
  };

  // Get category badge color
  const getCategoryColor = () => {
    switch (item.itemCategory) {
      case SignUpItemCategory.Mandatory:
        return 'bg-red-100 text-red-800 border-red-300';
      case SignUpItemCategory.Preferred:
        return 'bg-blue-100 text-blue-800 border-blue-300';
      case SignUpItemCategory.Suggested:
        return 'bg-green-100 text-green-800 border-green-300';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-300';
    }
  };

  const getCategoryLabel = () => {
    switch (item.itemCategory) {
      case SignUpItemCategory.Mandatory:
        return 'Mandatory';
      case SignUpItemCategory.Preferred:
        return 'Preferred';
      case SignUpItemCategory.Suggested:
        return 'Suggested';
      default:
        return '';
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md max-h-[90vh] overflow-y-auto">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>{existingCommitment ? 'Update Sign Up' : 'Sign Up to Bring Item'}</DialogTitle>
            <DialogDescription>
              {existingCommitment
                ? 'Update your sign-up details (set quantity to 0 to cancel)'
                : 'Fill in your details to sign up for bringing this item'}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 my-4">
            {/* Item Details */}
            <div className="p-3 bg-neutral-50 dark:bg-neutral-800 rounded-md border border-neutral-200 dark:border-neutral-700">
              <div className="flex items-start justify-between mb-2">
                <h3 className="font-semibold text-neutral-900 dark:text-neutral-100">
                  {item.itemDescription}
                </h3>
                <span
                  className={`px-2 py-1 text-xs font-medium rounded-full border ${getCategoryColor()}`}
                >
                  {getCategoryLabel()}
                </span>
              </div>
              {item.notes && (
                <p className="text-sm text-neutral-600 dark:text-neutral-400 mb-2">
                  {item.notes}
                </p>
              )}
              <div className="space-y-1">
                {existingCommitment && (
                  <p className="text-sm text-neutral-700 dark:text-neutral-300">
                    <span className="font-medium">Your current commitment:</span> {currentlyCommitted} items
                  </p>
                )}
                <p className="text-sm font-medium text-neutral-700 dark:text-neutral-300">
                  Available to {existingCommitment ? 'add' : 'sign up'}: {item.remainingQuantity} of {item.quantity}
                </p>
                {existingCommitment && (
                  <p className="text-xs text-neutral-600 dark:text-neutral-400">
                    You can change to any amount between 1 and {maxQuantity} items
                  </p>
                )}
              </div>
            </div>

            {/* Name Field */}
            <div>
              <label
                htmlFor="name"
                className="block text-sm font-medium text-neutral-700 dark:text-neutral-300 mb-1"
              >
                Your Name *
              </label>
              <input
                id="name"
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                className={`w-full px-3 py-2 border rounded-md ${
                  errors.name
                    ? 'border-red-500 focus:ring-red-500'
                    : 'border-neutral-300 dark:border-neutral-600 focus:ring-blue-500'
                } focus:outline-none focus:ring-2`}
                placeholder="Enter your full name"
              />
              {errors.name && <p className="mt-1 text-xs text-red-600">{errors.name}</p>}
            </div>

            {/* Email Field */}
            <div>
              <label
                htmlFor="email"
                className="block text-sm font-medium text-neutral-700 dark:text-neutral-300 mb-1"
              >
                Email Address *
              </label>
              <input
                id="email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className={`w-full px-3 py-2 border rounded-md ${
                  errors.email
                    ? 'border-red-500 focus:ring-red-500'
                    : 'border-neutral-300 dark:border-neutral-600 focus:ring-blue-500'
                } focus:outline-none focus:ring-2`}
                placeholder="your.email@example.com"
              />
              {errors.email && (
                <div className="mt-1 text-xs text-red-600">
                  <p>{errors.email}</p>
                  {errors.email.includes('not registered for the event') && (
                    <Link
                      href={`/events/${eventId}`}
                      className="underline hover:text-red-700 mt-1 inline-block"
                    >
                      Click here to register for the event
                    </Link>
                  )}
                </div>
              )}
            </div>

            {/* Phone Field */}
            <div>
              <label
                htmlFor="phone"
                className="block text-sm font-medium text-neutral-700 dark:text-neutral-300 mb-1"
              >
                Phone Number (Optional)
              </label>
              <input
                id="phone"
                type="tel"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                className="w-full px-3 py-2 border border-neutral-300 dark:border-neutral-600 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="+1 (555) 123-4567"
              />
            </div>

            {/* Quantity Selector */}
            <div>
              <label
                htmlFor="quantity"
                className="block text-sm font-medium text-neutral-700 dark:text-neutral-300 mb-1"
              >
                Quantity * {existingCommitment && <span className="text-xs text-neutral-500">(0 to cancel)</span>} (max: {maxQuantity})
              </label>
              <input
                id="quantity"
                type="number"
                min="0"
                max={maxQuantity}
                value={quantity}
                onChange={(e) => {
                  const val = parseInt(e.target.value);
                  // Allow 0 for cancellation, otherwise clamp to valid range
                  if (isNaN(val)) {
                    setQuantity(0);
                  } else {
                    setQuantity(Math.max(0, Math.min(val, maxQuantity)));
                  }
                }}
                className={`w-full px-3 py-2 border rounded-md ${
                  errors.quantity
                    ? 'border-red-500 focus:ring-red-500'
                    : 'border-neutral-300 dark:border-neutral-600 focus:ring-blue-500'
                } focus:outline-none focus:ring-2`}
              />
              {errors.quantity && <p className="mt-1 text-xs text-red-600">{errors.quantity}</p>}
              {existingCommitment && quantity === 0 && (
                <p className="mt-1 text-xs text-orange-600 font-medium">
                  ⚠️ Setting to 0 will cancel your entire commitment
                </p>
              )}
            </div>

            {/* Notes Field */}
            <div>
              <label
                htmlFor="notes"
                className="block text-sm font-medium text-neutral-700 dark:text-neutral-300 mb-1"
              >
                Additional Notes (Optional)
              </label>
              <textarea
                id="notes"
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                rows={3}
                className="w-full px-3 py-2 border border-neutral-300 dark:border-neutral-600 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Any special details or preferences..."
              />
              <p className="mt-1 text-xs text-neutral-500 dark:text-neutral-400">
                Example: "I'll bring vegetarian option" or "Homemade recipe"
              </p>
            </div>

            {/* Submit Error */}
            {errors.submit && (
              <div className="p-3 bg-red-50 border border-red-200 rounded-md">
                <p className="text-sm text-red-800">
                  {errors.submit}
                  {/* Session 30: Provide login link when user session is missing */}
                  {errors.submit.includes('User ID not available') && (
                    <>
                      {' '}
                      <Link
                        href={`/login?redirect=${encodeURIComponent(`/events/${eventId}`)}`}
                        className="font-medium text-red-900 underline hover:text-red-700"
                      >
                        Click here to log in
                      </Link>
                    </>
                  )}
                </p>
              </div>
            )}
          </div>

          <DialogFooter>
            <div className="flex gap-2 w-full">
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
                disabled={isSubmitting || isValidatingEmail}
              >
                Close
              </Button>
              <Button
                type="submit"
                disabled={isSubmitting || isValidatingEmail}
              >
                {isValidatingEmail
                  ? 'Validating email...'
                  : isSubmitting
                    ? existingCommitment ? 'Updating...' : 'Signing up...'
                    : existingCommitment ? 'Update Sign Up' : 'Confirm Sign Up'}
              </Button>
            </div>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
