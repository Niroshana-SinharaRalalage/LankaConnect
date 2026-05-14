'use client';

/**
 * Phase 6A.141 — Paid-event ticket check-in / QR scanner page.
 *
 * Organizer-only page accessed at `/events/{id}/manage/scan`. Opens the device
 * camera (via html5-qrcode), scans incoming QR codes, posts each to the backend
 * scan endpoint, and renders an accept/reject panel for gate staff.
 *
 * Architectural notes:
 * - F14 (camera-denied UX): if the browser denies camera permission, a yellow
 *   panel surfaces the manual-entry button prominently so the gate isn't blocked.
 * - F15 (cooldown keyed on lastScannedCode): the cooldown debounce uses
 *   `lastScannedCode == currentCode AND lastScanAt + 2s > now` so re-rendering
 *   phone-displayed QRs doesn't re-trigger a scan, but two different QRs in
 *   rapid succession both go through.
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
import type { ScanTicketResult } from '@/infrastructure/api/types/events.types';
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

const COOLDOWN_MS = 2000;

export default function ScanTicketPage({ params }: { params: Promise<{ id: string }> }) {
  const { id: eventId } = use(params);
  const router = useRouter();
  const { user } = useAuthStore();

  const { data: event, isLoading, error: fetchError } = useEventById(eventId);

  const scannerDivRef = useRef<HTMLDivElement>(null);
  const html5QrcodeRef = useRef<unknown>(null); // Html5Qrcode instance (any-shaped from dynamic import)
  const lastScanRef = useRef<{ code: string; at: number } | null>(null);

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

  const submitScan = useCallback(async (qrPayload: string) => {
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
  }, [eventId, playFeedback]);

  const submitManualScan = useCallback(async () => {
    const code = manualCode.trim();
    if (!code) return;
    setShowManualEntry(false);
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
  }, [eventId, manualCode, playFeedback]);

  const startCamera = useCallback(async () => {
    // Issue 2 fix: the viewfinder div is ALWAYS rendered (hidden via CSS when idle),
    // so scannerDivRef.current is never null on first click. The previous early-return
    // would no-op the click because the div was conditionally rendered.
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
          // F15: cooldown keyed on lastScannedCode + 2s window.
          const now = Date.now();
          const last = lastScanRef.current;
          if (last && last.code === decodedText && now - last.at < COOLDOWN_MS) {
            return; // same code in cooldown — ignore
          }
          lastScanRef.current = { code: decodedText, at: now };
          submitScan(decodedText);
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

        {/* Scanner viewfinder — ALWAYS mounted (Issue 2 fix); idle state hides via CSS so
            the camera button can grab a real ref synchronously on click. */}
        <div className="bg-white rounded-lg shadow-sm border p-4 mb-4">
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
            className={`w-full max-w-md mx-auto${outcome.kind === 'idle' ? ' hidden' : ''}`}
            data-testid="qr-scanner-region"
          />
        </div>

        {/* Result panel (only when not idle) */}
        {outcome.kind === 'accepted' && outcome.result && (
          <div
            data-testid="scan-result-accepted"
            className="bg-green-50 border-2 border-green-500 rounded-lg p-6 mb-4"
          >
            <div className="flex items-start gap-3">
              <CheckCircle2 className="w-10 h-10 text-green-600 flex-shrink-0" />
              <div className="flex-1">
                <h2 className="text-xl font-bold text-green-900">Accepted</h2>
                <p className="text-2xl font-bold text-green-900 mt-1">
                  {outcome.result.attendeeName ?? '(no name on registration)'}
                </p>
                {outcome.result.tier && (
                  <p className="text-green-700 font-medium">Tier: {outcome.result.tier}</p>
                )}
                {outcome.result.attendeeCount && outcome.result.attendeeCount > 1 && (
                  <p className="text-green-700 mt-1">
                    Party of {outcome.result.attendeeCount}
                    {outcome.result.tierBreakdown && outcome.result.tierBreakdown.length > 0 && (
                      <span>
                        {' '}({outcome.result.tierBreakdown.map((b) => `${b.count}× ${b.tier}`).join(', ')})
                      </span>
                    )}
                  </p>
                )}
                <p className="text-xs text-green-600 mt-2">
                  Ticket {outcome.result.ticketCode} • scanned by {outcome.result.scannedBy ?? 'you'}
                </p>
                {outcome.result.usedPreviousKey && (
                  <p className="text-xs text-amber-700 mt-1">
                    (Verified with rotated-out key — pre-rotation ticket)
                  </p>
                )}
              </div>
            </div>
          </div>
        )}

        {outcome.kind === 'rejected' && outcome.result && (
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
                  <p className="text-xs text-red-700 mt-2">
                    Ticket {outcome.result.ticketCode} for {outcome.result.attendeeName}
                  </p>
                )}
              </div>
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
          </div>
        )}

        {/* Issue 2.5 fix — distinct panel for server-side rejections (HTTP 400/500 etc.)
            so the operator knows to call a coordinator, not retry on the assumption it
            was a network blip. */}
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
