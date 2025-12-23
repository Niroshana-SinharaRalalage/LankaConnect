'use client';

import { useState, useEffect } from 'react';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { Download, Mail, Ticket, CheckCircle, XCircle, Clock, Users, QrCode, RefreshCw } from 'lucide-react';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type { TicketDto } from '@/infrastructure/api/types/events.types';
import { AgeCategory, Gender } from '@/infrastructure/api/types/events.types';

interface TicketSectionProps {
  eventId: string;
  isPaidEvent: boolean;
}

/**
 * Phase 6A.24: Ticket Section Component
 * Displays ticket information for paid event registrations
 * Features:
 * - QR code display
 * - PDF download
 * - Email resend
 * - Attendee list
 */
export function TicketSection({ eventId, isPaidEvent }: TicketSectionProps) {
  const [ticket, setTicket] = useState<TicketDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isDownloading, setIsDownloading] = useState(false);
  const [isResending, setIsResending] = useState(false);
  const [resendSuccess, setResendSuccess] = useState(false);

  useEffect(() => {
    if (!isPaidEvent) {
      setIsLoading(false);
      return;
    }

    const fetchTicket = async () => {
      try {
        setIsLoading(true);
        setError(null);
        const ticketData = await eventsRepository.getMyTicket(eventId);
        setTicket(ticketData);
      } catch (err) {
        // Check if it's a 404 (no ticket yet) or payment not completed
        if (err instanceof Error && err.message.includes('404')) {
          setError('Ticket not yet generated. It will be available after payment confirmation.');
        } else if (err instanceof Error && err.message.toLowerCase().includes('payment not completed')) {
          // Payment status shows pending - common during checkout process
          setError('Payment processing. Your ticket will be available once payment is confirmed.');
        } else {
          setError('Failed to load ticket. Please try again later.');
        }
        console.error('Failed to fetch ticket:', err);
      } finally {
        setIsLoading(false);
      }
    };

    fetchTicket();
  }, [eventId, isPaidEvent]);

  const handleDownloadPdf = async () => {
    if (isDownloading) return;

    try {
      setIsDownloading(true);
      const blob = await eventsRepository.downloadTicketPdf(eventId);

      // Create download link
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `ticket-${ticket?.ticketCode || 'event'}.pdf`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    } catch (err) {
      console.error('Failed to download PDF:', err);
      alert('Failed to download ticket PDF. Please try again.');
    } finally {
      setIsDownloading(false);
    }
  };

  const handleResendEmail = async () => {
    if (isResending) return;

    try {
      setIsResending(true);
      setResendSuccess(false);
      await eventsRepository.resendTicketEmail(eventId);
      setResendSuccess(true);
      // Clear success message after 5 seconds
      setTimeout(() => setResendSuccess(false), 5000);
    } catch (err) {
      console.error('Failed to resend email:', err);
      alert('Failed to resend ticket email. Please try again.');
    } finally {
      setIsResending(false);
    }
  };

  // Don't render for free events
  if (!isPaidEvent) {
    return null;
  }

  if (isLoading) {
    return (
      <Card className="mt-6">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Ticket className="h-5 w-5" />
            Your Ticket
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-center py-8">
            <RefreshCw className="h-6 w-6 animate-spin text-muted-foreground" />
            <span className="ml-2 text-muted-foreground">Loading ticket...</span>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (error || !ticket) {
    return (
      <Card className="mt-6">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Ticket className="h-5 w-5" />
            Your Ticket
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center gap-2 text-muted-foreground">
            <Clock className="h-5 w-5" />
            <span>{error || 'Ticket not available yet.'}</span>
          </div>
        </CardContent>
      </Card>
    );
  }

  const isExpired = new Date(ticket.expiresAt) < new Date();

  return (
    <Card className="mt-6">
      <CardHeader>
        <CardTitle className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Ticket className="h-5 w-5" />
            Your Ticket
          </div>
          {ticket.isValid && !isExpired ? (
            <Badge variant="featured" className="bg-green-500 text-white">
              <CheckCircle className="h-3 w-3 mr-1" />
              Valid
            </Badge>
          ) : (
            <Badge variant="hot">
              <XCircle className="h-3 w-3 mr-1" />
              {isExpired ? 'Expired' : 'Invalid'}
            </Badge>
          )}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-6">
        {/* Ticket Code */}
        <div className="flex flex-col md:flex-row md:items-start gap-6">
          {/* QR Code */}
          {ticket.qrCodeBase64 && (
            <div className="flex flex-col items-center">
              <div className="bg-white p-4 rounded-lg shadow-inner border">
                <img
                  src={`data:image/png;base64,${ticket.qrCodeBase64}`}
                  alt="Ticket QR Code"
                  className="w-40 h-40"
                />
              </div>
              <span className="text-xs text-muted-foreground mt-2 flex items-center gap-1">
                <QrCode className="h-3 w-3" />
                Scan at event check-in
              </span>
            </div>
          )}

          {/* Ticket Details */}
          <div className="flex-1 space-y-4">
            <div>
              <label className="text-sm font-medium text-muted-foreground">Ticket Code</label>
              <p className="text-xl font-mono font-bold tracking-wider">{ticket.ticketCode}</p>
            </div>

            {ticket.eventTitle && (
              <div>
                <label className="text-sm font-medium text-muted-foreground">Event</label>
                <p className="font-medium">{ticket.eventTitle}</p>
              </div>
            )}

            {ticket.eventStartDate && (
              <div>
                <label className="text-sm font-medium text-muted-foreground">Date</label>
                <p>{new Date(ticket.eventStartDate).toLocaleDateString('en-US', {
                  weekday: 'long',
                  year: 'numeric',
                  month: 'long',
                  day: 'numeric',
                  hour: 'numeric',
                  minute: '2-digit',
                })}</p>
              </div>
            )}

            {ticket.eventLocation && (
              <div>
                <label className="text-sm font-medium text-muted-foreground">Location</label>
                <p>{ticket.eventLocation}</p>
              </div>
            )}
          </div>
        </div>

        {/* Attendees */}
        {ticket.attendees && ticket.attendees.length > 0 && (
          <div>
            <label className="text-sm font-medium text-muted-foreground flex items-center gap-1 mb-2">
              <Users className="h-4 w-4" />
              Attendees ({ticket.attendeeCount})
            </label>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
              {ticket.attendees.map((attendee, index) => (
                <div
                  key={index}
                  className="flex items-center justify-between bg-muted/50 px-3 py-2 rounded-md"
                >
                  <span className="font-medium">{attendee.name}</span>
                  <span className="text-sm text-muted-foreground">
                    {attendee.ageCategory === AgeCategory.Adult || (attendee.ageCategory as unknown) === 'Adult' ? 'Adult' : 'Child'}
                    {attendee.gender !== null && attendee.gender !== undefined && ` • ${attendee.gender === Gender.Male || (attendee.gender as unknown) === 'Male' ? 'Male' : attendee.gender === Gender.Female || (attendee.gender as unknown) === 'Female' ? 'Female' : 'Other'}`}
                  </span>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Actions */}
        <div className="flex flex-col sm:flex-row gap-3 pt-4 border-t">
          <Button
            onClick={handleDownloadPdf}
            disabled={isDownloading}
            className="flex-1"
          >
            {isDownloading ? (
              <>
                <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
                Downloading...
              </>
            ) : (
              <>
                <Download className="h-4 w-4 mr-2" />
                Download Ticket
              </>
            )}
          </Button>

          <Button
            variant="outline"
            onClick={handleResendEmail}
            disabled={isResending}
            className="flex-1"
          >
            {isResending ? (
              <>
                <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
                Sending...
              </>
            ) : resendSuccess ? (
              <>
                <CheckCircle className="h-4 w-4 mr-2 text-green-500" />
                Email Sent!
              </>
            ) : (
              <>
                <Mail className="h-4 w-4 mr-2" />
                Resend Confirmation Email
              </>
            )}
          </Button>
        </div>

        {/* Expiry Notice */}
        {ticket.expiresAt && (
          <p className="text-xs text-muted-foreground text-center">
            Ticket {isExpired ? 'expired' : 'valid until'}{' '}
            {new Date(ticket.expiresAt).toLocaleDateString('en-US', {
              year: 'numeric',
              month: 'long',
              day: 'numeric',
            })}
          </p>
        )}
      </CardContent>
    </Card>
  );
}
