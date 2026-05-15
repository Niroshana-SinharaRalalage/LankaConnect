/**
 * Event Forms Management React Query Hooks
 *
 * Provides React Query hooks for Custom Forms (Survey/Form Sign-Up Type) API integration
 * Implements caching, optimistic updates, and proper error handling
 *
 * Custom Forms Feature: Google Forms-like functionality for events
 * - Form Builder (organizer): Create forms with 8 question types
 * - Form Submission (attendee): Anonymous responses with token-based editing
 * - Response Management (organizer): View and export responses
 *
 * @requires @tanstack/react-query
 * @requires eventsRepository from infrastructure/repositories/events.repository
 * @requires Form types from infrastructure/api/types/events.types
 */

import {
  useQuery,
  useQueries,
  useMutation,
  useQueryClient,
  UseQueryOptions,
  UseMutationOptions,
} from '@tanstack/react-query';

import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type {
  EventFormDto,
  EventFormDetailDto,
  FormResponseDto,
  FormResponsesPagedDto,
  PublicFormResponsesDto,  // Phase 6A.146
  CreateEventFormRequest,
  UpdateEventFormRequest,
  AddFormQuestionRequest,
  UpdateFormQuestionRequest,
  ReorderFormQuestionsRequest,
  SubmitFormResponseRequest,
  SubmitFormResponseResult,
  UpdateFormResponseRequest,
} from '@/infrastructure/api/types/events.types';

import { ApiError } from '@/infrastructure/api/client/api-errors';
import { eventKeys } from './useEvents';

/**
 * Query Keys for Event Forms
 * Centralized query key management for cache invalidation
 */
export const formKeys = {
  all: ['forms'] as const,
  lists: () => [...formKeys.all, 'list'] as const,
  list: (eventId: string) => [...formKeys.lists(), eventId] as const,
  details: () => [...formKeys.all, 'detail'] as const,
  detail: (eventId: string, formId: string) => [...formKeys.details(), eventId, formId] as const,
  responses: () => [...formKeys.all, 'responses'] as const,
  responsesList: (eventId: string, formId: string) =>
    [...formKeys.responses(), eventId, formId] as const,
  // Phase 6A.146 — public responses key (anonymous view).
  publicResponses: (eventId: string, formId: string) =>
    [...formKeys.responses(), 'public', eventId, formId] as const,
  myResponse: (eventId: string, formId: string, token: string) =>
    [...formKeys.responses(), 'mine', eventId, formId, token] as const,
};

// ==================== QUERIES ====================

/**
 * useEventForms Hook
 *
 * Fetches all forms for a specific event (organizer view)
 * Shows form summaries with response counts
 *
 * Features:
 * - Automatic caching with 5-minute stale time
 * - Refetch on window focus
 * - Proper error handling with ApiError types
 * - Only enabled when eventId is provided
 *
 * @param eventId - Event ID (GUID)
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data: forms, isLoading, error } = useEventForms(eventId);
 * ```
 */
export function useEventForms(
  eventId: string | undefined,
  options?: Omit<UseQueryOptions<EventFormDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: formKeys.list(eventId || ''),
    queryFn: () => eventsRepository.getEventForms(eventId!),
    enabled: !!eventId,
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true,
    retry: 1,
    ...options,
  });
}

/**
 * useEventFormDetail Hook
 *
 * Fetches form detail including questions
 * Public endpoint - can be used by attendees to fill out form
 *
 * @param eventId - Event ID (GUID)
 * @param formId - Form ID (GUID)
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data: form, isLoading } = useEventFormDetail(eventId, formId);
 * if (form) {
 *   form.questions.forEach(q => console.log(q.questionText));
 * }
 * ```
 */
export function useEventFormDetail(
  eventId: string | undefined,
  formId: string | undefined,
  options?: Omit<UseQueryOptions<EventFormDetailDto, ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: formKeys.detail(eventId || '', formId || ''),
    queryFn: () => eventsRepository.getEventFormDetail(eventId!, formId!),
    enabled: !!eventId && !!formId,
    staleTime: 3 * 60 * 1000, // 3 minutes (shorter for active forms)
    refetchOnWindowFocus: true,
    retry: 1,
    ...options,
  });
}

