/**
 * JWT Decoder Utility
 * Decodes JWT tokens to extract expiration time and other claims
 *
 * Note: This is NOT for token validation (that's done on the server)
 * This is only for reading token expiration to enable proactive refresh
 */

export interface DecodedJwt {
  exp: number; // Expiration time (Unix timestamp in seconds)
  iat: number; // Issued at (Unix timestamp in seconds)
  sub?: string; // Subject (usually user ID)
  email?: string;
  role?: string;
  [key: string]: any;
}

/**
 * Decode a JWT token (without validation)
 * Returns null if token is invalid or malformed
 */
export function decodeJwt(token: string): DecodedJwt | null {
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
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );

    return JSON.parse(jsonPayload);
  } catch (error) {
    console.error('Failed to decode JWT:', error);
    return null;
  }
}

/**
 * Get token expiration time in milliseconds
 * Returns null if token is invalid
 */
export function getTokenExpiration(token: string): number | null {
  const decoded = decodeJwt(token);

  if (!decoded || !decoded.exp) {
    return null;
  }

  // Convert from seconds to milliseconds
  return decoded.exp * 1000;
}

/**
 * Check if token is expired
 */
export function isTokenExpired(token: string): boolean {
  const expiration = getTokenExpiration(token);

  if (!expiration) {
    return true; // Treat invalid tokens as expired
  }

  return Date.now() >= expiration;
}

/**
 * Get time remaining until token expires (in milliseconds)
 * Returns 0 if token is already expired
 * Returns null if token is invalid
 */
export function getTimeUntilExpiration(token: string): number | null {
  const expiration = getTokenExpiration(token);

  if (!expiration) {
    return null;
  }

  const timeRemaining = expiration - Date.now();
  return Math.max(0, timeRemaining);
}

/**
 * Calculate when to refresh token (5 minutes before expiration)
 * Returns timestamp in milliseconds, or null if token is invalid
 */
export function getRefreshTime(token: string): number | null {
  const expiration = getTokenExpiration(token);

  if (!expiration) {
    return null;
  }

  // Refresh 5 minutes (300,000 ms) before expiration
  const refreshBuffer = 5 * 60 * 1000;
  return expiration - refreshBuffer;
}
