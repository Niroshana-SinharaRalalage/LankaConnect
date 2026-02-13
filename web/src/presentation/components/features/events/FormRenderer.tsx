'use client';

/**
 * FormRenderer Component
 *
 * Renders event form questions with all question types.
 * Handles form validation and submission.
 *
 * Custom Forms Feature - Phase 7: Attendee UI
 */

import { useState, useEffect } from 'react';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { Label } from '@/presentation/components/ui/Label';
import type { FormQuestionDto, SubmitFormAnswerItem, FormResponseDto } from '@/infrastructure/api/types/events.types';
import { FormQuestionType } from '@/infrastructure/api/types/events.types';

// Question type components
import { ShortTextQuestion } from './questions/ShortTextQuestion';
import { LongTextQuestion } from './questions/LongTextQuestion';
import { SingleChoiceQuestion } from './questions/SingleChoiceQuestion';
import { MultipleChoiceQuestion } from './questions/MultipleChoiceQuestion';
import { DropdownQuestion } from './questions/DropdownQuestion';
import { NumberQuestion } from './questions/NumberQuestion';
import { DateQuestion } from './questions/DateQuestion';
import { YesNoQuestion } from './questions/YesNoQuestion';

interface FormRendererProps {
  questions: FormQuestionDto[];
  onSubmit: (answers: SubmitFormAnswerItem[], respondentName?: string, respondentEmail?: string) => Promise<void>;
  isSubmitting: boolean;
  existingResponse?: FormResponseDto | null;
  allowMultipleResponses?: boolean;
}

interface AnswerState {
  [questionId: string]: {
    textValue?: string;
    selectedOptionIds?: string[];
    booleanValue?: boolean;
  };
}

