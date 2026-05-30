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

interface SponsorshipPackagesManagementSectionProps {
  eventId: string;
  /** Pass true when the parent has confirmed packages are enabled in sponsor config. */
  enabled: boolean;
}

/**
 * Phase 6A.156 — organizer-facing CRUD surface for sponsorship packages.
 *
 * Renders inside the AttendeesAndFinance tab as a new "Packages" sub-tab.
 * The buyer-facing surface (mounted inside the existing SponsorSection on
 * the public event page per user direction) lands in 6A.157.
 *
 * UX flow: header with "Add Package" CTA → grid of package cards. Each card
 * handles edit / delete / image-upload / image-clear via shared hooks.
 * Empty state guides the organizer when there are no packages yet.
 */
export function SponsorshipPackagesManagementSection({
  eventId,
  enabled,
}: SponsorshipPackagesManagementSectionProps) {
  const { data: packages, isLoading, error, refetch } = useSponsorshipPackages(eventId, enabled);
  const deleteMutation = useDeleteSponsorshipPackage();
  const uploadImageMutation = useUploadSponsorshipPackageImage();
  const clearImageMutation = useDeleteSponsorshipPackageImage();

  const [modalOpen, setModalOpen] = useState(false);
  const [editingPackage, setEditingPackage] = useState<SponsorshipPackageDto | null>(null);
  const [uploadingPackageId, setUploadingPackageId] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  if (!enabled) {
    return (
      <Card>
        <CardContent className="py-12">
          <div className="flex flex-col items-center justify-center text-center">
            <div className="w-14 h-14 rounded-full bg-amber-50 flex items-center justify-center mb-4">
              <Award className="h-7 w-7 text-amber-500" />
            </div>
            <h3 className="text-base font-semibold text-neutral-800 mb-2">
              Sponsorship Packages Not Enabled
            </h3>
            <p className="text-sm text-neutral-500 max-w-md mb-3">
              Turn on <strong>Enable sponsorship packages</strong> in your event&apos;s
              Sponsorship settings to start defining tiers (Gold / Silver / Bronze).
            </p>
            <p className="text-xs text-neutral-400 max-w-md">
              Packages let you sell curated sponsorship tiers (with perks and bundled
              tickets) alongside the existing custom-amount sponsorship flow.
            </p>
          </div>
        </CardContent>
      </Card>
    );
  }

  const handleAddNew = () => {
    setEditingPackage(null);
    setModalOpen(true);
  };

  const handleEdit = (pkg: SponsorshipPackageDto) => {
    setEditingPackage(pkg);
    setModalOpen(true);
  };

  const handleDelete = async (pkg: SponsorshipPackageDto) => {
    const confirmMsg =
      pkg.quantitySold > 0
        ? `"${pkg.name}" has ${pkg.quantitySold} active sponsor(s). It will be deactivated (soft-deleted) to preserve historical records. Continue?`
        : `Delete "${pkg.name}"? This cannot be undone.`;

    if (!window.confirm(confirmMsg)) return;

    try {
      await deleteMutation.mutateAsync({ eventId, packageId: pkg.id });
    } catch (err) {
      console.error('Failed to delete sponsorship package:', err);
      window.alert('Failed to delete the package. Please try again.');
    }
  };

  const handleImageUpload = (pkg: SponsorshipPackageDto) => {
    setUploadingPackageId(pkg.id);
    fileInputRef.current?.click();
  };

  const handleFileSelected = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = ''; // reset so the same file can be re-selected
    if (!file || !uploadingPackageId) {
      setUploadingPackageId(null);
      return;
    }

    try {
      await uploadImageMutation.mutateAsync({
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
    if (!window.confirm(`Remove the image from "${pkg.name}"?`)) return;
    try {
      await clearImageMutation.mutateAsync({ eventId, packageId: pkg.id });
    } catch (err) {
      console.error('Failed to clear package image:', err);
      window.alert('Failed to remove the image. Please try again.');
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <RefreshCw className="h-6 w-6 animate-spin text-neutral-400" />
        <span className="ml-2 text-neutral-500">Loading packages...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-start gap-2 p-4 bg-red-50 border border-red-200 rounded-md text-sm text-red-700">
        <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
        <div>
          <p className="font-medium">Failed to load sponsorship packages.</p>
          <Button size="sm" variant="ghost" onClick={() => refetch()} className="mt-1">
            <RefreshCw className="h-3.5 w-3.5 mr-1" />
            Retry
          </Button>
        </div>
      </div>
    );
  }

  const items = packages ?? [];

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between gap-2">
        <div>
          <h3 className="text-base font-semibold text-neutral-800 flex items-center gap-2">
            <Award className="h-5 w-5 text-amber-500" />
            Sponsorship Packages
          </h3>
          <p className="text-xs text-neutral-500 mt-0.5">
            Define tiered sponsorship offerings (Gold / Silver / Bronze) with perks and
            optional bundled tickets. Buyer-facing purchase lands in a future phase.
          </p>
        </div>
        <Button onClick={handleAddNew} size="sm">
          <Plus className="h-4 w-4 mr-1" />
          Add Package
        </Button>
      </div>

      {/* Empty state */}
      {items.length === 0 ? (
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
              <Button onClick={handleAddNew} size="sm">
                <Plus className="h-4 w-4 mr-1" />
                Create your first package
              </Button>
            </div>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {items.map((pkg) => (
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

      {/* Hidden file input for image uploads */}
      <input
        ref={fileInputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        className="hidden"
        onChange={handleFileSelected}
      />

      {/* Edit/create modal */}
      <SponsorshipPackageEditModal
        eventId={eventId}
        pkg={editingPackage}
        isOpen={modalOpen}
        onClose={() => setModalOpen(false)}
        onSaved={() => {
          /* React Query invalidation happens inside the hooks — no extra refetch needed */
        }}
      />
    </div>
  );
}
