'use client';

import { useState, useEffect } from 'react';
import { ShoppingBag, Plus, Minus } from 'lucide-react';
import { Input } from '@/presentation/components/ui/Input';
import { useAddOnDefinitions } from '@/presentation/hooks/useAddOns';
import type { AddOnConfigurationDto, AddOnDefinitionDto } from '@/infrastructure/api/types/events.types';

export interface AddOnSelection {
  definitionId: string;
  name: string;
  unitPrice: number;
  quantity: number;
}

interface AddOnOptionInFormProps {
  eventId: string;
  addOnConfig: AddOnConfigurationDto;
  onAddOnsChange: (selections: AddOnSelection[]) => void;
}

/**
 * Lightweight add-on selector shown inside the registration form.
 * Allows attendees to add purchasable items during event registration (combined checkout).
 * Follows the DonationOptionInForm pattern.
 */
export function AddOnOptionInForm({ eventId, addOnConfig, onAddOnsChange }: AddOnOptionInFormProps) {
  const { data: definitions, isLoading } = useAddOnDefinitions(eventId);
  const [selections, setSelections] = useState<Record<string, number>>({});

  // Filter to active definitions only
  const activeDefinitions = (definitions || [])
    .filter((d) => d.isActive)
    .sort((a, b) => a.sortOrder - b.sortOrder);

  // Notify parent when selections change
  useEffect(() => {
    const addOnSelections: AddOnSelection[] = Object.entries(selections)
      .filter(([, qty]) => qty > 0)
      .map(([defId, qty]) => {
        const def = activeDefinitions.find((d) => d.id === defId);
        return {
          definitionId: defId,
          name: def?.name || '',
          unitPrice: def?.price || 0,
          quantity: qty,
        };
      });
    onAddOnsChange(addOnSelections);
  }, [selections]); // eslint-disable-line react-hooks/exhaustive-deps

  const handleQuantityChange = (definitionId: string, delta: number, maxStock?: number | null) => {
    setSelections((prev) => {
      const current = prev[definitionId] || 0;
      let newQty = current + delta;
      if (newQty < 0) newQty = 0;
      if (maxStock != null && newQty > maxStock) newQty = maxStock;
      return { ...prev, [definitionId]: newQty };
    });
  };

  if (isLoading) {
    return (
      <div className="rounded-lg border border-emerald-200 bg-emerald-50/50 p-4">
        <div className="flex items-center gap-2">
          <ShoppingBag className="h-4 w-4 text-emerald-500 animate-pulse" />
          <span className="text-sm text-neutral-500">Loading add-ons...</span>
        </div>
      </div>
    );
  }

  if (activeDefinitions.length === 0) {
    return null; // Don't show section if no active add-ons
  }

  const totalAddOnAmount = Object.entries(selections).reduce((sum, [defId, qty]) => {
    const def = activeDefinitions.find((d) => d.id === defId);
    return sum + (def?.price || 0) * qty;
  }, 0);

  return (
    <div className="rounded-lg border border-emerald-200 bg-emerald-50/50 p-4">
      <div className="flex items-center gap-2 mb-3">
        <ShoppingBag className="h-4 w-4 text-emerald-500" />
        <span className="text-sm font-medium text-neutral-800">
          Add-ons (optional)
        </span>
      </div>

      {addOnConfig.addOnMessage && (
        <p className="text-xs text-neutral-600 mb-3">{addOnConfig.addOnMessage}</p>
      )}

      <div className="space-y-2">
        {activeDefinitions.map((def) => {
          const qty = selections[def.id] || 0;
          const isSoldOut = def.remainingStock != null && def.remainingStock <= 0;
          const maxStock = def.remainingStock;

          return (
            <div
              key={def.id}
              className={`flex items-center justify-between py-2 px-3 bg-white rounded border ${
                qty > 0 ? 'border-emerald-300' : 'border-neutral-200'
              }`}
            >
              <div className="flex-1 min-w-0 mr-3">
                <p className="text-sm font-medium text-neutral-800 truncate">{def.name}</p>
                <div className="flex items-center gap-2 text-xs text-neutral-500">
                  <span className="font-semibold text-emerald-700">${def.price.toFixed(2)}</span>
                  {isSoldOut ? (
                    <span className="text-red-500 font-medium">Sold out</span>
                  ) : maxStock != null ? (
                    <span>{maxStock} remaining</span>
                  ) : (
                    <span>Unlimited</span>
                  )}
                </div>
              </div>

              {!isSoldOut && (
                <div className="flex items-center gap-1">
                  <button
                    type="button"
                    onClick={() => handleQuantityChange(def.id, -1)}
                    disabled={qty <= 0}
                    className="p-1 rounded border border-neutral-300 text-neutral-500 hover:bg-neutral-100 disabled:opacity-30 disabled:cursor-not-allowed"
                  >
                    <Minus className="h-3 w-3" />
                  </button>
                  <span className="w-6 text-center text-sm font-medium text-neutral-800">
                    {qty}
                  </span>
                  <button
                    type="button"
                    onClick={() => handleQuantityChange(def.id, 1, maxStock)}
                    disabled={maxStock != null && qty >= maxStock}
                    className="p-1 rounded border border-neutral-300 text-neutral-500 hover:bg-neutral-100 disabled:opacity-30 disabled:cursor-not-allowed"
                  >
                    <Plus className="h-3 w-3" />
                  </button>
                </div>
              )}
            </div>
          );
        })}
      </div>

      {totalAddOnAmount > 0 && (
        <div className="mt-3 pt-2 border-t border-emerald-200 flex justify-between text-sm">
          <span className="text-neutral-600">Add-ons subtotal</span>
          <span className="font-semibold text-emerald-700">${totalAddOnAmount.toFixed(2)}</span>
        </div>
      )}
    </div>
  );
}
