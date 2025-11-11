(globalThis.TURBOPACK || (globalThis.TURBOPACK = [])).push([typeof document === "object" ? document.currentScript : undefined,
"[project]/src/infrastructure/storage/localStorage.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
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
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/infrastructure/api/client/api-errors.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
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
    constructor(message, statusCode, validationErrors){
        super(message);
        this.name = 'ApiError';
        this.statusCode = statusCode;
        this.validationErrors = validationErrors;
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
    constructor(message = 'Validation failed', validationErrors){
        super(message, 400, validationErrors);
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
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/infrastructure/api/client/api-client.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "ApiClient",
    ()=>ApiClient,
    "apiClient",
    ()=>apiClient
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$build$2f$polyfills$2f$process$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = /*#__PURE__*/ __turbopack_context__.i("[project]/node_modules/next/dist/build/polyfills/process.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$axios$2f$lib$2f$axios$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/axios/lib/axios.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/client/api-errors.ts [app-client] (ecmascript)");
;
;
class ApiClient {
    static instance;
    axiosInstance;
    authToken = null;
    constructor(config){
        const baseURL = config?.baseURL || ("TURBOPACK compile-time value", "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api") || 'http://localhost:5000/api';
        this.axiosInstance = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$axios$2f$lib$2f$axios$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"].create({
            baseURL,
            timeout: config?.timeout || 30000,
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
            return config;
        }, (error)=>Promise.reject(this.handleError(error)));
        // Response interceptor
        this.axiosInstance.interceptors.response.use((response)=>response, (error)=>Promise.reject(this.handleError(error)));
    }
    /**
   * Handle errors and convert to custom error types
   */ handleError(error) {
        if (__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$axios$2f$lib$2f$axios$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"].isAxiosError(error)) {
            const axiosError = error;
            // Network error (no response)
            if (!axiosError.response) {
                return new __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["NetworkError"](axiosError.message || 'Network error occurred');
            }
            const { status, data } = axiosError.response;
            // Extract error message
            const message = data?.message || data?.error || axiosError.message || 'An error occurred';
            // Handle specific status codes
            switch(status){
                case 400:
                    return new __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["ValidationError"](message, data?.errors || data?.validationErrors);
                case 401:
                    return new __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["UnauthorizedError"](message);
                case 403:
                    return new __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["ForbiddenError"](message);
                case 404:
                    return new __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["NotFoundError"](message);
                case 500:
                case 502:
                case 503:
                case 504:
                    return new __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["ServerError"](message, status);
                default:
                    return new __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["ApiError"](message, status);
            }
        }
        // Unknown error
        if (error instanceof Error) {
            return new __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["ApiError"](error.message);
        }
        return new __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["ApiError"]('An unknown error occurred');
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
   */ async postMultipart(url, formData, config) {
        const response = await this.axiosInstance.post(url, formData, {
            ...config,
            headers: {
                'Content-Type': 'multipart/form-data',
                ...config?.headers
            }
        });
        return response.data;
    }
}
const apiClient = ApiClient.getInstance();
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/store/useAuthStore.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "useAuthStore",
    ()=>useAuthStore
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zustand$2f$esm$2f$react$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/zustand/esm/react.mjs [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zustand$2f$esm$2f$middleware$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/zustand/esm/middleware.mjs [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$storage$2f$localStorage$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/storage/localStorage.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/client/api-client.ts [app-client] (ecmascript)");
;
;
;
;
const useAuthStore = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zustand$2f$esm$2f$react$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__["create"])()((0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zustand$2f$esm$2f$middleware$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__["devtools"])((0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zustand$2f$esm$2f$middleware$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__["persist"])((set, get)=>({
        // Initial state
        user: null,
        accessToken: null,
        refreshToken: null,
        isAuthenticated: false,
        isLoading: false,
        // Set authentication (after login/register)
        setAuth: (user, tokens)=>{
            // Store tokens in localStorage
            __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$storage$2f$localStorage$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["LocalStorageService"].setAccessToken(tokens.accessToken);
            __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$storage$2f$localStorage$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["LocalStorageService"].setRefreshToken(tokens.refreshToken);
            __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$storage$2f$localStorage$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["LocalStorageService"].setUser(user);
            // Set auth token in API client
            __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].setAuthToken(tokens.accessToken);
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
            // Clear localStorage
            __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$storage$2f$localStorage$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["LocalStorageService"].clearAuth();
            // Clear auth token from API client
            __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].clearAuthToken();
            set({
                user: null,
                accessToken: null,
                refreshToken: null,
                isAuthenticated: false,
                isLoading: false
            });
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
            __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$storage$2f$localStorage$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["LocalStorageService"].setUser(updatedUser);
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
                __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].setAuthToken(state.accessToken);
            }
        }
}), {
    name: 'AuthStore'
}));
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/auth/ProtectedRoute.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "ProtectedRoute",
    ()=>ProtectedRoute
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/navigation.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/store/useAuthStore.ts [app-client] (ecmascript)");
;
var _s = __turbopack_context__.k.signature();
'use client';
;
;
;
function ProtectedRoute({ children }) {
    _s();
    const router = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRouter"])();
    const { isAuthenticated, isLoading } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useAuthStore"])();
    const [isHydrated, setIsHydrated] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(false);
    // Wait for Zustand store to hydrate from localStorage
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useEffect"])({
        "ProtectedRoute.useEffect": ()=>{
            setIsHydrated(true);
        }
    }["ProtectedRoute.useEffect"], []);
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useEffect"])({
        "ProtectedRoute.useEffect": ()=>{
            // Only redirect after hydration is complete
            if (isHydrated && !isLoading && !isAuthenticated) {
                router.push('/login');
            }
        }
    }["ProtectedRoute.useEffect"], [
        isAuthenticated,
        isLoading,
        isHydrated,
        router
    ]);
    // Show loading state while hydrating or checking authentication
    if (!isHydrated || isLoading) {
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "min-h-screen flex items-center justify-center",
            style: {
                background: '#f7fafc'
            },
            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "text-center",
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "animate-spin rounded-full h-12 w-12 border-b-2 mx-auto mb-4",
                        style: {
                            borderColor: '#FF7900'
                        }
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/auth/ProtectedRoute.tsx",
                        lineNumber: 39,
                        columnNumber: 11
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                        style: {
                            color: '#8B1538',
                            fontSize: '0.9rem'
                        },
                        children: "Loading..."
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/auth/ProtectedRoute.tsx",
                        lineNumber: 43,
                        columnNumber: 11
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/auth/ProtectedRoute.tsx",
                lineNumber: 38,
                columnNumber: 9
            }, this)
        }, void 0, false, {
            fileName: "[project]/src/presentation/components/auth/ProtectedRoute.tsx",
            lineNumber: 37,
            columnNumber: 7
        }, this);
    }
    // Don't render children if not authenticated
    if (!isAuthenticated) {
        return null;
    }
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Fragment"], {
        children: children
    }, void 0, false);
}
_s(ProtectedRoute, "8hcz7+GcvDiSkBI6Abi6zFiGhQE=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRouter"],
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useAuthStore"]
    ];
});
_c = ProtectedRoute;
var _c;
__turbopack_context__.k.register(_c, "ProtectedRoute");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/lib/utils.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "cn",
    ()=>cn
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$clsx$2f$dist$2f$clsx$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/clsx/dist/clsx.mjs [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$tailwind$2d$merge$2f$dist$2f$bundle$2d$mjs$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/tailwind-merge/dist/bundle-mjs.mjs [app-client] (ecmascript)");
;
;
function cn(...inputs) {
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$tailwind$2d$merge$2f$dist$2f$bundle$2d$mjs$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__["twMerge"])((0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$clsx$2f$dist$2f$clsx$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__["clsx"])(inputs));
}
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/ui/Button.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "Button",
    ()=>Button,
    "buttonVariants",
    ()=>buttonVariants
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$class$2d$variance$2d$authority$2f$dist$2f$index$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/class-variance-authority/dist/index.mjs [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-client] (ecmascript)");
;
;
;
;
const buttonVariants = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$class$2d$variance$2d$authority$2f$dist$2f$index$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cva"])('inline-flex items-center justify-center whitespace-nowrap rounded-md text-sm font-medium ring-offset-background transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50', {
    variants: {
        variant: {
            default: 'bg-primary text-primary-foreground hover:bg-primary/90',
            destructive: 'bg-destructive text-destructive-foreground hover:bg-destructive/90',
            outline: 'border border-primary bg-background hover:bg-accent hover:text-accent-foreground',
            secondary: 'bg-secondary text-secondary-foreground hover:bg-secondary/80',
            ghost: 'hover:bg-accent hover:text-accent-foreground',
            link: 'text-primary underline-offset-4 hover:underline'
        },
        size: {
            default: 'h-10 px-4 py-2',
            sm: 'h-9 rounded-md px-3',
            lg: 'h-11 rounded-md px-8',
            icon: 'h-10 w-10'
        }
    },
    defaultVariants: {
        variant: 'default',
        size: 'default'
    }
});
/**
 * Button Component
 * Reusable button component with multiple variants and sizes
 * Follows UI/UX best practices with loading states and accessibility
 */ const Button = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["forwardRef"](_c = ({ className, variant, size, loading, disabled, children, ...props }, ref)=>{
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])(buttonVariants({
            variant,
            size,
            className
        })),
        ref: ref,
        disabled: disabled || loading,
        "aria-disabled": disabled || loading,
        ...props,
        children: loading ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Fragment"], {
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("svg", {
                    className: "mr-2 h-4 w-4 animate-spin",
                    xmlns: "http://www.w3.org/2000/svg",
                    fill: "none",
                    viewBox: "0 0 24 24",
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("circle", {
                            className: "opacity-25",
                            cx: "12",
                            cy: "12",
                            r: "10",
                            stroke: "currentColor",
                            strokeWidth: "4"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/ui/Button.tsx",
                            lineNumber: 60,
                            columnNumber: 15
                        }, ("TURBOPACK compile-time value", void 0)),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("path", {
                            className: "opacity-75",
                            fill: "currentColor",
                            d: "M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/ui/Button.tsx",
                            lineNumber: 68,
                            columnNumber: 15
                        }, ("TURBOPACK compile-time value", void 0))
                    ]
                }, void 0, true, {
                    fileName: "[project]/src/presentation/components/ui/Button.tsx",
                    lineNumber: 54,
                    columnNumber: 13
                }, ("TURBOPACK compile-time value", void 0)),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                    children: "Loading..."
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/ui/Button.tsx",
                    lineNumber: 74,
                    columnNumber: 13
                }, ("TURBOPACK compile-time value", void 0))
            ]
        }, void 0, true) : children
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Button.tsx",
        lineNumber: 45,
        columnNumber: 7
    }, ("TURBOPACK compile-time value", void 0));
});
_c1 = Button;
Button.displayName = 'Button';
;
var _c, _c1;
__turbopack_context__.k.register(_c, "Button$React.forwardRef");
__turbopack_context__.k.register(_c1, "Button");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/atoms/Logo.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "Logo",
    ()=>Logo
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$image$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/image.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-client] (ecmascript)");
;
;
;
const sizeClasses = {
    sm: 'h-10 w-10',
    md: 'h-16 w-16',
    lg: 'h-20 w-20',
    xl: 'h-24 w-24'
};
const textSizeClasses = {
    sm: 'text-lg',
    md: 'text-xl',
    lg: 'text-2xl',
    xl: 'text-3xl'
};
const imageSizes = {
    sm: 40,
    md: 64,
    lg: 80,
    xl: 96
};
function Logo({ size = 'md', showText = false, className }) {
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('flex items-center gap-3', className),
        suppressHydrationWarning: true,
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])(sizeClasses[size], 'relative flex-shrink-0'),
                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$image$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
                    src: "/images/lankaconnect-logo.png",
                    alt: "LankaConnect",
                    width: imageSizes[size],
                    height: imageSizes[size],
                    className: "object-contain w-full h-full",
                    priority: true
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/atoms/Logo.tsx",
                    lineNumber: 39,
                    columnNumber: 9
                }, this)
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/atoms/Logo.tsx",
                lineNumber: 38,
                columnNumber: 7
            }, this),
            showText && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('font-bold text-maroon', textSizeClasses[size]),
                children: "LankaConnect"
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/atoms/Logo.tsx",
                lineNumber: 49,
                columnNumber: 9
            }, this)
        ]
    }, void 0, true, {
        fileName: "[project]/src/presentation/components/atoms/Logo.tsx",
        lineNumber: 37,
        columnNumber: 5
    }, this);
}
_c = Logo;
var _c;
__turbopack_context__.k.register(_c, "Logo");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/domain/constants/metroAreas.constants.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

