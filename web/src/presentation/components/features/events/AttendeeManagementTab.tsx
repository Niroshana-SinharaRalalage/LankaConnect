/**
 * AttendeeManagementTab Component
 *
 * Phase 6A.45: Comprehensive attendee management for event organizers
 *
 * Features:
 * - Tabular view of all registrations with expandable rows
 * - Display contact information, payment status, attendee details
 * - Export to Excel (multi-sheet with signup lists) and CSV
 * - Summary statistics (total registrations, attendees, revenue)
 * - Responsive design with proper loading and error states
 *
 * @requires useEventAttendees hook
 * @requires useExportEventAttendees hook
 */

'use client';

import React, { useState } from 'react';
import {
  ChevronDown,
  ChevronRight,
  Download,
  Users,
  DollarSign,
  Calendar,
  Mail,
  Phone,
  MapPin,
} from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { useEventAttendees, useExportEventAttendees } from '@/presentation/hooks/useEvents';
import { RegistrationStatus, PaymentStatus, AgeCategory, Gender } from '@/infrastructure/api/types/events.types';
import type { EventAttendeeDto } from '@/infrastructure/api/types/events.types';

/**
 * Props for AttendeeManagementTab
 */
interface AttendeeManagementTabProps {
  eventId: string;
}

/**
 * Helper: Get badge color for registration status
 */
function getStatusColor(status: RegistrationStatus): string {
  switch (status) {
    case RegistrationStatus.Confirmed:
      return '#10B981'; // Green
    case RegistrationStatus.Pending:
      return '#F59E0B'; // Amber
    case RegistrationStatus.Cancelled:
      return '#EF4444'; // Red
    case RegistrationStatus.CheckedIn:
      return '#6366F1'; // Indigo
    default:
      return '#6B7280'; // Gray
  }
}

/**
 * Helper: Get badge color for payment status
 */
function getPaymentStatusColor(status: PaymentStatus): string {
  switch (status) {
    case PaymentStatus.Completed:
      return '#10B981'; // Green
    case PaymentStatus.Pending:
      return '#F59E0B'; // Amber
    case PaymentStatus.Failed:
      return '#EF4444'; // Red
    case PaymentStatus.NotRequired:
      return '#6B7280'; // Gray
    default:
      return '#6B7280';
  }
}

/**
 * Helper: Get display label for registration status
 */
function getRegistrationStatusLabel(status: RegistrationStatus): string {
  // Handle numeric enum values (backend sends numbers, not strings)
  const statusNum = typeof status === 'string' ? parseInt(status, 10) : status;

  switch (statusNum) {
    case 0: // RegistrationStatus.Pending
    case RegistrationStatus.Pending:
      return 'Pending';
    case 1: // RegistrationStatus.Confirmed
    case RegistrationStatus.Confirmed:
      return 'Confirmed';
    case 2: // RegistrationStatus.Waitlisted
    case RegistrationStatus.Waitlisted:
      return 'Waitlisted';
    case 3: // RegistrationStatus.CheckedIn
    case RegistrationStatus.CheckedIn:
      return 'Checked In';
    case 4: // RegistrationStatus.Completed
    case RegistrationStatus.Completed:
      return 'Completed';
    case 5: // RegistrationStatus.Cancelled
    case RegistrationStatus.Cancelled:
      return 'Cancelled';
    case 6: // RegistrationStatus.Refunded
    case RegistrationStatus.Refunded:
      return 'Refunded';
    default:
      console.warn('Unknown registration status:', status, typeof status);
      return `Unknown (${status})`;
  }
}

/**
 * Helper: Get display label for payment status
 */
function getPaymentStatusLabel(status: PaymentStatus): string {
  // Handle numeric enum values (backend sends numbers, not strings)
  const statusNum = typeof status === 'string' ? parseInt(status, 10) : status;

  switch (statusNum) {
    case 0: // PaymentStatus.Pending
    case PaymentStatus.Pending:
      return 'Pending';
    case 1: // PaymentStatus.Completed
    case PaymentStatus.Completed:
      return 'Completed';
    case 2: // PaymentStatus.Failed
    case PaymentStatus.Failed:
      return 'Failed';
    case 3: // PaymentStatus.Refunded
    case PaymentStatus.Refunded:
      return 'Refunded';
    case 4: // PaymentStatus.NotRequired
    case PaymentStatus.NotRequired:
      return 'N/A';
    default:
      console.warn('Unknown payment status:', status, typeof status);
      return `Unknown (${status})`;
  }
}

/**
 * AttendeeManagementTab Component
 */
