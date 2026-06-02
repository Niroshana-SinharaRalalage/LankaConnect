'use client';

import { Award, Users, Package } from 'lucide-react';
import { Card, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import type { SponsorshipPackagePublicDto } from '@/infrastructure/api/types/events.types';

interface PublicSponsorshipPackageCardProps {
  pkg: SponsorshipPackagePublicDto;
  /** Fires when the buyer clicks the Sponsor CTA — parent opens the purchase modal. */
  onSelect: (pkg: SponsorshipPackagePublicDto) => void;
}

/**
 * Phase 6A.157 — buyer-facing card for the public event detail page. Sibling
 * of {@link SponsorshipPackageCard} (organizer view) but display-only: NO
 * edit / delete / image-upload actions, just a Sponsor CTA that fires
 * {@link PublicSponsorshipPackageCardProps.onSelect}.
 *
 * Visual parity with the organizer card where possible (same image / tier
 * badge / perks layout) so the buyer's perception matches what the organizer
 * sees while configuring packages.
 *
 * Sold-out packages get a disabled CTA + "Sold out" badge. The server filters
 * sold-out rows from the public list, but this defense-in-depth surface
 * catches the race where stock decrements between page load and click.
 */
export function PublicSponsorshipPackageCard({
  pkg,
  onSelect,
}: PublicSponsorshipPackageCardProps) {
  const formatCurrency = (amount: number, currency: string): string => {
    if (currency === 'USD') return `$${amount.toFixed(2)}`;
    return `${amount.toFixed(2)} ${currency}`;
  };

  // CTA copy: distinguishes free-package recognition from paid sponsorship so
  // buyers know what to expect on the Stripe redirect (or no redirect).
  const ctaLabel = pkg.isSoldOut
    ? 'Sold out'
    : pkg.priceAmount === 0
      ? `Become a ${pkg.tier ? pkg.tier + ' ' : ''}Sponsor (Free)`
      : `Sponsor as ${pkg.tier || pkg.name}`;

  return (
    <Card className="flex flex-col h-full">
      <CardContent className="p-4 flex flex-col flex-1 gap-3">
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

        {/* Tier badge + name + sold-out pill */}
        <div className="flex items-start justify-between gap-2">
          <div className="flex-1 min-w-0">
            {pkg.tier && (
              <Badge className="mb-1 bg-amber-50 text-amber-700 border-amber-200">
                <Award className="h-3 w-3 mr-1" />
                {pkg.tier}
              </Badge>
            )}
            <h4 className="text-sm font-semibold text-neutral-800">
              {pkg.name}
            </h4>
          </div>
          {pkg.isSoldOut && (
            <Badge className="bg-red-50 text-red-700 border-red-200">
              Sold out
            </Badge>
          )}
        </div>

        {/* Description */}
        {pkg.description && (
          <p className="text-xs text-neutral-600 line-clamp-3">{pkg.description}</p>
        )}

        {/* Price + remaining stock + tickets (informational) */}
        <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm">
          <span className="font-semibold text-neutral-800">
            {pkg.priceAmount === 0 ? 'Free' : formatCurrency(pkg.priceAmount, pkg.priceCurrency)}
          </span>
          {pkg.remainingStock != null && pkg.remainingStock <= 5 && !pkg.isSoldOut && (
            <span className="text-xs text-amber-600">
              {pkg.remainingStock} left
            </span>
          )}
          {pkg.includedTicketCount > 0 && (
            <span className="text-xs text-neutral-600 flex items-center gap-1">
              <Users className="h-3 w-3" />
              {pkg.includedTicketCount} ticket{pkg.includedTicketCount === 1 ? '' : 's'} included
            </span>
          )}
        </div>

        {/* Perks list */}
        {pkg.perks.length > 0 && (
          <ul className="text-xs text-neutral-600 list-disc list-inside space-y-0.5 flex-1">
            {pkg.perks.map((perk, idx) => (
              <li key={idx}>{perk}</li>
            ))}
          </ul>
        )}

        {/* CTA */}
        <Button
          type="button"
          className="w-full mt-auto"
          onClick={() => onSelect(pkg)}
          disabled={pkg.isSoldOut}
          style={{ background: pkg.isSoldOut ? undefined : '#FF7900' }}
        >
          {ctaLabel}
        </Button>
      </CardContent>
    </Card>
  );
}