/**
 * useFormResponses Hook
 *
 * Fetches paginated responses for a form (organizer view)
 * Includes all answers for each response
 *
 * @param eventId - Event ID (GUID)
 * @param formId - Form ID (GUID)
 * @param page - Page number (default 1)
 * @param pageSize - Items per page (default 20)
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data, isLoading } = useFormResponses(eventId, formId, page, 50);
 * console.log(`Total responses: ${data?.totalCount}`);
 * ```
 */
export function useFormResponses(
  eventId: string | undefined,
  formId: string | undefined,
  page: number = 1,
  pageSize: number = 20,
  options?: Omit<UseQueryOptions<FormResponsesPagedDto, ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: [...formKeys.responsesList(eventId || '', formId || ''), page, pageSize] as const,
    queryFn: () => eventsRepository.getFormResponses(eventId!, formId!, page, pageSize),
    enabled: !!eventId && !!formId,
    staleTime: 2 * 60 * 1000, // 2 minutes (frequently changing data)
    refetchOnWindowFocus: true,
    retry: 1,
    ...options,
  });
}

/**
 * Phase 6A.146 — usePublicFormResponses Hook
 *
 * Fetches the public, PII-redacted form responses. Returns `null` (not an
 * error) when the backend responds 404 — that's how the organizer's
 * `allowAttendeesToViewResponses` toggle being off is expressed on the wire,
 * and we don't want the UI to flash an error banner for the most common path
 * (toggle off). The repository swallows the 404 → null mapping so this hook
 * just lets the data be `null | undefined` while the section renders nothing.
 *
 * @param eventId - Event ID
 * @param formId - Form ID
 * @param options - React Query options (retry/disable/etc.)
 */
export function usePublicFormResponses(
  eventId: string | undefined,
  formId: string | undefined,
  options?: Omit<UseQueryOptions<PublicFormResponsesDto | null, ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: formKeys.publicResponses(eventId || '', formId || ''),
    queryFn: () => eventsRepository.getPublicFormResponses(eventId!, formId!),
    enabled: !!eventId && !!formId,
    staleTime: 60 * 1000, // 1 minute — public view doesn't need to be real-time
    refetchOnWindowFocus: false,
    retry: 0,  // 404 is intentional — no point retrying
    ...options,
  });
}

/**
 * useMyFormResponse Hook
 *
 * Fetches respondent's own response using access token
 * Used for editing responses before deadline
 * Public endpoint - AllowAnonymous
 *
 * @param eventId - Event ID (GUID)
 * @param formId - Form ID (GUID)
 * @param accessToken - Access token from submission
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data: myResponse, isLoading } = useMyFormResponse(
 *   eventId,
 *   formId,
 *   tokenFromUrl
 * );
 * ```
 */
export function useMyFormResponse(
  eventId: string | undefined,
  formId: string | undefined,
  accessToken: string | undefined,
  options?: Omit<UseQueryOptions<FormResponseDto, ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: formKeys.myResponse(eventId || '', formId || '', accessToken || ''),
    queryFn: () => eventsRepository.getMyFormResponse(eventId!, formId!, accessToken!),
    enabled: !!eventId && !!formId && !!accessToken,
    staleTime: 60 * 1000, // 1 minute (user's own data)
    refetchOnWindowFocus: false, // Don't refetch on focus for edit page
    retry: 1,
    ...options,
  });
}

/**
 * useMyFormResponseByUserId Hook
 *
 * Phase 6A.106-110 Fix: Fetches logged-in user's response by userId (not token).
 * Enables Edit/Delete buttons in Signup Forms tab for logged-in users.
 * Returns null if user has no response (not an error - UI shows "Fill Out Form").
 *
 * @param eventId - Event ID (GUID)
 * @param formId - Form ID (GUID)
 * @param enabled - Whether to fetch (default: true if eventId and formId present)
 * @returns FormResponseDto if user has responded, null otherwise
 *
 * @example
 * ```tsx
 * const { data: userResponse } = useMyFormResponseByUserId(eventId, formId);
 * const hasResponded = userResponse !== null;
 * ```
 */
