'use client';

import { useEditor, EditorContent } from '@tiptap/react';
import StarterKit from '@tiptap/starter-kit';
// Phase 6A.147 — `ResizableImage` extends the base @tiptap/extension-image with
// a persisted `width` attribute and a React NodeView that draws a corner drag
// handle. Drop-in replacement; same `.configure({...})` API.
import { ResizableImage } from './editor/ResizableImage';
import Link from '@tiptap/extension-link';
import Placeholder from '@tiptap/extension-placeholder';
import CharacterCount from '@tiptap/extension-character-count';
import Underline from '@tiptap/extension-underline';
import TextAlign from '@tiptap/extension-text-align';
import { TextStyle } from '@tiptap/extension-text-style';
import Color from '@tiptap/extension-color';
import Highlight from '@tiptap/extension-highlight';
import { Table } from '@tiptap/extension-table';
import TableRow from '@tiptap/extension-table-row';
import TableHeader from '@tiptap/extension-table-header';
import TableCell from '@tiptap/extension-table-cell';
import {
  Bold,
  Italic,
  Underline as UnderlineIcon,
  Strikethrough,
  List,
  ListOrdered,
  Heading1,
  Heading2,
  Heading3,
  Link as LinkIcon,
  ImageIcon,
  Undo,
  Redo,
  AlignLeft,
  AlignCenter,
  AlignRight,
  AlignJustify,
  Table as TableIcon,
  Rows as RowsIcon,
  Columns as ColumnsIcon,
  Trash2,
  Palette,
  Highlighter,
} from 'lucide-react';
import { useCallback, useEffect, useRef, useMemo, useState } from 'react';
import { useDebouncedCallback } from 'use-debounce';

/**
 * Rich Text Editor Component using TipTap
 *
 * Email-body-like WYSIWYG with formatting, alignment, colors, tables, and image upload.
 *
 * @example
 * ```tsx
 * <RichTextEditor
 *   content={htmlContent}
 *   onChange={(html) => setValue('description', html)}
 *   placeholder="Write your event description here..."
 *   onImageUpload={uploadImage}
 * />
 * ```
 */

export interface RichTextEditorProps {
  /** HTML content to display */
  content: string;
  /** Callback when content changes (returns HTML) */
  onChange: (html: string) => void;
  /** Placeholder text when empty */
  placeholder?: string;
  /** Whether the field has validation error */
  error?: boolean;
  /** Error message to display */
  errorMessage?: string;
  /** Read-only mode */
  readonly?: boolean;
  /** Maximum character count */
  maxLength?: number;
  /** Minimum height in pixels */
  minHeight?: number;
  /** Callback for Azure Blob Storage image upload (used for toolbar button, paste, and drop) */
  onImageUpload?: (file: File) => Promise<string>;
}

const MAX_IMAGE_BYTES = 10 * 1024 * 1024; // 10 MB — matches backend limit

