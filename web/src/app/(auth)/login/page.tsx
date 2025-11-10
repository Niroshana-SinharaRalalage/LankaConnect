import { LoginForm } from '@/presentation/components/features/auth/LoginForm';
import Image from 'next/image';

export default function LoginPage() {
  return (
    <div
      className="min-h-screen flex items-center justify-center p-5"
      style={{
        backgroundImage: 'url(/images/sri-lankan-elephants.jpg)',
        backgroundSize: 'cover',
        backgroundPosition: 'center',
        backgroundRepeat: 'no-repeat'
      }}
    >
      {/* Split Panel Container */}
      <div className="relative w-full max-w-[1000px] grid grid-cols-1 md:grid-cols-2 bg-white rounded-[20px] overflow-hidden shadow-[0_20px_60px_rgba(0,0,0,0.3)]">
        {/* Left Panel - Branding */}
        <div
          className="hidden md:flex flex-col justify-center text-white px-10 py-[60px]"
          style={{
            background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 50%, #006400 100%)'
          }}
        >
          {/* Logo Section */}
          <div className="mb-10">
            <div className="flex items-center text-[2rem] font-bold mb-5">
              <Image
                src="/images/lankaconnect-logo.png"
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
          <div>
            <h1 className="text-[2.5rem] font-bold mb-5 drop-shadow-[2px_2px_4px_rgba(0,0,0,0.2)]">
              Welcome Back!
            </h1>
            <p className="text-[1.1rem] opacity-95 leading-[1.6] mb-[30px]">
              Connect with the vibrant Sri Lankan American community. Your gateway to events, culture, and connections.
            </p>
          </div>

          {/* Features */}
          <div>
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

        {/* Right Panel - Login Form */}
        <div className="flex flex-col justify-center px-[50px] py-[60px]" style={{ background: 'linear-gradient(to bottom, #ffffff 0%, #fef9f5 100%)' }}>
          {/* Mobile Logo */}
          <div className="mb-8 md:hidden text-center">
            <Image
              src="/images/lankaconnect-logo.png"
              alt="LankaConnect"
              width={60}
              height={60}
              className="mx-auto mb-2"
              priority
            />
            <span className="text-2xl font-bold" style={{ color: '#8B1538' }}>LankaConnect</span>
          </div>

          <LoginForm />
        </div>
      </div>
    </div>
  );
}
