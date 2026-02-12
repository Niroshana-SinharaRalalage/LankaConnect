'use client';

/**
 * YesNoQuestion Component
 *
 * Renders a Yes/No toggle question.
 *
 * Custom Forms Feature - Phase 7: Question Types
 */

import { Label } from '@/presentation/components/ui/Label';
import type { FormQuestionDto } from '@/infrastructure/api/types/events.types';

interface YesNoQuestionProps {
  question: FormQuestionDto;
  value?: { booleanValue?: boolean };
  onChange: (value: { booleanValue: boolean }) => void;
  error?: string;
}

export function YesNoQuestion({ question, value, onChange, error }: YesNoQuestionProps) {
  return (
    <div className="space-y-2">
      <Label>
        {question.questionText}
        {question.isRequired && <span className="text-red-500 ml-1">*</span>}
      </Label>
      {question.helpText && (
        <p className="text-sm text-gray-500">{question.helpText}</p>
      )}
      <div className="flex gap-4">
        <button
          type="button"
          onClick={() => onChange({ booleanValue: true })}
          className={`px-6 py-2 rounded-md border-2 transition-colors ${
            value?.booleanValue === true
              ? 'border-primary bg-primary text-white'
              : 'border-gray-300 bg-white text-gray-700 hover:border-gray-400'
          }`}
        >
          Yes
        </button>
        <button
          type="button"
          onClick={() => onChange({ booleanValue: false })}
          className={`px-6 py-2 rounded-md border-2 transition-colors ${
            value?.booleanValue === false
              ? 'border-primary bg-primary text-white'
              : 'border-gray-300 bg-white text-gray-700 hover:border-gray-400'
          }`}
        >
          No
        </button>
      </div>
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  );
}
