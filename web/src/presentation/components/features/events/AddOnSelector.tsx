'use client';

import { useState } from 'react';
import { ShoppingBag, RefreshCw } from 'lucide-react';
import { CollapsibleSection } from '@/presentation/components/ui/CollapsibleSection';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { Card, CardContent } from '@/presentation/components/ui/Card';
import { useAddOnDefinitions, usePurchaseAddOn } from '@/presentation/hooks/useAddOns';
import type { AddOnConfigurationDto, AddOnDefinitionDto } from '@/infrastructure/api/types/events.types';

interface AddOnSelectorProps {
  eventId: string;
  addOnConfig: AddOnConfigurationDto;
}

/**
 * Public-facing add-on selector shown on the event details page.
 * Displays available add-on items with stock levels and purchase buttons.
 * Purchase flow: collect buyer info + quantity -> create Stripe checkout -> redirect.
 */
export function AddOnSelector({ eventId, addOnConfig }: AddOnSelectorProps) {
  const [selectedAddOnId, setSelectedAddOnId] = useState<string | null>(null);
  const [buyerName, setBuyerName] = useState('');
  const [buyerEmail, setBuyerEmail] = useState('');
  const [buyerPhone, setBuyerPhone] = useState('');
  const [quantity, setQuantity] = useState(1);
  const [error, setError] = useState<string | null>(null);

  const { data: definitions, isLoading, isError, refetch } = useAddOnDefinitions(eventId);
  const purchaseAddOn = usePurchaseAddOn();

  // Filter active definitions and sort by sortOrder
  const activeDefinitions = (definitions ?? [])
    .filter((d) => d.isActive)
    .sort((a, b) => a.sortOrder - b.sortOrder);

  const selectedDefinition = activeDefinitions.find((d) => d.id === selectedAddOnId) ?? null;

  const isSoldOut = (def: AddOnDefinitionDto): boolean => {
    if (def.remainingStock === null || def.remainingStock === undefined) return false;
    return def.remainingStock <= 0;
  };

  const getMaxQuantity = (def: AddOnDefinitionDto): number => {
    if (def.remainingStock === null || def.remainingStock === undefined) return 99;
    return Math.max(def.remainingStock, 0);
  };

  const handleSelectAddOn = (definitionId: string) => {
    if (selectedAddOnId === definitionId) {
      // Toggle off if clicking the same card
      resetForm();
      return;
    }
    setSelectedAddOnId(definitionId);
    setQuantity(1);
    setError(null);
  };

  const resetForm = () => {
    setSelectedAddOnId(null);
    setBuyerName('');
    setBuyerEmail('');
    setBuyerPhone('');
    setQuantity(1);
    setError(null);
  };

  const handlePurchase = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!selectedDefinition) {
      setError('Please select an add-on.');
      return;
    }

    if (!buyerName.trim()) {
      setError('Please enter your name.');
      return;
    }

    if (!buyerEmail.trim()) {
      setError('Please enter your email address.');
      return;
    }

    if (quantity < 1) {
      setError('Quantity must be at least 1.');
      return;
    }

    const maxQty = getMaxQuantity(selectedDefinition);
    if (quantity > maxQty) {
      setError(`Only ${maxQty} remaining. Please reduce your quantity.`);
      return;
    }

    try {
      const checkoutUrl = await purchaseAddOn.mutateAsync({
        eventId,
        definitionId: selectedDefinition.id,
        request: {
          buyerName: buyerName.trim(),
          buyerEmail: buyerEmail.trim(),
          buyerPhone: buyerPhone.trim() || null,
          quantity,
          successUrl: `${window.location.origin}/events/${eventId}?addon=success`,
          cancelUrl: `${window.location.origin}/events/${eventId}?addon=cancelled`,
        },
      });

      if (checkoutUrl) {
        window.location.href = checkoutUrl;
      }
    } catch (err: any) {
      setError(err?.response?.data?.detail || 'Failed to process purchase. Please try again.');
    }
  };

  const formatPrice = (price: number, currency: string): string => {
    return `$${price.toFixed(2)}`;
  };

  return (
    <CollapsibleSection
      title="Event Add-Ons"
      icon={<ShoppingBag className="h-5 w-5 text-emerald-600" />}
      description={addOnConfig.addOnMessage || undefined}
      defaultOpen={false}
    >
      {/* Loading State */}
      {isLoading && (
        <div className="flex items-center justify-center py-8">
          <RefreshCw className="h-5 w-5 text-emerald-600 animate-spin" />
          <span className="ml-2 text-sm text-neutral-500">Loading add-ons...</span>
        </div>
      )}

      {/* Error State */}
      {isError && (
        <div className="text-center py-8">
          <p className="text-sm text-red-600 mb-2">Failed to load add-ons.</p>
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
          >
            Try Again
          </Button>
        </div>
      )}

      {/* Empty State */}
      {!isLoading && !isError && activeDefinitions.length === 0 && (
        <div className="flex flex-col items-center justify-center py-8 text-neutral-400">
          <ShoppingBag className="h-10 w-10 mb-2" />
          <p className="text-sm">No add-ons available for this event.</p>
        </div>
      )}

      {/* Add-On Cards Grid */}
      {!isLoading && !isError && activeDefinitions.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {activeDefinitions.map((definition) => {
            const soldOut = isSoldOut(definition);
            const isSelected = selectedAddOnId === definition.id;

            return (
              <div key={definition.id}>
                <Card
                  className={`transition-colors ${
                    isSelected
                      ? 'border-emerald-500 bg-emerald-50/50'
                      : soldOut
                        ? 'border-neutral-200 bg-neutral-50 opacity-75'
                        : 'border-neutral-200 hover:border-emerald-300'
                  }`}
                >
                  <CardContent className="p-4">
                    {/* Name */}
                    <h4 className="font-semibold text-neutral-900">{definition.name}</h4>

                    {/* Description */}
                    {definition.description && (
                      <p className="text-sm text-neutral-500 mt-1 line-clamp-2">
                        {definition.description}
                      </p>
                    )}

                    {/* Price & Stock */}
                    <div className="flex items-center justify-between mt-3">
                      <span className="text-lg font-semibold text-emerald-700">
                        {formatPrice(definition.price, definition.currency)}
                      </span>
                      <span
                        className={`text-xs font-medium px-2 py-0.5 rounded-full ${
                          soldOut
                            ? 'bg-red-100 text-red-700'
                            : definition.remainingStock !== null && definition.remainingStock !== undefined
                              ? 'bg-emerald-100 text-emerald-700'
                              : 'bg-neutral-100 text-neutral-600'
                        }`}
                      >
                        {soldOut
                          ? 'Sold out'
                          : definition.remainingStock !== null && definition.remainingStock !== undefined
                            ? `${definition.remainingStock} remaining`
                            : 'Unlimited'}
                      </span>
                    </div>

                    {/* Purchase Button */}
                    <Button
                      type="button"
                      disabled={soldOut}
                      onClick={() => handleSelectAddOn(definition.id)}
                      className="w-full mt-3"
                      variant={isSelected ? 'outline' : 'default'}
                      style={!isSelected && !soldOut ? { background: '#059669' } : undefined}
                    >
                      {isSelected ? 'Cancel' : soldOut ? 'Sold Out' : 'Purchase'}
                    </Button>
                  </CardContent>
                </Card>

                {/* Inline Purchase Form */}
                {isSelected && selectedDefinition && (
                  <Card className="mt-2 border-emerald-300 bg-emerald-50/30">
                    <CardContent className="p-4">
                      <form onSubmit={handlePurchase} className="space-y-3">
                        {/* Buyer Name */}
                        <div>
                          <label
                            htmlFor={`buyerName-${definition.id}`}
                            className="block text-sm font-medium text-neutral-700 mb-1"
                          >
                            Your Name *
                          </label>
                          <Input
                            id={`buyerName-${definition.id}`}
                            value={buyerName}
                            onChange={(e) => { setBuyerName(e.target.value); setError(null); }}
                            placeholder="Enter your name"
                            required
                          />
                        </div>

                        {/* Email */}
                        <div>
                          <label
                            htmlFor={`buyerEmail-${definition.id}`}
                            className="block text-sm font-medium text-neutral-700 mb-1"
                          >
                            Email Address *
                          </label>
                          <Input
                            id={`buyerEmail-${definition.id}`}
                            type="email"
                            value={buyerEmail}
                            onChange={(e) => { setBuyerEmail(e.target.value); setError(null); }}
                            placeholder="your@email.com"
                            required
                          />
                        </div>

                        {/* Phone (optional) */}
                        <div>
                          <label
                            htmlFor={`buyerPhone-${definition.id}`}
                            className="block text-sm font-medium text-neutral-700 mb-1"
                          >
                            Phone (optional)
                          </label>
                          <Input
                            id={`buyerPhone-${definition.id}`}
                            type="tel"
                            value={buyerPhone}
                            onChange={(e) => setBuyerPhone(e.target.value)}
                            placeholder="(555) 123-4567"
                          />
                        </div>

                        {/* Quantity */}
                        <div>
                          <label
                            htmlFor={`quantity-${definition.id}`}
                            className="block text-sm font-medium text-neutral-700 mb-1"
                          >
                            Quantity
                          </label>
                          <Input
                            id={`quantity-${definition.id}`}
                            type="number"
                            min={1}
                            max={getMaxQuantity(selectedDefinition)}
                            value={quantity}
                            onChange={(e) => { setQuantity(Math.max(1, parseInt(e.target.value, 10) || 1)); setError(null); }}
                          />
                        </div>

                        {/* Total */}
                        <div className="flex items-center justify-between py-2 px-3 bg-emerald-100 rounded-lg">
                          <span className="text-sm font-medium text-neutral-700">Total</span>
                          <span className="text-lg font-bold text-emerald-800">
                            {formatPrice(selectedDefinition.price * quantity, selectedDefinition.currency)}
                          </span>
                        </div>

                        {/* Error Message */}
                        {error && (
                          <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-sm text-red-600">
                            {error}
                          </div>
                        )}

                        {/* Action Buttons */}
                        <div className="flex gap-2">
                          <Button
                            type="submit"
                            disabled={purchaseAddOn.isPending}
                            className="flex-1"
                            style={{ background: '#059669' }}
                          >
                            {purchaseAddOn.isPending ? 'Processing...' : 'Buy Now'}
                          </Button>
                          <Button
                            type="button"
                            variant="outline"
                            onClick={resetForm}
                            disabled={purchaseAddOn.isPending}
                          >
                            Cancel
                          </Button>
                        </div>
                      </form>
                    </CardContent>
                  </Card>
                )}
              </div>
            );
          })}
        </div>
      )}
    </CollapsibleSection>
  );
}
