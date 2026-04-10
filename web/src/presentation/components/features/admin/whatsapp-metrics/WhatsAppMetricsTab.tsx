'use client';

/**
 * Phase 7A.5: WhatsApp Metrics Dashboard Tab
 * Admin tab component for monitoring WhatsApp message delivery metrics,
 * template status, and message history.
 * Follows EmailMetricsTab pattern for consistency.
 */

import { useState, useMemo } from 'react';
import {
  MessageCircle,
  CheckCircle,
  XCircle,
  Eye,
  RefreshCw,
  BarChart3,
  FileText,
  History,
  Send,
  ChevronDown,
  ChevronUp,
  Loader2,
  AlertTriangle,
} from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { PhoneInput } from '@/presentation/components/ui/PhoneInput';
import {
  useWhatsAppMetrics,
  useWhatsAppTemplates,
  useWhatsAppMessages,
  useSendTestWhatsApp,
} from '@/presentation/hooks/useWhatsApp';
import type {
  WhatsAppTemplateDto,
  WhatsAppMessageDto,
  GetWhatsAppMessagesFilters,
} from '@/infrastructure/api/types/whatsapp.types';
import { WhatsAppTemplateStatus, WhatsAppTemplateCategory, WhatsAppMessageStatus } from '@/infrastructure/api/types/whatsapp.types';
import { toE164 } from '@/presentation/lib/validators/whatsapp.schemas';

type ActiveSection = 'overview' | 'templates' | 'messages' | 'test';

export function WhatsAppMetricsTab() {
  const [activeSection, setActiveSection] = useState<ActiveSection>('overview');

  // Date range for metrics (default: last 30 days)
  const [dateRange] = useState(() => {
    const to = new Date();
    const from = new Date();
    from.setDate(from.getDate() - 30);
    return { from: from.toISOString(), to: to.toISOString() };
  });

  // Queries
  const {
    data: metrics,
    isLoading: loadingMetrics,
    refetch: refetchMetrics,
  } = useWhatsAppMetrics(dateRange.from, dateRange.to);

  const {
    data: templates,
    isLoading: loadingTemplates,
    refetch: refetchTemplates,
  } = useWhatsAppTemplates();

  const [messageFilters, setMessageFilters] = useState<GetWhatsAppMessagesFilters>({
    page: 1,
    pageSize: 20,
  });

  const {
    data: messages,
    isLoading: loadingMessages,
    refetch: refetchMessages,
  } = useWhatsAppMessages(messageFilters);

  const handleRefreshAll = () => {
    refetchMetrics();
    refetchTemplates();
    refetchMessages();
  };

  const isLoading = loadingMetrics || loadingTemplates || loadingMessages;

  return (
    <div className="space-y-6">
      {/* Header with Navigation and Refresh */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        {/* Section Navigation */}
        <div className="flex border-b border-gray-200 overflow-x-auto" style={{ scrollbarWidth: 'thin' }}>
          <TabButton
            active={activeSection === 'overview'}
            onClick={() => setActiveSection('overview')}
            icon={BarChart3}
            label="Overview"
          />
          <TabButton
            active={activeSection === 'templates'}
            onClick={() => setActiveSection('templates')}
            icon={FileText}
            label="Templates"
          />
          <TabButton
            active={activeSection === 'messages'}
            onClick={() => setActiveSection('messages')}
            icon={History}
            label="Message History"
          />
          <TabButton
            active={activeSection === 'test'}
            onClick={() => setActiveSection('test')}
            icon={Send}
            label="Send Test"
          />
        </div>

        {/* Refresh Button */}
        <button
          onClick={handleRefreshAll}
          disabled={isLoading}
          className="flex items-center gap-2 px-4 py-2 text-gray-600 bg-gray-100 rounded-md hover:bg-gray-200 transition-colors disabled:opacity-50"
        >
          <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} />
          <span>Refresh</span>
        </button>
      </div>

      {/* Overview Section */}
      {activeSection === 'overview' && (
        <OverviewSection metrics={metrics} isLoading={loadingMetrics} />
      )}

      {/* Templates Section */}
      {activeSection === 'templates' && (
        <TemplatesSection templates={templates} isLoading={loadingTemplates} />
      )}

      {/* Messages Section */}
      {activeSection === 'messages' && (
        <MessagesSection
          messages={messages}
          isLoading={loadingMessages}
          filters={messageFilters}
          onFiltersChange={setMessageFilters}
        />
      )}

      {/* Test Send Section */}
      {activeSection === 'test' && (
        <TestSendSection templates={templates} />
      )}
    </div>
  );
}

