/**
 * Regression tests for the four sub-config forms after the contents-only refactor.
 * Each form must NOT render its own card chrome (title/header) — the parent
 * (EventCreationForm / EventEditForm) now owns the chrome via <CollapsibleSection>.
 * Each form MUST still render its enable-toggle checkbox.
 *
 * Phase 6A.156-fix adds two new cases on SponsorConfigForm to pin the embedded
 * SponsorshipPackageEditor contract (rendered only when both the sponsor toggle
 * AND the packages toggle are on).
 */
import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { DonationConfigForm } from '../DonationConfigForm';
import { CollectionConfigForm } from '../CollectionConfigForm';
import { SponsorConfigForm } from '../SponsorConfigForm';
import { AddOnConfigForm } from '../AddOnConfigForm';

// AddOnDefinitionEditor pulls in API hooks; mock it to keep these tests pure.
vi.mock('../AddOnDefinitionEditor', () => ({
  AddOnDefinitionEditor: () => <div data-testid="addon-editor-stub" />,
}));

// Phase 6A.156-fix — SponsorshipPackageEditor pulls in React Query hooks; mock
// it so we can assert presence/absence by data-testid without mounting the
// real editor or its hook stack.
vi.mock('../SponsorshipPackageEditor', () => ({
  SponsorshipPackageEditor: () => <div data-testid="sponsorship-package-editor-stub" />,
}));

describe('Sub-config forms — contents-only contract', () => {
  describe('DonationConfigForm', () => {
    it('renders the toggle checkbox', () => {
      render(
        <DonationConfigForm
          isEnabled={false}
          onEnabledChange={() => {}}
          suggestedAmounts={[]}
          onSuggestedAmountsChange={() => {}}
          allowCustomAmount={false}
          onAllowCustomAmountChange={() => {}}
          minAmount={null}
          onMinAmountChange={() => {}}
          maxAmount={null}
          onMaxAmountChange={() => {}}
          donationMessage=""
          onDonationMessageChange={() => {}}
          showDonationSummary={false}
          onShowDonationSummaryChange={() => {}}
        />
      );
      expect(document.getElementById('enableDonations')).not.toBeNull();
    });

    it('does not render its own card title (parent owns chrome)', () => {
      render(
        <DonationConfigForm
          isEnabled={false}
          onEnabledChange={() => {}}
          suggestedAmounts={[]}
          onSuggestedAmountsChange={() => {}}
          allowCustomAmount={false}
          onAllowCustomAmountChange={() => {}}
          minAmount={null}
          onMinAmountChange={() => {}}
          maxAmount={null}
          onMaxAmountChange={() => {}}
          donationMessage=""
          onDonationMessageChange={() => {}}
          showDonationSummary={false}
          onShowDonationSummaryChange={() => {}}
        />
      );
      expect(screen.queryByText('Donations (Optional)')).not.toBeInTheDocument();
    });
  });

  describe('CollectionConfigForm', () => {
    const props = {
      isEnabled: false,
      onEnabledChange: () => {},
      goalAmount: null,
      onGoalAmountChange: () => {},
      showProgress: false,
      onShowProgressChange: () => {},
      suggestedAmounts: [],
      onSuggestedAmountsChange: () => {},
      allowCustomAmount: false,
      onAllowCustomAmountChange: () => {},
      minAmount: null,
      onMinAmountChange: () => {},
      maxAmount: null,
      onMaxAmountChange: () => {},
      collectionMessage: '',
      onCollectionMessageChange: () => {},
      showContributorCount: false,
      onShowContributorCountChange: () => {},
    };

    it('renders the toggle checkbox', () => {
      render(<CollectionConfigForm {...props} />);
      expect(document.getElementById('enableCollections')).not.toBeNull();
    });

    it('does not render its own card title', () => {
      render(<CollectionConfigForm {...props} />);
      expect(screen.queryByText(/Event Fund \/ Collections \(Optional\)/i)).not.toBeInTheDocument();
    });
  });

  describe('SponsorConfigForm', () => {
    const props = {
      isEnabled: false,
      onEnabledChange: () => {},
      acceptMoneySponsors: false,
      onAcceptMoneySponsorsChange: () => {},
      acceptItemSponsors: false,
      onAcceptItemSponsorsChange: () => {},
      minSponsorAmount: null,
      onMinSponsorAmountChange: () => {},
      sponsorMessage: '',
      onSponsorMessageChange: () => {},
      showSponsorList: false,
      onShowSponsorListChange: () => {},
    };

    it('renders the toggle checkbox', () => {
      render(<SponsorConfigForm {...props} />);
      expect(document.getElementById('enableSponsors')).not.toBeNull();
    });

    it('does not render its own card title', () => {
      render(<SponsorConfigForm {...props} />);
      expect(screen.queryByText('Sponsorships (Optional)')).not.toBeInTheDocument();
    });

    /**
     * Phase 6A.156-fix — operator UAT required folding the packages CRUD into
     * the sponsor config (not a separate Packages sub-tab). The editor is
     * gated on BOTH the sponsor toggle AND the packages toggle to keep the
     * tree quiet for callers that haven't opted in.
     */
    it('does NOT embed the packages editor when sponsors are disabled', () => {
      render(
        <SponsorConfigForm
          {...props}
          isEnabled={false}
          enablePackages={true}
          onEnablePackagesChange={() => {}}
        />
      );
      expect(screen.queryByTestId('sponsorship-package-editor-stub')).not.toBeInTheDocument();
    });

    it('does NOT embed the packages editor when the packages toggle is off (sponsors enabled)', () => {
      render(
        <SponsorConfigForm
          {...props}
          isEnabled={true}
          enablePackages={false}
          onEnablePackagesChange={() => {}}
        />
      );
      expect(screen.queryByTestId('sponsorship-package-editor-stub')).not.toBeInTheDocument();
    });

    it('embeds the packages editor when both sponsors AND packages toggles are on', () => {
      render(
        <SponsorConfigForm
          {...props}
          isEnabled={true}
          enablePackages={true}
          onEnablePackagesChange={() => {}}
        />
      );
      expect(screen.getByTestId('sponsorship-package-editor-stub')).toBeInTheDocument();
    });
  });

  describe('AddOnConfigForm', () => {
    const props = {
      isEnabled: false,
      onEnabledChange: () => {},
      availableDuringRegistration: false,
      onAvailableDuringRegistrationChange: () => {},
      availableStandalone: false,
      onAvailableStandaloneChange: () => {},
      addOnMessage: '',
      onAddOnMessageChange: () => {},
    };

    it('renders the toggle checkbox', () => {
      render(<AddOnConfigForm {...props} />);
      expect(document.getElementById('enableAddOns')).not.toBeNull();
    });

    it('does not render its own card title', () => {
      render(<AddOnConfigForm {...props} />);
      expect(screen.queryByText('Add-Ons (Optional)')).not.toBeInTheDocument();
    });
  });
});
