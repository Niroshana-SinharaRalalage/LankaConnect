'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { Logo } from '../atoms/Logo';
import { NewsletterMetroSelector } from '../features/newsletter/NewsletterMetroSelector';

interface FooterLinkProps {
  href: string;
  children: React.ReactNode;
  external?: boolean;
}

const FooterLink: React.FC<FooterLinkProps> = ({ href, children, external = false }) => {
  const linkClasses = "text-white/80 hover:text-[#FF7900] transition-colors duration-200";

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
  const [email, setEmail] = useState('');
  const [subscribeStatus, setSubscribeStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle');
  const [selectedMetroIds, setSelectedMetroIds] = useState<string[]>([]);
  const [receiveAllLocations, setReceiveAllLocations] = useState(false);
  const [currentYear, setCurrentYear] = useState<number>(2025); // Initialize with static value

  // Set current year on client side only to avoid hydration mismatch
  React.useEffect(() => {
    setCurrentYear(new Date().getFullYear());
  }, []);

  const linkCategories: LinkCategory[] = [
    {
      title: 'About',
      links: [
        { label: 'About Us', href: '/about' },
        { label: 'Our Mission', href: '/mission' },
        { label: 'Team', href: '/team' },
        { label: 'Contact', href: '/contact' },
      ],
    },
    {
      title: 'Community',
      links: [
        { label: 'Events', href: '/events' },
        { label: 'Forums', href: '/forums' },
        { label: 'Businesses', href: '/businesses' },
        { label: 'Culture', href: '/culture' },
      ],
    },
    {
      title: 'Resources',
      links: [
        { label: 'Help Center', href: '/help' },
        { label: 'Privacy Policy', href: '/privacy' },
        { label: 'Terms of Service', href: '/terms' },
        { label: 'FAQ', href: '/faq' },
      ],
    },
    {
      title: 'Connect',
      links: [
        { label: 'Facebook', href: 'https://facebook.com', external: true },
        { label: 'Twitter', href: 'https://twitter.com', external: true },
        { label: 'Instagram', href: 'https://instagram.com', external: true },
        { label: 'LinkedIn', href: 'https://linkedin.com', external: true },
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
    <footer className="bg-gradient-to-b from-[#8B1538] to-[#6B0F28] text-white" role="contentinfo">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8 py-12">
        {/* Logo, Tagline & Newsletter Section */}
        <div className="mb-12 pb-8 border-b border-white/10">
          <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-8">
            {/* Logo and Tagline */}
            <div className="flex-1">
              <div className="mb-3">
                <Logo size="md" />
              </div>
              <p className="text-white/70 text-sm max-w-md">
                Connecting the Sri Lankan diaspora worldwide. Share events, discover culture, and build community together.
              </p>
            </div>

            {/* Newsletter Signup */}
            <div className="flex-1 max-w-md">
              <h3 className="text-lg font-semibold mb-3">Stay Connected</h3>
              <p className="text-white/70 text-sm mb-4">
                Subscribe to our newsletter for the latest events and community updates.
              </p>
              <form onSubmit={handleNewsletterSubmit} className="space-y-3">
                <input
                  type="email"
                  placeholder="Enter your email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full px-4 py-2.5 rounded-lg bg-white text-[#8B1538] placeholder-[#8B1538]/50 focus:outline-none focus:ring-2 focus:ring-[#FF7900]"
                  aria-label="Email address for newsletter"
                  disabled={subscribeStatus === 'loading'}
                  required
                />

                {/* Metro Area Selection Component - Phase 5B.8 */}
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
                  className="w-full px-6 py-2.5 bg-[#FF7900] hover:bg-[#E56D00] text-white font-medium rounded-lg transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
                  disabled={subscribeStatus === 'loading'}
                  aria-label="Subscribe to newsletter"
                >
                  {subscribeStatus === 'loading' ? 'Subscribing...' : subscribeStatus === 'success' ? 'Subscribed!' : 'Subscribe'}
                </button>
              </form>
              {subscribeStatus === 'error' && (
                <p className="text-red-300 text-sm mt-2" role="alert">
                  Please enter a valid email address and select at least one location.
                </p>
              )}
              {subscribeStatus === 'success' && (
                <p className="text-green-300 text-sm mt-2" role="alert">
                  Thank you for subscribing!
                </p>
              )}
            </div>
          </div>
        </div>

        {/* Link Categories Grid */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-8 mb-12">
          {linkCategories.map((category) => (
            <div key={category.title}>
              <h3 className="text-lg font-semibold mb-4 text-white">{category.title}</h3>
              <ul className="space-y-3" role="list">
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

        {/* Copyright Section */}
        <div className="pt-8 border-t border-white/10">
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 text-sm text-white/60">
            <p>
              &copy; {currentYear} LankaConnect. All rights reserved.
            </p>
            <div className="flex gap-6">
              <Link href="/privacy" className="hover:text-[#FF7900] transition-colors duration-200">
                Privacy
              </Link>
              <Link href="/terms" className="hover:text-[#FF7900] transition-colors duration-200">
                Terms
              </Link>
              <Link href="/cookies" className="hover:text-[#FF7900] transition-colors duration-200">
                Cookies
              </Link>
            </div>
          </div>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
