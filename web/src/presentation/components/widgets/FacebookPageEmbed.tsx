'use client';

import { useEffect, useRef, useState } from 'react';

// Extend Window interface for Facebook SDK
declare global {
  interface Window {
    fbAsyncInit?: () => void;
    FB?: {
      init: (params: { xfbml: boolean; version: string }) => void;
      XFBML: {
        parse: (element?: HTMLElement) => void;
      };
    };
  }
}

interface FacebookPageEmbedProps {
  /** Facebook page URL */
  pageUrl: string;
  /** Tabs to display: 'timeline', 'events', 'messages' */
  tabs?: string;
  /** Width of the embed in pixels (180-500, or empty for auto) */
  width?: number;
  /** Height of the embed in pixels */
  height?: number;
  /** Show small header */
  smallHeader?: boolean;
  /** Hide cover photo */
  hideCover?: boolean;
  /** Show friend faces */
  showFacepile?: boolean;
  /** Adapt to container width */
  adaptContainerWidth?: boolean;
}

/**
 * Facebook Page Plugin embed component.
 * Loads the Facebook SDK and renders the page plugin with the specified configuration.
 * Falls back to a direct link if the embed fails to load.
 */
export function FacebookPageEmbed({
  pageUrl,
  tabs = 'timeline',
  width,
  height = 500,
  smallHeader = true,
  hideCover = false,
  showFacepile = false,
  adaptContainerWidth = true,
}: FacebookPageEmbedProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [sdkLoaded, setSdkLoaded] = useState(false);
  const [loadError, setLoadError] = useState(false);

  useEffect(() => {
    // Check if SDK is already loaded
    if (window.FB) {
      setSdkLoaded(true);
      return;
    }

    // Set up the async init callback
    window.fbAsyncInit = () => {
      window.FB?.init({
        xfbml: true,
        version: 'v21.0',
      });
      setSdkLoaded(true);
    };

    // Check if the script tag already exists
    if (document.getElementById('facebook-jssdk')) {
      return;
    }

    // Load the Facebook SDK
    const script = document.createElement('script');
    script.id = 'facebook-jssdk';
    script.src = 'https://connect.facebook.net/en_US/sdk.js#xfbml=1&version=v21.0';
    script.async = true;
    script.defer = true;
    script.crossOrigin = 'anonymous';
    script.onerror = () => {
      setLoadError(true);
    };

    // Set a timeout in case the SDK fails to load (e.g., ad blockers)
    const timeout = setTimeout(() => {
      if (!window.FB) {
        setLoadError(true);
      }
    }, 10000);

    document.body.appendChild(script);

    return () => {
      clearTimeout(timeout);
    };
  }, []);

  // Re-parse XFBML when SDK loads or container changes
  useEffect(() => {
    if (sdkLoaded && window.FB && containerRef.current) {
      window.FB.XFBML.parse(containerRef.current);
    }
  }, [sdkLoaded]);

  // Fallback UI when SDK fails to load (ad blockers, etc.)
  if (loadError) {
    return (
      <div className="text-center py-6">
        <div className="w-12 h-12 mx-auto mb-3 rounded-full bg-blue-50 flex items-center justify-center">
          <svg className="w-6 h-6 text-blue-600" fill="currentColor" viewBox="0 0 24 24">
            <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z" />
          </svg>
        </div>
        <p className="text-sm text-neutral-600 mb-2">View our marketplace on Facebook</p>
        <a
          href={pageUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 transition-colors"
        >
          <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
            <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z" />
          </svg>
          Visit LankaConnect on Facebook
        </a>
      </div>
    );
  }

  return (
    <div ref={containerRef}>
      {/* fb-root is required by the SDK */}
      <div id="fb-root"></div>
      <div
        className="fb-page"
        data-href={pageUrl}
        data-tabs={tabs}
        data-width={width || ''}
        data-height={height}
        data-small-header={smallHeader}
        data-adapt-container-width={adaptContainerWidth}
        data-hide-cover={hideCover}
        data-show-facepile={showFacepile}
      >
        {/* Fallback content shown while loading */}
        <blockquote cite={pageUrl} className="fb-xfbml-parse-ignore">
          <a href={pageUrl} target="_blank" rel="noopener noreferrer">
            <div className="flex items-center justify-center py-8">
              <div className="animate-pulse text-center">
                <div className="w-10 h-10 mx-auto mb-2 rounded-full bg-neutral-200"></div>
                <div className="h-3 w-24 mx-auto bg-neutral-200 rounded"></div>
              </div>
            </div>
          </a>
        </blockquote>
      </div>
    </div>
  );
}
