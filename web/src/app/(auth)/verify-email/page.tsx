'use client';

import { Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import { EmailVerification } from '@/presentation/components/features/auth/EmailVerification';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import Link from 'next/link';
import { ArrowLeft } from 'lucide-react';
import { OfficialLogo } from '@/presentation/components/atoms/OfficialLogo';

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
        background: 'linear-gradient(to-r, #FF7900, #8B1538, #006400)'
      }}
    >
      {/* Split Panel Container */}
      <div className="relative z-10 w-full max-w-[1000px] grid grid-cols-1 md:grid-cols-2 bg-white rounded-[20px] overflow-hidden shadow-[0_20px_60px_rgba(0,0,0,0.3)]">
        {/* Left Panel - Branding */}
        <div className="hidden md:flex flex-col justify-center text-white px-10 py-[60px] relative overflow-hidden bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800">
          {/* Decorative Background Pattern */}
          <div className="absolute inset-0 opacity-10">
            <div
              className="absolute inset-0"
              style={{
                backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
              }}
            ></div>
          </div>

          {/* Decorative gradient blobs */}
          <div className="absolute inset-0 overflow-hidden">
            <div className="absolute -top-24 -left-24 w-96 h-96 bg-orange-400/20 rounded-full blur-3xl"></div>
            <div className="absolute -bottom-24 -right-24 w-96 h-96 bg-emerald-400/20 rounded-full blur-3xl"></div>
            <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-rose-400/10 rounded-full blur-3xl"></div>
          </div>

          {/* Logo Section */}
          <div className="mb-8 relative z-10">
            <OfficialLogo size="md" textColor="text-white" subtitleColor="text-white/90" linkTo="/" />
          </div>

          {/* Welcome Text */}
          <div className="relative z-10">
            <h1 className="text-[1.75rem] font-semibold mb-4 drop-shadow-[2px_2px_4px_rgba(0,0,0,0.2)]">
              Verify Your Email
            </h1>
            <p className="text-base opacity-95 leading-relaxed mb-6">
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
          {/* Back to Home Link */}
          <Link
            href="/"
            className="inline-flex items-center text-sm text-gray-600 hover:text-[#FF7900] transition-colors mb-4"
          >
            <ArrowLeft className="w-4 h-4 mr-1" />
            Back to Home
          </Link>

          {/* Mobile Logo */}
          <div className="mb-6 md:hidden text-center">
            <OfficialLogo size="sm" linkTo="/" />
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
