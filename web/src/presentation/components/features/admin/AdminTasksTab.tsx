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
      {/* Horizontal Tab Navigation - Consistent underline style */}
      <div className="flex border-b border-gray-200 overflow-x-auto" style={{ scrollbarWidth: 'thin' }}>
        <TabButton
          active={activeSection === 'approvals'}
          onClick={() => setActiveSection('approvals')}
          icon={ClipboardCheck}
          label="Pending Approvals"
          badge={pendingApprovals.length > 0 ? pendingApprovals.length : undefined}
        />
        <TabButton
          active={activeSection === 'users'}
          onClick={() => setActiveSection('users')}
          icon={Users}
          label="User Management"
        />
        <TabButton
          active={activeSection === 'support'}
          onClick={() => setActiveSection('support')}
          icon={MessageSquare}
          label="Support/Feedback"
        />
        <TabButton
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

// Tab button with underline style - matches main TabPanel styling
function TabButton({
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
      {badge !== undefined && badge > 0 && (
        <span className="ml-1 px-2 py-0.5 text-xs font-medium rounded-full bg-[#8B1538] text-white">
          {badge}
        </span>
      )}
    </button>
  );
}