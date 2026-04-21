import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, within } from '@testing-library/react';
import {
  SignUpManagementSection,
  volunteerSectionLabels,
} from '@/presentation/components/features/events/SignUpManagementSection';
import {
  SignUpListDto,
  SignUpItemDto,
  SignUpCommitmentDto,
  SignUpItemCategory,
  SignUpType,
  SignUpItemType,
  SignUpKind,
} from '@/infrastructure/api/types/events.types';

/**
 * Phase 7D.1 Step G3: capture SignUpCommitmentModal props to verify the
 * section threads hideQuantitySelector correctly for volunteer lists without
 * opening the modal (which requires a click path through collapsed items).
 *
 * Single-responsibility spy — records last-received props for a targeted
 * assertion. Does not replace real modal behaviour; other tests exercise
 * the real modal with open=true.
 */
const modalPropsSpy: { last?: Record<string, unknown> } = {};
vi.mock('@/presentation/components/features/events/SignUpCommitmentModal', async () => {
  const actual = await vi.importActual<typeof import('@/presentation/components/features/events/SignUpCommitmentModal')>(
    '@/presentation/components/features/events/SignUpCommitmentModal'
  );
  return {
    ...actual,
    SignUpCommitmentModal: (props: Record<string, unknown>) => {
      modalPropsSpy.last = props;
      return null;
    },
  };
});

// Mock the hooks used by SignUpManagementSection
const mockUseEventSignUps = vi.fn();
const mockMutate = vi.fn();
const mockMutation = { mutate: mockMutate, mutateAsync: vi.fn(), isPending: false, isError: false, error: null };

vi.mock('@/presentation/hooks/useEventSignUps', () => ({
  useEventSignUps: (...args: unknown[]) => mockUseEventSignUps(...args),
  useCommitToSignUp: () => mockMutation,
  useCancelCommitment: () => mockMutation,
  useAddSignUpList: () => mockMutation,
  useRemoveSignUpList: () => mockMutation,
  useCommitToSignUpItem: () => mockMutation,
  useCommitToSignUpItemAnonymous: () => mockMutation,
  useAddOpenSignUpItem: () => mockMutation,
  useAddOpenSignUpItemAnonymous: () => mockMutation,
  useUpdateOpenSignUpItem: () => mockMutation,
  useCancelOpenSignUpItem: () => mockMutation,
  useReorderSignUpItems: () => mockMutation,
}));

vi.mock('@/presentation/store/useAuthStore', () => ({
  useAuthStore: () => ({ user: { userId: 'user-2' }, isAuthenticated: true }),
}));

// Phase 7D.1 Phase F added `useRouter()` to SignUpManagementSection for the
// data-driven Edit button. Tests that render the section must mock it.
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
}));

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
    itemType: SignUpItemType.Quantity,
    targetQuantity: 5,
    committedQuantity: 2,
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
    isFullyCommitted: false,
    isOpenItem: false
  };

  const mockSuggestedItem: SignUpItemDto = {
    id: 'suggested-1',
    itemDescription: 'Paper Plates',
    itemType: SignUpItemType.Quantity,
    targetQuantity: 10,
    committedQuantity: 0,
    remainingQuantity: 10,
    itemCategory: SignUpItemCategory.Suggested,
    notes: null,
    commitments: [],
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
    eventId: 'test-event-id',
    isOrganizer: false,
    userId: 'user-2'
  };

  beforeEach(() => {
    vi.clearAllMocks();
    // Default: return sign-up lists data from the hook
    mockUseEventSignUps.mockReturnValue({
      data: [mockSignUpList],
      isLoading: false,
      error: null,
    });
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

    mockUseEventSignUps.mockReturnValue({
      data: [emptyList],
      isLoading: false,
      error: null,
    });

    render(<SignUpManagementSection {...mockProps} />);

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

  /**
   * TEST 6: Open Items tab shows when hasOpenItems is enabled, even with 0 items
   * Critical test for Open Items feature - tabs must show even when no items exist yet
   * because users create their own items (not organizer-predefined)
   */
  it('should show Open Items tab when hasOpenItems is enabled, even with 0 items', () => {
    const emptyOpenList: SignUpListDto = {
      ...mockSignUpList,
      hasMandatoryItems: false,
      hasSuggestedItems: false,
      hasOpenItems: true,  // Category is enabled
      items: []  // No items yet - users will add their own
    };

    mockUseEventSignUps.mockReturnValue({
      data: [emptyOpenList],
      isLoading: false,
      error: null,
    });

    render(<SignUpManagementSection {...mockProps} />);

    // Should show Open Items (0) tab
    const openTab = screen.getByRole('tab', { name: /Open Items \(0\)/i });
    expect(openTab).toBeInTheDocument();

    // Click the tab to verify content
    fireEvent.click(openTab);

    // Should show "Sign Up" button in tab content
    expect(screen.getByRole('button', { name: /Sign Up/i })).toBeInTheDocument();

    // Should show empty state message
    expect(screen.getByText(/No one has signed up with their own item yet/i)).toBeInTheDocument();
  });

  /**
   * TEST 7: Open Items tab does NOT show when hasOpenItems is disabled
   * Verifies that the tab respects the category flag
   */
  it('should NOT show Open Items tab when hasOpenItems is false', () => {
    const noOpenList: SignUpListDto = {
      ...mockSignUpList,
      hasOpenItems: false  // Category disabled
    };

    mockUseEventSignUps.mockReturnValue({
      data: [noOpenList],
      isLoading: false,
      error: null,
    });

    render(<SignUpManagementSection {...mockProps} />);

    // Should NOT have Open Items tab
    expect(screen.queryByRole('tab', { name: /Open Items/i })).not.toBeInTheDocument();
  });

  /**
   * TEST 8: Open Items tab count updates when items are added
   * Verifies that the tab count is dynamic
   */
  it('should update Open Items tab count when items exist', () => {
    const mockOpenItem: SignUpItemDto = {
      id: 'open-1',
      itemDescription: 'Homemade Cookies',
      itemType: SignUpItemType.Quantity,
      targetQuantity: 24,
      committedQuantity: 24,
      remainingQuantity: 0,
      itemCategory: SignUpItemCategory.Open,
      notes: '2 dozen chocolate chip',
      commitments: [
        {
          id: 'commit-open-1',
          userId: 'user-2',
          contactName: 'Jane Smith',
          quantity: 24,
          itemDescription: 'Homemade Cookies',
          committedAt: '2026-01-16T14:00:00Z',
          signUpItemId: 'open-1'
        }
      ],
      isFullyCommitted: true,
      isOpenItem: true,
      createdByUserId: 'user-2'
    };

    const openListWithItems: SignUpListDto = {
      ...mockSignUpList,
      hasMandatoryItems: false,
      hasSuggestedItems: false,
      hasOpenItems: true,
      items: [mockOpenItem]
    };

    mockUseEventSignUps.mockReturnValue({
      data: [openListWithItems],
      isLoading: false,
      error: null,
    });

    render(<SignUpManagementSection {...mockProps} />);

    // Should show Open Items (1) tab
    expect(screen.getByRole('tab', { name: /Open Items \(1\)/i })).toBeInTheDocument();

    // Click tab to verify content shows
    const openTab = screen.getByRole('tab', { name: /Open Items \(1\)/i });
    fireEvent.click(openTab);

    // Should show the item
    expect(screen.getByText('Homemade Cookies')).toBeInTheDocument();
  });
});