export function useMyFormResponseByUserId(
  eventId: string | undefined,
  formId: string | undefined,
  options?: Omit<UseQueryOptions<FormResponseDto | null, ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: ['formResponse', 'my', eventId, formId],
    queryFn: () => eventsRepository.getMyFormResponseByUserId(eventId!, formId!),
    enabled: !!eventId && !!formId,
    staleTime: 60 * 1000, // 1 minute
    refetchOnWindowFocus: false,
    retry: 1,
    ...options,
  });
}

/**
 * useUserFormResponses Hook (Phase 6A.128)
 *
 * Batch-fetches logged-in user's responses for all active forms using React Query's useQueries.
 * Replaces the manual useEffect + useState pattern in page.tsx to ensure cache invalidation
 * from mutations (delete, submit, update) automatically propagates to the UI.
 *
 * @param eventId - Event ID (GUID)
 * @param formIds - Array of form IDs to check responses for
 * @param enabled - Whether queries should run (typically: isAuthenticated && formIds.length > 0)
 * @returns Record<formId, FormResponseDto | null> mapping each form to the user's response
 */
export function useUserFormResponses(
  eventId: string | undefined,
  formIds: string[],
  enabled: boolean = true,
) {
  const queries = useQueries({
    queries: formIds.map((formId) => ({
      queryKey: ['formResponse', 'my', eventId, formId],
      queryFn: () => eventsRepository.getMyFormResponseByUserId(eventId!, formId),
      enabled: enabled && !!eventId,
      staleTime: 60 * 1000, // 1 minute
      refetchOnWindowFocus: false,
      retry: 1,
    })),
  });

  // Derive the responses map reactively from query results
  const responsesMap: Record<string, FormResponseDto | null> = {};
  formIds.forEach((formId, index) => {
    const query = queries[index];
    // Only set response if query succeeded; treat errors as "no response"
    responsesMap[formId] = query.data ?? null;

    // Phase 6A.128b Fix: Proactively clean up stale localStorage tokens.
    // When API confirms no response exists (query succeeded with null/undefined data),
    // remove the stale localStorage token that would cause hasStoredToken to be true.
    if (query.isSuccess && !query.data && eventId && typeof window !== 'undefined') {
      const storageKey = `form_response_token_${eventId}_${formId}`;
      if (localStorage.getItem(storageKey)) {
        localStorage.removeItem(storageKey);
      }
    }
  });

  const isLoading = queries.some((q) => q.isLoading);

  return { userFormResponses: responsesMap, isFetchingResponses: isLoading };
}

// ==================== MUTATIONS - FORM CRUD ====================

/**
 * useCreateEventForm Hook
 *
 * Mutation for creating a new event form with initial questions (organizer)
 *
 * Features:
 * - Invalidates forms list cache after success
 * - Invalidates event detail cache (for updated counts)
 *
 * @example
 * ```tsx
 * const createForm = useCreateEventForm();
 *
 * await createForm.mutateAsync({
 *   eventId: 'event-123',
 *   title: 'RSVP Form',
 *   description: 'Please confirm attendance',
 *   allowMultipleResponses: false,
 *   responseDeadline: null,
 *   maxResponses: null,
 *   questions: [
 *     {
 *       questionText: 'Will you attend?',
 *       questionType: FormQuestionType.YesNo,
 *       isRequired: true,
 *       sortOrder: 0,
 *       helpText: null,
 *       options: null
 *     }
 *   ]
 * });
 * ```
 */
export function useCreateEventForm(
  options?: UseMutationOptions<
    string,
    ApiError,
    { eventId: string; request: CreateEventFormRequest }
  >
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, request }) => eventsRepository.createEventForm(eventId, request),
    onSuccess: (_, { eventId }) => {
      queryClient.invalidateQueries({ queryKey: formKeys.list(eventId) });
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });
    },
    ...options,
  });
}

