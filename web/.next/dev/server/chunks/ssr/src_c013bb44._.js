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
   * Session 23: Returns Stripe checkout URL for paid events, null for free events
   * NOTE: Backend RsvpRequest only needs userId and quantity (eventId is in URL path)
   */ async rsvpToEvent(eventId, userId, quantity = 1) {
        const request = {
            userId,
            quantity
        };
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/rsvp`, request);
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
   * Register anonymous attendee for an event
   * No authentication required - for users without accounts
   * Maps to backend RegisterAnonymousAttendeeCommand
   */ async registerAnonymous(eventId, request) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/register-anonymous`, request);
    }
    /**
   * Get current user's RSVPs
   * Epic 1: Backend now returns full EventDto[] instead of RsvpDto[] for better UX
   * Returns all events user has registered for
   */ async getUserRsvps() {
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/my-rsvps`);
    }
    /**
   * Get user's registration details for a specific event
   * Fix 1: Enhanced registration status detection
   * Returns full registration with attendee names and ages
   * Maps to backend GetUserRegistrationForEventQuery
   */ async getUserRegistrationForEvent(eventId) {
        try {
            return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].get(`${this.basePath}/${eventId}/my-registration`);
        } catch (error) {
            // Return null if no registration found (404)
            if (error?.response?.status === 404) {
                return null;
            }
            throw error;
        }
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
   * Create sign-up list WITH items in a single API call
   * Organizer-only operation
   * Maps to backend POST /api/events/{id}/signups
   * Returns the created sign-up list ID
   */ async createSignUpList(eventId, request) {
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].post(`${this.basePath}/${eventId}/signups`, request);
    }
    /**
   * Update sign-up list details (category, description, and category flags)
   * Phase 6A.13: Edit Sign-Up List feature
   */ async updateSignUpList(eventId, signupId, request) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].put(`${this.basePath}/${eventId}/signups/${signupId}`, request);
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
        // Use apiClient.postMultipart for proper authentication and error handling
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].postMultipart(`${this.basePath}/${eventId}/images`, formData);
    }
    /**
   * Delete an image from an event
   * Maps to backend DELETE /api/events/{eventId}/images/{imageId}
   *
   * @param eventId - Event ID (GUID)
   * @param imageId - Image ID (GUID)
   */ async deleteEventImage(eventId, imageId) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].delete(`${this.basePath}/${eventId}/images/${imageId}`);
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
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].put(`${this.basePath}/${eventId}/images/reorder`, {
            newOrders
        });
    }
    /**
   * Upload a video to an event
   * Maps to backend POST /api/events/{id}/videos
   *
   * @param eventId - Event ID (GUID)
   * @param videoFile - Video file to upload
   * @param thumbnailFile - Thumbnail image file
   * @returns EventVideoDto with video metadata
   */ async uploadEventVideo(eventId, videoFile, thumbnailFile) {
        const formData = new FormData();
        formData.append('video', videoFile);
        formData.append('thumbnail', thumbnailFile);
        // Use apiClient.postMultipart for proper authentication and error handling
        // Video files can be large (up to 100MB), so use 5-minute timeout
        return await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].postMultipart(`${this.basePath}/${eventId}/videos`, formData, {
            timeout: 300000
        });
    }
    /**
   * Delete a video from an event
   * Maps to backend DELETE /api/events/{eventId}/videos/{videoId}
   *
   * @param eventId - Event ID (GUID)
   * @param videoId - Video ID (GUID)
   */ async deleteEventVideo(eventId, videoId) {
        await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$client$2f$api$2d$client$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["apiClient"].delete(`${this.basePath}/${eventId}/videos/${videoId}`);
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
    ()=>useUpdateEvent,
    "useUserRegistrationDetails",
    ()=>useUserRegistrationDetails,
    "useUserRsvpForEvent",
    ()=>useUserRsvpForEvent,
    "useUserRsvps",
    ()=>useUserRsvps
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
function useUserRsvps(options) {
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQuery"])({
        queryKey: [
            'user-rsvps'
        ],
        queryFn: ()=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].getUserRsvps(),
        staleTime: 5 * 60 * 1000,
        refetchOnWindowFocus: true,
        retry: 1,
        ...options
    });
}
function useUserRsvpForEvent(eventId, options) {
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQuery"])({
        queryKey: [
            'user-rsvps'
        ],
        queryFn: ()=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].getUserRsvps(),
        select: (events)=>events.find((event)=>event.id === eventId),
        enabled: !!eventId,
        staleTime: 5 * 60 * 1000,
        refetchOnWindowFocus: true,
        retry: 1,
        ...options
    });
}
function useUserRegistrationDetails(eventId, isUserRegistered = false, options) {
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQuery"])({
        queryKey: [
            'user-registration',
            eventId
        ],
        queryFn: async ()=>{
            console.log('[useUserRegistrationDetails] Fetching registration details for event:', eventId);
            try {
                const result = await __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].getUserRegistrationForEvent(eventId);
                console.log('[useUserRegistrationDetails] Success:', result);
                console.log('[useUserRegistrationDetails] Attendees:', result?.attendees);
                console.log('[useUserRegistrationDetails] Attendees count:', result?.attendees?.length);
                console.log('[useUserRegistrationDetails] Full value:', JSON.stringify(result, null, 2));
                return result;
            } catch (error) {
                console.error('[useUserRegistrationDetails] Error:', error);
                throw error;
            }
        },
        enabled: !!eventId && isUserRegistered,
        staleTime: 5 * 60 * 1000,
        refetchOnWindowFocus: true,
        retry: false,
        ...options
    });
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
    useInvalidateEvents,
    useUserRsvps,
    useUserRsvpForEvent,
    useUserRegistrationDetails
};
}),
"[project]/src/presentation/hooks/useEventSignUps.ts [app-ssr] (ecmascript)", ((__turbopack_context__) => {
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
    "useCancelCommitment",
    ()=>useCancelCommitment,
    "useCommitToSignUp",
    ()=>useCommitToSignUp,
    "useCommitToSignUpItem",
    ()=>useCommitToSignUpItem,
    "useCreateSignUpList",
    ()=>useCreateSignUpList,
    "useEventSignUps",
    ()=>useEventSignUps,
    "useRemoveSignUpItem",
    ()=>useRemoveSignUpItem,
    "useRemoveSignUpList",
    ()=>useRemoveSignUpList,
    "useUpdateSignUpList",
    ()=>useUpdateSignUpList
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/useQuery.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/useMutation.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/@tanstack/react-query/build/modern/QueryClientProvider.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/repositories/events.repository.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEvents$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/hooks/useEvents.ts [app-ssr] (ecmascript)");
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
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useQuery$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQuery"])({
        queryKey: signUpKeys.list(eventId || ''),
        queryFn: ()=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].getEventSignUpLists(eventId),
        enabled: !!eventId,
        staleTime: 5 * 60 * 1000,
        refetchOnWindowFocus: true,
        retry: 1,
        ...options
    });
}
function useAddSignUpList() {
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: ({ eventId, ...data })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].addSignUpList(eventId, data),
        onSuccess: (_data, variables)=>{
            // Invalidate sign-up lists for this event
            queryClient.invalidateQueries({
                queryKey: signUpKeys.list(variables.eventId)
            });
            // Invalidate event detail to reflect updated event
            queryClient.invalidateQueries({
                queryKey: __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEvents$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventKeys"].detail(variables.eventId)
            });
        }
    });
}
function useRemoveSignUpList() {
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: ({ eventId, signupId })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].removeSignUpList(eventId, signupId),
        onMutate: async ({ eventId, signupId })=>{
            // Cancel queries
            await queryClient.cancelQueries({
                queryKey: signUpKeys.list(eventId)
            });
            // Snapshot for rollback
            const previousSignUps = queryClient.getQueryData(signUpKeys.list(eventId));
            // Optimistically remove from cache
            queryClient.setQueryData(signUpKeys.list(eventId), (old)=>old?.filter((s)=>s.id !== signupId) || []);
            return {
                previousSignUps
            };
        },
        onError: (err, { eventId }, context)=>{
            // Rollback on error
            if (context?.previousSignUps) {
                queryClient.setQueryData(signUpKeys.list(eventId), context.previousSignUps);
            }
        },
        onSuccess: (_data, variables)=>{
            // Invalidate to ensure consistency
            queryClient.invalidateQueries({
                queryKey: signUpKeys.list(variables.eventId)
            });
            queryClient.invalidateQueries({
                queryKey: __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEvents$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventKeys"].detail(variables.eventId)
            });
        }
    });
}
function useCommitToSignUp() {
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: ({ eventId, signupId, ...data })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].commitToSignUp(eventId, signupId, data),
        onMutate: async ({ eventId, signupId, userId, itemDescription, quantity })=>{
            // Cancel queries
            await queryClient.cancelQueries({
                queryKey: signUpKeys.list(eventId)
            });
            // Snapshot for rollback
            const previousSignUps = queryClient.getQueryData(signUpKeys.list(eventId));
            // Optimistically add commitment
            queryClient.setQueryData(signUpKeys.list(eventId), (old)=>{
                if (!old) return old;
                return old.map((signUp)=>{
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
                });
            });
            return {
                previousSignUps
            };
        },
        onError: (err, { eventId }, context)=>{
            // Rollback on error
            if (context?.previousSignUps) {
                queryClient.setQueryData(signUpKeys.list(eventId), context.previousSignUps);
            }
        },
        onSuccess: (_data, variables)=>{
            // Refetch to get accurate data from server (with real IDs)
            queryClient.invalidateQueries({
                queryKey: signUpKeys.list(variables.eventId)
            });
        }
    });
}
function useCancelCommitment() {
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: ({ eventId, signupId, ...data })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].cancelCommitment(eventId, signupId, data),
        onMutate: async ({ eventId, signupId, userId })=>{
            // Cancel queries
            await queryClient.cancelQueries({
                queryKey: signUpKeys.list(eventId)
            });
            // Snapshot for rollback
            const previousSignUps = queryClient.getQueryData(signUpKeys.list(eventId));
            // Optimistically remove commitment
            queryClient.setQueryData(signUpKeys.list(eventId), (old)=>{
                if (!old) return old;
                return old.map((signUp)=>{
                    if (signUp.id !== signupId) return signUp;
                    return {
                        ...signUp,
                        commitments: signUp.commitments.filter((c)=>c.userId !== userId),
                        commitmentCount: Math.max(0, signUp.commitmentCount - 1)
                    };
                });
            });
            return {
                previousSignUps
            };
        },
        onError: (err, { eventId }, context)=>{
            // Rollback on error
            if (context?.previousSignUps) {
                queryClient.setQueryData(signUpKeys.list(eventId), context.previousSignUps);
            }
        },
        onSuccess: (_data, variables)=>{
            // Refetch to ensure consistency
            queryClient.invalidateQueries({
                queryKey: signUpKeys.list(variables.eventId)
            });
        }
    });
}
function useCreateSignUpList() {
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: ({ eventId, ...data })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].createSignUpList(eventId, data),
        onSuccess: (_data, variables)=>{
            queryClient.invalidateQueries({
                queryKey: signUpKeys.list(variables.eventId)
            });
            queryClient.invalidateQueries({
                queryKey: __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEvents$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventKeys"].detail(variables.eventId)
            });
        }
    });
}
function useUpdateSignUpList(eventId) {
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: ({ signupId, ...data })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].updateSignUpList(eventId, signupId, data),
        onSuccess: ()=>{
            queryClient.invalidateQueries({
                queryKey: signUpKeys.list(eventId)
            });
            queryClient.invalidateQueries({
                queryKey: __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEvents$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventKeys"].detail(eventId)
            });
        }
    });
}
function useAddSignUpItem() {
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: ({ eventId, signupId, ...data })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].addSignUpItem(eventId, signupId, data),
        onSuccess: (_data, variables)=>{
            queryClient.invalidateQueries({
                queryKey: signUpKeys.list(variables.eventId)
            });
        }
    });
}
function useRemoveSignUpItem() {
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: ({ eventId, signupId, itemId })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].removeSignUpItem(eventId, signupId, itemId),
        onMutate: async ({ eventId, signupId, itemId })=>{
            await queryClient.cancelQueries({
                queryKey: signUpKeys.list(eventId)
            });
            const previousSignUps = queryClient.getQueryData(signUpKeys.list(eventId));
            // Optimistically remove item
            queryClient.setQueryData(signUpKeys.list(eventId), (old)=>{
                if (!old) return old;
                return old.map((signUp)=>{
                    if (signUp.id !== signupId) return signUp;
                    return {
                        ...signUp,
                        items: signUp.items.filter((item)=>item.id !== itemId)
                    };
                });
            });
            return {
                previousSignUps
            };
        },
        onError: (err, { eventId }, context)=>{
            if (context?.previousSignUps) {
                queryClient.setQueryData(signUpKeys.list(eventId), context.previousSignUps);
            }
        },
        onSuccess: (_data, variables)=>{
            queryClient.invalidateQueries({
                queryKey: signUpKeys.list(variables.eventId)
            });
        }
    });
}
function useCommitToSignUpItem() {
    const queryClient = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$QueryClientProvider$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useQueryClient"])();
    return (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f40$tanstack$2f$react$2d$query$2f$build$2f$modern$2f$useMutation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useMutation"])({
        mutationFn: ({ eventId, signupId, itemId, ...data })=>__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$repositories$2f$events$2e$repository$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["eventsRepository"].commitToSignUpItem(eventId, signupId, itemId, data),
        onSuccess: (_data, variables)=>{
            queryClient.invalidateQueries({
                queryKey: signUpKeys.list(variables.eventId)
            });
        }
    });
}
const __TURBOPACK__default__export__ = {
    useEventSignUps,
    useAddSignUpList,
    useRemoveSignUpList,
    useCommitToSignUp,
    useCancelCommitment,
    useCreateSignUpList,
    useAddSignUpItem,
    useRemoveSignUpItem,
    useCommitToSignUpItem
};
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
"[project]/src/presentation/components/ui/Input.tsx [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "Input",
    ()=>Input
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react-jsx-dev-runtime.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/lib/utils.ts [app-ssr] (ecmascript)");
;
;
;
/**
 * Input Component
 * Reusable input component with error states and accessibility
 * Follows UI/UX best practices
 */ const Input = /*#__PURE__*/ __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["forwardRef"](({ className, type = 'text', error, ...props }, ref)=>{
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
        type: type,
        className: (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$lib$2f$utils$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["cn"])('flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50', error && 'border-destructive focus-visible:ring-destructive', className),
        ref: ref,
        "aria-invalid": error ? 'true' : undefined,
        ...props
    }, void 0, false, {
        fileName: "[project]/src/presentation/components/ui/Input.tsx",
        lineNumber: 16,
        columnNumber: 7
    }, ("TURBOPACK compile-time value", void 0));
});
Input.displayName = 'Input';
;
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
    "PaymentStatus",
    ()=>PaymentStatus,
    "PricingType",
    ()=>PricingType,
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
var PricingType = /*#__PURE__*/ function(PricingType) {
    PricingType[PricingType["Single"] = 0] = "Single";
    PricingType[PricingType["AgeDual"] = 1] = "AgeDual";
    PricingType[PricingType["GroupTiered"] = 2] = "GroupTiered";
    return PricingType;
}({});
var PaymentStatus = /*#__PURE__*/ function(PaymentStatus) {
    PaymentStatus[PaymentStatus["Pending"] = 0] = "Pending";
    PaymentStatus[PaymentStatus["Completed"] = 1] = "Completed";
    PaymentStatus[PaymentStatus["Failed"] = 2] = "Failed";
    PaymentStatus[PaymentStatus["Refunded"] = 3] = "Refunded";
    PaymentStatus[PaymentStatus["NotRequired"] = 4] = "NotRequired";
    return PaymentStatus;
}({});
var SignUpType = /*#__PURE__*/ function(SignUpType) {
    SignUpType[SignUpType["Open"] = 0] = "Open";
    SignUpType[SignUpType["Predefined"] = 1] = "Predefined";
    return SignUpType;
}({});
var SignUpItemCategory = /*#__PURE__*/ function(SignUpItemCategory) {
    SignUpItemCategory["Mandatory"] = "Mandatory";
    SignUpItemCategory["Preferred"] = "Preferred";
    SignUpItemCategory["Suggested"] = "Suggested";
    return SignUpItemCategory;
}({});
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
"[project]/src/app/events/[id]/manage-signups/page.tsx [app-ssr] (ecmascript)", ((__turbopack_context__) => {
"use strict";

__turbopack_context__.s([
    "default",
    ()=>ManageSignUpsPage
]);
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react-jsx-dev-runtime.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/dist/server/route-modules/app-page/vendored/ssr/react.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/node_modules/next/navigation.js [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Header$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/layout/Header.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Footer$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/layout/Footer.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/store/useAuthStore.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEvents$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/hooks/useEvents.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEventSignUps$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/hooks/useEventSignUps.ts [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Card.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Button.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Input$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/presentation/components/ui/Input.tsx [app-ssr] (ecmascript)");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$plus$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Plus$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/plus.js [app-ssr] (ecmascript) <export default as Plus>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$trash$2d$2$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Trash2$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/trash-2.js [app-ssr] (ecmascript) <export default as Trash2>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$download$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Download$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/download.js [app-ssr] (ecmascript) <export default as Download>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$arrow$2d$left$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__ArrowLeft$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/arrow-left.js [app-ssr] (ecmascript) <export default as ArrowLeft>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$list$2d$plus$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__ListPlus$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/list-plus.js [app-ssr] (ecmascript) <export default as ListPlus>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$users$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Users$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/users.js [app-ssr] (ecmascript) <export default as Users>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$square$2d$pen$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Edit$3e$__ = __turbopack_context__.i("[project]/node_modules/lucide-react/dist/esm/icons/square-pen.js [app-ssr] (ecmascript) <export default as Edit>");
var __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__ = __turbopack_context__.i("[project]/src/infrastructure/api/types/events.types.ts [app-ssr] (ecmascript)");
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
function ManageSignUpsPage() {
    const params = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useParams"])();
    const router = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$navigation$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useRouter"])();
    const eventId = params.id;
    const { user, isAuthenticated } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$store$2f$useAuthStore$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useAuthStore"])();
    // Fetch event details
    const { data: event, isLoading: eventLoading } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEvents$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useEventById"])(eventId);
    // Fetch sign-up lists
    const { data: signUpLists, isLoading: signUpsLoading } = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEventSignUps$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useEventSignUps"])(eventId);
    // DEBUG: Log sign-up lists data
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useEffect"])(()=>{
        console.log('[ManageSignUps] Sign-up lists data:', signUpLists);
        console.log('[ManageSignUps] Loading state:', signUpsLoading);
    }, [
        signUpLists,
        signUpsLoading
    ]);
    // Mutations
    const removeSignUpListMutation = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEventSignUps$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useRemoveSignUpList"])();
    const createSignUpListMutation = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEventSignUps$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useCreateSignUpList"])();
    const removeSignUpItemMutation = (0, __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$hooks$2f$useEventSignUps$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useRemoveSignUpItem"])();
    // Form state
    const [showForm, setShowForm] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])(false);
    const [category, setCategory] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])('');
    const [description, setDescription] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])('');
    const [deleteConfirmId, setDeleteConfirmId] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])(null);
    const [submitError, setSubmitError] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])(null);
    // Category checkboxes - organizer selects which categories to enable
    const [hasMandatoryItems, setHasMandatoryItems] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])(false);
    const [hasPreferredItems, setHasPreferredItems] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])(false);
    const [hasSuggestedItems, setHasSuggestedItems] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])(false);
    const [mandatoryItems, setMandatoryItems] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])([]);
    const [preferredItems, setPreferredItems] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])([]);
    const [suggestedItems, setSuggestedItems] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])([]);
    // Separate form state for each category
    const [newMandatoryDesc, setNewMandatoryDesc] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])('');
    const [newMandatoryQty, setNewMandatoryQty] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])(1);
    const [newMandatoryNotes, setNewMandatoryNotes] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])('');
    const [newPreferredDesc, setNewPreferredDesc] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])('');
    const [newPreferredQty, setNewPreferredQty] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])(1);
    const [newPreferredNotes, setNewPreferredNotes] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])('');
    const [newSuggestedDesc, setNewSuggestedDesc] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])('');
    const [newSuggestedQty, setNewSuggestedQty] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])(1);
    const [newSuggestedNotes, setNewSuggestedNotes] = (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useState"])('');
    // Redirect if not authenticated or not authorized
    (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["useEffect"])(()=>{
        if (!isAuthenticated || !user?.userId) {
            router.push('/login?redirect=' + encodeURIComponent(`/events/${eventId}/manage-signups`));
            return;
        }
        // Check if user is organizer or admin
        const isAuthorized = event && (event.organizerId === user.userId || user.role === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["UserRole"].Admin || user.role === __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$auth$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["UserRole"].AdminManager);
        if (event && !isAuthorized) {
            // Redirect unauthorized users to event detail page
            router.push(`/events/${eventId}`);
        }
    }, [
        isAuthenticated,
        user,
        event,
        eventId,
        router
    ]);
    // Handle add mandatory item
    const handleAddMandatoryItem = ()=>{
        if (!newMandatoryDesc.trim()) {
            setSubmitError('Item description is required');
            return;
        }
        if (newMandatoryQty < 1) {
            setSubmitError('Quantity must be at least 1');
            return;
        }
        setMandatoryItems([
            ...mandatoryItems,
            {
                description: newMandatoryDesc.trim(),
                quantity: newMandatoryQty,
                notes: newMandatoryNotes.trim()
            }
        ]);
        setNewMandatoryDesc('');
        setNewMandatoryQty(1);
        setNewMandatoryNotes('');
        setSubmitError(null);
    };
    // Handle add preferred item
    const handleAddPreferredItem = ()=>{
        if (!newPreferredDesc.trim()) {
            setSubmitError('Item description is required');
            return;
        }
        if (newPreferredQty < 1) {
            setSubmitError('Quantity must be at least 1');
            return;
        }
        setPreferredItems([
            ...preferredItems,
            {
                description: newPreferredDesc.trim(),
                quantity: newPreferredQty,
                notes: newPreferredNotes.trim()
            }
        ]);
        setNewPreferredDesc('');
        setNewPreferredQty(1);
        setNewPreferredNotes('');
        setSubmitError(null);
    };
    // Handle add suggested item
    const handleAddSuggestedItem = ()=>{
        if (!newSuggestedDesc.trim()) {
            setSubmitError('Item description is required');
            return;
        }
        if (newSuggestedQty < 1) {
            setSubmitError('Quantity must be at least 1');
            return;
        }
        setSuggestedItems([
            ...suggestedItems,
            {
                description: newSuggestedDesc.trim(),
                quantity: newSuggestedQty,
                notes: newSuggestedNotes.trim()
            }
        ]);
        setNewSuggestedDesc('');
        setNewSuggestedQty(1);
        setNewSuggestedNotes('');
        setSubmitError(null);
    };
    // Handle remove item from category
    const handleRemoveMandatoryItem = (index)=>{
        setMandatoryItems(mandatoryItems.filter((_, i)=>i !== index));
    };
    const handleRemovePreferredItem = (index)=>{
        setPreferredItems(preferredItems.filter((_, i)=>i !== index));
    };
    const handleRemoveSuggestedItem = (index)=>{
        setSuggestedItems(suggestedItems.filter((_, i)=>i !== index));
    };
    // Handle create sign-up list WITH items in single API call
    const handleCreateSignUpList = async ()=>{
        console.log('[ManageSignUps] handleCreateSignUpList called');
        if (!category.trim()) {
            setSubmitError('Category is required');
            return;
        }
        if (!description.trim()) {
            setSubmitError('Description is required');
            return;
        }
        if (!hasMandatoryItems && !hasPreferredItems && !hasSuggestedItems) {
            setSubmitError('Please select at least one category (Mandatory, Preferred, or Suggested)');
            return;
        }
        // Validate at least one item exists
        const allItems = [
            ...mandatoryItems,
            ...preferredItems,
            ...suggestedItems
        ];
        if (allItems.length === 0) {
            setSubmitError('Please add at least one item to the sign-up list');
            return;
        }
        try {
            setSubmitError(null);
            // Convert items to API format
            const items = [
                ...mandatoryItems.map((item)=>({
                        itemDescription: item.description,
                        quantity: item.quantity,
                        itemCategory: __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["SignUpItemCategory"].Mandatory,
                        notes: item.notes || null
                    })),
                ...preferredItems.map((item)=>({
                        itemDescription: item.description,
                        quantity: item.quantity,
                        itemCategory: __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["SignUpItemCategory"].Preferred,
                        notes: item.notes || null
                    })),
                ...suggestedItems.map((item)=>({
                        itemDescription: item.description,
                        quantity: item.quantity,
                        itemCategory: __TURBOPACK__imported__module__$5b$project$5d2f$src$2f$infrastructure$2f$api$2f$types$2f$events$2e$types$2e$ts__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["SignUpItemCategory"].Suggested,
                        notes: item.notes || null
                    }))
            ];
            const payload = {
                eventId,
                category: category.trim(),
                description: description.trim(),
                hasMandatoryItems,
                hasPreferredItems,
                hasSuggestedItems,
                items
            };
            console.log('[ManageSignUps] Creating sign-up list with payload:', payload);
            // Create sign-up list WITH all items in single transactional API call
            const result = await createSignUpListMutation.mutateAsync(payload);
            console.log('[ManageSignUps] Sign-up list created successfully:', result);
            // Reset form
            setCategory('');
            setDescription('');
            setHasMandatoryItems(false);
            setHasPreferredItems(false);
            setHasSuggestedItems(false);
            setMandatoryItems([]);
            setPreferredItems([]);
            setSuggestedItems([]);
            setShowForm(false);
            console.log('[ManageSignUps] Form reset complete');
        } catch (err) {
            console.error('[ManageSignUps] Failed to create sign-up list:', err);
            setSubmitError(err instanceof Error ? err.message : 'Failed to create sign-up list');
        }
    };
    // Handle delete sign-up list
    const handleDeleteSignUpList = async (signupId)=>{
        try {
            await removeSignUpListMutation.mutateAsync({
                eventId,
                signupId
            });
            setDeleteConfirmId(null);
        } catch (err) {
            console.error('Failed to delete sign-up list:', err);
            alert('Failed to delete sign-up list. Please try again.');
        }
    };
    // Handle download commitments as CSV
    const handleDownloadCSV = ()=>{
        if (!signUpLists || signUpLists.length === 0) {
            alert('No sign-up lists to download');
            return;
        }
        // Build CSV content
        let csvContent = 'Category,Item Description,User ID,Quantity,Committed At\n';
        signUpLists.forEach((list)=>{
            (list.commitments || []).forEach((commitment)=>{
                csvContent += `"${list.category}","${commitment.itemDescription}","${commitment.userId}",${commitment.quantity},"${commitment.committedAt}"\n`;
            });
        });
        // Create download link
        const blob = new Blob([
            csvContent
        ], {
            type: 'text/csv;charset=utf-8;'
        });
        const link = document.createElement('a');
        const url = URL.createObjectURL(blob);
        link.setAttribute('href', url);
        link.setAttribute('download', `event-${eventId}-signups.csv`);
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    };
    // Don't render until authentication is confirmed
    if (!isAuthenticated || !user?.userId) {
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "min-h-screen bg-gradient-to-b from-neutral-50 to-white",
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Header$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Header"], {}, void 0, false, {
                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                    lineNumber: 318,
                    columnNumber: 9
                }, this),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12",
                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "text-center",
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                            className: "text-neutral-500",
                            children: "Redirecting to login..."
                        }, void 0, false, {
                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                            lineNumber: 321,
                            columnNumber: 13
                        }, this)
                    }, void 0, false, {
                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                        lineNumber: 320,
                        columnNumber: 11
                    }, this)
                }, void 0, false, {
                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                    lineNumber: 319,
                    columnNumber: 9
                }, this),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Footer$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {}, void 0, false, {
                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                    lineNumber: 324,
                    columnNumber: 9
                }, this)
            ]
        }, void 0, true, {
            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
            lineNumber: 317,
            columnNumber: 7
        }, this);
    }
    // Don't render until event is loaded
    if (eventLoading || !event) {
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "min-h-screen bg-gradient-to-b from-neutral-50 to-white",
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Header$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Header"], {}, void 0, false, {
                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                    lineNumber: 333,
                    columnNumber: 9
                }, this),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12",
                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "text-center",
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                            className: "text-neutral-500",
                            children: "Loading event..."
                        }, void 0, false, {
                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                            lineNumber: 336,
                            columnNumber: 13
                        }, this)
                    }, void 0, false, {
                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                        lineNumber: 335,
                        columnNumber: 11
                    }, this)
                }, void 0, false, {
                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                    lineNumber: 334,
                    columnNumber: 9
                }, this),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Footer$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {}, void 0, false, {
                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                    lineNumber: 339,
                    columnNumber: 9
                }, this)
            ]
        }, void 0, true, {
            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
            lineNumber: 332,
            columnNumber: 7
        }, this);
    }
    // Verify user is organizer
    if (event.organizerId !== user.userId) {
        return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
            className: "min-h-screen bg-gradient-to-b from-neutral-50 to-white",
            children: [
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Header$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Header"], {}, void 0, false, {
                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                    lineNumber: 348,
                    columnNumber: 9
                }, this),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                    className: "max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12",
                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "text-center",
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                            className: "text-destructive",
                            children: "You are not authorized to manage this event"
                        }, void 0, false, {
                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                            lineNumber: 351,
                            columnNumber: 13
                        }, this)
                    }, void 0, false, {
                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                        lineNumber: 350,
                        columnNumber: 11
                    }, this)
                }, void 0, false, {
                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                    lineNumber: 349,
                    columnNumber: 9
                }, this),
                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Footer$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {}, void 0, false, {
                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                    lineNumber: 354,
                    columnNumber: 9
                }, this)
            ]
        }, void 0, true, {
            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
            lineNumber: 347,
            columnNumber: 7
        }, this);
    }
    const totalCommitments = signUpLists?.reduce((sum, list)=>sum + list.commitmentCount, 0) || 0;
    return /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
        className: "min-h-screen bg-gradient-to-b from-neutral-50 to-white",
        children: [
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Header$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Header"], {}, void 0, false, {
                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                lineNumber: 363,
                columnNumber: 7
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 py-8 relative overflow-hidden",
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "absolute inset-0 opacity-10",
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                            className: "absolute inset-0",
                            style: {
                                backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`
                            }
                        }, void 0, false, {
                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                            lineNumber: 368,
                            columnNumber: 11
                        }, this)
                    }, void 0, false, {
                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                        lineNumber: 367,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
                                variant: "outline",
                                onClick: ()=>router.push(`/events/${eventId}/manage`),
                                className: "mb-4 bg-white/10 text-white border-white/30 hover:bg-white/20 hover:border-white/50",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$arrow$2d$left$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__ArrowLeft$3e$__["ArrowLeft"], {
                                        className: "h-4 w-4 mr-2"
                                    }, void 0, false, {
                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                        lineNumber: 382,
                                        columnNumber: 13
                                    }, this),
                                    "Back to Manage Event"
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                lineNumber: 377,
                                columnNumber: 11
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("h1", {
                                className: "text-3xl font-bold text-white mb-2",
                                children: "Manage Sign-Up Lists"
                            }, void 0, false, {
                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                lineNumber: 386,
                                columnNumber: 11
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                className: "text-lg text-white/90",
                                children: event.title
                            }, void 0, false, {
                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                lineNumber: 389,
                                columnNumber: 11
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                        lineNumber: 376,
                        columnNumber: 9
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                lineNumber: 366,
                columnNumber: 7
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                className: "max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8",
                children: [
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "grid grid-cols-1 md:grid-cols-2 gap-6 mb-8",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Card"], {
                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardContent"], {
                                    className: "p-6",
                                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "flex items-center justify-between",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                        className: "text-sm font-medium text-neutral-500",
                                                        children: "Total Sign-Up Lists"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                        lineNumber: 403,
                                                        columnNumber: 19
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                        className: "text-3xl font-bold text-neutral-900 mt-1",
                                                        children: signUpLists?.length || 0
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                        lineNumber: 404,
                                                        columnNumber: 19
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                lineNumber: 402,
                                                columnNumber: 17
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "p-3 rounded-lg",
                                                style: {
                                                    background: '#FFF4ED'
                                                },
                                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$list$2d$plus$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__ListPlus$3e$__["ListPlus"], {
                                                    className: "h-6 w-6",
                                                    style: {
                                                        color: '#FF7900'
                                                    }
                                                }, void 0, false, {
                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                    lineNumber: 409,
                                                    columnNumber: 19
                                                }, this)
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                lineNumber: 408,
                                                columnNumber: 17
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                        lineNumber: 401,
                                        columnNumber: 15
                                    }, this)
                                }, void 0, false, {
                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                    lineNumber: 400,
                                    columnNumber: 13
                                }, this)
                            }, void 0, false, {
                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                lineNumber: 399,
                                columnNumber: 11
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Card"], {
                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardContent"], {
                                    className: "p-6",
                                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "flex items-center justify-between",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                        className: "text-sm font-medium text-neutral-500",
                                                        children: "Total Commitments"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                        lineNumber: 419,
                                                        columnNumber: 19
                                                    }, this),
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                        className: "text-3xl font-bold text-neutral-900 mt-1",
                                                        children: totalCommitments
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                        lineNumber: 420,
                                                        columnNumber: 19
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                lineNumber: 418,
                                                columnNumber: 17
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "p-3 rounded-lg",
                                                style: {
                                                    background: '#FFF4ED'
                                                },
                                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$users$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Users$3e$__["Users"], {
                                                    className: "h-6 w-6",
                                                    style: {
                                                        color: '#FF7900'
                                                    }
                                                }, void 0, false, {
                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                    lineNumber: 423,
                                                    columnNumber: 19
                                                }, this)
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                lineNumber: 422,
                                                columnNumber: 17
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                        lineNumber: 417,
                                        columnNumber: 15
                                    }, this)
                                }, void 0, false, {
                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                    lineNumber: 416,
                                    columnNumber: 13
                                }, this)
                            }, void 0, false, {
                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                lineNumber: 415,
                                columnNumber: 11
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                        lineNumber: 398,
                        columnNumber: 9
                    }, this),
                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "flex items-center justify-between mb-6",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("h2", {
                                className: "text-2xl font-bold",
                                style: {
                                    color: '#8B1538'
                                },
                                children: "Sign-Up Lists"
                            }, void 0, false, {
                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                lineNumber: 432,
                                columnNumber: 11
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                className: "flex gap-3",
                                children: [
                                    signUpLists && signUpLists.length > 0 && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
                                        variant: "outline",
                                        onClick: handleDownloadCSV,
                                        className: "flex items-center gap-2",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$download$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Download$3e$__["Download"], {
                                                className: "h-4 w-4"
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                lineNumber: 442,
                                                columnNumber: 17
                                            }, this),
                                            "Download CSV"
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                        lineNumber: 437,
                                        columnNumber: 15
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
                                        onClick: ()=>setShowForm(!showForm),
                                        style: {
                                            background: '#FF7900'
                                        },
                                        className: "flex items-center gap-2",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$plus$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Plus$3e$__["Plus"], {
                                                className: "h-4 w-4"
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                lineNumber: 451,
                                                columnNumber: 15
                                            }, this),
                                            showForm ? 'Cancel' : 'Create Sign-Up List'
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                        lineNumber: 446,
                                        columnNumber: 13
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                lineNumber: 435,
                                columnNumber: 11
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                        lineNumber: 431,
                        columnNumber: 9
                    }, this),
                    showForm && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Card"], {
                        className: "mb-6",
                        children: [
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardHeader"], {
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardTitle"], {
                                        style: {
                                            color: '#8B1538'
                                        },
                                        children: "Create New Sign-Up List"
                                    }, void 0, false, {
                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                        lineNumber: 461,
                                        columnNumber: 15
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardDescription"], {
                                        children: "Add a new sign-up list for attendees to commit to bringing items"
                                    }, void 0, false, {
                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                        lineNumber: 462,
                                        columnNumber: 15
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                lineNumber: 460,
                                columnNumber: 13
                            }, this),
                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardContent"], {
                                className: "space-y-4",
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                htmlFor: "category",
                                                className: "block text-sm font-medium text-neutral-700 mb-2",
                                                children: "Category *"
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                lineNumber: 469,
                                                columnNumber: 17
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Input$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Input"], {
                                                id: "category",
                                                type: "text",
                                                placeholder: "e.g., Food & Drinks, Decorations, Supplies",
                                                value: category,
                                                onChange: (e)=>setCategory(e.target.value)
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                lineNumber: 472,
                                                columnNumber: 17
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                        lineNumber: 468,
                                        columnNumber: 15
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                htmlFor: "description",
                                                className: "block text-sm font-medium text-neutral-700 mb-2",
                                                children: "Description *"
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                lineNumber: 483,
                                                columnNumber: 17
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("textarea", {
                                                id: "description",
                                                rows: 3,
                                                placeholder: "Describe what items are needed or provide instructions...",
                                                className: "w-full px-4 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 resize-none",
                                                value: description,
                                                onChange: (e)=>setDescription(e.target.value)
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                lineNumber: 486,
                                                columnNumber: 17
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                        lineNumber: 482,
                                        columnNumber: 15
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "space-y-4",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                className: "block text-sm font-medium text-neutral-700 mb-3",
                                                children: "Select Item Categories * (at least one required)"
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                lineNumber: 498,
                                                columnNumber: 19
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "space-y-3",
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                        className: "flex items-center gap-3 p-3 border rounded-lg cursor-pointer hover:bg-neutral-50",
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
                                                                type: "checkbox",
                                                                checked: hasMandatoryItems,
                                                                onChange: (e)=>setHasMandatoryItems(e.target.checked),
                                                                className: "w-4 h-4 text-red-600"
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                lineNumber: 505,
                                                                columnNumber: 23
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                children: [
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                        className: "font-medium text-neutral-900",
                                                                        children: "Mandatory Items"
                                                                    }, void 0, false, {
                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                        lineNumber: 512,
                                                                        columnNumber: 25
                                                                    }, this),
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                        className: "text-sm text-neutral-500",
                                                                        children: "Required items that must be brought"
                                                                    }, void 0, false, {
                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                        lineNumber: 513,
                                                                        columnNumber: 25
                                                                    }, this)
                                                                ]
                                                            }, void 0, true, {
                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                lineNumber: 511,
                                                                columnNumber: 23
                                                            }, this)
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                        lineNumber: 504,
                                                        columnNumber: 21
                                                    }, this),
                                                    hasMandatoryItems && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        className: "border-t pt-4",
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("h4", {
                                                                className: "text-md font-semibold text-neutral-800 mb-3 flex items-center gap-2",
                                                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                                    className: "px-2 py-1 rounded text-xs font-medium bg-red-100 text-red-800",
                                                                    children: "Mandatory Items"
                                                                }, void 0, false, {
                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                    lineNumber: 520,
                                                                    columnNumber: 25
                                                                }, this)
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                lineNumber: 519,
                                                                columnNumber: 23
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                className: "grid grid-cols-1 lg:grid-cols-2 gap-4",
                                                                children: [
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                        className: "space-y-3",
                                                                        children: [
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                                children: [
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                                                        className: "block text-xs font-medium text-neutral-600 mb-1",
                                                                                        children: "Item Description *"
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 528,
                                                                                        columnNumber: 29
                                                                                    }, this),
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Input$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Input"], {
                                                                                        type: "text",
                                                                                        placeholder: "e.g., Rice (5 cups)",
                                                                                        value: newMandatoryDesc,
                                                                                        onChange: (e)=>setNewMandatoryDesc(e.target.value)
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 531,
                                                                                        columnNumber: 29
                                                                                    }, this)
                                                                                ]
                                                                            }, void 0, true, {
                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                lineNumber: 527,
                                                                                columnNumber: 27
                                                                            }, this),
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                                children: [
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                                                        className: "block text-xs font-medium text-neutral-600 mb-1",
                                                                                        children: "Quantity *"
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 539,
                                                                                        columnNumber: 29
                                                                                    }, this),
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Input$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Input"], {
                                                                                        type: "number",
                                                                                        min: "1",
                                                                                        value: newMandatoryQty,
                                                                                        onChange: (e)=>setNewMandatoryQty(parseInt(e.target.value) || 1)
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 542,
                                                                                        columnNumber: 29
                                                                                    }, this)
                                                                                ]
                                                                            }, void 0, true, {
                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                lineNumber: 538,
                                                                                columnNumber: 27
                                                                            }, this),
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                                children: [
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                                                        className: "block text-xs font-medium text-neutral-600 mb-1",
                                                                                        children: "Notes (optional)"
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 550,
                                                                                        columnNumber: 29
                                                                                    }, this),
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("textarea", {
                                                                                        rows: 2,
                                                                                        placeholder: "Any additional details...",
                                                                                        className: "w-full px-3 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 resize-none",
                                                                                        value: newMandatoryNotes,
                                                                                        onChange: (e)=>setNewMandatoryNotes(e.target.value)
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 553,
                                                                                        columnNumber: 29
                                                                                    }, this)
                                                                                ]
                                                                            }, void 0, true, {
                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                lineNumber: 549,
                                                                                columnNumber: 27
                                                                            }, this),
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
                                                                                type: "button",
                                                                                onClick: handleAddMandatoryItem,
                                                                                variant: "outline",
                                                                                className: "w-full",
                                                                                children: [
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$plus$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Plus$3e$__["Plus"], {
                                                                                        className: "h-4 w-4 mr-2"
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 567,
                                                                                        columnNumber: 29
                                                                                    }, this),
                                                                                    "Add Item"
                                                                                ]
                                                                            }, void 0, true, {
                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                lineNumber: 561,
                                                                                columnNumber: 27
                                                                            }, this)
                                                                        ]
                                                                    }, void 0, true, {
                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                        lineNumber: 526,
                                                                        columnNumber: 25
                                                                    }, this),
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                        className: "border rounded-lg overflow-hidden max-h-96 overflow-y-auto",
                                                                        children: mandatoryItems.length === 0 ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                            className: "p-4 text-center text-neutral-500 text-sm",
                                                                            children: "No items added yet"
                                                                        }, void 0, false, {
                                                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                            lineNumber: 575,
                                                                            columnNumber: 29
                                                                        }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("table", {
                                                                            className: "w-full",
                                                                            children: [
                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("thead", {
                                                                                    className: "bg-neutral-100 sticky top-0",
                                                                                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("tr", {
                                                                                        children: [
                                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("th", {
                                                                                                className: "px-3 py-2 text-left text-xs font-medium text-neutral-600",
                                                                                                children: "Item"
                                                                                            }, void 0, false, {
                                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                lineNumber: 582,
                                                                                                columnNumber: 35
                                                                                            }, this),
                                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("th", {
                                                                                                className: "px-3 py-2 text-left text-xs font-medium text-neutral-600",
                                                                                                children: "Qty"
                                                                                            }, void 0, false, {
                                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                lineNumber: 583,
                                                                                                columnNumber: 35
                                                                                            }, this),
                                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("th", {
                                                                                                className: "px-3 py-2 text-center text-xs font-medium text-neutral-600",
                                                                                                children: "Action"
                                                                                            }, void 0, false, {
                                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                lineNumber: 584,
                                                                                                columnNumber: 35
                                                                                            }, this)
                                                                                        ]
                                                                                    }, void 0, true, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 581,
                                                                                        columnNumber: 33
                                                                                    }, this)
                                                                                }, void 0, false, {
                                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                    lineNumber: 580,
                                                                                    columnNumber: 31
                                                                                }, this),
                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("tbody", {
                                                                                    children: mandatoryItems.map((item, index)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("tr", {
                                                                                            className: "border-t",
                                                                                            children: [
                                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("td", {
                                                                                                    className: "px-3 py-2 text-sm",
                                                                                                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                                                        children: [
                                                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                                                                className: "font-medium",
                                                                                                                children: item.description
                                                                                                            }, void 0, false, {
                                                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                                lineNumber: 592,
                                                                                                                columnNumber: 41
                                                                                                            }, this),
                                                                                                            item.notes && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                                                                className: "text-xs text-neutral-500",
                                                                                                                children: item.notes
                                                                                                            }, void 0, false, {
                                                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                                lineNumber: 594,
                                                                                                                columnNumber: 43
                                                                                                            }, this)
                                                                                                        ]
                                                                                                    }, void 0, true, {
                                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                        lineNumber: 591,
                                                                                                        columnNumber: 39
                                                                                                    }, this)
                                                                                                }, void 0, false, {
                                                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                    lineNumber: 590,
                                                                                                    columnNumber: 37
                                                                                                }, this),
                                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("td", {
                                                                                                    className: "px-3 py-2 text-sm",
                                                                                                    children: item.quantity
                                                                                                }, void 0, false, {
                                                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                    lineNumber: 598,
                                                                                                    columnNumber: 37
                                                                                                }, this),
                                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("td", {
                                                                                                    className: "px-3 py-2 text-center",
                                                                                                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
                                                                                                        variant: "outline",
                                                                                                        size: "sm",
                                                                                                        onClick: ()=>handleRemoveMandatoryItem(index),
                                                                                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$trash$2d$2$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Trash2$3e$__["Trash2"], {
                                                                                                            className: "h-4 w-4"
                                                                                                        }, void 0, false, {
                                                                                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                            lineNumber: 605,
                                                                                                            columnNumber: 41
                                                                                                        }, this)
                                                                                                    }, void 0, false, {
                                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                        lineNumber: 600,
                                                                                                        columnNumber: 39
                                                                                                    }, this)
                                                                                                }, void 0, false, {
                                                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                    lineNumber: 599,
                                                                                                    columnNumber: 37
                                                                                                }, this)
                                                                                            ]
                                                                                        }, index, true, {
                                                                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                            lineNumber: 589,
                                                                                            columnNumber: 35
                                                                                        }, this))
                                                                                }, void 0, false, {
                                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                    lineNumber: 587,
                                                                                    columnNumber: 31
                                                                                }, this)
                                                                            ]
                                                                        }, void 0, true, {
                                                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                            lineNumber: 579,
                                                                            columnNumber: 29
                                                                        }, this)
                                                                    }, void 0, false, {
                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                        lineNumber: 573,
                                                                        columnNumber: 25
                                                                    }, this)
                                                                ]
                                                            }, void 0, true, {
                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                lineNumber: 524,
                                                                columnNumber: 23
                                                            }, this)
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                        lineNumber: 518,
                                                        columnNumber: 21
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                lineNumber: 503,
                                                columnNumber: 19
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "space-y-3",
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                        className: "flex items-center gap-3 p-3 border rounded-lg cursor-pointer hover:bg-neutral-50",
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
                                                                type: "checkbox",
                                                                checked: hasPreferredItems,
                                                                onChange: (e)=>setHasPreferredItems(e.target.checked),
                                                                className: "w-4 h-4 text-blue-600"
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                lineNumber: 622,
                                                                columnNumber: 23
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                children: [
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                        className: "font-medium text-neutral-900",
                                                                        children: "Preferred Items"
                                                                    }, void 0, false, {
                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                        lineNumber: 629,
                                                                        columnNumber: 25
                                                                    }, this),
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                        className: "text-sm text-neutral-500",
                                                                        children: "Highly desired items that would be helpful"
                                                                    }, void 0, false, {
                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                        lineNumber: 630,
                                                                        columnNumber: 25
                                                                    }, this)
                                                                ]
                                                            }, void 0, true, {
                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                lineNumber: 628,
                                                                columnNumber: 23
                                                            }, this)
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                        lineNumber: 621,
                                                        columnNumber: 21
                                                    }, this),
                                                    hasPreferredItems && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        className: "border-t pt-4",
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("h4", {
                                                                className: "text-md font-semibold text-neutral-800 mb-3 flex items-center gap-2",
                                                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                                    className: "px-2 py-1 rounded text-xs font-medium bg-blue-100 text-blue-800",
                                                                    children: "Preferred Items"
                                                                }, void 0, false, {
                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                    lineNumber: 637,
                                                                    columnNumber: 25
                                                                }, this)
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                lineNumber: 636,
                                                                columnNumber: 23
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                className: "grid grid-cols-1 lg:grid-cols-2 gap-4",
                                                                children: [
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                        className: "space-y-3",
                                                                        children: [
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                                children: [
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                                                        className: "block text-xs font-medium text-neutral-600 mb-1",
                                                                                        children: "Item Description *"
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 645,
                                                                                        columnNumber: 29
                                                                                    }, this),
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Input$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Input"], {
                                                                                        type: "text",
                                                                                        placeholder: "e.g., Side dish",
                                                                                        value: newPreferredDesc,
                                                                                        onChange: (e)=>setNewPreferredDesc(e.target.value)
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 648,
                                                                                        columnNumber: 29
                                                                                    }, this)
                                                                                ]
                                                                            }, void 0, true, {
                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                lineNumber: 644,
                                                                                columnNumber: 27
                                                                            }, this),
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                                children: [
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                                                        className: "block text-xs font-medium text-neutral-600 mb-1",
                                                                                        children: "Quantity *"
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 656,
                                                                                        columnNumber: 29
                                                                                    }, this),
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Input$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Input"], {
                                                                                        type: "number",
                                                                                        min: "1",
                                                                                        value: newPreferredQty,
                                                                                        onChange: (e)=>setNewPreferredQty(parseInt(e.target.value) || 1)
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 659,
                                                                                        columnNumber: 29
                                                                                    }, this)
                                                                                ]
                                                                            }, void 0, true, {
                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                lineNumber: 655,
                                                                                columnNumber: 27
                                                                            }, this),
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                                children: [
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                                                        className: "block text-xs font-medium text-neutral-600 mb-1",
                                                                                        children: "Notes (optional)"
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 667,
                                                                                        columnNumber: 29
                                                                                    }, this),
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("textarea", {
                                                                                        rows: 2,
                                                                                        placeholder: "Any additional details...",
                                                                                        className: "w-full px-3 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 resize-none",
                                                                                        value: newPreferredNotes,
                                                                                        onChange: (e)=>setNewPreferredNotes(e.target.value)
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 670,
                                                                                        columnNumber: 29
                                                                                    }, this)
                                                                                ]
                                                                            }, void 0, true, {
                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                lineNumber: 666,
                                                                                columnNumber: 27
                                                                            }, this),
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
                                                                                type: "button",
                                                                                onClick: handleAddPreferredItem,
                                                                                variant: "outline",
                                                                                className: "w-full",
                                                                                children: [
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$plus$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Plus$3e$__["Plus"], {
                                                                                        className: "h-4 w-4 mr-2"
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 684,
                                                                                        columnNumber: 29
                                                                                    }, this),
                                                                                    "Add Item"
                                                                                ]
                                                                            }, void 0, true, {
                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                lineNumber: 678,
                                                                                columnNumber: 27
                                                                            }, this)
                                                                        ]
                                                                    }, void 0, true, {
                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                        lineNumber: 643,
                                                                        columnNumber: 25
                                                                    }, this),
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                        className: "border rounded-lg overflow-hidden max-h-96 overflow-y-auto",
                                                                        children: preferredItems.length === 0 ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                            className: "p-4 text-center text-neutral-500 text-sm",
                                                                            children: "No items added yet"
                                                                        }, void 0, false, {
                                                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                            lineNumber: 692,
                                                                            columnNumber: 29
                                                                        }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("table", {
                                                                            className: "w-full",
                                                                            children: [
                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("thead", {
                                                                                    className: "bg-neutral-100 sticky top-0",
                                                                                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("tr", {
                                                                                        children: [
                                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("th", {
                                                                                                className: "px-3 py-2 text-left text-xs font-medium text-neutral-600",
                                                                                                children: "Item"
                                                                                            }, void 0, false, {
                                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                lineNumber: 699,
                                                                                                columnNumber: 35
                                                                                            }, this),
                                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("th", {
                                                                                                className: "px-3 py-2 text-left text-xs font-medium text-neutral-600",
                                                                                                children: "Qty"
                                                                                            }, void 0, false, {
                                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                lineNumber: 700,
                                                                                                columnNumber: 35
                                                                                            }, this),
                                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("th", {
                                                                                                className: "px-3 py-2 text-center text-xs font-medium text-neutral-600",
                                                                                                children: "Action"
                                                                                            }, void 0, false, {
                                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                lineNumber: 701,
                                                                                                columnNumber: 35
                                                                                            }, this)
                                                                                        ]
                                                                                    }, void 0, true, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 698,
                                                                                        columnNumber: 33
                                                                                    }, this)
                                                                                }, void 0, false, {
                                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                    lineNumber: 697,
                                                                                    columnNumber: 31
                                                                                }, this),
                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("tbody", {
                                                                                    children: preferredItems.map((item, index)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("tr", {
                                                                                            className: "border-t",
                                                                                            children: [
                                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("td", {
                                                                                                    className: "px-3 py-2 text-sm",
                                                                                                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                                                        children: [
                                                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                                                                className: "font-medium",
                                                                                                                children: item.description
                                                                                                            }, void 0, false, {
                                                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                                lineNumber: 709,
                                                                                                                columnNumber: 41
                                                                                                            }, this),
                                                                                                            item.notes && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                                                                className: "text-xs text-neutral-500",
                                                                                                                children: item.notes
                                                                                                            }, void 0, false, {
                                                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                                lineNumber: 711,
                                                                                                                columnNumber: 43
                                                                                                            }, this)
                                                                                                        ]
                                                                                                    }, void 0, true, {
                                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                        lineNumber: 708,
                                                                                                        columnNumber: 39
                                                                                                    }, this)
                                                                                                }, void 0, false, {
                                                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                    lineNumber: 707,
                                                                                                    columnNumber: 37
                                                                                                }, this),
                                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("td", {
                                                                                                    className: "px-3 py-2 text-sm",
                                                                                                    children: item.quantity
                                                                                                }, void 0, false, {
                                                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                    lineNumber: 715,
                                                                                                    columnNumber: 37
                                                                                                }, this),
                                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("td", {
                                                                                                    className: "px-3 py-2 text-center",
                                                                                                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
                                                                                                        variant: "outline",
                                                                                                        size: "sm",
                                                                                                        onClick: ()=>handleRemovePreferredItem(index),
                                                                                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$trash$2d$2$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Trash2$3e$__["Trash2"], {
                                                                                                            className: "h-4 w-4"
                                                                                                        }, void 0, false, {
                                                                                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                            lineNumber: 722,
                                                                                                            columnNumber: 41
                                                                                                        }, this)
                                                                                                    }, void 0, false, {
                                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                        lineNumber: 717,
                                                                                                        columnNumber: 39
                                                                                                    }, this)
                                                                                                }, void 0, false, {
                                                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                    lineNumber: 716,
                                                                                                    columnNumber: 37
                                                                                                }, this)
                                                                                            ]
                                                                                        }, index, true, {
                                                                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                            lineNumber: 706,
                                                                                            columnNumber: 35
                                                                                        }, this))
                                                                                }, void 0, false, {
                                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                    lineNumber: 704,
                                                                                    columnNumber: 31
                                                                                }, this)
                                                                            ]
                                                                        }, void 0, true, {
                                                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                            lineNumber: 696,
                                                                            columnNumber: 29
                                                                        }, this)
                                                                    }, void 0, false, {
                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                        lineNumber: 690,
                                                                        columnNumber: 25
                                                                    }, this)
                                                                ]
                                                            }, void 0, true, {
                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                lineNumber: 641,
                                                                columnNumber: 23
                                                            }, this)
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                        lineNumber: 635,
                                                        columnNumber: 21
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                lineNumber: 620,
                                                columnNumber: 19
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                className: "space-y-3",
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                        className: "flex items-center gap-3 p-3 border rounded-lg cursor-pointer hover:bg-neutral-50",
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("input", {
                                                                type: "checkbox",
                                                                checked: hasSuggestedItems,
                                                                onChange: (e)=>setHasSuggestedItems(e.target.checked),
                                                                className: "w-4 h-4 text-green-600"
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                lineNumber: 739,
                                                                columnNumber: 23
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                children: [
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                        className: "font-medium text-neutral-900",
                                                                        children: "Suggested Items"
                                                                    }, void 0, false, {
                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                        lineNumber: 746,
                                                                        columnNumber: 25
                                                                    }, this),
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                        className: "text-sm text-neutral-500",
                                                                        children: "Optional items that would be nice to have"
                                                                    }, void 0, false, {
                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                        lineNumber: 747,
                                                                        columnNumber: 25
                                                                    }, this)
                                                                ]
                                                            }, void 0, true, {
                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                lineNumber: 745,
                                                                columnNumber: 23
                                                            }, this)
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                        lineNumber: 738,
                                                        columnNumber: 21
                                                    }, this),
                                                    hasSuggestedItems && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        className: "border-t pt-4",
                                                        children: [
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("h4", {
                                                                className: "text-md font-semibold text-neutral-800 mb-3 flex items-center gap-2",
                                                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("span", {
                                                                    className: "px-2 py-1 rounded text-xs font-medium bg-green-100 text-green-800",
                                                                    children: "Suggested Items"
                                                                }, void 0, false, {
                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                    lineNumber: 754,
                                                                    columnNumber: 25
                                                                }, this)
                                                            }, void 0, false, {
                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                lineNumber: 753,
                                                                columnNumber: 23
                                                            }, this),
                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                className: "grid grid-cols-1 lg:grid-cols-2 gap-4",
                                                                children: [
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                        className: "space-y-3",
                                                                        children: [
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                                children: [
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                                                        className: "block text-xs font-medium text-neutral-600 mb-1",
                                                                                        children: "Item Description *"
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 762,
                                                                                        columnNumber: 29
                                                                                    }, this),
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Input$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Input"], {
                                                                                        type: "text",
                                                                                        placeholder: "e.g., Dessert",
                                                                                        value: newSuggestedDesc,
                                                                                        onChange: (e)=>setNewSuggestedDesc(e.target.value)
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 765,
                                                                                        columnNumber: 29
                                                                                    }, this)
                                                                                ]
                                                                            }, void 0, true, {
                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                lineNumber: 761,
                                                                                columnNumber: 27
                                                                            }, this),
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                                children: [
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                                                        className: "block text-xs font-medium text-neutral-600 mb-1",
                                                                                        children: "Quantity *"
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 773,
                                                                                        columnNumber: 29
                                                                                    }, this),
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Input$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Input"], {
                                                                                        type: "number",
                                                                                        min: "1",
                                                                                        value: newSuggestedQty,
                                                                                        onChange: (e)=>setNewSuggestedQty(parseInt(e.target.value) || 1)
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 776,
                                                                                        columnNumber: 29
                                                                                    }, this)
                                                                                ]
                                                                            }, void 0, true, {
                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                lineNumber: 772,
                                                                                columnNumber: 27
                                                                            }, this),
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                                children: [
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("label", {
                                                                                        className: "block text-xs font-medium text-neutral-600 mb-1",
                                                                                        children: "Notes (optional)"
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 784,
                                                                                        columnNumber: 29
                                                                                    }, this),
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("textarea", {
                                                                                        rows: 2,
                                                                                        placeholder: "Any additional details...",
                                                                                        className: "w-full px-3 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 resize-none",
                                                                                        value: newSuggestedNotes,
                                                                                        onChange: (e)=>setNewSuggestedNotes(e.target.value)
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 787,
                                                                                        columnNumber: 29
                                                                                    }, this)
                                                                                ]
                                                                            }, void 0, true, {
                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                lineNumber: 783,
                                                                                columnNumber: 27
                                                                            }, this),
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
                                                                                type: "button",
                                                                                onClick: handleAddSuggestedItem,
                                                                                variant: "outline",
                                                                                className: "w-full",
                                                                                children: [
                                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$plus$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Plus$3e$__["Plus"], {
                                                                                        className: "h-4 w-4 mr-2"
                                                                                    }, void 0, false, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 801,
                                                                                        columnNumber: 29
                                                                                    }, this),
                                                                                    "Add Item"
                                                                                ]
                                                                            }, void 0, true, {
                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                lineNumber: 795,
                                                                                columnNumber: 27
                                                                            }, this)
                                                                        ]
                                                                    }, void 0, true, {
                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                        lineNumber: 760,
                                                                        columnNumber: 25
                                                                    }, this),
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                        className: "border rounded-lg overflow-hidden max-h-96 overflow-y-auto",
                                                                        children: suggestedItems.length === 0 ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                            className: "p-4 text-center text-neutral-500 text-sm",
                                                                            children: "No items added yet"
                                                                        }, void 0, false, {
                                                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                            lineNumber: 809,
                                                                            columnNumber: 29
                                                                        }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("table", {
                                                                            className: "w-full",
                                                                            children: [
                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("thead", {
                                                                                    className: "bg-neutral-100 sticky top-0",
                                                                                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("tr", {
                                                                                        children: [
                                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("th", {
                                                                                                className: "px-3 py-2 text-left text-xs font-medium text-neutral-600",
                                                                                                children: "Item"
                                                                                            }, void 0, false, {
                                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                lineNumber: 816,
                                                                                                columnNumber: 35
                                                                                            }, this),
                                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("th", {
                                                                                                className: "px-3 py-2 text-left text-xs font-medium text-neutral-600",
                                                                                                children: "Qty"
                                                                                            }, void 0, false, {
                                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                lineNumber: 817,
                                                                                                columnNumber: 35
                                                                                            }, this),
                                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("th", {
                                                                                                className: "px-3 py-2 text-center text-xs font-medium text-neutral-600",
                                                                                                children: "Action"
                                                                                            }, void 0, false, {
                                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                lineNumber: 818,
                                                                                                columnNumber: 35
                                                                                            }, this)
                                                                                        ]
                                                                                    }, void 0, true, {
                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                        lineNumber: 815,
                                                                                        columnNumber: 33
                                                                                    }, this)
                                                                                }, void 0, false, {
                                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                    lineNumber: 814,
                                                                                    columnNumber: 31
                                                                                }, this),
                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("tbody", {
                                                                                    children: suggestedItems.map((item, index)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("tr", {
                                                                                            className: "border-t",
                                                                                            children: [
                                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("td", {
                                                                                                    className: "px-3 py-2 text-sm",
                                                                                                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                                                        children: [
                                                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                                                                className: "font-medium",
                                                                                                                children: item.description
                                                                                                            }, void 0, false, {
                                                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                                lineNumber: 826,
                                                                                                                columnNumber: 41
                                                                                                            }, this),
                                                                                                            item.notes && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                                                                className: "text-xs text-neutral-500",
                                                                                                                children: item.notes
                                                                                                            }, void 0, false, {
                                                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                                lineNumber: 828,
                                                                                                                columnNumber: 43
                                                                                                            }, this)
                                                                                                        ]
                                                                                                    }, void 0, true, {
                                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                        lineNumber: 825,
                                                                                                        columnNumber: 39
                                                                                                    }, this)
                                                                                                }, void 0, false, {
                                                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                    lineNumber: 824,
                                                                                                    columnNumber: 37
                                                                                                }, this),
                                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("td", {
                                                                                                    className: "px-3 py-2 text-sm",
                                                                                                    children: item.quantity
                                                                                                }, void 0, false, {
                                                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                    lineNumber: 832,
                                                                                                    columnNumber: 37
                                                                                                }, this),
                                                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("td", {
                                                                                                    className: "px-3 py-2 text-center",
                                                                                                    children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
                                                                                                        variant: "outline",
                                                                                                        size: "sm",
                                                                                                        onClick: ()=>handleRemoveSuggestedItem(index),
                                                                                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$trash$2d$2$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Trash2$3e$__["Trash2"], {
                                                                                                            className: "h-4 w-4"
                                                                                                        }, void 0, false, {
                                                                                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                            lineNumber: 839,
                                                                                                            columnNumber: 41
                                                                                                        }, this)
                                                                                                    }, void 0, false, {
                                                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                        lineNumber: 834,
                                                                                                        columnNumber: 39
                                                                                                    }, this)
                                                                                                }, void 0, false, {
                                                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                                    lineNumber: 833,
                                                                                                    columnNumber: 37
                                                                                                }, this)
                                                                                            ]
                                                                                        }, index, true, {
                                                                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                            lineNumber: 823,
                                                                                            columnNumber: 35
                                                                                        }, this))
                                                                                }, void 0, false, {
                                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                    lineNumber: 821,
                                                                                    columnNumber: 31
                                                                                }, this)
                                                                            ]
                                                                        }, void 0, true, {
                                                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                            lineNumber: 813,
                                                                            columnNumber: 29
                                                                        }, this)
                                                                    }, void 0, false, {
                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                        lineNumber: 807,
                                                                        columnNumber: 25
                                                                    }, this)
                                                                ]
                                                            }, void 0, true, {
                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                lineNumber: 758,
                                                                columnNumber: 23
                                                            }, this)
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                        lineNumber: 752,
                                                        columnNumber: 21
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                lineNumber: 737,
                                                columnNumber: 19
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                        lineNumber: 497,
                                        columnNumber: 15
                                    }, this),
                                    submitError && /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "p-3 bg-red-50 border border-red-200 rounded-lg",
                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                            className: "text-sm text-red-600",
                                            children: submitError
                                        }, void 0, false, {
                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                            lineNumber: 857,
                                            columnNumber: 19
                                        }, this)
                                    }, void 0, false, {
                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                        lineNumber: 856,
                                        columnNumber: 17
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                        className: "flex items-center justify-end gap-3 pt-4",
                                        children: [
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
                                                variant: "outline",
                                                onClick: ()=>{
                                                    setShowForm(false);
                                                    setCategory('');
                                                    setDescription('');
                                                    setHasMandatoryItems(false);
                                                    setHasPreferredItems(false);
                                                    setHasSuggestedItems(false);
                                                    setMandatoryItems([]);
                                                    setPreferredItems([]);
                                                    setSuggestedItems([]);
                                                    setSubmitError(null);
                                                },
                                                children: "Cancel"
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                lineNumber: 863,
                                                columnNumber: 17
                                            }, this),
                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
                                                onClick: handleCreateSignUpList,
                                                disabled: createSignUpListMutation.isPending,
                                                style: {
                                                    background: '#FF7900'
                                                },
                                                children: createSignUpListMutation.isPending ? 'Creating...' : 'Create Sign-Up List'
                                            }, void 0, false, {
                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                lineNumber: 880,
                                                columnNumber: 17
                                            }, this)
                                        ]
                                    }, void 0, true, {
                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                        lineNumber: 862,
                                        columnNumber: 15
                                    }, this)
                                ]
                            }, void 0, true, {
                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                lineNumber: 466,
                                columnNumber: 13
                            }, this)
                        ]
                    }, void 0, true, {
                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                        lineNumber: 459,
                        columnNumber: 11
                    }, this),
                    signUpsLoading ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "grid grid-cols-1 gap-6",
                        children: [
                            ...Array(2)
                        ].map((_, i)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Card"], {
                                className: "animate-pulse",
                                children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardContent"], {
                                    className: "p-6",
                                    children: [
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                            className: "h-6 bg-neutral-200 rounded w-1/3 mb-4"
                                        }, void 0, false, {
                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                            lineNumber: 898,
                                            columnNumber: 19
                                        }, this),
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                            className: "h-4 bg-neutral-200 rounded w-2/3"
                                        }, void 0, false, {
                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                            lineNumber: 899,
                                            columnNumber: 19
                                        }, this)
                                    ]
                                }, void 0, true, {
                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                    lineNumber: 897,
                                    columnNumber: 17
                                }, this)
                            }, i, false, {
                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                lineNumber: 896,
                                columnNumber: 15
                            }, this))
                    }, void 0, false, {
                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                        lineNumber: 894,
                        columnNumber: 11
                    }, this) : signUpLists && signUpLists.length > 0 ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                        className: "grid grid-cols-1 gap-6",
                        children: signUpLists.map((list)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Card"], {
                                children: [
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardHeader"], {
                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                            className: "flex items-start justify-between",
                                            children: [
                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                    children: [
                                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardTitle"], {
                                                            style: {
                                                                color: '#8B1538'
                                                            },
                                                            children: list.category
                                                        }, void 0, false, {
                                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                            lineNumber: 911,
                                                            columnNumber: 23
                                                        }, this),
                                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardDescription"], {
                                                            className: "mt-2",
                                                            children: list.description
                                                        }, void 0, false, {
                                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                            lineNumber: 912,
                                                            columnNumber: 23
                                                        }, this)
                                                    ]
                                                }, void 0, true, {
                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                    lineNumber: 910,
                                                    columnNumber: 21
                                                }, this),
                                                deleteConfirmId === list.id ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                    className: "flex gap-2",
                                                    children: [
                                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
                                                            variant: "destructive",
                                                            size: "sm",
                                                            onClick: ()=>handleDeleteSignUpList(list.id),
                                                            disabled: removeSignUpListMutation.isPending,
                                                            children: "Confirm Delete"
                                                        }, void 0, false, {
                                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                            lineNumber: 916,
                                                            columnNumber: 25
                                                        }, this),
                                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
                                                            variant: "outline",
                                                            size: "sm",
                                                            onClick: ()=>setDeleteConfirmId(null),
                                                            children: "Cancel"
                                                        }, void 0, false, {
                                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                            lineNumber: 924,
                                                            columnNumber: 25
                                                        }, this)
                                                    ]
                                                }, void 0, true, {
                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                    lineNumber: 915,
                                                    columnNumber: 23
                                                }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                    className: "flex gap-2",
                                                    children: [
                                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
                                                            variant: "outline",
                                                            size: "sm",
                                                            onClick: ()=>router.push(`/events/${eventId}/manage-signups/${list.id}`),
                                                            className: "text-orange-600 hover:text-orange-700",
                                                            children: [
                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$square$2d$pen$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Edit$3e$__["Edit"], {
                                                                    className: "h-4 w-4 mr-2"
                                                                }, void 0, false, {
                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                    lineNumber: 940,
                                                                    columnNumber: 27
                                                                }, this),
                                                                "Edit"
                                                            ]
                                                        }, void 0, true, {
                                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                            lineNumber: 934,
                                                            columnNumber: 25
                                                        }, this),
                                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
                                                            variant: "outline",
                                                            size: "sm",
                                                            onClick: ()=>setDeleteConfirmId(list.id),
                                                            className: "text-red-600 hover:text-red-700",
                                                            children: [
                                                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$trash$2d$2$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Trash2$3e$__["Trash2"], {
                                                                    className: "h-4 w-4 mr-2"
                                                                }, void 0, false, {
                                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                    lineNumber: 949,
                                                                    columnNumber: 27
                                                                }, this),
                                                                "Delete"
                                                            ]
                                                        }, void 0, true, {
                                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                            lineNumber: 943,
                                                            columnNumber: 25
                                                        }, this)
                                                    ]
                                                }, void 0, true, {
                                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                    lineNumber: 933,
                                                    columnNumber: 23
                                                }, this)
                                            ]
                                        }, void 0, true, {
                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                            lineNumber: 909,
                                            columnNumber: 19
                                        }, this)
                                    }, void 0, false, {
                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                        lineNumber: 908,
                                        columnNumber: 17
                                    }, this),
                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardContent"], {
                                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                            className: "space-y-4",
                                            children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                children: [
                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("h4", {
                                                        className: "text-sm font-semibold text-neutral-700 mb-3",
                                                        children: [
                                                            "Commitments (",
                                                            list.commitmentCount || 0,
                                                            ")"
                                                        ]
                                                    }, void 0, true, {
                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                        lineNumber: 960,
                                                        columnNumber: 23
                                                    }, this),
                                                    (list.commitments?.length ?? 0) > 0 ? /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                        className: "space-y-2",
                                                        children: (list.commitments || []).map((commitment)=>/*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                className: "flex items-center justify-between p-3 bg-neutral-50 rounded-lg",
                                                                children: [
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("div", {
                                                                        className: "flex-1",
                                                                        children: [
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                                className: "font-medium text-neutral-900",
                                                                                children: commitment.itemDescription
                                                                            }, void 0, false, {
                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                lineNumber: 971,
                                                                                columnNumber: 33
                                                                            }, this),
                                                                            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                                className: "text-sm text-neutral-500",
                                                                                children: [
                                                                                    "Quantity: ",
                                                                                    commitment.quantity,
                                                                                    " • User: ",
                                                                                    commitment.userId.substring(0, 8),
                                                                                    "..."
                                                                                ]
                                                                            }, void 0, true, {
                                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                                lineNumber: 974,
                                                                                columnNumber: 33
                                                                            }, this)
                                                                        ]
                                                                    }, void 0, true, {
                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                        lineNumber: 970,
                                                                        columnNumber: 31
                                                                    }, this),
                                                                    /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                                        className: "text-sm text-neutral-400",
                                                                        children: new Date(commitment.committedAt).toLocaleDateString()
                                                                    }, void 0, false, {
                                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                        lineNumber: 978,
                                                                        columnNumber: 31
                                                                    }, this)
                                                                ]
                                                            }, commitment.id, true, {
                                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                                lineNumber: 966,
                                                                columnNumber: 29
                                                            }, this))
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                        lineNumber: 964,
                                                        columnNumber: 25
                                                    }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                                        className: "text-neutral-500 text-sm italic",
                                                        children: "No commitments yet"
                                                    }, void 0, false, {
                                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                        lineNumber: 985,
                                                        columnNumber: 25
                                                    }, this)
                                                ]
                                            }, void 0, true, {
                                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                                lineNumber: 959,
                                                columnNumber: 21
                                            }, this)
                                        }, void 0, false, {
                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                            lineNumber: 957,
                                            columnNumber: 19
                                        }, this)
                                    }, void 0, false, {
                                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                        lineNumber: 956,
                                        columnNumber: 17
                                    }, this)
                                ]
                            }, list.id, true, {
                                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                lineNumber: 907,
                                columnNumber: 15
                            }, this))
                    }, void 0, false, {
                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                        lineNumber: 905,
                        columnNumber: 11
                    }, this) : /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Card"], {
                        children: /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Card$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["CardContent"], {
                            className: "p-12 text-center",
                            children: [
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$list$2d$plus$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__ListPlus$3e$__["ListPlus"], {
                                    className: "h-16 w-16 mx-auto mb-4 text-neutral-400"
                                }, void 0, false, {
                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                    lineNumber: 998,
                                    columnNumber: 15
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("h3", {
                                    className: "text-xl font-semibold text-neutral-900 mb-2",
                                    children: "No Sign-Up Lists Yet"
                                }, void 0, false, {
                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                    lineNumber: 999,
                                    columnNumber: 15
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])("p", {
                                    className: "text-neutral-500 mb-6",
                                    children: "Create your first sign-up list to let attendees commit to bringing items"
                                }, void 0, false, {
                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                    lineNumber: 1002,
                                    columnNumber: 15
                                }, this),
                                /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$ui$2f$Button$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["Button"], {
                                    onClick: ()=>setShowForm(true),
                                    style: {
                                        background: '#FF7900'
                                    },
                                    children: [
                                        /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$lucide$2d$react$2f$dist$2f$esm$2f$icons$2f$plus$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__$3c$export__default__as__Plus$3e$__["Plus"], {
                                            className: "h-4 w-4 mr-2"
                                        }, void 0, false, {
                                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                            lineNumber: 1009,
                                            columnNumber: 17
                                        }, this),
                                        "Create Sign-Up List"
                                    ]
                                }, void 0, true, {
                                    fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                                    lineNumber: 1005,
                                    columnNumber: 15
                                }, this)
                            ]
                        }, void 0, true, {
                            fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                            lineNumber: 997,
                            columnNumber: 13
                        }, this)
                    }, void 0, false, {
                        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                        lineNumber: 996,
                        columnNumber: 11
                    }, this)
                ]
            }, void 0, true, {
                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                lineNumber: 396,
                columnNumber: 7
            }, this),
            /*#__PURE__*/ (0, __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$server$2f$route$2d$modules$2f$app$2d$page$2f$vendored$2f$ssr$2f$react$2d$jsx$2d$dev$2d$runtime$2e$js__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["jsxDEV"])(__TURBOPACK__imported__module__$5b$project$5d2f$src$2f$presentation$2f$components$2f$layout$2f$Footer$2e$tsx__$5b$app$2d$ssr$5d$__$28$ecmascript$29$__["default"], {}, void 0, false, {
                fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
                lineNumber: 1017,
                columnNumber: 7
            }, this)
        ]
    }, void 0, true, {
        fileName: "[project]/src/app/events/[id]/manage-signups/page.tsx",
        lineNumber: 362,
        columnNumber: 5
    }, this);
}
}),
];

//# sourceMappingURL=src_c013bb44._.js.map