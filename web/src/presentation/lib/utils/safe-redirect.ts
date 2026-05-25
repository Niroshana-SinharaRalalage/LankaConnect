/**
 * Phase 6A.144 — resolveSafeRedirect
 *
 * Same-origin guard for `?redirect=` query parameters used by auth forms.
 * Returns the cleaned same-origin path or the supplied fallback for any
 * value that fails the guard (cross-origin, scheme-relative, encoded
 * bypasses, malformed). Always anchor at `window.location.origin` — this
 * helper assumes a browser environment (it is only called from `'use client'`
 * components after user interaction).
 */
export function resolveSafeRedirect(value: string | null | undefined, fallback: string): string {
  if (value == null) return fallback;
  const trimmed = value.trim();
  if (trimmed.length === 0) return fallback;

  // Reject anything that looks like a scheme-relative or backslash bypass
  // before the URL parser silently normalizes it. `new URL('//evil.com', origin)`
  // produces a same-origin object on Node/jsdom but an attacker-controlled one
  // in Chrome — so screen these out explicitly.
  if (trimmed.startsWith('//') || trimmed.startsWith('/\\') || trimmed.startsWith('\\')) {
    return fallback;
  }

  // Reject after URL-decoding so encoded slashes don't sneak past the check above.
  let decoded = trimmed;
  try {
    decoded = decodeURIComponent(trimmed);
  } catch {
    // Malformed percent-encoding — treat as unsafe.
    return fallback;
  }
  if (decoded.startsWith('//') || decoded.startsWith('/\\') || decoded.startsWith('\\')) {
    return fallback;
  }

  // Reject dangerous schemes outright (URL constructor would otherwise parse them).
  const lower = decoded.toLowerCase();
  if (lower.startsWith('javascript:') || lower.startsWith('data:') || lower.startsWith('vbscript:')) {
    return fallback;
  }

  let parsed: URL;
  try {
    parsed = new URL(trimmed, window.location.origin);
  } catch {
    return fallback;
  }

  if (parsed.origin !== window.location.origin) {
    return fallback;
  }

  return `${parsed.pathname}${parsed.search}${parsed.hash}`;
}
