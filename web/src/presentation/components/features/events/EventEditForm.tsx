'use client';

import { useForm, useFieldArray, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useRouter } from 'next/navigation';
import { useState, useEffect, useCallback, useMemo } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Calendar, MapPin, Users, DollarSign, FileText, Tag, X, Mail, Link2, Star } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { MultiSelect } from '@/presentation/components/ui/MultiSelect';
import { editEventSchema, type EditEventFormData } from '@/presentation/lib/validators/event.schemas';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { EventCategory, Currency, RegistrationMode, type EventDto } from '@/infrastructure/api/types/events.types';
import { RegistrationModePicker } from './RegistrationModePicker';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import { useEmailGroups } from '@/presentation/hooks/useEmailGroups';
import { geocodeAddress } from '@/presentation/lib/utils/geocoding';
import { eventKeys } from '@/presentation/hooks/useEvents';
import { useEventCategories, useCurrencies } from '@/infrastructure/api/hooks/useReferenceData';
import { useContentImageUpload } from '@/presentation/hooks/useContentImageUpload';
import { buildCodeToIntMap, toDropdownOptions } from '@/infrastructure/api/utils/enum-mappers';
import { RichTextEditor } from '@/presentation/components/ui/RichTextEditor';
import { RevenueBreakdownPreview } from './RevenueBreakdownPreview';
import { TicketTierBuilder, type TicketTierFormData } from './TicketTierBuilder';
import { SeatingSection } from './SeatingSection';
import { TicketingMode, SeatingMode } from '@/infrastructure/api/types/events.types';
import { DonationConfigForm } from './DonationConfigForm';
import { CollectionConfigForm } from './CollectionConfigForm';
import { SponsorConfigForm } from './SponsorConfigForm';
import { AddOnConfigForm } from './AddOnConfigForm';
import { CoOrganizerInlineSearch } from './CoOrganizerInlineSearch';
import { SecondaryLocationFieldset } from './SecondaryLocationFieldset';
import type { UserSearchResultDto } from '@/infrastructure/api/types/events.types';

interface EventEditFormProps {
  event: EventDto;
}

/**
 * Event Edit Form Component
 * Allows organizers to edit existing events
 *
 * Features:
 * - Pre-filled form with existing event data
 * - Basic Information: Title, Description, Category
 * - Date & Time: Start and End dates
 * - Location: Full address details (optional)
 * - Capacity: Max attendees
 * - Pricing: Free or paid events with currency selection
 * - Validation: Zod schema with cross-field validation
 */
