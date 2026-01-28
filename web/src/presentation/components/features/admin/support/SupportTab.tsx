/**
 * Phase 6A.90: Support Tab
 * Main tab component for admin support ticket management
 */

'use client';

import { useState, useCallback } from 'react';
import { Search, MessageSquare, Clock, CheckCircle, AlertCircle, RefreshCw, ChevronLeft, ChevronRight } from 'lucide-react';
import {
  useSupportTickets,
  useSupportTicketDetails,
  useSupportTicketStatistics,
  useReplyToTicket,
  useUpdateTicketStatus,
  useAddTicketNote,
} from '@/presentation/hooks/useSupportTickets';
import type { SupportTicketDto, GetSupportTicketsRequest, SupportTicketStatus, SupportTicketPriority } from '@/infrastructure/api/types/admin-support.types';
import { TICKET_STATUS_OPTIONS, TICKET_PRIORITY_OPTIONS } from '@/infrastructure/api/types/admin-support.types';
import { TicketsTable } from './TicketsTable';
import { TicketDetailModal } from './TicketDetailModal';

export function SupportTab() {
  // Filter state
  const [filters, setFilters] = useState<GetSupportTicketsRequest>({
    page: 1,
    pageSize: 10,
    search: '',
    status: null,
    priority: null,
    unassignedOnly: null,
  });

  // Modal state
  const [selectedTicketId, setSelectedTicketId] = useState<string | null>(null);
  const [isDetailModalOpen, setIsDetailModalOpen] = useState(false);
  const [loadingTicketId, setLoadingTicketId] = useState<string | null>(null);

  // Debounced search
  const [searchInput, setSearchInput] = useState('');

  // Queries
  const { data: ticketsData, isLoading, error, refetch } = useSupportTickets(filters);
  const { data: statistics } = useSupportTicketStatistics();
  const { data: ticketDetails, isLoading: isLoadingDetails } = useSupportTicketDetails(
    isDetailModalOpen ? selectedTicketId : null
  );

  // Mutations
  const replyMutation = useReplyToTicket();
  const updateStatusMutation = useUpdateTicketStatus();
  const addNoteMutation = useAddTicketNote();

  // Handlers
  const handleSearch = useCallback((value: string) => {
    setSearchInput(value);
    // Debounce search
    const timeoutId = setTimeout(() => {
      setFilters((prev) => ({ ...prev, search: value, page: 1 }));
    }, 300);
    return () => clearTimeout(timeoutId);
  }, []);

  const handleStatusFilter = (status: string) => {
    setFilters((prev) => ({
      ...prev,
      status: status as SupportTicketStatus || null,
      page: 1,
    }));
  };

  const handlePriorityFilter = (priority: string) => {
    setFilters((prev) => ({
      ...prev,
      priority: priority as SupportTicketPriority || null,
      page: 1,
    }));
  };

  const handlePageChange = (newPage: number) => {
    setFilters((prev) => ({ ...prev, page: newPage }));
  };

  const handleViewDetails = (ticket: SupportTicketDto) => {
    setSelectedTicketId(ticket.id);
    setIsDetailModalOpen(true);
  };

  const handleUpdateStatus = async (ticketId: string, status: SupportTicketStatus) => {
    setLoadingTicketId(ticketId);
    try {
      await updateStatusMutation.mutateAsync({ ticketId, status });
    } catch (error) {
      console.error('Failed to update status:', error);
      alert('Failed to update ticket status. Please try again.');
    } finally {
      setLoadingTicketId(null);
    }
  };

  const handleReply = async (content: string) => {
    if (!selectedTicketId) return;
    try {
      await replyMutation.mutateAsync({ ticketId: selectedTicketId, content });
    } catch (error) {
      console.error('Failed to send reply:', error);
      alert('Failed to send reply. Please try again.');
    }
  };

  const handleAddNote = async (content: string) => {
    if (!selectedTicketId) return;
    try {
      await addNoteMutation.mutateAsync({ ticketId: selectedTicketId, content });
    } catch (error) {
      console.error('Failed to add note:', error);
      alert('Failed to add note. Please try again.');
    }
  };

  const handleUpdateStatusFromModal = async (status: SupportTicketStatus) => {
    if (!selectedTicketId) return;
    try {
      await updateStatusMutation.mutateAsync({ ticketId: selectedTicketId, status });
    } catch (error) {
      console.error('Failed to update status:', error);
      alert('Failed to update ticket status. Please try again.');
    }
  };

  // Computed values
  const totalPages = ticketsData ? Math.ceil(ticketsData.totalCount / (filters.pageSize || 10)) : 0;

  return (
    <div className="space-y-6">
      {/* Statistics Cards */}
      {statistics && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <StatCard
            icon={MessageSquare}
            label="Total Tickets"
            value={statistics.totalTickets}
            color="blue"
          />
          <StatCard
            icon={AlertCircle}
            label="New"
            value={statistics.newTickets}
            color="amber"
          />
          <StatCard
            icon={Clock}
            label="In Progress"
            value={statistics.inProgressTickets}
            color="purple"
          />
          <StatCard
            icon={CheckCircle}
            label="Resolved"
            value={statistics.resolvedTickets}
            color="green"
          />
        </div>
      )}

      {/* Filters */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
        <div className="flex flex-col md:flex-row gap-4">
          {/* Search */}
          <div className="flex-1">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                type="text"
                value={searchInput}
                onChange={(e) => handleSearch(e.target.value)}
                placeholder="Search by reference, name, or email..."
                className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-[#8B1538] focus:border-[#8B1538]"
              />
            </div>
          </div>

          {/* Status Filter */}
          <div className="w-full md:w-44">
            <select
              value={filters.status || ''}
              onChange={(e) => handleStatusFilter(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-[#8B1538] focus:border-[#8B1538]"
            >
              <option value="">All Status</option>
              {TICKET_STATUS_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>

          {/* Priority Filter */}
          <div className="w-full md:w-36">
            <select
              value={filters.priority || ''}
              onChange={(e) => handlePriorityFilter(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-[#8B1538] focus:border-[#8B1538]"
            >
              <option value="">All Priority</option>
              {TICKET_PRIORITY_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>

          {/* Refresh Button */}
          <button
            onClick={() => refetch()}
            disabled={isLoading}
            className="flex items-center justify-center gap-2 px-4 py-2 text-gray-600 bg-gray-100 rounded-md hover:bg-gray-200 transition-colors disabled:opacity-50"
          >
            <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} />
            <span className="hidden sm:inline">Refresh</span>
          </button>
        </div>
      </div>

      {/* Tickets Table */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        {isLoading ? (
          <div className="flex items-center justify-center py-12">
            <div className="w-8 h-8 border-4 border-gray-200 border-t-[#8B1538] rounded-full animate-spin" />
          </div>
        ) : error ? (
          <div className="text-center py-12 text-red-600">
            <p>Failed to load tickets. Please try again.</p>
            <button
              onClick={() => refetch()}
              className="mt-4 px-4 py-2 text-sm text-white bg-[#8B1538] rounded-md hover:bg-[#6d1029]"
            >
              Retry
            </button>
          </div>
        ) : ticketsData ? (
          <>
            <TicketsTable
              tickets={ticketsData.items}
              onViewDetails={handleViewDetails}
              onUpdateStatus={handleUpdateStatus}
              loadingTicketId={loadingTicketId}
            />

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex items-center justify-between px-6 py-4 border-t border-gray-200 bg-gray-50">
                <div className="text-sm text-gray-600">
                  Showing {((filters.page || 1) - 1) * (filters.pageSize || 10) + 1} to{' '}
                  {Math.min((filters.page || 1) * (filters.pageSize || 10), ticketsData.totalCount)} of{' '}
                  {ticketsData.totalCount} tickets
                </div>
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => handlePageChange((filters.page || 1) - 1)}
                    disabled={(filters.page || 1) <= 1}
                    className="p-2 text-gray-600 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <ChevronLeft className="w-4 h-4" />
                  </button>
                  <span className="text-sm text-gray-600">
                    Page {filters.page || 1} of {totalPages}
                  </span>
                  <button
                    onClick={() => handlePageChange((filters.page || 1) + 1)}
                    disabled={(filters.page || 1) >= totalPages}
                    className="p-2 text-gray-600 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <ChevronRight className="w-4 h-4" />
                  </button>
                </div>
              </div>
            )}
          </>
        ) : null}
      </div>

      {/* Ticket Detail Modal */}
      <TicketDetailModal
        isOpen={isDetailModalOpen}
        ticket={ticketDetails || null}
        isLoading={isLoadingDetails}
        onClose={() => {
          setIsDetailModalOpen(false);
          setSelectedTicketId(null);
        }}
        onReply={handleReply}
        onAddNote={handleAddNote}
        onUpdateStatus={handleUpdateStatusFromModal}
        isReplying={replyMutation.isPending}
        isAddingNote={addNoteMutation.isPending}
        isUpdatingStatus={updateStatusMutation.isPending}
      />
    </div>
  );
}

// Statistics Card Component
function StatCard({
  icon: Icon,
  label,
  value,
  color,
}: {
  icon: typeof MessageSquare;
  label: string;
  value: number;
  color: 'blue' | 'green' | 'amber' | 'purple';
}) {
  const colorStyles = {
    blue: 'bg-blue-50 text-blue-600',
    green: 'bg-green-50 text-green-600',
    amber: 'bg-amber-50 text-amber-600',
    purple: 'bg-purple-50 text-purple-600',
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
        </div>
      </div>
    </div>
  );
}
