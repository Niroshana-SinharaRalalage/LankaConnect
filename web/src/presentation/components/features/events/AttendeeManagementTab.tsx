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
  RefreshCw,
  QrCode,
} from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { useEventAttendees, useExportEventAttendees } from '@/presentation/hooks/useEvents';
import { RegistrationStatus, PaymentStatus, AgeCategory, Gender } from '@/infrastructure/api/types/events.types';
import type { EventAttendeeDto } from '@/infrastructure/api/types/events.types';
import { ResendConfirmationDialog } from './ResendConfirmationDialog';
import { QRCodeModal } from './QRCodeModal';

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
function getRegistrationStatusLabel(status: RegistrationStatus | number): string {
  // Convert to number if needed
  const statusNum = Number(status);

  // If NaN, return the original value as string
  if (isNaN(statusNum)) {
    console.warn('Invalid registration status:', status, typeof status);
    return String(status);
  }

  switch (statusNum) {
    case 0:
      return 'Pending';
    case 1:
      return 'Confirmed';
    case 2:
      return 'Waitlisted';
    case 3:
      return 'Checked In';
    case 4:
      return 'Completed';
    case 5:
      return 'Cancelled';
    case 6:
      return 'Refunded';
    default:
      console.warn('Unknown registration status value:', statusNum);
      return 'Unknown';
  }
}

/**
 * Helper: Get display label for payment status
 */
function getPaymentStatusLabel(status: PaymentStatus | number): string {
  // Convert to number if needed
  const statusNum = Number(status);

  // If NaN, return the original value as string
  if (isNaN(statusNum)) {
    console.warn('Invalid payment status:', status, typeof status);
    return String(status);
  }

  switch (statusNum) {
    case 0:
      return 'Pending';
    case 1:
      return 'Completed';
    case 2:
      return 'Failed';
    case 3:
      return 'Refunded';
    case 4:
      return 'N/A';
    default:
      console.warn('Unknown payment status value:', statusNum);
      return 'Unknown';
  }
}

/**
 * AttendeeManagementTab Component
 */
