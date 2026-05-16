'use client';

import { useState, useMemo } from 'react';
import { ShoppingBag, RefreshCw, ShoppingCart, Plus, Minus, Trash2, Package, CheckCircle, Clock } from 'lucide-react';
import { CollapsibleSection } from '@/presentation/components/ui/CollapsibleSection';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { Card, CardContent } from '@/presentation/components/ui/Card';
import { useAddOnDefinitions, usePurchaseAddOnCart } from '@/presentation/hooks/useAddOns';
import type { AddOnConfigurationDto, AddOnDefinitionDto, AddOnPurchaseDto } from '@/infrastructure/api/types/events.types';

interface CartItem {
  definitionId: string;
  quantity: number;
}

interface AddOnSelectorProps {
  eventId: string;
  addOnConfig: AddOnConfigurationDto;
  myAddOnPurchases?: AddOnPurchaseDto[];
}

/**
 * Public-facing add-on selector with cart support.
 * Users can add quantities to multiple add-ons and checkout all at once.
 * Free items complete immediately; paid items go to a single Stripe checkout.
 * Shows "Your Add-Ons" for logged-in users (auth-based, like "Your Sponsorships").
 */
export function AddOnSelector({ eventId, addOnConfig, myAddOnPurchases }: AddOnSelectorProps) {
  const [cart, setCart] = useState<CartItem[]>([]);
  const [showCheckout, setShowCheckout] = useState(false);
  const [buyerName, setBuyerName] = useState('');
  const [buyerEmail, setBuyerEmail] = useState('');
  const [buyerPhone, setBuyerPhone] = useState('');
  const [error, setError] = useState<string | null>(null);

  const { data: definitions, isLoading, isError, refetch } = useAddOnDefinitions(eventId);
  const purchaseCart = usePurchaseAddOnCart();

  // Filter active definitions and sort by sortOrder
  const activeDefinitions = useMemo(
    () =>
      (definitions ?? [])
        .filter((d) => d.isActive)
        .sort((a, b) => a.sortOrder - b.sortOrder),
    [definitions]
  );

  // Phase 6A.145 Commit 6 — section is default-collapsed like all other event-page
  // collapsibles. The top-of-page <AddOnsPreviewStrip> now surfaces the add-ons
  // prominently, so the bottom full-detail card is just a "Show details" target
  // for the pill navigation (architect H-1 + user UAT R6 request).
  const [sectionOpen, setSectionOpen] = useState(false);

  // Filter to only show completed/pending purchases (not abandoned/failed)
  const visiblePurchases = useMemo(() => {
    if (!myAddOnPurchases) return [];
    return myAddOnPurchases.filter((p) => p.status === 'Completed' || p.status === 'Pending');
  }, [myAddOnPurchases]);

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

      if (checkoutUrl) {
        window.location.href = checkoutUrl;
      }
    } catch (err: any) {
      setError(err?.response?.data?.detail || 'Failed to process purchase. Please try again.');
    }
  };

  return (
    <CollapsibleSection
      title="Event Add-Ons"
      icon={<ShoppingBag className="h-5 w-5 text-emerald-600" />}
      description={addOnConfig.addOnMessage || undefined}
      open={sectionOpen}
      onOpenChange={setSectionOpen}
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
                    {/* Phase 6A.143 — left-side 64x64 thumbnail with Package icon
                        fallback so card layout stays consistent across images-vs-no-images. */}
                    <div className="flex items-start gap-3">
                      {definition.imageUrl ? (
                        /* eslint-disable-next-line @next/next/no-img-element */
                        <img
                          src={definition.imageUrl}
                          alt={definition.name}
                          className="h-16 w-16 flex-shrink-0 rounded border border-neutral-200 object-cover bg-white"
                        />
                      ) : (
                        <div className="flex h-16 w-16 flex-shrink-0 items-center justify-center rounded border border-neutral-200 bg-neutral-50 text-neutral-300">
                          <Package className="h-6 w-6" />
                        </div>
                      )}
                      <div className="flex-1 min-w-0">
                        {/* Name */}
                        <h4 className="font-semibold text-neutral-900">{definition.name}</h4>

                        {/* Description */}
                        {definition.description && (
                          <p className="text-sm text-neutral-500 mt-1 line-clamp-2">
                            {definition.description}
                          </p>
                        )}
                      </div>
                    </div>

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

      {/* ─── Your Add-Ons (auth-based, like "Your Sponsorships") ─── */}
      {myAddOnPurchases && visiblePurchases.length > 0 && (
        <div className="mt-4 pt-4 border-t border-neutral-200">
          <h4 className="text-sm font-semibold text-neutral-700 mb-2">Your Add-Ons</h4>
          <div className="space-y-2">
            {visiblePurchases.map((purchase) => (
              <div
                key={purchase.id}
                className="flex items-center justify-between py-2 px-3 bg-white rounded border border-emerald-100"
              >
                <div className="flex items-center gap-3">
                  {purchase.status === 'Completed' ? (
                    <CheckCircle className="h-4 w-4 text-emerald-500 flex-shrink-0" />
                  ) : (
                    <Clock className="h-4 w-4 text-amber-500 flex-shrink-0" />
                  )}
                  <span className="text-sm font-semibold text-neutral-900">
                    {purchase.addOnName}
                  </span>
                  <span className="text-xs text-neutral-500">
                    x{purchase.quantity}
                  </span>
                  <span className="text-sm font-semibold text-neutral-900">
                    {purchase.totalAmount === 0 ? 'Free' : `$${(purchase.totalAmount ?? 0).toFixed(2)}`}
                  </span>
                </div>
                <div className="flex items-center gap-3">
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                    purchase.status === 'Completed' ? 'bg-green-100 text-green-700'
                      : purchase.status === 'Pending' ? 'bg-yellow-100 text-yellow-700'
                      : 'bg-neutral-100 text-neutral-600'
                  }`}>
                    {purchase.status}
                  </span>
                  <span className="text-xs text-neutral-500">
                    {new Date(purchase.createdAt).toLocaleDateString()}
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </CollapsibleSection>
  );
}
