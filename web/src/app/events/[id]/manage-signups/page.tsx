'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useEventById } from '@/presentation/hooks/useEvents';
import {
  useEventSignUps,
  useAddSignUpList,
  useRemoveSignUpList,
} from '@/presentation/hooks/useEventSignUps';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { Plus, Trash2, Download, ArrowLeft, ListPlus, Users } from 'lucide-react';
import { SignUpType, type AddSignUpListRequest } from '@/infrastructure/api/types/events.types';
import { UserRole } from '@/infrastructure/api/types/auth.types';

/**
 * Manage Sign-Up Lists Page
 * Allows event organizers to create, view, and manage sign-up lists for their events
 *
 * Features:
 * - Create new sign-up lists (Open or Predefined type)
 * - View all existing sign-up lists with commitments
 * - Delete sign-up lists
 * - Download commitments as CSV
 * - Authentication guard (organizer-only)
 */
export default function ManageSignUpsPage() {
  const params = useParams();
  const router = useRouter();
  const eventId = params.id as string;
  const { user, isAuthenticated } = useAuthStore();

  // Fetch event details
  const { data: event, isLoading: eventLoading } = useEventById(eventId);

  // Fetch sign-up lists
  const { data: signUpLists, isLoading: signUpsLoading } = useEventSignUps(eventId);

  // Mutations
  const addSignUpListMutation = useAddSignUpList();
  const removeSignUpListMutation = useRemoveSignUpList();

  // Form state
  const [showForm, setShowForm] = useState(false);
  const [category, setCategory] = useState('');
  const [description, setDescription] = useState('');
  const [signUpType, setSignUpType] = useState<SignUpType>(SignUpType.Open);
  const [predefinedItems, setPredefinedItems] = useState<string[]>([]);
  const [newItem, setNewItem] = useState('');
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);

  // Redirect if not authenticated or not authorized
  useEffect(() => {
    if (!isAuthenticated || !user?.userId) {
      router.push('/login?redirect=' + encodeURIComponent(`/events/${eventId}/manage-signups`));
      return;
    }

    // Check if user is organizer or admin
    const isAuthorized = event && (
      event.organizerId === user.userId ||
      user.role === UserRole.Admin ||
      user.role === UserRole.AdminManager
    );

    if (event && !isAuthorized) {
      // Redirect unauthorized users to event detail page
      router.push(`/events/${eventId}`);
    }
  }, [isAuthenticated, user, event, eventId, router]);

  // Handle add predefined item
  const handleAddPredefinedItem = () => {
    if (newItem.trim()) {
      setPredefinedItems([...predefinedItems, newItem.trim()]);
      setNewItem('');
    }
  };

  // Handle remove predefined item
  const handleRemovePredefinedItem = (index: number) => {
    setPredefinedItems(predefinedItems.filter((_, i) => i !== index));
  };

  // Handle create sign-up list
  const handleCreateSignUpList = async () => {
    if (!category.trim()) {
      setSubmitError('Category is required');
      return;
    }

    if (!description.trim()) {
      setSubmitError('Description is required');
      return;
    }

    if (signUpType === SignUpType.Predefined && predefinedItems.length === 0) {
      setSubmitError('Please add at least one predefined item');
      return;
    }

    try {
      setSubmitError(null);

      const request: AddSignUpListRequest = {
        category: category.trim(),
        description: description.trim(),
        signUpType,
        predefinedItems: signUpType === SignUpType.Predefined ? predefinedItems : undefined,
      };

      await addSignUpListMutation.mutateAsync({ eventId, ...request });

      // Reset form
      setCategory('');
      setDescription('');
      setSignUpType(SignUpType.Open);
      setPredefinedItems([]);
      setShowForm(false);
    } catch (err) {
      console.error('Failed to create sign-up list:', err);
      setSubmitError(err instanceof Error ? err.message : 'Failed to create sign-up list');
    }
  };

  // Handle delete sign-up list
  const handleDeleteSignUpList = async (signupId: string) => {
    try {
      await removeSignUpListMutation.mutateAsync({ eventId, signupId });
      setDeleteConfirmId(null);
    } catch (err) {
      console.error('Failed to delete sign-up list:', err);
      alert('Failed to delete sign-up list. Please try again.');
    }
  };

  // Handle download commitments as CSV
  const handleDownloadCSV = () => {
    if (!signUpLists || signUpLists.length === 0) {
      alert('No sign-up lists to download');
      return;
    }

    // Build CSV content
    let csvContent = 'Category,Item Description,User ID,Quantity,Committed At\n';

    signUpLists.forEach((list) => {
      list.commitments.forEach((commitment) => {
        csvContent += `"${list.category}","${commitment.itemDescription}","${commitment.userId}",${commitment.quantity},"${commitment.committedAt}"\n`;
      });
    });

    // Create download link
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);

    link.setAttribute('href', url);
    link.setAttribute('download', `event-${eventId}-signups.csv`);
    link.style.visibility = 'hidden';

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  // Don't render until authentication is confirmed
  if (!isAuthenticated || !user?.userId) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <Header />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <div className="text-center">
            <p className="text-neutral-500">Redirecting to login...</p>
          </div>
        </div>
        <Footer />
      </div>
    );
  }

  // Don't render until event is loaded
  if (eventLoading || !event) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <Header />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <div className="text-center">
            <p className="text-neutral-500">Loading event...</p>
          </div>
        </div>
        <Footer />
      </div>
    );
  }

  // Verify user is organizer
  if (event.organizerId !== user.userId) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <Header />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <div className="text-center">
            <p className="text-destructive">You are not authorized to manage this event</p>
          </div>
        </div>
        <Footer />
      </div>
    );
  }

  const totalCommitments = signUpLists?.reduce((sum, list) => sum + list.commitmentCount, 0) || 0;

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      {/* Page Header */}
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
            onClick={() => router.push(`/events/${eventId}/manage`)}
            className="mb-4 bg-white/10 text-white border-white/30 hover:bg-white/20 hover:border-white/50"
          >
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Manage Event
          </Button>

          <h1 className="text-3xl font-bold text-white mb-2">
            Manage Sign-Up Lists
          </h1>
          <p className="text-lg text-white/90">
            {event.title}
          </p>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Stats */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-neutral-500">Total Sign-Up Lists</p>
                  <p className="text-3xl font-bold text-neutral-900 mt-1">
                    {signUpLists?.length || 0}
                  </p>
                </div>
                <div className="p-3 rounded-lg" style={{ background: '#FFF4ED' }}>
                  <ListPlus className="h-6 w-6" style={{ color: '#FF7900' }} />
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-neutral-500">Total Commitments</p>
                  <p className="text-3xl font-bold text-neutral-900 mt-1">{totalCommitments}</p>
                </div>
                <div className="p-3 rounded-lg" style={{ background: '#FFF4ED' }}>
                  <Users className="h-6 w-6" style={{ color: '#FF7900' }} />
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Actions */}
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-bold" style={{ color: '#8B1538' }}>
            Sign-Up Lists
          </h2>
          <div className="flex gap-3">
            {signUpLists && signUpLists.length > 0 && (
              <Button
                variant="outline"
                onClick={handleDownloadCSV}
                className="flex items-center gap-2"
              >
                <Download className="h-4 w-4" />
                Download CSV
              </Button>
            )}
            <Button
              onClick={() => setShowForm(!showForm)}
              style={{ background: '#FF7900' }}
              className="flex items-center gap-2"
            >
              <Plus className="h-4 w-4" />
              {showForm ? 'Cancel' : 'Create Sign-Up List'}
            </Button>
          </div>
        </div>

        {/* Create Sign-Up List Form */}
        {showForm && (
          <Card className="mb-6">
            <CardHeader>
              <CardTitle style={{ color: '#8B1538' }}>Create New Sign-Up List</CardTitle>
              <CardDescription>
                Add a new sign-up list for attendees to commit to bringing items
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {/* Category */}
              <div>
                <label htmlFor="category" className="block text-sm font-medium text-neutral-700 mb-2">
                  Category *
                </label>
                <Input
                  id="category"
                  type="text"
                  placeholder="e.g., Food & Drinks, Decorations, Supplies"
                  value={category}
                  onChange={(e) => setCategory(e.target.value)}
                />
              </div>

              {/* Description */}
              <div>
                <label htmlFor="description" className="block text-sm font-medium text-neutral-700 mb-2">
                  Description *
                </label>
                <textarea
                  id="description"
                  rows={3}
                  placeholder="Describe what items are needed or provide instructions..."
                  className="w-full px-4 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 resize-none"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                />
              </div>

              {/* Sign-Up Type */}
              <div>
                <label htmlFor="signUpType" className="block text-sm font-medium text-neutral-700 mb-2">
                  Sign-Up Type *
                </label>
                <select
                  id="signUpType"
                  className="w-full px-4 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500"
                  value={signUpType}
                  onChange={(e) => setSignUpType(Number(e.target.value) as SignUpType)}
                >
                  <option value={SignUpType.Open}>Open (Users can specify any item)</option>
                  <option value={SignUpType.Predefined}>Predefined (Users choose from list)</option>
                </select>
              </div>

              {/* Predefined Items (shown only for Predefined type) */}
              {signUpType === SignUpType.Predefined && (
                <div>
                  <label className="block text-sm font-medium text-neutral-700 mb-2">
                    Predefined Items *
                  </label>
                  <div className="space-y-2">
                    {/* List of predefined items */}
                    {predefinedItems.map((item, index) => (
                      <div key={index} className="flex items-center gap-2">
                        <Input
                          type="text"
                          value={item}
                          disabled
                          className="flex-1"
                        />
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleRemovePredefinedItem(index)}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    ))}

                    {/* Add new item */}
                    <div className="flex items-center gap-2">
                      <Input
                        type="text"
                        placeholder="Enter item name"
                        value={newItem}
                        onChange={(e) => setNewItem(e.target.value)}
                        onKeyPress={(e) => {
                          if (e.key === 'Enter') {
                            e.preventDefault();
                            handleAddPredefinedItem();
                          }
                        }}
                        className="flex-1"
                      />
                      <Button
                        type="button"
                        onClick={handleAddPredefinedItem}
                        style={{ background: '#FF7900' }}
                      >
                        Add Item
                      </Button>
                    </div>
                  </div>
                </div>
              )}

              {/* Error Message */}
              {submitError && (
                <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
                  <p className="text-sm text-red-600">{submitError}</p>
                </div>
              )}

              {/* Form Actions */}
              <div className="flex items-center justify-end gap-3 pt-4">
                <Button
                  variant="outline"
                  onClick={() => {
                    setShowForm(false);
                    setCategory('');
                    setDescription('');
                    setSignUpType(SignUpType.Open);
                    setPredefinedItems([]);
                    setSubmitError(null);
                  }}
                >
                  Cancel
                </Button>
                <Button
                  onClick={handleCreateSignUpList}
                  disabled={addSignUpListMutation.isPending}
                  style={{ background: '#FF7900' }}
                >
                  {addSignUpListMutation.isPending ? 'Creating...' : 'Create Sign-Up List'}
                </Button>
              </div>
            </CardContent>
          </Card>
        )}

        {/* Sign-Up Lists */}
        {signUpsLoading ? (
          <div className="grid grid-cols-1 gap-6">
            {[...Array(2)].map((_, i) => (
              <Card key={i} className="animate-pulse">
                <CardContent className="p-6">
                  <div className="h-6 bg-neutral-200 rounded w-1/3 mb-4"></div>
                  <div className="h-4 bg-neutral-200 rounded w-2/3"></div>
                </CardContent>
              </Card>
            ))}
          </div>
        ) : signUpLists && signUpLists.length > 0 ? (
          <div className="grid grid-cols-1 gap-6">
            {signUpLists.map((list) => (
              <Card key={list.id}>
                <CardHeader>
                  <div className="flex items-start justify-between">
                    <div>
                      <CardTitle style={{ color: '#8B1538' }}>{list.category}</CardTitle>
                      <CardDescription className="mt-2">{list.description}</CardDescription>
                    </div>
                    {deleteConfirmId === list.id ? (
                      <div className="flex gap-2">
                        <Button
                          variant="destructive"
                          size="sm"
                          onClick={() => handleDeleteSignUpList(list.id)}
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
                      </div>
                    ) : (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setDeleteConfirmId(list.id)}
                        className="text-red-600 hover:text-red-700"
                      >
                        <Trash2 className="h-4 w-4 mr-2" />
                        Delete
                      </Button>
                    )}
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    {/* Type Badge */}
                    <div>
                      <span className="inline-block px-3 py-1 text-sm font-medium rounded-full border-2" style={{ borderColor: '#FF7900', color: '#FF7900', background: 'white' }}>
                        {list.signUpType === SignUpType.Open ? 'Open List' : 'Predefined List'}
                      </span>
                    </div>

                    {/* Predefined Items */}
                    {list.signUpType === SignUpType.Predefined && list.predefinedItems.length > 0 && (
                      <div>
                        <h4 className="text-sm font-semibold text-neutral-700 mb-2">Available Items:</h4>
                        <div className="flex flex-wrap gap-2">
                          {list.predefinedItems.map((item, index) => (
                            <span
                              key={index}
                              className="px-3 py-1 text-sm bg-neutral-100 text-neutral-700 rounded-full"
                            >
                              {item}
                            </span>
                          ))}
                        </div>
                      </div>
                    )}

                    {/* Commitments */}
                    <div>
                      <h4 className="text-sm font-semibold text-neutral-700 mb-3">
                        Commitments ({list.commitmentCount})
                      </h4>
                      {list.commitments.length > 0 ? (
                        <div className="space-y-2">
                          {list.commitments.map((commitment) => (
                            <div
                              key={commitment.id}
                              className="flex items-center justify-between p-3 bg-neutral-50 rounded-lg"
                            >
                              <div className="flex-1">
                                <p className="font-medium text-neutral-900">
                                  {commitment.itemDescription}
                                </p>
                                <p className="text-sm text-neutral-500">
                                  Quantity: {commitment.quantity} • User: {commitment.userId.substring(0, 8)}...
                                </p>
                              </div>
                              <p className="text-sm text-neutral-400">
                                {new Date(commitment.committedAt).toLocaleDateString()}
                              </p>
                            </div>
                          ))}
                        </div>
                      ) : (
                        <p className="text-neutral-500 text-sm italic">
                          No commitments yet
                        </p>
                      )}
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        ) : (
          <Card>
            <CardContent className="p-12 text-center">
              <ListPlus className="h-16 w-16 mx-auto mb-4 text-neutral-400" />
              <h3 className="text-xl font-semibold text-neutral-900 mb-2">
                No Sign-Up Lists Yet
              </h3>
              <p className="text-neutral-500 mb-6">
                Create your first sign-up list to let attendees commit to bringing items
              </p>
              <Button
                onClick={() => setShowForm(true)}
                style={{ background: '#FF7900' }}
              >
                <Plus className="h-4 w-4 mr-2" />
                Create Sign-Up List
              </Button>
            </CardContent>
          </Card>
        )}
      </div>

      <Footer />
    </div>
  );
}
