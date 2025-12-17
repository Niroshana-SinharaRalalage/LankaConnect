(globalThis.TURBOPACK || (globalThis.TURBOPACK = [])).push([typeof document === "object" ? document.currentScript : undefined,
"[project]/src/presentation/lib/validators/auth.schemas.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "emailSchema",
    ()=>emailSchema,
    "forgotPasswordSchema",
    ()=>forgotPasswordSchema,
    "loginSchema",
    ()=>loginSchema,
    "nameSchema",
    ()=>nameSchema,
    "passwordSchema",
    ()=>passwordSchema,
    "registerSchema",
    ()=>registerSchema,
    "resetPasswordSchema",
    ()=>resetPasswordSchema
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zod$2f$v4$2f$classic$2f$external$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__$2a$__as__z$3e$__ = __turbopack_context__.i("[project]/node_modules/zod/v4/classic/external.js [app-client] (ecmascript) <export * as z>");
;
const emailSchema = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zod$2f$v4$2f$classic$2f$external$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__$2a$__as__z$3e$__["z"].string().min(1, 'Email is required').email('Please enter a valid email address').toLowerCase();
const passwordSchema = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zod$2f$v4$2f$classic$2f$external$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__$2a$__as__z$3e$__["z"].string().min(8, 'Password must be at least 8 characters').regex(/[A-Z]/, 'Password must contain at least one uppercase letter').regex(/[a-z]/, 'Password must contain at least one lowercase letter').regex(/[0-9]/, 'Password must contain at least one number').regex(/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/, 'Password must contain at least one special character');
const nameSchema = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zod$2f$v4$2f$classic$2f$external$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__$2a$__as__z$3e$__["z"].string().min(1, 'This field is required').min(2, 'Must be at least 2 characters').max(50, 'Must be less than 50 characters').regex(/^[a-zA-Z\s'-]+$/, 'Only letters, spaces, hyphens and apostrophes allowed');
const loginSchema = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zod$2f$v4$2f$classic$2f$external$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__$2a$__as__z$3e$__["z"].object({
    email: emailSchema,
    password: __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zod$2f$v4$2f$classic$2f$external$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__$2a$__as__z$3e$__["z"].string().min(1, 'Password is required')
});
const registerSchema = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zod$2f$v4$2f$classic$2f$external$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__$2a$__as__z$3e$__["z"].object({
    email: emailSchema,
    password: passwordSchema,
    confirmPassword: __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zod$2f$v4$2f$classic$2f$external$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__$2a$__as__z$3e$__["z"].string().min(1, 'Please confirm your password'),
    firstName: nameSchema,
    lastName: nameSchema,
    selectedRole: __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zod$2f$v4$2f$classic$2f$external$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__$2a$__as__z$3e$__["z"].enum([
        'GeneralUser',
        'EventOrganizer'
    ]),
    preferredMetroAreaIds: __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zod$2f$v4$2f$classic$2f$external$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__$2a$__as__z$3e$__["z"].array(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zod$2f$v4$2f$classic$2f$external$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__$2a$__as__z$3e$__["z"].string()).min(1, 'Please select at least one metro area').max(20, 'Maximum 20 metro areas allowed'),
    agreeToTerms: __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zod$2f$v4$2f$classic$2f$external$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__$2a$__as__z$3e$__["z"].boolean().refine((val)=>val === true, {
        message: 'You must agree to the terms and conditions'
    }),
    agreeToApproval: __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zod$2f$v4$2f$classic$2f$external$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__$2a$__as__z$3e$__["z"].boolean().optional()
}).refine((data)=>data.password === data.confirmPassword, {
    message: 'Passwords do not match',
    path: [
        'confirmPassword'
    ]
}).refine((data)=>{
    // If Event Organizer selected, approval checkbox must be checked
    if (data.selectedRole === 'EventOrganizer') {
        return data.agreeToApproval === true;
    }
    return true;
}, {
    message: 'You must acknowledge that Event Organizer requests require admin approval',
    path: [
        'agreeToApproval'
    ]
});
const forgotPasswordSchema = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zod$2f$v4$2f$classic$2f$external$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__$2a$__as__z$3e$__["z"].object({
    email: emailSchema
});
const resetPasswordSchema = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zod$2f$v4$2f$classic$2f$external$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__$2a$__as__z$3e$__["z"].object({
    password: passwordSchema,
    confirmPassword: __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zod$2f$v4$2f$classic$2f$external$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__$2a$__as__z$3e$__["z"].string().min(1, 'Please confirm your password')
}).refine((data)=>data.password === data.confirmPassword, {
    message: 'Passwords do not match',
    path: [
        'confirmPassword'
    ]
});
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
   * @param credentials User email and password
   * @param rememberMe Keep user logged in for 30 days (like Facebook/Gmail)
   */ async login(credentials, rememberMe = false) {
        const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/login`, {
            ...credentials,
            rememberMe
        });
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
"[project]/src/presentation/components/features/auth/LoginForm.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "LoginForm",
    ()=>LoginForm
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$react$2d$hook$2d$form$2f$dist$2f$index$2e$esm$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/react-hook-form/dist/index.esm.mjs [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$hookform$2f$resolvers$2f$zod$2f$dist$2f$zod$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@hookform/resolvers/zod/dist/zod.mjs [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/navigation.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/client/app-dir/link.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$eye$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Eye$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/eye.js [app-client] (ecmascript) <export default as Eye>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$eye$2d$off$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__EyeOff$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/eye-off.js [app-client] (ecmascript) <export default as EyeOff>");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$validators$2f$auth$2e$schemas$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/validators/auth.schemas.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$auth$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/repositories/auth.repository.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/store/useAuthStore.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/client/api-errors.ts [app-client] (ecmascript)");
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
function LoginForm() {
    _s();
    const router = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRouter"])();
    const { setAuth } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useAuthStore"])();
    const [apiError, setApiError] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(null);
    const [showPassword, setShowPassword] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(false);
    const [rememberMe, setRememberMe] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(false);
    const { register, handleSubmit, formState: { errors, isSubmitting } } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$react$2d$hook$2d$form$2f$dist$2f$index$2e$esm$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useForm"])({
        resolver: (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$hookform$2f$resolvers$2f$zod$2f$dist$2f$zod$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__["zodResolver"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$validators$2f$auth$2e$schemas$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["loginSchema"])
    });
    const onSubmit = async (data)=>{
        try {
            setApiError(null);
            // Phase AUTH-IMPROVEMENT: Pass rememberMe to backend for extended sessions
            const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$auth$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["authRepository"].login(data, rememberMe);
            // Set auth state
            // Phase 6A.10: In development (localStorage mode), backend sends refreshToken in response body
            // In production (cookie mode), refreshToken is in HttpOnly cookie
            const tokens = {
                accessToken: response.accessToken,
                refreshToken: response.refreshToken || '',
                expiresIn: 3600
            };
            setAuth(response.user, tokens);
            // Phase 6A.8: Redirect to landing page after login
            // Users can access dashboard via menu or profile settings
            router.push('/');
        } catch (error) {
            if (error instanceof __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$errors$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["ApiError"]) {
                setApiError(error.message);
            } else {
                setApiError('An unexpected error occurred. Please try again.');
            }
        }
    };
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        className: "w-full",
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "mb-10",
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h2", {
                        className: "text-[2rem] font-semibold mb-2",
                        style: {
                            color: '#8B1538'
                        },
                        children: "Sign In"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                        lineNumber: 68,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                        className: "text-base",
                        style: {
                            color: '#666'
                        },
                        children: "Enter your credentials to access your account"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                        lineNumber: 69,
                        columnNumber: 9
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                lineNumber: 67,
                columnNumber: 7
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("form", {
                onSubmit: handleSubmit(onSubmit),
                children: [
                    apiError && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "mb-6 p-3 text-sm text-destructive bg-destructive/10 border border-destructive rounded-[10px]",
                        children: apiError
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                        lineNumber: 76,
                        columnNumber: 11
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "mb-[25px]",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                htmlFor: "email",
                                className: "block mb-2 font-medium text-[0.95rem]",
                                style: {
                                    color: '#333'
                                },
                                children: "Email Address"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                lineNumber: 83,
                                columnNumber: 11
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
                                id: "email",
                                type: "email",
                                placeholder: "you@example.com",
                                className: `w-full px-4 py-[15px] border-2 rounded-[10px] text-base transition-all duration-300 focus:outline-none ${errors.email ? 'border-destructive' : 'border-[#e0e0e0]'}`,
                                onFocus: (e)=>{
                                    if (!errors.email) {
                                        e.target.style.borderColor = '#FF7900';
                                        e.target.style.boxShadow = '0 0 0 3px rgba(255, 121, 0, 0.1)';
                                    }
                                },
                                ...register('email', {
                                    onBlur: (e)=>{
                                        if (!errors.email) {
                                            e.target.style.borderColor = '#e0e0e0';
                                            e.target.style.boxShadow = 'none';
                                        }
                                    }
                                })
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                lineNumber: 86,
                                columnNumber: 11
                            }, this),
                            errors.email && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                className: "mt-2 text-sm text-destructive",
                                children: errors.email.message
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                lineNumber: 109,
                                columnNumber: 13
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                        lineNumber: 82,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "mb-[25px]",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                htmlFor: "password",
                                className: "block mb-2 font-medium text-[0.95rem]",
                                style: {
                                    color: '#333'
                                },
                                children: "Password"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                lineNumber: 115,
                                columnNumber: 11
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "relative",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
                                        id: "password",
                                        type: showPassword ? 'text' : 'password',
                                        placeholder: "Enter your password",
                                        className: `w-full px-4 py-[15px] pr-12 border-2 rounded-[10px] text-base transition-all duration-300 focus:outline-none ${errors.password ? 'border-destructive' : 'border-[#e0e0e0]'}`,
                                        onFocus: (e)=>{
                                            if (!errors.password) {
                                                e.target.style.borderColor = '#FF7900';
                                                e.target.style.boxShadow = '0 0 0 3px rgba(255, 121, 0, 0.1)';
                                            }
                                        },
                                        ...register('password', {
                                            onBlur: (e)=>{
                                                if (!errors.password) {
                                                    e.target.style.borderColor = '#e0e0e0';
                                                    e.target.style.boxShadow = 'none';
                                                }
                                            }
                                        })
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                        lineNumber: 119,
                                        columnNumber: 13
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                        type: "button",
                                        onClick: ()=>setShowPassword(!showPassword),
                                        className: "absolute right-4 top-1/2 -translate-y-1/2 transition-colors",
                                        style: {
                                            color: '#666'
                                        },
                                        onMouseEnter: (e)=>e.currentTarget.style.color = '#333',
                                        onMouseLeave: (e)=>e.currentTarget.style.color = '#666',
                                        children: showPassword ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$eye$2d$off$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__EyeOff$3e$__["EyeOff"], {
                                            className: "w-5 h-5"
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                            lineNumber: 149,
                                            columnNumber: 31
                                        }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$eye$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Eye$3e$__["Eye"], {
                                            className: "w-5 h-5"
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                            lineNumber: 149,
                                            columnNumber: 64
                                        }, this)
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                        lineNumber: 141,
                                        columnNumber: 13
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                lineNumber: 118,
                                columnNumber: 11
                            }, this),
                            errors.password && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                className: "mt-2 text-sm text-destructive",
                                children: errors.password.message
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                lineNumber: 153,
                                columnNumber: 13
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                        lineNumber: 114,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "flex items-center justify-between mb-[30px]",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                className: "flex items-center cursor-pointer",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
                                        type: "checkbox",
                                        checked: rememberMe,
                                        onChange: (e)=>setRememberMe(e.target.checked),
                                        className: "w-[18px] h-[18px] mr-2 cursor-pointer",
                                        style: {
                                            accentColor: '#FF7900'
                                        }
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                        lineNumber: 160,
                                        columnNumber: 13
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                        className: "text-sm",
                                        style: {
                                            color: '#333'
                                        },
                                        children: "Remember me"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                        lineNumber: 167,
                                        columnNumber: 13
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                lineNumber: 159,
                                columnNumber: 11
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
                                href: "/forgot-password",
                                className: "text-sm font-medium transition-colors",
                                style: {
                                    color: '#FF7900'
                                },
                                onMouseEnter: (e)=>e.currentTarget.style.color = '#8B1538',
                                onMouseLeave: (e)=>e.currentTarget.style.color = '#FF7900',
                                children: "Forgot Password?"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                lineNumber: 169,
                                columnNumber: 11
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                        lineNumber: 158,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                        type: "submit",
                        disabled: isSubmitting,
                        className: "w-full py-4 text-white border-none rounded-[10px] text-[1.1rem] font-semibold cursor-pointer transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed",
                        style: {
                            background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)',
                            boxShadow: '0 4px 15px rgba(255, 121, 0, 0.3)'
                        },
                        onMouseEnter: (e)=>{
                            if (!isSubmitting) {
                                e.currentTarget.style.transform = 'translateY(-2px)';
                                e.currentTarget.style.boxShadow = '0 6px 25px rgba(255, 121, 0, 0.4)';
                            }
                        },
                        onMouseLeave: (e)=>{
                            if (!isSubmitting) {
                                e.currentTarget.style.transform = 'translateY(0)';
                                e.currentTarget.style.boxShadow = '0 4px 15px rgba(255, 121, 0, 0.3)';
                            }
                        },
                        children: isSubmitting ? 'Signing In...' : 'Sign In'
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                        lineNumber: 181,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "flex items-center my-[30px]",
                        style: {
                            color: '#999'
                        },
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex-1 h-px",
                                style: {
                                    background: '#e0e0e0'
                                }
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                lineNumber: 207,
                                columnNumber: 11
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                className: "px-4",
                                children: "OR"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                lineNumber: 208,
                                columnNumber: 11
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex-1 h-px",
                                style: {
                                    background: '#e0e0e0'
                                }
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                lineNumber: 209,
                                columnNumber: 11
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                        lineNumber: 206,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                        type: "button",
                        className: "w-full py-[14px] bg-white border-2 rounded-[10px] text-base font-medium cursor-pointer transition-all duration-300 flex items-center justify-center gap-2.5 mb-[15px]",
                        style: {
                            borderColor: '#e0e0e0'
                        },
                        onMouseEnter: (e)=>{
                            e.currentTarget.style.borderColor = '#FF7900';
                            e.currentTarget.style.background = 'rgba(255, 121, 0, 0.05)';
                        },
                        onMouseLeave: (e)=>{
                            e.currentTarget.style.borderColor = '#e0e0e0';
                            e.currentTarget.style.background = 'white';
                        },
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("svg", {
                                className: "w-6 h-6",
                                viewBox: "0 0 23 23",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("path", {
                                        fill: "#f3f3f3",
                                        d: "M0 0h23v23H0z"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                        lineNumber: 227,
                                        columnNumber: 13
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("path", {
                                        fill: "#f35325",
                                        d: "M1 1h10v10H1z"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                        lineNumber: 228,
                                        columnNumber: 13
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("path", {
                                        fill: "#81bc06",
                                        d: "M12 1h10v10H12z"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                        lineNumber: 229,
                                        columnNumber: 13
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("path", {
                                        fill: "#05a6f0",
                                        d: "M1 12h10v10H1z"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                        lineNumber: 230,
                                        columnNumber: 13
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("path", {
                                        fill: "#ffba08",
                                        d: "M12 12h10v10H12z"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                        lineNumber: 231,
                                        columnNumber: 13
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                                lineNumber: 226,
                                columnNumber: 11
                            }, this),
                            "Continue with Microsoft"
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                        lineNumber: 213,
                        columnNumber: 9
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                lineNumber: 73,
                columnNumber: 7
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "text-center mt-[30px]",
                style: {
                    color: '#666'
                },
                children: [
                    "Don't have an account?",
                    ' ',
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
                        href: "/register",
                        className: "font-semibold transition-colors",
                        style: {
                            color: '#FF7900'
                        },
                        onMouseEnter: (e)=>e.currentTarget.style.color = '#8B1538',
                        onMouseLeave: (e)=>e.currentTarget.style.color = '#FF7900',
                        children: "Join Community"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                        lineNumber: 240,
                        columnNumber: 9
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
                lineNumber: 238,
                columnNumber: 7
            }, this)
        ]
    }, void 0, true, {
        fileName: "[project]/src/presentation/components/features/auth/LoginForm.tsx",
        lineNumber: 65,
        columnNumber: 5
    }, this);
}
_s(LoginForm, "nzBhbqQWTqaDrzyjyt1m3/39WMo=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRouter"],
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useAuthStore"],
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$react$2d$hook$2d$form$2f$dist$2f$index$2e$esm$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useForm"]
    ];
});
_c = LoginForm;
var _c;
__turbopack_context__.k.register(_c, "LoginForm");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
]);

//# sourceMappingURL=src_64782f8c._.js.map