(globalThis.TURBOPACK || (globalThis.TURBOPACK = [])).push([typeof document === "object" ? document.currentScript : undefined,
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
"[project]/src/presentation/components/atoms/OfficialLogo.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "OfficialLogo",
    ()=>OfficialLogo
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/client/app-dir/link.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$atoms$2f$Logo$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/atoms/Logo.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-client] (ecmascript)");
;
;
;
;
function OfficialLogo({ size = 'md', textColor = 'text-[#8B1538]', subtitleColor = 'text-gray-600', linkTo = '/', className }) {
    const sizeConfig = {
        sm: {
            logoSize: 'sm',
            titleSize: 'text-lg',
            subtitleSize: 'text-[10px]',
            gap: 'ml-2'
        },
        md: {
            logoSize: 'md',
            titleSize: 'text-2xl',
            subtitleSize: 'text-xs',
            gap: 'ml-3'
        },
        lg: {
            logoSize: 'lg',
            titleSize: 'text-3xl',
            subtitleSize: 'text-sm',
            gap: 'ml-4'
        }
    };
    const config = sizeConfig[size];
    const logoContent = /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('flex items-center', className),
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$atoms$2f$Logo$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Logo"], {
                size: config.logoSize,
                showText: false
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/atoms/OfficialLogo.tsx",
                lineNumber: 50,
                columnNumber: 7
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: config.gap,
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])(config.titleSize, textColor),
                        children: "LankaConnect"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/atoms/OfficialLogo.tsx",
                        lineNumber: 52,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])(config.subtitleSize, subtitleColor, '-mt-1'),
                        children: "Sri Lankan Community Hub"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/atoms/OfficialLogo.tsx",
                        lineNumber: 53,
                        columnNumber: 9
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/atoms/OfficialLogo.tsx",
                lineNumber: 51,
                columnNumber: 7
            }, this)
        ]
    }, void 0, true, {
        fileName: "[project]/src/presentation/components/atoms/OfficialLogo.tsx",
        lineNumber: 49,
        columnNumber: 5
    }, this);
    if (linkTo) {
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
            href: linkTo,
            className: "hover:opacity-90 transition-opacity",
            children: logoContent
        }, void 0, false, {
            fileName: "[project]/src/presentation/components/atoms/OfficialLogo.tsx",
            lineNumber: 62,
            columnNumber: 7
        }, this);
    }
    return logoContent;
}
_c = OfficialLogo;
var _c;
__turbopack_context__.k.register(_c, "OfficialLogo");
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
"[project]/src/presentation/components/features/notifications/NotificationBell.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "NotificationBell",
    ()=>NotificationBell
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-client] (ecmascript)");
'use client';
;
;
function NotificationBell({ unreadCount, onClick, className = '' }) {
    const displayCount = unreadCount > 99 ? '99+' : unreadCount.toString();
    const hasUnread = unreadCount > 0;
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
        type: "button",
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('relative flex items-center justify-center w-10 h-10 rounded-full', 'hover:bg-gray-100 transition-all duration-200', 'focus:outline-none focus:ring-2 focus:ring-[#8B1538] focus:ring-offset-2', className),
        onClick: onClick,
        "aria-label": `Notifications${hasUnread ? ` (${unreadCount} unread)` : ''}`,
        title: hasUnread ? `${unreadCount} unread notifications` : 'Notifications',
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("svg", {
                className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('w-6 h-6 text-[#333]', hasUnread && 'animate-[bell-ring_1s_ease-in-out]'),
                fill: "none",
                stroke: "currentColor",
                viewBox: "0 0 24 24",
                xmlns: "http://www.w3.org/2000/svg",
                "aria-hidden": "true",
                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("path", {
                    strokeLinecap: "round",
                    strokeLinejoin: "round",
                    strokeWidth: 2,
                    d: "M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/features/notifications/NotificationBell.tsx",
                    lineNumber: 66,
                    columnNumber: 9
                }, this)
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/features/notifications/NotificationBell.tsx",
                lineNumber: 55,
                columnNumber: 7
            }, this),
            hasUnread && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('absolute top-0 right-0', 'flex items-center justify-center', 'min-w-[20px] h-5 px-1', 'bg-[#FF7900] text-white text-xs font-bold rounded-full', 'border-2 border-white', 'animate-[badge-pop_0.3s_ease-out]'),
                "aria-label": `${unreadCount} unread notifications`,
                children: displayCount
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/features/notifications/NotificationBell.tsx",
                lineNumber: 76,
                columnNumber: 9
            }, this)
        ]
    }, void 0, true, {
        fileName: "[project]/src/presentation/components/features/notifications/NotificationBell.tsx",
        lineNumber: 42,
        columnNumber: 5
    }, this);
} /**
 * Add these animations to your global CSS or tailwind.config.ts:
 *
 * @keyframes bell-ring {
 *   0%, 100% { transform: rotate(0deg); }
 *   10%, 30% { transform: rotate(-10deg); }
 *   20%, 40% { transform: rotate(10deg); }
 * }
 *
 * @keyframes badge-pop {
 *   0% { transform: scale(0); }
 *   50% { transform: scale(1.1); }
 *   100% { transform: scale(1); }
 * }
 */ 
_c = NotificationBell;
var _c;
__turbopack_context__.k.register(_c, "NotificationBell");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/infrastructure/api/types/notifications.types.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

