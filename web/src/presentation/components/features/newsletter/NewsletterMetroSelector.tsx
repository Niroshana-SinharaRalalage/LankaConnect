'use client';

import { useState } from 'react';
import { ChevronDown, ChevronRight } from 'lucide-react';
import {
  getMetrosGroupedByState,
  US_STATES,
  isStateLevelArea,
  getStateLevelAreas,
} from '@/domain/constants/metroAreas.constants';

interface NewsletterMetroSelectorProps {
  selectedMetroIds: string[];
  receiveAllLocations: boolean;
  onMetrosChange: (metroIds: string[]) => void;
  onReceiveAllChange: (receiveAll: boolean) => void;
  disabled?: boolean;
  maxSelections?: number;
}

/**
 * NewsletterMetroSelector Component
 * Phase 5B.8: Newsletter Metro Area Selection
 *
 * Allows users to select multiple metro areas for newsletter subscriptions
 * or opt for all locations. Uses hierarchical state-grouped dropdown pattern.
 */
export function NewsletterMetroSelector({
  selectedMetroIds,
  receiveAllLocations,
  onMetrosChange,
  onReceiveAllChange,
  disabled = false,
  maxSelections = 20,
}: NewsletterMetroSelectorProps) {
  const [expandedStates, setExpandedStates] = useState<Set<string>>(new Set());
  const [validationError, setValidationError] = useState<string>('');

  const metrosByState = getMetrosGroupedByState();

  const toggleStateExpansion = (stateCode: string) => {
    const newExpanded = new Set(expandedStates);
    if (newExpanded.has(stateCode)) {
      newExpanded.delete(stateCode);
    } else {
      newExpanded.add(stateCode);
    }
    setExpandedStates(newExpanded);
  };

  const handleToggleMetroArea = (metroId: string) => {
    const newSelection = selectedMetroIds.includes(metroId)
      ? selectedMetroIds.filter((id) => id !== metroId)
      : selectedMetroIds.length < maxSelections
        ? [...selectedMetroIds, metroId]
        : selectedMetroIds;

    if (newSelection.length > maxSelections) {
      setValidationError(`You cannot select more than ${maxSelections} metro areas`);
    } else {
      setValidationError('');
      onMetrosChange(newSelection);
    }
  };

  const handleReceiveAllChange = (receiveAll: boolean) => {
    onReceiveAllChange(receiveAll);
    if (receiveAll) {
      onMetrosChange([]); // Clear selections when choosing all locations
      setValidationError('');
    }
  };

  return (
    <div className="space-y-4">
      {/* Header */}
      <div>
        <label className="text-sm font-medium text-gray-700 mb-2 block">
          📍 Get notifications for events in:
        </label>
        <p className="text-xs text-gray-500 mb-3">
          Select up to {maxSelections} metro areas or receive updates from all locations
        </p>
      </div>

      {/* All Locations Checkbox */}
      <div className="mb-4">
        <label className="flex items-center text-sm text-gray-700 cursor-pointer">
          <input
            type="checkbox"
            checked={receiveAllLocations}
            onChange={(e) => handleReceiveAllChange(e.target.checked)}
            disabled={disabled}
            className="mr-2 w-4 h-4 rounded border-gray-300 text-[#FF7900] focus:ring-2 focus:ring-[#FF7900]"
          />
          <span className="font-medium">Send me events from all locations</span>
        </label>
      </div>

      {/* Metro Areas Selection (hidden when "all locations" is selected) */}
      {!receiveAllLocations && (
        <div className="border rounded-lg p-4 bg-white space-y-4">
          {/* State-Level Areas */}
          <div>
            <h4 className="text-xs font-semibold uppercase tracking-wider text-gray-700 mb-3">
              State-Wide Selections
            </h4>
            <div className="space-y-2">
              {getStateLevelAreas().map((metro) => {
                const isSelected = selectedMetroIds.includes(metro.id);
                return (
                  <label
                    key={metro.id}
                    className={`flex items-center gap-3 p-2 rounded-md border cursor-pointer transition-colors ${
                      disabled ? 'opacity-50 cursor-not-allowed' : ''
                    }`}
                    style={{
                      background: isSelected ? '#FFE8CC' : 'white',
                      borderColor: isSelected ? '#FF7900' : '#e2e8f0',
                    }}
                  >
                    <input
                      type="checkbox"
                      checked={isSelected}
                      onChange={() => handleToggleMetroArea(metro.id)}
                      disabled={disabled}
                      className="h-4 w-4 rounded border-gray-300"
                      aria-label={`Select all of ${metro.name}`}
                    />
                    <span className="flex-1 text-sm font-medium">All {metro.name}</span>
                  </label>
                );
              })}
            </div>
          </div>

          {/* Divider */}
          <div className="h-px bg-gray-200" />

          {/* City-Level Areas */}
          <div>
            <h4 className="text-xs font-semibold uppercase tracking-wider text-gray-700 mb-3">
              City Metro Areas
            </h4>
            <div className="space-y-2">
              {US_STATES.map((state) => {
                const metrosForState = metrosByState.get(state.code) || [];
                const cityMetros = metrosForState.filter((m) => !isStateLevelArea(m.id));

                if (cityMetros.length === 0) return null;

                const isExpanded = expandedStates.has(state.code);
                const selectedCountInState = selectedMetroIds.filter(
                  (id) => metrosForState.map((m) => m.id).includes(id) && !isStateLevelArea(id)
                ).length;

                return (
                  <div
                    key={state.code}
                    className="border rounded-md overflow-hidden"
                    style={{ borderColor: '#e2e8f0' }}
                  >
                    {/* State Header */}
                    <button
                      onClick={() => toggleStateExpansion(state.code)}
                      disabled={disabled}
                      className={`w-full flex items-center gap-2 p-3 text-left transition-colors text-sm ${
                        disabled ? 'opacity-50 cursor-not-allowed' : 'hover:bg-gray-50'
                      }`}
                      aria-expanded={isExpanded}
                      aria-controls={`newsletter-metros-${state.code}`}
                    >
                      {isExpanded ? (
                        <ChevronDown className="h-4 w-4" style={{ color: '#FF7900' }} />
                      ) : (
                        <ChevronRight className="h-4 w-4" style={{ color: '#FF7900' }} />
                      )}
                      <span className="flex-1 font-medium">{state.name}</span>
                      {selectedCountInState > 0 && (
                        <span
                          className="text-xs font-semibold px-2 py-0.5 rounded-full"
                          style={{ background: '#FFE8CC', color: '#8B1538' }}
                        >
                          {selectedCountInState} selected
                        </span>
                      )}
                    </button>

                    {/* Expandable Metros */}
                    {isExpanded && (
                      <div
                        id={`newsletter-metros-${state.code}`}
                        className="space-y-2 p-3 bg-gray-50 border-t"
                        style={{ borderColor: '#e2e8f0' }}
                      >
                        {cityMetros.map((metro) => {
                          const isSelected = selectedMetroIds.includes(metro.id);
                          return (
                            <label
                              key={metro.id}
                              className={`flex items-start gap-3 p-2 rounded-md border cursor-pointer transition-colors ${
                                disabled ? 'opacity-50 cursor-not-allowed' : ''
                              }`}
                              style={{
                                background: isSelected ? '#FFE8CC' : 'white',
                                borderColor: isSelected ? '#FF7900' : '#e2e8f0',
                              }}
                            >
                              <input
                                type="checkbox"
                                checked={isSelected}
                                onChange={() => handleToggleMetroArea(metro.id)}
                                disabled={disabled}
                                className="mt-0.5 h-4 w-4 rounded border-gray-300"
                                aria-label={`${metro.name}, ${metro.state}`}
                              />
                              <div className="flex-1 min-w-0">
                                <div className="text-sm font-medium">{metro.name}</div>
                                <div className="text-xs text-gray-500">
                                  {metro.cities.slice(0, 2).join(', ')}
                                  {metro.cities.length > 2 && `, +${metro.cities.length - 2} more`}
                                </div>
                              </div>
                            </label>
                          );
                        })}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          </div>

          {/* Selection Counter & Validation */}
          <div className="pt-2 border-t" style={{ borderColor: '#e2e8f0' }}>
            <p className="text-xs text-gray-600">
              {selectedMetroIds.length} of {maxSelections} selected
            </p>
            {validationError && (
              <p className="text-xs text-red-600 mt-1" role="alert">
                {validationError}
              </p>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
