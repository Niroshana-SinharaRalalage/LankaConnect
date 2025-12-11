import { ForgotPasswordForm } from '@/presentation/components/features/auth/ForgotPasswordForm';
import Image from 'next/image';

// Force dynamic rendering
export const dynamic = 'force-dynamic';

export default function ForgotPasswordPage() {
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
              Reset Your Password
            </h1>
            <p className="text-[1.1rem] opacity-95 leading-[1.6] mb-[30px]">
              Don't worry, it happens! Enter your email address and we'll send you instructions to reset your password and regain access to your account.
            </p>
          </div>

          {/* Features */}
          <div className="relative z-10">
            <div className="flex items-center mb-[15px] p-[15px] bg-white/10 backdrop-blur-[10px] rounded-[10px]">
              <div className="w-10 h-10 rounded-[10px] flex items-center justify-center text-[1.3rem] mr-[15px] flex-shrink-0" style={{ background: '#FFD700' }}>
                🔒
              </div>
              <div>
                <strong className="block">Secure Process</strong>
                <div className="text-[0.9rem] opacity-90">Your security is our priority</div>
              </div>
            </div>
            <div className="flex items-center mb-[15px] p-[15px] bg-white/10 backdrop-blur-[10px] rounded-[10px]">
              <div className="w-10 h-10 rounded-[10px] flex items-center justify-center text-[1.3rem] mr-[15px] flex-shrink-0" style={{ background: '#FFD700' }}>
                📧
              </div>
              <div>
                <strong className="block">Email Instructions</strong>
                <div className="text-[0.9rem] opacity-90">Check your inbox for reset link</div>
              </div>
            </div>
            <div className="flex items-center mb-[15px] p-[15px] bg-white/10 backdrop-blur-[10px] rounded-[10px]">
              <div className="w-10 h-10 rounded-[10px] flex items-center justify-center text-[1.3rem] mr-[15px] flex-shrink-0" style={{ background: '#FFD700' }}>
                ⚡
              </div>
              <div>
                <strong className="block">Quick Recovery</strong>
                <div className="text-[0.9rem] opacity-90">Get back to the community fast</div>
              </div>
            </div>
          </div>
        </div>

        {/* Right Panel - Forgot Password Form */}
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

          <ForgotPasswordForm />
        </div>
      </div>
    </div>
  );
}
