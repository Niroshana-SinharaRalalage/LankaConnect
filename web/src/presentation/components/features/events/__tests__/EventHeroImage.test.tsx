import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { EventHeroImage } from '../EventHeroImage';
import type { EventImageDto } from '@/infrastructure/api/types/events.types';

const buildImage = (overrides: Partial<EventImageDto> = {}): EventImageDto => ({
  id: 'img-1',
  imageUrl: 'https://example.com/flyer.jpg',
  displayOrder: 1,
  isPrimary: true,
  uploadedAt: '2026-05-08T00:00:00Z',
  ...overrides,
});

describe('EventHeroImage', () => {
  describe('Empty / no-image states', () => {
    it('renders nothing when images array is empty', () => {
      const { container } = render(
        <EventHeroImage
          images={[]}
          title="Vesak"
          categoryLabel="Community"
          variant="contained"
        />,
      );
      expect(container.firstChild).toBeNull();
    });

    it('renders nothing when images is undefined', () => {
      const { container } = render(
        <EventHeroImage
          images={undefined as unknown as readonly EventImageDto[]}
          title="Vesak"
          categoryLabel="Community"
          variant="contained"
        />,
      );
      expect(container.firstChild).toBeNull();
    });
  });

  describe('Primary-image selection', () => {
    it('uses the image flagged isPrimary=true when present', () => {
      const images = [
        buildImage({ id: 'a', imageUrl: 'https://cdn/test/a.jpg', isPrimary: false }),
        buildImage({ id: 'b', imageUrl: 'https://cdn/test/b.jpg', isPrimary: true }),
        buildImage({ id: 'c', imageUrl: 'https://cdn/test/c.jpg', isPrimary: false }),
      ];
      render(
        <EventHeroImage
          images={images}
          title="Vesak"
          categoryLabel="Community"
          variant="contained"
        />,
      );
      const img = screen.getByRole('img');
      expect(img).toHaveAttribute('src', 'https://cdn/test/b.jpg');
    });

    it('falls back to the first image when no isPrimary is set', () => {
      const images = [
        buildImage({ id: 'a', imageUrl: 'https://cdn/test/a.jpg', isPrimary: false }),
        buildImage({ id: 'b', imageUrl: 'https://cdn/test/b.jpg', isPrimary: false }),
      ];
      render(
        <EventHeroImage
          images={images}
          title="Vesak"
          categoryLabel="Community"
          variant="contained"
        />,
      );
      const img = screen.getByRole('img');
      expect(img).toHaveAttribute('src', 'https://cdn/test/a.jpg');
    });

    it('uses the title as alt text', () => {
      render(
        <EventHeroImage
          images={[buildImage()]}
          title="වෙසක් සීල සමාදානය"
          categoryLabel="Community"
          variant="contained"
        />,
      );
      expect(screen.getByAltText('වෙසක් සීල සමාදානය')).toBeInTheDocument();
    });
  });

  describe('Letterbox / object-fit (Option C + E)', () => {
    it('uses object-contain so the full image is visible without cropping', () => {
      render(
        <EventHeroImage
          images={[buildImage()]}
          title="Vesak"
          categoryLabel="Community"
          variant="contained"
        />,
      );
      const img = screen.getByRole('img');
      expect(img.className).toContain('object-contain');
      expect(img.className).not.toContain('object-cover');
    });

    it('applies a responsive aspect ratio (mobile 16:9, desktop 3:1) on contained variant', () => {
      const { container } = render(
        <EventHeroImage
          images={[buildImage()]}
          title="Vesak"
          categoryLabel="Community"
          variant="contained"
        />,
      );
      const heroDiv = container.querySelector('div')!;
      expect(heroDiv.className).toContain('aspect-[16/9]');
      expect(heroDiv.className).toContain('md:aspect-[3/1]');
      // No fixed height — replaced by responsive aspect ratio
      expect(heroDiv.className).not.toContain('h-96');
    });

    it('renders the gradient background as a letterbox fallback', () => {
      const { container } = render(
        <EventHeroImage
          images={[buildImage()]}
          title="Vesak"
          categoryLabel="Community"
          variant="contained"
        />,
      );
      const heroDiv = container.querySelector('div')!;
      expect(heroDiv.className).toContain('bg-gradient-to-br');
      expect(heroDiv.className).toContain('from-orange-500');
      expect(heroDiv.className).toContain('to-rose-500');
    });
  });

  describe('Variant: contained (Option C)', () => {
    it('does NOT apply w-screen / full-bleed classes', () => {
      const { container } = render(
        <EventHeroImage
          images={[buildImage()]}
          title="Vesak"
          categoryLabel="Community"
          variant="contained"
        />,
      );
      const heroDiv = container.querySelector('div')!;
      expect(heroDiv.className).not.toContain('w-screen');
    });
  });

  describe('Variant: fullWidth (Option E)', () => {
    it('applies w-full so the hero spans the parent container', () => {
      const { container } = render(
        <EventHeroImage
          images={[buildImage()]}
          title="Vesak"
          categoryLabel="Community"
          variant="fullWidth"
        />,
      );
      const heroDiv = container.querySelector('div')!;
      expect(heroDiv.className).toContain('w-full');
    });

    it('still uses object-contain to preserve the full image', () => {
      render(
        <EventHeroImage
          images={[buildImage()]}
          title="Vesak"
          categoryLabel="Community"
          variant="fullWidth"
        />,
      );
      const img = screen.getByRole('img');
      expect(img.className).toContain('object-contain');
    });

    it('applies the same responsive aspect ratio as contained', () => {
      const { container } = render(
        <EventHeroImage
          images={[buildImage()]}
          title="Vesak"
          categoryLabel="Community"
          variant="fullWidth"
        />,
      );
      const heroDiv = container.querySelector('div')!;
      expect(heroDiv.className).toContain('aspect-[16/9]');
      expect(heroDiv.className).toContain('md:aspect-[3/1]');
    });
  });

  describe('Category badge', () => {
    it('renders the category badge with the provided label', () => {
      render(
        <EventHeroImage
          images={[buildImage()]}
          title="Vesak"
          categoryLabel="Community"
          variant="contained"
        />,
      );
      expect(screen.getByText('Community')).toBeInTheDocument();
    });

    it('does not render a badge when categoryLabel is empty', () => {
      render(
        <EventHeroImage
          images={[buildImage()]}
          title="Vesak"
          categoryLabel=""
          variant="contained"
        />,
      );
      // Badge is anchored top-right inside hero; no text means no badge
      expect(screen.queryByText('Community')).not.toBeInTheDocument();
    });
  });
});
