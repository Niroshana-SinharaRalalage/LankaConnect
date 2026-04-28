'use client';

import { useForm, useFieldArray, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useRouter } from 'next/navigation';
import { useState, useMemo, useEffect } from 'react';
import { Calendar, MapPin, Users, DollarSign, FileText, Tag, Mail, Link2, Star, Heart, Wallet, HandCoins, PackagePlus } from 'lucide-react';
import { Card, CardContent } from '@/presentation/components/ui/Card';
import { CollapsibleSection } from '@/presentation/components/ui/CollapsibleSection';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { MultiSelect } from '@/presentation/components/ui/MultiSelect';
import { createEventSchema, type CreateEventFormData } from '@/presentation/lib/validators/event.schemas';
import { useCreateEvent } from '@/presentation/hooks/useEvents';
import { useEmailGroups } from '@/presentation/hooks/useEmailGroups';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useEventCategories, useCurrencies } from '@/infrastructure/api/hooks/useReferenceData';
import { useContentImageUpload } from '@/presentation/hooks/useContentImageUpload';
import { EventCategory, Currency, TicketingMode, SeatingMode, RegistrationMode } from '@/infrastructure/api/types/events.types';
import { RegistrationModePicker } from './RegistrationModePicker';
import { geocodeAddress } from '@/presentation/lib/utils/geocoding';
import { RichTextEditor } from '@/presentation/components/ui/RichTextEditor';
import { GroupPricingTierBuilder } from './GroupPricingTierBuilder';
import { TicketTierBuilder, type TicketTierFormData } from './TicketTierBuilder';
import { SeatingSection } from './SeatingSection';
import { RevenueBreakdownPreview } from './RevenueBreakdownPreview';
import { DonationConfigForm } from './DonationConfigForm';
import { CollectionConfigForm } from './CollectionConfigForm';
import { SponsorConfigForm } from './SponsorConfigForm';
import { AddOnConfigForm } from './AddOnConfigForm';
import { CoOrganizerInlineSearch } from './CoOrganizerInlineSearch';
import { SecondaryLocationFieldset } from './SecondaryLocationFieldset';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import { buildCodeToIntMap, toDropdownOptions } from '@/infrastructure/api/utils/enum-mappers';
import type { UserSearchResultDto } from '@/infrastructure/api/types/events.types';

// Identifiers for each collapsible section. Stable across create/edit so the
// FIELD_TO_SECTION map below works for both forms.
type SectionKey =
  | 'basic'
  | 'datetime'
  | 'location'
  | 'capacity'
  | 'email'
  | 'organizer'
  | 'donations'
  | 'collections'
  | 'sponsors'
  | 'addons';

// Map a Zod-validated form field path to the section that owns it. Used to
// auto-expand the offending section when validation fails on submit.
// NOTE: donations/collections/sponsors/addons are NOT zod-validated form fields
// today — they live in component state — so they don't appear here.
const FIELD_TO_SECTION: Record<string, SectionKey> = {
  title: 'basic',
  description: 'basic',
  category: 'basic',
  startDate: 'datetime',
  endDate: 'datetime',
  locationName: 'location',
  locationAddress: 'location',
  locationCity: 'location',
  locationState: 'location',
  locationZipCode: 'location',
  locationCountry: 'location',
  capacity: 'capacity',
  maxAttendeesPerRegistration: 'capacity',
  isFree: 'capacity',
  ticketPriceAmount: 'capacity',
  ticketPriceCurrency: 'capacity',
  adultPriceAmount: 'capacity',
  childPriceAmount: 'capacity',
  enableDualPricing: 'capacity',
  enableGroupPricing: 'capacity',
  enableTieredTicketing: 'capacity',
  ticketTiers: 'capacity',
  groupPricingTiers: 'capacity',
  registrationMode: 'capacity',
  emailGroupIds: 'email',
  publishOrganizerContact: 'organizer',
  organizerContacts: 'organizer',
};

/**
 * Event Creation Form Component
 * Allows organizers to create new events with comprehensive details
 *
 * Features:
 * - Basic Information: Title, Description, Category
 * - Date & Time: Start and End dates
 * - Location: Full address details (optional)
 * - Capacity: Max attendees
 * - Pricing: Free or paid events with currency selection
 * - Validation: Zod schema with cross-field validation
 */