/**
 * Notification API Types
 * Phase 6A.6: Notification System
 */ __turbopack_context__.s([
    "notificationTypeConfig",
    ()=>notificationTypeConfig
]);
const notificationTypeConfig = {
    RoleUpgradeApproved: {
        icon: '✓',
        color: 'text-green-600',
        bgColor: 'bg-green-50'
    },
    RoleUpgradeRejected: {
        icon: '✗',
        color: 'text-red-600',
        bgColor: 'bg-red-50'
    },
    FreeTrialExpiring: {
        icon: '⏰',
        color: 'text-orange-600',
        bgColor: 'bg-orange-50'
    },
    FreeTrialExpired: {
        icon: '⏱',
        color: 'text-red-600',
        bgColor: 'bg-red-50'
    },
    SubscriptionPaymentSucceeded: {
        icon: '✓',
        color: 'text-green-600',
        bgColor: 'bg-green-50'
    },
    SubscriptionPaymentFailed: {
        icon: '!',
        color: 'text-red-600',
        bgColor: 'bg-red-50'
    },
    System: {
        icon: 'ℹ',
        color: 'text-blue-600',
        bgColor: 'bg-blue-50'
    },
    Event: {
        icon: '📅',
        color: 'text-purple-600',
        bgColor: 'bg-purple-50'
    }
};
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/infrastructure/api/repositories/notifications.repository.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "NotificationsRepository",
    ()=>NotificationsRepository,
    "notificationsRepository",
    ()=>notificationsRepository
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/client/api-client.ts [app-client] (ecmascript)");
;
class NotificationsRepository {
    basePath = '/notifications';
    /**
   * Get unread notifications for the current user
   */ async getUnreadNotifications() {
        const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/unread`);
        return response;
    }
    /**
   * Mark a notification as read
   */ async markAsRead(notificationId) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${notificationId}/read`);
    }
    /**
   * Mark all notifications as read
   */ async markAllAsRead() {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/read-all`);
    }
}
const notificationsRepository = new NotificationsRepository();
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/hooks/useNotifications.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

/**
 * Notifications React Query Hooks
 * Phase 6A.6: Notification System
 *
 * Provides React Query hooks for Notifications API integration
 * Implements caching, optimistic updates, and proper error handling
 *
 * @requires @tanstack/react-query
 * @requires notificationsRepository from infrastructure/repositories/notifications.repository
 * @requires Notification types from infrastructure/api/types/notifications.types
 */ __turbopack_context__.s([
    "default",
    ()=>__TURBOPACK__default__export__,
    "notificationKeys",
    ()=>notificationKeys,
    "useInvalidateNotifications",
    ()=>useInvalidateNotifications,
    "useMarkAllNotificationsAsRead",
    ()=>useMarkAllNotificationsAsRead,
    "useMarkNotificationAsRead",
    ()=>useMarkNotificationAsRead,
    "useUnreadNotifications",
    ()=>useUnreadNotifications
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/useQuery.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/useMutation.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/QueryClientProvider.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$notifications$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/repositories/notifications.repository.ts [app-client] (ecmascript)");
var _s = __turbopack_context__.k.signature(), _s1 = __turbopack_context__.k.signature(), _s2 = __turbopack_context__.k.signature(), _s3 = __turbopack_context__.k.signature();
;
;
const notificationKeys = {
    all: [
        'notifications'
    ],
    unread: ()=>[
            ...notificationKeys.all,
            'unread'
        ]
};
function useUnreadNotifications(options) {
    _s();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQuery"])({
        queryKey: notificationKeys.unread(),
        queryFn: {
            "useUnreadNotifications.useQuery": async ()=>{
                const result = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$notifications$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["notificationsRepository"].getUnreadNotifications();
                return result;
            }
        }["useUnreadNotifications.useQuery"],
        staleTime: 1 * 60 * 1000,
        refetchInterval: 30 * 1000,
        refetchOnWindowFocus: true,
        retry: 1,
        ...options
    });
}
_s(useUnreadNotifications, "4ZpngI1uv+Uo3WQHEZmTQ5FNM+k=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQuery"]
    ];
});
function useMarkNotificationAsRead() {
    _s1();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: {
            "useMarkNotificationAsRead.useMutation": (notificationId)=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$notifications$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["notificationsRepository"].markAsRead(notificationId)
        }["useMarkNotificationAsRead.useMutation"],
        onMutate: {
            "useMarkNotificationAsRead.useMutation": async (notificationId)=>{
                // Cancel outgoing refetches
                await queryClient.cancelQueries({
                    queryKey: notificationKeys.unread()
                });
                // Snapshot previous value for rollback
                const previousNotifications = queryClient.getQueryData(notificationKeys.unread());
                // Optimistically remove the notification from unread list
                queryClient.setQueryData(notificationKeys.unread(), {
                    "useMarkNotificationAsRead.useMutation": (old)=>old?.filter({
                            "useMarkNotificationAsRead.useMutation": (n)=>n.id !== notificationId
                        }["useMarkNotificationAsRead.useMutation"]) || []
                }["useMarkNotificationAsRead.useMutation"]);
                return {
                    previousNotifications
                };
            }
        }["useMarkNotificationAsRead.useMutation"],
        onError: {
            "useMarkNotificationAsRead.useMutation": (_err, _variables, context)=>{
                // Rollback on error
                if (context?.previousNotifications) {
                    queryClient.setQueryData(notificationKeys.unread(), context.previousNotifications);
                }
            }
        }["useMarkNotificationAsRead.useMutation"],
        onSuccess: {
            "useMarkNotificationAsRead.useMutation": ()=>{
                // Invalidate to ensure consistency with server
                queryClient.invalidateQueries({
                    queryKey: notificationKeys.unread()
                });
            }
        }["useMarkNotificationAsRead.useMutation"]
    });
}
_s1(useMarkNotificationAsRead, "YK0wzM21ECnncaq5SECwU+/SVdQ=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"],
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"]
    ];
});
function useMarkAllNotificationsAsRead() {
    _s2();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: {
            "useMarkAllNotificationsAsRead.useMutation": ()=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$notifications$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["notificationsRepository"].markAllAsRead()
        }["useMarkAllNotificationsAsRead.useMutation"],
        onMutate: {
            "useMarkAllNotificationsAsRead.useMutation": async ()=>{
                // Cancel outgoing refetches
                await queryClient.cancelQueries({
                    queryKey: notificationKeys.unread()
                });
                // Snapshot previous value for rollback
                const previousNotifications = queryClient.getQueryData(notificationKeys.unread());
                // Optimistically clear all notifications
                queryClient.setQueryData(notificationKeys.unread(), []);
                return {
                    previousNotifications
                };
            }
        }["useMarkAllNotificationsAsRead.useMutation"],
        onError: {
            "useMarkAllNotificationsAsRead.useMutation": (_err, _variables, context)=>{
                // Rollback on error
                if (context?.previousNotifications) {
                    queryClient.setQueryData(notificationKeys.unread(), context.previousNotifications);
                }
            }
        }["useMarkAllNotificationsAsRead.useMutation"],
        onSuccess: {
            "useMarkAllNotificationsAsRead.useMutation": ()=>{
                // Invalidate to ensure consistency with server
                queryClient.invalidateQueries({
                    queryKey: notificationKeys.unread()
                });
            }
        }["useMarkAllNotificationsAsRead.useMutation"]
    });
}
_s2(useMarkAllNotificationsAsRead, "YK0wzM21ECnncaq5SECwU+/SVdQ=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"],
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"]
    ];
});
function useInvalidateNotifications() {
    _s3();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return {
        all: ()=>queryClient.invalidateQueries({
                queryKey: notificationKeys.all
            }),
        unread: ()=>queryClient.invalidateQueries({
                queryKey: notificationKeys.unread()
            })
    };
}
_s3(useInvalidateNotifications, "4R+oYVB2Uc11P7bp1KcuhpkfaTw=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"]
    ];
});
const __TURBOPACK__default__export__ = {
    useUnreadNotifications,
    useMarkNotificationAsRead,
    useMarkAllNotificationsAsRead,
    useInvalidateNotifications
};
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "NotificationDropdown",
    ()=>NotificationDropdown
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/client/app-dir/link.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$notifications$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/types/notifications.types.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useNotifications$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/hooks/useNotifications.ts [app-client] (ecmascript)");
;
var _s = __turbopack_context__.k.signature();
'use client';
;
;
;
;
;
function NotificationDropdown({ notifications, isOpen, onClose, className = '' }) {
    _s();
    const dropdownRef = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRef"](null);
    const markAsRead = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useNotifications$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMarkNotificationAsRead"])();
    const markAllAsRead = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useNotifications$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMarkAllNotificationsAsRead"])();
    // Close dropdown on outside click
    __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useEffect"]({
        "NotificationDropdown.useEffect": ()=>{
            if (!isOpen) return;
            const handleClickOutside = {
                "NotificationDropdown.useEffect.handleClickOutside": (event)=>{
                    if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
                        onClose();
                    }
                }
            }["NotificationDropdown.useEffect.handleClickOutside"];
            document.addEventListener('mousedown', handleClickOutside);
            return ({
                "NotificationDropdown.useEffect": ()=>document.removeEventListener('mousedown', handleClickOutside)
            })["NotificationDropdown.useEffect"];
        }
    }["NotificationDropdown.useEffect"], [
        isOpen,
        onClose
    ]);
    // Close on Escape key
    __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useEffect"]({
        "NotificationDropdown.useEffect": ()=>{
            if (!isOpen) return;
            const handleEscape = {
                "NotificationDropdown.useEffect.handleEscape": (event)=>{
                    if (event.key === 'Escape') {
                        onClose();
                    }
                }
            }["NotificationDropdown.useEffect.handleEscape"];
            document.addEventListener('keydown', handleEscape);
            return ({
                "NotificationDropdown.useEffect": ()=>document.removeEventListener('keydown', handleEscape)
            })["NotificationDropdown.useEffect"];
        }
    }["NotificationDropdown.useEffect"], [
        isOpen,
        onClose
    ]);
    const handleNotificationClick = async (notificationId)=>{
        try {
            await markAsRead.mutateAsync(notificationId);
        } catch (error) {
            console.error('Failed to mark notification as read:', error);
        }
    };
    const handleMarkAllAsRead = async ()=>{
        try {
            await markAllAsRead.mutateAsync();
            onClose();
        } catch (error) {
            console.error('Failed to mark all notifications as read:', error);
        }
    };
    const formatTimeAgo = (dateString)=>{
        const date = new Date(dateString);
        const now = new Date();
        const diffMs = now.getTime() - date.getTime();
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMins / 60);
        const diffDays = Math.floor(diffHours / 24);
        if (diffMins < 1) return 'Just now';
        if (diffMins < 60) return `${diffMins}m ago`;
        if (diffHours < 24) return `${diffHours}h ago`;
        if (diffDays < 7) return `${diffDays}d ago`;
        return date.toLocaleDateString();
    };
    if (!isOpen) return null;
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        ref: dropdownRef,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('absolute right-0 mt-2 w-80 sm:w-96', 'bg-white rounded-lg shadow-lg', 'border border-gray-200', 'z-50', 'animate-[dropdown-fade-in_0.2s_ease-out]', className),
        role: "menu",
        "aria-label": "Notifications menu",
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "flex items-center justify-between px-4 py-3 border-b border-gray-200",
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
                        className: "text-lg font-semibold text-[#8B1538]",
                        children: "Notifications"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                        lineNumber: 138,
                        columnNumber: 9
                    }, this),
                    notifications.length > 0 && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                        type: "button",
                        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('text-sm text-[#FF7900] font-medium', 'hover:text-[#E66D00] transition-colors', 'focus:outline-none focus:underline'),
                        onClick: handleMarkAllAsRead,
                        disabled: markAllAsRead.isPending,
                        children: markAllAsRead.isPending ? 'Marking...' : 'Mark all as read'
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                        lineNumber: 140,
                        columnNumber: 11
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                lineNumber: 137,
                columnNumber: 7
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "max-h-96 overflow-y-auto",
                children: notifications.length === 0 ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "px-4 py-8 text-center text-gray-500",
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("svg", {
                            className: "w-12 h-12 mx-auto mb-3 text-gray-400",
                            fill: "none",
                            stroke: "currentColor",
                            viewBox: "0 0 24 24",
                            xmlns: "http://www.w3.org/2000/svg",
                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("path", {
                                strokeLinecap: "round",
                                strokeLinejoin: "round",
                                strokeWidth: 2,
                                d: "M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                                lineNumber: 166,
                                columnNumber: 15
                            }, this)
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                            lineNumber: 159,
                            columnNumber: 13
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                            className: "font-medium",
                            children: "No new notifications"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                            lineNumber: 173,
                            columnNumber: 13
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                            className: "text-sm mt-1",
                            children: "You're all caught up!"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                            lineNumber: 174,
                            columnNumber: 13
                        }, this)
                    ]
                }, void 0, true, {
                    fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                    lineNumber: 158,
                    columnNumber: 11
                }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("ul", {
                    className: "divide-y divide-gray-100",
                    children: notifications.map((notification)=>{
                        const config = __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$notifications$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["notificationTypeConfig"][notification.type];
                        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("li", {
                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                type: "button",
                                className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('w-full px-4 py-3 text-left', 'hover:bg-gray-50 transition-colors', 'focus:outline-none focus:bg-gray-50'),
                                onClick: ()=>handleNotificationClick(notification.id),
                                disabled: markAsRead.isPending,
                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "flex gap-3",
                                    children: [
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                            className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('flex-shrink-0 w-10 h-10 rounded-full', 'flex items-center justify-center text-lg', config.bgColor, config.color),
                                            children: config.icon
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                                            lineNumber: 194,
                                            columnNumber: 23
                                        }, this),
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                            className: "flex-1 min-w-0",
                                            children: [
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                    className: "text-sm font-semibold text-[#333] truncate",
                                                    children: notification.title
                                                }, void 0, false, {
                                                    fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                                                    lineNumber: 207,
                                                    columnNumber: 25
                                                }, this),
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                    className: "text-sm text-gray-600 mt-1 line-clamp-2",
                                                    children: notification.message
                                                }, void 0, false, {
                                                    fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                                                    lineNumber: 210,
                                                    columnNumber: 25
                                                }, this),
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                    className: "text-xs text-gray-500 mt-1",
                                                    children: formatTimeAgo(notification.createdAt)
                                                }, void 0, false, {
                                                    fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                                                    lineNumber: 213,
                                                    columnNumber: 25
                                                }, this)
                                            ]
                                        }, void 0, true, {
                                            fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                                            lineNumber: 206,
                                            columnNumber: 23
                                        }, this),
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                            className: "flex-shrink-0",
                                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                className: "inline-block w-2 h-2 rounded-full bg-[#FF7900]",
                                                "aria-label": "Unread"
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                                                lineNumber: 220,
                                                columnNumber: 25
                                            }, this)
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                                            lineNumber: 219,
                                            columnNumber: 23
                                        }, this)
                                    ]
                                }, void 0, true, {
                                    fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                                    lineNumber: 192,
                                    columnNumber: 21
                                }, this)
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                                lineNumber: 182,
                                columnNumber: 19
                            }, this)
                        }, notification.id, false, {
                            fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                            lineNumber: 181,
                            columnNumber: 17
                        }, this);
                    })
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                    lineNumber: 177,
                    columnNumber: 11
                }, this)
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                lineNumber: 156,
                columnNumber: 7
            }, this),
            notifications.length > 0 && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "px-4 py-3 border-t border-gray-200",
                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
                    href: "/notifications",
                    className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('block w-full text-center text-sm font-medium', 'text-[#8B1538] hover:text-[#70112D]', 'py-2 rounded-md hover:bg-gray-50', 'transition-colors'),
                    onClick: onClose,
                    children: "View all notifications"
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                    lineNumber: 237,
                    columnNumber: 11
                }, this)
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                lineNumber: 236,
                columnNumber: 9
            }, this)
        ]
    }, void 0, true, {
        fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
        lineNumber: 123,
        columnNumber: 5
    }, this);
} /**
 * Add this animation to your global CSS or tailwind.config.ts:
 *
 * @keyframes dropdown-fade-in {
 *   from {
 *     opacity: 0;
 *     transform: translateY(-10px);
 *   }
 *   to {
 *     opacity: 1;
 *     transform: translateY(0);
 *   }
 * }
 */ 
_s(NotificationDropdown, "M59dhhELLBUsBOKMB2cXM+Qh88k=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useNotifications$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMarkNotificationAsRead"],
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useNotifications$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMarkAllNotificationsAsRead"]
    ];
});
_c = NotificationDropdown;
var _c;
__turbopack_context__.k.register(_c, "NotificationDropdown");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/layout/Header.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "Header",
    ()=>Header
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/client/app-dir/link.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$image$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/image.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/navigation.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$atoms$2f$OfficialLogo$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/atoms/OfficialLogo.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Button.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/store/useAuthStore.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$notifications$2f$NotificationBell$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/features/notifications/NotificationBell.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$notifications$2f$NotificationDropdown$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useNotifications$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/hooks/useNotifications.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$user$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__User$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/user.js [app-client] (ecmascript) <export default as User>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$log$2d$out$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__LogOut$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/log-out.js [app-client] (ecmascript) <export default as LogOut>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$down$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronDown$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/chevron-down.js [app-client] (ecmascript) <export default as ChevronDown>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$search$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Search$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/search.js [app-client] (ecmascript) <export default as Search>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$menu$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Menu$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/menu.js [app-client] (ecmascript) <export default as Menu>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$x$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__X$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/x.js [app-client] (ecmascript) <export default as X>");
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
function Header({ className = '' }) {
    _s();
    const { user, isAuthenticated, clearAuth } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useAuthStore"])();
    const router = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRouter"])();
    const [notificationDropdownOpen, setNotificationDropdownOpen] = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"](false);
    const [userMenuOpen, setUserMenuOpen] = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"](false);
    const [searchOpen, setSearchOpen] = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"](false);
    const [mobileMenuOpen, setMobileMenuOpen] = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"](false);
    const userMenuRef = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRef"](null);
    const searchRef = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRef"](null);
    // Fetch unread notifications only when authenticated
    const { data: unreadNotifications = [] } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useNotifications$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useUnreadNotifications"])({
        enabled: isAuthenticated
    });
    // Close dropdowns when clicking outside
    __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useEffect"]({
        "Header.useEffect": ()=>{
            function handleClickOutside(event) {
                if (userMenuRef.current && !userMenuRef.current.contains(event.target)) {
                    setUserMenuOpen(false);
                }
                if (searchRef.current && !searchRef.current.contains(event.target)) {
                    setSearchOpen(false);
                }
            }
            document.addEventListener('mousedown', handleClickOutside);
            return ({
                "Header.useEffect": ()=>document.removeEventListener('mousedown', handleClickOutside)
            })["Header.useEffect"];
        }
    }["Header.useEffect"], []);
    /**
   * Helper to get user initials from fullName
   * Returns first letter of first name and last name, or first two letters if single name
   */ const getUserInitials = (fullName)=>{
        const names = fullName.trim().split(' ');
        if (names.length === 1) {
            return names[0].substring(0, 2).toUpperCase();
        }
        return (names[0][0] + names[names.length - 1][0]).toUpperCase();
    };
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("header", {
        className: `sticky top-0 z-50 bg-white shadow-[0_2px_10px_rgba(0,0,0,0.08)] ${className}`,
        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("nav", {
            className: "container mx-auto px-4 sm:px-6 lg:px-8",
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "flex items-center justify-between py-4",
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$atoms$2f$OfficialLogo$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["OfficialLogo"], {
                            size: "md"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/layout/Header.tsx",
                            lineNumber: 75,
                            columnNumber: 11
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                            onClick: ()=>setMobileMenuOpen(!mobileMenuOpen),
                            className: "lg:hidden p-2 text-[#333] hover:text-[#FF7900] transition-colors",
                            "aria-label": "Toggle menu",
                            children: mobileMenuOpen ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$x$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__X$3e$__["X"], {
                                className: "w-6 h-6"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                lineNumber: 84,
                                columnNumber: 15
                            }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$menu$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Menu$3e$__["Menu"], {
                                className: "w-6 h-6"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                lineNumber: 86,
                                columnNumber: 15
                            }, this)
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/layout/Header.tsx",
                            lineNumber: 78,
                            columnNumber: 11
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("nav", {
                            className: "hidden lg:flex items-center gap-6",
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
                                    href: "/events",
                                    className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors",
                                    children: "Events"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                    lineNumber: 92,
                                    columnNumber: 13
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
                                    href: "/forums",
                                    className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors",
                                    children: "Forums"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                    lineNumber: 98,
                                    columnNumber: 13
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
                                    href: "/business",
                                    className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors",
                                    children: "Business"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                    lineNumber: 104,
                                    columnNumber: 13
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
                                    href: "/marketplace",
                                    className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors",
                                    children: "Marketplace"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                    lineNumber: 110,
                                    columnNumber: 13
                                }, this),
                                isAuthenticated && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
                                    href: "/dashboard",
                                    className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors",
                                    children: "Dashboard"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                    lineNumber: 117,
                                    columnNumber: 15
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "relative",
                                    ref: searchRef,
                                    children: [
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                            onClick: ()=>setSearchOpen(!searchOpen),
                                            className: "p-2 text-[#333] hover:text-[#FF7900] transition-colors",
                                            "aria-label": "Search",
                                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$search$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Search$3e$__["Search"], {
                                                className: "w-5 h-5"
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                lineNumber: 132,
                                                columnNumber: 17
                                            }, this)
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                            lineNumber: 127,
                                            columnNumber: 15
                                        }, this),
                                        searchOpen && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                            className: "absolute right-0 mt-2 w-80 bg-white rounded-lg shadow-lg border border-gray-200 p-4 z-50",
                                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
                                                type: "text",
                                                placeholder: "Search events, forums, businesses...",
                                                className: "w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#FF7900] focus:border-transparent",
                                                autoFocus: true
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                lineNumber: 137,
                                                columnNumber: 19
                                            }, this)
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                            lineNumber: 136,
                                            columnNumber: 17
                                        }, this)
                                    ]
                                }, void 0, true, {
                                    fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                    lineNumber: 126,
                                    columnNumber: 13
                                }, this)
                            ]
                        }, void 0, true, {
                            fileName: "[project]/src/presentation/components/layout/Header.tsx",
                            lineNumber: 91,
                            columnNumber: 11
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                            className: "flex items-center gap-4",
                            children: isAuthenticated && user ? // Authenticated: Show notification bell and user avatar with dropdown
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex items-center gap-3",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "relative",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$notifications$2f$NotificationBell$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["NotificationBell"], {
                                                unreadCount: unreadNotifications.length,
                                                onClick: ()=>setNotificationDropdownOpen(!notificationDropdownOpen)
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                lineNumber: 155,
                                                columnNumber: 19
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$notifications$2f$NotificationDropdown$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["NotificationDropdown"], {
                                                notifications: unreadNotifications,
                                                isOpen: notificationDropdownOpen,
                                                onClose: ()=>setNotificationDropdownOpen(false)
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                lineNumber: 159,
                                                columnNumber: 19
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                        lineNumber: 154,
                                        columnNumber: 17
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "relative",
                                        ref: userMenuRef,
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "flex items-center gap-2 cursor-pointer hover:opacity-90 transition-opacity",
                                                onClick: ()=>setUserMenuOpen(!userMenuOpen),
                                                role: "button",
                                                tabIndex: 0,
                                                onKeyDown: (e)=>{
                                                    if (e.key === 'Enter' || e.key === ' ') {
                                                        setUserMenuOpen(!userMenuOpen);
                                                    }
                                                },
                                                title: user.fullName,
                                                children: [
                                                    user.profilePhotoUrl ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$image$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
                                                        src: user.profilePhotoUrl,
                                                        alt: user.fullName,
                                                        width: 40,
                                                        height: 40,
                                                        className: "w-10 h-10 rounded-full object-cover"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                        lineNumber: 182,
                                                        columnNumber: 23
                                                    }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        className: "w-10 h-10 rounded-full flex items-center justify-center text-white font-bold",
                                                        style: {
                                                            background: 'linear-gradient(135deg, #FF7900, #8B1538)'
                                                        },
                                                        children: getUserInitials(user.fullName)
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                        lineNumber: 190,
                                                        columnNumber: 23
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                        className: "text-sm font-medium text-[#333] hidden lg:inline",
                                                        children: user.fullName
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                        lineNumber: 201,
                                                        columnNumber: 21
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$down$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronDown$3e$__["ChevronDown"], {
                                                        className: `w-4 h-4 text-[#666] transition-transform duration-200 hidden lg:block ${userMenuOpen ? 'transform rotate-180' : ''}`
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                        lineNumber: 206,
                                                        columnNumber: 21
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                lineNumber: 168,
                                                columnNumber: 19
                                            }, this),
                                            userMenuOpen && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "absolute right-0 mt-2 w-48 rounded-lg shadow-lg overflow-hidden z-50",
                                                style: {
                                                    background: 'white',
                                                    border: '1px solid #e2e8f0'
                                                },
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        className: "px-4 py-3 border-b border-gray-200",
                                                        style: {
                                                            background: '#f7fafc'
                                                        },
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-sm font-medium text-[#333]",
                                                                children: user.fullName
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                                lineNumber: 224,
                                                                columnNumber: 25
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-xs text-gray-500",
                                                                children: user.email
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                                lineNumber: 225,
                                                                columnNumber: 25
                                                            }, this)
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                        lineNumber: 223,
                                                        columnNumber: 23
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                                        onClick: ()=>{
                                                            setUserMenuOpen(false);
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
                                                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                                lineNumber: 236,
                                                                columnNumber: 25
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                                style: {
                                                                    color: '#2d3748'
                                                                },
                                                                children: "Profile"
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                                lineNumber: 237,
                                                                columnNumber: 25
                                                            }, this)
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                        lineNumber: 229,
                                                        columnNumber: 23
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        style: {
                                                            borderTop: '1px solid #e2e8f0'
                                                        }
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                        lineNumber: 240,
                                                        columnNumber: 23
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                                        onClick: async ()=>{
                                                            setUserMenuOpen(false);
                                                            try {
                                                                // Call logout endpoint if needed
                                                                const { authRepository } = await __turbopack_context__.A("[project]/src/infrastructure/api/repositories/auth.repository.ts [app-client] (ecmascript, async loader)");
                                                                await authRepository.logout();
                                                            } catch (error) {
                                                            // Silently handle logout errors
                                                            } finally{
                                                                clearAuth();
                                                                router.push('/login');
                                                            }
                                                        },
                                                        className: "w-full flex items-center gap-3 px-4 py-3 hover:bg-gray-50 transition-colors text-left",
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$log$2d$out$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__LogOut$3e$__["LogOut"], {
                                                                className: "w-4 h-4",
                                                                style: {
                                                                    color: '#8B1538'
                                                                }
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                                lineNumber: 259,
                                                                columnNumber: 25
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                                style: {
                                                                    color: '#2d3748'
                                                                },
                                                                children: "Logout"
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                                lineNumber: 260,
                                                                columnNumber: 25
                                                            }, this)
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                        lineNumber: 243,
                                                        columnNumber: 23
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                lineNumber: 215,
                                                columnNumber: 21
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                        lineNumber: 167,
                                        columnNumber: 17
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                lineNumber: 152,
                                columnNumber: 15
                            }, this) : // Not authenticated: Show Login and Sign Up buttons
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Fragment"], {
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                        variant: "outline",
                                        size: "default",
                                        className: "border-[#8B1538] text-[#8B1538] hover:bg-[#8B1538] hover:text-white font-semibold transition-all",
                                        onClick: ()=>router.push('/login'),
                                        children: "Login"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                        lineNumber: 269,
                                        columnNumber: 17
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                        variant: "default",
                                        size: "default",
                                        className: "bg-[#FF7900] hover:bg-[#E66D00] text-white font-semibold transition-all",
                                        onClick: ()=>router.push('/register'),
                                        children: "Sign Up"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                        lineNumber: 277,
                                        columnNumber: 17
                                    }, this)
                                ]
                            }, void 0, true)
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/layout/Header.tsx",
                            lineNumber: 149,
                            columnNumber: 11
                        }, this)
                    ]
                }, void 0, true, {
                    fileName: "[project]/src/presentation/components/layout/Header.tsx",
                    lineNumber: 73,
                    columnNumber: 9
                }, this),
                mobileMenuOpen && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "lg:hidden border-t border-gray-200 py-4",
                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "flex flex-col gap-4",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
                                href: "/events",
                                className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors px-4 py-2 hover:bg-gray-50 rounded-lg",
                                onClick: ()=>setMobileMenuOpen(false),
                                children: "Events"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                lineNumber: 295,
                                columnNumber: 15
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
                                href: "/forums",
                                className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors px-4 py-2 hover:bg-gray-50 rounded-lg",
                                onClick: ()=>setMobileMenuOpen(false),
                                children: "Forums"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                lineNumber: 302,
                                columnNumber: 15
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
                                href: "/business",
                                className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors px-4 py-2 hover:bg-gray-50 rounded-lg",
                                onClick: ()=>setMobileMenuOpen(false),
                                children: "Business"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                lineNumber: 309,
                                columnNumber: 15
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
                                href: "/marketplace",
                                className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors px-4 py-2 hover:bg-gray-50 rounded-lg",
                                onClick: ()=>setMobileMenuOpen(false),
                                children: "Marketplace"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                lineNumber: 316,
                                columnNumber: 15
                            }, this),
                            isAuthenticated && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
                                href: "/dashboard",
                                className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors px-4 py-2 hover:bg-gray-50 rounded-lg",
                                onClick: ()=>setMobileMenuOpen(false),
                                children: "Dashboard"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                lineNumber: 324,
                                columnNumber: 17
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "px-4",
                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
                                    type: "text",
                                    placeholder: "Search events, forums, businesses...",
                                    className: "w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#FF7900] focus:border-transparent"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                    lineNumber: 335,
                                    columnNumber: 17
                                }, this)
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                lineNumber: 334,
                                columnNumber: 15
                            }, this),
                            !isAuthenticated && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex flex-col gap-2 px-4 pt-4 border-t border-gray-200",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                        variant: "outline",
                                        size: "default",
                                        className: "w-full border-[#8B1538] text-[#8B1538] hover:bg-[#8B1538] hover:text-white font-semibold transition-all",
                                        onClick: ()=>{
                                            setMobileMenuOpen(false);
                                            router.push('/login');
                                        },
                                        children: "Login"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                        lineNumber: 345,
                                        columnNumber: 19
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                        variant: "default",
                                        size: "default",
                                        className: "w-full bg-[#FF7900] hover:bg-[#E66D00] text-white font-semibold transition-all",
                                        onClick: ()=>{
                                            setMobileMenuOpen(false);
                                            router.push('/register');
                                        },
                                        children: "Sign Up"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                        lineNumber: 356,
                                        columnNumber: 19
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                lineNumber: 344,
                                columnNumber: 17
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/layout/Header.tsx",
                        lineNumber: 293,
                        columnNumber: 13
                    }, this)
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/layout/Header.tsx",
                    lineNumber: 292,
                    columnNumber: 11
                }, this)
            ]
        }, void 0, true, {
            fileName: "[project]/src/presentation/components/layout/Header.tsx",
            lineNumber: 72,
            columnNumber: 7
        }, this)
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/layout/Header.tsx",
        lineNumber: 69,
        columnNumber: 5
    }, this);
}
_s(Header, "H4H/CVit1bYu9ezuHImHrbfOcsg=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useAuthStore"],
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRouter"],
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useNotifications$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useUnreadNotifications"]
    ];
});
_c = Header;
var _c;
__turbopack_context__.k.register(_c, "Header");
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
"[project]/src/presentation/components/ui/TreeDropdown.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "TreeDropdown",
    ()=>TreeDropdown
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$down$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronDown$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/chevron-down.js [app-client] (ecmascript) <export default as ChevronDown>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$right$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronRight$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/chevron-right.js [app-client] (ecmascript) <export default as ChevronRight>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$check$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Check$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/check.js [app-client] (ecmascript) <export default as Check>");
;
var _s = __turbopack_context__.k.signature();
'use client';
;
;
function TreeDropdown({ nodes, selectedIds, onSelectionChange, placeholder = 'Select items', maxSelections, disabled = false, className = '' }) {
    _s();
    const [isOpen, setIsOpen] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(false);
    const [expandedNodes, setExpandedNodes] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(new Set());
    const dropdownRef = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRef"])(null);
    // Close dropdown when clicking outside
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useEffect"])({
        "TreeDropdown.useEffect": ()=>{
            function handleClickOutside(event) {
                if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
                    setIsOpen(false);
                }
            }
            if (isOpen) {
                document.addEventListener('mousedown', handleClickOutside);
                return ({
                    "TreeDropdown.useEffect": ()=>document.removeEventListener('mousedown', handleClickOutside)
                })["TreeDropdown.useEffect"];
            }
        }
    }["TreeDropdown.useEffect"], [
        isOpen
    ]);
    const toggleNode = (nodeId)=>{
        const newExpanded = new Set(expandedNodes);
        if (newExpanded.has(nodeId)) {
            newExpanded.delete(nodeId);
        } else {
            newExpanded.add(nodeId);
        }
        setExpandedNodes(newExpanded);
    };
    /**
   * Recursively collect all child node IDs
   */ const getAllChildIds = (node)=>{
        const ids = [];
        if (node.children) {
            for (const child of node.children){
                ids.push(child.id);
                ids.push(...getAllChildIds(child));
            }
        }
        return ids;
    };
    /**
   * Find a node by ID in the tree
   */ const findNodeById = (nodeId, searchNodes = nodes)=>{
        for (const node of searchNodes){
            if (node.id === nodeId) {
                return node;
            }
            if (node.children) {
                const found = findNodeById(nodeId, node.children);
                if (found) return found;
            }
        }
        return null;
    };
    const toggleSelection = (nodeId)=>{
        const newSelected = new Set(selectedIds);
        const node = findNodeById(nodeId);
        if (!node) return;
        const hasChildren = node.children && node.children.length > 0;
        if (newSelected.has(nodeId)) {
            // Unchecking: remove node and all children
            newSelected.delete(nodeId);
            if (hasChildren) {
                const childIds = getAllChildIds(node);
                childIds.forEach((id)=>newSelected.delete(id));
            }
        } else {
            // Checking: For parent nodes with children, only add children (not parent itself)
            // For leaf nodes, add the node itself
            const idsToAdd = [];
            if (hasChildren) {
                // Parent node: only add children, not the parent ID
                idsToAdd.push(...getAllChildIds(node));
            } else {
                // Leaf node: add the node itself
                idsToAdd.push(nodeId);
            }
            // Check max selections
            if (maxSelections && newSelected.size + idsToAdd.length > maxSelections) {
                return; // Don't add if max would be exceeded
            }
            idsToAdd.forEach((id)=>newSelected.add(id));
        }
        onSelectionChange(Array.from(newSelected));
    };
    const renderTreeNode = (node, level = 0)=>{
        const hasChildren = node.children && node.children.length > 0;
        const isExpanded = expandedNodes.has(node.id);
        // Phase 6A.9 FIX: For parent nodes, check if ALL children are selected
        // This ensures state checkboxes show as checked when all cities are selected
        let isSelected = selectedIds.includes(node.id);
        if (hasChildren && !isSelected) {
            const childIds = getAllChildIds(node);
            if (childIds.length > 0) {
                isSelected = childIds.every((childId)=>selectedIds.includes(childId));
            }
        }
        const indentClass = level > 0 ? `ml-${level * 6}` : '';
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: `flex items-center gap-2 px-3 py-2 hover:bg-gray-50 cursor-pointer ${indentClass}`,
                    style: {
                        paddingLeft: `${level * 24 + 12}px`
                    },
                    children: [
                        hasChildren ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                            type: "button",
                            onClick: (e)=>{
                                e.stopPropagation();
                                toggleNode(node.id);
                            },
                            className: "p-0.5 hover:bg-gray-200 rounded",
                            "aria-label": isExpanded ? 'Collapse' : 'Expand',
                            children: isExpanded ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$down$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronDown$3e$__["ChevronDown"], {
                                className: "h-4 w-4",
                                style: {
                                    color: '#FF7900'
                                }
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                                lineNumber: 187,
                                columnNumber: 17
                            }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$right$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronRight$3e$__["ChevronRight"], {
                                className: "h-4 w-4",
                                style: {
                                    color: '#FF7900'
                                }
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                                lineNumber: 189,
                                columnNumber: 17
                            }, this)
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                            lineNumber: 177,
                            columnNumber: 13
                        }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                            className: "w-5"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                            lineNumber: 193,
                            columnNumber: 13
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                            className: "flex items-center gap-2 flex-1 cursor-pointer",
                            onClick: (e)=>e.stopPropagation(),
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
                                    type: "checkbox",
                                    checked: isSelected,
                                    onChange: ()=>toggleSelection(node.id),
                                    disabled: disabled,
                                    className: "h-4 w-4 rounded border-gray-300 focus:ring-2 focus:ring-offset-0",
                                    style: {
                                        accentColor: '#FF7900'
                                    }
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                                    lineNumber: 201,
                                    columnNumber: 13
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                    className: "text-sm",
                                    children: node.label
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                                    lineNumber: 211,
                                    columnNumber: 13
                                }, this),
                                isSelected && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$check$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Check$3e$__["Check"], {
                                    className: "h-3.5 w-3.5 ml-auto",
                                    style: {
                                        color: '#006400'
                                    }
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                                    lineNumber: 213,
                                    columnNumber: 15
                                }, this)
                            ]
                        }, void 0, true, {
                            fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                            lineNumber: 197,
                            columnNumber: 11
                        }, this)
                    ]
                }, void 0, true, {
                    fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                    lineNumber: 171,
                    columnNumber: 9
                }, this),
                hasChildren && isExpanded && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    children: node.children.map((child)=>renderTreeNode(child, level + 1))
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                    lineNumber: 220,
                    columnNumber: 11
                }, this)
            ]
        }, node.id, true, {
            fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
            lineNumber: 170,
            columnNumber: 7
        }, this);
    };
    const selectedCount = selectedIds.length;
    const displayText = selectedCount === 0 ? placeholder : `${selectedCount} ${selectedCount === 1 ? 'item' : 'items'} selected`;
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        ref: dropdownRef,
        className: `relative ${className}`,
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                type: "button",
                onClick: ()=>!disabled && setIsOpen(!isOpen),
                disabled: disabled,
                className: "w-full flex items-center justify-between px-4 py-2 bg-white border-2 rounded-lg text-sm transition-colors hover:border-gray-400 focus:outline-none focus:ring-2 focus:ring-offset-0 disabled:opacity-50 disabled:cursor-not-allowed",
                style: {
                    borderColor: isOpen ? '#FF7900' : '#e2e8f0'
                },
                "aria-haspopup": "listbox",
                "aria-expanded": isOpen,
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                        className: selectedCount === 0 ? 'text-gray-500' : 'text-gray-900',
                        children: displayText
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                        lineNumber: 248,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$down$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronDown$3e$__["ChevronDown"], {
                        className: `h-4 w-4 transition-transform ${isOpen ? 'rotate-180' : ''}`,
                        style: {
                            color: '#8B1538'
                        }
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                        lineNumber: 251,
                        columnNumber: 9
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                lineNumber: 237,
                columnNumber: 7
            }, this),
            isOpen && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "absolute z-50 w-full mt-2 bg-white border-2 rounded-lg shadow-lg max-h-96 overflow-y-auto",
                style: {
                    borderColor: '#FF7900'
                },
                role: "listbox",
                children: [
                    nodes.length === 0 ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "px-4 py-3 text-sm text-gray-500 text-center",
                        children: "No items available"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                        lineNumber: 265,
                        columnNumber: 13
                    }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "py-1",
                        children: nodes.map((node)=>renderTreeNode(node))
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                        lineNumber: 269,
                        columnNumber: 13
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "px-4 py-2 border-t bg-gray-50 flex items-center justify-between",
                        style: {
                            borderColor: '#e2e8f0'
                        },
                        children: [
                            maxSelections && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                className: "text-xs text-gray-600",
                                children: [
                                    selectedCount,
                                    " of ",
                                    maxSelections,
                                    " selected"
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                                lineNumber: 280,
                                columnNumber: 15
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                type: "button",
                                onClick: ()=>setIsOpen(false),
                                className: "px-3 py-1 text-xs font-medium text-white rounded hover:opacity-90 transition-opacity",
                                style: {
                                    backgroundColor: '#FF7900'
                                },
                                children: "Done"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                                lineNumber: 284,
                                columnNumber: 13
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                        lineNumber: 275,
                        columnNumber: 11
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                lineNumber: 259,
                columnNumber: 9
            }, this)
        ]
    }, void 0, true, {
        fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
        lineNumber: 235,
        columnNumber: 5
    }, this);
}
_s(TreeDropdown, "6IrrUpS5KP3z4r/2KGC5HtBH0Ko=");
_c = TreeDropdown;
var _c;
__turbopack_context__.k.register(_c, "TreeDropdown");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/infrastructure/api/repositories/metro-areas.repository.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "metroAreasRepository",
    ()=>metroAreasRepository
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/client/api-client.ts [app-client] (ecmascript)");
;
const metroAreasRepository = {
    /**
   * Get all active metro areas
   * Endpoint: GET /api/metro-areas?activeOnly=true
   */ async getAll (activeOnly = true) {
        const data = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].get('/metro-areas', {
            params: {
                activeOnly
            }
        });
        return data;
    },
    /**
   * Get metro areas by state
   * Client-side filtering of all metros by state code
   */ async getByState (stateCode) {
        const allMetros = await this.getAll();
        return allMetros.filter((metro)=>metro.state === stateCode);
    },
    /**
   * Get a single metro area by ID
   * Client-side lookup from all metros
   */ async getById (id) {
        const allMetros = await this.getAll();
        return allMetros.find((metro)=>metro.id === id);
    }
};
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/hooks/useMetroAreas.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "useMetroAreas",
    ()=>useMetroAreas
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$metro$2d$areas$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/repositories/metro-areas.repository.ts [app-client] (ecmascript)");
var _s = __turbopack_context__.k.signature();
;
;
function useMetroAreas() {
    _s();
    const [metroAreas, setMetroAreas] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])([]);
    const [isLoading, setIsLoading] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(true);
    const [error, setError] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(null);
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useEffect"])({
        "useMetroAreas.useEffect": ()=>{
            let isMounted = true;
            async function fetchMetroAreas() {
                try {
                    console.log('[useMetroAreas] Starting to fetch metro areas...');
                    setIsLoading(true);
                    setError(null);
                    const data = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$metro$2d$areas$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["metroAreasRepository"].getAll(true);
                    console.log('[useMetroAreas] Successfully fetched', data.length, 'metro areas');
                    if (isMounted) {
                        setMetroAreas(data);
                    }
                } catch (err) {
                    console.error('[useMetroAreas] ERROR fetching metro areas:', err);
                    if (isMounted) {
                        const errorMessage = err instanceof Error ? err.message : 'Failed to load metro areas';
                        setError(errorMessage);
                        console.error('Error fetching metro areas:', err);
                    }
                } finally{
                    if (isMounted) {
                        console.log('[useMetroAreas] Finished fetching (loading=false)');
                        setIsLoading(false);
                    }
                }
            }
            fetchMetroAreas();
            return ({
                "useMetroAreas.useEffect": ()=>{
                    isMounted = false;
                }
            })["useMetroAreas.useEffect"];
        }
    }["useMetroAreas.useEffect"], []);
    // Group metros by state
    const metroAreasByState = new Map();
    for (const metro of metroAreas){
        if (!metroAreasByState.has(metro.state)) {
            metroAreasByState.set(metro.state, []);
        }
        metroAreasByState.get(metro.state).push(metro);
    }
    // Sort metros within each state (state-level first, then alphabetically)
    for (const [, metros] of metroAreasByState){
        metros.sort((a, b)=>{
            if (a.isStateLevelArea && !b.isStateLevelArea) return -1;
            if (!a.isStateLevelArea && b.isStateLevelArea) return 1;
            return a.name.localeCompare(b.name);
        });
    }
    // Separate state-level and city-level metros
    const stateLevelMetros = metroAreas.filter((m)=>m.isStateLevelArea);
    const cityLevelMetros = metroAreas.filter((m)=>!m.isStateLevelArea);
    return {
        metroAreas,
        metroAreasByState,
        stateLevelMetros,
        cityLevelMetros,
        isLoading,
        error
    };
}
_s(useMetroAreas, "kM3BQFfNye0Frg1odBZ0AW1+3WI=");
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
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$domain$2f$constants$2f$metroAreas$2e$constants$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/domain/constants/metroAreas.constants.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$TreeDropdown$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/TreeDropdown.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useMetroAreas$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/hooks/useMetroAreas.ts [app-client] (ecmascript)");
;
var _s = __turbopack_context__.k.signature();
'use client';
;
;
;
;
function NewsletterMetroSelector({ selectedMetroIds, receiveAllLocations, onMetrosChange, onReceiveAllChange, disabled = false, maxSelections = 20 }) {
    _s();
    const [validationError, setValidationError] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])('');
    // Phase 6A.9: Fetch metro areas from API instead of hardcoded constants
    const { metroAreasByState, isLoading: metrosLoading, error: metrosError } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useMetroAreas$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMetroAreas"])();
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
    /**
   * Transform metro areas data into TreeNode format for TreeDropdown
   * Each state becomes a parent node, city metros become children
   */ const treeNodes = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMemo"])({
        "NewsletterMetroSelector.useMemo[treeNodes]": ()=>{
            const nodes = [];
            for (const state of __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$domain$2f$constants$2f$metroAreas$2e$constants$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["US_STATES"]){
                const metrosForState = metroAreasByState.get(state.code) || [];
                // Filter out state-level metros (like "All Alabama")
                // Note: After database cleanup, there should be no state-level metros
                const cityMetros = metrosForState.filter({
                    "NewsletterMetroSelector.useMemo[treeNodes].cityMetros": (m)=>!m.isStateLevelArea
                }["NewsletterMetroSelector.useMemo[treeNodes].cityMetros"]);
                // Only include states that have city metros
                if (cityMetros.length === 0) continue;
                // Create child nodes for each metro
                const children = cityMetros.map({
                    "NewsletterMetroSelector.useMemo[treeNodes].children": (metro)=>({
                            id: metro.id,
                            label: metro.name,
                            checked: selectedMetroIds.includes(metro.id)
                        })
                }["NewsletterMetroSelector.useMemo[treeNodes].children"]);
                // Create parent node for the state
                nodes.push({
                    id: `state-${state.code}`,
                    label: state.name,
                    checked: children.every({
                        "NewsletterMetroSelector.useMemo[treeNodes]": (child)=>selectedMetroIds.includes(child.id)
                    }["NewsletterMetroSelector.useMemo[treeNodes]"]),
                    children
                });
            }
            return nodes;
        }
    }["NewsletterMetroSelector.useMemo[treeNodes]"], [
        metroAreasByState,
        selectedMetroIds
    ]);
    const handleReceiveAllChange = (receiveAll)=>{
        onReceiveAllChange(receiveAll);
        if (receiveAll) {
            onMetrosChange([]); // Clear selections when choosing all locations
            setValidationError('');
        }
    };
    const handleSelectionChange = (newSelectedIds)=>{
        // Filter out state-level IDs (they start with "state-")
        const metroIds = newSelectedIds.filter((id)=>!id.startsWith('state-'));
        console.log('[NewsletterMetroSelector] TreeDropdown selection changed:');
        console.log('  Raw IDs from TreeDropdown:', newSelectedIds);
        console.log('  Filtered metro IDs (state-* removed):', metroIds);
        console.log('  Calling onMetrosChange with', metroIds.length, 'IDs');
        onMetrosChange(metroIds);
    };
    // Show loading state while fetching metros
    if (metrosLoading) {
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "space-y-4",
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                            className: "text-sm font-medium text-gray-700 mb-2 block",
                            children: "Get notifications for events in:"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                            lineNumber: 116,
                            columnNumber: 11
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                            className: "text-xs text-gray-500 mb-3",
                            children: "Loading metro areas..."
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                            lineNumber: 119,
                            columnNumber: 11
                        }, this)
                    ]
                }, void 0, true, {
                    fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                    lineNumber: 115,
                    columnNumber: 9
                }, this),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "flex items-center justify-center p-4",
                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "animate-spin rounded-full h-8 w-8 border-b-2",
                        style: {
                            borderColor: '#FF7900'
                        }
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                        lineNumber: 122,
                        columnNumber: 11
                    }, this)
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                    lineNumber: 121,
                    columnNumber: 9
                }, this)
            ]
        }, void 0, true, {
            fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
            lineNumber: 114,
            columnNumber: 7
        }, this);
    }
    // Show error if metros failed to load
    if (metrosError) {
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "space-y-4",
            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                        className: "text-sm font-medium text-gray-700 mb-2 block",
                        children: "Get notifications for events in:"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                        lineNumber: 133,
                        columnNumber: 11
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                        className: "text-xs text-red-600",
                        role: "alert",
                        children: [
                            "Failed to load metro areas: ",
                            metrosError
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                        lineNumber: 136,
                        columnNumber: 11
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                lineNumber: 132,
                columnNumber: 9
            }, this)
        }, void 0, false, {
            fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
            lineNumber: 131,
            columnNumber: 7
        }, this);
    }
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        className: "space-y-4",
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                        className: "text-sm font-medium text-gray-700 mb-2 block",
                        children: "Get notifications for events in:"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                        lineNumber: 148,
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
                        lineNumber: 151,
                        columnNumber: 9
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                lineNumber: 147,
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
                            lineNumber: 159,
                            columnNumber: 11
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                            className: "font-medium",
                            children: "Send me events from all locations"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                            lineNumber: 166,
                            columnNumber: 11
                        }, this)
                    ]
                }, void 0, true, {
                    fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                    lineNumber: 158,
                    columnNumber: 9
                }, this)
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                lineNumber: 157,
                columnNumber: 7
            }, this),
            !receiveAllLocations && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "space-y-3",
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$TreeDropdown$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["TreeDropdown"], {
                        nodes: treeNodes,
                        selectedIds: selectedMetroIds,
                        onSelectionChange: handleSelectionChange,
                        placeholder: "Select metro areas",
                        maxSelections: maxSelections,
                        disabled: disabled
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                        lineNumber: 174,
                        columnNumber: 11
                    }, this),
                    validationError && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                        className: "text-xs text-red-600",
                        role: "alert",
                        children: validationError
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                        lineNumber: 185,
                        columnNumber: 13
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                lineNumber: 172,
                columnNumber: 9
            }, this)
        ]
    }, void 0, true, {
        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
        lineNumber: 145,
        columnNumber: 5
    }, this);
}
_s(NewsletterMetroSelector, "XygrWInJXiBIrwexkwWY3pS5kdE=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useMetroAreas$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMetroAreas"]
    ];
});
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
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$facebook$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Facebook$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/facebook.js [app-client] (ecmascript) <export default as Facebook>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$twitter$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Twitter$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/twitter.js [app-client] (ecmascript) <export default as Twitter>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$instagram$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Instagram$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/instagram.js [app-client] (ecmascript) <export default as Instagram>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$youtube$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Youtube$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/youtube.js [app-client] (ecmascript) <export default as Youtube>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$mail$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Mail$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/mail.js [app-client] (ecmascript) <export default as Mail>");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/store/useAuthStore.ts [app-client] (ecmascript)");
;
var _s = __turbopack_context__.k.signature();
'use client';
;
;
;
;
;
;
const FooterLink = ({ href, children, external = false })=>{
    const linkClasses = "text-white/80 hover:text-white transition-colors duration-200 text-sm";
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
            lineNumber: 21,
            columnNumber: 7
        }, ("TURBOPACK compile-time value", void 0));
    }
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {
        href: href,
        className: linkClasses,
        children: children
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
        lineNumber: 34,
        columnNumber: 5
    }, ("TURBOPACK compile-time value", void 0));
};
_c = FooterLink;
const Footer = ()=>{
    _s();
    const { isAuthenticated } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useAuthStore"])();
    const [email, setEmail] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])('');
    const [subscribeStatus, setSubscribeStatus] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])('idle');
    const [selectedMetroIds, setSelectedMetroIds] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])([]);
    const [receiveAllLocations, setReceiveAllLocations] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(false);
    const [currentYear, setCurrentYear] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(2025);
    // Set current year on client side only to avoid hydration mismatch
    __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"].useEffect({
        "Footer.useEffect": ()=>{
            setCurrentYear(new Date().getFullYear());
        }
    }["Footer.useEffect"], []);
    const linkCategories = [
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
                    label: 'Cultural Hub',
                    href: '/culture'
                },
                ...isAuthenticated ? [
                    {
                        label: 'Dashboard',
                        href: '/dashboard'
                    }
                ] : []
            ]
        },
        {
            title: 'Marketplace',
            links: [
                {
                    label: 'Browse Listings',
                    href: '/marketplace'
                },
                {
                    label: 'Businesses',
                    href: '/business'
                },
                {
                    label: 'Services',
                    href: '/services'
                },
                {
                    label: 'Sell Items',
                    href: '/marketplace/sell'
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
                    label: 'Guidelines',
                    href: '/guidelines'
                },
                {
                    label: 'Safety',
                    href: '/safety'
                },
                {
                    label: 'Blog',
                    href: '/blog'
                }
            ]
        },
        {
            title: 'About',
            links: [
                {
                    label: 'Our Story',
                    href: '/about'
                },
                {
                    label: 'Contact Us',
                    href: '/contact'
                },
                {
                    label: 'Careers',
                    href: '/careers'
                },
                {
                    label: 'Press',
                    href: '/press'
                }
            ]
        }
    ];
    const handleNewsletterSubmit = async (e)=>{
        e.preventDefault();
        console.log('[Footer] Newsletter form submitted:');
        console.log('  Email:', email);
        console.log('  Receive all locations:', receiveAllLocations);
        console.log('  Selected metro IDs:', selectedMetroIds);
        console.log('  Selected metro count:', selectedMetroIds.length);
        if (!email || !email.includes('@')) {
            console.log('[Footer] ❌ Validation failed: Invalid email');
            setSubscribeStatus('error');
            return;
        }
        if (!receiveAllLocations && selectedMetroIds.length === 0) {
            console.log('[Footer] ❌ Validation failed: No metros selected and not receiving all locations');
            setSubscribeStatus('error');
            return;
        }
        console.log('[Footer] ✅ Validation passed, submitting...');
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
                    Email: email,
                    MetroAreaIds: receiveAllLocations ? [] : selectedMetroIds,
                    ReceiveAllLocations: receiveAllLocations,
                    Timestamp: new Date().toISOString()
                })
            });
            const data = await response.json();
            console.log('[Footer] Backend response:', response.status, data);
            if (data.success || data.Success) {
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
        className: "bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 text-white mt-24 relative overflow-hidden",
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "absolute inset-0 opacity-10",
                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "absolute inset-0",
                    style: {
                        backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`
                    }
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                    lineNumber: 169,
                    columnNumber: 9
                }, ("TURBOPACK compile-time value", void 0))
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                lineNumber: 168,
                columnNumber: 7
            }, ("TURBOPACK compile-time value", void 0)),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12",
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "bg-white/10 backdrop-blur-sm rounded-2xl p-8 mb-12 border border-white/20",
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                            className: "max-w-xl mx-auto",
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
                                    className: "text-2xl font-semibold mb-2 text-center",
                                    children: "Stay Connected"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                    lineNumber: 178,
                                    columnNumber: 13
                                }, ("TURBOPACK compile-time value", void 0)),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                    className: "text-white/90 mb-6 text-center",
                                    children: "Subscribe to our newsletter for the latest events and community updates."
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                    lineNumber: 179,
                                    columnNumber: 13
                                }, ("TURBOPACK compile-time value", void 0)),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("form", {
                                    onSubmit: handleNewsletterSubmit,
                                    className: "space-y-4",
                                    children: [
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
                                            type: "email",
                                            placeholder: "Enter your email",
                                            value: email,
                                            onChange: (e)=>setEmail(e.target.value),
                                            className: "w-full px-4 py-3 rounded-lg bg-white text-gray-900 placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-orange-500",
                                            "aria-label": "Email address for newsletter",
                                            disabled: subscribeStatus === 'loading',
                                            required: true
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                            lineNumber: 182,
                                            columnNumber: 15
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
                                                lineNumber: 194,
                                                columnNumber: 17
                                            }, ("TURBOPACK compile-time value", void 0))
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                            lineNumber: 193,
                                            columnNumber: 15
                                        }, ("TURBOPACK compile-time value", void 0)),
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                            type: "submit",
                                            className: "w-full px-6 py-3 bg-[#FF7900] hover:bg-[#E56D00] text-white font-medium rounded-lg transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed",
                                            disabled: subscribeStatus === 'loading',
                                            "aria-label": "Subscribe to newsletter",
                                            children: subscribeStatus === 'loading' ? 'Subscribing...' : subscribeStatus === 'success' ? 'Subscribed!' : 'Subscribe'
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                            lineNumber: 203,
                                            columnNumber: 15
                                        }, ("TURBOPACK compile-time value", void 0))
                                    ]
                                }, void 0, true, {
                                    fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                    lineNumber: 181,
                                    columnNumber: 13
                                }, ("TURBOPACK compile-time value", void 0)),
                                subscribeStatus === 'error' && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                    className: "text-red-300 text-sm mt-2 text-center",
                                    role: "alert",
                                    children: "Please enter a valid email address and select at least one location."
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                    lineNumber: 214,
                                    columnNumber: 15
                                }, ("TURBOPACK compile-time value", void 0)),
                                subscribeStatus === 'success' && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                    className: "text-green-300 text-sm mt-2 text-center",
                                    role: "alert",
                                    children: "Thank you for subscribing!"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                    lineNumber: 219,
                                    columnNumber: 15
                                }, ("TURBOPACK compile-time value", void 0))
                            ]
                        }, void 0, true, {
                            fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                            lineNumber: 177,
                            columnNumber: 11
                        }, ("TURBOPACK compile-time value", void 0))
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                        lineNumber: 176,
                        columnNumber: 9
                    }, ("TURBOPACK compile-time value", void 0)),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "grid grid-cols-2 md:grid-cols-4 gap-8 mb-12",
                        children: linkCategories.map((category)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h4", {
                                        className: "text-white font-semibold mb-4",
                                        children: category.title
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 230,
                                        columnNumber: 15
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("ul", {
                                        className: "space-y-2",
                                        role: "list",
                                        children: category.links.map((link)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("li", {
                                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(FooterLink, {
                                                    href: link.href,
                                                    external: link.external,
                                                    children: link.label
                                                }, void 0, false, {
                                                    fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                                    lineNumber: 234,
                                                    columnNumber: 21
                                                }, ("TURBOPACK compile-time value", void 0))
                                            }, link.label, false, {
                                                fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                                lineNumber: 233,
                                                columnNumber: 19
                                            }, ("TURBOPACK compile-time value", void 0)))
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 231,
                                        columnNumber: 15
                                    }, ("TURBOPACK compile-time value", void 0))
                                ]
                            }, category.title, true, {
                                fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                lineNumber: 229,
                                columnNumber: 13
                            }, ("TURBOPACK compile-time value", void 0)))
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                        lineNumber: 227,
                        columnNumber: 9
                    }, ("TURBOPACK compile-time value", void 0)),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "pt-8 border-t border-white/20 flex flex-col md:flex-row items-center justify-between gap-4",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex items-center gap-3",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$atoms$2f$Logo$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Logo"], {
                                        size: "md",
                                        showText: false
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 247,
                                        columnNumber: 13
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "text-white font-semibold",
                                                children: "LankaConnect"
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                                lineNumber: 249,
                                                columnNumber: 15
                                            }, ("TURBOPACK compile-time value", void 0)),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "text-white/80 text-sm",
                                                children: [
                                                    "© ",
                                                    currentYear,
                                                    " All rights reserved"
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                                lineNumber: 250,
                                                columnNumber: 15
                                            }, ("TURBOPACK compile-time value", void 0))
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 248,
                                        columnNumber: 13
                                    }, ("TURBOPACK compile-time value", void 0))
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                lineNumber: 246,
                                columnNumber: 11
                            }, ("TURBOPACK compile-time value", void 0)),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex items-center gap-3",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
                                        href: "https://facebook.com",
                                        target: "_blank",
                                        rel: "noopener noreferrer",
                                        className: "text-white/80 hover:text-white hover:bg-white/10 p-2 rounded-lg transition-colors",
                                        "aria-label": "Facebook",
                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$facebook$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Facebook$3e$__["Facebook"], {
                                            className: "h-5 w-5"
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                            lineNumber: 262,
                                            columnNumber: 15
                                        }, ("TURBOPACK compile-time value", void 0))
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 255,
                                        columnNumber: 13
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
                                        href: "https://twitter.com",
                                        target: "_blank",
                                        rel: "noopener noreferrer",
                                        className: "text-white/80 hover:text-white hover:bg-white/10 p-2 rounded-lg transition-colors",
                                        "aria-label": "Twitter",
                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$twitter$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Twitter$3e$__["Twitter"], {
                                            className: "h-5 w-5"
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                            lineNumber: 271,
                                            columnNumber: 15
                                        }, ("TURBOPACK compile-time value", void 0))
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 264,
                                        columnNumber: 13
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
                                        href: "https://instagram.com",
                                        target: "_blank",
                                        rel: "noopener noreferrer",
                                        className: "text-white/80 hover:text-white hover:bg-white/10 p-2 rounded-lg transition-colors",
                                        "aria-label": "Instagram",
                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$instagram$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Instagram$3e$__["Instagram"], {
                                            className: "h-5 w-5"
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                            lineNumber: 280,
                                            columnNumber: 15
                                        }, ("TURBOPACK compile-time value", void 0))
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 273,
                                        columnNumber: 13
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
                                        href: "https://youtube.com",
                                        target: "_blank",
                                        rel: "noopener noreferrer",
                                        className: "text-white/80 hover:text-white hover:bg-white/10 p-2 rounded-lg transition-colors",
                                        "aria-label": "YouTube",
                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$youtube$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Youtube$3e$__["Youtube"], {
                                            className: "h-5 w-5"
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                            lineNumber: 289,
                                            columnNumber: 15
                                        }, ("TURBOPACK compile-time value", void 0))
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 282,
                                        columnNumber: 13
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
                                        href: "mailto:contact@lankaconnect.com",
                                        className: "text-white/80 hover:text-white hover:bg-white/10 p-2 rounded-lg transition-colors",
                                        "aria-label": "Email",
                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$mail$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Mail$3e$__["Mail"], {
                                            className: "h-5 w-5"
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                            lineNumber: 296,
                                            columnNumber: 15
                                        }, ("TURBOPACK compile-time value", void 0))
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 291,
                                        columnNumber: 13
                                    }, ("TURBOPACK compile-time value", void 0))
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                lineNumber: 254,
                                columnNumber: 11
                            }, ("TURBOPACK compile-time value", void 0))
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                        lineNumber: 245,
                        columnNumber: 9
                    }, ("TURBOPACK compile-time value", void 0))
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                lineNumber: 174,
                columnNumber: 7
            }, ("TURBOPACK compile-time value", void 0))
        ]
    }, void 0, true, {
        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
        lineNumber: 166,
        columnNumber: 5
    }, ("TURBOPACK compile-time value", void 0));
};
_s(Footer, "tAvE+8aqALXCrPmb4kBe/aDsrRI=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useAuthStore"]
    ];
});
_c1 = Footer;
const __TURBOPACK__default__export__ = Footer;
var _c, _c1;
__turbopack_context__.k.register(_c, "FooterLink");
__turbopack_context__.k.register(_c1, "Footer");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/ui/Card.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "Card",
    ()=>Card,
    "CardContent",
    ()=>CardContent,
    "CardDescription",
    ()=>CardDescription,
    "CardFooter",
    ()=>CardFooter,
    "CardHeader",
    ()=>CardHeader,
    "CardTitle",
    ()=>CardTitle
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-client] (ecmascript)");
;
;
;
/**
 * Card Component
 * Reusable card container with header, content, and footer sections
 * Follows UI/UX best practices for content grouping
 */ const Card = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["forwardRef"](_c = ({ className, ...props }, ref)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('rounded-lg border bg-card text-card-foreground shadow-sm', className),
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Card.tsx",
        lineNumber: 11,
        columnNumber: 5
    }, ("TURBOPACK compile-time value", void 0)));
_c1 = Card;
Card.displayName = 'Card';
const CardHeader = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["forwardRef"](_c2 = ({ className, ...props }, ref)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('flex flex-col space-y-1.5 p-6', className),
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Card.tsx",
        lineNumber: 22,
        columnNumber: 5
    }, ("TURBOPACK compile-time value", void 0)));
_c3 = CardHeader;
CardHeader.displayName = 'CardHeader';
const CardTitle = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["forwardRef"](_c4 = ({ className, ...props }, ref)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('text-2xl font-semibold leading-none tracking-tight', className),
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Card.tsx",
        lineNumber: 29,
        columnNumber: 5
    }, ("TURBOPACK compile-time value", void 0)));
_c5 = CardTitle;
CardTitle.displayName = 'CardTitle';
const CardDescription = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["forwardRef"](_c6 = ({ className, ...props }, ref)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('text-sm text-muted-foreground', className),
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Card.tsx",
        lineNumber: 42,
        columnNumber: 3
    }, ("TURBOPACK compile-time value", void 0)));
_c7 = CardDescription;
CardDescription.displayName = 'CardDescription';
const CardContent = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["forwardRef"](_c8 = ({ className, ...props }, ref)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('p-6 pt-0', className),
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Card.tsx",
        lineNumber: 48,
        columnNumber: 5
    }, ("TURBOPACK compile-time value", void 0)));
_c9 = CardContent;
CardContent.displayName = 'CardContent';
const CardFooter = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["forwardRef"](_c10 = ({ className, ...props }, ref)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('flex items-center p-6 pt-0', className),
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Card.tsx",
        lineNumber: 55,
        columnNumber: 5
    }, ("TURBOPACK compile-time value", void 0)));
_c11 = CardFooter;
CardFooter.displayName = 'CardFooter';
;
var _c, _c1, _c2, _c3, _c4, _c5, _c6, _c7, _c8, _c9, _c10, _c11;
__turbopack_context__.k.register(_c, "Card$React.forwardRef");
__turbopack_context__.k.register(_c1, "Card");
__turbopack_context__.k.register(_c2, "CardHeader$React.forwardRef");
__turbopack_context__.k.register(_c3, "CardHeader");
__turbopack_context__.k.register(_c4, "CardTitle$React.forwardRef");
__turbopack_context__.k.register(_c5, "CardTitle");
__turbopack_context__.k.register(_c6, "CardDescription$React.forwardRef");
__turbopack_context__.k.register(_c7, "CardDescription");
__turbopack_context__.k.register(_c8, "CardContent$React.forwardRef");
__turbopack_context__.k.register(_c9, "CardContent");
__turbopack_context__.k.register(_c10, "CardFooter$React.forwardRef");
__turbopack_context__.k.register(_c11, "CardFooter");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/ui/Badge.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "Badge",
    ()=>Badge,
    "badgeVariants",
    ()=>badgeVariants
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$class$2d$variance$2d$authority$2f$dist$2f$index$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/class-variance-authority/dist/index.mjs [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-client] (ecmascript)");
;
;
;
;
const badgeVariants = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$class$2d$variance$2d$authority$2f$dist$2f$index$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cva"])('inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium transition-colors', {
    variants: {
        variant: {
            default: 'bg-gray-100 text-gray-800',
            cultural: 'bg-saffron-100 text-saffron-800',
            arts: 'bg-pink-100 text-pink-800',
            food: 'bg-orange-100 text-orange-800',
            business: 'bg-cyan-100 text-cyan-800',
            community: 'bg-purple-100 text-purple-800',
            featured: 'bg-green-100 text-green-800',
            new: 'bg-emerald-100 text-emerald-800',
            hot: 'bg-red-100 text-red-800'
        }
    },
    defaultVariants: {
        variant: 'default'
    }
});
/**
 * Badge Component
 *
 * Displays category labels, status indicators, and tags with color-coded variants.
 * Used for event categories, post types, and feature highlighting.
 *
 * @example
 * ```tsx
 * <Badge variant="cultural">Cultural</Badge>
 * <Badge variant="hot">Hot Deal</Badge>
 * ```
 *
 * Variants:
 * - default: Gray background (general use)
 * - cultural: Saffron color (cultural events)
 * - arts: Pink color (arts and performances)
 * - food: Orange color (food and culinary)
 * - business: Cyan color (business listings)
 * - community: Purple color (community events)
 * - featured: Green color (featured items)
 * - new: Emerald color (new items)
 * - hot: Red color (hot deals/trending)
 *
 * Phase: 6C.1 - Landing Page Redesign
 */ const Badge = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["forwardRef"](_c = ({ className, variant, ...props }, ref)=>{
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])(badgeVariants({
            variant
        }), className),
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Badge.tsx",
        lineNumber: 59,
        columnNumber: 7
    }, ("TURBOPACK compile-time value", void 0));
});
_c1 = Badge;
Badge.displayName = 'Badge';
;
var _c, _c1;
__turbopack_context__.k.register(_c, "Badge$React.forwardRef");
__turbopack_context__.k.register(_c1, "Badge");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/infrastructure/api/repositories/events.repository.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "EventsRepository",
    ()=>EventsRepository,
    "eventsRepository",
    ()=>eventsRepository
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$build$2f$polyfills$2f$process$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = /*#__PURE__*/ __turbopack_context__.i("[project]/node_modules/next/dist/build/polyfills/process.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/client/api-client.ts [app-client] (ecmascript)");
;
class EventsRepository {
    basePath = '/events';
    // ==================== PUBLIC QUERIES ====================
    /**
   * Get all events with optional filtering and location-based sorting
   * Maps to backend GetEventsQuery
   *
   * Location-based sorting:
   * - For authenticated users: Pass userId to sort by preferred metros or home location
   * - For anonymous users: Pass latitude + longitude to sort by coordinates
   * - For specific metro filter: Pass metroAreaIds
   */ async getEvents(filters = {}) {
        const params = new URLSearchParams();
        // Traditional filters
        if (filters.status !== undefined) params.append('status', String(filters.status));
        if (filters.category !== undefined) params.append('category', String(filters.category));
        if (filters.startDateFrom) params.append('startDateFrom', filters.startDateFrom);
        if (filters.startDateTo) params.append('startDateTo', filters.startDateTo);
        if (filters.isFreeOnly !== undefined) params.append('isFreeOnly', String(filters.isFreeOnly));
        if (filters.city) params.append('city', filters.city);
        // NEW: Location-based sorting parameters
        if (filters.state) params.append('state', filters.state);
        if (filters.userId) params.append('userId', filters.userId);
        if (filters.latitude !== undefined) params.append('latitude', String(filters.latitude));
        if (filters.longitude !== undefined) params.append('longitude', String(filters.longitude));
        if (filters.metroAreaIds && filters.metroAreaIds.length > 0) {
            // Add each metro area ID as a separate query parameter
            filters.metroAreaIds.forEach((id)=>params.append('metroAreaIds', id));
        }
        const queryString = params.toString();
        const url = queryString ? `${this.basePath}?${queryString}` : this.basePath;
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].get(url);
    }
    /**
   * Get event by ID
   * Maps to backend GetEventByIdQuery
   */ async getEventById(id) {
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/${id}`);
    }
    /**
   * Search events using full-text search (PostgreSQL FTS)
   * Returns paginated results with relevance scores
   */ async searchEvents(request) {
        const params = new URLSearchParams({
            searchTerm: request.searchTerm,
            page: String(request.page ?? 1),
            pageSize: String(request.pageSize ?? 20)
        });
        if (request.category !== undefined) params.append('category', String(request.category));
        if (request.isFreeOnly !== undefined) params.append('isFreeOnly', String(request.isFreeOnly));
        if (request.startDateFrom) params.append('startDateFrom', request.startDateFrom);
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/search?${params.toString()}`);
    }
    /**
   * Get nearby events using geospatial query
   * Maps to backend GetNearbyEventsQuery
   */ async getNearbyEvents(request) {
        const params = new URLSearchParams({
            latitude: String(request.latitude),
            longitude: String(request.longitude),
            radiusKm: String(request.radiusKm)
        });
        if (request.category !== undefined) params.append('category', String(request.category));
        if (request.isFreeOnly !== undefined) params.append('isFreeOnly', String(request.isFreeOnly));
        if (request.startDateFrom) params.append('startDateFrom', request.startDateFrom);
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/nearby?${params.toString()}`);
    }
    /**
   * Get featured events for landing page
   * Returns up to 4 events sorted by location relevance
   * For authenticated users: Uses preferred metro areas
   * For anonymous users: Uses provided coordinates or default location
   */ async getFeaturedEvents(userId, latitude, longitude) {
        const params = new URLSearchParams();
        if (userId) params.append('userId', userId);
        if (latitude !== undefined) params.append('latitude', String(latitude));
        if (longitude !== undefined) params.append('longitude', String(longitude));
        const queryString = params.toString();
        const url = queryString ? `${this.basePath}/featured?${queryString}` : `${this.basePath}/featured`;
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].get(url);
    }
    // ==================== AUTHENTICATED QUERIES ====================
    // ==================== AUTHENTICATED MUTATIONS ====================
    /**
   * Create a new event
   * Requires authentication
   * Maps to backend CreateEventCommand
   * Backend returns the event ID as a plain JSON string
   */ async createEvent(data) {
        // Backend returns event ID as a plain JSON string (e.g., "40b297c9-2867-4f6b-900c-b5d0f230efe8")
        const eventId = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(this.basePath, data);
        return eventId;
    }
    /**
   * Update an existing event
   * Requires authentication and ownership
   * Maps to backend UpdateEventCommand
   */ async updateEvent(id, data) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].put(`${this.basePath}/${id}`, data);
    }
    /**
   * Delete an event
   * Requires authentication and ownership
   * Only allowed for Draft/Cancelled events
   */ async deleteEvent(id) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].delete(`${this.basePath}/${id}`);
    }
    /**
   * Submit event for approval (if approval workflow is enabled)
   */ async submitForApproval(id) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${id}/submit`);
    }
    /**
   * Publish event (make it visible to public)
   * Requires authentication and ownership
   */ async publishEvent(id) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${id}/publish`);
    }
    /**
   * Cancel event with reason
   * Notifies all registered users
   */ async cancelEvent(id, reason) {
        const request = {
            reason
        };
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${id}/cancel`, request);
    }
    /**
   * Postpone event with reason
   * Changes status to Postponed
   */ async postponeEvent(id, reason) {
        const request = {
            reason
        };
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${id}/postpone`, request);
    }
    // ==================== RSVP OPERATIONS ====================
    /**
   * RSVP to an event
   * Creates a registration for the user
   * Maps to backend RsvpToEventCommand
   * NOTE: Backend RsvpRequest only needs userId and quantity (eventId is in URL path)
   */ async rsvpToEvent(eventId, userId, quantity = 1) {
        const request = {
            userId,
            quantity
        };
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/rsvp`, request);
    }
    /**
   * Cancel RSVP
   * Removes registration and frees up capacity
   */ async cancelRsvp(eventId) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].delete(`${this.basePath}/${eventId}/rsvp`);
    }
    /**
   * Update RSVP quantity
   * Changes number of attendees for registration
   */ async updateRsvp(eventId, userId, newQuantity) {
        const request = {
            userId,
            newQuantity
        };
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].put(`${this.basePath}/${eventId}/rsvp`, request);
    }
    /**
   * Register anonymous attendee for an event
   * No authentication required - for users without accounts
   * Maps to backend RegisterAnonymousAttendeeCommand
   */ async registerAnonymous(eventId, request) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/register-anonymous`, request);
    }
    /**
   * Get current user's RSVPs
   * Epic 1: Backend now returns full EventDto[] instead of RsvpDto[] for better UX
   * Returns all events user has registered for
   */ async getUserRsvps() {
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/my-rsvps`);
    }
    /**
   * Get upcoming events for user
   * Returns events happening in the future
   */ async getUpcomingEvents() {
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/upcoming`);
    }
    /**
   * Get events created by current user
   * Returns all events user has created as organizer
   */ async getUserCreatedEvents() {
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/my-events`);
    }
    // ==================== WAITING LIST ====================
    /**
   * Add user to waiting list
   * Used when event is at capacity
   */ async addToWaitingList(eventId) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/waiting-list`);
    }
    /**
   * Remove user from waiting list
   */ async removeFromWaitingList(eventId) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].delete(`${this.basePath}/${eventId}/waiting-list`);
    }
    /**
   * Get waiting list for event
   * Returns list of users waiting for spots
   */ async getWaitingList(eventId) {
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/${eventId}/waiting-list`);
    }
    // ==================== SIGN-UP MANAGEMENT ====================
    /**
   * Get all sign-up lists for an event
   * Returns sign-up lists with commitments
   * Maps to backend GET /api/events/{id}/signups
   */ async getEventSignUpLists(eventId) {
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/${eventId}/signups`);
    }
    /**
   * Add a sign-up list to event
   * Organizer-only operation
   * Maps to backend POST /api/events/{id}/signups
   */ async addSignUpList(eventId, request) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/signups`, request);
    }
    /**
   * Remove a sign-up list from event
   * Organizer-only operation
   * Maps to backend DELETE /api/events/{eventId}/signups/{signupId}
   */ async removeSignUpList(eventId, signupId) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].delete(`${this.basePath}/${eventId}/signups/${signupId}`);
    }
    /**
   * Commit to bringing an item to event
   * User commits to sign-up list
   * Maps to backend POST /api/events/{eventId}/signups/{signupId}/commit
   */ async commitToSignUp(eventId, signupId, request) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/signups/${signupId}/commit`, request);
    }
    /**
   * Cancel user's commitment to sign-up list
   * Maps to backend DELETE /api/events/{eventId}/signups/{signupId}/commit
   */ async cancelCommitment(eventId, signupId, request) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].delete(`${this.basePath}/${eventId}/signups/${signupId}/commit`, {
            data: request
        });
    }
    // ==================== CATEGORY-BASED SIGN-UP MANAGEMENT ====================
    /**
   * Add a category-based sign-up list to event
   * Organizer-only operation
   * Maps to backend POST /api/events/{id}/signups/categories
   */ async addSignUpListWithCategories(eventId, request) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/signups/categories`, request);
    }
    /**
   * Add an item to a category-based sign-up list
   * Organizer-only operation
   * Maps to backend POST /api/events/{eventId}/signups/{signupId}/items
   */ async addSignUpItem(eventId, signupId, request) {
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/signups/${signupId}/items`, request);
    }
    /**
   * Remove an item from a category-based sign-up list
   * Organizer-only operation
   * Maps to backend DELETE /api/events/{eventId}/signups/{signupId}/items/{itemId}
   */ async removeSignUpItem(eventId, signupId, itemId) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].delete(`${this.basePath}/${eventId}/signups/${signupId}/items/${itemId}`);
    }
    /**
   * User commits to bringing a specific item
   * Maps to backend POST /api/events/{eventId}/signups/{signupId}/items/{itemId}/commit
   */ async commitToSignUpItem(eventId, signupId, itemId, request) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/signups/${signupId}/items/${itemId}/commit`, request);
    }
    // ==================== MEDIA OPERATIONS ====================
    /**
   * Upload image to event gallery
   * Uses multipart/form-data for file upload
   */ async uploadEventImage(eventId, file) {
        const formData = new FormData();
        formData.append('image', file);
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].postMultipart(`${this.basePath}/${eventId}/images`, formData);
    }
    /**
   * Delete image from event gallery
   */ async deleteEventImage(eventId, imageId) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].delete(`${this.basePath}/${eventId}/images/${imageId}`);
    }
    // ==================== UTILITY OPERATIONS ====================
    /**
   * Export event as ICS calendar file
   * Returns blob for download
   */ async getEventIcs(eventId) {
        // Note: This endpoint returns a file, not JSON
        // Using fetch directly instead of apiClient
        const baseURL = ("TURBOPACK compile-time value", "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api") || 'http://localhost:5000/api';
        const response = await fetch(`${baseURL}${this.basePath}/${eventId}/ics`);
        if (!response.ok) {
            throw new Error('Failed to download ICS file');
        }
        return await response.blob();
    }
    /**
   * Record social share for analytics
   * Tracks event sharing on social media
   */ async recordEventShare(eventId, platform) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/share`, {
            platform
        });
    }
    // ==================== MEDIA MANAGEMENT ====================
    /**
   * Upload an image to an event
   * Maps to backend POST /api/events/{id}/images
   *
   * @param eventId - Event ID (GUID)
   * @param file - Image file to upload (max 10MB, jpg/png/gif/webp)
   * @returns EventImageDto with image metadata
   */ async uploadEventImage(eventId, file) {
        const formData = new FormData();
        formData.append('image', file);
        // Using fetch directly for multipart/form-data
        const baseURL = ("TURBOPACK compile-time value", "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api") || 'http://localhost:5000/api';
        const response = await fetch(`${baseURL}${this.basePath}/${eventId}/images`, {
            method: 'POST',
            body: formData,
            credentials: 'include'
        });
        if (!response.ok) {
            const error = await response.json().catch(()=>({
                    message: 'Upload failed'
                }));
            throw new Error(error.message || `Upload failed with status ${response.status}`);
        }
        return await response.json();
    }
    /**
   * Delete an image from an event
   * Maps to backend DELETE /api/events/{eventId}/images/{imageId}
   *
   * @param eventId - Event ID (GUID)
   * @param imageId - Image ID (GUID)
   */ async deleteEventImage(eventId, imageId) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].delete(`${this.basePath}/${eventId}/images/${imageId}`);
    }
    /**
   * Replace an existing event image
   * Maps to backend PUT /api/events/{eventId}/images/{imageId}
   *
   * @param eventId - Event ID (GUID)
   * @param imageId - Image ID (GUID) to replace
   * @param file - New image file
   * @returns Updated EventImageDto
   */ async replaceEventImage(eventId, imageId, file) {
        const formData = new FormData();
        formData.append('image', file);
        const baseURL = ("TURBOPACK compile-time value", "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api") || 'http://localhost:5000/api';
        const response = await fetch(`${baseURL}${this.basePath}/${eventId}/images/${imageId}`, {
            method: 'PUT',
            body: formData,
            credentials: 'include'
        });
        if (!response.ok) {
            const error = await response.json().catch(()=>({
                    message: 'Replace failed'
                }));
            throw new Error(error.message || `Replace failed with status ${response.status}`);
        }
        return await response.json();
    }
    /**
   * Reorder event images
   * Maps to backend PUT /api/events/{id}/images/reorder
   *
   * @param eventId - Event ID (GUID)
   * @param newOrders - Map of image ID to new display order (1-indexed)
   */ async reorderEventImages(eventId, newOrders) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].put(`${this.basePath}/${eventId}/images/reorder`, {
            newOrders
        });
    }
}
const eventsRepository = new EventsRepository();
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/hooks/useEvents.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

