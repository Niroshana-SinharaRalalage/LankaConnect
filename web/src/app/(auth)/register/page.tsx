import { RegisterForm } from '@/presentation/components/features/auth/RegisterForm';
import { OfficialLogo } from '@/presentation/components/atoms/OfficialLogo';

// Force dynamic rendering
export const dynamic = 'force-dynamic';

export default function RegisterPage() {
  return (
    <div
      className="min-h-screen flex items-center justify-center p-5"
      style={{
        background: 'linear-gradient(to-r, #FF7900, #8B1538, #006400)'
      }}
    >
      {/* Decorative Background Pattern */}
      <div className="absolute inset-0 opacity-10">
        <div
          className="absolute inset-0"
          style={{
            backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
          }}
        ></div>
      </div>

      {/* Split Panel Container */}
      <div className="relative z-10 w-full max-w-[1000px] grid grid-cols-1 md:grid-cols-2 bg-white rounded-[20px] overflow-hidden shadow-[0_20px_60px_rgba(0,0,0,0.3)]">
        {/* Left Panel - Branding */}
        <div className="hidden md:flex flex-col justify-center text-white px-10 py-[60px] relative overflow-hidden bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800">
          {/* Decorative gradient blobs */}
          <div className="absolute inset-0 overflow-hidden">
            <div className="absolute -top-24 -left-24 w-96 h-96 bg-orange-400/20 rounded-full blur-3xl"></div>
            <div className="absolute -bottom-24 -right-24 w-96 h-96 bg-emerald-400/20 rounded-full blur-3xl"></div>
            <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-rose-400/10 rounded-full blur-3xl"></div>
          </div>

          {/* Logo Section */}
          <div className="relative z-10 mb-8">
            <OfficialLogo size="md" textColor="text-white" subtitleColor="text-white/90" linkTo="/" />
          </div>

          {/* Welcome Text */}
          <div className="relative z-10">
            <h1 className="text-[1.75rem] font-semibold mb-4 drop-shadow-[2px_2px_4px_rgba(0,0,0,0.2)]">
              Join Our Community!
            </h1>
            <p className="text-base opacity-95 leading-relaxed mb-6">
              Become part of the vibrant Sri Lankan American community. Create your account to access events, connect with others, and explore cultural opportunities.
            </p>
          </div>

          {/* Features */}
          <div className="relative z-10">
            <div className="flex items-center mb-[15px] p-[15px] bg-white/10 backdrop-blur-[10px] rounded-[10px]">
              <div className="w-10 h-10 rounded-[10px] flex items-center justify-center text-[1.3rem] mr-[15px] flex-shrink-0" style={{ background: '#FFD700' }}>
                🎉
              </div>
              <div>
                <strong className="block">Cultural Events</strong>
                <div className="text-[0.9rem] opacity-90">Discover celebrations near you</div>
              </div>
            </div>
            <div className="flex items-center mb-[15px] p-[15px] bg-white/10 backdrop-blur-[10px] rounded-[10px]">
              <div className="w-10 h-10 rounded-[10px] flex items-center justify-center text-[1.3rem] mr-[15px] flex-shrink-0" style={{ background: '#FFD700' }}>
                🤝
              </div>
              <div>
                <strong className="block">Community Network</strong>
                <div className="text-[0.9rem] opacity-90">Connect with fellow Sri Lankans</div>
              </div>
            </div>
            <div className="flex items-center mb-[15px] p-[15px] bg-white/10 backdrop-blur-[10px] rounded-[10px]">
              <div className="w-10 h-10 rounded-[10px] flex items-center justify-center text-[1.3rem] mr-[15px] flex-shrink-0" style={{ background: '#FFD700' }}>
                💼
              </div>
              <div>
                <strong className="block">Local Businesses</strong>
                <div className="text-[0.9rem] opacity-90">Support our community enterprises</div>
              </div>
            </div>
          </div>
        </div>

        {/* Right Panel - Register Form */}
        <div className="flex flex-col justify-center px-[50px] py-[60px]" style={{ background: 'linear-gradient(to bottom, #ffffff 0%, #fef9f5 100%)' }}>
          {/* Mobile Logo */}
          <div className="mb-6 md:hidden text-center">
            <OfficialLogo size="sm" linkTo="/" />
          </div>

          <RegisterForm />
        </div>
      </div>
    </div>
  );
}
