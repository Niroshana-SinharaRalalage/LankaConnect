'use client';

import { useState } from 'react';
import {
  Package,
  Plus,
  Pencil,
  Trash2,
  ToggleLeft,
  ToggleRight,
  RefreshCw,
} from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { Input } from '@/presentation/components/ui/Input';
import { useAddOnDefinitions, useCreateAddOnDefinition, useUpdateAddOnDefinition } from '@/presentation/hooks/useAddOns';
import type { AddOnDefinitionDto } from '@/infrastructure/api/types/events.types';

/**
 * A pending add-on definition (not yet saved to backend).
 * Used in local mode when the event doesn't exist yet (create flow).
 */
export interface PendingAddOnDefinition {
  localId: string;
  name: string;
  description: string | null;
  price: number;
  currency: string;
  quantityLimit: number | null;
  sortOrder: number;
  isActive: boolean;
}

interface AddOnDefinitionEditorProps {
  /** When provided, editor operates in live mode (API calls). */
  eventId?: string;
  /** Local mode: pending definitions managed by parent. */
  pendingDefinitions?: PendingAddOnDefinition[];
  onPendingDefinitionsChange?: (definitions: PendingAddOnDefinition[]) => void;
}

function formatCurrency(amount: number, currency: string = 'USD'): string {
  if (currency === 'USD') {
    return `$${amount.toFixed(2)}`;
  }
  return `${amount.toFixed(2)} ${currency}`;
}

/**
 * Shared Add-On Definition editor supporting two modes:
 * - Live mode (eventId provided): CRUD via API hooks
 * - Local mode (eventId undefined): manages PendingAddOnDefinition[] in parent state
 */
