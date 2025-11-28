import { useState } from 'react';
import { Header } from './components/Header';
import { HeroSection } from './components/HeroSection';
import { QuickActions } from './components/QuickActions';
import { UpcomingEvents } from './components/UpcomingEvents';
import { MarketplaceSection } from './components/MarketplaceSection';
import { ForumHighlights } from './components/ForumHighlights';
import { CulturalSpotlight } from './components/CulturalSpotlight';
import { NewsUpdates } from './components/NewsUpdates';
import { Footer } from './components/Footer';

export default function App() {
  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />
      <HeroSection />
      <QuickActions />
      
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 space-y-16">
        {/* Main Content Grid */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          <div className="lg:col-span-2 space-y-8">
            <UpcomingEvents />
            <MarketplaceSection />
          </div>
          <div className="space-y-8">
            <ForumHighlights />
            <NewsUpdates />
          </div>
        </div>

        <CulturalSpotlight />
      </main>

      <Footer />
    </div>
  );
}
