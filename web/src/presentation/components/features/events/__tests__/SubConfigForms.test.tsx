/**
 * Regression tests for the four sub-config forms after the contents-only refactor.
 * Each form must NOT render its own card chrome (title/header) — the parent
 * (EventCreationForm / EventEditForm) now owns the chrome via <CollapsibleSection>.
 * Each form MUST still render its enable-toggle checkbox.
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
