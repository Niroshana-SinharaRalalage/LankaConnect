'use client';

import { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { Eye, EyeOff } from 'lucide-react';
import { loginSchema, type LoginFormData } from '@/presentation/lib/validators/auth.schemas';
import { authRepository } from '@/infrastructure/api/repositories/auth.repository';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { ApiError } from '@/infrastructure/api/client/api-errors';
import type { AuthTokens } from '@/infrastructure/api/types/auth.types';

/**
 * LoginForm Component
 * User login form with validation and error handling
 * Matches the design from 01-Login-Web.html mockup
 * Phase 6A.53: Shows email verification reminder when redirected from registration
 */
export function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { setAuth } = useAuthStore();
  const [apiError, setApiError] = useState<string | null>(null);
  const [showPassword, setShowPassword] = useState(false);
  const [rememberMe, setRememberMe] = useState(false);
  const [showRegistrationInfo, setShowRegistrationInfo] = useState(false);

  // Phase 6A.53: Check if user just registered
  useEffect(() => {
    if (searchParams.get('registered') === 'true') {
      setShowRegistrationInfo(true);
      // Remove query parameter from URL after showing message
      const url = new URL(window.location.href);
      url.searchParams.delete('registered');
      window.history.replaceState({}, '', url.pathname);
    }
  }, [searchParams]);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginFormData) => {
    try {
      setApiError(null);

      // Phase AUTH-IMPROVEMENT: Pass rememberMe to backend for extended sessions
      const response = await authRepository.login(data, rememberMe);

      // Set auth state
      // Phase 6A.10: In development (localStorage mode), backend sends refreshToken in response body
      // In production (cookie mode), refreshToken is in HttpOnly cookie
      const tokens: AuthTokens = {
        accessToken: response.accessToken,
        refreshToken: response.refreshToken || '', // Use refreshToken from response if present
        expiresIn: 3600,
      };
      setAuth(response.user, tokens);

      // Phase 6A.8: Redirect to landing page after login
      // Users can access dashboard via menu or profile settings
      router.push('/');
    } catch (error) {
      if (error instanceof ApiError) {
        setApiError(error.message);
      } else {
        setApiError('An unexpected error occurred. Please try again.');
      }
    }
  };

  return (
    <div className="w-full">
      {/* Form Header */}
      <div className="mb-10">
        <h2 className="text-[2rem] font-semibold mb-2" style={{ color: '#8B1538' }}>Sign In</h2>
        <p className="text-base" style={{ color: '#666' }}>Enter your credentials to access your account</p>
      </div>

      {/* Form */}
      <form onSubmit={handleSubmit(onSubmit)}>
        {/* Phase 6A.53: Registration Success Info */}
        {showRegistrationInfo && (
          <div className="mb-6 p-4 text-sm bg-blue-50 border border-blue-200 rounded-[10px]">
            <div className="flex items-start gap-3">
              <svg className="w-5 h-5 text-blue-600 flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
              </svg>
              <div className="flex-1">
                <p className="font-medium text-blue-900 mb-1">Registration Successful!</p>
                <p className="text-blue-700">We've sent a verification email to your inbox. Please check your email and click the verification link to activate your account before logging in.</p>
              </div>
              <button
                type="button"
                onClick={() => setShowRegistrationInfo(false)}
                className="text-blue-400 hover:text-blue-600"
                aria-label="Close"
              >
                <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
                </svg>
              </button>
            </div>
          </div>
        )}

        {/* API Error */}
        {apiError && (
          <div className="mb-6 p-3 text-sm text-destructive bg-destructive/10 border border-destructive rounded-[10px]">
            {apiError}
          </div>
        )}

        {/* Email Field */}
        <div className="mb-[25px]">
          <label htmlFor="email" className="block mb-2 font-medium text-[0.95rem]" style={{ color: '#333' }}>
            Email Address
          </label>
          <input
            id="email"
            type="email"
            placeholder="you@example.com"
            className={`w-full px-4 py-[15px] border-2 rounded-[10px] text-base transition-all duration-300 focus:outline-none ${
              errors.email ? 'border-destructive' : 'border-[#e0e0e0]'
            }`}
            onFocus={(e) => {
              if (!errors.email) {
                e.target.style.borderColor = '#FF7900';
                e.target.style.boxShadow = '0 0 0 3px rgba(255, 121, 0, 0.1)';
              }
            }}
            {...register('email', {
              onBlur: (e) => {
                if (!errors.email) {
                  e.target.style.borderColor = '#e0e0e0';
                  e.target.style.boxShadow = 'none';
                }
              }
            })}
          />
          {errors.email && (
            <p className="mt-2 text-sm text-destructive">{errors.email.message}</p>
          )}
        </div>

        {/* Password Field */}
        <div className="mb-[25px]">
          <label htmlFor="password" className="block mb-2 font-medium text-[0.95rem]" style={{ color: '#333' }}>
            Password
          </label>
          <div className="relative">
            <input
              id="password"
              type={showPassword ? 'text' : 'password'}
              placeholder="Enter your password"
              className={`w-full px-4 py-[15px] pr-12 border-2 rounded-[10px] text-base transition-all duration-300 focus:outline-none ${
                errors.password ? 'border-destructive' : 'border-[#e0e0e0]'
              }`}
              onFocus={(e) => {
                if (!errors.password) {
                  e.target.style.borderColor = '#FF7900';
                  e.target.style.boxShadow = '0 0 0 3px rgba(255, 121, 0, 0.1)';
                }
              }}
              {...register('password', {
                onBlur: (e) => {
                  if (!errors.password) {
                    e.target.style.borderColor = '#e0e0e0';
                    e.target.style.boxShadow = 'none';
                  }
                }
              })}
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute right-4 top-1/2 -translate-y-1/2 transition-colors"
              style={{ color: '#666' }}
              onMouseEnter={(e) => e.currentTarget.style.color = '#333'}
              onMouseLeave={(e) => e.currentTarget.style.color = '#666'}
            >
              {showPassword ? <EyeOff className="w-5 h-5" /> : <Eye className="w-5 h-5" />}
            </button>
          </div>
          {errors.password && (
            <p className="mt-2 text-sm text-destructive">{errors.password.message}</p>
          )}
        </div>

        {/* Form Options */}
        <div className="flex items-center justify-between mb-[30px]">
          <label className="flex items-center cursor-pointer">
            <input
              type="checkbox"
              checked={rememberMe}
              onChange={(e) => setRememberMe(e.target.checked)}
              className="w-[18px] h-[18px] mr-2 cursor-pointer"
              style={{ accentColor: '#FF7900' }}
            />
            <span className="text-sm" style={{ color: '#333' }}>Remember me</span>
          </label>
          <Link
            href="/forgot-password"
            className="text-sm font-medium transition-colors"
            style={{ color: '#FF7900' }}
            onMouseEnter={(e) => e.currentTarget.style.color = '#8B1538'}
            onMouseLeave={(e) => e.currentTarget.style.color = '#FF7900'}
          >
            Forgot Password?
          </Link>
        </div>

        {/* Submit Button */}
        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full py-4 text-white border-none rounded-[10px] text-[1.1rem] font-semibold cursor-pointer transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed"
          style={{
            background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)',
            boxShadow: '0 4px 15px rgba(255, 121, 0, 0.3)',
          }}
          onMouseEnter={(e) => {
            if (!isSubmitting) {
              e.currentTarget.style.transform = 'translateY(-2px)';
              e.currentTarget.style.boxShadow = '0 6px 25px rgba(255, 121, 0, 0.4)';
            }
          }}
          onMouseLeave={(e) => {
            if (!isSubmitting) {
              e.currentTarget.style.transform = 'translateY(0)';
              e.currentTarget.style.boxShadow = '0 4px 15px rgba(255, 121, 0, 0.3)';
            }
          }}
        >
          {isSubmitting ? 'Signing In...' : 'Sign In'}
        </button>

        {/* Divider */}
        <div className="flex items-center my-[30px]" style={{ color: '#999' }}>
          <div className="flex-1 h-px" style={{ background: '#e0e0e0' }}></div>
          <span className="px-4">OR</span>
          <div className="flex-1 h-px" style={{ background: '#e0e0e0' }}></div>
        </div>

        {/* Microsoft SSO Button */}
        <button
          type="button"
          className="w-full py-[14px] bg-white border-2 rounded-[10px] text-base font-medium cursor-pointer transition-all duration-300 flex items-center justify-center gap-2.5 mb-[15px]"
          style={{ borderColor: '#e0e0e0' }}
          onMouseEnter={(e) => {
            e.currentTarget.style.borderColor = '#FF7900';
            e.currentTarget.style.background = 'rgba(255, 121, 0, 0.05)';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.borderColor = '#e0e0e0';
            e.currentTarget.style.background = 'white';
          }}
        >
          <svg className="w-6 h-6" viewBox="0 0 23 23">
            <path fill="#f3f3f3" d="M0 0h23v23H0z"/>
            <path fill="#f35325" d="M1 1h10v10H1z"/>
            <path fill="#81bc06" d="M12 1h10v10H12z"/>
            <path fill="#05a6f0" d="M1 12h10v10H1z"/>
            <path fill="#ffba08" d="M12 12h10v10H12z"/>
          </svg>
          Continue with Microsoft
        </button>
      </form>

      {/* Signup Link */}
      <div className="text-center mt-[30px]" style={{ color: '#666' }}>
        Don't have an account?{' '}
        <Link
          href="/register"
          className="font-semibold transition-colors"
          style={{ color: '#FF7900' }}
          onMouseEnter={(e) => e.currentTarget.style.color = '#8B1538'}
          onMouseLeave={(e) => e.currentTarget.style.color = '#FF7900'}
        >
          Join Community
        </Link>
      </div>
    </div>
  );
}
