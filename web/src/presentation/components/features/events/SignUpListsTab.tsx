/**
 * SignUpListsTab Component
 *
 * Phase 6A.45: Signup lists tab for manage page
 * Displays and manages event signup lists
 *
 * Extracted from original manage page to support tabbed layout
 */

'use client';

import React from 'react';
import { useRouter } from 'next/navigation';
import { Upload, Download } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { SignUpManagementSection } from '@/presentation/components/features/events/SignUpManagementSection';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type { SignUpListDto } from '@/infrastructure/api/types/events.types';

interface SignUpListsTabProps {
  eventId: string;
  signUpLists: SignUpListDto[];
}

export function SignUpListsTab({ eventId, signUpLists }: SignUpListsTabProps) {
  const router = useRouter();

  // Phase 6A.69: Handle Download ZIP (backend-generated CSV files)
  // FIX: Use eventsRepository for consistent API routing through proxy
  const handleDownloadCSV = async () => {
    if (!signUpLists || signUpLists.length === 0) {
      alert('No sign-up lists to download');
      return;
    }

    try {
      // Use events repository for consistent API routing (goes through proxy)
      const blob = await eventsRepository.exportEventAttendees(eventId, 'signuplistszip');

      // Generate filename with timestamp
      const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, -5);
      const filename = `event-${eventId}-signup-lists-${timestamp}.zip`;

      // Trigger download
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
      console.error('Error downloading sign-up lists:', error);

      // Handle specific error cases
      if (error.response?.status === 403) {
        alert('You do not have permission to export sign-up lists');
      } else if (error.response?.status === 404) {
        alert('Event not found');
      } else if (error.response?.status === 400) {
        alert(error.response?.data?.message || 'Failed to export sign-up lists');
      } else {
        alert('An error occurred while downloading sign-up lists');
      }
    }
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle style={{ color: '#8B1538' }}>Sign-Up Lists</CardTitle>
            <CardDescription>Manage items that attendees can volunteer to bring</CardDescription>
          </div>
          <div className="flex gap-3">
            {signUpLists && signUpLists.length > 0 && (
              <Button
                variant="outline"
                onClick={handleDownloadCSV}
                className="flex items-center gap-2"
              >
                <Download className="h-4 w-4" />
                Download CSV
              </Button>
            )}
            <Button
              onClick={() => router.push(`/events/${eventId}/manage/create-signup-list`)}
              className="flex items-center gap-2 text-white"
              style={{ background: '#FF7900', color: 'white' }}
            >
              <Upload className="h-4 w-4" />
              Create Sign-Up List
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <SignUpManagementSection eventId={eventId} isOrganizer={true} />
      </CardContent>
    </Card>
  );
}