export function RichTextEditor({
  content,
  onChange,
  placeholder = 'Start writing...',
  error = false,
  errorMessage,
  readonly = false,
  maxLength = 50000,
  minHeight = 300,
  onImageUpload,
}: RichTextEditorProps) {
  const debouncedOnChange = useDebouncedCallback((html: string) => {
    onChange(html);
  }, 300);

  const [isUploadingImage, setIsUploadingImage] = useState(false);

  // Stable ref so editor extensions can call the latest uploader
  const onImageUploadRef = useRef(onImageUpload);
  useEffect(() => {
    onImageUploadRef.current = onImageUpload;
  }, [onImageUpload]);

  // Insert an uploaded image at the current selection
  const uploadAndInsertImage = useCallback(
    async (file: File, editorInstance: ReturnType<typeof useEditor>) => {
      const uploader = onImageUploadRef.current;
      if (!uploader || !editorInstance) return false;

      if (!file.type.startsWith('image/')) return false;
      if (file.size > MAX_IMAGE_BYTES) {
        alert('Image size must be less than 10MB');
        return true; // handled — block default paste/drop
      }

      try {
        setIsUploadingImage(true);
        const azureUrl = await uploader(file);
        editorInstance.chain().focus().setImage({ src: azureUrl }).run();
      } catch (err) {
        console.error('[RichTextEditor] Image upload failed:', err);
        alert(err instanceof Error ? err.message : 'Image upload failed. Please try again.');
      } finally {
        setIsUploadingImage(false);
      }
      return true;
    },
    [],
  );

  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: { levels: [1, 2, 3] },
      }),
      Underline,
      TextStyle,
      Color,
      Highlight.configure({ multicolor: true }),
      TextAlign.configure({
        types: ['heading', 'paragraph'],
        alignments: ['left', 'center', 'right', 'justify'],
      }),
      Table.configure({
        resizable: true,
        HTMLAttributes: { class: 'rte-table' },
      }),
      TableRow,
      TableHeader,
      TableCell,
      ResizableImage.configure({
        inline: true,
        allowBase64: false,
      }),
      Link.configure({
        openOnClick: false,
        HTMLAttributes: {
          class: 'text-orange-600 underline hover:text-orange-700',
        },
      }),
      Placeholder.configure({ placeholder }),
      CharacterCount.configure({
        limit: maxLength,
        mode: 'textSize',
      }),
    ],
    content,
    editable: !readonly,
    editorProps: {
      handlePaste: (view, event) => {
        const items = event.clipboardData?.items;
        if (!items || !onImageUploadRef.current) return false;
        for (const item of Array.from(items)) {
          if (item.type.startsWith('image/')) {
            const file = item.getAsFile();
            if (file) {
              event.preventDefault();
              void uploadAndInsertImage(file, editor);
              return true;
            }
          }
        }
        return false;
      },
      handleDrop: (view, event) => {
        const files = event.dataTransfer?.files;
        if (!files || files.length === 0 || !onImageUploadRef.current) return false;
        const imageFile = Array.from(files).find((f) => f.type.startsWith('image/'));
        if (!imageFile) return false;
        event.preventDefault();
        void uploadAndInsertImage(imageFile, editor);
        return true;
      },
    },
    onUpdate: ({ editor }) => {
      const html = editor.getHTML();
      lastContentRef.current = html;
      debouncedOnChange(html);
    },
  });

  const lastContentRef = useRef<string>(content || '');

  useEffect(() => {
    if (!editor) return;
    if (!content || content === '<p></p>') return;
    if (content === lastContentRef.current) return;
    editor.commands.setContent(content, { emitUpdate: false });
    lastContentRef.current = content;
  }, [editor, content]);

  const characterCount = editor?.storage.characterCount?.characters() || 0;

  const htmlSize = useMemo(() => {
    if (!editor) return '0.0';
    const html = editor.getHTML();
    return (new Blob([html]).size / 1024).toFixed(1);
  }, [editor?.getHTML()]);

  const addImage = useCallback(() => {
    if (!onImageUpload) {
      alert('Image upload is not configured for this editor');
      return;
    }

    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/jpeg,image/png,image/gif,image/webp';
    input.onchange = async (e) => {
      const file = (e.target as HTMLInputElement).files?.[0];
      if (!file || !editor) return;
      await uploadAndInsertImage(file, editor);
    };
    input.click();
  }, [editor, onImageUpload, uploadAndInsertImage]);

  const setLink = useCallback(() => {
    const previousUrl = editor?.getAttributes('link').href;
    const url = window.prompt('Enter URL:', previousUrl);
    if (url === null) return;
    if (url === '') {
      editor?.chain().focus().extendMarkRange('link').unsetLink().run();
      return;
    }
    editor?.chain().focus().extendMarkRange('link').setLink({ href: url }).run();
  }, [editor]);

  if (!editor) {
    return (
      <div className="w-full px-4 py-2 border border-neutral-300 rounded-lg bg-neutral-50" style={{ minHeight }}>
        <p className="text-neutral-400">Loading editor...</p>
      </div>
    );
  }

  const isInTable = editor.isActive('table');

  // Shared button class — keeps toolbar compact across many groups
  const btn = (active: boolean, disabled?: boolean) =>
    `p-2 rounded hover:bg-neutral-200 transition-colors ${active ? 'bg-neutral-300' : ''} ${
      disabled ? 'opacity-30 cursor-not-allowed' : ''
    }`;

  return (
    <div className="w-full">
      {/* Toolbar */}
      {!readonly && (
        <div className="border border-neutral-300 border-b-0 rounded-t-lg bg-neutral-50 p-2 flex flex-wrap gap-1 items-center">
          {/* Formatting */}
          <button
            type="button"
            onClick={() => editor.chain().focus().toggleBold().run()}
            className={btn(editor.isActive('bold'))}
            title="Bold (Ctrl+B)"
          >
            <Bold className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={() => editor.chain().focus().toggleItalic().run()}
            className={btn(editor.isActive('italic'))}
            title="Italic (Ctrl+I)"
          >
            <Italic className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={() => editor.chain().focus().toggleUnderline().run()}
            className={btn(editor.isActive('underline'))}
            title="Underline (Ctrl+U)"
          >
            <UnderlineIcon className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={() => editor.chain().focus().toggleStrike().run()}
            className={btn(editor.isActive('strike'))}
            title="Strikethrough"
          >
            <Strikethrough className="h-4 w-4" />
          </button>

          <div className="w-px h-6 bg-neutral-300 mx-1" />

          {/* Headings */}
          <button
            type="button"
            onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()}
            className={btn(editor.isActive('heading', { level: 1 }))}
            title="Heading 1"
          >
            <Heading1 className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
            className={btn(editor.isActive('heading', { level: 2 }))}
            title="Heading 2"
          >
            <Heading2 className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()}
            className={btn(editor.isActive('heading', { level: 3 }))}
            title="Heading 3"
          >
            <Heading3 className="h-4 w-4" />
          </button>

          <div className="w-px h-6 bg-neutral-300 mx-1" />

          {/* Colors — native color picker keeps it small and email-like */}
          <label
            className={btn(false)}
            title="Text color"
            style={{ display: 'inline-flex', alignItems: 'center', cursor: 'pointer' }}
          >
            <Palette className="h-4 w-4" />
            <input
              type="color"
              onChange={(e) => editor.chain().focus().setColor(e.target.value).run()}
              value={(editor.getAttributes('textStyle').color as string) || '#000000'}
              style={{
                width: 0,
                height: 0,
                opacity: 0,
                position: 'absolute',
                pointerEvents: 'none',
              }}
            />
          </label>
          <label
            className={btn(editor.isActive('highlight'))}
            title="Highlight color"
            style={{ display: 'inline-flex', alignItems: 'center', cursor: 'pointer' }}
          >
            <Highlighter className="h-4 w-4" />
            <input
              type="color"
              onChange={(e) => editor.chain().focus().toggleHighlight({ color: e.target.value }).run()}
              value={(editor.getAttributes('highlight').color as string) || '#FFFF00'}
              style={{
                width: 0,
                height: 0,
                opacity: 0,
                position: 'absolute',
                pointerEvents: 'none',
              }}
            />
          </label>

          <div className="w-px h-6 bg-neutral-300 mx-1" />

          {/* Alignment */}
          <button
            type="button"
            onClick={() => editor.chain().focus().setTextAlign('left').run()}
            className={btn(editor.isActive({ textAlign: 'left' }))}
            title="Align left"
          >
            <AlignLeft className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={() => editor.chain().focus().setTextAlign('center').run()}
            className={btn(editor.isActive({ textAlign: 'center' }))}
            title="Align center"
          >
            <AlignCenter className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={() => editor.chain().focus().setTextAlign('right').run()}
            className={btn(editor.isActive({ textAlign: 'right' }))}
            title="Align right"
          >
            <AlignRight className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={() => editor.chain().focus().setTextAlign('justify').run()}
            className={btn(editor.isActive({ textAlign: 'justify' }))}
            title="Justify"
          >
            <AlignJustify className="h-4 w-4" />
          </button>

          <div className="w-px h-6 bg-neutral-300 mx-1" />

          {/* Lists */}
          <button
            type="button"
            onClick={() => editor.chain().focus().toggleBulletList().run()}
            className={btn(editor.isActive('bulletList'))}
            title="Bullet list"
          >
            <List className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={() => editor.chain().focus().toggleOrderedList().run()}
            className={btn(editor.isActive('orderedList'))}
            title="Numbered list"
          >
            <ListOrdered className="h-4 w-4" />
          </button>

          <div className="w-px h-6 bg-neutral-300 mx-1" />

          {/* Insert: link, image, table */}
          <button
            type="button"
            onClick={setLink}
            className={btn(editor.isActive('link'))}
            title="Insert link"
          >
            <LinkIcon className="h-4 w-4" />
          </button>
          {onImageUpload && (
            <button
              type="button"
              onClick={addImage}
              disabled={isUploadingImage}
              className={btn(false, isUploadingImage)}
              title={isUploadingImage ? 'Uploading image...' : 'Insert image (or paste / drop)'}
            >
              <ImageIcon className="h-4 w-4" />
            </button>
          )}
          <button
            type="button"
            onClick={() =>
              editor.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run()
            }
            className={btn(false)}
            title="Insert 3×3 table"
          >
            <TableIcon className="h-4 w-4" />
          </button>

          {/* Table contextual controls — only when cursor is inside a table */}
          {isInTable && (
            <>
              <button
                type="button"
                onClick={() => editor.chain().focus().addRowAfter().run()}
                className={btn(false)}
                title="Add row below"
              >
                <RowsIcon className="h-4 w-4" />
              </button>
              <button
                type="button"
                onClick={() => editor.chain().focus().addColumnAfter().run()}
                className={btn(false)}
                title="Add column after"
              >
                <ColumnsIcon className="h-4 w-4" />
              </button>
              <button
                type="button"
                onClick={() => editor.chain().focus().deleteTable().run()}
                className={btn(false)}
                title="Delete table"
              >
                <Trash2 className="h-4 w-4" />
              </button>
            </>
          )}

          <div className="w-px h-6 bg-neutral-300 mx-1" />

          {/* History */}
          <button
            type="button"
            onClick={() => editor.chain().focus().undo().run()}
            disabled={!editor.can().chain().focus().undo().run()}
            className={btn(false, !editor.can().chain().focus().undo().run())}
            title="Undo (Ctrl+Z)"
          >
            <Undo className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={() => editor.chain().focus().redo().run()}
            disabled={!editor.can().chain().focus().redo().run()}
            className={btn(false, !editor.can().chain().focus().redo().run())}
            title="Redo (Ctrl+Shift+Z)"
          >
            <Redo className="h-4 w-4" />
          </button>
        </div>
      )}

      {/* Editor Content */}
      <EditorContent
        editor={editor}
        className={`prose prose-sm max-w-none w-full px-4 py-3 border rounded-b-lg focus-within:ring-2 focus-within:ring-offset-2 ${
          readonly ? 'bg-neutral-50 cursor-not-allowed' : 'bg-white'
        } ${
          error
            ? 'border-red-500 focus-within:ring-red-500'
            : 'border-neutral-300 focus-within:ring-orange-500'
        }`}
        style={{ minHeight: readonly ? 'auto' : minHeight }}
      />

      {/* Footer: Character Count & Error */}
      <div className="flex items-center justify-between mt-2">
        <div>
          {error && errorMessage && (
            <p className="text-sm text-red-600">{errorMessage}</p>
          )}
        </div>
        {!readonly && (
          <div className="text-xs text-neutral-500 space-y-1">
            <p className={characterCount > maxLength ? 'text-red-600 font-medium' : ''}>
              Text: {characterCount.toLocaleString()} / {maxLength.toLocaleString()} characters
            </p>
            <p className={parseFloat(htmlSize) > 5120 ? 'text-red-600 font-medium' : ''}>
              Size: {htmlSize} KB / 5,000 KB
            </p>
          </div>
        )}
      </div>

      {!readonly && !onImageUpload && (
        <p className="text-xs text-neutral-500 mt-1">
          Note: Image upload not available for this editor.
        </p>
      )}
      {!readonly && isUploadingImage && (
        <p className="text-xs text-orange-600 mt-1">
          Uploading image to Azure...
        </p>
      )}

      {/* Global TipTap Editor Styles */}
      <style jsx global>{`
        .ProseMirror {
          outline: none;
        }

        .ProseMirror p.is-editor-empty:first-child::before {
          content: attr(data-placeholder);
          float: left;
          color: #9CA3AF;
          pointer-events: none;
          height: 0;
        }

        .ProseMirror h1 {
          font-size: 2em;
          font-weight: 700;
          margin-top: 0.67em;
          margin-bottom: 0.67em;
          color: #8B1538;
        }

        .ProseMirror h2 {
          font-size: 1.5em;
          font-weight: 600;
          margin-top: 0.83em;
          margin-bottom: 0.83em;
          color: #8B1538;
        }

        .ProseMirror h3 {
          font-size: 1.17em;
          font-weight: 600;
          margin-top: 1em;
          margin-bottom: 1em;
          color: #8B1538;
        }

        .ProseMirror ul,
        .ProseMirror ol {
          padding-left: 1.5em;
          margin: 1em 0;
        }

        .ProseMirror img {
          max-width: 100%;
          height: auto;
          border-radius: 8px;
          margin: 1em 0;
        }

        /* Phase 6A.147 — resizable image NodeView wrapper + corner handle.
           The wrapper is inline so the image flows with surrounding text;
           the handle is only visible when the image node is selected
           (ProseMirror adds .is-selected via the NodeView). */
        .ProseMirror .resizable-image-wrapper {
          display: inline-block;
          position: relative;
          line-height: 0;
          max-width: 100%;
        }

        .ProseMirror .resizable-image-wrapper.is-selected img {
          outline: 2px solid #FF7900;
          outline-offset: 2px;
        }

        .ProseMirror .resizable-image-wrapper .resize-handle {
          position: absolute;
          width: 12px;
          height: 12px;
          background-color: #FF7900;
          border: 2px solid #ffffff;
          border-radius: 50%;
          box-shadow: 0 0 0 1px rgba(0, 0, 0, 0.15);
          cursor: nwse-resize;
          touch-action: none;
          z-index: 5;
        }

        .ProseMirror .resizable-image-wrapper .resize-handle-se {
          right: -6px;
          bottom: -6px;
        }

        .ProseMirror .resizable-image-wrapper .resize-handle:focus-visible {
          outline: 2px solid #2563EB;
          outline-offset: 2px;
        }

        .ProseMirror a {
          color: #FF7900;
          text-decoration: underline;
        }

        .ProseMirror a:hover {
          color: #E66D00;
        }

        /* Tables — email-body style */
        .ProseMirror table {
          border-collapse: collapse;
          margin: 1em 0;
          overflow: hidden;
          table-layout: fixed;
          width: 100%;
        }

        .ProseMirror table td,
        .ProseMirror table th {
          border: 1px solid #D1D5DB;
          box-sizing: border-box;
          min-width: 1em;
          padding: 8px 10px;
          position: relative;
          vertical-align: top;
        }

        .ProseMirror table th {
          background-color: #F3F4F6;
          font-weight: 600;
          text-align: left;
        }

        .ProseMirror table .selectedCell:after {
          background: rgba(255, 121, 0, 0.15);
          content: '';
          left: 0;
          right: 0;
          top: 0;
          bottom: 0;
          pointer-events: none;
          position: absolute;
          z-index: 2;
        }

        .ProseMirror table .column-resize-handle {
          background-color: #FF7900;
          bottom: -2px;
          pointer-events: none;
          position: absolute;
          right: -2px;
          top: 0;
          width: 4px;
        }

        .ProseMirror.resize-cursor {
          cursor: ew-resize;
        }

        /* Text alignment */
        .ProseMirror [style*='text-align: center'] {
          text-align: center;
        }
        .ProseMirror [style*='text-align: right'] {
          text-align: right;
        }
        .ProseMirror [style*='text-align: justify'] {
          text-align: justify;
        }

        /* Highlight mark */
        .ProseMirror mark {
          padding: 0 2px;
          border-radius: 2px;
        }
      `}</style>
    </div>
  );
}
