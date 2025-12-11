# Feed Display Components

This directory contains the feed display components for the LankaConnect platform, following Domain-Driven Design principles and Clean Architecture.

## Components

### FeedCard.tsx
Displays individual feed items with type-specific styling and interactions.

**Features:**
- Author avatar with Sri Lankan flag gradient (#FF7900 → #8B1538)
- Type-specific badges (Event, Business, Forum, Culture)
- Metadata display based on feed type
- Interactive action buttons (like, comment, share, etc.)
- Hover effect with warm background (#fff9f5)
- Border accent color based on feed type

**Props:**
```typescript
interface FeedCardProps {
  item: FeedItem;           // Domain model
  onClick?: (item: FeedItem) => void;
  className?: string;
}
```

**Usage:**
```tsx
<FeedCard
  item={feedItem}
  onClick={(item) => router.push(`/feed/${item.id}`)}
/>
```

### FeedTabs.tsx
Tab navigation for filtering feed content by type.

**Features:**
- Tabs: All | Events | Businesses | Culture | Forums
- Active tab with saffron underline (#FF7900)
- Optional badge counts per tab
- Horizontal scroll on mobile (responsive)
- Smooth transitions and hover effects

**Props:**
```typescript
interface FeedTabsProps {
  activeTab: FeedTabValue;  // 'all' | FeedType
  onTabChange: (tab: FeedTabValue) => void;
  counts?: Partial<Record<FeedTabValue, number>>;
  className?: string;
}
```

**Usage:**
```tsx
<FeedTabs
  activeTab={activeTab}
  onTabChange={setActiveTab}
  counts={{ all: 42, event: 12, business: 8 }}
/>
```

### ActivityFeed.tsx
Container component for feed display with loading and pagination.

**Features:**
- Grid layout for feed items
- Skeleton loaders during initial load
- Empty state with call-to-action
- Pagination support (10 items per page by default)
- "Load More" button with loading state
- Flexible pagination modes (local or API-driven)

**Props:**
```typescript
interface ActivityFeedProps {
  items: FeedItem[];
  loading?: boolean;
  emptyMessage?: string;
  onItemClick?: (item: FeedItem) => void;
  itemsPerPage?: number;
  hasMore?: boolean;
  onLoadMore?: () => void;
  loadingMore?: boolean;
  className?: string;
}
```

**Usage:**
```tsx
<ActivityFeed
  items={feedItems}
  loading={isLoading}
  emptyMessage="Start by creating your first post!"
  onItemClick={(item) => router.push(`/feed/${item.id}`)}
  hasMore={hasMoreItems}
  onLoadMore={loadMoreItems}
/>
```

## Domain Integration

These components use the following domain models and constants:

### Domain Models
- `FeedItem` - Main aggregate root
- `FeedAuthor` - Value object for author info
- `FeedAction` - Value object for actions
- `EventMetadata`, `BusinessMetadata`, `ForumMetadata`, `CultureMetadata` - Type-specific metadata

### Constants
- `FEED_TYPE_COLORS` - Color schemes per feed type
- `FEED_TYPE_LABELS` - Display labels and descriptions
- Helper functions: `isEventMetadata()`, `isBusinessMetadata()`, etc.

## Color Scheme

Following Sri Lankan flag colors throughout:

- **Saffron Orange**: `#FF7900` - Primary accent, active states
- **Maroon**: `#8B1538` - Secondary accent, gradients
- **Warm Background**: `#fff9f5` - Hover states
- **Type-specific colors**:
  - Event: Blue
  - Business: Green
  - Forum: Purple
  - Culture: Orange

## Dependencies

- `lucide-react` - Icons
- `date-fns` - Timestamp formatting
- `@/presentation/components/ui/Card` - Base card component
- `@/presentation/components/ui/Button` - Base button component

## File Organization

```
feed/
├── FeedCard.tsx       # Individual feed item display
├── FeedTabs.tsx       # Tab navigation
├── ActivityFeed.tsx   # Feed container with pagination
├── index.ts           # Barrel exports
└── README.md          # This file
```

## TypeScript

All components are fully typed with:
- Interface definitions for props
- JSDoc comments for documentation
- Type guards for metadata discrimination
- Proper readonly modifiers for domain models

## Responsive Design

- Mobile-first approach
- Horizontal scroll for tabs on mobile
- Flexible grid layout for feed items
- Touch-friendly tap targets (minimum 44px)

## Accessibility

- Semantic HTML elements
- ARIA labels for navigation
- Keyboard navigation support
- Focus indicators
- Screen reader friendly

## Next Steps

To integrate these components into the dashboard:

1. Import the components:
```tsx
import { FeedTabs, ActivityFeed } from '@/presentation/components/features/feed';
```

2. Set up state management:
```tsx
const [activeTab, setActiveTab] = useState<FeedTabValue>('all');
const [feedItems, setFeedItems] = useState<FeedItem[]>([]);
const [loading, setLoading] = useState(false);
```

3. Render the components:
```tsx
<FeedTabs activeTab={activeTab} onTabChange={setActiveTab} />
<ActivityFeed items={filteredItems} loading={loading} />
```

## Testing Considerations

- Test type-specific rendering (events, businesses, forums, culture)
- Test empty states
- Test loading states
- Test pagination behavior
- Test tab navigation
- Test responsive behavior
