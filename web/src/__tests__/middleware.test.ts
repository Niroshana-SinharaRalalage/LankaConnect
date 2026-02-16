import { describe, it, expect, beforeEach } from 'vitest';
import { NextRequest, NextResponse } from 'next/server';
import { middleware } from '../middleware';

describe('WWW Redirect Middleware', () => {
  describe('when hostname is www.lankaconnect.app', () => {
    it('should redirect to non-www domain with 301 status', () => {
      // Arrange
      const request = new NextRequest('https://www.lankaconnect.app/events');

      // Act
      const response = middleware(request);

      // Assert
      expect(response).toBeInstanceOf(NextResponse);
      expect(response.status).toBe(301);
      expect(response.headers.get('location')).toBe('https://lankaconnect.app/events');
    });

    it('should preserve query parameters in redirect', () => {
      // Arrange
      const request = new NextRequest('https://www.lankaconnect.app/events?category=cultural&metro=colombo');

      // Act
      const response = middleware(request);

      // Assert
      expect(response.status).toBe(301);
      expect(response.headers.get('location')).toBe('https://lankaconnect.app/events?category=cultural&metro=colombo');
    });

    it('should redirect root path correctly', () => {
      // Arrange
      const request = new NextRequest('https://www.lankaconnect.app/');

      // Act
      const response = middleware(request);

      // Assert
      expect(response.status).toBe(301);
      expect(response.headers.get('location')).toBe('https://lankaconnect.app/');
    });

    it('should redirect deep paths correctly', () => {
      // Arrange
      const request = new NextRequest('https://www.lankaconnect.app/events/123/signup');

      // Act
      const response = middleware(request);

      // Assert
      expect(response.status).toBe(301);
      expect(response.headers.get('location')).toBe('https://lankaconnect.app/events/123/signup');
    });
  });

  describe('when hostname is lankaconnect.app (non-www)', () => {
    it('should pass through without redirect', () => {
      // Arrange
      const request = new NextRequest('https://lankaconnect.app/events');

      // Act
      const response = middleware(request);

      // Assert
      // NextResponse.next() returns a response with undefined status for pass-through
      expect(response).toBeInstanceOf(NextResponse);
      expect(response.status).not.toBe(301);
    });
  });

  describe('when hostname is localhost (development)', () => {
    it('should pass through without redirect', () => {
      // Arrange
      const request = new NextRequest('http://localhost:3000/events');

      // Act
      const response = middleware(request);

      // Assert
      expect(response).toBeInstanceOf(NextResponse);
      expect(response.status).not.toBe(301);
    });
  });

  describe('when hostname is staging domain', () => {
    it('should pass through without redirect', () => {
      // Arrange
      const request = new NextRequest('https://lankaconnect-ui-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/events');

      // Act
      const response = middleware(request);

      // Assert
      expect(response).toBeInstanceOf(NextResponse);
      expect(response.status).not.toBe(301);
    });
  });

  describe('edge cases', () => {
    it('should handle URLs with hash fragments', () => {
      // Arrange
      const request = new NextRequest('https://www.lankaconnect.app/events#featured');

      // Act
      const response = middleware(request);

      // Assert
      expect(response.status).toBe(301);
      // Note: Hash fragments are not sent to server, so they won't be in location header
      expect(response.headers.get('location')).toBe('https://lankaconnect.app/events');
    });

    it('should handle URLs with port numbers correctly', () => {
      // Arrange
      const request = new NextRequest('https://www.lankaconnect.app:443/events');

      // Act
      const response = middleware(request);

      // Assert
      expect(response.status).toBe(301);
      expect(response.headers.get('location')).toContain('lankaconnect.app');
    });
  });

  describe('SEO and analytics', () => {
    it('should use 301 Moved Permanently for SEO', () => {
      // Arrange
      const request = new NextRequest('https://www.lankaconnect.app/');

      // Act
      const response = middleware(request);

      // Assert
      // 301 tells search engines this is a permanent redirect
      expect(response.status).toBe(301);
    });
  });
});
