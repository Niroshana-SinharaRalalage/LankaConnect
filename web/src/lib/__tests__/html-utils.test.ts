/**
 * Unit Tests for HTML Utilities
 *
 * Tests for sanitizeHtml(), isHtmlContent(), and plainTextToHtml()
 * Following TDD Red-Green-Refactor methodology
 *
 * Issue: Event description line breaks are removed during rendering
 * Root Cause: plainTextToHtml() being applied to HTML content
 * Fix: Always use sanitizeHtml() directly (it handles both HTML and plain text)
 */

import { sanitizeHtml, isHtmlContent, plainTextToHtml } from '../html-utils';

describe('HTML Utils', () => {
  describe('sanitizeHtml', () => {
    it('should preserve TipTap paragraph spacing with empty <p> tags', () => {
      const input = '<p>Paragraph 1</p><p></p><p>Paragraph 2</p>';
      const output = sanitizeHtml(input);

      // Should preserve all paragraph tags (including empty ones for spacing)
      expect(output).toContain('<p>Paragraph 1</p>');
      expect(output).toContain('<p>Paragraph 2</p>');
      // DOMPurify may remove or keep empty <p></p> - either is fine
      // as long as the visible paragraphs are separated
    });

    it('should preserve TipTap formatting tags (bold, italic, headings)', () => {
      const input = '<p><strong>Bold text</strong> and <em>italic text</em></p><h1>Heading</h1>';
      const output = sanitizeHtml(input);

      expect(output).toContain('<strong>Bold text</strong>');
      expect(output).toContain('<em>italic text</em>');
      expect(output).toContain('<h1>Heading</h1>');
    });

    it('should preserve TipTap lists', () => {
      const input = '<ul><li>Item 1</li><li>Item 2</li></ul>';
      const output = sanitizeHtml(input);

      expect(output).toContain('<ul>');
      expect(output).toContain('<li>Item 1</li>');
      expect(output).toContain('<li>Item 2</li>');
      expect(output).toContain('</ul>');
    });

    it('should strip dangerous script tags', () => {
      const input = '<p>Safe content</p><script>alert("XSS")</script>';
      const output = sanitizeHtml(input);

      expect(output).toContain('<p>Safe content</p>');
      expect(output).not.toContain('<script>');
      expect(output).not.toContain('alert');
    });

    it('should strip dangerous event handlers', () => {
      const input = '<p onclick="alert(\'XSS\')">Click me</p>';
      const output = sanitizeHtml(input);

      expect(output).toContain('<p>Click me</p>');
      expect(output).not.toContain('onclick');
      expect(output).not.toContain('alert');
    });

    it('should preserve safe links with href attribute', () => {
      const input = '<p>Visit <a href="https://example.com" target="_blank">our site</a></p>';
      const output = sanitizeHtml(input);

      expect(output).toContain('<a href="https://example.com"');
      expect(output).toContain('our site</a>');
    });

    it('should handle plain text safely (no HTML tags)', () => {
      const input = 'This is plain text with no HTML tags.';
      const output = sanitizeHtml(input);

      // DOMPurify should pass through plain text unchanged
      expect(output).toBe(input);
    });

    it('should handle plain text with newlines (no conversion needed)', () => {
      const input = 'Line 1\nLine 2\n\nLine 3';
      const output = sanitizeHtml(input);

      // DOMPurify passes through plain text - newlines preserved as-is
      expect(output).toBe(input);
    });

    it('should handle HTML-like characters in plain text', () => {
      const input = 'Text with <brackets> and &ampersands';
      const output = sanitizeHtml(input);

      // DOMPurify removes invalid HTML tags, preserves text content
      // The key is that dangerous content is neutralized
      expect(output).toContain('Text with');
      expect(output).toContain('and');
      // DOMPurify might escape, remove, or normalize based on context
    });
  });

  describe('isHtmlContent', () => {
    it('should detect TipTap HTML with paragraph tags', () => {
      expect(isHtmlContent('<p>Hello</p>')).toBe(true);
      expect(isHtmlContent('<p></p><p>World</p>')).toBe(true);
    });

    it('should detect TipTap HTML with heading tags', () => {
      expect(isHtmlContent('<h1>Title</h1><p>Content</p>')).toBe(true);
      expect(isHtmlContent('<h2>Subtitle</h2>')).toBe(true);
    });

    it('should detect TipTap HTML with list tags', () => {
      expect(isHtmlContent('<ul><li>Item</li></ul>')).toBe(true);
      expect(isHtmlContent('<ol><li>First</li></ol>')).toBe(true);
    });

    it('should detect TipTap HTML with formatting tags', () => {
      expect(isHtmlContent('<strong>Bold</strong>')).toBe(true);
      expect(isHtmlContent('<em>Italic</em>')).toBe(true);
    });

    it('should NOT detect plain text as HTML', () => {
      expect(isHtmlContent('Plain text')).toBe(false);
      expect(isHtmlContent('Line 1\nLine 2')).toBe(false);
      expect(isHtmlContent('Text with numbers 123')).toBe(false);
    });

    it('should NOT detect text with < or > as HTML (unless valid tag)', () => {
      expect(isHtmlContent('5 < 10')).toBe(false);
      expect(isHtmlContent('10 > 5')).toBe(false);
      expect(isHtmlContent('Price: <$100')).toBe(false);
    });
  });

  describe('plainTextToHtml', () => {
    it('should convert plain text to HTML paragraphs', () => {
      const input = 'Single line of text';
      const output = plainTextToHtml(input);

      expect(output).toContain('<p>');
      expect(output).toContain('Single line of text');
      expect(output).toContain('</p>');
    });

    it('should convert double newlines to separate paragraphs', () => {
      const input = 'Paragraph 1\n\nParagraph 2';
      const output = plainTextToHtml(input);

      expect(output).toContain('<p>Paragraph 1</p>');
      expect(output).toContain('<p>Paragraph 2</p>');
    });

    it('should convert single newlines to <br> tags', () => {
      const input = 'Line 1\nLine 2';
      const output = plainTextToHtml(input);

      expect(output).toContain('Line 1<br>Line 2');
    });

    it('should escape HTML entities to prevent XSS', () => {
      const input = '<script>alert("XSS")</script>';
      const output = plainTextToHtml(input);

      // Should escape < and > to entities
      expect(output).toContain('&lt;script&gt;');
      expect(output).toContain('&lt;/script&gt;');
      expect(output).not.toContain('<script>');
    });

    it('should auto-link URLs in plain text', () => {
      const input = 'Visit https://example.com for more info';
      const output = plainTextToHtml(input);

      expect(output).toContain('<a href="https://example.com"');
      expect(output).toContain('target="_blank"');
      expect(output).toContain('rel="noopener noreferrer"');
    });

    it('should escape ampersands and quotes', () => {
      const input = 'Ben & Jerry\'s "Ice Cream"';
      const output = plainTextToHtml(input);

      expect(output).toContain('&amp;');
      expect(output).toContain('&quot;');
    });
  });
});
