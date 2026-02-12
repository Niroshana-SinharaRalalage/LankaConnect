'use client';

/**
 * MultipleChoiceQuestion Component
 *
 * Renders a checkbox group for multiple-choice questions.
 *
 * Custom Forms Feature - Phase 7: Question Types
 */

import { Label } from '@/presentation/components/ui/Label';
import type { FormQuestionDto } from '@/infrastructure/api/types/events.types';

interface MultipleChoiceQuestionProps {
  question: FormQuestionDto;
  value?: { selectedOptionIds?: string[] };
  onChange: (value: { selectedOptionIds: string[] }) => void;
  error?: string;
}

export function MultipleChoiceQuestion({ question, value, onChange, error }: MultipleChoiceQuestionProps) {
  const selectedOptionIds = value?.selectedOptionIds || [];

  const handleChange = (optionId: string, checked: boolean) => {
    if (checked) {
      onChange({ selectedOptionIds: [...selectedOptionIds, optionId] });
    } else {
      onChange({ selectedOptionIds: selectedOptionIds.filter((id) => id !== optionId) });
    }
  };

  return (
    <div className="space-y-2">
      <Label>
        {question.questionText}
        {question.isRequired && <span className="text-red-500 ml-1">*</span>}
      </Label>
      {question.helpText && (
        <p className="text-sm text-gray-500">{question.helpText}</p>
      )}
      <div className="space-y-2">
        {question.options
          ?.sort((a, b) => a.sortOrder - b.sortOrder)
          .map((option) => (
            <div key={option.id} className="flex items-center">
              <input
                type="checkbox"
                id={`${question.id}-${option.id}`}
                value={option.id}
                checked={selectedOptionIds.includes(option.id)}
                onChange={(e) => handleChange(option.id, e.target.checked)}
                className="w-4 h-4 text-primary border-gray-300 rounded focus:ring-primary"
              />
              <label
                htmlFor={`${question.id}-${option.id}`}
                className="ml-3 text-sm text-gray-900 cursor-pointer"
              >
                {option.text}
              </label>
            </div>
          ))}
      </div>
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  );
}
