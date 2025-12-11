import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import Home from '@/app/page';

describe('Landing Page', () => {
  describe('Header Navigation', () => {
    it('should render header with logo and navigation links', () => {
      render(<Home />);

      const lankaConnectElements = screen.getAllByText('LankaConnect');
      expect(lankaConnectElements.length).toBeGreaterThan(0);

      // Check navigation links exist (they may appear multiple times in footer)
      expect(screen.getAllByText('Events').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Forums').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Business').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Culture').length).toBeGreaterThan(0);
    });

    it('should render auth buttons in header', () => {
      render(<Home />);

      expect(screen.getByRole('button', { name: /login/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /sign up/i })).toBeInTheDocument();
    });
  });

  describe('Hero Section', () => {
    it('should render hero heading and subheading', () => {
      render(<Home />);

      expect(screen.getByRole('heading', { name: /connect with sri lankan communities worldwide/i })).toBeInTheDocument();
      expect(screen.getByText(/building meaningful connections/i)).toBeInTheDocument();
    });

    it('should render CTA buttons', () => {
      render(<Home />);

      const getStartedButtons = screen.getAllByRole('button', { name: /get started/i });
      expect(getStartedButtons.length).toBeGreaterThan(0);
      expect(screen.getByRole('button', { name: /learn more/i })).toBeInTheDocument();
    });
  });

  describe('Community Stats Section', () => {
    it('should render stats using StatCard component', () => {
      render(<Home />);

      expect(screen.getByText('12,500+')).toBeInTheDocument();
      expect(screen.getByText('450+')).toBeInTheDocument();
      expect(screen.getByText('2,200+')).toBeInTheDocument();
    });

    it('should render stat labels', () => {
      render(<Home />);

      const allText = screen.getAllByText(/members|events|businesses/i);
      expect(allText.length).toBeGreaterThan(0);
    });
  });

  describe('Features Section', () => {
    it('should render features section', () => {
      render(<Home />);

      expect(screen.getByText(/discover events/i)).toBeInTheDocument();
      expect(screen.getByText(/connect with community/i)).toBeInTheDocument();
      expect(screen.getByText(/support local businesses/i)).toBeInTheDocument();
    });
  });

  describe('Responsive Design', () => {
    it('should have responsive container classes', () => {
      const { container } = render(<Home />);

      expect(container.querySelector('.container')).toBeInTheDocument();
    });
  });
});
