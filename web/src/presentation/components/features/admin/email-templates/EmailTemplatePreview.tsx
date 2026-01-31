/**
 * Phase 6A.89: Email Template Preview Component
 * Modal for previewing email templates with sample data
 */

'use client';

import { useState } from 'react';
import { X, Mail, FileText, Code, Send } from 'lucide-react';
import { usePreviewEmailTemplate } from '@/presentation/hooks/useEmailTemplateManagement';
import type { EmailTemplateListItemDto, TemplatePreviewResponse } from '@/infrastructure/api/types/email-template-management.types';

interface EmailTemplatePreviewProps {
  template: EmailTemplateListItemDto | null;
  onClose: () => void;
}

export function EmailTemplatePreview({ template, onClose }: EmailTemplatePreviewProps) {
  const [previewParams, setPreviewParams] = useState<Record<string, string>>({});
  const [previewResult, setPreviewResult] = useState<TemplatePreviewResponse | null>(null);
  const [viewMode, setViewMode] = useState<'html' | 'text'>('html');
  const previewMutation = usePreviewEmailTemplate();

  if (!template) return null;

  // Combine required and optional parameters
  const allParams = [...template.requiredParameters, ...template.optionalParameters];

  const handleParamChange = (key: string, value: string) => {
    setPreviewParams((prev) => ({ ...prev, [key]: value }));
  };

  const handlePreview = async () => {
    // Note: The preview API needs the template ID, but we only have the name here
    // In a real implementation, we'd need to fetch the ID first or update the API
    // For now, we'll show the template content with parameter substitution

    const subject = substituteParams(template.subject, previewParams);
    const textBody = substituteParams(template.plainTextTemplate, previewParams);
    const htmlBody = template.htmlTemplate ? substituteParams(template.htmlTemplate, previewParams) : null;

    setPreviewResult({
      subject,
      textBody,
      htmlBody,
      templateName: template.name,
    });
  };

  const substituteParams = (text: string, params: Record<string, string>): string => {
    let result = text;
    Object.entries(params).forEach(([key, value]) => {
      result = result.replace(new RegExp(`\\{\\{${key}\\}\\}`, 'g'), value || `{{${key}}}`);
      result = result.replace(new RegExp(`\\{${key}\\}`, 'g'), value || `{${key}}`);
    });
    return result;
  };

  // Generate sample values for common parameters
  const getSampleValue = (param: string): string => {
    const samples: Record<string, string> = {
      recipientName: 'John Doe',
      recipientEmail: 'john@example.com',
      userName: 'John Doe',
      userEmail: 'john@example.com',
      eventName: 'Sri Lankan New Year Celebration',
      eventTitle: 'Sri Lankan New Year Celebration',
      businessName: 'LankaConnect',
      companyName: 'LankaConnect',
      supportUrl: 'https://lankaconnect.com/support',
      unsubscribeUrl: 'https://lankaconnect.com/unsubscribe',
      verificationUrl: 'https://lankaconnect.com/verify?token=abc123',
      resetUrl: 'https://lankaconnect.com/reset?token=abc123',
    };
    return samples[param] || `[${param}]`;
  };

  const fillSampleValues = () => {
    const samples: Record<string, string> = {};
    allParams.forEach((param) => {
      samples[param] = getSampleValue(param);
    });
    setPreviewParams(samples);
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-4xl max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-blue-100 rounded-lg">
              <Mail className="w-5 h-5 text-blue-600" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-gray-900">Preview: {template.displayName || template.name}</h2>
              <p className="text-sm text-gray-500">{template.category}</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 p-6">
            {/* Parameters Panel */}
            <div className="lg:col-span-1 space-y-4">
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-medium text-gray-900">Parameters</h3>
                <button
                  onClick={fillSampleValues}
                  className="text-xs text-[#8B1538] hover:text-[#6d1029]"
                >
                  Fill sample values
                </button>
              </div>

              {allParams.length === 0 ? (
                <p className="text-sm text-gray-500">No parameters detected</p>
              ) : (
                <div className="space-y-3">
                  {allParams.map((param) => (
                    <div key={param}>
                      <label className="block text-xs font-medium text-gray-600 mb-1">
                        {`{{${param}}}`}
                        {template.requiredParameters.includes(param) && (
                          <span className="text-red-500 ml-1">*</span>
                        )}
                      </label>
                      <input
                        type="text"
                        value={previewParams[param] || ''}
                        onChange={(e) => handleParamChange(param, e.target.value)}
                        placeholder={getSampleValue(param)}
                        className="w-full px-2 py-1.5 text-sm border border-gray-200 rounded focus:outline-none focus:ring-1 focus:ring-[#8B1538]"
                      />
                    </div>
                  ))}
                </div>
              )}

              <button
                onClick={handlePreview}
                className="w-full flex items-center justify-center gap-2 px-4 py-2 text-sm font-medium text-white bg-[#8B1538] rounded-md hover:bg-[#6d1029] transition-colors"
              >
                <Send className="w-4 h-4" />
                Generate Preview
              </button>
            </div>

            {/* Preview Panel */}
            <div className="lg:col-span-2">
              {previewResult ? (
                <div className="space-y-4">
                  {/* Subject */}
                  <div className="bg-gray-50 rounded-lg p-4">
                    <p className="text-xs font-medium text-gray-500 mb-1">Subject</p>
                    <p className="text-sm font-medium text-gray-900">{previewResult.subject}</p>
                  </div>

                  {/* View Mode Toggle */}
                  {previewResult.htmlBody && (
                    <div className="flex border-b border-gray-200">
                      <button
                        onClick={() => setViewMode('html')}
                        className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 ${
                          viewMode === 'html'
                            ? 'text-[#8B1538] border-[#8B1538]'
                            : 'text-gray-500 border-transparent'
                        }`}
                      >
                        <Code className="w-4 h-4" />
                        HTML
                      </button>
                      <button
                        onClick={() => setViewMode('text')}
                        className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 ${
                          viewMode === 'text'
                            ? 'text-[#8B1538] border-[#8B1538]'
                            : 'text-gray-500 border-transparent'
                        }`}
                      >
                        <FileText className="w-4 h-4" />
                        Plain Text
                      </button>
                    </div>
                  )}

                  {/* Body Content */}
                  <div className="border border-gray-200 rounded-lg overflow-hidden">
                    {viewMode === 'html' && previewResult.htmlBody ? (
                      <iframe
                        srcDoc={previewResult.htmlBody}
                        className="w-full h-96 bg-white"
                        title="Email Preview"
                      />
                    ) : (
                      <pre className="p-4 text-sm text-gray-700 whitespace-pre-wrap font-sans bg-gray-50 h-96 overflow-auto">
                        {previewResult.textBody}
                      </pre>
                    )}
                  </div>
                </div>
              ) : (
                <div className="flex flex-col items-center justify-center h-full text-gray-400 py-12">
                  <Mail className="w-16 h-16 mb-4" />
                  <p className="text-sm">Fill in parameters and click "Generate Preview"</p>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="flex justify-end px-6 py-4 border-t border-gray-200 bg-gray-50">
          <button
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
}