export function AttendeeManagementTab({ eventId }: AttendeeManagementTabProps) {
  const [expandedRows, setExpandedRows] = useState<Set<string>>(new Set());
  const [isExporting, setIsExporting] = useState(false);

  // Phase 6A.X: Resend confirmation dialog state
  const [resendDialogOpen, setResendDialogOpen] = useState(false);
  const [selectedAttendee, setSelectedAttendee] = useState<{
    name: string;
    email: string;
    registrationId: string;
  } | null>(null);

  // Phase 6A.X: QR Code modal state
  const [qrModalOpen, setQrModalOpen] = useState(false);
  const [selectedTicket, setSelectedTicket] = useState<{
    ticketCode: string;
    qrCodeData: string;
    eventTitle: string;
    eventDate?: string;
    attendeeName: string;
    attendeeEmail: string;
  } | null>(null);

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

  // Phase 6A.X: Handler for resend email button
  const handleResendClick = (attendee: EventAttendeeDto) => {
    setSelectedAttendee({
      name: attendee.mainAttendeeName,
      email: attendee.contactEmail,
      registrationId: attendee.registrationId,
    });
    setResendDialogOpen(true);
  };

  // Phase 6A.X: Handler for QR code click
  const handleQrCodeClick = (attendee: EventAttendeeDto) => {
    if (!attendee.ticketCode || !attendee.qrCodeData) return;

    setSelectedTicket({
      ticketCode: attendee.ticketCode,
      qrCodeData: attendee.qrCodeData,
      eventTitle: attendeesData?.eventTitle || '',
      eventDate: undefined, // Event date not available in EventAttendeesResponse
      attendeeName: attendee.mainAttendeeName,
      attendeeEmail: attendee.contactEmail,
    });
    setQrModalOpen(true);
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

  const {
    attendees,
    totalRegistrations,
    totalAttendees,
    grossRevenue,
    commissionAmount,
    netRevenue,
    commissionRate,
    isFreeEvent,
    // Phase 6A.X: Detailed revenue breakdown
    totalSalesTax,
    totalStripeFees,
    totalPlatformCommission,
    totalOrganizerPayout,
    averageTaxRate,
    hasRevenueBreakdown,
  } = attendeesData;

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

        {/* Phase 6A.X: Revenue with detailed breakdown (when available) */}
        {!isFreeEvent && grossRevenue > 0 && (
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-neutral-600">Your Payout</p>
                  <p className="text-3xl font-bold text-green-700">
                    ${(hasRevenueBreakdown ? totalOrganizerPayout : netRevenue).toFixed(2)}
                  </p>
                  <p className="text-xs text-neutral-500 mt-1">
                    {hasRevenueBreakdown
                      ? 'After tax, Stripe fees & platform commission'
                      : `After ${(commissionRate * 100).toFixed(0)}% platform fee`}
                  </p>
                </div>
                <DollarSign className="h-10 w-10 text-orange-600" />
              </div>

              {/* Phase 6A.X: Detailed Breakdown (new events) */}
              {hasRevenueBreakdown ? (
                <div className="mt-3 pt-3 border-t border-neutral-200">
                  <div className="flex justify-between text-xs text-neutral-600 mb-1">
                    <span>Gross Revenue:</span>
                    <span className="font-medium">${grossRevenue.toFixed(2)}</span>
                  </div>
                  {totalSalesTax > 0 && (
                    <div className="flex justify-between text-xs text-amber-600 mb-1">
                      <span>Sales Tax ({(averageTaxRate * 100).toFixed(2)}%):</span>
                      <span className="font-medium">-${totalSalesTax.toFixed(2)}</span>
                    </div>
                  )}
                  <div className="flex justify-between text-xs text-red-600 mb-1">
                    <span>Stripe Fees (2.9% + $0.30):</span>
                    <span className="font-medium">-${totalStripeFees.toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between text-xs text-red-600 mb-1">
                    <span>Platform Commission (2%):</span>
                    <span className="font-medium">-${totalPlatformCommission.toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between text-xs text-green-700 font-semibold pt-1 border-t border-neutral-200">
                    <span>Your Payout:</span>
                    <span>${totalOrganizerPayout.toFixed(2)}</span>
                  </div>
                  <p className="text-[10px] text-neutral-400 mt-2">
                    * Sales tax collected based on event location. Stripe & LankaConnect fees shown separately.
                  </p>
                </div>
              ) : (
                /* Legacy Breakdown (older events without detailed breakdown) */
                <div className="mt-3 pt-3 border-t border-neutral-200">
                  <div className="flex justify-between text-xs text-neutral-600 mb-1">
                    <span>Gross Revenue:</span>
                    <span className="font-medium">${grossRevenue.toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between text-xs text-red-600 mb-1">
                    <span>Platform Fee ({(commissionRate * 100).toFixed(0)}%):</span>
                    <span className="font-medium">-${commissionAmount.toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between text-xs text-green-700 font-semibold pt-1 border-t border-neutral-200">
                    <span>Your Payout:</span>
                    <span>${netRevenue.toFixed(2)}</span>
                  </div>
                  <p className="text-[10px] text-neutral-400 mt-2">
                    * 5% platform fee includes both LankaConnect and Stripe processing fees
                  </p>
                </div>
              )}
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
            /* Attendees Table - Phase 6A.71: Added vertical scrolling */
            <div className="overflow-x-auto max-h-[600px] overflow-y-auto border rounded-lg">
              <table className="w-full border-collapse">
                <thead className="sticky top-0 z-10 bg-neutral-50">
                  <tr className="border-b border-neutral-200">
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700 w-10"></th>
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Main Attendee</th>
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Additional</th>
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Total</th>
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Adults/Kids</th>
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Gender</th>
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Email</th>
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Phone</th>
                    {/* Phase 6A.71: Hide payment/amount columns for free events */}
                    {!isFreeEvent && (
                      <>
                        <th className="text-left p-3 text-sm font-semibold text-neutral-700">Payment</th>
                        <th className="text-left p-3 text-sm font-semibold text-neutral-700">Net Amount</th>
                        {/* Phase 6A.X: Ticket Code column for paid events */}
                        <th className="text-left p-3 text-sm font-semibold text-neutral-700">Ticket Code</th>
                      </>
                    )}
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Status</th>
                    {/* Phase 6A.X: Actions column for resend confirmation */}
                    <th className="text-left p-3 text-sm font-semibold text-neutral-700">Actions</th>
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
                          <td className="p-3 text-sm text-neutral-600">
                            {attendee.additionalAttendees ? (
                              <div className="max-w-xs">
                                {attendee.additionalAttendees.split(', ').map((name, idx) => (
                                  <div key={idx} className="truncate" title={name}>
                                    • {name}
                                  </div>
                                ))}
                              </div>
                            ) : (
                              '—'
                            )}
                          </td>
                          <td className="p-3 text-sm text-neutral-900 font-medium">{attendee.totalAttendees}</td>
                          <td className="p-3 text-sm text-neutral-600">
                            {attendee.adultCount}A / {attendee.childCount}C
                          </td>
                          <td className="p-3 text-sm text-neutral-600">{attendee.genderDistribution || '—'}</td>
                          <td className="p-3 text-sm text-neutral-600">{attendee.contactEmail || '—'}</td>
                          <td className="p-3 text-sm text-neutral-600">{attendee.contactPhone || '—'}</td>
                          {/* Phase 6A.71: Hide payment/amount columns for free events */}
                          {!isFreeEvent && (
                            <>
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
                                {/* Phase 6A.71: Show NET amount (after commission) instead of GROSS */}
                                {attendee.netAmount ? `$${attendee.netAmount.toFixed(2)}` : '—'}
                              </td>
                              {/* Phase 6A.X: Ticket Code column - clickable to show QR modal */}
                              <td className="p-3">
                                {attendee.ticketCode ? (
                                  <button
                                    onClick={() => handleQrCodeClick(attendee)}
                                    className="flex items-center gap-1 text-blue-600 hover:text-blue-800 hover:underline cursor-pointer text-sm font-mono"
                                    title="Click to view QR code"
                                  >
                                    <QrCode className="h-4 w-4" />
                                    {attendee.ticketCode}
                                  </button>
                                ) : (
                                  <span className="text-neutral-400 text-sm">No ticket</span>
                                )}
                              </td>
                            </>
                          )}
                          <td className="p-3">
                            <Badge style={{ backgroundColor: getStatusColor(attendee.status), color: 'white' }}>
                              {getRegistrationStatusLabel(attendee.status)}
                            </Badge>
                          </td>
                          {/* Phase 6A.X: Actions column - resend confirmation button */}
                          <td className="p-3">
                            {/* Phase 6A.X: Check for status = 1 because getRegistrationStatusLabel has shifted mapping
                                status = 1 displays as "Confirmed" in the UI (even though enum says Confirmed = 2)
                                This matches the Phase 6A.74 pattern for status comparison handling */}
                            {Number(attendee.status) === 1 && (
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => handleResendClick(attendee)}
                                className="flex items-center gap-1 text-xs hover:bg-neutral-100"
                                title="Resend registration confirmation email"
                              >
                                <RefreshCw className="h-3 w-3" />
                                Resend
                              </Button>
                            )}
                          </td>
                        </tr>

                        {/* Expanded Row - Attendee Details */}
                        {isExpanded && (
                          <tr className="bg-neutral-50 border-b border-neutral-100">
                            {/* Phase 6A.X: Updated colSpan for new columns - Ticket Code (paid) + Actions (all) */}
                            {/* Free events: 10 columns (added Actions), Paid events: 13 columns (added Ticket Code + Actions) */}
                            <td colSpan={isFreeEvent ? 10 : 13} className="p-6">
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

                {/* Phase 6A.71: Table footer with totals - sticky at bottom */}
                {attendees.length > 0 && (
                  <tfoot className="sticky bottom-0 bg-neutral-100 border-t-2 border-neutral-300">
                    <tr>
                      {/* Phase 6A.X: Updated colSpan for new columns - Ticket Code (paid) + Actions (all) */}
                      {/* Free events: 10 columns (added Actions), Paid events: 13 columns (added Ticket Code + Actions) */}
                      <td colSpan={isFreeEvent ? 10 : 13} className="p-4">
                        <div className="flex justify-end items-center gap-8">
                          <div className="flex items-center gap-2">
                            <span className="text-sm font-semibold text-neutral-700">Total Registrations:</span>
                            <span className="text-lg font-bold text-blue-900">{totalRegistrations}</span>
                          </div>
                          <div className="flex items-center gap-2">
                            <span className="text-sm font-semibold text-neutral-700">Total Attendees:</span>
                            <span className="text-lg font-bold text-green-900">{totalAttendees}</span>
                          </div>
                          {!isFreeEvent && grossRevenue > 0 && (
                            <div className="flex items-center gap-2">
                              <span className="text-sm font-semibold text-neutral-700">Your Payout:</span>
                              <span className="text-lg font-bold text-orange-900">
                                ${(hasRevenueBreakdown ? totalOrganizerPayout : netRevenue).toFixed(2)}
                              </span>
                              <span className="text-xs text-neutral-500">
                                {hasRevenueBreakdown ? '(after tax + fees)' : '(after fees)'}
                              </span>
                            </div>
                          )}
                        </div>
                      </td>
                    </tr>
                  </tfoot>
                )}
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Phase 6A.X: Resend Confirmation Dialog */}
      <ResendConfirmationDialog
        open={resendDialogOpen}
        onOpenChange={setResendDialogOpen}
        attendee={selectedAttendee}
        eventId={eventId}
      />

      {/* Phase 6A.X: QR Code Modal */}
      <QRCodeModal
        open={qrModalOpen}
        onOpenChange={setQrModalOpen}
        ticketCode={selectedTicket?.ticketCode || null}
        qrCodeData={selectedTicket?.qrCodeData || null}
        eventTitle={selectedTicket?.eventTitle || ''}
        eventDate={selectedTicket?.eventDate}
        attendeeName={selectedTicket?.attendeeName || ''}
        attendeeEmail={selectedTicket?.attendeeEmail || ''}
      />
    </div>
  );
}
