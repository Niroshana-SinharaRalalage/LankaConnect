module.exports = [
"[project]/src/presentation/lib/utils.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "cn",
    ()=>cn
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$clsx$2f$dist$2f$clsx$2e$mjs__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/clsx/dist/clsx.mjs [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$tailwind$2d$merge$2f$dist$2f$bundle$2d$mjs$2e$mjs__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/tailwind-merge/dist/bundle-mjs.mjs [app-ssr] (ecmascript)");
;
;
function cn(...inputs) {
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$tailwind$2d$merge$2f$dist$2f$bundle$2d$mjs$2e$mjs__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["twMerge"])((0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$clsx$2f$dist$2f$clsx$2e$mjs__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["clsx"])(inputs));
}
}),
"[project]/src/presentation/components/atoms/Logo.tsx [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "Logo",
    ()=>Logo
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react-jsx-dev-runtime.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$image$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/image.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-ssr] (ecmascript)");
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
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])('flex items-center gap-3', className),
        suppressHydrationWarning: true,
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])(sizeClasses[size], 'relative flex-shrink-0'),
                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$image$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {
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
            showText && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])('font-bold text-maroon', textSizeClasses[size]),
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
}),
"[project]/src/presentation/components/atoms/OfficialLogo.tsx [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "OfficialLogo",
    ()=>OfficialLogo
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react-jsx-dev-runtime.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/client/app-dir/link.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$atoms$2f$Logo$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/atoms/Logo.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-ssr] (ecmascript)");
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
    const logoContent = /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])('flex items-center', className),
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$atoms$2f$Logo$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Logo"], {
                size: config.logoSize,
                showText: false
            }, void 0, false, {
                fileName: "[project]/src/presentation/components/atoms/OfficialLogo.tsx",
                lineNumber: 50,
                columnNumber: 7
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: config.gap,
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])(config.titleSize, textColor),
                        children: "LankaConnect"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/atoms/OfficialLogo.tsx",
                        lineNumber: 52,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])(config.subtitleSize, subtitleColor, '-mt-1'),
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
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {
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
}),
"[project]/src/presentation/components/ui/Button.tsx [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "Button",
    ()=>Button,
    "buttonVariants",
    ()=>buttonVariants
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react-jsx-dev-runtime.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$class$2d$variance$2d$authority$2f$dist$2f$index$2e$mjs__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/class-variance-authority/dist/index.mjs [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-ssr] (ecmascript)");
;
;
;
;
const buttonVariants = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$class$2d$variance$2d$authority$2f$dist$2f$index$2e$mjs__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cva"])('inline-flex items-center justify-center whitespace-nowrap rounded-md text-sm font-medium ring-offset-background transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50', {
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
 */ const Button = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["forwardRef"](({ className, variant, size, loading, disabled, children, ...props }, ref)=>{
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])(buttonVariants({
            variant,
            size,
            className
        })),
        ref: ref,
        disabled: disabled || loading,
        "aria-disabled": disabled || loading,
        ...props,
        children: loading ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Fragment"], {
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("svg", {
                    className: "mr-2 h-4 w-4 animate-spin",
                    xmlns: "http://www.w3.org/2000/svg",
                    fill: "none",
                    viewBox: "0 0 24 24",
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("circle", {
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
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("path", {
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
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
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
Button.displayName = 'Button';
;
}),
"[project]/src/presentation/components/features/notifications/NotificationBell.tsx [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "NotificationBell",
    ()=>NotificationBell
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react-jsx-dev-runtime.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-ssr] (ecmascript)");
'use client';
;
;
function NotificationBell({ unreadCount, onClick, className = '' }) {
    const displayCount = unreadCount > 99 ? '99+' : unreadCount.toString();
    const hasUnread = unreadCount > 0;
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
        type: "button",
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])('relative flex items-center justify-center w-10 h-10 rounded-full', 'hover:bg-gray-100 transition-all duration-200', 'focus:outline-none focus:ring-2 focus:ring-[#8B1538] focus:ring-offset-2', className),
        onClick: onClick,
        "aria-label": `Notifications${hasUnread ? ` (${unreadCount} unread)` : ''}`,
        title: hasUnread ? `${unreadCount} unread notifications` : 'Notifications',
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("svg", {
                className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])('w-6 h-6 text-[#333]', hasUnread && 'animate-[bell-ring_1s_ease-in-out]'),
                fill: "none",
                stroke: "currentColor",
                viewBox: "0 0 24 24",
                xmlns: "http://www.w3.org/2000/svg",
                "aria-hidden": "true",
                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("path", {
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
            hasUnread && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])('absolute top-0 right-0', 'flex items-center justify-center', 'min-w-[20px] h-5 px-1', 'bg-[#FF7900] text-white text-xs font-bold rounded-full', 'border-2 border-white', 'animate-[badge-pop_0.3s_ease-out]'),
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
}),
"[project]/src/infrastructure/api/types/notifications.types.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
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
}),
"[project]/src/infrastructure/api/repositories/notifications.repository.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "NotificationsRepository",
    ()=>NotificationsRepository,
    "notificationsRepository",
    ()=>notificationsRepository
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/client/api-client.ts [app-ssr] (ecmascript)");
;
class NotificationsRepository {
    basePath = '/notifications';
    /**
   * Get unread notifications for the current user
   */ async getUnreadNotifications() {
        const response = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/unread`);
        return response;
    }
    /**
   * Mark a notification as read
   */ async markAsRead(notificationId) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${notificationId}/read`);
    }
    /**
   * Mark all notifications as read
   */ async markAllAsRead() {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/read-all`);
    }
}
const notificationsRepository = new NotificationsRepository();
}),
"[project]/src/presentation/hooks/useNotifications.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
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
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/useQuery.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/useMutation.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/QueryClientProvider.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$notifications$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/repositories/notifications.repository.ts [app-ssr] (ecmascript)");
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
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQuery"])({
        queryKey: notificationKeys.unread(),
        queryFn: async ()=>{
            const result = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$notifications$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["notificationsRepository"].getUnreadNotifications();
            return result;
        },
        staleTime: 1 * 60 * 1000,
        refetchInterval: 30 * 1000,
        refetchOnWindowFocus: true,
        retry: 1,
        ...options
    });
}
function useMarkNotificationAsRead() {
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: (notificationId)=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$notifications$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["notificationsRepository"].markAsRead(notificationId),
        onMutate: async (notificationId)=>{
            // Cancel outgoing refetches
            await queryClient.cancelQueries({
                queryKey: notificationKeys.unread()
            });
            // Snapshot previous value for rollback
            const previousNotifications = queryClient.getQueryData(notificationKeys.unread());
            // Optimistically remove the notification from unread list
            queryClient.setQueryData(notificationKeys.unread(), (old)=>old?.filter((n)=>n.id !== notificationId) || []);
            return {
                previousNotifications
            };
        },
        onError: (_err, _variables, context)=>{
            // Rollback on error
            if (context?.previousNotifications) {
                queryClient.setQueryData(notificationKeys.unread(), context.previousNotifications);
            }
        },
        onSuccess: ()=>{
            // Invalidate to ensure consistency with server
            queryClient.invalidateQueries({
                queryKey: notificationKeys.unread()
            });
        }
    });
}
function useMarkAllNotificationsAsRead() {
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: ()=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$notifications$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["notificationsRepository"].markAllAsRead(),
        onMutate: async ()=>{
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
        },
        onError: (_err, _variables, context)=>{
            // Rollback on error
            if (context?.previousNotifications) {
                queryClient.setQueryData(notificationKeys.unread(), context.previousNotifications);
            }
        },
        onSuccess: ()=>{
            // Invalidate to ensure consistency with server
            queryClient.invalidateQueries({
                queryKey: notificationKeys.unread()
            });
        }
    });
}
function useInvalidateNotifications() {
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return {
        all: ()=>queryClient.invalidateQueries({
                queryKey: notificationKeys.all
            }),
        unread: ()=>queryClient.invalidateQueries({
                queryKey: notificationKeys.unread()
            })
    };
}
const __TURBOPACK__default__export__ = {
    useUnreadNotifications,
    useMarkNotificationAsRead,
    useMarkAllNotificationsAsRead,
    useInvalidateNotifications
};
}),
"[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "NotificationDropdown",
    ()=>NotificationDropdown
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react-jsx-dev-runtime.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/client/app-dir/link.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$notifications$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/types/notifications.types.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useNotifications$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/hooks/useNotifications.ts [app-ssr] (ecmascript)");
'use client';
;
;
;
;
;
;
function NotificationDropdown({ notifications, isOpen, onClose, className = '' }) {
    const dropdownRef = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useRef"](null);
    const markAsRead = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useNotifications$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMarkNotificationAsRead"])();
    const markAllAsRead = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useNotifications$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMarkAllNotificationsAsRead"])();
    // Close dropdown on outside click
    __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useEffect"](()=>{
        if (!isOpen) return;
        const handleClickOutside = (event)=>{
            if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
                onClose();
            }
        };
        document.addEventListener('mousedown', handleClickOutside);
        return ()=>document.removeEventListener('mousedown', handleClickOutside);
    }, [
        isOpen,
        onClose
    ]);
    // Close on Escape key
    __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useEffect"](()=>{
        if (!isOpen) return;
        const handleEscape = (event)=>{
            if (event.key === 'Escape') {
                onClose();
            }
        };
        document.addEventListener('keydown', handleEscape);
        return ()=>document.removeEventListener('keydown', handleEscape);
    }, [
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
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        ref: dropdownRef,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])('absolute right-0 mt-2 w-80 sm:w-96', 'bg-white rounded-lg shadow-lg', 'border border-gray-200', 'z-50', 'animate-[dropdown-fade-in_0.2s_ease-out]', className),
        role: "menu",
        "aria-label": "Notifications menu",
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "flex items-center justify-between px-4 py-3 border-b border-gray-200",
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
                        className: "text-lg font-semibold text-[#8B1538]",
                        children: "Notifications"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                        lineNumber: 138,
                        columnNumber: 9
                    }, this),
                    notifications.length > 0 && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                        type: "button",
                        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])('text-sm text-[#FF7900] font-medium', 'hover:text-[#E66D00] transition-colors', 'focus:outline-none focus:underline'),
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
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "max-h-96 overflow-y-auto",
                children: notifications.length === 0 ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "px-4 py-8 text-center text-gray-500",
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("svg", {
                            className: "w-12 h-12 mx-auto mb-3 text-gray-400",
                            fill: "none",
                            stroke: "currentColor",
                            viewBox: "0 0 24 24",
                            xmlns: "http://www.w3.org/2000/svg",
                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("path", {
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
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                            className: "font-medium",
                            children: "No new notifications"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                            lineNumber: 173,
                            columnNumber: 13
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
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
                }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("ul", {
                    className: "divide-y divide-gray-100",
                    children: notifications.map((notification)=>{
                        const config = __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$notifications$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["notificationTypeConfig"][notification.type];
                        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("li", {
                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                type: "button",
                                className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])('w-full px-4 py-3 text-left', 'hover:bg-gray-50 transition-colors', 'focus:outline-none focus:bg-gray-50'),
                                onClick: ()=>handleNotificationClick(notification.id),
                                disabled: markAsRead.isPending,
                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "flex gap-3",
                                    children: [
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                            className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])('flex-shrink-0 w-10 h-10 rounded-full', 'flex items-center justify-center text-lg', config.bgColor, config.color),
                                            children: config.icon
                                        }, void 0, false, {
                                            fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                                            lineNumber: 194,
                                            columnNumber: 23
                                        }, this),
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                            className: "flex-1 min-w-0",
                                            children: [
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                    className: "text-sm font-semibold text-[#333] truncate",
                                                    children: notification.title
                                                }, void 0, false, {
                                                    fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                                                    lineNumber: 207,
                                                    columnNumber: 25
                                                }, this),
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                    className: "text-sm text-gray-600 mt-1 line-clamp-2",
                                                    children: notification.message
                                                }, void 0, false, {
                                                    fileName: "[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx",
                                                    lineNumber: 210,
                                                    columnNumber: 25
                                                }, this),
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
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
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                            className: "flex-shrink-0",
                                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
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
            notifications.length > 0 && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "px-4 py-3 border-t border-gray-200",
                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {
                    href: "/notifications",
                    className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])('block w-full text-center text-sm font-medium', 'text-[#8B1538] hover:text-[#70112D]', 'py-2 rounded-md hover:bg-gray-50', 'transition-colors'),
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
}),
"[project]/src/presentation/components/layout/Header.tsx [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "Header",
    ()=>Header
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react-jsx-dev-runtime.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/client/app-dir/link.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$image$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/image.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/navigation.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$atoms$2f$OfficialLogo$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/atoms/OfficialLogo.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Button.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/store/useAuthStore.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$notifications$2f$NotificationBell$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/features/notifications/NotificationBell.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$notifications$2f$NotificationDropdown$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/features/notifications/NotificationDropdown.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useNotifications$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/hooks/useNotifications.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$user$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__User$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/user.js [app-ssr] (ecmascript) <export default as User>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$log$2d$out$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__LogOut$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/log-out.js [app-ssr] (ecmascript) <export default as LogOut>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$down$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronDown$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/chevron-down.js [app-ssr] (ecmascript) <export default as ChevronDown>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$search$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Search$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/search.js [app-ssr] (ecmascript) <export default as Search>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$menu$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Menu$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/menu.js [app-ssr] (ecmascript) <export default as Menu>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$x$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__X$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/x.js [app-ssr] (ecmascript) <export default as X>");
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
function Header({ className = '' }) {
    const { user, isAuthenticated, clearAuth } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useAuthStore"])();
    const router = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useRouter"])();
    const [notificationDropdownOpen, setNotificationDropdownOpen] = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"](false);
    const [userMenuOpen, setUserMenuOpen] = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"](false);
    const [searchOpen, setSearchOpen] = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"](false);
    const [mobileMenuOpen, setMobileMenuOpen] = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"](false);
    const userMenuRef = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useRef"](null);
    const searchRef = __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useRef"](null);
    // Fetch unread notifications only when authenticated
    const { data: unreadNotifications = [] } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useNotifications$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useUnreadNotifications"])({
        enabled: isAuthenticated
    });
    // Close dropdowns when clicking outside
    __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useEffect"](()=>{
        function handleClickOutside(event) {
            if (userMenuRef.current && !userMenuRef.current.contains(event.target)) {
                setUserMenuOpen(false);
            }
            if (searchRef.current && !searchRef.current.contains(event.target)) {
                setSearchOpen(false);
            }
        }
        document.addEventListener('mousedown', handleClickOutside);
        return ()=>document.removeEventListener('mousedown', handleClickOutside);
    }, []);
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
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("header", {
        className: `sticky top-0 z-50 bg-white shadow-[0_2px_10px_rgba(0,0,0,0.08)] ${className}`,
        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("nav", {
            className: "container mx-auto px-4 sm:px-6 lg:px-8",
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "flex items-center justify-between py-4",
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$atoms$2f$OfficialLogo$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["OfficialLogo"], {
                            size: "md"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/layout/Header.tsx",
                            lineNumber: 75,
                            columnNumber: 11
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                            onClick: ()=>setMobileMenuOpen(!mobileMenuOpen),
                            className: "lg:hidden p-2 text-[#333] hover:text-[#FF7900] transition-colors",
                            "aria-label": "Toggle menu",
                            children: mobileMenuOpen ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$x$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__X$3e$__["X"], {
                                className: "w-6 h-6"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                lineNumber: 84,
                                columnNumber: 15
                            }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$menu$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Menu$3e$__["Menu"], {
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
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("nav", {
                            className: "hidden lg:flex items-center gap-6",
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {
                                    href: "/events",
                                    className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors",
                                    children: "Events"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                    lineNumber: 92,
                                    columnNumber: 13
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {
                                    href: "/forums",
                                    className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors",
                                    children: "Forums"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                    lineNumber: 98,
                                    columnNumber: 13
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {
                                    href: "/business",
                                    className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors",
                                    children: "Business"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                    lineNumber: 104,
                                    columnNumber: 13
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {
                                    href: "/marketplace",
                                    className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors",
                                    children: "Marketplace"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                    lineNumber: 110,
                                    columnNumber: 13
                                }, this),
                                isAuthenticated && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {
                                    href: "/dashboard",
                                    className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors",
                                    children: "Dashboard"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                    lineNumber: 117,
                                    columnNumber: 15
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "relative",
                                    ref: searchRef,
                                    children: [
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                            onClick: ()=>setSearchOpen(!searchOpen),
                                            className: "p-2 text-[#333] hover:text-[#FF7900] transition-colors",
                                            "aria-label": "Search",
                                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$search$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Search$3e$__["Search"], {
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
                                        searchOpen && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                            className: "absolute right-0 mt-2 w-80 bg-white rounded-lg shadow-lg border border-gray-200 p-4 z-50",
                                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
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
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                            className: "flex items-center gap-4",
                            children: isAuthenticated && user ? // Authenticated: Show notification bell and user avatar with dropdown
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex items-center gap-3",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "relative",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$notifications$2f$NotificationBell$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["NotificationBell"], {
                                                unreadCount: unreadNotifications.length,
                                                onClick: ()=>setNotificationDropdownOpen(!notificationDropdownOpen)
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                lineNumber: 155,
                                                columnNumber: 19
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$notifications$2f$NotificationDropdown$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["NotificationDropdown"], {
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
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "relative",
                                        ref: userMenuRef,
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
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
                                                    user.profilePhotoUrl ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$image$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {
                                                        src: user.profilePhotoUrl,
                                                        alt: user.fullName,
                                                        width: 40,
                                                        height: 40,
                                                        className: "w-10 h-10 rounded-full object-cover"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                        lineNumber: 182,
                                                        columnNumber: 23
                                                    }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
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
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                        className: "text-sm font-medium text-[#333] hidden lg:inline",
                                                        children: user.fullName
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                        lineNumber: 201,
                                                        columnNumber: 21
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$down$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronDown$3e$__["ChevronDown"], {
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
                                            userMenuOpen && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "absolute right-0 mt-2 w-48 rounded-lg shadow-lg overflow-hidden z-50",
                                                style: {
                                                    background: 'white',
                                                    border: '1px solid #e2e8f0'
                                                },
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        className: "px-4 py-3 border-b border-gray-200",
                                                        style: {
                                                            background: '#f7fafc'
                                                        },
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                className: "text-sm font-medium text-[#333]",
                                                                children: user.fullName
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                                lineNumber: 224,
                                                                columnNumber: 25
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
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
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                                        onClick: ()=>{
                                                            setUserMenuOpen(false);
                                                            router.push('/profile');
                                                        },
                                                        className: "w-full flex items-center gap-3 px-4 py-3 hover:bg-gray-50 transition-colors text-left",
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$user$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__User$3e$__["User"], {
                                                                className: "w-4 h-4",
                                                                style: {
                                                                    color: '#FF7900'
                                                                }
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                                lineNumber: 236,
                                                                columnNumber: 25
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
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
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        style: {
                                                            borderTop: '1px solid #e2e8f0'
                                                        }
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                        lineNumber: 240,
                                                        columnNumber: 23
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                                        onClick: async ()=>{
                                                            setUserMenuOpen(false);
                                                            try {
                                                                // Call logout endpoint if needed
                                                                const { authRepository } = await __turbopack_context__.A("[project]/src/infrastructure/api/repositories/auth.repository.ts [app-ssr] (ecmascript, async loader)");
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
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$log$2d$out$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__LogOut$3e$__["LogOut"], {
                                                                className: "w-4 h-4",
                                                                style: {
                                                                    color: '#8B1538'
                                                                }
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                                                lineNumber: 259,
                                                                columnNumber: 25
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
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
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Fragment"], {
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
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
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
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
                mobileMenuOpen && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "lg:hidden border-t border-gray-200 py-4",
                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "flex flex-col gap-4",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {
                                href: "/events",
                                className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors px-4 py-2 hover:bg-gray-50 rounded-lg",
                                onClick: ()=>setMobileMenuOpen(false),
                                children: "Events"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                lineNumber: 295,
                                columnNumber: 15
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {
                                href: "/forums",
                                className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors px-4 py-2 hover:bg-gray-50 rounded-lg",
                                onClick: ()=>setMobileMenuOpen(false),
                                children: "Forums"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                lineNumber: 302,
                                columnNumber: 15
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {
                                href: "/business",
                                className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors px-4 py-2 hover:bg-gray-50 rounded-lg",
                                onClick: ()=>setMobileMenuOpen(false),
                                children: "Business"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                lineNumber: 309,
                                columnNumber: 15
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {
                                href: "/marketplace",
                                className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors px-4 py-2 hover:bg-gray-50 rounded-lg",
                                onClick: ()=>setMobileMenuOpen(false),
                                children: "Marketplace"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                lineNumber: 316,
                                columnNumber: 15
                            }, this),
                            isAuthenticated && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {
                                href: "/dashboard",
                                className: "text-[#333] hover:text-[#FF7900] font-medium transition-colors px-4 py-2 hover:bg-gray-50 rounded-lg",
                                onClick: ()=>setMobileMenuOpen(false),
                                children: "Dashboard"
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/layout/Header.tsx",
                                lineNumber: 324,
                                columnNumber: 17
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "px-4",
                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
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
                            !isAuthenticated && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex flex-col gap-2 px-4 pt-4 border-t border-gray-200",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
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
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
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
}),
"[project]/src/domain/constants/metroAreas.constants.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
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
}),
"[project]/src/presentation/components/ui/TreeDropdown.tsx [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "TreeDropdown",
    ()=>TreeDropdown
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react-jsx-dev-runtime.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$down$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronDown$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/chevron-down.js [app-ssr] (ecmascript) <export default as ChevronDown>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$right$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronRight$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/chevron-right.js [app-ssr] (ecmascript) <export default as ChevronRight>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$check$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Check$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/check.js [app-ssr] (ecmascript) <export default as Check>");
'use client';
;
;
;
function TreeDropdown({ nodes, selectedIds, onSelectionChange, placeholder = 'Select items', maxSelections, disabled = false, className = '' }) {
    const [isOpen, setIsOpen] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])(false);
    const [expandedNodes, setExpandedNodes] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])(new Set());
    const dropdownRef = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useRef"])(null);
    // Close dropdown when clicking outside
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useEffect"])(()=>{
        function handleClickOutside(event) {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
                setIsOpen(false);
            }
        }
        if (isOpen) {
            document.addEventListener('mousedown', handleClickOutside);
            return ()=>document.removeEventListener('mousedown', handleClickOutside);
        }
    }, [
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
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: `flex items-center gap-2 px-3 py-2 hover:bg-gray-50 cursor-pointer ${indentClass}`,
                    style: {
                        paddingLeft: `${level * 24 + 12}px`
                    },
                    children: [
                        hasChildren ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                            type: "button",
                            onClick: (e)=>{
                                e.stopPropagation();
                                toggleNode(node.id);
                            },
                            className: "p-0.5 hover:bg-gray-200 rounded",
                            "aria-label": isExpanded ? 'Collapse' : 'Expand',
                            children: isExpanded ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$down$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronDown$3e$__["ChevronDown"], {
                                className: "h-4 w-4",
                                style: {
                                    color: '#FF7900'
                                }
                            }, void 0, false, {
                                fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                                lineNumber: 187,
                                columnNumber: 17
                            }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$right$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronRight$3e$__["ChevronRight"], {
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
                        }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                            className: "w-5"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                            lineNumber: 193,
                            columnNumber: 13
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                            className: "flex items-center gap-2 flex-1 cursor-pointer",
                            onClick: (e)=>e.stopPropagation(),
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
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
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                    className: "text-sm",
                                    children: node.label
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                                    lineNumber: 211,
                                    columnNumber: 13
                                }, this),
                                isSelected && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$check$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Check$3e$__["Check"], {
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
                hasChildren && isExpanded && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
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
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        ref: dropdownRef,
        className: `relative ${className}`,
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
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
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                        className: selectedCount === 0 ? 'text-gray-500' : 'text-gray-900',
                        children: displayText
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                        lineNumber: 248,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$chevron$2d$down$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__ChevronDown$3e$__["ChevronDown"], {
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
            isOpen && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "absolute z-50 w-full mt-2 bg-white border-2 rounded-lg shadow-lg max-h-96 overflow-y-auto",
                style: {
                    borderColor: '#FF7900'
                },
                role: "listbox",
                children: [
                    nodes.length === 0 ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "px-4 py-3 text-sm text-gray-500 text-center",
                        children: "No items available"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                        lineNumber: 265,
                        columnNumber: 13
                    }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "py-1",
                        children: nodes.map((node)=>renderTreeNode(node))
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/ui/TreeDropdown.tsx",
                        lineNumber: 269,
                        columnNumber: 13
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "px-4 py-2 border-t bg-gray-50 flex items-center justify-between",
                        style: {
                            borderColor: '#e2e8f0'
                        },
                        children: [
                            maxSelections && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
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
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
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
}),
"[project]/src/infrastructure/api/repositories/metro-areas.repository.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "metroAreasRepository",
    ()=>metroAreasRepository
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/client/api-client.ts [app-ssr] (ecmascript)");
;
const metroAreasRepository = {
    /**
   * Get all active metro areas
   * Endpoint: GET /api/metro-areas?activeOnly=true
   */ async getAll (activeOnly = true) {
        const data = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].get('/metro-areas', {
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
}),
"[project]/src/presentation/hooks/useMetroAreas.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "useMetroAreas",
    ()=>useMetroAreas
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$metro$2d$areas$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/repositories/metro-areas.repository.ts [app-ssr] (ecmascript)");
;
;
function useMetroAreas() {
    const [metroAreas, setMetroAreas] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])([]);
    const [isLoading, setIsLoading] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])(true);
    const [error, setError] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])(null);
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useEffect"])(()=>{
        let isMounted = true;
        async function fetchMetroAreas() {
            try {
                console.log('[useMetroAreas] Starting to fetch metro areas...');
                setIsLoading(true);
                setError(null);
                const data = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$metro$2d$areas$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["metroAreasRepository"].getAll(true);
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
        return ()=>{
            isMounted = false;
        };
    }, []);
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
}),
"[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "NewsletterMetroSelector",
    ()=>NewsletterMetroSelector
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react-jsx-dev-runtime.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$domain$2f$constants$2f$metroAreas$2e$constants$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/domain/constants/metroAreas.constants.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$TreeDropdown$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/TreeDropdown.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useMetroAreas$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/hooks/useMetroAreas.ts [app-ssr] (ecmascript)");
'use client';
;
;
;
;
;
function NewsletterMetroSelector({ selectedMetroIds, receiveAllLocations, onMetrosChange, onReceiveAllChange, disabled = false, maxSelections = 20 }) {
    const [validationError, setValidationError] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])('');
    // Phase 6A.9: Fetch metro areas from API instead of hardcoded constants
    const { metroAreasByState, isLoading: metrosLoading, error: metrosError } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useMetroAreas$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMetroAreas"])();
    // Check validation whenever selectedMetroIds changes
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useEffect"])(()=>{
        if (selectedMetroIds.length > maxSelections) {
            setValidationError(`You cannot select more than ${maxSelections} metro areas`);
        } else {
            setValidationError('');
        }
    }, [
        selectedMetroIds,
        maxSelections
    ]);
    /**
   * Transform metro areas data into TreeNode format for TreeDropdown
   * Each state becomes a parent node, city metros become children
   */ const treeNodes = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMemo"])(()=>{
        const nodes = [];
        for (const state of __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$domain$2f$constants$2f$metroAreas$2e$constants$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["US_STATES"]){
            const metrosForState = metroAreasByState.get(state.code) || [];
            // Filter out state-level metros (like "All Alabama")
            // Note: After database cleanup, there should be no state-level metros
            const cityMetros = metrosForState.filter((m)=>!m.isStateLevelArea);
            // Only include states that have city metros
            if (cityMetros.length === 0) continue;
            // Create child nodes for each metro
            const children = cityMetros.map((metro)=>({
                    id: metro.id,
                    label: metro.name,
                    checked: selectedMetroIds.includes(metro.id)
                }));
            // Create parent node for the state
            nodes.push({
                id: `state-${state.code}`,
                label: state.name,
                checked: children.every((child)=>selectedMetroIds.includes(child.id)),
                children
            });
        }
        return nodes;
    }, [
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
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "space-y-4",
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                            className: "text-sm font-medium text-gray-700 mb-2 block",
                            children: "Get notifications for events in:"
                        }, void 0, false, {
                            fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                            lineNumber: 116,
                            columnNumber: 11
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
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
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "flex items-center justify-center p-4",
                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
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
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "space-y-4",
            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                        className: "text-sm font-medium text-gray-700 mb-2 block",
                        children: "Get notifications for events in:"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                        lineNumber: 133,
                        columnNumber: 11
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
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
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        className: "space-y-4",
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                        className: "text-sm font-medium text-gray-700 mb-2 block",
                        children: "Get notifications for events in:"
                    }, void 0, false, {
                        fileName: "[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx",
                        lineNumber: 148,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
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
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "mb-4",
                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                    className: "flex items-center text-sm text-gray-700 cursor-pointer",
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
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
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
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
            !receiveAllLocations && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "space-y-3",
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$TreeDropdown$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["TreeDropdown"], {
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
                    validationError && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
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
}),
"[project]/src/presentation/components/layout/Footer.tsx [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "default",
    ()=>__TURBOPACK__default__export__
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react-jsx-dev-runtime.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/client/app-dir/link.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$atoms$2f$Logo$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/atoms/Logo.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$newsletter$2f$NewsletterMetroSelector$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$facebook$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Facebook$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/facebook.js [app-ssr] (ecmascript) <export default as Facebook>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$twitter$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Twitter$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/twitter.js [app-ssr] (ecmascript) <export default as Twitter>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$instagram$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Instagram$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/instagram.js [app-ssr] (ecmascript) <export default as Instagram>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$youtube$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Youtube$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/youtube.js [app-ssr] (ecmascript) <export default as Youtube>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$mail$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Mail$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/mail.js [app-ssr] (ecmascript) <export default as Mail>");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/store/useAuthStore.ts [app-ssr] (ecmascript)");
'use client';
;
;
;
;
;
;
;
const FooterLink = ({ href, children, external = false })=>{
    const linkClasses = "text-white/80 hover:text-white transition-colors duration-200 text-sm";
    if (external) {
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
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
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$client$2f$app$2d$dir$2f$link$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {
        href: href,
        className: linkClasses,
        children: children
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
        lineNumber: 34,
        columnNumber: 5
    }, ("TURBOPACK compile-time value", void 0));
};
const Footer = ()=>{
    const { isAuthenticated } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useAuthStore"])();
    const [email, setEmail] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])('');
    const [subscribeStatus, setSubscribeStatus] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])('idle');
    const [selectedMetroIds, setSelectedMetroIds] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])([]);
    const [receiveAllLocations, setReceiveAllLocations] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])(false);
    const [currentYear, setCurrentYear] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])(2025);
    // Set current year on client side only to avoid hydration mismatch
    __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"].useEffect(()=>{
        setCurrentYear(new Date().getFullYear());
    }, []);
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
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("footer", {
        className: "bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 text-white mt-24 relative overflow-hidden",
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "absolute inset-0 opacity-10",
                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
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
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12",
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "bg-white/10 backdrop-blur-sm rounded-2xl p-8 mb-12 border border-white/20",
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                            className: "max-w-xl mx-auto",
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
                                    className: "text-2xl font-semibold mb-2 text-center",
                                    children: "Stay Connected"
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                    lineNumber: 178,
                                    columnNumber: 13
                                }, ("TURBOPACK compile-time value", void 0)),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                    className: "text-white/90 mb-6 text-center",
                                    children: "Subscribe to our newsletter for the latest events and community updates."
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                    lineNumber: 179,
                                    columnNumber: 13
                                }, ("TURBOPACK compile-time value", void 0)),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("form", {
                                    onSubmit: handleNewsletterSubmit,
                                    className: "space-y-4",
                                    children: [
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
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
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                            className: "bg-white/95 p-4 rounded-lg text-gray-800",
                                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$features$2f$newsletter$2f$NewsletterMetroSelector$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["NewsletterMetroSelector"], {
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
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
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
                                subscribeStatus === 'error' && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                    className: "text-red-300 text-sm mt-2 text-center",
                                    role: "alert",
                                    children: "Please enter a valid email address and select at least one location."
                                }, void 0, false, {
                                    fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                    lineNumber: 214,
                                    columnNumber: 15
                                }, ("TURBOPACK compile-time value", void 0)),
                                subscribeStatus === 'success' && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
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
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "grid grid-cols-2 md:grid-cols-4 gap-8 mb-12",
                        children: linkCategories.map((category)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("h4", {
                                        className: "text-white font-semibold mb-4",
                                        children: category.title
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 230,
                                        columnNumber: 15
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("ul", {
                                        className: "space-y-2",
                                        role: "list",
                                        children: category.links.map((link)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("li", {
                                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(FooterLink, {
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
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "pt-8 border-t border-white/20 flex flex-col md:flex-row items-center justify-between gap-4",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex items-center gap-3",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$atoms$2f$Logo$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Logo"], {
                                        size: "md",
                                        showText: false
                                    }, void 0, false, {
                                        fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                        lineNumber: 247,
                                        columnNumber: 13
                                    }, ("TURBOPACK compile-time value", void 0)),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "text-white font-semibold",
                                                children: "LankaConnect"
                                            }, void 0, false, {
                                                fileName: "[project]/src/presentation/components/layout/Footer.tsx",
                                                lineNumber: 249,
                                                columnNumber: 15
                                            }, ("TURBOPACK compile-time value", void 0)),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
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
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex items-center gap-3",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
                                        href: "https://facebook.com",
                                        target: "_blank",
                                        rel: "noopener noreferrer",
                                        className: "text-white/80 hover:text-white hover:bg-white/10 p-2 rounded-lg transition-colors",
                                        "aria-label": "Facebook",
                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$facebook$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Facebook$3e$__["Facebook"], {
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
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
                                        href: "https://twitter.com",
                                        target: "_blank",
                                        rel: "noopener noreferrer",
                                        className: "text-white/80 hover:text-white hover:bg-white/10 p-2 rounded-lg transition-colors",
                                        "aria-label": "Twitter",
                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$twitter$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Twitter$3e$__["Twitter"], {
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
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
                                        href: "https://instagram.com",
                                        target: "_blank",
                                        rel: "noopener noreferrer",
                                        className: "text-white/80 hover:text-white hover:bg-white/10 p-2 rounded-lg transition-colors",
                                        "aria-label": "Instagram",
                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$instagram$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Instagram$3e$__["Instagram"], {
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
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
                                        href: "https://youtube.com",
                                        target: "_blank",
                                        rel: "noopener noreferrer",
                                        className: "text-white/80 hover:text-white hover:bg-white/10 p-2 rounded-lg transition-colors",
                                        "aria-label": "YouTube",
                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$youtube$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Youtube$3e$__["Youtube"], {
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
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("a", {
                                        href: "mailto:contact@lankaconnect.com",
                                        className: "text-white/80 hover:text-white hover:bg-white/10 p-2 rounded-lg transition-colors",
                                        "aria-label": "Email",
                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$mail$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Mail$3e$__["Mail"], {
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
const __TURBOPACK__default__export__ = Footer;
}),
"[project]/src/presentation/components/ui/Card.tsx [app-ssr] (ecmascript)", ((__turbopack_context__) => {
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
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react-jsx-dev-runtime.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-ssr] (ecmascript)");
;
;
;
/**
 * Card Component
 * Reusable card container with header, content, and footer sections
 * Follows UI/UX best practices for content grouping
 */ const Card = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["forwardRef"](({ className, ...props }, ref)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])('rounded-lg border bg-card text-card-foreground shadow-sm', className),
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Card.tsx",
        lineNumber: 11,
        columnNumber: 5
    }, ("TURBOPACK compile-time value", void 0)));
Card.displayName = 'Card';
const CardHeader = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["forwardRef"](({ className, ...props }, ref)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])('flex flex-col space-y-1.5 p-6', className),
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Card.tsx",
        lineNumber: 22,
        columnNumber: 5
    }, ("TURBOPACK compile-time value", void 0)));
CardHeader.displayName = 'CardHeader';
const CardTitle = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["forwardRef"](({ className, ...props }, ref)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])('text-2xl font-semibold leading-none tracking-tight', className),
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Card.tsx",
        lineNumber: 29,
        columnNumber: 5
    }, ("TURBOPACK compile-time value", void 0)));
CardTitle.displayName = 'CardTitle';
const CardDescription = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["forwardRef"](({ className, ...props }, ref)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])('text-sm text-muted-foreground', className),
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Card.tsx",
        lineNumber: 42,
        columnNumber: 3
    }, ("TURBOPACK compile-time value", void 0)));
CardDescription.displayName = 'CardDescription';
const CardContent = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["forwardRef"](({ className, ...props }, ref)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])('p-6 pt-0', className),
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Card.tsx",
        lineNumber: 48,
        columnNumber: 5
    }, ("TURBOPACK compile-time value", void 0)));
CardContent.displayName = 'CardContent';
const CardFooter = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["forwardRef"](({ className, ...props }, ref)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])('flex items-center p-6 pt-0', className),
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Card.tsx",
        lineNumber: 55,
        columnNumber: 5
    }, ("TURBOPACK compile-time value", void 0)));
CardFooter.displayName = 'CardFooter';
;
}),
"[project]/src/presentation/components/ui/Badge.tsx [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "Badge",
    ()=>Badge,
    "badgeVariants",
    ()=>badgeVariants
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react-jsx-dev-runtime.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$class$2d$variance$2d$authority$2f$dist$2f$index$2e$mjs__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/class-variance-authority/dist/index.mjs [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-ssr] (ecmascript)");
;
;
;
;
const badgeVariants = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$class$2d$variance$2d$authority$2f$dist$2f$index$2e$mjs__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cva"])('inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium transition-colors', {
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
 */ const Badge = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["forwardRef"](({ className, variant, ...props }, ref)=>{
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
        ref: ref,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])(badgeVariants({
            variant
        }), className),
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Badge.tsx",
        lineNumber: 59,
        columnNumber: 7
    }, ("TURBOPACK compile-time value", void 0));
});
Badge.displayName = 'Badge';
;
}),
"[project]/src/infrastructure/api/repositories/events.repository.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "EventsRepository",
    ()=>EventsRepository,
    "eventsRepository",
    ()=>eventsRepository
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/client/api-client.ts [app-ssr] (ecmascript)");
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
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].get(url);
    }
    /**
   * Get event by ID
   * Maps to backend GetEventByIdQuery
   */ async getEventById(id) {
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/${id}`);
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
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/search?${params.toString()}`);
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
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/nearby?${params.toString()}`);
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
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].get(url);
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
        const eventId = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post(this.basePath, data);
        return eventId;
    }
    /**
   * Update an existing event
   * Requires authentication and ownership
   * Maps to backend UpdateEventCommand
   */ async updateEvent(id, data) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].put(`${this.basePath}/${id}`, data);
    }
    /**
   * Delete an event
   * Requires authentication and ownership
   * Only allowed for Draft/Cancelled events
   */ async deleteEvent(id) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].delete(`${this.basePath}/${id}`);
    }
    /**
   * Submit event for approval (if approval workflow is enabled)
   */ async submitForApproval(id) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${id}/submit`);
    }
    /**
   * Publish event (make it visible to public)
   * Requires authentication and ownership
   */ async publishEvent(id) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${id}/publish`);
    }
    /**
   * Cancel event with reason
   * Notifies all registered users
   */ async cancelEvent(id, reason) {
        const request = {
            reason
        };
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${id}/cancel`, request);
    }
    /**
   * Postpone event with reason
   * Changes status to Postponed
   */ async postponeEvent(id, reason) {
        const request = {
            reason
        };
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${id}/postpone`, request);
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
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/rsvp`, request);
    }
    /**
   * Cancel RSVP
   * Removes registration and frees up capacity
   */ async cancelRsvp(eventId) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].delete(`${this.basePath}/${eventId}/rsvp`);
    }
    /**
   * Update RSVP quantity
   * Changes number of attendees for registration
   */ async updateRsvp(eventId, userId, newQuantity) {
        const request = {
            userId,
            newQuantity
        };
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].put(`${this.basePath}/${eventId}/rsvp`, request);
    }
    /**
   * Get current user's RSVPs
   * Epic 1: Backend now returns full EventDto[] instead of RsvpDto[] for better UX
   * Returns all events user has registered for
   */ async getUserRsvps() {
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/my-rsvps`);
    }
    /**
   * Get upcoming events for user
   * Returns events happening in the future
   */ async getUpcomingEvents() {
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/upcoming`);
    }
    /**
   * Get events created by current user
   * Returns all events user has created as organizer
   */ async getUserCreatedEvents() {
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/my-events`);
    }
    // ==================== WAITING LIST ====================
    /**
   * Add user to waiting list
   * Used when event is at capacity
   */ async addToWaitingList(eventId) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/waiting-list`);
    }
    /**
   * Remove user from waiting list
   */ async removeFromWaitingList(eventId) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].delete(`${this.basePath}/${eventId}/waiting-list`);
    }
    /**
   * Get waiting list for event
   * Returns list of users waiting for spots
   */ async getWaitingList(eventId) {
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/${eventId}/waiting-list`);
    }
    // ==================== SIGN-UP MANAGEMENT ====================
    /**
   * Get all sign-up lists for an event
   * Returns sign-up lists with commitments
   * Maps to backend GET /api/events/{id}/signups
   */ async getEventSignUpLists(eventId) {
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/${eventId}/signups`);
    }
    /**
   * Add a sign-up list to event
   * Organizer-only operation
   * Maps to backend POST /api/events/{id}/signups
   */ async addSignUpList(eventId, request) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/signups`, request);
    }
    /**
   * Remove a sign-up list from event
   * Organizer-only operation
   * Maps to backend DELETE /api/events/{eventId}/signups/{signupId}
   */ async removeSignUpList(eventId, signupId) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].delete(`${this.basePath}/${eventId}/signups/${signupId}`);
    }
    /**
   * Commit to bringing an item to event
   * User commits to sign-up list
   * Maps to backend POST /api/events/{eventId}/signups/{signupId}/commit
   */ async commitToSignUp(eventId, signupId, request) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/signups/${signupId}/commit`, request);
    }
    /**
   * Cancel user's commitment to sign-up list
   * Maps to backend DELETE /api/events/{eventId}/signups/{signupId}/commit
   */ async cancelCommitment(eventId, signupId, request) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].delete(`${this.basePath}/${eventId}/signups/${signupId}/commit`, {
            data: request
        });
    }
    // ==================== CATEGORY-BASED SIGN-UP MANAGEMENT ====================
    /**
   * Add a category-based sign-up list to event
   * Organizer-only operation
   * Maps to backend POST /api/events/{id}/signups/categories
   */ async addSignUpListWithCategories(eventId, request) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/signups/categories`, request);
    }
    /**
   * Add an item to a category-based sign-up list
   * Organizer-only operation
   * Maps to backend POST /api/events/{eventId}/signups/{signupId}/items
   */ async addSignUpItem(eventId, signupId, request) {
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/signups/${signupId}/items`, request);
    }
    /**
   * Remove an item from a category-based sign-up list
   * Organizer-only operation
   * Maps to backend DELETE /api/events/{eventId}/signups/{signupId}/items/{itemId}
   */ async removeSignUpItem(eventId, signupId, itemId) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].delete(`${this.basePath}/${eventId}/signups/${signupId}/items/${itemId}`);
    }
    /**
   * User commits to bringing a specific item
   * Maps to backend POST /api/events/{eventId}/signups/{signupId}/items/{itemId}/commit
   */ async commitToSignUpItem(eventId, signupId, itemId, request) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/signups/${signupId}/items/${itemId}/commit`, request);
    }
    // ==================== MEDIA OPERATIONS ====================
    /**
   * Upload image to event gallery
   * Uses multipart/form-data for file upload
   */ async uploadEventImage(eventId, file) {
        const formData = new FormData();
        formData.append('image', file);
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].postMultipart(`${this.basePath}/${eventId}/images`, formData);
    }
    /**
   * Delete image from event gallery
   */ async deleteEventImage(eventId, imageId) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].delete(`${this.basePath}/${eventId}/images/${imageId}`);
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
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/share`, {
            platform
        });
    }
}
const eventsRepository = new EventsRepository();
}),
"[project]/src/presentation/hooks/useEvents.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
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
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/useQuery.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/useMutation.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/QueryClientProvider.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/repositories/events.repository.ts [app-ssr] (ecmascript)");
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
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQuery"])({
        queryKey: eventKeys.list(filters || {}),
        queryFn: async ()=>{
            const result = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].getEvents(filters);
            return result;
        },
        staleTime: 5 * 60 * 1000,
        refetchOnWindowFocus: true,
        retry: 1,
        ...options
    });
}
function useEventById(id, options) {
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQuery"])({
        queryKey: eventKeys.detail(id || ''),
        queryFn: ()=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].getEventById(id),
        enabled: !!id,
        staleTime: 10 * 60 * 1000,
        refetchOnWindowFocus: true,
        ...options
    });
}
function useSearchEvents(searchTerm, options) {
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQuery"])({
        queryKey: eventKeys.search(searchTerm || ''),
        queryFn: ()=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].searchEvents({
                searchTerm: searchTerm,
                page: 1,
                pageSize: 20
            }),
        enabled: !!searchTerm && searchTerm.length >= 2,
        staleTime: 2 * 60 * 1000,
        refetchOnWindowFocus: false,
        ...options
    });
}
function useFeaturedEvents(userId, latitude, longitude, options) {
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQuery"])({
        queryKey: eventKeys.featured(userId, latitude, longitude),
        queryFn: ()=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].getFeaturedEvents(userId, latitude, longitude),
        staleTime: 5 * 60 * 1000,
        refetchOnWindowFocus: true,
        retry: 1,
        ...options
    });
}
function useCreateEvent() {
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: (data)=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].createEvent(data),
        onSuccess: ()=>{
            // Invalidate all event lists to refetch with new event
            queryClient.invalidateQueries({
                queryKey: eventKeys.lists()
            });
        }
    });
}
function useUpdateEvent() {
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: ({ id, ...data })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].updateEvent(id, data),
        onMutate: async ({ id, ...newData })=>{
            // Cancel outgoing refetches
            await queryClient.cancelQueries({
                queryKey: eventKeys.detail(id)
            });
            // Snapshot previous value for rollback
            const previousEvent = queryClient.getQueryData(eventKeys.detail(id));
            // Optimistically update
            queryClient.setQueryData(eventKeys.detail(id), (old)=>{
                if (!old) return old;
                return {
                    ...old,
                    ...newData
                };
            });
            return {
                previousEvent
            };
        },
        onError: (err, { id }, context)=>{
            // Rollback on error
            if (context?.previousEvent) {
                queryClient.setQueryData(eventKeys.detail(id), context.previousEvent);
            }
        },
        onSuccess: (_data, variables)=>{
            // Invalidate affected queries
            queryClient.invalidateQueries({
                queryKey: eventKeys.detail(variables.id)
            });
            queryClient.invalidateQueries({
                queryKey: eventKeys.lists()
            });
        }
    });
}
function useDeleteEvent() {
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: (id)=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].deleteEvent(id),
        onMutate: async (id)=>{
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
        },
        onError: (err, id, context)=>{
            // Restore on error
            if (context?.previousEvent) {
                queryClient.setQueryData(eventKeys.detail(id), context.previousEvent);
            }
        },
        onSuccess: ()=>{
            // Invalidate lists
            queryClient.invalidateQueries({
                queryKey: eventKeys.lists()
            });
        }
    });
}
function useRsvpToEvent() {
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: (data)=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].rsvpToEvent(data.eventId, data.userId, data.quantity),
        onMutate: async ({ eventId })=>{
            // Cancel queries
            await queryClient.cancelQueries({
                queryKey: eventKeys.detail(eventId)
            });
            // Snapshot
            const previousEvent = queryClient.getQueryData(eventKeys.detail(eventId));
            // Optimistically update RSVP count
            queryClient.setQueryData(eventKeys.detail(eventId), (old)=>{
                if (!old) return old;
                return {
                    ...old,
                    currentRegistrations: old.currentRegistrations + 1
                };
            });
            return {
                previousEvent
            };
        },
        onError: (err, { eventId }, context)=>{
            // Rollback
            if (context?.previousEvent) {
                queryClient.setQueryData(eventKeys.detail(eventId), context.previousEvent);
            }
        },
        onSuccess: (_data, variables)=>{
            // Refetch to get accurate data from server
            queryClient.invalidateQueries({
                queryKey: eventKeys.detail(variables.eventId)
            });
        }
    });
}
function usePrefetchEvent() {
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (id)=>{
        queryClient.prefetchQuery({
            queryKey: eventKeys.detail(id),
            queryFn: ()=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].getEventById(id),
            staleTime: 10 * 60 * 1000
        });
    };
}
function useInvalidateEvents() {
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQueryClient"])();
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
}),
"[project]/src/presentation/hooks/useGeolocation.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "useGeolocation",
    ()=>useGeolocation
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react.js [app-ssr] (ecmascript)");
;
function useGeolocation(enabled = true) {
    const [state, setState] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])({
        latitude: null,
        longitude: null,
        loading: enabled,
        error: null
    });
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useEffect"])(()=>{
        if (!enabled) {
            setState({
                latitude: null,
                longitude: null,
                loading: false,
                error: null
            });
            return;
        }
        let isMounted = true;
        const getLocation = async ()=>{
            // Priority 1: Try browser Geolocation API (most accurate)
            if ('geolocation' in navigator) {
                navigator.geolocation.getCurrentPosition((position)=>{
                    if (isMounted) {
                        setState({
                            latitude: position.coords.latitude,
                            longitude: position.coords.longitude,
                            loading: false,
                            error: null
                        });
                    }
                }, async (error)=>{
                    // Browser geolocation failed/denied - fallback to IP geolocation
                    console.warn('Browser geolocation failed:', error.message);
                    await getIpBasedLocation();
                }, {
                    timeout: 10000,
                    maximumAge: 300000,
                    enableHighAccuracy: false
                });
            } else {
                // Browser doesn't support geolocation - use IP fallback
                await getIpBasedLocation();
            }
        };
        const getIpBasedLocation = async ()=>{
            try {
                // Using ipapi.co - free, no API key required
                // Rate limit: 1000 requests/day for free tier
                const response = await fetch('https://ipapi.co/json/', {
                    method: 'GET',
                    headers: {
                        'Accept': 'application/json'
                    }
                });
                if (!response.ok) {
                    throw new Error(`IP geolocation API error: ${response.status}`);
                }
                const data = await response.json();
                if (isMounted && data.latitude && data.longitude) {
                    setState({
                        latitude: data.latitude,
                        longitude: data.longitude,
                        loading: false,
                        error: null
                    });
                } else {
                    throw new Error('Invalid response from IP geolocation service');
                }
            } catch (err) {
                console.error('IP-based geolocation failed:', err);
                if (isMounted) {
                    setState({
                        latitude: null,
                        longitude: null,
                        loading: false,
                        error: err instanceof Error ? err.message : 'Failed to detect location'
                    });
                }
            }
        };
        getLocation();
        return ()=>{
            isMounted = false;
        };
    }, [
        enabled
    ]);
    return state;
}
}),
"[project]/src/infrastructure/api/types/events.types.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
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
}),
"[project]/src/presentation/utils/dateRanges.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

/**
 * Date Range Utilities
 * Provides helper functions for calculating date ranges used in filtering
 *
 * Phase: Events Page Enhancement
 * Feature: Advanced date filtering (this week, next week, next month)
 */ __turbopack_context__.s([
    "getDateRangeForOption",
    ()=>getDateRangeForOption,
    "getDateRangeLabel",
    ()=>getDateRangeLabel,
    "getNextMonthRange",
    ()=>getNextMonthRange,
    "getNextWeekRange",
    ()=>getNextWeekRange,
    "getThisWeekRange",
    ()=>getThisWeekRange,
    "getUpcomingRange",
    ()=>getUpcomingRange
]);
/**
 * Gets the start of today (midnight)
 */ function getStartOfToday() {
    const now = new Date();
    now.setHours(0, 0, 0, 0);
    return now;
}
/**
 * Gets the end of today (23:59:59.999)
 */ function getEndOfToday() {
    const now = new Date();
    now.setHours(23, 59, 59, 999);
    return now;
}
/**
 * Gets the start of the current week (Sunday)
 */ function getStartOfWeek(date = new Date()) {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day;
    d.setDate(diff);
    d.setHours(0, 0, 0, 0);
    return d;
}
/**
 * Gets the end of the current week (Saturday)
 */ function getEndOfWeek(date = new Date()) {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() + (6 - day);
    d.setDate(diff);
    d.setHours(23, 59, 59, 999);
    return d;
}
/**
 * Gets the start of the current month
 */ function getStartOfMonth(date = new Date()) {
    const d = new Date(date);
    d.setDate(1);
    d.setHours(0, 0, 0, 0);
    return d;
}
/**
 * Gets the end of the current month
 */ function getEndOfMonth(date = new Date()) {
    const d = new Date(date);
    d.setMonth(d.getMonth() + 1);
    d.setDate(0);
    d.setHours(23, 59, 59, 999);
    return d;
}
/**
 * Adds days to a date
 */ function addDays(date, days) {
    const result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
}
/**
 * Adds months to a date
 */ function addMonths(date, months) {
    const result = new Date(date);
    result.setMonth(result.getMonth() + months);
    return result;
}
function getThisWeekRange() {
    const startDateFrom = getStartOfToday().toISOString();
    const startDateTo = getEndOfWeek().toISOString();
    return {
        startDateFrom,
        startDateTo
    };
}
function getNextWeekRange() {
    const today = new Date();
    const nextWeekStart = addDays(getStartOfWeek(today), 7);
    const nextWeekEnd = addDays(getEndOfWeek(today), 7);
    return {
        startDateFrom: nextWeekStart.toISOString(),
        startDateTo: nextWeekEnd.toISOString()
    };
}
function getNextMonthRange() {
    const today = new Date();
    const nextMonth = addMonths(today, 1);
    const nextMonthStart = getStartOfMonth(nextMonth);
    const nextMonthEnd = getEndOfMonth(nextMonth);
    return {
        startDateFrom: nextMonthStart.toISOString(),
        startDateTo: nextMonthEnd.toISOString()
    };
}
function getUpcomingRange() {
    return {
        startDateFrom: new Date().toISOString()
    };
}
function getDateRangeForOption(option) {
    switch(option){
        case 'all':
            return {}; // No date filtering
        case 'upcoming':
            return getUpcomingRange();
        case 'thisWeek':
            return getThisWeekRange();
        case 'nextWeek':
            return getNextWeekRange();
        case 'nextMonth':
            return getNextMonthRange();
        default:
            return getUpcomingRange(); // Default to upcoming
    }
}
function getDateRangeLabel(option) {
    switch(option){
        case 'all':
            return 'All Events';
        case 'upcoming':
            return 'Upcoming Events';
        case 'thisWeek':
            return 'This Week';
        case 'nextWeek':
            return 'Next Week';
        case 'nextMonth':
            return 'Next Month';
        default:
            return 'Upcoming Events';
    }
}
}),
"[project]/src/infrastructure/api/types/auth.types.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
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
    UserRole["BusinessOwner"] = "BusinessOwner";
    UserRole["EventOrganizer"] = "EventOrganizer";
    UserRole["EventOrganizerAndBusinessOwner"] = "EventOrganizerAndBusinessOwner";
    UserRole["Admin"] = "Admin";
    UserRole["AdminManager"] = "AdminManager";
    return UserRole;
}({});
}),
"[project]/src/app/events/page.tsx [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "default",
    ()=>EventsPage
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react-jsx-dev-runtime.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/navigation.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Header$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/layout/Header.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Footer$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/layout/Footer.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Card.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Badge$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Badge.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Button.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$TreeDropdown$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/TreeDropdown.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$calendar$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Calendar$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/calendar.js [app-ssr] (ecmascript) <export default as Calendar>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$map$2d$pin$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__MapPin$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/map-pin.js [app-ssr] (ecmascript) <export default as MapPin>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$users$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Users$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/users.js [app-ssr] (ecmascript) <export default as Users>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$dollar$2d$sign$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__DollarSign$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/dollar-sign.js [app-ssr] (ecmascript) <export default as DollarSign>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$funnel$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Filter$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/funnel.js [app-ssr] (ecmascript) <export default as Filter>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$plus$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Plus$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/plus.js [app-ssr] (ecmascript) <export default as Plus>");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEvents$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/hooks/useEvents.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/store/useAuthStore.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useGeolocation$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/hooks/useGeolocation.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useMetroAreas$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/hooks/useMetroAreas.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/types/events.types.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$domain$2f$constants$2f$metroAreas$2e$constants$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/domain/constants/metroAreas.constants.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$utils$2f$dateRanges$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/utils/dateRanges.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/types/auth.types.ts [app-ssr] (ecmascript)");
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
;
;
function EventsPage() {
    const router = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useRouter"])();
    const { user } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useAuthStore"])();
    // For anonymous users, detect location via IP/browser geolocation
    const isAnonymous = !user?.userId;
    const { latitude, longitude, loading: locationLoading } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useGeolocation$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useGeolocation"])(isAnonymous);
    // Metro areas data for location filter
    const { metroAreasByState, stateLevelMetros, isLoading: metrosLoading } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useMetroAreas$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMetroAreas"])();
    // Filter states
    const [selectedCategory, setSelectedCategory] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])(undefined);
    const [selectedMetroIds, setSelectedMetroIds] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])([]);
    const [selectedState, setSelectedState] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])(undefined);
    const [dateRangeOption, setDateRangeOption] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])('upcoming');
    // Build filters for useEvents hook
    const filters = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMemo"])(()=>{
        const dateRange = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$utils$2f$dateRanges$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["getDateRangeForOption"])(dateRangeOption);
        return {
            category: selectedCategory,
            userId: user?.userId,
            latitude: isAnonymous ? latitude ?? undefined : undefined,
            longitude: isAnonymous ? longitude ?? undefined : undefined,
            metroAreaIds: selectedMetroIds.length > 0 ? selectedMetroIds : undefined,
            state: selectedState,
            ...dateRange
        };
    }, [
        selectedCategory,
        user?.userId,
        isAnonymous,
        latitude,
        longitude,
        selectedMetroIds,
        selectedState,
        dateRangeOption
    ]);
    // Fetch events with location-based sorting and filters
    const { data: events, isLoading: eventsLoading, error: eventsError } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEvents$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useEvents"])(filters);
    // Convert metro areas to tree structure for TreeDropdown
    const locationTreeNodes = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMemo"])(()=>{
        const nodes = [];
        __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$domain$2f$constants$2f$metroAreas$2e$constants$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["US_STATES"].forEach((state)=>{
            const metrosForState = metroAreasByState.get(state.code) || [];
            if (metrosForState.length === 0) return;
            // Separate state-level and city-level metros
            const stateMetro = metrosForState.find((m)=>m.isStateLevelArea);
            const cityMetros = metrosForState.filter((m)=>!m.isStateLevelArea);
            const children = [];
            // Add state-level metro first if it exists
            if (stateMetro) {
                children.push({
                    id: stateMetro.id,
                    label: `All of ${state.name}`,
                    checked: selectedMetroIds.includes(stateMetro.id)
                });
            }
            // Add city-level metros
            cityMetros.forEach((metro)=>{
                children.push({
                    id: metro.id,
                    label: metro.name,
                    checked: selectedMetroIds.includes(metro.id)
                });
            });
            nodes.push({
                id: state.code,
                label: state.name,
                checked: false,
                children
            });
        });
        return nodes;
    }, [
        metroAreasByState,
        selectedMetroIds
    ]);
    const handleLocationChange = (newSelectedIds)=>{
        setSelectedMetroIds(newSelectedIds);
    };
    const handleCategoryChange = (e)=>{
        const value = e.target.value;
        setSelectedCategory(value === '' ? undefined : Number(value));
    };
    const handleDateRangeChange = (e)=>{
        setDateRangeOption(e.target.value);
    };
    const clearFilters = ()=>{
        setSelectedCategory(undefined);
        setSelectedMetroIds([]);
        setSelectedState(undefined);
        setDateRangeOption('upcoming');
    };
    const hasActiveFilters = selectedCategory !== undefined || selectedMetroIds.length > 0 || selectedState !== undefined || dateRangeOption !== 'upcoming';
    // Category labels
    const categoryLabels = {
        [__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["EventCategory"].Religious]: 'Religious',
        [__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["EventCategory"].Cultural]: 'Cultural',
        [__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["EventCategory"].Community]: 'Community',
        [__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["EventCategory"].Educational]: 'Educational',
        [__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["EventCategory"].Social]: 'Social',
        [__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["EventCategory"].Business]: 'Business',
        [__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["EventCategory"].Charity]: 'Charity',
        [__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["EventCategory"].Entertainment]: 'Entertainment'
    };
    const isLoading = eventsLoading || isAnonymous && locationLoading || metrosLoading;
    // Check if user can create events (EventOrganizer, Admin, or AdminManager)
    const canUserCreateEvents = user && (user.role === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["UserRole"].EventOrganizer || user.role === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["UserRole"].Admin || user.role === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["UserRole"].AdminManager);
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        className: "min-h-screen bg-gradient-to-b from-neutral-50 to-white",
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Header$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Header"], {}, void 0, false, {
                fileName: "[project]/src/app/events/page.tsx",
                lineNumber: 158,
                columnNumber: 7
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 py-12 relative overflow-hidden",
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "absolute inset-0 opacity-10",
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                            className: "absolute inset-0",
                            style: {
                                backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`
                            }
                        }, void 0, false, {
                            fileName: "[project]/src/app/events/page.tsx",
                            lineNumber: 164,
                            columnNumber: 11
                        }, this)
                    }, void 0, false, {
                        fileName: "[project]/src/app/events/page.tsx",
                        lineNumber: 163,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "absolute inset-0 overflow-hidden",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "absolute -top-24 -left-24 w-96 h-96 bg-orange-400/20 rounded-full blur-3xl"
                            }, void 0, false, {
                                fileName: "[project]/src/app/events/page.tsx",
                                lineNumber: 174,
                                columnNumber: 11
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "absolute -bottom-24 -right-24 w-96 h-96 bg-emerald-400/20 rounded-full blur-3xl"
                            }, void 0, false, {
                                fileName: "[project]/src/app/events/page.tsx",
                                lineNumber: 175,
                                columnNumber: 11
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-rose-400/10 rounded-full blur-3xl"
                            }, void 0, false, {
                                fileName: "[project]/src/app/events/page.tsx",
                                lineNumber: 176,
                                columnNumber: 11
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/app/events/page.tsx",
                        lineNumber: 173,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8",
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                            className: "flex items-center justify-between",
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                    className: "flex-1",
                                    children: [
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("h1", {
                                            className: "text-4xl font-bold text-white mb-4",
                                            children: "Discover Events"
                                        }, void 0, false, {
                                            fileName: "[project]/src/app/events/page.tsx",
                                            lineNumber: 182,
                                            columnNumber: 15
                                        }, this),
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                            className: "text-lg text-white/90 max-w-2xl",
                                            children: "Find cultural, community, and social events relevant to you"
                                        }, void 0, false, {
                                            fileName: "[project]/src/app/events/page.tsx",
                                            lineNumber: 185,
                                            columnNumber: 15
                                        }, this)
                                    ]
                                }, void 0, true, {
                                    fileName: "[project]/src/app/events/page.tsx",
                                    lineNumber: 181,
                                    columnNumber: 13
                                }, this),
                                canUserCreateEvents && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
                                    onClick: ()=>router.push('/events/create'),
                                    className: "flex items-center gap-2",
                                    style: {
                                        background: '#FF7900'
                                    },
                                    children: [
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$plus$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Plus$3e$__["Plus"], {
                                            className: "h-5 w-5"
                                        }, void 0, false, {
                                            fileName: "[project]/src/app/events/page.tsx",
                                            lineNumber: 196,
                                            columnNumber: 17
                                        }, this),
                                        "Create Event"
                                    ]
                                }, void 0, true, {
                                    fileName: "[project]/src/app/events/page.tsx",
                                    lineNumber: 191,
                                    columnNumber: 15
                                }, this)
                            ]
                        }, void 0, true, {
                            fileName: "[project]/src/app/events/page.tsx",
                            lineNumber: 180,
                            columnNumber: 11
                        }, this)
                    }, void 0, false, {
                        fileName: "[project]/src/app/events/page.tsx",
                        lineNumber: 179,
                        columnNumber: 9
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/app/events/page.tsx",
                lineNumber: 161,
                columnNumber: 7
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8",
                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Card"], {
                    children: [
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardHeader"], {
                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex items-center justify-between",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "flex items-center gap-2",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$funnel$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Filter$3e$__["Filter"], {
                                                className: "h-5 w-5",
                                                style: {
                                                    color: '#FF7900'
                                                }
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/page.tsx",
                                                lineNumber: 210,
                                                columnNumber: 17
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardTitle"], {
                                                style: {
                                                    color: '#8B1538'
                                                },
                                                children: "Filters"
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/page.tsx",
                                                lineNumber: 211,
                                                columnNumber: 17
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/events/page.tsx",
                                        lineNumber: 209,
                                        columnNumber: 15
                                    }, this),
                                    hasActiveFilters && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                        onClick: clearFilters,
                                        className: "text-sm font-medium hover:underline",
                                        style: {
                                            color: '#FF7900'
                                        },
                                        children: "Clear All"
                                    }, void 0, false, {
                                        fileName: "[project]/src/app/events/page.tsx",
                                        lineNumber: 214,
                                        columnNumber: 17
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/app/events/page.tsx",
                                lineNumber: 208,
                                columnNumber: 13
                            }, this)
                        }, void 0, false, {
                            fileName: "[project]/src/app/events/page.tsx",
                            lineNumber: 207,
                            columnNumber: 11
                        }, this),
                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardContent"], {
                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "grid grid-cols-1 md:grid-cols-3 gap-4",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                className: "block text-sm font-medium text-neutral-700 mb-2",
                                                children: "Event Type"
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/page.tsx",
                                                lineNumber: 228,
                                                columnNumber: 17
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("select", {
                                                value: selectedCategory ?? '',
                                                onChange: handleCategoryChange,
                                                className: "w-full px-4 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500",
                                                disabled: isLoading,
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("option", {
                                                        value: "",
                                                        children: "All Types"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/events/page.tsx",
                                                        lineNumber: 237,
                                                        columnNumber: 19
                                                    }, this),
                                                    Object.entries(categoryLabels).map(([value, label])=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("option", {
                                                            value: value,
                                                            children: label
                                                        }, value, false, {
                                                            fileName: "[project]/src/app/events/page.tsx",
                                                            lineNumber: 239,
                                                            columnNumber: 21
                                                        }, this))
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/app/events/page.tsx",
                                                lineNumber: 231,
                                                columnNumber: 17
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/events/page.tsx",
                                        lineNumber: 227,
                                        columnNumber: 15
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                className: "block text-sm font-medium text-neutral-700 mb-2",
                                                children: "Event Date"
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/page.tsx",
                                                lineNumber: 248,
                                                columnNumber: 17
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("select", {
                                                value: dateRangeOption,
                                                onChange: handleDateRangeChange,
                                                className: "w-full px-4 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500",
                                                disabled: isLoading,
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("option", {
                                                        value: "upcoming",
                                                        children: "Upcoming Events"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/events/page.tsx",
                                                        lineNumber: 257,
                                                        columnNumber: 19
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("option", {
                                                        value: "thisWeek",
                                                        children: "This Week"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/events/page.tsx",
                                                        lineNumber: 258,
                                                        columnNumber: 19
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("option", {
                                                        value: "nextWeek",
                                                        children: "Next Week"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/events/page.tsx",
                                                        lineNumber: 259,
                                                        columnNumber: 19
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("option", {
                                                        value: "nextMonth",
                                                        children: "Next Month"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/events/page.tsx",
                                                        lineNumber: 260,
                                                        columnNumber: 19
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("option", {
                                                        value: "all",
                                                        children: "All Events"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/events/page.tsx",
                                                        lineNumber: 261,
                                                        columnNumber: 19
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/app/events/page.tsx",
                                                lineNumber: 251,
                                                columnNumber: 17
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/events/page.tsx",
                                        lineNumber: 247,
                                        columnNumber: 15
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                className: "block text-sm font-medium text-neutral-700 mb-2",
                                                children: "Location (State/Metro Area)"
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/page.tsx",
                                                lineNumber: 267,
                                                columnNumber: 17
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$TreeDropdown$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["TreeDropdown"], {
                                                nodes: locationTreeNodes,
                                                selectedIds: selectedMetroIds,
                                                onSelectionChange: handleLocationChange,
                                                placeholder: "Select location",
                                                maxSelections: 20,
                                                disabled: isLoading || metrosLoading,
                                                className: "w-full"
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/page.tsx",
                                                lineNumber: 270,
                                                columnNumber: 17
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/events/page.tsx",
                                        lineNumber: 266,
                                        columnNumber: 15
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/app/events/page.tsx",
                                lineNumber: 225,
                                columnNumber: 13
                            }, this)
                        }, void 0, false, {
                            fileName: "[project]/src/app/events/page.tsx",
                            lineNumber: 224,
                            columnNumber: 11
                        }, this)
                    ]
                }, void 0, true, {
                    fileName: "[project]/src/app/events/page.tsx",
                    lineNumber: 206,
                    columnNumber: 9
                }, this)
            }, void 0, false, {
                fileName: "[project]/src/app/events/page.tsx",
                lineNumber: 205,
                columnNumber: 7
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pb-12",
                children: isLoading ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6",
                    children: [
                        ...Array(6)
                    ].map((_, i)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Card"], {
                            className: "animate-pulse",
                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardContent"], {
                                className: "p-6",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "w-full h-48 bg-neutral-200 rounded-lg mb-4"
                                    }, void 0, false, {
                                        fileName: "[project]/src/app/events/page.tsx",
                                        lineNumber: 292,
                                        columnNumber: 19
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "h-6 bg-neutral-200 rounded w-3/4 mb-2"
                                    }, void 0, false, {
                                        fileName: "[project]/src/app/events/page.tsx",
                                        lineNumber: 293,
                                        columnNumber: 19
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "h-4 bg-neutral-200 rounded w-1/2 mb-4"
                                    }, void 0, false, {
                                        fileName: "[project]/src/app/events/page.tsx",
                                        lineNumber: 294,
                                        columnNumber: 19
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "h-4 bg-neutral-200 rounded w-full"
                                    }, void 0, false, {
                                        fileName: "[project]/src/app/events/page.tsx",
                                        lineNumber: 295,
                                        columnNumber: 19
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/app/events/page.tsx",
                                lineNumber: 291,
                                columnNumber: 17
                            }, this)
                        }, i, false, {
                            fileName: "[project]/src/app/events/page.tsx",
                            lineNumber: 290,
                            columnNumber: 15
                        }, this))
                }, void 0, false, {
                    fileName: "[project]/src/app/events/page.tsx",
                    lineNumber: 288,
                    columnNumber: 11
                }, this) : eventsError ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Card"], {
                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardContent"], {
                        className: "p-12 text-center",
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                            className: "text-destructive text-lg",
                            children: "Failed to load events. Please try again later."
                        }, void 0, false, {
                            fileName: "[project]/src/app/events/page.tsx",
                            lineNumber: 303,
                            columnNumber: 15
                        }, this)
                    }, void 0, false, {
                        fileName: "[project]/src/app/events/page.tsx",
                        lineNumber: 302,
                        columnNumber: 13
                    }, this)
                }, void 0, false, {
                    fileName: "[project]/src/app/events/page.tsx",
                    lineNumber: 301,
                    columnNumber: 11
                }, this) : !events || events.length === 0 ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Card"], {
                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardContent"], {
                        className: "p-12 text-center",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$calendar$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Calendar$3e$__["Calendar"], {
                                className: "h-16 w-16 mx-auto mb-4 text-neutral-400"
                            }, void 0, false, {
                                fileName: "[project]/src/app/events/page.tsx",
                                lineNumber: 311,
                                columnNumber: 15
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
                                className: "text-xl font-semibold text-neutral-900 mb-2",
                                children: "No Events Found"
                            }, void 0, false, {
                                fileName: "[project]/src/app/events/page.tsx",
                                lineNumber: 312,
                                columnNumber: 15
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                className: "text-neutral-500",
                                children: hasActiveFilters ? 'Try adjusting your filters to see more events.' : 'Check back soon for new events!'
                            }, void 0, false, {
                                fileName: "[project]/src/app/events/page.tsx",
                                lineNumber: 315,
                                columnNumber: 15
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/app/events/page.tsx",
                        lineNumber: 310,
                        columnNumber: 13
                    }, this)
                }, void 0, false, {
                    fileName: "[project]/src/app/events/page.tsx",
                    lineNumber: 309,
                    columnNumber: 11
                }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6",
                    children: events.map((event)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(EventCard, {
                            event: event,
                            categoryLabels: categoryLabels
                        }, event.id, false, {
                            fileName: "[project]/src/app/events/page.tsx",
                            lineNumber: 325,
                            columnNumber: 15
                        }, this))
                }, void 0, false, {
                    fileName: "[project]/src/app/events/page.tsx",
                    lineNumber: 323,
                    columnNumber: 11
                }, this)
            }, void 0, false, {
                fileName: "[project]/src/app/events/page.tsx",
                lineNumber: 286,
                columnNumber: 7
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Footer$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {}, void 0, false, {
                fileName: "[project]/src/app/events/page.tsx",
                lineNumber: 331,
                columnNumber: 7
            }, this)
        ]
    }, void 0, true, {
        fileName: "[project]/src/app/events/page.tsx",
        lineNumber: 157,
        columnNumber: 5
    }, this);
}
/**
 * Event Card Component
 * Displays individual event with image, title, date, location, category, and pricing
 */ function EventCard({ event, categoryLabels }) {
    const startDate = new Date(event.startDate);
    const formattedDate = startDate.toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
    });
    const formattedTime = startDate.toLocaleTimeString('en-US', {
        hour: 'numeric',
        minute: '2-digit'
    });
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Card"], {
        className: "hover:shadow-xl transition-all hover:-translate-y-1 cursor-pointer overflow-hidden",
        onClick: ()=>window.location.href = `/events/${event.id}`,
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "relative h-48 bg-gradient-to-br from-orange-500 to-rose-500",
                children: [
                    event.images && event.images.length > 0 ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("img", {
                        src: event.images[0].imageUrl,
                        alt: event.title,
                        className: "w-full h-full object-cover"
                    }, void 0, false, {
                        fileName: "[project]/src/app/events/page.tsx",
                        lineNumber: 366,
                        columnNumber: 11
                    }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "w-full h-full flex items-center justify-center text-6xl text-white",
                        children: "🎉"
                    }, void 0, false, {
                        fileName: "[project]/src/app/events/page.tsx",
                        lineNumber: 372,
                        columnNumber: 11
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "absolute top-3 right-3",
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Badge$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Badge"], {
                            variant: "default",
                            className: "text-white shadow-lg",
                            style: {
                                background: '#8B1538'
                            },
                            children: categoryLabels[event.category]
                        }, void 0, false, {
                            fileName: "[project]/src/app/events/page.tsx",
                            lineNumber: 379,
                            columnNumber: 11
                        }, this)
                    }, void 0, false, {
                        fileName: "[project]/src/app/events/page.tsx",
                        lineNumber: 378,
                        columnNumber: 9
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/app/events/page.tsx",
                lineNumber: 364,
                columnNumber: 7
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardContent"], {
                className: "p-6",
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
                        className: "text-lg font-semibold text-neutral-900 mb-3 line-clamp-2",
                        children: event.title
                    }, void 0, false, {
                        fileName: "[project]/src/app/events/page.tsx",
                        lineNumber: 391,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "flex items-center gap-2 text-sm text-neutral-600 mb-2",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$calendar$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Calendar$3e$__["Calendar"], {
                                className: "h-4 w-4",
                                style: {
                                    color: '#FF7900'
                                }
                            }, void 0, false, {
                                fileName: "[project]/src/app/events/page.tsx",
                                lineNumber: 397,
                                columnNumber: 11
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                children: [
                                    formattedDate,
                                    " at ",
                                    formattedTime
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/app/events/page.tsx",
                                lineNumber: 398,
                                columnNumber: 11
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/app/events/page.tsx",
                        lineNumber: 396,
                        columnNumber: 9
                    }, this),
                    event.city && event.state && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "flex items-center gap-2 text-sm text-neutral-600 mb-2",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$map$2d$pin$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__MapPin$3e$__["MapPin"], {
                                className: "h-4 w-4",
                                style: {
                                    color: '#FF7900'
                                }
                            }, void 0, false, {
                                fileName: "[project]/src/app/events/page.tsx",
                                lineNumber: 404,
                                columnNumber: 13
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                children: [
                                    event.city,
                                    ", ",
                                    event.state
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/app/events/page.tsx",
                                lineNumber: 405,
                                columnNumber: 13
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/app/events/page.tsx",
                        lineNumber: 403,
                        columnNumber: 11
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "flex items-center gap-2 text-sm text-neutral-600 mb-3",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$users$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Users$3e$__["Users"], {
                                className: "h-4 w-4",
                                style: {
                                    color: '#FF7900'
                                }
                            }, void 0, false, {
                                fileName: "[project]/src/app/events/page.tsx",
                                lineNumber: 411,
                                columnNumber: 11
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                children: [
                                    event.currentRegistrations,
                                    " / ",
                                    event.capacity,
                                    " registered"
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/app/events/page.tsx",
                                lineNumber: 412,
                                columnNumber: 11
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/app/events/page.tsx",
                        lineNumber: 410,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "flex items-center justify-between pt-3 border-t border-neutral-200",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex items-center gap-2",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$dollar$2d$sign$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__DollarSign$3e$__["DollarSign"], {
                                        className: "h-4 w-4",
                                        style: {
                                            color: '#FF7900'
                                        }
                                    }, void 0, false, {
                                        fileName: "[project]/src/app/events/page.tsx",
                                        lineNumber: 420,
                                        columnNumber: 13
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                        className: "text-sm font-semibold",
                                        style: {
                                            color: '#8B1538'
                                        },
                                        children: event.isFree ? 'Free Event' : `$${event.ticketPriceAmount?.toFixed(2)}`
                                    }, void 0, false, {
                                        fileName: "[project]/src/app/events/page.tsx",
                                        lineNumber: 421,
                                        columnNumber: 13
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/app/events/page.tsx",
                                lineNumber: 419,
                                columnNumber: 11
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("button", {
                                className: "px-4 py-2 rounded-lg text-sm font-medium text-white transition-colors",
                                style: {
                                    background: '#FF7900'
                                },
                                children: "View Details"
                            }, void 0, false, {
                                fileName: "[project]/src/app/events/page.tsx",
                                lineNumber: 425,
                                columnNumber: 11
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/app/events/page.tsx",
                        lineNumber: 418,
                        columnNumber: 9
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/app/events/page.tsx",
                lineNumber: 389,
                columnNumber: 7
            }, this)
        ]
    }, void 0, true, {
        fileName: "[project]/src/app/events/page.tsx",
        lineNumber: 359,
        columnNumber: 5
    }, this);
}
}),
];

//# sourceMappingURL=src_44f007c3._.js.map