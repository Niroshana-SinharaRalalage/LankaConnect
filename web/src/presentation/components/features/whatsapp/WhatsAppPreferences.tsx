'use client';

import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Settings, Bell, Clock, Globe, Loader2 } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import {
  useWhatsAppPreferences,
  useUpdateWhatsAppPreferences,
} from '@/presentation/hooks/useWhatsApp';
import {
  updatePreferencesSchema,
  type UpdatePreferencesFormData,
} from '@/presentation/lib/validators/whatsapp.schemas';

interface NotificationToggle {
  key: keyof Pick<
    UpdatePreferencesFormData,
    'eventRegistration' | 'eventReminder' | 'eventCancellation' | 'eventUpdate' |
    'signupCommitment' | 'refund' | 'newsletter' | 'newEvent' | 'payment'
  >;
  label: string;
  description: string;
}

const NOTIFICATION_TOGGLES: NotificationToggle[] = [
  { key: 'eventRegistration', label: 'Event Registration', description: 'Confirmation when you register for an event' },
  { key: 'eventReminder', label: 'Event Reminders', description: 'Reminders before events you\'re attending' },
  { key: 'eventCancellation', label: 'Event Cancellation', description: 'When an event you registered for is cancelled' },
  { key: 'eventUpdate', label: 'Event Updates', description: 'When event details change (time, location, etc.)' },
  { key: 'signupCommitment', label: 'Sign-up Commitments', description: 'Updates about your sign-up list commitments' },
  { key: 'refund', label: 'Refunds', description: 'When a refund is initiated or completed' },
  { key: 'newsletter', label: 'Newsletters', description: 'Community newsletters and announcements' },
  { key: 'newEvent', label: 'New Events', description: 'When new events are published in your area' },
  { key: 'payment', label: 'Payments', description: 'Payment confirmations and receipts' },
];

/**
 * WhatsAppPreferences Component
 * Phase 7A.4: Notification toggle preferences for WhatsApp
 *
 * Shows 9 notification type toggles + quiet hours settings
 * Only visible when WhatsApp is fully verified
 */