/**
 * useUpdateEventForm Hook
 *
 * Mutation for updating form metadata (title, description, settings)
 * Does NOT update questions - use question-specific mutations
 *
 * @example
 * ```tsx
 * const updateForm = useUpdateEventForm();
 *
 * await updateForm.mutateAsync({
 *   eventId,
 *   formId,
 *   request: {
 *     title: 'Updated RSVP Form',
 *     description: 'New description',
 *     allowMultipleResponses: false,
 *     responseDeadline: null,
 *     maxResponses: 100
 *   }
 * });
 * ```
 */
export function useUpdateEventForm(
  options?: UseMutationOptions<
    void,
    ApiError,
    { eventId: string; formId: string; request: UpdateEventFormRequest }
  >
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, formId, request }) =>
      eventsRepository.updateEventForm(eventId, formId, request),
    onSuccess: (_, { eventId, formId }) => {
      queryClient.invalidateQueries({ queryKey: formKeys.list(eventId) });
      queryClient.invalidateQueries({ queryKey: formKeys.detail(eventId, formId) });
    },
    ...options,
  });
}

/**
 * useDeleteEventForm Hook
 *
 * Mutation for deleting a form (only if no responses exist)
 *
 * @example
 * ```tsx
 * const deleteForm = useDeleteEventForm();
 *
 * await deleteForm.mutateAsync({ eventId, formId });
 * ```
 */
export function useDeleteEventForm(
  options?: UseMutationOptions<void, ApiError, { eventId: string; formId: string }>
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, formId }) => eventsRepository.deleteEventForm(eventId, formId),
    onSuccess: (_, { eventId }) => {
      queryClient.invalidateQueries({ queryKey: formKeys.list(eventId) });
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });
    },
    ...options,
  });
}

// ==================== MUTATIONS - LIFECYCLE ====================

/**
 * usePublishEventForm Hook
 *
 * Mutation for publishing a form (Draft -> Active)
 * Form must have at least one question
 *
 * @example
 * ```tsx
 * const publishForm = usePublishEventForm();
 *
 * await publishForm.mutateAsync({ eventId, formId });
 * ```
 */
export function usePublishEventForm(
  options?: UseMutationOptions<void, ApiError, { eventId: string; formId: string }>
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, formId }) => eventsRepository.publishEventForm(eventId, formId),
    onSuccess: (_, { eventId, formId }) => {
      queryClient.invalidateQueries({ queryKey: formKeys.list(eventId) });
      queryClient.invalidateQueries({ queryKey: formKeys.detail(eventId, formId) });
      // Force immediate refetch to update UI status badge without waiting for staleTime
      queryClient.refetchQueries({ queryKey: formKeys.list(eventId) });
    },
    ...options,
  });
}

/**
 * useCloseEventForm Hook
 *
 * Mutation for closing a form (Active -> Closed)
 * Stops accepting new responses
 *
 * @example
 * ```tsx
 * const closeForm = useCloseEventForm();
 *
 * await closeForm.mutateAsync({ eventId, formId });
 * ```
 */
export function useCloseEventForm(
  options?: UseMutationOptions<void, ApiError, { eventId: string; formId: string }>
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, formId }) => eventsRepository.closeEventForm(eventId, formId),
    onSuccess: (_, { eventId, formId }) => {
      queryClient.invalidateQueries({ queryKey: formKeys.list(eventId) });
      queryClient.invalidateQueries({ queryKey: formKeys.detail(eventId, formId) });
      // Force immediate refetch to update UI status badge without waiting for staleTime
      queryClient.refetchQueries({ queryKey: formKeys.list(eventId) });
    },
    ...options,
  });
}

/**
 * useReopenEventForm Hook
 *
 * Mutation for reopening a form (Closed -> Active)
 * Resumes accepting responses
 *
 * @example
 * ```tsx
 * const reopenForm = useReopenEventForm();
 *
 * await reopenForm.mutateAsync({ eventId, formId });
 * ```
 */
