'use client';

import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useRouter } from 'next/navigation';
import { useState, useEffect, useCallback } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Calendar, MapPin, Users, DollarSign, FileText, Tag } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { createEventSchema, type CreateEventFormData } from '@/presentation/lib/validators/event.schemas';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { EventCategory, Currency, type EventDto } from '@/infrastructure/api/types/events.types';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import { geocodeAddress } from '@/presentation/lib/utils/geocoding';
import { eventKeys } from '@/presentation/hooks/useEvents';

interface EventEditFormProps {
  event: EventDto;
}

/**
 * Event Edit Form Component
 * Allows organizers to edit existing events
 *
 * Features:
 * - Pre-filled form with existing event data
 * - Basic Information: Title, Description, Category
 * - Date & Time: Start and End dates
 * - Location: Full address details (optional)
 * - Capacity: Max attendees
 * - Pricing: Free or paid events with currency selection
 * - Validation: Zod schema with cross-field validation
 */
export function EventEditForm({ event }: EventEditFormProps) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { user } = useAuthStore();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Convert string enum to number (backend returns enums as strings due to JsonStringEnumConverter)
  // Wrapped in useCallback to prevent infinite re-renders in useEffect
  const convertCategoryToNumber = useCallback((category: any): number => {
    // If it's already a number, return it
    if (typeof category === 'number') return category;

    // If it's a string, map it to the enum value
    const categoryMap: Record<string, EventCategory> = {
      'Religious': EventCategory.Religious,
      'Cultural': EventCategory.Cultural,
      'Community': EventCategory.Community,
      'Educational': EventCategory.Educational,
      'Social': EventCategory.Social,
      'Business': EventCategory.Business,
      'Charity': EventCategory.Charity,
      'Entertainment': EventCategory.Entertainment,
    };

    return categoryMap[category] ?? EventCategory.Community;
  }, []);

  // Format dates for datetime-local input
  const formatDateForInput = (dateString: string | Date) => {
    const date = new Date(dateString);
    // Convert to local timezone and format as YYYY-MM-DDTHH:mm
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  };

  const {
    register,
    handleSubmit,
    watch,
    reset,
    formState: { errors },
  } = useForm<CreateEventFormData>({
    resolver: zodResolver(createEventSchema),
    defaultValues: {
      title: event.title,
      description: event.description,
      category: convertCategoryToNumber(event.category),
      startDate: formatDateForInput(event.startDate),
      endDate: formatDateForInput(event.endDate),
      capacity: event.capacity,
      isFree: event.isFree ?? true, // Ensure it's always a boolean
      ticketPriceAmount: event.ticketPriceAmount || undefined,
      ticketPriceCurrency: event.ticketPriceCurrency || Currency.USD,
      locationAddress: event.address || undefined,
      locationCity: event.city || undefined,
      locationState: event.state || undefined,
      locationZipCode: event.zipCode || undefined,
      locationCountry: event.country || undefined,
    },
  });

  // Reset form ONLY when event ID changes (prevents infinite re-renders)
  // We don't want to reset when user is typing!
  useEffect(() => {
    const categoryNumber = convertCategoryToNumber(event.category);
    console.log('🔄 Resetting form with event data:', {
      eventId: event.id,
      category: event.category,
      categoryType: typeof event.category,
      categoryNumber,
      isFree: event.isFree,
    });

    reset({
      title: event.title,
      description: event.description,
      category: categoryNumber,
      startDate: formatDateForInput(event.startDate),
      endDate: formatDateForInput(event.endDate),
      capacity: event.capacity,
      isFree: event.isFree,
      ticketPriceAmount: event.ticketPriceAmount || undefined,
      ticketPriceCurrency: event.ticketPriceCurrency || Currency.USD,
      locationAddress: event.address || undefined,
      locationCity: event.city || undefined,
      locationState: event.state || undefined,
      locationZipCode: event.zipCode || undefined,
      locationCountry: event.country || undefined,
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [event.id]); // Only reset when navigating to different event

  const isFree = watch('isFree');

  const onSubmit = handleSubmit(async (data) => {
    if (!user?.userId) {
      setSubmitError('You must be logged in to edit events');
      return;
    }

    if (event.organizerId !== user.userId) {
      setSubmitError('You can only edit your own events');
      return;
    }

    try {
      setIsSubmitting(true);
      setSubmitError(null);

      console.log('📋 Form Submission - Updating Event:', {
        eventId: event.id,
        userId: user.userId,
        userRole: user.role,
      });

      // Prepare event data for backend
      // UpdateEventRequest matches backend contract (excludes organizerId)
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

      // Convert datetime-local format to ISO 8601
      const startDateISO = new Date(data.startDate).toISOString();
      const endDateISO = new Date(data.endDate).toISOString();

      const eventData = {
        eventId: event.id,
        title: data.title,
        description: data.description,
        startDate: startDateISO,
        endDate: endDateISO,
        capacity: data.capacity,
        category: data.category,
        // Backend expects: LocationAddress, LocationCity, LocationState, LocationZipCode, LocationCountry
        // Backend infers isFree from ticketPriceAmount being null, so don't send isFree
        // CRITICAL: Use null for empty optional fields, NOT empty strings
        ...(hasCompleteLocation && {
          locationAddress: data.locationAddress,
          locationCity: data.locationCity,
          locationState: data.locationState || null,
          locationZipCode: data.locationZipCode || null,
          locationCountry: data.locationCountry || null,
          locationLatitude: locationLatitude ?? null,
          locationLongitude: locationLongitude ?? null,
        }),
        // Use null (not undefined) for nullable fields to match C# nullable types
        ticketPriceAmount: data.isFree ? null : data.ticketPriceAmount!,
        ticketPriceCurrency: data.isFree ? null : data.ticketPriceCurrency!,
      };

      console.log('📤 Updating event with payload:', JSON.stringify(eventData, null, 2));
      console.log('📋 Event details before update:', {
        eventId: event.id,
        eventIdType: typeof event.id,
        currentCategory: event.category,
        currentCategoryType: typeof event.category,
        newCategory: data.category,
        newCategoryType: typeof data.category,
        eventStatus: event.status,
        isFree: data.isFree,
        ticketPriceAmount: data.ticketPriceAmount,
        ticketPriceCurrency: data.ticketPriceCurrency,
      });

      console.log('🌐 API Request Details:', {
        url: `/events/${event.id}`,
        method: 'PUT',
        payloadSize: JSON.stringify(eventData).length,
        payloadKeys: Object.keys(eventData),
      });

      await eventsRepository.updateEvent(event.id, eventData);
      console.log('✅ Event updated successfully!');

      // Invalidate React Query cache to refresh event data
      await queryClient.invalidateQueries({ queryKey: eventKeys.detail(event.id) });
      await queryClient.invalidateQueries({ queryKey: eventKeys.lists() });
      console.log('🔄 Cache invalidated - fresh data will be fetched');

      // Redirect to event manage page
      router.push(`/events/${event.id}/manage`);
    } catch (err) {
      console.error('❌ Event update failed:', err);

      const errorMessage = err instanceof Error
        ? err.message
        : 'Failed to update event. Please try again.';
      setSubmitError(errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  });

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
            Update the essential details about your event
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
            Update when your event will take place
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
            Update where the event will take place (Optional but recommended)
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
            Update attendance limits and ticket pricing
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
                    step="0.01"
                    placeholder="e.g., 25.00"
                    error={!!errors.ticketPriceAmount}
                    {...register('ticketPriceAmount', { valueAsNumber: true })}
                  />
                  {errors.ticketPriceAmount && (
                    <p className="mt-1 text-sm text-destructive">{errors.ticketPriceAmount.message}</p>
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
            </div>
          )}
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
                📸 Manage Images & Videos
              </h4>
              <p className="text-sm text-blue-700">
                You can add or remove event images and videos from the event detail page.
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

      {/* Form Actions */}
      <div className="flex items-center justify-end gap-4">
        <Button
          type="button"
          variant="outline"
          onClick={() => router.push(`/events/${event.id}`)}
          disabled={isSubmitting}
        >
          Cancel
        </Button>
        <Button
          type="submit"
          disabled={isSubmitting}
          style={{ background: '#FF7900' }}
          className="min-w-[150px]"
        >
          {isSubmitting ? 'Updating...' : 'Update Event'}
        </Button>
      </div>
    </form>
  );
}