/**
 * Events React Query Hooks
 *
 * Provides React Query hooks for Events API integration
 * Implements caching, optimistic updates, and proper error handling
 *
 * PREREQUISITES:
 * - events.repository.ts must be created in infrastructure/repositories/
 * - events.types.ts must be created in infrastructure/api/types/
 *
 * @requires @tanstack/react-query
 * @requires eventsRepository from infrastructure/repositories/events.repository
 * @requires Event types from infrastructure/api/types/events.types
 */ __turbopack_context__.s([
    "default",
    ()=>__TURBOPACK__default__export__,
    "eventKeys",
    ()=>eventKeys,
    "useCreateEvent",
    ()=>useCreateEvent,
    "useDeleteEvent",
    ()=>useDeleteEvent,
    "useEventById",
    ()=>useEventById,
    "useEvents",
    ()=>useEvents,
    "useFeaturedEvents",
    ()=>useFeaturedEvents,
    "useInvalidateEvents",
    ()=>useInvalidateEvents,
    "usePrefetchEvent",
    ()=>usePrefetchEvent,
    "useRsvpToEvent",
    ()=>useRsvpToEvent,
    "useSearchEvents",
    ()=>useSearchEvents,
    "useUpdateEvent",
    ()=>useUpdateEvent
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/useQuery.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/useMutation.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/QueryClientProvider.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/repositories/events.repository.ts [app-client] (ecmascript)");
var _s = __turbopack_context__.k.signature(), _s1 = __turbopack_context__.k.signature(), _s2 = __turbopack_context__.k.signature(), _s3 = __turbopack_context__.k.signature(), _s4 = __turbopack_context__.k.signature(), _s5 = __turbopack_context__.k.signature(), _s6 = __turbopack_context__.k.signature(), _s7 = __turbopack_context__.k.signature(), _s8 = __turbopack_context__.k.signature(), _s9 = __turbopack_context__.k.signature();
;
;
const eventKeys = {
    all: [
        'events'
    ],
    lists: ()=>[
            ...eventKeys.all,
            'list'
        ],
    list: (filters)=>[
            ...eventKeys.lists(),
            filters
        ],
    details: ()=>[
            ...eventKeys.all,
            'detail'
        ],
    detail: (id)=>[
            ...eventKeys.details(),
            id
        ],
    search: (searchTerm)=>[
            ...eventKeys.all,
            'search',
            searchTerm
        ],
    featured: (userId, lat, lng)=>[
            ...eventKeys.all,
            'featured',
            {
                userId,
                lat,
                lng
            }
        ]
};
function useEvents(filters, options) {
    _s();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQuery"])({
        queryKey: eventKeys.list(filters || {}),
        queryFn: {
            "useEvents.useQuery": async ()=>{
                const result = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].getEvents(filters);
                return result;
            }
        }["useEvents.useQuery"],
        staleTime: 5 * 60 * 1000,
        refetchOnWindowFocus: true,
        retry: 1,
        ...options
    });
}
_s(useEvents, "4ZpngI1uv+Uo3WQHEZmTQ5FNM+k=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQuery"]
    ];
});
function useEventById(id, options) {
    _s1();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQuery"])({
        queryKey: eventKeys.detail(id || ''),
        queryFn: {
            "useEventById.useQuery": ()=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].getEventById(id)
        }["useEventById.useQuery"],
        enabled: !!id,
        staleTime: 10 * 60 * 1000,
        refetchOnWindowFocus: true,
        ...options
    });
}
_s1(useEventById, "4ZpngI1uv+Uo3WQHEZmTQ5FNM+k=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQuery"]
    ];
});
function useSearchEvents(searchTerm, options) {
    _s2();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQuery"])({
        queryKey: eventKeys.search(searchTerm || ''),
        queryFn: {
            "useSearchEvents.useQuery": ()=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].searchEvents({
                    searchTerm: searchTerm,
                    page: 1,
                    pageSize: 20
                })
        }["useSearchEvents.useQuery"],
        enabled: !!searchTerm && searchTerm.length >= 2,
        staleTime: 2 * 60 * 1000,
        refetchOnWindowFocus: false,
        ...options
    });
}
_s2(useSearchEvents, "4ZpngI1uv+Uo3WQHEZmTQ5FNM+k=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQuery"]
    ];
});
function useFeaturedEvents(userId, latitude, longitude, options) {
    _s3();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQuery"])({
        queryKey: eventKeys.featured(userId, latitude, longitude),
        queryFn: {
            "useFeaturedEvents.useQuery": ()=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].getFeaturedEvents(userId, latitude, longitude)
        }["useFeaturedEvents.useQuery"],
        staleTime: 5 * 60 * 1000,
        refetchOnWindowFocus: true,
        retry: 1,
        ...options
    });
}
_s3(useFeaturedEvents, "4ZpngI1uv+Uo3WQHEZmTQ5FNM+k=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQuery"]
    ];
});
function useCreateEvent() {
    _s4();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: {
            "useCreateEvent.useMutation": (data)=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].createEvent(data)
        }["useCreateEvent.useMutation"],
        onSuccess: {
            "useCreateEvent.useMutation": ()=>{
                // Invalidate all event lists to refetch with new event
                queryClient.invalidateQueries({
                    queryKey: eventKeys.lists()
                });
            }
        }["useCreateEvent.useMutation"]
    });
}
_s4(useCreateEvent, "YK0wzM21ECnncaq5SECwU+/SVdQ=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"],
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"]
    ];
});
function useUpdateEvent() {
    _s5();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: {
            "useUpdateEvent.useMutation": ({ id, ...data })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].updateEvent(id, data)
        }["useUpdateEvent.useMutation"],
        onMutate: {
            "useUpdateEvent.useMutation": async ({ id, ...newData })=>{
                // Cancel outgoing refetches
                await queryClient.cancelQueries({
                    queryKey: eventKeys.detail(id)
                });
                // Snapshot previous value for rollback
                const previousEvent = queryClient.getQueryData(eventKeys.detail(id));
                // Optimistically update
                queryClient.setQueryData(eventKeys.detail(id), {
                    "useUpdateEvent.useMutation": (old)=>{
                        if (!old) return old;
                        return {
                            ...old,
                            ...newData
                        };
                    }
                }["useUpdateEvent.useMutation"]);
                return {
                    previousEvent
                };
            }
        }["useUpdateEvent.useMutation"],
        onError: {
            "useUpdateEvent.useMutation": (err, { id }, context)=>{
                // Rollback on error
                if (context?.previousEvent) {
                    queryClient.setQueryData(eventKeys.detail(id), context.previousEvent);
                }
            }
        }["useUpdateEvent.useMutation"],
        onSuccess: {
            "useUpdateEvent.useMutation": (_data, variables)=>{
                // Invalidate affected queries
                queryClient.invalidateQueries({
                    queryKey: eventKeys.detail(variables.id)
                });
                queryClient.invalidateQueries({
                    queryKey: eventKeys.lists()
                });
            }
        }["useUpdateEvent.useMutation"]
    });
}
_s5(useUpdateEvent, "YK0wzM21ECnncaq5SECwU+/SVdQ=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"],
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"]
    ];
});
function useDeleteEvent() {
    _s6();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: {
            "useDeleteEvent.useMutation": (id)=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].deleteEvent(id)
        }["useDeleteEvent.useMutation"],
        onMutate: {
            "useDeleteEvent.useMutation": async (id)=>{
                // Cancel queries
                await queryClient.cancelQueries({
                    queryKey: eventKeys.detail(id)
                });
                // Snapshot for rollback
                const previousEvent = queryClient.getQueryData(eventKeys.detail(id));
                // Remove from cache immediately
                queryClient.removeQueries({
                    queryKey: eventKeys.detail(id)
                });
                return {
                    previousEvent
                };
            }
        }["useDeleteEvent.useMutation"],
        onError: {
            "useDeleteEvent.useMutation": (err, id, context)=>{
                // Restore on error
                if (context?.previousEvent) {
                    queryClient.setQueryData(eventKeys.detail(id), context.previousEvent);
                }
            }
        }["useDeleteEvent.useMutation"],
        onSuccess: {
            "useDeleteEvent.useMutation": ()=>{
                // Invalidate lists
                queryClient.invalidateQueries({
                    queryKey: eventKeys.lists()
                });
            }
        }["useDeleteEvent.useMutation"]
    });
}
_s6(useDeleteEvent, "YK0wzM21ECnncaq5SECwU+/SVdQ=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"],
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"]
    ];
});
function useRsvpToEvent() {
    _s7();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: {
            "useRsvpToEvent.useMutation": (data)=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].rsvpToEvent(data.eventId, data.userId, data.quantity)
        }["useRsvpToEvent.useMutation"],
        onMutate: {
            "useRsvpToEvent.useMutation": async ({ eventId })=>{
                // Cancel queries
                await queryClient.cancelQueries({
                    queryKey: eventKeys.detail(eventId)
                });
                // Snapshot
                const previousEvent = queryClient.getQueryData(eventKeys.detail(eventId));
                // Optimistically update RSVP count
                queryClient.setQueryData(eventKeys.detail(eventId), {
                    "useRsvpToEvent.useMutation": (old)=>{
                        if (!old) return old;
                        return {
                            ...old,
                            currentRegistrations: old.currentRegistrations + 1
                        };
                    }
                }["useRsvpToEvent.useMutation"]);
                return {
                    previousEvent
                };
            }
        }["useRsvpToEvent.useMutation"],
        onError: {
            "useRsvpToEvent.useMutation": (err, { eventId }, context)=>{
                // Rollback
                if (context?.previousEvent) {
                    queryClient.setQueryData(eventKeys.detail(eventId), context.previousEvent);
                }
            }
        }["useRsvpToEvent.useMutation"],
        onSuccess: {
            "useRsvpToEvent.useMutation": (_data, variables)=>{
                // Refetch to get accurate data from server
                queryClient.invalidateQueries({
                    queryKey: eventKeys.detail(variables.eventId)
                });
            }
        }["useRsvpToEvent.useMutation"]
    });
}
_s7(useRsvpToEvent, "YK0wzM21ECnncaq5SECwU+/SVdQ=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"],
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"]
    ];
});
function usePrefetchEvent() {
    _s8();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (id)=>{
        queryClient.prefetchQuery({
            queryKey: eventKeys.detail(id),
            queryFn: ()=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].getEventById(id),
            staleTime: 10 * 60 * 1000
        });
    };
}
_s8(usePrefetchEvent, "4R+oYVB2Uc11P7bp1KcuhpkfaTw=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"]
    ];
});
function useInvalidateEvents() {
    _s9();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return {
        all: ()=>queryClient.invalidateQueries({
                queryKey: eventKeys.all
            }),
        lists: ()=>queryClient.invalidateQueries({
                queryKey: eventKeys.lists()
            }),
        detail: (id)=>queryClient.invalidateQueries({
                queryKey: eventKeys.detail(id)
            })
    };
}
_s9(useInvalidateEvents, "4R+oYVB2Uc11P7bp1KcuhpkfaTw=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"]
    ];
});
const __TURBOPACK__default__export__ = {
    useEvents,
    useEventById,
    useSearchEvents,
    useFeaturedEvents,
    useCreateEvent,
    useUpdateEvent,
    useDeleteEvent,
    useRsvpToEvent,
    usePrefetchEvent,
    useInvalidateEvents
};
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/hooks/useEventSignUps.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

