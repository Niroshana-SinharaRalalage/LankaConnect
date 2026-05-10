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

    it('should preserve img tags with safe attributes (Azure blob images)', () => {
      const input = '<p>Check this:</p><img src="https://lcblob.blob.core.windows.net/images/photo.jpg" alt="Event photo" width="600" height="400">';
      const output = sanitizeHtml(input);

      expect(output).toContain('<img');
      expect(output).toContain('src="https://lcblob.blob.core.windows.net/images/photo.jpg"');
      expect(output).toContain('alt="Event photo"');
      expect(output).toContain('width="600"');
      expect(output).toContain('height="400"');
    });

    it('should strip dangerous attributes from img tags (onerror XSS)', () => {
      const input = '<img src="x" onerror="alert(\'XSS\')">';
      const output = sanitizeHtml(input);

      expect(output).not.toContain('onerror');
      expect(output).not.toContain('alert');
    });

    it('should preserve ordered lists', () => {
      const input = '<ol><li>First</li><li>Second</li><li>Third</li></ol>';
      const output = sanitizeHtml(input);

      expect(output).toContain('<ol>');
      expect(output).toContain('<li>First</li>');
      expect(output).toContain('<li>Second</li>');
      expect(output).toContain('<li>Third</li>');
      expect(output).toContain('</ol>');
    });

    it('should preserve blockquote and code tags', () => {
      const input = '<blockquote>A quote</blockquote><pre><code>const x = 1;</code></pre>';
      const output = sanitizeHtml(input);

      expect(output).toContain('<blockquote>A quote</blockquote>');
      expect(output).toContain('<code>const x = 1;</code>');
    });

    // Email-body editor upgrade: tables, color, alignment, highlight,
    // underline, strikethrough — see RichTextEditor.tsx
    it('should preserve TipTap tables with header row', () => {
      const input =
        '<table><thead><tr><th>Name</th><th>Date</th></tr></thead>' +
        '<tbody><tr><td>Dana</td><td>June 22</td></tr></tbody></table>';
      const output = sanitizeHtml(input);

      expect(output).toContain('<table>');
      expect(output).toContain('<thead>');
      expect(output).toContain('<th>Name</th>');
      expect(output).toContain('<td>Dana</td>');
      expect(output).toContain('</table>');
    });

    it('should preserve table cell colspan and rowspan attributes', () => {
      const input = '<table><tbody><tr><td colspan="2" rowspan="1">Merged</td></tr></tbody></table>';
      const output = sanitizeHtml(input);

      expect(output).toContain('colspan="2"');
      expect(output).toContain('rowspan="1"');
    });

    it('should preserve text color via inline style on <span>', () => {
      const input = '<p>Hello <span style="color: #FF7900">orange</span> world</p>';
      const output = sanitizeHtml(input);

      expect(output).toContain('<span');
      expect(output).toContain('color');
      expect(output).toContain('orange');
    });

    it('should preserve <mark> highlight with background-color style', () => {
      const input = '<p>This is <mark style="background-color: #FFFF00">highlighted</mark></p>';
      const output = sanitizeHtml(input);

      expect(output).toContain('<mark');
      expect(output).toContain('background-color');
      expect(output).toContain('highlighted');
    });

    it('should preserve text-align inline style on paragraph', () => {
      const input = '<p style="text-align: center">Centered text</p>';
      const output = sanitizeHtml(input);

      expect(output).toContain('text-align');
      expect(output).toContain('center');
      expect(output).toContain('Centered text');
    });

    it('should preserve underline (<u>) and strikethrough (<s>) tags', () => {
      const input = '<p><u>under</u> and <s>strike</s></p>';
      const output = sanitizeHtml(input);

      expect(output).toContain('<u>under</u>');
      expect(output).toContain('<s>strike</s>');
    });

    it('should strip javascript: URL inside style attribute (XSS regression)', () => {
      const input =
        '<p style="background: url(javascript:alert(1))">XSS attempt</p>' +
        '<span style="color: expression(alert(1))">Old IE XSS</span>';
      const output = sanitizeHtml(input);

      expect(output.toLowerCase()).not.toContain('javascript:');
      expect(output.toLowerCase()).not.toContain('expression(');
      // Surrounding text is preserved; only the dangerous CSS fragment is stripped
      expect(output).toContain('XSS attempt');
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
