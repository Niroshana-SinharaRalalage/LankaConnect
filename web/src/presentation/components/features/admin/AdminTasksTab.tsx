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
 */

'use client';

import { useState } from 'react';
import {
  ClipboardCheck,
  Users,
  MessageSquare,
  Activity,
  ChevronRight,
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

  const sections = [
    {
      id: 'approvals' as AdminSection,
      label: 'Pending Approvals',
      icon: ClipboardCheck,
      badge: pendingApprovals.length > 0 ? pendingApprovals.length : undefined,
    },
    {
      id: 'users' as AdminSection,
      label: 'User Management',
      icon: Users,
    },
    {
      id: 'support' as AdminSection,
      label: 'Support/Feedback',
      icon: MessageSquare,
    },
    {
      id: 'email-metrics' as AdminSection,
      label: 'Email Metrics',
      icon: Activity,
    },
  ];

  return (
    <div className="flex flex-col lg:flex-row gap-6">
      {/* Sidebar Navigation */}
      <div className="lg:w-64 flex-shrink-0">
        <nav className="bg-gray-50 rounded-lg p-2 space-y-1">
          {sections.map((section) => {
            const Icon = section.icon;
            const isActive = activeSection === section.id;

            return (
              <button
                key={section.id}
                onClick={() => setActiveSection(section.id)}
                className={`w-full flex items-center justify-between px-3 py-2.5 rounded-md transition-colors ${
                  isActive
                    ? 'bg-white text-[#8B1538] shadow-sm'
                    : 'text-gray-600 hover:bg-white hover:text-gray-900'
                }`}
              >
                <div className="flex items-center gap-2.5">
                  <Icon className={`w-4 h-4 ${isActive ? 'text-[#8B1538]' : 'text-gray-400'}`} />
                  <span className="text-sm font-medium">{section.label}</span>
                </div>
                <div className="flex items-center gap-2">
                  {section.badge && (
                    <span className="px-2 py-0.5 text-xs font-medium bg-[#8B1538] text-white rounded-full">
                      {section.badge}
                    </span>
                  )}
                  <ChevronRight className={`w-4 h-4 ${isActive ? 'text-[#8B1538]' : 'text-gray-300'}`} />
                </div>
              </button>
            );
          })}
        </nav>
      </div>

      {/* Content Area */}
      <div className="flex-1 min-w-0">
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