/**
 * Event Sign-Up Management React Query Hooks
 *
 * Provides React Query hooks for Event Sign-Up Lists API integration
 * Implements caching, optimistic updates, and proper error handling
 *
 * @requires @tanstack/react-query
 * @requires eventsRepository from infrastructure/repositories/events.repository
 * @requires SignUp types from infrastructure/api/types/events.types
 */ __turbopack_context__.s([
    "default",
    ()=>__TURBOPACK__default__export__,
    "signUpKeys",
    ()=>signUpKeys,
    "useAddSignUpItem",
    ()=>useAddSignUpItem,
    "useAddSignUpList",
    ()=>useAddSignUpList,
    "useAddSignUpListWithCategories",
    ()=>useAddSignUpListWithCategories,
    "useCancelCommitment",
    ()=>useCancelCommitment,
    "useCommitToSignUp",
    ()=>useCommitToSignUp,
    "useCommitToSignUpItem",
    ()=>useCommitToSignUpItem,
    "useEventSignUps",
    ()=>useEventSignUps,
    "useRemoveSignUpItem",
    ()=>useRemoveSignUpItem,
    "useRemoveSignUpList",
    ()=>useRemoveSignUpList
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/useQuery.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/useMutation.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/QueryClientProvider.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/repositories/events.repository.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEvents$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/hooks/useEvents.ts [app-client] (ecmascript)");
var _s = __turbopack_context__.k.signature(), _s1 = __turbopack_context__.k.signature(), _s2 = __turbopack_context__.k.signature(), _s3 = __turbopack_context__.k.signature(), _s4 = __turbopack_context__.k.signature(), _s5 = __turbopack_context__.k.signature(), _s6 = __turbopack_context__.k.signature(), _s7 = __turbopack_context__.k.signature(), _s8 = __turbopack_context__.k.signature();
;
;
;
const signUpKeys = {
    all: [
        'signups'
    ],
    lists: ()=>[
            ...signUpKeys.all,
            'list'
        ],
    list: (eventId)=>[
            ...signUpKeys.lists(),
            eventId
        ]
};
function useEventSignUps(eventId, options) {
    _s();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQuery"])({
        queryKey: signUpKeys.list(eventId || ''),
        queryFn: {
            "useEventSignUps.useQuery": ()=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].getEventSignUpLists(eventId)
        }["useEventSignUps.useQuery"],
        enabled: !!eventId,
        staleTime: 5 * 60 * 1000,
        refetchOnWindowFocus: true,
        retry: 1,
        ...options
    });
}
_s(useEventSignUps, "4ZpngI1uv+Uo3WQHEZmTQ5FNM+k=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQuery"]
    ];
});
function useAddSignUpList() {
    _s1();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: {
            "useAddSignUpList.useMutation": ({ eventId, ...data })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].addSignUpList(eventId, data)
        }["useAddSignUpList.useMutation"],
        onSuccess: {
            "useAddSignUpList.useMutation": (_data, variables)=>{
                // Invalidate sign-up lists for this event
                queryClient.invalidateQueries({
                    queryKey: signUpKeys.list(variables.eventId)
                });
                // Invalidate event detail to reflect updated event
                queryClient.invalidateQueries({
                    queryKey: __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEvents$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventKeys"].detail(variables.eventId)
                });
            }
        }["useAddSignUpList.useMutation"]
    });
}
_s1(useAddSignUpList, "YK0wzM21ECnncaq5SECwU+/SVdQ=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"],
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"]
    ];
});
function useRemoveSignUpList() {
    _s2();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: {
            "useRemoveSignUpList.useMutation": ({ eventId, signupId })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].removeSignUpList(eventId, signupId)
        }["useRemoveSignUpList.useMutation"],
        onMutate: {
            "useRemoveSignUpList.useMutation": async ({ eventId, signupId })=>{
                // Cancel queries
                await queryClient.cancelQueries({
                    queryKey: signUpKeys.list(eventId)
                });
                // Snapshot for rollback
                const previousSignUps = queryClient.getQueryData(signUpKeys.list(eventId));
                // Optimistically remove from cache
                queryClient.setQueryData(signUpKeys.list(eventId), {
                    "useRemoveSignUpList.useMutation": (old)=>old?.filter({
                            "useRemoveSignUpList.useMutation": (s)=>s.id !== signupId
                        }["useRemoveSignUpList.useMutation"]) || []
                }["useRemoveSignUpList.useMutation"]);
                return {
                    previousSignUps
                };
            }
        }["useRemoveSignUpList.useMutation"],
        onError: {
            "useRemoveSignUpList.useMutation": (err, { eventId }, context)=>{
                // Rollback on error
                if (context?.previousSignUps) {
                    queryClient.setQueryData(signUpKeys.list(eventId), context.previousSignUps);
                }
            }
        }["useRemoveSignUpList.useMutation"],
        onSuccess: {
            "useRemoveSignUpList.useMutation": (_data, variables)=>{
                // Invalidate to ensure consistency
                queryClient.invalidateQueries({
                    queryKey: signUpKeys.list(variables.eventId)
                });
                queryClient.invalidateQueries({
                    queryKey: __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEvents$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventKeys"].detail(variables.eventId)
                });
            }
        }["useRemoveSignUpList.useMutation"]
    });
}
_s2(useRemoveSignUpList, "YK0wzM21ECnncaq5SECwU+/SVdQ=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"],
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"]
    ];
});
function useCommitToSignUp() {
    _s3();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: {
            "useCommitToSignUp.useMutation": ({ eventId, signupId, ...data })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].commitToSignUp(eventId, signupId, data)
        }["useCommitToSignUp.useMutation"],
        onMutate: {
            "useCommitToSignUp.useMutation": async ({ eventId, signupId, userId, itemDescription, quantity })=>{
                // Cancel queries
                await queryClient.cancelQueries({
                    queryKey: signUpKeys.list(eventId)
                });
                // Snapshot for rollback
                const previousSignUps = queryClient.getQueryData(signUpKeys.list(eventId));
                // Optimistically add commitment
                queryClient.setQueryData(signUpKeys.list(eventId), {
                    "useCommitToSignUp.useMutation": (old)=>{
                        if (!old) return old;
                        return old.map({
                            "useCommitToSignUp.useMutation": (signUp)=>{
                                if (signUp.id !== signupId) return signUp;
                                return {
                                    ...signUp,
                                    commitments: [
                                        ...signUp.commitments,
                                        {
                                            id: `temp-${Date.now()}`,
                                            userId,
                                            itemDescription,
                                            quantity,
                                            committedAt: new Date().toISOString()
                                        }
                                    ],
                                    commitmentCount: signUp.commitmentCount + 1
                                };
                            }
                        }["useCommitToSignUp.useMutation"]);
                    }
                }["useCommitToSignUp.useMutation"]);
                return {
                    previousSignUps
                };
            }
        }["useCommitToSignUp.useMutation"],
        onError: {
            "useCommitToSignUp.useMutation": (err, { eventId }, context)=>{
                // Rollback on error
                if (context?.previousSignUps) {
                    queryClient.setQueryData(signUpKeys.list(eventId), context.previousSignUps);
                }
            }
        }["useCommitToSignUp.useMutation"],
        onSuccess: {
            "useCommitToSignUp.useMutation": (_data, variables)=>{
                // Refetch to get accurate data from server (with real IDs)
                queryClient.invalidateQueries({
                    queryKey: signUpKeys.list(variables.eventId)
                });
            }
        }["useCommitToSignUp.useMutation"]
    });
}
_s3(useCommitToSignUp, "YK0wzM21ECnncaq5SECwU+/SVdQ=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"],
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"]
    ];
});
function useCancelCommitment() {
    _s4();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: {
            "useCancelCommitment.useMutation": ({ eventId, signupId, ...data })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].cancelCommitment(eventId, signupId, data)
        }["useCancelCommitment.useMutation"],
        onMutate: {
            "useCancelCommitment.useMutation": async ({ eventId, signupId, userId })=>{
                // Cancel queries
                await queryClient.cancelQueries({
                    queryKey: signUpKeys.list(eventId)
                });
                // Snapshot for rollback
                const previousSignUps = queryClient.getQueryData(signUpKeys.list(eventId));
                // Optimistically remove commitment
                queryClient.setQueryData(signUpKeys.list(eventId), {
                    "useCancelCommitment.useMutation": (old)=>{
                        if (!old) return old;
                        return old.map({
                            "useCancelCommitment.useMutation": (signUp)=>{
                                if (signUp.id !== signupId) return signUp;
                                return {
                                    ...signUp,
                                    commitments: signUp.commitments.filter({
                                        "useCancelCommitment.useMutation": (c)=>c.userId !== userId
                                    }["useCancelCommitment.useMutation"]),
                                    commitmentCount: Math.max(0, signUp.commitmentCount - 1)
                                };
                            }
                        }["useCancelCommitment.useMutation"]);
                    }
                }["useCancelCommitment.useMutation"]);
                return {
                    previousSignUps
                };
            }
        }["useCancelCommitment.useMutation"],
        onError: {
            "useCancelCommitment.useMutation": (err, { eventId }, context)=>{
                // Rollback on error
                if (context?.previousSignUps) {
                    queryClient.setQueryData(signUpKeys.list(eventId), context.previousSignUps);
                }
            }
        }["useCancelCommitment.useMutation"],
        onSuccess: {
            "useCancelCommitment.useMutation": (_data, variables)=>{
                // Refetch to ensure consistency
                queryClient.invalidateQueries({
                    queryKey: signUpKeys.list(variables.eventId)
                });
            }
        }["useCancelCommitment.useMutation"]
    });
}
_s4(useCancelCommitment, "YK0wzM21ECnncaq5SECwU+/SVdQ=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"],
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"]
    ];
});
function useAddSignUpListWithCategories() {
    _s5();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: {
            "useAddSignUpListWithCategories.useMutation": ({ eventId, ...data })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].addSignUpListWithCategories(eventId, data)
        }["useAddSignUpListWithCategories.useMutation"],
        onSuccess: {
            "useAddSignUpListWithCategories.useMutation": (_data, variables)=>{
                queryClient.invalidateQueries({
                    queryKey: signUpKeys.list(variables.eventId)
                });
                queryClient.invalidateQueries({
                    queryKey: __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEvents$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventKeys"].detail(variables.eventId)
                });
            }
        }["useAddSignUpListWithCategories.useMutation"]
    });
}
_s5(useAddSignUpListWithCategories, "YK0wzM21ECnncaq5SECwU+/SVdQ=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"],
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"]
    ];
});
function useAddSignUpItem() {
    _s6();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: {
            "useAddSignUpItem.useMutation": ({ eventId, signupId, ...data })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].addSignUpItem(eventId, signupId, data)
        }["useAddSignUpItem.useMutation"],
        onSuccess: {
            "useAddSignUpItem.useMutation": (_data, variables)=>{
                queryClient.invalidateQueries({
                    queryKey: signUpKeys.list(variables.eventId)
                });
            }
        }["useAddSignUpItem.useMutation"]
    });
}
_s6(useAddSignUpItem, "YK0wzM21ECnncaq5SECwU+/SVdQ=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"],
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"]
    ];
});
function useRemoveSignUpItem() {
    _s7();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: {
            "useRemoveSignUpItem.useMutation": ({ eventId, signupId, itemId })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].removeSignUpItem(eventId, signupId, itemId)
        }["useRemoveSignUpItem.useMutation"],
        onMutate: {
            "useRemoveSignUpItem.useMutation": async ({ eventId, signupId, itemId })=>{
                await queryClient.cancelQueries({
                    queryKey: signUpKeys.list(eventId)
                });
                const previousSignUps = queryClient.getQueryData(signUpKeys.list(eventId));
                // Optimistically remove item
                queryClient.setQueryData(signUpKeys.list(eventId), {
                    "useRemoveSignUpItem.useMutation": (old)=>{
                        if (!old) return old;
                        return old.map({
                            "useRemoveSignUpItem.useMutation": (signUp)=>{
                                if (signUp.id !== signupId) return signUp;
                                return {
                                    ...signUp,
                                    items: signUp.items.filter({
                                        "useRemoveSignUpItem.useMutation": (item)=>item.id !== itemId
                                    }["useRemoveSignUpItem.useMutation"])
                                };
                            }
                        }["useRemoveSignUpItem.useMutation"]);
                    }
                }["useRemoveSignUpItem.useMutation"]);
                return {
                    previousSignUps
                };
            }
        }["useRemoveSignUpItem.useMutation"],
        onError: {
            "useRemoveSignUpItem.useMutation": (err, { eventId }, context)=>{
                if (context?.previousSignUps) {
                    queryClient.setQueryData(signUpKeys.list(eventId), context.previousSignUps);
                }
            }
        }["useRemoveSignUpItem.useMutation"],
        onSuccess: {
            "useRemoveSignUpItem.useMutation": (_data, variables)=>{
                queryClient.invalidateQueries({
                    queryKey: signUpKeys.list(variables.eventId)
                });
            }
        }["useRemoveSignUpItem.useMutation"]
    });
}
_s7(useRemoveSignUpItem, "YK0wzM21ECnncaq5SECwU+/SVdQ=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"],
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"]
    ];
});
function useCommitToSignUpItem() {
    _s8();
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: {
            "useCommitToSignUpItem.useMutation": ({ eventId, signupId, itemId, ...data })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].commitToSignUpItem(eventId, signupId, itemId, data)
        }["useCommitToSignUpItem.useMutation"],
        onSuccess: {
            "useCommitToSignUpItem.useMutation": (_data, variables)=>{
                queryClient.invalidateQueries({
                    queryKey: signUpKeys.list(variables.eventId)
                });
            }
        }["useCommitToSignUpItem.useMutation"]
    });
}
_s8(useCommitToSignUpItem, "YK0wzM21ECnncaq5SECwU+/SVdQ=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useQueryClient"],
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useMutation"]
    ];
});
const __TURBOPACK__default__export__ = {
    useEventSignUps,
    useAddSignUpList,
    useRemoveSignUpList,
    useCommitToSignUp,
    useCancelCommitment,
    useAddSignUpListWithCategories,
    useAddSignUpItem,
    useRemoveSignUpItem,
    useCommitToSignUpItem
};
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/infrastructure/api/types/events.types.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

