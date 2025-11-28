'use client';

/**
 * Landing Page - Redesigned to match Figma mockup exactly
 * Phase 6C.1 - Layout Redesign
 */

import * as React from 'react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Calendar, MessageCircle, Building2, Users, MapPin, Star, ArrowRight, Sparkles } from 'lucide-react';
import { Badge } from '@/presentation/components/ui/Badge';

export default function Home() {
  return (
    <div className="min-h-screen bg-gray-50">
      <Header />

      {/* Hero Section - Exact Figma Match */}
      <section
        className="relative text-white py-20 overflow-hidden"
        style={{
          background: 'linear-gradient(90deg, #FF5722 0%, #E91E63 25%, #C2185B 50%, #7B1FA2 75%, #00695C 100%)'
        }}
      >
        {/* Diagonal Stripe Pattern - Like Figma */}
        <div
          className="absolute inset-0 opacity-10"
          style={{
            backgroundImage: 'repeating-linear-gradient(45deg, transparent, transparent 40px, rgba(255,255,255,0.3) 40px, rgba(255,255,255,0.3) 80px)'
          }}
        />

        <div className="container mx-auto px-4 sm:px-6 lg:px-8 relative z-10">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">
            {/* Left Content */}
            <div>
              {/* Badge */}
              <div className="inline-flex items-center gap-2 bg-white/20 backdrop-blur-sm rounded-full px-5 py-2 mb-8">
                <Sparkles className="w-5 h-5" />
                <span className="text-sm font-medium">Connecting Sri Lankans Worldwide</span>
              </div>

              {/* Heading */}
              <h1 className="text-6xl font-bold mb-6 leading-tight">
                Your Community,<br />
                Your Heritage
              </h1>

              {/* Description */}
              <p className="text-lg mb-10 leading-relaxed">
                Join the largest Sri Lankan diaspora platform. Discover events, connect
                with businesses, engage in discussions, and celebrate our rich culture
                together.
              </p>

              {/* Buttons */}
              <div className="flex gap-4">
                <button className="bg-white text-saffron-600 px-8 py-3 rounded-lg font-semibold hover:bg-gray-100 transition-all flex items-center gap-2 shadow-lg">
                  Get Started <ArrowRight className="w-5 h-5" />
                </button>
                <button className="border-2 border-white text-white px-8 py-3 rounded-lg font-semibold hover:bg-white/10 transition-all">
                  Learn More
                </button>
              </div>
            </div>

            {/* Right - Feature Cards (2x2 Grid) */}
            <div className="grid grid-cols-2 gap-5">
              {/* Card 1 - Avurudu Festival */}
              <div className="bg-white rounded-2xl p-7 shadow-2xl hover:shadow-3xl transition-all">
                <div className="w-14 h-14 bg-gradient-to-br from-orange-400 to-orange-600 rounded-xl flex items-center justify-center mb-4 shadow-md">
                  <Calendar className="w-7 h-7 text-white" />
                </div>
                <h3 className="font-bold text-gray-900 mb-2 text-lg">Avurudu Festival</h3>
                <p className="text-sm text-gray-600">Tomorrow at 6:00 PM</p>
              </div>

              {/* Card 2 - Community Chat */}
              <div className="bg-white rounded-2xl p-7 shadow-2xl hover:shadow-3xl transition-all">
                <div className="w-14 h-14 bg-gradient-to-br from-pink-400 to-pink-600 rounded-xl flex items-center justify-center mb-4 shadow-md">
                  <MessageCircle className="w-7 h-7 text-white" />
                </div>
                <h3 className="font-bold text-gray-900 mb-2 text-lg">Community Chat</h3>
                <p className="text-sm text-gray-600">234 active discussions</p>
              </div>

              {/* Card 3 - Fresh Groceries */}
              <div className="bg-white rounded-2xl p-7 shadow-2xl hover:shadow-3xl transition-all">
                <div className="w-14 h-14 bg-gradient-to-br from-emerald-400 to-emerald-600 rounded-xl flex items-center justify-center mb-4 shadow-md">
                  <Building2 className="w-7 h-7 text-white" />
                </div>
                <h3 className="font-bold text-gray-900 mb-2 text-lg">Fresh Groceries</h3>
                <p className="text-sm text-gray-600">50+ Sri Lankan products</p>
              </div>

              {/* Card 4 - Cultural Events */}
              <div className="bg-white rounded-2xl p-7 shadow-2xl hover:shadow-3xl transition-all">
                <div className="w-14 h-14 bg-gradient-to-br from-amber-400 to-amber-600 rounded-xl flex items-center justify-center mb-4 shadow-md">
                  <Users className="w-7 h-7 text-white" />
                </div>
                <h3 className="font-bold text-gray-900 mb-2 text-lg">Cultural Events</h3>
                <p className="text-sm text-gray-600">Dance & Music classes</p>
              </div>
            </div>
          </div>

          {/* Stats Bar */}
          <div className="grid grid-cols-3 gap-12 mt-20 pt-10 border-t border-white/30">
            <div>
              <div className="text-5xl font-bold mb-2">25K+</div>
              <div className="text-white/90 text-lg">Members</div>
            </div>
            <div>
              <div className="text-5xl font-bold mb-2">1.2K+</div>
              <div className="text-white/90 text-lg">Events</div>
            </div>
            <div>
              <div className="text-5xl font-bold mb-2">500+</div>
              <div className="text-white/90 text-lg">Businesses</div>
            </div>
          </div>
        </div>
      </section>

      {/* Main Content - Events, Forums, Marketplace, News */}
      <section className="py-16 bg-white">
        <div className="container mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-10">
            {/* Left 2/3 Column */}
            <div className="lg:col-span-2 space-y-10">
              {/* Events Section - Matching Figma exactly */}
              <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
                <div className="flex items-center justify-between px-8 py-6 border-b border-gray-200">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 bg-orange-100 rounded-lg flex items-center justify-center">
                      <Calendar className="w-5 h-5 text-orange-600" />
                    </div>
                    <h2 className="text-2xl font-bold text-gray-900">Upcoming Events</h2>
                  </div>
                  <button className="text-orange-600 hover:text-orange-700 flex items-center gap-2 font-semibold text-sm">
                    View All <ArrowRight className="w-4 h-4" />
                  </button>
                </div>

                {/* Event Cards */}
                <div className="divide-y divide-gray-100">
                  {/* Event 1 */}
                  <div className="px-8 py-6 hover:bg-gray-50 transition-colors">
                    <div className="flex gap-5">
                      <div className="w-16 h-16 bg-gradient-to-br from-orange-400 to-red-500 rounded-xl flex items-center justify-center flex-shrink-0 shadow-md">
                        <Calendar className="w-8 h-8 text-white" />
                      </div>
                      <div className="flex-1">
                        <div className="flex items-start justify-between mb-3">
                          <h3 className="text-lg font-bold text-gray-900">Sinhala & Tamil New Year Celebration</h3>
                          <Badge variant="cultural" className="text-xs">Cultural</Badge>
                        </div>
                        <div className="space-y-2 text-sm text-gray-600 mb-4">
                          <div className="flex items-center gap-2">
                            <Calendar className="w-4 h-4" />
                            <span>April 14, 2024 • 6:00 PM - 11:00 PM</span>
                          </div>
                          <div className="flex items-center gap-2">
                            <MapPin className="w-4 h-4" />
                            <span>Sri Lankan Community Center, Toronto</span>
                          </div>
                          <div className="flex items-center gap-2">
                            <Users className="w-4 h-4" />
                            <span>234 attending</span>
                          </div>
                        </div>
                        <button className="px-5 py-2 bg-white border-2 border-gray-300 rounded-lg hover:bg-gray-50 font-semibold text-sm transition-colors">
                          Register
                        </button>
                      </div>
                    </div>
                  </div>

                  {/* Additional events would go here following same pattern */}
                </div>
              </div>

              {/* Forum Section */}
              <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
                <div className="flex items-center justify-between px-8 py-6 border-b border-gray-200">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 bg-pink-100 rounded-lg flex items-center justify-center">
                      <MessageCircle className="w-5 h-5 text-pink-600" />
                    </div>
                    <h2 className="text-2xl font-bold text-gray-900">Forum Highlights</h2>
                  </div>
                  <button className="text-orange-600 hover:text-orange-700 flex items-center gap-2 font-semibold text-sm">
                    <ArrowRight className="w-4 h-4" />
                  </button>
                </div>

                {/* Forum Posts */}
                <div className="divide-y divide-gray-100">
                  {/* Post 1 */}
                  <div className="px-8 py-6 hover:bg-gray-50 transition-colors cursor-pointer">
                    <div className="flex gap-4">
                      <div className="w-12 h-12 bg-gradient-to-br from-pink-500 to-purple-600 rounded-full flex items-center justify-center text-white font-bold flex-shrink-0 shadow-md">
                        SP
                      </div>
                      <div className="flex-1">
                        <div className="flex items-start justify-between mb-2">
                          <div className="flex-1">
                            <h3 className="font-bold text-gray-900 mb-1">Best places to buy Sri Lankan groceries?</h3>
                            <div className="flex items-center gap-2 text-sm text-gray-500">
                              <span className="font-medium">Saman P.</span>
                              <span>•</span>
                              <span>2h ago</span>
                            </div>
                          </div>
                          <Badge variant="hot" className="ml-3">Hot</Badge>
                        </div>
                        <div className="flex items-center gap-5 mt-4">
                          <Badge variant="food">Food & Lifestyle</Badge>
                          <div className="flex items-center gap-4 text-sm text-gray-600">
                            <span className="flex items-center gap-1.5">
                              <MessageCircle className="w-4 h-4" /> 24
                            </span>
                            <span className="flex items-center gap-1.5">
                              <Star className="w-4 h-4" /> 67
                            </span>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Right 1/3 Column - News & Updates */}
            <div>
              <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden sticky top-4">
                <div className="flex items-center justify-between px-6 py-5 border-b border-gray-200 bg-orange-50">
                  <div className="flex items-center gap-3">
                    <div className="w-8 h-8 bg-orange-500 rounded-lg flex items-center justify-center text-white text-sm font-bold">
                      📰
                    </div>
                    <h2 className="text-xl font-bold text-gray-900">News & Updates</h2>
                  </div>
                  <button className="text-orange-600 hover:text-orange-700">
                    <ArrowRight className="w-5 h-5" />
                  </button>
                </div>

                <div className="divide-y divide-gray-100">
                  {/* News Item 1 */}
                  <div className="px-6 py-5 hover:bg-gray-50 transition-colors cursor-pointer">
                    <Badge variant="business" className="mb-3 text-xs">Business</Badge>
                    <h3 className="font-bold text-gray-900 mb-2 leading-snug">
                      New Sri Lankan restaurant opens in downtown
                    </h3>
                    <p className="text-sm text-gray-600 mb-2">Authentic cuisine from Colombo arrives...</p>
                    <span className="text-xs text-gray-500">3h ago</span>
                  </div>

                  <div className="px-6 py-5 hover:bg-gray-50 transition-colors cursor-pointer">
                    <Badge variant="community" className="mb-3 text-xs">Community</Badge>
                    <h3 className="font-bold text-gray-900 mb-2 leading-snug">
                      Community raises $50K for Sri Lankan schools
                    </h3>
                    <p className="text-sm text-gray-600 mb-2">Successful fundraiser helps education...</p>
                    <span className="text-xs text-gray-500">1d ago</span>
                  </div>

                  <div className="px-6 py-5 hover:bg-gray-50 transition-colors cursor-pointer">
                    <Badge variant="cultural" className="mb-3 text-xs">Culture</Badge>
                    <h3 className="font-bold text-gray-900 mb-2 leading-snug">
                      Cultural dance competition winners announced
                    </h3>
                    <p className="text-sm text-gray-600 mb-2">Young performers showcase talent...</p>
                    <span className="text-xs text-gray-500">2d ago</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Cultural Spotlight */}
      <section className="py-16" style={{
        background: 'linear-gradient(90deg, #FF5722 0%, #E91E63 50%, #C2185B 100%)'
      }}>
        <div className="container mx-auto px-4 sm:px-6 lg:px-8 text-white">
          <div className="flex items-center gap-4 mb-4">
            <Sparkles className="w-8 h-8" />
            <h2 className="text-4xl font-bold">Cultural Spotlight</h2>
          </div>
          <p className="text-xl max-w-3xl leading-relaxed">
            Discover and preserve our rich heritage through interactive content and community
            contributions
          </p>
        </div>
      </section>

      {/* Newsletter - Stay Connected */}
      <section
        className="py-20 text-white relative overflow-hidden"
        style={{
          background: 'linear-gradient(90deg, #FF5722 0%, #E91E63 33%, #FF1744 66%, #00BCD4 100%)'
        }}
      >
        <div className="absolute inset-0 bg-gradient-to-br from-black/10 to-transparent" />

        <div className="container mx-auto px-4 sm:px-6 lg:px-8 relative z-10">
          <div className="max-w-2xl mx-auto text-center">
            <h2 className="text-4xl font-bold mb-5">Stay Connected</h2>
            <p className="text-xl mb-10">
              Get weekly updates about events, news, and community highlights
            </p>

            <form className="flex gap-4 max-w-lg mx-auto">
              <input
                type="email"
                placeholder="Enter your email"
                className="flex-1 px-6 py-4 rounded-xl text-gray-900 placeholder:text-gray-500 focus:outline-none focus:ring-4 focus:ring-white/30 shadow-lg text-lg"
              />
              <button
                type="submit"
                className="px-8 py-4 bg-gray-900 text-white rounded-xl font-bold hover:bg-gray-800 transition-all shadow-lg text-lg"
              >
                Subscribe
              </button>
            </form>
          </div>
        </div>
      </section>

      <Footer />
    </div>
  );
}
