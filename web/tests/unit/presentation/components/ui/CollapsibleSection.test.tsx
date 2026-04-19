import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { CollapsibleSection } from '@/presentation/components/ui/CollapsibleSection';

describe('CollapsibleSection', () => {
  it('renders title, description and children when defaultOpen=true', () => {
    render(
      <CollapsibleSection title="Signup Lists" description="Sign up for items" defaultOpen>
        <div>Inner content</div>
      </CollapsibleSection>
    );

    expect(screen.getByRole('heading', { name: 'Signup Lists' })).toBeInTheDocument();
    expect(screen.getByText('Sign up for items')).toBeInTheDocument();
    expect(screen.getByText('Inner content')).toBeInTheDocument();
  });

  it('shows the "Hide details" affordance label when expanded', () => {
    render(
      <CollapsibleSection title="Section" defaultOpen>
        <div>Body</div>
      </CollapsibleSection>
    );
    expect(screen.getByText('Hide details')).toBeInTheDocument();
    expect(screen.queryByText('Show details')).not.toBeInTheDocument();
  });

  it('shows the "Show details" affordance label when collapsed', () => {
    render(
      <CollapsibleSection title="Section" defaultOpen={false}>
        <div>Body</div>
      </CollapsibleSection>
    );
    expect(screen.getByText('Show details')).toBeInTheDocument();
    expect(screen.queryByText('Hide details')).not.toBeInTheDocument();
  });

  it('toggles aria-expanded and label when the header is clicked', () => {
    render(
      <CollapsibleSection title="Section" defaultOpen={false}>
        <div>Body</div>
      </CollapsibleSection>
    );

    const header = screen.getByRole('button', { expanded: false });
    expect(header).toHaveAttribute('aria-expanded', 'false');

    fireEvent.click(header);
    expect(header).toHaveAttribute('aria-expanded', 'true');
    expect(screen.getByText('Hide details')).toBeInTheDocument();

    fireEvent.click(header);
    expect(header).toHaveAttribute('aria-expanded', 'false');
    expect(screen.getByText('Show details')).toBeInTheDocument();
  });

  it('renders the summary preview only when collapsed', () => {
    render(
      <CollapsibleSection
        title="Signup Forms"
        defaultOpen={false}
        summary={<span>2 forms available</span>}
      >
        <div>Body</div>
      </CollapsibleSection>
    );

    // Collapsed: summary should show.
    expect(screen.getByText('2 forms available')).toBeInTheDocument();

    // Expand by clicking the header: summary should disappear.
    fireEvent.click(screen.getByRole('button'));
    expect(screen.queryByText('2 forms available')).not.toBeInTheDocument();
  });

  it('honors custom expand/collapse labels', () => {
    render(
      <CollapsibleSection
        title="Section"
        defaultOpen={false}
        expandLabel="Expand"
        collapseLabel="Collapse"
      >
        <div>Body</div>
      </CollapsibleSection>
    );
    expect(screen.getByText('Expand')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button'));
    expect(screen.getByText('Collapse')).toBeInTheDocument();
  });

  it('applies a custom borderColor when provided', () => {
    const { container } = render(
      <CollapsibleSection title="Section" borderColor="#FF7900">
        <div>Body</div>
      </CollapsibleSection>
    );
    const wrapper = container.firstChild as HTMLElement;
    expect(wrapper.style.borderColor).toBe('rgb(255, 121, 0)');
  });

  it('renders provided icon and badge', () => {
    render(
      <CollapsibleSection
        title="Section"
        icon={<span data-testid="icon">i</span>}
        badge={<span data-testid="badge">!</span>}
      >
        <div>Body</div>
      </CollapsibleSection>
    );
    expect(screen.getByTestId('icon')).toBeInTheDocument();
    expect(screen.getByTestId('badge')).toBeInTheDocument();
  });
});
