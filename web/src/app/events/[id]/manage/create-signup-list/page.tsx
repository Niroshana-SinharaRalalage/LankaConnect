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
  useCreateSignUpList,
  useRemoveSignUpItem,
} from '@/presentation/hooks/useEventSignUps';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { Plus, Trash2, Download, ArrowLeft, ListPlus, Users, X, Edit } from 'lucide-react';
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

  // DEBUG: Log sign-up lists data
  useEffect(() => {
    console.log('[ManageSignUps] Sign-up lists data:', signUpLists);
    console.log('[ManageSignUps] Loading state:', signUpsLoading);
  }, [signUpLists, signUpsLoading]);

  // Mutations
  const removeSignUpListMutation = useRemoveSignUpList();
  const createSignUpListMutation = useCreateSignUpList();
  const removeSignUpItemMutation = useRemoveSignUpItem();

  // Form state - Always show form on create page
  const [showForm, setShowForm] = useState(true);
  const [category, setCategory] = useState('');
  const [description, setDescription] = useState('');
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);


  // Category checkboxes - organizer selects which categories to enable
  const [hasMandatoryItems, setHasMandatoryItems] = useState(false);
  const [hasPreferredItems, setHasPreferredItems] = useState(false);
  const [hasSuggestedItems, setHasSuggestedItems] = useState(false);
  // Phase 6A.28: Open Items - users can add their own items
  const [hasOpenItems, setHasOpenItems] = useState(false);

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
      router.push('/login?redirect=' + encodeURIComponent(`/events/${eventId}/manage/create-signup-list`));
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

  // Handle create sign-up list WITH items in single API call
  const handleCreateSignUpList = async () => {
    console.log('[ManageSignUps] handleCreateSignUpList called');

    if (!category.trim()) {
      setSubmitError('Sign-Up List Name is required');
      return;
    }

    if (!description.trim()) {
      setSubmitError('Description is required');
      return;
    }

    // Phase 6A.28: Open Items doesn't require predefined items, so check separately
    // Note: hasPreferredItems is kept for backwards compatibility but hidden from UI
    if (!hasMandatoryItems && !hasSuggestedItems && !hasOpenItems) {
      setSubmitError('Please select at least one category (Mandatory, Suggested, or Open Items)');
      return;
    }

    // Validate at least one item exists (unless only Open Items is enabled)
    const allItems = [...mandatoryItems, ...preferredItems, ...suggestedItems];
    // Phase 6A.28: If only hasOpenItems is selected, we don't require predefined items
    // because users will add their own items
    const requiresPredefinedItems = hasMandatoryItems || hasPreferredItems || hasSuggestedItems;
    if (requiresPredefinedItems && allItems.length === 0) {
      setSubmitError('Please add at least one item to the sign-up list');
      return;
    }

    try {
      setSubmitError(null);

      // Convert items to API format
      const items = [
        ...mandatoryItems.map(item => ({
          itemDescription: item.description,
          quantity: item.quantity,
          itemCategory: SignUpItemCategory.Mandatory,
          notes: item.notes || null,
        })),
        ...preferredItems.map(item => ({
          itemDescription: item.description,
          quantity: item.quantity,
          itemCategory: SignUpItemCategory.Preferred,
          notes: item.notes || null,
        })),
        ...suggestedItems.map(item => ({
          itemDescription: item.description,
          quantity: item.quantity,
          itemCategory: SignUpItemCategory.Suggested,
          notes: item.notes || null,
        })),
      ];

      const payload = {
        eventId,
        category: category.trim(),
        description: description.trim(),
        hasMandatoryItems,
        hasPreferredItems,
        hasSuggestedItems,
        hasOpenItems, // Phase 6A.28: Open Items support
        items,
      };

      console.log('[ManageSignUps] Creating sign-up list with payload:', payload);

      // Create sign-up list WITH all items in single transactional API call
      const result = await createSignUpListMutation.mutateAsync(payload);

      console.log('[ManageSignUps] Sign-up list created successfully:', result);

      // Reset form
      setCategory('');
      setDescription('');
      setHasMandatoryItems(false);
      setHasPreferredItems(false);
      setHasSuggestedItems(false);
      setHasOpenItems(false); // Phase 6A.28
      setMandatoryItems([]);
      setPreferredItems([]);
      setSuggestedItems([]);
      setShowForm(false);

      console.log('[ManageSignUps] Form reset complete');

      // Navigate to manage page after successful creation
      router.push(`/events/${eventId}/manage`);
    } catch (err) {
      console.error('[ManageSignUps] Failed to create sign-up list:', err);
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
      (list.commitments || []).forEach((commitment) => {
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
            Create Sign-Up List
          </h1>
          <p className="text-lg text-white/90">
            {event.title}
          </p>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">

        {/* Create Sign-Up List Form - Always shown */}
        {(
          <Card className="mb-6">
            <CardHeader>
              <CardTitle style={{ color: '#8B1538' }}>Create New Sign-Up List</CardTitle>
              <CardDescription>
                Add a new sign-up list for attendees to commit to bringing items
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {/* Category/List Name */}
              <div>
                <label htmlFor="category" className="block text-sm font-medium text-neutral-700 mb-2">
                  Sign-Up List Name *
                </label>
                <Input
                  id="category"
                  type="text"
                  placeholder="e.g., Food & Drinks, Decorations, Supplies"
                  value={category}
                  onChange={(e) => setCategory(e.target.value)}
                />
                <p className="text-xs text-neutral-500 mt-1">
                  Give your sign-up list a descriptive name
                </p>
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
                  <label className="block text-sm font-medium text-neutral-700 mb-3">
                    Select Item Categories * (at least one required)
                  </label>

                  {/* Mandatory Items Checkbox + Section */}
                  <div className="space-y-3">
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
                  </div>

                  {/* Phase 6A.28: Preferred Items DEPRECATED - Hidden from UI
                      The "Preferred" category has been deprecated in favor of "Suggested" and "Open" categories.
                      The backend still supports Preferred for backwards compatibility with existing data.
                      State variables (hasPreferredItems, preferredItems, etc.) are kept for data migration purposes.
                  */}

                  {/* Suggested Items Checkbox + Section */}
                  <div className="space-y-3">
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

                  {/* Phase 6A.28: Open Items Checkbox */}
                  <div className="space-y-3">
                    <label className="flex items-center gap-3 p-3 border rounded-lg cursor-pointer hover:bg-neutral-50 border-purple-200 bg-purple-50/30">
                      <input
                        type="checkbox"
                        checked={hasOpenItems}
                        onChange={(e) => setHasOpenItems(e.target.checked)}
                        className="w-4 h-4 text-purple-600"
                      />
                      <div>
                        <p className="font-medium text-neutral-900">Open Items (Bring Your Own)</p>
                        <p className="text-sm text-neutral-500">
                          Allow attendees to sign up with their own items. Users can add custom items they&apos;ll bring.
                        </p>
                      </div>
                    </label>

                    {hasOpenItems && (
                      <div className="border-t pt-4 bg-purple-50/50 rounded-lg p-4 border border-purple-200">
                        <div className="flex items-center gap-2 mb-2">
                          <span className="px-2 py-1 rounded text-xs font-medium bg-purple-100 text-purple-800">
                            Open Items
                          </span>
                        </div>
                        <p className="text-sm text-neutral-600">
                          <strong>How it works:</strong> When this is enabled, attendees can click &quot;Sign Up&quot; to add their own items
                          (e.g., &quot;Homemade Cookies - 24 pieces&quot;). Each user manages their own items and can update or cancel them.
                        </p>
                        <p className="text-sm text-neutral-500 mt-2">
                          No predefined items needed - users will create their own when they sign up.
                        </p>
                      </div>
                    )}
                  </div>
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
                    setHasOpenItems(false); // Phase 6A.28
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
                  disabled={createSignUpListMutation.isPending}
                  style={{ background: '#FF7900' }}
                >
                  {createSignUpListMutation.isPending ? 'Creating...' : 'Create Sign-Up List'}
                </Button>
              </div>
            </CardContent>
          </Card>
        )}
      </div>

      <Footer />
    </div>
  );
}
