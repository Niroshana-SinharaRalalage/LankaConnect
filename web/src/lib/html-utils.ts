import DOMPurify from 'dompurify';

/**
 * Sanitize HTML content for safe rendering with dangerouslySetInnerHTML.
 * Allows formatting tags (p, b, i, a, ul, ol, li, h1-h3, br, strong, em, img)
 * but strips scripts, event handlers, and other dangerous content.
 */
export function sanitizeHtml(html: string): string {
  return DOMPurify.sanitize(html, {
    ALLOWED_TAGS: [
      'p', 'br', 'b', 'i', 'strong', 'em', 'u',
      'h1', 'h2', 'h3',
      'ul', 'ol', 'li',
      'a', 'blockquote', 'code', 'pre',
      'img',
    ],
    ALLOWED_ATTR: ['href', 'target', 'rel', 'class', 'src', 'alt', 'width', 'height'],
    ADD_ATTR: ['target'],
  });
}

/**
 * Detect whether a string contains HTML tags.
 * Used to distinguish between legacy plain-text descriptions
 * and new rich-text HTML descriptions.
 */
export function isHtmlContent(text: string): boolean {
  return /<[a-z][\s\S]*>/i.test(text);
}

/**
 * Convert plain text (with newlines) to simple HTML paragraphs.
 * Used for backward compatibility when rendering legacy plain-text
 * descriptions that were saved before the rich text editor was added.
 *
 * Also auto-links URLs found in plain text.
 */
export function plainTextToHtml(text: string): string {
  // Escape HTML entities first
  const escaped = text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');

  // Auto-link URLs
  const urlRegex = /(https?:\/\/[^\s<]+)/g;
  const linked = escaped.replace(
    urlRegex,
    '<a href="$1" target="_blank" rel="noopener noreferrer" class="text-orange-600 underline hover:text-orange-700">$1</a>'
  );

  // Split by double newlines into paragraphs, single newlines become <br>
  const paragraphs = linked.split(/\n\n+/);
  return paragraphs
    .map((p) => `<p>${p.replace(/\n/g, '<br>')}</p>`)
    .join('');
}
