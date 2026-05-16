'use client';

/**
 * Phase 6A.141 — Paid-event ticket check-in / QR scanner page.
 *
 * Organizer-only page accessed at `/events/{id}/manage/scan`. Opens the device
 * camera (via html5-qrcode), scans an incoming QR code, posts it to the backend
 * scan endpoint, and renders an accept/reject panel for gate staff.
 *
 * Architectural notes:
 * - One-shot scan mode (UAT R2 Issue B): camera stops as soon as the first
 *   decode fires. The operator reads the result, then taps "Scan Next Ticket"
 *   to start the camera again. This prevents the mobile-scroll bug where a
 *   still-live camera re-decoded the same QR and flipped the panel to
 *   "Already scanned".
 * - F14 (camera-denied UX): if the browser denies camera permission, a yellow
 *   panel surfaces the manual-entry button prominently so the gate isn't blocked.
 * - F16 (code-split): html5-qrcode is dynamic-imported so the scanner-only
 *   library doesn't bloat the main manage-page bundle.
 * - HTTP-status semantics: accepted + rejected are both HTTP 200 (the body's
 *   `result` field is the discriminator). Network/server failures render a
 *   yellow "no network" panel, NOT the red rejected panel — false-accepts at
 *   the door are unacceptable; better to make the staff retry.
 */

import { use, useEffect, useRef, useState, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, Keyboard, QrCode, Camera, CheckCircle2, XCircle, WifiOff, AlertCircle, ServerCrash } from 'lucide-react';
import { LankaEventsHeader } from '@/presentation/components/layout/LankaEventsHeader';
import Footer from '@/presentation/components/layout/Footer';
import { Button } from '@/presentation/components/ui/Button';
import { useEventById } from '@/presentation/hooks/useEvents';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type { ScanTicketResult, AddOnSummary } from '@/infrastructure/api/types/events.types';
import { NetworkError } from '@/infrastructure/api/client/api-errors';

// html5-qrcode is dynamic-imported inside the start handler so the scanner-only
// library never enters the main manage-page bundle (F16).
type Html5QrcodeModule = typeof import('html5-qrcode');

interface ScanOutcome {
  kind: 'idle' | 'scanning' | 'accepted' | 'rejected' | 'network-loss' | 'server-error' | 'camera-denied';
  result?: ScanTicketResult;
  errorMessage?: string;
}

/**
 * Issue 2.5 fix — classify a thrown scan error so the UI shows the right panel.
 *   NetworkError (true wire failure) → yellow "Cannot reach server"
 *   Any other ApiError (4xx/5xx including 400 InvalidOperation from backend) →
 *     yellow "Server rejected the scan" with the backend's actual message.
 * In all cases the ticket is NOT marked scanned — door stays closed.
 */
function classifyScanError(err: unknown): { kind: 'network-loss' | 'server-error'; errorMessage: string } {
  if (err instanceof NetworkError) {
    return { kind: 'network-loss', errorMessage: err.message };
  }
  if (err instanceof Error) {
    return { kind: 'server-error', errorMessage: err.message };
  }
  return { kind: 'server-error', errorMessage: 'Unknown error from server' };
}

/**
 * Format a per-attendee price for display. Returns a localized currency string
 * when both amount and currency are present, else null (caller hides the row).
 * USD example: $100.00; LKR example: LKR 5,000.00. Falls back to plain numeric
 * if Intl.NumberFormat doesn't recognize the currency code.
 */
function formatPrice(amount?: number | null, currency?: string | null): string | null {
  if (amount === null || amount === undefined || !currency) return null;
  try {
    return new Intl.NumberFormat(undefined, { style: 'currency', currency }).format(amount);
  } catch {
    return `${currency} ${amount.toFixed(2)}`;
  }
}

