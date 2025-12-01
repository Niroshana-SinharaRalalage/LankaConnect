'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useEventById } from '@/presentation/hooks/useEvents';
import {
  useEventSignUps,
  useRemoveSignUpList,
  useAddSignUpListWithCategories,
  useAddSignUpItem,
  useRemoveSignUpItem,
} from '@/presentation/hooks/useEventSignUps';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { Plus, Trash2, Download, ArrowLeft, ListPlus, Users, X } from 'lucide-react';
import { SignUpItemCategory } from '@/infrastructure/api/types/events.types';
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
  const removeSignUpListMutation = useRemoveSignUpList();
  const addSignUpListWithCategoriesMutation = useAddSignUpListWithCategories();
  const addSignUpItemMutation = useAddSignUpItem();
  const removeSignUpItemMutation = useRemoveSignUpItem();

  // Form state
  const [showForm, setShowForm] = useState(false);
  const [category, setCategory] = useState('');
  const [description, setDescription] = useState('');
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);

  // Category checkboxes - organizer selects which categories to enable
  const [hasMandatoryItems, setHasMandatoryItems] = useState(false);
  const [hasPreferredItems, setHasPreferredItems] = useState(false);
  const [hasSuggestedItems, setHasSuggestedItems] = useState(false);

  // Separate item arrays for each category
  type ItemType = {
    description: string;
    quantity: number;
    notes: string;
  };
  const [mandatoryItems, setMandatoryItems] = useState<ItemType[]>([]);
  const [preferredItems, setPreferredItems] = useState<ItemType[]>([]);
  const [suggestedItems, setSuggestedItems] = useState<ItemType[]>([]);

  // Separate form state for each category
  const [newMandatoryDesc, setNewMandatoryDesc] = useState('');
  const [newMandatoryQty, setNewMandatoryQty] = useState(1);
  const [newMandatoryNotes, setNewMandatoryNotes] = useState('');

  const [newPreferredDesc, setNewPreferredDesc] = useState('');
  const [newPreferredQty, setNewPreferredQty] = useState(1);
  const [newPreferredNotes, setNewPreferredNotes] = useState('');

  const [newSuggestedDesc, setNewSuggestedDesc] = useState('');
  const [newSuggestedQty, setNewSuggestedQty] = useState(1);
  const [newSuggestedNotes, setNewSuggestedNotes] = useState('');
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

  // Handle add mandatory item
  const handleAddMandatoryItem = () => {
    if (!newMandatoryDesc.trim()) {
      setSubmitError('Item description is required');
      return;
    }
    if (newMandatoryQty < 1) {
      setSubmitError('Quantity must be at least 1');
      return;
    }
    setMandatoryItems([...mandatoryItems, {
      description: newMandatoryDesc.trim(),
      quantity: newMandatoryQty,
      notes: newMandatoryNotes.trim(),
    }]);
    setNewMandatoryDesc('');
    setNewMandatoryQty(1);
    setNewMandatoryNotes('');
    setSubmitError(null);
  };

  // Handle add preferred item
  const handleAddPreferredItem = () => {
    if (!newPreferredDesc.trim()) {
      setSubmitError('Item description is required');
      return;
    }
    if (newPreferredQty < 1) {
      setSubmitError('Quantity must be at least 1');
      return;
    }
    setPreferredItems([...preferredItems, {
      description: newPreferredDesc.trim(),
      quantity: newPreferredQty,
      notes: newPreferredNotes.trim(),
    }]);
    setNewPreferredDesc('');
    setNewPreferredQty(1);
    setNewPreferredNotes('');
    setSubmitError(null);
  };

  // Handle add suggested item
  const handleAddSuggestedItem = () => {
    if (!newSuggestedDesc.trim()) {
      setSubmitError('Item description is required');
      return;
    }
    if (newSuggestedQty < 1) {
      setSubmitError('Quantity must be at least 1');
      return;
    }
    setSuggestedItems([...suggestedItems, {
      description: newSuggestedDesc.trim(),
      quantity: newSuggestedQty,
      notes: newSuggestedNotes.trim(),
    }]);
    setNewSuggestedDesc('');
    setNewSuggestedQty(1);
    setNewSuggestedNotes('');
    setSubmitError(null);
  };

  // Handle remove item from category
  const handleRemoveMandatoryItem = (index: number) => {
    setMandatoryItems(mandatoryItems.filter((_, i) => i !== index));
  };

  const handleRemovePreferredItem = (index: number) => {
    setPreferredItems(preferredItems.filter((_, i) => i !== index));
  };

  const handleRemoveSuggestedItem = (index: number) => {
    setSuggestedItems(suggestedItems.filter((_, i) => i !== index));
  };

  // Handle create sign-up lists (one per selected category)
  const handleCreateSignUpList = async () => {
    if (!category.trim()) {
      setSubmitError('Category is required');
      return;
    }

    if (!description.trim()) {
      setSubmitError('Description is required');
      return;
    }

    if (!hasMandatoryItems && !hasPreferredItems && !hasSuggestedItems) {
      setSubmitError('Please select at least one category (Mandatory, Preferred, or Suggested)');
      return;
    }

    try {
      setSubmitError(null);

      // Create Mandatory sign-up list if selected
      if (hasMandatoryItems && mandatoryItems.length > 0) {
        const mandatorySignUpId = await addSignUpListWithCategoriesMutation.mutateAsync({
          eventId,
          category: `${category.trim()} - Mandatory Items`,
          description: description.trim(),
          hasMandatoryItems: true,
          hasPreferredItems: false,
          hasSuggestedItems: false,
        });

        // Add all mandatory items
        for (const item of mandatoryItems) {
          await addSignUpItemMutation.mutateAsync({
            eventId,
            signupId: mandatorySignUpId as unknown as string,
            itemDescription: item.description,
            quantity: item.quantity,
            itemCategory: SignUpItemCategory.Mandatory,
            notes: item.notes || undefined,
          });
        }
      }

      // Create Preferred sign-up list if selected
      if (hasPreferredItems && preferredItems.length > 0) {
        const preferredSignUpId = await addSignUpListWithCategoriesMutation.mutateAsync({
          eventId,
          category: `${category.trim()} - Preferred Items`,
          description: description.trim(),
          hasMandatoryItems: false,
          hasPreferredItems: true,
          hasSuggestedItems: false,
        });

        // Add all preferred items
        for (const item of preferredItems) {
          await addSignUpItemMutation.mutateAsync({
            eventId,
            signupId: preferredSignUpId as unknown as string,
            itemDescription: item.description,
            quantity: item.quantity,
            itemCategory: SignUpItemCategory.Preferred,
            notes: item.notes || undefined,
          });
        }
      }

      // Create Suggested sign-up list if selected
      if (hasSuggestedItems && suggestedItems.length > 0) {
        const suggestedSignUpId = await addSignUpListWithCategoriesMutation.mutateAsync({
          eventId,
          category: `${category.trim()} - Suggested Items`,
          description: description.trim(),
          hasMandatoryItems: false,
          hasPreferredItems: false,
          hasSuggestedItems: true,
        });

        // Add all suggested items
        for (const item of suggestedItems) {
          await addSignUpItemMutation.mutateAsync({
            eventId,
            signupId: suggestedSignUpId as unknown as string,
            itemDescription: item.description,
            quantity: item.quantity,
            itemCategory: SignUpItemCategory.Suggested,
            notes: item.notes || undefined,
          });
        }
      }

      // Reset form
      setCategory('');
      setDescription('');
      setHasMandatoryItems(false);
      setHasPreferredItems(false);
      setHasSuggestedItems(false);
      setMandatoryItems([]);
      setPreferredItems([]);
      setSuggestedItems([]);
      setShowForm(false);
    } catch (err) {
      console.error('Failed to create sign-up lists:', err);
      setSubmitError(err instanceof Error ? err.message : 'Failed to create sign-up lists');
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

              {/* Category-Based Items */}
              <div className="space-y-4">
                  {/* Category Checkboxes */}
                  <div>
                    <label className="block text-sm font-medium text-neutral-700 mb-3">
                      Select Item Categories * (at least one required)
                    </label>
                    <div className="space-y-2">
                      <label className="flex items-center gap-3 p-3 border rounded-lg cursor-pointer hover:bg-neutral-50">
                        <input
                          type="checkbox"
                          checked={hasMandatoryItems}
                          onChange={(e) => setHasMandatoryItems(e.target.checked)}
                          className="w-4 h-4 text-red-600"
                        />
                        <div>
                          <p className="font-medium text-neutral-900">Mandatory Items</p>
                          <p className="text-sm text-neutral-500">Required items that must be brought</p>
                        </div>
                      </label>

                      <label className="flex items-center gap-3 p-3 border rounded-lg cursor-pointer hover:bg-neutral-50">
                        <input
                          type="checkbox"
                          checked={hasPreferredItems}
                          onChange={(e) => setHasPreferredItems(e.target.checked)}
                          className="w-4 h-4 text-blue-600"
                        />
                        <div>
                          <p className="font-medium text-neutral-900">Preferred Items</p>
                          <p className="text-sm text-neutral-500">Highly desired items</p>
                        </div>
                      </label>

                      <label className="flex items-center gap-3 p-3 border rounded-lg cursor-pointer hover:bg-neutral-50">
                        <input
                          type="checkbox"
                          checked={hasSuggestedItems}
                          onChange={(e) => setHasSuggestedItems(e.target.checked)}
                          className="w-4 h-4 text-green-600"
                        />
                        <div>
                          <p className="font-medium text-neutral-900">Suggested Items</p>
                          <p className="text-sm text-neutral-500">Optional items that would be nice to have</p>
                        </div>
                      </label>
                    </div>
                  </div>

                  {/* Mandatory Items Section */}
                  {hasMandatoryItems && (
                    <div className="border-t pt-4">
                      <h4 className="text-md font-semibold text-neutral-800 mb-3 flex items-center gap-2">
                        <span className="px-2 py-1 rounded text-xs font-medium bg-red-100 text-red-800">
                          Mandatory Items
                        </span>
                      </h4>
                      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                        {/* Left: Add Item Form */}
                        <div className="space-y-3">
                          <div>
                            <label className="block text-xs font-medium text-neutral-600 mb-1">
                              Item Description *
                            </label>
                            <Input
                              type="text"
                              placeholder="e.g., Rice (5 cups)"
                              value={newMandatoryDesc}
                              onChange={(e) => setNewMandatoryDesc(e.target.value)}
                            />
                          </div>
                          <div>
                            <label className="block text-xs font-medium text-neutral-600 mb-1">
                              Quantity *
                            </label>
                            <Input
                              type="number"
                              min="1"
                              value={newMandatoryQty}
                              onChange={(e) => setNewMandatoryQty(parseInt(e.target.value) || 1)}
                            />
                          </div>
                          <div>
                            <label className="block text-xs font-medium text-neutral-600 mb-1">
                              Notes (optional)
                            </label>
                            <textarea
                              rows={2}
                              placeholder="Any additional details..."
                              className="w-full px-3 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 resize-none"
                              value={newMandatoryNotes}
                              onChange={(e) => setNewMandatoryNotes(e.target.value)}
                            />
                          </div>
                          <Button
                            type="button"
                            onClick={handleAddMandatoryItem}
                            variant="outline"
                            className="w-full"
                          >
                            <Plus className="h-4 w-4 mr-2" />
                            Add Item
                          </Button>
                        </div>

                        {/* Right: Items Table */}
                        <div className="border rounded-lg overflow-hidden max-h-96 overflow-y-auto">
                          {mandatoryItems.length === 0 ? (
                            <div className="p-4 text-center text-neutral-500 text-sm">
                              No items added yet
                            </div>
                          ) : (
                            <table className="w-full">
                              <thead className="bg-neutral-100 sticky top-0">
                                <tr>
                                  <th className="px-3 py-2 text-left text-xs font-medium text-neutral-600">Item</th>
                                  <th className="px-3 py-2 text-left text-xs font-medium text-neutral-600">Qty</th>
                                  <th className="px-3 py-2 text-center text-xs font-medium text-neutral-600">Action</th>
                                </tr>
                              </thead>
                              <tbody>
                                {mandatoryItems.map((item, index) => (
                                  <tr key={index} className="border-t">
                                    <td className="px-3 py-2 text-sm">
                                      <div>
                                        <p className="font-medium">{item.description}</p>
                                        {item.notes && (
                                          <p className="text-xs text-neutral-500">{item.notes}</p>
                                        )}
                                      </div>
                                    </td>
                                    <td className="px-3 py-2 text-sm">{item.quantity}</td>
                                    <td className="px-3 py-2 text-center">
                                      <Button
                                        variant="outline"
                                        size="sm"
                                        onClick={() => handleRemoveMandatoryItem(index)}
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
                  )}

                  {/* Preferred Items Section */}
                  {hasPreferredItems && (
                    <div className="border-t pt-4">
                      <h4 className="text-md font-semibold text-neutral-800 mb-3 flex items-center gap-2">
                        <span className="px-2 py-1 rounded text-xs font-medium bg-blue-100 text-blue-800">
                          Preferred Items
                        </span>
                      </h4>
                      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                        {/* Left: Add Item Form */}
                        <div className="space-y-3">
                          <div>
                            <label className="block text-xs font-medium text-neutral-600 mb-1">
                              Item Description *
                            </label>
                            <Input
                              type="text"
                              placeholder="e.g., Side dish"
                              value={newPreferredDesc}
                              onChange={(e) => setNewPreferredDesc(e.target.value)}
                            />
                          </div>
                          <div>
                            <label className="block text-xs font-medium text-neutral-600 mb-1">
                              Quantity *
                            </label>
                            <Input
                              type="number"
                              min="1"
                              value={newPreferredQty}
                              onChange={(e) => setNewPreferredQty(parseInt(e.target.value) || 1)}
                            />
                          </div>
                          <div>
                            <label className="block text-xs font-medium text-neutral-600 mb-1">
                              Notes (optional)
                            </label>
                            <textarea
                              rows={2}
                              placeholder="Any additional details..."
                              className="w-full px-3 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 resize-none"
                              value={newPreferredNotes}
                              onChange={(e) => setNewPreferredNotes(e.target.value)}
                            />
                          </div>
                          <Button
                            type="button"
                            onClick={handleAddPreferredItem}
                            variant="outline"
                            className="w-full"
                          >
                            <Plus className="h-4 w-4 mr-2" />
                            Add Item
                          </Button>
                        </div>

                        {/* Right: Items Table */}
                        <div className="border rounded-lg overflow-hidden max-h-96 overflow-y-auto">
                          {preferredItems.length === 0 ? (
                            <div className="p-4 text-center text-neutral-500 text-sm">
                              No items added yet
                            </div>
                          ) : (
                            <table className="w-full">
                              <thead className="bg-neutral-100 sticky top-0">
                                <tr>
                                  <th className="px-3 py-2 text-left text-xs font-medium text-neutral-600">Item</th>
                                  <th className="px-3 py-2 text-left text-xs font-medium text-neutral-600">Qty</th>
                                  <th className="px-3 py-2 text-center text-xs font-medium text-neutral-600">Action</th>
                                </tr>
                              </thead>
                              <tbody>
                                {preferredItems.map((item, index) => (
                                  <tr key={index} className="border-t">
                                    <td className="px-3 py-2 text-sm">
                                      <div>
                                        <p className="font-medium">{item.description}</p>
                                        {item.notes && (
                                          <p className="text-xs text-neutral-500">{item.notes}</p>
                                        )}
                                      </div>
                                    </td>
                                    <td className="px-3 py-2 text-sm">{item.quantity}</td>
                                    <td className="px-3 py-2 text-center">
                                      <Button
                                        variant="outline"
                                        size="sm"
                                        onClick={() => handleRemovePreferredItem(index)}
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
                  )}

                  {/* Suggested Items Section */}
                  {hasSuggestedItems && (
                    <div className="border-t pt-4">
                      <h4 className="text-md font-semibold text-neutral-800 mb-3 flex items-center gap-2">
                        <span className="px-2 py-1 rounded text-xs font-medium bg-green-100 text-green-800">
                          Suggested Items
                        </span>
                      </h4>
                      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                        {/* Left: Add Item Form */}
                        <div className="space-y-3">
                          <div>
                            <label className="block text-xs font-medium text-neutral-600 mb-1">
                              Item Description *
                            </label>
                            <Input
                              type="text"
                              placeholder="e.g., Dessert"
                              value={newSuggestedDesc}
                              onChange={(e) => setNewSuggestedDesc(e.target.value)}
                            />
                          </div>
                          <div>
                            <label className="block text-xs font-medium text-neutral-600 mb-1">
                              Quantity *
                            </label>
                            <Input
                              type="number"
                              min="1"
                              value={newSuggestedQty}
                              onChange={(e) => setNewSuggestedQty(parseInt(e.target.value) || 1)}
                            />
                          </div>
                          <div>
                            <label className="block text-xs font-medium text-neutral-600 mb-1">
                              Notes (optional)
                            </label>
                            <textarea
                              rows={2}
                              placeholder="Any additional details..."
                              className="w-full px-3 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 resize-none"
                              value={newSuggestedNotes}
                              onChange={(e) => setNewSuggestedNotes(e.target.value)}
                            />
                          </div>
                          <Button
                            type="button"
                            onClick={handleAddSuggestedItem}
                            variant="outline"
                            className="w-full"
                          >
                            <Plus className="h-4 w-4 mr-2" />
                            Add Item
                          </Button>
                        </div>

                        {/* Right: Items Table */}
                        <div className="border rounded-lg overflow-hidden max-h-96 overflow-y-auto">
                          {suggestedItems.length === 0 ? (
                            <div className="p-4 text-center text-neutral-500 text-sm">
                              No items added yet
                            </div>
                          ) : (
                            <table className="w-full">
                              <thead className="bg-neutral-100 sticky top-0">
                                <tr>
                                  <th className="px-3 py-2 text-left text-xs font-medium text-neutral-600">Item</th>
                                  <th className="px-3 py-2 text-left text-xs font-medium text-neutral-600">Qty</th>
                                  <th className="px-3 py-2 text-center text-xs font-medium text-neutral-600">Action</th>
                                </tr>
                              </thead>
                              <tbody>
                                {suggestedItems.map((item, index) => (
                                  <tr key={index} className="border-t">
                                    <td className="px-3 py-2 text-sm">
                                      <div>
                                        <p className="font-medium">{item.description}</p>
                                        {item.notes && (
                                          <p className="text-xs text-neutral-500">{item.notes}</p>
                                        )}
                                      </div>
                                    </td>
                                    <td className="px-3 py-2 text-sm">{item.quantity}</td>
                                    <td className="px-3 py-2 text-center">
                                      <Button
                                        variant="outline"
                                        size="sm"
                                        onClick={() => handleRemoveSuggestedItem(index)}
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
                  )}
              </div>

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
                    setHasMandatoryItems(false);
                    setHasPreferredItems(false);
                    setHasSuggestedItems(false);
                    setMandatoryItems([]);
                    setPreferredItems([]);
                    setSuggestedItems([]);
                    setSubmitError(null);
                  }}
                >
                  Cancel
                </Button>
                <Button
                  onClick={handleCreateSignUpList}
                  disabled={addSignUpListWithCategoriesMutation.isPending}
                  style={{ background: '#FF7900' }}
                >
                  {addSignUpListWithCategoriesMutation.isPending ? 'Creating...' : 'Create Sign-Up List'}
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
