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
    const targetUrl = `${BACKEND_URL}/${path}`;

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

    console.log(`[Proxy] ${method} ${targetUrl}`, {
      contentType,
      isMultipart,
      hasBody: !!body,
    });

    // Make request to backend
    const response = await fetch(targetUrl, {
      method,
      headers,
      body,
      credentials: 'include', // Important: include cookies
      // @ts-ignore - duplex is required for streaming request bodies but not in TS types yet
      duplex: 'half', // Required for streaming multipart/form-data
    });

    // Get response body
    const responseText = await response.text();
    let responseBody: any;
    try {
      responseBody = JSON.parse(responseText);
    } catch {
      responseBody = responseText;
    }

    // Create Next.js response
    const nextResponse = new NextResponse(JSON.stringify(responseBody), {
      status: response.status,
      headers: {
        'Content-Type': 'application/json',
      },
    });

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
