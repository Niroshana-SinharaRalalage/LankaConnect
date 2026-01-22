'use client';

import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardContent } from '@/presentation/components/ui/Card';
import {
  Globe,
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
 */
export default function AboutPage() {
  const features = [
    {
      icon: Calendar,
      title: 'Community Events',
      description: 'Discover and participate in cultural celebrations, festivals, religious events, and community gatherings happening in your area and around the world.'
    },
    {
      icon: Store,
      title: 'Marketplace',
      description: 'Buy, sell, and trade within your trusted community. From traditional Sri Lankan goods to services, connect with reliable sellers and buyers.'
    },
    {
      icon: Users,
      title: 'Business Directory',
      description: 'Find and support Sri Lankan-owned businesses worldwide. Whether you need professional services or local shops, discover businesses you can trust.'
    },
    {
      icon: MessageSquare,
      title: 'Community Forums',
      description: 'Share experiences, ask questions, and stay connected with fellow Sri Lankans. Build meaningful relationships across borders and generations.'
    }
  ];

  const values = [
    {
      icon: Heart,
      title: 'Community First',
      description: 'We believe in the power of community. LankaConnect is built to strengthen bonds between Sri Lankans, whether they\'re down the street or across the ocean.'
    },
    {
      icon: Globe,
      title: 'Preserving Culture',
      description: 'Our rich Sri Lankan heritage deserves to thrive everywhere. We\'re dedicated to helping preserve and celebrate our traditions, language, and customs for future generations.'
    },
    {
      icon: MapPin,
      title: 'Bridging Distances',
      description: 'Distance should never weaken our connections. We bring Sri Lankans together, making it easy to find community no matter where life takes you.'
    }
  ];

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      {/* Hero Section */}
      <div className="bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 py-16 md:py-24">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <div className="flex justify-center mb-6">
            <Globe className="h-16 w-16 text-white" />
          </div>
          <h1 className="text-4xl md:text-5xl font-bold text-white mb-4">
            About LankaConnect
          </h1>
          <p className="text-xl md:text-2xl text-white/90 max-w-3xl mx-auto">
            Connecting Sri Lankans Worldwide
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
              LankaConnect is the premier digital platform connecting Sri Lankan diaspora
              communities across the globe. We bridge distances and strengthen cultural bonds
              by providing a unified space for events, marketplace, business networking,
              and community engagement. Whether you're in Colombo, Toronto, Melbourne, or
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

      {/* Vision Section */}
      <div className="bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 py-12 md:py-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <div className="flex justify-center mb-6">
            <Sparkles className="h-12 w-12 text-white" />
          </div>
          <h2 className="text-2xl md:text-3xl font-bold text-white mb-4">
            Our Vision
          </h2>
          <p className="text-lg text-white/90 max-w-3xl mx-auto leading-relaxed">
            To become the go-to platform for Sri Lankan diaspora worldwide, fostering
            a sense of belonging and community regardless of geographic location. We envision
            a world where every Sri Lankan, no matter where they live, can easily connect
            with their culture, community, and fellow compatriots.
          </p>
        </div>
      </div>

      {/* Join Us CTA */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 md:py-16">
        <Card className="border-0 shadow-lg bg-neutral-900 text-white">
          <CardContent className="p-8 md:p-12 text-center">
            <h2 className="text-2xl md:text-3xl font-bold mb-4">
              Join Our Growing Community
            </h2>
            <p className="text-neutral-300 mb-8 max-w-2xl mx-auto">
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
                className="inline-flex items-center justify-center px-6 py-3 bg-white/10 hover:bg-white/20 text-white font-medium rounded-lg transition-colors border border-white/20"
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
