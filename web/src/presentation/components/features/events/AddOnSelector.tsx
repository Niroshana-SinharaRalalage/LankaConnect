'use client';

import { useState, useMemo, useEffect } from 'react';
import { useSearchParams } from 'next/navigation';
import { ShoppingBag, RefreshCw, ShoppingCart, Plus, Minus, Trash2, Package, CheckCircle, Clock } from 'lucide-react';
import { CollapsibleSection } from '@/presentation/components/ui/CollapsibleSection';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { Card, CardContent } from '@/presentation/components/ui/Card';
import { useAddOnDefinitions, usePurchaseAddOnCart, useMyAddOnPurchases } from '@/presentation/hooks/useAddOns';
import type { AddOnConfigurationDto, AddOnDefinitionDto } from '@/infrastructure/api/types/events.types';

interface CartItem {
  definitionId: string;
  quantity: number;
}

interface AddOnSelectorProps {
  eventId: string;
  addOnConfig: AddOnConfigurationDto;
}

const STORAGE_KEY_PREFIX = 'lc_addon_email_';

/**
 * Public-facing add-on selector with cart support.
 * Users can add quantities to multiple add-ons and checkout all at once.
 * Free items complete immediately; paid items go to a single Stripe checkout.
 * Shows "My Add-Ons" section for returning buyers.
 */
