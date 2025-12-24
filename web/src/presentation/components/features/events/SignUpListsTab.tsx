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

  // Handle Download CSV
  const handleDownloadCSV = () => {
    if (!signUpLists || signUpLists.length === 0) {
      alert('No sign-up lists to download');
      return;
    }

    // Build CSV content
    let csvContent = 'Category,Item Description,User ID,Quantity,Committed At\n';

    signUpLists.forEach((list) => {
      (list.commitments || []).forEach((commitment) => {
        csvContent += `"${list.category}","${commitment.itemDescription}","${commitment.userId}",${commitment.quantity},"${commitment.committedAt}"\n`;
      });
    });

    // Create download link
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);

    link.setAttribute('href', url);
    link.setAttribute('download', `event-${eventId}-signups.csv`);
    link.style.visibility = 'hidden';

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
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
