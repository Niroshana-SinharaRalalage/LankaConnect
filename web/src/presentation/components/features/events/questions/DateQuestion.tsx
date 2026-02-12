'use client';

/**
 * DateQuestion Component
 *
 * Renders a date input question.
 *
 * Custom Forms Feature - Phase 7: Question Types
 */

import { Input } from '@/presentation/components/ui/Input';
import { Label } from '@/presentation/components/ui/Label';
import type { FormQuestionDto } from '@/infrastructure/api/types/events.types';

interface DateQuestionProps {
  question: FormQuestionDto;
  value?: { textValue?: string };
  onChange: (value: { textValue: string }) => void;
  error?: string;
}

export function DateQuestion({ question, value, onChange, error }: DateQuestionProps) {
  return (
    <div className="space-y-2">
      <Label htmlFor={question.id}>
        {question.questionText}
        {question.isRequired && <span className="text-red-500 ml-1">*</span>}
      </Label>
      {question.helpText && (
        <p className="text-sm text-gray-500">{question.helpText}</p>
      )}
      <Input
        id={question.id}
        type="date"
        value={value?.textValue || ''}
        onChange={(e) => onChange({ textValue: e.target.value })}
        className={error ? 'border-red-500' : ''}
      />
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  );
}
