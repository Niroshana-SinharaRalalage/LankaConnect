import { Sparkles, ArrowRight } from 'lucide-react';
import { Button } from './ui/button';

export function CulturalSpotlight() {
  const highlights = [
    {
      title: 'Traditional Arts',
      description: 'Explore Kandyan dance, drumming, and ancient crafts',
      icon: '🎭',
      color: 'from-orange-500 to-amber-500',
    },
    {
      title: 'Sri Lankan Cuisine',
      description: 'Authentic recipes and cooking techniques',
      icon: '🍛',
      color: 'from-rose-500 to-pink-500',
    },
    {
      title: 'Language Lessons',
      description: 'Learn Sinhala and Tamil with native speakers',
      icon: '📚',
      color: 'from-emerald-500 to-green-500',
    },
    {
      title: 'Heritage Sites',
      description: 'Virtual tours of historical landmarks',
      icon: '🏛️',
      color: 'from-amber-500 to-yellow-500',
    },
  ];

  return (
    <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-orange-600 via-rose-600 to-amber-600 p-8 md:p-12">
      {/* Decorative Elements */}
      <div className="absolute inset-0 overflow-hidden opacity-10">
        <div className="absolute top-0 right-0 w-96 h-96 bg-white rounded-full blur-3xl"></div>
        <div className="absolute bottom-0 left-0 w-96 h-96 bg-white rounded-full blur-3xl"></div>
      </div>

      <div className="relative">
        <div className="flex items-center gap-2 mb-4">
          <Sparkles className="h-6 w-6 text-white" />
          <h2 className="text-white text-3xl">Cultural Spotlight</h2>
        </div>
        <p className="text-white/90 text-lg mb-8 max-w-2xl">
          Discover and preserve our rich heritage through interactive content and community contributions
        </p>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
          {highlights.map((highlight, index) => (
            <div
              key={index}
              className="bg-white/95 backdrop-blur-sm rounded-2xl p-6 hover:bg-white transition-all hover:scale-105 cursor-pointer"
            >
              <div className={`w-16 h-16 rounded-2xl bg-gradient-to-br ${highlight.color} flex items-center justify-center text-3xl mb-4`}>
                {highlight.icon}
              </div>
              <h3 className="text-neutral-900 mb-2">{highlight.title}</h3>
              <p className="text-neutral-600 text-sm">{highlight.description}</p>
            </div>
          ))}
        </div>

        <div className="flex flex-col sm:flex-row gap-4">
          <Button size="lg" className="bg-white text-orange-600 hover:bg-neutral-100">
            Explore Culture Hub
            <ArrowRight className="ml-2 h-5 w-5" />
          </Button>
          <Button size="lg" variant="outline" className="border-white text-white hover:bg-white/10">
            Contribute Content
          </Button>
        </div>
      </div>
    </div>
  );
}
