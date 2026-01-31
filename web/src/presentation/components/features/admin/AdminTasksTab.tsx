/**
 * Phase 6A.89: Consolidated Admin Tasks Tab
 * Wrapper component that contains sub-sections for admin features:
 * - Approvals (Pending Role Upgrades)
 * - User Management
 * - Support/Feedback
 * - Email Metrics
 *
 * This solves Issue #1 (Dashboard Restructuring) and Issue #7 (Nested Tab Limitation)
 * by using section-based navigation instead of nested TabPanels.
 *
 * Updated: Using horizontal tabs for better content width utilization
 */

'use client';

import { useState } from 'react';
import {
  ClipboardCheck,
  Users,
  MessageSquare,
  Activity,
} from 'lucide-react';
import { ApprovalsTable } from './ApprovalsTable';
import { UserManagementTab } from './users';
import { SupportTab } from './support';
import { EmailMetricsTab } from './email-metrics';
import type { PendingRoleUpgradeDto } from '@/infrastructure/api/types/approvals.types';

type AdminSection = 'approvals' | 'users' | 'support' | 'email-metrics';

interface AdminTasksTabProps {
  pendingApprovals: PendingRoleUpgradeDto[];
  loadingApprovals: boolean;
  onApprovalsUpdate: () => Promise<void>;
}

export function AdminTasksTab({
  pendingApprovals,
  loadingApprovals,
  onApprovalsUpdate,
}: AdminTasksTabProps) {
  const [activeSection, setActiveSection] = useState<AdminSection>('approvals');

  return (
    <div className="space-y-6">
      {/* Horizontal Tab Navigation */}
      <div className="flex flex-wrap gap-2">
        <SectionButton
          active={activeSection === 'approvals'}
          onClick={() => setActiveSection('approvals')}
          icon={ClipboardCheck}
          label="Pending Approvals"
          badge={pendingApprovals.length > 0 ? pendingApprovals.length : undefined}
        />
        <SectionButton
          active={activeSection === 'users'}
          onClick={() => setActiveSection('users')}
          icon={Users}
          label="User Management"
        />
        <SectionButton
          active={activeSection === 'support'}
          onClick={() => setActiveSection('support')}
          icon={MessageSquare}
          label="Support/Feedback"
        />
        <SectionButton
          active={activeSection === 'email-metrics'}
          onClick={() => setActiveSection('email-metrics')}
          icon={Activity}
          label="Email Metrics"
        />
      </div>

      {/* Content Area - Full Width */}
      <div className="w-full">
        {/* Approvals Section */}
        {activeSection === 'approvals' && (
          <div>
            <h3 className="text-lg font-semibold mb-4 text-[#8B1538]">
              Pending Role Upgrade Approvals
            </h3>
            <div className="max-h-[600px] overflow-y-auto">
              {loadingApprovals ? (
                <div className="text-center py-8">
                  <div className="inline-block w-6 h-6 border-3 border-gray-200 border-t-[#8B1538] rounded-full animate-spin" />
                  <p className="mt-2 text-gray-600">Loading approvals...</p>
                </div>
              ) : (
                <ApprovalsTable approvals={pendingApprovals} onUpdate={onApprovalsUpdate} />
              )}
            </div>
          </div>
        )}

        {/* User Management Section */}
        {activeSection === 'users' && (
          <div className="max-h-[700px] overflow-y-auto">
            <UserManagementTab />
          </div>
        )}

        {/* Support/Feedback Section */}
        {activeSection === 'support' && (
          <div className="max-h-[700px] overflow-y-auto">
            <SupportTab />
          </div>
        )}

        {/* Email Metrics Section */}
        {activeSection === 'email-metrics' && (
          <div className="max-h-[700px] overflow-y-auto">
            <EmailMetricsTab />
          </div>
        )}
      </div>
    </div>
  );
}

// Horizontal section button component
function SectionButton({
  active,
  onClick,
  icon: Icon,
  label,
  badge,
}: {
  active: boolean;
  onClick: () => void;
  icon: typeof ClipboardCheck;
  label: string;
  badge?: number;
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
      {badge !== undefined && badge > 0 && (
        <span className={`px-2 py-0.5 text-xs font-medium rounded-full ${
          active
            ? 'bg-white/20 text-white'
            : 'bg-[#8B1538] text-white'
        }`}>
          {badge}
        </span>
      )}
    </button>
  );
}