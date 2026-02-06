/**
 * Business API Type Definitions
 * DTOs matching backend API contracts (LankaConnect.Application.Businesses.Common)
 * Phase 6A.59: Landing Page Search - Business search integration
 */

import type { PaginatedList } from './common.types';

// ==================== Enums ====================

/**
 * Business category enum matching backend LankaConnect.Domain.Business.Enums.BusinessCategory
 */
export enum BusinessCategory {
  Restaurant = 0,
  Grocery = 1,
  Services = 2,
  Healthcare = 3,
  Education = 4,
  Technology = 5,
  Retail = 6,
  Tourism = 7,
  RealEstate = 8,
  Finance = 9,
  Legal = 10,
  Transportation = 11,
  Entertainment = 12,
  Beauty = 13,
  Fitness = 14,
  Other = 15,
}

/**
 * Business status enum matching backend LankaConnect.Domain.Business.Enums.BusinessStatus
 */
export enum BusinessStatus {
  Active = 0,
  Inactive = 1,
  Suspended = 2,
  PendingApproval = 3,
}

// ==================== Business DTOs ====================

/**
 * Business DTO
 * Matches backend BusinessDto from LankaConnect.Application.Businesses.Common
 */
export interface BusinessDto {
  id: string;
  name: string;
  description: string;
  contactPhone: string;
  contactEmail: string;
  website: string;
  address: string;
  city: string;
  province: string;
  postalCode: string;
  latitude: number;
  longitude: number;
  category: BusinessCategory;
  status: BusinessStatus;
  rating?: number | null;
  reviewCount: number;
  isVerified: boolean;
  verifiedAt?: string | null; // ISO 8601 date-time
  ownerId: string;
  createdAt: string; // ISO 8601 date-time
  updatedAt?: string | null; // ISO 8601 date-time
  categories: readonly string[];
  tags: readonly string[];
}

// ==================== Request DTOs ====================

/**
 * Search businesses request
 * Matches backend SearchBusinessesQuery parameters
 */
export interface SearchBusinessesRequest {
  searchTerm?: string;
  category?: string; // Category name (e.g., "Restaurant")
  city?: string;
  province?: string;
  latitude?: number;
  longitude?: number;
  radiusKm?: number;
  minRating?: number;
  isVerified?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

// ==================== Response DTOs ====================

/**
 * Search businesses response (paginated)
 * Uses common PaginatedList wrapper
 */
export type SearchBusinessesResponse = PaginatedList<BusinessDto>;
