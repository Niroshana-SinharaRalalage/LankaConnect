/**
 * EditRegistrationModal Component
 *
 * Phase 6A.14: Modal dialog for editing event registration details
 *
 * Features:
 * - Edit attendee names, age categories, and gender
 * - Edit contact information (email, phone, address)
 * - Add/remove attendees for free events
 * - Prevents attendee count changes for paid events
 * - Form validation
 * - Error handling
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
import { AlertCircle, Plus, Trash2, User } from 'lucide-react';
import type { RegistrationDetailsDto, AttendeeDto, PaymentStatus } from '@/infrastructure/api/types/events.types';
import { AgeCategory, Gender } from '@/infrastructure/api/types/events.types';

interface EditRegistrationModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  registration: RegistrationDetailsDto | null;
  eventId: string;
  isFreeEvent: boolean;
  spotsLeft: number;
  onSave: (data: EditRegistrationData) => Promise<void>;
  isSubmitting?: boolean;
}

export interface EditRegistrationData {
  attendees: AttendeeDto[];
  email: string;
  phoneNumber: string;
  address?: string;
}

export function EditRegistrationModal({
  open,
  onOpenChange,
  registration,
  eventId,
  isFreeEvent,
  spotsLeft,
  onSave,
  isSubmitting = false,
}: EditRegistrationModalProps) {
  // Form state
  const [attendees, setAttendees] = useState<AttendeeDto[]>([]);
  const [email, setEmail] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [address, setAddress] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);

  // Determine if this is a paid registration (cannot change attendee count)
  // Phase 6A.79 Part 3: API returns string values, not numeric enums
  const isPaidRegistration = registration?.paymentStatus === 'Completed';
  const originalAttendeeCount = registration?.attendees?.length || registration?.quantity || 1;

  // Calculate max attendees allowed
  const maxAttendeesAllowed = isFreeEvent
    ? Math.min(10, originalAttendeeCount + spotsLeft) // Free: can add up to event capacity
    : originalAttendeeCount; // Paid: locked to original count

  // Initialize form with registration data when modal opens
  useEffect(() => {
    if (open && registration) {
      // Set attendees
      if (registration.attendees && registration.attendees.length > 0) {
        setAttendees([...registration.attendees]);
      } else {
        // Fallback: create placeholder attendees based on quantity
        const placeholders: AttendeeDto[] = [];
        const qty = registration.quantity || 1;
        for (let i = 0; i < qty; i++) {
          placeholders.push({ name: '', ageCategory: AgeCategory.Adult, gender: null });
        }
        setAttendees(placeholders);
      }

      // Set contact information
      setEmail(registration.contactEmail || '');
      setPhoneNumber(registration.contactPhone || '');
      setAddress(registration.contactAddress || '');

      // Clear errors
      setErrors({});
      setSubmitError(null);
    }
  }, [open, registration]);

  // Reset form when modal closes
  useEffect(() => {
    if (!open) {
      setAttendees([]);
      setEmail('');
      setPhoneNumber('');
      setAddress('');
      setErrors({});
      setSubmitError(null);
    }
  }, [open]);

  if (!registration) return null;

  // Update attendee field
  const updateAttendee = (index: number, field: keyof AttendeeDto, value: string | AgeCategory | Gender | null) => {
    const newAttendees = [...attendees];
    if (field === 'name') {
      newAttendees[index] = { ...newAttendees[index], name: value as string };
    } else if (field === 'ageCategory') {
      newAttendees[index] = { ...newAttendees[index], ageCategory: value as AgeCategory };
    } else if (field === 'gender') {
      newAttendees[index] = { ...newAttendees[index], gender: value as Gender | null };
    }
    setAttendees(newAttendees);

    // Clear specific error when user starts typing
    const errorKey = `attendee_${index}_${field}`;
    if (errors[errorKey]) {
      const newErrors = { ...errors };
      delete newErrors[errorKey];
      setErrors(newErrors);
    }
  };

  // Add new attendee
  const addAttendee = () => {
    if (attendees.length < maxAttendeesAllowed) {
      setAttendees([...attendees, { name: '', ageCategory: AgeCategory.Adult, gender: null }]);
    }
  };

  // Remove attendee
  const removeAttendee = (index: number) => {
    if (attendees.length > 1) {
      const newAttendees = attendees.filter((_, i) => i !== index);
      setAttendees(newAttendees);
    }
  };

  // Validate form
  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    // Validate attendees
    if (attendees.length === 0) {
      newErrors.attendees = 'At least one attendee is required';
    }

    attendees.forEach((attendee, index) => {
      if (!attendee.name || !attendee.name.trim()) {
        newErrors[`attendee_${index}_name`] = 'Name is required';
      } else if (attendee.name.length > 100) {
        newErrors[`attendee_${index}_name`] = 'Name cannot exceed 100 characters';
      }

      if (!attendee.ageCategory) {
        newErrors[`attendee_${index}_ageCategory`] = 'Age category is required';
      }
    });

    // Validate paid registration attendee count
    if (isPaidRegistration && attendees.length !== originalAttendeeCount) {
      newErrors.attendees = `Cannot change attendee count on a paid registration. Original count: ${originalAttendeeCount}`;
    }

    // Validate email
    if (!email.trim()) {
      newErrors.email = 'Email is required';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      newErrors.email = 'Invalid email format';
    }

    // Validate phone
    if (!phoneNumber.trim()) {
      newErrors.phoneNumber = 'Phone number is required';
    } else if (phoneNumber.length > 30) {
      newErrors.phoneNumber = 'Phone number cannot exceed 30 characters';
    }

    // Validate address (optional, but has max length)
    if (address && address.length > 500) {
      newErrors.address = 'Address cannot exceed 500 characters';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Handle form submission
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitError(null);

    if (!validateForm()) {
      return;
    }

    try {
      await onSave({
        attendees: attendees.map(a => ({
          name: a.name.trim(),
          ageCategory: a.ageCategory,
          gender: a.gender
        })),
        email: email.trim(),
        phoneNumber: phoneNumber.trim(),
        address: address.trim() || undefined,
      });
      onOpenChange(false);
    } catch (error: any) {
      console.error('Failed to update registration:', error);
      setSubmitError(
        error?.response?.data?.detail ||
        error?.response?.data?.message ||
        error?.message ||
        'Failed to update registration. Please try again.'
      );
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>Edit Registration</DialogTitle>
            <DialogDescription>
              Update your registration details for this event.
              {isPaidRegistration && (
                <span className="block mt-1 text-amber-600 dark:text-amber-400 font-medium">
                  Note: Attendee count cannot be changed for paid registrations.
                </span>
              )}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-6 my-4">
            {/* Attendees Section */}
            <div>
              <div className="flex items-center justify-between mb-3">
                <h3 className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
                  Attendees ({attendees.length})
                </h3>
                {!isPaidRegistration && attendees.length < maxAttendeesAllowed && (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={addAttendee}
                    className="flex items-center gap-1"
                  >
                    <Plus className="h-4 w-4" />
                    Add Attendee
                  </Button>
                )}
              </div>

              {errors.attendees && (
                <p className="mb-2 text-xs text-red-600">{errors.attendees}</p>
              )}

              <div className="space-y-3">
                {attendees.map((attendee, index) => (
                  <div
                    key={index}
                    className="p-3 bg-neutral-50 dark:bg-neutral-800 rounded-md border border-neutral-200 dark:border-neutral-700"
                  >
                    <div className="flex items-start justify-between mb-2">
                      <div className="flex items-center gap-2">
                        <User className="h-4 w-4 text-neutral-500" />
                        <span className="text-sm font-medium text-neutral-700 dark:text-neutral-300">
                          Attendee {index + 1}
                        </span>
                      </div>
                      {!isPaidRegistration && attendees.length > 1 && (
                        <button
                          type="button"
                          onClick={() => removeAttendee(index)}
                          className="text-red-500 hover:text-red-700 p-1"
                          title="Remove attendee"
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      )}
                    </div>

                    <div className="space-y-3">
                      {/* Name */}
                      <div>
                        <label
                          htmlFor={`attendee-name-${index}`}
                          className="block text-xs font-medium text-neutral-600 dark:text-neutral-400 mb-1"
                        >
                          Name *
                        </label>
                        <input
                          id={`attendee-name-${index}`}
                          type="text"
                          value={attendee.name}
                          onChange={(e) => updateAttendee(index, 'name', e.target.value)}
                          className={`w-full px-3 py-2 text-sm border rounded-md ${
                            errors[`attendee_${index}_name`]
                              ? 'border-red-500 focus:ring-red-500'
                              : 'border-neutral-300 dark:border-neutral-600 focus:ring-blue-500'
                          } focus:outline-none focus:ring-2`}
                          placeholder="Full name"
                        />
                        {errors[`attendee_${index}_name`] && (
                          <p className="mt-1 text-xs text-red-600">
                            {errors[`attendee_${index}_name`]}
                          </p>
                        )}
                      </div>

                      {/* Age Category */}
                      <div>
                        <label className="block text-xs font-medium text-neutral-600 dark:text-neutral-400 mb-2">
                          Age Category *
                        </label>
                        <div className="flex gap-4">
                          <label className="flex items-center gap-2 cursor-pointer">
                            <input
                              type="radio"
                              name={`attendee-age-category-${index}`}
                              checked={attendee.ageCategory === AgeCategory.Adult}
                              onChange={() => updateAttendee(index, 'ageCategory', AgeCategory.Adult)}
                              className="w-4 h-4 text-blue-600 focus:ring-blue-500"
                            />
                            <span className="text-sm text-neutral-700 dark:text-neutral-300">Adult</span>
                          </label>
                          <label className="flex items-center gap-2 cursor-pointer">
                            <input
                              type="radio"
                              name={`attendee-age-category-${index}`}
                              checked={attendee.ageCategory === AgeCategory.Child}
                              onChange={() => updateAttendee(index, 'ageCategory', AgeCategory.Child)}
                              className="w-4 h-4 text-blue-600 focus:ring-blue-500"
                            />
                            <span className="text-sm text-neutral-700 dark:text-neutral-300">Child</span>
                          </label>
                        </div>
                        {errors[`attendee_${index}_ageCategory`] && (
                          <p className="mt-1 text-xs text-red-600">
                            {errors[`attendee_${index}_ageCategory`]}
                          </p>
                        )}
                      </div>

                      {/* Gender */}
                      <div>
                        <label
                          htmlFor={`attendee-gender-${index}`}
                          className="block text-xs font-medium text-neutral-600 dark:text-neutral-400 mb-1"
                        >
                          Gender (Optional)
                        </label>
                        <select
                          id={`attendee-gender-${index}`}
                          value={attendee.gender ?? ''}
                          onChange={(e) =>
                            updateAttendee(index, 'gender', e.target.value ? parseInt(e.target.value) as Gender : null)
                          }
                          className="w-full px-3 py-2 text-sm border border-neutral-300 dark:border-neutral-600 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        >
                          <option value="">Select gender</option>
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

            {/* Contact Information Section */}
            <div>
              <h3 className="text-sm font-semibold text-neutral-900 dark:text-neutral-100 mb-3">
                Contact Information
              </h3>

              <div className="space-y-3">
                {/* Email */}
                <div>
                  <label
                    htmlFor="email"
                    className="block text-sm font-medium text-neutral-700 dark:text-neutral-300 mb-1"
                  >
                    Email *
                  </label>
                  <input
                    id="email"
                    type="email"
                    value={email}
                    onChange={(e) => {
                      setEmail(e.target.value);
                      if (errors.email) {
                        const newErrors = { ...errors };
                        delete newErrors.email;
                        setErrors(newErrors);
                      }
                    }}
                    className={`w-full px-3 py-2 border rounded-md ${
                      errors.email
                        ? 'border-red-500 focus:ring-red-500'
                        : 'border-neutral-300 dark:border-neutral-600 focus:ring-blue-500'
                    } focus:outline-none focus:ring-2`}
                    placeholder="your.email@example.com"
                  />
                  {errors.email && <p className="mt-1 text-xs text-red-600">{errors.email}</p>}
                </div>

                {/* Phone */}
                <div>
                  <label
                    htmlFor="phone"
                    className="block text-sm font-medium text-neutral-700 dark:text-neutral-300 mb-1"
                  >
                    Phone Number *
                  </label>
                  <input
                    id="phone"
                    type="tel"
                    value={phoneNumber}
                    onChange={(e) => {
                      setPhoneNumber(e.target.value);
                      if (errors.phoneNumber) {
                        const newErrors = { ...errors };
                        delete newErrors.phoneNumber;
                        setErrors(newErrors);
                      }
                    }}
                    className={`w-full px-3 py-2 border rounded-md ${
                      errors.phoneNumber
                        ? 'border-red-500 focus:ring-red-500'
                        : 'border-neutral-300 dark:border-neutral-600 focus:ring-blue-500'
                    } focus:outline-none focus:ring-2`}
                    placeholder="+1 (555) 123-4567"
                  />
                  {errors.phoneNumber && (
                    <p className="mt-1 text-xs text-red-600">{errors.phoneNumber}</p>
                  )}
                </div>

                {/* Address (Optional) */}
                <div>
                  <label
                    htmlFor="address"
                    className="block text-sm font-medium text-neutral-700 dark:text-neutral-300 mb-1"
                  >
                    Address (Optional)
                  </label>
                  <textarea
                    id="address"
                    value={address}
                    onChange={(e) => {
                      setAddress(e.target.value);
                      if (errors.address) {
                        const newErrors = { ...errors };
                        delete newErrors.address;
                        setErrors(newErrors);
                      }
                    }}
                    rows={2}
                    className={`w-full px-3 py-2 border rounded-md ${
                      errors.address
                        ? 'border-red-500 focus:ring-red-500'
                        : 'border-neutral-300 dark:border-neutral-600 focus:ring-blue-500'
                    } focus:outline-none focus:ring-2`}
                    placeholder="123 Main St, City, State ZIP"
                  />
                  {errors.address && <p className="mt-1 text-xs text-red-600">{errors.address}</p>}
                </div>
              </div>
            </div>

            {/* Submit Error */}
            {submitError && (
              <div className="p-3 bg-red-50 border border-red-200 rounded-md flex items-start gap-2">
                <AlertCircle className="h-5 w-5 text-red-600 flex-shrink-0 mt-0.5" />
                <div>
                  <p className="text-sm font-medium text-red-800">Update Failed</p>
                  <p className="text-sm text-red-700">{submitError}</p>
                </div>
              </div>
            )}
          </div>

          <DialogFooter>
            <div className="flex gap-2 w-full">
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
                disabled={isSubmitting}
                className="flex-1"
              >
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={isSubmitting}
                className="flex-1"
              >
                {isSubmitting ? 'Saving...' : 'Save Changes'}
              </Button>
            </div>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