/**
 * Events API Type Definitions
 * DTOs matching backend API contracts (LankaConnect.Application.Events.Common)
 */ // ==================== Enums ====================
/**
 * Event status enum matching backend LankaConnect.Domain.Events.Enums.EventStatus
 */ __turbopack_context__.s([
    "Currency",
    ()=>Currency,
    "EventCategory",
    ()=>EventCategory,
    "EventStatus",
    ()=>EventStatus,
    "RegistrationStatus",
    ()=>RegistrationStatus,
    "SignUpItemCategory",
    ()=>SignUpItemCategory,
    "SignUpType",
    ()=>SignUpType
]);
var EventStatus = /*#__PURE__*/ function(EventStatus) {
    EventStatus[EventStatus["Draft"] = 0] = "Draft";
    EventStatus[EventStatus["Published"] = 1] = "Published";
    EventStatus[EventStatus["Active"] = 2] = "Active";
    EventStatus[EventStatus["Postponed"] = 3] = "Postponed";
    EventStatus[EventStatus["Cancelled"] = 4] = "Cancelled";
    EventStatus[EventStatus["Completed"] = 5] = "Completed";
    EventStatus[EventStatus["Archived"] = 6] = "Archived";
    EventStatus[EventStatus["UnderReview"] = 7] = "UnderReview";
    return EventStatus;
}({});
var EventCategory = /*#__PURE__*/ function(EventCategory) {
    EventCategory[EventCategory["Religious"] = 0] = "Religious";
    EventCategory[EventCategory["Cultural"] = 1] = "Cultural";
    EventCategory[EventCategory["Community"] = 2] = "Community";
    EventCategory[EventCategory["Educational"] = 3] = "Educational";
    EventCategory[EventCategory["Social"] = 4] = "Social";
    EventCategory[EventCategory["Business"] = 5] = "Business";
    EventCategory[EventCategory["Charity"] = 6] = "Charity";
    EventCategory[EventCategory["Entertainment"] = 7] = "Entertainment";
    return EventCategory;
}({});
var RegistrationStatus = /*#__PURE__*/ function(RegistrationStatus) {
    RegistrationStatus[RegistrationStatus["Pending"] = 0] = "Pending";
    RegistrationStatus[RegistrationStatus["Confirmed"] = 1] = "Confirmed";
    RegistrationStatus[RegistrationStatus["Waitlisted"] = 2] = "Waitlisted";
    RegistrationStatus[RegistrationStatus["CheckedIn"] = 3] = "CheckedIn";
    RegistrationStatus[RegistrationStatus["Completed"] = 4] = "Completed";
    RegistrationStatus[RegistrationStatus["Cancelled"] = 5] = "Cancelled";
    RegistrationStatus[RegistrationStatus["Refunded"] = 6] = "Refunded";
    return RegistrationStatus;
}({});
var Currency = /*#__PURE__*/ function(Currency) {
    Currency[Currency["USD"] = 1] = "USD";
    Currency[Currency["LKR"] = 2] = "LKR";
    Currency[Currency["GBP"] = 3] = "GBP";
    Currency[Currency["EUR"] = 4] = "EUR";
    Currency[Currency["CAD"] = 5] = "CAD";
    Currency[Currency["AUD"] = 6] = "AUD";
    return Currency;
}({});
var SignUpType = /*#__PURE__*/ function(SignUpType) {
    SignUpType[SignUpType["Open"] = 0] = "Open";
    SignUpType[SignUpType["Predefined"] = 1] = "Predefined";
    return SignUpType;
}({});
var SignUpItemCategory = /*#__PURE__*/ function(SignUpItemCategory) {
    SignUpItemCategory[SignUpItemCategory["Mandatory"] = 0] = "Mandatory";
    SignUpItemCategory[SignUpItemCategory["Preferred"] = 1] = "Preferred";
    SignUpItemCategory[SignUpItemCategory["Suggested"] = 2] = "Suggested";
    return SignUpItemCategory;
}({});
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/features/events/SignUpManagementSection.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

