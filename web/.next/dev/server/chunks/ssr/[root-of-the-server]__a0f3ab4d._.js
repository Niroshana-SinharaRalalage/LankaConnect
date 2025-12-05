module.exports = [
"[externals]/next/dist/compiled/next-server/app-page-turbo.runtime.dev.js [external] (next/dist/compiled/next-server/app-page-turbo.runtime.dev.js, cjs)", ((__turbopack_context__, module, exports) => {

const mod = __turbopack_context__.x("next/dist/compiled/next-server/app-page-turbo.runtime.dev.js", () => require("next/dist/compiled/next-server/app-page-turbo.runtime.dev.js"));

module.exports = mod;
}),
"[project]/src/lib/react-query.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

/**
 * React Query Configuration
 *
 * This file exports a factory function for creating QueryClient instances.
 * DO NOT add 'use client' directive - this is a utility module imported by both
 * Server and Client Components.
 *
 * CRITICAL NEXT.JS 16 + REACT 19 PATTERN:
 * - The QueryClient must be created using useState in the Providers component
 * - Module-level singletons break during SSR/hydration with React 19's automatic batching
 * - See: https://tanstack.com/query/latest/docs/framework/react/guides/nextjs
 */ __turbopack_context__.s([
    "makeQueryClient",
    ()=>makeQueryClient
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$query$2d$core$2f$build$2f$modern$2f$queryClient$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/query-core/build/modern/queryClient.js [app-ssr] (ecmascript)");
;
function makeQueryClient() {
    return new __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$query$2d$core$2f$build$2f$modern$2f$queryClient$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["QueryClient"]({
        defaultOptions: {
            queries: {
                // With SSR, we usually want to set some default staleTime
                // above 0 to avoid refetching immediately on the client
                staleTime: 60 * 1000,
                // Refetch on window focus for data freshness
                refetchOnWindowFocus: true,
                // Only retry once on error (prevents infinite loops)
                retry: 1
            }
        }
    });
}
}),
"[externals]/next/dist/server/app-render/action-async-storage.external.js [external] (next/dist/server/app-render/action-async-storage.external.js, cjs)", ((__turbopack_context__, module, exports) => {

const mod = __turbopack_context__.x("next/dist/server/app-render/action-async-storage.external.js", () => require("next/dist/server/app-render/action-async-storage.external.js"));

module.exports = mod;
}),
"[externals]/next/dist/server/app-render/work-unit-async-storage.external.js [external] (next/dist/server/app-render/work-unit-async-storage.external.js, cjs)", ((__turbopack_context__, module, exports) => {

const mod = __turbopack_context__.x("next/dist/server/app-render/work-unit-async-storage.external.js", () => require("next/dist/server/app-render/work-unit-async-storage.external.js"));

module.exports = mod;
}),
"[externals]/next/dist/server/app-render/work-async-storage.external.js [external] (next/dist/server/app-render/work-async-storage.external.js, cjs)", ((__turbopack_context__, module, exports) => {

const mod = __turbopack_context__.x("next/dist/server/app-render/work-async-storage.external.js", () => require("next/dist/server/app-render/work-async-storage.external.js"));

module.exports = mod;
}),
"[externals]/util [external] (util, cjs)", ((__turbopack_context__, module, exports) => {

const mod = __turbopack_context__.x("util", () => require("util"));

module.exports = mod;
}),
"[externals]/stream [external] (stream, cjs)", ((__turbopack_context__, module, exports) => {

const mod = __turbopack_context__.x("stream", () => require("stream"));

module.exports = mod;
}),
"[externals]/path [external] (path, cjs)", ((__turbopack_context__, module, exports) => {

const mod = __turbopack_context__.x("path", () => require("path"));

module.exports = mod;
}),
"[externals]/http [external] (http, cjs)", ((__turbopack_context__, module, exports) => {

const mod = __turbopack_context__.x("http", () => require("http"));

module.exports = mod;
}),
"[externals]/https [external] (https, cjs)", ((__turbopack_context__, module, exports) => {

const mod = __turbopack_context__.x("https", () => require("https"));

module.exports = mod;
}),
"[externals]/url [external] (url, cjs)", ((__turbopack_context__, module, exports) => {

const mod = __turbopack_context__.x("url", () => require("url"));

module.exports = mod;
}),
"[externals]/fs [external] (fs, cjs)", ((__turbopack_context__, module, exports) => {

const mod = __turbopack_context__.x("fs", () => require("fs"));

module.exports = mod;
}),
"[externals]/crypto [external] (crypto, cjs)", ((__turbopack_context__, module, exports) => {

const mod = __turbopack_context__.x("crypto", () => require("crypto"));

module.exports = mod;
}),
"[externals]/http2 [external] (http2, cjs)", ((__turbopack_context__, module, exports) => {

const mod = __turbopack_context__.x("http2", () => require("http2"));

module.exports = mod;
}),
"[externals]/assert [external] (assert, cjs)", ((__turbopack_context__, module, exports) => {

const mod = __turbopack_context__.x("assert", () => require("assert"));

module.exports = mod;
}),
"[externals]/tty [external] (tty, cjs)", ((__turbopack_context__, module, exports) => {

const mod = __turbopack_context__.x("tty", () => require("tty"));

module.exports = mod;
}),
"[externals]/os [external] (os, cjs)", ((__turbopack_context__, module, exports) => {

const mod = __turbopack_context__.x("os", () => require("os"));

module.exports = mod;
}),
"[externals]/zlib [external] (zlib, cjs)", ((__turbopack_context__, module, exports) => {

const mod = __turbopack_context__.x("zlib", () => require("zlib"));

module.exports = mod;
}),
"[externals]/events [external] (events, cjs)", ((__turbopack_context__, module, exports) => {

const mod = __turbopack_context__.x("events", () => require("events"));

module.exports = mod;
}),
"[project]/src/infrastructure/api/client/api-errors.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

/**
 * Base API Error class
 */ __turbopack_context__.s([
    "ApiError",
    ()=>ApiError,
    "ForbiddenError",
    ()=>ForbiddenError,
    "NetworkError",
    ()=>NetworkError,
    "NotFoundError",
    ()=>NotFoundError,
    "ServerError",
    ()=>ServerError,
    "UnauthorizedError",
    ()=>UnauthorizedError,
    "ValidationError",
    ()=>ValidationError
]);
class ApiError extends Error {
    statusCode;
    validationErrors;
    response;
    constructor(message, statusCode, validationErrors, response){
        super(message);
        this.name = 'ApiError';
        this.statusCode = statusCode;
        this.validationErrors = validationErrors;
        this.response = response;
        Object.setPrototypeOf(this, ApiError.prototype);
    }
}
class NetworkError extends ApiError {
    constructor(message = 'Network error occurred'){
        super(message);
        this.name = 'NetworkError';
        Object.setPrototypeOf(this, NetworkError.prototype);
    }
}
class ValidationError extends ApiError {
    constructor(message = 'Validation failed', validationErrors, response){
        super(message, 400, validationErrors, response);
        this.name = 'ValidationError';
        Object.setPrototypeOf(this, ValidationError.prototype);
    }
}
class UnauthorizedError extends ApiError {
    constructor(message = 'Unauthorized'){
        super(message, 401);
        this.name = 'UnauthorizedError';
        Object.setPrototypeOf(this, UnauthorizedError.prototype);
    }
}
class ForbiddenError extends ApiError {
    constructor(message = 'Forbidden'){
        super(message, 403);
        this.name = 'ForbiddenError';
        Object.setPrototypeOf(this, ForbiddenError.prototype);
    }
}
class NotFoundError extends ApiError {
    constructor(message = 'Resource not found'){
        super(message, 404);
        this.name = 'NotFoundError';
        Object.setPrototypeOf(this, NotFoundError.prototype);
    }
}
class ServerError extends ApiError {
    constructor(message = 'Internal server error', statusCode = 500){
        super(message, statusCode);
        this.name = 'ServerError';
        Object.setPrototypeOf(this, ServerError.prototype);
    }
}
}),
"[project]/src/infrastructure/storage/localStorage.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

