'use client';

import { useEditor, EditorContent } from '@tiptap/react';
import StarterKit from '@tiptap/starter-kit';
import Image from '@tiptap/extension-image';
import Link from '@tiptap/extension-link';
import Placeholder from '@tiptap/extension-placeholder';
import CharacterCount from '@tiptap/extension-character-count';
import {
  Bold,
  Italic,
  List,
  ListOrdered,
  Heading1,
  Heading2,
  Heading3,
  Link as LinkIcon,
  ImageIcon,
  Undo,
  Redo,
} from 'lucide-react';
import { useCallback, useEffect } from 'react';

/**
 * Rich Text Editor Component using TipTap
 * Phase 6A.74 Part 5A
 *
 * Features:
 * - Bold, Italic formatting
 * - Headings (H1, H2, H3)
 * - Bullet lists, Numbered lists
 * - Links
 * - Image upload (base64 inline)
 * - Undo/Redo
 * - Character count
 * - HTML output
 *
 * @example
 * ```tsx
 * <RichTextEditor
 *   content={htmlContent}
 *   onChange={(html) => setValue('description', html)}
 *   placeholder="Write your newsletter content here..."
 *   error={!!errors.description}
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
}

export function RichTextEditor({
  content,
  onChange,
  placeholder = 'Start writing...',
  error = false,
  errorMessage,
  readonly = false,
  maxLength = 50000,
  minHeight = 300,
}: RichTextEditorProps) {
  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: {
          levels: [1, 2, 3],
        },
      }),
      Image.configure({
        inline: true,
        allowBase64: true,
      }),
      Link.configure({
        openOnClick: false,
        HTMLAttributes: {
          class: 'text-orange-600 underline hover:text-orange-700',
        },
      }),
      Placeholder.configure({
        placeholder,
      }),
      CharacterCount.configure({
        limit: maxLength,
        mode: 'textSize', // Count text only, not HTML markup or base64 images
      }),
    ],
    content,
    editable: !readonly,
    onUpdate: ({ editor }) => {
      const html = editor.getHTML();
      onChange(html);
    },
  });

  // Update editor content when prop changes (for form reset)
  useEffect(() => {
    if (editor && content !== editor.getHTML()) {
      editor.commands.setContent(content);
    }
  }, [content, editor]);

  // Get character count
  const characterCount = editor?.storage.characterCount?.characters() || 0;

  // Handle image upload
  const addImage = useCallback(() => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/*';
    input.onchange = async (e) => {
      const file = (e.target as HTMLInputElement).files?.[0];
      if (!file) return;

      // Validate file size (max 2MB)
      if (file.size > 2 * 1024 * 1024) {
        alert('Image size must be less than 2MB');
        return;
      }

      // Convert to base64
      const reader = new FileReader();
      reader.onload = (event) => {
        const base64 = event.target?.result as string;
        editor?.chain().focus().setImage({ src: base64 }).run();
      };
      reader.readAsDataURL(file);
    };
    input.click();
  }, [editor]);

  // Handle link insertion
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

  return (
    <div className="w-full">
      {/* Toolbar */}
      {!readonly && (
        <div className="border border-neutral-300 border-b-0 rounded-t-lg bg-neutral-50 p-2 flex flex-wrap gap-1">
          {/* Text Formatting */}
          <button
            type="button"
            onClick={() => editor.chain().focus().toggleBold().run()}
            disabled={!editor.can().chain().focus().toggleBold().run()}
            className={`p-2 rounded hover:bg-neutral-200 transition-colors ${
              editor.isActive('bold') ? 'bg-neutral-300' : ''
            }`}
            title="Bold"
          >
            <Bold className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={() => editor.chain().focus().toggleItalic().run()}
            disabled={!editor.can().chain().focus().toggleItalic().run()}
            className={`p-2 rounded hover:bg-neutral-200 transition-colors ${
              editor.isActive('italic') ? 'bg-neutral-300' : ''
            }`}
            title="Italic"
          >
            <Italic className="h-4 w-4" />
          </button>

          <div className="w-px h-6 bg-neutral-300 mx-1" />

          {/* Headings */}
          <button
            type="button"
            onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()}
            className={`p-2 rounded hover:bg-neutral-200 transition-colors ${
              editor.isActive('heading', { level: 1 }) ? 'bg-neutral-300' : ''
            }`}
            title="Heading 1"
          >
            <Heading1 className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
            className={`p-2 rounded hover:bg-neutral-200 transition-colors ${
              editor.isActive('heading', { level: 2 }) ? 'bg-neutral-300' : ''
            }`}
            title="Heading 2"
          >
            <Heading2 className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()}
            className={`p-2 rounded hover:bg-neutral-200 transition-colors ${
              editor.isActive('heading', { level: 3 }) ? 'bg-neutral-300' : ''
            }`}
            title="Heading 3"
          >
            <Heading3 className="h-4 w-4" />
          </button>

          <div className="w-px h-6 bg-neutral-300 mx-1" />

          {/* Lists */}
          <button
            type="button"
            onClick={() => editor.chain().focus().toggleBulletList().run()}
            className={`p-2 rounded hover:bg-neutral-200 transition-colors ${
              editor.isActive('bulletList') ? 'bg-neutral-300' : ''
            }`}
            title="Bullet List"
          >
            <List className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={() => editor.chain().focus().toggleOrderedList().run()}
            className={`p-2 rounded hover:bg-neutral-200 transition-colors ${
              editor.isActive('orderedList') ? 'bg-neutral-300' : ''
            }`}
            title="Numbered List"
          >
            <ListOrdered className="h-4 w-4" />
          </button>

          <div className="w-px h-6 bg-neutral-300 mx-1" />

          {/* Link & Image */}
          <button
            type="button"
            onClick={setLink}
            className={`p-2 rounded hover:bg-neutral-200 transition-colors ${
              editor.isActive('link') ? 'bg-neutral-300' : ''
            }`}
            title="Insert Link"
          >
            <LinkIcon className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={addImage}
            className="p-2 rounded hover:bg-neutral-200 transition-colors"
            title="Insert Image"
          >
            <ImageIcon className="h-4 w-4" />
          </button>

          <div className="w-px h-6 bg-neutral-300 mx-1" />

          {/* Undo/Redo */}
          <button
            type="button"
            onClick={() => editor.chain().focus().undo().run()}
            disabled={!editor.can().chain().focus().undo().run()}
            className="p-2 rounded hover:bg-neutral-200 transition-colors disabled:opacity-30"
            title="Undo"
          >
            <Undo className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={() => editor.chain().focus().redo().run()}
            disabled={!editor.can().chain().focus().redo().run()}
            className="p-2 rounded hover:bg-neutral-200 transition-colors disabled:opacity-30"
            title="Redo"
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
          <p className={`text-xs ${
            characterCount > maxLength ? 'text-red-600 font-medium' : 'text-neutral-500'
          }`}>
            {characterCount.toLocaleString()} / {maxLength.toLocaleString()} characters
          </p>
        )}
      </div>

      {/* Image upload note */}
      {!readonly && (
        <p className="text-xs text-neutral-500 mt-1">
          Tip: Images are embedded as base64. Keep images under 2MB for optimal email delivery.
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

        .ProseMirror a {
          color: #FF7900;
          text-decoration: underline;
        }

        .ProseMirror a:hover {
          color: #E66D00;
        }
      `}</style>
    </div>
  );
}