/**
 * Shared attendee-details block — rendered identically on the accepted panel and
 * on the four ticket-resolved rejection panels (already_scanned, expired,
 * invalidated, wrong_event).
 *
 * UAT R2 Issue A: operator wants to see who the ticket belongs to regardless of
 * accept/reject. `accent` picks the color palette to match the surrounding panel.
 *
 * UAT R3: when `result.attendees` is non-empty, render a scrollable per-attendee
 * sub-block below the header lines — each row shows name, age category, gender,
 * tier, and per-attendee price. For head-count-mode tickets (no Attendees in DTO)
 * the UI falls through to the legacy aggregate lines only — preserving backwards
 * compatible behavior for older registrations.
 */
function AttendeeBlock({ result, accent }: { result: ScanTicketResult; accent: 'green' | 'amber' | 'red' }) {
  const nameCls = accent === 'green' ? 'text-green-900' : accent === 'amber' ? 'text-amber-900' : 'text-red-900';
  const subCls = accent === 'green' ? 'text-green-700' : accent === 'amber' ? 'text-amber-800' : 'text-red-700';
  const metaCls = accent === 'green' ? 'text-green-600' : accent === 'amber' ? 'text-amber-700' : 'text-red-600';
  const rowBorderCls = accent === 'green' ? 'border-green-200' : accent === 'amber' ? 'border-amber-300' : 'border-red-200';
  const rowSubCls = accent === 'green' ? 'text-green-800' : accent === 'amber' ? 'text-amber-900' : 'text-red-800';

  const hasAttendees = !!(result.attendees && result.attendees.length > 0);

  return (
    <>
      {/* Header lines — primary contact name + tier + party-of-N. Preserved verbatim
          for backwards compat with head-count tickets and the existing UAT R2 shape. */}
      <p className={`text-2xl font-bold mt-1 ${nameCls}`}>
        {result.attendeeName ?? '(no name on registration)'}
      </p>
      {result.tier && (
        <p className={`${subCls} font-medium`}>Tier: {result.tier}</p>
      )}
      {result.attendeeCount && result.attendeeCount > 1 && (
        <p className={`${subCls} mt-1`}>
          Party of {result.attendeeCount}
          {result.tierBreakdown && result.tierBreakdown.length > 0 && (
            <span> ({result.tierBreakdown.map((b) => `${b.count}× ${b.tier}`).join(', ')})</span>
          )}
        </p>
      )}

      {/* UAT R3 — per-attendee detail list. Bounded height + inner scroll so a
          party of 10 doesn't push "Scan Next Ticket" off the mobile screen. */}
      {hasAttendees && (
        <div
          className={`mt-3 max-h-64 overflow-y-auto rounded border ${rowBorderCls} bg-white/40 divide-y ${rowBorderCls}`}
          data-testid="scan-attendee-list"
        >
          {result.attendees!.map((a, idx) => {
            const priceLabel = formatPrice(a.priceAmount, a.priceCurrency);
            return (
              <div key={idx} className="px-3 py-2 text-sm" data-testid={`scan-attendee-row-${idx}`}>
                <div className={`font-medium ${nameCls}`}>{a.name}</div>
                <div className={`text-xs ${rowSubCls}`}>
                  {a.ageCategory}
                  {a.gender && <> • {a.gender}</>}
                  {a.ticketTierName && <> • {a.ticketTierName}</>}
                  {a.seatLabel && <> • Seat {a.seatLabel}</>}
                  {priceLabel && <> • {priceLabel}</>}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {result.ticketCode && (
        <p className={`text-xs mt-2 ${metaCls}`}>
          Ticket {result.ticketCode}
          {result.scannedBy && <> • scanned by {result.scannedBy}</>}
        </p>
      )}
    </>
  );
}

/**
 * UAT R4 — confirmed-bundled add-ons for the scanned ticket. Rendered as a
 * separate sub-block below AttendeeBlock so the operator can see what extras
 * the attendee paid for (e.g. "Dinner Add-on x1 — $5.00"). Hidden entirely
 * when the server returned a null/empty addOns array — no empty card noise.
 */
function AddOnsBlock({ addOns, accent }: { addOns: AddOnSummary[] | null | undefined; accent: 'green' | 'amber' | 'red' }) {
  if (!addOns || addOns.length === 0) return null;
  const borderCls = accent === 'green' ? 'border-green-200' : accent === 'amber' ? 'border-amber-300' : 'border-red-200';
  const labelCls = accent === 'green' ? 'text-green-700' : accent === 'amber' ? 'text-amber-800' : 'text-red-700';
  const nameCls = accent === 'green' ? 'text-green-900' : accent === 'amber' ? 'text-amber-900' : 'text-red-900';
  return (
    <div className={`mt-3 rounded border ${borderCls} bg-white/40`} data-testid="scan-addons-list">
      <p className={`px-3 pt-2 pb-1 text-xs font-semibold uppercase tracking-wide ${labelCls}`}>
        Add-ons
      </p>
      <div className={`divide-y ${borderCls}`}>
        {addOns.map((a, idx) => {
          const total = formatPrice(a.totalAmount, a.currency);
          const unit = formatPrice(a.unitPrice, a.currency);
          return (
            <div key={idx} className="px-3 py-2 flex justify-between items-baseline text-sm" data-testid={`scan-addon-row-${idx}`}>
              <div className={`font-medium ${nameCls}`}>
                {a.name}
                {a.quantity > 1 && <span className={`ml-1 text-xs ${labelCls}`}>×{a.quantity}</span>}
              </div>
              <div className={`text-xs ${labelCls}`}>
                {a.quantity > 1 && unit && <span className="mr-2">{unit} ea</span>}
                {total && <span className={`font-medium ${nameCls}`}>{total}</span>}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

export default function ScanTicketPage({ params }: { params: Promise<{ id: string }> }) {
  const { id: eventId } = use(params);
  const router = useRouter();
  const { user } = useAuthStore();

  const { data: event, isLoading, error: fetchError } = useEventById(eventId);

  const scannerDivRef = useRef<HTMLDivElement>(null);
  const html5QrcodeRef = useRef<unknown>(null); // Html5Qrcode instance (any-shaped from dynamic import)
  // One-shot scan guard: flipped true on first decode to swallow stray re-decodes
  // that fire during the ~100ms window between decode callback and stopCamera() resolve.
  const decodedRef = useRef<boolean>(false);

  const [outcome, setOutcome] = useState<ScanOutcome>({ kind: 'idle' });
  const [showManualEntry, setShowManualEntry] = useState(false);
  const [manualCode, setManualCode] = useState('');
  const [audioEnabled, setAudioEnabled] = useState(true);

  // ============================================================
  // Authorization gate
  // ============================================================

  useEffect(() => {
    if (!isLoading && event && event.isCurrentUserOrganizer !== true) {
      // Not authorized — bounce to event detail.
      router.replace(`/events/${eventId}`);
    }
  }, [isLoading, event, eventId, router]);

  // ============================================================
  // Camera scanner lifecycle
  // ============================================================

  const playFeedback = useCallback((accepted: boolean) => {
    if (!audioEnabled) return;
    try {
      // Lightweight WebAudio beep (no external assets to load).
      const ctx = new (window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext)();
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.frequency.value = accepted ? 880 : 220; // hi pitch for accept, lo for reject
      gain.gain.value = 0.1;
      osc.connect(gain);
      gain.connect(ctx.destination);
      osc.start();
      osc.stop(ctx.currentTime + 0.15);
    } catch {
      // No audio context (older browsers / silent on some iOS). Vibrate-only fallback.
    }
    if ('vibrate' in navigator) {
      navigator.vibrate(accepted ? 200 : [100, 50, 100]);
    }
  }, [audioEnabled]);

  const stopCamera = useCallback(async () => {
    try {
      if (html5QrcodeRef.current) {
        const inst = html5QrcodeRef.current as { stop: () => Promise<void>; clear: () => void };
        await inst.stop();
        inst.clear();
        html5QrcodeRef.current = null;
      }
    } catch (err) {
      console.warn('[scan] camera stop failed (often safe to ignore)', err);
    }
  }, []);

  const submitScan = useCallback(async (qrPayload: string) => {
    // One-shot mode: stop the camera BEFORE the API call so it can't re-decode
    // the same QR while we're waiting on the network. decodedRef already swallows
    // any stray callbacks fired in the gap; this also blacks out the viewfinder.
    await stopCamera();
    setOutcome({ kind: 'scanning' });
    try {
      const result = await eventsRepository.scanTicket(eventId, qrPayload);
      if (result.result === 'accepted') {
        setOutcome({ kind: 'accepted', result });
        playFeedback(true);
      } else {
        setOutcome({ kind: 'rejected', result });
        playFeedback(false);
      }
    } catch (err) {
      // Issue 2.5 fix: differentiate true network failure from server-side rejection (400/500).
      // Both keep the door closed (no green panel) but show distinct operator messaging.
      console.error('[scan] error', err);
      setOutcome(classifyScanError(err));
    }
  }, [eventId, playFeedback, stopCamera]);

  const submitManualScan = useCallback(async () => {
    const code = manualCode.trim();
    if (!code) return;
    setShowManualEntry(false);
    // Stop the camera if it happened to be running — manual entry takes over.
    await stopCamera();
    setOutcome({ kind: 'scanning' });
    try {
      const result = await eventsRepository.scanTicketByCode(eventId, code);
      if (result.result === 'accepted') {
        setOutcome({ kind: 'accepted', result });
        playFeedback(true);
      } else {
        setOutcome({ kind: 'rejected', result });
        playFeedback(false);
      }
    } catch (err) {
      console.error('[scan] manual-entry error', err);
      setOutcome(classifyScanError(err));
    } finally {
      setManualCode('');
    }
  }, [eventId, manualCode, playFeedback, stopCamera]);

  const startCamera = useCallback(async () => {
    // One-shot guard: reset the flag every time we (re)start the camera so the
    // next decode is accepted. Without this, "Scan Next Ticket" would no-op.
    decodedRef.current = false;
    // The viewfinder div is ALWAYS mounted (hidden via CSS when idle/terminal),
    // so scannerDivRef.current is never null on first click.
    setOutcome({ kind: 'scanning' });
    if (!scannerDivRef.current) {
      // Should never happen now, but stay defensive — log instead of silent return.
      console.error('[scan] viewfinder div not yet mounted; aborting camera start');
      return;
    }

    try {
      // F16: dynamic import — html5-qrcode loads on first start, not at page load.
      const mod: Html5QrcodeModule = await import('html5-qrcode');
      const Html5Qrcode = mod.Html5Qrcode;

      const scanner = new Html5Qrcode(scannerDivRef.current.id);
      html5QrcodeRef.current = scanner;

      await scanner.start(
        { facingMode: 'environment' },
        {
          fps: 10,
          qrbox: { width: 250, height: 250 },
        },
        (decodedText) => {
          // One-shot mode: swallow any decode after the first. The decoder can
          // fire 2-3 times in the ~100ms window before stopCamera() actually
          // releases the video stream, so we set the flag synchronously here.
          if (decodedRef.current) return;
          decodedRef.current = true;
          void submitScan(decodedText);
        },
        () => {
          // per-frame "no QR detected" — silent, expected.
        }
      );
    } catch (err) {
      console.error('[scan] camera start failed', err);
      // F14: camera-denied / no-camera-available → yellow panel with manual entry CTA.
      setOutcome({
        kind: 'camera-denied',
        errorMessage: err instanceof Error ? err.message : 'Camera unavailable',
      });
    }
  }, [submitScan]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      void stopCamera();
    };
  }, [stopCamera]);

  // ============================================================
  // Render guards
  // ============================================================

  if (isLoading) {
    return (
      <div className="min-h-screen bg-neutral-50 flex items-center justify-center">
        <p className="text-neutral-600">Loading event…</p>
      </div>
    );
  }
  if (fetchError || !event) {
    return (
      <div className="min-h-screen bg-neutral-50 flex items-center justify-center">
        <p className="text-red-600">Event not found.</p>
      </div>
    );
  }
  if (!user || event.isCurrentUserOrganizer !== true) {
    // Brief flash before redirect kicks in
    return (
      <div className="min-h-screen bg-neutral-50 flex items-center justify-center">
        <p className="text-neutral-600">Authorization required…</p>
      </div>
    );
  }

  // ============================================================
  // Page
  // ============================================================

  return (
    <div className="min-h-screen bg-neutral-50 flex flex-col">
      <LankaEventsHeader />

      <main className="flex-1 max-w-3xl mx-auto w-full px-4 py-6">
        {/* Top bar */}
        <div className="flex items-center justify-between mb-4">
          <Button
            variant="outline"
            size="sm"
            onClick={() => router.push(`/events/${eventId}/manage`)}
          >
            <ArrowLeft className="w-4 h-4 mr-1" /> Back to Manage
          </Button>
          <div className="flex items-center gap-2">
            <label className="text-xs text-neutral-600 flex items-center gap-1 cursor-pointer">
              <input
                type="checkbox"
                checked={audioEnabled}
                onChange={(e) => setAudioEnabled(e.target.checked)}
              />
              Sound + vibrate
            </label>
          </div>
        </div>

        <h1 className="text-2xl font-bold mb-1 flex items-center gap-2">
          <QrCode className="w-6 h-6 text-primary" /> Scan Tickets
        </h1>
        <p className="text-neutral-600 mb-6">
          Scanning attendees into <span className="font-medium">{event.title}</span>.
          Point the camera at the QR on the attendee's ticket. Tap "Enter code manually" for damaged or unscannable codes.
        </p>

        {/* Scanner viewfinder card — ALWAYS mounted to keep the scanner div ref valid
            across state transitions, but hidden via CSS on terminal states so the
            result panel dominates the mobile screen (UAT R2 Issue B). */}
        <div
          className={`bg-white rounded-lg shadow-sm border p-4 mb-4${
            outcome.kind !== 'idle' && outcome.kind !== 'scanning' ? ' hidden' : ''
          }`}
        >
          {outcome.kind === 'idle' && (
            <div className="text-center py-8">
              <Camera className="w-12 h-12 mx-auto text-neutral-400 mb-3" />
              <p className="text-neutral-600 mb-4">Press "Start Scanning" to open the camera.</p>
              <Button onClick={startCamera} size="lg" data-testid="scan-start-button">
                <Camera className="w-5 h-5 mr-2" /> Start Scanning
              </Button>
            </div>
          )}
          <div
            id="qr-scanner-region"
            ref={scannerDivRef}
            className={`w-full max-w-md mx-auto${outcome.kind === 'scanning' ? '' : ' hidden'}`}
            data-testid="qr-scanner-region"
          />
        </div>

        {/* Result panel (only when terminal) */}
        {outcome.kind === 'accepted' && outcome.result && (
          <div
            data-testid="scan-result-accepted"
            className="bg-green-50 border-2 border-green-500 rounded-lg p-6 mb-4"
          >
            <div className="flex items-start gap-3">
              <CheckCircle2 className="w-10 h-10 text-green-600 flex-shrink-0" />
              <div className="flex-1">
                <h2 className="text-xl font-bold text-green-900">Accepted</h2>
                <AttendeeBlock result={outcome.result} accent="green" />
                <AddOnsBlock addOns={outcome.result.addOns} accent="green" />
                {outcome.result.usedPreviousKey && (
                  <p className="text-xs text-amber-700 mt-1">
                    (Verified with rotated-out key — pre-rotation ticket)
                  </p>
                )}
              </div>
            </div>
            <div className="mt-4 flex justify-center">
              <Button onClick={startCamera} size="lg" data-testid="scan-next-from-accepted">
                <Camera className="w-5 h-5 mr-2" /> Scan Next Ticket
              </Button>
            </div>
          </div>
        )}

        {/* Already-scanned: amber panel with CheckCircle2 — operator wants to see the
            attendee was a legitimate ticket-holder, just re-presenting. UAT R2 Issue A. */}
        {outcome.kind === 'rejected' && outcome.result?.reason === 'already_scanned' && (
          <div
            data-testid="scan-result-already-scanned"
            className="bg-amber-50 border-2 border-amber-500 rounded-lg p-6 mb-4"
          >
            <div className="flex items-start gap-3">
              <CheckCircle2 className="w-10 h-10 text-amber-600 flex-shrink-0" />
              <div className="flex-1">
                <h2 className="text-xl font-bold text-amber-900">
                  Already Scanned
                  {outcome.result.previousScanCount && outcome.result.previousScanCount > 0 && (
                    <span> ({outcome.result.previousScanCount}×)</span>
                  )}
                </h2>
                {outcome.result.scannedAt && (
                  <p className="text-amber-800 mt-1 text-sm">
                    First admitted {new Date(outcome.result.scannedAt).toLocaleString()}
                    {outcome.result.previousScannedBy && (
                      <> by {outcome.result.previousScannedBy}</>
                    )}
                  </p>
                )}
                <AttendeeBlock result={outcome.result} accent="amber" />
                <AddOnsBlock addOns={outcome.result.addOns} accent="amber" />
              </div>
            </div>
            <div className="mt-4 flex justify-center">
              <Button onClick={startCamera} size="lg" data-testid="scan-next-from-already-scanned">
                <Camera className="w-5 h-5 mr-2" /> Scan Next Ticket
              </Button>
            </div>
          </div>
        )}

        {/* Other rejections (expired, invalidated, wrong_event, invalid_signature,
            malformed_payload, ticket_not_found, malformed_request) — red panel. */}
        {outcome.kind === 'rejected' && outcome.result && outcome.result.reason !== 'already_scanned' && (
          <div
            data-testid="scan-result-rejected"
            className="bg-red-50 border-2 border-red-500 rounded-lg p-6 mb-4"
          >
            <div className="flex items-start gap-3">
              <XCircle className="w-10 h-10 text-red-600 flex-shrink-0" />
              <div className="flex-1">
                <h2 className="text-xl font-bold text-red-900">Rejected</h2>
                <p className="text-red-800 mt-1">
                  {outcome.result.reasonMessage ?? 'This ticket cannot be accepted.'}
                </p>
                {outcome.result.attendeeName && (
                  <AttendeeBlock result={outcome.result} accent="red" />
                )}
                <AddOnsBlock addOns={outcome.result.addOns} accent="red" />
              </div>
            </div>
            <div className="mt-4 flex justify-center">
              <Button onClick={startCamera} size="lg" data-testid="scan-next-from-rejected">
                <Camera className="w-5 h-5 mr-2" /> Scan Next Ticket
              </Button>
            </div>
          </div>
        )}

        {outcome.kind === 'network-loss' && (
          <div
            data-testid="scan-result-network-loss"
            className="bg-yellow-50 border-2 border-yellow-500 rounded-lg p-6 mb-4"
          >
            <div className="flex items-start gap-3">
              <WifiOff className="w-10 h-10 text-yellow-600 flex-shrink-0" />
              <div className="flex-1">
                <h2 className="text-xl font-bold text-yellow-900">Cannot reach server</h2>
                <p className="text-yellow-800 mt-1">
                  Check your network connection and try scanning again. The ticket has NOT been marked as
                  scanned — do not let the attendee through yet.
                </p>
                {outcome.errorMessage && (
                  <p className="text-xs text-yellow-700 mt-2 font-mono">{outcome.errorMessage}</p>
                )}
              </div>
            </div>
            <div className="mt-4 flex justify-center">
              <Button onClick={startCamera} size="lg" data-testid="scan-next-from-network-loss">
                <Camera className="w-5 h-5 mr-2" /> Scan Next Ticket
              </Button>
            </div>
          </div>
        )}

        {/* Distinct panel for server-side rejections (HTTP 400/500 etc.) so the
            operator knows to call a coordinator, not retry blindly. */}
        {outcome.kind === 'server-error' && (
          <div
            data-testid="scan-result-server-error"
            className="bg-yellow-50 border-2 border-yellow-500 rounded-lg p-6 mb-4"
          >
            <div className="flex items-start gap-3">
              <ServerCrash className="w-10 h-10 text-yellow-600 flex-shrink-0" />
              <div className="flex-1">
                <h2 className="text-xl font-bold text-yellow-900">Server rejected the scan</h2>
                <p className="text-yellow-800 mt-1">
                  Something went wrong on our end and the ticket was NOT marked as scanned. Try once more, and
                  if it still fails, please contact the event coordinator instead of letting the attendee through.
                </p>
                {outcome.errorMessage && (
                  <p className="text-xs text-yellow-700 mt-2 font-mono">{outcome.errorMessage}</p>
                )}
              </div>
            </div>
            <div className="mt-4 flex justify-center">
              <Button onClick={startCamera} size="lg" data-testid="scan-next-from-server-error">
                <Camera className="w-5 h-5 mr-2" /> Scan Next Ticket
              </Button>
            </div>
          </div>
        )}

        {outcome.kind === 'camera-denied' && (
          <div
            data-testid="scan-result-camera-denied"
            className="bg-yellow-50 border-2 border-yellow-500 rounded-lg p-6 mb-4"
          >
            <div className="flex items-start gap-3">
              <AlertCircle className="w-10 h-10 text-yellow-600 flex-shrink-0" />
              <div className="flex-1">
                <h2 className="text-xl font-bold text-yellow-900">Camera unavailable</h2>
                <p className="text-yellow-800 mt-1">
                  We couldn't access your camera. Either permission was denied, or this device has no
                  camera. You can still check tickets in by typing the code manually below.
                </p>
                <Button
                  onClick={() => setShowManualEntry(true)}
                  className="mt-3"
                  data-testid="scan-manual-entry-from-denied"
                >
                  <Keyboard className="w-4 h-4 mr-1" /> Enter ticket code manually
                </Button>
              </div>
            </div>
          </div>
        )}

        {/* Manual entry CTA — always visible at the bottom */}
        <div className="flex justify-center mb-4">
          <button
            type="button"
            onClick={() => setShowManualEntry(true)}
            className="text-sm text-primary hover:underline flex items-center gap-1"
            data-testid="scan-manual-entry-cta"
          >
            <Keyboard className="w-4 h-4" /> Enter ticket code manually
          </button>
        </div>
      </main>

      <Footer />

      {/* Manual-entry modal */}
      {showManualEntry && (
        <div
          className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4"
          data-testid="scan-manual-entry-modal"
        >
          <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
            <h2 className="text-lg font-bold mb-3">Enter ticket code</h2>
            <p className="text-sm text-neutral-600 mb-3">
              Type the ticket code shown beneath the QR (format: LC-YYYY-XXXXXX).
            </p>
            <input
              type="text"
              value={manualCode}
              onChange={(e) => setManualCode(e.target.value.toUpperCase())}
              placeholder="LC-2026-ABC123"
              className="w-full border rounded px-3 py-2 font-mono mb-4"
              autoFocus
              data-testid="scan-manual-entry-input"
            />
            <div className="flex gap-2 justify-end">
              <Button
                variant="outline"
                onClick={() => {
                  setShowManualEntry(false);
                  setManualCode('');
                }}
              >
                Cancel
              </Button>
              <Button
                onClick={submitManualScan}
                disabled={!manualCode.trim()}
                data-testid="scan-manual-entry-submit"
              >
                Submit
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
