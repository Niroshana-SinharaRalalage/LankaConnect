/**
 * Phase 6A.89: Email Template Editor Component
 * Modal for editing email template content
 */

'use client';

import { useState, useEffect } from 'react';
import { X, Save, AlertTriangle, CheckCircle, Code, FileText, Eye } from 'lucide-react';
import { useEmailTemplateById, useUpdateEmailTemplate, useToggleEmailTemplateActive } from '@/presentation/hooks/useEmailTemplateManagement';
import type { EmailTemplateDetailDto } from '@/infrastructure/api/types/email-template-management.types';

interface EmailTemplateEditorProps {
  templateId: string | null;
  onClose: () => void;
  onSaved: () => void;
}

type EditorView = 'text' | 'html' | 'preview';

export function EmailTemplateEditor({ templateId, onClose, onSaved }: EmailTemplateEditorProps) {
  const { data: template, isLoading, error } = useEmailTemplateById(templateId);
  const updateMutation = useUpdateEmailTemplate();
  const toggleActiveMutation = useToggleEmailTemplateActive();

  const [subject, setSubject] = useState('');
  const [textTemplate, setTextTemplate] = useState('');
  const [htmlTemplate, setHtmlTemplate] = useState('');
  const [tags, setTags] = useState('');
  const [editorView, setEditorView] = useState<EditorView>('text');
  const [hasChanges, setHasChanges] = useState(false);
  const [showConfirmDeactivate, setShowConfirmDeactivate] = useState(false);

  // Populate form when template loads
  useEffect(() => {
    if (template) {
      setSubject(template.subject);
      setTextTemplate(template.textTemplate);
      setHtmlTemplate(template.htmlTemplate || '');
      setTags(template.tags || '');
      setHasChanges(false);
    }
  }, [template]);

  const handleFieldChange = (setter: (value: string) => void, value: string) => {
    setter(value);
    setHasChanges(true);
  };

  const handleSave = async () => {
    if (!templateId) return;

    try {
      await updateMutation.mutateAsync({
        id: templateId,
        request: {
          subject,
          textTemplate,
          htmlTemplate: htmlTemplate || undefined,
          tags: tags || undefined,
        },
      });
      setHasChanges(false);
      onSaved();
    } catch (err) {
      console.error('Failed to save template:', err);
    }
  };

  const handleToggleActive = async () => {
    if (!templateId || !template) return;

    if (template.isActive) {
      // Deactivating - show confirmation
      setShowConfirmDeactivate(true);
    } else {
      // Activating - do it directly
      try {
        await toggleActiveMutation.mutateAsync({ id: templateId, isActive: true });
        onSaved();
      } catch (err) {
        console.error('Failed to activate template:', err);
      }
    }
  };

  const confirmDeactivate = async () => {
    if (!templateId) return;
    try {
      await toggleActiveMutation.mutateAsync({ id: templateId, isActive: false });
      setShowConfirmDeactivate(false);
      onSaved();
    } catch (err) {
      console.error('Failed to deactivate template:', err);
    }
  };

  if (!templateId) return null;

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-4xl max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
          <div>
            <h2 className="text-lg font-semibold text-gray-900">
              {isLoading ? 'Loading...' : `Edit Template: ${template?.name || ''}`}
            </h2>
            {template && (
              <p className="text-sm text-gray-500 mt-0.5">{template.category} • {template.type}</p>
            )}
          </div>
          <button
            onClick={onClose}
            className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-6">
          {isLoading ? (
            <div className="flex items-center justify-center py-12">
              <div className="w-8 h-8 border-4 border-gray-200 border-t-[#8B1538] rounded-full animate-spin" />
            </div>
          ) : error ? (
            <div className="text-center py-8 text-red-600">
              <AlertTriangle className="w-12 h-12 mx-auto mb-3" />
              <p>Failed to load template</p>
            </div>
          ) : template ? (
            <div className="space-y-6">
              {/* Template Info */}
              <div className="flex items-center justify-between bg-gray-50 rounded-lg p-4">
                <div>
                  <p className="text-sm text-gray-600">{template.description}</p>
                  <div className="flex gap-2 mt-2">
                    {template.requiredParameters.map((param) => (
                      <span key={param} className="px-2 py-0.5 text-xs bg-red-100 text-red-700 rounded">
                        {`{{${param}}}`} required
                      </span>
                    ))}
                    {template.optionalParameters.slice(0, 3).map((param) => (
                      <span key={param} className="px-2 py-0.5 text-xs bg-gray-200 text-gray-600 rounded">
                        {`{{${param}}}`}
                      </span>
                    ))}
                    {template.optionalParameters.length > 3 && (
                      <span className="px-2 py-0.5 text-xs bg-gray-200 text-gray-600 rounded">
                        +{template.optionalParameters.length - 3} more
                      </span>
                    )}
                  </div>
                </div>
                <button
                  onClick={handleToggleActive}
                  disabled={toggleActiveMutation.isPending}
                  className={`flex items-center gap-2 px-3 py-1.5 text-sm font-medium rounded transition-colors ${
                    template.isActive
                      ? 'bg-amber-100 text-amber-700 hover:bg-amber-200'
                      : 'bg-green-100 text-green-700 hover:bg-green-200'
                  }`}
                >
                  {template.isActive ? (
                    <>
                      <AlertTriangle className="w-4 h-4" />
                      Deactivate
                    </>
                  ) : (
                    <>
                      <CheckCircle className="w-4 h-4" />
                      Activate
                    </>
                  )}
                </button>
              </div>

              {/* Subject */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Subject Line
                </label>
                <input
                  type="text"
                  value={subject}
                  onChange={(e) => handleFieldChange(setSubject, e.target.value)}
                  className="w-full px-3 py-2 border border-gray-200 rounded-md focus:outline-none focus:ring-2 focus:ring-[#8B1538] font-mono text-sm"
                  placeholder="Email subject with {{parameters}}"
                />
              </div>

              {/* Editor Tabs */}
              <div>
                <div className="flex border-b border-gray-200">
                  <button
                    onClick={() => setEditorView('text')}
                    className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
                      editorView === 'text'
                        ? 'text-[#8B1538] border-[#8B1538]'
                        : 'text-gray-500 border-transparent hover:text-gray-700'
                    }`}
                  >
                    <FileText className="w-4 h-4" />
                    Plain Text
                  </button>
                  <button
                    onClick={() => setEditorView('html')}
                    className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
                      editorView === 'html'
                        ? 'text-[#8B1538] border-[#8B1538]'
                        : 'text-gray-500 border-transparent hover:text-gray-700'
                    }`}
                  >
                    <Code className="w-4 h-4" />
                    HTML
                  </button>
                  <button
                    onClick={() => setEditorView('preview')}
                    className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
                      editorView === 'preview'
                        ? 'text-[#8B1538] border-[#8B1538]'
                        : 'text-gray-500 border-transparent hover:text-gray-700'
                    }`}
                  >
                    <Eye className="w-4 h-4" />
                    Preview
                  </button>
                </div>

                <div className="mt-4">
                  {editorView === 'text' && (
                    <textarea
                      value={textTemplate}
                      onChange={(e) => handleFieldChange(setTextTemplate, e.target.value)}
                      rows={12}
                      className="w-full px-3 py-2 border border-gray-200 rounded-md focus:outline-none focus:ring-2 focus:ring-[#8B1538] font-mono text-sm resize-y"
                      placeholder="Plain text email content..."
                    />
                  )}
                  {editorView === 'html' && (
                    <textarea
                      value={htmlTemplate}
                      onChange={(e) => handleFieldChange(setHtmlTemplate, e.target.value)}
                      rows={12}
                      className="w-full px-3 py-2 border border-gray-200 rounded-md focus:outline-none focus:ring-2 focus:ring-[#8B1538] font-mono text-sm resize-y"
                      placeholder="<html>...</html>"
                    />
                  )}
                  {editorView === 'preview' && (
                    <div className="border border-gray-200 rounded-md p-4 min-h-[300px] bg-white">
                      {htmlTemplate ? (
                        <div
                          className="prose max-w-none"
                          dangerouslySetInnerHTML={{ __html: htmlTemplate }}
                        />
                      ) : (
                        <pre className="whitespace-pre-wrap text-sm text-gray-700 font-sans">
                          {textTemplate}
                        </pre>
                      )}
                    </div>
                  )}
                </div>
              </div>

              {/* Tags */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Tags (comma-separated)
                </label>
                <input
                  type="text"
                  value={tags}
                  onChange={(e) => handleFieldChange(setTags, e.target.value)}
                  className="w-full px-3 py-2 border border-gray-200 rounded-md focus:outline-none focus:ring-2 focus:ring-[#8B1538] text-sm"
                  placeholder="transactional, important, user-facing"
                />
              </div>
            </div>
          ) : null}
        </div>

        {/* Footer */}
        <div className="flex items-center justify-between px-6 py-4 border-t border-gray-200 bg-gray-50">
          <div className="text-sm text-gray-500">
            {hasChanges && (
              <span className="text-amber-600">Unsaved changes</span>
            )}
          </div>
          <div className="flex gap-3">
            <button
              onClick={onClose}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
            <button
              onClick={handleSave}
              disabled={!hasChanges || updateMutation.isPending}
              className="flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-[#8B1538] rounded-md hover:bg-[#6d1029] transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <Save className="w-4 h-4" />
              {updateMutation.isPending ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        </div>
      </div>

      {/* Confirm Deactivate Dialog */}
      {showConfirmDeactivate && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-60">
          <div className="bg-white rounded-lg shadow-xl p-6 max-w-md mx-4">
            <div className="flex items-center gap-3 mb-4">
              <div className="p-2 bg-amber-100 rounded-full">
                <AlertTriangle className="w-6 h-6 text-amber-600" />
              </div>
              <h3 className="text-lg font-semibold text-gray-900">Deactivate Template?</h3>
            </div>
            <p className="text-gray-600 mb-6">
              Deactivating this template will prevent it from being used to send emails. Are you sure?
            </p>
            <div className="flex gap-3 justify-end">
              <button
                onClick={() => setShowConfirmDeactivate(false)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={confirmDeactivate}
                disabled={toggleActiveMutation.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-amber-600 rounded-md hover:bg-amber-700"
              >
                {toggleActiveMutation.isPending ? 'Deactivating...' : 'Yes, Deactivate'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
