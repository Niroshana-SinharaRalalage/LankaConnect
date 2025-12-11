import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { PhotoUploadWidget } from '@/presentation/components/widgets/PhotoUploadWidget';

describe('PhotoUploadWidget', () => {
  const mockOnUpload = vi.fn();
  const mockOnDelete = vi.fn();
  const defaultProps = {
    onUpload: mockOnUpload,
    onDelete: mockOnDelete,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('rendering', () => {
    it('should render upload area when no photo is present', () => {
      render(<PhotoUploadWidget {...defaultProps} />);

      expect(screen.getByText(/upload photo/i)).toBeInTheDocument();
      expect(screen.getByText(/drag and drop or click to select/i)).toBeInTheDocument();
    });

    it('should render current photo when photoUrl is provided', () => {
      render(<PhotoUploadWidget {...defaultProps} currentPhotoUrl="https://example.com/photo.jpg" />);

      const image = screen.getByAltText(/profile photo/i);
      expect(image).toBeInTheDocument();
      expect(image).toHaveAttribute('src', expect.stringContaining('photo.jpg'));
    });

    it('should show accepted formats hint', () => {
      render(<PhotoUploadWidget {...defaultProps} />);

      expect(screen.getByText(/JPEG, JPG, PNG, WEBP/)).toBeInTheDocument();
    });

    it('should show max file size hint', () => {
      render(<PhotoUploadWidget {...defaultProps} maxSizeMB={5} />);

      expect(screen.getByText(/max 5mb/i)).toBeInTheDocument();
    });

    it('should render delete button when photo exists', () => {
      render(<PhotoUploadWidget {...defaultProps} currentPhotoUrl="https://example.com/photo.jpg" />);

      expect(screen.getByRole('button', { name: /delete photo/i })).toBeInTheDocument();
    });

    it('should not render delete button when no photo exists', () => {
      render(<PhotoUploadWidget {...defaultProps} />);

      expect(screen.queryByRole('button', { name: /delete photo/i })).not.toBeInTheDocument();
    });
  });

  describe('file selection via click', () => {
    it('should trigger file input when upload area is clicked', () => {
      render(<PhotoUploadWidget {...defaultProps} />);

      const uploadArea = screen.getByText(/drag and drop or click to select/i).closest('div');
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;

      expect(fileInput).toBeInTheDocument();

      const clickSpy = vi.spyOn(fileInput, 'click');
      fireEvent.click(uploadArea!);

      expect(clickSpy).toHaveBeenCalled();
    });

    it('should call onUpload when valid file is selected', async () => {
      render(<PhotoUploadWidget {...defaultProps} />);

      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
      const file = new File(['dummy content'], 'test.jpg', { type: 'image/jpeg' });

      fireEvent.change(fileInput, { target: { files: [file] } });

      await waitFor(() => {
        expect(mockOnUpload).toHaveBeenCalledWith(file);
      });
    });

    it('should accept only image files', () => {
      render(<PhotoUploadWidget {...defaultProps} />);

      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
      expect(fileInput).toHaveAttribute('accept', 'image/jpeg,image/jpg,image/png,image/webp');
    });
  });

  describe('drag and drop', () => {
    it('should highlight upload area on drag over', () => {
      render(<PhotoUploadWidget {...defaultProps} />);

      const uploadArea = screen.getByText(/drag and drop or click to select/i).closest('div');

      fireEvent.dragOver(uploadArea!, { dataTransfer: { files: [] } });

      expect(uploadArea).toHaveClass('border-primary');
    });

    it('should remove highlight on drag leave', () => {
      render(<PhotoUploadWidget {...defaultProps} />);

      const uploadArea = screen.getByText(/drag and drop or click to select/i).closest('div');

      fireEvent.dragOver(uploadArea!, { dataTransfer: { files: [] } });
      fireEvent.dragLeave(uploadArea!);

      expect(uploadArea).not.toHaveClass('border-primary');
    });

    it('should call onUpload when file is dropped', async () => {
      render(<PhotoUploadWidget {...defaultProps} />);

      const uploadArea = screen.getByText(/drag and drop or click to select/i).closest('div');
      const file = new File(['dummy content'], 'test.png', { type: 'image/png' });

      fireEvent.drop(uploadArea!, {
        dataTransfer: {
          files: [file],
        },
      });

      await waitFor(() => {
        expect(mockOnUpload).toHaveBeenCalledWith(file);
      });
    });
  });

  describe('file validation', () => {
    it('should show error for file exceeding max size', async () => {
      render(<PhotoUploadWidget {...defaultProps} maxSizeMB={1} />);

      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
      // Create 2MB file
      const largeFile = new File([new ArrayBuffer(2 * 1024 * 1024)], 'large.jpg', { type: 'image/jpeg' });

      fireEvent.change(fileInput, { target: { files: [largeFile] } });

      await waitFor(() => {
        expect(screen.getByText(/file size exceeds 1mb/i)).toBeInTheDocument();
      });

      expect(mockOnUpload).not.toHaveBeenCalled();
    });

    it('should show error for invalid file type', async () => {
      render(<PhotoUploadWidget {...defaultProps} />);

      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
      const invalidFile = new File(['dummy'], 'test.pdf', { type: 'application/pdf' });

      fireEvent.change(fileInput, { target: { files: [invalidFile] } });

      await waitFor(() => {
        expect(screen.getByText(/Only JPEG, JPG, PNG, WEBP images are allowed/)).toBeInTheDocument();
      });

      expect(mockOnUpload).not.toHaveBeenCalled();
    });

    it('should accept custom accepted formats', async () => {
      render(<PhotoUploadWidget {...defaultProps} acceptedFormats={['image/png']} />);

      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
      const jpgFile = new File(['dummy'], 'test.jpg', { type: 'image/jpeg' });

      fireEvent.change(fileInput, { target: { files: [jpgFile] } });

      await waitFor(() => {
        expect(screen.getByText(/Only PNG images are allowed/)).toBeInTheDocument();
      });

      expect(mockOnUpload).not.toHaveBeenCalled();
    });
  });

  describe('loading state', () => {
    it('should show loading spinner when isLoading is true', () => {
      render(<PhotoUploadWidget {...defaultProps} isLoading={true} />);

      expect(screen.getByRole('status')).toBeInTheDocument();
      expect(screen.getByText(/uploading/i)).toBeInTheDocument();
    });

    it('should disable upload area when loading', () => {
      render(<PhotoUploadWidget {...defaultProps} isLoading={true} />);

      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
      expect(fileInput).toBeDisabled();
    });

    it('should disable delete button when loading', () => {
      render(<PhotoUploadWidget {...defaultProps} currentPhotoUrl="https://example.com/photo.jpg" isLoading={true} />);

      const deleteButton = screen.getByRole('button', { name: /delete photo/i });
      expect(deleteButton).toBeDisabled();
    });
  });

  describe('error handling', () => {
    it('should display error message when error prop is provided', () => {
      render(<PhotoUploadWidget {...defaultProps} error="Failed to upload photo" />);

      expect(screen.getByText(/failed to upload photo/i)).toBeInTheDocument();
    });

    it('should show error styling when error exists', () => {
      render(<PhotoUploadWidget {...defaultProps} error="Upload failed" />);

      const errorMessage = screen.getByText(/upload failed/i);
      expect(errorMessage).toHaveClass('text-destructive');
    });
  });

  describe('delete functionality', () => {
    it('should call onDelete when delete button is clicked', async () => {
      render(<PhotoUploadWidget {...defaultProps} currentPhotoUrl="https://example.com/photo.jpg" />);

      const deleteButton = screen.getByRole('button', { name: /delete photo/i });
      fireEvent.click(deleteButton);

      await waitFor(() => {
        expect(mockOnDelete).toHaveBeenCalled();
      });
    });

    it('should not call onDelete when button is disabled', () => {
      render(<PhotoUploadWidget {...defaultProps} currentPhotoUrl="https://example.com/photo.jpg" isLoading={true} />);

      const deleteButton = screen.getByRole('button', { name: /delete photo/i });
      fireEvent.click(deleteButton);

      expect(mockOnDelete).not.toHaveBeenCalled();
    });
  });

  describe('accessibility', () => {
    it('should have proper ARIA labels', () => {
      render(<PhotoUploadWidget {...defaultProps} />);

      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
      expect(fileInput).toHaveAttribute('aria-label', 'Upload profile photo');
    });

    it('should have proper role for loading spinner', () => {
      render(<PhotoUploadWidget {...defaultProps} isLoading={true} />);

      const spinner = screen.getByRole('status');
      expect(spinner).toHaveAttribute('aria-live', 'polite');
    });

    it('should have descriptive alt text for current photo', () => {
      render(<PhotoUploadWidget {...defaultProps} currentPhotoUrl="https://example.com/photo.jpg" />);

      const image = screen.getByAltText(/current profile photo/i);
      expect(image).toBeInTheDocument();
    });
  });
});
