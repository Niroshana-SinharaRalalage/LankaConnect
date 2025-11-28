import { Button } from './ui/button';
import { ArrowRight, Sparkles } from 'lucide-react';

export function HeroSection() {
  return (
    <div className="relative overflow-hidden bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800">
      {/* Decorative Background Pattern */}
      <div className="absolute inset-0 opacity-10">
        <div className="absolute inset-0" style={{
          backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
        }}></div>
      </div>

      {/* Decorative gradient blobs */}
      <div className="absolute inset-0 overflow-hidden">
        <div className="absolute -top-24 -left-24 w-96 h-96 bg-orange-400/20 rounded-full blur-3xl"></div>
        <div className="absolute -bottom-24 -right-24 w-96 h-96 bg-emerald-400/20 rounded-full blur-3xl"></div>
        <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-rose-400/10 rounded-full blur-3xl"></div>
      </div>

      <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 md:py-24">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">
          {/* Left Content */}
          <div className="text-center lg:text-left">
            <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-white/20 backdrop-blur-sm border border-white/30 mb-6">
              <Sparkles className="h-4 w-4 text-white" />
              <span className="text-sm text-white">Connecting Sri Lankans Worldwide</span>
            </div>

            <h1 className="text-4xl md:text-5xl lg:text-6xl text-white mb-6">
              Your Community,
              <br />
              <span className="text-white drop-shadow-lg">
                Your Heritage
              </span>
            </h1>

            <p className="text-lg text-white/95 mb-8 max-w-xl mx-auto lg:mx-0">
              Join the largest Sri Lankan diaspora platform. Discover events, connect with businesses, 
              engage in discussions, and celebrate our rich culture together.
            </p>

            <div className="flex flex-col sm:flex-row gap-4 justify-center lg:justify-start">
              <Button size="lg" className="bg-white text-orange-600 hover:bg-neutral-100 shadow-lg">
                Get Started
                <ArrowRight className="ml-2 h-5 w-5" />
              </Button>
              <Button size="lg" variant="outline" className="border-white text-white hover:bg-white/10 backdrop-blur-sm">
                Explore Community
              </Button>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-3 gap-6 mt-12 pt-12 border-t border-white/20">
              <div>
                <div className="text-3xl text-white mb-1">25K+</div>
                <div className="text-sm text-white/90">Members</div>
              </div>
              <div>
                <div className="text-3xl text-white mb-1">1.2K+</div>
                <div className="text-sm text-white/90">Events</div>
              </div>
              <div>
                <div className="text-3xl text-white mb-1">500+</div>
                <div className="text-sm text-white/90">Businesses</div>
              </div>
            </div>
          </div>

          {/* Right Content - Feature Cards */}
          <div className="relative hidden lg:block">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-4">
                <FeatureCard
                  icon="🎉"
                  title="Avurudu Festival"
                  subtitle="Tomorrow at 6:00 PM"
                  color="from-orange-500 to-amber-500"
                />
                <FeatureCard
                  icon="🛍️"
                  title="Fresh Groceries"
                  subtitle="50+ Sri Lankan products"
                  color="from-emerald-500 to-green-500"
                />
              </div>
              <div className="space-y-4 mt-8">
                <FeatureCard
                  icon="💬"
                  title="Community Chat"
                  subtitle="234 active discussions"
                  color="from-rose-500 to-pink-500"
                />
                <FeatureCard
                  icon="🎭"
                  title="Cultural Events"
                  subtitle="Dance & Music classes"
                  color="from-amber-500 to-yellow-500"
                />
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function FeatureCard({ icon, title, subtitle, color }: { icon: string; title: string; subtitle: string; color: string }) {
  return (
    <div className="bg-white rounded-2xl p-6 shadow-lg hover:shadow-xl transition-all hover:-translate-y-1 cursor-pointer border border-neutral-100">
      <div className={`w-12 h-12 rounded-xl bg-gradient-to-br ${color} flex items-center justify-center text-2xl mb-4`}>
        {icon}
      </div>
      <div className="text-neutral-900 mb-1">{title}</div>
      <div className="text-sm text-neutral-500">{subtitle}</div>
    </div>
  );
}