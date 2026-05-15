import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { PublicFormResponsesSection } from '../PublicFormResponsesSection';
import { EventFormStatus, type EventFormDto, type PublicFormResponsesDto } from '@/infrastructure/api/types/events.types';

/**
 * Phase 6A.146 — PublicFormResponsesSection RTL coverage.
 *
 * The collapsible card rendered on the event detail page that shows public,
 * PII-redacted form responses when the organizer has flipped the toggle on.
 * Component self-gates by checking form.allowAttendeesToViewResponses AND
 * form.status — both must be true for anything to render.
 */

const usePublicFormResponsesMock = vi.fn();
vi.mock('@/presentation/hooks/useEventForms', () => ({
  usePublicFormResponses: (...args: unknown[]) => usePublicFormResponsesMock(...args),
}));

const makeForm = (overrides: Partial<EventFormDto> = {}): EventFormDto => ({
  id: 'form-1',
  eventId: 'event-1',
  title: 'Potluck signup',
  description: null,
  status: EventFormStatus.Active,
  allowMultipleResponses: false,
  responseDeadline: null,
  maxResponses: null,
  hasResponses: false,
  responseCount: 0,
  createdAt: '2026-05-01T00:00:00Z',
  updatedAt: '2026-05-01T00:00:00Z',
  allowAttendeesToViewResponses: true,
  ...overrides,
});

describe('PublicFormResponsesSection — Phase 6A.146', () => {
  it('renders nothing when allowAttendeesToViewResponses is false', () => {
    usePublicFormResponsesMock.mockReturnValue({ data: null, isLoading: false });

    const { container } = render(
      <PublicFormResponsesSection
        eventId="event-1"
        form={makeForm({ allowAttendeesToViewResponses: false })}
      />,
    );
    expect(container).toBeEmptyDOMElement();
  });

  it('renders nothing when status is Draft', () => {
    usePublicFormResponsesMock.mockReturnValue({ data: null, isLoading: false });

    const { container } = render(
      <PublicFormResponsesSection
        eventId="event-1"
        form={makeForm({ status: EventFormStatus.Draft })}
      />,
    );
    expect(container).toBeEmptyDOMElement();
  });

  it('renders nothing when status is Archived', () => {
    usePublicFormResponsesMock.mockReturnValue({ data: null, isLoading: false });

    const { container } = render(
      <PublicFormResponsesSection
        eventId="event-1"
        form={makeForm({ status: EventFormStatus.Archived })}
      />,
    );
    expect(container).toBeEmptyDOMElement();
  });

  it('renders empty state when toggle is on but there are no responses yet', () => {
    const empty: PublicFormResponsesDto = {
      formId: 'form-1',
      formTitle: 'Potluck signup',
      totalCount: 0,
      responses: [],
    };
    usePublicFormResponsesMock.mockReturnValue({ data: empty, isLoading: false });

    render(<PublicFormResponsesSection eventId="event-1" form={makeForm()} />);

    expect(screen.getByText(/potluck signup/i)).toBeInTheDocument();
    expect(screen.getByText(/no responses yet/i)).toBeInTheDocument();
  });

  it('renders response cards with "Respondent N · {date}" labels', () => {
    const payload: PublicFormResponsesDto = {
      formId: 'form-1',
      formTitle: 'Potluck signup',
      totalCount: 2,
      responses: [
        {
          id: 'r1',
          respondentLabel: 'Respondent 1',
          submittedOn: '2026-05-10',
          answers: [
            { questionId: 'q1', questionTextSnapshot: 'What are you bringing?', textValue: 'biriyani', selectedOptionTextSnapshots: [], booleanValue: null },
          ],
        },
        {
          id: 'r2',
          respondentLabel: 'Respondent 2',
          submittedOn: '2026-05-11',
          answers: [
            { questionId: 'q1', questionTextSnapshot: 'What are you bringing?', textValue: 'kottu', selectedOptionTextSnapshots: [], booleanValue: null },
          ],
        },
      ],
    };
    usePublicFormResponsesMock.mockReturnValue({ data: payload, isLoading: false });

    const { container } = render(<PublicFormResponsesSection eventId="event-1" form={makeForm()} />);

    expect(screen.getByText(/respondent 1/i)).toBeInTheDocument();
    expect(screen.getByText(/respondent 2/i)).toBeInTheDocument();
    // Date should be rendered alongside the label (any standard YYYY-MM-DD /
    // locale is acceptable). Use container.textContent (concatenates all text
    // nodes) rather than getByText (per-node matcher) so any rendering style
    // works.
    expect(container.textContent).toContain('2026');
  });

  it('renders question → answer pairs verbatim', () => {
    const payload: PublicFormResponsesDto = {
      formId: 'form-1',
      formTitle: 'Potluck signup',
      totalCount: 1,
      responses: [
        {
          id: 'r1',
          respondentLabel: 'Respondent 1',
          submittedOn: '2026-05-10',
          answers: [
            { questionId: 'q1', questionTextSnapshot: 'What are you bringing?', textValue: 'biriyani', selectedOptionTextSnapshots: [], booleanValue: null },
            { questionId: 'q2', questionTextSnapshot: 'How many people?', textValue: '4', selectedOptionTextSnapshots: [], booleanValue: null },
          ],
        },
      ],
    };
    usePublicFormResponsesMock.mockReturnValue({ data: payload, isLoading: false });

    render(<PublicFormResponsesSection eventId="event-1" form={makeForm()} />);

    expect(screen.getByText(/what are you bringing\?/i)).toBeInTheDocument();
    expect(screen.getByText(/biriyani/i)).toBeInTheDocument();
    expect(screen.getByText(/how many people\?/i)).toBeInTheDocument();
    expect(screen.getByText(/^4$/)).toBeInTheDocument();
  });

  it('does NOT render any respondent PII (no email, no name, no phone-shaped strings)', () => {
    // Even though the backend strips PII, the section MUST also not invent it
    // from any other field. This is a defense-in-depth assertion against future
    // edits that might accidentally surface upstream PII fields.
    const payload: PublicFormResponsesDto = {
      formId: 'form-1',
      formTitle: 'Potluck signup',
      totalCount: 1,
      responses: [
        {
          id: 'r1',
          respondentLabel: 'Respondent 1',
          submittedOn: '2026-05-10',
          answers: [
            { questionId: 'q1', questionTextSnapshot: 'Dish', textValue: 'biriyani', selectedOptionTextSnapshots: [], booleanValue: null },
          ],
        },
      ],
    };
    usePublicFormResponsesMock.mockReturnValue({ data: payload, isLoading: false });

    const { container } = render(
      <PublicFormResponsesSection eventId="event-1" form={makeForm()} />,
    );

    // No '@' character anywhere — covers any rendered email.
    expect(container.textContent).not.toContain('@');
    // Component must NOT render the literal property names of PII fields.
    expect(container.textContent).not.toMatch(/respondentEmail|respondentName|respondentUserId/i);
  });
});
