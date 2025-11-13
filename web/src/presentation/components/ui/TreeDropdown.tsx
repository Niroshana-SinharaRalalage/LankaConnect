'use client';

import { useState, useRef, useEffect } from 'react';
import { ChevronDown, ChevronRight, Check } from 'lucide-react';

/**
 * Tree node structure for hierarchical data
 */
export interface TreeNode {
  id: string;
  label: string;
  checked: boolean;
  children?: TreeNode[];
}

/**
 * TreeDropdown Props
 */
export interface TreeDropdownProps {
  /** Tree data structure */
  nodes: TreeNode[];
  /** Selected node IDs */
  selectedIds: string[];
  /** Callback when selection changes */
  onSelectionChange: (selectedIds: string[]) => void;
  /** Placeholder text */
  placeholder?: string;
  /** Maximum number of selections allowed */
  maxSelections?: number;
  /** Disabled state */
  disabled?: boolean;
  /** Custom styling */
  className?: string;
}

/**
 * TreeDropdown Component
 * Phase 6A.9: Tree structure dropdown for hierarchical selections
 *
 * Features:
 * - Hierarchical tree structure with expand/collapse
 * - Multi-select with checkboxes
 * - Max selection limit
 * - Keyboard accessible
 * - ARIA compliant
 */
export function TreeDropdown({
  nodes,
  selectedIds,
  onSelectionChange,
  placeholder = 'Select items',
  maxSelections,
  disabled = false,
  className = '',
}: TreeDropdownProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [expandedNodes, setExpandedNodes] = useState<Set<string>>(new Set());
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Close dropdown when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
      return () => document.removeEventListener('mousedown', handleClickOutside);
    }
  }, [isOpen]);

  const toggleNode = (nodeId: string) => {
    const newExpanded = new Set(expandedNodes);
    if (newExpanded.has(nodeId)) {
      newExpanded.delete(nodeId);
    } else {
      newExpanded.add(nodeId);
    }
    setExpandedNodes(newExpanded);
  };

  const toggleSelection = (nodeId: string) => {
    const newSelected = new Set(selectedIds);

    if (newSelected.has(nodeId)) {
      newSelected.delete(nodeId);
    } else {
      // Check max selections
      if (maxSelections && newSelected.size >= maxSelections) {
        return; // Don't add if max reached
      }
      newSelected.add(nodeId);
    }

    onSelectionChange(Array.from(newSelected));
  };

  const renderTreeNode = (node: TreeNode, level: number = 0) => {
    const hasChildren = node.children && node.children.length > 0;
    const isExpanded = expandedNodes.has(node.id);
    const isSelected = selectedIds.includes(node.id);
    const indentClass = level > 0 ? `ml-${level * 6}` : '';

    return (
      <div key={node.id}>
        <div
          className={`flex items-center gap-2 px-3 py-2 hover:bg-gray-50 cursor-pointer ${indentClass}`}
          style={{ paddingLeft: `${level * 24 + 12}px` }}
        >
          {/* Expand/Collapse button for parent nodes */}
          {hasChildren ? (
            <button
              onClick={(e) => {
                e.stopPropagation();
                toggleNode(node.id);
              }}
              className="p-0.5 hover:bg-gray-200 rounded"
              aria-label={isExpanded ? 'Collapse' : 'Expand'}
            >
              {isExpanded ? (
                <ChevronDown className="h-4 w-4" style={{ color: '#FF7900' }} />
              ) : (
                <ChevronRight className="h-4 w-4" style={{ color: '#FF7900' }} />
              )}
            </button>
          ) : (
            <span className="w-5" /> // Spacer for alignment
          )}

          {/* Checkbox */}
          <label
            className="flex items-center gap-2 flex-1 cursor-pointer"
            onClick={(e) => e.stopPropagation()}
          >
            <input
              type="checkbox"
              checked={isSelected}
              onChange={() => toggleSelection(node.id)}
              disabled={disabled}
              className="h-4 w-4 rounded border-gray-300 focus:ring-2 focus:ring-offset-0"
              style={{
                accentColor: '#FF7900',
              }}
            />
            <span className="text-sm">{node.label}</span>
            {isSelected && (
              <Check className="h-3.5 w-3.5 ml-auto" style={{ color: '#006400' }} />
            )}
          </label>
        </div>

        {/* Render children if expanded */}
        {hasChildren && isExpanded && (
          <div>
            {node.children!.map((child) => renderTreeNode(child, level + 1))}
          </div>
        )}
      </div>
    );
  };

  const selectedCount = selectedIds.length;
  const displayText =
    selectedCount === 0
      ? placeholder
      : `${selectedCount} ${selectedCount === 1 ? 'item' : 'items'} selected`;

  return (
    <div ref={dropdownRef} className={`relative ${className}`}>
      {/* Dropdown trigger */}
      <button
        type="button"
        onClick={() => !disabled && setIsOpen(!isOpen)}
        disabled={disabled}
        className="w-full flex items-center justify-between px-4 py-2 bg-white border-2 rounded-lg text-sm transition-colors hover:border-gray-400 focus:outline-none focus:ring-2 focus:ring-offset-0 disabled:opacity-50 disabled:cursor-not-allowed"
        style={{
          borderColor: isOpen ? '#FF7900' : '#e2e8f0',
        }}
        aria-haspopup="listbox"
        aria-expanded={isOpen}
      >
        <span className={selectedCount === 0 ? 'text-gray-500' : 'text-gray-900'}>
          {displayText}
        </span>
        <ChevronDown
          className={`h-4 w-4 transition-transform ${isOpen ? 'rotate-180' : ''}`}
          style={{ color: '#8B1538' }}
        />
      </button>

      {/* Dropdown menu */}
      {isOpen && (
        <div
          className="absolute z-50 w-full mt-2 bg-white border-2 rounded-lg shadow-lg max-h-96 overflow-y-auto"
          style={{ borderColor: '#FF7900' }}
          role="listbox"
        >
          {nodes.length === 0 ? (
            <div className="px-4 py-3 text-sm text-gray-500 text-center">
              No items available
            </div>
          ) : (
            <div className="py-1">
              {nodes.map((node) => renderTreeNode(node))}
            </div>
          )}

          {/* Selection count footer with Done button */}
          <div
            className="px-4 py-2 border-t bg-gray-50 flex items-center justify-between"
            style={{ borderColor: '#e2e8f0' }}
          >
            {maxSelections && (
              <span className="text-xs text-gray-600">
                {selectedCount} of {maxSelections} selected
              </span>
            )}
            <button
              type="button"
              onClick={() => setIsOpen(false)}
              className="px-3 py-1 text-xs font-medium text-white rounded hover:opacity-90 transition-opacity"
              style={{ backgroundColor: '#FF7900' }}
            >
              Done
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
