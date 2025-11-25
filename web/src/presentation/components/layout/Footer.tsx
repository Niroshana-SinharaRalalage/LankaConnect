'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { Logo } from '../atoms/Logo';
import { NewsletterMetroSelector } from '../features/newsletter/NewsletterMetroSelector';
import { Facebook, Twitter, Instagram, Youtube, Mail } from 'lucide-react';
import { useAuthStore } from '@/presentation/store/useAuthStore';

interface FooterLinkProps {
  href: string;
  children: React.ReactNode;
  external?: boolean;
}

const FooterLink: React.FC<FooterLinkProps> = ({ href, children, external = false }) => {
  const linkClasses = "text-white/80 hover:text-white transition-colors duration-200 text-sm";

  if (external) {
    return (
      <a
        href={href}
        target="_blank"
        rel="noopener noreferrer"
        className={linkClasses}
        aria-label={`${children} (opens in new tab)`}
      >
        {children}
      </a>
    );
  }

  return (
    <Link href={href} className={linkClasses}>
      {children}
    </Link>
  );
};

interface LinkCategory {
  title: string;
  links: Array<{
    label: string;
    href: string;
    external?: boolean;
  }>;
}

const Footer: React.FC = () => {
  const { isAuthenticated } = useAuthStore();
  const [email, setEmail] = useState('');
  const [subscribeStatus, setSubscribeStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle');
  const [selectedMetroIds, setSelectedMetroIds] = useState<string[]>([]);
  const [receiveAllLocations, setReceiveAllLocations] = useState(false);
  const [currentYear, setCurrentYear] = useState<number>(2025);

  // Set current year on client side only to avoid hydration mismatch
  React.useEffect(() => {
    setCurrentYear(new Date().getFullYear());
  }, []);

  const linkCategories: LinkCategory[] = [
    {
      title: 'Community',
      links: [
        { label: 'Events', href: '/events' },
        { label: 'Forums', href: '/forums' },
        { label: 'Cultural Hub', href: '/culture' },
        ...(isAuthenticated ? [{ label: 'Dashboard', href: '/dashboard' }] : []),
      ],
    },
    {
      title: 'Marketplace',
      links: [
        { label: 'Browse Listings', href: '/marketplace' },
        { label: 'Businesses', href: '/business' },
        { label: 'Services', href: '/services' },
        { label: 'Sell Items', href: '/marketplace/sell' },
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

  const handleNewsletterSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    console.log('[Footer] Newsletter form submitted:');
    console.log('  Email:', email);
    console.log('  Receive all locations:', receiveAllLocations);
    console.log('  Selected metro IDs:', selectedMetroIds);
    console.log('  Selected metro count:', selectedMetroIds.length);

    if (!email || !email.includes('@')) {
      console.log('[Footer] ❌ Validation failed: Invalid email');
      setSubscribeStatus('error');
      return;
    }

    if (!receiveAllLocations && selectedMetroIds.length === 0) {
      console.log('[Footer] ❌ Validation failed: No metros selected and not receiving all locations');
      setSubscribeStatus('error');
      return;
    }

    console.log('[Footer] ✅ Validation passed, submitting...');

    setSubscribeStatus('loading');

    try {
      // Call .NET backend API
      const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api';
      const response = await fetch(`${apiUrl}/newsletter/subscribe`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          Email: email,
          MetroAreaIds: receiveAllLocations ? [] : selectedMetroIds,
          ReceiveAllLocations: receiveAllLocations,
          Timestamp: new Date().toISOString(),
        }),
      });

      const data = await response.json();

      console.log('[Footer] Backend response:', response.status, data);

      if (data.success || data.Success) {
        setSubscribeStatus('success');
        setEmail('');
        setSelectedMetroIds([]);
        setReceiveAllLocations(false);

        // Reset status after 3 seconds
        setTimeout(() => {
          setSubscribeStatus('idle');
        }, 3000);
      } else {
        setSubscribeStatus('error');
      }
    } catch (error) {
      console.error('Newsletter subscription error:', error);
      setSubscribeStatus('error');
    }
  };

  return (
    <footer className="bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 text-white mt-24 relative overflow-hidden">
      {/* Decorative Background Pattern */}
      <div className="absolute inset-0 opacity-10">
        <div className="absolute inset-0" style={{
          backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
        }}></div>
      </div>

      <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        {/* Newsletter Section */}
        <div className="bg-white/10 backdrop-blur-sm rounded-2xl p-8 mb-12 border border-white/20">
          <div className="max-w-3xl mx-auto">
            <h3 className="text-2xl font-semibold mb-2 text-center">Stay Connected</h3>
            <p className="text-white/90 mb-6 text-center">Subscribe to our newsletter for the latest events and community updates.</p>

            <form onSubmit={handleNewsletterSubmit} className="space-y-4">
              <input
                type="email"
                placeholder="Enter your email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="w-full px-4 py-3 rounded-lg bg-white text-gray-900 placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-orange-500"
                aria-label="Email address for newsletter"
                disabled={subscribeStatus === 'loading'}
                required
              />

              <div className="bg-white/95 p-4 rounded-lg text-gray-800">
                <NewsletterMetroSelector
                  selectedMetroIds={selectedMetroIds}
                  receiveAllLocations={receiveAllLocations}
                  onMetrosChange={setSelectedMetroIds}
                  onReceiveAllChange={setReceiveAllLocations}
                  disabled={subscribeStatus === 'loading'}
                />
              </div>

              <button
                type="submit"
                className="w-full px-6 py-3 bg-gray-900 hover:bg-gray-800 text-white font-medium rounded-lg transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
                disabled={subscribeStatus === 'loading'}
                aria-label="Subscribe to newsletter"
              >
                {subscribeStatus === 'loading' ? 'Subscribing...' : subscribeStatus === 'success' ? 'Subscribed!' : 'Subscribe'}
              </button>
            </form>

            {subscribeStatus === 'error' && (
              <p className="text-red-300 text-sm mt-2 text-center" role="alert">
                Please enter a valid email address and select at least one location.
              </p>
            )}
            {subscribeStatus === 'success' && (
              <p className="text-green-300 text-sm mt-2 text-center" role="alert">
                Thank you for subscribing!
              </p>
            )}
          </div>
        </div>

        {/* Links Grid */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-8 mb-12">
          {linkCategories.map((category) => (
            <div key={category.title}>
              <h4 className="text-white font-semibold mb-4">{category.title}</h4>
              <ul className="space-y-2" role="list">
                {category.links.map((link) => (
                  <li key={link.label}>
                    <FooterLink href={link.href} external={link.external}>
                      {link.label}
                    </FooterLink>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        {/* Bottom Section */}
        <div className="pt-8 border-t border-white/20 flex flex-col md:flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <Logo size="md" showText={false} />
            <div>
              <div className="text-white font-semibold">LankaConnect</div>
              <div className="text-white/80 text-sm">&copy; {currentYear} All rights reserved</div>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <a
              href="https://facebook.com"
              target="_blank"
              rel="noopener noreferrer"
              className="text-white/80 hover:text-white hover:bg-white/10 p-2 rounded-lg transition-colors"
              aria-label="Facebook"
            >
              <Facebook className="h-5 w-5" />
            </a>
            <a
              href="https://twitter.com"
              target="_blank"
              rel="noopener noreferrer"
              className="text-white/80 hover:text-white hover:bg-white/10 p-2 rounded-lg transition-colors"
              aria-label="Twitter"
            >
              <Twitter className="h-5 w-5" />
            </a>
            <a
              href="https://instagram.com"
              target="_blank"
              rel="noopener noreferrer"
              className="text-white/80 hover:text-white hover:bg-white/10 p-2 rounded-lg transition-colors"
              aria-label="Instagram"
            >
              <Instagram className="h-5 w-5" />
            </a>
            <a
              href="https://youtube.com"
              target="_blank"
              rel="noopener noreferrer"
              className="text-white/80 hover:text-white hover:bg-white/10 p-2 rounded-lg transition-colors"
              aria-label="YouTube"
            >
              <Youtube className="h-5 w-5" />
            </a>
            <a
              href="mailto:contact@lankaconnect.com"
              className="text-white/80 hover:text-white hover:bg-white/10 p-2 rounded-lg transition-colors"
              aria-label="Email"
            >
              <Mail className="h-5 w-5" />
            </a>
          </div>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
