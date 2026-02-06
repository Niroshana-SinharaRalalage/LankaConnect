/**
 * Phase 6A.90: Admin Support Ticket Types
 * DTOs for admin support ticket management endpoints
 */

export interface SupportTicketDto {
  id: string;
  referenceId: string;
  name: string;
  email: string;
  subject: string;
  status: string;
  priority: string;
  assignedToUserId: string | null;
  assignedToName: string | null;
  replyCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface SupportTicketDetailsDto {
  id: string;
  referenceId: string;
  name: string;
  email: string;
  subject: string;
  message: string;
  status: string;
  priority: string;
  assignedToUserId: string | null;
  assignedToName: string | null;
  createdAt: string;
  updatedAt: string | null;
  replies: SupportTicketReplyDto[];
  notes: SupportTicketNoteDto[];
}

export interface SupportTicketReplyDto {
  id: string;
  content: string;
  adminUserId: string;
  adminUserName: string;
  createdAt: string;
}

export interface SupportTicketNoteDto {
  id: string;
  content: string;
  adminUserId: string;
  adminUserName: string;
  createdAt: string;
}

export interface SupportTicketStatisticsDto {
  totalTickets: number;
  newTickets: number;
  inProgressTickets: number;
  waitingForResponseTickets: number;
  resolvedTickets: number;
  closedTickets: number;
  unassignedTickets: number;
  ticketsByPriority: Record<string, number>;
}

export interface PagedSupportTicketsResult {
  items: SupportTicketDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface GetSupportTicketsRequest {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: SupportTicketStatus | null;
  priority?: SupportTicketPriority | null;
  assignedTo?: string | null;
  unassignedOnly?: boolean | null;
}

export interface ReplyToTicketRequest {
  content: string;
}

export interface UpdateTicketStatusRequest {
  status: SupportTicketStatus;
}

export interface AssignTicketRequest {
  assignToUserId: string;
}

export interface AddNoteRequest {
  content: string;
}

export type SupportTicketStatus =
  | 'New'
  | 'InProgress'
  | 'WaitingForResponse'
  | 'Resolved'
  | 'Closed';

export type SupportTicketPriority =
  | 'Low'
  | 'Normal'
  | 'High'
  | 'Urgent';

export const TICKET_STATUS_OPTIONS: { value: SupportTicketStatus; label: string }[] = [
  { value: 'New', label: 'New' },
  { value: 'InProgress', label: 'In Progress' },
  { value: 'WaitingForResponse', label: 'Waiting for Response' },
  { value: 'Resolved', label: 'Resolved' },
  { value: 'Closed', label: 'Closed' },
];

export const TICKET_PRIORITY_OPTIONS: { value: SupportTicketPriority; label: string }[] = [
  { value: 'Low', label: 'Low' },
  { value: 'Normal', label: 'Normal' },
  { value: 'High', label: 'High' },
  { value: 'Urgent', label: 'Urgent' },
];

export const STATUS_BADGE_COLORS: Record<string, { bg: string; text: string }> = {
  New: { bg: '#DBEAFE', text: '#1D4ED8' },
  InProgress: { bg: '#FEF3C7', text: '#D97706' },
  WaitingForResponse: { bg: '#E5E7EB', text: '#374151' },
  Resolved: { bg: '#D1FAE5', text: '#059669' },
  Closed: { bg: '#F3F4F6', text: '#6B7280' },
};

export const PRIORITY_BADGE_COLORS: Record<string, { bg: string; text: string }> = {
  Low: { bg: '#E5E7EB', text: '#374151' },
  Normal: { bg: '#DBEAFE', text: '#1D4ED8' },
  High: { bg: '#FEF3C7', text: '#D97706' },
  Urgent: { bg: '#FEE2E2', text: '#DC2626' },
};
