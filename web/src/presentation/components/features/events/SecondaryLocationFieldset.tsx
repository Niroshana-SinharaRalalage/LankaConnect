'use client';

import type { UseFormRegister, FieldErrors, UseFormWatch, UseFormSetValue, Path, FieldValues } from 'react-hook-form';
import { Input } from '@/presentation/components/ui/Input';
import { SecondaryLocationType } from '@/infrastructure/api/types/events.types';

/**
 * Phase 7C.1: Shared fieldset for optional secondary event location.
 *
 * Supports:
 *  - Parking lot (separate from primary venue)
 *  - Secondary venue (overflow / annex)
 *
 * When the user picks a type, address + city become required (enforced by Zod
 * superRefine in event.schemas.ts). When the type is cleared, all secondary
 * fields are ignored by the backend.
 *
 * The component is form-agnostic: callers pass RHF `register`, `watch`,
 * `setValue`, and `errors` so it reuses whichever schema (`createEventSchema`
 * or `editEventSchema`) the parent form was built against.
 */
interface SecondaryLocationFieldsetProps<T extends FieldValues> {
  register: UseFormRegister<T>;
  watch: UseFormWatch<T>;
  setValue: UseFormSetValue<T>;
  errors: FieldErrors<T>;
}

export function SecondaryLocationFieldset<T extends FieldValues>({
  register,
  watch,
  setValue,
  errors,
}: SecondaryLocationFieldsetProps<T>) {
  // Use untyped Path cast — the field names are the same across both schemas.
  const typeField = 'secondaryLocationType' as Path<T>;
  const selectedType = watch(typeField) as SecondaryLocationType | null | undefined;
  const hasSecondary = !!selectedType;

  const getError = (name: string): string | undefined => {
    const err = (errors as Record<string, { message?: string }>)[name];
    return err?.message;
  };

  // Clear all secondary fields when the user switches to "None"
  const handleTypeChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const raw = e.target.value;
    if (!raw) {
      setValue(typeField, null as any, { shouldDirty: true, shouldValidate: true });
      setValue('secondaryLocationName' as Path<T>, '' as any, { shouldDirty: true });
      setValue('secondaryLocationAddress' as Path<T>, '' as any, { shouldDirty: true });
      setValue('secondaryLocationCity' as Path<T>, '' as any, { shouldDirty: true });
      setValue('secondaryLocationState' as Path<T>, '' as any, { shouldDirty: true });
      setValue('secondaryLocationZipCode' as Path<T>, '' as any, { shouldDirty: true });
      setValue('secondaryLocationCountry' as Path<T>, '' as any, { shouldDirty: true });
    } else {
      setValue(typeField, raw as any, { shouldDirty: true, shouldValidate: true });
    }
  };

  return (
    <div className="space-y-4 pt-2">
      <div className="border-t border-neutral-200 pt-4">
        <h4 className="text-sm font-semibold text-neutral-800 mb-1">Secondary Location (optional)</h4>
        <p className="text-xs text-neutral-500 mb-3">
          Add a parking lot or secondary venue — useful when parking is at a different address or you have an overflow space.
        </p>

        {/* Type dropdown */}
        <div>
          <label htmlFor="secondaryLocationType" className="block text-sm font-medium text-neutral-700 mb-2">
            Type
          </label>
          <select
            id="secondaryLocationType"
            className="flex h-10 w-full rounded-md border border-neutral-300 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2"
            value={(selectedType as string | null | undefined) ?? ''}
            onChange={handleTypeChange}
          >
            <option value="">None</option>
            <option value={SecondaryLocationType.ParkingLot}>Parking Lot</option>
            <option value={SecondaryLocationType.SecondaryVenue}>Secondary Venue</option>
          </select>
          {getError('secondaryLocationType') && (
            <p className="mt-1 text-sm text-destructive">{getError('secondaryLocationType')}</p>
          )}
        </div>
      </div>

      {hasSecondary && (
        <div className="space-y-4 rounded-md border border-neutral-200 bg-neutral-50 p-4">
          {/* Venue name (optional) */}
          <div>
            <label htmlFor="secondaryLocationName" className="block text-sm font-medium text-neutral-700 mb-2">
              {selectedType === SecondaryLocationType.ParkingLot ? 'Parking Lot Name' : 'Venue Name'}
            </label>
            <Input
              id="secondaryLocationName"
              type="text"
              placeholder={selectedType === SecondaryLocationType.ParkingLot ? 'e.g., North Lot' : 'e.g., Overflow Hall'}
              error={!!getError('secondaryLocationName')}
              {...register('secondaryLocationName' as Path<T>)}
            />
            {getError('secondaryLocationName') && (
              <p className="mt-1 text-sm text-destructive">{getError('secondaryLocationName')}</p>
            )}
          </div>

          {/* Street address (required when type is set) */}
          <div>
            <label htmlFor="secondaryLocationAddress" className="block text-sm font-medium text-neutral-700 mb-2">
              Street Address *
            </label>
            <Input
              id="secondaryLocationAddress"
              type="text"
              placeholder="e.g., 500 Side Street"
              error={!!getError('secondaryLocationAddress')}
              {...register('secondaryLocationAddress' as Path<T>)}
            />
            {getError('secondaryLocationAddress') && (
              <p className="mt-1 text-sm text-destructive">{getError('secondaryLocationAddress')}</p>
            )}
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* City (required) */}
            <div>
              <label htmlFor="secondaryLocationCity" className="block text-sm font-medium text-neutral-700 mb-2">
                City *
              </label>
              <Input
                id="secondaryLocationCity"
                type="text"
                placeholder="e.g., Columbus"
                error={!!getError('secondaryLocationCity')}
                {...register('secondaryLocationCity' as Path<T>)}
              />
              {getError('secondaryLocationCity') && (
                <p className="mt-1 text-sm text-destructive">{getError('secondaryLocationCity')}</p>
              )}
            </div>

            {/* State */}
            <div>
              <label htmlFor="secondaryLocationState" className="block text-sm font-medium text-neutral-700 mb-2">
                State
              </label>
              <Input
                id="secondaryLocationState"
                type="text"
                placeholder="e.g., Ohio"
                error={!!getError('secondaryLocationState')}
                {...register('secondaryLocationState' as Path<T>)}
              />
              {getError('secondaryLocationState') && (
                <p className="mt-1 text-sm text-destructive">{getError('secondaryLocationState')}</p>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* ZIP Code */}
            <div>
              <label htmlFor="secondaryLocationZipCode" className="block text-sm font-medium text-neutral-700 mb-2">
                ZIP Code
              </label>
              <Input
                id="secondaryLocationZipCode"
                type="text"
                placeholder="e.g., 43215"
                error={!!getError('secondaryLocationZipCode')}
                {...register('secondaryLocationZipCode' as Path<T>)}
              />
              {getError('secondaryLocationZipCode') && (
                <p className="mt-1 text-sm text-destructive">{getError('secondaryLocationZipCode')}</p>
              )}
            </div>

            {/* Country */}
            <div>
              <label htmlFor="secondaryLocationCountry" className="block text-sm font-medium text-neutral-700 mb-2">
                Country
              </label>
              <Input
                id="secondaryLocationCountry"
                type="text"
                placeholder="e.g., United States"
                error={!!getError('secondaryLocationCountry')}
                {...register('secondaryLocationCountry' as Path<T>)}
              />
              {getError('secondaryLocationCountry') && (
                <p className="mt-1 text-sm text-destructive">{getError('secondaryLocationCountry')}</p>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
