/**
 * Phase 6A.147 — Resizable image extension for the TipTap RichTextEditor.
 *
 * Extends the base @tiptap/extension-image package to:
 *   1) Add an integer `width` attribute on the Image node schema, so the resized
 *      size persists losslessly in editor.getHTML().
 *   2) Wire a React NodeView (ResizableImageView) that draws a corner drag
 *      handle and updates the `width` attribute on pointerup. Aspect ratio is
 *      preserved via CSS `height: auto`.
 *
 * Persistence shape (chosen over inline style or percentage):
 *   <img src="..." alt="..." width="320">
 *
 * Rationale:
 *   - `width` is already in sanitizeHtml()'s ALLOWED_ATTR (web/src/lib/html-utils.ts),
 *     so no sanitizer change is needed.
 *   - Integer pixels are unambiguous and easy to test (vs. percentages that depend
 *     on the editor's content width at edit time).
 *   - The existing `.ProseMirror img { max-width: 100%; height: auto; }` rule
 *     guarantees the image still fits narrower viewports on the public page —
 *     pixel widths shrink gracefully, they never overflow.
 *
 * Failure handling for malformed inputs:
 *   - Non-numeric `width` attribute → parsed to null, dropped on render. The
 *     image still renders fluidly with no width attribute.
 */
import Image from '@tiptap/extension-image';
import { ReactNodeViewRenderer } from '@tiptap/react';

import { ResizableImageView } from './ResizableImageView';

export const ResizableImage = Image.extend({
  name: 'image',

  addAttributes() {
    return {
      ...this.parent?.(),
      width: {
        default: null,
        parseHTML: (element) => {
          const raw = element.getAttribute('width');
          if (raw === null || raw === '') return null;
          const parsed = parseInt(raw, 10);
          return Number.isNaN(parsed) || parsed <= 0 ? null : parsed;
        },
        renderHTML: (attributes) => {
          if (!attributes.width) return {};
          return { width: String(attributes.width) };
        },
      },
    };
  },

  addNodeView() {
    return ReactNodeViewRenderer(ResizableImageView);
  },
});