/**
 * Phase 7D.1 Phase G — kind prop + hideQuantitySelector threading.
 *
 * These tests verify that when the section is mounted in Volunteers mode
 * (kind + volunteer labels), the underlying SignUpCommitmentModal is asked
 * to hide its quantity selector. The actual rendering behaviour of the
 * modal with the prop is covered by SignUpCommitmentModal.labels.test.tsx.
 */
describe('SignUpManagementSection - Phase 7D.1 Phase G kind threading', () => {
  const mockVolunteerRole: SignUpItemDto = {
    id: 'role-1',
    itemDescription: 'Food Committee',
    itemType: SignUpItemType.Slot,
    itemCategory: SignUpItemCategory.Mandatory,
    notes: null,
    commitments: [],
    isFullyCommitted: false,
    isOpenItem: false,
    totalSlots: 5,
    filledSlots: 0,
    remainingSlots: 5,
  } as SignUpItemDto;

  const mockVolunteerList: SignUpListDto = {
    id: 'vol-list-1',
    category: 'Event Day Volunteers',
    description: 'Sign up to help',
    signUpType: SignUpType.Predefined,
    hasMandatoryItems: true,
    hasPreferredItems: false,
    hasSuggestedItems: false,
    hasOpenItems: false,
    items: [mockVolunteerRole],
    commitments: [],
    predefinedItems: [],
    commitmentCount: 0,
    kind: SignUpKind.Volunteers,
  } as SignUpListDto;

  const mockItemsList: SignUpListDto = {
    id: 'items-list-1',
    category: 'Food & Supplies',
    description: 'Bring items',
    signUpType: SignUpType.Predefined,
    hasMandatoryItems: true,
    hasPreferredItems: false,
    hasSuggestedItems: false,
    hasOpenItems: false,
    items: [mockVolunteerRole],
    commitments: [],
    predefinedItems: [],
    commitmentCount: 0,
    kind: SignUpKind.Items,
  } as SignUpListDto;

  beforeEach(() => {
    vi.clearAllMocks();
    modalPropsSpy.last = undefined;
  });

  it('passes hideQuantitySelector=true to the modal when kind=Volunteers', () => {
    mockUseEventSignUps.mockReturnValue({
      data: [mockVolunteerList],
      isLoading: false,
      error: null,
    });

    render(
      <SignUpManagementSection
        eventId="evt-1"
        isOrganizer={false}
        userId="user-2"
        kind={SignUpKind.Volunteers}
        labels={volunteerSectionLabels}
      />
    );

    expect(modalPropsSpy.last).toBeDefined();
    expect(modalPropsSpy.last?.hideQuantitySelector).toBe(true);
  });

  it('(regression guard) passes hideQuantitySelector=false/undefined to the modal when kind=Items', () => {
    mockUseEventSignUps.mockReturnValue({
      data: [mockItemsList],
      isLoading: false,
      error: null,
    });

    render(
      <SignUpManagementSection
        eventId="evt-1"
        isOrganizer={false}
        userId="user-2"
        kind={SignUpKind.Items}
      />
    );

    expect(modalPropsSpy.last).toBeDefined();
    // Must not be strictly true — undefined or false are both acceptable for Items mode.
    expect(modalPropsSpy.last?.hideQuantitySelector).not.toBe(true);
  });

  it('(regression guard) passes hideQuantitySelector=false/undefined to the modal when kind is omitted (legacy callers)', () => {
    mockUseEventSignUps.mockReturnValue({
      data: [mockItemsList],
      isLoading: false,
      error: null,
    });

    render(<SignUpManagementSection eventId="evt-1" isOrganizer={false} userId="user-2" />);

    expect(modalPropsSpy.last).toBeDefined();
    expect(modalPropsSpy.last?.hideQuantitySelector).not.toBe(true);
  });
});