export function useReopenEventForm(
  options?: UseMutationOptions<void, ApiError, { eventId: string; formId: string }>
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, formId }) => eventsRepository.reopenEventForm(eventId, formId),
    onSuccess: (_, { eventId, formId }) => {
      queryClient.invalidateQueries({ queryKey: formKeys.list(eventId) });
      queryClient.invalidateQueries({ queryKey: formKeys.detail(eventId, formId) });
      // Force immediate refetch to update UI status badge without waiting for staleTime
      queryClient.refetchQueries({ queryKey: formKeys.list(eventId) });
    },
    ...options,
  });
}

// ==================== MUTATIONS - QUESTION MANAGEMENT ====================

/**
 * useAddFormQuestion Hook
 *
 * Mutation for adding a question to a form
 *
 * @example
 * ```tsx
 * const addQuestion = useAddFormQuestion();
 *
 * await addQuestion.mutateAsync({
 *   eventId,
 *   formId,
 *   request: {
 *     questionText: 'What is your dietary preference?',
 *     questionType: FormQuestionType.SingleChoice,
 *     isRequired: true,
 *     sortOrder: 1,
 *     helpText: null,
 *     options: [
 *       { text: 'Vegetarian', sortOrder: 0 },
 *       { text: 'Vegan', sortOrder: 1 },
 *       { text: 'None', sortOrder: 2 }
 *     ]
 *   }
 * });
 * ```
 */
export function useAddFormQuestion(
  options?: UseMutationOptions<
    string,
    ApiError,
    { eventId: string; formId: string; request: AddFormQuestionRequest }
  >
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, formId, request }) =>
      eventsRepository.addFormQuestion(eventId, formId, request),
    onSuccess: (_, { eventId, formId }) => {
      queryClient.invalidateQueries({ queryKey: formKeys.detail(eventId, formId) });
    },
    ...options,
  });
}

/**
 * useUpdateFormQuestion Hook
 *
 * Mutation for updating a question
 * Type changes blocked when responses exist
 *
 * @example
 * ```tsx
 * const updateQuestion = useUpdateFormQuestion();
 *
 * await updateQuestion.mutateAsync({
 *   eventId,
 *   formId,
 *   questionId,
 *   request: {
 *     questionText: 'Updated question text',
 *     questionType: FormQuestionType.MultipleChoice,
 *     isRequired: true,
 *     sortOrder: 0,
 *     helpText: 'Select all that apply',
 *     options: [
 *       { id: 'existing-id', text: 'Option 1', sortOrder: 0 },
 *       { id: null, text: 'New Option 2', sortOrder: 1 }
 *     ]
 *   }
 * });
 * ```
 */
export function useUpdateFormQuestion(
  options?: UseMutationOptions<
    void,
    ApiError,
    { eventId: string; formId: string; questionId: string; request: UpdateFormQuestionRequest }
  >
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, formId, questionId, request }) =>
      eventsRepository.updateFormQuestion(eventId, formId, questionId, request),
    onSuccess: (_, { eventId, formId }) => {
      queryClient.invalidateQueries({ queryKey: formKeys.detail(eventId, formId) });
    },
    ...options,
  });
}

/**
 * useDeleteFormQuestion Hook
 *
 * Mutation for deleting a question
 * Blocked if responses exist
 *
 * @example
 * ```tsx
 * const deleteQuestion = useDeleteFormQuestion();
 *
 * await deleteQuestion.mutateAsync({ eventId, formId, questionId });
 * ```
 */
export function useDeleteFormQuestion(
  options?: UseMutationOptions<
    void,
    ApiError,
    { eventId: string; formId: string; questionId: string }
  >
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, formId, questionId }) =>
      eventsRepository.deleteFormQuestion(eventId, formId, questionId),
    onSuccess: (_, { eventId, formId }) => {
      queryClient.invalidateQueries({ queryKey: formKeys.detail(eventId, formId) });
    },
    ...options,
  });
}