/**
 * LocalStorage Utility
 * Type-safe wrapper for localStorage with error handling
 */ __turbopack_context__.s([
    "LocalStorageService",
    ()=>LocalStorageService
]);
const STORAGE_KEYS = {
    ACCESS_TOKEN: 'lankaconnect_access_token',
    REFRESH_TOKEN: 'lankaconnect_refresh_token',
    USER: 'lankaconnect_user'
};
class LocalStorageService {
    /**
   * Check if localStorage is available
   */ static isAvailable() {
        try {
            const test = '__localStorage_test__';
            localStorage.setItem(test, test);
            localStorage.removeItem(test);
            return true;
        } catch  {
            return false;
        }
    }
    /**
   * Get item from localStorage
   */ static getItem(key) {
        if (!this.isAvailable()) {
            console.warn('localStorage is not available');
            return null;
        }
        try {
            const item = localStorage.getItem(key);
            if (!item) return null;
            return JSON.parse(item);
        } catch (error) {
            console.error(`Error reading from localStorage (${key}):`, error);
            return null;
        }
    }
    /**
   * Set item in localStorage
   */ static setItem(key, value) {
        if (!this.isAvailable()) {
            console.warn('localStorage is not available');
            return false;
        }
        try {
            localStorage.setItem(key, JSON.stringify(value));
            return true;
        } catch (error) {
            console.error(`Error writing to localStorage (${key}):`, error);
            return false;
        }
    }
    /**
   * Remove item from localStorage
   */ static removeItem(key) {
        if (!this.isAvailable()) {
            console.warn('localStorage is not available');
            return;
        }
        try {
            localStorage.removeItem(key);
        } catch (error) {
            console.error(`Error removing from localStorage (${key}):`, error);
        }
    }
    /**
   * Clear all items from localStorage
   */ static clear() {
        if (!this.isAvailable()) {
            console.warn('localStorage is not available');
            return;
        }
        try {
            localStorage.clear();
        } catch (error) {
            console.error('Error clearing localStorage:', error);
        }
    }
    // Auth-specific methods
    static getAccessToken() {
        return this.getItem(STORAGE_KEYS.ACCESS_TOKEN);
    }
    static setAccessToken(token) {
        return this.setItem(STORAGE_KEYS.ACCESS_TOKEN, token);
    }
    static getRefreshToken() {
        return this.getItem(STORAGE_KEYS.REFRESH_TOKEN);
    }
    static setRefreshToken(token) {
        return this.setItem(STORAGE_KEYS.REFRESH_TOKEN, token);
    }
    static getUser() {
        return this.getItem(STORAGE_KEYS.USER);
    }
    static setUser(user) {
        return this.setItem(STORAGE_KEYS.USER, user);
    }
    static clearAuth() {
        this.removeItem(STORAGE_KEYS.ACCESS_TOKEN);
        this.removeItem(STORAGE_KEYS.REFRESH_TOKEN);
        this.removeItem(STORAGE_KEYS.USER);
    }
}
}),
"[project]/src/presentation/store/useAuthStore.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "useAuthStore",
    ()=>useAuthStore
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zustand$2f$esm$2f$react$2e$mjs__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/zustand/esm/react.mjs [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zustand$2f$esm$2f$middleware$2e$mjs__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/zustand/esm/middleware.mjs [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$storage$2f$localStorage$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/storage/localStorage.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/client/api-client.ts [app-ssr] (ecmascript)");
;
;
;
;
const useAuthStore = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zustand$2f$esm$2f$react$2e$mjs__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["create"])()((0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zustand$2f$esm$2f$middleware$2e$mjs__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["devtools"])((0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zustand$2f$esm$2f$middleware$2e$mjs__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["persist"])((set, get)=>({
        // Initial state
        user: null,
        accessToken: null,
        refreshToken: null,
        isAuthenticated: false,
        isLoading: false,
        // Set authentication (after login/register)
        setAuth: (user, tokens)=>{
            // Store tokens in localStorage
            __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$storage$2f$localStorage$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["LocalStorageService"].setAccessToken(tokens.accessToken);
            __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$storage$2f$localStorage$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["LocalStorageService"].setRefreshToken(tokens.refreshToken);
            __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$storage$2f$localStorage$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["LocalStorageService"].setUser(user);
            // Set auth token in API client
            __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].setAuthToken(tokens.accessToken);
            set({
                user,
                accessToken: tokens.accessToken,
                refreshToken: tokens.refreshToken,
                isAuthenticated: true,
                isLoading: false
            });
        },
        // Clear authentication (logout)
        clearAuth: ()=>{
            console.log('🔍 [AUTH STORE] clearAuth() called');
            console.trace('🔍 [AUTH STORE] Stack trace:');
            // Clear localStorage
            console.log('🔍 [AUTH STORE] Clearing localStorage');
            __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$storage$2f$localStorage$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["LocalStorageService"].clearAuth();
            // Clear auth token from API client
            console.log('🔍 [AUTH STORE] Clearing API client auth token');
            __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].clearAuthToken();
            console.log('🔍 [AUTH STORE] Setting state to unauthenticated');
            set({
                user: null,
                accessToken: null,
                refreshToken: null,
                isAuthenticated: false,
                isLoading: false
            });
            console.log('🔍 [AUTH STORE] clearAuth() completed');
        },
        // Set loading state
        setLoading: (loading)=>{
            set({
                isLoading: loading
            });
        },
        // Update user data
        updateUser: (userData)=>{
            const currentUser = get().user;
            if (!currentUser) return;
            const updatedUser = {
                ...currentUser,
                ...userData
            };
            __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$storage$2f$localStorage$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["LocalStorageService"].setUser(updatedUser);
            set({
                user: updatedUser
            });
        }
    }), {
    name: 'auth-storage',
    partialize: (state)=>({
            user: state.user,
            accessToken: state.accessToken,
            refreshToken: state.refreshToken,
            isAuthenticated: state.isAuthenticated
        }),
    onRehydrateStorage: ()=>(state)=>{
            // Restore auth token to API client on app load
            if (state?.accessToken) {
                __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].setAuthToken(state.accessToken);
            }
        }
}), {
    name: 'AuthStore'
}));
}),
"[project]/src/infrastructure/api/services/tokenRefreshService.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

