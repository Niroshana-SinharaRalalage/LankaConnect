'use client';

import React from 'react';
import Link from 'next/link';
import { Logo } from '../atoms/Logo';
import { Facebook, Twitter, Instagram, Youtube, Mail } from 'lucide-react';

interface FooterLinkCategory {
  title: string;
  links: Array<{
    label: string;
    href: string;
  }>;
}

const NewFooter: React.FC = () => {
  const [email, setEmail] = React.useState('');

  const footerLinks: FooterLinkCategory[] = [
    {
      title: 'Community',
      links: [
        { label: 'Events', href: '/events' },
        { label: 'Forums', href: '/forums' },
        { label: 'Directory', href: '/directory' },
        { label: 'Cultural Hub', href: '/culture' },
      ],
    },
    {
      title: 'Marketplace',
      links: [
        { label: 'Browse Listings', href: '/marketplace' },
        { label: 'Sell Items', href: '/marketplace/sell' },
        { label: 'Businesses', href: '/businesses' },
        { label: 'Services', href: '/services' },
      ],
    },
    {
      title: 'Resources',
      links: [
        { label: 'Help Center', href: '/help' },
        { label: 'Guidelines', href: '/guidelines' },
        { label: 'Safety', href: '/safety' },
        { label: 'Blog', href: '/blog' },
      ],
    },
    {
      title: 'About',
      links: [
        { label: 'Our Story', href: '/about' },
        { label: 'Contact Us', href: '/contact' },
        { label: 'Careers', href: '/careers' },
        { label: 'Press', href: '/press' },
      ],
    },
  ];

  const handleNewsletterSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    // TODO: Handle newsletter subscription
    console.log('Newsletter subscription:', email);
    setEmail('');
  };

  return (
    <footer className="bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 text-white mt-24 relative overflow-hidden">
      {/* Decorative Background Pattern */}
      <div className="absolute inset-0 opacity-10">
        <div
          className="absolute inset-0"
          style={{
            backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
          }}
        ></div>
      </div>

      <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        {/* Newsletter Section */}
        <div className="bg-white/10 backdrop-blur-sm rounded-2xl p-8 mb-12 border border-white/20">
          <div className="max-w-3xl mx-auto text-center">
            <h3 className="text-2xl font-bold mb-2">Stay Connected</h3>
            <p className="text-white/90 mb-6">
              Get weekly updates about events, news, and community highlights
            </p>
            <form onSubmit={handleNewsletterSubmit} className="flex flex-col sm:flex-row gap-3 max-w-md mx-auto">
              <input
                type="email"
                placeholder="Enter your email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="flex-1 px-4 py-3 rounded-lg bg-white text-neutral-900 border-0 placeholder:text-neutral-500 focus:outline-none focus:ring-2 focus:ring-white/30"
                required
              />
              <button
                type="submit"
                className="px-6 py-3 bg-neutral-900 hover:bg-neutral-800 text-white rounded-lg font-semibold transition-all"
              >
                Subscribe
              </button>
            </form>
          </div>
        </div>

        {/* Links Grid */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-8 mb-12">
          {footerLinks.map((category) => (
            <div key={category.title}>
              <h4 className="text-white font-bold mb-4">{category.title}</h4>
              <ul className="space-y-2">
                {category.links.map((link) => (
                  <li key={link.label}>
                    <Link
                      href={link.href}
                      className="text-white/80 hover:text-white transition-colors text-sm"
                    >
                      {link.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        {/* Bottom Section */}
        <div className="pt-8 border-t border-white/20 flex flex-col md:flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <Logo size="md" />
            <div>
              <div className="text-white font-bold">LankaConnect</div>
              <div className="text-white/80 text-sm">© {new Date().getFullYear()} All rights reserved</div>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <button className="w-10 h-10 rounded-full hover:bg-white/10 transition-colors flex items-center justify-center text-white/80 hover:text-white">
              <Facebook className="h-5 w-5" />
            </button>
            <button className="w-10 h-10 rounded-full hover:bg-white/10 transition-colors flex items-center justify-center text-white/80 hover:text-white">
              <Twitter className="h-5 w-5" />
            </button>
            <button className="w-10 h-10 rounded-full hover:bg-white/10 transition-colors flex items-center justify-center text-white/80 hover:text-white">
              <Instagram className="h-5 w-5" />
            </button>
            <button className="w-10 h-10 rounded-full hover:bg-white/10 transition-colors flex items-center justify-center text-white/80 hover:text-white">
              <Youtube className="h-5 w-5" />
            </button>
            <button className="w-10 h-10 rounded-full hover:bg-white/10 transition-colors flex items-center justify-center text-white/80 hover:text-white">
              <Mail className="h-5 w-5" />
            </button>
          </div>
        </div>
      </div>
    </footer>
  );
};

export default NewFooter;
