import { Store, ArrowRight, Star, MapPin } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Button } from './ui/button';
import { Badge } from './ui/badge';

export function MarketplaceSection() {
  const listings = [
    {
      title: 'Ceylon Cinnamon Sticks - Premium Quality',
      price: '$24.99',
      seller: 'Spice Heaven',
      rating: 4.9,
      reviews: 127,
      location: 'Toronto, ON',
      image: '🌿',
      category: 'Groceries',
      badge: 'Featured',
    },
    {
      title: 'Traditional Sri Lankan Batik Saree',
      price: '$89.99',
      seller: 'Lanka Textiles',
      rating: 4.8,
      reviews: 89,
      location: 'Vancouver, BC',
      image: '👗',
      category: 'Fashion',
      badge: 'New',
    },
    {
      title: 'Authentic Sri Lankan Curry Powder Set',
      price: '$19.99',
      seller: 'Ceylon Spices Co.',
      rating: 5.0,
      reviews: 203,
      location: 'Melbourne, VIC',
      image: '🌶️',
      category: 'Groceries',
      badge: 'Hot Deal',
    },
  ];

  return (
    <Card className="border-neutral-200 shadow-sm">
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle className="flex items-center gap-2 text-neutral-900">
          <Store className="h-6 w-6 text-emerald-600" />
          Marketplace
        </CardTitle>
        <Button variant="ghost" size="sm" className="text-emerald-600">
          Browse All
          <ArrowRight className="ml-2 h-4 w-4" />
        </Button>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {listings.map((listing, index) => (
            <div
              key={index}
              className="group bg-white rounded-xl border border-neutral-200 hover:border-emerald-200 overflow-hidden hover:shadow-lg transition-all cursor-pointer"
            >
              {/* Product Image */}
              <div className="relative h-40 bg-gradient-to-br from-emerald-50 to-green-50 flex items-center justify-center text-6xl">
                {listing.image}
                <Badge className="absolute top-3 right-3 bg-emerald-600 text-white">
                  {listing.badge}
                </Badge>
              </div>

              {/* Product Details */}
              <div className="p-4">
                <div className="text-emerald-600 text-sm mb-1">{listing.category}</div>
                <h3 className="text-neutral-900 mb-2 line-clamp-2 min-h-[3rem] group-hover:text-emerald-600 transition-colors">
                  {listing.title}
                </h3>

                <div className="flex items-center gap-2 mb-3">
                  <div className="flex items-center gap-1">
                    <Star className="h-4 w-4 fill-amber-400 text-amber-400" />
                    <span className="text-sm text-neutral-700">{listing.rating}</span>
                  </div>
                  <span className="text-neutral-400">•</span>
                  <span className="text-sm text-neutral-500">{listing.reviews} reviews</span>
                </div>

                <div className="flex items-center gap-2 mb-3 text-sm text-neutral-600">
                  <MapPin className="h-3.5 w-3.5" />
                  <span>{listing.location}</span>
                </div>

                <div className="flex items-center justify-between pt-3 border-t border-neutral-100">
                  <div className="text-2xl text-neutral-900">{listing.price}</div>
                  <Button size="sm" className="bg-emerald-600 hover:bg-emerald-700">
                    View
                  </Button>
                </div>
              </div>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