/**
 * Metro Areas Constants - Phase 5B.3
 *
 * Generated from backend MetroAreaSeeder.cs with GUID-based IDs.
 * Includes all 50 US states with state-level areas plus 100+ major metro areas.
 *
 * Data Structure:
 * - 50 state-level entries (All [StateName]) - used for broad geographic coverage
 * - 100+ major US metro areas grouped by state
 * - Each metro uses backend GUID as ID for consistency with API
 */ __turbopack_context__.s([
    "ALL_METRO_AREAS",
    ()=>ALL_METRO_AREAS,
    "US_STATES",
    ()=>US_STATES,
    "getCityLevelAreas",
    ()=>getCityLevelAreas,
    "getMetroById",
    ()=>getMetroById,
    "getMetrosByState",
    ()=>getMetrosByState,
    "getMetrosGroupedByState",
    ()=>getMetrosGroupedByState,
    "getStateLevelAreas",
    ()=>getStateLevelAreas,
    "getStateName",
    ()=>getStateName,
    "isStateLevelArea",
    ()=>isStateLevelArea,
    "searchMetrosByName",
    ()=>searchMetrosByName
]);
const US_STATES = [
    {
        code: 'AL',
        name: 'Alabama'
    },
    {
        code: 'AK',
        name: 'Alaska'
    },
    {
        code: 'AZ',
        name: 'Arizona'
    },
    {
        code: 'AR',
        name: 'Arkansas'
    },
    {
        code: 'CA',
        name: 'California'
    },
    {
        code: 'CO',
        name: 'Colorado'
    },
    {
        code: 'CT',
        name: 'Connecticut'
    },
    {
        code: 'DE',
        name: 'Delaware'
    },
    {
        code: 'FL',
        name: 'Florida'
    },
    {
        code: 'GA',
        name: 'Georgia'
    },
    {
        code: 'HI',
        name: 'Hawaii'
    },
    {
        code: 'ID',
        name: 'Idaho'
    },
    {
        code: 'IL',
        name: 'Illinois'
    },
    {
        code: 'IN',
        name: 'Indiana'
    },
    {
        code: 'IA',
        name: 'Iowa'
    },
    {
        code: 'KS',
        name: 'Kansas'
    },
    {
        code: 'KY',
        name: 'Kentucky'
    },
    {
        code: 'LA',
        name: 'Louisiana'
    },
    {
        code: 'ME',
        name: 'Maine'
    },
    {
        code: 'MD',
        name: 'Maryland'
    },
    {
        code: 'MA',
        name: 'Massachusetts'
    },
    {
        code: 'MI',
        name: 'Michigan'
    },
    {
        code: 'MN',
        name: 'Minnesota'
    },
    {
        code: 'MS',
        name: 'Mississippi'
    },
    {
        code: 'MO',
        name: 'Missouri'
    },
    {
        code: 'MT',
        name: 'Montana'
    },
    {
        code: 'NE',
        name: 'Nebraska'
    },
    {
        code: 'NV',
        name: 'Nevada'
    },
    {
        code: 'NH',
        name: 'New Hampshire'
    },
    {
        code: 'NJ',
        name: 'New Jersey'
    },
    {
        code: 'NM',
        name: 'New Mexico'
    },
    {
        code: 'NY',
        name: 'New York'
    },
    {
        code: 'NC',
        name: 'North Carolina'
    },
    {
        code: 'ND',
        name: 'North Dakota'
    },
    {
        code: 'OH',
        name: 'Ohio'
    },
    {
        code: 'OK',
        name: 'Oklahoma'
    },
    {
        code: 'OR',
        name: 'Oregon'
    },
    {
        code: 'PA',
        name: 'Pennsylvania'
    },
    {
        code: 'RI',
        name: 'Rhode Island'
    },
    {
        code: 'SC',
        name: 'South Carolina'
    },
    {
        code: 'SD',
        name: 'South Dakota'
    },
    {
        code: 'TN',
        name: 'Tennessee'
    },
    {
        code: 'TX',
        name: 'Texas'
    },
    {
        code: 'UT',
        name: 'Utah'
    },
    {
        code: 'VT',
        name: 'Vermont'
    },
    {
        code: 'VA',
        name: 'Virginia'
    },
    {
        code: 'WA',
        name: 'Washington'
    },
    {
        code: 'WV',
        name: 'West Virginia'
    },
    {
        code: 'WI',
        name: 'Wisconsin'
    },
    {
        code: 'WY',
        name: 'Wyoming'
    }
];
const ALL_METRO_AREAS = [
    // =====================
    // ALABAMA
    // =====================
    {
        id: '01000000-0000-0000-0000-000000000001',
        name: 'All Alabama',
        state: 'AL',
        cities: [
            'Statewide'
        ],
        centerLat: 32.8067,
        centerLng: -86.7113,
        radiusMiles: 200
    },
    {
        id: '01111111-1111-1111-1111-111111111001',
        name: 'Birmingham',
        state: 'AL',
        cities: [
            'Birmingham',
            'Hoover',
            'Vestavia Hills',
            'Alabaster',
            'Bessemer'
        ],
        centerLat: 33.5186,
        centerLng: -86.8104,
        radiusMiles: 30
    },
    {
        id: '01111111-1111-1111-1111-111111111002',
        name: 'Montgomery',
        state: 'AL',
        cities: [
            'Montgomery',
            'Prattville',
            'Millbrook'
        ],
        centerLat: 32.3792,
        centerLng: -86.3077,
        radiusMiles: 25
    },
    {
        id: '01111111-1111-1111-1111-111111111003',
        name: 'Mobile',
        state: 'AL',
        cities: [
            'Mobile',
            'Prichard',
            'Saraland',
            'Chickasaw'
        ],
        centerLat: 30.6954,
        centerLng: -88.0399,
        radiusMiles: 25
    },
    // =====================
    // ALASKA
    // =====================
    {
        id: '02000000-0000-0000-0000-000000000001',
        name: 'All Alaska',
        state: 'AK',
        cities: [
            'Statewide'
        ],
        centerLat: 64.0685,
        centerLng: -152.2782,
        radiusMiles: 300
    },
    {
        id: '02111111-1111-1111-1111-111111111001',
        name: 'Anchorage',
        state: 'AK',
        cities: [
            'Anchorage',
            'Eagle River',
            'Girdwood'
        ],
        centerLat: 61.2181,
        centerLng: -149.9003,
        radiusMiles: 30
    },
    // =====================
    // ARIZONA
    // =====================
    {
        id: '04000000-0000-0000-0000-000000000001',
        name: 'All Arizona',
        state: 'AZ',
        cities: [
            'Statewide'
        ],
        centerLat: 33.7298,
        centerLng: -111.4312,
        radiusMiles: 200
    },
    {
        id: '04111111-1111-1111-1111-111111111001',
        name: 'Phoenix',
        state: 'AZ',
        cities: [
            'Phoenix',
            'Scottsdale',
            'Tempe',
            'Glendale',
            'Chandler',
            'Gilbert',
            'Peoria',
            'Surprise'
        ],
        centerLat: 33.4484,
        centerLng: -112.0742,
        radiusMiles: 35
    },
    {
        id: '04111111-1111-1111-1111-111111111002',
        name: 'Tucson',
        state: 'AZ',
        cities: [
            'Tucson',
            'Oro Valley',
            'Marana',
            'Sahuarita'
        ],
        centerLat: 32.2226,
        centerLng: -110.9747,
        radiusMiles: 30
    },
    {
        id: '04111111-1111-1111-1111-111111111003',
        name: 'Mesa',
        state: 'AZ',
        cities: [
            'Mesa',
            'Apache Junction',
            'Queen Creek'
        ],
        centerLat: 33.4152,
        centerLng: -111.8317,
        radiusMiles: 25
    },
    // =====================
    // ARKANSAS
    // =====================
    {
        id: '05000000-0000-0000-0000-000000000001',
        name: 'All Arkansas',
        state: 'AR',
        cities: [
            'Statewide'
        ],
        centerLat: 34.9697,
        centerLng: -92.3731,
        radiusMiles: 200
    },
    {
        id: '05111111-1111-1111-1111-111111111001',
        name: 'Little Rock',
        state: 'AR',
        cities: [
            'Little Rock',
            'North Little Rock',
            'Conway',
            'Benton'
        ],
        centerLat: 34.7465,
        centerLng: -92.2896,
        radiusMiles: 30
    },
    {
        id: '05111111-1111-1111-1111-111111111002',
        name: 'Fayetteville',
        state: 'AR',
        cities: [
            'Fayetteville',
            'Springdale',
            'Rogers',
            'Bentonville'
        ],
        centerLat: 36.0627,
        centerLng: -94.1734,
        radiusMiles: 25
    },
    // =====================
    // CALIFORNIA
    // =====================
    {
        id: '06000000-0000-0000-0000-000000000001',
        name: 'All California',
        state: 'CA',
        cities: [
            'Statewide'
        ],
        centerLat: 36.1162,
        centerLng: -119.6816,
        radiusMiles: 250
    },
    {
        id: '06111111-1111-1111-1111-111111111001',
        name: 'Los Angeles',
        state: 'CA',
        cities: [
            'Los Angeles',
            'Long Beach',
            'Anaheim',
            'Santa Ana',
            'Irvine',
            'Glendale',
            'Pasadena',
            'Torrance',
            'Burbank'
        ],
        centerLat: 34.0522,
        centerLng: -118.2437,
        radiusMiles: 40
    },
    {
        id: '06111111-1111-1111-1111-111111111002',
        name: 'San Francisco Bay Area',
        state: 'CA',
        cities: [
            'San Francisco',
            'Oakland',
            'San Jose',
            'Berkeley',
            'Fremont',
            'Hayward',
            'Sunnyvale',
            'Santa Clara'
        ],
        centerLat: 37.7749,
        centerLng: -122.4194,
        radiusMiles: 40
    },
    {
        id: '06111111-1111-1111-1111-111111111003',
        name: 'San Diego',
        state: 'CA',
        cities: [
            'San Diego',
            'Chula Vista',
            'Oceanside',
            'Carlsbad',
            'El Cajon',
            'Vista'
        ],
        centerLat: 32.7157,
        centerLng: -117.1611,
        radiusMiles: 35
    },
    {
        id: '06111111-1111-1111-1111-111111111004',
        name: 'Sacramento',
        state: 'CA',
        cities: [
            'Sacramento',
            'Elk Grove',
            'Roseville',
            'Folsom',
            'Davis'
        ],
        centerLat: 38.5816,
        centerLng: -121.4944,
        radiusMiles: 30
    },
    {
        id: '06111111-1111-1111-1111-111111111005',
        name: 'Fresno',
        state: 'CA',
        cities: [
            'Fresno',
            'Clovis',
            'Madera',
            'Sanger'
        ],
        centerLat: 36.7469,
        centerLng: -119.7726,
        radiusMiles: 25
    },
    {
        id: '06111111-1111-1111-1111-111111111006',
        name: 'Inland Empire',
        state: 'CA',
        cities: [
            'Riverside',
            'San Bernardino',
            'Ontario',
            'Rancho Cucamonga',
            'Corona',
            'Moreno Valley'
        ],
        centerLat: 33.9819,
        centerLng: -117.2466,
        radiusMiles: 35
    },
    // =====================
    // COLORADO
    // =====================
    {
        id: '08000000-0000-0000-0000-000000000001',
        name: 'All Colorado',
        state: 'CO',
        cities: [
            'Statewide'
        ],
        centerLat: 39.0598,
        centerLng: -105.3111,
        radiusMiles: 200
    },
    {
        id: '08111111-1111-1111-1111-111111111001',
        name: 'Denver',
        state: 'CO',
        cities: [
            'Denver',
            'Aurora',
            'Lakewood',
            'Thornton',
            'Arvada',
            'Westminster',
            'Centennial'
        ],
        centerLat: 39.7392,
        centerLng: -104.9903,
        radiusMiles: 35
    },
    {
        id: '08111111-1111-1111-1111-111111111002',
        name: 'Colorado Springs',
        state: 'CO',
        cities: [
            'Colorado Springs',
            'Fountain',
            'Monument'
        ],
        centerLat: 38.8339,
        centerLng: -104.8202,
        radiusMiles: 30
    },
    // =====================
    // CONNECTICUT
    // =====================
    {
        id: '09000000-0000-0000-0000-000000000001',
        name: 'All Connecticut',
        state: 'CT',
        cities: [
            'Statewide'
        ],
        centerLat: 41.5978,
        centerLng: -72.7554,
        radiusMiles: 150
    },
    {
        id: '09111111-1111-1111-1111-111111111001',
        name: 'Hartford',
        state: 'CT',
        cities: [
            'Hartford',
            'West Hartford',
            'New Britain',
            'Bristol'
        ],
        centerLat: 41.7658,
        centerLng: -72.6734,
        radiusMiles: 25
    },
    {
        id: '09111111-1111-1111-1111-111111111002',
        name: 'Bridgeport',
        state: 'CT',
        cities: [
            'Bridgeport',
            'Stamford',
            'Norwalk',
            'Danbury'
        ],
        centerLat: 41.1834,
        centerLng: -73.1959,
        radiusMiles: 25
    },
    // =====================
    // DELAWARE
    // =====================
    {
        id: '10000000-0000-0000-0000-000000000001',
        name: 'All Delaware',
        state: 'DE',
        cities: [
            'Statewide'
        ],
        centerLat: 39.3185,
        centerLng: -75.5244,
        radiusMiles: 120
    },
    {
        id: '10111111-1111-1111-1111-111111111001',
        name: 'Wilmington',
        state: 'DE',
        cities: [
            'Wilmington',
            'Newark',
            'Dover'
        ],
        centerLat: 39.7391,
        centerLng: -75.5244,
        radiusMiles: 25
    },
    // =====================
    // FLORIDA
    // =====================
    {
        id: '12000000-0000-0000-0000-000000000001',
        name: 'All Florida',
        state: 'FL',
        cities: [
            'Statewide'
        ],
        centerLat: 27.6648,
        centerLng: -81.5158,
        radiusMiles: 250
    },
    {
        id: '12111111-1111-1111-1111-111111111001',
        name: 'Miami',
        state: 'FL',
        cities: [
            'Miami',
            'Fort Lauderdale',
            'Hollywood',
            'Coral Gables',
            'Hialeah',
            'Pembroke Pines'
        ],
        centerLat: 25.7617,
        centerLng: -80.1918,
        radiusMiles: 35
    },
    {
        id: '12111111-1111-1111-1111-111111111002',
        name: 'Orlando',
        state: 'FL',
        cities: [
            'Orlando',
            'Kissimmee',
            'Winter Park',
            'Sanford',
            'Altamonte Springs'
        ],
        centerLat: 28.5421,
        centerLng: -81.3723,
        radiusMiles: 30
    },
    {
        id: '12111111-1111-1111-1111-111111111003',
        name: 'Tampa Bay',
        state: 'FL',
        cities: [
            'Tampa',
            'St. Petersburg',
            'Clearwater',
            'Brandon',
            'Largo'
        ],
        centerLat: 27.9506,
        centerLng: -82.4572,
        radiusMiles: 30
    },
    {
        id: '12111111-1111-1111-1111-111111111004',
        name: 'Jacksonville',
        state: 'FL',
        cities: [
            'Jacksonville',
            'Jacksonville Beach',
            'Orange Park'
        ],
        centerLat: 30.3322,
        centerLng: -81.6557,
        radiusMiles: 30
    },
    // =====================
    // GEORGIA
    // =====================
    {
        id: '13000000-0000-0000-0000-000000000001',
        name: 'All Georgia',
        state: 'GA',
        cities: [
            'Statewide'
        ],
        centerLat: 33.0406,
        centerLng: -83.6431,
        radiusMiles: 200
    },
    {
        id: '13111111-1111-1111-1111-111111111001',
        name: 'Atlanta',
        state: 'GA',
        cities: [
            'Atlanta',
            'Sandy Springs',
            'Roswell',
            'Johns Creek',
            'Marietta',
            'Alpharetta',
            'Smyrna'
        ],
        centerLat: 33.7490,
        centerLng: -84.3880,
        radiusMiles: 40
    },
    {
        id: '13111111-1111-1111-1111-111111111002',
        name: 'Savannah',
        state: 'GA',
        cities: [
            'Savannah',
            'Pooler',
            'Hinesville'
        ],
        centerLat: 32.0809,
        centerLng: -81.0912,
        radiusMiles: 25
    },
    // =====================
    // HAWAII
    // =====================
    {
        id: '15000000-0000-0000-0000-000000000001',
        name: 'All Hawaii',
        state: 'HI',
        cities: [
            'Statewide'
        ],
        centerLat: 21.0943,
        centerLng: -157.4981,
        radiusMiles: 200
    },
    {
        id: '15111111-1111-1111-1111-111111111001',
        name: 'Honolulu',
        state: 'HI',
        cities: [
            'Honolulu',
            'Pearl City',
            'Kailua',
            'Waipahu'
        ],
        centerLat: 21.3099,
        centerLng: -157.8581,
        radiusMiles: 30
    },
    // =====================
    // IDAHO
    // =====================
    {
        id: '16000000-0000-0000-0000-000000000001',
        name: 'All Idaho',
        state: 'ID',
        cities: [
            'Statewide'
        ],
        centerLat: 44.2405,
        centerLng: -114.4787,
        radiusMiles: 200
    },
    {
        id: '16111111-1111-1111-1111-111111111001',
        name: 'Boise',
        state: 'ID',
        cities: [
            'Boise',
            'Meridian',
            'Nampa',
            'Caldwell'
        ],
        centerLat: 43.6150,
        centerLng: -116.2023,
        radiusMiles: 30
    },
    // =====================
    // ILLINOIS
    // =====================
    {
        id: '17000000-0000-0000-0000-000000000001',
        name: 'All Illinois',
        state: 'IL',
        cities: [
            'Statewide'
        ],
        centerLat: 40.3495,
        centerLng: -88.9861,
        radiusMiles: 200
    },
    {
        id: '17111111-1111-1111-1111-111111111001',
        name: 'Chicago',
        state: 'IL',
        cities: [
            'Chicago',
            'Aurora',
            'Naperville',
            'Joliet',
            'Rockford',
            'Elgin',
            'Waukegan',
            'Cicero',
            'Schaumburg',
            'Evanston'
        ],
        centerLat: 41.8781,
        centerLng: -87.6298,
        radiusMiles: 45
    },
    // =====================
    // INDIANA
    // =====================
    {
        id: '18000000-0000-0000-0000-000000000001',
        name: 'All Indiana',
        state: 'IN',
        cities: [
            'Statewide'
        ],
        centerLat: 39.8494,
        centerLng: -86.2604,
        radiusMiles: 200
    },
    {
        id: '18111111-1111-1111-1111-111111111001',
        name: 'Indianapolis',
        state: 'IN',
        cities: [
            'Indianapolis',
            'Carmel',
            'Fishers',
            'Noblesville',
            'Greenwood',
            'Lawrence'
        ],
        centerLat: 39.7684,
        centerLng: -86.1581,
        radiusMiles: 35
    },
    // =====================
    // IOWA
    // =====================
    {
        id: '19000000-0000-0000-0000-000000000001',
        name: 'All Iowa',
        state: 'IA',
        cities: [
            'Statewide'
        ],
        centerLat: 42.0115,
        centerLng: -93.2105,
        radiusMiles: 200
    },
    {
        id: '19111111-1111-1111-1111-111111111001',
        name: 'Des Moines',
        state: 'IA',
        cities: [
            'Des Moines',
            'West Des Moines',
            'Ankeny',
            'Urbandale'
        ],
        centerLat: 41.5868,
        centerLng: -93.6250,
        radiusMiles: 30
    },
    // =====================
    // KANSAS
    // =====================
    {
        id: '20000000-0000-0000-0000-000000000001',
        name: 'All Kansas',
        state: 'KS',
        cities: [
            'Statewide'
        ],
        centerLat: 38.5266,
        centerLng: -96.7265,
        radiusMiles: 200
    },
    {
        id: '20111111-1111-1111-1111-111111111001',
        name: 'Kansas City',
        state: 'KS',
        cities: [
            'Kansas City',
            'Overland Park',
            'Olathe',
            'Lawrence',
            'Shawnee'
        ],
        centerLat: 39.0997,
        centerLng: -94.5786,
        radiusMiles: 35
    },
    // =====================
    // KENTUCKY
    // =====================
    {
        id: '21000000-0000-0000-0000-000000000001',
        name: 'All Kentucky',
        state: 'KY',
        cities: [
            'Statewide'
        ],
        centerLat: 37.6681,
        centerLng: -84.6701,
        radiusMiles: 200
    },
    {
        id: '21111111-1111-1111-1111-111111111001',
        name: 'Louisville',
        state: 'KY',
        cities: [
            'Louisville',
            'Jeffersontown',
            'Shively',
            'St. Matthews'
        ],
        centerLat: 38.2527,
        centerLng: -85.7585,
        radiusMiles: 30
    },
    // =====================
    // LOUISIANA
    // =====================
    {
        id: '22000000-0000-0000-0000-000000000001',
        name: 'All Louisiana',
        state: 'LA',
        cities: [
            'Statewide'
        ],
        centerLat: 31.1695,
        centerLng: -91.8749,
        radiusMiles: 200
    },
    {
        id: '22111111-1111-1111-1111-111111111001',
        name: 'New Orleans',
        state: 'LA',
        cities: [
            'New Orleans',
            'Metairie',
            'Kenner',
            'Baton Rouge'
        ],
        centerLat: 29.9511,
        centerLng: -90.2623,
        radiusMiles: 30
    },
    // =====================
    // MAINE
    // =====================
    {
        id: '23000000-0000-0000-0000-000000000001',
        name: 'All Maine',
        state: 'ME',
        cities: [
            'Statewide'
        ],
        centerLat: 44.6939,
        centerLng: -69.3819,
        radiusMiles: 180
    },
    {
        id: '23111111-1111-1111-1111-111111111001',
        name: 'Portland',
        state: 'ME',
        cities: [
            'Portland',
            'South Portland',
            'Westbrook',
            'Biddeford'
        ],
        centerLat: 43.6591,
        centerLng: -70.2568,
        radiusMiles: 25
    },
    // =====================
    // MARYLAND
    // =====================
    {
        id: '24000000-0000-0000-0000-000000000001',
        name: 'All Maryland',
        state: 'MD',
        cities: [
            'Statewide'
        ],
        centerLat: 39.0639,
        centerLng: -76.8021,
        radiusMiles: 180
    },
    {
        id: '24111111-1111-1111-1111-111111111001',
        name: 'Baltimore',
        state: 'MD',
        cities: [
            'Baltimore',
            'Columbia',
            'Germantown',
            'Silver Spring',
            'Rockville'
        ],
        centerLat: 39.2904,
        centerLng: -76.6122,
        radiusMiles: 30
    },
    // =====================
    // MASSACHUSETTS
    // =====================
    {
        id: '25000000-0000-0000-0000-000000000001',
        name: 'All Massachusetts',
        state: 'MA',
        cities: [
            'Statewide'
        ],
        centerLat: 42.2352,
        centerLng: -71.0275,
        radiusMiles: 150
    },
    {
        id: '25111111-1111-1111-1111-111111111001',
        name: 'Boston',
        state: 'MA',
        cities: [
            'Boston',
            'Cambridge',
            'Quincy',
            'Lynn',
            'Newton',
            'Somerville',
            'Waltham',
            'Brookline'
        ],
        centerLat: 42.3601,
        centerLng: -71.0589,
        radiusMiles: 35
    },
    // =====================
    // MICHIGAN
    // =====================
    {
        id: '26000000-0000-0000-0000-000000000001',
        name: 'All Michigan',
        state: 'MI',
        cities: [
            'Statewide'
        ],
        centerLat: 43.3266,
        centerLng: -84.5361,
        radiusMiles: 200
    },
    {
        id: '26111111-1111-1111-1111-111111111001',
        name: 'Detroit',
        state: 'MI',
        cities: [
            'Detroit',
            'Warren',
            'Sterling Heights',
            'Ann Arbor',
            'Livonia',
            'Dearborn',
            'Westland'
        ],
        centerLat: 42.3314,
        centerLng: -83.0458,
        radiusMiles: 40
    },
    // =====================
    // MINNESOTA
    // =====================
    {
        id: '27000000-0000-0000-0000-000000000001',
        name: 'All Minnesota',
        state: 'MN',
        cities: [
            'Statewide'
        ],
        centerLat: 45.6945,
        centerLng: -93.9196,
        radiusMiles: 200
    },
    {
        id: '27111111-1111-1111-1111-111111111001',
        name: 'Minneapolis-St. Paul',
        state: 'MN',
        cities: [
            'Minneapolis',
            'St. Paul',
            'Rochester',
            'Bloomington',
            'Brooklyn Park',
            'Plymouth'
        ],
        centerLat: 44.9537,
        centerLng: -93.0900,
        radiusMiles: 35
    },
    // =====================
    // MISSISSIPPI
    // =====================
    {
        id: '28000000-0000-0000-0000-000000000001',
        name: 'All Mississippi',
        state: 'MS',
        cities: [
            'Statewide'
        ],
        centerLat: 32.7416,
        centerLng: -89.6787,
        radiusMiles: 200
    },
    {
        id: '28111111-1111-1111-1111-111111111001',
        name: 'Jackson',
        state: 'MS',
        cities: [
            'Jackson',
            'Gulfport',
            'Biloxi',
            'Hattiesburg'
        ],
        centerLat: 32.2988,
        centerLng: -90.1848,
        radiusMiles: 25
    },
    // =====================
    // MISSOURI
    // =====================
    {
        id: '29000000-0000-0000-0000-000000000001',
        name: 'All Missouri',
        state: 'MO',
        cities: [
            'Statewide'
        ],
        centerLat: 38.4561,
        centerLng: -92.2884,
        radiusMiles: 200
    },
    {
        id: '29111111-1111-1111-1111-111111111001',
        name: 'St. Louis',
        state: 'MO',
        cities: [
            'St. Louis',
            'St. Charles',
            "O'Fallon",
            'St. Peters',
            'Florissant'
        ],
        centerLat: 38.6270,
        centerLng: -90.1994,
        radiusMiles: 35
    },
    {
        id: '29111111-1111-1111-1111-111111111002',
        name: 'Kansas City',
        state: 'MO',
        cities: [
            'Kansas City',
            'Independence',
            'Lee\'s Summit',
            'Blue Springs'
        ],
        centerLat: 39.0997,
        centerLng: -94.5786,
        radiusMiles: 35
    },
    // =====================
    // MONTANA
    // =====================
    {
        id: '30000000-0000-0000-0000-000000000001',
        name: 'All Montana',
        state: 'MT',
        cities: [
            'Statewide'
        ],
        centerLat: 46.9219,
        centerLng: -109.6333,
        radiusMiles: 250
    },
    {
        id: '30111111-1111-1111-1111-111111111001',
        name: 'Billings',
        state: 'MT',
        cities: [
            'Billings',
            'Missoula',
            'Great Falls',
            'Bozeman'
        ],
        centerLat: 45.7833,
        centerLng: -103.8014,
        radiusMiles: 25
    },
    // =====================
    // NEBRASKA
    // =====================
    {
        id: '31000000-0000-0000-0000-000000000001',
        name: 'All Nebraska',
        state: 'NE',
        cities: [
            'Statewide'
        ],
        centerLat: 41.4925,
        centerLng: -99.9018,
        radiusMiles: 200
    },
    {
        id: '31111111-1111-1111-1111-111111111001',
        name: 'Omaha',
        state: 'NE',
        cities: [
            'Omaha',
            'Lincoln',
            'Bellevue',
            'Grand Island'
        ],
        centerLat: 41.2565,
        centerLng: -95.9345,
        radiusMiles: 30
    },
    // =====================
    // NEVADA
    // =====================
    {
        id: '32000000-0000-0000-0000-000000000001',
        name: 'All Nevada',
        state: 'NV',
        cities: [
            'Statewide'
        ],
        centerLat: 38.8026,
        centerLng: -117.0554,
        radiusMiles: 200
    },
    {
        id: '32111111-1111-1111-1111-111111111001',
        name: 'Las Vegas',
        state: 'NV',
        cities: [
            'Las Vegas',
            'Henderson',
            'North Las Vegas',
            'Paradise'
        ],
        centerLat: 36.1699,
        centerLng: -115.1398,
        radiusMiles: 30
    },
    {
        id: '32111111-1111-1111-1111-111111111002',
        name: 'Reno',
        state: 'NV',
        cities: [
            'Reno',
            'Sparks',
            'Carson City'
        ],
        centerLat: 39.5296,
        centerLng: -119.8138,
        radiusMiles: 25
    },
    // =====================
    // NEW HAMPSHIRE
    // =====================
    {
        id: '33000000-0000-0000-0000-000000000001',
        name: 'All New Hampshire',
        state: 'NH',
        cities: [
            'Statewide'
        ],
        centerLat: 43.4525,
        centerLng: -71.3102,
        radiusMiles: 150
    },
    {
        id: '33111111-1111-1111-1111-111111111001',
        name: 'Manchester',
        state: 'NH',
        cities: [
            'Manchester',
            'Nashua',
            'Concord',
            'Derry'
        ],
        centerLat: 42.9956,
        centerLng: -71.4548,
        radiusMiles: 25
    },
    // =====================
    // NEW JERSEY
    // =====================
    {
        id: '34000000-0000-0000-0000-000000000001',
        name: 'All New Jersey',
        state: 'NJ',
        cities: [
            'Statewide'
        ],
        centerLat: 40.2206,
        centerLng: -74.7597,
        radiusMiles: 150
    },
    {
        id: '34111111-1111-1111-1111-111111111001',
        name: 'Newark',
        state: 'NJ',
        cities: [
            'Newark',
            'Jersey City',
            'Paterson',
            'Elizabeth',
            'Edison',
            'Trenton'
        ],
        centerLat: 40.7357,
        centerLng: -74.1724,
        radiusMiles: 30
    },
    // =====================
    // NEW MEXICO
    // =====================
    {
        id: '35000000-0000-0000-0000-000000000001',
        name: 'All New Mexico',
        state: 'NM',
        cities: [
            'Statewide'
        ],
        centerLat: 34.8405,
        centerLng: -106.2371,
        radiusMiles: 250
    },
    {
        id: '35111111-1111-1111-1111-111111111001',
        name: 'Albuquerque',
        state: 'NM',
        cities: [
            'Albuquerque',
            'Rio Rancho',
            'Santa Fe',
            'Las Cruces'
        ],
        centerLat: 35.0844,
        centerLng: -106.6504,
        radiusMiles: 30
    },
    // =====================
    // NEW YORK
    // =====================
    {
        id: '36000000-0000-0000-0000-000000000001',
        name: 'All New York',
        state: 'NY',
        cities: [
            'Statewide'
        ],
        centerLat: 42.1657,
        centerLng: -74.9481,
        radiusMiles: 250
    },
    {
        id: '36111111-1111-1111-1111-111111111001',
        name: 'New York City',
        state: 'NY',
        cities: [
            'Manhattan',
            'Brooklyn',
            'Queens',
            'Bronx',
            'Staten Island',
            'Yonkers',
            'White Plains'
        ],
        centerLat: 40.7128,
        centerLng: -74.0060,
        radiusMiles: 40
    },
    {
        id: '36111111-1111-1111-1111-111111111002',
        name: 'Buffalo',
        state: 'NY',
        cities: [
            'Buffalo',
            'Cheektowaga',
            'West Seneca',
            'Amherst',
            'Tonawanda',
            'Niagara Falls'
        ],
        centerLat: 42.8864,
        centerLng: -78.8784,
        radiusMiles: 25
    },
    {
        id: '36111111-1111-1111-1111-111111111003',
        name: 'Albany',
        state: 'NY',
        cities: [
            'Albany',
            'Schenectady',
            'Troy',
            'Saratoga Springs'
        ],
        centerLat: 42.6526,
        centerLng: -73.7562,
        radiusMiles: 25
    },
    // =====================
    // NORTH CAROLINA
    // =====================
    {
        id: '37000000-0000-0000-0000-000000000001',
        name: 'All North Carolina',
        state: 'NC',
        cities: [
            'Statewide'
        ],
        centerLat: 35.6301,
        centerLng: -79.8064,
        radiusMiles: 200
    },
    {
        id: '37111111-1111-1111-1111-111111111001',
        name: 'Charlotte',
        state: 'NC',
        cities: [
            'Charlotte',
            'Concord',
            'Gastonia',
            'Rock Hill'
        ],
        centerLat: 35.2271,
        centerLng: -80.8431,
        radiusMiles: 30
    },
    {
        id: '37111111-1111-1111-1111-111111111002',
        name: 'Raleigh',
        state: 'NC',
        cities: [
            'Raleigh',
            'Durham',
            'Chapel Hill',
            'Cary',
            'Apex'
        ],
        centerLat: 35.7796,
        centerLng: -78.6382,
        radiusMiles: 30
    },
    // =====================
    // NORTH DAKOTA
    // =====================
    {
        id: '38000000-0000-0000-0000-000000000001',
        name: 'All North Dakota',
        state: 'ND',
        cities: [
            'Statewide'
        ],
        centerLat: 47.5289,
        centerLng: -99.7840,
        radiusMiles: 250
    },
    // =====================
    // OHIO
    // =====================
    {
        id: '39000000-0000-0000-0000-000000000001',
        name: 'All Ohio',
        state: 'OH',
        cities: [
            'Statewide'
        ],
        centerLat: 40.4173,
        centerLng: -82.9071,
        radiusMiles: 200
    },
    {
        id: '39111111-1111-1111-1111-111111111001',
        name: 'Cleveland',
        state: 'OH',
        cities: [
            'Cleveland',
            'Lakewood',
            'Parma',
            'Cleveland Heights',
            'Shaker Heights',
            'Euclid',
            'Mentor',
            'Strongsville',
            'Brunswick',
            'Westlake',
            'Aurora'
        ],
        centerLat: 41.4993,
        centerLng: -81.6944,
        radiusMiles: 30
    },
    {
        id: '39111111-1111-1111-1111-111111111002',
        name: 'Columbus',
        state: 'OH',
        cities: [
            'Columbus',
            'Dublin',
            'Westerville',
            'Grove City',
            'Hilliard',
            'Gahanna',
            'Upper Arlington',
            'Reynoldsburg',
            'Pickerington',
            'Worthington'
        ],
        centerLat: 39.9612,
        centerLng: -82.9988,
        radiusMiles: 30
    },
    {
        id: '39111111-1111-1111-1111-111111111003',
        name: 'Cincinnati',
        state: 'OH',
        cities: [
            'Cincinnati',
            'Mason',
            'Hamilton',
            'Fairfield',
            'Middletown',
            'Lebanon',
            'Blue Ash',
            'Sharonville',
            'West Chester',
            'Forest Park'
        ],
        centerLat: 39.1031,
        centerLng: -84.5120,
        radiusMiles: 30
    },
    {
        id: '39111111-1111-1111-1111-111111111004',
        name: 'Toledo',
        state: 'OH',
        cities: [
            'Toledo',
            'Sylvania',
            'Perrysburg',
            'Oregon',
            'Maumee',
            'Bowling Green',
            'Northwood',
            'Rossford'
        ],
        centerLat: 41.6528,
        centerLng: -83.5379,
        radiusMiles: 25
    },
    // =====================
    // OKLAHOMA
    // =====================
    {
        id: '40000000-0000-0000-0000-000000000001',
        name: 'All Oklahoma',
        state: 'OK',
        cities: [
            'Statewide'
        ],
        centerLat: 35.5653,
        centerLng: -96.9289,
        radiusMiles: 200
    },
    {
        id: '40111111-1111-1111-1111-111111111001',
        name: 'Oklahoma City',
        state: 'OK',
        cities: [
            'Oklahoma City',
            'Tulsa',
            'Norman',
            'Broken Arrow',
            'Edmond'
        ],
        centerLat: 35.4676,
        centerLng: -97.5164,
        radiusMiles: 30
    },
    // =====================
    // OREGON
    // =====================
    {
        id: '41000000-0000-0000-0000-000000000001',
        name: 'All Oregon',
        state: 'OR',
        cities: [
            'Statewide'
        ],
        centerLat: 43.8041,
        centerLng: -120.5542,
        radiusMiles: 200
    },
    {
        id: '41111111-1111-1111-1111-111111111001',
        name: 'Portland',
        state: 'OR',
        cities: [
            'Portland',
            'Eugene',
            'Salem',
            'Gresham',
            'Hillsboro',
            'Beaverton'
        ],
        centerLat: 45.5152,
        centerLng: -122.6784,
        radiusMiles: 30
    },
    // =====================
    // PENNSYLVANIA
    // =====================
    {
        id: '42000000-0000-0000-0000-000000000001',
        name: 'All Pennsylvania',
        state: 'PA',
        cities: [
            'Statewide'
        ],
        centerLat: 40.5908,
        centerLng: -77.2098,
        radiusMiles: 200
    },
    {
        id: '42111111-1111-1111-1111-111111111001',
        name: 'Philadelphia',
        state: 'PA',
        cities: [
            'Philadelphia',
            'Reading',
            'Allentown',
            'Bethlehem',
            'Lancaster'
        ],
        centerLat: 39.9526,
        centerLng: -75.1652,
        radiusMiles: 35
    },
    {
        id: '42111111-1111-1111-1111-111111111002',
        name: 'Pittsburgh',
        state: 'PA',
        cities: [
            'Pittsburgh',
            'Bethel Park',
            'Monroeville',
            'Mt. Lebanon',
            'Ross Township',
            'Moon Township'
        ],
        centerLat: 40.4406,
        centerLng: -79.9959,
        radiusMiles: 30
    },
    // =====================
    // RHODE ISLAND
    // =====================
    {
        id: '44000000-0000-0000-0000-000000000001',
        name: 'All Rhode Island',
        state: 'RI',
        cities: [
            'Statewide'
        ],
        centerLat: 41.6809,
        centerLng: -71.5118,
        radiusMiles: 120
    },
    {
        id: '44111111-1111-1111-1111-111111111001',
        name: 'Providence',
        state: 'RI',
        cities: [
            'Providence',
            'Warwick',
            'Cranston',
            'Pawtucket'
        ],
        centerLat: 41.8240,
        centerLng: -71.4128,
        radiusMiles: 25
    },
    // =====================
    // SOUTH CAROLINA
    // =====================
    {
        id: '45000000-0000-0000-0000-000000000001',
        name: 'All South Carolina',
        state: 'SC',
        cities: [
            'Statewide'
        ],
        centerLat: 33.8361,
        centerLng: -80.9066,
        radiusMiles: 200
    },
    {
        id: '45111111-1111-1111-1111-111111111001',
        name: 'Charleston',
        state: 'SC',
        cities: [
            'Charleston',
            'Columbia',
            'North Charleston',
            'Mount Pleasant'
        ],
        centerLat: 32.7765,
        centerLng: -79.9711,
        radiusMiles: 25
    },
    // =====================
    // SOUTH DAKOTA
    // =====================
    {
        id: '46000000-0000-0000-0000-000000000001',
        name: 'All South Dakota',
        state: 'SD',
        cities: [
            'Statewide'
        ],
        centerLat: 44.2998,
        centerLng: -99.4388,
        radiusMiles: 250
    },
    // =====================
    // TENNESSEE
    // =====================
    {
        id: '47000000-0000-0000-0000-000000000001',
        name: 'All Tennessee',
        state: 'TN',
        cities: [
            'Statewide'
        ],
        centerLat: 35.7478,
        centerLng: -86.6923,
        radiusMiles: 200
    },
    {
        id: '47111111-1111-1111-1111-111111111001',
        name: 'Nashville',
        state: 'TN',
        cities: [
            'Nashville',
            'Franklin',
            'Murfreesboro',
            'Brentwood'
        ],
        centerLat: 36.1627,
        centerLng: -86.7816,
        radiusMiles: 30
    },
    {
        id: '47111111-1111-1111-1111-111111111002',
        name: 'Memphis',
        state: 'TN',
        cities: [
            'Memphis',
            'Germantown',
            'Collierville',
            'Bartlett'
        ],
        centerLat: 35.1495,
        centerLng: -90.0490,
        radiusMiles: 30
    },
    // =====================
    // TEXAS
    // =====================
    {
        id: '48000000-0000-0000-0000-000000000001',
        name: 'All Texas',
        state: 'TX',
        cities: [
            'Statewide'
        ],
        centerLat: 31.9686,
        centerLng: -99.9018,
        radiusMiles: 300
    },
    {
        id: '48111111-1111-1111-1111-111111111001',
        name: 'Houston',
        state: 'TX',
        cities: [
            'Houston',
            'Sugar Land',
            'The Woodlands',
            'Pearland',
            'League City',
            'Pasadena',
            'Katy'
        ],
        centerLat: 29.7604,
        centerLng: -95.3698,
        radiusMiles: 40
    },
    {
        id: '48111111-1111-1111-1111-111111111002',
        name: 'Dallas-Fort Worth',
        state: 'TX',
        cities: [
            'Dallas',
            'Fort Worth',
            'Arlington',
            'Plano',
            'Irving',
            'Garland',
            'Frisco',
            'McKinney',
            'Grand Prairie'
        ],
        centerLat: 32.7767,
        centerLng: -96.7970,
        radiusMiles: 40
    },
    {
        id: '48111111-1111-1111-1111-111111111003',
        name: 'Austin',
        state: 'TX',
        cities: [
            'Austin',
            'Round Rock',
            'Cedar Park',
            'Georgetown',
            'Pflugerville',
            'San Marcos',
            'Leander'
        ],
        centerLat: 30.2672,
        centerLng: -97.7431,
        radiusMiles: 30
    },
    {
        id: '48111111-1111-1111-1111-111111111004',
        name: 'San Antonio',
        state: 'TX',
        cities: [
            'San Antonio',
            'New Braunfels',
            'Schertz',
            'Seguin',
            'Universal City',
            'Converse'
        ],
        centerLat: 29.4241,
        centerLng: -98.4936,
        radiusMiles: 30
    },
    // =====================
    // UTAH
    // =====================
    {
        id: '49000000-0000-0000-0000-000000000001',
        name: 'All Utah',
        state: 'UT',
        cities: [
            'Statewide'
        ],
        centerLat: 39.3210,
        centerLng: -111.0937,
        radiusMiles: 200
    },
    {
        id: '49111111-1111-1111-1111-111111111001',
        name: 'Salt Lake City',
        state: 'UT',
        cities: [
            'Salt Lake City',
            'West Valley City',
            'Provo',
            'West Jordan',
            'Orem',
            'Sandy'
        ],
        centerLat: 40.7608,
        centerLng: -111.8910,
        radiusMiles: 30
    },
    // =====================
    // VERMONT
    // =====================
    {
        id: '50000000-0000-0000-0000-000000000001',
        name: 'All Vermont',
        state: 'VT',
        cities: [
            'Statewide'
        ],
        centerLat: 44.0459,
        centerLng: -72.7107,
        radiusMiles: 150
    },
    // =====================
    // VIRGINIA
    // =====================
    {
        id: '51000000-0000-0000-0000-000000000001',
        name: 'All Virginia',
        state: 'VA',
        cities: [
            'Statewide'
        ],
        centerLat: 37.7693,
        centerLng: -78.1694,
        radiusMiles: 200
    },
    {
        id: '51111111-1111-1111-1111-111111111001',
        name: 'Richmond',
        state: 'VA',
        cities: [
            'Richmond',
            'Virginia Beach',
            'Norfolk',
            'Chesapeake',
            'Arlington',
            'Alexandria'
        ],
        centerLat: 37.5407,
        centerLng: -77.4360,
        radiusMiles: 30
    },
    // =====================
    // WASHINGTON
    // =====================
    {
        id: '53000000-0000-0000-0000-000000000001',
        name: 'All Washington',
        state: 'WA',
        cities: [
            'Statewide'
        ],
        centerLat: 47.7511,
        centerLng: -120.7401,
        radiusMiles: 250
    },
    {
        id: '53111111-1111-1111-1111-111111111001',
        name: 'Seattle',
        state: 'WA',
        cities: [
            'Seattle',
            'Bellevue',
            'Tacoma',
            'Everett',
            'Kent',
            'Renton',
            'Spokane',
            'Redmond',
            'Kirkland'
        ],
        centerLat: 47.6062,
        centerLng: -122.3321,
        radiusMiles: 35
    },
    // =====================
    // WEST VIRGINIA
    // =====================
    {
        id: '54000000-0000-0000-0000-000000000001',
        name: 'All West Virginia',
        state: 'WV',
        cities: [
            'Statewide'
        ],
        centerLat: 38.5976,
        centerLng: -80.4549,
        radiusMiles: 200
    },
    // =====================
    // WISCONSIN
    // =====================
    {
        id: '55000000-0000-0000-0000-000000000001',
        name: 'All Wisconsin',
        state: 'WI',
        cities: [
            'Statewide'
        ],
        centerLat: 44.2685,
        centerLng: -89.6165,
        radiusMiles: 200
    },
    {
        id: '55111111-1111-1111-1111-111111111001',
        name: 'Milwaukee',
        state: 'WI',
        cities: [
            'Milwaukee',
            'Madison',
            'Green Bay',
            'Kenosha',
            'Racine'
        ],
        centerLat: 43.0389,
        centerLng: -87.9065,
        radiusMiles: 30
    },
    // =====================
    // WYOMING
    // =====================
    {
        id: '56000000-0000-0000-0000-000000000001',
        name: 'All Wyoming',
        state: 'WY',
        cities: [
            'Statewide'
        ],
        centerLat: 42.7559,
        centerLng: -107.3025,
        radiusMiles: 250
    }
];
function getMetroById(id) {
    return ALL_METRO_AREAS.find((metro)=>metro.id === id);
}
function getMetrosByState(stateCode) {
    return ALL_METRO_AREAS.filter((metro)=>metro.state === stateCode);
}
function getStateName(stateCode) {
    return US_STATES.find((state)=>state.code === stateCode)?.name;
}
function searchMetrosByName(query) {
    const lowerQuery = query.toLowerCase().trim();
    if (!lowerQuery) {
        return ALL_METRO_AREAS;
    }
    return ALL_METRO_AREAS.filter((metro)=>metro.name.toLowerCase().includes(lowerQuery) || metro.cities.some((city)=>city.toLowerCase().includes(lowerQuery)) || metro.state.toLowerCase() === lowerQuery);
}
function isStateLevelArea(metroId) {
    const metro = getMetroById(metroId);
    return metro?.name.startsWith('All ') ?? false;
}
function getStateLevelAreas() {
    return ALL_METRO_AREAS.filter((metro)=>metro.name.startsWith('All '));
}
function getCityLevelAreas() {
    return ALL_METRO_AREAS.filter((metro)=>!metro.name.startsWith('All '));
}
function getMetrosGroupedByState() {
    const grouped = new Map();
    for (const metro of ALL_METRO_AREAS){
        if (!grouped.has(metro.state)) {
            grouped.set(metro.state, []);
        }
        grouped.get(metro.state).push(metro);
    }
    // Ensure state-level area is first in each group
    for (const [state, metros] of grouped){
        metros.sort((a, b)=>{
            if (a.name.startsWith('All ')) return -1;
            if (b.name.startsWith('All ')) return 1;
            return a.name.localeCompare(b.name);
        });
    }
    return grouped;
}
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "NewsletterMetroSelector",
    ()=>NewsletterMetroSelector
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$down$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronDown$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/chevron-down.js [app-client] (ecmascript) <export default as ChevronDown>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$right$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronRight$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/chevron-right.js [app-client] (ecmascript) <export default as ChevronRight>");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$domain$2f$constants$2f$metroAreas$2e$constants$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/domain/constants/metroAreas.constants.ts [app-client] (ecmascript)");
;
var _s = __turbopack_context__.k.signature();
'use client';
;
;
;
function NewsletterMetroSelector({ selectedMetroIds, receiveAllLocations, onMetrosChange, onReceiveAllChange, disabled = false, maxSelections = 20 }) {
    _s();
    const [expandedStates, setExpandedStates] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(new Set());
    const [validationError, setValidationError] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])('');
    // Check validation whenever selectedMetroIds changes
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useEffect"])({
        "NewsletterMetroSelector.useEffect": ()=>{
            if (selectedMetroIds.length > maxSelections) {
                setValidationError(`You cannot select more than ${maxSelections} metro areas`);
            } else {
                setValidationError('');
            }
        }
    }["NewsletterMetroSelector.useEffect"], [
        selectedMetroIds,
        maxSelections
    ]);
    const metrosByState = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$domain$2f$constants$2f$metroAreas$2e$constants$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["getMetrosGroupedByState"])();
    const toggleStateExpansion = (stateCode)=>{
        const newExpanded = new Set(expandedStates);
        if (newExpanded.has(stateCode)) {
            newExpanded.delete(stateCode);
        } else {
            newExpanded.add(stateCode);
        }
        setExpandedStates(newExpanded);
    };
    const handleToggleMetroArea = (metroId)=>{
        const newSelection = selectedMetroIds.includes(metroId) ? selectedMetroIds.filter((id)=>id !== metroId) : selectedMetroIds.length < maxSelections ? [
            ...selectedMetroIds,
            metroId
        ] : selectedMetroIds;
        if (newSelection.length > maxSelections) {
            setValidationError(`You cannot select more than ${maxSelections} metro areas`);
        } else {
            setValidationError('');
            onMetrosChange(newSelection);
        }
    };
    const handleReceiveAllChange = (receiveAll)=>{
        onReceiveAllChange(receiveAll);
        if (receiveAll) {
            onMetrosChange([]); // Clear selections when choosing all locations
            setValidationError('');
        }
    };
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        className: "space-y-4",
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                        className: "text-sm font-medium text-gray-700 mb-2 block",
                        children: "📍 Get notifications for events in:"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                        lineNumber: 87,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                        className: "text-xs text-gray-500 mb-3",
                        children: [
                            "Select up to ",
                            maxSelections,
                            " metro areas or receive updates from all locations"
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                        lineNumber: 90,
                        columnNumber: 9
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                lineNumber: 86,
                columnNumber: 7
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "mb-4",
                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                    className: "flex items-center text-sm text-gray-700 cursor-pointer",
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
                            type: "checkbox",
                            checked: receiveAllLocations,
                            onChange: (e)=>handleReceiveAllChange(e.target.checked),
                            disabled: disabled,
                            className: "mr-2 w-4 h-4 rounded border-gray-300 text-[#FF7900] focus:ring-2 focus:ring-[#FF7900]"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                            lineNumber: 98,
                            columnNumber: 11
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                            className: "font-medium",
                            children: "Send me events from all locations"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                            lineNumber: 105,
                            columnNumber: 11
                        }, this)
                    ]
                }, void 0, true, {
                    fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                    lineNumber: 97,
                    columnNumber: 9
                }, this)
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                lineNumber: 96,
                columnNumber: 7
            }, this),
            !receiveAllLocations && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "border rounded-lg p-4 bg-white space-y-4",
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h4", {
                                className: "text-xs font-semibold uppercase tracking-wider text-gray-700 mb-3",
                                children: "State-Wide Selections"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                lineNumber: 114,
                                columnNumber: 13
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "space-y-2",
                                children: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$domain$2f$constants$2f$metroAreas$2e$constants$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["getStateLevelAreas"])().map((metro)=>{
                                    const isSelected = selectedMetroIds.includes(metro.id);
                                    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                        className: `flex items-center gap-3 p-2 rounded-md border cursor-pointer transition-colors ${disabled ? 'opacity-50 cursor-not-allowed' : ''}`,
                                        style: {
                                            background: isSelected ? '#FFE8CC' : 'white',
                                            borderColor: isSelected ? '#FF7900' : '#e2e8f0'
                                        },
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
                                                type: "checkbox",
                                                checked: isSelected,
                                                onChange: ()=>handleToggleMetroArea(metro.id),
                                                disabled: disabled,
                                                className: "h-4 w-4 rounded border-gray-300",
                                                "aria-label": `Select all of ${metro.name}`
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                                lineNumber: 131,
                                                columnNumber: 21
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                className: "flex-1 text-sm font-medium",
                                                children: [
                                                    "All ",
                                                    metro.name
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                                lineNumber: 139,
                                                columnNumber: 21
                                            }, this)
                                        ]
                                    }, metro.id, true, {
                                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                        lineNumber: 121,
                                        columnNumber: 19
                                    }, this);
                                })
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                lineNumber: 117,
                                columnNumber: 13
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                        lineNumber: 113,
                        columnNumber: 11
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "h-px bg-gray-200"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                        lineNumber: 147,
                        columnNumber: 11
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h4", {
                                className: "text-xs font-semibold uppercase tracking-wider text-gray-700 mb-3",
                                children: "City Metro Areas"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                lineNumber: 151,
                                columnNumber: 13
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "space-y-2",
                                children: __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$domain$2f$constants$2f$metroAreas$2e$constants$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["US_STATES"].map((state)=>{
                                    const metrosForState = metrosByState.get(state.code) || [];
                                    const cityMetros = metrosForState.filter((m)=>!(0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$domain$2f$constants$2f$metroAreas$2e$constants$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["isStateLevelArea"])(m.id));
                                    if (cityMetros.length === 0) return null;
                                    const isExpanded = expandedStates.has(state.code);
                                    const selectedCountInState = selectedMetroIds.filter((id)=>metrosForState.map((m)=>m.id).includes(id) && !(0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$domain$2f$constants$2f$metroAreas$2e$constants$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["isStateLevelArea"])(id)).length;
                                    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "border rounded-md overflow-hidden",
                                        style: {
                                            borderColor: '#e2e8f0'
                                        },
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                                onClick: ()=>toggleStateExpansion(state.code),
                                                disabled: disabled,
                                                className: `w-full flex items-center gap-2 p-3 text-left transition-colors text-sm ${disabled ? 'opacity-50 cursor-not-allowed' : 'hover:bg-gray-50'}`,
                                                "aria-expanded": isExpanded,
                                                "aria-controls": `newsletter-metros-${state.code}`,
                                                children: [
                                                    isExpanded ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$down$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronDown$3e$__["ChevronDown"], {
                                                        className: "h-4 w-4",
                                                        style: {
                                                            color: '#FF7900'
                                                        }
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                                        lineNumber: 183,
                                                        columnNumber: 25
                                                    }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$right$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronRight$3e$__["ChevronRight"], {
                                                        className: "h-4 w-4",
                                                        style: {
                                                            color: '#FF7900'
                                                        }
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                                        lineNumber: 185,
                                                        columnNumber: 25
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                        className: "flex-1 font-medium",
                                                        children: state.name
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                                        lineNumber: 187,
                                                        columnNumber: 23
                                                    }, this),
                                                    selectedCountInState > 0 && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                        className: "text-xs font-semibold px-2 py-0.5 rounded-full",
                                                        style: {
                                                            background: '#FFE8CC',
                                                            color: '#8B1538'
                                                        },
                                                        children: [
                                                            selectedCountInState,
                                                            " selected"
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                                        lineNumber: 189,
                                                        columnNumber: 25
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                                lineNumber: 173,
                                                columnNumber: 21
                                            }, this),
                                            isExpanded && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                id: `newsletter-metros-${state.code}`,
                                                className: "space-y-2 p-3 bg-gray-50 border-t",
                                                style: {
                                                    borderColor: '#e2e8f0'
                                                },
                                                children: cityMetros.map((metro)=>{
                                                    const isSelected = selectedMetroIds.includes(metro.id);
                                                    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                        className: `flex items-start gap-3 p-2 rounded-md border cursor-pointer transition-colors ${disabled ? 'opacity-50 cursor-not-allowed' : ''}`,
                                                        style: {
                                                            background: isSelected ? '#FFE8CC' : 'white',
                                                            borderColor: isSelected ? '#FF7900' : '#e2e8f0'
                                                        },
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
                                                                type: "checkbox",
                                                                checked: isSelected,
                                                                onChange: ()=>handleToggleMetroArea(metro.id),
                                                                disabled: disabled,
                                                                className: "mt-0.5 h-4 w-4 rounded border-gray-300",
                                                                "aria-label": `${metro.name}, ${metro.state}`
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                                                lineNumber: 218,
                                                                columnNumber: 31
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                className: "flex-1 min-w-0",
                                                                children: [
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                        className: "text-sm font-medium",
                                                                        children: metro.name
                                                                    }, void 0, false, {
                                                                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                                                        lineNumber: 227,
                                                                        columnNumber: 33
                                                                    }, this),
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                        className: "text-xs text-gray-500",
                                                                        children: [
                                                                            metro.cities.slice(0, 2).join(', '),
                                                                            metro.cities.length > 2 && `, +${metro.cities.length - 2} more`
                                                                        ]
                                                                    }, void 0, true, {
                                                                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                                                        lineNumber: 228,
                                                                        columnNumber: 33
                                                                    }, this)
                                                                ]
                                                            }, void 0, true, {
                                                                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                                                lineNumber: 226,
                                                                columnNumber: 31
                                                            }, this)
                                                        ]
                                                    }, metro.id, true, {
                                                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                                        lineNumber: 208,
                                                        columnNumber: 29
                                                    }, this);
                                                })
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                                lineNumber: 200,
                                                columnNumber: 23
                                            }, this)
                                        ]
                                    }, state.code, true, {
                                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                        lineNumber: 167,
                                        columnNumber: 19
                                    }, this);
                                })
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                lineNumber: 154,
                                columnNumber: 13
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                        lineNumber: 150,
                        columnNumber: 11
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "pt-2 border-t",
                        style: {
                            borderColor: '#e2e8f0'
                        },
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                className: "text-xs text-gray-600",
                                children: [
                                    selectedMetroIds.length,
                                    " of ",
                                    maxSelections,
                                    " selected"
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                lineNumber: 246,
                                columnNumber: 13
                            }, this),
                            validationError && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                className: "text-xs text-red-600 mt-1",
                                role: "alert",
                                children: validationError
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                                lineNumber: 250,
                                columnNumber: 15
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                        lineNumber: 245,
                        columnNumber: 11
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                lineNumber: 111,
                columnNumber: 9
            }, this)
        ]
    }, void 0, true, {
        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
        lineNumber: 84,
        columnNumber: 5
    }, this);
}
_s(NewsletterMetroSelector, "/DB2eX5YiMrCa1/rzBO+V5YHBlU=");
_c = NewsletterMetroSelector;
var _c;
__turbopack_context__.k.register(_c, "NewsletterMetroSelector");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/layout/Footer.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "default",
    ()=>__TURBOPACK__default__export__
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$build$2f$polyfills$2f$process$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = /*#__PURE__*/ __turbopack_context__.i("[project]/node_modules/next/dist/build/polyfills/process.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/client/app-dir/link.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$atoms$2f$Logo$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/atoms/Logo.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$newsletter$2f$NewsletterMetroSelector$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx [app-client] (ecmascript)");
;
var _s = __turbopack_context__.k.signature();
'use client';
;
;
;
;
const FooterLink = ({ href, children, external = false })=>{
    const linkClasses = "text-white/80 hover:text-[#FF7900] transition-colors duration-200";
    if (external) {
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
            href: href,
            target: "_blank",
            rel: "noopener noreferrer",
            className: linkClasses,
            "aria-label": `${children} (opens in new tab)`,
            children: children
        }, void 0, false, {
            fileName: "[project]/src/presentation/components/layout/Footer.tsx",
            lineNumber: 19,
            columnNumber: 7
        }, ("TURBOPACK compile-time value", void 0));
    }
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
        href: href,
        className: linkClasses,
        children: children
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
        lineNumber: 32,
        columnNumber: 5
    }, ("TURBOPACK compile-time value", void 0));
};
_c = FooterLink;
const Footer = ()=>{
    _s();
    const [email, setEmail] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])('');
    const [subscribeStatus, setSubscribeStatus] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])('idle');
    const [selectedMetroIds, setSelectedMetroIds] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])([]);
    const [receiveAllLocations, setReceiveAllLocations] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(false);
    const [currentYear, setCurrentYear] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(2025); // Initialize with static value
    // Set current year on client side only to avoid hydration mismatch
    __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"].useEffect({
        "Footer.useEffect": ()=>{
            setCurrentYear(new Date().getFullYear());
        }
    }["Footer.useEffect"], []);
    const linkCategories = [
        {
            title: 'About',
            links: [
                {
                    label: 'About Us',
                    href: '/about'
                },
                {
                    label: 'Our Mission',
                    href: '/mission'
                },
                {
                    label: 'Team',
                    href: '/team'
                },
                {
                    label: 'Contact',
                    href: '/contact'
                }
            ]
        },
        {
            title: 'Community',
            links: [
                {
                    label: 'Events',
                    href: '/events'
                },
                {
                    label: 'Forums',
                    href: '/forums'
                },
                {
                    label: 'Businesses',
                    href: '/businesses'
                },
                {
                    label: 'Culture',
                    href: '/culture'
                }
            ]
        },
        {
            title: 'Resources',
            links: [
                {
                    label: 'Help Center',
                    href: '/help'
                },
                {
                    label: 'Privacy Policy',
                    href: '/privacy'
                },
                {
                    label: 'Terms of Service',
                    href: '/terms'
                },
                {
                    label: 'FAQ',
                    href: '/faq'
                }
            ]
        },
        {
            title: 'Connect',
            links: [
                {
                    label: 'Facebook',
                    href: 'https://facebook.com',
                    external: true
                },
                {
                    label: 'Twitter',
                    href: 'https://twitter.com',
                    external: true
                },
                {
                    label: 'Instagram',
                    href: 'https://instagram.com',
                    external: true
                },
                {
                    label: 'LinkedIn',
                    href: 'https://linkedin.com',
                    external: true
                }
            ]
        }
    ];
    const handleNewsletterSubmit = async (e)=>{
        e.preventDefault();
        if (!email || !email.includes('@')) {
            setSubscribeStatus('error');
            return;
        }
        if (!receiveAllLocations && selectedMetroIds.length === 0) {
            setSubscribeStatus('error');
            return;
        }
        setSubscribeStatus('loading');
        try {
            // Call .NET backend API
            const apiUrl = ("TURBOPACK compile-time value", "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api") || 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api';
            const response = await fetch(`${apiUrl}/newsletter/subscribe`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    email,
                    metroAreaIds: receiveAllLocations ? [] : selectedMetroIds,
                    receiveAllLocations,
                    timestamp: new Date().toISOString()
                })
            });
            const data = await response.json();
            if (data.success) {
                setSubscribeStatus('success');
                setEmail('');
                setSelectedMetroIds([]);
                setReceiveAllLocations(false);
                // Reset status after 3 seconds
                setTimeout(()=>{
                    setSubscribeStatus('idle');
                }, 3000);
            } else {
                setSubscribeStatus('error');
            }
        } catch (error) {
            console.error('Newsletter subscription error:', error);
            setSubscribeStatus('error');
        }
    };
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("footer", {
        className: "bg-gradient-to-b from-[#8B1538] to-[#6B0F28] text-white",
        role: "contentinfo",
        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "container mx-auto px-4 sm:px-6 lg:px-8 py-12",
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "mb-12 pb-8 border-b border-white/10",
                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "flex flex-col lg:flex-row lg:items-center lg:justify-between gap-8",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex-1",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "mb-3",
                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$atoms$2f$Logo$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Logo"], {
                                            size: "md"
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                            lineNumber: 159,
                                            columnNumber: 17
                                        }, ("TURBOPACK compile-time value", void 0))
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 158,
                                        columnNumber: 15
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                        className: "text-white/70 text-sm max-w-md",
                                        children: "Connecting the Sri Lankan diaspora worldwide. Share events, discover culture, and build community together."
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 161,
                                        columnNumber: 15
                                    }, ("TURBOPACK compile-time value", void 0))
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                lineNumber: 157,
                                columnNumber: 13
                            }, ("TURBOPACK compile-time value", void 0)),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex-1 max-w-md",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
                                        className: "text-lg font-semibold mb-3",
                                        children: "Stay Connected"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 168,
                                        columnNumber: 15
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                        className: "text-white/70 text-sm mb-4",
                                        children: "Subscribe to our newsletter for the latest events and community updates."
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 169,
                                        columnNumber: 15
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("form", {
                                        onSubmit: handleNewsletterSubmit,
                                        className: "space-y-3",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
                                                type: "email",
                                                placeholder: "Enter your email",
                                                value: email,
                                                onChange: (e)=>setEmail(e.target.value),
                                                className: "w-full px-4 py-2.5 rounded-lg bg-white text-[#8B1538] placeholder-[#8B1538]/50 focus:outline-none focus:ring-2 focus:ring-[#FF7900]",
                                                "aria-label": "Email address for newsletter",
                                                disabled: subscribeStatus === 'loading',
                                                required: true
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                                lineNumber: 173,
                                                columnNumber: 17
                                            }, ("TURBOPACK compile-time value", void 0)),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "bg-white/95 p-4 rounded-lg text-gray-800",
                                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$newsletter$2f$NewsletterMetroSelector$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["NewsletterMetroSelector"], {
                                                    selectedMetroIds: selectedMetroIds,
                                                    receiveAllLocations: receiveAllLocations,
                                                    onMetrosChange: setSelectedMetroIds,
                                                    onReceiveAllChange: setReceiveAllLocations,
                                                    disabled: subscribeStatus === 'loading'
                                                }, void 0, false, {
                                                    fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                                    lineNumber: 186,
                                                    columnNumber: 19
                                                }, ("TURBOPACK compile-time value", void 0))
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                                lineNumber: 185,
                                                columnNumber: 17
                                            }, ("TURBOPACK compile-time value", void 0)),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                                type: "submit",
                                                className: "w-full px-6 py-2.5 bg-[#FF7900] hover:bg-[#E56D00] text-white font-medium rounded-lg transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed",
                                                disabled: subscribeStatus === 'loading',
                                                "aria-label": "Subscribe to newsletter",
                                                children: subscribeStatus === 'loading' ? 'Subscribing...' : subscribeStatus === 'success' ? 'Subscribed!' : 'Subscribe'
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                                lineNumber: 195,
                                                columnNumber: 17
                                            }, ("TURBOPACK compile-time value", void 0))
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 172,
                                        columnNumber: 15
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    subscribeStatus === 'error' && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                        className: "text-red-300 text-sm mt-2",
                                        role: "alert",
                                        children: "Please enter a valid email address and select at least one location."
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 205,
                                        columnNumber: 17
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    subscribeStatus === 'success' && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                        className: "text-green-300 text-sm mt-2",
                                        role: "alert",
                                        children: "Thank you for subscribing!"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 210,
                                        columnNumber: 17
                                    }, ("TURBOPACK compile-time value", void 0))
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                lineNumber: 167,
                                columnNumber: 13
                            }, ("TURBOPACK compile-time value", void 0))
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                        lineNumber: 155,
                        columnNumber: 11
                    }, ("TURBOPACK compile-time value", void 0))
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                    lineNumber: 154,
                    columnNumber: 9
                }, ("TURBOPACK compile-time value", void 0)),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "grid grid-cols-2 md:grid-cols-4 gap-8 mb-12",
                    children: linkCategories.map((category)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
                                    className: "text-lg font-semibold mb-4 text-white",
                                    children: category.title
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                    lineNumber: 222,
                                    columnNumber: 15
                                }, ("TURBOPACK compile-time value", void 0)),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("ul", {
                                    className: "space-y-3",
                                    role: "list",
                                    children: category.links.map((link)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("li", {
                                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(FooterLink, {
                                                href: link.href,
                                                external: link.external,
                                                children: link.label
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                                lineNumber: 226,
                                                columnNumber: 21
                                            }, ("TURBOPACK compile-time value", void 0))
                                        }, link.label, false, {
                                            fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                            lineNumber: 225,
                                            columnNumber: 19
                                        }, ("TURBOPACK compile-time value", void 0)))
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                    lineNumber: 223,
                                    columnNumber: 15
                                }, ("TURBOPACK compile-time value", void 0))
                            ]
                        }, category.title, true, {
                            fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                            lineNumber: 221,
                            columnNumber: 13
                        }, ("TURBOPACK compile-time value", void 0)))
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                    lineNumber: 219,
                    columnNumber: 9
                }, ("TURBOPACK compile-time value", void 0)),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "pt-8 border-t border-white/10",
                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "flex flex-col md:flex-row md:items-center md:justify-between gap-4 text-sm text-white/60",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                children: [
                                    "© ",
                                    currentYear,
                                    " LankaConnect. All rights reserved."
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                lineNumber: 239,
                                columnNumber: 13
                            }, ("TURBOPACK compile-time value", void 0)),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex gap-6",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
                                        href: "/privacy",
                                        className: "hover:text-[#FF7900] transition-colors duration-200",
                                        children: "Privacy"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 243,
                                        columnNumber: 15
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
                                        href: "/terms",
                                        className: "hover:text-[#FF7900] transition-colors duration-200",
                                        children: "Terms"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 246,
                                        columnNumber: 15
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
                                        href: "/cookies",
                                        className: "hover:text-[#FF7900] transition-colors duration-200",
                                        children: "Cookies"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 249,
                                        columnNumber: 15
                                    }, ("TURBOPACK compile-time value", void 0))
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                lineNumber: 242,
                                columnNumber: 13
                            }, ("TURBOPACK compile-time value", void 0))
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                        lineNumber: 238,
                        columnNumber: 11
                    }, ("TURBOPACK compile-time value", void 0))
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                    lineNumber: 237,
                    columnNumber: 9
                }, ("TURBOPACK compile-time value", void 0))
            ]
        }, void 0, true, {
            fileName: "[project]/src/presentation/components/layout/Footer.tsx",
            lineNumber: 152,
            columnNumber: 7
        }, ("TURBOPACK compile-time value", void 0))
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
        lineNumber: 151,
        columnNumber: 5
    }, ("TURBOPACK compile-time value", void 0));
};
_s(Footer, "XSTpmnBLPHWcklGKLvdJfxUoEU0=");
_c1 = Footer;
const __TURBOPACK__default__export__ = Footer;
var _c, _c1;
__turbopack_context__.k.register(_c, "FooterLink");
__turbopack_context__.k.register(_c1, "Footer");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/infrastructure/api/repositories/auth.repository.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "AuthRepository",
    ()=>AuthRepository,
    "authRepository",
    ()=>authRepository
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/client/api-client.ts [app-client] (ecmascript)");
;
class AuthRepository {
    basePath = '/auth';
    /**
   * Login user
   */ async login(credentials) {
        const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/login`, credentials);
        return response;
    }
    /**
   * Register new user
   */ async register(userData) {
        const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/register`, userData);
        return response;
    }
    /**
   * Refresh access token
   */ async refreshToken(refreshToken) {
        const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/refresh-token`, {
            refreshToken
        });
        return response;
    }
    /**
   * Logout user
   */ async logout() {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/logout`);
    }
    /**
   * Request password reset
   */ async requestPasswordReset(email) {
        const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/forgot-password`, {
            email
        });
        return response;
    }
    /**
   * Reset password with token
   */ async resetPassword(token, newPassword) {
        const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/reset-password`, {
            token,
            newPassword
        });
        return response;
    }
    /**
   * Verify email with token
   */ async verifyEmail(token) {
        const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/verify-email`, {
            token
        });
        return response;
    }
    /**
   * Resend verification email
   */ async resendVerificationEmail(email) {
        const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/resend-verification`, {
            email
        });
        return response;
    }
    /**
   * [TEST ONLY] Verify user email without token validation
   * Only available in Development environment for E2E testing
   */ async testVerifyEmail(userId) {
        const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/test/verify-user/${userId}`, {});
        return response;
    }
}
const authRepository = new AuthRepository();
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/infrastructure/api/types/auth.types.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

/**
 * Auth API Types
 * DTOs matching backend API contracts for authentication
 */ // Request DTOs
__turbopack_context__.s([
    "UserRole",
    ()=>UserRole
]);
var UserRole = /*#__PURE__*/ function(UserRole) {
    UserRole["GeneralUser"] = "GeneralUser";
    UserRole["EventOrganizer"] = "EventOrganizer";
    UserRole["Admin"] = "Admin";
    UserRole["AdminManager"] = "AdminManager";
    return UserRole;
}({});
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/infrastructure/api/types/subscription.types.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

/**
 * Subscription Types
 * Phase 6A.1: Subscription management for Event Organizer accounts
 */ __turbopack_context__.s([
    "SubscriptionStatus",
    ()=>SubscriptionStatus
]);
var SubscriptionStatus = /*#__PURE__*/ function(SubscriptionStatus) {
    SubscriptionStatus["None"] = "None";
    SubscriptionStatus["Trialing"] = "Trialing";
    SubscriptionStatus["Active"] = "Active";
    SubscriptionStatus["PastDue"] = "PastDue";
    SubscriptionStatus["Canceled"] = "Canceled";
    SubscriptionStatus["Expired"] = "Expired";
    return SubscriptionStatus;
}({});
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/infrastructure/api/utils/role-helpers.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "canCreateEvents",
    ()=>canCreateEvents,
    "canCreateEventsWithSubscription",
    ()=>canCreateEventsWithSubscription,
    "canManageUsers",
    ()=>canManageUsers,
    "canModerateContent",
    ()=>canModerateContent,
    "getFreeTrialDaysRemaining",
    ()=>getFreeTrialDaysRemaining,
    "getRoleDisplayName",
    ()=>getRoleDisplayName,
    "getSubscriptionStatusDisplay",
    ()=>getSubscriptionStatusDisplay,
    "isAdmin",
    ()=>isAdmin,
    "isEventOrganizer",
    ()=>isEventOrganizer,
    "isSubscriptionActive",
    ()=>isSubscriptionActive,
    "requiresPayment",
    ()=>requiresPayment,
    "requiresSubscription",
    ()=>requiresSubscription
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/types/auth.types.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$subscription$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/types/subscription.types.ts [app-client] (ecmascript)");
;
;
function getRoleDisplayName(role) {
    switch(role){
        case __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["UserRole"].GeneralUser:
            return 'General User';
        case __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["UserRole"].EventOrganizer:
            return 'Event Organizer';
        case __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["UserRole"].Admin:
            return 'Administrator';
        case __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["UserRole"].AdminManager:
            return 'Admin Manager';
        default:
            return role;
    }
}
function canManageUsers(role) {
    return role === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["UserRole"].Admin || role === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["UserRole"].AdminManager;
}
function canCreateEvents(role, subscriptionStatus) {
    // General Users cannot create events
    if (role === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["UserRole"].GeneralUser) {
        return false;
    }
    // Admins can always create events
    if (isAdmin(role)) {
        return true;
    }
    // Event Organizers must have active subscription (trialing or paid)
    if (role === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["UserRole"].EventOrganizer) {
        if (!subscriptionStatus) {
            return false;
        }
        return canCreateEventsWithSubscription(subscriptionStatus);
    }
    return false;
}
function canModerateContent(role) {
    return role === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["UserRole"].Admin || role === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["UserRole"].AdminManager;
}
function isEventOrganizer(role) {
    return role === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["UserRole"].EventOrganizer;
}
function isAdmin(role) {
    return role === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["UserRole"].Admin || role === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["UserRole"].AdminManager;
}
function requiresSubscription(role) {
    return role === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["UserRole"].EventOrganizer;
}
function getSubscriptionStatusDisplay(status) {
    switch(status){
        case __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$subscription$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SubscriptionStatus"].None:
            return 'No Subscription';
        case __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$subscription$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SubscriptionStatus"].Trialing:
            return 'Free Trial';
        case __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$subscription$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SubscriptionStatus"].Active:
            return 'Active';
        case __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$subscription$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SubscriptionStatus"].PastDue:
            return 'Past Due';
        case __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$subscription$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SubscriptionStatus"].Canceled:
            return 'Canceled';
        case __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$subscription$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SubscriptionStatus"].Expired:
            return 'Expired';
        default:
            return status;
    }
}
function canCreateEventsWithSubscription(status) {
    return status === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$subscription$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SubscriptionStatus"].Trialing || status === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$subscription$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SubscriptionStatus"].Active;
}
function requiresPayment(status) {
    return status === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$subscription$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SubscriptionStatus"].PastDue || status === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$subscription$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SubscriptionStatus"].Expired;
}
function isSubscriptionActive(status) {
    return status === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$subscription$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SubscriptionStatus"].Trialing || status === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$subscription$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SubscriptionStatus"].Active;
}
function getFreeTrialDaysRemaining(freeTrialEndsAt) {
    if (!freeTrialEndsAt) {
        return null;
    }
    const endDate = new Date(freeTrialEndsAt);
    const now = new Date();
    const daysRemaining = Math.ceil((endDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
    return daysRemaining > 0 ? daysRemaining : 0;
}
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/features/dashboard/CulturalCalendar.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "CulturalCalendar",
    ()=>CulturalCalendar
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-client] (ecmascript)");
;
var _s = __turbopack_context__.k.signature();
;
;
const categoryStyles = {
    national: 'bg-blue-100 text-blue-800',
    religious: 'bg-purple-100 text-purple-800',
    cultural: 'bg-green-100 text-green-800',
    holiday: 'bg-orange-100 text-orange-800'
};
const formatDate = (dateString)=>{
    // Parse date as UTC to avoid timezone offset issues
    const [year, month, day] = dateString.split('-').map(Number);
    const date = new Date(Date.UTC(year, month - 1, day));
    return {
        day: day.toString(),
        month: date.toLocaleDateString('en-US', {
            month: 'short',
            timeZone: 'UTC'
        }).toUpperCase()
    };
};
const CulturalCalendar = /*#__PURE__*/ _s(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["forwardRef"](_c = _s(({ className, events, ...props }, ref)=>{
    _s();
    // Sort events by date
    const sortedEvents = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMemo"]({
        "CulturalCalendar.useMemo[sortedEvents]": ()=>{
            return [
                ...events
            ].sort({
                "CulturalCalendar.useMemo[sortedEvents]": (a, b)=>new Date(a.date).getTime() - new Date(b.date).getTime()
            }["CulturalCalendar.useMemo[sortedEvents]"]);
        }
    }["CulturalCalendar.useMemo[sortedEvents]"], [
        events
    ]);
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('', className),
        ...props,
        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "rounded-xl overflow-hidden",
            style: {
                background: 'white',
                boxShadow: '0 4px 6px rgba(0, 0, 0, 0.05)'
            },
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "px-5 py-4 font-semibold border-b",
                    style: {
                        background: 'linear-gradient(135deg, rgba(255,121,0,0.1) 0%, rgba(139,21,56,0.1) 100%)',
                        borderBottom: '1px solid #e2e8f0',
                        color: '#8B1538'
                    },
                    children: "🗓️ Cultural Calendar"
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/features/dashboard/CulturalCalendar.tsx",
                    lineNumber: 56,
                    columnNumber: 11
                }, ("TURBOPACK compile-time value", void 0)),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "p-5",
                    children: sortedEvents.length === 0 ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                        className: "text-sm text-center py-4",
                        style: {
                            color: '#718096'
                        },
                        children: "No upcoming events at this time"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/dashboard/CulturalCalendar.tsx",
                        lineNumber: 70,
                        columnNumber: 15
                    }, ("TURBOPACK compile-time value", void 0)) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "space-y-0",
                        role: "list",
                        children: sortedEvents.map((event, index)=>{
                            const dateInfo = formatDate(event.date);
                            return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex items-center py-3",
                                style: {
                                    borderBottom: index === sortedEvents.length - 1 ? 'none' : '1px solid #f1f5f9'
                                },
                                role: "listitem",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "rounded-lg text-center flex-shrink-0 mr-4",
                                        style: {
                                            background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)',
                                            color: 'white',
                                            padding: '0.5rem',
                                            minWidth: '60px'
                                        },
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                style: {
                                                    fontSize: '1.25rem',
                                                    fontWeight: 'bold',
                                                    lineHeight: '1'
                                                },
                                                children: dateInfo.day
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/dashboard/CulturalCalendar.tsx",
                                                lineNumber: 96,
                                                columnNumber: 25
                                            }, ("TURBOPACK compile-time value", void 0)),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                style: {
                                                    fontSize: '0.75rem',
                                                    opacity: 0.9
                                                },
                                                children: dateInfo.month
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/dashboard/CulturalCalendar.tsx",
                                                lineNumber: 99,
                                                columnNumber: 25
                                            }, ("TURBOPACK compile-time value", void 0))
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/presentation/components/features/dashboard/CulturalCalendar.tsx",
                                        lineNumber: 87,
                                        columnNumber: 23
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "flex-1 min-w-0",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h4", {
                                                "data-testid": "event-name",
                                                className: "font-medium truncate",
                                                style: {
                                                    fontSize: '0.9rem',
                                                    marginBottom: '0.25rem',
                                                    color: '#2d3748'
                                                },
                                                children: event.name
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/dashboard/CulturalCalendar.tsx",
                                                lineNumber: 106,
                                                columnNumber: 25
                                            }, ("TURBOPACK compile-time value", void 0)),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                style: {
                                                    fontSize: '0.8rem',
                                                    color: '#718096'
                                                },
                                                children: event.category === 'national' ? 'National holiday' : event.category === 'religious' ? 'Religious celebration' : event.category === 'cultural' ? 'Cultural event' : 'Public holiday'
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/dashboard/CulturalCalendar.tsx",
                                                lineNumber: 113,
                                                columnNumber: 25
                                            }, ("TURBOPACK compile-time value", void 0))
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/presentation/components/features/dashboard/CulturalCalendar.tsx",
                                        lineNumber: 105,
                                        columnNumber: 23
                                    }, ("TURBOPACK compile-time value", void 0))
                                ]
                            }, event.id, true, {
                                fileName: "[project]/src/presentation/components/features/dashboard/CulturalCalendar.tsx",
                                lineNumber: 78,
                                columnNumber: 21
                            }, ("TURBOPACK compile-time value", void 0));
                        })
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/dashboard/CulturalCalendar.tsx",
                        lineNumber: 74,
                        columnNumber: 15
                    }, ("TURBOPACK compile-time value", void 0))
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/features/dashboard/CulturalCalendar.tsx",
                    lineNumber: 68,
                    columnNumber: 11
                }, ("TURBOPACK compile-time value", void 0))
            ]
        }, void 0, true, {
            fileName: "[project]/src/presentation/components/features/dashboard/CulturalCalendar.tsx",
            lineNumber: 48,
            columnNumber: 9
        }, ("TURBOPACK compile-time value", void 0))
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/features/dashboard/CulturalCalendar.tsx",
        lineNumber: 47,
        columnNumber: 7
    }, ("TURBOPACK compile-time value", void 0));
}, "D9AhzRGMQpDZtzTokzolR5s5XV8=")), "D9AhzRGMQpDZtzTokzolR5s5XV8=");
_c1 = CulturalCalendar;
CulturalCalendar.displayName = 'CulturalCalendar';
var _c, _c1;
__turbopack_context__.k.register(_c, "CulturalCalendar$React.forwardRef");
__turbopack_context__.k.register(_c1, "CulturalCalendar");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/features/dashboard/FeaturedBusinesses.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "FeaturedBusinesses",
    ()=>FeaturedBusinesses
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$star$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Star$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/star.js [app-client] (ecmascript) <export default as Star>");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-client] (ecmascript)");
;
var _s = __turbopack_context__.k.signature();
;
;
;
const renderStars = (rating)=>{
    const stars = [];
    const fullStars = Math.floor(rating);
    for(let i = 0; i < 5; i++){
        stars.push(/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$star$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Star$3e$__["Star"], {
            className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('h-4 w-4', i < fullStars ? 'text-yellow-400 fill-yellow-400' : 'text-gray-300'),
            "data-icon": "star"
        }, i, false, {
            fileName: "[project]/src/presentation/components/features/dashboard/FeaturedBusinesses.tsx",
            lineNumber: 27,
            columnNumber: 7
        }, ("TURBOPACK compile-time value", void 0)));
    }
    return stars;
};
const FeaturedBusinesses = /*#__PURE__*/ _s(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["forwardRef"](_c = _s(({ className, businesses, onBusinessClick, limit, ...props }, ref)=>{
    _s();
    const displayedBusinesses = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMemo"]({
        "FeaturedBusinesses.useMemo[displayedBusinesses]": ()=>{
            return limit ? businesses.slice(0, limit) : businesses;
        }
    }["FeaturedBusinesses.useMemo[displayedBusinesses]"], [
        businesses,
        limit
    ]);
    const handleClick = (businessId)=>{
        if (onBusinessClick) {
            onBusinessClick(businessId);
        }
    };
    const handleKeyPress = (event, businessId)=>{
        if (event.key === 'Enter' && onBusinessClick) {
            onBusinessClick(businessId);
        }
    };
    const getInitials = (name)=>{
        return name.split(' ').map((word)=>word[0]).join('').toUpperCase().slice(0, 2);
    };
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('', className),
        ...props,
        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "rounded-xl overflow-hidden",
            style: {
                background: 'white',
                boxShadow: '0 4px 6px rgba(0, 0, 0, 0.05)'
            },
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "px-5 py-4 font-semibold border-b",
                    style: {
                        background: 'linear-gradient(135deg, rgba(255,121,0,0.1) 0%, rgba(139,21,56,0.1) 100%)',
                        borderBottom: '1px solid #e2e8f0',
                        color: '#8B1538'
                    },
                    children: "⭐ Featured Businesses"
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/features/dashboard/FeaturedBusinesses.tsx",
                    lineNumber: 83,
                    columnNumber: 11
                }, ("TURBOPACK compile-time value", void 0)),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "p-5",
                    children: displayedBusinesses.length === 0 ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                        className: "text-sm text-center py-4",
                        style: {
                            color: '#718096'
                        },
                        children: "No featured businesses available"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/dashboard/FeaturedBusinesses.tsx",
                        lineNumber: 97,
                        columnNumber: 15
                    }, ("TURBOPACK compile-time value", void 0)) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "space-y-0",
                        children: displayedBusinesses.map((business, index)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                role: onBusinessClick ? 'button' : undefined,
                                tabIndex: onBusinessClick ? 0 : undefined,
                                onClick: ()=>onBusinessClick && handleClick(business.id),
                                onKeyPress: (e)=>handleKeyPress(e, business.id),
                                className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('flex items-center py-3 transition-all', onBusinessClick && 'cursor-pointer'),
                                style: {
                                    borderBottom: index === displayedBusinesses.length - 1 ? 'none' : '1px solid #f1f5f9'
                                },
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "rounded-lg flex items-center justify-center text-white font-bold flex-shrink-0 mr-3",
                                        style: {
                                            background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)',
                                            width: '40px',
                                            height: '40px',
                                            fontSize: '0.8rem'
                                        },
                                        children: getInitials(business.name)
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/dashboard/FeaturedBusinesses.tsx",
                                        lineNumber: 118,
                                        columnNumber: 21
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "flex-1 min-w-0",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h4", {
                                                className: "font-medium truncate",
                                                style: {
                                                    fontSize: '0.9rem',
                                                    marginBottom: '0.25rem',
                                                    color: '#2d3748'
                                                },
                                                children: business.name
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/dashboard/FeaturedBusinesses.tsx",
                                                lineNumber: 132,
                                                columnNumber: 23
                                            }, ("TURBOPACK compile-time value", void 0)),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                style: {
                                                    fontSize: '0.8rem',
                                                    color: '#718096'
                                                },
                                                children: business.category
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/dashboard/FeaturedBusinesses.tsx",
                                                lineNumber: 138,
                                                columnNumber: 23
                                            }, ("TURBOPACK compile-time value", void 0))
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/presentation/components/features/dashboard/FeaturedBusinesses.tsx",
                                        lineNumber: 131,
                                        columnNumber: 21
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "ml-auto text-sm font-medium",
                                        style: {
                                            color: '#FFD700'
                                        },
                                        children: [
                                            "⭐ ",
                                            business.rating.toFixed(1)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/presentation/components/features/dashboard/FeaturedBusinesses.tsx",
                                        lineNumber: 144,
                                        columnNumber: 21
                                    }, ("TURBOPACK compile-time value", void 0))
                                ]
                            }, business.id, true, {
                                fileName: "[project]/src/presentation/components/features/dashboard/FeaturedBusinesses.tsx",
                                lineNumber: 103,
                                columnNumber: 19
                            }, ("TURBOPACK compile-time value", void 0)))
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/dashboard/FeaturedBusinesses.tsx",
                        lineNumber: 101,
                        columnNumber: 15
                    }, ("TURBOPACK compile-time value", void 0))
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/features/dashboard/FeaturedBusinesses.tsx",
                    lineNumber: 95,
                    columnNumber: 11
                }, ("TURBOPACK compile-time value", void 0))
            ]
        }, void 0, true, {
            fileName: "[project]/src/presentation/components/features/dashboard/FeaturedBusinesses.tsx",
            lineNumber: 75,
            columnNumber: 9
        }, ("TURBOPACK compile-time value", void 0))
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/features/dashboard/FeaturedBusinesses.tsx",
        lineNumber: 74,
        columnNumber: 7
    }, ("TURBOPACK compile-time value", void 0));
}, "8j5XouiBxu6lrx5ECSojrNkdjLA=")), "8j5XouiBxu6lrx5ECSojrNkdjLA=");
_c1 = FeaturedBusinesses;
FeaturedBusinesses.displayName = 'FeaturedBusinesses';
var _c, _c1;
__turbopack_context__.k.register(_c, "FeaturedBusinesses$React.forwardRef");
__turbopack_context__.k.register(_c1, "FeaturedBusinesses");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/features/dashboard/CommunityStats.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "CommunityStats",
    ()=>CommunityStats
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-client] (ecmascript)");
;
;
;
const formatNumber = (num)=>{
    return num.toLocaleString('en-US');
};
const LoadingSkeleton = ()=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        className: "space-y-4",
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h2", {
                className: "text-lg font-semibold",
                children: "Community Statistics"
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                lineNumber: 27,
                columnNumber: 5
            }, ("TURBOPACK compile-time value", void 0)),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "grid grid-cols-1 md:grid-cols-3 gap-4",
                children: [
                    1,
                    2,
                    3
                ].map((i)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "h-32 bg-gray-200 rounded-lg animate-pulse"
                    }, i, false, {
                        fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                        lineNumber: 30,
                        columnNumber: 9
                    }, ("TURBOPACK compile-time value", void 0)))
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                lineNumber: 28,
                columnNumber: 5
            }, ("TURBOPACK compile-time value", void 0)),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                className: "text-sm text-muted-foreground text-center",
                children: "Loading statistics..."
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                lineNumber: 36,
                columnNumber: 5
            }, ("TURBOPACK compile-time value", void 0))
        ]
    }, void 0, true, {
        fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
        lineNumber: 26,
        columnNumber: 3
    }, ("TURBOPACK compile-time value", void 0));
_c = LoadingSkeleton;
const CommunityStats = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["forwardRef"](_c1 = ({ className, stats, isLoading, error, ...props }, ref)=>{
    if (isLoading) {
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            ref: ref,
            className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('', className),
            ...props,
            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(LoadingSkeleton, {}, void 0, false, {
                fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                lineNumber: 50,
                columnNumber: 11
            }, ("TURBOPACK compile-time value", void 0))
        }, void 0, false, {
            fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
            lineNumber: 49,
            columnNumber: 9
        }, ("TURBOPACK compile-time value", void 0));
    }
    if (error) {
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            ref: ref,
            className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('', className),
            ...props,
            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "space-y-4",
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h2", {
                        className: "text-lg font-semibold",
                        children: "Community Statistics"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                        lineNumber: 59,
                        columnNumber: 13
                    }, ("TURBOPACK compile-time value", void 0)),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "p-4 bg-red-50 border border-red-200 rounded-lg",
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                            className: "text-sm text-red-600",
                            children: error
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                            lineNumber: 61,
                            columnNumber: 15
                        }, ("TURBOPACK compile-time value", void 0))
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                        lineNumber: 60,
                        columnNumber: 13
                    }, ("TURBOPACK compile-time value", void 0))
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                lineNumber: 58,
                columnNumber: 11
            }, ("TURBOPACK compile-time value", void 0))
        }, void 0, false, {
            fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
            lineNumber: 57,
            columnNumber: 9
        }, ("TURBOPACK compile-time value", void 0));
    }
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('', className),
        ...props,
        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "rounded-xl overflow-hidden",
            style: {
                background: 'white',
                boxShadow: '0 4px 6px rgba(0, 0, 0, 0.05)'
            },
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "px-5 py-4 font-semibold border-b",
                    style: {
                        background: 'linear-gradient(135deg, rgba(255,121,0,0.1) 0%, rgba(139,21,56,0.1) 100%)',
                        borderBottom: '1px solid #e2e8f0',
                        color: '#8B1538'
                    },
                    children: "📊 Community Stats"
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                    lineNumber: 78,
                    columnNumber: 11
                }, ("TURBOPACK compile-time value", void 0)),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "p-5",
                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "space-y-4",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex justify-between items-center",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                        style: {
                                            color: '#718096'
                                        },
                                        children: "Active Today"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                                        lineNumber: 94,
                                        columnNumber: 17
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("strong", {
                                        style: {
                                            color: '#2d3748'
                                        },
                                        children: formatNumber(stats.activeUsers)
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                                        lineNumber: 95,
                                        columnNumber: 17
                                    }, ("TURBOPACK compile-time value", void 0))
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                                lineNumber: 93,
                                columnNumber: 15
                            }, ("TURBOPACK compile-time value", void 0)),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex justify-between items-center",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                        style: {
                                            color: '#718096'
                                        },
                                        children: "Events This Week"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                                        lineNumber: 100,
                                        columnNumber: 17
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("strong", {
                                        style: {
                                            color: '#2d3748'
                                        },
                                        children: formatNumber(stats.recentPosts)
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                                        lineNumber: 101,
                                        columnNumber: 17
                                    }, ("TURBOPACK compile-time value", void 0))
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                                lineNumber: 99,
                                columnNumber: 15
                            }, ("TURBOPACK compile-time value", void 0)),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex justify-between items-center",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                        style: {
                                            color: '#718096'
                                        },
                                        children: "New Businesses"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                                        lineNumber: 106,
                                        columnNumber: 17
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("strong", {
                                        style: {
                                            color: '#2d3748'
                                        },
                                        children: formatNumber(23)
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                                        lineNumber: 107,
                                        columnNumber: 17
                                    }, ("TURBOPACK compile-time value", void 0))
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                                lineNumber: 105,
                                columnNumber: 15
                            }, ("TURBOPACK compile-time value", void 0)),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex justify-between items-center",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                        style: {
                                            color: '#718096'
                                        },
                                        children: "Forum Discussions"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                                        lineNumber: 112,
                                        columnNumber: 17
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("strong", {
                                        style: {
                                            color: '#2d3748'
                                        },
                                        children: formatNumber(stats.upcomingEvents)
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                                        lineNumber: 113,
                                        columnNumber: 17
                                    }, ("TURBOPACK compile-time value", void 0))
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                                lineNumber: 111,
                                columnNumber: 15
                            }, ("TURBOPACK compile-time value", void 0))
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                        lineNumber: 91,
                        columnNumber: 13
                    }, ("TURBOPACK compile-time value", void 0))
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
                    lineNumber: 90,
                    columnNumber: 11
                }, ("TURBOPACK compile-time value", void 0))
            ]
        }, void 0, true, {
            fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
            lineNumber: 70,
            columnNumber: 9
        }, ("TURBOPACK compile-time value", void 0))
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/features/dashboard/CommunityStats.tsx",
        lineNumber: 69,
        columnNumber: 7
    }, ("TURBOPACK compile-time value", void 0));
});
_c2 = CommunityStats;
CommunityStats.displayName = 'CommunityStats';
var _c, _c1, _c2;
__turbopack_context__.k.register(_c, "LoadingSkeleton");
__turbopack_context__.k.register(_c1, "CommunityStats$React.forwardRef");
__turbopack_context__.k.register(_c2, "CommunityStats");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/infrastructure/api/repositories/role-upgrade.repository.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "RoleUpgradeRepository",
    ()=>RoleUpgradeRepository,
    "roleUpgradeRepository",
    ()=>roleUpgradeRepository
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/client/api-client.ts [app-client] (ecmascript)");
;
class RoleUpgradeRepository {
    basePath = '/users/me';
    /**
   * Request a role upgrade to Event Organizer
   */ async requestUpgrade(request) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/request-upgrade`, request);
        return {
            success: true,
            message: 'Role upgrade request submitted successfully'
        };
    }
    /**
   * Cancel a pending role upgrade request
   */ async cancelUpgrade() {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/cancel-upgrade`, {});
        return {
            success: true,
            message: 'Role upgrade request canceled successfully'
        };
    }
}
const roleUpgradeRepository = new RoleUpgradeRepository();
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/hooks/useRoleUpgrade.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "useCancelRoleUpgrade",
    ()=>useCancelRoleUpgrade,
    "useRequestRoleUpgrade",
    ()=>useRequestRoleUpgrade
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/useMutation.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/QueryClientProvider.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$role$2d$upgrade$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/repositories/role-upgrade.repository.ts [app-client] (ecmascript)");
var _s = __turbopack_context__.k.signature(), _s1 = __turbopack_context__.k.signature();
;
;
function useRequestRoleUpgrade() {
    _s();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: {
            "useRequestRoleUpgrade.useMutation": (request)=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$role$2d$upgrade$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["roleUpgradeRepository"].requestUpgrade(request)
        }["useRequestRoleUpgrade.useMutation"],
        onSuccess: {
            "useRequestRoleUpgrade.useMutation": ()=>{
                // Invalidate user query to refresh user data with pending upgrade status
                queryClient.invalidateQueries({
                    queryKey: [
                        'user'
                    ]
                });
            }
        }["useRequestRoleUpgrade.useMutation"]
    });
}
_s(useRequestRoleUpgrade, "YK0wzM21ECnncaq5SECwU+/SVdQ=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"],
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"]
    ];
});
function useCancelRoleUpgrade() {
    _s1();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: {
            "useCancelRoleUpgrade.useMutation": ()=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$role$2d$upgrade$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["roleUpgradeRepository"].cancelUpgrade()
        }["useCancelRoleUpgrade.useMutation"],
        onSuccess: {
            "useCancelRoleUpgrade.useMutation": ()=>{
                // Invalidate user query to refresh user data
                queryClient.invalidateQueries({
                    queryKey: [
                        'user'
                    ]
                });
            }
        }["useCancelRoleUpgrade.useMutation"]
    });
}
_s1(useCancelRoleUpgrade, "YK0wzM21ECnncaq5SECwU+/SVdQ=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"],
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"]
    ];
});
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "UpgradeModal",
    ()=>UpgradeModal
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$x$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__X$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/x.js [app-client] (ecmascript) <export default as X>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$check$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Check$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/check.js [app-client] (ecmascript) <export default as Check>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$sparkles$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Sparkles$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/sparkles.js [app-client] (ecmascript) <export default as Sparkles>");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Button.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useRoleUpgrade$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/hooks/useRoleUpgrade.ts [app-client] (ecmascript)");
;
var _s = __turbopack_context__.k.signature();
'use client';
;
;
;
;
function UpgradeModal({ isOpen, onClose }) {
    _s();
    const [reason, setReason] = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"]('');
    const [showSuccess, setShowSuccess] = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"](false);
    const requestUpgrade = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useRoleUpgrade$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRequestRoleUpgrade"])();
    // Reset state when modal opens/closes
    __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useEffect"]({
        "UpgradeModal.useEffect": ()=>{
            if (!isOpen) {
                setReason('');
                setShowSuccess(false);
                requestUpgrade.reset();
            }
        }
    }["UpgradeModal.useEffect"], [
        isOpen,
        requestUpgrade
    ]);
    const handleSubmit = async (e)=>{
        e.preventDefault();
        if (reason.trim().length < 20) {
            return;
        }
        try {
            await requestUpgrade.mutateAsync({
                targetRole: 'EventOrganizer',
                reason: reason.trim()
            });
            setShowSuccess(true);
            setTimeout(()=>{
                onClose();
            }, 2000);
        } catch (error) {
            console.error('Failed to request upgrade:', error);
        }
    };
    if (!isOpen) return null;
    if (showSuccess) {
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4",
            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "bg-white rounded-xl p-8 max-w-md w-full text-center",
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4",
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$check$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Check$3e$__["Check"], {
                            className: "w-8 h-8 text-green-600"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                            lineNumber: 66,
                            columnNumber: 13
                        }, this)
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                        lineNumber: 65,
                        columnNumber: 11
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
                        className: "text-2xl font-bold text-[#8B1538] mb-2",
                        children: "Request Submitted!"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                        lineNumber: 68,
                        columnNumber: 11
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                        className: "text-gray-600",
                        children: "Your upgrade request has been submitted for admin review. You'll be notified once it's processed."
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                        lineNumber: 69,
                        columnNumber: 11
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                lineNumber: 64,
                columnNumber: 9
            }, this)
        }, void 0, false, {
            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
            lineNumber: 63,
            columnNumber: 7
        }, this);
    }
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        className: "fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4",
        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "bg-white rounded-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto",
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "p-6 flex items-center justify-between",
                    style: {
                        background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)'
                    },
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                            className: "flex items-center gap-3",
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$sparkles$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Sparkles$3e$__["Sparkles"], {
                                    className: "w-6 h-6 text-white"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                    lineNumber: 86,
                                    columnNumber: 13
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h2", {
                                    className: "text-2xl font-bold text-white",
                                    children: "Upgrade to Event Organizer"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                    lineNumber: 87,
                                    columnNumber: 13
                                }, this)
                            ]
                        }, void 0, true, {
                            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                            lineNumber: 85,
                            columnNumber: 11
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                            onClick: onClose,
                            className: "text-white hover:bg-white/20 rounded-lg p-2 transition-colors",
                            disabled: requestUpgrade.isPending,
                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$x$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__X$3e$__["X"], {
                                className: "w-6 h-6"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                lineNumber: 94,
                                columnNumber: 13
                            }, this)
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                            lineNumber: 89,
                            columnNumber: 11
                        }, this)
                    ]
                }, void 0, true, {
                    fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                    lineNumber: 81,
                    columnNumber: 9
                }, this),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "p-6",
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                            className: "mb-6",
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
                                    className: "text-lg font-semibold text-[#8B1538] mb-4",
                                    children: "Event Organizer Benefits"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                    lineNumber: 102,
                                    columnNumber: 13
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "space-y-3",
                                    children: [
                                        'Create and manage unlimited events',
                                        '6-month free trial to explore all features',
                                        'Access to event analytics and insights',
                                        'Priority event listing in search results',
                                        'Custom event branding and themes',
                                        'Email notifications to interested attendees'
                                    ].map((benefit, index)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                            className: "flex items-start gap-3",
                                            children: [
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                    className: "w-6 h-6 rounded-full bg-green-100 flex items-center justify-center flex-shrink-0 mt-0.5",
                                                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$check$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Check$3e$__["Check"], {
                                                        className: "w-4 h-4 text-green-600"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                                        lineNumber: 114,
                                                        columnNumber: 21
                                                    }, this)
                                                }, void 0, false, {
                                                    fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                                    lineNumber: 113,
                                                    columnNumber: 19
                                                }, this),
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                    className: "text-gray-700",
                                                    children: benefit
                                                }, void 0, false, {
                                                    fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                                    lineNumber: 116,
                                                    columnNumber: 19
                                                }, this)
                                            ]
                                        }, index, true, {
                                            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                            lineNumber: 112,
                                            columnNumber: 17
                                        }, this))
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                    lineNumber: 103,
                                    columnNumber: 13
                                }, this)
                            ]
                        }, void 0, true, {
                            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                            lineNumber: 101,
                            columnNumber: 11
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                            className: "mb-6 p-4 bg-orange-50 rounded-lg border border-orange-200",
                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                className: "text-sm text-gray-700",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                        className: "font-semibold text-[#FF7900]",
                                        children: "Free for 6 months!"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                        lineNumber: 125,
                                        columnNumber: 15
                                    }, this),
                                    " After your trial, Event Organizer subscription is just $9.99/month. Cancel anytime."
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                lineNumber: 124,
                                columnNumber: 13
                            }, this)
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                            lineNumber: 123,
                            columnNumber: 11
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("form", {
                            onSubmit: handleSubmit,
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "mb-6",
                                    children: [
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                            className: "block text-sm font-medium text-gray-700 mb-2",
                                            children: [
                                                "Tell us why you want to become an Event Organizer ",
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                    className: "text-red-500",
                                                    children: "*"
                                                }, void 0, false, {
                                                    fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                                    lineNumber: 134,
                                                    columnNumber: 67
                                                }, this)
                                            ]
                                        }, void 0, true, {
                                            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                            lineNumber: 133,
                                            columnNumber: 15
                                        }, this),
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("textarea", {
                                            value: reason,
                                            onChange: (e)=>setReason(e.target.value),
                                            className: "w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#FF7900] focus:border-transparent transition-all resize-none",
                                            rows: 4,
                                            placeholder: "Describe the types of events you plan to organize and how you'll engage the community... (minimum 20 characters)",
                                            disabled: requestUpgrade.isPending,
                                            required: true
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                            lineNumber: 136,
                                            columnNumber: 15
                                        }, this),
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                            className: "text-sm text-gray-500 mt-1",
                                            children: [
                                                reason.length,
                                                "/20 characters minimum"
                                            ]
                                        }, void 0, true, {
                                            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                            lineNumber: 145,
                                            columnNumber: 15
                                        }, this)
                                    ]
                                }, void 0, true, {
                                    fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                    lineNumber: 132,
                                    columnNumber: 13
                                }, this),
                                requestUpgrade.isError && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "mb-4 p-4 bg-red-50 border border-red-200 rounded-lg",
                                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                        className: "text-sm text-red-600",
                                        children: requestUpgrade.error instanceof Error ? requestUpgrade.error.message : 'Failed to submit upgrade request. Please try again.'
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                        lineNumber: 153,
                                        columnNumber: 17
                                    }, this)
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                    lineNumber: 152,
                                    columnNumber: 15
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "flex gap-3",
                                    children: [
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                            type: "button",
                                            onClick: onClose,
                                            variant: "outline",
                                            className: "flex-1",
                                            disabled: requestUpgrade.isPending,
                                            children: "Cancel"
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                            lineNumber: 163,
                                            columnNumber: 15
                                        }, this),
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                            type: "submit",
                                            className: "flex-1",
                                            style: {
                                                background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)',
                                                color: 'white'
                                            },
                                            disabled: requestUpgrade.isPending || reason.trim().length < 20,
                                            children: requestUpgrade.isPending ? 'Submitting...' : 'Submit Request'
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                            lineNumber: 172,
                                            columnNumber: 15
                                        }, this)
                                    ]
                                }, void 0, true, {
                                    fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                                    lineNumber: 162,
                                    columnNumber: 13
                                }, this)
                            ]
                        }, void 0, true, {
                            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                            lineNumber: 131,
                            columnNumber: 11
                        }, this)
                    ]
                }, void 0, true, {
                    fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
                    lineNumber: 99,
                    columnNumber: 9
                }, this)
            ]
        }, void 0, true, {
            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
            lineNumber: 79,
            columnNumber: 7
        }, this)
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx",
        lineNumber: 78,
        columnNumber: 5
    }, this);
}
_s(UpgradeModal, "+qwR1kobo/trtqdoNXVdJzV+oIQ=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useRoleUpgrade$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRequestRoleUpgrade"]
    ];
});
_c = UpgradeModal;
var _c;
__turbopack_context__.k.register(_c, "UpgradeModal");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "UpgradePendingBanner",
    ()=>UpgradePendingBanner
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$clock$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Clock$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/clock.js [app-client] (ecmascript) <export default as Clock>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$x$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__X$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/x.js [app-client] (ecmascript) <export default as X>");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Button.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useRoleUpgrade$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/hooks/useRoleUpgrade.ts [app-client] (ecmascript)");
;
var _s = __turbopack_context__.k.signature();
'use client';
;
;
;
;
function UpgradePendingBanner({ upgradeRequestedAt }) {
    _s();
    const [isDismissed, setIsDismissed] = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"](false);
    const [showCancelConfirm, setShowCancelConfirm] = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"](false);
    const cancelUpgrade = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useRoleUpgrade$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useCancelRoleUpgrade"])();
    const handleCancel = async ()=>{
        try {
            await cancelUpgrade.mutateAsync();
            setShowCancelConfirm(false);
        } catch (error) {
            console.error('Failed to cancel upgrade:', error);
        }
    };
    if (isDismissed) return null;
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Fragment"], {
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "mb-6 p-4 rounded-lg border-2 flex items-start gap-4",
                style: {
                    background: 'linear-gradient(135deg, #FFF5E6 0%, #FFE8CC 100%)',
                    borderColor: '#FF7900'
                },
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "flex-shrink-0",
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                            className: "w-10 h-10 rounded-full bg-[#FF7900] flex items-center justify-center",
                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$clock$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Clock$3e$__["Clock"], {
                                className: "w-5 h-5 text-white"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                                lineNumber: 50,
                                columnNumber: 13
                            }, this)
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                            lineNumber: 49,
                            columnNumber: 11
                        }, this)
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                        lineNumber: 48,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "flex-1",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
                                className: "text-lg font-semibold text-[#8B1538] mb-1",
                                children: "Event Organizer Upgrade Pending"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                                lineNumber: 55,
                                columnNumber: 11
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                className: "text-gray-700 text-sm mb-3",
                                children: "Your request to become an Event Organizer is under review. Our team will review your application and notify you within 2-3 business days."
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                                lineNumber: 58,
                                columnNumber: 11
                            }, this),
                            upgradeRequestedAt && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                className: "text-xs text-gray-600",
                                children: [
                                    "Requested on: ",
                                    new Date(upgradeRequestedAt).toLocaleDateString('en-US', {
                                        month: 'long',
                                        day: 'numeric',
                                        year: 'numeric',
                                        hour: '2-digit',
                                        minute: '2-digit'
                                    })
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                                lineNumber: 63,
                                columnNumber: 13
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "mt-3 flex gap-2",
                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                    size: "sm",
                                    variant: "outline",
                                    onClick: ()=>setShowCancelConfirm(true),
                                    disabled: cancelUpgrade.isPending,
                                    className: "text-sm",
                                    style: {
                                        borderColor: '#8B1538',
                                        color: '#8B1538'
                                    },
                                    children: cancelUpgrade.isPending ? 'Canceling...' : 'Cancel Request'
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                                    lineNumber: 75,
                                    columnNumber: 13
                                }, this)
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                                lineNumber: 74,
                                columnNumber: 11
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                        lineNumber: 54,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                        onClick: ()=>setIsDismissed(true),
                        className: "flex-shrink-0 text-gray-500 hover:text-gray-700 transition-colors",
                        title: "Dismiss banner",
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$x$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__X$3e$__["X"], {
                            className: "w-5 h-5"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                            lineNumber: 96,
                            columnNumber: 11
                        }, this)
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                        lineNumber: 91,
                        columnNumber: 9
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                lineNumber: 41,
                columnNumber: 7
            }, this),
            showCancelConfirm && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4",
                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "bg-white rounded-xl p-6 max-w-md w-full",
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
                            className: "text-xl font-bold text-[#8B1538] mb-3",
                            children: "Cancel Upgrade Request?"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                            lineNumber: 104,
                            columnNumber: 13
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                            className: "text-gray-700 mb-6",
                            children: "Are you sure you want to cancel your Event Organizer upgrade request? You can submit a new request anytime."
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                            lineNumber: 105,
                            columnNumber: 13
                        }, this),
                        cancelUpgrade.isError && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                            className: "mb-4 p-3 bg-red-50 border border-red-200 rounded-lg",
                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                className: "text-sm text-red-600",
                                children: "Failed to cancel request. Please try again."
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                                lineNumber: 112,
                                columnNumber: 17
                            }, this)
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                            lineNumber: 111,
                            columnNumber: 15
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                            className: "flex gap-3",
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                    variant: "outline",
                                    onClick: ()=>setShowCancelConfirm(false),
                                    className: "flex-1",
                                    disabled: cancelUpgrade.isPending,
                                    children: "Keep Request"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                                    lineNumber: 119,
                                    columnNumber: 15
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                    onClick: handleCancel,
                                    className: "flex-1",
                                    style: {
                                        background: '#8B1538',
                                        color: 'white'
                                    },
                                    disabled: cancelUpgrade.isPending,
                                    children: cancelUpgrade.isPending ? 'Canceling...' : 'Yes, Cancel'
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                                    lineNumber: 127,
                                    columnNumber: 15
                                }, this)
                            ]
                        }, void 0, true, {
                            fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                            lineNumber: 118,
                            columnNumber: 13
                        }, this)
                    ]
                }, void 0, true, {
                    fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                    lineNumber: 103,
                    columnNumber: 11
                }, this)
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx",
                lineNumber: 102,
                columnNumber: 9
            }, this)
        ]
    }, void 0, true);
}
_s(UpgradePendingBanner, "mXM5tlRMCV9Cw4Aj2pEOQ/TxArk=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useRoleUpgrade$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useCancelRoleUpgrade"]
    ];
});
_c = UpgradePendingBanner;
var _c;
__turbopack_context__.k.register(_c, "UpgradePendingBanner");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/app/(dashboard)/dashboard/page.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "default",
    ()=>DashboardPage
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$auth$2f$ProtectedRoute$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/auth/ProtectedRoute.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/store/useAuthStore.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Button.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$atoms$2f$Logo$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/atoms/Logo.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Footer$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/layout/Footer.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/navigation.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$auth$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/repositories/auth.repository.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$utils$2f$role$2d$helpers$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/utils/role-helpers.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/types/auth.types.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$calendar$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Calendar$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/calendar.js [app-client] (ecmascript) <export default as Calendar>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$message$2d$square$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__MessageSquare$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/message-square.js [app-client] (ecmascript) <export default as MessageSquare>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$heart$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Heart$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/heart.js [app-client] (ecmascript) <export default as Heart>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$message$2d$circle$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__MessageCircle$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/message-circle.js [app-client] (ecmascript) <export default as MessageCircle>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$share$2d$2$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Share2$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/share-2.js [app-client] (ecmascript) <export default as Share2>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$down$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronDown$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/chevron-down.js [app-client] (ecmascript) <export default as ChevronDown>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$user$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__User$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/user.js [app-client] (ecmascript) <export default as User>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$log$2d$out$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__LogOut$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/log-out.js [app-client] (ecmascript) <export default as LogOut>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$sparkles$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Sparkles$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/sparkles.js [app-client] (ecmascript) <export default as Sparkles>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$dashboard$2f$CulturalCalendar$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/features/dashboard/CulturalCalendar.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$dashboard$2f$FeaturedBusinesses$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/features/dashboard/FeaturedBusinesses.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$dashboard$2f$CommunityStats$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/features/dashboard/CommunityStats.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$role$2d$upgrade$2f$UpgradeModal$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/features/role-upgrade/UpgradeModal.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$role$2d$upgrade$2f$UpgradePendingBanner$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/features/role-upgrade/UpgradePendingBanner.tsx [app-client] (ecmascript)");
;
var _s = __turbopack_context__.k.signature();
'use client';
;
;
;
;
;
;
;
;
;
;
;
;
;
;
;
;
// Mock data for widgets
const MOCK_CULTURAL_EVENTS = [
    {
        id: '1',
        name: 'Vesak Day Celebration',
        date: '2025-05-23',
        category: 'religious'
    },
    {
        id: '2',
        name: 'Sri Lankan Independence Day',
        date: '2025-02-04',
        category: 'national'
    },
    {
        id: '3',
        name: 'Sinhala & Tamil New Year',
        date: '2025-04-14',
        category: 'cultural'
    },
    {
        id: '4',
        name: 'Poson Poya Day',
        date: '2025-06-21',
        category: 'holiday'
    }
];
const MOCK_BUSINESSES = [
    {
        id: '1',
        name: 'Ceylon Spice Market',
        category: 'Grocery',
        rating: 4.8,
        reviewCount: 125
    },
    {
        id: '2',
        name: 'Lanka Restaurant',
        category: 'Restaurant',
        rating: 4.6,
        reviewCount: 89
    },
    {
        id: '3',
        name: 'Serendib Boutique',
        category: 'Retail',
        rating: 4.9,
        reviewCount: 203
    }
];
const MOCK_COMMUNITY_STATS = {
    activeUsers: 12500,
    activeUsersTrend: {
        value: '+8.5%',
        direction: 'up'
    },
    recentPosts: 450,
    recentPostsTrend: {
        value: '+12.3%',
        direction: 'up'
    },
    upcomingEvents: 2200,
    upcomingEventsTrend: {
        value: '+5.7%',
        direction: 'up'
    }
};
const MOCK_ACTIVITIES = [
    {
        id: '1',
        type: 'event',
        author: {
            name: 'Priya Perera',
            avatar: 'PP'
        },
        content: 'Organizing a traditional Sri Lankan New Year celebration in Toronto! Join us for games, food, and cultural performances.',
        location: 'Toronto, ON',
        timestamp: '2 hours ago',
        likes: 24,
        comments: 8,
        image: undefined
    },
    {
        id: '2',
        type: 'post',
        author: {
            name: 'Raj Fernando',
            avatar: 'RF'
        },
        content: 'Just discovered an amazing Sri Lankan grocery store in Vancouver! They have fresh hoppers and all the spices you need.',
        location: 'Vancouver, BC',
        timestamp: '4 hours ago',
        likes: 15,
        comments: 5
    },
    {
        id: '3',
        type: 'business',
        author: {
            name: 'Lanka Restaurant',
            avatar: 'LR'
        },
        content: 'New menu items available! Try our authentic kottu roti and string hoppers. Special discount for community members this week.',
        location: 'Montreal, QC',
        timestamp: '6 hours ago',
        likes: 42,
        comments: 12
    }
];
function DashboardPage() {
    _s();
    const router = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRouter"])();
    const { user, clearAuth } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useAuthStore"])();
    const [selectedLocation, setSelectedLocation] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])('All Locations');
    const [showUserMenu, setShowUserMenu] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(false);
    const [mounted, setMounted] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(false);
    const [showUpgradeModal, setShowUpgradeModal] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(false);
    const userMenuRef = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRef"])(null);
    // Set mounted state after client-side hydration
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useEffect"])({
        "DashboardPage.useEffect": ()=>{
            setMounted(true);
        }
    }["DashboardPage.useEffect"], []);
    // Close dropdown when clicking outside
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useEffect"])({
        "DashboardPage.useEffect": ()=>{
            function handleClickOutside(event) {
                if (userMenuRef.current && !userMenuRef.current.contains(event.target)) {
                    setShowUserMenu(false);
                }
            }
            document.addEventListener('mousedown', handleClickOutside);
            return ({
                "DashboardPage.useEffect": ()=>document.removeEventListener('mousedown', handleClickOutside)
            })["DashboardPage.useEffect"];
        }
    }["DashboardPage.useEffect"], []);
    const handleLogout = async ()=>{
        try {
            await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$auth$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["authRepository"].logout();
        } catch (error) {
        // Silently handle logout errors (e.g., 401 when already logged out)
        // The error is expected and clearAuth will handle cleanup
        } finally{
            clearAuth();
            router.push('/login');
        }
    };
    const getInitials = (name)=>{
        return name.split(' ').map((n)=>n[0]).join('').toUpperCase();
    };
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$auth$2f$ProtectedRoute$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["ProtectedRoute"], {
        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "min-h-screen",
            style: {
                background: '#f7fafc'
            },
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("header", {
                    className: "bg-white sticky top-0 z-40",
                    style: {
                        background: 'rgba(255, 255, 255, 0.95)',
                        backdropFilter: 'blur(10px)',
                        boxShadow: '0 2px 20px rgba(0,0,0,0.1)'
                    },
                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4",
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                            className: "flex items-center justify-between",
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "flex items-center gap-8",
                                    children: [
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                            className: "flex items-center",
                                            children: [
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$atoms$2f$Logo$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Logo"], {
                                                    size: "md",
                                                    showText: false
                                                }, void 0, false, {
                                                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                    lineNumber: 208,
                                                    columnNumber: 19
                                                }, this),
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                    className: "ml-3 text-2xl font-bold text-[#8B1538]",
                                                    children: "LankaConnect"
                                                }, void 0, false, {
                                                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                    lineNumber: 209,
                                                    columnNumber: 19
                                                }, this)
                                            ]
                                        }, void 0, true, {
                                            fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                            lineNumber: 207,
                                            columnNumber: 17
                                        }, this),
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("nav", {
                                            className: "hidden md:flex items-center gap-6",
                                            children: [
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
                                                    href: "/",
                                                    className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors",
                                                    children: "Home"
                                                }, void 0, false, {
                                                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                    lineNumber: 214,
                                                    columnNumber: 19
                                                }, this),
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
                                                    href: "#events",
                                                    className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors",
                                                    children: "Events"
                                                }, void 0, false, {
                                                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                    lineNumber: 217,
                                                    columnNumber: 19
                                                }, this),
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
                                                    href: "#forums",
                                                    className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors",
                                                    children: "Forums"
                                                }, void 0, false, {
                                                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                    lineNumber: 220,
                                                    columnNumber: 19
                                                }, this),
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
                                                    href: "#business",
                                                    className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors",
                                                    children: "Business"
                                                }, void 0, false, {
                                                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                    lineNumber: 223,
                                                    columnNumber: 19
                                                }, this),
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
                                                    href: "#culture",
                                                    className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors",
                                                    children: "Culture"
                                                }, void 0, false, {
                                                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                    lineNumber: 226,
                                                    columnNumber: 19
                                                }, this),
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
                                                    href: "/dashboard",
                                                    className: "text-[#FF7900] hover:text-[#FF7900] font-medium transition-colors",
                                                    children: "Dashboard"
                                                }, void 0, false, {
                                                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                    lineNumber: 229,
                                                    columnNumber: 19
                                                }, this)
                                            ]
                                        }, void 0, true, {
                                            fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                            lineNumber: 213,
                                            columnNumber: 17
                                        }, this)
                                    ]
                                }, void 0, true, {
                                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                    lineNumber: 206,
                                    columnNumber: 15
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "flex items-center gap-4",
                                    suppressHydrationWarning: true,
                                    children: mounted && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "relative",
                                        ref: userMenuRef,
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                                onClick: ()=>setShowUserMenu(!showUserMenu),
                                                className: "flex items-center gap-3 hover:bg-gray-50 rounded-lg p-2 transition-colors",
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        className: "w-10 h-10 rounded-full flex items-center justify-center text-white font-semibold",
                                                        style: {
                                                            background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)'
                                                        },
                                                        children: getInitials(user?.fullName || 'U')
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                        lineNumber: 243,
                                                        columnNumber: 23
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        className: "hidden md:block text-left",
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-sm font-medium",
                                                                style: {
                                                                    color: '#8B1538'
                                                                },
                                                                children: user?.fullName
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                lineNumber: 250,
                                                                columnNumber: 25
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-xs",
                                                                style: {
                                                                    color: '#718096'
                                                                },
                                                                children: user?.role
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                lineNumber: 251,
                                                                columnNumber: 25
                                                            }, this)
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                        lineNumber: 249,
                                                        columnNumber: 23
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$down$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronDown$3e$__["ChevronDown"], {
                                                        className: `w-4 h-4 text-gray-600 transition-transform ${showUserMenu ? 'rotate-180' : ''}`
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                        lineNumber: 253,
                                                        columnNumber: 23
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                lineNumber: 239,
                                                columnNumber: 21
                                            }, this),
                                            showUserMenu && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "absolute right-0 mt-2 w-48 rounded-lg shadow-lg overflow-hidden z-50",
                                                style: {
                                                    background: 'white',
                                                    border: '1px solid #e2e8f0'
                                                },
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                                        onClick: ()=>{
                                                            setShowUserMenu(false);
                                                            router.push('/profile');
                                                        },
                                                        className: "w-full flex items-center gap-3 px-4 py-3 hover:bg-gray-50 transition-colors text-left",
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$user$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__User$3e$__["User"], {
                                                                className: "w-4 h-4",
                                                                style: {
                                                                    color: '#FF7900'
                                                                }
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                lineNumber: 272,
                                                                columnNumber: 27
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                                style: {
                                                                    color: '#2d3748'
                                                                },
                                                                children: "Profile"
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                lineNumber: 273,
                                                                columnNumber: 27
                                                            }, this)
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                        lineNumber: 265,
                                                        columnNumber: 25
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        style: {
                                                            borderTop: '1px solid #e2e8f0'
                                                        }
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                        lineNumber: 275,
                                                        columnNumber: 25
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                                        onClick: ()=>{
                                                            setShowUserMenu(false);
                                                            handleLogout();
                                                        },
                                                        className: "w-full flex items-center gap-3 px-4 py-3 hover:bg-gray-50 transition-colors text-left",
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$log$2d$out$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__LogOut$3e$__["LogOut"], {
                                                                className: "w-4 h-4",
                                                                style: {
                                                                    color: '#8B1538'
                                                                }
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                lineNumber: 283,
                                                                columnNumber: 27
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                                style: {
                                                                    color: '#2d3748'
                                                                },
                                                                children: "Logout"
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                lineNumber: 284,
                                                                columnNumber: 27
                                                            }, this)
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                        lineNumber: 276,
                                                        columnNumber: 25
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                lineNumber: 258,
                                                columnNumber: 23
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                        lineNumber: 238,
                                        columnNumber: 19
                                    }, this)
                                }, void 0, false, {
                                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                    lineNumber: 235,
                                    columnNumber: 15
                                }, this)
                            ]
                        }, void 0, true, {
                            fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                            lineNumber: 204,
                            columnNumber: 13
                        }, this)
                    }, void 0, false, {
                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                        lineNumber: 203,
                        columnNumber: 11
                    }, this)
                }, void 0, false, {
                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                    lineNumber: 198,
                    columnNumber: 9
                }, this),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("main", {
                    className: "max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8",
                    children: [
                        user?.pendingUpgradeRole && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$role$2d$upgrade$2f$UpgradePendingBanner$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["UpgradePendingBanner"], {
                            upgradeRequestedAt: user.upgradeRequestedAt ? new Date(user.upgradeRequestedAt) : undefined
                        }, void 0, false, {
                            fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                            lineNumber: 298,
                            columnNumber: 13
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                            className: "mb-8",
                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex flex-col sm:flex-row gap-3",
                                children: [
                                    user && user.role === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["UserRole"].GeneralUser && !user.pendingUpgradeRole && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                        onClick: ()=>setShowUpgradeModal(true),
                                        className: "flex-1 sm:flex-none rounded-lg",
                                        style: {
                                            background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)',
                                            color: 'white',
                                            transition: 'all 0.3s'
                                        },
                                        variant: "default",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$sparkles$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Sparkles$3e$__["Sparkles"], {
                                                className: "w-4 h-4 mr-2"
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                lineNumber: 318,
                                                columnNumber: 19
                                            }, this),
                                            "Upgrade to Event Organizer"
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                        lineNumber: 308,
                                        columnNumber: 17
                                    }, this),
                                    user && (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$utils$2f$role$2d$helpers$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["canCreateEvents"])(user.role) && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                        onClick: ()=>router.push('/events/create'),
                                        className: "flex-1 sm:flex-none rounded-lg",
                                        style: {
                                            background: '#FF7900',
                                            color: 'white',
                                            transition: 'all 0.3s'
                                        },
                                        variant: "default",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$calendar$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Calendar$3e$__["Calendar"], {
                                                className: "w-4 h-4 mr-2"
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                lineNumber: 334,
                                                columnNumber: 19
                                            }, this),
                                            "Create Event"
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                        lineNumber: 324,
                                        columnNumber: 17
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                        onClick: ()=>router.push('/forum/new'),
                                        className: "flex-1 sm:flex-none rounded-lg",
                                        variant: "outline",
                                        style: {
                                            background: 'white',
                                            borderColor: '#FF7900',
                                            color: '#FF7900'
                                        },
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$message$2d$square$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__MessageSquare$3e$__["MessageSquare"], {
                                                className: "w-4 h-4 mr-2"
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                lineNumber: 349,
                                                columnNumber: 17
                                            }, this),
                                            "Post Topic"
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                        lineNumber: 339,
                                        columnNumber: 15
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                lineNumber: 305,
                                columnNumber: 13
                            }, this)
                        }, void 0, false, {
                            fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                            lineNumber: 304,
                            columnNumber: 11
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                            className: "grid grid-cols-1 lg:grid-cols-3 gap-8",
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "lg:col-span-2 space-y-0",
                                    children: [
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                            className: "rounded-xl overflow-hidden",
                                            style: {
                                                background: 'white',
                                                boxShadow: '0 4px 6px rgba(0, 0, 0, 0.05)'
                                            },
                                            children: [
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                    className: "p-6 flex justify-between items-center",
                                                    style: {
                                                        background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)',
                                                        color: 'white'
                                                    },
                                                    children: [
                                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h2", {
                                                            className: "text-xl font-semibold",
                                                            children: "Community Activity"
                                                        }, void 0, false, {
                                                            fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                            lineNumber: 376,
                                                            columnNumber: 19
                                                        }, this),
                                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("select", {
                                                            className: "px-4 py-2 rounded-md text-white text-sm outline-none cursor-pointer",
                                                            style: {
                                                                background: 'rgba(255,255,255,0.2)',
                                                                border: 'none'
                                                            },
                                                            value: selectedLocation,
                                                            onChange: (e)=>setSelectedLocation(e.target.value),
                                                            children: [
                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("option", {
                                                                    style: {
                                                                        color: '#2d3748'
                                                                    },
                                                                    children: "All Locations"
                                                                }, void 0, false, {
                                                                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                    lineNumber: 386,
                                                                    columnNumber: 21
                                                                }, this),
                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("option", {
                                                                    style: {
                                                                        color: '#2d3748'
                                                                    },
                                                                    children: "Toronto, ON"
                                                                }, void 0, false, {
                                                                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                    lineNumber: 387,
                                                                    columnNumber: 21
                                                                }, this),
                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("option", {
                                                                    style: {
                                                                        color: '#2d3748'
                                                                    },
                                                                    children: "Vancouver, BC"
                                                                }, void 0, false, {
                                                                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                    lineNumber: 388,
                                                                    columnNumber: 21
                                                                }, this),
                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("option", {
                                                                    style: {
                                                                        color: '#2d3748'
                                                                    },
                                                                    children: "Montreal, QC"
                                                                }, void 0, false, {
                                                                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                    lineNumber: 389,
                                                                    columnNumber: 21
                                                                }, this)
                                                            ]
                                                        }, void 0, true, {
                                                            fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                            lineNumber: 377,
                                                            columnNumber: 19
                                                        }, this)
                                                    ]
                                                }, void 0, true, {
                                                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                    lineNumber: 369,
                                                    columnNumber: 17
                                                }, this),
                                                MOCK_ACTIVITIES.map((activity, index)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        className: "p-6 transition-colors hover:bg-opacity-100",
                                                        style: {
                                                            borderBottom: index === MOCK_ACTIVITIES.length - 1 ? 'none' : '1px solid #e2e8f0',
                                                            background: 'white'
                                                        },
                                                        onMouseEnter: (e)=>{
                                                            e.currentTarget.style.background = '#f8f9ff';
                                                        },
                                                        onMouseLeave: (e)=>{
                                                            e.currentTarget.style.background = 'white';
                                                        },
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                className: "flex items-center mb-3",
                                                                children: [
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                        className: "w-10 h-10 rounded-full flex items-center justify-center text-white font-bold mr-3",
                                                                        style: {
                                                                            background: 'linear-gradient(135deg, #FF7900, #8B1538)'
                                                                        },
                                                                        children: activity.author.avatar
                                                                    }, void 0, false, {
                                                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                        lineNumber: 411,
                                                                        columnNumber: 23
                                                                    }, this),
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                        className: "flex-1 min-w-0",
                                                                        children: [
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                                className: "font-semibold",
                                                                                style: {
                                                                                    color: '#8B1538'
                                                                                },
                                                                                children: activity.author.name
                                                                            }, void 0, false, {
                                                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                                lineNumber: 418,
                                                                                columnNumber: 25
                                                                            }, this),
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                                className: "text-xs",
                                                                                style: {
                                                                                    color: '#718096'
                                                                                },
                                                                                children: [
                                                                                    activity.timestamp,
                                                                                    " • ",
                                                                                    activity.location
                                                                                ]
                                                                            }, void 0, true, {
                                                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                                lineNumber: 419,
                                                                                columnNumber: 25
                                                                            }, this)
                                                                        ]
                                                                    }, void 0, true, {
                                                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                        lineNumber: 417,
                                                                        columnNumber: 23
                                                                    }, this),
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                                        className: "px-2 py-1 text-xs font-semibold rounded capitalize ml-auto",
                                                                        style: {
                                                                            background: activity.type === 'event' ? '#FFE8CC' : activity.type === 'post' ? '#FFDAB3' : '#D4EDDA',
                                                                            color: activity.type === 'event' ? '#8B1538' : activity.type === 'post' ? '#FF7900' : '#006400'
                                                                        },
                                                                        children: activity.type
                                                                    }, void 0, false, {
                                                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                        lineNumber: 423,
                                                                        columnNumber: 23
                                                                    }, this)
                                                                ]
                                                            }, void 0, true, {
                                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                lineNumber: 410,
                                                                columnNumber: 21
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                className: "mb-4",
                                                                children: [
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
                                                                        className: "font-semibold mb-2",
                                                                        style: {
                                                                            fontSize: '1.1rem',
                                                                            color: '#8B1538'
                                                                        },
                                                                        children: activity.type === 'event' ? 'Join Our Event' : activity.type === 'business' ? 'Special Announcement' : 'Community Discussion'
                                                                    }, void 0, false, {
                                                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                        lineNumber: 438,
                                                                        columnNumber: 23
                                                                    }, this),
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                        style: {
                                                                            color: '#718096',
                                                                            fontSize: '0.9rem',
                                                                            lineHeight: '1.5'
                                                                        },
                                                                        children: activity.content
                                                                    }, void 0, false, {
                                                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                        lineNumber: 442,
                                                                        columnNumber: 23
                                                                    }, this)
                                                                ]
                                                            }, void 0, true, {
                                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                lineNumber: 437,
                                                                columnNumber: 21
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                className: "flex gap-4 pt-4",
                                                                style: {
                                                                    borderTop: '1px solid #f1f5f9'
                                                                },
                                                                children: [
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                                                        className: "flex items-center gap-2 transition-colors text-sm",
                                                                        style: {
                                                                            color: '#718096'
                                                                        },
                                                                        onMouseEnter: (e)=>{
                                                                            e.currentTarget.style.color = '#FF7900';
                                                                        },
                                                                        onMouseLeave: (e)=>{
                                                                            e.currentTarget.style.color = '#718096';
                                                                        },
                                                                        children: [
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$heart$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Heart$3e$__["Heart"], {
                                                                                className: "w-4 h-4"
                                                                            }, void 0, false, {
                                                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                                lineNumber: 462,
                                                                                columnNumber: 25
                                                                            }, this),
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                                                children: activity.likes
                                                                            }, void 0, false, {
                                                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                                lineNumber: 463,
                                                                                columnNumber: 25
                                                                            }, this)
                                                                        ]
                                                                    }, void 0, true, {
                                                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                        lineNumber: 452,
                                                                        columnNumber: 23
                                                                    }, this),
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                                                        className: "flex items-center gap-2 transition-colors text-sm",
                                                                        style: {
                                                                            color: '#718096'
                                                                        },
                                                                        onMouseEnter: (e)=>{
                                                                            e.currentTarget.style.color = '#FF7900';
                                                                        },
                                                                        onMouseLeave: (e)=>{
                                                                            e.currentTarget.style.color = '#718096';
                                                                        },
                                                                        children: [
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$message$2d$circle$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__MessageCircle$3e$__["MessageCircle"], {
                                                                                className: "w-4 h-4"
                                                                            }, void 0, false, {
                                                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                                lineNumber: 475,
                                                                                columnNumber: 25
                                                                            }, this),
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                                                children: activity.comments
                                                                            }, void 0, false, {
                                                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                                lineNumber: 476,
                                                                                columnNumber: 25
                                                                            }, this)
                                                                        ]
                                                                    }, void 0, true, {
                                                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                        lineNumber: 465,
                                                                        columnNumber: 23
                                                                    }, this),
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                                                        className: "flex items-center gap-2 transition-colors text-sm ml-auto",
                                                                        style: {
                                                                            color: '#718096'
                                                                        },
                                                                        onMouseEnter: (e)=>{
                                                                            e.currentTarget.style.color = '#FF7900';
                                                                        },
                                                                        onMouseLeave: (e)=>{
                                                                            e.currentTarget.style.color = '#718096';
                                                                        },
                                                                        children: [
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$share$2d$2$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Share2$3e$__["Share2"], {
                                                                                className: "w-4 h-4"
                                                                            }, void 0, false, {
                                                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                                lineNumber: 488,
                                                                                columnNumber: 25
                                                                            }, this),
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                                                children: "Share"
                                                                            }, void 0, false, {
                                                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                                lineNumber: 489,
                                                                                columnNumber: 25
                                                                            }, this)
                                                                        ]
                                                                    }, void 0, true, {
                                                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                        lineNumber: 478,
                                                                        columnNumber: 23
                                                                    }, this)
                                                                ]
                                                            }, void 0, true, {
                                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                                lineNumber: 448,
                                                                columnNumber: 21
                                                            }, this)
                                                        ]
                                                    }, activity.id, true, {
                                                        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                        lineNumber: 395,
                                                        columnNumber: 19
                                                    }, this))
                                            ]
                                        }, void 0, true, {
                                            fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                            lineNumber: 361,
                                            columnNumber: 15
                                        }, this),
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                            className: "text-center pt-6",
                                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                                variant: "outline",
                                                className: "w-full sm:w-auto",
                                                style: {
                                                    borderColor: '#FF7900',
                                                    color: '#FF7900'
                                                },
                                                onMouseEnter: (e)=>{
                                                    e.currentTarget.style.background = '#FF7900';
                                                    e.currentTarget.style.color = 'white';
                                                },
                                                onMouseLeave: (e)=>{
                                                    e.currentTarget.style.background = 'transparent';
                                                    e.currentTarget.style.color = '#FF7900';
                                                },
                                                children: "Load More Activities"
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                                lineNumber: 498,
                                                columnNumber: 17
                                            }, this)
                                        }, void 0, false, {
                                            fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                            lineNumber: 497,
                                            columnNumber: 15
                                        }, this)
                                    ]
                                }, void 0, true, {
                                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                    lineNumber: 359,
                                    columnNumber: 13
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "space-y-8",
                                    children: [
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$dashboard$2f$CulturalCalendar$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["CulturalCalendar"], {
                                            events: MOCK_CULTURAL_EVENTS
                                        }, void 0, false, {
                                            fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                            lineNumber: 522,
                                            columnNumber: 15
                                        }, this),
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$dashboard$2f$FeaturedBusinesses$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["FeaturedBusinesses"], {
                                            businesses: MOCK_BUSINESSES
                                        }, void 0, false, {
                                            fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                            lineNumber: 525,
                                            columnNumber: 15
                                        }, this),
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$dashboard$2f$CommunityStats$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["CommunityStats"], {
                                            stats: MOCK_COMMUNITY_STATS
                                        }, void 0, false, {
                                            fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                            lineNumber: 528,
                                            columnNumber: 15
                                        }, this)
                                    ]
                                }, void 0, true, {
                                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                                    lineNumber: 520,
                                    columnNumber: 13
                                }, this)
                            ]
                        }, void 0, true, {
                            fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                            lineNumber: 357,
                            columnNumber: 11
                        }, this)
                    ]
                }, void 0, true, {
                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                    lineNumber: 295,
                    columnNumber: 9
                }, this),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Footer$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {}, void 0, false, {
                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                    lineNumber: 534,
                    columnNumber: 9
                }, this),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$role$2d$upgrade$2f$UpgradeModal$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["UpgradeModal"], {
                    isOpen: showUpgradeModal,
                    onClose: ()=>setShowUpgradeModal(false)
                }, void 0, false, {
                    fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
                    lineNumber: 537,
                    columnNumber: 9
                }, this)
            ]
        }, void 0, true, {
            fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
            lineNumber: 196,
            columnNumber: 7
        }, this)
    }, void 0, false, {
        fileName: "[project]/src/app/(dashboard)/dashboard/page.tsx",
        lineNumber: 195,
        columnNumber: 5
    }, this);
}
_s(DashboardPage, "Fiyh3JuTVxa34NXYssqGYFGpCY0=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRouter"],
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useAuthStore"]
    ];
});
_c = DashboardPage;
var _c;
__turbopack_context__.k.register(_c, "DashboardPage");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
]);

//# sourceMappingURL=src_714e76d3._.js.map