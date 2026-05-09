import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { RegistrationStatusHint } from '../RegistrationStatusHint';
import { RegistrationMode } from '@/infrastructure/api/types/events.types';

describe('RegistrationStatusHint (Phase 8YB.3)', () => {
  describe('Mode C — NoRegistration', () => {
    it('banner renders the "No registration required" headline + explanatory body', () => {
      render(
        <RegistrationStatusHint
          registrationMode={RegistrationMode.NoRegistration}
          variant="banner"
        />,
      );
      expect(screen.getByText(/no registration required/i)).toBeInTheDocument();
      expect(
        screen.getByText(/just show up|drop.in|contributions are still welcome/i),
      ).toBeInTheDocument();
    });

    it('banner uses the blue Info card visual language (matches existing prior-art)', () => {
      const { container } = render(
        <RegistrationStatusHint
          registrationMode={RegistrationMode.NoRegistration}
          variant="banner"
        />,
      );
      const banner = container.firstChild as HTMLElement;
      expect(banner.className).toMatch(/bg-blue-50|border-blue/);
    });

    it('pill renders compact "No registration required" status', () => {
      render(
        <RegistrationStatusHint
          registrationMode={RegistrationMode.NoRegistration}
          variant="pill"
        />,
      );
      expect(screen.getByText(/no registration required/i)).toBeInTheDocument();
    });

    it('pill is NOT a clickable button — it is a status indicator', () => {
      const { container } = render(
        <RegistrationStatusHint
          registrationMode={RegistrationMode.NoRegistration}
          variant="pill"
        />,
      );
      // No interactive role: must not render a button or anchor (those would
      // confuse users since there's nowhere to scroll to).
      expect(container.querySelector('button')).toBeNull();
      expect(container.querySelector('a')).toBeNull();
    });
  });

  describe('Other registration modes — render nothing', () => {
    const otherModes: ReadonlyArray<RegistrationMode> = [
      RegistrationMode.DetailedAttendees,
      RegistrationMode.HeadCountOnly,
      RegistrationMode.HeadCountByAge,
      RegistrationMode.HeadCountByGender,
      RegistrationMode.HeadCountByAgeAndGender,
      RegistrationMode.External,
    ];

    otherModes.forEach((mode) => {
      it(`banner returns null for ${mode}`, () => {
        const { container } = render(
          <RegistrationStatusHint registrationMode={mode} variant="banner" />,
        );
        expect(container.firstChild).toBeNull();
      });

      it(`pill returns null for ${mode}`, () => {
        const { container } = render(
          <RegistrationStatusHint registrationMode={mode} variant="pill" />,
        );
        expect(container.firstChild).toBeNull();
      });
    });
  });

  describe('Cancelled events — cancelled banner takes precedence', () => {
    it('banner returns null when isCancelled=true even for Mode C', () => {
      const { container } = render(
        <RegistrationStatusHint
          registrationMode={RegistrationMode.NoRegistration}
          variant="banner"
          isCancelled
        />,
      );
      expect(container.firstChild).toBeNull();
    });

    it('pill returns null when isCancelled=true even for Mode C', () => {
      const { container } = render(
        <RegistrationStatusHint
          registrationMode={RegistrationMode.NoRegistration}
          variant="pill"
          isCancelled
        />,
      );
      expect(container.firstChild).toBeNull();
    });
  });
});
