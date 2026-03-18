'use client';

import { PackagePlus } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';

interface AddOnConfigFormProps {
  /** Whether add-ons are enabled */
  isEnabled: boolean;
  onEnabledChange: (enabled: boolean) => void;
  /** Whether add-ons are available during registration */
  availableDuringRegistration: boolean;
  onAvailableDuringRegistrationChange: (available: boolean) => void;
  /** Whether add-ons are available for standalone purchase */
  availableStandalone: boolean;
  onAvailableStandaloneChange: (available: boolean) => void;
  /** Custom add-on message (optional) */
  addOnMessage: string;
  onAddOnMessageChange: (message: string) => void;
}

/**
 * Add-On configuration section for event create/edit forms.
 * Follows the DonationConfigForm pattern.
 */
export function AddOnConfigForm({
  isEnabled,
  onEnabledChange,
  availableDuringRegistration,
  onAvailableDuringRegistrationChange,
  availableStandalone,
  onAvailableStandaloneChange,
  addOnMessage,
  onAddOnMessageChange,
}: AddOnConfigFormProps) {
  const showAvailabilityWarning = isEnabled && !availableDuringRegistration && !availableStandalone;

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <PackagePlus className="h-5 w-5" style={{ color: '#FF7900' }} />
          <CardTitle style={{ color: '#8B1538' }}>Add-Ons (Optional)</CardTitle>
        </div>
        <CardDescription>
          Offer additional items or services that attendees can purchase alongside their registration
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Enable Toggle */}
        <div className="flex items-start space-x-3">
          <input
            type="checkbox"
            id="enableAddOns"
            checked={isEnabled}
            onChange={(e) => onEnabledChange(e.target.checked)}
            className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
          />
          <label htmlFor="enableAddOns" className="text-sm font-medium text-gray-700">
            Enable add-ons for this event
          </label>
        </div>

        {isEnabled && (
          <div className="ml-7 space-y-4 p-4 border border-gray-200 rounded-lg bg-gray-50">
            {/* Availability Options */}
            <div className="space-y-3">
              <label className="block text-sm font-medium text-gray-700">
                Add-On Availability
              </label>

              <div className="flex items-start space-x-3">
                <input
                  type="checkbox"
                  id="addOnDuringRegistration"
                  checked={availableDuringRegistration}
                  onChange={(e) => onAvailableDuringRegistrationChange(e.target.checked)}
                  className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <div>
                  <label htmlFor="addOnDuringRegistration" className="text-sm font-medium text-gray-700">
                    Offer add-ons during event registration
                  </label>
                  <p className="text-xs text-gray-500 mt-0.5">
                    Attendees can select add-ons while registering for the event
                  </p>
                </div>
              </div>

              <div className="flex items-start space-x-3">
                <input
                  type="checkbox"
                  id="addOnStandalone"
                  checked={availableStandalone}
                  onChange={(e) => onAvailableStandaloneChange(e.target.checked)}
                  className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <div>
                  <label htmlFor="addOnStandalone" className="text-sm font-medium text-gray-700">
                    Allow standalone add-on purchases from event page
                  </label>
                  <p className="text-xs text-gray-500 mt-0.5">
                    Anyone can purchase add-ons directly from the event details page
                  </p>
                </div>
              </div>

              {showAvailabilityWarning && (
                <p className="text-sm text-red-600 font-medium">
                  Must be available in at least one context (registration or standalone)
                </p>
              )}
            </div>

            {/* Add-On Message */}
            <div className="space-y-2">
              <label htmlFor="addOnMessage" className="block text-sm font-medium text-gray-700">
                Add-On Message (optional)
              </label>
              <textarea
                id="addOnMessage"
                value={addOnMessage}
                onChange={(e) => onAddOnMessageChange(e.target.value)}
                placeholder="Enhance your experience with these optional add-ons..."
                maxLength={500}
                rows={2}
                className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              <p className="text-xs text-gray-500">{addOnMessage.length}/500 characters</p>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
