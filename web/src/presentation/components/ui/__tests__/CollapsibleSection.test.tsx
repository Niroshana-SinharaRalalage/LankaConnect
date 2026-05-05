import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { CollapsibleSection } from '../CollapsibleSection';

describe('CollapsibleSection', () => {
  describe('Rendering', () => {
    it('renders title and children', () => {
      render(
        <CollapsibleSection title="Test Section">
          <p data-testid="child">Hello</p>
        </CollapsibleSection>
      );
      expect(screen.getByText('Test Section')).toBeInTheDocument();
      expect(screen.getByTestId('child')).toBeInTheDocument();
    });

    it('renders description when provided', () => {
      render(
        <CollapsibleSection title="T" description="A description">
          <p>x</p>
        </CollapsibleSection>
      );
      expect(screen.getByText('A description')).toBeInTheDocument();
    });

    it('renders icon and badge', () => {
      render(
        <CollapsibleSection
          title="T"
          icon={<span data-testid="icon">i</span>}
          badge={<span data-testid="badge">b</span>}
        >
          <p>x</p>
        </CollapsibleSection>
      );
      expect(screen.getByTestId('icon')).toBeInTheDocument();
      expect(screen.getByTestId('badge')).toBeInTheDocument();
    });
  });

  describe('Uncontrolled mode', () => {
    it('defaults to open when defaultOpen is not specified', () => {
      render(
        <CollapsibleSection title="T">
          <p>x</p>
        </CollapsibleSection>
      );
      const button = screen.getByRole('button', { name: /T/i });
      expect(button).toHaveAttribute('aria-expanded', 'true');
    });

    it('respects defaultOpen=false', () => {
      render(
        <CollapsibleSection title="T" defaultOpen={false}>
          <p>x</p>
        </CollapsibleSection>
      );
      const button = screen.getByRole('button', { name: /T/i });
      expect(button).toHaveAttribute('aria-expanded', 'false');
    });

    it('toggles open/closed on header click', () => {
      render(
        <CollapsibleSection title="T" defaultOpen={false}>
          <p>x</p>
        </CollapsibleSection>
      );
      const button = screen.getByRole('button', { name: /T/i });
      expect(button).toHaveAttribute('aria-expanded', 'false');
      fireEvent.click(button);
      expect(button).toHaveAttribute('aria-expanded', 'true');
      fireEvent.click(button);
      expect(button).toHaveAttribute('aria-expanded', 'false');
    });

    it('keeps children mounted when collapsed (CSS-grid animation, not unmount)', () => {
      render(
        <CollapsibleSection title="T" defaultOpen={false}>
          <p data-testid="child">Hidden but mounted</p>
        </CollapsibleSection>
      );
      // Collapsed but child remains in the DOM
      expect(screen.getByTestId('child')).toBeInTheDocument();
    });
  });

  describe('Controlled mode', () => {
    it('uses external open prop when provided', () => {
      const { rerender } = render(
        <CollapsibleSection title="T" open={false} onOpenChange={() => {}}>
          <p>x</p>
        </CollapsibleSection>
      );
      const button = screen.getByRole('button', { name: /T/i });
      expect(button).toHaveAttribute('aria-expanded', 'false');

      rerender(
        <CollapsibleSection title="T" open={true} onOpenChange={() => {}}>
          <p>x</p>
        </CollapsibleSection>
      );
      expect(button).toHaveAttribute('aria-expanded', 'true');
    });

    it('calls onOpenChange when header is clicked, does not flip internal state', () => {
      const onOpenChange = vi.fn();
      render(
        <CollapsibleSection title="T" open={false} onOpenChange={onOpenChange}>
          <p>x</p>
        </CollapsibleSection>
      );
      const button = screen.getByRole('button', { name: /T/i });
      fireEvent.click(button);
      expect(onOpenChange).toHaveBeenCalledWith(true);
      // External open=false unchanged → still collapsed because state is controlled
      expect(button).toHaveAttribute('aria-expanded', 'false');
    });

    it('ignores defaultOpen when open is provided', () => {
      render(
        <CollapsibleSection
          title="T"
          defaultOpen={true}
          open={false}
          onOpenChange={() => {}}
        >
          <p>x</p>
        </CollapsibleSection>
      );
      const button = screen.getByRole('button', { name: /T/i });
      expect(button).toHaveAttribute('aria-expanded', 'false');
    });
  });

  describe('Summary preview', () => {
    it('shows summary when collapsed', () => {
      render(
        <CollapsibleSection
          title="T"
          defaultOpen={false}
          summary={<span data-testid="summary">3 items</span>}
        >
          <p>x</p>
        </CollapsibleSection>
      );
      expect(screen.getByTestId('summary')).toBeInTheDocument();
    });

    it('hides summary when expanded', () => {
      render(
        <CollapsibleSection
          title="T"
          defaultOpen={true}
          summary={<span data-testid="summary">3 items</span>}
        >
          <p>x</p>
        </CollapsibleSection>
      );
      expect(screen.queryByTestId('summary')).not.toBeInTheDocument();
    });
  });
});
