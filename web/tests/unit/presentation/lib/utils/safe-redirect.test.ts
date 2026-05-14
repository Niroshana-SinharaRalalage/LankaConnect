import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { resolveSafeRedirect } from '@/presentation/lib/utils/safe-redirect';

/**
 * Phase 6A.144 — resolveSafeRedirect helper
 *
 * Centralized same-origin guard for any `?redirect=` query param. Both
 * LoginForm and RegisterForm route post-auth navigation through this helper
 * so an open-redirect bug fixed here is fixed everywhere.
 *
 * Contract:
 *   - null / empty / whitespace → fallback
 *   - same-origin URL (relative or absolute) → return pathname + search
 *   - cross-origin / scheme-relative / backslash bypass → fallback
 *   - encoded slashes that decode to // → fallback
 *
 * Architect-required from the 6A.144 RCA (correction A5/A6): MUST use
 * `new URL(value, origin)` and assert `parsed.origin === window.location.origin`.
 */

const ORIGIN = 'https://lankaconnect.example';

describe('resolveSafeRedirect — Phase 6A.144', () => {
  beforeEach(() => {
    // jsdom defaults to http://localhost/. Pin a stable origin for the suite.
    Object.defineProperty(window, 'location', {
      writable: true,
      value: new URL(ORIGIN),
    });
  });

  afterEach(() => {
    Object.defineProperty(window, 'location', {
      writable: true,
      value: new URL('http://localhost/'),
    });
  });

  describe('null / empty inputs', () => {
    it('returns the fallback when the value is null', () => {
      expect(resolveSafeRedirect(null, '/')).toBe('/');
    });

    it('returns the fallback when the value is an empty string', () => {
      expect(resolveSafeRedirect('', '/home')).toBe('/home');
    });

    it('returns the fallback when the value is only whitespace', () => {
      expect(resolveSafeRedirect('   ', '/')).toBe('/');
    });
  });

  describe('same-origin paths (happy paths)', () => {
    it('returns a simple relative path unchanged', () => {
      expect(resolveSafeRedirect('/events/abc', '/')).toBe('/events/abc');
    });

    it('preserves the query string', () => {
      expect(resolveSafeRedirect('/events/abc?intent=register', '/')).toBe(
        '/events/abc?intent=register',
      );
    });

    it('strips the origin from a fully-qualified same-origin URL', () => {
      expect(resolveSafeRedirect(`${ORIGIN}/events/abc?intent=register`, '/')).toBe(
        '/events/abc?intent=register',
      );
    });

    it('preserves multiple query params and ordering', () => {
      expect(resolveSafeRedirect('/events/abc?a=1&b=2&c=3', '/')).toBe(
        '/events/abc?a=1&b=2&c=3',
      );
    });
  });

  describe('open-redirect attempts (must reject)', () => {
    it('rejects a protocol-absolute cross-origin URL (https://evil.com)', () => {
      expect(resolveSafeRedirect('https://evil.com/steal', '/')).toBe('/');
    });

    it('rejects http://evil.com when current origin is https://lankaconnect.example', () => {
      expect(resolveSafeRedirect('http://lankaconnect.example/x', '/')).toBe('/');
    });

    it('rejects a scheme-relative URL (//evil.com)', () => {
      expect(resolveSafeRedirect('//evil.com/steal', '/')).toBe('/');
    });

    it('rejects a backslash-bypass attempt (/\\evil.com)', () => {
      expect(resolveSafeRedirect('/\\evil.com/steal', '/')).toBe('/');
    });

    it('rejects encoded slashes that decode to a scheme-relative URL', () => {
      // `%2F%2Fevil.com` decodes to `//evil.com`
      expect(resolveSafeRedirect('/%2F%2Fevil.com/steal', '/')).toBe('/');
    });

    it('rejects a javascript: URL', () => {
      // eslint-disable-next-line no-script-url
      expect(resolveSafeRedirect('javascript:alert(1)', '/')).toBe('/');
    });

    it('rejects a data: URL', () => {
      expect(resolveSafeRedirect('data:text/html,<script>alert(1)</script>', '/')).toBe('/');
    });
  });
});
