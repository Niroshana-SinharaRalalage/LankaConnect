'use client';

import * as React from 'react';
import { X, Mail, AlertCircle, Check } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { useCreateEmailGroup, useUpdateEmailGroup } from '@/presentation/hooks/useEmailGroups';
import {
  validateEmailAddresses,
  parseEmailAddresses,
} from '@/infrastructure/api/types/email-groups.types';
import type { EmailGroupDto } from '@/infrastructure/api/types/email-groups.types';

/**
 * Phase 6A.25: Email Group Modal Component
 * Modal for creating and editing email groups
 *
 * Features:
 * - Create new email groups
 * - Edit existing email groups
 * - Real-time email validation
 * - Email count display
 * - Loading and error states
 */

interface EmailGroupModalProps {
  isOpen: boolean;
  onClose: () => void;
  emailGroup?: EmailGroupDto; // If provided, modal is in edit mode
}

export function EmailGroupModal({ isOpen, onClose, emailGroup }: EmailGroupModalProps) {
  const [name, setName] = React.useState('');
  const [description, setDescription] = React.useState('');
  const [emailAddresses, setEmailAddresses] = React.useState('');
  const [validationResult, setValidationResult] = React.useState<{
    valid: string[];
    invalid: string[];
    duplicates: string[];
    uniqueCount: number;
    allValid: boolean;
  } | null>(null);
  const [showSuccess, setShowSuccess] = React.useState(false);

  const createEmailGroup = useCreateEmailGroup();
  const updateEmailGroup = useUpdateEmailGroup();

  const isEditMode = !!emailGroup;
  const isPending = createEmailGroup.isPending || updateEmailGroup.isPending;
  const error = createEmailGroup.error || updateEmailGroup.error;

  // Initialize form when modal opens or emailGroup changes
  React.useEffect(() => {
    if (isOpen) {
      if (emailGroup) {
        setName(emailGroup.name);
        setDescription(emailGroup.description || '');
        setEmailAddresses(emailGroup.emailAddresses);
        setValidationResult(validateEmailAddresses(emailGroup.emailAddresses));
      } else {
        setName('');
        setDescription('');
        setEmailAddresses('');
        setValidationResult(null);
      }
      setShowSuccess(false);
      createEmailGroup.reset();
      updateEmailGroup.reset();
    }
  }, [isOpen, emailGroup]);

  // Validate emails as user types
  React.useEffect(() => {
    if (emailAddresses.trim()) {
      const result = validateEmailAddresses(emailAddresses);
      setValidationResult(result);
    } else {
      setValidationResult(null);
    }
  }, [emailAddresses]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!name.trim() || !emailAddresses.trim()) {
      return;
    }

    if (!validationResult?.allValid) {
      return;
    }

    try {
      if (isEditMode && emailGroup) {
        await updateEmailGroup.mutateAsync({
          id: emailGroup.id,
          request: {
            name: name.trim(),
            description: description.trim() || undefined,
            emailAddresses: emailAddresses.trim(),
          },
        });
      } else {
        await createEmailGroup.mutateAsync({
          name: name.trim(),
          description: description.trim() || undefined,
          emailAddresses: emailAddresses.trim(),
        });
      }
      setShowSuccess(true);
      setTimeout(() => {
        onClose();
      }, 1500);
    } catch (err) {
      console.error('Failed to save email group:', err);
    }
  };

  if (!isOpen) return null;

  if (showSuccess) {
    return (
      <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
        <div className="bg-white rounded-xl p-8 max-w-md w-full text-center">
          <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <Check className="w-8 h-8 text-green-600" />
          </div>
          <h3 className="text-2xl font-bold text-[#8B1538] mb-2">
            {isEditMode ? 'Email Group Updated!' : 'Email Group Created!'}
          </h3>
          <p className="text-gray-600">
            {isEditMode
              ? 'Your email group has been successfully updated.'
              : 'Your new email group has been created successfully.'}
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div
          className="p-6 flex items-center justify-between"
          style={{ background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)' }}
        >
          <div className="flex items-center gap-3">
            <Mail className="w-6 h-6 text-white" />
            <h2 className="text-2xl font-bold text-white">
              {isEditMode ? 'Edit Email Group' : 'Create Email Group'}
            </h2>
          </div>
          <button
            onClick={onClose}
            className="text-white hover:bg-white/20 rounded-lg p-2 transition-colors"
            disabled={isPending}
          >
            <X className="w-6 h-6" />
          </button>
        </div>

        {/* Content */}
        <div className="p-6">
          <form onSubmit={handleSubmit}>
            {/* Name Field */}
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Group Name <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF7900] focus:border-transparent transition-all"
                placeholder="e.g., Marketing Team, Event Volunteers"
                disabled={isPending}
                required
                maxLength={200}
              />
            </div>

            {/* Description Field */}
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Description <span className="text-gray-400">(optional)</span>
              </label>
              <input
                type="text"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF7900] focus:border-transparent transition-all"
                placeholder="Brief description of this email group"
                disabled={isPending}
                maxLength={500}
              />
            </div>

            {/* Email Addresses Field */}
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Email Addresses <span className="text-red-500">*</span>
              </label>
              <textarea
                value={emailAddresses}
                onChange={(e) => setEmailAddresses(e.target.value)}
                className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF7900] focus:border-transparent transition-all resize-none font-mono text-sm"
                rows={5}
                placeholder="Enter email addresses separated by commas:&#10;john@example.com, jane@example.com, bob@example.com"
                disabled={isPending}
                required
              />

              {/* Email validation feedback */}
              {validationResult && (
                <div className="mt-2 space-y-2">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="text-sm text-gray-600">
                      {validationResult.uniqueCount} unique email{validationResult.uniqueCount !== 1 ? 's' : ''}
                    </span>
                    {validationResult.duplicates.length > 0 && (
                      <span className="text-sm text-amber-600">
                        | {validationResult.duplicates.length} duplicate{validationResult.duplicates.length !== 1 ? 's' : ''} removed
                      </span>
                    )}
                    {validationResult.invalid.length > 0 && (
                      <span className="text-sm text-red-600">
                        | {validationResult.invalid.length} invalid
                      </span>
                    )}
                  </div>

                  {/* Show duplicate emails warning */}
                  {validationResult.duplicates.length > 0 && (
                    <div className="p-3 bg-amber-50 border border-amber-200 rounded-lg">
                      <div className="flex items-start gap-2">
                        <AlertCircle className="w-4 h-4 text-amber-500 flex-shrink-0 mt-0.5" />
                        <div>
                          <p className="text-sm text-amber-700 font-medium">Duplicate email addresses found:</p>
                          <p className="text-sm text-amber-600 mt-1">
                            {validationResult.duplicates.join(', ')}
                          </p>
                          <p className="text-xs text-amber-500 mt-1">
                            Please remove duplicates before saving.
                          </p>
                        </div>
                      </div>
                    </div>
                  )}

                  {/* Show invalid emails */}
                  {validationResult.invalid.length > 0 && (
                    <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
                      <div className="flex items-start gap-2">
                        <AlertCircle className="w-4 h-4 text-red-500 flex-shrink-0 mt-0.5" />
                        <div>
                          <p className="text-sm text-red-700 font-medium">Invalid email addresses:</p>
                          <p className="text-sm text-red-600 mt-1">
                            {validationResult.invalid.join(', ')}
                          </p>
                        </div>
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* API Error Message */}
            {error && (
              <div className="mb-4 p-4 bg-red-50 border border-red-200 rounded-lg">
                <p className="text-sm text-red-600">
                  {error instanceof Error
                    ? error.message
                    : 'Failed to save email group. Please try again.'}
                </p>
              </div>
            )}

            {/* Action Buttons */}
            <div className="flex gap-3 pt-4">
              <Button
                type="button"
                onClick={onClose}
                variant="outline"
                className="flex-1"
                disabled={isPending}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                className="flex-1"
                style={{
                  background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)',
                  color: 'white',
                }}
                disabled={
                  isPending ||
                  !name.trim() ||
                  !emailAddresses.trim() ||
                  (validationResult ? !validationResult.allValid : true)
                }
              >
                {isPending
                  ? 'Saving...'
                  : isEditMode
                  ? 'Update Email Group'
                  : 'Create Email Group'}
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
