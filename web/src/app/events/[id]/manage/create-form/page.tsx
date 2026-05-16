'use client';

/**
 * Create Form Page
 *
 * Full-page form builder for creating custom event forms with multiple question types.
 * Follows signup list creation pattern with two-column layout.
 *
 * Features:
 * - Form metadata (title, description, settings)
 * - Question builder with 8 question types
 * - Drag-drop question reordering with dnd-kit
 * - Option management for choice-type questions
 * - Single transactional submission
 *
 * Custom Forms Feature - Phase 6B: Form Creation
 */

import { use, useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, Plus, Trash2, GripVertical, CheckCircle } from 'lucide-react';
import { v4 as uuidv4 } from 'uuid';
import {
  DndContext,
  closestCenter,
  PointerSensor,
  KeyboardSensor,
  useSensor,
  useSensors,
  DragEndEvent,
} from '@dnd-kit/core';
import {
  SortableContext,
  verticalListSortingStrategy,
  sortableKeyboardCoordinates,
  arrayMove,
} from '@dnd-kit/sortable';

import { LankaEventsHeader } from '@/presentation/components/layout/LankaEventsHeader';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { Badge } from '@/presentation/components/ui/Badge';
import { useEventById } from '@/presentation/hooks/useEvents';
import { useCreateEventForm } from '@/presentation/hooks/useEventForms';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { FormQuestionType } from '@/infrastructure/api/types/events.types';
import type { CreateEventFormRequest, CreateFormQuestionItem } from '@/infrastructure/api/types/events.types';
import { SortableQuestionCard } from '@/presentation/components/features/events/SortableQuestionCard';
import toast from 'react-hot-toast';

interface QuestionDraft {
  id: string; // Client-side temporary ID
  questionText: string;
  questionType: FormQuestionType;
  isRequired: boolean;
  sortOrder: number;
  helpText?: string | null;
  options?: { text: string; sortOrder: number }[];
}

