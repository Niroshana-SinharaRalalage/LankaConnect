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
import type { SignUpListDto } from '@/infrastructure/api/types/events.types';

interface SignUpListsTabProps {
  eventId: string;
  signUpLists: SignUpListDto[];
}

export function SignUpListsTab({ eventId, signUpLists }: SignUpListsTabProps) {
  const router = useRouter();

  // Phase 6A.69: Handle Download ZIP (backend-generated CSV files)
  const handleDownloadCSV = async () => {
    if (!signUpLists || signUpLists.length === 0) {
      alert('No sign-up lists to download');
      return;
    }

    try {
      // Call backend export endpoint (signuplistszip format)
      const response = await fetch(`/api/events/${eventId}/export?format=signuplistszip`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include', // Include cookies for authentication
      });

      if (!response.ok) {
        if (response.status === 403) {
          alert('You do not have permission to export sign-up lists');
        } else if (response.status === 404) {
          alert('Event not found');
        } else if (response.status === 400) {
          const errorText = await response.text();
          alert(errorText || 'Failed to export sign-up lists');
        } else {
          alert('Failed to export sign-up lists');
        }
        return;
      }

      // Get filename from Content-Disposition header
      const contentDisposition = response.headers.get('Content-Disposition');
      let filename = `event-${eventId}-signup-lists.zip`;

      if (contentDisposition) {
        const filenameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
        if (filenameMatch && filenameMatch[1]) {
          filename = filenameMatch[1].replace(/['"]/g, '');
        }
      }

      // Download ZIP file
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = filename;
      link.style.visibility = 'hidden';

      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    } catch (error) {
      console.error('Error downloading sign-up lists:', error);
      alert('An error occurred while downloading sign-up lists');
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
