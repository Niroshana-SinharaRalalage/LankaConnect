'use client';

import { useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { registerSchema, type RegisterFormData } from '@/presentation/lib/validators/auth.schemas';
import { authRepository } from '@/infrastructure/api/repositories/auth.repository';
import { UserRole } from '@/infrastructure/api/types/auth.types';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/presentation/components/ui/Card';
import { ApiError } from '@/infrastructure/api/client/api-errors';

/**
 * RegisterForm Component
 * User registration form with role selection and validation
 * Phase 6A.0: Added role selection with pricing display
 * Follows UI/UX best practices with accessibility and responsive design
 */
export function RegisterForm() {
  const router = useRouter();
  const [apiError, setApiError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    watch,
    control,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      selectedRole: 'GeneralUser',
    },
  });

  const selectedRole = watch('selectedRole');

  const onSubmit = async (data: RegisterFormData) => {
    try {
      setApiError(null);
      setSuccessMessage(null);

      const response = await authRepository.register({
        email: data.email,
        password: data.password,
        firstName: data.firstName,
        lastName: data.lastName,
        selectedRole: data.selectedRole === 'GeneralUser' ? UserRole.GeneralUser : UserRole.EventOrganizer,
      });

      if (data.selectedRole === 'EventOrganizer') {
        setSuccessMessage(
          'Registration successful! Your Event Organizer request is pending admin approval. Please check your email to verify your account.'
        );
      } else {
        setSuccessMessage(
          response.message || 'Registration successful! Please check your email to verify your account.'
        );
      }

      // Redirect to login after 3 seconds
      setTimeout(() => {
        router.push('/login');
      }, 3000);
    } catch (error) {
      if (error instanceof ApiError) {
        setApiError(error.message);
      } else {
        setApiError('An unexpected error occurred. Please try again.');
      }
    }
  };

  return (
    <Card className="w-full max-w-md">
      <CardHeader>
        <CardTitle>Create Account</CardTitle>
        <CardDescription>Join the LankaConnect community</CardDescription>
      </CardHeader>
      <form onSubmit={handleSubmit(onSubmit)}>
        <CardContent className="space-y-4">
          {apiError && (
            <div className="p-3 text-sm text-destructive bg-destructive/10 border border-destructive rounded-md">
              {apiError}
            </div>
          )}

          {successMessage && (
            <div className="p-3 text-sm text-green-700 bg-green-50 border border-green-200 rounded-md">
              {successMessage}
            </div>
          )}

          {/* Role Selection Section - Phase 6A.0 */}
          <div className="space-y-3 pb-4 border-b border-gray-200">
            <label className="text-sm font-medium block">Select Account Type</label>

            {/* General User Option */}
            <label
              className={`flex items-start p-4 border-2 rounded-lg cursor-pointer transition-all ${
                selectedRole === 'GeneralUser'
                  ? 'border-[#FF7900] bg-[#FFF5EB]'
                  : 'border-gray-200 hover:border-gray-300'
              }`}
            >
              <input
                type="radio"
                value="GeneralUser"
                className="mt-1 h-4 w-4 text-[#FF7900] focus:ring-[#FF7900] border-gray-300"
                {...register('selectedRole')}
              />
              <div className="ml-3 flex-1">
                <div className="font-semibold text-gray-900">General User</div>
                <div className="text-sm text-gray-600 mt-1">
                  Browse events, register for activities, and connect with the community
                </div>
                <div className="mt-2 inline-block px-2 py-1 text-xs font-semibold rounded" style={{ backgroundColor: '#E8F5E9', color: '#2E7D32' }}>
                  Always Free
                </div>
              </div>
            </label>

            {/* Event Organizer Option */}
            <label
              className={`flex items-start p-4 border-2 rounded-lg cursor-pointer transition-all ${
                selectedRole === 'EventOrganizer'
                  ? 'border-[#FF7900] bg-[#FFF5EB]'
                  : 'border-gray-200 hover:border-gray-300'
              }`}
            >
              <input
                type="radio"
                value="EventOrganizer"
                className="mt-1 h-4 w-4 text-[#FF7900] focus:ring-[#FF7900] border-gray-300"
                {...register('selectedRole')}
              />
              <div className="ml-3 flex-1">
                <div className="font-semibold text-gray-900">Event Organizer</div>
                <div className="text-sm text-gray-600 mt-1">
                  Create unlimited events, access templates, and get priority support
                </div>
                <div className="mt-2 space-y-1">
                  <div className="inline-block px-2 py-1 text-xs font-semibold rounded" style={{ backgroundColor: '#FFF3E0', color: '#E65100' }}>
                    Free for 6 months, then $10/month
                  </div>
                  <div className="text-xs text-gray-500 mt-1">
                    ⚠️ Requires admin approval
                  </div>
                </div>
              </div>
            </label>

            {errors.selectedRole && (
              <p className="text-sm text-destructive">{errors.selectedRole.message}</p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <label htmlFor="firstName" className="text-sm font-medium">
                First Name
              </label>
              <Input
                id="firstName"
                placeholder="John"
                error={!!errors.firstName}
                {...register('firstName')}
              />
              {errors.firstName && (
                <p className="text-sm text-destructive">{errors.firstName.message}</p>
              )}
            </div>

            <div className="space-y-2">
              <label htmlFor="lastName" className="text-sm font-medium">
                Last Name
              </label>
              <Input
                id="lastName"
                placeholder="Doe"
                error={!!errors.lastName}
                {...register('lastName')}
              />
              {errors.lastName && (
                <p className="text-sm text-destructive">{errors.lastName.message}</p>
              )}
            </div>
          </div>

          <div className="space-y-2">
            <label htmlFor="email" className="text-sm font-medium">
              Email
            </label>
            <Input
              id="email"
              type="email"
              placeholder="you@example.com"
              error={!!errors.email}
              {...register('email')}
            />
            {errors.email && (
              <p className="text-sm text-destructive">{errors.email.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <label htmlFor="password" className="text-sm font-medium">
              Password
            </label>
            <Input
              id="password"
              type="password"
              placeholder="Create a strong password"
              error={!!errors.password}
              {...register('password')}
            />
            {errors.password && (
              <p className="text-sm text-destructive">{errors.password.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <label htmlFor="confirmPassword" className="text-sm font-medium">
              Confirm Password
            </label>
            <Input
              id="confirmPassword"
              type="password"
              placeholder="Confirm your password"
              error={!!errors.confirmPassword}
              {...register('confirmPassword')}
            />
            {errors.confirmPassword && (
              <p className="text-sm text-destructive">{errors.confirmPassword.message}</p>
            )}
          </div>

          {/* Event Organizer Approval Checkbox - Phase 6A.0 */}
          {selectedRole === 'EventOrganizer' && (
            <div className="space-y-2 p-3 bg-orange-50 border border-orange-200 rounded-md">
              <div className="flex items-start space-x-2">
                <input
                  id="agreeToApproval"
                  type="checkbox"
                  className="mt-1 h-4 w-4 rounded border-gray-300"
                  {...register('agreeToApproval')}
                />
                <label htmlFor="agreeToApproval" className="text-sm text-gray-700">
                  I understand my Event Organizer request will be reviewed by the admin team
                </label>
              </div>
              {errors.agreeToApproval && (
                <p className="text-sm text-destructive">{errors.agreeToApproval.message}</p>
              )}
            </div>
          )}

          <div className="flex items-start space-x-2">
            <input
              id="agreeToTerms"
              type="checkbox"
              className="mt-1 h-4 w-4 rounded border-gray-300"
              {...register('agreeToTerms')}
            />
            <label htmlFor="agreeToTerms" className="text-sm text-muted-foreground">
              I agree to the{' '}
              <Link href="/terms" className="text-primary hover:underline">
                Terms of Service
              </Link>{' '}
              and{' '}
              <Link href="/privacy" className="text-primary hover:underline">
                Privacy Policy
              </Link>
            </label>
          </div>
          {errors.agreeToTerms && (
            <p className="text-sm text-destructive">{errors.agreeToTerms.message}</p>
          )}
        </CardContent>

        <CardFooter className="flex-col space-y-4">
          <Button
            type="submit"
            className="w-full"
            loading={isSubmitting}
            disabled={isSubmitting || !!successMessage}
          >
            Create Account
          </Button>

          <p className="text-sm text-center text-muted-foreground">
            Already have an account?{' '}
            <Link href="/login" className="text-primary hover:underline font-medium">
              Sign in
            </Link>
          </p>
        </CardFooter>
      </form>
    </Card>
  );
}
