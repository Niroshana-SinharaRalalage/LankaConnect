'use client';

/**
 * FormManagementSection Component
 *
 * Displays and manages event forms with CRUD operations.
 * Shows form cards with status badges, response counts, and action buttons.
 *
 * Custom Forms Feature - Phase 6A: Tab Integration
 */

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { FileText, Edit, Trash2, Play, StopCircle, RotateCcw, Users, Plus } from 'lucide-react';

import type { EventFormDto } from '@/infrastructure/api/types/events.types';
import { EventFormStatus, EventFormStatusLabels } from '@/infrastructure/api/types/events.types';
import { Card, CardContent, CardFooter } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { ConfirmDialog } from '@/presentation/components/ui/ConfirmDialog';
import {
  usePublishEventForm,
  useCloseEventForm,
  useReopenEventForm,
  useDeleteEventForm,
} from '@/presentation/hooks/useEventForms';
import toast from 'react-hot-toast';

interface FormManagementSectionProps {
  eventId: string;
  forms: EventFormDto[];
}

export function FormManagementSection({ eventId, forms }: FormManagementSectionProps) {
  const router = useRouter();

  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [formToDelete, setFormToDelete] = useState<string | null>(null);

  // Mutations
  const publishForm = usePublishEventForm({
    onSuccess: () => toast.success('Form published successfully'),
    onError: (error) => toast.error(error.message || 'Failed to publish form'),
  });

  const closeForm = useCloseEventForm({
    onSuccess: () => toast.success('Form closed successfully'),
    onError: (error) => toast.error(error.message || 'Failed to close form'),
  });

  const reopenForm = useReopenEventForm({
    onSuccess: () => toast.success('Form reopened successfully'),
    onError: (error) => toast.error(error.message || 'Failed to reopen form'),
  });

  const deleteForm = useDeleteEventForm({
    onSuccess: () => {
      toast.success('Form deleted successfully');
      setDeleteConfirmOpen(false);
      setFormToDelete(null);
    },
    onError: (error) => {
      toast.error(error.message || 'Failed to delete form');
    },
  });

  // Handlers
  const handlePublish = async (formId: string) => {
    await publishForm.mutateAsync({ eventId, formId });
  };

  const handleClose = async (formId: string) => {
    await closeForm.mutateAsync({ eventId, formId });
  };

  const handleReopen = async (formId: string) => {
    await reopenForm.mutateAsync({ eventId, formId });
  };

  const handleDeleteClick = (formId: string) => {
    setFormToDelete(formId);
    setDeleteConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (!formToDelete) return;
    await deleteForm.mutateAsync({ eventId, formId: formToDelete });
  };

  const handleViewResponses = (formId: string) => {
    router.push(`/events/${eventId}/forms/${formId}/responses`);
  };

  // Helper: Get status badge variant
  const getStatusBadgeColor = (status: string | EventFormStatus): string => {
    const statusStr = typeof status === 'string' ? status : EventFormStatusLabels[status];
    switch (statusStr) {
      case 'Draft':
        return 'bg-gray-100 text-gray-800';
      case 'Active':
        return 'bg-green-100 text-green-800';
      case 'Closed':
        return 'bg-amber-100 text-amber-800';
      case 'Archived':
        return 'bg-neutral-100 text-neutral-600';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  // Empty state
  if (forms.length === 0) {
    return (
      <div className="text-center py-12">
        <FileText className="w-12 h-12 text-gray-400 mx-auto mb-4" />
        <h3 className="text-lg font-medium text-gray-900 mb-2">No forms yet</h3>
        <p className="text-sm text-gray-500 mb-6">
          Create your first form to start collecting information from attendees.
        </p>
        <Button onClick={() => router.push(`/events/${eventId}/manage/create-form`)}>
          <Plus className="w-4 h-4 mr-2" />
          Create Form
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Forms Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {forms.map((form) => (
          <Card key={form.id} className="flex flex-col">
            <CardContent className="flex-1 p-6">
              {/* Header with status badge */}
              <div className="flex items-start justify-between mb-4">
                <div className="flex-1 min-w-0">
                  <h3 className="text-lg font-semibold text-gray-900 mb-1 truncate">
                    {form.title}
                  </h3>
                  {form.description && (
                    <p className="text-sm text-gray-500 line-clamp-2">
                      {form.description}
                    </p>
                  )}
                </div>
                <Badge className={getStatusBadgeColor(form.status)}>
                  {EventFormStatusLabels[form.status as keyof typeof EventFormStatusLabels] || form.status}
                </Badge>
              </div>

              {/* Stats */}
              <div className="space-y-2">
                <div className="flex items-center text-sm text-gray-600">
                  <Users className="w-4 h-4 mr-2" />
                  <span>
                    {form.responseCount} response{form.responseCount !== 1 ? 's' : ''}
                  </span>
                </div>
              </div>

              {/* Dates */}
              <div className="mt-4 pt-4 border-t text-xs text-gray-500">
                <p>Created {new Date(form.createdAt).toLocaleDateString()}</p>
                {form.responseDeadline && (
                  <p className="mt-1">
                    Deadline: {new Date(form.responseDeadline).toLocaleDateString()}
                  </p>
                )}
              </div>
            </CardContent>

            <CardFooter className="p-6 pt-0 flex flex-wrap gap-2">
              {/* Edit button (Draft only) */}
              {(form.status === EventFormStatus.Draft || form.status === 'Draft') && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => router.push(`/events/${eventId}/forms/${form.id}/edit`)}
                >
                  <Edit className="w-4 h-4 mr-1" />
                  Edit
                </Button>
              )}

              {/* Publish button (Draft only) */}
              {(form.status === EventFormStatus.Draft || form.status === 'Draft') && (
                <Button
                  variant="default"
                  size="sm"
                  onClick={() => handlePublish(form.id)}
                  disabled={publishForm.isPending}
                >
                  <Play className="w-4 h-4 mr-1" />
                  {publishForm.isPending ? 'Publishing...' : 'Publish'}
                </Button>
              )}

              {/* Close button (Active only) */}
              {(form.status === EventFormStatus.Active || form.status === 'Active') && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handleClose(form.id)}
                  disabled={closeForm.isPending}
                >
                  <StopCircle className="w-4 h-4 mr-1" />
                  {closeForm.isPending ? 'Closing...' : 'Close'}
                </Button>
              )}

              {/* Reopen button (Closed only) */}
              {(form.status === EventFormStatus.Closed || form.status === 'Closed') && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handleReopen(form.id)}
                  disabled={reopenForm.isPending}
                >
                  <RotateCcw className="w-4 h-4 mr-1" />
                  {reopenForm.isPending ? 'Reopening...' : 'Reopen'}
                </Button>
              )}

              {/* View Responses button (if responses exist) */}
              {form.responseCount > 0 && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handleViewResponses(form.id)}
                >
                  <Users className="w-4 h-4 mr-1" />
                  View Responses
                </Button>
              )}

              {/* Delete button (Draft or no responses) */}
              {(form.status === EventFormStatus.Draft || form.status === 'Draft' || form.responseCount === 0) && (
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={() => handleDeleteClick(form.id)}
                  disabled={deleteForm.isPending}
                >
                  <Trash2 className="w-4 h-4 mr-1" />
                  Delete
                </Button>
              )}
            </CardFooter>
          </Card>
        ))}
      </div>

      {/* Delete Confirmation Dialog */}
      <ConfirmDialog
        open={deleteConfirmOpen}
        onOpenChange={setDeleteConfirmOpen}
        onConfirm={handleDeleteConfirm}
        title="Delete Form"
        description="Are you sure you want to delete this form? This action cannot be undone."
        confirmLabel="Delete"
        variant="danger"
        isLoading={deleteForm.isPending}
      />
    </div>
  );
}