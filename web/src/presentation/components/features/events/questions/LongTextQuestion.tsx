'use client';

/**
 * LongTextQuestion Component
 *
 * Renders a multi-line textarea input question.
 *
 * Custom Forms Feature - Phase 7: Question Types
 */

import { Textarea } from '@/presentation/components/ui/Textarea';
import { Label } from '@/presentation/components/ui/Label';
import type { FormQuestionDto } from '@/infrastructure/api/types/events.types';

interface LongTextQuestionProps {
  question: FormQuestionDto;
  value?: { textValue?: string };
  onChange: (value: { textValue: string }) => void;
  error?: string;
}

export function LongTextQuestion({ question, value, onChange, error }: LongTextQuestionProps) {
  return (
    <div className="space-y-2">
      <Label htmlFor={question.id}>
        {question.questionText}
        {question.isRequired && <span className="text-red-500 ml-1">*</span>}
      </Label>
      {question.helpText && (
        <p className="text-sm text-gray-500">{question.helpText}</p>
      )}
      <Textarea
        id={question.id}
        value={value?.textValue || ''}
        onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => onChange({ textValue: e.target.value })}
        placeholder="Enter your answer"
        rows={4}
        maxLength={2000}
        className={error ? 'border-red-500' : ''}
      />
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  );
}
