/**
 * Phase 6A.89: Email Template List Component
 * Displays a searchable, filterable list of email templates for admin management
 */

'use client';

import { useState, useCallback, useRef, useEffect } from 'react';
import { Search, Mail, CheckCircle, XCircle, Edit, Eye, RefreshCw, Filter } from 'lucide-react';
import { useEmailTemplates } from '@/presentation/hooks/useEmailTemplateManagement';
import type { EmailTemplateListItemDto, EmailTemplateCategory } from '@/infrastructure/api/types/email-template-management.types';

interface EmailTemplateListProps {
  onSelectTemplate: (templateName: string, templateId?: string) => void;
  onPreviewTemplate: (template: EmailTemplateListItemDto) => void;
}

export function EmailTemplateList({ onSelectTemplate, onPreviewTemplate }: EmailTemplateListProps) {
  const [searchTerm, setSearchTerm] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [categoryFilter, setCategoryFilter] = useState<string>('');
  const [activeFilter, setActiveFilter] = useState<boolean | undefined>(undefined);
  const [page, setPage] = useState(1);
  const searchTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  // Cleanup search timeout on unmount
  useEffect(() => {
    return () => {
      if (searchTimeoutRef.current) {
        clearTimeout(searchTimeoutRef.current);
      }
    };
  }, []);

  // Debounced search
  const handleSearch = useCallback((value: string) => {
    setSearchInput(value);
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current);
    }
    searchTimeoutRef.current = setTimeout(() => {
      setSearchTerm(value);
      setPage(1);
    }, 300);
  }, []);

  const { data, isLoading, refetch } = useEmailTemplates({
    search: searchTerm || undefined,
    category: categoryFilter || undefined,
    isActive: activeFilter,
    page,
    pageSize: 20,
  });

  return (
    <div className="space-y-4">
      {/* Search and Filters */}
      <div className="flex flex-col sm:flex-row gap-3">
        {/* Search Input */}
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
          <input
            type="text"
            placeholder="Search templates..."
            value={searchInput}
            onChange={(e) => handleSearch(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-gray-200 rounded-md focus:outline-none focus:ring-2 focus:ring-[#8B1538] focus:border-transparent"
          />
        </div>

        {/* Category Filter */}
        <select
          value={categoryFilter}
          onChange={(e) => {
            setCategoryFilter(e.target.value);
            setPage(1);
          }}
          className="px-3 py-2 border border-gray-200 rounded-md focus:outline-none focus:ring-2 focus:ring-[#8B1538] text-sm"
        >
          <option value="">All Categories</option>
          <option value="Authentication">Authentication</option>
          <option value="Business">Business</option>
          <option value="Marketing">Marketing</option>
          <option value="Notification">Notification</option>
          <option value="System">System</option>
        </select>

        {/* Active Filter */}
        <select
          value={activeFilter === undefined ? '' : activeFilter.toString()}
          onChange={(e) => {
            const val = e.target.value;
            setActiveFilter(val === '' ? undefined : val === 'true');
            setPage(1);
          }}
          className="px-3 py-2 border border-gray-200 rounded-md focus:outline-none focus:ring-2 focus:ring-[#8B1538] text-sm"
        >
          <option value="">All Status</option>
          <option value="true">Active</option>
          <option value="false">Inactive</option>
        </select>

        {/* Refresh Button */}
        <button
          onClick={() => refetch()}
          disabled={isLoading}
          className="flex items-center gap-2 px-4 py-2 text-gray-600 bg-gray-100 rounded-md hover:bg-gray-200 transition-colors disabled:opacity-50"
        >
          <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} />
        </button>
      </div>

      {/* Template List */}
      <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
        {isLoading ? (
          <div className="flex items-center justify-center py-12">
            <div className="w-8 h-8 border-4 border-gray-200 border-t-[#8B1538] rounded-full animate-spin" />
          </div>
        ) : data?.templates.length === 0 ? (
          <div className="p-8 text-center text-gray-500">
            <Mail className="w-12 h-12 mx-auto mb-3 text-gray-300" />
            <p>No templates found</p>
          </div>
        ) : (
          <div className="divide-y divide-gray-200">
            {data?.templates.map((template) => (
              <TemplateRow
                key={template.name}
                template={template}
                onEdit={() => onSelectTemplate(template.name)}
                onPreview={() => onPreviewTemplate(template)}
              />
            ))}
          </div>
        )}
      </div>

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between px-4 py-3 bg-gray-50 rounded-lg">
          <div className="text-sm text-gray-600">
            Showing {(data.pageNumber - 1) * data.pageSize + 1} to{' '}
            {Math.min(data.pageNumber * data.pageSize, data.totalCount)} of {data.totalCount}
          </div>
          <div className="flex gap-2">
            <button
              onClick={() => setPage(page - 1)}
              disabled={!data.hasPreviousPage}
              className="px-3 py-1 text-sm text-gray-600 bg-white border border-gray-200 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Previous
            </button>
            <button
              onClick={() => setPage(page + 1)}
              disabled={!data.hasNextPage}
              className="px-3 py-1 text-sm text-gray-600 bg-white border border-gray-200 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Next
            </button>
          </div>
        </div>
      )}

      {/* Category Counts */}
      {data?.categoryCounts && Object.keys(data.categoryCounts).length > 0 && (
        <div className="flex flex-wrap gap-2 text-xs text-gray-500">
          {Object.entries(data.categoryCounts).map(([category, count]) => (
            <span
              key={category}
              className="px-2 py-1 bg-gray-100 rounded"
            >
              {category}: {count}
            </span>
          ))}
        </div>
      )}
    </div>
  );
}

function TemplateRow({
  template,
  onEdit,
  onPreview,
}: {
  template: EmailTemplateListItemDto;
  onEdit: () => void;
  onPreview: () => void;
}) {
  return (
    <div className="px-4 py-3 hover:bg-gray-50 flex items-center justify-between gap-4">
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <h4 className="text-sm font-medium text-gray-900 truncate">{template.displayName || template.name}</h4>
          {template.isActive ? (
            <span className="inline-flex items-center gap-1 px-2 py-0.5 text-xs font-medium bg-green-100 text-green-700 rounded-full">
              <CheckCircle className="w-3 h-3" />
              Active
            </span>
          ) : (
            <span className="inline-flex items-center gap-1 px-2 py-0.5 text-xs font-medium bg-gray-100 text-gray-600 rounded-full">
              <XCircle className="w-3 h-3" />
              Inactive
            </span>
          )}
        </div>
        <p className="text-xs text-gray-500 truncate mt-0.5">{template.description}</p>
        <div className="flex items-center gap-2 mt-1">
          <span className="text-xs text-gray-400 bg-gray-50 px-1.5 py-0.5 rounded">
            {template.category}
          </span>
          <span className="text-xs text-gray-400">
            Subject: {template.subject.substring(0, 50)}{template.subject.length > 50 ? '...' : ''}
          </span>
        </div>
      </div>
      <div className="flex items-center gap-2">
        <button
          onClick={onPreview}
          className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded transition-colors"
          title="Preview template"
        >
          <Eye className="w-4 h-4" />
        </button>
        <button
          onClick={onEdit}
          className="p-2 text-gray-400 hover:text-[#8B1538] hover:bg-red-50 rounded transition-colors"
          title="Edit template"
        >
          <Edit className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
}