export function AddOnDefinitionEditor({
  eventId,
  pendingDefinitions,
  onPendingDefinitionsChange,
}: AddOnDefinitionEditorProps) {
  const isLiveMode = !!eventId;

  // Live mode hooks
  const { data: liveDefinitions = [], isLoading } = useAddOnDefinitions(eventId, isLiveMode);
  const createDefinition = useCreateAddOnDefinition();
  const updateDefinition = useUpdateAddOnDefinition();

  // Form state
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formName, setFormName] = useState('');
  const [formDescription, setFormDescription] = useState('');
  const [formIsFree, setFormIsFree] = useState(false);
  const [formPrice, setFormPrice] = useState('');
  const [formQuantityLimit, setFormQuantityLimit] = useState('');
  const [formSortOrder, setFormSortOrder] = useState('0');
  const [formError, setFormError] = useState<string | null>(null);
  const [formSubmitting, setFormSubmitting] = useState(false);
  const [togglingId, setTogglingId] = useState<string | null>(null);

  // Unified definitions list
  const definitions: Array<{ id: string; name: string; description?: string | null; price: number; currency: string; quantityLimit?: number | null; quantitySold?: number; remainingStock?: number | null; sortOrder: number; isActive: boolean }> = isLiveMode
    ? liveDefinitions.map((d: AddOnDefinitionDto) => ({
        id: d.id,
        name: d.name,
        description: d.description,
        price: d.price,
        currency: d.currency,
        quantityLimit: d.quantityLimit,
        quantitySold: d.quantitySold,
        remainingStock: d.remainingStock,
        sortOrder: d.sortOrder,
        isActive: d.isActive,
      }))
    : (pendingDefinitions || []).map((d) => ({
        id: d.localId,
        name: d.name,
        description: d.description,
        price: d.price,
        currency: d.currency,
        quantityLimit: d.quantityLimit,
        sortOrder: d.sortOrder,
        isActive: d.isActive,
      }));

  const resetForm = () => {
    setFormName('');
    setFormDescription('');
    setFormIsFree(false);
    setFormPrice('');
    setFormQuantityLimit('');
    setFormSortOrder('0');
    setFormError(null);
    setEditingId(null);
  };

  const handleOpenCreate = () => {
    resetForm();
    setFormSortOrder(String(definitions.length));
    setShowForm(true);
  };

  const handleOpenEdit = (def: typeof definitions[0]) => {
    setEditingId(def.id);
    setFormName(def.name);
    setFormDescription(def.description || '');
    setFormIsFree(def.price === 0);
    setFormPrice(def.price === 0 ? '' : String(def.price));
    setFormQuantityLimit(def.quantityLimit != null ? String(def.quantityLimit) : '');
    setFormSortOrder(String(def.sortOrder));
    setFormError(null);
    setShowForm(true);
  };

  const handleCancelForm = () => {
    setShowForm(false);
    resetForm();
  };

  const handleFormSubmit = async (e?: React.FormEvent | React.MouseEvent) => {
    e?.preventDefault();
    setFormError(null);

    const name = formName.trim();
    if (!name) {
      setFormError('Name is required.');
      return;
    }

    const price = formIsFree ? 0 : (formPrice.trim() === '' ? 0 : parseFloat(formPrice));
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

    if (isLiveMode && eventId) {
      // Live mode: API calls
      try {
        setFormSubmitting(true);

        if (editingId) {
          const existingDef = liveDefinitions.find((d: AddOnDefinitionDto) => d.id === editingId);
          await updateDefinition.mutateAsync({
            eventId,
            definitionId: editingId,
            request: {
              name,
              description: formDescription.trim() || null,
              price,
              currency: 'USD',
              quantityLimit,
              sortOrder,
              isActive: existingDef?.isActive ?? true,
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
    } else {
      // Local mode: update pending definitions
      const current = pendingDefinitions || [];

      if (editingId) {
        const updated = current.map((d) =>
          d.localId === editingId
            ? { ...d, name, description: formDescription.trim() || null, price, quantityLimit, sortOrder }
            : d
        );
        onPendingDefinitionsChange?.(updated);
      } else {
        const newDef: PendingAddOnDefinition = {
          localId: crypto.randomUUID(),
          name,
          description: formDescription.trim() || null,
          price,
          currency: 'USD',
          quantityLimit,
          sortOrder,
          isActive: true,
        };
        onPendingDefinitionsChange?.([...current, newDef]);
      }

      setShowForm(false);
      resetForm();
    }
  };

  const handleToggleActive = async (def: typeof definitions[0]) => {
    if (isLiveMode && eventId) {
      try {
        setTogglingId(def.id);
        const existingDef = liveDefinitions.find((d: AddOnDefinitionDto) => d.id === def.id);
        if (existingDef) {
          await updateDefinition.mutateAsync({
            eventId,
            definitionId: def.id,
            request: {
              name: existingDef.name,
              description: existingDef.description,
              price: existingDef.price,
              currency: existingDef.currency,
              quantityLimit: existingDef.quantityLimit,
              sortOrder: existingDef.sortOrder,
              isActive: !existingDef.isActive,
            },
          });
        }
      } catch (err) {
        console.error('Failed to toggle add-on definition status:', err);
      } finally {
        setTogglingId(null);
      }
    } else {
      // Local mode: toggle in pending list
      const updated = (pendingDefinitions || []).map((d) =>
        d.localId === def.id ? { ...d, isActive: !d.isActive } : d
      );
      onPendingDefinitionsChange?.(updated);
    }
  };

  const handleRemove = (id: string) => {
    // Only available in local mode
    const updated = (pendingDefinitions || []).filter((d) => d.localId !== id);
    onPendingDefinitionsChange?.(updated);
  };

  if (isLiveMode && isLoading) {
    return (
      <div className="flex items-center justify-center py-6">
        <RefreshCw className="h-5 w-5 animate-spin text-neutral-400" />
        <span className="ml-2 text-sm text-neutral-500">Loading add-on items...</span>
      </div>
    );
  }

  return (
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
          <div className="mb-4 p-4 bg-neutral-50 border border-neutral-200 rounded-lg space-y-3">
            <h4 className="text-sm font-semibold text-neutral-700">
              {editingId ? 'Edit Add-On' : 'New Add-On'}
            </h4>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label htmlFor="addOnDefName" className="block text-xs font-medium text-neutral-600 mb-1">
                  Name *
                </label>
                <Input
                  id="addOnDefName"
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  placeholder="e.g., Event T-Shirt, Lunch Package"
                />
              </div>
              <div>
                <label htmlFor="addOnDefPrice" className="block text-xs font-medium text-neutral-600 mb-1">
                  Price (USD) *
                </label>
                <div className="relative">
                  <span className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500">$</span>
                  <Input
                    id="addOnDefPrice"
                    type="number"
                    min="0"
                    step="0.01"
                    value={formIsFree ? '0.00' : formPrice}
                    onChange={(e) => setFormPrice(e.target.value)}
                    placeholder="0.00"
                    className="pl-7"
                    disabled={formIsFree}
                  />
                </div>
                <label className="flex items-center gap-2 mt-1.5 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={formIsFree}
                    onChange={(e) => {
                      setFormIsFree(e.target.checked);
                      if (e.target.checked) setFormPrice('');
                    }}
                    className="h-3.5 w-3.5 rounded border-gray-300 text-violet-600 focus:ring-violet-500"
                  />
                  <span className="text-xs text-neutral-600">Free add-on (no charge)</span>
                </label>
              </div>
            </div>
            <div>
              <label htmlFor="addOnDefDescription" className="block text-xs font-medium text-neutral-600 mb-1">
                Description (optional)
              </label>
              <textarea
                id="addOnDefDescription"
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
                <label htmlFor="addOnDefQuantityLimit" className="block text-xs font-medium text-neutral-600 mb-1">
                  Quantity Limit (optional)
                </label>
                <Input
                  id="addOnDefQuantityLimit"
                  type="number"
                  min="1"
                  value={formQuantityLimit}
                  onChange={(e) => setFormQuantityLimit(e.target.value)}
                  placeholder="Unlimited"
                />
                <p className="text-[10px] text-neutral-400 mt-0.5">Leave empty for unlimited stock</p>
              </div>
              <div>
                <label htmlFor="addOnDefSortOrder" className="block text-xs font-medium text-neutral-600 mb-1">
                  Sort Order
                </label>
                <Input
                  id="addOnDefSortOrder"
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
                type="button"
                size="sm"
                disabled={formSubmitting}
                onClick={handleFormSubmit}
                style={{ background: '#7C3AED' }}
              >
                {formSubmitting
                  ? 'Saving...'
                  : editingId
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
          </div>
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
                  <th className="text-center px-4 py-3 font-medium text-neutral-600">Actions</th>
                </tr>
              </thead>
              <tbody>
                {definitions.map((def) => (
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
                      {def.price === 0 ? (
                        <Badge style={{ backgroundColor: '#10B981' }}>Free</Badge>
                      ) : (
                        <span className="font-semibold text-neutral-800">
                          {formatCurrency(def.price, def.currency)}
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-center">
                      {def.quantityLimit != null ? (
                        <span className="text-neutral-700">
                          {isLiveMode && def.quantitySold != null ? `${def.quantitySold}/` : ''}{def.quantityLimit}
                          {isLiveMode && def.remainingStock != null && def.remainingStock <= 5 && def.remainingStock > 0 && (
                            <span className="text-xs text-amber-600 ml-1">
                              ({def.remainingStock} left)
                            </span>
                          )}
                          {isLiveMode && def.remainingStock === 0 && (
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
                        {!isLiveMode && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleRemove(def.id)}
                            title="Remove"
                          >
                            <Trash2 className="h-4 w-4 text-red-400" />
                          </Button>
                        )}
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
  );
}