export default function CreateFormPage({ params }: { params: Promise<{ id: string }> }) {
  const { id: eventId } = use(params);
  const router = useRouter();
  const { user, isAuthenticated } = useAuthStore();
  const { data: event, isLoading: eventLoading } = useEventById(eventId);

  // Form metadata state
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [allowMultipleResponses, setAllowMultipleResponses] = useState(false);
  const [responseDeadline, setResponseDeadline] = useState('');
  const [maxResponses, setMaxResponses] = useState('');
  // Phase 6A.146 — public response visibility toggle. Defaults OFF (opt-in).
  const [allowAttendeesToViewResponses, setAllowAttendeesToViewResponses] = useState(false);

  // Questions array
  const [questions, setQuestions] = useState<QuestionDraft[]>([]);

  // New question form state
  const [newQuestionText, setNewQuestionText] = useState('');
  const [newQuestionType, setNewQuestionType] = useState<FormQuestionType>(FormQuestionType.ShortText);
  const [newQuestionRequired, setNewQuestionRequired] = useState(false);
  const [newQuestionHelpText, setNewQuestionHelpText] = useState('');
  const [newQuestionOptions, setNewQuestionOptions] = useState<string[]>(['', '']);

  // UI state
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [showSuccessMessage, setShowSuccessMessage] = useState(false);
  const [createdFormTitle, setCreatedFormTitle] = useState('');

  // Create form mutation
  const createFormMutation = useCreateEventForm({
    onSuccess: (formId, { request }) => {
      setCreatedFormTitle(request.title);
      setShowSuccessMessage(true);
      setSubmitError(null);
      // Don't navigate - let user choose next action
    },
    onError: (error) => {
      setSubmitError(error.message || 'Failed to create form');
      toast.error(error.message || 'Failed to create form');
    },
  });

  // Drag-drop sensors
  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  // Authorization check
  useEffect(() => {
    if (!isAuthenticated || !user?.userId) {
      router.push(`/login?redirect=${encodeURIComponent(`/events/${eventId}/manage/create-form`)}`);
      return;
    }

    if (event && event.isCurrentUserOrganizer !== true && user.role !== 'Admin' && user.role !== 'AdminManager') {
      router.push(`/events/${eventId}`);
    }
  }, [isAuthenticated, user, event, eventId, router]);

  // Check if question type needs options
  const questionTypeNeedsOptions = (type: FormQuestionType): boolean => {
    return [
      FormQuestionType.SingleChoice,
      FormQuestionType.MultipleChoice,
      FormQuestionType.Dropdown,
    ].includes(type);
  };

  // Handle add question
  const handleAddQuestion = () => {
    // Validate question text
    if (!newQuestionText.trim()) {
      setSubmitError('Question text is required');
      return;
    }

    // Validate options for choice types
    const needsOptions = questionTypeNeedsOptions(newQuestionType);
    const validOptions = newQuestionOptions.filter(o => o.trim());

    if (needsOptions && validOptions.length < 2) {
      setSubmitError('Choice questions need at least 2 options');
      return;
    }

    // Add question to local array
    const question: QuestionDraft = {
      id: uuidv4(),
      questionText: newQuestionText.trim(),
      questionType: newQuestionType,
      isRequired: newQuestionRequired,
      sortOrder: questions.length,
      helpText: newQuestionHelpText.trim() || null,
      options: needsOptions
        ? validOptions.map((text, i) => ({ text: text.trim(), sortOrder: i }))
        : undefined,
    };

    setQuestions([...questions, question]);

    // Reset form
    setNewQuestionText('');
    setNewQuestionType(FormQuestionType.ShortText);
    setNewQuestionRequired(false);
    setNewQuestionHelpText('');
    setNewQuestionOptions(['', '']);
    setSubmitError(null);
  };

  // Handle delete question
  const handleDeleteQuestion = (id: string) => {
    const updated = questions.filter(q => q.id !== id);
    // Update sortOrder
    const reordered = updated.map((q, i) => ({ ...q, sortOrder: i }));
    setQuestions(reordered);
  };

  // Handle drag end
  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;

    const oldIndex = questions.findIndex(q => q.id === active.id);
    const newIndex = questions.findIndex(q => q.id === over.id);

    const reordered = arrayMove(questions, oldIndex, newIndex);
    // Update sortOrder
    const updated = reordered.map((q, i) => ({ ...q, sortOrder: i }));
    setQuestions(updated);
  };

  // Handle add option
  const handleAddOption = () => {
    setNewQuestionOptions([...newQuestionOptions, '']);
  };

  // Handle remove option
  const handleRemoveOption = (index: number) => {
    if (newQuestionOptions.length <= 2) return; // Keep minimum 2 options
    const updated = [...newQuestionOptions];
    updated.splice(index, 1);
    setNewQuestionOptions(updated);
  };

  // Handle option text change
  const handleOptionChange = (index: number, value: string) => {
    const updated = [...newQuestionOptions];
    updated[index] = value;
    setNewQuestionOptions(updated);
  };

  // Handle create form
  const handleCreateForm = async () => {
    // Validate form level
    if (!title.trim()) {
      setSubmitError('Form title is required');
      return;
    }

    if (questions.length === 0) {
      setSubmitError('Add at least one question');
      return;
    }

    // Build payload
    const questionsPayload: CreateFormQuestionItem[] = questions.map(q => ({
      questionText: q.questionText,
      questionType: q.questionType,
      isRequired: q.isRequired,
      sortOrder: q.sortOrder,
      helpText: q.helpText,
      options: q.options || null,
    }));

    const payload: CreateEventFormRequest = {
      title: title.trim(),
      description: description.trim() || null,
      allowMultipleResponses,
      responseDeadline: responseDeadline ? new Date(responseDeadline).toISOString() : null,
      maxResponses: maxResponses ? parseInt(maxResponses, 10) : null,
      questions: questionsPayload,
      allowAttendeesToViewResponses,  // Phase 6A.146
    };

    // Submit
    await createFormMutation.mutateAsync({ eventId, request: payload });
  };

  if (eventLoading || !event) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <LankaEventsHeader />
        <div className="flex items-center justify-center py-12">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary" />
          <span className="ml-3 text-gray-600">Loading...</span>
        </div>
        <Footer />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <LankaEventsHeader />

      {/* Page Header */}
      <div className="bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 py-8">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center gap-4">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => router.push(`/events/${eventId}/manage?tab=forms`)}
              className="text-white hover:bg-white/20"
            >
              <ArrowLeft className="w-4 h-4 mr-2" />
              Back
            </Button>
            <div>
              <h1 className="text-3xl font-bold text-white mb-2">Create Form</h1>
              <p className="text-white/90">{event.title}</p>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Success Message */}
        {showSuccessMessage && (
          <div className="mb-8 p-4 bg-green-50 border border-green-200 rounded-lg flex items-start gap-3">
            <CheckCircle className="w-5 h-5 text-green-600 flex-shrink-0 mt-0.5" />
            <div className="flex-1">
              <p className="text-sm font-medium text-green-900">
                Form created successfully
              </p>
              <p className="text-sm text-green-700 mt-1">
                "{createdFormTitle}" has been created as a draft. You can now add more forms or go to manage page to publish it.
              </p>
              <div className="mt-3 flex gap-3">
                <Button
                  size="sm"
                  onClick={() => router.push(`/events/${eventId}/manage?tab=forms`)}
                >
                  Go to Signup Forms
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => {
                    setShowSuccessMessage(false);
                    setTitle('');
                    setDescription('');
                    setAllowMultipleResponses(false);
                    setResponseDeadline('');
                    setMaxResponses('');
                    setQuestions([]);
                  }}
                >
                  Create Another Form
                </Button>
              </div>
            </div>
          </div>
        )}

        {/* Error Display */}
        {submitError && (
          <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
            <p className="text-sm text-red-600">{submitError}</p>
          </div>
        )}

        {/* Form Details Section */}
        <Card className="mb-8">
          <CardHeader>
            <CardTitle>Form Details</CardTitle>
            <CardDescription>Basic information about this form</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            {/* Title */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Form Title <span className="text-red-500">*</span>
              </label>
              <Input
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="e.g., RSVP Form, Dietary Preferences Survey"
                maxLength={200}
              />
              <p className="text-xs text-gray-500 mt-1">{title.length}/200 characters</p>
            </div>

            {/* Description */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Description (Optional)
              </label>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Provide instructions or additional context for respondents"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-transparent"
                rows={3}
                maxLength={2000}
              />
              <p className="text-xs text-gray-500 mt-1">{description.length}/2000 characters</p>
            </div>

            {/* Settings */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              {/* Allow Multiple Responses */}
              <div className="flex items-center space-x-3">
                <input
                  type="checkbox"
                  id="allowMultiple"
                  checked={allowMultipleResponses}
                  onChange={(e) => setAllowMultipleResponses(e.target.checked)}
                  className="w-4 h-4 text-primary border-gray-300 rounded focus:ring-primary"
                />
                <label htmlFor="allowMultiple" className="text-sm font-medium text-gray-700 cursor-pointer">
                  Allow Multiple Responses
                </label>
              </div>

              {/* Response Deadline */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Response Deadline (Optional)
                </label>
                <Input
                  type="datetime-local"
                  value={responseDeadline}
                  onChange={(e) => setResponseDeadline(e.target.value)}
                />
              </div>

              {/* Max Responses */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Max Responses (Optional)
                </label>
                <Input
                  type="number"
                  value={maxResponses}
                  onChange={(e) => setMaxResponses(e.target.value)}
                  placeholder="e.g., 100"
                  min="1"
                />
              </div>
            </div>

            {/* Phase 6A.146 — Response Visibility Toggle */}
            <div className="mt-4 rounded-md border border-neutral-200 bg-neutral-50 p-4 dark:border-neutral-800 dark:bg-neutral-900/40">
              <label className="flex cursor-pointer items-start gap-3">
                <input
                  type="checkbox"
                  checked={allowAttendeesToViewResponses}
                  onChange={(e) => setAllowAttendeesToViewResponses(e.target.checked)}
                  data-testid="allow-attendees-toggle"
                  className="mt-1 h-4 w-4 rounded border-neutral-300 text-orange-600 focus:ring-orange-500"
                />
                <span className="flex-1">
                  <span className="block text-sm font-medium text-neutral-900 dark:text-neutral-100">
                    Allow event visitors to see responses
                  </span>
                  <span className="mt-1 block text-xs text-neutral-600 dark:text-neutral-400">
                    When enabled, anyone viewing the event can see all responses.
                    Respondent names appear as submitted; emails and account
                    identities stay hidden. Make sure your questions don&apos;t
                    ask for personal information (phone numbers, addresses,
                    etc.) if you turn this on.
                  </span>
                </span>
              </label>
            </div>
          </CardContent>
        </Card>

        {/* Question Builder Section */}
        <Card>
          <CardHeader>
            <CardTitle>Questions</CardTitle>
            <CardDescription>Add questions to collect information from respondents</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
              {/* Left Column: Add Question Form */}
              <div className="space-y-4">
                <h3 className="text-lg font-semibold text-gray-900">Add Question</h3>

                {/* Question Type */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Question Type
                  </label>
                  <select
                    value={newQuestionType}
                    onChange={(e) => {
                      const type = parseInt(e.target.value, 10) as FormQuestionType;
                      setNewQuestionType(type);
                      // Reset options when changing to/from choice types
                      if (questionTypeNeedsOptions(type) && newQuestionOptions.length < 2) {
                        setNewQuestionOptions(['', '']);
                      }
                    }}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-transparent"
                  >
                    <option value={FormQuestionType.ShortText}>Short Text</option>
                    <option value={FormQuestionType.LongText}>Long Text</option>
                    <option value={FormQuestionType.SingleChoice}>Single Choice</option>
                    <option value={FormQuestionType.MultipleChoice}>Multiple Choice</option>
                    <option value={FormQuestionType.Dropdown}>Dropdown</option>
                    <option value={FormQuestionType.Number}>Number</option>
                    <option value={FormQuestionType.Date}>Date</option>
                    <option value={FormQuestionType.YesNo}>Yes/No</option>
                  </select>
                </div>

                {/* Question Text */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Question Text <span className="text-red-500">*</span>
                  </label>
                  <Input
                    value={newQuestionText}
                    onChange={(e) => setNewQuestionText(e.target.value)}
                    placeholder="e.g., Will you attend the event?"
                    maxLength={500}
                  />
                </div>

                {/* Required Toggle */}
                <div className="flex items-center space-x-3">
                  <input
                    type="checkbox"
                    id="required"
                    checked={newQuestionRequired}
                    onChange={(e) => setNewQuestionRequired(e.target.checked)}
                    className="w-4 h-4 text-primary border-gray-300 rounded focus:ring-primary"
                  />
                  <label htmlFor="required" className="text-sm font-medium text-gray-700 cursor-pointer">
                    Required Question
                  </label>
                </div>

                {/* Help Text */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Help Text (Optional)
                  </label>
                  <Input
                    value={newQuestionHelpText}
                    onChange={(e) => setNewQuestionHelpText(e.target.value)}
                    placeholder="Additional instructions for this question"
                    maxLength={300}
                  />
                </div>

                {/* Options (for choice types) */}
                {questionTypeNeedsOptions(newQuestionType) && (
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Options <span className="text-red-500">*</span> (minimum 2)
                    </label>
                    <div className="space-y-2">
                      {newQuestionOptions.map((option, index) => (
                        <div key={index} className="flex items-center gap-2">
                          <Input
                            value={option}
                            onChange={(e) => handleOptionChange(index, e.target.value)}
                            placeholder={`Option ${index + 1}`}
                          />
                          {newQuestionOptions.length > 2 && (
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleRemoveOption(index)}
                            >
                              <Trash2 className="w-4 h-4 text-red-500" />
                            </Button>
                          )}
                        </div>
                      ))}
                      <Button variant="outline" size="sm" onClick={handleAddOption}>
                        <Plus className="w-4 h-4 mr-2" />
                        Add Option
                      </Button>
                    </div>
                  </div>
                )}

                {/* Add Question Button */}
                <Button onClick={handleAddQuestion} className="w-full">
                  <Plus className="w-4 h-4 mr-2" />
                  Add Question
                </Button>
              </div>

              {/* Right Column: Questions List */}
              <div>
                <h3 className="text-lg font-semibold text-gray-900 mb-4">
                  Questions ({questions.length})
                </h3>

                {questions.length === 0 ? (
                  <div className="text-center py-12 border-2 border-dashed border-gray-300 rounded-lg">
                    <p className="text-gray-500">No questions yet</p>
                    <p className="text-sm text-gray-400 mt-2">Add your first question using the form on the left</p>
                  </div>
                ) : (
                  <div className="space-y-3 max-h-96 overflow-y-auto">
                    <DndContext
                      sensors={sensors}
                      collisionDetection={closestCenter}
                      onDragEnd={handleDragEnd}
                    >
                      <SortableContext
                        items={questions.map(q => q.id)}
                        strategy={verticalListSortingStrategy}
                      >
                        {questions.map((question) => (
                          <SortableQuestionCard
                            key={question.id}
                            question={question}
                            onDelete={handleDeleteQuestion}
                          />
                        ))}
                      </SortableContext>
                    </DndContext>
                  </div>
                )}
              </div>
            </div>
          </CardContent>
          <CardFooter className="flex justify-between">
            <Button
              variant="ghost"
              onClick={() => router.push(`/events/${eventId}/manage?tab=forms`)}
            >
              Cancel
            </Button>
            <Button
              onClick={handleCreateForm}
              disabled={createFormMutation.isPending || !title.trim() || questions.length === 0}
            >
              {createFormMutation.isPending ? 'Creating...' : 'Create Form'}
            </Button>
          </CardFooter>
        </Card>
      </div>

      <Footer />
    </div>
  );
}
