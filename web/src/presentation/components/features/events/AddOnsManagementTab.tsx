'use client';

import { useState } from 'react';
import {
  ShoppingBag,
  Package,
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
  ToggleLeft,
  ToggleRight,
  Plus,
  Pencil,
  X,
} from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { Input } from '@/presentation/components/ui/Input';
import { useAddOnDefinitions, useEventAddOnPurchases, useCreateAddOnDefinition, useUpdateAddOnDefinition } from '@/presentation/hooks/useAddOns';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type { AddOnDefinitionDto, AddOnPurchaseDto, AddOnConfigurationDto } from '@/infrastructure/api/types/events.types';

interface AddOnsManagementTabProps {
  eventId: string;
  addOnConfig?: AddOnConfigurationDto | null;
}

function getPurchaseStatusColor(status: string): string {
  switch (status) {
    case 'Completed':
      return '#10B981';
    case 'Pending':
      return '#F59E0B';
    case 'Failed':
      return '#EF4444';
    case 'Refunded':
      return '#6366F1';
    case 'Cancelled':
      return '#6B7280';
    default:
      return '#6B7280';
  }
}

function formatCurrency(amount: number, currency: string = 'USD'): string {
  if (currency === 'USD') {
    return `$${amount.toFixed(2)}`;
  }
  return `${amount.toFixed(2)} ${currency}`;
}

/**
 * Add-Ons Management Tab for event organizers.
 * Two sections: Add-On Definitions (CRUD) and Purchase History (read-only).
 * Follows DonationsManagementTab pattern.
 */
