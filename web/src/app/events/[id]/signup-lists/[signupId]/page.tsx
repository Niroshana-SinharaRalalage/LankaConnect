'use client';

import { useEffect, useState, useMemo } from 'react';
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
  isQuantityBased,
  type SignUpItemDto,
} from '@/infrastructure/api/types/events.types';
import { UserRole } from '@/infrastructure/api/types/auth.types';

/**
 * Edit Sign-Up List Page
 * Phase 6A.13 (Revised): Full edit page for sign-up lists
 *
 * Allows event organizers to:
 * - Edit sign-up list category and description
 * - Toggle category flags (Mandatory, Preferred, Suggested)
 * - View all items grouped by category
 * - Add new items to each category
 * - Edit existing items (inline editing)
 * - Delete items
 */
export default function EditSignUpListPage() {
  const params = useParams();
  const router = useRouter();
  const eventId = params.id as string;
  const signupId = params.signupId as string;
  const { user, isAuthenticated } = useAuthStore();

  // Fetch event and sign-up lists
  const { data: event, isLoading: eventLoading } = useEventById(eventId);
  const { data: signUpLists, isLoading: signUpsLoading } = useEventSignUps(eventId);

  // Find the specific sign-up list we're editing
  const signUpList = signUpLists?.find(list => list.id === signupId);

  // Mutations
  const updateSignUpListMutation = useUpdateSignUpList(eventId);
  const addSignUpItemMutation = useAddSignUpItem();
  const updateSignUpItemMutation = useUpdateSignUpItem();
  const removeSignUpItemMutation = useRemoveSignUpItem();

  // Form state for list details
  const [category, setCategory] = useState('');
  const [description, setDescription] = useState('');
  const [hasMandatoryItems, setHasMandatoryItems] = useState(false);
  const [hasPreferredItems, setHasPreferredItems] = useState(false);
  const [hasSuggestedItems, setHasSuggestedItems] = useState(false);
  // Phase 6A.28: Open Items support
  const [hasOpenItems, setHasOpenItems] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  // Track original values for dirty state detection
  const [originalCategory, setOriginalCategory] = useState('');
  const [originalDescription, setOriginalDescription] = useState('');
  const [originalHasMandatoryItems, setOriginalHasMandatoryItems] = useState(false);
  const [originalHasPreferredItems, setOriginalHasPreferredItems] = useState(false);
  const [originalHasSuggestedItems, setOriginalHasSuggestedItems] = useState(false);
  const [originalHasOpenItems, setOriginalHasOpenItems] = useState(false);

  // Item editing state
  // Phase 6A.131: Track the item's type so save can send the correctly discriminated field.
  const [editingItemId, setEditingItemId] = useState<string | null>(null);
  const [editingItemDesc, setEditingItemDesc] = useState('');
  const [editingItemQty, setEditingItemQty] = useState(1);
  const [editingItemNotes, setEditingItemNotes] = useState('');
  const [editingItemType, setEditingItemType] = useState<SignUpItemType>(SignUpItemType.Quantity);

  // New item state for each category
  const [newMandatoryDesc, setNewMandatoryDesc] = useState('');
  const [newMandatoryQty, setNewMandatoryQty] = useState(1);
  const [newMandatoryNotes, setNewMandatoryNotes] = useState('');
  // Phase 6A.121: Slot-based item state for Mandatory
  const [newMandatoryItemType, setNewMandatoryItemType] = useState<SignUpItemType>(SignUpItemType.Quantity);
  const [newMandatorySlots, setNewMandatorySlots] = useState(1);
  const [newMandatorySuggestedPerSlot, setNewMandatorySuggestedPerSlot] = useState<number | undefined>(undefined);

  const [newPreferredDesc, setNewPreferredDesc] = useState('');
  const [newPreferredQty, setNewPreferredQty] = useState(1);
  const [newPreferredNotes, setNewPreferredNotes] = useState('');
  // Phase 6A.121: Slot-based item state for Preferred
  const [newPreferredItemType, setNewPreferredItemType] = useState<SignUpItemType>(SignUpItemType.Quantity);
  const [newPreferredSlots, setNewPreferredSlots] = useState(1);
  const [newPreferredSuggestedPerSlot, setNewPreferredSuggestedPerSlot] = useState<number | undefined>(undefined);

  const [newSuggestedDesc, setNewSuggestedDesc] = useState('');
  const [newSuggestedQty, setNewSuggestedQty] = useState(1);
  const [newSuggestedNotes, setNewSuggestedNotes] = useState('');
  // Phase 6A.121: Slot-based item state for Suggested
  const [newSuggestedItemType, setNewSuggestedItemType] = useState<SignUpItemType>(SignUpItemType.Quantity);
  const [newSuggestedSlots, setNewSuggestedSlots] = useState(1);
  const [newSuggestedSuggestedPerSlot, setNewSuggestedSuggestedPerSlot] = useState<number | undefined>(undefined);

  // Initialize form when sign-up list loads
  useEffect(() => {
    if (signUpList) {
      console.log('[EditSignUpList] Sign-up list loaded:', signUpList);
      console.log('[EditSignUpList] Items array:', signUpList.items);
      console.log('[EditSignUpList] Items count:', signUpList.items?.length || 0);
      console.log('[EditSignUpList] Category flags:', {
        hasMandatoryItems: signUpList.hasMandatoryItems,
        hasPreferredItems: signUpList.hasPreferredItems,
        hasSuggestedItems: signUpList.hasSuggestedItems
      });

      // Set current values
      setCategory(signUpList.category);
      setDescription(signUpList.description);
      setHasMandatoryItems(signUpList.hasMandatoryItems);
      setHasPreferredItems(signUpList.hasPreferredItems);
      setHasSuggestedItems(signUpList.hasSuggestedItems);
      setHasOpenItems(signUpList.hasOpenItems || false); // Phase 6A.28

      // Set original values for dirty state tracking
      setOriginalCategory(signUpList.category);
      setOriginalDescription(signUpList.description);
      setOriginalHasMandatoryItems(signUpList.hasMandatoryItems);
      setOriginalHasPreferredItems(signUpList.hasPreferredItems);
      setOriginalHasSuggestedItems(signUpList.hasSuggestedItems);
      setOriginalHasOpenItems(signUpList.hasOpenItems || false); // Phase 6A.28
    }
  }, [signUpList]);

  // Authentication guard
  useEffect(() => {
    if (!isAuthenticated || !user?.userId) {
      router.push('/login?redirect=' + encodeURIComponent(`/events/${eventId}/signup-lists/${signupId}`));
      return;
    }

    const isAuthorized = event && (
      event.isCurrentUserOrganizer === true ||
      user.role === UserRole.Admin ||
      user.role === UserRole.AdminManager
    );

    if (event && !isAuthorized) {
      router.push(`/events/${eventId}`);
    }
  }, [isAuthenticated, user, event, eventId, signupId, router]);

  // Compute dirty state - only show save button when changes are made
  const hasChanges = useMemo(() => {
    return category !== originalCategory ||
           description !== originalDescription ||
           hasMandatoryItems !== originalHasMandatoryItems ||
           hasPreferredItems !== originalHasPreferredItems ||
           hasSuggestedItems !== originalHasSuggestedItems ||
           hasOpenItems !== originalHasOpenItems; // Phase 6A.28: Track Open Items changes
  }, [
    category, description, hasMandatoryItems, hasPreferredItems, hasSuggestedItems, hasOpenItems,
    originalCategory, originalDescription, originalHasMandatoryItems, originalHasPreferredItems, originalHasSuggestedItems, originalHasOpenItems
  ]);

  // Handle save list details
  const handleSaveListDetails = async () => {
    if (!category.trim()) {
      setSubmitError('Category is required');
      return;
    }

    if (!description.trim()) {
      setSubmitError('Description is required');
      return;
    }

    // Phase 6A.28: Check Mandatory, Suggested, OR Open Items
    if (!hasMandatoryItems && !hasSuggestedItems && !hasOpenItems) {
      setSubmitError('Please select at least one category (Mandatory, Suggested, or Open Items)');
      return;
    }

    try {
      setSubmitError(null);
      await updateSignUpListMutation.mutateAsync({
        signupId,
        category: category.trim(),
        description: description.trim(),
        hasMandatoryItems,
        hasPreferredItems,
        hasSuggestedItems,
        hasOpenItems, // Phase 6A.28
      });
    } catch (err) {
      console.error('[EditSignUpList] Failed to update sign-up list:', err);
      setSubmitError(err instanceof Error ? err.message : 'Failed to update sign-up list');
    }
  };

  // Phase 6A.126: Check if a category has unsaved changes (checkbox checked but not saved to backend)
  const isMandatoryUnsaved = hasMandatoryItems && !signUpList?.hasMandatoryItems;
  const isSuggestedUnsaved = hasSuggestedItems && !signUpList?.hasSuggestedItems;

  // Handle add mandatory item - Phase 6A.121: Supports both quantity-based and slot-based
  const handleAddMandatoryItem = async () => {
    if (isMandatoryUnsaved) {
      setSubmitError('Please save the list details first (click "Save List Details" above) before adding items to the Mandatory category.');
      return;
    }
    if (!newMandatoryDesc.trim()) {
      setSubmitError('Item description is required');
      return;
    }
    if (newMandatoryItemType === SignUpItemType.Quantity && newMandatoryQty < 1) {
      setSubmitError('Quantity must be at least 1');
      return;
    }
    if (newMandatoryItemType === SignUpItemType.Slot && newMandatorySlots < 1) {
      setSubmitError('Number of slots must be at least 1');
      return;
    }

    try {
      setSubmitError(null);
      await addSignUpItemMutation.mutateAsync({
        eventId,
        signupId,
        itemDescription: newMandatoryDesc.trim(),
        itemType: newMandatoryItemType,
        targetQuantity: newMandatoryItemType === SignUpItemType.Quantity ? newMandatoryQty : undefined,
        availableSlots: newMandatoryItemType === SignUpItemType.Slot ? newMandatorySlots : undefined,
        suggestedPerSlot: newMandatoryItemType === SignUpItemType.Slot ? newMandatorySuggestedPerSlot : undefined,
        itemCategory: SignUpItemCategory.Mandatory,
        notes: newMandatoryNotes.trim() || undefined,
      });
      setNewMandatoryDesc('');
      setNewMandatoryQty(1);
      setNewMandatorySlots(1);
      setNewMandatorySuggestedPerSlot(undefined);
      setNewMandatoryNotes('');
      setNewMandatoryItemType(SignUpItemType.Quantity);
    } catch (err) {
      console.error('[EditSignUpList] Failed to add item:', err);
      setSubmitError(err instanceof Error ? err.message : 'Failed to add item');
    }
  };

  // Handle add preferred item - Phase 6A.121: Supports both quantity-based and slot-based
  const handleAddPreferredItem = async () => {
    if (!newPreferredDesc.trim()) {
      setSubmitError('Item description is required');
      return;
    }
    if (newPreferredItemType === SignUpItemType.Quantity && newPreferredQty < 1) {
      setSubmitError('Quantity must be at least 1');
      return;
    }
    if (newPreferredItemType === SignUpItemType.Slot && newPreferredSlots < 1) {
      setSubmitError('Number of slots must be at least 1');
      return;
    }

    try {
      setSubmitError(null);
      await addSignUpItemMutation.mutateAsync({
        eventId,
        signupId,
        itemDescription: newPreferredDesc.trim(),
        itemType: newPreferredItemType,
        targetQuantity: newPreferredItemType === SignUpItemType.Quantity ? newPreferredQty : undefined,
        availableSlots: newPreferredItemType === SignUpItemType.Slot ? newPreferredSlots : undefined,
        suggestedPerSlot: newPreferredItemType === SignUpItemType.Slot ? newPreferredSuggestedPerSlot : undefined,
        itemCategory: SignUpItemCategory.Preferred,
        notes: newPreferredNotes.trim() || undefined,
      });
      setNewPreferredDesc('');
      setNewPreferredQty(1);
      setNewPreferredSlots(1);
      setNewPreferredSuggestedPerSlot(undefined);
      setNewPreferredNotes('');
      setNewPreferredItemType(SignUpItemType.Quantity);
    } catch (err) {
      console.error('[EditSignUpList] Failed to add item:', err);
      setSubmitError(err instanceof Error ? err.message : 'Failed to add item');
    }
  };

  // Handle add suggested item - Phase 6A.121: Supports both quantity-based and slot-based
  const handleAddSuggestedItem = async () => {
    if (isSuggestedUnsaved) {
      setSubmitError('Please save the list details first (click "Save List Details" above) before adding items to the Suggested category.');
      return;
    }
    if (!newSuggestedDesc.trim()) {
      setSubmitError('Item description is required');
      return;
    }
    if (newSuggestedItemType === SignUpItemType.Quantity && newSuggestedQty < 1) {
      setSubmitError('Quantity must be at least 1');
      return;
    }
    if (newSuggestedItemType === SignUpItemType.Slot && newSuggestedSlots < 1) {
      setSubmitError('Number of slots must be at least 1');
      return;
    }

    try {
      setSubmitError(null);
      await addSignUpItemMutation.mutateAsync({
        eventId,
        signupId,
        itemDescription: newSuggestedDesc.trim(),
        itemType: newSuggestedItemType,
        targetQuantity: newSuggestedItemType === SignUpItemType.Quantity ? newSuggestedQty : undefined,
        availableSlots: newSuggestedItemType === SignUpItemType.Slot ? newSuggestedSlots : undefined,
        suggestedPerSlot: newSuggestedItemType === SignUpItemType.Slot ? newSuggestedSuggestedPerSlot : undefined,
        itemCategory: SignUpItemCategory.Suggested,
        notes: newSuggestedNotes.trim() || undefined,
      });
      setNewSuggestedDesc('');
      setNewSuggestedQty(1);
      setNewSuggestedSlots(1);
      setNewSuggestedSuggestedPerSlot(undefined);
      setNewSuggestedNotes('');
      setNewSuggestedItemType(SignUpItemType.Quantity);
    } catch (err) {
      console.error('[EditSignUpList] Failed to add item:', err);
      setSubmitError(err instanceof Error ? err.message : 'Failed to add item');
    }
  };

  // Handle delete item
  const handleDeleteItem = async (itemId: string) => {
    try {
      setSubmitError(null);
      await removeSignUpItemMutation.mutateAsync({ eventId, signupId, itemId });
    } catch (err) {
      console.error('[EditSignUpList] Failed to delete item:', err);
      setSubmitError(err instanceof Error ? err.message : 'Failed to delete item');
    }
  };

  // Handle start editing item (inline edit)
  // Phase 6A.131: Read the type-specific size field (targetQuantity vs totalSlots) from the
  // discriminated DTO. Previously read `item.quantity` which doesn't exist on the Phase 6A.121
  // shape, silently defaulting the input to 1 and overwriting the user's real value on save.
  const handleStartEditingItem = (item: SignUpItemDto) => {
    setEditingItemId(item.id);
    setEditingItemDesc(item.itemDescription);
    setEditingItemQty(isQuantityBased(item) ? item.targetQuantity : item.totalSlots);
    setEditingItemNotes(item.notes || '');
    setEditingItemType(item.itemType);
  };

  // Handle cancel editing
  const handleCancelEditingItem = () => {
    setEditingItemId(null);
    setEditingItemDesc('');
    setEditingItemQty(1);
    setEditingItemNotes('');
    setEditingItemType(SignUpItemType.Quantity);
  };

  /**
   * Handle save edited item.
   * Phase 6A.14: Edit Sign-Up Item feature.
   * Phase 6A.131: Dispatches the type-specific field (targetQuantity vs availableSlots). The
   * backend is authoritative on type — if somehow our cached itemType drifts, the server returns
   * 400 with an explicit message which we surface to the user.
   *
   * NOTE: The inline edit does not surface SuggestedPerSlot today — the handler preserves the
   * stored value when it's omitted from the payload. Full slot-metadata editing is tracked as a
   * follow-up enhancement.
   */
  const handleSaveEditedItem = async () => {
    if (!editingItemId) return;

    const isSlot = editingItemType === SignUpItemType.Slot;
    try {
      await updateSignUpItemMutation.mutateAsync({
        eventId,
        signupId,
        itemId: editingItemId,
        itemDescription: editingItemDesc.trim(),
        targetQuantity: isSlot ? null : editingItemQty,
        availableSlots: isSlot ? editingItemQty : null,
        notes: editingItemNotes.trim() || null,
      });

      handleCancelEditingItem();
    } catch (error) {
      const err = error as { response?: { data?: { detail?: string; message?: string } }; message?: string };
      const message =
        err?.response?.data?.detail ??
        err?.response?.data?.message ??
        err?.message ??
        'Failed to update item. Please try again.';
      console.error('Failed to update sign-up item', { itemId: editingItemId, error });
      alert(message);
    }
  };

  // Loading states
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

  if (eventLoading || signUpsLoading || !event || !signUpList) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <LankaEventsHeader />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <div className="text-center">
            <p className="text-neutral-500">Loading...</p>
          </div>
        </div>
        <Footer />
      </div>
    );
  }

  // Authorization check
  if (event.isCurrentUserOrganizer !== true && user.role !== UserRole.Admin && user.role !== UserRole.AdminManager) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <LankaEventsHeader />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          <div className="text-center">
            <p className="text-destructive">You are not authorized to edit this sign-up list</p>
          </div>
        </div>
        <Footer />
      </div>
    );
  }

  // Get items by category (with defensive checks)
  const mandatoryItems = signUpList.items?.filter(item => item.itemCategory === SignUpItemCategory.Mandatory) || [];
  const preferredItems = signUpList.items?.filter(item => item.itemCategory === SignUpItemCategory.Preferred) || [];
  const suggestedItems = signUpList.items?.filter(item => item.itemCategory === SignUpItemCategory.Suggested) || [];

  // Debug: Log filtered items
  console.log('[EditSignUpList] Filtered items:', {
    mandatoryItems: mandatoryItems.length,
    preferredItems: preferredItems.length,
    suggestedItems: suggestedItems.length
  });

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <LankaEventsHeader />

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
            Back to Manage Sign-Ups
          </Button>

          <h1 className="text-3xl font-bold text-white mb-2">
            Edit Sign-Up List
          </h1>
          <p className="text-lg text-white/90">
            {event.title}
          </p>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <Card className="mb-6">
          <CardHeader>
            <CardTitle style={{ color: '#8B1538' }}>Sign-Up List Details</CardTitle>
            <CardDescription>
              Edit the category, description, and category flags for this sign-up list
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {/* Sign-up list name */}
            <div>
              <label htmlFor="category" className="block text-sm font-medium text-neutral-700 mb-2">
                Sign-up list name *
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

            {/* Save Button - Only show when changes are made */}
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
                    {mandatoryItems.length > 0 && (
                      <p className="text-xs text-neutral-400 mt-1">
                        {mandatoryItems.length} item(s) in this category
                      </p>
                    )}
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
                        {/* Phase 6A.121: Item type selection */}
                        <div>
                          <label className="block text-xs font-medium text-neutral-600 mb-1">
                            Item Type *
                          </label>
                          <div className="flex gap-4">
                            <label className="flex items-center gap-2 cursor-pointer text-sm">
                              <input
                                type="radio"
                                checked={newMandatoryItemType === SignUpItemType.Quantity}
                                onChange={() => setNewMandatoryItemType(SignUpItemType.Quantity)}
                                className="w-4 h-4"
                              />
                              Quantity (e.g., &quot;10 plates&quot;)
                            </label>
                            <label className="flex items-center gap-2 cursor-pointer text-sm">
                              <input
                                type="radio"
                                checked={newMandatoryItemType === SignUpItemType.Slot}
                                onChange={() => setNewMandatoryItemType(SignUpItemType.Slot)}
                                className="w-4 h-4"
                              />
                              Slot (e.g., &quot;3 people&quot;)
                            </label>
                          </div>
                        </div>
                        {newMandatoryItemType === SignUpItemType.Quantity ? (
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
                        ) : (
                          <>
                            <div>
                              <label className="block text-xs font-medium text-neutral-600 mb-1">
                                Number of Slots *
                              </label>
                              <Input
                                type="number"
                                min="1"
                                max="100"
                                value={newMandatorySlots}
                                onChange={(e) => setNewMandatorySlots(parseInt(e.target.value) || 1)}
                              />
                            </div>
                            <div>
                              <label className="block text-xs font-medium text-neutral-600 mb-1">
                                Suggested per slot (optional)
                              </label>
                              <Input
                                type="number"
                                min="1"
                                placeholder="e.g., 5"
                                value={newMandatorySuggestedPerSlot ?? ''}
                                onChange={(e) => setNewMandatorySuggestedPerSlot(e.target.value ? parseInt(e.target.value) : undefined)}
                              />
                            </div>
                          </>
                        )}
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
                        {isMandatoryUnsaved && (
                          <div className="p-3 bg-amber-50 border border-amber-300 rounded-lg text-sm text-amber-800">
                            Save the list details above before adding items to this category.
                          </div>
                        )}
                        <Button
                          type="button"
                          onClick={handleAddMandatoryItem}
                          disabled={addSignUpItemMutation.isPending || isMandatoryUnsaved}
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
                                <th className="px-3 py-2 text-left text-xs font-medium text-neutral-600">Qty/Slots</th>
                                <th className="px-3 py-2 text-left text-xs font-medium text-neutral-600">Rmn</th>
                                <th className="px-3 py-2 text-center text-xs font-medium text-neutral-600">Action</th>
                              </tr>
                            </thead>
                            <tbody>
                              {mandatoryItems.map((item) => (
                                <tr key={item.id} className="border-t">
                                  {editingItemId === item.id ? (
                                    <>
                                      <td className="px-3 py-2">
                                        <Input
                                          value={editingItemDesc}
                                          onChange={(e) => setEditingItemDesc(e.target.value)}
                                          placeholder="Item description"
                                          className="text-sm"
                                        />
                                      </td>
                                      <td className="px-3 py-2">
                                        <Input
                                          type="number"
                                          min="1"
                                          value={editingItemQty}
                                          onChange={(e) => setEditingItemQty(parseInt(e.target.value) || 1)}
                                          className="w-16 text-sm"
                                        />
                                      </td>
                                      <td className="px-3 py-2 text-sm">-</td>
                                      <td className="px-3 py-2">
                                        <div className="flex gap-1 justify-center">
                                          <Button
                                            variant="outline"
                                            size="sm"
                                            onClick={handleSaveEditedItem}
                                          >
                                            <Check className="h-3 w-3" />
                                          </Button>
                                          <Button
                                            variant="outline"
                                            size="sm"
                                            onClick={handleCancelEditingItem}
                                          >
                                            <X className="h-3 w-3" />
                                          </Button>
                                        </div>
                                      </td>
                                    </>
                                  ) : (
                                    <>
                                      <td className="px-3 py-2 text-sm">
                                        <div>
                                          <p className="font-medium">{item.itemDescription}</p>
                                          {item.notes && (
                                            <p className="text-xs text-neutral-500">{item.notes}</p>
                                          )}
                                        </div>
                                      </td>
                                      <td className="px-3 py-2 text-sm">{isQuantityBased(item) ? item.targetQuantity : item.totalSlots}</td>
                                      <td className="px-3 py-2 text-sm">
                                        <span className={(isQuantityBased(item) ? item.remainingQuantity : item.remainingSlots) === 0 ? 'text-red-600' : 'text-green-600'}>
                                          {isQuantityBased(item) ? item.remainingQuantity : item.remainingSlots}
                                        </span>
                                      </td>
                                      <td className="px-3 py-2">
                                        <div className="flex gap-1 justify-center">
                                          <Button
                                            variant="outline"
                                            size="sm"
                                            onClick={() => handleStartEditingItem(item)}
                                            title="Edit item"
                                          >
                                            <Edit2 className="h-3 w-3" />
                                          </Button>
                                          <Button
                                            variant="outline"
                                            size="sm"
                                            onClick={() => handleDeleteItem(item.id)}
                                            disabled={(isQuantityBased(item) ? item.committedQuantity : item.filledSlots) > 0}
                                            title={(isQuantityBased(item) ? item.committedQuantity : item.filledSlots) > 0 ? 'Cannot delete item with commitments' : 'Delete item'}
                                            className="text-red-600"
                                          >
                                            <Trash2 className="h-3 w-3" />
                                          </Button>
                                        </div>
                                      </td>
                                    </>
                                  )}
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

              {/* Phase 6A.28: Preferred Items DEPRECATED - Hidden from UI */}
              {/* The "Preferred" category has been deprecated in favor of "Suggested" and "Open" categories. */}

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
                    {suggestedItems.length > 0 && (
                      <p className="text-xs text-neutral-400 mt-1">
                        {suggestedItems.length} item(s) in this category
                      </p>
                    )}
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
                        {/* Phase 6A.121: Item type selection for Suggested */}
                        <div>
                          <label className="block text-xs font-medium text-neutral-600 mb-1">
                            Item Type *
                          </label>
                          <div className="flex gap-4">
                            <label className="flex items-center gap-2 cursor-pointer text-sm">
                              <input
                                type="radio"
                                checked={newSuggestedItemType === SignUpItemType.Quantity}
                                onChange={() => setNewSuggestedItemType(SignUpItemType.Quantity)}
                                className="w-4 h-4"
                              />
                              Quantity
                            </label>
                            <label className="flex items-center gap-2 cursor-pointer text-sm">
                              <input
                                type="radio"
                                checked={newSuggestedItemType === SignUpItemType.Slot}
                                onChange={() => setNewSuggestedItemType(SignUpItemType.Slot)}
                                className="w-4 h-4"
                              />
                              Slot-based
                            </label>
                          </div>
                        </div>
                        {newSuggestedItemType === SignUpItemType.Quantity ? (
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
                        ) : (
                          <>
                            <div>
                              <label className="block text-xs font-medium text-neutral-600 mb-1">
                                Number of Slots *
                              </label>
                              <Input
                                type="number"
                                min="1"
                                max="100"
                                value={newSuggestedSlots}
                                onChange={(e) => setNewSuggestedSlots(parseInt(e.target.value) || 1)}
                              />
                            </div>
                            <div>
                              <label className="block text-xs font-medium text-neutral-600 mb-1">
                                Suggested per slot (optional)
                              </label>
                              <Input
                                type="number"
                                min="1"
                                placeholder="e.g., 5"
                                value={newSuggestedSuggestedPerSlot ?? ''}
                                onChange={(e) => setNewSuggestedSuggestedPerSlot(e.target.value ? parseInt(e.target.value) : undefined)}
                              />
                            </div>
                          </>
                        )}
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
                        {isSuggestedUnsaved && (
                          <div className="p-3 bg-amber-50 border border-amber-300 rounded-lg text-sm text-amber-800">
                            Save the list details above before adding items to this category.
                          </div>
                        )}
                        <Button
                          type="button"
                          onClick={handleAddSuggestedItem}
                          disabled={addSignUpItemMutation.isPending || isSuggestedUnsaved}
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
                                <th className="px-3 py-2 text-left text-xs font-medium text-neutral-600">Qty/Slots</th>
                                <th className="px-3 py-2 text-left text-xs font-medium text-neutral-600">Rmn</th>
                                <th className="px-3 py-2 text-center text-xs font-medium text-neutral-600">Action</th>
                              </tr>
                            </thead>
                            <tbody>
                              {suggestedItems.map((item) => (
                                <tr key={item.id} className="border-t">
                                  {editingItemId === item.id ? (
                                    <>
                                      <td className="px-3 py-2">
                                        <Input
                                          value={editingItemDesc}
                                          onChange={(e) => setEditingItemDesc(e.target.value)}
                                          placeholder="Item description"
                                          className="text-sm"
                                        />
                                      </td>
                                      <td className="px-3 py-2">
                                        <Input
                                          type="number"
                                          min="1"
                                          value={editingItemQty}
                                          onChange={(e) => setEditingItemQty(parseInt(e.target.value) || 1)}
                                          className="w-16 text-sm"
                                        />
                                      </td>
                                      <td className="px-3 py-2 text-sm">-</td>
                                      <td className="px-3 py-2">
                                        <div className="flex gap-1 justify-center">
                                          <Button
                                            variant="outline"
                                            size="sm"
                                            onClick={handleSaveEditedItem}
                                          >
                                            <Check className="h-3 w-3" />
                                          </Button>
                                          <Button
                                            variant="outline"
                                            size="sm"
                                            onClick={handleCancelEditingItem}
                                          >
                                            <X className="h-3 w-3" />
                                          </Button>
                                        </div>
                                      </td>
                                    </>
                                  ) : (
                                    <>
                                      <td className="px-3 py-2 text-sm">
                                        <div>
                                          <p className="font-medium">{item.itemDescription}</p>
                                          {item.notes && (
                                            <p className="text-xs text-neutral-500">{item.notes}</p>
                                          )}
                                        </div>
                                      </td>
                                      <td className="px-3 py-2 text-sm">{isQuantityBased(item) ? item.targetQuantity : item.totalSlots}</td>
                                      <td className="px-3 py-2 text-sm">
                                        <span className={(isQuantityBased(item) ? item.remainingQuantity : item.remainingSlots) === 0 ? 'text-red-600' : 'text-green-600'}>
                                          {isQuantityBased(item) ? item.remainingQuantity : item.remainingSlots}
                                        </span>
                                      </td>
                                      <td className="px-3 py-2">
                                        <div className="flex gap-1 justify-center">
                                          <Button
                                            variant="outline"
                                            size="sm"
                                            onClick={() => handleStartEditingItem(item)}
                                            title="Edit item"
                                          >
                                            <Edit2 className="h-3 w-3" />
                                          </Button>
                                          <Button
                                            variant="outline"
                                            size="sm"
                                            onClick={() => handleDeleteItem(item.id)}
                                            disabled={(isQuantityBased(item) ? item.committedQuantity : item.filledSlots) > 0}
                                            title={(isQuantityBased(item) ? item.committedQuantity : item.filledSlots) > 0 ? 'Cannot delete item with commitments' : 'Delete item'}
                                            className="text-red-600"
                                          >
                                            <Trash2 className="h-3 w-3" />
                                          </Button>
                                        </div>
                                      </td>
                                    </>
                                  )}
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

              {/* Phase 6A.28: Open Items Checkbox + Section */}
              <div className="space-y-3">
                <label className="flex items-center gap-3 p-3 border rounded-lg cursor-pointer hover:bg-neutral-50">
                  <input
                    type="checkbox"
                    checked={hasOpenItems}
                    onChange={(e) => setHasOpenItems(e.target.checked)}
                    className="w-4 h-4 text-purple-600"
                  />
                  <div>
                    <p className="font-medium text-neutral-900">Open Items (Bring Your Own)</p>
                    <p className="text-sm text-neutral-500">Allow attendees to sign up with their own items. Users can add custom items they'll bring.</p>
                  </div>
                </label>

                {hasOpenItems && (
                  <div className="ml-7 p-4 bg-purple-50 rounded-lg border border-purple-100">
                    <span className="px-2 py-1 rounded text-xs font-medium bg-purple-100 text-purple-800">
                      Open Items
                    </span>
                    <p className="text-sm text-neutral-600 mt-2">
                      <strong>How it works:</strong> When this is enabled, attendees can click "Sign Up" to add their own items
                      (e.g., "Homemade Cookies - 24 pieces"). Each user manages their own items and can update or cancel them.
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

            {/* Save Button - Only show when changes are made */}
            {hasChanges && (
              <div className="flex justify-end mt-6">
                <Button
                  onClick={handleSaveListDetails}
                  disabled={updateSignUpListMutation.isPending}
                  style={{ background: '#FF7900' }}
                  className="px-8 py-3"
                >
                  <Save className="h-5 w-5 mr-2" />
                  {updateSignUpListMutation.isPending ? 'Saving...' : 'Save List Details'}
                </Button>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      <Footer />
    </div>
  );
}
