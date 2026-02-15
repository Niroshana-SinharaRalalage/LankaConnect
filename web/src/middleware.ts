import { NextRequest, NextResponse } from 'next/server';

/**
 * Middleware to handle www → non-www redirect
 *
 * SEO Best Practice: Canonical URL is lankaconnect.app (non-www)
 *
 * This middleware:
 * - Redirects www.lankaconnect.app → lankaconnect.app with 301 (Permanent Redirect)
 * - Passes through all other domains (localhost, staging, production non-www)
 * - Preserves full URL path and query parameters
 *
 * @see docs/RCA_WWW_SUBDOMAIN_MISSING.md
 * @see docs/WWW_SUBDOMAIN_IMPLEMENTATION_GUIDE.md
 */
export function middleware(request: NextRequest): NextResponse {
  try {
    // Get the hostname from the request
    const hostname = request.nextUrl.hostname;

    // Only redirect www.lankaconnect.app → lankaconnect.app
    if (hostname === 'www.lankaconnect.app') {
      // Clone the URL and change hostname
      const url = request.nextUrl.clone();
      url.hostname = 'lankaconnect.app';

      // Log redirect for observability (in production, this goes to Azure Container App logs)
      if (process.env.NODE_ENV === 'production') {
        console.log('[WWW_REDIRECT]', {
          from: request.nextUrl.href,
          to: url.href,
          timestamp: new Date().toISOString(),
        });
      }

      // Return 301 Permanent Redirect
      // This tells search engines that www is permanently redirected to non-www
      return NextResponse.redirect(url, 301);
    }

    // Pass through for all other domains
    return NextResponse.next();
  } catch (error) {
    // If middleware fails, log error and pass through
    // This ensures the site remains accessible even if middleware has issues
    console.error('[MIDDLEWARE_ERROR] WWW redirect middleware failed:', {
      error: error instanceof Error ? error.message : String(error),
      url: request.url,
      timestamp: new Date().toISOString(),
    });

    // Fail gracefully - allow request to proceed
    return NextResponse.next();
  }
}

/**
 * Matcher configuration
 *
 * Apply middleware to all routes except:
 * - Static files (_next/static, favicon.ico, etc.)
 * - API routes (handled by Next.js API middleware)
 * - Image optimization (_next/image)
 *
 * This improves performance by not running middleware on static assets
 */
export const config = {
  matcher: [
    /*
     * Match all request paths except:
     * - _next/static (static files)
     * - _next/image (image optimization)
     * - favicon.ico, robots.txt, sitemap.xml (static files)
     * - Files with extensions (.js, .css, .png, etc.)
     */
    '/((?!_next/static|_next/image|favicon.ico|robots.txt|sitemap.xml|.*\\..*$).*)',
  ],
};