export function AttendeeManagementTab({ eventId }: AttendeeManagementTabProps) {
  const [expandedRows, setExpandedRows] = useState<Set<string>>(new Set());
  const [isExporting, setIsExporting] = useState(false);

  // Fetch attendees
  const { data: attendeesData, isLoading, error } = useEventAttendees(eventId);
  const exportMutation = useExportEventAttendees();

  // Toggle row expansion
  const toggleRow = (registrationId: string) => {
    setExpandedRows((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(registrationId)) {
        newSet.delete(registrationId);
      } else {
        newSet.add(registrationId);
      }
      return newSet;
    });
  };

  // Handle export
  const handleExport = async (format: 'excel' | 'csv') => {
    try {
      setIsExporting(true);
      const blob = await exportMutation.mutateAsync({ eventId, format });

      // Create download link
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `event-${eventId}-attendees.${format === 'excel' ? 'xlsx' : 'csv'}`;
      link.style.visibility = 'hidden';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    } catch (err) {
      console.error('Export failed:', err);
      alert('Failed to export attendees. Please try again.');
    } finally {
      setIsExporting(false);
    }
  };

  // Loading state
  if (isLoading) {
    return (
      <Card className="animate-pulse">
        <CardContent className="p-12">
          <div className="h-8 bg-neutral-200 rounded w-1/2 mb-4"></div>
          <div className="h-64 bg-neutral-200 rounded"></div>
        </CardContent>
      </Card>
    );
  }

  // Error state
  if (error || !attendeesData) {
    return (
      <Card>
        <CardContent className="p-12 text-center">
          <h2 className="text-2xl font-bold text-red-600 mb-4">Failed to Load Attendees</h2>
          <p className="text-neutral-600 mb-6">
            {error?.message || 'Unable to fetch attendee data. Please try again later.'}
          </p>
        </CardContent>
      </Card>
    );
  }

  const { attendees, totalRegistrations, totalAttendees, totalRevenue } = attendeesData;

  return (
    <div className="space-y-6">
      {/* Summary Statistics */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* Total Registrations */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-neutral-600">Total Registrations</p>
                <p className="text-3xl font-bold text-neutral-900">{totalRegistrations}</p>
              </div>
              <Users className="h-10 w-10 text-blue-600" />
            </div>
          </CardContent>
        </Card>

        {/* Total Attendees */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-neutral-600">Total Attendees</p>
                <p className="text-3xl font-bold text-neutral-900">{totalAttendees}</p>
              </div>
              <Users className="h-10 w-10 text-green-600" />
            </div>
          </CardContent>
        </Card>

        {/* Total Revenue */}
        {totalRevenue !== null && totalRevenue !== undefined && totalRevenue > 0 && (
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-neutral-600">Total Revenue</p>
                  <p className="text-3xl font-bold text-neutral-900">${totalRevenue.toFixed(2)}</p>
                </div>
                <DollarSign className="h-10 w-10 text-orange-600" />
              </div>
            </CardContent>
          </Card>
        )}
      </div>

      {/* Export Buttons */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle style={{ color: '#8B1538' }}>Attendee List</CardTitle>
              <CardDescription>
                {totalRegistrations} {totalRegistrations === 1 ? 'registration' : 'registrations'} with {totalAttendees}{' '}
                {totalAttendees === 1 ? 'attendee' : 'attendees'}
              </CardDescription>
            </div>
            <div className="flex gap-3">
              <Button
                variant="outline"
                onClick={() => handleExport('csv')}
                disabled={isExporting || attendees.length === 0}
                className="flex items-center gap-2"
              >
                <Download className="h-4 w-4" />
                Export CSV
              </Button>
              <Button
                onClick={() => handleExport('excel')}
                disabled={isExporting || attendees.length === 0}
                className="flex items-center gap-2 text-white"
                style={{ background: '#FF7900' }}
              >
                <Download className="h-4 w-4" />
                Export Excel
              </Button>
            </div>
          </div>
        </CardHeader>

        <CardContent>
          {/* No Attendees State */}
          {attendees.length === 0 ? (
            <div className="text-center py-12">
              <Users className="h-16 w-16 text-neutral-300 mx-auto mb-4" />
              <p className="text-neutral-600 text-lg font-medium mb-2">No Registrations Yet</p>
              <p className="text-neutral-500 text-sm">
                Attendees will appear here once people start registering for your event.
              </p>
            </div>
          ) : (
            /* Attendees Table */
            <div className="overflow-x-auto">
              <table className="w-full border-collapse">
                <thead>
                  <tr className="bg-neutral-50 border-b border-neutral-200">
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700 w-10"></th>
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Main Attendee</th>
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Additional</th>
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Total</th>
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Adults/Kids</th>
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Gender</th>
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Email</th>
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Phone</th>
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Payment</th>
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Amount</th>
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {attendees.map((attendee) => {
                    const isExpanded = expandedRows.has(attendee.registrationId);
                    return (
                      <React.Fragment key={attendee.registrationId}>
                        {/* Main Row */}
                        <tr className="border-b border-neutral-100 hover:bg-neutral-50 transition-colors">
                          <td className="p-3">
                            <button
                              onClick={() => toggleRow(attendee.registrationId)}
                              className="p-1 hover:bg-neutral-200 rounded transition-colors"
                              aria-label={isExpanded ? 'Collapse row' : 'Expand row'}
                            >
                              {isExpanded ? (
                                <ChevronDown className="h-4 w-4 text-neutral-600" />
                              ) : (
                                <ChevronRight className="h-4 w-4 text-neutral-600" />
                              )}
                            </button>
                          </td>
                          <td className="p-3 text-sm text-neutral-900 font-medium">{attendee.mainAttendeeName}</td>
                          <td className="p-3 text-sm text-neutral-600">{attendee.additionalAttendees}</td>
                          <td className="p-3 text-sm text-neutral-900 font-medium">{attendee.totalAttendees}</td>
                          <td className="p-3 text-sm text-neutral-600">
                            {attendee.adultCount}A / {attendee.childCount}C
                          </td>
                          <td className="p-3 text-sm text-neutral-600">{attendee.genderDistribution || '—'}</td>
                          <td className="p-3 text-sm text-neutral-600">{attendee.contactEmail || '—'}</td>
                          <td className="p-3 text-sm text-neutral-600">{attendee.contactPhone || '—'}</td>
                          <td className="p-3">
                            {attendee.paymentStatus === PaymentStatus.NotRequired ? (
                              <span className="text-sm text-neutral-500">N/A</span>
                            ) : (
                              <Badge style={{ backgroundColor: getPaymentStatusColor(attendee.paymentStatus), color: 'white' }}>
                                {getPaymentStatusLabel(attendee.paymentStatus)}
                              </Badge>
                            )}
                          </td>
                          <td className="p-3 text-sm text-neutral-900 font-medium">
                            {attendee.totalAmount ? `$${attendee.totalAmount.toFixed(2)}` : '—'}
                          </td>
                          <td className="p-3">
                            <Badge style={{ backgroundColor: getStatusColor(attendee.status), color: 'white' }}>
                              {getRegistrationStatusLabel(attendee.status)}
                            </Badge>
                          </td>
                        </tr>

                        {/* Expanded Row - Attendee Details */}
                        {isExpanded && (
                          <tr className="bg-neutral-50 border-b border-neutral-100">
                            <td colSpan={11} className="p-6">
                              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                {/* Left Column - Attendee Details */}
                                <div>
                                  <h4 className="text-sm font-semibold text-neutral-700 mb-3">Attendee Details</h4>
                                  <div className="space-y-2">
                                    {attendee.attendees.map((person, index) => (
                                      <div key={index} className="flex items-center gap-3 p-2 bg-white rounded border border-neutral-200">
                                        <div className="flex-1">
                                          <p className="text-sm font-medium text-neutral-900">{person.name}</p>
                                          <div className="flex gap-3 mt-1">
                                            <Badge className="bg-blue-100 text-blue-700 text-xs">
                                              {AgeCategory[person.ageCategory]}
                                            </Badge>
                                            {person.gender && (
                                              <Badge className="bg-purple-100 text-purple-700 text-xs">
                                                {Gender[person.gender]}
                                              </Badge>
                                            )}
                                          </div>
                                        </div>
                                      </div>
                                    ))}
                                  </div>
                                </div>

                                {/* Right Column - Contact & Registration Info */}
                                <div className="space-y-4">
                                  <div>
                                    <h4 className="text-sm font-semibold text-neutral-700 mb-3">Contact Information</h4>
                                    <div className="space-y-2">
                                      {attendee.contactEmail && (
                                        <div className="flex items-center gap-2 text-sm text-neutral-600">
                                          <Mail className="h-4 w-4 text-[#FF7900]" />
                                          {attendee.contactEmail}
                                        </div>
                                      )}
                                      {attendee.contactPhone && (
                                        <div className="flex items-center gap-2 text-sm text-neutral-600">
                                          <Phone className="h-4 w-4 text-[#FF7900]" />
                                          {attendee.contactPhone}
                                        </div>
                                      )}
                                      {attendee.contactAddress && (
                                        <div className="flex items-center gap-2 text-sm text-neutral-600">
                                          <MapPin className="h-4 w-4 text-[#FF7900]" />
                                          {attendee.contactAddress}
                                        </div>
                                      )}
                                    </div>
                                  </div>

                                  <div>
                                    <h4 className="text-sm font-semibold text-neutral-700 mb-3">Registration Info</h4>
                                    <div className="space-y-2">
                                      <div className="flex items-center gap-2 text-sm text-neutral-600">
                                        <Calendar className="h-4 w-4 text-[#FF7900]" />
                                        {new Date(attendee.createdAt).toLocaleString()}
                                      </div>
                                      <div className="text-xs text-neutral-500">
                                        ID: {attendee.registrationId.substring(0, 8)}...
                                      </div>
                                    </div>
                                  </div>
                                </div>
                              </div>
                            </td>
                          </tr>
                        )}
                      </React.Fragment>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
