/**
 * Phase 6A.90: Admin Support Ticket Repository
 * API methods for admin support ticket management
 */

import { apiClient } from '../client/api-client';
import type {
  SupportTicketDto,
  SupportTicketDetailsDto,
  SupportTicketStatisticsDto,
  PagedSupportTicketsResult,
  GetSupportTicketsRequest,
  ReplyToTicketRequest,
  UpdateTicketStatusRequest,
  AssignTicketRequest,
  AddNoteRequest,
  SupportTicketStatus,
} from '../types/admin-support.types';

const ADMIN_SUPPORT_BASE = '/admin/support-tickets';

export const adminSupportRepository = {
  /**
   * Get paginated list of support tickets
   */
  async getTickets(request: GetSupportTicketsRequest = {}): Promise<PagedSupportTicketsResult> {
    const params = new URLSearchParams();

    if (request.page) params.append('page', request.page.toString());
    if (request.pageSize) params.append('pageSize', request.pageSize.toString());
    if (request.search) params.append('search', request.search);
    if (request.status) params.append('status', request.status);
    if (request.priority) params.append('priority', request.priority);
    if (request.assignedTo) params.append('assignedTo', request.assignedTo);
    if (request.unassignedOnly !== null && request.unassignedOnly !== undefined) {
      params.append('unassignedOnly', request.unassignedOnly.toString());
    }

    const queryString = params.toString();
    const url = queryString ? `${ADMIN_SUPPORT_BASE}?${queryString}` : ADMIN_SUPPORT_BASE;

    const response = await apiClient.get<PagedSupportTicketsResult>(url);
    return response;
  },

  /**
   * Get detailed support ticket by ID
   */
  async getTicketById(ticketId: string): Promise<SupportTicketDetailsDto> {
    const response = await apiClient.get<SupportTicketDetailsDto>(`${ADMIN_SUPPORT_BASE}/${ticketId}`);
    return response;
  },

  /**
   * Get support ticket statistics
   */
  async getStatistics(): Promise<SupportTicketStatisticsDto> {
    const response = await apiClient.get<SupportTicketStatisticsDto>(`${ADMIN_SUPPORT_BASE}/statistics`);
    return response;
  },

  /**
   * Reply to a support ticket
   */
  async replyToTicket(ticketId: string, request: ReplyToTicketRequest): Promise<void> {
    await apiClient.post(`${ADMIN_SUPPORT_BASE}/${ticketId}/reply`, request);
  },

  /**
   * Update support ticket status
   */
  async updateStatus(ticketId: string, status: SupportTicketStatus): Promise<void> {
    const request: UpdateTicketStatusRequest = { status };
    await apiClient.post(`${ADMIN_SUPPORT_BASE}/${ticketId}/status`, request);
  },

  /**
   * Assign support ticket to admin user
   */
  async assignTicket(ticketId: string, assignToUserId: string): Promise<void> {
    const request: AssignTicketRequest = { assignToUserId };
    await apiClient.post(`${ADMIN_SUPPORT_BASE}/${ticketId}/assign`, request);
  },

  /**
   * Add internal note to support ticket
   */
  async addNote(ticketId: string, request: AddNoteRequest): Promise<void> {
    await apiClient.post(`${ADMIN_SUPPORT_BASE}/${ticketId}/notes`, request);
  },
};
