'use client';

import * as React from 'react';
import { QrCode, X, Calendar, Mail, User, CheckCircle } from 'lucide-react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/presentation/components/ui/Dialog';
import QRCode from 'qrcode';

export interface QRCodeModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  ticketCode: string | null;
  qrCodeData: string | null;
  eventTitle: string;
  eventDate?: string;
  attendeeName: string;
  attendeeEmail: string;
}

/**
 * QRCodeModal Component
 * Phase 6A.X: Display ticket QR code for event check-in
 *
 * Features:
 * - Large QR code display (400x400px)
 * - Ticket code prominently displayed
 * - Event title and date
 * - Registration details (attendee name, email)
 * - Instructions for event check-in
 * - Scannable QR code encoding: TicketCode|EventId|RegistrationId (base64)
 */
export function QRCodeModal({
  open,
  onOpenChange,
  ticketCode,
  qrCodeData,
  eventTitle,
  eventDate,
  attendeeName,
  attendeeEmail,
}: QRCodeModalProps) {
  const [qrCodeUrl, setQrCodeUrl] = React.useState<string | null>(null);
  const [error, setError] = React.useState<string | null>(null);

  // Generate QR code when dialog opens
  React.useEffect(() => {
    if (open && qrCodeData) {
      generateQRCode(qrCodeData);
    } else {
      setQrCodeUrl(null);
      setError(null);
    }
  }, [open, qrCodeData]);

  const generateQRCode = async (data: string) => {
    try {
      // Generate QR code as data URL
      const url = await QRCode.toDataURL(data, {
        width: 400,
        margin: 2,
        color: {
          dark: '#000000',
          light: '#FFFFFF',
        },
        errorCorrectionLevel: 'H', // High error correction for better scanning
      });
      setQrCodeUrl(url);
      setError(null);
    } catch (err) {
      console.error('Failed to generate QR code:', err);
      setError('Failed to generate QR code');
      setQrCodeUrl(null);
    }
  };

  const formatDate = (dateString: string | undefined): string => {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <div className="flex items-start justify-between">
            <div className="flex items-start gap-4 flex-1">
              <div className="p-2 rounded-full bg-indigo-100 flex-shrink-0">
                <QrCode className="w-6 h-6 text-indigo-600" />
              </div>
              <div className="flex-1">
                <DialogTitle className="text-xl font-bold text-[#8B1538]">
                  Event Ticket
                </DialogTitle>
                <p className="text-sm text-gray-600 mt-1">
                  Scan this code at event check-in
                </p>
              </div>
            </div>
          </div>
        </DialogHeader>

        <div className="space-y-6 mt-4">
          {/* Ticket Code */}
          {ticketCode && (
            <div className="text-center">
              <p className="text-sm text-gray-600 mb-1">Ticket Code</p>
              <p className="text-3xl font-bold text-[#8B1538] tracking-wider font-mono">
                {ticketCode}
              </p>
            </div>
          )}

          {/* QR Code */}
          <div className="flex justify-center py-6">
            {qrCodeUrl ? (
              <div className="relative">
                <img
                  src={qrCodeUrl}
                  alt="Ticket QR Code"
                  className="w-[400px] h-[400px] border-4 border-gray-200 rounded-lg shadow-lg"
                />
                <div className="absolute top-2 right-2 bg-green-100 rounded-full p-1">
                  <CheckCircle className="w-5 h-5 text-green-600" />
                </div>
              </div>
            ) : error ? (
              <div className="w-[400px] h-[400px] border-4 border-red-200 rounded-lg flex items-center justify-center bg-red-50">
                <div className="text-center p-8">
                  <X className="w-12 h-12 text-red-600 mx-auto mb-2" />
                  <p className="text-red-900 font-medium">{error}</p>
                </div>
              </div>
            ) : (
              <div className="w-[400px] h-[400px] border-4 border-gray-200 rounded-lg flex items-center justify-center bg-gray-50">
                <div className="text-center">
                  <div className="inline-block w-8 h-8 border-4 border-gray-300 border-t-indigo-600 rounded-full animate-spin mb-2"></div>
                  <p className="text-gray-600">Generating QR code...</p>
                </div>
              </div>
            )}
          </div>

          {/* Event and Attendee Details */}
          <div className="bg-gray-50 rounded-lg p-6 space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {/* Event Details */}
              <div className="space-y-3">
                <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide">
                  Event Details
                </h3>
                <div className="flex items-start gap-2">
                  <Calendar className="w-4 h-4 text-gray-500 mt-0.5 flex-shrink-0" />
                  <div>
                    <p className="text-sm font-medium text-gray-900">
                      {eventTitle}
                    </p>
                    {eventDate && (
                      <p className="text-xs text-gray-600 mt-1">
                        {formatDate(eventDate)}
                      </p>
                    )}
                  </div>
                </div>
              </div>

              {/* Attendee Details */}
              <div className="space-y-3">
                <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide">
                  Attendee Details
                </h3>
                <div className="space-y-2">
                  <div className="flex items-center gap-2">
                    <User className="w-4 h-4 text-gray-500 flex-shrink-0" />
                    <p className="text-sm text-gray-900">{attendeeName}</p>
                  </div>
                  <div className="flex items-center gap-2">
                    <Mail className="w-4 h-4 text-gray-500 flex-shrink-0" />
                    <p className="text-sm text-gray-900">{attendeeEmail}</p>
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Instructions */}
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
            <div className="flex items-start gap-3">
              <div className="p-1 rounded-full bg-blue-100">
                <QrCode className="w-4 h-4 text-blue-600" />
              </div>
              <div className="flex-1">
                <p className="text-sm font-medium text-blue-900">
                  How to use this ticket
                </p>
                <p className="text-xs text-blue-700 mt-1">
                  Present this QR code at event check-in. Event staff will scan it to verify your registration.
                  You can also show your ticket code ({ticketCode}) for manual verification.
                </p>
              </div>
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
