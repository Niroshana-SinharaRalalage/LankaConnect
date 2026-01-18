'use client';

import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useState, useEffect, useMemo } from 'react';
import { Mail, FileText, Users, MapPin, Calendar, MapPinIcon, UserCheck, ListChecks } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { MultiSelect } from '@/presentation/components/ui/MultiSelect';
import { TreeDropdown, type TreeNode } from '@/presentation/components/ui/TreeDropdown';
import { US_STATES } from '@/domain/constants/metroAreas.constants';
import { RichTextEditor } from '@/presentation/components/ui/RichTextEditor';
import { createNewsletterSchema, type CreateNewsletterFormData } from '@/presentation/lib/validators/newsletter.schemas';
import { useCreateNewsletter, useUpdateNewsletter, useNewsletterById } from '@/presentation/hooks/useNewsletters';
import { useEmailGroups } from '@/presentation/hooks/useEmailGroups';
import { useEvents, useEventById } from '@/presentation/hooks/useEvents';
import { useEventSignUps } from '@/presentation/hooks/useEventSignUps';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useMetroAreas } from '@/presentation/hooks/useMetroAreas';

/**
 * Newsletter Form Component - Phase 6A.74 Part 5A (Restructured)
 *
 * Changes from Part 4:
 * - Event selection moved to TOP of form
 * - Auto-population when event selected (title, metadata)
 * - Rich text editor replaces textarea
 * - Event metadata card displays event info
 * - Enhanced UX with event-first workflow
 *
 * Form Structure:
 * 1. Event Linkage (Optional - TOP)
 * 2. Basic Information (Title + Rich Text Content)
 * 3. Recipients (Email Groups + Subscribers)
 * 4. Location Targeting (Conditional)
 */

export interface NewsletterFormProps {
  newsletterId?: string;
  initialEventId?: string; // Pre-fill event ID for event-specific newsletters
  onSuccess?: (newsletterId?: string) => void;
  onCancel?: () => void;
}

