/**
 * SignUpManagementSection Component
 *
 * Complete UI for Event Sign-Up Management feature
 * Displays sign-up lists with commitments and allows users to commit or cancel
 *
 * Features:
 * - View all sign-up lists for an event
 * - See existing commitments from other users
 * - Commit to bringing items (authenticated users)
 * - Cancel commitments (own commitments only)
 * - Organizer can add/remove sign-up lists
 * - NEW: Category-based sign-ups (Mandatory, Preferred, Suggested)
 * - Backward compatible with legacy Open/Predefined sign-ups
 *
 * @requires useEventSignUps hook
 * @requires useCommitToSignUp, useCancelCommitment mutations
 * @requires useCommitToSignUpItem mutation (category-based)
 */

'use client';

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  useEventSignUps,
  useCommitToSignUp,
  useCancelCommitment,
  useAddSignUpList,
  useRemoveSignUpList,
  useCommitToSignUpItem,
  useCommitToSignUpItemAnonymous, // GitHub Issue #41
  // Phase 6A.27: Open Sign-Up Items
  useAddOpenSignUpItem,
  useAddOpenSignUpItemAnonymous,
  useUpdateOpenSignUpItem,
  useCancelOpenSignUpItem,
  // Phase 6A.132: Drag-drop reorder
  useReorderSignUpItems,
} from '@/presentation/hooks/useEventSignUps';
import { SignUpType, SignUpItemCategory, SignUpItemDto, SignUpCommitmentDto, SignUpKind, isQuantityBased } from '@/infrastructure/api/types/events.types';
import {
  DndContext,
  closestCenter,
  PointerSensor,
  KeyboardSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core';
import {
  SortableContext,
  verticalListSortingStrategy,
  sortableKeyboardCoordinates,
  arrayMove,
  useSortable,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { GripVertical } from 'lucide-react';
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import {
  SignUpCommitmentModal,
  CommitmentFormData,
  AnonymousCommitmentFormData,
  SignUpCommitmentLabels,
  volunteerCommitmentLabels,
} from './SignUpCommitmentModal';
import { OpenItemSignUpModal, OpenItemFormData } from './OpenItemSignUpModal';
import { Plus, Edit, Trash2, ChevronDown, AlertCircle, Lightbulb } from 'lucide-react';
import { TabPanel, Tab } from '@/presentation/components/ui/TabPanel';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { ConfirmDialog } from '@/presentation/components/ui/ConfirmDialog';
import toast from 'react-hot-toast';

/**
 * Phase 7D.1 Step 21: section-level copy overrides let the volunteer UI (Phase F/G)
 * inject volunteer-specific labels without forking the component. Omit to keep
 * existing sign-up UX 100% identical.
 */
export interface SignUpListsSectionLabels {
  sectionHeading: (count: number) => string;
  emptyStateOrganizer: { title: string; helperText: string };
  emptyStateAttendeeTitle: string;
  emptyStateAttendeeDescription: string;
  signUpButton: string;
  updateSignUpButton: string;
  cancelSignUpButton: string;
  cancelCommitmentDialog: { title: string; description: string };
  cancelSignUpDialog: { title: string; description: string };
  cancelOpenItemDialog: { title: string; description: string };
  commitmentModal?: SignUpCommitmentLabels;
}

export const defaultSignUpListsSectionLabels: SignUpListsSectionLabels = {
  sectionHeading: (count) =>
    `Sign-Up Lists (This event has ${count} sign-up ${count === 1 ? 'list' : 'lists'})`,
  emptyStateOrganizer: {
    title: 'No sign-up lists for this event yet.',
    helperText: 'Create one to let attendees volunteer to bring items!',
  },
  emptyStateAttendeeTitle: 'Sign-Up Lists',
  emptyStateAttendeeDescription: 'No sign-up lists for this event yet.',
  signUpButton: 'Sign Up',
  updateSignUpButton: 'Update Sign Up',
  cancelSignUpButton: 'Cancel Sign Up',
  cancelCommitmentDialog: {
    title: 'Cancel Commitment',
    description:
      'Are you sure you want to cancel your commitment to this sign-up list? This action cannot be undone.',
  },
  cancelSignUpDialog: {
    title: 'Cancel Sign-Up',
    description:
      'Are you sure you want to cancel your sign-up for this item? This action cannot be undone.',
  },
  cancelOpenItemDialog: {
    title: 'Cancel Sign-Up',
    description:
      'Are you sure you want to remove this item you offered to bring? This action cannot be undone.',
  },
};

/**
 * Phase 7D.1 step 22: volunteer-facing copy. Mirrors the default shape but
 * relabels list-, button-, and dialog-level text for volunteer lists. Uses
 * `volunteerCommitmentLabels` for the nested modal so the experience is
 * end-to-end volunteer-branded.
 */
export const volunteerSectionLabels: SignUpListsSectionLabels = {
  sectionHeading: (count) =>
    `Volunteer Roles (This event has ${count} volunteer ${count === 1 ? 'list' : 'lists'})`,
  emptyStateOrganizer: {
    title: 'No volunteer lists for this event yet.',
    helperText: 'Create one to let attendees volunteer for specific roles!',
  },
  emptyStateAttendeeTitle: 'Volunteer Roles',
  emptyStateAttendeeDescription: 'No volunteer roles for this event yet.',
  signUpButton: 'Volunteer',
  updateSignUpButton: 'Update Volunteer Sign Up',
  cancelSignUpButton: 'Cancel Volunteer Sign Up',
  cancelCommitmentDialog: {
    title: 'Cancel Volunteer Commitment',
    description:
      'Are you sure you want to cancel your volunteer commitment? This action cannot be undone.',
  },
  cancelSignUpDialog: {
    title: 'Cancel Volunteer Sign-Up',
    description:
      'Are you sure you want to cancel your volunteer sign-up for this role? This action cannot be undone.',
  },
  cancelOpenItemDialog: {
    title: 'Cancel Volunteer Sign-Up',
    description:
      'Are you sure you want to cancel this volunteer sign-up? This action cannot be undone.',
  },
  commitmentModal: volunteerCommitmentLabels,
};

/**
 * Props for SignUpManagementSection
 */
interface SignUpManagementSectionProps {
  eventId: string;
  userId?: string; // Current user ID (undefined if not logged in)
  isOrganizer?: boolean; // Is current user the event organizer
  /**
   * Optional copy overrides. Omit to keep existing UX unchanged.
   */
  labels?: SignUpListsSectionLabels;
  /**
   * Phase 7D.1 step 22: filter fetched lists by kind. Omit to fetch unfiltered
   * (existing behaviour — backward compatible with all pre-7D.1 callers).
   */
  kind?: SignUpKind;
}

/**
 * Phase 6A.132: dnd-kit requires `useSortable` to be called per-item, and React
 * hooks cannot be called in a loop inside the parent component. This wrapper
 * moves the hook out, and exposes the sortable primitives via a render prop so
 * the large per-item JSX can continue to close over its parent-scope helpers
 * (modals, mutations, labels) without hoisting them into props.
 */
interface SortableSignUpItemProps {
  id: string;
  disabled: boolean;
  children: (args: {
    setNodeRef: (node: HTMLElement | null) => void;
    style: React.CSSProperties;
    attributes: ReturnType<typeof useSortable>['attributes'];
    listeners: ReturnType<typeof useSortable>['listeners'];
    isDragging: boolean;
  }) => React.ReactNode;
}

function SortableSignUpItem({ id, disabled, children }: SortableSignUpItemProps) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id, disabled });
  const style: React.CSSProperties = {
    transform: CSS.Transform.toString(transform),
    transition,
  };
  return <>{children({ setNodeRef, style, attributes, listeners, isDragging })}</>;
}

