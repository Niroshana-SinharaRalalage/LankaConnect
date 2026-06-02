'use client';

import { useRef, useState } from 'react';
import { Plus, Award, RefreshCw, AlertCircle } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { Card, CardContent } from '@/presentation/components/ui/Card';
import {
  useSponsorshipPackages,
  useDeleteSponsorshipPackage,
  useUploadSponsorshipPackageImage,
  useDeleteSponsorshipPackageImage,
} from '@/presentation/hooks/useSponsorshipPackages';
import { SponsorshipPackageCard } from './SponsorshipPackageCard';
import { SponsorshipPackageEditModal } from './SponsorshipPackageEditModal';
import type { SponsorshipPackageDto } from '@/infrastructure/api/types/events.types';

/**
 * Phase 6A.156-fix — a pending sponsorship package (not yet saved to backend).
 * Used in local mode when the event doesn't exist yet (create flow). Mirrors
 * {@link PendingAddOnDefinition} shape so reviewers familiar with the add-on
 * inline-editor pattern can navigate this code unchanged.
 *
 * `localId` is a client-side UUID used only for React keys and array
 * dispatch; it is dropped at submit time when the editor's parent loops over
 * pendingPackages to POST each one to the backend.
 */
export interface PendingSponsorshipPackage {
  localId: string;
  name: string;
  description: string | null;
  price: number;
  currency: string;
  quantityLimit: number | null;
  sortOrder: number;
  tier: string | null;
  perks: string[];
  includedTicketCount: number;
  isActive: boolean;
}

interface SponsorshipPackageEditorProps {
  /** When provided, editor operates in live mode (API calls via React Query). */
  eventId?: string;
  /** Local mode: pending packages managed by parent. */
  pendingPackages?: PendingSponsorshipPackage[];
  onPendingPackagesChange?: (packages: PendingSponsorshipPackage[]) => void;
}

/**
 * Phase 6A.156-fix — dual-mode editor for organizer-defined sponsorship
 * packages (Gold/Silver/Bronze tiers). Mirrors {@link AddOnDefinitionEditor}
 * byte-for-byte where it can:
 *
 * <ul>
 *   <li><b>Live mode</b> (eventId provided): fetches via
 *       <code>useSponsorshipPackages</code>, mutates via the live React Query
 *       hooks. Used inside {@link SponsorsManagementTab} (post-creation) and
 *       {@link SponsorConfigForm} when called from
 *       {@link EventEditForm}.</li>
 *   <li><b>Local mode</b> (no eventId): manages a
 *       {@link PendingSponsorshipPackage}[] in parent state via the
 *       <code>onPendingPackagesChange</code> callback. Used inside
 *       {@link SponsorConfigForm} when called from
 *       {@link EventCreationForm} (event doesn't exist yet, so the parent
 *       form persists the queue after event creation).</li>
 * </ul>
 *
 * <p>Image upload is <b>live-mode only</b> — the API requires a real
 * packageId. New packages in local mode show a "Save first" hint inside the
 * modal (mirrors {@link AddOnDefinitionEditor} behaviour at
 * <code>AddOnDefinitionEditor.tsx:528-532</code>).</p>
 *
 * <p>Per CLAUDE.md observability rule: every async mutation is wrapped in
 * try/catch with <code>console.error</code> + user-facing fallback.</p>
 */
