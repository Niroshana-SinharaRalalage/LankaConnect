/**
 * AddAttendeesModal Component
 *
 * Add-Only Attendees with Delta Payment Feature
 *
 * Allows users with paid event registrations to add new attendees
 * and pay only the price difference via Stripe checkout.
 *
 * Features:
 * - Multi-step wizard (Add → Review → Pay)
 * - Real-time price calculation
 * - Attendee validation
 * - Pending addition detection
 * - Stripe checkout redirect
 */

'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/presentation/components/ui/Dialog';
import { Button } from '@/presentation/components/ui/Button';
import {
  AlertCircle,
  ArrowLeft,
  ArrowRight,
  Check,
  Loader2,
  Plus,
  Trash2,
  User,
  Users,
  CreditCard,
  Clock,
} from 'lucide-react';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type {
  AdditionPriceResultDto,
  NewAttendeeDto,
  PendingAdditionDto,
} from '@/infrastructure/api/types/events.types';
import { AgeCategory, Gender } from '@/infrastructure/api/types/events.types';

interface AddAttendeesModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  registrationId: string;
  eventId: string;
  eventTitle: string;
  currentAttendeeCount: number;
  maxAttendeesPerRegistration: number;
  /** Callback when attendees are successfully added (after payment completes) */
  onSuccess?: () => void;
}

type WizardStep = 'add' | 'review' | 'pending';

