/**
 * Slice 6 Chunk S6.7 — PresetLibraryModal tests.
 *
 * Focus: renders preset grid, forwards onSelect, surfaces loading / error /
 * empty / selecting states. The hook is mocked so we only test the
 * component's presentation and event wiring.
 */

import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';

vi.mock('@/presentation/hooks/useVenueLayouts', () => ({
  useLayoutPresets: vi.fn(),
  useUserTemplates: vi.fn(),
}));

// next/image expects Next's runtime; shim it to a plain <img>.
vi.mock('next/image', () => ({
  __esModule: true,
  default: (props: React.ImgHTMLAttributes<HTMLImageElement>) =>
    React.createElement('img', props),
}));

import { useLayoutPresets, useUserTemplates } from '@/presentation/hooks/useVenueLayouts';
import { PresetLibraryModal } from '@/presentation/components/features/events/PresetLibraryModal';
import type { LayoutPresetDto, VenueLayoutDto } from '@/infrastructure/api/types/events.types';

const mockUseLayoutPresets = useLayoutPresets as unknown as ReturnType<typeof vi.fn>;
const mockUseUserTemplates = useUserTemplates as unknown as ReturnType<typeof vi.fn>;

/** Default templates-hook stub: empty list, idle state. Individual tests override. */
function setUseUserTemplatesIdle() {
  mockUseUserTemplates.mockReturnValue({
    data: [],
    isLoading: false,
    isError: false,
    error: null,
    refetch: vi.fn(),
  });
}

const fakePresets: LayoutPresetDto[] = [
  {
    id: 'theater-classic',
    name: 'Theater Classic',
    description: '10 rows × 20 seats facing a central stage.',
    layoutType: 'Theater',
    totalCapacity: 200,
    thumbnailUrl: '/layouts/presets/theater-classic.svg',
  },
  {
    id: 'banquet-round-8',
    name: 'Banquet · 15 Round Tables × 8',
    description: '15 round tables, 8 seats each.',
    layoutType: 'Banquet',
    totalCapacity: 120,
    thumbnailUrl: '/layouts/presets/banquet-round-8.svg',
  },
];

function mountLoaded(overrides: Partial<Parameters<typeof PresetLibraryModal>[0]> = {}) {
  mockUseLayoutPresets.mockReturnValue({
    data: fakePresets,
    isLoading: false,
    isError: false,
    error: null,
    refetch: vi.fn(),
  });
  const onOpenChange = vi.fn();
  const onSelect = vi.fn();
  render(
    <PresetLibraryModal
      open
      onOpenChange={onOpenChange}
      onSelect={onSelect}
      {...overrides}
    />,
  );
  return { onOpenChange, onSelect };
}

beforeEach(() => {
  vi.clearAllMocks();
  // Default the user-templates hook so existing tests (which only target the
  // built-in tab) don't crash if the modal renders the Mine tabpanel ever.
  setUseUserTemplatesIdle();
});

