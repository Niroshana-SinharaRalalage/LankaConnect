'use client';

import { Input } from '@/presentation/components/ui/Input';

interface SponsorConfigFormProps {
  /** Whether sponsorships are enabled */
  isEnabled: boolean;
  onEnabledChange: (enabled: boolean) => void;
  /** Whether to accept monetary sponsorships */
  acceptMoneySponsors: boolean;
  onAcceptMoneySponsorsChange: (accept: boolean) => void;
  /** Whether to accept item-based sponsorships */
  acceptItemSponsors: boolean;
  onAcceptItemSponsorsChange: (accept: boolean) => void;
  /** Minimum monetary sponsorship amount (optional) */
  minSponsorAmount: number | null;
  onMinSponsorAmountChange: (amount: number | null) => void;
  /** Custom sponsor message (optional) */
  sponsorMessage: string;
  onSponsorMessageChange: (message: string) => void;
  /** Whether to show sponsor list publicly */
  showSponsorList: boolean;
  onShowSponsorListChange: (show: boolean) => void;
  /**
   * Phase 6A.145 — opt-in threshold for per-sponsor image uploads.
   * Null = feature OFF. When set, sponsors whose money amount (or item EstimatedValue)
   * reaches this threshold can attach a logo/image displayed on the event details page.
   */
  minAmountForSponsorImage: number | null;
  onMinAmountForSponsorImageChange: (amount: number | null) => void;
}

/**
 * Sponsor configuration section for event create/edit forms.
 * Phase 6A.145: replaced the banner-on-config concept (rolled back) with a
 * threshold field gating per-sponsor image uploads.
 */
export function SponsorConfigForm({
  isEnabled,
  onEnabledChange,
  acceptMoneySponsors,
  onAcceptMoneySponsorsChange,
  acceptItemSponsors,
  onAcceptItemSponsorsChange,
  minSponsorAmount,
  onMinSponsorAmountChange,
  sponsorMessage,
  onSponsorMessageChange,
  showSponsorList,
  onShowSponsorListChange,
  minAmountForSponsorImage,
  onMinAmountForSponsorImageChange,
}: SponsorConfigFormProps) {
  const showTypeWarning = isEnabled && !acceptMoneySponsors && !acceptItemSponsors;

  return (
    <div className="space-y-4">
        {/* Enable Toggle */}
        <div className="flex items-start space-x-3">
          <input
            type="checkbox"
            id="enableSponsors"
            checked={isEnabled}
            onChange={(e) => onEnabledChange(e.target.checked)}
            className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
          />
          <label htmlFor="enableSponsors" className="text-sm font-medium text-gray-700">
            Enable sponsorships for this event
          </label>
        </div>

        {isEnabled && (
          <div className="ml-7 space-y-4 p-4 border border-gray-200 rounded-lg bg-gray-50">
            {/* Sponsor Types */}
            <div className="space-y-3">
              <label className="block text-sm font-medium text-gray-700">
                Accepted Sponsor Types
              </label>

              <div className="flex items-start space-x-3">
                <input
                  type="checkbox"
                  id="acceptMoneySponsors"
                  checked={acceptMoneySponsors}
                  onChange={(e) => onAcceptMoneySponsorsChange(e.target.checked)}
                  className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <div>
                  <label htmlFor="acceptMoneySponsors" className="text-sm font-medium text-gray-700">
                    Accept monetary sponsorships (via Stripe)
                  </label>
                  <p className="text-xs text-gray-500 mt-0.5">
                    Sponsors can contribute money through secure online payment
                  </p>
                </div>
              </div>

              <div className="flex items-start space-x-3">
                <input
                  type="checkbox"
                  id="acceptItemSponsors"
                  checked={acceptItemSponsors}
                  onChange={(e) => onAcceptItemSponsorsChange(e.target.checked)}
                  className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <div>
                  <label htmlFor="acceptItemSponsors" className="text-sm font-medium text-gray-700">
                    Accept item-based sponsorships
                  </label>
                  <p className="text-xs text-gray-500 mt-0.5">
                    Sponsors can commit to providing items, services, or in-kind contributions
                  </p>
                </div>
              </div>

              {showTypeWarning && (
                <p className="text-sm text-red-600 font-medium">
                  Must accept at least one sponsor type (monetary or item-based)
                </p>
              )}
            </div>

            {/* Min Sponsor Amount (only when money sponsors enabled) */}
            {acceptMoneySponsors && (
              <div className="space-y-2">
                <label htmlFor="minSponsorAmount" className="block text-sm font-medium text-gray-700">
                  Minimum Sponsorship Amount (optional)
                </label>
                <div className="relative max-w-xs">
                  <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500 text-sm">$</span>
                  <Input
                    id="minSponsorAmount"
                    type="number"
                    min="1.00"
                    step="0.01"
                    value={minSponsorAmount ?? ''}
                    onChange={(e) => {
                      const val = e.target.value;
                      onMinSponsorAmountChange(val ? parseFloat(val) : null);
                    }}
                    placeholder="e.g. 50.00"
                    className="pl-7 text-sm"
                  />
                </div>
              </div>
            )}

            {/* Phase 6A.145 — opt-in image-upload threshold. Null = feature OFF.
                When set, sponsors meeting this threshold get to upload a logo/image
                shown publicly on the event details page. */}
            <div className="space-y-2">
              <label htmlFor="minAmountForSponsorImage" className="block text-sm font-medium text-gray-700">
                Image upload threshold (optional)
              </label>
              <div className="relative max-w-xs">
                <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500 text-sm">$</span>
                <Input
                  id="minAmountForSponsorImage"
                  type="number"
                  min="1.00"
                  step="0.01"
                  value={minAmountForSponsorImage ?? ''}
                  onChange={(e) => {
                    const val = e.target.value;
                    onMinAmountForSponsorImageChange(val ? parseFloat(val) : null);
                  }}
                  placeholder="e.g. 100.00"
                  className="pl-7 text-sm"
                />
              </div>
              <p className="text-xs text-gray-500">
                Sponsors who contribute this amount or more (money or item value) can attach a
                logo/image to their sponsorship. Leave blank to disable sponsor images entirely.
              </p>
            </div>

            {/* Sponsor Message */}
            <div className="space-y-2">
              <label htmlFor="sponsorMessage" className="block text-sm font-medium text-gray-700">
                Sponsor Message (optional)
              </label>
              <textarea
                id="sponsorMessage"
                value={sponsorMessage}
                onChange={(e) => onSponsorMessageChange(e.target.value)}
                placeholder="Support our event as a sponsor! Your generous contribution helps make this possible..."
                maxLength={500}
                rows={2}
                className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              <p className="text-xs text-gray-500">{sponsorMessage.length}/500 characters</p>
            </div>

            {/* Show Sponsor List */}
            <div className="flex items-start space-x-3">
              <input
                type="checkbox"
                id="showSponsorList"
                checked={showSponsorList}
                onChange={(e) => onShowSponsorListChange(e.target.checked)}
                className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <div>
                <label htmlFor="showSponsorList" className="text-sm font-medium text-gray-700">
                  Display sponsor list publicly on event page
                </label>
                <p className="text-xs text-gray-500 mt-0.5">
                  Show sponsors and their contributions to all event visitors
                </p>
              </div>
            </div>
        </div>
      )}
    </div>
  );
}
