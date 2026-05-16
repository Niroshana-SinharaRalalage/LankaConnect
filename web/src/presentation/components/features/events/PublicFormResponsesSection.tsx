'use client';

import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/presentation/components/ui/Card';
import { Users, MessageSquare } from 'lucide-react';
import { usePublicFormResponses } from '@/presentation/hooks/useEventForms';
import {
  EventFormStatus,
  type EventFormDto,
} from '@/infrastructure/api/types/events.types';

/**
 * Phase 6A.146 — Public Form Responses Section
 *
 * Collapsible card rendered on the event detail page for any form whose
 * organizer has flipped AllowAttendeesToViewResponses to true AND whose
 * status is Active or Closed. PII (respondent name / email / user id) is
 * redacted at the backend projection layer — this component only displays
 * answer text alongside ordinal "Respondent N" labels and a date-only
 * submission date.
 *
 * The component self-gates on (a) the toggle being on AND (b) the status
 * being eligible. If either fails the component renders nothing — which
 * lets the event detail page mount it unconditionally for every form
 * without duplicating gate logic at the call site.
 */

interface PublicFormResponsesSectionProps {
  eventId: string;
  form: EventFormDto;
  /**
   * 2026-05-15 UAT correction: when `embedded` is true the section drops its
   * own outer Card wrapper and the duplicate title header — the parent form
   * card already shows the title and a toggle button, so showing them again
   * is just visual noise. The standalone (non-embedded) variant is kept for
   * any future surface that wants to render the responses in isolation.
   */
  embedded?: boolean;
}

function isStatusEligible(status: EventFormDto['status']): boolean {
  // Server returns the enum as a string ("Active"|"Closed"|...) OR the numeric
  // value depending on JSON converter — match both shapes defensively.
  return (
    status === EventFormStatus.Active ||
    status === EventFormStatus.Closed ||
    status === 'Active' ||
    status === 'Closed'
  );
}

function formatQuestionLabel(question: string): string {
  // 2026-05-15 UAT correction: organizers don't always end questions with
  // a punctuation mark. Append a colon so the question/answer separator is
  // visible ("Number of Attendee: 5"), but skip it when the question already
  // ends with sentence-terminating punctuation so we don't get "your name?:".
  const trimmed = (question ?? '').trim();
  if (!trimmed) return '';
  const last = trimmed[trimmed.length - 1];
  if (last === '?' || last === ':' || last === '.' || last === '!') return trimmed;
  return `${trimmed}:`;
}

function formatSubmittedOn(submittedOn: string): string {
  // Backend sends DateOnly as "YYYY-MM-DD" (architect-locked decision to drop
  // time-of-day for timing-correlation mitigation). Render in the user's
  // locale; fall back to the raw string if parsing fails.
  try {
    const [y, m, d] = submittedOn.split('-').map(Number);
    if (!y || !m || !d) return submittedOn;
    return new Date(y, m - 1, d).toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  } catch (err) {
    console.warn('[PublicFormResponsesSection] failed to format submittedOn', submittedOn, err);
    return submittedOn;
  }
}

