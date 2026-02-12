'use client';

/**
 * SingleChoiceQuestion Component
 *
 * Renders a radio button group for single-choice questions.
 *
 * Custom Forms Feature - Phase 7: Question Types
 */

import { Label } from '@/presentation/components/ui/Label';
import type { FormQuestionDto } from '@/infrastructure/api/types/events.types';

interface SingleChoiceQuestionProps {
  question: FormQuestionDto;
  value?: { selectedOptionIds?: string[] };
  onChange: (value: { selectedOptionIds: string[] }) => void;
  error?: string;
}

export function SingleChoiceQuestion({ question, value, onChange, error }: SingleChoiceQuestionProps) {
  const selectedOptionId = value?.selectedOptionIds?.[0];

  const handleChange = (optionId: string) => {
    onChange({ selectedOptionIds: [optionId] });
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
                type="radio"
                id={`${question.id}-${option.id}`}
                name={question.id}
                value={option.id}
                checked={selectedOptionId === option.id}
                onChange={() => handleChange(option.id)}
                className="w-4 h-4 text-primary border-gray-300 focus:ring-primary"
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