describe('PresetLibraryModal', () => {
  it('does not fetch presets while closed', () => {
    mockUseLayoutPresets.mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: false,
      error: null,
      refetch: vi.fn(),
    });

    render(
      <PresetLibraryModal
        open={false}
        onOpenChange={vi.fn()}
        onSelect={vi.fn()}
      />,
    );

    expect(mockUseLayoutPresets).toHaveBeenCalledWith({ enabled: false });
  });

  it('renders a card per preset with name, capacity, layout type, and description', () => {
    mountLoaded();

    expect(screen.getByText('Theater Classic')).toBeInTheDocument();
    expect(screen.getByText('Banquet · 15 Round Tables × 8')).toBeInTheDocument();
    expect(screen.getByText('200 seats')).toBeInTheDocument();
    expect(screen.getByText('120 seats')).toBeInTheDocument();
    expect(screen.getByText('Theater')).toBeInTheDocument();
    expect(screen.getByText('Banquet')).toBeInTheDocument();
    expect(
      screen.getByText('10 rows × 20 seats facing a central stage.'),
    ).toBeInTheDocument();
  });

  it('uses thumbnailUrl as the card image src', () => {
    mountLoaded();

    const theaterImg = screen.getByAltText('Theater Classic') as HTMLImageElement;
    // next/image is shimmed to <img>; src attribute is forwarded directly.
    expect(theaterImg.getAttribute('src')).toBe('/layouts/presets/theater-classic.svg');
  });

  it('calls onSelect with the full preset when a card is clicked', async () => {
    const { onSelect } = mountLoaded();

    fireEvent.click(screen.getByTestId('preset-card-banquet-round-8'));

    await waitFor(() => expect(onSelect).toHaveBeenCalledTimes(1));
    expect(onSelect).toHaveBeenCalledWith(fakePresets[1]);
  });

  it('shows spinner only on the selecting card, disables the others', () => {
    mountLoaded({
      isSelecting: true,
      selectingPresetId: 'theater-classic',
    });

    expect(
      screen.getByTestId('preset-card-spinner-theater-classic'),
    ).toBeInTheDocument();
    expect(
      screen.queryByTestId('preset-card-spinner-banquet-round-8'),
    ).not.toBeInTheDocument();
    expect(
      screen.getByTestId('preset-card-banquet-round-8'),
    ).toBeDisabled();
    // The selecting card itself is NOT disabled — user already committed to it.
    expect(
      screen.getByTestId('preset-card-theater-classic'),
    ).not.toBeDisabled();
  });

  it('renders the loading state while isLoading is true', () => {
    mockUseLayoutPresets.mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
      error: null,
      refetch: vi.fn(),
    });
    render(
      <PresetLibraryModal open onOpenChange={vi.fn()} onSelect={vi.fn()} />,
    );

    expect(screen.getByTestId('preset-modal-loading')).toBeInTheDocument();
  });

  it('renders the error state with a retry button that calls refetch', () => {
    const refetch = vi.fn();
    mockUseLayoutPresets.mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: true,
      error: new Error('Network down'),
      refetch,
    });
    render(
      <PresetLibraryModal open onOpenChange={vi.fn()} onSelect={vi.fn()} />,
    );

    expect(screen.getByTestId('preset-modal-error')).toBeInTheDocument();
    expect(screen.getByText(/Network down/)).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /try again/i }));
    expect(refetch).toHaveBeenCalledTimes(1);
  });

  it('renders an empty-state message when presets array is empty', () => {
    mockUseLayoutPresets.mockReturnValue({
      data: [],
      isLoading: false,
      isError: false,
      error: null,
      refetch: vi.fn(),
    });
    render(
      <PresetLibraryModal open onOpenChange={vi.fn()} onSelect={vi.fn()} />,
    );

    expect(screen.getByText(/no presets are available/i)).toBeInTheDocument();
  });

  it('swallows onSelect rejections so the modal stays usable', async () => {
    const errorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
    mockUseLayoutPresets.mockReturnValue({
      data: fakePresets,
      isLoading: false,
      isError: false,
      error: null,
      refetch: vi.fn(),
    });
    const rejectingSelect = vi.fn().mockRejectedValue(new Error('boom'));
    render(
      <PresetLibraryModal
        open
        onOpenChange={vi.fn()}
        onSelect={rejectingSelect}
      />,
    );

    fireEvent.click(screen.getByTestId('preset-card-theater-classic'));

    await waitFor(() => expect(rejectingSelect).toHaveBeenCalled());
    // If the error had propagated, the test runner would flag an unhandled rejection.
    expect(errorSpy).toHaveBeenCalled();
    errorSpy.mockRestore();
  });
});

// ──────────────────────── S8.10: Mine tab ────────────────────────

const fakeTemplates: VenueLayoutDto[] = [
  {
    id: 'tmpl-1',
    name: 'My Theater Setup',
    eventId: null,
    layoutType: 'Theater',
    isTemplate: true,
    createdByUserId: 'user-1',
    totalCapacity: 200,
    createdAt: '2026-04-26T10:00:00Z',
    rowVersion: 1,
    canvas: { width: 1200, height: 800, scale: 1, backgroundColor: '#fff' },
    zones: [],
    tables: [],
    decorations: [],
  },
  {
    id: 'tmpl-2',
    name: 'My Banquet Setup',
    eventId: null,
    layoutType: 'Banquet',
    isTemplate: true,
    createdByUserId: 'user-1',
    totalCapacity: 80,
    createdAt: '2026-04-25T10:00:00Z',
    rowVersion: 1,
    canvas: { width: 1200, height: 800, scale: 1, backgroundColor: '#fff' },
    zones: [],
    tables: [],
    decorations: [],
  } as VenueLayoutDto,
] as VenueLayoutDto[];

