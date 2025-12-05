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
} from '@/presentation/hooks/useEventSignUps';
import { SignUpType, SignUpItemCategory, SignUpItemDto } from '@/infrastructure/api/types/events.types';
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { SignUpCommitmentModal, CommitmentFormData } from './SignUpCommitmentModal';
import { Plus, Edit, Trash2 } from 'lucide-react';

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

  // Organizer delete confirmation state
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);

  // Fetch sign-up lists
  const { data: signUpLists, isLoading, error } = useEventSignUps(eventId);

  // Mutations
  const commitToSignUp = useCommitToSignUp();
  const cancelCommitment = useCancelCommitment();
  const commitToSignUpItem = useCommitToSignUpItem();
  const removeSignUpListMutation = useRemoveSignUpList();

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
    if (!userId) {
      throw new Error('Please log in to commit to items');
    }

    await commitToSignUpItem.mutateAsync({
      eventId,
      signupId: data.signUpListId,
      itemId: data.itemId,
      userId,
      quantity: data.quantity,
      notes: data.notes,
      contactName: data.contactName,
      contactEmail: data.contactEmail,
      contactPhone: data.contactPhone,
    });
  };

  // Open commitment modal
  const openCommitmentModal = (signUpListId: string, item: SignUpItemDto) => {
    if (!userId) {
      alert('Please log in to commit to items');
      return;
    }
    setSelectedSignUpListId(signUpListId);
    setSelectedItem(item);
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
      default:
        return 'Unknown';
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
    return (
      <div className="py-8">
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle>Sign-Up Lists</CardTitle>
                <CardDescription>
                  No sign-up lists for this event yet.
                  {isOrganizer && ' Create one to let attendees volunteer to bring items!'}
                </CardDescription>
              </div>
              {isOrganizer && (
                <Button
                  onClick={() => router.push(`/events/${eventId}/manage-signups`)}
                  className="bg-orange-600 hover:bg-orange-700 text-white"
                >
                  <Plus className="h-4 w-4 mr-2" />
                  Create Sign-Up List
                </Button>
              )}
            </div>
          </CardHeader>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold">Sign-Up Lists</h2>
        {isOrganizer && (
          <Button
            onClick={() => router.push(`/events/${eventId}/manage-signups`)}
            className="bg-orange-600 hover:bg-orange-700 text-white"
          >
            <Plus className="h-4 w-4 mr-2" />
            Create Sign-Up List
          </Button>
        )}
      </div>

      {signUpLists.map((signUpList) => {
        // Check if current user has committed to this list
        const userCommitment = signUpList.commitments.find((c) => c.userId === userId);

        // Check if this is a category-based sign-up (has items)
        const isCategoryBased = signUpList.items && signUpList.items.length > 0;

        return (
          <Card key={signUpList.id}>
            <CardHeader>
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <CardTitle>{signUpList.category}</CardTitle>
                  <CardDescription>{signUpList.description}</CardDescription>
                  <div className="text-sm text-muted-foreground">
                    {isCategoryBased ? (
                      <div className="flex gap-2 flex-wrap mt-2">
                        {signUpList.hasMandatoryItems && (
                          <span className="px-2 py-1 rounded text-xs font-medium bg-red-100 text-red-800">
                            Has Mandatory Items
                          </span>
                        )}
                        {signUpList.hasPreferredItems && (
                          <span className="px-2 py-1 rounded text-xs font-medium bg-blue-100 text-blue-800">
                            Has Preferred Items
                          </span>
                        )}
                        {signUpList.hasSuggestedItems && (
                          <span className="px-2 py-1 rounded text-xs font-medium bg-green-100 text-green-800">
                            Has Suggested Items
                          </span>
                        )}
                      </div>
                    ) : (
                      <span>Type: {signUpList.signUpType === SignUpType.Predefined ? 'Predefined Items' : 'Open'}</span>
                    )}
                  </div>
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
                          onClick={() => router.push(`/events/${eventId}/manage-signups/${signUpList.id}`)}
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
                  {/* Group items by category */}
                  {[SignUpItemCategory.Mandatory, SignUpItemCategory.Preferred, SignUpItemCategory.Suggested].map((category) => {
                    const categoryItems = signUpList.items.filter(item => item.itemCategory === category);

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
                                    <p className="font-medium">{item.itemDescription}</p>
                                    {item.notes && (
                                      <p className="text-sm text-muted-foreground mt-1">{item.notes}</p>
                                    )}
                                  </div>
                                  <div className="text-right ml-4">
                                    <p className={`text-sm font-medium ${remainingQty === 0 ? 'text-green-600' : 'text-blue-600'}`}>
                                      {item.committedQuantity} of {item.quantity} committed
                                    </p>
                                    <p className="text-xs text-muted-foreground">
                                      {remainingQty} remaining
                                    </p>
                                  </div>
                                </div>

                                {/* Progress bar */}
                                <div className="w-full bg-gray-200 rounded-full h-2">
                                  <div
                                    className={`h-2 rounded-full ${
                                      percentCommitted === 100 ? 'bg-green-500' : 'bg-blue-500'
                                    }`}
                                    style={{ width: `${percentCommitted}%` }}
                                  />
                                </div>

                                {/* Commitments */}
                                {item.commitments.length > 0 && (
                                  <div className="space-y-1 mt-2">
                                    <p className="text-xs font-medium text-muted-foreground">Commitments:</p>
                                    {item.commitments.map((commitment) => (
                                      <div key={commitment.id} className="text-sm bg-muted p-2 rounded">
                                        <div className="flex justify-between items-center">
                                          <span>Quantity: {commitment.quantity}</span>
                                          {commitment.userId === userId && (
                                            <span className="text-xs text-blue-600 font-medium">(You)</span>
                                          )}
                                        </div>
                                        {commitment.notes && (
                                          <p className="text-xs text-muted-foreground mt-1">Note: {commitment.notes}</p>
                                        )}
                                      </div>
                                    ))}
                                  </div>
                                )}

                                {/* Commit button - Opens modal */}
                                {!userItemCommitment && userId && remainingQty > 0 && (
                                  <div className="mt-3">
                                    <Button
                                      onClick={() => openCommitmentModal(signUpList.id, item)}
                                      size="sm"
                                      variant="outline"
                                      className="w-full sm:w-auto"
                                    >
                                      I can bring this
                                    </Button>
                                  </div>
                                )}

                                {!userId && remainingQty > 0 && (
                                  <p className="text-xs text-muted-foreground mt-2">
                                    Please log in to commit to this item
                                  </p>
                                )}

                                {userItemCommitment && (
                                  <div className="mt-2 p-2 bg-blue-50 border border-blue-200 rounded">
                                    <p className="text-sm font-medium text-blue-800">
                                      You committed to bring {userItemCommitment.quantity} of this item
                                    </p>
                                  </div>
                                )}

                                {remainingQty === 0 && !userItemCommitment && (
                                  <div className="mt-2 p-2 bg-green-50 border border-green-200 rounded">
                                    <p className="text-sm font-medium text-green-800">
                                      ✓ Fully committed - Thank you everyone!
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
        onCommit={handleCommitToItem}
        isSubmitting={commitToSignUpItem.isPending}
      />
    </div>
  );
}

export default SignUpManagementSection;
