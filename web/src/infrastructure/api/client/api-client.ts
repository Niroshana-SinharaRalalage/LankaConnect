import axios, { AxiosInstance, AxiosError, AxiosRequestConfig, AxiosResponse } from 'axios';
import {
  ApiError,
  NetworkError,
  ValidationError,
  UnauthorizedError,
  ForbiddenError,
  NotFoundError,
  ServerError,
} from './api-errors';
import { tokenRefreshService } from '../services/tokenRefreshService';

/**
 * API Client Configuration
 */
export interface ApiClientConfig {
  baseURL: string;
  timeout?: number;
  headers?: Record<string, string>;
}

/**
 * Callback for handling 401 Unauthorized errors (token expiration)
 */
type UnauthorizedCallback = () => void;

/**
 * API Client
 * Singleton pattern for managing HTTP requests
 */
export class ApiClient {
  private static instance: ApiClient;
  private axiosInstance: AxiosInstance;
  private authToken: string | null = null;
  private onUnauthorized: UnauthorizedCallback | null = null;

  private constructor(config?: Partial<ApiClientConfig>) {
    const baseURL = config?.baseURL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

    this.axiosInstance = axios.create({
      baseURL,
      timeout: config?.timeout || 30000,
      withCredentials: true, // Enable credentials for CORS requests
      headers: {
        'Content-Type': 'application/json',
        ...config?.headers,
      },
    });

    this.setupInterceptors();
  }

  /**
   * Get singleton instance
   */
  public static getInstance(config?: Partial<ApiClientConfig>): ApiClient {
    if (!ApiClient.instance) {
      ApiClient.instance = new ApiClient(config);
    }
    return ApiClient.instance;
  }

  /**
   * Setup request and response interceptors
   */
  private setupInterceptors(): void {
    // Request interceptor
    this.axiosInstance.interceptors.request.use(
      (config) => {
        // Add auth token if available
        if (this.authToken) {
          config.headers.Authorization = `Bearer ${this.authToken}`;
        }

        // PHASE 6A.10: Comprehensive request logging for debugging
        const authHeader = config.headers.Authorization;
        const authValue = typeof authHeader === 'string' && authHeader.startsWith('Bearer ')
          ? `Bearer ${authHeader.substring(7, 30)}...`
          : 'Not set';

        console.log('🚀 API Request:', {
          method: config.method?.toUpperCase(),
          url: config.url,
          baseURL: config.baseURL,
          fullURL: `${config.baseURL}${config.url}`,
          headers: {
            'Content-Type': config.headers['Content-Type'],
            'Authorization': authValue,
            'Origin': config.headers.Origin || (typeof window !== 'undefined' ? window.location.origin : 'SSR'),
          },
          data: config.data ? JSON.stringify(config.data).substring(0, 200) : 'No data',
        });

        return config;
      },
      (error) => {
        console.error('❌ Request Interceptor Error:', error);
        return Promise.reject(this.handleError(error));
      }
    );

    // Response interceptor
    this.axiosInstance.interceptors.response.use(
      (response) => {
        // PHASE 6A.10: Log successful responses
        console.log('✅ API Response Success:', {
          status: response.status,
          statusText: response.statusText,
          url: response.config.url,
          headers: {
            'Access-Control-Allow-Origin': response.headers['access-control-allow-origin'],
            'Access-Control-Allow-Credentials': response.headers['access-control-allow-credentials'],
            'Content-Type': response.headers['content-type'],
          },
          dataSize: JSON.stringify(response.data || {}).length,
        });
        return response;
      },
      async (error) => {
        const originalRequest = error.config;

        // Phase 6A.25: Only log non-401 errors at this point
        // 401 errors are handled silently by token refresh below
        const is401 = error.response?.status === 401;
        if (!is401) {
          console.log('🔍 [AUTH INTERCEPTOR] Response error received:', {
            status: error.response?.status,
            url: originalRequest?.url,
            method: originalRequest?.method,
            hasResponse: !!error.response,
            errorMessage: error.message,
          });
        }

        // Check if this is a 401 error and we haven't already retried
        if (is401 && !originalRequest._retry) {
          // Skip refresh for auth endpoints (login, register, refresh)
          const isAuthEndpoint = originalRequest.url?.includes('/Auth/login') ||
                                  originalRequest.url?.includes('/Auth/register') ||
                                  originalRequest.url?.includes('/Auth/refresh');

          if (!isAuthEndpoint) {
            // Mark that we've tried to refresh for this request
            originalRequest._retry = true;

            try {
              // Attempt to refresh the token silently
              const newToken = await tokenRefreshService.refreshAccessToken();

              if (newToken) {
                // Update the Authorization header with the new token
                originalRequest.headers['Authorization'] = `Bearer ${newToken}`;

                // Phase 6A.25: Silent retry - no logging for successful token refresh
                // Retry the original request
                return this.axiosInstance(originalRequest);
              } else {
                // Token refresh returned null - only log failures
                console.warn('⚠️ [AUTH] Token refresh failed - session may have expired');

                // For cross-origin scenarios (localhost -> staging), the refresh token cookie
                // may not be sent, causing a 400 Bad Request. In this case, we should just
                // reject the request without triggering logout, letting the user continue
                // working with their current (possibly expired) session until they explicitly
                // re-login.
                const isCrossOrigin = typeof window !== 'undefined' &&
                                     window.location.hostname === 'localhost';

                if (!isCrossOrigin && this.onUnauthorized) {
                  this.onUnauthorized();
                }

                return Promise.reject(new Error('Token refresh failed - please try logging in again'));
              }
            } catch (refreshError) {
              // Only log actual refresh errors
              console.warn('⚠️ [AUTH] Token refresh error:', refreshError);

              // For cross-origin scenarios, don't trigger logout on refresh failure
              const isCrossOrigin = typeof window !== 'undefined' &&
                                   window.location.hostname === 'localhost';

              if (!isCrossOrigin && this.onUnauthorized) {
                this.onUnauthorized();
              }

              return Promise.reject(refreshError);
            }
          }
        }

        // PHASE 6A.10: Comprehensive error logging
        // Phase 6A.25: Only log non-401 errors or 401 errors that won't be retried
        // 401 errors that trigger token refresh are expected and handled gracefully
        const is401WithRetry = error.response?.status === 401 && originalRequest._retry;
        const is401FirstAttempt = error.response?.status === 401 && !originalRequest._retry;

        // Skip logging for 401 first attempts (will be handled by token refresh above)
        // and for 401 retry failures (already logged in the refresh flow)
        if (!is401FirstAttempt) {
          console.error('❌ API Response Error:', {
            message: error.message,
            name: error.name,
            code: error.code,
            request: error.request ? {
              method: error.config?.method,
              url: error.config?.url,
              headers: error.config?.headers,
            } : 'No request object',
            response: error.response ? {
              status: error.response.status,
              statusText: error.response.statusText,
              headers: error.response.headers,
              data: error.response.data,
            } : 'No response object',
            isAxiosError: axios.isAxiosError(error),
            wasRetried: is401WithRetry,
          });
        }
        return Promise.reject(this.handleError(error));
      }
    );
  }

