'use client';

import { useEffect, useState } from 'react';
import { AlertTriangle, ArrowRight, CheckCircle2, Loader2 } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/presentation/components/ui/Dialog';
import { Button } from '@/presentation/components/ui/Button';
import {
  RegistrationMode,
  type ConvertRegistrationModeResult,
} from '@/infrastructure/api/types/events.types';
import { useConvertRegistrationMode } from '@/presentation/hooks/useEvents';

/**
 * Phase 7F-B.5 (architect-approved 2026-04-30): mode-conversion confirmation dialog
 * with diff preview powered by the dry-run branch of the API.
 *
 * Flow:
 *   1. On open, fire `dryRun: true` to compute the conversion report (no mutation).
 *   2. Render the diff: target mode, migrated count, skipped rows with reasons.
 *   3. Cancel → close, no commit.
 *   4. Confirm → fire `dryRun: false` to commit, then close + bubble result up.
 *
 * UX rules per architect plan §3 7F-B.5:
 *   - Always opt OUT of `notifyAttendees` for now (default-off; 7F-B.4 ships the email
 *     template later — UI gains the toggle when the email is wired).
 *   - Surface skipped reasons explicitly (e.g. "1 registration skipped: pending payment
 *     must resolve first") so the organiser knows what to do next.
 *   - Disable Confirm if every active registration is skipped — committing would no-op.
 */

interface ConvertRegistrationModeDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  eventId: string;
  fromMode: RegistrationMode;
  targetMode: RegistrationMode;
  /** Called after a successful commit so the parent can react (toast / refetch). */
  onComplete?: (result: ConvertRegistrationModeResult) => void;
}

const MODE_LABELS: Record<RegistrationMode, string> = {
  [RegistrationMode.DetailedAttendees]: 'Detailed (Per-Attendee)',
  [RegistrationMode.HeadCountOnly]: 'Head Count Only',
  [RegistrationMode.HeadCountByAge]: 'Head Count by Age',
  [RegistrationMode.HeadCountByGender]: 'Head Count by Gender',
  [RegistrationMode.HeadCountByAgeAndGender]: 'Head Count by Age × Gender',
  [RegistrationMode.NoRegistration]: 'No Registration',
};

