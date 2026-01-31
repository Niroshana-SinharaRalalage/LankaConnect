/**
 * Phase 6A.87: Email Metrics Dashboard Tab
 * Admin tab component for monitoring email sending metrics and migration progress
 */

'use client';

import { useState } from 'react';
import {
  Mail,
  CheckCircle,
  XCircle,
  AlertTriangle,
  RefreshCw,
  TrendingUp,
  Activity,
  Clock,
  BarChart3,
  ChevronDown,
  ChevronUp,
  Settings,
  Info,
} from 'lucide-react';
import { EmailTemplateList, EmailTemplateEditor, EmailTemplatePreview } from '../email-templates';
import type { EmailTemplateListItemDto } from '@/infrastructure/api/types/email-template-management.types';
import {
  useEmailMetricsSummary,
  useEmailTemplateStats,
  useEmailFailures,
  useEmailValidationFailures,
  useEmailMigrationProgress,
  useResetEmailMetrics,
} from '@/presentation/hooks/useEmailMetrics';
import type {
  TemplateStatsDto,
  EmailFailureDto,
  ValidationFailureDto,
  HandlerMigrationStatusDto,
} from '@/infrastructure/api/types/email-metrics.types';

export function EmailMetricsTab() {
  // Phase 6A.89: Added 'manage-templates' section for template editing
  const [activeSection, setActiveSection] = useState<'overview' | 'template-usage' | 'manage-templates' | 'failures' | 'migration'>('overview');
  const [expandedTemplate, setExpandedTemplate] = useState<string | null>(null);

  // Phase 6A.89: State for template management modals
  const [selectedTemplateId, setSelectedTemplateId] = useState<string | null>(null);
  const [previewTemplate, setPreviewTemplate] = useState<EmailTemplateListItemDto | null>(null);

  // Queries
  const { data: summary, isLoading: loadingSummary, refetch: refetchSummary } = useEmailMetricsSummary();
  const { data: templateStats, isLoading: loadingTemplates, refetch: refetchTemplates } = useEmailTemplateStats();
  const { data: failures, isLoading: loadingFailures, refetch: refetchFailures } = useEmailFailures();
  const { data: validationFailures, isLoading: loadingValidation, refetch: refetchValidation } = useEmailValidationFailures();
  const { data: migrationProgress, isLoading: loadingMigration, refetch: refetchMigration } = useEmailMigrationProgress();

  // Mutation for reset (Dev/Staging only)
  const resetMetricsMutation = useResetEmailMetrics();

  const handleRefreshAll = () => {
    refetchSummary();
    refetchTemplates();
    refetchFailures();
    refetchValidation();
    refetchMigration();
  };

  const isLoading = loadingSummary || loadingTemplates || loadingFailures || loadingValidation || loadingMigration;

  return (
    <div className="space-y-6">
      {/* Header with Navigation and Refresh */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        {/* Section Navigation - Phase 6A.89: Added Manage Templates, renamed Templates to Template Usage */}
        <div className="flex flex-wrap gap-2">
          <SectionButton
            active={activeSection === 'overview'}
            onClick={() => setActiveSection('overview')}
            icon={BarChart3}
            label="Overview"
          />
          <SectionButton
            active={activeSection === 'template-usage'}
            onClick={() => setActiveSection('template-usage')}
            icon={Mail}
            label="Template Usage"
          />
          <SectionButton
            active={activeSection === 'manage-templates'}
            onClick={() => setActiveSection('manage-templates')}
            icon={Settings}
            label="Manage Templates"
          />
          <SectionButton
            active={activeSection === 'failures'}
            onClick={() => setActiveSection('failures')}
            icon={AlertTriangle}
            label="Failures"
          />
          <SectionButtonWithTooltip
            active={activeSection === 'migration'}
            onClick={() => setActiveSection('migration')}
            icon={TrendingUp}
            label="Migration"
            tooltip="Developer tool for tracking typed email parameter migration progress"
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
        <div className="space-y-6">
          {/* Summary Stats */}
          {summary && (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              <StatCard
                icon={Mail}
                label="Total Emails Sent"
                value={summary.totalEmailsSent}
                color="blue"
              />
              <StatCard
                icon={CheckCircle}
                label="Successful"
                value={summary.totalSuccesses}
                subValue={`${summary.globalSuccessRate.toFixed(1)}% success rate`}
                color="green"
              />
              <StatCard
                icon={XCircle}
                label="Failed"
                value={summary.totalFailures}
                color="red"
              />
              <StatCard
                icon={TrendingUp}
                label="Migration Progress"
                value={`${summary.migrationProgressPercentage.toFixed(0)}%`}
                subValue={`${summary.handlersUsingTypedParameters}/${summary.totalHandlersRecorded} handlers`}
                color="purple"
              />
            </div>
          )}

          {/* Quick Stats */}
          {summary && (
            <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">Quick Stats</h3>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
                <QuickStat label="Templates Used" value={summary.totalTemplatesUsed} />
                <QuickStat label="Handlers Recorded" value={summary.totalHandlersRecorded} />
                <QuickStat label="Migrated Handlers" value={summary.handlersUsingTypedParameters} />
                <QuickStat
                  label="Last Updated"
                  value={new Date(summary.timestamp).toLocaleTimeString()}
                />
              </div>
            </div>
          )}

          {loadingSummary && <LoadingSpinner />}
        </div>
      )}

      {/* Template Usage Section - Phase 6A.89: Renamed from 'templates' */}
      {activeSection === 'template-usage' && (
        <div className="space-y-4">
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
            <div className="px-6 py-4 border-b border-gray-200">
              <h3 className="text-lg font-semibold text-gray-900">
                Template Statistics
                {templateStats && (
                  <span className="ml-2 text-sm font-normal text-gray-500">
                    ({templateStats.totalTemplates} templates)
                  </span>
                )}
              </h3>
            </div>

            {loadingTemplates ? (
              <LoadingSpinner />
            ) : templateStats?.templates.length === 0 ? (
              <div className="p-8 text-center text-gray-500">
                No template statistics available yet.
              </div>
            ) : (
              <div className="divide-y divide-gray-200">
                {templateStats?.templates.map((template) => (
                  <TemplateStatsRow
                    key={template.templateName}
                    template={template}
                    isExpanded={expandedTemplate === template.templateName}
                    onToggle={() =>
                      setExpandedTemplate(
                        expandedTemplate === template.templateName ? null : template.templateName
                      )
                    }
                  />
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {/* Manage Templates Section - Phase 6A.89: NEW */}
      {activeSection === 'manage-templates' && (
        <div className="space-y-4">
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h3 className="text-lg font-semibold text-gray-900">Email Template Management</h3>
                <p className="text-sm text-gray-500 mt-1">
                  View, edit, and manage email templates. Changes take effect immediately.
                </p>
              </div>
            </div>
            <EmailTemplateList
              onSelectTemplate={(templateName, templateId) => {
                // We need to get the ID from the template - for now use name as ID
                // In production, the list should return IDs
                setSelectedTemplateId(templateId || templateName);
              }}
              onPreviewTemplate={(template) => setPreviewTemplate(template)}
            />
          </div>
        </div>
      )}

      {/* Template Editor Modal - Phase 6A.89 */}
      {selectedTemplateId && (
        <EmailTemplateEditor
          templateId={selectedTemplateId}
          onClose={() => setSelectedTemplateId(null)}
          onSaved={() => setSelectedTemplateId(null)}
        />
      )}

      {/* Template Preview Modal - Phase 6A.89 */}
      {previewTemplate && (
        <EmailTemplatePreview
          template={previewTemplate}
          onClose={() => setPreviewTemplate(null)}
        />
      )}

      {/* Failures Section */}
      {activeSection === 'failures' && (
        <div className="space-y-6">
          {/* Email Failures */}
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
            <div className="px-6 py-4 border-b border-gray-200">
              <h3 className="text-lg font-semibold text-gray-900">
                Recent Email Failures
                {failures && (
                  <span className="ml-2 text-sm font-normal text-gray-500">
                    ({failures.totalCount} total)
                  </span>
                )}
              </h3>
            </div>

            {loadingFailures ? (
              <LoadingSpinner />
            ) : failures?.failures.length === 0 ? (
              <div className="p-8 text-center text-gray-500">
                <CheckCircle className="w-12 h-12 mx-auto mb-3 text-green-500" />
                <p>No recent email failures. Great job!</p>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Time</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Template</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Recipient</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Error</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-200">
                    {failures?.failures.map((failure, index) => (
                      <FailureRow key={index} failure={failure} />
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* Validation Failures */}
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
            <div className="px-6 py-4 border-b border-gray-200">
              <h3 className="text-lg font-semibold text-gray-900">
                Validation Failures
                {validationFailures && (
                  <span className="ml-2 text-sm font-normal text-gray-500">
                    ({validationFailures.totalCount} total)
                  </span>
                )}
              </h3>
            </div>

            {loadingValidation ? (
              <LoadingSpinner />
            ) : validationFailures?.failures.length === 0 ? (
              <div className="p-8 text-center text-gray-500">
                <CheckCircle className="w-12 h-12 mx-auto mb-3 text-green-500" />
                <p>No validation failures. All parameters are valid!</p>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Time</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Handler</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Template</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Errors</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-200">
                    {validationFailures?.failures.map((failure, index) => (
                      <ValidationFailureRow key={index} failure={failure} />
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Migration Section */}
      {activeSection === 'migration' && (
        <div className="space-y-6">
          {/* Migration Progress Bar */}
          {migrationProgress && (
            <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">Migration Progress</h3>
              <div className="mb-4">
                <div className="flex justify-between text-sm text-gray-600 mb-2">
                  <span>{migrationProgress.migratedHandlers} of {migrationProgress.totalHandlers} handlers migrated</span>
                  <span>{migrationProgress.migrationPercentage.toFixed(1)}%</span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-4">
                  <div
                    className="bg-gradient-to-r from-[#FF7900] to-[#8B1538] h-4 rounded-full transition-all duration-500"
                    style={{ width: `${migrationProgress.migrationPercentage}%` }}
                  />
                </div>
              </div>
            </div>
          )}

          {/* Handler Migration Status */}
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
            <div className="px-6 py-4 border-b border-gray-200">
              <h3 className="text-lg font-semibold text-gray-900">Handler Migration Status</h3>
            </div>

            {loadingMigration ? (
              <LoadingSpinner />
            ) : migrationProgress?.handlers.length === 0 ? (
              <div className="p-8 text-center text-gray-500">
                No handler data available yet.
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Handler Name</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Total Emails</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Last Used</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-200">
                    {migrationProgress?.handlers.map((handler) => (
                      <HandlerStatusRow key={handler.handlerName} handler={handler} />
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

// Sub-components

function SectionButton({
  active,
  onClick,
  icon: Icon,
  label,
}: {
  active: boolean;
  onClick: () => void;
  icon: typeof Mail;
  label: string;
}) {
  return (
    <button
      onClick={onClick}
      className={`flex items-center gap-2 px-4 py-2 rounded-md transition-colors ${
        active
          ? 'bg-[#8B1538] text-white'
          : 'bg-white text-gray-600 border border-gray-200 hover:bg-gray-50'
      }`}
    >
      <Icon className="w-4 h-4" />
      <span className="text-sm font-medium">{label}</span>
    </button>
  );
}

// Phase 6A.89: SectionButton with tooltip for Migration tab (Issue #6)
function SectionButtonWithTooltip({
  active,
  onClick,
  icon: Icon,
  label,
  tooltip,
}: {
  active: boolean;
  onClick: () => void;
  icon: typeof Mail;
  label: string;
  tooltip: string;
}) {
  const [showTooltip, setShowTooltip] = useState(false);

  return (
    <div className="relative">
      <button
        onClick={onClick}
        onMouseEnter={() => setShowTooltip(true)}
        onMouseLeave={() => setShowTooltip(false)}
        className={`flex items-center gap-2 px-4 py-2 rounded-md transition-colors ${
          active
            ? 'bg-[#8B1538] text-white'
            : 'bg-white text-gray-600 border border-gray-200 hover:bg-gray-50'
        }`}
      >
        <Icon className="w-4 h-4" />
        <span className="text-sm font-medium">{label}</span>
        <Info className={`w-3 h-3 ${active ? 'text-white/70' : 'text-gray-400'}`} />
      </button>
      {showTooltip && (
        <div className="absolute z-10 top-full left-0 mt-1 w-64 p-2 bg-gray-900 text-white text-xs rounded shadow-lg">
          {tooltip}
        </div>
      )}
    </div>
  );
}

function StatCard({
  icon: Icon,
  label,
  value,
  subValue,
  color,
}: {
  icon: typeof Mail;
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

function TemplateStatsRow({
  template,
  isExpanded,
  onToggle,
}: {
  template: TemplateStatsDto;
  isExpanded: boolean;
  onToggle: () => void;
}) {
  const successRateColor =
    template.successRate >= 95
      ? 'text-green-600'
      : template.successRate >= 80
      ? 'text-amber-600'
      : 'text-red-600';

  return (
    <div>
      <div
        className="px-6 py-4 flex items-center justify-between hover:bg-gray-50 cursor-pointer"
        onClick={onToggle}
      >
        <div className="flex-1">
          <p className="font-medium text-gray-900 text-sm">{template.templateName}</p>
          <p className="text-xs text-gray-500">{template.totalSent} emails sent</p>
        </div>
        <div className="flex items-center gap-6">
          <div className="text-right">
            <p className={`font-semibold ${successRateColor}`}>{template.successRate.toFixed(1)}%</p>
            <p className="text-xs text-gray-400">success rate</p>
          </div>
          <div className="text-right">
            <p className="font-medium text-gray-700">{template.averageDurationMs}ms</p>
            <p className="text-xs text-gray-400">avg duration</p>
          </div>
          {isExpanded ? (
            <ChevronUp className="w-5 h-5 text-gray-400" />
          ) : (
            <ChevronDown className="w-5 h-5 text-gray-400" />
          )}
        </div>
      </div>

      {isExpanded && (
        <div className="px-6 py-4 bg-gray-50 border-t border-gray-100">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div>
              <p className="text-xs text-gray-500">Success Count</p>
              <p className="font-semibold text-green-600">{template.successCount}</p>
            </div>
            <div>
              <p className="text-xs text-gray-500">Failure Count</p>
              <p className="font-semibold text-red-600">{template.failureCount}</p>
            </div>
            <div>
              <p className="text-xs text-gray-500">Validation Failures</p>
              <p className="font-semibold text-amber-600">{template.validationFailures}</p>
            </div>
            <div>
              <p className="text-xs text-gray-500">Template Not Found</p>
              <p className="font-semibold text-gray-600">{template.templateNotFoundCount}</p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function FailureRow({ failure }: { failure: EmailFailureDto }) {
  return (
    <tr className="hover:bg-gray-50">
      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
        {new Date(failure.timestamp).toLocaleString()}
      </td>
      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
        {failure.templateName}
      </td>
      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
        {failure.recipientEmail}
      </td>
      <td className="px-6 py-4 text-sm text-red-600 max-w-xs truncate">
        {failure.errorMessage}
      </td>
    </tr>
  );
}

function ValidationFailureRow({ failure }: { failure: ValidationFailureDto }) {
  return (
    <tr className="hover:bg-gray-50">
      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
        {new Date(failure.timestamp).toLocaleString()}
      </td>
      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
        {failure.handlerName}
      </td>
      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
        {failure.templateName}
      </td>
      <td className="px-6 py-4 text-sm text-amber-600">
        <ul className="list-disc list-inside">
          {failure.errors.map((error, idx) => (
            <li key={idx} className="truncate max-w-xs">{error}</li>
          ))}
        </ul>
      </td>
    </tr>
  );
}

function HandlerStatusRow({ handler }: { handler: HandlerMigrationStatusDto }) {
  return (
    <tr className="hover:bg-gray-50">
      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
        {handler.handlerName}
      </td>
      <td className="px-6 py-4 whitespace-nowrap">
        {handler.usesTypedParameters ? (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
            <CheckCircle className="w-3 h-3" />
            Migrated
          </span>
        ) : (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-800">
            <Clock className="w-3 h-3" />
            Pending
          </span>
        )}
      </td>
      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
        {handler.totalEmails}
      </td>
      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
        {handler.lastUsed ? new Date(handler.lastUsed).toLocaleString() : 'Never'}
      </td>
    </tr>
  );
}
