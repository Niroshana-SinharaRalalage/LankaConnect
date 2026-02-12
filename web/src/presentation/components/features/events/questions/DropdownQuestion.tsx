'use client';

/**
 * DropdownQuestion Component
 *
 * Renders a dropdown select for single-choice questions.
 *
 * Custom Forms Feature - Phase 7: Question Types
 */

import { Label } from '@/presentation/components/ui/Label';
import type { FormQuestionDto } from '@/infrastructure/api/types/events.types';

interface DropdownQuestionProps {
  question: FormQuestionDto;
  value?: { selectedOptionIds?: string[] };
  onChange: (value: { selectedOptionIds: string[] }) => void;
  error?: string;
}

export function DropdownQuestion({ question, value, onChange, error }: DropdownQuestionProps) {
  const selectedOptionId = value?.selectedOptionIds?.[0] || '';

  const handleChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const optionId = e.target.value;
    if (optionId) {
      onChange({ selectedOptionIds: [optionId] });
    } else {
      onChange({ selectedOptionIds: [] });
    }
  };

  return (
    <div className="space-y-2">
      <Label htmlFor={question.id}>
        {question.questionText}
        {question.isRequired && <span className="text-red-500 ml-1">*</span>}
      </Label>
      {question.helpText && (
        <p className="text-sm text-gray-500">{question.helpText}</p>
      )}
      <select
        id={question.id}
        value={selectedOptionId}
        onChange={handleChange}
        className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
          error ? 'border-red-500' : 'border-gray-300'
        }`}
      >
        <option value="">-- Select an option --</option>
        {question.options
          ?.sort((a, b) => a.sortOrder - b.sortOrder)
          .map((option) => (
            <option key={option.id} value={option.id}>
              {option.text}
            </option>
          ))}
      </select>
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  );
}
