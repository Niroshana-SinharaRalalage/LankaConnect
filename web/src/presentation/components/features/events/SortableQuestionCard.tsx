'use client';

/**
 * SortableQuestionCard Component
 *
 * Draggable question card for form builder.
 * Uses dnd-kit for drag-drop functionality.
 *
 * Custom Forms Feature - Phase 6B: Form Creation
 */

import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { GripVertical, Trash2 } from 'lucide-react';

import { Card, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { FormQuestionType } from '@/infrastructure/api/types/events.types';
import { cn } from '@/presentation/lib/utils';

interface QuestionDraft {
  id: string;
  questionText: string;
  questionType: FormQuestionType;
  isRequired: boolean;
  sortOrder: number;
  helpText?: string | null;
  options?: { text: string; sortOrder: number }[];
}

interface SortableQuestionCardProps {
  question: QuestionDraft;
  onDelete: (id: string) => void;
}

// Question type labels
const questionTypeLabels: Record<FormQuestionType, string> = {
  [FormQuestionType.ShortText]: 'Short Text',
  [FormQuestionType.LongText]: 'Long Text',
  [FormQuestionType.SingleChoice]: 'Single Choice',
  [FormQuestionType.MultipleChoice]: 'Multiple Choice',
  [FormQuestionType.Dropdown]: 'Dropdown',
  [FormQuestionType.Number]: 'Number',
  [FormQuestionType.Date]: 'Date',
  [FormQuestionType.YesNo]: 'Yes/No',
};

// Badge colors by question type
const getQuestionTypeBadgeColor = (type: FormQuestionType): string => {
  switch (type) {
    case FormQuestionType.ShortText:
    case FormQuestionType.LongText:
      return 'bg-blue-100 text-blue-800';
    case FormQuestionType.SingleChoice:
    case FormQuestionType.MultipleChoice:
    case FormQuestionType.Dropdown:
      return 'bg-green-100 text-green-800';
    case FormQuestionType.Number:
      return 'bg-purple-100 text-purple-800';
    case FormQuestionType.Date:
      return 'bg-amber-100 text-amber-800';
    case FormQuestionType.YesNo:
      return 'bg-gray-100 text-gray-800';
    default:
      return 'bg-gray-100 text-gray-800';
  }
};

export function SortableQuestionCard({ question, onDelete }: SortableQuestionCardProps) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: question.id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  return (
    <div ref={setNodeRef} style={style}>
      <Card className={cn('relative', isDragging && 'opacity-50')}>
        <CardContent className="p-4">
          <div className="flex items-start gap-3">
            {/* Drag Handle */}
            <div
              {...attributes}
              {...listeners}
              className="cursor-grab active:cursor-grabbing pt-1"
            >
              <GripVertical className="w-5 h-5 text-gray-400" />
            </div>

            {/* Content */}
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2 mb-2">
                <Badge className={getQuestionTypeBadgeColor(question.questionType)}>
                  {questionTypeLabels[question.questionType]}
                </Badge>
                {question.isRequired && (
                  <span className="text-red-500 text-sm font-medium">Required</span>
                )}
              </div>

              <p className="text-sm font-medium text-gray-900 break-words">
                {question.questionText}
              </p>

              {question.helpText && (
                <p className="text-xs text-gray-500 mt-1">{question.helpText}</p>
              )}

              {question.options && question.options.length > 0 && (
                <div className="mt-2">
                  <p className="text-xs text-gray-500 mb-1">
                    {question.options.length} option{question.options.length !== 1 ? 's' : ''}:
                  </p>
                  <ul className="text-xs text-gray-600 space-y-1">
                    {question.options.slice(0, 3).map((opt, idx) => (
                      <li key={idx} className="truncate">• {opt.text}</li>
                    ))}
                    {question.options.length > 3 && (
                      <li className="text-gray-400">+ {question.options.length - 3} more</li>
                    )}
                  </ul>
                </div>
              )}
            </div>

            {/* Delete Button */}
            <Button
              variant="ghost"
              size="sm"
              onClick={() => onDelete(question.id)}
              className="shrink-0"
            >
              <Trash2 className="w-4 h-4 text-red-500" />
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
