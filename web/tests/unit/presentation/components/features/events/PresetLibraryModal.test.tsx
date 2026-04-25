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
}));

// next/image expects Next's runtime; shim it to a plain <img>.
vi.mock('next/image', () => ({
  __esModule: true,
  default: (props: React.ImgHTMLAttributes<HTMLImageElement>) =>
    React.createElement('img', props),
}));

import { useLayoutPresets } from '@/presentation/hooks/useVenueLayouts';
import { PresetLibraryModal } from '@/presentation/components/features/events/PresetLibraryModal';
import type { LayoutPresetDto } from '@/infrastructure/api/types/events.types';

const mockUseLayoutPresets = useLayoutPresets as unknown as ReturnType<typeof vi.fn>;

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
