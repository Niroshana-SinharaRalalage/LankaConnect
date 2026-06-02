'use client';

import { useRef, useState, useEffect } from 'react';
import { ImagePlus, X } from 'lucide-react';

const MAX_BYTES = 5 * 1024 * 1024; // 5 MB — matches backend limit
const ACCEPT = 'image/jpeg,image/png,image/webp';

/**
 * Phase 6A.151 C6 — shared sponsor-image picker. Three states per slot:
 *   1. No new file + no existing image → empty-state CTA ("Attach...")
 *   2. No new file + existing image → preview + "Replace" + "Remove"
 *   3. New file staged                → preview the new file + "Clear"
 *
 * Phase 6A.162 — gains an optional second slot for a brochure/flyer.
 * When the parent passes a `brochure` prop, the component renders TWO
 * stacked single pickers (logo on top, brochure beneath) with their own
 * independent state + handlers. When `brochure` is undefined → today's
 * single-picker behavior, byte-for-byte unchanged — zero regression for
 * callers that haven't opted in.
 *
 * Pure presentational. Owns no network state. Backend enforces the same
 * 5 MB + MIME guards; this component pre-filters so the user sees the
 * error before the network round-trip.
 */
export interface SponsorImagePickerBrochureSlot {
  value: File | null;
  onChange: (file: File | null) => void;
  existingImageUrl?: string | null;
  onRemoveExisting?: () => void;
  label?: string;
  helperText?: string;
}

interface SponsorImagePickerProps {
  /** The currently-staged new LOGO file (or null when nothing picked). */
  value: File | null;
  /** Called when the user picks or clears the logo file. */
  onChange: (file: File | null) => void;
  /** Existing logo URL on the sponsor (server-side). */
  existingImageUrl?: string | null;
  /** Called when the user clicks "Remove" on the EXISTING logo. */
  onRemoveExisting?: () => void;
  /** When true, all inputs are read-only. */
  disabled?: boolean;
  /** Field label shown above the logo picker. */
  label?: string;
  /** Inline helper text below the logo picker. */
  helperText?: string;
  /**
   * Phase 6A.162 — optional brochure/flyer slot. When provided, the
   * component renders TWO stacked pickers (logo + brochure) with
   * independent state.
   */
  brochure?: SponsorImagePickerBrochureSlot;
}

/**
 * Single-slot picker — extracted so the dual mode renders two
 * independent instances. The public SponsorImagePicker wrapper below
 * forwards its props into this internal component.
 */
