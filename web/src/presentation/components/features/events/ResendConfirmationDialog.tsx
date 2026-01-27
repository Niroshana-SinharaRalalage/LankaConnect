'use client';

import * as React from 'react';
import { Mail, CheckCircle, XCircle, Loader2 } from 'lucide-react';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/presentation/components/ui/Dialog';
import { Button } from '@/presentation/components/ui/Button';
import { useResendAttendeeConfirmation } from '@/presentation/hooks/useEvents';

export interface ResendConfirmationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  attendee: {
    name: string;
    email: string;
    registrationId: string;
  } | null;
  eventId: string;
}

/**
 * ResendConfirmationDialog Component
 * Phase 6A.X: Resend registration confirmation email to attendee
 *
 * Features:
 * - Shows attendee name and email
 * - Confirm/Cancel buttons
 * - Loading state during send
 * - Success message (green checkmark) after successful send
 * - Error message (red X) on failure
 * - Close button enabled after success/error
 */
export function ResendConfirmationDialog({
  open,
  onOpenChange,
  attendee,
  eventId,
}: ResendConfirmationDialogProps) {
  const resendMutation = useResendAttendeeConfirmation();
  const [status, setStatus] = React.useState<'idle' | 'success' | 'error'>('idle');
  const [errorMessage, setErrorMessage] = React.useState<string | null>(null);

  // Reset status when dialog opens/closes
  React.useEffect(() => {
    if (!open) {
      setStatus('idle');
      setErrorMessage(null);
      resendMutation.reset();
    }
  }, [open, resendMutation]);

  const handleConfirm = async () => {
    if (!attendee) return;

    setStatus('idle');
    setErrorMessage(null);

    try {
      await resendMutation.mutateAsync({
        eventId,
        registrationId: attendee.registrationId,
      });
      setStatus('success');
    } catch (error: any) {
      setStatus('error');
      setErrorMessage(error?.response?.data?.message || error?.message || 'Failed to send email');
    }
  };

  const handleClose = () => {
    onOpenChange(false);
  };

  if (!attendee) return null;

  const isLoading = resendMutation.isPending;
  const isComplete = status === 'success' || status === 'error';

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <div className="flex items-start gap-4">
            <div className="p-2 rounded-full bg-blue-100 flex-shrink-0">
              <Mail className="w-6 h-6 text-blue-600" />
            </div>
            <div className="flex-1">
              <DialogTitle>Resend Registration Confirmation</DialogTitle>
              <DialogDescription className="mt-2">
                {status === 'idle' && (
                  <>
                    <div className="space-y-1">
                      <p className="text-sm text-gray-600">
                        Send confirmation email to:
                      </p>
                      <p className="text-sm font-medium text-gray-900">{attendee.name}</p>
                      <p className="text-sm text-gray-700">{attendee.email}</p>
                    </div>
                    <p className="mt-3 text-sm text-gray-600">
                      This will resend the full registration confirmation email,
                      including ticket PDF for paid events.
                    </p>
                  </>
                )}
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        {/* Success/Error Messages */}
        {status === 'success' && (
          <div className="p-4 bg-green-50 border border-green-200 rounded-lg flex items-start gap-3">
            <CheckCircle className="w-5 h-5 text-green-600 flex-shrink-0 mt-0.5" />
            <div className="flex-1">
              <p className="text-sm font-medium text-green-900">
                Confirmation email sent successfully
              </p>
              <p className="text-xs text-green-700 mt-1">
                Email sent to {attendee.email}
              </p>
            </div>
          </div>
        )}

        {status === 'error' && (
          <div className="p-4 bg-red-50 border border-red-200 rounded-lg flex items-start gap-3">
            <XCircle className="w-5 h-5 text-red-600 flex-shrink-0 mt-0.5" />
            <div className="flex-1">
              <p className="text-sm font-medium text-red-900">
                Failed to send email
              </p>
              {errorMessage && (
                <p className="text-xs text-red-700 mt-1">{errorMessage}</p>
              )}
            </div>
          </div>
        )}

        <DialogFooter>
          {!isComplete ? (
            <>
              <Button
                variant="outline"
                onClick={handleClose}
                disabled={isLoading}
              >
                Cancel
              </Button>
              <Button
                onClick={handleConfirm}
                disabled={isLoading}
                className="bg-blue-600 hover:bg-blue-700 text-white"
              >
                {isLoading ? (
                  <>
                    <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                    Sending...
                  </>
                ) : (
                  <>
                    <Mail className="w-4 h-4 mr-2" />
                    Send Email
                  </>
                )}
              </Button>
            </>
          ) : (
            <Button onClick={handleClose} className="w-full">
              Close
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