  /**
   * Handle errors and convert to custom error types
   */
  private handleError(error: unknown): ApiError {
    if (axios.isAxiosError(error)) {
      const axiosError = error as AxiosError<any>;

      // Network error (no response)
      if (!axiosError.response) {
        return new NetworkError(axiosError.message || 'Network error occurred');
      }

      const { status, data } = axiosError.response;

      // Extract error message
      // Note: Backend returns errors in ProblemDetails format with message in .detail field
      const message = data?.detail || data?.message || data?.error || axiosError.message || 'An error occurred';

      // Handle specific status codes
      switch (status) {
        case 400:
          return new ValidationError(message, data?.errors || data?.validationErrors, axiosError.response);
        case 401:
          // Token expired or invalid
          // Note: onUnauthorized callback is already triggered in the response interceptor
          // after token refresh fails, so we don't call it again here to avoid double redirects
          return new UnauthorizedError(message);
        case 403:
          return new ForbiddenError(message);
        case 404:
          return new NotFoundError(message);
        case 500:
        case 502:
        case 503:
        case 504:
          return new ServerError(message, status);
        default:
          return new ApiError(message, status, undefined, axiosError.response);
      }
    }

    // Unknown error
    if (error instanceof Error) {
      return new ApiError(error.message);
    }

    return new ApiError('An unknown error occurred');
  }

  /**
   * Set callback for handling 401 Unauthorized errors
   */
  public setUnauthorizedCallback(callback: UnauthorizedCallback): void {
    this.onUnauthorized = callback;
  }

  /**
   * Set authentication token
   */
  public setAuthToken(token: string): void {
    this.authToken = token;
  }

  /**
   * Clear authentication token
   */
  public clearAuthToken(): void {
    this.authToken = null;
  }

  /**
   * GET request
   */
  public async get<T = any>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response: AxiosResponse<T> = await this.axiosInstance.get(url, config);
    return response.data;
  }

  /**
   * POST request
   */
  public async post<T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    const response: AxiosResponse<T> = await this.axiosInstance.post(url, data, config);
    return response.data;
  }

  /**
   * PUT request
   */
  public async put<T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    const response: AxiosResponse<T> = await this.axiosInstance.put(url, data, config);
    return response.data;
  }

  /**
   * PATCH request
   */
  public async patch<T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    const response: AxiosResponse<T> = await this.axiosInstance.patch(url, data, config);
    return response.data;
  }

  /**
   * DELETE request
   */
  public async delete<T = any>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response: AxiosResponse<T> = await this.axiosInstance.delete(url, config);
    return response.data;
  }

  /**
   * POST request with multipart/form-data (for file uploads)
   * Note: Deletes Content-Type to let browser set multipart/form-data with boundary
   */
  public async postMultipart<T = any>(
    url: string,
    formData: FormData,
    config?: AxiosRequestConfig
  ): Promise<T> {
    // Delete Content-Type header to let browser set multipart/form-data with boundary
    const response: AxiosResponse<T> = await this.axiosInstance.post(url, formData, {
      ...config,
      headers: {
        ...config?.headers,
        'Content-Type': undefined, // Delete Content-Type so browser sets it correctly
      },
    });
    return response.data;
  }
}

// Export singleton instance
export const apiClient = ApiClient.getInstance();