/**
 * SignUpManagementSection Component
 */
export function SignUpManagementSection({
  eventId,
  userId,
  isOrganizer = false,
  labels,
  kind,
}: SignUpManagementSectionProps) {
  const router = useRouter();
  const effectiveLabels = labels ?? defaultSignUpListsSectionLabels;
  const [commitDialogOpen, setCommitDialogOpen] = useState(false);
  const [selectedSignUpId, setSelectedSignUpId] = useState<string | null>(null);
  const [itemDescription, setItemDescription] = useState('');
  const [quantity, setQuantity] = useState(1);

  // Category-based sign-up modal state
  const [commitModalOpen, setCommitModalOpen] = useState(false);
  const [selectedItem, setSelectedItem] = useState<SignUpItemDto | null>(null);
  const [selectedSignUpListId, setSelectedSignUpListId] = useState<string>('');
  const [selectedExistingCommitment, setSelectedExistingCommitment] = useState<SignUpCommitmentDto | null>(null);

  // Organizer delete confirmation state
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);

  // Tab state for multiple sign-up lists
  const [activeTabId, setActiveTabId] = useState<string | null>(null);

  // Phase 6A.118: Track expanded/collapsed state for signup items (default: collapsed)
  const [expandedItems, setExpandedItems] = useState<Set<string>>(new Set());

  // Phase 6A.118: Toggle function for expand/collapse
  const toggleItemExpanded = (itemId: string) => {
    setExpandedItems(prev => {
      const newSet = new Set(prev);
      if (newSet.has(itemId)) {
        newSet.delete(itemId);
      } else {
        newSet.add(itemId);
      }
      return newSet;
    });
  };

  // GitHub Issue #31: Unified cancel dialog state (replaces ugly browser confirm())
  type CancelDialogType = 'commitment' | 'signUpItem' | 'openItem';
  interface CancelDialogState {
    open: boolean;
    type: CancelDialogType | null;
    signUpListId: string;
    itemId?: string;
  }
  const [cancelDialog, setCancelDialog] = useState<CancelDialogState>({
    open: false,
    type: null,
    signUpListId: '',
    itemId: undefined,
  });
  const [isCancelPending, setIsCancelPending] = useState(false);

  // Phase 6A.27: Open sign-up item modal state
  const [openItemModalOpen, setOpenItemModalOpen] = useState(false);
  const [openItemSignUpListId, setOpenItemSignUpListId] = useState<string>('');
  const [openItemSignUpListCategory, setOpenItemSignUpListCategory] = useState<string>('');
  const [editingOpenItem, setEditingOpenItem] = useState<SignUpItemDto | null>(null);

  // Fetch sign-up lists (Phase 7D.1: optional kind filter)
  const { data: signUpLists, isLoading, error } = useEventSignUps(eventId, kind);

  // Mutations
  const commitToSignUp = useCommitToSignUp();
  const cancelCommitment = useCancelCommitment();
  const commitToSignUpItem = useCommitToSignUpItem();
  const commitToSignUpItemAnonymous = useCommitToSignUpItemAnonymous(); // GitHub Issue #41
  const removeSignUpListMutation = useRemoveSignUpList();

  // Phase 6A.27: Open sign-up item mutations
  const addOpenSignUpItem = useAddOpenSignUpItem();
  const addOpenSignUpItemAnonymous = useAddOpenSignUpItemAnonymous();
  const updateOpenSignUpItem = useUpdateOpenSignUpItem();
  const cancelOpenSignUpItem = useCancelOpenSignUpItem();

  // Phase 6A.132: Drag-drop reorder (organizer-only)
  const reorderSignUpItems = useReorderSignUpItems();

  // Phase 6A.132: dnd-kit sensors — 8px activation distance lets clicks on
  // non-handle regions still work; keyboard coordinate getter enables a11y.
  const dragSensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  );

  // Auth store for user info
  const { user } = useAuthStore();

  // Initialize active tab on first load (moved here to fix hooks order)
  React.useEffect(() => {
    if (activeTabId === null && signUpLists && signUpLists.length > 0) {
      setActiveTabId(signUpLists[0].id);
    }
  }, [signUpLists]);

  // GitHub Issue #31: Helper function to get dialog content based on cancel type
  const getCancelDialogContent = (type: CancelDialogType | null) => {
    switch (type) {
      case 'commitment':
        return effectiveLabels.cancelCommitmentDialog;
      case 'signUpItem':
        return effectiveLabels.cancelSignUpDialog;
      case 'openItem':
        return effectiveLabels.cancelOpenItemDialog;
      default:
        return { title: '', description: '' };
    }
  };

  // GitHub Issue #31: Unified cancel confirm handler
  const handleCancelConfirm = async () => {
    if (!cancelDialog.type || !userId) return;

    setIsCancelPending(true);
    try {
      switch (cancelDialog.type) {
        case 'commitment':
          await cancelCommitment.mutateAsync({
            eventId,
            signupId: cancelDialog.signUpListId,
            userId,
          });
          break;
        case 'signUpItem':
          await commitToSignUpItem.mutateAsync({
            eventId,
            signupId: cancelDialog.signUpListId,
            itemId: cancelDialog.itemId!,
            userId,
            quantity: 0, // Signal full cancellation
            notes: '',
            contactName: '',
            contactEmail: '',
            contactPhone: '',
          });
          break;
        case 'openItem':
          await cancelOpenSignUpItem.mutateAsync({
            eventId,
            signupId: cancelDialog.signUpListId,
            itemId: cancelDialog.itemId!,
          });
          break;
      }
    } catch (error) {
      console.error(`Failed to cancel ${cancelDialog.type}:`, error);
      // Error is handled by mutation's onError or we can add toast here
    } finally {
      setIsCancelPending(false);
      setCancelDialog({ open: false, type: null, signUpListId: '', itemId: undefined });
    }
  };

  // Handle commit to sign-up
  const handleCommit = async (signUpId: string) => {
    if (!userId) {
      toast.error('Please log in to commit to items');
      return;
    }

    if (!itemDescription.trim()) {
      toast.error('Please enter an item description');
      return;
    }

    try {
      await commitToSignUp.mutateAsync({
        eventId,
        signupId: signUpId,
        userId,
        itemDescription: itemDescription.trim(),
        quantity,
      });

      // Reset form
      setItemDescription('');
      setQuantity(1);
      setCommitDialogOpen(false);
      setSelectedSignUpId(null);
    } catch (err) {
      console.error('Failed to commit:', err);
      toast.error('Failed to commit. Please try again.');
    }
  };

  // Handle cancel commitment - GitHub Issue #31: Opens styled dialog instead of browser confirm()
  const handleCancel = (signUpId: string) => {
    if (!userId) return;
    setCancelDialog({
      open: true,
      type: 'commitment',
      signUpListId: signUpId,
      itemId: undefined,
    });
  };

  // Handle commit to specific item (category-based) via modal
  // Phase 2: Now includes contact information
  const handleCommitToItem = async (data: CommitmentFormData) => {
    await commitToSignUpItem.mutateAsync({
      eventId,
      signupId: data.signUpListId,
      itemId: data.itemId,
      userId: data.userId,
      quantity: data.quantity,
      notes: data.notes,
      contactName: data.contactName,
      contactEmail: data.contactEmail,
      contactPhone: data.contactPhone,
    });
  };

  // Phase 6A.23: Handle anonymous commit to specific item
  // Used when user is not logged in but is registered for event
  // GitHub Issue #41: Uses React Query mutation for proper cache invalidation (no page reload)
  const handleCommitToItemAnonymous = async (data: AnonymousCommitmentFormData) => {
    await commitToSignUpItemAnonymous.mutateAsync({
      eventId,
      signupId: data.signUpListId,
      itemId: data.itemId,
      contactEmail: data.contactEmail,
      quantity: data.quantity,
      notes: data.notes,
      contactName: data.contactName,
      contactPhone: data.contactPhone,
    });
    // Cache is automatically invalidated by the mutation hook
    // No page reload needed - scroll position is preserved
  };

  // Handle cancel sign-up item commitment (Phase 6A.20)
  // GitHub Issue #31: Opens styled dialog instead of browser confirm()
  const handleCancelSignUp = (signUpListId: string, itemId: string) => {
    if (!userId) {
      toast.error('Please log in to cancel sign-ups');
      return;
    }
    setCancelDialog({
      open: true,
      type: 'signUpItem',
      signUpListId,
      itemId,
    });
  };

  // Open commitment modal - Phase 6A.23: Available for ALL users (logged in or not)
  // User enters email in modal, validation happens on submit (not on button click)
  // Pass existing commitment if available (for pre-filling the form)
  const openCommitmentModal = (signUpListId: string, item: SignUpItemDto, existingCommitment?: SignUpCommitmentDto) => {
    // Phase 6A.23: NO login check here - modal opens for everyone
    // Email validation happens when user submits the form
    setSelectedSignUpListId(signUpListId);
    setSelectedItem(item);
    setSelectedExistingCommitment(existingCommitment || null);
    setCommitModalOpen(true);
  };

  // Handle delete sign-up list (organizer only)
  const handleDeleteSignUpList = async (signupId: string) => {
    try {
      await removeSignUpListMutation.mutateAsync({ eventId, signupId });
      setDeleteConfirmId(null);
    } catch (err) {
      console.error('Failed to delete sign-up list:', err);
      toast.error('Failed to delete sign-up list. Please try again.');
    }
  };

  // Get category badge color - Phase 6A.28: Updated to match item card colors
  // Avoiding green since it's used for Registration details section
  const getCategoryColor = (category: SignUpItemCategory) => {
    switch (category) {
      case SignUpItemCategory.Mandatory:
        return 'bg-orange-100 text-orange-800 border-orange-300';
      case SignUpItemCategory.Preferred:
        return 'bg-rose-100 text-rose-800 border-rose-300';
      case SignUpItemCategory.Suggested:
        return 'bg-sky-100 text-sky-800 border-sky-300';
      case SignUpItemCategory.Open:
        return 'bg-violet-100 text-violet-800 border-violet-300';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-300';
    }
  };

  // Get item card background and border colors based on category
  // Phase 6A.28: Distinct colors matching LankaConnect theme (Saffron/Orange primary, Maroon secondary)
  // Avoiding green since it's used for Registration details section
  const getItemCardStyle = (category: SignUpItemCategory) => {
    switch (category) {
      case SignUpItemCategory.Mandatory:
        // Orange/Saffron - LankaConnect primary color, indicates required items
        return 'bg-orange-50 border-l-[6px] border-l-orange-500 border border-orange-200 shadow-sm';
      case SignUpItemCategory.Preferred:
        // Rose/Pink - indicates preferred items (hidden but kept for data consistency)
        return 'bg-rose-50 border-l-[6px] border-l-rose-500 border border-rose-200 shadow-sm';
      case SignUpItemCategory.Suggested:
        // Sky blue - cool, calm optional suggestions
        return 'bg-sky-50 border-l-[6px] border-l-sky-500 border border-sky-200 shadow-sm';
      case SignUpItemCategory.Open:
        // Violet/Purple - creative, user-contributed items
        return 'bg-violet-50 border-l-[6px] border-l-violet-500 border border-violet-200 shadow-sm';
      default:
        return 'bg-gray-50 border-l-[6px] border-l-gray-500 border border-gray-200 shadow-sm';
    }
  };

  // Get category label - Phase 6A.28: Updated to include "Items" suffix for clarity
  const getCategoryLabel = (category: SignUpItemCategory) => {
    switch (category) {
      case SignUpItemCategory.Mandatory:
        return 'Mandatory Items';
      case SignUpItemCategory.Preferred:
        return 'Preferred Items';
      case SignUpItemCategory.Suggested:
        return 'Suggested Items';
      case SignUpItemCategory.Open:
        return 'Open Items';
      default:
        return 'Unknown Items';
    }
  };

  // Phase 6A.27: Open item handlers
  // Phase 6A.44: Allow anonymous users to add Open Items
  const openAddOpenItemModal = (signUpListId: string, signUpListCategory: string) => {
    setOpenItemSignUpListId(signUpListId);
    setOpenItemSignUpListCategory(signUpListCategory);
    setEditingOpenItem(null);
    setOpenItemModalOpen(true);
  };

  const openEditOpenItemModal = (signUpListId: string, signUpListCategory: string, item: SignUpItemDto) => {
    if (!userId) {
      toast.error('Please log in to edit items');
      return;
    }
    setOpenItemSignUpListId(signUpListId);
    setOpenItemSignUpListCategory(signUpListCategory);
    setEditingOpenItem(item);
    setOpenItemModalOpen(true);
  };

  // Phase 6A.44: Support both authenticated and anonymous users for Open Items
  const handleOpenItemSubmit = async (data: OpenItemFormData) => {
    // Anonymous users must provide contact info
    if (!userId && !data.contactEmail) {
      throw new Error('Email is required for anonymous users');
    }

    if (editingOpenItem) {
      // Update existing Open item (only authenticated users can update)
      await updateOpenSignUpItem.mutateAsync({
        eventId,
        signupId: openItemSignUpListId,
        itemId: editingOpenItem.id,
        itemName: data.itemName,
        quantity: data.quantity,
        notes: data.notes,
        contactName: data.contactName,
        contactEmail: data.contactEmail,
        contactPhone: data.contactPhone,
      });
    } else {
      // Add new Open item - use appropriate endpoint based on auth status
      if (userId) {
        // Authenticated user
        await addOpenSignUpItem.mutateAsync({
          eventId,
          signupId: openItemSignUpListId,
          itemName: data.itemName,
          quantity: data.quantity,
          notes: data.notes,
          contactName: data.contactName,
          contactEmail: data.contactEmail,
          contactPhone: data.contactPhone,
        });
      } else {
        // Anonymous user - use anonymous endpoint
        await addOpenSignUpItemAnonymous.mutateAsync({
          eventId,
          signupId: openItemSignUpListId,
          contactEmail: data.contactEmail!, // Required for anonymous users
          itemName: data.itemName,
          quantity: data.quantity,
          notes: data.notes,
          contactName: data.contactName,
          contactPhone: data.contactPhone,
        });
      }
    }
  };

  const handleOpenItemCancel = async () => {
    if (!editingOpenItem || !userId) return;

    await cancelOpenSignUpItem.mutateAsync({
      eventId,
      signupId: openItemSignUpListId,
      itemId: editingOpenItem.id,
    });
  };

  // Handle direct cancel of open item from list
  // GitHub Issue #31: Opens styled dialog instead of browser confirm()
  const handleCancelOpenItem = (signUpListId: string, itemId: string) => {
    if (!userId) {
      toast.error('Please log in to cancel sign-ups');
      return;
    }
    setCancelDialog({
      open: true,
      type: 'openItem',
      signUpListId,
      itemId,
    });
  };

  // Loading state
  if (isLoading) {
    return (
      <div className="py-8">
        <p className="text-center text-muted-foreground">Loading sign-up lists...</p>
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div className="py-8">
        <p className="text-center text-destructive">
          Failed to load sign-up lists. Please try again.
        </p>
      </div>
    );
  }

  // No sign-up lists
  if (!signUpLists || signUpLists.length === 0) {
    // Issue 3 Fix: Don't show duplicate Card on manage page (isOrganizer=true)
    // The manage page already has a Card wrapper with header/actions
    if (isOrganizer) {
      return (
        <div className="py-8 text-center text-muted-foreground">
          <p>{effectiveLabels.emptyStateOrganizer.title}</p>
          <p className="text-sm mt-2">{effectiveLabels.emptyStateOrganizer.helperText}</p>
        </div>
      );
    }

    // For attendee view (event detail page), show Card
    return (
      <div className="py-8">
        <Card>
          <CardHeader>
            <CardTitle>{effectiveLabels.emptyStateAttendeeTitle}</CardTitle>
            <CardDescription>
              {effectiveLabels.emptyStateAttendeeDescription}
            </CardDescription>
          </CardHeader>
        </Card>
      </div>
    );
  }

  // Determine which lists to show
  const listsToShow = activeTabId
    ? signUpLists.filter(list => list.id === activeTabId)
    : signUpLists;

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">
        {effectiveLabels.sectionHeading(signUpLists.length)}
      </h2>

      {/* Tab navigation - Only show if multiple lists */}
      {signUpLists.length > 1 && (
        <div className="flex gap-2 border-b border-gray-200 overflow-x-auto">
          {signUpLists.map((list) => (
            <button
              key={list.id}
              onClick={() => setActiveTabId(list.id)}
              className={`px-4 py-2 font-medium text-sm whitespace-nowrap border-b-2 transition-colors ${
                activeTabId === list.id
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-600 hover:text-gray-900'
              }`}
            >
              {list.category}
              {/* Phase 6A.28 Issue 2 Fix: Remove commitment count from tabs - redundant info already shown per item */}
            </button>
          ))}
        </div>
      )}

      {listsToShow.map((signUpList) => {
        // Check if current user has committed to this list
        const userCommitment = signUpList.commitments.find((c) => c.userId === userId);

        // Check if this is a category-based sign-up (has items or has category flags)
        // Phase 6A.28: hasPreferredItems kept for backwards compatibility but Preferred UI is hidden
        const isCategoryBased = (signUpList.items && signUpList.items.length > 0) ||
          signUpList.hasMandatoryItems || signUpList.hasPreferredItems ||
          signUpList.hasSuggestedItems || signUpList.hasOpenItems;

        // Phase 6A.27: Get Open items (user-submitted)
        const openItems = signUpList.items?.filter(item => item.isOpenItem) || [];
        const userOpenItems = openItems.filter(item => item.createdByUserId === userId);

        return (
          <Card key={signUpList.id}>
            <CardHeader>
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <CardTitle>{signUpList.category}</CardTitle>
                  <CardDescription>{signUpList.description}</CardDescription>
                </div>
                {isOrganizer && (
                  <div className="flex gap-2 ml-4">
                    {deleteConfirmId === signUpList.id ? (
                      <>
                        <Button
                          variant="destructive"
                          size="sm"
                          onClick={() => handleDeleteSignUpList(signUpList.id)}
                          disabled={removeSignUpListMutation.isPending}
                        >
                          Confirm Delete
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => setDeleteConfirmId(null)}
                        >
                          Cancel
                        </Button>
                      </>
                    ) : (
                      <>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() =>
                            router.push(
                              signUpList.kind === SignUpKind.Volunteers
                                ? `/events/${eventId}/volunteer-lists/${signUpList.id}`
                                : `/events/${eventId}/signup-lists/${signUpList.id}`
                            )
                          }
                          className="text-orange-600 hover:text-orange-700"
                        >
                          <Edit className="h-4 w-4 mr-2" />
                          Edit
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => setDeleteConfirmId(signUpList.id)}
                          className="text-red-600 hover:text-red-700"
                        >
                          <Trash2 className="h-4 w-4 mr-2" />
                          Delete
                        </Button>
                      </>
                    )}
                  </div>
                )}
              </div>
            </CardHeader>

            <CardContent>
              {/* CATEGORY-BASED SIGN-UPS (NEW) */}
              {isCategoryBased ? (
                <div className="space-y-6">
                  {/* Phase 6A.119: Tab-based Category Navigation */}
                  {(() => {
                    // Filter items by category
                    const mandatoryItems = signUpList.items.filter(
                      item => item.itemCategory === SignUpItemCategory.Mandatory && !item.isOpenItem
                    );
                    const suggestedItems = signUpList.items.filter(
                      item => item.itemCategory === SignUpItemCategory.Suggested && !item.isOpenItem
                    );
                    const openItems = signUpList.items.filter(item => item.isOpenItem);

                    // Phase 6A.132: Drag-drop reorder handler — operates on a single category
                    // (Mandatory, Suggested, Open) but the backend enforces exact-set equality
                    // across the whole list, so we merge the reordered sub-sequence back into
                    // the full ordered item array before sending.
                    const makeHandleCategoryDragEnd = (categoryItems: SignUpItemDto[]) =>
                      (event: DragEndEvent) => {
                        const { active, over } = event;
                        if (!over || active.id === over.id) return;
                        const oldIndex = categoryItems.findIndex(i => i.id === active.id);
                        const newIndex = categoryItems.findIndex(i => i.id === over.id);
                        if (oldIndex === -1 || newIndex === -1) return;
                        const reorderedCategory = arrayMove(categoryItems, oldIndex, newIndex);
                        const categoryIdSet = new Set(categoryItems.map(i => i.id));
                        const reorderedIter = reorderedCategory[Symbol.iterator]();
                        const orderedItemIds = signUpList.items.map(fullItem =>
                          categoryIdSet.has(fullItem.id)
                            ? (reorderedIter.next().value as SignUpItemDto).id
                            : fullItem.id
                        );
                        reorderSignUpItems.mutate(
                          { eventId, signupId: signUpList.id, orderedItemIds },
                          {
                            onError: (err: Error) => {
                              toast.error(err.message || 'Reorder failed. Please try again.');
                            },
                          }
                        );
                      };

                    // Helper function to render items for a category
                    const renderCategoryItems = (items: SignUpItemDto[], category: SignUpItemCategory) => (
                      <DndContext
                        sensors={dragSensors}
                        collisionDetection={closestCenter}
                        onDragEnd={makeHandleCategoryDragEnd(items)}
                      >
                        <SortableContext
                          items={items.map(i => i.id)}
                          strategy={verticalListSortingStrategy}
                        >
                          <div className="space-y-3">
                            {items.map((item) => {
                              const userItemCommitment = item.commitments.find(c => c.userId === userId);
                              const totalQty = isQuantityBased(item) ? item.targetQuantity : item.totalSlots;
                              const committedQty = isQuantityBased(item) ? item.committedQuantity : item.filledSlots;
                              const remainingQty = isQuantityBased(item) ? item.remainingQuantity : item.remainingSlots;
                              const percentCommitted = Math.round((committedQty / totalQty) * 100);
                              const isExpanded = expandedItems.has(item.id);

                              return (
                                <SortableSignUpItem key={item.id} id={item.id} disabled={!isOrganizer}>
                                  {({ setNodeRef, style, attributes, listeners, isDragging }) => (
                                    <div
                                      ref={setNodeRef}
                                      style={style}
                                      className={`rounded-lg p-4 space-y-2 ${getItemCardStyle(category)} ${isDragging ? 'opacity-60 ring-2 ring-orange-400 shadow-lg' : ''}`}
                                    >
                                      {/* Phase 6A.132: Drag handle — organizer-only. touch-none
                                          keeps mobile pointer events from scrolling the page during drag. */}
                                      {isOrganizer && (
                                        <button
                                          type="button"
                                          {...attributes}
                                          {...listeners}
                                          aria-label={`Drag to reorder ${item.itemDescription}`}
                                          className="inline-flex items-center gap-1 rounded p-1 text-neutral-400 hover:bg-neutral-100 hover:text-neutral-600 cursor-grab active:cursor-grabbing touch-none"
                                        >
                                          <GripVertical className="h-4 w-4" />
                                          <span className="sr-only">Drag to reorder</span>
                                        </button>
                                      )}
                                {/* Phase 6A.118: Item Header - Always Visible */}
                                <div className="flex items-start gap-2">
                                  <div className="flex-1 min-w-0">
                                    <div className="flex items-center gap-2 flex-wrap">
                                      <p className="font-medium">{item.itemDescription}</p>
                                      <span className="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded-full font-semibold">
                                        {isQuantityBased(item) ? 'Suggested Quantity' : 'Suggested Slots'}: {totalQty}
                                      </span>
                                    </div>
                                    {item.notes && (
                                      <p className="text-sm text-muted-foreground mt-1">{item.notes}</p>
                                    )}
                                    {/* Phase 6A.118: Show commitment status in collapsed view */}
                                    <div className="text-xs text-muted-foreground mt-1 flex gap-3">
                                      <span>{committedQty} of {totalQty} filled</span>
                                      <span className={remainingQty === 0 ? 'text-green-600 font-medium' : ''}>
                                        {remainingQty} remaining
                                      </span>
                                    </div>
                                  </div>

                                  {/* Explicit Show/Hide details pill — matches CollapsibleSection affordance.
                                      Text hidden on mobile so the pill collapses to chevron-only on narrow screens. */}
                                  <button
                                    onClick={() => toggleItemExpanded(item.id)}
                                    aria-label={isExpanded ? 'Collapse item details' : 'Expand item details'}
                                    aria-expanded={isExpanded}
                                    className="inline-flex items-center gap-1.5 flex-shrink-0 rounded-full border border-neutral-300 bg-white px-2.5 py-1 text-xs font-medium text-neutral-700 shadow-sm hover:border-neutral-400 hover:bg-neutral-50 transition-colors"
                                  >
                                    <span className="hidden sm:inline">
                                      {isExpanded ? 'Hide details' : 'Show details'}
                                    </span>
                                    <ChevronDown
                                      className={`h-3.5 w-3.5 transition-transform duration-200 ${isExpanded ? 'rotate-180' : ''}`}
                                      aria-hidden="true"
                                    />
                                  </button>
                                </div>

                                {/* Phase 6A.123: Sign Up/Update button - Always Visible (outside collapsible) */}
                                {!isOrganizer && (remainingQty > 0 || userItemCommitment) && (
                                  <div className="flex gap-2">
                                    <Button
                                      onClick={() => openCommitmentModal(signUpList.id, item, userItemCommitment)}
                                      size="sm"
                                      variant={userItemCommitment ? "default" : "outline"}
                                    >
                                      {userItemCommitment ? effectiveLabels.updateSignUpButton : effectiveLabels.signUpButton}
                                    </Button>
                                    {userItemCommitment && (
                                      <Button
                                        onClick={() => handleCancelSignUp(signUpList.id, item.id)}
                                        size="sm"
                                        variant="destructive"
                                      >
                                        {effectiveLabels.cancelSignUpButton}
                                      </Button>
                                    )}
                                  </div>
                                )}

                                {/* Phase 6A.118: Details - Conditionally Visible (Collapsible) */}
                                {isExpanded && (
                                  <>
                                    {/* Progress bar with counts */}
                                <div className="space-y-1">
                                  <div className="w-full bg-gray-200 rounded-full h-2">
                                    <div
                                      className={`h-2 rounded-full ${
                                        percentCommitted === 100 ? 'bg-green-500' : 'bg-blue-500'
                                      }`}
                                      style={{ width: `${percentCommitted}%` }}
                                    />
                                  </div>
                                  <div className="text-xs text-muted-foreground flex justify-between">
                                    <span>{committedQty} of {totalQty} filled</span>
                                    <span>{remainingQty} remaining</span>
                                  </div>
                                </div>

                                {/* Participants - Show names and quantities */}
                                {item.commitments.length > 0 && (
                                  <div className="mt-3">
                                    <table className="min-w-full text-sm">
                                      <thead>
                                        <tr className="border-b">
                                          <th className="text-left py-1 px-2 font-medium text-muted-foreground">Name</th>
                                          <th className="text-left py-1 px-2 font-medium text-muted-foreground">{isQuantityBased(item) ? 'Quantity' : 'Slots'}</th>
                                        </tr>
                                      </thead>
                                      <tbody>
                                        {item.commitments.map((commitment) => (
                                          <tr key={commitment.id} className="border-b">
                                            <td className="py-1 px-2">
                                              {commitment.contactName || 'Anonymous'}
                                              {commitment.userId === userId && (
                                                <span className="text-xs text-blue-600 font-medium ml-1">(You)</span>
                                              )}
                                            </td>
                                            <td className="py-1 px-2">{commitment.quantity}</td>
                                          </tr>
                                        ))}
                                      </tbody>
                                    </table>
                                  </div>
                                )}

                                {/* Your commitment info */}
                                {userItemCommitment && (
                                  <div className="mt-2 p-2 bg-blue-50 border border-blue-200 rounded">
                                    <p className="text-sm font-medium text-blue-800">
                                      {isQuantityBased(item)
                                        ? `You committed to bring ${userItemCommitment.quantity} of this item`
                                        : `You claimed ${userItemCommitment.quantity} slot(s) for this item`}
                                    </p>
                                  </div>
                                )}

                                    {/* All slots filled message */}
                                    {remainingQty === 0 && (
                                      <div className="mt-2 p-2 bg-green-50 border border-green-200 rounded">
                                        <p className="text-sm font-medium text-green-800">
                                          ✓ All {totalQty} slots filled - Thank you everyone!
                                        </p>
                                      </div>
                                    )}
                                  </>
                                )}
                                    </div>
                                  )}
                                </SortableSignUpItem>
                              );
                            })}
                          </div>
                        </SortableContext>
                      </DndContext>
                    );

                    // Build tabs array (only non-empty categories)
                    const categoryTabs: Tab[] = [];

                    if (mandatoryItems.length > 0) {
                      categoryTabs.push({
                        id: 'mandatory',
                        label: `Mandatory Items (${mandatoryItems.length})`,
                        icon: AlertCircle,
                        content: renderCategoryItems(mandatoryItems, SignUpItemCategory.Mandatory)
                      });
                    }

                    if (suggestedItems.length > 0) {
                      categoryTabs.push({
                        id: 'suggested',
                        label: `Suggested Items (${suggestedItems.length})`,
                        icon: Lightbulb,
                        content: renderCategoryItems(suggestedItems, SignUpItemCategory.Suggested)
                      });
                    }

                    // Phase 6A.27: Open Items as Tab
                    // IMPORTANT: Show tab whenever hasOpenItems is enabled, even with 0 items
                    // Users need to see the "Sign Up" button to add their own items
                    if (signUpList.hasOpenItems) {
                      categoryTabs.push({
                        id: 'open',
                        label: `Open Items (${openItems.length})`,
                        icon: Plus,
                        // Phase 6A.120: Custom purple styling for Open Items tab
                        className: 'open-items-tab',
                        style: {
                          borderColor: '#9333EA',
                        },
                        content: (
                    <div className="space-y-3">
                      {/* Phase 6A.120: Header with Sign Up button in top right */}
                      <div className="flex justify-between items-start gap-4 border-b pb-3">
                        <div className="flex-1">
                          <h4 className="font-semibold flex items-center gap-2 mb-2">
                            <span className={`px-3 py-1.5 rounded-md text-sm font-semibold border ${getCategoryColor(SignUpItemCategory.Open)}`}>
                              {getCategoryLabel(SignUpItemCategory.Open)}
                            </span>
                            <span className="text-sm text-muted-foreground">
                              (Bring your own item)
                            </span>
                          </h4>
                          <p className="text-sm text-muted-foreground">
                            You can add your own item to bring to this sign-up list.
                          </p>
                        </div>

                        {/* Phase 6A.120: Sign Up button moved to top right */}
                        {/* Phase 6A.44: Allow anonymous sign-ups for Open Items */}
                        {/* Phase 6A.28 Issue 1 Fix: Hide buttons on manage page (isOrganizer=true) */}
                        {!isOrganizer && (
                          <Button
                            onClick={() => openAddOpenItemModal(signUpList.id, signUpList.category)}
                            size="sm"
                            variant="default"
                            className="flex-shrink-0"
                            style={{
                              background: '#FF7900',
                              color: 'white',
                              fontWeight: 600
                            }}
                          >
                            <Plus className="h-4 w-4 mr-1" />
                            Sign Up
                          </Button>
                        )}
                      </div>

                      {/* Display existing Open items */}
                      {openItems.length > 0 ? (
                        <div className="space-y-3 mt-4">
                          {openItems.map((item) => {
                            const isOwnItem = item.createdByUserId === userId;
                            const commitment = item.commitments?.[0];

                            return (
                              <div key={item.id} className={`rounded-lg p-4 space-y-2 ${getItemCardStyle(SignUpItemCategory.Open)}`}>
                                <div className="flex justify-between items-start">
                                  <div className="flex-1">
                                    <div className="flex items-center gap-2">
                                      <p className="font-medium">{item.itemDescription}</p>
                                      <span className="text-xs bg-purple-100 text-purple-800 px-2 py-1 rounded-full font-semibold">
                                        Qty: {isQuantityBased(item) ? item.targetQuantity : item.totalSlots}
                                      </span>
                                      {isOwnItem && (
                                        <span className="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded-full font-medium">
                                          Your item
                                        </span>
                                      )}
                                    </div>
                                    {item.notes && (
                                      <p className="text-sm text-muted-foreground mt-1">{item.notes}</p>
                                    )}
                                    {commitment && (
                                      <p className="text-sm text-muted-foreground mt-1">
                                        Brought by: {commitment.contactName || 'Anonymous'}
                                        {commitment.contactEmail && ` (${commitment.contactEmail})`}
                                      </p>
                                    )}
                                  </div>
                                </div>

                                {/* Update/Cancel buttons for own items */}
                                {/* Phase 6A.28 Issue 1 Fix: Hide buttons on manage page (isOrganizer=true) */}
                                {!isOrganizer && isOwnItem && (
                                  <div className="mt-3 flex gap-2">
                                    <Button
                                      onClick={() => openEditOpenItemModal(signUpList.id, signUpList.category, item)}
                                      size="sm"
                                      variant="default"
                                    >
                                      Update Sign Up
                                    </Button>
                                    {/* GitHub Issue #31: Button now opens styled dialog */}
                                    <Button
                                      onClick={() => handleCancelOpenItem(signUpList.id, item.id)}
                                      size="sm"
                                      variant="destructive"
                                    >
                                      Cancel Sign Up
                                    </Button>
                                  </div>
                                )}
                              </div>
                            );
                          })}
                        </div>
                      ) : (
                        <p className="text-sm text-muted-foreground italic mt-4">
                          No one has signed up with their own item yet. Be the first!
                        </p>
                      )}
                    </div>
                        )
                      });
                    }

                    // Return TabPanel if there are tabs, otherwise show empty state
                    if (categoryTabs.length === 0) {
                      return (
                        <div className="text-center py-8 text-muted-foreground">
                          No items in this signup list yet
                        </div>
                      );
                    }

                    // Phase 6A.118 Fix: Remove defaultTab to prevent tab reset on state changes
                    return <TabPanel tabs={categoryTabs} />;
                  })()}
                </div>
              ) : (
                /* LEGACY OPEN/PREDEFINED SIGN-UPS */
                <>
                  {/* Predefined items */}
                  {signUpList.signUpType === SignUpType.Predefined && signUpList.predefinedItems.length > 0 && (
                    <div className="mb-4">
                      <h4 className="font-semibold mb-2">Suggested Items:</h4>
                      <ul className="list-disc list-inside text-sm text-muted-foreground">
                        {signUpList.predefinedItems.map((item, index) => (
                          <li key={index}>{item}</li>
                        ))}
                      </ul>
                    </div>
                  )}

                  {/* Existing commitments */}
                  {signUpList.commitments.length > 0 ? (
                    <div>
                      <h4 className="font-semibold mb-2">
                        {/* Phase 6A.28 Issue 2 Fix: Remove commitment count - redundant */}
                        Commitments:
                      </h4>
                      <div className="space-y-2">
                        {signUpList.commitments.map((commitment) => (
                          <div
                            key={commitment.id}
                            className="flex justify-between items-center p-2 bg-muted rounded-md"
                          >
                            <div>
                              <p className="font-medium">{commitment.itemDescription}</p>
                              <p className="text-sm text-muted-foreground">
                                Quantity: {commitment.quantity}
                              </p>
                            </div>
                            {commitment.userId === userId && (
                              <Button
                                variant="destructive"
                                size="sm"
                                onClick={() => handleCancel(signUpList.id)}
                                disabled={cancelCommitment.isPending}
                              >
                                {cancelCommitment.isPending ? 'Canceling...' : 'Cancel'}
                              </Button>
                            )}
                          </div>
                        ))}
                      </div>
                    </div>
                  ) : (
                    <p className="text-sm text-muted-foreground">No commitments yet. Be the first!</p>
                  )}
                </>
              )}
            </CardContent>

            {/* Footer only for legacy sign-ups */}
            {!isCategoryBased && (
              <CardFooter>
                {!userCommitment && userId && (
                  <div className="w-full space-y-3">
                    {selectedSignUpId === signUpList.id ? (
                      <div className="space-y-3">
                        <div>
                          <label className="block text-sm font-medium mb-1">
                            Item Description *
                          </label>
                          <input
                            type="text"
                            value={itemDescription}
                            onChange={(e) => setItemDescription(e.target.value)}
                            placeholder="What will you bring?"
                            className="w-full px-3 py-2 border rounded-md"
                          />
                        </div>
                        <div>
                          <label className="block text-sm font-medium mb-1">Quantity</label>
                          <input
                            type="number"
                            min="1"
                            value={quantity}
                            onChange={(e) => setQuantity(parseInt(e.target.value) || 1)}
                            className="w-full px-3 py-2 border rounded-md"
                          />
                        </div>
                        <div className="flex gap-2">
                          <Button
                            onClick={() => handleCommit(signUpList.id)}
                            disabled={commitToSignUp.isPending || !itemDescription.trim()}
                          >
                            {commitToSignUp.isPending ? 'Committing...' : 'Confirm'}
                          </Button>
                          <Button
                            variant="outline"
                            onClick={() => {
                              setSelectedSignUpId(null);
                              setItemDescription('');
                              setQuantity(1);
                            }}
                          >
                            Cancel
                          </Button>
                        </div>
                      </div>
                    ) : (
                      <Button onClick={() => setSelectedSignUpId(signUpList.id)}>
                        I can bring something
                      </Button>
                    )}
                  </div>
                )}
                {!userId && (
                  <p className="text-sm text-muted-foreground">
                    Please log in to commit to items
                  </p>
                )}
              </CardFooter>
            )}
          </Card>
        );
      })}

      {/* Sign-Up Commitment Modal */}
      <SignUpCommitmentModal
        open={commitModalOpen}
        onOpenChange={setCommitModalOpen}
        item={selectedItem}
        signUpListId={selectedSignUpListId}
        eventId={eventId}
        existingCommitment={selectedExistingCommitment}
        onCommit={handleCommitToItem}
        onCommitAnonymous={handleCommitToItemAnonymous}
        isSubmitting={commitToSignUpItem.isPending}
        labels={effectiveLabels.commitmentModal}
        hideQuantitySelector={kind === SignUpKind.Volunteers}
      />

      {/* Phase 6A.27: Open Item Sign-Up Modal */}
      <OpenItemSignUpModal
        open={openItemModalOpen}
        onOpenChange={setOpenItemModalOpen}
        signUpListId={openItemSignUpListId}
        signUpListCategory={openItemSignUpListCategory}
        eventId={eventId}
        existingItem={editingOpenItem}
        onSubmit={handleOpenItemSubmit}
        onCancel={editingOpenItem ? handleOpenItemCancel : undefined}
        isSubmitting={addOpenSignUpItem.isPending || updateOpenSignUpItem.isPending || cancelOpenSignUpItem.isPending}
      />

      {/* GitHub Issue #31: Unified Cancel Confirmation Dialog */}
      <ConfirmDialog
        open={cancelDialog.open}
        onOpenChange={(open) => {
          if (!open) {
            setCancelDialog({ open: false, type: null, signUpListId: '', itemId: undefined });
          }
        }}
        title={getCancelDialogContent(cancelDialog.type).title}
        description={getCancelDialogContent(cancelDialog.type).description}
        confirmLabel="Yes, Cancel"
        cancelLabel="No, Keep It"
        onConfirm={handleCancelConfirm}
        variant="warning"
        isLoading={isCancelPending}
      />
    </div>
  );
}

export default SignUpManagementSection;
