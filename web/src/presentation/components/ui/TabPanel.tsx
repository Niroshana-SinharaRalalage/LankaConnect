'use client';

import * as React from 'react';
import { LucideIcon } from 'lucide-react';

export interface Tab {
  id: string;
  label: string;
  icon?: LucideIcon;
  content: React.ReactNode;
}

export interface TabPanelProps {
  tabs: Tab[];
  defaultTab?: string;
  onChange?: (tabId: string) => void;
  className?: string;
}

/**
 * TabPanel Component
 * Reusable tabbed interface with keyboard navigation and accessibility
 * Features: Sri Lankan flag colors, smooth transitions, keyboard navigation
 * Phase: Epic 1 Dashboard Enhancements
 */
export function TabPanel({ tabs, defaultTab, onChange, className = '' }: TabPanelProps) {
  const [activeTab, setActiveTab] = React.useState<string>(
    defaultTab || (tabs.length > 0 ? tabs[0].id : '')
  );

  const activeTabContent = React.useMemo(
    () => tabs.find((tab) => tab.id === activeTab)?.content,
    [tabs, activeTab]
  );

  const handleTabClick = (tabId: string) => {
    setActiveTab(tabId);
    onChange?.(tabId);
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLDivElement>) => {
    const currentIndex = tabs.findIndex((tab) => tab.id === activeTab);

    if (e.key === 'ArrowRight') {
      e.preventDefault();
      const nextIndex = (currentIndex + 1) % tabs.length;
      const nextTabId = tabs[nextIndex].id;
      setActiveTab(nextTabId);
      onChange?.(nextTabId);
    } else if (e.key === 'ArrowLeft') {
      e.preventDefault();
      const prevIndex = currentIndex === 0 ? tabs.length - 1 : currentIndex - 1;
      const prevTabId = tabs[prevIndex].id;
      setActiveTab(prevTabId);
      onChange?.(prevTabId);
    }
  };

  return (
    <div className={`w-full ${className}`}>
      {/* Tab Navigation */}
      <div
        role="tablist"
        className="flex border-b border-gray-200 overflow-x-auto"
        onKeyDown={handleKeyDown}
        style={{ scrollbarWidth: 'thin' }}
      >
        {tabs.map((tab) => {
          const isActive = tab.id === activeTab;
          const Icon = tab.icon;

          return (
            <button
              key={tab.id}
              role="tab"
              aria-selected={isActive}
              aria-controls={`tabpanel-${tab.id}`}
              id={`tab-${tab.id}`}
              onClick={() => handleTabClick(tab.id)}
              className={`
                flex items-center gap-2 px-4 py-3 font-medium text-sm
                transition-all duration-200 whitespace-nowrap
                ${
                  isActive
                    ? 'border-b-2 text-[#8B1538]'
                    : 'text-gray-600 hover:text-[#FF7900] hover:bg-gray-50'
                }
              `}
              style={
                isActive
                  ? {
                      borderImage: 'linear-gradient(90deg, #FF7900 0%, #8B1538 100%) 1',
                    }
                  : undefined
              }
            >
              {Icon && <Icon className="w-4 h-4" />}
              <span>{tab.label}</span>
            </button>
          );
        })}
      </div>

      {/* Tab Content */}
      <div
        role="tabpanel"
        id={`tabpanel-${activeTab}`}
        aria-labelledby={`tab-${activeTab}`}
        className="mt-6"
      >
        {activeTabContent}
      </div>
    </div>
  );
}
