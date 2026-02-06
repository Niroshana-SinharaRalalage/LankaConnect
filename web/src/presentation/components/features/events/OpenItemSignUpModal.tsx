/**
 * OpenItemSignUpModal Component
 *
 * Phase 6A.27: Modal dialog for adding/editing Open sign-up items
 * Users can add their own items to sign-up lists with hasOpenItems enabled
 *
 * Features:
 * - Add new Open items with name, quantity, notes
 * - Update existing Open items (owner only)
 * - Auto-fills Name, Email, Phone from logged-in user
 * - Quantity validation (1-1000)
 * - Form validation
 * - SignUpGenius-style UX
 */

'use client';

import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/presentation/components/ui/Dialog';
import { Button } from '@/presentation/components/ui/Button';
import { PhoneInput } from '@/presentation/components/ui/PhoneInput';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import type { SignUpItemDto } from '@/infrastructure/api/types/events.types';

interface OpenItemSignUpModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  signUpListId: string;
  signUpListCategory: string;
  eventId: string;
  /** Existing item for edit mode, null for add mode */
  existingItem?: SignUpItemDto | null;
  onSubmit: (data: OpenItemFormData) => Promise<void>;
  onCancel?: () => Promise<void>;
  isSubmitting?: boolean;
}

export interface OpenItemFormData {
  itemName: string;
  quantity: number;
  notes?: string;
  contactName?: string;
  contactEmail?: string;
  contactPhone?: string;
}