export function ConvertRegistrationModeDialog({
  open,
  onOpenChange,
  eventId,
  fromMode,
  targetMode,
  onComplete,
}: ConvertRegistrationModeDialogProps) {
  const convertMutation = useConvertRegistrationMode();
  const [preview, setPreview] = useState<ConvertRegistrationModeResult | null>(null);
  const [previewError, setPreviewError] = useState<string | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);
  const [committing, setCommitting] = useState(false);
  const [commitError, setCommitError] = useState<string | null>(null);

  // Auto-fire the dry-run preview when the dialog opens (or when target changes mid-flight).
  useEffect(() => {
    if (!open) return;
    let cancelled = false;
    setPreview(null);
    setPreviewError(null);
    setPreviewLoading(true);
    convertMutation
      .mutateAsync({
        eventId,
        payload: { targetMode, dryRun: true, notifyAttendees: false },
      })
      .then((res) => {
        if (!cancelled) setPreview(res);
      })
      .catch((err) => {
        if (!cancelled) {
          setPreviewError(
            err?.response?.data?.detail ??
              err?.message ??
              'Failed to compute the conversion preview. Try again.',
          );
        }
      })
      .finally(() => {
        if (!cancelled) setPreviewLoading(false);
      });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, eventId, targetMode]);

  const handleConfirm = async () => {
    setCommitting(true);
    setCommitError(null);
    try {
      const result = await convertMutation.mutateAsync({
        eventId,
        payload: { targetMode, dryRun: false, notifyAttendees: false },
      });
      onComplete?.(result);
      onOpenChange(false);
    } catch (err: unknown) {
      const e = err as { response?: { data?: { detail?: string } }; message?: string };
      setCommitError(
        e?.response?.data?.detail ??
          e?.message ??
          'Failed to commit the conversion. The audit trail was not written.',
      );
    } finally {
      setCommitting(false);
    }
  };

  const allSkipped = preview && preview.totalProcessed > 0 && preview.migratedCount === 0;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>Change registration mode?</DialogTitle>
          <DialogDescription>
            Existing registrations on this event will be migrated to the new shape.
            Review the preview below before committing.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-2">
          <div className="flex items-center gap-3 text-sm">
            <span className="rounded-md bg-neutral-100 px-2 py-1 font-medium text-neutral-800">
              {MODE_LABELS[fromMode]}
            </span>
            <ArrowRight className="h-4 w-4 text-neutral-400" />
            <span className="rounded-md bg-orange-100 px-2 py-1 font-medium text-orange-800">
              {MODE_LABELS[targetMode]}
            </span>
          </div>

          {previewLoading && (
            <div className="flex items-center gap-2 text-sm text-neutral-600">
              <Loader2 className="h-4 w-4 animate-spin" />
              Computing preview…
            </div>
          )}

          {previewError && (
            <div className="flex items-start gap-2 rounded-md bg-red-50 border border-red-200 p-3 text-sm text-red-800">
              <AlertTriangle className="h-4 w-4 flex-shrink-0 mt-0.5" />
              <span>{previewError}</span>
            </div>
          )}

          {preview && (
            <div className="rounded-lg border border-neutral-200 bg-neutral-50 p-4 space-y-3">
              <div className="flex items-center gap-2 text-sm font-semibold text-neutral-900">
                <CheckCircle2 className="h-4 w-4 text-emerald-600" />
                {preview.totalProcessed === 0
                  ? 'No active registrations — only the event mode will flip.'
                  : `${preview.totalProcessed} active registration${preview.totalProcessed === 1 ? '' : 's'} reviewed`}
              </div>
              {preview.totalProcessed > 0 && (
                <div className="grid grid-cols-2 gap-3 text-sm">
                  <div>
                    <div className="text-xs text-neutral-500">Will be migrated</div>
                    <div className="text-lg font-semibold text-emerald-700">
                      {preview.migratedCount}
                    </div>
                  </div>
                  <div>
                    <div className="text-xs text-neutral-500">Will be skipped</div>
                    <div className="text-lg font-semibold text-amber-700">
                      {preview.skippedCount}
                    </div>
                  </div>
                </div>
              )}
              {preview.skipped.length > 0 && (
                <div className="space-y-1 text-xs">
                  <div className="font-medium text-neutral-700">Skipped reasons:</div>
                  <ul className="list-disc list-inside space-y-0.5 text-neutral-700">
                    {preview.skipped.slice(0, 5).map((s) => (
                      <li key={s.registrationId}>
                        {s.reasonCode}: {s.reason}
                      </li>
                    ))}
                    {preview.skipped.length > 5 && (
                      <li className="italic text-neutral-500">
                        …and {preview.skipped.length - 5} more
                      </li>
                    )}
                  </ul>
                </div>
              )}
            </div>
          )}

          {allSkipped && (
            <div className="flex items-start gap-2 rounded-md bg-amber-50 border border-amber-200 p-3 text-sm text-amber-900">
              <AlertTriangle className="h-4 w-4 flex-shrink-0 mt-0.5" />
              <span>
                Every active registration would be skipped. Resolve the blocking conditions
                above (e.g. cancel pending payments) before retrying.
              </span>
            </div>
          )}

          {commitError && (
            <div className="flex items-start gap-2 rounded-md bg-red-50 border border-red-200 p-3 text-sm text-red-800">
              <AlertTriangle className="h-4 w-4 flex-shrink-0 mt-0.5" />
              <span>{commitError}</span>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={committing}
          >
            Cancel
          </Button>
          <Button
            onClick={handleConfirm}
            disabled={
              previewLoading ||
              !!previewError ||
              !preview ||
              !!allSkipped ||
              committing
            }
            className="bg-orange-600 hover:bg-orange-700 text-white"
          >
            {committing ? (
              <>
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                Converting…
              </>
            ) : (
              'Confirm & Convert'
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
