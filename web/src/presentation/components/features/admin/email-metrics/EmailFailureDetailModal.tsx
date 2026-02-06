/**
 * Phase 6A.99: Email Failure Detail Modal
 * Shows full error message and failure details with copy-to-clipboard functionality
 */

'use client';

import { useState } from 'react';
import { X, AlertTriangle, Mail, Calendar, Code, Copy, Check, Hash } from 'lucide-react';
import type { EmailFailureDto } from '@/infrastructure/api/types/email-metrics.types';

interface EmailFailureDetailModalProps {
  isOpen: boolean;
  failure: EmailFailureDto | null;
  onClose: () => void;
}

export function EmailFailureDetailModal({
  isOpen,
  failure,
  onClose,
}: EmailFailureDetailModalProps) {
  const [copied, setCopied] = useState(false);

  if (!isOpen || !failure) return null;

  const handleCopy = async () => {
    const text = `
Correlation ID: ${failure.correlationId || 'N/A'}
Template: ${failure.templateName}
Recipient: ${failure.recipientEmail}
Handler: ${failure.handlerName || 'N/A'}
Timestamp: ${new Date(failure.timestamp).toISOString()}

Error Message:
${failure.errorMessage}
    `.trim();

    try {
      await navigator.clipboard.writeText(text);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (error) {
      console.error('Failed to copy to clipboard:', error);
    }
  };

  const formatDate = (dateString: string) => {
    try {
      return new Date(dateString).toLocaleString();
    } catch {
      return dateString;
    }
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-2xl max-h-[90vh] overflow-hidden flex flex-col">
        {/* Header */}
        <div
          className="px-6 py-4 flex items-center justify-between"
          style={{ background: 'linear-gradient(135deg, #8B1538 0%, #FF7900 100%)' }}
        >
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-white/20 rounded-lg flex items-center justify-center">
              <AlertTriangle className="w-5 h-5 text-white" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-white">Email Failure Details</h2>
              <p className="text-sm text-white/80">{failure.templateName}</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-2 text-white/80 hover:text-white hover:bg-white/10 rounded-lg transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-6 space-y-4">
          {/* Metadata Grid */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="flex items-start gap-2 text-sm">
              <Calendar className="w-4 h-4 text-gray-400 mt-0.5 flex-shrink-0" />
              <div>
                <span className="text-gray-500 block">Timestamp</span>
                <span className="font-medium">{formatDate(failure.timestamp)}</span>
              </div>
            </div>
            <div className="flex items-start gap-2 text-sm">
              <Mail className="w-4 h-4 text-gray-400 mt-0.5 flex-shrink-0" />
              <div>
                <span className="text-gray-500 block">Recipient</span>
                <span className="font-medium break-all">{failure.recipientEmail}</span>
              </div>
            </div>
            <div className="flex items-start gap-2 text-sm">
              <Code className="w-4 h-4 text-gray-400 mt-0.5 flex-shrink-0" />
              <div>
                <span className="text-gray-500 block">Handler</span>
                <span className="font-medium">{failure.handlerName || 'N/A'}</span>
              </div>
            </div>
            {failure.correlationId && (
              <div className="flex items-start gap-2 text-sm">
                <Hash className="w-4 h-4 text-gray-400 mt-0.5 flex-shrink-0" />
                <div>
                  <span className="text-gray-500 block">Correlation ID</span>
                  <span className="font-mono text-xs break-all">{failure.correlationId}</span>
                </div>
              </div>
            )}
          </div>

          {/* Error Message */}
          <div>
            <h3 className="text-sm font-medium text-gray-700 mb-2">Error Message</h3>
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
              <pre className="text-sm text-red-700 whitespace-pre-wrap break-words font-mono">
                {failure.errorMessage}
              </pre>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="px-6 py-4 bg-gray-50 border-t border-gray-200 flex justify-end gap-3">
          <button
            onClick={handleCopy}
            className="flex items-center gap-2 px-4 py-2 text-gray-600 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
          >
            {copied ? (
              <Check className="w-4 h-4 text-green-500" />
            ) : (
              <Copy className="w-4 h-4" />
            )}
            {copied ? 'Copied!' : 'Copy to Clipboard'}
          </button>
          <button
            onClick={onClose}
            className="px-4 py-2 text-white rounded-md transition-colors"
            style={{ background: 'linear-gradient(90deg, #FF7900 0%, #8B1538 100%)' }}
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
}