/**
 * Token Refresh Service
 * Handles automatic token refresh with retry queue to prevent duplicate refreshes
 *
 * Features:
 * - Automatic retry on 401 Unauthorized
 * - Prevents duplicate refresh requests using a queue
 * - Thread-safe refresh operation
 * - Transparent token refresh without user interaction
 */ __turbopack_context__.s([
    "tokenRefreshService",
    ()=>tokenRefreshService
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/client/api-client.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/store/useAuthStore.ts [app-ssr] (ecmascript)");
;
;
class TokenRefreshService {
    isRefreshing = false;
    refreshSubscribers = [];
    /**
   * Check if token refresh is currently in progress
   */ get isRefreshInProgress() {
        return this.isRefreshing;
    }
    /**
   * Add subscriber to be notified when token refresh completes
   */ subscribeTokenRefresh(callback) {
        this.refreshSubscribers.push(callback);
    }
    /**
   * Notify all subscribers that token refresh is complete
   */ onTokenRefreshed(token) {
        this.refreshSubscribers.forEach((callback)=>callback(token));
        this.refreshSubscribers = [];
    }
    /**
   * Attempt to refresh the access token
   * Returns new access token if successful, null otherwise
   */ async refreshAccessToken() {
        console.log('🔍 [TOKEN REFRESH] refreshAccessToken() called');
        // If refresh is already in progress, queue this request
        if (this.isRefreshing) {
            console.log('🔍 [TOKEN REFRESH] Refresh already in progress, queueing this request');
            return new Promise((resolve)=>{
                this.subscribeTokenRefresh((token)=>{
                    console.log('🔍 [TOKEN REFRESH] Queued request received token:', token ? 'YES' : 'NO');
                    resolve(token);
                });
            });
        }
        console.log('🔍 [TOKEN REFRESH] Starting new refresh operation');
        this.isRefreshing = true;
        try {
            console.log('🔄 [TOKEN REFRESH] Calling POST /Auth/refresh...');
            // Call the refresh endpoint
            // Note: Refresh token is in HttpOnly cookie, backend reads it automatically
            const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post('/Auth/refresh', {});
            console.log('🔍 [TOKEN REFRESH] Response received:', {
                hasAccessToken: !!response?.accessToken,
                tokenExpiresAt: response?.tokenExpiresAt
            });
            const { accessToken, tokenExpiresAt } = response;
            console.log('✅ [TOKEN REFRESH] Token refreshed successfully', {
                expiresAt: tokenExpiresAt,
                tokenLength: accessToken?.length
            });
            // Update auth store with new token
            const { setAuth, user } = __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useAuthStore"].getState();
            console.log('🔍 [TOKEN REFRESH] Auth store state:', {
                hasUser: !!user,
                userId: user?.id
            });
            if (user) {
                console.log('🔍 [TOKEN REFRESH] Updating auth store with new token');
                setAuth(user, {
                    accessToken,
                    refreshToken: '',
                    expiresIn: 1800
                });
            } else {
                console.warn('⚠️ [TOKEN REFRESH] No user in auth store, cannot update token');
            }
            // Notify all queued requests
            console.log('🔍 [TOKEN REFRESH] Notifying queued requests, count:', this.refreshSubscribers.length);
            this.onTokenRefreshed(accessToken);
            this.isRefreshing = false;
            console.log('🔍 [TOKEN REFRESH] Refresh complete, returning new token');
            return accessToken;
        } catch (error) {
            console.error('❌ [TOKEN REFRESH] Token refresh failed with error:', {
                message: error?.message,
                status: error?.statusCode || error?.response?.status,
                response: error?.response?.data
            });
            // Clear auth and notify subscribers
            this.isRefreshing = false;
            console.log('🔍 [TOKEN REFRESH] Notifying queued requests with empty token');
            this.onTokenRefreshed('');
            // Clear auth state - user needs to login again
            console.log('🔍 [TOKEN REFRESH] Clearing auth state via clearAuth()');
            const { clearAuth } = __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useAuthStore"].getState();
            clearAuth();
            console.log('🔍 [TOKEN REFRESH] Returning null');
            return null;
        }
    }
    /**
   * Retry a failed request after refreshing the token
   */ async retryRequestAfterRefresh(originalRequest) {
        const newToken = await this.refreshAccessToken();
        if (!newToken) {
            throw new Error('Token refresh failed - please login again');
        }
        // Retry the original request with the new token
        try {
            return await originalRequest();
        } catch (error) {
            console.error('❌ Retry request failed even after token refresh:', error);
            throw error;
        }
    }
}
const tokenRefreshService = new TokenRefreshService();
}),
"[project]/src/infrastructure/api/client/api-client.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "ApiClient",
    ()=>ApiClient,
    "apiClient",
    ()=>apiClient
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$axios$2f$lib$2f$axios$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/axios/lib/axios.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/client/api-errors.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$services$2f$tokenRefreshService$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/services/tokenRefreshService.ts [app-ssr] (ecmascript)");
;
;
;
class ApiClient {
    static instance;
    axiosInstance;
    authToken = null;
    onUnauthorized = null;
    constructor(config){
        const baseURL = config?.baseURL || ("TURBOPACK compile-time value", "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api") || 'http://localhost:5000/api';
        this.axiosInstance = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$axios$2f$lib$2f$axios$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"].create({
            baseURL,
            timeout: config?.timeout || 30000,
            withCredentials: true,
            headers: {
                'Content-Type': 'application/json',
                ...config?.headers
            }
        });
        this.setupInterceptors();
    }
    /**
   * Get singleton instance
   */ static getInstance(config) {
        if (!ApiClient.instance) {
            ApiClient.instance = new ApiClient(config);
        }
        return ApiClient.instance;
    }
    /**
   * Setup request and response interceptors
   */ setupInterceptors() {
        // Request interceptor
        this.axiosInstance.interceptors.request.use((config)=>{
            // Add auth token if available
            if (this.authToken) {
                config.headers.Authorization = `Bearer ${this.authToken}`;
            }
            // PHASE 6A.10: Comprehensive request logging for debugging
            const authHeader = config.headers.Authorization;
            const authValue = typeof authHeader === 'string' && authHeader.startsWith('Bearer ') ? `Bearer ${authHeader.substring(7, 30)}...` : 'Not set';
            console.log('🚀 API Request:', {
                method: config.method?.toUpperCase(),
                url: config.url,
                baseURL: config.baseURL,
                fullURL: `${config.baseURL}${config.url}`,
                headers: {
                    'Content-Type': config.headers['Content-Type'],
                    'Authorization': authValue,
                    'Origin': config.headers.Origin || (("TURBOPACK compile-time falsy", 0) ? "TURBOPACK unreachable" : 'SSR')
                },
                data: config.data ? JSON.stringify(config.data).substring(0, 200) : 'No data'
            });
            return config;
        }, (error)=>{
            console.error('❌ Request Interceptor Error:', error);
            return Promise.reject(this.handleError(error));
        });
        // Response interceptor
        this.axiosInstance.interceptors.response.use((response)=>{
            // PHASE 6A.10: Log successful responses
            console.log('✅ API Response Success:', {
                status: response.status,
                statusText: response.statusText,
                url: response.config.url,
                headers: {
                    'Access-Control-Allow-Origin': response.headers['access-control-allow-origin'],
                    'Access-Control-Allow-Credentials': response.headers['access-control-allow-credentials'],
                    'Content-Type': response.headers['content-type']
                },
                dataSize: JSON.stringify(response.data || {}).length
            });
            return response;
        }, async (error)=>{
            const originalRequest = error.config;
            console.log('🔍 [AUTH INTERCEPTOR] Response error received:', {
                status: error.response?.status,
                url: originalRequest?.url,
                method: originalRequest?.method,
                hasResponse: !!error.response,
                alreadyRetried: !!originalRequest._retry,
                errorMessage: error.message
            });
            // Check if this is a 401 error and we haven't already retried
            if (error.response?.status === 401 && !originalRequest._retry) {
                console.log('🔍 [AUTH INTERCEPTOR] 401 Unauthorized detected');
                // Skip refresh for auth endpoints (login, register, refresh)
                const isAuthEndpoint = originalRequest.url?.includes('/Auth/login') || originalRequest.url?.includes('/Auth/register') || originalRequest.url?.includes('/Auth/refresh');
                console.log('🔍 [AUTH INTERCEPTOR] Is auth endpoint?', isAuthEndpoint);
                if (!isAuthEndpoint) {
                    console.log('🔓 [AUTH INTERCEPTOR] Attempting token refresh...');
                    // Mark that we've tried to refresh for this request
                    originalRequest._retry = true;
                    try {
                        // Attempt to refresh the token
                        console.log('🔍 [AUTH INTERCEPTOR] Calling tokenRefreshService.refreshAccessToken()');
                        const newToken = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$services$2f$tokenRefreshService$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["tokenRefreshService"].refreshAccessToken();
                        console.log('🔍 [AUTH INTERCEPTOR] Token refresh result:', newToken ? 'SUCCESS' : 'FAILED (null)');
                        if (newToken) {
                            // Update the Authorization header with the new token
                            originalRequest.headers['Authorization'] = `Bearer ${newToken}`;
                            console.log('🔄 [AUTH INTERCEPTOR] Retrying request with new token...');
                            // Retry the original request
                            return this.axiosInstance(originalRequest);
                        } else {
                            console.error('❌ [AUTH INTERCEPTOR] Token refresh returned null - triggering logout');
                            if (this.onUnauthorized) {
                                console.log('🔍 [AUTH INTERCEPTOR] Calling onUnauthorized callback');
                                this.onUnauthorized();
                            }
                            return Promise.reject(new Error('Token refresh returned null'));
                        }
                    } catch (refreshError) {
                        console.error('❌ [AUTH INTERCEPTOR] Token refresh threw error:', refreshError);
                        console.error('❌ [AUTH INTERCEPTOR] Clearing auth and redirecting to login');
                        // Token refresh failed - trigger logout callback to clear auth state
                        if (this.onUnauthorized) {
                            console.log('🔍 [AUTH INTERCEPTOR] Calling onUnauthorized callback after refresh error');
                            this.onUnauthorized();
                        }
                        return Promise.reject(refreshError);
                    }
                } else {
                    console.log('🔍 [AUTH INTERCEPTOR] Skipping token refresh for auth endpoint');
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
                    headers: error.config?.headers
                } : 'No request object',
                response: error.response ? {
                    status: error.response.status,
                    statusText: error.response.statusText,
                    headers: error.response.headers,
                    data: error.response.data
                } : 'No response object',
                isAxiosError: __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$axios$2f$lib$2f$axios$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"].isAxiosError(error)
            });
            return Promise.reject(this.handleError(error));
        });
    }
    /**
   * Handle errors and convert to custom error types
   */ handleError(error) {
        if (__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$axios$2f$lib$2f$axios$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"].isAxiosError(error)) {
            const axiosError = error;
            // Network error (no response)
            if (!axiosError.response) {
                return new __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["NetworkError"](axiosError.message || 'Network error occurred');
            }
            const { status, data } = axiosError.response;
            // Extract error message
            const message = data?.message || data?.error || axiosError.message || 'An error occurred';
            // Handle specific status codes
            switch(status){
                case 400:
                    return new __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["ValidationError"](message, data?.errors || data?.validationErrors, axiosError.response);
                case 401:
                    // Token expired or invalid
                    // Note: onUnauthorized callback is already triggered in the response interceptor
                    // after token refresh fails, so we don't call it again here to avoid double redirects
                    return new __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["UnauthorizedError"](message);
                case 403:
                    return new __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["ForbiddenError"](message);
                case 404:
                    return new __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["NotFoundError"](message);
                case 500:
                case 502:
                case 503:
                case 504:
                    return new __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["ServerError"](message, status);
                default:
                    return new __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["ApiError"](message, status, undefined, axiosError.response);
            }
        }
        // Unknown error
        if (error instanceof Error) {
            return new __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["ApiError"](error.message);
        }
        return new __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["ApiError"]('An unknown error occurred');
    }
    /**
   * Set callback for handling 401 Unauthorized errors
   */ setUnauthorizedCallback(callback) {
        this.onUnauthorized = callback;
    }
    /**
   * Set authentication token
   */ setAuthToken(token) {
        this.authToken = token;
    }
    /**
   * Clear authentication token
   */ clearAuthToken() {
        this.authToken = null;
    }
    /**
   * GET request
   */ async get(url, config) {
        const response = await this.axiosInstance.get(url, config);
        return response.data;
    }
    /**
   * POST request
   */ async post(url, data, config) {
        const response = await this.axiosInstance.post(url, data, config);
        return response.data;
    }
    /**
   * PUT request
   */ async put(url, data, config) {
        const response = await this.axiosInstance.put(url, data, config);
        return response.data;
    }
    /**
   * PATCH request
   */ async patch(url, data, config) {
        const response = await this.axiosInstance.patch(url, data, config);
        return response.data;
    }
    /**
   * DELETE request
   */ async delete(url, config) {
        const response = await this.axiosInstance.delete(url, config);
        return response.data;
    }
    /**
   * POST request with multipart/form-data (for file uploads)
   * Note: Deletes Content-Type to let browser set multipart/form-data with boundary
   */ async postMultipart(url, formData, config) {
        // Delete Content-Type header to let browser set multipart/form-data with boundary
        const response = await this.axiosInstance.post(url, formData, {
            ...config,
            headers: {
                ...config?.headers,
                'Content-Type': undefined
            }
        });
        return response.data;
    }
}
const apiClient = ApiClient.getInstance();
}),
"[project]/src/infrastructure/utils/jwtDecoder.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

