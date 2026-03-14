'use client';

/**
 * PhotoAlbumManagementTab Component
 *
 * Multi-album management tab on the Event Manage page.
 * Shows sub-tabs for each album + "Create Album" CTA.
 *
 * States handled:
 * 1. Loading — spinner while album data loads
 * 2. No albums — empty state with "Create Album" CTA
 * 3. Has albums — sub-tabs per album with gallery, actions, and upload
 */

import { useState } from 'react';
import {
  Camera,
  Image,
  Loader2,
  AlertCircle,
  CheckCircle2,
  Plus,
  Send,
  Download,
  Trash2,
  Pencil,
} from 'lucide-react';
import { Card, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import {
  useEventAlbums,
  useCreateAlbum,
  useUpdateAlbumDetails,
  usePublishAlbum,
  useSendAlbumNotification,
  useDeleteAlbum,
  useDownloadAlbumZip,
} from '@/presentation/hooks/usePhotoAlbum';
import { AlbumGallery } from './AlbumGallery';
import { AlbumStatus } from '@/infrastructure/api/types/events.types';
import type { PhotoAlbumDto } from '@/infrastructure/api/types/events.types';

export interface PhotoAlbumManagementTabProps {
  eventId: string;
}

function getStatusBadge(status: AlbumStatus): { label: string; className: string } {
  switch (status) {
    case AlbumStatus.Draft:
      return { label: 'Draft', className: 'bg-yellow-100 text-yellow-800' };
    case AlbumStatus.Published:
      return { label: 'Published', className: 'bg-green-100 text-green-800' };
    default:
      return { label: String(status), className: 'bg-gray-100 text-gray-800' };
  }
}

export function PhotoAlbumManagementTab({ eventId }: PhotoAlbumManagementTabProps) {
  const { data: albums, isLoading, error } = useEventAlbums(eventId);
  const createAlbum = useCreateAlbum();
  const updateAlbumDetails = useUpdateAlbumDetails();
  const publishAlbum = usePublishAlbum();
  const sendNotification = useSendAlbumNotification();
  const deleteAlbum = useDeleteAlbum();
  const downloadZip = useDownloadAlbumZip();

  const [activeAlbumId, setActiveAlbumId] = useState<string | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [newAlbumName, setNewAlbumName] = useState('');
  const [newAlbumDescription, setNewAlbumDescription] = useState('');
  const [inlineMessage, setInlineMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  // Inline edit state
  const [isEditing, setIsEditing] = useState(false);
  const [editName, setEditName] = useState('');
  const [editDescription, setEditDescription] = useState('');

  const showMessage = (type: 'success' | 'error', text: string) => {
    setInlineMessage({ type, text });
    setTimeout(() => setInlineMessage(null), 5000);
  };

  // Set active album to first album if not set
  const activeAlbum = albums?.find((a) => a.id === activeAlbumId) ?? albums?.[0] ?? null;

  const handleCreateAlbum = async () => {
    if (!newAlbumName.trim()) return;
    try {
      const album = await createAlbum.mutateAsync({
        eventId,
        request: {
          name: newAlbumName.trim(),
          description: newAlbumDescription.trim() || undefined,
        },
      });
      setActiveAlbumId(album.id);
      setShowCreateForm(false);
      setNewAlbumName('');
      setNewAlbumDescription('');
      showMessage('success', `Album "${album.name}" created!`);
    } catch {
      showMessage('error', 'Failed to create album. Please try again.');
    }
  };

  const handlePublish = async (album: PhotoAlbumDto) => {
    try {
      await publishAlbum.mutateAsync({ eventId, albumId: album.id });
      showMessage('success', `Album "${album.name}" published!`);
    } catch {
      showMessage('error', 'Failed to publish album. Please try again.');
    }
  };

  const handleSendNotification = async (album: PhotoAlbumDto) => {
    try {
      await sendNotification.mutateAsync({ eventId, albumId: album.id });
      showMessage('success', `Notification sent for "${album.name}"!`);
    } catch {
      showMessage('error', 'Failed to send notification. Please try again.');
    }
  };

  const handleDeleteAlbum = async (album: PhotoAlbumDto) => {
    if (!confirm(`Delete album "${album.name}"? This will remove all photos.`)) return;
    try {
      await deleteAlbum.mutateAsync({ eventId, albumId: album.id });
      setActiveAlbumId(null);
      showMessage('success', `Album "${album.name}" deleted.`);
    } catch {
      showMessage('error', 'Failed to delete album. Please try again.');
    }
  };

  const handleDownloadZip = async (album: PhotoAlbumDto) => {
    try {
      await downloadZip.mutateAsync({ eventId, albumId: album.id, albumName: album.name });
    } catch {
      showMessage('error', 'Failed to download ZIP. Please try again.');
    }
  };

  const startEditing = (album: PhotoAlbumDto) => {
    setEditName(album.name);
    setEditDescription(album.description ?? '');
    setIsEditing(true);
  };

  const handleSaveEdit = async (album: PhotoAlbumDto) => {
    if (!editName.trim()) return;
    try {
      await updateAlbumDetails.mutateAsync({
        eventId,
        albumId: album.id,
        request: {
          name: editName.trim(),
          description: editDescription.trim() || undefined,
        },
      });
      setIsEditing(false);
      showMessage('success', `Album "${editName.trim()}" updated!`);
    } catch {
      showMessage('error', 'Failed to update album. Please try again.');
    }
  };

  // Loading state
  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-16">
        <Loader2 className="h-8 w-8 animate-spin text-gray-400 mb-3" />
        <p className="text-sm text-gray-500">Loading photo albums...</p>
      </div>
    );
  }

  // Error state
  if (error && !albums) {
    return (
      <div className="flex flex-col items-center justify-center py-16">
        <AlertCircle className="h-10 w-10 text-red-300 mb-3" />
        <p className="text-sm text-red-600">Failed to load albums. Please refresh the page.</p>
      </div>
    );
  }

  // Inline message banner
  const messageBanner = inlineMessage && (
    <div
      className={`flex items-center gap-2 p-3 rounded-lg mb-4 ${
        inlineMessage.type === 'success'
          ? 'bg-green-50 border border-green-200 text-green-700'
          : 'bg-red-50 border border-red-200 text-red-700'
      }`}
    >
      {inlineMessage.type === 'success' ? (
        <CheckCircle2 className="h-4 w-4 flex-shrink-0" />
      ) : (
        <AlertCircle className="h-4 w-4 flex-shrink-0" />
      )}
      <p className="text-sm">{inlineMessage.text}</p>
    </div>
  );

  // No albums yet — show create CTA
  if (!albums || albums.length === 0) {
    return (
      <div className="space-y-4">
        {messageBanner}

        {/* Create Album Form */}
        {showCreateForm ? (
          <Card>
            <CardContent className="pt-6 space-y-4">
              <h3 className="text-lg font-semibold text-gray-900">Create Photo Album</h3>
              <div>
                <label htmlFor="album-name" className="block text-sm font-medium text-gray-700 mb-1">
                  Album Name *
                </label>
                <input
                  id="album-name"
                  type="text"
                  value={newAlbumName}
                  onChange={(e) => setNewAlbumName(e.target.value)}
                  placeholder="e.g., Diwali Night Photos"
                  maxLength={100}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent"
                />
              </div>
              <div>
                <label htmlFor="album-desc" className="block text-sm font-medium text-gray-700 mb-1">
                  Description (optional)
                </label>
                <textarea
                  id="album-desc"
                  value={newAlbumDescription}
                  onChange={(e) => setNewAlbumDescription(e.target.value)}
                  placeholder="Brief description of the album"
                  rows={2}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent"
                />
              </div>
              <div className="flex gap-2">
                <Button
                  onClick={handleCreateAlbum}
                  disabled={!newAlbumName.trim() || createAlbum.isPending}
                  className="text-white"
                  style={{ background: '#8B1538' }}
                >
                  {createAlbum.isPending ? (
                    <>
                      <Loader2 className="h-4 w-4 animate-spin mr-2" />
                      Creating...
                    </>
                  ) : (
                    'Create Album'
                  )}
                </Button>
                <Button variant="outline" onClick={() => setShowCreateForm(false)}>
                  Cancel
                </Button>
              </div>
            </CardContent>
          </Card>
        ) : (
          <div className="text-center py-12">
            <Camera className="h-16 w-16 text-purple-300 mx-auto mb-4" />
            <h3 className="text-xl font-semibold text-gray-900 mb-2">No Photo Albums Yet</h3>
            <p className="text-gray-600 mb-6 max-w-md mx-auto">
              Create photo albums to share photos from your event.
              Photos are automatically deleted after 7 days.
            </p>
            <Button
              onClick={() => setShowCreateForm(true)}
              className="text-white"
              style={{ background: '#8B1538' }}
            >
              <Plus className="h-4 w-4 mr-2" />
              Create Photo Album
            </Button>
          </div>
        )}
      </div>
    );
  }

  // Has albums — render sub-tabs + active album content
  const statusBadge = activeAlbum ? getStatusBadge(activeAlbum.status) : null;

  return (
    <div className="space-y-6">
      {messageBanner}

      {/* Album Sub-Tabs */}
      <div className="flex items-center gap-2 overflow-x-auto pb-2 border-b">
        {albums.map((album) => (
          <button
            key={album.id}
            type="button"
            onClick={() => setActiveAlbumId(album.id)}
            className={`px-4 py-2 text-sm font-medium rounded-t-lg whitespace-nowrap transition-colors ${
              activeAlbum?.id === album.id
                ? 'bg-white border border-b-0 border-gray-200 text-gray-900'
                : 'text-gray-500 hover:text-gray-700 hover:bg-gray-50'
            }`}
          >
            {album.name}
            <span className="ml-2 text-xs text-gray-400">({album.photoCount})</span>
          </button>
        ))}
        <button
          type="button"
          onClick={() => setShowCreateForm(true)}
          className="px-3 py-2 text-sm font-medium text-purple-600 hover:bg-purple-50 rounded-lg whitespace-nowrap"
        >
          <Plus className="h-4 w-4 inline mr-1" />
          New Album
        </button>
      </div>

      {/* Create Form (inline) */}
      {showCreateForm && (
        <Card>
          <CardContent className="pt-6 space-y-4">
            <h3 className="text-lg font-semibold text-gray-900">Create Photo Album</h3>
            <div>
              <label htmlFor="album-name" className="block text-sm font-medium text-gray-700 mb-1">
                Album Name *
              </label>
              <input
                id="album-name"
                type="text"
                value={newAlbumName}
                onChange={(e) => setNewAlbumName(e.target.value)}
                placeholder="e.g., After Party Photos"
                maxLength={100}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent"
              />
            </div>
            <div>
              <label htmlFor="album-desc" className="block text-sm font-medium text-gray-700 mb-1">
                Description (optional)
              </label>
              <textarea
                id="album-desc"
                value={newAlbumDescription}
                onChange={(e) => setNewAlbumDescription(e.target.value)}
                rows={2}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent"
              />
            </div>
            <div className="flex gap-2">
              <Button
                onClick={handleCreateAlbum}
                disabled={!newAlbumName.trim() || createAlbum.isPending}
                className="text-white"
                style={{ background: '#8B1538' }}
              >
                {createAlbum.isPending ? (
                  <>
                    <Loader2 className="h-4 w-4 animate-spin mr-2" />
                    Creating...
                  </>
                ) : (
                  'Create Album'
                )}
              </Button>
              <Button variant="outline" onClick={() => setShowCreateForm(false)}>
                Cancel
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Active Album Content */}
      {activeAlbum && (
        <>
          {/* Album Stats & Actions Card */}
          <Card>
            <CardContent className="pt-6 space-y-4">
              {/* Inline Edit Form */}
              {isEditing ? (
                <div className="space-y-3">
                  <div>
                    <label htmlFor="edit-name" className="block text-sm font-medium text-gray-700 mb-1">
                      Album Name *
                    </label>
                    <input
                      id="edit-name"
                      type="text"
                      value={editName}
                      onChange={(e) => setEditName(e.target.value)}
                      maxLength={100}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent"
                    />
                  </div>
                  <div>
                    <label htmlFor="edit-desc" className="block text-sm font-medium text-gray-700 mb-1">
                      Description (optional)
                    </label>
                    <textarea
                      id="edit-desc"
                      value={editDescription}
                      onChange={(e) => setEditDescription(e.target.value)}
                      rows={2}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent"
                    />
                  </div>
                  <div className="flex gap-2">
                    <Button
                      onClick={() => handleSaveEdit(activeAlbum)}
                      disabled={!editName.trim() || updateAlbumDetails.isPending}
                      className="text-white"
                      style={{ background: '#8B1538' }}
                      size="sm"
                    >
                      {updateAlbumDetails.isPending ? (
                        <>
                          <Loader2 className="h-3 w-3 animate-spin mr-1" />
                          Saving...
                        </>
                      ) : (
                        'Save'
                      )}
                    </Button>
                    <Button variant="outline" size="sm" onClick={() => setIsEditing(false)}>
                      Cancel
                    </Button>
                  </div>
                </div>
              ) : (
                <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                  {/* Left: Status + Stats + Edit */}
                  <div className="flex items-center gap-4 flex-wrap">
                    {statusBadge && (
                      <Badge className={`text-sm px-3 py-1 font-semibold ${statusBadge.className}`}>
                        {statusBadge.label}
                      </Badge>
                    )}
                    <div className="flex items-center gap-4 text-sm text-gray-600">
                      <span className="flex items-center gap-1">
                        <Image className="h-4 w-4" />
                        {activeAlbum.photoCount} {activeAlbum.photoCount === 1 ? 'photo' : 'photos'}
                      </span>
                      <span>{activeAlbum.retentionDays}-day retention</span>
                    </div>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => startEditing(activeAlbum)}
                      className="text-gray-500 hover:text-gray-700"
                    >
                      <Pencil className="h-3 w-3 mr-1" />
                      Edit
                    </Button>
                  </div>

                  {/* Right: Actions */}
                  <div className="flex items-center gap-2 flex-wrap">
                    {/* Publish (Draft + has photos) */}
                    {activeAlbum.status === AlbumStatus.Draft && activeAlbum.photoCount > 0 && (
                      <Button
                        onClick={() => handlePublish(activeAlbum)}
                        disabled={publishAlbum.isPending}
                        className="text-white"
                        style={{ background: '#8B1538' }}
                        size="sm"
                      >
                        {publishAlbum.isPending ? (
                          <>
                            <Loader2 className="h-3 w-3 animate-spin mr-1" />
                            Publishing...
                          </>
                        ) : (
                          'Publish'
                        )}
                      </Button>
                    )}

                    {/* Send Email (Published only) */}
                    {activeAlbum.status === AlbumStatus.Published && (
                      <Button
                        onClick={() => handleSendNotification(activeAlbum)}
                        disabled={sendNotification.isPending}
                        variant="outline"
                        size="sm"
                      >
                        {sendNotification.isPending ? (
                          <>
                            <Loader2 className="h-3 w-3 animate-spin mr-1" />
                            Sending...
                          </>
                        ) : (
                          <>
                            <Send className="h-3 w-3 mr-1" />
                            Send Email
                          </>
                        )}
                      </Button>
                    )}

                    {/* Download ZIP (has photos) */}
                    {activeAlbum.photoCount > 0 && (
                      <Button
                        onClick={() => handleDownloadZip(activeAlbum)}
                        disabled={downloadZip.isPending}
                        variant="outline"
                        size="sm"
                      >
                        {downloadZip.isPending ? (
                          <Loader2 className="h-3 w-3 animate-spin" />
                        ) : (
                          <>
                            <Download className="h-3 w-3 mr-1" />
                            ZIP
                          </>
                        )}
                      </Button>
                    )}

                    {/* Delete (Draft only) */}
                    {activeAlbum.status === AlbumStatus.Draft && (
                      <Button
                        onClick={() => handleDeleteAlbum(activeAlbum)}
                        disabled={deleteAlbum.isPending}
                        variant="outline"
                        className="text-red-600 border-red-300 hover:bg-red-50"
                        size="sm"
                      >
                        {deleteAlbum.isPending ? (
                          <Loader2 className="h-3 w-3 animate-spin" />
                        ) : (
                          <>
                            <Trash2 className="h-3 w-3 mr-1" />
                            Delete
                          </>
                        )}
                      </Button>
                    )}
                  </div>
                </div>
              )}

              {/* Album description (display mode) */}
              {!isEditing && activeAlbum.description && (
                <p className="text-sm text-gray-500">{activeAlbum.description}</p>
              )}
            </CardContent>
          </Card>

          {/* Gallery with upload — always visible */}
          <AlbumGallery
            eventId={eventId}
            album={activeAlbum}
            isOrganizer={true}
            canUpload={true}
          />
        </>
      )}
    </div>
  );
}
