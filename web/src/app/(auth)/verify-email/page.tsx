'use client';

import { Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import { EmailVerification } from '@/presentation/components/features/auth/EmailVerification';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import Link from 'next/link';
import Image from 'next/image';

/**
 * EmailVerificationContent Component
 * Inner component that uses useSearchParams to get the token
 */
function EmailVerificationContent() {
  const searchParams = useSearchParams();
  const token = searchParams.get('token');

  if (!token) {
    return (
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Invalid Verification Link</CardTitle>
          <CardDescription>
            The email verification link is invalid or missing the required token.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="p-4 bg-red-50 border border-red-200 rounded-md">
            <p className="text-sm text-red-800">
              This email verification link appears to be invalid. Please check your email for the correct link
              or contact support if you continue to have issues.
            </p>
          </div>
          <div className="flex flex-col space-y-2">
            <Link href="/login" className="text-primary hover:underline text-sm text-center">
              Back to Login
            </Link>
            <Link href="/contact" className="text-primary hover:underline text-sm text-center">
              Contact Support
            </Link>
          </div>
        </CardContent>
      </Card>
    );
  }

  return <EmailVerification token={token} />;
}

/**
 * VerifyEmailPage Component
 * Page for verifying email address with a token from email
 */
export default function VerifyEmailPage() {
  return (
    <div
      className="min-h-screen flex items-center justify-center p-5"
      style={{
        backgroundImage: 'url(/images/batik-sri-lanka.jpg)',
        backgroundSize: 'cover',
        backgroundPosition: 'center',
        backgroundRepeat: 'no-repeat'
      }}
    >

      {/* Split Panel Container */}
      <div className="relative z-10 w-full max-w-[1000px] grid grid-cols-1 md:grid-cols-2 bg-white rounded-[20px] overflow-hidden shadow-[0_20px_60px_rgba(0,0,0,0.3)]">
        {/* Left Panel - Branding */}
        <div className="hidden md:flex flex-col justify-center text-white px-10 py-[60px] relative overflow-hidden" style={{ background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 50%, #006400 100%)' }}>
          {/* Animated Background - Pulsing radial gradient */}
          <div
            className="absolute -top-1/2 -right-1/2 w-[200%] h-[200%] pointer-events-none"
            style={{
              background: 'radial-gradient(circle, rgba(255,255,255,0.1) 0%, transparent 70%)',
              animation: 'pulse 8s ease-in-out infinite'
            }}
          />

          {/* Logo Section */}
          <div className="relative z-10 mb-10">
            <div className="flex items-center text-[2rem] font-bold mb-5">
              <Image
                src="/lankaconnect-logo-transparent.png"
                alt="LankaConnect"
                width={80}
                height={80}
                className="mr-5"
                priority
              />
              LankaConnect
            </div>
          </div>

          {/* Welcome Text */}
          <div className="relative z-10">
            <h1 className="text-[2.5rem] font-bold mb-5 drop-shadow-[2px_2px_4px_rgba(0,0,0,0.2)]">
              Verify Your Email
            </h1>
            <p className="text-[1.1rem] opacity-95 leading-[1.6] mb-[30px]">
              Welcome to LankaConnect! Just one more step to activate your account and join the vibrant Sri Lankan American community.
            </p>
          </div>

          {/* Features */}
          <div className="relative z-10">
            <div className="flex items-center mb-[15px] p-[15px] bg-white/10 backdrop-blur-[10px] rounded-[10px]">
              <div className="w-10 h-10 rounded-[10px] flex items-center justify-center text-[1.3rem] mr-[15px] flex-shrink-0" style={{ background: '#FFD700' }}>
                ✉️
              </div>
              <div>
                <strong className="block">Email Confirmation</strong>
                <div className="text-[0.9rem] opacity-90">Verify your email address</div>
              </div>
            </div>
            <div className="flex items-center mb-[15px] p-[15px] bg-white/10 backdrop-blur-[10px] rounded-[10px]">
              <div className="w-10 h-10 rounded-[10px] flex items-center justify-center text-[1.3rem] mr-[15px] flex-shrink-0" style={{ background: '#FFD700' }}>
                🛡️
              </div>
              <div>
                <strong className="block">Account Security</strong>
                <div className="text-[0.9rem] opacity-90">Protect your account access</div>
              </div>
            </div>
            <div className="flex items-center mb-[15px] p-[15px] bg-white/10 backdrop-blur-[10px] rounded-[10px]">
              <div className="w-10 h-10 rounded-[10px] flex items-center justify-center text-[1.3rem] mr-[15px] flex-shrink-0" style={{ background: '#FFD700' }}>
                🚀
              </div>
              <div>
                <strong className="block">Get Started</strong>
                <div className="text-[0.9rem] opacity-90">Access all features instantly</div>
              </div>
            </div>
          </div>
        </div>

        {/* Right Panel - Email Verification */}
        <div className="flex flex-col justify-center px-[50px] py-[60px]" style={{ background: 'linear-gradient(to bottom, #ffffff 0%, #fef9f5 100%)' }}>
          {/* Mobile Logo */}
          <div className="mb-8 md:hidden text-center">
            <Image
              src="/lankaconnect-logo-transparent.png"
              alt="LankaConnect"
              width={60}
              height={60}
              className="mx-auto mb-2"
              priority
            />
            <span className="text-2xl font-bold" style={{ color: '#8B1538' }}>LankaConnect</span>
          </div>

          <Suspense
            fallback={
              <Card className="w-full">
                <CardHeader>
                  <CardTitle>Loading...</CardTitle>
                </CardHeader>
              </Card>
            }
          >
            <EmailVerificationContent />
          </Suspense>
        </div>
      </div>
    </div>
  );
}