export function AddOnsManagementTab({ eventId, addOnConfig }: AddOnsManagementTabProps) {
  const [isExporting, setIsExporting] = useState(false);
  const [togglingId, setTogglingId] = useState<string | null>(null);

  // Add-On CRUD form state
  const [showForm, setShowForm] = useState(false);
  const [editingDefinition, setEditingDefinition] = useState<AddOnDefinitionDto | null>(null);
  const [formName, setFormName] = useState('');
  const [formDescription, setFormDescription] = useState('');
  const [formPrice, setFormPrice] = useState('');
  const [formQuantityLimit, setFormQuantityLimit] = useState('');
  const [formSortOrder, setFormSortOrder] = useState('0');
  const [formError, setFormError] = useState<string | null>(null);
  const [formSubmitting, setFormSubmitting] = useState(false);

  const isEnabled = addOnConfig?.isEnabled === true;
  const { data: definitions = [], isLoading: definitionsLoading } = useAddOnDefinitions(eventId, isEnabled);
  const { data: purchasesData, isLoading: purchasesLoading, error, refetch } = useEventAddOnPurchases(eventId, isEnabled);

  // Show enable prompt when feature is not configured
  if (!isEnabled) {
    return (
      <div className="flex flex-col items-center justify-center py-16 px-4">
        <div className="w-16 h-16 rounded-full bg-emerald-100 flex items-center justify-center mb-4">
          <ShoppingBag className="h-8 w-8 text-emerald-500" />
        </div>
        <h3 className="text-lg font-semibold text-neutral-800 mb-2">Add-Ons Not Enabled</h3>
        <p className="text-sm text-neutral-500 text-center max-w-md mb-4">
          Enable the Add-Ons feature to sell additional items (t-shirts, meals, parking passes, etc.)
          during registration or as standalone purchases.
        </p>
        <p className="text-xs text-neutral-400 text-center max-w-sm">
          To enable, edit your event and turn on Add-Ons in the financial features section,
          or use the API to configure add-on settings.
        </p>
      </div>
    );
  }
  const createDefinition = useCreateAddOnDefinition();
  const updateDefinition = useUpdateAddOnDefinition();

  const isLoading = definitionsLoading || purchasesLoading;

  const resetForm = () => {
    setFormName('');
    setFormDescription('');
    setFormPrice('');
    setFormQuantityLimit('');
    setFormSortOrder('0');
    setFormError(null);
    setEditingDefinition(null);
  };

  const handleOpenCreate = () => {
    resetForm();
    setFormSortOrder(String(definitions.length));
    setShowForm(true);
  };

  const handleOpenEdit = (def: AddOnDefinitionDto) => {
    setEditingDefinition(def);
    setFormName(def.name);
    setFormDescription(def.description || '');
    setFormPrice(String(def.price));
    setFormQuantityLimit(def.quantityLimit != null ? String(def.quantityLimit) : '');
    setFormSortOrder(String(def.sortOrder));
    setFormError(null);
    setShowForm(true);
  };

  const handleCancelForm = () => {
    setShowForm(false);
    resetForm();
  };

  const handleFormSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    const name = formName.trim();
    if (!name) {
      setFormError('Name is required.');
      return;
    }

    const price = parseFloat(formPrice);
    if (isNaN(price) || price < 0) {
      setFormError('Price must be a valid number ($0 or more).');
      return;
    }

    const quantityLimit = formQuantityLimit.trim()
      ? parseInt(formQuantityLimit, 10)
      : null;
    if (quantityLimit !== null && (isNaN(quantityLimit) || quantityLimit < 1)) {
      setFormError('Quantity limit must be at least 1.');
      return;
    }

    const sortOrder = parseInt(formSortOrder, 10) || 0;

    try {
      setFormSubmitting(true);

      if (editingDefinition) {
        await updateDefinition.mutateAsync({
          eventId,
          definitionId: editingDefinition.id,
          request: {
            name,
            description: formDescription.trim() || null,
            price,
            currency: 'USD',
            quantityLimit,
            sortOrder,
            isActive: editingDefinition.isActive,
          },
        });
      } else {
        await createDefinition.mutateAsync({
          eventId,
          request: {
            name,
            description: formDescription.trim() || null,
            price,
            currency: 'USD',
            quantityLimit,
            sortOrder,
          },
        });
      }

      setShowForm(false);
      resetForm();
    } catch (err: any) {
      console.error('Failed to save add-on definition:', err);
      setFormError(err?.response?.data?.detail || 'Failed to save add-on. Please try again.');
    } finally {
      setFormSubmitting(false);
    }
  };

  const handleToggleActive = async (definition: AddOnDefinitionDto) => {
    try {
      setTogglingId(definition.id);
      await updateDefinition.mutateAsync({
        eventId,
        definitionId: definition.id,
        request: {
          name: definition.name,
          description: definition.description,
          price: definition.price,
          currency: definition.currency,
          quantityLimit: definition.quantityLimit,
          sortOrder: definition.sortOrder,
          isActive: !definition.isActive,
        },
      });
    } catch (err) {
      console.error('Failed to toggle add-on definition status:', err);
    } finally {
      setTogglingId(null);
    }
  };

  const handleExport = async (format: 'excel' | 'csv') => {
    try {
      setIsExporting(true);
      const blob = await eventsRepository.exportAddOnPurchases(eventId, format);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `add-on-purchases-${eventId}.${format === 'excel' ? 'xlsx' : 'csv'}`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err) {
      console.error('Failed to export add-on purchases:', err);
    } finally {
      setIsExporting(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <RefreshCw className="h-6 w-6 animate-spin text-neutral-400" />
        <span className="ml-2 text-neutral-500">Loading add-ons...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-sm text-red-600">
        Failed to load add-on data. Please try again.
      </div>
    );
  }

  const summary = purchasesData?.summary;
  const purchases = purchasesData?.purchases || [];

  return (
    <div className="space-y-6">
      {/* Add-On Configuration Settings */}
      {addOnConfig && (
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-base">
              <Settings className="h-4 w-4 text-neutral-500" />
              Add-On Settings
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 text-sm">
              <div className="flex items-center gap-2">
                {addOnConfig.isEnabled ? (
                  <CheckCircle className="h-4 w-4 text-emerald-500" />
                ) : (
                  <XCircle className="h-4 w-4 text-neutral-400" />
                )}
                <span className="text-neutral-700">
                  {addOnConfig.isEnabled ? 'Add-ons enabled' : 'Add-ons disabled'}
                </span>
              </div>
              {addOnConfig.isEnabled && (
                <>
                  <div className="flex items-center gap-2">
                    {addOnConfig.availableDuringRegistration ? (
                      <CheckCircle className="h-4 w-4 text-emerald-500" />
                    ) : (
                      <XCircle className="h-4 w-4 text-neutral-400" />
                    )}
                    <span className="text-neutral-700">Available during registration</span>
                  </div>
                  <div className="flex items-center gap-2">
                    {addOnConfig.availableStandalone ? (
                      <CheckCircle className="h-4 w-4 text-emerald-500" />
                    ) : (
                      <XCircle className="h-4 w-4 text-neutral-400" />
                    )}
                    <span className="text-neutral-700">Available standalone</span>
                  </div>
                  {addOnConfig.addOnMessage && (
                    <div className="sm:col-span-2 lg:col-span-3">
                      <span className="text-neutral-500">Message:</span>{' '}
                      <span className="text-neutral-700 italic">&ldquo;{addOnConfig.addOnMessage}&rdquo;</span>
                    </div>
                  )}
                </>
              )}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Section 1: Add-On Definitions */}
      <Card>
        <CardHeader className="pb-3">
          <div className="flex items-center justify-between">
            <CardTitle className="flex items-center gap-2 text-base">
              <Package className="h-4 w-4 text-neutral-500" />
              Add-On Items ({definitions.length})
            </CardTitle>
            <Button
              size="sm"
              onClick={handleOpenCreate}
              disabled={showForm}
              style={{ background: '#7C3AED' }}
            >
              <Plus className="h-4 w-4 mr-1" />
              Create Add-On
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {/* Inline Create/Edit Form */}
          {showForm && (
            <form onSubmit={handleFormSubmit} className="mb-4 p-4 bg-neutral-50 border border-neutral-200 rounded-lg space-y-3">
              <h4 className="text-sm font-semibold text-neutral-700">
                {editingDefinition ? 'Edit Add-On' : 'New Add-On'}
              </h4>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label htmlFor="addOnName" className="block text-xs font-medium text-neutral-600 mb-1">
                    Name *
                  </label>
                  <Input
                    id="addOnName"
                    value={formName}
                    onChange={(e) => setFormName(e.target.value)}
                    placeholder="e.g., Event T-Shirt, Lunch Package"
                    required
                  />
                </div>
                <div>
                  <label htmlFor="addOnPrice" className="block text-xs font-medium text-neutral-600 mb-1">
                    Price (USD) *
                  </label>
                  <div className="relative">
                    <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500">$</span>
                    <Input
                      id="addOnPrice"
                      type="number"
                      min="0"
                      step="0.01"
                      value={formPrice}
                      onChange={(e) => setFormPrice(e.target.value)}
                      placeholder="0.00"
                      className="pl-7"
                      required
                    />
                  </div>
                </div>
              </div>
              <div>
                <label htmlFor="addOnDescription" className="block text-xs font-medium text-neutral-600 mb-1">
                  Description (optional)
                </label>
                <textarea
                  id="addOnDescription"
                  value={formDescription}
                  onChange={(e) => setFormDescription(e.target.value)}
                  placeholder="Brief description of this add-on..."
                  className="w-full rounded-md border border-neutral-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet-500"
                  rows={2}
                  maxLength={500}
                />
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label htmlFor="addOnQuantityLimit" className="block text-xs font-medium text-neutral-600 mb-1">
                    Quantity Limit (optional)
                  </label>
                  <Input
                    id="addOnQuantityLimit"
                    type="number"
                    min="1"
                    value={formQuantityLimit}
                    onChange={(e) => setFormQuantityLimit(e.target.value)}
                    placeholder="Unlimited"
                  />
                  <p className="text-[10px] text-neutral-400 mt-0.5">Leave empty for unlimited stock</p>
                </div>
                <div>
                  <label htmlFor="addOnSortOrder" className="block text-xs font-medium text-neutral-600 mb-1">
                    Sort Order
                  </label>
                  <Input
                    id="addOnSortOrder"
                    type="number"
                    min="0"
                    value={formSortOrder}
                    onChange={(e) => setFormSortOrder(e.target.value)}
                  />
                  <p className="text-[10px] text-neutral-400 mt-0.5">Lower numbers appear first</p>
                </div>
              </div>

              {formError && (
                <div className="p-2 bg-red-50 border border-red-200 rounded text-sm text-red-600">
                  {formError}
                </div>
              )}

              <div className="flex items-center gap-2 pt-1">
                <Button
                  type="submit"
                  size="sm"
                  disabled={formSubmitting}
                  style={{ background: '#7C3AED' }}
                >
                  {formSubmitting
                    ? 'Saving...'
                    : editingDefinition
                      ? 'Update Add-On'
                      : 'Create Add-On'}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={handleCancelForm}
                  disabled={formSubmitting}
                >
                  Cancel
                </Button>
              </div>
            </form>
          )}

          {definitions.length === 0 && !showForm ? (
            <div className="text-center py-10 text-neutral-500">
              <Package className="h-10 w-10 mx-auto mb-3 text-neutral-300" />
              <p className="text-sm">No add-on items defined yet.</p>
              <p className="text-xs text-neutral-400 mt-1">
                Click &ldquo;Create Add-On&rdquo; above to add items like meals, t-shirts, or gift bags.
              </p>
            </div>
          ) : definitions.length > 0 ? (
            <div className="overflow-x-auto rounded-lg border border-neutral-200">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-neutral-50 border-b border-neutral-200">
                    <th className="text-left px-4 py-3 font-medium text-neutral-600">Name</th>
                    <th className="text-right px-4 py-3 font-medium text-neutral-600">Price</th>
                    <th className="text-center px-4 py-3 font-medium text-neutral-600">Stock</th>
                    <th className="text-center px-4 py-3 font-medium text-neutral-600">Status</th>
                    <th className="text-center px-4 py-3 font-medium text-neutral-600">Sort Order</th>
                    <th className="text-center px-4 py-3 font-medium text-neutral-600">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {definitions.map((def: AddOnDefinitionDto) => (
                    <tr key={def.id} className="border-b border-neutral-100 hover:bg-neutral-50">
                      <td className="px-4 py-3">
                        <div>
                          <span className="font-medium text-neutral-800">{def.name}</span>
                          {def.description && (
                            <p className="text-xs text-neutral-500 mt-0.5 max-w-[250px] truncate">
                              {def.description}
                            </p>
                          )}
                        </div>
                      </td>
                      <td className="px-4 py-3 text-right">
                        <span className="font-semibold text-neutral-800">
                          {formatCurrency(def.price, def.currency)}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-center">
                        {def.quantityLimit != null ? (
                          <span className="text-neutral-700">
                            {def.quantitySold}/{def.quantityLimit}
                            {def.remainingStock != null && def.remainingStock <= 5 && def.remainingStock > 0 && (
                              <span className="text-xs text-amber-600 ml-1">
                                ({def.remainingStock} left)
                              </span>
                            )}
                            {def.remainingStock === 0 && (
                              <span className="text-xs text-red-500 ml-1">(Sold out)</span>
                            )}
                          </span>
                        ) : (
                          <span className="text-neutral-500 text-xs">Unlimited</span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-center">
                        <Badge
                          style={{
                            backgroundColor: def.isActive ? '#10B981' : '#6B7280',
                          }}
                        >
                          {def.isActive ? 'Active' : 'Inactive'}
                        </Badge>
                      </td>
                      <td className="px-4 py-3 text-center text-neutral-600">
                        {def.sortOrder}
                      </td>
                      <td className="px-4 py-3 text-center">
                        <div className="flex items-center justify-center gap-1">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleOpenEdit(def)}
                            disabled={showForm}
                            title="Edit"
                          >
                            <Pencil className="h-4 w-4 text-neutral-500" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleToggleActive(def)}
                            disabled={togglingId === def.id}
                            title={def.isActive ? 'Deactivate' : 'Activate'}
                          >
                            {togglingId === def.id ? (
                              <RefreshCw className="h-4 w-4 animate-spin text-neutral-400" />
                            ) : def.isActive ? (
                              <ToggleRight className="h-5 w-5 text-emerald-500" />
                            ) : (
                              <ToggleLeft className="h-5 w-5 text-neutral-400" />
                            )}
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}
        </CardContent>
      </Card>

      {/* Section 2: Purchase History */}

      {/* Summary Cards */}
      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <Card>
            <CardContent className="pt-4 pb-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-violet-50">
                  <ShoppingBag className="h-5 w-5 text-violet-500" />
                </div>
                <div>
                  <p className="text-xs text-neutral-500">Total Purchases</p>
                  <p className="text-lg font-bold text-neutral-800">{summary.completedPurchases}</p>
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
                  <p className="text-xs text-neutral-500">Total Revenue</p>
                  <p className="text-lg font-bold text-neutral-800">
                    {formatCurrency(summary.totalRevenue, summary.currency)}
                  </p>
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
                  <p className="text-xs text-neutral-500">Items Sold</p>
                  <p className="text-lg font-bold text-neutral-800">{summary.totalItemsSold}</p>
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
                  <p className="text-xs text-neutral-500">Organizer Payout</p>
                  <p className="text-lg font-bold text-neutral-800">
                    {formatCurrency(summary.totalOrganizerPayout, summary.currency)}
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Payout Breakdown Card */}
      {summary && summary.completedPurchases > 0 && summary.totalRevenue > 0 && (
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-neutral-600">Your Payout</p>
                <p className="text-3xl font-bold text-green-700">
                  {formatCurrency(summary.totalOrganizerPayout, summary.currency)}
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
                <span className="font-medium">
                  {formatCurrency(summary.totalRevenue, summary.currency)}
                </span>
              </div>
              <div className="flex justify-between text-xs text-red-600 mb-1">
                <span>Stripe Fees (2.9% + $0.30):</span>
                <span className="font-medium">
                  -{formatCurrency(summary.totalStripeFees, summary.currency)}
                </span>
              </div>
              <div className="flex justify-between text-xs text-red-600 mb-1">
                <span>Platform Commission (2%):</span>
                <span className="font-medium">
                  -{formatCurrency(summary.totalPlatformCommission, summary.currency)}
                </span>
              </div>
              <div className="flex justify-between text-xs text-green-700 font-semibold pt-1 border-t border-neutral-200">
                <span>Your Payout:</span>
                <span>{formatCurrency(summary.totalOrganizerPayout, summary.currency)}</span>
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
          All Purchases ({purchases.length})
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
            disabled={isExporting || purchases.length === 0}
          >
            <Download className="h-4 w-4 mr-1" />
            {isExporting ? 'Exporting...' : 'Export Excel'}
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => handleExport('csv')}
            disabled={isExporting || purchases.length === 0}
          >
            <Download className="h-4 w-4 mr-1" />
            CSV
          </Button>
        </div>
      </div>

      {/* Purchases Table */}
      {purchases.length === 0 ? (
        <div className="text-center py-12 text-neutral-500">
          <ShoppingBag className="h-10 w-10 mx-auto mb-3 text-neutral-300" />
          <p className="text-sm">No purchases yet.</p>
          <p className="text-xs text-neutral-400 mt-1">
            Purchases will appear here once someone buys an add-on.
          </p>
        </div>
      ) : (
        <div className="overflow-x-auto rounded-lg border border-neutral-200">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-neutral-50 border-b border-neutral-200">
                <th className="text-left px-4 py-3 font-medium text-neutral-600">Buyer</th>
                <th className="text-left px-4 py-3 font-medium text-neutral-600">Add-On</th>
                <th className="text-center px-4 py-3 font-medium text-neutral-600">Qty</th>
                <th className="text-right px-4 py-3 font-medium text-neutral-600">Unit Price</th>
                <th className="text-right px-4 py-3 font-medium text-neutral-600">Total</th>
                <th className="text-center px-4 py-3 font-medium text-neutral-600">Status</th>
                <th className="text-left px-4 py-3 font-medium text-neutral-600">Date</th>
              </tr>
            </thead>
            <tbody>
              {purchases.map((purchase: AddOnPurchaseDto) => (
                <tr key={purchase.id} className="border-b border-neutral-100 hover:bg-neutral-50">
                  <td className="px-4 py-3">
                    <div>
                      <span className="font-medium text-neutral-800">{purchase.buyerName}</span>
                      <div className="space-y-0.5 mt-0.5">
                        <div className="flex items-center gap-1 text-xs text-neutral-600">
                          <Mail className="h-3 w-3" />
                          <span>{purchase.buyerEmail}</span>
                        </div>
                        {purchase.buyerPhone && (
                          <div className="flex items-center gap-1 text-xs text-neutral-500">
                            <Phone className="h-3 w-3" />
                            <span>{purchase.buyerPhone}</span>
                          </div>
                        )}
                      </div>
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <span className="text-neutral-800">{purchase.addOnName}</span>
                  </td>
                  <td className="px-4 py-3 text-center text-neutral-700">
                    {purchase.quantity}
                  </td>
                  <td className="px-4 py-3 text-right text-neutral-700">
                    {formatCurrency(purchase.unitPrice, purchase.currency)}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <span className="font-semibold text-neutral-800">
                      {formatCurrency(purchase.totalAmount, purchase.currency)}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-center">
                    <Badge style={{ backgroundColor: getPurchaseStatusColor(purchase.status) }}>
                      {purchase.status}
                    </Badge>
                  </td>
                  <td className="px-4 py-3 text-neutral-600">
                    <div className="flex items-center gap-1 text-xs">
                      <Calendar className="h-3 w-3" />
                      <span>
                        {purchase.paymentCompletedAt
                          ? new Date(purchase.paymentCompletedAt).toLocaleDateString()
                          : new Date(purchase.createdAt).toLocaleDateString()}
                      </span>
                    </div>
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
