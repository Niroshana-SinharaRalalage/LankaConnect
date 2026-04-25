'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { LankaEventsHeader } from '@/presentation/components/layout/LankaEventsHeader';
import Footer from '@/presentation/components/layout/Footer';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useEventById } from '@/presentation/hooks/useEvents';
import {
  useEventSignUps,
  useUpdateSignUpList,
  useAddSignUpItem,
  useUpdateSignUpItem,
  useRemoveSignUpItem,
} from '@/presentation/hooks/useEventSignUps';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { Plus, Trash2, ArrowLeft, Save, Edit2, X, Check } from 'lucide-react';
import {
  SignUpItemCategory,
  SignUpItemType,
  SignUpKind,
  isQuantityBased,
} from '@/infrastructure/api/types/events.types';
import { UserRole } from '@/infrastructure/api/types/auth.types';

/**
 * Phase 7D.1 step 25: Edit Volunteer List Page.
 *
 * Slot-based-only analog of the sign-up list editor. Organizers can rename the
 * list, update its description, and CRUD volunteer roles (each role is one
 * slot-based item). Volunteer lists enforce `hasOpenItems = false` and reject
 * quantity items at the domain layer, so this page never exposes those toggles.
 */
export default function EditVolunteerListPage() {
  const params = useParams();
  const router = useRouter();
  const eventId = params.id as string;
  const signupId = params.signupId as string;
  const { user, isAuthenticated } = useAuthStore();

  const { data: event, isLoading: eventLoading } = useEventById(eventId);
  const { data: signUpLists, isLoading: signUpsLoading } = useEventSignUps(
    eventId,
    SignUpKind.Volunteers
  );

  const signUpList = signUpLists?.find((list) => list.id === signupId);

  const updateSignUpListMutation = useUpdateSignUpList(eventId);
  const addSignUpItemMutation = useAddSignUpItem();
  const updateSignUpItemMutation = useUpdateSignUpItem();
  const removeSignUpItemMutation = useRemoveSignUpItem();

  const [category, setCategory] = useState('');
  const [description, setDescription] = useState('');
  const [originalCategory, setOriginalCategory] = useState('');
  const [originalDescription, setOriginalDescription] = useState('');
  const [submitError, setSubmitError] = useState<string | null>(null);

  const [editingItemId, setEditingItemId] = useState<string | null>(null);
  const [editingItemDesc, setEditingItemDesc] = useState('');
  const [editingItemSlots, setEditingItemSlots] = useState(1);
  const [editingItemNotes, setEditingItemNotes] = useState('');

  const [newRoleName, setNewRoleName] = useState('');
  const [newRoleSlots, setNewRoleSlots] = useState(1);
  const [newRoleNotes, setNewRoleNotes] = useState('');

  useEffect(() => {
    if (!isAuthenticated || !user?.userId) {
      router.push(
        '/login?redirect=' +
          encodeURIComponent(`/events/${eventId}/volunteer-lists/${signupId}`)
      );
      return;
    }

    const isAuthorized =
      event &&
      (event.isCurrentUserOrganizer === true ||
        user.role === UserRole.Admin ||
        user.role === UserRole.AdminManager);

    if (event && !isAuthorized) {
      router.push(`/events/${eventId}`);
    }
  }, [isAuthenticated, user, event, eventId, signupId, router]);

  useEffect(() => {
    if (signUpList) {
      setCategory(signUpList.category);
      setDescription(signUpList.description);
      setOriginalCategory(signUpList.category);
      setOriginalDescription(signUpList.description);
    }
  }, [signUpList]);

  const isDirty = category !== originalCategory || description !== originalDescription;

  const handleSaveListDetails = async () => {
    if (!category.trim()) {
      setSubmitError('Volunteer List Name is required');
      return;
    }
    if (!description.trim()) {
      setSubmitError('Description is required');
      return;
    }

    try {
      setSubmitError(null);
      await updateSignUpListMutation.mutateAsync({
        signupId,
        category: category.trim(),
        description: description.trim(),
        hasMandatoryItems: true,
        hasPreferredItems: false,
        hasSuggestedItems: false,
        hasOpenItems: false,
      });
      setOriginalCategory(category.trim());
      setOriginalDescription(description.trim());
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : 'Failed to save volunteer list');
    }
  };

  const handleAddRole = async () => {
    if (!newRoleName.trim()) {
      setSubmitError('Role name is required');
      return;
    }
    if (newRoleSlots < 1) {
      setSubmitError('At least 1 volunteer slot is required');
      return;
    }

    try {
      setSubmitError(null);
      await addSignUpItemMutation.mutateAsync({
        eventId,
        signupId,
        itemDescription: newRoleName.trim(),
        itemType: SignUpItemType.Slot,
        itemCategory: SignUpItemCategory.Mandatory,
        targetQuantity: null,
        availableSlots: newRoleSlots,
        suggestedPerSlot: null,
        notes: newRoleNotes.trim() || null,
      });
      setNewRoleName('');
      setNewRoleSlots(1);
      setNewRoleNotes('');
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : 'Failed to add volunteer role');
    }
  };

  const handleStartEditRole = (itemId: string) => {
    const item = signUpList?.items?.find((i) => i.id === itemId);
    if (!item || isQuantityBased(item)) return;
    setEditingItemId(itemId);
    setEditingItemDesc(item.itemDescription);
    setEditingItemSlots(item.totalSlots);
    setEditingItemNotes(item.notes || '');
  };

  const handleCancelEditRole = () => {
    setEditingItemId(null);
    setEditingItemDesc('');
    setEditingItemSlots(1);
    setEditingItemNotes('');
  };

  const handleSaveRole = async () => {
    if (!editingItemId) return;
    if (!editingItemDesc.trim()) {
      setSubmitError('Role name is required');
      return;
    }
    if (editingItemSlots < 1) {
      setSubmitError('At least 1 volunteer slot is required');
      return;
    }

    try {
      setSubmitError(null);
      await updateSignUpItemMutation.mutateAsync({
        eventId,
        signupId,
        itemId: editingItemId,
        itemDescription: editingItemDesc.trim(),
        targetQuantity: null,
        availableSlots: editingItemSlots,
        suggestedPerSlot: null,
        notes: editingItemNotes.trim() || null,
      });
      handleCancelEditRole();
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : 'Failed to update role');
    }
  };

  const handleRemoveRole = async (itemId: string) => {
    if (!confirm('Remove this volunteer role? Existing commitments for this role will be unlinked.')) return;

    try {
      setSubmitError(null);
      await removeSignUpItemMutation.mutateAsync({ eventId, signupId, itemId });
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : 'Failed to remove role');
    }
  };

  if (!isAuthenticated || !user?.userId) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <LankaEventsHeader />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 text-center">
          <p className="text-neutral-500">Redirecting to login...</p>
        </div>
        <Footer />
      </div>
    );
  }

  if (eventLoading || signUpsLoading || !event) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <LankaEventsHeader />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 text-center">
          <p className="text-neutral-500">Loading volunteer list...</p>
        </div>
        <Footer />
      </div>
    );
  }

  if (event.isCurrentUserOrganizer !== true) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <LankaEventsHeader />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 text-center">
          <p className="text-destructive">You are not authorized to manage this event</p>
        </div>
        <Footer />
      </div>
    );
  }

  if (!signUpList) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <LankaEventsHeader />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 text-center">
          <p className="text-destructive">Volunteer list not found</p>
          <Button
            className="mt-4"
            variant="outline"
            onClick={() => router.push(`/events/${eventId}/manage?tab=volunteers`)}
          >
            Back to Volunteers
          </Button>
        </div>
        <Footer />
      </div>
    );
  }

  const roles = (signUpList.items ?? []).filter((item) => !isQuantityBased(item));

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <LankaEventsHeader />

      <div className="bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 py-8 relative overflow-hidden">
        <div className="absolute inset-0 opacity-10">
          <div
            className="absolute inset-0"
            style={{
              backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
            }}
          ></div>
        </div>

        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <Button
            variant="outline"
            onClick={() => router.push(`/events/${eventId}/manage?tab=volunteers`)}
            className="mb-4 bg-white/10 text-white border-white/30 hover:bg-white/20 hover:border-white/50"
          >
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Manage Event
          </Button>

          <h1 className="text-3xl font-bold text-white mb-2">Edit Volunteer List</h1>
          <p className="text-lg text-white/90">{event.title}</p>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-6">
        <Card>
          <CardHeader>
            <CardTitle style={{ color: '#8B1538' }}>List Details</CardTitle>
            <CardDescription>Rename the list or update its description</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-neutral-700 mb-2">
                Volunteer List Name *
              </label>
              <Input
                type="text"
                value={category}
                onChange={(e) => setCategory(e.target.value)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-neutral-700 mb-2">
                Description *
              </label>
              <textarea
                rows={3}
                className="w-full px-4 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 resize-none"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
            </div>
            <div className="flex items-center justify-end gap-3 pt-2">
              <Button
                variant="outline"
                onClick={() => {
                  setCategory(originalCategory);
                  setDescription(originalDescription);
                }}
                disabled={!isDirty}
              >
                Revert
              </Button>
              <Button
                onClick={handleSaveListDetails}
                disabled={!isDirty || updateSignUpListMutation.isPending}
                style={{ background: '#FF7900' }}
              >
                <Save className="h-4 w-4 mr-2" />
                {updateSignUpListMutation.isPending ? 'Saving...' : 'Save Details'}
              </Button>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle style={{ color: '#8B1538' }}>Volunteer Roles</CardTitle>
            <CardDescription>
              Each role lists how many volunteers you need. Existing sign-ups stay intact when you
              rename a role.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            {roles.length === 0 ? (
              <div className="p-4 text-center text-neutral-500 text-sm border rounded-lg">
                No volunteer roles yet — add one below.
              </div>
            ) : (
              <div className="border rounded-lg overflow-hidden">
                <table className="w-full">
                  <thead className="bg-neutral-100">
                    <tr>
                      <th className="px-3 py-2 text-left text-xs font-medium text-neutral-600">
                        Role
                      </th>
                      <th className="px-3 py-2 text-left text-xs font-medium text-neutral-600">
                        Volunteers Needed
                      </th>
                      <th className="px-3 py-2 text-left text-xs font-medium text-neutral-600">
                        Filled
                      </th>
                      <th className="px-3 py-2 text-center text-xs font-medium text-neutral-600">
                        Actions
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {roles.map((role) => {
                      const totalSlots = isQuantityBased(role) ? 0 : role.totalSlots;
                      const filledSlots = isQuantityBased(role) ? 0 : role.filledSlots;
                      const isEditing = editingItemId === role.id;

                      return (
                        <tr key={role.id} className="border-t align-top">
                          <td className="px-3 py-2 text-sm">
                            {isEditing ? (
                              <div className="space-y-2">
                                <Input
                                  type="text"
                                  value={editingItemDesc}
                                  onChange={(e) => setEditingItemDesc(e.target.value)}
                                />
                                <textarea
                                  rows={2}
                                  className="w-full px-2 py-1 border border-neutral-300 rounded text-xs focus:outline-none focus:ring-1 focus:ring-orange-500"
                                  placeholder="Notes"
                                  value={editingItemNotes}
                                  onChange={(e) => setEditingItemNotes(e.target.value)}
                                />
                              </div>
                            ) : (
                              <div>
                                <p className="font-medium">{role.itemDescription}</p>
                                {role.notes && (
                                  <p className="text-xs text-neutral-500">{role.notes}</p>
                                )}
                              </div>
                            )}
                          </td>
                          <td className="px-3 py-2 text-sm">
                            {isEditing ? (
                              <Input
                                type="number"
                                min="1"
                                max="500"
                                value={editingItemSlots}
                                onChange={(e) =>
                                  setEditingItemSlots(parseInt(e.target.value) || 1)
                                }
                              />
                            ) : (
                              totalSlots
                            )}
                          </td>
                          <td className="px-3 py-2 text-sm">
                            {filledSlots} / {totalSlots}
                          </td>
                          <td className="px-3 py-2 text-center">
                            {isEditing ? (
                              <div className="flex gap-2 justify-center">
                                <Button
                                  size="sm"
                                  onClick={handleSaveRole}
                                  disabled={updateSignUpItemMutation.isPending}
                                  style={{ background: '#FF7900' }}
                                >
                                  <Check className="h-4 w-4" />
                                </Button>
                                <Button size="sm" variant="outline" onClick={handleCancelEditRole}>
                                  <X className="h-4 w-4" />
                                </Button>
                              </div>
                            ) : (
                              <div className="flex gap-2 justify-center">
                                <Button
                                  size="sm"
                                  variant="outline"
                                  onClick={() => handleStartEditRole(role.id)}
                                >
                                  <Edit2 className="h-4 w-4" />
                                </Button>
                                <Button
                                  size="sm"
                                  variant="outline"
                                  onClick={() => handleRemoveRole(role.id)}
                                  disabled={removeSignUpItemMutation.isPending}
                                >
                                  <Trash2 className="h-4 w-4 text-red-600" />
                                </Button>
                              </div>
                            )}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}

            <div className="border rounded-lg p-4 space-y-3 bg-orange-50/30">
              <h4 className="font-medium text-neutral-800">Add New Role</h4>
              <div>
                <label className="block text-xs font-medium text-neutral-600 mb-1">
                  Role Name *
                </label>
                <Input
                  type="text"
                  placeholder="e.g., Food Committee"
                  value={newRoleName}
                  onChange={(e) => setNewRoleName(e.target.value)}
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-neutral-600 mb-1">
                  Volunteers Needed *
                </label>
                <Input
                  type="number"
                  min="1"
                  max="500"
                  value={newRoleSlots}
                  onChange={(e) => setNewRoleSlots(parseInt(e.target.value) || 1)}
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-neutral-600 mb-1">
                  Notes (optional)
                </label>
                <textarea
                  rows={2}
                  className="w-full px-3 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 resize-none"
                  value={newRoleNotes}
                  onChange={(e) => setNewRoleNotes(e.target.value)}
                />
              </div>
              <Button
                onClick={handleAddRole}
                disabled={addSignUpItemMutation.isPending}
                variant="outline"
                className="w-full"
              >
                <Plus className="h-4 w-4 mr-2" />
                {addSignUpItemMutation.isPending ? 'Adding...' : 'Add Role'}
              </Button>
            </div>

            {submitError && (
              <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
                <p className="text-sm text-red-600">{submitError}</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      <Footer />
    </div>
  );
}
