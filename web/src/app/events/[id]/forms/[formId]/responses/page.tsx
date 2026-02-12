'use client';

/**
 * Form Responses Viewer Page
 *
 * Organizer dashboard for viewing, exporting, and managing form responses.
 * Displays paginated table with response data and actions.
 *
 * Custom Forms Feature - Phase 8: Response Management
 */

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, Download, Trash2, ChevronDown, ChevronUp, Users } from 'lucide-react';
import toast from 'react-hot-toast';

import { useFormResponses, useDeleteFormResponse, useEventFormDetail } from '@/presentation/hooks/useEventForms';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { ConfirmDialog } from '@/presentation/components/ui/ConfirmDialog';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type { FormResponseDto, FormAnswerDto } from '@/infrastructure/api/types/events.types';

interface ResponsesPageProps {
  params: Promise<{ id: string; formId: string }>;
}

export default function ResponsesPage({ params }: ResponsesPageProps) {
  const router = useRouter();

  const [eventId, setEventId] = useState<string>('');
  const [formId, setFormId] = useState<string>('');
  const [currentPage, setCurrentPage] = useState(1);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [responseToDelete, setResponseToDelete] = useState<string | null>(null);
  const [expandedRows, setExpandedRows] = useState<Set<string>>(new Set());
  const [isExporting, setIsExporting] = useState(false);

  const pageSize = 20;

  // Unwrap params
  useEffect(() => {
    params.then(({ id, formId: fId }) => {
      setEventId(id);
      setFormId(fId);
    });
  }, [params]);

  // Fetch form detail (for title and questions)
  const { data: form } = useEventFormDetail(eventId, formId, {
    enabled: !!eventId && !!formId,
  });

  // Fetch responses
  const { data: responsesData, isLoading, error } = useFormResponses(
    eventId,
    formId,
    currentPage,
    pageSize,
    { enabled: !!eventId && !!formId }
  );

  // Delete mutation
  const deleteMutation = useDeleteFormResponse({
    onSuccess: () => {
      toast.success('Response deleted successfully');
      setDeleteConfirmOpen(false);
      setResponseToDelete(null);
    },
    onError: (error) => {
      toast.error(error.message || 'Failed to delete response');
    },
  });

  const handleDeleteClick = (responseId: string) => {
    setResponseToDelete(responseId);
    setDeleteConfirmOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (!responseToDelete || !eventId || !formId) return;
    await deleteMutation.mutateAsync({ eventId, formId, responseId: responseToDelete });
  };

  const toggleRowExpansion = (responseId: string) => {
    setExpandedRows((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(responseId)) {
        newSet.delete(responseId);
      } else {
        newSet.add(responseId);
      }
      return newSet;
    });
  };

  const handleExport = async (format: 'csv' | 'excel') => {
    if (!eventId || !formId) return;

    setIsExporting(true);
    try {
      const blob = await eventsRepository.exportFormResponses(eventId, formId, format);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${form?.title || 'form'}_responses.${format === 'csv' ? 'csv' : 'xlsx'}`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
      toast.success(`Responses exported as ${format.toUpperCase()}`);
    } catch (error) {
      toast.error('Failed to export responses');
      console.error('Export error:', error);
    } finally {
      setIsExporting(false);
    }
  };

  // Get answer for a specific question
  const getAnswerForQuestion = (response: FormResponseDto, questionId: string): FormAnswerDto | undefined => {
    return response.answers.find((a) => a.formQuestionId === questionId);
  };

  // Format answer value for display
  const formatAnswerValue = (answer: FormAnswerDto | undefined): string => {
    if (!answer) return '-';

    if (answer.booleanValue !== null && answer.booleanValue !== undefined) {
      return answer.booleanValue ? 'Yes' : 'No';
    }

    if (answer.selectedOptionTextSnapshots && answer.selectedOptionTextSnapshots.length > 0) {
      return answer.selectedOptionTextSnapshots.join(', ');
    }

    if (answer.textValue) {
      return answer.textValue;
    }

    return '-';
  };

  // Loading state
  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 py-8 px-4">
        <div className="max-w-7xl mx-auto">
          <Card>
            <CardContent className="py-12">
              <div className="flex items-center justify-center">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary" />
                <span className="ml-3 text-gray-600">Loading responses...</span>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div className="min-h-screen bg-gray-50 py-8 px-4">
        <div className="max-w-7xl mx-auto">
          <Card>
            <CardContent className="py-12">
              <div className="text-center">
                <p className="text-red-600 mb-4">Failed to load responses</p>
                <p className="text-sm text-gray-500 mb-6">{error.message}</p>
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

  const responses = responsesData?.responses || [];
  const totalCount = responsesData?.totalCount || 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="min-h-screen bg-gray-50 py-8 px-4">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="mb-6">
          <Button
            variant="ghost"
            onClick={() => router.push(`/events/${eventId}/manage?tab=forms`)}
            className="mb-4"
          >
            <ArrowLeft className="w-4 h-4 mr-2" />
            Back to Forms
          </Button>
        </div>

        {/* Main Card */}
        <Card>
          <CardHeader>
            <div className="flex items-start justify-between">
              <div>
                <CardTitle>{form?.title} - Responses</CardTitle>
                <CardDescription className="mt-2">
                  {totalCount} response{totalCount !== 1 ? 's' : ''} submitted
                </CardDescription>
              </div>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handleExport('csv')}
                  disabled={isExporting || totalCount === 0}
                >
                  <Download className="w-4 h-4 mr-2" />
                  {isExporting ? 'Exporting...' : 'Export CSV'}
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handleExport('excel')}
                  disabled={isExporting || totalCount === 0}
                >
                  <Download className="w-4 h-4 mr-2" />
                  {isExporting ? 'Exporting...' : 'Export Excel'}
                </Button>
              </div>
            </div>
          </CardHeader>

          <CardContent>
            {/* Empty State */}
            {responses.length === 0 ? (
              <div className="text-center py-12">
                <Users className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">No responses yet</h3>
                <p className="text-sm text-gray-500">
                  Responses will appear here once attendees fill out the form.
                </p>
              </div>
            ) : (
              <>
                {/* Responses Table */}
                <div className="overflow-x-auto">
                  <table className="w-full border-collapse">
                    <thead>
                      <tr className="border-b bg-gray-50">
                        <th className="text-left p-3 text-sm font-medium text-gray-700">Expand</th>
                        <th className="text-left p-3 text-sm font-medium text-gray-700">Name</th>
                        <th className="text-left p-3 text-sm font-medium text-gray-700">Email</th>
                        <th className="text-left p-3 text-sm font-medium text-gray-700">Submitted</th>
                        {form?.questions?.slice(0, 3).map((question) => (
                          <th key={question.id} className="text-left p-3 text-sm font-medium text-gray-700">
                            {question.questionText.length > 30
                              ? question.questionText.substring(0, 30) + '...'
                              : question.questionText}
                          </th>
                        ))}
                        <th className="text-right p-3 text-sm font-medium text-gray-700">Actions</th>
                      </tr>
                    </thead>
                    <tbody>
                      {responses.map((response) => (
                        <>
                          {/* Main Row */}
                          <tr key={response.id} className="border-b hover:bg-gray-50">
                            <td className="p-3">
                              <button
                                onClick={() => toggleRowExpansion(response.id)}
                                className="text-gray-600 hover:text-gray-900"
                              >
                                {expandedRows.has(response.id) ? (
                                  <ChevronUp className="w-4 h-4" />
                                ) : (
                                  <ChevronDown className="w-4 h-4" />
                                )}
                              </button>
                            </td>
                            <td className="p-3 text-sm text-gray-900">
                              {response.respondentName || '-'}
                            </td>
                            <td className="p-3 text-sm text-gray-600">
                              {response.respondentEmail || '-'}
                            </td>
                            <td className="p-3 text-sm text-gray-600">
                              {new Date(response.submittedAt).toLocaleDateString()}
                            </td>
                            {form?.questions?.slice(0, 3).map((question) => {
                              const answer = getAnswerForQuestion(response, question.id);
                              const value = formatAnswerValue(answer);
                              return (
                                <td key={question.id} className="p-3 text-sm text-gray-600">
                                  <div className="max-w-xs truncate" title={value}>
                                    {value}
                                  </div>
                                </td>
                              );
                            })}
                            <td className="p-3 text-right">
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => handleDeleteClick(response.id)}
                                disabled={deleteMutation.isPending}
                              >
                                <Trash2 className="w-4 h-4 text-red-500" />
                              </Button>
                            </td>
                          </tr>

                          {/* Expanded Row */}
                          {expandedRows.has(response.id) && (
                            <tr className="border-b bg-gray-50">
                              <td colSpan={6 + (form?.questions?.length || 0)} className="p-6">
                                <div className="space-y-4">
                                  <h4 className="font-medium text-gray-900 mb-4">All Answers</h4>
                                  {form?.questions
                                    ?.sort((a, b) => a.sortOrder - b.sortOrder)
                                    .map((question) => {
                                      const answer = getAnswerForQuestion(response, question.id);
                                      return (
                                        <div key={question.id} className="border-b pb-3">
                                          <p className="text-sm font-medium text-gray-700 mb-1">
                                            {question.questionText}
                                          </p>
                                          <p className="text-sm text-gray-900">
                                            {formatAnswerValue(answer)}
                                          </p>
                                        </div>
                                      );
                                    })}
                                </div>
                              </td>
                            </tr>
                          )}
                        </>
                      ))}
                    </tbody>
                  </table>
                </div>

                {/* Pagination */}
                {totalPages > 1 && (
                  <div className="flex items-center justify-between mt-6 pt-6 border-t">
                    <p className="text-sm text-gray-600">
                      Showing {(currentPage - 1) * pageSize + 1} to{' '}
                      {Math.min(currentPage * pageSize, totalCount)} of {totalCount} responses
                    </p>
                    <div className="flex gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                        disabled={currentPage === 1}
                      >
                        Previous
                      </Button>
                      <span className="px-4 py-2 text-sm text-gray-700">
                        Page {currentPage} of {totalPages}
                      </span>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                        disabled={currentPage === totalPages}
                      >
                        Next
                      </Button>
                    </div>
                  </div>
                )}
              </>
            )}
          </CardContent>
        </Card>

        {/* Delete Confirmation Dialog */}
        <ConfirmDialog
          open={deleteConfirmOpen}
          onOpenChange={setDeleteConfirmOpen}
          onConfirm={handleDeleteConfirm}
          title="Delete Response"
          description="Are you sure you want to delete this response? This action cannot be undone."
          confirmLabel="Delete"
          variant="danger"
          isLoading={deleteMutation.isPending}
        />
      </div>
    </div>
  );
}
