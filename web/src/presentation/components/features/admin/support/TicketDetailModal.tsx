/**
 * Phase 6A.90: Ticket Detail Modal
 * Modal for viewing ticket details and sending replies
 */

'use client';

import { useState } from 'react';
import { format } from 'date-fns';
import { X, Mail, User, Calendar, MessageSquare, FileText, Send, StickyNote } from 'lucide-react';
import type { SupportTicketDetailsDto, SupportTicketStatus } from '@/infrastructure/api/types/admin-support.types';
import { STATUS_BADGE_COLORS, PRIORITY_BADGE_COLORS, TICKET_STATUS_OPTIONS } from '@/infrastructure/api/types/admin-support.types';

interface TicketDetailModalProps {
  isOpen: boolean;
  ticket: SupportTicketDetailsDto | null;
  isLoading: boolean;
  onClose: () => void;
  onReply: (content: string) => Promise<void>;
  onAddNote: (content: string) => Promise<void>;
  onUpdateStatus: (status: SupportTicketStatus) => Promise<void>;
  isReplying: boolean;
  isAddingNote: boolean;
  isUpdatingStatus: boolean;
}

export function TicketDetailModal({
  isOpen,
  ticket,
  isLoading,
  onClose,
  onReply,
  onAddNote,
  onUpdateStatus,
  isReplying,
  isAddingNote,
  isUpdatingStatus,
}: TicketDetailModalProps) {
  const [replyContent, setReplyContent] = useState('');
  const [noteContent, setNoteContent] = useState('');
  const [activeTab, setActiveTab] = useState<'details' | 'replies' | 'notes'>('details');

  if (!isOpen) return null;

  const handleReply = async () => {
    if (!replyContent.trim()) return;
    await onReply(replyContent);
    setReplyContent('');
  };

  const handleAddNote = async () => {
    if (!noteContent.trim()) return;
    await onAddNote(noteContent);
    setNoteContent('');
  };

  const statusColors = ticket ? (STATUS_BADGE_COLORS[ticket.status] || STATUS_BADGE_COLORS.New) : STATUS_BADGE_COLORS.New;
  const priorityColors = ticket ? (PRIORITY_BADGE_COLORS[ticket.priority] || PRIORITY_BADGE_COLORS.Normal) : PRIORITY_BADGE_COLORS.Normal;

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-4xl max-h-[90vh] overflow-hidden flex flex-col">
        {/* Header */}
        <div
          className="px-6 py-4 flex items-center justify-between"
          style={{ background: 'linear-gradient(135deg, #8B1538 0%, #FF7900 100%)' }}
        >
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-white/20 rounded-lg flex items-center justify-center">
              <MessageSquare className="w-5 h-5 text-white" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-white">
                {isLoading ? 'Loading...' : ticket?.referenceId || 'Ticket Details'}
              </h2>
              {ticket && (
                <p className="text-sm text-white/80">{ticket.subject}</p>
              )}
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-2 text-white/80 hover:text-white hover:bg-white/10 rounded-lg transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {isLoading ? (
          <div className="flex-1 flex items-center justify-center py-12">
            <div className="w-8 h-8 border-4 border-gray-200 border-t-[#8B1538] rounded-full animate-spin" />
          </div>
        ) : ticket ? (
          <>
            {/* Status and Priority Badges */}
            <div className="px-6 py-3 bg-gray-50 border-b border-gray-200 flex items-center gap-4 flex-wrap">
              <div className="flex items-center gap-2">
                <span className="text-xs text-gray-500 uppercase">Status:</span>
                <span
                  className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                  style={{ backgroundColor: statusColors.bg, color: statusColors.text }}
                >
                  {TICKET_STATUS_OPTIONS.find(s => s.value === ticket.status)?.label || ticket.status}
                </span>
              </div>
              <div className="flex items-center gap-2">
                <span className="text-xs text-gray-500 uppercase">Priority:</span>
                <span
                  className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                  style={{ backgroundColor: priorityColors.bg, color: priorityColors.text }}
                >
                  {ticket.priority}
                </span>
              </div>
              {ticket.status !== 'Closed' && (
                <div className="ml-auto">
                  <select
                    value={ticket.status}
                    onChange={(e) => onUpdateStatus(e.target.value as SupportTicketStatus)}
                    disabled={isUpdatingStatus}
                    className="text-sm border border-gray-300 rounded-md px-2 py-1 focus:outline-none focus:ring-2 focus:ring-[#8B1538]"
                  >
                    {TICKET_STATUS_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </div>
              )}
            </div>

            {/* Tabs */}
            <div className="px-6 border-b border-gray-200">
              <div className="flex gap-4">
                <button
                  onClick={() => setActiveTab('details')}
                  className={`py-3 px-1 text-sm font-medium border-b-2 transition-colors ${
                    activeTab === 'details'
                      ? 'border-[#8B1538] text-[#8B1538]'
                      : 'border-transparent text-gray-500 hover:text-gray-700'
                  }`}
                >
                  <FileText className="w-4 h-4 inline mr-1" />
                  Details
                </button>
                <button
                  onClick={() => setActiveTab('replies')}
                  className={`py-3 px-1 text-sm font-medium border-b-2 transition-colors ${
                    activeTab === 'replies'
                      ? 'border-[#8B1538] text-[#8B1538]'
                      : 'border-transparent text-gray-500 hover:text-gray-700'
                  }`}
                >
                  <MessageSquare className="w-4 h-4 inline mr-1" />
                  Replies ({ticket.replies.length})
                </button>
                <button
                  onClick={() => setActiveTab('notes')}
                  className={`py-3 px-1 text-sm font-medium border-b-2 transition-colors ${
                    activeTab === 'notes'
                      ? 'border-[#8B1538] text-[#8B1538]'
                      : 'border-transparent text-gray-500 hover:text-gray-700'
                  }`}
                >
                  <StickyNote className="w-4 h-4 inline mr-1" />
                  Notes ({ticket.notes.length})
                </button>
              </div>
            </div>

            {/* Content */}
            <div className="flex-1 overflow-y-auto p-6">
              {activeTab === 'details' && (
                <div className="space-y-6">
                  {/* Submitter Info */}
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div className="flex items-center gap-3 p-3 bg-gray-50 rounded-lg">
                      <User className="w-5 h-5 text-gray-400" />
                      <div>
                        <p className="text-xs text-gray-500">Submitter</p>
                        <p className="text-sm font-medium text-gray-900">{ticket.name}</p>
                      </div>
                    </div>
                    <div className="flex items-center gap-3 p-3 bg-gray-50 rounded-lg">
                      <Mail className="w-5 h-5 text-gray-400" />
                      <div>
                        <p className="text-xs text-gray-500">Email</p>
                        <p className="text-sm font-medium text-gray-900">{ticket.email}</p>
                      </div>
                    </div>
                    <div className="flex items-center gap-3 p-3 bg-gray-50 rounded-lg">
                      <Calendar className="w-5 h-5 text-gray-400" />
                      <div>
                        <p className="text-xs text-gray-500">Submitted</p>
                        <p className="text-sm font-medium text-gray-900">
                          {format(new Date(ticket.createdAt), 'PPpp')}
                        </p>
                      </div>
                    </div>
                    <div className="flex items-center gap-3 p-3 bg-gray-50 rounded-lg">
                      <User className="w-5 h-5 text-gray-400" />
                      <div>
                        <p className="text-xs text-gray-500">Assigned To</p>
                        <p className="text-sm font-medium text-gray-900">
                          {ticket.assignedToName || <span className="text-amber-600">Unassigned</span>}
                        </p>
                      </div>
                    </div>
                  </div>

                  {/* Message */}
                  <div>
                    <h3 className="text-sm font-medium text-gray-700 mb-2">Message</h3>
                    <div className="p-4 bg-gray-50 rounded-lg">
                      <p className="text-sm text-gray-900 whitespace-pre-wrap">{ticket.message}</p>
                    </div>
                  </div>
                </div>
              )}

              {activeTab === 'replies' && (
                <div className="space-y-4">
                  {/* Replies List */}
                  {ticket.replies.length === 0 ? (
                    <div className="text-center py-8 text-gray-500">
                      <MessageSquare className="w-12 h-12 mx-auto mb-2 text-gray-300" />
                      <p>No replies yet</p>
                    </div>
                  ) : (
                    <div className="space-y-4">
                      {ticket.replies.map((reply) => (
                        <div key={reply.id} className="p-4 bg-blue-50 rounded-lg border border-blue-100">
                          <div className="flex items-center justify-between mb-2">
                            <span className="text-sm font-medium text-blue-800">
                              {reply.adminUserName}
                            </span>
                            <span className="text-xs text-blue-600">
                              {format(new Date(reply.createdAt), 'PPpp')}
                            </span>
                          </div>
                          <p className="text-sm text-gray-900 whitespace-pre-wrap">{reply.content}</p>
                        </div>
                      ))}
                    </div>
                  )}

                  {/* Reply Form */}
                  {ticket.status !== 'Closed' && (
                    <div className="mt-6 pt-4 border-t border-gray-200">
                      <h4 className="text-sm font-medium text-gray-700 mb-2">Send Reply</h4>
                      <textarea
                        value={replyContent}
                        onChange={(e) => setReplyContent(e.target.value)}
                        placeholder="Type your reply here... (This will be sent to the submitter via email)"
                        rows={4}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-[#8B1538] focus:border-[#8B1538]"
                      />
                      <div className="mt-2 flex justify-end">
                        <button
                          onClick={handleReply}
                          disabled={!replyContent.trim() || isReplying}
                          className="flex items-center gap-2 px-4 py-2 text-white bg-[#8B1538] rounded-md hover:bg-[#6d1029] transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                          {isReplying ? (
                            <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                          ) : (
                            <Send className="w-4 h-4" />
                          )}
                          Send Reply
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              )}

              {activeTab === 'notes' && (
                <div className="space-y-4">
                  {/* Notes List */}
                  {ticket.notes.length === 0 ? (
                    <div className="text-center py-8 text-gray-500">
                      <StickyNote className="w-12 h-12 mx-auto mb-2 text-gray-300" />
                      <p>No internal notes yet</p>
                    </div>
                  ) : (
                    <div className="space-y-4">
                      {ticket.notes.map((note) => (
                        <div key={note.id} className="p-4 bg-amber-50 rounded-lg border border-amber-100">
                          <div className="flex items-center justify-between mb-2">
                            <span className="text-sm font-medium text-amber-800">
                              {note.adminUserName}
                            </span>
                            <span className="text-xs text-amber-600">
                              {format(new Date(note.createdAt), 'PPpp')}
                            </span>
                          </div>
                          <p className="text-sm text-gray-900 whitespace-pre-wrap">{note.content}</p>
                        </div>
                      ))}
                    </div>
                  )}

                  {/* Add Note Form */}
                  <div className="mt-6 pt-4 border-t border-gray-200">
                    <h4 className="text-sm font-medium text-gray-700 mb-2">Add Internal Note</h4>
                    <p className="text-xs text-gray-500 mb-2">
                      Internal notes are only visible to admin users
                    </p>
                    <textarea
                      value={noteContent}
                      onChange={(e) => setNoteContent(e.target.value)}
                      placeholder="Add an internal note..."
                      rows={3}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-[#8B1538] focus:border-[#8B1538]"
                    />
                    <div className="mt-2 flex justify-end">
                      <button
                        onClick={handleAddNote}
                        disabled={!noteContent.trim() || isAddingNote}
                        className="flex items-center gap-2 px-4 py-2 text-white bg-amber-600 rounded-md hover:bg-amber-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                      >
                        {isAddingNote ? (
                          <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                        ) : (
                          <StickyNote className="w-4 h-4" />
                        )}
                        Add Note
                      </button>
                    </div>
                  </div>
                </div>
              )}
            </div>
          </>
        ) : (
          <div className="flex-1 flex items-center justify-center py-12">
            <p className="text-gray-500">Ticket not found</p>
          </div>
        )}
      </div>
    </div>
  );
}