export function SponsorshipPackageEditor({
  eventId,
  pendingPackages,
  onPendingPackagesChange,
}: SponsorshipPackageEditorProps) {
  const isLiveMode = !!eventId;

  // Live-mode hooks. React's rules-of-hooks require these to be called
  // unconditionally, so the query is always declared — but it self-gates
  // (enabled=false when no eventId, so no network call fires in local mode).
  // Mutations are declared eagerly; their mutateAsync is only called from the
  // live-mode branches below.
  const liveQuery = useSponsorshipPackages(eventId, isLiveMode);
  const liveDeleteMutation = useDeleteSponsorshipPackage();
  const liveUploadImageMutation = useUploadSponsorshipPackageImage();
  const liveClearImageMutation = useDeleteSponsorshipPackageImage();

  const [modalOpen, setModalOpen] = useState(false);
  const [editingPackage, setEditingPackage] = useState<SponsorshipPackageDto | null>(null);
  const [uploadingPackageId, setUploadingPackageId] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // ──────────────────────────────────────────────
  // Unified package list (live DTOs OR pending-to-DTO projection)
  // ──────────────────────────────────────────────
  //
  // We render the same SponsorshipPackageCard in both modes. Local mode
  // projects each PendingSponsorshipPackage into a DTO-shaped object so the
  // card component is mode-agnostic. quantitySold/createdAt/updatedAt are
  // synthesized with safe defaults (0 / now / null) since the package
  // doesn't exist on the backend yet.
  const packages: SponsorshipPackageDto[] = isLiveMode
    ? (liveQuery.data ?? [])
    : (pendingPackages ?? []).map((p) => ({
        id: p.localId,
        eventId: '',
        name: p.name,
        description: p.description,
        priceAmount: p.price,
        priceCurrency: p.currency,
        quantityLimit: p.quantityLimit,
        quantitySold: 0,
        remainingStock: p.quantityLimit,
        isActive: p.isActive,
        sortOrder: p.sortOrder,
        imageUrl: null,
        imageBlobName: null,
        tier: p.tier,
        perks: p.perks,
        includedTicketCount: p.includedTicketCount,
        createdAt: new Date().toISOString(),
        updatedAt: null,
      }));

  // ──────────────────────────────────────────────
  // Handlers
  // ──────────────────────────────────────────────

  const handleAddNew = () => {
    setEditingPackage(null);
    setModalOpen(true);
  };

  const handleEdit = (pkg: SponsorshipPackageDto) => {
    setEditingPackage(pkg);
    setModalOpen(true);
  };

  const handleDelete = async (pkg: SponsorshipPackageDto) => {
    if (isLiveMode && eventId) {
      const confirmMsg =
        pkg.quantitySold > 0
          ? `"${pkg.name}" has ${pkg.quantitySold} active sponsor(s). It will be deactivated (soft-deleted) to preserve historical records. Continue?`
          : `Delete "${pkg.name}"? This cannot be undone.`;
      if (!window.confirm(confirmMsg)) return;
      try {
        await liveDeleteMutation.mutateAsync({ eventId, packageId: pkg.id });
      } catch (err) {
        console.error('Failed to delete sponsorship package:', err);
        window.alert('Failed to delete the package. Please try again.');
      }
    } else {
      // Local mode: drop from pending list (no API call, no confirm — the
      // organiser hasn't committed the event yet, so the package never existed
      // server-side).
      const remaining = (pendingPackages ?? []).filter((p) => p.localId !== pkg.id);
      onPendingPackagesChange?.(remaining);
    }
  };

  const handleImageUpload = (pkg: SponsorshipPackageDto) => {
    // Image upload is live-mode only — local-mode users get the same hint as
    // the add-on flow ("Save first, then upload on next edit"). The button
    // still works visually but the click path no-ops here.
    if (!isLiveMode) {
      window.alert('Save the event first — you can upload an image after the package is created.');
      return;
    }
    setUploadingPackageId(pkg.id);
    fileInputRef.current?.click();
  };

  const handleFileSelected = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = ''; // reset so the same file can be re-selected
    if (!file || !uploadingPackageId || !eventId) {
      setUploadingPackageId(null);
      return;
    }
    try {
      await liveUploadImageMutation.mutateAsync({
        eventId,
        packageId: uploadingPackageId,
        file,
      });
    } catch (err) {
      console.error('Failed to upload package image:', err);
      window.alert('Failed to upload the image. Please try again.');
    } finally {
      setUploadingPackageId(null);
    }
  };

  const handleImageClear = async (pkg: SponsorshipPackageDto) => {
    if (!isLiveMode || !eventId) return; // No image in local mode → nothing to clear
    if (!window.confirm(`Remove the image from "${pkg.name}"?`)) return;
    try {
      await liveClearImageMutation.mutateAsync({ eventId, packageId: pkg.id });
    } catch (err) {
      console.error('Failed to clear package image:', err);
      window.alert('Failed to remove the image. Please try again.');
    }
  };

  /**
   * Local-mode save handler passed to the modal as onSubmitOverride.
   * Translates the modal's form payload into a PendingSponsorshipPackage
   * (create) or applies the diff to an existing one (edit). Always wraps with
   * onPendingPackagesChange so React Query / live mutations stay completely
   * out of the local-mode code path.
   */
  const handleLocalSubmit = async (request: {
    name: string;
    description?: string | null;
    price: number;
    currency?: string;
    quantityLimit?: number | null;
    sortOrder: number;
    tier?: string | null;
    perks?: string[];
    includedTicketCount: number;
    isActive: boolean;
  }) => {
    const current = pendingPackages ?? [];
    if (editingPackage) {
      // Update — preserve the localId, replace all editable fields.
      const updated = current.map((p) =>
        p.localId === editingPackage.id
          ? {
              ...p,
              name: request.name,
              description: request.description ?? null,
              price: request.price,
              currency: request.currency ?? p.currency,
              quantityLimit: request.quantityLimit ?? null,
              sortOrder: request.sortOrder,
              tier: request.tier ?? null,
              perks: request.perks ?? [],
              includedTicketCount: request.includedTicketCount,
              isActive: request.isActive,
            }
          : p
      );
      onPendingPackagesChange?.(updated);
    } else {
      // Create — mint a localId for React keys + parent dispatch.
      const newPending: PendingSponsorshipPackage = {
        localId:
          typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function'
            ? crypto.randomUUID()
            : `pending-${Date.now()}-${Math.floor(Math.random() * 1e9)}`,
        name: request.name,
        description: request.description ?? null,
        price: request.price,
        currency: request.currency ?? 'USD',
        quantityLimit: request.quantityLimit ?? null,
        sortOrder: request.sortOrder,
        tier: request.tier ?? null,
        perks: request.perks ?? [],
        includedTicketCount: request.includedTicketCount,
        isActive: request.isActive,
      };
      onPendingPackagesChange?.([...current, newPending]);
    }
  };

  // ──────────────────────────────────────────────
  // Render
  // ──────────────────────────────────────────────

  if (isLiveMode && liveQuery.isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <RefreshCw className="h-6 w-6 animate-spin text-neutral-400" />
        <span className="ml-2 text-neutral-500">Loading packages...</span>
      </div>
    );
  }

  if (isLiveMode && liveQuery.error) {
    return (
      <div className="flex items-start gap-2 p-4 bg-red-50 border border-red-200 rounded-md text-sm text-red-700">
        <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
        <div>
          <p className="font-medium">Failed to load sponsorship packages.</p>
          <Button type="button" size="sm" variant="ghost" onClick={() => liveQuery.refetch()} className="mt-1">
            <RefreshCw className="h-3.5 w-3.5 mr-1" />
            Retry
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Header — count is rendered in the title for parity with AddOn editor */}
      <div className="flex items-center justify-between gap-2">
        <div>
          <h3 className="text-base font-semibold text-neutral-800 flex items-center gap-2">
            <Award className="h-5 w-5 text-amber-500" />
            Sponsorship Packages ({packages.length})
          </h3>
          <p className="text-xs text-neutral-500 mt-0.5">
            Define tiered sponsorship offerings (Gold / Silver / Bronze) with perks and
            optional bundled tickets. Buyer-facing purchase lands in a future phase.
          </p>
        </div>
        <Button type="button" onClick={handleAddNew} size="sm">
          <Plus className="h-4 w-4 mr-1" />
          Add Package
        </Button>
      </div>

      {/* Empty state */}
      {packages.length === 0 ? (
        <Card>
          <CardContent className="py-12">
            <div className="flex flex-col items-center justify-center text-center">
              <Award className="h-10 w-10 text-neutral-300 mb-3" />
              <h4 className="text-sm font-medium text-neutral-700 mb-1">
                No sponsorship packages yet
              </h4>
              <p className="text-xs text-neutral-500 max-w-md mb-4">
                Start by creating your first tier. Most events use Gold / Silver / Bronze,
                but tier labels are free-text — name them whatever fits your event.
              </p>
              <Button type="button" onClick={handleAddNew} size="sm">
                <Plus className="h-4 w-4 mr-1" />
                Create your first package
              </Button>
            </div>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {packages.map((pkg) => (
            <SponsorshipPackageCard
              key={pkg.id}
              pkg={pkg}
              onEdit={handleEdit}
              onDelete={handleDelete}
              onImageUpload={handleImageUpload}
              onImageClear={handleImageClear}
            />
          ))}
        </div>
      )}

      {/* Hidden file input for image uploads (live mode only) */}
      <input
        ref={fileInputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        className="hidden"
        onChange={handleFileSelected}
      />

      {/* Edit/create modal — same modal both modes, dispatched via onSubmitOverride
          when in local mode so the modal never touches the live mutations. */}
      <SponsorshipPackageEditModal
        eventId={eventId ?? ''}
        pkg={editingPackage}
        isOpen={modalOpen}
        onClose={() => setModalOpen(false)}
        onSaved={() => {
          /* React Query invalidation happens inside the live hooks — no extra refetch
             needed in live mode. Local mode wires onSubmitOverride to mutate parent
             state directly, so no refetch is meaningful there either. */
        }}
        onSubmitOverride={isLiveMode ? undefined : handleLocalSubmit}
      />
    </div>
  );
}