export function EventEditForm({ event }: EventEditFormProps) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { user } = useAuthStore();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Donation Feature: Donation configuration state (pre-filled from event)
  const [donationsEnabled, setDonationsEnabled] = useState(event.donationConfig?.isEnabled ?? false);
  const [donationSuggestedAmounts, setDonationSuggestedAmounts] = useState<number[]>(event.donationConfig?.suggestedAmounts ?? []);
  const [donationAllowCustom, setDonationAllowCustom] = useState(event.donationConfig?.allowCustomAmount ?? true);
  const [donationMinAmount, setDonationMinAmount] = useState<number | null>(event.donationConfig?.minAmount ?? null);
  const [donationMaxAmount, setDonationMaxAmount] = useState<number | null>(event.donationConfig?.maxAmount ?? null);
  const [donationMessage, setDonationMessage] = useState(event.donationConfig?.donationMessage ?? '');
  const [showDonationSummary, setShowDonationSummary] = useState(event.donationConfig?.showDonationSummary ?? false);

  // Collection (Event Fund) configuration state (pre-filled from event)
  const [collectionsEnabled, setCollectionsEnabled] = useState(event.collectionConfig?.isEnabled ?? false);
  const [collectionGoalAmount, setCollectionGoalAmount] = useState<number | null>(event.collectionConfig?.goalAmount ?? null);
  const [collectionShowProgress, setCollectionShowProgress] = useState(event.collectionConfig?.showProgress ?? false);
  const [collectionSuggestedAmounts, setCollectionSuggestedAmounts] = useState<number[]>(event.collectionConfig?.suggestedAmounts ?? []);
  const [collectionAllowCustom, setCollectionAllowCustom] = useState(event.collectionConfig?.allowCustomAmount ?? true);
  const [collectionMinAmount, setCollectionMinAmount] = useState<number | null>(event.collectionConfig?.minAmount ?? null);
  const [collectionMaxAmount, setCollectionMaxAmount] = useState<number | null>(event.collectionConfig?.maxAmount ?? null);
  const [collectionMessage, setCollectionMessage] = useState(event.collectionConfig?.collectionMessage ?? '');
  const [showContributorCount, setShowContributorCount] = useState(event.collectionConfig?.showContributorCount ?? false);

  // Sponsor configuration state (pre-filled from event)
  const [sponsorsEnabled, setSponsorsEnabled] = useState(event.sponsorConfig?.isEnabled ?? false);
  const [acceptMoneySponsors, setAcceptMoneySponsors] = useState(event.sponsorConfig?.acceptMoneySponsors ?? true);
  const [acceptItemSponsors, setAcceptItemSponsors] = useState(event.sponsorConfig?.acceptItemSponsors ?? true);
  const [minSponsorAmount, setMinSponsorAmount] = useState<number | null>(event.sponsorConfig?.minSponsorAmount ?? null);
  const [sponsorMessage, setSponsorMessage] = useState(event.sponsorConfig?.sponsorMessage ?? '');
  const [showSponsorList, setShowSponsorList] = useState(event.sponsorConfig?.showSponsorList ?? false);

  // Add-On configuration state (pre-filled from event)
  const [addOnsEnabled, setAddOnsEnabled] = useState(event.addOnConfig?.isEnabled ?? false);
  const [addOnAvailableDuringRegistration, setAddOnAvailableDuringRegistration] = useState(event.addOnConfig?.availableDuringRegistration ?? true);
  const [addOnAvailableStandalone, setAddOnAvailableStandalone] = useState(event.addOnConfig?.availableStandalone ?? true);
  const [addOnMessage, setAddOnMessage] = useState(event.addOnConfig?.addOnMessage ?? '');

  // Phase 6A.32: Fetch email groups for selection
  const { data: emailGroups = [], isLoading: isLoadingEmailGroups } = useEmailGroups();

  // Phase 6A.106 Part 3: Azure image upload for rich text editor
  const { mutateAsync: uploadImage } = useContentImageUpload();

  // Phase 6A.47: Fetch EventCategory and Currency reference data from API
  const { data: categories } = useEventCategories();
  const { data: currencies } = useCurrencies();

  // Convert string enum to number (backend returns enums as strings due to JsonStringEnumConverter)
  // Wrapped in useCallback to prevent infinite re-renders in useEffect
  const convertCategoryToNumber = useCallback((category: any): number => {
    // If it's already a number, return it
    if (typeof category === 'number') return category;

    // If it's a string, map it to the enum value using reference data
    const categoryMap = buildCodeToIntMap<EventCategory>(categories);
    return categoryMap[category] ?? EventCategory.Community;
  }, [categories]);

  // Session 33 Fix: Convert currency string/number to Currency enum value
  // Backend may return "USD" (string) or 1 (number) depending on serialization
  const convertCurrencyToNumber = useCallback((currency: any): Currency => {
    // If it's already a valid Currency enum number, return it
    if (typeof currency === 'number' && currency >= 0 && currency <= 5) {
      return currency as Currency;
    }

    // If it's a string, map it to the enum value using reference data
    if (typeof currency === 'string') {
      const currencyMap = buildCodeToIntMap<Currency>(currencies);
      return currencyMap[currency] ?? Currency.USD;
    }

    // Default to USD
    return Currency.USD;
  }, [currencies]);

  // Format dates for datetime-local input
  const formatDateForInput = (dateString: string | Date) => {
    const date = new Date(dateString);
    // Convert to local timezone and format as YYYY-MM-DDTHH:mm
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  };

  const {
    register,
    handleSubmit,
    watch,
    reset,
    setValue,
    control,
    formState: { errors },
  } = useForm<EditEventFormData>({
    resolver: zodResolver(editEventSchema) as any,
    defaultValues: {
      title: event.title,
      description: event.description,
      category: convertCategoryToNumber(event.category),
      startDate: formatDateForInput(event.startDate),
      endDate: formatDateForInput(event.endDate),
      capacity: event.capacity,
      // Issue #51: Max attendees per registration
      maxAttendeesPerRegistration: event.maxAttendeesPerRegistration || 10,
      isFree: event.isFree ?? true,
      // Phase 7E.5: Per-event registration capture mode (defensive default per architect §6 —
      // tolerate stale React Query payloads that pre-date the registrationMode field).
      registrationMode: event.registrationMode ?? RegistrationMode.DetailedAttendees,
      // Pricing mode toggles
      enableDualPricing: event.hasDualPricing ?? false,
      enableGroupPricing: event.hasGroupPricing ?? false,
      enableTieredTicketing: event.ticketingMode === TicketingMode.Tiered,
      ticketTiers: [],
      // Single pricing - undefined by default, populated by reset() if applicable
      ticketPriceAmount: undefined,
      ticketPriceCurrency: undefined,
      // Dual pricing - undefined by default, populated by reset() if applicable
      adultPriceAmount: undefined,
      adultPriceCurrency: undefined,
      childPriceAmount: undefined,
      childPriceCurrency: undefined,
      childAgeLimit: undefined,
      // Group pricing - populated by reset() if applicable
      groupPricingTiers: [],
      // Location
      locationAddress: event.address || undefined,
      locationCity: event.city || undefined,
      locationState: event.state || undefined,
      locationZipCode: event.zipCode || undefined,
      locationCountry: event.country || undefined,
      // Phase 7C.1: Primary venue name + secondary location
      locationName: event.locationName || undefined,
      secondaryLocationType: event.secondaryLocationType ?? null,
      secondaryLocationName: event.secondaryLocationName || undefined,
      secondaryLocationAddress: event.secondaryAddress || undefined,
      secondaryLocationCity: event.secondaryCity || undefined,
      secondaryLocationState: event.secondaryState || undefined,
      secondaryLocationZipCode: event.secondaryZipCode || undefined,
      secondaryLocationCountry: event.secondaryCountry || undefined,
      // Phase 6A.32: Email Groups Integration
      emailGroupIds: event.emailGroupIds || [],
      // Phase 6A.X: Event Organizer Contact Details (multiple contacts)
      publishOrganizerContact: event.publishOrganizerContact ?? false,
      organizerContacts: event.organizerContacts?.map((c: any) => ({
        contactName: c.contactName || '',
        contactEmail: c.contactEmail || '',
        contactPhone: c.contactPhone || '',
        isPrimary: c.isPrimary || false,
      })) || [],
    },
  });

  // Reset form ONLY when event ID changes (prevents infinite re-renders)
  // We don't want to reset when user is typing!
  useEffect(() => {
    const categoryNumber = convertCategoryToNumber(event.category);

    // Session 33 Fix: Convert backend currency values to proper enum numbers
    const ticketCurrency = event.ticketPriceCurrency ? convertCurrencyToNumber(event.ticketPriceCurrency) : Currency.USD;
    const adultCurrency = event.adultPriceCurrency ? convertCurrencyToNumber(event.adultPriceCurrency) : Currency.USD;
    const childCurrency = event.childPriceCurrency ? convertCurrencyToNumber(event.childPriceCurrency) : Currency.USD;

    console.log('🔄 Resetting form with event data:', {
      eventId: event.id,
      category: event.category,
      categoryType: typeof event.category,
      categoryNumber,
      isFree: event.isFree,
      // Session 33: Debug pricing mode loading
      hasDualPricing: event.hasDualPricing,
      hasGroupPricing: event.hasGroupPricing,
      adultPriceAmount: event.adultPriceAmount,
      adultPriceCurrency: event.adultPriceCurrency,
      adultCurrencyConverted: adultCurrency,
      childPriceAmount: event.childPriceAmount,
      childPriceCurrency: event.childPriceCurrency,
      childCurrencyConverted: childCurrency,
      childAgeLimit: event.childAgeLimit,
      ticketPriceAmount: event.ticketPriceAmount,
      ticketPriceCurrency: event.ticketPriceCurrency,
      ticketCurrencyConverted: ticketCurrency,
      // Phase 6A.32: Email groups debug
      emailGroupIds: event.emailGroupIds,
      emailGroupsCount: event.emailGroupIds?.length || 0,
    });

    // Session 33: Properly load existing pricing data including dual pricing
    // Determine pricing mode to set correct defaults
    // Phase 8 Fix: Tiered mode takes precedence — when ticketingMode is Tiered,
    // disable dual/group pricing even if their flags are still true in the DB
    // (the old pricing data remains until the event is saved in the new mode)
    const isTieredMode = event.ticketingMode === TicketingMode.Tiered;
    const hasDualPricing = !isTieredMode && (event.hasDualPricing ?? false);
    const hasGroupPricing = !isTieredMode && (event.hasGroupPricing ?? false);
    const hasSinglePricing = !event.isFree && !hasDualPricing && !hasGroupPricing && !isTieredMode;

    reset({
      title: event.title,
      description: event.description,
      category: categoryNumber,
      startDate: formatDateForInput(event.startDate),
      endDate: formatDateForInput(event.endDate),
      capacity: event.capacity,
      // Issue #51: Max attendees per registration
      maxAttendeesPerRegistration: event.maxAttendeesPerRegistration || 10,
      isFree: event.isFree,
      // Session 33 Fix: Load pricing data with PROPERLY CONVERTED currency values
      // Single pricing - only set if in single pricing mode
      ticketPriceAmount: hasSinglePricing ? (event.ticketPriceAmount ?? undefined) : undefined,
      ticketPriceCurrency: hasSinglePricing ? ticketCurrency : undefined,
      // Dual pricing - only set if in dual pricing mode
      enableDualPricing: hasDualPricing,
      adultPriceAmount: hasDualPricing ? (event.adultPriceAmount ?? undefined) : undefined,
      adultPriceCurrency: hasDualPricing ? adultCurrency : undefined,
      childPriceAmount: hasDualPricing ? (event.childPriceAmount ?? undefined) : undefined,
      childPriceCurrency: hasDualPricing ? childCurrency : undefined,
      childAgeLimit: hasDualPricing ? (event.childAgeLimit ?? undefined) : undefined,
      // Phase 8: Tiered ticketing
      enableTieredTicketing: event.ticketingMode === TicketingMode.Tiered,
      ticketTiers: event.ticketingMode === TicketingMode.Tiered && event.ticketTiers
        ? event.ticketTiers.map(tier => ({
            id: tier.id,
            name: tier.name,
            description: tier.description || '',
            adultPriceAmount: tier.adultPriceAmount,
            adultPriceCurrency: convertCurrencyToNumber(tier.adultPriceCurrency),
            childPriceAmount: tier.childPriceAmount ?? null,
            childPriceCurrency: tier.childPriceCurrency ? convertCurrencyToNumber(tier.childPriceCurrency) : null,
            childAgeLimit: tier.childAgeLimit ?? null,
            capacity: tier.capacity,
            maxPerUser: tier.maxPerUser,
            sortOrder: tier.sortOrder,
            isFree: tier.isFree,
          }))
        : [],
      // Group pricing - Session 44: Convert currency values from string to number
      enableGroupPricing: hasGroupPricing,
      groupPricingTiers: hasGroupPricing && event.groupPricingTiers
        ? event.groupPricingTiers.map(tier => ({
            ...tier,
            currency: convertCurrencyToNumber(tier.currency),
          }))
        : undefined,
      // Location
      locationAddress: event.address || undefined,
      locationCity: event.city || undefined,
      locationState: event.state || undefined,
      locationZipCode: event.zipCode || undefined,
      locationCountry: event.country || undefined,
      // Phase 7C.1: Primary venue name + secondary location
      locationName: event.locationName || undefined,
      secondaryLocationType: event.secondaryLocationType ?? null,
      secondaryLocationName: event.secondaryLocationName || undefined,
      secondaryLocationAddress: event.secondaryAddress || undefined,
      secondaryLocationCity: event.secondaryCity || undefined,
      secondaryLocationState: event.secondaryState || undefined,
      secondaryLocationZipCode: event.secondaryZipCode || undefined,
      secondaryLocationCountry: event.secondaryCountry || undefined,
      // Phase 6A.32: Email Groups Integration
      emailGroupIds: event.emailGroupIds || [],
      // Phase 6A.X: Event Organizer Contact Details (multiple contacts)
      publishOrganizerContact: event.publishOrganizerContact ?? false,
      organizerContacts: event.organizerContacts?.map((c: any) => ({
        contactName: c.contactName || '',
        contactEmail: c.contactEmail || '',
        contactPhone: c.contactPhone || '',
        isPrimary: c.isPrimary || false,
        linkedUserId: c.linkedUserId || null,
      })) || [],
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [event.id]); // Only reset when navigating to different event

  const isFree = watch('isFree');
  const enableDualPricing = watch('enableDualPricing');
  const enableGroupPricing = watch('enableGroupPricing');
  const enableTieredTicketing = watch('enableTieredTicketing');
  const ticketTiers = (watch('ticketTiers') || []) as TicketTierFormData[];
  const publishOrganizerContact = watch('publishOrganizerContact');

  // Seating Redesign Slice 1: local controlled state for assigned seating toggle.
  // Persisted server-side on submit via eventsRepository.setSeatingMode(...).
  const [seatingMode, setSeatingModeState] = useState<SeatingMode>(
    event.seatingMode ?? SeatingMode.GeneralAdmission
  );
  const [seatingModeError, setSeatingModeError] = useState<string | null>(null);

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

  // Session 33: Use useFieldArray for dynamic group pricing tiers management
  const { fields, append, remove } = useFieldArray({
    control,
    name: 'groupPricingTiers',
  });

  // Dynamic organizer contacts field array (matches EventCreationForm pattern)
  const { fields: contactFields, append: appendContact, remove: removeContact } = useFieldArray({
    control,
    name: 'organizerContacts',
  });

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

  const onSubmit = handleSubmit(async (data: EditEventFormData) => {
    if (!user?.userId) {
      setSubmitError('You must be logged in to edit events');
      return;
    }

    if (event.isCurrentUserOrganizer !== true) {
      setSubmitError('You can only edit your own events');
      return;
    }

    try {
      setIsSubmitting(true);
      setSubmitError(null);

      console.log('📋 Form Submission - Updating Event:', {
        eventId: event.id,
        userId: user.userId,
        userRole: user.role,
      });

      // Prepare event data for backend
      // UpdateEventRequest matches backend contract (excludes organizerId)
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

      // Convert datetime-local format to ISO 8601
      const startDateISO = new Date(data.startDate).toISOString();
      const endDateISO = new Date(data.endDate).toISOString();

      // Session 33: Determine pricing mode and build appropriate pricing fields
      const isDualPricing = !data.isFree && data.enableDualPricing;
      const isGroupPricing = !data.isFree && data.enableGroupPricing;
      const isTieredTicketing = !data.isFree && data.enableTieredTicketing;
      const isSinglePricing = !data.isFree && !data.enableDualPricing && !data.enableGroupPricing && !data.enableTieredTicketing;

      const eventData = {
        eventId: event.id,
        title: data.title,
        description: data.description,
        startDate: startDateISO,
        endDate: endDateISO,
        capacity: data.capacity,
        // Issue #51: Max attendees per registration
        maxAttendeesPerRegistration: data.maxAttendeesPerRegistration,
        category: data.category,
        // Phase 6A.32: Email Groups Integration
        emailGroupIds: data.emailGroupIds || [],
        // IsFreeEvent fix: Send explicit free event flag to backend
        isFree: data.isFree ?? false,
        // Phase 7E.5: Send registration mode if changed. Backend rejects mode change once
        // registrations exist (Event.SetRegistrationMode guard) — surfaces as 400.
        ...(data.registrationMode &&
          data.registrationMode !== event.registrationMode && {
            registrationMode: data.registrationMode,
          }),
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
        // Backend expects: LocationAddress, LocationCity, LocationState, LocationZipCode, LocationCountry
        // CRITICAL: Use null for empty optional fields, NOT empty strings
        ...(hasCompleteLocation && {
          locationAddress: data.locationAddress,
          locationCity: data.locationCity,
          locationState: data.locationState || null,
          locationZipCode: data.locationZipCode || null,
          locationCountry: data.locationCountry || null,
          locationLatitude: locationLatitude ?? null,
          locationLongitude: locationLongitude ?? null,
        }),
        // Phase 7C.1: Primary venue name (null clears)
        locationName: data.locationName?.trim() ? data.locationName.trim() : null,
        // Phase 7C.1: Secondary location (null type clears entire secondary location)
        secondaryLocationType: data.secondaryLocationType ?? null,
        ...(data.secondaryLocationType && {
          secondaryLocationName: data.secondaryLocationName?.trim() || null,
          secondaryLocationAddress: data.secondaryLocationAddress || null,
          secondaryLocationCity: data.secondaryLocationCity || null,
          secondaryLocationState: data.secondaryLocationState || null,
          secondaryLocationZipCode: data.secondaryLocationZipCode || null,
          secondaryLocationCountry: data.secondaryLocationCountry || null,
        }),
        // Session 33: Pricing fields - send appropriate fields based on pricing mode
        // Single pricing mode
        ticketPriceAmount: isSinglePricing ? data.ticketPriceAmount : null,
        ticketPriceCurrency: isSinglePricing ? data.ticketPriceCurrency : null,
        // Dual pricing mode (adult/child)
        adultPriceAmount: isDualPricing ? data.adultPriceAmount : null,
        adultPriceCurrency: isDualPricing ? data.adultPriceCurrency : null,
        childPriceAmount: isDualPricing ? data.childPriceAmount : null,
        childPriceCurrency: isDualPricing ? data.childPriceCurrency : null,
        childAgeLimit: isDualPricing ? data.childAgeLimit : null,
        // Session 33: Group pricing mode - use form data directly
        ...(isGroupPricing && data.groupPricingTiers && data.groupPricingTiers.length > 0 && {
          groupPricingTiers: data.groupPricingTiers.map((tier) => ({
            minAttendees: tier.minAttendees,
            maxAttendees: tier.maxAttendees ?? null,
            pricePerPerson: tier.pricePerPerson,
            currency: tier.currency,
          })),
        }),
        // Phase 8: Tiered ticketing mode
        ...(isTieredTicketing && {
          ticketingMode: 'Tiered' as const,
          ticketTiers: (data.ticketTiers as TicketTierFormData[] | undefined)?.map((tier) => ({
            id: tier.id || undefined,
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
        }),
      };

      console.log('📤 Updating event with payload:', JSON.stringify(eventData, null, 2));
      console.log('📋 Event details before update:', {
        eventId: event.id,
        eventStatus: event.status,
        isFree: data.isFree,
        pricingMode: isDualPricing ? 'dual' : isSinglePricing ? 'single' : 'free',
        // Single pricing
        ticketPriceAmount: data.ticketPriceAmount,
        // Dual pricing
        enableDualPricing: data.enableDualPricing,
        adultPriceAmount: data.adultPriceAmount,
        childPriceAmount: data.childPriceAmount,
        childAgeLimit: data.childAgeLimit,
      });

      console.log('🌐 API Request Details:', {
        url: `/events/${event.id}`,
        method: 'PUT',
        payloadSize: JSON.stringify(eventData).length,
        payloadKeys: Object.keys(eventData),
      });

      await eventsRepository.updateEvent(event.id, eventData);
      console.log('✅ Event updated successfully!');

      // Post-update: Save financial config forms via separate API calls
      // ALWAYS send all 3 (unlike Create) to allow disabling previously-enabled configs
      try {
        await Promise.all([
          eventsRepository.updateCollectionConfig(event.id, {
            isEnabled: collectionsEnabled,
            goalAmount: collectionsEnabled ? collectionGoalAmount : null,
            showProgress: collectionsEnabled ? collectionShowProgress : false,
            suggestedAmounts: collectionsEnabled && collectionSuggestedAmounts.length > 0 ? collectionSuggestedAmounts : null,
            allowCustomAmount: collectionsEnabled ? collectionAllowCustom : false,
            minAmount: collectionsEnabled ? collectionMinAmount : null,
            maxAmount: collectionsEnabled ? collectionMaxAmount : null,
            collectionMessage: collectionsEnabled ? (collectionMessage || null) : null,
            showContributorCount: collectionsEnabled ? showContributorCount : false,
          }),
          eventsRepository.updateSponsorConfig(event.id, {
            isEnabled: sponsorsEnabled,
            acceptMoneySponsors: sponsorsEnabled ? acceptMoneySponsors : false,
            acceptItemSponsors: sponsorsEnabled ? acceptItemSponsors : false,
            minSponsorAmount: sponsorsEnabled ? minSponsorAmount : null,
            sponsorMessage: sponsorsEnabled ? (sponsorMessage || null) : null,
            showSponsorList: sponsorsEnabled ? showSponsorList : false,
          }),
          eventsRepository.updateAddOnConfig(event.id, {
            isEnabled: addOnsEnabled,
            availableDuringRegistration: addOnsEnabled ? addOnAvailableDuringRegistration : false,
            availableStandalone: addOnsEnabled ? addOnAvailableStandalone : false,
            addOnMessage: addOnsEnabled ? (addOnMessage || null) : null,
          }),
        ]);
        console.log('✅ Financial config forms saved successfully');
      } catch (configErr) {
        console.error('⚠️ Some financial configs failed to save:', configErr);
        // Continue to cache invalidation and redirect — main event data was saved
      }

      // Phase 8 Fix: Sync ticket tiers via dedicated CRUD endpoints
      // The main updateEvent() call does NOT handle tier data — tiers are managed
      // via separate API endpoints (setTicketingMode, addTicketTier, updateTicketTier, removeTicketTier).
      try {
        const wasAlreadyTiered = event.ticketingMode === TicketingMode.Tiered;
        const formTiers = (data.ticketTiers as TicketTierFormData[] | undefined) || [];

        if (isTieredTicketing) {
          // Step 1: Set ticketing mode to Tiered (if not already)
          if (!wasAlreadyTiered) {
            await eventsRepository.setTicketingMode(event.id, TicketingMode.Tiered);
            console.log('✅ Ticketing mode set to Tiered');
          }

          // Step 2: Determine which tiers to add, update, or remove
          const existingTierIds = new Set(
            (event.ticketTiers || []).map(t => t.id)
          );
          const formTierIds = new Set(
            formTiers.filter(t => t.id).map(t => t.id!)
          );

          // Tiers to remove: exist on server but not in form
          const tiersToRemove = (event.ticketTiers || []).filter(t => !formTierIds.has(t.id));
          // Tiers to update: exist on server AND in form (have id)
          const tiersToUpdate = formTiers.filter(t => t.id && existingTierIds.has(t.id));
          // Tiers to add: no id, or id not found on server
          const tiersToAdd = formTiers.filter(t => !t.id || !existingTierIds.has(t.id));

          // Execute removals first, then updates and adds in parallel
          for (const tier of tiersToRemove) {
            try {
              await eventsRepository.removeTicketTier(event.id, tier.id);
              console.log(`✅ Removed tier: ${tier.name}`);
            } catch (removeErr) {
              console.error(`⚠️ Failed to remove tier ${tier.name}:`, removeErr);
            }
          }

          const tierPromises: Promise<void | string>[] = [];

          for (const tier of tiersToUpdate) {
            tierPromises.push(
              eventsRepository.updateTicketTier(event.id, tier.id!, {
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
            );
          }

          for (const tier of tiersToAdd) {
            tierPromises.push(
              eventsRepository.addTicketTier(event.id, {
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
            );
          }

          if (tierPromises.length > 0) {
            await Promise.all(tierPromises);
            console.log(`✅ Tier sync complete: ${tiersToUpdate.length} updated, ${tiersToAdd.length} added, ${tiersToRemove.length} removed`);
          }
        } else if (wasAlreadyTiered) {
          // User disabled tiered ticketing — revert to SingleTier
          await eventsRepository.setTicketingMode(event.id, TicketingMode.SingleTier);
          console.log('✅ Ticketing mode reverted to SingleTier');
        }
      } catch (tierErr) {
        console.error('⚠️ Tier sync failed:', tierErr);
        // Non-blocking — main event data was saved, tiers can be managed from manage page
      }

      // Seating Redesign Slice 1: Persist assigned-seating toggle.
      // Runs AFTER setTicketingMode so the domain sees TicketingMode.Tiered
      // before accepting AssignedSeating. Reverts to GA when the user
      // disables tiered ticketing.
      try {
        const previousSeatingMode = event.seatingMode ?? SeatingMode.GeneralAdmission;
        const effectiveDesiredMode = isTieredTicketing
          ? seatingMode
          : SeatingMode.GeneralAdmission;

        if (effectiveDesiredMode !== previousSeatingMode) {
          console.log(
            `🪑 Seating mode change: ${previousSeatingMode} → ${effectiveDesiredMode}`
          );
          await eventsRepository.setSeatingMode(event.id, effectiveDesiredMode);
          console.log(`✅ Seating mode updated to ${effectiveDesiredMode}`);
          if (effectiveDesiredMode !== seatingMode) {
            setSeatingModeState(effectiveDesiredMode);
          }
        }
      } catch (seatErr) {
        const message = seatErr instanceof Error
          ? seatErr.message
          : 'Failed to update seating mode.';
        console.error('⚠️ Seating mode update failed:', seatErr);
        setSeatingModeError(message);
        // Non-blocking — user can retry from the form; main event data saved.
      }

      // Invalidate React Query cache to refresh event data
      await queryClient.invalidateQueries({ queryKey: eventKeys.detail(event.id) });
      await queryClient.invalidateQueries({ queryKey: eventKeys.lists() });
      console.log('🔄 Cache invalidated - fresh data will be fetched');

      // Redirect to event manage page
      router.push(`/events/${event.id}/manage`);
    } catch (err) {
      console.error('❌ Event update failed:', err);

      const errorMessage = err instanceof Error
        ? err.message
        : 'Failed to update event. Please try again.';
      setSubmitError(errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  });

  // Phase 6A.47: Convert reference data to dropdown options
  const categoryOptions = toDropdownOptions(categories);
  const currencyOptions = toDropdownOptions(currencies);

  return (
    <form onSubmit={onSubmit} className="space-y-6">
      {/* Phase 6A.X: Table-Style Grid Layout for Better Readability */}

      {/* Basic Information Section */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <FileText className="h-5 w-5" style={{ color: '#FF7900' }} />
            <CardTitle style={{ color: '#8B1538' }}>Basic Information</CardTitle>
          </div>
          <CardDescription>
            Update the essential details about your event
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-5">
          {/* Event Title */}
          <div className="border-b pb-4">
            <label htmlFor="title" className="block text-sm font-semibold text-neutral-700 mb-2">
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
          <div className="border-b pb-4">
            <label className="block text-sm font-semibold text-neutral-700 mb-2">
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
            <label htmlFor="category" className="block text-sm font-semibold text-neutral-700 mb-2">
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
        </CardContent>
      </Card>

      {/* Date & Time Section */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Calendar className="h-5 w-5" style={{ color: '#FF7900' }} />
            <CardTitle style={{ color: '#8B1538' }}>Date & Time</CardTitle>
          </div>
          <CardDescription>
            Update when your event will take place
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
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
        </CardContent>
      </Card>

      {/* Location Section */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <MapPin className="h-5 w-5" style={{ color: '#FF7900' }} />
            <CardTitle style={{ color: '#8B1538' }}>Location</CardTitle>
          </div>
          <CardDescription>
            Update where the event will take place (Optional but recommended)
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
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
            register={register}
            watch={watch}
            setValue={setValue}
            errors={errors}
          />
        </CardContent>
      </Card>

      {/* Capacity & Pricing Section */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Users className="h-5 w-5" style={{ color: '#FF7900' }} />
            <CardTitle style={{ color: '#8B1538' }}>Capacity & Pricing</CardTitle>
          </div>
          <CardDescription>
            Update attendance limits and ticket pricing
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
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

          {/* Phase 7E.5: Registration Mode Picker (edit flow). Mirrors the create form;
              auto-clears the selection when the shape change makes the current mode invalid. */}
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
                  hasGroupTiers: !isFree && (watch('enableGroupPricing') ?? false),
                  hasTicketTiers: !isFree && (watch('enableTieredTicketing') ?? false),
                }}
                helpText={
                  // The domain method Event.SetRegistrationMode rejects mode change once
                  // registrations exist on the event. Show a hint so organisers know why
                  // changes after publishing may fail server-side.
                  event.currentRegistrations > 0
                    ? `Note: ${event.currentRegistrations} registration${event.currentRegistrations === 1 ? '' : 's'} already exist. Server may reject mode changes (Phase 7F adds backfill).`
                    : undefined
                }
              />
            )}
          />

          {/* Pricing Fields (shown only if not free) - Session 33: Added pricing mode toggles */}
          {!isFree && (
            <div className="space-y-4 p-4 border-2 border-orange-200 rounded-lg bg-orange-50">
              <div className="flex items-center gap-2 mb-2">
                <DollarSign className="h-5 w-5" style={{ color: '#FF7900' }} />
                <h4 className="text-sm font-semibold text-neutral-900">Ticket Pricing</h4>
              </div>

              {/* Pricing Mode Selection - Session 33 */}
              <div className="space-y-3">
                {/* Dual Pricing Toggle */}
                <div className="flex items-center gap-3 p-3 bg-white rounded-lg border border-orange-200">
                  <input
                    id="enableDualPricing"
                    type="checkbox"
                    className="h-5 w-5 rounded border-neutral-300 text-orange-500 focus:ring-2 focus:ring-orange-500"
                    {...register('enableDualPricing', {
                      onChange: (e) => {
                        if (e.target.checked) {
                          setValue('enableGroupPricing', false);
                          setValue('enableTieredTicketing', false);
                          setValue('ticketTiers', []);
                        }
                      }
                    })}
                  />
                  <label htmlFor="enableDualPricing" className="text-sm font-medium text-neutral-700">
                    Enable Adult/Child Pricing (different prices for adults and children)
                  </label>
                </div>

                {/* Group Pricing Toggle - Phase 6D */}
                <div className="flex items-center gap-3 p-3 bg-white rounded-lg border border-orange-200">
                  <input
                    id="enableGroupPricing"
                    type="checkbox"
                    className="h-5 w-5 rounded border-neutral-300 text-orange-500 focus:ring-2 focus:ring-orange-500"
                    {...register('enableGroupPricing', {
                      onChange: (e) => {
                        if (e.target.checked) {
                          setValue('enableDualPricing', false);
                          setValue('enableTieredTicketing', false);
                          setValue('ticketTiers', []);
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
                          setValue('enableDualPricing', false);
                          setValue('enableGroupPricing', false);
                        } else {
                          setValue('ticketTiers', []);
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

              {/* Dual Pricing Fields (adult/child) - Session 33 */}
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

              {/* Group Pricing - Session 33: Editable group pricing tiers with useFieldArray */}
              {enableGroupPricing && (
                <div className="space-y-4">
                  <div className="flex items-center justify-between mb-4">
                    <div>
                      <h4 className="text-sm font-semibold text-neutral-900">Group Pricing Tiers</h4>
                      <p className="text-xs text-neutral-600 mt-1">
                        Edit pricing tiers by changing the attendee numbers and prices below
                      </p>
                    </div>
                    {/* Session 44: Add Tier Button */}
                    <Button
                      type="button"
                      variant="outline"
                      onClick={() => append({
                        minAttendees: fields.length > 0 ? (watch(`groupPricingTiers.${fields.length - 1}.maxAttendees`) ?? 0) + 1 : 1,
                        maxAttendees: null,
                        pricePerPerson: 0,
                        currency: Currency.USD,
                      })}
                      className="flex items-center gap-2"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                      </svg>
                      Add Tier
                    </Button>
                  </div>

                  {/* Existing tiers with inline editable inputs using useFieldArray */}
                  {fields.length > 0 && (
                    <div className="space-y-4">
                      {fields.map((field, index) => {
                        const tierPrice = watch(`groupPricingTiers.${index}.pricePerPerson`) as number | undefined;
                        const tierCurrency = watch(`groupPricingTiers.${index}.currency`) as Currency | undefined ?? Currency.USD;
                        return (
                          <div key={field.id} className="p-4 bg-white border-2 border-orange-200 rounded-lg space-y-4">
                            <div className="flex items-center justify-between">
                              <h5 className="text-sm font-semibold text-neutral-900">Tier {index + 1}</h5>
                              <div className="flex items-center gap-2">
                                <span className="text-xs bg-orange-100 text-orange-700 px-2 py-1 rounded">
                                  Original: {field.minAttendees}{field.maxAttendees ? `-${field.maxAttendees}` : '+'} attendees
                                </span>
                                <button
                                  type="button"
                                  onClick={() => remove(index)}
                                  className="p-1 hover:bg-red-50 rounded transition-colors"
                                  title="Delete this tier"
                                >
                                  <X className="h-4 w-4 text-red-600" />
                                </button>
                              </div>
                            </div>

                            {/* Improved layout: Attendees on left (narrower), Price on right (wider) */}
                            <div className="grid grid-cols-12 gap-3">
                              {/* Min Attendees - 2 cols */}
                              <div className="col-span-6 sm:col-span-2">
                                <label className="block text-sm font-medium text-neutral-700 mb-2">
                                  Min *
                                </label>
                                <Input
                                  type="number"
                                  min="1"
                                  max="10000"
                                  className="w-full"
                                  {...register(`groupPricingTiers.${index}.minAttendees`, { valueAsNumber: true })}
                                  error={!!errors.groupPricingTiers?.[index]?.minAttendees}
                                />
                                {errors.groupPricingTiers?.[index]?.minAttendees && (
                                  <p className="mt-1 text-xs text-destructive">
                                    {errors.groupPricingTiers[index]?.minAttendees?.message}
                                  </p>
                                )}
                              </div>

                              {/* Max Attendees - 2 cols */}
                              <div className="col-span-6 sm:col-span-2">
                                <label className="block text-sm font-medium text-neutral-700 mb-2">
                                  Max
                                </label>
                                <Input
                                  type="number"
                                  min="1"
                                  max="10000"
                                  placeholder="∞"
                                  className="w-full"
                                  {...register(`groupPricingTiers.${index}.maxAttendees`, {
                                    setValueAs: (v) => v === '' || v === null || v === undefined ? null : parseInt(v)
                                  })}
                                  error={!!errors.groupPricingTiers?.[index]?.maxAttendees}
                                />
                                {errors.groupPricingTiers?.[index]?.maxAttendees && (
                                  <p className="mt-1 text-xs text-destructive">
                                    {errors.groupPricingTiers[index]?.maxAttendees?.message}
                                  </p>
                                )}
                              </div>

                              {/* Price Per Person - 8 cols (wider) */}
                              <div className="col-span-12 sm:col-span-8">
                                <label className="block text-sm font-medium text-neutral-700 mb-2">
                                  Price Per Person *
                                </label>
                                <div className="flex items-center gap-2">
                                  <select
                                    className="flex-shrink-0 px-2 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500"
                                    {...register(`groupPricingTiers.${index}.currency`, { valueAsNumber: true })}
                                  >
                                    {currencyOptions.map(curr => (
                                      <option key={curr.value} value={curr.value}>
                                        {curr.label}
                                      </option>
                                    ))}
                                  </select>
                                  <Input
                                    type="number"
                                    min="0"
                                    max="10000"
                                    step="1"
                                    className="flex-1"
                                    {...register(`groupPricingTiers.${index}.pricePerPerson`, { valueAsNumber: true })}
                                    error={!!errors.groupPricingTiers?.[index]?.pricePerPerson}
                                  />
                                </div>
                                {errors.groupPricingTiers?.[index]?.pricePerPerson && (
                                  <p className="mt-1 text-xs text-destructive">
                                    {errors.groupPricingTiers[index]?.pricePerPerson?.message}
                                  </p>
                                )}
                              </div>
                            </div>

                            {/* Phase 6A.X: Revenue breakdown preview - full width below */}
                            <RevenueBreakdownPreview
                              ticketPrice={tierPrice}
                              currency={tierCurrency}
                              state={watch('locationState') as string | undefined}
                              country={watch('locationCountry') as string | undefined}
                              priceLabel="person"
                            />
                          </div>
                        );
                      })}
                    </div>
                  )}

                  {/* Helpful guidelines */}
                  <div className="p-3 bg-blue-50 border border-blue-200 rounded-lg">
                    <h5 className="text-xs font-semibold text-blue-900 mb-1">Tier Guidelines:</h5>
                    <ul className="text-xs text-blue-700 space-y-1 list-disc list-inside">
                      <li>First tier must start at 1 attendee</li>
                      <li>Tiers must be continuous with no gaps</li>
                      <li>All tiers must use the same currency</li>
                      <li>Only the last tier can have unlimited max attendees</li>
                    </ul>
                  </div>

                  {errors.groupPricingTiers && typeof errors.groupPricingTiers === 'object' && 'message' in errors.groupPricingTiers && (
                    <p className="mt-2 text-sm text-destructive">{errors.groupPricingTiers.message as string}</p>
                  )}
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

              {/* Seating Redesign Slice 1 + Slice 6 S6.9: inline assigned-seating
                  toggle. Passing eventId activates the preset-library picker
                  and the live layout preview below the toggle. */}
              {enableTieredTicketing && (
                <SeatingSection
                  ticketingMode={TicketingMode.Tiered}
                  value={seatingMode}
                  onChange={(mode) => {
                    setSeatingModeState(mode);
                    if (seatingModeError) setSeatingModeError(null);
                  }}
                  errorMessage={seatingModeError}
                  eventId={event.id}
                />
              )}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Phase 6A.32: Email Groups Section */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Mail className="h-5 w-5" style={{ color: '#FF7900' }} />
            <CardTitle style={{ color: '#8B1538' }}>Email Groups (Optional)</CardTitle>
          </div>
          <CardDescription>
            Select email groups to notify about this event
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <MultiSelect
            options={emailGroups.map(group => ({
              id: group.id,
              label: group.name,
              disabled: !group.isActive
            }))}
            value={watch('emailGroupIds') || []}
            onChange={(ids) => setValue('emailGroupIds', ids, { shouldDirty: true, shouldTouch: true, shouldValidate: true })}
            placeholder="Select email groups to notify"
            isLoading={isLoadingEmailGroups}
            error={!!errors.emailGroupIds}
            errorMessage={errors.emailGroupIds?.message}
            helperText="Select groups that should receive invitations for this event"
          />
        </CardContent>
      </Card>

      {/* Organizer Contact Details (Multiple Contacts) */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Users className="h-5 w-5" style={{ color: '#FF7900' }} />
            <CardTitle style={{ color: '#8B1538' }}>Organizer Contacts (Optional)</CardTitle>
          </div>
          <CardDescription>
            Publish organizer contact information so attendees can reach you
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
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
        </CardContent>
      </Card>

      {/* Donation Feature: Donation Configuration */}
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

      {/* Collection (Event Fund) Configuration */}
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

      {/* Sponsor Configuration */}
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

      {/* Add-On Configuration */}
      <AddOnConfigForm
        isEnabled={addOnsEnabled}
        onEnabledChange={setAddOnsEnabled}
        availableDuringRegistration={addOnAvailableDuringRegistration}
        onAvailableDuringRegistrationChange={setAddOnAvailableDuringRegistration}
        availableStandalone={addOnAvailableStandalone}
        onAvailableStandaloneChange={setAddOnAvailableStandalone}
        addOnMessage={addOnMessage}
        onAddOnMessageChange={setAddOnMessage}
        eventId={event.id}
      />

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
                📸 Manage Images & Videos
              </h4>
              <p className="text-sm text-blue-700">
                You can add or remove event images and videos from the event detail page.
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Validation Errors Display */}
      {Object.keys(errors).length > 0 && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
          <p className="text-sm font-semibold text-red-700 mb-2">Please fix the following errors:</p>
          <ul className="text-sm text-red-600 space-y-1 list-disc list-inside">
            {Object.entries(errors).map(([field, error]: any) => (
              <li key={field}>
                <strong>{field}:</strong> {error.message || 'Invalid value'}
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* API Error Message */}
      {submitError && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
          <p className="text-sm text-red-600">{submitError}</p>
        </div>
      )}

      {/* Form Actions */}
      <div className="flex items-center justify-end gap-4">
        <Button
          type="button"
          variant="outline"
          onClick={() => router.push(`/events/${event.id}/manage`)}
          disabled={isSubmitting}
        >
          Cancel
        </Button>
        <Button
          type="submit"
          disabled={isSubmitting}
          style={{ background: '#FF7900' }}
          className="min-w-[150px]"
        >
          {isSubmitting ? 'Updating...' : 'Update Event'}
        </Button>
      </div>
    </form>
  );
}
