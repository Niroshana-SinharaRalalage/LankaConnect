/**
 * Next.js API Route Proxy for Staging Backend
 *
 * Purpose: Forward API requests from localhost:3000 to Azure staging backend
 *
 * Why needed:
 * - Frontend: http://localhost:3000 (HTTP)
 * - Backend: https://staging.azurecontainerapps.io (HTTPS)
 * - Browsers block Secure cookies from being sent over HTTP
 * - This proxy makes browser see same-origin (localhost:3000/api/proxy/...)
 *
 * How it works:
 * - Browser sends request to localhost:3000/api/proxy/Auth/login (same-origin HTTP)
 * - Next.js server forwards to staging backend over HTTPS (server-to-server, no browser restrictions)
 * - Backend sets Secure cookies in response
 * - Next.js server receives them and forwards to browser
 * - Browser stores cookies under localhost:3000 domain
 * - Subsequent requests include cookies (same-origin)
 */

import { NextRequest, NextResponse } from 'next/server';

// IMPORTANT: Use explicit staging URL, NOT NEXT_PUBLIC_API_URL (which points to /api/proxy)
// Always use the staging backend URL (local development should still use Azure staging for API calls)
const BACKEND_URL = 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api';

export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ path: string[] }> }
) {
  const resolvedParams = await params;
  return forwardRequest(request, resolvedParams.path, 'GET');
}

export async function POST(
  request: NextRequest,
  { params }: { params: Promise<{ path: string[] }> }
) {
  const resolvedParams = await params;
  return forwardRequest(request, resolvedParams.path, 'POST');
}

export async function PUT(
  request: NextRequest,
  { params }: { params: Promise<{ path: string[] }> }
) {
  const resolvedParams = await params;
  return forwardRequest(request, resolvedParams.path, 'PUT');
}

export async function DELETE(
  request: NextRequest,
  { params }: { params: Promise<{ path: string[] }> }
) {
  const resolvedParams = await params;
  return forwardRequest(request, resolvedParams.path, 'DELETE');
}

export async function PATCH(
  request: NextRequest,
  { params }: { params: Promise<{ path: string[] }> }
) {
  const resolvedParams = await params;
  return forwardRequest(request, resolvedParams.path, 'PATCH');
}

