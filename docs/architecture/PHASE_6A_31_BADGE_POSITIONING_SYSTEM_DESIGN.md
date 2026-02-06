# System Architecture Design: Badge Positioning and Scaling System
## Phase 6A.31 - Per-Location Badge Customization

**Status**: Design Proposal
**Created**: 2025-12-14
**Author**: System Architecture Designer
**Related Phases**: 6A.27 (Badge Management), 6A.28 (Duration Model), 6A.30 (Badge Preview)

---

## Executive Summary

This document provides a comprehensive architectural design for implementing per-location badge customization with responsive scaling, addressing the limitations of the current hardcoded badge positioning system (Phase 6A.30).

**Key Architectural Decisions:**
- **Data Model**: Separate BadgeLocationConfig entity with one-to-many relationship
- **Position Strategy**: Hybrid (corner anchor + percentage offsets)
- **Size Strategy**: Percentage ratios with optional aspect ratio lock
- **Storage**: Decimal ratios (0.0-1.0) for responsive scaling
- **UI Pattern**: Tab-based interface with draggable canvas preview
- **Migration**: Automated with sensible defaults based on current Position enum

**Quality Attributes Prioritized:**
1. **Responsiveness**: Scales perfectly from mobile (100px) to desktop (384px)
2. **Maintainability**: Clean separation of concerns, DDD principles
3. **Usability**: Intuitive drag-and-drop with precise pixel inputs
4. **Performance**: Simple calculations, minimal database overhead
5. **Extensibility**: Easy to add new display locations

---

## Table of Contents