/**
 * SignUpManagementSection Component
 *
 * Complete UI for Event Sign-Up Management feature
 * Displays sign-up lists with commitments and allows users to commit or cancel
 *
 * Features:
 * - View all sign-up lists for an event
 * - See existing commitments from other users
 * - Commit to bringing items (authenticated users)
 * - Cancel commitments (own commitments only)
 * - Organizer can add/remove sign-up lists
 * - NEW: Category-based sign-ups (Mandatory, Preferred, Suggested)
 * - Backward compatible with legacy Open/Predefined sign-ups
 *
 * @requires useEventSignUps hook
 * @requires useCommitToSignUp, useCancelCommitment mutations
 * @requires useCommitToSignUpItem mutation (category-based)
 */ __turbopack_context__.s([
    "SignUpManagementSection",
    ()=>SignUpManagementSection,
    "default",
    ()=>__TURBOPACK__default__export__
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEventSignUps$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/hooks/useEventSignUps.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/types/events.types.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Card.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Button.tsx [app-client] (ecmascript)");
;
var _s = __turbopack_context__.k.signature();
'use client';
;
;
;
;
;
function SignUpManagementSection({ eventId, userId, isOrganizer = false }) {
    _s();
    const [commitDialogOpen, setCommitDialogOpen] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(false);
    const [selectedSignUpId, setSelectedSignUpId] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(null);
    const [itemDescription, setItemDescription] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])('');
    const [quantity, setQuantity] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(1);
    // Category-based sign-up state
    const [selectedItemId, setSelectedItemId] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(null);
    const [commitQuantity, setCommitQuantity] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(1);
    const [commitNotes, setCommitNotes] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])('');
    // Fetch sign-up lists
    const { data: signUpLists, isLoading, error } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEventSignUps$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useEventSignUps"])(eventId);
    // Mutations
    const commitToSignUp = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEventSignUps$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useCommitToSignUp"])();
    const cancelCommitment = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEventSignUps$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useCancelCommitment"])();
    const commitToSignUpItem = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEventSignUps$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useCommitToSignUpItem"])();
    // Handle commit to sign-up
    const handleCommit = async (signUpId)=>{
        if (!userId) {
            alert('Please log in to commit to items');
            return;
        }
        if (!itemDescription.trim()) {
            alert('Please enter an item description');
            return;
        }
        try {
            await commitToSignUp.mutateAsync({
                eventId,
                signupId: signUpId,
                userId,
                itemDescription: itemDescription.trim(),
                quantity
            });
            // Reset form
            setItemDescription('');
            setQuantity(1);
            setCommitDialogOpen(false);
            setSelectedSignUpId(null);
        } catch (err) {
            console.error('Failed to commit:', err);
            alert('Failed to commit. Please try again.');
        }
    };
    // Handle cancel commitment
    const handleCancel = async (signUpId)=>{
        if (!userId) {
            return;
        }
        if (!confirm('Are you sure you want to cancel your commitment?')) {
            return;
        }
        try {
            await cancelCommitment.mutateAsync({
                eventId,
                signupId: signUpId,
                userId
            });
        } catch (err) {
            console.error('Failed to cancel commitment:', err);
            alert('Failed to cancel commitment. Please try again.');
        }
    };
    // Handle commit to specific item (category-based)
    const handleCommitToItem = async (signUpId, itemId)=>{
        if (!userId) {
            alert('Please log in to commit to items');
            return;
        }
        if (commitQuantity < 1) {
            alert('Please enter a valid quantity');
            return;
        }
        try {
            await commitToSignUpItem.mutateAsync({
                eventId,
                signupId: signUpId,
                itemId,
                userId,
                quantity: commitQuantity,
                notes: commitNotes.trim() || undefined
            });
            // Reset form
            setSelectedItemId(null);
            setCommitQuantity(1);
            setCommitNotes('');
        } catch (err) {
            console.error('Failed to commit to item:', err);
            alert('Failed to commit. Please try again.');
        }
    };
    // Get category badge color
    const getCategoryColor = (category)=>{
        switch(category){
            case __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SignUpItemCategory"].Mandatory:
                return 'bg-red-100 text-red-800 border-red-300';
            case __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SignUpItemCategory"].Preferred:
                return 'bg-blue-100 text-blue-800 border-blue-300';
            case __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SignUpItemCategory"].Suggested:
                return 'bg-green-100 text-green-800 border-green-300';
            default:
                return 'bg-gray-100 text-gray-800 border-gray-300';
        }
    };
    // Get category label
    const getCategoryLabel = (category)=>{
        switch(category){
            case __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SignUpItemCategory"].Mandatory:
                return 'Mandatory';
            case __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SignUpItemCategory"].Preferred:
                return 'Preferred';
            case __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SignUpItemCategory"].Suggested:
                return 'Suggested';
            default:
                return 'Unknown';
        }
    };
    // Loading state
    if (isLoading) {
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "py-8",
            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                className: "text-center text-muted-foreground",
                children: "Loading sign-up lists..."
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                lineNumber: 196,
                columnNumber: 9
            }, this)
        }, void 0, false, {
            fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
            lineNumber: 195,
            columnNumber: 7
        }, this);
    }
    // Error state
    if (error) {
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "py-8",
            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                className: "text-center text-destructive",
                children: "Failed to load sign-up lists. Please try again."
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                lineNumber: 205,
                columnNumber: 9
            }, this)
        }, void 0, false, {
            fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
            lineNumber: 204,
            columnNumber: 7
        }, this);
    }
    // No sign-up lists
    if (!signUpLists || signUpLists.length === 0) {
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "py-8",
            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Card"], {
                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["CardHeader"], {
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["CardTitle"], {
                            children: "Sign-Up Lists"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                            lineNumber: 218,
                            columnNumber: 13
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["CardDescription"], {
                            children: [
                                "No sign-up lists for this event yet.",
                                isOrganizer && ' Create one to let attendees volunteer to bring items!'
                            ]
                        }, void 0, true, {
                            fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                            lineNumber: 219,
                            columnNumber: 13
                        }, this)
                    ]
                }, void 0, true, {
                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                    lineNumber: 217,
                    columnNumber: 11
                }, this)
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                lineNumber: 216,
                columnNumber: 9
            }, this)
        }, void 0, false, {
            fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
            lineNumber: 215,
            columnNumber: 7
        }, this);
    }
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        className: "space-y-6",
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h2", {
                className: "text-2xl font-bold",
                children: "Sign-Up Lists"
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                lineNumber: 231,
                columnNumber: 7
            }, this),
            signUpLists.map((signUpList)=>{
                // Check if current user has committed to this list
                const userCommitment = signUpList.commitments.find((c)=>c.userId === userId);
                // Check if this is a category-based sign-up (has items)
                const isCategoryBased = signUpList.items && signUpList.items.length > 0;
                return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Card"], {
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["CardHeader"], {
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["CardTitle"], {
                                    children: signUpList.category
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                    lineNumber: 243,
                                    columnNumber: 15
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["CardDescription"], {
                                    children: signUpList.description
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                    lineNumber: 244,
                                    columnNumber: 15
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "text-sm text-muted-foreground",
                                    children: isCategoryBased ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "flex gap-2 flex-wrap mt-2",
                                        children: [
                                            signUpList.hasMandatoryItems && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                className: "px-2 py-1 rounded text-xs font-medium bg-red-100 text-red-800",
                                                children: "Has Mandatory Items"
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                lineNumber: 249,
                                                columnNumber: 23
                                            }, this),
                                            signUpList.hasPreferredItems && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                className: "px-2 py-1 rounded text-xs font-medium bg-blue-100 text-blue-800",
                                                children: "Has Preferred Items"
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                lineNumber: 254,
                                                columnNumber: 23
                                            }, this),
                                            signUpList.hasSuggestedItems && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                className: "px-2 py-1 rounded text-xs font-medium bg-green-100 text-green-800",
                                                children: "Has Suggested Items"
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                lineNumber: 259,
                                                columnNumber: 23
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                        lineNumber: 247,
                                        columnNumber: 19
                                    }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                        children: [
                                            "Type: ",
                                            signUpList.signUpType === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SignUpType"].Predefined ? 'Predefined Items' : 'Open'
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                        lineNumber: 265,
                                        columnNumber: 19
                                    }, this)
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                    lineNumber: 245,
                                    columnNumber: 15
                                }, this)
                            ]
                        }, void 0, true, {
                            fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                            lineNumber: 242,
                            columnNumber: 13
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["CardContent"], {
                            children: isCategoryBased ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "space-y-6",
                                children: [
                                    __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SignUpItemCategory"].Mandatory,
                                    __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SignUpItemCategory"].Preferred,
                                    __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SignUpItemCategory"].Suggested
                                ].map((category)=>{
                                    const categoryItems = signUpList.items.filter((item)=>item.itemCategory === category);
                                    if (categoryItems.length === 0) return null;
                                    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "space-y-3",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h4", {
                                                className: "font-semibold flex items-center gap-2",
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                        className: `px-2 py-1 rounded text-xs font-medium border ${getCategoryColor(category)}`,
                                                        children: getCategoryLabel(category)
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                        lineNumber: 283,
                                                        columnNumber: 27
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                        className: "text-sm text-muted-foreground",
                                                        children: [
                                                            "(",
                                                            categoryItems.length,
                                                            " ",
                                                            categoryItems.length === 1 ? 'item' : 'items',
                                                            ")"
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                        lineNumber: 286,
                                                        columnNumber: 27
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                lineNumber: 282,
                                                columnNumber: 25
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "space-y-3",
                                                children: categoryItems.map((item)=>{
                                                    const userItemCommitment = item.commitments.find((c)=>c.userId === userId);
                                                    const remainingQty = item.remainingQuantity;
                                                    const percentCommitted = Math.round(item.committedQuantity / item.quantity * 100);
                                                    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        className: "border rounded-lg p-4 space-y-2",
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                className: "flex justify-between items-start",
                                                                children: [
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                        className: "flex-1",
                                                                        children: [
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                                className: "font-medium",
                                                                                children: item.itemDescription
                                                                            }, void 0, false, {
                                                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                                lineNumber: 301,
                                                                                columnNumber: 37
                                                                            }, this),
                                                                            item.notes && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                                className: "text-sm text-muted-foreground mt-1",
                                                                                children: item.notes
                                                                            }, void 0, false, {
                                                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                                lineNumber: 303,
                                                                                columnNumber: 39
                                                                            }, this)
                                                                        ]
                                                                    }, void 0, true, {
                                                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                        lineNumber: 300,
                                                                        columnNumber: 35
                                                                    }, this),
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                        className: "text-right ml-4",
                                                                        children: [
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                                className: `text-sm font-medium ${remainingQty === 0 ? 'text-green-600' : 'text-blue-600'}`,
                                                                                children: [
                                                                                    item.committedQuantity,
                                                                                    " of ",
                                                                                    item.quantity,
                                                                                    " committed"
                                                                                ]
                                                                            }, void 0, true, {
                                                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                                lineNumber: 307,
                                                                                columnNumber: 37
                                                                            }, this),
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                                className: "text-xs text-muted-foreground",
                                                                                children: [
                                                                                    remainingQty,
                                                                                    " remaining"
                                                                                ]
                                                                            }, void 0, true, {
                                                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                                lineNumber: 310,
                                                                                columnNumber: 37
                                                                            }, this)
                                                                        ]
                                                                    }, void 0, true, {
                                                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                        lineNumber: 306,
                                                                        columnNumber: 35
                                                                    }, this)
                                                                ]
                                                            }, void 0, true, {
                                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                lineNumber: 299,
                                                                columnNumber: 33
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                className: "w-full bg-gray-200 rounded-full h-2",
                                                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                    className: `h-2 rounded-full ${percentCommitted === 100 ? 'bg-green-500' : 'bg-blue-500'}`,
                                                                    style: {
                                                                        width: `${percentCommitted}%`
                                                                    }
                                                                }, void 0, false, {
                                                                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                    lineNumber: 318,
                                                                    columnNumber: 35
                                                                }, this)
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                lineNumber: 317,
                                                                columnNumber: 33
                                                            }, this),
                                                            item.commitments.length > 0 && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                className: "space-y-1 mt-2",
                                                                children: [
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                        className: "text-xs font-medium text-muted-foreground",
                                                                        children: "Commitments:"
                                                                    }, void 0, false, {
                                                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                        lineNumber: 329,
                                                                        columnNumber: 37
                                                                    }, this),
                                                                    item.commitments.map((commitment)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                            className: "text-sm bg-muted p-2 rounded",
                                                                            children: [
                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                                    className: "flex justify-between items-center",
                                                                                    children: [
                                                                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                                                            children: [
                                                                                                "Quantity: ",
                                                                                                commitment.quantity
                                                                                            ]
                                                                                        }, void 0, true, {
                                                                                            fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                                            lineNumber: 333,
                                                                                            columnNumber: 43
                                                                                        }, this),
                                                                                        commitment.userId === userId && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                                                            className: "text-xs text-blue-600 font-medium",
                                                                                            children: "(You)"
                                                                                        }, void 0, false, {
                                                                                            fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                                            lineNumber: 335,
                                                                                            columnNumber: 45
                                                                                        }, this)
                                                                                    ]
                                                                                }, void 0, true, {
                                                                                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                                    lineNumber: 332,
                                                                                    columnNumber: 41
                                                                                }, this),
                                                                                commitment.notes && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                                    className: "text-xs text-muted-foreground mt-1",
                                                                                    children: [
                                                                                        "Note: ",
                                                                                        commitment.notes
                                                                                    ]
                                                                                }, void 0, true, {
                                                                                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                                    lineNumber: 339,
                                                                                    columnNumber: 43
                                                                                }, this)
                                                                            ]
                                                                        }, commitment.id, true, {
                                                                            fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                            lineNumber: 331,
                                                                            columnNumber: 39
                                                                        }, this))
                                                                ]
                                                            }, void 0, true, {
                                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                lineNumber: 328,
                                                                columnNumber: 35
                                                            }, this),
                                                            !userItemCommitment && userId && remainingQty > 0 && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                className: "mt-3",
                                                                children: selectedItemId === item.id ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                    className: "space-y-2",
                                                                    children: [
                                                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                            children: [
                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                                                    className: "block text-sm font-medium mb-1",
                                                                                    children: [
                                                                                        "Quantity (max: ",
                                                                                        remainingQty,
                                                                                        ")"
                                                                                    ]
                                                                                }, void 0, true, {
                                                                                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                                    lineNumber: 352,
                                                                                    columnNumber: 43
                                                                                }, this),
                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
                                                                                    type: "number",
                                                                                    min: "1",
                                                                                    max: remainingQty,
                                                                                    value: commitQuantity,
                                                                                    onChange: (e)=>setCommitQuantity(Math.min(parseInt(e.target.value) || 1, remainingQty)),
                                                                                    className: "w-full px-3 py-2 border rounded-md"
                                                                                }, void 0, false, {
                                                                                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                                    lineNumber: 355,
                                                                                    columnNumber: 43
                                                                                }, this)
                                                                            ]
                                                                        }, void 0, true, {
                                                                            fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                            lineNumber: 351,
                                                                            columnNumber: 41
                                                                        }, this),
                                                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                            children: [
                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                                                    className: "block text-sm font-medium mb-1",
                                                                                    children: "Notes (optional)"
                                                                                }, void 0, false, {
                                                                                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                                    lineNumber: 365,
                                                                                    columnNumber: 43
                                                                                }, this),
                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("textarea", {
                                                                                    value: commitNotes,
                                                                                    onChange: (e)=>setCommitNotes(e.target.value),
                                                                                    placeholder: "Any additional details...",
                                                                                    rows: 2,
                                                                                    className: "w-full px-3 py-2 border rounded-md"
                                                                                }, void 0, false, {
                                                                                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                                    lineNumber: 368,
                                                                                    columnNumber: 43
                                                                                }, this)
                                                                            ]
                                                                        }, void 0, true, {
                                                                            fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                            lineNumber: 364,
                                                                            columnNumber: 41
                                                                        }, this),
                                                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                            className: "flex gap-2",
                                                                            children: [
                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                                                                    onClick: ()=>handleCommitToItem(signUpList.id, item.id),
                                                                                    disabled: commitToSignUpItem.isPending,
                                                                                    size: "sm",
                                                                                    children: commitToSignUpItem.isPending ? 'Committing...' : 'Confirm Commitment'
                                                                                }, void 0, false, {
                                                                                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                                    lineNumber: 377,
                                                                                    columnNumber: 43
                                                                                }, this),
                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                                                                    variant: "outline",
                                                                                    size: "sm",
                                                                                    onClick: ()=>{
                                                                                        setSelectedItemId(null);
                                                                                        setCommitQuantity(1);
                                                                                        setCommitNotes('');
                                                                                    },
                                                                                    children: "Cancel"
                                                                                }, void 0, false, {
                                                                                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                                    lineNumber: 384,
                                                                                    columnNumber: 43
                                                                                }, this)
                                                                            ]
                                                                        }, void 0, true, {
                                                                            fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                            lineNumber: 376,
                                                                            columnNumber: 41
                                                                        }, this)
                                                                    ]
                                                                }, void 0, true, {
                                                                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                    lineNumber: 350,
                                                                    columnNumber: 39
                                                                }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                                                    onClick: ()=>{
                                                                        setSelectedItemId(item.id);
                                                                        setCommitQuantity(Math.min(1, remainingQty));
                                                                    },
                                                                    size: "sm",
                                                                    variant: "outline",
                                                                    children: "I can bring this"
                                                                }, void 0, false, {
                                                                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                    lineNumber: 398,
                                                                    columnNumber: 39
                                                                }, this)
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                lineNumber: 348,
                                                                columnNumber: 35
                                                            }, this),
                                                            !userId && remainingQty > 0 && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-xs text-muted-foreground mt-2",
                                                                children: "Please log in to commit to this item"
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                lineNumber: 413,
                                                                columnNumber: 35
                                                            }, this),
                                                            userItemCommitment && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                className: "mt-2 p-2 bg-blue-50 border border-blue-200 rounded",
                                                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                    className: "text-sm font-medium text-blue-800",
                                                                    children: [
                                                                        "You committed to bring ",
                                                                        userItemCommitment.quantity,
                                                                        " of this item"
                                                                    ]
                                                                }, void 0, true, {
                                                                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                    lineNumber: 420,
                                                                    columnNumber: 37
                                                                }, this)
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                lineNumber: 419,
                                                                columnNumber: 35
                                                            }, this),
                                                            remainingQty === 0 && !userItemCommitment && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                className: "mt-2 p-2 bg-green-50 border border-green-200 rounded",
                                                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                    className: "text-sm font-medium text-green-800",
                                                                    children: "✓ Fully committed - Thank you everyone!"
                                                                }, void 0, false, {
                                                                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                    lineNumber: 428,
                                                                    columnNumber: 37
                                                                }, this)
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                lineNumber: 427,
                                                                columnNumber: 35
                                                            }, this)
                                                        ]
                                                    }, item.id, true, {
                                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                        lineNumber: 298,
                                                        columnNumber: 31
                                                    }, this);
                                                })
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                lineNumber: 291,
                                                columnNumber: 25
                                            }, this)
                                        ]
                                    }, category, true, {
                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                        lineNumber: 281,
                                        columnNumber: 23
                                    }, this);
                                })
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                lineNumber: 273,
                                columnNumber: 17
                            }, this) : /* LEGACY OPEN/PREDEFINED SIGN-UPS */ /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Fragment"], {
                                children: [
                                    signUpList.signUpType === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SignUpType"].Predefined && signUpList.predefinedItems.length > 0 && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "mb-4",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h4", {
                                                className: "font-semibold mb-2",
                                                children: "Suggested Items:"
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                lineNumber: 447,
                                                columnNumber: 23
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("ul", {
                                                className: "list-disc list-inside text-sm text-muted-foreground",
                                                children: signUpList.predefinedItems.map((item, index)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("li", {
                                                        children: item
                                                    }, index, false, {
                                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                        lineNumber: 450,
                                                        columnNumber: 27
                                                    }, this))
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                lineNumber: 448,
                                                columnNumber: 23
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                        lineNumber: 446,
                                        columnNumber: 21
                                    }, this),
                                    signUpList.commitments.length > 0 ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h4", {
                                                className: "font-semibold mb-2",
                                                children: [
                                                    "Commitments (",
                                                    signUpList.commitmentCount,
                                                    "):"
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                lineNumber: 459,
                                                columnNumber: 23
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "space-y-2",
                                                children: signUpList.commitments.map((commitment)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        className: "flex justify-between items-center p-2 bg-muted rounded-md",
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                children: [
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                        className: "font-medium",
                                                                        children: commitment.itemDescription
                                                                    }, void 0, false, {
                                                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                        lineNumber: 469,
                                                                        columnNumber: 31
                                                                    }, this),
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                        className: "text-sm text-muted-foreground",
                                                                        children: [
                                                                            "Quantity: ",
                                                                            commitment.quantity
                                                                        ]
                                                                    }, void 0, true, {
                                                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                        lineNumber: 470,
                                                                        columnNumber: 31
                                                                    }, this)
                                                                ]
                                                            }, void 0, true, {
                                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                lineNumber: 468,
                                                                columnNumber: 29
                                                            }, this),
                                                            commitment.userId === userId && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                                                variant: "destructive",
                                                                size: "sm",
                                                                onClick: ()=>handleCancel(signUpList.id),
                                                                disabled: cancelCommitment.isPending,
                                                                children: cancelCommitment.isPending ? 'Canceling...' : 'Cancel'
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                                lineNumber: 475,
                                                                columnNumber: 31
                                                            }, this)
                                                        ]
                                                    }, commitment.id, true, {
                                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                        lineNumber: 464,
                                                        columnNumber: 27
                                                    }, this))
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                lineNumber: 462,
                                                columnNumber: 23
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                        lineNumber: 458,
                                        columnNumber: 21
                                    }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                        className: "text-sm text-muted-foreground",
                                        children: "No commitments yet. Be the first!"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                        lineNumber: 489,
                                        columnNumber: 21
                                    }, this)
                                ]
                            }, void 0, true)
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                            lineNumber: 270,
                            columnNumber: 13
                        }, this),
                        !isCategoryBased && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["CardFooter"], {
                            children: [
                                !userCommitment && userId && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "w-full space-y-3",
                                    children: selectedSignUpId === signUpList.id ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "space-y-3",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                        className: "block text-sm font-medium mb-1",
                                                        children: "Item Description *"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                        lineNumber: 503,
                                                        columnNumber: 27
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
                                                        type: "text",
                                                        value: itemDescription,
                                                        onChange: (e)=>setItemDescription(e.target.value),
                                                        placeholder: "What will you bring?",
                                                        className: "w-full px-3 py-2 border rounded-md"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                        lineNumber: 506,
                                                        columnNumber: 27
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                lineNumber: 502,
                                                columnNumber: 25
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                        className: "block text-sm font-medium mb-1",
                                                        children: "Quantity"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                        lineNumber: 515,
                                                        columnNumber: 27
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
                                                        type: "number",
                                                        min: "1",
                                                        value: quantity,
                                                        onChange: (e)=>setQuantity(parseInt(e.target.value) || 1),
                                                        className: "w-full px-3 py-2 border rounded-md"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                        lineNumber: 516,
                                                        columnNumber: 27
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                lineNumber: 514,
                                                columnNumber: 25
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "flex gap-2",
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                                        onClick: ()=>handleCommit(signUpList.id),
                                                        disabled: commitToSignUp.isPending || !itemDescription.trim(),
                                                        children: commitToSignUp.isPending ? 'Committing...' : 'Confirm'
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                        lineNumber: 525,
                                                        columnNumber: 27
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                                        variant: "outline",
                                                        onClick: ()=>{
                                                            setSelectedSignUpId(null);
                                                            setItemDescription('');
                                                            setQuantity(1);
                                                        },
                                                        children: "Cancel"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                        lineNumber: 531,
                                                        columnNumber: 27
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                                lineNumber: 524,
                                                columnNumber: 25
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                        lineNumber: 501,
                                        columnNumber: 23
                                    }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                        onClick: ()=>setSelectedSignUpId(signUpList.id),
                                        children: "I can bring something"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                        lineNumber: 544,
                                        columnNumber: 23
                                    }, this)
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                    lineNumber: 499,
                                    columnNumber: 19
                                }, this),
                                !userId && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                    className: "text-sm text-muted-foreground",
                                    children: "Please log in to commit to items"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                                    lineNumber: 551,
                                    columnNumber: 19
                                }, this)
                            ]
                        }, void 0, true, {
                            fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                            lineNumber: 497,
                            columnNumber: 15
                        }, this)
                    ]
                }, signUpList.id, true, {
                    fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
                    lineNumber: 241,
                    columnNumber: 11
                }, this);
            })
        ]
    }, void 0, true, {
        fileName: "[project]/src/presentation/components/features/events/SignUpManagementSection.tsx",
        lineNumber: 230,
        columnNumber: 5
    }, this);
}
_s(SignUpManagementSection, "KXt3omaaWh0kdwgU2xqogIqf0gI=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEventSignUps$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useEventSignUps"],
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEventSignUps$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useCommitToSignUp"],
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEventSignUps$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useCancelCommitment"],
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEventSignUps$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useCommitToSignUpItem"]
    ];
});
_c = SignUpManagementSection;
const __TURBOPACK__default__export__ = SignUpManagementSection;
var _c;
__turbopack_context__.k.register(_c, "SignUpManagementSection");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/ui/Input.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "Input",
    ()=>Input
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-client] (ecmascript)");
;
;
;
/**
 * Input Component
 * Reusable input component with error states and accessibility
 * Follows UI/UX best practices
 */ const Input = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["forwardRef"](_c = ({ className, type = 'text', error, ...props }, ref)=>{
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
        type: type,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50', error && 'border-destructive focus-visible:ring-destructive', className),
        ref: ref,
        "aria-invalid": error ? 'true' : undefined,
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Input.tsx",
        lineNumber: 16,
        columnNumber: 7
    }, ("TURBOPACK compile-time value", void 0));
});
_c1 = Input;
Input.displayName = 'Input';
;
var _c, _c1;
__turbopack_context__.k.register(_c, "Input$React.forwardRef");
__turbopack_context__.k.register(_c1, "Input");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/infrastructure/api/repositories/profile.repository.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "ProfileRepository",
    ()=>ProfileRepository,
    "profileRepository",
    ()=>profileRepository
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$build$2f$polyfills$2f$process$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = /*#__PURE__*/ __turbopack_context__.i("[project]/node_modules/next/dist/build/polyfills/process.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/client/api-client.ts [app-client] (ecmascript)");
;
class ProfileRepository {
    basePath = '/users';
    /**
   * Get user profile by ID
   * @param userId User GUID
   * @returns Promise resolving to UserProfile
   */ async getProfile(userId) {
        const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/${userId}`);
        return response;
    }
    /**
   * Upload profile photo
   * @param userId User GUID
   * @param file Image file (max 5MB)
   * @returns Promise resolving to PhotoUploadResponse with new URL
   *
   * Note: Uses native fetch instead of Axios to avoid Content-Type header issues with multipart/form-data
   */ async uploadProfilePhoto(userId, file) {
        const formData = new FormData();
        formData.append('image', file); // Backend expects 'image' parameter (UsersController.cs line 112)
        // Use native fetch API for file uploads to avoid Axios header conflicts
        // Fetch automatically sets correct Content-Type with boundary for FormData
        const baseURL = ("TURBOPACK compile-time value", "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api") || 'http://localhost:5000/api';
        const url = `${baseURL}${this.basePath}/${userId}/profile-photo`;
        const token = __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"]['authToken']; // Access private token field
        const response = await fetch(url, {
            method: 'POST',
            body: formData,
            credentials: 'include',
            headers: {
                ...token ? {
                    'Authorization': `Bearer ${token}`
                } : {}
            }
        });
        if (!response.ok) {
            const errorText = await response.text();
            let errorMessage = `Upload failed with status ${response.status}`;
            try {
                const errorJson = JSON.parse(errorText);
                errorMessage = errorJson.message || errorJson.error || errorMessage;
            } catch  {
            // Not JSON, use text
            }
            throw new Error(errorMessage);
        }
        return await response.json();
    }
    /**
   * Delete profile photo
   * @param userId User GUID
   * @returns Promise resolving to success message
   */ async deleteProfilePhoto(userId) {
        const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].delete(`${this.basePath}/${userId}/profile-photo`);
        return response;
    }
    /**
   * Update user location
   * @param userId User GUID
   * @param location Location data (all fields optional)
   * @returns Promise resolving to updated UserProfile
   */ async updateLocation(userId, location) {
        const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].put(`${this.basePath}/${userId}/location`, location);
        return response;
    }
    /**
   * Update cultural interests
   * Phase 6A.9 FIX: Backend returns 204 No Content, so we reload profile after update
   * @param userId User GUID
   * @param interests Cultural interests (0-10 items)
   * @returns Promise resolving to updated UserProfile
   */ async updateCulturalInterests(userId, interests) {
        // PUT request returns 204 No Content on success (UsersController.cs line 295)
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].put(`${this.basePath}/${userId}/cultural-interests`, interests);
        // Reload full profile to get updated culturalInterests
        const updatedProfile = await this.getProfile(userId);
        return updatedProfile;
    }
    /**
   * Update languages
   * @param userId User GUID
   * @param languages Languages with proficiency levels
   * @returns Promise resolving to updated UserProfile
   */ async updateLanguages(userId, languages) {
        const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].put(`${this.basePath}/${userId}/languages`, languages);
        return response;
    }
    /**
   * Update basic user information
   * @param userId User GUID
   * @param basicInfo First name, last name, phone, bio
   * @returns Promise resolving to updated UserProfile
   */ async updateBasicInfo(userId, basicInfo) {
        // Note: This endpoint doesn't exist in backend yet
        // Using PUT /api/users/{id} as a placeholder
        // Backend team should add dedicated endpoint for basic info updates
        const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].put(`${this.basePath}/${userId}`, basicInfo);
        return response;
    }
    /**
   * Update user's preferred metro areas for location-based filtering
   * Phase 5B: User Preferred Metro Areas - Expanded to 20 max limit
   * Phase 6A.9 FIX: Backend returns 204 No Content, so we reload profile after update
   * @param userId User GUID
   * @param metroAreas Metro area IDs (0-20 items, GUIDs)
   * @returns Promise resolving to updated UserProfile
   */ async updatePreferredMetroAreas(userId, request) {
        // PUT request returns 204 No Content on success
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].put(`${this.basePath}/${userId}/preferred-metro-areas`, request);
        // Reload full profile to get updated preferredMetroAreas
        const updatedProfile = await this.getProfile(userId);
        return updatedProfile;
    }
    /**
   * Get user's preferred metro areas with full details
   * Phase 5B: User Preferred Metro Areas
   * @param userId User GUID
   * @returns Promise resolving to array of metro area GUIDs
   */ async getPreferredMetroAreas(userId) {
        const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/${userId}/preferred-metro-areas`);
        return response;
    }
}
const profileRepository = new ProfileRepository();
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/store/useProfileStore.ts [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "useProfileStore",
    ()=>useProfileStore
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zustand$2f$esm$2f$react$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/zustand/esm/react.mjs [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zustand$2f$esm$2f$middleware$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/zustand/esm/middleware.mjs [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$profile$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/repositories/profile.repository.ts [app-client] (ecmascript)");
;
;
;
const initialSectionStates = {
    photo: 'idle',
    basicInfo: 'idle',
    location: 'idle',
    culturalInterests: 'idle',
    languages: 'idle',
    preferredMetroAreas: 'idle'
};
const useProfileStore = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zustand$2f$esm$2f$react$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__["create"])()((0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$zustand$2f$esm$2f$middleware$2e$mjs__$5b$app$2d$client$5d$__$28$ecmascript$29$__["devtools"])((set, get)=>({
        // Initial state
        profile: null,
        originalProfile: null,
        isLoading: false,
        error: null,
        sectionStates: {
            ...initialSectionStates
        },
        // Set profile
        setProfile: (profile)=>{
            set({
                profile,
                originalProfile: JSON.parse(JSON.stringify(profile)),
                error: null
            });
        },
        // Load profile from API
        loadProfile: async (userId)=>{
            set({
                isLoading: true,
                error: null
            });
            try {
                const profile = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$profile$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["profileRepository"].getProfile(userId);
                get().setProfile(profile);
            } catch (error) {
                const errorMessage = error instanceof Error ? error.message : 'Failed to load profile';
                set({
                    error: errorMessage,
                    profile: null,
                    originalProfile: null
                });
            } finally{
                set({
                    isLoading: false
                });
            }
        },
        // Clear profile
        clearProfile: ()=>{
            set({
                profile: null,
                originalProfile: null,
                isLoading: false,
                error: null,
                sectionStates: {
                    ...initialSectionStates
                }
            });
        },
        // Upload profile photo
        uploadPhoto: async (userId, file)=>{
            set((state)=>({
                    sectionStates: {
                        ...state.sectionStates,
                        photo: 'saving'
                    },
                    error: null
                }));
            try {
                const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$profile$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["profileRepository"].uploadProfilePhoto(userId, file);
                // Update profile with new photo URL
                set((state)=>({
                        profile: state.profile ? {
                            ...state.profile,
                            profilePhotoUrl: response.profilePhotoUrl
                        } : null,
                        originalProfile: state.originalProfile ? {
                            ...state.originalProfile,
                            profilePhotoUrl: response.profilePhotoUrl
                        } : null,
                        sectionStates: {
                            ...state.sectionStates,
                            photo: 'success'
                        }
                    }));
                // Reset to idle after 2 seconds
                setTimeout(()=>{
                    set((state)=>({
                            sectionStates: {
                                ...state.sectionStates,
                                photo: 'idle'
                            }
                        }));
                }, 2000);
            } catch (error) {
                const errorMessage = error instanceof Error ? error.message : 'Failed to upload photo';
                set((state)=>({
                        error: errorMessage,
                        sectionStates: {
                            ...state.sectionStates,
                            photo: 'error'
                        }
                    }));
            }
        },
        // Delete profile photo
        deletePhoto: async (userId)=>{
            set((state)=>({
                    sectionStates: {
                        ...state.sectionStates,
                        photo: 'saving'
                    },
                    error: null
                }));
            try {
                await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$profile$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["profileRepository"].deleteProfilePhoto(userId);
                // Update profile to remove photo URL
                set((state)=>({
                        profile: state.profile ? {
                            ...state.profile,
                            profilePhotoUrl: null
                        } : null,
                        originalProfile: state.originalProfile ? {
                            ...state.originalProfile,
                            profilePhotoUrl: null
                        } : null,
                        sectionStates: {
                            ...state.sectionStates,
                            photo: 'success'
                        }
                    }));
                // Reset to idle after 2 seconds
                setTimeout(()=>{
                    set((state)=>({
                            sectionStates: {
                                ...state.sectionStates,
                                photo: 'idle'
                            }
                        }));
                }, 2000);
            } catch (error) {
                const errorMessage = error instanceof Error ? error.message : 'Failed to delete photo';
                set((state)=>({
                        error: errorMessage,
                        sectionStates: {
                            ...state.sectionStates,
                            photo: 'error'
                        }
                    }));
            }
        },
        // Update basic info
        updateBasicInfo: async (userId, basicInfo)=>{
            set((state)=>({
                    sectionStates: {
                        ...state.sectionStates,
                        basicInfo: 'saving'
                    },
                    error: null
                }));
            try {
                const updatedProfile = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$profile$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["profileRepository"].updateBasicInfo(userId, basicInfo);
                get().setProfile(updatedProfile);
                set((state)=>({
                        sectionStates: {
                            ...state.sectionStates,
                            basicInfo: 'success'
                        }
                    }));
                // Reset to idle after 2 seconds
                setTimeout(()=>{
                    set((state)=>({
                            sectionStates: {
                                ...state.sectionStates,
                                basicInfo: 'idle'
                            }
                        }));
                }, 2000);
            } catch (error) {
                const errorMessage = error instanceof Error ? error.message : 'Failed to update basic info';
                set((state)=>({
                        error: errorMessage,
                        sectionStates: {
                            ...state.sectionStates,
                            basicInfo: 'error'
                        }
                    }));
            }
        },
        // Update location
        updateLocation: async (userId, location)=>{
            set((state)=>({
                    sectionStates: {
                        ...state.sectionStates,
                        location: 'saving'
                    },
                    error: null
                }));
            try {
                const updatedProfile = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$profile$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["profileRepository"].updateLocation(userId, location);
                get().setProfile(updatedProfile);
                set((state)=>({
                        sectionStates: {
                            ...state.sectionStates,
                            location: 'success'
                        }
                    }));
                // Reset to idle after 2 seconds
                setTimeout(()=>{
                    set((state)=>({
                            sectionStates: {
                                ...state.sectionStates,
                                location: 'idle'
                            }
                        }));
                }, 2000);
            } catch (error) {
                const errorMessage = error instanceof Error ? error.message : 'Failed to update location';
                set((state)=>({
                        error: errorMessage,
                        sectionStates: {
                            ...state.sectionStates,
                            location: 'error'
                        }
                    }));
            }
        },
        // Update cultural interests
        updateCulturalInterests: async (userId, interests)=>{
            set((state)=>({
                    sectionStates: {
                        ...state.sectionStates,
                        culturalInterests: 'saving'
                    },
                    error: null
                }));
            try {
                const updatedProfile = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$profile$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["profileRepository"].updateCulturalInterests(userId, interests);
                get().setProfile(updatedProfile);
                set((state)=>({
                        sectionStates: {
                            ...state.sectionStates,
                            culturalInterests: 'success'
                        }
                    }));
                // Reset to idle after 2 seconds
                setTimeout(()=>{
                    set((state)=>({
                            sectionStates: {
                                ...state.sectionStates,
                                culturalInterests: 'idle'
                            }
                        }));
                }, 2000);
            } catch (error) {
                const errorMessage = error instanceof Error ? error.message : 'Failed to update cultural interests';
                set((state)=>({
                        error: errorMessage,
                        sectionStates: {
                            ...state.sectionStates,
                            culturalInterests: 'error'
                        }
                    }));
            }
        },
        // Update languages
        updateLanguages: async (userId, languages)=>{
            set((state)=>({
                    sectionStates: {
                        ...state.sectionStates,
                        languages: 'saving'
                    },
                    error: null
                }));
            try {
                const updatedProfile = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$profile$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["profileRepository"].updateLanguages(userId, languages);
                get().setProfile(updatedProfile);
                set((state)=>({
                        sectionStates: {
                            ...state.sectionStates,
                            languages: 'success'
                        }
                    }));
                // Reset to idle after 2 seconds
                setTimeout(()=>{
                    set((state)=>({
                            sectionStates: {
                                ...state.sectionStates,
                                languages: 'idle'
                            }
                        }));
                }, 2000);
            } catch (error) {
                const errorMessage = error instanceof Error ? error.message : 'Failed to update languages';
                set((state)=>({
                        error: errorMessage,
                        sectionStates: {
                            ...state.sectionStates,
                            languages: 'error'
                        }
                    }));
            }
        },
        // Phase 5B: Update user's preferred metro areas for location-based filtering
        // Validates against max 20 limit (expanded from 10)
        updatePreferredMetroAreas: async (userId, metroAreas)=>{
            // Frontend validation: Check max 20 limit
            // Phase 6A.9 FIX: Property name changed to PascalCase to match backend
            if (metroAreas.MetroAreaIds.length > 20) {
                set((state)=>({
                        error: 'Cannot select more than 20 metro areas',
                        sectionStates: {
                            ...state.sectionStates,
                            preferredMetroAreas: 'error'
                        }
                    }));
                return;
            }
            set((state)=>({
                    sectionStates: {
                        ...state.sectionStates,
                        preferredMetroAreas: 'saving'
                    },
                    error: null
                }));
            try {
                const updatedProfile = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$profile$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["profileRepository"].updatePreferredMetroAreas(userId, metroAreas);
                get().setProfile(updatedProfile);
                set((state)=>({
                        sectionStates: {
                            ...state.sectionStates,
                            preferredMetroAreas: 'success'
                        }
                    }));
                // Reset to idle after 2 seconds
                setTimeout(()=>{
                    set((state)=>({
                            sectionStates: {
                                ...state.sectionStates,
                                preferredMetroAreas: 'idle'
                            }
                        }));
                }, 2000);
            } catch (error) {
                const errorMessage = error instanceof Error ? error.message : 'Failed to update preferred metro areas';
                set((state)=>({
                        error: errorMessage,
                        sectionStates: {
                            ...state.sectionStates,
                            preferredMetroAreas: 'error'
                        }
                    }));
            }
        },
        // Mark section as dirty (has unsaved changes)
        markSectionDirty: (section)=>{
            set((state)=>({
                    sectionStates: {
                        ...state.sectionStates,
                        [section]: 'dirty'
                    }
                }));
        },
        // Mark section as clean (no unsaved changes)
        markSectionClean: (section)=>{
            set((state)=>({
                    sectionStates: {
                        ...state.sectionStates,
                        [section]: 'idle'
                    }
                }));
        },
        // Reset section state to idle
        resetSectionState: (section)=>{
            set((state)=>({
                    sectionStates: {
                        ...state.sectionStates,
                        [section]: 'idle'
                    }
                }));
        },
        // Check if section has unsaved changes
        isSectionDirty: (section)=>{
            return get().sectionStates[section] === 'dirty';
        }
    }), {
    name: 'ProfileStore'
}));
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/features/events/EventRegistrationForm.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "EventRegistrationForm",
    ()=>EventRegistrationForm
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Input$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Input.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Button.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$clock$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Clock$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/clock.js [app-client] (ecmascript) <export default as Clock>");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/store/useAuthStore.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useProfileStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/store/useProfileStore.ts [app-client] (ecmascript)");
;
var _s = __turbopack_context__.k.signature();
'use client';
;
;
;
;
;
;
function EventRegistrationForm({ eventId, spotsLeft, isFree, ticketPrice, isProcessing, onSubmit, error }) {
    _s();
    const { user } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useAuthStore"])();
    const { profile, loadProfile } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useProfileStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useProfileStore"])();
    // Form state
    const [quantity, setQuantity] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(1);
    const [name, setName] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])('');
    const [age, setAge] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])('');
    const [address, setAddress] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])('');
    const [email, setEmail] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])('');
    const [phoneNumber, setPhoneNumber] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])('');
    // Validation state
    const [touched, setTouched] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])({
        name: false,
        age: false,
        address: false,
        email: false,
        phoneNumber: false
    });
    // Load profile for authenticated users
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useEffect"])({
        "EventRegistrationForm.useEffect": ()=>{
            if (user?.userId && !profile) {
                loadProfile(user.userId);
            }
        }
    }["EventRegistrationForm.useEffect"], [
        user?.userId,
        profile,
        loadProfile
    ]);
    // Auto-fill from profile for authenticated users
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useEffect"])({
        "EventRegistrationForm.useEffect": ()=>{
            if (user && profile) {
                setName(`${profile.firstName} ${profile.lastName}`.trim());
                setEmail(profile.email);
                setPhoneNumber(profile.phoneNumber || '');
                // Build address from location if available
                if (profile.location) {
                    const addressParts = [
                        profile.location.city,
                        profile.location.state,
                        profile.location.zipCode
                    ].filter(Boolean);
                    setAddress(addressParts.join(', '));
                }
            }
        }
    }["EventRegistrationForm.useEffect"], [
        user,
        profile
    ]);
    // Validation
    const errors = {
        name: touched.name && !name.trim() ? 'Name is required' : '',
        age: touched.age && (!age || age < 1 || age > 120) ? 'Valid age is required (1-120)' : '',
        address: touched.address && !address.trim() ? 'Address is required' : '',
        email: touched.email && (!email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) ? 'Valid email is required' : '',
        phoneNumber: touched.phoneNumber && (!phoneNumber.trim() || !/^\+?[\d\s\-()]+$/.test(phoneNumber)) ? 'Valid phone number is required' : ''
    };
    const isFormValid = !user ? name.trim() && age && age >= 1 && age <= 120 && address.trim() && email.trim() && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email) && phoneNumber.trim() && /^\+?[\d\s\-()]+$/.test(phoneNumber) : true;
    const handleSubmit = async (e)=>{
        e.preventDefault();
        if (!user) {
            // Mark all fields as touched for validation
            setTouched({
                name: true,
                age: true,
                address: true,
                email: true,
                phoneNumber: true
            });
            if (!isFormValid) {
                return;
            }
            // Anonymous registration
            const anonymousData = {
                name: name.trim(),
                age: Number(age),
                address: address.trim(),
                email: email.trim(),
                phoneNumber: phoneNumber.trim(),
                quantity
            };
            await onSubmit(anonymousData);
        } else {
            // Authenticated registration
            await onSubmit({
                userId: user.userId,
                quantity
            });
        }
    };
    const totalPrice = ticketPrice ? ticketPrice * quantity : 0;
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("form", {
        onSubmit: handleSubmit,
        className: "space-y-4",
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                        className: "block text-sm font-medium mb-2 text-neutral-700",
                        children: "Number of Attendees"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                        lineNumber: 135,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Input$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Input"], {
                        type: "number",
                        min: "1",
                        max: Math.min(10, spotsLeft),
                        value: quantity,
                        onChange: (e)=>setQuantity(parseInt(e.target.value) || 1),
                        disabled: isProcessing,
                        className: "w-full"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                        lineNumber: 138,
                        columnNumber: 9
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                lineNumber: 134,
                columnNumber: 7
            }, this),
            !user && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Fragment"], {
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "border-t pt-4",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h4", {
                                className: "text-sm font-semibold mb-3 text-neutral-700",
                                children: "Your Contact Information"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 153,
                                columnNumber: 13
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                className: "text-xs text-neutral-500 mb-4",
                                children: "We'll use this information to send you event updates and confirmations."
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 154,
                                columnNumber: 13
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                        lineNumber: 152,
                        columnNumber: 11
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                className: "block text-sm font-medium mb-2 text-neutral-700",
                                children: [
                                    "Full Name ",
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                        className: "text-red-500",
                                        children: "*"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                        lineNumber: 162,
                                        columnNumber: 25
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 161,
                                columnNumber: 13
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Input$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Input"], {
                                type: "text",
                                value: name,
                                onChange: (e)=>setName(e.target.value),
                                onBlur: ()=>setTouched({
                                        ...touched,
                                        name: true
                                    }),
                                error: !!errors.name,
                                disabled: isProcessing,
                                placeholder: "Enter your full name",
                                className: "w-full"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 164,
                                columnNumber: 13
                            }, this),
                            errors.name && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                className: "text-xs text-red-600 mt-1",
                                children: errors.name
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 175,
                                columnNumber: 15
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                        lineNumber: 160,
                        columnNumber: 11
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                className: "block text-sm font-medium mb-2 text-neutral-700",
                                children: [
                                    "Age ",
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                        className: "text-red-500",
                                        children: "*"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                        lineNumber: 182,
                                        columnNumber: 19
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 181,
                                columnNumber: 13
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Input$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Input"], {
                                type: "number",
                                min: "1",
                                max: "120",
                                value: age,
                                onChange: (e)=>setAge(e.target.value ? parseInt(e.target.value) : ''),
                                onBlur: ()=>setTouched({
                                        ...touched,
                                        age: true
                                    }),
                                error: !!errors.age,
                                disabled: isProcessing,
                                placeholder: "Enter your age",
                                className: "w-full"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 184,
                                columnNumber: 13
                            }, this),
                            errors.age && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                className: "text-xs text-red-600 mt-1",
                                children: errors.age
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 197,
                                columnNumber: 15
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                        lineNumber: 180,
                        columnNumber: 11
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                className: "block text-sm font-medium mb-2 text-neutral-700",
                                children: [
                                    "Address ",
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                        className: "text-red-500",
                                        children: "*"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                        lineNumber: 204,
                                        columnNumber: 23
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 203,
                                columnNumber: 13
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Input$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Input"], {
                                type: "text",
                                value: address,
                                onChange: (e)=>setAddress(e.target.value),
                                onBlur: ()=>setTouched({
                                        ...touched,
                                        address: true
                                    }),
                                error: !!errors.address,
                                disabled: isProcessing,
                                placeholder: "Enter your address",
                                className: "w-full"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 206,
                                columnNumber: 13
                            }, this),
                            errors.address && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                className: "text-xs text-red-600 mt-1",
                                children: errors.address
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 217,
                                columnNumber: 15
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                        lineNumber: 202,
                        columnNumber: 11
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                className: "block text-sm font-medium mb-2 text-neutral-700",
                                children: [
                                    "Email ",
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                        className: "text-red-500",
                                        children: "*"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                        lineNumber: 224,
                                        columnNumber: 21
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 223,
                                columnNumber: 13
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Input$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Input"], {
                                type: "email",
                                value: email,
                                onChange: (e)=>setEmail(e.target.value),
                                onBlur: ()=>setTouched({
                                        ...touched,
                                        email: true
                                    }),
                                error: !!errors.email,
                                disabled: isProcessing,
                                placeholder: "your.email@example.com",
                                className: "w-full"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 226,
                                columnNumber: 13
                            }, this),
                            errors.email && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                className: "text-xs text-red-600 mt-1",
                                children: errors.email
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 237,
                                columnNumber: 15
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                        lineNumber: 222,
                        columnNumber: 11
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                className: "block text-sm font-medium mb-2 text-neutral-700",
                                children: [
                                    "Phone Number ",
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                        className: "text-red-500",
                                        children: "*"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                        lineNumber: 244,
                                        columnNumber: 28
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 243,
                                columnNumber: 13
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Input$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Input"], {
                                type: "tel",
                                value: phoneNumber,
                                onChange: (e)=>setPhoneNumber(e.target.value),
                                onBlur: ()=>setTouched({
                                        ...touched,
                                        phoneNumber: true
                                    }),
                                error: !!errors.phoneNumber,
                                disabled: isProcessing,
                                placeholder: "+1-123-456-7890",
                                className: "w-full"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 246,
                                columnNumber: 13
                            }, this),
                            errors.phoneNumber && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                className: "text-xs text-red-600 mt-1",
                                children: errors.phoneNumber
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 257,
                                columnNumber: 15
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                        lineNumber: 242,
                        columnNumber: 11
                    }, this)
                ]
            }, void 0, true),
            user && profile && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "border-t pt-4",
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h4", {
                        className: "text-sm font-semibold mb-3 text-neutral-700",
                        children: "Registration Details"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                        lineNumber: 266,
                        columnNumber: 11
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "bg-neutral-50 p-4 rounded-lg space-y-2 text-sm",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                        className: "font-medium",
                                        children: "Name:"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                        lineNumber: 269,
                                        columnNumber: 15
                                    }, this),
                                    " ",
                                    profile.firstName,
                                    " ",
                                    profile.lastName
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 268,
                                columnNumber: 13
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                        className: "font-medium",
                                        children: "Email:"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                        lineNumber: 272,
                                        columnNumber: 15
                                    }, this),
                                    " ",
                                    profile.email
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 271,
                                columnNumber: 13
                            }, this),
                            profile.phoneNumber && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                        className: "font-medium",
                                        children: "Phone:"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                        lineNumber: 276,
                                        columnNumber: 17
                                    }, this),
                                    " ",
                                    profile.phoneNumber
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                                lineNumber: 275,
                                columnNumber: 15
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                        lineNumber: 267,
                        columnNumber: 11
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                        className: "text-xs text-neutral-500 mt-2",
                        children: "Registration details from your profile"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                        lineNumber: 280,
                        columnNumber: 11
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                lineNumber: 265,
                columnNumber: 9
            }, this),
            !isFree && totalPrice > 0 && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "p-4 bg-neutral-50 rounded-lg border-t-2 border-orange-500",
                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "flex justify-between items-center",
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                            className: "text-base font-medium text-neutral-700",
                            children: "Total"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                            lineNumber: 290,
                            columnNumber: 13
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                            className: "text-xl font-bold",
                            style: {
                                color: '#8B1538'
                            },
                            children: [
                                "$",
                                totalPrice.toFixed(2)
                            ]
                        }, void 0, true, {
                            fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                            lineNumber: 291,
                            columnNumber: 13
                        }, this)
                    ]
                }, void 0, true, {
                    fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                    lineNumber: 289,
                    columnNumber: 11
                }, this)
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                lineNumber: 288,
                columnNumber: 9
            }, this),
            error && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "p-4 bg-red-50 border border-red-200 rounded-lg",
                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                    className: "text-sm text-red-600",
                    children: error
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                    lineNumber: 301,
                    columnNumber: 11
                }, this)
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                lineNumber: 300,
                columnNumber: 9
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                type: "submit",
                disabled: isProcessing || !user && !isFormValid,
                className: "w-full text-lg py-6",
                style: {
                    background: '#FF7900'
                },
                children: isProcessing ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Fragment"], {
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$clock$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Clock$3e$__["Clock"], {
                            className: "h-5 w-5 mr-2 animate-spin"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                            lineNumber: 314,
                            columnNumber: 13
                        }, this),
                        "Processing..."
                    ]
                }, void 0, true) : isFree ? 'Register for Free' : 'Continue to Payment'
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                lineNumber: 306,
                columnNumber: 7
            }, this),
            !user && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                className: "text-sm text-center text-neutral-500",
                children: [
                    "Have an account? ",
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
                        href: "/login",
                        className: "text-orange-600 hover:underline",
                        children: "Sign in"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                        lineNumber: 326,
                        columnNumber: 28
                    }, this),
                    " to register faster"
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
                lineNumber: 325,
                columnNumber: 9
            }, this)
        ]
    }, void 0, true, {
        fileName: "[project]/src/presentation/components/features/events/EventRegistrationForm.tsx",
        lineNumber: 132,
        columnNumber: 5
    }, this);
}
_s(EventRegistrationForm, "tw389uSNjy/y1KXXyagsvwOue4A=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useAuthStore"],
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useProfileStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useProfileStore"]
    ];
});
_c = EventRegistrationForm;
var _c;
__turbopack_context__.k.register(_c, "EventRegistrationForm");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/ui/Dialog.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "Dialog",
    ()=>Dialog,
    "DialogContent",
    ()=>DialogContent,
    "DialogDescription",
    ()=>DialogDescription,
    "DialogFooter",
    ()=>DialogFooter,
    "DialogHeader",
    ()=>DialogHeader,
    "DialogTitle",
    ()=>DialogTitle
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-client] (ecmascript)");
;
var _s = __turbopack_context__.k.signature();
'use client';
;
;
function Dialog({ open, onOpenChange, children }) {
    _s();
    __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useEffect"]({
        "Dialog.useEffect": ()=>{
            const handleEscape = {
                "Dialog.useEffect.handleEscape": (e)=>{
                    if (e.key === 'Escape') {
                        onOpenChange(false);
                    }
                }
            }["Dialog.useEffect.handleEscape"];
            if (open) {
                document.addEventListener('keydown', handleEscape);
                document.body.style.overflow = 'hidden';
            }
            return ({
                "Dialog.useEffect": ()=>{
                    document.removeEventListener('keydown', handleEscape);
                    document.body.style.overflow = 'unset';
                }
            })["Dialog.useEffect"];
        }
    }["Dialog.useEffect"], [
        open,
        onOpenChange
    ]);
    if (!open) return null;
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        className: "fixed inset-0 z-50",
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "fixed inset-0 bg-black/80 backdrop-blur-sm",
                onClick: ()=>onOpenChange(false),
                "aria-hidden": "true"
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/ui/Dialog.tsx",
                lineNumber: 50,
                columnNumber: 7
            }, this),
            children
        ]
    }, void 0, true, {
        fileName: "[project]/src/presentation/components/ui/Dialog.tsx",
        lineNumber: 48,
        columnNumber: 5
    }, this);
}
_s(Dialog, "OD7bBpZva5O2jO+Puf00hKivP7c=");
_c = Dialog;
const DialogContent = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["forwardRef"](_c1 = ({ className, children, ...props }, ref)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('fixed left-1/2 top-1/2 z-50 -translate-x-1/2 -translate-y-1/2', 'bg-white dark:bg-neutral-900 rounded-lg shadow-xl', 'w-full max-w-lg p-6', className),
        onClick: (e)=>e.stopPropagation(),
        ...props,
        children: children
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Dialog.tsx",
        lineNumber: 63,
        columnNumber: 5
    }, ("TURBOPACK compile-time value", void 0)));
