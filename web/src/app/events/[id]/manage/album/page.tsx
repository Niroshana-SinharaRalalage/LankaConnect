'use client';

/**
 * Album Management Page
 *
 * Organizer-only page at /events/{id}/manage/album for creating and managing
 * the event photo album. Provides settings, moderation queue, and album lifecycle controls.
 */

import { use, useState } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, Camera, Settings, Shield, Image, Loader2 } from 'lucide-react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import {
  usePhotoAlbum,
  useCreateAlbum,
  usePublishAlbum,
  useCloseAlbum,
} from '@/presentation/hooks/usePhotoAlbum';
import { AlbumSettingsForm } from '@/presentation/components/features/events/AlbumSettingsForm';
import { AlbumModerationQueue } from '@/presentation/components/features/events/AlbumModerationQueue';
import {
  AlbumStatus,
  AlbumModerationMode,
} from '@/infrastructure/api/types/events.types';

function getStatusBadge(status: AlbumStatus): { label: string; color: string } {
  switch (status) {
    case AlbumStatus.Draft:
      return { label: 'Draft', color: '#F59E0B' };
    case AlbumStatus.Published:
      return { label: 'Published', color: '#10B981' };
    case AlbumStatus.Closed:
      return { label: 'Closed', color: '#6B7280' };
    default:
      return { label: status, color: '#6B7280' };
  }
}

export default function AlbumManagePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const router = useRouter();
  const { user } = useAuthStore();

  const { data: album, isLoading, error } = usePhotoAlbum(id);
  const createAlbum = useCreateAlbum();
  const publishAlbum = usePublishAlbum();
  const closeAlbum = useCloseAlbum();

  const [activeSection, setActiveSection] = useState<'settings' | 'moderation'>('settings');

  const handleCreateAlbum = () => {
    createAlbum.mutate({ eventId: id });
  };

  const handlePublish = () => {
    publishAlbum.mutate({ eventId: id });
  };

  const handleClose = () => {
    closeAlbum.mutate({ eventId: id });
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
        <Header />
        <div className="flex items-center justify-center py-24">
          <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
        </div>
        <Footer />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      {/* Header */}
      <div className="bg-gradient-to-r from-purple-700 via-purple-800 to-indigo-900 py-8">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-white mb-1">Photo Album</h1>
              <p className="text-white/80 text-sm">Manage your event photo album</p>
            </div>
            {album && (
              <Badge
                className="text-sm px-3 py-1 font-bold"
                style={{ backgroundColor: getStatusBadge(album.status).color, color: '#fff' }}
              >
                {getStatusBadge(album.status).label}
              </Badge>
            )}
          </div>
        </div>
      </div>

      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        {/* Back button */}
        <Button
          variant="outline"
          onClick={() => router.push(`/events/${id}/manage?tab=album`)}
          className="flex items-center gap-2 mb-6"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to Event Management
        </Button>

        {/* No album yet — show create button */}
        {!album && !error && (
          <Card>
            <CardContent className="text-center py-12">
              <Camera className="h-16 w-16 text-purple-300 mx-auto mb-4" />
              <h2 className="text-xl font-semibold text-gray-900 mb-2">No Photo Album Yet</h2>
              <p className="text-gray-600 mb-6 max-w-md mx-auto">
                Create a photo album to let attendees share and view photos from your event.
                Photos are automatically deleted after 7 days.
              </p>
              <Button
                onClick={handleCreateAlbum}
                disabled={createAlbum.isPending}
                className="text-white"
                style={{ background: '#8B1538' }}
              >
                {createAlbum.isPending ? 'Creating...' : 'Create Photo Album'}
              </Button>
              {createAlbum.isError && (
                <p className="text-red-600 text-sm mt-3">
                  Failed to create album. Please try again.
                </p>
              )}
            </CardContent>
          </Card>
        )}

        {/* Album exists — show management UI */}
        {album && (
          <div className="space-y-6">
            {/* Stats & Actions */}
            <Card>
              <CardContent className="pt-6">
                <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                  <div className="flex items-center gap-6">
                    <div className="text-center">
                      <p className="text-2xl font-bold text-gray-900">{album.photoCount}</p>
                      <p className="text-xs text-gray-500">Photos</p>
                    </div>
                    <div className="text-center">
                      <p className="text-2xl font-bold text-gray-900">{album.retentionDays}</p>
                      <p className="text-xs text-gray-500">Day retention</p>
                    </div>
                    {album.coverPhotoUrl && (
                      <div className="hidden sm:block">
                        <img
                          src={album.coverPhotoUrl}
                          alt="Cover"
                          className="h-16 w-16 object-cover rounded-lg border"
                        />
                      </div>
                    )}
                  </div>

                  <div className="flex items-center gap-2 flex-wrap">
                    {/* View public gallery */}
                    <Button
                      variant="outline"
                      onClick={() => router.push(`/events/${id}/photos`)}
                      className="flex items-center gap-2"
                    >
                      <Image className="h-4 w-4" />
                      View Gallery
                    </Button>

                    {/* Publish */}
                    {album.status === AlbumStatus.Draft && (
                      <Button
                        onClick={handlePublish}
                        disabled={publishAlbum.isPending}
                        className="text-white"
                        style={{ background: '#10B981' }}
                      >
                        {publishAlbum.isPending ? 'Publishing...' : 'Publish Album'}
                      </Button>
                    )}

                    {/* Close */}
                    {album.status === AlbumStatus.Published && (
                      <Button
                        onClick={handleClose}
                        disabled={closeAlbum.isPending}
                        variant="outline"
                        className="text-red-600 border-red-300 hover:bg-red-50"
                      >
                        {closeAlbum.isPending ? 'Closing...' : 'Close Album'}
                      </Button>
                    )}
                  </div>
                </div>

                {publishAlbum.isError && (
                  <p className="text-red-600 text-sm mt-3">Failed to publish album.</p>
                )}
                {closeAlbum.isError && (
                  <p className="text-red-600 text-sm mt-3">Failed to close album.</p>
                )}
              </CardContent>
            </Card>

            {/* Section toggle */}
            <div className="flex gap-2">
              <Button
                variant={activeSection === 'settings' ? 'default' : 'outline'}
                onClick={() => setActiveSection('settings')}
                className="flex items-center gap-2"
              >
                <Settings className="h-4 w-4" />
                Settings
              </Button>
              <Button
                variant={activeSection === 'moderation' ? 'default' : 'outline'}
                onClick={() => setActiveSection('moderation')}
                className="flex items-center gap-2"
              >
                <Shield className="h-4 w-4" />
                Moderation
              </Button>
            </div>

            {/* Settings section */}
            {activeSection === 'settings' && (
              <AlbumSettingsForm eventId={id} album={album} />
            )}

            {/* Moderation section */}
            {activeSection === 'moderation' && (
              <AlbumModerationQueue eventId={id} />
            )}
          </div>
        )}
      </div>

      <Footer />
    </div>
  );
}
