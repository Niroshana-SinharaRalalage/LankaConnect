import DOMPurify from 'dompurify';

/**
 * CSS properties safe to keep inside a `style="..."` attribute.
 *
 * DOMPurify v3 does not parse CSS inside inline style attributes — it passes
 * the raw value through. Without an explicit allowlist, an attacker can inject
 * `style="background:url(javascript:alert(1))"` or `style="color:expression(...)"`
 * (legacy IE) and bypass tag/attribute sanitization. We strip every declaration
 * whose property is not in this list, and reject any value containing a
 * dangerous token (url(...), javascript:, expression(), behavior:, <, >).
 */
const ALLOWED_CSS_PROPS = new Set<string>([
  // text styling — emitted by TipTap Color, Highlight, TextAlign extensions
  'color',
  'background-color',
  'text-align',
  'text-decoration',
  'font-weight',
  'font-style',
  // table layout — emitted by TipTap Table column-resize
  'width',
  'min-width',
  'height',
  'vertical-align',
]);

const DANGEROUS_CSS_TOKEN = /url\s*\(|javascript\s*:|expression\s*\(|behavior\s*:|[<>]/i;

function sanitizeStyleAttribute(rawStyle: string): string {
  return rawStyle
    .split(';')
    .map((decl) => decl.trim())
    .filter((decl) => decl.length > 0)
    .map((decl) => {
      const colonIdx = decl.indexOf(':');
      if (colonIdx === -1) return null;
      const property = decl.slice(0, colonIdx).trim().toLowerCase();
      const value = decl.slice(colonIdx + 1).trim();
      if (!ALLOWED_CSS_PROPS.has(property)) return null;
      if (DANGEROUS_CSS_TOKEN.test(value)) return null;
      return `${property}: ${value}`;
    })
    .filter((decl): decl is string => decl !== null)
    .join('; ');
}

// One-time module-level hook: filter the `style` attribute through the CSS
// allowlist before DOMPurify finalizes the attribute. This module is the sole
// consumer of dompurify in the codebase, so the hook is safe to set globally.
DOMPurify.addHook('uponSanitizeAttribute', (_node, data) => {
  if (data.attrName === 'style' && typeof data.attrValue === 'string') {
    data.attrValue = sanitizeStyleAttribute(data.attrValue);
    if (data.attrValue.length === 0) {
      data.keepAttr = false;
    }
  }
});

/**
 * Sanitize HTML content for safe rendering with dangerouslySetInnerHTML.
 *
 * Allows the formatting tags emitted by the TipTap RichTextEditor — tables,
 * inline styles for color/alignment, highlights (<mark>), strikethrough,
 * and underline — while DOMPurify strips scripts, event handlers, and
 * javascript: URLs, and the `uponSanitizeAttribute` hook above filters
 * inline CSS to the ALLOWED_CSS_PROPS allowlist.
 */
export function sanitizeHtml(html: string): string {
  return DOMPurify.sanitize(html, {
    ALLOWED_TAGS: [
      'p', 'br', 'b', 'i', 'strong', 'em', 'u', 's', 'del', 'mark', 'span',
      'h1', 'h2', 'h3',
      'ul', 'ol', 'li',
      'a', 'blockquote', 'code', 'pre',
      'img',
      'table', 'thead', 'tbody', 'tfoot', 'tr', 'th', 'td', 'colgroup', 'col',
    ],
    ALLOWED_ATTR: [
      'href', 'target', 'rel', 'class', 'src', 'alt', 'width', 'height',
      'style',
      'colspan', 'rowspan', 'align', 'colwidth',
    ],
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