_c2 = DialogContent;
DialogContent.displayName = 'DialogContent';
const DialogHeader = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["forwardRef"](_c3 = ({ className, ...props }, ref)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('flex flex-col space-y-1.5 mb-4', className),
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Dialog.tsx",
        lineNumber: 82,
        columnNumber: 5
    }, ("TURBOPACK compile-time value", void 0)));
_c4 = DialogHeader;
DialogHeader.displayName = 'DialogHeader';
const DialogTitle = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["forwardRef"](_c5 = ({ className, ...props }, ref)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h2", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('text-lg font-semibold text-neutral-900 dark:text-neutral-100', className),
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Dialog.tsx",
        lineNumber: 91,
        columnNumber: 3
    }, ("TURBOPACK compile-time value", void 0)));
_c6 = DialogTitle;
DialogTitle.displayName = 'DialogTitle';
const DialogDescription = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["forwardRef"](_c7 = ({ className, ...props }, ref)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('text-sm text-neutral-600 dark:text-neutral-400', className),
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Dialog.tsx",
        lineNumber: 103,
        columnNumber: 3
    }, ("TURBOPACK compile-time value", void 0)));
_c8 = DialogDescription;
DialogDescription.displayName = 'DialogDescription';
const DialogFooter = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["forwardRef"](_c9 = ({ className, ...props }, ref)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["cn"])('flex items-center justify-end gap-3 mt-6', className),
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Dialog.tsx",
        lineNumber: 113,
        columnNumber: 5
    }, ("TURBOPACK compile-time value", void 0)));
