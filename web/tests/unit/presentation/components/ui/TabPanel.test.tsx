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
});
