import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, within } from '@testing-library/react';
import { SignUpManagementSection } from '@/presentation/components/features/events/SignUpManagementSection';
import { SignUpListDto, SignUpItemDto, SignUpCommitmentDto, SignUpItemCategory, SignUpType } from '@/infrastructure/api/types/events.types';

/**
 * Test Suite for SignUpManagementSection Component
 * Phase 6A.118: Signup Lists UI/UX Enhancements
 *
 * Tests the following enhancements:
 * 1. Badge displays "Suggested Quantities" instead of "Required"
 * 2. Items are collapsed by default
 * 3. Expand/collapse functionality works
 * 4. Multiple items maintain independent state
 * 5. Tab-based category navigation
 * 6. Empty categories don't show tabs
 * 7. State persists when switching tabs
 */
describe('SignUpManagementSection - Phase 6A.118 Enhancements', () => {
  // Mock data setup
  const mockMandatoryItem: SignUpItemDto = {
    id: 'mandatory-1',
    itemDescription: 'Chicken Curry',
    quantity: 5,
    remainingQuantity: 3,
    itemCategory: SignUpItemCategory.Mandatory,
    notes: 'Non-spicy preferred',
    commitments: [
      {
        id: 'commit-1',
        userId: 'user-1',
        contactName: 'John Doe',
        quantity: 2,
        itemDescription: 'Chicken Curry',
        committedAt: '2026-01-15T10:00:00Z',
        signUpItemId: 'mandatory-1'
      }
    ],
    committedQuantity: 2,
    isFullyCommitted: false,
    isOpenItem: false
  };

  const mockSuggestedItem: SignUpItemDto = {
    id: 'suggested-1',
    itemDescription: 'Paper Plates',
    quantity: 10,
    remainingQuantity: 10,
    itemCategory: SignUpItemCategory.Suggested,
    notes: null,
    commitments: [],
    committedQuantity: 0,
    isFullyCommitted: false,
    isOpenItem: false
  };

  const mockSignUpList: SignUpListDto = {
    id: 'list-1',
    category: 'Food & Supplies',
    description: 'Please bring items to share',
    signUpType: SignUpType.Predefined,
    hasMandatoryItems: true,
    hasPreferredItems: false,
    hasSuggestedItems: true,
    hasOpenItems: false,
    items: [mockMandatoryItem, mockSuggestedItem],
    commitments: [],
    predefinedItems: [],
    commitmentCount: 1
  };

  const mockProps = {
    signUpLists: [mockSignUpList],
    isLoading: false,
    isOrganizer: false,
    userId: 'user-2'
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  /**
   * TEST 1: Badge displays "Suggested Quantities" instead of "Required"
   * Verifies that the terminology has been updated for clarity
   */
  it('should display "Suggested Quantities" instead of "Required"', () => {
    render(<SignUpManagementSection {...mockProps} />);

    // Should find "Suggested Quantities" text
    expect(screen.getByText(/Suggested Quantities:/i)).toBeInTheDocument();

    // Should NOT find old "Required" text (excluding aria labels)
    const requiredElements = screen.queryAllByText(/^Required:/i);
    expect(requiredElements.length).toBe(0);
  });

  /**
   * TEST 2: Items are collapsed by default
   * Verifies that progress bars and commitment tables are hidden initially
   */
  it('should render items collapsed by default', () => {
    render(<SignUpManagementSection {...mockProps} />);

    // Item descriptions should be visible (header)
    expect(screen.getByText('Chicken Curry')).toBeInTheDocument();
    expect(screen.getByText('Paper Plates')).toBeInTheDocument();

    // Progress bar text should NOT be visible (collapsed)
    expect(screen.queryByText(/of.*filled/i)).not.toBeInTheDocument();

    // Commitments table should NOT be visible (collapsed)
    expect(screen.queryByText('John Doe')).not.toBeInTheDocument();

    // Sign Up buttons should NOT be visible (collapsed)
    expect(screen.queryByRole('button', { name: /Sign Up/i })).not.toBeInTheDocument();
  });

  /**
   * TEST 3: Expand item when chevron is clicked
   * Verifies that clicking the expand icon reveals item details
   */
  it('should expand item when chevron icon is clicked', () => {
    render(<SignUpManagementSection {...mockProps} />);

    // Find and click the expand button (should have aria-label)
    const expandButtons = screen.getAllByLabelText(/Expand item details/i);
    expect(expandButtons.length).toBeGreaterThan(0);

    fireEvent.click(expandButtons[0]);

    // After expansion, progress bar should be visible
    expect(screen.getByText(/of.*filled/i)).toBeInTheDocument();

    // Commitment details should be visible
    expect(screen.getByText('John Doe')).toBeInTheDocument();

    // Button text should change to "Collapse"
    const collapseButton = screen.getByLabelText(/Collapse item details/i);
    expect(collapseButton).toBeInTheDocument();
  });

  /**
   * TEST 4: Multiple items toggle independently
   * Verifies that each item maintains its own expand/collapse state
   */
  it('should toggle multiple items independently', () => {
    render(<SignUpManagementSection {...mockProps} />);

    const expandButtons = screen.getAllByLabelText(/Expand item details/i);
    expect(expandButtons.length).toBe(2); // 2 items

    // Expand first item
    fireEvent.click(expandButtons[0]);
    expect(screen.getByText('John Doe')).toBeInTheDocument(); // Commitment visible

    // Expand second item
    fireEvent.click(expandButtons[1]);
    expect(screen.getAllByText(/of.*filled/i).length).toBe(2); // Both progress bars visible

    // Collapse first item
    const collapseButtons = screen.getAllByLabelText(/Collapse item details/i);
    fireEvent.click(collapseButtons[0]);

    // First item collapsed (commitment hidden)
    expect(screen.queryByText('John Doe')).not.toBeInTheDocument();

    // Second item still expanded (progress bar visible)
    expect(screen.getByText(/of.*filled/i)).toBeInTheDocument();
  });

  /**
   * TEST 5: Category tabs render correctly
   * Verifies that TabPanel is used with Mandatory, Suggested, Open tabs
   */
  it('should render category tabs (Mandatory, Suggested, Open)', () => {
    render(<SignUpManagementSection {...mockProps} />);

    // Should have tabs for Mandatory and Suggested (Open not enabled)
    expect(screen.getByRole('tab', { name: /Mandatory Items/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /Suggested Items/i })).toBeInTheDocument();

    // Open Items tab should NOT appear (hasOpenItems = false)
    expect(screen.queryByRole('tab', { name: /Open Items/i })).not.toBeInTheDocument();
  });

  /**
   * TEST 6: Empty categories don't show tabs
   * Verifies that tabs only appear for categories with items
   */
  it('should only show tabs for non-empty categories', () => {
    const emptyList: SignUpListDto = {
      ...mockSignUpList,
      hasMandatoryItems: true,
      hasSuggestedItems: false,
      items: [mockMandatoryItem] // Only mandatory items
    };

    render(<SignUpManagementSection {...mockProps} signUpLists={[emptyList]} />);

    // Should have Mandatory tab
    expect(screen.getByRole('tab', { name: /Mandatory Items/i })).toBeInTheDocument();

    // Should NOT have Suggested tab (no items)
    expect(screen.queryByRole('tab', { name: /Suggested Items/i })).not.toBeInTheDocument();
  });

  /**
   * TEST 7: State persists when switching tabs
   * Verifies that expanded/collapsed state is maintained across tab switches
   */
  it('should maintain expand/collapse state when switching tabs', () => {
    render(<SignUpManagementSection {...mockProps} />);

    // Start on Mandatory tab, expand the item
    const mandatoryTab = screen.getByRole('tab', { name: /Mandatory Items/i });
    fireEvent.click(mandatoryTab);

    const expandButtons = screen.getAllByLabelText(/Expand item details/i);
    fireEvent.click(expandButtons[0]);

    // Verify expanded
    expect(screen.getByText('John Doe')).toBeInTheDocument();

    // Switch to Suggested tab
    const suggestedTab = screen.getByRole('tab', { name: /Suggested Items/i });
    fireEvent.click(suggestedTab);

    // Switch back to Mandatory tab
    fireEvent.click(mandatoryTab);

    // Item should still be expanded
    expect(screen.getByText('John Doe')).toBeInTheDocument();
    expect(screen.getByLabelText(/Collapse item details/i)).toBeInTheDocument();
  });
});
