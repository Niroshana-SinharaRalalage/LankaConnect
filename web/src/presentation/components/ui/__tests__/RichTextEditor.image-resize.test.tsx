/**
 * Phase 6A.147 — Image resize in the RichTextEditor.
 *
 * The base @tiptap/extension-image package only stores src/alt/title, so a
 * resized image cannot survive a round-trip through the editor. This test
 * suite is the RED step for the custom ResizableImage extension that adds a
 * persisted `width` attribute (integer pixels) and a React NodeView for the
 * drag affordance. Tests target the headless editor (schema + getHTML) rather
 * than the DOM drag interaction — drag handles are smoke-tested in dev mode.
 *
 * If you change persistence (e.g. switch from `width="320"` to inline style),
 * update these tests AND the sanitizer allowlist together.
 */
import { describe, it, expect } from 'vitest';
import { Editor } from '@tiptap/core';
import StarterKit from '@tiptap/starter-kit';

import { ResizableImage } from '../editor/ResizableImage';

function makeEditor(initialHtml = '') {
  return new Editor({
    extensions: [StarterKit, ResizableImage.configure({ inline: true, allowBase64: false })],
    content: initialHtml,
  });
}

describe('ResizableImage extension (Phase 6A.147)', () => {
  describe('schema: width attribute', () => {
    it('parses and emits an integer width attribute', () => {
      const editor = makeEditor('<p><img src="https://cdn.test/a.png" width="320" /></p>');
      const html = editor.getHTML();
      expect(html).toContain('src="https://cdn.test/a.png"');
      expect(html).toMatch(/width="320"/);
      editor.destroy();
    });

    it('renders without width when none is set (legacy images)', () => {
      const editor = makeEditor('<p><img src="https://cdn.test/legacy.png" /></p>');
      const html = editor.getHTML();
      expect(html).toContain('src="https://cdn.test/legacy.png"');
      expect(html).not.toMatch(/width=/);
      editor.destroy();
    });

    it('updates width via updateAttributes and reflects it in getHTML', () => {
      const editor = makeEditor('<p><img src="https://cdn.test/a.png" width="400" /></p>');

      // Select the image node and update its width attribute.
      const { state } = editor;
      let imagePos = -1;
      state.doc.descendants((node, pos) => {
        if (node.type.name === 'image') {
          imagePos = pos;
          return false;
        }
        return true;
      });
      expect(imagePos).toBeGreaterThanOrEqual(0);

      editor.chain().setNodeSelection(imagePos).updateAttributes('image', { width: 200 }).run();

      expect(editor.getHTML()).toMatch(/width="200"/);
      editor.destroy();
    });

    it('clamps non-numeric width input to null (defensive parsing)', () => {
      const editor = makeEditor('<p><img src="https://cdn.test/a.png" width="not-a-number" /></p>');
      const html = editor.getHTML();
      // Garbage in → attribute dropped, image still renders.
      expect(html).toContain('src="https://cdn.test/a.png"');
      expect(html).not.toMatch(/width="not-a-number"/);
      editor.destroy();
    });
  });

  describe('history: undo/redo a resize', () => {
    it('restores the previous width on undo', () => {
      const editor = makeEditor('<p><img src="https://cdn.test/a.png" width="400" /></p>');

      const { state } = editor;
      let imagePos = -1;
      state.doc.descendants((node, pos) => {
        if (node.type.name === 'image') {
          imagePos = pos;
          return false;
        }
        return true;
      });

      editor.chain().setNodeSelection(imagePos).updateAttributes('image', { width: 150 }).run();
      expect(editor.getHTML()).toMatch(/width="150"/);

      editor.commands.undo();
      expect(editor.getHTML()).toMatch(/width="400"/);

      editor.destroy();
    });
  });
});
