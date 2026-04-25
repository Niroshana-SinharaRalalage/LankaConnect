'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { LankaEventsHeader } from '@/presentation/components/layout/LankaEventsHeader';
import Footer from '@/presentation/components/layout/Footer';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useEventById } from '@/presentation/hooks/useEvents';
import { useCreateSignUpList } from '@/presentation/hooks/useEventSignUps';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { Plus, Trash2, ArrowLeft } from 'lucide-react';
import {
  SignUpItemCategory,
  SignUpItemType,
  SignUpKind,
} from '@/infrastructure/api/types/events.types';
import { UserRole } from '@/infrastructure/api/types/auth.types';

/**
 * Phase 7D.1 step 24: Create Volunteer List page.
 *
 * Simplified create form — volunteer lists are slot-based only (1 volunteer =
 * 1 slot) and Organizer-defined only (hasOpenItems is always false, matching
 * the domain-level constraint in SignUpList.CreateVolunteerList).
 *
 * Mirrors create-signup-list/page.tsx auth/redirect/layout patterns but drops
 * the quantity/suggested/open toggles that do not apply to volunteer roles.
 */
export default function CreateVolunteerListPage() {
  const params = useParams();
  const router = useRouter();
  const eventId = params.id as string;
  const { user, isAuthenticated } = useAuthStore();

  const { data: event, isLoading: eventLoading } = useEventById(eventId);
  const createSignUpListMutation = useCreateSignUpList();

  const [category, setCategory] = useState('');
  const [description, setDescription] = useState('');
  const [submitError, setSubmitError] = useState<string | null>(null);

  type VolunteerRoleEntry = {
    roleName: string;
    volunteersNeeded: number;
    notes: string;
  };
  const [roles, setRoles] = useState<VolunteerRoleEntry[]>([]);
  const [newRoleName, setNewRoleName] = useState('');
  const [newRoleSlots, setNewRoleSlots] = useState(1);
  const [newRoleNotes, setNewRoleNotes] = useState('');

  useEffect(() => {
    if (!isAuthenticated || !user?.userId) {
      router.push(
        '/login?redirect=' +
          encodeURIComponent(`/events/${eventId}/manage/create-volunteer-list`)
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
  }, [isAuthenticated, user, event, eventId, router]);

  const handleAddRole = () => {
    if (!newRoleName.trim()) {
      setSubmitError('Role name is required');
      return;
    }
    if (newRoleSlots < 1) {
      setSubmitError('At least 1 volunteer slot is required');
      return;
    }
    setRoles([
      ...roles,
      {
        roleName: newRoleName.trim(),
        volunteersNeeded: newRoleSlots,
        notes: newRoleNotes.trim(),
      },
    ]);
    setNewRoleName('');
    setNewRoleSlots(1);
    setNewRoleNotes('');
    setSubmitError(null);
  };

  const handleRemoveRole = (index: number) => {
    setRoles(roles.filter((_, i) => i !== index));
  };

  const handleCreate = async () => {
    if (!category.trim()) {
      setSubmitError('Volunteer List Name is required');
      return;
    }
    if (!description.trim()) {
      setSubmitError('Description is required');
      return;
    }
    if (roles.length === 0) {
      setSubmitError('Please add at least one volunteer role');
      return;
    }

    try {
      setSubmitError(null);

      const items = roles.map((role) => ({
        itemDescription: role.roleName,
        itemType: SignUpItemType.Slot,
        itemCategory: SignUpItemCategory.Mandatory,
        targetQuantity: null,
        availableSlots: role.volunteersNeeded,
        suggestedPerSlot: null,
        notes: role.notes || null,
      }));

      await createSignUpListMutation.mutateAsync({
        eventId,
        category: category.trim(),
        description: description.trim(),
        kind: SignUpKind.Volunteers,
        hasMandatoryItems: true,
        hasPreferredItems: false,
        hasSuggestedItems: false,
        hasOpenItems: false,
        items,
      });

      router.push(`/events/${eventId}/manage?tab=volunteers`);
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : 'Failed to create volunteer list');
    }
  };

  if (!isAuthenticated || !user?.userId) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <LankaEventsHeader />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <div className="text-center">
            <p className="text-neutral-500">Redirecting to login...</p>
          </div>
        </div>
        <Footer />
      </div>
    );
  }

  if (eventLoading || !event) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <LankaEventsHeader />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <div className="text-center">
            <p className="text-neutral-500">Loading event...</p>
          </div>
        </div>
        <Footer />
      </div>
    );
  }

  if (event.isCurrentUserOrganizer !== true) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <LankaEventsHeader />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <div className="text-center">
            <p className="text-destructive">You are not authorized to manage this event</p>
          </div>
        </div>
        <Footer />
      </div>
    );
  }

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

          <h1 className="text-3xl font-bold text-white mb-2">Create Volunteer List</h1>
          <p className="text-lg text-white/90">{event.title}</p>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <Card className="mb-6">
          <CardHeader>
            <CardTitle style={{ color: '#8B1538' }}>New Volunteer List</CardTitle>
            <CardDescription>
              Add a group of volunteer roles attendees can sign up for (e.g. Food Committee: 5
              volunteers, Decorations: 3 volunteers)
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label
                htmlFor="category"
                className="block text-sm font-medium text-neutral-700 mb-2"
              >
                Volunteer List Name *
              </label>
              <Input
                id="category"
                type="text"
                placeholder="e.g., Event Day Volunteers, Setup Crew, Food Committee"
                value={category}
                onChange={(e) => setCategory(e.target.value)}
              />
              <p className="text-xs text-neutral-500 mt-1">
                A short name that groups the roles below
              </p>
            </div>

            <div>
              <label
                htmlFor="description"
                className="block text-sm font-medium text-neutral-700 mb-2"
              >
                Description *
              </label>
              <textarea
                id="description"
                rows={3}
                placeholder="Describe what these volunteers will do and any special requirements..."
                className="w-full px-4 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 resize-none"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
            </div>

            <div className="space-y-3">
              <label className="block text-sm font-medium text-neutral-700">
                Volunteer Roles * (at least one required)
              </label>

              <div className="border rounded-lg p-4 space-y-4 bg-orange-50/30">
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                  <div className="space-y-3">
                    <div>
                      <label className="block text-xs font-medium text-neutral-600 mb-1">
                        Role Name *
                      </label>
                      <Input
                        type="text"
                        placeholder="e.g., Food Committee, Decoration Team"
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
                      <p className="text-xs text-neutral-500 mt-1">
                        Number of volunteer slots to fill for this role
                      </p>
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-neutral-600 mb-1">
                        Notes (optional)
                      </label>
                      <textarea
                        rows={2}
                        placeholder="Any extra details — shift time, skills, contact..."
                        className="w-full px-3 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 resize-none"
                        value={newRoleNotes}
                        onChange={(e) => setNewRoleNotes(e.target.value)}
                      />
                    </div>
                    <Button
                      type="button"
                      onClick={handleAddRole}
                      variant="outline"
                      className="w-full"
                    >
                      <Plus className="h-4 w-4 mr-2" />
                      Add Role
                    </Button>
                  </div>

                  <div className="border rounded-lg overflow-hidden max-h-96 overflow-y-auto bg-white">
                    {roles.length === 0 ? (
                      <div className="p-4 text-center text-neutral-500 text-sm">
                        No roles added yet
                      </div>
                    ) : (
                      <table className="w-full">
                        <thead className="bg-neutral-100 sticky top-0">
                          <tr>
                            <th className="px-3 py-2 text-left text-xs font-medium text-neutral-600">
                              Role
                            </th>
                            <th className="px-3 py-2 text-left text-xs font-medium text-neutral-600">
                              Slots
                            </th>
                            <th className="px-3 py-2 text-center text-xs font-medium text-neutral-600">
                              Action
                            </th>
                          </tr>
                        </thead>
                        <tbody>
                          {roles.map((role, index) => (
                            <tr key={index} className="border-t">
                              <td className="px-3 py-2 text-sm">
                                <div>
                                  <p className="font-medium">{role.roleName}</p>
                                  {role.notes && (
                                    <p className="text-xs text-neutral-500">{role.notes}</p>
                                  )}
                                </div>
                              </td>
                              <td className="px-3 py-2 text-sm">
                                {role.volunteersNeeded} slot
                                {role.volunteersNeeded > 1 ? 's' : ''}
                              </td>
                              <td className="px-3 py-2 text-center">
                                <Button
                                  variant="outline"
                                  size="sm"
                                  onClick={() => handleRemoveRole(index)}
                                >
                                  <Trash2 className="h-4 w-4" />
                                </Button>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    )}
                  </div>
                </div>
              </div>
            </div>

            {submitError && (
              <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
                <p className="text-sm text-red-600">{submitError}</p>
              </div>
            )}

            <div className="flex items-center justify-end gap-3 pt-4">
              <Button
                variant="outline"
                onClick={() => router.push(`/events/${eventId}/manage?tab=volunteers`)}
              >
                Cancel
              </Button>
              <Button
                onClick={handleCreate}
                disabled={createSignUpListMutation.isPending}
                style={{ background: '#FF7900' }}
              >
                {createSignUpListMutation.isPending ? 'Creating...' : 'Create Volunteer List'}
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>

      <Footer />
    </div>
  );
}
