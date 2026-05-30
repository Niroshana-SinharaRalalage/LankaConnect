'use client';

import { useEffect, useState } from 'react';
import { X, Plus, Trash2, AlertCircle } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { Label } from '@/presentation/components/ui/Label';
import {
  useCreateSponsorshipPackage,
  useUpdateSponsorshipPackage,
} from '@/presentation/hooks/useSponsorshipPackages';
import type { SponsorshipPackageDto } from '@/infrastructure/api/types/events.types';

interface SponsorshipPackageEditModalProps {
  eventId: string;
  /** Existing package to edit, or null for create-new. */
  pkg: SponsorshipPackageDto | null;
  isOpen: boolean;
  onClose: () => void;
  onSaved: () => void;
}

const MAX_PERKS = 10;
const MAX_PERK_LENGTH = 200;
const MAX_NAME_LENGTH = 200;
const MAX_DESCRIPTION_LENGTH = 1000;
const MAX_TIER_LENGTH = 100;
const MAX_INCLUDED_TICKETS = 20;

/**
 * Phase 6A.156 — modal form for creating or editing a sponsorship package.
 * Field validation mirrors the domain constants in `SponsorshipPackage.cs`
 * so the user sees friendly errors before submit; server-side validation
 * remains the source of truth.
 */
