'use client';

import { useEffect } from 'react';
import { ProtectedRoute } from '@/presentation/components/auth/ProtectedRoute';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import { ProfilePhotoSection } from '@/presentation/components/features/profile/ProfilePhotoSection';
import { BasicInfoSection } from '@/presentation/components/features/profile/BasicInfoSection';
import { CulturalInterestsSection } from '@/presentation/components/features/profile/CulturalInterestsSection';
import { PreferredMetroAreasSection } from '@/presentation/components/features/profile/PreferredMetroAreasSection';
import { Button } from '@/presentation/components/ui/Button';
import { OfficialLogo } from '@/presentation/components/atoms/OfficialLogo';
import { useRouter } from 'next/navigation';
import { authRepository } from '@/infrastructure/api/repositories/auth.repository';
import { ArrowLeft } from 'lucide-react';
import Footer from '@/presentation/components/layout/Footer';
import Link from 'next/link';

/**
 * Profile Page
 *
 * User profile management page with:
 * - Profile photo upload/delete (Phase 6A.64)
 * - Basic info editing with location (Phase 6A.70)
 * - Cultural interests editing
 * - Preferred metro areas
 */
export default function ProfilePage() {
  const router = useRouter();
  const { user, clearAuth, isAuthenticated, isLoading: authLoading } = useAuthStore();
  const { loadProfile, profile, isLoading } = useProfileStore();

  // Load profile on mount when user is authenticated
  useEffect(() => {
    if (isAuthenticated && user?.userId && !profile && !isLoading) {
      loadProfile(user.userId);
    }
  }, [isAuthenticated, user?.userId, profile, isLoading, loadProfile]);

  const handleLogout = async () => {
    try {
      await authRepository.logout();
    } catch (error) {
      // Silently handle logout errors (e.g., 401 when already logged out)
      // The error is expected and clearAuth will handle cleanup
    } finally {
      clearAuth();
      router.push('/login');
    }
  };

  const handleBackToDashboard = () => {
    router.push('/dashboard');
  };

  const getInitials = (name: string) => {
    return name
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase();
  };

  return (
    <ProtectedRoute>
      <div className="min-h-screen" style={{ background: '#f7fafc' }}>
        {/* Header */}
        <header className="bg-white border-b sticky top-0 z-40" style={{
          background: 'rgba(255, 255, 255, 0.95)',
          backdropFilter: 'blur(10px)',
          boxShadow: '0 2px 20px rgba(0,0,0,0.1)'
        }}>
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-4">
                <OfficialLogo size="md" />
                <div className="h-6 w-px bg-gray-300"></div>
                <h1 className="text-2xl font-bold" style={{ color: '#8B1538' }}>My Profile</h1>
              </div>

              <div className="flex items-center gap-4">
                <Button
                  onClick={handleBackToDashboard}
                  variant="outline"
                  size="sm"
                  style={{
                    borderColor: '#FF7900',
                    color: '#8B1538'
                  }}
                >
                  <ArrowLeft className="w-4 h-4 mr-2" />
                  Dashboard
                </Button>
                <Button
                  onClick={handleLogout}
                  variant="outline"
                  size="sm"
                  style={{
                    borderColor: '#8B1538',
                    color: '#8B1538'
                  }}
                >
                  Logout
                </Button>
              </div>
            </div>
          </div>
        </header>

        {/* Main Content */}
        <main className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="space-y-6">
            {/* Profile Photo Section */}
            <ProfilePhotoSection />

            {/* Basic Info Section - Phase 6A.70 (now includes Location fields) */}
            <BasicInfoSection />

            {/* Cultural Interests Section */}
            <CulturalInterestsSection />

            {/* Preferred Metro Areas Section - Phase 5B */}
            <PreferredMetroAreasSection />

            {/* Future Sections - Phase 3+ */}
            {/* <BasicInfoSection /> */}

            {/* Loading State */}
            {isLoading && (
              <div className="bg-white rounded-xl shadow p-6 text-center">
                <div className="flex items-center justify-center gap-3">
                  <div
                    className="animate-spin rounded-full h-8 w-8 border-b-2"
                    style={{ borderColor: '#FF7900' }}
                  ></div>
                  <p style={{ color: '#8B1538' }}>Loading profile...</p>
                </div>
              </div>
            )}
          </div>
        </main>

        {/* Footer */}
        <Footer />
      </div>
    </ProtectedRoute>
  );
}