export function NewsletterForm({ newsletterId, initialEventId, onSuccess, onCancel }: NewsletterFormProps) {
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
  const { data: events = [], isLoading: isLoadingEvents } = useEvents({});
  const { metroAreas, metroAreasByState, stateLevelMetros, isLoading: isLoadingMetroAreas } = useMetroAreas();

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    reset,
    control,
    formState: { errors, isSubmitting },
  } = useForm<CreateNewsletterFormData>({
    resolver: zodResolver(createNewsletterSchema),
    defaultValues: {
      title: '',
      description: '',
      emailGroupIds: undefined,
      includeNewsletterSubscribers: true,
      eventId: initialEventId || undefined,
      targetAllLocations: false,
      metroAreaIds: undefined,
    },
  });

  // Watch fields for conditional rendering and auto-population
  const selectedEventId = watch('eventId');
  const targetAllLocations = watch('targetAllLocations');
  const currentTitle = watch('title');

  // Show location targeting only if not event-linked (newsletter subscribers are always included by default)
  const showLocationTargeting = !selectedEventId;

  // Fetch selected event details for metadata display and auto-population
  const { data: selectedEvent } = useEventById(selectedEventId || '', {
    enabled: !!selectedEventId,
  });

  // Fetch sign-up lists for selected event
  const { data: signUpLists = [] } = useEventSignUps(selectedEventId || '', {
    enabled: !!selectedEventId,
  });

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

  // Auto-populate title when event is selected (only if title is empty or was auto-generated)
  useEffect(() => {
    if (selectedEvent && !isEditMode) {
      // Only auto-populate if title is empty or looks like a previous auto-population
      if (!currentTitle || currentTitle.startsWith('Newsletter for ') || currentTitle.startsWith('[UPDATE] on ')) {
        setValue('title', `[UPDATE] on ${selectedEvent.title}`);
      }
    }
  }, [selectedEvent, isEditMode, currentTitle, setValue]);

  // Issue #3 Fix: Auto-populate event links template in description when event is selected
  // User wants this behavior - see screenshot showing links in editor
  useEffect(() => {
    if (!selectedEvent || isEditMode) return;

    const currentDescription = watch('description');

    // Only auto-populate if description is completely empty
    if (currentDescription && currentDescription.trim() !== '' && currentDescription !== '<p></p>') {
      return;
    }

    const frontendUrl = typeof window !== 'undefined' ? window.location.origin : '';

    // Template matching screenshot - event links at bottom with placeholder text at top
    const eventLinksTemplate = `
<p>[Write your newsletter content here.....]</p>

<p style="margin-top: 16px;">Learn more about the event: <a href="${frontendUrl}/events/${selectedEvent.id}">View Event Details</a></p>

<p>Checkout the Sign Up lists: <a href="${frontendUrl}/events/${selectedEvent.id}#sign-ups">View Event Sign-up Lists</a></p>
    `.trim();

    setValue('description', eventLinksTemplate);
  }, [selectedEvent, isEditMode, watch, setValue]);

  // Build location tree for TreeDropdown (Issue #8)
  const selectedMetroIds = watch('metroAreaIds') || [];
  const locationTree = useMemo((): TreeNode[] => {
    const stateNodes = US_STATES.map(state => {
      const stateMetros = metroAreasByState.get(state.code) || [];
      const stateLevelMetro = stateLevelMetros.find(m => m.state === state.code);
      const stateId = stateLevelMetro?.id || state.code;

      return {
        id: stateId,
        label: `All ${state.name}`,
        checked: selectedMetroIds.includes(stateId),
        children: stateMetros
          .filter(m => !m.isStateLevelArea) // Only city-level metros as children
          .map(metro => ({
            id: metro.id,
            label: `${metro.name}, ${metro.state}`,
            checked: selectedMetroIds.includes(metro.id),
          })),
      };
    });

    // Only include states that have at least one metro area
    const populatedStateNodes = stateNodes.filter(node =>
      node.children && node.children.length > 0
    );

    return populatedStateNodes;
  }, [metroAreasByState, stateLevelMetros, selectedMetroIds]);

  // Handle location tree selection change
  const handleLocationTreeChange = (selectedIds: string[]) => {
    console.log('[NewsletterForm] Location selection changed:', selectedIds);
    setValue('metroAreaIds', selectedIds.length > 0 ? selectedIds : undefined);
  };

  // Issue #5: Log metro areas loaded for debugging
  useEffect(() => {
    if (metroAreas.length > 0) {
      console.log('[NewsletterForm] Metro areas loaded:', metroAreas.length);
      console.log('[NewsletterForm] Sample metro IDs:', metroAreas.slice(0, 3).map(m => ({ id: m.id, name: m.name, state: m.state })));
    }
  }, [metroAreas]);

  const onSubmit = handleSubmit(async (data) => {
    try {
      setSubmitError(null);

      if (isEditMode && newsletterId) {
        await updateMutation.mutateAsync({ id: newsletterId, ...data });
        onSuccess?.(newsletterId);
      } else {
        const newsletterId = await createMutation.mutateAsync(data);
        onSuccess?.(newsletterId);
      }
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
      {/* Form Validation Errors Summary */}
      {Object.keys(errors).length > 0 && (
        <div className="bg-amber-50 border border-amber-200 text-amber-800 px-4 py-3 rounded-lg">
          <p className="font-semibold mb-2 flex items-center gap-2">
            <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
            </svg>
            Please fix the following errors:
          </p>
          <ul className="list-disc list-inside text-sm space-y-1">
            {errors.title && <li>Title: {errors.title.message}</li>}
            {errors.description && <li>Description: {errors.description.message}</li>}
            {errors.emailGroupIds && <li>Recipients: {errors.emailGroupIds.message}</li>}
            {errors.eventId && <li>Event: {errors.eventId.message}</li>}
            {errors.metroAreaIds && <li>Location: {errors.metroAreaIds.message}</li>}
            {errors.targetAllLocations && <li>Location Targeting: {errors.targetAllLocations.message}</li>}
          </ul>
        </div>
      )}

      {/* Submit Error (API/Network errors) */}
      {submitError && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg">
          <p className="font-semibold mb-1 flex items-center gap-2">
            <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
            </svg>
            Error
          </p>
          <p className="text-sm">{submitError}</p>
        </div>
      )}

      {/* 1. EVENT LINKAGE - MOVED TO TOP */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Calendar className="h-5 w-5" style={{ color: '#FF7900' }} />
            <CardTitle style={{ color: '#8B1538' }}>Event Linkage</CardTitle>
          </div>
          <CardDescription>
            {isEditMode
              ? 'Optionally link this newsletter to an event'
              : 'Start by linking to an event (recommended) or create a standalone newsletter'
            }
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Event Selection Dropdown */}
          <div>
            <label htmlFor="eventId" className="block text-sm font-medium text-neutral-700 mb-2">
              Select Event (Optional)
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
            <p className="mt-1 text-xs text-neutral-500">
              {isEditMode
                ? 'Change the linked event or remove the linkage'
                : 'Linking to an event will auto-populate the title and add event details to the email'
              }
            </p>
          </div>

          {/* Event Metadata Card - Show when event is selected */}
          {selectedEvent && (
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 space-y-3">
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <p className="font-semibold text-neutral-900 mb-2">{selectedEvent.title}</p>

                  <div className="grid grid-cols-2 gap-3 text-sm">
                    {/* Location */}
                    {(selectedEvent.city || selectedEvent.state) && (
                      <div className="flex items-center gap-2 text-neutral-700">
                        <MapPinIcon className="h-4 w-4 text-neutral-500" />
                        <span>
                          {[selectedEvent.city, selectedEvent.state].filter(Boolean).join(', ')}
                        </span>
                      </div>
                    )}

                    {/* Date */}
                    <div className="flex items-center gap-2 text-neutral-700">
                      <Calendar className="h-4 w-4 text-neutral-500" />
                      <span>{new Date(selectedEvent.startDate).toLocaleDateString()}</span>
                    </div>

                    {/* Attendees */}
                    <div className="flex items-center gap-2 text-neutral-700">
                      <UserCheck className="h-4 w-4 text-neutral-500" />
                      <span>{selectedEvent.currentRegistrations || 0} registered</span>
                    </div>

                    {/* Sign-up Lists */}
                    <div className="flex items-center gap-2 text-neutral-700">
                      <ListChecks className="h-4 w-4 text-neutral-500" />
                      <span>{signUpLists.length} sign-up {signUpLists.length === 1 ? 'list' : 'lists'}</span>
                    </div>
                  </div>
                </div>
              </div>

              <div className="pt-2 border-t border-blue-300">
                <p className="text-xs text-blue-800">
                  <strong>Email will include:</strong> Event details, registration button, and sign-up list links
                </p>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* 2. BASIC INFORMATION */}
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
            {selectedEvent && (
              <p className="mt-1 text-xs text-blue-600">
                💡 Title auto-populated from event. Feel free to customize it!
              </p>
            )}
          </div>

          {/* Rich Text Content - REPLACED TEXTAREA */}
          <div>
            <label htmlFor="description" className="block text-sm font-medium text-neutral-700 mb-2">
              Newsletter Content *
            </label>
            <Controller
              name="description"
              control={control}
              render={({ field }) => (
                <RichTextEditor
                  content={field.value}
                  onChange={field.onChange}
                  placeholder="Write your newsletter content here....."
                  error={!!errors.description}
                  errorMessage={errors.description?.message}
                  minHeight={300}
                  maxLength={50000}
                />
              )}
            />
          </div>
        </CardContent>
      </Card>

      {/* 3. RECIPIENTS */}
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
            {selectedEvent && (
              <p className="mt-1 text-xs text-neutral-500">
                Note: If event is linked, the email will also go to event-specific email groups automatically.
              </p>
            )}
          </div>

          {/* Newsletter subscribers are always included by default */}
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-3">
            <p className="text-sm text-blue-800">
              <strong>Note:</strong> Newsletter subscribers are automatically included as recipients.
            </p>
          </div>
        </CardContent>
      </Card>

      {/* 4. LOCATION TARGETING (Conditional - Only if not event-linked) */}
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

            {/* Metro Areas (only if not targeting all) - Issue #8: TreeDropdown */}
            {!targetAllLocations && (
              <div>
                <label className="block text-sm font-medium text-neutral-700 mb-2">
                  Specific Metro Areas
                </label>
                {isLoadingMetroAreas ? (
                  <div className="text-sm text-gray-500 py-2">Loading locations...</div>
                ) : (
                  <TreeDropdown
                    nodes={locationTree}
                    selectedIds={selectedMetroIds}
                    onSelectionChange={handleLocationTreeChange}
                    placeholder="Select metro areas by state"
                    disabled={isLoadingMetroAreas}
                  />
                )}
                {errors.metroAreaIds && (
                  <p className="mt-1 text-sm text-red-600">{errors.metroAreaIds.message}</p>
                )}
                <p className="mt-1 text-xs text-neutral-500">
                  Expand states to select specific metro areas
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
