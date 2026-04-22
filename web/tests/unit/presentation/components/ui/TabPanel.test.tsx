import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { TabPanel } from '@/presentation/components/ui/TabPanel';
import { Calendar, User, Settings } from 'lucide-react';

describe('TabPanel', () => {
  const mockTabs = [
    {
      id: 'tab1',
      label: 'Tab 1',
      icon: Calendar,
      content: <div>Tab 1 Content</div>,
    },
    {
      id: 'tab2',
      label: 'Tab 2',
      icon: User,
      content: <div>Tab 2 Content</div>,
    },
    {
      id: 'tab3',
      label: 'Tab 3',
      icon: Settings,
      content: <div>Tab 3 Content</div>,
    },
  ];

  it('should render all tab labels', () => {
    render(<TabPanel tabs={mockTabs} />);

    expect(screen.getByText('Tab 1')).toBeInTheDocument();
    expect(screen.getByText('Tab 2')).toBeInTheDocument();
    expect(screen.getByText('Tab 3')).toBeInTheDocument();
  });

  it('should render the first tab content by default', () => {
    render(<TabPanel tabs={mockTabs} />);

    expect(screen.getByText('Tab 1 Content')).toBeInTheDocument();
    expect(screen.queryByText('Tab 2 Content')).not.toBeInTheDocument();
    expect(screen.queryByText('Tab 3 Content')).not.toBeInTheDocument();
  });

  it('should render default tab content when specified', () => {
    render(<TabPanel tabs={mockTabs} defaultTab="tab2" />);

    expect(screen.queryByText('Tab 1 Content')).not.toBeInTheDocument();
    expect(screen.getByText('Tab 2 Content')).toBeInTheDocument();
    expect(screen.queryByText('Tab 3 Content')).not.toBeInTheDocument();
  });

  it('should switch tabs when clicked', () => {
    render(<TabPanel tabs={mockTabs} />);

    // Initially showing tab 1
    expect(screen.getByText('Tab 1 Content')).toBeInTheDocument();

    // Click tab 2
    fireEvent.click(screen.getByText('Tab 2'));
    expect(screen.queryByText('Tab 1 Content')).not.toBeInTheDocument();
    expect(screen.getByText('Tab 2 Content')).toBeInTheDocument();

    // Click tab 3
    fireEvent.click(screen.getByText('Tab 3'));
    expect(screen.queryByText('Tab 2 Content')).not.toBeInTheDocument();
    expect(screen.getByText('Tab 3 Content')).toBeInTheDocument();
  });

  it('should call onChange callback when tab is switched', () => {
    const onChange = vi.fn();
    render(<TabPanel tabs={mockTabs} onChange={onChange} />);

    fireEvent.click(screen.getByText('Tab 2'));
    expect(onChange).toHaveBeenCalledWith('tab2');

    fireEvent.click(screen.getByText('Tab 3'));
    expect(onChange).toHaveBeenCalledWith('tab3');
  });

  it('should apply active styles to the current tab', () => {
    render(<TabPanel tabs={mockTabs} />);

    const tab1Button = screen.getByText('Tab 1').closest('button');
    const tab2Button = screen.getByText('Tab 2').closest('button');

    expect(tab1Button).toHaveClass('border-b-2');
    expect(tab2Button).not.toHaveClass('border-b-2');

    fireEvent.click(screen.getByText('Tab 2'));

    expect(tab1Button).not.toHaveClass('border-b-2');
    expect(tab2Button).toHaveClass('border-b-2');
  });

  it('should support keyboard navigation with arrow keys', () => {
    render(<TabPanel tabs={mockTabs} />);

    const tabList = screen.getByRole('tablist');

    // Press ArrowRight to move to next tab
    fireEvent.keyDown(tabList, { key: 'ArrowRight' });
    expect(screen.getByText('Tab 2 Content')).toBeInTheDocument();

    // Press ArrowRight again
    fireEvent.keyDown(tabList, { key: 'ArrowRight' });
    expect(screen.getByText('Tab 3 Content')).toBeInTheDocument();

    // Press ArrowLeft to move back
    fireEvent.keyDown(tabList, { key: 'ArrowLeft' });
    expect(screen.getByText('Tab 2 Content')).toBeInTheDocument();
  });

  it('should wrap around when navigating past the last tab', () => {
    render(<TabPanel tabs={mockTabs} />);

    const tabList = screen.getByRole('tablist');

    // Navigate to last tab
    fireEvent.keyDown(tabList, { key: 'ArrowRight' });
    fireEvent.keyDown(tabList, { key: 'ArrowRight' });
    expect(screen.getByText('Tab 3 Content')).toBeInTheDocument();

    // Press ArrowRight - should wrap to first tab
    fireEvent.keyDown(tabList, { key: 'ArrowRight' });
    expect(screen.getByText('Tab 1 Content')).toBeInTheDocument();
  });

  it('should render tabs without icons if not provided', () => {
    const tabsWithoutIcons = [
      {
        id: 'tab1',
        label: 'Simple Tab 1',
        content: <div>Content 1</div>,
      },
      {
        id: 'tab2',
        label: 'Simple Tab 2',
        content: <div>Content 2</div>,
      },
    ];

    render(<TabPanel tabs={tabsWithoutIcons} />);

    expect(screen.getByText('Simple Tab 1')).toBeInTheDocument();
    expect(screen.getByText('Simple Tab 2')).toBeInTheDocument();
  });

  it('should have proper ARIA attributes', () => {
    render(<TabPanel tabs={mockTabs} />);

    const tabList = screen.getByRole('tablist');
    expect(tabList).toBeInTheDocument();

    const tabs = screen.getAllByRole('tab');
    expect(tabs).toHaveLength(3);

    const selectedTab = tabs[0];
    expect(selectedTab).toHaveAttribute('aria-selected', 'true');
    expect(selectedTab).toHaveAttribute('aria-controls');

    const unselectedTab = tabs[1];
    expect(unselectedTab).toHaveAttribute('aria-selected', 'false');
  });

  // Phase 6A.132 UX follow-up 3: user-selected tab must survive parent re-renders that
  // only change the `tabs` array reference (new array, same IDs + same defaultTab).
  // Before this fix, the sync effect depended on both [defaultTab, tabs] — because
  // parents typically build `tabs` inline, every re-render produced a new array
  // reference and the effect would re-run `setActiveTab(defaultTab)`, snapping the
  // user back to the default. Manifested on the Event Management page: clicking an
  // up/down arrow in a sign-up list triggered a signup-list refetch → parent
  // re-render → `tabs` array new reference → TabPanel reset to "Event Details".
  it('should preserve user-clicked tab when parent re-renders with a new tabs array reference', () => {
    const buildTabs = () => [
      { id: 'tab1', label: 'Tab 1', icon: Calendar, content: <div>Tab 1 Content</div> },
      { id: 'tab2', label: 'Tab 2', icon: User, content: <div>Tab 2 Content</div> },
      { id: 'tab3', label: 'Tab 3', icon: Settings, content: <div>Tab 3 Content</div> },
    ];

    const { rerender } = render(<TabPanel tabs={buildTabs()} defaultTab="tab1" />);
    expect(screen.getByText('Tab 1 Content')).toBeInTheDocument();

    // User clicks Tab 2 → active tab becomes tab2
    fireEvent.click(screen.getByText('Tab 2'));
    expect(screen.getByText('Tab 2 Content')).toBeInTheDocument();
    expect(screen.queryByText('Tab 1 Content')).not.toBeInTheDocument();

    // Parent re-renders: fresh `tabs` array reference, same defaultTab value.
    // This simulates the React Query refetch → parent re-render scenario.
    rerender(<TabPanel tabs={buildTabs()} defaultTab="tab1" />);

    // Active tab must stay on user's choice, NOT snap back to defaultTab.
    expect(screen.getByText('Tab 2 Content')).toBeInTheDocument();
    expect(screen.queryByText('Tab 1 Content')).not.toBeInTheDocument();

    // And a second re-render (e.g. another refetch) must still not reset.
    rerender(<TabPanel tabs={buildTabs()} defaultTab="tab1" />);
    expect(screen.getByText('Tab 2 Content')).toBeInTheDocument();
  });

  // Regression guard: when defaultTab genuinely changes (e.g. URL param change
  // via browser back/forward), TabPanel must still sync to the new value.
  // This is the originally-intended behaviour of the sync effect (Phase 6A.74
  // Part 14 Fix #3) and must be preserved.
  it('should sync active tab when defaultTab prop actually changes value', () => {
    const { rerender } = render(<TabPanel tabs={mockTabs} defaultTab="tab1" />);
    expect(screen.getByText('Tab 1 Content')).toBeInTheDocument();

    // Simulate URL change: ?tab=tab3
    rerender(<TabPanel tabs={mockTabs} defaultTab="tab3" />);
    expect(screen.getByText('Tab 3 Content')).toBeInTheDocument();
    expect(screen.queryByText('Tab 1 Content')).not.toBeInTheDocument();

    // Simulate another URL change: ?tab=tab2
    rerender(<TabPanel tabs={mockTabs} defaultTab="tab2" />);
    expect(screen.getByText('Tab 2 Content')).toBeInTheDocument();
    expect(screen.queryByText('Tab 3 Content')).not.toBeInTheDocument();
  });

  // Regression guard: a defaultTab that doesn't match any tab id must not
  // change the active tab (mirrors the `tabs.some(...)` guard in the effect).
  it('should ignore defaultTab changes that do not match any tab id', () => {
    const { rerender } = render(<TabPanel tabs={mockTabs} defaultTab="tab2" />);
    expect(screen.getByText('Tab 2 Content')).toBeInTheDocument();

    rerender(<TabPanel tabs={mockTabs} defaultTab="non-existent-tab" />);

    // Active tab should still be tab2
    expect(screen.getByText('Tab 2 Content')).toBeInTheDocument();
  });
});
