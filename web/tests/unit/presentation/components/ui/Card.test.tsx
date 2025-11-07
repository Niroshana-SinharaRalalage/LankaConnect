import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/presentation/components/ui/Card';

describe('Card Components', () => {
  describe('Card', () => {
    it('should render card container', () => {
      render(<Card>Card content</Card>);

      const card = screen.getByText(/card content/i);
      expect(card).toBeInTheDocument();
    });

    it('should have default styling', () => {
      render(<Card data-testid="card">Card content</Card>);

      const card = screen.getByTestId('card');
      expect(card).toHaveClass('rounded-lg', 'border', 'bg-card');
    });

    it('should accept custom className', () => {
      render(<Card className="custom-class" data-testid="card">Card content</Card>);

      const card = screen.getByTestId('card');
      expect(card).toHaveClass('custom-class');
    });
  });

  describe('CardHeader', () => {
    it('should render card header', () => {
      render(<CardHeader>Header content</CardHeader>);

      const header = screen.getByText(/header content/i);
      expect(header).toBeInTheDocument();
    });

    it('should have default styling', () => {
      render(<CardHeader data-testid="header">Header</CardHeader>);

      const header = screen.getByTestId('header');
      expect(header).toHaveClass('flex', 'flex-col', 'space-y-1.5');
    });
  });

  describe('CardTitle', () => {
    it('should render card title', () => {
      render(<CardTitle>Card Title</CardTitle>);

      const title = screen.getByText(/card title/i);
      expect(title).toBeInTheDocument();
    });

    it('should have default styling', () => {
      render(<CardTitle data-testid="title">Title</CardTitle>);

      const title = screen.getByTestId('title');
      expect(title).toHaveClass('text-2xl', 'font-semibold');
    });
  });

  describe('CardDescription', () => {
    it('should render card description', () => {
      render(<CardDescription>Card description text</CardDescription>);

      const description = screen.getByText(/card description text/i);
      expect(description).toBeInTheDocument();
    });

    it('should have default styling', () => {
      render(<CardDescription data-testid="description">Description</CardDescription>);

      const description = screen.getByTestId('description');
      expect(description).toHaveClass('text-sm', 'text-muted-foreground');
    });
  });

  describe('CardContent', () => {
    it('should render card content', () => {
      render(<CardContent>Main content</CardContent>);

      const content = screen.getByText(/main content/i);
      expect(content).toBeInTheDocument();
    });

    it('should have default styling', () => {
      render(<CardContent data-testid="content">Content</CardContent>);

      const content = screen.getByTestId('content');
      expect(content).toHaveClass('p-6', 'pt-0');
    });
  });

  describe('CardFooter', () => {
    it('should render card footer', () => {
      render(<CardFooter>Footer content</CardFooter>);

      const footer = screen.getByText(/footer content/i);
      expect(footer).toBeInTheDocument();
    });

    it('should have default styling', () => {
      render(<CardFooter data-testid="footer">Footer</CardFooter>);

      const footer = screen.getByTestId('footer');
      expect(footer).toHaveClass('flex', 'items-center', 'p-6', 'pt-0');
    });
  });

  describe('composition', () => {
    it('should render complete card with all parts', () => {
      render(
        <Card>
          <CardHeader>
            <CardTitle>Login</CardTitle>
            <CardDescription>Enter your credentials</CardDescription>
          </CardHeader>
          <CardContent>Form fields here</CardContent>
          <CardFooter>Footer actions</CardFooter>
        </Card>
      );

      expect(screen.getByText(/login/i)).toBeInTheDocument();
      expect(screen.getByText(/enter your credentials/i)).toBeInTheDocument();
      expect(screen.getByText(/form fields here/i)).toBeInTheDocument();
      expect(screen.getByText(/footer actions/i)).toBeInTheDocument();
    });
  });
});
