'use client';

import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useState, useEffect } from 'react';
import { Mail, FileText, Users, MapPin } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { MultiSelect } from '@/presentation/components/ui/MultiSelect';
import { createNewsletterSchema, type CreateNewsletterFormData } from '@/presentation/lib/validators/newsletter.schemas';
import { useCreateNewsletter, useUpdateNewsletter, useNewsletterById } from '@/presentation/hooks/useNewsletters';
import { useEmailGroups } from '@/presentation/hooks/useEmailGroups';
import { useEvents } from '@/presentation/hooks/useEvents';
import { useAuthStore } from '@/presentation/store/useAuthStore';

export interface NewsletterFormProps {
  newsletterId?: string;
  onSuccess?: () => void;
  onCancel?: () => void;
}

export function NewsletterForm({ newsletterId, onSuccess, onCancel }: NewsletterFormProps) {
  const isEditMode = !!newsletterId;
  const createMutation = useCreateNewsletter();
  const updateMutation = useUpdateNewsletter();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const { user } = useAuthStore();

  // Fetch data
  const { data: newsletter, isLoading: isLoadingNewsletter } = useNewsletterById(newsletterId || '', {
    enabled: isEditMode,
  });
  const { data: emailGroups = [], isLoading: isLoadingEmailGroups } = useEmailGroups();
  // Fetch events (TODO: Filter by organizer when API supports it)
  const { data: events = [], isLoading: isLoadingEvents } = useEvents({});

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateNewsletterFormData>({
    resolver: zodResolver(createNewsletterSchema),
    defaultValues: {
      title: '',
      description: '',
      emailGroupIds: undefined,
      includeNewsletterSubscribers: true,
      eventId: undefined,
      targetAllLocations: false,
      metroAreaIds: undefined,
    },
  });

  // Watch fields for conditional rendering
  const includeNewsletterSubscribers = watch('includeNewsletterSubscribers');
  const eventId = watch('eventId');
  const targetAllLocations = watch('targetAllLocations');

  // Show location targeting only if: includeNewsletterSubscribers && !eventId
  const showLocationTargeting = includeNewsletterSubscribers && !eventId;

  // Load newsletter data for edit mode
  useEffect(() => {
    if (newsletter && isEditMode) {
      reset({
        title: newsletter.title,
        description: newsletter.description,
        emailGroupIds: newsletter.emailGroupIds || undefined,
        includeNewsletterSubscribers: newsletter.includeNewsletterSubscribers,
        eventId: newsletter.eventId || undefined,
        targetAllLocations: newsletter.targetAllLocations,
        metroAreaIds: newsletter.metroAreaIds || undefined,
      });
    }
  }, [newsletter, isEditMode, reset]);

  const onSubmit = handleSubmit(async (data) => {
    try {
      setSubmitError(null);

      if (isEditMode && newsletterId) {
        await updateMutation.mutateAsync({ id: newsletterId, ...data });
      } else {
        await createMutation.mutateAsync(data);
      }

      onSuccess?.();
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to save newsletter. Please try again.';
      setSubmitError(errorMessage);
    }
  });

  if (isEditMode && isLoadingNewsletter) {
    return <div className="text-center py-12">Loading newsletter...</div>;
  }

  return (
    <form onSubmit={onSubmit} className="space-y-6">
      {/* Submit Error */}
      {submitError && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg">
          {submitError}
        </div>
      )}

      {/* Basic Information */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <FileText className="h-5 w-5" style={{ color: '#FF7900' }} />
            <CardTitle style={{ color: '#8B1538' }}>Basic Information</CardTitle>
          </div>
          <CardDescription>
            Provide the title and content for your newsletter
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Title */}
          <div>
            <label htmlFor="title" className="block text-sm font-medium text-neutral-700 mb-2">
              Newsletter Title *
            </label>
            <Input
              id="title"
              type="text"
              placeholder="e.g., Monthly Community Update - January 2026"
              error={!!errors.title}
              {...register('title')}
            />
            {errors.title && (
              <p className="mt-1 text-sm text-destructive">{errors.title.message}</p>
            )}
          </div>

          {/* Description */}
          <div>
            <label htmlFor="description" className="block text-sm font-medium text-neutral-700 mb-2">
              Newsletter Content *
            </label>
            <textarea
              id="description"
              rows={6}
              placeholder="Write your newsletter content here..."
              className={`w-full px-4 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 resize-none ${
                errors.description ? 'border-destructive' : 'border-neutral-300'
              }`}
              {...register('description')}
            />
            {errors.description && (
              <p className="mt-1 text-sm text-destructive">{errors.description.message}</p>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Recipients */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Users className="h-5 w-5" style={{ color: '#FF7900' }} />
            <CardTitle style={{ color: '#8B1538' }}>Recipients</CardTitle>
          </div>
          <CardDescription>
            Select who should receive this newsletter
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Email Groups */}
          <div>
            <label className="block text-sm font-medium text-neutral-700 mb-2">
              Email Groups
            </label>
            <MultiSelect
              options={emailGroups.map(g => ({ id: g.id, label: g.name }))}
              value={watch('emailGroupIds') || []}
              onChange={(ids) => setValue('emailGroupIds', ids)}
              placeholder="Select email groups"
              isLoading={isLoadingEmailGroups}
              error={!!errors.emailGroupIds}
              errorMessage={errors.emailGroupIds?.message}
            />
          </div>

          {/* Include Newsletter Subscribers */}
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="includeNewsletterSubscribers"
              {...register('includeNewsletterSubscribers')}
              className="h-4 w-4 text-orange-600 focus:ring-orange-500 border-gray-300 rounded"
            />
            <label htmlFor="includeNewsletterSubscribers" className="text-sm font-medium text-neutral-700">
              Include Newsletter Subscribers
            </label>
          </div>

          {/* Event Linkage */}
          <div>
            <label htmlFor="eventId" className="block text-sm font-medium text-neutral-700 mb-2">
              Link to Event (Optional)
            </label>
            <select
              id="eventId"
              className={`w-full px-4 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 ${
                errors.eventId ? 'border-destructive' : 'border-neutral-300'
              }`}
              {...register('eventId')}
              disabled={isLoadingEvents}
            >
              <option value="">No event linkage</option>
              {events.map((event) => (
                <option key={event.id} value={event.id}>
                  {event.title}
                </option>
              ))}
            </select>
            {errors.eventId && (
              <p className="mt-1 text-sm text-destructive">{errors.eventId.message}</p>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Location Targeting (Conditional) */}
      {showLocationTargeting && (
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <MapPin className="h-5 w-5" style={{ color: '#FF7900' }} />
              <CardTitle style={{ color: '#8B1538' }}>Location Targeting</CardTitle>
            </div>
            <CardDescription>
              Target newsletter subscribers by location
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {/* Target All Locations */}
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="targetAllLocations"
                {...register('targetAllLocations')}
                className="h-4 w-4 text-orange-600 focus:ring-orange-500 border-gray-300 rounded"
              />
              <label htmlFor="targetAllLocations" className="text-sm font-medium text-neutral-700">
                Target All Locations
              </label>
            </div>

            {/* Metro Areas (only if not targeting all) */}
            {!targetAllLocations && (
              <div>
                <label className="block text-sm font-medium text-neutral-700 mb-2">
                  Specific Metro Areas
                </label>
                <MultiSelect
                  options={[]} // TODO: Fetch metro areas when API is ready
                  value={watch('metroAreaIds') || []}
                  onChange={(ids) => setValue('metroAreaIds', ids)}
                  placeholder="Select metro areas"
                  error={!!errors.metroAreaIds}
                  errorMessage={errors.metroAreaIds?.message}
                />
                <p className="mt-1 text-xs text-neutral-500">
                  Leave empty to target all locations
                </p>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Validation Error - At least one recipient source */}
      {errors.emailGroupIds && (
        <div className="bg-amber-50 border border-amber-200 text-amber-700 px-4 py-3 rounded-lg">
          {errors.emailGroupIds.message}
        </div>
      )}

      {/* Form Actions */}
      <div className="flex gap-3 justify-end">
        {onCancel && (
          <Button type="button" variant="outline" onClick={onCancel}>
            Cancel
          </Button>
        )}
        <Button
          type="submit"
          disabled={isSubmitting}
          className="bg-[#FF7900] hover:bg-[#E66D00] text-white"
        >
          {isSubmitting ? 'Saving...' : isEditMode ? 'Update Newsletter' : 'Create Newsletter'}
        </Button>
      </div>
    </form>
  );
}
