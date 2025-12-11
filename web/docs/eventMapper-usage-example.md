# EventMapper Usage Examples

This document provides examples of how to use the `eventMapper.ts` utility to convert backend EventDto to FeedItem for the landing page feed.

## Basic Usage

### Converting Single Event

```typescript
import { mapEventToFeedItem } from '@/presentation/utils/eventMapper';
import { eventsApi } from '@/infrastructure/api/events.api';

// Fetch event from API
const event = await eventsApi.getEvent('event-123');

// Convert to FeedItem
const feedItem = mapEventToFeedItem(event);

// Use in FeedCard component
<FeedCard item={feedItem} />
```

### Converting Event List

```typescript
import { mapEventListToFeedItems } from '@/presentation/utils/eventMapper';
import { eventsApi } from '@/infrastructure/api/events.api';

// Fetch multiple events
const events = await eventsApi.getEvents();

// Convert all to FeedItems
const feedItems = mapEventListToFeedItems(events);

// Render in feed
<ActivityFeed items={feedItems} />
```

## Advanced Usage

### With Event Thumbnails

```typescript
import { mapEventToFeedItem, getEventThumbnail } from '@/presentation/utils/eventMapper';

const event = await eventsApi.getEvent('event-123');
const feedItem = mapEventToFeedItem(event);
const thumbnail = getEventThumbnail(event);

// Use thumbnail in UI
<img src={thumbnail} alt={feedItem.title} />
```

### Checking Event Capacity

```typescript
import {
  mapEventToFeedItem,
  isEventFull,
  getAvailableSpots
} from '@/presentation/utils/eventMapper';

const event = await eventsApi.getEvent('event-123');
const feedItem = mapEventToFeedItem(event);

if (isEventFull(event)) {
  return <Badge>Sold Out</Badge>;
}

const spotsLeft = getAvailableSpots(event);
return <Badge>{spotsLeft} spots remaining</Badge>;
```

### Formatting Event Prices

```typescript
import { mapEventToFeedItem, formatEventPrice } from '@/presentation/utils/eventMapper';

const event = await eventsApi.getEvent('event-123');
const feedItem = mapEventToFeedItem(event);
const priceLabel = formatEventPrice(event);

// Displays "Free", "$25.00", or "LKR 500"
<span>{priceLabel}</span>
```

### Formatting Date Ranges

```typescript
import { mapEventToFeedItem, formatEventDateRange } from '@/presentation/utils/eventMapper';

const event = await eventsApi.getEvent('event-123');
const feedItem = mapEventToFeedItem(event);
const dateRange = formatEventDateRange(event);

// Displays "Mar 15, 2024" or "Mar 15, 2024 - Mar 17, 2024"
<span>{dateRange}</span>
```

## Integration with React Query

### Custom Hook with Mapping

```typescript
import { useQuery } from '@tanstack/react-query';
import { eventsApi } from '@/infrastructure/api/events.api';
import { mapEventListToFeedItems } from '@/presentation/utils/eventMapper';

export function useEventsFeed() {
  return useQuery({
    queryKey: ['events', 'feed'],
    queryFn: async () => {
      const events = await eventsApi.getEvents({
        status: EventStatus.Published,
      });
      return mapEventListToFeedItems(events);
    },
  });
}

// Usage in component
function FeedPage() {
  const { data: feedItems, isLoading } = useEventsFeed();

  if (isLoading) return <LoadingSpinner />;

  return <ActivityFeed items={feedItems} />;
}
```

## Edge Cases Handled

### 1. Events Without Location

```typescript
// Event with no physical location
const onlineEvent = {
  ...eventDto,
  address: null,
  city: null,
  state: null,
};

const feedItem = mapEventToFeedItem(onlineEvent);
// feedItem.location will be "Online"
```

### 2. Events Without Images

```typescript
const eventWithoutImage = {
  ...eventDto,
  images: [],
};

const thumbnail = getEventThumbnail(eventWithoutImage);
// Returns '/images/placeholder-event.jpg'
```

### 3. Free Events

```typescript
const freeEvent = {
  ...eventDto,
  isFree: true,
  ticketPriceAmount: null,
};

const price = formatEventPrice(freeEvent);
// Returns 'Free'
```

### 4. Single-Day vs Multi-Day Events

```typescript
// Single-day event
const singleDay = {
  ...eventDto,
  startDate: '2024-04-13T10:00:00Z',
  endDate: '2024-04-13T18:00:00Z',
};

formatEventDateRange(singleDay);
// Returns "Apr 13, 2024"

// Multi-day event
const multiDay = {
  ...eventDto,
  startDate: '2024-04-13T10:00:00Z',
  endDate: '2024-04-15T18:00:00Z',
};

formatEventDateRange(multiDay);
// Returns "Apr 13, 2024 - Apr 15, 2024"
```

## Type Safety

All functions are fully typed with TypeScript:

```typescript
// ✓ Type-safe mapping
const event: EventDto = await eventsApi.getEvent('123');
const feedItem: FeedItem = mapEventToFeedItem(event);

// ✓ Compile-time checks
feedItem.type; // 'event' | 'business' | 'forum' | 'culture'
feedItem.metadata; // EventMetadata | BusinessMetadata | ...

// ✓ Type guards available
if (isEventMetadata(feedItem.metadata)) {
  feedItem.metadata.date; // string
  feedItem.metadata.venue; // string | undefined
}
```

## Error Handling

The mapper uses the `createFeedItem` factory function which includes validation:

```typescript
try {
  const feedItem = mapEventToFeedItem(invalidEvent);
} catch (error) {
  // Throws if:
  // - Event ID is empty
  // - Event title is empty
  // - Author name is empty
  // - Metadata type doesn't match feed type
  console.error('Invalid event data:', error);
}
```

## Testing

Example test structure:

```typescript
import { describe, it, expect } from 'vitest';
import { mapEventToFeedItem } from '@/presentation/utils/eventMapper';

describe('mapEventToFeedItem', () => {
  it('should map EventDto to FeedItem', () => {
    const event = createMockEvent();
    const feedItem = mapEventToFeedItem(event);

    expect(feedItem.id).toBe(event.id);
    expect(feedItem.type).toBe('event');
    expect(feedItem.title).toBe(event.title);
  });
});
```

## Best Practices

1. **Always use the mapper** - Don't manually create FeedItem objects from EventDto
2. **Cache mapped results** - Use React Query to cache the converted feed items
3. **Handle errors gracefully** - Wrap mapper calls in try-catch for invalid data
4. **Use helper functions** - Leverage `formatEventPrice`, `getEventThumbnail`, etc.
5. **Type guards** - Use `isEventMetadata()` when accessing event-specific fields

## Future Enhancements

- [ ] Add organizer details when user API is available
- [ ] Include comment counts when comments feature is implemented
- [ ] Add share counts when sharing feature is implemented
- [ ] Support for RSVP status (interested/not interested)
- [ ] Add event images carousel support
- [ ] Support for event videos in feed cards
