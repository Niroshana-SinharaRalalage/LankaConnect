'use client';

/**
 * Public Form View Page
 *
 * Allows attendees to view and respond to custom event forms.
 * Supports anonymous submissions with token-based editing.
 *
 * Custom Forms Feature - Phase 7: Attendee UI
 */

import { useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { ArrowLeft, CheckCircle, Trash2 } from 'lucide-react';
import toast from 'react-hot-toast';

import { useEventFormDetail, useSubmitFormResponse, useMyFormResponse, useMyFormResponseByUserId, useDeleteFormResponse } from '@/presentation/hooks/useEventForms';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { FormRenderer } from '@/presentation/components/features/events/FormRenderer';
import type { SubmitFormAnswerItem, SubmitFormResponseRequest } from '@/infrastructure/api/types/events.types';
import { useAuthStore } from '@/presentation/store/useAuthStore';

interface FormViewPageProps {
  params: Promise<{ id: string; formId: string }>;
}

export default function FormViewPage({ params }: FormViewPageProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const token = searchParams.get('token');
  const { isAuthenticated } = useAuthStore();

  const [eventId, setEventId] = useState<string>('');
  const [formId, setFormId] = useState<string>('');
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  // Phase 7.X: Auto-restore access token from localStorage for better UX
  // User shouldn't need to manually copy/paste edit links
  useEffect(() => {
    if (eventId && formId) {
      const storageKey = `form_response_token_${eventId}_${formId}`;
      const storedToken = localStorage.getItem(storageKey);

      // Priority: URL token > stored token
      if (token) {
        setAccessToken(token);
        // Also save URL token to localStorage for future visits
        localStorage.setItem(storageKey, token);
      } else if (storedToken) {
        setAccessToken(storedToken);
      }
    }
  }, [eventId, formId, token]);

  // Unwrap params
  useEffect(() => {
    params.then(({ id, formId: fId }) => {
      setEventId(id);
      setFormId(fId);
    });
  }, [params]);

  // Fetch form detail
  const { data: form, isLoading, error } = useEventFormDetail(eventId, formId, {
    enabled: !!eventId && !!formId,
  });

  // Phase 6A.106-110: Fetch existing response using TWO strategies:
  // 1. For anonymous users: Use access token (from URL or localStorage)
  // 2. For logged-in users: Use userId (no token needed)
  const { data: tokenBasedResponse } = useMyFormResponse(eventId, formId, accessToken || '', {
    enabled: !!eventId && !!formId && !!accessToken && !isAuthenticated, // Only for anonymous users with token
  });

  const { data: userBasedResponse } = useMyFormResponseByUserId(eventId, formId, {
    enabled: !!eventId && !!formId && isAuthenticated, // Only for logged-in users
  });

  // Merge responses: prioritize user-based (logged-in) over token-based (anonymous)
  const existingResponse = userBasedResponse || tokenBasedResponse;

  // Submit mutation
  const submitMutation = useSubmitFormResponse({
    onSuccess: (data) => {
      setAccessToken(data.accessToken);
      setIsSubmitted(true);

      // Phase 7.X: Save token to localStorage so user can edit later without copy/paste
      if (eventId && formId) {
        const storageKey = `form_response_token_${eventId}_${formId}`;
        localStorage.setItem(storageKey, data.accessToken);
      }

      toast.success('Response submitted successfully!');
    },
    onError: (error) => {
      toast.error(error.message || 'Failed to submit response');
    },
  });

  // Phase 6A.106: Delete mutation for canceling response
  const deleteMutation = useDeleteFormResponse({
    onSuccess: () => {
      toast.success('Response cancelled successfully');
      // Clear access token and redirect to event page
      setAccessToken(null);
      router.push(`/events/${eventId}`);
    },
    onError: (error) => {
      toast.error(error.message || 'Failed to cancel response');
    },
  });

  const handleSubmit = async (answers: SubmitFormAnswerItem[], respondentName?: string, respondentEmail?: string) => {
    if (!eventId || !formId) return;

    const request: SubmitFormResponseRequest = {
      respondentName: respondentName || null,
      respondentEmail: respondentEmail || null,
      answers,
    };

    await submitMutation.mutateAsync({
      eventId,
      formId,
      request,
    });
  };

  // Phase 6A.106: Handle response deletion with confirmation
  // Supports both logged-in users (no token) and anonymous users (with token)
  const handleDelete = async () => {
    if (!eventId || !formId || !existingResponse) return;

    // Logged-in users don't need token, anonymous users do
    if (!isAuthenticated && !accessToken) {
      toast.error('Unable to delete response. Please use the edit link from your email.');
      return;
    }

    setShowDeleteConfirm(false);

    await deleteMutation.mutateAsync({
      eventId,
      formId,
      responseId: existingResponse.id,
      accessToken: accessToken || undefined, // Only pass token for anonymous users
    });
  };

  // Loading state
  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 py-8 px-4">
        <div className="max-w-3xl mx-auto">
          <Card>
            <CardContent className="py-12">
              <div className="flex items-center justify-center">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary" />
                <span className="ml-3 text-gray-600">Loading form...</span>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  // Error state
  if (error || !form) {
    return (
      <div className="min-h-screen bg-gray-50 py-8 px-4">
        <div className="max-w-3xl mx-auto">
          <Card>
            <CardContent className="py-12">
              <div className="text-center">
                <p className="text-red-600 mb-4">Failed to load form</p>
                <p className="text-sm text-gray-500 mb-6">
                  {error?.message || 'Form not found'}
                </p>
                <Button onClick={() => router.back()}>
                  <ArrowLeft className="w-4 h-4 mr-2" />
                  Go Back
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  // Check if form is accepting responses
  const isFormActive = form.status === 'Active' || form.status === 1;
  const isDeadlinePassed = form.responseDeadline && new Date(form.responseDeadline) < new Date();
  const isMaxResponsesReached = form.maxResponses && form.responseCount >= form.maxResponses;
  const canSubmit = isFormActive && !isDeadlinePassed && !isMaxResponsesReached;

  // Success state after submission
  if (isSubmitted && accessToken) {
    const editUrl = `${window.location.origin}${window.location.pathname}?token=${accessToken}`;

    return (
      <div className="min-h-screen bg-gray-50 py-8 px-4">
        <div className="max-w-3xl mx-auto">
          <Card>
            <CardContent className="py-12">
              <div className="text-center">
                <CheckCircle className="w-16 h-16 text-green-500 mx-auto mb-4" />
                <h2 className="text-2xl font-semibold text-gray-900 mb-2">
                  Response Submitted!
                </h2>
                <p className="text-gray-600 mb-6">
                  Your response has been recorded successfully.
                </p>

                {form.responseDeadline && !isDeadlinePassed && (
                  <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
                    <p className="text-sm text-blue-800 mb-2">
                      <strong>You can edit your response until the deadline:</strong>
                    </p>
                    <p className="text-sm text-blue-600 mb-3">
                      {new Date(form.responseDeadline).toLocaleString()}
                    </p>
                    <div className="bg-white rounded border border-blue-300 p-3 mb-3">
                      <p className="text-xs text-gray-600 mb-1">Edit Link (save this):</p>
                      <input
                        type="text"
                        value={editUrl}
                        readOnly
                        className="w-full text-xs font-mono bg-gray-50 px-2 py-1 rounded border"
                        onClick={(e) => e.currentTarget.select()}
                      />
                    </div>
                    <Button
                      size="sm"
                      onClick={() => {
                        navigator.clipboard.writeText(editUrl);
                        toast.success('Edit link copied to clipboard!');
                      }}
                    >
                      Copy Edit Link
                    </Button>
                  </div>
                )}

                <Button onClick={() => router.push(`/events/${eventId}`)}>
                  Back to Event
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8 px-4">
      <div className="max-w-3xl mx-auto">
        {/* Header */}
        <div className="mb-6">
          <Button
            variant="ghost"
            onClick={() => router.push(`/events/${eventId}`)}
            className="mb-4"
          >
            <ArrowLeft className="w-4 h-4 mr-2" />
            Back to Event
          </Button>
        </div>

        {/* Form Card */}
        <Card>
          <CardHeader>
            <CardTitle>{form.title}</CardTitle>
            {form.description && (
              <CardDescription className="text-base mt-2">
                {form.description}
              </CardDescription>
            )}

            {/* Form Metadata */}
            <div className="flex flex-wrap gap-4 mt-4 pt-4 border-t text-sm text-gray-600">
              {form.responseDeadline && (
                <div>
                  <span className="font-medium">Deadline:</span>{' '}
                  {new Date(form.responseDeadline).toLocaleString()}
                </div>
              )}
              {form.maxResponses && (
                <div>
                  <span className="font-medium">Responses:</span>{' '}
                  {form.responseCount} / {form.maxResponses}
                </div>
              )}
            </div>

            {/* Status Messages */}
            {!canSubmit && (
              <div className="mt-4 p-4 bg-amber-50 border border-amber-200 rounded-lg">
                <p className="text-sm text-amber-800">
                  {!isFormActive && 'This form is not currently accepting responses.'}
                  {isDeadlinePassed && 'The response deadline has passed.'}
                  {isMaxResponsesReached && 'This form has reached the maximum number of responses.'}
                </p>
              </div>
            )}

            {existingResponse && (
              <div className="mt-4 p-4 bg-blue-50 border border-blue-200 rounded-lg">
                <div className="flex items-start justify-between">
                  <p className="text-sm text-blue-800">
                    <strong>Editing your response</strong>
                    {form.responseDeadline && !isDeadlinePassed && (
                      <span> - You can make changes until {new Date(form.responseDeadline).toLocaleString()}</span>
                    )}
                  </p>
                  {/* Phase 6A.106: Cancel/Delete Response Button */}
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setShowDeleteConfirm(true)}
                    className="text-red-600 hover:text-red-700 hover:bg-red-50 -mt-1"
                  >
                    <Trash2 className="w-4 h-4 mr-1" />
                    Cancel Response
                  </Button>
                </div>
              </div>
            )}
          </CardHeader>

          <CardContent>
            {/* Form Renderer */}
            {canSubmit ? (
              <FormRenderer
                questions={form.questions || []}
                onSubmit={handleSubmit}
                isSubmitting={submitMutation.isPending}
                existingResponse={existingResponse}
                allowMultipleResponses={form.allowMultipleResponses}
              />
            ) : (
              <div className="text-center py-8 text-gray-500">
                This form is not accepting responses at this time.
              </div>
            )}
          </CardContent>
        </Card>

        {/* Phase 6A.106: Delete Confirmation Dialog */}
        {showDeleteConfirm && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
            <Card className="max-w-md w-full">
              <CardHeader>
                <CardTitle className="flex items-center text-red-600">
                  <Trash2 className="w-5 h-5 mr-2" />
                  Cancel Response?
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-gray-700 mb-6">
                  Are you sure you want to cancel your response? This action cannot be undone.
                  You will receive a confirmation email.
                </p>
                <div className="flex gap-3 justify-end">
                  <Button
                    variant="outline"
                    onClick={() => setShowDeleteConfirm(false)}
                    disabled={deleteMutation.isPending}
                  >
                    Keep Response
                  </Button>
                  <Button
                    variant="destructive"
                    onClick={handleDelete}
                    disabled={deleteMutation.isPending}
                  >
                    {deleteMutation.isPending ? (
                      <span className="flex items-center">
                        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2" />
                        Canceling...
                      </span>
                    ) : (
                      <>
                        <Trash2 className="w-4 h-4 mr-2" />
                        Cancel Response
                      </>
                    )}
                  </Button>
                </div>
              </CardContent>
            </Card>
          </div>
        )}
      </div>
    </div>
  );
}