describe('PresetLibraryModal — Mine tab (S8.10)', () => {
  it('renders Built-in tab active by default', () => {
    mountLoaded();
    expect(screen.getByTestId('preset-modal-tab-builtin')).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByTestId('preset-modal-tab-mine')).toHaveAttribute('aria-selected', 'false');
    expect(screen.getByTestId('preset-modal-tabpanel-builtin')).toBeInTheDocument();
    expect(screen.queryByTestId('preset-modal-tabpanel-mine')).toBeNull();
  });

  it('does not fetch user templates while the Built-in tab is active', () => {
    mountLoaded();
    // Mine query should be enabled=false while the user is on the built-in tab.
    expect(mockUseUserTemplates).toHaveBeenCalledWith({ enabled: false });
  });

  it('clicking the Mine tab switches active panel and triggers user templates fetch', () => {
    mountLoaded();
    fireEvent.click(screen.getByTestId('preset-modal-tab-mine'));
    expect(screen.getByTestId('preset-modal-tab-mine')).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByTestId('preset-modal-tabpanel-mine')).toBeInTheDocument();
    // Latest call to useUserTemplates should now have enabled: true.
    const lastCall = mockUseUserTemplates.mock.calls.at(-1)![0];
    expect(lastCall).toEqual({ enabled: true });
  });

  it('shows a loading state in the Mine tab while templates fetch', () => {
    mockUseUserTemplates.mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
      error: null,
      refetch: vi.fn(),
    });
    mountLoaded();
    fireEvent.click(screen.getByTestId('preset-modal-tab-mine'));
    expect(screen.getByTestId('mine-modal-loading')).toBeInTheDocument();
  });

  it('shows an error state with retry in the Mine tab', () => {
    const refetch = vi.fn();
    mockUseUserTemplates.mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: true,
      error: new Error('templates 500'),
      refetch,
    });
    mountLoaded();
    fireEvent.click(screen.getByTestId('preset-modal-tab-mine'));
    expect(screen.getByTestId('mine-modal-error')).toBeInTheDocument();
    expect(screen.getByText(/templates 500/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /try again/i }));
    expect(refetch).toHaveBeenCalledTimes(1);
  });

  it('shows a friendly empty-state when the user has no saved templates', () => {
    // Default mock already returns data=[] — just switch to the Mine tab.
    mountLoaded();
    fireEvent.click(screen.getByTestId('preset-modal-tab-mine'));
    expect(screen.getByTestId('mine-modal-empty')).toBeInTheDocument();
    expect(screen.getByText(/save as template/i)).toBeInTheDocument();
  });

  it('renders a card per saved template with name, layoutType, and capacity', () => {
    mockUseUserTemplates.mockReturnValue({
      data: fakeTemplates,
      isLoading: false,
      isError: false,
      error: null,
      refetch: vi.fn(),
    });
    mountLoaded();
    fireEvent.click(screen.getByTestId('preset-modal-tab-mine'));
    expect(screen.getByTestId('mine-card-tmpl-1')).toBeInTheDocument();
    expect(screen.getByTestId('mine-card-tmpl-2')).toBeInTheDocument();
    expect(screen.getByText('My Theater Setup')).toBeInTheDocument();
    expect(screen.getByText('My Banquet Setup')).toBeInTheDocument();
    expect(screen.getByText('200 seats')).toBeInTheDocument();
    expect(screen.getByText('80 seats')).toBeInTheDocument();
  });

  it('clicking a Mine card calls onSelectMine with the full template DTO', async () => {
    mockUseUserTemplates.mockReturnValue({
      data: fakeTemplates,
      isLoading: false,
      isError: false,
      error: null,
      refetch: vi.fn(),
    });
    const onSelectMine = vi.fn();
    mountLoaded({ onSelectMine });
    fireEvent.click(screen.getByTestId('preset-modal-tab-mine'));
    fireEvent.click(screen.getByTestId('mine-card-tmpl-1'));

    await waitFor(() => expect(onSelectMine).toHaveBeenCalledTimes(1));
    expect(onSelectMine).toHaveBeenCalledWith(fakeTemplates[0]);
  });

  it('shows spinner only on the selecting Mine card and disables the others', () => {
    mockUseUserTemplates.mockReturnValue({
      data: fakeTemplates,
      isLoading: false,
      isError: false,
      error: null,
      refetch: vi.fn(),
    });
    mountLoaded({
      onSelectMine: vi.fn(),
      isSelectingMine: true,
      selectingMineId: 'tmpl-1',
    });
    fireEvent.click(screen.getByTestId('preset-modal-tab-mine'));
    expect(screen.getByTestId('mine-card-spinner-tmpl-1')).toBeInTheDocument();
    expect(screen.queryByTestId('mine-card-spinner-tmpl-2')).toBeNull();
    expect(screen.getByTestId('mine-card-tmpl-2')).toBeDisabled();
    expect(screen.getByTestId('mine-card-tmpl-1')).not.toBeDisabled();
  });

  it('Mine card is a no-op when onSelectMine is not provided', async () => {
    mockUseUserTemplates.mockReturnValue({
      data: fakeTemplates,
      isLoading: false,
      isError: false,
      error: null,
      refetch: vi.fn(),
    });
    mountLoaded(); // no onSelectMine
    fireEvent.click(screen.getByTestId('preset-modal-tab-mine'));
    // Click should not throw.
    fireEvent.click(screen.getByTestId('mine-card-tmpl-1'));
    // Nothing else to assert — the test just verifies no exception.
  });
});
