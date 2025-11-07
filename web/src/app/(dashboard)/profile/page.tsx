'use client';

import { useEffect } from 'react';
import { ProtectedRoute } from '@/presentation/components/auth/ProtectedRoute';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import { ProfilePhotoSection } from '@/presentation/components/features/profile/ProfilePhotoSection';
import { Button } from '@/presentation/components/ui/Button';
import { useRouter } from 'next/navigation';
import { authRepository } from '@/infrastructure/api/repositories/auth.repository';

/**
 * Profile Page
 *
 * User profile management page with:
 * - Profile photo upload/delete
 * - Basic info editing (future)
 * - Location info editing (future)
 * - Cultural interests editing (future)
 * - Languages editing (future)
 *
 * Phase 1: Photo upload only
 * Future phases will add additional sections
 */
export default function ProfilePage() {
  const router = useRouter();
  const { user, clearAuth } = useAuthStore();
  const { loadProfile, profile, isLoading } = useProfileStore();

  // Load profile on mount
  useEffect(() => {
    if (user?.userId && !profile) {
      loadProfile(user.userId);
    }
  }, [user?.userId, profile, loadProfile]);

  const handleLogout = async () => {
    try {
      await authRepository.logout();
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      clearAuth();
      router.push('/login');
    }
  };

  const handleBackToDashboard = () => {
    router.push('/dashboard');
  };

  return (
    <ProtectedRoute>
      <div className="min-h-screen bg-gray-50">
        {/* Header */}
        <header className="bg-white shadow">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6 flex justify-between items-center">
            <div className="flex items-center gap-4">
              <Button onClick={handleBackToDashboard} variant="outline" size="sm">
                ← Back to Dashboard
              </Button>
              <h1 className="text-3xl font-bold text-gray-900">My Profile</h1>
            </div>
            <Button onClick={handleLogout} variant="outline">
              Logout
            </Button>
          </div>
        </header>

        {/* Main Content */}
        <main className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="space-y-6">
            {/* Welcome Section */}
            <div className="bg-white rounded-lg shadow p-6">
              <h2 className="text-2xl font-semibold mb-2">
                Welcome, {user?.firstName} {user?.lastName}!
              </h2>
              <p className="text-gray-600">
                Manage your profile information to help others in the Sri Lankan community
                connect with you.
              </p>
            </div>

            {/* Profile Photo Section */}
            <ProfilePhotoSection />

            {/* Future Sections - Phase 2+ */}
            {/* <BasicInfoSection /> */}
            {/* <LocationSection /> */}
            {/* <CulturalInterestsSection /> */}
            {/* <LanguagesSection /> */}

            {/* Loading State */}
            {isLoading && (
              <div className="bg-white rounded-lg shadow p-6 text-center">
                <p className="text-gray-600">Loading profile...</p>
              </div>
            )}
          </div>
        </main>
      </div>
    </ProtectedRoute>
  );
}
