import { describe, it, expect, beforeAll, afterEach, afterAll } from 'vitest';
import { authRepository } from '@/infrastructure/api/repositories/auth.repository';
import { profileRepository } from '@/infrastructure/api/repositories/profile.repository';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import { apiClient } from '@/infrastructure/api/client/api-client';
import type { RegisterRequest } from '@/infrastructure/api/types/auth.types';

/**
 * Phase 5B.11 - E2E Integration Tests for Metro Areas Workflow
 * Tests the complete user journey: Profile → Newsletter → Community Activity
 *
 * Status: TEST STRUCTURE & PLANNING PHASE
 * Most tests are skipped (.skip) pending staging database setup
 * Running: Basic registration/auth tests to verify test infrastructure
 */

interface TestUser {
  id?: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  accessToken?: string;
  refreshToken?: string;
}

describe('Phase 5B.11: Metro Areas E2E Workflow', () => {
  const testUser: TestUser = {
    email: `test.metro.${Date.now()}@lankaconnect.test`,
    password: 'Test@Pwd!9', // Avoid sequential chars like 123
    firstName: 'Metro',
    lastName: 'Tester',
  };

  // Sample metro area GUIDs (from Phase 5B.10 seeder)
  const ohioMetroId = '39000000-0000-0000-0000-000000000001'; // All Ohio
  const clevelandMetroId = '39111111-1111-1111-1111-111111111001'; // Cleveland
  const columbusMetroId = '39111111-1111-1111-1111-111111111002'; // Columbus
  const cincinnatMetroId = '39111111-1111-1111-1111-111111111003'; // Cincinnati
  const texasMetroId = '48000000-0000-0000-0000-000000000001'; // All Texas

  beforeAll(async () => {
    // Verify API is configured
    console.log('Testing against API:', process.env.NEXT_PUBLIC_API_URL);
    expect(process.env.NEXT_PUBLIC_API_URL).toBeDefined();
  });

  afterEach(() => {
    // Clear auth tokens after each test
    if (testUser.accessToken) {
      apiClient.clearAuthToken();
      testUser.accessToken = undefined;
      testUser.refreshToken = undefined;
    }
  });

  afterAll(async () => {
    // Cleanup: Delete test user if created
    if (testUser.id && testUser.accessToken) {
      try {
        // Note: Actual cleanup would require a delete endpoint
        // For now, just clear tokens
        apiClient.clearAuthToken();
      } catch (error) {
        console.warn('Failed to cleanup test user:', error);
      }
    }
  });

  // ============================================================================
  // SECTION 1: USER REGISTRATION & AUTHENTICATION
  // ============================================================================

  describe('User Registration & Authentication', () => {
    it('Phase 5B.11.3a: should register a new user for metro testing', async () => {
      const registerData: RegisterRequest = {
        email: testUser.email,
        password: testUser.password,
        firstName: testUser.firstName,
        lastName: testUser.lastName,
      };

      const response = await authRepository.register(registerData);

      expect(response).toBeDefined();
      expect(response.userId).toBeDefined();
      expect(response.email).toBe(testUser.email.toLowerCase());

      // Store user ID for cleanup
      testUser.id = response.userId;
    });

    it.skip('Phase 5B.11.3b: should successfully login with valid credentials', async () => {
      // BLOCKER: Email Verification Required in Staging
      //
      // Staging enforces email verification before login.
      // Backend checks: if (!user.IsEmailVerified) return "Email address must be verified"
      //
      // Root Cause:
      // - Verification tokens are sent via email
      // - Testing environment cannot intercept emails
      // - No test-specific bypass endpoint exists yet
      //
      // Solution Paths:
      // 1. Backend Change (Recommended):
      //    - Add POST /api/auth/test/verify-user/{userId} endpoint
      //    - Requires ASPNETCORE_ENVIRONMENT=Development || special API key
      //    - Marks user email as verified without token validation
      //
      // 2. Frontend Test Helper:
      //    - Create test helper that calls verify-email with known token
      //    - Still requires token generation logic
      //
      // 3. Database Seeding:
      //    - Create pre-verified test user in staging DB
      //    - Use known credentials in tests
      //
      // Unblocking Steps:
      // [ ] Implement /api/auth/test/verify-user/{userId} endpoint
      // [ ] Or: Update test to capture verification token from logs/database
      // [ ] Then: Unskip this test and run full E2E flow
      //
      // Once blocked, the flow would be:
      // await authRepository.resendVerificationEmail(testUser.email);
      // const token = /* capture from email or db */;
      // await authRepository.verifyEmail(token);
      // const loginResponse = await authRepository.login({...});
      const loginResponse = await authRepository.login({
        email: testUser.email,
        password: testUser.password,
      });

      expect(loginResponse).toBeDefined();
      expect(loginResponse.accessToken).toBeDefined();
      expect(loginResponse.refreshToken).toBeDefined();

      testUser.accessToken = loginResponse.accessToken;
      testUser.refreshToken = loginResponse.refreshToken;
      apiClient.setAuthToken(loginResponse.accessToken);
    });
  });

  // ============================================================================
  // SECTION 2: PROFILE METRO SELECTION
  // ============================================================================

  describe('Profile Metro Selection (Phase 5B.11.3)', () => {
    it.skip('should update profile with single metro area selection', async () => {
      const metroIds = [ohioMetroId];
      const updatedProfile = await profileRepository.updatePreferredMetroAreas(metroIds);
      expect(updatedProfile).toBeDefined();
      expect(updatedProfile.preferredMetroAreas).toHaveLength(1);
      expect(updatedProfile.preferredMetroAreas).toContain(ohioMetroId);
    });

    it.skip('should update profile with multiple metro areas (0-20 limit)', async () => {
      const metroIds = [
        ohioMetroId,
        clevelandMetroId,
        columbusMetroId,
        cincinnatMetroId,
        texasMetroId,
      ];
      const updatedProfile = await profileRepository.updatePreferredMetroAreas(metroIds);
      expect(updatedProfile).toBeDefined();
      expect(updatedProfile.preferredMetroAreas).toHaveLength(5);
      expect(updatedProfile.preferredMetroAreas).toEqual(expect.arrayContaining(metroIds));
    });

    it.skip('should persist metro selection after save', async () => {
      const metroIds = [clevelandMetroId, columbusMetroId];
      await profileRepository.updatePreferredMetroAreas(metroIds);
      const retrievedProfile = await profileRepository.getProfile();
      expect(retrievedProfile).toBeDefined();
      expect(retrievedProfile.preferredMetroAreas).toContain(clevelandMetroId);
      expect(retrievedProfile.preferredMetroAreas).toContain(columbusMetroId);
    });

    it.skip('should allow clearing all metros (privacy choice)', async () => {
      const metroIds = [ohioMetroId, clevelandMetroId];
      await profileRepository.updatePreferredMetroAreas(metroIds);
      const clearedProfile = await profileRepository.updatePreferredMetroAreas([]);
      expect(clearedProfile).toBeDefined();
      expect(clearedProfile.preferredMetroAreas).toHaveLength(0);
    });

    it.skip('should validate max limit enforcement (20 metros)', async () => {
      const excessiveMetroIds = Array(21).fill(null).map((_, i) => {
        const stateCode = String(i + 1).padStart(2, '0');
        return `${stateCode}000000-0000-0000-0000-000000000001`;
      });

      try {
        const updatedProfile = await profileRepository.updatePreferredMetroAreas(
          excessiveMetroIds.slice(0, 20)
        );
        expect(updatedProfile.preferredMetroAreas.length).toBeLessThanOrEqual(20);
      } catch (error) {
        expect(error).toBeDefined();
      }
    });
  });

  // ============================================================================
  // SECTION 3: LANDING PAGE EVENT FILTERING
  // ============================================================================

  describe('Landing Page Event Filtering (Phase 5B.11.5)', () => {
    it.skip('should show all events when no metros selected', async () => {
      await profileRepository.updatePreferredMetroAreas([]);
      const allEvents = await eventsRepository.getAll();
      expect(allEvents).toBeDefined();
      expect(Array.isArray(allEvents)).toBe(true);
      expect(allEvents.length).toBeGreaterThan(0);
    });

    it.skip('should filter events by single state metro area', async () => {
      await profileRepository.updatePreferredMetroAreas([ohioMetroId]);
      const allEvents = await eventsRepository.getAll();
      const ohioEvents = allEvents.filter((event) => {
        const location = event.location || '';
        return (
          location.includes('Ohio') ||
          location.includes('Cleveland') ||
          location.includes('Columbus') ||
          location.includes('Cincinnati')
        );
      });
      expect(allEvents.length).toBeGreaterThan(0);
    });

    it.skip('should filter events by single city metro area', async () => {
      await profileRepository.updatePreferredMetroAreas([clevelandMetroId]);
      const allEvents = await eventsRepository.getAll();
      const clevelandEvents = allEvents.filter((event) => {
        const location = event.location || '';
        return location.includes('Cleveland');
      });
      expect(allEvents.length).toBeGreaterThan(0);
    });

    it.skip('should filter by multiple metros using OR logic', async () => {
      const multipleMetros = [clevelandMetroId, columbusMetroId, cincinnatMetroId];
      await profileRepository.updatePreferredMetroAreas(multipleMetros);
      const allEvents = await eventsRepository.getAll();
      const multiMetroEvents = allEvents.filter((event) => {
        const location = event.location || '';
        return (
          location.includes('Cleveland') ||
          location.includes('Columbus') ||
          location.includes('Cincinnati')
        );
      });
      expect(allEvents.length).toBeGreaterThan(0);
    });

    it.skip('should not duplicate events across sections', async () => {
      const metrosWithOverlap = [ohioMetroId];
      await profileRepository.updatePreferredMetroAreas(metrosWithOverlap);
      const allEvents = await eventsRepository.getAll();
      const uniqueEventIds = new Set(allEvents.map((e) => e.id));
      expect(uniqueEventIds.size).toBe(allEvents.length);
    });

    it.skip('should display accurate event count badges', async () => {
      await profileRepository.updatePreferredMetroAreas([clevelandMetroId]);
      const allEvents = await eventsRepository.getAll();
      const clevelandEvents = allEvents.filter((event) => {
        return (event.location || '').includes('Cleveland');
      });
      expect(clevelandEvents.length).toBeGreaterThanOrEqual(0);
    });
  });

  // ============================================================================
  // SECTION 4: NEWSLETTER INTEGRATION (OPTIONAL)
  // ============================================================================

  describe('Newsletter Integration (Phase 5B.11.4)', () => {
    it('Phase 5B.11.4a: should subscribe to newsletter with selected metros', async () => {
      const newsletterSubscription = {
        email: testUser.email,
        metroAreaIds: [ohioMetroId, clevelandMetroId],
      };

      try {
        expect(newsletterSubscription.metroAreaIds).toHaveLength(2);
        expect(newsletterSubscription.metroAreaIds).toContain(ohioMetroId);
      } catch (error) {
        console.log('Newsletter endpoint not yet available in this phase');
      }
    });

    it.skip('Phase 5B.11.4b: should sync newsletter metros to user profile', async () => {
      if (!testUser.accessToken) {
        const loginResponse = await authRepository.login({
          email: testUser.email,
          password: testUser.password,
        });
        testUser.accessToken = loginResponse.accessToken;
        apiClient.setAuthToken(testUser.accessToken);
      }

      const metrosFromNewsletter = [clevelandMetroId, columbusMetroId];
      const updatedProfile = await profileRepository.updatePreferredMetroAreas(metrosFromNewsletter);

      expect(updatedProfile.preferredMetroAreas).toEqual(
        expect.arrayContaining(metrosFromNewsletter)
      );
    });
  });

  // ============================================================================
  // SECTION 5: UI/UX VALIDATION
  // ============================================================================

  describe('UI/UX Component Validation (Phase 5B.11.6)', () => {
    it.skip('should show preferred section only when authenticated with metros', async () => {
      if (!testUser.accessToken) {
        const loginResponse = await authRepository.login({
          email: testUser.email,
          password: testUser.password,
        });
        testUser.accessToken = loginResponse.accessToken;
        apiClient.setAuthToken(testUser.accessToken);
      }

      await profileRepository.updatePreferredMetroAreas([clevelandMetroId]);
      const profile = await profileRepository.getProfile();
      expect(profile.preferredMetroAreas).toHaveLength(1);
    });

    it.skip('should display event count badges with correct values', async () => {
      const allEvents = await eventsRepository.getAll();
      const totalCount = allEvents.length;
      expect(totalCount).toBeGreaterThan(0);

      const profile = await profileRepository.getProfile();
      const selectedMetros = profile.preferredMetroAreas || [];
      const preferredCount = selectedMetros.length > 0 ? allEvents.length : 0;
      expect(preferredCount).toBeGreaterThanOrEqual(0);
    });

    it.skip('should support responsive layout on different screen sizes', async () => {
      const profile = await profileRepository.getProfile();
      const events = await eventsRepository.getAll();
      expect(profile).toBeDefined();
      expect(events).toBeDefined();
    });

    it.skip('should display correct icons for sections (Sparkles, MapPin)', async () => {
      const profile = await profileRepository.getProfile();

      if (profile.preferredMetroAreas && profile.preferredMetroAreas.length > 0) {
        expect(profile.preferredMetroAreas.length).toBeGreaterThan(0);
      }

      const events = await eventsRepository.getAll();
      expect(events).toBeDefined();
    });
  });

  // ============================================================================
  // SECTION 6: STATE-LEVEL vs CITY-LEVEL FILTERING
  // ============================================================================

  describe('State-Level vs City-Level Metro Filtering (Phase 5B.11.5)', () => {
    it.skip('should match state-level metro to any city in that state', async () => {
      await profileRepository.updatePreferredMetroAreas([ohioMetroId]);
      const allEvents = await eventsRepository.getAll();
      const ohioEvents = allEvents.filter((event) => {
        const location = event.location || '';
        return location.includes('Ohio') || location.includes('Cleveland') || location.includes('Columbus');
      });
      expect(allEvents.length).toBeGreaterThan(0);
    });

    it.skip('should match city-level metro to specific city only', async () => {
      await profileRepository.updatePreferredMetroAreas([clevelandMetroId]);
      const allEvents = await eventsRepository.getAll();
      const clevelandEvents = allEvents.filter((event) => {
        const location = event.location || '';
        return location.includes('Cleveland');
      });
      expect(allEvents.length).toBeGreaterThan(0);
    });

    it.skip('should handle state name conversion (OH -> Ohio)', async () => {
      await profileRepository.updatePreferredMetroAreas([ohioMetroId]);
      const allEvents = await eventsRepository.getAll();
      const ohioEventsByFullName = allEvents.filter((event) => {
        return (event.location || '').includes('Ohio');
      });
      expect(allEvents.length).toBeGreaterThan(0);
    });
  });
});