export function AddOnSelector({ eventId, addOnConfig }: AddOnSelectorProps) {
  const [cart, setCart] = useState<CartItem[]>([]);
  const [showCheckout, setShowCheckout] = useState(false);
  const [buyerName, setBuyerName] = useState('');
  const [buyerEmail, setBuyerEmail] = useState('');
  const [buyerPhone, setBuyerPhone] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [savedEmail, setSavedEmail] = useState<string | null>(null);
  const [lookupEmail, setLookupEmail] = useState('');
  const [showLookup, setShowLookup] = useState(false);

  const searchParams = useSearchParams();

  const { data: definitions, isLoading, isError, refetch } = useAddOnDefinitions(eventId);
  const purchaseCart = usePurchaseAddOnCart();
  const { data: myPurchases, isLoading: myPurchasesLoading, refetch: refetchMyPurchases } = useMyAddOnPurchases(
    eventId,
    savedEmail,
    !!savedEmail
  );

  // On mount, check localStorage for saved buyer email
  useEffect(() => {
    try {
      const stored = localStorage.getItem(`${STORAGE_KEY_PREFIX}${eventId}`);
      if (stored) {
        setSavedEmail(stored);
      }
    } catch {
      // localStorage not available
    }
  }, [eventId]);

  // After successful purchase redirect, refetch purchases
  useEffect(() => {
    const addonParam = searchParams.get('addon');
    if (addonParam === 'success' && savedEmail) {
      refetchMyPurchases();
    }
  }, [searchParams, savedEmail, refetchMyPurchases]);

  // Filter active definitions and sort by sortOrder
  const activeDefinitions = useMemo(
    () =>
      (definitions ?? [])
        .filter((d) => d.isActive)
        .sort((a, b) => a.sortOrder - b.sortOrder),
    [definitions]
  );

  const isSoldOut = (def: AddOnDefinitionDto): boolean => {
    if (def.remainingStock === null || def.remainingStock === undefined) return false;
    return def.remainingStock <= 0;
  };

  const getMaxQuantity = (def: AddOnDefinitionDto): number => {
    if (def.remainingStock === null || def.remainingStock === undefined) return 99;
    return Math.max(def.remainingStock, 0);
  };

  const getCartQuantity = (definitionId: string): number => {
    return cart.find((item) => item.definitionId === definitionId)?.quantity ?? 0;
  };

  const updateCart = (definitionId: string, quantity: number) => {
    setError(null);
    if (quantity <= 0) {
      setCart((prev) => prev.filter((item) => item.definitionId !== definitionId));
    } else {
      setCart((prev) => {
        const existing = prev.find((item) => item.definitionId === definitionId);
        if (existing) {
          return prev.map((item) =>
            item.definitionId === definitionId ? { ...item, quantity } : item
          );
        }
        return [...prev, { definitionId, quantity }];
      });
    }
  };

  const addToCart = (def: AddOnDefinitionDto) => {
    const current = getCartQuantity(def.id);
    const max = getMaxQuantity(def);
    if (current < max) {
      updateCart(def.id, current + 1);
    }
  };

  const removeFromCart = (definitionId: string) => {
    const current = getCartQuantity(definitionId);
    if (current > 0) {
      updateCart(definitionId, current - 1);
    }
  };

  const clearCart = () => {
    setCart([]);
    setShowCheckout(false);
    setBuyerName('');
    setBuyerEmail('');
    setBuyerPhone('');
    setError(null);
  };

  const cartTotal = useMemo(() => {
    return cart.reduce((total, item) => {
      const def = activeDefinitions.find((d) => d.id === item.definitionId);
      if (!def) return total;
      return total + def.price * item.quantity;
    }, 0);
  }, [cart, activeDefinitions]);

  const cartItemCount = useMemo(() => {
    return cart.reduce((total, item) => total + item.quantity, 0);
  }, [cart]);

  const formatPrice = (price: number): string => {
    if (price === 0) return 'Free';
    return `$${price.toFixed(2)}`;
  };

  const handleCheckout = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (cart.length === 0) {
      setError('Your cart is empty. Add some items first.');
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

    // Validate quantities against available stock
    for (const item of cart) {
      const def = activeDefinitions.find((d) => d.id === item.definitionId);
      if (!def) {
        setError('One or more items in your cart are no longer available.');
        return;
      }
      const max = getMaxQuantity(def);
      if (item.quantity > max) {
        setError(`Only ${max} remaining for "${def.name}". Please reduce your quantity.`);
        return;
      }
    }

    try {
      // Save buyer email to localStorage for "My Add-Ons" section
      try {
        localStorage.setItem(`${STORAGE_KEY_PREFIX}${eventId}`, buyerEmail.trim().toLowerCase());
      } catch {
        // localStorage not available
      }

      const checkoutUrl = await purchaseCart.mutateAsync({
        eventId,
        request: {
          buyerName: buyerName.trim(),
          buyerEmail: buyerEmail.trim(),
          buyerPhone: buyerPhone.trim() || null,
          items: cart.map((item) => ({
            addOnDefinitionId: item.definitionId,
            quantity: item.quantity,
          })),
          successUrl: `${window.location.origin}/events/${eventId}?addon=success`,
          cancelUrl: `${window.location.origin}/events/${eventId}?addon=cancelled`,
        },
      });

      // Update saved email for immediate "My Add-Ons" display
      setSavedEmail(buyerEmail.trim().toLowerCase());

      if (checkoutUrl) {
        window.location.href = checkoutUrl;
      }
    } catch (err: any) {
      setError(err?.response?.data?.detail || 'Failed to process purchase. Please try again.');
    }
  };

  const handleLookup = (e: React.FormEvent) => {
    e.preventDefault();
    if (lookupEmail.trim()) {
      const email = lookupEmail.trim().toLowerCase();
      try {
        localStorage.setItem(`${STORAGE_KEY_PREFIX}${eventId}`, email);
      } catch {
        // localStorage not available
      }
      setSavedEmail(email);
      setShowLookup(false);
    }
  };

  // Filter to only show completed/pending (not abandoned/failed)
  const visiblePurchases = useMemo(() => {
    if (!myPurchases) return [];
    return myPurchases.filter((p) => p.status === 'Completed' || p.status === 'Pending');
  }, [myPurchases]);

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
          <Button variant="outline" size="sm" onClick={() => refetch()}>
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
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {activeDefinitions.map((definition) => {
              const soldOut = isSoldOut(definition);
              const qty = getCartQuantity(definition.id);
              const inCart = qty > 0;
              const max = getMaxQuantity(definition);

              return (
                <Card
                  key={definition.id}
                  className={`transition-colors ${
                    inCart
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
                        {formatPrice(definition.price)}
                      </span>
                      <span
                        className={`text-xs font-medium px-2 py-0.5 rounded-full ${
                          soldOut
                            ? 'bg-red-100 text-red-700'
                            : definition.remainingStock !== null &&
                                definition.remainingStock !== undefined
                              ? 'bg-emerald-100 text-emerald-700'
                              : 'bg-neutral-100 text-neutral-600'
                        }`}
                      >
                        {soldOut
                          ? 'Sold out'
                          : definition.remainingStock !== null &&
                              definition.remainingStock !== undefined
                            ? `${definition.remainingStock} remaining`
                            : 'Unlimited'}
                      </span>
                    </div>

                    {/* Quantity Controls */}
                    {soldOut ? (
                      <Button type="button" disabled className="w-full mt-3" variant="outline">
                        Sold Out
                      </Button>
                    ) : inCart ? (
                      <div className="flex items-center gap-2 mt-3">
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          onClick={() => removeFromCart(definition.id)}
                          className="h-9 w-9 p-0"
                        >
                          {qty === 1 ? (
                            <Trash2 className="h-4 w-4 text-red-500" />
                          ) : (
                            <Minus className="h-4 w-4" />
                          )}
                        </Button>
                        <span className="flex-1 text-center font-semibold text-neutral-900">
                          {qty}
                        </span>
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          onClick={() => addToCart(definition)}
                          disabled={qty >= max}
                          className="h-9 w-9 p-0"
                        >
                          <Plus className="h-4 w-4" />
                        </Button>
                        {definition.price > 0 && (
                          <span className="text-sm font-medium text-emerald-700 ml-2">
                            {formatPrice(definition.price * qty)}
                          </span>
                        )}
                      </div>
                    ) : (
                      <Button
                        type="button"
                        onClick={() => addToCart(definition)}
                        className="w-full mt-3"
                        style={{ background: '#059669' }}
                      >
                        {definition.price === 0 ? 'Add Free Item' : 'Add to Cart'}
                      </Button>
                    )}
                  </CardContent>
                </Card>
              );
            })}
          </div>

          {/* Cart Summary & Checkout */}
          {cart.length > 0 && (
            <div className="mt-6">
              {!showCheckout ? (
                /* Cart Summary Bar */
                <div className="flex items-center justify-between p-4 bg-emerald-50 border border-emerald-200 rounded-lg">
                  <div className="flex items-center gap-3">
                    <ShoppingCart className="h-5 w-5 text-emerald-600" />
                    <span className="text-sm font-medium text-neutral-700">
                      {cartItemCount} item{cartItemCount !== 1 ? 's' : ''} in cart
                    </span>
                    <span className="text-lg font-bold text-emerald-800">
                      {cartTotal === 0 ? 'Free' : `$${cartTotal.toFixed(2)}`}
                    </span>
                  </div>
                  <div className="flex gap-2">
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={clearCart}
                    >
                      Clear
                    </Button>
                    <Button
                      type="button"
                      size="sm"
                      onClick={() => setShowCheckout(true)}
                      style={{ background: '#059669' }}
                    >
                      {cartTotal === 0 ? 'Get Now' : 'Checkout'}
                    </Button>
                  </div>
                </div>
              ) : (
                /* Checkout Form */
                <Card className="border-emerald-300 bg-emerald-50/30">
                  <CardContent className="p-4">
                    {/* Cart Items Summary */}
                    <div className="mb-4 space-y-2">
                      <h4 className="font-semibold text-neutral-900 flex items-center gap-2">
                        <ShoppingCart className="h-4 w-4 text-emerald-600" />
                        Your Cart
                      </h4>
                      {cart.map((item) => {
                        const def = activeDefinitions.find((d) => d.id === item.definitionId);
                        if (!def) return null;
                        return (
                          <div
                            key={item.definitionId}
                            className="flex items-center justify-between text-sm py-1 border-b border-emerald-100 last:border-0"
                          >
                            <span className="text-neutral-700">
                              {def.name} x{item.quantity}
                            </span>
                            <span className="font-medium text-neutral-900">
                              {formatPrice(def.price * item.quantity)}
                            </span>
                          </div>
                        );
                      })}
                      <div className="flex items-center justify-between pt-2 font-bold">
                        <span className="text-neutral-900">Total</span>
                        <span className="text-emerald-800 text-lg">
                          {cartTotal === 0 ? 'Free' : `$${cartTotal.toFixed(2)}`}
                        </span>
                      </div>
                    </div>

                    <form onSubmit={handleCheckout} className="space-y-3">
                      {/* Buyer Name */}
                      <div>
                        <label
                          htmlFor="cart-buyerName"
                          className="block text-sm font-medium text-neutral-700 mb-1"
                        >
                          Your Name *
                        </label>
                        <Input
                          id="cart-buyerName"
                          value={buyerName}
                          onChange={(e) => {
                            setBuyerName(e.target.value);
                            setError(null);
                          }}
                          placeholder="Enter your name"
                          required
                        />
                      </div>

                      {/* Email */}
                      <div>
                        <label
                          htmlFor="cart-buyerEmail"
                          className="block text-sm font-medium text-neutral-700 mb-1"
                        >
                          Email Address *
                        </label>
                        <Input
                          id="cart-buyerEmail"
                          type="email"
                          value={buyerEmail}
                          onChange={(e) => {
                            setBuyerEmail(e.target.value);
                            setError(null);
                          }}
                          placeholder="your@email.com"
                          required
                        />
                      </div>

                      {/* Phone (optional) */}
                      <div>
                        <label
                          htmlFor="cart-buyerPhone"
                          className="block text-sm font-medium text-neutral-700 mb-1"
                        >
                          Phone (optional)
                        </label>
                        <Input
                          id="cart-buyerPhone"
                          type="tel"
                          value={buyerPhone}
                          onChange={(e) => setBuyerPhone(e.target.value)}
                          placeholder="(555) 123-4567"
                        />
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
                          disabled={purchaseCart.isPending}
                          className="flex-1"
                          style={{ background: '#059669' }}
                        >
                          {purchaseCart.isPending
                            ? 'Processing...'
                            : cartTotal === 0
                              ? 'Get Now'
                              : `Pay $${cartTotal.toFixed(2)}`}
                        </Button>
                        <Button
                          type="button"
                          variant="outline"
                          onClick={() => setShowCheckout(false)}
                          disabled={purchaseCart.isPending}
                        >
                          Back
                        </Button>
                      </div>
                    </form>
                  </CardContent>
                </Card>
              )}
            </div>
          )}
        </>
      )}

      {/* ─── My Add-Ons Section ─── */}
      {!isLoading && !isError && (
        <div className="mt-6 pt-6 border-t border-neutral-200">
          <div className="flex items-center justify-between mb-3">
            <h4 className="font-semibold text-neutral-900 flex items-center gap-2">
              <Package className="h-5 w-5 text-emerald-600" />
              My Add-Ons
            </h4>
            {!savedEmail && !showLookup && (
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setShowLookup(true)}
              >
                Look up my purchases
              </Button>
            )}
            {savedEmail && (
              <button
                type="button"
                className="text-xs text-neutral-400 hover:text-neutral-600"
                onClick={() => {
                  try {
                    localStorage.removeItem(`${STORAGE_KEY_PREFIX}${eventId}`);
                  } catch { /* ignore */ }
                  setSavedEmail(null);
                  setShowLookup(false);
                }}
              >
                Clear
              </button>
            )}
          </div>

          {/* Email Lookup Form */}
          {showLookup && !savedEmail && (
            <form onSubmit={handleLookup} className="flex gap-2 mb-4">
              <Input
                type="email"
                value={lookupEmail}
                onChange={(e) => setLookupEmail(e.target.value)}
                placeholder="Enter the email you used to purchase"
                required
                className="flex-1"
              />
              <Button type="submit" size="sm" style={{ background: '#059669' }}>
                Look Up
              </Button>
              <Button type="button" variant="outline" size="sm" onClick={() => setShowLookup(false)}>
                Cancel
              </Button>
            </form>
          )}

          {/* Purchases List */}
          {savedEmail && myPurchasesLoading && (
            <div className="flex items-center justify-center py-4">
              <RefreshCw className="h-4 w-4 text-emerald-600 animate-spin" />
              <span className="ml-2 text-sm text-neutral-500">Loading your purchases...</span>
            </div>
          )}

          {savedEmail && !myPurchasesLoading && visiblePurchases.length === 0 && (
            <p className="text-sm text-neutral-400 py-2">
              No purchases found for {savedEmail}.
            </p>
          )}

          {savedEmail && !myPurchasesLoading && visiblePurchases.length > 0 && (
            <div className="space-y-2">
              {visiblePurchases.map((purchase) => (
                <div
                  key={purchase.id}
                  className={`flex items-center justify-between p-3 rounded-lg border ${
                    purchase.status === 'Completed'
                      ? 'bg-emerald-50/50 border-emerald-200'
                      : 'bg-amber-50/50 border-amber-200'
                  }`}
                >
                  <div className="flex items-center gap-3">
                    {purchase.status === 'Completed' ? (
                      <CheckCircle className="h-4 w-4 text-emerald-500 flex-shrink-0" />
                    ) : (
                      <Clock className="h-4 w-4 text-amber-500 flex-shrink-0" />
                    )}
                    <div>
                      <span className="text-sm font-medium text-neutral-900">
                        {purchase.addOnName}
                      </span>
                      <span className="text-xs text-neutral-500 ml-2">
                        x{purchase.quantity}
                      </span>
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className="text-sm font-medium text-neutral-700">
                      {purchase.totalAmount === 0 ? 'Free' : `$${purchase.totalAmount.toFixed(2)}`}
                    </span>
                    <span
                      className={`text-xs font-medium px-2 py-0.5 rounded-full ${
                        purchase.status === 'Completed'
                          ? 'bg-emerald-100 text-emerald-700'
                          : 'bg-amber-100 text-amber-700'
                      }`}
                    >
                      {purchase.status === 'Completed' ? 'Purchased' : 'Pending Payment'}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </CollapsibleSection>
  );
}
