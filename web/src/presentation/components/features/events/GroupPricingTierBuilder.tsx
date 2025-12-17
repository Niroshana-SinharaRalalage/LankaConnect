'use client';

import { useState } from 'react';
import { Plus, X, AlertCircle } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { Currency } from '@/infrastructure/api/types/events.types';
import type { GroupPricingTierFormData } from '@/presentation/lib/validators/event.schemas';

/**
 * Group Pricing Tier Builder Component
 * Phase 6D: Tiered Group Pricing
 *
 * Allows event organizers to create quantity-based pricing tiers with:
 * - Min/max attendee ranges
 * - Price per person
 * - Currency selection
 * - Validation for gaps/overlaps
 * - Visual tier range display
 */

interface GroupPricingTierBuilderProps {
  tiers: GroupPricingTierFormData[];
  onChange: (tiers: GroupPricingTierFormData[]) => void;
  defaultCurrency: Currency;
  errors?: string;
}

export function GroupPricingTierBuilder({
  tiers,
  onChange,
  defaultCurrency,
  errors,
}: GroupPricingTierBuilderProps) {
  const [showAddForm, setShowAddForm] = useState(false);
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const [newTier, setNewTier] = useState<Partial<GroupPricingTierFormData>>({
    minAttendees: tiers.length === 0 ? 1 : undefined,
    maxAttendees: undefined,
    pricePerPerson: undefined,
    currency: defaultCurrency,
  });
  const [tierErrors, setTierErrors] = useState<string | null>(null);

  // Calculate suggested minAttendees for next tier
  const suggestedMinAttendees = (): number => {
    if (tiers.length === 0) return 1;
    const sortedTiers = [...tiers].sort((a, b) => a.minAttendees - b.minAttendees);
    const lastTier = sortedTiers[sortedTiers.length - 1];
    return lastTier.maxAttendees ? lastTier.maxAttendees + 1 : lastTier.minAttendees + 1;
  };

  // Format tier range for display
  const formatTierRange = (tier: GroupPricingTierFormData): string => {
    if (!tier.maxAttendees) return `${tier.minAttendees}+`;
    if (tier.minAttendees === tier.maxAttendees) return `${tier.minAttendees}`;
    return `${tier.minAttendees}-${tier.maxAttendees}`;
  };

  // Validate tier before adding
  const validateNewTier = (): boolean => {
    setTierErrors(null);

    if (!newTier.minAttendees || newTier.minAttendees < 1) {
      setTierErrors('Minimum attendees must be at least 1');
      return false;
    }

    if (newTier.maxAttendees !== undefined && newTier.maxAttendees !== null) {
      if (newTier.maxAttendees < newTier.minAttendees) {
        setTierErrors('Maximum attendees must be greater than or equal to minimum');
        return false;
      }
    }

    if (!newTier.pricePerPerson || newTier.pricePerPerson < 0) {
      setTierErrors('Price per person must be greater than or equal to 0');
      return false;
    }

    if (newTier.pricePerPerson > 10000) {
      setTierErrors('Price per person cannot exceed $10,000');
      return false;
    }

    // Check for overlaps with existing tiers
    const sortedTiers = [...tiers].sort((a, b) => a.minAttendees - b.minAttendees);
    for (const existingTier of sortedTiers) {
      // Check if new tier overlaps with existing tier
      const newMin = newTier.minAttendees;
      const newMax = newTier.maxAttendees ?? Number.MAX_SAFE_INTEGER;
      const existingMin = existingTier.minAttendees;
      const existingMax = existingTier.maxAttendees ?? Number.MAX_SAFE_INTEGER;

      if (newMin <= existingMax && newMax >= existingMin) {
        setTierErrors(`This tier overlaps with existing tier ${formatTierRange(existingTier)}`);
        return false;
      }
    }

    return true;
  };

  // Add new tier
  const handleAddTier = () => {
    if (!validateNewTier()) return;

    const tier: GroupPricingTierFormData = {
      minAttendees: newTier.minAttendees!,
      maxAttendees: newTier.maxAttendees ?? null,
      pricePerPerson: newTier.pricePerPerson!,
      currency: newTier.currency || defaultCurrency,
    };

    onChange([...tiers, tier]);

    // Reset form
    setNewTier({
      minAttendees: suggestedMinAttendees(),
      maxAttendees: undefined,
      pricePerPerson: undefined,
      currency: defaultCurrency,
    });
    setShowAddForm(false);
    setTierErrors(null);
  };

  // Remove tier
  const handleRemoveTier = (index: number) => {
    const updatedTiers = tiers.filter((_, i) => i !== index);
    onChange(updatedTiers);
  };

  // Open add form with suggested values
  const handleOpenAddForm = () => {
    setNewTier({
      minAttendees: suggestedMinAttendees(),
      maxAttendees: undefined,
      pricePerPerson: undefined,
      currency: defaultCurrency,
    });
    setShowAddForm(true);
    setTierErrors(null);
  };

  // Sort tiers by minAttendees for display
  const sortedTiers = [...tiers].sort((a, b) => a.minAttendees - b.minAttendees);

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h4 className="text-sm font-semibold text-neutral-900">Pricing Tiers</h4>
          <p className="text-xs text-neutral-600 mt-1">
            Define price per person based on group size
          </p>
        </div>
        {!showAddForm && (
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleOpenAddForm}
            className="flex items-center gap-1"
          >
            <Plus className="h-4 w-4" />
            Add Tier
          </Button>
        )}
      </div>

      {/* Existing Tiers List */}
      {sortedTiers.length > 0 && (
        <div className="space-y-2">
          {sortedTiers.map((tier, index) => (
            <div
              key={`${tier.minAttendees}-${tier.maxAttendees}`}
              className="flex items-center justify-between p-3 bg-white border border-neutral-200 rounded-lg hover:border-orange-300 transition-colors"
            >
              <div className="flex items-center gap-4 flex-1">
                <div className="flex items-center gap-2">
                  <span className="text-sm font-medium text-neutral-700 bg-neutral-100 px-3 py-1 rounded">
                    {formatTierRange(tier)}
                  </span>
                  <span className="text-xs text-neutral-500">attendees</span>
                </div>
                <div className="flex items-center gap-2">
                  <span className="text-base font-semibold text-orange-600">
                    {tier.currency === Currency.USD ? '$' : 'Rs'} {tier.pricePerPerson.toFixed(2)}
                  </span>
                  <span className="text-xs text-neutral-500">per person</span>
                </div>
              </div>
              <button
                type="button"
                onClick={() => handleRemoveTier(tiers.indexOf(tier))}
                className="p-1 hover:bg-red-50 rounded transition-colors"
                title="Remove tier"
              >
                <X className="h-4 w-4 text-red-600" />
              </button>
            </div>
          ))}
        </div>
      )}

      {/* Add Tier Form */}
      {showAddForm && (
        <div className="p-4 bg-orange-50 border-2 border-orange-200 rounded-lg space-y-4">
          <div className="flex items-center justify-between mb-2">
            <h5 className="text-sm font-semibold text-neutral-900">New Pricing Tier</h5>
            <button
              type="button"
              onClick={() => {
                setShowAddForm(false);
                setTierErrors(null);
              }}
              className="p-1 hover:bg-orange-100 rounded transition-colors"
            >
              <X className="h-4 w-4 text-neutral-600" />
            </button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {/* Min Attendees */}
            <div>
              <label className="block text-sm font-medium text-neutral-700 mb-2">
                Min Attendees *
              </label>
              <Input
                type="number"
                min="1"
                max="10000"
                placeholder="1"
                value={newTier.minAttendees || ''}
                onChange={(e) =>
                  setNewTier({ ...newTier, minAttendees: parseInt(e.target.value) || undefined })
                }
              />
            </div>

            {/* Max Attendees */}
            <div>
              <label className="block text-sm font-medium text-neutral-700 mb-2">
                Max Attendees
              </label>
              <Input
                type="number"
                min={newTier.minAttendees || 1}
                max="10000"
                placeholder="Leave empty for unlimited"
                value={newTier.maxAttendees || ''}
                onChange={(e) =>
                  setNewTier({
                    ...newTier,
                    maxAttendees: e.target.value ? parseInt(e.target.value) : undefined,
                  })
                }
              />
              <p className="mt-1 text-xs text-neutral-500">Leave empty for unlimited (e.g., "6+")</p>
            </div>

            {/* Price Per Person */}
            <div>
              <label className="block text-sm font-medium text-neutral-700 mb-2">
                Price Per Person *
              </label>
              <div className="flex items-center gap-2">
                <select
                  className="px-2 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500"
                  value={newTier.currency || defaultCurrency}
                  onChange={(e) =>
                    setNewTier({ ...newTier, currency: parseInt(e.target.value) as Currency })
                  }
                >
                  <option value={Currency.USD}>USD ($)</option>
                  <option value={Currency.LKR}>LKR (Rs)</option>
                </select>
                <Input
                  type="number"
                  min="0"
                  max="10000"
                  step="1"
                  placeholder="25"
                  value={newTier.pricePerPerson || ''}
                  onChange={(e) =>
                    setNewTier({ ...newTier, pricePerPerson: parseFloat(e.target.value) || undefined })
                  }
                />
              </div>
              {/* Session 33: Commission info message for group pricing - standardized format */}
              {(newTier.pricePerPerson ?? 0) > 0 && (
                <div className="mt-2 p-2 bg-gray-50 border border-gray-200 rounded text-xs text-gray-600">
                  <p>5% (Stripe + LankaConnect commission) applies</p>
                  <p className="font-medium text-green-700">
                    You'll receive: ${((newTier.pricePerPerson ?? 0) * 0.95).toFixed(2)} per person
                  </p>
                </div>
              )}
            </div>
          </div>

          {/* Tier-specific errors */}
          {tierErrors && (
            <div className="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-lg">
              <AlertCircle className="w-5 h-5 text-red-600 mt-0.5 flex-shrink-0" />
              <p className="text-sm text-red-700">{tierErrors}</p>
            </div>
          )}

          {/* Add Button */}
          <div className="flex justify-end gap-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setShowAddForm(false);
                setTierErrors(null);
              }}
            >
              Cancel
            </Button>
            <Button type="button" onClick={handleAddTier}>
              Add Tier
            </Button>
          </div>
        </div>
      )}

      {/* Validation Errors from parent form */}
      {errors && (
        <div className="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-lg">
          <AlertCircle className="w-5 h-5 text-red-600 mt-0.5 flex-shrink-0" />
          <p className="text-sm text-red-700">{errors}</p>
        </div>
      )}

      {/* Empty State */}
      {sortedTiers.length === 0 && !showAddForm && (
        <div className="p-6 text-center border-2 border-dashed border-neutral-300 rounded-lg">
          <p className="text-sm text-neutral-600 mb-3">No pricing tiers added yet</p>
          <Button type="button" variant="outline" size="sm" onClick={handleOpenAddForm}>
            <Plus className="h-4 w-4 mr-1" />
            Add Your First Tier
          </Button>
        </div>
      )}

      {/* Helpful Tips */}
      {sortedTiers.length > 0 && !showAddForm && (
        <div className="flex items-start gap-2 p-3 bg-blue-50 border border-blue-200 rounded-lg">
          <svg className="w-5 h-5 text-blue-600 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
          </svg>
          <div className="text-xs text-blue-700">
            <p className="font-semibold mb-1">Tier Guidelines:</p>
            <ul className="list-disc list-inside space-y-1">
              <li>First tier must start at 1 attendee</li>
              <li>Tiers must be continuous with no gaps</li>
              <li>Only the last tier can have unlimited max attendees</li>
              <li>All tiers must use the same currency</li>
            </ul>
          </div>
        </div>
      )}
    </div>
  );
}
