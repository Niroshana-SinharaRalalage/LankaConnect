/**
 * Next.js API Route Proxy
 *
 * Purpose: Forward API requests to the backend API server
 *
 * Why needed:
 * - Frontend and backend may run on different origins
 * - Browsers block Secure cookies from being sent cross-origin
 * - This proxy makes browser see same-origin (/api/proxy/...)
 *
 * Safety:
 * - In production (NODE_ENV=production), BACKEND_API_URL is REQUIRED.
 *   If missing, all proxy requests return 503 (fail-closed).
 *   This prevents accidental routing to staging.
 * - In development, falls back to staging API for convenience.
 *
 * Post-Incident Fix (2026-04-17): Removed hardcoded staging fallback in
 * production. Uses env-validation module for fail-closed safety.
 */

import { NextRequest, NextResponse } from 'next/server';
import { getEnvValidation } from '@/lib/env-validation';

// Fail-closed: in production, BACKEND_URL is null if BACKEND_API_URL is missing.
// The guard in forwardRequest() returns 503 for every request.
const envCheck = getEnvValidation();
const BACKEND_URL = envCheck.backendUrl;

if (!envCheck.isValid) {
  console.error(
    `[PROXY] FATAL: Environment validation failed. Errors: ${envCheck.errors.join('; ')}. ` +
    `All proxy requests will return 503 Service Unavailable.`
  );
} else {
  console.log(`[PROXY] Backend URL configured: ${BACKEND_URL}`);
}

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
  // Fail-closed guard: if BACKEND_URL is null, the env var is missing in
  // a deployed environment. Return 503 instead of silently routing to staging.
  if (!BACKEND_URL) {
    console.error(
      '[PROXY] Refusing to proxy request: BACKEND_API_URL is not configured. ' +
      `Path: ${pathSegments.join('/')}, Method: ${method}`
    );
    return NextResponse.json(
      {
        error: 'Service configuration error',
        message: 'Backend API URL is not configured. This is a deployment issue — not a user error.',
      },
      { status: 503 }
    );
  }

  try {
    const path = pathSegments.join('/');
    // CRITICAL FIX: Preserve query string parameters for filtering, pagination, etc.
    // Without this, GET requests with query params (e.g., /events?category=0) lose their filters
    const queryString = request.nextUrl.search; // Returns "?param=value" or empty string
    const targetUrl = `${BACKEND_URL}/${path}${queryString}`;

    // Get Content-Type to detect multipart/form-data
    const contentType = request.headers.get('content-type');
    const isMultipart = contentType?.includes('multipart/form-data');
    const contentLength = request.headers.get('content-length');

    // Get request body if present
    // CRITICAL: For multipart/form-data, STREAM the body (don't buffer into memory).
    // Buffering via arrayBuffer() causes OOM for large videos (67+ MB) because the entire
    // file is loaded into Node.js heap. Instead, stream the ReadableStream body directly
    // and forward Content-Length from the browser's original request header.
    let body: BodyInit | undefined;
    if (method !== 'GET' && method !== 'DELETE') {
      if (isMultipart) {
        // Stream multipart body directly — avoids OOM for large uploads (up to 500 MB)
        // Content-Length is forwarded from the browser's request header (set below)
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

    // Forward conditional-request headers. Without this, EVERY backend
    // mutation that uses optimistic concurrency (If-Match) silently fails
    // with "If-Match header is required" because the proxy was an explicit
    // whitelist that didn't include the conditional family. Bug surfaced in
    // Slice 8 S8.8 canvas-editor Save through the UI on staging — discovered
    // 2026-04-28 via "Save failed: If-Match header is required" toast on a
    // PUT /batch that the API expected to be conditional. Direct backend
    // curls worked because they bypassed this proxy. The four headers below
    // are the standard HTTP conditional-request set; forwarding all of them
    // future-proofs the proxy for ETag-based caching too.
    const conditionalHeaders: ReadonlyArray<string> = [
      'If-Match',
      'If-None-Match',
      'If-Modified-Since',
      'If-Unmodified-Since',
    ];
    for (const name of conditionalHeaders) {
      const value = request.headers.get(name);
      if (value !== null && value !== '') {
        headers[name] = value;
      }
    }

    // Forward Content-Length for multipart uploads (critical for large video uploads)
    // When body is ArrayBuffer, fetch() doesn't auto-set Content-Length from the browser's original header
    // Backend needs Content-Length to properly parse multipart form data for large files
    if (isMultipart && contentLength) {
      headers['Content-Length'] = contentLength;
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
      bodySize: typeof body === 'string' ? body.length : (contentLength || 'streaming'),
      contentLength,
      isAuthRefresh,
      hasRefreshToken,
    });

    // Build fetch options
    const fetchOptions: RequestInit = {
      method,
      headers,
      body,
      credentials: 'include', // Important: include cookies
      // CRITICAL: Large file uploads (videos up to 500MB) need longer timeout
      // Backend has 5-minute timeout for video uploads, proxy needs at least as long
      // Add buffer for network overhead: 10 minutes total
      signal: AbortSignal.timeout(600000), // 10 minutes
    };

    // REQUIRED for streaming request bodies (ReadableStream) in Node.js fetch
    if (isMultipart && body) {
      // @ts-ignore - duplex is required for streaming but not in TS types yet
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

    // Phase 6A.24 FIX: Check if response is binary (PDF, images, ZIP, Excel, etc.)
    // Binary responses must be streamed as-is, not parsed as text/JSON
    // Phase 6A.69: Added application/zip for sign-up list exports, and Excel formats for attendee exports
    const responseContentType = response.headers.get('content-type') || '';
    const isBinaryResponse = responseContentType.includes('application/pdf') ||
                            responseContentType.includes('application/octet-stream') ||
                            responseContentType.includes('application/zip') ||
                            responseContentType.includes('application/vnd.openxmlformats') || // Excel (.xlsx)
                            responseContentType.includes('application/vnd.ms-excel') || // Excel (.xls)
                            responseContentType.includes('text/csv') || // CSV files
                            responseContentType.includes('image/') ||
                            responseContentType.includes('video/') ||
                            responseContentType.includes('audio/');

    if (isBinaryResponse) {
      console.log('[Proxy] Binary response detected:', {
        path,
        contentType: responseContentType,
        status: response.status,
      });

      // Stream binary response as-is with proper headers
      const binaryData = await response.arrayBuffer();
      const nextResponse = new NextResponse(binaryData, {
        status: response.status,
        headers: {
          'Content-Type': responseContentType,
          'Content-Disposition': response.headers.get('content-disposition') || '',
        },
      });

      // Forward Set-Cookie headers from backend for binary responses too
      const setCookieHeaders = response.headers.getSetCookie?.() || [];
      setCookieHeaders.forEach(cookie => {
        const cookieParts = cookie.split(';').map(p => p.trim());
        const [nameValue, ...attributes] = cookieParts;
        const newAttributes = attributes
          .filter(attr => !attr.toLowerCase().startsWith('secure'))
          .filter(attr => !attr.toLowerCase().startsWith('samesite=none'));
        newAttributes.push('SameSite=Lax');
        newAttributes.push('Path=/');
        const newCookie = [nameValue, ...newAttributes].join('; ');
        nextResponse.headers.append('Set-Cookie', newCookie);
      });

      return nextResponse;
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