/**
 * JWT Decoder Utility
 * Decodes JWT tokens to extract expiration time and other claims
 *
 * Note: This is NOT for token validation (that's done on the server)
 * This is only for reading token expiration to enable proactive refresh
 */ __turbopack_context__.s([
    "decodeJwt",
    ()=>decodeJwt,
    "getRefreshTime",
    ()=>getRefreshTime,
    "getTimeUntilExpiration",
    ()=>getTimeUntilExpiration,
    "getTokenExpiration",
    ()=>getTokenExpiration,
    "isTokenExpired",
    ()=>isTokenExpired
]);
function decodeJwt(token) {
    if (!token) {
        return null;
    }
    try {
        // JWT format: header.payload.signature
        const parts = token.split('.');
        if (parts.length !== 3) {
            console.error('Invalid JWT format - expected 3 parts');
            return null;
        }
        // Decode the payload (second part)
        const payload = parts[1];
        // Base64URL decode
        const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map((c)=>'%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)).join(''));
        return JSON.parse(jsonPayload);
    } catch (error) {
        console.error('Failed to decode JWT:', error);
        return null;
    }
}
function getTokenExpiration(token) {
    const decoded = decodeJwt(token);
    if (!decoded || !decoded.exp) {
        return null;
    }
    // Convert from seconds to milliseconds
    return decoded.exp * 1000;
}
function isTokenExpired(token) {
    const expiration = getTokenExpiration(token);
    if (!expiration) {
        return true; // Treat invalid tokens as expired
    }
    return Date.now() >= expiration;
}
function getTimeUntilExpiration(token) {
    const expiration = getTokenExpiration(token);
    if (!expiration) {
        return null;
    }
    const timeRemaining = expiration - Date.now();
    return Math.max(0, timeRemaining);
}
function getRefreshTime(token) {
    const expiration = getTokenExpiration(token);
    if (!expiration) {
        return null;
    }
    // Refresh 5 minutes (300,000 ms) before expiration
    const refreshBuffer = 5 * 60 * 1000;
    return expiration - refreshBuffer;
}
}),
"[project]/src/presentation/hooks/useTokenRefresh.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

