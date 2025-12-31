import { apiClient } from '../client/api-client';
import type {
  BusinessDto,
  SearchBusinessesRequest,
  SearchBusinessesResponse,
} from '../types/business.types';

/**
 * BusinessesRepository
 * Handles all business-related API calls following the repository pattern
 * Phase 6A.59: Landing Page Search - Business search integration
 *
 * Backend endpoints from BusinessesController.cs:
 * - GET /api/businesses - Get all businesses (paginated)
 * - GET /api/businesses/search - Search businesses with filters
 * - GET /api/businesses/{id} - Get business by ID
 * - POST /api/businesses - Create business
 * - PUT /api/businesses/{id} - Update business
 * - DELETE /api/businesses/{id} - Delete business
 */
export class BusinessesRepository {
  private readonly basePath = '/businesses';

  // ==================== PUBLIC QUERIES ====================

  /**
   * Search businesses with optional filters and pagination
   * Maps to backend SearchBusinessesQuery
   *
   * Backend controller parameters (BusinessesController.cs:145):
   * - searchTerm: string (text search across name, description, tags)
   * - category: string (category name)
   * - city: string (city filter)
   * - province: string (province filter)
   * - latitude/longitude/radiusKm: geospatial search
   * - minRating: decimal (minimum rating filter)
   * - isVerified: bool (verified businesses only)
   * - pageNumber: int (default: 1)
   * - pageSize: int (default: 10, max: 50)
   */
  async search(request: SearchBusinessesRequest = {}): Promise<SearchBusinessesResponse> {
    const params = new URLSearchParams();

    if (request.searchTerm) params.append('searchTerm', request.searchTerm);
    if (request.category) params.append('category', request.category);
    if (request.city) params.append('city', request.city);
    if (request.province) params.append('province', request.province);
    if (request.latitude !== undefined) params.append('latitude', String(request.latitude));
    if (request.longitude !== undefined) params.append('longitude', String(request.longitude));
    if (request.radiusKm !== undefined) params.append('radiusKm', String(request.radiusKm));
    if (request.minRating !== undefined) params.append('minRating', String(request.minRating));
    if (request.isVerified !== undefined) params.append('isVerified', String(request.isVerified));

    // Pagination
    params.append('pageNumber', String(request.pageNumber ?? 1));
    params.append('pageSize', String(request.pageSize ?? 20));

    const queryString = params.toString();
    const url = queryString ? `${this.basePath}/search?${queryString}` : `${this.basePath}/search`;

    return await apiClient.get<SearchBusinessesResponse>(url);
  }

  /**
   * Get business by ID
   * Maps to backend GetBusinessByIdQuery
   */
  async getBusinessById(id: string): Promise<BusinessDto> {
    return await apiClient.get<BusinessDto>(`${this.basePath}/${id}`);
  }

  /**
   * Get all businesses with pagination
   * Maps to backend GetAllBusinessesQuery
   */
  async getAllBusinesses(pageNumber: number = 1, pageSize: number = 20): Promise<SearchBusinessesResponse> {
    const params = new URLSearchParams({
      pageNumber: String(pageNumber),
      pageSize: String(pageSize),
    });

    return await apiClient.get<SearchBusinessesResponse>(`${this.basePath}?${params.toString()}`);
  }

  // ==================== AUTHENTICATED MUTATIONS ====================

  // Note: Create, Update, Delete methods can be added here when needed
  // For Phase 6A.59, we only need search functionality
}

/**
 * Singleton instance of the businesses repository
 * Export for use in React components and hooks
 */
export const businessesRepository = new BusinessesRepository();