export function AddAttendeesModal({
  open,
  onOpenChange,
  registrationId,
  eventId,
  eventTitle,
  currentAttendeeCount,
  maxAttendeesPerRegistration,
  onSuccess,
}: AddAttendeesModalProps) {
  // Wizard state
  const [step, setStep] = useState<WizardStep>('add');

  // Form state
  const [newAttendees, setNewAttendees] = useState<NewAttendeeDto[]>([
    { name: '', ageCategory: AgeCategory.Adult, gender: null },
  ]);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // API state
  const [isCalculating, setIsCalculating] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isCheckingPending, setIsCheckingPending] = useState(false);
  const [priceResult, setPriceResult] = useState<AdditionPriceResultDto | null>(null);
  const [pendingAddition, setPendingAddition] = useState<PendingAdditionDto | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);

  // Calculate remaining slots
  const remainingSlots = maxAttendeesPerRegistration - currentAttendeeCount;
  const canAddMore = newAttendees.length < remainingSlots;

  // Check for pending additions when modal opens
  useEffect(() => {
    if (open && registrationId) {
      checkPendingAddition();
    }
  }, [open, registrationId]);

  // Reset state when modal closes
  useEffect(() => {
    if (!open) {
      setStep('add');
      setNewAttendees([{ name: '', ageCategory: AgeCategory.Adult, gender: null }]);
      setErrors({});
      setPriceResult(null);
      setSubmitError(null);
    }
  }, [open]);

  const checkPendingAddition = async () => {
    try {
      setIsCheckingPending(true);
      const pending = await eventsRepository.getPendingAddition(registrationId);
      if (pending && pending.status === 'Pending') {
        setPendingAddition(pending);
        setStep('pending');
      } else {
        setPendingAddition(null);
        setStep('add');
      }
    } catch (error) {
      console.error('[AddAttendeesModal] Error checking pending addition:', error);
      // Continue to add step if check fails
      setStep('add');
    } finally {
      setIsCheckingPending(false);
    }
  };

  // Add new attendee slot
  const addAttendee = () => {
    if (canAddMore) {
      setNewAttendees([
        ...newAttendees,
        { name: '', ageCategory: AgeCategory.Adult, gender: null },
      ]);
    }
  };

  // Remove attendee slot
  const removeAttendee = (index: number) => {
    if (newAttendees.length > 1) {
      const updated = newAttendees.filter((_, i) => i !== index);
      setNewAttendees(updated);
      // Clear errors for removed attendee
      const newErrors = { ...errors };
      delete newErrors[`attendee_${index}_name`];
      setErrors(newErrors);
    }
  };

  // Update attendee field
  const updateAttendee = (
    index: number,
    field: keyof NewAttendeeDto,
    value: string | AgeCategory | Gender | null
  ) => {
    const updated = [...newAttendees];
    if (field === 'name') {
      updated[index] = { ...updated[index], name: value as string };
    } else if (field === 'ageCategory') {
      updated[index] = { ...updated[index], ageCategory: value as AgeCategory };
    } else if (field === 'gender') {
      updated[index] = { ...updated[index], gender: value as Gender | null };
    }
    setNewAttendees(updated);

    // Clear error when user starts typing
    const errorKey = `attendee_${index}_${field}`;
    if (errors[errorKey]) {
      const newErrors = { ...errors };
      delete newErrors[errorKey];
      setErrors(newErrors);
    }
  };

  // Validate form
  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (newAttendees.length === 0) {
      newErrors.general = 'Please add at least one attendee';
      setErrors(newErrors);
      return false;
    }

    newAttendees.forEach((attendee, index) => {
      if (!attendee.name || !attendee.name.trim()) {
        newErrors[`attendee_${index}_name`] = 'Name is required';
      } else if (attendee.name.length > 100) {
        newErrors[`attendee_${index}_name`] = 'Name cannot exceed 100 characters';
      }
    });

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Calculate price and move to review step
  const handleCalculatePrice = async () => {
    if (!validateForm()) return;

    try {
      setIsCalculating(true);
      setSubmitError(null);

      const result = await eventsRepository.calculateAdditionPrice(registrationId, {
        newAttendees: newAttendees.map((a) => ({
          name: a.name.trim(),
          ageCategory: a.ageCategory,
          gender: a.gender,
        })),
      });

      if (!result.isValid) {
        setSubmitError(result.errorMessage || 'Unable to calculate price');
        return;
      }

      if (result.hasPendingAddition) {
        // Refresh pending addition status
        await checkPendingAddition();
        return;
      }

      setPriceResult(result);
      setStep('review');
    } catch (error: any) {
      console.error('[AddAttendeesModal] Error calculating price:', error);
      setSubmitError(
        error?.response?.data?.detail ||
          error?.message ||
          'Failed to calculate price. Please try again.'
      );
    } finally {
      setIsCalculating(false);
    }
  };

  // Initiate payment
  const handleInitiatePayment = async () => {
    if (!priceResult) return;

    try {
      setIsSubmitting(true);
      setSubmitError(null);

      // Build success/cancel URLs
      const baseUrl = window.location.origin;
      const successUrl = `${baseUrl}/events/${eventId}?addition=success&registrationId=${registrationId}`;
      const cancelUrl = `${baseUrl}/events/${eventId}?addition=cancelled&registrationId=${registrationId}`;

      const result = await eventsRepository.initiateAddAttendees(registrationId, {
        newAttendees: newAttendees.map((a) => ({
          name: a.name.trim(),
          ageCategory: a.ageCategory,
          gender: a.gender,
        })),
        successUrl,
        cancelUrl,
      });

      if (!result.success) {
        setSubmitError(result.errorMessage || 'Failed to initiate payment');
        return;
      }

      // Redirect to Stripe checkout
      if (result.checkoutUrl) {
        window.location.href = result.checkoutUrl;
      } else {
        setSubmitError('No checkout URL received');
      }
    } catch (error: any) {
      console.error('[AddAttendeesModal] Error initiating payment:', error);
      setSubmitError(
        error?.response?.data?.detail ||
          error?.message ||
          'Failed to initiate payment. Please try again.'
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  // Cancel pending addition
  const handleCancelPending = async () => {
    try {
      setIsSubmitting(true);
      setSubmitError(null);

      await eventsRepository.cancelPendingAddition(registrationId);
      setPendingAddition(null);
      setStep('add');
    } catch (error: any) {
      console.error('[AddAttendeesModal] Error cancelling pending addition:', error);
      setSubmitError(
        error?.response?.data?.detail ||
          error?.message ||
          'Failed to cancel. Please try again.'
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  // Resume pending checkout
  const handleResumePending = () => {
    if (pendingAddition?.checkoutSessionId) {
      // Re-initiate to get a fresh checkout URL
      handleInitiatePayment();
    }
  };

  // Format currency
  const formatCurrency = (amount: number, currency: string = 'USD') => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
    }).format(amount);
  };

  // Format expiry time
  const formatExpiryTime = (expiresAt: string) => {
    const expiry = new Date(expiresAt);
    const now = new Date();
    const diffMs = expiry.getTime() - now.getTime();

    if (diffMs <= 0) return 'Expired';

    const hours = Math.floor(diffMs / (1000 * 60 * 60));
    const minutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));

    if (hours > 0) {
      return `${hours}h ${minutes}m remaining`;
    }
    return `${minutes}m remaining`;
  };

  // Loading state while checking pending
  if (isCheckingPending) {
    return (
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="max-w-md">
          <div className="flex flex-col items-center justify-center py-8">
            <Loader2 className="h-8 w-8 animate-spin text-blue-600" />
            <p className="mt-4 text-sm text-neutral-600">Checking registration status...</p>
          </div>
        </DialogContent>
      </Dialog>
    );
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
        {/* STEP: PENDING ADDITION */}
        {step === 'pending' && pendingAddition && (
          <>
            <DialogHeader>
              <DialogTitle className="flex items-center gap-2">
                <Clock className="h-5 w-5 text-amber-500" />
                Pending Payment
              </DialogTitle>
              <DialogDescription>
                You have a pending attendee addition that requires payment.
              </DialogDescription>
            </DialogHeader>

            <div className="my-4 space-y-4">
              <div className="p-4 bg-amber-50 border border-amber-200 rounded-lg">
                <p className="text-sm font-medium text-amber-800 mb-2">
                  {pendingAddition.newAttendees.length} attendee(s) waiting to be added
                </p>
                <ul className="text-sm text-amber-700 space-y-1">
                  {pendingAddition.newAttendees.map((a, i) => (
                    <li key={i}>• {a.name} ({a.ageCategory})</li>
                  ))}
                </ul>
                <p className="mt-3 text-sm font-semibold text-amber-900">
                  Amount: {formatCurrency(pendingAddition.additionalAmount, pendingAddition.currency)}
                </p>
                {pendingAddition.expiresAt && (
                  <p className="mt-1 text-xs text-amber-600">
                    <Clock className="h-3 w-3 inline mr-1" />
                    {formatExpiryTime(pendingAddition.expiresAt)}
                  </p>
                )}
              </div>

              {submitError && (
                <div className="p-3 bg-red-50 border border-red-200 rounded-md flex items-start gap-2">
                  <AlertCircle className="h-5 w-5 text-red-600 flex-shrink-0 mt-0.5" />
                  <p className="text-sm text-red-700">{submitError}</p>
                </div>
              )}
            </div>

            <DialogFooter>
              <div className="flex gap-2 w-full">
                <Button
                  type="button"
                  variant="outline"
                  onClick={handleCancelPending}
                  disabled={isSubmitting}
                  className="flex-1"
                >
                  {isSubmitting ? 'Cancelling...' : 'Cancel & Start Over'}
                </Button>
                <Button
                  type="button"
                  onClick={handleResumePending}
                  disabled={isSubmitting}
                  className="flex-1"
                >
                  {isSubmitting ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                      Processing...
                    </>
                  ) : (
                    <>
                      <CreditCard className="h-4 w-4 mr-2" />
                      Complete Payment
                    </>
                  )}
                </Button>
              </div>
            </DialogFooter>
          </>
        )}

        {/* STEP: ADD ATTENDEES */}
        {step === 'add' && (
          <form
            onSubmit={(e) => {
              e.preventDefault();
              handleCalculatePrice();
            }}
          >
            <DialogHeader>
              <DialogTitle className="flex items-center gap-2">
                <Users className="h-5 w-5 text-blue-600" />
                Add Attendees
              </DialogTitle>
              <DialogDescription>
                Add new attendees to your registration for &quot;{eventTitle}&quot;.
                <br />
                <span className="text-xs text-neutral-500">
                  Current: {currentAttendeeCount} | Max: {maxAttendeesPerRegistration} |
                  Available: {remainingSlots}
                </span>
              </DialogDescription>
            </DialogHeader>

            <div className="my-4 space-y-4">
              {/* Attendee List */}
              <div className="space-y-3">
                {newAttendees.map((attendee, index) => (
                  <div
                    key={index}
                    className="p-3 bg-neutral-50 dark:bg-neutral-800 rounded-md border border-neutral-200 dark:border-neutral-700"
                  >
                    <div className="flex items-start justify-between mb-2">
                      <div className="flex items-center gap-2">
                        <User className="h-4 w-4 text-neutral-500" />
                        <span className="text-sm font-medium text-neutral-700 dark:text-neutral-300">
                          New Attendee {index + 1}
                        </span>
                      </div>
                      {newAttendees.length > 1 && (
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
                          htmlFor={`new-attendee-name-${index}`}
                          className="block text-xs font-medium text-neutral-600 dark:text-neutral-400 mb-1"
                        >
                          Name *
                        </label>
                        <input
                          id={`new-attendee-name-${index}`}
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
                              name={`new-attendee-age-${index}`}
                              checked={attendee.ageCategory === AgeCategory.Adult}
                              onChange={() => updateAttendee(index, 'ageCategory', AgeCategory.Adult)}
                              className="w-4 h-4 text-blue-600 focus:ring-blue-500"
                            />
                            <span className="text-sm text-neutral-700 dark:text-neutral-300">
                              Adult
                            </span>
                          </label>
                          <label className="flex items-center gap-2 cursor-pointer">
                            <input
                              type="radio"
                              name={`new-attendee-age-${index}`}
                              checked={attendee.ageCategory === AgeCategory.Child}
                              onChange={() => updateAttendee(index, 'ageCategory', AgeCategory.Child)}
                              className="w-4 h-4 text-blue-600 focus:ring-blue-500"
                            />
                            <span className="text-sm text-neutral-700 dark:text-neutral-300">
                              Child
                            </span>
                          </label>
                        </div>
                      </div>

                      {/* Gender (Optional) */}
                      <div>
                        <label
                          htmlFor={`new-attendee-gender-${index}`}
                          className="block text-xs font-medium text-neutral-600 dark:text-neutral-400 mb-1"
                        >
                          Gender (Optional)
                        </label>
                        <select
                          id={`new-attendee-gender-${index}`}
                          value={attendee.gender ?? ''}
                          onChange={(e) =>
                            updateAttendee(
                              index,
                              'gender',
                              e.target.value ? (parseInt(e.target.value) as Gender) : null
                            )
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

              {/* Add More Button */}
              {canAddMore && (
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={addAttendee}
                  className="w-full flex items-center justify-center gap-2"
                >
                  <Plus className="h-4 w-4" />
                  Add Another Attendee ({remainingSlots - newAttendees.length} slots left)
                </Button>
              )}

              {/* Error Messages */}
              {errors.general && (
                <p className="text-sm text-red-600">{errors.general}</p>
              )}
              {submitError && (
                <div className="p-3 bg-red-50 border border-red-200 rounded-md flex items-start gap-2">
                  <AlertCircle className="h-5 w-5 text-red-600 flex-shrink-0 mt-0.5" />
                  <p className="text-sm text-red-700">{submitError}</p>
                </div>
              )}
            </div>

            <DialogFooter>
              <div className="flex gap-2 w-full">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => onOpenChange(false)}
                  disabled={isCalculating}
                  className="flex-1"
                >
                  Cancel
                </Button>
                <Button type="submit" disabled={isCalculating} className="flex-1">
                  {isCalculating ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                      Calculating...
                    </>
                  ) : (
                    <>
                      Review &amp; Pay
                      <ArrowRight className="h-4 w-4 ml-2" />
                    </>
                  )}
                </Button>
              </div>
            </DialogFooter>
          </form>
        )}

        {/* STEP: REVIEW & PAY */}
        {step === 'review' && priceResult && (
          <>
            <DialogHeader>
              <DialogTitle className="flex items-center gap-2">
                <CreditCard className="h-5 w-5 text-green-600" />
                Review &amp; Pay
              </DialogTitle>
              <DialogDescription>
                Review the pricing details before proceeding to payment.
              </DialogDescription>
            </DialogHeader>

            <div className="my-4 space-y-4">
              {/* Event Info */}
              <div className="p-3 bg-blue-50 border border-blue-200 rounded-lg">
                <p className="text-sm font-medium text-blue-800">{priceResult.eventTitle}</p>
                <p className="text-xs text-blue-600 mt-1">
                  Current attendees: {priceResult.currentAttendeeCount} →
                  New total: {priceResult.totalAttendeeCount}
                </p>
              </div>

              {/* Attendee Breakdown */}
              <div>
                <h4 className="text-sm font-semibold text-neutral-700 dark:text-neutral-300 mb-2">
                  New Attendees ({priceResult.newAttendeesCount})
                </h4>
                <div className="space-y-2">
                  {priceResult.attendeeBreakdown.map((attendee, index) => (
                    <div
                      key={index}
                      className="flex justify-between items-center p-2 bg-neutral-50 dark:bg-neutral-800 rounded"
                    >
                      <div>
                        <p className="text-sm font-medium">{attendee.name}</p>
                        <p className="text-xs text-neutral-500">
                          {attendee.ageCategory === AgeCategory.Adult ? 'Adult' : 'Child'}
                        </p>
                      </div>
                      <p className="text-sm font-semibold">
                        {formatCurrency(attendee.price, priceResult.currency)}
                      </p>
                    </div>
                  ))}
                </div>
              </div>

              {/* Price Summary */}
              <div className="border-t pt-4 space-y-2">
                <div className="flex justify-between text-sm">
                  <span className="text-neutral-600">Previously Paid</span>
                  <span>{formatCurrency(priceResult.currentTotalPaid, priceResult.currency)}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-neutral-600">New Total</span>
                  <span>{formatCurrency(priceResult.newTotalPrice, priceResult.currency)}</span>
                </div>
                <div className="flex justify-between text-lg font-bold border-t pt-2 mt-2">
                  <span>Amount Due</span>
                  <span className="text-green-600">
                    {formatCurrency(priceResult.additionalAmount, priceResult.currency)}
                  </span>
                </div>
              </div>

              {/* Capacity Info */}
              {priceResult.remainingCapacity !== null && priceResult.remainingCapacity !== undefined && (
                <p className="text-xs text-neutral-500">
                  Event capacity: {priceResult.remainingCapacity} spots remaining after this addition
                </p>
              )}

              {/* Error */}
              {submitError && (
                <div className="p-3 bg-red-50 border border-red-200 rounded-md flex items-start gap-2">
                  <AlertCircle className="h-5 w-5 text-red-600 flex-shrink-0 mt-0.5" />
                  <p className="text-sm text-red-700">{submitError}</p>
                </div>
              )}
            </div>

            <DialogFooter>
              <div className="flex gap-2 w-full">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setStep('add')}
                  disabled={isSubmitting}
                  className="flex-1"
                >
                  <ArrowLeft className="h-4 w-4 mr-2" />
                  Back
                </Button>
                <Button
                  type="button"
                  onClick={handleInitiatePayment}
                  disabled={isSubmitting}
                  className="flex-1"
                >
                  {isSubmitting ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                      Processing...
                    </>
                  ) : (
                    <>
                      <CreditCard className="h-4 w-4 mr-2" />
                      Pay {formatCurrency(priceResult.additionalAmount, priceResult.currency)}
                    </>
                  )}
                </Button>
              </div>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
