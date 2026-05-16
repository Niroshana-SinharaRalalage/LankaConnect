'use client';

import { useRef, useState } from 'react';
import { ImagePlus, X, DollarSign, Package } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { useCreateOffPlatformSponsor } from '@/presentation/hooks/useSponsors';
import type {
  CreateOffPlatformSponsorRequest,
  SponsorConfigurationDto,
} from '@/infrastructure/api/types/events.types';

interface AddOffPlatformSponsorModalProps {
  eventId: string;
  sponsorConfig?: SponsorConfigurationDto | null;
  open: boolean;
  onClose: () => void;
  onCreated?: (sponsorId: string) => void;
}

/**
 * Phase 6A.145 — modal for the organizer to record an off-platform sponsorship
 * (cash money or in-kind item collected outside the platform). Bypasses Stripe.
 * Dual-mode toggle mirrors the public SponsorSection. Optional image upload —
 * organizer always bypasses the SponsorConfig.MinAmountForSponsorImage threshold
 * (architect E-1 override), so the field is always available.
 */
export function AddOffPlatformSponsorModal({
  eventId,
  sponsorConfig,
  open,
  onClose,
  onCreated,
}: AddOffPlatformSponsorModalProps) {
  const [type, setType] = useState<'Money' | 'Item'>('Money');
  const [sponsorName, setSponsorName] = useState('');
  const [sponsorEmail, setSponsorEmail] = useState('');
  const [sponsorPhone, setSponsorPhone] = useState('');
  const [sponsorOrganization, setSponsorOrganization] = useState('');
  const [sponsorNotes, setSponsorNotes] = useState('');
  // Money
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState('USD');
  // Item
  const [itemName, setItemName] = useState('');
  const [itemDescription, setItemDescription] = useState('');
  const [estimatedValue, setEstimatedValue] = useState('');
  // Image
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const mutation = useCreateOffPlatformSponsor();
  const acceptMoney = sponsorConfig?.acceptMoneySponsors !== false;
  const acceptItem = sponsorConfig?.acceptItemSponsors !== false;

  const resetForm = () => {
    setType('Money');
    setSponsorName('');
    setSponsorEmail('');
    setSponsorPhone('');
    setSponsorOrganization('');
    setSponsorNotes('');
    setAmount('');
    setCurrency('USD');
    setItemName('');
    setItemDescription('');
    setEstimatedValue('');
    setImageFile(null);
    setError(null);
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  const handleClose = () => {
    if (mutation.isPending) return;
    resetForm();
    onClose();
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!sponsorName.trim()) return setError('Sponsor name is required.');
    if (!sponsorEmail.trim()) return setError('Sponsor email is required.');

    const payload: CreateOffPlatformSponsorRequest = {
      type,
      sponsorName: sponsorName.trim(),
      sponsorEmail: sponsorEmail.trim(),
      sponsorPhone: sponsorPhone.trim() || null,
      sponsorOrganization: sponsorOrganization.trim() || null,
      sponsorNotes: sponsorNotes.trim() || null,
      image: imageFile,
    };

    if (type === 'Money') {
      const parsedAmount = parseFloat(amount);
      if (isNaN(parsedAmount) || parsedAmount <= 0)
        return setError('Amount must be greater than $0.');
      payload.amount = parsedAmount;
      payload.currency = currency;
    } else {
      if (!itemName.trim()) return setError('Item name is required.');
      payload.itemName = itemName.trim();
      payload.itemDescription = itemDescription.trim() || null;
      const parsedValue = parseFloat(estimatedValue);
      payload.estimatedValue = isNaN(parsedValue) ? null : parsedValue;
    }

    if (imageFile && imageFile.size > 5 * 1024 * 1024) {
      return setError('Image too large (max 5MB).');
    }

    try {
      const result = await mutation.mutateAsync({ eventId, request: payload });
      onCreated?.(result.sponsorId);
      resetForm();
      onClose();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to record sponsorship.';
      console.error('CreateOffPlatformSponsor failed:', err);
      setError(msg);
    }
  };

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      data-testid="off-platform-sponsor-modal"
    >
      <div className="w-full max-w-2xl rounded-lg bg-white shadow-xl max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between border-b border-neutral-200 p-4">
          <h2 className="text-lg font-semibold text-neutral-900">Record Off-Platform Sponsorship</h2>
          <button
            type="button"
            onClick={handleClose}
            disabled={mutation.isPending}
            className="text-neutral-400 hover:text-neutral-600"
            aria-label="Close"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4 p-4">
          <p className="text-sm text-neutral-600">
            Use this form when a sponsor pays you directly (cash) or donates an item
            without going through the platform. The sponsorship is recorded as Completed
            immediately — no Stripe involved.
          </p>

          {/* Type toggle */}
          {(acceptMoney || acceptItem) && (
            <div className="flex rounded-lg bg-neutral-100 p-1">
              {acceptMoney && (
                <button
                  type="button"
                  onClick={() => setType('Money')}
                  className={`flex-1 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                    type === 'Money'
                      ? 'bg-white text-emerald-700 shadow-sm'
                      : 'text-neutral-600 hover:text-emerald-600'
                  }`}
                >
                  <DollarSign className="inline h-4 w-4 mr-1" /> Cash / Money
                </button>
              )}
              {acceptItem && (
                <button
                  type="button"
                  onClick={() => setType('Item')}
                  className={`flex-1 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                    type === 'Item'
                      ? 'bg-white text-indigo-700 shadow-sm'
                      : 'text-neutral-600 hover:text-indigo-600'
                  }`}
                >
                  <Package className="inline h-4 w-4 mr-1" /> In-Kind Item
                </button>
              )}
            </div>
          )}

          {/* Common fields */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-neutral-600 mb-1">Sponsor name *</label>
              <Input value={sponsorName} onChange={(e) => setSponsorName(e.target.value)} placeholder="e.g. Papa Johns" />
            </div>
            <div>
              <label className="block text-xs font-medium text-neutral-600 mb-1">Sponsor email *</label>
              <Input type="email" value={sponsorEmail} onChange={(e) => setSponsorEmail(e.target.value)} placeholder="contact@example.com" />
            </div>
            <div>
              <label className="block text-xs font-medium text-neutral-600 mb-1">Phone (optional)</label>
              <Input value={sponsorPhone} onChange={(e) => setSponsorPhone(e.target.value)} placeholder="+1 555 1234" />
            </div>
            <div>
              <label className="block text-xs font-medium text-neutral-600 mb-1">Organization (optional)</label>
              <Input value={sponsorOrganization} onChange={(e) => setSponsorOrganization(e.target.value)} placeholder="Papa Johns Pizza" />
            </div>
          </div>

          {/* Type-specific fields */}
          {type === 'Money' ? (
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
              <div className="sm:col-span-2">
                <label className="block text-xs font-medium text-neutral-600 mb-1">Amount *</label>
                <div className="relative">
                  <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500">$</span>
                  <Input type="number" min="0.01" step="0.01" value={amount} onChange={(e) => setAmount(e.target.value)} placeholder="500.00" className="pl-7" />
                </div>
              </div>
              <div>
                <label className="block text-xs font-medium text-neutral-600 mb-1">Currency</label>
                <select
                  value={currency}
                  onChange={(e) => setCurrency(e.target.value)}
                  className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
                >
                  <option value="USD">USD</option>
                  <option value="EUR">EUR</option>
                  <option value="GBP">GBP</option>
                  <option value="CAD">CAD</option>
                  <option value="AUD">AUD</option>
                  <option value="LKR">LKR</option>
                </select>
              </div>
            </div>
          ) : (
            <div className="space-y-3">
              <div>
                <label className="block text-xs font-medium text-neutral-600 mb-1">Item name *</label>
                <Input value={itemName} onChange={(e) => setItemName(e.target.value)} placeholder="e.g. 50 pizzas" />
              </div>
              <div>
                <label className="block text-xs font-medium text-neutral-600 mb-1">Description (optional)</label>
                <textarea
                  value={itemDescription}
                  onChange={(e) => setItemDescription(e.target.value)}
                  rows={2}
                  className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-neutral-600 mb-1">Estimated value (optional)</label>
                <div className="relative max-w-xs">
                  <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500">$</span>
                  <Input type="number" min="0" step="0.01" value={estimatedValue} onChange={(e) => setEstimatedValue(e.target.value)} placeholder="500.00" className="pl-7" />
                </div>
              </div>
            </div>
          )}

          {/* Notes */}
          <div>
            <label className="block text-xs font-medium text-neutral-600 mb-1">Notes (optional)</label>
            <textarea
              value={sponsorNotes}
              onChange={(e) => setSponsorNotes(e.target.value)}
              rows={2}
              placeholder="Internal notes, e.g. 'Cash delivered on event day'"
              className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          {/* Image upload — organizer always bypasses threshold */}
          <div>
            <label className="block text-xs font-medium text-neutral-600 mb-1">Logo / image (optional)</label>
            <div className="flex items-center gap-3">
              {imageFile ? (
                <div className="flex items-center gap-2 rounded border border-neutral-200 px-3 py-2 text-sm bg-neutral-50">
                  <ImagePlus className="h-4 w-4 text-neutral-400" />
                  <span className="text-neutral-700 truncate max-w-[200px]">{imageFile.name}</span>
                  <button
                    type="button"
                    onClick={() => {
                      setImageFile(null);
                      if (fileInputRef.current) fileInputRef.current.value = '';
                    }}
                    className="text-red-500 hover:text-red-700"
                    aria-label="Remove image"
                  >
                    <X className="h-4 w-4" />
                  </button>
                </div>
              ) : (
                <Button type="button" variant="outline" size="sm" onClick={() => fileInputRef.current?.click()}>
                  <ImagePlus className="h-4 w-4 mr-1" /> Choose image
                </Button>
              )}
              <input
                ref={fileInputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp,image/gif"
                className="hidden"
                onChange={(e) => setImageFile(e.target.files?.[0] ?? null)}
              />
            </div>
            <p className="text-[10px] text-neutral-400 mt-0.5">JPG / PNG / WebP / GIF, max 5MB.</p>
          </div>

          {error && (
            <div className="rounded border border-red-200 bg-red-50 p-2 text-sm text-red-700">
              {error}
            </div>
          )}

          <div className="flex justify-end gap-2 border-t border-neutral-200 pt-3">
            <Button type="button" variant="outline" onClick={handleClose} disabled={mutation.isPending}>
              Cancel
            </Button>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? 'Saving...' : 'Record Sponsorship'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
