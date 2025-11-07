import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { Input } from '@/presentation/components/ui/Input';

describe('Input Component', () => {
  describe('rendering', () => {
    it('should render input element', () => {
      render(<Input />);

      const input = screen.getByRole('textbox');
      expect(input).toBeInTheDocument();
    });

    it('should render with placeholder', () => {
      render(<Input placeholder="Enter your email" />);

      const input = screen.getByPlaceholderText(/enter your email/i);
      expect(input).toBeInTheDocument();
    });

    it('should render with default type="text"', () => {
      render(<Input />);

      const input = screen.getByRole('textbox');
      expect(input).toHaveAttribute('type', 'text');
    });

    it('should support type="email"', () => {
      render(<Input type="email" />);

      const input = screen.getByRole('textbox');
      expect(input).toHaveAttribute('type', 'email');
    });

    it('should support type="password"', () => {
      render(<Input type="password" />);

      const input = document.querySelector('input[type="password"]');
      expect(input).toBeInTheDocument();
    });
  });

  describe('states', () => {
    it('should be disabled when disabled prop is true', () => {
      render(<Input disabled />);

      const input = screen.getByRole('textbox');
      expect(input).toBeDisabled();
    });

    it('should show error state', () => {
      render(<Input error />);

      const input = screen.getByRole('textbox');
      expect(input).toHaveClass('border-destructive');
    });

    it('should show disabled styling', () => {
      render(<Input disabled />);

      const input = screen.getByRole('textbox');
      expect(input).toHaveClass('disabled:cursor-not-allowed');
    });
  });

  describe('interaction', () => {
    it('should call onChange when value changes', () => {
      const handleChange = vi.fn();
      render(<Input onChange={handleChange} />);

      const input = screen.getByRole('textbox');
      fireEvent.change(input, { target: { value: 'test@example.com' } });

      expect(handleChange).toHaveBeenCalledTimes(1);
    });

    it('should update value', () => {
      render(<Input defaultValue="initial" />);

      const input = screen.getByRole('textbox') as HTMLInputElement;
      expect(input.value).toBe('initial');

      fireEvent.change(input, { target: { value: 'updated' } });
      expect(input.value).toBe('updated');
    });

    it('should not be editable when disabled', () => {
      const handleChange = vi.fn();
      render(<Input disabled onChange={handleChange} />);

      const input = screen.getByRole('textbox');
      fireEvent.change(input, { target: { value: 'test' } });

      // Disabled inputs don't trigger onChange
      expect(handleChange).not.toHaveBeenCalled();
    });
  });

  describe('accessibility', () => {
    it('should support aria-label', () => {
      render(<Input aria-label="Email address" />);

      const input = screen.getByLabelText(/email address/i);
      expect(input).toBeInTheDocument();
    });

    it('should support aria-describedby for error messages', () => {
      render(<Input aria-describedby="email-error" />);

      const input = screen.getByRole('textbox');
      expect(input).toHaveAttribute('aria-describedby', 'email-error');
    });

    it('should have aria-invalid when error prop is true', () => {
      render(<Input error />);

      const input = screen.getByRole('textbox');
      expect(input).toHaveAttribute('aria-invalid', 'true');
    });
  });

  describe('custom styling', () => {
    it('should accept custom className', () => {
      render(<Input className="custom-class" />);

      const input = screen.getByRole('textbox');
      expect(input).toHaveClass('custom-class');
    });

    it('should merge custom className with default classes', () => {
      render(<Input className="custom-class" />);

      const input = screen.getByRole('textbox');
      expect(input).toHaveClass('custom-class', 'flex');
    });
  });

  describe('ref forwarding', () => {
    it('should forward ref to input element', () => {
      const ref = React.createRef<HTMLInputElement>();
      render(<Input ref={ref} />);

      expect(ref.current).toBeInstanceOf(HTMLInputElement);
    });
  });
});
