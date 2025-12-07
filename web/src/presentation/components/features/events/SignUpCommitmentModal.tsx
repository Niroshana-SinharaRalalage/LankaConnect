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
import { SignUpItemCategory, type SignUpItemDto } from '@/infrastructure/api/types/events.types';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';

interface SignUpCommitmentModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  item: SignUpItemDto | null;
  signUpListId: string;
  eventId: string;
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

  // Auto-fill user details when modal opens
  useEffect(() => {
    if (open && user) {
      setName(user.fullName || '');
      setEmail(user.email || '');
      // Phone is not in UserDto yet - will be added in Phase 2
      setPhone('');
    }
  }, [open, user]);

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

  // Set initial quantity when item changes
  useEffect(() => {
    if (item && open) {
      setQuantity(Math.min(1, item.remainingQuantity));
    }
  }, [item, open]);

  if (!item) return null;

  const remainingQty = item.remainingQuantity;
  const maxQuantity = remainingQty;

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

    if (quantity < 1) {
      newErrors.quantity = 'Quantity must be at least 1';
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
      <DialogContent className="max-w-md">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>Commit to Bring Item</DialogTitle>
            <DialogDescription>
              Fill in your details to commit to bringing this item
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
              <p className="text-sm font-medium text-neutral-700 dark:text-neutral-300">
                Available: {remainingQty} of {item.quantity}
              </p>
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
                Quantity * (max: {maxQuantity})
              </label>
              <input
                id="quantity"
                type="number"
                min="1"
                max={maxQuantity}
                value={quantity}
                onChange={(e) => {
                  const val = parseInt(e.target.value) || 1;
                  setQuantity(Math.max(1, Math.min(val, maxQuantity)));
                }}
                className={`w-full px-3 py-2 border rounded-md ${
                  errors.quantity
                    ? 'border-red-500 focus:ring-red-500'
                    : 'border-neutral-300 dark:border-neutral-600 focus:ring-blue-500'
                } focus:outline-none focus:ring-2`}
              />
              {errors.quantity && <p className="mt-1 text-xs text-red-600">{errors.quantity}</p>}
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
                <p className="text-sm text-red-800">{errors.submit}</p>
              </div>
            )}
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={isSubmitting || isValidatingEmail}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting || isValidatingEmail}>
              {isValidatingEmail ? 'Validating email...' : isSubmitting ? 'Confirming...' : 'Confirm Commitment'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
