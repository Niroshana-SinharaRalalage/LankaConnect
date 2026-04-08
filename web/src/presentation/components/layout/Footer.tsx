'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import toast from 'react-hot-toast';
import { Logo } from '../atoms/Logo';
import { NewsletterMetroSelector } from '../features/newsletter/NewsletterMetroSelector';
import { Facebook, Twitter, Instagram, Youtube, Mail } from 'lucide-react';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { WhatsAppInlineOptIn } from '../features/whatsapp/WhatsAppInlineOptIn';
import { toE164 } from '@/presentation/lib/validators/whatsapp.schemas';

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
  // Phase 6A.X Issue #27/#28: Separate validation error state for inline display
  const [validationError, setValidationError] = useState<string | null>(null);
  // Phase 6A.99 Issue #57: Inline success message instead of toast
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  // Phase 6A.99: Server error message for inline display
  const [serverError, setServerError] = useState<string | null>(null);
  // Phase 7A.6C: WhatsApp opt-in state
  const [whatsAppEnabled, setWhatsAppEnabled] = useState(false);
  const [whatsAppPhone, setWhatsAppPhone] = useState('');

  // Set current year on client side only to avoid hydration mismatch
  React.useEffect(() => {
    setCurrentYear(new Date().getFullYear());
  }, []);

  // Phase 6A.76: Updated footer links - removed Cultural Hub, Services, Sell Items,
  // entire Resources category, Careers, Press. Renamed "Our Story" to "About Us".
  const linkCategories: LinkCategory[] = [
    {
      title: 'Community',
      links: [
        { label: 'Events', href: '/events' },
        { label: 'Forums', href: '/forums' },
        ...(isAuthenticated ? [{ label: 'Dashboard', href: '/dashboard' }] : []),
      ],
    },
    {
      title: 'Marketplace',
      links: [
        { label: 'Browse Listings', href: '/marketplace' },
        { label: 'Businesses', href: '/business' },
      ],
    },
    {
      title: 'About',
      links: [
        { label: 'About Us', href: '/about' },
        { label: 'Contact Us', href: '/contact' },
      ],
    },
  ];

  // Phase 6A.99 Issue #57: Refactored to use inline messages instead of toast
  const handleNewsletterSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setValidationError(null);
    setSuccessMessage(null);
    setServerError(null);

    console.log('[Footer] Newsletter form submitted:');
    console.log('  Email:', email);
    console.log('  Receive all locations:', receiveAllLocations);
    console.log('  Selected metro IDs:', selectedMetroIds);
    console.log('  Selected metro count:', selectedMetroIds.length);

    // Client-side validation - use inline errors
    if (!email || !email.includes('@')) {
      console.log('[Footer] ❌ Validation failed: Invalid email');
      setValidationError('Please enter a valid email address.');
      return;
    }

    if (!receiveAllLocations && selectedMetroIds.length === 0) {
      console.log('[Footer] ❌ Validation failed: No metros selected and not receiving all locations');
      setValidationError('Please select at least one location or choose "All Locations".');
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
          // Phase 7A.6C: WhatsApp opt-in
          ...(whatsAppEnabled && whatsAppPhone && {
            WhatsAppPhoneNumber: toE164(whatsAppPhone),
          }),
        }),
      });

      const data = await response.json();

      console.log('[Footer] Backend response:', response.status, data);

      if (data.success || data.Success) {
        // Phase 6A.99 Issue #57: Use inline success message instead of toast
        setSuccessMessage('Thank you for subscribing! Please check your email to confirm your subscription.');
        setSubscribeStatus('success');
        setEmail('');
        setSelectedMetroIds([]);
        setReceiveAllLocations(false);
        setWhatsAppEnabled(false);
        setWhatsAppPhone('');

        // Auto-clear success message after 10 seconds
        setTimeout(() => {
          setSuccessMessage(null);
          setSubscribeStatus('idle');
        }, 10000);
      } else {
        // Phase 6A.99 Issue #57: Show specific backend error message inline
        const errorMessage = data.message || data.Message || 'Subscription failed. Please try again.';

        // Special handling for already-subscribed (friendlier message)
        if (errorMessage.toLowerCase().includes('already subscribed')) {
          setServerError('This email is already subscribed to our newsletter.');
        } else {
          setServerError(errorMessage);
        }
        setSubscribeStatus('idle');
      }
    } catch (error) {
      console.error('Newsletter subscription error:', error);
      setServerError('Network error. Please check your connection and try again.');
      setSubscribeStatus('idle');
    }
  };

  return (
    <footer className="bg-transparent text-white mt-0 relative overflow-hidden">

      <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        {/* Newsletter Section */}
        <div className="bg-white/10 backdrop-blur-sm rounded-2xl p-8 mb-12 border border-white/20">
          <div className="max-w-xl mx-auto">
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

              {/* Phase 7A.6C: WhatsApp opt-in for newsletter subscribers */}
              <div className="bg-white/95 p-4 rounded-lg text-gray-800">
                <WhatsAppInlineOptIn
                  enabled={whatsAppEnabled}
                  onEnabledChange={(enabled) => {
                    setWhatsAppEnabled(enabled);
                    if (!enabled) setWhatsAppPhone('');
                  }}
                  phoneNumber={whatsAppPhone}
                  onPhoneNumberChange={setWhatsAppPhone}
                  disabled={subscribeStatus === 'loading'}
                  description="Also receive community updates and event alerts via WhatsApp."
                />
              </div>

              <button
                type="submit"
                className="w-full px-6 py-3 bg-[#FF7900] hover:bg-[#E56D00] text-white font-medium rounded-lg transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
                disabled={subscribeStatus === 'loading'}
                aria-label="Subscribe to newsletter"
              >
                {subscribeStatus === 'loading' ? 'Subscribing...' : subscribeStatus === 'success' ? 'Subscribed!' : 'Subscribe'}
              </button>
            </form>

            {/* Phase 6A.99 Issue #57: Inline messages for validation, success, and errors */}
            {validationError && subscribeStatus !== 'loading' && (
              <p className="text-red-300 text-sm mt-3 text-center" role="alert">
                {validationError}
              </p>
            )}

            {/* Phase 6A.99: Inline success message */}
            {successMessage && (
              <div className="mt-4 p-3 bg-green-500/20 border border-green-400/30 rounded-lg" role="status">
                <p className="text-green-200 text-sm text-center flex items-center justify-center gap-2">
                  <svg className="w-5 h-5 text-green-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  {successMessage}
                </p>
              </div>
            )}

            {/* Phase 6A.99: Inline server error message */}
            {serverError && subscribeStatus !== 'loading' && (
              <div className="mt-4 p-3 bg-red-500/20 border border-red-400/30 rounded-lg" role="alert">
                <p className="text-red-200 text-sm text-center flex items-center justify-center gap-2">
                  <svg className="w-5 h-5 text-red-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  {serverError}
                </p>
              </div>
            )}
          </div>
        </div>

        {/* Links Grid - Phase 6A.76: Changed from 4 to 3 columns after removing Resources category */}
        <div className="grid grid-cols-2 md:grid-cols-3 gap-8 mb-12">
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
