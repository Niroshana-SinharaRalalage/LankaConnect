'use client';

/**
 * MetroAreaContext
 *
 * React Context for global metro area selection and user location state.
 * Provides:
 * - Selected metro area
 * - User's detected location
 * - Detection state (loading, error)
 * - Persistence to localStorage
 */

import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import type { MetroArea } from '@/domain/models/MetroArea';
import type { UserLocation } from '@/domain/models/Location';
import { requestGeolocationWithError } from '@/presentation/utils/geolocation';
import { calculateDistance } from '@/presentation/utils/distance';

/**
 * Metro area context state
 */
interface MetroAreaContextState {
  selectedMetroArea: MetroArea | null;
  userLocation: UserLocation | null;
  isDetecting: boolean;
  detectionError: string | null;
  availableMetros: readonly MetroArea[];
  setMetroArea: (metro: MetroArea | null) => void;
  detectLocation: () => Promise<void>;
  clearLocation: () => void;
  setAvailableMetros: (metros: readonly MetroArea[]) => void;
  findClosestMetro: (location: UserLocation) => MetroArea | null;
}

/**
 * Default context value
 */
const defaultContextValue: MetroAreaContextState = {
  selectedMetroArea: null,
  userLocation: null,
  isDetecting: false,
  detectionError: null,
  availableMetros: [],
  setMetroArea: () => {},
  detectLocation: async () => {},
  clearLocation: () => {},
  setAvailableMetros: () => {},
  findClosestMetro: () => null,
};

/**
 * Metro area context
 */
const MetroAreaContext = createContext<MetroAreaContextState>(defaultContextValue);

/**
 * localStorage keys
 */
const STORAGE_KEYS = {
  SELECTED_METRO: 'lankaconnect_selected_metro',
  USER_LOCATION: 'lankaconnect_user_location',
} as const;

/**
 * Metro area provider props
 */
interface MetroAreaProviderProps {
  children: React.ReactNode;
  /**
   * Auto-select closest metro when location is detected
   * @default true
   */
  autoSelectClosest?: boolean;
}

/**
 * Metro area provider component
 *
 * Manages global metro area selection and user location.
 * Persists state to localStorage.
 */
export function MetroAreaProvider({ children, autoSelectClosest = true }: MetroAreaProviderProps) {
  const [selectedMetroArea, setSelectedMetroArea] = useState<MetroArea | null>(null);
  const [userLocation, setUserLocation] = useState<UserLocation | null>(null);
  const [isDetecting, setIsDetecting] = useState(false);
  const [detectionError, setDetectionError] = useState<string | null>(null);
  const [availableMetros, setAvailableMetros] = useState<readonly MetroArea[]>([]);

  /**
   * Load persisted state from localStorage on mount
   */
  useEffect(() => {
    try {
      // Load selected metro area
      const storedMetro = localStorage.getItem(STORAGE_KEYS.SELECTED_METRO);
      if (storedMetro) {
        const metro = JSON.parse(storedMetro) as MetroArea;
        setSelectedMetroArea(metro);
      }

      // Load user location
      const storedLocation = localStorage.getItem(STORAGE_KEYS.USER_LOCATION);
      if (storedLocation) {
        const location = JSON.parse(storedLocation);
        // Convert timestamp string back to Date
        const userLoc: UserLocation = {
          ...location,
          timestamp: new Date(location.timestamp),
        };
        setUserLocation(userLoc);
      }
    } catch (error) {
      console.error('Failed to load metro area state from localStorage:', error);
    }
  }, []);

  /**
   * Set selected metro area and persist to localStorage
   */
  const setMetroArea = useCallback((metro: MetroArea | null) => {
    setSelectedMetroArea(metro);

    try {
      if (metro) {
        localStorage.setItem(STORAGE_KEYS.SELECTED_METRO, JSON.stringify(metro));
      } else {
        localStorage.removeItem(STORAGE_KEYS.SELECTED_METRO);
      }
    } catch (error) {
      console.error('Failed to save metro area to localStorage:', error);
    }
  }, []);

  /**
   * Find the closest metro area to a given location
   */
  const findClosestMetro = useCallback((location: UserLocation): MetroArea | null => {
    if (availableMetros.length === 0) {
      return null;
    }

    // Filter out state-level metros (those starting with 'all-')
    const regionalMetros = availableMetros.filter(metro => !metro.id.startsWith('all-'));

    if (regionalMetros.length === 0) {
      return null;
    }

    // Calculate distances and find closest
    let closestMetro: MetroArea | null = null;
    let closestDistance = Infinity;

    regionalMetros.forEach(metro => {
      const distance = calculateDistance(
        location.latitude,
        location.longitude,
        metro.centerLat,
        metro.centerLng
      );

      if (distance < closestDistance) {
        closestDistance = distance;
        closestMetro = metro;
      }
    });

    return closestMetro;
  }, [availableMetros]);

  /**
   * Detect user's current location using geolocation API
   */
  const detectLocation = useCallback(async () => {
    setIsDetecting(true);
    setDetectionError(null);

    try {
      const { location, error } = await requestGeolocationWithError();

      if (location) {
        setUserLocation(location);

        // Persist to localStorage
        try {
          localStorage.setItem(STORAGE_KEYS.USER_LOCATION, JSON.stringify(location));
        } catch (storageError) {
          console.error('Failed to save location to localStorage:', storageError);
        }

        // Auto-select closest metro if enabled
        if (autoSelectClosest && availableMetros.length > 0) {
          const closest = findClosestMetro(location);
          if (closest) {
            setMetroArea(closest);
          }
        }
      } else if (error) {
        setDetectionError(error.message);
      } else {
        setDetectionError('Failed to detect location. Please try again.');
      }
    } catch (error) {
      console.error('Unexpected error during location detection:', error);
      setDetectionError('An unexpected error occurred. Please try again.');
    } finally {
      setIsDetecting(false);
    }
  }, [autoSelectClosest, availableMetros, findClosestMetro, setMetroArea]);

  /**
   * Clear user location and remove from localStorage
   */
  const clearLocation = useCallback(() => {
    setUserLocation(null);
    setDetectionError(null);

    try {
      localStorage.removeItem(STORAGE_KEYS.USER_LOCATION);
    } catch (error) {
      console.error('Failed to remove location from localStorage:', error);
    }
  }, []);

  const contextValue: MetroAreaContextState = {
    selectedMetroArea,
    userLocation,
    isDetecting,
    detectionError,
    availableMetros,
    setMetroArea,
    detectLocation,
    clearLocation,
    setAvailableMetros,
    findClosestMetro,
  };

  return (
    <MetroAreaContext.Provider value={contextValue}>
      {children}
    </MetroAreaContext.Provider>
  );
}

/**
 * Hook to use metro area context
 *
 * @throws Error if used outside MetroAreaProvider
 */
export function useMetroArea(): MetroAreaContextState {
  const context = useContext(MetroAreaContext);

  if (!context) {
    throw new Error('useMetroArea must be used within MetroAreaProvider');
  }

  return context;
}
