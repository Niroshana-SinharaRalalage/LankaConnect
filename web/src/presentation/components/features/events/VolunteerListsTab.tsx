/**
 * VolunteerListsTab Component
 *
 * Phase 7D.1 step 22: Organizer-facing tab for managing volunteer lists.
 * Mirrors SignUpListsTab but filters to volunteer-kind lists and exports via
 * the volunteer-specific backend formats (`volunteerszip` / `volunteersexcel`).
 *
 * Copy, create-button route, and commitment modal labels are swapped to use
 * the volunteer variants from SignUpManagementSection + SignUpCommitmentModal.
 */

'use client';

import React from 'react';
import { useRouter } from 'next/navigation';
import { Download, Users } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import {
  SignUpManagementSection,
  volunteerSectionLabels,
} from '@/presentation/components/features/events/SignUpManagementSection';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import { SignUpKind, type SignUpListDto } from '@/infrastructure/api/types/events.types';

interface VolunteerListsTabProps {
  eventId: string;
  /**
   * Full (unfiltered) list of sign-up lists from the manage page. Used here
   * only to derive the volunteer-only subset for enabling/disabling the
   * export buttons — the nested SignUpManagementSection fetches its own
   * kind-filtered copy via useEventSignUps(eventId, SignUpKind.Volunteers).
   */
  signUpLists: SignUpListDto[];
}

export function VolunteerListsTab({ eventId, signUpLists }: VolunteerListsTabProps) {
  const router = useRouter();

  const volunteerLists = React.useMemo(
    () => (signUpLists ?? []).filter((list) => list.kind === SignUpKind.Volunteers),
    [signUpLists]
  );

  const handleExportCSV = async () => {
    if (volunteerLists.length === 0) {
      alert('No volunteer lists to export');
      return;
    }

    try {
      const blob = await eventsRepository.exportEventAttendees(eventId, 'volunteerszip');

      const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, -5);
      const filename = `event-${eventId}-volunteer-lists-csv-${timestamp}.zip`;

      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = filename;
      link.style.visibility = 'hidden';

      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    } catch (error: any) {
      console.error('Error exporting volunteer lists to CSV:', error);

      if (error.response?.status === 403) {
        alert('You do not have permission to export volunteer lists');
      } else if (error.response?.status === 404) {
        alert('Event not found');
      } else if (error.response?.status === 400) {
        alert(error.response?.data?.message || 'Failed to export volunteer lists');
      } else {
        alert('An error occurred while exporting volunteer lists');
      }
    }
  };

  const handleExportExcel = async () => {
    if (volunteerLists.length === 0) {
      alert('No volunteer lists to export');
      return;
    }

    try {
      const blob = await eventsRepository.exportEventAttendees(eventId, 'volunteersexcel');

      const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, -5);
      const filename = `event-${eventId}-volunteer-lists-excel-${timestamp}.zip`;

      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = filename;
      link.style.visibility = 'hidden';

      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    } catch (error: any) {
      console.error('Error exporting volunteer lists to Excel:', error);

      if (error.response?.status === 403) {
        alert('You do not have permission to export volunteer lists');
      } else if (error.response?.status === 404) {
        alert('Event not found');
      } else if (error.response?.status === 400) {
        alert(error.response?.data?.message || 'Failed to export volunteer lists');
      } else {
        alert('An error occurred while exporting volunteer lists');
      }
    }
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle style={{ color: '#8B1538' }}>Volunteer Roles</CardTitle>
            <CardDescription>Recruit volunteers for specific roles on this event</CardDescription>
          </div>
          <div className="flex gap-3">
            {volunteerLists.length > 0 && (
              <>
                <Button
                  variant="outline"
                  onClick={handleExportCSV}
                  className="flex items-center gap-2"
                >
                  <Download className="h-4 w-4" />
                  Export CSV
                </Button>
                <Button
                  variant="outline"
                  onClick={handleExportExcel}
                  className="flex items-center gap-2"
                >
                  <Download className="h-4 w-4" />
                  Export Excel
                </Button>
              </>
            )}
            <Button
              onClick={() => router.push(`/events/${eventId}/manage/create-volunteer-list`)}
              className="flex items-center gap-2 text-white"
              style={{ background: '#FF7900', color: 'white' }}
            >
              <Users className="h-4 w-4" />
              Create Volunteer List
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <SignUpManagementSection
          eventId={eventId}
          isOrganizer={true}
          kind={SignUpKind.Volunteers}
          labels={volunteerSectionLabels}
        />
      </CardContent>
    </Card>
  );
}
