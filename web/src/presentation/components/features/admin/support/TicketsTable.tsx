/**
 * Phase 6A.90: Support Tickets Table
 * Table component for displaying support tickets
 */

'use client';

import { formatDistanceToNow } from 'date-fns';
import { MessageSquare, MoreVertical, Eye, Send, CheckCircle, XCircle } from 'lucide-react';
import type { SupportTicketDto, SupportTicketStatus } from '@/infrastructure/api/types/admin-support.types';
import { STATUS_BADGE_COLORS, PRIORITY_BADGE_COLORS, TICKET_STATUS_OPTIONS } from '@/infrastructure/api/types/admin-support.types';
import { useState, useRef, useEffect } from 'react';

interface TicketsTableProps {
  tickets: SupportTicketDto[];
  onViewDetails: (ticket: SupportTicketDto) => void;
  onUpdateStatus: (ticketId: string, status: SupportTicketStatus) => void;
  loadingTicketId: string | null;
}

export function TicketsTable({
  tickets,
  onViewDetails,
  onUpdateStatus,
  loadingTicketId,
}: TicketsTableProps) {
  const [openMenuId, setOpenMenuId] = useState<string | null>(null);
  const menuRef = useRef<HTMLDivElement>(null);

  // Close dropdown when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setOpenMenuId(null);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  if (tickets.length === 0) {
    return (
      <div className="text-center py-12">
        <MessageSquare className="w-12 h-12 text-gray-300 mx-auto mb-4" />
        <p className="text-gray-500">No support tickets found</p>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full">
        <thead>
          <tr className="border-b border-gray-200 bg-gray-50">
            <th className="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">
              Ticket
            </th>
            <th className="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">
              Status
            </th>
            <th className="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">
              Priority
            </th>
            <th className="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">
              Assigned To
            </th>
            <th className="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">
              Replies
            </th>
            <th className="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">
              Created
            </th>
            <th className="px-6 py-3 text-right text-xs font-semibold text-gray-600 uppercase tracking-wider">
              Actions
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-200">
          {tickets.map((ticket) => {
            const isLoading = loadingTicketId === ticket.id;
            const statusColors = STATUS_BADGE_COLORS[ticket.status] || STATUS_BADGE_COLORS.New;
            const priorityColors = PRIORITY_BADGE_COLORS[ticket.priority] || PRIORITY_BADGE_COLORS.Normal;

            return (
              <tr
                key={ticket.id}
                className={`hover:bg-gray-50 transition-colors ${isLoading ? 'opacity-50' : ''}`}
              >
                {/* Ticket Info */}
                <td className="px-6 py-4">
                  <div className="flex flex-col">
                    <button
                      onClick={() => onViewDetails(ticket)}
                      className="text-sm font-medium text-[#8B1538] hover:underline text-left"
                    >
                      {ticket.referenceId}
                    </button>
                    <span className="text-sm text-gray-900 mt-1">{ticket.subject}</span>
                    <span className="text-xs text-gray-500 mt-1">
                      {ticket.name} &lt;{ticket.email}&gt;
                    </span>
                  </div>
                </td>

                {/* Status */}
                <td className="px-6 py-4">
                  <span
                    className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                    style={{
                      backgroundColor: statusColors.bg,
                      color: statusColors.text,
                    }}
                  >
                    {TICKET_STATUS_OPTIONS.find(s => s.value === ticket.status)?.label || ticket.status}
                  </span>
                </td>

                {/* Priority */}
                <td className="px-6 py-4">
                  <span
                    className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                    style={{
                      backgroundColor: priorityColors.bg,
                      color: priorityColors.text,
                    }}
                  >
                    {ticket.priority}
                  </span>
                </td>

                {/* Assigned To */}
                <td className="px-6 py-4">
                  <span className="text-sm text-gray-600">
                    {ticket.assignedToName || (
                      <span className="text-amber-600 font-medium">Unassigned</span>
                    )}
                  </span>
                </td>

                {/* Reply Count */}
                <td className="px-6 py-4">
                  <div className="flex items-center gap-1 text-sm text-gray-600">
                    <MessageSquare className="w-4 h-4" />
                    <span>{ticket.replyCount}</span>
                  </div>
                </td>

                {/* Created At */}
                <td className="px-6 py-4">
                  <span className="text-sm text-gray-600">
                    {formatDistanceToNow(new Date(ticket.createdAt), { addSuffix: true })}
                  </span>
                </td>

                {/* Actions */}
                <td className="px-6 py-4 text-right">
                  <div className="relative" ref={openMenuId === ticket.id ? menuRef : null}>
                    <button
                      onClick={() => setOpenMenuId(openMenuId === ticket.id ? null : ticket.id)}
                      disabled={isLoading}
                      className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-md transition-colors disabled:opacity-50"
                    >
                      <MoreVertical className="w-4 h-4" />
                    </button>

                    {openMenuId === ticket.id && (
                      <div className="absolute right-0 mt-1 w-48 bg-white rounded-md shadow-lg border border-gray-200 z-10">
                        <div className="py-1">
                          <button
                            onClick={() => {
                              onViewDetails(ticket);
                              setOpenMenuId(null);
                            }}
                            className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                          >
                            <Eye className="w-4 h-4" />
                            View Details
                          </button>

                          {ticket.status !== 'InProgress' && ticket.status !== 'Closed' && (
                            <button
                              onClick={() => {
                                onUpdateStatus(ticket.id, 'InProgress');
                                setOpenMenuId(null);
                              }}
                              className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                            >
                              <Send className="w-4 h-4" />
                              Mark In Progress
                            </button>
                          )}

                          {ticket.status !== 'Resolved' && ticket.status !== 'Closed' && (
                            <button
                              onClick={() => {
                                onUpdateStatus(ticket.id, 'Resolved');
                                setOpenMenuId(null);
                              }}
                              className="w-full flex items-center gap-2 px-4 py-2 text-sm text-green-600 hover:bg-gray-100"
                            >
                              <CheckCircle className="w-4 h-4" />
                              Mark Resolved
                            </button>
                          )}

                          {ticket.status !== 'Closed' && (
                            <button
                              onClick={() => {
                                onUpdateStatus(ticket.id, 'Closed');
                                setOpenMenuId(null);
                              }}
                              className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-500 hover:bg-gray-100"
                            >
                              <XCircle className="w-4 h-4" />
                              Close Ticket
                            </button>
                          )}
                        </div>
                      </div>
                    )}
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
