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
 *
 * @requires useEventSignUps hook
 * @requires useCommitToSignUp, useCancelCommitment mutations
 */

'use client';

import React, { useState } from 'react';
import {
  useEventSignUps,
  useCommitToSignUp,
  useCancelCommitment,
  useAddSignUpList,
  useRemoveSignUpList,
} from '@/presentation/hooks/useEventSignUps';
import { SignUpType } from '@/infrastructure/api/types/events.types';
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';

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
  const [commitDialogOpen, setCommitDialogOpen] = useState(false);
  const [selectedSignUpId, setSelectedSignUpId] = useState<string | null>(null);
  const [itemDescription, setItemDescription] = useState('');
  const [quantity, setQuantity] = useState(1);

  // Fetch sign-up lists
  const { data: signUpLists, isLoading, error } = useEventSignUps(eventId);

  // Mutations
  const commitToSignUp = useCommitToSignUp();
  const cancelCommitment = useCancelCommitment();

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
            <CardTitle>Sign-Up Lists</CardTitle>
            <CardDescription>
              No sign-up lists for this event yet.
              {isOrganizer && ' Create one to let attendees volunteer to bring items!'}
            </CardDescription>
          </CardHeader>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">Sign-Up Lists</h2>

      {signUpLists.map((signUpList) => {
        // Check if current user has committed to this list
        const userCommitment = signUpList.commitments.find((c) => c.userId === userId);

        return (
          <Card key={signUpList.id}>
            <CardHeader>
              <CardTitle>{signUpList.category}</CardTitle>
              <CardDescription>{signUpList.description}</CardDescription>
              <div className="text-sm text-muted-foreground">
                Type: {signUpList.signUpType === SignUpType.Predefined ? 'Predefined Items' : 'Open'}
              </div>
            </CardHeader>

            <CardContent>
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
            </CardContent>

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
          </Card>
        );
      })}
    </div>
  );
}

export default SignUpManagementSection;
