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

        // Check if this is a 401 error and we haven't already retried
        if (error.response?.status === 401 && !originalRequest._retry) {
          // Skip refresh for auth endpoints (login, register, refresh)
          const isAuthEndpoint = originalRequest.url?.includes('/Auth/login') ||
                                  originalRequest.url?.includes('/Auth/register') ||
                                  originalRequest.url?.includes('/Auth/refresh');

          if (!isAuthEndpoint) {
            console.log('🔓 401 Unauthorized - attempting token refresh...');

            // Mark that we've tried to refresh for this request
            originalRequest._retry = true;

            try {
              // Attempt to refresh the token
              const newToken = await tokenRefreshService.refreshAccessToken();

              if (newToken) {
                // Update the Authorization header with the new token
                originalRequest.headers['Authorization'] = `Bearer ${newToken}`;

                console.log('🔄 Retrying request with new token...');

                // Retry the original request
                return this.axiosInstance(originalRequest);
              }
            } catch (refreshError) {
              console.error('❌ Token refresh failed, redirecting to login');
              // Token refresh failed - user will be redirected to login by tokenRefreshService
              return Promise.reject(refreshError);
            }
          }
        }

        // PHASE 6A.10: Comprehensive error logging
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
        });
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
      const message = data?.message || data?.error || axiosError.message || 'An error occurred';

      // Handle specific status codes
      switch (status) {
        case 400:
          return new ValidationError(message, data?.errors || data?.validationErrors);
        case 401:
          // Token expired or invalid - trigger logout callback
          if (this.onUnauthorized) {
            this.onUnauthorized();
          }
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
          return new ApiError(message, status);
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
