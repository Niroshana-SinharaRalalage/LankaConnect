'use client';

import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { Calendar, MapPin, Users, DollarSign, FileText, Tag, Mail } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { MultiSelect } from '@/presentation/components/ui/MultiSelect';
import { createEventSchema, type CreateEventFormData } from '@/presentation/lib/validators/event.schemas';
import { useCreateEvent } from '@/presentation/hooks/useEvents';
import { useEmailGroups } from '@/presentation/hooks/useEmailGroups';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { EventCategory, Currency } from '@/infrastructure/api/types/events.types';
import { geocodeAddress } from '@/presentation/lib/utils/geocoding';
import { GroupPricingTierBuilder } from './GroupPricingTierBuilder';

/**
 * Event Creation Form Component
 * Allows organizers to create new events with comprehensive details
 *
 * Features:
 * - Basic Information: Title, Description, Category
 * - Date & Time: Start and End dates
 * - Location: Full address details (optional)
 * - Capacity: Max attendees
 * - Pricing: Free or paid events with currency selection
 * - Validation: Zod schema with cross-field validation
 */
export function EventCreationForm() {
  const router = useRouter();
  const { user } = useAuthStore();
  const createEventMutation = useCreateEvent();
  const [submitError, setSubmitError] = useState<string | null>(null);

  // Phase 6A.32: Fetch email groups for selection
  const { data: emailGroups = [], isLoading: isLoadingEmailGroups } = useEmailGroups();

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm({
    resolver: zodResolver(createEventSchema),
    defaultValues: {
      isFree: true,
      enableDualPricing: false,
      enableGroupPricing: false,
      groupPricingTiers: [],
      capacity: 50,
      // Session 33: Don't set default currencies - they should only be set when user enters pricing mode
      // This prevents validation errors when switching between pricing modes
      ticketPriceAmount: undefined,
      ticketPriceCurrency: undefined,
      adultPriceAmount: undefined,
      adultPriceCurrency: Currency.USD,
      childPriceAmount: undefined,
      childPriceCurrency: Currency.USD,
      childAgeLimit: undefined,
      category: EventCategory.Community, // Default to Community instead of empty
    },
  });

  const isFree = watch('isFree');
  const enableDualPricing = watch('enableDualPricing');
  const enableGroupPricing = watch('enableGroupPricing');
  const groupPricingTiers = watch('groupPricingTiers') || [];

  // Session 33: Track validation errors for display
  const hasErrors = Object.keys(errors).length > 0;

  // Session 33: Handle form validation errors - displays message when validation fails
  const onValidationError = (validationErrors: Record<string, any>) => {
    console.error('❌ Form Validation Failed:', validationErrors);
    // List all validation error fields for debugging
    const errorFields = Object.keys(validationErrors);
    console.error('Fields with errors:', errorFields);

    // Session 33: Debug - log current form values for pricing mode
    console.log('🔍 Debug - Current form state:', {
      isFree,
      enableDualPricing,
      enableGroupPricing,
      ticketPriceAmount: watch('ticketPriceAmount'),
      ticketPriceCurrency: watch('ticketPriceCurrency'),
      adultPriceAmount: watch('adultPriceAmount'),
      childPriceAmount: watch('childPriceAmount'),
    });

    setSubmitError(`Please fix the validation errors: ${errorFields.join(', ')}`);
    // Scroll to top of form to show errors
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const onSubmit = handleSubmit(async (data) => {
    if (!user?.userId) {
      setSubmitError('You must be logged in to create events');
      return;
    }

    try {
      setSubmitError(null);

      // PHASE 6A.10: Log user authentication state
      console.log('📋 Form Submission - User Context:', {
        userId: user.userId,
        userRole: user.role,
        userName: user.fullName,
        isAuthenticated: true,
      });

      // Prepare event data for backend
      // IMPORTANT: Location fields are ALL required if ANY location field is provided
      // Database constraint: Address must have Street, City, State, ZipCode, Country
      const hasCompleteLocation = !!(data.locationAddress && data.locationCity);

      // Geocode address to get lat/long coordinates for location-based filtering
      let locationLatitude: number | undefined;
      let locationLongitude: number | undefined;

      if (hasCompleteLocation) {
        console.log('🗺️ Geocoding address for location-based filtering...');
        const geocodeResult = await geocodeAddress(
          data.locationAddress!,
          data.locationCity!,
          data.locationState || undefined,
          data.locationCountry || 'United States',
          data.locationZipCode || undefined
        );

        if (geocodeResult) {
          locationLatitude = geocodeResult.latitude;
          locationLongitude = geocodeResult.longitude;
          console.log('✅ Geocoding successful:', {
            lat: locationLatitude,
            lon: locationLongitude,
            display: geocodeResult.displayName,
          });
        } else {
          console.warn('⚠️ Geocoding failed - event will not appear in location-based filters');
          // Continue anyway - location text will still be saved
        }
      }

      const eventData = {
        title: data.title,
        description: data.description,
        startDate: data.startDate,
        endDate: data.endDate,
        organizerId: user.userId,
        capacity: data.capacity,
        category: data.category,
        // Phase 6A.32: Email Groups Integration
        emailGroupIds: data.emailGroupIds || [],
        // Only include location if we have at least address and city
        ...(hasCompleteLocation && {
          locationAddress: data.locationAddress,
          locationCity: data.locationCity,
          locationState: data.locationState || '',
          locationZipCode: data.locationZipCode || '',
          locationCountry: data.locationCountry || 'United States',
          locationLatitude,
          locationLongitude,
        }),
        // Phase 6D: Group tiered pricing (highest priority)
        // Session 21: Dual pricing support
        // Legacy: Single pricing
        ...(data.isFree
          ? {}
          : data.enableGroupPricing
          ? {
              // Send group tiered pricing
              groupPricingTiers: data.groupPricingTiers?.map((tier) => ({
                minAttendees: tier.minAttendees,
                maxAttendees: tier.maxAttendees,
                pricePerPerson: tier.pricePerPerson,
                currency: tier.currency,
              })),
            }
          : data.enableDualPricing
          ? {
              // Send dual pricing
              adultPriceAmount: data.adultPriceAmount!,
              adultPriceCurrency: data.adultPriceCurrency!,
              childPriceAmount: data.childPriceAmount!,
              childPriceCurrency: data.childPriceCurrency!,
              childAgeLimit: data.childAgeLimit!,
            }
          : {
              // Send single pricing (legacy format)
              ticketPriceAmount: data.ticketPriceAmount!,
              ticketPriceCurrency: data.ticketPriceCurrency!,
            }),
      };

      // PHASE 6A.10: Comprehensive payload logging
      console.log('📤 Creating event with payload:', JSON.stringify(eventData, null, 2));
      console.log('📊 Payload Analysis:', {
        hasLocation: hasCompleteLocation,
        hasCoordinates: !!(locationLatitude && locationLongitude),
        isFree: data.isFree,
        hasTicketPrice: !!data.ticketPriceAmount,
        categoryValue: data.category,
        dataSize: JSON.stringify(eventData).length,
      });

      console.log('⏳ Sending request to API...');
      const eventId = await createEventMutation.mutateAsync(eventData);
      console.log('✅ Event created successfully! ID:', eventId);

      // Session 33: Redirect to dashboard instead of event management page
      router.push('/dashboard');
    } catch (err) {
      // PHASE 6A.10: Enhanced error logging
      console.error('❌ Event creation failed - Detailed Analysis:');
      console.error('Error Type:', err?.constructor?.name);
      console.error('Error Message:', err instanceof Error ? err.message : 'Unknown error');
      console.error('Error Stack:', err instanceof Error ? err.stack : 'No stack trace');

      // Extract detailed error message from API response
      const errorMessage = err instanceof Error
        ? err.message
        : typeof err === 'object' && err !== null && 'message' in err
        ? String(err.message)
        : 'Failed to create event. Please try again.';
      setSubmitError(errorMessage);

      // Log full error for debugging
      console.error('Full error object:', JSON.stringify(err, Object.getOwnPropertyNames(err), 2));

      // Check if error has response data
      if (typeof err === 'object' && err !== null && 'response' in err) {
        const axiosError = err as any;
        console.error('Response Status:', axiosError.response?.status);
        console.error('Response Headers:', axiosError.response?.headers);
        console.error('Response Data:', axiosError.response?.data);
      }
    }
  }, onValidationError);  // Session 33: Add validation error callback

  // Category labels
  const categoryOptions = [
    { value: EventCategory.Religious, label: 'Religious' },
    { value: EventCategory.Cultural, label: 'Cultural' },
    { value: EventCategory.Community, label: 'Community' },
    { value: EventCategory.Educational, label: 'Educational' },
    { value: EventCategory.Social, label: 'Social' },
    { value: EventCategory.Business, label: 'Business' },
    { value: EventCategory.Charity, label: 'Charity' },
    { value: EventCategory.Entertainment, label: 'Entertainment' },
  ];

  return (
    <form onSubmit={onSubmit} className="space-y-6">
      {/* Basic Information Section */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <FileText className="h-5 w-5" style={{ color: '#FF7900' }} />
            <CardTitle style={{ color: '#8B1538' }}>Basic Information</CardTitle>
          </div>
          <CardDescription>
            Provide the essential details about your event
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Event Title */}
          <div>
            <label htmlFor="title" className="block text-sm font-medium text-neutral-700 mb-2">
              Event Title *
            </label>
            <Input
              id="title"
              type="text"
              placeholder="e.g., Sri Lankan Cultural Festival 2025"
              error={!!errors.title}
              {...register('title')}
            />
            {errors.title && (
              <p className="mt-1 text-sm text-destructive">{errors.title.message}</p>
            )}
          </div>

          {/* Event Description */}
          <div>
            <label htmlFor="description" className="block text-sm font-medium text-neutral-700 mb-2">
              Event Description *
            </label>
            <textarea
              id="description"
              rows={6}
              placeholder="Provide a detailed description of your event, including what attendees can expect..."
              className={`w-full px-4 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 resize-none ${
                errors.description ? 'border-destructive' : 'border-neutral-300'
              }`}
              {...register('description')}
            />
            {errors.description && (
              <p className="mt-1 text-sm text-destructive">{errors.description.message}</p>
            )}
          </div>

          {/* Event Category */}
          <div>
            <label htmlFor="category" className="block text-sm font-medium text-neutral-700 mb-2">
              Event Category *
            </label>
            <div className="relative">
              <Tag className="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-neutral-400" />
              <select
                id="category"
                className={`w-full pl-10 pr-4 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 appearance-none ${
                  errors.category ? 'border-destructive' : 'border-neutral-300'
                }`}
                {...register('category', { valueAsNumber: true })}
              >
                {categoryOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </div>
            {errors.category && (
              <p className="mt-1 text-sm text-destructive">{errors.category.message}</p>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Date & Time Section */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Calendar className="h-5 w-5" style={{ color: '#FF7900' }} />
            <CardTitle style={{ color: '#8B1538' }}>Date & Time</CardTitle>
          </div>
          <CardDescription>
            Specify when your event will take place
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Start Date & Time */}
            <div>
              <label htmlFor="startDate" className="block text-sm font-medium text-neutral-700 mb-2">
                Start Date & Time *
              </label>
              <Input
                id="startDate"
                type="datetime-local"
                error={!!errors.startDate}
                {...register('startDate')}
              />
              {errors.startDate && (
                <p className="mt-1 text-sm text-destructive">{errors.startDate.message}</p>
              )}
            </div>

            {/* End Date & Time */}
            <div>
              <label htmlFor="endDate" className="block text-sm font-medium text-neutral-700 mb-2">
                End Date & Time *
              </label>
              <Input
                id="endDate"
                type="datetime-local"
                error={!!errors.endDate}
                {...register('endDate')}
              />
              {errors.endDate && (
                <p className="mt-1 text-sm text-destructive">{errors.endDate.message}</p>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Location Section */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <MapPin className="h-5 w-5" style={{ color: '#FF7900' }} />
            <CardTitle style={{ color: '#8B1538' }}>Location</CardTitle>
          </div>
          <CardDescription>
            Where will the event take place? (Optional but recommended)
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Address */}
          <div>
            <label htmlFor="locationAddress" className="block text-sm font-medium text-neutral-700 mb-2">
              Street Address
            </label>
            <Input
              id="locationAddress"
              type="text"
              placeholder="e.g., 123 Main Street"
              error={!!errors.locationAddress}
              {...register('locationAddress')}
            />
            {errors.locationAddress && (
              <p className="mt-1 text-sm text-destructive">{errors.locationAddress.message}</p>
            )}
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* City */}
            <div>
              <label htmlFor="locationCity" className="block text-sm font-medium text-neutral-700 mb-2">
                City
              </label>
              <Input
                id="locationCity"
                type="text"
                placeholder="e.g., Columbus"
                error={!!errors.locationCity}
                {...register('locationCity')}
              />
              {errors.locationCity && (
                <p className="mt-1 text-sm text-destructive">{errors.locationCity.message}</p>
              )}
            </div>

            {/* State */}
            <div>
              <label htmlFor="locationState" className="block text-sm font-medium text-neutral-700 mb-2">
                State
              </label>
              <Input
                id="locationState"
                type="text"
                placeholder="e.g., Ohio"
                error={!!errors.locationState}
                {...register('locationState')}
              />
              {errors.locationState && (
                <p className="mt-1 text-sm text-destructive">{errors.locationState.message}</p>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* ZIP Code */}
            <div>
              <label htmlFor="locationZipCode" className="block text-sm font-medium text-neutral-700 mb-2">
                ZIP Code
              </label>
              <Input
                id="locationZipCode"
                type="text"
                placeholder="e.g., 43201"
                error={!!errors.locationZipCode}
                {...register('locationZipCode')}
              />
              {errors.locationZipCode && (
                <p className="mt-1 text-sm text-destructive">{errors.locationZipCode.message}</p>
              )}
            </div>

            {/* Country */}
            <div>
              <label htmlFor="locationCountry" className="block text-sm font-medium text-neutral-700 mb-2">
                Country
              </label>
              <Input
                id="locationCountry"
                type="text"
                placeholder="e.g., United States"
                error={!!errors.locationCountry}
                {...register('locationCountry')}
              />
              {errors.locationCountry && (
                <p className="mt-1 text-sm text-destructive">{errors.locationCountry.message}</p>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Capacity & Pricing Section */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Users className="h-5 w-5" style={{ color: '#FF7900' }} />
            <CardTitle style={{ color: '#8B1538' }}>Capacity & Pricing</CardTitle>
          </div>
          <CardDescription>
            Set attendance limits and ticket pricing
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Capacity */}
          <div>
            <label htmlFor="capacity" className="block text-sm font-medium text-neutral-700 mb-2">
              Maximum Capacity *
            </label>
            <Input
              id="capacity"
              type="number"
              min="1"
              max="10000"
              placeholder="e.g., 100"
              error={!!errors.capacity}
              {...register('capacity', { valueAsNumber: true })}
            />
            {errors.capacity && (
              <p className="mt-1 text-sm text-destructive">{errors.capacity.message}</p>
            )}
          </div>

          {/* Free Event Toggle */}
          <div className="flex items-center gap-3 p-4 bg-neutral-50 rounded-lg">
            <input
              id="isFree"
              type="checkbox"
              className="h-5 w-5 rounded border-neutral-300 text-orange-500 focus:ring-2 focus:ring-orange-500"
              {...register('isFree')}
            />
            <label htmlFor="isFree" className="text-sm font-medium text-neutral-700">
              This is a free event (no ticket purchase required)
            </label>
          </div>

          {/* Pricing Fields (shown only if not free) */}
          {!isFree && (
            <div className="space-y-4 p-4 border-2 border-orange-200 rounded-lg bg-orange-50">
              <div className="flex items-center gap-2 mb-2">
                <DollarSign className="h-5 w-5" style={{ color: '#FF7900' }} />
                <h4 className="text-sm font-semibold text-neutral-900">Ticket Pricing</h4>
              </div>

              {/* Pricing Mode Selection */}
              <div className="space-y-3">
                {/* Dual Pricing Toggle - Session 33: Fixed onChange override bug + clear fields */}
                <div className="flex items-center gap-3 p-3 bg-white rounded-lg border border-orange-200">
                  <input
                    id="enableDualPricing"
                    type="checkbox"
                    className="h-5 w-5 rounded border-neutral-300 text-orange-500 focus:ring-2 focus:ring-orange-500"
                    {...register('enableDualPricing', {
                      onChange: (e) => {
                        if (e.target.checked) {
                          // Disable other pricing modes
                          setValue('enableGroupPricing', false);
                          // Clear single pricing fields to prevent validation conflicts
                          setValue('ticketPriceAmount', undefined);
                          setValue('ticketPriceCurrency', undefined);
                          // Clear group pricing fields
                          setValue('groupPricingTiers', []);
                          // Set default currencies for dual pricing
                          setValue('adultPriceCurrency', Currency.USD);
                          setValue('childPriceCurrency', Currency.USD);
                        } else {
                          // When unchecking, clear dual pricing fields
                          setValue('adultPriceAmount', undefined);
                          setValue('adultPriceCurrency', undefined);
                          setValue('childPriceAmount', undefined);
                          setValue('childPriceCurrency', undefined);
                          setValue('childAgeLimit', undefined);
                          // Set default for single pricing
                          setValue('ticketPriceCurrency', Currency.USD);
                        }
                      }
                    })}
                  />
                  <label htmlFor="enableDualPricing" className="text-sm font-medium text-neutral-700">
                    Enable Adult/Child Pricing (different prices for adults and children)
                  </label>
                </div>

                {/* Group Pricing Toggle - Phase 6D - Session 33: Fixed onChange override bug + clear fields */}
                <div className="flex items-center gap-3 p-3 bg-white rounded-lg border border-orange-200">
                  <input
                    id="enableGroupPricing"
                    type="checkbox"
                    className="h-5 w-5 rounded border-neutral-300 text-orange-500 focus:ring-2 focus:ring-orange-500"
                    {...register('enableGroupPricing', {
                      onChange: (e) => {
                        if (e.target.checked) {
                          // Disable other pricing modes
                          setValue('enableDualPricing', false);
                          // Clear single pricing fields
                          setValue('ticketPriceAmount', undefined);
                          setValue('ticketPriceCurrency', undefined);
                          // Clear dual pricing fields
                          setValue('adultPriceAmount', undefined);
                          setValue('adultPriceCurrency', undefined);
                          setValue('childPriceAmount', undefined);
                          setValue('childPriceCurrency', undefined);
                          setValue('childAgeLimit', undefined);
                        } else {
                          // When unchecking, clear group pricing fields
                          setValue('groupPricingTiers', []);
                          // Set default for single pricing
                          setValue('ticketPriceCurrency', Currency.USD);
                        }
                      }
                    })}
                  />
                  <label htmlFor="enableGroupPricing" className="text-sm font-medium text-neutral-700">
                    Enable Group Tiered Pricing (quantity-based discounts for groups)
                  </label>
                </div>
              </div>

              {/* Single Pricing Fields (default) */}
              {!enableDualPricing && !enableGroupPricing && (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {/* Ticket Price */}
                  <div>
                    <label htmlFor="ticketPriceAmount" className="block text-sm font-medium text-neutral-700 mb-2">
                      Ticket Price *
                    </label>
                    <Input
                      id="ticketPriceAmount"
                      type="number"
                      min="0"
                      max="10000"
                      step="1"
                      placeholder="e.g., 25"
                      error={!!errors.ticketPriceAmount}
                      {...register('ticketPriceAmount', { valueAsNumber: true })}
                    />
                    {errors.ticketPriceAmount && (
                      <p className="mt-1 text-sm text-destructive">{errors.ticketPriceAmount.message}</p>
                    )}
                    {/* Session 33: Commission info message - standardized format */}
                    {(watch('ticketPriceAmount') ?? 0) > 0 && (
                      <div className="mt-2 p-2 bg-gray-50 border border-gray-200 rounded text-xs text-gray-600">
                        <p>5% (Stripe + LankaConnect commission) applies</p>
                        <p className="font-medium text-green-700">
                          You'll receive: ${((watch('ticketPriceAmount') ?? 0) * 0.95).toFixed(2)} per ticket
                        </p>
                      </div>
                    )}
                  </div>

                  {/* Currency */}
                  <div>
                    <label htmlFor="ticketPriceCurrency" className="block text-sm font-medium text-neutral-700 mb-2">
                      Currency *
                    </label>
                    <select
                      id="ticketPriceCurrency"
                      className={`w-full px-4 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 ${
                        errors.ticketPriceCurrency ? 'border-destructive' : 'border-neutral-300'
                      }`}
                      defaultValue={Currency.USD}
                      {...register('ticketPriceCurrency', { valueAsNumber: true })}
                    >
                      <option value={Currency.USD}>USD ($)</option>
                      <option value={Currency.LKR}>LKR (Rs)</option>
                    </select>
                    {errors.ticketPriceCurrency && (
                      <p className="mt-1 text-sm text-destructive">{errors.ticketPriceCurrency.message}</p>
                    )}
                  </div>
                </div>
              )}

              {/* Group Pricing Tier Builder - Phase 6D */}
              {enableGroupPricing && (
                <div className="space-y-4">
                  <GroupPricingTierBuilder
                    tiers={groupPricingTiers}
                    onChange={(tiers) => setValue('groupPricingTiers', tiers)}
                    defaultCurrency={watch('ticketPriceCurrency') || Currency.USD}
                    errors={errors.groupPricingTiers?.message}
                  />
                </div>
              )}

              {/* Dual Pricing Fields (adult/child) */}
              {enableDualPricing && !enableGroupPricing && (
                <div className="space-y-4">
                  {/* Adult Pricing Row */}
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label htmlFor="adultPriceAmount" className="block text-sm font-medium text-neutral-700 mb-2">
                        Adult Ticket Price *
                      </label>
                      <Input
                        id="adultPriceAmount"
                        type="number"
                        min="0"
                        max="10000"
                        step="1"
                        placeholder="e.g., 25"
                        error={!!errors.adultPriceAmount}
                        {...register('adultPriceAmount', { valueAsNumber: true })}
                      />
                      {errors.adultPriceAmount && (
                        <p className="mt-1 text-sm text-destructive">{errors.adultPriceAmount.message}</p>
                      )}
                      {/* Session 33: Commission info for adult price - standardized format */}
                      {(watch('adultPriceAmount') ?? 0) > 0 && (
                        <div className="mt-2 p-2 bg-gray-50 border border-gray-200 rounded text-xs text-gray-600">
                          <p>5% (Stripe + LankaConnect commission) applies</p>
                          <p className="font-medium text-green-700">
                            You'll receive: ${((watch('adultPriceAmount') ?? 0) * 0.95).toFixed(2)} per ticket
                          </p>
                        </div>
                      )}
                    </div>

                    <div>
                      <label htmlFor="adultPriceCurrency" className="block text-sm font-medium text-neutral-700 mb-2">
                        Currency *
                      </label>
                      <select
                        id="adultPriceCurrency"
                        className={`w-full px-4 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 ${
                          errors.adultPriceCurrency ? 'border-destructive' : 'border-neutral-300'
                        }`}
                        defaultValue={Currency.USD}
                        {...register('adultPriceCurrency', { valueAsNumber: true })}
                      >
                        <option value={Currency.USD}>USD ($)</option>
                        <option value={Currency.LKR}>LKR (Rs)</option>
                      </select>
                      {errors.adultPriceCurrency && (
                        <p className="mt-1 text-sm text-destructive">{errors.adultPriceCurrency.message}</p>
                      )}
                    </div>
                  </div>

                  {/* Child Pricing Row */}
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div>
                      <label htmlFor="childPriceAmount" className="block text-sm font-medium text-neutral-700 mb-2">
                        Child Ticket Price *
                      </label>
                      <Input
                        id="childPriceAmount"
                        type="number"
                        min="0"
                        max="10000"
                        step="1"
                        placeholder="e.g., 15"
                        error={!!errors.childPriceAmount}
                        {...register('childPriceAmount', { valueAsNumber: true })}
                      />
                      {errors.childPriceAmount && (
                        <p className="mt-1 text-sm text-destructive">{errors.childPriceAmount.message}</p>
                      )}
                      {/* Session 33: Commission info for child price - standardized format */}
                      {(watch('childPriceAmount') ?? 0) > 0 && (
                        <div className="mt-2 p-2 bg-gray-50 border border-gray-200 rounded text-xs text-gray-600">
                          <p>5% (Stripe + LankaConnect commission) applies</p>
                          <p className="font-medium text-green-700">
                            You'll receive: ${((watch('childPriceAmount') ?? 0) * 0.95).toFixed(2)} per ticket
                          </p>
                        </div>
                      )}
                    </div>

                    <div>
                      <label htmlFor="childPriceCurrency" className="block text-sm font-medium text-neutral-700 mb-2">
                        Currency *
                      </label>
                      <select
                        id="childPriceCurrency"
                        className={`w-full px-4 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 ${
                          errors.childPriceCurrency ? 'border-destructive' : 'border-neutral-300'
                        }`}
                        defaultValue={Currency.USD}
                        {...register('childPriceCurrency', { valueAsNumber: true })}
                      >
                        <option value={Currency.USD}>USD ($)</option>
                        <option value={Currency.LKR}>LKR (Rs)</option>
                      </select>
                      {errors.childPriceCurrency && (
                        <p className="mt-1 text-sm text-destructive">{errors.childPriceCurrency.message}</p>
                      )}
                    </div>

                    <div>
                      <label htmlFor="childAgeLimit" className="block text-sm font-medium text-neutral-700 mb-2">
                        Child Age Limit *
                      </label>
                      <Input
                        id="childAgeLimit"
                        type="number"
                        min="1"
                        max="18"
                        placeholder="12"
                        error={!!errors.childAgeLimit}
                        {...register('childAgeLimit', { valueAsNumber: true })}
                      />
                      {errors.childAgeLimit && (
                        <p className="mt-1 text-sm text-destructive">{errors.childAgeLimit.message}</p>
                      )}
                      <p className="mt-1 text-xs text-neutral-500">Age under which child pricing applies (1-18)</p>
                    </div>
                  </div>

                  {/* Helpful note */}
                  <div className="flex items-start gap-2 p-3 bg-blue-50 border border-blue-200 rounded-lg">
                    <svg className="w-5 h-5 text-blue-600 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                      <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
                    </svg>
                    <p className="text-xs text-blue-700">
                      Example: If child age limit is 12, attendees age 11 and under will be charged the child price, while attendees age 12 and over will be charged the adult price.
                    </p>
                  </div>
                </div>
              )}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Phase 6A.32: Email Groups Section */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Mail className="h-5 w-5" style={{ color: '#FF7900' }} />
            <CardTitle style={{ color: '#8B1538' }}>Email Groups (Optional)</CardTitle>
          </div>
          <CardDescription>
            Select email groups to notify about this event
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <MultiSelect
            options={emailGroups.map(group => ({
              id: group.id,
              label: group.name,
              disabled: !group.isActive
            }))}
            value={watch('emailGroupIds') || []}
            onChange={(ids) => setValue('emailGroupIds', ids)}
            placeholder="Select email groups to notify"
            isLoading={isLoadingEmailGroups}
            error={!!errors.emailGroupIds}
            errorMessage={errors.emailGroupIds?.message}
            helperText="Select groups that should receive invitations for this event"
          />
        </CardContent>
      </Card>

      {/* Note about Media */}
      <Card>
        <CardContent className="py-6">
          <div className="flex items-start gap-3 p-4 bg-blue-50 border border-blue-200 rounded-lg">
            <div className="flex-shrink-0">
              <svg className="w-5 h-5 text-blue-600 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
              </svg>
            </div>
            <div className="flex-1">
              <h4 className="text-sm font-semibold text-blue-900 mb-1">
                📸 Upload Images & Videos After Creation
              </h4>
              <p className="text-sm text-blue-700">
                You'll be able to add event images and videos from the event detail page after creating your event.
                This helps make your event more attractive to attendees!
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Error Message */}
      {submitError && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
          <p className="text-sm text-red-600">{submitError}</p>
        </div>
      )}

      {/* Session 33: Validation Error Summary - Shows when form has validation errors */}
      {hasErrors && !submitError && (
        <div className="p-4 bg-amber-50 border border-amber-200 rounded-lg">
          <div className="flex items-start gap-2">
            <svg className="w-5 h-5 text-amber-600 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
            </svg>
            <div>
              <p className="text-sm font-medium text-amber-800">Please fix the following errors:</p>
              <ul className="mt-1 text-sm text-amber-700 list-disc list-inside">
                {Object.entries(errors).map(([field, error]) => (
                  <li key={field}>{field}: {(error as any)?.message || 'Invalid'}</li>
                ))}
              </ul>
            </div>
          </div>
        </div>
      )}

      {/* Form Actions */}
      <div className="flex items-center justify-end gap-4">
        <Button
          type="button"
          variant="outline"
          onClick={() => router.push('/dashboard')}
          disabled={isSubmitting}
        >
          Cancel
        </Button>
        <Button
          type="submit"
          disabled={isSubmitting || createEventMutation.isPending}
          style={{ background: '#FF7900' }}
          className="min-w-[150px]"
        >
          {isSubmitting || createEventMutation.isPending ? 'Creating...' : 'Create Event'}
        </Button>
      </div>
    </form>
  );
}
