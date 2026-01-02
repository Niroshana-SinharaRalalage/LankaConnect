'use client';

import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/presentation/components/ui/Dialog';
import { Button } from '@/presentation/components/ui/Button';
import { AlertTriangle } from 'lucide-react';

interface DeleteEventModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onConfirm: () => void;
  isDeleting: boolean;
  eventTitle: string;
  error?: string | null;
}

export function DeleteEventModal({
  open,
  onOpenChange,
  onConfirm,
  isDeleting,
  eventTitle,
  error,
}: DeleteEventModalProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-red-600 dark:text-red-500">
            <AlertTriangle className="h-5 w-5" />
            Delete Event - CRITICAL WARNING
          </DialogTitle>
          <DialogDescription>
            This action <span className="font-bold text-red-600 dark:text-red-500">CANNOT</span> be undone.
          </DialogDescription>
        </DialogHeader>

        <div className="py-4 space-y-4">
          <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
            <p className="text-sm font-semibold text-red-900 dark:text-red-100 mb-2">
              The event will be PERMANENTLY DELETED from the database.
            </p>
            <p className="text-sm text-red-800 dark:text-red-200">
              Are you absolutely sure you want to delete &quot;{eventTitle}&quot;?
            </p>
          </div>

          {error && (
            <div className="bg-red-50 dark:bg-red-900/20 border border-red-300 dark:border-red-700 rounded-lg p-4">
              <p className="text-sm font-semibold text-red-900 dark:text-red-100 mb-1">
                Cannot Delete Event
              </p>
              <p className="text-sm text-red-800 dark:text-red-200">
                {error}
              </p>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button
            variant="ghost"
            onClick={() => onOpenChange(false)}
            disabled={isDeleting}
          >
            Cancel
          </Button>
          <Button
            variant="destructive"
            onClick={onConfirm}
            disabled={isDeleting || !!error}
            className="bg-red-600 hover:bg-red-700"
          >
            {isDeleting ? 'Deleting...' : 'Permanently Delete'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