export function SponsorshipPackageEditModal({
  eventId,
  pkg,
  isOpen,
  onClose,
  onSaved,
}: SponsorshipPackageEditModalProps) {
  const isEditing = pkg != null;

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [price, setPrice] = useState('0');
  const [currency, setCurrency] = useState('USD');
  const [quantityLimit, setQuantityLimit] = useState<string>(''); // empty = unlimited
  const [sortOrder, setSortOrder] = useState('0');
  const [tier, setTier] = useState('');
  const [perks, setPerks] = useState<string[]>([]);
  const [includedTicketCount, setIncludedTicketCount] = useState('0');
  const [isActive, setIsActive] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const createMutation = useCreateSponsorshipPackage();
  const updateMutation = useUpdateSponsorshipPackage();

  // Reset form when the package or open state changes
  useEffect(() => {
    if (!isOpen) return;
    if (pkg) {
      setName(pkg.name);
      setDescription(pkg.description ?? '');
      setPrice(String(pkg.priceAmount));
      setCurrency(pkg.priceCurrency || 'USD');
      setQuantityLimit(pkg.quantityLimit != null ? String(pkg.quantityLimit) : '');
      setSortOrder(String(pkg.sortOrder));
      setTier(pkg.tier ?? '');
      setPerks([...pkg.perks]);
      setIncludedTicketCount(String(pkg.includedTicketCount));
      setIsActive(pkg.isActive);
    } else {
      setName('');
      setDescription('');
      setPrice('0');
      setCurrency('USD');
      setQuantityLimit('');
      setSortOrder('0');
      setTier('');
      setPerks([]);
      setIncludedTicketCount('0');
      setIsActive(true);
    }
    setError(null);
  }, [pkg, isOpen]);

  const addPerk = () => {
    if (perks.length >= MAX_PERKS) return;
    setPerks([...perks, '']);
  };

  const updatePerk = (idx: number, value: string) => {
    const next = [...perks];
    next[idx] = value;
    setPerks(next);
  };

  const removePerk = (idx: number) => {
    setPerks(perks.filter((_, i) => i !== idx));
  };

  const validate = (): string | null => {
    if (!name.trim()) return 'Package name is required';
    if (name.length > MAX_NAME_LENGTH) return `Name cannot exceed ${MAX_NAME_LENGTH} characters`;
    if (description.length > MAX_DESCRIPTION_LENGTH)
      return `Description cannot exceed ${MAX_DESCRIPTION_LENGTH} characters`;

    const priceNum = parseFloat(price);
    if (isNaN(priceNum) || priceNum < 0) return 'Price must be zero or positive';

    if (quantityLimit.trim()) {
      const limitNum = parseInt(quantityLimit, 10);
      if (isNaN(limitNum) || limitNum <= 0) return 'Quantity limit must be a positive integer';
      if (pkg && limitNum < pkg.quantitySold)
        return `Quantity limit (${limitNum}) cannot be less than already sold (${pkg.quantitySold})`;
    }

    if (tier.length > MAX_TIER_LENGTH) return `Tier label cannot exceed ${MAX_TIER_LENGTH} characters`;

    const ticketNum = parseInt(includedTicketCount, 10);
    if (isNaN(ticketNum) || ticketNum < 0 || ticketNum > MAX_INCLUDED_TICKETS)
      return `Included tickets must be between 0 and ${MAX_INCLUDED_TICKETS}`;

    const meaningfulPerks = perks.filter((p) => p.trim().length > 0);
    if (meaningfulPerks.length > MAX_PERKS) return `Cannot have more than ${MAX_PERKS} perks`;
    for (const perk of meaningfulPerks) {
      if (perk.length > MAX_PERK_LENGTH)
        return `Each perk cannot exceed ${MAX_PERK_LENGTH} characters`;
    }

    return null;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    const validationError = validate();
    if (validationError) {
      setError(validationError);
      return;
    }

    const meaningfulPerks = perks.map((p) => p.trim()).filter((p) => p.length > 0);
    const priceNum = parseFloat(price);
    const limitNum = quantityLimit.trim() ? parseInt(quantityLimit, 10) : null;
    const sortOrderNum = parseInt(sortOrder, 10) || 0;
    const ticketNum = parseInt(includedTicketCount, 10) || 0;

    try {
      if (isEditing && pkg) {
        await updateMutation.mutateAsync({
          eventId,
          packageId: pkg.id,
          request: {
            name: name.trim(),
            description: description.trim() || null,
            price: priceNum,
            currency,
            quantityLimit: limitNum,
            sortOrder: sortOrderNum,
            tier: tier.trim() || null,
            perks: meaningfulPerks,
            includedTicketCount: ticketNum,
            isActive,
          },
        });
      } else {
        await createMutation.mutateAsync({
          eventId,
          request: {
            name: name.trim(),
            description: description.trim() || null,
            price: priceNum,
            currency,
            quantityLimit: limitNum,
            sortOrder: sortOrderNum,
            tier: tier.trim() || null,
            perks: meaningfulPerks,
            includedTicketCount: ticketNum,
          },
        });
      }
      onSaved();
      onClose();
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Failed to save package';
      setError(msg);
    }
  };

  if (!isOpen) return null;

  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      onClick={onClose}
    >
      <div
        className="bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between p-4 border-b border-neutral-200 sticky top-0 bg-white z-10">
          <h3 className="text-lg font-semibold text-neutral-800">
            {isEditing ? 'Edit Sponsorship Package' : 'New Sponsorship Package'}
          </h3>
          <button
            type="button"
            onClick={onClose}
            className="p-1 rounded-md hover:bg-neutral-100 text-neutral-500"
            aria-label="Close"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-4 space-y-4">
          {error && (
            <div className="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-md text-sm text-red-700">
              <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}

          {/* Name + Tier */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <div className="md:col-span-2">
              <Label htmlFor="pkg-name">Package Name *</Label>
              <Input
                id="pkg-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="e.g. Gold Sponsor"
                maxLength={MAX_NAME_LENGTH}
                required
              />
            </div>
            <div>
              <Label htmlFor="pkg-tier">Tier Label</Label>
              <Input
                id="pkg-tier"
                value={tier}
                onChange={(e) => setTier(e.target.value)}
                placeholder="e.g. Gold"
                maxLength={MAX_TIER_LENGTH}
              />
            </div>
          </div>

          {/* Description */}
          <div>
            <Label htmlFor="pkg-description">Description</Label>
            <textarea
              id="pkg-description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={2}
              maxLength={MAX_DESCRIPTION_LENGTH}
              className="w-full px-3 py-2 text-sm border border-neutral-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="Optional — shown on the package card"
            />
          </div>

          {/* Price + Quantity Limit + Sort Order */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <div>
              <Label htmlFor="pkg-price">Price *</Label>
              <Input
                id="pkg-price"
                type="number"
                step="0.01"
                min="0"
                value={price}
                onChange={(e) => setPrice(e.target.value)}
                required
              />
            </div>
            <div>
              <Label htmlFor="pkg-qty">Quantity Limit</Label>
              <Input
                id="pkg-qty"
                type="number"
                min="1"
                value={quantityLimit}
                onChange={(e) => setQuantityLimit(e.target.value)}
                placeholder="Unlimited"
              />
            </div>
            <div>
              <Label htmlFor="pkg-sort">Sort Order</Label>
              <Input
                id="pkg-sort"
                type="number"
                value={sortOrder}
                onChange={(e) => setSortOrder(e.target.value)}
              />
            </div>
          </div>

          {/* Included Tickets */}
          <div>
            <Label htmlFor="pkg-tickets">
              Included Tickets (0 = perks only, max {MAX_INCLUDED_TICKETS})
            </Label>
            <Input
              id="pkg-tickets"
              type="number"
              min="0"
              max={MAX_INCLUDED_TICKETS}
              value={includedTicketCount}
              onChange={(e) => setIncludedTicketCount(e.target.value)}
            />
            <p className="text-xs text-neutral-500 mt-1">
              Bundled-ticket allocation lands in a future phase. Field is persisted now so
              your selections survive the upgrade.
            </p>
          </div>

          {/* Perks */}
          <div>
            <div className="flex items-center justify-between mb-2">
              <Label>Perks ({perks.length} / {MAX_PERKS})</Label>
              <Button
                type="button"
                size="sm"
                variant="outline"
                onClick={addPerk}
                disabled={perks.length >= MAX_PERKS}
              >
                <Plus className="h-3.5 w-3.5 mr-1" />
                Add Perk
              </Button>
            </div>
            {perks.length === 0 ? (
              <p className="text-xs text-neutral-400 italic">No perks added yet.</p>
            ) : (
              <ul className="space-y-2">
                {perks.map((perk, idx) => (
                  <li key={idx} className="flex items-center gap-2">
                    <Input
                      value={perk}
                      onChange={(e) => updatePerk(idx, e.target.value)}
                      maxLength={MAX_PERK_LENGTH}
                      placeholder="e.g. Logo on event banner"
                    />
                    <Button
                      type="button"
                      size="sm"
                      variant="ghost"
                      onClick={() => removePerk(idx)}
                      className="text-red-600 hover:bg-red-50"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  </li>
                ))}
              </ul>
            )}
          </div>

          {/* Active toggle (edit-mode only — new packages are always created active) */}
          {isEditing && (
            <div className="flex items-start gap-2 pt-2 border-t border-neutral-200">
              <input
                type="checkbox"
                id="pkg-active"
                checked={isActive}
                onChange={(e) => setIsActive(e.target.checked)}
                className="mt-1 h-4 w-4 rounded border-neutral-300"
              />
              <label htmlFor="pkg-active" className="text-sm text-neutral-700">
                Active (uncheck to soft-deactivate)
              </label>
            </div>
          )}

          <div className="flex items-center justify-end gap-2 pt-4 border-t border-neutral-200">
            <Button type="button" variant="ghost" onClick={onClose} disabled={isSubmitting}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Saving...' : isEditing ? 'Save Changes' : 'Create Package'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