export function OpenItemSignUpModal({
  open,
  onOpenChange,
  signUpListId,
  signUpListCategory,
  eventId,
  existingItem,
  onSubmit,
  onCancel,
  isSubmitting = false,
}: OpenItemSignUpModalProps) {
  const { user } = useAuthStore();
  const isEditMode = !!existingItem;

  // Form state
  const [itemName, setItemName] = useState('');
  const [quantity, setQuantity] = useState(1);
  const [notes, setNotes] = useState('');
  const [contactName, setContactName] = useState('');
  const [contactEmail, setContactEmail] = useState('');
  const [contactPhone, setContactPhone] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [showCancelConfirm, setShowCancelConfirm] = useState(false);

  // Auto-fill user details and existing item data when modal opens
  useEffect(() => {
    if (open) {
      if (existingItem) {
        // Edit mode - pre-fill with existing item data
        setItemName(existingItem.itemDescription);
        setQuantity(existingItem.quantity);
        setNotes(existingItem.notes || '');
        // Get contact info from first commitment (user's own)
        const userCommitment = existingItem.commitments?.[0];
        setContactName(userCommitment?.contactName || user?.fullName || '');
        setContactEmail(userCommitment?.contactEmail || user?.email || '');
        setContactPhone(userCommitment?.contactPhone || '');
      } else if (user) {
        // Add mode with logged-in user - use their defaults
        setItemName('');
        setQuantity(1);
        setNotes('');
        setContactName(user.fullName || '');
        setContactEmail(user.email || '');
        setContactPhone('');
      } else {
        // Add mode, anonymous user - start empty
        setItemName('');
        setQuantity(1);
        setNotes('');
        setContactName('');
        setContactEmail('');
        setContactPhone('');
      }
      setErrors({});
      setShowCancelConfirm(false);
    }
  }, [open, user, existingItem]);

  // Reset form when modal closes
  useEffect(() => {
    if (!open) {
      setItemName('');
      setQuantity(1);
      setNotes('');
      setContactName('');
      setContactEmail('');
      setContactPhone('');
      setErrors({});
      setShowCancelConfirm(false);
    }
  }, [open]);

  // Validate form
  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!itemName.trim()) {
      newErrors.itemName = 'Item name is required';
    } else if (itemName.trim().length < 2) {
      newErrors.itemName = 'Item name must be at least 2 characters';
    } else if (itemName.trim().length > 200) {
      newErrors.itemName = 'Item name must be less than 200 characters';
    }

    if (quantity < 1) {
      newErrors.quantity = 'Quantity must be at least 1';
    } else if (quantity > 1000) {
      newErrors.quantity = 'Maximum quantity is 1000';
    }

    if (!contactName.trim()) {
      newErrors.contactName = 'Your name is required';
    }

    if (!contactEmail.trim()) {
      newErrors.contactEmail = 'Email is required';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(contactEmail)) {
      newErrors.contactEmail = 'Invalid email format';
    }

    if (notes && notes.length > 500) {
      newErrors.notes = 'Notes must be less than 500 characters';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Handle form submission
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    try {
      const formData: OpenItemFormData = {
        itemName: itemName.trim(),
        quantity,
        notes: notes.trim() || undefined,
        contactName: contactName.trim() || undefined,
        contactEmail: contactEmail.trim() || undefined,
        contactPhone: contactPhone.trim() || undefined,
      };

      await onSubmit(formData);
      onOpenChange(false);
    } catch (error) {
      console.error('Failed to submit Open item:', error);
      setErrors({
        submit: error instanceof Error ? error.message : 'Failed to submit. Please try again.',
      });
    }
  };

  // Handle cancel/delete
  const handleCancelItem = async () => {
    if (!onCancel) return;

    try {
      await onCancel();
      onOpenChange(false);
    } catch (error) {
      console.error('Failed to cancel Open item:', error);
      setErrors({
        submit: error instanceof Error ? error.message : 'Failed to cancel. Please try again.',
      });
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md max-h-[90vh] overflow-y-auto">
        {showCancelConfirm ? (
          // Cancel confirmation view
          <div>
            <DialogHeader>
              <DialogTitle className="text-red-600">Cancel Sign Up?</DialogTitle>
              <DialogDescription>
                Are you sure you want to cancel your sign up for &quot;{existingItem?.itemDescription}&quot;?
                This action cannot be undone.
              </DialogDescription>
            </DialogHeader>
            <DialogFooter className="mt-6">
              <div className="flex gap-2 w-full">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setShowCancelConfirm(false)}
                  disabled={isSubmitting}
                >
                  Go Back
                </Button>
                <Button
                  type="button"
                  variant="destructive"
                  onClick={handleCancelItem}
                  disabled={isSubmitting}
                >
                  {isSubmitting ? 'Cancelling...' : 'Yes, Cancel Sign Up'}
                </Button>
              </div>
            </DialogFooter>
          </div>
        ) : (
          // Main form view
          <form onSubmit={handleSubmit}>
            <DialogHeader>
              <DialogTitle>
                {isEditMode ? 'Update Your Sign Up' : 'Sign Up with Your Own Item'}
              </DialogTitle>
              <DialogDescription>
                {isEditMode
                  ? 'Update the details of your sign up item'
                  : `Add an item you\'ll bring to "${signUpListCategory}"`}
              </DialogDescription>
            </DialogHeader>

            <div className="space-y-4 my-4">
              {/* Sign-up list info */}
              <div className="p-3 bg-blue-50 dark:bg-blue-900/20 rounded-md border border-blue-200 dark:border-blue-800">
                <div className="flex items-center gap-2">
                  <span className="px-2 py-1 text-xs font-medium rounded-full border bg-purple-100 text-purple-800 border-purple-300">
                    Open
                  </span>
                  <span className="text-sm font-medium text-blue-800 dark:text-blue-200">
                    {signUpListCategory}
                  </span>
                </div>
                <p className="text-xs text-blue-600 dark:text-blue-400 mt-1">
                  You can bring your own item to contribute to this sign-up list
                </p>
              </div>

              {/* Item Name Field */}
              <div>
                <label
                  htmlFor="itemName"
                  className="block text-sm font-medium text-neutral-700 dark:text-neutral-300 mb-1"
                >
                  What will you bring? *
                </label>
                <input
                  id="itemName"
                  type="text"
                  value={itemName}
                  onChange={(e) => setItemName(e.target.value)}
                  className={`w-full px-3 py-2 border rounded-md ${
                    errors.itemName
                      ? 'border-red-500 focus:ring-red-500'
                      : 'border-neutral-300 dark:border-neutral-600 focus:ring-blue-500'
                  } focus:outline-none focus:ring-2`}
                  placeholder="e.g., Homemade Cookies, Fruit Salad, Paper Plates"
                />
                {errors.itemName && <p className="mt-1 text-xs text-red-600">{errors.itemName}</p>}
              </div>

              {/* Quantity Field */}
              <div>
                <label
                  htmlFor="quantity"
                  className="block text-sm font-medium text-neutral-700 dark:text-neutral-300 mb-1"
                >
                  How many? *
                </label>
                <input
                  id="quantity"
                  type="number"
                  min="1"
                  max="1000"
                  value={quantity}
                  onChange={(e) => {
                    const val = parseInt(e.target.value);
                    if (!isNaN(val)) {
                      setQuantity(Math.max(1, Math.min(val, 1000)));
                    }
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
                  Additional Details (Optional)
                </label>
                <textarea
                  id="notes"
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  rows={2}
                  className={`w-full px-3 py-2 border rounded-md ${
                    errors.notes
                      ? 'border-red-500 focus:ring-red-500'
                      : 'border-neutral-300 dark:border-neutral-600 focus:ring-blue-500'
                  } focus:outline-none focus:ring-2`}
                  placeholder="Any special notes (e.g., &quot;Gluten-free&quot;, &quot;Serves 10&quot;)"
                />
                {errors.notes && <p className="mt-1 text-xs text-red-600">{errors.notes}</p>}
              </div>

              {/* Contact Information Section */}
              <div className="pt-2 border-t border-neutral-200 dark:border-neutral-700">
                <h4 className="text-sm font-medium text-neutral-700 dark:text-neutral-300 mb-3">
                  Your Contact Information
                </h4>

                {/* Contact Name */}
                <div className="mb-3">
                  <label
                    htmlFor="contactName"
                    className="block text-sm font-medium text-neutral-700 dark:text-neutral-300 mb-1"
                  >
                    Your Name *
                  </label>
                  <input
                    id="contactName"
                    type="text"
                    value={contactName}
                    onChange={(e) => setContactName(e.target.value)}
                    className={`w-full px-3 py-2 border rounded-md ${
                      errors.contactName
                        ? 'border-red-500 focus:ring-red-500'
                        : 'border-neutral-300 dark:border-neutral-600 focus:ring-blue-500'
                    } focus:outline-none focus:ring-2`}
                    placeholder="Your full name"
                  />
                  {errors.contactName && (
                    <p className="mt-1 text-xs text-red-600">{errors.contactName}</p>
                  )}
                </div>

                {/* Contact Email */}
                <div className="mb-3">
                  <label
                    htmlFor="contactEmail"
                    className="block text-sm font-medium text-neutral-700 dark:text-neutral-300 mb-1"
                  >
                    Email Address *
                  </label>
                  <input
                    id="contactEmail"
                    type="email"
                    value={contactEmail}
                    onChange={(e) => setContactEmail(e.target.value)}
                    className={`w-full px-3 py-2 border rounded-md ${
                      errors.contactEmail
                        ? 'border-red-500 focus:ring-red-500'
                        : 'border-neutral-300 dark:border-neutral-600 focus:ring-blue-500'
                    } focus:outline-none focus:ring-2`}
                    placeholder="your.email@example.com"
                  />
                  {errors.contactEmail && (
                    <p className="mt-1 text-xs text-red-600">{errors.contactEmail}</p>
                  )}
                </div>

                {/* Contact Phone - GitHub Issue #30: PhoneInput restricts invalid characters */}
                <div>
                  <label
                    htmlFor="contactPhone"
                    className="block text-sm font-medium text-neutral-700 dark:text-neutral-300 mb-1"
                  >
                    Phone Number (Optional)
                  </label>
                  <PhoneInput
                    id="contactPhone"
                    value={contactPhone}
                    onChange={setContactPhone}
                    placeholder="+1-234-567-8901"
                  />
                </div>
              </div>

              {/* Submit Error */}
              {errors.submit && (
                <div className="p-3 bg-red-50 border border-red-200 rounded-md">
                  <p className="text-sm text-red-800">{errors.submit}</p>
                </div>
              )}
            </div>

            <DialogFooter>
              <div className="flex flex-col gap-2 w-full">
                <div className="flex gap-2">
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => onOpenChange(false)}
                    disabled={isSubmitting}
                    className="flex-1"
                  >
                    Cancel
                  </Button>
                  <Button type="submit" disabled={isSubmitting} className="flex-1">
                    {isSubmitting
                      ? isEditMode
                        ? 'Updating...'
                        : 'Signing up...'
                      : isEditMode
                        ? 'Update Sign Up'
                        : 'Sign Up'}
                  </Button>
                </div>
                {isEditMode && onCancel && (
                  <Button
                    type="button"
                    variant="ghost"
                    className="text-red-600 hover:text-red-700 hover:bg-red-50"
                    onClick={() => setShowCancelConfirm(true)}
                    disabled={isSubmitting}
                  >
                    Cancel My Sign Up
                  </Button>
                )}
              </div>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