export function EventCreationForm() {
  const router = useRouter();
  const { user } = useAuthStore();
  const createEventMutation = useCreateEvent();
  const [submitError, setSubmitError] = useState<string | null>(null);

  // Seating Redesign Slice 1: Track assigned-seating intent before the event
  // exists. Applied via eventsRepository.setSeatingMode(...) after the event
  // (and Tiered ticketing mode) has been created.
  const [seatingMode, setSeatingModeState] = useState<SeatingMode>(SeatingMode.GeneralAdmission);
  const [seatingModeError, setSeatingModeError] = useState<string | null>(null);

  // Donation Feature: Donation configuration state
  const [donationsEnabled, setDonationsEnabled] = useState(false);
  const [donationSuggestedAmounts, setDonationSuggestedAmounts] = useState<number[]>([]);
  const [donationAllowCustom, setDonationAllowCustom] = useState(true);
  const [donationMinAmount, setDonationMinAmount] = useState<number | null>(null);
  const [donationMaxAmount, setDonationMaxAmount] = useState<number | null>(null);
  const [donationMessage, setDonationMessage] = useState('');
  const [showDonationSummary, setShowDonationSummary] = useState(false);

  // Collection (Event Fund) configuration state
  const [collectionsEnabled, setCollectionsEnabled] = useState(false);
  const [collectionGoalAmount, setCollectionGoalAmount] = useState<number | null>(null);
  const [collectionShowProgress, setCollectionShowProgress] = useState(false);
  const [collectionSuggestedAmounts, setCollectionSuggestedAmounts] = useState<number[]>([]);
  const [collectionAllowCustom, setCollectionAllowCustom] = useState(true);
  const [collectionMinAmount, setCollectionMinAmount] = useState<number | null>(null);
  const [collectionMaxAmount, setCollectionMaxAmount] = useState<number | null>(null);
  const [collectionMessage, setCollectionMessage] = useState('');
  const [showContributorCount, setShowContributorCount] = useState(false);

  // Sponsor configuration state
  const [sponsorsEnabled, setSponsorsEnabled] = useState(false);
  const [acceptMoneySponsors, setAcceptMoneySponsors] = useState(true);
  const [acceptItemSponsors, setAcceptItemSponsors] = useState(true);
  const [minSponsorAmount, setMinSponsorAmount] = useState<number | null>(null);
  const [sponsorMessage, setSponsorMessage] = useState('');
  const [showSponsorList, setShowSponsorList] = useState(false);

  // Add-On configuration state
  const [addOnsEnabled, setAddOnsEnabled] = useState(false);
  const [addOnAvailableDuringRegistration, setAddOnAvailableDuringRegistration] = useState(true);
  const [addOnAvailableStandalone, setAddOnAvailableStandalone] = useState(true);
  const [addOnMessage, setAddOnMessage] = useState('');
  const [pendingAddOnDefinitions, setPendingAddOnDefinitions] = useState<import('./AddOnDefinitionEditor').PendingAddOnDefinition[]>([]);

  // Section open/close state. Create flow lands with only Basic Information open
  // so the user has a clear starting point; everything else is collapsed but
  // still mounted (CollapsibleSection uses CSS-grid animation, so react-hook-form
  // state survives toggling).
  const [sectionOpen, setSectionOpen] = useState<Record<SectionKey, boolean>>({
    basic: true,
    datetime: false,
    location: false,
    capacity: false,
    email: false,
    organizer: false,
    donations: false,
    collections: false,
    sponsors: false,
    addons: false,
  });
  const setOpen = (key: SectionKey, value: boolean) =>
    setSectionOpen((prev) => ({ ...prev, [key]: value }));
  const openSection = (key: SectionKey) => {
    setSectionOpen((prev) => (prev[key] ? prev : { ...prev, [key]: true }));
    requestAnimationFrame(() => {
      const el = document.getElementById(key);
      el?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
  };

  // Phase 6A.106 Part 3: Azure image upload for rich text editor
  const { mutateAsync: uploadImage } = useContentImageUpload();

  // Phase 6A.32: Fetch email groups for selection
  const { data: emailGroups = [], isLoading: isLoadingEmailGroups } = useEmailGroups();

  // Phase 6A.47: Fetch event categories and currencies from reference data API
  const { data: categories, isLoading: isLoadingEventInterests } = useEventCategories();
  const { data: currencies } = useCurrencies();

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    control,
    formState: { errors, isSubmitting },
  } = useForm({
    resolver: zodResolver(createEventSchema),
    defaultValues: {
      isFree: true,
      // Phase 7E.5: Default to DetailedAttendees so existing event-creation flows are unchanged.
      registrationMode: RegistrationMode.DetailedAttendees,
      enableDualPricing: false,
      enableGroupPricing: false,
      enableTieredTicketing: false,
      groupPricingTiers: [],
      ticketTiers: [],
      capacity: 50,
      // Issue #51: Max attendees per registration - default to 10
      maxAttendeesPerRegistration: 10,
      // Session 33: Don't set default currencies - they should only be set when user enters pricing mode
      // This prevents validation errors when switching between pricing modes
      ticketPriceAmount: undefined,
      ticketPriceCurrency: undefined,
      adultPriceAmount: undefined,
      adultPriceCurrency: Currency.USD,
      childPriceAmount: undefined,
      childPriceCurrency: Currency.USD,
      childAgeLimit: undefined,
      category: EventCategory.Community, // Default to Community instead of empty
    },
  });

  const isFree = watch('isFree');
  const enableDualPricing = watch('enableDualPricing');
  const enableGroupPricing = watch('enableGroupPricing');
  const enableTieredTicketing = watch('enableTieredTicketing');
  // Phase 7E.5: Mode picker state
  const registrationMode = watch('registrationMode') ?? RegistrationMode.DetailedAttendees;
  const groupPricingTiers = watch('groupPricingTiers') || [];
  const ticketTiers = (watch('ticketTiers') || []) as TicketTierFormData[];
  const publishOrganizerContact = watch('publishOrganizerContact');

  // Dynamic organizer contacts field array
  const { fields: contactFields, append: appendContact, remove: removeContact } = useFieldArray({
    control,
    name: 'organizerContacts',
  });

  // Auto-populate first organizer contact from user profile when checkbox is checked
  useEffect(() => {
    if (publishOrganizerContact && user) {
      const currentContacts = watch('organizerContacts') || [];
      if (currentContacts.length === 0) {
        setValue('organizerContacts', [{
          contactName: user.fullName || '',
          contactEmail: user.email || '',
          contactPhone: user.phoneNumber || '',
          isPrimary: true,
        }]);
      }
    }
  }, [publishOrganizerContact, user, setValue, watch]);

  // Co-organizer inline search state
  const [showCoOrgSearch, setShowCoOrgSearch] = useState(false);

  // Get list of already-linked user IDs to exclude from search results
  const linkedUserIds = useMemo(() => {
    const contacts = watch('organizerContacts') || [];
    return contacts
      .map((c) => c.linkedUserId)
      .filter((id): id is string => !!id);
  }, [watch('organizerContacts')]);

  // Handle selecting a user from inline search
  const handleSearchUserSelected = (selectedUser: UserSearchResultDto) => {
    appendContact({
      contactName: selectedUser.displayName,
      contactEmail: selectedUser.email,
      contactPhone: '',
      isPrimary: false,
      linkedUserId: selectedUser.id,
    });
    setShowCoOrgSearch(false);
  };

  // Toggle primary organizer status for a contact
  const handleTogglePrimary = (index: number) => {
    const contacts = watch('organizerContacts') || [];
    const isCurrentlyPrimary = contacts[index]?.isPrimary;
    // Clear primary from all contacts first
    contacts.forEach((_, i) => setValue(`organizerContacts.${i}.isPrimary`, false));
    // If wasn't primary, set this one as primary; if was primary, leave all cleared
    if (!isCurrentlyPrimary) {
      setValue(`organizerContacts.${index}.isPrimary`, true);
    }
  };

  // Session 33: Track validation errors for display
  const hasErrors = Object.keys(errors).length > 0;

  // Session 33: Handle form validation errors - displays message when validation fails
  const onValidationError = (validationErrors: Record<string, any>) => {
    console.error('❌ Form Validation Failed:', validationErrors);
    // List all validation error fields for debugging
    const errorFields = Object.keys(validationErrors);
    console.error('Fields with errors:', errorFields);

    // Session 33: Debug - log current form values for pricing mode
    console.log('🔍 Debug - Current form state:', {
      isFree,
      enableDualPricing,
      enableGroupPricing,
      enableTieredTicketing,
      ticketPriceAmount: watch('ticketPriceAmount'),
      ticketPriceCurrency: watch('ticketPriceCurrency'),
      adultPriceAmount: watch('adultPriceAmount'),
      childPriceAmount: watch('childPriceAmount'),
      ticketTiers: watch('ticketTiers'),
    });

    // Open every section that owns an errored field so the user is not stuck
    // with a hidden-but-invalid field. We also collect the first section to
    // scroll to after the DOM updates.
    const sectionsToOpen = new Set<SectionKey>();
    let firstSection: SectionKey | null = null;
    errorFields.forEach((field) => {
      const section = FIELD_TO_SECTION[field];
      if (section) {
        sectionsToOpen.add(section);
        if (firstSection === null) firstSection = section;
      } else if (process.env.NODE_ENV !== 'production') {
        console.warn(`[EventCreationForm] No FIELD_TO_SECTION mapping for errored field: ${field}`);
      }
    });
    if (sectionsToOpen.size > 0) {
      setSectionOpen((prev) => {
        const next = { ...prev };
        sectionsToOpen.forEach((k) => (next[k] = true));
        return next;
      });
      requestAnimationFrame(() => {
        if (firstSection) {
          const el = document.getElementById(firstSection);
          el?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
      });
    }

    setSubmitError(`Please fix the validation errors: ${errorFields.join(', ')}`);
  };

  const onSubmit = handleSubmit(async (data) => {
    if (!user?.userId) {
      setSubmitError('You must be logged in to create events');
      return;
    }

    try {
      setSubmitError(null);

      // PHASE 6A.10: Log user authentication state
      console.log('📋 Form Submission - User Context:', {
        userId: user.userId,
        userRole: user.role,
        userName: user.fullName,
        isAuthenticated: true,
      });

      // Prepare event data for backend
      // IMPORTANT: Location fields are ALL required if ANY location field is provided
      // Database constraint: Address must have Street, City, State, ZipCode, Country
      const hasCompleteLocation = !!(data.locationAddress && data.locationCity);

      // Geocode address to get lat/long coordinates for location-based filtering
      let locationLatitude: number | undefined;
      let locationLongitude: number | undefined;

      if (hasCompleteLocation) {
        console.log('🗺️ Geocoding address for location-based filtering...');
        const geocodeResult = await geocodeAddress(
          data.locationAddress!,
          data.locationCity!,
          data.locationState || undefined,
          data.locationCountry || 'United States',
          data.locationZipCode || undefined
        );

        if (geocodeResult) {
          locationLatitude = geocodeResult.latitude;
          locationLongitude = geocodeResult.longitude;
          console.log('✅ Geocoding successful:', {
            lat: locationLatitude,
            lon: locationLongitude,
            display: geocodeResult.displayName,
          });
        } else {
          console.warn('⚠️ Geocoding failed - event will not appear in location-based filters');
          // Continue anyway - location text will still be saved
        }
      }

      // Issue #48 Fix: Convert local datetime-local input values to UTC ISO strings
      // The datetime-local input returns local time (e.g., "2024-01-15T14:00")
      // We need to convert this to UTC before sending to the backend
      const startDateUtc = new Date(data.startDate).toISOString();
      const endDateUtc = new Date(data.endDate).toISOString();

      const eventData = {
        title: data.title,
        description: data.description,
        startDate: startDateUtc,
        endDate: endDateUtc,
        organizerId: user.userId,
        capacity: data.capacity,
        // Issue #51: Max attendees per registration
        maxAttendeesPerRegistration: data.maxAttendeesPerRegistration,
        category: data.category,
        // Phase 6A.32: Email Groups Integration
        emailGroupIds: data.emailGroupIds || [],
        // IsFreeEvent fix: Send explicit free event flag to backend
        isFree: data.isFree ?? false,
        // Phase 7E.5: Per-event registration capture mode (default DetailedAttendees back-compat).
        registrationMode: data.registrationMode ?? RegistrationMode.DetailedAttendees,
        // Organizer Contact Details (multiple contacts)
        publishOrganizerContact: data.publishOrganizerContact || false,
        organizerContacts: data.publishOrganizerContact
          ? (data.organizerContacts || []).map((c, idx) => ({
              contactName: c.contactName,
              contactEmail: c.contactEmail || null,
              contactPhone: c.contactPhone || null,
              isPrimary: c.isPrimary || false, // Preserve user's choice
              linkedUserId: c.linkedUserId || null,
            }))
          : [],
        // Donation Feature: Donation configuration
        donationsEnabled,
        ...(donationsEnabled && {
          donationSuggestedAmounts: donationSuggestedAmounts,
          donationAllowCustomAmount: donationAllowCustom,
          donationMinAmount: donationMinAmount,
          donationMaxAmount: donationMaxAmount,
          donationMessage: donationMessage || null,
          showDonationSummary: showDonationSummary,
        }),
        // Only include location if we have at least address and city
        ...(hasCompleteLocation && {
          locationAddress: data.locationAddress,
          locationCity: data.locationCity,
          locationState: data.locationState || '',
          locationZipCode: data.locationZipCode || '',
          locationCountry: data.locationCountry || 'United States',
          locationLatitude,
          locationLongitude,
        }),
        // Phase 7C.1: Primary venue name
        ...(data.locationName?.trim() && { locationName: data.locationName.trim() }),
        // Phase 7C.1: Secondary location (parking lot or secondary venue)
        ...(data.secondaryLocationType && {
          secondaryLocationType: data.secondaryLocationType,
          secondaryLocationName: data.secondaryLocationName?.trim() || undefined,
          secondaryLocationAddress: data.secondaryLocationAddress || undefined,
          secondaryLocationCity: data.secondaryLocationCity || undefined,
          secondaryLocationState: data.secondaryLocationState || undefined,
          secondaryLocationZipCode: data.secondaryLocationZipCode || undefined,
          secondaryLocationCountry: data.secondaryLocationCountry || undefined,
        }),
        // Phase 8: Tiered ticketing (highest priority)
        // Phase 6D: Group tiered pricing
        // Session 21: Dual pricing support
        // Legacy: Single pricing
        ...(data.isFree
          ? {}
          : data.enableTieredTicketing
          ? {
              // Phase 8: Send tiered ticketing data
              ticketingMode: 'Tiered' as const,
              ticketTiers: (data.ticketTiers as TicketTierFormData[] | undefined)?.map((tier) => ({
                name: tier.name,
                description: tier.description || null,
                adultPriceAmount: tier.adultPriceAmount,
                adultPriceCurrency: tier.adultPriceCurrency,
                childPriceAmount: tier.childPriceAmount ?? null,
                childPriceCurrency: tier.childPriceCurrency ?? null,
                childAgeLimit: tier.childAgeLimit ?? null,
                capacity: tier.capacity,
                maxPerUser: tier.maxPerUser,
                sortOrder: tier.sortOrder,
              })),
            }
          : data.enableGroupPricing
          ? {
              // Send group tiered pricing
              groupPricingTiers: data.groupPricingTiers?.map((tier) => ({
                minAttendees: tier.minAttendees,
                maxAttendees: tier.maxAttendees,
                pricePerPerson: tier.pricePerPerson,
                currency: tier.currency,
              })),
            }
          : data.enableDualPricing
          ? {
              // Send dual pricing
              adultPriceAmount: data.adultPriceAmount!,
              adultPriceCurrency: data.adultPriceCurrency!,
              childPriceAmount: data.childPriceAmount!,
              childPriceCurrency: data.childPriceCurrency!,
              childAgeLimit: data.childAgeLimit!,
            }
          : {
              // Send single pricing (legacy format)
              ticketPriceAmount: data.ticketPriceAmount!,
              ticketPriceCurrency: data.ticketPriceCurrency!,
            }),
      };

      // PHASE 6A.10: Comprehensive payload logging
      // Issue #48: Added timezone conversion logging
      console.log('🕐 Timezone conversion (Issue #48 fix):', {
        localStartInput: data.startDate,
        localEndInput: data.endDate,
        utcStartDate: startDateUtc,
        utcEndDate: endDateUtc,
        browserTimezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
      });
      console.log('📤 Creating event with payload:', JSON.stringify(eventData, null, 2));
      console.log('📊 Payload Analysis:', {
        hasLocation: hasCompleteLocation,
        hasCoordinates: !!(locationLatitude && locationLongitude),
        isFree: data.isFree,
        hasTicketPrice: !!data.ticketPriceAmount,
        categoryValue: data.category,
        dataSize: JSON.stringify(eventData).length,
      });

      console.log('⏳ Sending request to API...');
      const eventId = await createEventMutation.mutateAsync(eventData);
      console.log('✅ Event created successfully! ID:', eventId);

      // Post-create: Save financial config forms via separate API calls
      // Only send for ENABLED configs (disabled is the default for new events)
      const configPromises: Promise<void>[] = [];

      if (collectionsEnabled) {
        configPromises.push(
          eventsRepository.updateCollectionConfig(eventId, {
            isEnabled: true,
            goalAmount: collectionGoalAmount,
            showProgress: collectionShowProgress,
            suggestedAmounts: collectionSuggestedAmounts.length > 0 ? collectionSuggestedAmounts : null,
            allowCustomAmount: collectionAllowCustom,
            minAmount: collectionMinAmount,
            maxAmount: collectionMaxAmount,
            collectionMessage: collectionMessage || null,
            showContributorCount: showContributorCount,
          })
        );
      }

      if (sponsorsEnabled) {
        configPromises.push(
          eventsRepository.updateSponsorConfig(eventId, {
            isEnabled: true,
            acceptMoneySponsors,
            acceptItemSponsors,
            minSponsorAmount: minSponsorAmount,
            sponsorMessage: sponsorMessage || null,
            showSponsorList,
          })
        );
      }

      if (addOnsEnabled) {
        configPromises.push(
          eventsRepository.updateAddOnConfig(eventId, {
            isEnabled: true,
            availableDuringRegistration: addOnAvailableDuringRegistration,
            availableStandalone: addOnAvailableStandalone,
            addOnMessage: addOnMessage || null,
          })
        );
      }

      if (configPromises.length > 0) {
        try {
          await Promise.all(configPromises);
          console.log('✅ Financial config forms saved successfully');
        } catch (configErr) {
          console.error('⚠️ Some financial configs failed to save:', configErr);
          // Non-blocking — event was created, configs can be set later from manage page
        }
      }

      // Create pending add-on definitions (queued during local-mode editing)
      if (addOnsEnabled && pendingAddOnDefinitions.length > 0) {
        try {
          await Promise.all(
            pendingAddOnDefinitions.map((def) =>
              eventsRepository.createAddOnDefinition(eventId, {
                name: def.name,
                description: def.description,
                price: def.price,
                currency: def.currency,
                quantityLimit: def.quantityLimit,
                sortOrder: def.sortOrder,
              })
            )
          );
          console.log(`✅ ${pendingAddOnDefinitions.length} add-on definitions created`);
        } catch (defErr) {
          console.error('⚠️ Some add-on definitions failed to save:', defErr);
          // Non-blocking — definitions can be added later from manage page
        }
      }

      // Phase 8: Create ticket tiers via dedicated CRUD endpoints after event creation
      // The createEvent() call does NOT handle tier data — tiers require separate API calls.
      if (data.enableTieredTicketing && data.ticketTiers && (data.ticketTiers as TicketTierFormData[]).length > 0) {
        try {
          // Step 1: Set ticketing mode to Tiered
          await eventsRepository.setTicketingMode(eventId, TicketingMode.Tiered);
          console.log('✅ Ticketing mode set to Tiered');

          // Step 2: Create each tier
          const formTiers = data.ticketTiers as TicketTierFormData[];
          await Promise.all(
            formTiers.map((tier) =>
              eventsRepository.addTicketTier(eventId, {
                name: tier.name,
                description: tier.description || null,
                adultPriceAmount: tier.adultPriceAmount,
                adultPriceCurrency: tier.adultPriceCurrency as unknown as Currency,
                childPriceAmount: tier.childPriceAmount ?? null,
                childPriceCurrency: tier.childPriceCurrency ? (tier.childPriceCurrency as unknown as Currency) : undefined,
                childAgeLimit: tier.childAgeLimit ?? undefined,
                capacity: tier.capacity,
                maxPerUser: tier.maxPerUser,
                sortOrder: tier.sortOrder,
              })
            )
          );
          console.log(`✅ ${formTiers.length} ticket tiers created`);
        } catch (tierErr) {
          console.error('⚠️ Tier creation failed:', tierErr);
          // Non-blocking — tiers can be added later from manage page
        }
      }

      // Seating Redesign Slice 1: If the organizer enabled assigned seating
      // during creation, persist the flip AFTER setTicketingMode(Tiered).
      // The backend rejects AssignedSeating on a non-Tiered event.
      if (data.enableTieredTicketing && seatingMode === SeatingMode.AssignedSeating) {
        try {
          console.log('🪑 Applying assigned seating mode to new event');
          await eventsRepository.setSeatingMode(eventId, SeatingMode.AssignedSeating);
          console.log('✅ Seating mode set to AssignedSeating');
        } catch (seatErr) {
          // Non-blocking — event exists, tiers exist; user can toggle from manage page.
          console.error('⚠️ Seating mode update failed:', seatErr);
          setSeatingModeError(
            seatErr instanceof Error ? seatErr.message : 'Failed to enable assigned seating.'
          );
        }
      }

      // Phase 6A.41: Redirect to manage page for seamless event setup workflow
      // User can immediately upload images, configure sign-ups, and publish
      router.push(`/events/${eventId}/manage`);
    } catch (err) {
      // PHASE 6A.10: Enhanced error logging
      console.error('❌ Event creation failed - Detailed Analysis:');
      console.error('Error Type:', err?.constructor?.name);
      console.error('Error Message:', err instanceof Error ? err.message : 'Unknown error');
      console.error('Error Stack:', err instanceof Error ? err.stack : 'No stack trace');

      // Extract detailed error message from API response
      const errorMessage = err instanceof Error
        ? err.message
        : typeof err === 'object' && err !== null && 'message' in err
        ? String(err.message)
        : 'Failed to create event. Please try again.';
      setSubmitError(errorMessage);

      // Log full error for debugging
      console.error('Full error object:', JSON.stringify(err, Object.getOwnPropertyNames(err), 2));

      // Check if error has response data
      if (typeof err === 'object' && err !== null && 'response' in err) {
        const axiosError = err as any;
        console.error('Response Status:', axiosError.response?.status);
        console.error('Response Headers:', axiosError.response?.headers);
        console.error('Response Data:', axiosError.response?.data);
      }
    }
  }, onValidationError);  // Session 33: Add validation error callback

  // Phase 6A.47: Map categories to category options using buildCodeToIntMap utility
  const categoryCodeToEnumValue = useMemo(
    () => buildCodeToIntMap<EventCategory>(categories),
    [categories]
  );

  const categoryOptions = categories?.map(cat => ({
    value: categoryCodeToEnumValue[cat.code] ?? EventCategory.Community,
    label: cat.name,
  })) ?? [];

  // Phase 6A.47: Convert currencies to dropdown options
  const currencyOptions = useMemo(() => toDropdownOptions(currencies), [currencies]);

  return (
    <form onSubmit={onSubmit} className="space-y-6">
      {/* Basic Information Section */}
      <div id="basic" className="scroll-mt-20">
        <CollapsibleSection
          title="Basic Information"
          description="Provide the essential details about your event"
          icon={<FileText className="h-5 w-5" style={{ color: '#FF7900' }} />}
          open={sectionOpen.basic}
          onOpenChange={(o) => setOpen('basic', o)}
        >
          <div className="space-y-4">
          {/* Event Title */}
          <div>
            <label htmlFor="title" className="block text-sm font-medium text-neutral-700 mb-2">
              Event Title *
            </label>
            <Input
              id="title"
              type="text"
              placeholder="e.g., Sri Lankan Cultural Festival 2025"
              error={!!errors.title}
              {...register('title')}
            />
            {errors.title && (
              <p className="mt-1 text-sm text-destructive">{errors.title.message}</p>
            )}
          </div>

          {/* Event Description */}
          <div>
            <label className="block text-sm font-medium text-neutral-700 mb-2">
              Event Description *
            </label>
            <Controller
              name="description"
              control={control}
              render={({ field }) => (
                <RichTextEditor
                  content={field.value || ''}
                  onChange={field.onChange}
                  onImageUpload={uploadImage}
                  placeholder="Provide a detailed description of your event, including what attendees can expect..."
                  error={!!errors.description}
                  errorMessage={errors.description?.message}
                  maxLength={5000}
                  minHeight={200}
                />
              )}
            />
          </div>

          {/* Event Category */}
          <div>
            <label htmlFor="category" className="block text-sm font-medium text-neutral-700 mb-2">
              Event Category *
            </label>
            <div className="relative">
              <Tag className="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-neutral-400" />
              <select
                id="category"
                className={`w-full pl-10 pr-4 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 appearance-none ${
                  errors.category ? 'border-destructive' : 'border-neutral-300'
                }`}
                {...register('category', { valueAsNumber: true })}
              >
                {categoryOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </div>
            {errors.category && (
              <p className="mt-1 text-sm text-destructive">{errors.category.message}</p>
            )}
          </div>
          </div>
        </CollapsibleSection>
      </div>

      {/* Date & Time Section */}
      <div id="datetime" className="scroll-mt-20">
        <CollapsibleSection
          title="Date & Time"
          description="Specify when your event will take place"
          icon={<Calendar className="h-5 w-5" style={{ color: '#FF7900' }} />}
          open={sectionOpen.datetime}
          onOpenChange={(o) => setOpen('datetime', o)}
        >
          <div className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Start Date & Time */}
            <div>
              <label htmlFor="startDate" className="block text-sm font-medium text-neutral-700 mb-2">
                Start Date & Time *
              </label>
              <Input
                id="startDate"
                type="datetime-local"
                error={!!errors.startDate}
                {...register('startDate')}
              />
              {errors.startDate && (
                <p className="mt-1 text-sm text-destructive">{errors.startDate.message}</p>
              )}
            </div>

            {/* End Date & Time */}
            <div>
              <label htmlFor="endDate" className="block text-sm font-medium text-neutral-700 mb-2">
                End Date & Time *
              </label>
              <Input
                id="endDate"
                type="datetime-local"
                error={!!errors.endDate}
                {...register('endDate')}
              />
              {errors.endDate && (
                <p className="mt-1 text-sm text-destructive">{errors.endDate.message}</p>
              )}
            </div>
          </div>
          </div>
        </CollapsibleSection>
      </div>

      {/* Location Section */}
      <div id="location" className="scroll-mt-20">
        <CollapsibleSection
          title="Location"
          description="Where will the event take place? (Optional but recommended)"
          icon={<MapPin className="h-5 w-5" style={{ color: '#FF7900' }} />}
          open={sectionOpen.location}
          onOpenChange={(o) => setOpen('location', o)}
        >
          <div className="space-y-4">
          {/* Venue Name (Phase 7C.1) */}
          <div>
            <label htmlFor="locationName" className="block text-sm font-medium text-neutral-700 mb-2">
              Venue Name
            </label>
            <Input
              id="locationName"
              type="text"
              placeholder="e.g., Park Community Hall"
              error={!!errors.locationName}
              {...register('locationName')}
            />
            {errors.locationName && (
              <p className="mt-1 text-sm text-destructive">{errors.locationName.message}</p>
            )}
          </div>

          {/* Address */}
          <div>
            <label htmlFor="locationAddress" className="block text-sm font-medium text-neutral-700 mb-2">
              Street Address
            </label>
            <Input
              id="locationAddress"
              type="text"
              placeholder="e.g., 123 Main Street"
              error={!!errors.locationAddress}
              {...register('locationAddress')}
            />
            {errors.locationAddress && (
              <p className="mt-1 text-sm text-destructive">{errors.locationAddress.message}</p>
            )}
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* City */}
            <div>
              <label htmlFor="locationCity" className="block text-sm font-medium text-neutral-700 mb-2">
                City
              </label>
              <Input
                id="locationCity"
                type="text"
                placeholder="e.g., Columbus"
                error={!!errors.locationCity}
                {...register('locationCity')}
              />
              {errors.locationCity && (
                <p className="mt-1 text-sm text-destructive">{errors.locationCity.message}</p>
              )}
            </div>

            {/* State */}
            <div>
              <label htmlFor="locationState" className="block text-sm font-medium text-neutral-700 mb-2">
                State
              </label>
              <Input
                id="locationState"
                type="text"
                placeholder="e.g., Ohio"
                error={!!errors.locationState}
                {...register('locationState')}
              />
              {errors.locationState && (
                <p className="mt-1 text-sm text-destructive">{errors.locationState.message}</p>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* ZIP Code */}
            <div>
              <label htmlFor="locationZipCode" className="block text-sm font-medium text-neutral-700 mb-2">
                ZIP Code
              </label>
              <Input
                id="locationZipCode"
                type="text"
                placeholder="e.g., 43201"
                error={!!errors.locationZipCode}
                {...register('locationZipCode')}
              />
              {errors.locationZipCode && (
                <p className="mt-1 text-sm text-destructive">{errors.locationZipCode.message}</p>
              )}
            </div>

            {/* Country */}
            <div>
              <label htmlFor="locationCountry" className="block text-sm font-medium text-neutral-700 mb-2">
                Country
              </label>
              <Input
                id="locationCountry"
                type="text"
                placeholder="e.g., United States"
                error={!!errors.locationCountry}
                {...register('locationCountry')}
              />
              {errors.locationCountry && (
                <p className="mt-1 text-sm text-destructive">{errors.locationCountry.message}</p>
              )}
            </div>
          </div>

          {/* Phase 7C.1: Secondary location (parking lot or secondary venue) */}
          <SecondaryLocationFieldset
            register={register as any}
            watch={watch as any}
            setValue={setValue as any}
            errors={errors as any}
          />
          </div>
        </CollapsibleSection>
      </div>

      {/* Capacity & Pricing Section */}
      <div id="capacity" className="scroll-mt-20">
        <CollapsibleSection
          title="Capacity & Pricing"
          description="Set attendance limits and ticket pricing"
          icon={<Users className="h-5 w-5" style={{ color: '#FF7900' }} />}
          open={sectionOpen.capacity}
          onOpenChange={(o) => setOpen('capacity', o)}
        >
          <div className="space-y-4">
          {/* Capacity */}
          <div>
            <label htmlFor="capacity" className="block text-sm font-medium text-neutral-700 mb-2">
              Maximum Capacity *
            </label>
            <Input
              id="capacity"
              type="number"
              min="1"
              max="10000"
              placeholder="e.g., 100"
              error={!!errors.capacity}
              {...register('capacity', { valueAsNumber: true })}
            />
            {errors.capacity && (
              <p className="mt-1 text-sm text-destructive">{errors.capacity.message}</p>
            )}
          </div>

          {/* Issue #51: Max Attendees Per Registration */}
          <div>
            <label htmlFor="maxAttendeesPerRegistration" className="block text-sm font-medium text-neutral-700 mb-2">
              Max Attendees Per Registration
            </label>
            <Input
              id="maxAttendeesPerRegistration"
              type="number"
              min="1"
              max="50"
              placeholder="e.g., 10"
              error={!!errors.maxAttendeesPerRegistration}
              {...register('maxAttendeesPerRegistration', { valueAsNumber: true })}
            />
            {errors.maxAttendeesPerRegistration && (
              <p className="mt-1 text-sm text-destructive">{errors.maxAttendeesPerRegistration.message}</p>
            )}
            <p className="mt-1 text-xs text-neutral-500">
              Maximum number of attendees allowed in a single registration (1-50)
            </p>
          </div>

          {/* Free Event Toggle */}
          <div className="flex items-center gap-3 p-4 bg-neutral-50 rounded-lg">
            <input
              id="isFree"
              type="checkbox"
              className="h-5 w-5 rounded border-neutral-300 text-orange-500 focus:ring-2 focus:ring-orange-500"
              {...register('isFree')}
            />
            <label htmlFor="isFree" className="text-sm font-medium text-neutral-700">
              This is a free event (no ticket purchase required)
            </label>
          </div>

          {/* Phase 7E.5: Registration Mode Picker.
              Compatibility with the current event shape is queried server-side via
              `useAllowedRegistrationModes`; the picker disables incompatible options and
              auto-clears the selection if a shape change makes the current mode invalid
              (architect hot-spot #5). */}
          <Controller
            control={control}
            name="registrationMode"
            render={({ field }) => (
              <RegistrationModePicker
                value={field.value ?? RegistrationMode.DetailedAttendees}
                onChange={field.onChange}
                shape={{
                  isFreeAttendance: isFree ?? true,
                  hasDualPricing: !isFree && enableDualPricing,
                  hasGroupTiers: !isFree && enableGroupPricing,
                  hasTicketTiers: !isFree && enableTieredTicketing,
                  // Other axes (named seating, identity-bound add-on, matrix pricing) aren't
                  // captured at create-time today — defaulted false. As those fields land,
                  // wire them in here.
                }}
              />
            )}
          />

          {/* Pricing Fields (shown only if not free) */}
          {!isFree && (
            <div className="space-y-4 p-4 border-2 border-orange-200 rounded-lg bg-orange-50">
              <div className="flex items-center gap-2 mb-2">
                <DollarSign className="h-5 w-5" style={{ color: '#FF7900' }} />
                <h4 className="text-sm font-semibold text-neutral-900">Ticket Pricing</h4>
              </div>

              {/* Pricing Mode Selection */}
              <div className="space-y-3">
                {/* Dual Pricing Toggle - Session 33: Fixed onChange override bug + clear fields */}
                <div className="flex items-center gap-3 p-3 bg-white rounded-lg border border-orange-200">
                  <input
                    id="enableDualPricing"
                    type="checkbox"
                    className="h-5 w-5 rounded border-neutral-300 text-orange-500 focus:ring-2 focus:ring-orange-500"
                    {...register('enableDualPricing', {
                      onChange: (e) => {
                        if (e.target.checked) {
                          // Disable other pricing modes
                          setValue('enableGroupPricing', false);
                          setValue('enableTieredTicketing', false);
                          // Clear single pricing fields to prevent validation conflicts
                          setValue('ticketPriceAmount', undefined);
                          setValue('ticketPriceCurrency', undefined);
                          // Clear group pricing fields
                          setValue('groupPricingTiers', []);
                          // Clear ticket tier fields
                          setValue('ticketTiers', []);
                          // Set default currencies for dual pricing
                          setValue('adultPriceCurrency', Currency.USD);
                          setValue('childPriceCurrency', Currency.USD);
                        } else {
                          // When unchecking, clear dual pricing fields
                          setValue('adultPriceAmount', undefined);
                          setValue('adultPriceCurrency', undefined);
                          setValue('childPriceAmount', undefined);
                          setValue('childPriceCurrency', undefined);
                          setValue('childAgeLimit', undefined);
                          // Set default for single pricing
                          setValue('ticketPriceCurrency', Currency.USD);
                        }
                      }
                    })}
                  />
                  <label htmlFor="enableDualPricing" className="text-sm font-medium text-neutral-700">
                    Enable Adult/Child Pricing (different prices for adults and children)
                  </label>
                </div>

                {/* Group Pricing Toggle - Phase 6D - Session 33: Fixed onChange override bug + clear fields */}
                <div className="flex items-center gap-3 p-3 bg-white rounded-lg border border-orange-200">
                  <input
                    id="enableGroupPricing"
                    type="checkbox"
                    className="h-5 w-5 rounded border-neutral-300 text-orange-500 focus:ring-2 focus:ring-orange-500"
                    {...register('enableGroupPricing', {
                      onChange: (e) => {
                        if (e.target.checked) {
                          // Disable other pricing modes
                          setValue('enableDualPricing', false);
                          setValue('enableTieredTicketing', false);
                          // Clear single pricing fields
                          setValue('ticketPriceAmount', undefined);
                          setValue('ticketPriceCurrency', undefined);
                          // Clear dual pricing fields
                          setValue('adultPriceAmount', undefined);
                          setValue('adultPriceCurrency', undefined);
                          setValue('childPriceAmount', undefined);
                          setValue('childPriceCurrency', undefined);
                          setValue('childAgeLimit', undefined);
                          // Clear ticket tier fields
                          setValue('ticketTiers', []);
                        } else {
                          // When unchecking, clear group pricing fields
                          setValue('groupPricingTiers', []);
                          // Set default for single pricing
                          setValue('ticketPriceCurrency', Currency.USD);
                        }
                      }
                    })}
                  />
                  <label htmlFor="enableGroupPricing" className="text-sm font-medium text-neutral-700">
                    Enable Group Tiered Pricing (quantity-based discounts for groups)
                  </label>
                </div>

                {/* Phase 8: Tiered Ticketing Toggle */}
                <div className="flex items-center gap-3 p-3 bg-white rounded-lg border border-orange-200">
                  <input
                    id="enableTieredTicketing"
                    type="checkbox"
                    className="h-5 w-5 rounded border-neutral-300 text-orange-500 focus:ring-2 focus:ring-orange-500"
                    {...register('enableTieredTicketing', {
                      onChange: (e) => {
                        if (e.target.checked) {
                          // Disable other pricing modes
                          setValue('enableDualPricing', false);
                          setValue('enableGroupPricing', false);
                          // Clear single pricing fields
                          setValue('ticketPriceAmount', undefined);
                          setValue('ticketPriceCurrency', undefined);
                          // Clear dual pricing fields
                          setValue('adultPriceAmount', undefined);
                          setValue('adultPriceCurrency', undefined);
                          setValue('childPriceAmount', undefined);
                          setValue('childPriceCurrency', undefined);
                          setValue('childAgeLimit', undefined);
                          // Clear group pricing fields
                          setValue('groupPricingTiers', []);
                        } else {
                          // When unchecking, clear ticket tiers
                          setValue('ticketTiers', []);
                          // Set default for single pricing
                          setValue('ticketPriceCurrency', Currency.USD);
                        }
                      }
                    })}
                  />
                  <label htmlFor="enableTieredTicketing" className="text-sm font-medium text-neutral-700">
                    Enable Multi-Tier Ticketing (VIP, Plus, Basic tiers with separate pricing and capacity)
                  </label>
                </div>
              </div>

              {/* Single Pricing Fields (default) */}
              {!enableDualPricing && !enableGroupPricing && !enableTieredTicketing && (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {/* Ticket Price */}
                  <div>
                    <label htmlFor="ticketPriceAmount" className="block text-sm font-medium text-neutral-700 mb-2">
                      Ticket Price *
                    </label>
                    <Input
                      id="ticketPriceAmount"
                      type="number"
                      min="0"
                      max="10000"
                      step="1"
                      placeholder="e.g., 25"
                      error={!!errors.ticketPriceAmount}
                      {...register('ticketPriceAmount', { valueAsNumber: true })}
                    />
                    {errors.ticketPriceAmount && (
                      <p className="mt-1 text-sm text-destructive">{errors.ticketPriceAmount.message}</p>
                    )}
                    {/* Phase 6A.X: Revenue breakdown preview with detailed fees */}
                    <RevenueBreakdownPreview
                      ticketPrice={watch('ticketPriceAmount') as number | undefined}
                      currency={(watch('ticketPriceCurrency') as Currency | undefined) ?? Currency.USD}
                      state={watch('locationState') as string | undefined}
                      country={watch('locationCountry') as string | undefined}
                      priceLabel="ticket"
                    />
                  </div>

                  {/* Currency */}
                  <div>
                    <label htmlFor="ticketPriceCurrency" className="block text-sm font-medium text-neutral-700 mb-2">
                      Currency *
                    </label>
                    <select
                      id="ticketPriceCurrency"
                      className={`w-full px-4 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 ${
                        errors.ticketPriceCurrency ? 'border-destructive' : 'border-neutral-300'
                      }`}
                      defaultValue={Currency.USD}
                      {...register('ticketPriceCurrency', { valueAsNumber: true })}
                    >
                      {currencyOptions.map(curr => (
                        <option key={curr.value} value={curr.value}>
                          {curr.label}
                        </option>
                      ))}
                    </select>
                    {errors.ticketPriceCurrency && (
                      <p className="mt-1 text-sm text-destructive">{errors.ticketPriceCurrency.message}</p>
                    )}
                  </div>
                </div>
              )}

              {/* Group Pricing Tier Builder - Phase 6D */}
              {enableGroupPricing && (
                <div className="space-y-4">
                  <GroupPricingTierBuilder
                    tiers={groupPricingTiers}
                    onChange={(tiers) => setValue('groupPricingTiers', tiers)}
                    defaultCurrency={watch('ticketPriceCurrency') || Currency.USD}
                    errors={errors.groupPricingTiers?.message}
                    maxAttendeesPerRegistration={watch('maxAttendeesPerRegistration') || 10}
                  />
                </div>
              )}

              {/* Phase 8: Ticket Tier Builder */}
              {enableTieredTicketing && (
                <div className="space-y-4">
                  <TicketTierBuilder
                    tiers={ticketTiers}
                    onChange={(tiers) => setValue('ticketTiers', tiers)}
                    defaultCurrency={watch('ticketPriceCurrency') || Currency.USD}
                    eventCapacity={watch('capacity')}
                    errors={errors.ticketTiers?.message}
                  />
                </div>
              )}

              {/* Seating Redesign Slice 1: Inline assigned-seating toggle,
                  only visible when tiered ticketing is enabled. */}
              {enableTieredTicketing && (
                <SeatingSection
                  ticketingMode={TicketingMode.Tiered}
                  value={seatingMode}
                  onChange={(mode) => {
                    setSeatingModeState(mode);
                    if (seatingModeError) setSeatingModeError(null);
                  }}
                  errorMessage={seatingModeError}
                />
              )}

              {/* Dual Pricing Fields (adult/child) */}
              {enableDualPricing && !enableGroupPricing && (
                <div className="space-y-4">
                  {/* Adult Pricing Row */}
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label htmlFor="adultPriceAmount" className="block text-sm font-medium text-neutral-700 mb-2">
                        Adult Ticket Price *
                      </label>
                      <Input
                        id="adultPriceAmount"
                        type="number"
                        min="0"
                        max="10000"
                        step="1"
                        placeholder="e.g., 25"
                        error={!!errors.adultPriceAmount}
                        {...register('adultPriceAmount', { valueAsNumber: true })}
                      />
                      {errors.adultPriceAmount && (
                        <p className="mt-1 text-sm text-destructive">{errors.adultPriceAmount.message}</p>
                      )}
                      {/* Phase 6A.X: Revenue breakdown preview for adult price */}
                      <RevenueBreakdownPreview
                        ticketPrice={watch('adultPriceAmount') as number | undefined}
                        currency={(watch('adultPriceCurrency') as Currency | undefined) ?? Currency.USD}
                        state={watch('locationState') as string | undefined}
                        country={watch('locationCountry') as string | undefined}
                        priceLabel="adult ticket"
                      />
                    </div>

                    <div>
                      <label htmlFor="adultPriceCurrency" className="block text-sm font-medium text-neutral-700 mb-2">
                        Currency *
                      </label>
                      <select
                        id="adultPriceCurrency"
                        className={`w-full px-4 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 ${
                          errors.adultPriceCurrency ? 'border-destructive' : 'border-neutral-300'
                        }`}
                        defaultValue={Currency.USD}
                        {...register('adultPriceCurrency', { valueAsNumber: true })}
                      >
                        {currencyOptions.map(curr => (
                          <option key={curr.value} value={curr.value}>
                            {curr.label}
                          </option>
                        ))}
                      </select>
                      {errors.adultPriceCurrency && (
                        <p className="mt-1 text-sm text-destructive">{errors.adultPriceCurrency.message}</p>
                      )}
                    </div>
                  </div>

                  {/* Child Pricing Row */}
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div>
                      <label htmlFor="childPriceAmount" className="block text-sm font-medium text-neutral-700 mb-2">
                        Child Ticket Price *
                      </label>
                      <Input
                        id="childPriceAmount"
                        type="number"
                        min="0"
                        max="10000"
                        step="1"
                        placeholder="e.g., 15"
                        error={!!errors.childPriceAmount}
                        {...register('childPriceAmount', { valueAsNumber: true })}
                      />
                      {errors.childPriceAmount && (
                        <p className="mt-1 text-sm text-destructive">{errors.childPriceAmount.message}</p>
                      )}
                      {/* Phase 6A.X: Revenue breakdown preview for child price */}
                      <RevenueBreakdownPreview
                        ticketPrice={watch('childPriceAmount') as number | undefined}
                        currency={(watch('childPriceCurrency') as Currency | undefined) ?? Currency.USD}
                        state={watch('locationState') as string | undefined}
                        country={watch('locationCountry') as string | undefined}
                        priceLabel="child ticket"
                      />
                    </div>

                    <div>
                      <label htmlFor="childPriceCurrency" className="block text-sm font-medium text-neutral-700 mb-2">
                        Currency *
                      </label>
                      <select
                        id="childPriceCurrency"
                        className={`w-full px-4 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500 ${
                          errors.childPriceCurrency ? 'border-destructive' : 'border-neutral-300'
                        }`}
                        defaultValue={Currency.USD}
                        {...register('childPriceCurrency', { valueAsNumber: true })}
                      >
                        {currencyOptions.map(curr => (
                          <option key={curr.value} value={curr.value}>
                            {curr.label}
                          </option>
                        ))}
                      </select>
                      {errors.childPriceCurrency && (
                        <p className="mt-1 text-sm text-destructive">{errors.childPriceCurrency.message}</p>
                      )}
                    </div>

                    <div>
                      <label htmlFor="childAgeLimit" className="block text-sm font-medium text-neutral-700 mb-2">
                        Child Age Limit *
                      </label>
                      <Input
                        id="childAgeLimit"
                        type="number"
                        min="1"
                        max="18"
                        placeholder="12"
                        error={!!errors.childAgeLimit}
                        {...register('childAgeLimit', { valueAsNumber: true })}
                      />
                      {errors.childAgeLimit && (
                        <p className="mt-1 text-sm text-destructive">{errors.childAgeLimit.message}</p>
                      )}
                      <p className="mt-1 text-xs text-neutral-500">Age under which child pricing applies (1-18)</p>
                    </div>
                  </div>

                  {/* Helpful note */}
                  <div className="flex items-start gap-2 p-3 bg-blue-50 border border-blue-200 rounded-lg">
                    <svg className="w-5 h-5 text-blue-600 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                      <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
                    </svg>
                    <p className="text-xs text-blue-700">
                      Example: If child age limit is 12, attendees age 11 and under will be charged the child price, while attendees age 12 and over will be charged the adult price.
                    </p>
                  </div>
                </div>
              )}
            </div>
          )}
          </div>
        </CollapsibleSection>
      </div>

      {/* Phase 6A.32: Email Groups Section */}
      <div id="email" className="scroll-mt-20">
        <CollapsibleSection
          title="Email Groups (Optional)"
          description="Select email groups to notify about this event"
          icon={<Mail className="h-5 w-5" style={{ color: '#FF7900' }} />}
          open={sectionOpen.email}
          onOpenChange={(o) => setOpen('email', o)}
        >
          <div className="space-y-4">
          <MultiSelect
            options={emailGroups.map(group => ({
              id: group.id,
              label: group.name,
              disabled: !group.isActive
            }))}
            value={watch('emailGroupIds') || []}
            onChange={(ids) => setValue('emailGroupIds', ids)}
            placeholder="Select email groups to notify"
            isLoading={isLoadingEmailGroups}
            error={!!errors.emailGroupIds}
            errorMessage={errors.emailGroupIds?.message}
            helperText="Select groups that should receive invitations for this event"
          />
          </div>
        </CollapsibleSection>
      </div>

      {/* Organizer Contact Details (Multiple Contacts) */}
      <div id="organizer" className="scroll-mt-20">
        <CollapsibleSection
          title="Organizer Contacts (Optional)"
          description="Publish organizer contact information so attendees can reach you"
          icon={<Users className="h-5 w-5" style={{ color: '#FF7900' }} />}
          open={sectionOpen.organizer}
          onOpenChange={(o) => setOpen('organizer', o)}
        >
          <div className="space-y-4">
          {/* Publish Toggle Checkbox */}
          <div className="flex items-start space-x-3">
            <input
              type="checkbox"
              id="publishOrganizerContact"
              {...register('publishOrganizerContact')}
              className="mt-1 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
            />
            <label htmlFor="publishOrganizerContact" className="text-sm font-medium text-gray-700">
              Publish organizer contact information with this event
            </label>
          </div>

          {/* Show contact fields only when checkbox is checked */}
          {watch('publishOrganizerContact') && (
            <div className="ml-7 space-y-4">
              {contactFields.map((field, index) => {
                const contactLinkedUserId = watch(`organizerContacts.${index}.linkedUserId`);
                const contactIsPrimary = watch(`organizerContacts.${index}.isPrimary`);
                return (
                <div key={field.id} className={`p-4 border rounded-lg space-y-3 ${contactLinkedUserId ? 'border-green-300 bg-green-50' : 'border-gray-200 bg-gray-50'}`}>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-semibold text-gray-700">
                        {contactIsPrimary ? 'Primary Organizer' : `Contact ${index + 1}`}
                      </span>
                      {contactLinkedUserId && (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">
                          <Link2 className="h-3 w-3" /> Linked User
                        </span>
                      )}
                    </div>
                    <div className="flex items-center gap-2">
                      <button
                        type="button"
                        onClick={() => handleTogglePrimary(index)}
                        className={`flex items-center gap-1 text-xs font-medium px-2 py-1 rounded transition-colors ${
                          contactIsPrimary
                            ? 'bg-blue-100 text-blue-700 hover:bg-blue-200'
                            : 'bg-gray-100 text-gray-500 hover:bg-gray-200'
                        }`}
                        title={contactIsPrimary ? 'Remove primary status' : 'Set as primary organizer'}
                      >
                        <Star className={`h-3.5 w-3.5 ${contactIsPrimary ? 'fill-blue-500 text-blue-500' : ''}`} />
                        {contactIsPrimary ? 'Primary' : 'Set Primary'}
                      </button>
                      {index > 0 && (
                        <button
                          type="button"
                          onClick={() => removeContact(index)}
                          className="text-sm text-red-600 hover:text-red-800"
                        >
                          Remove
                        </button>
                      )}
                    </div>
                  </div>

                  {/* Contact Name */}
                  <div className="space-y-1">
                    <label className="block text-sm font-medium text-gray-700">
                      Name *
                    </label>
                    <Input
                      type="text"
                      placeholder="Full name"
                      error={!!errors.organizerContacts?.[index]?.contactName}
                      {...register(`organizerContacts.${index}.contactName`)}
                    />
                    {errors.organizerContacts?.[index]?.contactName && (
                      <p className="text-sm text-destructive">{errors.organizerContacts[index]?.contactName?.message}</p>
                    )}
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                    {/* Contact Email */}
                    <div className="space-y-1">
                      <label className="block text-sm font-medium text-gray-700">
                        Email
                      </label>
                      <Input
                        type="email"
                        placeholder="email@example.com"
                        error={!!errors.organizerContacts?.[index]?.contactEmail}
                        {...register(`organizerContacts.${index}.contactEmail`)}
                      />
                      {errors.organizerContacts?.[index]?.contactEmail && (
                        <p className="text-sm text-destructive">{errors.organizerContacts[index]?.contactEmail?.message}</p>
                      )}
                    </div>

                    {/* Contact Phone */}
                    <div className="space-y-1">
                      <label className="block text-sm font-medium text-gray-700">
                        Phone
                      </label>
                      <Input
                        type="tel"
                        placeholder="+1 (555) 123-4567"
                        error={!!errors.organizerContacts?.[index]?.contactPhone}
                        {...register(`organizerContacts.${index}.contactPhone`)}
                      />
                      {errors.organizerContacts?.[index]?.contactPhone && (
                        <p className="text-sm text-destructive">{errors.organizerContacts[index]?.contactPhone?.message}</p>
                      )}
                    </div>
                  </div>
                </div>
                );
              })}

              {/* Inline co-organizer search */}
              {showCoOrgSearch && (
                <CoOrganizerInlineSearch
                  onSelectUser={handleSearchUserSelected}
                  excludeUserIds={linkedUserIds}
                  onClose={() => setShowCoOrgSearch(false)}
                />
              )}

              {/* Add Contact Buttons (max 10) */}
              {contactFields.length < 10 && !showCoOrgSearch ? (
                <div className="flex flex-wrap gap-3">
                  <button
                    type="button"
                    onClick={() => appendContact({ contactName: '', contactEmail: '', contactPhone: '', isPrimary: false })}
                    className="flex items-center gap-2 text-sm text-blue-600 hover:text-blue-800 font-medium"
                  >
                    <span className="text-lg">+</span> Add Contact Manually
                  </button>
                  <button
                    type="button"
                    onClick={() => setShowCoOrgSearch(true)}
                    className="flex items-center gap-2 text-sm text-[#FF7900] hover:text-[#e06d00] font-medium"
                  >
                    <Users className="h-4 w-4" /> Search & Add LankaConnect User
                  </button>
                </div>
              ) : contactFields.length >= 10 ? (
                <p className="text-sm text-neutral-500">Maximum 10 organizer contacts reached</p>
              ) : null}

              {/* Validation Error */}
              {errors.organizerContacts && !Array.isArray(errors.organizerContacts) && (
                <p className="text-sm text-destructive">{(errors.organizerContacts as any).message}</p>
              )}

              <p className="text-sm text-gray-600">
                Each contact requires a name and at least one contact method (email or phone)
              </p>
            </div>
          )}
          </div>
        </CollapsibleSection>
      </div>

      {/* Donation Feature: Donation Configuration */}
      <div id="donations" className="scroll-mt-20">
        <CollapsibleSection
          title="Donations (Optional)"
          description="Allow attendees and visitors to donate to support your event"
          icon={<Heart className="h-5 w-5" style={{ color: '#FF7900' }} />}
          open={sectionOpen.donations}
          onOpenChange={(o) => setOpen('donations', o)}
        >
          <DonationConfigForm
            isEnabled={donationsEnabled}
            onEnabledChange={setDonationsEnabled}
            suggestedAmounts={donationSuggestedAmounts}
            onSuggestedAmountsChange={setDonationSuggestedAmounts}
            allowCustomAmount={donationAllowCustom}
            onAllowCustomAmountChange={setDonationAllowCustom}
            minAmount={donationMinAmount}
            onMinAmountChange={setDonationMinAmount}
            maxAmount={donationMaxAmount}
            onMaxAmountChange={setDonationMaxAmount}
            donationMessage={donationMessage}
            onDonationMessageChange={setDonationMessage}
            showDonationSummary={showDonationSummary}
            onShowDonationSummaryChange={setShowDonationSummary}
          />
        </CollapsibleSection>
      </div>

      {/* Collection (Event Fund) Configuration */}
      <div id="collections" className="scroll-mt-20">
        <CollapsibleSection
          title="Event Fund / Collections (Optional)"
          description="Set up a fundraising collection to gather contributions for your event"
          icon={<Wallet className="h-5 w-5" style={{ color: '#FF7900' }} />}
          open={sectionOpen.collections}
          onOpenChange={(o) => setOpen('collections', o)}
        >
          <CollectionConfigForm
            isEnabled={collectionsEnabled}
            onEnabledChange={setCollectionsEnabled}
            goalAmount={collectionGoalAmount}
            onGoalAmountChange={setCollectionGoalAmount}
            showProgress={collectionShowProgress}
            onShowProgressChange={setCollectionShowProgress}
            suggestedAmounts={collectionSuggestedAmounts}
            onSuggestedAmountsChange={setCollectionSuggestedAmounts}
            allowCustomAmount={collectionAllowCustom}
            onAllowCustomAmountChange={setCollectionAllowCustom}
            minAmount={collectionMinAmount}
            onMinAmountChange={setCollectionMinAmount}
            maxAmount={collectionMaxAmount}
            onMaxAmountChange={setCollectionMaxAmount}
            collectionMessage={collectionMessage}
            onCollectionMessageChange={setCollectionMessage}
            showContributorCount={showContributorCount}
            onShowContributorCountChange={setShowContributorCount}
          />
        </CollapsibleSection>
      </div>

      {/* Sponsor Configuration */}
      <div id="sponsors" className="scroll-mt-20">
        <CollapsibleSection
          title="Sponsorships (Optional)"
          description="Allow individuals and organizations to sponsor your event with monetary or item contributions"
          icon={<HandCoins className="h-5 w-5" style={{ color: '#FF7900' }} />}
          open={sectionOpen.sponsors}
          onOpenChange={(o) => setOpen('sponsors', o)}
        >
          <SponsorConfigForm
            isEnabled={sponsorsEnabled}
            onEnabledChange={setSponsorsEnabled}
            acceptMoneySponsors={acceptMoneySponsors}
            onAcceptMoneySponsorsChange={setAcceptMoneySponsors}
            acceptItemSponsors={acceptItemSponsors}
            onAcceptItemSponsorsChange={setAcceptItemSponsors}
            minSponsorAmount={minSponsorAmount}
            onMinSponsorAmountChange={setMinSponsorAmount}
            sponsorMessage={sponsorMessage}
            onSponsorMessageChange={setSponsorMessage}
            showSponsorList={showSponsorList}
            onShowSponsorListChange={setShowSponsorList}
          />
        </CollapsibleSection>
      </div>

      {/* Add-On Configuration */}
      <div id="addons" className="scroll-mt-20">
        <CollapsibleSection
          title="Add-Ons (Optional)"
          description="Offer additional items or services that attendees can purchase alongside their registration"
          icon={<PackagePlus className="h-5 w-5" style={{ color: '#FF7900' }} />}
          open={sectionOpen.addons}
          onOpenChange={(o) => setOpen('addons', o)}
        >
          <AddOnConfigForm
            isEnabled={addOnsEnabled}
            onEnabledChange={setAddOnsEnabled}
            availableDuringRegistration={addOnAvailableDuringRegistration}
            onAvailableDuringRegistrationChange={setAddOnAvailableDuringRegistration}
            availableStandalone={addOnAvailableStandalone}
            onAvailableStandaloneChange={setAddOnAvailableStandalone}
            addOnMessage={addOnMessage}
            onAddOnMessageChange={setAddOnMessage}
            pendingDefinitions={pendingAddOnDefinitions}
            onPendingDefinitionsChange={setPendingAddOnDefinitions}
          />
        </CollapsibleSection>
      </div>

      {/* Note about Media */}
      <Card>
        <CardContent className="py-6">
          <div className="flex items-start gap-3 p-4 bg-blue-50 border border-blue-200 rounded-lg">
            <div className="flex-shrink-0">
              <svg className="w-5 h-5 text-blue-600 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
              </svg>
            </div>
            <div className="flex-1">
              <h4 className="text-sm font-semibold text-blue-900 mb-1">
                📸 Upload Images & Videos After Creation
              </h4>
              <p className="text-sm text-blue-700">
                You'll be able to add event images and videos from the event detail page after creating your event.
                This helps make your event more attractive to attendees!
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Error Message */}
      {submitError && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
          <p className="text-sm text-red-600">{submitError}</p>
        </div>
      )}

      {/* Session 33: Validation Error Summary - Shows when form has validation errors */}
      {hasErrors && !submitError && (
        <div className="p-4 bg-amber-50 border border-amber-200 rounded-lg">
          <div className="flex items-start gap-2">
            <svg className="w-5 h-5 text-amber-600 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
            </svg>
            <div>
              <p className="text-sm font-medium text-amber-800">Please fix the following errors:</p>
              <ul className="mt-1 text-sm text-amber-700 list-disc list-inside">
                {Object.entries(errors).map(([field, error]) => {
                  const targetSection = FIELD_TO_SECTION[field];
                  return (
                    <li key={field}>
                      {targetSection ? (
                        <button
                          type="button"
                          onClick={() => openSection(targetSection)}
                          className="text-left underline decoration-dotted hover:text-amber-900 hover:decoration-solid focus:outline-none focus:ring-2 focus:ring-amber-400 rounded"
                        >
                          {field}: {(error as any)?.message || 'Invalid'}
                        </button>
                      ) : (
                        <span>{field}: {(error as any)?.message || 'Invalid'}</span>
                      )}
                    </li>
                  );
                })}
              </ul>
            </div>
          </div>
        </div>
      )}

      {/* Form Actions */}
      <div className="flex items-center justify-end gap-4">
        <Button
          type="button"
          variant="outline"
          onClick={() => router.push('/dashboard')}
          disabled={isSubmitting}
        >
          Cancel
        </Button>
        <Button
          type="submit"
          disabled={isSubmitting || createEventMutation.isPending}
          style={{ background: '#FF7900' }}
          className="min-w-[150px]"
        >
          {isSubmitting || createEventMutation.isPending ? 'Creating...' : 'Create Event'}
        </Button>
      </div>
    </form>
  );
}
