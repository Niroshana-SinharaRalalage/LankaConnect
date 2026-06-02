'use client';

import { Pencil, Trash2, ImageIcon, ImageOff, Award, Users, Package } from 'lucide-react';
import { Card, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import type { SponsorshipPackageDto } from '@/infrastructure/api/types/events.types';

interface SponsorshipPackageCardProps {
  pkg: SponsorshipPackageDto;
  onEdit: (pkg: SponsorshipPackageDto) => void;
  onDelete: (pkg: SponsorshipPackageDto) => void;
  onImageUpload: (pkg: SponsorshipPackageDto) => void;
  onImageClear: (pkg: SponsorshipPackageDto) => void;
}

/**
 * Phase 6A.156 — single package card used in the organizer management grid.
 * Shows image, tier badge, name, price, stock, perks count, included-ticket
 * count, and the per-card actions (Edit / Delete / Image set/clear).
 *
 * Inactive packages get a muted card body so the organizer can see what's
 * deactivated without scanning a status column.
 */
export function SponsorshipPackageCard({
  pkg,
  onEdit,
  onDelete,
  onImageUpload,
  onImageClear,
}: SponsorshipPackageCardProps) {
  const formatCurrency = (amount: number, currency: string): string => {
    if (currency === 'USD') return `$${amount.toFixed(2)}`;
    return `${amount.toFixed(2)} ${currency}`;
  };

  return (
    <Card className={pkg.isActive ? '' : 'opacity-60'}>
      <CardContent className="p-4">
        <div className="flex flex-col gap-3">
          {/* Image */}
          {pkg.imageUrl ? (
            <div className="aspect-video w-full overflow-hidden rounded-md bg-neutral-100">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={pkg.imageUrl}
                alt={pkg.name}
                className="h-full w-full object-cover"
              />
            </div>
          ) : (
            <div className="flex aspect-video w-full items-center justify-center rounded-md border border-dashed border-neutral-200 bg-neutral-50">
              <Package className="h-10 w-10 text-neutral-300" />
            </div>
          )}

          {/* Tier badge + name + status pill */}
          <div className="flex items-start justify-between gap-2">
            <div className="flex-1 min-w-0">
              {pkg.tier && (
                <Badge className="mb-1 bg-amber-50 text-amber-700 border-amber-200">
                  <Award className="h-3 w-3 mr-1" />
                  {pkg.tier}
                </Badge>
              )}
              <h4 className="text-sm font-semibold text-neutral-800 truncate">
                {pkg.name}
              </h4>
            </div>
            {!pkg.isActive && (
              <Badge className="bg-neutral-100 text-neutral-500 border-neutral-200">
                Inactive
              </Badge>
            )}
          </div>

          {/* Description */}
          {pkg.description && (
            <p className="text-xs text-neutral-600 line-clamp-2">{pkg.description}</p>
          )}

          {/* Price + stock + tickets */}
          <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm">
            <span className="font-semibold text-neutral-800">
              {formatCurrency(pkg.priceAmount, pkg.priceCurrency)}
            </span>
            {pkg.quantityLimit != null ? (
              <span className="text-xs text-neutral-500">
                {pkg.quantitySold} / {pkg.quantityLimit} sold
              </span>
            ) : (
              <span className="text-xs text-neutral-500">Unlimited</span>
            )}
            {pkg.includedTicketCount > 0 && (
              <span className="text-xs text-neutral-600 flex items-center gap-1">
                <Users className="h-3 w-3" />
                {pkg.includedTicketCount} ticket{pkg.includedTicketCount === 1 ? '' : 's'}
              </span>
            )}
          </div>

          {/* Perks (count + first two) */}
          {pkg.perks.length > 0 && (
            <div className="text-xs text-neutral-600">
              <div className="font-medium mb-1">
                {pkg.perks.length} perk{pkg.perks.length === 1 ? '' : 's'}:
              </div>
              <ul className="list-disc list-inside space-y-0.5">
                {pkg.perks.slice(0, 3).map((perk, idx) => (
                  <li key={idx} className="truncate">
                    {perk}
                  </li>
                ))}
                {pkg.perks.length > 3 && (
                  <li className="text-neutral-400 list-none">
                    + {pkg.perks.length - 3} more
                  </li>
                )}
              </ul>
            </div>
          )}

          {/* Actions */}
          <div className="flex flex-wrap items-center justify-end gap-1 pt-2 border-t border-neutral-100">
            <Button
              size="sm"
              variant="ghost"
              onClick={() => onImageUpload(pkg)}
              title={pkg.imageUrl ? 'Replace image' : 'Upload image'}
            >
              <ImageIcon className="h-3.5 w-3.5" />
            </Button>
            {pkg.imageUrl && (
              <Button
                size="sm"
                variant="ghost"
                onClick={() => onImageClear(pkg)}
                title="Clear image"
              >
                <ImageOff className="h-3.5 w-3.5" />
              </Button>
            )}
            <Button size="sm" variant="ghost" onClick={() => onEdit(pkg)}>
              <Pencil className="h-3.5 w-3.5 mr-1" />
              Edit
            </Button>
            <Button
              size="sm"
              variant="ghost"
              onClick={() => onDelete(pkg)}
              className="text-red-600 hover:text-red-700 hover:bg-red-50"
            >
              <Trash2 className="h-3.5 w-3.5" />
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
