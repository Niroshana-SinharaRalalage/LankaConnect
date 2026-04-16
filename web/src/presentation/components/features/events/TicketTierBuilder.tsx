'use client';

import { useState, useMemo } from 'react';
import { Plus, X, Edit2, Check, ChevronUp, ChevronDown } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { Currency } from '@/infrastructure/api/types/events.types';
import type { TicketTierDto } from '@/infrastructure/api/types/events.types';
import { useCurrencies } from '@/infrastructure/api/hooks/useReferenceData';
import { toDropdownOptions, getNameFromIntValue } from '@/infrastructure/api/utils/enum-mappers';

/**
 * Phase 8: Ticket Tier Builder Component
 *
 * Allows event organizers to create and manage ticket tiers (VIP, Plus, Basic, custom).
 * Each tier has:
 * - Name and description
 * - Adult price (required)
 * - Optional child pricing with age limit
 * - Capacity and max per user
 * - Sort order for display
 *
 * Used in EventCreationForm and EventEditForm.
 */

export interface TicketTierFormData {
  id?: string; // Present for existing tiers (edit mode)
  name: string;
  description: string;
  adultPriceAmount: number;
  adultPriceCurrency: Currency;
  childPriceAmount?: number | null;
  childPriceCurrency?: Currency | null;
  childAgeLimit?: number | null;
  capacity: number;
  maxPerUser: number;
  sortOrder: number;
  isFree?: boolean; // Computed: adultPriceAmount === 0
}

interface TicketTierBuilderProps {
  tiers: TicketTierFormData[];
  onChange: (tiers: TicketTierFormData[]) => void;
  defaultCurrency: Currency;
  eventCapacity?: number;
  errors?: string;
}

const DEFAULT_TIERS: Omit<TicketTierFormData, 'sortOrder'>[] = [
  { name: 'VIP', description: 'Premium seating with exclusive perks', adultPriceAmount: 0, adultPriceCurrency: Currency.USD, capacity: 30, maxPerUser: 10 },
  { name: 'Plus', description: 'Enhanced experience with added benefits', adultPriceAmount: 0, adultPriceCurrency: Currency.USD, capacity: 50, maxPerUser: 10 },
  { name: 'Basic', description: 'General admission', adultPriceAmount: 0, adultPriceCurrency: Currency.USD, capacity: 100, maxPerUser: 10 },
];