export function FormRenderer({
  questions,
  onSubmit,
  isSubmitting,
  existingResponse,
  allowMultipleResponses,
}: FormRendererProps) {
  const [answers, setAnswers] = useState<AnswerState>({});
  const [respondentName, setRespondentName] = useState('');
  const [respondentEmail, setRespondentEmail] = useState('');
  const [validationErrors, setValidationErrors] = useState<{ [questionId: string]: string }>({});

  // Pre-fill existing response
  useEffect(() => {
    if (existingResponse) {
      const prefilledAnswers: AnswerState = {};
      existingResponse.answers.forEach((answer) => {
        prefilledAnswers[answer.formQuestionId] = {
          textValue: answer.textValue || undefined,
          selectedOptionIds: answer.selectedOptionIds || undefined,
          booleanValue: answer.booleanValue ?? undefined,
        };
      });
      setAnswers(prefilledAnswers);
      setRespondentName(existingResponse.respondentName || '');
      setRespondentEmail(existingResponse.respondentEmail || '');
    }
  }, [existingResponse]);

  const handleAnswerChange = (
    questionId: string,
    value: { textValue?: string; selectedOptionIds?: string[]; booleanValue?: boolean }
  ) => {
    setAnswers((prev) => ({
      ...prev,
      [questionId]: value,
    }));

    // Clear validation error when user makes a change
    if (validationErrors[questionId]) {
      setValidationErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[questionId];
        return newErrors;
      });
    }
  };

  const validateForm = (): boolean => {
    const errors: { [questionId: string]: string } = {};

    questions.forEach((question) => {
      if (question.isRequired) {
        const answer = answers[question.id];

        if (!answer) {
          errors[question.id] = 'This field is required';
          return;
        }

        // Backend sends enum as string, frontend enum is numeric
        // Helper to check if questionType matches (handles both string and numeric)
        const questionType = question.questionType;
        const isType = (stringValue: string, numericValue: FormQuestionType): boolean => {
          return questionType === stringValue || questionType === numericValue;
        };

        // Validate based on question type
        if (isType('ShortText', FormQuestionType.ShortText) || isType('LongText', FormQuestionType.LongText)) {
          if (!answer.textValue || answer.textValue.trim() === '') {
            errors[question.id] = 'This field is required';
          }
        } else if (isType('SingleChoice', FormQuestionType.SingleChoice) || isType('Dropdown', FormQuestionType.Dropdown)) {
          if (!answer.selectedOptionIds || answer.selectedOptionIds.length === 0) {
            errors[question.id] = 'Please select an option';
          }
        } else if (isType('MultipleChoice', FormQuestionType.MultipleChoice)) {
          if (!answer.selectedOptionIds || answer.selectedOptionIds.length === 0) {
            errors[question.id] = 'Please select at least one option';
          }
        } else if (isType('Number', FormQuestionType.Number)) {
          if (!answer.textValue || answer.textValue.trim() === '') {
            errors[question.id] = 'This field is required';
          } else if (isNaN(Number(answer.textValue))) {
            errors[question.id] = 'Please enter a valid number';
          }
        } else if (isType('Date', FormQuestionType.Date)) {
          if (!answer.textValue || answer.textValue.trim() === '') {
            errors[question.id] = 'This field is required';
          }
        } else if (isType('YesNo', FormQuestionType.YesNo)) {
          if (answer.booleanValue === undefined || answer.booleanValue === null) {
            errors[question.id] = 'Please select Yes or No';
          }
        }
      }
    });

    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    // Convert answers to API format
    const formAnswers: SubmitFormAnswerItem[] = questions.map((question) => {
      const answer = answers[question.id];

      return {
        questionId: question.id,
        textValue: answer?.textValue || null,
        selectedOptionIds: answer?.selectedOptionIds || null,
        booleanValue: answer?.booleanValue ?? null,
      };
    });

    await onSubmit(formAnswers, respondentName || undefined, respondentEmail || undefined);
  };

  const renderQuestion = (question: FormQuestionDto) => {
    const answer = answers[question.id];
    const error = validationErrors[question.id];

    const commonProps = {
      question,
      value: answer,
      onChange: (value: any) => handleAnswerChange(question.id, value),
      error,
    };

    // Backend sends enum as string (e.g., "ShortText"), frontend enum is numeric (0)
    // Need to check both string and numeric values (same pattern as EventFormStatus fix)
    const questionType = question.questionType;

    // Helper to check if questionType matches (handles both string and numeric)
    const isType = (stringValue: string, numericValue: FormQuestionType): boolean => {
      return questionType === stringValue || questionType === numericValue;
    };

    if (isType('ShortText', FormQuestionType.ShortText)) {
      return <ShortTextQuestion key={question.id} {...commonProps} />;
    }
    if (isType('LongText', FormQuestionType.LongText)) {
      return <LongTextQuestion key={question.id} {...commonProps} />;
    }
    if (isType('SingleChoice', FormQuestionType.SingleChoice)) {
      return <SingleChoiceQuestion key={question.id} {...commonProps} />;
    }
    if (isType('MultipleChoice', FormQuestionType.MultipleChoice)) {
      return <MultipleChoiceQuestion key={question.id} {...commonProps} />;
    }
    if (isType('Dropdown', FormQuestionType.Dropdown)) {
      return <DropdownQuestion key={question.id} {...commonProps} />;
    }
    if (isType('Number', FormQuestionType.Number)) {
      return <NumberQuestion key={question.id} {...commonProps} />;
    }
    if (isType('Date', FormQuestionType.Date)) {
      return <DateQuestion key={question.id} {...commonProps} />;
    }
    if (isType('YesNo', FormQuestionType.YesNo)) {
      return <YesNoQuestion key={question.id} {...commonProps} />;
    }

    return null;
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Respondent Info */}
      <div className="space-y-4 pb-6 border-b">
        <div>
          <Label htmlFor="respondentName">Your Name (Optional)</Label>
          <Input
            id="respondentName"
            type="text"
            value={respondentName}
            onChange={(e) => setRespondentName(e.target.value)}
            placeholder="Enter your name"
            maxLength={200}
          />
        </div>
        <div>
          <Label htmlFor="respondentEmail">
            Your Email {!existingResponse && '(Required for editing)'}
          </Label>
          <Input
            id="respondentEmail"
            type="email"
            value={respondentEmail}
            onChange={(e) => setRespondentEmail(e.target.value)}
            placeholder="Enter your email"
            maxLength={255}
          />
          {!existingResponse && (
            <p className="text-xs text-gray-500 mt-1">
              Provide your email to receive an edit link after submission
            </p>
          )}
        </div>
      </div>

      {/* Questions */}
      <div className="space-y-6">
        {questions
          .sort((a, b) => a.sortOrder - b.sortOrder)
          .map((question) => renderQuestion(question))}
      </div>

      {/* Submit Button */}
      <div className="pt-6 border-t">
        <Button
          type="submit"
          disabled={isSubmitting}
          className="w-full sm:w-auto"
        >
          {isSubmitting
            ? existingResponse
              ? 'Updating Response...'
              : 'Submitting...'
            : existingResponse
            ? 'Update Response'
            : 'Submit Response'}
        </Button>
      </div>
    </form>
  );
}