function SingleSlotPicker({
  value,
  onChange,
  existingImageUrl,
  onRemoveExisting,
  disabled = false,
  label,
  helperText,
  emptyCtaLabel,
  altText,
}: {
  value: File | null;
  onChange: (file: File | null) => void;
  existingImageUrl?: string | null;
  onRemoveExisting?: () => void;
  disabled?: boolean;
  label: string;
  helperText?: string;
  emptyCtaLabel: string;
  altText: string;
}) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!value) {
      setPreviewUrl(null);
      return;
    }
    const url = URL.createObjectURL(value);
    setPreviewUrl(url);
    return () => URL.revokeObjectURL(url);
  }, [value]);

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    setError(null);
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > MAX_BYTES) {
      setError(`Image too large — max ${Math.round(MAX_BYTES / 1024 / 1024)} MB`);
      e.target.value = '';
      return;
    }
    if (!ACCEPT.split(',').includes(file.type)) {
      setError('Allowed types: JPEG, PNG, WebP');
      e.target.value = '';
      return;
    }
    onChange(file);
  };

  const handleClearStaged = () => {
    setError(null);
    onChange(null);
    if (inputRef.current) inputRef.current.value = '';
  };

  const triggerPicker = () => {
    if (disabled) return;
    inputRef.current?.click();
  };

  const showStagedPreview = previewUrl != null;
  const showExistingPreview = !showStagedPreview && !!existingImageUrl;

  return (
    <div>
      <label className="block text-xs font-medium text-neutral-600 mb-1">{label}</label>

      {/* Staged-file preview (a new file the user just picked) */}
      {showStagedPreview && (
        <div className="flex items-center gap-3 rounded-md border border-indigo-200 bg-indigo-50 p-2">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={previewUrl!}
            alt={`New ${altText} preview`}
            className="h-16 w-16 rounded object-contain bg-white"
          />
          <div className="flex-1 min-w-0">
            <p className="text-xs font-medium text-neutral-800 truncate">{value?.name}</p>
            <p className="text-xs text-neutral-500">
              {value ? `${(value.size / 1024).toFixed(0)} KB` : ''}
            </p>
          </div>
          <button
            type="button"
            onClick={handleClearStaged}
            disabled={disabled}
            className="text-neutral-500 hover:text-red-600 disabled:opacity-50"
            aria-label={`Clear staged ${altText}`}
            title="Clear"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
      )}

      {/* Existing-image preview (server-side image) */}
      {showExistingPreview && (
        <div className="flex items-center gap-3 rounded-md border border-neutral-200 bg-neutral-50 p-2">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={existingImageUrl!}
            alt={`Current sponsor ${altText}`}
            className="h-16 w-16 rounded object-contain bg-white"
          />
          <div className="flex-1 min-w-0">
            <p className="text-xs text-neutral-600">Current {altText}</p>
          </div>
          <button
            type="button"
            onClick={triggerPicker}
            disabled={disabled}
            className="text-xs text-indigo-600 hover:text-indigo-800 underline disabled:opacity-50"
          >
            Replace
          </button>
          {onRemoveExisting && (
            <button
              type="button"
              onClick={onRemoveExisting}
              disabled={disabled}
              className="text-xs text-red-600 hover:text-red-800 underline disabled:opacity-50"
            >
              Remove
            </button>
          )}
        </div>
      )}

      {/* Empty-state CTA */}
      {!showStagedPreview && !showExistingPreview && (
        <button
          type="button"
          onClick={triggerPicker}
          disabled={disabled}
          className="w-full flex items-center justify-center gap-2 rounded-md border border-dashed border-neutral-300 bg-white px-3 py-3 text-sm text-neutral-600 hover:border-indigo-300 hover:bg-indigo-50/50 hover:text-indigo-700 disabled:opacity-50"
        >
          <ImagePlus className="h-4 w-4" />
          {emptyCtaLabel}
        </button>
      )}

      <input
        ref={inputRef}
        type="file"
        accept={ACCEPT}
        className="hidden"
        onChange={handleFileSelect}
        disabled={disabled}
      />

      {error && <p className="mt-1 text-xs text-red-600">{error}</p>}
      {helperText && !error && (
        <p className="mt-1 text-xs text-neutral-500">{helperText}</p>
      )}
    </div>
  );
}

export function SponsorImagePicker({
  value,
  onChange,
  existingImageUrl,
  onRemoveExisting,
  disabled = false,
  label = 'Sponsor logo / image (optional)',
  helperText,
  brochure,
}: SponsorImagePickerProps) {
  if (!brochure) {
    // Single-picker mode: byte-identical with the pre-6A.162 behaviour.
    return (
      <SingleSlotPicker
        value={value}
        onChange={onChange}
        existingImageUrl={existingImageUrl}
        onRemoveExisting={onRemoveExisting}
        disabled={disabled}
        label={label}
        helperText={helperText}
        emptyCtaLabel="Attach a logo (JPEG/PNG/WebP · max 5 MB)"
        altText="logo"
      />
    );
  }

  // Phase 6A.162 — dual-picker mode: stacked on mobile, side-by-side on md+.
  return (
    <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
      <SingleSlotPicker
        value={value}
        onChange={onChange}
        existingImageUrl={existingImageUrl}
        onRemoveExisting={onRemoveExisting}
        disabled={disabled}
        label={label}
        helperText={helperText}
        emptyCtaLabel="Attach a logo (JPEG/PNG/WebP · max 5 MB)"
        altText="logo"
      />
      <SingleSlotPicker
        value={brochure.value}
        onChange={brochure.onChange}
        existingImageUrl={brochure.existingImageUrl}
        onRemoveExisting={brochure.onRemoveExisting}
        disabled={disabled}
        label={brochure.label ?? 'Brochure / flyer (optional)'}
        helperText={brochure.helperText}
        emptyCtaLabel="Attach a brochure (JPEG/PNG/WebP · max 5 MB)"
        altText="brochure"
      />
    </div>
  );
}
