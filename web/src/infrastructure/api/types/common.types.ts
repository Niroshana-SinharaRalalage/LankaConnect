/**
 * Common API Types
 * Shared DTOs used across multiple API domains
 */

/**
 * Generic paged result container for paginated queries
 * Matches backend PagedResult<T> from LankaConnect.Application.Common.Models
 */
export interface PagedResult<T> {
  items: readonly T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

/**
 * Pagination request parameters
 */
export interface PaginationRequest {
  page?: number;
  pageSize?: number;
}

/**
 * Generic API response wrapper
 */
export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
}

/**
 * Generic paginated list container (C# PaginatedList<T> format)
 * Matches backend PaginatedList<T> from LankaConnect.Application.Common.Models
 * Used by Business search and other paginated endpoints
 */
export interface PaginatedList<T> {
  items: readonly T[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