_c10 = DialogFooter;
DialogFooter.displayName = 'DialogFooter';
var _c, _c1, _c2, _c3, _c4, _c5, _c6, _c7, _c8, _c9, _c10;
__turbopack_context__.k.register(_c, "Dialog");
__turbopack_context__.k.register(_c1, "DialogContent$React.forwardRef");
__turbopack_context__.k.register(_c2, "DialogContent");
__turbopack_context__.k.register(_c3, "DialogHeader$React.forwardRef");
__turbopack_context__.k.register(_c4, "DialogHeader");
__turbopack_context__.k.register(_c5, "DialogTitle$React.forwardRef");
__turbopack_context__.k.register(_c6, "DialogTitle");
__turbopack_context__.k.register(_c7, "DialogDescription$React.forwardRef");
__turbopack_context__.k.register(_c8, "DialogDescription");
__turbopack_context__.k.register(_c9, "DialogFooter$React.forwardRef");
__turbopack_context__.k.register(_c10, "DialogFooter");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/presentation/components/features/events/MediaGallery.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "MediaGallery",
    ()=>MediaGallery
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$x$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__X$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/x.js [app-client] (ecmascript) <export default as X>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$play$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Play$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/play.js [app-client] (ecmascript) <export default as Play>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$left$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronLeft$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/chevron-left.js [app-client] (ecmascript) <export default as ChevronLeft>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$right$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronRight$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/chevron-right.js [app-client] (ecmascript) <export default as ChevronRight>");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Dialog$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Dialog.tsx [app-client] (ecmascript)");
;
var _s = __turbopack_context__.k.signature();
'use client';
;
;
;
function MediaGallery({ images = [], videos = [], className = '' }) {
    _s();
    const [lightboxOpen, setLightboxOpen] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(false);
    const [currentIndex, setCurrentIndex] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(0);
    const [mediaType, setMediaType] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])('image');
    // Combine images and videos for total media count
    const totalImages = images.length;
    const totalVideos = videos.length;
    const totalMedia = totalImages + totalVideos;
    if (totalMedia === 0) {
        return null; // Don't render if no media
    }
    // Open lightbox for image
    const openImageLightbox = (index)=>{
        setCurrentIndex(index);
        setMediaType('image');
        setLightboxOpen(true);
    };
    // Open lightbox for video
    const openVideoLightbox = (index)=>{
        setCurrentIndex(index);
        setMediaType('video');
        setLightboxOpen(true);
    };
    // Navigate to previous media
    const goToPrevious = ()=>{
        if (mediaType === 'image') {
            setCurrentIndex((prev)=>prev === 0 ? totalImages - 1 : prev - 1);
        } else {
            setCurrentIndex((prev)=>prev === 0 ? totalVideos - 1 : prev - 1);
        }
    };
    // Navigate to next media
    const goToNext = ()=>{
        if (mediaType === 'image') {
            setCurrentIndex((prev)=>prev === totalImages - 1 ? 0 : prev + 1);
        } else {
            setCurrentIndex((prev)=>prev === totalVideos - 1 ? 0 : prev + 1);
        }
    };
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Fragment"], {
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: `space-y-6 ${className}`,
                children: [
                    totalImages > 0 && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
                                className: "text-lg font-semibold text-neutral-900 dark:text-neutral-100 mb-3",
                                children: [
                                    "Photos (",
                                    totalImages,
                                    ")"
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                lineNumber: 76,
                                columnNumber: 13
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4",
                                children: images.sort((a, b)=>a.displayOrder - b.displayOrder).map((image, index)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                        onClick: ()=>openImageLightbox(index),
                                        className: "relative aspect-square rounded-lg overflow-hidden bg-neutral-100 dark:bg-neutral-800 hover:opacity-90 transition-opacity group",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("img", {
                                                src: image.imageUrl,
                                                alt: `Event photo ${image.displayOrder}`,
                                                className: "w-full h-full object-cover"
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                                lineNumber: 88,
                                                columnNumber: 21
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "absolute inset-0 bg-black/0 group-hover:bg-black/10 transition-colors"
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                                lineNumber: 93,
                                                columnNumber: 21
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "absolute top-2 left-2 bg-black/50 text-white text-xs px-2 py-1 rounded",
                                                children: image.displayOrder
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                                lineNumber: 94,
                                                columnNumber: 21
                                            }, this)
                                        ]
                                    }, image.id, true, {
                                        fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                        lineNumber: 83,
                                        columnNumber: 19
                                    }, this))
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                lineNumber: 79,
                                columnNumber: 13
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                        lineNumber: 75,
                        columnNumber: 11
                    }, this),
                    totalVideos > 0 && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
                                className: "text-lg font-semibold text-neutral-900 dark:text-neutral-100 mb-3",
                                children: [
                                    "Videos (",
                                    totalVideos,
                                    ")"
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                lineNumber: 106,
                                columnNumber: 13
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4",
                                children: videos.sort((a, b)=>a.displayOrder - b.displayOrder).map((video, index)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                        onClick: ()=>openVideoLightbox(index),
                                        className: "relative aspect-video rounded-lg overflow-hidden bg-neutral-100 dark:bg-neutral-800 hover:opacity-90 transition-opacity group",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("img", {
                                                src: video.thumbnailUrl,
                                                alt: `Video ${video.displayOrder}`,
                                                className: "w-full h-full object-cover"
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                                lineNumber: 118,
                                                columnNumber: 21
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "absolute inset-0 bg-black/30 group-hover:bg-black/40 transition-colors flex items-center justify-center",
                                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                    className: "bg-white/90 rounded-full p-4",
                                                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$play$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Play$3e$__["Play"], {
                                                        className: "h-8 w-8 text-neutral-900",
                                                        fill: "currentColor"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                                        lineNumber: 125,
                                                        columnNumber: 25
                                                    }, this)
                                                }, void 0, false, {
                                                    fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                                    lineNumber: 124,
                                                    columnNumber: 23
                                                }, this)
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                                lineNumber: 123,
                                                columnNumber: 21
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "absolute top-2 left-2 bg-black/50 text-white text-xs px-2 py-1 rounded",
                                                children: video.displayOrder
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                                lineNumber: 128,
                                                columnNumber: 21
                                            }, this),
                                            video.durationSeconds && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "absolute bottom-2 right-2 bg-black/70 text-white text-xs px-2 py-1 rounded",
                                                children: [
                                                    Math.floor(video.durationSeconds / 60),
                                                    ":",
                                                    String(video.durationSeconds % 60).padStart(2, '0')
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                                lineNumber: 132,
                                                columnNumber: 23
                                            }, this)
                                        ]
                                    }, video.id, true, {
                                        fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                        lineNumber: 113,
                                        columnNumber: 19
                                    }, this))
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                lineNumber: 109,
                                columnNumber: 13
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                        lineNumber: 105,
                        columnNumber: 11
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                lineNumber: 72,
                columnNumber: 7
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Dialog$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Dialog"], {
                open: lightboxOpen,
                onOpenChange: setLightboxOpen,
                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Dialog$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["DialogContent"], {
                    className: "max-w-7xl w-full h-[90vh] p-0 bg-black/95",
                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "relative w-full h-full flex items-center justify-center",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                onClick: ()=>setLightboxOpen(false),
                                className: "absolute top-4 right-4 z-50 p-2 rounded-full bg-white/10 hover:bg-white/20 transition-colors",
                                "aria-label": "Close lightbox",
                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$x$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__X$3e$__["X"], {
                                    className: "h-6 w-6 text-white"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                    lineNumber: 153,
                                    columnNumber: 15
                                }, this)
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                lineNumber: 148,
                                columnNumber: 13
                            }, this),
                            (mediaType === 'image' && totalImages > 1 || mediaType === 'video' && totalVideos > 1) && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Fragment"], {
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                        onClick: goToPrevious,
                                        className: "absolute left-4 z-50 p-3 rounded-full bg-white/10 hover:bg-white/20 transition-colors",
                                        "aria-label": "Previous media",
                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$left$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronLeft$3e$__["ChevronLeft"], {
                                            className: "h-6 w-6 text-white"
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                            lineNumber: 165,
                                            columnNumber: 19
                                        }, this)
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                        lineNumber: 160,
                                        columnNumber: 17
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                        onClick: goToNext,
                                        className: "absolute right-4 z-50 p-3 rounded-full bg-white/10 hover:bg-white/20 transition-colors",
                                        "aria-label": "Next media",
                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$right$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronRight$3e$__["ChevronRight"], {
                                            className: "h-6 w-6 text-white"
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                            lineNumber: 172,
                                            columnNumber: 19
                                        }, this)
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                        lineNumber: 167,
                                        columnNumber: 17
                                    }, this)
                                ]
                            }, void 0, true),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "w-full h-full flex items-center justify-center p-12",
                                children: [
                                    mediaType === 'image' && images[currentIndex] && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("img", {
                                        src: images[currentIndex].imageUrl,
                                        alt: `Event photo ${images[currentIndex].displayOrder}`,
                                        className: "max-w-full max-h-full object-contain"
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                        lineNumber: 180,
                                        columnNumber: 17
                                    }, this),
                                    mediaType === 'video' && videos[currentIndex] && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("video", {
                                        src: videos[currentIndex].videoUrl,
                                        controls: true,
                                        autoPlay: true,
                                        className: "max-w-full max-h-full",
                                        poster: videos[currentIndex].thumbnailUrl,
                                        children: "Your browser does not support the video tag."
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                        lineNumber: 188,
                                        columnNumber: 17
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                lineNumber: 178,
                                columnNumber: 13
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "absolute bottom-4 left-1/2 -translate-x-1/2 bg-white/10 text-white text-sm px-4 py-2 rounded-full",
                                children: [
                                    currentIndex + 1,
                                    " / ",
                                    mediaType === 'image' ? totalImages : totalVideos
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                                lineNumber: 201,
                                columnNumber: 13
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                        lineNumber: 146,
                        columnNumber: 11
                    }, this)
                }, void 0, false, {
                    fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                    lineNumber: 145,
                    columnNumber: 9
                }, this)
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/features/events/MediaGallery.tsx",
                lineNumber: 144,
                columnNumber: 7
            }, this)
        ]
    }, void 0, true);
}
_s(MediaGallery, "F9sd4UDpVjm04z/qivq+58R5cbI=");
_c = MediaGallery;
var _c;
__turbopack_context__.k.register(_c, "MediaGallery");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
"[project]/src/app/events/[id]/page.tsx [app-client] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "default",
    ()=>EventDetailPage
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/jsx-dev-runtime.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/compiled/react/index.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/navigation.js [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$arrow$2d$left$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ArrowLeft$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/arrow-left.js [app-client] (ecmascript) <export default as ArrowLeft>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$calendar$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Calendar$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/calendar.js [app-client] (ecmascript) <export default as Calendar>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$map$2d$pin$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__MapPin$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/map-pin.js [app-client] (ecmascript) <export default as MapPin>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$users$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Users$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/users.js [app-client] (ecmascript) <export default as Users>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$dollar$2d$sign$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__DollarSign$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/dollar-sign.js [app-client] (ecmascript) <export default as DollarSign>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$clock$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Clock$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/clock.js [app-client] (ecmascript) <export default as Clock>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$circle$2d$alert$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__AlertCircle$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/circle-alert.js [app-client] (ecmascript) <export default as AlertCircle>");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Header$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/layout/Header.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Footer$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/layout/Footer.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Card.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Button.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Badge$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Badge.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEvents$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/hooks/useEvents.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$events$2f$SignUpManagementSection$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/features/events/SignUpManagementSection.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$events$2f$EventRegistrationForm$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/features/events/EventRegistrationForm.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$events$2f$MediaGallery$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/features/events/MediaGallery.tsx [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/store/useAuthStore.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/types/events.types.ts [app-client] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/repositories/events.repository.ts [app-client] (ecmascript)");
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
function EventDetailPage({ params }) {
    _s();
    const { id } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["use"])(params);
    const router = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRouter"])();
    const { user } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useAuthStore"])();
    const [isProcessing, setIsProcessing] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(false);
    const [error, setError] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(null);
    const [isJoiningWaitlist, setIsJoiningWaitlist] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(false);
    const [isPublishing, setIsPublishing] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$index$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useState"])(false);
    // Fetch event details
    const { data: event, isLoading, error: fetchError } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEvents$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useEventById"])(id);
    // RSVP mutation
    const rsvpMutation = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEvents$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRsvpToEvent"])();
    // Category labels
    const categoryLabels = {
        [__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["EventCategory"].Religious]: 'Religious',
        [__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["EventCategory"].Cultural]: 'Cultural',
        [__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["EventCategory"].Community]: 'Community',
        [__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["EventCategory"].Educational]: 'Educational',
        [__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["EventCategory"].Social]: 'Social',
        [__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["EventCategory"].Business]: 'Business',
        [__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["EventCategory"].Charity]: 'Charity',
        [__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["EventCategory"].Entertainment]: 'Entertainment'
    };
    // Handle Registration (both anonymous and authenticated)
    const handleRegistration = async (data)=>{
        if (!event) return;
        try {
            setIsProcessing(true);
            setError(null);
            // Check if this is anonymous or authenticated registration
            if ('userId' in data) {
                // Authenticated user registration
                // For paid events, redirect to Stripe Checkout
                if (!event.isFree && event.ticketPriceAmount) {
                    // TODO: Integrate with Stripe checkout session for paid events
                    // For now, handle as free RSVP
                    await rsvpMutation.mutateAsync({
                        eventId: id,
                        userId: data.userId,
                        quantity: data.quantity
                    });
                } else {
                    // Free event - direct RSVP
                    await rsvpMutation.mutateAsync({
                        eventId: id,
                        userId: data.userId,
                        quantity: data.quantity
                    });
                }
            } else {
                // Anonymous registration
                await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].registerAnonymous(id, data);
                // Show success message
                alert('Registration successful! We\'ve sent a confirmation email to ' + data.email);
                // Reload to show updated registration count
                window.location.reload();
            }
            setIsProcessing(false);
        } catch (err) {
            console.error('Registration failed:', err);
            setError(err instanceof Error ? err.message : 'Failed to register. Please try again.');
            setIsProcessing(false);
        }
    };
    // Handle Waitlist
    const handleJoinWaitlist = async ()=>{
        if (!user?.userId) {
            router.push('/login?redirect=' + encodeURIComponent(`/events/${id}`));
            return;
        }
        try {
            setIsJoiningWaitlist(true);
            setError(null);
            await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].addToWaitingList(id);
            setIsJoiningWaitlist(false);
            // Show success message or update UI
            alert('Successfully joined waitlist! You will be notified when a spot becomes available.');
        } catch (err) {
            console.error('Failed to join waitlist:', err);
            setError(err instanceof Error ? err.message : 'Failed to join waitlist. Please try again.');
            setIsJoiningWaitlist(false);
        }
    };
    // Handle Publish Event
    const handlePublishEvent = async ()=>{
        if (!event || event.organizerId !== user?.userId) {
            return;
        }
        try {
            setIsPublishing(true);
            setError(null);
            await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["eventsRepository"].publishEvent(id);
            setIsPublishing(false);
            // Reload page to show updated status
            window.location.reload();
        } catch (err) {
            console.error('Failed to publish event:', err);
            setError(err instanceof Error ? err.message : 'Failed to publish event. Please try again.');
            setIsPublishing(false);
        }
    };
    if (isLoading) {
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "min-h-screen bg-gradient-to-b from-neutral-50 to-white",
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Header$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Header"], {}, void 0, false, {
                    fileName: "[project]/src/app/events/[id]/page.tsx",
                    lineNumber: 143,
                    columnNumber: 9
                }, this),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12",
                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Card"], {
                        className: "animate-pulse",
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["CardContent"], {
                            className: "p-12",
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "h-8 bg-neutral-200 rounded w-3/4 mb-4"
                                }, void 0, false, {
                                    fileName: "[project]/src/app/events/[id]/page.tsx",
                                    lineNumber: 147,
                                    columnNumber: 15
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "h-4 bg-neutral-200 rounded w-1/2 mb-8"
                                }, void 0, false, {
                                    fileName: "[project]/src/app/events/[id]/page.tsx",
                                    lineNumber: 148,
                                    columnNumber: 15
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "h-64 bg-neutral-200 rounded mb-8"
                                }, void 0, false, {
                                    fileName: "[project]/src/app/events/[id]/page.tsx",
                                    lineNumber: 149,
                                    columnNumber: 15
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "h-32 bg-neutral-200 rounded"
                                }, void 0, false, {
                                    fileName: "[project]/src/app/events/[id]/page.tsx",
                                    lineNumber: 150,
                                    columnNumber: 15
                                }, this)
                            ]
                        }, void 0, true, {
                            fileName: "[project]/src/app/events/[id]/page.tsx",
                            lineNumber: 146,
                            columnNumber: 13
                        }, this)
                    }, void 0, false, {
                        fileName: "[project]/src/app/events/[id]/page.tsx",
                        lineNumber: 145,
                        columnNumber: 11
                    }, this)
                }, void 0, false, {
                    fileName: "[project]/src/app/events/[id]/page.tsx",
                    lineNumber: 144,
                    columnNumber: 9
                }, this),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Footer$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {}, void 0, false, {
                    fileName: "[project]/src/app/events/[id]/page.tsx",
                    lineNumber: 154,
                    columnNumber: 9
                }, this)
            ]
        }, void 0, true, {
            fileName: "[project]/src/app/events/[id]/page.tsx",
            lineNumber: 142,
            columnNumber: 7
        }, this);
    }
    if (fetchError || !event) {
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "min-h-screen bg-gradient-to-b from-neutral-50 to-white",
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Header$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Header"], {}, void 0, false, {
                    fileName: "[project]/src/app/events/[id]/page.tsx",
                    lineNumber: 162,
                    columnNumber: 9
                }, this),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12",
                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Card"], {
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["CardContent"], {
                            className: "p-12 text-center",
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$circle$2d$alert$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__AlertCircle$3e$__["AlertCircle"], {
                                    className: "h-16 w-16 mx-auto mb-4 text-destructive"
                                }, void 0, false, {
                                    fileName: "[project]/src/app/events/[id]/page.tsx",
                                    lineNumber: 166,
                                    columnNumber: 15
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
                                    className: "text-xl font-semibold text-neutral-900 mb-2",
                                    children: "Event Not Found"
                                }, void 0, false, {
                                    fileName: "[project]/src/app/events/[id]/page.tsx",
                                    lineNumber: 167,
                                    columnNumber: 15
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                    className: "text-neutral-500 mb-6",
                                    children: "The event you're looking for doesn't exist or has been removed."
                                }, void 0, false, {
                                    fileName: "[project]/src/app/events/[id]/page.tsx",
                                    lineNumber: 170,
                                    columnNumber: 15
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                    onClick: ()=>router.push('/events'),
                                    children: "Back to Events"
                                }, void 0, false, {
                                    fileName: "[project]/src/app/events/[id]/page.tsx",
                                    lineNumber: 173,
                                    columnNumber: 15
                                }, this)
                            ]
                        }, void 0, true, {
                            fileName: "[project]/src/app/events/[id]/page.tsx",
                            lineNumber: 165,
                            columnNumber: 13
                        }, this)
                    }, void 0, false, {
                        fileName: "[project]/src/app/events/[id]/page.tsx",
                        lineNumber: 164,
                        columnNumber: 11
                    }, this)
                }, void 0, false, {
                    fileName: "[project]/src/app/events/[id]/page.tsx",
                    lineNumber: 163,
                    columnNumber: 9
                }, this),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Footer$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {}, void 0, false, {
                    fileName: "[project]/src/app/events/[id]/page.tsx",
                    lineNumber: 179,
                    columnNumber: 9
                }, this)
            ]
        }, void 0, true, {
            fileName: "[project]/src/app/events/[id]/page.tsx",
            lineNumber: 161,
            columnNumber: 7
        }, this);
    }
    const startDate = new Date(event.startDate);
    const endDate = new Date(event.endDate);
    const formattedStartDate = startDate.toLocaleDateString('en-US', {
        month: 'long',
        day: 'numeric',
        year: 'numeric'
    });
    const formattedStartTime = startDate.toLocaleTimeString('en-US', {
        hour: 'numeric',
        minute: '2-digit'
    });
    const formattedEndTime = endDate.toLocaleTimeString('en-US', {
        hour: 'numeric',
        minute: '2-digit'
    });
    const isFull = event.currentRegistrations >= event.capacity;
    const spotsLeft = event.capacity - event.currentRegistrations;
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        className: "min-h-screen bg-gradient-to-b from-neutral-50 to-white",
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Header$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Header"], {}, void 0, false, {
                fileName: "[project]/src/app/events/[id]/page.tsx",
                lineNumber: 205,
                columnNumber: 7
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6",
                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "flex items-center justify-between gap-4",
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                            variant: "outline",
                            onClick: ()=>router.push('/events'),
                            className: "flex items-center gap-2",
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$arrow$2d$left$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__ArrowLeft$3e$__["ArrowLeft"], {
                                    className: "h-4 w-4"
                                }, void 0, false, {
                                    fileName: "[project]/src/app/events/[id]/page.tsx",
                                    lineNumber: 215,
                                    columnNumber: 13
                                }, this),
                                "Back to Events"
                            ]
                        }, void 0, true, {
                            fileName: "[project]/src/app/events/[id]/page.tsx",
                            lineNumber: 210,
                            columnNumber: 11
                        }, this),
                        event && user && event.organizerId === user.userId && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                            className: "flex items-center gap-3",
                            children: event.status === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["EventStatus"].Draft && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                onClick: handlePublishEvent,
                                disabled: isPublishing,
                                className: "flex items-center gap-2",
                                style: {
                                    background: '#10B981'
                                },
                                children: isPublishing ? 'Publishing...' : 'Publish Event'
                            }, void 0, false, {
                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                lineNumber: 224,
                                columnNumber: 17
                            }, this)
                        }, void 0, false, {
                            fileName: "[project]/src/app/events/[id]/page.tsx",
                            lineNumber: 221,
                            columnNumber: 13
                        }, this)
                    ]
                }, void 0, true, {
                    fileName: "[project]/src/app/events/[id]/page.tsx",
                    lineNumber: 209,
                    columnNumber: 9
                }, this)
            }, void 0, false, {
                fileName: "[project]/src/app/events/[id]/page.tsx",
                lineNumber: 208,
                columnNumber: 7
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pb-12",
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Card"], {
                        className: "overflow-hidden",
                        children: [
                            event.images && event.images.length > 0 && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "relative h-96 bg-gradient-to-br from-orange-500 to-rose-500",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("img", {
                                        src: event.images[0].imageUrl,
                                        alt: event.title,
                                        className: "w-full h-full object-cover"
                                    }, void 0, false, {
                                        fileName: "[project]/src/app/events/[id]/page.tsx",
                                        lineNumber: 244,
                                        columnNumber: 15
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "absolute top-4 right-4",
                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Badge$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Badge"], {
                                            variant: "default",
                                            className: "text-white shadow-lg text-base px-4 py-2",
                                            style: {
                                                background: '#8B1538'
                                            },
                                            children: categoryLabels[event.category]
                                        }, void 0, false, {
                                            fileName: "[project]/src/app/events/[id]/page.tsx",
                                            lineNumber: 250,
                                            columnNumber: 17
                                        }, this)
                                    }, void 0, false, {
                                        fileName: "[project]/src/app/events/[id]/page.tsx",
                                        lineNumber: 249,
                                        columnNumber: 15
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                lineNumber: 243,
                                columnNumber: 13
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["CardContent"], {
                                className: "p-8",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "mb-8",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("h1", {
                                                className: "text-4xl font-bold text-neutral-900 mb-4",
                                                children: event.title
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                lineNumber: 264,
                                                columnNumber: 15
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                className: "text-lg text-neutral-600 leading-relaxed whitespace-pre-wrap",
                                                children: event.description
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                lineNumber: 267,
                                                columnNumber: 15
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/events/[id]/page.tsx",
                                        lineNumber: 263,
                                        columnNumber: 13
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "grid grid-cols-1 md:grid-cols-2 gap-6 mb-8",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "flex items-start gap-3",
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        className: "p-3 rounded-lg",
                                                        style: {
                                                            background: '#FFF4ED'
                                                        },
                                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$calendar$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Calendar$3e$__["Calendar"], {
                                                            className: "h-6 w-6",
                                                            style: {
                                                                color: '#FF7900'
                                                            }
                                                        }, void 0, false, {
                                                            fileName: "[project]/src/app/events/[id]/page.tsx",
                                                            lineNumber: 277,
                                                            columnNumber: 19
                                                        }, this)
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/events/[id]/page.tsx",
                                                        lineNumber: 276,
                                                        columnNumber: 17
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-sm font-medium text-neutral-500",
                                                                children: "Date & Time"
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                                lineNumber: 280,
                                                                columnNumber: 19
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-base font-semibold text-neutral-900",
                                                                children: formattedStartDate
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                                lineNumber: 281,
                                                                columnNumber: 19
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-sm text-neutral-600",
                                                                children: [
                                                                    formattedStartTime,
                                                                    " - ",
                                                                    formattedEndTime
                                                                ]
                                                            }, void 0, true, {
                                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                                lineNumber: 284,
                                                                columnNumber: 19
                                                            }, this)
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/app/events/[id]/page.tsx",
                                                        lineNumber: 279,
                                                        columnNumber: 17
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                lineNumber: 275,
                                                columnNumber: 15
                                            }, this),
                                            event.city && event.state && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "flex items-start gap-3",
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        className: "p-3 rounded-lg",
                                                        style: {
                                                            background: '#FFF4ED'
                                                        },
                                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$map$2d$pin$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__MapPin$3e$__["MapPin"], {
                                                            className: "h-6 w-6",
                                                            style: {
                                                                color: '#FF7900'
                                                            }
                                                        }, void 0, false, {
                                                            fileName: "[project]/src/app/events/[id]/page.tsx",
                                                            lineNumber: 294,
                                                            columnNumber: 21
                                                        }, this)
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/events/[id]/page.tsx",
                                                        lineNumber: 293,
                                                        columnNumber: 19
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-sm font-medium text-neutral-500",
                                                                children: "Location"
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                                lineNumber: 297,
                                                                columnNumber: 21
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-base font-semibold text-neutral-900",
                                                                children: [
                                                                    event.city,
                                                                    ", ",
                                                                    event.state
                                                                ]
                                                            }, void 0, true, {
                                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                                lineNumber: 298,
                                                                columnNumber: 21
                                                            }, this),
                                                            event.address && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-sm text-neutral-600",
                                                                children: event.address
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                                lineNumber: 302,
                                                                columnNumber: 23
                                                            }, this)
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/app/events/[id]/page.tsx",
                                                        lineNumber: 296,
                                                        columnNumber: 19
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                lineNumber: 292,
                                                columnNumber: 17
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "flex items-start gap-3",
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        className: "p-3 rounded-lg",
                                                        style: {
                                                            background: '#FFF4ED'
                                                        },
                                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$users$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Users$3e$__["Users"], {
                                                            className: "h-6 w-6",
                                                            style: {
                                                                color: '#FF7900'
                                                            }
                                                        }, void 0, false, {
                                                            fileName: "[project]/src/app/events/[id]/page.tsx",
                                                            lineNumber: 311,
                                                            columnNumber: 19
                                                        }, this)
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/events/[id]/page.tsx",
                                                        lineNumber: 310,
                                                        columnNumber: 17
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-sm font-medium text-neutral-500",
                                                                children: "Capacity"
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                                lineNumber: 314,
                                                                columnNumber: 19
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-base font-semibold text-neutral-900",
                                                                children: [
                                                                    event.currentRegistrations,
                                                                    " / ",
                                                                    event.capacity,
                                                                    " registered"
                                                                ]
                                                            }, void 0, true, {
                                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                                lineNumber: 315,
                                                                columnNumber: 19
                                                            }, this),
                                                            isFull ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Badge$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Badge"], {
                                                                className: "mt-1 bg-red-600 text-white",
                                                                children: "Event Full"
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                                lineNumber: 319,
                                                                columnNumber: 21
                                                            }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-sm text-neutral-600",
                                                                children: [
                                                                    spotsLeft,
                                                                    " ",
                                                                    spotsLeft === 1 ? 'spot' : 'spots',
                                                                    " remaining"
                                                                ]
                                                            }, void 0, true, {
                                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                                lineNumber: 321,
                                                                columnNumber: 21
                                                            }, this)
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/app/events/[id]/page.tsx",
                                                        lineNumber: 313,
                                                        columnNumber: 17
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                lineNumber: 309,
                                                columnNumber: 15
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "flex items-start gap-3",
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        className: "p-3 rounded-lg",
                                                        style: {
                                                            background: '#FFF4ED'
                                                        },
                                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$dollar$2d$sign$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__DollarSign$3e$__["DollarSign"], {
                                                            className: "h-6 w-6",
                                                            style: {
                                                                color: '#FF7900'
                                                            }
                                                        }, void 0, false, {
                                                            fileName: "[project]/src/app/events/[id]/page.tsx",
                                                            lineNumber: 331,
                                                            columnNumber: 19
                                                        }, this)
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/events/[id]/page.tsx",
                                                        lineNumber: 330,
                                                        columnNumber: 17
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-sm font-medium text-neutral-500",
                                                                children: "Pricing"
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                                lineNumber: 334,
                                                                columnNumber: 19
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-base font-semibold",
                                                                style: {
                                                                    color: '#8B1538'
                                                                },
                                                                children: event.isFree ? 'Free Event' : `$${event.ticketPriceAmount?.toFixed(2)} per person`
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                                lineNumber: 335,
                                                                columnNumber: 19
                                                            }, this),
                                                            !event.isFree && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-sm text-neutral-600",
                                                                children: event.ticketPriceCurrency === 1 ? 'USD' : 'LKR'
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                                lineNumber: 339,
                                                                columnNumber: 21
                                                            }, this)
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/app/events/[id]/page.tsx",
                                                        lineNumber: 333,
                                                        columnNumber: 17
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                lineNumber: 329,
                                                columnNumber: 15
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/events/[id]/page.tsx",
                                        lineNumber: 273,
                                        columnNumber: 13
                                    }, this),
                                    (event.images && event.images.length > 0 || event.videos && event.videos.length > 0) && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "mb-8",
                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$events$2f$MediaGallery$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["MediaGallery"], {
                                            images: event.images,
                                            videos: event.videos
                                        }, void 0, false, {
                                            fileName: "[project]/src/app/events/[id]/page.tsx",
                                            lineNumber: 350,
                                            columnNumber: 17
                                        }, this)
                                    }, void 0, false, {
                                        fileName: "[project]/src/app/events/[id]/page.tsx",
                                        lineNumber: 349,
                                        columnNumber: 15
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Card"], {
                                        className: "border-2",
                                        style: {
                                            borderColor: '#FF7900'
                                        },
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["CardHeader"], {
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["CardTitle"], {
                                                        children: "Register for this Event"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/events/[id]/page.tsx",
                                                        lineNumber: 357,
                                                        columnNumber: 17
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["CardDescription"], {
                                                        children: isFull ? 'This event is currently full. Join the waitlist to be notified when spots become available.' : 'Reserve your spot now!'
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/events/[id]/page.tsx",
                                                        lineNumber: 358,
                                                        columnNumber: 17
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                lineNumber: 356,
                                                columnNumber: 15
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["CardContent"], {
                                                children: !isFull ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$events$2f$EventRegistrationForm$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["EventRegistrationForm"], {
                                                    eventId: id,
                                                    spotsLeft: spotsLeft,
                                                    isFree: event.isFree,
                                                    ticketPrice: event.ticketPriceAmount ?? undefined,
                                                    isProcessing: isProcessing,
                                                    onSubmit: handleRegistration,
                                                    error: error
                                                }, void 0, false, {
                                                    fileName: "[project]/src/app/events/[id]/page.tsx",
                                                    lineNumber: 366,
                                                    columnNumber: 19
                                                }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Fragment"], {
                                                    children: [
                                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                            className: "space-y-4",
                                                            children: [
                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Button"], {
                                                                    onClick: handleJoinWaitlist,
                                                                    disabled: isJoiningWaitlist,
                                                                    className: "w-full text-lg py-6",
                                                                    variant: "outline",
                                                                    style: {
                                                                        borderColor: '#FF7900',
                                                                        color: '#FF7900'
                                                                    },
                                                                    children: isJoiningWaitlist ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["Fragment"], {
                                                                        children: [
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$clock$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__$3c$export__default__as__Clock$3e$__["Clock"], {
                                                                                className: "h-5 w-5 mr-2 animate-spin"
                                                                            }, void 0, false, {
                                                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                                                lineNumber: 388,
                                                                                columnNumber: 29
                                                                            }, this),
                                                                            "Joining..."
                                                                        ]
                                                                    }, void 0, true) : 'Join Waitlist'
                                                                }, void 0, false, {
                                                                    fileName: "[project]/src/app/events/[id]/page.tsx",
                                                                    lineNumber: 379,
                                                                    columnNumber: 23
                                                                }, this),
                                                                !user?.userId && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                    className: "text-sm text-center text-neutral-500",
                                                                    children: "You'll be redirected to login before joining the waitlist"
                                                                }, void 0, false, {
                                                                    fileName: "[project]/src/app/events/[id]/page.tsx",
                                                                    lineNumber: 397,
                                                                    columnNumber: 25
                                                                }, this)
                                                            ]
                                                        }, void 0, true, {
                                                            fileName: "[project]/src/app/events/[id]/page.tsx",
                                                            lineNumber: 378,
                                                            columnNumber: 21
                                                        }, this),
                                                        error && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                            className: "mt-4 p-4 bg-red-50 border border-red-200 rounded-lg",
                                                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-sm text-red-600",
                                                                children: error
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                                lineNumber: 405,
                                                                columnNumber: 25
                                                            }, this)
                                                        }, void 0, false, {
                                                            fileName: "[project]/src/app/events/[id]/page.tsx",
                                                            lineNumber: 404,
                                                            columnNumber: 23
                                                        }, this)
                                                    ]
                                                }, void 0, true)
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                                lineNumber: 364,
                                                columnNumber: 15
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/events/[id]/page.tsx",
                                        lineNumber: 355,
                                        columnNumber: 13
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/app/events/[id]/page.tsx",
                                lineNumber: 261,
                                columnNumber: 11
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/app/events/[id]/page.tsx",
                        lineNumber: 240,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "mt-8",
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$events$2f$SignUpManagementSection$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["SignUpManagementSection"], {
                            eventId: id,
                            userId: user?.userId,
                            isOrganizer: event.organizerId === user?.userId
                        }, void 0, false, {
                            fileName: "[project]/src/app/events/[id]/page.tsx",
                            lineNumber: 417,
                            columnNumber: 11
                        }, this)
                    }, void 0, false, {
                        fileName: "[project]/src/app/events/[id]/page.tsx",
                        lineNumber: 416,
                        columnNumber: 9
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/app/events/[id]/page.tsx",
                lineNumber: 239,
                columnNumber: 7
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$compiled$2f$react$2f$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Footer$2e$tsx__$5b$app$2d$client$5d$__$28$ecmascript$29$__["default"], {}, void 0, false, {
                fileName: "[project]/src/app/events/[id]/page.tsx",
                lineNumber: 425,
                columnNumber: 7
            }, this)
        ]
    }, void 0, true, {
        fileName: "[project]/src/app/events/[id]/page.tsx",
        lineNumber: 204,
        columnNumber: 5
    }, this);
}
_s(EventDetailPage, "v/6mVaifLHNdboI+jFa9aN6bgiY=", false, function() {
    return [
        __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRouter"],
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useAuthStore"],
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEvents$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useEventById"],
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEvents$2e$ts__$5b$app$2d$client$5d$__$28$ecmascript$29$__["useRsvpToEvent"]
    ];
});
_c = EventDetailPage;
var _c;
__turbopack_context__.k.register(_c, "EventDetailPage");
if (typeof globalThis.$RefreshHelpers$ === 'object' && globalThis.$RefreshHelpers !== null) {
    __turbopack_context__.k.registerExports(__turbopack_context__.m, globalThis.$RefreshHelpers$);
}
}),
]);

//# sourceMappingURL=src_0b8b6525._.js.map