export function TicketTierBuilder({
  tiers,
  onChange,
  defaultCurrency,
  eventCapacity,
  errors,
}: TicketTierBuilderProps) {
  const { data: currencies } = useCurrencies();
  const currencyOptions = useMemo(() => toDropdownOptions(currencies), [currencies]);

  const [showAddForm, setShowAddForm] = useState(false);
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const [newTier, setNewTier] = useState<Partial<TicketTierFormData>>({
    name: '',
    description: '',
    adultPriceAmount: 0,
    adultPriceCurrency: defaultCurrency,
    capacity: 50,
    maxPerUser: 10,
  });
  const [enableChildPricing, setEnableChildPricing] = useState(false);
  const [tierError, setTierError] = useState<string | null>(null);

  const totalTierCapacity = useMemo(
    () => tiers.reduce((sum, t) => sum + (t.capacity || 0), 0),
    [tiers]
  );

  const addDefaultTiers = () => {
    const defaults = DEFAULT_TIERS.map((t, i) => ({
      ...t,
      adultPriceCurrency: defaultCurrency,
      sortOrder: i,
    }));
    onChange(defaults);
  };

  const validateTier = (tier: Partial<TicketTierFormData>): string | null => {
    if (!tier.name?.trim()) return 'Tier name is required';
    if (tier.adultPriceAmount == null || tier.adultPriceAmount < 0) return 'Adult price must be 0 or greater';
    if (!tier.capacity || tier.capacity < 1) return 'Capacity must be at least 1';
    if (!tier.maxPerUser || tier.maxPerUser < 1) return 'Max per user must be at least 1';

    // Check for duplicate names (case-insensitive)
    const existingNames = tiers
      .filter((_, i) => i !== editingIndex)
      .map(t => t.name.toLowerCase());
    if (existingNames.includes(tier.name.trim().toLowerCase())) {
      return `A tier named "${tier.name.trim()}" already exists`;
    }

    // Child pricing validation
    if (enableChildPricing) {
      if (tier.childPriceAmount == null || tier.childPriceAmount < 0) return 'Child price must be 0 or greater';
      if (!tier.childAgeLimit || tier.childAgeLimit < 1 || tier.childAgeLimit > 17) return 'Child age limit must be between 1 and 17';
    }

    return null;
  };

  const handleAddTier = () => {
    const error = validateTier(newTier);
    if (error) {
      setTierError(error);
      return;
    }

    const tier: TicketTierFormData = {
      name: newTier.name!.trim(),
      description: newTier.description || '',
      adultPriceAmount: newTier.adultPriceAmount!,
      adultPriceCurrency: newTier.adultPriceCurrency || defaultCurrency,
      childPriceAmount: enableChildPricing ? newTier.childPriceAmount : null,
      childPriceCurrency: enableChildPricing ? (newTier.childPriceCurrency || defaultCurrency) : null,
      childAgeLimit: enableChildPricing ? newTier.childAgeLimit : null,
      capacity: newTier.capacity!,
      maxPerUser: newTier.maxPerUser!,
      sortOrder: tiers.length,
      isFree: newTier.adultPriceAmount === 0,
    };

    onChange([...tiers, tier]);
    resetForm();
  };

  const handleUpdateTier = () => {
    if (editingIndex === null) return;
    const error = validateTier(newTier);
    if (error) {
      setTierError(error);
      return;
    }

    const updated = [...tiers];
    updated[editingIndex] = {
      ...updated[editingIndex],
      name: newTier.name!.trim(),
      description: newTier.description || '',
      adultPriceAmount: newTier.adultPriceAmount!,
      adultPriceCurrency: newTier.adultPriceCurrency || defaultCurrency,
      childPriceAmount: enableChildPricing ? newTier.childPriceAmount : null,
      childPriceCurrency: enableChildPricing ? (newTier.childPriceCurrency || defaultCurrency) : null,
      childAgeLimit: enableChildPricing ? newTier.childAgeLimit : null,
      capacity: newTier.capacity!,
      maxPerUser: newTier.maxPerUser!,
      isFree: newTier.adultPriceAmount === 0,
    };
    onChange(updated);
    resetForm();
  };

  const startEdit = (index: number) => {
    const tier = tiers[index];
    setEditingIndex(index);
    setNewTier({
      name: tier.name,
      description: tier.description,
      adultPriceAmount: tier.adultPriceAmount,
      adultPriceCurrency: tier.adultPriceCurrency,
      childPriceAmount: tier.childPriceAmount,
      childPriceCurrency: tier.childPriceCurrency,
      childAgeLimit: tier.childAgeLimit,
      capacity: tier.capacity,
      maxPerUser: tier.maxPerUser,
    });
    setEnableChildPricing(tier.childPriceAmount != null);
    setShowAddForm(true);
    setTierError(null);
  };

  const removeTier = (index: number) => {
    const updated = tiers.filter((_, i) => i !== index).map((t, i) => ({ ...t, sortOrder: i }));
    onChange(updated);
  };

  const moveTier = (index: number, direction: 'up' | 'down') => {
    const newIndex = direction === 'up' ? index - 1 : index + 1;
    if (newIndex < 0 || newIndex >= tiers.length) return;
    const updated = [...tiers];
    [updated[index], updated[newIndex]] = [updated[newIndex], updated[index]];
    onChange(updated.map((t, i) => ({ ...t, sortOrder: i })));
  };

  const resetForm = () => {
    setShowAddForm(false);
    setEditingIndex(null);
    setNewTier({
      name: '',
      description: '',
      adultPriceAmount: 0,
      adultPriceCurrency: defaultCurrency,
      capacity: 50,
      maxPerUser: 10,
    });
    setEnableChildPricing(false);
    setTierError(null);
  };

  const formatPrice = (amount: number, currency: Currency) => {
    const currencyName = getNameFromIntValue(currencies, currency) || 'USD';
    return amount === 0 ? 'FREE' : `${currencyName} ${amount.toFixed(2)}`;
  };

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h4 className="text-sm font-medium text-gray-700">Ticket Tiers</h4>
        {tiers.length === 0 && (
          <Button type="button" variant="outline" size="sm" onClick={addDefaultTiers}>
            Use Default Tiers (VIP / Plus / Basic)
          </Button>
        )}
      </div>

      {/* Capacity summary */}
      {tiers.length > 0 && (
        <div className={`text-xs px-3 py-2 rounded-md ${
          eventCapacity && totalTierCapacity > eventCapacity
            ? 'bg-red-50 text-red-700'
            : 'bg-blue-50 text-blue-700'
        }`}>
          Total tier capacity: {totalTierCapacity}
          {eventCapacity ? ` / ${eventCapacity} event capacity` : ''}
          {eventCapacity && totalTierCapacity > eventCapacity && ' (exceeds event capacity!)'}
        </div>
      )}

      {/* Existing tiers list */}
      {tiers.length > 0 && (
        <div className="space-y-2">
          {tiers.map((tier, index) => (
            <div
              key={index}
              className="flex items-center gap-3 p-3 bg-gray-50 rounded-lg border border-gray-200"
            >
              {/* Reorder buttons */}
              <div className="flex flex-col gap-0.5">
                <button
                  type="button"
                  onClick={() => moveTier(index, 'up')}
                  disabled={index === 0}
                  className="p-0.5 text-gray-400 hover:text-gray-600 disabled:opacity-30"
                >
                  <ChevronUp size={14} />
                </button>
                <button
                  type="button"
                  onClick={() => moveTier(index, 'down')}
                  disabled={index === tiers.length - 1}
                  className="p-0.5 text-gray-400 hover:text-gray-600 disabled:opacity-30"
                >
                  <ChevronDown size={14} />
                </button>
              </div>

              {/* Tier info */}
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2">
                  <span className="font-medium text-sm text-gray-900">{tier.name}</span>
                  {tier.isFree || tier.adultPriceAmount === 0 ? (
                    <span className="px-1.5 py-0.5 text-xs font-medium bg-green-100 text-green-700 rounded">FREE</span>
                  ) : (
                    <span className="text-sm text-gray-600">
                      {formatPrice(tier.adultPriceAmount, tier.adultPriceCurrency)}
                    </span>
                  )}
                  {tier.childPriceAmount != null && (
                    <span className="text-xs text-gray-500">
                      / Child: {formatPrice(tier.childPriceAmount, tier.childPriceCurrency || tier.adultPriceCurrency)}
                    </span>
                  )}
                </div>
                <div className="text-xs text-gray-500 mt-0.5">
                  Capacity: {tier.capacity} | Max/user: {tier.maxPerUser}
                  {tier.description && ` | ${tier.description}`}
                </div>
              </div>

              {/* Actions */}
              <button
                type="button"
                onClick={() => startEdit(index)}
                className="p-1.5 text-gray-400 hover:text-blue-600"
                title="Edit tier"
              >
                <Edit2 size={14} />
              </button>
              <button
                type="button"
                onClick={() => removeTier(index)}
                className="p-1.5 text-gray-400 hover:text-red-600"
                title="Remove tier"
              >
                <X size={14} />
              </button>
            </div>
          ))}
        </div>
      )}

      {/* Add/Edit tier form */}
      {showAddForm ? (
        <div className="p-4 border border-blue-200 rounded-lg bg-blue-50/50 space-y-3">
          <h5 className="text-sm font-medium text-gray-700">
            {editingIndex !== null ? 'Edit Tier' : 'Add New Tier'}
          </h5>

          {tierError && (
            <p className="text-sm text-red-600">{tierError}</p>
          )}

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Tier Name *</label>
              <Input
                value={newTier.name || ''}
                onChange={(e) => setNewTier({ ...newTier, name: e.target.value })}
                placeholder="e.g., VIP, Plus, Basic"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Description</label>
              <Input
                value={newTier.description || ''}
                onChange={(e) => setNewTier({ ...newTier, description: e.target.value })}
                placeholder="Optional description"
              />
            </div>
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Adult Price *</label>
              <Input
                type="number"
                min="0"
                step="0.01"
                value={newTier.adultPriceAmount ?? ''}
                onChange={(e) => setNewTier({ ...newTier, adultPriceAmount: parseFloat(e.target.value) || 0 })}
                placeholder="0.00"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Currency</label>
              <select
                value={newTier.adultPriceCurrency || defaultCurrency}
                onChange={(e) => setNewTier({ ...newTier, adultPriceCurrency: parseInt(e.target.value) as Currency })}
                className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
              >
                {currencyOptions.map((opt) => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Capacity *</label>
              <Input
                type="number"
                min="1"
                value={newTier.capacity ?? ''}
                onChange={(e) => setNewTier({ ...newTier, capacity: parseInt(e.target.value) || 0 })}
                placeholder="50"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Max Per User</label>
              <Input
                type="number"
                min="1"
                max="50"
                value={newTier.maxPerUser ?? 10}
                onChange={(e) => setNewTier({ ...newTier, maxPerUser: parseInt(e.target.value) || 10 })}
              />
            </div>
          </div>

          {/* Child pricing toggle */}
          <div className="space-y-2">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={enableChildPricing}
                onChange={(e) => {
                  setEnableChildPricing(e.target.checked);
                  if (!e.target.checked) {
                    setNewTier({ ...newTier, childPriceAmount: null, childAgeLimit: null });
                  }
                }}
                className="rounded border-gray-300"
              />
              <span className="text-sm text-gray-700">Enable child pricing for this tier</span>
            </label>

            {enableChildPricing && (
              <div className="grid grid-cols-2 gap-3 pl-6">
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Child Price *</label>
                  <Input
                    type="number"
                    min="0"
                    step="0.01"
                    value={newTier.childPriceAmount ?? ''}
                    onChange={(e) => setNewTier({ ...newTier, childPriceAmount: parseFloat(e.target.value) || 0 })}
                    placeholder="0.00"
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Child Age Limit *</label>
                  <Input
                    type="number"
                    min="1"
                    max="17"
                    value={newTier.childAgeLimit ?? ''}
                    onChange={(e) => setNewTier({ ...newTier, childAgeLimit: parseInt(e.target.value) || null })}
                    placeholder="12"
                  />
                </div>
              </div>
            )}
          </div>

          {/* Actions */}
          <div className="flex gap-2 pt-1">
            <Button type="button" size="sm" onClick={editingIndex !== null ? handleUpdateTier : handleAddTier}>
              <Check size={14} className="mr-1" />
              {editingIndex !== null ? 'Update Tier' : 'Add Tier'}
            </Button>
            <Button type="button" variant="outline" size="sm" onClick={resetForm}>
              Cancel
            </Button>
          </div>
        </div>
      ) : (
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => {
            setShowAddForm(true);
            setTierError(null);
          }}
        >
          <Plus size={14} className="mr-1" />
          Add Tier
        </Button>
      )}

      {/* Errors */}
      {errors && (
        <p className="text-sm text-red-600">{errors}</p>
      )}
    </div>
  );
}
