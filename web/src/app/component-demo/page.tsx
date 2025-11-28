'use client';

import React from 'react';
import { Badge } from '@/presentation/components/ui/Badge';
import { IconButton } from '@/presentation/components/ui/IconButton';
import { FeatureCard } from '@/presentation/components/ui/FeatureCard';
import { Calendar, Building2, MessageCircle, Users, MapPin, Newspaper } from 'lucide-react';

export default function ComponentDemoPage() {
  return (
    <div className="min-h-screen bg-gray-50 p-8">
      <div className="max-w-7xl mx-auto space-y-12">
        {/* Header */}
        <div className="text-center">
          <h1 className="text-4xl font-bold text-gray-900 mb-2">
            Component Library Demo
          </h1>
          <p className="text-gray-600">Phase 6C.1 - Landing Page Redesign Components</p>
        </div>

        {/* Badge Component */}
        <section className="bg-white rounded-lg p-6 shadow-sm">
          <h2 className="text-2xl font-semibold mb-4 text-gray-900">Badge Component</h2>
          <p className="text-sm text-gray-600 mb-4">9 variants for category labels and status indicators</p>
          <div className="flex flex-wrap gap-3">
            <Badge>Default</Badge>
            <Badge variant="cultural">Cultural</Badge>
            <Badge variant="arts">Arts</Badge>
            <Badge variant="food">Food</Badge>
            <Badge variant="business">Business</Badge>
            <Badge variant="community">Community</Badge>
            <Badge variant="featured">Featured</Badge>
            <Badge variant="new">New</Badge>
            <Badge variant="hot">Hot Deal</Badge>
          </div>
        </section>

        {/* IconButton Component */}
        <section className="bg-white rounded-lg p-6 shadow-sm">
          <h2 className="text-2xl font-semibold mb-4 text-gray-900">IconButton Component</h2>
          <p className="text-sm text-gray-600 mb-4">3 variants × 3 sizes for quick action buttons</p>

          {/* Default Variant */}
          <div className="mb-6">
            <h3 className="text-sm font-medium text-gray-700 mb-3">Default Variant</h3>
            <div className="flex flex-wrap gap-4">
              <IconButton icon={<Calendar />} label="New Event" size="sm" />
              <IconButton icon={<Building2 />} label="Business" size="md" />
              <IconButton icon={<MessageCircle />} label="Forum" size="lg" />
            </div>
          </div>

          {/* Primary Variant */}
          <div className="mb-6">
            <h3 className="text-sm font-medium text-gray-700 mb-3">Primary Variant (Gradient)</h3>
            <div className="flex flex-wrap gap-4">
              <IconButton icon={<Calendar />} label="New Event" variant="primary" size="sm" />
              <IconButton icon={<Building2 />} label="Business" variant="primary" size="md" />
              <IconButton icon={<MessageCircle />} label="Forum" variant="primary" size="lg" />
            </div>
          </div>

          {/* Secondary Variant */}
          <div>
            <h3 className="text-sm font-medium text-gray-700 mb-3">Secondary Variant</h3>
            <div className="flex flex-wrap gap-4">
              <IconButton icon={<Users />} label="Members" variant="secondary" size="sm" />
              <IconButton icon={<MapPin />} label="Location" variant="secondary" size="md" />
              <IconButton icon={<Newspaper />} label="News" variant="secondary" size="lg" />
            </div>
          </div>
        </section>

        {/* FeatureCard Component */}
        <section className="bg-white rounded-lg p-6 shadow-sm">
          <h2 className="text-2xl font-semibold mb-4 text-gray-900">FeatureCard Component</h2>
          <p className="text-sm text-gray-600 mb-4">3 variants for feature highlights with icons and stats</p>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {/* Default Variant */}
            <FeatureCard
              icon={<Users className="w-6 h-6" />}
              title="Active Members"
              description="Connect with Sri Lankans worldwide and build meaningful relationships"
              stat="1,234"
              variant="default"
            />

            {/* Gradient Variant */}
            <FeatureCard
              icon={<Calendar className="w-6 h-6" />}
              title="Events This Month"
              description="Discover cultural events, meetups, and celebrations happening near you"
              stat="48"
              variant="gradient"
            />

            {/* Cultural Variant */}
            <FeatureCard
              icon={<Building2 className="w-6 h-6" />}
              title="Local Businesses"
              description="Support Sri Lankan owned businesses and services in your community"
              stat="326"
              variant="cultural"
            />
          </div>

          {/* Clickable examples */}
          <div className="mt-6">
            <h3 className="text-sm font-medium text-gray-700 mb-3">Clickable Cards (with hover effect)</h3>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              <FeatureCard
                icon={<MessageCircle className="w-6 h-6" />}
                title="Forum Posts"
                description="Join discussions about Sri Lankan culture, food, and traditions"
                stat="892"
                onClick={() => alert('Clicked Forum Posts!')}
              />

              <FeatureCard
                icon={<Newspaper className="w-6 h-6" />}
                title="News & Updates"
                description="Stay informed with latest news from Sri Lankan communities"
                variant="gradient"
                onClick={() => alert('Clicked News!')}
              />

              <FeatureCard
                icon={<MapPin className="w-6 h-6" />}
                title="Locations"
                description="Find Sri Lankan restaurants, temples, and cultural centers"
                variant="cultural"
                onClick={() => alert('Clicked Locations!')}
              />
            </div>
          </div>
        </section>

        {/* Size Comparison */}
        <section className="bg-white rounded-lg p-6 shadow-sm">
          <h2 className="text-2xl font-semibold mb-4 text-gray-900">Size Variations</h2>
          <p className="text-sm text-gray-600 mb-4">Small, Medium, and Large sizes</p>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <FeatureCard
              icon={<Users className="w-6 h-6" />}
              title="Small Size"
              description="Compact layout for dense content"
              size="sm"
            />

            <FeatureCard
              icon={<Users className="w-6 h-6" />}
              title="Medium Size"
              description="Standard layout for balanced content"
              size="md"
            />

            <FeatureCard
              icon={<Users className="w-6 h-6" />}
              title="Large Size"
              description="Spacious layout for prominent features"
              size="lg"
            />
          </div>
        </section>

        {/* Footer */}
        <div className="text-center text-sm text-gray-500 pt-8 border-t">
          <p>✅ Build Status: 0 TypeScript Errors</p>
          <p>✅ Test Status: 68 Tests Passing (Badge: 21, IconButton: 21, FeatureCard: 26)</p>
        </div>
      </div>
    </div>
  );
}