// ==================== Overview Section ====================

function OverviewSection({
  metrics,
  isLoading,
}: {
  metrics: ReturnType<typeof useWhatsAppMetrics>['data'];
  isLoading: boolean;
}) {
  if (isLoading) return <LoadingSpinner />;

  if (!metrics) {
    return (
      <div className="p-8 text-center text-gray-500">
        No metrics data available. WhatsApp may not be enabled yet.
      </div>
    );
  }

  // Sort template breakdown by count descending
  const templateBreakdown = useMemo(() => {
    return Object.entries(metrics.byTemplate)
      .sort(([, a], [, b]) => b - a);
  }, [metrics.byTemplate]);

  return (
    <div className="space-y-6">
      {/* Summary Stats */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          icon={MessageCircle}
          label="Total Sent"
          value={metrics.totalSent}
          color="blue"
        />
        <StatCard
          icon={CheckCircle}
          label="Delivered"
          value={metrics.totalDelivered}
          subValue={`${metrics.deliveryRate.toFixed(1)}% delivery rate`}
          color="green"
        />
        <StatCard
          icon={Eye}
          label="Read"
          value={metrics.totalRead}
          subValue={`${metrics.readRate.toFixed(1)}% read rate`}
          color="purple"
        />
        <StatCard
          icon={XCircle}
          label="Failed"
          value={metrics.totalFailed}
          color="red"
        />
      </div>

      {/* Quick Stats */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Quick Stats</h3>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
          <QuickStat label="Period" value={`${new Date(metrics.from).toLocaleDateString()} - ${new Date(metrics.to).toLocaleDateString()}`} />
          <QuickStat label="Templates Used" value={Object.keys(metrics.byTemplate).length} />
          <QuickStat
            label="Delivery Rate"
            value={`${metrics.deliveryRate.toFixed(1)}%`}
          />
          <QuickStat
            label="Read Rate"
            value={`${metrics.readRate.toFixed(1)}%`}
          />
        </div>
      </div>

      {/* Template Breakdown */}
      {templateBreakdown.length > 0 && (
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-200">
            <h3 className="text-lg font-semibold text-gray-900">Messages by Template</h3>
          </div>
          <div className="divide-y divide-gray-200">
            {templateBreakdown.map(([templateName, count]) => (
              <div key={templateName} className="px-6 py-3 flex items-center justify-between hover:bg-gray-50">
                <span className="text-sm font-medium text-gray-900">{templateName}</span>
                <span className="text-sm text-gray-600">{count} messages</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

// ==================== Templates Section ====================

function TemplatesSection({
  templates,
  isLoading,
}: {
  templates: WhatsAppTemplateDto[] | undefined;
  isLoading: boolean;
}) {
  const [expandedTemplate, setExpandedTemplate] = useState<string | null>(null);

  if (isLoading) return <LoadingSpinner />;

  if (!templates || templates.length === 0) {
    return (
      <div className="p-8 text-center text-gray-500">
        No WhatsApp templates registered yet.
      </div>
    );
  }

  const approvedCount = templates.filter(t => t.status === WhatsAppTemplateStatus.Approved).length;
  const pendingCount = templates.filter(t => t.status === WhatsAppTemplateStatus.Pending).length;
  const rejectedCount = templates.filter(t => t.status === WhatsAppTemplateStatus.Rejected).length;

  return (
    <div className="space-y-4">
      {/* Summary badges */}
      <div className="flex flex-wrap gap-3">
        <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
          <CheckCircle className="w-3.5 h-3.5" />
          {approvedCount} Approved
        </span>
        <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-amber-100 text-amber-800">
          <AlertTriangle className="w-3.5 h-3.5" />
          {pendingCount} Pending
        </span>
        {rejectedCount > 0 && (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-red-100 text-red-800">
            <XCircle className="w-3.5 h-3.5" />
            {rejectedCount} Rejected
          </span>
        )}
      </div>

      {/* Templates list */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-200">
          <h3 className="text-lg font-semibold text-gray-900">
            WhatsApp Templates
            <span className="ml-2 text-sm font-normal text-gray-500">
              ({templates.length} total)
            </span>
          </h3>
        </div>
        <div className="divide-y divide-gray-200">
          {templates.map((template) => (
            <TemplateRow
              key={template.id}
              template={template}
              isExpanded={expandedTemplate === template.id}
              onToggle={() =>
                setExpandedTemplate(expandedTemplate === template.id ? null : template.id)
              }
            />
          ))}
        </div>
      </div>
    </div>
  );
}

function TemplateRow({
  template,
  isExpanded,
  onToggle,
}: {
  template: WhatsAppTemplateDto;
  isExpanded: boolean;
  onToggle: () => void;
}) {
  const statusBadge = () => {
    switch (template.status) {
      case WhatsAppTemplateStatus.Approved:
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
            <CheckCircle className="w-3 h-3" />
            Approved
          </span>
        );
      case WhatsAppTemplateStatus.Pending:
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-800">
            Pending
          </span>
        );
      case WhatsAppTemplateStatus.Rejected:
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800">
            <XCircle className="w-3 h-3" />
            Rejected
          </span>
        );
    }
  };

  const categoryBadge = template.category === WhatsAppTemplateCategory.Marketing
    ? <span className="px-2 py-0.5 rounded text-xs bg-purple-100 text-purple-700">Marketing</span>
    : <span className="px-2 py-0.5 rounded text-xs bg-blue-100 text-blue-700">Utility</span>;

  return (
    <div>
      <div
        className="px-6 py-4 flex items-center justify-between hover:bg-gray-50 cursor-pointer"
        onClick={onToggle}
      >
        <div className="flex-1">
          <div className="flex items-center gap-2">
            <p className="font-medium text-gray-900 text-sm">{template.displayName}</p>
            {categoryBadge}
          </div>
          <p className="text-xs text-gray-500 mt-0.5">{template.templateName}</p>
        </div>
        <div className="flex items-center gap-4">
          {statusBadge()}
          <span className="text-xs text-gray-400">{template.parameterCount} params</span>
          {isExpanded ? (
            <ChevronUp className="w-5 h-5 text-gray-400" />
          ) : (
            <ChevronDown className="w-5 h-5 text-gray-400" />
          )}
        </div>
      </div>

      {isExpanded && (
        <div className="px-6 py-4 bg-gray-50 border-t border-gray-100 space-y-3">
          {template.headerText && (
            <div>
              <p className="text-xs text-gray-500 font-medium">Header</p>
              <p className="text-sm text-gray-700">{template.headerText}</p>
            </div>
          )}
          <div>
            <p className="text-xs text-gray-500 font-medium">Body</p>
            <p className="text-sm text-gray-700 whitespace-pre-wrap">{template.bodyText}</p>
          </div>
          {template.footerText && (
            <div>
              <p className="text-xs text-gray-500 font-medium">Footer</p>
              <p className="text-sm text-gray-700">{template.footerText}</p>
            </div>
          )}
          {template.parameterNames.length > 0 && (
            <div>
              <p className="text-xs text-gray-500 font-medium">Parameters</p>
              <div className="flex flex-wrap gap-1 mt-1">
                {template.parameterNames.map((param) => (
                  <span key={param} className="px-2 py-0.5 rounded bg-gray-200 text-xs text-gray-700">
                    {param}
                  </span>
                ))}
              </div>
            </div>
          )}
          <div className="grid grid-cols-2 gap-4 pt-2">
            <div>
              <p className="text-xs text-gray-500">Language</p>
              <p className="text-sm font-medium text-gray-700">{template.language}</p>
            </div>
            <div>
              <p className="text-xs text-gray-500">Usable</p>
              <p className="text-sm font-medium text-gray-700">{template.isUsable ? 'Yes' : 'No'}</p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ==================== Messages Section ====================

function MessagesSection({
  messages,
  isLoading,
  filters,
  onFiltersChange,
}: {
  messages: WhatsAppMessageDto[] | undefined;
  isLoading: boolean;
  filters: GetWhatsAppMessagesFilters;
  onFiltersChange: (filters: GetWhatsAppMessagesFilters) => void;
}) {
  if (isLoading) return <LoadingSpinner />;

  return (
    <div className="space-y-4">
      {/* Pagination controls */}
      <div className="flex items-center justify-between">
        <p className="text-sm text-gray-500">
          Page {filters.page || 1} &middot; {messages?.length || 0} messages
        </p>
        <div className="flex gap-2">
          <Button
            variant="outline"
            size="sm"
            disabled={!filters.page || filters.page <= 1}
            onClick={() => onFiltersChange({ ...filters, page: (filters.page || 1) - 1 })}
          >
            Previous
          </Button>
          <Button
            variant="outline"
            size="sm"
            disabled={!messages || messages.length < (filters.pageSize || 20)}
            onClick={() => onFiltersChange({ ...filters, page: (filters.page || 1) + 1 })}
          >
            Next
          </Button>
        </div>
      </div>

      {/* Messages table */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        {!messages || messages.length === 0 ? (
          <div className="p-8 text-center text-gray-500">
            No WhatsApp messages found.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Time</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Phone</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Template</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Retries</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Error</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {messages.map((msg) => (
                  <MessageRow key={msg.id} message={msg} />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}

function MessageRow({ message }: { message: WhatsAppMessageDto }) {
  const statusBadge = () => {
    const statusConfig: Record<string, { bg: string; text: string }> = {
      [WhatsAppMessageStatus.Sent]: { bg: 'bg-blue-100', text: 'text-blue-800' },
      [WhatsAppMessageStatus.Delivered]: { bg: 'bg-green-100', text: 'text-green-800' },
      [WhatsAppMessageStatus.Read]: { bg: 'bg-purple-100', text: 'text-purple-800' },
      [WhatsAppMessageStatus.Failed]: { bg: 'bg-red-100', text: 'text-red-800' },
      [WhatsAppMessageStatus.Sending]: { bg: 'bg-yellow-100', text: 'text-yellow-800' },
      [WhatsAppMessageStatus.Scheduled]: { bg: 'bg-gray-100', text: 'text-gray-800' },
    };

    const config = statusConfig[message.status] || { bg: 'bg-gray-100', text: 'text-gray-800' };
    return (
      <span className={`inline-flex px-2 py-0.5 rounded-full text-xs font-medium ${config.bg} ${config.text}`}>
        {message.status}
      </span>
    );
  };

  const sentTime = message.sentAt || message.failedAt;

  return (
    <tr className="hover:bg-gray-50">
      <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">
        {sentTime ? new Date(sentTime).toLocaleString() : '—'}
      </td>
      <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-700 font-mono">
        {message.toPhoneNumber}
      </td>
      <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-900">
        {message.templateName || '—'}
      </td>
      <td className="px-4 py-3 whitespace-nowrap">
        {statusBadge()}
      </td>
      <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">
        {message.retryCount}
      </td>
      <td className="px-4 py-3 text-sm text-red-600 max-w-xs truncate" title={message.errorMessage || undefined}>
        {message.errorMessage || '—'}
      </td>
    </tr>
  );
}

// ==================== Test Send Section ====================

function TestSendSection({ templates }: { templates: WhatsAppTemplateDto[] | undefined }) {
  const sendTestMutation = useSendTestWhatsApp();
  const [phone, setPhone] = useState('');
  const [selectedTemplate, setSelectedTemplate] = useState('');

  const approvedTemplates = useMemo(
    () => templates?.filter(t => t.isUsable) || [],
    [templates]
  );

  const handleSendTest = async () => {
    if (!phone || !selectedTemplate) return;

    const e164Phone = toE164(phone);
    await sendTestMutation.mutateAsync({
      recipientPhone: e164Phone,
      templateName: selectedTemplate,
      parameters: null,
    });
  };

  return (
    <div className="max-w-lg space-y-6">
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Send Test WhatsApp Message</h3>
        <p className="text-sm text-gray-500 mb-6">
          Send a test message to verify WhatsApp integration. Only approved templates can be used.
        </p>

        <div className="space-y-4">
          <div>
            <label htmlFor="test-phone" className="block text-sm font-medium text-gray-700 mb-1">
              Recipient Phone Number
            </label>
            <PhoneInput
              id="test-phone"
              value={phone}
              onChange={setPhone}
              placeholder="+1 (234) 567-8901"
            />
            <p className="text-xs text-gray-500 mt-1">E.164 format (e.g., +12345678901)</p>
          </div>

          <div>
            <label htmlFor="test-template" className="block text-sm font-medium text-gray-700 mb-1">
              Template
            </label>
            <select
              id="test-template"
              value={selectedTemplate}
              onChange={(e) => setSelectedTemplate(e.target.value)}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-offset-2"
            >
              <option value="">Select a template...</option>
              {approvedTemplates.map((t) => (
                <option key={t.id} value={t.templateName}>
                  {t.displayName} ({t.templateName})
                </option>
              ))}
            </select>
            {approvedTemplates.length === 0 && (
              <p className="text-xs text-amber-600 mt-1">
                No approved templates available. Templates must be approved by Meta before use.
              </p>
            )}
          </div>

          <Button
            onClick={handleSendTest}
            disabled={sendTestMutation.isPending || !phone || !selectedTemplate}
            className="w-full"
            style={{ backgroundColor: '#25D366', color: 'white' }}
          >
            {sendTestMutation.isPending ? (
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
            ) : (
              <Send className="h-4 w-4 mr-2" />
            )}
            Send Test Message
          </Button>

          {sendTestMutation.data && (
            <div className={`p-3 rounded-lg text-sm ${
              sendTestMutation.data.success
                ? 'bg-green-50 text-green-800'
                : 'bg-red-50 text-red-800'
            }`}>
              {sendTestMutation.data.success
                ? `Message sent! ID: ${sendTestMutation.data.messageId}`
                : `Failed: ${sendTestMutation.data.errorMessage}`
              }
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

// ==================== Shared Components ====================

function TabButton({
  active,
  onClick,
  icon: Icon,
  label,
}: {
  active: boolean;
  onClick: () => void;
  icon: typeof BarChart3;
  label: string;
}) {
  return (
    <button
      onClick={onClick}
      className={`
        flex items-center gap-2 px-4 py-3 font-medium text-sm
        transition-all duration-200 whitespace-nowrap
        ${
          active
            ? 'border-b-2 text-[#8B1538]'
            : 'text-gray-600 hover:text-[#FF7900] hover:bg-gray-50'
        }
      `}
      style={
        active
          ? { borderImage: 'linear-gradient(90deg, #FF7900 0%, #8B1538 100%) 1' }
          : undefined
      }
    >
      <Icon className="w-4 h-4" />
      <span>{label}</span>
    </button>
  );
}

function StatCard({
  icon: Icon,
  label,
  value,
  subValue,
  color,
}: {
  icon: typeof MessageCircle;
  label: string;
  value: number | string;
  subValue?: string;
  color: 'blue' | 'green' | 'red' | 'purple' | 'amber';
}) {
  const colorStyles = {
    blue: 'bg-blue-50 text-blue-600',
    green: 'bg-green-50 text-green-600',
    red: 'bg-red-50 text-red-600',
    purple: 'bg-purple-50 text-purple-600',
    amber: 'bg-amber-50 text-amber-600',
  };

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
      <div className="flex items-center gap-3">
        <div className={`p-2 rounded-lg ${colorStyles[color]}`}>
          <Icon className="w-5 h-5" />
        </div>
        <div>
          <p className="text-sm text-gray-500">{label}</p>
          <p className="text-2xl font-semibold text-gray-900">{value}</p>
          {subValue && <p className="text-xs text-gray-400">{subValue}</p>}
        </div>
      </div>
    </div>
  );
}

function QuickStat({ label, value }: { label: string; value: string | number }) {
  return (
    <div>
      <p className="text-sm text-gray-500">{label}</p>
      <p className="text-xl font-semibold text-gray-900">{value}</p>
    </div>
  );
}

function LoadingSpinner() {
  return (
    <div className="flex items-center justify-center py-12">
      <div className="w-8 h-8 border-4 border-gray-200 border-t-[#8B1538] rounded-full animate-spin" />
    </div>
  );
}
