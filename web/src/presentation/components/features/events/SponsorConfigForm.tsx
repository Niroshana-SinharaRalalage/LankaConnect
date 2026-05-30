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
   * Phase 6A.156 — whether to expose the organizer-defined sponsorship-package
   * grid (Gold/Silver/Bronze tiers) on the public event page. Optional for
   * backward compatibility with callers that pre-date 6A.156 — when undefined,
   * the toggle is hidden entirely.
   */
  enablePackages?: boolean;
  onEnablePackagesChange?: (enabled: boolean) => void;
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
  enablePackages,
  onEnablePackagesChange,
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

            {/* Phase 6A.156 — Enable sponsorship packages */}
            {onEnablePackagesChange && (
              <div className="flex items-start space-x-3">
                <input
                  type="checkbox"
                  id="enablePackages"
                  checked={enablePackages ?? false}
                  onChange={(e) => onEnablePackagesChange(e.target.checked)}
                  className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <div>
                  <label htmlFor="enablePackages" className="text-sm font-medium text-gray-700">
                    Enable sponsorship packages (Gold / Silver / Bronze tiers)
                  </label>
                  <p className="text-xs text-gray-500 mt-0.5">
                    Define tiered sponsorships with perks and bundled tickets. Manage
                    packages in the &ldquo;Packages&rdquo; tab under Attendees &amp; Finance.
                    Buyer-facing purchase lands in a follow-up phase.
                  </p>
                </div>
              </div>
            )}
        </div>
      )}
    </div>
  );
}
