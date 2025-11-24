'use client';

import * as React from 'react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { Badge } from '@/presentation/components/ui/Badge';
import { Sparkles, ArrowRight, Calendar, MapPin, Users, Clock, Store, MessageSquare, Newspaper, Star, ThumbsUp, Flame, ShoppingBag } from 'lucide-react';
import { useFeaturedEvents } from '@/presentation/hooks/useEvents';
import { useAuthStore } from '@/presentation/store/useAuthStore';

export default function Home() {
  const { user } = useAuthStore();
  const { data: featuredEvents, isLoading: eventsLoading, error: eventsError } = useFeaturedEvents(user?.userId);

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      {/* Hero Section - Exact Figma Design */}
      <div className="relative overflow-hidden bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800">
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

        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 md:py-24">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">
            {/* Left Content */}
            <div className="text-center lg:text-left">
              {/* Badge with Icon */}
              <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-white/20 backdrop-blur-sm border border-white/30 mb-6">
                <Sparkles className="h-4 w-4 text-white" />
                <span className="text-sm text-white">Connecting Sri Lankans Worldwide</span>
              </div>

              {/* Heading */}
              <h1 className="text-4xl md:text-5xl lg:text-6xl text-white mb-6">
                Your Community,
                <br />
                <span className="text-white drop-shadow-lg">Your Heritage</span>
              </h1>

              {/* Description */}
              <p className="text-lg text-white/95 mb-8 max-w-xl mx-auto lg:mx-0">
                Join the largest Sri Lankan diaspora platform. Discover events, connect
                with businesses, engage in discussions, and celebrate our rich culture
                together.
              </p>

              {/* Removed News & Updates button per user request */}

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

            {/* Right - Featured Events Cards (from Database) */}
            <div className="relative hidden lg:block">
              {eventsLoading ? (
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-4">
                    {[...Array(2)].map((_, i) => (
                      <div key={i} className="bg-white rounded-2xl p-6 shadow-lg border border-neutral-100 animate-pulse">
                        <div className="w-12 h-12 rounded-xl bg-neutral-200 mb-4"></div>
                        <div className="h-4 bg-neutral-200 rounded w-3/4 mb-2"></div>
                        <div className="h-3 bg-neutral-200 rounded w-1/2"></div>
                      </div>
                    ))}
                  </div>
                  <div className="space-y-4 mt-8">
                    {[...Array(2)].map((_, i) => (
                      <div key={i} className="bg-white rounded-2xl p-6 shadow-lg border border-neutral-100 animate-pulse">
                        <div className="w-12 h-12 rounded-xl bg-neutral-200 mb-4"></div>
                        <div className="h-4 bg-neutral-200 rounded w-3/4 mb-2"></div>
                        <div className="h-3 bg-neutral-200 rounded w-1/2"></div>
                      </div>
                    ))}
                  </div>
                </div>
              ) : eventsError || !featuredEvents || featuredEvents.length === 0 ? (
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-4">
                    <div className="bg-white rounded-2xl p-6 shadow-lg border border-neutral-100">
                      <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-orange-500 to-amber-500 flex items-center justify-center text-2xl mb-4">
                        🎉
                      </div>
                      <div className="text-neutral-900 font-semibold mb-1">No Events Yet</div>
                      <div className="text-sm text-neutral-500">Check back soon</div>
                    </div>
                    <div className="bg-white rounded-2xl p-6 shadow-lg border border-neutral-100">
                      <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-emerald-500 to-green-500 flex items-center justify-center text-2xl mb-4">
                        📅
                      </div>
                      <div className="text-neutral-900 font-semibold mb-1">Coming Soon</div>
                      <div className="text-sm text-neutral-500">New events weekly</div>
                    </div>
                  </div>
                  <div className="space-y-4 mt-8">
                    <div className="bg-white rounded-2xl p-6 shadow-lg border border-neutral-100">
                      <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-rose-500 to-pink-500 flex items-center justify-center text-2xl mb-4">
                        🎭
                      </div>
                      <div className="text-neutral-900 font-semibold mb-1">Cultural Events</div>
                      <div className="text-sm text-neutral-500">Stay tuned</div>
                    </div>
                    <div className="bg-white rounded-2xl p-6 shadow-lg border border-neutral-100">
                      <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-amber-500 to-yellow-500 flex items-center justify-center text-2xl mb-4">
                        🌟
                      </div>
                      <div className="text-neutral-900 font-semibold mb-1">Join Community</div>
                      <div className="text-sm text-neutral-500">Connect with us</div>
                    </div>
                  </div>
                </div>
              ) : (
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-4">
                    {featuredEvents.slice(0, 2).map((event) => (
                      <div key={event.id} className="bg-white rounded-2xl p-6 shadow-lg hover:shadow-xl transition-all hover:-translate-y-1 cursor-pointer border border-neutral-100">
                        <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-orange-500 to-amber-500 flex items-center justify-center text-2xl mb-4">
                          {event.images && event.images.length > 0 ? (
                            <img src={event.images[0].imageUrl} alt={event.title} className="w-full h-full object-cover rounded-xl" />
                          ) : (
                            '🎉'
                          )}
                        </div>
                        <div className="text-neutral-900 font-semibold mb-1 line-clamp-1">{event.title}</div>
                        <div className="text-sm text-neutral-500">
                          {new Date(event.startDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} at {new Date(event.startDate).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}
                        </div>
                      </div>
                    ))}
                  </div>
                  <div className="space-y-4 mt-8">
                    {featuredEvents.slice(2, 4).map((event) => (
                      <div key={event.id} className="bg-white rounded-2xl p-6 shadow-lg hover:shadow-xl transition-all hover:-translate-y-1 cursor-pointer border border-neutral-100">
                        <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-rose-500 to-pink-500 flex items-center justify-center text-2xl mb-4">
                          {event.images && event.images.length > 0 ? (
                            <img src={event.images[0].imageUrl} alt={event.title} className="w-full h-full object-cover rounded-xl" />
                          ) : (
                            '🎭'
                          )}
                        </div>
                        <div className="text-neutral-900 font-semibold mb-1 line-clamp-1">{event.title}</div>
                        <div className="text-sm text-neutral-500">
                          {new Date(event.startDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} at {new Date(event.startDate).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* View All Events Button - Below feature cards */}
              <div className="mt-6 flex justify-center">
                <a href="#events" className="inline-flex items-center justify-center px-8 py-3 bg-white text-orange-600 hover:bg-neutral-100 shadow-lg rounded-lg font-semibold transition-all">
                  <Calendar className="mr-2 h-5 w-5" />
                  View All Events
                </a>
              </div>
            </div>
          </div>
        </div>
      </div>


      {/* Main Content */}
      <section className="py-16 bg-neutral-50">
        <div className="container mx-auto px-6 lg:px-8">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
            {/* Left Column - Forum Highlights + News (stacked) then Business */}
            <div className="lg:col-span-2 space-y-8">
              {/* Forum Highlights and News & Updates - Side by side */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                {/* Forum Highlights */}
                <Card className="border-neutral-200 shadow-sm">
                  <CardHeader className="flex flex-row items-center justify-between px-6 py-4 border-b border-neutral-100">
                    <CardTitle className="flex items-center gap-2 text-neutral-900 text-lg font-semibold">
                      <MessageSquare className="h-5 w-5 text-rose-600" />
                      Forum Highlights
                    </CardTitle>
                    <button className="text-rose-600 hover:text-rose-700">
                      <ArrowRight className="h-5 w-5" />
                    </button>
                  </CardHeader>

                  <CardContent className="p-6 space-y-4">
                    {/* Post 1 */}
                    <div className="group relative overflow-hidden rounded-xl border border-neutral-200 hover:border-rose-200 transition-all hover:shadow-md bg-white p-4 cursor-pointer">
                      <div className="flex gap-3">
                        <div className="w-10 h-10 bg-gradient-to-br from-pink-500 to-purple-600 rounded-full flex items-center justify-center text-white font-bold flex-shrink-0 text-sm">
                          SP
                        </div>
                        <div className="flex-1 min-w-0">
                          <div className="flex items-start gap-2 mb-1">
                            <h3 className="font-semibold text-neutral-900 text-sm group-hover:text-rose-600 transition-colors flex-1">
                              Best places to buy Sri Lankan groceries?
                            </h3>
                            <Badge variant="hot" className="flex items-center gap-1 flex-shrink-0">
                              <Flame className="h-3 w-3" />
                              Hot
                            </Badge>
                          </div>
                          <div className="flex items-center gap-2 text-xs text-neutral-500 mb-2">
                            <span>Saman P.</span>
                            <span>•</span>
                            <span>2h ago</span>
                          </div>
                          <div className="flex items-center gap-3 mt-2">
                            <Badge variant="food">Food</Badge>
                            <div className="flex items-center gap-3 text-xs text-neutral-600">
                              <span className="flex items-center gap-1">
                                <MessageSquare className="h-3 w-3" />
                                24
                              </span>
                              <span className="flex items-center gap-1">
                                <ThumbsUp className="h-3 w-3" />
                                67
                              </span>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>

                    {/* Post 2 */}
                    <div className="group relative overflow-hidden rounded-xl border border-neutral-200 hover:border-rose-200 transition-all hover:shadow-md bg-white p-4 cursor-pointer">
                      <div className="flex gap-3">
                        <div className="w-10 h-10 bg-gradient-to-br from-blue-500 to-cyan-600 rounded-full flex items-center justify-center text-white font-bold flex-shrink-0 text-sm">
                          NR
                        </div>
                        <div className="flex-1 min-w-0">
                          <h3 className="font-semibold text-neutral-900 text-sm mb-1 group-hover:text-rose-600 transition-colors">
                            Teaching Sinhala to kids abroad
                          </h3>
                          <div className="flex items-center gap-2 text-xs text-neutral-500 mb-2">
                            <span>Nisha R.</span>
                            <span>•</span>
                            <span>5h ago</span>
                          </div>
                          <div className="flex items-center gap-3 mt-2">
                            <Badge variant="community">Parenting</Badge>
                            <div className="flex items-center gap-3 text-xs text-neutral-600">
                              <span className="flex items-center gap-1">
                                <MessageSquare className="h-3 w-3" />
                                18
                              </span>
                              <span className="flex items-center gap-1">
                                <ThumbsUp className="h-3 w-3" />
                                45
                              </span>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>

                    {/* Post 3 */}
                    <div className="group relative overflow-hidden rounded-xl border border-neutral-200 hover:border-rose-200 transition-all hover:shadow-md bg-white p-4 cursor-pointer">
                      <div className="flex gap-3">
                        <div className="w-10 h-10 bg-gradient-to-br from-green-500 to-emerald-600 rounded-full flex items-center justify-center text-white font-bold flex-shrink-0 text-sm">
                          AP
                        </div>
                        <div className="flex-1 min-w-0">
                          <h3 className="font-semibold text-neutral-900 text-sm mb-1 group-hover:text-rose-600 transition-colors">
                            Sri Lankan recipes to try this weekend
                          </h3>
                          <div className="flex items-center gap-2 text-xs text-neutral-500 mb-2">
                            <span>Amara P.</span>
                            <span>•</span>
                            <span>8h ago</span>
                          </div>
                          <div className="flex items-center gap-3 mt-2">
                            <Badge variant="food">Food</Badge>
                            <div className="flex items-center gap-3 text-xs text-neutral-600">
                              <span className="flex items-center gap-1">
                                <MessageSquare className="h-3 w-3" />
                                32
                              </span>
                              <span className="flex items-center gap-1">
                                <ThumbsUp className="h-3 w-3" />
                                89
                              </span>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </CardContent>
                </Card>

                {/* News & Updates */}
                <Card className="border-neutral-200 shadow-sm">
                  <CardHeader className="flex flex-row items-center justify-between px-6 py-4 border-b border-neutral-100">
                    <CardTitle className="flex items-center gap-2 text-neutral-900 text-lg font-semibold">
                      <Newspaper className="h-5 w-5 text-amber-600" />
                      News & Updates
                    </CardTitle>
                    <button className="text-amber-600 hover:text-amber-700">
                      <ArrowRight className="h-5 w-5" />
                    </button>
                  </CardHeader>

                  <CardContent className="p-6 space-y-4">
                    {/* News 1 */}
                    <div className="group relative overflow-hidden rounded-xl border border-neutral-200 hover:border-amber-200 transition-all hover:shadow-md bg-white p-4 cursor-pointer">
                      <Badge variant="business">Business</Badge>
                      <h3 className="font-semibold text-neutral-900 mt-3 mb-2 leading-snug group-hover:text-amber-600 transition-colors">
                        New Sri Lankan restaurant opens in downtown
                      </h3>
                      <p className="text-sm text-neutral-600 mb-2">Authentic cuisine from Colombo arrives...</p>
                      <div className="flex items-center gap-1 text-xs text-neutral-500">
                        <Clock className="h-3 w-3" />
                        <span>3h ago</span>
                      </div>
                    </div>

                    {/* News 2 */}
                    <div className="group relative overflow-hidden rounded-xl border border-neutral-200 hover:border-amber-200 transition-all hover:shadow-md bg-white p-4 cursor-pointer">
                      <Badge variant="community">Community</Badge>
                      <h3 className="font-semibold text-neutral-900 mt-3 mb-2 leading-snug group-hover:text-amber-600 transition-colors">
                        Community raises $50K for Sri Lankan schools
                      </h3>
                      <p className="text-sm text-neutral-600 mb-2">Successful fundraiser helps education...</p>
                      <div className="flex items-center gap-1 text-xs text-neutral-500">
                        <Clock className="h-3 w-3" />
                        <span>1d ago</span>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              </div>

              {/* Business Section */}
              <Card className="border-neutral-200 shadow-sm">
                <CardHeader className="flex flex-row items-center justify-between px-6 py-4 border-b border-neutral-100">
                  <CardTitle className="flex items-center gap-2 text-neutral-900 text-lg font-semibold">
                    <Store className="h-5 w-5 text-emerald-600" />
                    Business
                  </CardTitle>
                  <button className="text-emerald-600 hover:text-emerald-700 font-semibold flex items-center gap-1 text-sm">
                    Browse All
                    <ArrowRight className="h-4 w-4" />
                  </button>
                </CardHeader>

                <CardContent className="p-6">
                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                  {/* Business 1 */}
                  <div className="group relative overflow-hidden rounded-xl border border-neutral-200 hover:border-emerald-200 transition-all hover:shadow-md bg-white p-6 cursor-pointer">
                    <div className="flex items-center justify-between mb-4">
                      <div className="w-16 h-16 rounded-xl bg-gradient-to-br from-orange-100 to-amber-100 flex items-center justify-center text-3xl flex-shrink-0 group-hover:scale-105 transition-transform">
                        🍜
                      </div>
                      <Badge variant="food">Restaurant</Badge>
                    </div>
                    <h3 className="text-base font-semibold text-neutral-900 group-hover:text-emerald-600 transition-colors mb-2">
                      Lanka Kitchen Restaurant
                    </h3>
                    <p className="text-xs text-neutral-600 mb-4 line-clamp-2">
                      Authentic Sri Lankan cuisine with traditional recipes passed down through generations
                    </p>
                    <div className="flex items-center gap-2 text-xs text-neutral-600 mb-2">
                      <MapPin className="h-3.5 w-3.5 flex-shrink-0 text-emerald-600" />
                      <span>Downtown Toronto, ON</span>
                    </div>
                    <div className="flex items-center gap-1 text-xs text-neutral-600 mb-4">
                      <Star className="h-3.5 w-3.5 fill-amber-400 text-amber-400 flex-shrink-0" />
                      <span className="font-semibold">4.8</span>
                      <span>• 156 reviews</span>
                    </div>
                    <button className="w-full px-5 py-2 border border-neutral-200 hover:border-emerald-200 hover:bg-emerald-50 rounded-lg font-semibold text-sm transition-all">
                      View Details
                    </button>
                  </div>

                  {/* Business 2 */}
                  <div className="group relative overflow-hidden rounded-xl border border-neutral-200 hover:border-emerald-200 transition-all hover:shadow-md bg-white p-6 cursor-pointer">
                    <div className="flex items-center justify-between mb-4">
                      <div className="w-16 h-16 rounded-xl bg-gradient-to-br from-pink-100 to-rose-100 flex items-center justify-center text-3xl flex-shrink-0 group-hover:scale-105 transition-transform">
                        💇
                      </div>
                      <Badge variant="arts">Beauty</Badge>
                    </div>
                    <h3 className="text-base font-semibold text-neutral-900 group-hover:text-emerald-600 transition-colors mb-2">
                      Ceylon Salon & Spa
                    </h3>
                    <p className="text-xs text-neutral-600 mb-4 line-clamp-2">
                      Professional hair care and beauty services with Ayurvedic treatments
                    </p>
                    <div className="flex items-center gap-2 text-xs text-neutral-600 mb-2">
                      <MapPin className="h-3.5 w-3.5 flex-shrink-0 text-emerald-600" />
                      <span>Scarborough, ON</span>
                    </div>
                    <div className="flex items-center gap-1 text-xs text-neutral-600 mb-4">
                      <Star className="h-3.5 w-3.5 fill-amber-400 text-amber-400 flex-shrink-0" />
                      <span className="font-semibold">4.9</span>
                      <span>• 203 reviews</span>
                    </div>
                    <button className="w-full px-5 py-2 border border-neutral-200 hover:border-emerald-200 hover:bg-emerald-50 rounded-lg font-semibold text-sm transition-all">
                      View Details
                    </button>
                  </div>

                  {/* Business 3 */}
                  <div className="group relative overflow-hidden rounded-xl border border-neutral-200 hover:border-emerald-200 transition-all hover:shadow-md bg-white p-6 cursor-pointer">
                    <div className="flex items-center justify-between mb-4">
                      <div className="w-16 h-16 rounded-xl bg-gradient-to-br from-blue-100 to-cyan-100 flex items-center justify-center text-3xl flex-shrink-0 group-hover:scale-105 transition-transform">
                        📚
                      </div>
                      <Badge variant="business">Education</Badge>
                    </div>
                    <h3 className="text-base font-semibold text-neutral-900 group-hover:text-emerald-600 transition-colors mb-2">
                      Sinhala Learning Center
                    </h3>
                    <p className="text-xs text-neutral-600 mb-4 line-clamp-2">
                      Language classes for children and adults, preserving our cultural heritage
                    </p>
                    <div className="flex items-center gap-2 text-xs text-neutral-600 mb-2">
                      <MapPin className="h-3.5 w-3.5 flex-shrink-0 text-emerald-600" />
                      <span>Mississauga, ON</span>
                    </div>
                    <div className="flex items-center gap-1 text-xs text-neutral-600 mb-4">
                      <Star className="h-3.5 w-3.5 fill-amber-400 text-amber-400 flex-shrink-0" />
                      <span className="font-semibold">4.7</span>
                      <span>• 89 reviews</span>
                    </div>
                    <button className="w-full px-5 py-2 border border-neutral-200 hover:border-emerald-200 hover:bg-emerald-50 rounded-lg font-semibold text-sm transition-all">
                      View Details
                    </button>
                  </div>
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* Right Sidebar - Marketplace */}
            <div>
              <Card className="border-neutral-200 shadow-sm">
                <CardHeader className="flex flex-row items-center justify-between px-6 py-4 border-b border-neutral-100">
                  <CardTitle className="flex items-center gap-2 text-neutral-900 text-lg font-semibold">
                    <ShoppingBag className="h-5 w-5 text-emerald-600" />
                    Marketplace
                  </CardTitle>
                  <button className="text-emerald-600 hover:text-emerald-700">
                    <ArrowRight className="h-5 w-5" />
                  </button>
                </CardHeader>

                <CardContent className="p-6 space-y-4">
                  {/* Product 1 */}
                  <div className="group relative overflow-hidden rounded-xl border border-neutral-200 hover:border-emerald-200 transition-all hover:shadow-md bg-white p-4 cursor-pointer">
                    <div className="flex items-start gap-3">
                      <div className="w-16 h-16 rounded-lg bg-gradient-to-br from-emerald-100 to-green-100 flex items-center justify-center text-3xl flex-shrink-0 group-hover:scale-105 transition-transform">
                        🌿
                      </div>
                      <div className="flex-1 min-w-0">
                        <h3 className="text-sm font-semibold text-neutral-900 group-hover:text-emerald-600 transition-colors mb-1">
                          Ceylon Cinnamon Sticks
                        </h3>
                        <div className="flex items-center gap-1 text-xs text-neutral-600 mb-2">
                          <Star className="h-3 w-3 fill-amber-400 text-amber-400 flex-shrink-0" />
                          <span className="font-semibold">4.9</span>
                          <span className="text-neutral-400">•</span>
                          <MapPin className="h-3 w-3 flex-shrink-0 text-emerald-600" />
                          <span>Toronto</span>
                        </div>
                        <div className="text-lg font-bold text-emerald-600">$24.99</div>
                      </div>
                    </div>
                  </div>

                  {/* Product 2 */}
                  <div className="group relative overflow-hidden rounded-xl border border-neutral-200 hover:border-emerald-200 transition-all hover:shadow-md bg-white p-4 cursor-pointer">
                    <div className="flex items-start gap-3">
                      <div className="w-16 h-16 rounded-lg bg-gradient-to-br from-purple-100 to-pink-100 flex items-center justify-center text-3xl flex-shrink-0 group-hover:scale-105 transition-transform">
                        👗
                      </div>
                      <div className="flex-1 min-w-0">
                        <h3 className="text-sm font-semibold text-neutral-900 group-hover:text-emerald-600 transition-colors mb-1">
                          Batik Saree
                        </h3>
                        <div className="flex items-center gap-1 text-xs text-neutral-600 mb-2">
                          <Star className="h-3 w-3 fill-amber-400 text-amber-400 flex-shrink-0" />
                          <span className="font-semibold">4.8</span>
                          <span className="text-neutral-400">•</span>
                          <MapPin className="h-3 w-3 flex-shrink-0 text-emerald-600" />
                          <span>Scarborough</span>
                        </div>
                        <div className="text-lg font-bold text-emerald-600">$89.99</div>
                      </div>
                    </div>
                  </div>

                  {/* Product 3 */}
                  <div className="group relative overflow-hidden rounded-xl border border-neutral-200 hover:border-emerald-200 transition-all hover:shadow-md bg-white p-4 cursor-pointer">
                    <div className="flex items-start gap-3">
                      <div className="w-16 h-16 rounded-lg bg-gradient-to-br from-orange-100 to-red-100 flex items-center justify-center text-3xl flex-shrink-0 group-hover:scale-105 transition-transform">
                        🌶️
                      </div>
                      <div className="flex-1 min-w-0">
                        <h3 className="text-sm font-semibold text-neutral-900 group-hover:text-emerald-600 transition-colors mb-1">
                          Curry Powder Set
                        </h3>
                        <div className="flex items-center gap-1 text-xs text-neutral-600 mb-2">
                          <Star className="h-3 w-3 fill-amber-400 text-amber-400 flex-shrink-0" />
                          <span className="font-semibold">4.7</span>
                          <span className="text-neutral-400">•</span>
                          <MapPin className="h-3 w-3 flex-shrink-0 text-emerald-600" />
                          <span>Mississauga</span>
                        </div>
                        <div className="text-lg font-bold text-emerald-600">$19.99</div>
                      </div>
                    </div>
                  </div>

                  {/* Product 4 */}
                  <div className="group relative overflow-hidden rounded-xl border border-neutral-200 hover:border-emerald-200 transition-all hover:shadow-md bg-white p-4 cursor-pointer">
                    <div className="flex items-start gap-3">
                      <div className="w-16 h-16 rounded-lg bg-gradient-to-br from-amber-100 to-yellow-100 flex items-center justify-center text-3xl flex-shrink-0 group-hover:scale-105 transition-transform">
                        🎭
                      </div>
                      <div className="flex-1 min-w-0">
                        <h3 className="text-sm font-semibold text-neutral-900 group-hover:text-emerald-600 transition-colors mb-1">
                          Traditional Masks
                        </h3>
                        <div className="flex items-center gap-1 text-xs text-neutral-600 mb-2">
                          <Star className="h-3 w-3 fill-amber-400 text-amber-400 flex-shrink-0" />
                          <span className="font-semibold">5.0</span>
                          <span className="text-neutral-400">•</span>
                          <MapPin className="h-3 w-3 flex-shrink-0 text-emerald-600" />
                          <span>Brampton</span>
                        </div>
                        <div className="text-lg font-bold text-emerald-600">$45.00</div>
                      </div>
                    </div>
                  </div>

                  {/* Product 5 */}
                  <div className="group relative overflow-hidden rounded-xl border border-neutral-200 hover:border-emerald-200 transition-all hover:shadow-md bg-white p-4 cursor-pointer">
                    <div className="flex items-start gap-3">
                      <div className="w-16 h-16 rounded-lg bg-gradient-to-br from-rose-100 to-red-100 flex items-center justify-center text-3xl flex-shrink-0 group-hover:scale-105 transition-transform">
                        🍵
                      </div>
                      <div className="flex-1 min-w-0">
                        <h3 className="text-sm font-semibold text-neutral-900 group-hover:text-emerald-600 transition-colors mb-1">
                          Ceylon Tea Collection
                        </h3>
                        <div className="flex items-center gap-1 text-xs text-neutral-600 mb-2">
                          <Star className="h-3 w-3 fill-amber-400 text-amber-400 flex-shrink-0" />
                          <span className="font-semibold">4.9</span>
                          <span className="text-neutral-400">•</span>
                          <MapPin className="h-3 w-3 flex-shrink-0 text-emerald-600" />
                          <span>Markham</span>
                        </div>
                        <div className="text-lg font-bold text-emerald-600">$34.99</div>
                      </div>
                    </div>
                  </div>

                  {/* Product 6 */}
                  <div className="group relative overflow-hidden rounded-xl border border-neutral-200 hover:border-emerald-200 transition-all hover:shadow-md bg-white p-4 cursor-pointer">
                    <div className="flex items-start gap-3">
                      <div className="w-16 h-16 rounded-lg bg-gradient-to-br from-indigo-100 to-blue-100 flex items-center justify-center text-3xl flex-shrink-0 group-hover:scale-105 transition-transform">
                        📿
                      </div>
                      <div className="flex-1 min-w-0">
                        <h3 className="text-sm font-semibold text-neutral-900 group-hover:text-emerald-600 transition-colors mb-1">
                          Handcrafted Jewelry
                        </h3>
                        <div className="flex items-center gap-1 text-xs text-neutral-600 mb-2">
                          <Star className="h-3 w-3 fill-amber-400 text-amber-400 flex-shrink-0" />
                          <span className="font-semibold">4.8</span>
                          <span className="text-neutral-400">•</span>
                          <MapPin className="h-3 w-3 flex-shrink-0 text-emerald-600" />
                          <span>Richmond Hill</span>
                        </div>
                        <div className="text-lg font-bold text-emerald-600">$65.00</div>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      </section>


      <Footer />
    </div>
  );
}
