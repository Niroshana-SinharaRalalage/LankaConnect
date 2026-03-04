/**
 * CoOrganizerSearchModal Component
 *
 * Phase 6A.133: Search and select registered users to link as co-organizers.
 * Supports multi-select and batch linking.
 */

'use client';

import React, { useState, useCallback, useRef } from 'react';
import { Search, X, UserPlus, Loader2, Check } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/presentation/components/ui/Dialog';
import { Button } from '@/presentation/components/ui/Button';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type { UserSearchResultDto, OrganizerContactDto } from '@/infrastructure/api/types/events.types';

interface CoOrganizerSearchModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  eventId: string;
  /** Contacts that don't yet have a linked user */
  unlinkableContacts: OrganizerContactDto[];
  onLinkComplete: () => Promise<any>;
}

interface ContactUserMapping {
  contactId: string;
  contactName: string;
  userId: string;
  userDisplayName: string;
}

export function CoOrganizerSearchModal({
  open,
  onOpenChange,
  eventId,
  unlinkableContacts,
  onLinkComplete,
}: CoOrganizerSearchModalProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<UserSearchResultDto[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [selectedMappings, setSelectedMappings] = useState<ContactUserMapping[]>([]);
  const [assigningContactId, setAssigningContactId] = useState<string | null>(null);
  const [isLinking, setIsLinking] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const searchTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const handleSearch = useCallback(async (query: string) => {
    setSearchQuery(query);
    setError(null);

    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current);
    }

    if (query.trim().length < 2) {
      setSearchResults([]);
      return;
    }

    searchTimeoutRef.current = setTimeout(async () => {
      setIsSearching(true);
      try {
        const results = await eventsRepository.searchUsers(query.trim());
        setSearchResults(results);
      } catch (err) {
        console.error('User search failed:', err);
        setError('Failed to search users. Please try again.');
        setSearchResults([]);
      } finally {
        setIsSearching(false);
      }
    }, 300);
  }, []);

  const handleSelectUser = (user: UserSearchResultDto) => {
    if (!assigningContactId) return;

    const contact = unlinkableContacts.find(c => c.id === assigningContactId);
    if (!contact) return;

    // Remove any existing mapping for this contact or user
    setSelectedMappings(prev => [
      ...prev.filter(m => m.contactId !== assigningContactId && m.userId !== user.id),
      {
        contactId: assigningContactId,
        contactName: contact.contactName,
        userId: user.id,
        userDisplayName: user.displayName,
      },
    ]);
    setAssigningContactId(null);
    setSearchQuery('');
    setSearchResults([]);
  };

  const handleRemoveMapping = (contactId: string) => {
    setSelectedMappings(prev => prev.filter(m => m.contactId !== contactId));
  };

  const handleBatchLink = async () => {
    if (selectedMappings.length === 0) return;

    setIsLinking(true);
    setError(null);

    try {
      await eventsRepository.batchLinkOrganizerContacts(
        eventId,
        selectedMappings.map(m => ({ contactId: m.contactId, userId: m.userId }))
      );
      await onLinkComplete();
      handleClose();
    } catch (err: any) {
      console.error('Batch link failed:', err);
      setError(err?.message || 'Failed to link co-organizers. Please try again.');
    } finally {
      setIsLinking(false);
    }
  };

  const handleClose = () => {
    setSearchQuery('');
    setSearchResults([]);
    setSelectedMappings([]);
    setAssigningContactId(null);
    setError(null);
    onOpenChange(false);
  };

  // Contacts that haven't been mapped yet
  const unmappedContacts = unlinkableContacts.filter(
    c => !selectedMappings.some(m => m.contactId === c.id)
  );

  // Users already selected (to prevent duplicates in search results)
  const selectedUserIds = new Set(selectedMappings.map(m => m.userId));

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="max-w-2xl max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Link Co-Organizers</DialogTitle>
          <DialogDescription>
            Search for registered users and assign them to organizer contacts.
            Linked users gain full event management access.
          </DialogDescription>
        </DialogHeader>

        {error && (
          <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-sm text-red-700">
            {error}
          </div>
        )}

        {/* Step 1: Unlinked contacts list */}
        {!assigningContactId && (
          <div className="space-y-3">
            <h4 className="text-sm font-medium text-neutral-700">
              Select a contact to assign a user:
            </h4>
            {unmappedContacts.length === 0 && selectedMappings.length === 0 ? (
              <p className="text-sm text-neutral-500 italic">
                All organizer contacts already have linked users.
              </p>
            ) : (
              <>
                {unmappedContacts.map(contact => (
                  <button
                    key={contact.id}
                    onClick={() => setAssigningContactId(contact.id)}
                    className="w-full flex items-center justify-between p-3 border border-neutral-200 rounded-lg hover:bg-neutral-50 transition-colors text-left"
                  >
                    <div>
                      <span className="font-medium text-neutral-800">{contact.contactName}</span>
                      {contact.contactEmail && (
                        <span className="text-sm text-neutral-500 ml-2">{contact.contactEmail}</span>
                      )}
                    </div>
                    <UserPlus className="h-4 w-4 text-[#FF7900]" />
                  </button>
                ))}
              </>
            )}

            {/* Show pending mappings */}
            {selectedMappings.length > 0 && (
              <div className="mt-4 space-y-2">
                <h4 className="text-sm font-medium text-neutral-700">
                  Pending assignments ({selectedMappings.length}):
                </h4>
                {selectedMappings.map(mapping => (
                  <div
                    key={mapping.contactId}
                    className="flex items-center justify-between p-3 bg-green-50 border border-green-200 rounded-lg"
                  >
                    <div className="flex items-center gap-2">
                      <Check className="h-4 w-4 text-green-600" />
                      <span className="text-sm text-neutral-800">
                        <span className="font-medium">{mapping.contactName}</span>
                        <span className="text-neutral-500 mx-1">&rarr;</span>
                        <span className="font-medium text-green-700">{mapping.userDisplayName}</span>
                      </span>
                    </div>
                    <button
                      onClick={() => handleRemoveMapping(mapping.contactId)}
                      className="p-1 text-neutral-400 hover:text-red-600 rounded"
                      title="Remove assignment"
                    >
                      <X className="h-4 w-4" />
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* Step 2: User search (shown when a contact is selected) */}
        {assigningContactId && (
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <h4 className="text-sm font-medium text-neutral-700">
                Search user for:{' '}
                <span className="text-[#8B1538]">
                  {unlinkableContacts.find(c => c.id === assigningContactId)?.contactName}
                </span>
              </h4>
              <button
                onClick={() => {
                  setAssigningContactId(null);
                  setSearchQuery('');
                  setSearchResults([]);
                }}
                className="text-sm text-neutral-500 hover:text-neutral-700"
              >
                Back
              </button>
            </div>

            {/* Search input */}
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-neutral-400" />
              <input
                type="text"
                placeholder="Search by name, email, or phone..."
                value={searchQuery}
                onChange={(e) => handleSearch(e.target.value)}
                className="w-full pl-10 pr-4 py-2 text-sm border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#FF7900] focus:border-transparent"
                autoFocus
              />
              {isSearching && (
                <Loader2 className="absolute right-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-neutral-400 animate-spin" />
              )}
            </div>

            {/* Search results */}
            {searchResults.length > 0 && (
              <div className="border border-neutral-200 rounded-lg divide-y divide-neutral-100 max-h-60 overflow-y-auto">
                {searchResults.map(user => {
                  const alreadySelected = selectedUserIds.has(user.id);
                  return (
                    <button
                      key={user.id}
                      onClick={() => !alreadySelected && handleSelectUser(user)}
                      disabled={alreadySelected}
                      className={`w-full flex items-center gap-3 p-3 text-left transition-colors ${
                        alreadySelected
                          ? 'bg-neutral-50 opacity-50 cursor-not-allowed'
                          : 'hover:bg-neutral-50'
                      }`}
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
                      {alreadySelected && (
                        <span className="text-xs text-neutral-400">Already assigned</span>
                      )}
                    </button>
                  );
                })}
              </div>
            )}

            {searchQuery.trim().length >= 2 && !isSearching && searchResults.length === 0 && (
              <p className="text-sm text-neutral-500 text-center py-4">
                No users found matching &ldquo;{searchQuery}&rdquo;
              </p>
            )}
          </div>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={handleClose} disabled={isLinking}>
            Cancel
          </Button>
          <Button
            onClick={handleBatchLink}
            disabled={selectedMappings.length === 0 || isLinking}
            loading={isLinking}
            className="bg-[#8B1538] hover:bg-[#6d1029] text-white"
          >
            Link {selectedMappings.length} Co-Organizer{selectedMappings.length !== 1 ? 's' : ''}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
