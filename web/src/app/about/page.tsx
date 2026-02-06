'use client';

import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Logo } from '@/presentation/components/atoms/Logo';
import { Card, CardContent } from '@/presentation/components/ui/Card';
import {
  Heart,
  Users,
  Calendar,
  Store,
  MessageSquare,
  Sparkles,
  MapPin
} from 'lucide-react';

/**
 * About Us Page
 * Phase 6A.76: Comprehensive description of LankaConnect and its purpose
 * Aligned with landing page content and LankaConnect branding
 */
export default function AboutPage() {
  const features = [
    {
      icon: Calendar,
      title: 'Community Events',
      description: 'Discover and participate in cultural celebrations, festivals, religious events, and community gatherings happening in your area.'
    },
    {
      icon: Store,
      title: 'Marketplace',
      description: 'Buy, sell, and trade within your trusted community. From traditional Sri Lankan goods to services, connect with reliable sellers and buyers.'
    },
    {
      icon: Users,
      title: 'Business Directory',
      description: 'Find and support Sri Lankan-owned businesses in the USA. Whether you need professional services or local shops, discover businesses you can trust.'
    },
    {
      icon: MessageSquare,
      title: 'Community Forums',
      description: 'Share experiences, ask questions, and stay connected with fellow Sri Lankans. Build meaningful relationships and engage in discussions.'
    }
  ];

  const values = [
    {
      icon: Heart,
      title: 'Community First',
      description: 'We believe in the power of community. LankaConnect is built to strengthen bonds between Sri Lankans, whether they\'re down the street or across the country.'
    },
    {
      icon: Sparkles,
      title: 'Preserving Culture',
      description: 'Our rich Sri Lankan heritage deserves to thrive everywhere. We\'re dedicated to helping preserve and celebrate our traditions, language, and customs for future generations.'
    },
    {
      icon: MapPin,
      title: 'Bridging Distances',
      description: 'Distance should never weaken our connections. We bring Sri Lankans together, making it easy to find community no matter where you live in the USA.'
    }
  ];

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      {/* Hero Section - Same style as landing page */}
      <div className="relative overflow-hidden bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800">
        {/* Decorative Background Pattern - Same as landing page and footer */}
        <div className="absolute inset-0 opacity-10">
          <div
            className="absolute inset-0"
            style={{
              backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
            }}
          ></div>
        </div>

        {/* Decorative gradient blobs - Same as landing page */}
        <div className="absolute inset-0 overflow-hidden">
          <div className="absolute -top-24 -left-24 w-96 h-96 bg-orange-400/20 rounded-full blur-3xl"></div>
          <div className="absolute -bottom-24 -right-24 w-96 h-96 bg-emerald-400/20 rounded-full blur-3xl"></div>
          <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-rose-400/10 rounded-full blur-3xl"></div>
        </div>

        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 md:py-24 text-center">
          {/* Logo */}
          <div className="flex justify-center mb-6">
            <Logo size="xl" showText={false} />
          </div>

          <h1 className="text-4xl md:text-5xl font-bold text-white mb-4">
            About LankaConnect
          </h1>

          {/* Badge - Same style as landing page */}
          <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-white/20 backdrop-blur-sm border border-white/30 mb-6">
            <Sparkles className="h-4 w-4 text-white" />
            <span className="text-sm text-white">One Country, One Community</span>
          </div>

          <p className="text-xl text-white/90 max-w-2xl mx-auto">
            Join the largest Sri Lankan community platform in the USA
          </p>
        </div>
      </div>

      {/* Mission Section */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 md:py-16">
        <Card className="border-0 shadow-lg">
          <CardContent className="p-8 md:p-12">
            <h2 className="text-2xl md:text-3xl font-bold text-neutral-900 mb-6 text-center">
              Our Mission
            </h2>
            <p className="text-lg text-neutral-700 leading-relaxed max-w-4xl mx-auto text-center">
              LankaConnect is the premier digital platform connecting Sri Lankan communities
              across the United States. We bridge distances and strengthen cultural bonds
              by providing a unified space for events, marketplace, business networking,
              and community engagement. Whether you're in New York, Los Angeles, Chicago, or
              anywhere in between, LankaConnect helps you stay connected to your roots and
              your community.
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Features Section */}
      <div className="bg-neutral-50 py-12 md:py-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <h2 className="text-2xl md:text-3xl font-bold text-neutral-900 mb-8 text-center">
            What We Offer
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {features.map((feature) => (
              <Card key={feature.title} className="border-0 shadow-md hover:shadow-lg transition-shadow">
                <CardContent className="p-6">
                  <div className="flex items-start gap-4">
                    <div className="flex-shrink-0">
                      <div className="w-12 h-12 rounded-lg bg-gradient-to-r from-orange-500 to-rose-600 flex items-center justify-center">
                        <feature.icon className="h-6 w-6 text-white" />
                      </div>
                    </div>
                    <div>
                      <h3 className="text-lg font-semibold text-neutral-900 mb-2">
                        {feature.title}
                      </h3>
                      <p className="text-neutral-600 leading-relaxed">
                        {feature.description}
                      </p>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </div>

      {/* Values Section */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 md:py-16">
        <h2 className="text-2xl md:text-3xl font-bold text-neutral-900 mb-8 text-center">
          Our Values
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {values.map((value) => (
            <Card key={value.title} className="border-0 shadow-md text-center">
              <CardContent className="p-6">
                <div className="flex justify-center mb-4">
                  <div className="w-14 h-14 rounded-full bg-gradient-to-r from-orange-500 via-rose-600 to-emerald-600 flex items-center justify-center">
                    <value.icon className="h-7 w-7 text-white" />
                  </div>
                </div>
                <h3 className="text-lg font-semibold text-neutral-900 mb-2">
                  {value.title}
                </h3>
                <p className="text-neutral-600 leading-relaxed">
                  {value.description}
                </p>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>

      {/* Vision Section - Using gradient theme */}
      <div className="relative overflow-hidden bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 py-12 md:py-16">
        {/* Decorative Background Pattern */}
        <div className="absolute inset-0 opacity-10">
          <div
            className="absolute inset-0"
            style={{
              backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
            }}
          ></div>
        </div>

        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-2xl md:text-3xl font-bold text-white mb-4">
            Our Vision
          </h2>
          <p className="text-lg text-white/90 max-w-3xl mx-auto leading-relaxed">
            To become the go-to platform for Sri Lankan communities in the USA, fostering
            a sense of belonging and community regardless of where you live. We envision
            a future where every Sri Lankan in America can easily connect with their culture,
            community, and fellow compatriots — and eventually expand to serve Sri Lankans worldwide.
          </p>
        </div>
      </div>

      {/* Join Us CTA - White card style like Mission section */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 md:py-16">
        <Card className="border-0 shadow-lg">
          <CardContent className="p-8 md:p-12 text-center">
            <h2 className="text-2xl md:text-3xl font-bold text-neutral-900 mb-4">
              Join Our Growing Community
            </h2>
            <p className="text-neutral-600 mb-8 max-w-2xl mx-auto">
              Whether you're looking to discover events, find trusted businesses,
              connect with fellow Sri Lankans, or simply stay in touch with your heritage,
              LankaConnect is here for you.
            </p>
            <div className="flex flex-col sm:flex-row gap-4 justify-center">
              <a
                href="/events"
                className="inline-flex items-center justify-center px-6 py-3 bg-orange-600 hover:bg-orange-700 text-white font-medium rounded-lg transition-colors"
              >
                Explore Events
              </a>
              <a
                href="/contact"
                className="inline-flex items-center justify-center px-6 py-3 bg-neutral-100 hover:bg-neutral-200 text-neutral-700 font-medium rounded-lg transition-colors"
              >
                Get in Touch
              </a>
            </div>
          </CardContent>
        </Card>
      </div>

      <Footer />
    </div>
  );
}