/**
 * useReorderFormQuestions Hook
 *
 * Mutation for reordering questions
 * Pass array of question IDs in desired order
 *
 * @example
 * ```tsx
 * const reorderQuestions = useReorderFormQuestions();
 *
 * await reorderQuestions.mutateAsync({
 *   eventId,
 *   formId,
 *   request: {
 *     questionIdsInOrder: ['q3-id', 'q1-id', 'q2-id']
 *   }
 * });
 * ```
 */
export function useReorderFormQuestions(
  options?: UseMutationOptions<
    void,
    ApiError,
    { eventId: string; formId: string; request: ReorderFormQuestionsRequest }
  >
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, formId, request }) =>
      eventsRepository.reorderFormQuestions(eventId, formId, request),
    onSuccess: (_, { eventId, formId }) => {
      queryClient.invalidateQueries({ queryKey: formKeys.detail(eventId, formId) });
    },
    ...options,
  });
}

// ==================== MUTATIONS - RESPONSE SUBMISSION ====================

/**
 * useSubmitFormResponse Hook
 *
 * Mutation for submitting a form response (public - AllowAnonymous)
 * Returns access token for editing response before deadline
 *
 * Features:
 * - Generates cryptographic access token
 * - Validates required questions are answered
 * - Checks form is accepting responses
 * - Invalidates response counts
 *
 * @example
 * ```tsx
 * const submitResponse = useSubmitFormResponse();
 *
 * const result = await submitResponse.mutateAsync({
 *   eventId,
 *   formId,
 *   request: {
 *     respondentName: 'John Doe',
 *     respondentEmail: 'john@example.com',
 *     answers: [
 *       {
 *         questionId: 'q1-id',
 *         booleanValue: true,
 *         textValue: null,
 *         selectedOptionIds: null
 *       },
 *       {
 *         questionId: 'q2-id',
 *         booleanValue: null,
 *         textValue: null,
 *         selectedOptionIds: ['opt1-id', 'opt2-id']
 *       }
 *     ]
 *   }
 * });
 *
 * // Store the access token for editing
 * console.log('Access token:', result.accessToken);
 * console.log('Edit URL:', `/events/${eventId}/forms/${formId}?token=${result.accessToken}`);
 * ```
 */
export function useSubmitFormResponse(
  options?: UseMutationOptions<
    SubmitFormResponseResult,
    ApiError,
    { eventId: string; formId: string; request: SubmitFormResponseRequest }
  >
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, formId, request }) =>
      eventsRepository.submitFormResponse(eventId, formId, request),
    onSuccess: (_, { eventId, formId }) => {
      // Invalidate form list to update response counts
      queryClient.invalidateQueries({ queryKey: formKeys.list(eventId) });
      // Invalidate responses list if organizer is viewing
      queryClient.invalidateQueries({ queryKey: formKeys.responsesList(eventId, formId) });
      // Invalidate user-based response query so "You already responded" updates for logged-in users
      queryClient.invalidateQueries({ queryKey: ['formResponse', 'my', eventId, formId] });
    },
    ...options,
  });
}

/**
 * useUpdateFormResponse Hook
 *
 * Mutation for updating an existing response (public - AllowAnonymous)
 * Requires access token from initial submission
 * Blocked if deadline has passed
 *
 * @example
 * ```tsx
 * const updateResponse = useUpdateFormResponse();
 *
 * await updateResponse.mutateAsync({
 *   eventId,
 *   formId,
 *   responseId,
 *   accessToken: tokenFromUrl,
 *   request: {
 *     answers: [
 *       {
 *         questionId: 'q1-id',
 *         booleanValue: false,  // Changed from true
 *         textValue: null,
 *         selectedOptionIds: null
 *       }
 *     ]
 *   }
 * });
 * ```
 */
