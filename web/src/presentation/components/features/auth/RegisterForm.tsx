'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { registerSchema, type RegisterFormData } from '@/presentation/lib/validators/auth.schemas';
import { authRepository } from '@/infrastructure/api/repositories/auth.repository';
import { UserRole } from '@/infrastructure/api/types/auth.types';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/presentation/components/ui/Card';
import { ApiError } from '@/infrastructure/api/client/api-errors';
import { MetroAreasSelector } from '@/presentation/components/features/auth/MetroAreasSelector';
import { WhatsAppInlineOptIn } from '@/presentation/components/features/whatsapp/WhatsAppInlineOptIn';
import { toE164 } from '@/presentation/lib/validators/whatsapp.schemas';
import { resolveSafeRedirect } from '@/presentation/lib/utils/safe-redirect';

/**
 * RegisterForm Component
 * User registration form — all users join as General User
 * Follows UI/UX best practices with accessibility and responsive design
 */
export function RegisterForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [apiError, setApiError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      preferredMetroAreaIds: [],
      whatsAppEnabled: false,
      whatsAppPhoneNumber: '',
    },
  });

  const preferredMetroAreaIds = watch('preferredMetroAreaIds') || [];
  const whatsAppEnabled = watch('whatsAppEnabled') || false;
  const whatsAppPhoneNumber = watch('whatsAppPhoneNumber') || '';

  const onSubmit = async (data: RegisterFormData) => {
    try {
      setApiError(null);
      setSuccessMessage(null);

      // Phase 7A.6A: Include WhatsApp phone if opted in
      const whatsAppPhone = data.whatsAppEnabled && data.whatsAppPhoneNumber
        ? toE164(data.whatsAppPhoneNumber)
        : undefined;

      await authRepository.register({
        email: data.email,
        password: data.password,
        firstName: data.firstName,
        lastName: data.lastName,
        selectedRole: UserRole.GeneralUser,
        preferredMetroAreaIds: data.preferredMetroAreaIds,
        whatsAppPhoneNumber: whatsAppPhone || undefined,
      });

      // Phase 6A.53: Redirect to login page with info message about email verification
      // Phase 6A.144: thread ?redirect= through to /login so the user is bounced
      // back to the originating surface (e.g. an event detail page) after they
      // verify and sign in. Same-origin guard via resolveSafeRedirect.
      const safe = resolveSafeRedirect(searchParams.get('redirect'), '');
      const loginUrl = safe
        ? `/login?registered=true&redirect=${encodeURIComponent(safe)}`
        : '/login?registered=true';
      router.push(loginUrl);
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

          {/* Metro Areas Selection - Required for registration */}
          <MetroAreasSelector
            value={preferredMetroAreaIds}
            onChange={(ids) => setValue('preferredMetroAreaIds', ids, { shouldValidate: true })}
            error={errors.preferredMetroAreaIds?.message}
            required={true}
            minSelection={1}
            maxSelection={20}
          />

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

          {/* Phase 7A.6A: WhatsApp Opt-In */}
          <WhatsAppInlineOptIn
            enabled={whatsAppEnabled}
            onEnabledChange={(enabled) => {
              setValue('whatsAppEnabled', enabled, { shouldValidate: true });
              if (!enabled) {
                setValue('whatsAppPhoneNumber', '', { shouldValidate: false });
              }
            }}
            phoneNumber={whatsAppPhoneNumber}
            onPhoneNumberChange={(phone) => setValue('whatsAppPhoneNumber', phone, { shouldValidate: true })}
            phoneError={errors.whatsAppPhoneNumber?.message}
            disabled={isSubmitting}
          />

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
