'use client';

import { useState, useEffect } from 'react';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { X } from 'lucide-react';
import { useUpdateSignUpList } from '@/presentation/hooks/useEventSignUps';
import type { SignUpListDto } from '@/infrastructure/api/types/events.types';

interface EditSignUpListModalProps {
  eventId: string;
  signUpList: SignUpListDto;
  isOpen: boolean;
  onClose: () => void;
}

/**
 * EditSignUpListModal Component
 * Phase 6A.13: Edit Sign-Up List feature
 *
 * Allows event organizers to edit sign-up list details:
 * - Category
 * - Description
 * - Category flags (Mandatory, Preferred, Suggested)
 *
 * Note: Items are managed separately via existing add/remove operations
 */
export function EditSignUpListModal({ eventId, signUpList, isOpen, onClose }: EditSignUpListModalProps) {
  // Form state
  const [category, setCategory] = useState('');
  const [description, setDescription] = useState('');
  const [hasMandatoryItems, setHasMandatoryItems] = useState(false);
  const [hasPreferredItems, setHasPreferredItems] = useState(false);
  const [hasSuggestedItems, setHasSuggestedItems] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  // Mutation hook
  const updateSignUpListMutation = useUpdateSignUpList(eventId);

  // Initialize form with sign-up list data when modal opens
  useEffect(() => {
    if (isOpen && signUpList) {
      setCategory(signUpList.category);
      setDescription(signUpList.description);
      setHasMandatoryItems(signUpList.hasMandatoryItems);
      setHasPreferredItems(signUpList.hasPreferredItems);
      setHasSuggestedItems(signUpList.hasSuggestedItems);
      setSubmitError(null);
    }
  }, [isOpen, signUpList]);

  // Handle form submission
  const handleSubmit = async () => {
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

      await updateSignUpListMutation.mutateAsync({
        signupId: signUpList.id,
        category: category.trim(),
        description: description.trim(),
        hasMandatoryItems,
        hasPreferredItems,
        hasSuggestedItems,
      });

      // Close modal on success
      onClose();
    } catch (err) {
      console.error('[EditSignUpListModal] Failed to update sign-up list:', err);
      setSubmitError(err instanceof Error ? err.message : 'Failed to update sign-up list');
    }
  };

  // Handle close
  const handleClose = () => {
    setSubmitError(null);
    onClose();
  };

  // Don't render if not open
  if (!isOpen) return null;

  return (
    <>
      {/* Modal Overlay */}
      <div
        className="fixed inset-0 bg-black bg-opacity-50 z-40"
        onClick={handleClose}
      />

      {/* Modal Content */}
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
          {/* Modal Header */}
          <div className="flex items-center justify-between p-6 border-b">
            <h2 className="text-2xl font-bold" style={{ color: '#8B1538' }}>
              Edit Sign-Up List
            </h2>
            <button
              onClick={handleClose}
              className="text-neutral-400 hover:text-neutral-600 transition-colors"
            >
              <X className="h-6 w-6" />
            </button>
          </div>

          {/* Modal Body */}
          <div className="p-6 space-y-4">
            {/* Category */}
            <div>
              <label htmlFor="edit-category" className="block text-sm font-medium text-neutral-700 mb-2">
                Category *
              </label>
              <Input
                id="edit-category"
                type="text"
                placeholder="e.g., Food & Drinks, Decorations, Supplies"
                value={category}
                onChange={(e) => setCategory(e.target.value)}
              />
            </div>

            {/* Description */}
            <div>
              <label htmlFor="edit-description" className="block text-sm font-medium text-neutral-700 mb-2">
                Description *
              </label>
              <textarea
                id="edit-description"
                rows={3}
                placeholder="Describe what items are needed or provide instructions..."
                className="w-full px-4 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 resize-none"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
            </div>

            {/* Category Checkboxes */}
            <div className="space-y-4">
              <label className="block text-sm font-medium text-neutral-700 mb-3">
                Item Categories * (at least one required)
              </label>

              {/* Mandatory Items Checkbox */}
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
                  {signUpList.items.filter(item => item.itemCategory === 0).length > 0 && (
                    <p className="text-xs text-neutral-400 mt-1">
                      {signUpList.items.filter(item => item.itemCategory === 0).length} item(s) in this category
                    </p>
                  )}
                </div>
              </label>

              {/* Preferred Items Checkbox */}
              <label className="flex items-center gap-3 p-3 border rounded-lg cursor-pointer hover:bg-neutral-50">
                <input
                  type="checkbox"
                  checked={hasPreferredItems}
                  onChange={(e) => setHasPreferredItems(e.target.checked)}
                  className="w-4 h-4 text-blue-600"
                />
                <div>
                  <p className="font-medium text-neutral-900">Preferred Items</p>
                  <p className="text-sm text-neutral-500">Highly desired items that would be helpful</p>
                  {signUpList.items.filter(item => item.itemCategory === 1).length > 0 && (
                    <p className="text-xs text-neutral-400 mt-1">
                      {signUpList.items.filter(item => item.itemCategory === 1).length} item(s) in this category
                    </p>
                  )}
                </div>
              </label>

              {/* Suggested Items Checkbox */}
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
                  {signUpList.items.filter(item => item.itemCategory === 2).length > 0 && (
                    <p className="text-xs text-neutral-400 mt-1">
                      {signUpList.items.filter(item => item.itemCategory === 2).length} item(s) in this category
                    </p>
                  )}
                </div>
              </label>
            </div>

            {/* Error Message */}
            {submitError && (
              <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
                <p className="text-sm text-red-600">{submitError}</p>
              </div>
            )}
          </div>

          {/* Modal Footer */}
          <div className="flex items-center justify-end gap-3 p-6 border-t">
            <Button
              variant="outline"
              onClick={handleClose}
              disabled={updateSignUpListMutation.isPending}
            >
              Cancel
            </Button>
            <Button
              onClick={handleSubmit}
              disabled={updateSignUpListMutation.isPending}
              style={{ background: '#FF7900' }}
            >
              {updateSignUpListMutation.isPending ? 'Saving...' : 'Save Changes'}
            </Button>
          </div>
        </div>
      </div>
    </>
  );
}