export function WhatsAppPreferences() {
  const { data: preferences, isLoading } = useWhatsAppPreferences();
  const updateMutation = useUpdateWhatsAppPreferences();

  const form = useForm<UpdatePreferencesFormData>({
    resolver: zodResolver(updatePreferencesSchema),
    defaultValues: {
      eventRegistration: true,
      eventReminder: true,
      eventCancellation: true,
      eventUpdate: true,
      signupCommitment: true,
      refund: true,
      newsletter: true,
      newEvent: true,
      payment: true,
      preferredLanguage: 'en',
      quietHoursStart: null,
      quietHoursEnd: null,
      respectCulturalTiming: true,
    },
  });

  // Sync form with server preferences when loaded
  useEffect(() => {
    if (preferences && preferences.isFullyVerified) {
      form.reset({
        eventRegistration: preferences.notifyEventRegistration,
        eventReminder: preferences.notifyEventReminder,
        eventCancellation: preferences.notifyEventCancellation,
        eventUpdate: preferences.notifyEventUpdate,
        signupCommitment: preferences.notifySignupCommitment,
        refund: preferences.notifyRefund,
        newsletter: preferences.notifyNewsletter,
        newEvent: preferences.notifyNewEvent,
        payment: preferences.notifyPayment,
        preferredLanguage: preferences.preferredLanguage || 'en',
        quietHoursStart: preferences.quietHoursStart,
        quietHoursEnd: preferences.quietHoursEnd,
        respectCulturalTiming: preferences.respectCulturalTiming,
      });
    }
  }, [preferences, form]);

  // Don't render if not fully verified
  if (isLoading || !preferences?.isFullyVerified) {
    return null;
  }

  const handleSave = async (data: UpdatePreferencesFormData) => {
    await updateMutation.mutateAsync({
      eventRegistration: data.eventRegistration,
      eventReminder: data.eventReminder,
      eventCancellation: data.eventCancellation,
      eventUpdate: data.eventUpdate,
      signupCommitment: data.signupCommitment,
      refund: data.refund,
      newsletter: data.newsletter,
      newEvent: data.newEvent,
      payment: data.payment,
      preferredLanguage: data.preferredLanguage,
      quietHoursStart: data.quietHoursStart,
      quietHoursEnd: data.quietHoursEnd,
      respectCulturalTiming: data.respectCulturalTiming,
    });
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-3">
          <div className="p-2 rounded-lg" style={{ backgroundColor: '#25D36610' }}>
            <Settings className="h-5 w-5" style={{ color: '#25D366' }} />
          </div>
          <div>
            <CardTitle className="text-lg">WhatsApp Notification Preferences</CardTitle>
            <CardDescription>
              Choose which notifications you want to receive via WhatsApp
            </CardDescription>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <form onSubmit={form.handleSubmit(handleSave)} className="space-y-6">
          {/* Notification Toggles */}
          <div className="space-y-1">
            <div className="flex items-center gap-2 mb-3">
              <Bell className="h-4 w-4 text-gray-500" />
              <h3 className="text-sm font-semibold text-gray-700">Notification Types</h3>
            </div>
            <div className="space-y-2">
              {NOTIFICATION_TOGGLES.map((toggle) => (
                <label
                  key={toggle.key}
                  className="flex items-center justify-between p-3 rounded-lg hover:bg-gray-50 cursor-pointer"
                >
                  <div className="flex-1 mr-4">
                    <p className="text-sm font-medium text-gray-900">{toggle.label}</p>
                    <p className="text-xs text-gray-500">{toggle.description}</p>
                  </div>
                  <input
                    type="checkbox"
                    {...form.register(toggle.key)}
                    className="h-4 w-4 rounded border-gray-300 focus:ring-2"
                    style={{ accentColor: '#25D366' }}
                  />
                </label>
              ))}
            </div>
          </div>

          {/* Quiet Hours */}
          <div className="space-y-3 border-t pt-4">
            <div className="flex items-center gap-2">
              <Clock className="h-4 w-4 text-gray-500" />
              <h3 className="text-sm font-semibold text-gray-700">Quiet Hours</h3>
            </div>
            <p className="text-xs text-gray-500">
              Messages during quiet hours will be queued and sent afterwards
            </p>
            <div className="flex items-center gap-4">
              <div className="flex-1">
                <label htmlFor="wa-quiet-start" className="text-xs text-gray-600">From</label>
                <input
                  id="wa-quiet-start"
                  type="time"
                  {...form.register('quietHoursStart')}
                  className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-offset-2"
                />
              </div>
              <div className="flex-1">
                <label htmlFor="wa-quiet-end" className="text-xs text-gray-600">To</label>
                <input
                  id="wa-quiet-end"
                  type="time"
                  {...form.register('quietHoursEnd')}
                  className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-offset-2"
                />
              </div>
            </div>
          </div>

          {/* Cultural Timing */}
          <div className="border-t pt-4">
            <label className="flex items-center justify-between p-3 rounded-lg hover:bg-gray-50 cursor-pointer">
              <div className="flex items-center gap-3 flex-1 mr-4">
                <Globe className="h-4 w-4 text-gray-500" />
                <div>
                  <p className="text-sm font-medium text-gray-900">Respect Cultural Timing</p>
                  <p className="text-xs text-gray-500">
                    Delay messages during Sri Lankan holidays and cultural observances
                  </p>
                </div>
              </div>
              <input
                type="checkbox"
                {...form.register('respectCulturalTiming')}
                className="h-4 w-4 rounded border-gray-300 focus:ring-2"
                style={{ accentColor: '#25D366' }}
              />
            </label>
          </div>

          {/* Save Button */}
          <Button
            type="submit"
            disabled={updateMutation.isPending || !form.formState.isDirty}
            className="w-full"
            style={{ backgroundColor: '#25D366', color: 'white' }}
          >
            {updateMutation.isPending ? (
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
            ) : null}
            Save Preferences
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
