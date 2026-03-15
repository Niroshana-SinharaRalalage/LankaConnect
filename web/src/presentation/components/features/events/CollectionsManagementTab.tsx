'use client';

import { useState } from 'react';
import {
  Wallet,
  Download,
  DollarSign,
  Users,
  TrendingUp,
  Mail,
  Phone,
  Calendar,
  RefreshCw,
  Settings,
  CheckCircle,
  XCircle,
  Target,
} from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { useEventCollections } from '@/presentation/hooks/useCollections';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type { CollectionDto, CollectionConfigurationDto } from '@/infrastructure/api/types/events.types';

interface CollectionsManagementTabProps {
  eventId: string;
  collectionConfig?: CollectionConfigurationDto | null;
}

function getCollectionStatusColor(status: string): string {
  switch (status) {
    case 'Completed':
      return '#10B981';
    case 'Pending':
      return '#F59E0B';
    case 'Failed':
      return '#EF4444';
    case 'Refunded':
      return '#6366F1';
    case 'Abandoned':
      return '#6B7280';
    default:
      return '#6B7280';
  }
}

/**
 * Collections Management Tab for event organizers.
 * Shows collection (fundraising) summary, goal progress, collections table, and export button.
 * Follows DonationsManagementTab pattern.
 */
export function CollectionsManagementTab({ eventId, collectionConfig }: CollectionsManagementTabProps) {
  const [isExporting, setIsExporting] = useState(false);

  const { data: collectionsData, isLoading, error, refetch } = useEventCollections(eventId);

  const handleExport = async (format: 'excel' | 'csv') => {
    try {
      setIsExporting(true);
      const blob = await eventsRepository.exportCollections(eventId, format);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `collections-${eventId}.${format === 'excel' ? 'xlsx' : 'csv'}`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err) {
      console.error('Failed to export collections:', err);
    } finally {
      setIsExporting(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <RefreshCw className="h-6 w-6 animate-spin text-neutral-400" />
        <span className="ml-2 text-neutral-500">Loading collections...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-sm text-red-600">
        Failed to load collections. Please try again.
      </div>
    );
  }

  const summary = collectionsData?.summary;
  const collections = collectionsData?.collections || [];

  return (
    <div className="space-y-6">
      {/* Collection Configuration */}
      {collectionConfig && (
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-base">
              <Settings className="h-4 w-4 text-neutral-500" />
              Collection Settings
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 text-sm">
              <div className="flex items-center gap-2">
                {collectionConfig.isEnabled ? (
                  <CheckCircle className="h-4 w-4 text-emerald-500" />
                ) : (
                  <XCircle className="h-4 w-4 text-neutral-400" />
                )}
                <span className="text-neutral-700">
                  {collectionConfig.isEnabled ? 'Collections enabled' : 'Collections disabled'}
                </span>
              </div>
              {collectionConfig.isEnabled && (
                <>
                  {collectionConfig.goalAmount != null && collectionConfig.goalAmount > 0 && (
                    <div>
                      <span className="text-neutral-500">Goal amount:</span>{' '}
                      <span className="font-medium text-neutral-700">
                        ${collectionConfig.goalAmount.toFixed(2)}
                      </span>
                    </div>
                  )}
                  <div className="flex items-center gap-2">
                    {collectionConfig.showProgress ? (
                      <CheckCircle className="h-4 w-4 text-emerald-500" />
                    ) : (
                      <XCircle className="h-4 w-4 text-neutral-400" />
                    )}
                    <span className="text-neutral-700">Show progress</span>
                  </div>
                  <div className="flex items-center gap-2">
                    {collectionConfig.showContributorCount ? (
                      <CheckCircle className="h-4 w-4 text-emerald-500" />
                    ) : (
                      <XCircle className="h-4 w-4 text-neutral-400" />
                    )}
                    <span className="text-neutral-700">Show contributor count</span>
                  </div>
                  {collectionConfig.suggestedAmounts.length > 0 && (
                    <div>
                      <span className="text-neutral-500">Suggested amounts:</span>{' '}
                      <span className="font-medium text-neutral-700">
                        {collectionConfig.suggestedAmounts.map((a) => `$${a.toFixed(2)}`).join(', ')}
                      </span>
                    </div>
                  )}
                  <div className="flex items-center gap-2">
                    {collectionConfig.allowCustomAmount ? (
                      <CheckCircle className="h-4 w-4 text-emerald-500" />
                    ) : (
                      <XCircle className="h-4 w-4 text-neutral-400" />
                    )}
                    <span className="text-neutral-700">Custom amount</span>
                  </div>
                  {(collectionConfig.minAmount || collectionConfig.maxAmount) && (
                    <div>
                      <span className="text-neutral-500">Range:</span>{' '}
                      <span className="font-medium text-neutral-700">
                        {collectionConfig.minAmount ? `$${collectionConfig.minAmount.toFixed(2)}` : 'No min'}
                        {' — '}
                        {collectionConfig.maxAmount ? `$${collectionConfig.maxAmount.toFixed(2)}` : 'No max'}
                      </span>
                    </div>
                  )}
                  {collectionConfig.collectionMessage && (
                    <div className="sm:col-span-2 lg:col-span-3">
                      <span className="text-neutral-500">Message:</span>{' '}
                      <span className="text-neutral-700 italic">&ldquo;{collectionConfig.collectionMessage}&rdquo;</span>
                    </div>
                  )}
                </>
              )}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Summary Cards */}
      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <Card>
            <CardContent className="pt-4 pb-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-violet-50">
                  <Wallet className="h-5 w-5 text-violet-500" />
                </div>
                <div>
                  <p className="text-xs text-neutral-500">Total Collections</p>
                  <p className="text-lg font-bold text-neutral-800">{summary.completedCollections}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-4 pb-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-emerald-50">
                  <DollarSign className="h-5 w-5 text-emerald-600" />
                </div>
                <div>
                  <p className="text-xs text-neutral-500">Total Amount</p>
                  <p className="text-lg font-bold text-neutral-800">${summary.totalAmount.toFixed(2)}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-4 pb-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-blue-50">
                  <TrendingUp className="h-5 w-5 text-blue-600" />
                </div>
                <div>
                  <p className="text-xs text-neutral-500">Average Collection</p>
                  <p className="text-lg font-bold text-neutral-800">${summary.averageCollection.toFixed(2)}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-4 pb-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-orange-50">
                  <Users className="h-5 w-5 text-orange-600" />
                </div>
                <div>
                  <p className="text-xs text-neutral-500">Contributors</p>
                  <p className="text-lg font-bold text-neutral-800">{summary.contributorCount}</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Goal Progress Bar */}
      {summary && summary.goalAmount != null && summary.goalAmount > 0 && (
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between mb-3">
              <div className="flex items-center gap-2">
                <Target className="h-5 w-5 text-violet-500" />
                <span className="text-sm font-medium text-neutral-700">Goal Progress</span>
              </div>
              <span className="text-sm font-semibold text-neutral-800">
                ${summary.totalAmount.toFixed(2)} / ${summary.goalAmount.toFixed(2)}
              </span>
            </div>
            <div className="w-full bg-neutral-200 rounded-full h-3 overflow-hidden">
              <div
                className="h-3 rounded-full bg-violet-500 transition-all duration-500"
                style={{ width: `${Math.min(summary.goalProgressPercent ?? 0, 100)}%` }}
              />
            </div>
            <div className="flex items-center justify-between mt-2">
              <span className="text-xs text-neutral-500">
                {(summary.goalProgressPercent ?? 0).toFixed(1)}% reached
              </span>
              {(summary.goalProgressPercent ?? 0) >= 100 && (
                <span className="text-xs font-medium text-emerald-600 flex items-center gap-1">
                  <CheckCircle className="h-3 w-3" />
                  Goal reached!
                </span>
              )}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Payout Breakdown Card */}
      {summary && summary.completedCollections > 0 && summary.totalAmount > 0 && (
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-neutral-600">Your Payout</p>
                <p className="text-3xl font-bold text-green-700">
                  ${summary.totalOrganizerPayout.toFixed(2)}
                </p>
                <p className="text-xs text-neutral-500 mt-1">
                  After Stripe fees &amp; platform commission
                </p>
              </div>
              <DollarSign className="h-10 w-10 text-orange-600" />
            </div>

            <div className="mt-3 pt-3 border-t border-neutral-200">
              <div className="flex justify-between text-xs text-neutral-600 mb-1">
                <span>Gross Revenue:</span>
                <span className="font-medium">${summary.totalAmount.toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-xs text-red-600 mb-1">
                <span>Stripe Fees (2.9% + $0.30):</span>
                <span className="font-medium">-${summary.totalStripeFees.toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-xs text-red-600 mb-1">
                <span>Platform Commission (2%):</span>
                <span className="font-medium">-${summary.totalPlatformCommission.toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-xs text-green-700 font-semibold pt-1 border-t border-neutral-200">
                <span>Your Payout:</span>
                <span>${summary.totalOrganizerPayout.toFixed(2)}</span>
              </div>
              <p className="text-[10px] text-neutral-400 mt-2">
                * Stripe &amp; LankaConnect fees shown separately.
              </p>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Actions Row */}
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-neutral-700">
          All Collections ({collections.length})
        </h3>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            disabled={isLoading}
          >
            <RefreshCw className={`h-4 w-4 mr-1 ${isLoading ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => handleExport('excel')}
            disabled={isExporting || collections.length === 0}
          >
            <Download className="h-4 w-4 mr-1" />
            {isExporting ? 'Exporting...' : 'Export Excel'}
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => handleExport('csv')}
            disabled={isExporting || collections.length === 0}
          >
            <Download className="h-4 w-4 mr-1" />
            CSV
          </Button>
        </div>
      </div>

      {/* Collections Table */}
      {collections.length === 0 ? (
        <div className="text-center py-12 text-neutral-500">
          <Wallet className="h-10 w-10 mx-auto mb-3 text-neutral-300" />
          <p className="text-sm">No collections yet.</p>
          <p className="text-xs text-neutral-400 mt-1">Collections will appear here once someone contributes.</p>
        </div>
      ) : (
        <div className="overflow-x-auto rounded-lg border border-neutral-200">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-neutral-50 border-b border-neutral-200">
                <th className="text-left px-4 py-3 font-medium text-neutral-600">Contributor</th>
                <th className="text-left px-4 py-3 font-medium text-neutral-600">Contact</th>
                <th className="text-right px-4 py-3 font-medium text-neutral-600">Amount</th>
                <th className="text-center px-4 py-3 font-medium text-neutral-600">Status</th>
                <th className="text-left px-4 py-3 font-medium text-neutral-600">Date</th>
                <th className="text-left px-4 py-3 font-medium text-neutral-600">Notes</th>
              </tr>
            </thead>
            <tbody>
              {collections.map((collection: CollectionDto) => (
                <tr key={collection.id} className="border-b border-neutral-100 hover:bg-neutral-50">
                  <td className="px-4 py-3">
                    <span className="font-medium text-neutral-800">{collection.contributorName}</span>
                  </td>
                  <td className="px-4 py-3">
                    <div className="space-y-0.5">
                      <div className="flex items-center gap-1 text-xs text-neutral-600">
                        <Mail className="h-3 w-3" />
                        <span>{collection.contributorEmail}</span>
                      </div>
                      {collection.contributorPhone && (
                        <div className="flex items-center gap-1 text-xs text-neutral-500">
                          <Phone className="h-3 w-3" />
                          <span>{collection.contributorPhone}</span>
                        </div>
                      )}
                    </div>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <span className="font-semibold text-neutral-800">
                      ${collection.amount.toFixed(2)}
                    </span>
                    {collection.currency !== 'USD' && (
                      <span className="text-xs text-neutral-500 ml-1">{collection.currency}</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-center">
                    <Badge style={{ backgroundColor: getCollectionStatusColor(collection.status) }}>
                      {collection.status}
                    </Badge>
                  </td>
                  <td className="px-4 py-3 text-neutral-600">
                    <div className="flex items-center gap-1 text-xs">
                      <Calendar className="h-3 w-3" />
                      <span>
                        {collection.paymentCompletedAt
                          ? new Date(collection.paymentCompletedAt).toLocaleDateString()
                          : new Date(collection.createdAt).toLocaleDateString()}
                      </span>
                    </div>
                  </td>
                  <td className="px-4 py-3 text-xs text-neutral-500 max-w-[200px] truncate">
                    {collection.contributorNotes || '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