async function forwardRequest(
  request: NextRequest,
  pathSegments: string[],
  method: string
) {
  try {
    const path = pathSegments.join('/');
    // CRITICAL FIX: Preserve query string parameters for filtering, pagination, etc.
    // Without this, GET requests with query params (e.g., /events?category=0) lose their filters
    const queryString = request.nextUrl.search; // Returns "?param=value" or empty string
    const targetUrl = `${BACKEND_URL}/${path}${queryString}`;

    // Get Content-Type to detect multipart/form-data
    const contentType = request.headers.get('content-type');
    const isMultipart = contentType?.includes('multipart/form-data');

    // Get request body if present
    // CRITICAL: For multipart/form-data, stream body as-is to preserve binary data and boundary
    let body: BodyInit | undefined;
    if (method !== 'GET' && method !== 'DELETE') {
      if (isMultipart) {
        // Stream multipart body as-is (don't read as text - corrupts binary data)
        body = request.body ?? undefined;
      } else {
        // For JSON, read as text
        try {
          body = await request.text();
        } catch {
          body = undefined;
        }
      }
    }

    // Forward cookies from request
    const cookies = request.cookies.getAll();
    const cookieHeader = cookies.map(c => `${c.name}=${c.value}`).join('; ');

    // PHASE 6A.10: Detailed cookie forwarding logging for token refresh debugging
    const isAuthRefresh = path === 'Auth/refresh';
    const hasRefreshToken = cookies.some(c => c.name === 'refreshToken');

    if (isAuthRefresh) {
      console.log('🔍 [PROXY] Token Refresh Request Detected', {
        path,
        method,
        allCookies: cookies.map(c => ({ name: c.name, valueLength: c.value.length })),
        hasRefreshToken,
        refreshTokenValue: hasRefreshToken ? cookies.find(c => c.name === 'refreshToken')?.value.substring(0, 30) + '...' : 'NOT FOUND',
        cookieHeaderLength: cookieHeader.length,
        totalCookies: cookies.length,
      });
    }

    // Build headers for backend request
    const headers: HeadersInit = {
      // CRITICAL: Preserve exact Content-Type for multipart (includes boundary parameter)
      'Content-Type': contentType || 'application/json',
      'Accept': request.headers.get('accept') || 'application/json',
    };

    // Forward Authorization header if present
    const authHeader = request.headers.get('authorization');
    if (authHeader) {
      headers['Authorization'] = authHeader;
    }

    // Forward cookies
    if (cookieHeader) {
      headers['Cookie'] = cookieHeader;
    }

    // PHASE 6A.10: Log headers being sent to backend for token refresh
    if (isAuthRefresh) {
      console.log('🔍 [PROXY] Headers being sent to backend:', {
        'Content-Type': headers['Content-Type'],
        'Authorization': authHeader ? `Bearer ${authHeader.toString().substring(7, 30)}...` : 'Not present',
        'Cookie': cookieHeader ? `${cookieHeader.substring(0, 100)}...` : 'Not present',
        cookieHeaderFull: cookieHeader,
      });
    }

    console.log(`[Proxy] ${method} ${targetUrl}`, {
      contentType,
      isMultipart,
      hasBody: !!body,
      isAuthRefresh,
      hasRefreshToken,
    });

    // Build fetch options
    const fetchOptions: RequestInit = {
      method,
      headers,
      body,
      credentials: 'include', // Important: include cookies
      // CRITICAL: Large file uploads (videos up to 100MB) need longer timeout
      // Backend has 5-minute timeout for video uploads, proxy needs at least as long
      // Add buffer for network overhead: 10 minutes total
      signal: AbortSignal.timeout(600000), // 10 minutes
    };

    // Only add duplex for multipart/form-data streaming
    if (isMultipart && body) {
      // @ts-ignore - duplex is required for streaming request bodies but not in TS types yet
      fetchOptions.duplex = 'half';
    }

    // Make request to backend
    const response = await fetch(targetUrl, fetchOptions);

    // PHASE 6A.10: Log backend response for token refresh
    if (isAuthRefresh) {
      console.log('🔍 [PROXY] Backend Response:', {
        status: response.status,
        statusText: response.statusText,
        headers: {
          'Content-Type': response.headers.get('content-type'),
          'Set-Cookie': response.headers.get('set-cookie'),
        },
        setCookieHeaders: response.headers.getSetCookie?.() || [],
      });
    }

    // Get response body - handle empty responses (e.g., successful free event registration returns null)
    const responseText = await response.text();
    let responseBody: any;

    // Special handling for RSVP endpoint
    const isRsvpEndpoint = path.includes('events') && path.endsWith('rsvp');

    // Phase 6A.13: Log set-primary requests for debugging
    const isSetPrimaryEndpoint = path.includes('set-primary');
    if (isSetPrimaryEndpoint) {
      console.log('[Proxy] Set Primary Image Response:', {
        path,
        status: response.status,
        statusText: response.statusText,
        responseText: responseText?.substring(0, 500),
      });
    }

    // Handle empty/null responses
    if (!responseText || responseText === 'null' || responseText.trim() === '') {
      console.log('[Proxy] Empty response body received (normal for free event registration)');
      responseBody = null;
    } else {
      try {
        responseBody = JSON.parse(responseText);
      } catch {
        responseBody = responseText;
      }
    }

    // Log RSVP responses for debugging
    if (isRsvpEndpoint) {
      console.log('[Proxy] RSVP Response:', {
        status: response.status,
        responseBody,
        responseText: responseText?.substring(0, 100),
        isNull: responseBody === null,
        isEmpty: !responseText || responseText.trim() === '',
      });
    }

    // PHASE 6A.10: Log response body for token refresh
    if (isAuthRefresh) {
      console.log('🔍 [PROXY] Backend Response Body:', {
        status: response.status,
        bodyType: typeof responseBody,
        hasAccessToken: responseBody?.accessToken ? 'YES' : 'NO',
        hasError: responseBody?.error ? 'YES' : 'NO',
        errorMessage: responseBody?.error || responseBody?.message || 'None',
        body: responseBody,
      });
    }

    // Create Next.js response - handle different response types
    let nextResponse: NextResponse;

    // Special handling for 204 No Content - MUST NOT have a body per HTTP spec
    if (response.status === 204) {
      nextResponse = new NextResponse(null, {
        status: 204,
        statusText: 'No Content',
      });
    } else {
      // For other responses, return JSON
      // Return 'null' as JSON string for null responses (not empty string)
      const responseContent = responseBody === null ? 'null' : JSON.stringify(responseBody);
      nextResponse = new NextResponse(responseContent, {
        status: response.status,
        headers: {
          'Content-Type': 'application/json',
        },
      });
    }

    // Forward Set-Cookie headers from backend
    const setCookieHeaders = response.headers.getSetCookie?.() || [];
    setCookieHeaders.forEach(cookie => {
      // Parse cookie and convert Secure/SameSite for localhost
      const cookieParts = cookie.split(';').map(p => p.trim());
      const [nameValue, ...attributes] = cookieParts;

      // Rebuild cookie without Secure flag for localhost
      const newAttributes = attributes
        .filter(attr => !attr.toLowerCase().startsWith('secure'))
        .filter(attr => !attr.toLowerCase().startsWith('samesite=none'));

      // Add SameSite=Lax for localhost
      newAttributes.push('SameSite=Lax');
      newAttributes.push('Path=/');

      const newCookie = [nameValue, ...newAttributes].join('; ');
      nextResponse.headers.append('Set-Cookie', newCookie);
    });

    return nextResponse;
  } catch (error) {
    console.error('[Proxy] Error:', error);
    return NextResponse.json(
      { error: 'Proxy error', details: error instanceof Error ? error.message : 'Unknown error' },
      { status: 500 }
    );
  }
}