export function useUpdateFormResponse(
  options?: UseMutationOptions<
    void,
    ApiError,
    {
      eventId: string;
      formId: string;
      responseId: string;
      accessToken?: string;  // Optional for logged-in users
      request: UpdateFormResponseRequest;
    }
  >
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, formId, responseId, accessToken, request }) =>
      eventsRepository.updateFormResponse(eventId, formId, responseId, accessToken, request),
    onSuccess: (_, { eventId, formId, accessToken }) => {
      // Phase 6A.111: Comprehensive cache invalidation to ensure UI updates immediately

      // 1. Invalidate specific response (token-based for anonymous users)
      if (accessToken) {
        queryClient.invalidateQueries({ queryKey: formKeys.myResponse(eventId, formId, accessToken) });
      }

      // 2. Invalidate user-based response query (for logged-in users)
      queryClient.invalidateQueries({ queryKey: ['formResponse', 'my', eventId, formId] });

      // 3. Invalidate form detail (shows updated questions/answers in UI)
      queryClient.invalidateQueries({ queryKey: formKeys.detail(eventId, formId) });

      // 4. Invalidate ALL paginated responses (not just base key)
      queryClient.invalidateQueries({
        queryKey: formKeys.responsesList(eventId, formId),
        exact: false  // Invalidates all pages (page=1, page=2, etc.)
      });

      // 5. Invalidate form list (updates response counts in form summary)
      queryClient.invalidateQueries({ queryKey: formKeys.list(eventId) });

      // 6. Invalidate ALL form queries (wildcard pattern like useUpdateEvent)
      queryClient.invalidateQueries({ queryKey: formKeys.all });

      // 7. Immediate refetch (don't wait for staleTime)
      queryClient.refetchQueries({ queryKey: formKeys.detail(eventId, formId) });
    },
    ...options,
  });
}

/**
 * useDeleteFormResponse Hook
 *
 * Phase 6A.106: Mutation for deleting a form response
 * Supports both organizer and user deletion:
 * - Organizer: Authenticated, no token required (removes spam/test responses)
 * - Anonymous user: Requires access token from submission
 * - Logged-in user: Authenticated, no token required (verified by userId)
 *
 * Features:
 * - Clears localStorage token after successful deletion
 * - Invalidates response list and form counts
 * - Cancellation email sent by backend handler
 *
 * @example
 * ```tsx
 * // Anonymous user deletion
 * const deleteResponse = useDeleteFormResponse();
 * await deleteResponse.mutateAsync({
 *   eventId,
 *   formId,
 *   responseId,
 *   accessToken: tokenFromUrl
 * });
 *
 * // Organizer deletion
 * await deleteResponse.mutateAsync({
 *   eventId,
 *   formId,
 *   responseId
 * });
 * ```
 */
export function useDeleteFormResponse(
  options?: UseMutationOptions<
    void,
    ApiError,
    { eventId: string; formId: string; responseId: string; accessToken?: string }
  >
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, formId, responseId, accessToken }) =>
      eventsRepository.deleteFormResponse(eventId, formId, responseId, accessToken),
    onSuccess: (_, { eventId, formId, accessToken }) => {
      // Clear localStorage token if present (cross-browser cleanup)
      const storageKey = `form_response_token_${eventId}_${formId}`;
      localStorage.removeItem(storageKey);

      // 1. Invalidate token-based response cache (anonymous users)
      if (accessToken) {
        queryClient.invalidateQueries({ queryKey: formKeys.myResponse(eventId, formId, accessToken) });
      }

      // 2. Invalidate user-based response query (logged-in users)
      // This was missing — caused stale "You already responded" after deletion
      queryClient.invalidateQueries({ queryKey: ['formResponse', 'my', eventId, formId] });

      // 3. Invalidate form detail (updates question/answer state in UI)
      queryClient.invalidateQueries({ queryKey: formKeys.detail(eventId, formId) });

      // 4. Invalidate ALL paginated responses (not just base key)
      queryClient.invalidateQueries({
        queryKey: formKeys.responsesList(eventId, formId),
        exact: false,
      });

      // 5. Invalidate form list (updates response counts in form summary)
      queryClient.invalidateQueries({ queryKey: formKeys.list(eventId) });

      // 6. Invalidate ALL form queries (wildcard catch-all)
      queryClient.invalidateQueries({ queryKey: formKeys.all });
    },
    ...options,
  });
}
