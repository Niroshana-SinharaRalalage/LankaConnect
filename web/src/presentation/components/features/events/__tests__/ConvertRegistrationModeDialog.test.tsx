import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ConvertRegistrationModeDialog } from '../ConvertRegistrationModeDialog';
import { RegistrationMode } from '@/infrastructure/api/types/events.types';

/**
 * Phase 7F-B.5 — RTL coverage for the mode-conversion confirmation dialog.
 *
 * Architect-required behaviours:
 *   - Opens with a dry-run preview fired against the API.
 *   - Cancel does NOT call the commit endpoint.
 *   - Confirm fires a second call with `dryRun: false` and bubbles the result up.
 *   - When every active registration would be skipped, Confirm is disabled.
 */

const mutateAsyncMock = vi.fn();
vi.mock('@/presentation/hooks/useEvents', () => ({
  useConvertRegistrationMode: () => ({ mutateAsync: mutateAsyncMock }),
}));

function renderWithClient(ui: React.ReactElement) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

beforeEach(() => {
  mutateAsyncMock.mockReset();
});

describe('ConvertRegistrationModeDialog — Phase 7F-B.5', () => {
  it('fires dry-run preview when opened and renders the migrated/skipped counts', async () => {
    mutateAsyncMock.mockResolvedValueOnce({
      aggregateConversionId: null,
      totalProcessed: 3,
      migratedCount: 2,
      skippedCount: 1,
      migrated: [],
      skipped: [
        {
          registrationId: 'reg-1',
          reasonCode: 'PendingAdditionMustResolveFirst',
          reason: 'Pending payment must complete first',
        },
      ],
      wasDryRun: true,
    });

    renderWithClient(
      <ConvertRegistrationModeDialog
        open
        onOpenChange={() => {}}
        eventId="evt-1"
        fromMode={RegistrationMode.DetailedAttendees}
        targetMode={RegistrationMode.HeadCountByAge}
      />,
    );

    // First call should be dry-run
    await waitFor(() => expect(mutateAsyncMock).toHaveBeenCalled());
    expect(mutateAsyncMock).toHaveBeenCalledWith({
      eventId: 'evt-1',
      payload: {
        targetMode: RegistrationMode.HeadCountByAge,
        dryRun: true,
        notifyAttendees: false,
      },
    });

    // Counts visible — use the unique "Will be skipped" label
    expect(await screen.findByText('Will be skipped')).toBeInTheDocument();
    expect(screen.getByText('Will be migrated')).toBeInTheDocument();
    expect(screen.getByText(/PendingAdditionMustResolveFirst/i)).toBeInTheDocument();
    // 3 active registrations summary
    expect(screen.getByText(/3 active registrations reviewed/i)).toBeInTheDocument();
  });

  it('Cancel does NOT trigger a second commit call', async () => {
    mutateAsyncMock.mockResolvedValueOnce({
      aggregateConversionId: null,
      totalProcessed: 1,
      migratedCount: 1,
      skippedCount: 0,
      migrated: [],
      skipped: [],
      wasDryRun: true,
    });
    const onOpenChange = vi.fn();

    renderWithClient(
      <ConvertRegistrationModeDialog
        open
        onOpenChange={onOpenChange}
        eventId="evt-1"
        fromMode={RegistrationMode.DetailedAttendees}
        targetMode={RegistrationMode.HeadCountOnly}
      />,
    );

    await waitFor(() => expect(mutateAsyncMock).toHaveBeenCalledTimes(1));
    fireEvent.click(screen.getByRole('button', { name: /Cancel/i }));

    expect(onOpenChange).toHaveBeenCalledWith(false);
    expect(mutateAsyncMock).toHaveBeenCalledTimes(1); // still only the preview, no commit
  });

  it('Confirm fires the commit call (dryRun=false) and bubbles the result via onComplete', async () => {
    // Two responses: dry-run preview + real commit
    mutateAsyncMock.mockResolvedValueOnce({
      aggregateConversionId: null,
      totalProcessed: 2,
      migratedCount: 2,
      skippedCount: 0,
      migrated: [],
      skipped: [],
      wasDryRun: true,
    });
    mutateAsyncMock.mockResolvedValueOnce({
      aggregateConversionId: 'agg-1',
      totalProcessed: 2,
      migratedCount: 2,
      skippedCount: 0,
      migrated: [],
      skipped: [],
      wasDryRun: false,
    });
    const onComplete = vi.fn();

    renderWithClient(
      <ConvertRegistrationModeDialog
        open
        onOpenChange={() => {}}
        eventId="evt-1"
        fromMode={RegistrationMode.DetailedAttendees}
        targetMode={RegistrationMode.HeadCountOnly}
        onComplete={onComplete}
      />,
    );

    await waitFor(() => expect(mutateAsyncMock).toHaveBeenCalledTimes(1));
    fireEvent.click(screen.getByRole('button', { name: /Confirm & Convert/i }));

    await waitFor(() => expect(mutateAsyncMock).toHaveBeenCalledTimes(2));
    expect(mutateAsyncMock.mock.calls[1][0]).toEqual({
      eventId: 'evt-1',
      payload: {
        targetMode: RegistrationMode.HeadCountOnly,
        dryRun: false,
        notifyAttendees: false,
      },
    });
    expect(onComplete).toHaveBeenCalledWith(
      expect.objectContaining({ aggregateConversionId: 'agg-1', migratedCount: 2 }),
    );
  });

  it('disables Confirm when every active registration would be skipped', async () => {
    mutateAsyncMock.mockResolvedValueOnce({
      aggregateConversionId: null,
      totalProcessed: 2,
      migratedCount: 0,
      skippedCount: 2,
      migrated: [],
      skipped: [
        {
          registrationId: 'r1',
          reasonCode: 'PendingAdditionMustResolveFirst',
          reason: 'Pending payment',
        },
        {
          registrationId: 'r2',
          reasonCode: 'NamedSeatsRequireDetailedAttendees',
          reason: 'Named seat assigned',
        },
      ],
      wasDryRun: true,
    });

    renderWithClient(
      <ConvertRegistrationModeDialog
        open
        onOpenChange={() => {}}
        eventId="evt-1"
        fromMode={RegistrationMode.DetailedAttendees}
        targetMode={RegistrationMode.HeadCountByGender}
      />,
    );

    await waitFor(() => expect(mutateAsyncMock).toHaveBeenCalled());
    const confirm = await screen.findByRole('button', { name: /Confirm & Convert/i });
    expect(confirm).toBeDisabled();
    expect(screen.getByText(/Every active registration would be skipped/i)).toBeInTheDocument();
  });
});