export function PublicFormResponsesSection({ eventId, form, embedded = false }: PublicFormResponsesSectionProps) {
  // Self-gate: bail before issuing a network call when the form isn't eligible.
  // Reflects the same defense-in-depth that the backend handler enforces, so
  // even if a stale cache surfaces an ineligible form briefly we won't render.
  const enabled = form.allowAttendeesToViewResponses && isStatusEligible(form.status);

  const { data, isLoading } = usePublicFormResponses(
    enabled ? eventId : undefined,
    enabled ? form.id : undefined,
  );

  if (!enabled) return null;

  if (isLoading) {
    const skeleton = (
      <div className="animate-pulse space-y-2 py-2">
        <div className="h-4 w-1/2 rounded bg-neutral-200 dark:bg-neutral-800" />
        <div className="h-4 w-2/3 rounded bg-neutral-200 dark:bg-neutral-800" />
      </div>
    );
    if (embedded) return <div data-testid="public-responses-loading">{skeleton}</div>;
    return (
      <Card className="my-4">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <Users className="h-5 w-5 text-emerald-600" />
            {form.title}
          </CardTitle>
        </CardHeader>
        <CardContent>{skeleton}</CardContent>
      </Card>
    );
  }

  // Hook swallows 404 → null. If the backend tightened the gate after the
  // organizer flipped the toggle off, we treat it as "no public view".
  if (!data) return null;

  const responses = data.responses ?? [];

  // Privacy note + response list — same content, two outer chrome variants.
  const body = (
    <>
      <p className="mb-3 text-xs text-neutral-500 dark:text-neutral-400">
        Respondent emails and contact details are hidden for privacy.
      </p>
      {responses.length === 0 ? (
        <div className="rounded-md border border-dashed border-neutral-300 bg-neutral-50 p-4 text-center text-sm text-neutral-600 dark:border-neutral-700 dark:bg-neutral-900/40 dark:text-neutral-400">
          No responses yet — be the first to submit.
        </div>
      ) : (
        <ul className="space-y-3">
          {responses.map((r) => (
            <li
              key={r.id}
              className="rounded-md border border-neutral-200 bg-white p-3 dark:border-neutral-800 dark:bg-neutral-900"
            >
              <div className="mb-2 flex items-center gap-2 text-sm font-medium text-neutral-700 dark:text-neutral-300">
                <MessageSquare className="h-4 w-4 text-orange-600" aria-hidden="true" />
                {/*
                  2026-05-15 product correction: surface respondent name when
                  provided (attribution is normal in sign-up contexts); fall
                  back to the ordinal label for anonymous respondents who
                  skipped the optional name field. Email + userId remain off
                  the wire entirely.
                */}
                <span>{r.respondentName?.trim() || r.respondentLabel}</span>
                <span className="text-neutral-400">·</span>
                <span className="text-neutral-500">{formatSubmittedOn(r.submittedOn)}</span>
              </div>
              <dl className="space-y-1 text-sm">
                {r.answers.map((a) => {
                  const display =
                    a.textValue ??
                    (a.selectedOptionTextSnapshots && a.selectedOptionTextSnapshots.length > 0
                      ? a.selectedOptionTextSnapshots.join(', ')
                      : a.booleanValue == null
                        ? '—'
                        : a.booleanValue
                          ? 'Yes'
                          : 'No');
                  return (
                    <div key={a.questionId} className="flex flex-col sm:flex-row sm:gap-2">
                      <dt className="font-medium text-neutral-700 dark:text-neutral-300">
                        {formatQuestionLabel(a.questionTextSnapshot)}
                      </dt>
                      <dd className="text-neutral-600 dark:text-neutral-400">{display}</dd>
                    </div>
                  );
                })}
              </dl>
            </li>
          ))}
        </ul>
      )}
    </>
  );

  if (embedded) {
    // Parent (the Signup Forms form card) already shows the title and owns the
    // Show/Hide toggle. Render only the privacy note + responses, separated
    // from the form info above by a subtle top border.
    return (
      <div
        data-testid="public-responses-embedded"
        className="mt-4 border-t border-neutral-200 pt-4 dark:border-neutral-800"
      >
        {body}
      </div>
    );
  }

  // Standalone variant — keeps the original outer Card + title for any
  // surface that mounts the section independently of a form card.
  return (
    <Card className="my-4 border-emerald-200/60 dark:border-emerald-900/40">
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-base">
          <Users className="h-5 w-5 text-emerald-600" aria-hidden="true" />
          <span>{data.formTitle || form.title}</span>
          <span className="ml-auto text-sm font-normal text-neutral-500">
            {data.totalCount} {data.totalCount === 1 ? 'response' : 'responses'}
          </span>
        </CardTitle>
      </CardHeader>
      <CardContent>{body}</CardContent>
    </Card>
  );
}