/**
 * useTokenRefresh Hook
 * Proactively refreshes JWT token before it expires
 *
 * Usage: Call this hook in your root layout or app component
 * Example: useTokenRefresh();
 *
 * Features:
 * - Automatically refreshes token 5 minutes before expiration
 * - Clears timer on logout
 * - Handles edge cases (invalid tokens, no token, etc.)
 */ __turbopack_context__.s([
    "useTokenRefresh",
    ()=>useTokenRefresh
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/store/useAuthStore.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$utils$2f$jwtDecoder$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/utils/jwtDecoder.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$services$2f$tokenRefreshService$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/services/tokenRefreshService.ts [app-ssr] (ecmascript)");
;
;
;
;
function useTokenRefresh() {
    const { accessToken, isAuthenticated } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useAuthStore"])();
    const timerRef = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useRef"])(null);
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useEffect"])(()=>{
        // Clear any existing timer
        if (timerRef.current) {
            clearTimeout(timerRef.current);
            timerRef.current = null;
        }
        // Only set up refresh if user is authenticated and has a token
        if (!isAuthenticated || !accessToken) {
            console.log('⏸️ Token refresh timer: Not authenticated or no token');
            return;
        }
        const refreshTime = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$utils$2f$jwtDecoder$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["getRefreshTime"])(accessToken);
        if (!refreshTime) {
            console.warn('⚠️ Unable to determine token refresh time - token may be invalid');
            return;
        }
        const now = Date.now();
        const timeUntilRefresh = refreshTime - now;
        // If token should already be refreshed, refresh immediately
        if (timeUntilRefresh <= 0) {
            console.log('⚡ Token refresh overdue - refreshing immediately');
            __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$services$2f$tokenRefreshService$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["tokenRefreshService"].refreshAccessToken().catch((error)=>{
                console.error('❌ Immediate token refresh failed:', error);
            });
            return;
        }
        console.log('⏰ Token refresh scheduled in:', {
            minutes: Math.round(timeUntilRefresh / 60000),
            seconds: Math.round(timeUntilRefresh / 1000)
        });
        // Set timer to refresh token
        timerRef.current = setTimeout(()=>{
            console.log('🔄 Proactive token refresh triggered');
            __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$services$2f$tokenRefreshService$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["tokenRefreshService"].refreshAccessToken().catch((error)=>{
                console.error('❌ Proactive token refresh failed:', error);
            });
        }, timeUntilRefresh);
        // Cleanup function
        return ()=>{
            if (timerRef.current) {
                console.log('🧹 Clearing token refresh timer');
                clearTimeout(timerRef.current);
                timerRef.current = null;
            }
        };
    }, [
        accessToken,
        isAuthenticated
    ]);
    // Also check token expiration on mount and every minute
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useEffect"])(()=>{
        if (!isAuthenticated || !accessToken) {
            return;
        }
        const checkInterval = setInterval(()=>{
            const timeRemaining = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$utils$2f$jwtDecoder$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["getTimeUntilExpiration"])(accessToken);
            if (timeRemaining === null) {
                console.warn('⚠️ Invalid token detected in periodic check');
                return;
            }
            // If less than 5 minutes remaining, refresh now
            if (timeRemaining < 5 * 60 * 1000 && timeRemaining > 0) {
                console.log('⚡ Less than 5 minutes remaining - refreshing token');
                __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$services$2f$tokenRefreshService$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["tokenRefreshService"].refreshAccessToken().catch((error)=>{
                    console.error('❌ Periodic token refresh failed:', error);
                });
            }
        }, 60 * 1000); // Check every minute
        return ()=>clearInterval(checkInterval);
    }, [
        accessToken,
        isAuthenticated
    ]);
}
}),
"[project]/src/presentation/providers/AuthProvider.tsx [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "AuthProvider",
    ()=>AuthProvider
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react-jsx-dev-runtime.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/navigation.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/client/api-client.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/store/useAuthStore.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useTokenRefresh$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/hooks/useTokenRefresh.ts [app-ssr] (ecmascript)");
'use client';
;
;
;
;
;
;
function AuthProvider({ children }) {
    const router = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useRouter"])();
    const clearAuth = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useAuthStore"])((state)=>state.clearAuth);
    // Phase AUTH-IMPROVEMENT: Proactive token refresh
    // Automatically refreshes token 5 minutes before expiration
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useTokenRefresh$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useTokenRefresh"])();
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useEffect"])(()=>{
        let isHandling401 = false;
        // Set up 401 error handler
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].setUnauthorizedCallback(()=>{
            console.log('🔍 [AUTH PROVIDER] onUnauthorized callback triggered');
            console.log('🔍 [AUTH PROVIDER] isHandling401:', isHandling401);
            // Prevent multiple simultaneous logout/redirect calls
            if (isHandling401) {
                console.log('🔍 [AUTH PROVIDER] Already handling 401, skipping');
                return;
            }
            isHandling401 = true;
            console.log('🔍 [AUTH PROVIDER] Calling clearAuth()');
            // Clear authentication state
            clearAuth();
            console.log('🔍 [AUTH PROVIDER] Redirecting to /login');
            // Redirect to login page
            router.push('/login');
            // Reset flag after redirect
            setTimeout(()=>{
                isHandling401 = false;
                console.log('🔍 [AUTH PROVIDER] Reset isHandling401 flag');
            }, 1000);
        });
    }, [
        router,
        clearAuth
    ]);
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Fragment"], {
        children: children
    }, void 0, false);
}
}),
"[project]/src/app/providers.tsx [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "Providers",
    ()=>Providers
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react-jsx-dev-runtime.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/QueryClientProvider.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$lib$2f$react$2d$query$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/lib/react-query.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$providers$2f$AuthProvider$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/providers/AuthProvider.tsx [app-ssr] (ecmascript)");
'use client';
;
;
;
;
;
function Providers({ children }) {
    // ✅ CORRECT: Use useState with initialization function
    // This creates the QueryClient ONCE on client mount
    // and ensures it survives React 19's automatic batching during hydration
    const [queryClient] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])(()=>(0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$lib$2f$react$2d$query$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["makeQueryClient"])());
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["QueryClientProvider"], {
        client: queryClient,
        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$providers$2f$AuthProvider$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["AuthProvider"], {
            children: children
        }, void 0, false, {
            fileName: "[project]/src/app/providers.tsx",
            lineNumber: 32,
            columnNumber: 7
        }, this)
    }, void 0, false, {
        fileName: "[project]/src/app/providers.tsx",
        lineNumber: 31,
        columnNumber: 5
    }, this);
}
}),
];

//# sourceMappingURL=%5Broot-of-the-server%5D__a0f3ab4d._.js.map