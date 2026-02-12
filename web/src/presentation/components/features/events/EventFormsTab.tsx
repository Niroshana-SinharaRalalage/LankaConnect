'use client';

/**
 * EventFormsTab Component
 *
 * Top-level tab component for managing event forms (surveys/questionnaires).
 * Displays list of forms with CRUD actions for organizers.
 *
 * Custom Forms Feature - Phase 6A: Tab Integration
 */

import { useRouter } from 'next/navigation';
import { Plus } from 'lucide-react';

import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { useEventForms } from '@/presentation/hooks/useEventForms';
import { FormManagementSection } from './FormManagementSection';

interface EventFormsTabProps {
  eventId: string;
}

export function EventFormsTab({ eventId }: EventFormsTabProps) {
  const router = useRouter();
  const { data: forms, isLoading, error } = useEventForms(eventId);

  if (isLoading) {
    return (
      <Card>
        <CardContent className="py-12">
          <div className="flex items-center justify-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary" />
            <span className="ml-3 text-gray-600">Loading forms...</span>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card>
        <CardContent className="py-12">
          <div className="text-center">
            <p className="text-red-600 mb-4">Failed to load forms</p>
            <p className="text-sm text-gray-500">{error.message}</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-start justify-between">
          <div>
            <CardTitle>Signup Forms</CardTitle>
            <CardDescription>
              Create custom forms to collect information from attendees
            </CardDescription>
          </div>
          <Button
            onClick={() => router.push(`/events/${eventId}/manage/create-form`)}
            className="shrink-0"
          >
            <Plus className="w-4 h-4 mr-2" />
            Create Form
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        <FormManagementSection eventId={eventId} forms={forms || []} />
      </CardContent>
    </Card>
  );
}