'use client';

import { useEffect } from 'react';
import { Users } from 'lucide-react';
import {
  RegistrationMode,
  type AllowedRegistrationModesRequest,
} from '@/infrastructure/api/types/events.types';
import { useAllowedRegistrationModes } from '@/presentation/hooks/useEvents';

/**
 * Phase 7E.5b/c: Per-event Mode picker for the create / edit forms.
 *
 * Renders a radio group of the 6 registration modes with descriptions, and disables options
 * that aren't compatible with the current event shape. Auto-clears the selection when the
 * shape change makes the previously-selected mode invalid (architect hot-spot #5).
 *
 * Visual style mirrors the existing pricing-toggle cards in the event create form
 * (border-orange-200, rounded-lg, gap-3) for consistency.
 */
interface RegistrationModePickerProps {
  /** Currently selected mode. */
  value: RegistrationMode;
  /** Setter — called when the user picks a different mode OR auto-clear fires. */
  onChange: (mode: RegistrationMode) => void;
  /** Current event-shape inputs. The picker re-queries allowed modes on every shape change. */
  shape: AllowedRegistrationModesRequest;
  /** Optional disabled flag to lock the picker (e.g. when registrations exist on the event). */
  disabled?: boolean;
  /** Optional helper text displayed under the picker (e.g. why options are disabled). */
  helpText?: string;
}

interface ModeOption {
  value: RegistrationMode;
  label: string;
  description: string;
}

const MODE_OPTIONS: readonly ModeOption[] = [
  {
    value: RegistrationMode.DetailedAttendees,
    label: 'Detailed (Per-Attendee)',
    description:
      'Each attendee provides Name, Adult/Child, and Male/Female. Best for ticketed events that need names per ticket. (default)',
  },
  {
    value: RegistrationMode.HeadCountOnly,
    label: 'Head Count Only',
    description:
      'Lead attendee Name + total head count (including the lead). No demographic detail captured.',
  },
  {
    value: RegistrationMode.HeadCountByAge,
    label: 'Head Count by Age',
    description:
      'Lead Name + Adults + Children. Total auto-derived from leaves. Required for dual-pricing events.',
  },
  {
    value: RegistrationMode.HeadCountByGender,
    label: 'Head Count by Gender',
    description: 'Lead Name + Males + Females. Total auto-derived. Useful for demographic reporting.',
  },
  {
    value: RegistrationMode.HeadCountByAgeAndGender,
    label: 'Head Count by Age × Gender',
    description:
      'Lead Name + Adult-Male / Adult-Female / Child-Male / Child-Female. Full demographic capture.',
  },
  {
    value: RegistrationMode.NoRegistration,
    label: 'No Registration',
    description:
      'Drop-in event. No registration form. Standalone donations / sponsors / add-ons / collections still work. Free attendance only.',
  },
  {
    value: RegistrationMode.External,
    label: 'External Registration',
    description:
      'Registration handled by an external provider (e.g. Eventbrite, Humanitix, cash-at-door). LankaConnect renders your link / instructions in the registration section. Ticket prices can still be defined and shown for reference. Used with Paid — external registration link.',
  },
];

export function RegistrationModePicker({
  value,
  onChange,
  shape,
  disabled = false,
  helpText,
}: RegistrationModePickerProps) {
  // Re-queries on every shape change. Falls back to [DetailedAttendees] if the query is
  // loading or errors — the picker never shows zero options.
  const { data: allowed = [RegistrationMode.DetailedAttendees], isLoading } =
    useAllowedRegistrationModes(shape);

  // Architect hot-spot #5: auto-clear the selection when the shape change makes the current
  // mode invalid (e.g. organiser flips "isFree" off after picking NoRegistration).
  useEffect(() => {
    if (!isLoading && !allowed.includes(value)) {
      onChange(RegistrationMode.DetailedAttendees);
    }
  }, [isLoading, allowed, value, onChange]);

  return (
    <div className="space-y-3 p-4 border-2 border-orange-200 rounded-lg bg-orange-50">
      <div className="flex items-start gap-2">
        <Users className="h-5 w-5 text-orange-600 mt-0.5" />
        <div>
          <h4 className="text-sm font-semibold text-neutral-800">Registration Mode</h4>
          <p className="text-xs text-neutral-600 mt-0.5">
            Choose how attendees register. Default <strong>Detailed</strong> matches every existing
            event. Pick a Head Count mode for lighter capture; pick No Registration for drop-in events.
          </p>
        </div>
      </div>

      <div className="space-y-2">
        {MODE_OPTIONS.map((opt) => {
          const isAllowed = allowed.includes(opt.value);
          const isSelected = value === opt.value;
          const optionDisabled = disabled || !isAllowed;

          return (
            <label
              key={opt.value}
              className={`flex items-start gap-3 p-3 rounded-md border transition-colors ${
                optionDisabled
                  ? 'border-neutral-200 bg-neutral-100 opacity-60 cursor-not-allowed'
                  : isSelected
                    ? 'border-orange-400 bg-white cursor-pointer'
                    : 'border-neutral-200 bg-white hover:border-orange-300 cursor-pointer'
              }`}
            >
              <input
                type="radio"
                name="registrationMode"
                value={opt.value}
                checked={isSelected}
                disabled={optionDisabled}
                onChange={() => !optionDisabled && onChange(opt.value)}
                className="mt-0.5 h-4 w-4 border-neutral-300 text-orange-500 focus:ring-2 focus:ring-orange-500 disabled:opacity-50"
              />
              <div className="flex-1 min-w-0">
                <div className="flex items-baseline gap-2">
                  <span className="text-sm font-medium text-neutral-800">{opt.label}</span>
                  {!isAllowed && !disabled && (
                    <span
                      className="text-xs text-neutral-500"
                      title="Not compatible with the current event shape"
                    >
                      (not available for this event)
                    </span>
                  )}
                </div>
                <p className="text-xs text-neutral-600 mt-0.5 leading-relaxed">{opt.description}</p>
              </div>
            </label>
          );
        })}
      </div>

      {helpText && <p className="text-xs text-neutral-500 italic">{helpText}</p>}
    </div>
  );
}
