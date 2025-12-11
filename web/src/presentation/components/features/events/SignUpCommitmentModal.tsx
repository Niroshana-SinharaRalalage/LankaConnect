/**
 * SignUpCommitmentModal Component
 *
 * Professional modal dialog for committing to sign-up list items
 * Phase 6A.23: Updated to support anonymous sign-up workflow
 *
 * Features:
 * - Works for both logged-in and anonymous users
 * - Auto-fills Name, Email, Phone from logged-in user (if available)
 * - Validates email on submit:
 *   1. If member account → prompts to log in
 *   2. If anonymous + registered → allows commitment
 *   3. If not registered → prompts to register first
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
  existingCommitment?: SignUpCommitmentDto | null;
  onCommit: (data: CommitmentFormData) => Promise<void>;
  onCommitAnonymous?: (data: AnonymousCommitmentFormData) => Promise<void>;
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

export interface AnonymousCommitmentFormData {
  signUpListId: string;
  itemId: string;
  quantity: number;
  notes?: string;
  contactName?: string;
  contactEmail: string;
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
  onCommitAnonymous,
  isSubmitting = false,
}: SignUpCommitmentModalProps) {
  const { user } = useAuthStore();
  const isLoggedIn = !!user?.userId;

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
    if (open) {
      if (existingCommitment) {
        // Updating existing commitment - pre-fill with that data
        setName(existingCommitment.contactName || user?.fullName || '');
        setEmail(existingCommitment.contactEmail || user?.email || '');
        setPhone(existingCommitment.contactPhone || '');
        setQuantity(existingCommitment.quantity);
        setNotes(existingCommitment.notes || '');
      } else if (user) {
        // New commitment with logged-in user - use their defaults
        setName(user.fullName || '');
        setEmail(user.email || '');
        setPhone('');
        setQuantity(1);
        setNotes('');
      } else {
        // New commitment, anonymous user - start empty
        setName('');
        setEmail('');
        setPhone('');
        setQuantity(1);
        setNotes('');
      }
      setErrors({});
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

  if (!item) return null;

  const currentlyCommitted = existingCommitment?.quantity || 0;
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

    if (quantity < 0) {
      newErrors.quantity = 'Quantity cannot be negative';
    }

    if (quantity > maxQuantity) {
      newErrors.quantity = `Maximum ${maxQuantity} available`;
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  /**
   * Handle form submission with proper UX flow
   * Phase 6A.23: Supports both logged-in and anonymous users
   */
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    setIsValidatingEmail(true);
    setErrors({});

    try {
      // Step 1: Check email registration status
      const registrationCheck = await eventsRepository.checkEventRegistrationByEmail(eventId, email.trim());

      // Step 2: Handle based on result
      if (registrationCheck.shouldPromptLogin) {
        // Email belongs to a member - they should log in
        setErrors({
          email: "This email is associated with a LankaConnect account. Please log in to sign up for items."
        });
        setIsValidatingEmail(false);
        return;
      }

      if (registrationCheck.needsEventRegistration) {
        // Not registered for event
        setErrors({
          email: 'This email is not registered for the event. You must register for the event first.'
        });
        setIsValidatingEmail(false);
        return;
      }

      // Step 3: User can proceed - determine which path
      setIsValidatingEmail(false);

      if (isLoggedIn && user?.userId) {
        // Logged-in user - use authenticated endpoint
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

        await onCommit(commitmentData);
        onOpenChange(false);
      } else if (registrationCheck.canCommitAnonymously && onCommitAnonymous) {
        // Anonymous user registered for event - use anonymous endpoint
        const anonymousData: AnonymousCommitmentFormData = {
          signUpListId,
          itemId: item.id,
          quantity,
          notes: notes.trim() || undefined,
          contactName: name.trim() || undefined,
          contactEmail: email.trim(),
          contactPhone: phone.trim() || undefined,
        };

        await onCommitAnonymous(anonymousData);
        onOpenChange(false);
      } else {
        // Fallback error - shouldn't happen if flow is correct
        setErrors({ submit: 'Unable to process your sign-up. Please try again.' });
      }
    } catch (error) {
      console.error('Failed to process sign-up:', error);
      setErrors({ submit: error instanceof Error ? error.message : 'Failed to sign up. Please try again.' });
      setIsValidatingEmail(false);
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
                  {errors.email.includes('associated with a LankaConnect account') && (
                    <Link
                      href={`/login?redirect=${encodeURIComponent(`/events/${eventId}`)}`}
                      className="underline hover:text-red-700 mt-1 inline-block font-medium"
                    >
                      Click here to log in
                    </Link>
                  )}
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
                  Setting to 0 will cancel your entire commitment
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
                Example: &quot;I&apos;ll bring vegetarian option&quot; or &quot;Homemade recipe&quot;
              </p>
            </div>

            {/* Submit Error */}
            {errors.submit && (
              <div className="p-3 bg-red-50 border border-red-200 rounded-md">
                <p className="text-sm text-red-800">{errors.submit}</p>
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
                  ? 'Validating...'
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