1. [Architecture Decision Records](#architecture-decision-records)
2. [Entity Relationship Diagram](#entity-relationship-diagram)
3. [Domain Model Design](#domain-model-design)
4. [Database Schema Changes](#database-schema-changes)
5. [API Contract Design](#api-contract-design)
6. [Calculation Engine](#calculation-engine)
7. [UI Component Architecture](#ui-component-architecture)
8. [Migration Strategy](#migration-strategy)
9. [Risk Assessment](#risk-assessment)
10. [Implementation Roadmap](#implementation-roadmap)

---

## 1. Architecture Decision Records

### ADR-001: Badge Location Configuration Storage Model

**Status**: Proposed

**Context**:
Need to store per-location badge configurations for 3 display contexts while maintaining backward compatibility and clean separation of concerns.

**Current State (Phase 6A.30)**:
- Single `Position` enum on Badge entity (TopLeft/TopRight/BottomLeft/BottomRight)
- Hardcoded sizes in BadgeOverlayGroup component (50px, 42px, 80px)
- No per-location customization

**Decision**:
Implement **Separate BadgeLocationConfig Entity** with one-to-many relationship from Badge.

**Rationale**:

| Factor | Option A: On Badge | Option B: On EventBadge | Option C: Separate Entity | Winner |
|--------|-------------------|------------------------|--------------------------|--------|
| Separation of Concerns | Poor - Bloats Badge | Wrong level | Excellent | **C** |
| Query Performance | Good | Good | Needs JOIN | A/B |
| Extensibility | Hard - Schema change | Hard - Schema change | Easy - Add rows | **C** |
| DDD Principles | Violates SRP | Wrong aggregate | Clean aggregate | **C** |
| Storage Overhead | Medium | High | Low | **C** |

**Winner**: Option C - Separate entity provides best long-term maintainability despite JOIN overhead.

**Implementation**:
```csharp
// Badge remains focused on core badge properties
public class Badge : BaseEntity
{
    public string Name { get; private set; }
    public string ImageUrl { get; private set; }
    // Remove: public BadgePosition Position { get; private set; }

    // One-to-many relationship
    private readonly List<BadgeLocationConfig> _locationConfigs = new();
    public IReadOnlyCollection<BadgeLocationConfig> LocationConfigs => _locationConfigs.AsReadOnly();
}

// Separate entity for display configuration
public class BadgeLocationConfig : BaseEntity
{
    public Guid BadgeId { get; private set; }
    public DisplayLocation Location { get; private set; }
    public BadgeCorner AnchorCorner { get; private set; }
    public decimal OffsetXRatio { get; private set; }
    public decimal OffsetYRatio { get; private set; }
    public decimal SizeWidthRatio { get; private set; }
    public decimal SizeHeightRatio { get; private set; }
    public bool LockAspectRatio { get; private set; }
    public bool IsEnabled { get; private set; }
}
```

**Consequences**:
- **Positive**: Easy to add new display locations (e.g., notification thumbnails)
- **Positive**: Badge entity remains focused on core properties
- **Negative**: Additional JOIN query for badge preview (acceptable cost)
- **Negative**: More complex migration from Phase 6A.30

**Alternatives Considered**:
- **Option A** (3 sets on Badge entity): Creates bloated Badge entity with 18+ new columns (6 fields × 3 locations)
- **Option B** (on EventBadge): Wrong level - config is badge property, not event-badge relationship

---

### ADR-002: Position and Size Calculation Strategy

**Status**: Proposed

**Context**:
Need responsive badge positioning that works across:
- **Mobile**: 100px × 80px containers
- **Tablet**: 192px × 144px containers
- **Desktop**: 384px × 288px containers

While giving creators precise control over badge appearance.

**Decision**:
Use **Hybrid Ratio-Based Approach**:
- **Anchor Corner** (enum): TopLeft, TopRight, BottomLeft, BottomRight
- **Offset Ratios** (decimal 0.0-1.0): X and Y as percentage of container dimensions
- **Size Ratios** (decimal 0.0-1.0): Width and height as percentage of container dimensions

**Rationale**:

| Strategy | Responsiveness | Precision | UI Complexity | Performance | Winner |
|----------|---------------|-----------|--------------|-------------|--------|
| Absolute Pixels | Poor | Excellent | Simple | Excellent | ❌ |
| Pure Ratios | Excellent | Good | Complex | Excellent | ⭐ |
| Hybrid (Anchor + Ratios) | Excellent | Excellent | Medium | Excellent | **✅** |

**Calculation Formula**:
```typescript
function calculateBadgePosition(
  containerWidth: number,
  containerHeight: number,
  config: BadgeLocationConfig
): { x: number; y: number; width: number; height: number } {
  // Calculate badge size first (needed for right/bottom anchors)
  const badgeWidth = containerWidth * config.sizeWidthRatio;
  const badgeHeight = containerHeight * config.sizeHeightRatio;

  let x = 0, y = 0;

  switch (config.anchorCorner) {
    case BadgeCorner.TopLeft:
      x = containerWidth * config.offsetXRatio;
      y = containerHeight * config.offsetYRatio;
      break;
    case BadgeCorner.TopRight:
      x = containerWidth * (1 - config.offsetXRatio) - badgeWidth;
      y = containerHeight * config.offsetYRatio;
      break;
    case BadgeCorner.BottomLeft:
      x = containerWidth * config.offsetXRatio;
      y = containerHeight * (1 - config.offsetYRatio) - badgeHeight;
      break;
    case BadgeCorner.BottomRight:
      x = containerWidth * (1 - config.offsetXRatio) - badgeWidth;
      y = containerHeight * (1 - config.offsetYRatio) - badgeHeight;
      break;
  }

  return { x, y, width: badgeWidth, height: badgeHeight };
}
```

**User Experience Example**:

Creator sets badge on **Event Detail Hero** (384px × 288px):
- Badge size: 50px × 50px = **14.3% width, 17.4% height**
- Position: Top Right, 0px offset = **0% offset from right edge**

System stores:
```json
{
  "sizeWidthRatio": 0.143,
  "sizeHeightRatio": 0.174,
  "anchorCorner": "TopRight",
  "offsetXRatio": 0.0,
  "offsetYRatio": 0.0
}
```

On mobile (100px × 80px), system calculates:
- Badge size: 14.3px × 13.9px ✅
- Position: Top Right, 0px offset ✅

**Consequences**:
- **Positive**: Perfect responsive scaling across all devices
- **Positive**: Creators work with familiar pixel values (UI auto-converts)
- **Positive**: Simple multiplication on render (high performance)
- **Negative**: UI must transparently convert pixels ↔ ratios
- **Negative**: Edge cases need validation (badge larger than container)

**Aspect Ratio Lock**:
When `LockAspectRatio = true`:
```typescript
// If user changes width, auto-adjust height
if (lockAspectRatio) {
  const aspectRatio = originalWidth / originalHeight;
  sizeHeightRatio = sizeWidthRatio / aspectRatio;
}
```

---

### ADR-003: UI Component Architecture

**Status**: Proposed

**Context**:
Badge creators need to configure 3 different display locations with different container sizes and requirements.

**Current UI (Phase 6A.30)**:
- Single dropdown for global Position (TopLeft/TopRight/BottomLeft/BottomRight)
- Static preview showing 3 mock locations with hardcoded sizes
- No per-location customization

**Decision**:
Implement **Tab-Based Interface** with interactive canvas:

```
┌────────────────────────────────────────────────────────┐
│ Badge Customization                                    │
├────────────────────────────────────────────────────────┤
│ Tabs: [📋 Events Listing] [🏠 Home Featured] [🎯 Event Detail] │
├────────────────────────────────────────────────────────┤
│ Preview Canvas (Mock Event Card with correct dimensions) │
│  ┌──────────────────────────────────────┐              │
│  │  📷 Mock Event Image (192×144)       │              │
│  │                                      │              │
│  │     ┌──────┐  ← Draggable badge     │              │
│  │     │BADGE │  ← Resize handles       │              │
│  │     └──────┘                         │              │
│  └──────────────────────────────────────┘              │
│                                                        │
│ Position Controls:                                     │
│  Anchor: [TopLeft ▼]                                  │
│  X Offset: [0] px  (0% of container)                  │
│  Y Offset: [0] px  (0% of container)                  │
│                                                        │
│ Size Controls:                                         │
│  Width:  [27] px  (14.0% of container)                │
│  Height: [27] px  (18.8% of container)                │
│  ☑ Lock aspect ratio                                  │
│                                                        │
│ Quick Actions:                                         │
│  ☐ Use same config for all locations                  │
│  [Reset to Default]  [Copy from Event Detail]         │
└────────────────────────────────────────────────────────┘
```

**Rationale**:

| Layout | Pros | Cons | Score |
|--------|------|------|-------|
| **Tabs** | Focused, clean, familiar pattern | Requires clicking to switch | **9/10** ✅ |
| Accordion | See all at once | Cramped, lots of scrolling | 6/10 |
| Side-by-side | Compare locations | Too wide, mobile unfriendly | 5/10 |

**Component Hierarchy**:
```typescript
<BadgeCustomizationDialog>
  <TabGroup>
    <Tab label="Events Listing">
      <BadgeCanvasEditor
        containerDimensions={{ width: 192, height: 144 }}
        config={eventsListingConfig}
        onConfigChange={handleEventsListingChange}
      />
      <BadgePositionControls config={eventsListingConfig} />
      <BadgeSizeControls config={eventsListingConfig} />
    </Tab>

    <Tab label="Home Featured">
      <BadgeCanvasEditor
        containerDimensions={{ width: 160, height: 120 }}
        config={homeFeaturedConfig}
        onConfigChange={handleHomeFeaturedChange}
      />
      {/* ... same controls ... */}
    </Tab>

    <Tab label="Event Detail">
      <BadgeCanvasEditor
        containerDimensions={{ width: 384, height: 288 }}
        config={eventDetailConfig}
        onConfigChange={handleEventDetailChange}
      />
      {/* ... same controls ... */}
    </Tab>
  </TabGroup>

  <QuickActions>
    <CopyConfigButton />
    <ResetToDefaultButton />
    <UseSameForAllCheckbox />
  </QuickActions>
</BadgeCustomizationDialog>
```

**Interaction Flow**:

1. **Drag Badge**: Updates `offsetXRatio` and `offsetYRatio` in real-time
2. **Resize Badge**: Updates `sizeWidthRatio` and `sizeHeightRatio`
3. **Change Anchor**: Recalculates offset ratios to maintain visual position
4. **Type Pixels**: UI converts to ratio before updating state
5. **Lock Aspect Ratio**: Constrains resize to maintain original ratio

**Consequences**:
- **Positive**: Intuitive drag-and-drop workflow
- **Positive**: Precise pixel control for power users
- **Positive**: Real-time preview prevents mistakes
- **Negative**: Complex state management (3 configs × 8 fields each = 24 state variables)
- **Negative**: Canvas drag/resize implementation complexity

---

### ADR-004: Display Location Enumeration

**Status**: Proposed

**Context**:
Need to identify which display context a configuration applies to. Currently no such concept exists.

**Decision**:
Create `DisplayLocation` enum with 3 initial values:

```csharp
namespace LankaConnect.Domain.Badges.Enums;

/// <summary>
/// Identifies where a badge is displayed in the application
/// Each location has different container dimensions and user context
/// </summary>
public enum DisplayLocation
{
    /// <summary>
    /// Badge on event cards in /events listing page
    /// Container: 192px × 144px (Tailwind h-48)
    /// Context: User browsing all events
    /// </summary>
    EventsListing = 0,

    /// <summary>
    /// Badge on featured event banner on home page
    /// Container: Variable width × 160px (Tailwind h-40)
    /// Context: Prominent hero section, attracts attention
    /// </summary>
    HomeFeatured = 1,

    /// <summary>
    /// Badge on large hero image in event detail page
    /// Container: Variable width × 384px (Tailwind h-96)
    /// Context: User viewing specific event details
    /// </summary>
    EventDetailHero = 2

    // Future: NotificationThumbnail = 3
    // Future: SharePreview = 4
}
```

**Rationale**:
- **Extensibility**: Easy to add new locations without schema changes
- **Type Safety**: Enum prevents invalid location values
- **Documentation**: Each location documents its dimensions and context
- **Database**: Stores as int (0, 1, 2) for efficiency

**Container Dimension Reference**:
```typescript
export const DISPLAY_LOCATION_DIMENSIONS = {
  [DisplayLocation.EventsListing]: { width: 192, height: 144 },
  [DisplayLocation.HomeFeatured]: { width: 160, height: 120 }, // Approximation for variable width
  [DisplayLocation.EventDetailHero]: { width: 384, height: 288 }
} as const;
```

---

## 2. Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────┐
│ Badge (Aggregate Root)                                  │
├─────────────────────────────────────────────────────────┤
│ PK  Id: Guid                                            │
│     Name: string (max 50)                               │
│     ImageUrl: string                                    │
│     BlobName: string                                    │
│     IsActive: bool                                      │
│     IsSystem: bool                                      │
│     DisplayOrder: int                                   │
│     CreatedByUserId: Guid?                              │
│     DefaultDurationDays: int?                           │
│     CreatedAt: DateTime                                 │
│     UpdatedAt: DateTime                                 │
│                                                         │
│ ❌ Position: BadgePosition (DEPRECATED - Phase 6A.31)   │
│                                                         │
│ Navigation:                                             │
│     LocationConfigs: List<BadgeLocationConfig>          │
└─────────────────────────────────────────────────────────┘
                        │ 1
                        │ owns
                        │ * (0-3 initially, one per DisplayLocation)
                        ▼
┌─────────────────────────────────────────────────────────┐
│ BadgeLocationConfig (Entity)                            │
├─────────────────────────────────────────────────────────┤
│ PK  Id: Guid                                            │
│ FK  BadgeId: Guid                                       │
│ UK  Location: DisplayLocation (enum: 0,1,2)             │
│     AnchorCorner: BadgeCorner (enum: 0,1,2,3)           │
│     OffsetXRatio: decimal(5,4)  [0.0000 - 1.0000]       │
│     OffsetYRatio: decimal(5,4)  [0.0000 - 1.0000]       │
│     SizeWidthRatio: decimal(5,4) [0.0000 - 1.0000]      │
│     SizeHeightRatio: decimal(5,4) [0.0000 - 1.0000]     │
│     LockAspectRatio: bool                               │
│     IsEnabled: bool                                     │
│     CreatedAt: DateTime                                 │
│     UpdatedAt: DateTime                                 │
│                                                         │
│ Navigation:                                             │
│     Badge: Badge (parent aggregate)                     │
│                                                         │
│ Constraints:                                            │
│   UNIQUE (BadgeId, Location) - One config per location │
│   CHECK (OffsetXRatio >= 0 AND OffsetXRatio <= 1)       │
│   CHECK (OffsetYRatio >= 0 AND OffsetYRatio <= 1)       │
│   CHECK (SizeWidthRatio > 0 AND SizeWidthRatio <= 1)    │
│   CHECK (SizeHeightRatio > 0 AND SizeHeightRatio <= 1)  │
└─────────────────────────────────────────────────────────┘
                        │
                        │ (No change to EventBadge)
                        │
┌─────────────────────────────────────────────────────────┐
│ EventBadge (Junction Table - Unchanged)                │
├─────────────────────────────────────────────────────────┤
│ PK  EventId: Guid                                       │
│ PK  BadgeId: Guid                                       │
│     AssignedAt: DateTime                                │
│     ExpiresAt: DateTime?                                │
│     AssignedBy: Guid                                    │
│                                                         │
│ Navigation:                                             │
│     Event: Event                                        │
│     Badge: Badge                                        │
└─────────────────────────────────────────────────────────┘
                        │
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│ Event (Aggregate Root - Unchanged)                     │
│ ...existing fields...                                   │
└─────────────────────────────────────────────────────────┘

Enums:
┌──────────────────────────────────────┐
│ DisplayLocation (new)                │
│ - EventsListing = 0                  │
│ - HomeFeatured = 1                   │
│ - EventDetailHero = 2                │
└──────────────────────────────────────┘

┌──────────────────────────────────────┐
│ BadgeCorner (renamed from            │
│ BadgePosition)                       │
│ - TopLeft = 0                        │
│ - TopRight = 1                       │
│ - BottomLeft = 2                     │
│ - BottomRight = 3                    │
└──────────────────────────────────────┘
```

**Key Relationships**:
- **Badge → BadgeLocationConfig**: One-to-Many (one badge can have up to 3 configs, one per DisplayLocation)
- **Unique Constraint**: (BadgeId, Location) ensures only one config per location
- **Cascade Delete**: Deleting a badge deletes its location configs (owned entity)
- **No Change to EventBadge**: Badge assignment to events remains unchanged

---

## 3. Domain Model Design

### 3.1 BadgeLocationConfig Entity

```csharp
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Badges.Enums;

namespace LankaConnect.Domain.Badges;

/// <summary>
/// Defines how a badge should be displayed at a specific location in the application
/// Stores position and size as ratios (0.0-1.0) for responsive scaling
/// Phase 6A.31: Replaces single Position enum with per-location customization
/// </summary>
public class BadgeLocationConfig : BaseEntity
{
    /// <summary>
    /// The badge this configuration belongs to
    /// </summary>
    public Guid BadgeId { get; private set; }

    /// <summary>
    /// Where in the application this configuration applies
    /// </summary>
    public DisplayLocation Location { get; private set; }

    /// <summary>
    /// Which corner of the container the badge is anchored to
    /// </summary>
    public BadgeCorner AnchorCorner { get; private set; }

    /// <summary>
    /// Horizontal offset from anchor as ratio of container width
    /// Range: 0.0 (no offset) to 1.0 (full container width)
    /// Example: 0.05 = 5% offset from anchor corner
    /// </summary>
    public decimal OffsetXRatio { get; private set; }

    /// <summary>
    /// Vertical offset from anchor as ratio of container height
    /// Range: 0.0 (no offset) to 1.0 (full container height)
    /// </summary>
    public decimal OffsetYRatio { get; private set; }

    /// <summary>
    /// Badge width as ratio of container width
    /// Range: 0.0001 (tiny badge) to 1.0 (full container width)
    /// Example: 0.143 = 14.3% of container width
    /// </summary>
    public decimal SizeWidthRatio { get; private set; }

    /// <summary>
    /// Badge height as ratio of container height
    /// Range: 0.0001 (tiny badge) to 1.0 (full container height)
    /// Example: 0.174 = 17.4% of container height
    /// </summary>
    public decimal SizeHeightRatio { get; private set; }

    /// <summary>
    /// If true, resizing badge maintains aspect ratio
    /// Useful for logos that must not be distorted
    /// </summary>
    public bool LockAspectRatio { get; private set; }

    /// <summary>
    /// If false, badge is not rendered at this location
    /// Allows disabling badge on specific display contexts
    /// </summary>
    public bool IsEnabled { get; private set; }

    // Navigation property to parent aggregate
    public Badge Badge { get; private set; } = null!;

    // EF Core constructor
    private BadgeLocationConfig()
    {
    }

    private BadgeLocationConfig(
        Guid badgeId,
        DisplayLocation location,
        BadgeCorner anchorCorner,
        decimal offsetXRatio,
        decimal offsetYRatio,
        decimal sizeWidthRatio,
        decimal sizeHeightRatio,
        bool lockAspectRatio,
        bool isEnabled = true)
    {
        BadgeId = badgeId;
        Location = location;
        AnchorCorner = anchorCorner;
        OffsetXRatio = offsetXRatio;
        OffsetYRatio = offsetYRatio;
        SizeWidthRatio = sizeWidthRatio;
        SizeHeightRatio = sizeHeightRatio;
        LockAspectRatio = lockAspectRatio;
        IsEnabled = isEnabled;
    }

    /// <summary>
    /// Factory method to create a new badge location configuration
    /// </summary>
    public static Result<BadgeLocationConfig> Create(
        Guid badgeId,
        DisplayLocation location,
        BadgeCorner anchorCorner,
        decimal offsetXRatio,
        decimal offsetYRatio,
        decimal sizeWidthRatio,
        decimal sizeHeightRatio,
        bool lockAspectRatio = false,
        bool isEnabled = true)
    {
        // Validate badge ID
        if (badgeId == Guid.Empty)
            return Result<BadgeLocationConfig>.Failure("Badge ID is required");

        // Validate offset ratios
        if (offsetXRatio < 0 || offsetXRatio > 1)
            return Result<BadgeLocationConfig>.Failure("Offset X ratio must be between 0 and 1");

        if (offsetYRatio < 0 || offsetYRatio > 1)
            return Result<BadgeLocationConfig>.Failure("Offset Y ratio must be between 0 and 1");

        // Validate size ratios
        if (sizeWidthRatio <= 0 || sizeWidthRatio > 1)
            return Result<BadgeLocationConfig>.Failure("Size width ratio must be between 0 and 1");

        if (sizeHeightRatio <= 0 || sizeHeightRatio > 1)
            return Result<BadgeLocationConfig>.Failure("Size height ratio must be between 0 and 1");

        // Validate reasonable badge sizes (at least 1% of container)
        if (sizeWidthRatio < 0.01m)
            return Result<BadgeLocationConfig>.Failure("Badge width must be at least 1% of container width");

        if (sizeHeightRatio < 0.01m)
            return Result<BadgeLocationConfig>.Failure("Badge height must be at least 1% of container height");

        var config = new BadgeLocationConfig(
            badgeId,
            location,
            anchorCorner,
            offsetXRatio,
            offsetYRatio,
            sizeWidthRatio,
            sizeHeightRatio,
            lockAspectRatio,
            isEnabled);

        return Result<BadgeLocationConfig>.Success(config);
    }

    /// <summary>
    /// Updates the position configuration
    /// </summary>
    public Result UpdatePosition(
        BadgeCorner anchorCorner,
        decimal offsetXRatio,
        decimal offsetYRatio)
    {
        if (offsetXRatio < 0 || offsetXRatio > 1)
            return Result.Failure("Offset X ratio must be between 0 and 1");

        if (offsetYRatio < 0 || offsetYRatio > 1)
            return Result.Failure("Offset Y ratio must be between 0 and 1");

        AnchorCorner = anchorCorner;
        OffsetXRatio = offsetXRatio;
        OffsetYRatio = offsetYRatio;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Updates the size configuration
    /// </summary>
    public Result UpdateSize(
        decimal sizeWidthRatio,
        decimal sizeHeightRatio,
        bool lockAspectRatio)
    {
        if (sizeWidthRatio <= 0 || sizeWidthRatio > 1)
            return Result.Failure("Size width ratio must be between 0 and 1");

        if (sizeHeightRatio <= 0 || sizeHeightRatio > 1)
            return Result.Failure("Size height ratio must be between 0 and 1");

        if (sizeWidthRatio < 0.01m)
            return Result.Failure("Badge width must be at least 1% of container width");

        if (sizeHeightRatio < 0.01m)
            return Result.Failure("Badge height must be at least 1% of container height");

        SizeWidthRatio = sizeWidthRatio;
        SizeHeightRatio = sizeHeightRatio;
        LockAspectRatio = lockAspectRatio;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Enables the badge at this location
    /// </summary>
    public Result Enable()
    {
        if (IsEnabled)
            return Result.Failure("Configuration is already enabled");

        IsEnabled = true;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Disables the badge at this location (badge won't render)
    /// </summary>
    public Result Disable()
    {
        if (!IsEnabled)
            return Result.Failure("Configuration is already disabled");

        IsEnabled = false;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Calculates actual pixel dimensions for a given container size
    /// </summary>
    public (int x, int y, int width, int height) CalculatePixelDimensions(
        int containerWidth,
        int containerHeight)
    {
        // Calculate badge size first (needed for right/bottom anchors)
        var badgeWidth = (int)Math.Round(containerWidth * SizeWidthRatio);
        var badgeHeight = (int)Math.Round(containerHeight * SizeHeightRatio);

        int x, y;

        switch (AnchorCorner)
        {
            case BadgeCorner.TopLeft:
                x = (int)Math.Round(containerWidth * OffsetXRatio);
                y = (int)Math.Round(containerHeight * OffsetYRatio);
                break;

            case BadgeCorner.TopRight:
                x = (int)Math.Round(containerWidth * (1 - OffsetXRatio)) - badgeWidth;
                y = (int)Math.Round(containerHeight * OffsetYRatio);
                break;

            case BadgeCorner.BottomLeft:
                x = (int)Math.Round(containerWidth * OffsetXRatio);
                y = (int)Math.Round(containerHeight * (1 - OffsetYRatio)) - badgeHeight;
                break;

            case BadgeCorner.BottomRight:
                x = (int)Math.Round(containerWidth * (1 - OffsetXRatio)) - badgeWidth;
                y = (int)Math.Round(containerHeight * (1 - OffsetYRatio)) - badgeHeight;
                break;

            default:
                throw new InvalidOperationException($"Unknown anchor corner: {AnchorCorner}");
        }

        // Ensure badge stays within container bounds
        x = Math.Max(0, Math.Min(x, containerWidth - badgeWidth));
        y = Math.Max(0, Math.Min(y, containerHeight - badgeHeight));

        return (x, y, badgeWidth, badgeHeight);
    }
}
```

### 3.2 Updated Badge Entity

```csharp
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Badges.Enums;

namespace LankaConnect.Domain.Badges;

/// <summary>
/// Represents a visual overlay badge/sticker that can be applied to event images
/// Phase 6A.31: Added per-location configuration support via BadgeLocationConfig
/// </summary>
public class Badge : BaseEntity
{
    public string Name { get; private set; }
    public string ImageUrl { get; private set; }
    public string BlobName { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsSystem { get; private set; }
    public int DisplayOrder { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public int? DefaultDurationDays { get; private set; }

    /// <summary>
    /// DEPRECATED: Single global position replaced by per-location configs
    /// Kept for backward compatibility during migration (Phase 6A.31)
    /// Will be removed in future phase after all data migrated
    /// </summary>
    [Obsolete("Use LocationConfigs instead. Will be removed after Phase 6A.31 migration.")]
    public BadgePosition? Position { get; private set; }

    // One-to-many relationship with location configs
    private readonly List<BadgeLocationConfig> _locationConfigs = new();
    public IReadOnlyCollection<BadgeLocationConfig> LocationConfigs => _locationConfigs.AsReadOnly();

    // EF Core constructor
    private Badge()
    {
        Name = null!;
        ImageUrl = null!;
        BlobName = null!;
    }

    private Badge(
        string name,
        string imageUrl,
        string blobName,
        bool isSystem,
        int displayOrder,
        Guid? createdByUserId,
        int? defaultDurationDays = null)
    {
        Name = name;
        ImageUrl = imageUrl;
        BlobName = blobName;
        IsActive = true;
        IsSystem = isSystem;
        DisplayOrder = displayOrder;
        CreatedByUserId = createdByUserId;
        DefaultDurationDays = defaultDurationDays;
        Position = null; // Phase 6A.31: Deprecated
    }

    /// <summary>
    /// Factory method to create a new custom badge with per-location configs
    /// Phase 6A.31: Now accepts location configs instead of single position
    /// </summary>
    public static Result<Badge> Create(
        string name,
        string imageUrl,
        string blobName,
        int displayOrder,
        Guid createdByUserId,
        List<BadgeLocationConfig>? locationConfigs = null,
        int? defaultDurationDays = null)
    {
        // ... existing validations ...

        var badge = new Badge(
            name.Trim(),
            imageUrl,
            blobName,
            isSystem: false,
            displayOrder,
            createdByUserId,
            defaultDurationDays);

        // Add location configs if provided
        if (locationConfigs != null && locationConfigs.Any())
        {
            foreach (var config in locationConfigs)
            {
                badge._locationConfigs.Add(config);
            }
        }

        return Result<Badge>.Success(badge);
    }

    /// <summary>
    /// Adds or updates a location configuration
    /// Phase 6A.31: Replaces single Position property
    /// </summary>
    public Result AddOrUpdateLocationConfig(BadgeLocationConfig config)
    {
        if (config.BadgeId != Id)
            return Result.Failure("Configuration badge ID does not match this badge");

        // Check if config already exists for this location
        var existing = _locationConfigs.FirstOrDefault(c => c.Location == config.Location);
        if (existing != null)
        {
            _locationConfigs.Remove(existing);
        }

        _locationConfigs.Add(config);
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Gets the configuration for a specific display location
    /// Returns null if no configuration exists for that location
    /// </summary>
    public BadgeLocationConfig? GetLocationConfig(DisplayLocation location)
    {
        return _locationConfigs.FirstOrDefault(c => c.Location == location);
    }

    /// <summary>
    /// Checks if badge has configuration for a specific location
    /// </summary>
    public bool HasLocationConfig(DisplayLocation location)
    {
        return _locationConfigs.Any(c => c.Location == location && c.IsEnabled);
    }

    /// <summary>
    /// Creates default location configs for all display locations
    /// Uses provided corner or defaults to TopRight
    /// Phase 6A.31: Used during migration from single Position to per-location configs
    /// </summary>
    public Result CreateDefaultLocationConfigs(BadgeCorner defaultCorner = BadgeCorner.TopRight)
    {
        var locations = Enum.GetValues<DisplayLocation>();

        foreach (var location in locations)
        {
            // Skip if config already exists
            if (_locationConfigs.Any(c => c.Location == location))
                continue;

            // Create sensible defaults based on location
            var (widthRatio, heightRatio) = GetDefaultSizeForLocation(location);

            var configResult = BadgeLocationConfig.Create(
                Id,
                location,
                defaultCorner,
                offsetXRatio: 0.0m,
                offsetYRatio: 0.0m,
                sizeWidthRatio: widthRatio,
                sizeHeightRatio: heightRatio,
                lockAspectRatio: true,
                isEnabled: true);

            if (configResult.IsFailure)
                return Result.Failure($"Failed to create default config for {location}: {configResult.Error}");

            _locationConfigs.Add(configResult.Value);
        }

        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Gets default size ratios for each display location
    /// Based on typical container dimensions and desired badge prominence
    /// </summary>
    private (decimal widthRatio, decimal heightRatio) GetDefaultSizeForLocation(DisplayLocation location)
    {
        return location switch
        {
            DisplayLocation.EventsListing => (0.140m, 0.188m),     // 27px on 192×144
            DisplayLocation.HomeFeatured => (0.150m, 0.200m),      // 24px on 160×120
            DisplayLocation.EventDetailHero => (0.143m, 0.174m),   // 50px on 350×288
            _ => (0.150m, 0.150m) // Fallback
        };
    }

    // ... rest of existing methods unchanged ...
}
```

### 3.3 New Enums

```csharp
namespace LankaConnect.Domain.Badges.Enums;

/// <summary>
/// Identifies where a badge is displayed in the application
/// Phase 6A.31: New enum for per-location badge configuration
/// </summary>
public enum DisplayLocation
{
    /// <summary>
    /// Badge on event cards in /events listing page
    /// Typical container: 192px × 144px (Tailwind h-48)
    /// Context: User browsing all events, needs quick visual identification
    /// </summary>
    EventsListing = 0,

    /// <summary>
    /// Badge on featured event banner on home page
    /// Typical container: Variable width × 160px (Tailwind h-40)
    /// Context: Prominent hero section on landing page
    /// </summary>
    HomeFeatured = 1,

    /// <summary>
    /// Badge on large hero image in event detail page
    /// Typical container: Variable width × 384px (Tailwind h-96)
    /// Context: User viewing specific event, badge can be larger and more prominent
    /// </summary>
    EventDetailHero = 2
}

/// <summary>
/// Corner anchor point for badge positioning
/// Phase 6A.31: Renamed from BadgePosition to BadgeCorner for clarity
/// Used in conjunction with offset ratios for precise positioning
/// </summary>
public enum BadgeCorner
{
    TopLeft = 0,
    TopRight = 1,
    BottomLeft = 2,
    BottomRight = 3
}
```

---

## 4. Database Schema Changes

### 4.1 Migration SQL

```sql
-- Phase 6A.31: Badge Positioning and Scaling System
-- Creates BadgeLocationConfig table and migrates existing Position data

-- Step 1: Create DisplayLocation and BadgeCorner enums (handled by EF Core)

-- Step 2: Create BadgeLocationConfigs table
CREATE TABLE [BadgeLocationConfigs] (
    [Id] uniqueidentifier NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [BadgeId] uniqueidentifier NOT NULL,
    [Location] int NOT NULL,  -- DisplayLocation enum
    [AnchorCorner] int NOT NULL,  -- BadgeCorner enum
    [OffsetXRatio] decimal(5,4) NOT NULL DEFAULT 0.0,
    [OffsetYRatio] decimal(5,4) NOT NULL DEFAULT 0.0,
    [SizeWidthRatio] decimal(5,4) NOT NULL,
    [SizeHeightRatio] decimal(5,4) NOT NULL,
    [LockAspectRatio] bit NOT NULL DEFAULT 1,
    [IsEnabled] bit NOT NULL DEFAULT 1,
    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),

    -- Foreign key to Badge
    CONSTRAINT [FK_BadgeLocationConfigs_Badges_BadgeId]
        FOREIGN KEY ([BadgeId])
        REFERENCES [Badges]([Id])
        ON DELETE CASCADE,

    -- Unique constraint: One config per badge per location
    CONSTRAINT [UQ_BadgeLocationConfigs_BadgeId_Location]
        UNIQUE ([BadgeId], [Location]),

    -- Check constraints for valid ratios
    CONSTRAINT [CK_BadgeLocationConfigs_OffsetXRatio]
        CHECK ([OffsetXRatio] >= 0.0 AND [OffsetXRatio] <= 1.0),

    CONSTRAINT [CK_BadgeLocationConfigs_OffsetYRatio]
        CHECK ([OffsetYRatio] >= 0.0 AND [OffsetYRatio] <= 1.0),

    CONSTRAINT [CK_BadgeLocationConfigs_SizeWidthRatio]
        CHECK ([SizeWidthRatio] > 0.0 AND [SizeWidthRatio] <= 1.0),

    CONSTRAINT [CK_BadgeLocationConfigs_SizeHeightRatio]
        CHECK ([SizeHeightRatio] > 0.0 AND [SizeHeightRatio] <= 1.0)
);

-- Step 3: Create index for common query pattern
CREATE NONCLUSTERED INDEX [IX_BadgeLocationConfigs_BadgeId_Location]
    ON [BadgeLocationConfigs]([BadgeId], [Location])
    INCLUDE ([IsEnabled]);

-- Step 4: Migrate existing Position data to location configs
-- For each existing badge, create 3 location configs with defaults
INSERT INTO [BadgeLocationConfigs] (
    [Id],
    [BadgeId],
    [Location],
    [AnchorCorner],
    [OffsetXRatio],
    [OffsetYRatio],
    [SizeWidthRatio],
    [SizeHeightRatio],
    [LockAspectRatio],
    [IsEnabled],
    [CreatedAt],
    [UpdatedAt]
)
SELECT
    NEWID() as [Id],
    b.[Id] as [BadgeId],
    loc.[Location],
    ISNULL(b.[Position], 1) as [AnchorCorner],  -- Default to TopRight (1) if Position is NULL
    0.0 as [OffsetXRatio],
    0.0 as [OffsetYRatio],
    CASE loc.[Location]
        WHEN 0 THEN 0.1406  -- EventsListing: 27px / 192px
        WHEN 1 THEN 0.1500  -- HomeFeatured: 24px / 160px
        WHEN 2 THEN 0.1429  -- EventDetailHero: 50px / 350px
    END as [SizeWidthRatio],
    CASE loc.[Location]
        WHEN 0 THEN 0.1875  -- EventsListing: 27px / 144px
        WHEN 1 THEN 0.2000  -- HomeFeatured: 24px / 120px
        WHEN 2 THEN 0.1736  -- EventDetailHero: 50px / 288px
    END as [SizeHeightRatio],
    1 as [LockAspectRatio],
    1 as [IsEnabled],
    GETUTCDATE() as [CreatedAt],
    GETUTCDATE() as [UpdatedAt]
FROM [Badges] b
CROSS JOIN (
    SELECT 0 as [Location]  -- EventsListing
    UNION SELECT 1          -- HomeFeatured
    UNION SELECT 2          -- EventDetailHero
) loc
WHERE b.[IsDeleted] = 0;  -- Only migrate active badges

-- Step 5: Mark Position column as deprecated (but keep for rollback safety)
-- In future migration, after confirming data integrity, we can drop this column
-- ALTER TABLE [Badges] DROP COLUMN [Position];

PRINT 'Phase 6A.31: BadgeLocationConfig table created and data migrated successfully';
PRINT 'Total location configs created: ' + CAST(@@ROWCOUNT AS varchar);
```

### 4.2 EF Core Configuration

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Badges.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class BadgeLocationConfigConfiguration : IEntityTypeConfiguration<BadgeLocationConfig>
{
    public void Configure(EntityTypeBuilder<BadgeLocationConfig> builder)
    {
        builder.ToTable("BadgeLocationConfigs");

        builder.HasKey(c => c.Id);

        // Foreign key to Badge
        builder.HasOne(c => c.Badge)
            .WithMany(b => b.LocationConfigs)
            .HasForeignKey(c => c.BadgeId)
            .OnDelete(DeleteBehavior.Cascade);  // Delete configs when badge is deleted

        // Unique constraint: One config per badge per location
        builder.HasIndex(c => new { c.BadgeId, c.Location })
            .IsUnique()
            .HasDatabaseName("UQ_BadgeLocationConfigs_BadgeId_Location");

        // Index for common query pattern
        builder.HasIndex(c => new { c.BadgeId, c.Location })
            .HasDatabaseName("IX_BadgeLocationConfigs_BadgeId_Location")
            .IncludeProperties(c => c.IsEnabled);

        // Property configurations
        builder.Property(c => c.Location)
            .IsRequired()
            .HasConversion<int>();  // Store enum as int

        builder.Property(c => c.AnchorCorner)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.OffsetXRatio)
            .IsRequired()
            .HasColumnType("decimal(5,4)")
            .HasDefaultValue(0.0m);

        builder.Property(c => c.OffsetYRatio)
            .IsRequired()
            .HasColumnType("decimal(5,4)")
            .HasDefaultValue(0.0m);

        builder.Property(c => c.SizeWidthRatio)
            .IsRequired()
            .HasColumnType("decimal(5,4)");

        builder.Property(c => c.SizeHeightRatio)
            .IsRequired()
            .HasColumnType("decimal(5,4)");

        builder.Property(c => c.LockAspectRatio)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(c => c.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
    }
}

public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        // ... existing configurations ...

        // Phase 6A.31: Mark Position as deprecated but keep for backward compatibility
        builder.Property(b => b.Position)
            .IsRequired(false)  // Now nullable
            .HasConversion<int?>();

        // One-to-many with location configs
        builder.HasMany(b => b.LocationConfigs)
            .WithOne(c => c.Badge)
            .HasForeignKey(c => c.BadgeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

## 5. API Contract Design

### 5.1 DTOs

```csharp
namespace LankaConnect.Application.Badges.Common;

/// <summary>
/// Badge location configuration for API responses
/// Phase 6A.31
/// </summary>
public class BadgeLocationConfigDto
{
    public Guid Id { get; set; }
    public DisplayLocation Location { get; set; }
    public BadgeCorner AnchorCorner { get; set; }
    public decimal OffsetXRatio { get; set; }
    public decimal OffsetYRatio { get; set; }
    public decimal SizeWidthRatio { get; set; }
    public decimal SizeHeightRatio { get; set; }
    public bool LockAspectRatio { get; set; }
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Badge details for API responses
/// Phase 6A.31: Added LocationConfigs
/// </summary>
public class BadgeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }
    public int DisplayOrder { get; set; }
    public int? DefaultDurationDays { get; set; }

    /// <summary>
    /// Per-location display configurations (Phase 6A.31)
    /// </summary>
    public List<BadgeLocationConfigDto> LocationConfigs { get; set; } = new();

    /// <summary>
    /// DEPRECATED: Use LocationConfigs instead
    /// Kept for backward compatibility with pre-6A.31 clients
    /// </summary>
    [Obsolete("Use LocationConfigs instead")]
    public BadgePosition? Position { get; set; }
}

/// <summary>
/// Request to create badge location configuration
/// </summary>
public class CreateBadgeLocationConfigDto
{
    public DisplayLocation Location { get; set; }
    public BadgeCorner AnchorCorner { get; set; }
    public decimal OffsetXRatio { get; set; }
    public decimal OffsetYRatio { get; set; }
    public decimal SizeWidthRatio { get; set; }
    public decimal SizeHeightRatio { get; set; }
    public bool LockAspectRatio { get; set; }
    public bool IsEnabled { get; set; } = true;
}
```

### 5.2 Commands

```csharp
using MediatR;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Badges.Enums;

namespace LankaConnect.Application.Badges.Commands.CreateBadge;

/// <summary>
/// Command to create a new badge with per-location configurations
/// Phase 6A.31: Replaced Position with LocationConfigs
/// </summary>
public record CreateBadgeCommand : IRequest<Result<Guid>>
{
    public string Name { get; init; } = string.Empty;
    public IFormFile ImageFile { get; init; } = null!;
    public int DisplayOrder { get; init; }
    public Guid CreatedByUserId { get; init; }
    public int? DefaultDurationDays { get; init; }

    /// <summary>
    /// Per-location display configurations (Phase 6A.31)
    /// If empty, default configs will be created for all locations
    /// </summary>
    public List<CreateBadgeLocationConfigDto> LocationConfigs { get; init; } = new();
}

public class CreateBadgeCommandValidator : AbstractValidator<CreateBadgeCommand>
{
    public CreateBadgeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Badge name is required")
            .MaximumLength(50).WithMessage("Badge name cannot exceed 50 characters");

        RuleFor(x => x.ImageFile)
            .NotNull().WithMessage("Badge image is required")
            .Must(file => file.ContentType.StartsWith("image/"))
                .WithMessage("File must be an image");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be non-negative");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("Creator user ID is required");

        RuleFor(x => x.DefaultDurationDays)
            .GreaterThan(0).When(x => x.DefaultDurationDays.HasValue)
            .WithMessage("Duration must be a positive number of days");

        // Validate location configs
        RuleForEach(x => x.LocationConfigs).SetValidator(new BadgeLocationConfigValidator());

        // Validate no duplicate locations
        RuleFor(x => x.LocationConfigs)
            .Must(configs => configs.Select(c => c.Location).Distinct().Count() == configs.Count)
            .When(x => x.LocationConfigs.Any())
            .WithMessage("Cannot have multiple configurations for the same location");
    }
}

public class BadgeLocationConfigValidator : AbstractValidator<CreateBadgeLocationConfigDto>
{
    public BadgeLocationConfigValidator()
    {
        RuleFor(x => x.OffsetXRatio)
            .InclusiveBetween(0m, 1m).WithMessage("Offset X ratio must be between 0 and 1");

        RuleFor(x => x.OffsetYRatio)
            .InclusiveBetween(0m, 1m).WithMessage("Offset Y ratio must be between 0 and 1");

        RuleFor(x => x.SizeWidthRatio)
            .GreaterThan(0m).WithMessage("Size width ratio must be greater than 0")
            .LessThanOrEqualTo(1m).WithMessage("Size width ratio must be less than or equal to 1")
            .GreaterThanOrEqualTo(0.01m).WithMessage("Badge must be at least 1% of container width");

        RuleFor(x => x.SizeHeightRatio)
            .GreaterThan(0m).WithMessage("Size height ratio must be greater than 0")
            .LessThanOrEqualTo(1m).WithMessage("Size height ratio must be less than or equal to 1")
            .GreaterThanOrEqualTo(0.01m).WithMessage("Badge must be at least 1% of container height");
    }
}

namespace LankaConnect.Application.Badges.Commands.UpdateBadge;

/// <summary>
/// Command to update badge location configurations
/// Phase 6A.31
/// </summary>
public record UpdateBadgeLocationConfigsCommand : IRequest<Result>
{
    public Guid BadgeId { get; init; }
    public List<CreateBadgeLocationConfigDto> LocationConfigs { get; init; } = new();
}
```

### 5.3 Command Handlers

```csharp
using MediatR;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Badges.Repositories;
using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Badges.Commands.CreateBadge;

public class CreateBadgeCommandHandler : IRequestHandler<CreateBadgeCommand, Result<Guid>>
{
    private readonly IBadgeRepository _badgeRepository;
    private readonly IImageUploadService _imageUploadService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBadgeCommandHandler(
        IBadgeRepository badgeRepository,
        IImageUploadService imageUploadService,
        IUnitOfWork unitOfWork)
    {
        _badgeRepository = badgeRepository;
        _imageUploadService = imageUploadService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateBadgeCommand request, CancellationToken cancellationToken)
    {
        // Upload image
        var uploadResult = await _imageUploadService.UploadBadgeImageAsync(
            request.ImageFile,
            cancellationToken);

        if (uploadResult.IsFailure)
            return Result<Guid>.Failure(uploadResult.Error);

        // Create badge entity
        var badgeResult = Badge.Create(
            request.Name,
            uploadResult.Value.Url,
            uploadResult.Value.BlobName,
            request.DisplayOrder,
            request.CreatedByUserId,
            locationConfigs: null,  // Add configs after badge creation
            request.DefaultDurationDays);

        if (badgeResult.IsFailure)
        {
            // Cleanup uploaded image
            await _imageUploadService.DeleteBadgeImageAsync(uploadResult.Value.BlobName, cancellationToken);
            return Result<Guid>.Failure(badgeResult.Error);
        }

        var badge = badgeResult.Value;

        // Create location configs
        if (request.LocationConfigs.Any())
        {
            // Use user-provided configs
            foreach (var configDto in request.LocationConfigs)
            {
                var configResult = BadgeLocationConfig.Create(
                    badge.Id,
                    configDto.Location,
                    configDto.AnchorCorner,
                    configDto.OffsetXRatio,
                    configDto.OffsetYRatio,
                    configDto.SizeWidthRatio,
                    configDto.SizeHeightRatio,
                    configDto.LockAspectRatio,
                    configDto.IsEnabled);

                if (configResult.IsFailure)
                {
                    await _imageUploadService.DeleteBadgeImageAsync(uploadResult.Value.BlobName, cancellationToken);
                    return Result<Guid>.Failure(configResult.Error);
                }

                badge.AddOrUpdateLocationConfig(configResult.Value);
            }
        }
        else
        {
            // Create default configs for all locations
            var defaultResult = badge.CreateDefaultLocationConfigs();
            if (defaultResult.IsFailure)
            {
                await _imageUploadService.DeleteBadgeImageAsync(uploadResult.Value.BlobName, cancellationToken);
                return Result<Guid>.Failure(defaultResult.Error);
            }
        }

        // Save to database
        await _badgeRepository.AddAsync(badge);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(badge.Id);
    }
}
```

### 5.4 API Endpoints

```csharp
using Microsoft.AspNetCore.Mvc;
using MediatR;
using LankaConnect.Application.Badges.Commands.CreateBadge;
using LankaConnect.Application.Badges.Queries.GetBadges;

namespace LankaConnect.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BadgesController : ControllerBase
{
    private readonly IMediator _mediator;

    public BadgesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new badge with per-location configurations
    /// Phase 6A.31: Added locationConfigs parameter
    /// </summary>
    /// <param name="request">Badge creation data including optional location configs</param>
    /// <returns>ID of created badge</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBadge([FromForm] CreateBadgeRequest request)
    {
        var command = new CreateBadgeCommand
        {
            Name = request.Name,
            ImageFile = request.ImageFile,
            DisplayOrder = request.DisplayOrder,
            CreatedByUserId = request.CreatedByUserId,
            DefaultDurationDays = request.DefaultDurationDays,
            LocationConfigs = request.LocationConfigs ?? new()
        };

        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(
            nameof(GetBadge),
            new { id = result.Value },
            new { id = result.Value });
    }

    /// <summary>
    /// Gets badge details including all location configurations
    /// Phase 6A.31: Response includes LocationConfigs array
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BadgeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBadge(Guid id)
    {
        var query = new GetBadgeQuery { BadgeId = id };
        var result = await _mediator.Send(query);

        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Updates badge location configurations
    /// Phase 6A.31: New endpoint
    /// </summary>
    [HttpPut("{id:guid}/location-configs")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLocationConfigs(
        Guid id,
        [FromBody] List<CreateBadgeLocationConfigDto> locationConfigs)
    {
        var command = new UpdateBadgeLocationConfigsCommand
        {
            BadgeId = id,
            LocationConfigs = locationConfigs
        };

        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }
}

public record CreateBadgeRequest
{
    public string Name { get; init; } = string.Empty;
    public IFormFile ImageFile { get; init; } = null!;
    public int DisplayOrder { get; init; }
    public Guid CreatedByUserId { get; init; }
    public int? DefaultDurationDays { get; init; }

    /// <summary>
    /// Optional: Per-location configurations
    /// If not provided, default configs created for all locations
    /// </summary>
    public List<CreateBadgeLocationConfigDto>? LocationConfigs { get; init; }
}
```

---

## 6. Calculation Engine

### 6.1 TypeScript Calculation Functions

```typescript
// src/infrastructure/utils/badgePositioning.ts

/**
 * Phase 6A.31: Badge positioning and scaling calculation engine
 * Converts ratio-based configurations to pixel coordinates for responsive rendering
 */

import { DisplayLocation, BadgeCorner } from '@/infrastructure/api/types/badge.types';

/**
 * Container dimensions for each display location
 * Based on Tailwind CSS utility classes used in layout
 */
export const DISPLAY_LOCATION_DIMENSIONS: Record<DisplayLocation, { width: number; height: number }> = {
  [DisplayLocation.EventsListing]: { width: 192, height: 144 },      // h-48 on event cards
  [DisplayLocation.HomeFeatured]: { width: 160, height: 120 },       // h-40 on hero banner (approximate for variable width)
  [DisplayLocation.EventDetailHero]: { width: 384, height: 288 }     // h-96 on detail page
};

/**
 * Badge location configuration from API
 */
export interface BadgeLocationConfig {
  location: DisplayLocation;
  anchorCorner: BadgeCorner;
  offsetXRatio: number;  // 0.0 - 1.0
  offsetYRatio: number;  // 0.0 - 1.0
  sizeWidthRatio: number;  // 0.0 - 1.0
  sizeHeightRatio: number;  // 0.0 - 1.0
  lockAspectRatio: boolean;
  isEnabled: boolean;
}

/**
 * Calculated pixel dimensions for rendering
 */
export interface BadgePixelLayout {
  x: number;
  y: number;
  width: number;
  height: number;
}

/**
 * Calculates badge pixel layout from ratio-based configuration
 *
 * @param containerWidth - Actual container width in pixels
 * @param containerHeight - Actual container height in pixels
 * @param config - Badge location configuration with ratios
 * @returns Pixel coordinates and dimensions for rendering
 *
 * @example
 * // Badge configured for Event Detail (384x288):
 * // Size: 50x50px (14.3% width, 17.4% height), Top Right, 0px offset
 * const config = {
 *   anchorCorner: BadgeCorner.TopRight,
 *   offsetXRatio: 0.0,
 *   offsetYRatio: 0.0,
 *   sizeWidthRatio: 0.143,
 *   sizeHeightRatio: 0.174,
 *   // ...
 * };
 *
 * // On mobile (100x80):
 * const layout = calculateBadgeLayout(100, 80, config);
 * // Returns: { x: 86, y: 0, width: 14, height: 14 }
 */
export function calculateBadgeLayout(
  containerWidth: number,
  containerHeight: number,
  config: BadgeLocationConfig
): BadgePixelLayout {
  // Calculate badge size first (needed for right/bottom anchor calculations)
  const badgeWidth = Math.round(containerWidth * config.sizeWidthRatio);
  const badgeHeight = Math.round(containerHeight * config.sizeHeightRatio);

  let x = 0;
  let y = 0;

  // Calculate position based on anchor corner
  switch (config.anchorCorner) {
    case BadgeCorner.TopLeft:
      // Offset is from top-left corner
      x = Math.round(containerWidth * config.offsetXRatio);
      y = Math.round(containerHeight * config.offsetYRatio);
      break;

    case BadgeCorner.TopRight:
      // Offset is from top-right corner (inward)
      x = Math.round(containerWidth * (1 - config.offsetXRatio)) - badgeWidth;
      y = Math.round(containerHeight * config.offsetYRatio);
      break;

    case BadgeCorner.BottomLeft:
      // Offset is from bottom-left corner (upward and rightward)
      x = Math.round(containerWidth * config.offsetXRatio);
      y = Math.round(containerHeight * (1 - config.offsetYRatio)) - badgeHeight;
      break;

    case BadgeCorner.BottomRight:
      // Offset is from bottom-right corner (inward and upward)
      x = Math.round(containerWidth * (1 - config.offsetXRatio)) - badgeWidth;
      y = Math.round(containerHeight * (1 - config.offsetYRatio)) - badgeHeight;
      break;

    default:
      console.error(`Unknown anchor corner: ${config.anchorCorner}`);
      // Fallback to top-left
      x = 0;
      y = 0;
  }

  // Ensure badge stays within container bounds (safety check)
  x = Math.max(0, Math.min(x, containerWidth - badgeWidth));
  y = Math.max(0, Math.min(y, containerHeight - badgeHeight));

  return {
    x,
    y,
    width: badgeWidth,
    height: badgeHeight
  };
}

/**
 * Converts pixel offset to ratio based on container dimensions and anchor corner
 * Used when user drags badge in UI - converts pixel position to storable ratio
 *
 * @param pixelX - Badge x position in pixels
 * @param pixelY - Badge y position in pixels
 * @param badgeWidth - Badge width in pixels
 * @param badgeHeight - Badge height in pixels
 * @param containerWidth - Container width in pixels
 * @param containerHeight - Container height in pixels
 * @param anchorCorner - Which corner badge is anchored to
 * @returns Offset ratios for storage
 */
export function pixelToRatio(
  pixelX: number,
  pixelY: number,
  badgeWidth: number,
  badgeHeight: number,
  containerWidth: number,
  containerHeight: number,
  anchorCorner: BadgeCorner
): { offsetXRatio: number; offsetYRatio: number } {
  let offsetXRatio = 0;
  let offsetYRatio = 0;

  switch (anchorCorner) {
    case BadgeCorner.TopLeft:
      offsetXRatio = pixelX / containerWidth;
      offsetYRatio = pixelY / containerHeight;
      break;

    case BadgeCorner.TopRight:
      // Calculate inward offset from right edge
      offsetXRatio = (containerWidth - pixelX - badgeWidth) / containerWidth;
      offsetYRatio = pixelY / containerHeight;
      break;

    case BadgeCorner.BottomLeft:
      offsetXRatio = pixelX / containerWidth;
      // Calculate upward offset from bottom edge
      offsetYRatio = (containerHeight - pixelY - badgeHeight) / containerHeight;
      break;

    case BadgeCorner.BottomRight:
      offsetXRatio = (containerWidth - pixelX - badgeWidth) / containerWidth;
      offsetYRatio = (containerHeight - pixelY - badgeHeight) / containerHeight;
      break;
  }

  // Clamp to valid range [0, 1]
  offsetXRatio = Math.max(0, Math.min(1, offsetXRatio));
  offsetYRatio = Math.max(0, Math.min(1, offsetYRatio));

  return { offsetXRatio, offsetYRatio };
}

/**
 * Converts pixel size to ratio based on container dimensions
 *
 * @param pixelWidth - Badge width in pixels
 * @param pixelHeight - Badge height in pixels
 * @param containerWidth - Container width in pixels
 * @param containerHeight - Container height in pixels
 * @returns Size ratios for storage
 */
export function pixelSizeToRatio(
  pixelWidth: number,
  pixelHeight: number,
  containerWidth: number,
  containerHeight: number
): { sizeWidthRatio: number; sizeHeightRatio: number } {
  const sizeWidthRatio = Math.max(0.01, Math.min(1, pixelWidth / containerWidth));
  const sizeHeightRatio = Math.max(0.01, Math.min(1, pixelHeight / containerHeight));

  return { sizeWidthRatio, sizeHeightRatio };
}

/**
 * Maintains aspect ratio when resizing badge
 *
 * @param newWidth - New width in pixels
 * @param originalWidth - Original width in pixels
 * @param originalHeight - Original height in pixels
 * @param lockAspectRatio - Whether to maintain aspect ratio
 * @returns Adjusted height to maintain aspect ratio
 */
export function calculateAspectRatioHeight(
  newWidth: number,
  originalWidth: number,
  originalHeight: number,
  lockAspectRatio: boolean
): number {
  if (!lockAspectRatio) return originalHeight;

  const aspectRatio = originalWidth / originalHeight;
  return Math.round(newWidth / aspectRatio);
}

/**
 * Gets the configuration for a specific location from badge location configs
 *
 * @param locationConfigs - Array of badge location configurations
 * @param location - Target display location
 * @returns Config for location or null if not found
 */
export function getLocationConfig(
  locationConfigs: BadgeLocationConfig[],
  location: DisplayLocation
): BadgeLocationConfig | null {
  return locationConfigs.find(c => c.location === location && c.isEnabled) || null;
}

/**
 * Validates that badge fits within container bounds
 *
 * @param layout - Calculated badge pixel layout
 * @param containerWidth - Container width in pixels
 * @param containerHeight - Container height in pixels
 * @returns True if badge fits within bounds
 */
export function isValidBadgeLayout(
  layout: BadgePixelLayout,
  containerWidth: number,
  containerHeight: number
): boolean {
  return (
    layout.x >= 0 &&
    layout.y >= 0 &&
    layout.x + layout.width <= containerWidth &&
    layout.y + layout.height <= containerHeight &&
    layout.width > 0 &&
    layout.height > 0
  );
}
```

---

## 7. UI Component Architecture

### 7.1 Component Hierarchy

```typescript
// Component structure for badge customization dialog
<BadgeCustomizationDialog>
  ├── <DialogHeader>
  │   └── "Badge Customization"
  │
  ├── <TabGroup currentTab={activeTab} onTabChange={setActiveTab}>
  │   ├── <Tab id="events-listing" label="📋 Events Listing">
  │   ├── <Tab id="home-featured" label="🏠 Home Featured">
  │   └── <Tab id="event-detail" label="🎯 Event Detail">
  │
  ├── <TabPanel value="events-listing">
  │   ├── <BadgeCanvasEditor>
  │   │   ├── <MockEventCard dimensions={{w:192, h:144}}>
  │   │   │   └── <DraggableBadge>
  │   │   │       └── <ResizeHandles />
  │   │   └── <PositionGuides />
  │   │
  │   ├── <BadgePositionControls>
  │   │   ├── <Select> Anchor Corner
  │   │   ├── <Input> X Offset (px)
  │   │   └── <Input> Y Offset (px)
  │   │
  │   ├── <BadgeSizeControls>
  │   │   ├── <Input> Width (px)
  │   │   ├── <Input> Height (px)
  │   │   └── <Checkbox> Lock Aspect Ratio
  │   │
  │   └── <ConfigPreview>
  │       └── "Badge will be 14.0% × 18.8% of container"
  │
  ├── <TabPanel value="home-featured">
  │   └── [Same structure as events-listing]
  │
  ├── <TabPanel value="event-detail">
  │   └── [Same structure as events-listing]
  │
  ├── <QuickActions>
  │   ├── <Button onClick={copyFromEventDetail}>
  │   │   └── "Copy from Event Detail"
  │   ├── <Button onClick={resetToDefaults}>
  │   │   └── "Reset to Defaults"
  │   └── <Checkbox checked={useSameForAll}>
  │       └── "Use same config for all locations"
  │
  └── <DialogFooter>
      ├── <Button variant="outline" onClick={onCancel}>Cancel
      └── <Button onClick={handleSave}>Save Badge
```

### 7.2 Key Components

```typescript
// src/presentation/components/features/badges/BadgeCanvasEditor.tsx

import React, { useRef, useState, useCallback } from 'react';
import { BadgeLocationConfig, DisplayLocation, BadgeCorner } from '@/infrastructure/api/types/badge.types';
import { calculateBadgeLayout, pixelToRatio, pixelSizeToRatio } from '@/infrastructure/utils/badgePositioning';

interface BadgeCanvasEditorProps {
  badgeImageUrl: string;
  containerDimensions: { width: number; height: number };
  config: BadgeLocationConfig;
  onConfigChange: (config: Partial<BadgeLocationConfig>) => void;
}

/**
 * Interactive canvas for dragging and resizing badge preview
 * Phase 6A.31
 */
export function BadgeCanvasEditor({
  badgeImageUrl,
  containerDimensions,
  config,
  onConfigChange
}: BadgeCanvasEditorProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [isResizing, setIsResizing] = useState(false);
  const [dragStart, setDragStart] = useState({ x: 0, y: 0 });

  // Calculate current badge layout
  const layout = calculateBadgeLayout(
    containerDimensions.width,
    containerDimensions.height,
    config
  );

  // Handle drag start
  const handleMouseDown = useCallback((e: React.MouseEvent) => {
    if (!containerRef.current) return;

    const rect = containerRef.current.getBoundingClientRect();
    setIsDragging(true);
    setDragStart({
      x: e.clientX - rect.left - layout.x,
      y: e.clientY - rect.top - layout.y
    });
  }, [layout.x, layout.y]);

  // Handle drag move
  const handleMouseMove = useCallback((e: React.MouseEvent) => {
    if (!isDragging || !containerRef.current) return;

    const rect = containerRef.current.getBoundingClientRect();
    const newX = e.clientX - rect.left - dragStart.x;
    const newY = e.clientY - rect.top - dragStart.y;

    // Convert pixel position to ratio
    const { offsetXRatio, offsetYRatio } = pixelToRatio(
      newX,
      newY,
      layout.width,
      layout.height,
      containerDimensions.width,
      containerDimensions.height,
      config.anchorCorner
    );

    onConfigChange({ offsetXRatio, offsetYRatio });
  }, [isDragging, dragStart, layout.width, layout.height, containerDimensions, config.anchorCorner, onConfigChange]);

  // Handle drag end
  const handleMouseUp = useCallback(() => {
    setIsDragging(false);
    setIsResizing(false);
  }, []);

  // Handle resize (simplified - would need 8 resize handles in real implementation)
  const handleResize = useCallback((newWidth: number, newHeight: number) => {
    const { sizeWidthRatio, sizeHeightRatio } = pixelSizeToRatio(
      newWidth,
      newHeight,
      containerDimensions.width,
      containerDimensions.height
    );

    onConfigChange({ sizeWidthRatio, sizeHeightRatio });
  }, [containerDimensions, onConfigChange]);

  return (
    <div className="relative">
      {/* Mock event card container */}
      <div
        ref={containerRef}
        className="relative bg-gray-100 rounded-lg overflow-hidden cursor-move"
        style={{
          width: containerDimensions.width,
          height: containerDimensions.height
        }}
        onMouseMove={handleMouseMove}
        onMouseUp={handleMouseUp}
        onMouseLeave={handleMouseUp}
      >
        {/* Placeholder event image */}
        <div className="w-full h-full bg-gradient-to-br from-blue-200 to-purple-200 flex items-center justify-center text-gray-500">
          Mock Event Image
        </div>

        {/* Draggable badge */}
        <div
          className={`absolute cursor-move ${isDragging ? 'opacity-70' : ''}`}
          style={{
            left: layout.x,
            top: layout.y,
            width: layout.width,
            height: layout.height
          }}
          onMouseDown={handleMouseDown}
        >
          <img
            src={badgeImageUrl}
            alt="Badge"
            className="w-full h-full object-contain pointer-events-none"
            draggable={false}
          />

          {/* Resize handles (8 total: corners + midpoints) */}
          {!isDragging && (
            <>
              {/* Top-left corner */}
              <div
                className="absolute -top-1 -left-1 w-3 h-3 bg-blue-500 rounded-full cursor-nw-resize"
                onMouseDown={(e) => {
                  e.stopPropagation();
                  setIsResizing(true);
                }}
              />
              {/* Top-right corner */}
              <div
                className="absolute -top-1 -right-1 w-3 h-3 bg-blue-500 rounded-full cursor-ne-resize"
                onMouseDown={(e) => {
                  e.stopPropagation();
                  setIsResizing(true);
                }}
              />
              {/* Bottom-left corner */}
              <div
                className="absolute -bottom-1 -left-1 w-3 h-3 bg-blue-500 rounded-full cursor-sw-resize"
                onMouseDown={(e) => {
                  e.stopPropagation();
                  setIsResizing(true);
                }}
              />
              {/* Bottom-right corner */}
              <div
                className="absolute -bottom-1 -right-1 w-3 h-3 bg-blue-500 rounded-full cursor-se-resize"
                onMouseDown={(e) => {
                  e.stopPropagation();
                  setIsResizing(true);
                }}
              />
            </>
          )}
        </div>

        {/* Position guides (grid lines) */}
        <div className="absolute inset-0 pointer-events-none">
          {/* Vertical center line */}
          <div className="absolute left-1/2 top-0 bottom-0 w-px bg-blue-300 opacity-30" />
          {/* Horizontal center line */}
          <div className="absolute top-1/2 left-0 right-0 h-px bg-blue-300 opacity-30" />
        </div>
      </div>

      {/* Dimensions display */}
      <div className="mt-2 text-sm text-gray-600">
        Container: {containerDimensions.width}×{containerDimensions.height}px |
        Badge: {layout.width}×{layout.height}px ({(config.sizeWidthRatio * 100).toFixed(1)}% × {(config.sizeHeightRatio * 100).toFixed(1)}%)
      </div>
    </div>
  );
}
```

```typescript
// src/presentation/components/features/badges/BadgePositionControls.tsx

import React from 'react';
import { BadgeLocationConfig, BadgeCorner } from '@/infrastructure/api/types/badge.types';
import { Select, Input } from '@/presentation/components/ui';

interface BadgePositionControlsProps {
  config: BadgeLocationConfig;
  containerDimensions: { width: number; height: number };
  onConfigChange: (config: Partial<BadgeLocationConfig>) => void;
}

/**
 * Precise position controls with pixel/ratio conversion
 * Phase 6A.31
 */
export function BadgePositionControls({
  config,
  containerDimensions,
  onConfigChange
}: BadgePositionControlsProps) {
  // Convert ratio to pixels for display
  const offsetXPixels = Math.round(containerDimensions.width * config.offsetXRatio);
  const offsetYPixels = Math.round(containerDimensions.height * config.offsetYRatio);

  // Handle pixel input change (convert to ratio)
  const handleOffsetXChange = (pixels: number) => {
    const offsetXRatio = Math.max(0, Math.min(1, pixels / containerDimensions.width));
    onConfigChange({ offsetXRatio });
  };

  const handleOffsetYChange = (pixels: number) => {
    const offsetYRatio = Math.max(0, Math.min(1, pixels / containerDimensions.height));
    onConfigChange({ offsetYRatio });
  };

  return (
    <div className="space-y-4">
      <h3 className="font-semibold text-sm">Position</h3>

      {/* Anchor corner selection */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Anchor Corner
        </label>
        <Select
          value={config.anchorCorner.toString()}
          onValueChange={(value) => onConfigChange({ anchorCorner: parseInt(value) as BadgeCorner })}
        >
          <option value={BadgeCorner.TopLeft}>Top Left</option>
          <option value={BadgeCorner.TopRight}>Top Right</option>
          <option value={BadgeCorner.BottomLeft}>Bottom Left</option>
          <option value={BadgeCorner.BottomRight}>Bottom Right</option>
        </Select>
      </div>

      {/* Offset inputs */}
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            X Offset
          </label>
          <div className="flex items-center space-x-2">
            <Input
              type="number"
              min={0}
              max={containerDimensions.width}
              value={offsetXPixels}
              onChange={(e) => handleOffsetXChange(parseInt(e.target.value) || 0)}
              className="flex-1"
            />
            <span className="text-sm text-gray-500">px</span>
          </div>
          <p className="text-xs text-gray-500 mt-1">
            {(config.offsetXRatio * 100).toFixed(1)}% of container
          </p>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Y Offset
          </label>
          <div className="flex items-center space-x-2">
            <Input
              type="number"
              min={0}
              max={containerDimensions.height}
              value={offsetYPixels}
              onChange={(e) => handleOffsetYChange(parseInt(e.target.value) || 0)}
              className="flex-1"
            />
            <span className="text-sm text-gray-500">px</span>
          </div>
          <p className="text-xs text-gray-500 mt-1">
            {(config.offsetYRatio * 100).toFixed(1)}% of container
          </p>
        </div>
      </div>
    </div>
  );
}
```

---

## 8. Migration Strategy

### 8.1 Migration Phases

**Phase 1: Schema Migration (Safe - No Breaking Changes)**
- Create `BadgeLocationConfigs` table
- Add foreign key to `Badges` table
- Keep existing `Position` column (mark as deprecated but functional)
- Migrate existing badges to location configs using defaults

**Phase 2: API Migration (Backward Compatible)**
- Add `LocationConfigs` array to `BadgeDto`
- Keep deprecated `Position` field in responses
- Accept both old and new request formats
- Frontend sends `LocationConfigs`, backend populates legacy `Position`

**Phase 3: Frontend Migration (Progressive Enhancement)**
- Update badge creation dialog to new UI (tabs with canvas)
- Update badge display components to use `LocationConfigs`
- Fallback to legacy `Position` if `LocationConfigs` empty
- Deploy frontend changes

**Phase 4: Deprecation Cleanup (Future Phase 6A.32)**
- Remove `Position` column from `Badges` table
- Remove `Position` from DTOs and API responses
- Remove legacy fallback code
- Update documentation

### 8.2 Migration Script

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.31: Badge Positioning and Scaling System
    /// Creates BadgeLocationConfig table and migrates existing Position data
    /// </summary>
    public partial class AddBadgeLocationConfigsPhase6A31 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create BadgeLocationConfigs table
            migrationBuilder.CreateTable(
                name: "BadgeLocationConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    BadgeId = table.Column<Guid>(nullable: false),
                    Location = table.Column<int>(nullable: false),
                    AnchorCorner = table.Column<int>(nullable: false),
                    OffsetXRatio = table.Column<decimal>(type: "decimal(5,4)", nullable: false, defaultValue: 0.0m),
                    OffsetYRatio = table.Column<decimal>(type: "decimal(5,4)", nullable: false, defaultValue: 0.0m),
                    SizeWidthRatio = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    SizeHeightRatio = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    LockAspectRatio = table.Column<bool>(nullable: false, defaultValue: true),
                    IsEnabled = table.Column<bool>(nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeLocationConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BadgeLocationConfigs_Badges_BadgeId",
                        column: x => x.BadgeId,
                        principalTable: "Badges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Step 2: Create unique index
            migrationBuilder.CreateIndex(
                name: "UQ_BadgeLocationConfigs_BadgeId_Location",
                table: "BadgeLocationConfigs",
                columns: new[] { "BadgeId", "Location" },
                unique: true);

            // Step 3: Create covering index for common queries
            migrationBuilder.CreateIndex(
                name: "IX_BadgeLocationConfigs_BadgeId_Location",
                table: "BadgeLocationConfigs",
                columns: new[] { "BadgeId", "Location" })
                .Annotation("SqlServer:Include", new[] { "IsEnabled" });

            // Step 4: Add check constraints
            migrationBuilder.Sql(@"
                ALTER TABLE [BadgeLocationConfigs]
                ADD CONSTRAINT [CK_BadgeLocationConfigs_OffsetXRatio]
                CHECK ([OffsetXRatio] >= 0.0 AND [OffsetXRatio] <= 1.0);

                ALTER TABLE [BadgeLocationConfigs]
                ADD CONSTRAINT [CK_BadgeLocationConfigs_OffsetYRatio]
                CHECK ([OffsetYRatio] >= 0.0 AND [OffsetYRatio] <= 1.0);

                ALTER TABLE [BadgeLocationConfigs]
                ADD CONSTRAINT [CK_BadgeLocationConfigs_SizeWidthRatio]
                CHECK ([SizeWidthRatio] > 0.0 AND [SizeWidthRatio] <= 1.0);

                ALTER TABLE [BadgeLocationConfigs]
                ADD CONSTRAINT [CK_BadgeLocationConfigs_SizeHeightRatio]
                CHECK ([SizeHeightRatio] > 0.0 AND [SizeHeightRatio] <= 1.0);
            ");

            // Step 5: Migrate existing badges
            // Create 3 location configs for each badge (one per DisplayLocation)
            migrationBuilder.Sql(@"
                INSERT INTO [BadgeLocationConfigs] (
                    [Id],
                    [BadgeId],
                    [Location],
                    [AnchorCorner],
                    [OffsetXRatio],
                    [OffsetYRatio],
                    [SizeWidthRatio],
                    [SizeHeightRatio],
                    [LockAspectRatio],
                    [IsEnabled],
                    [CreatedAt],
                    [UpdatedAt]
                )
                SELECT
                    NEWID() as [Id],
                    b.[Id] as [BadgeId],
                    loc.[Location],
                    ISNULL(b.[Position], 1) as [AnchorCorner],  -- Default to TopRight (1)
                    0.0 as [OffsetXRatio],
                    0.0 as [OffsetYRatio],
                    CASE loc.[Location]
                        WHEN 0 THEN 0.1406  -- EventsListing: 27px / 192px
                        WHEN 1 THEN 0.1500  -- HomeFeatured: 24px / 160px
                        WHEN 2 THEN 0.1429  -- EventDetailHero: 50px / 350px
                    END as [SizeWidthRatio],
                    CASE loc.[Location]
                        WHEN 0 THEN 0.1875  -- EventsListing: 27px / 144px
                        WHEN 1 THEN 0.2000  -- HomeFeatured: 24px / 120px
                        WHEN 2 THEN 0.1736  -- EventDetailHero: 50px / 288px
                    END as [SizeHeightRatio],
                    1 as [LockAspectRatio],
                    1 as [IsEnabled],
                    GETUTCDATE() as [CreatedAt],
                    GETUTCDATE() as [UpdatedAt]
                FROM [Badges] b
                CROSS JOIN (
                    SELECT 0 as [Location]  -- EventsListing
                    UNION SELECT 1          -- HomeFeatured
                    UNION SELECT 2          -- EventDetailHero
                ) loc
                WHERE b.[IsDeleted] = 0;  -- Only migrate active badges
            ");

            // Step 6: Make Position nullable (for future removal)
            migrationBuilder.AlterColumn<int>(
                name: "Position",
                table: "Badges",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore Position to non-nullable
            migrationBuilder.AlterColumn<int>(
                name: "Position",
                table: "Badges",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            // Drop BadgeLocationConfigs table (cascade deletes records)
            migrationBuilder.DropTable(name: "BadgeLocationConfigs");
        }
    }
}
```

### 8.3 Data Validation Script

```sql
-- Phase 6A.31: Validation script to verify migration success

-- 1. Check all badges have location configs
SELECT
    b.Id,
    b.Name,
    COUNT(blc.Id) as ConfigCount,
    CASE
        WHEN COUNT(blc.Id) = 3 THEN 'OK'
        ELSE 'MISSING CONFIGS'
    END as Status
FROM Badges b
LEFT JOIN BadgeLocationConfigs blc ON b.Id = blc.BadgeId
WHERE b.IsDeleted = 0
GROUP BY b.Id, b.Name
HAVING COUNT(blc.Id) != 3;

-- Should return 0 rows if all badges migrated correctly

-- 2. Check for invalid ratios
SELECT *
FROM BadgeLocationConfigs
WHERE
    OffsetXRatio < 0 OR OffsetXRatio > 1 OR
    OffsetYRatio < 0 OR OffsetYRatio > 1 OR
    SizeWidthRatio <= 0 OR SizeWidthRatio > 1 OR
    SizeHeightRatio <= 0 OR SizeHeightRatio > 1;

-- Should return 0 rows

-- 3. Check for duplicate location configs
SELECT
    BadgeId,
    Location,
    COUNT(*) as DuplicateCount
FROM BadgeLocationConfigs
GROUP BY BadgeId, Location
HAVING COUNT(*) > 1;

-- Should return 0 rows

-- 4. Summary statistics
SELECT
    Location,
    COUNT(*) as TotalConfigs,
    AVG(SizeWidthRatio) as AvgWidthRatio,
    AVG(SizeHeightRatio) as AvgHeightRatio,
    COUNT(CASE WHEN IsEnabled = 1 THEN 1 END) as EnabledCount
FROM BadgeLocationConfigs
GROUP BY Location
ORDER BY Location;

-- Expected output:
-- Location 0 (EventsListing): ~N configs, avg 0.14 width, 0.19 height
-- Location 1 (HomeFeatured): ~N configs, avg 0.15 width, 0.20 height
-- Location 2 (EventDetailHero): ~N configs, avg 0.14 width, 0.17 height
```

---

## 9. Risk Assessment

### 9.1 Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| **Database Migration Failure** | Low | High | - Test migration on staging<br>- Keep Position column during transition<br>- Automated rollback script |
| **Performance Degradation** | Medium | Medium | - Index on (BadgeId, Location)<br>- Lazy-load location configs<br>- Client-side caching |
| **Complex UI State Management** | Medium | Medium | - Use React Hook Form for state<br>- Throttle drag/resize events<br>- Unit test state transitions |
| **Responsive Calculation Errors** | Low | High | - Extensive unit tests for calculation engine<br>- Boundary validation (min 1%, max 100%)<br>- Visual regression tests |
| **Backward Compatibility Break** | Low | High | - Keep Position field until Phase 6A.32<br>- Support both old/new API formats<br>- Progressive frontend rollout |

### 9.2 User Experience Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| **UI Too Complex** | Medium | Medium | - Provide "Use same for all" quick action<br>- Smart defaults (copy from Event Detail)<br>- Video tutorial for badge creation |
| **Badge Outside Container** | Low | High | - Real-time validation in canvas<br>- Boundary clamping in calculations<br>- Visual warnings when too close to edge |
| **Inconsistent Badge Sizes** | Medium | Low | - Lock aspect ratio by default<br>- Show percentage preview<br>- Template library with presets |
| **Mobile Responsiveness Issues** | Low | Medium | - Test on actual mobile devices<br>- Min badge size validation (1%)<br>- Fallback to center position if calc fails |

### 9.3 Business Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| **Increased Support Requests** | Medium | Low | - In-app tooltips and help text<br>- FAQ documentation<br>- Admin can reset badges to defaults |
| **Data Loss During Migration** | Low | High | - Full database backup before migration<br>- Dry-run on production replica<br>- Audit log of changes |
| **Slow Adoption** | Medium | Low | - Auto-migrate existing badges with sensible defaults<br>- No user action required initially<br>- Gradual rollout to EventOrganizers |

---

## 10. Implementation Roadmap

### Phase 6A.31.1: Database and Domain (Week 1)

**Day 1-2: Domain Model**
- [ ] Create `DisplayLocation` enum
- [ ] Rename `BadgePosition` to `BadgeCorner` enum
- [ ] Create `BadgeLocationConfig` entity
- [ ] Update `Badge` entity (add `LocationConfigs` navigation)
- [ ] Write domain unit tests (300+ tests for validation logic)

**Day 3-4: Data Layer**
- [ ] Create EF Core configuration for `BadgeLocationConfig`
- [ ] Update `BadgeConfiguration` (make Position nullable)
- [ ] Write migration `AddBadgeLocationConfigsPhase6A31`
- [ ] Test migration on local SQL Server
- [ ] Write validation script

**Day 5: Testing**
- [ ] Run migration on staging database
- [ ] Verify all badges have 3 location configs
- [ ] Check no invalid ratios
- [ ] Performance test (query 1000 badges with configs)
- [ ] Deploy to staging

### Phase 6A.31.2: Application Layer (Week 2)

**Day 1-2: DTOs and Commands**
- [ ] Create `BadgeLocationConfigDto`
- [ ] Update `BadgeDto` (add `LocationConfigs` array)
- [ ] Create `CreateBadgeLocationConfigDto`
- [ ] Update `CreateBadgeCommand` (add `LocationConfigs` parameter)
- [ ] Update `UpdateBadgeCommand`
- [ ] Write validators

**Day 3-4: Handlers and Repositories**
- [ ] Update `CreateBadgeCommandHandler`
- [ ] Create `UpdateBadgeLocationConfigsCommandHandler`
- [ ] Update `GetBadgeQueryHandler` (include location configs)
- [ ] Update `IBadgeRepository` interface
- [ ] Implement repository methods

**Day 5: API Endpoints**
- [ ] Update `POST /api/badges` (accept location configs)
- [ ] Create `PUT /api/badges/{id}/location-configs`
- [ ] Update `GET /api/badges/{id}` (return location configs)
- [ ] Update Swagger documentation
- [ ] Test with Postman

### Phase 6A.31.3: Frontend Utilities (Week 3)

**Day 1-2: Calculation Engine**
- [ ] Create `badgePositioning.ts` utility file
- [ ] Implement `calculateBadgeLayout()`
- [ ] Implement `pixelToRatio()`
- [ ] Implement `pixelSizeToRatio()`
- [ ] Implement `calculateAspectRatioHeight()`
- [ ] Write 50+ unit tests (Vitest)

**Day 3: Type Definitions**
- [ ] Create `DisplayLocation` enum (TypeScript)
- [ ] Update `BadgeCorner` enum
- [ ] Create `BadgeLocationConfig` interface
- [ ] Update `BadgeDto` type
- [ ] Update API client types

**Day 4-5: Repository Updates**
- [ ] Update `BadgesRepository.createBadge()` (send location configs)
- [ ] Update `BadgesRepository.updateLocationConfigs()`
- [ ] Update `BadgesRepository.getBadge()` (parse location configs)
- [ ] Test API integration

### Phase 6A.31.4: UI Components (Week 4)

**Day 1-2: Canvas Editor**
- [ ] Create `BadgeCanvasEditor.tsx` component
- [ ] Implement draggable badge
- [ ] Implement resize handles (8 total)
- [ ] Add position guides (grid lines)
- [ ] Add real-time dimension display
- [ ] Test on 3 container sizes

**Day 3: Position and Size Controls**
- [ ] Create `BadgePositionControls.tsx`
- [ ] Implement anchor corner dropdown
- [ ] Implement X/Y offset inputs (with pixel ↔ ratio conversion)
- [ ] Create `BadgeSizeControls.tsx`
- [ ] Implement width/height inputs
- [ ] Implement aspect ratio lock checkbox

**Day 4-5: Customization Dialog**
- [ ] Create `BadgeCustomizationDialog.tsx`
- [ ] Implement tab navigation (Events Listing, Home Featured, Event Detail)
- [ ] Wire up 3 tab panels with canvas editors
- [ ] Add quick actions (Copy, Reset, Use Same for All)
- [ ] Implement form state management (React Hook Form)
- [ ] Add validation errors display

### Phase 6A.31.5: Integration and Testing (Week 5)

**Day 1-2: Badge Display Components**
- [ ] Update `BadgeOverlayGroup.tsx` (use `LocationConfigs`)
- [ ] Remove hardcoded sizes (50px, 42px, 80px)
- [ ] Implement `calculateBadgeLayout()` for responsive rendering
- [ ] Add fallback to legacy `Position` if no configs
- [ ] Test on all 3 display locations

**Day 3: End-to-End Testing**
- [ ] Create new badge with custom locations (Playwright test)
- [ ] Drag badge on canvas
- [ ] Resize badge maintaining aspect ratio
- [ ] Save and verify badge appears correctly on all 3 locations
- [ ] Test on mobile viewport

**Day 4: Visual Regression Testing**
- [ ] Capture screenshots of badges at all 3 locations
- [ ] Compare with Phase 6A.30 screenshots (should match defaults)
- [ ] Verify responsive scaling (100px, 192px, 384px containers)
- [ ] Check badge positioning accuracy (± 2px tolerance)

**Day 5: Documentation and Deploy**
- [ ] Create Phase 6A.31 summary document
- [ ] Update PROGRESS_TRACKER.md
- [ ] Update STREAMLINED_ACTION_PLAN.md
- [ ] Update PHASE_6A_MASTER_INDEX.md
- [ ] Deploy to staging
- [ ] Deploy to production

### Post-Deployment (Week 6)

**Day 1-2: Monitoring**
- [ ] Monitor application logs for positioning errors
- [ ] Check database query performance (avg < 50ms)
- [ ] Review user feedback on badge customization
- [ ] Fix any critical bugs

**Day 3-5: Future Enhancements (Phase 6A.32)**
- [ ] Remove deprecated `Position` column from database
- [ ] Remove `Position` from API responses
- [ ] Add badge template library
- [ ] Add rotation support (future enhancement)
- [ ] Add opacity/transparency support (future enhancement)

---

## 11. Success Criteria

### Functional Requirements
- [ ] Badges can be positioned at any corner with pixel-precise offsets
- [ ] Badges can be sized independently at each of 3 display locations
- [ ] Badge size scales responsively from mobile (100px) to desktop (384px)
- [ ] Drag-and-drop canvas allows visual badge positioning
- [ ] Pixel inputs auto-convert to/from ratios transparently
- [ ] Aspect ratio lock maintains badge proportions
- [ ] All existing badges migrated with sensible defaults
- [ ] No breaking changes to existing API consumers

### Non-Functional Requirements
- [ ] Badge positioning calculation < 5ms on average device
- [ ] Database query time < 50ms for badge with location configs
- [ ] UI remains responsive during drag operations (60fps)
- [ ] Migration completes in < 30 seconds for 1000 badges
- [ ] Zero data loss during migration (100% badges migrated)
- [ ] Backward compatible API for 2 releases (Phase 6A.31 + 6A.32)

### Quality Attributes
- [ ] 90% test coverage on calculation engine
- [ ] Visual regression tests pass (± 2px tolerance)
- [ ] No accessibility regressions (WCAG 2.1 AA compliant)
- [ ] Database constraints prevent invalid ratios
- [ ] Clean architecture principles followed (DDD, CQRS)

---

## 12. Appendices

### Appendix A: Container Dimension Reference

| Display Location | Tailwind Class | Default Width | Default Height | Responsive Behavior |
|-----------------|---------------|---------------|----------------|---------------------|
| Events Listing | `h-48` | 192px | 144px | Fixed aspect ratio (4:3) |
| Home Featured | `h-40` | Variable (100%) | 160px | Full width, fixed height |
| Event Detail Hero | `h-96` | Variable (100%) | 384px | Full width, fixed height |

### Appendix B: Default Badge Size Ratios

Based on Phase 6A.30 hardcoded sizes:

| Location | Old Hardcoded Size | Container Size | Width Ratio | Height Ratio |
|----------|-------------------|----------------|-------------|--------------|
| Events Listing | 27px × 27px | 192×144 | 0.1406 (14.06%) | 0.1875 (18.75%) |
| Home Featured | 42px × 42px | 160×120 | 0.2625 (26.25%) | 0.3500 (35.00%) |
| Event Detail Hero | 50px × 50px | 350×288 | 0.1429 (14.29%) | 0.1736 (17.36%) |

**Note**: Home Featured had oversized badge (42px) - migration uses more reasonable 24px (15%) for consistency.

### Appendix C: Calculation Examples

**Example 1: Top Right Badge on Mobile**

Given:
- Container: 100px × 80px
- Config: `{ anchorCorner: TopRight, offsetXRatio: 0, offsetYRatio: 0, sizeWidthRatio: 0.143, sizeHeightRatio: 0.174 }`

Calculation:
```
badgeWidth = 100 * 0.143 = 14.3 → 14px
badgeHeight = 80 * 0.174 = 13.92 → 14px
x = 100 * (1 - 0) - 14 = 86px
y = 80 * 0 = 0px
```

Result: `{ x: 86, y: 0, width: 14, height: 14 }`

**Example 2: Bottom Left Badge with Offset**

Given:
- Container: 384px × 288px
- Config: `{ anchorCorner: BottomLeft, offsetXRatio: 0.05, offsetYRatio: 0.05, sizeWidthRatio: 0.10, sizeHeightRatio: 0.10 }`

Calculation:
```
badgeWidth = 384 * 0.10 = 38.4 → 38px
badgeHeight = 288 * 0.10 = 28.8 → 29px
x = 384 * 0.05 = 19.2 → 19px
y = 288 * (1 - 0.05) - 29 = 244.6 → 245px
```

Result: `{ x: 19, y: 245, width: 38, height: 29 }`

### Appendix D: Glossary

| Term | Definition |
|------|------------|
| **Anchor Corner** | Reference point (TopLeft/TopRight/BottomLeft/BottomRight) from which badge position is calculated |
| **Aspect Ratio Lock** | Constraint that maintains badge width:height ratio during resizing |
| **Display Location** | Application context where badge is shown (Events Listing, Home Featured, Event Detail Hero) |
| **Offset Ratio** | Distance from anchor corner as percentage of container dimensions (0.0-1.0) |
| **Size Ratio** | Badge dimensions as percentage of container dimensions (0.0-1.0) |
| **Container** | Parent HTML element that holds the event image and badge overlay |

---

## Document Revision History

| Date | Version | Author | Changes |
|------|---------|--------|---------|
| 2025-12-14 | 1.0 | System Architecture Designer | Initial design proposal |

---

**End of Architecture Design Document**
