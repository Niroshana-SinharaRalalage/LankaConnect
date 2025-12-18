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
  // Phase 6A.27: Open Sign-Up Items
  useAddOpenSignUpItem,
  useUpdateOpenSignUpItem,
  useCancelOpenSignUpItem,
} from '@/presentation/hooks/useEventSignUps';
import { SignUpType, SignUpItemCategory, SignUpItemDto, SignUpCommitmentDto } from '@/infrastructure/api/types/events.types';
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { SignUpCommitmentModal, CommitmentFormData, AnonymousCommitmentFormData } from './SignUpCommitmentModal';
import { OpenItemSignUpModal, OpenItemFormData } from './OpenItemSignUpModal';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import { Plus, Edit, Trash2 } from 'lucide-react';
import { useAuthStore } from '@/presentation/store/useAuthStore';

/**
 * Props for SignUpManagementSection
 */
interface SignUpManagementSectionProps {
  eventId: string;
  userId?: string; // Current user ID (undefined if not logged in)
  isOrganizer?: boolean; // Is current user the event organizer
}

/**
 * SignUpManagementSection Component
 */
export function SignUpManagementSection({
  eventId,
  userId,
  isOrganizer = false,
}: SignUpManagementSectionProps) {
  const router = useRouter();
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

  // Cancel sign-up state
  const [cancelConfirmId, setCancelConfirmId] = useState<string | null>(null);
  const [isCancelling, setIsCancelling] = useState(false);

  // Phase 6A.27: Open sign-up item modal state
  const [openItemModalOpen, setOpenItemModalOpen] = useState(false);
  const [openItemSignUpListId, setOpenItemSignUpListId] = useState<string>('');
  const [openItemSignUpListCategory, setOpenItemSignUpListCategory] = useState<string>('');
  const [editingOpenItem, setEditingOpenItem] = useState<SignUpItemDto | null>(null);

  // Fetch sign-up lists
  const { data: signUpLists, isLoading, error } = useEventSignUps(eventId);

  // Mutations
  const commitToSignUp = useCommitToSignUp();
  const cancelCommitment = useCancelCommitment();
  const commitToSignUpItem = useCommitToSignUpItem();
  const removeSignUpListMutation = useRemoveSignUpList();

  // Phase 6A.27: Open sign-up item mutations
  const addOpenSignUpItem = useAddOpenSignUpItem();
  const updateOpenSignUpItem = useUpdateOpenSignUpItem();
  const cancelOpenSignUpItem = useCancelOpenSignUpItem();

  // Auth store for user info
  const { user } = useAuthStore();

  // Initialize active tab on first load (moved here to fix hooks order)
  React.useEffect(() => {
    if (activeTabId === null && signUpLists && signUpLists.length > 0) {
      setActiveTabId(signUpLists[0].id);
    }
  }, [signUpLists]);

  // Handle commit to sign-up
  const handleCommit = async (signUpId: string) => {
    if (!userId) {
      alert('Please log in to commit to items');
      return;
    }

    if (!itemDescription.trim()) {
      alert('Please enter an item description');
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
      alert('Failed to commit. Please try again.');
    }
  };

  // Handle cancel commitment
  const handleCancel = async (signUpId: string) => {
    if (!userId) {
      return;
    }

    if (!confirm('Are you sure you want to cancel your commitment?')) {
      return;
    }

    try {
      await cancelCommitment.mutateAsync({
        eventId,
        signupId: signUpId,
        userId,
      });
    } catch (err) {
      console.error('Failed to cancel commitment:', err);
      alert('Failed to cancel commitment. Please try again.');
    }
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
  const handleCommitToItemAnonymous = async (data: AnonymousCommitmentFormData) => {
    await eventsRepository.commitToSignUpItemAnonymous(
      eventId,
      data.signUpListId,
      data.itemId,
      {
        contactEmail: data.contactEmail,
        quantity: data.quantity,
        notes: data.notes,
        contactName: data.contactName,
        contactPhone: data.contactPhone,
      }
    );
    // Invalidate cache to refresh sign-up lists
    // Note: Since we're using direct repository call, we need to manually trigger a refetch
    window.location.reload();
  };

  // Handle cancel sign-up item commitment (Phase 6A.20)
  const handleCancelSignUp = async (signUpListId: string, itemId: string) => {
    if (!userId) {
      alert('Please log in to cancel sign-ups');
      return;
    }

    if (!confirm('Are you sure you want to cancel your sign-up for this item?')) {
      return;
    }

    try {
      setIsCancelling(true);
      setCancelConfirmId(itemId);

      // Call API with quantity = 0 to signal cancellation
      await commitToSignUpItem.mutateAsync({
        eventId,
        signupId: signUpListId,
        itemId: itemId,
        userId: userId,
        quantity: 0, // Signal full cancellation
        notes: '',
        contactName: '',
        contactEmail: '',
        contactPhone: '',
      });

      setCancelConfirmId(null);
    } catch (error) {
      console.error('Failed to cancel sign-up:', error);
      alert('Failed to cancel sign-up. Please try again.');
      setCancelConfirmId(null);
    } finally {
      setIsCancelling(false);
    }
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
      alert('Failed to delete sign-up list. Please try again.');
    }
  };

  // Get category badge color
  const getCategoryColor = (category: SignUpItemCategory) => {
    switch (category) {
      case SignUpItemCategory.Mandatory:
        return 'bg-red-100 text-red-800 border-red-300';
      case SignUpItemCategory.Preferred:
        return 'bg-blue-100 text-blue-800 border-blue-300';
      case SignUpItemCategory.Suggested:
        return 'bg-green-100 text-green-800 border-green-300';
      case SignUpItemCategory.Open:
        return 'bg-purple-100 text-purple-800 border-purple-300';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-300';
    }
  };

  // Get category label
  const getCategoryLabel = (category: SignUpItemCategory) => {
    switch (category) {
      case SignUpItemCategory.Mandatory:
        return 'Mandatory';
      case SignUpItemCategory.Preferred:
        return 'Preferred';
      case SignUpItemCategory.Suggested:
        return 'Suggested';
      case SignUpItemCategory.Open:
        return 'Open';
      default:
        return 'Unknown';
    }
  };

  // Phase 6A.27: Open item handlers
  const openAddOpenItemModal = (signUpListId: string, signUpListCategory: string) => {
    if (!userId) {
      alert('Please log in to add items');
      return;
    }
    setOpenItemSignUpListId(signUpListId);
    setOpenItemSignUpListCategory(signUpListCategory);
    setEditingOpenItem(null);
    setOpenItemModalOpen(true);
  };

  const openEditOpenItemModal = (signUpListId: string, signUpListCategory: string, item: SignUpItemDto) => {
    if (!userId) {
      alert('Please log in to edit items');
      return;
    }
    setOpenItemSignUpListId(signUpListId);
    setOpenItemSignUpListCategory(signUpListCategory);
    setEditingOpenItem(item);
    setOpenItemModalOpen(true);
  };

  const handleOpenItemSubmit = async (data: OpenItemFormData) => {
    if (!userId) {
      throw new Error('Please log in to submit items');
    }

    if (editingOpenItem) {
      // Update existing Open item
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
      // Add new Open item
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
  const handleCancelOpenItem = async (signUpListId: string, itemId: string) => {
    if (!userId) {
      alert('Please log in to cancel sign-ups');
      return;
    }

    if (!confirm('Are you sure you want to cancel your sign-up for this item?')) {
      return;
    }

    try {
      setIsCancelling(true);
      setCancelConfirmId(itemId);

      await cancelOpenSignUpItem.mutateAsync({
        eventId,
        signupId: signUpListId,
        itemId: itemId,
      });

      setCancelConfirmId(null);
    } catch (error) {
      console.error('Failed to cancel open item sign-up:', error);
      alert('Failed to cancel sign-up. Please try again.');
      setCancelConfirmId(null);
    } finally {
      setIsCancelling(false);
    }
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
          <p>No sign-up lists for this event yet.</p>
          <p className="text-sm mt-2">Create one to let attendees volunteer to bring items!</p>
        </div>
      );
    }

    // For attendee view (event detail page), show Card
    return (
      <div className="py-8">
        <Card>
          <CardHeader>
            <CardTitle>Sign-Up Lists</CardTitle>
            <CardDescription>
              No sign-up lists for this event yet.
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
        Sign-Up Lists (This event has {signUpLists.length} sign-up {signUpLists.length === 1 ? 'list' : 'lists'})
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
              <span className="ml-1 text-xs text-gray-500">
                ({list.commitmentCount})
              </span>
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
                          onClick={() => router.push(`/events/${eventId}/signup-lists/${signUpList.id}`)}
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
                  {/* Group items by category - Phase 6A.28: Preferred is DEPRECATED and hidden from UI */}
                  {[SignUpItemCategory.Mandatory, SignUpItemCategory.Suggested].map((category) => {
                    // For predefined categories, filter out Open items
                    // Phase 6A.28: Also skip Preferred items - they are deprecated
                    const categoryItems = signUpList.items.filter(item =>
                      item.itemCategory === category &&
                      !item.isOpenItem &&
                      item.itemCategory !== SignUpItemCategory.Preferred
                    );

                    if (categoryItems.length === 0) return null;

                    return (
                      <div key={category} className="space-y-3">
                        <h4 className="font-semibold flex items-center gap-2">
                          <span className={`px-2 py-1 rounded text-xs font-medium border ${getCategoryColor(category)}`}>
                            {getCategoryLabel(category)}
                          </span>
                          <span className="text-sm text-muted-foreground">
                            ({categoryItems.length} {categoryItems.length === 1 ? 'item' : 'items'})
                          </span>
                        </h4>

                        <div className="space-y-3">
                          {categoryItems.map((item) => {
                            const userItemCommitment = item.commitments.find(c => c.userId === userId);
                            const remainingQty = item.remainingQuantity;
                            const percentCommitted = Math.round((item.committedQuantity / item.quantity) * 100);

                            return (
                              <div key={item.id} className="border rounded-lg p-4 space-y-2">
                                <div className="flex justify-between items-start">
                                  <div className="flex-1">
                                    <div className="flex items-center gap-2">
                                      <p className="font-medium">{item.itemDescription}</p>
                                      <span className="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded-full font-semibold">
                                        Required: {item.quantity}
                                      </span>
                                    </div>
                                    {item.notes && (
                                      <p className="text-sm text-muted-foreground mt-1">{item.notes}</p>
                                    )}
                                  </div>
                                </div>

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
                                    <span>{item.committedQuantity} of {item.quantity} filled</span>
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
                                          <th className="text-left py-1 px-2 font-medium text-muted-foreground">Quantity</th>
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

                                {/* Sign Up/Update button - Show if remaining qty OR user has commitment */}
                                {(remainingQty > 0 || userItemCommitment) && (
                                  <div className="mt-3 flex gap-2">
                                    <Button
                                      onClick={() => openCommitmentModal(signUpList.id, item, userItemCommitment)}
                                      size="sm"
                                      variant={userItemCommitment ? "default" : "outline"}
                                    >
                                      {userItemCommitment ? 'Update Sign Up' : 'Sign Up'}
                                    </Button>
                                    {/* Cancel button - Show only if user has commitment */}
                                    {userItemCommitment && (
                                      <Button
                                        onClick={() => handleCancelSignUp(signUpList.id, item.id)}
                                        size="sm"
                                        variant="destructive"
                                        disabled={isCancelling && cancelConfirmId === item.id}
                                      >
                                        {isCancelling && cancelConfirmId === item.id ? 'Cancelling...' : 'Cancel Sign Up'}
                                      </Button>
                                    )}
                                  </div>
                                )}

                                {/* Your commitment info */}
                                {userItemCommitment && (
                                  <div className="mt-2 p-2 bg-blue-50 border border-blue-200 rounded">
                                    <p className="text-sm font-medium text-blue-800">
                                      You committed to bring {userItemCommitment.quantity} of this item
                                    </p>
                                  </div>
                                )}

                                {/* All slots filled message */}
                                {remainingQty === 0 && (
                                  <div className="mt-2 p-2 bg-green-50 border border-green-200 rounded">
                                    <p className="text-sm font-medium text-green-800">
                                      ✓ All {item.quantity} slots filled - Thank you everyone!
                                    </p>
                                  </div>
                                )}
                              </div>
                            );
                          })}
                        </div>
                      </div>
                    );
                  })}

                  {/* Phase 6A.27: Open Items Section */}
                  {signUpList.hasOpenItems && (
                    <div className="space-y-3 border-t pt-4 mt-4">
                      <div className="flex items-center gap-2">
                        <h4 className="font-semibold flex items-center gap-2">
                          <span className={`px-2 py-1 rounded text-xs font-medium border ${getCategoryColor(SignUpItemCategory.Open)}`}>
                            Open
                          </span>
                          <span className="text-sm text-muted-foreground">
                            (Bring your own item)
                          </span>
                        </h4>
                      </div>

                      <p className="text-sm text-muted-foreground">
                        You can add your own item to bring to this sign-up list.
                      </p>

                      {/* Display existing Open items */}
                      {openItems.length > 0 ? (
                        <div className="space-y-3">
                          {openItems.map((item) => {
                            const isOwnItem = item.createdByUserId === userId;
                            const commitment = item.commitments?.[0];

                            return (
                              <div key={item.id} className="border rounded-lg p-4 space-y-2 bg-purple-50/50">
                                <div className="flex justify-between items-start">
                                  <div className="flex-1">
                                    <div className="flex items-center gap-2">
                                      <p className="font-medium">{item.itemDescription}</p>
                                      <span className="text-xs bg-purple-100 text-purple-800 px-2 py-1 rounded-full font-semibold">
                                        Qty: {item.quantity}
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
                                        Bringing by: {commitment.contactName || 'Anonymous'}
                                        {commitment.contactEmail && ` (${commitment.contactEmail})`}
                                      </p>
                                    )}
                                  </div>
                                </div>

                                {/* Update/Cancel buttons for own items */}
                                {isOwnItem && (
                                  <div className="mt-3 flex gap-2">
                                    <Button
                                      onClick={() => openEditOpenItemModal(signUpList.id, signUpList.category, item)}
                                      size="sm"
                                      variant="default"
                                    >
                                      Update Sign Up
                                    </Button>
                                    <Button
                                      onClick={() => handleCancelOpenItem(signUpList.id, item.id)}
                                      size="sm"
                                      variant="destructive"
                                      disabled={isCancelling && cancelConfirmId === item.id}
                                    >
                                      {isCancelling && cancelConfirmId === item.id ? 'Cancelling...' : 'Cancel Sign Up'}
                                    </Button>
                                  </div>
                                )}
                              </div>
                            );
                          })}
                        </div>
                      ) : (
                        <p className="text-sm text-muted-foreground italic">
                          No one has signed up with their own item yet. Be the first!
                        </p>
                      )}

                      {/* Sign Up button for authenticated users */}
                      {userId && (
                        <Button
                          onClick={() => openAddOpenItemModal(signUpList.id, signUpList.category)}
                          size="sm"
                          variant="outline"
                        >
                          Sign Up
                        </Button>
                      )}

                      {/* Login prompt for non-authenticated users */}
                      {!userId && (
                        <p className="text-sm text-muted-foreground">
                          Please log in to sign up with your own item.
                        </p>
                      )}
                    </div>
                  )}
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
                        Commitments ({signUpList.commitmentCount}):
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
    </div>
  );
}

export default SignUpManagementSection;
