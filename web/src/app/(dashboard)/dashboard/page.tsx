'use client';

import { ProtectedRoute } from '@/presentation/components/auth/ProtectedRoute';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { Button } from '@/presentation/components/ui/Button';
import { useRouter } from 'next/navigation';
import { authRepository } from '@/infrastructure/api/repositories/auth.repository';

export default function DashboardPage() {
  const router = useRouter();
  const { user, clearAuth } = useAuthStore();

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

  return (
    <ProtectedRoute>
      <div className="min-h-screen bg-gray-50">
        <header className="bg-white shadow">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6 flex justify-between items-center">
            <h1 className="text-3xl font-bold text-gray-900">Dashboard</h1>
            <Button onClick={handleLogout} variant="outline">
              Logout
            </Button>
          </div>
        </header>

        <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="space-y-6">
            {/* User Info Card */}
            <div className="bg-white rounded-lg shadow p-6">
              <h2 className="text-2xl font-semibold mb-4">Welcome, {user?.firstName}!</h2>
              <div className="space-y-2">
                <p className="text-gray-600">
                  <span className="font-medium">Email:</span> {user?.email}
                </p>
                <p className="text-gray-600">
                  <span className="font-medium">Name:</span> {user?.firstName} {user?.lastName}
                </p>
                <p className="text-gray-600">
                  <span className="font-medium">Role:</span> {user?.role}
                </p>
                <p className="text-gray-600">
                  <span className="font-medium">Email Verified:</span>{' '}
                  {user?.isEmailVerified ? 'Yes' : 'No'}
                </p>
              </div>
            </div>

            {/* Quick Actions Card */}
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="text-xl font-semibold mb-4">Quick Actions</h3>
              <div className="flex gap-4">
                <Button onClick={() => router.push('/profile')} variant="default">
                  Edit Profile
                </Button>
              </div>
            </div>
          </div>
        </main>
      </div>
    </ProtectedRoute>
  );
}
