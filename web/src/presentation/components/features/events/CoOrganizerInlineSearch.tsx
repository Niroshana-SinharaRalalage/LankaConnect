/**
 * CoOrganizerInlineSearch Component
 *
 * Phase 6A.133 UX Fix: Inline search for registered LankaConnect users.
 * Used in EventCreationForm and EventEditForm to search and add co-organizers
 * directly during event creation/editing.
 */

'use client';

import React, { useState, useCallback, useRef, useEffect } from 'react';
import { Search, Loader2, UserPlus } from 'lucide-react';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type { UserSearchResultDto } from '@/infrastructure/api/types/events.types';

interface CoOrganizerInlineSearchProps {
  /** Called when a user is selected from search results */
  onSelectUser: (user: UserSearchResultDto) => void;
  /** User IDs already added as contacts (to prevent duplicates) */
  excludeUserIds?: string[];
  /** Called to close/hide the search */
  onClose: () => void;
}

export function CoOrganizerInlineSearch({
  onSelectUser,
  excludeUserIds = [],
  onClose,
}: CoOrganizerInlineSearchProps) {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<UserSearchResultDto[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const searchTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    inputRef.current?.focus();
  }, []);

  const handleSearch = useCallback(async (value: string) => {
    setQuery(value);
    setError(null);

    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current);
    }

    if (value.trim().length < 2) {
      setResults([]);
      return;
    }

    searchTimeoutRef.current = setTimeout(async () => {
      setIsSearching(true);
      try {
        const searchResults = await eventsRepository.searchUsers(value.trim());
        // Filter out already-added users
        const filtered = searchResults.filter(
          (u) => !excludeUserIds.includes(u.id)
        );
        setResults(filtered);
      } catch (err) {
        console.error('User search failed:', err);
        setError('Failed to search users.');
        setResults([]);
      } finally {
        setIsSearching(false);
      }
    }, 300);
  }, [excludeUserIds]);

  const handleSelect = (user: UserSearchResultDto) => {
    onSelectUser(user);
    setQuery('');
    setResults([]);
  };

  return (
    <div className="p-4 border border-[#FF7900] rounded-lg bg-orange-50 space-y-3">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <UserPlus className="h-4 w-4 text-[#FF7900]" />
          <span className="text-sm font-medium text-neutral-700">
            Search LankaConnect User
          </span>
        </div>
        <button
          type="button"
          onClick={onClose}
          className="text-sm text-neutral-500 hover:text-neutral-700"
        >
          Cancel
        </button>
      </div>

      <div className="relative">
        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-neutral-400" />
        <input
          ref={inputRef}
          type="text"
          placeholder="Search by name, email, or phone..."
          value={query}
          onChange={(e) => handleSearch(e.target.value)}
          className="w-full pl-10 pr-4 py-2 text-sm border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#FF7900] focus:border-transparent"
        />
        {isSearching && (
          <Loader2 className="absolute right-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-neutral-400 animate-spin" />
        )}
      </div>

      {error && (
        <p className="text-sm text-red-600">{error}</p>
      )}

      {results.length > 0 && (
        <div className="border border-neutral-200 rounded-lg divide-y divide-neutral-100 max-h-48 overflow-y-auto bg-white">
          {results.map((user) => (
            <button
              key={user.id}
              type="button"
              onClick={() => handleSelect(user)}
              className="w-full flex items-center gap-3 p-3 text-left hover:bg-neutral-50 transition-colors"
            >
              <div className="h-8 w-8 rounded-full bg-[#FFE8CC] flex items-center justify-center flex-shrink-0">
                <span className="text-sm font-medium text-[#8B1538]">
                  {user.displayName.charAt(0).toUpperCase()}
                </span>
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-neutral-800 truncate">
                  {user.displayName}
                </p>
                <p className="text-xs text-neutral-500 truncate">{user.email}</p>
              </div>
              <UserPlus className="h-4 w-4 text-[#FF7900] flex-shrink-0" />
            </button>
          ))}
        </div>
      )}

      {query.trim().length >= 2 && !isSearching && results.length === 0 && !error && (
        <p className="text-sm text-neutral-500 text-center py-2">
          No users found matching &ldquo;{query}&rdquo;
        </p>
      )}
    </div>
  );
}
