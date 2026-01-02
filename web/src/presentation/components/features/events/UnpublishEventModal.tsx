'use client';

import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/presentation/components/ui/Dialog';
import { Button } from '@/presentation/components/ui/Button';

interface UnpublishEventModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onConfirm: () => void;
  isUnpublishing: boolean;
  eventTitle: string;
}

export function UnpublishEventModal({
  open,
  onOpenChange,
  onConfirm,
  isUnpublishing,
  eventTitle,
}: UnpublishEventModalProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Unpublish Event</DialogTitle>
          <DialogDescription>
            Are you sure you want to unpublish &quot;{eventTitle}&quot;?
          </DialogDescription>
        </DialogHeader>

        <div className="py-4">
          <p className="text-sm text-neutral-600 dark:text-neutral-400">
            This event will return to <span className="font-semibold">Draft status</span> and will no longer be visible to the public until you publish it again.
          </p>
        </div>

        <DialogFooter>
          <Button
            variant="ghost"
            onClick={() => onOpenChange(false)}
            disabled={isUnpublishing}
          >
            Cancel
          </Button>
          <Button
            variant="destructive"
            onClick={onConfirm}
            disabled={isUnpublishing}
          >
            {isUnpublishing ? 'Unpublishing...' : 'Unpublish Event'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
